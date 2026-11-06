using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace YesterdayConvoy.Editor
{
    // Assets/Scenes is the authored, shippable scene directory; test fixtures stay under Assets/Tests.
    public static class GameSceneCatalog
    {
        public static string[] BuildPaths()
        {
            string[] primary={CampaignTools.CampaignScene,ShopMapTools.ScenePath,InitialDemoTools.ScenePath,CampaignTools.SandboxScene,MudrockTrialTools.ScenePath};
            var authored=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.EndsWith(".unity",StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            foreach(string required in primary)if(!File.Exists(required))throw new InvalidOperationException("Missing primary scene: "+required);
            return primary.Concat(authored).Distinct(StringComparer.Ordinal).ToArray();
        }
        public static void UpdateBuildSettings()=>EditorBuildSettings.scenes=BuildPaths().Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
    }
}
