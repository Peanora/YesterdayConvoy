using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class SilverAshWaveTests
    {
        static ManualBattle New(bool partner=false,int enemies=2)
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.SilverAshReign,1,1,1,10,attackMultiplier:1.7f);
            var caster=new DefenderSettings("silver","silver","D26",Vector3.zero,10000,20,0,10,10,windup:.2f,actionDuration:.5f,skill:skill);
            var near=new DefenderSettings("partner","partner","D01",Vector3.forward*.5f,10000,1,0,.01f,100,canAttack:false);
            // Keep a genuine live defender after the explicit death fixtures so
            // the battle may continue simulating the already emitted projectile.
            var keeper=new DefenderSettings("keeper","keeper","D01",Vector3.forward*50,10000,1,0,.01f,100,canAttack:false);
            var b=new ManualBattle(new ManualBattleSettings("card","unit",30,1,12,1,.001f,100000,.001f,1,1,1,1),
                new[]{Vector3.right*2.5f,Vector3.right*2.5f+Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:1,unitInterval:100,unitRange:.01f),roster:partner?new[]{caster,near,keeper}:new[]{caster,keeper});
            b.Begin();
            for(int i=0;i<enemies;i++){Assert.That(b.TryDeploy(b.RunId,i+1,b.Entry).Success,Is.True);if(i+1<enemies)b.Advance(.01f);}
            for(int i=0;i<200&&b.SilverAshWaves.Count<(partner?2:1);i++)b.Advance(.01f);
            Assert.That(b.SilverAshWaves.Count,Is.EqualTo(partner?2:1));return b;
        }
        static void To(ManualBattle battle,double at){if(at>battle.Clock)battle.Advance((float)(at-battle.Clock));}
        static CombatShot[] Hits(ManualBattle battle,int sequence)=>battle.CombatShots.Where(s=>s.SilverAshWaveSequence==sequence&&s.Applied).ToArray();
        static void Kill(ManualBattle battle,DefenderState defender)
        {
            typeof(ManualBattle).GetMethod("DamageDefenderFrom",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(battle,new object[]{defender,defender.MaxHealth*100,CombatDamageType.Arts,null,false,1f});
            Assert.That(defender.Alive,Is.False);
        }
        [Test]public void SweepSettlesDuringFlightAndEachWaveHitsEachTargetOnlyOnce()
        {
            var b=New();var wave=b.SilverAshWaves.Single();
            To(b,wave.StartedAt+.08);Assert.That(wave.Launched,Is.True);Assert.That(wave.Position.x,Is.GreaterThan(wave.Origin.x));Assert.That(Hits(b,wave.Sequence),Is.Empty);
            To(b,wave.StartedAt+.22);var early=Hits(b,wave.Sequence);
            Assert.That(b.Clock,Is.LessThan(wave.EndsAt));Assert.That(early.Length,Is.EqualTo(2),"Swept contacts settle before the .3s flight finishes.");
            To(b,wave.EndsAt+.4);var final=Hits(b,wave.Sequence);
            Assert.That(final.Length,Is.EqualTo(2));Assert.That(final.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(2));
            Assert.That(Vector3.Distance(wave.Position,wave.Origin+wave.Direction*3),Is.LessThan(.00001f),"The projectile center stops after exactly three units of travel.");
        }
        [Test]public void SelfAndPartnerKeepSeparateLaunchSnapshotsAndFixedDirections()
        {
            var b=New(true);var waves=b.SilverAshWaves.ToArray();var caster=b.Defender;var partner=b.FindDefender(caster.PartnerId);
            Assert.That(waves.Select(w=>w.OwnerId).Distinct().Count(),Is.EqualTo(2));Assert.That(waves.Select(w=>w.Sequence).Distinct().Count(),Is.EqualTo(2));
            Assert.That(waves.Single(w=>w.OwnerId==caster.EntityId).Origin,Is.EqualTo(caster.Position));Assert.That(waves.Single(w=>w.OwnerId==partner.EntityId).Origin,Is.EqualTo(partner.Position));
            var origins=waves.Select(w=>w.Origin).ToArray();var directions=waves.Select(w=>w.Direction).ToArray();
            // Explicit motion fixture: the selected target leaves the beam; a
            // second target remains on the original path. Neither beam may home.
            b.Units[0].Motion.Advance(5000);caster.Position=Vector3.left*10;partner.Position=Vector3.back*10;
            To(b,waves.Max(w=>w.EndsAt)+.05);
            for(int i=0;i<waves.Length;i++)
            {
                Assert.That(waves[i].Origin,Is.EqualTo(origins[i]));Assert.That(waves[i].Direction,Is.EqualTo(directions[i]));
                Assert.That(Vector3.Distance(waves[i].Position,origins[i]+directions[i]*3),Is.LessThan(.00001f));
                Assert.That(Hits(b,waves[i].Sequence).Select(s=>s.TargetId),Is.EquivalentTo(new[]{b.Units[1].EntityId}),"Range checks also use the captured emission origin and facing.");
            }
        }
        [Test]public void PauseFreezesPendingAndLiveTravelAndRestartClearsTheOldRun()
        {
            var b=New(true);var waves=b.SilverAshWaves.ToArray();double clock=b.Clock;
            Assert.That(waves.All(w=>!w.Launched),Is.True);b.SetPaused(true);b.Advance(10);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(waves.All(w=>!w.Launched),Is.True);
            b.SetPaused(false);To(b,waves[0].StartedAt+.08);var positions=waves.Select(w=>w.Position).ToArray();int shots=b.CombatShots.Count;
            b.SetPaused(true);clock=b.Clock;b.Advance(10);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.CombatShots.Count,Is.EqualTo(shots));
            for(int i=0;i<waves.Length;i++)Assert.That(waves[i].Position,Is.EqualTo(positions[i]));
            int run=b.RunId;b.Restart();Assert.That(b.RunId,Is.GreaterThan(run));Assert.That(b.SilverAshWaves,Is.Empty);Assert.That(b.CombatShots,Is.Empty);
        }
        [Test]public void EmittedWaveKeepsSweepingAfterItsSourceDies()
        {
            var b=New();var wave=b.SilverAshWaves.Single();To(b,wave.StartedAt+.04);Assert.That(wave.Launched,Is.True);
            Kill(b,b.Defender);To(b,wave.EndsAt+.05);
            Assert.That(wave.Cancelled,Is.False);Assert.That(Vector3.Distance(wave.Position,wave.Origin+wave.Direction*3),Is.LessThan(.00001f));
            Assert.That(Hits(b,wave.Sequence).Length,Is.EqualTo(2));Assert.That(Hits(b,wave.Sequence).All(s=>s.PersistAfterSourceDeath),Is.True);
        }
        [TestCase(false)][TestCase(true)]public void UnreleasedWaveCancelsWhenItsCasterOrItsOwnerDies(bool killPartner)
        {
            var b=New(true);var caster=b.Defender;var partner=b.FindDefender(caster.PartnerId);var waves=b.SilverAshWaves.ToArray();
            var affected=waves.Where(w=>!killPartner||w.OwnerId==partner.EntityId).ToArray();Assert.That(affected.All(w=>!w.Launched),Is.True);
            Kill(b,killPartner?partner:caster);To(b,waves.Max(w=>w.EndsAt)+.05);
            foreach(var wave in affected){Assert.That(wave.Cancelled,Is.True);Assert.That(Hits(b,wave.Sequence),Is.Empty);}
            if(killPartner)Assert.That(Hits(b,waves.Single(w=>w.OwnerId==caster.EntityId).Sequence).Length,Is.EqualTo(2));
        }
    }
}
