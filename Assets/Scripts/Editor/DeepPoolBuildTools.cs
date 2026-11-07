using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DeepPoolBuildTools
    {
        // Use the existing saved scene catalog without modifying EditorBuildSettings or the open Player.
        public static void BuildWindows()
        {
            PreparedArtTools.PrepareAll();
            InitialDemoTools.Validate();
            const string directory="Builds/Windows-DeepPool";
            Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=GameSceneCatalog.BuildPaths(),
                locationPathName=directory+"/YesterdayConvoy.exe",
                target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException("Deep-pool build failed: "+report.summary.result);
            Debug.Log("DEEP_POOL_BUILD_PASS: "+report.summary.totalSize+" bytes; "+report.summary.totalErrors+" errors.");
        }
    }
}
