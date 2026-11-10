using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class BlackForestSceneTests
    {
        ManualBattleController controller;
        float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        IEnumerator Ready()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            while((controller==null||!controller.BlackForestChallengeActive||controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)
            {controller=Object.FindAnyObjectByType<ManualBattleController>();yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.BlackForestChallengeActive,Is.True);
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False,"Forest preparation did not finish.");
            // This fixture accepts the configured default form; normal play still shows the existing prompt.
            typeof(ManualBattleController).GetField("inspectedSynergy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,null);
            yield return null;
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);controller=Object.FindAnyObjectByType<ManualBattleController>();
            yield return Ready();Assert.That(controller.Loop.CanShop,Is.True);Time.timeScale=0;
        }
        void AssertEncounter()
        {
            var data=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            Assert.That(controller.TacticalMapId,Is.EqualTo(BlackForestLayout.Id));
            Assert.That(controller.Battle.DP,Is.EqualTo(BlackForestChallengePlan.InitialDP));
            Assert.That(controller.Battle.Defense,Is.EqualTo(BlackForestChallengePlan.Defense));
            Assert.That(controller.Battle.Defenders.Count,Is.EqualTo(9));
            foreach(var guard in data.defenders)
            {
                var live=controller.Battle.Defenders.Single(d=>d.Settings.Id==guard.id);
                Assert.That(live.MaxHealth,Is.EqualTo(guard.health),guard.id+" must not inherit the twelve-fold sandbox HP.");
                Assert.That(live.Settings.Attack,Is.EqualTo(guard.attack),guard.id+" must not inherit the eight-fold sandbox attack.");
                Assert.That(live.HighGround,Is.EqualTo(guard.position.y>.5f),guard.id);
            }
            Assert.That(controller.Battle.Lanes.Count,Is.EqualTo(3));
            Assert.That(controller.Battle.Lanes.Select(l=>l.ExitId).Distinct().Count(),Is.EqualTo(2));
            foreach(var lane in controller.Battle.Lanes)
            {
                var position=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(lane.Entry));
                Assert.That(new Rect(185,60,1095,492).Contains(position),Is.True,"The enlarged map must keep every red gate clear of HUD and cards.");
            }
            Assert.That(Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length,Is.EqualTo(238));
        }
        void AssertStock()
        {
            Assert.That(controller.Shop.Roles.Count,Is.EqualTo(CampaignBalance.Profiles.Count));Assert.That(controller.Shop.UnlimitedGold,Is.True);
            foreach(var role in controller.Shop.Roles)
            {
                int owned=controller.Shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>ShopTrialModel.Copies(p.Star));
                Assert.That(controller.Shop.Available(role.Id)+owned+controller.Shop.Offers.Count(id=>id==role.Id),Is.EqualTo(controller.Shop.Capacity(role.Id)),role.Id);
            }
            controller.Shop.Validate();
        }
        void Click(string cardId)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==cardId);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));
            int before=controller.Battle.Units.Count(u=>!u.SynergySupport);
            var point=controller.ShopPieceRect(index).center;
            controller.ProcessShopPointer(EventType.MouseDown,point);
            controller.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(controller.Battle.Units.Count(u=>!u.SynergySupport),Is.EqualTo(before+1),cardId+" / "+controller.ShopFeedback);
        }
        [UnityTest]public IEnumerator SavedTerrainPersistsAcrossShopTransactionsRestartAndSceneExit()
        {
            yield return Load();var c=controller;AssertEncounter();AssertStock();
            Assert.That(c.Shop.Level,Is.EqualTo(BlackForestChallengePlan.InitialLevel));
            Assert.That(c.Shop.Bench.Select(p=>p.Id),Is.EquivalentTo(BlackForestChallengePlan.StartingIds));
            foreach(var piece in BlackForestChallengePlan.StartingPieces())
            {
                var stats=CampaignBalance.Profiles[piece.Id];var card=c.Battle.FindCard(piece.Id);
                Assert.That(card,Is.Not.Null);
                Assert.That(card.Settings.Health,Is.EqualTo(Mathf.Floor(stats.HP*CampaignBalance.HPScale(stats.Cost,piece.Star)+.5f)),piece.Id+" must use normal Campaign health.");
            }
            var tile=Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).First(t=>t.coordinate==new Vector2Int(8,8));
            Vector3 moved=tile.transform.position+Vector3.up*.1f;tile.transform.position=moved;
            int gold=c.Shop.Gold,offer=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]!=null);
            Assert.That(c.PurchaseShopCard(offer).Success,Is.True);Assert.That(c.Shop.Gold,Is.EqualTo(gold));AssertStock();
            Assert.That(tile.transform.position,Is.EqualTo(moved));
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();Click("CARD_M07");
            Assert.That(c.Battle.Units.Single(u=>!u.SynergySupport).RouteId,Is.EqualTo("R1"));
            c.Battle.SetPaused(true);Vector3 position=c.Battle.Units[0].Motion.Position;c.Tick(5);
            Assert.That(c.Battle.Units[0].Motion.Position,Is.EqualTo(position));
            Assert.That(c.RestartCampaignEncounter(),Is.True);yield return Ready();AssertEncounter();
            Assert.That(c.Battle.Units,Is.Empty);Assert.That(tile.transform.position,Is.EqualTo(moved));
            c.LeaveShopTrial();yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            Assert.That(Object.FindAnyObjectByType<BlackForestTile>(),Is.Null);
        }
        [UnityTest]public IEnumerator PausedActiveGateClicksKeepRealCostsAndTheForestOnReturnToPreparation()
        {
            yield return Load();var c=controller;Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();
            c.Battle.Begin();c.Battle.SetPaused(true);double clock=c.Battle.Clock;
            var names=new[]{"CARD_M07","CARD_M08","CARD_M15"};int expectedDP=BlackForestChallengePlan.InitialDP;
            var health=c.Battle.Defenders.Select(d=>d.Health).ToArray();var sp=c.Battle.Defenders.Select(d=>d.SkillSP).ToArray();
            for(int lane=0;lane<3;lane++)
            {
                var card=c.Battle.FindCard(names[lane]);int uses=card.RemainingUses;expectedDP-=card.Settings.DeploymentCost;
                Click(names[lane]);var unit=c.Battle.Units.Single(u=>!u.SynergySupport&&u.CardId==names[lane]);
                Assert.That(unit.RouteId,Is.EqualTo("R1"));Assert.That(unit.ExitId,Is.EqualTo("B1"));
                Assert.That(card.RemainingUses,Is.EqualTo(uses-1));Assert.That(c.Battle.CardCooldown(names[lane]),Is.GreaterThan(0));
                Assert.That(c.Battle.DP,Is.EqualTo(expectedDP));
            }
            var positions=c.Battle.Units.Select(u=>u.Motion.Position).ToArray();c.Tick(10);
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.Units.Select(u=>u.Motion.Position),Is.EqualTo(positions));
            Assert.That(c.Battle.Defenders.Select(d=>d.Health),Is.EqualTo(health));Assert.That(c.Battle.Defenders.Select(d=>d.SkillSP),Is.EqualTo(sp));
            Assert.That(c.RestartCampaignEncounter(),Is.True);yield return Ready();AssertEncounter();AssertStock();
            Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.ChessPieces,Is.Empty);Assert.That(c.Battle.Clock,Is.Zero);
            typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
            yield return Ready();Assert.That(c.Loop.CanShop,Is.True);AssertEncounter();AssertStock();
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();AssertEncounter();
        }
    }
}
