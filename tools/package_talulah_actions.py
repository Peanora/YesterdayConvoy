"""Package approved V13 body frames without changing Talulah's four existing clips.

Run with the project's agreed Python runtime, passing --source to the V13 preview
directory. ExtraClips PNGs go to Art; catalogs and source records go to Data.
Existing .meta files are never rewritten or moved by this tool.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image


PPU = 88.8888889
SOURCE_SIZE = 640
TARGET_SIZE = 512
SOURCE_ROOT = (320, 495)
TARGET_ROOT = (256, 396)
PADDING = 4
MAIN_ACTIONS = {"Idle_A", "Move_A", "Attack_A", "Skill_1_A"}
KEYS = {
    "Die": "death",
    "Attack_B": "extra_attack_b",
    "Idle_B": "extra_idle_b",
    "Move_B": "extra_move_b",
    "Skill_1_B": "extra_skill_1_b",
    "Skill_2_A": "extra_skill_2_a",
    "Skill_2_B": "extra_skill_2_b",
    "Skill_3": "extra_skill_3",
    "Reborn_1_Start": "extra_reborn_1_start",
    "Reborn_1_End": "extra_reborn_1_end",
    "Reborn_2_Start": "extra_reborn_2_start",
    "Reborn_2_End": "extra_reborn_2_end",
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_frames(source: Path, clip: dict) -> list[Image.Image]:
    """V13 batches contain four columns of up to twelve 640px cells."""
    folder = source / clip["folder"]
    frames = []
    for first in range(0, clip["count"], 12):
        count = min(12, clip["count"] - first)
        with Image.open(folder / f"{first}.png") as page:
            page = page.convert("RGBA")
            if page.size != (4 * SOURCE_SIZE, math.ceil(count / 4) * SOURCE_SIZE):
                raise ValueError(f"Unexpected V13 batch size: {folder}/{first}.png")
            for i in range(count):
                left, top = i % 4 * SOURCE_SIZE, i // 4 * SOURCE_SIZE
                frame = page.crop((left, top, left + SOURCE_SIZE, top + SOURCE_SIZE))
                # V4 uses .4px/bone; V13 uses .5px/bone. Keep the old PPU and world scale.
                frames.append(frame.resize((TARGET_SIZE, TARGET_SIZE), Image.Resampling.LANCZOS))
    return frames


def common_bounds(frames: list[Image.Image]) -> tuple[int, int, int, int]:
    bounds = [frame.getchannel("A").getbbox() for frame in frames]
    bounds = [box for box in bounds if box is not None]
    if not bounds:
        raise ValueError("A timed body action must contain visible pixels.")
    left = min(TARGET_ROOT[0], min(b[0] for b in bounds))
    top = min(TARGET_ROOT[1], min(b[1] for b in bounds))
    right = max(TARGET_ROOT[0], max(b[2] for b in bounds))
    bottom = max(TARGET_ROOT[1], max(b[3] for b in bounds))
    return (
        max(0, math.floor((left - PADDING) / 4) * 4),
        max(0, math.floor((top - PADDING) / 4) * 4),
        min(TARGET_SIZE, math.ceil((right + PADDING) / 4) * 4),
        min(TARGET_SIZE, math.ceil((bottom + PADDING) / 4) * 4),
    )


def package_clip(source: Path, output: Path, clip: dict, fps: int) -> dict:
    frames = read_frames(source, clip)
    left, top, right, bottom = common_bounds(frames)
    width, height = right - left, bottom - top
    columns = min(16, 2048 // width)
    rows = 2048 // height
    capacity = columns * rows
    key = KEYS[clip["name"]]
    pages = []
    for first in range(0, len(frames), capacity):
        count = min(capacity, len(frames) - first)
        page = Image.new("RGBA", (columns * width, math.ceil(count / columns) * height))
        for i in range(count):
            page.paste(frames[first + i].crop((left, top, right, bottom)),
                       (i % columns * width, i // columns * height))
        filename = f"{key}-{len(pages)}.png"
        page.save(output / filename, compress_level=9)
        pages.append({"file": filename, "width": page.width, "height": page.height, "count": count})
    return {
        "key": key, "source": clip["name"], "duration": clip["duration"], "fps": fps,
        "count": len(frames), "cellWidth": width, "cellHeight": height, "columns": columns,
        "pivotX": (TARGET_ROOT[0] - left) / width,
        "pivotY": (bottom - TARGET_ROOT[1]) / height,
        "pixelsPerUnit": PPU, "pages": pages,
        "sourceCrop": [left, top, right, bottom],
        "bindingStatus": "BOUND_DEATH_PRESENTATION" if key == "death" else "IMPORTED_NOT_BOUND",
    }


def verify(output: Path, catalogs: Path) -> dict:
    manifest = json.loads((catalogs / "manifest.json").read_text(encoding="utf-8"))
    assert {clip["key"] for clip in manifest["clips"]} == set(KEYS.values())
    total_frames = 0
    total_pages = 0
    for clip in manifest["clips"]:
        assert 0 < clip["duration"] and 0 < clip["count"] <= 600
        assert 0 <= clip["pivotX"] <= 1 and 0 <= clip["pivotY"] <= 1
        assert 0 < clip["columns"] <= 16
        assert clip["pixelsPerUnit"] == PPU
        assert sum(page["count"] for page in clip["pages"]) == clip["count"]
        for page in clip["pages"]:
            with Image.open(output / page["file"]) as image:
                assert image.size == (page["width"], page["height"])
                assert image.width <= 2048 and image.height <= 2048
                assert image.width % 4 == image.height % 4 == 0
                assert page["count"] <= clip["columns"] * (image.height // clip["cellHeight"])
        total_frames += clip["count"]
        total_pages += len(clip["pages"])
    hidden = json.loads((catalogs / "hidden-waits.json").read_text(encoding="utf-8"))
    assert {row["source"] for row in hidden["states"]} == {"Reborn_1_Idle", "Reborn_2_Idle"}
    for state in hidden["states"]:
        assert state["sourceDuration"] == 0 and state["bindingStatus"] == "IMPORTED_NOT_BOUND"
        with Image.open(output / state["file"]) as image:
            assert image.getchannel("A").getextrema() == (0, 0)
    result = {"timedClips": len(manifest["clips"]), "frames": total_frames,
              "pages": total_pages, "hiddenWaitStates": len(hidden["states"]),
              "worldUnitsPerSourceBone": .4 / PPU, "runtimeKeys": ["idle", "walk", "attack", "cast", "death"],
              "unityTests": "NOT_RUN", "unityBuild": "NOT_RUN"}
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path, help="Approved V13 preview directory")
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = root / "Assets/Art/Characters/Monsters/Boss/TalulahOriginal/ExtraClips"
    catalogs = root / "Assets/Data/ArtCatalogs/TalulahOriginal/ExtraClips"
    if args.verify_only:
        print(json.dumps(verify(output, catalogs), ensure_ascii=False))
        return
    source = args.source.resolve(strict=True)
    data = json.loads((source / "body-manifest.json").read_text(encoding="utf-8"))
    assert (data["width"], data["height"], data["fps"]) == (640, 640, 30)
    assert data["pivotX"] * 640 == SOURCE_ROOT[0] and (1 - data["pivotY"]) * 640 == SOURCE_ROOT[1]
    # Check the complete approved input set before creating or replacing output files.
    if source == output or source in output.parents or output in source.parents:
        raise ValueError("Source captures and packaged Art output must not overlap.")
    allowed = MAIN_ACTIONS | set(KEYS) | {"Reborn_1_Idle", "Reborn_2_Idle"}
    if len({clip["name"] for clip in data["clips"]}) != len(data["clips"]):
        raise ValueError("Duplicate body action name.")
    for clip in data["clips"]:
        folder = (source / clip["folder"]).resolve()
        if not folder.is_relative_to(source) or clip["name"] not in allowed:
            raise ValueError("Unsupported body action or source path.")
        for first in range(0, clip["count"], 12):
            if not (folder / f"{first}.png").is_file():
                raise FileNotFoundError(folder / f"{first}.png")
    output.mkdir(parents=True, exist_ok=True)
    catalogs.mkdir(parents=True, exist_ok=True)
    manifest = {"version": 1, "cardId": "CARD_TALULAH_TRIAL", "ppu": PPU,
                "referenceSpeed": .75, "clips": []}
    hidden = {"version": 1, "meaning": "Invisible zero-duration source states; not ordinary playable clips.",
              "sourceRootPixels": list(SOURCE_ROOT), "packagedRootPixels": list(TARGET_ROOT), "states": []}
    source_files = {"body-manifest.json": sha256(source / "body-manifest.json")}
    for clip in data["clips"]:
        if clip["name"] in MAIN_ACTIONS:
            continue
        for first in range(0, clip["count"], 12):
            file = Path(clip["folder"]) / f"{first}.png"
            source_files[file.as_posix()] = sha256(source / file)
        if clip["duration"] == 0:
            frame = read_frames(source, clip)[0]
            assert frame.getchannel("A").getextrema() == (0, 0), clip["name"]
            filename = clip["name"].lower() + "-hidden.png"
            frame.save(output / filename, compress_level=9)
            hidden["states"].append({"source": clip["name"], "sourceDuration": 0, "file": filename,
                                     "width": TARGET_SIZE, "height": TARGET_SIZE,
                                     "pixelsPerUnit": PPU, "visible": False,
                                     "bindingStatus": "IMPORTED_NOT_BOUND"})
        else:
            manifest["clips"].append(package_clip(source, output, clip, data["fps"]))
            print(clip["name"], manifest["clips"][-1]["count"], flush=True)
    (catalogs / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (catalogs / "hidden-waits.json").write_text(json.dumps(hidden, ensure_ascii=False, indent=2), encoding="utf-8")
    result = verify(output, catalogs)
    record = {
        "version": 1, "sourceModel": "enemy_1515_bsnake", "sourceSpineVersion": "3.8.99",
        "sourceModelUrl": "https://torappu.prts.wiki/assets/enemy_spine/enemy_1515_bsnake/enemy_1515_bsnake.skel",
        "sourcePreview": str(source), "sourceFilesSha256": source_files,
        "sourcePixelsPerBone": .5, "existingMainPixelsPerBone": .4, "uniformResize": .8,
        "sourceRootPixels": list(SOURCE_ROOT), "packagedRootPixels": list(TARGET_ROOT),
        "packagedPPU": PPU, "cropPolicy": "One padded alpha-union crop per complete action, retaining the source root.",
        "runtimeBinding": "Only death is added to the four existing runtime sequences. All other extras remain IMPORTED_NOT_BOUND.",
        "rights": "Original Arknights character, textures and animation belong to Hypergryph/original rights holders. Public redistribution permission unconfirmed; user upload instruction is not a rights-holder license.",
        "verification": result,
    }
    (catalogs / "SOURCE-RECORD.json").write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
