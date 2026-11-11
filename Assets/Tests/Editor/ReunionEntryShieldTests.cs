using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ReunionEntryShieldTests
    {
        static readonly string[] Members={"CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M02","CARD_M11","CARD_M15","CARD_M21"};
        const BindingFlags State=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,1000,.1f,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:1,unitArmor:0,unitRange:.1f,unitInterval:100,
            defenderHealth:1000000,defenderAttack:10,defenderRange:6,defenderInterval:100,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
            projectileTime:.2f,hasSkill:false,canAttack:false);
        static ManualBattle Make(out DeployedUnitState elite,bool near=false,bool incomingShot=false)
        {
            Vector3 position=near?Vector3.right*.5f:incomingShot?Vector3.right*4:new Vector3(1000,0,50);
            var guard=new DefenderSettings("guard","Guard",null,position,1000000,10,0,6,100,canAttack:incomingShot);
            var b=new ManualBattle(Unit(Members[0]),new[]{Vector3.zero,Vector3.right*10000},position,Combat(),roster:new[]{guard});
            b.ConfigureCardLoadout(Members.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(),true)));
            b.ConfigureSynergies(Members.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}));
            Assert.That(b.ReunionTier,Is.EqualTo(8));
            b.Begin();b.SetPaused(true);
            int request=0;
            foreach(string id in new[]{"CARD_M07","CARD_M11","CARD_M21"})
                Assert.That(b.TryDeployRole(b.RunId,++request,b.Entry,id,true).Success,Is.True);
            elite=b.Units.FirstOrDefault(u=>u.ReunionEliteSupport);
            Assert.That(elite,Is.Not.Null,"A real mixed-formation muster must create the elite support.");
            Assert.That(elite.ReunionEntryShieldPending,Is.True);
            Assert.That(elite.SynergyShield,Is.Zero);
            b.SetPaused(false);return b;
        }
        static void Set(object value,string property,object next)=>value.GetType().GetProperty(property,State).SetValue(value,next);
        static object Call(ManualBattle b,string name,params object[] args)
        {
            var method=typeof(ManualBattle).GetMethods(State).SingleOrDefault(m=>m.Name==name&&m.GetParameters().Length>=args.Length&&
                m.GetParameters().Skip(args.Length).All(p=>p.IsOptional)&&m.GetParameters().Take(args.Length).Select((p,i)=>p.ParameterType.IsInstanceOfType(args[i])).All(valid=>valid));
            Assert.That(method,Is.Not.Null,name);
            return method.Invoke(b,args.Concat(method.GetParameters().Skip(args.Length).Select(p=>p.DefaultValue)).ToArray());
        }
        static bool Hit(ManualBattle b,DeployedUnitState u,float attack,CombatDamageType type)
            =>(bool)Call(b,"TryDefenderDamageUnit",u,attack,type,0f,false);
        static float EntryShield(DeployedUnitState u)=>Mathf.Floor(u.MaxHealth*.2f+.5f);

        [TestCase(CombatDamageType.Physical)]
        [TestCase(CombatDamageType.Arts)]
        public void FirstDefenderHitActivatesAndConsumesTheShieldBeforeHealthLoss(CombatDamageType type)
        {
            var b=Make(out var u);float hp=u.Health,shield=EntryShield(u);
            float attack=shield+7+(type==CombatDamageType.Physical?b.UnitArmor(u):0);
            Assert.That(Hit(b,u,attack,type),Is.True);
            Assert.That(u.ReunionEntryShieldPending,Is.False);Assert.That(u.SynergyShield,Is.Zero);
            Assert.That(u.Health,Is.EqualTo(hp-7).Within(.001f));Assert.That(u.SynergyShieldUntil,Is.EqualTo(b.Clock+5).Within(.001));
            Assert.That(b.CombatFeedback.Where(e=>e.Kind==CombatFeedbackKind.ShieldAbsorbed&&e.TargetId==u.EntityId).Sum(e=>e.Amount),Is.EqualTo(shield).Within(.001f));
        }

        [Test]public void FirstDefenderTrueHitAlsoConsumesTheNewShieldBeforeHealthLoss()
        {
            var b=Make(out var u);float hp=u.Health,shield=EntryShield(u);
            Assert.That((bool)Call(b,"ApplyFifthTrueDamage",u,shield+7),Is.True);
            Assert.That(u.ReunionEntryShieldPending,Is.False);Assert.That(u.SynergyShield,Is.Zero);
            Assert.That(u.Health,Is.EqualTo(hp-7).Within(.001f));
            Assert.That(b.CombatFeedback.Where(e=>e.Kind==CombatFeedbackKind.ShieldAbsorbed&&e.TargetId==u.EntityId).Sum(e=>e.Amount),Is.EqualTo(shield).Within(.001f));
        }

        [Test]public void DefenderTrueDamageKeepsBypassingOtherSynergiesOrdinaryShield()
        {
            var b=Make(out _);var normal=b.Units.First(u=>!u.SynergySupport);
            Set(normal,"SynergyShield",100f);Set(normal,"SynergyShieldUntil",100d);float hp=normal.Health;
            Assert.That((bool)Call(b,"ApplyFifthTrueDamage",normal,7f),Is.True);
            Assert.That(normal.Health,Is.EqualTo(hp-7));Assert.That(normal.SynergyShield,Is.EqualTo(100));
        }

        [Test]public void InvulnerableTargetsRejectEveryIncomingTypeWithoutSpendingTheFirstShield()
        {
            var b=Make(out var u);Set(u,"MandraInvulnerableUntil",100d);float hp=u.Health;
            foreach(var type in new[]{CombatDamageType.Physical,CombatDamageType.Arts})Assert.That(Hit(b,u,100,type),Is.False);
            Assert.That((bool)Call(b,"ApplyFifthTrueDamage",u,100f),Is.False);
            Assert.That(u.ReunionEntryShieldPending,Is.True);Assert.That(u.SynergyShield,Is.Zero);Assert.That(u.Health,Is.EqualTo(hp));
            Set(u,"MandraInvulnerableUntil",double.NegativeInfinity);
            Assert.That(Hit(b,u,1,CombatDamageType.Arts),Is.True);
            Assert.That(u.ReunionEntryShieldPending,Is.False);Assert.That(u.SynergyShield,Is.EqualTo(EntryShield(u)-1));
        }

        [Test]public void WalkingAndPausedIncomingHitsDoNotStartTheFiveSecondWindow()
        {
            var b=Make(out var u);Vector3 start=u.Motion.Position;b.Advance(10);
            Assert.That(u.Motion.Position,Is.Not.EqualTo(start));Assert.That(u.ReunionEntryShieldPending,Is.True);Assert.That(u.SynergyShield,Is.Zero);
            b.SetPaused(true);double clock=b.Clock;float hp=u.Health;b.Advance(100);
            Assert.That(Hit(b,u,100,CombatDamageType.Arts),Is.False);
            Assert.That((bool)Call(b,"ApplyFifthTrueDamage",u,100f),Is.False);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.Health,Is.EqualTo(hp));Assert.That(u.ReunionEntryShieldPending,Is.True);
        }

        [Test]public void RealFirstAttackStartsTheShieldAndPauseFreezesItsWindow()
        {
            var b=Make(out var u,near:true);b.Advance(.01f);
            Assert.That(u.OrdinaryAttacksFired,Is.EqualTo(1));Assert.That(b.CombatShots.Any(s=>s.SourceId==u.EntityId&&!s.FromDefender),Is.True);
            Assert.That(u.ReunionEntryShieldPending,Is.False);Assert.That(u.SynergyShield,Is.EqualTo(EntryShield(u)));
            Assert.That(u.SynergyShieldUntil,Is.EqualTo(u.LastAttackTime+5).Within(.001));
            b.SetPaused(true);double clock=b.Clock,until=u.SynergyShieldUntil;b.Advance(100);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.SynergyShieldUntil,Is.EqualTo(until));Assert.That(u.SynergyShield,Is.EqualTo(EntryShield(u)));
        }

        [Test]public void NaturalDefenderProjectileUsesTheFirstShieldAtItsImpact()
        {
            var b=Make(out var u,incomingShot:true);Set(b.Defender,"TargetId",u.EntityId);float hp=u.Health;
            b.Advance(.01f);Assert.That(u.ReunionEntryShieldPending,Is.True,"Scheduling a distant incoming shot is not its impact.");
            b.Advance(.3f);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.TargetId==u.EntityId&&s.Applied),Is.True);
            Assert.That(u.ReunionEntryShieldPending,Is.False);Assert.That(u.Health,Is.EqualTo(hp));
            Assert.That(u.SynergyShield,Is.LessThan(EntryShield(u)));Assert.That(u.SynergyShield,Is.GreaterThan(0));
        }

        [Test]public void FacilitiesAndSelfLossNeverStartTheFirstEngagementShield()
        {
            var b=Make(out var u);float hp=u.Health;
            Call(b,"TrueDamage",u,1f);
            Assert.That(b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts),Is.True);
            Call(b,"ApplyUnitHealthDamage",u,1f);
            Assert.That(u.Health,Is.EqualTo(hp-3));Assert.That(u.ReunionEntryShieldPending,Is.True);Assert.That(u.SynergyShield,Is.Zero);
        }

        [Test]public void FacilityTrueDamageConsumesAnAlreadyActiveEliteShieldButStillBypassesOtherSynergyShields()
        {
            var b=Make(out var elite);Assert.That(Hit(b,elite,1,CombatDamageType.Arts),Is.True);
            float hp=elite.Health,shield=elite.SynergyShield;double until=elite.SynergyShieldUntil;
            Call(b,"TrueDamage",elite,shield+7);
            Assert.That(elite.Health,Is.EqualTo(hp-7));Assert.That(elite.SynergyShield,Is.Zero);
            Assert.That(elite.ReunionEntryShieldPending,Is.False);Assert.That(elite.SynergyShieldUntil,Is.EqualTo(until));
            var normal=b.Units.First(u=>!u.SynergySupport);Set(normal,"SynergyShield",100f);Set(normal,"SynergyShieldUntil",100d);hp=normal.Health;
            Call(b,"TrueDamage",normal,7f);
            Assert.That(normal.Health,Is.EqualTo(hp-7));Assert.That(normal.SynergyShield,Is.EqualTo(100));
        }

        [Test]public void ConsumedShieldsCannotBeGrantedAgainByHitsHealingOrLaterAttacks()
        {
            var b=Make(out var u);float shield=EntryShield(u);Assert.That(Hit(b,u,shield+1,CombatDamageType.Arts),Is.True);
            double until=u.SynergyShieldUntil;Call(b,"HealUnit",u,10000f);float hp=u.Health;
            Assert.That(Hit(b,u,1,CombatDamageType.Arts),Is.True);
            Assert.That(u.Health,Is.EqualTo(hp-1));Assert.That(u.SynergyShield,Is.Zero);Assert.That(u.SynergyShieldUntil,Is.EqualTo(until));
            b.Advance(5.1f);Set(b.Defender,"Position",u.Motion.Position+Vector3.right*.25f);b.Advance(.01f);
            Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));Assert.That(u.ReunionEntryShieldPending,Is.False);
            Assert.That(u.SynergyShield,Is.Zero);Assert.That(u.SynergyShieldUntil,Is.EqualTo(until));
        }

        [Test]public void AnUnusedRemainderExpiresAndTheNextAttackCannotRestartItsWindow()
        {
            var b=Make(out var u);Assert.That(Hit(b,u,1,CombatDamageType.Arts),Is.True);
            double until=u.SynergyShieldUntil;float remainder=u.SynergyShield;b.Advance(5.1f);
            Assert.That(until,Is.LessThan(b.Clock));Assert.That(remainder,Is.GreaterThan(0));
            Set(b.Defender,"Position",u.Motion.Position+Vector3.right*.25f);b.Advance(.01f);
            Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));Assert.That(u.SynergyShieldUntil,Is.EqualTo(until));
            float hp=u.Health;Assert.That(Hit(b,u,1,CombatDamageType.Arts),Is.True);
            Assert.That(u.Health,Is.EqualTo(hp-1));Assert.That(u.ReunionEntryShieldPending,Is.False);
            Assert.That(u.SynergyShield,Is.EqualTo(remainder));Assert.That(u.SynergyShieldUntil,Is.EqualTo(until));
        }
    }
}
