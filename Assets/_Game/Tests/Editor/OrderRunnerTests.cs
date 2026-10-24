using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class OrderRunnerTests
    {
        static ManualBattleSettings S(string id,float speed)=>new ManualBattleSettings("CARD_"+id,id+"_TEST",30,1,12,1,.1f,1000,speed,1,1,1,1);
        static EngagementSettings C(bool runner=false,bool commander=false)=>new EngagementSettings(unitRange:.001f,defenderRange:.001f,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
            effects:new MonsterEffectSettings(orderRunner:runner,tacticalCommander:commander));
        static ManualBattle Create(bool runner=true)
        {
            var b=new ManualBattle(S("M15",.8f),new[]{Vector3.zero,Vector3.forward*100},Vector3.left*100,C(runner));
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M17",2),C(commander:true))});return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,long id,string role)
        {var r=b.TryDeployRole(b.RunId,id,b.Entry,"CARD_"+role);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void RealCommandAddsPointThreeSpeedAndSourceDeathRestoresBaseDistance()
        {
            var b=Create();b.Begin();var u=Deploy(b,1,"M15");b.Advance(1);Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(.8).Within(.001));
            var h=Deploy(b,2,"M17");double before=u.Motion.DistanceTravelled;b.Advance(1);Assert.That(u.Motion.DistanceTravelled-before,Is.EqualTo(1.1).Within(.001));
            b.TryDamageUnit(h.EntityId,10000,CombatDamageType.Arts);before=u.Motion.DistanceTravelled;b.Advance(1);Assert.That(u.Motion.DistanceTravelled-before,Is.EqualTo(.8).Within(.001));
        }
        [Test]public void MultipleCommandersDoNotAddMovementBonusMoreThanOnce()
        {
            var b=Create();b.Begin();var u=Deploy(b,1,"M15");b.Advance(.5f);Deploy(b,2,"M17");b.Advance(.3f);Deploy(b,3,"M17");
            Assert.That(b.TacticalCommanders,Is.EqualTo(2));Assert.That(u.Motion.Speed*b.UnitMoveMultiplier(u),Is.EqualTo(1.1f).Within(.001));
        }
        [Test]public void DefaultOffKeepsOriginalSpeedDespiteCommand()
        {
            var b=Create(false);b.Begin();var u=Deploy(b,1,"M15");b.Advance(.5f);Deploy(b,2,"M17");Assert.That(b.UnitMoveMultiplier(u),Is.EqualTo(1));
        }
        [Test]public void PauseAndRestartDoNotAccumulateDistanceOrKeepOrders()
        {
            var b=Create();b.Begin();var u=Deploy(b,1,"M15");b.Advance(.5f);Deploy(b,2,"M17");b.SetPaused(true);double distance=u.Motion.DistanceTravelled;b.Advance(30);
            Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(distance));b.Restart();Assert.That(b.TacticalCommanders,Is.Zero);
        }
    }
}
