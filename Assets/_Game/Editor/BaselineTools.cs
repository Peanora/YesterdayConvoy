using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class BaselineTools
    {
        public const string EntryScene=InitialDemoTools.ScenePath;
        [MenuItem("Yesterday Convoy/Baseline/Initialize")]
        public static void Initialize() => InitialDemoTools.Initialize();
        [MenuItem("Yesterday Convoy/Baseline/Validate")]
        public static void Validate() => InitialDemoTools.Validate();

        [MenuItem("Yesterday Convoy/Baseline/Build Windows")]
        public static void BuildWindows()
        {
            if (File.Exists(DemoArtTools.CardPath)) DemoArtTools.Validate();
            else InitialDemoTools.Validate();
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { InitialDemoTools.ScenePath },
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
