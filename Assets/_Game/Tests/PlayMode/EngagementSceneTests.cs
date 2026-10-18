using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class EngagementSceneTests
    {
        private float originalScale;
        [SetUp] public void SetUp() => originalScale=Time.timeScale;
        [TearDown] public void TearDown() => Time.timeScale=originalScale;

        [UnityTest]
        public IEnumerator RealSceneManualSPKillsKroosAndCompletesTheGateThenResets()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Assert.That(b.EngagementEnabled,Is.True); Assert.That(c.CombatView.DefenderVisual.sprite,Is.EqualTo(c.Definition.Engagement.KroosSprite));
            Assert.That(b.Units.Count,Is.Zero); c.SelectCard(true); Assert.That(c.TryDeployAt(b.Entry).Success,Is.True);
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.NotReady));
            int count=1; bool stopped=false,independent=false;
            for(int i=0;i<2000 && b.Phase==BattlePhase.Running;i++)
            {
                c.Tick(0.05f);
                if(count<4 && b.Clock+0.00001>=count*2)
                { Assert.That(c.TryDeployAt(b.Entry).Success,Is.True); count++; }
                foreach(var u in b.Units)
                {
                    stopped |= u.Active && u.HasCombatTarget && !u.IsMoving;
                    if(b.CheckSkill(u.EntityId)==SkillFailure.None)
                    {
                        c.SelectSkillUnit(u.EntityId); int dp=b.DP;
                        Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None)); Assert.That(b.DP,Is.EqualTo(dp));
                        if(b.Units.Count>1 && u==b.Units[0]) independent=b.Units[1].SkillSP>0;
                    }
                }
                yield return null;
            }
            Assert.That(stopped,Is.True); Assert.That(independent,Is.True);
            Assert.That(b.Defender.Alive,Is.False); Assert.That(b.SkillCasts,Is.GreaterThan(0));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won)); Assert.That(b.Defense,Is.Zero);
            Assert.That(c.Definition.Engagement.Snapshot().DefenderHealth,Is.EqualTo(180),"Runtime cannot write to authored data.");
            int oldRun=b.RunId; c.RestartBattle(); yield return null;
            Assert.That(b.RunId,Is.EqualTo(oldRun+1)); Assert.That(b.Defender.Alive,Is.True);
            Assert.That(b.SkillCasts,Is.Zero); Assert.That(b.CombatShots.Count,Is.Zero); Assert.That(c.ViewCount,Is.Zero);
            Assert.That(c.CombatView.DefenderVisual.enabled,Is.True); Assert.That(c.SelectedEntityId,Is.Null);
        }

        [UnityTest]
        public IEnumerator PauseAndDoubleSpeedUseTheSameClockForSPAndPresentation()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            c.SelectCard(true); c.TryDeployAt(b.Entry); c.ToggleSpeed(); c.Tick(0.5f); yield return null;
            Assert.That(b.Clock,Is.EqualTo(1).Within(0.0001)); Assert.That(b.Units[0].SkillSP,Is.EqualTo(9).Within(0.0001));
            Assert.That(b.Units[0].Motion.DistanceTravelled,Is.EqualTo(0.75).Within(0.0001));
            b.SetPaused(true); double clock=b.Clock; float sp=b.Units[0].SkillSP;
            Vector3 position=c.CombatView.DefenderVisual.transform.position;
            c.Tick(20); yield return null;
            Assert.That(b.Clock,Is.EqualTo(clock)); Assert.That(b.Units[0].SkillSP,Is.EqualTo(sp));
            Assert.That(c.CombatView.DefenderVisual.transform.position,Is.EqualTo(position));
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.Paused));
            c.RestartBattle(); Assert.That(c.SimulationSpeed,Is.EqualTo(1)); Assert.That(b.IsPaused,Is.False);
        }

        [UnityTest]
        public IEnumerator SequentialUnitsWithoutManualSkillLoseAndTheRunStops()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle; c.SelectCard(true);
            for(int attempt=0;attempt<4;attempt++)
            {
                Assert.That(c.TryDeployAt(b.Entry).Success,Is.True);
                for(int i=0;i<800 && b.ActiveCount>0 && b.Phase==BattlePhase.Running;i++) { c.Tick(0.05f); yield return null; }
            }
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost)); Assert.That(b.Defender.Alive,Is.True);
            Assert.That(b.SkillCasts,Is.Zero); Assert.That(b.ActiveCount,Is.Zero);
            double clock=b.Clock; c.Tick(100); Assert.That(b.Clock,Is.EqualTo(clock));
            c.SelectCard(true);
            Assert.That(c.TryDeployAt(b.Entry).Failure,Is.EqualTo(DeployFailure.Finished));
            foreach(var shot in b.CombatShots) Assert.That(shot.Resolved,Is.True);
        }
    }
}
