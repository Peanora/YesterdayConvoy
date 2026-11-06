using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class OriginalCombatVfxTests
    {
        static Type Reader=>typeof(ManualBattle).Assembly.GetType("YesterdayConvoy.OriginalCombatVfxArt");
        static Type Director=>typeof(ManualBattle).Assembly.GetType("YesterdayConvoy.CommonCombatVfxDirector");
        const BindingFlags Static=BindingFlags.NonPublic|BindingFlags.Static;
        const BindingFlags Instance=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        const string Valid="{\"version\":1,\"fps\":30,\"encoding\":\"sqrt-linear-rgb-effective-alpha\",\"clips\":[{\"key\":\"common/probe\",\"count\":3,\"width\":4,\"height\":4,\"columns\":2,\"ppu\":4,\"pivotX\":0.5,\"pivotY\":0.5,\"duration\":0.1,\"hdrRange\":16,\"loop\":false,\"directional\":true,\"forwardDegrees\":90,\"pages\":[{\"file\":\"Actor/probe.png\",\"count\":3,\"width\":8,\"height\":8}]}]}";
        static object Parse(string json)=>Reader.GetMethod("ReadPackage",Static).Invoke(null,new object[]{"OriginalCombatVfx/Monsters",json});
        static object First(object array)=>((IEnumerable)array).Cast<object>().First();
        static object Value(object o,string field)=>o.GetType().GetField(field,Instance).GetValue(o);
        [Test]public void MetadataParsingNormalizesSafeActorFoldersWithoutUploadingAnyTexture()
        {
            int before=ArtMemoryCache.TextureLoads;var clip=First(Parse(Valid));var sequence=Value(clip,"Sequence");
            Assert.That(Value(sequence,"Root"),Is.EqualTo("OriginalCombatVfx/Monsters/Actor"));
            var page=((IEnumerable)Value(sequence,"Pages")).Cast<object>().Single();Assert.That(Value(page,"file"),Is.EqualTo("probe.png"));
            Assert.That(Value(clip,"HdrRange"),Is.EqualTo(16));Assert.That(Value(clip,"ForwardDegrees"),Is.EqualTo(90));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
        }
        [Test]public void CorruptDimensionsEncodingNamespaceAndEscapingPathsAreRejected()
        {
            foreach(string json in new[]{Valid.Replace("sqrt-linear-rgb-effective-alpha","straight-alpha"),Valid.Replace("\"fps\":30","\"fps\":15"),
                Valid.Replace("Actor/probe.png","../probe.png"),Valid.Replace("\"ppu\":4","\"ppu\":0"),Valid.Replace("\"width\":8","\"width\":12"),
                Valid.Replace("common/probe","defender/probe"),Valid.Replace("\"count\":3,\"width\":8","\"count\":2,\"width\":8")})
            {var e=Assert.Throws<TargetInvocationException>(()=>Parse(json));Assert.That(e.InnerException,Is.TypeOf<InvalidDataException>());}
        }
        [Test]public void InvalidAndEndedAgesDoNotDecodeAndOneShotCannotBecomeAnUnapprovedLoop()
        {
            var clip=First(Parse(Valid));var at=clip.GetType().GetMethod("At",Instance);int before=ArtMemoryCache.TextureLoads;
            foreach(double age in new[]{-.01,.101,100,double.PositiveInfinity,double.NaN})Assert.That(at.Invoke(clip,new object[]{age,false}),Is.Null);
            var e=Assert.Throws<TargetInvocationException>(()=>at.Invoke(clip,new object[]{1d,true}));Assert.That(e.InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
        }
        [Test]public void ExistingNativeMonstersKeepTheirDeathOwnerAndViviKeepsItsShotOwner()
        {
            var owns=Director.GetMethod("OriginalMonsterOwns",Static);var duration=Director.GetMethod("EventDuration",Static);
            foreach(string card in new[]{"CARD_WISADEL_APRIL","CARD_TALULAH_TRIAL","CARD_M20","CARD_M28","CARD_M25","CARD_M32"})
                Assert.That(owns.Invoke(null,new object[]{card}),Is.EqualTo(true));
            foreach(string card in new[]{"CARD_TALULAH_TRIAL","CARD_M20","CARD_M28","CARD_M25","CARD_M32"})
                Assert.That(duration.Invoke(null,new object[]{true,card,null}),Is.EqualTo(0));
            Assert.That(owns.Invoke(null,new object[]{"CARD_M07"}),Is.EqualTo(false));
        }
        [Test]public void FirstTwelveSkillsUseProvenSharedKeysWithoutInventingKroosOrStewardAura()
        {
            var key=Director.GetMethod("BuffKey",Static);var owns=Director.GetMethod("OwnsDefenderBuff",Static);
            Assert.That(key.Invoke(null,new object[]{DefenderSkillKind.BeagleGuard}),Is.EqualTo("common/common_ignite_defense"));
            Assert.That(key.Invoke(null,new object[]{DefenderSkillKind.SpotHealingMode}),Is.EqualTo("common/common_ignite_support"));
            Assert.That(key.Invoke(null,new object[]{DefenderSkillKind.MidnightEnchant}),Is.EqualTo("common/common_ignite_attack"));
            foreach(var kind in new[]{DefenderSkillKind.KroosDoubleShot,DefenderSkillKind.StewardStrike})
            {Assert.That(key.Invoke(null,new object[]{kind}),Is.Null);Assert.That(owns.Invoke(null,new object[]{kind}),Is.EqualTo(true));}
            Assert.That(owns.Invoke(null,new object[]{DefenderSkillKind.PramanixSnow}),Is.EqualTo(false));
        }
        [Test]public void RequiredCommonKeysArePresentDistinctAndMetadataOnly()
        {
            var keys=((IEnumerable)Director.GetProperty("RequiredKeys",Static).GetValue(null)).Cast<string>().ToArray();
            Assert.That(keys.Distinct().Count(),Is.EqualTo(keys.Length));int before=ArtMemoryCache.TextureLoads;
            foreach(string key in keys)
            {
                Assert.That(key.StartsWith("common/")||key=="monster/firstar_attack_01_hit",Is.True);
                if(key=="common/common_gongjian_trail_01")continue; // Documented native TrailRenderer capture gap only.
                Assert.That(Reader.GetMethod("Get",Static).Invoke(null,new object[]{key}),Is.Not.Null,key);
            }
            foreach(var sequence in ((IEnumerable)Director.GetMethod("SequencesFor",Static).Invoke(null,null)).Cast<object>())
                Assert.That(Value(sequence,"Count"),Is.GreaterThan(0));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(before));
        }
        [Test]public void PendingUntilUsesOnlyResolvedEmittedBurstsNotAFutureQueueOrGlobalDelay()
        {
            var settings=new ManualBattleSettings("PROBE","PROBE",1,1,1,1,1,100,1,1,1,1,1);
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.forward*100);
            var pending=Director.GetMethod("PendingUntil",Static);
            Assert.That(pending.Invoke(null,new object[]{b}),Is.EqualTo(double.NegativeInfinity));
            var list=(System.Collections.Generic.List<InfectionBurst>)typeof(ManualBattle).GetField("infectionBursts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);
            var burst=new InfectionBurst{At=20,Resolved=false};list.Add(burst);
            Assert.That(pending.Invoke(null,new object[]{b}),Is.EqualTo(double.NegativeInfinity));
            burst.Resolved=true;Assert.That(pending.Invoke(null,new object[]{b}),Is.EqualTo(double.NegativeInfinity),"A future event must not extend the terminal clock.");
            burst.At=0;var clip=Reader.GetMethod("Get",Static).Invoke(null,new object[]{"common/common_firemagic_hit_01"});
            float duration=(float)clip.GetType().GetProperty("Duration",Instance).GetValue(clip);
            Assert.That((double)pending.Invoke(null,new object[]{b}),Is.EqualTo(duration).Within(.00001));
        }
        [Test]public void FailedProjectilePreservesItsAlreadyEmittedCasterTail()
        {
            var settings=new ManualBattleSettings("CARD_M12","SNOW",5,1,1,1,1,100,.1f,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:1,unitRange:5,unitInterval:3,defenderHealth:5000,defenderAttack:1,defenderRange:.01f,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,attackDuration:.6f,attackWindup:.4f,projectileTime:1);
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.right*2,combat);b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);b.Advance(.6f);
            var shot=b.CombatShots.First(x=>!x.FromDefender);typeof(ManualBattle).GetMethod("ApplyDefenderLoss",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{b.Defender,b.Defender.Health});
            b.Advance(.9f);Assert.That(shot.Resolved&&!shot.Applied,Is.True);
            var clip=Reader.GetMethod("Get",Static).Invoke(null,new object[]{"monster/snmage_attack_01_start"});float duration=(float)clip.GetType().GetProperty("Duration",Instance).GetValue(clip);
            var pending=Director.GetMethod("PendingUntil",Static);double end=(double)pending.Invoke(null,new object[]{b});
            Assert.That(end,Is.EqualTo(shot.StartedAt-.4+duration).Within(.0001));Assert.That(end,Is.GreaterThan(b.Clock));
            b.Advance(3);Assert.That((double)pending.Invoke(null,new object[]{b}),Is.LessThanOrEqualTo(b.Clock),"A completed cast cannot become a constant terminal delay.");
        }
    }
}
