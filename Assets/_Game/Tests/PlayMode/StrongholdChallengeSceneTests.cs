using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class StrongholdChallengeSceneTests
    {
        static Vector2 Gui(Vector3 p){var v=Camera.main.WorldToScreenPoint(p);return TacticalViewport.Current.FromScreen(v);}
        [UnityTest]public IEnumerator BothRedGatesPreviewAndClearTheirEntireRouteBeforeDeployment()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial(0,StrongholdChallengeLayout.Id);yield return null;
            var environment=Object.FindAnyObjectByType<StrongholdMapEnvironment>();Assert.That(c.Battle.Defenders.Count(d=>d.HighGround),Is.EqualTo(3));
            Assert.That(GameObject.Find("Raised defender platform"),Is.Not.Null);
            foreach(var lane in c.Battle.Lanes)
            {
                c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.ShopBenchRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,Gui(lane.Entry));c.Tick(0);
                Assert.That(environment.RouteVisible,Is.True);Assert.That(c.Battle.Units,Is.Empty);
                c.ProcessShopPointer(EventType.MouseDrag,new Vector2(640,400));c.Tick(0);Assert.That(environment.RouteVisible,Is.False);
                c.CancelShopDrag();
            }
            var defender=c.Battle.Defenders[0];c.ProcessShopPointer(EventType.MouseDown,Gui(defender.Position+Camera.main.transform.up*.75f));
            Assert.That(c.DefenderInspectorVisible,Is.True);Assert.That(c.InspectedDefenderId,Is.EqualTo(defender.EntityId));
            Assert.That(c.ShopMapPointAvailable(ManualBattleController.DefenderInspectorRect.center),Is.False);
            c.SetShopVisible(true);Assert.That(c.DefenderInspectorVisible,Is.False);c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator SixStarChallengeCanResetAndSwitchToBothPreviousMaps()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial(0,StrongholdChallengeLayout.Id);yield return null;
            Assert.That(c.Shop.Gold,Is.EqualTo(24));Assert.That(c.Battle.Defenders.All(d=>d.Settings.Skill.Enabled),Is.True);
            Assert.That(c.PurchaseShopCard(3).Success,Is.True);
            c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.ShopBenchRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,Gui(c.Battle.Lanes[0].Entry));c.ProcessShopPointer(EventType.MouseUp,Gui(c.Battle.Lanes[0].Entry));
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Running));c.Tick(2);
            c.RestartBattle();Assert.That(c.Battle.Defenders.Count(d=>d.HighGround),Is.EqualTo(3));Assert.That(c.Battle.Clock,Is.Zero);
            c.EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;Assert.That(c.Battle.Defenders.Count,Is.EqualTo(8));Assert.That(c.Battle.StrongholdChallenge,Is.False);
            c.EnterShopTrial();yield return null;yield return null;Assert.That(c.Battle.StrongholdMapEnabled,Is.False);Assert.That(Object.FindAnyObjectByType<StrongholdMapEnvironment>(),Is.Null);
            c.LeaveShopTrial();yield return null;
        }
    }
}
