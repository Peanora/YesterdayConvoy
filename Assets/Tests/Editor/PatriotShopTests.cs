using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class PatriotShopTests
    {
        static BasicRosterProfile Profile(int star)=>CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M20"),star,100,10);
        [Test]public void ExistingProfileAndFiveCostStarsKeepNativeIdentityAndMechanics()
        {
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/M20");Assert.DoesNotThrow(()=>new BasicRosterProfile(data.Snapshot(),data.Engagement.Snapshot()));
            var one=Profile(1);Assert.That(one.Unit.Health,Is.EqualTo(600));Assert.That(one.Combat.UnitAttack,Is.EqualTo(60));Assert.That(one.Combat.HasSkill,Is.False);
            Assert.That(Profile(2).Unit.Health,Is.EqualTo(1140));Assert.That(Profile(2).Combat.UnitAttack,Is.EqualTo(102));
            Assert.That(Profile(3).Unit.Health,Is.EqualTo(3000));Assert.That(Profile(3).Combat.UnitAttack,Is.EqualTo(180));
            Assert.That(CampaignBalance.Tags("CARD_M20"),Is.EqualTo(new[]{"游击队","整合运动","萨卡兹"}));Assert.That(CampaignBalance.Reunion("CARD_M20")&&CampaignBalance.Guerrilla("CARD_M20"));
        }
        [Test]public void FiveCostRefreshAndFullBenchMergeConserveNineCopiesAndChargeOnce()
        {
            var shop=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="CARD_M20",Name="爱国者",Cost=5},new ShopTrialModel.Role{Id="CARD_M08",Name="弩手",Cost=1}},73);
            var full=new[]{new ShopTrialModel.Piece{Id="CARD_M20",Star=1},new ShopTrialModel.Piece{Id="CARD_M20",Star=1}}.Concat(Enumerable.Range(0,8).Select(i=>new ShopTrialModel.Piece{Id="CARD_M08",Star=1}));
            shop.Preset(full,new[]{"CARD_M20"},8,0,40);var r=shop.Purchase(0);Assert.That(r.Success);Assert.That(r.FinalStar,Is.EqualTo(2));Assert.That(shop.Bench.Count,Is.EqualTo(9));Assert.That(shop.Gold,Is.EqualTo(35));shop.Validate();
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),9,0,10000);bool offered=false;for(int i=0;i<100&&!offered;i++){shop.Refresh(true);offered=shop.Offers.Contains("CARD_M20");}Assert.That(offered);
        }
    }
}
