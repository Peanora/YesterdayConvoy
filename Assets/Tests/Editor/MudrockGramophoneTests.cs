using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class MudrockGramophoneTests
    {
        static ManualBattle Create(float charge=100,bool target=true,float initialSP=35,float range=3)
        {
            var c=new EngagementSettings(unitAttack:1,unitRange:.1f,unitInterval:3,initialSP:initialSP,requiredSP:35,spPerSecond:1,skillAmmo:0,skillAttack:1,skillInterval:1.5f,skillRange:999,mudrockCapture:true,effects:new MonsterEffectSettings(mudrock:true));
            var b=new ManualBattle(new ManualBattleSettings("CARD_M28","mudrock",30,1,3,3,.1f,10000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.right,c,roster:new[]{new DefenderSettings("guard","guard","D04",new Vector3(1,0,0),10000,1,0,.1f,10,canAttack:false)});
            b.ConfigureGramophones(target?new[]{new GramophoneSettings("A",new Vector3(2,0,0),charge,range)}:Array.Empty<GramophoneSettings>());b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);return b;
        }
        [Test]public void CaptureIsDelayedIdempotentAndLocksTheWholeAction()
        {
            var b=Create();var u=b.Units[0];Assert.That(b.TryActivateSkill(b.RunId,10,u.EntityId),Is.EqualTo(SkillFailure.None));Assert.That(b.TryActivateSkill(b.RunId,10,u.EntityId),Is.EqualTo(SkillFailure.None));Assert.That(b.MudrockCaptures.Count,Is.EqualTo(1));
            b.Advance(.8f);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Defenders));Assert.That(u.Motion.DistanceTravelled,Is.Zero);b.Advance(.1f);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Monsters));
            b.Advance(.5f);Assert.That(u.Motion.DistanceTravelled,Is.Zero);Assert.That(u.SkillSP,Is.Zero);b.Advance(.2f);Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(0));Assert.That(u.SkillSP,Is.GreaterThan(0));
        }
        [Test]public void PauseFreezesChargeCastAndDeviceActions()
        {var b=Create();b.TryActivateSkill(b.RunId,2,b.Units[0].EntityId);b.SetPaused(true);b.Advance(100);Assert.That(b.Clock,Is.Zero);Assert.That(b.Gramophones[0].Charge,Is.EqualTo(100));Assert.That(b.MudrockCaptures[0].Resolved,Is.False);}
        [Test]public void NoTargetDoesNotSpendSkillResourcesOrUseCount()
        {var b=Create(target:false);var u=b.Units[0];Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.NoTarget));Assert.That(u.SkillSP,Is.EqualTo(35));Assert.That(u.SkillsUsed,Is.Zero);}
        [Test]public void CasterDeathCancelsPendingCapture()
        {var b=Create();var u=b.Units[0];b.TryActivateSkill(b.RunId,2,u.EntityId);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);b.Advance(1);Assert.That(b.MudrockCaptures[0].Cancelled,Is.True);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Defenders));}
        [Test]public void DevicesAttackOpponentsAndHealTheirCurrentSide()
        {var b=Create(charge:0);var u=b.Units[0];b.TryDamageUnit(u.EntityId,500,CombatDamageType.Physical);float start=u.Health;b.Advance(6);Assert.That(b.Defender.Health,Is.LessThan(10000));Assert.That(u.Health,Is.GreaterThan(start));Assert.That(b.GramophoneActions.Any(a=>a.Healing&&a.Applied),Is.True);}
        [Test]public void DefendersCanReclaimAndRestartRestoresAuthoredOwnership()
        {var b=Create(charge:99.9f,range:.1f);b.Advance(.2f);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Defenders));b.TryActivateSkill(b.RunId,2,b.Units[0].EntityId);b.Advance(1);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Monsters));b.Advance(101);Assert.That(b.Gramophones[0].Owner,Is.EqualTo(GramophoneOwner.Defenders));b.Restart();Assert.That(b.Gramophones[0].Charge,Is.EqualTo(99.9f));Assert.That(b.MudrockCaptures,Is.Empty);}
        [Test]public void DuplicateConfigurationsFailAtomically()
        {var b=Create();b.Restart();Assert.Throws<ArgumentException>(()=>b.ConfigureGramophones(new[]{new GramophoneSettings("x",Vector3.zero),new GramophoneSettings("x",Vector3.one)}));Assert.That(b.Gramophones.Count,Is.EqualTo(1));}
        [Test]public void NoAmmoHybridCaptureConfigurationIsAcceptedOnlyForMudrock()
        {Assert.Throws<ArgumentException>(()=>new EngagementSettings(mudrockCapture:true));Assert.That(Create().CombatSettings.MudrockCapture,Is.True);}
    }
}
