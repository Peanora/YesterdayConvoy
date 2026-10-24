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
    public sealed class OrderRunnerSceneTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator AuthoredSoldierActuallyTravelsFasterAfterRealHeraldDeployment()
        {
            float previous=Time.timeScale;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/GuerrillaTrials/GuerrillaCommand.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<GuerrillaTrialController>();Assert.That(c.Deploy("CARD_M15"),Is.True);var u=c.Battle.Units[0];c.Advance(1);yield return null;
                double distance=u.Motion.DistanceTravelled;c.Advance(.5f);yield return null;Assert.That(u.Motion.DistanceTravelled-distance,Is.EqualTo(.4).Within(.001));
                Assert.That(c.Deploy("CARD_M17"),Is.True);distance=u.Motion.DistanceTravelled;c.Advance(.5f);yield return null;
                Assert.That(u.Motion.DistanceTravelled-distance,Is.EqualTo(.55).Within(.001));
                var v=Object.FindObjectsByType<DemoSpriteVisual>().Single(v=>v.transform.parent.name==u.EntityId);Assert.That(v.Renderer.sprite.name,Does.StartWith("M15/walk/"));
                c.Battle.SetPaused(true);distance=u.Motion.DistanceTravelled;c.Advance(20);Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(distance));c.ResetTrial();yield return null;Assert.That(c.Battle.Units,Is.Empty);
            }
            finally{Time.timeScale=previous;}
        }
#endif
    }
}
