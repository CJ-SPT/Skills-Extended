"""Attach native EFT container/ballistic components to the compiled olive case.

Unity authoring needs no EFT script stubs. The installed game's exact serialized
component layouts and sounds are copied from a dependency-complete donor bundle.
Requires UnityPy 1.25.3. No game or editor process is used by this step.
"""

import argparse
import copy
import hashlib
import json
from pathlib import Path

import UnityPy


class Stream:
    flags = 0

    def __init__(self, data):
        self.data = data

    def save(self):
        return self.data


def pointers(node):
    if isinstance(node, dict):
        if "m_FileID" in node and "m_PathID" in node:
            yield node
        else:
            for value in node.values():
                yield from pointers(value)
    elif isinstance(node, (list, tuple)):
        for value in node:
            yield from pointers(value)


def load(path):
    env = UnityPy.load(str(path))
    bundle = next(iter(env.files.values()))
    assets = [value for value in bundle.files.values() if hasattr(value, "objects")]
    if len(assets) != 1:
        raise ValueError("Expected one serialized asset file per case bundle.")
    return bundle, assets[0]


def build(visual, donor, output):
    bundle, asset = load(visual)
    native_bundle, native = load(donor)
    if asset.unity_version != "2022.3.43f1" or native.unity_version != asset.unity_version:
        raise ValueError("Both bundles must target Unity 2022.3.43f1.")
    if any(ref.path != "Resources/unity_builtin_extra" for ref in asset.externals) or native.externals:
        raise ValueError("The case must not depend on another bundle.")
    trees = {pid: obj.read_typetree() for pid, obj in asset.objects.items()}
    native_trees = {pid: obj.read_typetree() for pid, obj in native.objects.items()}
    objects = {data["m_Name"]: pid for pid, data in trees.items() if asset.objects[pid].type.name == "GameObject"}
    cap_id = objects["cap"]
    cap_transform = next(data for pid, data in trees.items()
                         if asset.objects[pid].type.name == "Transform" and data["m_GameObject"]["m_PathID"] == cap_id)
    pivot = cap_transform["m_LocalPosition"]
    scripts = {data["m_ClassName"]: pid for pid, data in native_trees.items()
               if native.objects[pid].type.name == "MonoScript"}
    container_id = next(pid for pid, data in native_trees.items()
                        if native.objects[pid].type.name == "MonoBehaviour"
                        and data["m_Script"]["m_PathID"] == scripts["LootableContainer"])
    ballistic_id = next(pid for pid, data in native_trees.items()
                        if native.objects[pid].type.name == "MonoBehaviour"
                        and data["m_Script"]["m_PathID"] == scripts["BallisticCollider"])
    imported = {}
    types = {}
    next_id = max([0] + list(asset.objects)) + 1
    audio = bytearray()
    audio_name = "CAB-skills-signal-case-native-audio.resS"

    def import_native(pid, tree_override=None):
        nonlocal next_id
        if tree_override is None and pid in imported:
            return imported[pid]
        source = native.objects[pid]
        obj = copy.copy(source)
        obj.path_id = next_id
        next_id += 1
        if tree_override is None:
            imported[pid] = obj.path_id
        tree = copy.deepcopy(tree_override if tree_override is not None else native_trees[pid])
        for ref in pointers(tree):
            if ref["m_FileID"]:
                raise ValueError("Unexpected external native reference.")
            target = ref["m_PathID"]
            # m_GameObject overrides already point at the generated prefab, not the donor.
            if ref is tree.get("m_GameObject"):
                continue
            if target:
                if native.objects[target].type.name not in ("MonoScript", "AudioClip"):
                    raise ValueError("Unexpected native component dependency.")
                ref["m_PathID"] = import_native(target)
        if source.type.name == "AudioClip":
            resource = tree["m_Resource"]
            stream = next(value for key, value in native_bundle.files.items()
                          if key.endswith(Path(resource["m_Source"]).name))
            stream.Position = resource["m_Offset"]
            data = stream.read_bytes(resource["m_Size"])
            if len(data) != resource["m_Size"]:
                raise ValueError("Native sound is not dependency-complete.")
            resource["m_Source"] = "archive:/" + asset.name + "/" + audio_name
            resource["m_Offset"] = len(audio)
            audio.extend(data)
        if source.type_id not in types:
            serialized = copy.copy(source.serialized_type)
            if serialized.script_type_index >= 0:
                identifier = copy.copy(native.script_types[serialized.script_type_index])
                identifier.local_identifier_in_file = import_native(identifier.local_identifier_in_file)
                serialized.script_type_index = len(asset.script_types)
                asset.script_types.append(identifier)
            types[source.type_id] = len(asset.types)
            asset.types.append(serialized)
        obj.assets_file = asset
        obj.type_id = types[source.type_id]
        obj.serialized_type = asset.types[obj.type_id]
        obj.save_typetree(tree)
        asset.objects[obj.path_id] = obj
        trees[obj.path_id] = tree
        return obj.path_id

    def attach(go_id, template_id, values):
        tree = copy.deepcopy(native_trees[template_id])
        tree.update(values)
        tree["m_GameObject"] = {"m_FileID": 0, "m_PathID": go_id}
        new_id = import_native(template_id, tree)
        trees[go_id]["m_Component"].append({"component": {"m_FileID": 0, "m_PathID": new_id}})
        asset.objects[go_id].save_typetree(trees[go_id])
        return new_id

    # Lid hinge is at rear (-Z). A negative X rotation raises the front (+Z).
    # Interaction positions are relative to the closed lid pivot, in Unity metres.
    metadata = json.loads(visual.with_name("visual-validation.json").read_text())
    front = metadata["closedBounds"]["max"]["z"]
    center_y = metadata["closedBounds"]["center"]["y"]
    container = attach(cap_id, container_id, {
        "Id": "skills-signal-cache-olive",
        "_currentAngle": 0.0,
        "OpenAngle": -105.0,
        "CloseAngle": 0.0,
        "DoorAxis": 0,
        "DoorForward": 4,
        "ClosedPosition": copy.deepcopy(pivot),
        "OpenPosition": copy.deepcopy(pivot),
        "interactPosition1": {"x": -pivot["x"], "y": -pivot["y"], "z": front + .65 - pivot["z"]},
        "interactPosition2": {"x": -pivot["x"], "y": -pivot["y"], "z": front + .65 - pivot["z"]},
        "viewTarget1": {"x": -pivot["x"], "y": center_y - pivot["y"], "z": front - pivot["z"]},
    })
    ballistic_targets = [pid for name, pid in objects.items() if name.endswith("_BALLISTIC")]
    if len(ballistic_targets) != 4:
        raise ValueError("Expected four case/lid shell and metal ballistic colliders.")
    for target in ballistic_targets:
        metal = "metallic" in trees[target]["m_Name"].lower()
        attach(target, ballistic_id, {"_typeOfMaterial": 16 if metal else 20})
    # Preserve the generated prefab graph and include all grafted component dependencies.
    header = next(obj for obj in asset.objects.values() if obj.type.name == "AssetBundle")
    tree = header.read_typetree()
    if len(tree["m_Container"]) != 1:
        raise ValueError("Expected exactly one case prefab.")
    entry = tree["m_Container"][0][1]
    tree["m_Name"] = tree["m_AssetBundleName"] = "skills-signal-case-olive"
    builtins = [ref for ref in tree["m_PreloadTable"] if ref["m_FileID"]]
    tree["m_PreloadTable"] = [{"m_FileID": 0, "m_PathID": pid} for pid in asset.objects if pid != header.path_id] + builtins
    entry["preloadIndex"] = 0
    entry["preloadSize"] = len(tree["m_PreloadTable"])
    tree["m_Container"] = [("signal-case.prefab", entry)]
    header.save_typetree(tree)
    bundle.files[audio_name] = Stream(bytes(audio))
    raw = bundle.save(packer="lz4")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(raw)
    report = validate(output)
    report.update({"sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw),
                   "donorSha256": hashlib.sha256(donor.read_bytes()).hexdigest(),
                   "visualSha256": hashlib.sha256(visual.read_bytes()).hexdigest(),
                   "containerPathId": container, "audioBytes": len(audio)})
    output.with_suffix(".validation.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))


def validate(path):
    bundle, asset = load(path)
    assert all(ref.path == "Resources/unity_builtin_extra" for ref in asset.externals)
    trees = {pid: obj.read_typetree() for pid, obj in asset.objects.items()}
    for tree in trees.values():
        for ref in pointers(tree):
            if ref["m_FileID"]:
                assert 0 < ref["m_FileID"] <= len(asset.externals)
                assert ref["m_PathID"] > 0
            else:
                assert not ref["m_PathID"] or ref["m_PathID"] in asset.objects
    for serialized in asset.types:
        if serialized.script_type_index >= 0:
            script = asset.script_types[serialized.script_type_index]
            assert script.local_serialized_file_index == 0
            assert script.local_identifier_in_file in asset.objects
    scripts = [tree for pid, tree in trees.items() if asset.objects[pid].type.name == "MonoScript"]
    assert {tree["m_ClassName"] for tree in scripts} == {"LootableContainer", "BallisticCollider"}
    assert all(tree["m_AssemblyName"] == "Assembly-CSharp" for tree in scripts)
    containers = [tree for tree in trees.values() if tree.get("Template") == "5909d50c86f774659e6aaebe"]
    assert len(containers) == 1
    assert containers[0]["OpenAngle"] == -105 and containers[0]["DoorAxis"] == 0
    assert containers[0]["ClosedPosition"] == containers[0]["OpenPosition"]
    cap_id = containers[0]["m_GameObject"]["m_PathID"]
    assert trees[cap_id]["m_Name"] == "cap" and trees[cap_id]["m_Layer"] == 22
    cap_boxes = [tree for pid, tree in trees.items() if asset.objects[pid].type.name == "BoxCollider"
                 and tree["m_GameObject"]["m_PathID"] == cap_id]
    assert len(cap_boxes) == 1 and not cap_boxes[0]["m_IsTrigger"]
    assert all(value > 0 for value in cap_boxes[0]["m_Size"].values())
    behaviors = [tree for pid, tree in trees.items() if asset.objects[pid].type.name == "MonoBehaviour"]
    assert len(behaviors) == 5
    for behavior in behaviors:
        go = trees[behavior["m_GameObject"]["m_PathID"]]
        if "Template" not in behavior:
            assert go["m_Layer"] == 12 and go["m_Name"].endswith("_BALLISTIC")
            assert behavior["_typeOfMaterial"] == (16 if "metallic" in go["m_Name"].lower() else 20)
    sounds = [tree for pid, tree in trees.items() if asset.objects[pid].type.name == "AudioClip"]
    assert len(sounds) == 2
    for sound in sounds:
        resource = sound["m_Resource"]
        assert resource["m_Source"].startswith("archive:/" + asset.name + "/")
        stream = next(value for key, value in bundle.files.items() if key.endswith(Path(resource["m_Source"]).name))
        assert resource["m_Size"] > 0
        stream.Position = resource["m_Offset"]
        assert len(stream.read_bytes(resource["m_Size"])) == resource["m_Size"]
    materials = [tree for pid, tree in trees.items() if asset.objects[pid].type.name == "Material"]
    assert len(materials) == 1
    assert "01" in materials[0]["m_Name"] or "olive" in materials[0]["m_Name"].lower()
    names = [tree.get("m_Name", "") for tree in trees.values()]
    assert not any("toolbox" in name.lower() for name in names)
    assert not any(name in ("ammo_box_02_albedo", "ammo_box_03_albedo") for name in names)
    header = next(tree for pid, tree in trees.items() if asset.objects[pid].type.name == "AssetBundle")
    assert [name for name, _ in header["m_Container"]] == ["signal-case.prefab"]
    return {"objects": len(asset.objects), "containers": len(containers), "sounds": len(sounds), "variant": "olive"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("visual", type=Path)
    parser.add_argument("donor", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    build(args.visual, args.donor, args.output)
