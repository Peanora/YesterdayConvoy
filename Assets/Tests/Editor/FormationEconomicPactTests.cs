using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationEconomicPactTests
    {
        static readonly string[] keepers={"M17","M16","B05","B01","B22","M38"};
        [SetUp] public void Begin()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
        [TearDown] public void End()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        static string Card(string id)=>FormationCampaignRules.Find(id).CardId;
        internal static ShopTrialModel Make(int seed,int gold=100,int level=9,string[] units=null,string[] offers=null)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u=>new ShopTrialModel.Role{Id=u.CardId,Name=u.Name,Cost=u.Cost}),seed);
            shop.EnableFormationEconomy();
            shop.Preset((units??keepers).Select(id=>new ShopTrialModel.Piece{Id=Card(id),Star=1}),
                (offers??Array.Empty<string>()).Select(Card),level,0,gold);
            return shop;
        }
        [Test] public void ReserveCountsDistinctKindsAndBadgesDoNotCreateEconomicMembers()
        {
            var shop=Make(1,units:new[]{"M17","M17","M16"});var economy=new FormationEconomyV09(shop,1);
            Assert.That(economy.Count("远见"),Is.EqualTo(2));Assert.That(economy.Level("远见"),Is.EqualTo(2));
            Assert.That(shop.IsLineupInstance(shop.Bench[0].Instance),Is.False);
            Assert.That(FormationCampaignRun.BadgeTraits,Does.Not.Contain("远见"));
            Assert.That(FormationCampaignRules.Find("M17").Styles,Does.Contain("远见"));
        }
        [Test] public void FailedOrAutomaticRefreshAndGiftDoNotFarmLayers()
        {
            var shop=Make(1,0);var economy=new FormationEconomyV09(shop,1);
            Assert.That(shop.Refresh().Success,Is.False);Assert.That(economy.ForesightLayers,Is.Zero);
            Assert.That(shop.Refresh(true).Success,Is.True);Assert.That(economy.MiracleLayers,Is.Zero);
            Assert.That(shop.Grant(Card("M17")).Success,Is.True);Assert.That(economy.ForesightLayers,Is.Zero);
            shop.Locked=true;shop.FreeRefreshes=1;Assert.That(shop.Refresh().Success,Is.False);
            Assert.That(shop.FreeRefreshes,Is.EqualTo(1));
        }
        [Test] public void FreeManualRefreshChainsAndCrossingMilestonesPaysExactlyOnce()
        {
            var shop=Make(71);var economy=new FormationEconomyV09(shop,71);
            shop.FreeRefreshes=80;
            for(int i=0;i<75;i++)Assert.That(shop.Refresh().Success,Is.True);
            Assert.That(economy.ForesightLayers,Is.EqualTo(300));Assert.That(economy.MiracleLayers,Is.EqualTo(300));
            Assert.That(economy.EarnedGold,Is.EqualTo(120));Assert.That(shop.Gold,Is.EqualTo(220));
            int coupons=shop.FreeRefreshes;
            for(int i=0;i<20;i++)Assert.That(shop.Refresh().Success,Is.True);
            Assert.That(shop.FreeRefreshes,Is.EqualTo(coupons)); // 100% proc spends one and returns one.
            Assert.That(shop.Gold,Is.GreaterThan(220));shop.Validate();
        }
        [Test] public void DiscountedZeroPricePurchaseAtZeroGoldSellsAtBaseValueAndConservesStock()
        {
            var shop=Make(13,offers:new[]{"M17"});var economy=new FormationEconomyV09(shop,13);
            shop.FreeRefreshes=20;for(int i=0;i<20;i++)shop.Refresh();
            Assert.That(economy.ForesightLayers,Is.EqualTo(80));Assert.That(shop.Price(Card("M17")),Is.Zero);
            // Find a real stocked offer, avoiding a fixture reset that would fabricate an available card.
            for(int i=0;i<300&&!shop.Offers.Contains(Card("M17"));i++){shop.FreeRefreshes++;shop.Refresh();}
            Assert.That(shop.Offers,Does.Contain(Card("M17")));
            shop.AddGold(-shop.Gold);int available=shop.Available(Card("M17"));
            var bought=shop.Purchase(Array.IndexOf(shop.Offers.ToArray(),Card("M17")));
            Assert.That(bought.Success,Is.True);Assert.That(bought.Value,Is.Zero);
            Assert.That(shop.Quote(bought.Instance).Value,Is.EqualTo(1));
            Assert.That(shop.Sell(bought.Instance).Success,Is.True);
            Assert.That(shop.Gold,Is.GreaterThanOrEqualTo(1));
            Assert.That(shop.Available(Card("M17")),Is.EqualTo(available+1));shop.Validate();
        }
        [Test] public void PermanentDiscountSurvivesBenchingOrSellingEconomicBodiesButNoInactiveProc()
        {
            var shop=Make(9);var economy=new FormationEconomyV09(shop,9);shop.FreeRefreshes=40;
            for(int i=0;i<40;i++)shop.Refresh();
            foreach(var piece in shop.Bench.ToArray())shop.Sell(piece.Instance);
            Assert.That(shop.Price(Card("WIS")),Is.EqualTo(4));
            int layers=economy.MiracleLayers;shop.Refresh();
            Assert.That(economy.MiracleLayers,Is.EqualTo(layers));Assert.That(economy.FreeChance,Is.Zero);
        }
        [Test] public void OnlySettledNativeCastsPayOnceNoSummonsOrRefundedRetries()
        {
            var shop=Make(6);var economy=new FormationEconomyV09(shop,6);
            var a=shop.Bench.Single(p=>p.Id==Card("M17"));var b=shop.Bench.Single(p=>p.Id==Card("B01"));
            shop.SetLineup(new[]{a.Id,b.Id},new[]{a.Instance,b.Instance});
            var casts=new[]{new FormationBattleModel.CastEvent{Instance=a.Instance,UnitId="M17"},
                new FormationBattleModel.CastEvent{Instance=b.Instance,UnitId="B01"},
                new FormationBattleModel.CastEvent{Instance=-10,UnitId="M17"}};
            economy.SettleCasts("1-1",casts);economy.SettleCasts("1-1",casts);
            Assert.That(economy.ForesightLayers,Is.EqualTo(2));Assert.That(economy.MiracleLayers,Is.EqualTo(2));
            economy.SettleCasts("1-2",casts);Assert.That(economy.ForesightLayers,Is.EqualTo(4));
        }
        [Test] public void FullReserveFailedPurchaseDoesNotEarnOrSpendAndUnrelatedModesStayUnchanged()
        {
            var shop=Make(2,units:new[]{"M17","M16","B05","B01","B22","M38","M18","M28","M25","M11"},offers:new[]{"B03"});
            var economy=new FormationEconomyV09(shop,2);
            Assert.That(shop.Purchase(0).Success,Is.False);Assert.That(shop.Gold,Is.EqualTo(100));Assert.That(economy.MiracleLayers,Is.Zero);
            FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
            Assert.That(economy.Enabled,Is.False);Assert.That(FormationCampaignRules.Find("M17").Styles,Does.Not.Contain("远见"));
        }
        [Test] public void CampaignSettlementIncludesNativeSkillIncomeAndRepeatedSettlementCannotPayTwice()
        {
            var shop=Make(36);var run=new FormationCampaignRun(shop,36);
            while(run.HasBlockingChoice)Assert.That(run.Choose(0).Success,Is.True);
            var piece=shop.Bench.Single(p=>p.Id==Card("M17"));
            shop.SetLineup(new[]{piece.Id},new[]{piece.Instance});
            var casts=Enumerable.Range(0,5).Select(_=>new FormationBattleModel.CastEvent{Instance=piece.Instance,UnitId="M17"}).ToArray();
            Assert.That(run.SettleBattle(true,1,3,0,false,casts),Is.Not.Null);
            Assert.That(run.Economy.ForesightLayers,Is.EqualTo(10));Assert.That(run.Economy.EarnedGold,Is.EqualTo(2));
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var settlement=typeof(FormationCampaignRun).GetProperty("LastBattleSettlement",flags).GetValue(run);
            Assert.That((int)settlement.GetType().GetProperty("BonusGold",flags).GetValue(settlement),Is.GreaterThanOrEqualTo(2));
            int gold=shop.Gold;Assert.That(run.SettleBattle(true,1,3,0,false,casts),Is.Null);Assert.That(shop.Gold,Is.EqualTo(gold));
            var next=new FormationCampaignRun(shop,36);
            Assert.That(next.Economy.ForesightLayers,Is.Zero);Assert.That(shop.Price(Card("WIS")),Is.EqualTo(5));
        }
        [Serializable] sealed class Row {public string variant;public int cases,wins,totalRefreshes,earnedGold;public float meanRefreshes,meanEndingGold;}
        [Serializable] sealed class Report {public string scope="Real finite ShopTrialModel transactions. Fixed level 9; 100 liquid gold plus six 1-star economic bodies and one 1-star WIS; up to 1000 manual refreshes. Buy/sell non-target non-keeper offers, buy WIS to 3-star. Baseline has identical starting resources/policy without pact effects. Not a natural-campaign acquisition/win rate; XP, setup cost/time and equipment excluded.";public List<Row> rows=new List<Row>();}
        [Test] public void ActualFiniteShopComparisonTargetsNineFiveCostCopies()
        {
            var report=new Report();
            foreach(bool enabled in new[]{false,true})
            {
                FormationBalanceV09.Select(enabled?FormationBalanceV09.Variant.B:FormationBalanceV09.Variant.Baseline);
                var row=new Row{variant=enabled?"远见+奇迹":"原经济",cases=200};
                for(int seed=0;seed<row.cases;seed++)
                {
                    var shop=Make(seed,units:keepers.Concat(new[]{"WIS"}).ToArray());var economy=new FormationEconomyV09(shop,seed);
                    shop.Refresh(true);
                    for(int step=0;step<1000;step++)
                    {
                        for(int i=0;i<5;i++)
                        {
                            string id=shop.Offers[i];if(id==null||keepers.Select(Card).Contains(id))continue;
                            var buy=shop.Purchase(i);if(!buy.Success)continue;
                            if(id!=Card("WIS"))Assert.That(shop.Sell(buy.Instance).Success,Is.True);
                        }
                        if(shop.Bench.Any(p=>p.Id==Card("WIS")&&p.Star==3)){row.wins++;break;}
                        if(!shop.Refresh().Success)break;row.totalRefreshes++;
                    }
                    row.earnedGold+=economy.EarnedGold;row.meanEndingGold+=shop.Gold;shop.Validate();
                }
                row.meanRefreshes=(float)row.totalRefreshes/row.cases;row.meanEndingGold/=row.cases;report.rows.Add(row);
            }
            Directory.CreateDirectory("artifacts/economy-v09");File.WriteAllText("artifacts/economy-v09/five-cost-comparison.json",JsonUtility.ToJson(report,true));
            Assert.That(report.rows[1].wins,Is.GreaterThan(report.rows[0].wins));
            Assert.That(report.rows[1].wins,Is.GreaterThanOrEqualTo(180));
        }
    }
}
