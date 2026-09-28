"""Repair/validate the legacy PDA shader link without rebuilding its artwork.

Requires UnityPy 1.25.3. The game shader bundle is read only, never redistributed.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path

import UnityPy

SHADER = "p0/Reflective/Bumped Specular SMap"
OLD_CAB = "CAB-1dc8d26be8722a766953ce9d8a444e8c"
OLD_ID = -9098473984068178372


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--shaders", type=Path, required=True)
    parser.add_argument("--bundle", type=Path, default=Path(__file__).resolve().parents[2] / "Server/SkillsExtended.Server/bundles/pda.bundle")
    parser.add_argument("--write", action="store_true", help="Repair the bundle after validating preservation; otherwise check only")
    args = parser.parse_args()
    shaders = UnityPy.load(str(args.shaders))
    matches = []
    for obj in shaders.objects:
        if obj.type.name == "Shader":
            tree = obj.read_typetree()
            if tree["m_ParsedForm"]["m_Name"] == SHADER:
                matches.append((obj, tree))
    require(len(matches) == 1, "Expected one compatible game shader")
    shader, shader_tree = matches[0]
    cab = shader.assets_file.name
    external_path = f"archive:/{cab}/{cab}"

    source = args.bundle.read_bytes()
    env = UnityPy.load(source)
    objects = {obj.path_id: obj for obj in env.objects}
    materials = [obj for obj in objects.values() if obj.type.name == "Material"]
    bundles = [obj for obj in objects.values() if obj.type.name == "AssetBundle"]
    require(len(materials) == len(bundles) == 1, "Unexpected PDA bundle structure")
    material, bundle = materials[0], bundles[0]
    before_material, before_bundle = material.read_typetree(), bundle.read_typetree()
    data = material.assets_file
    require(len(data.externals) == 1, "Unexpected external dependencies")
    old_pointer = before_material["m_Shader"]
    new_pointer = {"m_FileID": 1, "m_PathID": shader.path_id}
    properties = before_material["m_SavedProperties"]
    property_names = {name for pairs in properties.values() for name, value in pairs}
    required_names = {p["m_Name"] for p in shader_tree["m_ParsedForm"]["m_PropInfo"]["m_Props"]}
    require(required_names <= property_names, "Target shader does not match the authored material properties")
    main_texture = dict(properties["m_TexEnvs"])["_MainTex"]["m_Texture"]
    require(main_texture["m_FileID"] == 0 and objects[main_texture["m_PathID"]].type.name == "Texture2D", "Missing PDA texture")
    require(before_bundle["m_Dependencies"] == ["shaders"], "Unexpected shader dependency")
    require(old_pointer in before_bundle["m_PreloadTable"], "Missing shader preload")

    if old_pointer == new_pointer and data.externals[0].path == external_path:
        print(f"PASS: PDA shader resolves to {SHADER} in {cab}; texture and preload are present.")
        return

    require(old_pointer == {"m_FileID": 1, "m_PathID": OLD_ID}, "Unrecognized original shader pointer")
    require(data.externals[0].path == f"archive:/{OLD_CAB}/{OLD_CAB}", "Unrecognized original shader bundle")
    require(args.write, "PDA still references the obsolete shader bundle; use --write to repair it")
    after_material, after_bundle = copy.deepcopy(before_material), copy.deepcopy(before_bundle)
    after_material["m_Shader"] = new_pointer
    after_bundle["m_PreloadTable"] = [new_pointer if p == old_pointer else p for p in after_bundle["m_PreloadTable"]]
    material.save_typetree(after_material)
    bundle.save_typetree(after_bundle)
    data.externals[0].path = external_path
    data.mark_changed()
    output = env.file.save(packer="lz4")

    # Reload serialized output before writing. All unrelated objects, including
    # mesh, texture, renderer, colliders and prefab components, must be identical.
    rebuilt = UnityPy.load(output)
    actual = {obj.path_id: obj for obj in rebuilt.objects}
    require(actual.keys() == objects.keys(), "Object identities changed")
    for identity, original in objects.items():
        if identity == material.path_id:
            require(actual[identity].read_typetree() == after_material, "Material changed unexpectedly")
        elif identity == bundle.path_id:
            require(actual[identity].read_typetree() == after_bundle, "Prefab/preload metadata changed unexpectedly")
        else:
            require(actual[identity].get_raw_data() == original.get_raw_data(), f"Object {identity} changed")
    require(actual[material.path_id].assets_file.externals[0].path == external_path, "External reference was not saved")
    staged = args.bundle.with_suffix(".bundle.tmp")
    staged.write_bytes(output)
    staged.replace(args.bundle)
    print(json.dumps({
        "shader": SHADER, "shader_cab": cab, "shader_path_id": shader.path_id,
        "before_sha256": hashlib.sha256(source).hexdigest(),
        "after_sha256": hashlib.sha256(output).hexdigest(),
        "preserved_objects": len(objects) - 2,
        "material_properties_preserved": True,
    }, indent=2))


if __name__ == "__main__":
    main()
