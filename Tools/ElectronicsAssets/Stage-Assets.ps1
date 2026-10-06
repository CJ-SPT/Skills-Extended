param([string]$Sdk = 'F:\SPT 4.1.x\Development\CJ-SDK')
$ErrorActionPreference = 'Stop'
$destination = Join-Path $Sdk 'Assets\Mods\SkillsExtended.Assets'
New-Item -ItemType Directory -Force -Path "$destination\Editor", "$destination\Inputs", "$destination\Runtime" | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot\ElectronicsUiBuilder.cs" -Destination "$destination\Editor\ElectronicsUiBuilder.cs"
Copy-Item -LiteralPath "$PSScriptRoot\ElectronicsHudArtwork.cs" -Destination "$destination\Editor\ElectronicsHudArtwork.cs"
Copy-Item -LiteralPath "$PSScriptRoot\ElectronicsLine.shader" -Destination "$destination\ElectronicsLine.shader"
Copy-Item -LiteralPath "$PSScriptRoot\ElectronicsGauge.shader" -Destination "$destination\ElectronicsGauge.shader"
Get-ChildItem -LiteralPath "$PSScriptRoot\Inputs" -File | Where-Object Extension -in '.png', '.ogg' | Copy-Item -Destination "$destination\Inputs"
Write-Output "Staged Skills Extended authoring inputs at $destination. No application was launched."

Copy-Item -LiteralPath "$PSScriptRoot\..\..\Client\SkillsExtended.Client.Skills\Skills\Electronics\ElectronicsBoardFx.cs" -Destination "$destination\Runtime\ElectronicsBoardFx.cs"
Copy-Item -LiteralPath "$PSScriptRoot\..\..\Client\SkillsExtended.Client.Skills\Skills\Electronics\ElectronicsUiVisuals.cs" -Destination "$destination\Runtime\ElectronicsUiVisuals.cs"
$commonProject = Join-Path $PSScriptRoot '..\..\SkillsExtended.Common\SkillsExtended.Common.csproj'
& dotnet build $commonProject -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'Could not build the shared localization library.' }
Copy-Item -LiteralPath "$PSScriptRoot\..\..\SkillsExtended.Common\bin\Release\netstandard2.1\SkillsExtended.Common.dll" -Destination "$destination\Runtime\SkillsExtended.Common.dll"
# This shared type is now also consumed by the runtime animation-preview component.
if (Test-Path -LiteralPath "$destination\Editor\ElectronicsUiVisuals.cs") { Remove-Item -LiteralPath "$destination\Editor\ElectronicsUiVisuals.cs" }
if (Test-Path -LiteralPath "$destination\Editor\ElectronicsUiVisuals.cs.meta") { Remove-Item -LiteralPath "$destination\Editor\ElectronicsUiVisuals.cs.meta" }
