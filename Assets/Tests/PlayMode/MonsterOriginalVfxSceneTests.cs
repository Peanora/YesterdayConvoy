using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterOriginalVfxSceneTests
    {
        float previous;
        [SetUp]public void Setup()=>previous=Time.timeScale;
        [TearDown]public void TearDown()=>Time.timeScale=previous;

        [UnityTest]public IEnumerator TwoRealExplosiveDeathsKeepTheirOwnNativeFramesPauseAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            foreach(string id in new[]{"CARD_M04","CARD_M06"})
            {
                Assert.That(c.TryEnterMonsterTest(id),Is.True);while(c.ArtLoading)yield return null;
                c.SelectCard(true);var a=c.TryDeployAt(c.Battle.Entry);Assert.That(a.Success,Is.True);
                c.Tick(c.Battle.Settings.Cooldown+.02f);yield return null;
                c.SelectCard(true);var z=c.TryDeployAt(c.Battle.Entry);Assert.That(z.Success,Is.True);
                Assert.That(c.Battle.TryDamageUnit(a.EntityId,100000,CombatDamageType.Arts),Is.True);
                Assert.That(c.Battle.TryDamageUnit(z.EntityId,100000,CombatDamageType.Arts),Is.True);
                Assert.That(c.Battle.DeathBlasts.Count,Is.EqualTo(2));
                c.Tick(c.Battle.CombatSettings.Effects.BlastDelay+.15f);yield return null;
                var view=Object.FindAnyObjectByType<MonsterDeathEffectView>();
                string key=id=="CARD_M04"?"bslime_die_01_start":"snslime_die_01_start";
                var renderers=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&r.sprite.name.Contains(key)).ToArray();
                Assert.That(renderers.Length,Is.EqualTo(2),"Each real death gets its own original blast, rather than sharing one visual.");
                Assert.That(renderers[0].transform.position,Is.Not.EqualTo(renderers[1].transform.position));
                var frames=renderers.Select(r=>r.sprite).ToArray();double clock=c.PresentationClock;
                c.Battle.SetPaused(true);c.Tick(4);yield return null;
                Assert.That(c.PresentationClock,Is.EqualTo(clock));
                for(int i=0;i<frames.Length;i++)Assert.That(renderers[i].sprite,Is.SameAs(frames[i]));
                c.RestartBattle();yield return null;
                Assert.That(view.VisibleBlasts,Is.Zero);Assert.That(c.Battle.DeathBlasts,Is.Empty);
            }
        }
    }
}
