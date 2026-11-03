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
        public const string ScenePath="Assets/Scenes/BattleShop.unity";
        [MenuItem("Yesterday Convoy/Shop Trial/Prepare Independent Map")]
        public static void Prepare()
        {
            if(!File.Exists(ScenePath))throw new InvalidOperationException("Restore BattleShop.unity from Git; scenes are authored assets.");
            EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if(!File.Exists(ScenePath))Prepare();
            Directory.CreateDirectory("Builds/ShopMap");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath,InitialDemoTools.ScenePath},"Builds/ShopMap/YesterdayConvoy-Shop.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Shop build failed: "+report.summary.result);
            Debug.Log("SHOP_MAP_BUILD_PASS: "+report.summary.totalSize+" bytes; errors="+report.summary.totalErrors);
        }
    }
}
