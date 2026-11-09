using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class QuietRouteGuideTests
    {
        ManualBattleController controller;
        float previousScale;

        [SetUp]public void Setup()
        {
            previousScale=Time.timeScale;Time.timeScale=0;CampaignJourneySession.Clear();
        }
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            CampaignJourneySession.Clear();Time.timeScale=previousScale;
        }

        IEnumerator LoadForest()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            do
            {
                controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.BlackForestChallengeActive&&!controller.ArtLoading&&
                    !controller.ShopPanelTransitioning&&controller.Loop!=null&&controller.Loop.CanShop)break;
                yield return null;
            }while(Time.realtimeSinceStartupAsDouble<deadline);
            Assert.That(controller,Is.Not.Null);
            Assert.That(controller.BlackForestChallengeActive&&controller.Loop.CanShop,Is.True);
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            if(controller.CampaignOverlayVisible)controller.CloseCampaignOverlay();
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            deadline=Time.realtimeSinceStartupAsDouble+30;
            while((controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            if(controller.SimulationSpeed!=1)controller.ToggleSpeed();
            ClearHover();
        }

        void ClearHover()=>controller.ProcessShopPointer(EventType.MouseMove,new Vector2(-100,-100));
        void Hover(DeploymentLane lane)
        {
            var point=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(lane.Entry));
            Assert.That(controller.ShopMapPointAvailable(point),Is.True,"The actual red gate must remain reachable through the production pointer path.");
            controller.ProcessShopPointer(EventType.MouseMove,point);
        }
        void Click(string cardId)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(piece=>piece.Id==cardId);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var point=controller.ShopPieceRect(index).center;
            controller.ProcessShopPointer(EventType.MouseDown,point);controller.ProcessShopPointer(EventType.MouseUp,point);
        }
        WaveRouteGuideStroke[] OnlyRoute(string id)
        {
            var strokes=controller.WaveRouteGuideStrokes().ToArray();
            Assert.That(strokes,Is.Not.Empty);
            Assert.That(strokes.All(stroke=>stroke.LaneId==id),Is.True,"Preview and old-wave routes must replace, rather than stack over, the current guide.");
            Assert.That(strokes.All(stroke=>stroke.Pulse),Is.True,"The renderer receives only moving dash spans, without a continuous underlay.");
            return strokes;
        }

        [UnityTest]public IEnumerator FormalJourneyUsesTheSameSingleCurrentWaveDashGuideAndGatePreview()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");
            yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);
            controller.BeginJourney(44);yield return CampaignJourneyEvidenceProbe.Ready(1,c=>controller=c);
            yield return CampaignJourneyEvidenceProbe.FirstCombat(c=>controller=c);
            Assert.That(controller.JourneyActive&&controller.Battle.WavesEnabled,Is.True);
            Assert.That(controller.Battle.CampaignV2Stage,Is.EqualTo(3));
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            yield return CampaignJourneyEvidenceProbe.Ready(3,c=>controller=c);
            ClearHover();
            string current=controller.Battle.CurrentWaveLaneId;
            Assert.That(OnlyRoute(current).All(stroke=>stroke.Open),Is.True);
            var other=controller.Battle.Lanes.First(lane=>lane.Id!=current);
            Hover(other);Assert.That(OnlyRoute(other.Id).Any(stroke=>stroke.Open),Is.False);
            ClearHover();OnlyRoute(current);
        }

        [UnityTest]public IEnumerator ForestOldWaveUnitsDoNotAddAnotherRouteAndHoverAndPauseKeepOneGuide()
        {
            yield return LoadForest();var battle=controller.Battle;
            string previous=battle.CurrentWaveLaneId;
            controller.Tick((float)battle.WaveRemaining-.1f);Click("CARD_M07");
            Assert.That(battle.Units.Any(unit=>unit.Active&&unit.RouteId==previous),Is.True);
            controller.Tick(.15f);ClearHover();
            Assert.That(battle.CurrentWaveIndex,Is.EqualTo(1));
            Assert.That(battle.Units.Any(unit=>unit.Active&&unit.RouteId==previous),Is.True,"A real previous-wave entity must still exist for this overlap regression.");
            string current=battle.CurrentWaveLaneId;OnlyRoute(current);
            Hover(battle.Lanes.Single(lane=>lane.Id==previous));OnlyRoute(previous);
            ClearHover();var before=OnlyRoute(current);
            controller.Tick(.25f);var moving=OnlyRoute(current);
            Assert.That(moving.Select(stroke=>stroke.Projection.To).SequenceEqual(before.Select(stroke=>stroke.Projection.To)),Is.False,
                "Unpaused simulation must visibly move the dash endpoints along the route.");
            battle.SetPaused(true);double clock=battle.Clock;var paused=OnlyRoute(current);
            controller.Tick(5);var after=OnlyRoute(current);
            Assert.That(battle.Clock,Is.EqualTo(clock));
            CollectionAssert.AreEqual(paused.Select(stroke=>stroke.Projection.From),after.Select(stroke=>stroke.Projection.From));
            CollectionAssert.AreEqual(paused.Select(stroke=>stroke.Projection.To),after.Select(stroke=>stroke.Projection.To));
        }

        [UnityTest]public IEnumerator FinalAdvanceHidesTheDefaultGuideEvenWhileTheLastWaveStillHasAUnit()
        {
            yield return LoadForest();var battle=controller.Battle;
            controller.Tick(battle.WavePlan.Waves.Sum(wave=>wave.DeploymentSeconds)-.1f);
            Click("CARD_M07");Assert.That(battle.Units.Any(unit=>unit.Active),Is.True);
            controller.Tick(.15f);ClearHover();
            Assert.That(battle.FinalAdvanceActive,Is.True);Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(battle.Units.Any(unit=>unit.Active),Is.True);
            Assert.That(controller.WaveRouteGuideStrokes(),Is.Empty);
        }

        [UnityTest]public IEnumerator UndergroundPreviewOmitsThePortalConnectorAndReturnsToAnEmptyDefault()
        {
            var map=ApprovedMapCatalog.Find("approved-map-04");
            yield return SceneManager.LoadSceneAsync(map.sceneName);
            yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            Assert.That(controller.Battle.WavesEnabled,Is.False);
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            ClearHover();Assert.That(controller.WaveRouteGuideStrokes(),Is.Empty);
            var portal=map.mechanics.portals.Single();
            var lane=controller.Battle.Lanes.Single(item=>item.Id==portal.routeId);
            Assert.That(RouteGuideGeometry.Pulses(lane.Points,controller.Battle.Clock).Any(span=>span.EdgeIndex==portal.sourcePointIndex),Is.True,
                "The authored underground connector intersects moving dashes, so the omission check is not vacuous.");
            Hover(lane);var strokes=OnlyRoute(lane.Id);
            Assert.That(strokes.Any(stroke=>controller.Battle.ApprovedPortalSegment(stroke.LaneId,stroke.Projection.Source.EdgeIndex)),Is.False);
            Assert.That(strokes.Any(stroke=>stroke.Projection.Source.EdgeIndex<portal.sourcePointIndex),Is.True);
            Assert.That(strokes.Any(stroke=>stroke.Projection.Source.EdgeIndex>=portal.destinationPointIndex),Is.True);
            ClearHover();Assert.That(controller.WaveRouteGuideStrokes(),Is.Empty);
        }
    }
}
