using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class MandragoraPresentationTests
    {
        float scale;
        [SetUp]public void Save()=>scale=Time.timeScale;
        [TearDown]public void Restore()=>Time.timeScale=scale;
        [UnityTest]public IEnumerator ShopMergeDeploymentNativePixelsPauseFacingAndReset()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");ManualBattleController c=null;
            for(int i=0;i<1200;i++){c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading)break;yield return null;}
            Assert.That(c!=null&&c.SynergySandbox&&!c.ArtLoading,"Sandbox must finish its actual loading path");
            yield return c.VerifyMandragora(false);Assert.That(c.MandragoraVerificationComplete);Assert.That(c.MandragoraVerificationPassed);c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator OriginalAtlasFramesRetainTheirPremultipliedMaterialAndTiming()
        {
            yield return null;
            foreach(string key in new[]{"c1_skill_01_beam","c1_attack_hit","c2_attack_01_trail_01","c2_skill_02_begin_01","reborn_start_01","stonepillar_collapse_01_left","warning_head_01","block_01"})
            {var clip=MandragoraVfxArt.Load(key);Assert.That(clip.Count,Is.GreaterThan(30),key);Assert.That(clip.At(0),Is.Not.Null);Assert.That(clip.At(-1),Is.Null);Assert.That(clip.At(clip.Duration),Is.Null);Assert.That(clip.At(clip.Duration,true),Is.SameAs(clip.At(0)));}
            Assert.That(Resources.Load<Material>("MudrockVfx/OriginalEffect").shader.isSupported);
        }
    }
}
