using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public readonly struct BossRosterRampageInfo
    {
        public readonly float Accumulated,Threshold,TickDamage;public readonly double Until;public readonly int Bursts;public readonly bool Immune;
        internal BossRosterRampageInfo(float accumulated,float tick,double until,int bursts,bool immune){Accumulated=accumulated;Threshold=100;TickDamage=tick;Until=until;Bursts=bursts;Immune=immune;}
    }
    public sealed partial class ManualBattle
    {
        sealed class HoundRuntime
        {internal int Mode,Generation,Bite;internal double NextJumpAt,NextMeleeAt,Until,NextBiteAt;internal Vector3 Home;}
        sealed class HoundDefender
        {internal float Rampage,TickDamage;internal double AccuracyUntil,RampageUntil,NextTickAt,LastAction=double.NegativeInfinity;internal int Bursts,LastCasts;internal string Source;}
        readonly Dictionary<string,HoundRuntime> bossHounds=new Dictionary<string,HoundRuntime>();
        readonly Dictionary<string,HoundDefender> bossHoundDefenders=new Dictionary<string,HoundDefender>();
        readonly HashSet<string> bossRampageImmune=new HashSet<string>(StringComparer.Ordinal);
        void ResetBossHounds(){bossHounds.Clear();bossHoundDefenders.Clear();}
        HoundRuntime HoundState(BossRosterObject o)
        {if(!bossHounds.TryGetValue(o.Id,out var s)){s=new HoundRuntime{NextJumpAt=o.StartedAt+15};bossHounds.Add(o.Id,s);}return s;}
        HoundDefender HoundTarget(DefenderState d)
        {if(!bossHoundDefenders.TryGetValue(d.EntityId,out var s)){s=new HoundDefender();bossHoundDefenders.Add(d.EntityId,s);}return s;}
        public void ConfigureBossRosterRampageImmunity(IEnumerable<string> defenderIds)
        {if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Configure authored element immunity before deployment.");bossRampageImmune.Clear();foreach(var id in defenderIds??Array.Empty<string>())if(!string.IsNullOrEmpty(id))bossRampageImmune.Add(id);}
        public BossRosterRampageInfo BossRosterRampage(DefenderState d)
        {bool immune=d!=null&&bossRampageImmune.Contains(d.EntityId);return d!=null&&bossHoundDefenders.TryGetValue(d.EntityId,out var s)?new BossRosterRampageInfo(s.Rampage,s.TickDamage,s.RampageUntil,s.Bursts,immune):new BossRosterRampageInfo(0,0,0,0,immune);}
        public float BossRosterDefenderHitChance(DefenderState d,CombatDamageType type)=>d!=null&&(type==CombatDamageType.Physical||type==CombatDamageType.Arts)&&bossHoundDefenders.TryGetValue(d.EntityId,out var s)&&Clock<s.AccuracyUntil?.2f:1;
        public bool BossRosterDefenderAttackHits(DefenderState d,CombatDamageType type)
        {float chance=BossRosterDefenderHitChance(d,type);return chance>=1||d.Roll()<chance;}
        public float BossRosterDefenderAttackSpeedBonus(DefenderState d)=>d!=null&&bossHoundDefenders.TryGetValue(d.EntityId,out var s)&&Clock<s.RampageUntil?.5f:0;
        public bool TryControlBossRosterObject(int runId,string objectId,float duration)
        {
            if(runId!=RunId||IsPaused||Phase!=BattlePhase.Running||duration<=0||float.IsNaN(duration)||float.IsInfinity(duration))return false;
            var o=bossRosterObjects.Find(x=>x.Id==objectId&&x.Alive&&x.Kind=="hound");if(o==null)return false;var state=HoundState(o);
            // The original Jump graph owns stun/freeze/disarm/fear immunity through
            // Begin/Channel/End. This does not grant immunity to the normal hound.
            if(state.Mode!=0)return false;state.Generation++;o.ControlledUntil=Math.Max(o.ControlledUntil,Clock+duration);o.Animation="Idle";o.AnimationAt=Clock;o.AnimationUntil=o.ControlledUntil;o.AnimationLoop=true;return true;
        }
        void StepBossHound(DeployedUnitState owner,BossRosterObject o,float step)
        {
            var s=HoundState(o);
            if(s.Mode==1)
            {var d=FindDefender(o.TargetId);if(d==null||!d.Alive){EndBossHound(owner,o,s,false);return;}if(Clock+1e-6<o.AnimationUntil)return;s.Mode=2;s.Until=Clock+10;s.NextBiteAt=Clock+1;o.Position=o.TargetPosition=d.Position;o.Animation="Skill_Loop";o.AnimationAt=Clock;o.AnimationUntil=s.Until;o.AnimationLoop=true;BossEmit(owner,"StateStart","attachBite",d.Position,native:"jump",animation:o.Animation,activation:o.ActivationId,targetId:d.EntityId,end:s.Until,origin:o.Position);}
            if(s.Mode==2)
            {
                var d=FindDefender(o.TargetId);if(d==null||!d.Alive){EndBossHound(owner,o,s,false);return;}o.Position=o.TargetPosition=d.Position;
                while(s.NextBiteAt<=Clock+1e-6&&s.NextBiteAt<=s.Until+1e-6&&d.Alive)
                {s.NextBiteAt+=1;o.ShotAt=o.ImpactAt=Clock;BossHoundContact(owner,o,d,"attachBite",s.Bite++%3);}
                if(!d.Alive||Clock+1e-6>=s.Until)EndBossHound(owner,o,s,false);return;
            }
            if(s.Mode==3){if(Clock+1e-6<o.AnimationUntil)return;s.Mode=0;s.NextJumpAt=Clock+40;o.TargetId=null;o.Animation="Idle";o.AnimationAt=Clock;o.AnimationLoop=true;}
            if(Clock<o.ControlledUntil)return;
            if(Clock<o.AnimationUntil&&!o.AnimationLoop)return;
            if(Clock+1e-6>=s.NextJumpAt)
            {var d=BossLiving().Where(d=>GroundDistance(d.Position,o.Position)<=2).OrderBy(d=>GroundDistance(d.Position,o.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();if(d!=null){s.Mode=1;s.Generation++;s.Home=o.Position;s.Bite=0;o.TargetId=d.EntityId;o.TargetPosition=d.Position;o.Animation="Skill_Begin";o.AnimationAt=o.AttackStartedAt=Clock;o.AnimationUntil=Clock+.1666666716;o.AnimationLoop=false;o.ActivationId=++bossActivationSequence;BossEmit(owner,"SkillStart","attachBite",d.Position,native:"jump",animation:o.Animation,activation:o.ActivationId,targetId:d.EntityId,end:Clock+10.5,origin:o.Position);return;}}
            var target=BossLiving().Where(d=>!d.HighGround&&d.Settings.BlockCount>0&&DefenderCanBlock(d)).OrderBy(d=>GroundDistance(d.Position,o.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();if(target==null)return;
            if(GroundDistance(o.Position,target.Position)>.55f)
            {o.Position=Vector3.MoveTowards(o.Position,target.Position,1.3f*step);if(o.Animation!="Move"){o.Animation="Move";o.AnimationAt=Clock;o.AnimationLoop=true;}return;}
            if(Clock+1e-6<s.NextMeleeAt){if(o.Animation!="Idle"){o.Animation="Idle";o.AnimationAt=Clock;o.AnimationLoop=true;}return;}
            s.NextMeleeAt=Clock+1.8;o.Animation="Attack";o.AnimationAt=o.AttackStartedAt=Clock;o.AnimationUntil=Clock+1.4;o.AnimationLoop=false;o.ActivationId=++bossActivationSequence;long activation=o.ActivationId;int generation=s.Generation;
            BossEmit(owner,"AttackStart","hound",target.Position,native:"Attack",animation:"Attack",activation:activation,targetId:target.EntityId,end:o.AnimationUntil,origin:o.Position);
            BossAfter(owner,.6666666865,()=>{if(!o.Alive||s.Generation!=generation||s.Mode!=0||Clock<o.ControlledUntil||!target.Alive||GroundDistance(o.Position,target.Position)>.65f)return;o.ShotAt=o.ImpactAt=Clock;BossHoundContact(owner,o,target,"hound",0);});
        }
        void BossHoundContact(DeployedUnitState owner,BossRosterObject o,DefenderState d,string key,int hit)
        {
            float attack=UnitCombat(owner).UnitAttack*UnitAttackMultiplier(owner)*(280f/900f),before=d.Health,debt=d.Debt,total=defenders.Sum(x=>x.Health);DamageDefenderFrom(d,attack,CombatDamageType.Arts,owner,derived:true);
            bool applied=d.Health<before||d.Debt>debt||defenders.Sum(x=>x.Health)<total;BossEmit(owner,"Impact",key,d.Position,native:key=="attachBite"?"jump":"Attack",activation:o.ActivationId,hit:hit,targetId:d.EntityId,applied:applied,origin:o.Position);
            if(!applied||!d.Alive)return;if(key=="attachBite")HoundTarget(d).AccuracyUntil=Math.Max(HoundTarget(d).AccuracyUntil,Clock+1.5);AddBossHoundRampage(owner,d,attack*.3f);
        }
        void EndBossHound(DeployedUnitState owner,BossRosterObject o,HoundRuntime s,bool dead)
        {
            if(s.Mode==0)return;long activation=o.ActivationId;s.Generation++;s.Mode=dead?0:3;s.NextJumpAt=Clock+40;if(!dead)o.Position=s.Home;o.Animation=dead?"Die":"Skill_End";o.AnimationAt=Clock;o.AnimationUntil=Clock+(dead?1:.3333333433);o.AnimationLoop=false;
            BossEmit(owner,"SkillEnd","attachBite",o.Position,native:"jump",animation:o.Animation,activation:activation,targetId:o.TargetId,cancelled:dead,origin:o.Position);if(dead)o.TargetId=null;
        }
        void AddBossHoundRampage(DeployedUnitState owner,DefenderState d,float amount)
        {
            if(!d.Alive||bossRampageImmune.Contains(d.EntityId)||amount<=0)return;var s=HoundTarget(d);if(Clock<s.RampageUntil)return;s.Source=owner.EntityId;s.Rampage=Math.Min(100,s.Rampage+amount);if(s.Rampage<100-.00001f)return;
            // Existing game element accumulators use 100 rather than original 1000.
            // Source burst 100/+50/cap600 is scaled by the same explicit /10 here.
            s.Rampage=0;s.RampageUntil=Clock+15;s.NextTickAt=Clock+1;s.TickDamage=10;s.LastAction=d.LastActionTime;s.LastCasts=d.SkillCasts;s.Bursts++;
            BossEmit(owner,"StateStart","rampage",d.Position,native:"ANGER",targetId:d.EntityId,applied:true,end:s.RampageUntil);
        }
        void StepBossHoundStatuses()
        {
            foreach(var pair in bossHoundDefenders)
            {
                var d=FindDefender(pair.Key);var s=pair.Value;if(d==null||!d.Alive)continue;if(s.RampageUntil<=0)continue;
                if(d.LastActionTime>s.LastAction){s.LastAction=d.LastActionTime;s.TickDamage=Math.Min(60,s.TickDamage+5);}
                if(d.SkillCasts>s.LastCasts){s.TickDamage=Math.Min(60,s.TickDamage+5*(d.SkillCasts-s.LastCasts));s.LastCasts=d.SkillCasts;}
                while(s.NextTickAt<=Clock+1e-6&&s.NextTickAt<=s.RampageUntil+1e-6&&d.Alive)
                {s.NextTickAt+=1;float hp=d.Health,debt=d.Debt,total=defenders.Sum(x=>x.Health);SourcedTrueDamage(d,s.TickDamage,null);var owner=FindUnit(s.Source);if(owner!=null)BossEmit(owner,"Impact","rampage",d.Position,native:"ANGER",targetId:d.EntityId,applied:d.Health<hp||d.Debt>debt||defenders.Sum(x=>x.Health)<total);}
                if(Clock+1e-6>=s.RampageUntil){s.RampageUntil=0;s.TickDamage=0;var owner=FindUnit(s.Source);if(owner!=null)BossEmit(owner,"StateEnd","rampage",d.Position,native:"ANGER",targetId:d.EntityId);}
            }
        }
    }
}
