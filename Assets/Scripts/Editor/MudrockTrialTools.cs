using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class MudrockTrialTools
    {
        public const string ScenePath="Assets/Scenes/MudrockTrial.unity";
        static void Set(UnityEngine.Object o,string key,Action<SerializedProperty> edit){var s=new SerializedObject(o);edit(s.FindProperty(key));s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
        [MenuItem("Yesterday Convoy/Scenes/Open Mudrock Gramophones")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
        public static void Create()
        {
            if(System.IO.File.Exists(ScenePath)){Debug.Log("MudrockTrial already authored; existing scene preserved.");return;}
            AssetDatabase.Refresh();
            const string data="Assets/Data/MudrockTrial.asset",combat="Assets/Data/MudrockEngagement.asset";
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(data)==null)AssetDatabase.CopyAsset("Assets/Data/Resources/MonsterTests/M28.asset",data);
            if(AssetDatabase.LoadAssetAtPath<EngagementDefinition>(combat)==null)AssetDatabase.CopyAsset("Assets/Data/Battles/M28.asset",combat);
            var d=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(data);var e=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(combat);
            Set(d,"engagement",p=>p.objectReferenceValue=e);Set(d,"unitHealth",p=>p.floatValue=6000);Set(d,"levelId",p=>p.stringValue="MUDROCK_GRAMOPHONE_TRIAL");
            Set(e,"defenderPosition",p=>p.vector3Value=new Vector3(0,.35f,0));Set(e,"defenderHealth",p=>p.floatValue=6000);Set(e,"defenderAttack",p=>p.floatValue=30);
            Set(e,"extraDefenders",p=>{p.arraySize=1;p.GetArrayElementAtIndex(0).vector3Value=new Vector3(2,.35f,0);});
            var scene=EditorSceneManager.OpenScene(InitialDemoTools.ScenePath);var controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
            Set(controller,"definition",p=>p.objectReferenceValue=d);var demo=UnityEngine.Object.FindAnyObjectByType<InitialDemo>();Set(demo,"card",p=>p.objectReferenceValue=d.Card);UnityEngine.Object.FindAnyObjectByType<DemoCardView>()?.SetCard(d.Card);
            var root=new GameObject("Authored gramophones");var boot=root.AddComponent<MudrockTrialBootstrap>();boot.devices=new GramophoneAnchor[3];
            string[] names={"west","east","neutral"};Vector3[] positions={new Vector3(0,.06f,1),new Vector3(3,.06f,0),new Vector3(-3,.06f,0)};
            var material=new Material(Shader.Find("Yesterday Convoy/Mudrock Original Baked"));const string matPath="Assets/Art/VFX/Mudrock/Resources/MudrockVfx/OriginalEffect.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(existing==null){AssetDatabase.CreateAsset(material,matPath);existing=material;}else UnityEngine.Object.DestroyImmediate(material);
            foreach(int i in Enumerable.Range(0,3)){var go=new GameObject("L-44 "+names[i]);go.transform.SetParent(root.transform);go.transform.position=positions[i];go.transform.rotation=Camera.main.transform.rotation;var anchor=go.AddComponent<GramophoneAnchor>();anchor.deviceId=names[i];anchor.initialCharge=i==1?100:50;boot.devices[i]=anchor;var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=MudrockVfxArt.Load("towerIdle_all").At(0);renderer.sharedMaterial=existing;
                // Store a persistent sprite subasset, never a temporary runtime sprite reference.
                var persistent=UnityEngine.Object.Instantiate(renderer.sprite);persistent.hideFlags=HideFlags.None;string spritePath="Assets/Art/VFX/Mudrock/TowerEditorSprite.asset";var saved=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);if(saved==null){AssetDatabase.CreateAsset(persistent,spritePath);saved=persistent;}else UnityEngine.Object.DestroyImmediate(persistent);renderer.sprite=saved;}
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{CampaignTools.CampaignScene,ShopMapTools.ScenePath,InitialDemoTools.ScenePath,CampaignTools.SandboxScene,ScenePath}.Select(x=>new EditorBuildSettingsScene(x,true)).ToArray();AssetDatabase.SaveAssets();Debug.Log("MUDROCK_AUTHORED_SCENE_PASS");
        }
    }
}
