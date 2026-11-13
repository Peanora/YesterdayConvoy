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

        [TestCase("D21")][TestCase("D22")][TestCase("D27")]
        [TestCase("D28")][TestCase("D29")][TestCase("D30")]
        public void AddedClipsKeepNativeBodyScaleAndFootOrigin(string id)
        {
            foreach(string suffix in new[]{"","B"})
            {
                string role=id+suffix;
                var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("DefenderArt/"+role+"/manifest.json")));
                var idle=Array.Find(data.clips,c=>c.key=="idle");var art=DefenderArt.Load(role);
                Assert.That(art.StartDuration,Is.GreaterThan(0));
                foreach(var clip in data.clips)if(clip.pixelsPerUnit>0)
                {
                    Assert.That(clip.scale/clip.pixelsPerUnit,Is.EqualTo(idle.scale/data.ppu).Within(.000001),role+"/"+clip.key+" must preserve source-pixel world scale");
                    var sprite=art[clip.key].Frames[0];
                    Assert.That(sprite.pixelsPerUnit,Is.EqualTo(clip.pixelsPerUnit).Within(.001));
                    Assert.That(sprite.bounds.min.x+sprite.pivot.x/sprite.pixelsPerUnit,Is.EqualTo(0).Within(.00001));
                    Assert.That(sprite.bounds.min.y+sprite.pivot.y/sprite.pixelsPerUnit,Is.EqualTo(0).Within(.00001));
                }
                Assert.That(art.StartAt(art.StartDuration+.01),Is.Null,"Start must return control to idle without looping.");
            }
        }

        [Test] public void NativeAttackVariantsCycleWithoutChangingActionWindow()
        {
            foreach(string suffix in new[]{"","B"})
            {
                var snow=DefenderArt.Load("D27"+suffix);Assert.That(snow.SkillAttackVariants,Is.EqualTo(3));
                foreach(int i in new[]{0,1,2,3})
                {
                    string key=i%3==0?"skill_attack":i%3==1?"skill_attack_b":"skill_attack_c";
                    Assert.That(snow.AdvancedPose(5,10,-1,true,.5f,2,i).name,Does.Contain("/"+key+"/"));
                }
                var wang=DefenderArt.Load("D28"+suffix);float duration=wang.ActionDuration;
                Assert.That(wang.AttackVariants,Is.EqualTo(4));
                for(int i=0;i<4;i++)
                {
                    string key=new[]{"attack","attack_b","attack_2_a","attack_2_b"}[i];
                    Assert.That(wang.ActionAt(duration*.5,i).name,Does.Contain("/"+key+"/"));
                    Assert.That(wang.ActionDuration,Is.EqualTo(duration));
                }
                Assert.That(wang.IdleAt(1,2).name,Does.Contain("/idle_2/"));
            }
            Assert.That(DefenderArt.Load("D30").IdleAt(10.2).name,Does.Contain("/idle_2/"));
            Assert.That(DefenderArt.Load("D29B").AdvancedPose(5,10,-1,false,0,1).name,Does.Contain("/idle/"),"Missing native rear S3 must not fabricate a front-facing rear pose.");
        }

        [Test] public void OriginalChessPieceHasFiniteTileScaleAndRealPhases()
        {
            var piece=DefenderArt.Load("D28Chess");
            Assert.That(piece.Portrait,Is.Null);Assert.That(piece.StartDuration,Is.GreaterThan(.3));
            foreach(string key in new[]{"start","idle","attack"})Assert.That(piece[key].Frames.Length,Is.GreaterThan(20));
            Assert.That(piece["idle"].Frames[0].bounds.size.x,Is.InRange(.3f,1f));
        }

        [TestCase(1f)][TestCase(2f)][TestCase(.7f)]
        public void Mon3trClawKeepsItsNativeRecoveryInsteadOfTheNormalHealingWindow(float rate)
        {
            var art=DefenderArt.Load("D23");
            double duration=art.ActionDurationFor(true,2.833333,rate),contact=(2.0/3)/rate;
            Assert.That(duration,Is.EqualTo(1.5/rate).Within(.000001));
            Assert.That(art.ActionDurationFor(false,2.833333,rate),Is.EqualTo(2.833333/rate).Within(.000001));
            // Check the recovery pose well after the claw connects. With the old
            // ordinary-action window this source phase was only about 0.92 sec.
            double age=1.117/rate;
            var pose=art.AdvancedPose(5,10,-1,true,age,duration,0,false,contact);
            Assert.That(pose,Is.SameAs(art["skill_attack"].At(1.117,false)));
            Assert.That(duration,Is.LessThan(art.ActionDurationFor(false,2.833333,rate)));
        }

        [Test] public void ReloadAfterUnityDestroysCachedSpritesReturnsUsableResources()
        {
            var previous=DefenderArt.Load("D28Chess");
            UnityEngine.Object.DestroyImmediate(previous["idle"].Frames[0]);
            var reloaded=DefenderArt.Load("D28Chess");
            Assert.That(reloaded,Is.Not.SameAs(previous));
            Assert.That(reloaded["idle"].Frames[0],Is.Not.Null);
            Assert.That(reloaded["idle"].Frames[0].texture,Is.Not.Null);
        }
    }
}
