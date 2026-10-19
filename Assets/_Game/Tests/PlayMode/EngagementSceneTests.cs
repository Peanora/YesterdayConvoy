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
            Assert.That(c.CombatView.DefenderVisual.sprite.name,Is.EqualTo("kroos-comic"),"Normal M01 must use the new shared-style Kroos asset.");
            Assert.That(c.CombatView.DefenderVisual.sprite.texture.filterMode,Is.EqualTo(FilterMode.Bilinear));
            Assert.That(b.Settings.InitialDP,Is.EqualTo(12));
            Assert.That(b.Settings.InitialDefense,Is.EqualTo(1)); Assert.That(b.Settings.DeploymentLimit,Is.EqualTo(1));
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("维维美"));
            Assert.That(b.CombatSettings.UnitRange,Is.EqualTo(b.CombatSettings.DefenderRange));
            Assert.That(b.CombatSettings.SkillRange,Is.GreaterThanOrEqualTo(b.CombatSettings.UnitRange));
            Assert.That(b.CombatSettings.BlockCount,Is.Zero);
            var platform=GameObject.Find("Platform cap -1,1").GetComponent<Renderer>();
            Assert.That(b.Defender.Position.y,Is.GreaterThan(platform.bounds.max.y));
            Assert.That(b.Units.Count,Is.Zero); c.SelectCard(true); Assert.That(c.TryDeployAt(b.Entry).Success,Is.True);
            Assert.That(c.Selected,Is.False);
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.NotReady));
            bool stopped=false;
            for(int i=0;i<2000 && b.Phase==BattlePhase.Running;i++)
            {
                c.Tick(0.05f);
                foreach(var u in b.Units)
                {
                    stopped |= u.Active && u.HasCombatTarget && !u.IsMoving;
                    Assert.That(u.IsBlocked,Is.False,"High-ground Kroos never blocks; range targeting causes the stop.");
                    if(b.CheckSkill(u.EntityId)==SkillFailure.None)
                    {
                        c.SelectSkillUnit(u.EntityId); int dp=b.DP;
                        Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None)); Assert.That(b.DP,Is.EqualTo(dp));
                    }
                }
                yield return null;
            }
            Assert.That(stopped,Is.True); Assert.That(b.Units.Count,Is.EqualTo(1));
            var firstShot=b.CombatShots[0];
            Assert.That(firstShot.FromDefender,Is.False,"Wisadel starts shooting on entering the shared range.");
            Vector3 offset=firstShot.Origin-firstShot.Destination; offset.y=0;
            Assert.That(offset.magnitude,Is.GreaterThan(3),"This is ranged engagement, not contact distance.");
            Assert.That(b.Defender.Alive,Is.False); Assert.That(b.SkillCasts,Is.GreaterThan(0));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won)); Assert.That(b.Defense,Is.Zero);
            Assert.That(c.SettlementVisible,Is.True); Assert.That(b.Breaches,Is.EqualTo(1));
            Assert.That(c.Definition.Engagement.Snapshot().DefenderHealth,Is.EqualTo(180),"Runtime cannot write to authored data.");
            int oldRun=b.RunId;
            Vector2 screen=ManualBattleController.ScreenPosition(ManualBattleController.SettlementRestartRect.center);
            Vector2 gui=new Vector2(screen.x,Screen.height-screen.y);
            c.ProcessPointerEvent(EventType.MouseDown,gui,0); c.Tick(1);
            c.ProcessPointerEvent(EventType.MouseUp,gui,0); yield return null;
            Assert.That(c.SettlementVisible,Is.False); Assert.That(b.Defense,Is.EqualTo(1)); Assert.That(b.RemainingUses,Is.EqualTo(1));
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
            Vector3 logicalPosition=b.Defender.Position;
            Vector3 idlePosition=c.CombatView.DefenderVisual.transform.position;
            c.Tick(0.2f); yield return null;
            Vector3 idleOffset=c.CombatView.DefenderVisual.transform.position-logicalPosition;
            Assert.That(Vector3.Distance(idlePosition,c.CombatView.DefenderVisual.transform.position),Is.GreaterThan(0.0001f));
            Assert.That(idleOffset.magnitude,Is.LessThanOrEqualTo(0.0401f));
            Assert.That(Vector3.Cross(idleOffset,Camera.main.transform.up).magnitude,Is.LessThan(0.0001f));
            Assert.That(b.Defender.Position,Is.EqualTo(logicalPosition),"Idle must never move the logical defender or its range.");
            b.SetPaused(true); double clock=b.Clock; float sp=b.Units[0].SkillSP;
            Vector3 position=c.CombatView.DefenderVisual.transform.position;
            c.Tick(20); yield return null;
            Assert.That(b.Clock,Is.EqualTo(clock)); Assert.That(b.Units[0].SkillSP,Is.EqualTo(sp));
            Assert.That(c.CombatView.DefenderVisual.transform.position,Is.EqualTo(position));
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.Paused));
            c.RestartBattle(); Assert.That(c.SimulationSpeed,Is.EqualTo(1)); Assert.That(b.IsPaused,Is.False);
            yield return null;
            Assert.That(c.CombatView.DefenderVisual.transform.position,Is.EqualTo(b.Defender.Position));
        }

        [UnityTest]
        public IEnumerator KroosIdlesBeforeDeploymentWithoutStartingTheBattle()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Vector3 logicalPosition=b.Defender.Position;
            Vector3 first=c.CombatView.DefenderVisual.transform.position;
            yield return new WaitForSeconds(0.4f);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Ready)); Assert.That(b.Clock,Is.Zero);
            Assert.That(b.Units.Count,Is.Zero); Assert.That(b.Defender.Position,Is.EqualTo(logicalPosition));
            Assert.That(Vector3.Distance(first,c.CombatView.DefenderVisual.transform.position),Is.GreaterThan(0.001f));
            Assert.That(Vector3.Distance(logicalPosition,c.CombatView.DefenderVisual.transform.position),Is.LessThanOrEqualTo(0.0401f));
        }

        [UnityTest]
        public IEnumerator SequentialUnitsWithoutManualSkillLoseAndTheRunStops()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            for(int attempt=0;attempt<b.Settings.DeploymentLimit && b.Phase!=BattlePhase.Lost;attempt++)
            {
                c.SelectCard(true);
                Assert.That(c.TryDeployAt(b.Entry).Success,Is.True);
                for(int i=0;i<800 && b.ActiveCount>0 && b.Phase==BattlePhase.Running;i++) { c.Tick(0.05f); yield return null; }
            }
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
            Assert.That(b.SkillCasts,Is.Zero); Assert.That(b.ActiveCount,Is.Zero);
            double clock=b.Clock; c.Tick(100); Assert.That(b.Clock,Is.EqualTo(clock));
            c.SelectCard(true);
            Assert.That(c.TryDeployAt(b.Entry).Failure,Is.EqualTo(DeployFailure.Finished));
            foreach(var shot in b.CombatShots) Assert.That(shot.Resolved,Is.True);
        }

        [UnityTest]
        public IEnumerator SingleDeploymentStaysExhaustedAfterCooldownAndPause()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            c.SelectCard(true);
            Assert.That(c.TryDeployAt(b.Entry).Success,Is.True); Assert.That(c.Selected,Is.False);
            int dp=b.DP, uses=b.RemainingUses;
            c.Tick(2);
            Assert.That(c.TryDeployAt(b.Entry).Failure,Is.EqualTo(DeployFailure.NotStarted));
            Assert.That(b.DP,Is.EqualTo(dp)); Assert.That(b.RemainingUses,Is.EqualTo(uses)); Assert.That(b.Units.Count,Is.EqualTo(1));
            c.SelectCard(true); b.SetPaused(true);
            Assert.That(c.TryDeployAt(b.Entry).Failure,Is.EqualTo(DeployFailure.Paused)); Assert.That(c.Selected,Is.True);
            b.SetPaused(false);
            Assert.That(c.TryDeployAt(b.Entry+Vector3.right*20).Failure,Is.EqualTo(DeployFailure.WrongEntry)); Assert.That(c.Selected,Is.True);
            Assert.That(c.TryDeployAt(b.Entry).Failure,Is.EqualTo(DeployFailure.NoUses));
            Assert.That(b.DP,Is.EqualTo(dp)); Assert.That(b.RemainingUses,Is.Zero); Assert.That(b.Units.Count,Is.EqualTo(1));
        }
    }
}
