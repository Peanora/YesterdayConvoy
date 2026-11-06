using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace YesterdayConvoy
{
    // Pure metadata/event mapping. No battle mutation and no texture decoding.
    internal static class MonsterOriginalVfx
    {
        sealed class PhaseTail{internal int Run;internal double Until=double.NegativeInfinity;}
        static readonly ConditionalWeakTable<ManualBattle,PhaseTail> phaseTails=new ConditionalWeakTable<ManualBattle,PhaseTail>();
        internal static void RecordPhaseTail(ManualBattle battle,double at,string raw)
        {
            var tail=phaseTails.GetValue(battle,_=>new PhaseTail());
            if(tail.Run!=battle.RunId){tail.Run=battle.RunId;tail.Until=double.NegativeInfinity;}
            tail.Until=Math.Max(tail.Until,at+Duration(raw));
        }
        static readonly Dictionary<string,string[]> required=new Dictionary<string,string[]>
        {
            {"M04",new[]{"bslime_die_01_start"}}, {"M06",new[]{"snslime_die_01_start"}},
            {"M05",new[]{"mslime_attack_01_trail","mslime_attack_01_hit"}},
            {"M11",Array.Empty<string>()},
            {"M12",new[]{"snmage_attack_01_start","snmage_attack_01_trail","snmage_attack_01_hit","snmage_skill_01_start","snmage_skill_01_buff"}},
            {"M14",new[]{"firstar_attack_01_start","firstar_attack_02_start","firstar_attack_01_trail","firstar_attack_01_hit","firstar_skill_01_buff","firstar_skill_01_range","firstar_skill_02_start","firstar_skill_02_trail","firstar_skill_02_range","firstar_skill_02_range_02","firstar_buff_01_start","firstar_buff_02_start","firstar_buff_03_start"}},
            {"M16",new[]{"sotisp_attack_01_trail","sotisp_attack_01_hit"}},
            {"M26",new[]{"enemy_mdsnpr_attack_01_start","enemy_mdsnpr_attack_01_trail","enemy_mdsnpr_attack_01_hit"}},
            {"M42",new[]{"enemy_mdsnpr_attack_01_start","enemy_mdsnpr_attack_01_trail","enemy_mdsnpr_attack_01_hit"}},
            {"M30",new[]{"enemy_duskls_attack_start","enemy_duskls_attack_hit","enemy_duskls_idle_2","enemy_duskls_weapon_buff","enemy_duskls_revive_2","enemy_duskls_revive_3","enemy_duskls_revive_4"}},
            {"M31",new[]{"enemy_duspfr_2_attack_01_front_start2","enemy_duspfr_2_attack_01_hit","enemy_duspfr_2_die_01","enemy_duspfr_2_die_01_hit"}},
            {"M33",new[]{"enemy_dsurch_01_start","enemy_dsurch_01_start_02","common/common_nerve_injury_01_hit"}},
            {"M34",new[]{"enemy_dslntc_buff_01","enemy_dslntc_skill_begin_buff","enemy_dslntc_attack_01_hit","common/common_nerve_injury_01_hit"}},
            {"M36",new[]{"enemy_dsmant_attack_01_start","enemy_dsmant_attack_01_trail","enemy_dsmant_attack_01_hit","enemy_dsmant_attack_02_start","enemy_dsmant_attack_02_start_02","enemy_dsmant_attack_02_hit","enemy_dsmant_skill_01_start","enemy_dsmant_skill_01_buff","enemy_dsmant_skill_01_buff_02","enemy_dsmant_skill_01_buff_03","enemy_dsmant_skill_01_hit","common/common_nerve_injury_01_hit"}},
            {"M38",new[]{"dmage_skill_01_start","dmage_skill_01_line","dmage_skill_01_buff"}},
        };
        internal static IEnumerable<string> RequiredKeysForCards(IEnumerable<string> cardIds)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            if(cardIds==null)yield break;
            foreach(var id in cardIds)
            {
                string role=id!=null&&id.StartsWith("CARD_",StringComparison.Ordinal)?id.Substring(5):id;
                if(role==null||!required.TryGetValue(role,out var keys))continue;
                foreach(string raw in keys){string key=raw.StartsWith("common/",StringComparison.Ordinal)?raw:"monster/"+raw;if(seen.Add(key))yield return key;}
            }
        }
        internal static float Duration(string raw,float fallback=0)
        {
            string key=raw.StartsWith("monster/",StringComparison.Ordinal)||raw.StartsWith("common/",StringComparison.Ordinal)?raw:"monster/"+raw;
            return OriginalCombatVfxArt.TryGet(key,out var clip)?clip.Duration:fallback;
        }
        internal static float DeathTail(DeployedUnitState unit)
        {
            if(unit?.CardId=="CARD_M31")return Math.Max(Duration("enemy_duspfr_2_die_01"),Duration("enemy_duspfr_2_die_01_hit"));
            return 0;
        }
        internal static double PendingUntil(ManualBattle battle)
        {
            if(battle==null)return double.NegativeInfinity;
            double until=double.NegativeInfinity;
            foreach(var e in battle.DeathBlasts)
                if(e.Resolved)until=Math.Max(until,e.DetonateAt+Duration(e.Cold?"snslime_die_01_start":"bslime_die_01_start"));
            foreach(var e in battle.FuelExplosions)
                if(e.Resolved)until=Math.Max(until,e.DetonateAt+Math.Max(Duration("enemy_duspfr_2_die_01"),Duration("enemy_duspfr_2_die_01_hit")));
            foreach(var e in battle.FrostSeals)
                if(e.Resolved&&!e.Cancelled)until=Math.Max(until,Math.Max(e.ImpactAt+FrostSealCast.FreezeDuration,e.StartedAt+Duration("firstar_skill_02_range")));
            foreach(var e in battle.IceRings)until=Math.Max(until,e.Time+Duration("firstar_skill_01_range"));
            foreach(var e in battle.SnowstormPulses)
                if(e.Targets.Count>0)until=Math.Max(until,e.At+Duration("firstar_skill_01_range"));
            foreach(var e in battle.CreeperPulses)
                until=Math.Max(until,e.Time+Math.Max(Duration("enemy_dsurch_01_start"),Duration("enemy_dsurch_01_start_02")));
            foreach(var d in battle.Defenders)
                if(!double.IsNegativeInfinity(d.LastNeuralBurstAt))until=Math.Max(until,d.LastNeuralBurstAt+Duration("common/common_nerve_injury_01_hit"));
            foreach(var e in battle.CorrosiveBubbles)
                if(!double.IsNegativeInfinity(e.LastPulseAt))until=Math.Max(until,e.LastPulseAt+Duration("enemy_dsmant_skill_01_hit"));
            foreach(var e in battle.FlameSprays)
                if(e.Ticks>0)until=Math.Max(until,e.StartedAt+.5+(e.Ticks-1)*.5+Duration("enemy_duspfr_2_attack_01_hit"));
            foreach(var u in battle.Units)
                if(u.CardId=="CARD_M30"&&u.FlamePhase==FlamechaserPhase.Reforming)
                    RecordPhaseTail(battle,u.FlamePhaseStartedAt,"enemy_duskls_revive_3");
            if(phaseTails.TryGetValue(battle,out var phase)&&phase.Run==battle.RunId)until=Math.Max(until,phase.Until);
            return until;
        }
    }
}
