using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Owns the industrial map presentation only. Reads the existing route without changing it.</summary>
    public static partial class TacticalMapTools
    {
        public const string RootName = "Industrial Map";
        private const string Art = "Assets/_Game/Art/Environment/Industrial/";
        private const string Materials = "Assets/_Game/Art/Materials/Industrial/";
        private static Material concrete, asphalt, steel, dark, amber, cyan, red, blue;
        private static Material trim1, trim2;

        [MenuItem("Yesterday Convoy/Initial Demo/Apply Tactical Map")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Materials);
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Art + "Textures"))
            {
                if (path.EndsWith(".meta")) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = path.Contains("normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }
            foreach (string path in Directory.GetFiles(Art + "Models", "*.fbx"))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = false;
                importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
            }
            concrete = Mat("Concrete", new Color(0.34f,0.40f,0.46f), "concrete-color.jpg");
            asphalt = Mat("Asphalt", new Color(0.32f,0.38f,0.44f), "asphalt-color.jpg");
            steel = Mat("Steel", new Color(0.22f,0.28f,0.34f));
            dark = Mat("Recess", new Color(0.07f,0.095f,0.12f));
            amber = Mat("Safety Amber", new Color(0.46f,0.43f,0.27f), unlit:true, opaque:true);
            cyan = Mat("Guide Cyan", new Color(0.14f,0.57f,0.69f), unlit:true, opaque:true);
            red = Mat("Entry Red", new Color(0.95f,0.22f,0.24f), unlit:true);
            blue = Mat("Target Blue", new Color(0.16f,0.64f,0.94f), unlit:true);
            trim1 = Mat("Trim 01", new Color(0.44f,0.52f,0.59f), "trim-01-color.png", "trim-01-normal.png");
            trim2 = Mat("Trim 02", new Color(0.44f,0.52f,0.59f), "trim-02-color.png", "trim-02-normal.png");

            var scene = EditorSceneManager.OpenScene(InitialDemoTools.ScenePath, OpenSceneMode.Single);
            var demo = UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var data = new SerializedObject(demo);
            var route = data.FindProperty("waypoints");
            Vector3[] points = Enumerable.Range(0, route.arraySize).Select(i => route.GetArrayElementAtIndex(i).vector3Value).ToArray();
            foreach (MeshRenderer renderer in GameObject.Find("Raised Tactical Board").GetComponentsInChildren<MeshRenderer>())
                renderer.enabled = false;
            Transform map = RootNameObject(RootName, null);
            BuildDiorama(map,points);
            TacticalGate((Transform)data.FindProperty("redGate").objectReferenceValue,red);
            TacticalGate((Transform)data.FindProperty("blueGate").objectReferenceValue,blue);
            ContactShadow((Transform)data.FindProperty("monster").objectReferenceValue,0.9f,0.48f,-0.025f);
            ContactShadow((Transform)data.FindProperty("tower").objectReferenceValue,1.35f,0.8f,0.015f);
            ConfigureReferenceCamera();
            // The card is now a screen-space roster entry. Preserve its prefab and asset references.
            foreach(var view in UnityEngine.Object.FindObjectsByType<DemoCardView>())
                foreach(var renderer in view.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f,0.39f,0.42f);
            var keyLight=GameObject.Find("Key Light")?.GetComponent<Light>();
            if(keyLight!=null)
            {
                keyLight.intensity=0.95f;
                keyLight.transform.rotation=Quaternion.Euler(48,-32,0);
                keyLight.shadows=LightShadows.Soft;
                keyLight.shadowStrength=0.55f;
                keyLight.shadowBias=0.04f;
                keyLight.color=new Color(.95f,.97f,1);
            }
            Camera.main.backgroundColor = new Color(0.035f,0.05f,0.07f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Validate();
        }

        public static void Validate()
        {
            DemoArtTools.Validate();
        }

        [MenuItem("Yesterday Convoy/Initial Demo/Align Camera to Reference")]
        public static void ApplyCamera()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
            ConfigureReferenceCamera();
            EditorSceneManager.SaveScene(scene);
            Validate();
        }

        private static void ConfigureReferenceCamera()
        {
            // Estimated from the supplied image's square tiles and near/far size change, not original-game data.
            Camera camera=Camera.main;
            camera.orthographic=false;
            camera.usePhysicalProperties=false;
            camera.fieldOfView=24;
            camera.transform.rotation=Quaternion.Euler(60,0,0);
            camera.transform.position=new Vector3(0,0,0.7f)-camera.transform.forward*24;
            foreach(var visual in UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>())
            {
                visual.transform.rotation=camera.transform.rotation;
                visual.Renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,visual.transform.parent.position);
            }
        }

        public static void ValidatePresentation()
        {
            Transform root = GameObject.Find(RootName)?.transform;
            if (root == null || root.Find("Foundation") == null || root.GetComponentsInChildren<Collider>().Length != 0)
                throw new InvalidOperationException("Tactical map: missing presentation or unexpected gameplay colliders.");
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name.Contains("Error")))
                    throw new InvalidOperationException("Tactical map: missing shader/material on " + renderer.name);
            foreach(string name in new[]{"Safety Amber","Guide Cyan"})
            {
                var paint=AssetDatabase.LoadAssetAtPath<Material>(Materials+name+".mat");
                if(paint==null || paint.renderQueue>=2500)
                    throw new InvalidOperationException("Tactical map: solid ground paint must use the opaque depth-writing pass: "+name);
            }
            var demo = UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var data = new SerializedObject(demo);
            var route = data.FindProperty("waypoints");
            for (int i=0;i<route.arraySize;i++)
            {
                Vector3 point=route.GetArrayElementAtIndex(i).vector3Value;
                Transform cell=root.Find($"Cell {Mathf.RoundToInt(point.x)},{Mathf.RoundToInt(point.z)}");
                if (cell == null || cell.position.y+cell.localScale.y*0.5f>0.01f)
                    throw new InvalidOperationException("Tactical map: raised terrain overlaps an existing route endpoint.");
            }
            Camera camera=Camera.main;
            float pitch=camera.transform.eulerAngles.x;
            if(camera.orthographic || pitch<59 || pitch>61 || Mathf.Abs(camera.fieldOfView-24)>0.01f ||
                Mathf.Abs(camera.transform.eulerAngles.y)>0.01f || Mathf.Abs(camera.transform.eulerAngles.z)>0.01f)
                throw new InvalidOperationException("Tactical map: expected frontal 60-degree perspective view with 24-degree field of view.");
            // Test the actual ground-plane intersections of the full viewport, including HUD-covered corners.
            var ground=new Plane(Vector3.up,new Vector3(0,-0.25f,0));
            foreach(Vector3 corner in new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.one})
            {
                Ray ray=camera.ViewportPointToRay(corner);
                if(!ground.Raycast(ray,out float distance))throw new InvalidOperationException("Tactical map: viewport misses ground.");
                Vector3 hit=ray.GetPoint(distance);
                if(Mathf.Abs(hit.x)>19 || Mathf.Abs(hit.z)>19)throw new InvalidOperationException("Tactical map: yard does not fill the frame.");
            }
            Debug.Log("TACTICAL_MAP_PASS: full-frame map; 60-degree frontal perspective; FOV24; original route; no colliders; valid materials.");
        }

        private static bool IsOnRoute(int x,int z,Vector3[] route)
        {
            for(int i=1;i<route.Length;i++)
            {
                Vector3 a=route[i-1],b=route[i];
                if (a.x==b.x && x==a.x && z>=Mathf.Min(a.z,b.z) && z<=Mathf.Max(a.z,b.z) ||
                    a.z==b.z && z==a.z && x>=Mathf.Min(a.x,b.x) && x<=Mathf.Max(a.x,b.x)) return true;
            }
            return false;
        }

        private static Transform RootNameObject(string name,Transform parent)
        {
            Transform existing=parent==null ? GameObject.Find(name)?.transform : parent.Find(name);
            if(existing!=null)return existing;
            var root=new GameObject(name).transform;
            root.SetParent(parent,false);
            return root;
        }

        private static void Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            Transform child=parent.Find(name);
            if(child==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name=name;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                child=go.transform;
                child.SetParent(parent,false);
            }
            child.gameObject.SetActive(true);
            child.localPosition=position;
            child.localScale=scale;
            child.GetComponent<Renderer>().sharedMaterial=material;
            child.GetComponent<Renderer>().enabled=true;
            child.gameObject.isStatic=true;
        }

        private static void Prop(string name,string model,Transform parent,Vector3 position,float width,float height,float yaw)
        {
            Transform child=parent.Find(name);
            if(child==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/"+model);
                if(source==null)throw new InvalidOperationException("Missing selected model: "+model);
                child=((GameObject)PrefabUtility.InstantiatePrefab(source,parent)).transform;
                child.name=name;
            }
            child.gameObject.SetActive(true);
            child.localPosition=Vector3.zero;
            child.localRotation=Quaternion.Euler(0,yaw,0);
            child.localScale=Vector3.one;
            var renderers=child.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float factor=Mathf.Min(width/Mathf.Max(bounds.size.x,bounds.size.z),height/Mathf.Max(0.001f,bounds.size.y));
            child.localScale=Vector3.one*factor;
            bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            child.localPosition=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m!=null && (m.name.Contains("02") || m.name==trim2.name) ? trim2 : trim1).ToArray();
                renderer.gameObject.isStatic=true;
            }
        }

        private static void Pipe(string name,Transform parent,Vector3 position,float length)
        {
            Transform child=parent.Find(name);
            if(child==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name=name;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                child=go.transform;
                child.SetParent(parent,false);
            }
            child.gameObject.SetActive(true);
            child.localPosition=position;
            child.localRotation=Quaternion.Euler(0,0,90);
            child.localScale=new Vector3(0.13f,length*0.5f,0.13f);
            child.GetComponent<Renderer>().sharedMaterial=steel;
            child.gameObject.isStatic=true;
        }

        private static void ContactShadow(Transform owner,float width,float depth,float height)
        {
            Transform child=owner.Find("Contact shadow");
            if(child==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name="Contact shadow";
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                child=go.transform;
                child.SetParent(owner,false);
            }
            child.localPosition=new Vector3(0,height,0);
            child.localScale=new Vector3(width,0.003f,depth);
            var renderer=child.GetComponent<MeshRenderer>();
            renderer.enabled=true;
            renderer.sharedMaterial=Mat("Contact Shadow",new Color(0.015f,0.025f,0.04f,0.32f),unlit:true);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }

        private static void TacticalGate(Transform owner,Material color)
        {
            owner.Find("Series Sprite").GetComponent<SpriteRenderer>().enabled=false;
            Transform root=RootNameObject("Tactical Gate",owner);
            Shape("Threshold",root,new Vector3(0,0.01f,0),new Vector3(0.96f,0.04f,0.96f),dark);
            for(int level=0;level<2;level++)
            {
                float y=0.055f+level*0.78f;
                Stroke("Outline "+level,root,new[]{new Vector3(-0.47f,y,-0.47f),new Vector3(-0.47f,y,0.47f),new Vector3(0.47f,y,0.47f),new Vector3(0.47f,y,-0.47f),new Vector3(-0.47f,y,-0.47f)},color,0.027f);
            }
            for(int i=0;i<4;i++)
            {
                float x=i<2?-0.47f:0.47f,z=i%2==0?-0.47f:0.47f;
                Stroke("Post "+i,root,new[]{new Vector3(x,0.055f,z),new Vector3(x,0.835f,z)},color,0.022f);
            }
            Stroke("Through chevron",root,new[]{new Vector3(-0.23f,0.045f,0.14f),new Vector3(0,0.045f,-0.10f),new Vector3(0.23f,0.045f,0.14f)},color,0.045f);
        }

        private static void Stroke(string name,Transform parent,Vector3[] points,Material material,float width)
        {
            Transform child=RootNameObject(name,parent);
            var line=child.GetComponent<LineRenderer>();
            if(line==null)line=child.gameObject.AddComponent<LineRenderer>();
            child.gameObject.SetActive(true);
            line.useWorldSpace=false;
            line.sharedMaterial=material;
            line.widthMultiplier=width;
            line.positionCount=points.Length;
            line.SetPositions(points);
        }

        private static Material Mat(string name,Color color,string texture=null,string normal=null,bool unlit=false,bool opaque=false)
        {
            string path=Materials+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader=Shader.Find(unlit ? (opaque ? "Unlit/Color" : "Sprites/Default") : "Standard");
            if(material==null)
            {
                material=new Material(shader);
                AssetDatabase.CreateAsset(material,path);
            }
            else if(material.shader!=shader)material.shader=shader;
            material.color=color;
            if(!unlit)
            {
                material.SetFloat("_Glossiness",0.08f);
                material.mainTexture=texture==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/"+texture);
                if(normal!=null)
                {
                    material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/"+normal));
                    material.SetFloat("_BumpScale",0.4f);
                    material.EnableKeyword("_NORMALMAP");
                }
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
