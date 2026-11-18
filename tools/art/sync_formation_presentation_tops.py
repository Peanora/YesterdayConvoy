"""Record painted per-motion source tops without changing any image or motion field.

Usage: python sync_formation_presentation_tops.py <original-review-root>
Spine geometry/capture bounds may enclose large transparent regions (notably B08).
The runtime value is the union of actual alpha>8 pixels, transformed by the original
capture pivot and PPU; original geometry/report hashes remain separate evidence.
"""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(PROJECT / "tools"))
from package_boss_roster import meta


def main():
    original = Path(sys.argv[1]).resolve(strict=True)
    for task in ("B08", "B21"):
        data = PROJECT / "Assets/Data/ArtCatalogs/BossRoster" / task / "main"
        art = PROJECT / "Assets/Art/Characters/Monsters/BossRoster" / task / "main"
        report_path = original / task / "model-report-main.json"
        report_bytes = report_path.read_bytes()
        report = json.loads(report_bytes)
        retained_report = data / "ORIGINAL-MOTION-BOUNDS.json"
        retained_report.write_bytes(report_bytes)
        meta(retained_report, PROJECT)
        source_motions = {row["name"]: row for row in report["animations"]}
        manifest_path = data / "manifest.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        rows = []
        for clip in manifest["clips"]:
            before = dict(clip)
            before.pop("presentationTop", None)
            top = float("-inf")
            samples = 0
            cell, columns = clip["cell"], clip["columns"]
            ppu = clip.get("pixelsPerUnit", manifest["ppu"])
            files = []
            for page in clip["pages"]:
                path = art / page["file"]
                payload = path.read_bytes()
                files.append({"file": page["file"], "sha256": hashlib.sha256(payload).hexdigest()})
                with Image.open(path) as image:
                    alpha = image.getchannel("A").point(lambda value: 255 if value > 8 else 0)
                    for index in range(page["count"]):
                        x, y = index % columns * cell, index // columns * cell
                        bounds = alpha.crop((x, y, x + cell, y + cell)).getbbox()
                        if bounds:
                            top = max(top, (cell - bounds[1] - clip["pivotY"] * cell) / ppu)
                            samples += 1
            if samples == 0:
                # Some retained source wait motions are completely transparent.
                # They have no painted top; record zero rather than use empty-cell size.
                top = 0
            elif top <= 0:
                raise ValueError("Painted body top is below its capture root: " + task + "/" + clip["source"])
            clip["presentationTop"] = top
            after = dict(clip)
            after.pop("presentationTop", None)
            assert after == before
            geometry = source_motions[clip["source"]]["bounds"]
            geometry_top = geometry[3] * .0045 if geometry else None
            # Geometry reports remain independent evidence; runtime metadata follows
            # the actually painted original capture when their envelopes differ.
            rows.append({"source": clip["source"], "presentationTop": top,
                         "sourceGeometryBounds": geometry, "sourceGeometryTop": geometry_top,
                         "paintedMinusReportedGeometryTop": None if geometry_top is None else top - geometry_top,
                         "captureBounds": clip.get("captureBounds"), "visibleFrames": samples,
                         "frameCount": clip["count"], "pixelsPerUnit": ppu, "pivotY": clip["pivotY"],
                         "pages": files})
        manifest["presentationTopPolicy"] = "Per-motion union of original capture alpha>8 pixels, transformed by fixed source pivot/PPU. Transparent canvas and transparent source attachment space excluded."
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        provenance = data / "PRESENTATION-TOPS-SOURCE.json"
        provenance.write_text(json.dumps({
            "version": 1, "task": task, "method": "ORIGINAL_CAPTURE_PAINTED_ALPHA_UNION",
            "alphaThreshold": 8, "originalReviewReport": str(report_path),
            "originalReviewReportSha256": hashlib.sha256(report_bytes).hexdigest(),
            "retainedOriginalReport": retained_report.name,
            "worldUnitsPerSourceBone": .0045, "rows": rows,
            "preserved": "All PNG bytes, image SHA, source duration/events/count/pages/pivot/PPU unchanged; only presentationTop and its explicit policy are added.",
            "unityTests": "NOT_RUN_BY_GENERATOR",
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        meta(provenance, PROJECT)
        print(task, "PRESENTATION_TOP_PASS", len(rows), "motions", flush=True)


if __name__ == "__main__":
    main()
