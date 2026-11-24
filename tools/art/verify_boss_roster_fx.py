"""Independent source-byte, provenance hash, atlas/anchor and embedded Player-binding verification."""
from pathlib import Path
import argparse,hashlib,json,math,sys
p=argparse.ArgumentParser();p.add_argument('--python-libs',type=Path,required=True);p.add_argument('--output',type=Path,default=Path('Library/BossRosterFxEvidence/source-byte-qa.json'));a=p.parse_args();sys.path.insert(0,str(a.python_libs))
from PIL import Image,ImageChops
root=Path('Assets/Data/ArtCatalogs/BossRosterFx');image_root=Path('Assets/Art/VFX/BossRoster');result={'packages':[],'clips':0,'frames':0,'pages':0,'sourceHashes':0,'status':'PASS'}
for folder in sorted(p for p in root.glob('B*')if p.is_dir()):
 manifest=json.loads((folder/'manifest.json').read_text(encoding='utf8'));source=json.loads((folder/'SOURCE-RECORD.json').read_text(encoding='utf8'));records={c['key']:c for c in source['clips']};keys=set();frames=pages=texture_bytes=0
 assert manifest['encoding']=='sqrt-linear-rgb-effective-alpha'and manifest['fps']==30
 assert manifest['bindings']==json.loads((folder/'bindings.json').read_text(encoding='utf8'))['bindings'],'Player manifest bindings differ from Editor sidecar'
 for clip in manifest['clips']:
  key=clip['key'];assert key not in keys;keys.add(key);record=records[key]
  assert clip['width']%4==clip['height']%4==0 and clip['duration']==clip['count']/30
  assert clip['count']==sum(p['count']for p in clip['pages']) and 0<=clip['pivotX']<=1 and 0<=clip['pivotY']<=1
  assert record['sourceReport']['status']=='PASS'and record['sourceReport']['edgePixels']==record['sourceReport']['clippedPixels']==0
  offset=0
  for page in clip['pages']:
   file=image_root/folder.name/page['file'];assert file.exists()and Path(str(file)+'.meta').exists();atlas=Image.open(file).convert('RGBA')
   assert atlas.size==(page['width'],page['height'])and max(atlas.size)<=2048
   assert page['width']==clip['columns']*clip['width'] and page['height']%clip['height']==0
   texture_bytes+=page['width']*page['height'];pages+=1
   for i in range(page['count']):
    source_file=Path(record['sourceFolder'])/f'{offset+i:04d}-hdr.png';raw=source_file.read_bytes()
    assert hashlib.sha256(raw).hexdigest()==record['sourceFrameSha256'][offset+i],str(source_file)
    src=Image.open(source_file).convert('RGBA').crop(record['crop']);wanted=Image.new('RGBA',(clip['width'],clip['height']));wanted.paste(src,(0,0))
    x=(i%clip['columns'])*clip['width'];y=(i//clip['columns'])*clip['height'];cell=atlas.crop((x,y,x+clip['width'],y+clip['height']))
    assert all(v==0 for pair in ImageChops.difference(cell,wanted).getextrema()for v in pair),(key,offset+i)
    frames+=1;result['sourceHashes']+=1
   offset+=page['count']
 for binding in manifest['bindings']:
  assert binding['key']in keys
  if binding['loop']:assert next(c for c in manifest['clips']if c['key']==binding['key'])['loop']
 result['packages'].append(dict(task=folder.name,clips=len(keys),frames=frames,pages=pages,dxt5TextureBytes=texture_bytes,bindings=len(manifest['bindings'])));result['clips']+=len(keys);result['frames']+=frames;result['pages']+=pages
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
print('BOSS_FX_SOURCE_BYTES_PASS',len(result['packages']),'packages',result['clips'],'clips',result['frames'],'frames',result['pages'],'pages',flush=True)
