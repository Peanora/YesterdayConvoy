"""Normalize the approved highwizard CC0 black-background frames; no AI redrawing."""
from pathlib import Path
import argparse
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('source',type=Path,help='Extracted fireb directory from the original archive')
parser.add_argument('output',type=Path)
args=parser.parse_args()
sheet=Image.new('RGBA',(640,640))
for i in range(25):
    source=Image.open(args.source/f'fireB{i+1:04}.png').convert('RGB')
    if source.size!=(128,128): raise ValueError('Expected original 128x128 frames')
    pixels=[]
    for rgb in source.getdata():
        maximum=max(rgb)
        pixels.append((0,0,0,0) if maximum<5 else tuple(min(255,round(c*255/maximum)) for c in rgb)+(maximum,))
    frame=Image.new('RGBA',source.size); frame.putdata(pixels)
    sheet.paste(frame,(i%5*128,i//5*128))
args.output.parent.mkdir(parents=True,exist_ok=True)
sheet.save(args.output)
print(f'Prepared 25 frames: {args.output}; alpha={sheet.getchannel("A").getextrema()}')
