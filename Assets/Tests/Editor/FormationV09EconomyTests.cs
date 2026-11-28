using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationV09EconomyTests
    {
        [Serializable] sealed class Row {public string target,variant;public int level,cases,wins;public float rate,meanSpend;}
        [Serializable] sealed class Report {public string scope="Production ShopTrialModel, real finite stock and real purchases. Fixed level, 80 gold, no XP/interests/other team purchases; 1 starting copy, hero adds 1 stock-reserved gift. Recruitment probability only, NOT natural campaign win rate.";public List<Row> rows=new List<Row>();}
        [TearDown]public void Reset()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        [Test]public void FiniteRecruitmentPoliciesConserveGoldAndStockAcrossEightThousandSeeds()
        {
            var report=new Report();
            foreach(var fixture in new[]{("M11",5,3,"H11"),("M13",6,3,"H13"),("M31",7,3,"H31"),("B04",8,2,""),("WIS",9,2,"")})
            foreach(bool hero in fixture.Item4.Length>0?new[]{false,true}:new[]{false})
            {
                var row=new Row{target=fixture.Item1,level=fixture.Item2,variant=hero?"B+hero":"Baseline",cases=1000};
                FormationBalanceV09.Select(hero?FormationBalanceV09.Variant.B:FormationBalanceV09.Variant.Baseline);
                string card=FormationCampaignRules.Find(fixture.Item1).CardId;
                for(int seed=0;seed<row.cases;seed++)
                {
                    var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(unit=>new ShopTrialModel.Role{Id=unit.CardId,Name=unit.Name,Cost=unit.Cost}),seed);
                    shop.EnableFormationEconomy();var run=new FormationCampaignRun(shop,seed);
                    while(run.HasBlockingChoice)run.Choose(0);
                    shop.Preset(new[]{new ShopTrialModel.Piece{Id=card,Star=1}},Array.Empty<string>(),fixture.Item2,0,80);
                    // Keep production reward reservation/delivery and one-hero validation.
                    if(hero)Assert.That((bool)typeof(FormationCampaignRun).GetMethod("ApplyHero",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(run,new object[]{fixture.Item4}),Is.True);
                    shop.FreeRefreshes=0;Assert.That(shop.Refresh(true).Success,Is.True);
                    bool Done()=>shop.Bench.Any(piece=>piece.Id==card&&piece.Star>=fixture.Item3);
                    for(int attempt=0;attempt<50&&!Done();attempt++)
                    {
                        for(int slot=0;slot<5&&!Done();slot++)if(shop.Offers[slot]==card&&shop.CanAfford(shop.Definition(card).Cost))Assert.That(shop.Purchase(slot).Success,Is.True);
                        if(Done()||!shop.CanAfford(2))break;
                        Assert.That(shop.Refresh().Success,Is.True);
                    }
                    if(Done())row.wins++;row.meanSpend+=80-shop.Gold;
                    Assert.That(shop.Gold,Is.InRange(0,80));shop.Validate();
                }
                row.meanSpend/=row.cases;row.rate=(float)row.wins/row.cases;report.rows.Add(row);
            }
            Directory.CreateDirectory("artifacts/v09");File.WriteAllText("artifacts/v09/production-shop-comparison.json",JsonUtility.ToJson(report,true));
            Assert.That(report.rows.Count,Is.EqualTo(8));
        }
    }
}
