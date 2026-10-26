using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class GuerrillaTrialTools
    {
        const string Scene="Assets/_Game/GuerrillaTrials/GuerrillaCommand.unity";
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/_Game/GuerrillaTrials");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/_Game/MapPreviews/01-IndustrialTransfer.unity");
            foreach(var nav in UnityEngine.Object.FindObjectsByType<MapPreviewNavigation>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(nav.gameObject);
            var c=new GameObject("Guerrilla command trial").AddComponent<GuerrillaTrialController>();c.route=MapPreviewTools.RouteFor(0);
            c.roles=new[]{"M03","M15","M16","M17","M18","M19"}.Select(id=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/_Game/Data/Resources/MonsterTests/"+id+".asset")).ToArray();
            c.interfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
            if(c.roles.Any(r=>r==null))throw new InvalidOperationException("Missing authored guerrilla role.");
            EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("GUERRILLA_SCENE_PASS: independent authored command trial");
        }
        public static void Build()
        {
            var r=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/GuerrillaTrials/YesterdayConvoy-Guerrillas.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Guerrilla build failed");
            Debug.Log("GUERRILLA_BUILD_PASS: "+r.summary.totalSize+" bytes; 0 errors.");
        }
    }
}
