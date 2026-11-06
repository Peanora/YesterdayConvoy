using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ArtLoadingBenchmarkTests
    {
        [Serializable]sealed class Manifest{public Clip[] clips;}
        [Serializable]sealed class Clip{public string key;}
        [UnityTest]public IEnumerator ColdMenuAndYuPreparation()
        {
            var timer=Stopwatch.StartNew();yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var loading=c.GetType().GetProperty("ArtLoading");
            while(loading!=null&&(bool)loading.GetValue(c)){yield return null;}
            long menu=timer.ElapsedMilliseconds;long memory=Profiler.GetTotalAllocatedMemoryLong();
            timer.Restart();var art=DefenderArt.Load("D22");
            // Old Load eagerly loaded every action. Request every original action on the new lazy path too.
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("DefenderArt/D22/manifest.json")));
            foreach(var clip in manifest.clips)Assert.That(art[clip.key].Frames.Length,Is.GreaterThan(0));
            string label=Array.IndexOf(Environment.GetCommandLineArgs(),"-artPerfBaseline")>=0?"before":"after";
            string report="menu_ms="+menu+"\nyu_front_ms="+timer.ElapsedMilliseconds+"\nmenu_unity_allocated_bytes="+memory+"\n";
            Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/art-loading-"+label+".txt",report);UnityEngine.Debug.Log("ART_LOADING_BENCHMARK "+label+" "+report.Replace('\n',' '));
        }
    }
}
