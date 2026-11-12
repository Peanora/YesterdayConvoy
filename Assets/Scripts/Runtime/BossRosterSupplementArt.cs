using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace YesterdayConvoy
{
    // Reuses already imported original Talulah pages; never copies a legacy library.
    internal static class BossRosterSupplementArt
    {
        [Serializable] sealed class Manifest {public int version,fps;public Data[] clips;}
        [Serializable] sealed class Data {public string key;public int count,width,height,columns;public float ppu,pivotX,pivotY,duration,hdrRange;public Page[] pages;}
        [Serializable] sealed class Page {public string file;public int count;}
        static readonly Dictionary<string,BossRosterFxArt.Clip> clips=new Dictionary<string,BossRosterFxArt.Clip>(StringComparer.Ordinal);
        static readonly HashSet<string> loops=new HashSet<string>(StringComparer.Ordinal){"orb-body-idle","orb-map_fire_crystal_buff_01_start","target-burn-loop","original-energy-stream"};
        internal static BossRosterFxArt.Clip Talulah(string key)
        {
            if(clips.TryGetValue(key,out var value))return value;
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("TalulahUnboundVfx/manifest.json")));
            if(manifest==null||manifest.version!=1||manifest.fps!=30)throw new InvalidDataException("Invalid Talulah supplemental manifest.");
            var d=Array.Find(manifest.clips,c=>c.key==key);if(d==null||d.count<=0||d.ppu<=0||d.pages==null)throw new InvalidDataException("Missing original Talulah supplemental clip: "+key);
            var pages=Array.ConvertAll(d.pages,p=>new ArtSequence.Page{file=p.file,count=p.count,width=d.columns*d.width,height=Mathf.CeilToInt(p.count/(float)d.columns)*d.height});
            var sequence=new ArtSequence{Root="TalulahUnboundVfx",Key=key,Name="TalulahNative/"+key,Count=d.count,Width=d.width,Height=d.height,Columns=d.columns,Ppu=d.ppu,PivotX=d.pivotX,PivotY=d.pivotY,Duration=d.duration,Fps=30,Pages=pages};
            value=new BossRosterFxArt.Clip(sequence,d.hdrRange,0,loops.Contains(key));clips.Add(key,value);return value;
        }
        internal static BossRosterFxArt.Clip Frost(string key)
        {var original=OriginalCombatVfxArt.Get("monster/"+key);return new BossRosterFxArt.Clip(original.Sequence,original.HdrRange,0,original.Loop);}
        internal static IEnumerable<ArtSequence> SequencesFor(string card)
        {
            if(card=="CARD_TALULAH_TRIAL")foreach(var key in new[]{"ignite-A-caster","ignite-A-target","ignite-B-caster","ignite-B-target","explode-A-caster","explode-A-target","explode-B-caster","explode-B-target","summon-B-caster","target-burn-loop","orb-body-idle","orb-body-die","orb-map_fire_crystal_birth_01_start","orb-map_fire_crystal_birth_01_buff","orb-map_fire_crystal_buff_01_start","orb-map_fire_crystal_die_01_start","orb-map_fire_crystal_skill_01_start","original-energy-stream","original-fire-impact"})yield return Talulah(key).Sequence;
            if(card=="CARD_M14")foreach(var key in new[]{"firstar_skill_02_range_02","firstar_skill_02_range","firstar_skill_01_range"})yield return Frost(key).Sequence;
        }
    }
}
