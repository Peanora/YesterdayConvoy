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
    public sealed class TacticalCommandSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator DifferentAuthoredRolesShareRealStackingOrdersAndNaturalDeathsRemoveThem()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/GuerrillaCommand.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();Assert.That(c.Deploy("CARD_M15"),Is.True);c.Advance(.8f);yield return null;
                Assert.That(c.Deploy("CARD_M16"),Is.True);c.Advance(.8f);yield return null;Assert.That(c.Deploy("CARD_M17"),Is.True);c.Advance(.8f);yield return null;Assert.That(c.Deploy("CARD_M17"),Is.True);yield return null;
                Assert.That(c.Battle.TacticalCommanders,Is.EqualTo(2));Assert.That(c.Battle.Units.Select(u=>u.UnitId).Distinct().Count(),Is.EqualTo(3));
                foreach(var u in c.Battle.Units)
                {
                    var v=Object.FindObjectsByType<DemoSpriteVisual>().Single(v=>v.transform.parent.name==u.EntityId);
                    Assert.That(v.Renderer.sprite.name,Does.StartWith(u.CardId.Substring(5)+"/"));
                    Assert.That(c.Battle.UnitAttackMultiplier(u),Is.EqualTo(1.2f).Within(.001));
                }
                for(int i=0;i<1400&&c.Battle.TacticalCommanders>0;i++){c.Advance(.1f);yield return null;}
                Assert.That(c.Battle.TacticalCommanders,Is.Zero);
                Assert.That(c.Battle.Units.Where(u=>u.CardId=="CARD_M17").All(u=>u.Resolution==UnitResolution.Dead&&c.Battle.CombatShots.Any(s=>s.Applied&&s.FromDefender&&s.TargetId==u.EntityId)),Is.True);
                Assert.That(c.Battle.TacticalAttackMultiplier,Is.EqualTo(1));Assert.That(c.Battle.TacticalArmorBonus,Is.Zero);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Advance(10);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ResetTrial();yield return null;Assert.That(c.Battle.Units,Is.Empty);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
