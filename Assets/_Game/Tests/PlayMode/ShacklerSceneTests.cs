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
    public sealed class ShacklerSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ActualCastStopsGuardThenRealSilenceButtonReleasesAndNeverCastsAgain()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/01-IndustrialTransfer.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(5);yield return null;Assert.That(c.Deploy(),Is.True);
                var u=c.Battle.Units[0];bool begin=false,loop=false,end=false;int shots=0;
                for(int i=0;i<800;i++)
                {
                    c.Battle.Advance(.05f);yield return null;var v=Object.FindObjectsByType<DemoSpriteVisual>().Single();
                    begin|=v.FrameState=="Shacklebegin";
                    if(u.Shackle==null)continue;
                    if(u.Shackle.Ticks==1)shots=c.Battle.ShotsFired;
                    if(u.Shackle.Ticks>=3){loop=v.FrameState=="Shackleloop";break;}
                }
                Assert.That(begin&&loop,Is.True);Assert.That(c.Battle.ShotsFired,Is.EqualTo(shots));Assert.That(c.Battle.DefenderShackled(c.Battle.Defender),Is.True);
                Assert.That(c.SilenceShacklers(),Is.EqualTo(1));Assert.That(c.Battle.DefenderShackled(c.Battle.Defender),Is.False);
                yield return null;end=Object.FindObjectsByType<DemoSpriteVisual>().Single().FrameState=="Shackleend";Assert.That(end,Is.True);
                for(int i=0;i<600;i++){c.Battle.Advance(.05f);yield return null;}
                Assert.That(c.Battle.Shackles.Count,Is.EqualTo(1));Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));Assert.That(c.Battle.ShotsFired,Is.GreaterThan(shots));
                c.ResetTrial(5);yield return null;Assert.That(c.Battle.Shackles,Is.Empty);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
