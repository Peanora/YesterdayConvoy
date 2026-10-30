using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopTrialTests
    {
        static ShopTrialModel Model()=>new ShopTrialModel(Enumerable.Range(0,20).Select(i=>new ShopTrialModel.Role{Id="R"+i,Name="R"+i,Cost=1}));
        static ShopTrialModel.Piece Piece(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        [TestCase(false,9,2)][TestCase(true,7,3)]
        public void FullBenchCanConfirmOneAtomicMerge(bool chain,int expectedCount,int expectedStar)
        {
            var m=Model();var pieces=new[]{Piece("R0"),Piece("R0")}.ToList();if(chain){pieces.Add(Piece("R0",2));pieces.Add(Piece("R0",2));}
            pieces.AddRange(Enumerable.Range(1,10-pieces.Count).Select(i=>Piece("R"+i)));
            m.Preset(pieces,new[]{"R0","R19"});int gold=m.Gold;
            Assert.That(m.Purchase(1).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Bench.Count,Is.EqualTo(10));
            Assert.That(m.Purchase(0).NeedsConfirmation,Is.True);Assert.That(m.Gold,Is.EqualTo(gold));
            var r=m.Purchase(0,true);Assert.That(r.Success,Is.True);Assert.That(r.FinalStar,Is.EqualTo(expectedStar));Assert.That(m.Bench.Count,Is.EqualTo(expectedCount));Assert.That(m.Gold,Is.EqualTo(gold-1));m.Validate();
        }
        [Test]public void FullBenchWithoutMergeCannotSpendOrReserveAnotherCard()
        {
            var m=Model();m.Preset(Enumerable.Range(0,10).Select(i=>Piece("R"+i)),new[]{"R0"});var before=m.Offers.ToArray();
            int stock=m.Available("R0"),gold=m.Gold;Assert.That(m.Purchase(0,true).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Available("R0"),Is.EqualTo(stock));CollectionAssert.AreEqual(before,m.Offers);m.Validate();
        }
        [Test]public void QuoteAndSaleAgreeAndReturnNineCopiesOnlyOnce()
        {
            var m=Model();m.Preset(new[]{Piece("R0",3)},new[]{"R1"});int id=m.Bench[0].Instance,gold=m.Gold;
            Assert.That(m.Quote(id).Value,Is.EqualTo(8));Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.Sell(id).Value,Is.EqualTo(8));Assert.That(m.Available("R0"),Is.EqualTo(9));Assert.That(m.Sell(id).Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold+8));m.Validate();
        }
        [Test]public void ExperienceCarryAndLockedRerollPreserveCurrentShop()
        {
            var m=Model();m.Preset(new[]{Piece("R0")},new[]{"R1","R2"});var before=m.Offers.ToArray();Assert.That(m.Upgrade().Success,Is.True);Assert.That(m.Level,Is.EqualTo(6));Assert.That(m.XP,Is.EqualTo(2));CollectionAssert.AreEqual(before,m.Offers);
            m.Locked=true;int gold=m.Gold;Assert.That(m.Refresh().Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));CollectionAssert.AreEqual(before,m.Offers);m.Validate();
        }
    }
}
