"""Stage the licensed olive Fab case in the local CJ-SDK authoring project."""

import argparse
import hashlib
import json
from pathlib import Path
import tarfile


FILES = {
    "meshes/ammo_box.FBX",
    "materials/ammo_box_01.mat",
    "prefabs/ammo_box_01.prefab",
    "textures/ammo_box_01_albedo.tga",
    "textures/ammo_box_normal.tga",
    "textures/ammo_box_MetallicOcclusionSmoothness.tga",
}
LISTING = "https://www.fab.com/listings/76f33491-c282-487d-9b70-407ea18eb425"


def stage(package, sdk, receipt):
    destination = sdk / "Assets/Mods/SkillsExtended.Assets/SignalsCase/Source"
    payload = {}
    with tarfile.open(package, "r:gz") as archive:
        for member in archive.getmembers():
            if not member.name.endswith("/pathname"):
                continue
            name = archive.extractfile(member).read().decode("utf-8")
            prefix = "Assets/ammo_box/"
            if not name.startswith(prefix) or name[len(prefix):] not in FILES:
                continue
            relative = name[len(prefix):]
            stem = member.name.rsplit("/", 1)[0]
            for suffix, source in (("", "asset"), (".meta", "asset.meta")):
                payload[relative + suffix] = archive.extractfile(stem + "/" + source).read()
    if set(payload) != FILES | {name + ".meta" for name in FILES}:
        raise ValueError("The package does not contain the expected complete olive case inputs.")
    # Source assets may be shared privately with collaborators, not as loose public assets.
    # Keep these local authoring inputs and generated prefabs out of the SDK repository.
    exclude = sdk / ".git/info/exclude"
    if not exclude.parent.is_dir():
        raise ValueError("Expected a normal local CJ-SDK Git checkout.")
    rule = "/Assets/Mods/SkillsExtended.Assets/SignalsCase"
    previous = exclude.read_text() if exclude.exists() else ""
    if rule not in previous.splitlines():
        exclude.write_text(previous.rstrip() + "\n" + rule + "\n" + rule + ".meta\n")
    for relative, data in sorted(payload.items()):
        path = destination / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    result = {
        "listing": LISTING,
        "author": "Alpen wolf",
        "license": "Fab Standard License",
        "licenseUrl": "https://www.fab.com/eula",
        "variant": "ammo_box_01 (olive)",
        "packageSha256": hashlib.sha256(package.read_bytes()).hexdigest(),
        "files": {name: hashlib.sha256(data).hexdigest() for name, data in sorted(payload.items())},
    }
    receipt.parent.mkdir(parents=True, exist_ok=True)
    receipt.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"destination": str(destination), "files": len(payload), "receipt": str(receipt)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path)
    parser.add_argument("sdk", type=Path)
    parser.add_argument("receipt", type=Path)
    args = parser.parse_args()
    stage(args.package, args.sdk, args.receipt)
