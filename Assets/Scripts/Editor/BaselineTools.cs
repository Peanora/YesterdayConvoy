using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class BaselineTools
    {
        public const string EntryScene=FormationBattleTools.ScenePath;
        [MenuItem("Yesterday Convoy/Baseline/Initialize")]
        public static void Initialize() => FormationBattleTools.Author();
        [MenuItem("Yesterday Convoy/Baseline/Validate")]
        public static void Validate()
        {
            FormationCampaignTools.Validate();
            FormationRosterTools.Validate();
        }

        [MenuItem("Yesterday Convoy/Baseline/Build Windows")]
        public static void BuildWindows()
        {
            PreparedArtTools.PrepareAll();
            Validate();
            GameSceneCatalog.UpdateBuildSettings();
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GameSceneCatalog.BuildPaths(),
                locationPathName = "Builds/Windows/YesterdayConvoy.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("BASELINE_BUILD_PASS: " + report.summary.totalSize + " bytes; " + report.summary.totalErrors + " errors.");
        }
    }
}
