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
    public sealed class TauntSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator AuthoredShieldTakesActualFireFromLivingSoldier()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/GuerrillaCommand.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();Assert.That(c.Deploy("CARD_M15"),Is.True);var soldier=c.Battle.Units[0];c.Advance(3);Assert.That(c.Deploy("CARD_M18"),Is.True);var shield=c.Battle.Units.Last();bool previousTarget=false,retarget=false;
                for(int i=0;i<300;i++){c.Advance(.1f);yield return null;previousTarget|=c.Battle.Defenders.Any(d=>d.TargetId==soldier.EntityId);if(soldier.Active&&c.Battle.Defenders.All(d=>d.TargetId==shield.EntityId)){retarget=true;break;}}
                Assert.That(previousTarget&&retarget,Is.True);c.Advance(.5f);float hp=soldier.Health,shieldHP=shield.Health;c.Advance(2);yield return null;
                Assert.That(soldier.Health,Is.EqualTo(hp));Assert.That(shield.Health,Is.LessThan(shieldHP));Assert.That(c.Battle.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.TargetId==shield.EntityId),Is.True);
                Assert.That(Object.FindObjectsByType<DemoSpriteVisual>().Single(v=>v.transform.parent.name==shield.EntityId).Renderer.sprite.name,Does.StartWith("M18/"));c.ResetTrial();yield return null;Assert.That(c.Battle.Units,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
