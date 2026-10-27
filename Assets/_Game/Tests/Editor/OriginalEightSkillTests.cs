using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class OriginalEightSkillTests
    {
        static DefenderSettings Guard(DefenderSkillSettings skill,DefenderRole role=DefenderRole.Fighter,float armor=0,float hp=100,
            float interval=1,float range=10,Vector3? position=null)
            =>new DefenderSettings("guard","guard","D01",position??Vector3.forward,hp,20,armor,range,interval,0,role,
                windup:.1f,actionDuration:.2f,healingRange:10,skill:skill);
        static ManualBattle New(DefenderSettings guard,float incoming=1,float enemyInterval=100,bool enemyCanAttack=true)
        {
            var b=new ManualBattle(new ManualBattleSettings("card","unit",30,1,10,1,.1f,100000,1,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:incoming,unitInterval:enemyInterval,unitRange:10,canAttack:enemyCanAttack),roster:new[]{guard});
            return b;
        }
        static void Begin(ManualBattle b){b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);}

        [Test]public void BeagleDefenseReducesPhysicalDamageAndExpiresWithoutMutatingBaseArmor()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.BeagleGuard,1,1,1,.8f,defenseMultiplier:1.5f);
            var b=New(Guard(skill,armor:20),incoming:50);Begin(b);b.Advance(.4f);
            Assert.That(b.DefenderArmor(b.Defender),Is.EqualTo(30));Assert.That(b.Defender.Health,Is.EqualTo(80));
            Assert.That(b.Defender.Settings.Armor,Is.EqualTo(20));b.Advance(1);Assert.That(b.DefenderArmor(b.Defender),Is.EqualTo(20));
            b.Restart();Assert.That(b.Defender.SkillCasts,Is.Zero);Assert.That(double.IsNegativeInfinity(b.Defender.SkillActionUntil),Is.True);
        }
        [Test]public void SpotOnlyHealsDuringModeAndDoesNotAttackWhileModeIsActive()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.SpotHealingMode,0,1,1,1,1.45f,healingInterval:.3f);
            var b=New(Guard(skill,DefenderRole.Guardian,interval:.6f),incoming:40);Begin(b);b.Advance(.9f);
            Assert.That(b.CombatShots.Any(s=>s.Healing),Is.False);Assert.That(b.CombatShots.Any(s=>s.FromDefender&&!s.Healing),Is.True);
            b.Advance(.8f);var d=b.Defender;Assert.That(d.SkillCasts,Is.EqualTo(1));
            Assert.That(b.CombatShots.Any(s=>s.Healing&&s.Applied&&Math.Abs(s.Attack-29)<.001),Is.True);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender&&!s.Healing&&s.StartedAt>d.SkillStartedAt),Is.False);
            b.Advance(.8f);Assert.That(b.DefenderSkillActive(d),Is.False);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender&&!s.Healing&&s.StartedAt>d.SkillActiveUntil),Is.True);
        }
        [Test]public void MelanthaGetsFiftyPercentPhysicalAttackWithoutChangingOtherGuards()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.MelanthaStrength,1,1,1,1,1.5f);
            var b=New(Guard(s));Begin(b);b.Advance(.3f);
            Assert.That(b.CombatShots.Single(x=>x.FromDefender).Attack,Is.EqualTo(30));Assert.That(b.CombatShots.Single(x=>x.FromDefender).DamageType,Is.EqualTo(CombatDamageType.Physical));
        }
        [Test]public void KroosChargesPerAttackNotTimeAndConsumesOneSkillForTwoSeparatelyArmoredHits()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.KroosDoubleShot,0,4,0,0,1.4f);
            var b=New(Guard(s));b.Begin();b.Advance(10);Assert.That(b.Defender.SkillSP,Is.Zero);
            b.TryDeploy(b.RunId,1,b.Entry);b.Advance(3.7f);Assert.That(b.Defender.SkillSP,Is.EqualTo(4));Assert.That(b.Defender.SkillCasts,Is.Zero);
            float hp=b.Units[0].Health;b.Advance(1);
            var empowered=b.CombatShots.Where(x=>x.FromDefender&&x.Empowered).ToArray();Assert.That(empowered.Length,Is.EqualTo(2));
            Assert.That(empowered.All(x=>x.Applied&&x.Attack==28),Is.True);Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.Defender.SkillSP,Is.Zero);
            Assert.That(hp-b.Units[0].Health,Is.EqualTo(52).Within(.001));
        }
        [Test]public void DeadKroosCannotFinishQueuedDoubleShotOrGainMoreSP()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.KroosDoubleShot,0,4,0,0,1.4f);
            var b=New(Guard(s,hp:2),incoming:1,enemyInterval:4);Begin(b);b.Advance(4.8f);
            Assert.That(b.Defender.Alive,Is.False);var shots=b.CombatShots.Where(x=>x.FromDefender&&x.Empowered).ToArray();
            Assert.That(shots.Length,Is.EqualTo(2));Assert.That(shots.All(x=>x.Resolved&&!x.Applied),Is.True);Assert.That(b.Defender.SkillSP,Is.Zero);
        }
        [Test]public void StewardStrongStrikeIsOneArtsHitAtOneHundredNinetyPercent()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.StewardStrike,0,4,0,0,1.9f);
            var guard=new DefenderSettings("guard","guard","D08",Vector3.forward,100,20,0,10,1,damageType:CombatDamageType.Arts,skill:s);
            var b=New(guard);Begin(b);b.Advance(4.6f);var shot=b.CombatShots.Single(x=>x.FromDefender&&x.Empowered);
            Assert.That(shot.Attack,Is.EqualTo(38));Assert.That(shot.DamageType,Is.EqualTo(CombatDamageType.Arts));Assert.That(shot.Applied,Is.True);
        }
        static BasicRosterProfile Profile(string id,float armor,int taunt=0)
        {
            return new BasicRosterProfile(new ManualBattleSettings("CARD_"+id,id+"_TEST",30,1,10,1,.1f,100000,1,1,1,1,1),
                new EngagementSettings(unitArmor:armor,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                    effects:new MonsterEffectSettings(tauntLevel:taunt)));
        }
        [TestCase(0,"CARD_M02")][TestCase(1,"CARD_M05")]
        public void StewardPrioritizesCurrentArmorButStillRespectsHigherTaunt(int taunt,string expected)
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.StewardStrike,0,4,0,0,1.9f);
            var lanes=new[]{new DeploymentLane("a","exit",new[]{Vector3.zero,Vector3.forward*30}),new DeploymentLane("b","exit",new[]{Vector3.right*3,Vector3.forward*30}),new DeploymentLane("c","exit",new[]{Vector3.left*3,Vector3.forward*30})};
            var b=new ManualBattle(new ManualBattleSettings("card","unit",30,1,10,1,.1f,100000,1,1,1,1,1),lanes[0].Points,Vector3.one,
                new EngagementSettings(),roster:new[]{Guard(s,position:Vector3.forward*2)},deploymentLanes:lanes);
            b.ConfigureRosterProfiles(new[]{Profile("M02",30),Profile("M05",10,taunt)});b.Begin();
            Assert.That(b.TryDeploy(b.RunId,1,lanes[0].Entry).Success,Is.True);b.Advance(.15f);
            Assert.That(b.TryDeployRole(b.RunId,2,lanes[1].Entry,"CARD_M02").Success,Is.True);b.Advance(.15f);
            Assert.That(b.TryDeployRole(b.RunId,3,lanes[2].Entry,"CARD_M05").Success,Is.True);b.Advance(1);
            Assert.That(b.FindUnit(b.Defender.TargetId).CardId,Is.EqualTo(expected));
        }
        [Test]public void HibiscusOnlyHealsAndUsesOneHundredFiftyPercentHealing()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.HibiscusHealing,0,.5f,1,1,1.5f);
            var b=New(Guard(s,DefenderRole.Medic),incoming:40);Begin(b);b.Advance(1.7f);
            Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.CombatShots.Any(x=>x.Healing&&x.Applied&&x.Attack==30),Is.True);
            Assert.That(b.CombatShots.Any(x=>x.FromDefender&&!x.Healing),Is.False);
        }
        [Test]public void OrchidHinderChangesActualDistanceExpiresAndFreezesOnPause()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.OrchidSwiftStrike,0,45,1,25,1.25f,1.25f);
            var b=New(Guard(s,position:Vector3.right*2,interval:100),enemyCanAttack:false);Begin(b);b.Advance(.4f);
            var u=b.Units[0];Assert.That(u.HinderedUntil,Is.GreaterThan(b.Clock));Assert.That(b.UnitMoveMultiplier(u),Is.EqualTo(.2f));
            double before=u.Motion.DistanceTravelled;b.Advance(.5f);Assert.That(u.Motion.DistanceTravelled-before,Is.EqualTo(.1).Within(.002));
            b.SetPaused(true);double clock=b.Clock;b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.UnitMoveMultiplier(u),Is.EqualTo(.2f));
            b.SetPaused(false);b.Advance(1);Assert.That(b.UnitMoveMultiplier(u),Is.EqualTo(1));b.Restart();Assert.That(b.Units,Is.Empty);
        }
        [Test]public void LavaSpeedSkillSpeedsUpRealAttacksAndAOEStillDamagesNeighbors()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.LavaChant,1,1,1,2,attackSpeedMultiplier:1.5f);
            var guard=new DefenderSettings("guard","lava","D09",Vector3.forward,100,20,0,10,1,damageType:CombatDamageType.Arts,windup:.1f,actionDuration:.2f,splashRadius:2,skill:s);
            var lanes=new[]{new DeploymentLane("a","exit",new[]{Vector3.zero,Vector3.forward*30}),new DeploymentLane("b","exit",new[]{Vector3.right*1.5f,Vector3.forward*30})};
            var b=new ManualBattle(new ManualBattleSettings("card","unit",30,1,10,1,.1f,10000,.1f,1,1,1,1),lanes[0].Points,Vector3.one,
                new EngagementSettings(unitAttack:1),roster:new[]{guard},deploymentLanes:lanes);
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,lanes[0].Entry).Success,Is.True);b.Advance(.15f);
            Assert.That(b.TryDeploy(b.RunId,2,lanes[1].Entry).Success,Is.True);b.Advance(.85f);
            Assert.That(b.Defender.LastActionRate,Is.EqualTo(1.5f));Assert.That(b.CombatShots.Count(x=>x.FromDefender),Is.EqualTo(2));Assert.That(b.Units.All(u=>u.Health<u.MaxHealth),Is.True);
        }
        [Test]public void EveryMixedRosterHasEightSkillsWhileEarlierFourProfilesStayIntact()
        {
            foreach(string resource in new[]{"DefenderTrial","MapTrials/city","MapTrials/ruins"})
            {var data=Resources.Load<DefenderTrialDefinition>(resource);Assert.That(data.Snapshot().Count(x=>x.Skill.Enabled),Is.EqualTo(8));}
            var d=Resources.Load<DefenderTrialDefinition>("DefenderTrials/D06");Assert.That(d.Snapshot()[0].Skill.Kind,Is.EqualTo(DefenderSkillKind.PlumeSwiftStrike));
            Assert.That(d.Snapshot()[0].Skill.RequiredSP,Is.EqualTo(45));
        }
    }
}
