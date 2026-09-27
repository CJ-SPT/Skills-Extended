# SPT mod packages

Building the server or the solution automatically creates the complete core mod ZIP
under `artifacts/packages/<Configuration>/SkillsExtended-<Version>-<Configuration>.zip`.
The server builds the API, skills client, and prepatcher first so packaging does not
depend on old build outputs or files from an installed mod.

To produce a release package without copying files into your game installation:

```powershell
dotnet build Server/SkillsExtended.Server/SkillsExtended.Server.csproj -c Release -p:DeploySkillsExtended=false
```

Extract the ZIP directly into the SPT installation folder. Its layout is:

```text
BepInEx/
  patchers/SkillsExtended.Client.Prepatch.dll
  plugins/SkillsExtended/
    SkillsExtended.Client.API.dll
    SkillsExtended.Client.Skills.dll
    SkillsExtended.Common.dll
    Images/
    bundles/
SPT_Runtime/user/mods/SkillsExtended/
  SkillsExtended.Server.dll
  SkillsExtended.Common.dll
  bundles.json
  Resources/
  wwwroot/
  bundles/
```

The Fika project separately produces `SkillsExtendedFika-<Version>-<Configuration>.zip`
containing `BepInEx/plugins/SkillsExtended/SkillsExtendedFika.dll`. Install it alongside
the core package when using Fika. Building the full solution requires a compatible
Fika reference, either installed or supplied with `-p:FikaAssemblyPath=...`.

Packaging runs for Debug, Release, and BETA builds. Archives contain only the mod's
runtime files, without debug symbols, game assemblies, or third-party dependencies.
Each archive is assembled in a fresh staging directory to exclude removed files.
The version and configuration label come from the project build settings. The
solution's existing BETA configuration maps the server and Fika projects to Debug,
so those solution builds produce Debug-labeled archives while preserving the
solution's client configuration mappings. Use Release for distribution.

Use `-p:PackageSkillsExtended=false` to disable packaging (and the server's extra
client build dependencies). Deployment remains independently controlled by
`DeploySkillsExtended`. Override the archive directory with
`-p:SkillsExtendedPackageOutputDir=...` if needed.
