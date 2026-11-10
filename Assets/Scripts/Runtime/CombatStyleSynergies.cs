using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class StyleBody
        {
            public bool WardUsed,RageUsed;
            public double WardUntil,RageUntil;
            public float Ward;
            public int Hits;
            public readonly HashSet<int> CountedOrdinaries=new HashSet<int>();
            public readonly Dictionary<int,float> LeechSpent=new Dictionary<int,float>();
        }
        sealed class StyleTarget
        {
            public double RuptureUntil,ExposedUntil;
            public readonly Dictionary<string,double> Marksmen=new Dictionary<string,double>(StringComparer.Ordinal);
        }
        readonly HashSet<string> styleMembers=new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<CombatStyle,int> styleTiers=new Dictionary<CombatStyle,int>();
        readonly Dictionary<string,StyleBody> styleBodies=new Dictionary<string,StyleBody>(StringComparer.Ordinal);
        readonly Dictionary<string,StyleTarget> styleTargets=new Dictionary<string,StyleTarget>(StringComparer.Ordinal);
        readonly HashSet<string> coordinationKinds=new HashSet<string>(StringComparer.Ordinal);
        int coordinationPending;
        public bool CombatStylesEnabled {get;private set;}
        public int StylePulses {get;private set;}
        public int StyleWards {get;private set;}
        public int StyleRages {get;private set;}
        public int StyleCoordinations {get;private set;}
        public float StyleSPGranted {get;private set;}
        public int StyleTier(CombatStyle style)=>styleTiers.TryGetValue(style,out int tier)?tier:0;
        void ConfigureCombatStyles(IEnumerable<string> ids,bool enabled)
        {
            CombatStylesEnabled=enabled;styleMembers.Clear();styleTiers.Clear();
            if(!enabled)return;
            foreach(string id in ids)styleMembers.Add(id);
            foreach(var d in CombatStyleRules.Definitions)styleTiers[d.Style]=CombatStyleRules.Tier(d.Style,d.Members.Count(styleMembers.Contains));
        }
        void ResetCombatStyles()
        {styleBodies.Clear();styleTargets.Clear();coordinationKinds.Clear();coordinationPending=0;StylePulses=StyleWards=StyleRages=StyleCoordinations=0;StyleSPGranted=0;}
        bool StyleNative(DeployedUnitState u)=>CombatStylesEnabled&&u!=null&&!u.SynergySupport&&styleMembers.Contains(u.CardId);
        StyleBody StyleState(DeployedUnitState u)
        {if(!styleBodies.TryGetValue(u.EntityId,out var state))styleBodies[u.EntityId]=state=new StyleBody();return state;}
        StyleTarget StyleState(DefenderState d)
        {if(!styleTargets.TryGetValue(d.EntityId,out var state))styleTargets[d.EntityId]=state=new StyleTarget();return state;}
        public float StyleWardRemaining(DeployedUnitState u)=>u!=null&&styleBodies.TryGetValue(u.EntityId,out var s)&&s.WardUntil>Clock?s.Ward:0;
        public double StyleRageRemaining(DeployedUnitState u)=>u!=null&&styleBodies.TryGetValue(u.EntityId,out var s)?Math.Max(0,s.RageUntil-Clock):0;
        public int StyleMysticProgress(DeployedUnitState u)=>u!=null&&styleBodies.TryGetValue(u.EntityId,out var s)?s.Hits%3:0;
        public double StyleRuptureRemaining(DefenderState d)=>d!=null&&styleTargets.TryGetValue(d.EntityId,out var s)?Math.Max(0,s.RuptureUntil-Clock):0;
        public double StyleExposureRemaining(DefenderState d)=>d!=null&&styleTargets.TryGetValue(d.EntityId,out var s)?Math.Max(0,s.ExposedUntil-Clock):0;
        float StyleAttackBonus(DeployedUnitState u)=>StyleNative(u)&&StyleRageRemaining(u)>0?CombatStyleRules.RageAttack(StyleTier(CombatStyle.Frenzy)):0;
        float StyleArmor(DefenderState d,float armor,CombatDamageType type,bool derived,DeployedUnitState source)
            =>!derived&&StyleNative(source)&&type==CombatDamageType.Physical&&StyleRuptureRemaining(d)>0?
                armor-Mathf.Max(0,armor)*CombatStyleRules.Pierce(StyleTier(CombatStyle.Assault)):armor;
        float StyleRangedMultiplier(DefenderState d,bool derived,DeployedUnitState source)
            =>!derived&&StyleNative(source)&&!UnitCombat(source).Melee&&StyleExposureRemaining(d)>0?1+CombatStyleRules.Exposure(StyleTier(CombatStyle.Marksman)):1;
        void OnStyleDirectHit(DeployedUnitState source,DefenderState target)
        {
            if(!StyleNative(source))return;
            if(CombatStyleRules.For(source.CardId)==CombatStyle.Marksman&&StyleTier(CombatStyle.Marksman)>0&&target.Alive)
            {
                var s=StyleState(target);s.Marksmen[source.CardId]=Clock;
                if(s.Marksmen.Any(p=>p.Key!=source.CardId&&Clock-p.Value<=2.5+1e-6))s.ExposedUntil=Clock+4;
            }
            ContributeStyleCoordination(source);
        }
        void ContributeStyleCoordination(DeployedUnitState source)
        {
            int tier=StyleTier(CombatStyle.Coordination);
            if(tier==0||!StyleNative(source)||CombatStyleRules.For(source.CardId)!=CombatStyle.Coordination||!coordinationKinds.Add(source.CardId))return;
            coordinationPending++;if(coordinationPending<2)return;coordinationPending=0;StyleCoordinations++;
            foreach(var u in units)
            {
                if(!StyleNative(u)||!ContractActionable(u))continue;
                var c=UnitCombat(u);if(!c.HasSkill)continue;
                float gained=Mathf.Min(CombatStyleRules.SP(tier),Mathf.Max(0,c.RequiredSP-u.SkillSP));
                if(gained<=0)continue;u.SkillSP+=gained;StyleSPGranted+=gained;
                combatFeedback.Add(Clock,CombatFeedbackTarget.Monster,u.EntityId,u.Motion.Position,CombatFeedbackKind.SkillPointsRestored,gained);
            }
            combatFeedback.Add(Clock,CombatFeedbackTarget.Monster,source.EntityId,source.Motion.Position,CombatFeedbackKind.StatusApplied,0,CombatFeedbackStatus.Coordination);
        }
        void OnStyleOrdinaryHit(DeployedUnitState u,DefenderState d,CombatShot shot,float lifeLoss,bool effective)
        {
            if(!StyleNative(u)||shot.StyleOrdinaryAction<=0||shot.SynergyOrder||!effective)return;
            var style=CombatStyleRules.For(u.CardId);int tier=StyleTier(style);if(tier==0)return;
            var s=StyleState(u);
            if(style==CombatStyle.Assault&&d.Alive&&units.Any(friend=>StyleNative(friend)&&friend.Active&&!friend.IsReviving&&friend.BlockerId==d.EntityId))StyleState(d).RuptureUntil=Clock+4;
            if(style==CombatStyle.Mystic&&s.CountedOrdinaries.Add(shot.StyleOrdinaryAction))
            {
                s.Hits++;
                if(s.Hits%3==0)
                {
                    StylePulses++;
                    var targets=new List<DefenderState>();if(d.Alive)targets.Add(d);
                    targets.AddRange(defenders.Where(t=>t!=d&&t.Alive&&GroundDistance(t.Position,d.Position)<=1)
                        .OrderBy(t=>GroundDistance(t.Position,d.Position)).ThenBy(t=>t.EntityId,StringComparer.Ordinal).Take(2));
                    float attack=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*CombatStyleRules.Pulse(tier);
                    foreach(var target in targets)DamageDefenderFrom(target,attack,CombatDamageType.Arts,u,true);
                    combatFeedback.Add(Clock,CombatFeedbackTarget.Defender,d.EntityId,d.Position,CombatFeedbackKind.StatusApplied,0,CombatFeedbackStatus.ArcanePulse);
                }
            }
            if(style==CombatStyle.Frenzy&&StyleRageRemaining(u)>0&&shot.TargetId==shot.StylePrimaryTarget)
            {
                s.LeechSpent.TryGetValue(shot.StyleOrdinaryAction,out float spent);
                float restored=HealUnit(u,Mathf.Min(lifeLoss*CombatStyleRules.Leech(tier),Mathf.Max(0,u.MaxHealth*.05f-spent)));
                s.LeechSpent[shot.StyleOrdinaryAction]=spent+restored;
            }
        }
        void OnStyleHealthLoss(DeployedUnitState u)
        {
            if(!StyleNative(u)||CombatStyleRules.For(u.CardId)!=CombatStyle.Frenzy||StyleTier(CombatStyle.Frenzy)==0||u.Health<=0||u.Health>u.MaxHealth*.5f)return;
            var s=StyleState(u);if(s.RageUsed)return;s.RageUsed=true;s.RageUntil=Clock+8;StyleRages++;
        }
        void OnStyleDefenderHit(DeployedUnitState u)
        {
            if(!StyleNative(u)||!ContractActionable(u)||CombatStyleRules.For(u.CardId)!=CombatStyle.Bulwark)return;
            int tier=StyleTier(CombatStyle.Bulwark);if(tier==0)return;var s=StyleState(u);if(s.WardUsed)return;
            s.WardUsed=true;StyleWards++;
            var targets=new[]{u}.Concat(units.Where(t=>t!=u&&StyleNative(t)&&ContractActionable(t)&&GroundDistance(t.Motion.Position,u.Motion.Position)<=1.5f)
                .OrderBy(t=>GroundDistance(t.Motion.Position,u.Motion.Position)).ThenBy(t=>t.EntityId,StringComparer.Ordinal).Take(tier>=6?2:1));
            foreach(var target in targets)
            {
                var ward=StyleState(target);float old=ward.WardUntil>Clock?ward.Ward:0;
                ward.Ward=Mathf.Max(old,target.MaxHealth*CombatStyleRules.Shield(tier));ward.WardUntil=Clock+5;
            }
        }
        float AbsorbStyleWard(DeployedUnitState u,float damage)
        {
            float absorbed=Mathf.Min(damage,StyleWardRemaining(u));
            if(absorbed>0){StyleState(u).Ward-=absorbed;RecordUnitShieldAbsorption(u,absorbed);}return damage-absorbed;
        }
        void RefreshStyleStatuses()
        {
            foreach(var u in units)
            {
                if(!u.Active||u.IsReviving||!styleBodies.TryGetValue(u.EntityId,out var s))continue;
                if(s.Ward>0)AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,u.Motion.Position,CombatFeedbackStatus.Bulwark,s.WardUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,u.Motion.Position,CombatFeedbackStatus.Frenzy,s.RageUntil);
            }
            foreach(var d in defenders)
            {
                if(!d.Alive||!styleTargets.TryGetValue(d.EntityId,out var s))continue;
                AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,d.Position,CombatFeedbackStatus.Ruptured,s.RuptureUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,d.Position,CombatFeedbackStatus.Exposed,s.ExposedUntil);
            }
        }
    }
}
