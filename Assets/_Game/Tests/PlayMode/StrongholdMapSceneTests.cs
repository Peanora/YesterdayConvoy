using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class StrongholdMapSceneTests
    {
        [UnityTest]public IEnumerator TexturedMapAndBatchedEffectsFreezeWithCombatAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;yield return null;
            var art=Resources.Load<StrongholdMapArt>("StrongholdMapArt");Assert.That(art.metal01.width,Is.EqualTo(2048));Assert.That(art.normal02.width,Is.EqualTo(2048));
            Assert.That(art.surfaceShader.isSupported&&art.energyShader.isSupported,Is.True);
            Assert.That(Object.FindAnyObjectByType<StrongholdMapEnvironment>().NormalMappedSurfaceCount,Is.GreaterThanOrEqualTo(3));
            var fx=Object.FindAnyObjectByType<StrongholdMapVfx>();Assert.That(fx.VertexCount,Is.GreaterThan(0));Assert.That(fx.VertexCount,Is.LessThan(2048));
            Assert.That(c.Battle.Units,Is.Empty,"Visual effects must not create combat entities.");
            c.StartBattle();c.Tick(.2f);fx.Present();double clock=fx.VisualClock;c.Battle.SetPaused(true);
            yield return new WaitForSecondsRealtime(.15f);Assert.That(fx.VisualClock,Is.EqualTo(clock));
            c.Battle.SetPaused(false);c.Tick(.2f);fx.Present();Assert.That(fx.VisualClock,Is.GreaterThan(clock));
            c.LeaveShopTrial();yield return null;yield return null;Assert.That(fx==null,Is.True);
        }
        [UnityTest]public IEnumerator PortalHidesTheExistingModelAndRestoresItAfterTheWait()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;
            c.StartBattle();var lane=c.Battle.Lanes[0];var receipt=c.Battle.TryDeploy(c.Battle.RunId,1,lane.Entry);Assert.That(receipt.Success,Is.True);
            var u=c.Battle.FindUnit(receipt.EntityId);float distance=0;for(int i=1;i<=6;i++)distance+=Vector3.Distance(lane.Points[i-1],lane.Points[i]);
            u.Motion.ApproachDistance(distance,distance/u.Motion.Speed+.1f);c.Tick(.05f);yield return null;
            var view=Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.name==u.EntityId&&t.gameObject.scene.IsValid());
            Assert.That(view.gameObject.activeSelf,Is.False);Assert.That(c.Battle.StrongholdInTransit(u.EntityId),Is.True);
            c.Battle.SetPaused(true);c.Tick(100);Assert.That(view.gameObject.activeSelf,Is.False);c.Battle.SetPaused(false);
            c.Tick(3.1f);Assert.That(view.gameObject.activeSelf,Is.True);Assert.That(c.Battle.Units.Count,Is.EqualTo(1));Assert.That(c.Battle.StrongholdPortalTrips,Is.EqualTo(1));
            c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator RealMapKeepsShopAndOriginalAssetAndResetsWithoutLeaking()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var original=Resources.Load<DefenderTrialDefinition>("MapTrials/city");string before=JsonUtility.ToJson(original);
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;yield return null;
            Assert.That(c.TacticalMapId,Is.EqualTo(StrongholdMapLayout.Id));Assert.That(c.Battle.StrongholdMapEnabled,Is.True);
            Assert.That(c.Battle.Lanes.Count,Is.EqualTo(2));Assert.That(c.Battle.Defenders.Count,Is.EqualTo(8));
            Assert.That(Object.FindAnyObjectByType<StrongholdMapEnvironment>(),Is.Not.Null);
            Assert.That(c.Battle.Settings.Health,Is.EqualTo(c.RosterDefinition(0).Snapshot().Health*4));
            Assert.That(c.PurchaseShopCard(3).Success,Is.True);Assert.That(c.Shop.Bench.Any(p=>p.Id=="CARD_M18"),Is.True);
            c.SetShopVisible(false);yield return new WaitForSecondsRealtime(.3f);
            var gate=c.Battle.Lanes[1].Entry;var gui=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(gate));
            var start=ManualBattleController.ShopBenchRect(0).center;
            c.ProcessShopPointer(EventType.MouseDown,start);c.ProcessShopPointer(EventType.MouseDrag,gui);c.ProcessShopPointer(EventType.MouseUp,gui);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1));Assert.That(c.Battle.Units[0].RouteId,Is.EqualTo(c.Battle.Lanes[1].Id));
            Assert.That(c.PurchaseShopCard(1).Success,Is.False);c.Tick(1);Assert.That(c.Battle.Clock,Is.GreaterThan(0));
            c.EnterShopTrial(2,StrongholdMapLayout.Id);yield return null;var merged=c.PurchaseShopCard(0);Assert.That(merged.FinalStar,Is.EqualTo(3));
            c.EnterShopTrial(0,StrongholdMapLayout.Id);Assert.That(c.Battle.StrongholdInfectionTouches,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);
            c.EnterShopTrial();yield return null;yield return null;Assert.That(c.Battle.StrongholdMapEnabled,Is.False);
            Assert.That(Object.FindAnyObjectByType<StrongholdMapEnvironment>(),Is.Null);Assert.That(JsonUtility.ToJson(original),Is.EqualTo(before));
            c.LeaveShopTrial();yield return null;Assert.That(c.TestSelectionOpen,Is.True);
        }
    }
}
