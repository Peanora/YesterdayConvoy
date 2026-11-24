"""Install an original-Spine re-capture without changing source action timings.

Usage: python tools/art/install_formation_body_resample.py <capture-directory>
The matching runtime manifest and source provenance are updated; no Unity is launched.
"""
from pathlib import Path
import copy
import hashlib
import json
import shutil
import sys

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(PROJECT / "tools"))
from package_boss_roster import meta, recaptured_body_segments


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    capture_root = Path(sys.argv[1]).resolve(strict=True)
    capture_path = capture_root / "capture.json"
    capture = json.loads(capture_path.read_text(encoding="utf-8"))
    task = capture.get("task")
    if not capture.get("completed") or task not in {"B08", "B18", "B23"}:
        raise ValueError("A complete B08, B18 or B23 original-source re-capture is required")
    data_root = PROJECT / "Assets/Data/ArtCatalogs/BossRoster" / task / "main"
    art_root = PROJECT / "Assets/Art/Characters/Monsters/BossRoster" / task / "main"
    runtime_path = data_root / "manifest.json"
    runtime = json.loads(runtime_path.read_text(encoding="utf-8"))
    provenance_path = data_root / "SOURCE-RECORD.json"
    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    original_clips = {clip["source"]: clip for clip in runtime["clips"]}
    if set(original_clips) != {clip["source"] for clip in capture["clips"]}:
        raise ValueError("Source action set differs")
    expected_files = {page["file"] for clip in capture["clips"] for page in clip["pages"]}
    if expected_files != {record["file"] for record in capture["files"]}:
        raise ValueError("Captured frame pages differ from the source manifest")
    for record in capture["files"]:
        filename = record["file"]
        if Path(filename).name != filename or not filename.startswith("hq-"):
            raise ValueError("Invalid capture page name")
        source = capture_root / filename
        if digest(source) != record["sha256"]:
            raise ValueError("Capture hash mismatch: " + filename)
        target = art_root / filename
        shutil.copyfile(source, target)
        meta(target, PROJECT)
    updated = []
    for captured in capture["clips"]:
        original = original_clips[captured["source"]]
        for key in ("source", "duration", "count", "fps", "events", "loop", "transparentWait"):
            if original.get(key) != captured.get(key):
                raise ValueError("Original motion metadata changed: " + captured["source"] + "/" + key)
        item = copy.deepcopy(original)
        for key in ("cell", "columns", "pivotX", "pivotY", "pixelsPerUnit", "pages"):
            item[key] = captured[key]
        item["segments"] = recaptured_body_segments(task, captured)
        item["captureBounds"] = captured["captureBounds"]
        item["captureSourcePixelsPerBone"] = captured["sourcePixelsPerBone"]
        updated.append(item)
    runtime["clips"] = updated
    if task == "B08":
        runtime["capturePolicy"] = "ORIGINAL_SPINE_PER_MOTION_VIEWPORT_512_A_1024_B_FIXED_WORLD_ORIGIN"
        density_policy = "A motions use 512-pixel cells and B motions use 1024-pixel cells."
    else:
        runtime["capturePolicy"] = "ORIGINAL_SPINE_PER_MOTION_VIEWPORT_512_768_FIXED_WORLD_ORIGIN"
        density_policy = "Uses 512-pixel cells and 768-pixel cells for the widest motion bounds."
    runtime["pixelsPerUnitPolicy"] = "Per clip; global ppu retained only for the unchanged portrait. Source world scale .0045 is unchanged."
    write_json(runtime_path, runtime)
    capture_target = data_root / "HQ-CAPTURE.json"
    shutil.copyfile(capture_path, capture_target)
    meta(capture_target, PROJECT)
    provenance.setdefault("previousCaptureManifestSha256", provenance.get("atlasManifestSha256"))
    provenance["atlasManifestSha256"] = digest(capture_target)
    provenance["atlasFiles"] = capture["files"]
    provenance["recapture"] = {
        "date": "2026-11-18" if task == "B08" else "2026-11-21",
        "method": capture["method"],
        "sourceFiles": capture["sourceFiles"],
        "sourceDurationsEventsAndFrameCountsPreserved": True,
        "perMotionFixedPivotAndPPU": True,
        "noAIUpscale": True,
        "alphaChecks": len(capture["alphaChecks"]),
        "edgePixels": sum(row["edge"] for row in capture["alphaChecks"]),
        "legacyCapturePagesRetainedForProvenanceNotReferencedByRuntime": True,
    }
    provenance["geometryPolicy"] = (
        "Re-rendered the same original Spine skeleton, textures, curves and timings. "
        + density_policy
        + " Each motion keeps a fixed viewport, pivot and pixels per unit. Original source bone/world scale stays .0045."
    )
    provenance["unityTests"] = "NOT_RUN_BY_RESAMPLER_OR_INSTALLER"
    provenance["unityCamera"] = "PENDING_ROOT_GPU_VERIFICATION"
    write_json(provenance_path, provenance)
    print("FORMATION_BODY_RESAMPLE_INSTALL_PASS: %d original actions; %d new pages; unchanged durations/events/frames" % (len(updated), len(expected_files)))


if __name__ == "__main__":
    main()
