using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterSelectionTests
    {
        private float scale;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }
        private static string Id(ManualBattleController c,string name) => c.MonsterTests.Single(x=>x.Name==name).Id;
        private static IEnumerator Load() { yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0; }

        [UnityTest]
        public IEnumerator StartupMenuBlocksBattleAndPendingChoicesDoNotMutateResources()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.TestSelectionOpen,Is.True); Assert.That(c.MonsterTests.Count,Is.EqualTo(41));
            Assert.That(c.MonsterTests.Count(x=>x.Playable),Is.EqualTo(c.MonsterTests.Single(x=>x.Name=="源石虫").Playable ? 3 : 2));
            var battle=c.Battle; int dp=battle.DP;
            c.SelectCard(true); c.StartBattle();
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(battle.Entry)).Success,Is.False);
            c.Tick(100); Assert.That(c.Dragging,Is.False); Assert.That(battle.Units,Is.Empty);
            Assert.That(battle.Clock,Is.Zero); Assert.That(c.EffectiveSimulationSpeed,Is.Zero);
            Assert.That(c.TryEnterMonsterTest(Id(c,"猎狗")),Is.False); Assert.That(c.TryEnterMonsterTest("unknown"),Is.False);
            Assert.That(c.TestSelectionOpen,Is.True); Assert.That(c.Battle,Is.SameAs(battle)); Assert.That(battle.DP,Is.EqualTo(dp));
            Assert.That(c.TryEnterMonsterTest(Id(c,"维维美")),Is.True);
            Assert.That(c.TestSelectionOpen,Is.False); Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("维维美"));
            c.SelectCard(true); Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
        }

        [UnityTest]
        public IEnumerator ReturningFromCastingOrDraggingClearsEverythingBeforeEnteringAnotherMonster()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>();
            c.TryEnterMonsterTest(Id(c,"塔露拉")); c.SelectCard(true); c.TryDeployAt(c.Battle.Entry); c.Tick(4.05f);
            c.OpenSkillMenu(c.Battle.Units[0].EntityId); Assert.That(c.CastFocusedSkill(),Is.EqualTo(SkillFailure.None));
            c.Tick(1.25f); yield return null;
            var fire=Object.FindAnyObjectByType<FireRainView>(); Assert.That(fire.VisibleStreams,Is.GreaterThan(0));
            c.ReturnToMonsterTests(); yield return null;
            Assert.That(c.TestSelectionOpen,Is.True); Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.Dragging,Is.False);
            Assert.That(c.ViewCount,Is.Zero); Assert.That(c.Battle.Units,Is.Empty); Assert.That(c.Battle.CombatShots,Is.Empty);
            Assert.That(c.Battle.SkillCasts,Is.Zero); Assert.That(c.Battle.Clock,Is.Zero); Assert.That(fire.VisibleStreams,Is.Zero);
            c.Tick(50); Assert.That(c.Battle.Clock,Is.Zero);
            c.TryEnterMonsterTest(Id(c,"维维美")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));
            Assert.That(c.Battle.DP,Is.EqualTo(12)); Assert.That(c.Battle.RemainingUses,Is.EqualTo(1)); Assert.That(c.Battle.IsPaused,Is.False);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center)); Assert.That(c.Dragging,Is.True);
            c.ReturnToMonsterTests(); c.TryEnterMonsterTest(Id(c,"塔露拉"));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(c.Battle.Entry)).Success,Is.False);
            Assert.That(c.Battle.Units,Is.Empty); Assert.That(c.Battle.DP,Is.EqualTo(12)); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(3));
            foreach(var defender in c.Battle.Defenders) Assert.That(defender.Health,Is.EqualTo(c.Battle.CombatSettings.DefenderHealth));
        }

        [UnityTest]
        public IEnumerator SettlementReturnRequiresMatchingPointerEdgesAndReenteringStartsFresh()
        {
            yield return Load(); var c=Object.FindAnyObjectByType<ManualBattleController>();
            c.TryEnterMonsterTest(Id(c,"维维美")); c.SelectCard(true); c.TryDeployAt(c.Battle.Entry); c.Tick(100);
            Assert.That(c.SettlementVisible,Is.True);
            Vector2 s=ManualBattleController.ScreenPosition(ManualBattleController.SettlementReturnRect.center);
            Vector2 gui=new Vector2(s.x,Screen.height-s.y);
            c.ProcessPointerEvent(EventType.MouseDown,Vector2.zero,0); c.ProcessPointerEvent(EventType.MouseUp,gui,0);
            Assert.That(c.TestSelectionOpen,Is.False);
            c.ProcessPointerEvent(EventType.MouseDown,gui,0); c.ProcessPointerEvent(EventType.MouseUp,gui,0);
            Assert.That(c.TestSelectionOpen,Is.True); Assert.That(c.SettlementVisible,Is.False);
            c.TryEnterMonsterTest(Id(c,"维维美")); Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));
            Assert.That(c.Battle.Units,Is.Empty); Assert.That(c.Battle.DP,Is.EqualTo(12)); Assert.That(c.Battle.Defense,Is.EqualTo(1));
        }
    }
}
