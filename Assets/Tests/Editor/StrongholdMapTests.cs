using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class StrongholdMapTests
    {
        static ManualBattle Model(bool configured=true,bool infection=false)
        {
            var c=new EngagementSettings(unitAttack:10,unitArmor:0,unitRange:.1f,unitInterval:1,defenderHealth:1000,defenderAttack:1,defenderArmor:0,defenderRange:.1f,defenderInterval:1,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            var lanes=StrongholdMapLayout.Lanes();var route=infection?new[]{StrongholdMapLayout.Cell(6,3),StrongholdMapLayout.Cell(6,3)+Vector3.forward*100}:lanes[0].points;
            var b=new ManualBattle(new ManualBattleSettings("CARD_M08","M08_ARCHER",30,1,3,1,.1f,1000,infection?.001f:2,1,1,1,1),route,Vector3.one*100,c);
            if(configured)b.ConfigureStrongholdMap();return b;
        }
        static DeployedUnitState Start(ManualBattle b){b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void InfectionBuffDamagePauseAndRestartAreAuthoritative()
        {
            var b=Model(infection:true);var u=Start(b);b.Advance(.1f);Assert.That(b.StrongholdInfected(u.EntityId),Is.True);
            Assert.That(b.UnitAttackMultiplier(u),Is.EqualTo(1.2f).Within(.001f));Assert.That(b.UnitAttackRate(u),Is.EqualTo(1.2f).Within(.001f));
            b.Advance(1.05f);Assert.That(u.Health,Is.EqualTo(999));b.SetPaused(true);b.Advance(30);Assert.That(u.Health,Is.EqualTo(999));
            b.Restart();Assert.That(b.StrongholdInfectionTouches,Is.Zero);Assert.That(b.StrongholdInfected(u.EntityId),Is.False);
        }
        [Test]public void PortalWaitsThreeSimulationSecondsAndCannotTakeDirectHits()
        {
            var b=Model();var u=Start(b);var route=StrongholdMapLayout.Lanes()[0].points;float distance=0;for(int i=1;i<=6;i++)distance+=Vector3.Distance(route[i-1],route[i]);
            b.Advance(distance/2+.1f);Assert.That(b.StrongholdInTransit(u.EntityId),Is.True);Vector3 p=u.Motion.Position;float hp=u.Health;
            Assert.That(b.TryDamageUnit(u.EntityId,999,CombatDamageType.Physical),Is.False);
            b.SetPaused(true);b.Advance(100);Assert.That(u.Motion.Position,Is.EqualTo(p));b.SetPaused(false);
            b.Advance(2.5f);Assert.That(b.StrongholdInTransit(u.EntityId),Is.True);b.Advance(.6f);
            Assert.That(b.StrongholdPortalTrips,Is.EqualTo(1));Assert.That(b.StrongholdInTransit(u.EntityId),Is.False);
            Assert.That(Vector3.Distance(u.Motion.Position,route[7]),Is.LessThan(.5f));Assert.That(u.Health,Is.LessThanOrEqualTo(hp));
            b.Restart();Assert.That(b.StrongholdPortalTrips,Is.Zero);Assert.That(b.StrongholdInTransit(u.EntityId),Is.False);
        }
        [Test]public void OtherMapsDoNotAcquireInfectionOrPortalBehavior()
        {
            var b=Model(configured:false,infection:true);var u=Start(b);b.Advance(3);
            Assert.That(u.Health,Is.EqualTo(1000));Assert.That(b.UnitAttackMultiplier(u),Is.EqualTo(1));Assert.That(b.StrongholdInfectionTouches,Is.Zero);
        }
        [Test]public void ExhaustedAttackersLoseAndRestartClearsTheResult()
        {
            var b=Model(infection:true);b.Begin();
            for(int i=0;i<3;i++)
            {
                var result=b.TryDeploy(b.RunId,i+1,b.Entry);Assert.That(result.Success,Is.True);
                Assert.That(b.TryDamageUnit(result.EntityId,2000,CombatDamageType.Physical),Is.True);b.Advance(.2f);
            }
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));b.Restart();
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(b.RemainingUses,Is.EqualTo(3));Assert.That(b.Units,Is.Empty);
        }
    }
}
