using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class AdvancedSynergySceneTests
    {
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<900;i++){var c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading)yield break;yield return null;}
            Assert.Fail("Sandbox timeout");
        }
        [UnityTest]public IEnumerator SupplyAndPreparationNeverSpendDeploymentResources()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));int dp=c.Battle.DP;
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Count,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(dp));
            c.StartCampaign(false,44);Assert.That(c.Loop.RewardStage);dp=c.Battle.DP;
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Count,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(dp));c.ClaimCampaignSupply();Assert.That(c.Loop.Reward,Is.Not.Null);c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator NormalRosterDragDeploysAndPauseKeepsItsClock()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(3));Assert.That(c.StartLoopNextBattle());yield return new WaitForSecondsRealtime(.35f);
            int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M26");
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Any(u=>u.CardId=="CARD_M26"),Is.True,"Normal card drag must deploy the visible roster card");
            Assert.That(c.Battle.DP,Is.EqualTo(97));c.Battle.SetPaused(true);
            bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M27");
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Any(u=>u.CardId=="CARD_M27"),Is.True);Assert.That(c.Battle.DP,Is.EqualTo(93));
            double clock=c.Battle.Clock;c.Tick(1);Assert.That(c.Battle.Clock,Is.EqualTo(clock));c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator ThirtySixCardPoolAndFourConstructionPresetsAreReachable()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.Shop.Roles.Count,Is.EqualTo(42));Assert.That(c.Shop.Roles.Count(r=>r.Cost==5),Is.EqualTo(7));
            Assert.That(c.SetSandboxRoster(1));Assert.That(c.Battle.InfectionTier,Is.EqualTo(6));Assert.That(c.Shop.Bench.Any(p=>p.Id=="CARD_M04"));
            Assert.That(c.SetSandboxRoster(2));Assert.That(c.Battle.GuerrillaTier,Is.EqualTo(6));
            Assert.That(c.SetSandboxRoster(3));Assert.That(c.Battle.MudrockTier,Is.EqualTo(2));Assert.That(c.Shop.Bench.Any(p=>p.Id=="CARD_M26"));
            Assert.That(c.SetSandboxRoster(0));Assert.That(c.Battle.SnowTier,Is.EqualTo(5));yield return null;c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator InfectionFormsLockAtStartAndSplitBodiesHaveSeparateVisualPositions()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(1));Assert.That(c.SetCampaignForms(InfectionForm.Swarm,InfectionForm.Shell));
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();var r=c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_ORIGINIUM_SLUG");Assert.That(r.Success);c.Tick(.01f);yield return null;
            Assert.That(c.Battle.Units.Count,Is.EqualTo(3));Assert.That(c.Battle.DP,Is.EqualTo(98));Assert.That(c.Battle.Units.Select(u=>u.VisualOffset.x).Distinct().Count(),Is.EqualTo(3));
            Assert.That(c.SetCampaignForms(InfectionForm.Burst,InfectionForm.Shell),Is.False);Assert.That(c.Battle.InfectionShape,Is.EqualTo(InfectionForm.Swarm));c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator RealGuerrillaUnitsEarnAndSpendOrdersWithoutChangingRosterTiers()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(2));Assert.That(c.StartLoopNextBattle());c.Battle.Begin();
            long request=100;foreach(var p in c.Shop.Bench)c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,p.Id);
            bool used=false;for(int i=0;i<400&&c.Loop.CanDeploy;i++){c.Tick(.1f);if(c.Battle.Orders>=6&&c.UseCampaignOrder(false)){used=true;break;}if(i%10==0)yield return null;}
            Assert.That(used);Assert.That(c.Battle.OrderStrikes,Is.GreaterThan(0));Assert.That(c.Battle.GuerrillaTier,Is.EqualTo(6));Assert.That(c.Battle.OrdersUsed,Is.EqualTo(1));
            c.Battle.SetPaused(true);int points=c.Battle.Orders;Assert.That(c.UseCampaignOrder(true),Is.False);Assert.That(c.Battle.Orders,Is.EqualTo(points));c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator GolemDragsDuringPauseAndPlaysOriginalFullAttack()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(3));Assert.That(c.StartLoopNextBattle());c.Battle.Begin();c.Battle.SetPaused(true);yield return new WaitForSecondsRealtime(.3f);
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.GolemCardRect.center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            var golem=c.Battle.Units.Single(u=>u.Golem);Assert.That(c.Battle.GolemDeployed);Assert.That(c.Battle.DP,Is.EqualTo(96));c.Tick(1);Assert.That(c.Battle.Clock,Is.Zero);yield return null;
            var visual=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.GetComponentsInParent<Transform>().Any(t=>t.name==golem.EntityId));
            Assert.That(visual.Renderer.sprite.name,Does.StartWith("GOLEM/"));Assert.That(visual.FrameIndex,Is.Zero);
            c.Battle.SetPaused(false);for(int i=0;i<100&&double.IsNegativeInfinity(golem.LastAttackTime);i++){c.Tick(.1f);if(i%10==0)yield return null;}
            Assert.That(golem.LastAttackTime,Is.GreaterThanOrEqualTo(0));var position=golem.Motion.Position;c.Tick(1);Assert.That(golem.Motion.Position,Is.EqualTo(position));
            yield return null;Assert.That(visual.FrameState,Is.EqualTo("Attack"));Assert.That(visual.FrameIndex,Is.GreaterThan(0));
            Assert.That(c.Battle.UnitCombat(golem).AttackDuration,Is.EqualTo(5.166667f));c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator NaturalMudrockBattleAwardsAutomaticGrowthAndPreservesItForTheNextRound()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(3));Assert.That(c.StartLoopNextBattle());c.Battle.Begin();
            long request=500;foreach(var p in c.Shop.Bench)c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,p.Id);Assert.That(c.Battle.TryDeployGolem(c.Battle.RunId,++request,c.Battle.Entry).Success);
            for(int i=0;i<2600&&c.Loop.CanDeploy;i++)
            {
                if(i>0&&i%50==0)foreach(var p in c.Shop.Bench)c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,p.Id);
                c.Tick(.1f);if(i%10==0)yield return null;
            }
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(c.Loop.GolemExperience,Is.EqualTo(3));
            typeof(ManualBattleController).GetMethod("ReplaySandbox",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);
            Assert.That(c.Battle.GolemStage,Is.EqualTo(1));Assert.That(c.Battle.GolemDeployed,Is.False);c.LeaveShopTrial();
        }
    }
}
