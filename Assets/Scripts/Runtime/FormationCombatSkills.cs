using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal const float WisadelShellInterval = 1.1f;
        internal const float WisadelShellMinimumFlight = .5f;
        const int TalulahRainDuration = 8;

        internal double TalulahRainEnd(FormationActor fighter)
        {
            if(fighter.ArtId!="TALULAH_TRIAL"||Campaign?.TalulahCost!=5)return double.NegativeInfinity;
            return zones.FindLast(zone=>zone.Source==fighter&&zone.Kind=="burn")?.Until??double.NegativeInfinity;
        }

        bool CastCampaignSkill(FormationFighter u,FormationDefender target,bool free=false)
        {
            var s=State(u);string k=Key(u);var main=Carry;
            if(s.Spec==null||(!free&&target==null))return false;
            if(k=="B21"&&s.BusyUntil+1>Clock&&s.Casts>0)return false;
            if(k=="WIS"&&Campaign.WisadelSkill==2&&Buff(u,"wisadel")>0)return false;
            if(!free)u.SkillCharge=Mathf.Max(0,u.SkillCharge-u.SkillRequired);
            if(s.Echo>0){GainSP(u,s.Echo,true);s.Echo=0;}
            float action=s.Spec.ActionSeconds*(Buff(u,"haste")>0?Buff(u,"haste"):1)*(k=="B04"&&s.Stage>0?.8f:1);
            action=Mathf.Max(.35f,action);
            if(k=="TAL")action=Campaign.TalulahCost==5?2:Campaign.TalulahCost==4?1.5f:1.2f;
            if(k=="WIS"&&Campaign.WisadelSkill==2)action=.6f;
            var presentation=BeginAction(u,action,true);s.Casts++;SkillCasts.Add(new CastEvent{At=Clock,Instance=u.Instance,UnitId=k});s.Stacks["hasteused"]=Buff(u,"haste");s.Buffs.Remove("haste");
            if(k=="TAL"&&Campaign.TalulahCost==5)
                foreach(var pending in presentationActions)
                    if(pending.ActorId==u.Id&&!pending.Skill&&pending.ContactAt>Clock+1e-8)pending.Cancelled=true;
            float skillBonus=1+Buff(u,"skill_bonus")+Buff(u,"king_damage")+Buff(u,"revival_skill")+Buff(u,"ember");
            if(Campaign.HasAugment("CG1")&&IsResonanceMember(u))skillBonus+=.1f;
            if(Campaign.TalulahEnhanced&&k=="TAL"&&Campaign.TalulahCost==3)skillBonus+=.25f;
            s.Stacks["cast_multiplier"]=skillBonus;s.Buffs.Remove("skill_bonus");s.Buffs.Remove("revival_skill");s.Buffs.Remove("ember");
            if(HasItem(u,"CF")&&!AttackRecovery(u))AddStack(u,"manual",1,5);
            if(HasItem(u,"CC")&&!AttackRecovery(u))AddStack(u,"reactor",1,10);
            if(Tagged(u,"伊比利亚")&&Level("伊比利亚")>=2&&Stack(u,"evidence")>=12){s.Stacks["evidence"]-=12;s.Stacks["cast_multiplier"]+=.35f;}
            var firstSuiStrike = k=="B21" ? PrepareSuiClawStrike(u,presentation,0,target,presentation.ContactAt) : null;
            QueueSkillAction(u,presentation,()=>
            {
            switch(k)
            {
                case "M03": SkillSingle(u,target,target.Health<target.MaxHealth*.5f?3.4f:2.6f,0);break;
                case "M11": SkillSingle(u,target,Buff(target,"freeze")>0?3.8f:2.5f,0);Cold(target);break;
                case "M17":
                    HealLow(u,2,1.6f);foreach(var ally in Units)State(ally).Buffs.Remove("herald_as");if(main!=null)GiveBuff(main,"herald_as",.12f,999);PromptSupport(u);break;
                case "M21":
                    float hit=SkillSingle(u,target,2.8f,0);CombatHeal(u,u,hit*.25f,true,true);break;
                case "M35":
                    SkillSingle(u,target,2.4f,0);Element(u,target,18,false);BlinkBehind(u,target,.8f);break;
                case "B01":
                    SkillArea(u,target,2.4f,0,1.8f,4);foreach(var d in Nearby(target.Position,AreaRadius(u,1.8f),4,target))Stun(d,1);
                    if(s.Casts>=2)ScheduleOwned(u,Clock+.3,()=>{if(u.Alive){SkillArea(u,target,1.2f,0,1.8f,4);}});break;
                case "B22":
                    float coefficient=.55f+Mathf.Min(10,s.Casts-1)*.02f;
                    SkillSequence(u,target,4,coefficient,0,action);
                    AddStack(u,"ramp",1,10);
                    if(s.Casts>=5)
                    {
                        u.ActionUntil=Math.Max(u.ActionUntil,presentation.At+action+.35);
                        ScheduleOwned(u,presentation.At+action,()=>
                        {
                            if(!u.Alive||SnowBossFreezeRemaining(u)>0)return;var t=FighterTarget(u);if(t==null)return;
                            var follow=BeginAction(u,.35,false);
                            QueueOrdinaryAction(u,t,follow,()=>{s.Autos++;NativeSegment(u,new[]{t},u.Attack*.6f,0,false,0,false);CompleteAuto(u,true);});
                        });
                    }
                    break;
                case "M12":
                    var frozen=Nearby(target.Position,AreaRadius(u,2),5,target).Where(d=>Buff(d,"freeze")>0).ToArray();
                    SkillArea(u,target,3.2f,1,2,5);foreach(var d in frozen)CombatDamage(u,d,u.Attack*Power(u),1,true,false);foreach(var d in Nearby(target.Position,AreaRadius(u,2),5,target))Cold(d);break;
                case "M13":
                    bool broken=Buff(target,"freeze")>0;SkillSingle(u,target,broken?6.5f:4,0);
                    if(broken){State(target).Buffs.Remove("freeze");if(Level("雪怪")>=4&&Clock-State(target).LastSnowBreak>=4){State(target).LastSnowBreak=Clock;foreach(var f in Units.Where(f=>f.Enabled&&Tagged(f,"雪怪")))GainSP(f,3);}}
                    break;
                case "M16": SkillSequence(u,target,5,.95f,0,action);break;
                case "M25":
                    foreach(var f in LowAllies(3)){CombatHeal(u,f,u.Attack*2.5f*Power(u),true,true);if(Buff(f,"slow")>0)State(f).Buffs.Remove("slow");else State(f).Buffs.Remove("silence");GiveBuff(f,"heal_received",.15f,4);}PromptSupport(u);break;
                case "M27":
                    NativeSegment(u,new[]{target},u.Attack*3+u.MaxHealth*.08f,0,true,0,false);Shield(u,u,u.MaxHealth*.25f,8,"native");break;
                case "M29": Shield(u,u,u.MaxHealth*.4f,8,"native");SkillArea(u,target,3,0,1.8f,3);break;
                case "M30": s.Stacks["fireautos"]=3;break;
                case "M38":
                    var highest=Defenders.Where(d=>d.Alive).OrderByDescending(d=>d.Attack).FirstOrDefault();
                    if(highest!=null){if(State(highest).Boss)GiveBuff(highest,"move_slow",.3f,3);else Stun(highest,3);GiveBuff(highest,"vulnerable",.15f,8);SkillSingle(u,highest,.6f,1);}break;
                case "B03": BlinkBehind(u,target,.8f);Shield(u,u,u.MaxHealth*.25f,4,"native");SkillSequence(u,target,3,1.6f,0,action);break;
                case "B05": SkillArea(u,target,3.3f,0,2,5);break;
                case "M18":
                    GiveBuff(u,"taunt",1,5);Shield(u,u,u.MaxHealth*.5f,8,"native");if(main!=null)GiveBuff(main,"shieldguard",.15f,8);PromptSupport(u);break;
                case "M24": SkillSequence(u,target,4,1.6f,1,action,1.6f,3);break;
                case "M42":
                    NativeSegment(u,Nearby(target.Position,AreaRadius(u,2),5,target),u.Attack*5.2f,1,true,0,true);foreach(var d in Nearby(target.Position,AreaRadius(u,2),5,target))Stun(d,1);break;
                case "M31": SkillSequence(u,target,6,1,1,action,2.3f,5,12);break;
                case "M34": SkillSequence(u,target,4,1.5f,1,action,2.2f,4,15,false);break;
                case "TAL":
                    int cost=Campaign.TalulahCost;
                    if(cost==3){SkillArea(u,target,5,1,2,5);AddZone(u,target.Position,AreaRadius(u,2),.2f,6,5,"burn");}
                    else
                    {
                        int n=cost==5?6:Campaign.TalulahEnhanced?4:3;
                        SkillSequence(u,target,n,cost==5?2.6f:2.8f,1,action,cost==5?100:2.5f,cost==5?8:6);
                        AddZone(u,target.Position,cost==5?100:AreaRadius(u,2.5f),cost==5?.5f:.35f,cost==5?TalulahRainDuration:6,cost==5?8:6,"burn");
                        if(cost==5&&u.Star>=3)for(int sec=2;sec<=TalulahRainDuration;sec+=2)ScheduleOwned(u,Clock+sec,()=>{if(u.Alive)foreach(var d in Defenders.Where(d=>d.Alive).Take(8).ToArray())CombatDamage(u,d,u.Attack*.5f*Power(u),2,true,false);});
                        if(cost==4&&Campaign.TalulahEnhanced)Shield(u,u,u.MaxHealth*.2f,8,"talulah");
                    }break;
                case "B09":
                    if(main!=null){s.RecordOwner=main;s.RecordUntil=Clock+8;s.RecordDamage=0;GiveBuff(main,"skill_bonus",.15f,999);}PromptSupport(u);break;
                case "B14":SkillArea(u,target,3.5f,0,2,3);break;
                case "B23":
                    SkillSequence(u,target,6,.9f+Mathf.Min(8,s.Casts-1)*.08f,0,action);
                    if(s.Casts%3==0){var extra=Defenders.FirstOrDefault(d=>d.Alive&&d!=target);if(extra!=null)SkillSequence(u,extra,6,.9f+Mathf.Min(8,s.Casts-1)*.08f,0,action);}break;
                case "M14":
                    SkillArea(u,target,6.5f,1,3,6);foreach(var d in Nearby(target.Position,AreaRadius(u,3),6,target)){Cold(d);if(s.Stage>0)Schedule(Clock+.5,()=>Cold(d));}
                    AddZone(u,target.Position,AreaRadius(u,3),.55f,6,6,"snow");break;
                case "M28":
                    NativeSegment(u,Nearby(target.Position,AreaRadius(u,2),5,target),u.Attack*5+u.MaxHealth*.12f,0,true,0,true);
                    Shield(u,u,u.MaxHealth*.3f,8,"native");break;
                case "M32":
                    foreach(var f in (main==null?LowAllies(2):new[]{main}.Concat(LowAllies(3).Where(f=>f!=main).Take(2))))Shield(u,f,f.MaxHealth*.35f,8,"mandragora");
                    if(main!=null){GiveBuff(main,"attack",.35f,8);var enemy=FighterTarget(main);if(enemy!=null){GiveBuff(enemy,"mandra_armor",-20,8);GiveBuff(enemy,"mandra_mr",-10,8);}}PromptSupport(u);break;
                case "M36":
                    SkillSequence(u,target,5,.5f,1,action,2.5f,5,20,false);
                    foreach(var d in Nearby(target.Position,AreaRadius(u,2.5f),5,target))GiveBuff(d,"skill_vulnerable",.3f,8);break;
                case "B04":SkillSpread(u,target,8,1.1f,action);break;
                case "B07":
                    var marked=main==null?target:FighterTarget(main)??target;
                    foreach(var d in Nearby(marked.Position,100,2,marked))GiveBuff(d,"arts_vulnerable",.3f,8);
                    if(main!=null){GiveBuff(main,"eblana_fire",30,8);Shield(u,main,main.MaxHealth*.2f,8,"eblana");}PromptSupport(u);break;
                case "B15":
                    AddZone(u,target.Position,1.55f,0,10,100,"kingdom");if(main!=null)GiveBuff(main,"kingdom_damage",.25f,10);PromptSupport(u);break;
                case "B24":
                    float trench=1.5f*(Buff(u,"next_trench")>0?1.5f:1);s.Buffs.Remove("next_trench");
                    AddZone(u,target.Position,100,trench,6,100,"trench");break;
                case "B25":
                    SkillSequence(u,target,8,1.05f,0,action);
                    if(s.Casts%3==0)ScheduleOwned(u,Clock+action-.05,()=>{if(u.Alive)SkillArea(u,target,3,1,1.8f,3);});break;
                case "B26":
                    SkillSingle(u,main==null?target:FighterTarget(main)??target,2.5f,1);
                    if(main!=null){double duration=10+Buff(u,"yi_duration");GiveBuff(main,"yi_power",.3f,duration);GiveBuff(main,"yi_sp",1.2f,duration);}
                    var sui=Named("B21");if(sui!=null&&Level("岁影")>=2){AddStack(sui,"chess",1,4);GainSP(u,3);}PromptSupport(u);break;
                case "M20":
                    GiveBuff(u,"taunt",1,6);foreach(var f in Units.Where(f=>f.Enabled&&f.Alive))Shield(u,f,u.MaxHealth*.15f*(s.Stage>0?1.5f:1),8,"patriot");
                    if(main!=null){GiveBuff(main,"patriot_dr",.3f,8);GiveBuff(main,"patriot_attack",.2f,8);}break;
                case "WIS":
                    if(Campaign.WisadelSkill==1)SkillSequence(u,target,u.Star>=3?6:4,2.4f,0,1);
                    else if(Campaign.WisadelSkill==2){GiveBuff(u,"wisadel",1,8);presentation.EndAt=Math.Max(presentation.EndAt,Clock+8);}
                    else
                    {
                        int shells=u.Star>=3?12:6;s.BusyUntil=presentation.At+shells*WisadelShellInterval;u.ActionUntil=Math.Max(u.ActionUntil,s.BusyUntil);
                        presentation.EndAt=Math.Max(presentation.EndAt,s.BusyUntil);
                        for(int i=0;i<shells;i++)
                        {
                            int shell=i;double launch=presentation.LaunchedAt+i*(double)WisadelShellInterval;
                            ScheduleOwned(u,launch,()=>
                            {
                                if(!u.Alive)return;var t=FighterTarget(u);if(t==null)return;
                                QueueSkillProjectile(u,presentation,t,shell,"Skill_3_Loop",launch,()=>SkillArea(u,t,4.5f,0,2.2f,6),false);
                            });
                        }
                    }break;
                case "B08":SkillArea(u,target,12,1,3,7);AddZone(u,target.Position,AreaRadius(u,3),.8f,8,7,"burn");break;
                case "B10":
                    foreach(var f in (main==null?LowAllies(3):new[]{main}.Concat(LowAllies(3).Where(f=>f!=main).Take(2))))CombatHeal(u,f,u.Attack*5*Power(u),true,true);
                    if(main!=null){GiveBuff(main,"can_attack",.45f,10);GiveBuff(main,"can_as",.45f,10);}
                    if(s.Casts==1)BattleGold+=u.Star>=3?10:u.Star==2?4:2;break;
                case "B11":
                    double dur=u.Star>=2?12:10;
                    foreach(var f in u.Star>=3?Units.Where(f=>f.Enabled&&f.Alive):main==null?Array.Empty<FormationFighter>():new[]{main}){GiveBuff(f,"king_sp",1,dur);GiveBuff(f,"king_damage",.35f,dur);}
                    foreach(var d in Defenders.Where(d=>d.Alive))GiveBuff(d,"king_mr",-20,dur);PromptSupport(u);break;
                case "B12":
                    if(u.Star>=3&&s.Casts==1){NativeSegment(u,Defenders.Where(d=>d.Alive).ToArray(),u.Attack*60,2,true,0,true);GainSP(u,18);}
                    else SkillArea(u,target,15,2,100,7);
                    if(s.Casts%3==0)NativeSegment(u,Defenders.Where(d=>d.Alive).ToArray(),u.Attack*9,2,true,0,true);break;
                case "B13":
                    foreach(var f in u.Star>=3?Units.Where(f=>f.Enabled&&f.Alive):main==null?Array.Empty<FormationFighter>():new[]{main}){GiveBuff(f,"saint_damage",u.Attack*(u.Star>=3?2:u.Star==2?1:.6f),10);}break;
                case "B21":
                    StartSuiClawSkill(u,target,firstSuiStrike);break;
            }
            if(Tagged(u,"雪怪")&&Level("雪怪")>=4&&target!=null)Cold(target);
            });
            ScheduleSkillCompletion(u,presentation,presentation.At+Math.Max(k=="B22"?action-.001f:action,s.BusyUntil-presentation.At),()=>SkillFinished(u));
            return true;
        }
        float SkillSingle(FormationFighter u,FormationDefender target,float mult,int type)
        {if(target==null)return 0;return NativeSegment(u,new[]{target},u.Attack*mult,type,true,0,false);}
        float AreaRadius(FormationActor u,float radius)
        {int l=Tagged(u,"爆破")?Level("爆破"):0;return radius*(1+(l>=6?.3f:l>=4?.2f:l>=2?.1f:0)+(Campaign.HasAugment("CG4")?.15f:0));}
        void SkillArea(FormationFighter u,FormationDefender target,float mult,int type,float radius,int cap)
        {if(target==null)return;NativeSegment(u,Nearby(target.Position,AreaRadius(u,radius),cap,target),u.Attack*mult,type,true,0,true);}
        void SkillSequence(FormationFighter u,FormationDefender target,int segments,float mult,int type,float action,float radius=0,int cap=1,float element=0,bool fire=true)
        {
            var center=target.Position;
            for(int i=0;i<segments;i++)
            {
                int segment=i;
                double at=Clock+(segments==1?0:i*(double)Math.Max(.01f,action-.05f)/segments);
                ScheduleOwned(u,at,()=>
                {
                    if(!u.Alive)return;var t=target.Alive?target:FighterTarget(u);if(t==null)return;
                    var victims=Nearby(radius>0?center:t.Position,radius>0?AreaRadius(u,radius):0,cap,t);
                    var parent=activePresentationContext?.SkillCompletion?.Presentation;
                    if(Key(u)=="WIS"&&Campaign.WisadelSkill==1&&parent!=null)
                        QueueSkillProjectile(u,parent,t,segment,"Skill_1",at,()=>NativeSegment(u,victims,u.Attack*mult,type,true,element,radius>0,fire),true);
                    else NativeSegment(u,victims,u.Attack*mult,type,true,element,radius>0,fire);
                });
            }
        }
        void SkillSpread(FormationFighter u,FormationDefender target,int segments,float mult,float action)
        {
            var candidates=Nearby(target.Position,1.5f,3,target);
            for(int i=0;i<segments;i++)
            {
                int index=i;ScheduleOwned(u,Clock+i*(double)Math.Max(.01f,action-.05f)/segments,()=>
                {
                    var living=candidates.Where(d=>d.Alive).ToArray();
                    var victim=living.Length>0?living[index%living.Length]:FighterTarget(u);
                    if(victim!=null)NativeSegment(u,new[]{victim},u.Attack*mult,0,true,0,false);
                });
            }
        }
        void SkillFinished(FormationFighter u)
        {
            if(!u.Alive)return;var s=State(u);
            if(HasItem(u,"BF"))s.Stacks["coil"]=0;
            if(HasItem(u,"FF")&&s.Casts%4==0)
            {
                foreach(var hit in s.SkillDamage){var target=Defenders.FirstOrDefault(d=>d.Id==hit.Key&&d.Alive);if(target!=null)CombatDamage(u,target,hit.Value*.4f,2,true,false);}
                foreach(var heal in s.SkillHealing){var ally=Units.FirstOrDefault(f=>f.Id==heal.Key&&f.Alive);if(ally!=null)CombatHeal(u,ally,heal.Value*.4f,true,false);}
                foreach(var shield in s.SkillShields){var ally=Units.FirstOrDefault(f=>f.Id==shield.Key&&f.Alive);if(ally!=null)Shield(u,ally,shield.Value*.4f,8,"mirror");}
            }
            var partner=ResonancePartner();if(ResonanceLeader()==u&&partner?.Alive==true)
            {
                if(Key(partner)=="M25"&&Clock-State(partner).LastHealSP>=1){State(partner).LastHealSP=Clock;CombatHeal(partner,u,u.MaxHealth*.04f,true,false);}
                if(Key(partner)=="B09"){var t=FighterTarget(u);if(t!=null)CombatDamage(partner,t,Stack(u,"last_skill_raw")*.3f,2,true,false);}
                if(Campaign.HasAugment("CP3")&&Stack(u,"resonant_cast")>0){GainSP(partner,2);s.Stacks["resonant_cast"]=0;}
            }
        }
        FormationFighter[] LowAllies(int n)=>Units.Where(f=>f.Enabled&&f.Alive).OrderBy(f=>f.Health/f.MaxHealth).ThenBy(f=>f.Id).Take(n).ToArray();
        void HealLow(FormationFighter u,int n,float mult){foreach(var f in LowAllies(n))CombatHeal(u,f,u.Attack*mult*Power(u),true,true);}
        void BlinkBehind(FormationFighter u,FormationActor target,float distance)
        {
            if(SnowBossFreezeRemaining(u)>0)return;
            Vector2 desired=target.Position+new Vector2(distance,0);desired.x=Mathf.Clamp(desired.x,BattleAreaStart,Width-1);desired.y=Mathf.Clamp(desired.y,0,Height-1);
            if(Terrain==null||Terrain.CanTravel(u.Position,desired))u.Position=desired;
        }
    }
}
