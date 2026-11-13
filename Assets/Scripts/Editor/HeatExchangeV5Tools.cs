using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
 // One-time editor authoring of the approved V5 preview. No runtime terrain builder.
 public static class HeatExchangeV5Tools
 {
  public const string ScenePath="Assets/Scenes/Map05HeatExchange.unity";
  const string Art="Assets/Art/Environment/HeatExchangeV5";
  const string Source="Assets/Data/ArtCatalogs/HeatExchangeV5/map-v5.json";
  const string Profile="Assets/Data/Resources/ApprovedMaps/Map05.asset";
  const string Battle="Assets/Data/Resources/ApprovedMaps/Map05Battle.asset";
  [Serializable] public sealed class Transfer {public string[] rows;public int width,height;public float platformHeight;public MeshData[] meshes;public MatData[] materials;public TextureData[] textures;}
  [Serializable] public sealed class MeshData {public int id,material,col,row;public string name,path,gateId;public Vector3 position;public bool isTileTop,castShadow,receiveShadow;public float[] vertices,normals,uv,panelUv,shelterEdges;public int[] triangles;}
  [Serializable] public sealed class MatData {public int id,side;public string name,role,type,colorSRGB,map,normalMap,roughnessMap;public float[] colorLinear,emissiveLinear,normalScale;public float roughness,metalness,opacity,emissiveIntensity;public bool transparent,depthWrite;}
  [Serializable] public sealed class TextureData {public string id,file,colorSpace;public int wrapS,wrapT;}
  [Serializable] sealed class Layouts {public string version;public Layout[] maps;}
  [Serializable] sealed class Layout {public int index,width,height;public string id,title,sceneName;public string[] rows;public Route[] routes;public Guard[] guards;public Portal[] portals;public Cell[] heatCells;}
  [Serializable] sealed class Cell {public int x,y;}
  [Serializable] sealed class Route {public string id,exitId;public Cell[] cells;}
  [Serializable] sealed class Guard {public Cell cell;public bool high;}
  [Serializable] sealed class Portal {public string routeId;public int sourcePointIndex,destinationPointIndex;public float transitSeconds;}

  [MenuItem("Yesterday Convoy/Heat Exchange V5/Apply Approved Preview")]
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring the saved map.");
   for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes first.");
   var source=JsonUtility.FromJson<Transfer>(File.ReadAllText(Source));
   if(source.meshes==null||source.meshes.Length<140||source.meshes.Count(m=>m.isTileTop)!=140)throw new InvalidOperationException("Incomplete approved V5 transfer.");
   Ensure(Art+"/Materials");Ensure(Art+"/Meshes");
   var materials=Materials(source);var meshes=Meshes(source);
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
   var field=Object.FindAnyObjectByType<AuthoredBattlefield>();if(field==null)throw new InvalidOperationException("Saved battlefield missing.");
   foreach(var child in field.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   field.name="Heat exchange station V5 / saved editable battlefield";
   var terrain=Child(field.transform,"Terrain / 14 x 10 editable cells");
   var props=Child(field.transform,"Landmark / 05 / Heat exchange V5 equipment and maintenance courtyard");
   var tiles=new Transform[14,10];
   for(int row=0;row<10;row++)for(int col=0;col<14;col++){
    var t=Child(terrain,((char)('A'+col))+(row+1).ToString("00")+" / "+(source.rows[row][col]=='H'?"small raised service platform":"maintenance paving"));
    t.position=new Vector3(col-6.5f,0,4.5f-row);var tile=t.gameObject.AddComponent<ApprovedMapTile>();tile.cell=new Vector2Int(col,row);tile.kind=source.rows[row][col].ToString();tiles[col,row]=t;
   }
   var gates=new Dictionary<string,Transform>();
   foreach(var id in new[]{"R1","R2","B1"})gates[id]=Child(field.transform,id+(id=="B1"?" / Blue exit":" / Red entry"));
   foreach(var s in source.meshes){
    Transform parent=props;string name=s.name;
    if(s.isTileTop){parent=tiles[s.col,s.row];name=source.rows[s.row][s.col]=='H'?"Beveled platform cap":"Textured ground slab";}
    else if(s.name=="road slab"||s.name=="continuous elevated service roof"){
     int c=Mathf.RoundToInt(s.position.x+6.5f),r=Mathf.RoundToInt(4.5f-s.position.z);if(c>=0&&c<14&&r>=0&&r<10)parent=tiles[c,r];
    }
    foreach(var id in gates.Keys)if(s.path.StartsWith("gate "+id+"/",StringComparison.Ordinal))parent=gates[id];
    if(!string.IsNullOrEmpty(s.gateId)&&gates.ContainsKey(s.gateId))parent=gates[s.gateId];
    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.position=s.position;
    go.GetComponent<MeshFilter>().sharedMesh=meshes[s.id];var r0=go.GetComponent<MeshRenderer>();r0.sharedMaterial=materials[s.material];r0.shadowCastingMode=s.castShadow?ShadowCastingMode.On:ShadowCastingMode.Off;r0.receiveShadows=s.receiveShadow;
    GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
   }
   var map=AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(Profile);map.gridRows=source.rows;map.platformHeight=source.platformHeight;
   var encounter=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Battle);
   if(!encounter.defenders.Any(d=>d.artId=="D27")){
    var original=Resources.Load<DefenderTrialDefinition>("DefenderTrials/D27");var d=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(original.defenders.First(x=>x.artId=="D27")));
    d.id="map05-pramanix";d.position=new Vector3(-4.5f,.72f,1.5f);encounter.defenders=encounter.defenders.Concat(new[]{d}).ToArray();
   }else encounter.defenders.First(d=>d.artId=="D27").position=new Vector3(-4.5f,.72f,1.5f);
   AddHeatMarkers(field.transform,map);
   encounter.surfaces=field.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
   UpdateLayoutSource(source.rows);
   Lighting();
   EditorUtility.SetDirty(map);EditorUtility.SetDirty(encounter);AssetDatabase.SaveAssets();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Validate();RenderPreview();
   Debug.Log("HEAT_EXCHANGE_V5_APPLIED: approved authored geometry/materials; 140 editable cells; original two routes, three gates and three heat pumps; original four guards plus D27; no runtime terrain construction.");
  }
  static Dictionary<int,Mesh> Meshes(Transfer source)
  {
   string path=Art+"/Meshes/Geometry.asset";var saved=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToDictionary(m=>m.name);
   var result=new Dictionary<int,Mesh>();
   foreach(var s in source.meshes){
    var m=new Mesh{name="V5-"+s.id.ToString("0000"),indexFormat=s.vertices.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16};
    m.vertices=Enumerable.Range(0,s.vertices.Length/3).Select(i=>new Vector3(s.vertices[i*3],s.vertices[i*3+1],s.vertices[i*3+2])).ToArray();
    m.normals=Enumerable.Range(0,s.normals.Length/3).Select(i=>new Vector3(s.normals[i*3],s.normals[i*3+1],s.normals[i*3+2])).ToArray();
    m.uv=Pairs(s.uv);m.uv2=Pairs(s.panelUv);
    if(s.shelterEdges!=null&&s.shelterEdges.Length==m.vertexCount*4)m.colors=Enumerable.Range(0,m.vertexCount).Select(i=>new Color(s.shelterEdges[i*4],s.shelterEdges[i*4+1],s.shelterEdges[i*4+2],s.shelterEdges[i*4+3])).ToArray();
    m.triangles=s.triangles;m.RecalculateBounds();m.RecalculateTangents();
    if(saved.TryGetValue(m.name,out var existing)){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);m=existing;}
    else if(!File.Exists(path))AssetDatabase.CreateAsset(m,path);else AssetDatabase.AddObjectToAsset(m,path);
    EditorUtility.SetDirty(m);result[s.id]=m;
   }
   AssetDatabase.SaveAssets();return result;
  }
  static Vector2[] Pairs(float[] a)=>a==null?Array.Empty<Vector2>():Enumerable.Range(0,a.Length/2).Select(i=>new Vector2(a[i*2],a[i*2+1])).ToArray();
  static Dictionary<int,Material> Materials(Transfer source)
  {
   var normalIds=new HashSet<string>(source.materials.Select(m=>m.normalMap).Where(x=>!string.IsNullOrEmpty(x)));
   var textures=new Dictionary<string,Texture2D>();
   foreach(var t in source.textures){string path=Art+"/"+t.file.Replace("textures/","Textures/");AssetDatabase.ImportAsset(path);
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normalIds.Contains(t.id)?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=t.colorSpace=="srgb"&&!normalIds.Contains(t.id);importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.anisoLevel=8;importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=t.wrapS==1000?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.SaveAndReimport();textures[t.id]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
   }
   var result=new Dictionary<int,Material>();
   foreach(var s in source.materials){
    string path=Art+"/Materials/M"+s.id.ToString("000")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);string shader=!string.IsNullOrEmpty(s.role)?"YesterdayConvoy/Heat Station V5":s.type=="MeshBasicMaterial"?"YesterdayConvoy/Heat Station Marking":"Standard";
    if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}else m.shader=Shader.Find(shader);
    m.name=string.IsNullOrEmpty(s.role)?s.name:s.role;ColorUtility.TryParseHtmlString("#"+s.colorSRGB,out var color);color.a=s.opacity;m.SetColor("_Color",color);
    if(m.HasProperty("_Cull"))m.SetFloat("_Cull",s.side==2?0:2);
    if(!string.IsNullOrEmpty(s.map))m.SetTexture("_MainTex",textures[s.map]);
    if(!string.IsNullOrEmpty(s.normalMap)){m.SetTexture("_BumpMap",textures[s.normalMap]);m.EnableKeyword("_NORMALMAP");}
    if(!string.IsNullOrEmpty(s.roughnessMap)&&m.HasProperty("_RoughnessMap"))m.SetTexture("_RoughnessMap",textures[s.roughnessMap]);
    if(m.HasProperty("_BumpScale"))m.SetFloat("_BumpScale",s.normalScale?.FirstOrDefault()??.04f);
    if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",s.metalness);
    if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",1-s.roughness);
    if(!string.IsNullOrEmpty(s.role)){
     bool metal=s.role!="cast-top"&&s.role!="service-paving"&&s.role!="cast-wall"&&s.role!="perimeter-cast";
     var detail=s.role=="cast-top"?.19f:s.role=="service-paving"||s.role=="cast-wall"?.16f:s.role=="perimeter-cast"?.14f:s.role=="painted-machinery"?.075f:s.role=="worn-steel"?.07f:s.role=="charcoal-recess"?.03f:s.role=="oxidized-service-pipe"?.09f:.04f;
     m.SetFloat("_Detail",detail);m.SetFloat("_Mean",metal?19.5f:2.2f);m.SetFloat("_Roughness",s.roughness);m.SetVector("_Role",new Vector4(s.role=="cast-top"?1:0,s.role=="service-paving"?1:0,s.role=="cast-wall"||s.role=="perimeter-cast"?1:0,metal?1:0));
    }else if(s.transparent&&shader=="Standard"){
     m.SetFloat("_Mode",2);m.SetOverrideTag("RenderType","Transparent");m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;
    }
    if(s.emissiveLinear!=null&&s.emissiveLinear.Any(v=>v>0)&&m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(s.emissiveLinear[0],s.emissiveLinear[1],s.emissiveLinear[2])*s.emissiveIntensity);}
    EditorUtility.SetDirty(m);result[s.id]=m;
   }
   AssetDatabase.SaveAssets();return result;
  }
  static void UpdateLayoutSource(string[] rows)
  {
   const string path="Assets/Data/ApprovedMaps/layouts.json";var data=JsonUtility.FromJson<Layouts>(File.ReadAllText(path));var map=data.maps.First(m=>m.index==5);
   if(map.rows.SequenceEqual(rows)&&map.guards.Any(g=>g.cell.x==2&&g.cell.y==3&&g.high))return;
   map.rows=rows;
   if(!map.guards.Any(g=>g.cell.x==2&&g.cell.y==3))map.guards=map.guards.Concat(new[]{new Guard{cell=new Cell{x=2,y=3},high=true}}).ToArray();
   File.WriteAllText(path,JsonUtility.ToJson(data,true)+"\n");AssetDatabase.ImportAsset(path);
  }
  static void AddHeatMarkers(Transform parent,ApprovedMapDefinition map)
  {
   string meshPath=Art+"/Meshes/HeatRim.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
   if(mesh==null){mesh=new Mesh{name="Heat pump warning rim"};var v=new List<Vector3>();var tris=new List<int>();
    foreach(var rect in new[]{new Rect(-.45f,-.45f,.9f,.035f),new Rect(-.45f,.415f,.9f,.035f),new Rect(-.45f,-.415f,.035f,.83f),new Rect(.415f,-.415f,.035f,.83f)}){int i=v.Count;v.AddRange(new[]{new Vector3(rect.xMin,0,rect.yMin),new Vector3(rect.xMin,0,rect.yMax),new Vector3(rect.xMax,0,rect.yMax),new Vector3(rect.xMax,0,rect.yMin)});tris.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
    mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,meshPath);
   }
   string matPath=Art+"/Materials/HeatWarning.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(Shader.Find("Unlit/Color")){color=new Color(.2f,.17f,.13f)};AssetDatabase.CreateAsset(material,matPath);}
   for(int i=0;i<map.mechanics.heatPumps.Length;i++){var go=new GameObject("Heat pump "+i+" / actual simulation warning",typeof(MeshFilter),typeof(MeshRenderer),typeof(ApprovedHeatMarker));go.transform.SetParent(parent,false);go.transform.position=map.mechanics.heatPumps[i].position+Vector3.up*.08f;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<ApprovedHeatMarker>().index=i;}
  }
  static void Lighting()
  {
   foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))Object.DestroyImmediate(l.gameObject);
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.42f,.44f,.44f);RenderSettings.ambientEquatorColor=new Color(.28f,.30f,.29f);RenderSettings.ambientGroundColor=new Color(.15f,.17f,.16f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.15f,.19f,.21f);RenderSettings.fogDensity=.011f;
   var sun=new GameObject("V5 daylight / consistent actor shadow").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.LookRotation(-new Vector3(-7,16,10));sun.color=new Color(.96f,.95f,.91f);sun.intensity=.90f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.68f;sun.shadowBias=.015f;sun.shadowNormalBias=.04f;sun.shadowResolution=LightShadowResolution.VeryHigh;
   var fill=new GameObject("V5 cool reflected fill").AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.LookRotation(-new Vector3(8,5,-7));fill.color=new Color(.76f,.87f,.90f);fill.intensity=.12f;
   foreach(var p in new[]{new Vector3(-.5f,.85f,2.41f),new Vector3(1.55f,.9f,5.15f),new Vector3(-7.95f,.62f,-2.45f)}){var l=new GameObject("V5 maintenance work light").AddComponent<Light>();l.type=LightType.Point;l.transform.position=p;l.color=new Color(1,.78f,.46f);l.range=3;l.intensity=.8f;}
   var camera=Camera.main;camera.orthographic=false;camera.fieldOfView=27;camera.transform.position=new Vector3(0,22.2f,-12.8f);camera.transform.rotation=Quaternion.Euler(60,0,0);camera.backgroundColor=new Color(.125f,.16f,.17f);
  }
  public static void Validate(){ApprovedMapsSceneTools.Validate();var map=AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(Profile);if(map.gridRows.Take(7).SelectMany(r=>r.Take(4)).Count(c=>c=='H')!=6||!map.encounter.defenders.Any(d=>d.artId=="D27"))throw new InvalidOperationException("V5 courtyard/D27 missing.");Debug.Log("HEAT_EXCHANGE_V5_VALIDATED");}
  public static void RenderPreview()
  {
   EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var c=Camera.main;var rt=new RenderTexture(1920,1080,24){antiAliasing=2};var old=c.targetTexture;var active=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
   try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("artifacts/heat-exchange-v5");File.WriteAllBytes("artifacts/heat-exchange-v5/saved-map.png",image.EncodeToPNG());string path="Assets/Art/Environment/ApprovedMaps/Thumbnails/Map05.png";File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path);var map=AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(Profile);map.preview=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();}
   finally{c.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
  }
  static Transform Child(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
  static void Ensure(string path){if(AssetDatabase.IsValidFolder(path))return;Ensure(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
 }
}
