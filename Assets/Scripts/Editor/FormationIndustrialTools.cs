using System;
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
    /// <summary>Retired geometric courtyard authoring, kept only as implementation history.</summary>
    public static class FormationIndustrialTools
    {
        const string RootName="Industrial courtyard · saved geometry";
        const string Assets="Assets/Art/Environment/FormationBattle/Industrial";
        const string TextureRoot="Assets/Art/Environment/Industrial/Textures/";
        static Material floor,floorAlt,floorWet,wall,steel,edge,dark,amber,white,red,blue;
        static Mesh cube,quad,cylinder;

        public static void Apply()
        {
            throw new InvalidOperationException("The geometric industrial courtyard authoring entry is retired and cannot overwrite the approved painted scene. Use Yesterday Convoy > Formation Battle > Apply Approved Painted Courtyard (FormationPaintedMapTools.Apply).");
        }

        // No menu or public entry reaches this rejected visual version.
        static void ApplyRetiredReference()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save existing dirty scenes first.");
            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            if(controller==null)throw new InvalidOperationException("Formation scene controller missing.");
            Directory.CreateDirectory(Assets);AssetDatabase.Refresh();
            cube=Mesh("UnitCube");quad=Mesh("GroundQuad");cylinder=Mesh("UnitCylinder");
            floor=Mat("Concrete",new Color(.51f,.55f,.60f),"concrete-color.jpg",.30f,.05f);
            floorAlt=Mat("ConcreteAlternate",new Color(.47f,.51f,.56f),"concrete-color.jpg",.37f,.05f);
            floorWet=Mat("DampConcrete",new Color(.45f,.51f,.58f),"concrete-color.jpg",.76f,.22f);
            wall=Mat("Wall",new Color(.32f,.38f,.45f),"concrete-color.jpg",.18f);
            steel=Mat("Steel",new Color(.45f,.52f,.59f),"trim-01-color.png",.36f,.42f);
            edge=Mat("MachinedEdges",new Color(.53f,.59f,.63f),"trim-02-color.png",.42f,.38f);
            dark=Mat("Recess",new Color(.065f,.085f,.11f),null,.19f,.15f);
            amber=Mat("SafetyAmber",new Color(.90f,.61f,.18f),null,.27f,.1f,true);
            white=Mat("CoolLamps",new Color(.59f,.79f,.88f),null,.22f,0,true);
            red=Mat("EntryRed",new Color(.94f,.13f,.16f),null,.15f,0,true);
            blue=Mat("ExitBlue",new Color(.10f,.65f,.94f),null,.15f,0,true);
            foreach(var m in new[]{floor,floorAlt,floorWet}){m.mainTextureScale=new Vector2(.43f,.43f);EditorUtility.SetDirty(m);}

            var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(old!=null)Object.DestroyImmediate(old);
            var terrain=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.StartsWith("Authored rectangle",StringComparison.Ordinal));
            if(terrain==null)throw new InvalidOperationException("Saved rectangle terrain missing.");
            // Preserve every manually saved cell transform; replace only this mode's previous greybox trim.
            foreach(var t in terrain.transform.Cast<Transform>().ToArray())
                if(!t.name.StartsWith("Tile ",StringComparison.Ordinal))Object.DestroyImmediate(t.gameObject);
            int index=0;
            foreach(var t in terrain.transform.Cast<Transform>())
            {
                t.GetComponent<Renderer>().sharedMaterial=index%11==0?floorWet:index%4==0?floorAlt:floor;
                index++;
            }
            if(index!=112)throw new InvalidOperationException("The logical rectangle must keep 112 cells.");
            var root=new GameObject(RootName).transform;
            var surround=Group(root,"Courtyard perimeter and service floor");
            var asphalt=Mat("ServiceAsphalt",new Color(.37f,.43f,.51f),"asphalt-color.jpg",.64f,.12f);asphalt.mainTextureScale=new Vector2(6,4);EditorUtility.SetDirty(asphalt);
            Box(surround,"Continuous service ground",new Vector3(0,-.19f,0),new Vector3(34,.16f,18),asphalt);
            Box(surround,"Battle slab foundation",new Vector3(0,-.16f,0),new Vector3(14.35f,.15f,6.65f),dark);
            foreach(int sign in new[]{-1,1})
            {
                Box(surround,"Concrete perimeter curb",new Vector3(0,.045f,sign*3.34f),new Vector3(14.5f,.18f,.16f),wall);
                Box(surround,"Steel curb cap",new Vector3(0,.15f,sign*3.34f),new Vector3(14.5f,.035f,.20f),edge);
                for(int i=-6;i<=6;i+=2)
                {
                    Box(surround,"Inset amber curb lamp",new Vector3(i,.175f,sign*3.34f),new Vector3(.42f,.028f,.075f),amber);
                    Box(surround,"Drain recess",new Vector3(i+.70f,.008f,sign*3.04f),new Vector3(.62f,.012f,.20f),dark);
                    for(int bar=0;bar<7;bar++)Box(surround,"Drain grate",new Vector3(i+.45f+bar*.08f,.019f,sign*3.04f),new Vector3(.025f,.014f,.18f),edge);
                }
            }
            foreach(int sign in new[]{-1,1})
            {
                Box(surround,"Open play boundary",new Vector3(sign*7.2f,.02f,0),new Vector3(.16f,.13f,6.55f),wall);
                Fence(surround,new Vector3(sign*8.65f,0,0),6.1f,true);
            }
            // Flush maintenance panels are surfaces, not collision or routing obstacles.
            foreach(var cell in new[]{new Vector2(2,1),new Vector2(4,6),new Vector2(9,1),new Vector2(11,5)})
            {
                Vector3 p=FormationBattleController.WorldPoint(cell);p.y=.012f;
                Box(root,"Flush maintenance cover",p,new Vector3(.46f,.014f,.36f),steel);
                for(int i=0;i<4;i++)
                {
                    var slat=Box(root,"Inset panel slat",p+new Vector3(-.14f+i*.09f,.01f,0),new Vector3(.018f,.008f,.27f),dark);
                    slat.localRotation=Quaternion.Euler(0,-25,0);
                }
            }

            var warehouse=Group(root,"Warehouse facade, shutters and service gantry");
            foreach(float x in new[]{-3.8f,-.4f,3.0f})Warehouse(warehouse,x);
            Box(warehouse,"Rear elevated service deck",new Vector3(.1f,1.55f,4.8f),new Vector3(13.5f,.13f,.58f),steel);
            for(int i=-6;i<=6;i+=2)
            {
                Beam(warehouse,"Catwalk post",new Vector3(i,1.61f,4.63f),new Vector3(i,1.97f,4.63f),.035f,amber);
                Box(warehouse,"Deck support",new Vector3(i,1.07f,4.91f),new Vector3(.15f,.90f,.18f),dark);
            }
            Beam(warehouse,"Catwalk handrail",new Vector3(-6.7f,1.97f,4.63f),new Vector3(6.9f,1.97f,4.63f),.035f,amber);
            Beam(warehouse,"Back wall continuous duct",new Vector3(-7,.72f,4.7f),new Vector3(7,.72f,4.7f),.13f,steel);
            for(int i=-7;i<=7;i+=2)Box(warehouse,"Duct joint",new Vector3(i,.72f,4.7f),new Vector3(.08f,.18f,.18f),edge);
            foreach(float x in new[]{-6.5f,6.5f})
            {
                var vent=Prop(warehouse,"Prop_Vent_Big.fbx",new Vector3(x,0,4.0f),1.1f);
                if(vent==null)Box(warehouse,"Ventilation cabinet",new Vector3(x,.5f,4),new Vector3(.9f,1,.7f),steel);
                Box(warehouse,"Utility switchbox",new Vector3(x, .4f,3.53f),new Vector3(.37f,.68f,.21f),edge);
            }
            Crane(root);
            var foreground=Group(root,"Low foreground freight and safety rails");
            foreach(float x in new[]{-7.9f,-6.7f,5.8f,7.4f})
            {
                Prop(foreground,"Prop_Crate3.fbx",new Vector3(x,0,-3.90f),.75f);
                Box(foreground,"Low service block",new Vector3(x,.12f,-4.1f),new Vector3(1.05f,.24f,.52f),wall);
            }
            foreach(float x in new[]{-4.8f,-2.4f,1.8f,4.2f})Fence(foreground,new Vector3(x,0,-3.80f),1.25f,false);
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<4;i++)
                {
                    var b=Box(foreground,"Painted perimeter hazard stripe",new Vector3(side*6.1f+i*.13f,.015f,side*2.96f),new Vector3(.055f,.008f,.30f),amber);
                    b.localRotation=Quaternion.Euler(0,35,0);
                }
            var gates=Group(root,"Open gate markers · visual boundaries only");
            for(int row=0;row<8;row++){Gate(gates,"Red",5.5f,row,red);Gate(gates,"Blue",13,row,blue);}

            var camera=controller.battleCamera;
            camera.orthographicSize=3.80f;camera.transform.rotation=Quaternion.Euler(60,0,0);
            camera.transform.position=new Vector3(0,17.32f,-10)+camera.transform.up*.72f;
            camera.backgroundColor=new Color(.045f,.063f,.085f);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.50f,.59f,.72f);
            RenderSettings.ambientEquatorColor=new Color(.35f,.42f,.51f);
            RenderSettings.ambientGroundColor=new Color(.19f,.23f,.28f);
            foreach(var light in Object.FindObjectsByType<Light>())if(light.type==LightType.Directional)
            {
                light.intensity=.82f;light.color=new Color(.84f,.90f,1);light.transform.rotation=Quaternion.Euler(55,-28,0);
                light.shadows=LightShadows.Soft;light.shadowStrength=.52f;
            }
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            Validate();
        }

        static Transform Group(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static Mesh Mesh(string name)
        {
            var result=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ApprovedMaps/Meshes/"+name+".asset");
            if(result==null)throw new InvalidOperationException("Saved map mesh missing: "+name);return result;
        }
        static Material Mat(string name,Color color,string texture,float gloss,float metallic=0,bool glow=false)
        {
            string path=Assets+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created=m==null;
            if(created)m=new Material(Shader.Find("Standard")){name="Formation "+name};
            m.color=color;m.enableInstancing=true;
            m.SetFloat("_Glossiness",gloss);m.SetFloat("_Metallic",metallic);
            if(texture!=null)
            {
                m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+texture);
                if(m.mainTexture==null)throw new InvalidOperationException("Industrial texture missing: "+texture);
                string normal=texture.Replace("-color","-normal");
                if(normal!=texture&&File.Exists(TextureRoot+normal))
                {m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+normal));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.45f);}
            }
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.85f);}
            if(created)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material mat)=>Shape(parent,name,cube,p,size,mat);
        static Transform Shape(Transform parent,string name,Mesh mesh,Vector3 p,Vector3 scale,Material mat)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;return go.transform;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
        {var t=Box(parent,name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
        static void Lamp(Transform parent,Vector3 p,float width,bool warm)
        {
            Box(parent,"Lamp steel housing",p,new Vector3(width+.1f,.085f,.12f),dark);
            Box(parent,"Inset practical light",p+new Vector3(0,-.048f,-.015f),new Vector3(width,.025f,.09f),warm?amber:white);
            var light=new GameObject("Practical illumination",typeof(Light));light.transform.SetParent(parent,false);light.transform.localPosition=p+new Vector3(0,-.12f,-.18f);
            var l=light.GetComponent<Light>();l.type=LightType.Point;l.range=3;l.intensity=warm?1.6f:.65f;l.color=warm?new Color(1,.65f,.28f):new Color(.5f,.75f,1);l.shadows=LightShadows.None;
        }
        static void Warehouse(Transform parent,float x)
        {
            var g=Group(parent,"Loading bay "+x);g.localPosition=new Vector3(x,0,4.0f);
            Box(g,"Concrete warehouse shell",new Vector3(0,.64f,.2f),new Vector3(3.10f,1.28f,1.25f),wall);
            Box(g,"Shutter black recess",new Vector3(0,.55f,-.44f),new Vector3(2.31f,1.08f,.055f),dark);
            for(int i=0;i<14;i++)Box(g,"Roller shutter rib",new Vector3(0,.075f+i*.071f,-.48f),new Vector3(2.12f,.052f,.035f),steel);
            foreach(float side in new[]{-1.19f,1.19f})
            {
                Box(g,"Door jamb",new Vector3(side,.58f,-.49f),new Vector3(.13f,1.16f,.14f),edge);
                for(int i=0;i<4;i++)
                {
                    var warning=Box(g,"Jamb caution stripe",new Vector3(side,.17f+i*.16f,-.565f),new Vector3(.14f,.064f,.012f),amber);
                    warning.localRotation=Quaternion.Euler(0,0,25);
                }
            }
            Box(g,"Warehouse roof rim",new Vector3(0,1.31f,.19f),new Vector3(3.24f,.10f,1.35f),edge);
            Lamp(g,new Vector3(0,1.14f,-.55f),1.42f,false);
            Box(g,"Facade corner column",new Vector3(1.52f,.78f,-.40f),new Vector3(.16f,1.56f,.20f),wall);
            Lamp(g,new Vector3(1.53f,1.53f,-.48f),.32f,true);
        }
        static void Fence(Transform root,Vector3 p,float length,bool longitudinal)
        {
            var g=Group(root,"Low service railing");g.localPosition=p;if(longitudinal)g.localRotation=Quaternion.Euler(0,90,0);
            foreach(float x in new[]{-length/2,0,length/2})
            {Box(g,"Rail footing",new Vector3(x,.04f,0),new Vector3(.12f,.08f,.17f),dark);Box(g,"Rail stanchion",new Vector3(x,.23f,0),new Vector3(.045f,.46f,.045f),amber);}
            foreach(float y in new[]{.20f,.45f})Box(g,"Horizontal safety rail",new Vector3(0,y,0),new Vector3(length,.032f,.032f),amber);
        }
        static void Crane(Transform root)
        {
            var g=Group(root,"Compact freight gantry crane");g.localPosition=new Vector3(-6.75f,0,4.04f);
            foreach(float x in new[]{-1.4f,1.4f})
            {
                Box(g,"Crane footing",new Vector3(x,.12f,0),new Vector3(.44f,.24f,.80f),steel);
                Box(g,"Gantry column",new Vector3(x,1.02f,0),new Vector3(.16f,1.8f,.19f),amber);
                Beam(g,"Crane brace",new Vector3(x,1.78f,0),new Vector3(x+Mathf.Sign(-x)*.47f,1.97f,0),.11f,steel);
            }
            Box(g,"Gantry overhead beam",new Vector3(0,1.98f,0),new Vector3(3.15f,.22f,.34f),amber);
            Box(g,"Hoist trolley",new Vector3(-.35f,1.84f,0),new Vector3(.38f,.17f,.37f),dark);
            Beam(g,"Hoist cable",new Vector3(-.35f,1.8f,0),new Vector3(-.35f,1.10f,0),.025f,steel);
            var crate=Prop(g,"Prop_Crate3.fbx",new Vector3(-.35f,.35f,0),.65f);
            if(crate!=null)Beam(g,"Suspension cable",new Vector3(-.35f,1.1f,0),new Vector3(-.35f,.94f,0),.025f,steel);
        }
        static Transform Prop(Transform root,string file,Vector3 position,float maximum)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Industrial/Models/"+file);if(source==null)return null;
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.name="Reused industrial "+Path.GetFileNameWithoutExtension(file);instance.transform.SetParent(root,false);
            instance.transform.localPosition=Vector3.zero;var renders=instance.GetComponentsInChildren<Renderer>();if(renders.Length==0)return instance.transform;
            var bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
            instance.transform.localScale*=maximum/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            foreach(var r in renders)r.sharedMaterials=r.sharedMaterials.Select(_=>steel).ToArray();
            bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
            Vector3 world=root.TransformPoint(position);instance.transform.position+=world-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var c in instance.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            return instance.transform;
        }
        static void Gate(Transform parent,string color,float col,int row,Material mat)
        {
            var g=Group(parent,color+" gate row "+row);var p=FormationBattleController.WorldPoint(new Vector2(col,row));p.y=.02f;g.localPosition=p;
            // A narrow open cuboid symbol; no collider, no solid door, no route or spawn logic.
            float w=.72f,d=.68f,h=.72f;
            foreach(float x in new[]{-w/2,w/2})foreach(float z in new[]{-d/2,d/2})
                Box(g,"Open gate vertical",new Vector3(x,h/2,z),new Vector3(.018f,h,.018f),mat);
            foreach(float y in new[]{.012f,h})
            {
                foreach(float x in new[]{-w/2,w/2})Box(g,"Open gate depth frame",new Vector3(x,y,0),new Vector3(.018f,.018f,d),mat);
                foreach(float z in new[]{-d/2,d/2})Box(g,"Open gate lintel",new Vector3(0,y,z),new Vector3(w,.018f,.018f),mat);
            }
            Beam(g,"Gate symbol triangle",new Vector3(-.13f,h+.025f,-.25f),new Vector3(.13f,h+.025f,-.25f),.014f,mat);
            Beam(g,"Gate symbol triangle",new Vector3(.13f,h+.025f,-.25f),new Vector3(0,h+.025f,.20f),.014f,mat);
            Beam(g,"Gate symbol triangle",new Vector3(0,h+.025f,.20f),new Vector3(-.13f,h+.025f,-.25f),.014f,mat);
        }
        public static void Validate()
        {
            FormationBattleTools.Validate();
            var root=SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(root==null)throw new InvalidOperationException("Saved industrial courtyard missing.");
            var all=root.GetComponentsInChildren<Transform>();
            foreach(string color in new[]{"Red","Blue"})for(int row=0;row<8;row++)
            {
                var gate=all.Single(t=>t.name==color+" gate row "+row);
                var p=FormationBattleController.WorldPoint(new Vector2(color=="Red"?5.5f:13,row));p.y=.02f;
                if(Vector3.Distance(p,gate.position)>.001f||gate.GetComponentsInChildren<Collider>().Any(c=>c.enabled))throw new InvalidOperationException("Gate alignment or passage invalid.");
            }
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())foreach(var material in r.sharedMaterials)
                if(material==null||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))throw new InvalidOperationException("Unsaved courtyard material.");
            Debug.Log("FORMATION_INDUSTRIAL_VALIDATE_PASS: 112 cells, 8 red gates, 8 blue gates, saved open 3D courtyard");
        }
    }
}
