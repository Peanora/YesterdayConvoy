"""Package the approved map placements with their matching native source meshes.

Usage: python tools/art/export_formation_variants.py <redesign preview directory>
The preview renderer's --export-placement mode supplies each placement.json.
"""

import argparse
import hashlib
import json
import shutil
from pathlib import Path


STAGES = {"05": "SyracuseStreet", "09": "BlueIceValley", "12": "GildedHall"}
GUARDS = {
    "05": [
        ([(15, 2), (15, 1), (16, 2), (14, 2)], [(17, 2), (17, 1), (18, 2), (18, 1), (18, 0)], (16, 1), (14, 1)),
        ([(14, 5), (15, 5), (15, 6), (16, 5)], [(17, 5), (18, 5), (17, 6), (18, 6), (19, 6)], (15, 7), (17, 4)),
    ],
    "09": [
        ([(15, 2), (15, 3), (16, 2), (15, 1)], [(17, 2), (17, 1), (18, 2), (18, 1), (18, 3)], (16, 1), (16, 3)),
        ([(17, 3), (18, 3), (17, 4), (18, 4)], [(19, 3), (19, 4), (19, 5), (20, 4), (20, 5)], (18, 5), (20, 3)),
    ],
    "12": [
        ([(15, 3), (15, 2), (16, 3), (14, 3)], [(17, 3), (17, 2), (18, 2), (18, 3), (20, 2)], (16, 2), (20, 3)),
        ([(15, 4), (15, 5), (16, 4), (14, 4)], [(17, 4), (17, 5), (18, 5), (18, 4), (20, 5)], (16, 5), (20, 4)),
    ],
}


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def point(coords):
    return dict(zip("xy", coords))


def scene_mesh(name, source, asphalt):
    uv = source["uv"]
    if asphalt:
        width, depth, _ = source["size"]
        uv = [
            (.739 + (x / width + .5) * .235, .166 + (y / depth + .5) * .047) if z >= -.019 else original
            for (x, y, z), original in zip(source["vertices"], uv, strict=True)
        ]
    return {
        "name": name + ("_asphalt" if asphalt else ""),
        "sourceName": name,
        "meshId": source["meshId"],
        "rendererId": source["rendererId"],
        "vertices": [round(value, 7) for x, y, z in source["vertices"] for value in (x, z, y)],
        "uv": [round(value, 7) for pair in uv for value in pair],
        "surfaces": [
            {"material": surface["material"], "indices": [i for a, b, c in surface["faces"] for i in (a, c, b)]}
            for surface in source["surfaces"]
        ],
    }


def export(stage, preview, target):
    source_dir = preview.parent / stage
    source = read(source_dir / "source.json")
    approved = read(preview / stage / "placement.json")
    parts = {}
    for placement in approved["placements"]:
        native = placement["part"]
        asphalt = placement["finish"] == "city-asphalt"
        mesh_name = native + ("_asphalt" if asphalt else "")
        if mesh_name not in parts:
            parts[mesh_name] = scene_mesh(native, source["parts"][native], asphalt)
        placement["part"] = mesh_name
        placement["angle"] *= -1
        placement["size"] = dict(zip("xzy", placement["size"]))
        if placement["tile"] and placement["top"] == -.035:
            placement["top"] = 0  # Open ice is flush with the movement plane and ground effects.
    keys = {surface["material"] for part in parts.values() for surface in part["surfaces"]}
    materials = [dict(name=name, **source["materials"][name]) for name in sorted(keys)]
    textures = []
    textures_dir = target / "Textures"
    textures_dir.mkdir(parents=True, exist_ok=True)
    for atlas in sorted({material["atlas"] for material in materials}):
        file = source_dir / atlas
        record = source["textures"][file.stem]
        if hashlib.sha256(file.read_bytes()).hexdigest() != record["sha256"]:
            raise ValueError(f"Atlas digest mismatch: {file}")
        shutil.copyfile(file, textures_dir / atlas)
        textures.append(dict(atlas=atlas, **record))
    payload = dict(
        stage=stage,
        family=source["family"],
        bundles=source["bundles"],
        textures=textures,
        rows=approved["rows"],
        guards=[dict(front=list(map(point, front)), rear=list(map(point, rear)),
                     reconstruction=point(reconstruction), chess=point(chess))
                for front, rear, reconstruction, chess in GUARDS[stage]],
        placements=approved["placements"],
        finishes=[dict(name=name, tint=dict(zip("rgba", tint)), mix=amount)
                  for name, (tint, amount, _) in approved["finishes"].items()],
        materials=materials,
        parts=list(parts.values()),
    )
    (target / "source.json").write_text(json.dumps(payload, separators=(",", ":")) + "\n", encoding="utf-8")
    print(stage, source["family"], len(parts), "mesh variants,", len(textures), "original atlases")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("preview", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2] / "Assets/Art/Environment/FormationVariants"
    for stage, name in STAGES.items():
        export(stage, args.preview, root / name)
