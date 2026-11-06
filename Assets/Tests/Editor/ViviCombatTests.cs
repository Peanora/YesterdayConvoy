using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ViviCombatTests
    {
        const string Card="CARD_WISADEL_APRIL";
        static ManualBattle Battle(string card=Card,float flight=.22f,float secondaryArmor=20,float health=1000,MonsterEffectSettings effects=default)
        {
            var settings=new ManualBattleSettings(card,"VIVI",100,1,1,2,.1f,10000,1,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:14,unitArmor:0,unitRange:5,unitInterval:1.7f,defenderRange:.01f,
                initialSP:12,requiredSP:12,spPerSecond:1,skillAmmo:6,skillAttack:105,skillInterval:3.2f,skillRange:5,projectileTime:flight,effects:effects);
            var roster=new[]{
                new DefenderSettings("primary","P",null,Vector3.right,health,1,3,.01f,100),
                new DefenderSettings("inside","I",null,new Vector3(3.1f,2,0),health,1,secondaryArmor,.01f,100),
                new DefenderSettings("outside","O",null,new Vector3(3.3f,0,0),health,1,0,.01f,100)};
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.right,combat,roster:roster);
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);return b;
        }
        static DeployedUnitState Unit(ManualBattle b)=>b.Units.Single();
        static void Cast(ManualBattle b)=>Assert.That(b.TryActivateSkill(b.RunId,1,Unit(b).EntityId),Is.EqualTo(SkillFailure.None));
        static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);
        static void Kill(ManualBattle b,DefenderState target)=>typeof(ManualBattle).GetMethod("ApplyDefenderLoss",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{target,target.Health});
        static bool Flip(ManualBattle b,Camera c,bool previous)=>(bool)typeof(ManualBattle).Assembly.GetType("YesterdayConvoy.ViviCombat")
            .GetMethod("VisualFlip",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b,Unit(b),c,previous});

        [Test]public void SkillHitsAllLivingDefendersInsideGroundRadiusWithTheirOwnArmorExactlyOnce()
        {
            var b=Battle();Cast(b);b.Advance(.35f);var shot=b.CombatShots.Single(s=>!s.FromDefender);
            Assert.That(shot.Empowered&&shot.Applied,Is.True);Assert.That(shot.SplashRadius,Is.EqualTo(2.2f));
            Assert.That(b.FindDefender("primary").Health,Is.EqualTo(898));
            Assert.That(b.FindDefender("inside").Health,Is.EqualTo(915),"Height is ignored for a ground explosion; each defender uses its own armor.");
            Assert.That(b.FindDefender("outside").Health,Is.EqualTo(1000));Assert.That(shot.DamageDealt,Is.EqualTo(187));
            var losses=b.CombatFeedback.Where(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost).ToArray();
            Assert.That(losses.Length,Is.EqualTo(2));Assert.That(losses.Sum(e=>e.Amount),Is.EqualTo(187));
            b.Advance(.2f);Assert.That(b.FindDefender("primary").Health,Is.EqualTo(898),"A resolved explosion cannot tick damage again.");
        }
        [Test]public void DeadPrimaryDoesNotCancelTheAlreadyLaunchedGroundExplosion()
        {
            var b=Battle(flight:.5f);Cast(b);b.Advance(.05f);var shot=b.CombatShots.Single(s=>!s.FromDefender);
            var center=shot.Destination;Kill(b,b.FindDefender("primary"));b.Advance(.55f);
            Assert.That(shot.Applied,Is.True);Assert.That(shot.Destination,Is.EqualTo(center));
            Assert.That(b.FindDefender("inside").Health,Is.EqualTo(915));Assert.That(b.FindDefender("outside").Health,Is.EqualTo(1000));
            Assert.That(shot.DamageDealt,Is.EqualTo(85),"The dead primary is neither revived nor hit twice.");
        }
        [Test]public void PauseFreezesShellAmmoAndHealthThenResumesOneExplosion()
        {
            var b=Battle(flight:.5f);Cast(b);b.Advance(.05f);var unit=Unit(b);var position=unit.Motion.Position;
            b.SetPaused(true);double clock=b.Clock;int ammo=unit.SkillAmmo;b.Advance(20);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(unit.SkillAmmo,Is.EqualTo(ammo));Assert.That(b.Defender.Health,Is.EqualTo(1000));
            Assert.That(unit.Motion.Position,Is.EqualTo(position));b.SetPaused(false);b.Advance(.55f);
            Assert.That(b.Defender.Health,Is.EqualTo(898));Assert.That(unit.Motion.Position,Is.EqualTo(position),"The fired skill still owns its action lock.");
        }
        [Test]public void SixShellsRetainTheirTimingDamageAndDoNotTurnOrdinaryShotsIntoSplash()
        {
            var b=Battle(health:100000);Cast(b);b.Advance(19.7f);
            var shells=b.CombatShots.Where(s=>!s.FromDefender&&s.Empowered).ToArray();Assert.That(shells.Length,Is.EqualTo(6));
            Assert.That(shells.All(s=>s.Applied&&s.SplashRadius==2.2f&&s.Attack==105),Is.True);
            for(int i=1;i<shells.Length;i++)Assert.That(shells[i].StartedAt-shells[i-1].StartedAt,Is.EqualTo(3.2).Within(.051));
            Assert.That(b.FindDefender("inside").Health,Is.EqualTo(100000-6*85));
            Assert.That(b.CombatShots.Where(s=>!s.FromDefender&&!s.Empowered).All(s=>s.SplashRadius==0),Is.True);
            Assert.That(Unit(b).SkillAmmo,Is.Zero);Assert.That(Unit(b).SkillsUsed,Is.EqualTo(1));
        }
        [Test]public void OtherAmmoSkillsKeepTheirSingleTargetRuleAndViviCannotBlastAfterDeath()
        {
            var other=Battle(card:"OTHER");Cast(other);other.Advance(.35f);
            Assert.That(other.CombatShots.Single(s=>!s.FromDefender).SplashRadius,Is.Zero);Assert.That(other.FindDefender("inside").Health,Is.EqualTo(1000));
            var b=Battle(flight:.5f);Cast(b);b.Advance(.05f);Assert.That(b.TryDamageUnit(Unit(b).EntityId,100000,CombatDamageType.Physical),Is.True);
            b.Advance(1);Assert.That(b.Defender.Health,Is.EqualTo(1000));Assert.That(b.FindDefender("inside").Health,Is.EqualTo(1000));
        }
        [Test]public void SecondaryDamageStillUsesMinimumOneAfterArmor()
        {
            var b=Battle(secondaryArmor:500);Cast(b);b.Advance(.35f);
            Assert.That(b.FindDefender("inside").Health,Is.EqualTo(999));Assert.That(b.CombatShots.Single(s=>!s.FromDefender).DamageDealt,Is.EqualTo(103));
        }
        [Test]public void FacingUsesLureBeforeFearAndKeepsPauseVerticalAndIdleOrientation()
        {
            var b=Battle();var u=Unit(b);u.Motion.Advance(4);Set(u,"IsMoving",true);
            var go=new GameObject("Vivi facing camera");var camera=go.AddComponent<Camera>();
            try
            {
                Assert.That(Flip(b,camera,true),Is.False);Set(u,"FearUntil",10d);Assert.That(Flip(b,camera,false),Is.True);
                Set(u,"LureUntil",10d);Set(u,"LureDistance",8d);Assert.That(Flip(b,camera,true),Is.False,"A forward lure overrides an active fear.");
                Set(u,"LureDistance",1d);Assert.That(Flip(b,camera,false),Is.True);
                b.SetPaused(true);Assert.That(Flip(b,camera,true),Is.True);b.SetPaused(false);
                camera.transform.rotation=Quaternion.Euler(60,90,0);Assert.That(Flip(b,camera,true),Is.True,"A vertical screen direction keeps facing.");
                Set(u,"IsMoving",false);Assert.That(Flip(b,camera,false),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]public void FacingIgnoresStaleControlTimersWhenImmuneAndLocksTheActualFiredShot()
        {
            var immune=Battle(effects:new MonsterEffectSettings(patriot:true));var u=Unit(immune);
            Set(u,"PatriotPhase",PatriotPhase.Destruction);Set(u,"IsMoving",true);Set(u,"FearUntil",10d);Set(u,"LureUntil",10d);Set(u,"LureDistance",-1d);
            var go=new GameObject("Vivi immune facing");var camera=go.AddComponent<Camera>();
            try
            {
                Assert.That(immune.UnitControlImmune(u),Is.True);Assert.That(Flip(immune,camera,true),Is.False);
                var b=Battle();u=Unit(b);u.Motion.Advance(2);Cast(b);b.Advance(.05f);
                Assert.That(Flip(b,camera,false),Is.True,"A shot to a target behind the route faces that target at launch.");
                Kill(b,b.Defender);b.Advance(.6f);Assert.That(Flip(b,camera,false),Is.True,"Changing target cannot turn an already-fired gesture.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
