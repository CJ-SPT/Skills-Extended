param([string]$Eve = 'E:\Eve', [string]$Sdk = 'F:\SPT 4.1.x\Development\CJ-SDK')
$ErrorActionPreference = 'Stop'
$destination = Join-Path $Sdk 'Assets\Mods\SkillsExtended.Assets\Inputs'
if (!(Test-Path -LiteralPath $destination)) { throw 'Run Stage-Assets.ps1 first.' }
$mapping = @{
    'cpu'='coremedium'; 'core-standard'='corelow'; 'core-secure'='coremedium'; 'core-hardened'='corehigh'
    'shield'='defsoftfirewall'; 'bug'='defsoftantivirus'; 'wrench'='defsofthoneypothealing'; 'radio'='defsofthoneypotstrength'
    'heart-pulse'='utilselfrepair'; 'scissors'='utilkernalrot'; 'shield-check'='utilpolymorphshield'; 'zap'='utilsecondvector'; 'database'='tiledatacache'
    'node-ring'='tileexplored'; 'defense-ring'='tileiconframe'; 'hidden-node'='tileblocked'; 'actionable-node'='tileunflipped'
    'utility-slot'='utiltilebase'; 'hud-background'='hudbg'; 'hud-icon'='hudicon'; 'hud-bar'='hudbarstripes'; 'hud-bar-mirror'='hudbarstripesmirrored'
    'board-background'='boardbg'; 'coherence-symbol'='coherence'; 'strength-symbol'='strength'; 'hover-node'='tilehover'
    'fx-ring'='healring2'; 'fx-glow'='healring1'; 'fx-infected'='infected1'; 'fx-white'='tile'
    'fx-trace-1'='linebleed/horiz01'; 'fx-trace-2'='linebleed/horiz02'; 'fx-trace-3'='linebleed/horiz03'; 'fx-trace-4'='linebleed/horiz04'
}
$index = @{}
Get-Content -LiteralPath (Join-Path $Eve 'tq\resfileindex.txt') | ForEach-Object {
    $parts = $_.Split(','); if ($parts[0].StartsWith('res:/ui/texture/classes/hacking/')) { $index[$parts[0]] = $parts[1] }
}
$receipt = @()
foreach ($name in $mapping.Keys) {
    $resource = 'res:/ui/texture/classes/hacking/' + $mapping[$name] + '.png'
    $relative = $index[$resource]
    if (!$relative) { throw "Missing EVE index entry: $resource" }
    $source = Join-Path (Join-Path $Eve 'ResFiles') $relative
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination ($name + '.png'))
    $receipt += [pscustomobject]@{ Name=$name; Resource=$resource; Source=$source; SHA256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash }
}
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'eve-source-receipt.json') -Encoding utf8
Write-Output "Imported $($receipt.Count) EVE textures into the local SDK asset inputs. The EVE installation was read only."
