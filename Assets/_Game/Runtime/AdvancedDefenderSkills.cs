using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class DefenderDroneState
    {
        public string SourceId {get;internal set;}
        public string TargetId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public int Index {get;internal set;}
        public bool Locked {get;internal set;}
        public float Ramp {get;internal set;}=.2f;
        internal double NextAttackAt;
    }

    // Four selected S3s, isolated from old skills. Authored configurations are never mutated.
    public sealed partial class ManualBattle
    {
        readonly List<DefenderDroneState> defenderDrones=new List<DefenderDroneState>();
        public IReadOnlyList<DefenderDroneState> DefenderDrones=>defenderDrones.AsReadOnly();
        public bool IsAdvancedDefender(DefenderState d)=>d!=null&&(int)d.Settings.Skill.Kind>=(int)DefenderSkillKind.LapplandHavoc;
        public float DefenderRange(DefenderState d)=>DefenderSkillActive(d)&&d.Settings.Skill.EffectRadius>0&&
            (d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory||d.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification)?d.Settings.Skill.EffectRadius:d.Settings.Range;
        public bool DefenderCanBlock(DefenderState d)=>d.Settings.Skill.Kind!=DefenderSkillKind.MlynarGlory||DefenderSkillActive(d);
        public bool DefenderCanAttack(DefenderState d)=>d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory?DefenderSkillActive(d):d.Settings.CanAttack;
        float LiberatorAttackMultiplier(DefenderState d)=>d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory?1+d.LiberatorBonus:1;
        public float DefenderCharge(DefenderState d)=>d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory?
            Mathf.Clamp01((float)(Clock-d.ChargeStartedAt)/Mathf.Max(.01f,d.Settings.Skill.ChargeSeconds)):0;
        bool AdvancedSkillUseful(DefenderState d)
        {
            var kind=d.Settings.Skill.Kind;if(!IsAdvancedDefender(d))return false;
            foreach(var u in units)if(CanDefenderTarget(u)&&(kind==DefenderSkillKind.LapplandHavoc||kind==DefenderSkillKind.YuKitchen||GroundDistance(d.Position,u.Motion.Position)<=d.Settings.Skill.EffectRadius))return true;
            if(kind==DefenderSkillKind.SariaCalcification||kind==DefenderSkillKind.YuKitchen)
                foreach(var ally in defenders)if(ally.Alive&&ally.Health<ally.MaxHealth-.001f&&GroundDistance(d.Position,ally.Position)<=d.Settings.Skill.EffectRadius)return true;
            return false;
        }
        void BeginAdvancedDefenderSkill(DefenderState d)
        {
            if(!IsAdvancedDefender(d))return;
            var s=d.Settings.Skill;d.AdvancedSkillWasActive=true;
            d.SkillActionUntil=Clock+(s.Kind==DefenderSkillKind.YuKitchen?1.6:s.Kind==DefenderSkillKind.LapplandHavoc?1:s.Kind==DefenderSkillKind.SariaCalcification?.7:1d/3);
            d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);d.NextAuraAt=d.SkillActionUntil;
            if(s.Kind==DefenderSkillKind.YuKitchen)
            {
                d.MaxHealth=d.Settings.Health*s.HealthMultiplier;d.Health*=s.HealthMultiplier;
                DeployedUnitState nearest=null;
                foreach(var u in units)if(CanDefenderTarget(u)&&(nearest==null||GroundDistance(d.Position,u.Motion.Position)<GroundDistance(d.Position,nearest.Motion.Position)))nearest=u;
                var direction=nearest==null?Vector3.left:nearest.Motion.Position-d.Position;direction.y=0;
                d.WallNormal=Mathf.Abs(direction.x)>=Mathf.Abs(direction.z)?new Vector3(Mathf.Sign(direction.x),0,0):new Vector3(0,0,Mathf.Sign(direction.z));
            }
            if(s.Kind==DefenderSkillKind.MlynarGlory)d.LiberatorBonus=4*DefenderCharge(d);
            if(s.Kind==DefenderSkillKind.LapplandHavoc)
            {
                defenderDrones.RemoveAll(v=>v.SourceId==d.EntityId);
                for(int i=0;i<s.Targets;i++)defenderDrones.Add(new DefenderDroneState{SourceId=d.EntityId,Index=i,Position=d.Position});
            }
        }
        void StepAdvancedDefenders(float step)
        {
            bool globalRecovery=false;int alive=0;
            foreach(var d in defenders)if(d.Alive)alive++;
            foreach(var d in defenders)
            {
                if(!IsAdvancedDefender(d))continue;
                if(d.AdvancedSkillWasActive&&!DefenderSkillActive(d))
                {
                    if(d.Settings.Skill.Kind==DefenderSkillKind.YuKitchen)
                    {d.Health=Math.Min(d.Settings.Health,d.Health/d.Settings.Skill.HealthMultiplier);d.MaxHealth=d.Settings.Health;}
                    d.AdvancedSkillWasActive=false;d.SkillEndedAt=d.SkillActiveUntil;d.ChargeStartedAt=d.SkillActiveUntil;d.LiberatorBonus=0;
                }
                if(d.Alive&&d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory&&!DefenderSkillActive(d))d.LiberatorBonus=2*DefenderCharge(d);
                globalRecovery|=d.Alive&&d.Settings.Skill.Kind==DefenderSkillKind.YuKitchen&&DefenderSkillActive(d);
            }
            if(alive>=4)foreach(var ally in defenders)
            {
                if(!ally.Alive||!globalRecovery&&ally.Settings.Skill.Kind!=DefenderSkillKind.YuKitchen)continue;
                float amount=ally.MaxHealth*.015f*step;
                float gain=Math.Min(amount,ally.MaxHealth-ally.Health);ally.Health+=gain;
                ally.BurnAccumulated=Math.Max(0,ally.BurnAccumulated-amount);ally.NeuralAccumulated=Math.Max(0,ally.NeuralAccumulated-amount);
                if(gain>0)foreach(var source in defenders)if(source.Alive&&source.Settings.Skill.Kind==DefenderSkillKind.YuKitchen&&
                    (source==ally||DefenderSkillActive(source))){source.SkillHealthRestored+=gain;break;}
            }
            StepDefenderDrones(step);
        }
        bool StepAdvancedDefenderAction(DefenderState d)
        {
            if(d.Settings.Skill.Kind!=DefenderSkillKind.SariaCalcification||!DefenderSkillActive(d))return false;
            if(Clock+1e-6<d.NextAttackTime)return true;
            d.NextAttackTime=Clock+1;d.LastActionTime=Clock;d.LastActionHealing=true;d.LastActionRate=1;
            foreach(var ally in defenders)
            {
                if(!ally.Alive||ally.Health>=ally.MaxHealth-.001f||GroundDistance(d.Position,ally.Position)>d.Settings.Skill.EffectRadius)continue;
                d.TargetId=ally.EntityId;
                combatShots.Add(new CombatShot(d.EntityId,ally.EntityId,true,true,d.Position,ally.Position,Clock,.05f,DefenderSkillAttack(d)*d.Settings.Skill.HealFraction,healing:true));
            }
            return true;
        }
        void HealDefender(DefenderState source,DefenderState target,float amount)
        {
            float gain=Math.Min(amount,target.MaxHealth-target.Health);target.Health+=gain;
            if(source.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification)
            {
                source.SkillHealthRestored+=gain;if(DefenderSkillActive(source))source.AuraHeals++;
                if(target.Settings.Skill.Enabled&&!DefenderSkillActive(target))target.SkillSP=Math.Min(target.Settings.Skill.RequiredSP,target.SkillSP+1);
            }
        }
        public float UnitArtsVulnerability(DeployedUnitState u)
        {
            float result=1;
            foreach(var d in defenders)if(d.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification&&DefenderSkillActive(d)&&GroundDistance(d.Position,u.Motion.Position)<=d.Settings.Skill.EffectRadius)
                result=Math.Max(result,d.Settings.Skill.DamageAmplification);
            return result;
        }
        public float DefenderAuraMoveMultiplier(DeployedUnitState u)
        {
            if(u==null)return 1;float result=1;
            foreach(var d in defenders)if(d.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification&&DefenderSkillActive(d)&&GroundDistance(d.Position,u.Motion.Position)<=d.Settings.Skill.EffectRadius)
                result=Math.Min(result,d.Settings.Skill.MoveMultiplier);
            foreach(var drone in defenderDrones)
            {var d=FindDefender(drone.SourceId);if(drone.Locked&&DefenderSkillActive(d)&&GroundDistance(drone.Position,u.Motion.Position)<=.9f)result=Math.Min(result,d.Settings.Skill.MoveMultiplier);}
            return result;
        }
        bool StepUnitFear(DeployedUnitState u,float step)
        {
            if(Clock>=u.FearUntil||PatriotControlImmune(u))return false;
            u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;
            u.IsMoving=!IsSkillActionPlaying(u)&&Clock>=u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate;
            if(u.IsMoving)u.Motion.Retreat(step*UnitMoveMultiplier(u));
            return true;
        }
        void ScheduleOrdinaryDrone(DefenderState d,DeployedUnitState target,float travel)
        {
            if(d.DroneTargetId!=target.EntityId){d.DroneTargetId=target.EntityId;d.DroneRamp=.2f;}
            var shot=new CombatShot(d.EntityId,target.EntityId,true,false,d.Position+Vector3.up*.4f,target.Motion.Position,
                Clock+d.Settings.Windup,travel,DefenderSkillAttack(d)*d.DroneRamp,damageType:CombatDamageType.Arts){Drone=true};
            combatShots.Add(shot);d.DroneRamp=Math.Min(1.1f,d.DroneRamp+.15f);d.DroneHits++;
        }
        void StepDefenderDrones(float step)
        {
            foreach(var v in defenderDrones)
            {
                var d=FindDefender(v.SourceId);if(!DefenderSkillActive(d))continue;
                double age=Clock-d.SkillStartedAt;
                if(age<1.3){float a=v.Index*Mathf.PI*2/d.Settings.Skill.Targets;v.Position=d.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(float)age;continue;}
                var target=FindUnit(v.TargetId);
                if(!CanDefenderTarget(target))
                {
                    target=null;foreach(var u in units)if(CanDefenderTarget(u)&&(target==null||GroundDistance(v.Position,u.Motion.Position)<GroundDistance(v.Position,target.Motion.Position)))target=u;
                    v.TargetId=target?.EntityId;v.Locked=false;v.Ramp=.2f;
                }
                if(target==null)
                {float a=(float)Clock+v.Index*2;v.Position=d.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));continue;}
                v.Position=Vector3.MoveTowards(v.Position,target.Motion.Position,(float)Math.Min(4,2+age-1.3)*step);
                if(GroundDistance(v.Position,target.Motion.Position)>.15f)continue;
                if(!v.Locked)
                {
                    v.Locked=true;v.NextAttackAt=Clock;
                    if(!PatriotControlImmune(target)){target.FearUntil=Math.Max(target.FearUntil,Clock+2);d.FearApplications++;}
                }
                if(Clock+1e-6<v.NextAttackAt)continue;
                v.NextAttackAt=Clock+d.Settings.Interval;d.DroneHits++;
                combatShots.Add(new CombatShot(d.EntityId,target.EntityId,true,true,v.Position,target.Motion.Position,Clock,.05f,
                    DefenderSkillAttack(d)*v.Ramp,damageType:CombatDamageType.Arts){Drone=true});v.Ramp=Math.Min(1.1f,v.Ramp+.15f);
            }
            foreach(var u in units)
            {
                if(!CanDefenderTarget(u))continue;DefenderState strongest=null;Vector3 origin=Vector3.zero;
                foreach(var v in defenderDrones)
                {
                    var d=FindDefender(v.SourceId);if(!v.Locked||!DefenderSkillActive(d)||GroundDistance(v.Position,u.Motion.Position)>.9f)continue;
                    if(strongest==null||DefenderSkillAttack(d)>DefenderSkillAttack(strongest)){strongest=d;origin=v.Position;}
                }
                if(strongest==null){u.NextDroneAreaAt=Clock+1;continue;}
                if(Clock+1e-6<u.NextDroneAreaAt)continue;u.NextDroneAreaAt=Clock+1;
                if(TryDamageUnit(u.EntityId,DefenderSkillAttack(strongest),CombatDamageType.Arts))OnAdvancedDefenderHit(strongest,u,CombatDamageType.Arts,origin,true);
            }
            defenderDrones.RemoveAll(v=>!DefenderSkillActive(FindDefender(v.SourceId)));
        }
        public Vector3 DefenderWallPoint(DefenderState d)=>d.Position+d.WallNormal*.5f;
        public bool DefenderWallActive(DefenderState d)=>d!=null&&d.Settings.Skill.Kind==DefenderSkillKind.YuKitchen&&DefenderSkillActive(d)&&Clock>=d.SkillActionUntil;
        public static bool CrossesDefenderWall(Vector3 origin,Vector3 target,Vector3 point,Vector3 normal)
        {origin.y=target.y=point.y=normal.y=0;return Vector3.Dot(origin-point,normal)*Vector3.Dot(target-point,normal)<0;}
        void StepDefenderWalls()
        {
            foreach(var shot in combatShots)
            {
                if(shot.Resolved||shot.FromDefender||shot.FireRain||UnitCombat(FindUnit(shot.SourceId)).Melee||shot.WallChecked||shot.ImpactAt-shot.StartedAt<=.001||shot.ImpactAt<Clock)continue;
                foreach(var wall in defenders)
                {
                    if(!DefenderWallActive(wall)||!CrossesDefenderWall(shot.Origin,shot.Destination,DefenderWallPoint(wall),wall.WallNormal))continue;
                    // Check exactly when the travelling projectile crosses the wall, not at launch.
                    float a=Vector3.Dot(shot.Origin-DefenderWallPoint(wall),wall.WallNormal),b=Vector3.Dot(shot.Destination-DefenderWallPoint(wall),wall.WallNormal);
                    double crossing=shot.StartedAt+(shot.ImpactAt-shot.StartedAt)*a/(a-b);
                    if(Clock+1e-6<crossing)continue;shot.WallChecked=true;
                    if(wall.Roll()<wall.Settings.Skill.EraseChance){shot.Resolved=shot.Erased=true;wall.ErasedProjectiles++;}
                    break;
                }
            }
        }
        void OnAdvancedDefenderHit(DefenderState source,DeployedUnitState target,CombatDamageType type,Vector3 origin,bool drone)
        {
            if(!target.Active||target.IsReviving)return;
            foreach(var wall in defenders)
                if(source!=wall&&type==CombatDamageType.Arts&&DefenderWallActive(wall)&&CrossesDefenderWall(origin,target.Motion.Position,DefenderWallPoint(wall),wall.WallNormal))
                {AddScorch(target,DefenderSkillAttack(wall)*wall.Settings.Skill.ElementFraction);wall.BurnApplications++;}
            if(!source.Settings.Kasimierz)return;
            foreach(var d in defenders)if(d.Settings.Skill.Kind==DefenderSkillKind.MlynarGlory&&DefenderSkillActive(d)&&GroundDistance(d.Position,target.Motion.Position)<=d.Settings.Skill.EffectRadius)
                ApplyPureUnitDamage(target,d.Settings.Attack*(1+d.LiberatorBonus)*.11f);
        }
        void OnLiberatorKill(DefenderState d)
        {
            if(d.Settings.Skill.Kind!=DefenderSkillKind.MlynarGlory||!DefenderSkillActive(d))return;
            d.LiberatorBonus=Math.Max(0,d.LiberatorBonus-.1f);d.LiberatorKills++;
        }
        void ApplyPureUnitDamage(DeployedUnitState u,float amount)
        {
            if(!u.Active||u.IsReviving||UnitInvulnerable(u))return;
            ApplyUnitHealthDamage(u,u.FlamePhase==FlamechaserPhase.Ember?1:amount);
        }
        void AddScorch(DeployedUnitState u,float amount)
        {
            if(!u.Active||u.IsReviving||u.ScorchUntil>Clock)return;
            var e=UnitCombat(u).Effects;
            bool boss=e.Patriot||e.FrostNova||e.Mandragora||e.Mudrock||UnitCombat(u).FireRain;
            u.ScorchAccumulated+=amount;
            if(u.ScorchAccumulated<(boss?200:100))return;
            u.ScorchAccumulated=0;u.ScorchUntil=Clock+10;u.ScorchBursts++;ApplyPureUnitDamage(u,700);
        }
    }
}
