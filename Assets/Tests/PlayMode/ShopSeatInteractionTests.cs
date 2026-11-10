using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopSeatInteractionTests
    {
        [TearDown]public void RestoreClock(){Time.timeScale=1;}
        static Rect Bounds(ManualBattleController c,int instance)
            =>c.ShopPieceRect(c.Shop.Bench.ToList().FindIndex(p=>p.Instance==instance));
        static void Drag(ManualBattleController c,int instance,Rect destination)
        {
            c.ProcessShopPointer(EventType.MouseDown,Bounds(c,instance).center);
            c.ProcessShopPointer(EventType.MouseDrag,destination.center);
            c.ProcessShopPointer(EventType.MouseUp,destination.center);
        }
        static void Field(ManualBattleController c,string name,object value)
            =>typeof(ManualBattleController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,value);
        [UnityTest]public IEnumerator JourneySupplyPreparationMovesSwapsAndRejectsLockedSeatsUntilClaim()
        {
            float previousScale=Time.timeScale;Time.timeScale=0;CampaignJourneySession.Clear();
            ManualBattleController c=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");
                yield return CampaignJourneyEvidenceProbe.Host(value=>c=value);
                c.BeginJourney(44);yield return CampaignJourneyEvidenceProbe.Ready(1,value=>c=value);
                var shop=c.Shop;var loop=c.Loop;
                Assert.That(loop.CanShop,Is.True,"The first supply opens directly in preparation.");
                Assert.That(loop.RewardStage,Is.True);Assert.That(c.Battle.GuaranteedSupply,Is.True);
                Assert.That(shop.Level,Is.EqualTo(1));Assert.That(shop.Bench.Count,Is.EqualTo(3));
                var selected=shop.Bench.Single(p=>shop.IsLineupInstance(p.Instance));
                var reserve=shop.Bench.First(p=>!shop.IsLineupInstance(p.Instance));
                Rect selectedSeat=Bounds(c,selected.Instance),reserveSeat=Bounds(c,reserve.Instance);
                string economy=CampaignJourneyEvidenceProbe.State(shop);

                // A full one-card formation still allows an atomic exchange with reserve.
                Drag(c,reserve.Instance,selectedSeat);
                Assert.That(Bounds(c,reserve.Instance),Is.EqualTo(selectedSeat));
                Assert.That(Bounds(c,selected.Instance),Is.EqualTo(reserveSeat));
                Assert.That(shop.IsLineupInstance(reserve.Instance),Is.True);
                Assert.That(shop.IsLineupInstance(selected.Instance),Is.False);

                Drag(c,selected.Instance,ManualBattleController.ShopBenchRect(8));
                Assert.That(Bounds(c,selected.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(8)));
                Drag(c,selected.Instance,ManualBattleController.ShopLineupRect(1));
                Assert.That(Bounds(c,selected.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(8)),
                    "A drag cannot put a second card into a locked population slot.");
                Assert.That(shop.Bench.Count(p=>shop.IsLineupInstance(p.Instance)),Is.EqualTo(1));

                Drag(c,reserve.Instance,ManualBattleController.ShopBenchRect(7));
                Assert.That(Bounds(c,reserve.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(7)));
                Assert.That(shop.Bench.Any(p=>shop.IsLineupInstance(p.Instance)),Is.False);
                Drag(c,reserve.Instance,ManualBattleController.ShopLineupRect(0));
                Assert.That(Bounds(c,reserve.Instance),Is.EqualTo(ManualBattleController.ShopLineupRect(0)));
                Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(economy));
                Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);
                Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);

                // The public supply action closes editing immediately, including a drag in flight.
                c.ProcessShopPointer(EventType.MouseDown,Bounds(c,reserve.Instance).center);
                c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopBenchRect(6).center);
                c.ClaimCampaignSupply();
                Assert.That(loop.Phase,Is.EqualTo(BattleShopPhase.Settlement));
                Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));
                c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopBenchRect(6).center);
                string claimedEconomy=CampaignJourneyEvidenceProbe.State(shop);
                var claimedLineup=shop.Bench.Where(p=>shop.IsLineupInstance(p.Instance)).Select(p=>p.Instance).ToArray();
                Drag(c,reserve.Instance,ManualBattleController.ShopBenchRect(5));
                Assert.That(c.MoveShopCard(reserve.Instance,false,4),Is.False);
                CollectionAssert.AreEqual(claimedLineup,shop.Bench.Where(p=>shop.IsLineupInstance(p.Instance)).Select(p=>p.Instance).ToArray());
                CollectionAssert.AreEqual(new[]{reserve.Instance},claimedLineup,
                    "Claiming cannot commit a pending move or replace the prepared formation.");
                Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(claimedEconomy));
                Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);
                Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);
                c.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(value=>c=value);
            }
            finally{CampaignJourneySession.Clear();Time.timeScale=previousScale;}
        }
        [UnityTest]public IEnumerator DragMovesSwapsAndCancelsThenStartCompactsOnlyLineup()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartCampaign(true,8125);
            yield return null;yield return new WaitForSecondsRealtime(.3f);
            var first=c.Shop.Bench[0];var second=c.Shop.Bench[1];int gold=c.Shop.Gold,count=c.Shop.Bench.Count;
            Drag(c,first.Instance,ManualBattleController.ShopLineupRect(7));
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopLineupRect(7)));
            Assert.That(Bounds(c,second.Instance),Is.EqualTo(ManualBattleController.ShopLineupRect(1)));
            Drag(c,first.Instance,ManualBattleController.ShopLineupRect(1));
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopLineupRect(1)));
            Assert.That(Bounds(c,second.Instance),Is.EqualTo(ManualBattleController.ShopLineupRect(7)));
            Drag(c,first.Instance,ManualBattleController.ShopBenchRect(6));
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(6)));
            // Rebuilding through an existing preparation option must retain holes and reserve choices.
            c.SetShopVisible(false);c.SetShopVisible(true);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(6)));
            c.ProcessShopPointer(EventType.MouseDown,Bounds(c,first.Instance).center);
            c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopLineupRect(1).center);
            c.CancelShopDrag();c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopLineupRect(1).center);
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(6)));
            c.ProcessShopPointer(EventType.MouseDown,Bounds(c,first.Instance).center);
            c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopLineupRect(1).center);
            Field(c,"loopMenuOpen",true);c.ProcessShopPointer(EventType.MouseMove,ManualBattleController.ShopLineupRect(1).center);
            Field(c,"loopMenuOpen",false);c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopLineupRect(1).center);
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(6)),"A modal cancels the entire pending move.");
            c.ProcessShopPointer(EventType.MouseDown,Bounds(c,first.Instance).center);
            c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopLineupRect(1).center);
            bool diagnostic=(bool)typeof(ManualBattleController).GetField("diagnostic",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
            try
            {
                Field(c,"diagnostic",false); // Batch diagnostics skip focus handling; exercise the ordinary player path explicitly.
                c.SendMessage("OnApplicationFocus",false,SendMessageOptions.RequireReceiver);
            }
            finally{Field(c,"diagnostic",diagnostic);}
            c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopLineupRect(1).center);
            Assert.That(Bounds(c,first.Instance),Is.EqualTo(ManualBattleController.ShopBenchRect(6)),"Losing focus cancels the pending move.");
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(c.Battle.Clock,Is.Zero,"Preparation remains frozen while focus loss cancels the drag.");
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return null;
            Assert.That(c.Battle.FindCard(first.Id),Is.Null,"Manual reserve choice survives the battle rebuild.");
            Assert.That(c.Shop.Bench.Where(p=>c.Shop.IsLineupInstance(p.Instance)).OrderBy(p=>Bounds(c,p.Instance).x).Last().Instance,
                Is.EqualTo(second.Instance),"Stable compaction leaves the rightmost selected card last, independently of HUD spacing.");
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));
            c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator ExactSameKindSwapChangesProfileAndSynergyThenSaleLeavesARealHole()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartCampaign(true,9125);
            yield return null;yield return new WaitForSecondsRealtime(.3f);
            const string id="CARD_M08";
            Assert.That(c.CommissionCampaignCard(id).Success,Is.True);
            Assert.That(c.CommissionCampaignCard(id).Success,Is.True);
            Assert.That(c.CommissionCampaignCard(id).Success,Is.True);
            var high=c.Shop.Bench.Single(p=>p.Id==id&&p.Star==2);var low=c.Shop.Bench.Single(p=>p.Id==id&&p.Star==1);
            float highHealth=c.Battle.FindCard(id).Settings.Health;int highDP=c.Battle.FindCard(id).Settings.DeploymentCost;
            Assert.That(c.Battle.ReunionAverageStar,Is.GreaterThan(1));
            var highSeat=Bounds(c,high.Instance);var reserveSeat=Bounds(c,low.Instance);
            Drag(c,low.Instance,highSeat);
            Assert.That(Bounds(c,low.Instance),Is.EqualTo(highSeat));Assert.That(Bounds(c,high.Instance),Is.EqualTo(reserveSeat));
            Assert.That(c.Battle.FindCard(id).Settings.Health,Is.LessThan(highHealth));
            Assert.That(c.Battle.FindCard(id).Settings.DeploymentCost,Is.EqualTo(highDP-1));
            Assert.That(c.Battle.ReunionAverageStar,Is.EqualTo(1),"Synergy snapshots read the selected low-star card, not the reserve maximum.");
            int stock=c.Shop.Available(id),count=c.Shop.Bench.Count;
            Drag(c,low.Instance,ManualBattleController.ShopRightSaleDropRect);
            Assert.That(c.Shop.Bench.Count,Is.EqualTo(count-1));Assert.That(c.Shop.Available(id),Is.EqualTo(stock+1));
            Assert.That(c.Battle.FindCard(id),Is.Null,"Selling a lineup card never pulls the same kind out of reserve.");
            Assert.That(Bounds(c,high.Instance),Is.EqualTo(reserveSeat));
            c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator FullReserveAndLineupSwapAtomicallyThroughPointerInput()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartCampaign(true,10125);
            yield return null;yield return new WaitForSecondsRealtime(.3f);
            string[] ids={"CARD_M07","CARD_M08","CARD_M09","CARD_M11","CARD_M12","CARD_M13","CARD_M15","CARD_M16","CARD_M17","CARD_M18"};
            c.Shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),new string[0],10,0,100);
            var lineup=(List<string>)typeof(ManualBattleController).GetField("shopLineup",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
            lineup.Clear();lineup.AddRange(ids);Field(c,"seatedShop",null);
            typeof(ManualBattleController).GetMethod("SyncShopDeck",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{false,false});
            foreach(string id in ids)Assert.That(c.CommissionCampaignCard(id).Success,Is.True);
            Assert.That(c.Shop.Bench.Count,Is.EqualTo(20));
            var selected=c.Shop.Bench.First(p=>p.Id==ids[0]);var reserve=c.Shop.Bench.Last(p=>p.Id==ids[0]);
            var selectedSeat=Bounds(c,selected.Instance);var reserveSeat=Bounds(c,reserve.Instance);
            var offers=c.Shop.Offers.ToArray();int stock=c.Shop.Available(ids[0]),gold=c.Shop.Gold;
            Drag(c,reserve.Instance,selectedSeat);
            Assert.That(Bounds(c,reserve.Instance),Is.EqualTo(selectedSeat));Assert.That(Bounds(c,selected.Instance),Is.EqualTo(reserveSeat));
            Assert.That(c.Shop.Bench.Count,Is.EqualTo(20));Assert.That(c.Shop.Gold,Is.EqualTo(gold));
            Assert.That(c.Shop.Available(ids[0]),Is.EqualTo(stock));CollectionAssert.AreEqual(offers,c.Shop.Offers);c.Shop.Validate();
            c.LeaveShopTrial();yield return null;
        }
    }
}
