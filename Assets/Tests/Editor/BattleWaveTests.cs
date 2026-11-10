using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class BattleWaveTests
    {
        static ManualBattleSettings Settings(string id="CARD_M07",int uses=20,int cost=2,float cooldown=6,float speed=1)
            =>new ManualBattleSettings(id,"WAVE_TEST",99,99,uses,cost,cooldown,100,speed,1,1,1,1);

        static DeploymentLane[] Lanes(float length=10000)
            =>Enumerable.Range(0,3).Select(i=>new DeploymentLane("R"+(i+1),"B"+(i+1),
                new[]{Vector3.right*i*10,Vector3.right*i*10+Vector3.forward*length})).ToArray();

        static BattleWavePlan Plan(float seconds=45,float final=120)
            =>new BattleWavePlan(new[]{new BattleWaveDefinition("R1",16,seconds),new BattleWaveDefinition("R2",16,seconds),
                new BattleWaveDefinition("R3",18,seconds)},final);

        static ManualBattle Model(ManualBattleSettings? settings=null,float length=10000,bool combat=false)
        {
            var lanes=Lanes(length);
            var battle=new ManualBattle(settings??Settings(),lanes[0].Points,Vector3.one*1000,
                combat?(EngagementSettings?)new EngagementSettings(initialSP:0,requiredSP:100,blockCount:0):null,
                null,combat?new[]{new DefenderSettings("far","far",null,Vector3.one*1000,1000,1,0,.001f,10000,canAttack:false)}:null,lanes);
            return battle;
        }

        [Test]public void PlansValidateTheirInputsAndKeepAnImmutableSnapshot()
        {
            Assert.Throws<ArgumentException>(()=>new BattleWaveDefinition(" ",1,45));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new BattleWaveDefinition("R1",-1,45));
            foreach(float invalid in new[]{0,-1,float.NaN,float.PositiveInfinity})
            {
                Assert.Throws<ArgumentOutOfRangeException>(()=>new BattleWaveDefinition("R1",1,invalid));
                Assert.Throws<ArgumentOutOfRangeException>(()=>new BattleWavePlan(new[]{new BattleWaveDefinition("R1",1,45)},invalid));
            }
            Assert.Throws<ArgumentNullException>(()=>new BattleWavePlan(null,120));
            Assert.Throws<ArgumentException>(()=>new BattleWavePlan(Array.Empty<BattleWaveDefinition>(),120));
            Assert.Throws<ArgumentException>(()=>new BattleWavePlan(new BattleWaveDefinition[]{null},120));
            var source=new[]{new BattleWaveDefinition("R1",0,45)};
            var plan=new BattleWavePlan(source,120);source[0]=new BattleWaveDefinition("R3",99,1);
            Assert.That(plan.Waves[0].LaneId,Is.EqualTo("R1"));Assert.That(plan.Waves[0].DP,Is.Zero);
            Assert.That(plan.TotalDuration,Is.EqualTo(165));
            Assert.Throws<NotSupportedException>(()=>((IList<BattleWaveDefinition>)plan.Waves).Clear());
        }

        [Test]public void ConfigurationRequiresKnownLanesAndAnUnstartedBattle()
        {
            var b=Model();
            Assert.Throws<ArgumentNullException>(()=>b.ConfigureWaves(null));
            Assert.Throws<ArgumentException>(()=>b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("absent",1,45)},120)));
            Assert.That(b.WavesEnabled,Is.False);Assert.That(b.DP,Is.EqualTo(99));
            b.ConfigureWaves(Plan());Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.WaveCount,Is.EqualTo(3));
            Assert.That(b.CanDeployOnLane("R1"),Is.True);Assert.That(b.CanDeployOnLane("R2"),Is.False);
            b.Advance(100);Assert.That(b.Clock,Is.Zero);Assert.That(b.WaveRemaining,Is.EqualTo(45));
            b.Begin();Assert.Throws<InvalidOperationException>(()=>b.ConfigureWaves(Plan()));
            Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.BattleTimeRemaining,Is.EqualTo(255));
        }

        [Test]public void EachBoundaryReplacesTheBudgetAndOpensOnlyItsOwnGate()
        {
            var b=Model();b.ConfigureWaves(Plan());b.Begin();
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[0].Entry).Success,Is.True);
            Assert.That(b.DP,Is.EqualTo(14));
            Assert.That(b.TryDeploy(b.RunId,2,b.Lanes[1].Entry).Failure,Is.EqualTo(DeployFailure.WaveEntryClosed));
            b.Advance(45);
            Assert.That(b.Clock,Is.EqualTo(45).Within(1e-7));Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));
            Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.CurrentWaveBudget,Is.EqualTo(16));Assert.That(b.CurrentWaveLaneId,Is.EqualTo("R2"));
            Assert.That(b.TryDeploy(b.RunId,3,b.Lanes[0].Entry).Failure,Is.EqualTo(DeployFailure.WaveEntryClosed));
            Assert.That(b.TryDeploy(b.RunId,4,b.Lanes[1].Entry).Success,Is.True);
            b.Advance(45);
            Assert.That(b.CurrentWaveIndex,Is.EqualTo(2));Assert.That(b.CurrentWaveBudget,Is.EqualTo(18));Assert.That(b.DP,Is.EqualTo(18));
            Assert.That(b.CanDeployOnLane("R3"),Is.True);Assert.That(b.CanDeployOnLane("R2"),Is.False);
        }

        [Test]public void UsesAndCooldownCarryAcrossWavesAndDuplicateRequestsCannotSpendANewBudget()
        {
            var b=Model(Settings(cooldown:10));b.ConfigureWaves(Plan());b.Begin();b.Advance(44);
            var receipt=b.TryDeploy(b.RunId,11,b.Lanes[0].Entry);
            Assert.That(receipt.Success,Is.True);b.Advance(1);
            int dp=b.DP,uses=b.RemainingUses;
            Assert.That(b.CooldownRemaining,Is.EqualTo(9).Within(.00001));
            Assert.That(b.TryDeploy(b.RunId,11,b.Lanes[0].Entry).EntityId,Is.EqualTo(receipt.EntityId));
            Assert.That(b.DP,Is.EqualTo(dp));Assert.That(b.RemainingUses,Is.EqualTo(uses));
            Assert.That(b.TryDeploy(b.RunId,12,b.Lanes[1].Entry).Failure,Is.EqualTo(DeployFailure.CoolingDown));
            b.Advance(9);Assert.That(b.TryDeploy(b.RunId,12,b.Lanes[1].Entry).Success,Is.True);
            Assert.That(b.RemainingUses,Is.EqualTo(18));
        }

        [Test]public void DifferentCardsKeepIndependentCooldownsAndTheirWholeBattleUses()
        {
            var b=Model(combat:true);
            var c=new EngagementSettings(initialSP:0,requiredSP:100,blockCount:0);
            b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Settings("CARD_M08",uses:1,cooldown:24),c,true)});
            b.ConfigureWaves(Plan());b.Begin();
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[0].Entry).Success,Is.True);
            Assert.That(b.TryDeployRole(b.RunId,2,b.Lanes[0].Entry,"CARD_M08").Success,Is.True);
            Assert.That(b.CardCooldown("CARD_M07"),Is.EqualTo(6));Assert.That(b.CardCooldown("CARD_M08"),Is.EqualTo(24));
            b.Advance(45);
            Assert.That(b.FindCard("CARD_M08").RemainingUses,Is.Zero);
            Assert.That(b.TryDeployRole(b.RunId,3,b.Lanes[1].Entry,"CARD_M08").Failure,Is.EqualTo(DeployFailure.NoUses));
            Assert.That(b.FindCard("CARD_M07").RemainingUses,Is.EqualTo(19));
        }

        [Test]public void PauseFreezesWaveTimeAndAllowsOnlyAtomicCurrentGateDeployments()
        {
            var b=Model();b.ConfigureWaves(Plan());b.Begin();b.SetPaused(true);
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[1].Entry,true).Failure,Is.EqualTo(DeployFailure.WaveEntryClosed));
            var placed=b.TryDeploy(b.RunId,2,b.Lanes[0].Entry,true);Assert.That(placed.Success,Is.True);
            b.Advance(300);Assert.That(b.Clock,Is.Zero);Assert.That(b.WaveRemaining,Is.EqualTo(45));
            Assert.That(b.CooldownRemaining,Is.EqualTo(6));Assert.That(b.Units[0].Motion.DistanceTravelled,Is.Zero);
            Assert.That(b.DP,Is.EqualTo(14));Assert.That(b.RemainingUses,Is.EqualTo(19));
            b.SetPaused(false);b.Advance(45);Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));
        }

        [Test]public void AnEmptyExhaustedWaveWaitsForTheNextBudgetRatherThanLosing()
        {
            var b=Model(Settings(cost:20));b.ConfigureWaves(Plan());b.Begin();
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));b.Advance(90);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(b.CurrentWaveIndex,Is.EqualTo(2));
            Assert.That(b.DP,Is.EqualTo(18));b.Advance(45);
            Assert.That(b.FinalAdvanceActive,Is.True);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
            Assert.That(b.TimedOut,Is.False);Assert.That(b.Clock,Is.EqualTo(135).Within(1e-7));
        }

        [Test]public void FinalAdvanceClosesEveryGateDiscardsMoneyAndKeepsExistingUnitsMoving()
        {
            var b=Model();b.ConfigureWaves(Plan());b.Begin();b.TryDeploy(b.RunId,1,b.Lanes[0].Entry);b.Advance(135);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(b.FinalAdvanceActive,Is.True);
            Assert.That(b.DeploymentWindowOpen,Is.False);Assert.That(b.CurrentWaveIndex,Is.EqualTo(2));
            Assert.That(b.CurrentWaveLaneId,Is.Null);Assert.That(b.DP,Is.Zero);Assert.That(b.CurrentWaveBudget,Is.Zero);
            Assert.That(b.WaveRemaining,Is.EqualTo(120).Within(1e-7));
            foreach(var lane in b.Lanes)
            {
                Assert.That(b.CanDeployOnLane(lane.Id),Is.False);
                Assert.That(b.TryDeploy(b.RunId,2,lane.Entry).Failure,Is.EqualTo(DeployFailure.DeploymentWindowEnded));
            }
            double distance=b.Units[0].Motion.DistanceTravelled;b.Advance(5);
            Assert.That(b.Units[0].Motion.DistanceTravelled,Is.GreaterThan(distance));
        }

        [Test]public void ACoarseFrameStopsExactlyAtTheDeadlineAndRejectsLaterArrivals()
        {
            var b=Model(length:256);b.ConfigureWaves(Plan());b.Begin();b.TryDeploy(b.RunId,1,b.Lanes[0].Entry);b.Advance(1000);
            Assert.That(b.Clock,Is.EqualTo(255).Within(1e-7));Assert.That(b.TimedOut,Is.True);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(b.Breaches,Is.Zero);Assert.That(b.BattleTimeRemaining,Is.Zero);
            double distance=b.Units[0].Motion.DistanceTravelled;b.Advance(100);
            Assert.That(b.Clock,Is.EqualTo(255));Assert.That(b.Units[0].Motion.DistanceTravelled,Is.EqualTo(distance));
        }

        [TestCase(false)][TestCase(true)]public void ALastInstantBreachWinsBeforeTheTimeout(bool combat)
        {
            var lanes=Lanes(4);
            var b=new ManualBattle(new ManualBattleSettings("CARD","UNIT",16,1,1,2,1,100,1,1,1,1,1),lanes[0].Points,Vector3.one*1000,
                combat?(EngagementSettings?)new EngagementSettings():null,null,combat?Array.Empty<DefenderSettings>():null,lanes,allowEmptyRoster:combat);
            b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("R1",16,1)},3));b.Begin();
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[0].Entry).Success,Is.True);b.Advance(10);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.TimedOut,Is.False);Assert.That(b.Breaches,Is.EqualTo(1));
            Assert.That(b.Clock,Is.EqualTo(4).Within(.00001));
        }

        [TestCase(1f,70f)][TestCase(1.1f,0f)]public void DeadlineSettlesDueDeathEffectsButNeverWaitsForLaterEffects(float delay,float expectedDamage)
        {
            var lanes=Lanes();
            var effects=new MonsterEffectSettings(blastAttack:70,blastRadius:2,blastDelay:delay);
            var combat=new EngagementSettings(unitRange:.01f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,
                skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:effects);
            var guard=new DefenderSettings("guard","guard",null,Vector3.forward*1.5f,100,1,0,.001f,10000,canAttack:false);
            var b=new ManualBattle(Settings(),lanes[0].Points,Vector3.one*1000,combat,null,new[]{guard},lanes);
            b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("R1",16,1)},1));b.Begin();
            var deployed=b.TryDeploy(b.RunId,1,b.Lanes[0].Entry);b.Advance(1);
            Assert.That(b.TryDamageUnit(deployed.EntityId,10000,CombatDamageType.Arts),Is.True);
            Assert.That(b.HasPendingDeathBlast,Is.True);b.Advance(100);
            Assert.That(b.Clock,Is.EqualTo(2));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(b.TimedOut,Is.True);
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(expectedDamage));
            Assert.That(b.Defender.Health,Is.EqualTo(100-expectedDamage));
        }

        [Test]public void UnitsSkillsDeadDefendersAndDamageRankingPersistAcrossWaveBoundaries()
        {
            var lanes=Lanes();
            var combat=new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:5,defenderRange:.001f,blockCount:0,initialSP:0,requiredSP:100);
            var guards=new[]{new DefenderSettings("near","near",null,Vector3.forward*2,15,1,0,.001f,10000,canAttack:false),
                new DefenderSettings("far","far",null,Vector3.one*1000,1000,1,0,.001f,10000,canAttack:false)};
            var b=new ManualBattle(Settings(),lanes[0].Points,Vector3.one*1000,combat,null,guards,lanes);
            b.ConfigureWaves(Plan());b.Begin();var deployed=b.TryDeploy(b.RunId,1,b.Lanes[0].Entry);b.Advance(44);
            var u=b.FindUnit(deployed.EntityId);Assert.That(b.TryDamageUnit(u.EntityId,7,CombatDamageType.Arts),Is.True);
            float hp=u.Health,sp=u.SkillSP,damage=b.TotalPlayerDamage;double distance=u.Motion.DistanceTravelled;
            Assert.That(b.Defenders[0].Alive,Is.False);Assert.That(damage,Is.EqualTo(15));b.Advance(1);
            Assert.That(b.FindUnit(deployed.EntityId),Is.SameAs(u));Assert.That(u.Health,Is.EqualTo(hp));
            Assert.That(u.SkillSP,Is.GreaterThan(sp));Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(distance));
            Assert.That(b.Defenders[0].Alive,Is.False);Assert.That(b.TotalPlayerDamage,Is.EqualTo(damage));
        }

        [Test]public void RestartRetainsThePlanButResetsWaveRuntimeAndRejectsTheOldRun()
        {
            var b=Model();var plan=Plan();b.ConfigureWaves(plan);b.Begin();int run=b.RunId;b.TryDeploy(run,1,b.Lanes[0].Entry);b.Advance(300);
            Assert.That(b.TimedOut,Is.True);b.Restart();
            Assert.That(b.WavePlan,Is.SameAs(plan));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(b.Clock,Is.Zero);
            Assert.That(b.TimedOut,Is.False);Assert.That(b.FinalAdvanceActive,Is.False);Assert.That(b.CurrentWaveIndex,Is.Zero);
            Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.RemainingUses,Is.EqualTo(20));Assert.That(b.CooldownRemaining,Is.Zero);
            Assert.That(b.Units,Is.Empty);Assert.That(b.TryDeploy(run,1,b.Lanes[0].Entry).Failure,Is.EqualTo(DeployFailure.StaleRun));
        }

        [Test]public void GolemDeploymentUsesTheSameWaveGateAndCannotBypassFinalAdvance()
        {
            var b=Model(combat:true);
            b.ConfigureSynergies(new[]{new ShopTrialModel.Piece{Id="CARD_M26",Star=1},new ShopTrialModel.Piece{Id="CARD_M27",Star=1}});
            b.ConfigureWaves(Plan());b.Begin();
            Assert.That(b.TryDeployGolem(b.RunId,1,b.Lanes[1].Entry).Failure,Is.EqualTo(DeployFailure.WaveEntryClosed));
            Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.GolemDeployed,Is.False);
            Assert.That(b.TryDeploy(b.RunId,2,b.Lanes[0].Entry).Success,Is.True);b.Advance(135);
            Assert.That(b.TryDeployGolem(b.RunId,3,b.Lanes[2].Entry).Failure,Is.EqualTo(DeployFailure.DeploymentWindowEnded));
            b.Restart();b.Begin();Assert.That(b.TryDeployGolem(b.RunId,4,b.Lanes[0].Entry).Success,Is.True);
            var golem=b.Units.Single(u=>u.Golem);b.Advance(45);
            Assert.That(b.TryDeployGolem(b.RunId,4,b.Lanes[0].Entry).EntityId,Is.EqualTo(golem.EntityId));
            Assert.That(b.DP,Is.EqualTo(16));Assert.That(b.GolemDeployed,Is.True);
        }

        [Test]public void LegacyBattlesKeepAllGatesAndTheirOriginalExhaustionRule()
        {
            var b=Model(Settings(cost:100));Assert.That(b.WavesEnabled,Is.False);
            Assert.That(b.Lanes.All(l=>b.CanDeployOnLane(l.Id)),Is.True);Assert.That(b.CanDeployOnLane("absent"),Is.False);
            b.Begin();Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(b.TimedOut,Is.False);
            var ongoing=Model();ongoing.Begin();ongoing.Advance(300);
            Assert.That(ongoing.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(ongoing.TimedOut,Is.False);
        }
    }
}
