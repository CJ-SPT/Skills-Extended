param(
    [string]$Sdk = 'F:\SPT 4.1.x\Development\CJ-SDK',
    [string]$Eve = 'E:\Eve',
    [string]$Unity = 'F:\Unity\Editor\2022.3.43f1\Editor\Unity.exe',
    [Parameter(Mandatory)][string]$Decoder
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
$decoderPath = (Resolve-Path -LiteralPath $Decoder).Path
$output = Join-Path $repo 'artifacts\electronics-ui'
Push-Location $repo
try {
    & "$PSScriptRoot\Stage-Assets.ps1" -Sdk $Sdk
    & "$PSScriptRoot\Import-EveAssets.ps1" -Sdk $Sdk -Eve $Eve
    python "$PSScriptRoot\Import-EveSounds.py" --eve $Eve --sdk $Sdk --decoder $decoderPath
    if ($LASTEXITCODE -ne 0) { throw 'EVE audio import failed.' }
    dotnet run --project Tests/SkillsExtended.ElectronicsTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false -- --write-preview
    if ($LASTEXITCODE -ne 0) { throw 'Simulation/preview fixture checks failed.' }
    $arguments = '-batchmode -quit -projectPath "{0}" -executeMethod ElectronicsUiBuilder.Build -electronicsOutput "{1}" -electronicsPreview -logFile "{1}\build-local.log"' -f $Sdk, $output
    $started = Get-Date
    $build = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    # Wait for this editor process, not the process tree (licensing helpers may outlive it).
    $build.WaitForExit()
    if ($build.ExitCode -ne 0) { throw "Unity build failed. See $output\build-local.log" }
    $receipt = Get-Item -LiteralPath "$output\validation.txt"
    if ($receipt.LastWriteTime -lt $started) { throw 'Unity did not produce a fresh validation receipt.' }
    Copy-Item -LiteralPath "$output\electronics_ui.bundle" -Destination "$repo\Client\SkillsExtended.Client.Skills\Resources\bundles\electronics_ui.bundle"
    Copy-Item -LiteralPath "$Sdk\Assets\Mods\SkillsExtended.Assets\Inputs\eve-source-receipt.json", "$Sdk\Assets\Mods\SkillsExtended.Assets\Inputs\eve-audio-receipt.json" -Destination "$repo\Client\SkillsExtended.Client.Skills\Resources\Notices"
    Write-Output "Validated local bundle copied into client Resources. Layout previews: $output"
} finally { Pop-Location }
