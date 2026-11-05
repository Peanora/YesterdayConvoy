using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YesterdayConvoy.Editor
{
    public static class SceneOrganizationTools
    {
        const string MapFolder="Assets/Prefabs/Resources/Maps";
        [MenuItem("Yesterday Convoy/Scenes/Open Monster Trial")]
        public static void OpenMonster()=>EditorSceneManager.OpenScene(InitialDemoTools.ScenePath);
        [MenuItem("Yesterday Convoy/Scenes/Open Battle and Shop")]
        public static void OpenShop()=>EditorSceneManager.OpenScene(ShopMapTools.ScenePath);
        [MenuItem("Yesterday Convoy/Scenes/New Map Scene")]
        public static void NewMap()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string path=EditorUtility.SaveFilePanelInProject("New map scene","NewMap","unity","Save each new map as a scene asset","Assets/Scenes");
            if(string.IsNullOrEmpty(path))return;
            if(!path.StartsWith("Assets/Scenes/",StringComparison.Ordinal))throw new InvalidOperationException("Map scenes belong in Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,path);
        }
        public static void FinalizePrimary(){Migrate();BaselineTools.BuildWindows();}
        public static void CaptureAndBuild()
        {
            Validate();Directory.CreateDirectory("artifacts");
            foreach(string path in new[]{InitialDemoTools.ScenePath,ShopMapTools.ScenePath})
            {
                EditorSceneManager.OpenScene(path);var camera=Camera.main;
                var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("artifacts/"+Path.GetFileNameWithoutExtension(path)+"-authored.png",image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
            }
            StrongholdMapTools.Build();
        }
        // Explicit, one-time migration. Never called on scene load or Play.
        public static void Migrate()
        {
            Directory.CreateDirectory(MapFolder);Directory.CreateDirectory("Assets/Data/Resources/Maps");Directory.CreateDirectory("Assets/Art/Environment/BattleShop");AssetDatabase.Refresh();
            var map=Resources.Load<DefenderTrialDefinition>("Maps/BattleShop");
            if(map==null)throw new InvalidOperationException("Restore Maps/BattleShop.asset from Git; migration already saved the authored definition.");
            var scene=EditorSceneManager.OpenScene(ShopMapTools.ScenePath);
            var root=UnityEngine.Object.FindAnyObjectByType<AuthoredBattlefield>(FindObjectsInactive.Include);
            if(root==null)
            {
                var go=new GameObject("BattleShopEnvironment");go.AddComponent<AuthoredBattlefield>();
                var baker=new BattleShopMapBaker(go);baker.Build(map);
                int mat=0,mesh=0;
                foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&!AssetDatabase.Contains(material))AssetDatabase.CreateAsset(material,"Assets/Art/Environment/BattleShop/Surface-"+(mat++)+".mat");
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh!=null&&!AssetDatabase.Contains(filter.sharedMesh))AssetDatabase.CreateAsset(filter.sharedMesh,"Assets/Art/Environment/BattleShop/Mesh-"+(mesh++)+".asset");
                PrefabUtility.SaveAsPrefabAsset(go,MapFolder+"/BattleShopEnvironment.prefab");
            }
            var industrial=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Industrial Map");if(industrial!=null)UnityEngine.Object.DestroyImmediate(industrial);
            var bindings=UnityEngine.Object.FindAnyObjectByType<InitialDemo>();bindings.RedGate.gameObject.SetActive(false);bindings.BlueGate.gameObject.SetActive(false);
            foreach(var actor in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))actor.enabled=false;
            var camera=Camera.main;camera.transform.rotation=Quaternion.Euler(60,0,0);camera.transform.position=camera.transform.rotation*new Vector3(0,-1.7f,-35)+new Vector3(0,0,.7f);camera.fieldOfView=24;
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            GameSceneCatalog.UpdateBuildSettings();
            Validate();Debug.Log("AUTHORED_SCENES_MIGRATION_PASS");
        }
        public static void Validate()
        {
            var scenes=Directory.GetFiles("Assets","*.unity",SearchOption.AllDirectories);
            if(!File.Exists(InitialDemoTools.ScenePath)||!File.Exists(ShopMapTools.ScenePath))throw new InvalidOperationException("Required authored scene missing");
            if(scenes.Any(path=>!path.Replace('\\','/').StartsWith("Assets/Scenes/",StringComparison.Ordinal)))throw new InvalidOperationException("Every map scene must be in Assets/Scenes");
            foreach(string path in scenes)
            {
                var scene=EditorSceneManager.OpenScene(path);
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new InvalidOperationException("Missing script: "+t.name);
                if(path.Replace('\\','/').EndsWith("BattleShop.unity")&&UnityEngine.Object.FindAnyObjectByType<AuthoredBattlefield>(FindObjectsInactive.Include)==null)throw new InvalidOperationException("Authored shop geometry missing");
            }
            Debug.Log("AUTHORED_SCENES_VALIDATION_PASS: saved geometry, no missing scripts");
        }
    }
}
