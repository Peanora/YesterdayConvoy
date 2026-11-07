using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopTrialTests
    {
        static ShopTrialModel Model()=>new ShopTrialModel(Enumerable.Range(0,20).Select(i=>new ShopTrialModel.Role{Id="R"+i,Name="R"+i,Cost=1}));
        static ShopTrialModel.Piece Piece(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        [TestCase(false,9,2)][TestCase(true,7,3)]
        public void FullBenchMergesInOnePurchaseAndPreviewIsReadOnly(bool chain,int expectedCount,int expectedStar)
        {
            var m=Model();var pieces=new[]{Piece("R0"),Piece("R0")}.ToList();if(chain){pieces.Add(Piece("R0",2));pieces.Add(Piece("R0",2));}
            pieces.AddRange(Enumerable.Range(1,10-pieces.Count).Select(i=>Piece("R"+i)));
            m.Preset(pieces,new[]{"R0","R19"});int gold=m.Gold;
            Assert.That(m.Purchase(1).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Bench.Count,Is.EqualTo(10));
            var before=m.Bench.Select(p=>$"{p.Instance}:{p.Id}:{p.Star}").ToArray();int stock=m.Available("R0");
            for(int i=0;i<20;i++){var preview=m.PreviewPurchase(0);Assert.That(preview.Success,Is.True);Assert.That(preview.FinalStar,Is.EqualTo(expectedStar));}
            CollectionAssert.AreEqual(before,m.Bench.Select(p=>$"{p.Instance}:{p.Id}:{p.Star}").ToArray());Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Available("R0"),Is.EqualTo(stock));
            var r=m.Purchase(0);Assert.That(r.Success,Is.True);Assert.That(r.FinalStar,Is.EqualTo(expectedStar));Assert.That(m.Bench.Count,Is.EqualTo(expectedCount));Assert.That(m.Gold,Is.EqualTo(gold-1));
            Assert.That(m.Purchase(0).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold-1));m.Validate();
        }
        [Test]public void FullBenchWithoutMergeCannotSpendOrReserveAnotherCard()
        {
            var m=Model();m.Preset(Enumerable.Range(0,10).Select(i=>Piece("R"+i)),new[]{"R0"});var before=m.Offers.ToArray();
            int stock=m.Available("R0"),gold=m.Gold;Assert.That(m.PreviewPurchase(0).Success,Is.False);Assert.That(m.Purchase(0).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Available("R0"),Is.EqualTo(stock));CollectionAssert.AreEqual(before,m.Offers);m.Validate();
        }
        [Test]public void NonCampaignHasTenIndependentReserveSeatsAndCannotMoveLineupIntoAnEleventh()
        {
            var m=Model();m.Preset(Enumerable.Range(0,10).Select(i=>Piece("R"+i)),new[]{"R10","R11"});int selected=m.Bench[0].Instance;
            Assert.That(m.CampaignEconomy,Is.False);Assert.That(m.SetLineup(new[]{"R0"},new[]{selected}));
            Assert.That(m.Purchase(0).Success);Assert.That(m.Bench.Count,Is.EqualTo(11));Assert.That(m.Bench.Count(p=>!m.IsLineupInstance(p.Instance)),Is.EqualTo(10));m.Validate();
            int gold=m.Gold,stock=m.Available("R11");Assert.That(m.Purchase(1).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Available("R11"),Is.EqualTo(stock));Assert.That(m.Offers[1],Is.EqualTo("R11"));
            Assert.That(m.SetLineup(new string[0],new int[0]),Is.False);Assert.That(m.IsLineupInstance(selected));Assert.That(m.Bench.Count,Is.EqualTo(11));m.Validate();
        }
        [Test]public void RankPreviewDoesNotAuthorizeUnaffordablePurchases()
        {
            var m=Model();m.Preset(new[]{Piece("R0"),Piece("R0")},new[]{"R0"},gold:0);
            Assert.That(m.PreviewPurchase(0).FinalStar,Is.EqualTo(2));int stock=m.Available("R0");
            Assert.That(m.Purchase(0).Success,Is.False);Assert.That(m.Gold,Is.Zero);Assert.That(m.Bench.Count,Is.EqualTo(2));Assert.That(m.Available("R0"),Is.EqualTo(stock));Assert.That(m.Offers[0],Is.EqualTo("R0"));m.Validate();
        }
        [Test]public void RankPreviewUpdatesAfterPurchaseAndSale()
        {
            var m=Model();m.Preset(new[]{Piece("R0"),Piece("R0")},new[]{"R0","R0"});
            Assert.That(m.PreviewPurchase(1).FinalStar,Is.EqualTo(2));var bought=m.Purchase(0);Assert.That(bought.FinalStar,Is.EqualTo(2));
            Assert.That(m.PreviewPurchase(0).Success,Is.False);Assert.That(m.PreviewPurchase(1).FinalStar,Is.EqualTo(1));m.Sell(bought.Instance);
            Assert.That(m.PreviewPurchase(1).FinalStar,Is.EqualTo(1));m.Validate();
        }
        [Test]public void QuoteAndSaleAgreeAndReturnNineCopiesOnlyOnce()
        {
            var m=Model();m.Preset(new[]{Piece("R0",3)},new[]{"R1"});int id=m.Bench[0].Instance,gold=m.Gold;
            Assert.That(m.Quote(id).Value,Is.EqualTo(8));Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Sell(id).Value,Is.EqualTo(8));Assert.That(m.Available("R0"),Is.EqualTo(30));Assert.That(m.Sell(id).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold+8));m.Validate();
        }
        [TestCase(1,30,4)][TestCase(2,25,3)][TestCase(3,18,3)][TestCase(4,10,3)][TestCase(5,9,3)]
        public void PerKindStockUsesRecruitCostAndRejectsOverReservationAtomically(int cost,int stock,int maxStar)
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=cost}});
            Assert.That(m.Capacity("A"),Is.EqualTo(stock));Assert.That(m.Available("A"),Is.EqualTo(stock));Assert.That(m.MaxStar("A"),Is.EqualTo(maxStar));
            var pieces=new System.Collections.Generic.List<ShopTrialModel.Piece>();int remaining=stock;
            for(int star=maxStar;star>=1;star--)while(remaining>=ShopTrialModel.Copies(star)){pieces.Add(Piece("A",star));remaining-=ShopTrialModel.Copies(star);}
            m.Preset(pieces,new string[0]);Assert.That(m.Available("A"),Is.Zero);var before=m.Bench.Select(p=>p.Instance+":"+p.Star).ToArray();
            Assert.Throws<System.ArgumentException>(()=>m.Preset(pieces,new[]{"A"}));
            CollectionAssert.AreEqual(before,m.Bench.Select(p=>p.Instance+":"+p.Star));Assert.That(m.Available("A"),Is.Zero);Assert.That(m.Offers.All(o=>o==null));m.Validate();
        }
        [Test]public void FullBenchCanChainOneTwoThreeToFourWithReadOnlyPreviewAndOnePayment()
        {
            var m=Model();var pieces=new[]{Piece("R0"),Piece("R0"),Piece("R0",2),Piece("R0",2),Piece("R0",3),Piece("R0",3)}.ToList();
            pieces.AddRange(Enumerable.Range(1,4).Select(i=>Piece("R"+i)));m.Preset(pieces,new[]{"R0"});int gold=m.Gold;
            Assert.That(m.Available("R0"),Is.EqualTo(3));
            for(int i=0;i<10;i++){Assert.That(m.PreviewPurchase(0).FinalStar,Is.EqualTo(4));Assert.That(m.Bench.Count,Is.EqualTo(10));Assert.That(m.Gold,Is.EqualTo(gold));}
            var result=m.Purchase(0);Assert.That(result.Success);Assert.That(result.FinalStar,Is.EqualTo(4));Assert.That(m.Bench.Count,Is.EqualTo(5));
            Assert.That(ShopTrialModel.Copies(m.Bench.Single(p=>p.Id=="R0").Star),Is.EqualTo(27));Assert.That(m.Gold,Is.EqualTo(gold-1));Assert.That(m.Available("R0"),Is.EqualTo(3));
            Assert.That(m.Purchase(0).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold-1));m.Validate();
        }
        [Test]public void FourStarStillLeavesThreeRealCopiesAndSellingReturnsTwentySevenOnce()
        {
            var m=Model();m.Preset(new[]{Piece("R0",4)},new[]{"R0","R0","R0"});int champion=m.Bench.Single().Instance;
            for(int i=0;i<3;i++)Assert.That(m.Purchase(i).Success);
            Assert.That(m.Bench.Count,Is.EqualTo(2));Assert.That(m.Bench.Single(p=>p.Instance!=champion).Star,Is.EqualTo(2));Assert.That(m.Available("R0"),Is.Zero);
            Assert.That(m.Bench.Where(p=>p.Id=="R0").Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(30));
            int gold=m.Gold;Assert.That(m.Quote(champion).Value,Is.EqualTo(26));Assert.That(m.Sell(champion).Value,Is.EqualTo(26));
            Assert.That(m.Available("R0"),Is.EqualTo(27));Assert.That(m.Gold,Is.EqualTo(gold+26));Assert.That(m.Sell(champion).Success,Is.False);
            Assert.That(m.Available("R0"),Is.EqualTo(27));Assert.That(m.Gold,Is.EqualTo(gold+26));m.Validate();
        }
        [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]public void FourStarIsRejectedForOtherRecruitCostsWithoutMutation(int cost)
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=cost}});m.Preset(new[]{Piece("A")},new[]{"A"});
            int stock=m.Available("A"),gold=m.Gold;Assert.Throws<System.ArgumentException>(()=>m.Preset(new[]{Piece("A",4)},new string[0]));
            Assert.That(m.Bench.Single().Star,Is.EqualTo(1));Assert.That(m.Available("A"),Is.EqualTo(stock));Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Offers[0],Is.EqualTo("A"));m.Validate();
        }
        [TestCase(false)][TestCase(true)]public void MergeKeepsSelectedLineupInstanceInsteadOfAnOlderBenchCopy(bool chain)
        {
            var m=Model();m.EnableCampaign();var pieces=new[]{Piece("R0"),Piece("R0")}.ToList();
            if(chain)pieces.AddRange(new[]{Piece("R0",2),Piece("R0",2),Piece("R0",3),Piece("R0",3)});
            m.Preset(pieces,new[]{"R0"},xp:0);int selected=m.Bench[1].Instance;
            Assert.That(m.SetLineup(new[]{"R0"},new[]{selected}));Assert.That(m.SetLineup(new[]{"R0"},new[]{int.MaxValue}),Is.False);
            Assert.That(m.IsLineupInstance(selected));var result=m.Purchase(0);
            Assert.That(result.Instance,Is.EqualTo(selected));Assert.That(m.Bench.Single().Instance,Is.EqualTo(selected));Assert.That(m.Bench.Single().Star,Is.EqualTo(chain?4:2));Assert.That(m.IsLineupInstance(selected));m.Validate();
        }
        [Test]public void RefreshAfterFourStarDisplaysOnlyThreeRemainingCopiesAndEmptyRefreshCannotCharge()
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=1}});m.Preset(new[]{Piece("A",4)},new string[0]);
            Assert.That(m.Refresh().Success);Assert.That(m.Offers.Count(o=>o=="A"),Is.EqualTo(3));Assert.That(m.Available("A"),Is.Zero);
            for(int i=0;i<3;i++)Assert.That(m.Purchase(i).Success);int gold=m.Gold;
            Assert.That(m.Refresh().Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));m.Validate();
        }
        [Test]public void ExperienceCarryAndLockedRerollPreserveCurrentShop()
        {
            var m=Model();m.Preset(new[]{Piece("R0")},new[]{"R1","R2"});var before=m.Offers.ToArray();Assert.That(m.Upgrade().Success,Is.True);Assert.That(m.Level,Is.EqualTo(6));Assert.That(m.XP,Is.EqualTo(2));CollectionAssert.AreEqual(before,m.Offers);
            m.Locked=true;int gold=m.Gold;Assert.That(m.Refresh().Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));CollectionAssert.AreEqual(before,m.Offers);m.Validate();
        }
        static System.Reflection.PropertyInfo Guarantee=>typeof(ShopTrialModel).GetProperty("GuaranteedRefreshOffer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        [Test]public void GuaranteedFirstOfferHonorsLockAndMoneyThenConsumesExactlyOneStock()
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="T",Name="Talulah",Cost=5},new ShopTrialModel.Role{Id="L",Name="Low",Cost=1}},73);
            m.Preset(new ShopTrialModel.Piece[0],new[]{"L"},level:7,xp:0,gold:1);Guarantee.SetValue(m,"T");
            m.Locked=true;Assert.That(m.Refresh(true).Success,Is.False);Assert.That(Guarantee.GetValue(m),Is.EqualTo("T"));
            m.Locked=false;Assert.That(m.Refresh().Success,Is.False);Assert.That(m.Gold,Is.EqualTo(1));Assert.That(m.Offers[0],Is.EqualTo("L"));
            Assert.That(m.Refresh(true).Success,Is.True);Assert.That(m.Offers[0],Is.EqualTo("T"));Assert.That(m.Offers.Count(x=>x!=null),Is.EqualTo(5));
            Assert.That(m.Gold,Is.EqualTo(1));Assert.That(m.Available("T")+m.Offers.Count(x=>x=="T"),Is.EqualTo(9));Assert.That(Guarantee.GetValue(m),Is.Null);m.Validate();
        }
        [Test]public void ExhaustedGuaranteedCardCannotChargeOrReplaceOffersAndNewRunClearsGuarantee()
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="T",Name="Talulah",Cost=5},new ShopTrialModel.Role{Id="L",Name="Low",Cost=1}},73);
            m.Preset(new[]{Piece("T",3)},new[]{"L"},level:7,xp:0,gold:20);Guarantee.SetValue(m,"T");
            Assert.That(m.Refresh().Success,Is.False);Assert.That(m.Gold,Is.EqualTo(20));Assert.That(m.Offers[0],Is.EqualTo("L"));Assert.That(m.Available("T"),Is.Zero);m.Validate();
            m.Preset(new ShopTrialModel.Piece[0],new string[0]);Assert.That(Guarantee.GetValue(m),Is.Null);
        }
    }
}
