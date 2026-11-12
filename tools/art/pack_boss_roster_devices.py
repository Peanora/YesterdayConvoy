"""Append exact auxiliary native samples without repacking the verified main Boss pages."""
from pathlib import Path
import argparse,hashlib,json,math,sys,uuid
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--python-libs',type=Path,required=True);a=p.parse_args();sys.path.insert(0,str(a.python_libs))
from PIL import Image,ImageChops
project=Path.cwd();source=a.source;guid_ns=uuid.UUID('7fed063b-e5f9-4d7d-8a65-bd99c3ad915e')
def meta(path):
 file=Path(str(path)+'.meta')
 if not file.exists():
  guid=uuid.uuid5(guid_ns,path.relative_to(project).as_posix()).hex
  template=project/'Assets/Art/VFX/BossRoster/B22/enemy_mirbst_a_attack_01_start/00.png.meta'
  if path.suffix.lower()=='.png'and template.exists():
   import re
   file.write_text(re.sub(r'^guid: [0-9a-f]+','guid: '+guid,template.read_text(encoding='utf8'),flags=re.M),encoding='utf8')
  else:file.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'if path.is_dir()else''),encoding='utf8')
def append(task,keys,frames,metadata):
 root=project/'Assets/Data/ArtCatalogs/BossRosterFx'/task;manifest=json.loads((root/'manifest.json').read_text(encoding='utf8'));record=json.loads((root/'SOURCE-RECORD.json').read_text(encoding='utf8'));old={c['key']for c in manifest['clips']}
 md={m['key']:m for m in json.loads(metadata.read_text(encoding='utf8'))['prefabs']}
 for key in keys:
  if key in old:continue
  folder=frames/key;q=json.loads((folder/'report.json').read_text());assert q['status']=='PASS'and q['edgePixels']==q['clippedPixels']==0
  images=[];boxes=[];hashes=[]
  for i in range(q['frames']):
   file=folder/f'{i:04d}-hdr.png';im=Image.open(file).convert('RGBA');images.append(im);r,g,b,alpha=im.split();boxes.append(ImageChops.lighter(ImageChops.lighter(r,g),ImageChops.lighter(b,alpha)).getbbox());hashes.append(hashlib.sha256(file.read_bytes()).hexdigest())
  size=q['size'];px=q['pivotX']*size;py=(1-q['pivotY'])*size;nonempty=[b for b in boxes if b]
  left=max(0,math.floor(min([px]+[b[0]for b in nonempty])-4));top=max(0,math.floor(min([py]+[b[1]for b in nonempty])-4));right=min(size,math.ceil(max([px]+[b[2]for b in nonempty])+4));bottom=min(size,math.ceil(max([py]+[b[3]for b in nonempty])+4));w=math.ceil((right-left)/4)*4;h=math.ceil((bottom-top)/4)*4
  fields=next((json.loads(s['fields'])for n in md[key]['nodes']for s in n.get('scripts',[])if s['type']in ['ParticleEffect','ProjectileEffect']),{});loop=bool(fields.get('_isLoop',False))and boxes[-1]is not None
  if 'trail'in key and boxes[-1]is not None:loop=True
  count=len(images)if loop else min(len(images),max(i for i,b in enumerate(boxes)if b)+3);columns=min(12,2048//w);per=columns*(2048//h);pages=[];out=project/'Assets/Art/VFX/BossRoster'/task/key;out.mkdir(exist_ok=True);meta(out)
  for start in range(0,count,per):
   n=min(per,count-start);atlas=Image.new('RGBA',(columns*w,math.ceil(n/columns)*h))
   for i in range(n):atlas.paste(images[start+i].crop((left,top,right,bottom)),((i%columns)*w,(i//columns)*h))
   file=out/f'{len(pages):02d}.png';atlas.save(file,optimize=True);meta(file);pages.append(dict(file=key+'/'+file.name,count=n,width=atlas.width,height=atlas.height))
  manifest['clips'].append(dict(key=key,count=count,width=w,height=h,columns=columns,ppu=q['pixelsPerWorldUnit'],pivotX=(px-left)/w,pivotY=1-(py-top)/h,duration=count/q['fps'],hdrRange=q['hdrRange'],loop=loop,sourceStart=0,pages=pages));manifest['preload'][0]['keys'].append(key)
  record['clips'].append(dict(key=key,sourceFolder=str(folder),sourceFrames=q['frames'],runtimeFrames=count,sourceFrameSha256=hashes,sourceReport=q,sourceObjectFields=fields,sourceStart=0,crop=[left,top,right,bottom],adaptation='Exact original auxiliary Actor reference. Original HDR RGBA bytes, anchor and PPU preserved; explicit original object state controls presentation.'))
  print(task,key,count,'frames',len(pages),'pages',flush=True)
 (root/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8');(root/'SOURCE-RECORD.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
cannon=json.loads((source/'ADDITIONAL-NATIVE-BINDINGS.json').read_text(encoding='utf8'))['units']['trap_196_cnnon']['refs']
cannon=[k for k in cannon if json.loads((source/'frames/auxiliary'/k/'report.json').read_text())['status']=='PASS']
append('B23',cannon,source/'frames/auxiliary',source/'aux-cache/metadata.json')
append('B14',['enemy_cadesd_start_01','enemy_cadesd_skill_02_start_01','enemy_cadesd_skill_02_start_02'],source/'runtime-additions/P14/frames',source/'runtime-additions/P14/native-cache/metadata.json')

# Optional runtime additions are exact Actor/skin-hook closures. Existing verified pages are never repacked.
hound=source/'runtime-additions/HoundProto'
if (hound/'COMPLETE.json').exists()and(hound/'native-cache/metadata.json').exists():
 keys=json.loads((hound/'COMPLETE.json').read_text(encoding='utf8'))['refs']
 append('B14',keys,hound/'frames',hound/'native-cache/metadata.json')
soul=source/'runtime-additions/Wward'
if (soul/'jobs.json').exists():
 keys=[j['output']for j in json.loads((soul/'jobs.json').read_text(encoding='utf8'))['jobs']if json.loads((soul/'frames'/j['output']/'report.json').read_text(encoding='utf8'))['status']=='PASS']
 append('B20',keys,soul/'frames',soul/'native-cache/metadata.json')
devices=source/'runtime-additions/SceneDevices'
if (devices/'metadata-alias.json').exists():
 rows=json.loads((devices/'metadata-alias.json').read_text(encoding='utf8'))['prefabs']
 for task,prefix in [('B08','trap_220'),('B09','trap_247'),('B11','trap_134'),('B12','enemy_209')]:
  append(task,[r['key']for r in rows if r['key'].startswith(prefix)],devices/'frames',devices/'metadata-alias.json')
  root=project/'Assets/Data/ArtCatalogs/BossRosterFx'/task;manifest=json.loads((root/'manifest.json').read_text(encoding='utf8'));records=json.loads((root/'SOURCE-RECORD.json').read_text(encoding='utf8'))
  for clip in manifest['clips']:
   if clip['key']=='trap_134_condtr__trap_006_antidr_working':clip['loop']=True;clip['loopPeriod']=1.250000238418579
  for record in records['clips']:
   if '__structure'in record['key']:record['adaptation']='Original mesh/UV/albedo/Legacy animation; simplified Unlit/Texture display material because recovered Android Torappu/Scene/Trap/Shadow has no supported Vulkan subshader. Exact captured HDR RGBA retained; native scene shading is not reproduced.'
  (root/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8');(root/'SOURCE-RECORD.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
