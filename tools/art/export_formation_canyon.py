import argparse
import hashlib
import json
import sys
from pathlib import Path

parser = argparse.ArgumentParser(description="Export the selected pieces of the approved Guard Protocol #02 layout.")
parser.add_argument("--client", type=Path, required=True, help="Local Arknights_Data directory")
parser.add_argument("--runtime", type=Path, required=True, help="Local UnityPy and AK LZ4 adapter")
parser.add_argument("--output", type=Path, default=Path("Assets/Art/Environment/FormationCanyon"))
args = parser.parse_args()
sys.path[:0] = [str(args.runtime / "tools-python"), str(args.runtime / "external-tools")]
import aklz4
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

prefab = "battle/prefabs/levelprefabs/activities/act3autochess/level_act3autochess_m02/level_act3autochess_m02_sub_01/level_act3autochess_m02_sub_01.ab"
atlas = "arts/maps/map_autochesssand/res.ab"
patch = args.client / "PersistentData/Bundles"
stream = args.client / "StreamingAssets/AB/Windows"
parts_to_export = {
    "S_Ground_tiles_wilds_133322", "S_Ground_tiles_wilds_133301", "S_Ground_tiles_wilds_133316",
    "S_Ground_hide01_850", "S_Ground_hide01_853", "S_Ground_hide01_1479",
    "S_Ground_out_14_2754", "S_Ground_out_14_2763", "S_Ground_out_14_2782", "S_Ground_out_14_2756",
    "S_Outworld_rock_678", "S_Outworld_rock_680", "S_Outworld_rock_683", "S_Outworld_rock_686",
    "S_Outworld_stone_542", "S_Outworld_out_2771", "S_Outworld_out_2778",
    "S_outworld_highground218", "S_outworld_water_44", "S_Outworld_plane_188",
    "S_outworld_grass64", "S_outworld_grass_132",
}
atlas_to_export = {"TX_AutochessSand_A", "TX_AutochessSand_Grass_A", "TX_AutochessSand_sand_A",
                   "TX_AutochessSand_shuidi_A", "TX_AutochessSand_shuidi_N"}
patches = {"S_outworld_grass64": ("Reed patch", (-2.1, -4.0)),
           "S_outworld_grass_132": ("Low grass", (1.0, -1.4))}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def asset_ref(obj):
    return {"file": obj.assets_file.name, "pathId": str(obj.path_id)}


def flatten(rows, width, scale=1):
    return [round(float(value) * scale, 7) for row in rows for value in row[:width]]


def crop_patch(vertices, triangles, anchor):
    root = min(vertices, key=lambda vertex: (vertex[0] - anchor[0]) ** 2
               + (vertex[2] - anchor[1]) ** 2 + abs(vertex[1]) * .18)
    return [[face for face in group if all(
        (vertices[index][0] - root[0]) ** 2 + (vertices[index][2] - root[2]) ** 2 < 1.30
        for index in face)] for group in triangles]


def export_renderer(obj, renderer, game_object):
    source_filter = next(entry.component.read() for entry in game_object.m_Component if entry.component.type.name == "MeshFilter")
    mesh = source_filter.m_Mesh.deref()
    geometry = MeshHandler(mesh.read())
    geometry.process()
    triangles = geometry.get_triangles()
    if renderer.m_StaticBatchInfo.subMeshCount:
        raise ValueError("Expected unbatched prefab mesh: " + game_object.m_Name)
    vertices = [tuple(float(value) * .01 for value in row[:3]) for row in geometry.m_Vertices]
    name = game_object.m_Name
    if name in patches:
        name, anchor = patches[name]
        triangles = crop_patch(vertices, triangles, anchor)
    indices = sorted({index for group in triangles for triangle in group for index in triangle})
    used = [vertices[index] for index in indices]
    center_x = (min(vertex[0] for vertex in used) + max(vertex[0] for vertex in used)) * .5
    center_z = (min(vertex[2] for vertex in used) + max(vertex[2] for vertex in used)) * .5
    top = max(vertex[1] for vertex in used)
    remap = {old: new for new, old in enumerate(indices)}
    return {
        "name": name, "sourceName": game_object.m_Name, "renderer": asset_ref(obj), "mesh": asset_ref(mesh),
        "vertices": flatten([(x - center_x, y - top, z - center_z) for x, y, z in used], 3),
        "normals": flatten([geometry.m_Normals[index] for index in indices], 3),
        "uv": flatten([geometry.m_UV0[index] for index in indices], 2),
        "surfaces": [{"material": renderer.m_Materials[slot].read().m_Name,
                      "triangles": [remap[index] for face in group for index in face]}
                     for slot, group in enumerate(triangles)],
    }


def export():
    environment = UnityPy.load(str(patch / prefab), str(stream / atlas))
    texture_dir = args.output / "Textures"
    texture_dir.mkdir(parents=True, exist_ok=True)
    parts, textures, seen = [], [], set()
    for obj in environment.objects:
        if obj.type.name == "MeshRenderer":
            renderer = obj.read()
            game_object = renderer.m_GameObject.read()
            if game_object.m_Name in parts_to_export:
                parts.append(export_renderer(obj, renderer, game_object))
                seen.add(game_object.m_Name)
        elif obj.type.name == "Texture2D":
            texture = obj.read()
            if texture.m_Name in atlas_to_export:
                path = texture_dir / (texture.m_Name + ".png")
                texture.image.save(path)
                textures.append({"name": texture.m_Name, "source": asset_ref(obj), "exportSha256": digest(path)})
    if seen != parts_to_export or {texture["name"] for texture in textures} != atlas_to_export:
        raise ValueError("Approved source part or texture missing from the local client")
    sources = [{"file": path, "version": json.loads((root / "hot_update_list.json").read_text(encoding="utf-8-sig"))["versionId"],
                "sha256": digest(root / path)} for root, path in ((patch, prefab), (stream, atlas))]
    catalog = {"source": sources, "parts": sorted(parts, key=lambda part: part["name"]), "textures": textures}
    (args.output / "source.json").write_text(json.dumps(catalog, separators=(",", ":")), encoding="utf-8")
    print(f"Guard Protocol #02: {len(parts)} original parts and {len(textures)} original textures.")


if __name__ == "__main__":
    export()
