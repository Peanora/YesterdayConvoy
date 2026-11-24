"""Losslessly re-encode captured PNG pages and verify every RGBA byte is unchanged."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import hashlib
import io
import json
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve(strict=True)
manifest_path = root / "capture.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))


def optimize(record):
    path = root / record["file"]
    original = path.read_bytes()
    if hashlib.sha256(original).hexdigest() != record["sha256"]:
        raise ValueError("Capture hash mismatch: " + record["file"])
    image = Image.open(io.BytesIO(original))
    pixels = image.convert("RGBA").tobytes()
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True, compress_level=9)
    result = buffer.getvalue()
    if Image.open(io.BytesIO(result)).convert("RGBA").tobytes() != pixels:
        raise ValueError("Lossless pixel verification failed: " + record["file"])
    if len(result) >= len(original):
        result = original
    path.write_bytes(result)
    return {**record, "originalCaptureSha256": record.get("originalCaptureSha256", record["sha256"]),
            "rgbaSha256": hashlib.sha256(pixels).hexdigest(), "bytes": len(result),
            "sha256": hashlib.sha256(result).hexdigest()}


before = sum(record["bytes"] for record in manifest["files"])
with ThreadPoolExecutor(max_workers=4) as pool:
    manifest["files"] = list(pool.map(optimize, manifest["files"]))
manifest["pngEncoding"] = "Lossless PNG optimization; every decoded RGBA byte compared equal to the original Spine renderer capture."
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("ORIGINAL_CAPTURE_LOSSLESS_PASS", len(manifest["files"]), before, sum(record["bytes"] for record in manifest["files"]))
