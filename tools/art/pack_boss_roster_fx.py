"""Pack approved native HDR frames without recoloring or modifying their RGB/alpha encoding."""
from pathlib import Path
import argparse,hashlib,json,math,sys,uuid

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True,type=Path)
parser.add_argument('--project',type=Path,default=Path.cwd())
parser.add_argument('--tasks',default='B22')
parser.add_argument('--python-libs',type=Path)
args=parser.parse_args()
if args.python_libs:sys.path.insert(0,str(args.python_libs))
from PIL import Image,ImageChops
sys.stdout.reconfigure(encoding='utf8')

project=args.project.resolve();source=args.source.resolve();tasks=args.tasks.split(',')
image_root=project/'Assets/Art/VFX/BossRoster';data_root=project/'Assets/Data/ArtCatalogs/BossRosterFx'
samples=json.loads((source/'ALL-EXACT-FX-SAMPLES.json').read_text(encoding='utf8'))['entries']
metadata={m['key']:m for m in json.loads((source/'native-cache/metadata.json').read_text(encoding='utf8'))['prefabs']}
guid_namespace=uuid.UUID('7fed063b-e5f9-4d7d-8a65-bd99c3ad915e')

def meta(path):
    target=Path(str(path)+'.meta')
    if target.exists():return
    guid=uuid.uuid5(guid_namespace,path.relative_to(project).as_posix()).hex
    template=project/'Assets/Art/VFX/BossRoster/B22/enemy_mirbst_a_attack_01_start/00.png.meta'
    if path.suffix.lower()=='.png'and template.exists():
        import re
        target.write_text(re.sub(r'^guid: [0-9a-f]+','guid: '+guid,template.read_text(encoding='utf8'),flags=re.M),encoding='utf8');return
    target.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'if path.is_dir()else ''),encoding='utf8')

def directory(path):
    path.mkdir(parents=True,exist_ok=True)
    current=path
    while current!=project/'Assets' and current.is_relative_to(project/'Assets'):
        meta(current);current=current.parent

def write_json(path,data):
    directory(path.parent);path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8');meta(path)

def bounds(frame):
    red,green,blue,alpha=frame.split()
    return ImageChops.lighter(ImageChops.lighter(red,green),ImageChops.lighter(blue,alpha)).getbbox()

def pack(task):
    records=[e for e in samples if task in e['tasks'] and e['status']=='PASS'];clips=[];provenance=[]
    for entry in records:
        key=entry['key'];folder=source/entry['folder'];qa=json.loads((folder/'report.json').read_text(encoding='utf8'))
        assert qa['status']=='PASS' and qa['edgePixels']==qa['clippedPixels']==0
        frames=[];boxes=[];hashes=[]
        for i in range(qa['frames']):
            file=folder/f'{i:04d}-hdr.png';frame=Image.open(file).convert('RGBA');frames.append(frame);boxes.append(bounds(frame));hashes.append(hashlib.sha256(file.read_bytes()).hexdigest())
        ppu=qa['pixelsPerWorldUnit'];size=qa['size'];px=qa['pivotX']*size;py=(1-qa['pivotY'])*size
        nonempty=[b for b in boxes if b];left=max(0,math.floor(min([px]+[b[0]for b in nonempty])-4));top=max(0,math.floor(min([py]+[b[1]for b in nonempty])-4))
        right=min(size,math.ceil(max([px]+[b[2]for b in nonempty])+4));bottom=min(size,math.ceil(max([py]+[b[3]for b in nonempty])+4))
        width=math.ceil((right-left)/4)*4;height=math.ceil((bottom-top)/4)*4
        source_fields=next((json.loads(s['fields'])for n in metadata[key]['nodes']for s in n.get('scripts',[])if s['type'] in ['ParticleEffect','ProjectileEffect']),{})
        # The source holder's loop flag alone is insufficient: a finished visual is never re-emitted.
        loop=bool(source_fields.get('_isLoop',False)) and boxes[-1] is not None
        if 'trail' in key and boxes[-1] is not None:loop=True
        count=len(frames)if loop else min(len(frames),max(i for i,b in enumerate(boxes)if b)+3)
        columns=min(12,2048//width);per_page=columns*(2048//height);pages=[];out=image_root/task/key;directory(out)
        for first in range(0,count,per_page):
            n=min(per_page,count-first);page=Image.new('RGBA',(columns*width,math.ceil(n/columns)*height))
            for i in range(n):page.paste(frames[first+i].crop((left,top,right,bottom)),((i%columns)*width,(i//columns)*height))
            file=out/f'{len(pages):02d}.png';page.save(file,optimize=True);meta(file)
            pages.append(dict(file=key+"/"+file.name,count=n,width=page.width,height=page.height))
        clip=dict(key=key,count=count,width=width,height=height,columns=columns,ppu=ppu,pivotX=(px-left)/width,pivotY=1-(py-top)/height,duration=count/qa['fps'],hdrRange=qa['hdrRange'],loop=loop,sourceStart=entry.get('sourceDelay',0),pages=pages)
        clips.append(clip);provenance.append(dict(key=key,sourceFolder=str(folder),sourceFrames=qa['frames'],runtimeFrames=count,sourceFrameSha256=hashes,sourceReport=qa,sourceObjectFields=source_fields,sourceStart=entry.get('sourceDelay',0),crop=[left,top,right,bottom],adaptation='Lossless HDR bytes; shared crop including original anchor; empty one-shot tail removed. Repeated state layers use a bounded sampled window and still require the original state end event.'))
        print(task,key,'frames',count,'pages',len(pages),flush=True)
    if task!='B22':
        write_json(data_root/task/'manifest.json',dict(version=1,task=task,fps=30,encoding='sqrt-linear-rgb-effective-alpha',clips=clips,preload=[dict(phase=0,keys=[c['key']for c in clips])]))
        write_json(data_root/task/'SOURCE-RECORD.json',dict(version=1,task=task,sourceVersion='26-10-29-15-36-11-33649006',sourceBinding='Exact Actor/Projectile/BuffTemplate closure',rights='Original Hypergryph media; redistribution permission unconfirmed. User approved project integration, not a rights-holder license.',clips=provenance))
        print('PACKED',task,len(clips),'clips',sum(len(c['pages'])for c in clips),'pages',flush=True)
        return
    prefix='enemy_mirbst_';attack=[prefix+'a_attack_01_'+s for s in ['start','start_02','hit','hit_02']]
    phase2=[c['key']for c in clips if c['key'].startswith(prefix+'b_')]
    write_json(data_root/task/'manifest.json',dict(version=1,task=task,fps=30,encoding='sqrt-linear-rgb-effective-alpha',clips=clips,preload=[dict(phase=0,keys=[c['key']for c in clips]),dict(phase=2,keys=phase2)]))
    bindings=[]
    def bind(kind,skill,key,attach='owner',phase=-1,hitIndex=-1,loop=False,finish=False):
        bindings.append(dict(kind=kind,skill=skill,key=key,attach=attach,phase=phase,hitIndex=hitIndex,loop=loop,finishOnEnd=finish,offsetX=0,offsetY=0,offsetZ=0))
    for k in attack[:2]:bind('AttackStart','attack',k,phase=0)
    for i,k in enumerate(attack[2:]):bind('Impact','attack',k,attach='target',phase=0,hitIndex=i)
    bind('AttackStart','attack',prefix+'b_attack_01_start',phase=2)
    bind('Impact','attack',prefix+'b_attack_01_hit',attach='target',phase=2)
    bind('SkillStart','rock',prefix+'b_skill_01_start',phase=2)
    bind('SkillStart','rock',prefix+'b_skill_01_start_02',attach='target',phase=2,loop=True,finish=True)
    bind('ProjectileSpawn','rock',prefix+'b_skill_01_trail',attach='projectile',phase=2,loop=True,finish=True)
    bind('Impact','rock',prefix+'b_skill_01_hit',attach='target',phase=2)
    bind('StateStart','flee','common_invincible_03',loop=True,finish=True)
    for raw in ['buff_02_1','buff_02_2','buff_02_3','buff_03']:bind('StateStart','rage',prefix+raw,loop=True,finish=True)
    write_json(data_root/task/'bindings.json',dict(version=1,task=task,bindings=bindings,note='B22 representative native Actor evidence. Actual simulation events provide activation/hit index/state lifetime; no damage is generated by these mappings.'))
    write_json(data_root/task/'SOURCE-RECORD.json',dict(version=1,task=task,sourceVersion='26-10-29-15-36-11-33649006',sourceBinding='COMPLETE-NATIVE-BINDINGS.json / enemy_1571_mirbst',rights='Original Hypergryph media; redistribution authorization unconfirmed. User approved project integration; this is not a rights-holder license.',clips=provenance))
    print('PACKED',task,len(clips),'clips',sum(len(c['pages'])for c in clips),'pages',flush=True)

for task in tasks:
    pack(task)
