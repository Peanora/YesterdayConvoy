using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class InitialDemoTools
    {
        public const string ScenePath="Assets/_Game/Scenes/InitialDemo.unity";
        [MenuItem("Yesterday Convoy/Initial Demo/Open Current Scene")]
        public static void Initialize()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!File.Exists(ScenePath)) throw new InvalidOperationException("Restore the current scene from Git; legacy generation has been removed.");
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            Validate();
        }

        public static void Validate()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath) EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var demos=UnityEngine.Object.FindObjectsByType<InitialDemo>();
            if(demos.Length!=1 || Camera.main==null) throw new InvalidOperationException("Expected one current battle scene and camera.");
            var source=demos[0];
            if(source.Monster==null || source.RedGate==null || source.BlueGate==null || source.Card==null || source.InterfaceFont==null)
                throw new InvalidOperationException("Missing current battle scene binding.");
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var child in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject)!=0)
                        throw new InvalidOperationException("Missing script on "+child.name);
            ManualDeploymentTools.Validate();
            Debug.Log("INITIAL_DEMO_SCENE_PASS: current comic template, authored Kroos, gates, route and camera.");
        }
    }
}
