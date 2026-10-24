using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    // Independent authored scenes: never opens or changes InitialDemo.
    public static class MapPreviewTools
    {
        const string Folder = "Assets/_Game/MapPreviews";
        static Transform root;
        static Material floor, lane, edge, metal, dark, accent, red, blue, snow, water, reed;
        static readonly string[] Names = { "01-IndustrialTransfer", "02-SnowRelay", "03-ReedCanal" };
        static readonly Vector2Int[][] Routes = {
            new[]{ V(-6,2),V(-2,2),V(-2,-2),V(3,-2),V(3,1),V(6,1) },
            new[]{ V(-6,-2),V(-4,-2),V(-4,2),V(0,2),V(0,-1),V(4,-1),V(4,3),V(6,3) },
            new[]{ V(-6,1),V(-3,1),V(-3,-2),V(2,-2),V(2,2),V(6,2) }
        };
        static Vector2Int V(int x,int z) => new Vector2Int(x,z);

        [MenuItem("Yesterday Convoy/Map Previews/Generate Three Scenes and Images")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder + "/Materials");
            Directory.CreateDirectory("artifacts/map-previews");
            AssetDatabase.Refresh();
            floor=Mat("Concrete",new Color(.28f,.34f,.37f),true);
            lane=Mat("Route",new Color(.51f,.55f,.55f),true);
            edge=Mat("Edge",new Color(.43f,.50f,.53f));
            metal=Mat("Steel",new Color(.16f,.23f,.28f));
            dark=Mat("Recess",new Color(.055f,.085f,.11f));
            accent=Mat("Amber",new Color(.70f,.49f,.20f),false,true);
            red=Mat("Entry",new Color(.95f,.23f,.22f),false,true);
            blue=Mat("Exit",new Color(.12f,.62f,.95f),false,true);
            snow=Mat("Snow",new Color(.70f,.79f,.83f),true);
            water=Mat("Water",new Color(.075f,.23f,.25f));
            water.shader=Shader.Find("YesterdayConvoy/Static Canal");
            water.SetColor("_Deep",new Color(.018f,.085f,.10f));
            water.SetColor("_Shallow",new Color(.055f,.24f,.23f));
            EditorUtility.SetDirty(water);
            reed=Mat("Reeds",new Color(.38f,.40f,.24f));
            for(int theme=0;theme<3;theme++) GenerateScene(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("MAP_PREVIEWS_PASS: three independent scenes, continuous routes, valid materials; rendered 1920x1080.");
        }

        static void GenerateScene(int theme)
        {
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            root=new GameObject(Names[theme]).transform;
            var route=Routes[theme];
            Box("Surrounding terrain",0,-.7f,0,50,.3f,50,theme==1?snow:floor);
            if(theme==2)Box("Canal basin",0,-.40f,0,40,.08f,40,water);
            else Box("Concrete foundation",0,-.30f,.5f,13.3f,.5f,8.3f,metal);
            for(int x=-6;x<=6;x++)for(int z=-3;z<=4;z++)
            {
                bool on=OnRoute(x,z,route);
                bool channel=theme==2 && (x==-1||x==0) && z!=-2;
                if(channel)continue;
                bool raised=!on && ((theme==0 && ((x>=0&&x<=1&&z>=0&&z<=2)||(x==-4&&z<=0))) ||
                    (theme==1&&((x==-2&&z<=0)||(x==2&&z>=1))) ||
                    (theme==2&&((x==-4&&z<=-1)||(x==4&&z<=0))));
                float h=raised?.34f:0;
                Box($"Cell {x},{z}",x,h*.5f-.12f,z,.976f,.24f+h,.976f,on?lane:(theme==1?snow:floor));
                Frame($"Tile rim {x},{z}",x,z,h+.004f,.94f,.012f,edge);
                if(raised)
                {
                    Box("Raised recess",x,h+.01f,z,.77f,.018f,.77f,metal);
                    Box("Platform designation",x,h+.025f,z-.35f,.28f,.012f,.035f,accent);
                }
                if(on && ((x+z)%2==0))
                    Box("Route marker",x-.35f,.015f,z,.045f,.012f,.24f,accent);
                if(!on && !raised && (x*7+z*3)%11==0)
                {
                    Box("Service recess",x,.01f,z,.55f,.018f,.32f,metal);
                    for(int k=0;k<5;k++)Box("Drain",x-.20f+k*.1f,.023f,z,.035f,.012f,.25f,dark);
                }
            }
            // Dashed, low-contrast direction guides are distinct from cell borders.
            for(int i=1;i<route.Length;i++)
            {
                Vector2 a=route[i-1],b=route[i], d=(b-a).normalized;
                int count=Mathf.RoundToInt(Vector2.Distance(a,b));
                for(int j=0;j<count;j++)
                {
                    Vector2 p=a+d*(j+.5f);
                    Box("Direction guide",p.x,.02f,p.y,d.x==0?.035f:.23f,.012f,d.x==0?.23f:.035f,accent);
                }
            }
            Gate(route[0],red,"Enemy entrance"); Gate(route[route.Length-1],blue,"Defense exit");
            if(theme==0)Industrial(); else if(theme==1)Snow(); else Canal();
            // Perimeter structure stays outside the playable grid.
            for(int s=-1;s<=1;s+=2)
            {
                Box("Outer coping",s*6.65f,.04f,.5f,.18f,.22f,8.35f,edge);
                if(s<0)continue;
                for(int k=0;k<5;k++)
                {
                    float x=-5.4f+k*2.7f;
                    Box("Retaining wall",x,.30f,5.15f,2.5f,.9f,.6f,metal);
                    Box("Wall cap",x,.79f,5.15f,2.6f,.09f,.70f,theme==1?snow:edge);
                    Box("Wall panel",x,.32f,4.83f,2.12f,.47f,.035f,dark);
                }
            }
            var camera=new GameObject("Map Preview Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera";camera.fieldOfView=24;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            camera.transform.rotation=Quaternion.Euler(60,0,0);
            camera.transform.position=new Vector3(0,0,.7f)-camera.transform.forward*24;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.08f,.11f);
            var key=new GameObject("Key Light",typeof(Light)).GetComponent<Light>();
            key.type=LightType.Directional;key.intensity=1.15f;key.color=theme==1?new Color(.85f,.93f,1):new Color(1,.96f,.88f);
            key.transform.rotation=Quaternion.Euler(48,-32,0);key.shadows=LightShadows.Soft;key.shadowStrength=.6f;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.39f,.44f,.48f);
            var ui=new GameObject("Preview Navigation").AddComponent<MapPreviewNavigation>();ui.mapIndex=theme;
            ui.interfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
            if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Unexpected colliders");
            if(root.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial==null))throw new InvalidOperationException("Missing material");
            for(int x=-6;x<=6;x++)for(int z=-3;z<=4;z++)
                if(OnRoute(x,z,route)&&root.Find($"Cell {x},{z}")==null)throw new InvalidOperationException("Route crosses missing tile");
            EditorSceneManager.SaveScene(scene,Folder+"/"+Names[theme]+".unity");
            var rt=new RenderTexture(1920,1080,24);rt.antiAliasing=4;camera.targetTexture=rt;
            camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes("artifacts/map-previews/"+Names[theme]+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }

        static void Industrial()
        {
            for(int s=-1;s<=1;s+=2)
            {
                Box("Service building",s*8.1f,.65f,1.2f,1.8f,1.8f,5.8f,metal);
                Box("Roof slab",s*8.1f,1.60f,1.2f,2f,.15f,6f,edge);
                for(int k=0;k<6;k++)Box("Roof rib",s*8.1f,1.7f,-1.2f+k*.9f,1.6f,.08f,.12f,dark);
            }
            for(int k=0;k<3;k++)Crate(-.5f+k*1.1f,3.6f,.62f);
            Tower(-5,3.8f,false);Tower(5,-2.7f,false);
            Facility(1,1,"Altar location");
            for(int k=0;k<3;k++)Box("Rear utility pipe",0,.2f+k*.18f,4.4f,7,.11f,.11f,edge);
        }

        static void Snow()
        {
            for(int k=0;k<9;k++)
            {
                float x=-7+k*1.8f,z=k%2==0?6.4f:-5.1f;
                Box("Ice embankment",x,-.05f,z,1.65f,.85f,1.4f,edge);
                Box("Snow drift",x,.42f,z,1.8f,.13f,1.55f,snow);
            }
            Tower(-5,3.9f,true);Tower(5.7f,-2.6f,true);
            Facility(2,2,"Frost field location");
            for(int k=0;k<3;k++)
            {
                Crate(1+k*.85f,3.7f,.6f);
                Box("Snow on supplies",1+k*.85f,.63f,3.7f,.66f,.08f,.66f,snow);
            }
            for(int s=-1;s<=1;s+=2)
                for(int k=0;k<4;k++)Box("Ice fracture",s*(7.1f+k*.19f),-.45f,-2+k*1.3f,.10f,.02f,1.2f,metal);
        }

        static void Canal()
        {
            for(int x=-1;x<=0;x++)
            {
                Box("Bridge support",x,-.28f,-2,.9f,.3f,.9f,metal);
                for(int s=-1;s<=1;s+=2)Box("Bridge safety rail",x,.22f,-2+s*.46f,.96f,.045f,.05f,edge);
            }
            for(int s=-1;s<=1;s+=2)
            {
                Box("Canal retaining bank",s*7.5f,-.27f,.4f,.3f,.45f,12,metal);
                for(int k=0;k<11;k++)
                {
                    float z=-4.4f+k*.92f;
                    for(int j=0;j<5;j++)
                    {
                        float x=s*(7.0f+(j%3)*.20f),h=.35f+((k*7+j*3)%5)*.1f;
                        Box("Reed stalk",x,-.35f+h*.5f,z+j*.07f,.025f,h,.025f,reed);
                        Box("Reed head",x,-.30f+h,z+j*.07f,.055f,.16f,.055f,accent);
                    }
                    Box("Water reflection",s*7.25f,-.35f,z,.55f,.012f,.025f,edge);
                }
            }
            Tower(-5,3.7f,false);Facility(4,-1,"Gramophone location");Facility(-4,-1,"Reed ignition location");
            for(int k=0;k<4;k++)Crate(2.9f+k*.85f,3.9f,.56f);
        }

        static void Tower(float x,float z,bool winter)
        {
            Box("Relay base",x,.12f,z,.82f,.24f,.82f,metal);
            Box("Relay cabinet",x,.65f,z,.58f,.86f,.58f,floor);
            Box("Relay panel",x,.66f,z-.302f,.40f,.52f,.025f,dark);
            for(int k=0;k<3;k++)Box("Relay vent",x,.52f+k*.13f,z-.32f,.30f,.035f,.025f,edge);
            Box("Relay roof",x,1.14f,z,.75f,.13f,.75f,winter?snow:edge);
            Box("Antenna",x,1.8f,z,.045f,1.2f,.045f,metal);
            Box("Signal bar",x,2.1f,z,.7f,.035f,.035f,blue);
        }
        static void Crate(float x,float z,float size)
        {
            Box("Supply crate",x,size*.5f,z,size,size,size,metal);
            Box("Crate face",x,size*.5f,z-size*.505f,size*.76f,size*.70f,.02f,edge);
            Box("Crate stripe",x,size*.5f,z-size*.53f,size*.10f,size*.64f,.015f,accent);
        }
        static void Facility(int x,int z,string name)
        {
            var marker=new GameObject(name).transform;marker.SetParent(root);marker.position=new Vector3(x,.4f,z);
            Frame(name+" placeholder",x,z,.405f,.66f,.045f,blue);
            // A recessed, clearly inactive socket; no gameplay component or active-effect claim.
            Box(name+" socket",x,.39f,z,.5f,.035f,.5f,dark);
        }
        static void Gate(Vector2Int p,Material mat,string name)
        {
            Frame(name+" threshold",p.x,p.y,.04f,.86f,.035f,mat);
            Frame(name+" top",p.x,p.y,.83f,.86f,.035f,mat);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                Box(name+" post",p.x+x*.43f,.43f,p.y+z*.43f,.035f,.8f,.035f,mat);
        }
        static bool OnRoute(int x,int z,Vector2Int[] points)
        {
            for(int i=1;i<points.Length;i++)
            {
                var a=points[i-1];var b=points[i];
                if(a.x==b.x&&x==a.x&&z>=Math.Min(a.y,b.y)&&z<=Math.Max(a.y,b.y)||
                   a.y==b.y&&z==a.y&&x>=Math.Min(a.x,b.x)&&x<=Math.Max(a.x,b.x))return true;
            }
            return false;
        }
        static void Frame(string name,float x,float z,float y,float size,float width,Material m)
        {
            for(int s=-1;s<=1;s+=2)
            {
                Box(name,x+s*size*.5f,y,z,width,.012f,size,m);
                Box(name,x,y,z+s*size*.5f,size,.012f,width,m);
            }
        }
        static void Box(string name,float x,float y,float z,float sx,float sy,float sz,Material m)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.localPosition=new Vector3(x,y,z);go.transform.localScale=new Vector3(sx,sy,sz);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;go.isStatic=true;
        }
        static Material Mat(string name,Color color,bool texture=false,bool unlit=false)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find(unlit?"Unlit/Color":"Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;if(!unlit)m.SetFloat("_Glossiness",.10f);
            if(texture)m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Environment/Industrial/Textures/concrete-color.jpg");
            EditorUtility.SetDirty(m);return m;
        }
        public static void Build()
        {
            var result=BuildPipeline.BuildPlayer(Names.Select(n=>Folder+"/"+n+".unity").ToArray(),
                "Builds/MapPreviews/MapPreviews.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Map preview build failed");
            Debug.Log("MAP_PREVIEW_BUILD_PASS: "+result.summary.totalSize+" bytes");
        }
    }
}
