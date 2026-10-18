using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ManualBattleTests
    {
        private static ManualBattleSettings Settings(int dp=8,int defense=3,int uses=6,int cost=2,float cooldown=2,
            float health=100,float range=2.8f,float damage=16)
            => new ManualBattleSettings("CARD_TEST","UNIT_TEST",dp,defense,uses,cost,cooldown,health,1,1,range,damage,1);
        private static ManualBattle New(ManualBattleSettings settings, float length=100, bool towerAtEntry=false)
            => new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*length},towerAtEntry ? Vector3.zero : new Vector3(1000,0,0));

        [Test]
        public void DeploymentCommitsOnceAndRapidRequestsCannotDoubleSpend()
        {
            var battle=New(Settings()); battle.Begin();
            var first=battle.TryDeploy(battle.RunId,1,battle.Entry);
            Assert.That(first.Success,Is.True);
            Assert.That(battle.DP,Is.EqualTo(6)); Assert.That(battle.RemainingUses,Is.EqualTo(5));
            var repeated=battle.TryDeploy(battle.RunId,1,battle.Entry);
            Assert.That(repeated.EntityId,Is.EqualTo(first.EntityId));
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Failure,Is.EqualTo(DeployFailure.CoolingDown));
            Assert.That(battle.DP,Is.EqualTo(6)); Assert.That(battle.RemainingUses,Is.EqualTo(5)); Assert.That(battle.Units.Count,Is.EqualTo(1));
        }

        [Test]
        public void InvalidTargetsAndUnstartedRequestsLeaveResourcesAndCooldownUntouched()
        {
            var battle=New(Settings());
            Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Failure,Is.EqualTo(DeployFailure.NotStarted));
            battle.Begin();
            Assert.That(battle.TryDeploy(battle.RunId,1,Vector3.right*10).Failure,Is.EqualTo(DeployFailure.WrongEntry));
            Assert.That(battle.TryDeploy(battle.RunId,1,new Vector3(float.NaN,0,0)).Failure,Is.EqualTo(DeployFailure.WrongEntry));
            Assert.That(battle.DP,Is.EqualTo(8)); Assert.That(battle.RemainingUses,Is.EqualTo(6));
            Assert.That(battle.CooldownRemaining,Is.Zero); Assert.That(battle.Units.Count,Is.Zero);
            Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True,"A failed request was never committed.");
        }

        [Test]
        public void InsufficientMoneyAndExhaustedUsesCannotPartiallyDeploy()
        {
            foreach(bool exhaustedUses in new[]{false,true})
            {
                var battle=New(exhaustedUses ? Settings(uses:1) : Settings(dp:3)); battle.Begin();
                battle.TryDeploy(battle.RunId,1,battle.Entry);
                int dp=battle.DP,uses=battle.RemainingUses; double cooldown=battle.CooldownRemaining;
                var result=battle.TryDeploy(battle.RunId,2,battle.Entry);
                Assert.That(result.Failure,Is.EqualTo(exhaustedUses ? DeployFailure.NoUses : DeployFailure.NoDP));
                Assert.That(battle.DP,Is.EqualTo(dp)); Assert.That(battle.RemainingUses,Is.EqualTo(uses));
                Assert.That(battle.CooldownRemaining,Is.EqualTo(cooldown)); Assert.That(battle.Units.Count,Is.EqualTo(1));
            }
        }

        [Test]
        public void BusyEntryRejectsWithoutSpendingThenBecomesAvailable()
        {
            var battle=New(Settings(cooldown:0.1f)); battle.Begin(); battle.TryDeploy(battle.RunId,1,battle.Entry);
            battle.Advance(0.1f);
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Failure,Is.EqualTo(DeployFailure.EntryBusy));
            Assert.That(battle.DP,Is.EqualTo(6));
            battle.Advance(0.3f);
            Assert.That(battle.TryDeploy(battle.RunId,3,battle.Entry).Success,Is.True);
            Assert.That(battle.ActiveCount,Is.EqualTo(2));
            Assert.That(battle.Units[0].EntityId,Is.Not.EqualTo(battle.Units[1].EntityId));
            Assert.That(battle.Units[0].Motion,Is.Not.SameAs(battle.Units[1].Motion));
        }

        [Test]
        public void PauseFreezesMovementCombatAndCooldownAndDoesNotQueueDeployment()
        {
            var battle=New(Settings(),towerAtEntry:true); battle.Begin(); battle.TryDeploy(battle.RunId,1,battle.Entry); battle.Advance(0.5f);
            double clock=battle.Clock,cd=battle.CooldownRemaining; Vector3 position=battle.Units[0].Motion.Position;
            float health=battle.Units[0].Health; int shots=battle.ShotsFired;
            Assert.That(shots,Is.GreaterThan(0),"Pause is checked under actual tower fire.");
            battle.SetPaused(true); battle.Advance(20);
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Failure,Is.EqualTo(DeployFailure.Paused));
            Assert.That(battle.Clock,Is.EqualTo(clock)); Assert.That(battle.CooldownRemaining,Is.EqualTo(cd));
            Assert.That(battle.Units[0].Motion.Position,Is.EqualTo(position)); Assert.That(battle.DP,Is.EqualTo(6));
            Assert.That(battle.Units[0].Health,Is.EqualTo(health)); Assert.That(battle.ShotsFired,Is.EqualTo(shots));
            battle.SetPaused(false); battle.Advance(1.5f);
            Assert.That(battle.CooldownRemaining,Is.LessThan(0.00001));
            Assert.That(battle.Units.Count,Is.EqualTo(1),"Resume must not invent an unconfirmed deployment.");
            Assert.That(battle.Units[0].Health,Is.LessThan(health),"Tower attacks resume on the simulation clock.");
        }

        [Test]
        public void LastAffordableUnitCanStillWinAndTheGateSettlesOnce()
        {
            var battle=New(Settings(dp:2,defense:1,uses:1),length:2); battle.Begin(); battle.TryDeploy(battle.RunId,1,battle.Entry);
            battle.Advance(1);
            Assert.That(battle.DP,Is.Zero); Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            battle.Advance(2);
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Won)); Assert.That(battle.Defense,Is.Zero);
            Assert.That(battle.Breaches,Is.EqualTo(1));
            battle.Advance(100);
            Assert.That(battle.Defense,Is.Zero); Assert.That(battle.Breaches,Is.EqualTo(1));
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Failure,Is.EqualTo(DeployFailure.Finished));
        }

        [Test]
        public void AnEmptyFieldWithAnAffordableCoolingCardWaitsRatherThanLoses()
        {
            var battle=New(Settings(dp:4,defense:2,uses:2,cooldown:5),length:0.1f);
            battle.Begin(); battle.TryDeploy(battle.RunId,1,battle.Entry); battle.Advance(0.2f);
            Assert.That(battle.ActiveCount,Is.Zero); Assert.That(battle.CooldownRemaining,Is.GreaterThan(4));
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            battle.Advance(4.8f);
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Success,Is.True);
            battle.Advance(0.2f);
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Won));
        }

        [Test]
        public void ExhaustedResourcesOnlyLoseAfterTheLastUnitDies()
        {
            var battle=New(Settings(dp:2,uses:1,range:10,damage:100),towerAtEntry:true);
            battle.Begin(); battle.TryDeploy(battle.RunId,1,battle.Entry);
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            battle.Advance(0.1f);
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Lost)); Assert.That(battle.ActiveCount,Is.Zero);
            Assert.That(battle.Defense,Is.EqualTo(3)); Assert.That(battle.Units[0].Resolution,Is.EqualTo(UnitResolution.Dead));
        }

        [Test]
        public void ThereIsNoTimeoutOrPassiveResourceRecovery()
        {
            var battle=New(Settings()); battle.Begin(); battle.Advance(600);
            Assert.That(battle.DP,Is.EqualTo(8)); Assert.That(battle.RemainingUses,Is.EqualTo(6));
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
        }

        [Test]
        public void RetryClearsTransactionsAndRejectsStaleRunRequests()
        {
            var battle=New(Settings()); battle.Begin();
            int oldRun=battle.RunId; string oldCard=battle.CardInstanceId;
            var oldUnit=battle.TryDeploy(oldRun,1,battle.Entry);
            battle.Advance(1); battle.SetPaused(true); battle.Restart();
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Ready)); Assert.That(battle.IsPaused,Is.False);
            Assert.That(battle.DP,Is.EqualTo(8)); Assert.That(battle.RemainingUses,Is.EqualTo(6));
            Assert.That(battle.Defense,Is.EqualTo(3)); Assert.That(battle.Clock,Is.Zero); Assert.That(battle.CooldownRemaining,Is.Zero);
            Assert.That(battle.Units.Count,Is.Zero); Assert.That(battle.ShotsFired,Is.Zero);
            Assert.That(battle.CardInstanceId,Is.Not.EqualTo(oldCard));
            Assert.That(battle.TryDeploy(oldRun,1,battle.Entry).Failure,Is.EqualTo(DeployFailure.StaleRun));
            battle.Begin();
            Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).EntityId,Is.Not.EqualTo(oldUnit.EntityId));
        }

        [Test]
        public void TowerTargetsFurthestUnitWithoutSharingHealthAcrossEntities()
        {
            var battle=New(Settings(cooldown:0.1f,range:10),towerAtEntry:true);
            battle.Begin(); var first=battle.TryDeploy(battle.RunId,1,battle.Entry); battle.Advance(1);
            battle.TryDeploy(battle.RunId,2,battle.Entry); battle.Advance(1);
            Assert.That(battle.LastShotEntityId,Is.EqualTo(first.EntityId));
            Assert.That(battle.Units[0].Health,Is.LessThan(battle.Units[1].Health));
            Assert.That(battle.Units[1].Health,Is.EqualTo(100));
        }

        [Test]
        public void InvalidSettingsAndInvalidTimeAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>Settings(cost:0));
            Assert.Throws<ArgumentException>(()=>Settings(cooldown:float.NaN));
            var battle=New(Settings());
            Assert.Throws<ArgumentOutOfRangeException>(()=>battle.Advance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>battle.Advance(float.PositiveInfinity));
        }
    }
}
