using System;
using System.Collections.Generic;

namespace YesterdayConvoy
{
    /// <summary>Original source-key registry. Only requested defenders contribute their actual effect sequences.</summary>
    public static class DefenderVfxArt
    {
        static readonly HashSet<string> supported=new HashSet<string>(StringComparer.Ordinal)
        { "D01","D02","D03","D04","D05","D06","D07","D08","D09","D10","D11","D12","D21","D22","D23","D24","D25","D26","D27","D28","D29","D30","D32","D33","D34","D35" };
        static readonly Dictionary<string,string[]> keys=new Dictionary<string,string[]>(StringComparer.Ordinal)
        {
            ["D03"] = new[] { "defender/D03/spot_skill_01_trail","defender/D03/spot_skill_01_hit","common/common_hit_01" },
            ["D05"] = new[] { "defender/D05/midn_attack_01_trail","defender/D05/midn_attack_01_hit","defender/D05/midn_skill_01_trail","defender/D05/midn_skill_01_hit" },
            ["D08"] = new[] { "defender/D08/stward_skill_01_trail","defender/D08/stward_skill_01_hit" },
            ["D09"] = new[] { "defender/D09/lava_attack_01_hit" },
            ["D11"] = new[] { "common/common_heal_hit_01" },
            ["D12"] = new[] { "defender/D12/orchid_attack_01_trail","defender/D12/orchid_attack_01_hit" },
            ["D21"] = new[] { "defender/D21/whitw2_attack_01_start","defender/D21/whitw2_attack_01_trail","defender/D21/whitw2_hit_01","defender/D21/whitw2_token_01","defender/D21/whitw2_token_hit_01","defender/D21/whitw2_skill_03_start_01","defender/D21/whitw2_skill_03_token_hit_01","defender/D21/whitw2_skill_03_token_trail_01","defender/D21/whitw2_skill_03_trail_01","defender/D21/whitw2_skill_03_buff_start" },
            ["D22"] = new[] { "defender/D22/yu_attack_01","defender/D22/yu_hit_01","defender/D22/yu_skill3_attack_01","defender/D22/yu_skill3_hit_01","defender/D22/yu_skill3_buff_01","defender/D22/yu_skill3_annex_01","defender/D22/yu_skill3_range_01","defender/D22/yu_skill3_range_01_l","defender/D22/yu_skill3_range_01_r","defender/D22/yu_skill3_range_01_l_end","defender/D22/yu_skill3_range_01_r_end","defender/D22/yu_skill3_range_01_l_u_end","defender/D22/yu_skill3_range_01_l_d_end","defender/D22/yu_skill3_range_01_r_u_end","defender/D22/yu_skill3_range_01_r_d_end" },
            ["D23"] = new[] { "defender/D23/monstr_attack_start_01","defender/D23/monstr_attack_trail","defender/D23/monstr_attack_hit_01","defender/D23/monstr_skill_03_heal","defender/D23/monstr_skill_03_start_01","defender/D23/monstr_skill_03_hit","defender/D23/monstr_skill_03_begin_01","defender/D23/monstr_skill_03_begin_02","defender/D23/monstr_skill_03_begin_03","defender/D23/monstr_skill_03_end_01","defender/D23/monstr_skill_03_end_02","defender/D23/monstr_skill_03_buff_01","defender/D23/monstr_skill_03_buff_02","defender/D23/monstr_skill_03_buff_03","defender/D23/monstr_skill_03_buff_04","defender/D23/monstr_skill_03_buff_05","defender/D23/monstr_heal_line_02" },
            ["D24"] = new[] { "defender/D24/angel2_attack_loop_start_01","defender/D24/angel2_attack_loop_trail_01","defender/D24/angel2_attack_loop_hit_01","defender/D24/angel2_skill_3_begin_trail_01","defender/D24/angel2_skill_3_begin_range_01","defender/D24/angel2_skill_3_loop_start_01","defender/D24/angel2_skill_3_loop_trail_01","defender/D24/angel2_skill_3_loop_hit_01","defender/D24/angel2_skill_3_buff_01","defender/D24/angel2_skill_3_ground_01","defender/D24/angel2_attack_down_loop_start_01","defender/D24/angel2_skill_3_down_loop_start_01","defender/D24/angel2_buff_01_trail_01" },
            ["D25"] = new[] { "defender/D25/hsgma2_attack_01_start_01","defender/D25/hsgma2_attack_01_hit_01","defender/D25/hsgma2_skill_03_start_01","defender/D25/hsgma2_skill_03_start_02","defender/D25/hsgma2_skill_03_start_03","defender/D25/hsgma2_skill_03_start_04","defender/D25/hsgma2_skill_03_start_05","defender/D25/hsgma2_skill_03_start_06","defender/D25/hsgma2_skill_03_hit_01","defender/D25/hsgma2_skill_03_hit_01_1","defender/D25/hsgma2_skill_03_hit_02","defender/D25/hsgma2_skill_03_hit_02_1","defender/D25/hsgma2_skill_03_hit_02_2","defender/D25/hsgma2_skill_03_hit_02_3","defender/D25/hsgma2_skill_03_buff_01","defender/D25/hsgma2_skill_03_buff_02","defender/D25/hsgma2_skill_03_buff_03","defender/D25/hsgma2_skill_03_range_01","defender/D25/hsgma2_skill_03_range_02" },
            ["D26"] = new[] { "defender/D26/svash2_attack_start_a_01","defender/D26/svash2_attack_start_a_02","defender/D26/svash2_attack_start_b_01","defender/D26/svash2_attack_start_c_01","defender/D26/svash2_attack_hit_01","defender/D26/svash2_skill_03_combat_start_01_2","defender/D26/svash2_skill_03_combat_start_02","defender/D26/svash2_skill_03_buff_01","defender/D26/svash2_skill_03_buff_weapon_01","defender/D26/svash2_skill_03_deploy_01","defender/D26/svash2_skill_03_range_01","defender/D26/svash2_eagle_fly_01" },
            ["D27"] = new[] { "defender/D27/sbell2_skill_03_start_01","defender/D27/sbell2_skill_03_hit",
                "defender/D27/sbell2_skill_03_buff_01","defender/D27/sbell2_skill_03_range","defender/D27/sbell2_skill_03_flow",
                "defender/D27/sbell2_ground_01","defender/D27/sbell2_ground_02","defender/D27/sbell2_ground_03","defender/D27/sbell2_buff_01_hit_01" },
            ["D28"] = new[] { "defender/D28/wang_skill_03_range_start","defender/D28/wang_skill_03_buff_02","defender/D28/wang_skill_03_range","defender/D28/wang_attack_01_start","defender/D28/wang_attack_01_trail","defender/D28/wang_attack_01_hit","defender/D28/wang_passive_01_start","defender/D28/wang_passive_01_buff","defender/D28/wang_passive_01_buff_02" },
            ["D29"] = new[] { "defender/D29/demkni_attack_01_hit","defender/D29/demkni_skill_03_range","defender/D29/demkni_skill_03_self","defender/D29/demkni_skill_03_buff","defender/D29/common_heal_hit_01" },
            ["D30"] = new[] { "defender/D30/mlynar_skill_03_buff_01","defender/D30/mlynar_skill_03_range_01","defender/D30/mlynar_skill_03_start_01","defender/D30/mlynar_skill_03_hit_01","defender/D30/mlynar_skill_03_buff_02","defender/D30/mlynar_skill_03_buff_03","defender/D30/mlynar_attack_01_buff_04" },
            ["D32"] = new[] { "defender/D32/chen3_attack_start_01","defender/D32/chen3_attack_hit_01","defender/D32/chen3_buff_start_01","defender/D32/chen3_shield_buff_01","defender/D32/chen3_shield_buff_02","defender/D32/chen3_skill_03_buff_l_01","defender/D32/chen3_skill_03_buff_r_01","defender/D32/chen3_skill_03_weapon_01","defender/D32/chen3_skill_03_start_01","defender/D32/chen3_skill_03_hit_01","defender/D32/chen3_skill_03_hit_02","defender/D32/chen3_skill_03_hit_03","defender/D32/chen3_skill_03_hit_04","defender/D32/chen3_skill_03_start_02_dir_right","defender/D32/chen3_skill_03_start_02_dir_left","defender/D32/chen3_skill_03_start_02_dir_up","defender/D32/chen3_skill_03_start_02_dir_down","defender/D32/chen3_skill_03_range_01_dir_right","defender/D32/chen3_skill_03_range_01_dir_left","defender/D32/chen3_skill_03_range_01_dir_up","defender/D32/chen3_skill_03_range_01_dir_down","defender/D32/chen3_skill_03_trail_01_dir_right","defender/D32/chen3_skill_03_trail_01_dir_left","defender/D32/chen3_skill_03_trail_01_dir_up","defender/D32/chen3_skill_03_trail_01_dir_down","defender/D32/chen3_skill_03_tail_ribbon" },
            ["D33"] = new[] { "defender/D33/aglna2_attack_hit_01","defender/D33/aglna2_attack_start_01","defender/D33/aglna2_attack_trail_01","defender/D33/aglna2_skill_03_buff_left_01","defender/D33/aglna2_skill_03_buff_right_01","defender/D33/aglna2_skill_03_buff_weapon_01","defender/D33/aglna2_skill_03_combat_start_01","defender/D33/aglna2_skill_03_fly_trail_01","defender/D33/aglna2_skill_03_hit_01","defender/D33/aglna2_skill_03_range_01","defender/D33/aglna2_skill_03_range_line_01","defender/D33/aglna2_skill_03_start_01","defender/D33/aglna2_skill_03_start_02","defender/D33/aglna2_skill_03_trail_01" },
            ["D34"] = new[] { "defender/D34/clemnt_attack_01_hit_01","defender/D34/clemnt_attack_01_start_01","defender/D34/clemnt_attack_01_start_01_down","defender/D34/clemnt_skill_03_buff_01_l","defender/D34/clemnt_skill_03_buff_01_r","defender/D34/clemnt_skill_03_buff_02","defender/D34/clemnt_skill_03_hit_01","defender/D34/clemnt_skill_03_mark_all_01","defender/D34/clemnt_skill_03_range","defender/D34/clemnt_skill_03_range_end","defender/D34/clemnt_skill_03_start_01","defender/D34/clemnt_skill_03_start_01_down","defender/D34/clemnt_skill_03_start_01_up" },
            ["D35"] = new[] { "defender/D35/kalts2_skill_01_start_01","defender/D35/kalts2_skill_02_buff_01","defender/D35/kalts2_skill_02_indicator_damage_01","defender/D35/kalts2_skill_02_indicator_heal_01","defender/D35/kalts2_skill_02_range_damage_01","defender/D35/kalts2_skill_02_range_damage_01_delay","defender/D35/kalts2_skill_02_range_heal_01","defender/D35/kalts2_skill_02_range_heal_01_delay","defender/D35/kalts2_skill_02_start_damage_01","defender/D35/kalts2_skill_02_start_heal_01","defender/D35/kalts2_skill_02_trail_damage_01","defender/D35/kalts2_skill_02_trail_heal_01","defender/D35/folnic_skill_02_start","defender/D35/kalts2_skill_01_trail_01","defender/D35/kalts2_skill_01_hit_01" },
        };
        static readonly Dictionary<string,string> knownGaps=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["defender/D11/ansel_attack_01_trail"] = "Original TrailRenderer point-age playback pending verified adapter.",
            ["defender/D24/angel2_skill_3_begin_start_001"] = "Original EmptyEffect control with no visible Renderer, intentional no-op.",
            ["defender/D33/aglna2_skill_03_combat_start_01"] = "Original UnitMode Combat has a distinct selector but its FSM trigger is not serialized here; only the verified normal S3 Attack branch is bound, with no rear/first-hit guess.",
            ["defender/D23/monstr_heal_line_01"] = "Original ordinary healing chain line is unavailable; retain original normal start/projectile/hit, never replace it with the S3 line02.",
            ["defender/D24/angel2_skill_3_begin_trail_01"] = "The current model records instantaneous successful reinforcement landing; no proven pre-landing interval, so no fake post-landing flight is rendered.",
            ["defender/D30/mlynar_attack_01_buff_04"] = "Preloaded original Default/AnimAttack Idle_2 emitter has NeverTrigger; no proven gameplay trigger, so no fabricated attack.",
            ["defender/D26/svash2_skill_03_combat_start_01"] = "Original weapon TrailRenderer sampled with source C_Weapon trajectory remains empty; no substitute layer is rendered.",
        };
        public static IReadOnlyDictionary<string,string> KnownGaps=>knownGaps;
        public static bool SupportsArtId(string artId)=>artId!=null&&supported.Contains(artId);
        public static bool SupportsDefender(DefenderState defender)=>defender!=null&&SupportsArtId(defender.Settings.ArtId);
        public static bool Supports(DefenderSkillKind kind)=>kind>DefenderSkillKind.None&&kind<=DefenderSkillKind.KaltsitEsperanta||kind==DefenderSkillKind.ChenDawnstreak;
        public static IEnumerable<string> RequiredKeysForArtIds(IEnumerable<string> artIds)
        {
            if(artIds==null)throw new ArgumentNullException(nameof(artIds));
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string id in artIds)if(id!=null&&keys.TryGetValue(id,out var needed))
                foreach(string key in needed)if(seen.Add(key))yield return key;
        }
    }
}
