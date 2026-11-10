using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopAndSynergyUiTests
    {
        [Test]public void OddsUseTheRealTenLevelsAndDoNotReopenLockedOrEmptyCosts()
        {
            var shop=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="one",Name="one",Cost=1},new ShopTrialModel.Role{Id="two",Name="two",Cost=2}});
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="one",Star=4},new ShopTrialModel.Piece{Id="one",Star=2}},new string[0],4,0,10);
            string before=CampaignJourneyEvidenceProbe.State(shop);
            Assert.That(ShopProbabilityUi.FirstDraw(shop,4),Is.EqualTo(new[]{0f,100f,0f,0f,0f}));
            Assert.That(ShopProbabilityUi.FirstDraw(shop,1).All(n=>n==0),Is.True,"An empty unlocked cost cannot unlock a higher cost.");
            foreach(int level in Enumerable.Range(1,10))Assert.That(ShopProbabilityUi.Baseline(level),Is.EqualTo(ShopTrialModel.Odds[level-1]));
            var copy=ShopProbabilityUi.Baseline(1);copy[0]=0;Assert.That(ShopTrialModel.Odds[0][0],Is.EqualTo(100));
            Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(before));
        }
        [Test]public void DetailShowsTheWholeRosterAndOnlyTheHighestFormationTierIsActive()
        {
            var summary=SynergySummary.From(new[]{"CARD_M07","CARD_M08","CARD_M09","CARD_M10"},campaignV2:true).Single(s=>s.Name=="整合运动");
            var model=SynergyDetailUiModel.Build(summary,true,CampaignBalance.Profiles.Keys);
            Assert.That(model.members.Count(m=>m.selected),Is.EqualTo(4));
            Assert.That(model.members.Length,Is.EqualTo(CampaignBalance.Profiles.Keys.Count(CampaignBalance.Reunion)));
            Assert.That(model.members.Any(m=>!m.selected),Is.True);
            Assert.That(model.tiers.Where(t=>t.active).Select(t=>t.tier),Is.EqualTo(new[]{4}));
            Assert.That(model.tiers.Single(t=>t.tier==2).active,Is.False);
            Assert.That(model.tiers.Single(t=>t.tier==4).text,Does.Contain("生命150/攻击24/甲2"));
        }
        [Test]public void EveryFormalSynergyHasSeparateNonemptyTierRowsAndLegacyDescriptionsRemainAvailable()
        {
            foreach(bool v2 in new[]{false,true})foreach(var summary in SynergySummary.From(CampaignBalance.Profiles.Keys,campaignV2:v2))
            {
                var model=SynergyDetailUiModel.Build(summary,v2,CampaignBalance.Profiles.Keys);
                Assert.That(model.tiers.Select(t=>t.tier),Is.EqualTo(summary.Thresholds),summary.Name);
                Assert.That(model.tiers.All(t=>!string.IsNullOrWhiteSpace(t.text)),Is.True,summary.Name);
                Assert.That(model.tiers.Count(t=>t.active),Is.EqualTo(summary.Tier>0?1:0),summary.Name);
                if(v2)Assert.That(model.tiers.All(t=>!t.text.Contains("原试验说明")),Is.True,summary.Name);
            }
        }
        [Test]public void ResourceBlockLeavesAllPreparationCardsAndTheMapAboveItClear()
        {
            var hud=ManualBattleController.ShopResourceHudRect;
            foreach(int index in Enumerable.Range(0,10))
            {Assert.That(hud.Overlaps(ManualBattleController.ShopLineupRect(index)),Is.False);Assert.That(hud.Overlaps(ManualBattleController.ShopBenchRect(index)),Is.False);}
            Assert.That(hud.yMin,Is.GreaterThanOrEqualTo(552));Assert.That(hud.yMax,Is.LessThanOrEqualTo(720));
        }
    }
}
