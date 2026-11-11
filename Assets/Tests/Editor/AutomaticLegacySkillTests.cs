using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class AutomaticLegacySkillTests
    {
        static ManualBattle Fixture(string card,out DeployedUnitState u,string skill="s3",bool automatic=true,bool device=false,bool begin=true,float flight=.1f)
        {
            bool frost=card=="CARD_M14",talulah=card=="CARD_TALULAH_TRIAL",mudrock=card=="CARD_M28",wolf=card==SilverWolfCombat.CardId;
            var effects=new MonsterEffectSettings(frostNova:frost,talulahSecondPhase:talulah,mudrock:mudrock);
            var c=new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:2.6f,unitInterval:2,defenderHealth:1000000,defenderAttack:.01f,defenderArmor:0,defenderRange:3,defenderInterval:10000,
                initialSP:wolf?28:12,requiredSP:wolf?28:12,spPerSecond:1,skillAmmo:frost||mudrock?0:wolf?3:6,skillAttack:35,skillInterval:frost?3:mudrock?1.5f:3.2f,skillRange:3,projectileTime:flight,
                attackDuration:.6f,attackWindup:.3f,effects:effects,fireRain:talulah,frostSeal:frost,mudrockCapture:mudrock);
            var settings=new ManualBattleSettings(card,"automatic-legacy",100,1,4,1,.1f,100000,.01f,1,30,1,10000);
            var guards=new[]{new DefenderSettings("guard","靶",null,new Vector3(frost?2.3f:1,0,0),1000000,.01f,0,3,10000,0,canAttack:false)};
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*1000},Vector3.right,c,roster:guards);
            b.ConfigureBossCoreViviSkill(skill);
            if(mudrock)b.ConfigureGramophones(device?new[]{new GramophoneSettings("gramophone",Vector3.forward*30,100,.1f)}:Array.Empty<GramophoneSettings>());
            if(automatic)b.EnableAutomaticSkills();u=null;
            if(begin){b.Begin();var receipt=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(receipt.Success,Is.True);u=b.FindUnit(receipt.EntityId);}
            return b;
        }

        [Test]public void ViviS3UsesRealFortyOfFiftyAndStopsSpUntilTheSixthShellActionEnds()
        {
            var b=Fixture(ViviCombat.CardId,out var u);u.NextAttackTime=10000;
            Assert.That(u.SkillSP,Is.EqualTo(40));b.Advance(9.9f);Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            float ready=b.FindUnit(u.EntityId).SkillSP;b.SetPaused(true);b.Advance(60);Assert.That(u.SkillSP,Is.EqualTo(ready));b.SetPaused(false);
            b.Advance(.2f);var cast=b.AutomaticMonsterCasts.Single(x=>x.SkillId=="s3");Assert.That(cast.At,Is.EqualTo(10).Within(.051));Assert.That(u.SkillSP,Is.Zero);
            for(int i=0;i<600&&u.SkillAmmo>0;i++)b.Advance(.05f);
            Assert.That(u.SkillAmmo,Is.Zero);Assert.That(b.CombatShots.Count(x=>!x.FromDefender&&x.Empowered),Is.EqualTo(6));Assert.That(b.IsSkillActionPlaying(u),Is.True);Assert.That(u.SkillSP,Is.Zero);
            b.Advance(.2f);Assert.That(u.SkillSP,Is.Zero,"The last launched shell still owns its entire action.");
            b.Advance((float)(u.LastAttackTime+b.UnitCombat(u).SkillInterval-b.Clock+.2));Assert.That(u.SkillSP,Is.GreaterThan(0));Assert.That(u.SkillSP,Is.LessThan(1));
        }

        [Test]public void ViviS1AutomaticallyEnhancesTheNextAttackAfterTwoActualAttackRecoveries()
        {
            var b=Fixture(ViviCombat.CardId,out var u,"s1");b.Advance(2.9f);
            Assert.That(b.BossCoreState(u).SP,Is.EqualTo(2));Assert.That(b.AutomaticMonsterCasts.Any(x=>x.SkillId=="s1"),Is.False);
            b.Advance(1.5f);var cast=b.AutomaticMonsterCasts.Single(x=>x.SkillId=="s1");Assert.That(cast.At,Is.GreaterThan(3.9));
            Assert.That(b.BossCoreState(u).SP,Is.Zero);b.Advance(1);Assert.That(b.BossRosterEvents.Any(x=>x.SkillKey=="s1"&&x.Kind=="Impact"&&x.Applied),Is.True);
        }

        [Test]public void ViviS2RemainsActiveForTwentyFiveSecondsAndAutomaticallyReachesOverload()
        {
            var b=Fixture(ViviCombat.CardId,out var u,"s2");u.NextAttackTime=10000;b.Advance(10.1f);
            var start=b.BossRosterEvents.Single(x=>x.SkillKey=="s2"&&x.Kind=="SkillStart");Assert.That(start.At,Is.EqualTo(10).Within(.051));double until=b.BossCoreState(u).ActiveUntil;
            b.Advance(13);Assert.That(b.BossCoreState(u).Phase,Is.EqualTo(2));Assert.That(b.BossCoreState(u).ActiveUntil,Is.EqualTo(until));Assert.That(b.BossRosterEvents.Any(x=>x.SkillKey=="s2"&&x.Kind=="SkillEnd"),Is.False);
            b.SetPaused(true);double clock=b.Clock;b.Advance(60);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.BossCoreState(u).ActiveUntil,Is.EqualTo(until));b.SetPaused(false);
            b.Advance(15);var end=b.BossRosterEvents.Single(x=>x.SkillKey=="s2"&&x.Kind=="SkillEnd");Assert.That(end.At-start.At,Is.GreaterThanOrEqualTo(25-.001));Assert.That(end.ActivationId,Is.EqualTo(start.ActivationId));Assert.That(end.Animation,Is.EqualTo("Skill_2_Overload_End"));Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="s2"),Is.EqualTo(1));
        }

        [Test]public void ViviAttackSpRecoversOnActualReleaseBeforeArrivalAndDoesNotRecoverTwiceOnHit()
        {
            var b=Fixture(ViviCombat.CardId,out var u,"s1",flight:3);float hp=b.Defender.Health;b.Advance(.5f);
            Assert.That(b.BossCoreState(u).SP,Is.EqualTo(1));Assert.That(b.Defender.Health,Is.EqualTo(hp),"The ordinary projectile has been released but has not reached its target.");
            b.Advance(2);Assert.That(b.BossCoreState(u).SP,Is.EqualTo(2));b.Advance(1.5f);
            Assert.That(b.Defender.Health,Is.LessThan(hp));Assert.That(b.BossCoreState(u).SP,Is.EqualTo(2));Assert.That(b.AutomaticMonsterCasts.Any(x=>x.SkillId=="s1"),Is.False);
            b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="s1"),Is.EqualTo(1));Assert.That(b.BossCoreState(u).SP,Is.Zero);
            b.Advance(1.5f);Assert.That(b.BossCoreState(u).SP,Is.Zero,"The second previously released ordinary projectile arriving during the enhanced action must not recover SP again.");
        }

        [Test]public void SourceSpIsIndependentPerEntityWaitsForTargetsAndRestartsWithoutDisablingAutomation()
        {
            var b=Fixture(ViviCombat.CardId,out var first);b.Defender.Position=Vector3.right*100;b.Advance(5);
            var second=b.FindUnit(b.TryDeploy(b.RunId,2,b.Entry).EntityId);Assert.That(first.SkillSP,Is.EqualTo(45).Within(.01));Assert.That(second.SkillSP,Is.EqualTo(40));
            b.Advance(10);Assert.That(first.SkillSP,Is.EqualTo(50).Within(.001));Assert.That(second.SkillSP,Is.EqualTo(50).Within(.001));Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.Defender.Position=Vector3.right;b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="s3"),Is.EqualTo(2));
            b.Restart();Assert.That(b.AutomaticSkillsEnabled,Is.True);Assert.That(b.AutomaticMonsterCasts,Is.Empty);b.Begin();var renewed=b.FindUnit(b.TryDeploy(b.RunId,1,b.Entry).EntityId);Assert.That(renewed.SkillSP,Is.EqualTo(40));Assert.That(renewed.SkillsUsed,Is.Zero);
        }

        [Test]public void WinterScarUsesTheNativeTenPointFiveRingAndDoesNotDoubleCastLegacyFrostSeal()
        {
            var b=Fixture("CARD_M14",out var u);u.NextAttackTime=10000;b.Advance(10.4f);Assert.That(b.IceRings,Is.Empty);b.Advance(.2f);
            Assert.That(b.IceRings.Single().Time,Is.EqualTo(10.5).Within(.051));b.Advance(25);
            Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="blackIce"),Is.EqualTo(1));Assert.That(b.FrostSeals,Is.Empty);
            var rings=b.AutomaticMonsterCasts.Where(x=>x.SkillId=="iceRing").ToArray();Assert.That(rings.Length,Is.EqualTo(3));Assert.That(rings[1].At-rings[0].At,Is.EqualTo(10.5).Within(.051));Assert.That(rings[2].At-rings[1].At,Is.EqualTo(10.5).Within(.051));
            b.TryDamageUnit(u.EntityId,u.MaxHealth*2,CombatDamageType.Physical);double revived=u.ReviveAt;b.Advance(5.1f);Assert.That(b.BossCoreState(u).ProtectedUntil,Is.EqualTo(revived+20));Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="coldWave"),Is.EqualTo(1));Assert.That(u.NextIceRingAt,Is.EqualTo(revived+10.5).Within(.051));
        }

        [Test]public void TalulahKeepsOnlySourcePeriodicActionsAndDefeatTriggeredStageRain()
        {
            var b=Fixture("CARD_TALULAH_TRIAL",out var u);u.NextAttackTime=10000;b.Advance(40);
            Assert.That(b.AutomaticMonsterCasts.Any(x=>x.SkillId=="ignite"),Is.True);Assert.That(b.AutomaticMonsterCasts.Any(x=>x.SkillId=="detonate"),Is.True);Assert.That(b.CombatShots.Any(x=>x.FireRain),Is.False);Assert.That(b.AutomaticMonsterCasts.Any(x=>x.SkillId=="firstRain"),Is.False);
            b.TryDamageUnit(u.EntityId,u.MaxHealth*2,CombatDamageType.Physical);b.Advance(5.2f);
            Assert.That(b.TalulahAutomaticRainActive(u),Is.True);Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="firstRain"),Is.EqualTo(1));Assert.That(u.SkillAmmo,Is.Zero);
            b.Advance(28);Assert.That(b.TalulahSecondForm(u),Is.True);Assert.That(b.AutomaticMonsterCasts.Count(x=>x.SkillId=="firstRain"),Is.EqualTo(1));Assert.That(b.CombatShots.Any(x=>x.FireRain),Is.False);
        }

        [Test]public void MudrockWaitsThirtyFiveSecondsAndARealEligibleGramophoneBeforeSpending()
        {
            var b=Fixture("CARD_M28",out var u,device:true);u.NextAttackTime=10000;
            Assert.That(u.SkillSP,Is.Zero);b.Advance(34.9f);Assert.That(b.MudrockCaptures,Is.Empty);b.Advance(1.6f);
            Assert.That(b.MudrockCaptures.Count,Is.EqualTo(1));Assert.That(b.MudrockCaptures[0].StartedAt,Is.GreaterThanOrEqualTo(35-.001));Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Monsters));b.Advance(40);Assert.That(b.MudrockCaptures.Count,Is.EqualTo(1));Assert.That(u.SkillSP,Is.EqualTo(35));
            var noDevice=Fixture("CARD_M28",out var waiting);noDevice.Advance(50);Assert.That(noDevice.MudrockCaptures,Is.Empty);Assert.That(waiting.SkillSP,Is.EqualTo(35));Assert.That(waiting.SkillsUsed,Is.Zero);
        }

        [Test]public void SilverWolfUsesTheAdoptedReadySpAndWaitsForALegalTarget()
        {
            var b=Fixture(SilverWolfCombat.CardId,out var u);b.Defender.Position=Vector3.right*100;b.Advance(2);Assert.That(u.SkillSP,Is.EqualTo(28));Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.Defender.Position=Vector3.right;b.SetPaused(true);b.Advance(10);Assert.That(b.AutomaticMonsterCasts,Is.Empty);b.SetPaused(false);b.Advance(.1f);
            Assert.That(b.AutomaticMonsterCasts.Single().SkillId,Is.EqualTo("transform"));Assert.That(u.SkillSP,Is.Zero);Assert.That(SilverWolfCombat.Transforming(u,b.Clock),Is.True);Assert.That(u.SkillAmmo,Is.EqualTo(3));
        }

        [Test]public void LowLevelFixturesRetainTheirExplicitManualSkillResourcesWhenAutoIsNotEnabled()
        {
            var b=Fixture(ViviCombat.CardId,out var u,automatic:false);Assert.That(u.SkillSP,Is.EqualTo(12));int objects=b.BossRosterObjects.Count;Assert.That(u.CombatTargetId,Is.Null);Assert.That(b.AutomaticLegacySkillStatus(u),Is.Not.Null);Assert.That(b.BossRosterObjects.Count,Is.EqualTo(objects));Assert.That(u.CombatTargetId,Is.Null,"Reading automatic status must not select a combat target.");Assert.That(b.TryActivateSkill(b.RunId,8,u.EntityId),Is.EqualTo(SkillFailure.None));Assert.That(u.SkillAmmo,Is.EqualTo(6));Assert.That(b.AutomaticMonsterCasts,Is.Empty);
        }
    }
}
