param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$NativeContainers,
    [string]$Sdk = 'F:\SPT 4.1.x\Development\CJ-SDK',
    [string]$Unity = 'F:\Unity\Editor\2022.3.43f1\Editor\Unity.exe',
    [switch]$RunUnityBatch
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
$output = Join-Path $repo 'artifacts\signals-case'
$builder = Join-Path $Sdk 'Assets\Mods\SkillsExtended.Assets\Editor\SignalsCaseVisualBuilder.cs'
$libraryPath = Join-Path $repo 'artifacts\signals-python'
$previousPythonPath = $env:PYTHONPATH
try {
    $env:PYTHONPATH = $libraryPath
    python "$PSScriptRoot\Stage-Case.py" $Package $Sdk "$output\source-receipt.json"
    if ($LASTEXITCODE -ne 0) { throw 'Olive case staging failed.' }
    python "$PSScriptRoot\Extract-Case.py" $NativeContainers "$output\native-toolbox-complete.bundle"
    if ($LASTEXITCODE -ne 0) { throw 'Native component/sound extraction failed.' }
    Copy-Item -LiteralPath "$PSScriptRoot\SignalsCaseVisualBuilder.cs" -Destination $builder
    if (-not $RunUnityBatch) {
        Write-Output 'Staged the olive case and builder. No editor was launched. Use -RunUnityBatch for the offline Unity asset build.'
        return
    }
    if (Test-Path -LiteralPath (Join-Path $Sdk 'Temp\UnityLockfile')) {
        throw 'CJ-SDK is locked by an editor. Leave that editor under user control and build when it is available.'
    }
    $started = Get-Date
    $arguments = '-batchmode -quit -projectPath "{0}" -executeMethod SignalsCaseVisualBuilder.Build -signalsOutput "{1}" -logFile "{1}\build.log"' -f $Sdk, $output
    $build = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $build.WaitForExit()
    if ($build.ExitCode -ne 0) { throw "Unity asset build failed. See $output\build.log" }
    $validation = Get-Item -LiteralPath "$output\visual-validation.json"
    if ($validation.LastWriteTime -lt $started) { throw 'Unity did not write a fresh visual validation receipt.' }
    python "$PSScriptRoot\Graft-Case.py" "$output\signal_case_visual.bundle" "$output\native-toolbox-complete.bundle" "$output\signal_case.bundle"
    if ($LASTEXITCODE -ne 0) { throw 'Native container graft or validation failed.' }
    $verifiedAt = Get-Date
    $verifyArguments = '-batchmode -quit -projectPath "{0}" -executeMethod SignalsCaseVisualBuilder.ValidatePackaged -signalsOutput "{1}" -logFile "{1}\verify-packaged.log"' -f $Sdk, $output
    $verify = Start-Process -FilePath $Unity -ArgumentList $verifyArguments -WindowStyle Hidden -PassThru
    $verify.WaitForExit()
    if ($verify.ExitCode -ne 0) { throw "Final bundle failed to load/render in Unity. See $output\verify-packaged.log" }
    $verified = Get-Item -LiteralPath "$output\packaged-visual-validation.txt"
    if ($verified.LastWriteTime -lt $verifiedAt) { throw 'Unity did not write a fresh final-bundle validation receipt.' }
    Copy-Item -LiteralPath "$output\signal_case.bundle" -Destination "$repo\Client\SkillsExtended.Client.Skills\Resources\bundles\signal_case.bundle"
    Copy-Item -LiteralPath "$output\source-receipt.json" -Destination "$repo\Client\SkillsExtended.Client.Skills\Resources\Notices\SignalsCase-Source.json"
    Write-Output "Validated olive cache bundle copied into client Resources. Previews and validation: $output"
} finally {
    $env:PYTHONPATH = $previousPythonPath
}
