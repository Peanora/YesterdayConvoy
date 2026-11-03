using System.Linq;
using NUnit.Framework;
namespace YesterdayConvoy.Tests
{
    public sealed class BattleShopRunTests
    {
        static ShopTrialModel New(int gold=10){var m=new ShopTrialModel(Enumerable.Range(0,10).Select(i=>new ShopTrialModel.Role{Id="R"+i,Name="R"+i,Cost=1}));m.Preset(new[]{new ShopTrialModel.Piece{Id="R0",Star=1}},new[]{"R1","R2","R3","R4","R5"},1,0,gold);return m;}
        [Test] public void SettlementIsIdempotentUsesPreRewardInterestAndAdvancesExperience()
        {var m=New(59);var a=m.SettleRound("1",true);Assert.That(a.Interest,Is.EqualTo(5));Assert.That(m.Gold,Is.EqualTo(69));Assert.That(m.Level,Is.EqualTo(2));Assert.That(m.SettleRound("1",false),Is.SameAs(a));Assert.That(m.Gold,Is.EqualTo(69));var b=m.SettleRound("2",true);Assert.That(b.Streak,Is.EqualTo(1));Assert.That(m.Gold,Is.EqualTo(80));m.Validate();}
        [Test] public void LockedShopAndInventoryPersistAcrossRewardAndShopEntry()
        {var m=New();m.Locked=true;var offers=m.Offers.ToArray();var run=new BattleShopRun(m,7);run.Observe(BattlePhase.Won);Assert.That(run.OpenShop(),Is.True);Assert.That(run.OpenShop(),Is.False);CollectionAssert.AreEqual(offers,m.Offers);Assert.That(m.Locked,Is.True);Assert.That(m.Bench.Single().Instance,Is.EqualTo(1));Assert.That(m.Available("R0"),Is.EqualTo(8));m.Validate();}
        [Test] public void FreeRefreshNeverChargesAndPurchasedCardSurvivesSecondBattle()
        {var m=New();var run=new BattleShopRun(m,7);run.Observe(BattlePhase.Won);int gold=m.Gold;run.OpenShop();Assert.That(m.Gold,Is.EqualTo(gold));var receipt=m.Purchase(0);Assert.That(receipt.Success,Is.True);int instance=receipt.Instance;Assert.That(run.PrepareBattle(),Is.True);run.Observe(BattlePhase.Won);Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(m.Bench.Any(p=>p.Instance==instance),Is.True);Assert.That(run.OpenShop(),Is.False);m.Validate();}
        [Test] public void FailureHasBaseAndInterestButCannotShopOrAdvance()
        {var m=New(30);var run=new BattleShopRun(m,7);run.Observe(BattlePhase.Lost);Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(run.Reward.Total,Is.EqualTo(8));Assert.That(run.Reward.Experience,Is.Zero);Assert.That(run.OpenShop(),Is.False);Assert.That(run.PrepareBattle(),Is.False);int gold=m.Gold;run.Observe(BattlePhase.Lost);Assert.That(m.Gold,Is.EqualTo(gold));}
        [Test] public void RunningHasNoRewardAndCannotOpenShop()
        {var m=New();var run=new BattleShopRun(m,7);run.Observe(BattlePhase.Running);Assert.That(run.CanShop,Is.False);Assert.That(run.OpenShop(),Is.False);Assert.That(m.Gold,Is.EqualTo(10));}
    }
}
