using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

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
            // A battle crash was captured in GfxDeviceD3D12Base::SetTextures on
            // Unity 6000.6.5f1. Ship the D3D11 backend verified in Player,
            // rather than requiring a launch argument on each recipient's machine.
            const BuildTarget target = BuildTarget.StandaloneWindows64;
            bool previousDefault = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            var previousApis = PlayerSettings.GetGraphicsAPIs(target);
            BuildReport report;
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
                Debug.Log("BASELINE_WINDOWS_GRAPHICS: Direct3D11 (combat-verified native FX backend).");
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GameSceneCatalog.BuildPaths(),
                    locationPathName = "Builds/Windows/YesterdayConvoy.exe",
                    target = target,
                    options = BuildOptions.Development
                });
            }
            finally
            {
                PlayerSettings.SetGraphicsAPIs(target, previousApis);
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, previousDefault);
            }
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("BASELINE_BUILD_PASS: " + report.summary.totalSize + " bytes; " + report.summary.totalErrors + " errors.");
        }
    }
}
