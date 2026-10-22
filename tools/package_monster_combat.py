"""Pack already-approved LOCAL frames. Outputs contain original media: never commit them."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

PROFILES = {
    'M02': ('M02-Hound-v1', 'Move_Loop', 1.1),
    'M03': ('M03-GuerrillaHound-v1', 'Run_Loop', 1.35),
    'M04': ('M04-ExplosiveSlug-v1', 'Move_Loop', .65),
    'M05': ('M05-AcidSlug-v1', 'Move_Loop', .65),
    'M06': ('M06-IceSlug-v1', 'Move_Loop', .65),
    'M07': ('M07-Soldier-v1', 'Move_Loop', .75),
    'M08': ('M08-Archer-v1', 'Move_Loop', .75),
    'M09': ('M09-Caster-v1', 'Move_Loop', .65),
    'M10': ('M10-Defender-v1', 'Move_Loop', .55),
}

def package(previews, destination, task):
    folder, move, speed = PROFILES[task]
    source = previews / folder
    out = destination / task
    out.mkdir(parents=True, exist_ok=True)
    durations = {c['name']: c['duration'] for c in json.loads((source/'clips.json').read_text(encoding='utf-8'))}
    pivot = (280/640, 1-550/640) if task == 'M08' else (.5, 1-490/640)
    clips, records = [], []
    def save(image, name):
        image.save(out/name)
        records.append({'file': name, 'sha256': hashlib.sha256((out/name).read_bytes()).hexdigest()})
    for key, raw in [('idle','Idle'),('walk',move),('attack','Attack'),('death','Die')]:
        count = round(durations[raw]*60)
        pages = []
        for first in range(0, count, 64):
            n = min(count-first,64)
            rows = (n+7)//8
            sheet = Image.new('RGBA',(2048,rows*256))
            for j in range(n):
                with Image.open(source/'frames'/raw/f'{first+j:04}.png') as frame:
                    assert frame.size == (640,640), 'Unexpected source canvas'
                    sheet.paste(frame.convert('RGBA').resize((256,256),Image.Resampling.LANCZOS),(j%8*256,j//8*256))
            filename = f'{key}-{first//64:02}.png'
            save(sheet,filename)
            pages.append({'file':filename,'count':n,'width':sheet.width,'height':sheet.height})
        clips.append({'key':key,'count':count,'duration':count/60,'cell':256,'columns':8,'pivotX':pivot[0],'pivotY':pivot[1],'pages':pages})
    with Image.open(source/'card.png') as portrait: save(portrait.convert('RGBA'),'portrait.png')
    with Image.open(source/'frames/Idle/0000.png') as idle:
        bounds=idle.getbbox()
        height=(bounds[3]-bounds[1])*.4/80+.15
    manifest={'version':1,'task':task,'ppu':80,'referenceSpeed':speed,'height':height,'clips':clips,'files':records,
              'provenance':'Approved local preview '+folder+'; source and rights records remain beside it. No public redistribution license confirmed.'}
    (out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(task, sum(c['count'] for c in clips),'frames',len(records),'local PNGs')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--previews',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--task',choices=PROFILES,required=True)
    args=parser.parse_args()
    package(args.previews,args.output,args.task)
