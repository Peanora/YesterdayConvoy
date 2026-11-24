from pathlib import Path
import hashlib, io, json, sys, urllib.request, zipfile, argparse

parser=argparse.ArgumentParser(description='Download the four documented original LS-5 bundles to an ignored processing cache, checking source manifest MD5.')
parser.add_argument('--runtime',type=Path,default=Path('E:/珍贵资料/昨日圆车_美术预览/2026-11-05_泥岩原作技能-v1'),help='Existing UnityPy/aklz4 runtime and hot_update_list.json directory')
parser.add_argument('--cache',type=Path,default=None)
args=parser.parse_args()
ROOT = args.cache or Path(__file__).resolve().parents[2]/'artifacts/formation-native-source'
TOOLS = args.runtime
sys.path[:0] = [str(TOOLS/'tools-python'), str(TOOLS/'external-tools')]
import aklz4, UnityPy

manifest = json.loads((TOOLS/'hot_update_list.json').read_text(encoding='utf8'))
info = {x['name']: x for x in manifest['abInfos']}
version = manifest['versionId']
names = [
 'scenes/obt/weekly/level_weekly_killcost_5/level_weekly_killcost_5.ab',
 'arts/maps/map_chernobog_a/res.ab',
 'arts/maps/map_chernobog_a/background_mesh/bg_qcity.ab',
 'scenes/obt/weekly/level_weekly_killcost_5/level_weekly_killcost_5/lightingdata.ab',
]
rows=[]
for name in names:
    p=ROOT/'original-bundles'/name
    p.parent.mkdir(parents=True,exist_ok=True)
    url='https://ak.hycdn.cn/assetbundle/official/Android/assets/'+version+'/'+name.removesuffix('.ab').replace('/','_')+'.dat'
    if not p.exists():
        data=urllib.request.urlopen(url,timeout=45).read()
        if data[:2]==b'PK':
            with zipfile.ZipFile(io.BytesIO(data)) as z:
                data=z.read(name)
        p.write_bytes(data)
    data=p.read_bytes()
    assert hashlib.md5(data).hexdigest()==info[name]['md5'],name
    env=UnityPy.load(str(p))
    counts={}
    objects=[]
    for obj in env.objects:
        counts[obj.type.name]=counts.get(obj.type.name,0)+1
        if obj.type.name in ('Mesh','Material','Texture2D','GameObject','Light','RenderSettings','LightmapSettings','Camera'):
            try:
                v=obj.read()
                objects.append({'type':obj.type.name,'pathId':obj.path_id,'name':getattr(v,'m_Name','')})
            except Exception as ex: objects.append({'type':obj.type.name,'error':str(ex)})
    row={'name':name,'url':url,'file':str(p),'md5':hashlib.md5(data).hexdigest(),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'metadata':info[name],'counts':counts,'containers':list(env.container),'objects':objects}
    rows.append(row)
    print(json.dumps({'name':name,'bytes':len(data),'counts':counts,'containers':list(env.container)[:5],'samples':objects[:6]},ensure_ascii=False),flush=True)
(ROOT/'ACQUISITION.json').write_text(json.dumps({'version':version,'rights':'Original proprietary Arknights media; no open redistribution license established.','files':rows},ensure_ascii=False,indent=2),encoding='utf8')
