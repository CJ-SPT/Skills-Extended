"""Export original skill sprites from the local EFT asset file without modifying it.
Requires UnityPy 1.25.3. Run with the locally installed UnityPy package on PYTHONPATH.
"""
import argparse
import hashlib
import json
from pathlib import Path
import UnityPy

parser = argparse.ArgumentParser()
parser.add_argument("source", type=Path)
parser.add_argument("destination", type=Path)
args = parser.parse_args()
args.destination.mkdir(parents=True, exist_ok=True)
mapping = json.loads(Path(__file__).with_name("NativeIcons.json").read_text())
by_sprite = {sprite: key for key, sprite in mapping.items()}
env = UnityPy.load(str(args.source))
exported = {}
for obj in env.objects:
    if obj.type.name != "Sprite":
        continue
    data = obj.read()
    if data.m_Name not in by_sprite:
        continue
    key = by_sprite[data.m_Name]
    image = data.image
    if image.width == 0 or image.height == 0:
        raise ValueError("Empty native sprite: " + data.m_Name)
    path = args.destination / ("Skill_" + key + ".png")
    image.save(path)
    exported[key] = {"sprite": data.m_Name, "path_id": obj.path_id,
        "size": list(image.size), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
    print(key, data.m_Name, image.size, flush=True)
missing = set(mapping) - set(exported)
if missing:
    raise ValueError("Missing native sprites: " + ", ".join(sorted(missing)))
receipt = {"source": str(args.source.resolve()), "icons": exported}
(args.destination / "native-icons-extraction.json").write_text(json.dumps(receipt, indent=2))
