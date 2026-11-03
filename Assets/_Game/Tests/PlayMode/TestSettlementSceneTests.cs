using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class TestSettlementSceneTests
    {
        static FieldInfo Switch=>typeof(ManualBattleController).GetField("loopTestSettlementOpen",BindingFlags.Instance|BindingFlags.NonPublic);
        static bool Finish(ManualBattleController c,bool won)=>(bool)typeof(ManualBattleController).GetMethod("TryTestSettlement",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{won});

        [UnityTest] public IEnumerator ReadyVictoryPaysOnceThenAllowsPurchaseAndSecondBattle()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;
                Assert.That((bool)Switch.GetValue(c),Is.False);Assert.That(Finish(c,true),Is.False);
                Switch.SetValue(c,true);Assert.That(c.ShopMapPointAvailable(new Vector2(1100,130)),Is.False);
                var b=c.Battle;Assert.That(Finish(c,true),Is.True);yield return null;
                Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.Defense,Is.Zero);Assert.That(b.Clock,Is.Zero);
                Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Settlement));Assert.That(c.Shop.Gold,Is.EqualTo(38));
                int gold=c.Shop.Gold;Switch.SetValue(c,true);Assert.That(Finish(c,false),Is.False);c.Tick(10);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
                Assert.That(c.OpenLoopShop(),Is.True);yield return new WaitForSecondsRealtime(.4f);
                Assert.That(c.ShopVisible,Is.True);Assert.That(c.Loop.Stage,Is.EqualTo(2));Assert.That((bool)Switch.GetValue(c),Is.False);
                int slot=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]!=null);string bought=c.Shop.Offers[slot];
                Assert.That(c.PurchaseShopCard(slot).Success,Is.True);Assert.That(c.Shop.Bench.Any(p=>p.Id==bought),Is.True);
                Assert.That(c.StartLoopNextBattle(),Is.True);yield return new WaitForSecondsRealtime(.3f);
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(c.Battle.DP,Is.EqualTo(24));Assert.That(c.Battle.Units,Is.Empty);
                Switch.SetValue(c,true);Assert.That(Finish(c,true),Is.True);Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(c.OpenLoopShop(),Is.False);
                c.LeaveShopTrial();
            }
            finally{Time.timeScale=previous;}
        }
        [UnityTest] public IEnumerator PausedFailureStopsCombatPaysOnceAndRestartResetsSwitch()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;
                c.StartBattle();Assert.That(c.Battle.TryDeploy(c.Battle.RunId,1,c.Battle.Entry).Success,Is.True);c.Tick(.5f);yield return null;
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;var position=c.Battle.Units[0].Motion.Position;
                Switch.SetValue(c,true);Assert.That(Finish(c,false),Is.True);c.Tick(20);yield return null;
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(c.Battle.IsPaused,Is.False);
                Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.Units[0].Motion.Position,Is.EqualTo(position));
                Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(c.Shop.Gold,Is.EqualTo(38));Assert.That(c.OpenLoopShop(),Is.False);
                Switch.SetValue(c,true);Assert.That(Finish(c,true),Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(38));
                c.RestartBattle();yield return null;
                Assert.That((bool)Switch.GetValue(c),Is.False);Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Preparation));Assert.That(c.Shop.Gold,Is.EqualTo(30));Assert.That(c.Battle.DP,Is.EqualTo(24));
                Assert.That(Finish(c,true),Is.False);c.LeaveShopTrial();
            }
            finally{Time.timeScale=previous;}
        }
    }
}
