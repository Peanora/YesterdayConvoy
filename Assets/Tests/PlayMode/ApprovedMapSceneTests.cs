using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ApprovedMapSceneTests
    {
        ManualBattleController controller;
        float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        IEnumerator Load(ApprovedMapDefinition map)
        {
            Time.timeScale=0;yield return SceneManager.LoadSceneAsync(map.sceneName);
            yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
        }
        IEnumerator Ready(ApprovedMapDefinition map)=>ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);

        [UnityTest]public IEnumerator OrdinaryPreparationKeepsItsFormPromptAndPublicCloseAcceptsOnlyTheExistingDefault()
        {
            var map=ApprovedMapCatalog.Find("approved-map-01");Time.timeScale=0;
            yield return SceneManager.LoadSceneAsync(map.sceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+240;
            do
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.ApprovedMapActive&&controller.ApprovedMap==map&&!controller.ArtLoading&&
                    !controller.ShopPanelTransitioning&&controller.Loop!=null&&controller.Loop.CanShop)break;
                yield return null;
            }while(Time.realtimeSinceStartupAsDouble<deadline);
            Assert.That(controller,Is.Not.Null);Assert.That(controller.ApprovedMap,Is.SameAs(map));
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            Assert.That(controller.CampaignOverlayVisible,Is.True,"Ordinary preparation must still offer the infection choice.");
            var battle=controller.Battle;var form=battle.InfectionShape;int dp=battle.DP;
            var bench=controller.Shop.Bench.Select(piece=>piece.Id+"/"+piece.Star).ToArray();
            controller.CloseCampaignOverlay();yield return null;
            Assert.That(controller.CampaignOverlayVisible,Is.False);Assert.That(controller.Battle,Is.SameAs(battle));
            Assert.That(battle.InfectionShape,Is.EqualTo(form));Assert.That(battle.DP,Is.EqualTo(dp));Assert.That(battle.Clock,Is.Zero);
            CollectionAssert.AreEqual(bench,controller.Shop.Bench.Select(piece=>piece.Id+"/"+piece.Star));
            Assert.That(ApprovedMapEvidenceProbe.StockConserved(controller),Is.True);
        }

        [UnityTest]public IEnumerator AllEightScenesKeepAuthoredTerrainNormalValuesPausedDeploymentAndCleanSceneExit()
        {
            var maps=ApprovedMapCatalog.All;Assert.That(maps.Length,Is.EqualTo(8));
            foreach(var map in maps)
            {
                string source=JsonUtility.ToJson(map),encounter=JsonUtility.ToJson(map.encounter);
                yield return Load(map);var c=controller;
                Assert.That(c.ApprovedMap,Is.SameAs(map));Assert.That(c.ApprovedMapActive,Is.True);
                Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(map.sceneName));
                Assert.That(Object.FindAnyObjectByType<ApprovedMapBootstrap>().map,Is.SameAs(map));
                var terrain=Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None);
                Assert.That(terrain.Length,Is.EqualTo(map.width*map.height));var saved=new HashSet<ApprovedMapTile>(terrain);
                var root=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(root,Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(c.Loop.CanShop,Is.True);Assert.That(c.Shop.Level,Is.EqualTo(8));Assert.That(c.Shop.UnlimitedGold,Is.True);
                Assert.That(c.Battle.DP,Is.EqualTo(50));Assert.That(c.Battle.Defense,Is.EqualTo(6));
                Assert.That(c.Battle.WavesEnabled,Is.False);Assert.That(c.Battle.StrongholdMapEnabled,Is.False);
                CollectionAssert.AreEqual(ApprovedMapPlan.StartingIds,c.Shop.Bench.Select(piece=>piece.Id));
                Assert.That(ApprovedMapEvidenceProbe.StockConserved(c),Is.True);Assert.That(ApprovedMapEvidenceProbe.NormalProfiles(c),Is.True);
                foreach(var guard in map.encounter.defenders)
                {
                    var live=c.Battle.FindDefender(guard.id);Assert.That(live,Is.Not.Null);
                    Assert.That(live.MaxHealth,Is.EqualTo(guard.health));Assert.That(live.Settings.Attack,Is.EqualTo(guard.attack));
                    Assert.That(live.Position,Is.EqualTo(guard.position));
                }
                Assert.That(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x,60),Is.EqualTo(0).Within(.1f));
                Assert.That(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y,0),Is.EqualTo(0).Within(.1f));
                foreach(var lane in c.Battle.Lanes)
                {
                    var screen=Camera.main.WorldToScreenPoint(lane.Entry);
                    Assert.That(screen.z,Is.GreaterThan(0));
                    Assert.That(ManualBattleController.ShopMapSafeRect.Contains(TacticalViewport.Current.FromScreen(screen)),Is.True,map.id+" / "+lane.Id);
                }
                // Moving an existing object checks that restart reuses the scene instance;
                // it does not save an edit to the source scene or manufacture terrain.
                var selected=terrain.First();Vector3 original=selected.transform.position;selected.transform.position=original+Vector3.up*.1f;
                Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready(map);
                string first=ApprovedMapPlan.StartingIds[0],second=ApprovedMapPlan.StartingIds[1];
                Assert.That(ApprovedMapEvidenceProbe.Drag(c,first,c.Battle.Lanes[0].Entry),Is.True,c.ShopFeedback);
                c.Battle.SetPaused(true);var card=c.Battle.FindCard(second);int dp=c.Battle.DP,uses=card.RemainingUses;
                Assert.That(ApprovedMapEvidenceProbe.Drag(c,second,c.Battle.Lanes.Last().Entry),Is.True,c.ShopFeedback);
                var unit=c.Battle.Units.Last(item=>item.CardId==second&&!item.SynergySupport);
                Assert.That(unit.RouteId,Is.EqualTo(c.Battle.Lanes.Last().Id));Assert.That(unit.ExitId,Is.EqualTo(c.Battle.Lanes.Last().ExitId));
                Assert.That(c.Battle.DP,Is.EqualTo(dp-card.Settings.DeploymentCost));Assert.That(card.RemainingUses,Is.EqualTo(uses-1));
                Assert.That(c.Battle.CardCooldown(second),Is.GreaterThan(0));
                double clock=c.Battle.Clock;var units=c.Battle.Units.ToArray();var positions=units.Select(item=>item.Motion.Position).ToArray();
                var health=units.Select(item=>item.Health).ToArray();var skill=units.Select(item=>item.SkillSP).ToArray();
                var cooldown=c.Battle.Cards.Select(item=>c.Battle.CardCooldown(item.Settings.CardId)).ToArray();
                c.Tick(10);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                CollectionAssert.AreEqual(positions,units.Select(item=>item.Motion.Position));CollectionAssert.AreEqual(health,units.Select(item=>item.Health));
                CollectionAssert.AreEqual(skill,units.Select(item=>item.SkillSP));
                CollectionAssert.AreEqual(cooldown,c.Battle.Cards.Select(item=>c.Battle.CardCooldown(item.Settings.CardId)));
                dp=c.Battle.DP;int count=c.Battle.Units.Count;
                Assert.That(ApprovedMapEvidenceProbe.Drag(c,second,c.Battle.Lanes[0].Entry),Is.False);
                Assert.That(c.Battle.DP,Is.EqualTo(dp));Assert.That(c.Battle.Units.Count,Is.EqualTo(count));
                Assert.That(c.RestartCampaignEncounter(),Is.True);yield return Ready(map);
                Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(50));
                Assert.That(saved.SetEquals(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None)),Is.True);
                Assert.That(selected.transform.position,Is.EqualTo(original+Vector3.up*.1f));selected.transform.position=original;
                Assert.That(JsonUtility.ToJson(map),Is.EqualTo(source));Assert.That(JsonUtility.ToJson(map.encounter),Is.EqualTo(encounter));
                c.RestartBattle();yield return Ready(map);Assert.That(c.Loop.CanShop,Is.True);
                Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.ApprovedPortalTrips,Is.Zero);
                Assert.That(c.Battle.DP,Is.EqualTo(50));Assert.That(c.Battle.Defense,Is.EqualTo(6));
                c.LeaveShopTrial();yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
                Assert.That(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None),Is.Empty);
            }
        }

        [UnityTest]public IEnumerator MailConnectorIsConfiguredOnlyUndergroundAndSurvivesRestart()
        {
            var map=ApprovedMapCatalog.Find("approved-map-04");yield return Load(map);
            Assert.That(map.mechanics.portals.Length,Is.EqualTo(1));var portal=map.mechanics.portals[0];
            Assert.That(controller.Battle.ApprovedPortalSegment(portal.routeId,portal.sourcePointIndex),Is.True);
            Assert.That(controller.Battle.ApprovedPortalSegment(portal.routeId,portal.destinationPointIndex),Is.False);
            Assert.That(controller.Battle.ApprovedPortalTrips,Is.Zero);
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(map);
            Assert.That(ApprovedMapEvidenceProbe.Drag(controller,ApprovedMapPlan.StartingIds[0],controller.Battle.Lanes[0].Entry),Is.True);
            Assert.That(controller.RestartCampaignEncounter(),Is.True);yield return Ready(map);
            Assert.That(controller.Battle.ApprovedPortalTrips,Is.Zero);Assert.That(controller.Battle.Units,Is.Empty);
            Assert.That(controller.Battle.ApprovedPortalSegment(portal.routeId,portal.sourcePointIndex),Is.True);
            Assert.That(controller.Battle.ApprovedHeatPumpCount,Is.Zero);
        }

        [UnityTest]public IEnumerator ThreeHeatPumpsUseTheCombatClockAndPauseAndRestartTheirPhase()
        {
            var map=ApprovedMapCatalog.Find("approved-map-05");yield return Load(map);var c=controller;
            Assert.That(c.Battle.ApprovedHeatPumpCount,Is.EqualTo(3));
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready(map);
            Assert.That(ApprovedMapEvidenceProbe.Drag(c,ApprovedMapPlan.StartingIds[0],c.Battle.Lanes[0].Entry),Is.True);
            c.Battle.SetPaused(false);if(c.SimulationSpeed!=1)c.ToggleSpeed();
            for(int i=0;i<32;i++)c.Tick(.05f);
            Assert.That(c.Battle.Clock,Is.EqualTo(1.6).Within(.001));
            Assert.That(c.Battle.ApprovedHeatState(0),Is.EqualTo(2));
            Assert.That(c.Battle.ApprovedHeatState(1),Is.Zero);Assert.That(c.Battle.ApprovedHeatState(2),Is.Zero);
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;
            var states=Enumerable.Range(0,3).Select(c.Battle.ApprovedHeatState).ToArray();c.Tick(30);
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));CollectionAssert.AreEqual(states,Enumerable.Range(0,3).Select(c.Battle.ApprovedHeatState));
            Assert.That(c.RestartCampaignEncounter(),Is.True);yield return Ready(map);
            Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.ApprovedHeatState(0),Is.EqualTo(1));
            Assert.That(c.Battle.ApprovedHeatDamageToUnits,Is.Zero);Assert.That(c.Battle.ApprovedHeatDamageToDefenders,Is.Zero);
        }
    }
}
