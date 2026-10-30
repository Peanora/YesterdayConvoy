using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class ShopMapTools
    {
        public const string ScenePath="Assets/_Game/ShopTrials/ShopMap.unity";
        [MenuItem("Yesterday Convoy/Shop Trial/Prepare Independent Map")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/_Game/ShopTrials");AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(InitialDemoTools.ScenePath);
            new GameObject("Shop map trial entry").AddComponent<ShopMapBootstrap>();
            if(!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath))throw new InvalidOperationException("Cannot save shop trial scene");
            AssetDatabase.SaveAssets();Debug.Log("SHOP_MAP_SCENE_READY");
        }
        public static void Build()
        {
            if(!File.Exists(ScenePath))Prepare();
            Directory.CreateDirectory("Builds/ShopMap");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/ShopMap/YesterdayConvoy-Shop.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Shop build failed: "+report.summary.result);
            Debug.Log("SHOP_MAP_BUILD_PASS: "+report.summary.totalSize+" bytes; errors="+report.summary.totalErrors);
        }
    }
}
