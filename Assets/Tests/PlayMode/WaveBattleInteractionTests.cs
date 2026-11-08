using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class WaveBattleInteractionTests
    {
        ManualBattleController controller;
        float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        IEnumerator Load(bool waves=true,int preset=-1)
        {
            yield return SceneManager.LoadSceneAsync(waves?BlackForestLayout.SceneName:"SynergyTrial");
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            do
            {
                controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.CampaignActive&&!controller.ArtLoading&&!controller.ShopPanelTransitioning&&
                    controller.Loop.CanShop&&(!waves||controller.BlackForestChallengeActive))break;
                yield return null;
            }while(Time.realtimeSinceStartupAsDouble<deadline);
            Assert.That(controller,Is.Not.Null);
            Assert.That(controller.CampaignActive&&controller.Loop.CanShop,Is.True);
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            typeof(ManualBattleController).GetField("inspectedSynergy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,null);
            if(preset>=0)
            {
                typeof(ManualBattleController).GetMethod("PrepareSandboxRoster",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(controller,new object[]{preset});
                yield return null;
                deadline=Time.realtimeSinceStartupAsDouble+180;
                while((controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
                Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False,"The production roster preparation must finish before combat.");
                Assert.That(controller.Loop.CanShop,Is.True);
                typeof(ManualBattleController).GetField("inspectedSynergy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,null);
            }
            Time.timeScale=0;
            Assert.That(controller.Battle.WavesEnabled,Is.EqualTo(waves));
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            deadline=Time.realtimeSinceStartupAsDouble+30;
            while((controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            controller.Battle.Begin();
        }
        Vector2 Card(string id)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);
            Assert.That(index,Is.GreaterThanOrEqualTo(0),id);
            return controller.ShopPieceRect(index).center;
        }
        int NormalUnits=>controller.Battle.Units.Count(u=>!u.SynergySupport);
        void Click(string id)
        {
            var point=Card(id);controller.ProcessShopPointer(EventType.MouseDown,point);
            controller.ProcessShopPointer(EventType.MouseUp,point);
        }

        [UnityTest]public IEnumerator PausedCardClickCommitsOnceAtTheOpenGateAndCooldownRejectsRepeat()
        {
            yield return Load();var b=controller.Battle;b.SetPaused(true);
            string id="CARD_M07";var state=b.FindCard(id);int dp=b.DP,uses=state.RemainingUses;
            var point=Card(id);controller.ProcessShopPointer(EventType.MouseDown,point);
            Assert.That(NormalUnits,Is.Zero,"Press alone cannot spend resources.");
            controller.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(NormalUnits,Is.EqualTo(1));
            Assert.That(b.Units.Single(u=>!u.SynergySupport).RouteId,Is.EqualTo(b.CurrentWaveLaneId));
            Assert.That(b.DP,Is.EqualTo(dp-state.Settings.DeploymentCost));
            Assert.That(state.RemainingUses,Is.EqualTo(uses-1));
            double cd=b.CardCooldown(id),remaining=b.WaveRemaining;
            Assert.That(cd,Is.GreaterThan(0));
            controller.ProcessShopPointer(EventType.MouseUp,point);Click(id);controller.Tick(10);
            Assert.That(NormalUnits,Is.EqualTo(1));Assert.That(b.Clock,Is.Zero);
            Assert.That(b.WaveRemaining,Is.EqualTo(remaining));Assert.That(b.CardCooldown(id),Is.EqualTo(cd));
            Assert.That(b.DP,Is.EqualTo(dp-state.Settings.DeploymentCost));
            Assert.That(controller.Dragging||controller.Selected,Is.False);
        }

        [UnityTest]public IEnumerator DraggingAwayCancelFocusLossAndRestartNeverCommitAnOldClick()
        {
            yield return Load();var b=controller.Battle;b.SetPaused(true);int dp=b.DP;
            var card=Card("CARD_M07");
            controller.ProcessShopPointer(EventType.MouseDown,card);
            controller.ProcessShopPointer(EventType.MouseDrag,card+Vector2.up*20);
            controller.ProcessShopPointer(EventType.MouseUp,card);
            Assert.That(NormalUnits,Is.Zero);
            controller.ProcessShopPointer(EventType.MouseDown,card);
            typeof(ManualBattleController).GetMethod("ShopPointerEvent",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(controller,new object[]{EventType.MouseDown,card,1});
            controller.ProcessShopPointer(EventType.MouseUp,card);Assert.That(NormalUnits,Is.Zero);
            controller.ProcessShopPointer(EventType.MouseDown,card);controller.CancelShopDrag();
            controller.ProcessShopPointer(EventType.MouseUp,card);Assert.That(NormalUnits,Is.Zero);
            controller.ProcessShopPointer(EventType.MouseDown,card);
            var diagnostic=typeof(ManualBattleController).GetField("diagnostic",BindingFlags.Instance|BindingFlags.NonPublic);
            bool wasDiagnostic=(bool)diagnostic.GetValue(controller);diagnostic.SetValue(controller,false);
            typeof(ManualBattleController).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{false});
            diagnostic.SetValue(controller,wasDiagnostic);
            controller.ProcessShopPointer(EventType.MouseUp,card);
            Assert.That(NormalUnits,Is.Zero);Assert.That(b.IsPaused,Is.True);Assert.That(b.DP,Is.EqualTo(dp));
            controller.ProcessShopPointer(EventType.MouseDown,card);b.Restart();b.Begin();
            controller.ProcessShopPointer(EventType.MouseUp,card);
            Assert.That(NormalUnits,Is.Zero);Assert.That(b.DP,Is.EqualTo(dp));
        }

        [UnityTest]public IEnumerator AClickHeldAcrossTheWaveBoundaryCancelsAndTheNextClickUsesTheNewGate()
        {
            yield return Load();var b=controller.Battle;var card=Card("CARD_M07");
            controller.ProcessShopPointer(EventType.MouseDown,card);
            controller.Tick((float)b.WaveRemaining+.05f);
            Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));
            controller.ProcessShopPointer(EventType.MouseUp,card);
            Assert.That(NormalUnits,Is.Zero);Assert.That(b.DP,Is.EqualTo(b.CurrentWaveBudget));
            b.SetPaused(true);Click("CARD_M07");
            Assert.That(NormalUnits,Is.EqualTo(1));
            Assert.That(b.Units.Single(u=>!u.SynergySupport).RouteId,Is.EqualTo(b.WavePlan.Waves[1].LaneId));
        }

        [UnityTest]public IEnumerator FinalAdvanceClosesCardDeploymentWhileTheBattleAndPauseStillWork()
        {
            yield return Load();var b=controller.Battle;
            // Enter the tail with a real, newly deployed unit; an empty army may legitimately lose early.
            controller.Tick(b.WavePlan.Waves.Sum(w=>w.DeploymentSeconds)-.1f);
            Click("CARD_M07");Assert.That(NormalUnits,Is.EqualTo(1));controller.Tick(.15f);
            Assert.That(b.FinalAdvanceActive,Is.True);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            int dp=b.DP,uses=b.FindCard("CARD_M07").RemainingUses;double clock=b.Clock,remaining=b.BattleTimeRemaining;
            b.SetPaused(true);Click("CARD_M07");controller.Tick(5);
            Assert.That(NormalUnits,Is.EqualTo(1));Assert.That(b.DP,Is.EqualTo(dp));
            Assert.That(b.FindCard("CARD_M07").RemainingUses,Is.EqualTo(uses));
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.BattleTimeRemaining,Is.EqualTo(remaining));
            Assert.That(controller.Dragging||controller.Selected,Is.False);
        }

        [UnityTest]public IEnumerator ARegularSynergyTrialRetainsPausedDragDeployment()
        {
            yield return Load(false);var b=controller.Battle;b.SetPaused(true);int dp=b.DP;
            var card=Card("CARD_M07");Click("CARD_M07");Assert.That(NormalUnits,Is.Zero);
            Vector2 gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(b.Entry));
            controller.ProcessShopPointer(EventType.MouseDown,card);controller.ProcessShopPointer(EventType.MouseDrag,gate);
            controller.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(NormalUnits,Is.EqualTo(1));Assert.That(b.DP,Is.EqualTo(dp-b.FindCard("CARD_M07").Settings.DeploymentCost));
            Assert.That(b.Clock,Is.Zero);
        }

        [UnityTest]public IEnumerator RankingClicksNeverDeployTheWaveGolemAndItsOwnCardCommitsOnlyOnce()
        {
            yield return Load(preset:8);var b=controller.Battle;b.SetPaused(true);
            Assert.That(b.MudrockTier,Is.GreaterThanOrEqualTo(2));
            var ranking=ManualBattleController.DamageRankingPanelRect;
            var golem=ManualBattleController.GolemCardRect;
            Assert.That(golem.Overlaps(ranking),Is.False,"The giant's clickable card must be entirely outside the damage ranking.");
            int dp=b.DP,cost=b.GolemCost;
            Assert.That(cost,Is.LessThanOrEqualTo(dp),"This is a real affordable deployment, not a false pass caused by insufficient DP.");
            var points=new[]{ranking.center,ranking.position+new Vector2(12,12),
                new Vector2(ranking.xMax-12,ranking.yMax-12),new Vector2(1200,470)};
            foreach(var point in points)
            {
                Assert.That(ranking.Contains(point),Is.True);
                controller.ProcessShopPointer(EventType.MouseDown,point);
                controller.ProcessShopPointer(EventType.MouseUp,point);
                Assert.That(b.GolemDeployed,Is.False);Assert.That(b.Units,Is.Empty);
                Assert.That(b.DP,Is.EqualTo(dp),"Ranking clicks cannot spend deployment resources.");
            }
            controller.ProcessShopPointer(EventType.MouseDown,golem.center);
            Assert.That(b.GolemDeployed,Is.False);Assert.That(b.DP,Is.EqualTo(dp));
            controller.ProcessShopPointer(EventType.MouseUp,golem.center);
            Assert.That(b.GolemDeployed,Is.True);Assert.That(b.DP,Is.EqualTo(dp-cost));
            Assert.That(b.Units.Single(u=>u.CardId==CampaignBalance.GolemCardId).RouteId,Is.EqualTo(b.CurrentWaveLaneId));
            controller.ProcessShopPointer(EventType.MouseUp,golem.center);
            controller.ProcessShopPointer(EventType.MouseDown,golem.center);
            controller.ProcessShopPointer(EventType.MouseUp,golem.center);controller.Tick(5);
            Assert.That(b.Units.Count(u=>u.CardId==CampaignBalance.GolemCardId),Is.EqualTo(1));
            Assert.That(b.DP,Is.EqualTo(dp-cost));Assert.That(b.Clock,Is.Zero);
        }
    }
}
