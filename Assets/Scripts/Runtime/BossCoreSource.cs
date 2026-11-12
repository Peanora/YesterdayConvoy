using System;
using System.Collections.Generic;
namespace YesterdayConvoy
{
    internal static class BossCoreSource
    {
        static readonly Dictionary<string,Dictionary<string,BossRosterRules.Clip>> clips=new Dictionary<string,Dictionary<string,BossRosterRules.Clip>>(StringComparer.Ordinal){
            {"CARD_TALULAH_TRIAL",new Dictionary<string,BossRosterRules.Clip>(StringComparer.Ordinal){
                {"Attack_A",new BossRosterRules.Clip("Attack_A",2.33333325f,new float[]{1.03333330f})},
                {"Attack_B",new BossRosterRules.Clip("Attack_B",2.33333325f,new float[]{1.03333330f})},
                {"Default",new BossRosterRules.Clip("Default",0.00000000f,new float[]{})},
                {"Die",new BossRosterRules.Clip("Die",2.66666675f,new float[]{})},
                {"Idle_A",new BossRosterRules.Clip("Idle_A",9.60000038f,new float[]{})},
                {"Idle_B",new BossRosterRules.Clip("Idle_B",9.60000038f,new float[]{})},
                {"Move_A",new BossRosterRules.Clip("Move_A",3.59999990f,new float[]{})},
                {"Move_B",new BossRosterRules.Clip("Move_B",3.59999990f,new float[]{})},
                {"Reborn_1_End",new BossRosterRules.Clip("Reborn_1_End",2.76666665f,new float[]{})},
                {"Reborn_1_Idle",new BossRosterRules.Clip("Reborn_1_Idle",0.00000000f,new float[]{})},
                {"Reborn_1_Start",new BossRosterRules.Clip("Reborn_1_Start",3.13333344f,new float[]{})},
                {"Reborn_2_End",new BossRosterRules.Clip("Reborn_2_End",1.23333335f,new float[]{})},
                {"Reborn_2_Idle",new BossRosterRules.Clip("Reborn_2_Idle",0.00000000f,new float[]{})},
                {"Reborn_2_Start",new BossRosterRules.Clip("Reborn_2_Start",5.33333349f,new float[]{})},
                {"Skill_3",new BossRosterRules.Clip("Skill_3",2.33333325f,new float[]{0.89999998f})},
                {"Skill_1_A",new BossRosterRules.Clip("Skill_1_A",1.96666670f,new float[]{1.16666663f})},
                {"Skill_1_B",new BossRosterRules.Clip("Skill_1_B",1.96666670f,new float[]{1.16666663f})},
                {"Skill_2_A",new BossRosterRules.Clip("Skill_2_A",1.96666670f,new float[]{1.03333330f})},
                {"Skill_2_B",new BossRosterRules.Clip("Skill_2_B",1.96666670f,new float[]{1.03333330f})},
            }},
            {"CARD_M14",new Dictionary<string,BossRosterRules.Clip>(StringComparer.Ordinal){
                {"Attack",new BossRosterRules.Clip("Attack",1.60000002f,new float[]{0.93333334f})},
                {"Default",new BossRosterRules.Clip("Default",0.00000000f,new float[]{})},
                {"Die",new BossRosterRules.Clip("Die",5.33333349f,new float[]{})},
                {"Idle",new BossRosterRules.Clip("Idle",4.00000000f,new float[]{})},
                {"Move",new BossRosterRules.Clip("Move",1.60000002f,new float[]{})},
                {"Skill_1",new BossRosterRules.Clip("Skill_1",3.00000000f,new float[]{1.83333337f})},
                {"Skill_2",new BossRosterRules.Clip("Skill_2",7.19999981f,new float[]{})},
                {"Skill_3",new BossRosterRules.Clip("Skill_3",3.66666675f,new float[]{2.90000010f})},
            }},
            {"CARD_WISADEL_APRIL",new Dictionary<string,BossRosterRules.Clip>(StringComparer.Ordinal){
                {"Attack_A",new BossRosterRules.Clip("Attack_A",2.09999990f,new float[]{0.60000002f})},
                {"Attack_B",new BossRosterRules.Clip("Attack_B",2.09999990f,new float[]{0.60000002f})},
                {"Attack_C",new BossRosterRules.Clip("Attack_C",2.09999990f,new float[]{0.60000002f})},
                {"Attack_Down_A",new BossRosterRules.Clip("Attack_Down_A",2.09999990f,new float[]{0.60000002f})},
                {"Attack_Down_B",new BossRosterRules.Clip("Attack_Down_B",2.09999990f,new float[]{0.60000002f})},
                {"Attack_Down_C",new BossRosterRules.Clip("Attack_Down_C",2.09999990f,new float[]{0.60000002f})},
                {"Default",new BossRosterRules.Clip("Default",0.00000000f,new float[]{})},
                {"Die",new BossRosterRules.Clip("Die",1.00000000f,new float[]{})},
                {"Idle",new BossRosterRules.Clip("Idle",10.00000000f,new float[]{})},
                {"Skill_1",new BossRosterRules.Clip("Skill_1",2.09999990f,new float[]{0.60000002f})},
                {"Skill_2_Begin",new BossRosterRules.Clip("Skill_2_Begin",0.33333334f,new float[]{})},
                {"Skill_2_End",new BossRosterRules.Clip("Skill_2_End",0.10000000f,new float[]{})},
                {"Skill_2_Idle",new BossRosterRules.Clip("Skill_2_Idle",10.00000000f,new float[]{})},
                {"Skill_2_Loop",new BossRosterRules.Clip("Skill_2_Loop",1.39999998f,new float[]{0.26666668f})},
                {"Skill_2_Overload_Begin",new BossRosterRules.Clip("Skill_2_Overload_Begin",0.16666667f,new float[]{})},
                {"Skill_2_Overload_End",new BossRosterRules.Clip("Skill_2_Overload_End",0.50000000f,new float[]{})},
                {"Skill_2_Overload_Idle",new BossRosterRules.Clip("Skill_2_Overload_Idle",10.00000000f,new float[]{})},
                {"Skill_2_Overload_Loop",new BossRosterRules.Clip("Skill_2_Overload_Loop",1.39999998f,new float[]{0.26666668f})},
                {"Skill_3_Begin",new BossRosterRules.Clip("Skill_3_Begin",0.83333331f,new float[]{})},
                {"Skill_3_End",new BossRosterRules.Clip("Skill_3_End",0.50000000f,new float[]{})},
                {"Skill_3_Idle",new BossRosterRules.Clip("Skill_3_Idle",8.00000000f,new float[]{})},
                {"Skill_3_Loop",new BossRosterRules.Clip("Skill_3_Loop",5.00000000f,new float[]{0.53333336f})},
                {"Skill_Down_3_Idle",new BossRosterRules.Clip("Skill_Down_3_Idle",8.00000000f,new float[]{})},
                {"Skill_Down_3_Loop",new BossRosterRules.Clip("Skill_Down_3_Loop",5.00000000f,new float[]{0.53333336f})},
                {"Start",new BossRosterRules.Clip("Start",1.06666672f,new float[]{})},
                {"Stun",new BossRosterRules.Clip("Stun",1.00000000f,new float[]{})},
            }},
        };
        internal static BossRosterRules.Clip Action(string card,string raw)=>raw!=null&&clips.TryGetValue(card??"",out var all)&&all.TryGetValue(raw,out var clip)?clip:null;
    }
}
