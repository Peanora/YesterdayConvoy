"""Pack approved frames and transparent portraits for the game's runtime assets."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

PROFILES = {
    'M02': ('M02-Hound-v1', 'Move_Loop', 1.1),
    'M03': ('M03-GuerrillaHound-v1', 'Run_Loop', 1.35),
    'M04': ('M04-ExplosiveSlug-v1', 'Move', .65),
    'M05': ('M05-AcidSlug-v1', 'Move_Loop', .65),
    'M06': ('M06-IceSlug-v1', 'Move', .65),
    'M07': ('M07-Soldier-v1', 'Move_Loop', .75),
    'M08': ('M08-Archer-v1', 'Move_Loop', .75),
    'M09': ('M09-Caster-v1', 'Move_Loop', .65),
    'M10': ('M10-Defender-v1', 'Move_Loop', .55),
}

def portrait_from_idle(frame):
    """Keep the existing character; trim transparent padding and fit its silhouette."""
    frame = frame.convert('RGBA')
    bounds = frame.getchannel('A').point(lambda alpha: 255 if alpha > 16 else 0).getbbox()
    if bounds is None:
        raise ValueError('Cannot make a portrait from an empty idle frame')
    subject = frame.crop(bounds)
    scale = 248 / max(subject.size)
    size = tuple(max(1, round(length * scale)) for length in subject.size)
    # Runtime loading compresses textures; both dimensions must align to BC blocks.
    canvas = tuple((length + 8 + 3) // 4 * 4 for length in size)
    portrait = Image.new('RGBA', canvas)
    offset = tuple((outer - inner) // 2 for outer, inner in zip(canvas, size))
    portrait.paste(subject.resize(size, Image.Resampling.LANCZOS), offset)
    return portrait


def refresh_portraits(root):
    """Refresh tracked runtime portraits without the author's raw workspace."""
    for task in PROFILES:
        folder = root / task
        manifest_path = folder / 'manifest.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
        if manifest['version'] != 1 or manifest['task'] != task:
            raise ValueError(f'Unexpected runtime package: {task}')
        idle = next(clip for clip in manifest['clips'] if clip['key'] == 'idle')
        filename = idle['pages'][0]['file']
        if Path(filename).name != filename:
            raise ValueError('Expected an atlas filename within the package')
        records = [record for record in manifest['files'] if record['file'] == 'portrait.png']
        if len(records) != 1:
            raise ValueError(f'Expected one portrait record: {task}')
        with Image.open(folder / filename) as atlas:
            portrait = portrait_from_idle(atlas.crop((0, 0, idle['cell'], idle['cell'])))
        portrait_path = folder / 'portrait.png'
        portrait.save(portrait_path)
        records[0]['sha256'] = hashlib.sha256(portrait_path.read_bytes()).hexdigest()
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
        print(task, portrait.size, 'transparent portrait; animation pages unchanged')


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
    with Image.open(out/clips[0]['pages'][0]['file']) as atlas:
        save(portrait_from_idle(atlas.crop((0, 0, 256, 256))), 'portrait.png')
    with Image.open(source/'frames/Idle/0000.png') as idle:
        bounds=idle.getbbox()
        height=(bounds[3]-bounds[1])*.4/80+.15
    manifest={'version':1,'task':task,'ppu':80,'referenceSpeed':speed,'height':height,'clips':clips,'files':records,
              'provenance':'Approved local preview '+folder+'; source and rights records remain beside it. No public redistribution license confirmed.'}
    (out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2) + '\n',encoding='utf-8',newline='\n')
    print(task, sum(c['count'] for c in clips),'frames',len(records),'local PNGs')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--previews',type=Path)
    parser.add_argument('--output',type=Path)
    parser.add_argument('--task',choices=PROFILES)
    parser.add_argument('--refresh-portraits', type=Path, metavar='RUNTIME_ROOT',
                        help='Rebuild M02-M10 portraits from their tracked idle atlases')
    args=parser.parse_args()
    if args.refresh_portraits:
        if args.previews or args.output or args.task:
            parser.error('--refresh-portraits cannot be combined with packaging arguments')
        refresh_portraits(args.refresh_portraits)
    else:
        if not (args.previews and args.output and args.task):
            parser.error('Packaging requires --previews, --output and --task')
        package(args.previews,args.output,args.task)
