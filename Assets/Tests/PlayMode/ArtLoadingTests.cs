using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ArtLoadingTests
    {
        long budget;float timescale;
        [SetUp]public void Setup(){budget=ArtMemoryCache.BudgetBytes;timescale=Time.timeScale;ArtMemoryCache.Clear();}
        [TearDown]public void Cleanup(){ArtMemoryCache.BudgetBytes=budget;Time.timeScale=timescale;}
        [UnityTest]public IEnumerator MenuMetadataAndPortraitDoNotDecodeBattleActions()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            int before=ArtMemoryCache.TextureLoads;var c=Object.FindAnyObjectByType<ManualBattleController>();
            foreach(var option in c.MonsterTests.Where(o=>o.Playable))Assert.That(option.DefinitionForArtTest(),Is.True);
            // Every role was validated; none of their full action sets were requested.
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
            var art=DefenderArt.Load("D22");Assert.That(art.ActionDuration,Is.GreaterThan(0));Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
            Assert.That(art.Portrait,Is.Not.Null);Assert.That(ArtMemoryCache.TextureLoads-before,Is.LessThanOrEqualTo(1));
            Assert.That(ArtMemoryCache.ResidentBytes,Is.LessThan(96L*1024*1024));
        }
        [UnityTest]public IEnumerator PreparedPixelsKeepFramesAndSafeEvictionReloadsOnlyUnusedClips()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var art=DefenderArt.Load("D22");int png=ArtMemoryCache.RuntimePngLoads,prepared=ArtMemoryCache.PreparedLoads;
            var frames=art["idle"].Frames;var first=frames[0];Assert.That(frames.Length,Is.GreaterThan(100));
            Assert.That(ArtMemoryCache.PreparedLoads,Is.GreaterThan(prepared));Assert.That(ArtMemoryCache.RuntimePngLoads,Is.EqualTo(png));
            Assert.That(first.texture.isReadable,Is.False);Assert.That(first.rect.height,Is.GreaterThan(32));
            var inactive=DefenderArt.Load("D01")["idle"].Frames[0];
            ArtMemoryCache.Pin(new[]{"DefenderArt/D22/idle"});ArtMemoryCache.BudgetBytes=1;ArtMemoryCache.Trim();yield return null;
            Assert.That(first!=null,Is.True,"Active clip must remain even if it exceeds the cache budget.");Assert.That(inactive==null,Is.True);
            Assert.That(art["idle"].Frames[0],Is.SameAs(first));Assert.That(DefenderArt.Load("D01")["idle"].Frames[0]!=null,Is.True);
        }
        [UnityTest]public IEnumerator RealEntryPreparesAcrossFramesFreezesSimulationAndWarmsFirstAttack()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(true);c.SelectCard(true);c.TryDeployAt(c.Battle.Entry);c.Tick(.2f);
            var previous=c.Battle;double clock=previous.Clock;int frames=0;
            var prepare=c.PrepareBattleShop();try
            {
                while(prepare.MoveNext())
                {
                    if(c.Battle==previous){c.Tick(1);Assert.That(previous.Clock,Is.EqualTo(clock));}
                    Assert.That(c.ArtLoading,Is.True);frames++;yield return prepare.Current;
                }
            }
            finally{(prepare as IDisposable)?.Dispose();}
            Assert.That(frames,Is.GreaterThan(2));Assert.That(c.ArtLoading,Is.False);Assert.That(c.BattleShopLoopActive,Is.True);
            int textures=ArtMemoryCache.TextureLoads;
            foreach(string id in new[]{"D21","D22","D27","D28","D29","D30"})
            {
                var art=DefenderArt.Load(id);_ = art["idle"].Frames;
                // Full original actions were included before the entry completed.
                art.AdvancedPose(2,10,0,true,.2,1);art.ActionAt(.2);
            }
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(textures),"First action must reuse prepared frames.");
        }
        [UnityTest]public IEnumerator ExplicitNewRunCancelsPendingBootstrapInsteadOfBeingOverwrittenLater()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(56);
            for(int i=0;i<8;i++)yield return null;
            Assert.That(c.Loop.Seed,Is.EqualTo(56));Assert.That(c.ArtLoading,Is.False);
            Assert.That(c.Battle.Units,Is.Empty);c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator VisibleShopOffersStayPinnedWithoutRepeatedLoadingPages()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(56);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(ManualBattleController).GetField("loopTestSettlementOpen",flags).SetValue(c,true);
            Assert.That((bool)typeof(ManualBattleController).GetMethod("TryTestSettlement",flags).Invoke(c,new object[]{true}),Is.True);
            ArtMemoryCache.BudgetBytes=1;Assert.That(c.OpenLoopShop(),Is.True);yield return null;yield return null;
            while(c.ArtLoading)yield return null;
            int loaded=ArtMemoryCache.TextureLoads;
            for(int i=0;i<10;i++){yield return null;Assert.That(c.ArtLoading,Is.False);Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loaded));}
            c.LeaveShopTrial();
        }
    }
    static class ArtTestOptionExtensions
    {
        public static bool DefinitionForArtTest(this MonsterTestOption option)
        {
            var field=typeof(MonsterTestOption).GetProperty("Definition",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var data=(ManualBattleDefinition)field.GetValue(option);data.Snapshot();return data.Card.HasFrameAnimation;
        }
    }
}
