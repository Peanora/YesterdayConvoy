"""Install the verified original game#9 outfit capture, leaving B20 untouched."""
from pathlib import Path
import hashlib
import json
import shutil
import sys

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(PROJECT / "tools"))
from package_boss_roster import ensure_folder, meta, segments


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta(path, PROJECT)


def main():
    source = Path(sys.argv[1]).resolve(strict=True)
    capture = read(source / "capture/capture.json")
    report = read(source / "model-report.json")
    if not capture.get("completed") or capture["task"] != "WIS_APRIL_G9":
        raise ValueError("Completed game#9 original-source capture required")
    data = PROJECT / "Assets/Data/ArtCatalogs/BossRoster/WIS_APRIL_G9/main"
    art = PROJECT / "Assets/Art/Characters/Monsters/BossRoster/WIS_APRIL_G9/main"
    for directory in (data, art):
        ensure_folder(directory, PROJECT)
    files = {row["file"]: row for row in capture["files"]}
    clips = []
    for row in capture["clips"]:
        original = next(a for a in report["animations"] if a["name"] == row["source"])
        if row["duration"] != original["duration"] or row["events"] != original["events"]:
            raise ValueError("Original action timing or events changed")
        for page in row["pages"]:
            filename = page["file"]
            if Path(filename).name != filename:
                raise ValueError("Invalid capture page path")
            src = source / "capture" / filename
            if hashlib.sha256(src.read_bytes()).hexdigest() != files[filename]["sha256"]:
                raise ValueError("Capture page hash mismatch")
            shutil.copyfile(src, art / filename)
            meta(art / filename, PROJECT)
        clips.append({**row, "segments": segments(row)})
    idle = next(row for row in clips if row["source"] == "Idle")
    raw_idle = next(row for row in report["animations"] if row["name"] == "Idle")
    aliases = {"idle": "Idle", "attack": "Attack_A", "cast": "Skill_3_Begin", "skill": "Skill_3_Begin", "death": "Die", "start": "Start"}
    manifest = {
        "version": 1, "task": "WIS_APRIL_G9", "cardId": "CARD_WISADEL_APRIL",
        "name": "维维美 · 绝对主角", "view": "main", "model": report["model"], "skin": report["skin"],
        "ppu": idle["pixelsPerUnit"], "height": (raw_idle["bounds"][3] - raw_idle["bounds"][1]) * .0045,
        "referenceSpeed": .65, "worldUnitsPerSourceBone": .0045,
        "sourceClientBoneScale": .0027,
        "sourceSkinId": "char_1035_wisdel@game#9",
        "sourceSpineVersion": report["version"],
        "aliases": [{"key": key, "source": value} for key, value in aliases.items()],
        "runtimeSources": list(dict.fromkeys(aliases.values())), "clips": clips,
        "zeroDurationStates": [row for row in report["animations"] if row["duration"] == 0],
        "noMoveClip": True,
        "missingActionPolicy": "No original Move exists: retain original Idle while relocating; no invented walk. Full source S1/S2/S3/Die are present.",
    }
    write(data / "manifest.json", manifest)
    write(data / "CAPTURE-QA.json", capture)
    acquisition = read(source / "ACQUISITION.json")
    prefab = read(source / "original-skin-prefab.json")
    skinpack = read(source / "original-skinpack-acquisition.json")
    crosscheck = read(source / "original-body-crosscheck.json")
    if not any(row["name"].endswith(".skel") and row["matchesPrtsBodyFile"] for row in crosscheck):
        raise ValueError("PRTS body does not match the retained official skinpack skeleton")
    write(data / "ORIGINAL-SKIN-PREFAB.json", prefab)
    write(data / "ORIGINAL-SKINPACK-SOURCE.json", skinpack)
    write(data / "ORIGINAL-BODY-CROSSCHECK.json", crosscheck)
    write(data / "SKIN-TABLE-EXTRACT.json", read(source / "skin_table-wisdel-extract.json"))
    write(data / "SOURCE-RECORD.json", {
        **acquisition, "date": "2026-11-18", "package": "WIS_APRIL_G9/main",
        "method": capture["method"], "sourceSpineVersion": report["version"],
        "originalClientVersion": skinpack["version"],
        "originalSkinpackManifestDigest": skinpack["manifestDigest"],
        "frontSkeletonMatchesOriginalBundleByteForByte": True,
        "atlasNormalization": "Original page name game#9 becomes game_9 in the PRTS export; all other atlas bytes match.",
        "originalSkinPrefab": {"bundle": prefab["bundle"], "sha256": prefab["bundleSha256"], "container": prefab["container"]},
        "originalPrefabFaceSwitcherScale": .27000001072883606,
        "originalSkeletonDataAssetScale": .009999999776482582,
        "presentationWorldUnitsPerSourceBone": .0045,
        "presentationScalePolicy": "Same calibrated original-bone/world ratio as all formation native roles; no rarity or Size normalization. Original client .27*.01 transforms retained separately.",
        "bodyFiles": capture["files"], "sourceDurationsAndEventsPreserved": True,
        "completeSourceSkillsAndDeath": True, "hasOriginalMove": False,
        "userGifFallback": "Existing WisadelComic assets and CardDefinition Sprite references retained unchanged.",
        "noAIEnhancement": True, "unityTests": "NOT_RUN_BY_INSTALLER",
        "rights": "Original Arknights media; user requested local game integration. No claim of open redistribution permission.",
    })
    shutil.copyfile(source / "capture/Idle-first.png", art / "portrait.png")
    meta(art / "portrait.png", PROJECT)
    print("WISADEL_APRIL_INSTALL_PASS: %d original actions, %d pages; original events preserved; B20 unchanged" % (len(clips), len(files)))


if __name__ == "__main__":
    main()
