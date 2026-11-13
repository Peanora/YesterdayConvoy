using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class BossCoreVisualState
    {
        public string EntityId,SkillId="",Animation="";public double At,Until,ActiveUntil,NextShotAt,LastShotAt=double.NegativeInfinity;
        public float Rate=1,SP;public int Phase=1;public bool Loop,Armed,ReviveSeen,ReviveComplete;public double RevivalAt,ProtectedUntil,NextIgniteAt,NextExplodeAt,NextEnergyAt,NextBlackIceAt;
        internal uint Random=739391;internal long Activation,SkillActivation;internal double LastRing=double.NegativeInfinity;
    }
    public sealed partial class ManualBattle
    {
        sealed class CorePending {internal double At;internal int Run;internal string Owner;internal Action Apply;internal bool Survive;}
        sealed class CoreBreath {internal string Source,Target;internal double At,Until,NextAt;}
        readonly Dictionary<string,BossCoreVisualState> bossCoreStates=new Dictionary<string,BossCoreVisualState>();
        readonly Dictionary<string,CoreBreath> coreBreaths=new Dictionary<string,CoreBreath>();
        readonly Dictionary<string,DefenderState> coreIncomingSources=new Dictionary<string,DefenderState>();
        readonly List<CorePending> corePending=new List<CorePending>();
        readonly HashSet<CombatShot> coreSeenShots=new HashSet<CombatShot>();
        readonly HashSet<(string Source,double Release)> coreSPAttacks=new HashSet<(string,double)>();
        readonly Dictionary<string,int> coreMarks=new Dictionary<string,int>();
        readonly Dictionary<long,string> coreRequests=new Dictionary<long,string>();
        readonly Dictionary<string,float> coreAppliedHealthDamage=new Dictionary<string,float>(StringComparer.Ordinal);
        string coreViviSkill="s3";
        public string BossCoreViviSkill=>coreViviSkill;
        public float BossCoreDamage(DeployedUnitState u,string skillId,string targetId=null)
        {if(u==null||string.IsNullOrEmpty(skillId))return 0;return coreAppliedHealthDamage.TryGetValue(u.EntityId+"/"+skillId+"/"+(targetId??"*"),out var value)?value:0;}
        bool BossCoreMayRegenerateLegacySP(DeployedUnitState u)
        {if(u==null||!u.Active||u.IsReviving||TalulahTransitionLocked(u))return false;var c=UnitCombat(u);return u.CardId=="CARD_TALULAH_TRIAL"&&c.FireRain&&Clock>=u.FireRainStartedAt+c.SkillInterval||u.CardId=="CARD_M14"&&c.FrostSeal&&Clock>=u.FrostSealStartedAt+c.SkillInterval;}
        public void ConfigureBossCoreViviSkill(string skillId)
        {if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Select Vivi skill during preparation.");if(skillId!="s1"&&skillId!="s2"&&skillId!="s3")throw new ArgumentException("Choose s1, s2 or s3.");coreViviSkill=skillId;}
        static bool HasCoreExtension(DeployedUnitState u)=>u!=null&&(u.CardId=="CARD_TALULAH_TRIAL"||u.CardId=="CARD_M14"||ViviCombat.IsVivi(u.CardId));
        public BossCoreVisualState BossCoreState(DeployedUnitState u)
        {
            if(!HasCoreExtension(u))return null;if(bossCoreStates.TryGetValue(u.EntityId,out var state))return state;
            state=new BossCoreVisualState{EntityId=u.EntityId,SP=coreViviSkill=="s2"?15:0,NextIgniteAt=u.SpawnTime+19,NextExplodeAt=u.SpawnTime+35,NextEnergyAt=u.SpawnTime+75,NextBlackIceAt=u.SpawnTime+35};foreach(char c in u.EntityId)state.Random=(state.Random^c)*16777619;bossCoreStates.Add(u.EntityId,state);
            if(ViviCombat.IsVivi(u.CardId))CoreSpawnSoul(u,1);return state;
        }
        public bool UsesBossCoreSkill(DeployedUnitState u)=>u!=null&&ViviCombat.IsVivi(u.CardId)&&coreViviSkill!="s3";
        public IReadOnlyList<BossRosterSkillInfo> BossCoreSkills(DeployedUnitState u)
        {
            if(!UsesBossCoreSkill(u))return Array.Empty<BossRosterSkillInfo>();var s=BossCoreState(u);float required=coreViviSkill=="s1"?2:25;var failure=CheckBossCoreSkillCore(u,false);
            return new[]{new BossRosterSkillInfo{SkillId=coreViviSkill,Name=coreViviSkill=="s1"?"定点清算":"饱和复仇",Description=coreViviSkill=="s1"?"攻击回复；强化下一击并追加余震与晕眩":AutomaticSkillsEnabled?"满技力后自动开启25秒，三目标；后半段过载四连发":"持续25秒，三目标；后半段过载四连发，可主动关闭",Ready=failure==SkillFailure.None,Failure=IsPaused?SkillFailure.Paused:failure,SP=s.SP,MaxSP=required}};
        }
        public SkillFailure CheckBossCoreSkill(DeployedUnitState u)=>CheckBossCoreSkillCore(u,true);
        SkillFailure CheckBossCoreSkillCore(DeployedUnitState u,bool pause)
        {
            if(!UsesBossCoreSkill(u))return SkillFailure.NoSkill;if(Phase!=BattlePhase.Running)return SkillFailure.NotRunning;if(pause&&IsPaused)return SkillFailure.Paused;if(!u.Active||u.IsReviving)return SkillFailure.MissingEntity;
            var s=BossCoreState(u);if(s.Armed||Clock<s.Until)return SkillFailure.AlreadyActive;
            if(coreViviSkill=="s2"&&Clock<s.ActiveUntil)return SkillFailure.None;
            return s.SP+.00001f>=(coreViviSkill=="s1"?2:25)?SkillFailure.None:SkillFailure.NotReady;
        }
        public SkillFailure TryActivateBossCoreSkill(int runId,long requestId,string entityId)
        {
            if(runId!=RunId)return SkillFailure.StaleRun;var u=FindUnit(entityId);string key=entityId+"/"+coreViviSkill;if(coreRequests.TryGetValue(requestId,out var old))return old==key?SkillFailure.None:SkillFailure.StaleRun;
            var failure=CheckBossCoreSkill(u);if(failure!=SkillFailure.None)return failure;var s=BossCoreState(u);coreRequests.Add(requestId,key);
            if(coreViviSkill=="s2"&&Clock<s.ActiveUntil){CoreStopVivi(u,s);return SkillFailure.None;}
            s.SP=0;s.Activation=++bossActivationSequence;s.SkillActivation=s.Activation;s.Phase=1;u.SkillsUsed++;SkillCasts++;s.SkillId=coreViviSkill;
            if(coreViviSkill=="s1")s.Armed=true;
            else {s.ActiveUntil=Clock+25;s.NextShotAt=Clock;CoreAnimate(u,"Skill_2_Begin","s2");CoreEmit(u,"SkillStart","s2","skchr_wisdel_2",u.Motion.Position,animation:"Skill_2_Begin",end:s.ActiveUntil);}
            return SkillFailure.None;
        }
        void ResetBossCoreExtensions(){bossCoreStates.Clear();coreBreaths.Clear();coreIncomingSources.Clear();corePending.Clear();coreSeenShots.Clear();coreSPAttacks.Clear();coreMarks.Clear();coreRequests.Clear();coreAppliedHealthDamage.Clear();}
        void CoreAfter(DeployedUnitState u,double delay,Action action,bool survive=false)=>corePending.Add(new CorePending{At=Clock+Math.Max(0,delay),Run=RunId,Owner=u.EntityId,Apply=action,Survive=survive});
        void StepBossCoreExtensions(float step)
        {
            if(AutomaticSkillsEnabled&&coreViviSkill=="s1")
                foreach(var shot in combatShots)
                {
                    if(shot.FromDefender||shot.Empowered||shot.SynergyOrder||shot.Erased||Clock+1e-6<shot.StartedAt||coreSPAttacks.Contains((shot.SourceId,shot.StartedAt)))continue;
                    var attacker=FindUnit(shot.SourceId);
                    if(attacker==null||!attacker.Active||attacker.IsReviving||!ViviCombat.IsVivi(attacker.CardId))continue;
                    coreSPAttacks.Add((shot.SourceId,shot.StartedAt));var state=BossCoreState(attacker);state.SP=Math.Min(2,state.SP+1);
                }
            foreach(var due in corePending.Where(x=>x.At<=Clock+1e-6).OrderBy(x=>x.At).ToArray())
            {corePending.Remove(due);var owner=FindUnit(due.Owner);if(due.Run==RunId&&owner!=null&&(due.Survive||owner.Active&&!owner.IsReviving))due.Apply();}
            foreach(var u in units.ToArray())
            {
                var s=BossCoreState(u);if(s==null||!u.Active)continue;ObserveAutomaticLegacyMonsterSkills(u);
                if(ViviCombat.IsVivi(u.CardId))
                {if(coreViviSkill=="s2"&&Clock>=s.ActiveUntil)s.SP=Math.Min(25,s.SP+step);if(s.SkillId=="s2"&&s.ActiveUntil>0&&Clock>=s.ActiveUntil&&(!AutomaticSkillsEnabled||Clock>=s.Until))CoreStopVivi(u,s);if(coreViviSkill=="s3"&&s.SkillId=="s3"&&u.SkillAmmo==0&&!IsSkillActionPlaying(u)){CoreEmit(u,"SkillEnd","s3","skchr_wisdel_3",u.Motion.Position,activation:s.SkillActivation);s.SkillId="";}}
                    if(u.CardId=="CARD_TALULAH_TRIAL"&&!u.IsReviving&&!TalulahTransitionLocked(u))
                {
                    int phase=TalulahSecondForm(u)?2:1;
                    if(phase!=s.Phase){s.Phase=phase;s.NextIgniteAt=u.TalulahPhaseStartedAt+19;s.NextExplodeAt=u.TalulahPhaseStartedAt+35;s.NextEnergyAt=u.TalulahPhaseStartedAt+75;}
                    bool automaticBodyReady=!AutomaticSkillsEnabled||AutomaticLegacyBodyAvailable(u);
                    if(Clock>=s.NextIgniteAt&&Clock>=s.Until&&!IsSkillActionPlaying(u)&&automaticBodyReady){s.NextIgniteAt=Clock+19;CoreTalulahCast(u,"ignite");}
                    if(Clock>=s.NextExplodeAt&&Clock>=s.Until&&!IsSkillActionPlaying(u)&&(!AutomaticSkillsEnabled||AutomaticLegacyBodyAvailable(u))){s.NextExplodeAt=Clock+35;CoreTalulahCast(u,"detonate");}
                    if(s.Phase>=2&&Clock>=s.NextEnergyAt&&Clock>=s.Until&&(!AutomaticSkillsEnabled||AutomaticLegacyBodyAvailable(u))){s.NextEnergyAt=Clock+50;CoreTalulahCast(u,"energy");}
                }
                if(u.CardId=="CARD_M14"&&UnitCombat(u).Effects.FrostNova)
                {
                    if(u.IsReviving&&!s.ReviveSeen){s.ReviveSeen=true;s.RevivalAt=u.ReviveAt;}
                    if(s.ReviveSeen&&!s.ReviveComplete&&Clock+1e-6>=s.RevivalAt){s.ReviveComplete=true;s.Phase=2;s.ProtectedUntil=s.RevivalAt+20;s.NextBlackIceAt=Clock+35;CoreColdWave(u);}
                    if(!u.IsReviving&&Clock>=s.NextBlackIceAt&&Clock>=s.Until&&!IsSkillActionPlaying(u)&&(!AutomaticSkillsEnabled||AutomaticLegacyBodyAvailable(u))){s.NextBlackIceAt=Clock+35;CoreBlackIce(u);}
                }
            }
            foreach(var pair in coreBreaths.ToArray())
            {
                var breath=pair.Value;var target=FindDefender(breath.Target);var source=FindUnit(breath.Source);
                if(target==null||!target.Alive||source==null||Clock>=breath.Until){coreBreaths.Remove(pair.Key);continue;}
                if(Clock+1e-6>=breath.NextAt){breath.NextAt+=1;float fraction=.1f+.36f*Mathf.Clamp01((float)((Clock-breath.At)/30));CoreHit(source,target,UnitCombat(source).UnitAttack*UnitAttackMultiplier(source)*fraction,CombatDamageType.Arts,"breath","Ignite",true);}
            }
            CoreStepObjects(step);
        }
        bool StepBossCoreUnit(DeployedUnitState u,float step)
        {
            if(!UsesBossCoreSkill(u))return false;var s=BossCoreState(u);if(!u.Active||u.IsReviving)return false;
            if(Clock<s.Until){u.IsMoving=false;return true;}
            if(coreViviSkill=="s1")
            {
                if(!s.Armed||Clock+1e-6<u.NextAttackTime)return false;var target=ChooseDefender(u,UnitCombat(u).UnitRange);if(target==null)return false;
                s.Armed=false;s.Activation=++bossActivationSequence;CoreAnimate(u,"Skill_1","s1");u.LastAttackTime=Clock;u.LastAttackRate=1;u.NextAttackTime=Clock+UnitAttackInterval(u);u.IsMoving=false;
                CoreEmit(u,"SkillStart","s1","skchr_wisdel_1",target.Position,animation:"Skill_1",targetId:target.EntityId,end:s.Until);
                long activation=s.Activation;double contact=CoreContact(u,"Skill_1");CoreAfter(u,contact,()=>CoreLaunch(u,target,target.Position,UnitCombat(u).UnitAttack*UnitAttackMultiplier(u),"s1",3,1.2f,true));CoreAfter(u,s.Until-Clock,()=>CoreEmit(u,"SkillEnd","s1","skchr_wisdel_1",u.Motion.Position,activation:activation));return true;
            }
            if(coreViviSkill!="s2"||Clock>=s.ActiveUntil)return false;
            var c=UnitCombat(u);var primary=ChooseDefender(u,c.UnitRange);
            if(primary==null)return false;
            u.IsMoving=false;u.HasCombatTarget=true;u.CombatTargetId=primary.EntityId;
            bool overload=Clock>=s.ActiveUntil-12.5;if(overload&&s.Phase<2){s.Phase=2;CoreAnimate(u,"Skill_2_Overload_Begin","s2");CoreEmit(u,"StateStart","s2","skchr_wisdel_2",u.Motion.Position,animation:s.Animation,end:s.Until);return true;}string raw=overload?"Skill_2_Overload_Loop":"Skill_2_Loop";
            if(Clock+1e-6>=s.NextShotAt)
            {
                CoreAnimate(u,raw,"s2");s.Activation=++bossActivationSequence;s.LastShotAt=Clock;u.LastAttackTime=Clock;u.LastAttackRate=1;
                s.NextShotAt=Clock+Math.Max(.2f,c.UnitInterval-.7f);u.NextAttackTime=s.NextShotAt;
                var targets=BossLiving().Where(d=>GroundDistance(u.Motion.Position,d.Position)<=c.UnitRange).OrderBy(d=>GroundDistance(d.Position,u.Motion.Position)).ToArray();
                if(overload)
                {for(int i=0;i<4;i++){int index=i;CoreAfter(u,CoreContact(u,raw)+i*.1,()=>{var living=targets.Where(d=>d.Alive).ToArray();if(living.Length==0)return;var selected=living[(int)(CoreRandom(s)*(uint)living.Length)];CoreLaunch(u,selected,selected.Position,c.UnitAttack*UnitAttackMultiplier(u)*.8f,"s2",1,.5f,false,index);});}}
                else foreach(var target in targets.Take(3)){var d=target;CoreAfter(u,CoreContact(u,raw),()=>CoreLaunch(u,d,d.Position,c.UnitAttack*UnitAttackMultiplier(u)*1.35f,"s2",1,.5f,false));}
            }
            return true;
        }
        static float CoreRandom(BossCoreVisualState s){s.Random=s.Random*1664525+1013904223;return(s.Random&0xffffff)/16777216f;}
        void CoreStopVivi(DeployedUnitState u,BossCoreVisualState s)
        {bool overload=s.Phase>=2;s.ActiveUntil=0;CoreAnimate(u,overload?"Skill_2_Overload_End":"Skill_2_End","s2");CoreEmit(u,"SkillEnd","s2","skchr_wisdel_2",u.Motion.Position,animation:s.Animation,activation:s.SkillActivation);s.SkillId="";s.Phase=1;}
        double CoreContact(DeployedUnitState u,string raw){var clip=BossCoreSource.Action(u.CardId,raw);return clip!=null&&clip.Hits.Length>0?clip.Hits[0]:Math.Min(.5,clip?.Duration??.5);}
        void CoreAnimate(DeployedUnitState u,string raw,string skill,float rate=1)
        {var s=BossCoreState(u);var clip=BossCoreSource.Action(u.CardId,raw);if(clip==null)return;s.Animation=raw;s.SkillId=skill;s.At=Clock;s.Rate=rate;s.Until=Clock+clip.Duration/rate;s.Loop=false;}
        public bool BossCoreFrame(DeployedUnitState u,out string raw,out double age,out bool loop)=>BossCoreFrame(u,Clock,out raw,out age,out loop);
        public bool BossCoreFrame(DeployedUnitState u,double presentationClock,out string raw,out double age,out bool loop)
        {raw=null;age=0;loop=false;if(u==null||!u.Active||u.IsReviving||TalulahTransitionLocked(u)||!bossCoreStates.TryGetValue(u.EntityId,out var s)||Clock>=s.Until||string.IsNullOrEmpty(s.Animation))return false;raw=s.Animation;age=Math.Max(0,Clock-s.At)*s.Rate;loop=s.Loop;return true;}
        bool BossCoreActionLocked(DeployedUnitState u)=>u!=null&&bossCoreStates.TryGetValue(u.EntityId,out var s)&&Clock<s.Until;
        bool BossCoreInvulnerable(DeployedUnitState u)=>u!=null&&u.CardId=="CARD_M14"&&bossCoreStates.TryGetValue(u.EntityId,out var s)&&Clock<s.ProtectedUntil;
        public bool BossCoreCamouflaged(DeployedUnitState u)=>u!=null&&u.Active&&ViviCombat.IsVivi(u.CardId)&&bossRosterObjects.Any(o=>o.OwnerId==u.EntityId&&o.Kind=="soul"&&o.Alive&&GroundDistance(o.Position,u.Motion.Position)<=2);
        void BossCoreSetIncomingSource(DeployedUnitState u,DefenderState source){if(u!=null)coreIncomingSources[u.EntityId]=source;}
        float BossCoreIncomingDamageMultiplier(DeployedUnitState u)
        {
            if(u==null)return 1;coreIncomingSources.TryGetValue(u.EntityId,out var source);coreIncomingSources.Remove(u.EntityId);
            return u.CardId=="CARD_TALULAH_TRIAL"&&source!=null&&coreBreaths.TryGetValue(u.EntityId+"/"+source.EntityId,out var breath)&&breath.Until>Clock?.5f:1;
        }
        bool BossCoreUsesTrueOrdinary(DeployedUnitState source,CombatShot shot)=>source?.CardId=="CARD_TALULAH_TRIAL"&&shot!=null&&!shot.FromDefender&&!shot.FireRain&&!shot.Empowered;
        public void BossCoreOnUnitHit(DeployedUnitState source,DefenderState target,CombatShot shot)
        {
            if(source==null||target==null||shot==null||shot.FromDefender||!HasCoreExtension(source))return;
            if(source.CardId=="CARD_TALULAH_TRIAL"&&!shot.FireRain&&!shot.Empowered)
            {CoreApplyBreath(source,target);}
            if(source.CardId=="CARD_M14"&&!shot.Empowered&&shot.ColdDuration<=0&&target.Alive)TryApplyCold(target.EntityId,5);
            if(!ViviCombat.IsVivi(source.CardId)||!coreSeenShots.Add(shot))return;
            target=FindDefender(shot.TargetId)??target;
            var s=BossCoreState(source);if(coreViviSkill=="s1"&&!AutomaticSkillsEnabled)s.SP=Math.Min(2,s.SP+1);
            if(target.Alive)coreMarks[source.EntityId+"/"+target.EntityId]=1;
            Vector3 center=shot.Destination;float attack=UnitCombat(source).UnitAttack*UnitAttackMultiplier(source);
            CoreAfter(source,.3,()=>CoreShock(source,center,1.1f,shot.Empowered?shot.Attack*.5f:attack*.5f,"aftershock","Attack",shot.Empowered),true);
        }
        public void BossCoreOnIceRingHit(DeployedUnitState source,DefenderState target,bool applied)
        {if(source?.CardId!="CARD_M14"||target==null)return;var s=BossCoreState(source);CoreEmit(source,"Impact","iceRing",s.ReviveComplete?"IceBurst[Reborn]":"IceBurst",target.Position,targetId:target.EntityId,applied:applied);if(target.Alive&&applied)TryApplyCold(target.EntityId,10);}
        public void BossCoreOnIceRingStarted(DeployedUnitState source)
        {
            if(source?.CardId!="CARD_M14")return;var s=BossCoreState(source);bool reborn=s.ReviveComplete;string raw=reborn?"Skill_3":"Attack",native=reborn?"IceBurst[Reborn]":"IceBurst";
            float duration=V2Skill(source,"iceRing","actionDuration",3),clip=BossCoreSource.Action(source.CardId,raw).Duration;s.Activation=++bossActivationSequence;long activation=s.Activation;
            // Keep the established three-second gameplay window while displaying the
            // entire original clip. The animation is never borrowed for black ice.
            CoreAnimate(source,raw,"iceRing",clip/duration);CoreEmit(source,"SkillStart","iceRing",native,source.Motion.Position,animation:raw,activation:activation,end:s.Until);
            CoreAfter(source,duration,()=>CoreEmit(source,"SkillEnd","iceRing",native,source.Motion.Position,activation:activation));
        }
        float BossCoreIceRingMultiplier(DeployedUnitState source,DefenderState target)=>source?.CardId=="CARD_M14"&&target.FrozenUntil>Clock?2:1;
        float BossCoreIceRingRadius(DeployedUnitState source,float original)=>source?.CardId=="CARD_M14"&&BossCoreState(source)?.ReviveComplete==true?Math.Max(original,3.3f):original;
        public void BossCoreOnSkillStarted(DeployedUnitState u)
        {
            if(u==null||!ViviCombat.IsVivi(u.CardId)||coreViviSkill!="s3")return;var s=BossCoreState(u);s.Activation=++bossActivationSequence;s.SkillActivation=s.Activation;CoreAnimate(u,"Skill_3_Begin","s3");CoreSpawnSoul(u,2);CoreEmit(u,"SkillStart","s3","skchr_wisdel_3",u.Motion.Position,animation:"Skill_3_Begin");
        }
        void CoreLaunch(DeployedUnitState u,DefenderState primary,Vector3 center,float attack,string key,int shocks,float shockScale,bool stun,int hit=0)
        {
            var s=BossCoreState(u);double flight=GroundDistance(u.Motion.Position,center)/10;s.LastShotAt=Clock;long activation=s.Activation;
            CoreEmit(u,"ProjectileSpawn",key,key=="s1"?"skchr_wisdel_1":"skchr_wisdel_2",center,targetId:primary?.EntityId,end:Clock+flight,hit:hit,activation:activation);
            CoreAfter(u,flight,()=>
            {
                CoreArea(u,center,1.3f,attack,CombatDamageType.Physical,key,key=="s1"?"skchr_wisdel_1":"skchr_wisdel_2",activation);
                if(primary!=null&&primary.Alive){coreMarks[u.EntityId+"/"+primary.EntityId]=1;if(stun)BossLock(primary,1.5f);}
                for(int i=0;i<shocks;i++)CoreAfter(u,.25+i*.15,()=>CoreShock(u,center,1.3f,attack*shockScale,key,"Attack",false,activation),true);
            },true);
        }
        void CoreTryMarkExplosion(DeployedUnitState u,DefenderState target,Vector3 center,bool guaranteed)
        {if(!u.Active||target==null||!target.Alive||!coreMarks.ContainsKey(u.EntityId+"/"+target.EntityId))return;var s=BossCoreState(u);if(!guaranteed&&CoreRandom(s)>=.15f)return;coreMarks.Remove(u.EntityId+"/"+target.EntityId);CoreArea(u,center,1.1f,UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*1.5f,CombatDamageType.Physical,"markExplosion","Talent1");foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,center)<1.1f))BossLock(d,1);}
        void CoreShock(DeployedUnitState u,Vector3 center,float radius,float attack,string key,string native,bool guaranteed,long activation=0)
        {var targets=BossLiving().Where(d=>GroundDistance(d.Position,center)<=radius).ToArray();foreach(var d in targets)CoreHit(u,d,attack,CombatDamageType.Physical,key,native,activation:activation);foreach(var d in targets)CoreTryMarkExplosion(u,d,d.Position,guaranteed);}
        void CoreApplyBreath(DeployedUnitState u,DefenderState d)
        {if(d==null||!d.Alive)return;string key=u.EntityId+"/"+d.EntityId;if(coreBreaths.TryGetValue(key,out var existing))existing.Until=Clock+30.5;else coreBreaths[key]=new CoreBreath{Source=u.EntityId,Target=d.EntityId,At=Clock,Until=Clock+30.5,NextAt=Clock+1};CoreEmit(u,"StateStart","breath","Ignite",d.Position,targetId:d.EntityId,end:Clock+30.5);}
        void CoreTalulahCast(DeployedUnitState u,string id)
        {
            var s=BossCoreState(u);string suffix=TalulahSecondForm(u)?"B":"A",raw=id=="ignite"?"Skill_1_"+suffix:id=="detonate"?"Skill_2_"+suffix:"Skill_3";CoreAnimate(u,raw,id);s.Activation=++bossActivationSequence;
            string native=id=="ignite"?"Ignite":id=="detonate"?"DragonFireExplode":"SummonFlame";CoreEmit(u,"SkillStart",id,native,u.Motion.Position,animation:raw,end:s.Until);
            CoreAfter(u,CoreContact(u,raw),()=>
            {
                float attack=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u);
                if(id=="ignite")foreach(var d in BossLiving().Where(d=>!coreBreaths.ContainsKey(u.EntityId+"/"+d.EntityId)).Take(2).ToArray())CoreApplyBreath(u,d);
                else if(id=="detonate")foreach(var breath in coreBreaths.Where(x=>x.Value.Source==u.EntityId).ToArray())
                {var d=FindDefender(breath.Value.Target);coreBreaths.Remove(breath.Key);if(d==null||!d.Alive)continue;CoreHit(u,d,attack,CombatDamageType.Arts,id,native);foreach(var other in BossLiving().Where(x=>x!=d&&(Math.Abs(x.Position.x-d.Position.x)<.6f||Math.Abs(x.Position.z-d.Position.z)<.6f)&&GroundDistance(x.Position,d.Position)<=1.6f).ToArray()){CoreHit(u,other,attack,CombatDamageType.Arts,id,native);CoreApplyBreath(u,other);}}
                else {Vector3 position=CoreScenePoint(1);CoreObject(u,"energy","能量聚合体",position,u.MaxHealth*.2f,4).NextAt=Clock+25;}
            });
            CoreAfter(u,Math.Max(0,s.Until-Clock),()=>CoreEmit(u,"SkillEnd",id,native,u.Motion.Position));
        }
        Vector3 CoreScenePoint(int index)=>route[Mathf.Clamp(index,0,route.Length-1)];
        void CoreBlackIce(DeployedUnitState u)
        {
            var s=BossCoreState(u);string raw="Skill_1",native=s.Phase>=2?"IceShield[Reborn]":"IceShield";CoreAnimate(u,raw,"blackIce");s.Activation=++bossActivationSequence;CoreEmit(u,"SkillStart","blackIce",native,u.Motion.Position,animation:raw,end:s.Until);
            CoreAfter(u,CoreContact(u,raw),()=>
            {int n=s.Phase>=2?3:2;var positions=defenders.Select(d=>d.Settings.Position).Distinct().Where(p=>GroundDistance(p,u.Motion.Position)<=2).OrderBy(p=>p.x).ThenBy(p=>p.z).ToList();for(int i=0;i<n&&positions.Count>0;i++){int index=(int)(CoreRandom(s)*positions.Count);var p=positions[index];positions.RemoveAt(index);CoreObject(u,"blackIce","永久黑冰封地",p,1,.55f);foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,p)<.5f).ToArray())CoreHit(u,d,d.MaxHealth*4,CombatDamageType.Arts,"blackIce",native,true);}});
        }
        public bool BossCoreGroundSealed(Vector3 position)=>bossRosterObjects.Any(o=>o.Alive&&o.Kind=="blackIce"&&GroundDistance(o.Position,position)<o.Radius);
        void CoreColdWave(DeployedUnitState u)
        {
            foreach(var p in new[]{CoreScenePoint(0),CoreScenePoint(route.Length/2)}.Distinct())CoreObject(u,"iceCrystal","源石冰晶",p,1,3).NextAt=Clock+15;
            CoreEmit(u,"Summon","coldWave","SummonFrosts",u.Motion.Position);
            if(AutomaticSkillsEnabled)RecordAutomaticMonsterCast(u,"coldWave");
        }
        BossRosterObject CoreObject(DeployedUnitState u,string kind,string name,Vector3 position,float health,float radius)
        {var s=BossCoreState(u);var o=new BossRosterObject{Id=$"run-{RunId}/core-object-{++bossEventSequence}",OwnerId=u.EntityId,CardId=u.CardId,Kind=kind,Name=name,Position=position,Health=health,MaxHealth=health,Radius=radius,StartedAt=Clock,NextAt=Clock+5,ActivationId=s.Activation,Interaction=BossObjectInteractionFor(kind)};bossRosterObjects.Add(o);CoreEmit(u,"Summon",kind,kind,position,targetId:o.Id);return o;}
        void CoreSpawnSoul(DeployedUnitState u,int count)
        {for(int i=0;i<count&&bossRosterObjects.Count(o=>o.OwnerId==u.EntityId&&o.Kind=="soul"&&o.Alive)<3;i++){int n=bossRosterObjects.Count(o=>o.OwnerId==u.EntityId&&o.Kind=="soul"&&o.Alive);CoreObject(u,"soul","魂灵之影",u.Motion.Position+Vector3.forward*(n%2==0?.8f:-.8f),u.MaxHealth*.4f,2);}}
        void CoreStepObjects(float step)
        {
            foreach(var o in bossRosterObjects.Where(o=>o.Alive&&(o.Kind=="soul"||o.Kind=="energy"||o.Kind=="iceCrystal"||o.Kind=="energyFireball")).ToArray())
            {
                var owner=FindUnit(o.OwnerId);if(owner==null||!owner.Active&&o.Kind=="soul"){o.Removed=true;o.DestroyedAt=Clock;continue;}
                if(o.Kind=="energyFireball")
                {o.Position+=o.Velocity*step;var struck=BossLiving().FirstOrDefault(d=>GroundDistance(d.Position,o.Position)<=o.Radius);if(struck!=null){CoreHit(owner,struck,UnitCombat(owner).UnitAttack*UnitAttackMultiplier(owner),CombatDamageType.Arts,"energyFireball","EnergyCrystal");CoreApplyBreath(owner,struck);o.Removed=true;o.DestroyedAt=Clock;}else if(Clock>=o.Until){o.Removed=true;o.DestroyedAt=Clock;}continue;}
                if(o.Kind=="energy"&&Clock>=o.NextAt){CoreEnergyBurst(owner,o);continue;}
                if(Clock>=o.NextAt)
                {
                    if(o.Kind=="iceCrystal"){foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,o.Position)<=o.Radius).ToArray())TryApplyCold(d.EntityId,10);o.NextAt=Clock+15;CoreEmit(owner,"StateStart","coldWave","SummonFrosts",o.Position,targetId:o.Id,end:Clock+1);}
                    else
                    {var target=BossLiving().Where(d=>GroundDistance(d.Position,owner.Motion.Position)<=(o.Kind=="soul"?UnitCombat(owner).UnitRange:o.Radius)).OrderBy(d=>o.Kind=="soul"&&coreMarks.ContainsKey(owner.EntityId+"/"+d.EntityId)?1:0).ThenBy(d=>GroundDistance(d.Position,o.Position)).FirstOrDefault();
                     if(target!=null){var state=BossCoreState(owner);o.AttackStartedAt=Clock;o.ShotAt=double.NegativeInfinity;o.ImpactAt=Clock+.033333335+GroundDistance(o.Position,target.Position)/15;o.TargetId=target.EntityId;o.TargetPosition=target.Position;o.Animation="Attack";o.AnimationAt=Clock;o.AnimationUntil=Clock+2.0333333;o.AnimationLoop=false;long activation=++bossActivationSequence;o.ActivationId=activation;
                      CoreEmit(owner,"SkillStart","soul","sktok_wisdel_wward",target.Position,animation:"Attack",targetId:target.EntityId,activation:activation,end:o.AnimationUntil,origin:o.Position);
                      CoreAfter(owner,.033333335,()=>{if(o.Alive){o.ShotAt=Clock;CoreEmit(owner,"ProjectileSpawn","soul","sktok_wisdel_wward",target.Position,targetId:target.EntityId,activation:activation,end:o.ImpactAt,origin:o.Position);}});
                      CoreAfter(owner,o.ImpactAt-Clock,()=>{if(!target.Alive||o.ShotAt<o.AttackStartedAt)return;CoreHit(owner,target,UnitCombat(owner).UnitAttack*UnitAttackMultiplier(owner)*.6f,CombatDamageType.Arts,"soul","sktok_wisdel_wward",activation:activation);BossSlow(target,1,.2f);coreMarks[owner.EntityId+"/"+target.EntityId]=1;},true);
                      o.NextAt=Clock+(o.Kind=="soul"?5-(int)(CoreRandom(state)*3):3);}
                    }
                }
                if(!BossObjectDamageable(o))continue;foreach(var d in BossLiving().Where(d=>d.Settings.CanAttack&&DefenderCanAttack(d)&&d.NextAttackTime<=Clock&&!BossRosterDefenderActionLocked(d)&&DefenderRangeContains(d,o.Position)).ToArray())
                {d.NextAttackTime=Clock+DefenderEffectiveInterval(d);d.LastActionTime=Clock;if(!BossRosterDefenderAttackHits(d,d.Settings.DamageType))continue;float damage=CalculateDamage(DefenderSkillAttack(d),0,0,d.Settings.DamageType);o.Health=Math.Max(0,o.Health-damage);if(!o.Alive){o.DestroyedAt=Clock;CoreEmit(owner,"Despawn",o.Kind,o.Kind,o.Position,targetId:o.Id);break;}}
            }
        }
        void CoreEnergyBurst(DeployedUnitState owner,BossRosterObject crystal)
        {
            crystal.Removed=true;crystal.DestroyedAt=Clock;CoreEmit(owner,"Despawn","energy","EnergyCrystal",crystal.Position,targetId:crystal.Id,applied:true);
            foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {var ball=CoreObject(owner,"energyFireball","聚合体火球",crystal.Position,1,.4f);ball.Velocity=direction*2;ball.Until=Clock+10;CoreEmit(owner,"ProjectileSpawn","energyFireball","EnergyCrystal",crystal.Position+direction*20,targetId:ball.Id,end:ball.Until,origin:crystal.Position);}
        }
        void CoreHit(DeployedUnitState u,DefenderState d,float amount,CombatDamageType type,string skill,string native,bool trueDamage=false,long activation=0)
        {if(d==null||!d.Alive||amount<=0)return;float before=d.Health,debt=d.Debt;double total=defenders.Sum(x=>(double)x.Health);if(trueDamage)SourcedTrueDamage(d,amount,u);else DamageDefenderFrom(d,amount,type,u,derived:true);float loss=(float)Math.Max(0,total-defenders.Sum(x=>(double)x.Health));if(loss>0){foreach(string key in new[]{u.EntityId+"/"+skill+"/"+d.EntityId,u.EntityId+"/"+skill+"/*"}){coreAppliedHealthDamage.TryGetValue(key,out float old);coreAppliedHealthDamage[key]=old+loss;}}CoreEmit(u,"Impact",skill,native,d.Position,targetId:d.EntityId,applied:d.Health<before||d.Debt>debt||loss>0,activation:activation);}
        void CoreArea(DeployedUnitState u,Vector3 center,float radius,float amount,CombatDamageType type,string skill,string native,long activation=0)
        {foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,center)<=radius).ToArray())CoreHit(u,d,amount,type,skill,native,activation:activation);}
        void CoreEmit(DeployedUnitState u,string kind,string skill,string native,Vector3 target,string animation="",string targetId=null,bool applied=false,long activation=0,int hit=0,double end=double.PositiveInfinity,Vector3? origin=null)
        {var s=BossCoreState(u);bossRosterEvents.Add(new BossRosterVisualEvent(++bossEventSequence,activation==0?s.Activation:activation,RunId,u.EntityId,u.CardId,kind,skill,native,animation,s.Phase,hit,Clock,end,origin??u.Motion.Position,target,targetId,applied,false));if(AutomaticSkillsEnabled&&kind=="SkillStart"&&skill!="soul")RecordAutomaticMonsterCast(u,skill);}
    }
    internal static partial class BossCoreArt
    {
        static partial void QueryMechanicFrame(ManualBattle b,DeployedUnitState u,ref string raw,ref double age,ref bool loop,ref bool found)
        {if(b.BossCoreFrame(u,out var chosen,out var elapsed,out var repeats)){raw=chosen;age=elapsed;loop=repeats;found=true;}}
    }
}
