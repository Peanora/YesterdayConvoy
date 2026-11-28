using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationEconomicDistributionTests
    {
        static readonly string[] foresight={"M17","M16","B09","B26","B12"};
        static readonly string[] miracle={"B01","M38","B23","B25","B13"};
        static string Card(string id)=>FormationCampaignRules.Find(id).CardId;
        [SetUp] public void Begin()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
        [TearDown] public void End()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        static ShopTrialModel Make(string[] ids,int star=1,string onlyStar=null)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u=>new ShopTrialModel.Role{Id=u.CardId,Name=u.Name,Cost=u.Cost}),17);
            shop.EnableFormationEconomy();
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=Card(id),Star=onlyStar==null||id==onlyStar?star:1}),Array.Empty<string>(),9,0,100);
            return shop;
        }
        [TestCase("远见")][TestCase("奇迹")]
        public void RealCatalogHasExactlyOneMemberPerCostAndRetainsBaseUnitCosts(string trait)
        {
            var members=FormationCampaignRules.AllUnits.Where(u=>FormationEconomyV09.Member(u.Id,trait)).OrderBy(u=>u.Cost).ToArray();
            Assert.That(members.Select(u=>u.Id),Is.EqualTo(trait=="远见"?foresight:miracle));
            Assert.That(members.Select(u=>u.Cost),Is.EqualTo(new[]{1,2,3,4,5}));
            foreach(var member in members){Assert.That(FormationCampaignRules.Find(member.Id).Styles.Contains(trait),Is.True);Assert.That(FormationEconomyV09.Member(member.CardId,trait),Is.True);}
            var shop=Make(members.Select(u=>u.Id).ToArray());var run=new FormationCampaignRun(shop,17);
            var view=FormationTraitView.For(run,shop,trait);
            Assert.That(view.Members.Select(m=>m.CardId),Is.EquivalentTo(members.Select(m=>m.CardId)));
            Assert.That(view.Members.Select(m=>m.Cost).OrderBy(c=>c),Is.EqualTo(new[]{1,2,3,4,5}));
            foreach(string removed in trait=="远见"?new[]{"B05"}:new[]{"B22","B03"})
            {Assert.That(FormationEconomyV09.Member(removed,trait),Is.False);Assert.That(FormationCampaignRules.Find(removed).Styles.Contains(trait),Is.False);}
        }
        static IEnumerable<TestCaseData> CastCases()
        {
            foreach(bool f in new[]{true,false})foreach(int cost in Enumerable.Range(1,5))foreach(int level in new[]{2,3})foreach(int star in new[]{1,3})
                yield return new TestCaseData(f,cost,level,star);
        }
        [TestCaseSource(nameof(CastCases))]
        public void CastIncomeMatchesCostTierStarAndFourCastLimit(bool f,int cost,int level,int star)
        {
            string[] roster=f?foresight:miracle;string id=roster[cost-1];
            var ids=new[]{id}.Concat(roster.Where(x=>x!=id).Take(level-1)).ToArray();
            var shop=Make(ids,star,id);var actor=shop.Bench.Single(p=>p.Id==Card(id));
            // One explicitly starred body; leave the other activation bodies one-star.
            var economy=new FormationEconomyV09(shop,17);economy.ObservePromotions();
            int before=f?economy.ForesightLayers:economy.MiracleLayers;
            Assert.That(before,Is.EqualTo(star==3?new[]{32,36,40,44,48}[cost-1]:0));
            shop.SetLineup(new[]{actor.Id},new[]{actor.Instance});
            var casts=Enumerable.Range(0,20).Select(_=>new FormationBattleModel.CastEvent{Instance=actor.Instance,UnitId=id}).ToArray();
            economy.SettleCasts("1-4",casts);
            int perCast=cost+(f?1:0)+(level==3?1:0)+(star==3?2:0);
            int expected=before+4*perCast+(star==3?6:3);
            Assert.That(f?economy.ForesightLayers:economy.MiracleLayers,Is.EqualTo(expected));
            economy.SettleCasts("1-4",casts);economy.ObservePromotions();
            Assert.That(f?economy.ForesightLayers:economy.MiracleLayers,Is.EqualTo(expected));
        }
        [Test] public void NewNonMembersDoNotActivateMiracleAndTwoNewOneStarsStillUnlockFullChance()
        {
            var shop=Make(new[]{"B01","B22","B03"});var economy=new FormationEconomyV09(shop,17);
            Assert.That(economy.Count("奇迹"),Is.EqualTo(1));Assert.That(economy.Level("奇迹"),Is.Zero);
            Assert.That(shop.Grant(Card("M38")).Success,Is.True);
            typeof(FormationEconomyV09).GetProperty("MiracleLayers").SetValue(economy,325);
            Assert.That(economy.MiracleChainReady,Is.True);Assert.That(economy.FreeChance,Is.EqualTo(1));
            shop.Sell(shop.Bench.Single(p=>p.Id==Card("M38")).Instance);
            Assert.That(economy.FreeChance,Is.Zero);Assert.That(economy.MiracleLayers,Is.EqualTo(325));
        }
        [Test] public void EachThreeStarKindPaysOnceAndLowCostPromotionDoesNotNeedFiveCost()
        {
            var shop=Make(new[]{"M17","M16"},3);var economy=new FormationEconomyV09(shop,17);
            economy.ObservePromotions();Assert.That(economy.ForesightLayers,Is.EqualTo(68));
            shop.Sell(shop.Bench.Single(p=>p.Id==Card("M17")).Instance);
            shop.Grant(Card("M17"),4);economy.ObservePromotions();Assert.That(economy.ForesightLayers,Is.EqualTo(68));
            shop.FreeRefreshes=100;for(int i=0;i<100;i++)shop.Refresh();
            Assert.That(economy.ForesightLayers,Is.EqualTo(68));shop.Validate();
        }
        [Test] public void ReserveCopyCannotExtendFieldActorsFourCastLimit()
        {
            var shop=Make(new[]{"M17","M17","M16","B09"});var economy=new FormationEconomyV09(shop,17);
            var actors=shop.Bench.Where(p=>p.Id==Card("M17")).ToArray();
            Assert.That(shop.SetLineup(new[]{actors[0].Id},new[]{actors[0].Instance}),Is.True);
            var casts=Enumerable.Range(0,20).Select(i=>new FormationBattleModel.CastEvent{Instance=actors[i%2].Instance,UnitId="M17"})
                .Concat(new[]{new FormationBattleModel.CastEvent{Instance=-1,UnitId="B12"},new FormationBattleModel.CastEvent{Instance=shop.Bench.Single(p=>p.Id==Card("B09")).Instance,UnitId="B09"}});
            economy.SettleCasts("1-4",casts);Assert.That(economy.ForesightLayers,Is.EqualTo(15));
        }
    }
}
