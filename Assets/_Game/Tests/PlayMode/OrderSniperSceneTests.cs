using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace YesterdayConvoy.Tests
{
    public sealed class OrderSniperSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator AuthoredSniperChangesFromSingleToTwoActualTargetsAfterRealCommand()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/GuerrillaCommand.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();Assert.That(c.Deploy("CARD_M16"),Is.True);var u=c.Battle.Units[0];
                for(int i=0;i<500&&u.OrdinaryAttacksFired==0;i++){c.Advance(.1f);yield return null;}
                Assert.That(c.Battle.CombatShots.Count(s=>!s.FromDefender&&s.SourceId==u.EntityId),Is.EqualTo(1));Assert.That(c.Deploy("CARD_M17"),Is.True);
                for(int i=0;i<80;i++){c.Advance(.05f);yield return null;}
                var pair=c.Battle.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==u.EntityId).GroupBy(s=>s.StartedAt).FirstOrDefault(g=>g.Count()==2);
                Assert.That(pair,Is.Not.Null);Assert.That(pair.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(2));Assert.That(pair.All(s=>s.Applied&&s.DamageDealt>0),Is.True);
                Assert.That(Object.FindObjectsByType<DemoSpriteVisual>().Single(v=>v.transform.parent.name==u.EntityId).Renderer.sprite.name,Does.StartWith("M16/"));
                c.ResetTrial();yield return null;Assert.That(c.Battle.Units,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
