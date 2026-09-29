param([string]$TarkovDir = 'F:\SPT 4.1.x')

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
dotnet build $PSScriptRoot -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }

# Model SPT's declarative pre-load enum extension on an isolated test assembly.
# Never modify installed game/server assemblies or the NuGet/build copies.
$fixture = Join-Path $repo ('artifacts\tests\server-enums-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'bin\Release\net10.0\*') -Destination $fixture -Recurse
Add-Type -Path (Join-Path $TarkovDir 'BepInEx\core\Mono.Cecil.dll')
$corePath = Join-Path $fixture 'SPTarkov.Server.Core.dll'
$reader = [Mono.Cecil.ReaderParameters]::new()
$reader.InMemory = $true
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($corePath, $reader)
try
{
    $definitions = Get-Content -Raw (Join-Path $repo 'Server\SkillsExtended.Server\Prepatch\EnumExtensions.json') | ConvertFrom-Json
    foreach ($definition in $definitions)
    {
        $enum = $core.MainModule.GetType($definition.enumType)
        if ($null -eq $enum -or !$enum.IsEnum) { throw 'Server enum type missing.' }
        if ($enum.Fields | Where-Object { $_.Name -eq $definition.constantName -or ($_.HasConstant -and [long]$_.Constant -eq $definition.constantValue) })
        {
            throw 'Server enum extension conflicts with an existing name/value.'
        }
        $field = [Mono.Cecil.FieldDefinition]::new($definition.constantName,
            [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static -bor [Mono.Cecil.FieldAttributes]::Literal -bor [Mono.Cecil.FieldAttributes]::HasDefault, $enum)
        $underlyingType = ($enum.Fields | Where-Object Name -eq 'value__').FieldType.FullName
        $field.Constant = [Convert]::ChangeType($definition.constantValue, [Type]::GetType($underlyingType))
        $enum.Fields.Add($field)
    }
    $core.Write($corePath)
}
finally { $core.Dispose() }

$env:SKILLS_EFT_ROOT = $TarkovDir
dotnet (Join-Path $fixture 'SkillsExtended.ElectronicsTests.dll')
if ($LASTEXITCODE -ne 0) { throw 'Serialization checks failed.' }
