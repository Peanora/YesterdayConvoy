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
            shop.EnableJourneyEconomy();shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M07",Star=1}},Array.Empty<string>(),1,0,5);
            return shop;
        }
        static void AdvanceTo(BattleShopRun run,int stage)
        {
            while(run.Stage<stage)
            {
                Assert.That(run.Observe(BattlePhase.Won),Is.True);
                Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);
            }
        }
        [Test]public void ControlledSettlement_ThirteenUniqueReceiptsUseV2Rewards()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);int bases=0;
            for(int stage=1;stage<=13;stage++)
            {
                Assert.That(run.Stage,Is.EqualTo(stage));Assert.That(run.RewardStage,Is.EqualTo(new[]{1,2,5,9,12}.Contains(stage)));
                Assert.That(run.Observe(BattlePhase.Won),Is.True);bases+=run.Reward.Base;
                Assert.That(run.Health,Is.EqualTo(100));Assert.That(run.LastHealthLoss,Is.Zero);Assert.That(run.LastBattleWon,Is.True);
                int gold=shop.Gold,xp=shop.XP;
                Assert.That(run.Observe(BattlePhase.Won),Is.False);Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));
                if(stage<13){Assert.That(run.OpenShop(),Is.True);Assert.That(run.OpenShop(),Is.False);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(bases,Is.EqualTo(71));Assert.That(shop.Gold,Is.EqualTo(134));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(run.OpenShop(),Is.False);
            Assert.That(shop.Available("CARD_M07")+shop.Offers.Count(id=>id=="CARD_M07")+shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(30));
        }
        [TestCase(0,3,3,0)]
        [TestCase(1,3,3,7)]
        [TestCase(2,3,3,14)]
        [TestCase(3,3,3,20)]
        [TestCase(1,6,6,5)]
        [TestCase(5,6,6,25)]
        [TestCase(6,6,6,30)]
        [TestCase(1,6,13,10)]
        [TestCase(3,6,13,30)]
        [TestCase(6,6,13,60)]
        public void HealthLossUsesUnmetGateFractionWithIntegerCeiling(int remaining,int initial,int stage,int expected)
        {
            Assert.That(CampaignJourneyHealth.CalculateLoss(remaining,initial,stage),Is.EqualTo(expected));
        }
        [TestCase(1,0)]
        [TestCase(2,0)]
        [TestCase(3,20)]
        [TestCase(4,20)]
        [TestCase(5,0)]
        [TestCase(6,30)]
        [TestCase(7,25)]
        [TestCase(8,30)]
        [TestCase(9,0)]
        [TestCase(10,45)]
        [TestCase(11,35)]
        [TestCase(12,0)]
        [TestCase(13,60)]
        public void StageDamageLimitIncreasesLateCampaignPressureAndKeepsSuppliesSafe(int stage,int expected)
        {
            Assert.That(CampaignJourneyHealth.DamageLimit(stage),Is.EqualTo(expected));
        }
        [TestCase(0)]
        [TestCase(14)]
        public void StageDamageLimitRejectsUnknownStages(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignJourneyHealth.DamageLimit(stage));
        }
        [TestCase(-1,3)]
        [TestCase(4,3)]
        [TestCase(0,0)]
        [TestCase(0,-1)]
        public void HealthLossRejectsInvalidGateSnapshots(int remaining,int initial)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignJourneyHealth.CalculateLoss(remaining,initial,3));
        }
        [Test]public void SurvivingLossSettlesNormalDefeatRewardsAndAllowsNextStageOnce()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);AdvanceTo(run,4);
            Assert.That(shop.WinStreak,Is.EqualTo(1));int before=shop.Gold;
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:1,initialDefense:2),Is.True);
            Assert.That(run.LastBattleWon,Is.False);Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.MaxHealth,Is.EqualTo(100));
            Assert.That(run.LastHealthLoss,Is.EqualTo(10));Assert.That(run.LastRemainingDefense,Is.EqualTo(1));Assert.That(run.LastInitialDefense,Is.EqualTo(2));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Settlement));Assert.That(run.Reward.Base,Is.EqualTo(5));Assert.That(run.Reward.Experience,Is.Zero);
            Assert.That(shop.WinStreak,Is.Zero);Assert.That(run.Reward.Streak,Is.Zero);Assert.That(shop.Gold,Is.EqualTo(before+run.Reward.Base+run.Reward.Interest));
            int settledGold=shop.Gold,settledXP=shop.XP;
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:2,initialDefense:2),Is.False);
            Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.LastHealthLoss,Is.EqualTo(10));Assert.That(shop.Gold,Is.EqualTo(settledGold));Assert.That(shop.XP,Is.EqualTo(settledXP));
            Assert.That(run.RestartEncounter(),Is.False);Assert.That(run.OpenShop(),Is.True);Assert.That(run.Stage,Is.EqualTo(5));Assert.That(run.Health,Is.EqualTo(90));
            Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void FailedCombatRequiresActualDefenseBeforeChangingHealthOrEconomy()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);AdvanceTo(run,3);int gold=shop.Gold,xp=shop.XP;
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost));
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost,remainingDefense:1));
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost,initialDefense:1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Observe(BattlePhase.Lost,remainingDefense:2,initialDefense:1));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Preparation));Assert.That(run.Health,Is.EqualTo(100));
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));Assert.That(run.Reward.Key,Is.EqualTo("stage-2"));
        }
        [Test]public void CumulativeLossReachesZeroAndEndsChallengeWithoutAdvancing()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);int[] expected={80,60,30,5,0};int lossIndex=0;
            for(int stage=1;stage<=8;stage++)
            {
                if(run.RewardStage)Assert.That(run.Observe(BattlePhase.Won),Is.True);
                else
                {
                    int defense=CampaignJourneyCatalog.Stage(stage).defense;
                    Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:defense,initialDefense:defense),Is.True);
                    Assert.That(run.Health,Is.EqualTo(expected[lossIndex++]));
                }
                if(stage<8){Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(run.LastHealthLoss,Is.EqualTo(5),"Actual health removed is clamped at zero.");
            Assert.That(run.OpenShop(),Is.False);Assert.That(run.RestartEncounter(),Is.False);Assert.That(run.Stage,Is.EqualTo(8));
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:4,initialDefense:4),Is.False);Assert.That(run.Health,Is.Zero);
        }
        [Test]public void SurvivingFinalBossLossCompletesJourneyWithoutClaimingBattleVictory()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);AdvanceTo(run,13);
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:3,initialDefense:6),Is.True);
            Assert.That(run.LastBattleWon,Is.False);Assert.That(run.LastHealthLoss,Is.EqualTo(30));Assert.That(run.Health,Is.EqualTo(70));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void LosingAllThreeBossBattlesExhaustsHealthAtFinalStage()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);int[] expected={70,25,0};int bossIndex=0;
            for(int stage=1;stage<=13;stage++)
            {
                if(new[]{6,10,13}.Contains(stage))
                {
                    int defense=CampaignJourneyCatalog.Stage(stage).defense;
                    Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:defense,initialDefense:defense),Is.True);
                    Assert.That(run.Health,Is.EqualTo(expected[bossIndex++]));
                }
                else Assert.That(run.Observe(BattlePhase.Won),Is.True);
                if(stage<13){Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(run.Health,Is.Zero);Assert.That(run.LastBattleWon,Is.False);
            Assert.That(run.LastHealthLoss,Is.EqualTo(25));Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void SupplyAndVictoryPreserveHealthAndFreshChallengeResetsIt()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);AdvanceTo(run,4);
            run.Observe(BattlePhase.Lost,remainingDefense:1,initialDefense:2);run.OpenShop();run.PrepareBattle();
            Assert.That(run.Stage,Is.EqualTo(5));Assert.That(run.Health,Is.EqualTo(90));
            Assert.That(run.Observe(BattlePhase.Won,remainingDefense:0,initialDefense:1),Is.True);
            Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.LastHealthLoss,Is.Zero);
            run.OpenShop();run.PrepareBattle();Assert.That(run.RestartEncounter(),Is.True);Assert.That(run.Health,Is.EqualTo(90));
            run.Observe(BattlePhase.Won,remainingDefense:0,initialDefense:3);Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.LastHealthLoss,Is.Zero);
            Assert.That(new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true).Health,Is.EqualTo(100));
        }
        [Test]public void LegacyChallengeStillEndsOnItsFirstDefeatWithoutJourneyHealth()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true);
            Assert.That(run.Observe(BattlePhase.Lost),Is.True);Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));
            Assert.That(run.MaxHealth,Is.Zero);Assert.That(run.Health,Is.Zero);Assert.That(run.OpenShop(),Is.False);
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
        [Test]public void OpeningSuppliesRemainSmallAndDoNotCreateCombatWinStreak()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);
            run.Observe(BattlePhase.Won);Assert.That(shop.Gold,Is.EqualTo(8));Assert.That(shop.WinStreak,Is.Zero);
            run.OpenShop();run.PrepareBattle();run.Observe(BattlePhase.Won);
            Assert.That(shop.Gold,Is.EqualTo(12));Assert.That(shop.WinStreak,Is.Zero);Assert.That(run.Reward.Streak,Is.Zero);
            for(int stage=3;stage<=5;stage++)
            {
                run.OpenShop();run.PrepareBattle();run.Observe(BattlePhase.Won);
                if(stage==4){Assert.That(shop.WinStreak,Is.EqualTo(2));Assert.That(run.Reward.Streak,Is.EqualTo(1));}
            }
            Assert.That(shop.WinStreak,Is.EqualTo(2));Assert.That(run.Reward.Streak,Is.Zero,"Supply neither advances nor cashes the combat streak.");
        }
        [Test]public void ShortJourneyXpCurveDoesNotRetuneLegacyTrialEconomy()
        {
            var shop=NewShop();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),5,0,5);
            Assert.That(shop.NextLevelXP,Is.EqualTo(10));shop.EnableCampaign();Assert.That(shop.NextLevelXP,Is.EqualTo(14));
            shop.EnableJourneyEconomy();Assert.That(shop.NextLevelXP,Is.EqualTo(10));
            Assert.That(shop.Upgrade().Success,Is.True);Assert.That(shop.Gold,Is.EqualTo(1));Assert.That(shop.XP,Is.EqualTo(4));
            Assert.That(shop.Upgrade().Success,Is.False);Assert.That(shop.Gold,Is.EqualTo(1));Assert.That(shop.XP,Is.EqualTo(4));
        }
    }
}
