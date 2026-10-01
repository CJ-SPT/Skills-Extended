param(
    [string]$TarkovDir = 'F:\SPT 4.1.x',
    [string]$ValidationReceipt = (Join-Path $PSScriptRoot '..\artifacts\developer-editor\validation.json')
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$game = (Resolve-Path -LiteralPath $TarkovDir).Path
$receipt = Get-Content -Raw -LiteralPath $ValidationReceipt | ConvertFrom-Json
if ($receipt.result -ne 'passed') { throw 'The developer editor validation receipt must pass before installation.' }
$files = @($receipt.files)
foreach ($file in $files) {
    $source = [IO.Path]::GetFullPath((Join-Path $repo $file.source))
    $destination = [IO.Path]::GetFullPath((Join-Path $game $file.destination))
    if (-not $source.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase) -or
        -not $destination.StartsWith($game + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Install receipt contains an out-of-scope path.'
    }
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $file.sha256) {
        throw "Validated build changed: $source. Run validation again."
    }
    if (Test-Path -LiteralPath $destination) {
        try {
            $handle = [IO.File]::Open($destination, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $handle.Dispose()
        } catch {
            $app = if ($file.destination.StartsWith('SPT_Runtime')) { 'SPT server' } else { 'game client' }
            throw "Installation blocked by $destination. Close the $app manually and rerun this installer. No installed files were changed."
        }
    }
}
$configs = Join-Path $game 'SPT_Runtime\user\mods\SkillsExtended\Resources\Configs'
$configHashes = @{}
if (Test-Path -LiteralPath $configs) {
    Get-ChildItem -LiteralPath $configs -File | ForEach-Object { $configHashes[$_.FullName] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$backup = Join-Path $repo ('artifacts\developer-editor\install-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
$existing = @{}
foreach ($file in $files) {
    $destination = Join-Path $game $file.destination
    $prior = Join-Path $backup $file.destination
    $existing[$file.destination] = Test-Path -LiteralPath $destination
    if ($existing[$file.destination]) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $prior) -Force | Out-Null
        Copy-Item -LiteralPath $destination -Destination $prior
    }
}
$replaced = [Collections.Generic.List[string]]::new()
try {
    foreach ($file in $files) {
        $destination = Join-Path $game $file.destination
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repo $file.source) -Destination $destination -Force
        $replaced.Add($file.destination)
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $file.sha256) { throw "Installed hash mismatch: $destination" }
    }
    foreach ($path in $configHashes.Keys) {
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $configHashes[$path]) { throw "Configuration changed during installation: $path" }
    }
} catch {
    foreach ($relative in $replaced) {
        $destination = [IO.Path]::GetFullPath((Join-Path $game $relative))
        if (-not $destination.StartsWith($game + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Rollback path is outside the installation.' }
        if ($existing[$relative]) { Copy-Item -LiteralPath (Join-Path $backup $relative) -Destination $destination -Force }
        else { Remove-Item -LiteralPath $destination }
    }
    throw
}
[ordered]@{
    installed_at = (Get-Date -Format o)
    files = $files
    configuration_files_preserved = $configHashes.Count
    backup = $backup
    validation = [IO.Path]::GetFullPath($ValidationReceipt)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backup 'installation.json') -Encoding utf8
Write-Output "Installed and SHA-256 verified $($files.Count) files. Preserved $($configHashes.Count) configuration files. Backup: $backup"
Write-Output 'Restart the SPT server and game client manually to load the update.'
