using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class CombatFeedbackSceneTests
    {
        [UnityTest]public IEnumerator SceneBindsAppliedResultsPauseFreezesAndRestartDropsOldRun()
        {
            float previous=Time.timeScale;
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            try
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(false);c.StartBattle();
                var receipt=c.Battle.TryDeploy(c.Battle.RunId,111,c.Battle.Entry);Assert.That(receipt.Success,Is.True);
                var unit=c.Battle.FindUnit(receipt.EntityId);var view=Object.FindAnyObjectByType<CombatFeedbackView>();Assert.That(view,Is.Not.Null);
                float before=unit.Health;c.Battle.TryDamageUnit(unit.EntityId,4,CombatDamageType.Physical);view.Refresh();
                Assert.That(view.Presentation.Visible.Any(x=>x.TargetId==unit.EntityId&&x.Kind==CombatFeedbackKind.HealthLost),Is.True);
                Assert.That(view.Presentation.Visible.Where(x=>x.Kind==CombatFeedbackKind.HealthLost).Sum(x=>x.Amount),Is.EqualTo(before-unit.Health));
                double time=c.PresentationClock;c.Battle.SetPaused(true);c.Tick(10);view.Refresh();
                Assert.That(c.PresentationClock,Is.EqualTo(time));Assert.That(view.Presentation.Visible,Is.Not.Empty);
                int oldRun=c.Battle.RunId;c.RestartBattle();view.Refresh();Assert.That(c.Battle.RunId,Is.GreaterThan(oldRun));Assert.That(view.Presentation.Visible,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
        [UnityTest]public IEnumerator ReadOnlyOverlayDoesNotConsumeCharacterSkillOrDragPointerEvents()
        {
            float previous=Time.timeScale;
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            try
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.SwitchTrial(false);c.StartBattle();
                var gate=Camera.main.WorldToScreenPoint(c.Battle.Entry);int dp=c.Battle.DP;
                c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(0).center));c.ProcessPointerMove(gate);
                var receipt=c.ProcessPointerUp(gate);Assert.That(receipt.Success,Is.True);Assert.That(c.Battle.DP,Is.LessThan(dp));
                c.Battle.TryDamageUnit(receipt.EntityId,2,CombatDamageType.Physical);var v=Object.FindAnyObjectByType<CombatFeedbackView>();v.Refresh();yield return null;
                // Real existing input entry, not a new feedback test-only input API.
                c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.UnitScreenRect(receipt.EntityId).center));
                Assert.That(c.SkillMenuOpen,Is.True,"Labels cannot steal the unit selection hit.");
                Assert.That(v.Presentation.Visible,Is.Not.Empty);
            }
            finally{Time.timeScale=previous;}
        }
    }
}
