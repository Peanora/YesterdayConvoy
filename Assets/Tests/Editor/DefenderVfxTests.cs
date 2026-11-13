using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderVfxTests
    {
        [Serializable] sealed class Manifest {public int version,fps;public string encoding;public Data[] clips;}
        [Serializable] sealed class Data {public string key,sourcePrefab;public int count,width,height;public float duration,ppu;public bool loop;public Page[] pages;}
        [Serializable] sealed class Page {public string file;public int count,width,height;}
        [Serializable] sealed class Identity {public string characterId,name;}
        static readonly HashSet<string> NativeArtIds=new HashSet<string>(new[]{"D21","D22","D23","D24","D25","D26","D28","D29","D30","D32","D33","D34","D35"},StringComparer.Ordinal);
        static Manifest Read()=>JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("OriginalCombatVfx/Defenders/manifest.json")));
        static Transform NativeNode(Transform root,int[] indices)
        {
            foreach(int index in indices??Array.Empty<int>())
            {Assert.That(index,Is.InRange(0,root.childCount-1),root.name);root=root.GetChild(index);}
            return root;
        }
        static void Finite(float value,string label)=>Assert.That(!float.IsNaN(value)&&!float.IsInfinity(value),Is.True,label);
        [Test] public void RequestedFireChenIsDawnstreakWithSwordSkillAndHerOwnFrontAndBackSources()
        {
            var trial=Resources.Load<DefenderTrialDefinition>("DefenderTrials/D32");
            Assert.That(trial,Is.Not.Null);
            var guard=trial.defenders.Single(d=>d.id=="D32");
            Assert.That(guard.displayName,Is.EqualTo("赤刃明霄陈"));
            Assert.That(guard.skill.kind,Is.EqualTo(DefenderSkillKind.ChenDawnstreak));
            Assert.That(guard.blocks,Is.EqualTo(1));
            foreach(string variant in new[]{"D32","D32B"})
            {
                var identity=JsonUtility.FromJson<Identity>(File.ReadAllText(ArtSourcePaths.Resolve("DefenderArt/"+variant+"/sources.json")));
                Assert.That(identity.characterId,Is.EqualTo("char_1050_chen3"),variant+" cannot silently resolve to the summer Chen model.");
                Assert.That(identity.name,Is.EqualTo("赤刃明霄陈"));
                Assert.That(DefenderArt.Load(variant).DurationOf("skill_attack"),Is.GreaterThan(1));
            }
            var keys=DefenderVfxArt.RequiredKeysForArtIds(new[]{"D32"}).ToArray();
            Assert.That(keys,Is.Not.Empty);
            Assert.That(keys.All(k=>k.StartsWith("defender/D32/chen3_",StringComparison.Ordinal)),Is.True,"D32 must not preload the misidentified chen2 water/slime family.");
        }
        [Test] public void ApprovedSnowPreloadsAttackFieldsAndRealLayerStates()
        {
            Assert.That(DefenderVfxArt.RequiredKeysForArtIds(new[]{"D27","D27"}),Is.EquivalentTo(new[]{
                "defender/D27/sbell2_skill_03_start_01","defender/D27/sbell2_skill_03_hit",
                "defender/D27/sbell2_skill_03_buff_01","defender/D27/sbell2_skill_03_range","defender/D27/sbell2_skill_03_flow",
                "defender/D27/sbell2_ground_01","defender/D27/sbell2_ground_02","defender/D27/sbell2_ground_03","defender/D27/sbell2_buff_01_hit_01"}));
            var data=Read();
            foreach(var c in data.clips.Where(c=>c.key.StartsWith("defender/D27/sbell2_skill_03_start_01",StringComparison.Ordinal)))
                Assert.That(c.loop,Is.False,"An attack burst cannot turn into an idle snow loop.");
        }
        [UnityTest] public IEnumerator EveryRequestedLayerHasItsOriginalNativeAssetOrApprovedBakedPages()
        {
            var data=Read();Assert.That(data.version,Is.EqualTo(1));Assert.That(data.fps,Is.EqualTo(30));
            Assert.That(data.encoding,Is.EqualTo("sqrt-linear-rgb-effective-alpha"));
            var all=data.clips.ToDictionary(c=>c.key);
            var requested=DefenderVfxArt.RequiredKeysForArtIds(new[]{"D03","D05","D08","D09","D11","D12","D21","D22","D23","D24","D25","D26","D27","D28","D29","D30","D32","D33","D34","D35"}).Where(k=>k.StartsWith("defender/",StringComparison.Ordinal)).ToArray();
            var native=requested.Where(k=>NativeArtIds.Contains(k.Split('/')[1])).ToArray();
            foreach(string key in native)Assert.That(OriginalNativeFxCatalog.Supports(key),Is.True,"Missing native runtime dependency: "+key);
            yield return OriginalNativeFxCatalog.Prepare(native);
            foreach(string key in requested)
            {
                if(NativeArtIds.Contains(key.Split('/')[1]))
                {
                    Assert.That(OriginalNativeFxCatalog.TryPrepared(key,out var effect,out var prefab),Is.True,key);
                    Assert.That(prefab,Is.Not.Null,key);Assert.That(effect.sourcePrefab,Is.Not.Null.And.Not.Empty,key);
                    Assert.That(effect.asset,Does.EndWith(".prefab"),key);Assert.That(effect.nodes,Is.Not.Empty,key);
                    foreach(var node in effect.nodes)Assert.That(NativeNode(prefab.transform,node.indices),Is.Not.Null,node.path);
                    foreach(var descriptor in effect.renderers)
                    {
                        var renderer=NativeNode(prefab.transform,descriptor.indices).GetComponents<Renderer>().SingleOrDefault(r=>r.GetType().Name==descriptor.kind);
                        Assert.That(renderer,Is.Not.Null,key+" / "+descriptor.path);
                        var slots=renderer.sharedMaterials;
                        if(renderer is ParticleSystemRenderer)
                            Assert.That(slots.Length,Is.LessThanOrEqualTo(descriptor.materials.Length),descriptor.path+" / extra runtime material slots");
                        else Assert.That(slots.Length,Is.EqualTo(descriptor.materials.Length),descriptor.path);
                        for(int slot=0;slot<descriptor.materials.Length;slot++)
                        {
                            var material=descriptor.materials[slot];
                            if(slot>=slots.Length)
                            {
                                // Unity 6 can normalize a ParticleSystemRenderer's duplicate/null
                                // trailing slots. Distinct serialized materials must remain present.
                                bool duplicate=material!=null&&Enumerable.Range(0,slots.Length).Any(n=>slots[n]!=null&&descriptor.materials[n]!=null&&
                                    string.Equals(descriptor.materials[n].sourceId,material.sourceId,StringComparison.Ordinal));
                                Assert.That(renderer is ParticleSystemRenderer&&(material==null||duplicate),Is.True,descriptor.path+" / missing distinct source material "+slot);
                            }
                            else if(material==null){Assert.That(slots[slot],Is.Null,descriptor.path+" / empty slot");continue;}
                            else Assert.That(slots[slot],Is.Not.Null,descriptor.path+" / missing source material "+slot);
                            if(material==null)continue;
                            Assert.That(material,Is.Not.Null,descriptor.path);Assert.That(material.sourceId,Is.Not.Null.And.Not.Empty);
                            Assert.That(material.shader,Is.Not.Null.And.Not.Empty);Assert.That(OriginalNativeFxCatalog.ShaderFor(material).isSupported,Is.True,material.shader);
                            foreach(var value in material.floats??Array.Empty<OriginalNativeFxCatalog.Float>())
                            {Assert.That(value.name,Is.Not.Null.And.Not.Empty);Finite(value.value,material.name+" / "+value.name);}
                            foreach(var value in material.colors??Array.Empty<OriginalNativeFxCatalog.Colour>())
                            {
                                Assert.That(value.name,Is.Not.Null.And.Not.Empty);
                                // The stock source material retains an infinite inverse
                                // distance for disabled camera fading. UnityStandardParticles
                                // never reads it without _FADING_ON; preserve the source value.
                                bool disabledCameraFade=material.shader=="Particles/Standard Unlit"&&value.name=="_CameraFadeParams"&&
                                    !(material.keywords??Array.Empty<string>()).Contains("_FADING_ON")&&
                                    (material.floats??Array.Empty<OriginalNativeFxCatalog.Float>()).Any(f=>f.name=="_CameraFadingEnabled"&&f.value==0);
                                if(disabledCameraFade&&float.IsPositiveInfinity(value.value.g))
                                {Finite(value.value.r,material.name);Finite(value.value.b,material.name);Finite(value.value.a,material.name);continue;}
                                for(int channel=0;channel<4;channel++)Finite(value.value[channel],material.name+" / "+value.name);
                            }
                            foreach(var texture in material.textures??Array.Empty<OriginalNativeFxCatalog.Texture>())
                            {
                                Assert.That(texture.name,Is.Not.Null.And.Not.Empty);
                                Finite(texture.scale.x,texture.name);Finite(texture.scale.y,texture.name);Finite(texture.offset.x,texture.name);Finite(texture.offset.y,texture.name);
                            }
                        }
                    }
                    continue;
                }
                Assert.That(all.ContainsKey(key),Is.True,"Missing runtime dependency: "+key);var clip=all[key];
                Assert.That(clip.sourcePrefab,Is.Not.Null.And.Not.Empty);Assert.That(clip.count,Is.InRange(1,600));
                Assert.That(clip.ppu,Is.GreaterThan(0));Assert.That(clip.duration,Is.EqualTo(clip.count/30f).Within(.0001));
                Assert.That(clip.width%4,Is.Zero);Assert.That(clip.height%4,Is.Zero);
                Assert.That(clip.pages.Sum(p=>p.count),Is.EqualTo(clip.count));
                foreach(var page in clip.pages)
                {
                    Assert.That(page.width,Is.InRange(4,2048));Assert.That(page.height,Is.InRange(4,2048));
                    Assert.That(File.Exists(ArtSourcePaths.Resolve("OriginalCombatVfx/Defenders/"+page.file)),Is.True,page.file);
                }
            }
        }
        [TestCase("D03","spot_skill_01_hit")][TestCase("D05","midn_skill_01_hit")]
        [TestCase("D08","stward_skill_01_hit")][TestCase("D09","lava_attack_01_hit")]
        [TestCase("D12","orchid_attack_01_hit")][TestCase("D22","yu_skill3_hit_01")]
        [TestCase("D23","monstr_skill_03_hit")][TestCase("D24","angel2_skill_3_loop_hit_01")]
        [TestCase("D25","hsgma2_skill_03_hit_02_3")][TestCase("D26","svash2_skill_03_range_01")]
        [TestCase("D28","wang_passive_01_buff_02")][TestCase("D29","demkni_attack_01_hit")]
        [TestCase("D30","mlynar_skill_03_hit_01")]
        [TestCase("D32","chen3_skill_03_hit_01")][TestCase("D33","aglna2_skill_03_hit_01")]
        [TestCase("D34","clemnt_skill_03_hit_01")][TestCase("D35","kalts2_skill_02_range_damage_01")]
        public void NormalAndSelectedSkillEventsUseTheirOwnOriginalFamily(string id,string raw)
        {Assert.That(DefenderVfxArt.RequiredKeysForArtIds(new[]{id}),Does.Contain("defender/"+id+"/"+raw));}
        [Test] public void UnimplementedModelsAreNotFalselyClaimedAsCompleteEffects()
        {
            for(int n=1;n<=12;n++)Assert.That(DefenderVfxArt.SupportsArtId("D"+n.ToString("D2")),Is.True);
            for(int n=21;n<=30;n++)Assert.That(DefenderVfxArt.SupportsArtId("D"+n),Is.True);
            for(int n=32;n<=35;n++)Assert.That(DefenderVfxArt.SupportsArtId("D"+n),Is.True);
            Assert.That(DefenderVfxArt.SupportsArtId("D31"),Is.False,"The separate Wisadel Boss skin is not a normal recruitable defender.");
            for(int n=13;n<=20;n++)Assert.That(DefenderVfxArt.SupportsArtId("D"+n),Is.False);
            Assert.That(DefenderVfxArt.SupportsArtId(null),Is.False);
            Assert.That(DefenderVfxArt.RequiredKeysForArtIds(new[]{"unknown"}),Is.Empty);
        }
    }
}
