using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class MandragoraRosterTests
    {
        static ManualBattle Mixed(int star=2)
        {
            var basic=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M29"),1,100,10);
            var mandra=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M32"),star,100,10);
            var guard=new DefenderSettings("guard","test",null,Vector3.forward,100000,1,0,.01f,1,0);
            var b=new ManualBattle(basic.Unit,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,basic.Combat,null,new[]{guard});
            b.ConfigureCardLoadout(new[]{mandra});b.Begin();return b;
        }
        [Test]public void MixedSecondCardUsesItsOwnStarHealthAndArtsDamage()
        {
            var b=Mixed();var r=b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M32");Assert.That(r.Success);var u=b.FindUnit(r.EntityId);
            Assert.That(u.MaxHealth,Is.EqualTo(1900));b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(5.01f);
            Assert.That(u.MandraPhase,Is.EqualTo(MandragoraPhase.Second));Assert.That(u.MaxHealth,Is.EqualTo(1900));Assert.That(b.UnitAirborne(u));
            float hp=b.Defender.Health;b.Advance(1.01f);Assert.That(hp-b.Defender.Health,Is.GreaterThanOrEqualTo(b.UnitCombat(u).UnitAttack*.3f-.01f));
        }
        [Test]public void MixedPillarBreaksOnlyMandragoraShieldAndRestoresOwnMaximum()
        {
            var b=Mixed(3);b.Restart();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.left,power:10)});b.Begin();
            var u=b.FindUnit(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M32").EntityId);Assert.That(b.TryTopplePillar(0,Vector3.left*2));Assert.That(u.StoneShield,Is.False);
            b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(5.01f);Assert.That(u.MaxHealth,Is.EqualTo(5000));Assert.That(u.Health,Is.EqualTo(5000));
        }
        [Test]public void FiveCostRefreshPurchaseAndFullBenchMergeConserveNineCopies()
        {
            var shop=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="CARD_M32",Name="蔓德拉",Cost=5},new ShopTrialModel.Role{Id="CARD_M07",Name="士兵",Cost=1}},77);
            var full=new[]{new ShopTrialModel.Piece{Id="CARD_M32",Star=1},new ShopTrialModel.Piece{Id="CARD_M32",Star=1}}.Concat(Enumerable.Range(0,8).Select(i=>new ShopTrialModel.Piece{Id="CARD_M07",Star=1}));
            shop.Preset(full,new[]{"CARD_M32"},8,0,40);var result=shop.Purchase(0);Assert.That(result.Success);Assert.That(result.FinalStar,Is.EqualTo(2));Assert.That(shop.Bench.Count,Is.EqualTo(9));Assert.That(shop.Gold,Is.EqualTo(35));shop.Validate();
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),9,0,10000);bool offered=false;for(int i=0;i<100&&!offered;i++){shop.Refresh(true);offered=shop.Offers.Contains("CARD_M32");}Assert.That(offered,"Five-cost refresh odds must include Mandragora");
        }
    }
}
