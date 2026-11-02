using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class BattleShopLoopSceneTests
    {
        static Vector2 Gui(Vector3 world){var p=Camera.main.WorldToScreenPoint(world);return new Vector2(p.x*1280/Screen.width,(Screen.height-p.y)*720/Screen.height);}
        [UnityTest]public IEnumerator TwoRealBattlesPreservePurchasedCardsAndResetCombat()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartBattleShopLoop(73);yield return null;
            Assert.That(Object.FindObjectsByType<ActorGrounding>(FindObjectsSortMode.None).Length,Is.EqualTo(2));
            var economy=c.Shop;Assert.That(economy.Level,Is.EqualTo(1));Assert.That(economy.Bench.Count,Is.EqualTo(1));
            for(int stage=1;stage<=2;stage++)
            {
                if(stage==2){Assert.That(c.OpenLoopShop(),Is.True);yield return null;int slot=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]!=null&&c.Shop.Offers[i]!=c.Shop.Bench[0].Id);Assert.That(c.PurchaseShopCard(slot).Success,Is.True);Assert.That(c.StartLoopNextBattle(),Is.True);yield return new WaitForSecondsRealtime(.25f);}
                Assert.That(c.Shop,Is.SameAs(economy));Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.DP,Is.EqualTo(12));
                int sent=0;
                for(int tick=0;tick<5000&&c.Loop.CanDeploy;tick++)
                {
                    if(tick%30==0&&sent<6){var gate=c.Battle.Lanes[sent%2].Entry;var p=Gui(gate);var bench=ManualBattleController.ShopBenchRect(sent%c.Shop.Bench.Count).center;c.ProcessShopPointer(EventType.MouseDown,bench);c.ProcessShopPointer(EventType.MouseDrag,p);c.ProcessShopPointer(EventType.MouseUp,p);sent++;}
                    c.Tick(.1f);if(tick%100==0)yield return null;
                }
                foreach(var view in Object.FindObjectsByType<ActorGrounding>(FindObjectsSortMode.None))Assert.That(view.Shadow.transform.position.y,Is.EqualTo(view.transform.position.y+.016f).Within(.002f));
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),"stage "+stage+" / "+c.Battle.Clock);
                int gold=c.Shop.Gold;c.Tick(20);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
            }
            Assert.That(c.Battle.Units.Select(u=>u.CardId).Distinct().Count(),Is.GreaterThan(1));
            Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(c.Shop.Bench.Count,Is.GreaterThan(1));
            c.RestartBattle();Assert.That(c.Loop.Stage,Is.EqualTo(1));Assert.That(c.Shop.Gold,Is.EqualTo(10));Assert.That(c.Battle.Units,Is.Empty);c.LeaveShopTrial();Time.timeScale=1;
        }
        [UnityTest]public IEnumerator FailureCannotReopenShopAndRestartRemovesItsRewards()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            try
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartBattleShopLoop(73);c.StartBattle();
                for(int i=0;i<6;i++)
                {
                    var receipt=c.Battle.TryDeploy(c.Battle.RunId,i+1,c.Battle.Entry);Assert.That(receipt.Success,Is.True);
                    var u=c.Battle.Units.Last();c.Battle.TryDamageUnit(u.EntityId,1000000,CombatDamageType.Arts);c.Tick(5);
                }
                Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(c.Shop.Gold,Is.EqualTo(16));
                Assert.That(c.OpenLoopShop(),Is.False);Assert.That(c.PurchaseShopCard(0).Success,Is.False);
                c.RestartBattle();Assert.That(c.Shop.Gold,Is.EqualTo(10));Assert.That(c.Shop.Level,Is.EqualTo(1));Assert.That(c.Battle.Clock,Is.Zero);c.LeaveShopTrial();
            }finally{Time.timeScale=1;}
        }
    }
}
