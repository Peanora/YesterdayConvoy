using System;
using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class CampaignJourneyRunTests
    {
        [TearDown]public void ClearSession()=>CampaignJourneySession.Clear();
        static ShopTrialModel NewShop()
        {
            var shop=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="CARD_M07",Name="士兵",Cost=1}},44);
            shop.EnableCampaign();shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M07",Star=1}},Array.Empty<string>(),1,0,10);
            return shop;
        }
        [Test]public void ControlledSettlement_ThirteenUniqueReceiptsUseV2Rewards()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);int bases=0;
            for(int stage=1;stage<=13;stage++)
            {
                Assert.That(run.Stage,Is.EqualTo(stage));Assert.That(run.RewardStage,Is.EqualTo(new[]{1,2,5,9,12}.Contains(stage)));
                Assert.That(run.Observe(BattlePhase.Won),Is.True);bases+=run.Reward.Base;
                int gold=shop.Gold,xp=shop.XP;
                Assert.That(run.Observe(BattlePhase.Won),Is.False);Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));
                if(stage<13){Assert.That(run.OpenShop(),Is.True);Assert.That(run.OpenShop(),Is.False);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(bases,Is.EqualTo(238));Assert.That(shop.Gold,Is.EqualTo(337));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(run.OpenShop(),Is.False);
            Assert.That(shop.Available("CARD_M07")+shop.Offers.Count(id=>id=="CARD_M07")+shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(30));
        }
        [Test]public void ControlledFailureEndsChallengeAndDoesNotAdvance()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);
            Assert.That(run.Observe(BattlePhase.Lost),Is.True);Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));
            Assert.That(run.OpenShop(),Is.False);Assert.That(run.RestartEncounter(),Is.False);
            Assert.That(run.Stage,Is.EqualTo(1));
        }
        [Test]public void RestartBeforeSettlementPreservesEconomyAndReceiptAvailability()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);
            run.Observe(BattlePhase.Won);run.OpenShop();
            shop.Locked=true;shop.Commission("CARD_M07");var offers=shop.Offers.ToArray();int gold=shop.Gold,xp=shop.XP,available=shop.Available("CARD_M07");
            run.PrepareBattle();Assert.That(run.RestartEncounter(),Is.True);
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));Assert.That(shop.Available("CARD_M07"),Is.EqualTo(available));
            Assert.That(shop.Offers,Is.EqualTo(offers));Assert.That(shop.Locked,Is.True);Assert.That(shop.CommissionUsed,Is.True);
            Assert.That(run.Reward.Key,Is.EqualTo("stage-1"));
        }
        [Test]public void SessionFreshChallengeClearsPriorIdentity()
        {
            var a=CampaignJourneySession.Begin(44);Assert.That(a.Seed,Is.EqualTo(44));
            var b=CampaignJourneySession.Begin(99);Assert.That(b,Is.Not.SameAs(a));Assert.That(b.Stage,Is.EqualTo(1));Assert.That(b.Ended,Is.False);
            CampaignJourneySession.Clear();Assert.That(CampaignJourneySession.Current,Is.Null);
        }
    }
}
