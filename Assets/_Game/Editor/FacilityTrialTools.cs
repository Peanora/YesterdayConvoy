using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class FacilityTrialTools
    {
        const string Folder="Assets/_Game/FacilityTrials";
        static readonly string[] Maps={"01-IndustrialTransfer","02-SnowRelay","03-ReedCanal"};
        [MenuItem("Yesterday Convoy/Facilities/Generate Trial Scenes")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int i=0;i<3;i++)
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Game/MapPreviews/"+Maps[i]+".unity");
                foreach(var nav in UnityEngine.Object.FindObjectsByType<MapPreviewNavigation>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(nav.gameObject);
                var controller=new GameObject("Facility combat trial").AddComponent<FacilityTrialController>();controller.mapIndex=i;
                controller.interfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
                string[] ids=i==0?new[]{"M19","M07","M29","M31","M32"}:i==1?new[]{"M14"}:new[]{"M28","M30","M26","M33"};
                controller.roles=ids.Select(id=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/_Game/Data/Resources/MonsterTests/"+id+".asset")).ToArray();
                if(controller.roles.Any(r=>r==null))throw new InvalidOperationException("Missing role trial definition");
                controller.route=MapPreviewTools.RouteFor(i);
                controller.deployableTiles=GameObject.Find(Maps[i]).GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Cell ")&&!controller.route.Any(p=>Mathf.RoundToInt(p.x)==Mathf.RoundToInt(t.position.x)&&Mathf.RoundToInt(p.z)==Mathf.RoundToInt(t.position.z))).Select(t=>new Vector3(t.position.x,0,t.position.z)).ToArray();
                EditorSceneManager.SaveScene(scene,Folder+"/"+Maps[i]+".unity");
            }
            AssetDatabase.SaveAssets();Debug.Log("FACILITY_SCENES_PASS: 3 independent battle scenes");
        }
        public static void Build()
        {
            var report=BuildPipeline.BuildPlayer(Maps.Select(m=>Folder+"/"+m+".unity").ToArray(),"Builds/FacilityTrials/YesterdayConvoy-Facilities.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Facility build failed");
            Debug.Log("FACILITY_BUILD_PASS: "+report.summary.totalSize+" bytes");
        }
    }
}
