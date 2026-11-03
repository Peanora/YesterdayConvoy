using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class PatriotTests
    {
        static ManualBattleSettings S(string role="M20",float speed=1)=>new ManualBattleSettings("CARD_"+role,role+"_TEST",30,1,12,1,.1f,1000,speed,1,1,1,1);
        static EngagementSettings C(bool patriot=true,int blocks=0,float armor=0,bool runner=false,bool sniper=false)=>new EngagementSettings(unitAttack:100,unitArmor:5,unitRange:.75f,unitInterval:4,
            defenderHealth:10000,defenderAttack:1,defenderArmor:armor,defenderRange:.001f,blockCount:blocks,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
            attackDuration:1.5f,attackWindup:.75f,effects:new MonsterEffectSettings(patriot:patriot,unitResistance:45,orderRunner:runner,orderSniper:sniper));
        static ManualBattle Create(Vector3? guard=null,int blocks=0,float armor=0,Vector3[] extra=null,float speed=1)=>new ManualBattle(S(speed:speed),new[]{Vector3.zero,Vector3.forward*100},guard??Vector3.right*.4f,C(blocks:blocks,armor:armor),extra);
        static DeployedUnitState Start(ManualBattle b){b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        static void Reborn(ManualBattle b,DeployedUnitState u){Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.True);Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));}
        [Test]public void MarchDefenseResistanceTauntAndFourSeparateBlockedHits()
        {
            var b=Create(blocks:1,armor:100);var u=Start(b);Assert.That(b.UnitArmor(u),Is.EqualTo(21));Assert.That(b.UnitResistance(u),Is.EqualTo(90));Assert.That(b.UnitTaunt(u),Is.EqualTo(2));
            b.Advance(1.8f);var shots=b.CombatShots.Where(s=>!s.FromDefender).ToArray();Assert.That(shots.Length,Is.EqualTo(4));Assert.That(shots.All(s=>s.Applied),Is.True);Assert.That(shots.Select(s=>s.ImpactAt).Distinct().Count(),Is.EqualTo(4));
            Assert.That(b.Defenders[0].Health,Is.EqualTo(9680).Within(.01));Assert.That(u.OrdinaryAttacksFired,Is.EqualTo(1));
        }
        [Test]public void MarchDoesNotAttackNearbyUnblockedGuard()
        {var b=Create();var u=Start(b);b.Advance(.3f);Assert.That(u.IsBlocked,Is.False);Assert.That(u.OrdinaryAttacksFired,Is.Zero);}
        [Test]public void FirstFatalDamageWaitsSixtySecondsThenRestoresHealthAndFifteenSecondProtection()
        {
            var b=Create(guard:Vector3.right*10,speed:.01f);var u=Start(b);Reborn(b,u);double distance=u.Motion.DistanceTravelled;b.Advance(59.9f);
            Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));Assert.That(u.Health,Is.Zero);Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(distance));
            Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.False);b.Advance(.2f);
            Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Destruction));Assert.That(u.Health,Is.EqualTo(1000));Assert.That(b.UnitArmor(u),Is.EqualTo(5));Assert.That(b.UnitResistance(u),Is.EqualTo(45));Assert.That(b.UnitTaunt(u),Is.Zero);
            Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.False);b.Advance(15.1f);Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.True);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
        }
        [Test]public void ImmolationUsesRealDamageThroughArmorAndCamouflage()
        {
            var b=Create(armor:9999,speed:.001f);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.right*.4f)});var u=Start(b);Reborn(b,u);
            Assert.That(u.ImmolationTicks,Is.EqualTo(1));Assert.That(b.Defenders[0].Health,Is.EqualTo(9994.79).Within(.01));b.Advance(.81f);Assert.That(u.ImmolationTicks,Is.EqualTo(2));Assert.That(u.ImmolationDamage,Is.EqualTo(10.42).Within(.02));
        }
        [Test]public void SpearWaitsFifteenSecondsSelectsFarthestAuthoredHighGroundAndActuallyHits()
        {
            var b=Create(guard:Vector3.right*2,extra:new[]{Vector3.right*5,Vector3.right*8},speed:.001f);
            Assert.That(b.ConfigureDefenderHighGround(b.Defenders[0].EntityId,true),Is.True);Assert.That(b.ConfigureDefenderHighGround(b.Defenders[1].EntityId,true),Is.True);
            var u=Start(b);Reborn(b,u);b.Advance(60.1f);b.Advance(14.7f);Assert.That(u.Spear,Is.Null);b.Advance(.4f);
            Assert.That(u.Spear,Is.Not.Null);Assert.That(u.Spear.TargetId,Is.EqualTo(b.Defenders[1].EntityId),"Farther ground guard is not a high-ground target.");Assert.That(u.Spear.Shot.Applied,Is.False);
            b.Advance(3.3f);Assert.That(u.Spear.Shot.Applied,Is.True);Assert.That(u.Spear.Shot.DamageDealt,Is.EqualTo(135).Within(.01));Assert.That(b.Defenders[2].Health,Is.EqualTo(10000));
            var first=u.Spear;b.Advance(19.5f);Assert.That(u.Spear,Is.SameAs(first));b.Advance(1);Assert.That(u.Spear,Is.Not.SameAs(first));
        }
        [Test]public void NoHighGroundDoesNotInventSpearTarget()
        {var b=Create(guard:Vector3.right*5,speed:.001f);var u=Start(b);Reborn(b,u);b.Advance(80);Assert.That(u.Spear,Is.Null);Assert.That(b.ConfigureDefenderHighGround(b.Defenders[0].EntityId,true),Is.False);}
        [Test]public void SourceAuraPersistsThroughRebirthButDoesNotApplyToOwnSecondPhaseAndDoesNotStack()
        {
            var b=Create(guard:Vector3.right*10);b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M15"),C(false))});var first=Start(b);b.Advance(.4f);var second=b.TryDeploy(b.RunId,2,b.Entry);b.Advance(.4f);
            var friend=b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M15");var ally=b.FindUnit(friend.EntityId);Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1.2f).Within(.001));Assert.That(b.UnitArmor(ally),Is.EqualTo(7));
            Reborn(b,first);Assert.That(b.HasTacticalCommand,Is.True);Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1.2f).Within(.001));Reborn(b,b.FindUnit(second.EntityId));b.Advance(60.1f);
            Assert.That(b.UnitAttackMultiplier(first),Is.EqualTo(1));Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1.2f).Within(.001));
            b.Advance(15.1f);b.TryDamageUnit(first.EntityId,100000,CombatDamageType.Arts);b.TryDamageUnit(second.EntityId,100000,CombatDamageType.Arts);
            Assert.That(b.PatriotPresent,Is.False);Assert.That(b.HasTacticalCommand,Is.False);Assert.That(b.UnitAttackMultiplier(ally),Is.EqualTo(1));
        }
        [Test]public void DestructionAddsFixedMoveSpeedAndRejectsStunAndFreeze()
        {
            var b=Create(guard:Vector3.right*50);var u=Start(b);Assert.That(b.TryDisablePatriot(u.EntityId,2),Is.True);b.Advance(1);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            Reborn(b,u);b.Advance(60.1f);Assert.That(b.UnitMoveMultiplier(u)*u.Motion.Speed,Is.EqualTo(1.1f).Within(.001));Assert.That(b.TryDisablePatriot(u.EntityId,2),Is.False);Assert.That(b.TryDisablePatriot(u.EntityId,2,true),Is.False);
        }
        [Test]public void DeathDuringSpearWindupCancelsTheActualPendingShot()
        {
            var b=Create(guard:Vector3.right*5,speed:.001f);b.ConfigureDefenderHighGround(b.Defenders[0].EntityId,true);var u=Start(b);Reborn(b,u);b.Advance(75.2f);
            Assert.That(u.Spear,Is.Not.Null);var shot=u.Spear.Shot;Assert.That(shot.Applied,Is.False);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(4);
            Assert.That(shot.Resolved,Is.True);Assert.That(shot.Applied,Is.False);Assert.That(b.Defenders[0].Health,Is.EqualTo(10000));
        }
        [Test]public void AuthoredHighGroundCannotBlockMarchingUnits()
        {
            var b=Create(blocks:1);b.ConfigureDefenderHighGround(b.Defenders[0].EntityId,true);var u=Start(b);b.Advance(.2f);Assert.That(u.IsBlocked,Is.False);Assert.That(u.OrdinaryAttacksFired,Is.Zero);
        }
        [Test]public void PatriotProvidesRunnerAndSniperCommandThroughRebirthAndRemovesItOnFinalDeath()
        {
            var b=Create(guard:new Vector3(-.4f,0,5),extra:new[]{new Vector3(.4f,0,5)});
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M15"),C(false,runner:true)),new BasicRosterProfile(S("M16"),C(false,sniper:true))});
            var source=Start(b);b.Advance(.4f);var runner=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M15");b.Advance(.4f);var sniper=b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M16");
            Assert.That(runner.Success&&sniper.Success,Is.True);var r=b.FindUnit(runner.EntityId);var n=b.FindUnit(sniper.EntityId);
            Assert.That(b.TacticalCommanders,Is.Zero);Assert.That(b.UnitMoveMultiplier(r),Is.EqualTo(1.3f).Within(.001));Assert.That(b.UnitAttackTargets(n),Is.EqualTo(2));b.Advance(6.2f);
            Assert.That(b.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==n.EntityId).GroupBy(s=>s.StartedAt).Any(g=>g.Count()==2&&g.All(s=>s.Applied)),Is.True);
            Reborn(b,source);Assert.That(b.UnitAttackTargets(n),Is.EqualTo(2));b.Advance(75.2f);b.TryDamageUnit(source.EntityId,100000,CombatDamageType.Arts);
            Assert.That(b.UnitAttackTargets(n),Is.EqualTo(1));Assert.That(b.UnitMoveMultiplier(r),Is.EqualTo(1));
        }
        [Test]public void PauseAndRestartFreezeAndClearRebirthAndAura()
        {
            var b=Create();var u=Start(b);Reborn(b,u);b.SetPaused(true);b.Advance(80);Assert.That(b.Clock,Is.Zero);Assert.That(u.ImmolationTicks,Is.EqualTo(1));b.Restart();Assert.That(b.Units,Is.Empty);Assert.That(b.PatriotPresent,Is.False);
        }
    }
}
