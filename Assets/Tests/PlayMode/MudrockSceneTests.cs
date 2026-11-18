using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MudrockSceneTests
    {
        [UnityTest]public IEnumerator BoardShieldClipsAndPremultipliedShaderRemainAvailable()
        {
            yield return null;
            foreach(string key in new[]{"shield1_ground","shield1_over","shieldBreak_ground","shieldBreak_over","shieldRefresh_over","attackStart_over"})
            {
                var clip=MudrockVfxArt.Load(key);
                Assert.That(clip,Is.Not.Null,key);
                Assert.That(clip.Count,Is.GreaterThan(1),key);
                Assert.That(clip.At(0),Is.Not.Null,key);
            }
            Assert.That(Shader.Find("Yesterday Convoy/Mudrock Original Baked"),Is.Not.Null);
        }
    }
}
