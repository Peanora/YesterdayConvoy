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
    public sealed class FlamechaserSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ActualM30UsesEmberAndReviveArtAndCanBeEliminatedByRevealingDefender()
        {
            float previous=Time.timeScale;
            try
            {
                foreach(bool reveal in new[]{false,true})
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/FacilityTrials/03-ReedCanal.unity",new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;Time.timeScale=0;
                    var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(1);c.SetReveal(reveal);yield return null;
                    Assert.That(c.Deploy(),Is.True);var u=c.Battle.Units[0];bool ember=false,revive=false,death=false;
                    for(int i=0;i<700;i++)
                    {
                        c.Battle.Advance(.1f);yield return null;
                        var visual=Object.FindObjectsByType<DemoSpriteVisual>().FirstOrDefault(v=>v.transform.parent.name==u.EntityId);
                        if(visual!=null)
                        {
                            ember|=visual.FrameState=="Ember";
                            revive|=visual.FrameState=="EmberRevive";
                            death|=visual.FrameState=="EmberDeath";
                        }
                        if(reveal?u.Resolution==UnitResolution.Dead&&death:u.EmberRevivals>0)break;
                    }
                    Assert.That(ember,Is.True);Assert.That(u.EmberTransformations,Is.GreaterThan(0));Assert.That(c.Battle.Ignitions,Is.GreaterThan(0));
                    Assert.That(c.Battle.CombatShots.Any(s=>s.Applied&&s.BurnDamage>0),Is.True);
                    if(reveal){Assert.That(death,Is.True);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(u.EmberRevivals,Is.Zero);}
                    else{Assert.That(revive,Is.True);Assert.That(u.EmberRevivals,Is.GreaterThan(0));Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Soldier));}
                    c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(20);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                    c.ResetTrial(1);yield return null;Assert.That(c.Battle.Units,Is.Empty);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
                    Assert.That(c.Battle.Facilities.Where(f=>f.Settings.Kind==FacilityKind.Reeds).Count(f=>f.Burning),Is.EqualTo(1));
                    Assert.That(c.Battle.Ignitions,Is.EqualTo(1));
                }
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
