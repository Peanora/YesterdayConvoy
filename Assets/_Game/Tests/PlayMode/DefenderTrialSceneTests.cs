using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderTrialSceneTests
    {
        private float scale;
        [SetUp] public void Setup() => scale=Time.timeScale;
        [TearDown] public void Teardown() => Time.timeScale=scale;

        [UnityTest] public IEnumerator EightOriginalModelsPauseRestartAndReturnRestoreTheBorrowedScene()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var camera=Camera.main; Vector3 cameraPosition=camera.transform.position;
            var original=GameObject.Find("Industrial Map");
            Assert.That(c.TryEnterDefenderTrial(),Is.True); yield return null;
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(8)); Assert.That(original.activeSelf,Is.False);
            var visuals=Object.FindAnyObjectByType<DefenderRosterView>(); Assert.That(visuals.Count,Is.EqualTo(8));
            foreach(var d in c.Battle.Defenders)
            {
                var r=visuals.FindVisual(d.EntityId); Assert.That(r.sprite.name,Does.StartWith(d.Settings.ArtId+"/idle/"));
                Assert.That(r.transform.position,Is.EqualTo(d.Position));
                Assert.That(r.transform.localScale,Is.EqualTo(Vector3.one));
            }
            c.StartBattle(); c.Battle.SetPaused(true);
            Vector2 entry=camera.WorldToScreenPoint(c.Battle.Entry);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center)); c.ProcessPointerMove(entry);
            Assert.That(c.ProcessPointerUp(entry).Success,Is.True); c.Tick(10); yield return null;
            Assert.That(c.Battle.Clock,Is.Zero); var sprite=visuals.FindVisual("D01").sprite;
            c.Tick(10); yield return null; Assert.That(visuals.FindVisual("D01").sprite,Is.SameAs(sprite));
            c.Battle.SetPaused(false); c.Tick(.5f); yield return null;
            Assert.That(visuals.FindVisual("D07").sprite.name,Does.Contain("/attack/"));
            c.RestartBattle(); Assert.That(c.Battle.Units.Count,Is.Zero);
            c.ReturnToMonsterTests(); yield return null; yield return null;
            Assert.That(c.DefenderTrialActive,Is.False); Assert.That(c.TestSelectionOpen,Is.True);
            Assert.That(original.activeSelf,Is.True); Assert.That(camera.transform.position,Is.EqualTo(cameraPosition));
            Assert.That(Object.FindAnyObjectByType<DefenderRosterView>(),Is.Null);
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator ReloadingAnActiveTrialDoesNotRestoreDestroyedGatesOrTheNewCamera()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            Vector3 originalCamera=Camera.main.transform.position;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.TryEnterDefenderTrial(),Is.True); yield return null;
            Assert.That(Camera.main.transform.position,Is.Not.EqualTo(originalCamera));
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null;
            c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.DefenderTrialActive,Is.False);
            Assert.That(c.TestSelectionOpen,Is.True);
            Assert.That(Camera.main.transform.position,Is.EqualTo(originalCamera));
            Assert.That(Object.FindAnyObjectByType<DefenseTrialMap>(),Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FireRainCompletesTheDefenseTrialAndDefendersUseTheirOwnHealth()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); Assert.That(c.TryEnterDefenderTrial(),Is.True);
            c.SelectCard(true); Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            bool healed=false,attacked=false;
            for(int i=0;i<1800 && c.Battle.Phase==BattlePhase.Running;i++)
            {
                c.Tick(.05f);
                var u=c.Battle.Units[0];
                if(c.Battle.IsSkillReady(u.EntityId)) { c.SelectSkillUnit(u.EntityId); c.ActivateSelectedSkill(); }
                foreach(var shot in c.Battle.CombatShots) { healed|=shot.Healing && shot.Applied; attacked|=shot.FromDefender && !shot.Healing; }
                if(i%50==0) yield return null;
            }
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));
            Assert.That(healed && attacked,Is.True); Assert.That(c.Battle.Defense,Is.Zero);
            c.RestartBattle(); foreach(var d in c.Battle.Defenders) Assert.That(d.Health,Is.EqualTo(d.MaxHealth));
        }
    }
}
