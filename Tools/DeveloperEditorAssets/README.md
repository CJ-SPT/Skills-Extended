# Shared developer editor UI assets

The shipped `skills_editor_toolkit.bundle` contains the author's Campaigns UI
Toolkit theme, font, panel settings, control templates, and runtime UI shaders.
Its bundle name and serialized-file identities are independent, so Campaigns and
Skills Extended can load their UI simultaneously. No Campaigns DLL is required.
The original asset paths inside the bundle are retained for stable template lookup.

The reference USS/UXML sources under `Toolkit` document the ported controls. The
workspace and tool-specific inspectors are assembled by `DeveloperEditorView`.
They use a central transparent scene view with fixed logical browser/inspector
widths and scaling based on both screen dimensions.

Re-port from a validated Campaigns asset build using the existing UnityPy 1.25.3
environment used by the Signals case tooling:

```powershell
$env:PYTHONPATH = (Resolve-Path artifacts/signals-python).Path
python Tools/DeveloperEditorAssets/Build-Toolkit.py --source 'path/to/wtt_campaigns_editor_toolkit.bundle'
python Tools/DeveloperEditorAssets/Build-Toolkit.py --verify
```

This operation does not start a game, server, or Unity editor. It writes the
standalone client resource and a source/hash receipt. Bundle verification checks
the serialized identities, UXML, stylesheet, font, shaders, and absence of mod
assembly or external bundle dependencies; it does not establish live rendering.
