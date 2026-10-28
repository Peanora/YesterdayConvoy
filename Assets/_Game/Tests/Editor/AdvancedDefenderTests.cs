using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class AdvancedDefenderTests
    {
        static DefenderSkillSettings Skill(DefenderSkillKind kind,float duration=10,float initial=1,float required=1)
            =>new DefenderSkillSettings(kind,initial,required,1,duration,kind==DefenderSkillKind.MlynarGlory?1.5f:1.8f,
                healFraction:.2f,healthMultiplier:1.8f,effectRadius:10,damageAmplification:1.4f,moveMultiplier:.4f,
                elementFraction:1,eraseChance:1,chargeSeconds:4,targets:kind==DefenderSkillKind.LapplandHavoc?3:5);
        static DefenderSettings Guard(string id,Vector3 p,DefenderSkillSettings skill=default,CombatDamageType type=CombatDamageType.Physical,bool attack=true)
            =>new DefenderSettings(id,id,"D01",p,10000,20,0,10,1,0,damageType:type,windup:.1f,actionDuration:.2f,skill:skill,canAttack:attack,kasimierz:skill.Kind==DefenderSkillKind.MlynarGlory);
        static ManualBattle New(DefenderSettings[] guards,float incoming=1,float enemyInterval=1,bool enemyAttack=true,float hp=100000,float speed=.1f)
            =>new ManualBattle(new ManualBattleSettings("card","unit",30,1,20,1,.1f,hp,speed,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:incoming,unitInterval:enemyInterval,unitRange:20,projectileTime:.2f,canAttack:enemyAttack),roster:guards);
        static void Begin(ManualBattle b){b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);}

        [Test]public void MlynarHoldsNoAttackOrBlockBeforeSkillAndChargeFreezesWithPause()
        {
            var b=New(new[]{Guard("m",Vector3.forward,Skill(DefenderSkillKind.MlynarGlory,initial:0,required:5),attack:false)});Begin(b);b.Advance(3);
            Assert.That(b.DefenderCanBlock(b.Defender),Is.False);Assert.That(b.CombatShots.Any(s=>s.FromDefender),Is.False);
            Assert.That(b.DefenderCharge(b.Defender),Is.EqualTo(.75).Within(.001));b.SetPaused(true);b.Advance(20);
            Assert.That(b.DefenderCharge(b.Defender),Is.EqualTo(.75).Within(.001));b.SetPaused(false);b.Advance(3);
            Assert.That(b.DefenderCanBlock(b.Defender),Is.True);Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.Attack>100),Is.True);
        }
        [Test]public void MlynarAttacksAtMostFiveDistinctTargetsAndResetsChargeAtEnd()
        {
            var b=New(new[]{Guard("m",Vector3.forward,Skill(DefenderSkillKind.MlynarGlory,duration:2,initial:0,required:5),attack:false)},enemyAttack:false,speed:1);Begin(b);
            for(int i=0;i<5;i++){b.Advance(.6f);Assert.That(b.TryDeploy(b.RunId,i+2,b.Entry).Success,Is.True);}
            b.Advance(3);var group=b.CombatShots.Where(s=>s.FromDefender).GroupBy(s=>s.StartedAt).First();
            Assert.That(group.Count(),Is.EqualTo(5));Assert.That(group.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(5));
            b.Advance(1.2f);Assert.That(b.DefenderSkillActive(b.Defender),Is.False);Assert.That(b.DefenderCharge(b.Defender),Is.LessThan(.4));
        }
        [Test]public void LapplandPursuesOutsideOrdinaryRangeFearsThenReturnsAndResetClearsSummons()
        {
            var d=new DefenderSettings("l","l","D21",Vector3.right*5,10000,20,0,.1f,1,skill:Skill(DefenderSkillKind.LapplandHavoc,duration:5),damageType:CombatDamageType.Arts);
            var b=New(new[]{d},enemyAttack:false);Begin(b);bool backwards=false;
            for(int i=0;i<95;i++){double distance=b.Units[0].Motion.DistanceTravelled;b.Advance(.05f);backwards|=b.Units[0].Motion.DistanceTravelled<distance;}
            Assert.That(b.Defender.FearApplications,Is.GreaterThan(0));Assert.That(backwards,Is.True);
            Assert.That(b.CombatShots.Any(s=>s.Drone&&s.Applied),Is.True);Assert.That(b.DefenderDrones.Count,Is.EqualTo(3));
            b.Advance(1);Assert.That(b.DefenderDrones,Is.Empty);b.Restart();Assert.That(b.Defender.DroneHits,Is.Zero);Assert.That(b.Units,Is.Empty);
        }
        [Test]public void LapplandRetargetsSurvivorAfterFirstTargetDiesAndDeathRemovesDrones()
        {
            var b=New(new[]{Guard("l",Vector3.forward,Skill(DefenderSkillKind.LapplandHavoc))},enemyAttack:false,speed:1);Begin(b);b.Advance(.6f);Assert.That(b.TryDeploy(b.RunId,2,b.Entry).Success,Is.True);b.Advance(2);
            var first=b.Units[0];Assert.That(b.TryDamageUnit(first.EntityId,200000,CombatDamageType.Arts),Is.True);b.Advance(.1f);
            Assert.That(b.DefenderDrones.All(d=>d.TargetId==b.Units[1].EntityId),Is.True);
            var lethal=New(new[]{new DefenderSettings("l","l","D21",Vector3.forward,2,20,0,10,1,skill:Skill(DefenderSkillKind.LapplandHavoc))},incoming:5);
            Begin(lethal);lethal.Advance(1);Assert.That(lethal.Defender.Alive,Is.False);Assert.That(lethal.DefenderDrones,Is.Empty);
        }
        [Test]public void SariaHealsAllWoundedInRangeGivesSPAndDoesNotOrdinarilyAttackDuringChannel()
        {
            var waiting=new DefenderSkillSettings(DefenderSkillKind.MidnightEnchant,0,100,1,1);
            var b=New(new[]{Guard("s",Vector3.right,Skill(DefenderSkillKind.SariaCalcification)),Guard("ally",Vector3.forward*.2f,waiting)},incoming:30,enemyInterval:.6f);
            Begin(b);b.Advance(3);
            Assert.That(b.Defender.AuraHeals,Is.GreaterThan(0));Assert.That(b.Defender.SkillHealthRestored,Is.GreaterThan(0));
            Assert.That(b.FindDefender("ally").SkillSP,Is.GreaterThan(3));
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="s"&&!s.Healing),Is.False);
            Assert.That(b.Defenders.All(d=>d.Health<=d.MaxHealth),Is.True);
        }
        [Test]public void SariaAuraAmplifiesOnlyArtsAndSlowsRealMovementThenExpires()
        {
            var b=New(new[]{Guard("s",Vector3.right,Skill(DefenderSkillKind.SariaCalcification,duration:2))},enemyAttack:false);Begin(b);b.Advance(.1f);var u=b.Units[0];
            float health=u.Health;b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);Assert.That(health-u.Health,Is.EqualTo(98).Within(.01));
            health=u.Health;b.TryDamageUnit(u.EntityId,100,CombatDamageType.Arts);Assert.That(health-u.Health,Is.EqualTo(140).Within(.01));
            double distance=u.Motion.DistanceTravelled;b.Advance(.5f);Assert.That(u.Motion.DistanceTravelled-distance,Is.EqualTo(.02).Within(.002));
            b.Advance(2);Assert.That(b.UnitArtsVulnerability(u),Is.EqualTo(1));Assert.That(b.DefenderAuraMoveMultiplier(u),Is.EqualTo(1));
        }
        [Test]public void YuHealthBuffIsTemporaryAndFourGuardRecoveryDoesNotOverheal()
        {
            var guards=new[]{Guard("y",Vector3.forward,Skill(DefenderSkillKind.YuKitchen,duration:2)),Guard("a",Vector3.right),Guard("b",Vector3.left),Guard("c",Vector3.back)};
            var b=New(guards,incoming:30,enemyInterval:.6f);Begin(b);b.Advance(1);
            Assert.That(b.Defender.MaxHealth,Is.EqualTo(18000));Assert.That(b.Defender.Settings.Health,Is.EqualTo(10000));
            Assert.That(b.Defender.SkillHealthRestored,Is.GreaterThan(0));b.Advance(1.5f);Assert.That(b.Defender.MaxHealth,Is.EqualTo(10000));
            Assert.That(b.Defenders.All(d=>d.Health<=d.MaxHealth),Is.True);b.Restart();Assert.That(b.Defender.MaxHealth,Is.EqualTo(10000));
        }
        [Test]public void YuWallBeginsAfterGestureErasesCrossingShotsAndOtherCasterBuildsElementalBurst()
        {
            var b=New(new[]{Guard("y",Vector3.right*2,Skill(DefenderSkillKind.YuKitchen)),Guard("caster",Vector3.right*3,type:CombatDamageType.Arts)},incoming:2,enemyInterval:.3f);Begin(b);b.Advance(1);
            Assert.That(b.DefenderWallActive(b.Defender),Is.False);Assert.That(b.Defender.ErasedProjectiles,Is.Zero);
            b.Advance(6);Assert.That(b.DefenderWallActive(b.Defender),Is.True);Assert.That(b.Defender.ErasedProjectiles,Is.GreaterThan(0));
            Assert.That(b.CombatShots.Where(s=>s.Erased).All(s=>s.Resolved&&!s.Applied),Is.True);
            Assert.That(b.Defender.BurnApplications,Is.GreaterThan(0));Assert.That(b.Units[0].ScorchBursts,Is.GreaterThan(0));
            Assert.That(b.UnitResistance(b.Units[0]),Is.EqualTo(-20));
        }
        [Test]public void WallCrossingUsesBothSidesAndCardinalNormal()
        {
            Assert.That(ManualBattle.CrossesDefenderWall(Vector3.left,Vector3.right,Vector3.zero,Vector3.right),Is.True);
            Assert.That(ManualBattle.CrossesDefenderWall(Vector3.left,Vector3.left*2,Vector3.zero,Vector3.right),Is.False);
            Assert.That(ManualBattle.CrossesDefenderWall(Vector3.back,Vector3.forward,Vector3.zero,Vector3.forward),Is.True);
        }
        [Test]public void MlynarOwnOrdinaryKillReducesActiveTraitBonusByTenPercentagePoints()
        {
            var b=New(new[]{Guard("m",Vector3.forward,Skill(DefenderSkillKind.MlynarGlory,initial:0,required:2),attack:false)},enemyAttack:false,hp:50);Begin(b);b.Advance(2.8f);
            Assert.That(b.Defender.LiberatorKills,Is.EqualTo(1));Assert.That(b.Units[0].Active,Is.False);
            Assert.That(b.Defender.LiberatorBonus,Is.EqualTo(1.9f).Within(.001));
        }
        [Test]public void YuOwnArtsAndOtherPhysicalHitsDoNotTriggerWallElementalDamage()
        {
            var b=New(new[]{Guard("y",Vector3.right*2,Skill(DefenderSkillKind.YuKitchen),CombatDamageType.Arts),Guard("physical",Vector3.right*3)},enemyAttack:false);Begin(b);b.Advance(5);
            Assert.That(b.DefenderWallActive(b.Defender),Is.True);Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.Applied),Is.True);
            Assert.That(b.Defender.BurnApplications,Is.Zero);Assert.That(b.Units[0].ScorchAccumulated,Is.Zero);
        }
        [Test]public void YuWallDoesNotDeleteMeleeDamage()
        {
            var g=Guard("y",Vector3.right*2,Skill(DefenderSkillKind.YuKitchen));
            var b=new ManualBattle(new ManualBattleSettings("card","unit",30,1,5,1,.1f,100000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:2,unitInterval:.3f,unitRange:20,projectileTime:.2f,melee:true),roster:new[]{g});Begin(b);b.Advance(5);
            Assert.That(b.DefenderWallActive(b.Defender),Is.True);Assert.That(b.Defender.ErasedProjectiles,Is.Zero);Assert.That(b.CombatShots.Any(s=>!s.FromDefender&&s.Applied),Is.True);
        }
        [Test]public void LapplandOverlappingDroneAreasApplyOneDamageTickRatherThanThree()
        {
            var b=New(new[]{Guard("l",Vector3.forward,Skill(DefenderSkillKind.LapplandHavoc),CombatDamageType.Arts)},enemyAttack:false);Begin(b);b.Advance(3.1f);
            float projectile=b.CombatShots.Where(s=>s.FromDefender&&s.Applied).Sum(s=>s.Attack);
            float area=b.Units[0].MaxHealth-b.Units[0].Health-projectile;
            Assert.That(b.DefenderDrones.Count,Is.EqualTo(3));Assert.That(area,Is.GreaterThan(0));Assert.That(area,Is.LessThanOrEqualTo(72.1f));
        }
        [Test]public void RetreatNeverLeavesImmutableRouteOrCreatesNegativeProgress()
        {
            var motion=new RouteMotion(new[]{Vector3.zero,Vector3.right,Vector3.right+Vector3.forward},1);motion.Advance(1.5f);motion.Retreat(1);
            Assert.That(motion.Position,Is.EqualTo(Vector3.right*.5f));motion.Retreat(10);Assert.That(motion.Position,Is.EqualTo(Vector3.zero));Assert.That(motion.DistanceTravelled,Is.Zero);
            motion.Advance(.5f);Assert.That(motion.Position,Is.EqualTo(Vector3.right*.5f));Assert.Throws<ArgumentOutOfRangeException>(()=>motion.Retreat(float.NaN));
        }
        [Test]public void FourOriginalS3ModelsAndConfigurationsAreAvailableWithoutFabricatedMlynarAttack()
        {
            foreach(string id in ManualBattleController.DefenderBatch4Ids)
            {
                var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Assert.That(data,Is.Not.Null);Assert.That(data.Snapshot()[0].Skill.Enabled,Is.True);
                Assert.That(DefenderArt.Load(id).Portrait,Is.Not.Null);Assert.That(DefenderArt.Load(id+"B")["idle"].Frames.Length,Is.GreaterThan(1));
            }
            Assert.That(DefenderArt.Load("D30").ActionDuration,Is.Zero);Assert.That(Resources.Load<DefenderTrialDefinition>("DefenderTrials/D30").Snapshot()[0].CanAttack,Is.False);
        }
    }
}
