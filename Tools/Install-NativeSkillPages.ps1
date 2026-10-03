# Install the validated skill pages, original icons, and game-default controls.
# Close the SPT server and connected game/headless clients yourself before running.
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskInstalls = @(
    @{ root = 'F:\SPT 4.1.x'; receipt = 'artifacts\native-icons-defaults-validation-main.json' },
    @{ root = 'F:\SPT 4.1.x - Headless'; receipt = 'artifacts\native-icons-defaults-validation-headless.json' }
)
# Check both installations before changing either one.
foreach ($taskInstall in $taskInstalls) {
    $taskRoot = (Resolve-Path -LiteralPath $taskInstall.root).Path
    $taskReceipt = Get-Content -LiteralPath (Join-Path $taskRepo $taskInstall.receipt) -Raw | ConvertFrom-Json
    if ($taskReceipt.result -ne 'passed') { throw 'Validation must pass before installation.' }
    foreach ($taskFile in $taskReceipt.files) {
        $taskSource = [IO.Path]::GetFullPath((Join-Path $taskRepo $taskFile.source))
        $taskTarget = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskFile.destination))
        if (-not $taskSource.StartsWith($taskRepo + '\', [StringComparison]::OrdinalIgnoreCase) -or
            -not $taskTarget.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'An install path is outside its expected directory.'
        }
        if ((Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash -ne $taskFile.sha256) {
            throw "Validated source changed: $taskSource"
        }
        if (Test-Path -LiteralPath $taskTarget) {
            try {
                $taskHandle = [IO.File]::Open($taskTarget, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                $taskHandle.Dispose()
            } catch {
                throw "Installation blocked by $taskTarget. Close its application manually and run this installer again. No installed files were changed."
            }
        }
    }
}
foreach ($taskInstall in $taskInstalls) {
    & (Join-Path $PSScriptRoot 'Install-DeveloperEditor.ps1') -TarkovDir $taskInstall.root `
        -ValidationReceipt (Join-Path $taskRepo $taskInstall.receipt)
}
