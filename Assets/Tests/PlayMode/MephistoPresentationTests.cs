using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class MephistoPresentationTests
    {
        float scale;
        [SetUp]public void Save()=>scale=Time.timeScale;
        [TearDown]public void Restore()=>Time.timeScale=scale;
        [UnityTest]public IEnumerator FiveCostMergeDeployHealingPixelsPauseFacingAndClear()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");ManualBattleController c=null;
            for(int i=0;i<1200;i++){c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading)break;yield return null;}
            Assert.That(c!=null&&c.SynergySandbox&&!c.ArtLoading);yield return c.VerifyMephisto(false);Assert.That(c.MephistoVerificationComplete);Assert.That(c.MephistoVerificationPassed);
        }
        [UnityTest]public IEnumerator NativeFramesAndMaterialSupportTargetBackend()
        {
            yield return null;
            foreach(string key in new[]{"attack_01_start","attack_01_hit","buff_01_start","buff_01_end"})
            {var c=MephistoVfxArt.Load(key);Assert.That(c.Count,Is.GreaterThan(30));Assert.That(c.At(0),Is.Not.Null);Assert.That(c.At(-1),Is.Null);Assert.That(c.At(c.Duration),Is.Null);Assert.That(c.At(c.Duration,true),Is.SameAs(c.At(0)));}
            Assert.That(Resources.Load<Material>("MudrockVfx/OriginalEffect").shader.isSupported);
        }
    }
}
