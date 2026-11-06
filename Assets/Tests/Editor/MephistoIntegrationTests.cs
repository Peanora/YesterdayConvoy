using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class MephistoIntegrationTests
    {
        static BasicRosterProfile Profile(string id,int star=1)=>CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/"+id),star,100,10);
        static ManualBattle Mixed(int star=1,bool fast=false)
        {
            var m=Profile("M25",star);var s=m.Unit;
            if(fast)s=new ManualBattleSettings(s.CardId,s.UnitId,100,10,3,1,.1f,s.Health,.001f,1,1,1,1);
            var b=new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.right*100,m.Combat);
            b.ConfigureCardLoadout(new[]{Profile("M21"),Profile("M22"),Profile("M23"),Profile("M08"),Profile("M10")});b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string card,long req)
        {var r=b.TryDeployRole(b.RunId,req,b.Entry,card);Assert.That(r.Success);return b.FindUnit(r.EntityId);}
        [Test]public void ThreeTargetHealingUsesOwnStarProfileAndExactNativeEventOnce()
        {
            var b=Mixed(2);var healer=Deploy(b,"CARD_M25",1);var targets=new[]{"CARD_M10","CARD_M22","CARD_M23"}.Select((id,i)=>Deploy(b,id,i+2)).ToArray();
            foreach(var u in targets)b.TryDamageUnit(u.EntityId,u.MaxHealth*.9f,CombatDamageType.Arts);
            b.Advance(.05f);var cast=b.HealingCasts.Single();Assert.That(cast.ImpactAt-cast.StartedAt,Is.EqualTo(1.1).Within(.00001));
            Assert.That(cast.TargetIds.Count,Is.EqualTo(3));Assert.That(healer.MaxHealth,Is.EqualTo(1140));
            b.SetPaused(true);double clock=b.Clock;b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(cast.Resolved,Is.False);b.SetPaused(false);
            b.Advance(1.04f);Assert.That(cast.Resolved,Is.False);b.Advance(.07f);Assert.That(cast.Resolved);Assert.That(cast.AppliedTargetIds.Count,Is.EqualTo(3));
            Assert.That(cast.HealthRestored,Is.EqualTo(161.5f*3).Within(.01));float total=healer.HealingDone;b.Advance(.1f);Assert.That(healer.HealingDone,Is.EqualTo(total));
            Assert.That(b.CombatShots.All(s=>s.FromDefender||s.SourceId!=healer.EntityId));Assert.That(healer.OrdinaryAttacksFired,Is.Zero);Assert.That(b.UnitCombat(healer).HasSkill,Is.False);
        }
        [Test]public void KilledRecipientDoesNotReviveOrAppearInAppliedTargetsAndCasterDeathCancelsPending()
        {
            var b=Mixed();var healer=Deploy(b,"CARD_M25",1);var target=Deploy(b,"CARD_M08",2);b.TryDamageUnit(target.EntityId,40,CombatDamageType.Arts);b.Advance(.05f);
            var cast=b.HealingCasts.Single();b.TryDamageUnit(target.EntityId,10000,CombatDamageType.Arts);b.Advance(1.2f);
            Assert.That(cast.Resolved);Assert.That(cast.AppliedTargetIds,Is.Empty);Assert.That(cast.HealthRestored,Is.Zero);Assert.That(target.Active,Is.False);
            b.Restart();b.Begin();healer=Deploy(b,"CARD_M25",1);target=Deploy(b,"CARD_M08",2);b.TryDamageUnit(target.EntityId,40,CombatDamageType.Arts);b.Advance(.05f);
            cast=b.HealingCasts.Single();b.TryDamageUnit(healer.EntityId,10000,CombatDamageType.Arts);b.Advance(1.2f);
            Assert.That(cast.Resolved);Assert.That(cast.AppliedTargetIds,Is.Empty);Assert.That(target.HealthRecovered,Is.Zero);
            b.Restart();Assert.That(b.HealingCasts,Is.Empty);Assert.That(b.RegenerationMultiplier,Is.EqualTo(1));
        }
        [Test]public void TwoHealersDoNotMultiplyRegenerationOrReportZeroHealingAsImpact()
        {
            var b=Mixed(fast:true);Deploy(b,"CARD_M25",1);b.Advance(.11f);Deploy(b,"CARD_M25",2);var target=Deploy(b,"CARD_M08",3);
            Assert.That(b.RegenerationMultiplier,Is.EqualTo(2));b.TryDamageUnit(target.EntityId,20,CombatDamageType.Arts);b.Advance(.05f);var casts=b.HealingCasts.ToArray();Assert.That(casts.Length,Is.EqualTo(2));b.Advance(1.2f);
            Assert.That(casts.Sum(c=>c.AppliedTargetIds.Count),Is.EqualTo(1));Assert.That(casts.Sum(c=>c.HealthRestored),Is.EqualTo(20).Within(.001));
        }
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
