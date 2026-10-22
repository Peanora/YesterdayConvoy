using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ArcherCombatTests
    {
        private float scale;
        private ManualBattleController c;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.TryEnterMonsterTest("CARD_M08"),Is.True,"Install approved M08 local combat package before this integration suite.");
        }
        private DeployedUnitState Deploy()
        { c.SelectCard(true);var r=c.TryDeployAt(c.Battle.Entry);Assert.That(r.Success,Is.True);return c.Battle.FindUnit(r.EntityId); }
        private DemoSpriteVisual Visual(DeployedUnitState u) => Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.transform.parent.name==u.EntityId);

        [UnityTest] public IEnumerator PausedDeploymentWalkAndRangedAttackUseApprovedFramesAndSimulationClock()
        {
            yield return Load(); c.StartBattle();c.Battle.SetPaused(true);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
            c.ProcessPointerMove(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(c.Battle.Entry)).Success,Is.True);
            yield return null;var u=c.Battle.Units[0];var v=Visual(u);var position=u.Motion.Position;int frame=v.FrameIndex;
            c.Tick(2);yield return null;Assert.That(c.Battle.Clock,Is.Zero);Assert.That(u.Motion.Position,Is.EqualTo(position));Assert.That(v.FrameIndex,Is.EqualTo(frame));
            Assert.That(c.Battle.DP,Is.EqualTo(11));Assert.That(v.Renderer.sprite.name,Does.StartWith("M08/"));
            c.Battle.SetPaused(false);c.Tick(.3f);yield return null;Assert.That(v.FrameState,Is.EqualTo("Walk"));
            for(int i=0;i<300 && double.IsNegativeInfinity(u.LastAttackTime);i++) { c.Tick(.02f);yield return null; }
            Assert.That(u.HasCombatTarget,Is.True);Assert.That(v.FrameState,Is.EqualTo("Attack"));
            var shot=c.Battle.CombatShots.First(s=>!s.FromDefender);Assert.That(shot.StartedAt-u.LastAttackTime,Is.EqualTo(32.0/60).Within(.001));
            float health=c.Battle.Defender.Health;c.Tick(.4f);yield return null;Assert.That(c.Battle.Defender.Health,Is.EqualTo(health),"Windup must precede projectile damage.");
            c.Battle.SetPaused(true);frame=v.FrameIndex;double clock=c.Battle.Clock;c.Tick(10);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(v.FrameIndex,Is.EqualTo(frame));
            c.Battle.SetPaused(false);c.Tick(.5f);yield return null;Assert.That(c.Battle.Defender.Health,Is.LessThan(health));
            c.OpenSkillMenu(u.EntityId);Assert.That(c.SkillMenuOpen,Is.False);Assert.That(u.SkillSP,Is.Zero);
            Assert.That(c.Battle.CheckSkill(u.EntityId),Is.EqualTo(SkillFailure.NoSkill));
            c.RestartBattle();Assert.That(c.Battle.CombatShots,Is.Empty);Assert.That(c.ViewCount,Is.Zero);
        }

        [UnityTest] public IEnumerator LoneArcherDiesWithFullPausedDeathTailAndRestartsFresh()
        {
            yield return Load();var u=Deploy();
            for(int i=0;i<900 && u.Active;i++) { c.Tick(.03f);yield return null; }
            Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));yield return null;
            var v=Visual(u);Assert.That(v.FrameState,Is.EqualTo("Death"));c.Tick(.3f);yield return null;Assert.That(v.FrameIndex,Is.GreaterThan(0));
            c.Battle.SetPaused(true);int frame=v.FrameIndex;c.Tick(3);yield return null;Assert.That(v.FrameIndex,Is.EqualTo(frame));
            c.Battle.SetPaused(false);c.Tick(.8f);yield return null;Assert.That(c.ViewCount,Is.Zero);
            c.RestartBattle();Assert.That(c.Battle.Defender.Health,Is.EqualTo(140));Assert.That(c.Battle.DP,Is.EqualTo(12));Assert.That(c.Battle.Units,Is.Empty);
        }

        [UnityTest] public IEnumerator ThreeArchersDefeatDefenderBreachOncePerEntityAndReturnWithoutLeakingState()
        {
            yield return Load();Deploy();c.Tick(1.1f);yield return null;Deploy();c.Tick(1.1f);yield return null;Deploy();
            c.ToggleSpeed();Assert.That(c.SimulationSpeed,Is.EqualTo(2));
            for(int i=0;i<1000 && c.Battle.Phase==BattlePhase.Running;i++) { c.Tick(.03f);yield return null; }
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(c.Battle.Defender.Alive,Is.False);
            Assert.That(c.Battle.Breaches,Is.GreaterThanOrEqualTo(1));Assert.That(c.Battle.SkillCasts,Is.Zero);
            int breaches=c.Battle.Breaches;c.Tick(50);Assert.That(c.Battle.Breaches,Is.EqualTo(breaches));
            c.ReturnToMonsterTests();yield return null;Assert.That(c.ViewCount,Is.Zero);Assert.That(c.Battle.CombatShots,Is.Empty);
            Assert.That(c.TryEnterMonsterTest("CARD_M08"),Is.True);Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.DP,Is.EqualTo(12));
        }
    }
}
