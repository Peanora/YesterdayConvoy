"""Pack approved, premultiplied offline frames; no original bundle is a runtime dependency."""
import argparse
import gzip
import json
from pathlib import Path
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    specs = [
        ('ground', '2026-11-05_塔露拉火雨-v12/frames/floor-fire', 36, 144, (184, 248, 328, 424), 96),
        ('stream', '2026-11-05_塔露拉整套动作-v10/frames/trail-full', 0, 351, (64, 304, 512, 448), 56),
        ('impact', '2026-11-05_塔露拉整套动作-v10/frames/impact', 0, 39, (176, 296, 336, 440), 96),
    ]
    manifest = {'version': 1, 'fps': 30, 'clips': []}
    for key, folder, start, count, box, ppu in specs:
        w, h = box[2]-box[0], box[3]-box[1]
        columns, rows = 2048//w, 2048//h
        capacity = columns*rows
        clip = dict(key=key, width=w, height=h, columns=columns, count=count, ppu=ppu,
                    pivotX=(256-box[0])/w, pivotY=1-(368-box[1])/h, pages=[])
        for page, first in enumerate(range(0, count, capacity)):
            n = min(capacity, count-first)
            atlas = Image.new('RGBA', (columns*w, ((n+columns-1)//columns)*h))
            for i in range(n):
                raw = gzip.decompress((args.preview_root/folder/f'{start+first+i}.gz').read_bytes())
                image = Image.frombytes('RGBA', (512, 512), raw).transpose(Image.Transpose.FLIP_TOP_BOTTOM).crop(box)
                # No alpha-based paste: preserve additive RGB in pixels whose alpha is zero.
                atlas.paste(image, (i % columns*w, i//columns*h))
            name = f'{key}-{page:02}.png'
            atlas.save(args.output/name)
            clip['pages'].append(dict(file=name, count=n))
        manifest['clips'].append(clip)
        print(key, count, len(clip['pages']))
    (args.output/'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
