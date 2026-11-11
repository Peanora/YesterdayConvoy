using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class BossAllyBuff {internal double Until,SleepUntil,ImmortalUntil;internal float Attack=1,Rate=1,HealFraction;}
        readonly Dictionary<string,BossAllyBuff> bossAllyBuffs=new Dictionary<string,BossAllyBuff>();
        public float BossRosterUnitAttackMultiplier(DeployedUnitState u)=>u!=null&&bossAllyBuffs.TryGetValue(u.EntityId,out var b)&&b.Until>Clock?b.Attack:1;
        public float BossRosterUnitAttackRate(DeployedUnitState u)=>u!=null&&bossAllyBuffs.TryGetValue(u.EntityId,out var b)&&b.Until>Clock?b.Rate:1;
        BossAllyBuff BossBuff(DeployedUnitState u){if(!bossAllyBuffs.TryGetValue(u.EntityId,out var b)){b=new BossAllyBuff();bossAllyBuffs[u.EntityId]=b;}return b;}
        bool BossSleep(DeployedUnitState u)=>u!=null&&bossAllyBuffs.TryGetValue(u.EntityId,out var b)&&b.SleepUntil>Clock;
        IEnumerable<DefenderState> BossLiving()=>defenders.Where(d=>d.Alive);
        IEnumerable<DeployedUnitState> BossFriends(DeployedUnitState u)=>units.Where(a=>a!=u&&a.Active&&!a.IsReviving);
        void BossOtherSkill(DeployedUnitState u,string id,long activation)
        {
            var s=BossRoster(u);var target=ChooseDefender(u,100);float a=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*s.AttackMultiplier;Vector3 p=target?.Position??u.Motion.Position;
            switch(u.CardId)
            {
                case "CARD_B01":
                    foreach(var d in BossLiving().Where(x=>GroundDistance(x.Position,u.Motion.Position)<=2.5f).OrderBy(x=>GroundDistance(x.Position,u.Motion.Position)).Take(u.Health<u.MaxHealth*.5f?3:1).ToArray())
                    {var mark=BossObject(u,"c4","C4炸药",d.Position,1,.4f,3.2);mark.TargetId=d.EntityId;Vector3 origin=u.Motion.Position,endpoint=d.Position;BossEmit(u,"ProjectileSpawn",id,endpoint,native:"C4",activation:activation,targetId:d.EntityId,end:Clock+3.2);BossAfter(u,3.2,()=>{int before=bossRosterEvents.Count;BossHit(u,d,a*1.8f,CombatDamageType.Physical,id,activation);if(bossRosterEvents.Count==before)BossEmit(u,"Impact",id,endpoint,native:"C4",activation:activation,targetId:d.EntityId,applied:false,origin:origin);mark.Removed=true;mark.DestroyedAt=Clock;},true);}break;
                case "CARD_B03":BossBlink(u,target,0,id,activation);break;
                case "CARD_B04":
                    if(id=="dash")BossBlink(u,target,a*2,id,activation);
                    else if(id=="blizzard")BossArea(u,u.Motion.Position,2.2f,a*1.8f,CombatDamageType.Physical,id,activation);
                    else if(id=="killWindow")foreach(var d in BossLiving().ToArray()){d.SkillSP=0;BossLock(d,4,true,true);}break;
                case "CARD_B05":
                    if(id=="blink")BossBlink(u,target,0,id,activation);
                    else{BossArea(u,p,1.8f,a*1.6f,CombatDamageType.Physical,id,activation);BossObject(u,"flare","照明区域",p,1,2.2f,4);}break;
                case "CARD_B07":
                    if(id=="dominate")foreach(var d in BossLiving().OrderBy(x=>GroundDistance(x.Position,u.Motion.Position)).Take(s.Phase>=2?2:1).ToArray())
                    {BossLock(d,3,true);BossObject(u,"chain","焦竭之火",d.Position,a,1,3).TargetId=d.EntityId;BossAfter(u,3,()=>{BossArea(u,d.Position,1.6f,a*1.8f,CombatDamageType.Arts,id,activation);foreach(var x in BossLiving().Where(x=>GroundDistance(x.Position,d.Position)<1.6f).ToArray())ApplyBurn(x,70,u);});}
                    else if(id=="sacrifice")
                    {int n=0;foreach(var friend in BossFriends(u).Where(x=>UnitCombat(x).Effects.Flamechaser).Take(5).ToArray()){ApplyUnitHealthDamage(friend,friend.Health+1);n++;}s.Shield+=n*u.MaxHealth*.2f;BossEmit(u,"StateStart",id,u.Motion.Position,activation:activation,applied:n>0,end:Clock+8);}break;
                case "CARD_B08":
                    if(id=="grantFire"){BossArea(u,p,1.8f,a*1.7f,CombatDamageType.Arts,id,activation);foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,p)<1.8f).ToArray())BossObject(u,"fire","炽心",d.Position,1,.65f,15);}
                    else if(id=="blind")
                    {var strong=BossLiving().OrderByDescending(d=>d.Settings.Attack).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();if(strong!=null){BossLock(strong,4);for(int i=-1;i<=1;i++)BossObject(u,"oldfire","旧日之火",strong.Position+Vector3.forward*i,a*2,.65f,20);}}
                    else if(id=="flameDance")
                    {foreach(var d in BossLiving().Where(d=>BossInObject("fire",d.Position)).ToArray()){BossHit(u,d,a*2.4f,CombatDamageType.Arts,id,activation);for(int i=-2;i<=2;i++)BossObject(u,"fire","终舞火区",d.Position+Vector3.right*i,1,.65f,12);}}break;
                case "CARD_B09":
                    if(id=="absurdity")foreach(var camera in bossRosterObjects.Where(o=>o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Alive).Take(s.Phase>=2?2:1).ToArray())
                    {camera.Shield=a*3;camera.BuffUntil=Clock+8;BossAfter(u,8,()=>{if(camera.Alive&&camera.Shield>0){BossArea(u,camera.Position,camera.Radius,a*2,CombatDamageType.Arts,id,activation);foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,camera.Position)<camera.Radius).ToArray())ApplyNeuralDamage(d,70,u);}camera.Shield=0;camera.Health=camera.MaxHealth;});}
                    else if(id=="intermission")
                    {var ordered=BossLiving().Where(d=>GroundDistance(d.Position,u.Motion.Position)<=UnitCombat(u).UnitRange&&bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Shield<=0&&GroundDistance(d.Position,o.Position)<=o.Radius)).OrderBy(d=>GroundDistance(d.Position,u.Motion.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).ToArray();for(int i=0;i<ordered.Length;i++){var d=ordered[i];int index=i;BossAfter(u,i*.45,()=>{BossHit(u,d,a*(1+.2f*Math.Min(8,index)),CombatDamageType.Arts,id,activation,index);if(d.Alive)ApplyNeuralDamage(d,35,u);});}}break;
                case "CARD_B10":
                    s.Counts["potions"]=Count(s,"potions")+1;
                    if(Count(s,"potions")%2==1)foreach(var ally in BossFriends(u).ToArray()){var b=BossBuff(ally);b.Until=b.SleepUntil=Clock+5;b.HealFraction=.04f;}
                    else foreach(var ally in BossFriends(u).OrderBy(x=>x.Health/x.MaxHealth).Take(2).ToArray()){var b=BossBuff(ally);b.Until=b.ImmortalUntil=Clock+6;b.Attack=b.Rate=2;}
                    BossEmit(u,"StateStart",id,u.Motion.Position,activation:activation,end:Clock+6);break;
                case "CARD_B11":
                    if(id=="sluggish")
                    {foreach(var old in bossRosterObjects.Where(o=>o.Kind=="mark"&&o.OwnerId==u.EntityId&&o.Alive).ToArray()){var d=FindDefender(old.TargetId);if(d!=null&&d.Alive)BossExecute(u,d,id,activation);old.Removed=true;}
                     foreach(var d in BossLiving().Take(s.Phase>=2?1:2).ToArray()){var mark=BossObject(u,"mark","慵钝之印",d.Position,5,.5f);mark.TargetId=d.EntityId;BossLock(d,9999,true);}}
                    else if(id=="folly")
                    {var living=BossLiving().ToArray();for(int i=0;i<Math.Min(2,living.Length);i++){var d=living[i];var dest=living[(i+1)%living.Length];d.Position=dest.Settings.Position;d.HighGround=dest.HighGround;BossEmit(u,"Impact",id,d.Position,activation:activation,targetId:d.EntityId,applied:true);}}
                    else if(id=="edict")
                    {foreach(var friend in BossFriends(u)){var b=BossBuff(friend);b.Attack=1.35f;b.Until=Clock+6;}s.Timers["edictUntil"]=Clock+6;s.Counts["edictActivation"]=(int)activation;BossRememberEdictStuns(u,s,activation);BossAfter(u,6,()=>{foreach(var friend in BossFriends(u).Where(x=>s.Consumed.Contains("edict/"+activation+"/"+x.EntityId)).ToArray()){float before=friend.Health;TryDamageUnit(friend.EntityId,a*3,CombatDamageType.Arts);BossEmit(u,"Impact",id,friend.Motion.Position,activation:activation,targetId:friend.EntityId,applied:friend.Health<before);}s.Timers["edictUntil"]=Clock;});}
                    else if(id=="annihilation")foreach(var d in BossLiving().ToArray())BossHit(u,d,a*3,CombatDamageType.Arts,id,activation);break;
                case "CARD_B12":
                    if(id=="survival")
                    {var left=BossLiving().OrderBy(d=>d.Position.x).FirstOrDefault();if(left!=null){BossObject(u,"seal","封印区域",left.Position,1,.65f);BossExecute(u,left,id,activation);}s.Region=Math.Min(3,s.Region+1);}
                    else if(id=="blackCrown")
                    {s.BlackCrownCharges--;foreach(var d in BossLiving().Take(7).ToArray())BossHit(u,d,a*1.6f,CombatDamageType.Arts,id,activation,trueDamage:true);}
                    else if(id=="idea")
                    {var idea=BossObject(u,"idea","仅剩的创意",u.Motion.Position+Vector3.right*4,a*2,.4f,16);idea.Velocity=(u.Motion.Position-idea.Position).normalized*1.2f;}
                    else if(id=="eternal")
                    {foreach(var d in BossLiving().ToArray())BossExecute(u,d,id,activation);s.Phase=3;BossSetAnimation(u,"C_Start",false);s.LockedUntil=Clock+(BossRosterRules.Action(u.CardId,"C_Start")?.Duration??2);}
                    break;
                case "CARD_B13":
                    if(id=="rite")
                    {var dense=BossLiving().OrderByDescending(d=>BossLiving().Count(x=>GroundDistance(d.Position,x.Position)<1.5f)).FirstOrDefault();if(dense!=null)for(int i=0;i<8;i++)BossAfter(u,i*.5,()=>BossArea(u,dense.Position,1.5f,a*.35f,CombatDamageType.Arts,id,activation,true));}
                    else if(id=="sacrament")
                    {s.Transitioning=true;string leave=s.Phase==3?"B_Leave_2":"B_Leave_1";BossSetAnimation(u,leave,false);double delay=BossRosterRules.Action(u.CardId,leave)?.Duration??2;BossAfter(u,delay,()=>{s.Phase=s.Phase==2?3:2;s.Transitioning=false;u.Health=Math.Min(u.MaxHealth,u.Health+u.MaxHealth*.5f);BossSetAnimation(u,s.Phase==3?"B_Start_2":"B_Start_1",false);BossEmit(u,"PhaseStart",id,u.Motion.Position,animation:s.Animation,activation:activation);});}break;
                case "CARD_B14":
                    if(id=="roar")
                    {var nearby=BossLiving().Where(d=>GroundDistance(d.Position,u.Motion.Position)<2.5f).ToArray();if(nearby.Length>0)foreach(var d in nearby)BossHit(u,d,a*4/nearby.Length,CombatDamageType.Physical,id,activation);else foreach(var d in BossLiving().ToArray()){BossHit(u,d,a*2,CombatDamageType.Physical,id,activation);BossLock(d,2);}}
                    else if(id=="traverse")BossBlink(u,target,a, id,activation,true);
                    else if(id=="echoes")BossPainPhase(u);break;
                case "CARD_B15":
                    if(id=="sow")
                    {BossArea(u,p,1.7f,a*1.2f,CombatDamageType.Arts,id,activation);foreach(var d in BossLiving().Take(3).ToArray())BossObject(u,"domain","国度",d.Position,1,.7f,40);if(s.Phase>=2)BossObject(u,"core","惘核",u.Motion.Position+Vector3.forward,a*3,.5f,25);}
                    else if(id=="obituary")foreach(var d in BossLiving().Where(d=>BossInObject("domain",d.Position)).ToArray())BossExecute(u,d,id,activation);
                    else if(id=="domainBurst"){if(!s.PartnerDown)s.AttackMultiplier*=2;s.PartnerDown=true;foreach(var d in BossLiving().ToArray())BossObject(u,"domain","国度",d.Position,1,.75f,40);}break;
                case "CARD_B21":
                    if(id=="insomnia")
                    {s.Present=true;BossSetAnimation(u,"Skill_Begin_B",false);if(target!=null){double distance=u.Motion.ProjectDistance(target.Position);u.Motion.ApproachDistance(distance,10000);BossArea(u,target.Position,1.4f,a*5,CombatDamageType.Physical,id,activation);BossObject(u,"seal","封印地面",target.Position,1,.7f,30);}BossAfter(u,3,()=>BossOtherSkill(u,"mountains",activation));s.Timers["depart"]=Clock+20;}
                    else if(id=="mountains")for(int i=0;i<3;i++){int index=i;BossAfter(u,i*1.1,()=>{BossSetAnimation(u,"Skill_Loop_B",false);foreach(var d in BossLiving().ToArray())BossHit(u,d,a*1.3f,CombatDamageType.Physical,id,activation,index);});}
                    else if(id=="awaken")foreach(var d in BossLiving().ToArray())BossExecute(u,d,id,activation);break;
                case "CARD_B23":
                    if(id=="gospel")for(int i=0;i<10;i++){int index=i;BossAfter(u,i*.22,()=>{var d=BossLiving().OrderBy(x=>Count(s,"erosion/"+x.EntityId)).ThenBy(x=>x.EntityId,StringComparer.Ordinal).FirstOrDefault();if(d==null)return;BossHit(u,d,a*.4f,CombatDamageType.Physical,id,activation,index);s.Counts["erosion/"+d.EntityId]=Count(s,"erosion/"+d.EntityId)+15;if(Count(s,"erosion/"+d.EntityId)>=100){s.Counts["erosion/"+d.EntityId]=0;d.ArmorReduction+=3;BossHit(u,d,a,CombatDamageType.Physical,id,activation,index);}});}
                    else if(id=="seek")foreach(var cannon in bossRosterObjects.Where(o=>o.OwnerId==u.EntityId&&o.Kind=="cannon"&&o.Alive)){cannon.BuffUntil=Clock+6;cannon.PowerMultiplier=1.7f;cannon.NextAt=Clock+.1;}
                    else if(id=="pierce")BossBlink(u,target,a*1.8f,id,activation);break;
                case "CARD_B24":
                    if(id=="incite")
                    {BossArea(u,u.Motion.Position,1.8f,a*1.2f,CombatDamageType.Physical,id,activation);foreach(var d in defenders.Where(d=>d.Reserve))s.Timers["bench/"+d.EntityId]=double.PositiveInfinity;if(s.Phase>=2)BossObject(u,"deyin","殆因",u.Motion.Position+Vector3.forward,a*3,.5f,25);}
                    else if(id=="deployMarked")
                    {var bench=defenders.FirstOrDefault(d=>d.Reserve&&Timer(s,"bench/"+d.EntityId)>Clock);if(bench!=null){bench.Reserve=false;BossMarkedDeployment(u,bench);}}
                    else if(id=="deathKnock")foreach(var d in BossLiving().Where(d=>!BossInObject("domain",d.Position)&&!BossInObject("trench",d.Position)).ToArray())BossExecute(u,d,id,activation);break;
                case "CARD_B25":
                    if(id=="retribution")foreach(var d in BossLiving().Take(2).ToArray())
                    {Vector3 tile=d.Position;BossObject(u,"aim","报偿锁定",tile,1,.3f,5);BossAfter(u,5,()=>{BossArea(u,tile,1.3f,a*1.6f,CombatDamageType.Physical,id,activation);if(!d.Alive||GroundDistance(d.Position,tile)>.5f)BossObject(u,"fire","不甘的火",tile,1,.7f,15);else BossLock(d,2);});}
                    else if(id=="judgement")
                    {int hits=3+s.Matrix/2;for(int i=0;i<hits;i++){int index=i;BossAfter(u,i*.15,()=>{var d=ChooseDefender(u,4);if(d!=null)BossArea(u,d.Position,s.Phase>=2?1.8f:.9f,a*(s.Phase>=2?1:1.3f),CombatDamageType.Physical,id,activation);});}}
                    else if(id=="penalty")BossPenalty(u,p,activation);
                    else if(id=="absorb")BossMatrix(u,1);break;
                case "CARD_B26":
                    if(id=="balance"){s.Polarity=s.Polarity=="yin"?"yang":"yin";if(s.Phase>=2)s.Shield+=u.MaxHealth*.3f;BossEmit(u,"StateStart",id,u.Motion.Position,activation:activation,applied:true);}
                    else if(id=="beams")for(int i=0;i<(s.Phase>=2?4:2);i++){bool left=i%2==0;var beam=BossObject(u,"beam","梁",u.Motion.Position+Vector3.right*(left?-3:5)+Vector3.forward*(i/2-1),10,.3f,10);beam.Velocity=Vector3.right*(left?1.2f:-1.2f);beam.Polarity=left?"yin":"yang";}
                    break;
            }
        }
        void BossRememberEdictStuns(DeployedUnitState u,BossRosterState s,long activation)
        {foreach(var friend in BossFriends(u))if(!UnitControlImmune(friend)&&(friend.PillarStunnedUntil>Clock||friend.ShackleStunnedUntil>Clock||friend.SprayStunnedUntil>Clock))s.Consumed.Add("edict/"+activation+"/"+friend.EntityId);}
        void BossBlink(DeployedUnitState u,DefenderState target,float damage,string id,long activation,bool slow=false)
        {
            if(target==null)return;var from=u.Motion.Position;var aim=target.Position+u.Motion.Direction*1.1f;double distance=u.Motion.ProjectDistance(aim);u.Motion.ApproachDistance(distance,10000);
            if(u.BlockerId!=null){var old=FindDefender(u.BlockerId);if(old!=null)old.BlockedCount=Math.Max(0,old.BlockedCount-1);}u.IsBlocked=false;u.BlockerId=null;
            if(damage>0)foreach(var d in BossLiving().Where(d=>GroundDistance(d.Position,from)<=GroundDistance(aim,from)+.6f).ToArray()){BossHit(u,d,damage,slow?CombatDamageType.Arts:CombatDamageType.Physical,id,activation);if(slow)BossSlow(d,5,.7f);}
            var s=BossRoster(u);string end=u.CardId=="CARD_B04"?(s.Phase>=2?"B_Skill1_End":"A_Skill1_End"):u.CardId=="CARD_B05"?"Skill_End":u.CardId=="CARD_B14"?"Skill_2_End":u.CardId=="CARD_B23"?"B_Skill_1_End":"Appear";BossSetAnimation(u,end,false);s.LockedUntil=Math.Max(s.LockedUntil,s.AnimationUntil);BossEmit(u,"StateEnd",id,u.Motion.Position,animation:end,activation:activation,applied:true);
        }
        void BossExecute(DeployedUnitState u,DefenderState d,string id,long activation)
        {BossHit(u,d,d.MaxHealth*4+UnitCombat(u).UnitAttack,CombatDamageType.Arts,id,activation,trueDamage:true);}
        void BossMatrix(DeployedUnitState u,int amount)
        {var s=BossRoster(u);s.Matrix+=amount;if(s.Matrix>(s.Phase>=2?7:6)){s.Matrix=0;s.Timers["palsy"]=Clock+15;BossSetAnimation(u,s.Phase>=2?"B_Stun_Begin":"A_Stun_Begin",false);BossEmit(u,"StateStart","overload",u.Motion.Position,animation:s.Animation,end:Clock+15);BossAfter(u,15,()=>{BossSetAnimation(u,s.Phase>=2?"B_Stun_End":"A_Stun_End",false);BossEmit(u,"StateEnd","overload",u.Motion.Position,animation:s.Animation);});}}
        void BossPenalty(DeployedUnitState u,Vector3 center,long activation)
        {var s=BossRoster(u);if(Clock<Timer(s,"palsy")||BossInObject("matrixField",u.Motion.Position))return;float attack=UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*s.AttackMultiplier;foreach(var friend in units.Where(x=>x.Active&&x.CardId=="CARD_B25"&&GroundDistance(x.Motion.Position,u.Motion.Position)<=UnitCombat(u).UnitRange)){var other=BossRoster(friend);if(other.Matrix<(other.Phase>=2?7:6)&&Clock>=Timer(other,"palsy")&&!BossInObject("matrixField",friend.Motion.Position))BossMatrix(friend,1);}BossArea(u,center,1.5f,attack*(s.Phase>=2?.5f:.4f),CombatDamageType.Arts,"penalty",activation);foreach(var offset in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})BossObject(u,"fire","绝罚燃烧",center+offset,1,.55f,10);}
        void BossMarkedDeployment(DeployedUnitState u,DefenderState d)
        {
            var s=BossRoster(u);s.Timers.Remove("bench/"+d.EntityId);float before=d.Health;d.Health=Math.Max(1,d.Health-d.MaxHealth*.5f);RecordDefenderHealthLoss(d,before,d.Position);
            foreach(var o in bossRosterObjects.Where(o=>o.Kind=="domain"&&GroundDistance(o.Position,d.Position)<1.5f))o.Removed=true;
            foreach(int sign in new[]{-1,1}){var ball=BossObject(u,"fireball","郁结火球",d.Position,1,.2f,8);ball.Velocity=Vector3.right*sign*2;ball.TargetId=d.EntityId;ball.NextAt=Clock+1;}
        }
        void BossPainPhase(DeployedUnitState u)
        {
            var s=BossRoster(u);if(s.Phase>=2)return;s.Phase=2;s.Transitioning=true;u.Health=u.MaxHealth;BossSetAnimation(u,"Revive_1",false);for(int i=0;i<4;i++)BossObject(u,"hound","猎犬proto",u.Motion.Position+Vector3.right*i*.5f+Vector3.forward, u.MaxHealth*.12f,1);
            BossEmit(u,"PhaseStart","houndWave",u.Motion.Position,animation:s.Animation);
        }
        void BossStartPainWave(DeployedUnitState u)
        {
            var s=BossRoster(u);if(s.Phase!=2||!s.Consumed.Add("painWave"))return;s.Phase=3;s.Transitioning=false;u.Health=u.MaxHealth;BossSetAnimation(u,"Revive_2",true);BossEmit(u,"PhaseStart","echoes",u.Motion.Position,animation:s.Animation);
            // Original stages own these waves. Here the combat extension owns a small
            // staggered wave, unlocked by actual hound deaths, rather than a time-only fixture.
            for(int i=0;i<3;i++){int ordinal=i;BossAfter(u,i,()=>{var pain=BossObject(u,"pain","痛楚化身",u.Motion.Position+Vector3.right*(ordinal+1),u.MaxHealth*.4f,2);s.Counts["painSpawned"]=Count(s,"painSpawned")+1;pain.InvulnerableUntil=Clock+3;pain.Animation="Start";pain.AnimationAt=Clock;pain.AnimationUntil=Clock+1;pain.AnimationLoop=false;pain.NextAt=Clock+3;pain.BuffUntil=double.PositiveInfinity;pain.PowerMultiplier=1+.3f*Math.Min(2,Count(s,"painDead"));s.Timers["painRoar/"+pain.Id]=Clock+10;});}
        }
        void BossPainBlink(DeployedUnitState owner,BossRosterObject o)
        {
            o.Animation="Skill_2_Begin";o.AnimationAt=Clock;o.AnimationUntil=Clock+5.1999998;o.AnimationLoop=false;o.InvulnerableUntil=o.AnimationUntil+.5666667;o.NextAt=o.InvulnerableUntil;long activation=++bossActivationSequence;
            BossEmit(owner,"SkillStart","painBlink",o.Position,native:"Blink",animation:o.Animation,targetId:o.Id,activation:activation,end:o.AnimationUntil,origin:o.Position);
            BossAfter(owner,5.1999998,()=>{if(!o.Alive)return;Vector3 from=o.Position;var target=BossLiving().OrderByDescending(d=>d.Settings.BlockCount).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();if(target!=null)o.Position=target.Position+Vector3.right;o.Animation="Skill_2_End";o.AnimationAt=Clock;o.AnimationUntil=Clock+.5666667;BossEmit(owner,"SkillEnd","painBlink",o.Position,native:"Blink",animation:o.Animation,targetId:o.Id,activation:activation,origin:from);var destination=o.Position;double flight=GroundDistance(from,destination)/10;BossEmit(owner,"ProjectileSpawn","painBlink",destination,native:"Blink",targetId:o.Id,activation:activation,end:Clock+flight,origin:from);BossAfter(owner,flight,()=>{foreach(var d in BossLiving().Where(d=>BossSegmentDistance(d.Position,from,destination)<=1.5f).ToArray()){BossHit(owner,d,UnitCombat(owner).UnitAttack*.8f*o.PowerMultiplier,CombatDamageType.Arts,"painBlink",activation);BossSlow(d,10,.3f);}},true);});
        }
        static float BossSegmentDistance(Vector3 point,Vector3 from,Vector3 to){Vector3 delta=to-from;float fraction=delta.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(point-from,delta)/delta.sqrMagnitude):0;return GroundDistance(point,from+delta*fraction);}
    }
}
