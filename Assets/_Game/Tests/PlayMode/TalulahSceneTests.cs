using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class TalulahSceneTests
    {
        private float scale;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }
        [UnityTest]
        public IEnumerator SwitchDragCastPauseAndReturnToOriginalTrial()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true);
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("塔露拉")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(3));
            c.StartBattle(); c.Battle.SetPaused(true);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(c.Battle.Entry)).Success,Is.True);
            c.Tick(10); Assert.That(c.Battle.Clock,Is.Zero);
            c.Battle.SetPaused(false); c.Tick(4.05f); yield return null;
            var u=c.Battle.Units[0]; Assert.That(c.Battle.IsSkillReady(u.EntityId),Is.True);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.UnitScreenRect(u.EntityId).center));
            Assert.That(c.SkillMenuOpen,Is.True); c.Tick(1); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(.03f));
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.SkillCastRect.center));
            Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.Battle.SkillCasts,Is.EqualTo(1));
            c.Tick(1.25f); yield return null;
            foreach(var d in c.Battle.Defenders) Assert.That(d.Health,Is.LessThan(c.Battle.CombatSettings.DefenderHealth));
            var visual=Object.FindAnyObjectByType<DemoSpriteVisual>();
            Assert.That(visual.FrameState,Is.EqualTo("Skill"));
            c.Battle.SetPaused(true); double clock=c.Battle.Clock; var sprite=visual.Renderer.sprite;
            c.Tick(10); yield return null; Assert.That(c.Battle.Clock,Is.EqualTo(clock)); Assert.That(visual.Renderer.sprite,Is.EqualTo(sprite));
            c.RestartBattle(); yield return null;
            Assert.That(c.Battle.CombatShots,Is.Empty); Assert.That(c.Battle.Units,Is.Empty);
            foreach(var d in c.Battle.Defenders) Assert.That(d.Alive,Is.True);
            c.SwitchTrial(false); yield return null;
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("维维美")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<DemoCardView>().Card,Is.EqualTo(c.Definition.Card));
        }
    }
}
