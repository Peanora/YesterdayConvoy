using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class OriginalEightSkillSceneTests
    {
        [UnityTest]public IEnumerator EightNativeModelsCastActualSkillsPauseAndResetThroughIndividualEntries()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in ManualBattleController.OriginalEightIds)
                {
                    Assert.That(c.TryEnterDefenderSkillTrial(id),Is.True,id);yield return null;yield return null;
                    var d=c.Battle.FindDefender(id);var v=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual(id);
                    Assert.That(v.sprite.name,Does.StartWith(id));Assert.That(v.transform.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(Quaternion.Angle(v.transform.rotation,Camera.main.transform.rotation),Is.LessThan(.01));
                    var idle=v.sprite;c.Tick(.73f);yield return null;Assert.That(v.sprite,Is.Not.SameAs(idle),id);Assert.That(c.Battle.Clock,Is.Zero);
                    c.StartBattle();c.Battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(c.Battle.Entry);
                    c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(2).center));c.ProcessPointerMove(gate);Assert.That(c.ProcessPointerUp(gate).Success,Is.True,id);
                    float sp=d.SkillSP;c.Tick(20);Assert.That(d.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.Clock,Is.Zero);c.Battle.SetPaused(false);
                    bool observed=false;
                    for(int i=0;i<1300&&d.Alive&&!observed;i++){c.Tick(.1f);observed=c.OriginalEightSkillObserved(id);if(i%50==0)yield return null;}
                    Assert.That(observed,Is.True,id+" actual skill effect");yield return null;
                    c.Battle.SetPaused(true);var frame=v.sprite;sp=d.SkillSP;double clock=c.Battle.Clock;c.Tick(10);yield return null;
                    Assert.That(d.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(v.sprite,Is.SameAs(frame));
                    c.RestartBattle();d=c.Battle.FindDefender(id);Assert.That(d.SkillCasts,Is.Zero);Assert.That(d.SkillSP,Is.EqualTo(d.Settings.Skill.InitialSP));Assert.That(c.Battle.Units,Is.Empty);
                    c.ReturnToMonsterTests();yield return null;yield return null;Assert.That(c.DefenderSkillTrialActive,Is.False);
                }
            }
            finally{Time.timeScale=previous;}
        }
    }
}
