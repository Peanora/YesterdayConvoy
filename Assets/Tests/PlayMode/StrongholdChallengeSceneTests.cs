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
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
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
        [UnityTest]public IEnumerator SceneGeometrySurvivesRestartWithoutRebuildingOrDiscardingEdits()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            while(c.ArtLoading)yield return null;
            var root=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(root,Is.Not.Null);
            int count=root.GetComponentsInChildren<Renderer>().Length;
            var tile=root.transform.GetChild(1);var changed=tile.localPosition+Vector3.up*.01f;tile.localPosition=changed;
            c.RestartBattle();yield return null;
            Assert.That(Object.FindAnyObjectByType<AuthoredBattlefield>(),Is.SameAs(root));
            Assert.That(root.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(count));Assert.That(tile.localPosition,Is.EqualTo(changed));
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(6));Assert.That(c.Battle.Units,Is.Empty);
            c.LeaveShopTrial();yield return null;Assert.That(root.gameObject.activeSelf,Is.False);
        }
        [UnityTest]public IEnumerator ReturningFromShopMenuLoadsTheAuthoredMonsterScene()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            typeof(ManualBattleController).GetMethod("ReturnToTrialScene",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);
            yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MonsterTrial"));
            Assert.That(GameObject.Find("Industrial Map"),Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<AuthoredBattlefield>(),Is.Null);
            Assert.That(Object.FindAnyObjectByType<ManualBattleController>().TestSelectionOpen,Is.True);
        }
    }
}
