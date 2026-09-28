# SPT mod packages

Building the solution automatically creates the complete core mod ZIP
under `artifacts/packages/<Configuration>/SkillsExtended-<Version>-<Configuration>.zip`.
The solution's `SkillsExtended.Package` project runs last, after the server, API,
skills client, prepatcher, and Fika integration have built successfully. This works
with Build Solution in Visual Studio and with `dotnet build`; individual mod project
builds do not create archives.

To produce a release package without copying files into your game installation:

```powershell
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false
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

The solution also produces `SkillsExtendedFika-<Version>-<Configuration>.zip`
containing `BepInEx/plugins/SkillsExtended/SkillsExtendedFika.dll`, the matching
Electronics UI bundle, asset notices, and Electronics instructions. Install it alongside
the core package when using Fika. Building the full solution requires a compatible
Fika reference, either installed or supplied with `-p:FikaAssemblyPath=...`.

Packaging runs for Debug, Release, and BETA builds. Archives contain only the mod's
runtime files, without debug symbols, game assemblies, or third-party dependencies.
Each archive is assembled in a fresh staging directory to exclude removed files.
The version comes from each mod project's build settings; the archive configuration
label and output folder follow the solution configuration. BETA packages preserve
the solution's existing per-project Debug/BETA mappings. Use Release for distribution.

Use `-p:PackageSkillsExtended=false` to disable packaging (and the server's extra
client build dependencies). Deployment remains independently controlled by
`DeploySkillsExtended`. Override the archive directory with
`-p:SkillsExtendedPackageOutputDir=...` if needed.

The current Electronics bundle is Corey's local EVE texture/audio build. See
[ELECTRONICS.md](ELECTRONICS.md) and [the asset builder](Tools/ElectronicsAssets/README.md)
for provenance, reproduction, configuration and live acceptance. Both archives
include the same validated bundle and notices; the Fika archive still requires
the complete core package. No installation or application restart is performed
by the offline build command above.
