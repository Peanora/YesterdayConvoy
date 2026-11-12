using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class AutomaticLegacyMonsterState
        {
            internal double RecordedRainAt=double.NegativeInfinity;
        }

        readonly Dictionary<string,AutomaticLegacyMonsterState> automaticLegacyMonsterStates=new Dictionary<string,AutomaticLegacyMonsterState>(StringComparer.Ordinal);
        long automaticLegacyRequest=long.MinValue;

        void ResetAutomaticLegacyMonsterSkills()
        {automaticLegacyMonsterStates.Clear();automaticLegacyRequest=long.MinValue;}

        // Enemy initial/cost fields are initial cooldown / repeat cooldown, whereas
        // the operator's initial SP is already charged. Keep those meanings separate.
        void InitializeAutomaticLegacyMonsterSkill(DeployedUnitState u)
        {
            if(!AutomaticSkillsEnabled||!EngagementEnabled||u==null||BossRosterCatalog.Contains(u.CardId)||automaticLegacyMonsterStates.ContainsKey(u.EntityId))return;
            automaticLegacyMonsterStates.Add(u.EntityId,new AutomaticLegacyMonsterState());
            var c=UnitCombat(u);
            if(ViviCombat.IsVivi(u.CardId)&&coreViviSkill=="s3")u.SkillSP=u.SkillAmmo>0?0:40;
            else if(c.MudrockCapture)u.SkillSP=0;
            if(u.CardId=="CARD_M14"&&c.Effects.FrostNova&&double.IsNegativeInfinity(u.IceRingStartedAt)&&!u.IsReviving)
                u.NextIceRingAt=u.SpawnTime+10.5;
        }

        float LegacyAutomaticRequiredSP(DeployedUnitState u)
        {
            if(u==null||!EngagementEnabled)return 0;
            var c=UnitCombat(u);
            if(!AutomaticSkillsEnabled)return c.RequiredSP;
            if(ViviCombat.IsVivi(u.CardId)&&coreViviSkill=="s3")return 50;
            return c.MudrockCapture?35:c.RequiredSP;
        }

        // True means this method owns the old SP field this step. S1/S2 own their
        // separate Core SP field; other legacy fixtures keep their original rules.
        bool StepAutomaticLegacySkillSP(DeployedUnitState u,float step)
        {
            if(!AutomaticSkillsEnabled||u==null||BossRosterCatalog.Contains(u.CardId))return false;
            InitializeAutomaticLegacyMonsterSkill(u);
            var c=UnitCombat(u);
            if(ViviCombat.IsVivi(u.CardId))
            {
                if(coreViviSkill!="s3")return true;
                if(u.Active&&!u.IsReviving&&u.SkillAmmo==0&&!IsSkillActionPlaying(u)&&!StrongholdInTransit(u.EntityId)&&!ApprovedWaitingAtPortal(u.EntityId))
                    u.SkillSP=Math.Min(50,u.SkillSP+step);
                return true;
            }
            if(!c.MudrockCapture)return false;
            // The enemy cooldown runs from cast start. A missing device leaves the
            // completed cooldown ready; it does not consume a use or reset SP.
            if(u.Active&&!u.IsReviving)u.SkillSP=Math.Min(35,u.SkillSP+step);
            return true;
        }

        float AutomaticLegacyIceRingCooldown(DeployedUnitState u)=>
            AutomaticSkillsEnabled&&u?.CardId=="CARD_M14"?10.5f:V2Skill(u,"iceRing","cooldown",8.5f);

        bool AutomaticLegacyBodyAvailable(DeployedUnitState u)=>
            !IsSkillActionPlaying(u)&&Clock+1e-6>=u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate;

        bool AutomaticLegacyHasTarget(DeployedUnitState u,float range)
        {
            var blocker=FindDefender(u.BlockerId);
            if(blocker!=null&&blocker.Alive&&UnitCanReachDefender(u,blocker)&&GroundDistance(u.Motion.Position,blocker.Position)<=range)return true;
            foreach(var d in defenders)
                if(d.Alive&&UnitCanReachDefender(u,d)&&!DefenderCamouflaged(d)&&GroundDistance(u.Motion.Position,d.Position)<=range)return true;
            return false;
        }

        bool TryAutomaticLegacyMonsterSkill(DeployedUnitState u)
        {
            if(!AutomaticSkillsEnabled||!EngagementEnabled||u==null||!u.Active||u.IsReviving||IsPaused||Phase!=BattlePhase.Running||BossRosterCatalog.Contains(u.CardId))return false;
            InitializeAutomaticLegacyMonsterSkill(u);
            var c=UnitCombat(u);
            if(!c.HasSkill||StrongholdInTransit(u.EntityId)||ApprovedWaitingAtPortal(u.EntityId)||TalulahTransitionLocked(u))return false;
            // These two old manual entries predate the native core implementation.
            // Their actual abilities already have independent source clocks/events.
            if(u.CardId=="CARD_TALULAH_TRIAL"||u.CardId=="CARD_M14"&&c.Effects.FrostNova)return false;
            if(UsesBossCoreSkill(u))
            {
                var s=BossCoreState(u);
                if(s.Armed||coreViviSkill=="s2"&&Clock<s.ActiveUntil||!AutomaticLegacyBodyAvailable(u))return false;
                if(coreViviSkill=="s1"&&Clock+1e-6<u.NextAttackTime)return false;
                if(ChooseDefender(u,c.UnitRange)==null||CheckBossCoreSkill(u)!=SkillFailure.None)return false;
                return TryActivateBossCoreSkill(RunId,automaticLegacyRequest++,u.EntityId)==SkillFailure.None;
            }
            if(!AutomaticLegacyBodyAvailable(u)||u.SkillAmmo>0)return false;
            if(c.MudrockCapture)
            {if(ChooseGramophone(u)==null)return false;}
            else if(ChooseDefender(u,c.SkillRange)==null)return false;
            if(CheckSkill(u.EntityId)!=SkillFailure.None)return false;
            if(TryActivateSkill(RunId,automaticLegacyRequest++,u.EntityId)!=SkillFailure.None)return false;
            if(!ViviCombat.IsVivi(u.CardId))RecordAutomaticMonsterCast(u,c.MudrockCapture?"capture":SilverWolfCombat.IsWolf(u)?"transform":"legacy");
            return true;
        }

        void ObserveAutomaticLegacyMonsterSkills(DeployedUnitState u)
        {
            if(!AutomaticSkillsEnabled||u==null||u.CardId!="CARD_TALULAH_TRIAL"||!u.Active)return;
            InitializeAutomaticLegacyMonsterSkill(u);
            if(!automaticLegacyMonsterStates.TryGetValue(u.EntityId,out var state)||!TalulahAutomaticRainActive(u)||state.RecordedRainAt==u.TalulahAutomaticRainStartedAt)return;
            state.RecordedRainAt=u.TalulahAutomaticRainStartedAt;
            RecordAutomaticMonsterCast(u,u.TalulahPhase==TalulahPhase.FinalRain?"finalRain":"firstRain");
        }

        internal string AutomaticLegacySkillStatus(DeployedUnitState u)
        {
            if(!EngagementEnabled||u==null||BossRosterCatalog.Contains(u.CardId))return null;
            var c=UnitCombat(u);
            if(ViviCombat.IsVivi(u.CardId))
            {
                bossCoreStates.TryGetValue(u.EntityId,out var s);
                if(coreViviSkill=="s1")return s?.Armed==true?"定点清算：强化下一击已就绪":"定点清算：攻击回复 "+(s?.SP??0).ToString("0")+"/2；下一击自动强化";
                if(coreViviSkill=="s2")return s!=null&&Clock<s.ActiveUntil?"饱和复仇："+(s.Phase>=2?"过载中 ":"生效中 ")+Math.Max(0,s.ActiveUntil-Clock).ToString("0.0")+"秒":s?.SkillId=="s2"&&Clock<s.Until?"饱和复仇：完成末次攻击动作":"饱和复仇："+(s?.SP??15).ToString("0.0")+"/25；就绪且有目标时自动开启25秒";
                if(u.SkillAmmo>0||IsSkillActionPlaying(u))return "爆裂黎明：剩余"+u.SkillAmmo+"发；技能与末发动作期间停止回复";
                float sp=!AutomaticSkillsEnabled||automaticLegacyMonsterStates.ContainsKey(u.EntityId)?u.SkillSP:40;
                return "爆裂黎明："+sp.ToString("0.0")+"/50；"+(!AutomaticLegacyHasTarget(u,c.SkillRange)?"等待攻击范围内目标":"就绪后自动开启");
            }
            if(u.CardId=="CARD_M14"&&c.Effects.FrostNova)
            {
                if(u.IsReviving)return "冬痕：重生中；重生后自动召唤寒潮与20秒保护";
                bossCoreStates.TryGetValue(u.EntityId,out var s);
                return "冰环 "+Math.Max(0,u.NextIceRingAt-Clock).ToString("0.0")+"秒／黑冰 "+Math.Max(0,(s?.NextBlackIceAt??u.SpawnTime+35)-Clock).ToString("0.0")+"秒；各按原作独立自动触发";
            }
            if(u.CardId=="CARD_TALULAH_TRIAL")
            {
                if(TalulahAutomaticRainActive(u))return "不息恶火：阶段火雨 "+u.TalulahAutomaticWavesFired+"/10";
                if(TalulahTransitionLocked(u))return "塔露拉：阶段转换中";
                bossCoreStates.TryGetValue(u.EntityId,out var s);
                return "点燃 "+Math.Max(0,(s?.NextIgniteAt??u.SpawnTime+19)-Clock).ToString("0.0")+"秒／引爆 "+Math.Max(0,(s?.NextExplodeAt??u.SpawnTime+35)-Clock).ToString("0.0")+"秒"+(TalulahSecondForm(u)?"／能量聚合体 "+Math.Max(0,(s?.NextEnergyAt??u.TalulahPhaseStartedAt+75)-Clock).ToString("0.0")+"秒":"；被击倒后进入阶段火雨");
            }
            if(!c.HasSkill)return null;
            float required=LegacyAutomaticRequiredSP(u);
            float current=AutomaticSkillsEnabled&&c.MudrockCapture&&!automaticLegacyMonsterStates.ContainsKey(u.EntityId)?0:u.SkillSP;
            if(c.MudrockCapture)return "占据："+current.ToString("0.0")+"/35；"+(ChooseGramophone(u)==null?"等待未占领留声机":"就绪后自动夺取最近留声机");
            if(u.SkillAmmo>0||IsSkillActionPlaying(u))return SilverWolfCombat.IsWolf(u)?"自动变身：特殊攻击中，剩余"+u.SkillAmmo+"次":"自动技能生效中";
            return (SilverWolfCombat.IsWolf(u)?"自动变身":"自动技能")+"："+current.ToString("0.0")+"/"+required.ToString("0.0")+"；"+(!AutomaticLegacyHasTarget(u,c.SkillRange)?"等待合法目标":"就绪后自动释放");
        }
    }
}
