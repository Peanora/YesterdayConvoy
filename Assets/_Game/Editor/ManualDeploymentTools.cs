using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class ManualDeploymentTools
    {
        public const string DefinitionPath="Assets/_Game/Data/Battles/ManualDemo.asset";

        [MenuItem("Yesterday Convoy/Initial Demo/Enable Manual Deployment")]
        public static void Initialize()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
            Directory.CreateDirectory("Assets/_Game/Data/Battles"); AssetDatabase.Refresh();
            var definition=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(DefinitionPath);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<ManualBattleDefinition>();
                var values=new SerializedObject(definition);
                values.FindProperty("card").objectReferenceValue=AssetDatabase.LoadAssetAtPath<CardDefinition>(DemoArtTools.CardPath);
                values.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition,DefinitionPath);
            }
            var source=UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var controller=source.GetComponent<ManualBattleController>();
            if(controller==null)controller=source.gameObject.AddComponent<ManualBattleController>();
            var bindings=new SerializedObject(controller);
            bindings.FindProperty("scene").objectReferenceValue=source;
            bindings.FindProperty("definition").objectReferenceValue=definition;
            bindings.ApplyModifiedPropertiesWithoutUndo();
            source.enabled=controller.enabled=true;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Validate();
        }

        public static void Validate()
        {
            var controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
            if(controller==null || !controller.enabled)throw new InvalidOperationException("Manual deployment controller missing or disabled.");
            controller.ValidateConfiguration();
            Debug.Log("MANUAL_BATTLE_CONFIG_PASS: one card; read-only trial definition; existing route and art.");
        }
    }
}
