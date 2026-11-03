using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class StrongholdMapTools
    {
        public static void Build()
        {
            if(!File.Exists(ShopMapTools.ScenePath))throw new InvalidOperationException("Independent shop scene required.");
            Directory.CreateDirectory("Builds/StrongholdMap");
            var report=BuildPipeline.BuildPlayer(new[]{ShopMapTools.ScenePath},"Builds/StrongholdMap/YesterdayConvoy-Stronghold.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Stronghold build failed: "+report.summary.result);
            Debug.Log("STRONGHOLD_BUILD_PASS: "+report.summary.totalSize+" bytes; errors="+report.summary.totalErrors);
        }
    }
}
