using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FifthDefenderSceneTests
    {
        [UnityTest]public IEnumerator ThreeSixStarEntriesRenderNativeSkillsPauseAndCleanUp()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in ManualBattleController.DefenderBatch5Ids)
                {
                    Assert.That(c.TryEnterDefenderSkillTrial(id),Is.True,id);yield return null;yield return null;
                    var d=c.Battle.FindDefender(id);var v=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual(id);
                    Assert.That(v.sprite.name,Does.StartWith(id));Assert.That(v.transform.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(Quaternion.Angle(v.transform.rotation,Camera.main.transform.rotation),Is.LessThan(.01));
                    var idle=v.sprite;c.Tick(.73f);yield return null;Assert.That(v.sprite,Is.Not.SameAs(idle),id);Assert.That(c.Battle.Clock,Is.Zero);
                    c.StartBattle();c.Battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(c.Battle.Entry);
                    c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(2).center));c.ProcessPointerMove(gate);Assert.That(c.ProcessPointerUp(gate).Success,Is.True,id);
                    float sp=d.SkillSP;c.Tick(20);Assert.That(d.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.Clock,Is.Zero);c.Battle.SetPaused(false);
                    bool observed=false;int extras=0;
                    for(int i=0;i<2400&&d.Alive&&!observed;i++)
                    {
                        c.Tick(.1f);
                        if(extras<4&&i>10+extras*18)
                        {
                            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.CardSlot(2).center));c.ProcessPointerMove(gate);
                            if(c.ProcessPointerUp(gate).Success)extras++;
                        }
                        observed=c.Batch5SkillObserved(id);if(i%50==0)yield return null;
                    }
                    Assert.That(observed,Is.True,id+" actual skill effect; casts="+d.SkillCasts+" burn="+d.BurnApplications+" erase="+d.ErasedProjectiles+" heal="+d.SkillHealthRestored);
                    yield return null;Assert.That(v.sprite.name,Does.Contain("/skill_"),id);
                    c.Battle.SetPaused(true);var frame=v.sprite;sp=d.SkillSP;double clock=c.Battle.Clock;c.Tick(10);yield return null;
                    Assert.That(d.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(v.sprite,Is.SameAs(frame));
                    c.RestartBattle();d=c.Battle.FindDefender(id);Assert.That(d.SkillCasts,Is.Zero);Assert.That(d.SkillSP,Is.EqualTo(d.Settings.Skill.InitialSP));Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.DefenderDrones,Is.Empty);
                    c.ReturnToMonsterTests();yield return null;yield return null;Assert.That(c.DefenderSkillTrialActive,Is.False);
                }
            }
            finally{Time.timeScale=previous;}
        }
    }
}
