"""Package approved Boss source motion without resampling, resizing or launching Unity.

Existing source PNGs are copied byte-for-byte; long motions are split at page boundaries to fit
ArtSequence's 600-frame cache unit, while the raw clip retains its full duration.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import re
import shutil
import struct
import uuid

WORLD_UNITS_PER_BONE = .0045  # Existing original Talulah body scale (.4 / 88.8888889).
RUNTIME_ALIASES = {
    "B01": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Skill", "death": "Die"},
    "B02": {"idle": "Idle", "walk": "Move", "attack": "Attack", "death": "Die"},
    "B03": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Disappear", "death": "Die"},
    "B04": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill2", "death": "B_Die"},
    "B05": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Skill", "death": "Die"},
    "B06": {"idle": "C1_Idle", "walk": "C1_Move", "attack": "C1_Attack", "cast": "C1_Skill_2", "death": "C2_Die_1"},
    "B07": {"idle": "Idle_a", "walk": "Move_a", "attack": "Attack_a", "cast": "Skill_a_1", "death": "Die_End"},
    "B08": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_1", "death": "B_Die"},
    "B09": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_2", "death": "B_Die"},
    "B10": {"idle": "Idle", "walk": "Move", "cast": "Skill"},
    "B11": {"idle": "A_Idle", "attack": "A_Attack", "cast": "A_Skill_1", "death": "B_Die"},
    "B12": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_Begin", "death": "C_Die"},
    "B13": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "death": "B_Die_1"},
    "B14": {"idle": "Idle", "attack": "Attack", "cast": "Skill_1", "death": "Die"},
    "B15": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_1", "death": "B_Die"},
    "B16": {"idle": "Idle_A", "walk": "Move_A", "attack": "Attack_A", "cast": "Skill_1_A", "death": "Die"},
    "B17": {"idle": "Idle_1", "walk": "Move_1", "attack": "Attack_1", "cast": "Skill", "death": "Die"},
    "B18": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Skill_1", "death": "Die"},
    "B19": {"idle": "Idle", "walk": "Move", "attack": "Attack_1", "cast": "Skill_1", "death": "Die"},
    "B20": {"idle": "Idle", "attack": "Attack_A", "cast": "Skill_3_Begin", "death": "Die"},
    "B21": {"idle": "Skill_Idle_A", "cast": "Skill_Begin_A", "death": "Die_3_End_A"},
    "B22": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Skill", "death": "Die"},
    "B23": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack_1", "cast": "A_Skill_1", "death": "B_Die"},
    "B24": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_1", "death": "B_Die"},
    "B25": {"idle": "A_Idle", "walk": "A_Move", "attack": "A_Attack", "cast": "A_Skill_1_Begin", "death": "B_Die"},
    "B26": {"idle": "Idle", "walk": "Move", "attack": "Attack_1", "cast": "Skill_1", "death": "Die"},
    "P14": {"idle": "Idle", "attack": "Attack", "cast": "Skill_1", "death": "Die_1"},
    "P12I": {"idle": "Idle", "walk": "Move", "death": "Die", "start": "Start"},
    "P15C": {"idle": "Idle", "walk": "Move", "attack": "Attack", "death": "Die"},
    "P14H": {"idle": "Idle", "walk": "Move", "attack": "Attack", "cast": "Skill_Begin", "death": "Die"},
    "P20": {"idle": "Idle", "attack": "Attack", "death": "Die", "start": "Start"},
    "P24D": {"idle": "Idle", "walk": "Move", "attack": "Attack", "death": "Die"},
    "P25M": {"idle": "Idle", "walk": "Move", "attack": "Attack", "death": "Die", "start": "Start"},
    "P26": {"idle": "Idle", "walk": "Move", "cast": "Skill", "death": "Die"},
}
CORE_CARDS = {"B02": "CARD_M25", "B06": "CARD_M32", "B16": "CARD_TALULAH_TRIAL", "B17": "CARD_M20", "B18": "CARD_M14", "B19": "CARD_M28", "B20": "CARD_WISADEL_APRIL"}
ADDITIONAL_AUXILIARIES = {"P12I": "Idea", "P15C": "CoreRed", "P24D": "CoreBlack", "P25M": "Matrix"}
SEPARATE_AUXILIARY_REPORTS = ("P20", "P14H", *ADDITIONAL_AUXILIARIES)


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_bytes(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def write_json(path, data):
    write_bytes(path, (json.dumps(data, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))


def meta(path, project, folder=False):
    target = path.with_name(path.name + ".meta")
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "YesterdayConvoy/" + path.relative_to(project).as_posix()).hex
    text = "fileFormatVersion: 2\nguid: " + guid + "\n"
    if folder:
        text += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".json":
        text += "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".png":
        # Unity 6000.6.5f1 interprets a GUID-only PNG meta as obsolete importer v1.
        # Reuse a real current v13 2D TextureImporter, replacing only its asset GUID.
        templates = [project / "Assets/Art/Characters/Monsters/BossRoster/B22/main/Attack-000.png.meta",
                     project / "Assets/Art/Characters/Monsters/Boss/TalulahOriginal/ExtraClips/death-0.png.meta"]
        template = next((p.read_text(encoding="utf-8") for p in templates if p.exists() and
                         re.search(r"(?ms)^TextureImporter:.*?^  serializedVersion: 13\s*$", p.read_text(encoding="utf-8"))), None)
        if template is None:
            raise ValueError("A current Unity v13 TextureImporter template is required for new PNG metadata")
        text = re.sub(r"(?m)^guid: [^\r\n]+", "guid: " + guid, template)
    write_bytes(target, text.encode("utf-8"))


def ensure_folder(path, project):
    missing = []
    current = path
    while not current.exists():
        missing.append(current)
        current = current.parent
    for folder in reversed(missing):
        folder.mkdir()
        meta(folder, project, True)
    current = path
    while current.is_relative_to(project / "Assets") and current != project / "Assets":
        meta(current, project, True)
        current = current.parent


def source_file(folder, name):
    path = (folder / name).resolve(strict=True)
    if not path.is_relative_to(folder) or path.name != name:
        raise ValueError("Unsupported source page path: " + name)
    return path


def dimensions(path):
    data = path.read_bytes()[:24]
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("Not a PNG: " + str(path))
    return struct.unpack(">II", data[16:24])


def segments(clip):
    result, pages, count, first = [], [], 0, 0
    for page in clip["pages"]:
        if pages and count + page["count"] > 600:
            result.append({"firstFrame": first, "count": count, "pages": pages})
            first += count
            pages, count = [], 0
        if page["count"] > 600:
            raise ValueError("A source page exceeds one ArtSequence cache unit")
        pages.append(dict(page))
        count += page["count"]
    if pages:
        result.append({"firstFrame": first, "count": count, "pages": pages})
    return result


def validate_source(folder, data):
    names = set()
    files = {item["file"]: item for item in data["files"]}
    for clip in data["clips"]:
        if clip["key"] in names or clip["source"] != clip["key"]:
            raise ValueError("Duplicate or renamed original motion")
        names.add(clip["key"])
        if clip["duration"] <= 0 or clip["fps"] != 60 or clip["count"] <= 0:
            raise ValueError("Invalid timed original clip")
        if sum(p["count"] for p in clip["pages"]) != clip["count"]:
            raise ValueError("Source frame count mismatch")
        for page in clip["pages"]:
            path = source_file(folder, page["file"])
            if sha256(path) != files[page["file"]]["sha256"]:
                raise ValueError("Source page hash differs: " + str(path))
            if dimensions(path) != (page["width"], page["height"]):
                raise ValueError("Source PNG dimension mismatch")
            cell = clip["cell"]
            if page["width"] != clip["columns"] * cell or page["height"] % cell or max(page["width"], page["height"]) > 2048:
                raise ValueError("Source atlas is incompatible with ArtSequence")
    for state in data["zeroDurationStates"]:
        if state["duration"] != 0 or state["name"] in names:
            raise ValueError("Invalid zero-duration metadata state")


def package(project, source, task, view="main"):
    auxiliary = task.startswith("P")
    source_root = source / "auxiliary" if auxiliary else source
    folder = (project / "Assets/Art/Characters/Monsters/BossRoster" / task / view if auxiliary else source / "atlases" / task / view).resolve(strict=True)
    capture_manifest = project / "Assets/Data/ArtCatalogs/BossRoster" / task / view / "CAPTURE-MANIFEST.json" if auxiliary else folder / "manifest.json"
    data = read_json(capture_manifest)
    validate_source(folder, data)
    models = [read_json(project / "Assets/Data/ArtCatalogs/BossRoster" / task / "main/SOURCE-MODEL-REPORT.json")] if task in SEPARATE_AUXILIARY_REPORTS else read_json(source_root / "MODEL-REPORT.json")["models"]
    model = next(m for m in models if m["task"] == task and m["view"] == view)
    if task == "P20":
        acquisition = read_json(source / "native-fx/runtime-additions/Wward/EXTRACTION.json")
    elif task == "P14H":
        acquisition = {"files": read_json(source / "native-fx/runtime-additions/HoundProto/ACQUISITION.json")["records"]}
    elif task in ADDITIONAL_AUXILIARIES:
        addition = source / "native-fx/runtime-additions" / ADDITIONAL_AUXILIARIES[task]
        acquisition = read_json(addition / "ACQUISITION.json")
        for record in acquisition["files"]:
            assert sha256(addition / "source" / record["file"]) == record["sha256"], (task, record["file"])
    else:
        acquisition = next(r for r in read_json(source_root / "ACQUISITION.json")["records"] if r["task"] == task)
    aliases = dict(RUNTIME_ALIASES[task])
    if task == "B20" and view == "back":
        aliases.pop("death")
    by_name = {clip["key"]: clip for clip in data["clips"]}
    if any(key not in by_name for key in aliases.values()):
        raise ValueError("A runtime alias is absent from the original motion set")
    out = project / "Assets/Art/Characters/Monsters/BossRoster" / task / view
    catalogs = project / "Assets/Data/ArtCatalogs/BossRoster" / task / view
    ensure_folder(out, project)
    ensure_folder(catalogs, project)
    ppu = data["scale"] / WORLD_UNITS_PER_BONE
    idle_bounds = next(a["bounds"] for a in model["animations"] if a["name"] == aliases["idle"])
    manifest = {
        "version": 1, "task": task, "cardId": "AUX_" + task if auxiliary else CORE_CARDS.get(task, "CARD_" + task), "name": data["name"],
        "view": view, "model": model["model"], "skin": model["skin"],
        "ppu": ppu, "referenceSpeed": .75,
        "height": (idle_bounds[3] - idle_bounds[1]) * WORLD_UNITS_PER_BONE,
        "sourcePixelsPerBone": data["scale"], "worldUnitsPerSourceBone": WORLD_UNITS_PER_BONE,
        "scaleStatus": "TALULAH_BODY_BASELINE_B22_ACTUAL_CAMERA_VERIFIED_OTHER_UNITS_PENDING_CAMERA_REVIEW",
        "pivotX": data["pivotX"], "pivotY": data["pivotY"],
        "captureBounds": data["captureBounds"],
        "aliases": [{"key": key, "source": value} for key, value in aliases.items()],
        "runtimeSources": list(dict.fromkeys(aliases.values())), "clips": [],
        "role": "SKILL_AUXILIARY_NOT_A_CARD" if auxiliary else "EXISTING_CARD_APPROVED_SOURCE_VISUAL" if task in CORE_CARDS else "NEW_BOSS_CARD",
        "movementPresentation": "ORIGINAL_MOVE" if "walk" in aliases else "NO_SOURCE_MOVE_IDLE_TRANSLATION_ONLY_IF_BATTLE_REQUIRES",
        "idlePresentation": "VISIBLE_SOURCE_SKILL_IDLE_ADAPTATION_ORIGINAL_IDLE_REMAINS_TRANSPARENT" if task == "B21" else "ORIGINAL_IDLE",
        "deathPresentation": "ORIGINAL_FINAL_DEATH_SOURCE_PHASE_MUST_BE_SELECTED_BY_BATTLE" if aliases.get("death", "").startswith(("B_", "C_")) else "NO_SOURCE_DEATH" if "death" not in aliases else "ORIGINAL_DEATH",
        "zeroDurationStates": data["zeroDurationStates"],
        "sampling": "Frame i is the unchanged source sample at min(i / fps, sourceDuration); the terminal sample is retained.",
        "status": "IMPORTED_NOT_BOUND",
    }
    for clip in data["clips"]:
        entry = {key: value for key, value in clip.items() if key != "pages"}
        entry["pixelsPerUnit"] = ppu
        entry["segments"] = segments(clip)
        # Flat pages are also understood by the existing PreparedArtTools build planner.
        entry["pages"] = clip["pages"]
        manifest["clips"].append(entry)
        for page in clip["pages"]:
            src = source_file(folder, page["file"])
            dst = out / page["file"]
            if not dst.exists() or sha256(dst) != sha256(src):
                shutil.copyfile(src, dst)
            meta(dst, project)
    portrait = out / "portrait.png" if auxiliary else source / task / ("poster.png" if view == "main" else "poster-" + view + ".png")
    if not portrait.exists():
        portrait = source_root / task / "poster.png"
    if not (out / "portrait.png").exists() or sha256(out / "portrait.png") != sha256(portrait):
        shutil.copyfile(portrait, out / "portrait.png")
    meta(out / "portrait.png", project)
    write_json(catalogs / "manifest.json", manifest)
    meta(catalogs / "manifest.json", project)
    provenance = {
        "version": 1, "task": task, "view": view, "sourcePreview": str(source),
        "approvedScope": "User approved all 26 selected Bosses and their necessary original skill auxiliaries for game integration; the B22 representative passed the actual Unity camera gate.",
        "reference": model["reference"], "model": model["model"], "spineVersion": model["version"],
        "sourceFiles": acquisition["files"], "atlasManifestSha256": sha256(capture_manifest),
        "atlasFiles": data["files"], "portraitSha256": sha256(portrait),
        "captureAlphaSampling": data.get("alphaSampling"),
        "geometryPolicy": "Original frame PNG bytes, frame count, source duration, root pivot and source event payloads preserved. No image resizing or animation retiming.",
        "worldScale": {"unitsPerSourceBone": WORLD_UNITS_PER_BONE, "ppu": ppu, "basis": "Existing original Talulah body scale; representative B22 actual camera verified. Other units retain source proportions pending camera review."},
        "runtimePolicy": "Only runtimeSources are proposed for default prewarm; raw optional motions use the existing per-sequence cache on demand. Zero-duration states have metadata only.",
        "rights": "Original Arknights model, textures and motions belong to Hypergryph/original rights holders. Open redistribution permission unconfirmed; user upload authorization is not a rights-holder license.",
        "unityTests": "NOT_RUN_BY_PACKAGER", "unityCamera": "NOT_RUN_BY_PACKAGER",
    }
    write_json(catalogs / "SOURCE-RECORD.json", provenance)
    meta(catalogs / "SOURCE-RECORD.json", project)
    if auxiliary:
        for filename in ("CAPTURE-MANIFEST.json", "CAPTURE-QA.json"):
            meta(catalogs / filename, project)
        if task in SEPARATE_AUXILIARY_REPORTS:
            meta(catalogs / "SOURCE-MODEL-REPORT.json", project)
    installation_path = project / "Assets/Data/ArtCatalogs/BossRoster/installation.json"
    installation = read_json(installation_path) if installation_path.exists() else {"version": 1, "units": []}
    installation.pop("excludedExistingCores", None)
    installation["coreCardBindings"] = CORE_CARDS
    attack = by_name.get(aliases.get("attack"))
    contacts = [e["time"] for e in attack["events"] if e["name"] == "OnAttack"] if attack else []
    installed = {"task": task, "cardId": manifest["cardId"], "name": data["name"], "view": view,
                 "model": model["model"], "manifest": f"BossRoster/{task}/{view}/manifest.json",
                 "aliases": manifest["aliases"], "runtimeSources": manifest["runtimeSources"],
                 "hasAttack": bool(attack), "attackDuration": attack["duration"] if attack else 0,
                 "attackContact": contacts[0] if contacts else 0, "sourceOnAttackTimes": contacts,
                 "ppu": ppu, "height": manifest["height"], "status": "IMPORTED_NOT_BOUND"}
    for key in ("role", "movementPresentation", "idlePresentation", "deathPresentation"):
        installed[key] = manifest[key]
    installation["units"] = sorted([u for u in installation["units"] if (u["task"], u["view"]) != (task, view)] + [installed], key=lambda u: (u["task"], u["view"]))
    write_json(installation_path, installation)
    meta(installation_path, project)
    return verify(project, task, view)


def verify(project, task, view):
    out = project / "Assets/Art/Characters/Monsters/BossRoster" / task / view
    catalogs = project / "Assets/Data/ArtCatalogs/BossRoster" / task / view
    data = read_json(catalogs / "manifest.json")
    source = read_json(catalogs / "SOURCE-RECORD.json")
    preview = Path(source["sourcePreview"])
    auxiliary = task.startswith("P")
    expected = read_json(catalogs / "CAPTURE-MANIFEST.json" if auxiliary else preview / "atlases" / task / view / "manifest.json")
    assert data["cardId"] == ("AUX_" + task if auxiliary else CORE_CARDS.get(task, "CARD_" + task))
    originals = [read_json(catalogs / "SOURCE-MODEL-REPORT.json")] if task in SEPARATE_AUXILIARY_REPORTS else read_json((preview / "auxiliary" if auxiliary else preview) / "MODEL-REPORT.json")["models"]
    original = next(m for m in originals if (m["task"], m["view"]) == (task, view))
    by_source = {a["name"]: a for a in original["animations"]}
    assert len(data["clips"]) == len(expected["clips"])
    assert data["zeroDurationStates"] == expected["zeroDurationStates"]
    assert data["ppu"] == expected["scale"] / WORLD_UNITS_PER_BONE
    hashes = {f["file"]: f["sha256"] for f in source["atlasFiles"]}
    pages = 0
    for clip, expected_clip in zip(data["clips"], expected["clips"]):
        for key, value in expected_clip.items():
            assert clip[key] == value, (task, view, clip["source"], key)
        native = by_source[clip["source"]]
        assert clip["duration"] == native["duration"] and clip["events"] == native["events"]
        assert clip["duration"] > 0 and clip["pixelsPerUnit"] == data["ppu"]
        assert 0 <= clip["pivotX"] <= 1 and 0 <= clip["pivotY"] <= 1
        assert sum(s["count"] for s in clip["segments"]) == clip["count"]
        assert sum(p["count"] for p in clip["pages"]) == clip["count"]
        first = 0
        for segment in clip["segments"]:
            assert segment["firstFrame"] == first and 0 < segment["count"] <= 600
            first += segment["count"]
        assert [p for s in clip["segments"] for p in s["pages"]] == clip["pages"]
        for page in clip["pages"]:
            path = out / page["file"]
            assert sha256(path) == hashes[page["file"]]
            assert dimensions(path) == (page["width"], page["height"])
            assert path.with_name(path.name + ".meta").exists()
            pages += 1
    assert sha256(out / "portrait.png") == source["portraitSha256"]
    assert all(s["duration"] == 0 for s in data["zeroDurationStates"])
    assert {p.name for p in out.glob("*.png")} == set(hashes) | {"portrait.png"}
    for folder in (out, catalogs):
        assert folder.with_name(folder.name + ".meta").exists()
        for path in folder.iterdir():
            if path.suffix in (".png", ".json"):
                assert path.with_name(path.name + ".meta").exists(), path
    result = {"task": task, "view": view, "timedClips": len(data["clips"]),
              "frames": sum(c["count"] for c in data["clips"]), "pages": pages,
              "zeroDurationMetadataStates": len(data["zeroDurationStates"]),
              "runtimeSources": data["runtimeSources"], "ppu": data["ppu"],
              "pngBytes": sum(p.stat().st_size for p in out.glob("*.png")),
              "cacheSegments": sum(len(c["segments"]) for c in data["clips"]),
              "originalBytesAndHashesPreserved": True, "originalTimingEventsAndPivotPreserved": True, "unityTests": "NOT_RUN"}
    write_json(catalogs / "PACKAGE-QA.json", result)
    meta(catalogs / "PACKAGE-QA.json", project)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--task", default="B22", choices=sorted(RUNTIME_ALIASES))
    parser.add_argument("--all", action="store_true", help="All approved 26 bodies (including seven existing CardIds), Wisadel front/back, and eight required skill auxiliaries")
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1]
    source = args.source.resolve(strict=True)
    entries = [(m["task"], m["view"]) for m in read_json(source / "MODEL-REPORT.json")["models"]] + [(task, "main") for task in RUNTIME_ALIASES if task.startswith("P")] if args.all else [(args.task, "front" if args.task == "B20" else "main")]
    results = []
    for task, view in entries:
        result = verify(project, task, view) if args.verify_only else package(project, source, task, view)
        results.append(result)
        print(json.dumps(result, ensure_ascii=False), flush=True)
    if args.all:
        summary = {"models": len(results), "newRecruitableBosses": 19, "existingCoreCardsWithApprovedSourceVisuals": CORE_CARDS, "skillAuxiliaries": len([task for task in RUNTIME_ALIASES if task.startswith("P")]),
                   "externalCompleteBodyLibrary": {"models": 27, "timedClips": 393, "location": str(source)},
                   "timedClips": sum(r["timedClips"] for r in results), "frames": sum(r["frames"] for r in results),
                   "pages": sum(r["pages"] for r in results), "zeroDurationMetadataStates": sum(r["zeroDurationMetadataStates"] for r in results),
                   "pngBytes": sum(r["pngBytes"] for r in results), "cacheSegments": sum(r["cacheSegments"] for r in results),
                   "originalBytesAndHashesPreserved": True, "originalTimingEventsAndPivotPreserved": True,
                   "packages": results, "unityTests": "NOT_RUN_BY_PACKAGER"}
        path = project / "Assets/Data/ArtCatalogs/BossRoster/PACKAGE-QA.json"
        write_json(path, summary)
        meta(path, project)
        print(json.dumps(summary, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
