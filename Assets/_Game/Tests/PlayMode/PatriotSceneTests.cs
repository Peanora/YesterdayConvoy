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
    public sealed class PatriotSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator MobilityPresetNaturallyShowsSecondPhaseOriginalWalking()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/Patriot.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();c.patriotMobilityTrial=true;c.ResetTrial();Assert.That(c.Deploy("CARD_M20"),Is.True);var u=c.Battle.Units[0];bool walk=false;
                for(int i=0;i<1200&&u.Active;i++){c.Advance(.1f);yield return null;if(u.PatriotPhase==PatriotPhase.Destruction&&u.IsMoving){var v=Object.FindObjectsByType<DemoSpriteVisual>().Single(s=>s.transform.parent.name==u.EntityId);walk=v.Renderer.sprite.name.StartsWith("M20/extra_move_2/");if(walk)break;}}
                Assert.That(walk,Is.True);Assert.That(c.Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==u.EntityId),Is.True);c.ResetTrial();yield return null;
            }
            finally{Time.timeScale=previous;}
        }
        [UnityTest]public IEnumerator AuthoredPatriotNaturallyRebirthsWithThreeOriginalClipsAndHitsHighGround()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/Patriot.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();Assert.That(c.patriotTrial,Is.True);Assert.That(c.Deploy("CARD_M20"),Is.True);var u=c.Battle.Units[0];
                bool begin=false,loop=false,end=false,second=false,hit=false;double rebornAt=double.NaN,secondAt=double.NaN;
                for(int i=0;i<1300&&u.Active;i++)
                {
                    c.Advance(.1f);yield return null;
                    var v=Object.FindObjectsByType<DemoSpriteVisual>().Single(s=>s.transform.parent.name==u.EntityId);
                    begin|=v.FrameState=="PatriotReborn1";loop|=v.FrameState=="PatriotReborn2";end|=v.FrameState=="PatriotReborn3";
                    if(u.PatriotPhase==PatriotPhase.Reborn&&double.IsNaN(rebornAt))rebornAt=u.PatriotPhaseStartedAt;
                    if(u.PatriotPhase==PatriotPhase.Destruction){second=true;secondAt=u.PatriotPhaseStartedAt;Assert.That(v.Renderer.sprite.name,Does.StartWith("M20/"));}
                    if(u.Spear?.Shot.Applied==true&&u.Spear.Shot.DamageDealt>0){hit=true;break;}
                }
                Assert.That(begin&&loop&&end&&second&&hit,Is.True,$"clips={begin}/{loop}/{end}; second={second}; hit={hit}; phase={u.PatriotPhase}; clock={c.Battle.Clock}; hp={u.Health}");
                Assert.That(secondAt-rebornAt,Is.EqualTo(60).Within(.06));Assert.That(c.Battle.FindDefender(u.Spear.TargetId).HighGround,Is.True);Assert.That(u.ImmolationDamage,Is.GreaterThan(0));
                Assert.That(c.Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==u.EntityId),Is.True);Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));
                c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Advance(20);Assert.That(c.Battle.Clock,Is.EqualTo(clock));c.ResetTrial();yield return null;Assert.That(c.Battle.Units,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
