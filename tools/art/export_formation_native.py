from pathlib import Path
import sys,json,hashlib,re,math,argparse
parser=argparse.ArgumentParser(description='Export checked LS-5 source renderer submeshes into portable FormationNative JSON and PNGs.')
parser.add_argument('--runtime',type=Path,default=Path('E:/珍贵资料/昨日圆车_美术预览/2026-11-05_泥岩原作技能-v1'),help='Existing directory containing tools-python/UnityPy and external-tools/aklz4.py')
parser.add_argument('--source-cache',type=Path,default=None)
parser.add_argument('--output',type=Path,default=None)
args=parser.parse_args()
TOOLS=args.runtime
sys.path[:0]=[str(TOOLS/'tools-python'),str(TOOLS/'external-tools')]
import aklz4,UnityPy,numpy as np
from UnityPy.helpers.MeshHelper import MeshHandler
from PIL import Image
PROJECT=Path(__file__).resolve().parents[2]
ROOT=PROJECT/'artifacts/formation-native-source'
SOURCE=args.source_cache or ROOT/'original-bundles'
OUT=args.output or PROJECT/'Assets/Art/Environment/FormationNative'
OUT.mkdir(parents=True,exist_ok=True)
env=UnityPy.load(str(SOURCE))

def vec(v):return [float(v.x),float(v.y),float(v.z)]
def quat(v):return [float(v.x),float(v.y),float(v.z),float(v.w)]
def col(v):return [float(v.r),float(v.g),float(v.b),float(v.a)]
def v2(v):return [float(v.x),float(v.y)]
def rounded(a):return [[round(float(v),7) for v in row] for row in a]
def flat(a):return [round(float(v),7) for row in a for v in row]
def objref(o):return {'file':o.assets_file.name,'pathId':str(o.path_id)}
def refkey(o):return o.assets_file.name+':'+str(o.path_id)
def matrix(t):
 x,y,z,w=quat(t.m_LocalRotation)
 r=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])
 m=np.eye(4);m[:3,:3]=r@np.diag(vec(t.m_LocalScale));m[:3,3]=vec(t.m_LocalPosition)
 if t.m_Father.m_PathID:m=matrix(t.m_Father.read())@m
 return m
C=np.array([[1,0,0],[0,0,-1],[0,1,0]])
textures=[]; tex_names={};materials=[];mat_names={};parts=[]
for o in env.objects:
 if o.type.name!='Texture2D':continue
 t=o.read(); name=t.m_Name; image=t.image
 if name.endswith('_N'):
  # These Android ASTC normals are already RGB tangent normals (alpha uniformly 255).
  image=image.convert('RGB')
 elif name=='Lightmap-0_comp_light':
  # PIL's decoded RGBM preserves original encoded lightmap channels; mark as raw data.
  image=image.convert('RGBA')
 folder=OUT/'Textures';folder.mkdir(exist_ok=True)
 filename=name+'.png';image.save(folder/filename)
 tex_names[refkey(o)]=filename
 textures.append({'name':name,'file':'Textures/'+filename,'width':image.width,'height':image.height,'normalMap':name.endswith('_N'),'linear':name.endswith(('_M','_N')) or name.startswith('Lightmap-'),'source':objref(o),'sha256':hashlib.sha256((folder/filename).read_bytes()).hexdigest(),'sourceFormat':int(t.m_TextureFormat),'normalAdaptation':'Original decoded tangent RGB; opaque alpha omitted' if name.endswith('_N') else ''})
for o in env.objects:
 if o.type.name!='Material':continue
 t=o.read();row={'name':t.m_Name,'key':refkey(o),'source':objref(o),'textures':[],'floats':[],'colors':[],'keywords':list(t.m_ValidKeywords or []),'queue':t.m_CustomRenderQueue}
 for name,x in t.m_SavedProperties.m_TexEnvs:
  if not x.m_Texture.m_PathID:continue
  try:filename=tex_names[refkey(x.m_Texture.deref())]
  except Exception:filename=''
  row['textures'].append({'property':name,'file':'Textures/'+filename if filename else '', 'scale':v2(x.m_Scale),'offset':v2(x.m_Offset),'pathId':str(x.m_Texture.m_PathID)})
 row['floats']=[{'name':n,'value':float(v)}for n,v in t.m_SavedProperties.m_Floats]
 row['colors']=[{'name':n,'value':col(v)}for n,v in t.m_SavedProperties.m_Colors]
 materials.append(row);mat_names[refkey(o)]=t.m_Name
handlers={}
scene_files={o.assets_file for o in env.objects if o.type.name=='Camera'}
for o in env.objects:
 if o.type.name!='MeshRenderer':continue
 r=o.read();go=r.m_GameObject.read();tr=next(x.component.read() for x in go.m_Component if x.component.type.name=='Transform');mf=next(x.component.read() for x in go.m_Component if x.component.type.name=='MeshFilter')
 if not mf.m_Mesh.m_PathID:continue
 if o.assets_file not in scene_files:continue
 mesh_obj=mf.m_Mesh.deref();mesh=mesh_obj.read();key=refkey(mesh_obj)
 if key not in handlers:
  h=MeshHandler(mesh);h.process();handlers[key]=(h,h.get_triangles())
 h,subtris=handlers[key];batch=r.m_StaticBatchInfo
 selected=list(range(batch.firstSubMesh,batch.firstSubMesh+batch.subMeshCount))if batch.subMeshCount else list(range(len(subtris)))
 all_ids=sorted(set(i for si in selected for tri in subtris[si] for i in tri));remap={v:i for i,v in enumerate(all_ids)}
 positions=np.asarray([h.m_Vertices[i]for i in all_ids]);normals=np.asarray([h.m_Normals[i][:3]for i in all_ids])if h.m_Normals else None
 w=matrix(tr)
 if not batch.subMeshCount:
  positions=(w[:3,:3]@positions.T).T+w[:3,3]
  if normals is not None:normals=(np.linalg.inv(w[:3,:3]).T@normals.T).T
 positions=(C@positions.T).T
 if normals is not None:
  normals=(C@normals.T).T;length=np.linalg.norm(normals,axis=1);normals/=np.maximum(length[:,None],1e-8)
 lo=positions.min(axis=0);hi=positions.max(axis=0);pivot=np.array([(lo[0]+hi[0])/2,0,(lo[2]+hi[2])/2]);positions-=pivot
 slots=[]
 for p in r.m_Materials:
  try:slots.append(mat_names[refkey(p.deref())])
  except Exception:slots.append('MT_Qcity_BG'if go.m_Name=='BG_Qcity'else 'UNRESOLVED:'+str(p.m_PathID))
 if len(selected)!=len(slots):raise ValueError((go.m_Name,len(selected),slots))
 row={'id':len(parts),'name':go.m_Name,'renderer':objref(o),'sourceMesh':objref(mesh_obj),'sourceMeshName':mesh.m_Name,'staticBatchFirst':int(batch.firstSubMesh),'staticBatchCount':int(batch.subMeshCount),'sourceTransform':{'position':vec(tr.m_LocalPosition),'rotation':quat(tr.m_LocalRotation),'scale':vec(tr.m_LocalScale),'worldMatrix':w.flatten().tolist()},'position':rounded([pivot])[0],'boundsMin':rounded([lo])[0],'boundsMax':rounded([hi])[0],'vertices':flat(positions),'normals':flat(normals)if normals is not None else [],'uv':flat([h.m_UV0[i]for i in all_ids])if h.m_UV0 else [],'uv2':flat([h.m_UV1[i]for i in all_ids])if h.m_UV1 else [],'colors':flat([h.m_Colors[i]for i in all_ids])if h.m_Colors else [],'submeshes':[{'triangles':[remap[i] for tri in subtris[si]for i in tri],'material':slots[j]}for j,si in enumerate(selected)],'lightmapIndex':int(r.m_LightmapIndex),'lightmapScaleOffset':[float(r.m_LightmapTilingOffset.x),float(r.m_LightmapTilingOffset.y),float(r.m_LightmapTilingOffset.z),float(r.m_LightmapTilingOffset.w)],'enabled':bool(r.m_Enabled),'castShadows':int(r.m_CastShadows),'receiveShadows':bool(r.m_ReceiveShadows)}
 parts.append(row)
scene_settings=[]
for o in env.objects:
 if o.type.name not in ('Camera','Light','RenderSettings','LightmapSettings'):continue
 row={'type':o.type.name,'source':objref(o),'data':o.read_typetree()}
 if o.type.name in ('Camera','Light'):
  v=o.read();go=v.m_GameObject.read();tr=next(x.component.read()for x in go.m_Component if x.component.type.name=='Transform');row['name']=go.m_Name;row['worldMatrix']=matrix(tr).flatten().tolist();row['localPosition']=vec(tr.m_LocalPosition);row['localRotation']=quat(tr.m_LocalRotation)
 scene_settings.append(row)
index={'version':1,'sourceGame':'Arknights','sourceStage':'LS-5 / level_weekly_killcost_5','resourceVersion':'26-10-29-15-36-11-33649006','coordinateConversion':'source x,y,z to project x,-z,y; static-batch vertices already source world positions; each part recentered only in ground x/z','textures':textures,'materials':materials,'parts':parts}
(OUT/'ls5-native.json').write_text(json.dumps(index,ensure_ascii=False,separators=(',',':')),encoding='utf8')
(OUT/'ls5-scene-settings.json').write_text(json.dumps(scene_settings,ensure_ascii=False,indent=2),encoding='utf8')
(ROOT/'part-summary.json').write_text(json.dumps([{k:p[k]for k in ('id','name','position','boundsMin','boundsMax','staticBatchFirst','lightmapScaleOffset')}for p in parts],ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({'parts':len(parts),'textures':len(textures),'materials':len(materials),'vertices':sum(len(x['vertices'])//3 for x in parts),'unresolved':[x['name']for x in parts if any(s['material'].startswith('UNRESOLVED')for s in x['submeshes'])],'out':str(OUT)}))
