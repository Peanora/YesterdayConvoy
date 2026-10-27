using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FirstSpeakerSceneTests
    {
        [UnityTest]public IEnumerator ActualFirstSpeakerShowsFullOriginalCastAndDistributesBubbleHitsWithoutInjection()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M36"),Is.True);
                Assert.That(c.Battle.Defenders.Count,Is.EqualTo(3));c.SelectCard(true);var receipt=c.TryDeployAt(c.Battle.Entry);Assert.That(receipt.Success,Is.True);
                var u=c.Battle.FindUnit(receipt.EntityId);bool begin=false,loop=false,end=false;
                for(int i=0;i<1800&&u.Active;i++)
                {
                    c.Tick(.05f);yield return null;
                    var v=Object.FindObjectsByType<DemoSpriteVisual>().Single();
                    begin|=v.FrameState=="Bubblebegin";loop|=v.FrameState=="Bubbleloop";end|=v.FrameState=="Bubbleend";
                    Assert.That(v.Renderer.sprite.name,Does.StartWith("M36/"));
                    if(begin&&loop&&end&&c.Battle.CorrosiveBubbles.Any(b=>b.Pulses==5))break;
                }
                Assert.That(begin&&loop&&end,Is.True);Assert.That(c.Battle.CorrosiveBubbles.SelectMany(b=>b.TargetIds).Distinct().Count(),Is.GreaterThan(1));
                Assert.That(c.Battle.CombatShots.Any(s=>s.Applied&&!s.FromDefender&&s.NeuralDamage>0),Is.True);
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(20);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.ReturnToMonsterTests();yield return null;Assert.That(c.Battle.CorrosiveBubbles,Is.Empty);Assert.That(c.ViewCount,Is.Zero);
            }
            finally{Time.timeScale=previous;}
        }
    }
}
