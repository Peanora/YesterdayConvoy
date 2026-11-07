using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class SynergyActivationTools
    {
        // The ordinary Player may still be open. Keep its running installation intact.
        public static void BuildWindows()
        {
            PreparedArtTools.PrepareAll();
            InitialDemoTools.Validate();
            const string directory="Builds/Windows-SynergyActivation";
            Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=GameSceneCatalog.BuildPaths(),
                locationPathName=directory+"/YesterdayConvoy.exe",
                target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Synergy activation build failed: "+report.summary.result);
            Debug.Log("SYNERGY_ACTIVATION_BUILD_PASS: "+report.summary.totalSize+" bytes; "+report.summary.totalErrors+" errors.");
        }
    }
}
