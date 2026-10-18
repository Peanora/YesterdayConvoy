using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ManualDeploymentTests
    {
        private float originalScale;
        [SetUp] public void SetUp() => originalScale=Time.timeScale;
        [TearDown] public void TearDown() => Time.timeScale=originalScale;

        [UnityTest]
        public IEnumerator SelectDeployPauseMultipleUnitsWinAndRetryUsingTheRealScene()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var controller=Object.FindAnyObjectByType<ManualBattleController>();
            var source=Object.FindAnyObjectByType<InitialDemo>();
            ManualBattle battle=controller.Battle;
            Assert.That(controller.enabled,Is.True); Assert.That(source.enabled,Is.False);
            Assert.That(source.Monster.gameObject.activeSelf,Is.False,"There is no automatic unit before deployment.");
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Ready)); Assert.That(battle.Units.Count,Is.Zero);
            Assert.That(source.Card.RecruitCost,Is.EqualTo(5)); Assert.That(battle.Settings.DeploymentCost,Is.EqualTo(2));
            Assert.That(controller.TryDeployAt(battle.Entry).Success,Is.False);
            controller.SelectCard(true);
            Assert.That(controller.TryDeployAt(source.BlueGate.position).Failure,Is.EqualTo(DeployFailure.WrongEntry));
            Assert.That(battle.DP,Is.EqualTo(8)); Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Ready));
            Assert.That(controller.TryDeployAt(battle.Entry).Success,Is.True);
            yield return null;
            Assert.That(controller.ViewCount,Is.EqualTo(1));
            var firstView=GameObject.Find(battle.Units[0].EntityId).GetComponentInChildren<DemoSpriteVisual>();
            Assert.That(firstView.Puppet,Is.Not.Null);
            battle.SetPaused(true);
            Vector3 foot=firstView.Puppet.LeftFoot; double clock=battle.Clock,cd=battle.CooldownRemaining;
            controller.SelectCard(false); controller.SelectCard(true);
            Assert.That(controller.TryDeployAt(battle.Entry).Failure,Is.EqualTo(DeployFailure.Paused));
            battle.Advance(10); yield return null;
            Assert.That(battle.Clock,Is.EqualTo(clock)); Assert.That(battle.CooldownRemaining,Is.EqualTo(cd));
            Assert.That(firstView.Puppet.LeftFoot,Is.EqualTo(foot));
            battle.SetPaused(false);
            for(int i=0;i<3;i++) { battle.Advance(2); Assert.That(controller.TryDeployAt(battle.Entry).Success,Is.True); yield return null; }
            Assert.That(controller.ViewCount,Is.EqualTo(4)); Assert.That(battle.ActiveCount,Is.EqualTo(4));
            var newest=GameObject.Find(battle.Units[3].EntityId).GetComponentInChildren<DemoSpriteVisual>();
            Assert.That(newest.Puppet.MovementWeight,Is.Zero,"Each spawned visual starts its own ease-in from the shared simulation clock.");
            Assert.That(battle.DP,Is.Zero); Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            for(int i=0;i<160 && battle.Phase==BattlePhase.Running;i++) { battle.Advance(0.25f); yield return null; }
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Won)); Assert.That(battle.Defense,Is.Zero);
            Assert.That(controller.Definition.Snapshot().InitialDP,Is.EqualTo(8),"Spending must not write back to the shared definition.");
            int oldRun=battle.RunId; controller.RestartBattle(); yield return null;
            Assert.That(controller.ViewCount,Is.Zero); Assert.That(battle.Units.Count,Is.Zero);
            Assert.That(battle.RunId,Is.EqualTo(oldRun+1)); Assert.That(battle.DP,Is.EqualTo(8));
            Assert.That(controller.Selected,Is.False); Assert.That(battle.RemainingUses,Is.EqualTo(6));
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Ready)); Assert.That(source.Monster.gameObject.activeSelf,Is.False);
            Assert.That(controller.Definition.Card.RecruitCost,Is.EqualTo(5),"Runtime must not mutate authored card data.");
        }

        [UnityTest]
        public IEnumerator SpacedSingleUnitsLoseOnlyAfterAllAffordableDeploymentsAreResolved()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var controller=Object.FindAnyObjectByType<ManualBattleController>();
            ManualBattle battle=controller.Battle;
            controller.SelectCard(true);
            for(int attempt=0;attempt<4;attempt++)
            {
                Assert.That(controller.TryDeployAt(battle.Entry).Success,Is.True);
                Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
                for(int tick=0;tick<80 && battle.ActiveCount>0;tick++) { battle.Advance(0.5f); yield return null; }
                if(attempt<3)Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running),"Affordable future deployments remain.");
            }
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Lost)); Assert.That(battle.ActiveCount,Is.Zero);
            Assert.That(battle.Defense,Is.EqualTo(3)); Assert.That(battle.DP,Is.Zero);
            yield return null; Assert.That(controller.ViewCount,Is.Zero);
        }
    }
}
