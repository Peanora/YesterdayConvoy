using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Editor.Tests
{
    public sealed class BossRosterFxTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const string Valid="{\"version\":1,\"task\":\"B22\",\"fps\":30,\"encoding\":\"sqrt-linear-rgb-effective-alpha\",\"clips\":[{\"key\":\"probe\",\"count\":3,\"width\":4,\"height\":4,\"columns\":2,\"ppu\":4,\"pivotX\":0.5,\"pivotY\":0.5,\"duration\":0.1,\"hdrRange\":16,\"sourceStart\":0.5,\"loop\":false,\"pages\":[{\"file\":\"probe/00.png\",\"count\":3,\"width\":8,\"height\":8}]}],\"bindings\":[],\"preload\":[{\"phase\":0,\"keys\":[\"probe\"]}]}";
        static object Parse(string json)=>typeof(BossRosterFxArt).GetMethod("Parse",Static).Invoke(null,new object[]{"B22",json,null});
        [Test]public void PlayerEmbeddedBindingsAndSourceWarmupParseWithoutTextureDecode()
        {
            int loads=ArtMemoryCache.TextureLoads;var package=Parse(Valid);
            var clips=(IDictionary)package.GetType().GetField("Clips",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(package);
            var clip=(BossRosterFxArt.Clip)clips["probe"];Assert.That(clip.SourceStart,Is.EqualTo(.5));Assert.That(clip.Duration,Is.EqualTo(.6f).Within(.00001));
            Assert.That(clip.At(.49),Is.Null);Assert.That(clip.At(.61),Is.Null);Assert.Throws<InvalidOperationException>(()=>clip.At(1,true));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }
        [Test]public void CorruptSourceNamespaceEscapesAndUnapprovedLoopsAreRejected()
        {
            foreach(var json in new[]{Valid.Replace("probe/00.png","../00.png"),Valid.Replace("\"ppu\":4","\"ppu\":0"),Valid.Replace("\"width\":8","\"width\":12"),Valid.Replace("\"count\":3,\"width\":8","\"count\":2,\"width\":8"),Valid.Replace("\"task\":\"B22\"","\"task\":\"B23\""),Valid.Replace("\"bindings\":[]","\"bindings\":[{\"kind\":\"SkillStart\",\"key\":\"probe\",\"attach\":\"owner\",\"loop\":true}]")})
            {var error=Assert.Throws<TargetInvocationException>(()=>Parse(json));Assert.That(error.InnerException,Is.TypeOf<InvalidDataException>());}
        }
        [Test]public void FacingAliasesKeepIndexedAttacksDistinct()
        {
            Assert.That(BossRosterEffectView.AnimationMatches("Attack","A_Attack"),Is.True);
            Assert.That(BossRosterEffectView.AnimationMatches("Attack","Attack_b"),Is.True);
            Assert.That(BossRosterEffectView.AnimationMatches("A_Attack_1","B_Attack_1"),Is.True);
            Assert.That(BossRosterEffectView.AnimationMatches("Attack_1","Attack_2"),Is.False);
            Assert.That(BossRosterEffectView.AnimationMatches("Combat","Attack_a"),Is.False);
        }
        [Test]public void SelectedCorePreparationReusesOriginalEnergyAndExactAuxiliaryIdentities()
        {
            int loads=ArtMemoryCache.TextureLoads;
            var sequences=BossRosterFxArt.SequencesFor(new[]{"CARD_TALULAH_TRIAL","CARD_WISADEL_APRIL","CARD_B14","CARD_B23","CARD_M14"}).ToArray();
            Assert.That(sequences.Any(s=>s.Root=="TalulahUnboundVfx"&&s.Key=="orb-body-idle"),Is.True);
            Assert.That(sequences.Any(s=>s.Root.StartsWith("BossRoster/P14H/",StringComparison.Ordinal)),Is.True);
            Assert.That(sequences.Any(s=>s.Root.StartsWith("BossRoster/P20/",StringComparison.Ordinal)),Is.True);
            Assert.That(sequences.Any(s=>s.Root==BossRosterCannonArt.Root),Is.True);
            Assert.That(sequences.Select(s=>s.CacheKey).Distinct().Count(),Is.EqualTo(sequences.Length));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }
        [Test]public void OriginalProjectilesKeepTheirActualFlightAcrossSourceDeathAndSkillEnd()
        {
            var settings=new ManualBattleSettings("PROBE","PROBE",1,1,1,1,1,100,1,1,1,1,1);
            var battle=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.forward*100);var root=new GameObject("FX lifecycle test");
            try
            {
                var view=root.AddComponent<BossRosterEffectView>();view.Bind(()=>battle);
                void Event(long id,string kind,double at)=>view.Receive(id,7,battle.RunId,"source","CARD_B01",kind,"attack","Attack","Attack",1,0,at,10,Vector3.zero,Vector3.right*10,"target",false,false);
                Event(1,"ProjectileSpawn",0);Assert.That(view.ActiveEffects,Is.GreaterThan(0));Assert.That(view.PendingUntil,Is.EqualTo(10));
                Event(2,"SkillEnd",1);Assert.That(view.PendingUntil,Is.EqualTo(10));Event(3,"Death",2);Assert.That(view.PendingUntil,Is.EqualTo(10));
                int count=view.ActiveEffects;Event(3,"Death",2);Assert.That(view.ActiveEffects,Is.EqualTo(count));
                view.Clear();Assert.That(view.ActiveEffects,Is.Zero);Assert.That(double.IsNegativeInfinity(view.PendingUntil),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
