using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class TalulahPhaseTests
    {
        const string Card="CARD_TALULAH_TRIAL";
        static ManualBattle Battle(bool second=true,bool blast=false)
        {
            var settings=new ManualBattleSettings(Card,"TALULAH",2,1,1,2,1,600,.5f,1,1,1,1);
            var effects=new MonsterEffectSettings(blastAttack:blast?10:0,blastRadius:blast?1:0,talulahSecondPhase:second);
            var combat=new EngagementSettings(unitAttack:60,unitArmor:0,unitRange:3,unitInterval:3,
                defenderHealth:100000,defenderAttack:1,defenderArmor:10000,defenderRange:.1f,defenderInterval:100,blockCount:0,
                initialSP:12,requiredSP:12,spPerSecond:1,skillAmmo:6,skillAttack:48,skillInterval:6,skillRange:3,
                fireRain:true,effects:effects);
            var b=new ManualBattle(settings,new[]{Vector3.zero,new Vector3(100,0,0)},new Vector3(1,0,0),combat);
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success);return b;
        }
        static DeployedUnitState Unit(ManualBattle b)=>b.Units.Single();
        static void At(ManualBattle b,double clock)
        {
            Assert.That(b.Clock,Is.LessThanOrEqualTo(clock+1e-5));
            b.Advance((float)Math.Max(0,clock-b.Clock));
        }
        static void Lethal(ManualBattle b)=>Assert.That(b.TryDamageUnit(Unit(b).EntityId,100000,CombatDamageType.Physical));

        [Test] public void OnlyTheThreeStarCampaignSnapshotEnablesTheSecondPhase()
        {
            var data=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset");
            Assert.That(data,Is.Not.Null);
            for(int star=1;star<=3;star++)
                Assert.That(CampaignBalance.Build(data,star,100,6).Combat.Effects.TalulahSecondPhase,Is.EqualTo(star==3));
            var original=data.Engagement.Snapshot().Effects;
            Assert.That(original.TalulahSecondPhase,Is.False,"The existing single-phase test asset must remain unchanged.");
            var enabled=new MonsterEffectSettings(talulahSecondPhase:true);
            Assert.That(CampaignBalance.CopyEffects(enabled,2).TalulahSecondPhase);
            Assert.That(CampaignBalance.CopyEffects(enabled,talulahSecondPhase:false).TalulahSecondPhase,Is.False);
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(effects:enabled));
        }

        [Test] public void SinglePhaseDefeatStillDiesImmediatelyAndNeverStartsAutomaticRain()
        {
            var b=Battle(false);var u=Unit(b);Lethal(b);
            Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(b.TalulahAutomaticRainActive(u),Is.False);
            Assert.That(u.TalulahAutomaticWavesFired,Is.Zero);
        }

        [Test] public void FirstLethalOwnsFiveSecondsThenTenArtsWavesAndCannotSettleEarly()
        {
            var b=Battle(blast:true);var u=Unit(b);Lethal(b);
            Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.FirstFall));
            Assert.That(u.Active);Assert.That(u.Health,Is.Zero);Assert.That(b.DeathBlasts,Is.Empty);
            Assert.That(b.CheckSkill(u.EntityId),Is.EqualTo(SkillFailure.AlreadyActive));
            var p=u.Motion.Position;float sp=u.SkillSP;
            At(b,4.999);Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.FirstFall));
            At(b,5.001);Assert.That(b.TalulahAutomaticRainActive(u));
            Assert.That(u.TalulahAutomaticRainStartedAt,Is.EqualTo(5).Within(.00001));
            Assert.That(u.TalulahAutomaticWavesFired,Is.Zero);
            At(b,6.999);Assert.That(u.TalulahAutomaticWavesFired,Is.Zero);
            At(b,7.001);Assert.That(u.TalulahAutomaticWavesFired,Is.EqualTo(1));
            Assert.That(b.Defender.Health,Is.EqualTo(99940).Within(.01),"Automatic rain uses arts damage, not the target's huge physical armor.");
            At(b,25.001);Assert.That(u.TalulahAutomaticWavesFired,Is.EqualTo(10));
            At(b,32.999);Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.FirstRain));
            Assert.That(u.Motion.Position,Is.EqualTo(p));Assert.That(u.SkillSP,Is.EqualTo(sp));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(b.DeathBlasts,Is.Empty);
        }

        [Test] public void ReappearanceRestoresOneAndHalfHealthAndProtectsForFifteenSeconds()
        {
            var b=Battle();var u=Unit(b);Lethal(b);At(b,33.001);
            Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.Second));
            Assert.That(b.TalulahSecondForm(u));Assert.That(u.MaxHealth,Is.EqualTo(900));Assert.That(u.Health,Is.EqualTo(900));
            Assert.That(u.TalulahPhaseStartedAt,Is.EqualTo(33).Within(.00001));
            Assert.That(u.TalulahInvulnerableUntil,Is.EqualTo(48).Within(.00001));
            Assert.That(b.UnitAttackMultiplier(u),Is.EqualTo(1.5f));
            Assert.That(b.TalulahTransitionLocked(u));
            Assert.That(b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical),Is.False);
            At(b,35.77);Assert.That(b.TalulahTransitionLocked(u),Is.False);Assert.That(b.IsSkillReady(u.EntityId));
            Assert.That(b.UnitInvulnerable(u));
            At(b,48.001);Assert.That(b.UnitInvulnerable(u),Is.False);
        }

        [Test] public void AutomaticRainDoesNotSpendOrRegenerateManualSPAndPauseFreezesEveryPhase()
        {
            var b=Battle();var u=Unit(b);
            Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(u.SkillAmmo,Is.EqualTo(6));Lethal(b);
            Assert.That(u.SkillAmmo,Is.Zero);Assert.That(b.IsSkillActionPlaying(u),Is.False);
            Assert.That(u.SkillSP,Is.Zero);Assert.That(b.SkillCasts,Is.EqualTo(1));
            At(b,10);double clock=b.Clock;b.SetPaused(true);b.Advance(200);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.TalulahAutomaticWavesFired,Is.EqualTo(2));
            Assert.That(u.SkillSP,Is.Zero);b.SetPaused(false);At(b,33.001);
            Assert.That(b.SkillCasts,Is.EqualTo(1));Assert.That(u.SkillSP,Is.Zero);
            At(b,35.70);Assert.That(u.SkillSP,Is.Zero);
            At(b,36);Assert.That(u.SkillSP,Is.GreaterThan(0));
            At(b,48.01);Assert.That(b.IsSkillReady(u.EntityId));
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.None));
            double start=b.Clock;var p=u.Motion.Position;At(b,start+5.99);
            Assert.That(b.IsSkillActionPlaying(u));Assert.That(u.Motion.Position,Is.EqualTo(p));
            At(b,start+6.01);Assert.That(b.IsSkillActionPlaying(u),Is.False);
        }

        [Test] public void SecondLethalWaitsForFinalRainThenRunsOneFinalDeathPath()
        {
            var b=Battle(blast:true);var u=Unit(b);Lethal(b);At(b,48.01);Lethal(b);
            double began=b.Clock;
            Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.FinalFall));Assert.That(u.Active);
            Assert.That(b.DeathBlasts,Is.Empty);
            At(b,began+5.001);Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.FinalRain));
            float health=b.Defender.Health;At(b,began+7.001);
            Assert.That(health-b.Defender.Health,Is.EqualTo(90).Within(.01));
            At(b,began+32.999);Assert.That(u.Active);Assert.That(b.DeathBlasts,Is.Empty);
            At(b,began+33.001);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(u.TalulahPhase,Is.EqualTo(TalulahPhase.Dead));
            Assert.That(u.TalulahAutomaticWavesFired,Is.EqualTo(10));Assert.That(b.DeathBlasts.Count,Is.EqualTo(1));
            Assert.That(u.DeathTime,Is.EqualTo(began+33).Within(.051));
        }

        [Test] public void TransitionCannotBeHealedAndRestartMakesANewFirstPhase()
        {
            var b=Battle();var u=Unit(b);Lethal(b);
            var heal=typeof(ManualBattle).GetMethod("HealUnit",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That((float)heal.Invoke(b,new object[]{u,500f}),Is.Zero);Assert.That(u.Health,Is.Zero);
            var order=typeof(ManualBattle).GetMethod("CanReceiveOrder",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That((bool)order.Invoke(b,new object[]{u}),Is.False,"A team order cannot bypass the transition's action lock.");
            b.Restart();Assert.That(b.Units,Is.Empty);b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success);
            var fresh=Unit(b);Assert.That(fresh.TalulahPhase,Is.EqualTo(TalulahPhase.First));
            Assert.That(fresh.Health,Is.EqualTo(600));Assert.That(double.IsNegativeInfinity(fresh.TalulahAutomaticRainStartedAt));
            Assert.That(fresh.TalulahAutomaticWavesFired,Is.Zero);
        }

        [Test] public void FallingErasesOnlyUnappliedSourceShotsAndPreservesCompletedHits()
        {
            var pending=Battle();pending.Advance(.05f);
            var cancelled=pending.CombatShots.Single(s=>!s.FromDefender);
            Assert.That(cancelled.Applied,Is.False);Lethal(pending);
            Assert.That(cancelled.Resolved);Assert.That(cancelled.Erased,"A cancelled sword effect must not linger over the falling actor.");
            var completed=Battle();completed.Advance(.35f);
            var landed=completed.CombatShots.Single(s=>!s.FromDefender);
            Assert.That(landed.Applied);float health=completed.Defender.Health;Lethal(completed);
            Assert.That(landed.Erased,Is.False);Assert.That(completed.Defender.Health,Is.EqualTo(health));
        }

        [Test] public void FacingFollowsLureBeforeFearAndKeepsStoppedOrPausedFacing()
        {
            var b=Battle();var u=Unit(b);u.Motion.Advance(4);
            void Set(string name,object value)=>typeof(DeployedUnitState).GetProperty(name).SetValue(u,value);
            var visual=typeof(ManualBattle).Assembly.GetType("YesterdayConvoy.OriginalTalulahArt").GetMethod("VisualFlip",BindingFlags.Static|BindingFlags.NonPublic);
            var go=new GameObject("Talulah facing test");var camera=go.AddComponent<Camera>();
            bool Flip(bool previous)=>(bool)visual.Invoke(null,new object[]{b,u,camera,previous});
            try
            {
                Set("LureUntil",10d);Set("LureDistance",1d);Set("FearUntil",10d);
                Assert.That(Flip(false),Is.True,"Lure behind the unit moves backwards along its route.");
                Set("LureDistance",3d);Assert.That(Flip(true),Is.False,"A forward lure owns the action before fear.");
                Set("LureUntil",double.NegativeInfinity);Assert.That(Flip(false));
                Set("IsMoving",false);Assert.That(Flip(false),Is.False,"Stopped units without a target keep their last facing.");
                Set("IsMoving",true);Set("FearUntil",double.NegativeInfinity);Assert.That(Flip(true),Is.False);
                b.SetPaused(true);Assert.That(Flip(true));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test] public void SecondPhasePreparationAddsOnlyTheNineteenUsedClipsWithoutDecodingTextures()
        {
            var data=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset");
            var plan=typeof(ManualBattleController).GetMethod("Plan",BindingFlags.Static|BindingFlags.NonPublic);
            string[] Keys(bool second)
            {
                var result=(IEnumerable)plan.Invoke(null,new object[]{Array.Empty<DefenderPlacement>(),new[]{data.Card},false,second});
                return result.Cast<object>().Select(clip=>(string)clip.GetType().GetProperty("CacheKey").GetValue(clip)).ToArray();
            }
            int before=ArtMemoryCache.TextureLoads;
            var baseline=Keys(false);var phase=Keys(true);
            Assert.That(baseline.Count(key=>key.StartsWith("TalulahOriginal/",StringComparison.Ordinal)),Is.EqualTo(5));
            Assert.That(phase.Except(baseline).Count(),Is.EqualTo(19),"Shared combat effects must not inflate the second-phase increment.");
            Assert.That(phase,Does.Contain("TalulahOriginal/ExtraClips/extra_move_b"));
            Assert.That(phase,Does.Contain("TalulahUnboundVfx/ambient-B-wing"));
            Assert.That(phase.Any(key=>key.Contains("original-target-burn")||key.Contains("orb-")),Is.False);
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before),"Planning and pinning must not synchronously decode B atlases.");
            var method=typeof(YesterdayConvoy.Editor.PreparedArtTools).GetMethod("TalulahPhasePages",BindingFlags.Static|BindingFlags.NonPublic);
            var pages=(HashSet<string>)method.Invoke(null,null);
            Assert.That(pages.Count,Is.EqualTo(41));Assert.That(pages,Does.Contain("attack-B-caster-00.png"));
            Assert.That(pages.Any(page=>page.StartsWith("original-target-burn")||page.StartsWith("orb-")),Is.False);
        }
    }
}
