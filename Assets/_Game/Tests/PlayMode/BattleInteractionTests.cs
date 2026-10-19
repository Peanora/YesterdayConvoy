using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class BattleInteractionTests
    {
        private float originalScale;
        [SetUp] public void SetUp() => originalScale=Time.timeScale;
        [TearDown] public void TearDown() => Time.timeScale=originalScale;
        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo",LoadSceneMode.Single);
            yield return null; Time.timeScale=0;
        }
        private static Vector2 Entry(ManualBattleController c) => Camera.main.WorldToScreenPoint(c.Battle.Entry);
        private static void BeginDrag(ManualBattleController c) => c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
        private static Vector2 Body(ManualBattleController c,string id) => ManualBattleController.ScreenPosition(c.UnitScreenRect(id).center);
        private static void Deploy(ManualBattleController c)
        {
            BeginDrag(c); c.ProcessPointerMove(Entry(c)); Assert.That(c.ProcessPointerUp(Entry(c)).Success,Is.True);
        }

        [UnityTest]
        public IEnumerator PausedDragCommitsOnceAndInvalidOrCoolingDropsSpendNothing()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            c.StartBattle(); b.SetPaused(true); BeginDrag(c);
            Assert.That(c.Dragging,Is.True); Assert.That(b.Units.Count,Is.Zero);
            c.ProcessPointerMove(Entry(c)); Assert.That(c.ProcessPointerUp(Entry(c)).Success,Is.True);
            Assert.That(b.IsPaused,Is.True); Assert.That(b.DP,Is.EqualTo(10)); Assert.That(b.Units.Count,Is.EqualTo(1));
            Assert.That(c.Dragging,Is.False); Assert.That(c.Selected,Is.False);
            c.ProcessPointerUp(Entry(c)); Assert.That(b.Units.Count,Is.EqualTo(1));
            BeginDrag(c); Assert.That(c.ProcessPointerUp(Entry(c)).Failure,Is.EqualTo(DeployFailure.CoolingDown));
            BeginDrag(c); Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(b.Entry+Vector3.right*10)).Success,Is.False);
            c.Tick(30); Assert.That(b.Clock,Is.Zero); Assert.That(b.CooldownRemaining,Is.EqualTo(2));
            Assert.That(b.Units[0].SkillSP,Is.EqualTo(8)); Assert.That(b.Units[0].Motion.DistanceTravelled,Is.Zero);
            Assert.That(b.DP,Is.EqualTo(10)); Assert.That(b.RemainingUses,Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator BodyClickSlowsEveryClockAndEmptyClickCancelsOrIconCastsOnce()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Deploy(c); c.Tick(4); string id=b.Units[0].EntityId;
            Assert.That(b.IsSkillReady(id),Is.True); Assert.That(b.SkillCasts,Is.Zero);
            c.ToggleSpeed(); c.ProcessPointerDown(Body(c,id));
            Assert.That(c.SkillMenuOpen,Is.True); Assert.That(c.SelectedEntityId,Is.EqualTo(id)); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(.03f));
            double clock=b.Clock,cd=b.CooldownRemaining; int dp=b.DP;
            c.Tick(1); Assert.That(b.Clock-clock,Is.EqualTo(.03).Within(.0001));
            Assert.That(b.SkillCasts,Is.Zero); Assert.That(b.DP,Is.EqualTo(dp));
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(new Vector2(12,500)));
            Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(2)); Assert.That(b.SkillCasts,Is.Zero);
            c.ProcessPointerDown(Body(c,id));
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.SkillCastRect.center));
            Assert.That(b.SkillCasts,Is.EqualTo(1)); Assert.That(b.Units[0].SkillAmmo,Is.EqualTo(6));
            Assert.That(b.IsSkillReady(id),Is.False); Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(2));
            Assert.That(b.DP,Is.EqualTo(dp)); c.ProcessPointerUp(Entry(c)); Assert.That(b.SkillCasts,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FullPauseDominatesSkillFocusAndReadinessStaysVisibleWithoutCasting()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Deploy(c); c.Tick(4); string id=b.Units[0].EntityId;
            b.SetPaused(true); c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.ReadyMarkerRect(id).center));
            Assert.That(c.SkillMenuOpen,Is.True); Assert.That(c.EffectiveSimulationSpeed,Is.Zero); Assert.That(b.IsSkillReady(id),Is.True);
            double clock=b.Clock; c.Tick(10); Assert.That(b.Clock,Is.EqualTo(clock));
            Assert.That(c.CastFocusedSkill(),Is.EqualTo(SkillFailure.Paused)); Assert.That(b.SkillCasts,Is.Zero);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(new Vector2(12,500)));
            Assert.That(b.IsPaused,Is.True); Assert.That(c.EffectiveSimulationSpeed,Is.Zero); Assert.That(b.IsSkillReady(id),Is.True);
            BeginDrag(c); c.CancelInteraction(); c.ProcessPointerUp(Entry(c)); Assert.That(b.Units.Count,Is.EqualTo(1));
            c.RestartBattle(); Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.Dragging,Is.False); Assert.That(b.Units.Count,Is.Zero);
        }

        [UnityTest]
        public IEnumerator DeadSelectionClosesFocusAndRestartRejectsAnOldDrag()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Deploy(c); c.Tick(4); string id=b.Units[0].EntityId; c.ProcessPointerDown(Body(c,id));
            c.Tick(300); yield return null;
            Assert.That(b.FindUnit(id).Active,Is.False); Assert.That(c.SkillMenuOpen,Is.False); Assert.That(b.IsSkillReady(id),Is.False);
            Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(1));
            BeginDrag(c); b.Restart();
            Assert.That(c.ProcessPointerUp(Entry(c)).Failure,Is.EqualTo(DeployFailure.StaleRun)); Assert.That(b.DP,Is.EqualTo(12)); Assert.That(b.Units.Count,Is.Zero);
        }

        [UnityTest]
        public IEnumerator RepeatedBodyClicksCycleOverlappingEntitiesWithoutCasting()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            Deploy(c); c.Tick(2); Deploy(c); c.Tick(5.5f);
            Assert.That(b.Units[0].Active && b.Units[1].Active,Is.True);
            Vector2 point=Body(c,b.Units[1].EntityId);
            c.ProcessPointerDown(point); string first=c.SelectedEntityId;
            c.ProcessPointerDown(point);
            Assert.That(c.SelectedEntityId,Is.Not.EqualTo(first)); Assert.That(c.SkillMenuOpen,Is.True);
            Assert.That(b.SkillCasts,Is.Zero); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(.03f));
        }

        [UnityTest]
        public IEnumerator QueuedPointerEdgesCanCompleteAPausedDragWithinOneSimulationFrame()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>(); var b=c.Battle;
            c.StartBattle(); b.SetPaused(true);
            Vector2 card=ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center),entry=Entry(c);
            foreach(var type in new[]{EventType.MouseDown,EventType.MouseDrag,EventType.MouseUp})
            {
                Vector2 screen=type==EventType.MouseDown ? card : entry;
                Assert.That(c.ProcessPointerEvent(type,new Vector2(screen.x,Screen.height-screen.y),0),Is.True);
            }
            Assert.That(b.Units.Count,Is.EqualTo(1)); Assert.That(b.DP,Is.EqualTo(10)); Assert.That(b.Clock,Is.Zero);
            Assert.That(b.IsPaused,Is.True); Assert.That(c.Dragging,Is.False);
        }
    }
}
