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

All Skills Extended web pages require an SPT administrator account. Open
`/skills-extended` on your SPT server and use the server's existing sign-in.

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
SPT_Runtime/user/patchers/com.cj.skillsextended/
  EnumExtensions.json
```

The server enum declaration is required: it adds `Hacking = 200` before SPT loads
the mod. Install the complete core package and restart the server before starting
the client so both sides serialize the skill as `Hacking`. Existing skill progress
stored under numeric ID 200 remains readable.

The solution also produces `SkillsExtendedFika-<Version>-<Configuration>.zip`
containing `BepInEx/plugins/SkillsExtended/SkillsExtendedFika.dll`, the matching
Electronics UI bundle. Install it alongside
the core package when using Fika. Building the full solution requires a compatible
Fika reference, either installed or supplied with `-p:FikaAssemblyPath=...`.

Packaging runs for Debug, Release, and BETA builds. Archives contain only the mod's
runtime files, without debug symbols, game assemblies, or third-party dependencies.
Notices, provenance receipts, documentation folders and document files are excluded
from both distributions, including `Resources/modpage.md`. Their source copies
remain in the repository. Runtime configuration, locales, release-note data and
web editor assets remain packaged.
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
include the same validated bundle; the Fika archive still requires
the complete core package. No installation or application restart is performed
by the offline build command above.

Lock Picking 2.0 art is embedded in `SkillsExtended.Client.Skills.dll`; its
provenance notice remains in the repository's `Resources/Notices/LockPicking-Art.txt`. The Mini Pack source
archive, FBX files, and standalone texture atlases are not release-package inputs.
See [LOCKPICKING.md](LOCKPICKING.md) for controls and live acceptance.
