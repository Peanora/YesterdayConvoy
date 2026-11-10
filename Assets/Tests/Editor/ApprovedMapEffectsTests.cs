using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ApprovedMapEffectsTests
    {
        static readonly Vector3[] Path={Vector3.zero,Vector3.right,Vector3.right*9,Vector3.right*30};
        static EngagementSettings Combat(bool attacks=false)=>new EngagementSettings(unitAttack:1,unitRange:.001f,defenderRange:.001f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,canAttack:attacks);
        static DefenderSettings Guard(Vector3? position=null,int blocks=0,float health=1000)=>
            new DefenderSettings("guard","guard",null,position??Vector3.forward*100,health,1,0,.001f,100,blocks,canAttack:false);
        static ManualBattle Model(float speed=1,EngagementSettings? combat=null,DefenderSettings? guard=null)=>
            new ManualBattle(new ManualBattleSettings("card","unit",100,1,5,1,.1f,1000,speed,1,1,1,1),Path,Vector3.forward*100,
                combat??Combat(),roster:new[]{guard??Guard()});
        static ApprovedMapMechanics Portal(int source=1,float duration=1.5f)=>new ApprovedMapMechanics
        {portals=new[]{new ApprovedPortal{routeId="R1",sourcePointIndex=source,destinationPointIndex=2,transitSeconds=duration}}};
        static ApprovedHeatPump Pump(float warning=1.5f,float phase=0)=>new ApprovedHeatPump
        {position=Vector3.zero,radius=2,period=10,warning=warning,active=2,dps=12,phase=phase};
        static DeployedUnitState Start(ManualBattle b,long request=1)
        {if(b.Phase==BattlePhase.Ready)b.Begin();var r=b.TryDeploy(b.RunId,request,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        // Status fixtures isolate map behavior without widening production setters
        // or depending on a particular boss/defender's full skill scheduler.
        static void Status(DeployedUnitState unit,string property,object value)=>
            typeof(DeployedUnitState).GetProperty(property).GetSetMethod(true).Invoke(unit,new[]{value});

        [Test]public void FastMovementClampsToEntranceThenExitsWithoutWalkingAcrossTheConnector()
        {
            var b=Model(100);b.ConfigureApprovedMapEffects(Portal());var u=Start(b);b.Advance(.05f);
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);Assert.That(b.StrongholdInTransit(u.EntityId),Is.True);
            Assert.That(Vector3.Distance(u.Motion.Position,Vector3.right),Is.LessThanOrEqualTo(.0001f),u.Motion.Position.ToString("R"));Assert.That(u.IsMoving,Is.False);
            Assert.That(b.ApprovedPortalSegment("R1",0),Is.False);Assert.That(b.ApprovedPortalSegment("R1",1),Is.True);
            Assert.That(b.ApprovedPortalSegment("R1",2),Is.False);Assert.That(b.ApprovedPortalSegment("other",1),Is.False);
            b.Advance(1.45f);Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);Assert.That(Vector3.Distance(u.Motion.Position,Vector3.right),Is.LessThanOrEqualTo(.0001f),u.Motion.Position.ToString("R"));
            b.Advance(.1f);Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);
            Assert.That(u.Motion.Position.x,Is.InRange(9,14.001f));
        }

        [Test]public void TransitExcludesIncomingDamageAndPauseFreezesItThenRestartRetainsConfiguration()
        {
            var b=Model();b.ConfigureApprovedMapEffects(Portal());var u=Start(b);b.Advance(1.05f);
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);
            Assert.That(b.TryDamageUnit(u.EntityId,999,CombatDamageType.Physical),Is.False);
            float remaining=b.ApprovedTransitRemaining(u.EntityId);double clock=b.Clock;
            b.SetPaused(true);b.Advance(30);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.ApprovedTransitRemaining(u.EntityId),Is.EqualTo(remaining));
            b.Restart();Assert.That(b.ApprovedPortalTrips,Is.Zero);Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);
            var next=Start(b);b.Advance(1.05f);Assert.That(b.ApprovedInTransit(next.EntityId),Is.True);
        }

        [Test]public void ExitingReevaluatesTheDefenderBlockAtTheDestination()
        {
            var b=Model(guard:Guard(Vector3.right*9,1));b.ConfigureApprovedMapEffects(Portal(duration:.2f));var u=Start(b);
            b.Advance(1.4f);Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));Assert.That(u.IsBlocked,Is.True);
            Assert.That(b.Defender.BlockedCount,Is.EqualTo(1));Assert.That(u.Motion.Position.x,Is.EqualTo(9).Within(.0001));
        }

        [Test]public void FiredSkillCompletesItsMovementLockBeforeEnteringATunnelEvenAfterItsTargetDies()
        {
            var c=new EngagementSettings(unitAttack:1,unitRange:5,defenderRange:.001f,blockCount:0,initialSP:1,requiredSP:1,
                skillAmmo:1,skillAttack:100,skillInterval:3.2f,skillRange:5,projectileTime:.01f);
            var b=Model(combat:c,guard:Guard(Vector3.right*3,health:1));b.ConfigureApprovedMapEffects(Portal(source:0));var u=Start(b);
            Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.None));b.Advance(.2f);
            Assert.That(b.Defender.Alive,Is.False);Assert.That(b.IsSkillActionPlaying(u),Is.True);
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            b.Advance(3);Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);
            b.Advance(.1f);Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
        }

        [Test]public void EntranceWaitsForAnAlreadyFiredSkillProjectileAndPausePreservesItsDamageAmmoAndSingleTransit()
        {
            var c=new EngagementSettings(unitAttack:1,unitRange:.001f,defenderRange:.001f,blockCount:0,initialSP:1,requiredSP:1,
                skillAmmo:1,skillAttack:100,skillInterval:.2f,skillRange:5,projectileTime:3);
            var b=Model(combat:c,guard:Guard(Vector3.right*3,health:1));b.ConfigureApprovedMapEffects(Portal(source:0));var u=Start(b);
            Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.None));b.Advance(.5f);
            var shot=b.CombatShots.Single(s=>s.SourceId==u.EntityId);
            Assert.That(b.IsSkillActionPlaying(u),Is.False);Assert.That(b.ApprovedWaitingAtPortal(u.EntityId),Is.True);
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            Assert.That(shot.Resolved,Is.False);Assert.That(b.Defender.Health,Is.EqualTo(1));Assert.That(u.SkillAmmo,Is.Zero);
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.AlreadyActive));
            double clock=b.Clock;float sp=u.SkillSP;b.SetPaused(true);b.Advance(30);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.SkillSP,Is.EqualTo(sp));Assert.That(shot.Resolved,Is.False);
            b.SetPaused(false);b.Advance(2.45f);Assert.That(shot.Resolved,Is.False);Assert.That(b.ApprovedWaitingAtPortal(u.EntityId),Is.True);
            b.Advance(.15f);Assert.That(shot.Resolved,Is.True);Assert.That(shot.Applied,Is.True);Assert.That(shot.DamageDealt,Is.EqualTo(1));
            Assert.That(b.Defender.Alive,Is.False);Assert.That(b.ApprovedWaitingAtPortal(u.EntityId),Is.False);Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);
            Assert.That(u.SkillsUsed,Is.EqualTo(1));Assert.That(u.SkillAmmo,Is.Zero);Assert.That(b.CombatShots.Count(s=>s.SourceId==u.EntityId),Is.EqualTo(1));
            b.Advance(1.6f);Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);
        }

        [Test]public void FearAtAnExitCannotWalkBackAcrossTheUndergroundConnector()
        {
            var b=Model();b.ConfigureApprovedMapEffects(Portal(duration:.1f));var u=Start(b);b.Advance(1.3f);
            Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));Status(u,nameof(DeployedUnitState.FearUntil),b.Clock+2);b.Advance(1);
            Assert.That(u.Motion.Position.x,Is.EqualTo(9).Within(.0001));Assert.That(b.ApprovedInTransit(u.EntityId),Is.False);
        }

        [Test]public void APreviouslySelectedReadySkillCannotBeCastWhileItsUnitIsUnderground()
        {
            var c=new EngagementSettings(unitRange:.001f,defenderRange:.001f,blockCount:0,initialSP:12);
            var b=Model(combat:c);b.ConfigureApprovedMapEffects(Portal(source:0));var u=Start(b);b.Advance(.05f);
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);Assert.That(b.IsSkillReady(u.EntityId),Is.False);
            Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.AlreadyActive));Assert.That(u.SkillsUsed,Is.Zero);
        }

        [Test]public void HeatDeathAfterTunnelCreatesOneUnhurtHerdSpiritWhoseFearCannotRetreatIntoTheConnector()
        {
            var b=new ManualBattle(new ManualBattleSettings("CARD_M07","M07_SOLDIER",100,1,5,1,.1f,200,1,1,1,1,1),
                Path,Vector3.forward*100,Combat(),roster:new[]{Guard()});
            b.ConfigureSynergies(new[]{"CARD_M21","CARD_M22","CARD_M23","CARD_M24"}.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}));
            var config=Portal(duration:.1f);
            config.heatPumps=new[]{new ApprovedHeatPump{position=Vector3.right*9,radius=2,period=10,warning=0,active=.05f,dps=10000,phase=1.2f}};
            b.ConfigureApprovedMapEffects(config);var source=Start(b);
            Assert.DoesNotThrow(()=>b.Advance(1.4f));Assert.That(source.Active,Is.False);Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));
            var host=b.Units.Single(u=>b.IsHerdHost(u.EntityId));Assert.That(b.Units.Count,Is.EqualTo(2));Assert.That(b.HerdUsed,Is.EqualTo(1));
            Assert.That(host.IsReviving,Is.True);Assert.That(host.Health,Is.EqualTo(host.MaxHealth));Assert.That(host.Motion.Position.x,Is.GreaterThanOrEqualTo(9));
            b.Advance(1.3f);Assert.That(host.IsReviving,Is.False);Assert.That(host.Health,Is.EqualTo(host.MaxHealth));
            Status(host,nameof(DeployedUnitState.FearUntil),b.Clock+2);b.Advance(1);
            Assert.That(host.Motion.Position.x,Is.EqualTo(9).Within(.0001));Assert.That(b.ApprovedInTransit(host.EntityId),Is.False);
            Assert.That(b.ApprovedPortalTrips,Is.EqualTo(1));Assert.That(b.HerdUsed,Is.EqualTo(1));
        }

        [Test]public void HeatHasARealWarningThenAppliesOnlyActiveTimeToBothSidesAndCreditsActualDamage()
        {
            var b=Model(.001f,guard:Guard(Vector3.zero));var pump=Pump();
            b.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{pump}});var u=Start(b);
            Assert.That(b.ApprovedHeatState(0),Is.EqualTo(1));b.Advance(1.4f);
            Assert.That(u.Health,Is.EqualTo(1000));Assert.That(b.Defender.Health,Is.EqualTo(1000));
            b.Advance(.6f);Assert.That(b.ApprovedHeatState(0),Is.EqualTo(2));
            Assert.That(1000-u.Health,Is.EqualTo(6).Within(.02));Assert.That(1000-b.Defender.Health,Is.EqualTo(6).Within(.02));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(6).Within(.02));
            Assert.That(b.DamageRanking.Single().CardId,Is.EqualTo(ManualBattle.SharedDamageCardId));
            b.Advance(2);Assert.That(b.ApprovedHeatState(0),Is.Zero);
            Assert.That(b.ApprovedHeatDamageToUnits,Is.EqualTo(24).Within(.03));
            Assert.That(b.ApprovedHeatDamageToDefenders,Is.EqualTo(24).Within(.03));
        }

        [Test]public void PumpPhasesUseOneClockAndPauseFreezesWarningsDamageAndRestartCounters()
        {
            var b=Model(.001f);b.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{Pump(),Pump(phase:3.3f),Pump(phase:6.6f)}});
            var u=Start(b);Assert.That(b.ApprovedHeatPumpCount,Is.EqualTo(3));Assert.That(b.ApprovedHeatState(1),Is.Zero);
            b.Advance(2);float health=u.Health;double clock=b.Clock;
            b.SetPaused(true);b.Advance(100);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.Health,Is.EqualTo(health));Assert.That(b.ApprovedHeatState(0),Is.EqualTo(2));
            b.SetPaused(false);b.Advance(1.4f);Assert.That(b.ApprovedHeatState(1),Is.EqualTo(1));
            b.Restart();Assert.That(b.ApprovedHeatDamageToUnits,Is.Zero);Assert.That(b.ApprovedHeatPumpCount,Is.EqualTo(3));Assert.That(b.ApprovedHeatState(0),Is.EqualTo(1));
        }

        [Test]public void HeatCannotHitUnitsInTransitRevivingOrInvulnerable()
        {
            var b=Model();var config=Portal(source:0,duration:3);config.heatPumps=new[]{Pump(warning:0)};
            b.ConfigureApprovedMapEffects(config);var u=Start(b);b.Advance(.05f);float enteredHealth=u.Health;
            Assert.That(b.ApprovedInTransit(u.EntityId),Is.True);b.Advance(1);Assert.That(u.Health,Is.EqualTo(enteredHealth));
            var immune=Model(.001f);immune.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{Pump(warning:0)}});
            var v=Start(immune);Status(v,nameof(DeployedUnitState.MandraInvulnerableUntil),100d);immune.Advance(1);Assert.That(v.Health,Is.EqualTo(1000));
            var reviving=Model(.001f);reviving.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{Pump(warning:0)}});
            var w=Start(reviving);Status(w,nameof(DeployedUnitState.IsReviving),true);Status(w,nameof(DeployedUnitState.ReviveAt),100d);reviving.Advance(1);Assert.That(w.Health,Is.EqualTo(1000));
        }

        [Test]public void HeatDeathsResolveOnceWithoutOverkillRankingOrRepeatBreach()
        {
            var b=Model(.001f,guard:Guard(Vector3.zero,health:3));
            b.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{Pump(warning:0)}});Start(b);b.Advance(1);
            Assert.That(b.Defender.Alive,Is.False);Assert.That(b.TotalPlayerDamage,Is.EqualTo(3).Within(.0001));
            b.Advance(1);Assert.That(b.TotalPlayerDamage,Is.EqualTo(3).Within(.0001));Assert.That(b.Breaches,Is.Zero);
        }

        [Test]public void FatalHeatReleasesTheBlockedUnitAndTheDefenderCapacityInTheSameStep()
        {
            var b=Model(.001f,guard:Guard(Vector3.zero,blocks:1));var p=Pump(warning:.2f);p.dps=10000;
            b.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{p}});var u=Start(b);b.Advance(.1f);
            Assert.That(u.IsBlocked,Is.True);Assert.That(b.Defender.BlockedCount,Is.EqualTo(1));b.Advance(.3f);
            Assert.That(u.Active,Is.False);Assert.That(u.IsBlocked,Is.False);Assert.That(b.Defender.BlockedCount,Is.Zero);
            Assert.That(b.ApprovedHeatDamageToUnits,Is.EqualTo(1000).Within(.01));
        }

        [Test]public void ConfigurationIsDeepCopiedAndInvalidReplacementIsAtomic()
        {
            var b=Model(.001f);var config=Portal();config.heatPumps=new[]{Pump(warning:0)};b.ConfigureApprovedMapEffects(config);
            config.portals[0].sourcePointIndex=2;config.heatPumps[0].position=Vector3.one*100;config.heatPumps[0].dps=100;
            Assert.That(b.ApprovedPortalSegment("R1",1),Is.True);Assert.That(b.ApprovedHeatPosition(0),Is.EqualTo(Vector3.zero));
            var bad=Portal();bad.heatPumps=new[]{Pump(),new ApprovedHeatPump{dps=float.NaN}};
            Assert.Throws<ArgumentException>(()=>b.ConfigureApprovedMapEffects(bad));
            Assert.That(b.ApprovedHeatPumpCount,Is.EqualTo(1));var u=Start(b);b.Advance(1);
            Assert.That(1000-u.Health,Is.EqualTo(12).Within(.02));Assert.Throws<InvalidOperationException>(()=>b.ConfigureApprovedMapEffects(new ApprovedMapMechanics()));
        }

        [Test]public void UnknownRoutesOverlappingPortalsAndInvalidPumpTimingAreRejected()
        {
            var b=Model();var missing=Portal();missing.portals[0].routeId="missing";
            Assert.Throws<ArgumentException>(()=>b.ConfigureApprovedMapEffects(missing));
            var overlap=Portal();overlap.portals=new[]{overlap.portals[0],new ApprovedPortal{routeId="R1",sourcePointIndex=0,destinationPointIndex=2}};
            Assert.Throws<ArgumentException>(()=>b.ConfigureApprovedMapEffects(overlap));
            var p=Pump();p.warning=9;p.active=2;
            Assert.Throws<ArgumentException>(()=>b.ConfigureApprovedMapEffects(new ApprovedMapMechanics{heatPumps=new[]{p}}));
            Assert.That(b.ApprovedHeatPumpCount,Is.Zero);Assert.That(b.ApprovedPortalSegment("R1",1),Is.False);
        }

        [Test]public void UnconfiguredMapsRetainTheirOriginalMovementAndReceiveNoHeatOrTransitState()
        {
            var b=Model();var u=Start(b);b.Advance(2);
            Assert.That(u.Motion.Position.x,Is.EqualTo(2).Within(.0001));Assert.That(u.Health,Is.EqualTo(1000));
            Assert.That(b.ApprovedPortalTrips,Is.Zero);Assert.That(b.ApprovedHeatPumpCount,Is.Zero);Assert.That(b.StrongholdInTransit(u.EntityId),Is.False);
        }
    }
}
