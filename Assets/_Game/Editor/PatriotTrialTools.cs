using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class PatriotTrialTools
    {
        const string Scene="Assets/_Game/GuerrillaTrials/Patriot.unity";
        public static void Generate()
        {
            var scene=AbilityFixtureTools.NewHost();
            var c=new GameObject("Independent Patriot trial").AddComponent<GuerrillaTrialController>();c.patriotTrial=true;c.route=AbilityFixtureTools.RouteFor(0);
            c.roles=new[]{"M20","M15","M16","M17","M18","M19"}.Select(id=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/_Game/Data/Resources/MonsterTests/"+id+".asset")).ToArray();
            c.interfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
            if(c.roles.Any(r=>r==null))throw new InvalidOperationException("Missing authored Patriot trial role.");
            EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("PATRIOT_SCENE_PASS");
        }
        public static void Build()
        {
            var r=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/PatriotTrial/YesterdayConvoy-Patriot.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Patriot build failed");
            Debug.Log("PATRIOT_BUILD_PASS: "+r.summary.totalSize+" bytes; 0 errors.");
        }
    }
}
