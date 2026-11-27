using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy.Editor
{
    /// <summary>Local numeric iteration after a full build; never use after art/scene/serialized-layout changes.</summary>
    public static class FormationBalanceBuildTools
    {
        public static void BuildCodeOnly()
        {
            const string output="Builds/Windows/YesterdayConvoy.exe";
            if(!File.Exists(output)||!Directory.Exists("Builds/Windows/YesterdayConvoy_Data/Managed"))
                throw new InvalidOperationException("A successful full Windows build is required first.");
            const BuildTarget target=BuildTarget.StandaloneWindows64;
            bool automatic=PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            var apis=PlayerSettings.GetGraphicsAPIs(target);BuildReport report;
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);
                PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D11});
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=GameSceneCatalog.BuildPaths(),
                    target=target,locationPathName=output,options=BuildOptions.Development|BuildOptions.BuildScriptsOnly});
            }
            finally {PlayerSettings.SetGraphicsAPIs(target,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,automatic);}
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Code-only build failed: "+report.summary.result);
            Debug.Log("BALANCE_CODE_BUILD_PASS: "+report.summary.totalSize+" bytes; "+report.summary.totalErrors+" errors. Reuses previously verified player data.");
        }
    }
}
