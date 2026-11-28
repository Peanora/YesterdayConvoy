using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class FormationEconomicPactR2RegressionTests
    {
        static readonly string[] keepers={"M17","M16","B05","B01","B22","M38"};
        [SetUp] public void Begin()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
        [TearDown] public void End()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        static FormationEconomyV09 Previous(ShopTrialModel shop,int seed)=>(FormationEconomyV09)Activator.CreateInstance(typeof(FormationEconomyV09),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{shop,seed,true},null);
        static string Card(string id)=>FormationCampaignRules.Find(id).CardId;
        static ShopTrialModel Make(int seed,int gold=100,int star=1,string[] units=null,string[] offers=null)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u=>new ShopTrialModel.Role{Id=u.CardId,Name=u.Name,Cost=u.Cost}),seed);
            shop.EnableFormationEconomy();
            shop.Preset((units??keepers).Select(id=>new ShopTrialModel.Piece{Id=Card(id),Star=id=="WIS"?1:star}),(offers??Array.Empty<string>()).Select(Card),9,0,gold);
            return shop;
        }
        static void Layers(FormationEconomyV09 e,int f,int m)
        {
            // Explicit pre-earned-history fixture, not a natural earning result.
            typeof(FormationEconomyV09).GetProperty("ForesightLayers").GetSetMethod(true).Invoke(e,new object[]{f});
            typeof(FormationEconomyV09).GetProperty("MiracleLayers").GetSetMethod(true).Invoke(e,new object[]{m});
        }
        [Test] public void FiveHundredRefreshesCannotBuildLayersOrReturnGold()
        {
            var shop=Make(71);var e=Previous(shop,71);shop.FreeRefreshes=500;
            for(int i=0;i<500;i++)Assert.That(shop.Refresh().Success,Is.True);
            Assert.That(e.ForesightLayers,Is.Zero);Assert.That(e.MiracleLayers,Is.Zero);Assert.That(e.EarnedGold,Is.Zero);Assert.That(shop.Gold,Is.EqualTo(100));
            Assert.That(e.FreeChance,Is.EqualTo(.1f).Within(.001));shop.FreeRefreshes=0;shop.Refresh();Assert.That(e.ForesightLayers,Is.Zero);
        }
        [Test] public void FailedAutomaticRefreshAndGiftDoNotProgressPacts()
        {
            var shop=Make(1,0);var e=Previous(shop,1);Assert.That(shop.Refresh().Success,Is.False);Assert.That(shop.Refresh(true).Success,Is.True);
            Assert.That(shop.Grant(Card("M17")).Success,Is.True);Assert.That(e.ForesightLayers,Is.Zero);Assert.That(e.MiracleLayers,Is.Zero);
            shop.Locked=true;shop.FreeRefreshes=1;Assert.That(shop.Refresh().Success,Is.False);Assert.That(shop.FreeRefreshes,Is.EqualTo(1));
        }
        [Test] public void PurchaseOfSameKindOnlyStacksOncePerNodeEvenAfterReturnVisit()
        {
            var shop=Make(9,offers:new[]{"M17","M17","M17"});var e=Previous(shop,9);e.BeginNode("1-1");
            for(int i=0;i<2;i++){var buy=shop.Purchase(i);Assert.That(buy.Success,Is.True);shop.Sell(buy.Instance);}Assert.That(e.ForesightLayers,Is.EqualTo(2));
            e.BeginNode("1-2");var next=shop.Purchase(2);Assert.That(next.Success,Is.True);shop.Sell(next.Instance);Assert.That(e.ForesightLayers,Is.EqualTo(4));
            e.BeginNode("1-1");for(int i=0;i<300&&!shop.Offers.Contains(Card("M17"));i++)shop.Refresh(true);
            Assert.That(shop.Purchase(Array.IndexOf(shop.Offers.ToArray(),Card("M17"))).Success,Is.True);Assert.That(e.ForesightLayers,Is.EqualTo(4));
        }
        [Test] public void OneStarCollectorsCannotUnlockDiscountOrInfiniteChainsEvenWithLayers()
        {
            var shop=Make(3);var e=Previous(shop,3);Layers(e,1000,1000);
            Assert.That(e.Qualified("远见"),Is.Zero);Assert.That(e.Qualified("奇迹"),Is.Zero);Assert.That(shop.Price(Card("WIS")),Is.EqualTo(5));
            Assert.That(shop.Price(Card("M17")),Is.EqualTo(1));Assert.That(e.FreeChance,Is.EqualTo(.8f));
        }
        [TestCase(119,1,5)][TestCase(120,0,5)][TestCase(299,0,5)][TestCase(300,0,4)]
        public void DiscountRequiresLayersAndMaintainedTwoStarKinds(int layers,int low,int high)
        {
            var shop=Make(4,star:2);var e=Previous(shop,4);Layers(e,layers,0);Assert.That(shop.Price(Card("M17")),Is.EqualTo(low));Assert.That(shop.Price(Card("WIS")),Is.EqualTo(high));
            if(layers>=300){shop.Sell(shop.Bench.Single(p=>p.Id==Card("B05")).Instance);Assert.That(shop.Price(Card("WIS")),Is.EqualTo(5));Assert.That(shop.Price(Card("M17")),Is.Zero);
                shop.Sell(shop.Bench.Single(p=>p.Id==Card("M16")).Instance);Assert.That(shop.Price(Card("M17")),Is.EqualTo(1));Assert.That(e.ForesightLayers,Is.EqualTo(layers));}
        }
        [Test] public void MatureMiracleChainsButSellingQualifiedMembersCancelsInfinity()
        {
            var shop=Make(5,star:2);var e=Previous(shop,5);Layers(e,300,449);Assert.That(e.FreeChance,Is.LessThan(1));Layers(e,300,450);Assert.That(e.FreeChance,Is.EqualTo(1));
            shop.FreeRefreshes=1;for(int i=0;i<200;i++)Assert.That(shop.Refresh().Success,Is.True);Assert.That(shop.FreeRefreshes,Is.EqualTo(1));
            Assert.That(e.MiracleLayers,Is.EqualTo(450));Assert.That(e.ForesightLayers,Is.EqualTo(300));shop.Sell(shop.Bench.Single(p=>p.Id==Card("M38")).Instance);Assert.That(e.FreeChance,Is.EqualTo(.64f).Within(.001));
        }
        [Test] public void MatureZeroPriceArbitrageConservesStock()
        {
            var shop=Make(13,0,star:2,offers:new[]{"M03"});var e=Previous(shop,13);Layers(e,300,450);int stock=shop.Available(Card("M03"));var buy=shop.Purchase(0);
            Assert.That(buy.Success,Is.True);Assert.That(buy.Value,Is.Zero);Assert.That(shop.Quote(buy.Instance).Value,Is.EqualTo(1));shop.Sell(buy.Instance);
            Assert.That(shop.Gold,Is.EqualTo(1));Assert.That(shop.Available(Card("M03")),Is.EqualTo(stock+1));shop.Validate();
        }
        [Test] public void NativeCastsOnlyPayFirstSixPerKindAndOneSettlement()
        {
            var shop=Make(6);var e=Previous(shop,6);var a=shop.Bench.Single(p=>p.Id==Card("M17"));shop.SetLineup(new[]{a.Id},new[]{a.Instance});
            var casts=Enumerable.Range(0,100).Select(_=>new FormationBattleModel.CastEvent{Instance=a.Instance,UnitId="M17"}).Concat(new[]{new FormationBattleModel.CastEvent{Instance=-10,UnitId="B01"}}).ToArray();
            e.SettleCasts("1-1",casts);e.SettleCasts("1-1",casts);Assert.That(e.ForesightLayers,Is.EqualTo(12));Assert.That(e.MiracleLayers,Is.Zero);Assert.That(e.EarnedGold,Is.Zero);
            e.SettleCasts("1-2",casts);Assert.That(e.ForesightLayers,Is.EqualTo(24));Assert.That(e.EarnedGold,Is.EqualTo(2));
        }
        [Test] public void CampaignSettlementPaysSkillIncomeOnceAndNewRunResetsIt()
        {
            var shop=Make(36);var run=new FormationCampaignRun(shop,36);while(run.HasBlockingChoice)run.Choose(0);
            var actors=shop.Bench.Where(p=>p.Id==Card("M17")||p.Id==Card("M16")).ToArray();shop.SetLineup(actors.Select(p=>p.Id),actors.Select(p=>p.Instance));
            var casts=actors.SelectMany(p=>Enumerable.Range(0,5).Select(_=>new FormationBattleModel.CastEvent{Instance=p.Instance,UnitId=FormationCampaignRules.Find(p.Id).Id})).ToArray();
            Assert.That(run.SettleBattle(true,1,3,0,false,casts),Is.Not.Null);Assert.That(run.Economy.ForesightLayers,Is.EqualTo(26));Assert.That(run.Economy.EarnedGold,Is.EqualTo(2));int gold=shop.Gold;
            Assert.That(run.SettleBattle(true,1,3,0,false,casts),Is.Null);Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(new FormationCampaignRun(shop,36).Economy.ForesightLayers,Is.Zero);
        }
        [Test] public void NativeCastBudgetNeedsMultipleCompletedBattles()
        {
            var shop=Make(7,star:2);var e=Previous(shop,7);shop.SetLineup(shop.Bench.Select(p=>p.Id),shop.Bench.Select(p=>p.Instance));
            var casts=shop.Bench.SelectMany(p=>Enumerable.Range(0,6).Select(_=>new FormationBattleModel.CastEvent{Instance=p.Instance,UnitId=FormationCampaignRules.Find(p.Id).Id})).ToArray();
            // Arithmetic fixture; does not assert a natural team survives/casts this often.
            for(int i=1;i<=13;i++){e.SettleCasts("budget-"+i,casts);Assert.That(e.GlobalDiscountReady,Is.EqualTo(i>=9));Assert.That(e.MiracleChainReady,Is.EqualTo(i>=13));}
            Assert.That(e.ForesightLayers,Is.EqualTo(468));Assert.That(e.MiracleLayers,Is.EqualTo(468));
        }
        [Serializable] sealed class Row {public string variant;public int cases,wins,totalRefreshes,earnedGold,maxForesight,maxMiracle;public float meanRefreshes,meanEndingGold;}
        [Serializable] sealed class Report {public string scope="Production finite ShopTrialModel: fixed level 9/100 cash/one WIS/six economic bodies; cold states zero layers, mature fixture explicitly PRESETS 300/450 layers and six TWO-star bodies. Buy/sell other offers, buy WIS; stop at success/insolvency/1000 manual refreshes. Setup, XP, real-battle earning excluded. Cold vs mature has DIFFERENT prerequisites, not equal-resource or natural-campaign success rates.";public List<Row> rows=new List<Row>();}
        [Test] public void FiniteShopCannotSelfStartButPreEarnedMaturityCanStillChaseFiveCosts()
        {
            var report=new Report();
            foreach(var fixture in new[]{("一星零层",1,false),("二星零层",2,false),("二星成熟预设",2,true)})
            {
                var row=new Row{variant=fixture.Item1,cases=200};
                for(int seed=0;seed<row.cases;seed++)
                {
                    var shop=Make(seed,star:fixture.Item2,units:keepers.Concat(new[]{"WIS"}).ToArray());var e=Previous(shop,seed);if(fixture.Item3)Layers(e,300,450);shop.Refresh(true);
                    for(int step=0;step<1000;step++)
                    {
                        for(int i=0;i<5;i++){string id=shop.Offers[i];if(id==null||keepers.Select(Card).Contains(id))continue;var buy=shop.Purchase(i);if(!buy.Success)continue;if(id!=Card("WIS"))Assert.That(shop.Sell(buy.Instance).Success,Is.True);}
                        if(shop.Bench.Any(p=>p.Id==Card("WIS")&&p.Star==3)){row.wins++;break;}if(!shop.Refresh().Success)break;row.totalRefreshes++;
                    }
                    row.earnedGold+=e.EarnedGold;row.meanEndingGold+=shop.Gold;row.maxForesight=Math.Max(row.maxForesight,e.ForesightLayers);row.maxMiracle=Math.Max(row.maxMiracle,e.MiracleLayers);shop.Validate();
                    if(!fixture.Item3){Assert.That(e.GlobalDiscountReady,Is.False);Assert.That(e.MiracleChainReady,Is.False);}
                }
                row.meanRefreshes=(float)row.totalRefreshes/row.cases;row.meanEndingGold/=row.cases;report.rows.Add(row);
            }
            Directory.CreateDirectory("artifacts/economy-v09-r3/r2-regression");File.WriteAllText("artifacts/economy-v09-r3/r2-regression/five-cost-comparison.json",JsonUtility.ToJson(report,true));
            Assert.That(report.rows.Take(2).All(r=>r.maxForesight<120&&r.maxMiracle<450),Is.True);Assert.That(report.rows[2].wins,Is.GreaterThanOrEqualTo(180));
        }
    }
}
