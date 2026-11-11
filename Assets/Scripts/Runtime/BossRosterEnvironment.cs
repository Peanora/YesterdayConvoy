using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        void BossSlowDance(DeployedUnitState u)
        {
            var s=BossRoster(u);s.Generation++;s.Phase=2;s.Transitioning=true;s.Timers["slowDance"]=Clock+4;u.Health=1;u.IsMoving=false;BossSetAnimation(u,"A_Die_Start",false);
            if(!bossRosterObjects.Any(o=>o.OwnerId==u.EntityId&&o.Kind=="oldfire"&&o.Alive))for(int i=-1;i<=1;i++)BossObject(u,"oldfire","旧日之火",u.Motion.Position+Vector3.forward*i*1.1f,u.MaxHealth*.12f,.65f,20);
            BossEmit(u,"PhaseStart","slowDance",u.Motion.Position,animation:"A_Die_Start",end:Clock+4);
        }
        void BossOtherPassive(DeployedUnitState u,BossRosterState s,float step)
        {
            if(u.CardId=="CARD_B22"&&s.Transitioning&&Clock<Timer(s,"retreatUntil")){u.Motion.ApproachDistance(Math.Max(0,Timer(s,"retreatTo")),step*1.5f);u.IsMoving=true;}
            if(u.CardId=="CARD_B08"&&s.Phase==2&&s.Transitioning)
            {u.Health=Math.Min(u.MaxHealth,u.Health+u.MaxHealth*.25f*step);if(u.Health>=u.MaxHealth){s.Phase=bossRosterObjects.Any(o=>o.OwnerId==u.EntityId&&o.Kind=="oldfire"&&o.Alive)?1:3;s.Transitioning=false;foreach(var o in bossRosterObjects.Where(o=>o.OwnerId==u.EntityId&&o.Kind=="oldfire"))o.Removed=true;BossSetAnimation(u,BossIdle(u,s),true);BossEmit(u,"PhaseStart","danceChoice",u.Motion.Position,animation:s.Animation);}return;}
            if(s.Transitioning)return;
            if(u.CardId=="CARD_B11"&&Clock<Timer(s,"edictUntil"))BossRememberEdictStuns(u,s,Count(s,"edictActivation"));
            if(u.CardId=="CARD_B22"&&s.Phase==1&&Clock>=Timer(s,"rage")&&!s.Fled)BossFlee(u);
            if(u.CardId=="CARD_B21"&&Clock>=s.NextPassiveAt&&!s.Consumed.Contains("firstAppear")){s.Consumed.Add("firstAppear");BossOtherSkill(u,"insomnia",++bossActivationSequence);}
            if(u.CardId=="CARD_B21"&&Timer(s,"depart")<=Clock&&s.Consumed.Contains("firstAppear")&&BossLiving().Any()){s.Present=false;BossSetAnimation(u,"Skill_End_B",false);s.Timers["depart"]=double.PositiveInfinity;}
            if(u.CardId=="CARD_B21"&&!s.Present&&s.Consumed.Contains("firstAppear")&&!BossLiving().Any()){s.Present=true;BossSetAnimation(u,BossIdle(u,s),true);BossEmit(u,"StateEnd","arenaWait",u.Motion.Position,animation:s.Animation);}
            if((u.CardId=="CARD_B15"||u.CardId=="CARD_B24")&&s.Phase>=2&&!s.PartnerDown)
            {string partner=u.CardId=="CARD_B15"?"CARD_B24":"CARD_B15";if(units.Any(x=>x.CardId==partner&&x.Resolution==UnitResolution.Dead)){if(u.CardId=="CARD_B15")BossOtherSkill(u,"domainBurst",++bossActivationSequence);else{s.PartnerDown=true;s.AttackMultiplier*=2;}}}
            if(u.CardId=="CARD_B24")
            {if(s.PartnerDown)foreach(var d in defenders.Where(d=>d.Reserve))s.Timers["bench/"+d.EntityId]=double.PositiveInfinity;foreach(var d in BossLiving().Where(d=>Timer(s,"bench/"+d.EntityId)>Clock).ToArray())BossMarkedDeployment(u,d);}
            if(Clock+1e-6<s.NextAuraAt)return;s.NextAuraAt=Clock+1;
            if(u.CardId=="CARD_B25"&&s.Phase>=2)foreach(var d in BossLiving().Where(d=>GroundDistance(u.Motion.Position,d.Position)<2.5f).ToArray()){BossHit(u,d,UnitCombat(u).UnitAttack*.2f,CombatDamageType.Arts,"aura");ApplyBurn(d,15,u);}
            if(u.CardId=="CARD_B10"&&Clock>=s.NextPassiveAt){s.NextPassiveAt=Clock+10;var skill=BossRosterRules.Skills(u.CardId).First();if(Clock>=s.LockedUntil)BossCast(u,skill,true);}
            if(u.CardId=="CARD_B07")foreach(var friend in BossFriends(u).Where(x=>x.FlamePhase==FlamechaserPhase.Ember))HealUnit(friend,friend.MaxHealth*.02f);
            if(u.CardId=="CARD_B25")foreach(var d in defenders.Where(d=>!d.Alive&&GroundDistance(d.Position,u.Motion.Position)<(s.Phase>=2?3.5f:3)&&s.Consumed.Add("penalty/"+d.EntityId)).ToArray())BossPenalty(u,d.Position,++bossActivationSequence);
        }
        void BossOtherThresholds(DeployedUnitState u,BossRosterState s)
        {
            if(u.Health<=0||s.Transitioning||s.Final)return;float ratio=u.Health/u.MaxHealth;
            if(u.CardId=="CARD_B04"&&s.Phase>=2)
            {foreach(float t in new[]{.75f,.5f,.25f})if(ratio<t&&s.Consumed.Add("quarter/"+t))BossOtherSkill(u,"killWindow",++bossActivationSequence);}
            if(u.CardId=="CARD_B11"&&ratio<.5f&&s.Consumed.Add("folly/"+s.Phase))BossAutoSkill(u,"folly");
            if(u.CardId=="CARD_B21"&&ratio<.5f&&s.Consumed.Add("awaken"))BossAutoSkill(u,"awaken");
            if(u.CardId=="CARD_B23"&&s.Phase>=2)foreach(float t in new[]{.75f,.5f,.25f})if(ratio<t&&s.Consumed.Add("pierce/"+t))BossAutoSkill(u,"pierce");
            if((u.CardId=="CARD_B15"||u.CardId=="CARD_B24")&&s.Phase>=2)
            {foreach(float t in s.SourceVariant=="TL-S-4"?new[]{.75f,.5f,.25f}:new[]{.5f})if(ratio<t&&s.Consumed.Add("fatal/"+t)){string id=u.CardId=="CARD_B15"?"obituary":"deathKnock";BossAutoSkill(u,id,s.SourceVariant=="TL-S-4"?"Skill2Hp"+(int)Math.Round(t*100):"Skill2OnlyHp50");}}
        }
        void BossAutoSkill(DeployedUnitState u,string id,string native=null)
        {
            var s=BossRoster(u);var spec=BossRosterRules.Skills(u.CardId).First(x=>x.Id==id);double due=Math.Max(Clock,Math.Max(s.LockedUntil,Timer(s,"autoUntil")));
            // Reserve the body window now: one large hit crossing several thresholds queues
            // in stable descending order; original lock-window competition is an explicit adaptation.
            string raw=BossRosterRules.SkillAnimation(u.CardId,spec,s.Phase);double duration=BossRosterRules.Action(u.CardId,raw)?.Duration??Math.Max(.4f,spec.Duration);s.Timers["autoUntil"]=due+duration+.05;s.LockedUntil=due+duration;
            BossAfter(u,due-Clock,()=>BossCast(u,spec,true,native));
        }
        void BossSpecialOrdinaryHit(DeployedUnitState u,DefenderState target,float attack,long activation,int hit)
        {
            var s=BossRoster(u);var type=UnitCombat(u).Effects.DamageType;
            if(u.CardId=="CARD_B22"&&s.Phase>=2){BossArea(u,target.Position,1.5f,attack,CombatDamageType.Physical,"attack",activation);return;}
            if(u.CardId=="CARD_B23"&&!u.IsBlocked){BossArea(u,target.Position,1.1f,attack,CombatDamageType.Physical,"attack",activation);return;}
            if(u.CardId=="CARD_B13"&&s.Phase>=2){foreach(var d in BossLiving().OrderBy(x=>GroundDistance(x.Position,u.Motion.Position)).Take(3).ToArray())BossHit(u,d,attack,CombatDamageType.Arts,"attack",activation,hit);return;}
            if(u.CardId=="CARD_B26")
            {foreach(var d in BossLiving().OrderByDescending(x=>BossDefenderPolarity(x)!=s.Polarity).ThenBy(x=>GroundDistance(x.Position,u.Motion.Position)).ThenBy(x=>x.EntityId,StringComparer.Ordinal).Take(2).ToArray())BossHit(u,d,attack,CombatDamageType.Arts,"attack",activation,hit);return;}
            if(u.CardId=="CARD_B25"){attack*=1+s.Matrix*.12f;type=s.Phase>=2?CombatDamageType.Arts:CombatDamageType.Physical;}
            if(u.CardId=="CARD_B09")attack*=1+(u.OrdinaryAttacksFired%4==0?.5f:0);
            BossHit(u,target,attack,type,"attack",activation,hit,u.CardId=="CARD_B12");
            if(u.CardId=="CARD_B09"&&target.Alive)ApplyNeuralDamage(target,15,u);
        }
        void BossOtherObjects(float step)
        {
            foreach(var pair in bossAllyBuffs){var u=FindUnit(pair.Key);if(u!=null&&u.Active&&pair.Value.Until>Clock&&pair.Value.HealFraction>0)HealUnit(u,u.MaxHealth*pair.Value.HealFraction*step);}
            foreach(var o in bossRosterObjects.ToArray())
            {
                if(!o.Alive)continue;var owner=FindUnit(o.OwnerId);var state=BossRoster(owner);if(owner==null||state==null)continue;
                if(Clock>=o.Until){o.Removed=true;o.DestroyedAt=Clock;BossEmit(owner,"Despawn",o.Kind,o.Position,activation:o.ActivationId,targetId:o.Id);continue;}
                if(o.Kind=="fireball"&&Clock<o.NextAt)continue;
                if(o.Kind=="matrix"&&Clock<o.NextAt)continue;
                if(o.Kind=="matrix")o.Velocity=(owner.Motion.Position-o.Position).normalized*.7f;
                if(o.Kind=="hound")StepBossHound(owner,o,step);
                if(o.Kind=="pain")
                {
                    if(o.Animation=="Skill_2_Begin"&&Clock<o.AnimationUntil){o.Health=Math.Min(o.MaxHealth,o.Health+o.MaxHealth*.05f*step);continue;}
                    if(Clock>=o.AnimationUntil&&Clock>=Timer(state,"painRoar/"+o.Id))
                    {state.Timers["painRoar/"+o.Id]=Clock+25;o.Animation="Skill_1";o.AnimationAt=Clock;o.AnimationUntil=Clock+1.9333333;o.AnimationLoop=false;o.NextAt=o.AnimationUntil;long activation=++bossActivationSequence;o.ActivationId=activation;BossEmit(owner,"SkillStart","painRoar",o.Position,native:"Multi",animation:o.Animation,targetId:o.Id,activation:activation,end:o.AnimationUntil,origin:o.Position);BossAfter(owner,.8,()=>{if(!o.Alive)return;var targets=BossLiving().Where(d=>GroundDistance(d.Position,o.Position)<=o.Radius).ToArray();if(targets.Length>0)foreach(var d in targets)BossHit(owner,d,UnitCombat(owner).UnitAttack*3.5f*o.PowerMultiplier/targets.Length,CombatDamageType.Physical,"painRoar",activation);else foreach(var d in BossLiving().ToArray()){BossHit(owner,d,UnitCombat(owner).UnitAttack*.8f*o.PowerMultiplier,CombatDamageType.Physical,"painRoar",activation);BossLock(d,5);}});BossAfter(owner,1.9333333,()=>BossEmit(owner,"SkillEnd","painRoar",o.Position,native:"Multi",animation:"Skill_1",targetId:o.Id,activation:activation,origin:o.Position));continue;}
                    if(Clock<o.AnimationUntil)continue;
                }
                if(o.Velocity.sqrMagnitude>0)o.Position+=o.Velocity*step;
                if(o.Kind=="idea")
                {var blocker=BossLiving().FirstOrDefault(d=>GroundDistance(d.Position,o.Position)<.4f&&o.HitIds.Add(d.EntityId));if(blocker!=null)o.Velocity=-o.Velocity;if(GroundDistance(o.Position,owner.Motion.Position)<.5f){state.BlackCrownCharges++;o.Removed=true;BossEmit(owner,"Impact","idea",o.Position,targetId:owner.EntityId,applied:true);}}
                if(o.Kind=="beam")
                {var opposing=bossRosterObjects.FirstOrDefault(x=>x!=o&&x.Alive&&x.Kind=="beam"&&x.Polarity!=o.Polarity&&GroundDistance(x.Position,o.Position)<.3f);if(opposing!=null){BossBeamBurst(owner,o,true);BossBeamBurst(FindUnit(opposing.OwnerId),opposing,true);continue;}if(BossLiving().Any(d=>GroundDistance(d.Position,o.Position)<.3f)){BossBeamBurst(owner,o,false);continue;}}
                if(o.Kind=="fireball")foreach(var d in BossLiving().Where(d=>d.EntityId!=o.TargetId&&GroundDistance(d.Position,o.Position)<.5f&&!o.HitIds.Contains(d.EntityId)).ToArray())
                {o.HitIds.Add(d.EntityId);BossHit(owner,d,UnitCombat(owner).UnitAttack*1.2f,CombatDamageType.Arts,o.Kind,o.ActivationId);o.Removed=true;o.DestroyedAt=Clock;break;}
                if(o.Kind=="domain")foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,o.Position)<o.Radius))BossSlow(d,.2f,.3f);
                if(o.Kind=="matrix")
                {
                    if(o.Velocity.sqrMagnitude>0&&BossInObject("matrixField",o.Position)){o.Removed=true;o.DestroyedAt=Clock;foreach(var d in BossLiving().Where(d=>BossInObject("matrixField",d.Position)))d.SkillSP=Math.Min(d.Settings.Skill.RequiredSP,d.SkillSP+2);continue;}
                    var absorber=units.Where(x=>x.Active&&x.CardId=="CARD_B25"&&GroundDistance(x.Motion.Position,o.Position)<.8f).OrderBy(x=>x.EntityId,StringComparer.Ordinal).FirstOrDefault();if(absorber!=null){var absorberState=BossRoster(absorber);if(Clock>=Timer(absorberState,"palsy")&&Clock>=Timer(absorberState,"absorbNext")){absorberState.Timers["absorbNext"]=Clock+.1;BossMatrix(absorber,1);o.Removed=true;o.DestroyedAt=Clock;BossEmit(absorber,"Impact","absorb",o.Position,applied:true,targetId:o.Id);}}if(!o.Alive)continue;
                }
                if(o.Kind=="matrixField"&&GroundDistance(o.Position,owner.Motion.Position)<=o.Radius&&Clock>=o.NextAt){o.NextAt=Clock+3;state.Matrix=Math.Max(0,state.Matrix-1);}
                if(o.Kind!="matrix"&&Clock>=o.NextAt)
                {
                    o.NextAt=Clock+1;
                    if(o.Kind=="fire")BossArea(owner,o.Position,o.Radius,UnitCombat(owner).UnitAttack*.18f,CombatDamageType.Arts,"fire",o.ActivationId);
                    if(o.Kind=="cannon"||o.Kind=="pain")
                    {
                        var target=BossLiving().Where(d=>GroundDistance(d.Position,o.Position)<=o.Radius).OrderBy(d=>GroundDistance(d.Position,o.Position)).FirstOrDefault();if(target!=null)
                        {o.ActivationId=++bossActivationSequence;o.AttackStartedAt=Clock;if(o.Kind=="pain"){o.Animation="Attack";o.AnimationAt=Clock;o.AnimationUntil=Clock+1.6;o.AnimationLoop=false;}float windup=o.Kind=="pain"?.8f:0;o.ImpactAt=Clock+windup+GroundDistance(o.Position,target.Position)/5;o.TargetPosition=target.Position;o.TargetId=target.EntityId;long activation=o.ActivationId;Vector3 center=target.Position,origin=o.Position;
                         BossEmit(owner,"AttackStart",o.Kind,center,native:"Attack",animation:o.Kind=="pain"?"Attack":"",activation:activation,targetId:target.EntityId,end:Clock+(o.Kind=="pain"?1.6:windup),origin:origin);
                         BossAfter(owner,windup,()=>{if(!o.Alive)return;o.ShotAt=Clock;BossEmit(owner,"ProjectileSpawn",o.Kind,center,native:"Attack",activation:activation,targetId:target.EntityId,end:o.ImpactAt,origin:origin);},true);
                         BossAfter(owner,o.ImpactAt-Clock,()=>{if(!o.Alive&&o.ShotAt<o.AttackStartedAt)return;o.ImpactAt=Clock;int before=bossRosterEvents.Count;BossArea(owner,center,o.Kind=="cannon"?1.2f:.8f,UnitCombat(owner).UnitAttack*(o.BuffUntil>Clock?o.PowerMultiplier:1),CombatDamageType.Physical,o.Kind,activation);if(bossRosterEvents.Count==before)BossEmit(owner,"Impact",o.Kind,center,native:"Attack",activation:activation,targetId:o.TargetId,applied:false,origin:origin);},true);}
                        o.NextAt=Clock+(o.Kind=="cannon"?4:2.5);
                    }
                }
                // Objects consume a real defender attack, never a free extra shot or a UI-injected HP change.
                if(BossObjectDamageable(o))
                foreach(var d in BossLiving().Where(d=>d.Settings.CanAttack&&DefenderCanAttack(d)&&!BossRosterDefenderActionLocked(d)&&d.NextAttackTime<=Clock&&GroundDistance(d.Position,o.Position)<=DefenderRange(d)).ToArray())
                {d.LastActionTime=Clock;d.LastActionHealing=false;d.NextAttackTime=Clock+DefenderEffectiveInterval(d);if(BossRosterDefenderAttackHits(d,d.Settings.DamageType))BossObjectDamage(o,DefenderSkillAttack(d),d);if(!o.Alive||!BossObjectDamageable(o))break;}
                if(o.Kind=="fireball"&&GroundDistance(o.Position,owner.Motion.Position)>12){foreach(var d in BossLiving().ToArray())BossHit(owner,d,UnitCombat(owner).UnitAttack*1.5f,CombatDamageType.Arts,"fireball",o.ActivationId);o.Removed=true;o.DestroyedAt=Clock;}
            }
        }
        void BossObjectDamage(BossRosterObject o,float damage,DefenderState attacker=null)
        {
            if(!BossObjectDamageable(o)||damage<=0)return;
            if(o.Interaction==BossObjectInteraction.CameraSeal){o.Shield=Math.Max(0,o.Shield-damage);if(o.Shield<=0){o.BuffUntil=Clock;o.Health=o.MaxHealth;var cameraOwner=FindUnit(o.OwnerId);if(cameraOwner!=null)BossEmit(cameraOwner,"StateEnd","cameraSeal",o.Position,targetId:o.Id,applied:true);}return;}
            if(o.Interaction==BossObjectInteraction.HitCount)damage=1;if(o.Shield>0){float absorbed=Math.Min(damage,o.Shield);o.Shield-=absorbed;damage-=absorbed;}
            o.Health=Math.Max(0,o.Health-damage);if(o.Health>0)return;o.DestroyedAt=Clock;var owner=FindUnit(o.OwnerId);var s=BossRoster(owner);if(s==null)return;
            if(o.Kind=="hound")EndBossHound(owner,o,HoundState(o),true);
            BossEmit(owner,"Despawn",o.Kind,o.Position,activation:o.ActivationId,targetId:o.Id,applied:true);
            if(o.Kind=="matrix"&&attacker!=null)attacker.SkillSP=Math.Min(attacker.Settings.Skill.RequiredSP,attacker.SkillSP+2);
            if(o.Kind=="mark"){var d=FindDefender(o.TargetId);if(d!=null){var status=BossDefender(d);status.ActionUntil=status.SkillUntil=Clock;status.CannotBlock=false;}}
            if(o.Kind=="hound"&&!bossRosterObjects.Any(x=>x.OwnerId==o.OwnerId&&x.Kind=="hound"&&x.Alive))BossStartPainWave(owner);
            if(o.Kind=="pain")
            {s.Counts["painDead"]=Count(s,"painDead")+1;foreach(var friend in BossFriends(owner)){var buff=BossBuff(friend);buff.Attack=1+.3f*Math.Min(2,Count(s,"painDead"));buff.Until=double.PositiveInfinity;}var rest=bossRosterObjects.Where(x=>x.OwnerId==o.OwnerId&&x.Kind=="pain"&&x.Alive).ToArray();foreach(var x in rest){x.PowerMultiplier=1+.3f*Math.Min(2,Count(s,"painDead"));x.BuffUntil=double.PositiveInfinity;BossPainBlink(owner,x);}if(Count(s,"painDead")>=3){owner.Health=0;DefeatUnit(owner);}}
        }
        static string BossDefenderPolarity(DefenderState d){uint code=2166136261;foreach(char c in d.EntityId)code=(code^c)*16777619;return(code&1)==0?"yin":"yang";}
        void BossBeamBurst(DeployedUnitState owner,BossRosterObject o,bool strong)
        {if(owner==null||!o.Alive)return;o.Removed=true;o.DestroyedAt=Clock;var position=o.Position;BossEmit(owner,"Despawn","beam",position,targetId:o.Id,activation:o.ActivationId);BossAfter(owner,.5,()=>{foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,position)<=1.25f).ToArray()){bool same=BossDefenderPolarity(d)==o.Polarity;BossHit(owner,d,UnitCombat(owner).UnitAttack*(strong?(same?1.8f:4.2f):(same?.6f:1.4f)),CombatDamageType.Arts,"beam",o.ActivationId);}},true);}
        string BossNativeKey(DeployedUnitState u,string id)
        {
            var s=BossRoster(u);var spec=BossRosterRules.Skills(u.CardId).FirstOrDefault(x=>x.Id==id);string key=spec?.NativeKey??id;if(s==null)return key;if(s.NativeOverrides.TryGetValue(id,out var overridden))return overridden;
            if(id=="attack")return u.CardId=="CARD_B13"&&s.Phase>=2?(s.Phase==3?"m2attack":"m1attack"):"Attack";if(id=="painRoar")return "Multi";if(id=="painBlink")return "Blink";if(id=="pain"||id=="cannon"||id=="hound")return "Attack";
            if(u.CardId=="CARD_B04"&&s.Phase>=2)return id=="dash"?"Blink2":id=="blizzard"?"CircleAttack2":key;
            if(u.CardId=="CARD_B07"&&s.Phase>=2)return id=="dominate"?"Chain_2":id=="sacrifice"?"Sacrifice_2":key;
            if(u.CardId=="CARD_B08"&&s.Phase==3)return id=="grantFire"?"burn_tile_phase_2":id=="blind"?"bone_born_phase_2":key;
            if(u.CardId=="CARD_B09"&&s.Phase>=2&&id=="absurdity")return "control2";
            if(u.CardId=="CARD_B11"&&s.Phase>=2)return id=="sluggish"?"DeathmarkS2":id=="folly"?"RebuildS2":id=="annihilation"?"Suicide":key;
            if(u.CardId=="CARD_B12"&&s.Phase>=2&&id=="survival")return "DeepBreathS4";
            if(u.CardId=="CARD_B13"&&id=="rite")return s.Phase==3?"M2RangeProjectile":"M1RangeProjectile";
            if((u.CardId=="CARD_B15"||u.CardId=="CARD_B24")&&s.Phase>=2)return id=="sow"||id=="incite"?"blkhlySharedSkill":id=="obituary"||id=="deathKnock"?"Skill2OnlyHp50":id=="domainBurst"?"Skill3":key;
            if(u.CardId=="CARD_B23"&&s.Phase>=2&&id=="gospel")return "M1MultiRangedAttack";
            if(u.CardId=="CARD_B25"&&s.Phase>=2)return id=="retribution"?"Shootgun_2":id=="judgement"?"FireSeed_Headshot_2":id=="penalty"?"boom_area_2":key;return key;
        }
    }
}
