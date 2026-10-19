"""Lossless GIF frame extraction for the explicitly supplied local prototype references.

Requires Pillow. Example: python tools/art/convert_supplied_wisadel_gifs.py --source-dir <folder>
Then run Unity menu Yesterday Convoy/Initial Demo/Apply Supplied Comic Frames.
No web downloads, background keying, repainting, frame-wise recentering or rescaling.
"""
import argparse
import hashlib
import json
from io import BytesIO
from pathlib import Path
from PIL import Image


def save_png(image,path):
    buffer=BytesIO(); image.save(buffer,format='PNG'); content=buffer.getvalue()
    if not path.exists() or path.read_bytes()!=content: path.write_bytes(content)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir',type=Path,required=True)
    parser.add_argument('--project-root',type=Path,default=Path(__file__).resolve().parents[2])
    args=parser.parse_args(); root=args.project_root
    art=root/'Assets/_Game/Art/Characters/Monsters/Boss/WisadelComic'
    clips=[('walk','bf10eb0ddb1f0f14e0a3ca94c00d3735.gif',139,319),
           ('idle','1b488e2bd8eb9f4f9b41e6c801d18e22.gif',123.5,291),
           ('attack','457f7ca9427283994df616b1f4c9823b.gif',200.5,292),
           ('skill','eae171a7435b664d9c1460159a169e05.gif',256.5,323)]
    record={'sourceType':'User supplied attachments, 2026-10-19','license':'Third-party Arknights character art; public redistribution rights not established; local prototype only','clips':[]}
    for name,file,x,ground in clips:
        source=args.source_dir/file; image=Image.open(source)
        folder=art/name; folder.mkdir(parents=True,exist_ok=True)
        count=1 if name=='idle' else image.n_frames; outputs=[]
        for i in range(count):
            image.seek(i); frame=image.convert('RGBA')
            if frame.getchannel('A').getextrema()!=(0,255):
                raise ValueError(f'Missing source transparency: {file}, frame {i}')
            path=folder/f'{name}-{i:03}.png'; save_png(frame,path)
            outputs.append(path.relative_to(root).as_posix())
        record['clips'].append(dict(name=name,original=file,sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
            sourceFrames=image.n_frames,usedFrames=count,size=list(image.size),secondsPerFrame=.05,
            pivot=[x/image.width,(image.height-ground)/image.height],ppu=286/1.65,files=outputs))
    image=Image.open(args.source_dir/clips[1][1]); portrait=root/'Assets/_Game/Art/Cards/Portraits/wisadel-comic-idle.png'
    save_png(image.convert('RGBA'),portrait); record['portrait']=portrait.relative_to(root).as_posix()
    image=Image.open(args.source_dir/clips[3][1]); faces=root/'Assets/_Game/Art/VFX/Attacks/WisadelComic'
    faces.mkdir(parents=True,exist_ok=True); record['faceCrops']=[]
    for index,frame in enumerate([0,6,28,35,42,57]):
        image.seek(frame); path=faces/f'face-{index:02}.png'
        save_png(image.convert('RGBA').crop((125,8,390,272)),path)
        record['faceCrops'].append(dict(sourceFrame=frame,rect=[125,8,390,272],file=path.relative_to(root).as_posix()))
    destination=root/'docs/art/source-records/wisadel-comic.json'; destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Extracted 148 frame/idle PNGs, 1 portrait and 6 expression crops. Source pixels and alpha retained.')


if __name__=='__main__': main()
