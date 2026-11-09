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
        [Test]public void ControlledSettlement_TwelveUniqueReceiptsUseV2Rewards()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);int bases=0;
            for(int stage=1;stage<=12;stage++)
            {
                Assert.That(run.Stage,Is.EqualTo(stage));Assert.That(run.RewardStage,Is.EqualTo(new[]{1,4,8,11}.Contains(stage)));
                Assert.That(run.Observe(BattlePhase.Won),Is.True);bases+=run.Reward.Base;
                Assert.That(run.Health,Is.EqualTo(100));Assert.That(run.LastHealthLoss,Is.Zero);Assert.That(run.LastBattleWon,Is.True);
                int gold=shop.Gold,xp=shop.XP;
                Assert.That(run.Observe(BattlePhase.Won),Is.False);Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));
                if(stage<12){Assert.That(run.OpenShop(),Is.True);Assert.That(run.OpenShop(),Is.False);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(run.TotalStages,Is.EqualTo(12));Assert.That(bases,Is.EqualTo(136));Assert.That(shop.Gold,Is.EqualTo(269));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(run.OpenShop(),Is.False);
            Assert.That(shop.Available("CARD_M07")+shop.Offers.Count(id=>id=="CARD_M07")+shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(30));
        }
        [TestCase(0,3,2,0)]
        [TestCase(1,3,2,7)]
        [TestCase(2,3,2,14)]
        [TestCase(3,3,2,20)]
        [TestCase(1,6,5,5)]
        [TestCase(5,6,5,25)]
        [TestCase(6,6,5,30)]
        [TestCase(1,6,12,10)]
        [TestCase(3,6,12,30)]
        [TestCase(6,6,12,60)]
        public void HealthLossUsesUnmetGateFractionWithIntegerCeiling(int remaining,int initial,int stage,int expected)
        {
            Assert.That(CampaignJourneyHealth.CalculateLoss(remaining,initial,stage),Is.EqualTo(expected));
        }
        [TestCase(1,0)]
        [TestCase(2,20)]
        [TestCase(3,20)]
        [TestCase(4,0)]
        [TestCase(5,30)]
        [TestCase(6,25)]
        [TestCase(7,30)]
        [TestCase(8,0)]
        [TestCase(9,45)]
        [TestCase(10,35)]
        [TestCase(11,0)]
        [TestCase(12,60)]
        public void StageDamageLimitIncreasesLateCampaignPressureAndKeepsSuppliesSafe(int stage,int expected)
        {
            Assert.That(CampaignJourneyHealth.DamageLimit(stage),Is.EqualTo(expected));
        }
        [TestCase(0)]
        [TestCase(13)]
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
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);AdvanceTo(run,3);
            Assert.That(shop.WinStreak,Is.EqualTo(1));int before=shop.Gold;
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:1,initialDefense:2),Is.True);
            Assert.That(run.LastBattleWon,Is.False);Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.MaxHealth,Is.EqualTo(100));
            Assert.That(run.LastHealthLoss,Is.EqualTo(10));Assert.That(run.LastRemainingDefense,Is.EqualTo(1));Assert.That(run.LastInitialDefense,Is.EqualTo(2));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Settlement));Assert.That(run.Reward.Base,Is.EqualTo(10));Assert.That(run.Reward.Experience,Is.Zero);
            Assert.That(shop.WinStreak,Is.Zero);Assert.That(run.Reward.Streak,Is.Zero);Assert.That(shop.Gold,Is.EqualTo(before+run.Reward.Base+run.Reward.Interest));
            int settledGold=shop.Gold,settledXP=shop.XP;
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:2,initialDefense:2),Is.False);
            Assert.That(run.Health,Is.EqualTo(90));Assert.That(run.LastHealthLoss,Is.EqualTo(10));Assert.That(shop.Gold,Is.EqualTo(settledGold));Assert.That(shop.XP,Is.EqualTo(settledXP));
            Assert.That(run.RestartEncounter(),Is.False);Assert.That(run.OpenShop(),Is.True);Assert.That(run.Stage,Is.EqualTo(4));Assert.That(run.Health,Is.EqualTo(90));
            Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void FailedCombatRequiresActualDefenseBeforeChangingHealthOrEconomy()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);AdvanceTo(run,2);int gold=shop.Gold,xp=shop.XP;
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost));
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost,remainingDefense:1));
            Assert.Throws<ArgumentException>(()=>run.Observe(BattlePhase.Lost,initialDefense:1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.Observe(BattlePhase.Lost,remainingDefense:2,initialDefense:1));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Preparation));Assert.That(run.Health,Is.EqualTo(100));
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));Assert.That(run.Reward.Key,Is.EqualTo("stage-1"));
        }
        [Test]public void CumulativeLossReachesZeroAndEndsChallengeWithoutAdvancing()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);int[] expected={80,60,30,5,0};int lossIndex=0;
            for(int stage=1;stage<=7;stage++)
            {
                if(run.RewardStage)Assert.That(run.Observe(BattlePhase.Won),Is.True);
                else
                {
                    int defense=CampaignJourneyCatalog.Stage(stage).defense;
                    Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:defense,initialDefense:defense),Is.True);
                    Assert.That(run.Health,Is.EqualTo(expected[lossIndex++]));
                }
                if(stage<7){Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(run.LastHealthLoss,Is.EqualTo(5),"Actual health removed is clamped at zero.");
            Assert.That(run.OpenShop(),Is.False);Assert.That(run.RestartEncounter(),Is.False);Assert.That(run.Stage,Is.EqualTo(7));
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:4,initialDefense:4),Is.False);Assert.That(run.Health,Is.Zero);
        }
        [Test]public void SurvivingFinalBossLossCompletesJourneyWithoutClaimingBattleVictory()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);AdvanceTo(run,12);
            Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:3,initialDefense:6),Is.True);
            Assert.That(run.LastBattleWon,Is.False);Assert.That(run.LastHealthLoss,Is.EqualTo(30));Assert.That(run.Health,Is.EqualTo(70));
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Complete));Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void LosingAllThreeBossBattlesExhaustsHealthAtFinalStage()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);int[] expected={70,25,0};int bossIndex=0;
            for(int stage=1;stage<=12;stage++)
            {
                if(new[]{5,9,12}.Contains(stage))
                {
                    int defense=CampaignJourneyCatalog.Stage(stage).defense;
                    Assert.That(run.Observe(BattlePhase.Lost,remainingDefense:defense,initialDefense:defense),Is.True);
                    Assert.That(run.Health,Is.EqualTo(expected[bossIndex++]));
                }
                else Assert.That(run.Observe(BattlePhase.Won),Is.True);
                if(stage<12){Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);}
            }
            Assert.That(run.Phase,Is.EqualTo(BattleShopPhase.Failed));Assert.That(run.Health,Is.Zero);Assert.That(run.LastBattleWon,Is.False);
            Assert.That(run.LastHealthLoss,Is.EqualTo(25));Assert.That(run.OpenShop(),Is.False);
        }
        [Test]public void SupplyAndVictoryPreserveHealthAndFreshChallengeResetsIt()
        {
            var run=new BattleShopRun(NewShop(),44,campaign:true,journeyV2:true);AdvanceTo(run,3);
            run.Observe(BattlePhase.Lost,remainingDefense:1,initialDefense:2);run.OpenShop();run.PrepareBattle();
            Assert.That(run.Stage,Is.EqualTo(4));Assert.That(run.Health,Is.EqualTo(90));
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
        static ShopTrialModel.Role[] StartingRoles()=>new[]{
            new ShopTrialModel.Role{Id="CARD_M07",Name="士兵",Cost=1},
            new ShopTrialModel.Role{Id="CARD_M08",Name="弩手",Cost=1},
            new ShopTrialModel.Role{Id="CARD_M09",Name="术师",Cost=2},
            new ShopTrialModel.Role{Id="CARD_M04",Name="高能源石虫",Cost=2},
            new ShopTrialModel.Role{Id="CARD_M15",Name="游击队战士",Cost=1},
            new ShopTrialModel.Role{Id="CARD_M41",Name="银狼",Cost=5},
            new ShopTrialModel.Role{Id="CARD_TALULAH_TRIAL",Name="塔露拉",Cost=5}
        };
        [TestCase(44)]
        [TestCase(99)]
        [TestCase(73191)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void StartingCardsAreThreeDistinctLowCostRolesReproducibleAcrossCatalogOrder(int seed)
        {
            var roles=StartingRoles();var gifts=CampaignJourneyEconomy.StartingCards(roles,seed);
            Assert.That(gifts.Length,Is.EqualTo(3));Assert.That(gifts.Distinct().Count(),Is.EqualTo(3));
            Assert.That(gifts.All(id=>roles.Single(r=>r.Id==id).Cost<=2),Is.True);
            CollectionAssert.AreEqual(gifts,CampaignJourneyEconomy.StartingCards(roles,seed));
            CollectionAssert.AreEqual(gifts,CampaignJourneyEconomy.StartingCards(roles.Reverse(),seed));
        }
        [Test]public void StartingCardsVaryAcrossSeedsAndCanIncludeBothRecruitmentCosts()
        {
            var roles=StartingRoles();
            var samples=Enumerable.Range(1,64).Select(seed=>CampaignJourneyEconomy.StartingCards(roles,seed)).ToArray();
            Assert.That(samples.Select(cards=>string.Join(",",cards)).Distinct().Count(),Is.GreaterThan(1));
            var received=samples.SelectMany(cards=>cards).Distinct().ToArray();
            CollectionAssert.AreEquivalent(roles.Where(r=>r.Cost<=2).Select(r=>r.Id),received);
            Assert.That(received.Any(id=>roles.Single(r=>r.Id==id).Cost==1),Is.True);
            Assert.That(received.Any(id=>roles.Single(r=>r.Id==id).Cost==2),Is.True);
        }
        [Test]public void ThreeStartingCardsReserveRealStockAndRespectOneCardPopulationAfterFreeShop()
        {
            var roles=StartingRoles().Where(r=>CampaignBalance.ShopEligible(r.Id)).ToArray();
            var shop=new ShopTrialModel(roles,44);shop.EnableJourneyEconomy();
            var gifts=CampaignJourneyEconomy.StartingCards(shop.Roles,44);
            shop.Preset(gifts.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),1,0,CampaignJourneyEconomy.InitialGold);
            Assert.That(shop.SetLineup(gifts.Take(1),shop.Bench.Take(1).Select(p=>p.Instance)),Is.True);
            Assert.That(shop.Refresh(true).Success,Is.True);shop.Validate();
            Assert.That(shop.Bench.Count,Is.EqualTo(3));Assert.That(shop.Bench.All(p=>p.Star==1),Is.True);
            Assert.That(shop.Bench.Count(p=>shop.IsLineupInstance(p.Instance)),Is.EqualTo(1));
            Assert.That(shop.Bench.Count(p=>!shop.IsLineupInstance(p.Instance)),Is.EqualTo(2));
            Assert.That(shop.Gold,Is.EqualTo(5));Assert.That(shop.Level,Is.EqualTo(1));Assert.That(shop.XP,Is.Zero);
            foreach(var role in roles)
                Assert.That(shop.Available(role.Id)+shop.Offers.Count(id=>id==role.Id)+shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(shop.Capacity(role.Id)));
        }
        [Test]public void StartingCardsRejectInsufficientEligibleLowCostKinds()
        {
            Assert.Throws<InvalidOperationException>(()=>CampaignJourneyEconomy.StartingCards(StartingRoles().Take(2),44));
            Assert.Throws<InvalidOperationException>(()=>CampaignJourneyEconomy.StartingCards(StartingRoles().Where(r=>r.Cost==5),44));
        }
        [Test]public void SilverWolfRetainsIndependentCombatSupportWhileShopEligibilityExcludesHer()
        {
            Assert.That(CampaignBalance.Supports(SilverWolfCombat.CardId),Is.True);
            Assert.That(CampaignBalance.ShopEligible(SilverWolfCombat.CardId),Is.False);
            Assert.That(CampaignBalance.ShopEligible("CARD_TALULAH_TRIAL"),Is.True);
            Assert.That(CampaignBalance.ShopEligible("UNKNOWN_CARD"),Is.False);
        }
        [Test]public void OpeningRewardAndLaterSupplyDoNotCreateCombatWinStreak()
        {
            var shop=NewShop();var run=new BattleShopRun(shop,44,campaign:true,journeyV2:true);
            run.Observe(BattlePhase.Won);Assert.That(shop.Gold,Is.EqualTo(13));Assert.That(shop.WinStreak,Is.Zero);Assert.That(run.Reward.Streak,Is.Zero);
            for(int stage=2;stage<=4;stage++)
            {
                run.OpenShop();run.PrepareBattle();run.Observe(BattlePhase.Won);
                if(stage==3){Assert.That(shop.WinStreak,Is.EqualTo(2));Assert.That(run.Reward.Streak,Is.EqualTo(2));}
            }
            Assert.That(shop.WinStreak,Is.EqualTo(2));Assert.That(run.Reward.Streak,Is.Zero,"Supply neither advances nor cashes the combat streak.");
        }
        [Test]public void JourneyVariableRewardsDoubleWithoutRepeatingAuthoredBaseOrXp()
        {
            var shop=NewShop();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),1,0,50);
            var first=shop.SettleRound("first",true,baseGold:10,victoryXP:2);
            Assert.That(first.Base,Is.EqualTo(10));Assert.That(first.Interest,Is.EqualTo(10));Assert.That(first.Streak,Is.Zero);
            Assert.That(first.Experience,Is.EqualTo(2));Assert.That(shop.Gold,Is.EqualTo(70));Assert.That(shop.Level,Is.EqualTo(2));
            var second=shop.SettleRound("second",true,baseGold:10,victoryXP:2);
            Assert.That(second.Interest,Is.EqualTo(10));Assert.That(second.Streak,Is.EqualTo(2));Assert.That(second.Total,Is.EqualTo(22));
            var supply=shop.SettleRound("supply",true,baseGold:8,victoryXP:2,countsForStreak:false);
            Assert.That(supply.Base,Is.EqualTo(8));Assert.That(supply.Interest,Is.EqualTo(10));Assert.That(supply.Streak,Is.Zero);
            Assert.That(shop.WinStreak,Is.EqualTo(2));
            var defeat=shop.SettleRound("defeat",false,baseGold:10,victoryXP:2);
            Assert.That(defeat.Base,Is.EqualTo(10));Assert.That(defeat.Interest,Is.EqualTo(10));Assert.That(defeat.Streak,Is.Zero);
            Assert.That(defeat.Experience,Is.Zero);Assert.That(shop.WinStreak,Is.Zero);
            int gold=shop.Gold;Assert.That(shop.SettleRound("defeat",false,baseGold:10),Is.SameAs(defeat));Assert.That(shop.Gold,Is.EqualTo(gold));

            shop.EnableCampaign();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),1,0,50);
            var legacy=shop.SettleRound("old-first",true,baseGold:5,victoryXP:2);
            Assert.That(legacy.Base,Is.EqualTo(5));Assert.That(legacy.Interest,Is.EqualTo(5));Assert.That(legacy.Total,Is.EqualTo(10));
            Assert.That(shop.SettleRound("old-second",true).Streak,Is.EqualTo(1));
        }
        [TestCase(0,0)]
        [TestCase(9,0)]
        [TestCase(10,2)]
        [TestCase(19,2)]
        [TestCase(49,8)]
        [TestCase(50,10)]
        [TestCase(100,10)]
        public void DoubledJourneyInterestKeepsTenGoldStepsAndFiveStepCap(int opening,int expected)
        {
            var shop=NewShop();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),1,0,opening);
            var reward=shop.SettleRound("interest",false,baseGold:0,victoryXP:0);
            Assert.That(reward.Interest,Is.EqualTo(expected));Assert.That(reward.Total,Is.EqualTo(expected));
            Assert.That(shop.Gold,Is.EqualTo(opening+expected));
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
