using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    internal enum SeventhDefenderEventKind { SkillStart, SkillEnd, StickyGround, FlightIntercept, CorrosionBurst, MedicalDetonation, HealingPulse, DawnWaveLaunched, DawnWaveTurned, DawnWaveEnded }
    internal readonly struct SeventhDefenderEvent
    {
        internal readonly SeventhDefenderEventKind Kind;
        internal readonly string SourceId,TargetId;
        internal readonly double At,EndsAt;
        internal readonly Vector3 Position,Direction;
        internal readonly int RunId,Sequence;
        internal SeventhDefenderEvent(SeventhDefenderEventKind kind,string source,string target,double at,double ends,Vector3 position,int run,int sequence,Vector3 direction=default)
        {Kind=kind;SourceId=source;TargetId=target;At=at;EndsAt=ends;Position=position;Direction=direction;RunId=run;Sequence=sequence;}
    }

    public sealed partial class ManualBattle
    {
        sealed class SeventhField { internal string SourceId,TargetId;internal Vector3 Position;internal double At,Until,ActionAt,ReleaseAt;internal float Attack,Radius;internal bool Medical,MedicalEnemy,MedicalSub,MedicalSubHeal,Corrosion,CorrosionCore,Resolved; }
        sealed class CorrosionState { internal float Accumulated,ArmorReduction;internal double ImmuneUntil; }
        readonly List<SeventhDefenderEvent> seventhEvents=new List<SeventhDefenderEvent>();
        readonly List<SeventhField> seventhFields=new List<SeventhField>();
        readonly List<SeventhField> medicalSubmissions=new List<SeventhField>();
        readonly Dictionary<string,CorrosionState> seventhCorrosion=new Dictionary<string,CorrosionState>();
        int seventhSequence;
        internal IReadOnlyList<SeventhDefenderEvent> SeventhDefenderEvents=>seventhEvents.AsReadOnly();
        public bool IsSeventh(DefenderState d)=>d!=null&&((int)d.Settings.Skill.Kind>=(int)DefenderSkillKind.ChenHolungday&&(int)d.Settings.Skill.Kind<=(int)DefenderSkillKind.KaltsitEsperanta||d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak);
        internal bool SeventhUsesAmmo(DefenderState d)=>IsSeventh(d)&&d.Settings.Skill.Kind!=DefenderSkillKind.ChenDawnstreak;
        void ResetSeventhDefenders(){seventhEvents.Clear();seventhFields.Clear();medicalSubmissions.Clear();seventhCorrosion.Clear();seventhSequence=0;ResetChenDawn();}
        void SeventhEvent(SeventhDefenderEventKind kind,DefenderState d,string target,Vector3 position,double at,double until,Vector3 direction=default)
        {
            // A small presentation journal, never a second damage source. Replays are scoped to RunId.
            seventhEvents.Add(new SeventhDefenderEvent(kind,d.EntityId,target,at,until,position,RunId,++seventhSequence,direction));
            if(seventhEvents.Count>256)seventhEvents.RemoveRange(0,seventhEvents.Count-256);
        }
        CombatShot QueueDefenderShot(DefenderState d,CombatShot shot,double? actionAt=null,float? actionRate=null)
        {
            shot.DefenderActionStartedAt=actionAt??(shot.Drone&&shot.Empowered?Clock:d.LastActionTime);
            shot.DefenderActionRate=actionRate??(shot.Drone&&shot.Empowered?1:d.LastActionRate);
            shot.DefenderSkillWasActive=DefenderSkillActive(d)||IsFifthActive(d);
            combatShots.Add(shot);return shot;
        }
        bool BeginSeventhSkill(DefenderState d)
        {
            if(!IsSeventh(d))return false;
            if(d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak){BeginChenDawnSkill(d);return true;}
            d.AdvancedSkillWasActive=true;d.SkillActionUntil=Clock+SeventhStartup(d.Settings.Skill.Kind);d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);
            switch(d.Settings.Skill.Kind){case DefenderSkillKind.ChenHolungday:d.Ammo=32;break;case DefenderSkillKind.AngelinaWishes:d.Ammo=31;break;default:d.Ammo=10;break;}
            SeventhEvent(SeventhDefenderEventKind.SkillStart,d,null,d.Position,Clock,d.SkillActiveUntil);return true;
        }
        static float SeventhStartup(DefenderSkillKind kind)=>kind==DefenderSkillKind.ChenDawnstreak?13f/30:kind==DefenderSkillKind.ChenHolungday?.1f:kind==DefenderSkillKind.AngelinaWishes?1:kind==DefenderSkillKind.ClemensiaCorrosion?.2f:2f/3;
        static float SeventhEnd(DefenderSkillKind kind)=>kind==DefenderSkillKind.ChenDawnstreak?8f/30:kind==DefenderSkillKind.ChenHolungday?.1f:kind==DefenderSkillKind.AngelinaWishes?1:kind==DefenderSkillKind.ClemensiaCorrosion?.2f:2f/3;
        float SeventhAttackWindup(DefenderState d)=>d.Settings.Skill.Kind==DefenderSkillKind.ChenHolungday?.5f:d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes?(DefenderSkillActive(d)?.5f:.4f):d.Settings.Skill.Kind==DefenderSkillKind.ClemensiaCorrosion?(DefenderSkillActive(d)?13f/30:11f/30):8f/15;
        void FinishSeventhAmmo(DefenderState d)
        {if(d.Ammo<=0)d.SkillActiveUntil=Math.Min(d.SkillActiveUntil,Math.Max(Clock+.01,d.LastActionTime+d.Settings.ActionDuration/d.LastActionRate));}
        void StepSeventhDefenders(float step)
        {
            StepChenDawnWaves(step);
            foreach(var d in defenders)
            {
                if(!IsSeventh(d))continue;
                if(d.AdvancedSkillWasActive&&!DefenderSkillActive(d))
                {d.AdvancedSkillWasActive=false;d.SkillEndedAt=Clock;d.SkillActionUntil=Math.Max(d.SkillActionUntil,Clock+SeventhEnd(d.Settings.Skill.Kind));d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);SeventhEvent(SeventhDefenderEventKind.SkillEnd,d,null,d.Position,Clock,Clock);}
                bool moving=StepSeventhFlight(d,step);
                if(moving&&!d.SeventhMoving){d.SeventhMoveStartedAt=Clock;d.SeventhMoveEndedAt=double.NegativeInfinity;}
                else if(!moving&&d.SeventhMoving)
                {
                    d.SeventhMoveEndedAt=Clock;
                    if(d.Alive&&DefenderSkillActive(d)){d.SkillActionUntil=Math.Max(d.SkillActionUntil,Clock+1f/3);d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);}
                }
                d.SeventhMoving=moving;
            }
            medicalSubmissions.Clear();
            // Resolve scheduled medical/elemental detonations before the ordinary projectile pass.
            foreach(var f in seventhFields)
            {
                if(f.Resolved||Clock+1e-6<f.At||!f.Medical&&!f.MedicalSub&&!f.Corrosion&&!f.CorrosionCore)continue;
                f.Resolved=true;var d=FindDefender(f.SourceId);if(d==null||f.Medical&&!d.Alive&&d.DeathTime<f.ReleaseAt)continue;
                if(f.Medical)
                {
                    var medicalEnemy=FindUnit(f.TargetId);var medicalAlly=FindDefender(f.TargetId);
                    if(CanDefenderTarget(medicalEnemy))f.Position=medicalEnemy.Motion.Position;else if(medicalAlly!=null&&medicalAlly.Alive)f.Position=medicalAlly.Position;
                    SeventhEvent(SeventhDefenderEventKind.MedicalDetonation,d,f.TargetId,f.Position,Clock,Clock+1);
                    if(f.MedicalEnemy)QueueMedicalDamage(d,f,true);else QueueMedicalHealing(d,f);
                    // Source projectile action emits the opposite sub-projectile, which settles on its real .4 second lifetime.
                    medicalSubmissions.Add(new SeventhField{SourceId=f.SourceId,TargetId=f.TargetId,Position=f.Position,At=Clock+.4,Until=Clock+1.4,ActionAt=f.ActionAt,Attack=f.Attack,MedicalSub=true,MedicalSubHeal=f.MedicalEnemy});
                }
                else if(f.MedicalSub)
                {if(f.MedicalSubHeal)QueueMedicalHealing(d,f);else QueueMedicalDamage(d,f,false);}
                else if(f.CorrosionCore)
                {
                    var victim=FindUnit(f.TargetId);if(CanDefenderTarget(victim))QueueDefenderShot(d,new CombatShot(d.EntityId,victim.EntityId,true,true,f.Position,victim.Motion.Position,Clock,0,500){TrueDamage=true,SeventhSecondary=true,PersistAfterSourceDeath=true},f.ActionAt,1);
                }
                else
                {
                    SeventhEvent(SeventhDefenderEventKind.CorrosionBurst,d,f.TargetId,f.Position,Clock,Clock+1);
                    foreach(var u in units)if(CanDefenderTarget(u)&&!UnitAirborne(u)&&SeventhCrossContains(f.Position,u.Motion.Position))
                    {
                        for(int hit=0;hit<3;hit++)QueueDefenderShot(d,new CombatShot(d.EntityId,u.EntityId,true,true,f.Position,u.Motion.Position,Clock+hit*.1,0,f.Attack*2.7f){SeventhSecondary=true,PersistAfterSourceDeath=true},f.ActionAt,1);
                        // There is no elemental-resistance stat in this prototype. The separately labelled elemental component bypasses physical/arts armor.
                        QueueDefenderShot(d,new CombatShot(d.EntityId,u.EntityId,true,true,f.Position,u.Motion.Position,Clock+.2,0,f.Attack*2){TrueDamage=true,SeventhSecondary=true,PersistAfterSourceDeath=true},f.ActionAt,1);
                    }
                }
            }
            seventhFields.AddRange(medicalSubmissions);
            seventhFields.RemoveAll(f=>f.Until<Clock&&(f.Resolved||!f.Medical&&!f.MedicalSub&&!f.Corrosion&&!f.CorrosionCore));
        }
        bool StepSeventhFlight(DefenderState d,float step)
        {
            if(!d.Alive||!DefenderSkillActive(d)||d.Settings.Skill.Kind!=DefenderSkillKind.AngelinaWishes||d.BlockedCount>0||d.NeuralUntil>Clock||d.FrozenUntil>Clock||DefenderShackled(d)||Clock<d.SkillActionUntil||Clock<d.LastActionTime+d.Settings.ActionDuration/d.LastActionRate)return false;
            // The course game has fixed lanes. Only a legal, unblocked aerial target inside the authored range can be intercepted.
            DeployedUnitState closest=null;
            foreach(var u in units)if(CanDefenderTarget(u)&&UnitAirborne(u)&&!BossRosterUnblockable(u)&&!u.IsBlocked&&DefenderSkillContains(d,u.Motion.Position)&&
                (closest==null||GroundDistance(d.Position,u.Motion.Position)<GroundDistance(d.Position,closest.Motion.Position)))closest=u;
            if(closest==null||GroundDistance(d.Position,closest.Motion.Position)<=d.Settings.BlockRadius)return false;
            if(!d.SeventhMoving||d.SeventhInterceptTarget!=closest.EntityId){d.SeventhInterceptTarget=closest.EntityId;SeventhEvent(SeventhDefenderEventKind.FlightIntercept,d,closest.EntityId,closest.Motion.Position,Clock,Clock+1);}
            var destination=closest.Motion.Position;destination.y=d.Position.y;d.Position=Vector3.MoveTowards(d.Position,destination,3*step);
            return true;
        }
        void QueueMedicalDamage(DefenderState d,SeventhField f,bool hinder)
        {
            foreach(var u in units)if(CanDefenderTarget(u)&&GroundDistance(f.Position,u.Motion.Position)<=1.5f)
                QueueDefenderShot(d,new CombatShot(d.EntityId,u.EntityId,true,true,f.Position,u.Motion.Position,Clock,0,f.Attack*3.5f){TrueDamage=true,HinderDuration=hinder?5:0,SeventhSecondary=true,PersistAfterSourceDeath=true},f.ActionAt,1);
        }
        void QueueMedicalHealing(DefenderState d,SeventhField f)
        {
            foreach(var ally in defenders)if(ally.Alive&&!ally.DebtLife&&ally.Health<ally.MaxHealth-.001f&&GroundDistance(f.Position,ally.Position)<=1.5f)
                QueueDefenderShot(d,new CombatShot(d.EntityId,ally.EntityId,true,true,f.Position,ally.Position,Clock,0,f.Attack*1.7f,healing:true){SeventhSecondary=true,PersistAfterSourceDeath=true},f.ActionAt,1);
        }
        static bool SeventhCrossContains(Vector3 center,Vector3 position)
        {float x=Mathf.Abs(position.x-center.x),z=Mathf.Abs(position.z-center.z);return x<=.5f&&z<=1.5f||z<=.5f&&x<=1.5f;}
        internal bool SeventhFlying(DefenderState d)=>d!=null&&d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&DefenderSkillActive(d);
        internal int SeventhAmmoCapacity(DefenderState d)=>!SeventhUsesAmmo(d)?0:d.Settings.Skill.Kind==DefenderSkillKind.ChenHolungday?32:d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes?31:10;
        internal int SeventhBlockCapacity(DefenderState d)=>SeventhFlying(d)?2:d.Settings.BlockCount;
        internal int DefenderGroundBlockCapacity(DefenderState d)=>d.HighGround||!DefenderCanBlock(d)||SeventhFlying(d)?0:SeventhBlockCapacity(d);
        internal int DefenderAirBlockCapacity(DefenderState d)=>SeventhFlying(d)&&DefenderCanBlock(d)?SeventhBlockCapacity(d):0;
        bool SeventhVisibleToAttacker(DefenderState d,DeployedUnitState u)=>!(SeventhFlying(d)&&!UnitAirborne(u));
        bool SeventhCanBlock(DefenderState d,DeployedUnitState u)=>UnitAirborne(u)?SeventhFlying(d):!SeventhFlying(d);
        float SeventhDamageMultiplier(DefenderState d,DeployedUnitState source)=>d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&DefenderSkillActive(d)&&source!=null&&UnitAirborne(source)?.4f:1;
        float SeventhArmorReduction(DeployedUnitState u)
        {
            if(u==null)return 0;
            float corrosion=seventhCorrosion.TryGetValue(u.EntityId,out var state)?state.ArmorReduction:0;
            if(UnitAirborne(u))return corrosion;
            foreach(var f in seventhFields)if(!f.Medical&&!f.MedicalSub&&!f.Corrosion&&!f.CorrosionCore&&Clock>=f.At&&Clock<f.Until&&GroundDistance(f.Position,u.Motion.Position)<=f.Radius)return corrosion+20; // Original 200 DEF at this trial's 1:10 stat scale; non-stacking.
            return corrosion;
        }
        float SeventhMoveMultiplier(DeployedUnitState u)
        {
            if(u==null||UnitControlImmune(u))return 1;
            if(!UnitAirborne(u))
            {
                foreach(var f in seventhFields)if(!f.Medical&&!f.MedicalSub&&!f.Corrosion&&!f.CorrosionCore&&Clock>=f.At&&Clock<f.Until&&GroundDistance(f.Position,u.Motion.Position)<=f.Radius)return .6f;
                return 1;
            }
            foreach(var d in defenders)if(d.Alive&&d.Settings.Skill.Kind==DefenderSkillKind.AngelinaWishes&&DefenderSkillActive(d)&&DefenderRangeContains(d,u.Motion.Position))return .6f;
            return 1;
        }
        bool StepSeventhDefenderAction(DefenderState d)
        {
            if(!IsSeventh(d)||d.Settings.Skill.Kind!=DefenderSkillKind.KaltsitEsperanta||!DefenderSkillActive(d))return false;
            if(d.Ammo<=0||Clock+1e-6<d.NextAttackTime)return true;
            DeployedUnitState enemy=null;
            foreach(var u in units)if(CanDefenderTarget(u)&&DefenderRangeContains(d,u.Motion.Position)&&PreferDefenderTarget(d,u,enemy))enemy=u;
            DefenderState ally=null;
            if(enemy==null)foreach(var a in defenders)if(a.Alive&&!a.DebtLife&&a.Health<a.MaxHealth-.001f&&DefenderRangeContains(d,a.Position)&&(ally==null||a.Health/a.MaxHealth<ally.Health/ally.MaxHealth))ally=a;
            if(enemy==null&&ally==null)return true;
            d.TargetId=enemy?.EntityId??ally.EntityId;d.LastActionTime=Clock;d.LastActionRate=1;d.LastActionHealing=enemy==null;d.LastAttackSkill=true;d.NextAttackTime=Clock+d.Settings.Interval/DefenderAttackRate(d);
            var at=Clock+SeventhAttackWindup(d)+CombatSettings.ProjectileTime;
            seventhFields.Add(new SeventhField{SourceId=d.EntityId,TargetId=d.TargetId,Position=enemy?.Motion.Position??ally.Position,At=at,Until=at+1,ActionAt=Clock,ReleaseAt=Clock+SeventhAttackWindup(d),Attack=DefenderSkillAttack(d),Medical=true,MedicalEnemy=enemy!=null});
            // This launch is a real medical projectile. Area recipients are evaluated at impact, so targets cannot be hit at their old positions.
            SeventhEvent(SeventhDefenderEventKind.HealingPulse,d,d.TargetId,enemy?.Motion.Position??ally.Position,Clock+SeventhAttackWindup(d),at);
            d.Ammo--;FinishSeventhAmmo(d);return true;
        }
        bool ScheduleSeventhAttack(DefenderState d,DeployedUnitState primary,float travel)
        {
            if(d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak)return ScheduleChenDawnAttack(d,primary);
            if(!IsSeventh(d)||d.Settings.Skill.Kind==DefenderSkillKind.KaltsitEsperanta)return false;
            bool active=DefenderSkillActive(d);var kind=d.Settings.Skill.Kind;
            if(active&&d.Ammo<=0)return true;
            d.LastActionTime=Clock;d.LastActionRate=1;d.LastActionHealing=false;d.LastAttackSkill=active;d.NextAttackTime=Clock+d.Settings.Interval/DefenderAttackRate(d);
            var targets=new List<DeployedUnitState>{primary};
            int maximum=kind==DefenderSkillKind.ChenHolungday?int.MaxValue:active?3:1;
            foreach(var u in units)if(targets.Count<maximum&&u!=primary&&CanDefenderTarget(u)&&DefenderRangeContains(d,u.Motion.Position))targets.Add(u);
            if(active&&kind==DefenderSkillKind.AngelinaWishes)
                foreach(var u in units)if(!targets.Contains(u)&&CanDefenderTarget(u)&&UnitAirborne(u)&&DefenderRangeContains(d,u.Motion.Position)){targets.Add(u);break;}
            int hits=active&&kind==DefenderSkillKind.ChenHolungday?2:1;
            foreach(var u in targets)for(int hit=0;hit<hits;hit++)
            {
                float scale=kind==DefenderSkillKind.ChenHolungday?(active||GroundDistance(d.Position,u.Motion.Position)<=1.5f?1.5f:1):active&&kind==DefenderSkillKind.AngelinaWishes?3.05f:active&&kind==DefenderSkillKind.ClemensiaCorrosion?2:1;
                QueueDefenderShot(d,new CombatShot(d.EntityId,u.EntityId,true,active,d.Position,u.Motion.Position,Clock+SeventhAttackWindup(d)+hit*.1f,travel,DefenderSkillAttack(d)*scale));
            }
            ShotsFired++;LastShotTime=Clock;LastShotEntityId=primary.EntityId;LastShotPosition=primary.Motion.Position;d.SeventhAttacks++;
            if(active&&kind==DefenderSkillKind.ChenHolungday)
            {
                double at=Clock+SeventhAttackWindup(d)+travel;var p=d.Position;
                seventhFields.Add(new SeventhField{SourceId=d.EntityId,Position=p,At=at,Until=at+5,Radius=DefenderRange(d)});
                SeventhEvent(SeventhDefenderEventKind.StickyGround,d,primary.EntityId,p,at,at+5,primary.Motion.Position-p);d.Ammo-=2;
            }
            else if(active&&kind==DefenderSkillKind.AngelinaWishes)d.Ammo--;
            FinishSeventhAmmo(d);return true;
        }
        void OnSeventhDefenderHit(DefenderState source,DeployedUnitState target,CombatShot shot,float healthLost)
        {
            if(!IsSeventh(source)||source.Settings.Skill.Kind!=DefenderSkillKind.ClemensiaCorrosion||!shot.Empowered||shot.SeventhSecondary||healthLost<=0)return;
            if(!seventhCorrosion.TryGetValue(target.EntityId,out var state)){state=new CorrosionState();seventhCorrosion.Add(target.EntityId,state);}
            if(Clock<state.ImmuneUntil)return;
            state.Accumulated+=healthLost*.2f;
            // Standard enemies use a 1000-point bar in the source. This course trial uses 1:10 combat stats.
            var effects=UnitCombat(target).Effects;
            bool boss=BossRosterCatalog.Contains(target.CardId)||effects.Patriot||effects.FrostNova||effects.Mandragora||effects.Mudrock||UnitCombat(target).FireRain;
            if(state.Accumulated<(boss?200:100))return;
            state.Accumulated=0;state.ImmuneUntil=Clock+8;state.ArmorReduction+=12;
            seventhFields.Add(new SeventhField{SourceId=source.EntityId,TargetId=target.EntityId,Position=target.Motion.Position,At=Clock,Until=Clock+1,ActionAt=shot.DefenderActionStartedAt,CorrosionCore=true});
            foreach(var d in defenders)if(d.Alive&&d.Settings.Skill.Kind==DefenderSkillKind.ClemensiaCorrosion&&DefenderSkillActive(d)&&d.Ammo>0)
            {d.Ammo--;d.SeventhBursts++;seventhFields.Add(new SeventhField{SourceId=d.EntityId,TargetId=target.EntityId,Position=target.Motion.Position,At=Clock,Until=Clock+1,ActionAt=Clock,Attack=DefenderSkillAttack(d),Corrosion=true});FinishSeventhAmmo(d);}
        }
    }
}
