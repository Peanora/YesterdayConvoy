using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class PatriotShopTests
    {
        static BasicRosterProfile Profile(int star)=>CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M20"),star,100,10);
        static ManualBattle Mixed(int star=2,bool far=false)
        {
            var basic=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M08"),1,100,10);
            var p=Profile(star);var g=far?new Vector3(50,0,50):Vector3.right*.4f;
            var guards=new[]{new DefenderSettings("ground","ground","D01",g,100000,1,0,.01f,1,2),new DefenderSettings("high","high","D01",Vector3.right*5,100000,1,0,.01f,1,0)};
            var b=new ManualBattle(basic.Unit,new[]{Vector3.zero,Vector3.forward*100},g,basic.Combat,roster:guards);b.ConfigureCardLoadout(new[]{p});b.ConfigureDefenderHighGround("high",true);b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,long req=1)=>b.FindUnit(b.TryDeployRole(b.RunId,req,b.Entry,"CARD_M20").EntityId);
        [Test]public void ExistingProfileAndFiveCostStarsKeepNativeIdentityAndMechanics()
        {
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/M20");Assert.DoesNotThrow(()=>new BasicRosterProfile(data.Snapshot(),data.Engagement.Snapshot()));
            var one=Profile(1);Assert.That(one.Unit.Health,Is.EqualTo(600));Assert.That(one.Combat.UnitAttack,Is.EqualTo(60));Assert.That(one.Combat.HasSkill,Is.False);
            Assert.That(Profile(2).Unit.Health,Is.EqualTo(1140));Assert.That(Profile(2).Combat.UnitAttack,Is.EqualTo(102));
            Assert.That(Profile(3).Unit.Health,Is.EqualTo(3000));Assert.That(Profile(3).Combat.UnitAttack,Is.EqualTo(180));
            Assert.That(CampaignBalance.Tags("CARD_M20"),Is.EqualTo(new[]{"游击队","整合运动","萨卡兹"}));Assert.That(CampaignBalance.Reunion("CARD_M20")&&CampaignBalance.Guerrilla("CARD_M20"));
        }
        [Test]public void SecondaryRosterPatriotUsesOwnDefenseAndFourDistinctContacts()
        {
            var b=Mixed();var u=Deploy(b);Assert.That(u,Is.Not.Null);Assert.That(b.IsPatriot(u));Assert.That(u.MaxHealth,Is.EqualTo(1140));
            Assert.That(b.UnitArmor(u),Is.EqualTo(27));Assert.That(b.UnitResistance(u),Is.EqualTo(90));b.Advance(1.1f);
            var shots=b.CombatShots.Where(s=>s.SourceId==u.EntityId).ToArray();Assert.That(shots.Length,Is.EqualTo(4));Assert.That(shots.All(s=>s.Applied));Assert.That(shots.Select(s=>s.ImpactAt).Distinct().Count(),Is.EqualTo(4));
            Assert.That(shots.Sum(s=>s.DamageDealt),Is.EqualTo(102*1.2f*1.5f*4).Within(.05));
            var ally=b.FindUnit(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M08").EntityId);Assert.That(b.IsPatriot(ally),Is.False);
        }
        [Test]public void MixedThreeStarRebirthRestoresOwnMaximumAndSchedulesOriginalHighGroundSpear()
        {
            var b=Mixed(3,true);var u=Deploy(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));b.Advance(59.9f);Assert.That(u.Health,Is.Zero);
            b.Advance(.2f);Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Destruction));Assert.That(u.Health,Is.EqualTo(3000));Assert.That(b.UnitInvulnerable(u));
            b.Advance(15.2f);Assert.That(u.Spear,Is.Not.Null);Assert.That(u.Spear.TargetId,Is.EqualTo("high"));b.Advance(2);Assert.That(u.Spear.Shot.Applied);Assert.That(u.Spear.Shot.DamageDealt,Is.GreaterThan(0));
            b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
        }
        [Test]public void TwoPurchasedPatriotsDoNotStackCommandAndRestartClearsPresence()
        {
            var b=Mixed(far:true);var ally=b.FindUnit(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M08").EntityId);Deploy(b,2);Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1.2f));b.Advance(8.1f);Assert.That(Deploy(b,3),Is.Not.Null);
            Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1.2f));Assert.That(b.TacticalArmorBonus,Is.EqualTo(2));Assert.That(b.HasTacticalCommand);b.Restart();Assert.That(b.PatriotPresent,Is.False);
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
