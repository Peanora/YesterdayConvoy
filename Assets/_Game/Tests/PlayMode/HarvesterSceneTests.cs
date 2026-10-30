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
    public sealed class HarvesterSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator ActualHarvesterUsesWakeAndCombatArtDrainsHealthAndAppliesNeuralThroughAuraAndMelee()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Tests/Fixtures/03-ReedCanal.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<FacilityTrialController>();c.ResetTrial(4);yield return null;
                Assert.That(c.Deploy(),Is.True);var u=c.Battle.Units[0];bool wake=false,walk=false,idle=false,attack=false;
                for(int i=0;i<600;i++)
                {
                    c.Battle.Advance(.1f);yield return null;
                    var v=Object.FindObjectsByType<DemoSpriteVisual>().Single();
                    wake|=v.FrameState=="HarvesterWake";walk|=v.FrameState=="HarvesterWalk";idle|=v.FrameState=="HarvesterIdle";attack|=v.FrameState=="Attack";
                    Assert.That(v.Renderer.sprite.name,Does.StartWith("M34/"));
                    if(c.Battle.NeuralBursts>0&&u.IsMoving)break;
                }
                Assert.That(wake&&walk&&idle&&attack,Is.True);Assert.That(u.HarvesterAuraTicks,Is.GreaterThan(0));Assert.That(u.HarvesterHealthLost,Is.GreaterThan(0));
                Assert.That(c.Battle.NeuralBursts,Is.GreaterThan(0));Assert.That(c.Battle.CombatShots.Any(s=>s.Applied&&!s.FromDefender&&s.NeuralDamage>0),Is.True);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Battle.Advance(30);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ResetTrial(4);yield return null;Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.NeuralBursts,Is.Zero);Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(),Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
