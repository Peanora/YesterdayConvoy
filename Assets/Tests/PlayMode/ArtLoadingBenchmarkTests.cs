using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ArtLoadingBenchmarkTests
    {
        [UnityTest]public IEnumerator ColdBoardAndFirstGuardFrames()
        {
            ArtMemoryCache.Clear();
            FormationShopSession.StartNew(20261117,fullCampaign:true);
            var timer=Stopwatch.StartNew();
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            var board=Object.FindAnyObjectByType<FormationBattleController>();
            Assert.That(board,Is.Not.Null);
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while((!board.Ready||!board.RosterArtReady)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(board.Ready&&board.RosterArtReady,Is.True);
            long boardTime=timer.ElapsedMilliseconds;
            long memory=Profiler.GetTotalAllocatedMemoryLong();
            int frameCount=0;
            foreach(var guard in board.Model.Defenders)
            {
                frameCount+=DefenderArt.Load(guard.ArtId)["idle"].Frames.Length;
            }
            Assert.That(frameCount,Is.GreaterThan(0));
            string report=$"board_ms={boardTime}\nboard_unity_allocated_bytes={memory}\ninitial_guard_frames={frameCount}\n";
            Directory.CreateDirectory("artifacts");
            File.WriteAllText("artifacts/art-loading-formation.txt",report);
            UnityEngine.Debug.Log("FORMATION_ART_LOADING_BENCHMARK "+report.Replace('\n',' '));
        }
    }
}
