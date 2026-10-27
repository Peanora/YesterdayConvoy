using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderSkillSceneTests
    {
        [UnityTest]public IEnumerator FourDefenderTrialsPresentOriginalModelsCastTheirOwnSkillsAndReset()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in ManualBattleController.DefenderBatch1Ids)
                {
                    Assert.That(c.TryEnterDefenderSkillTrial(id),Is.True);yield return null;yield return null;
                    var guard=c.Battle.FindDefender(id);var views=Object.FindAnyObjectByType<DefenderRosterView>();
                    Assert.That(views.Count,Is.EqualTo(id=="D11"?2:1));
                    var renderer=views.FindVisual(id);Assert.That(renderer.sprite.name,Does.StartWith(id));
                    Assert.That(Quaternion.Angle(renderer.transform.rotation,Camera.main.transform.rotation),Is.LessThan(.01));
                    Assert.That(renderer.transform.localScale,Is.EqualTo(Vector3.one));
                    var idle=renderer.sprite;c.Tick(.73f);yield return null;Assert.That(renderer.sprite,Is.Not.SameAs(idle));Assert.That(c.Battle.Clock,Is.Zero);
                    c.StartBattle();c.Battle.SetPaused(true);
                    var card=c.CardSlot(id=="D06"?2:0).center;Vector2 gate=Camera.main.WorldToScreenPoint(c.Battle.Entry);
                    c.ProcessPointerDown(ManualBattleController.ScreenPosition(card));c.ProcessPointerMove(gate);Assert.That(c.ProcessPointerUp(gate).Success,Is.True);
                    float sp=guard.SkillSP;c.Tick(20);Assert.That(guard.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.Clock,Is.Zero);
                    c.Battle.SetPaused(false);
                    for(int i=0;i<1000&&guard.SkillCasts==0&&guard.Alive;i++){c.Tick(.1f);if(i%50==0)yield return null;}
                    Assert.That(guard.Alive,Is.True,id);Assert.That(guard.SkillCasts,Is.GreaterThan(0),id);yield return null;
                    if(id=="D02")Assert.That(guard.SkillHealthRestored,Is.GreaterThan(0));
                    if(id=="D05")
                    {c.Tick(2);yield return null;Assert.That(c.Battle.CombatShots.Any(s=>s.SourceId==id&&s.DamageType==CombatDamageType.Arts),Is.True);}
                    if(id=="D06")
                    {c.Tick(1.3f);yield return null;Assert.That(guard.LastActionRate,Is.GreaterThan(1));}
                    if(id=="D11")
                    {c.Tick(2);yield return null;Assert.That(c.Battle.CombatShots.Any(s=>s.SourceId==id&&s.Healing&&s.Applied),Is.True);Assert.That(c.Battle.CombatShots.Any(s=>s.SourceId==id&&!s.Healing),Is.False);}
                    c.Battle.SetPaused(true);sp=guard.SkillSP;var frame=renderer.sprite;c.Tick(10);yield return null;
                    Assert.That(guard.SkillSP,Is.EqualTo(sp));Assert.That(renderer.sprite,Is.SameAs(frame));
                    c.RestartBattle();guard=c.Battle.FindDefender(id);Assert.That(guard.SkillCasts,Is.Zero);Assert.That(guard.SkillSP,Is.EqualTo(guard.Settings.Skill.InitialSP));Assert.That(c.Battle.Units,Is.Empty);
                    c.ReturnToMonsterTests();yield return null;yield return null;Assert.That(c.DefenderSkillTrialActive,Is.False);Assert.That(c.TacticalMapActive,Is.False);
                }
            }
            finally{Time.timeScale=previous;}
        }
    }
}
