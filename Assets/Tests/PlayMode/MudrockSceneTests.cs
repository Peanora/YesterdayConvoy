using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class MudrockSceneTests
    {
        float previous;
        [SetUp]public void SaveTime()=>previous=Time.timeScale;
        [TearDown]public void RestoreTime()=>Time.timeScale=previous;
        [UnityTest]public IEnumerator AuthoredDevicesManualCapturePauseAndRestartHaveActualOriginalVisuals()
        {
            yield return SceneManager.LoadSceneAsync("MudrockTrial");yield return null;yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TestSelectionOpen,Is.False);Assert.That(c.Battle.Gramophones.Count,Is.EqualTo(3));
            c.StartBattle();c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);c.Tick(35.1f);yield return null;
            var u=c.Battle.Units[0];Assert.That(u.Active,Is.True);Assert.That(c.Battle.IsSkillReady(u.EntityId),Is.True);c.SelectSkillUnit(u.EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));var point=u.Motion.Position;
            c.Tick(.9f);yield return null;Assert.That(c.Battle.Gramophones.Any(g=>g.Owner==GramophoneOwner.Monsters),Is.True);Assert.That(u.Motion.Position,Is.EqualTo(point));Assert.That(c.GetComponent<MudrockVfxView>().VisibleEffects,Is.GreaterThan(0));
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(10);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));c.Battle.SetPaused(false);c.Tick(.7f);yield return null;Assert.That(c.Battle.IsSkillActionPlaying(u),Is.False);
            c.RestartBattle();yield return null;Assert.That(c.Battle.GramophoneActions,Is.Empty);Assert.That(c.Battle.MudrockCaptures,Is.Empty);
        }
        [UnityTest]public IEnumerator OriginalSkillClipsAndPremultipliedMaterialsAreAvailable()
        {
            yield return SceneManager.LoadSceneAsync("MudrockTrial");yield return null;
            foreach(string key in new[]{"towerIdle_all","towerCapture_all","towerOccupied_all","shield3_over","captureStart_over","attack3_over"}){var clip=MudrockVfxArt.Load(key);Assert.That(clip,Is.Not.Null,key);Assert.That(clip.Count,Is.GreaterThan(30));Assert.That(clip.At(0),Is.Not.Null);}
            Assert.That(Object.FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None).Length,Is.EqualTo(3));Assert.That(Shader.Find("Yesterday Convoy/Mudrock Original Baked"),Is.Not.Null);
        }
    }
}
