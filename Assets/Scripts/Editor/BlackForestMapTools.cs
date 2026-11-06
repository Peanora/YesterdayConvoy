using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    public static class BlackForestMapTools
    {
        public const string ScenePath="Assets/Scenes/BlackForestCrossing.unity";
        const string Art="Assets/Art/Environment/BlackForest",Data="Assets/Data/Resources/Maps/BlackForestCrossing.asset";
        static int meshNumber;
        static Material moss,blackflow,amber,vein,stone,bark,dark,red,blue,leaf,twig;
        static Mesh topMesh,petalMesh;
        static readonly System.Random random=new System.Random(1733);
        static float R(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        [MenuItem("Yesterday Convoy/Scenes/Open Black Forest Crossing")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
        [MenuItem("Yesterday Convoy/Black Forest/Create Missing Authored Map")]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring maps");
            if(File.Exists(ScenePath)){Validate();Debug.Log("BLACK_FOREST_EXISTING_SCENE_PRESERVED");return;}
            BlackForestLayout.Validate();Directory.CreateDirectory(Art);Directory.CreateDirectory(Art+"/Meshes");AssetDatabase.Refresh();
            MakeMaterials();
            var data=Object.Instantiate(Resources.Load<DefenderTrialDefinition>("Maps/BattleShop"));
            data.name="BlackForestCrossing";data.mapId=BlackForestLayout.Id;data.mapTitle="树海 · 交错回廊";data.terrainOnly=true;
            data.lanes=BlackForestLayout.Lanes();data.route=data.lanes[0].points;data.defenders=Array.Empty<DefenderPlacement>();data.rosterCards=Array.Empty<ManualBattleDefinition>();
            data.battle=Resources.Load<ManualBattleDefinition>("MonsterTests/M08");data.raised=Enumerable.Range(1,14).SelectMany(y=>Enumerable.Range(1,17).Where(x=>BlackForestLayout.Tile(x,y)=='H').Select(x=>new Vector2Int(x,y))).ToArray();
            data.surfaces=new[]{moss,blackflow,amber,vein,stone,bark};AssetDatabase.CreateAsset(data,Data);
            if(!AssetDatabase.CopyAsset(InitialDemoTools.ScenePath,ScenePath))throw new InvalidOperationException("Could not copy the authored scene host");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            foreach(var boot in Object.FindObjectsByType<ShopMapBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(boot);
            foreach(var boot in Object.FindObjectsByType<CampaignBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(boot);
            var old=GameObject.Find("Industrial Map");if(old!=null)Object.DestroyImmediate(old);
            foreach(var oldField in Object.FindObjectsByType<AuthoredBattlefield>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(oldField.gameObject);
            var host=Object.FindAnyObjectByType<InitialDemo>();host.RedGate.gameObject.SetActive(false);host.BlueGate.gameObject.SetActive(false);
            foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))sr.enabled=false;
            var root=new GameObject("Authored Black Forest Crossing");root.AddComponent<AuthoredBattlefield>();
            var geometry=new GameObject("Terrain - edit and save these scene objects").transform;geometry.SetParent(root.transform,false);
            var foliage=new GameObject("Peripheral roots and foliage - no collision").transform;foliage.SetParent(root.transform,false);
            Box("Dark forest basin",root.transform,new Vector3(0,-.65f,0),new Vector3(34,.2f,27),dark);
            for(int y=1;y<=14;y++)for(int x=1;x<=17;x++)BuildCell(geometry,x,y);
            foreach(var lane in data.lanes)Gate(root.transform,lane.id,lane.points[0],red);
            foreach(var lane in data.lanes.GroupBy(l=>l.exitId).Select(g=>g.First()))Gate(root.transform,lane.exitId,lane.points.Last(),blue);
            MakeSurroundings(foliage);
            var bootstrap=new GameObject("Black Forest trial entry").AddComponent<BlackForestBootstrap>();bootstrap.encounter=data;
            Camera.main.transform.rotation=Quaternion.Euler(60,0,0);Camera.main.transform.position=Camera.main.transform.rotation*new Vector3(0,0,-44);Camera.main.fieldOfView=24;Camera.main.backgroundColor=new Color(.016f,.032f,.035f);Camera.main.clearFlags=CameraClearFlags.SolidColor;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.43f,.54f,.53f);RenderSettings.ambientEquatorColor=new Color(.31f,.36f,.32f);RenderSettings.ambientGroundColor=new Color(.17f,.21f,.18f);
            RenderSettings.fog=false;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){
                if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(52,-28,0);light.color=new Color(.94f,1,.88f);light.intensity=1.25f;light.shadows=LightShadows.Soft;}
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            GameSceneCatalog.UpdateBuildSettings();Validate();RenderPreview();
            Debug.Log("BLACK_FOREST_AUTHORED_PASS: 17x14, 3 red / 2 blue, 38/40/42 fixed routes, saved geometry and materials; no runtime generation");
        }
        static void MakeMaterials()
        {
            var import=(TextureImporter)AssetImporter.GetAtPath(Art+"/GroundAtlas.png");import.textureType=TextureImporterType.Default;import.maxTextureSize=2048;import.wrapMode=TextureWrapMode.Clamp;import.mipmapEnabled=true;import.textureCompression=TextureImporterCompression.CompressedHQ;import.SaveAndReimport();
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/GroundAtlas.png");
            moss=Mat("Moss floor",new Color(.95f,.99f,.91f),atlas,new Vector2(0,.5f));
            blackflow=Mat("Blackflow stained floor",new Color(.85f,.96f,.96f),atlas,new Vector2(.5f,.5f),.38f);
            amber=Mat("Amber resin floor",Color.white,atlas,Vector2.zero,.24f);
            vein=Mat("Ancient root vein floor",Color.white,atlas,new Vector2(.5f,0));
            bark=Mat("Black twisted bark",new Color(.16f,.24f,.23f),atlas,new Vector2(.5f,0));
            stone=Mat("Pale worn high ground",new Color(.67f,.77f,.73f));
            dark=Mat("Inaccessible forest recess",new Color(.025f,.052f,.05f));
            leaf=Mat("Crimson forest leaves",new Color(.33f,.065f,.10f));twig=Mat("Desaturated teal twigs",new Color(.13f,.29f,.27f));
            red=Glow("Red deployment gates",new Color(.94f,.13f,.14f));blue=Glow("Blue shared defense exits",new Color(.08f,.60f,.96f));
            topMesh=new Mesh{name="Editable forest tile cap"};topMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};topMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};topMesh.triangles=new[]{0,2,1,0,3,2};topMesh.RecalculateNormals();AssetDatabase.CreateAsset(topMesh,Art+"/TileCap.asset");
            petalMesh=new Mesh{name="Curved forest leaf"};petalMesh.vertices=new[]{Vector3.zero,new Vector3(-.19f,.18f,.35f),new Vector3(0,.30f,.8f),new Vector3(.19f,.18f,.35f),new Vector3(0,.24f,.37f)};petalMesh.triangles=new[]{0,1,4,1,2,4,2,3,4,3,0,4,4,1,0,4,2,1,4,3,2,4,0,3};petalMesh.RecalculateNormals();AssetDatabase.CreateAsset(petalMesh,Art+"/Leaf.asset");
        }
        static Material Mat(string name,Color color,Texture2D texture=null,Vector2 offset=default,float smoothness=.12f)
        {
            var mat=new Material(Shader.Find("Standard")){name=name,color=color,mainTexture=texture,enableInstancing=true};mat.SetFloat("_Glossiness",smoothness);
            if(texture!=null){mat.mainTextureScale=new Vector2(.492f,.492f);mat.mainTextureOffset=offset+Vector2.one*.004f;}
            AssetDatabase.CreateAsset(mat,Art+"/"+name.Replace(' ','-')+".mat");return mat;
        }
        static Material Glow(string name,Color color)
        {
            var mat=new Material(Shader.Find("Sprites/Default")){name=name,color=color};AssetDatabase.CreateAsset(mat,Art+"/"+name.Replace(' ','-')+".mat");return mat;
        }
        static Transform Box(string name,Transform parent,Vector3 p,Vector3 scale,Material m)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go.transform;
        }
        static Transform MeshObject(string name,Transform parent,Mesh mesh,Material material,Vector3 position,Vector3 scale)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.isStatic=true;return go.transform;
        }
        static void BuildCell(Transform parent,int x,int y)
        {
            var tile=new GameObject(((char)('A'+x-1))+y.ToString("00")+" / "+BlackForestLayout.Surface(x,y)).AddComponent<BlackForestTile>();tile.transform.SetParent(parent,false);tile.transform.localPosition=BlackForestLayout.Cell(x,y);tile.coordinate=new Vector2Int(x,y);tile.surface=BlackForestLayout.Surface(x,y);
            if(tile.surface==BlackForestSurface.Inaccessible){
                Box("Root bound impassable recess",tile.transform,new Vector3(0,-.23f,0),new Vector3(.98f,.14f,.98f),dark);
                for(int n=0;n<2;n++)Tube(tile.transform,new Vector3(-.45f,-.10f,-.30f+n*.5f),new Vector3(.45f,-.08f,.28f-n*.4f),.08f,Vector3.up*.08f,bark);return;
            }
            bool raised=tile.surface==BlackForestSurface.HighGround;float h=raised?.72f:0;
            Box(raised?"Root carved platform sides":"Stone floor base",tile.transform,new Vector3(0,raised?.18f:-.15f,0),new Vector3(.98f,raised?1.04f:.30f,.98f),bark);
            if(raised){
                Box("Worn stone platform cap",tile.transform,new Vector3(0,h-.045f,0),new Vector3(.96f,.09f,.96f),stone);
                Box("Moss weathered cap inset",tile.transform,new Vector3(0,h+.006f,0),new Vector3(.82f,.018f,.82f),stone);
                // Low engraved perimeter; no warning lights or terrain effects.
                for(int side=0;side<4;side++){float a=side*Mathf.PI*.5f;Box("Carved root edging",tile.transform,new Vector3(Mathf.Cos(a)*.43f,h+.015f,Mathf.Sin(a)*.43f),side%2==0?new Vector3(.018f,.012f,.8f):new Vector3(.8f,.012f,.018f),bark);}
            }else{
                Material mat=tile.surface==BlackForestSurface.Blackflow?blackflow:tile.surface==BlackForestSurface.Amber?amber:tile.surface==BlackForestSurface.RootVein?vein:moss;
                MeshObject("Walkable surface - visual only",tile.transform,topMesh,mat,Vector3.up*.012f,new Vector3(.96f,1,.96f));
            }
        }
        static void Tube(Transform parent,Vector3 start,Vector3 end,float radius,Vector3 bend,Material material)
        {
            const int rings=14,sides=7;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3 axis=(end-start).normalized,side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized,up=Vector3.Cross(axis,side).normalized;
            for(int ring=0;ring<=rings;ring++){
                float t=(float)ring/rings;Vector3 center=Vector3.Lerp(start,end,t)+bend*Mathf.Sin(t*Mathf.PI);
                for(int n=0;n<sides;n++){float a=(float)n/sides*Mathf.PI*2+t*1.7f,r=radius*Mathf.Lerp(1,.45f,t)*(1+.12f*Mathf.Sin(n*2.3f+t*17));vertices.Add(center+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*r);uv.Add(new Vector2((float)n/sides,t));}
            }
            for(int ring=0;ring<rings;ring++)for(int n=0;n<sides;n++){int a=ring*sides+n,b=ring*sides+(n+1)%sides,c=a+sides,d=b+sides;triangles.AddRange(new[]{a,c,b,b,c,d});}
            var mesh=new Mesh{name="Authored twisted root "+meshNumber};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Art+"/Meshes/Root-"+(meshNumber++)+".asset");MeshObject("Saved twisted forest root",parent,mesh,material,Vector3.zero,Vector3.one);
        }
        static void MakeSurroundings(Transform parent)
        {
            foreach(int side in new[]{-1,1}){
                for(int n=0;n<7;n++){
                    float z=-8+n*2.7f;
                    Tube(parent,new Vector3(side*R(8.8f,10.8f),-.3f,z),new Vector3(side*R(9,11),R(.5f,1.8f),z+2.2f),R(.25f,.42f),new Vector3(side*R(.2f,.8f),R(.2f,.6f),0),bark);
                    if(n%2==0)Tube(parent,new Vector3(side*10,0,z),new Vector3(side*R(10,12),R(2.8f,4.5f),z+R(-1,1)),.38f,new Vector3(side*.7f,0,0),bark);
                }
                for(int n=0;n<5;n++){
                    float x=-10+n*4.7f;float z=side*8.1f;
                    Tube(parent,new Vector3(x,-.1f,z),new Vector3(x+5,R(.2f,.8f),z+side*R(.1f,.8f)),.31f,new Vector3(0,.45f,side*.3f),bark);
                    if(side>0)Tube(parent,new Vector3(x,0,z+1),new Vector3(x-1,R(3,5),z+2),.4f,Vector3.right*.9f,bark);
                }
            }
            for(int n=0;n<32;n++){
                float x,z;
                if(n%2==0){x=(n%4==0?-1:1)*R(9.2f,11);z=R(-8,8);}else{x=R(-10,10);z=(n%4==1?-1:1)*R(8.3f,9.6f);}
                var position=new Vector3(x,.05f,z);
                for(int j=0;j<6;j++){
                    var t=MeshObject("Dark crimson forest leaf",parent,petalMesh,j%3==0?twig:leaf,position,Vector3.one*R(.65f,1.1f));t.localRotation=Quaternion.Euler(R(-12,20),j*60+R(-15,15),0);
                }
                Tube(parent,position,position+new Vector3(R(-.5f,.5f),R(.5f,1),R(-.5f,.5f)),.025f,Vector3.right*.1f,twig);
            }
        }
        static void Gate(Transform parent,string name,Vector3 p,Material material)
        {
            var group=new GameObject(name+" / "+(name[0]=='R'?"固定路线红门":"共享防御蓝门")).transform;group.SetParent(parent,false);group.localPosition=p;
            var corners=new[]{new Vector3(-.43f,.04f,-.43f),new Vector3(.43f,.04f,-.43f),new Vector3(.43f,.04f,.43f),new Vector3(-.43f,.04f,.43f)};
            for(int i=0;i<4;i++){
                Vector3 a=corners[i],b=corners[(i+1)%4];Line(group,a,b,material);Line(group,a+Vector3.up*.8f,b+Vector3.up*.8f,material);Line(group,a,a+Vector3.up*.8f,material);
            }
        }
        static void Line(Transform parent,Vector3 a,Vector3 b,Material m)
        {
            var line=new GameObject("Saved gate outline").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=false;line.positionCount=2;line.SetPositions(new[]{a,b});line.widthMultiplier=.025f;line.sharedMaterial=m;line.shadowCastingMode=ShadowCastingMode.Off;
        }
        [MenuItem("Yesterday Convoy/Black Forest/Validate Saved Map")]
        public static void Validate()
        {
            BlackForestLayout.Validate();var scene=EditorSceneManager.OpenScene(ScenePath);var root=Object.FindAnyObjectByType<AuthoredBattlefield>();
            if(root==null||Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length!=238)throw new InvalidOperationException("Missing authored forest tiles");
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh==null||!AssetDatabase.Contains(filter.sharedMesh))throw new InvalidOperationException("Transient or missing map mesh: "+filter.name);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterial==null||!AssetDatabase.Contains(r.sharedMaterial))throw new InvalidOperationException("Transient or missing map material: "+r.name);
            if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorations must not introduce collision or line-of-sight rules");
            var data=Object.FindAnyObjectByType<BlackForestBootstrap>().encounter;_=data.Snapshot();
            if(data.LaneSnapshot().Length!=3||data.lanes.Select(l=>l.exitId).Distinct().Count()!=2||!data.terrainOnly)throw new InvalidOperationException("Wrong gate or encounter configuration");
            Debug.Log("BLACK_FOREST_SAVED_ASSETS_PASS: terrain, meshes, materials and lane configuration are persistent and editable");
        }
        public static void RenderPreview()
        {
            Directory.CreateDirectory("Builds/BlackForestEvidence");var camera=Camera.main;var rt=new RenderTexture(1920,1080,24);var before=camera.targetTexture;camera.targetTexture=rt;camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("Builds/BlackForestEvidence/editor-map.png",image.EncodeToPNG());camera.targetTexture=before;RenderTexture.active=active;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void RefineMaterials()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var texture=new Texture2D(512,512,TextureFormat.RGB24,false);var pixels=new Color[512*512];
            for(int y=0;y<512;y++)for(int x=0;x<512;x++){
                float u=x/511f,v=y/511f;
                float n=Mathf.PerlinNoise(u*10.7f,v*10.7f)*.6f+Mathf.PerlinNoise(u*63,v*63)*.22f;
                float vein=Mathf.Abs(Mathf.Sin(u*29+Mathf.Sin(v*21)*1.6f+Mathf.PerlinNoise(u*4,v*4)*5));
                float border=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                Color color=Color.Lerp(new Color(.44f,.51f,.49f),new Color(.74f,.79f,.73f),n);
                if(vein<.10f)color=Color.Lerp(color,new Color(.29f,.37f,.33f),.6f);
                if(border<.035f)color*=.48f;else if(border<.047f)color*=1.13f;
                pixels[y*512+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(Art+"/WornStoneCap.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.Refresh();
            var cap=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Pale-worn-high-ground.mat");cap.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/WornStoneCap.png");cap.color=new Color(.85f,.94f,.91f);cap.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(cap);
            var recess=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Inaccessible-forest-recess.mat");recess.color=new Color(.004f,.012f,.013f);EditorUtility.SetDirty(recess);
            // Separate front and rear vertices prevent opposite triangle normals cancelling out.
            var verts=new List<Vector3>();var triangles=new List<int>();
            for(int side=0;side<2;side++){
                int offset=verts.Count;
                for(int segment=0;segment<=8;segment++){
                    float t=segment/8f,w=Mathf.Sin(t*Mathf.PI)*.17f;
                    verts.Add(new Vector3(-w,t*.22f+Mathf.Sin(t*Mathf.PI)*.08f,t*.8f));verts.Add(new Vector3(w,t*.22f+Mathf.Sin(t*Mathf.PI)*.08f,t*.8f));
                }
                for(int segment=0;segment<8;segment++){
                    int a=offset+segment*2;triangles.AddRange(side==0?new[]{a,a+2,a+1,a+1,a+2,a+3}:new[]{a+1,a+2,a,a+3,a+2,a+1});
                }
            }
            var leafMesh=new Mesh{name="Curved two-sided forest leaf"};leafMesh.SetVertices(verts);leafMesh.SetTriangles(triangles,0);leafMesh.RecalculateNormals();leafMesh.RecalculateBounds();var savedLeaf=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Leaf.asset");EditorUtility.CopySerialized(leafMesh,savedLeaf);EditorUtility.SetDirty(savedLeaf);Object.DestroyImmediate(leafMesh);
            var redLeaf=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Crimson-forest-leaves.mat");redLeaf.color=new Color(.48f,.10f,.14f);EditorUtility.SetDirty(redLeaf);
            foreach(var tile in Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Where(t=>t.surface==BlackForestSurface.HighGround))
                foreach(Transform child in tile.transform.Cast<Transform>().ToArray())if(child.name=="Carved root edging")Object.DestroyImmediate(child.gameObject);
            RenderSettings.reflectionIntensity=0;RenderSettings.ambientSkyColor=new Color(.30f,.40f,.38f);RenderSettings.ambientEquatorColor=new Color(.20f,.27f,.24f);RenderSettings.ambientGroundColor=new Color(.10f,.15f,.12f);
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional)l.intensity=.95f;
            AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);RenderPreview();Debug.Log("BLACK_FOREST_MATERIAL_REFINEMENT_PASS");
        }
        public static void Build()=>BaselineTools.BuildWindows();
    }
}
