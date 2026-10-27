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
    public sealed class FacilitySceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ThreeAuthoredMapsUseRealRoleAnimationsAndNaturalFacilityEffects()
        {
            float previous=Time.timeScale;
            try
            {
                var names=new[]{"01-IndustrialTransfer","02-SnowRelay","03-ReedCanal","03-ReedCanal"};
                for(int n=0;n<4;n++)
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/"+names[n]+".unity",new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;Time.timeScale=0;
                    var c=Object.FindAnyObjectByType<FacilityTrialController>();Assert.That(c,Is.Not.Null);
                    if(n==3)c.ResetTrial(1);Assert.That(c.Deploy(),Is.True);
                    bool effect=false;
                    for(int i=0;i<1800&&!effect;i++)
                    {
                        c.Battle.Advance(.1f);
                        effect=n==0?c.Battle.Units.Any(u=>u.AltarEmpowered):n==1?c.Battle.SealedTileCount>0:n==2?
                            c.Battle.AttackerGramophoneHits>0:c.Battle.CombatShots.Any(s=>s.Applied&&s.BurnDamage>0);
                        if(i%20==0)yield return null;
                    }
                    yield return null;
                    Assert.That(effect,Is.True,"Natural facility effect: "+n);
                    Assert.That(Object.FindObjectsByType<DemoSpriteVisual>().Any(v=>v.Renderer!=null&&v.Renderer.sprite!=null),Is.True);
                    c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(30);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                    c.ResetTrial(c.SelectedRole);yield return null;
                    Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.SealedTileCount,Is.Zero);
                    Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
                }
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
