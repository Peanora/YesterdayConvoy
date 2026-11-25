"""Package the user-approved Steam Knight V2 original skeleton and isolated FX bake.

Only decoded frames and original source metadata enter the runtime project. The original
client, raw bundles, tools and external preview directories are never runtime dependencies.
"""
from pathlib import Path
import argparse,json,uuid,hashlib,re
from PIL import Image,ImageChops
parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--fx',type=Path)
args=parser.parse_args();PROJECT=Path(__file__).resolve().parents[2]
def write_json(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def meta(p):
 m=p.with_name(p.name+'.meta')
 if m.exists():return
 guid=uuid.uuid5(uuid.NAMESPACE_URL,'YesterdayConvoy/'+p.relative_to(PROJECT).as_posix()).hex
 if p.is_dir():text=f'fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 elif p.suffix=='.png':
  template=PROJECT/'Assets/Art/Characters/Monsters/BossRoster/B22/main/Attack-000.png.meta'
  text=re.sub(r'(?m)^guid: .*$', 'guid: '+guid,template.read_text(encoding='utf-8'))
 else:text=f'fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 m.write_text(text,encoding='utf-8')
def tree_meta(root):
 p=root
 while p!=PROJECT/'Assets':meta(p);p=p.parent
 for p in sorted(root.rglob('*')):
  if p.suffix!='.meta':meta(p)
def pages(frames,out,prefix,cell,columns,crop=None):
 out.mkdir(parents=True,exist_ok=True);result=[];capacity=columns*columns
 for first in range(0,len(frames),capacity):
  count=min(capacity,len(frames)-first);height=((count+columns-1)//columns)*cell
  image=Image.new('RGBA',(columns*cell,height))
  for i,p in enumerate(frames[first:first+count]):
   with Image.open(p) as frame:image.paste(frame.crop(crop) if crop else frame,((i%columns)*cell,(i//columns)*cell))
  file=f'{prefix}-{first//capacity:03}.png';image.save(out/file)
  result.append({'file':file,'count':count,'width':image.width,'height':image.height})
 return result

def effect_crop(frames):
 # Include additive RGB even where alpha is zero, and keep the original anchor.
 union=None
 for p in frames:
  with Image.open(p) as im:
   mask=im.getchannel('R')
   for channel in ['G','B','A']:mask=ImageChops.lighter(mask,im.getchannel(channel))
   union=mask if union is None else ImageChops.lighter(union,mask)
 x0,y0,x1,y1=union.getbbox() or (256,256,257,257)
 x0=min(x0,256);y0=min(y0,256);x1=max(x1,257);y1=max(y1,257)
 size=min(512,max(64,((max(x1-x0,y1-y0)+7)//4)*4))
 left=max(0,min(512-size,(x0+x1-size)//2));top=max(0,min(512-size,(y0+y1-size)//2))
 return (left,top,left+size,top+size),size,(256-left)/size,1-(256-top)/size
source=args.source
if not args.fx:
 capture=json.loads((source/'reference/body-capture.json').read_text(encoding='utf-8'))
 aerial=json.loads((source/'reference/aerial-capture.json').read_text(encoding='utf-8'))
 images=PROJECT/'Assets/Art/Characters/Monsters/BossRoster/STEAM/main';data=PROJECT/'Assets/Data/ArtCatalogs/BossRoster/STEAM/main'
 clips=[]
 for c in capture['clips']:
  folder=source/('actor-frames-aerial' if c['name'].startswith('Skill_2_') else 'actor-frames-v2')/c['name']
  if not folder.exists():continue
  count=round(c['duration']*30);frames=[folder/f'{i:04}.png' for i in range(count)]
  clip={'key':c['name'],'source':c['name'],'count':count,'duration':count/30,'fps':30,'loop':c['name'].startswith(('Idle_','Move_','Stun_')),'cell':768,'columns':2,'pivotX':c['pivotX'],'pivotY':c['pivotY'],'pixelsPerUnit':c['ppu'],'events':c['events'],'presentationTop':2.95}
  if c['name'].startswith('Skill_2_'):clip.update({k:aerial[k] for k in ['pivotX','pivotY']});clip['pixelsPerUnit']=aerial['ppu']
  clip['pages']=pages(frames,images,c['name'],768,2);segments=[];at=0
  for first in range(0,len(clip['pages']),4):
   ps=clip['pages'][first:first+4];n=sum(p['count'] for p in ps);segments.append({'firstFrame':at,'count':n,'pages':ps});at+=n
  clip['segments']=segments;clips.append(clip);print('BODY',c['name'],count,len(clip['pages']),flush=True)
 aliases={'idle':'Idle_A','walk':'Move_A','attack':'Attack_A','cast':'Skill_1_Begin_A','death':'Die_B'}
 manifest={'version':1,'task':'STEAM','cardId':'CARD_STEAM','name':'蒸汽骑士','view':'main','model':'enemy_1533_stmkgt','skin':'default','ppu':clips[0]['pixelsPerUnit'],'height':2.95,'referenceSpeed':.6,'aliases':[{'key':k,'source':v} for k,v in aliases.items()],'runtimeSources':list(aliases.values()),'clips':clips,'zeroDurationStates':[]}
 write_json(data/'manifest.json',manifest)
 with Image.open(source/'actor-frames-v2/Idle_A/0000.png') as im:im.save(images/'portrait.png')
 mounts=json.loads((source/'reference/mounts.json').read_text(encoding='utf-8'));write_json(PROJECT/'Assets/Data/Resources/SteamKnight/mounts.json',mounts)
 tree_meta(images);tree_meta(data);tree_meta(PROJECT/'Assets/Data/Resources/SteamKnight')
else:
 capture=json.loads((args.fx/'capture.json').read_text(encoding='utf-8'));clips=[]
 images=PROJECT/'Assets/Art/VFX/BossRoster/STEAM';data=PROJECT/'Assets/Data/ArtCatalogs/BossRosterFx/STEAM'
 for c in capture['clips']:
  frames=[args.fx/c['key']/f'{i:04}.png' for i in range(c['frames'])]
  crop,cell,pivotX,pivotY=effect_crop(frames);columns=min(8,2048//cell)
  folder=images/c['key'];ps=pages(frames,folder,'page',cell,columns,crop)
  retained={p['file'] for p in ps}
  for stale in folder.glob('page-*.png'):
   if stale.name not in retained:
    stale.unlink();stale.with_name(stale.name+'.meta').unlink(missing_ok=True)
  for p in ps:p['file']=c['key']+'/'+p['file']
  clips.append({'key':c['key'],'count':c['frames'],'width':cell,'height':cell,'columns':columns,'duration':c['duration'],'ppu':c['ppu'],'pivotX':pivotX,'pivotY':pivotY,'hdrRange':c['hdrRange'],'sourceStart':0,'loopPeriod':c['duration'] if c['loop'] else 0,'loop':c['loop'],'pages':ps})
  print('FX',c['key'],c['frames'],len(ps),flush=True)
 active=[c['key'] for c in clips if not c['key'].startswith('reborn_') and c['key']!='skill_02_hit_02']
 write_json(data/'manifest.json',{'version':1,'task':'STEAM','fps':30,'encoding':capture['encoding'],'clips':clips,'bindings':[],'preload':[{'phase':0,'keys':active}]})
 tree_meta(images);tree_meta(data)
print('STEAM_NATIVE_INSTALL_PASS')
