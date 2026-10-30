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
    public sealed class NeuralSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ActualCreeperRetaliatesOnDamageAndWalksPastNeurallyStunnedGuardWithoutOrdinaryAttack()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/03-ReedCanal.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(3);yield return null;
                Assert.That(c.Deploy(),Is.True);var u=c.Battle.Units[0];bool walkingAfterBurst=false;
                for(int i=0;i<800;i++)
                {
                    c.Battle.Advance(.1f);yield return null;
                    var v=Object.FindObjectsByType<DemoSpriteVisual>().Single();
                    Assert.That(v.Renderer.sprite.name,Does.StartWith("M33/"));Assert.That(v.FrameState,Is.Not.EqualTo("Attack"));
                    walkingAfterBurst=c.Battle.NeuralBursts>0&&u.IsMoving&&c.Battle.Defender.NeuralUntil>c.Battle.Clock;
                    if(walkingAfterBurst)break;
                }
                Assert.That(walkingAfterBurst,Is.True);Assert.That(u.CreeperRetaliations,Is.GreaterThanOrEqualTo(4));
                Assert.That(c.Battle.CreeperPulses.Any(p=>p.TargetsHit>0),Is.True);Assert.That(c.Battle.CombatShots.Any(s=>!s.FromDefender),Is.False);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(30);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ResetTrial(3);yield return null;Assert.That(c.Battle.CreeperPulses,Is.Empty);Assert.That(c.Battle.NeuralBursts,Is.Zero);
                Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
