using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        public bool IsFifth(DefenderState d)=>d!=null&&(int)d.Settings.Skill.Kind>=(int)DefenderSkillKind.Mon3trMeltdown&&(int)d.Settings.Skill.Kind<=(int)DefenderSkillKind.HoshigumaInferno;
        public bool IsFifthActive(DefenderState d)=>IsFifth(d)&&(DefenderSkillActive(d)||d.Settings.Skill.Kind==DefenderSkillKind.HoshigumaInferno&&d.ProtectionUntil>Clock);
        bool BeginFifthSkill(DefenderState d)
        {
            if(!IsFifth(d))return false;
            d.FifthActive=true;d.SkillActionUntil=Clock+.7;d.NextAttackTime=d.SkillActionUntil;
            if(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown)
            {d.Position=d.Settings.RelayPosition;d.HighGround=false;d.MaxHealth=d.Settings.Health+500;d.Health*=d.MaxHealth/d.Settings.Health;d.Transfers++;}
            if(d.Settings.Skill.Kind==DefenderSkillKind.ExusiaiDelivery)
            {
                d.Ammo=50;
                DefenderState reserve=null;
                foreach(var ally in defenders)if(ally.Reserve){reserve=ally;break;}
                if(reserve!=null)
                {
                    reserve.Reserve=false;reserve.Position=d.Settings.RelayPosition;reserve.HighGround=false;reserve.SkillSP=Math.Min(reserve.Settings.Skill.RequiredSP,reserve.SkillSP+6);d.Reinforcements++;
                    foreach(var u in units)if(CanDefenderTarget(u)&&GroundDistance(u.Motion.Position,reserve.Position)<=1.3f)TryDamageUnit(u.EntityId,DefenderSkillAttack(d)*2.5f,CombatDamageType.Physical);
                }
            }
            if(d.Settings.Skill.Kind==DefenderSkillKind.HoshigumaInferno){d.MaxHealth=d.Settings.Health*1.7f;d.Health*=1.7f;}
            return true;
        }
        void EndMon3tr(DefenderState d,bool lethal)
        {
            float ratio=Math.Max(0,d.Health/d.MaxHealth);d.Position=d.Settings.Position;d.HighGround=d.Position.y>.2f;d.MaxHealth=d.Settings.Health;
            d.Health=lethal?1:Math.Max(1,ratio*d.MaxHealth);d.SkillActiveUntil=Clock;d.SkillEndedAt=Clock;d.SkillActionUntil=Clock+.833333;d.NextAttackTime=d.SkillActionUntil;
            d.ProtectionUntil=Clock+1;d.FifthActive=false;d.TargetId=null;d.Transfers++;
        }
        void StepFifthDefenders(float dt)
        {
            foreach(var d in defenders)
            {
                if(!IsFifth(d)||!d.Alive)continue;
                var kind=d.Settings.Skill.Kind;
                if(kind==DefenderSkillKind.ExusiaiDelivery)d.Health=Math.Max(1,d.Health-d.MaxHealth*.01f*dt);
                if(kind==DefenderSkillKind.Mon3trMeltdown&&DefenderSkillActive(d))ApplyDefenderLoss(d,8*dt);
                if(kind==DefenderSkillKind.HoshigumaInferno)
                {
                    if(d.DebtLife&&Clock-d.LastReceivedDamage>=5)
                    {d.Debt=Math.Max(0,d.Debt-d.MaxHealth*.05f*dt);if(d.Debt==0){d.DebtLife=false;d.Health=Math.Max(1,d.MaxHealth*.05f*dt);}}
                    // AI closes while threatened in the second half, or to protect a wounded ally.
                    bool threatened=false;
                    foreach(var u in units)threatened|=CanDefenderTarget(u)&&GroundDistance(u.Motion.Position,d.Position)<=DefenderRange(d);
                    if(DefenderSkillActive(d)&&threatened&&Clock-d.SkillStartedAt>=16&&d.ProtectionUntil<d.SkillStartedAt)
                    {d.SkillActiveUntil=Clock;d.ProtectionUntil=Clock+11;d.ProtectedHits++;}
                    if(d.ProtectionUntil>double.NegativeInfinity&&Clock>=d.ProtectionUntil)
                    {d.Departed=true;d.Health=0;d.DebtLife=false;d.DeathTime=Clock;d.TargetId=null;continue;}
                }
                if(d.FifthActive&&!DefenderSkillActive(d)&&d.ProtectionUntil<=Clock)
                {
                    if(kind==DefenderSkillKind.Mon3trMeltdown)EndMon3tr(d,false);
                    else {d.FifthActive=false;d.SkillEndedAt=Clock;if(kind==DefenderSkillKind.HoshigumaInferno){d.Health/=1.7f;d.MaxHealth=d.Settings.Health;}}
                }
            }
        }
        void ApplyDefenderLoss(DefenderState d,float loss)
        {
            if(!d.Alive)return;
            // Lethal interception is bounded by the original incoming excess, without recursion.
            if(loss>=d.Health&&!d.DebtLife)
                foreach(var protector in defenders)
                    if(protector!=d&&protector.Alive&&protector.Settings.Skill.Kind==DefenderSkillKind.HoshigumaInferno&&protector.ProtectionUntil>Clock&&GroundDistance(protector.Position,d.Position)<=protector.Settings.Skill.EffectRadius)
                    {float excess=loss-Math.Max(0,d.Health-1);loss-=excess;float paid=Math.Min(protector.Health,excess);protector.Health-=paid;protector.Debt+=excess-paid;if(protector.Health<=0)protector.DebtLife=true;protector.ProtectedHits++;break;}
            if(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown&&loss>=d.Health)
            {if(DefenderSkillActive(d)){EndMon3tr(d,true);return;}if(d.ProtectionUntil>Clock){d.Health=1;return;}}
            if(d.Settings.Skill.Kind==DefenderSkillKind.HoshigumaInferno)
            {
                d.LastReceivedDamage=Clock;
                if(d.DebtLife){d.Debt+=loss;}
                else if(loss>=d.Health){d.Debt=loss-d.Health;d.Health=0;d.DebtLife=true;}
                else d.Health-=loss;
                if(d.DebtLife&&d.Debt>=d.MaxHealth*2&&d.ProtectionUntil<=Clock){d.DebtLife=false;d.Health=0;}
            }
            else d.Health=Math.Max(0,d.Health-loss);
        }
        void ScheduleMon3trChain(DefenderState d,DefenderState first,float travel)
        {
            var visited=new HashSet<string>{first.EntityId};var previous=first;float scale=.75f;
            for(int i=0;i<2;i++)
            {
                DefenderState next=null;
                foreach(var ally in defenders)if(ally.Alive&&!ally.DebtLife&&!visited.Contains(ally.EntityId)&&GroundDistance(previous.Position,ally.Position)<=2&&
                    (next==null||ally.Health/ally.MaxHealth<next.Health/next.MaxHealth))next=ally;
                if(next==null)break;visited.Add(next.EntityId);
                combatShots.Add(new CombatShot(d.EntityId,next.EntityId,true,false,previous.Position,next.Position,Clock+d.Settings.Windup,travel,DefenderSkillAttack(d)*scale,healing:true));
                previous=next;scale*=.75f;
            }
        }
        bool ApplyFifthTrueDamage(DeployedUnitState u,float amount)
        {if(!CanDefenderTarget(u)||UnitInvulnerable(u))return false;ApplyPureUnitDamage(u,amount);return true;}
        bool ScheduleFifthAttack(DefenderState d,DeployedUnitState primary,float travel)
        {
            if(!IsFifth(d)||!IsFifthActive(d))return false;
            if(Clock<d.NextAttackTime||Clock<d.SkillActionUntil)return true;
            if(d.Settings.Skill.Kind==DefenderSkillKind.ExusiaiDelivery&&d.Ammo<=0)return true;
            var kind=d.Settings.Skill.Kind;
            var targets=new List<DeployedUnitState>();
            if(primary!=null&&CanDefenderTarget(primary))targets.Add(primary);
            int max=kind==DefenderSkillKind.Mon3trMeltdown?3:kind==DefenderSkillKind.HoshigumaInferno?2:1;
            foreach(var u in units)if(targets.Count<max&&u!=primary&&CanDefenderTarget(u)&&GroundDistance(u.Motion.Position,d.Position)<=DefenderRange(d))targets.Add(u);
            if(targets.Count==0)return true;
            int hits=kind==DefenderSkillKind.ExusiaiDelivery?5:kind==DefenderSkillKind.HoshigumaInferno?(d.ProtectionUntil>Clock?4:2):1;
            d.LastActionTime=Clock;d.LastActionHealing=false;d.LastActionRate=1;d.NextAttackTime=Clock+(kind==DefenderSkillKind.Mon3trMeltdown?1.333333:d.Settings.Interval)/DefenderAttackRate(d);
            foreach(var u in targets)for(int hit=0;hit<hits;hit++)
            {
                var shot=new CombatShot(d.EntityId,u.EntityId,true,true,d.Position,u.Motion.Position,Clock+d.Settings.Windup+hit*(kind==DefenderSkillKind.ExusiaiDelivery?1d/30:.08),travel,
                    DefenderSkillAttack(d)*(kind==DefenderSkillKind.ExusiaiDelivery?1.35f:1),damageType:kind==DefenderSkillKind.HoshigumaInferno?CombatDamageType.Arts:CombatDamageType.Physical){TrueDamage=kind==DefenderSkillKind.Mon3trMeltdown};
                combatShots.Add(shot);if(shot.TrueDamage)d.TrueHits++;
            }
            if(kind==DefenderSkillKind.Mon3trMeltdown){float gain=Math.Min(d.MaxHealth-d.Health,DefenderSkillAttack(d)*.5f);d.Health+=gain;d.SkillHealthRestored+=gain;}
            if(kind==DefenderSkillKind.ExusiaiDelivery){d.Ammo-=5;if(d.Ammo==0){d.SkillActiveUntil=Clock+d.Settings.ActionDuration;}}
            return true;
        }
    }
}
