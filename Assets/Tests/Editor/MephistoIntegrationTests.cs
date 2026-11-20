using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class MephistoIntegrationTests
    {
        [Test]public void FullBenchFiveCostMergeAndNativeRosterPolicyPreserveNineCopyStock()
        {
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/M25");Assert.DoesNotThrow(()=>new BasicRosterProfile(data.Snapshot(),data.Engagement.Snapshot()));
            Assert.That(CampaignBalance.Tags("CARD_M25"),Is.EqualTo(new[]{"牧群","整合运动"}));Assert.That(CampaignBalance.Reunion("CARD_M25"));
            var shop=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="CARD_M25",Name="梅菲斯特",Cost=5},new ShopTrialModel.Role{Id="CARD_M08",Name="弩手",Cost=1}},55);
            var full=new[]{new ShopTrialModel.Piece{Id="CARD_M25",Star=1},new ShopTrialModel.Piece{Id="CARD_M25",Star=1}}.Concat(Enumerable.Range(0,8).Select(i=>new ShopTrialModel.Piece{Id="CARD_M08",Star=1}));
            shop.Preset(full,new[]{"CARD_M25"},8,0,40);var r=shop.Purchase(0);Assert.That(r.Success);Assert.That(r.FinalStar,Is.EqualTo(2));Assert.That(shop.Bench.Count,Is.EqualTo(9));Assert.That(shop.Gold,Is.EqualTo(35));shop.Validate();
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),9,0,10000);bool offered=false;for(int i=0;i<100&&!offered;i++){shop.Refresh(true);offered=shop.Offers.Contains("CARD_M25");}Assert.That(offered);
        }
    }
}
