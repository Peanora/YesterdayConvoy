using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace YesterdayConvoy.Editor
{
    // All encounters use the same authored board.
    public static class GameSceneCatalog
    {
        public static string[] BuildPaths()
        {
            string scene=FormationBattleTools.ScenePath;
            if(!File.Exists(scene))throw new InvalidOperationException("Missing formation scene: "+scene);
            return new[]{scene};
        }
        public static void UpdateBuildSettings()=>EditorBuildSettings.scenes=BuildPaths().Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
    }
}
