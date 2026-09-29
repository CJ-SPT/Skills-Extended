# Offline receiver preview

Run from the repository root:

```powershell
dotnet run --project Tools/SignalsAssets/Preview -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false -- artifacts/signals-ui
python Tools/SignalsAssets/Preview/render.py artifacts/signals-ui
```

The renderer requires Pillow. It uses the CJ-SDK Liberation Sans font when present,
otherwise Windows Arial. The export compiles the production receiver layout and
graphic builders against a small Unity API shim. It exports receiver, pairing,
unlocked, and practice states; the renderer produces 1080p, 1440p, and ultrawide
images plus text-fit measurements in `layout-checks.json`.

These are offline previews, not Unity screenshots. They do not validate native
font rasterization, masking, pointer dispatch, keyboard capture, or live resizing.
Those require Corey's in-game acceptance. No game, server, or editor is started.
