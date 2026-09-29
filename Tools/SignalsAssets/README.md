# Signals assets

`SignalsSkillIcon.svg` is the authored vector source for the receiver icon.
The checked-in 128px PNG uses the same antenna/radio motif.

`Extract-Case.py` selects the native toolbox and its complete dependency closure
from a locally exported native container library. It writes a separate bundle
with a unique CAB identity, remaps streamed resources, and verifies every object
reference. The native library was exported from this installed EFT build's assets;
it is only an authoring input. Campaigns is not a runtime dependency.

With UnityPy 1.25.3 installed:

```powershell
python Tools/SignalsAssets/Extract-Case.py <native-containers.bundle> Client/SkillsExtended.Client.Skills/Resources/bundles/signal_case.bundle
```

No source assets are modified and no game/editor process is launched. Only the
selected case is packaged, rather than the entire source library.
