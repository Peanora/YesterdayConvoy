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
    public sealed class MandragoraSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ActualM32GazesBreaksShieldRebirthsAndCommandsPillarWithOriginalArt()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/FacilityTrials/01-IndustrialTransfer.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(4);yield return null;
                Assert.That(c.Deploy(),Is.True);var u=c.Battle.Units[0];bool toppled=false,gaze=false,reborn=false,airborne=false,command=false;
                for(int i=0;i<1600;i++)
                {
                    c.Battle.Advance(.1f);yield return null;
                    foreach(var v in Object.FindObjectsByType<DemoSpriteVisual>())
                    {
                        gaze|=v.FrameState=="Gazeloop";reborn|=v.FrameState=="MandraReborn";command|=v.FrameState=="PillarCommand";
                        Assert.That(v.Renderer.sprite,Is.Not.Null);
                    }
                    if(!toppled&&u.Gaze!=null&&u.Gaze.Ticks>0)toppled=c.ToppleMandragoraPillar();
                    airborne|=c.Battle.UnitAirborne(u);
                    if(c.Battle.PillarCommands.Any(p=>p.Applied))break;
                }
                Assert.That(toppled&&gaze&&reborn&&airborne&&command,Is.True);
                Assert.That(u.StoneShieldBreaks,Is.GreaterThan(0));Assert.That(u.StoneAuraHits,Is.GreaterThan(0));
                Assert.That(c.Battle.PillarCommands.Any(p=>p.Applied),Is.True);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(30);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ResetTrial(4);yield return null;Assert.That(c.Battle.Gazes,Is.Empty);Assert.That(c.Battle.PillarCommands,Is.Empty);
                Assert.That(c.Battle.Facilities.Any(f=>f.Collapsed),Is.False);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
