using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class PatriotShopPresentationTests
    {
        float scale;
        [SetUp]public void Save()=>scale=Time.timeScale;
        [TearDown]public void Restore()=>Time.timeScale=scale;
        [UnityTest]public IEnumerator FiveCostFullBenchMergeDeployAndMixedNativePhases()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");ManualBattleController c=null;
            for(int i=0;i<1200;i++){c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading)break;yield return null;}
            Assert.That(c!=null&&c.SynergySandbox&&!c.ArtLoading);yield return c.VerifyPatriotShop(false);Assert.That(c.PatriotShopVerificationComplete);Assert.That(c.PatriotShopVerificationPassed);
        }
    }
}
