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
    public sealed class FlamerazerSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator OriginalSprayAnimationsNaturalBurnChainAndPillarSurvivePauseAndRestart()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/01-IndustrialTransfer.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(3);yield return null;
                Assert.That(c.Deploy(),Is.True);bool start=false,loop=false,end=false;
                for(int i=0;i<600;i++)
                {
                    c.Battle.Advance(.1f);if(i==10||i==20)Assert.That(c.Deploy(),Is.True);
                    yield return null;
                    foreach(var visual in Object.FindObjectsByType<DemoSpriteVisual>())
                    {
                        start|=visual.FrameState=="Spraystart";loop|=visual.FrameState=="Sprayloop";end|=visual.FrameState=="Sprayend";
                        if(visual.FrameState.StartsWith("Spray"))Assert.That(visual.GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
                    }
                    if(c.Battle.FuelExplosions.Count(e=>e.Resolved)>=3)break;
                }
                Assert.That(start&&loop&&end,Is.True);Assert.That(c.Battle.BurnBursts,Is.GreaterThan(0));
                Assert.That(c.Battle.FuelExplosions.Count(e=>e.Resolved),Is.EqualTo(3));
                Assert.That(c.Battle.FuelExplosions.Sum(e=>e.ChainHits),Is.GreaterThan(0));
                Assert.That(c.Battle.Facilities.Any(f=>f.Collapsed),Is.True);Assert.That(c.Battle.RubbleTiles,Is.Not.Empty);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(20);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ResetTrial(3);yield return null;Assert.That(c.Battle.FuelExplosions,Is.Empty);Assert.That(c.Battle.FlameSprays,Is.Empty);
                Assert.That(c.Battle.Facilities.Any(f=>f.Collapsed),Is.False);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
