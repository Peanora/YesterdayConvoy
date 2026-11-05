using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class CombatFeedbackTests
    {
        static ManualBattle Create(float health=1000,MonsterEffectSettings effects=default)
        {
            var settings=new ManualBattleSettings("FEEDBACK","FEEDBACK",1000,1,200,1,.1f,health,.1f,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:1,unitArmor:0,unitRange:.1f,defenderHealth:1000,defenderAttack:1,
                defenderArmor:0,defenderRange:.1f,blockCount:0,hasSkill:false,effects:effects);
            var result=new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*1000},Vector3.right*50,combat);result.Begin();return result;
        }
        static DeployedUnitState Deploy(ManualBattle b,long request=1)
        {var receipt=b.TryDeploy(b.RunId,request,b.Entry);Assert.That(receipt.Success,Is.True);return b.FindUnit(receipt.EntityId);}
        static object Call(ManualBattle b,string method,params object[] args)
            =>typeof(ManualBattle).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,args);
        static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);

        [Test]public void OverkillRecordsActualHpAtImpactOnceAndCapturesPositionBeforeDeath()
        {
            var b=Create(30);var u=Deploy(b);Vector3 impact=u.Motion.Position;
            Assert.That(b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical),Is.True);
            var hits=b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthLost).ToArray();
            Assert.That(hits.Length,Is.EqualTo(1));Assert.That(hits[0].Amount,Is.EqualTo(30));
            Assert.That(hits[0].TargetType,Is.EqualTo(CombatFeedbackTarget.Monster));Assert.That(hits[0].WorldPosition,Is.EqualTo(impact));
            Assert.That(hits[0].RunId,Is.EqualTo(b.RunId));Assert.That(hits[0].At,Is.EqualTo(b.Clock));
            Assert.That(b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical),Is.False);Assert.That(b.CombatFeedback.Count,Is.EqualTo(1));
        }
        [Test]public void ShieldAbsorptionAndRemainingHealthLossAreSeparateAppliedAmounts()
        {
            var b=Create();var u=Deploy(b);Set(u,"SynergyShield",40f);Set(u,"SynergyShieldUntil",10d);
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);
            Assert.That(b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.ShieldAbsorbed).Sum(x=>x.Amount),Is.EqualTo(40));
            Assert.That(b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthLost).Sum(x=>x.Amount),Is.EqualTo(60));
            Assert.That(u.Health,Is.EqualTo(940));
            var arts=Create(effects:new MonsterEffectSettings(artsBarrier:true));var protectedUnit=Deploy(arts);
            Set(protectedUnit,"ArtsBarrierRemaining",40f);arts.TryDamageUnit(protectedUnit.EntityId,100,CombatDamageType.Arts);
            Assert.That(arts.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.ShieldAbsorbed).Sum(x=>x.Amount),Is.EqualTo(40));
            Assert.That(arts.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthLost).Sum(x=>x.Amount),Is.EqualTo(60),"Barrier maximum-HP normalization must not become extra damage.");
        }
        [Test]public void DefenderLossIsRecordedAtLossOnlyAndHealingClampsWithoutInventedOverheal()
        {
            var b=Create();Deploy(b);var d=b.Defender;
            Call(b,"DamageDefender",d,70f,CombatDamageType.Physical,false,null,1f);
            Assert.That(b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthLost).Count(),Is.EqualTo(1),"Do not record at both wrapper and loss.");
            Call(b,"HealDefender",d,d,10000f);Call(b,"HealDefender",d,d,10000f);
            var heals=b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthRestored).ToArray();
            Assert.That(heals.Length,Is.EqualTo(1));Assert.That(heals[0].Amount,Is.EqualTo(70));Assert.That(d.Health,Is.EqualTo(d.MaxHealth));
        }
        [Test]public void MonsterHealingUsesActualGainAndFrostNovaRevivalDoesNotBecomeHealing()
        {
            var b=Create();var u=Deploy(b);b.TryDamageUnit(u.EntityId,20,CombatDamageType.Physical);
            Assert.That((float)Call(b,"HealUnit",u,10000f),Is.EqualTo(20));Call(b,"HealUnit",u,10000f);
            Assert.That(b.CombatFeedback.Where(x=>x.Kind==CombatFeedbackKind.HealthRestored).Sum(x=>x.Amount),Is.EqualTo(20));
            var frost=Create(30,new MonsterEffectSettings(frostNova:true));var nova=Deploy(frost);
            frost.TryDamageUnit(nova.EntityId,1000,CombatDamageType.Physical);Assert.That(nova.IsReviving,Is.True);
            frost.Advance(5.1f);Assert.That(nova.IsReviving,Is.False);
            Assert.That(frost.CombatFeedback.Any(x=>x.Kind==CombatFeedbackKind.HealthRestored),Is.False,"Revival is not healing.");
        }
        [Test]public void ColdBecomesFrozenRefreshDoesNotSpamAndPauseAndRestartUseAuthoritativeState()
        {
            var b=Create();Deploy(b);b.TryApplyCold(b.Defender.EntityId,3);b.TryApplyCold(b.Defender.EntityId,3);b.TryApplyCold(b.Defender.EntityId,3);
            Assert.That(b.CombatFeedback.Count(x=>x.Status==CombatFeedbackStatus.Cold),Is.EqualTo(1));
            Assert.That(b.CombatFeedback.Count(x=>x.Status==CombatFeedbackStatus.Frozen),Is.EqualTo(1));
            Assert.That(b.CombatStatuses.Any(x=>x.Status==CombatFeedbackStatus.Frozen),Is.True);
            b.SetPaused(true);double clock=b.Clock;int events=b.CombatFeedback.Count;b.Advance(10);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.CombatFeedback.Count,Is.EqualTo(events));
            Assert.That(b.TryApplyCold(b.Defender.EntityId,10),Is.False);
            int run=b.RunId;b.Restart();Assert.That(b.RunId,Is.GreaterThan(run));Assert.That(b.CombatFeedback,Is.Empty);Assert.That(b.CombatStatuses,Is.Empty);
        }
        [Test]public void ImmuneUnitDoesNotDisplayHinderedOrFearDespiteLeftoverTimers()
        {
            var b=Create(effects:new MonsterEffectSettings(patriot:true));var u=Deploy(b);
            Set(u,"PatriotPhase",PatriotPhase.Destruction);Set(u,"HinderedUntil",10d);Set(u,"FearUntil",10d);
            b.Advance(.05f);
            Assert.That(b.UnitControlImmune(u),Is.True);
            Assert.That(b.CombatStatuses.Any(x=>x.TargetId==u.EntityId&&(x.Status==CombatFeedbackStatus.Hindered||x.Status==CombatFeedbackStatus.Fear)),Is.False);
        }
        [Test]public void JournalIsBoundedOrderedReadOnlyAndPresentationConsumesEachSequenceOnce()
        {
            var b=Create();var u=Deploy(b);
            for(int i=0;i<300;i++){b.TryDamageUnit(u.EntityId,1,CombatDamageType.Physical);Call(b,"HealUnit",u,1f);}
            Assert.That(b.CombatFeedback.Count,Is.EqualTo(256));Assert.That(b.CombatFeedback.Last().Sequence,Is.EqualTo(600));
            Assert.That(b.CombatFeedback.Select(x=>x.Sequence),Is.Ordered);var list=(IList<CombatFeedbackEvent>)b.CombatFeedback;
            Assert.That(()=>list.Clear(),Throws.TypeOf<NotSupportedException>());
            var p=new CombatFeedbackPresentation();p.Sync(b,b.Clock);float amount=p.Visible.Sum(x=>x.Amount);p.Sync(b,b.Clock);
            Assert.That(p.Visible.Sum(x=>x.Amount),Is.EqualTo(amount));Assert.That(p.Visible.Count,Is.EqualTo(2));
            p.Sync(b,b.Clock+CombatFeedbackPresentation.Lifetime+.01);Assert.That(p.Visible,Is.Empty);
            b.Restart();p.Sync(b,b.Clock);Assert.That(p.Visible,Is.Empty);Assert.That(p.ConsumedSequence,Is.Zero);
        }
        [Test]public void BurstFromManyTargetsHasNinetySixBubbleCeilingAndDoesNotMutateBattle()
        {
            var b=Create();for(int i=1;i<=100;i++){Deploy(b,i);b.Advance(.11f);}
            foreach(var u in b.Units)b.TryDamageUnit(u.EntityId,1,CombatDamageType.Physical);
            float health=b.Units.Sum(u=>u.Health);var p=new CombatFeedbackPresentation();p.Sync(b,b.Clock);
            Assert.That(p.Visible.Count,Is.EqualTo(96));Assert.That(b.Units.Sum(u=>u.Health),Is.EqualTo(health));
            b.SetPaused(true);double clock=b.Clock;b.Advance(100);p.Sync(b,b.Clock);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(p.Visible.Count,Is.EqualTo(96));
        }
    }
}
