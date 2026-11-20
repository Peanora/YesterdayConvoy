using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FieldAnimationTests
    {
        [Serializable] sealed class Manifest { public float ppu; public Clip[] clips; }
        [Serializable] sealed class Clip { public string key; public float scale,pixelsPerUnit; }

        [TestCase("D29")][TestCase("D30")]
        public void AddedBossClipsKeepNativeBodyScaleAndFootOrigin(string id)
        {
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("DefenderArt/"+id+"/manifest.json")));
            var idle=Array.Find(data.clips,clip=>clip.key=="idle");
            var art=DefenderArt.Load(id);
            Assert.That(art.StartDuration,Is.GreaterThan(0));
            foreach(var clip in data.clips)
            {
                if(clip.pixelsPerUnit<=0)continue;
                Assert.That(clip.scale/clip.pixelsPerUnit,Is.EqualTo(idle.scale/data.ppu).Within(.000001),id+"/"+clip.key);
                var sprite=art[clip.key].Frames[0];
                Assert.That(sprite.pixelsPerUnit,Is.EqualTo(clip.pixelsPerUnit).Within(.001));
                Assert.That(sprite.bounds.min.x+sprite.pivot.x/sprite.pixelsPerUnit,Is.EqualTo(0).Within(.00001));
                Assert.That(sprite.bounds.min.y+sprite.pivot.y/sprite.pixelsPerUnit,Is.EqualTo(0).Within(.00001));
            }
            Assert.That(art.StartAt(art.StartDuration+.01),Is.Null,"Start must return control to idle without looping.");
        }

        [Test] public void HoshigumaSkillAttackVariantsCycleWithoutChangingActionWindow()
        {
            var art=DefenderArt.Load("D25");
            var keys=new[]{"skill_attack","skill_attack_b","skill_attack_c"};
            Assert.That(art.SkillAttackVariants,Is.EqualTo(keys.Length));
            for(int i=0;i<=keys.Length;i++)
                Assert.That(art.AdvancedPose(5,10,-1,true,.5f,2,i).name,Does.Contain("/"+keys[i%keys.Length]+"/"));
        }

        [Test] public void MlynarHasItsAlternateIdleAndNoFabricatedOrdinaryAttack()
        {
            var art=DefenderArt.Load("D30");
            Assert.That(art.IdleAt(10.2).name,Does.Contain("/idle_2/"));
            Assert.That(art.ActionDuration,Is.Zero);
        }

        [Test] public void ReloadAfterUnityDestroysCachedSpritesReturnsUsableResources()
        {
            var previous=DefenderArt.Load("D01");
            UnityEngine.Object.DestroyImmediate(previous["idle"].Frames[0]);
            var reloaded=DefenderArt.Load("D01");
            Assert.That(reloaded,Is.Not.SameAs(previous));
            Assert.That(reloaded["idle"].Frames[0],Is.Not.Null);
            Assert.That(reloaded["idle"].Frames[0].texture,Is.Not.Null);
        }
    }
}
