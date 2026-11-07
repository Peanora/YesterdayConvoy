using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class LateBattleShopSceneTests
    {
        [UnityTest]public IEnumerator LatePresetKeepsCostBasedStockAndBoughtRoleEntersRealSecondBattle()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            try
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);yield return null;
                Assert.That(c.Shop.Level,Is.EqualTo(7));Assert.That(c.Shop.Gold,Is.EqualTo(30));Assert.That(c.Shop.Bench.Count,Is.EqualTo(5));
                foreach(var piece in c.Shop.Bench)Assert.That(c.Shop.Available(piece.Id),Is.EqualTo(c.Shop.Capacity(piece.Id)-1));
                var economy=c.Shop;string bought=null;
                for(int stage=1;stage<=2;stage++)
                {
                    if(stage==2)
                    {
                        Assert.That(c.OpenLoopShop(),Is.True);yield return new WaitForSecondsRealtime(.35f);
                        int before=c.Shop.Bench.Count,goldBefore=c.Shop.Gold;
                        c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,new Vector2(640,480));c.ProcessShopPointer(EventType.MouseUp,new Vector2(640,480));
                        Assert.That(c.ShopVisible,Is.True,"Cancelling a reserve drag must keep the mandatory between-stage store operable.");
                        Assert.That(c.Shop.Bench.Count,Is.EqualTo(before));Assert.That(c.Shop.Gold,Is.EqualTo(goldBefore));
                        for(int roll=0;roll<10&&bought==null;roll++)
                        {
                            int slot=Enumerable.Range(0,5).Where(i=>c.Shop.Offers[i]=="CARD_M15").DefaultIfEmpty(-1).First();
                            if(slot>=0){bought=c.Shop.Offers[slot];Assert.That(c.PurchaseShopCard(slot).Success,Is.True);}
                            else Assert.That(c.Shop.Refresh().Success,Is.True);
                        }
                        Assert.That(bought,Is.Not.Null);yield return new WaitForSecondsRealtime(.85f);
                        Assert.That(c.ShopBenchModel(c.Shop.Bench.Last().Instance),Is.Not.Null);
                        Assert.That(c.StartLoopNextBattle(),Is.True);yield return new WaitForSecondsRealtime(.25f);
                    }
                    Assert.That(c.Shop,Is.SameAs(economy));Assert.That(c.Battle.DP,Is.EqualTo(24));Assert.That(c.Battle.Clock,Is.Zero);
                    Assert.That(c.Battle.Defenders.Count,Is.EqualTo(6));Assert.That(c.Battle.Defenders.Count(d=>d.HighGround),Is.EqualTo(3));
                    var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);int index=0;
                    for(int tick=0;tick<2600&&c.Loop.CanDeploy;tick++)
                    {
                        while(index<plan.orders.Length&&plan.orders[index].time<=c.Battle.Clock+.001)
                        {
                            var order=plan.orders[index++];string card=stage==2&&index==2?bought:order.card;
                            int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==card);var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[order.lane].Entry));
                            int count=c.Battle.Units.Count;c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                            Assert.That(c.Battle.Units.Count,Is.EqualTo(count+1),"drag "+index+" stage "+stage);
                            if(index==1){c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(8);Assert.That(c.Battle.Clock,Is.EqualTo(clock));c.Battle.SetPaused(false);}
                        }
                        c.Tick(.1f);if(tick%100==0)yield return null;
                    }
                    Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),"stage "+stage);int gold=c.Shop.Gold;c.Tick(10);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
                }
                Assert.That(c.Battle.Units.Any(u=>u.CardId==bought),Is.True);Assert.That(c.Loop.Phase,Is.EqualTo(BattleShopPhase.Complete));c.Shop.Validate();
                c.RestartBattle();Assert.That(c.Loop.LateGame,Is.True);Assert.That(c.Shop.Level,Is.EqualTo(7));Assert.That(c.Shop.Gold,Is.EqualTo(30));Assert.That(c.Shop.Bench.Count,Is.EqualTo(5));
                c.LeaveShopTrial();
            }
            finally{Time.timeScale=1;}
        }
    }
}
