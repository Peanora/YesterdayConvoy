using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class BossPending {internal double At;internal long Order;internal string Owner;internal int Generation;internal Action Apply;internal bool SurviveDeath;}
        sealed class BossDefenderStatus {internal double ActionUntil,SkillUntil,HealUntil,SlowUntil;internal float Rate=1;internal bool CannotBlock;}
        readonly Dictionary<string,BossRosterState> bossRosterStates=new Dictionary<string,BossRosterState>();
        readonly Dictionary<string,BossDefenderStatus> bossDefenderStates=new Dictionary<string,BossDefenderStatus>();
        readonly List<BossPending> bossPending=new List<BossPending>();
        readonly List<Action> bossSummonQueue=new List<Action>();
        readonly List<BossRosterObject> bossRosterObjects=new List<BossRosterObject>();
        readonly List<BossRosterVisualEvent> bossRosterEvents=new List<BossRosterVisualEvent>();
        readonly Dictionary<long,string> bossRequests=new Dictionary<long,string>();
        long bossEventSequence,bossActivationSequence,bossPendingSequence;string bossSourceVariant="TL-10";bool bossMatrixFeed=true;
        public IReadOnlyList<BossRosterVisualEvent> BossRosterEvents=>bossRosterEvents.AsReadOnly();
        public IReadOnlyList<BossRosterObject> BossRosterObjects=>bossRosterObjects.AsReadOnly();
        public BossRosterState BossRoster(DeployedUnitState u)
        {
            if(u==null||!BossRosterRules.Supports(u.CardId))return null;
            if(!bossRosterStates.TryGetValue(u.EntityId,out var s)){InitializeBossRosterUnit(u);s=bossRosterStates[u.EntityId];}return s;
        }
        public void ConfigureBossRosterSourceVariant(string variant)
        {if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Select source variant before deployment.");if(variant!="TL-10"&&variant!="TL-S-4")throw new ArgumentException("Unknown original level variant.");bossSourceVariant=variant;}
        public string BossRosterSourceVariant=>bossSourceVariant;
        public void ConfigureBossRosterMatrixFeed(bool enabled){if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Configure source map feed before deployment.");bossMatrixFeed=enabled;}
        void ResetBossRoster(){ResetBossCoreExtensions();ResetBossHounds();bossRosterStates.Clear();bossDefenderStates.Clear();bossAllyBuffs.Clear();bossPending.Clear();bossSummonQueue.Clear();bossRosterObjects.Clear();bossRosterEvents.Clear();bossRequests.Clear();bossEventSequence=bossActivationSequence=bossPendingSequence=0;}
        void InitializeBossRosterUnit(DeployedUnitState u)
        {
            if(u==null||!BossRosterRules.Supports(u.CardId)||bossRosterStates.ContainsKey(u.EntityId))return;
            var s=new BossRosterState{EntityId=u.EntityId,CardId=u.CardId,SourceVariant=bossSourceVariant,NextAuraAt=Clock+1,NextPassiveAt=Clock+5};
            foreach(var skill in BossRosterRules.Skills(u.CardId))s.SP[skill.Id]=Math.Min(skill.RequiredSP,skill.RequiredSP*.5f);
            bossRosterStates.Add(u.EntityId,s);u.SkillAmmo=0;u.SkillSP=BossRosterRules.Skills(u.CardId).Count>0?s.SP[BossRosterRules.Skills(u.CardId)[0].Id]:0;
            if(u.CardId=="CARD_B22"){s.Timers["rage"]=Clock+12;s.DamageMultiplier=.35f;}
            if(u.CardId=="CARD_B25"){s.Matrix=3;if(bossMatrixFeed)for(int i=0;i<4;i++){var matrix=BossObject(u,"matrix","塑能基质",u.Motion.Position+Vector3.right*(2+i*.35f),u.MaxHealth*.08f,.4f,40);matrix.Velocity=Vector3.left*.7f;matrix.NextAt=Clock+i*2;}}
            if(u.CardId=="CARD_B21")s.Present=false;
            if(u.CardId=="CARD_B05")BossObject(u,"smoke","烟雾",u.Motion.Position,0,2.2f);
            if(u.CardId=="CARD_B09"){BossObject(u,"camera","摄影机",u.Motion.Position+Vector3.right*2,UnitCombat(u).UnitAttack*4,2.3f);BossObject(u,"camera","摄影机",u.Motion.Position+Vector3.right*4,UnitCombat(u).UnitAttack*4,2.3f);}
            if(u.CardId=="CARD_B11")BossObject(u,"node","调谐节点",u.Motion.Position+Vector3.right*2,UnitCombat(u).UnitAttack*4,1);
            if(u.CardId=="CARD_B12")BossObject(u,"waymark","昔日道标",u.Motion.Position+Vector3.right*2,UnitCombat(u).UnitAttack*5,1);
            if(u.CardId=="CARD_B23"){BossObject(u,"cannon","盐坨子炮",u.Motion.Position+Vector3.forward,UnitCombat(u).UnitAttack*5,3);BossObject(u,"cannon","盐坨子炮",u.Motion.Position-Vector3.forward,UnitCombat(u).UnitAttack*5,3);}
            BossSetAnimation(u,BossIdle(u,s),true);BossEmit(u,"Summon","deploy",u.Motion.Position,animation:s.Animation);
        }
        public IReadOnlyList<BossRosterSkillInfo> BossRosterSkills(DeployedUnitState u)
        {
            var s=BossRoster(u);if(s==null)return Array.Empty<BossRosterSkillInfo>();var result=new List<BossRosterSkillInfo>();
            foreach(var spec in BossRosterRules.Skills(u.CardId).Where(x=>BossManualSkill(u.CardId,x.Id))){var failure=CheckBossRosterSkillCore(u,s,spec,false);result.Add(new BossRosterSkillInfo{SkillId=spec.Id,Name=spec.Name,Description=spec.Description,SP=s.SP[spec.Id],MaxSP=spec.RequiredSP,Ready=failure==SkillFailure.None,Failure=IsPaused?SkillFailure.Paused:failure});}return result.AsReadOnly();
        }
        static bool BossManualSkill(string card,string skill)
        {
            switch(card){case "CARD_B04":return skill!="killWindow";case "CARD_B11":return skill!="folly"&&skill!="annihilation";case "CARD_B14":return skill!="echoes";case "CARD_B15":return skill!="domainBurst"&&skill!="obituary";case "CARD_B21":return skill!="awaken"&&skill!="mountains";case "CARD_B22":return skill!="rage"&&skill!="flee";case "CARD_B23":return skill!="pierce";case "CARD_B24":return skill!="deployMarked"&&skill!="deathKnock";case "CARD_B25":return skill!="penalty"&&skill!="absorb";case "CARD_B12":return skill!="eternal";default:return true;}
        }
        bool BossSkillHasTarget(DeployedUnitState u,BossRosterState s,string skill)
        {
            if(u.CardId=="CARD_B01"&&skill=="c4")return ChooseDefender(u,2.5f)!=null;
            if(u.CardId=="CARD_B03"&&skill=="blink")return u.IsBlocked&&FindDefender(u.BlockerId)?.Alive==true;
            if(u.CardId=="CARD_B09"&&skill=="absurdity")return bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&o.Kind=="camera");
            if(u.CardId=="CARD_B09"&&skill=="intermission")return BossLiving().Any(d=>GroundDistance(d.Position,u.Motion.Position)<=UnitCombat(u).UnitRange&&bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Shield<=0&&GroundDistance(d.Position,o.Position)<=o.Radius));
            if(u.CardId=="CARD_B25"&&(skill=="judgement"||skill=="retribution"))return ChooseDefender(u,skill=="judgement"?(s.Phase>=2?2.5f:1.1f):UnitCombat(u).UnitRange)!=null;
            switch(u.CardId)
            {
                case "CARD_B01":case "CARD_B04":case "CARD_B05":case "CARD_B14":case "CARD_B21":return ChooseDefender(u,100)!=null;
                case "CARD_B07":return skill!="dominate"||ChooseDefender(u,100)!=null;
                case "CARD_B08":return skill=="flameDance"||ChooseDefender(u,100)!=null;
                case "CARD_B11":return skill=="edict"||ChooseDefender(u,s.Phase>=2?100:2.2f)!=null;
                case "CARD_B12":return skill!="blackCrown"||ChooseDefender(u,100)!=null;
                case "CARD_B13":return skill!="rite"||ChooseDefender(u,100)!=null;
                case "CARD_B22":return skill!="rock"||ChooseDefender(u,100)!=null;
                case "CARD_B23":return skill!="gospel"||ChooseDefender(u,UnitCombat(u).UnitRange)!=null;
                default:return true;
            }
        }
        public bool BossRosterReady(DeployedUnitState u)=>BossRosterSkills(u).Any(s=>s.Ready);
        public SkillFailure CheckBossRosterSkill(DeployedUnitState u,string skillId=null)
        {
            var s=BossRoster(u);if(s==null)return SkillFailure.NoSkill;var spec=BossRosterRules.Skills(u.CardId).FirstOrDefault(x=>BossManualSkill(u.CardId,x.Id)&&(skillId==null||x.Id==skillId));return spec==null?SkillFailure.NoSkill:CheckBossRosterSkillCore(u,s,spec,true);
        }
        SkillFailure CheckBossRosterSkillCore(DeployedUnitState u,BossRosterState s,BossRosterRules.Skill spec,bool checkPause)
        {
            if(!EngagementEnabled||Phase!=BattlePhase.Running)return SkillFailure.NotRunning;if(checkPause&&IsPaused)return SkillFailure.Paused;
            if(!u.Active||s.Final)return SkillFailure.MissingEntity;if(s.Transitioning||Clock<s.LockedUntil||Clock<Timer(s,"palsy"))return SkillFailure.AlreadyActive;
            if(s.Phase<spec.MinimumPhase)return SkillFailure.NotReady;if(s.SP[spec.Id]+.00001f<spec.RequiredSP)return SkillFailure.NotReady;
            if(u.CardId=="CARD_B14"&&s.Phase>=2)return SkillFailure.NoSkill;
            if(!BossSkillHasTarget(u,s,spec.Id))return SkillFailure.NoTarget;
            if(spec.Id=="rock"&&s.Phase<2||spec.Id=="flee"&&(s.Fled||s.Phase!=1))return SkillFailure.NoTarget;
            if(spec.Id=="blackCrown"&&s.BlackCrownCharges<=0)return SkillFailure.NoTarget;
            if(spec.Id=="flameDance"&&!defenders.Any(d=>d.Alive&&BossInObject("fire",d.Position)))return SkillFailure.NoTarget;
            if(spec.Id=="deployMarked"&&!defenders.Any(d=>d.Reserve&&Timer(s,"bench/"+d.EntityId)>Clock))return SkillFailure.NoTarget;
            return SkillFailure.None;
        }
        public SkillFailure TryActivateBossRosterSkill(int runId,long requestId,string entityId,string skillId)
        {
            if(runId!=RunId)return SkillFailure.StaleRun;var u=FindUnit(entityId);var s=BossRoster(u);if(s==null)return SkillFailure.NoSkill;
            var spec=BossRosterRules.Skills(u.CardId).FirstOrDefault(x=>BossManualSkill(u.CardId,x.Id)&&(skillId==null||x.Id==skillId));if(spec==null)return SkillFailure.NoSkill;string key=entityId+"/"+spec.Id;
            if(bossRequests.TryGetValue(requestId,out var previous))return previous==key?SkillFailure.None:SkillFailure.StaleRun;
            var failure=CheckBossRosterSkill(u,spec.Id);if(failure!=SkillFailure.None)return failure;s.SP[spec.Id]=0;u.SkillsUsed++;SkillCasts++;bossRequests.Add(requestId,key);BossCast(u,spec);return SkillFailure.None;
        }
        bool TryActivateBossRoster(DeployedUnitState u){var info=BossRosterSkills(u).FirstOrDefault(x=>x.Ready);return info!=null&&TryActivateBossRosterSkill(RunId,long.MinValue+ ++bossPendingSequence,u.EntityId,info.SkillId)==SkillFailure.None;}
        void BossCast(DeployedUnitState u,BossRosterRules.Skill skill,bool automatic=false,string nativeOverride=null)
        {
            var s=BossRoster(u);if(s==null||s.Final)return;string raw=BossRosterRules.SkillAnimation(u.CardId,skill,s.Phase);var clip=BossRosterRules.Action(u.CardId,raw);
            float hit=clip!=null&&clip.Hits.Length>0?clip.Hits[0]:skill.Windup,duration=clip?.Duration??Math.Max(.4f,skill.Duration);
            if(nativeOverride==null)s.NativeOverrides.Remove(skill.Id);else s.NativeOverrides[skill.Id]=nativeOverride;
            if(new[]{"blink","dash","traverse","pierce"}.Contains(skill.Id)&&clip!=null)hit=clip.Duration;
            var sequence=BossCastSequence(u,skill.Id);double offset=0;bool found=false;
            if(sequence.Length>1)
            {
                foreach(string part in sequence)
                {
                    var action=BossRosterRules.Action(u.CardId,part);if(action==null)continue;double hold=action.Duration;
                    if(part.Contains("Loop")){if(u.CardId=="CARD_B25")hold=Math.Max(hold,(skill.Id=="retribution"?5:3)-offset);if(u.CardId=="CARD_B23")hold=Math.Max(hold,2.2);}
                    if(!found&&action.Hits.Length>0){hit=(float)offset+action.Hits[0];found=true;}
                    string captured=part;bool loop=part.Contains("Loop");double at=offset,holdCaptured=hold;
                    if(at>0)BossAfter(u,at,()=>BossSetAnimation(u,captured,loop,holdCaptured));offset+=hold;
                }
                duration=(float)offset;if(sequence.Length>0)raw=sequence[0];
            }
            if(u.CardId=="CARD_B25"&&skill.Id=="retribution")hit=0;
            // C4 explicitly does not wait for Spine OnAttack. Native preDelay=.6;
            // its attached projectile lives 3.2s, so the clip's OnAttack@3.8 is the
            // detonation beat, rather than a second installation delay.
            if(u.CardId=="CARD_B01"&&skill.Id=="c4")hit=.6f;
            s.Activation=++bossActivationSequence;s.LockedUntil=Clock+Math.Max(hit,duration);u.IsMoving=false;u.NextAttackTime=Math.Max(u.NextAttackTime,s.LockedUntil);
            if(!string.IsNullOrEmpty(raw))BossSetAnimation(u,raw,false,duration);
            long activation=s.Activation;BossEmit(u,"SkillStart",skill.Id,u.Motion.Position,native:BossNativeKey(u,skill.Id),animation:raw,activation:activation,end:s.LockedUntil);
            BossAfter(u,hit,()=>ExecuteBossRosterSkill(u,skill.Id,activation));BossAfter(u,Math.Max(hit,duration),()=>BossEmit(u,"SkillEnd",skill.Id,u.Motion.Position,native:BossNativeKey(u,skill.Id),activation:activation));
        }
        string[] BossCastSequence(DeployedUnitState u,string skill)
        {
            int phase=BossRoster(u).Phase;string prefix=phase>=2?"B_":"A_";
            if(u.CardId=="CARD_B08"&&skill=="flameDance")return new[]{"B_Skill_3_Start","B_Skill_3_Loop","B_Skill_3_End"};
            if(u.CardId=="CARD_B12"&&skill=="survival")return new[]{prefix+"Skill_Begin",prefix+"Skill_Loop",prefix+"Skill_End"};
            if(u.CardId=="CARD_B13"&&skill=="rite"){string n=phase==3?"2":"1";return new[]{"B_Skill_Begin_"+n,"B_Skill_Loop_"+n,"B_Skill_End_"+n};}
            if(u.CardId=="CARD_B23"&&skill=="gospel")return new[]{"A_Skill_2_Begin","A_Skill_2_Loop","A_Skill_2_End"};
            if(u.CardId=="CARD_B25"&&(skill=="retribution"||skill=="judgement")){string n=skill=="retribution"?"1":"2";return new[]{prefix+"Skill_"+n+"_Begin",prefix+"Skill_"+n+"_Loop",prefix+"Skill_"+n+"_End"};}
            if(u.CardId=="CARD_B26"&&skill=="beams")return new[]{"Skill_2_Begin","Skill_2_Loop","Skill_2_End"};return Array.Empty<string>();
        }
        void BossAfter(DeployedUnitState u,double delay,Action apply,bool surviveDeath=false)
        {var s=BossRoster(u);bossPending.Add(new BossPending{At=Clock+Math.Max(0,delay),Order=++bossPendingSequence,Owner=u.EntityId,Generation=s?.Generation??0,Apply=apply,SurviveDeath=surviveDeath});}
        void StepBossRoster(float step)
        {
            if(!EngagementEnabled)return;
            if(bossSummonQueue.Count>0){var pending=bossSummonQueue.ToArray();bossSummonQueue.Clear();foreach(var spawn in pending)spawn();}
            foreach(var due in bossPending.Where(x=>x.At<=Clock+1e-6).OrderBy(x=>x.At).ThenBy(x=>x.Order).ToArray())
            {bossPending.Remove(due);var owner=FindUnit(due.Owner);var s=BossRoster(owner);if(s!=null&&(due.SurviveDeath||s.Generation==due.Generation)&&(due.SurviveDeath||owner.Active&&!s.Final))due.Apply();}
            foreach(var u in units.ToArray())
            {
                var s=BossRoster(u);if(s==null||!u.Active||s.Final)continue;
                if(!s.Transitioning&&Clock>=s.LockedUntil)foreach(var spec in BossRosterRules.Skills(u.CardId))s.SP[spec.Id]=Math.Min(spec.RequiredSP,s.SP[spec.Id]+step);
                if(BossRosterRules.Skills(u.CardId).Count>0)u.SkillSP=s.SP[BossRosterRules.Skills(u.CardId)[0].Id];
                StepBossRosterPassive(u,s,step);
            }
            StepBossHoundStatuses();StepBossRosterObjects(step);StepBossCoreExtensions(step);
        }
        bool StepBossRosterUnit(DeployedUnitState u,float step)
        {
            if(BossSleep(u)){u.IsMoving=false;return true;}if(StepBossCoreUnit(u,step))return true;var s=BossRoster(u);if(s==null)return false;if(s.Transitioning||!s.Present||Clock<Timer(s,"palsy")||Clock<s.LockedUntil||u.CardId=="CARD_B14"&&s.Phase>=2){u.IsMoving=false;return true;}
            var c=UnitCombat(u);float range=u.CardId=="CARD_B13"&&s.Phase>=2?3.5f:u.CardId=="CARD_B11"&&s.Phase>=2?100:c.UnitRange;var target=u.CardId=="CARD_B10"||u.CardId=="CARD_B21"||u.CardId=="CARD_B25"&&!u.IsBlocked?null:ChooseDefender(u,range);
            u.HasCombatTarget=target!=null;u.CombatTargetId=target?.EntityId;
            u.IsMoving=!u.IsBlocked&&target==null&&!(u.CardId=="CARD_B11"&&s.Phase>=2);
            if(u.IsMoving){double before=u.Motion.DistanceTravelled;u.Motion.Advance(step*BossRosterMovementMultiplier(u)*UnitMoveMultiplier(u));ClampApprovedRouteMotion(u,before);if(!s.AnimationLoop||s.Animation!=BossMove(u,s))BossSetAnimation(u,BossMove(u,s),true);}
            if(u.Motion.HasArrived){if(BossArenaEnabled)u.IsMoving=false;else{u.Resolution=UnitResolution.Breached;u.IsMoving=u.HasCombatTarget=u.IsBlocked=false;RegisterStrongholdBreach(u);BossEmit(u,"Despawn","exit",u.Motion.Position);}return true;}
            if(target!=null&&Clock+1e-6>=u.NextAttackTime)BossOrdinaryAttack(u,target);
            else if(!u.IsMoving&&Clock>=s.AnimationUntil&&!s.AnimationLoop)BossSetAnimation(u,BossIdle(u,s),true);
            return true;
        }
        public bool BossRosterMoveLock(DeployedUnitState u){var s=BossRoster(u);return BossCoreActionLocked(u)||s!=null&&(s.Transitioning||!s.Present||Clock<s.LockedUntil||Clock<Timer(s,"palsy"));}
        public bool BossRosterInvulnerable(DeployedUnitState u){var s=BossRoster(u);return BossCoreInvulnerable(u)||s!=null&&(u.CardId=="CARD_B10"||s.Transitioning||Clock<s.InvulnerableUntil||!s.Present||u.CardId=="CARD_B14"&&s.Phase>=2||u.CardId=="CARD_B12"&&s.Phase==1&&s.Region<3&&u.Health<=u.MaxHealth*.5f+.0001f);}
        public bool BossRosterUnblockable(DeployedUnitState u)=>u!=null&&(u.CardId=="CARD_B10"||u.CardId=="CARD_B11"||u.CardId=="CARD_B14"&&BossRoster(u)?.Phase>=2);
        public bool HasPendingBossRosterDamage=>bossPending.Any(p=>p.SurviveDeath)||corePending.Any(p=>p.Survive);
        public double BossRosterPendingUntil=>Math.Max(bossPending.Where(p=>p.SurviveDeath).Select(p=>p.At).DefaultIfEmpty(double.NegativeInfinity).Max(),corePending.Where(p=>p.Survive).Select(p=>p.At).DefaultIfEmpty(double.NegativeInfinity).Max());
        public bool BossRosterInvisible(DeployedUnitState u){var s=BossRoster(u);return u!=null&&u.Active&&(BossCoreCamouflaged(u)||s!=null&&u.CardId=="CARD_B04"&&s.Phase>=2||BossInObject("smoke",u.Motion.Position)&&!BossInObject("flare",u.Motion.Position));}
        public bool BossRosterAirborne(DeployedUnitState u)=>BossRoster(u)?.Phase==3&&u.CardId=="CARD_B13";
        // The existing shared API means every supported control at once. Original
        // bosses have different per-control flags; absence of a typed API grants none.
        public bool BossRosterControlImmune(DeployedUnitState u)=>false;
        public float BossRosterMovementMultiplier(DeployedUnitState u){var s=BossRoster(u);return s!=null&&u.CardId=="CARD_B22"&&Timer(s,"rage")>Clock?1.5f:1;}
        float BossRosterAdjustIncomingDamage(DeployedUnitState u,float amount,CombatDamageType type)
        {
            amount*=BossCoreIncomingDamageMultiplier(u);if(units.Any(x=>x!=u&&x.Active&&x.CardId=="CARD_B11"))amount*=.8f;var s=BossRoster(u);if(s==null)return amount;s.UnattributedDamage=0;float scale=s.DamageMultiplier;var source=s.IncomingDefender;s.IncomingDefender=null;
            if(u.CardId=="CARD_B22")scale=Timer(s,"rage")>Clock?.35f:1;
            if(u.CardId=="CARD_B25")scale*=Math.Max(.25f,1-s.Matrix*.08f);
            if(u.CardId=="CARD_B09"&&source!=null&&!bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Shield<=0&&GroundDistance(source.Position,o.Position)<=o.Radius))scale*=.3f;
            if(u.CardId=="CARD_B11"&&s.Phase>=2&&source!=null&&source.Position.x>=u.Motion.Position.x)scale*=.1f;
            if(u.CardId=="CARD_B12"&&s.Phase==1)scale*=.5f;
            if(u.CardId=="CARD_B08"&&BossInObject("fire",u.Motion.Position))scale*=.3f;
            if((u.CardId=="CARD_B15"||u.CardId=="CARD_B24")&&bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&(o.Kind=="core"||o.Kind=="deyin")))scale*=.3f;
            return amount*scale;
        }
        float BossRosterAbsorbHealthDamage(DeployedUnitState u,float amount)
        {
            if(bossAllyBuffs.TryGetValue(u.EntityId,out var buff)&&buff.ImmortalUntil>Clock)amount=Math.Min(amount,Math.Max(0,u.Health-1));var s=BossRoster(u);if(s==null)return amount;
            if(u.CardId=="CARD_B14"&&s.Phase==3){var avatar=bossRosterObjects.FirstOrDefault(o=>o.OwnerId==u.EntityId&&o.Kind=="pain"&&o.Alive);if(avatar!=null){BossObjectDamage(avatar,amount);return 0;}}
            if(s.Shield>0){float block=Math.Min(s.Shield,amount);s.Shield-=block;amount-=block;RecordUnitShieldAbsorption(u,block);}
            if(Timer(s,"immortal")>Clock)amount=Math.Min(amount,Math.Max(0,u.Health-1));
            if(u.CardId=="CARD_B12"&&s.Phase==1&&s.Region<3)amount=Math.Min(amount,Math.Max(0,u.Health-u.MaxHealth*.5f));return amount;
        }
        void BossRosterHealthChanged(DeployedUnitState u,float beforeHealth)
        {
            var s=BossRoster(u);if(s==null)return;s.UnattributedDamage=Math.Max(0,beforeHealth-u.Health);BossThresholds(u,s);
        }
        void BossRosterBeginIncomingDamage(DeployedUnitState u,DefenderState sourceDefender=null){BossCoreSetIncomingSource(u,sourceDefender);var s=BossRoster(u);if(s!=null){s.UnattributedDamage=0;s.IncomingDefender=sourceDefender;}}
        float BossRosterAdjustPureDamage(DeployedUnitState u,float amount)=>amount*BossCoreIncomingDamageMultiplier(u);
        void BossRosterRecordDefenderHit(DefenderState d,DeployedUnitState u)
        {var s=BossRoster(u);if(s==null||d==null)return;s.DamageByDefender.TryGetValue(d.EntityId,out float old);s.DamageByDefender[d.EntityId]=old+s.UnattributedDamage;s.UnattributedDamage=0;}
        void OnBossRosterDefenderDamage(DeployedUnitState u,DefenderState d,float amount){var s=BossRoster(u);if(s==null||d==null)return;s.DamageByDefender.TryGetValue(d.EntityId,out float old);s.DamageByDefender[d.EntityId]=old+Math.Max(0,amount);s.UnattributedDamage=0;}
        bool BossRosterPreventDeath(DeployedUnitState u)
        {
            var s=BossRoster(u);if(s==null)return false;
            if(u.CardId=="CARD_B14"&&s.Phase==1){BossPainPhase(u);return true;}
            if(u.CardId=="CARD_B22"&&s.Phase==1)s.Fled=true;
            if(u.CardId=="CARD_B08"&&s.Phase<3){BossSlowDance(u);return true;}
            if(u.CardId=="CARD_B12"&&s.Phase==2&&!s.Consumed.Contains("eternal")){s.Consumed.Add("eternal");s.Transitioning=true;u.Health=u.MaxHealth;BossAfter(u,2.5,()=>{s.Transitioning=false;BossOtherSkill(u,"eternal",++bossActivationSequence);});return true;}
            if(s.Phase==1&&new[]{"CARD_B04","CARD_B07","CARD_B09","CARD_B11","CARD_B12","CARD_B13","CARD_B15","CARD_B22","CARD_B23","CARD_B24","CARD_B25","CARD_B26"}.Contains(u.CardId)){BossBeginPhase(u);return true;}
            s.Generation++;s.Final=true;s.Transitioning=false;BossSetAnimation(u,BossDeath(u,s),false);BossEmit(u,"Death","death",u.Motion.Position,animation:s.Animation,end:s.AnimationUntil);return false;
        }
        public bool BossRosterFrame(DeployedUnitState u,out string raw,out double age,out bool loop)=>BossRosterFrame(u,Clock,out raw,out age,out loop);
        public bool BossRosterFrame(DeployedUnitState u,double presentationClock,out string raw,out double age,out bool loop)
        {var s=BossRoster(u);raw=s?.Animation;double clock=s!=null&&s.Final?Math.Max(Clock,presentationClock):Clock;age=s==null?0:Math.Max(0,clock-s.AnimationAt)*s.AnimationRate;loop=s?.AnimationLoop??false;return s!=null&&!string.IsNullOrEmpty(raw)&&BossRosterRules.Action(u.CardId,raw)!=null;}
        BossDefenderStatus BossDefender(DefenderState d){if(d==null)return null;if(!bossDefenderStates.TryGetValue(d.EntityId,out var s)){s=new BossDefenderStatus();bossDefenderStates.Add(d.EntityId,s);}return s;}
        public bool BossRosterDefenderActionLocked(DefenderState d)=>d!=null&&bossDefenderStates.TryGetValue(d.EntityId,out var s)&&s.ActionUntil>Clock;
        public bool BossRosterDefenderSkillLocked(DefenderState d)=>d!=null&&bossDefenderStates.TryGetValue(d.EntityId,out var s)&&s.SkillUntil>Clock;
        public bool BossRosterDefenderCanBlock(DefenderState d)=>d==null||!bossDefenderStates.TryGetValue(d.EntityId,out var s)||!s.CannotBlock||s.ActionUntil<=Clock;
        public bool BossRosterDefenderCanHeal(DefenderState d)=>d==null||!bossDefenderStates.TryGetValue(d.EntityId,out var s)||s.HealUntil<=Clock;
        public float BossRosterDefenderAttackRate(DefenderState d)=>d!=null&&bossDefenderStates.TryGetValue(d.EntityId,out var s)&&s.SlowUntil>Clock?s.Rate:1;
        static double Timer(BossRosterState s,string key)=>s.Timers.TryGetValue(key,out var value)?value:double.NegativeInfinity;
        static int Count(BossRosterState s,string key)=>s.Counts.TryGetValue(key,out var value)?value:0;
        bool BossInObject(string kind,Vector3 p)=>bossRosterObjects.Any(o=>o.Alive&&o.Kind==kind&&GroundDistance(o.Position,p)<=o.Radius);
        BossRosterObject BossObject(DeployedUnitState u,string kind,string name,Vector3 position,float hp,float radius,double duration=double.PositiveInfinity)
        {
            var o=new BossRosterObject{Id=$"run-{RunId}/boss-object-{++bossEventSequence}",Kind=kind,Name=name,OwnerId=u.EntityId,CardId=u.CardId,Position=position,Health=hp>0?hp:1,MaxHealth=hp>0?hp:1,Radius=radius,StartedAt=Clock,Until=Clock+duration,NextAt=Clock+1,ActivationId=BossRoster(u).Activation,Interaction=BossObjectInteractionFor(kind)};bossRosterObjects.Add(o);BossEmit(u,"Summon",kind,position,targetId:o.Id,activation:o.ActivationId);return o;
        }
        static BossObjectInteraction BossObjectInteractionFor(string kind)
        {switch(kind){case "oldfire":case "cannon":case "pain":case "hound":case "core":case "deyin":case "energy":case "soul":return BossObjectInteraction.Health;case "mark":case "beam":return BossObjectInteraction.HitCount;case "camera":return BossObjectInteraction.CameraSeal;case "node":return BossObjectInteraction.Occupy;case "matrix":return BossObjectInteraction.Absorb;default:return BossObjectInteraction.None;}}
        bool BossObjectDamageable(BossRosterObject o)=>o!=null&&o.Alive&&Clock>=o.InvulnerableUntil&&(o.Interaction==BossObjectInteraction.Health||o.Interaction==BossObjectInteraction.HitCount||o.Interaction==BossObjectInteraction.Absorb||o.Interaction==BossObjectInteraction.CameraSeal&&o.Shield>0&&Clock<o.BuffUntil);
        void BossSetAnimation(DeployedUnitState u,string raw,bool loop,double duration=-1)
        {var s=BossRoster(u);if(s==null||BossRosterRules.Action(u.CardId,raw)==null)return;s.Animation=raw;s.AnimationAt=Clock;s.AnimationUntil=loop?double.PositiveInfinity:Clock+(duration>=0?duration:BossRosterRules.Action(u.CardId,raw).Duration);s.AnimationLoop=loop;s.AnimationRate=1;}
        void BossEmit(DeployedUnitState u,string kind,string key,Vector3 target,string native="",string animation="",long activation=0,int hit=0,string targetId=null,bool applied=false,bool cancelled=false,double end=double.PositiveInfinity,Vector3? origin=null,int? phase=null,int? region=null)
        {var s=BossRoster(u);bossRosterEvents.Add(new BossRosterVisualEvent(++bossEventSequence,activation==0?s?.Activation??0:activation,RunId,u.EntityId,u.CardId,kind,key,string.IsNullOrEmpty(native)?BossNativeKey(u,key):native,animation,phase??s?.Phase??1,hit,Clock,end,origin??u.Motion.Position,target,targetId,applied,cancelled,region??s?.Region??1));}
        string BossAvailable(DeployedUnitState u,params string[] names)=>names.FirstOrDefault(n=>BossRosterRules.Action(u.CardId,n)!=null)??"";
        string BossIdle(DeployedUnitState u,BossRosterState s)=>u.CardId=="CARD_B21"?(s.Phase>=2?"Skill_Idle_B":"Skill_Idle_A"):u.CardId=="CARD_B07"?(s.Phase>=2?"Idle_b":"Idle_a"):u.CardId=="CARD_B13"&&s.Phase>=2?(s.Phase==3?"B_Idle_2":"B_Idle_1"):BossAvailable(u,s.Phase>=2?"B_Idle":"A_Idle",s.Phase>=2?"Idle_2":"Idle","Idle","A_Idle");
        string BossMove(DeployedUnitState u,BossRosterState s)
        {string raw=u.CardId=="CARD_B07"?(s.Phase>=2?"Move_b":"Move_a"):u.CardId=="CARD_B13"&&s.Phase>=2?(s.Phase==3?"B_Move_2":"B_Move_1"):BossAvailable(u,s.Phase>=2?"B_Move":"A_Move",s.Phase>=2?"Move_2":"Move","Move","A_Move");return string.IsNullOrEmpty(raw)?BossIdle(u,s):raw;}
        string BossDeath(DeployedUnitState u,BossRosterState s)=>u.CardId=="CARD_B13"&&s.Phase>=2?(s.Phase==3?"B_Die_2":"B_Die_1"):BossAvailable(u,"B_Die","Die_2","Die","A_Die");
        string BossAttack(DeployedUnitState u,BossRosterState s)
        {
            if(u.CardId=="CARD_B26")return s.Polarity=="yin"?"Attack_1":"Attack_2";
            if(u.CardId=="CARD_B22")return s.Phase>=2?"Attack_2":"Attack";
            if(u.CardId=="CARD_B23")return (s.Phase>=2?"B_":"A_")+(u.IsBlocked?"Attack_2":"Attack_1");
            if(u.CardId=="CARD_B07")return u.IsBlocked?(s.Phase>=2?"Combat_b":"Combat_a"):(s.Phase>=2?"Attack_b":"Attack_a");
            if(u.CardId=="CARD_B13"&&s.Phase>=2)return s.Phase==3?"B_Attack_Begin_2":"B_Attack_Begin_1";
            if(u.CardId=="CARD_B05"&&u.IsBlocked)return "Combat";
            return BossAvailable(u,s.Phase>=2?"B_Attack":"A_Attack","Attack","A_Attack");
        }
        void BossOrdinaryAttack(DeployedUnitState u,DefenderState target)
        {
            var s=BossRoster(u);var c=UnitCombat(u);string raw=BossAttack(u,s);var clip=BossRosterRules.Action(u.CardId,raw);float duration=clip?.Duration??c.AttackDuration;
            float[] hits=clip!=null&&clip.Hits.Length>0?clip.Hits:new[]{c.AttackWindup};
            if(u.CardId=="CARD_B13"&&s.Phase>=2)
            {string n=s.Phase==3?"2":"1";var middle=BossRosterRules.Action(u.CardId,"B_Attack_Loop_"+n);var finish=BossRosterRules.Action(u.CardId,"B_Attack_End_"+n);double begin=duration;hits=middle.Hits.Take(3).Select(x=>(float)begin+x).ToArray();duration+=middle.Duration+finish.Duration;BossAfter(u,begin,()=>BossSetAnimation(u,"B_Attack_Loop_"+n,false));BossAfter(u,begin+middle.Duration,()=>BossSetAnimation(u,"B_Attack_End_"+n,false));}
            float rate=BossRosterUnitAttackRate(u);u.LastAttackTime=Clock;u.LastAttackRate=rate;u.LastAttackEmpowered=false;u.OrdinaryAttacksFired++;u.NextAttackTime=Clock+Math.Max(c.UnitInterval,duration)/rate;s.LockedUntil=Clock+duration/rate;s.Activation=++bossActivationSequence;BossSetAnimation(u,raw,false,duration/rate);s.AnimationRate=rate;long activation=s.Activation;
            BossEmit(u,"AttackStart","attack",target.Position,native:BossNativeKey(u,"attack"),animation:raw,activation:activation,targetId:target.EntityId,end:s.LockedUntil);
            if(u.CardId=="CARD_B22"&&s.Phase==1)hits=new[]{.6333333f,.8666667f};
            bool blocked=u.IsBlocked;for(int i=0;i<hits.Length;i++){int index=i;BossAfter(u,hits[i]/rate,()=>BossOrdinaryHit(u,target,activation,index,blocked));}
        }
        void BossOrdinaryHit(DeployedUnitState u,DefenderState target,long activation,int hit,bool blocked)
        {
            if(target==null)return;var s=BossRoster(u);float attack=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*s.AttackMultiplier;
            var projectile=BossRosterProjectiles.For(u.CardId,s.Phase,s.Region,s.Polarity,blocked);
            if(projectile.Ranged)
            {
                var targets=u.CardId=="CARD_B26"?BossLiving().OrderByDescending(d=>BossDefenderPolarity(d)!=s.Polarity).ThenBy(d=>GroundDistance(d.Position,u.Motion.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).Take(2).ToArray():new[]{target};
                if(u.CardId=="CARD_B13"){var choices=BossLiving().Where(d=>GroundDistance(d.Position,u.Motion.Position)<=3.5f).OrderBy(d=>GroundDistance(d.Position,u.Motion.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).ToArray();targets=choices.Length>0?new[]{choices[hit%choices.Length]}:Array.Empty<DefenderState>();}
                foreach(var selected in targets.Where(d=>d.Alive)){var d=selected;Vector3 destination=d.Position,origin=u.Motion.Position;int phase=s.Phase,region=s.Region;string native=BossNativeKey(u,"attack");double flight=Math.Min(10,GroundDistance(origin,destination)/projectile.Speed);float power=attack*(u.CardId=="CARD_B09"&&u.OrdinaryAttacksFired%4==0?1.5f:1);
                    BossEmit(u,"ProjectileSpawn","attack",destination,native:native,animation:s.Animation,activation:activation,hit:hit,targetId:d.EntityId,end:Clock+flight,origin:origin,phase:phase,region:region);
                    BossAfter(u,flight,()=>BossRangedImpact(u,d,destination,power,activation,hit,phase,region,native,origin),true);}
                return;
            }
            if(!target.Alive)return;
            if(u.CardId=="CARD_B22"&&s.Phase==1){if(Timer(s,"vulnerable/"+target.EntityId)>Clock)attack*=1.8f;s.Timers["vulnerable/"+target.EntityId]=Clock+4;}
            BossSpecialOrdinaryHit(u,target,attack,activation,hit);
        }
        void BossRangedImpact(DeployedUnitState u,DefenderState target,Vector3 center,float attack,long activation,int hit,int phase,int region,string native,Vector3 origin)
        {
            float radius=u.CardId=="CARD_B23"?1.1f:u.CardId=="CARD_B11"?(phase>=2?2:1.5f):0;var targets=radius>0?BossLiving().Where(d=>GroundDistance(d.Position,center)<=radius).ToArray():target.Alive?new[]{target}:Array.Empty<DefenderState>();
            if(targets.Length==0)BossEmit(u,"Impact","attack",center,native:native,activation:activation,hit:hit,targetId:target.EntityId,applied:false,origin:origin,phase:phase,region:region);
            foreach(var d in targets){float before=d.Health,debt=d.Debt,total=defenders.Sum(x=>x.Health);if(u.CardId=="CARD_B12")SourcedTrueDamage(d,attack,u);else DamageDefenderFrom(d,attack,u.CardId=="CARD_B01"||u.CardId=="CARD_B05"||u.CardId=="CARD_B23"?CombatDamageType.Physical:CombatDamageType.Arts,u);bool applied=d.Health<before||d.Debt>debt||defenders.Sum(x=>x.Health)<total;BossEmit(u,"Impact","attack",d.Position,native:native,activation:activation,hit:hit,targetId:d.EntityId,applied:applied,phase:phase,region:region);if(u.CardId=="CARD_B09"&&applied&&d.Alive)ApplyNeuralDamage(d,15,u);}
        }
        void BossHit(DeployedUnitState u,DefenderState d,float amount,CombatDamageType type,string key,long activation=0,int hit=0,bool trueDamage=false)
        {if(d==null||!d.Alive)return;float before=d.Health,debt=d.Debt,total=defenders.Sum(x=>x.Health);if(trueDamage)SourcedTrueDamage(d,amount,u);else DamageDefenderFrom(d,amount,type,u);BossEmit(u,"Impact",key,d.Position,native:BossNativeKey(u,key),activation:activation,hit:hit,targetId:d.EntityId,applied:d.Health<before||d.Debt>debt||defenders.Sum(x=>x.Health)<total);}
        void BossArea(DeployedUnitState u,Vector3 center,float radius,float damage,CombatDamageType type,string key,long activation=0,bool trueDamage=false)
        {foreach(var d in defenders.Where(d=>d.Alive&&GroundDistance(d.Position,center)<=radius).ToArray())BossHit(u,d,damage,type,key,activation,trueDamage:trueDamage);}
        void BossThresholds(DeployedUnitState u,BossRosterState s)
        {
            if(u.CardId=="CARD_B22"&&s.Phase==1&&u.Health>0&&u.Health<u.MaxHealth*.3f&&!s.Fled)BossFlee(u);
            BossOtherThresholds(u,s);
        }
        void ExecuteBossRosterSkill(DeployedUnitState u,string id,long activation)
        {
            var s=BossRoster(u);var target=ChooseDefender(u,100);float attack=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*s.AttackMultiplier;
            if(u.CardId=="CARD_B22")
            {
                if(id=="rage"){s.Timers["rage"]=Clock+8;BossEmit(u,"StateStart",id,u.Motion.Position,activation:activation,end:Clock+8);}
                if(id=="flee")BossFlee(u);
                if(id=="rock")
                {var chosen=defenders.Where(d=>d.Alive).OrderByDescending(d=>s.DamageByDefender.TryGetValue(d.EntityId,out var v)?v:0).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();if(chosen!=null){Vector3 origin=u.Motion.Position,destination=chosen.Position;double flight=GroundDistance(origin,destination)/5;BossEmit(u,"ProjectileSpawn",id,destination,native:"ThrowStone",activation:activation,targetId:chosen.EntityId,end:Clock+flight);BossAfter(u,flight,()=>{int before=bossRosterEvents.Count;BossHit(u,chosen,attack*2,CombatDamageType.Physical,id,activation);if(bossRosterEvents.Count==before)BossEmit(u,"Impact",id,destination,native:"ThrowStone",activation:activation,targetId:chosen.EntityId,applied:false,origin:origin);BossLock(chosen,2,true);},true);}}
            }
            else BossOtherSkill(u,id,activation);
        }
        void BossLock(DefenderState d,float duration,bool block=false,bool heal=false){if(d==null||!d.Alive)return;var state=BossDefender(d);state.ActionUntil=Math.Max(state.ActionUntil,Clock+duration);state.SkillUntil=Math.Max(state.SkillUntil,Clock+duration);state.CannotBlock|=block;if(heal)state.HealUntil=Math.Max(state.HealUntil,Clock+duration);}
        void BossSlow(DefenderState d,float duration,float rate){var state=BossDefender(d);if(state.SlowUntil<=Clock)state.Rate=1;state.SlowUntil=Math.Max(state.SlowUntil,Clock+duration);state.Rate=Math.Min(state.Rate,rate);}
        void BossFlee(DeployedUnitState u){var s=BossRoster(u);if(s.Fled)return;s.Fled=true;BossBeginPhase(u,"Move_2_Begin");}
        void BossBeginPhase(DeployedUnitState u,string raw=null)
        {
            var s=BossRoster(u);if(s.Transitioning)return;s.Generation++;s.Transitioning=true;u.Health=Math.Max(1,u.Health);u.IsMoving=u.HasCombatTarget=u.IsBlocked=false;u.CombatTargetId=u.BlockerId=null;CancelUnitShots(u);
            string[] sequence=BossPhaseSequence(u);if(raw!=null)sequence=new[]{raw};double total=0;
            foreach(string part in sequence){var clip=BossRosterRules.Action(u.CardId,part);if(clip==null||clip.Duration<=0)continue;string captured=part;double at=total;if(at==0)BossSetAnimation(u,captured,false);else BossAfter(u,at,()=>BossSetAnimation(u,captured,false));total+=clip.Duration;}
            double delay=Math.Max(2.5,total);s.LockedUntil=Clock+delay;
            if(u.CardId=="CARD_B22"){s.Timers["retreatTo"]=Math.Max(0,u.Motion.DistanceTravelled-2);s.Timers["retreatUntil"]=s.LockedUntil;}
            if(u.CardId=="CARD_B15"||u.CardId=="CARD_B24"){string id=u.CardId=="CARD_B15"?"obituary":"deathKnock";BossAfter(u,3,()=>BossOtherSkill(u,id,++bossActivationSequence));}
            BossEmit(u,"PhaseStart","reborn",u.Motion.Position,animation:s.Animation,end:s.LockedUntil);
            BossAfter(u,delay,()=>{s.Phase++;s.Transitioning=false;s.LockedUntil=Clock;u.Health=u.MaxHealth;u.DeathTime=double.PositiveInfinity;u.NextAttackTime=Clock;BossSetAnimation(u,u.CardId=="CARD_B22"?"Move":BossIdle(u,s),true);BossEmit(u,"StateEnd","reborn",u.Motion.Position,animation:s.Animation);if(u.CardId=="CARD_B25")s.Matrix=3;});
        }
        string[] BossPhaseSequence(DeployedUnitState u)
        {
            switch(u.CardId){case "CARD_B04":return new[]{"Revive1","Revive2","Revive3"};case "CARD_B07":return new[]{"Die","Die_Idle","Die_End"};case "CARD_B09":return new[]{"Revive_Start","Revive_Loop","Revive_End"};case "CARD_B11":return new[]{"Revive_1","Revive_2","Revive_3"};case "CARD_B12":return new[]{"Revive_Begin","Revive_Loop","Revive_End"};case "CARD_B13":return new[]{"A_Revive_1","A_Revive_2","A_Revive_3"};case "CARD_B15":case "CARD_B24":return new[]{"A_Skill_2"};case "CARD_B23":return new[]{"A_Revive_1","A_Revive_2","A_Revive_3_B","B_Start"};case "CARD_B25":return new[]{"A_Revive_Begin","A_Revive_Loop","A_Revive_End"};case "CARD_B26":return new[]{"Revive_Begin","Revive_Loop","Revive_End"};default:return new[]{"Move_2_Begin"};}
        }
        void StepBossRosterPassive(DeployedUnitState u,BossRosterState s,float step)=>BossOtherPassive(u,s,step);
        void StepBossRosterObjects(float step)=>BossOtherObjects(step);
        public bool TryDamageBossRosterObject(int runId,string objectId,float damage)
        {if(runId!=RunId||IsPaused||Phase!=BattlePhase.Running||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return false;var o=bossRosterObjects.Find(x=>x.Id==objectId&&x.Alive);if(!BossObjectDamageable(o))return false;BossObjectDamage(o,damage);return true;}
    }
}
