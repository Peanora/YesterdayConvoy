using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Task-local output keeps the currently running Windows playtest untouched.</summary>
    public static class GuerrillaOrderBuildTools
    {
        public static void BuildWindows()
        {
            PreparedArtTools.PrepareAll();
            if(File.Exists(DemoArtTools.CardPath))DemoArtTools.Validate();else InitialDemoTools.Validate();
            string output="Builds/Windows-GuerrillaOrder/YesterdayConvoy.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=GameSceneCatalog.BuildPaths(),locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Guerrilla Windows build failed: "+report.summary.result);
            Debug.Log("GUERRILLA_ORDER_BUILD_PASS: "+report.summary.totalSize+" bytes; "+report.summary.totalErrors+" errors.");
        }
    }
}
