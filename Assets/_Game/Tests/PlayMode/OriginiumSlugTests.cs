using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class OriginiumSlugTests
    {
        private float scale;
        private ManualBattleController c;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            c=Object.FindAnyObjectByType<ManualBattleController>();
            var entry=c.MonsterTests.Single(x=>x.Name=="源石虫");
            if(!entry.Playable) Assert.Ignore("Approved slug original media is local-only and not installed on this machine.");
            Assert.That(c.TryEnterMonsterTest(entry.Id),Is.True);
        }
        private DeployedUnitState Deploy()
        { c.SelectCard(true);var r=c.TryDeployAt(c.Battle.Entry);Assert.That(r.Success,Is.True);return c.Battle.FindUnit(r.EntityId); }
        private DemoSpriteVisual Visual(DeployedUnitState u) => Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(x=>x.transform.parent.name==u.EntityId);

        [UnityTest] public IEnumerator PauseDeploymentUsesApprovedArtAndNoSkillInterfaceOrCharge()
        {
            yield return Load();Assert.That(c.MonsterTests.Count(x=>x.Playable),Is.EqualTo(3));
            c.StartBattle();c.Battle.SetPaused(true);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
            c.ProcessPointerMove(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(c.Battle.Entry)).Success,Is.True);
            yield return null;var u=c.Battle.Units[0];var v=Visual(u);int frame=v.FrameIndex;
            c.Tick(10);yield return null;
            Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(11));Assert.That(c.Battle.RemainingUses,Is.EqualTo(2));
            Assert.That(v.FrameIndex,Is.EqualTo(frame));Assert.That(v.Renderer.sprite.name,Does.StartWith("OriginiumSlug/"));
            c.OpenSkillMenu(u.EntityId);Assert.That(c.SkillMenuOpen,Is.False);
            Assert.That(c.Battle.CheckSkill(u.EntityId),Is.EqualTo(SkillFailure.NoSkill));
            c.Battle.SetPaused(false);c.Tick(.1f);yield return null;c.Tick(.8f);yield return null;
            Assert.That(v.FrameState,Is.EqualTo("Walk"));Assert.That(u.SkillSP,Is.Zero);
            c.Battle.SetPaused(true);frame=v.FrameIndex;var position=u.Motion.Position;c.Tick(4);yield return null;
            Assert.That(v.FrameIndex,Is.EqualTo(frame));Assert.That(u.Motion.Position,Is.EqualTo(position));
            c.RestartBattle();Assert.That(c.ViewCount,Is.Zero);Assert.That(c.Battle.RemainingUses,Is.EqualTo(3));
        }

        [UnityTest] public IEnumerator SingleSlugShowsMeleeAndCompletePausedDeathThenRestartsFresh()
        {
            yield return Load();var u=Deploy();bool moved=false,attacked=false;
            for(int i=0;i<450 && u.Active;i++)
            { c.Tick(.05f);yield return null;var v=Visual(u);moved|=v.FrameState=="Walk";attacked|=v.FrameState=="Attack"; }
            // Two unspent deployments keep this battle open; exhaust the remaining uses in a separate final-loss check.
            Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(moved && attacked,Is.True);
            yield return null;var death=Visual(u);Assert.That(death.FrameState,Is.EqualTo("Death"));
            c.Tick(.2f);yield return null;Assert.That(death.FrameIndex,Is.GreaterThan(0));
            c.Battle.SetPaused(true);int frame=death.FrameIndex;c.Tick(2);yield return null;Assert.That(death.FrameIndex,Is.EqualTo(frame));
            c.Battle.SetPaused(false);c.Tick(.35f);yield return null;Assert.That(c.ViewCount,Is.Zero);
            Assert.That(c.Battle.SkillCasts,Is.Zero);Assert.That(c.Battle.Defender.Health,Is.LessThan(100));
            // Restart rather than mutate authored data to test clean scene state.
            c.ReturnToMonsterTests();yield return null;c.TryEnterMonsterTest("CARD_ORIGINIUM_SLUG");
            Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.Defender.Health,Is.EqualTo(100));Assert.That(c.Battle.DP,Is.EqualTo(12));
        }

        [UnityTest] public IEnumerator IsolatedDeploymentsCanLoseAndFinalDeathTailCompletesBeforeSettlement()
        {
            yield return Load();
            for(int n=0;n<3;n++)
            {
                var u=Deploy();
                for(int i=0;i<450 && u.Active;i++) { c.Tick(.05f);yield return null; }
                Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead),"A lone slug should not beat this defender.");
                if(n<2) { c.Tick(.6f);yield return null; }
            }
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(c.SettlementVisible,Is.False);
            Assert.That(c.ViewCount,Is.EqualTo(1));double clock=c.Battle.Clock;
            c.Tick(.2f);yield return null;Assert.That(c.ViewCount,Is.EqualTo(1));Assert.That(c.SettlementVisible,Is.False);
            c.Tick(.31f);yield return null;Assert.That(c.ViewCount,Is.Zero);Assert.That(c.SettlementVisible,Is.True);
            Assert.That(c.Battle.Clock,Is.EqualTo(clock),"Post-battle death presentation must not tick combat.");
            c.RestartBattle();Assert.That(c.SettlementVisible,Is.False);Assert.That(c.PresentationClock,Is.Zero);
        }

        [UnityTest] public IEnumerator ThreeStaggeredSlugsCanDefeatKroosAndReachTheBlueGate()
        {
            yield return Load();Deploy();c.Tick(1.05f);yield return null;Deploy();c.Tick(1.05f);yield return null;Deploy();
            for(int i=0;i<900 && c.Battle.Phase==BattlePhase.Running;i++) { c.Tick(.05f);yield return null; }
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(c.Battle.Defender.Alive,Is.False);
            Assert.That(c.Battle.Breaches,Is.GreaterThanOrEqualTo(1));Assert.That(c.Battle.Defense,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(9));Assert.That(c.Battle.SkillCasts,Is.Zero);
            c.RestartBattle();yield return null;Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(c.ViewCount,Is.Zero);
        }
    }
}
