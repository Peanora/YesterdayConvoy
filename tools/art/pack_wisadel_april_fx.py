"""Losslessly package native game#9 captures into a standalone runtime staging tree."""
from pathlib import Path
import argparse, hashlib, json, math, sys, uuid

parser=argparse.ArgumentParser()
parser.add_argument('--capture',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--project',type=Path,default=Path.cwd())
args=parser.parse_args()
sys.stdout.reconfigure(encoding='utf8')
from PIL import Image,ImageChops

capture,output,project=args.capture.resolve(),args.output.resolve(),args.project.resolve()
audit=json.loads((capture/'SOURCE-AUDIT.json').read_text(encoding='utf8'))
metadata={p['key']:p for p in json.loads((capture/'native-cache/metadata.json').read_text(encoding='utf8'))['prefabs']}
task='WIS_APRIL_G9'
art=output/'Assets/Art/VFX/BossRoster'/task
data=output/'Assets/Data/ArtCatalogs/BossRosterFx'/task
namespace=uuid.UUID('31ba0112-8ab6-42e8-98ab-c2ebcf05f2bc')
template=(project/'Assets/Art/VFX/BossRoster/B22/enemy_mirbst_a_attack_01_start/00.png.meta').read_text(encoding='utf8')
def meta(path):
    import re
    rel=path.relative_to(output).as_posix();guid=uuid.uuid5(namespace,rel).hex
    if path.suffix=='.png':
        text=re.sub(r'^guid: [0-9a-f]+','guid: '+guid,template,flags=re.M)
        text=text.replace('sRGBTexture: 1','sRGBTexture: 0')
    else:text='fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' if path.is_dir() else '')
    Path(str(path)+'.meta').write_text(text,encoding='utf8')
def directory(path):
    path.mkdir(parents=True,exist_ok=True)
    current=path
    while current!=output/'Assets':
        meta(current);current=current.parent
def write(path,value):
    directory(path.parent);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8');meta(path)
def box(image):
    red,green,blue,alpha=image.split()
    return ImageChops.lighter(ImageChops.lighter(red,green),ImageChops.lighter(blue,alpha)).getbbox()
clips=[];provenance=[];failures=[]
for key in audit['keys']:
    folder=capture/'frames'/key
    if not (folder/'report.json').exists():failures.append((key,'missing'));continue
    qa=json.loads((folder/'report.json').read_text(encoding='utf8'))
    if qa['status']!='PASS':failures.append((key,qa['status']));continue
    frames=[Image.open(folder/f'{i:04d}-hdr.png').convert('RGBA') for i in range(qa['frames'])]
    boxes=[box(f) for f in frames];visible=[b for b in boxes if b]
    if not visible:failures.append((key,'empty'));continue
    size=qa['size'];px=qa['pivotX']*size;py=(1-qa['pivotY'])*size
    left=max(0,math.floor(min([px]+[b[0]for b in visible])-4));top=max(0,math.floor(min([py]+[b[1]for b in visible])-4))
    right=min(size,math.ceil(max([px]+[b[2]for b in visible])+4));bottom=min(size,math.ceil(max([py]+[b[3]for b in visible])+4))
    width=math.ceil((right-left)/4)*4;height=math.ceil((bottom-top)/4)*4
    fields=next((json.loads(s['fields'])for n in metadata[key]['nodes']for s in n.get('scripts',[])if s['type']in ['ParticleEffect','ProjectileEffect']),{})
    loop=bool(fields.get('_isLoop',False)) and boxes[-1] is not None
    count=len(frames)if loop else min(len(frames),max(i for i,b in enumerate(boxes)if b)+3)
    columns=max(1,min(12,2048//width));per_page=columns*(2048//height)
    if per_page<=0:raise ValueError('Oversized native frame: '+key)
    pages=[];directory(art/key)
    for first in range(0,count,per_page):
        n=min(per_page,count-first);page=Image.new('RGBA',(columns*width,math.ceil(n/columns)*height))
        for i in range(n):page.paste(frames[first+i].crop((left,top,right,bottom)),((i%columns)*width,(i//columns)*height))
        dest=art/key/f'{len(pages):02d}.png';page.save(dest,optimize=True);meta(dest)
        pages.append(dict(file=key+'/'+dest.name,count=n,width=page.width,height=page.height))
    # Same world conversion as the new original body: .0045/.0027. The native
    # effects do NOT inherit FaceSwitcher's .27 (FollowMountPoint.followScale=0).
    native_to_game=.0045/.0027
    clips.append(dict(key=key,count=count,width=width,height=height,columns=columns,ppu=qa['pixelsPerWorldUnit']/native_to_game,
        pivotX=(px-left)/width,pivotY=1-(py-top)/height,duration=count/qa['fps'],hdrRange=qa['hdrRange'],loop=loop,sourceStart=0,pages=pages))
    provenance.append(dict(key=key,sourceReport=qa,sourceObjectFields=fields,crop=[left,top,right,bottom],
        frameSha256=[hashlib.sha256((folder/f'{i:04d}-hdr.png').read_bytes()).hexdigest()for i in range(count)],
        adaptation='Original native shader HDR capture; shared crop and original root pivot. Fixed 2x is applied only at runtime. Projectile camera tracks its 10 native-unit/s path.'))
    print('PACKED',key,count,'frames',len(pages),'pages',flush=True)
if failures:raise RuntimeError('Capture gaps: '+str(failures))
write(data/'manifest.json',dict(version=1,task=task,fps=30,encoding='sqrt-linear-rgb-effective-alpha',clips=clips,
    preload=[dict(phase=0,keys=[c['key']for c in clips])],bindings=[]))
write(data/'bindings.json',dict(version=1,task=task,bindings=[],
    note='FormationLegacyEffects owns real action/impact/state routing. This package must never enter B20 sale#14 native owner routing.'))
muzzles=capture.parent/'WIS-source/source-muzzles.json'
if muzzles.exists():write(data/'ORIGINAL-MUZZLES.json',json.loads(muzzles.read_text(encoding='utf8')))
write(data/'SOURCE-RECORD.json',dict(version=1,task=task,skin='char_1035_wisdel@game#9',
    sourceVersion=audit['version'],sourceBundle='battle/prefabs/effects/wisdel.ab',sourceBundleSha256=audit['sourceBundleSha256'],
    replacements=audit['replacements'],nativeToGame=.0045/.0027,clips=provenance,
    rights='Original Hypergryph media; redistribution permission unconfirmed. User approved integration, not a rights-holder license.',
    limitations=['Native Torappu game scripts are not present; mount/lifecycle are supplied by real Formation events.',
        'No separate S1/S2 skin replacement exists: these clips intentionally use default wisdel sources.',
        'S3 original game#9 explosion uses original fruit particles; old user-GIF six faces are not substituted.',
        'Original native S3 token-spawn, camouflage and birth sources are packaged for provenance; they are not fake-triggered when this ruleset has no corresponding event.']))
print('COMPLETE',len(clips),'clips',sum(len(c['pages'])for c in clips),'pages',flush=True)
