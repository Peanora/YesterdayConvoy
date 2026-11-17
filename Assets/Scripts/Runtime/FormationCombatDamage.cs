using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        FormationDefender[] Nearby(Vector2 point,float radius,int cap,FormationDefender preferred=null)
        {
            if(radius<=0)return preferred!=null&&preferred.Alive?new[]{preferred}:Array.Empty<FormationDefender>();
            return Defenders.Where(d=>d.Alive&&(d.Position-point).sqrMagnitude<=radius*radius)
                .OrderBy(d=>d==preferred?0:1).ThenBy(d=>(d.Position-point).sqrMagnitude).ThenBy(d=>d.Id).Take(cap).ToArray();
        }
        float NativeSegment(FormationFighter source,IEnumerable<FormationDefender> targets,float raw,int type,bool skill,float element,bool area,bool fire=true)
        {
            if(!source.Alive)return 0;var s=State(source);var victims=targets.Where(t=>t!=null&&t.Alive).ToArray();if(victims.Length==0)return 0;
            s.ActionHits++;AddStack(source,"native_segments",1);float total=0;
            float mult=skill?Power(source)*Mathf.Max(1,Stack(source,"cast_multiplier")):1;
            int blast=Tagged(source,"爆破")?Level("爆破"):0;if(area&&skill)mult*=1+(blast>=6?.5f:blast>=4?.3f:blast>=2?.15f:0);
            if(!skill&&victims.Length==1&&Campaign.HasAugment("CS5"))mult*=1.12f;
            for(int i=0;i<victims.Length;i++)
            {
                var t=victims[i];
                if(blast>=6&&skill&&s.ActionHits==1){GiveBuff(t,"blast_armor",-15,6);GiveBuff(t,"blast_mr",-15,6);}
                float actual=CombatDamage(source,t,raw*(Key(source)=="M42"&&skill&&t.Shield>0?1.25f:1)*mult*(skill&&area&&blast>=4&&i>0?1.15f:1),type,skill,true);total+=actual;
                if(skill){s.SkillTargets.Add(t.Id);AddStack(source,"last_skill_raw",actual);s.SkillDamage[t.Id]=(s.SkillDamage.TryGetValue(t.Id,out var prev)?prev:0)+actual;}
                if(element>0)Element(source,t,element,fire);
                if(Tagged(source,"海嗣")&&Level("海嗣")>=2&&seaCells.Any(c=>(source.Position-c).sqrMagnitude<=.6f*.6f))Element(source,t,Level("海嗣")>=5?15:Level("海嗣")>=4?10:6,false);
                if(Tagged(source,"深池")&&Level("深池")>=2)DeepBurn(source,t);
                if(Buff(source,"eblana_fire")>0&&s.ActionHits==1&&i==0)Element(source,t,30,true);
                if(Buff(source,"saint_damage")>0&&t.Alive)CombatDamage(source,t,Buff(source,"saint_damage"),type,skill,false);
                if(skill&&HasItem(source,"AF")&&s.MeasuredTargets.Add(t.Id))AddStack(source,"measure",1,8);
            }
            if(Tagged(source,"伊比利亚")&&Level("伊比利亚")>=2)AddStack(source,"evidence",1,24);
            if(HasItem(source,"AA")&&(int)Stack(source,"native_segments")%3==0)AddStack(source,"saw",1,10);
            if(Key(source)=="M24")CombatHeal(source,source,total*.2f,skill,false);
            if(Key(source)=="B25")CombatHeal(source,source,total*.15f,skill,false);
            if(skill)
            {
                if(HasItem(source,"AC")&&s.SkillSPReturned<4){float n=Mathf.Min(.5f,4-s.SkillSPReturned);GainSP(source,n);s.SkillSPReturned+=n;}
                PromptDamage(source);
                var partner=ResonancePartner();
                if(ResonanceLeader()==source&&partner?.Alive==true&&Key(partner)=="B13"&&s.SkillBeats<6){s.SkillBeats++;Beat(source,1,true);}
            }
            int combo=Tagged(source,"连击")?Level("连击"):0;
            if(combo>=2)
            {
                s.ComboProgress++;int required=combo>=8?3:combo>=6?4:combo>=4?5:6;
                if(s.ComboProgress>=required&&s.ComboPerAction<4)
                {
                    s.ComboProgress-=required;s.ComboPerAction++;var t=FighterTarget(source);
                    if(t!=null)CombatDamage(source,t,source.Attack*(combo>=8?1.4f:combo>=6?1:combo>=4?.8f:.6f),type,skill,false);
                    Beat(source,.5f,false);if(combo>=6)GainSP(source,combo>=8?2:1);
                    if(HasItem(source,"AC")&&Stack(source,"tape_auto")<1){GainSP(source,.25f);AddStack(source,"tape_auto",.25f,1);}
                }
            }
            foreach(var recorder in Units.Where(u=>u.Enabled&&Key(u)=="B09"))
            {var r=State(recorder);if(r.RecordOwner==source&&r.RecordUntil>Clock)r.RecordDamage=Mathf.Min(recorder.Attack*24,r.RecordDamage+total*.25f);}
            return total;
        }
        float CombatDamage(FormationActor source,FormationActor target,float raw,int type,bool skill,bool native,bool transferred=false)
        {
            if(target==null||!target.Alive||raw<=0)return 0;var t=State(target);var s=State(source);
            float armor=target.Armor+Buff(target,"mandra_armor")+Buff(target,"blast_armor")+Buff(target,"kingdom_armor");
            if(Buff(target,"armorbreak")>0)armor-=Mathf.Min(5,Stack(target,"armorbreak"))*2;
            if(source is FormationFighter sf&&HasItem(sf,"AA")&&armor>0)armor*=1-Stack(source,"saw")*.02f;
            float mr=target.MagicResistance+Buff(target,"mandra_mr")+Buff(target,"blast_mr")+Buff(target,"kingdom_mr")+Buff(target,"king_mr")+Buff(target,"burn_mr");
            float baseDamage=type==2?raw:type==1?raw*(1-Mathf.Clamp(mr,-50,80)/100):Mathf.Max(1,raw-Mathf.Max(-30,armor));
            bool inKingdom=zones.Any(z=>z.Kind=="kingdom"&&z.Until>Clock&&z.Cells!=null&&z.Cells.Contains(Vector2Int.RoundToInt(source.Position)));
            float amp=Buff(source,"theatre_damage")+(inKingdom?Buff(source,"kingdom_damage"):0)+Buff(target,"vulnerable")+(type==1?Buff(target,"arts_vulnerable"):0)+(skill&&native?Buff(target,"skill_vulnerable"):0);
            if(source is FormationFighter)
            {
                int l=Level("接棒");amp+=s.HandoffStacks*(l>=6?.18f:l>=4?.12f:.08f);
                if(Tagged(source,"雪怪")&&Level("雪怪")>=2&&Buff(target,"cold")>0)amp+=.15f;
                if(Tagged(source,"雪怪")&&Level("雪怪")>=4&&Buff(target,"freeze")>0)amp+=.3f;
                if(Key(source)=="B24"&&inKingdom&&Level("国度")>=2)amp+=.2f;
                if(s.RidingCarrier!=0&&Level("便车")>=6)amp+=.25f;
            }
            float reduction=Buff(target,"patriot_dr");
            if(Buff(target,"shieldguard")>0&&Units.Any(u=>Key(u)=="M18"&&u.Alive&&u.Shield>0))reduction+=.15f;
            if(Tagged(target,"海嗣")&&Level("海嗣")>=2&&seaCells.Any(c=>(target.Position-c).sqrMagnitude<=.36f))reduction+=Level("海嗣")>=5?.2f:Level("海嗣")>=4?.15f:.1f;
            if(t.RidingCarrier!=0&&source is FormationDefender&&skill&&Campaign.HasAugment("CP4"))reduction+=.25f;
            float amount=Mathf.Max(1,baseDamage*(1+amp)*(1-Mathf.Min(.85f,reduction)));
            if(target is FormationFighter actor&&!transferred)
            {
                int theatre=Level("剧场");var lead=PairMember(Campaign.TheaterLeadInstance)??Units.FirstOrDefault(u=>u.Enabled&&Tagged(u,"剧场"));
                if(theatre>=2&&lead==actor&&actor.Health<actor.MaxHealth*.4f&&Buff(actor,"theatre_lock")==0)
                {
                    var backstage=Units.Where(u=>u!=actor&&u.Enabled&&u.Alive&&Tagged(u,"剧场")&&Stack(u,"understudy")==0).OrderBy(u=>(u.Position-actor.Position).sqrMagnitude).FirstOrDefault();
                    if(backstage!=null){AddStack(backstage,"understudy",1,1);GiveBuff(actor,"theatre_guard",backstage.Id,4);GiveBuff(actor,"theatre_lock",1,8);Schedule(Clock+4,()=>GiveBuff(actor,"theatre_damage",theatre>=6?.4f:theatre>=4?.25f:.15f,8));}
                }
                if(Buff(actor,"theatre_guard")!=0)
                {
                    var guard=Units.FirstOrDefault(u=>u.Id==(int)Buff(actor,"theatre_guard")&&u.Alive);float share=theatre>=6?.6f:theatre>=4?.4f:.25f;
                    if(guard!=null){CombatDamage(source,guard,amount*share,2,skill,false,true);amount*=1-share;}
                }
            }
            float originalAmount=amount;
            foreach(var shield in t.Shields.OrderBy(sh=>sh.Until).ToArray())
            {
                float absorbed=Mathf.Min(amount,shield.Amount);shield.Amount-=absorbed;amount-=absorbed;
                if(target is FormationFighter f&&absorbed>0)
                {
                    t.ShieldAbsorbed+=absorbed;t.ReflectAbsorbed+=absorbed;
                    if(HasItem(f,"CD")&&t.ShieldAbsorbed>=f.MaxHealth*.08f&&Clock-t.LastShieldSP>=4){t.ShieldAbsorbed-=f.MaxHealth*.08f;t.LastShieldSP=Clock;GainSP(f,2);}
                    if(HasItem(f,"BD")&&t.ReflectAbsorbed>=f.MaxHealth*.15f&&Clock-t.LastReflect>=2){t.ReflectAbsorbed-=f.MaxHealth*.15f;t.LastReflect=Clock;CombatDamage(f,source,f.Attack*.8f,0,false,false,true);}
                    if(shield.Renewal&&Campaign.HasAugment("CG5")&&Clock-t.LastResShield>=4&&absorbed>0)
                    {float n=AddStack(f,"res_reflect",absorbed);if(n>=f.MaxHealth*.15f){t.Stacks["res_reflect"]=0;t.LastResShield=Clock;CombatDamage(shield.Source??f,source,(shield.Source??f).Attack*.8f,1,false,false,true);}}
                }
                if(amount<=0)break;
            }
            target.Shield=t.Shields.Sum(sh=>Mathf.Max(0,sh.Amount));float healthLoss=Mathf.Min(target.Health,amount);target.Health=Mathf.Max(0,target.Health-healthLoss);
            source.DamageDone+=healthLoss;target.LastHitAt=Clock;
            if(t.InitialGuard)t.LowestHealth=Mathf.Min(t.LowestHealth,target.Health);
            if(target is FormationFighter fighter)
            {
                RecordTaken(fighter,healthLoss,type,originalAmount);
                if(Key(fighter)=="B04"&&fighter.Health<fighter.MaxHealth*.5f&&t.Stage==0){t.Stage=1;fighter.PhaseStage=1;NativePhasePresentation(fighter);GiveBuff(fighter,"stage_as",.3f,999);GainSP(fighter,6);RelayReturned(fighter);}
                if(HasItem(fighter,"BE")&&!t.EmergencyUsedThisLife&&fighter.Health<fighter.MaxHealth*.3f&&Stack(fighter,"emergency")<2&&fighter.Health>0){t.EmergencyUsedThisLife=true;AddStack(fighter,"emergency",1,2);Shield(fighter,fighter,fighter.MaxHealth*.2f,6,"emergency");GiveBuff(fighter,"move",.4f,4);}
            }
            bool killed=target.Health<=0;
            Hits.Add(new FormationHit{At=Clock,From=source.Position,To=target.Position,FromId=source.Id,ToId=target.Id,Amount=healthLoss,Skill=skill,Killed=killed});
            if(killed)
            {
                if(target is FormationFighter f)KillCampaignFighter(f);
                else
                {
                    target.Alive=false;target.LastDeathAt=Clock;target.TargetId=-1;
                    if(t.InitialGuard){killedGuards++;if(Campaign.HasAugment("CG6")&&killedGuards%3==0)foreach(var friend in Units.Where(u=>u.Enabled&&u.Alive))GainSP(friend,AttackRecovery(friend)?1:3,AttackRecovery(friend));}
                    DeepEmber(target);
                }
            }
            if(t.Boss&&target.Alive)
            {
                int plane=Campaign.CurrentNode.Plane;int stage=plane==3?(target.Health<target.MaxHealth*.35f?2:target.Health<target.MaxHealth*.7f?1:0):(target.Health<target.MaxHealth*.5f?1:0);
                if(stage>t.Stage){t.Stage=stage;target.PhaseStage=stage;target.Interval=t.BaseInterval/(1+(plane==1?.2f:plane==3?.15f:0));}
                if(plane==1&&source.Alive&&!transferred)
                {float allowance=Mathf.Max(0,target.Attack-Stack(target,"bossreflect"));float reflect=Mathf.Min(healthLoss*.15f,allowance);if(reflect>0){AddStack(target,"bossreflect",reflect);CombatDamage(target,source,reflect,2,false,false,true);Schedule(Clock+1,()=>State(target).Stacks["bossreflect"]=Mathf.Max(0,Stack(target,"bossreflect")-reflect));}}
            }
            return healthLoss;
        }
        void RecordTaken(FormationFighter f,float loss,int type,float includingShield)
        {
            var s=State(f);s.DamageStored+=loss;s.FurnaceStored+=loss;
            if(HasItem(f,"AE"))while(s.DamageStored>=f.MaxHealth*.2f){s.DamageStored-=f.MaxHealth*.2f;AddStack(f,"blooddebt",1,6);}
            if(HasItem(f,"EF"))while(s.FurnaceStored>=f.MaxHealth*.2f){s.FurnaceStored-=f.MaxHealth*.2f;AddStack(f,"furnace",1,6);}
            if(HasItem(f,"DF")&&type==1){s.ArtsStored+=includingShield;while(s.ArtsStored>=f.MaxHealth*.08f){s.ArtsStored-=f.MaxHealth*.08f;AddStack(f,"prism",1,6);}}
            if(HasItem(f,"DD")&&includingShield>0&&Clock-s.LastFold>=1){s.LastFold=Clock;AddStack(f,"fold",1,10);}
            if(Key(f)=="M33")
            {
                s.CrawlerStored+=loss;
                if(s.CrawlerStored>=f.MaxHealth*.15f&&Clock-s.LastReflect>=.8){s.LastReflect=Clock;s.CrawlerStored-=f.MaxHealth*.15f;var victims=Nearby(f.Position,2.2f,4);foreach(var d in victims){CombatDamage(f,d,f.Attack*1.8f*Power(f),1,false,false);Element(f,d,20,false);}}
            }
            if(Key(f)=="B05"&&Buff(f,"hanleap")==0&&Defenders.Any(d=>d.Alive&&d.Range<2&&(d.Position-f.Position).sqrMagnitude<=2.25f))
            {Vector2 before=f.Position,back=before+Vector2.left;if(Terrain==null||Terrain.CanTravel(before,back))f.Position=back;GiveBuff(f,"hanleap",1,12);}
        }
        void SelfLoss(FormationFighter f,float amount)
        {if(f.Health<=1)return;float loss=Mathf.Min(amount,f.Health-1);f.Health-=loss;RecordTaken(f,loss*.5f,2,0);}
        void CombatHeal(FormationActor source,FormationFighter target,float raw,bool skill,bool native)
        {
            if(target==null||!target.Alive||raw<=0)return;int renewal=Tagged(target,"复苏")?Level("复苏"):0;
            float receive=(renewal>=6?.4f:renewal>=4?.25f:renewal>=2?.15f:0)+Buff(target,"heal_received")-Buff(target,"heal_down");
            float amount=raw*(1+receive)*(native&&skill&&source is FormationFighter resonanceHealer&&Campaign.HasAugment("CG1")&&IsResonanceMember(resonanceHealer)?1.1f:1),actual=Mathf.Min(amount,target.MaxHealth-target.Health),over=Mathf.Max(0,amount-actual);
            target.Health+=actual;source.HealingDone+=actual;
            if(native&&skill){var owner=State(source);owner.SkillHealing[target.Id]=(owner.SkillHealing.TryGetValue(target.Id,out var prev)?prev:0)+amount;}
            if(actual>0)Hits.Add(new FormationHit{At=Clock,From=source.Position,To=target.Position,FromId=source.Id,ToId=target.Id,Amount=actual,Heal=true,Skill=skill});
            if(renewal>=2&&over>0)
            {
                float shield=Mathf.Min(target.MaxHealth*.15f,over*(renewal>=6?.5f:renewal>=4?.35f:.2f));
                bool gained=Shield(source,target,shield,8,"renewal:"+source.Id,true);
                if(gained&&renewal>=6&&Carry==target)GiveBuff(target,"revival_skill",Mathf.Min(.24f,Buff(target,"revival_skill")+.12f),999);
            }
            if(native&&source is FormationFighter f&&HasItem(f,"CE"))
            {var s=State(f);s.HealStored+=actual;if(s.HealStored>=f.MaxHealth*.1f&&Clock-s.LastHealSP>=3){s.LastHealSP=Clock;s.HealStored-=f.MaxHealth*.1f;GainSP(f,3);}}
        }
        bool Shield(FormationActor source,FormationFighter target,float amount,double seconds,string key,bool renewal=false)
        {
            if(target==null||!target.Alive||amount<=0)return false;var t=State(target);
            if(source.LastSkillAt==Clock&&source is FormationFighter resonanceShielder&&Campaign.HasAugment("CG1")&&IsResonanceMember(resonanceShielder))amount*=1.1f;
            if(source is FormationFighter&&source.LastSkillAt==Clock&&!key.StartsWith("mirror")){var owner=State(source);owner.SkillShields[target.Id]=(owner.SkillShields.TryGetValue(target.Id,out var prev)?prev:0)+amount;}
            if(Tagged(target,"泥岩")){int l=Level("泥岩");amount*=1+(l>=4?.4f:l>=3?.25f:l>=2?.15f:0);}
            amount*=1+Buff(target,"golem_shield");
            var existing=t.Shields.FirstOrDefault(s=>s.Key==key);float old=existing?.Amount??0;
            float renewalOther=t.Shields.Where(s=>s.Renewal&&s!=existing).Sum(s=>s.Amount);
            float other=t.Shields.Where(s=>s!=existing).Sum(s=>s.Amount);
            amount=Mathf.Min(amount,target.MaxHealth*.8f-other);if(renewal)amount=Mathf.Min(amount,target.MaxHealth*.3f-renewalOther);
            if(existing==null){existing=new CombatShield{Key=key,Source=source,Renewal=renewal};t.Shields.Add(existing);}
            existing.Amount=Mathf.Max(old,Mathf.Max(0,amount));existing.Until=Clock+seconds;target.Shield=t.Shields.Sum(s=>s.Amount);
            return existing.Amount>old;
        }
        void Cold(FormationActor d)
        {
            if(d==null||!d.Alive)return;
            if(Buff(d,"cold")>0&&Buff(d,"freeze_lock")==0)
            {if(State(d).Boss)GiveBuff(d,"cold",1,4);else GiveBuff(d,"freeze",1,2);GiveBuff(d,"freeze_lock",1,6);State(d).Buffs.Remove("cold");}
            else GiveBuff(d,"cold",1,4);
        }
        void Stun(FormationActor d,float time){if(d!=null&&d.Alive)GiveBuff(d,"stun",1,time);}
        void Element(FormationActor source,FormationActor target,float amount,bool fire)
        {
            if(!target.Alive)return;string key=fire?"fire":"nerve";string locked=fire?"fire_lock":"nerve_lock";
            if(Buff(target,locked)>0)return;
            float n=AddStack(target,key,amount);if(n<100)return;State(target).Stacks[key]=0;
            GiveBuff(target,locked,1,fire?2:4);
            if(fire){CombatDamage(source,target,source.Attack*1.5f,1,false,false);GiveBuff(target,"burn_mr",-10,6);}
            else{CombatDamage(source,target,source.Attack,2,false,false);if(State(target).Boss)GiveBuff(target,"nerve_slow",.2f,4);else Stun(target,1.5f);}
            if(!fire&&Tagged(source,"海嗣")&&Level("海嗣")>=4&&State(source).Spec?.Factions.Contains("海嗣")==true&&seaBursts.Add(Key(source))&&seaExtra<3){seaExtra++;ExtendSea();}
        }
        void DeepBurn(FormationFighter source,FormationActor target)
        {
            int l=Level("深池");float mult=l>=5?.4f:l>=4?.25f:.15f;string kind="deep:"+source.Id;
            var zone=zones.FirstOrDefault(z=>z.Kind==kind&&z.Center==target.Position);
            if(zone==null)AddZone(source,target.Position,.2f,mult,4,1,kind);
            else{zone.Multiplier=Mathf.Max(zone.Multiplier,mult);zone.Until=Clock+4;}
        }
        void DeepEmber(FormationActor dead)
        {
            if(!State(dead).InitialGuard||!zones.Any(z=>z.Kind.StartsWith("deep:")&&z.Until>Clock&&(z.Center-dead.Position).sqrMagnitude<.25f))return;
            var receiver=Units.Where(u=>u.Enabled&&u.Alive&&Tagged(u,"深池")).OrderBy(u=>(u.Position-dead.Position).sqrMagnitude).FirstOrDefault();
            if(receiver==null)return;int l=Level("深池");GiveBuff(receiver,"ember",l>=5?.5f:l>=4?.35f:.2f,999);
            if(l>=5&&Buff(receiver,"ember_lock")==0){GainSP(receiver,4);GiveBuff(receiver,"ember_lock",1,2);}
        }
        void AddZone(FormationActor source,Vector2 center,float radius,float multiplier,double duration,int cap,string kind)
        {
            var z=new CombatZone{Source=source,Center=center,Radius=radius,Multiplier=multiplier,Until=Clock+duration,Next=Clock+1,Cap=cap,Kind=kind};
            if(kind=="kingdom")z.Cells=Enumerable.Range(DeploymentColumns+1,Width-DeploymentColumns-2).SelectMany(x=>Enumerable.Range(0,Height).Select(y=>new Vector2Int(x,y))).OrderBy(c=>((Vector2)c-center).sqrMagnitude).ThenBy(c=>c.x).ThenBy(c=>c.y).Take(7).ToArray();
            zones.Add(z);
        }
        void TickZones()
        {
            if(Clock>=nextSeaDecay){nextSeaDecay=Clock+1;foreach(var d in Defenders){State(d).Stacks["fire"]=Mathf.Max(0,Stack(d,"fire")-5);State(d).Stacks["nerve"]=Mathf.Max(0,Stack(d,"nerve")-5);}}
            foreach(var z in zones.ToArray())
            {
                if(z.Next>Clock||z.Until+1e-8<Clock)continue;z.Next+=1;
                var victims=Nearby(z.Center,z.Radius,z.Cap);
                if(z.Kind=="kingdom")
                {
                    foreach(var d in Defenders.Where(d=>d.Alive&&z.Cells.Contains(Vector2Int.RoundToInt(d.Position))).ToArray())
                    {
                        GiveBuff(d,"kingdom_armor",-15,1.1);GiveBuff(d,"kingdom_mr",-15,1.1);
                        if(d.Health<d.MaxHealth*.12f&&Stack(d,"execution")==0){AddStack(d,"execution",1,1);CombatDamage(z.Source,d,State(d).Boss||State(d).Elite?z.Source.Attack*3:d.Health+100,2,true,false);}
                    }continue;
                }
                if(z.Kind=="trench")victims=Defenders.Where(d=>d.Alive&&new[]{-.8f,0,.8f}.Any(offset=>Mathf.Abs(d.Position.y-z.Center.y-offset)<=.6f)).ToArray();
                foreach(var d in victims)
                {
                    float mult=z.Multiplier*(z.Kind=="trench"?Math.Min(2,new[]{-.8f,0,.8f}.Count(offset=>Mathf.Abs(d.Position.y-z.Center.y-offset)<=.6f)):1);
                    CombatDamage(z.Source,d,z.Source.Attack*mult*Power(z.Source),1,true,false);
                    if(z.Kind=="trench"&&Level("国度")>=2&&Buff(d,"kingdom_mr")<0)GiveBuff(d,"kingdom_mr",-30,1.1);
                }
            }
            zones.RemoveAll(z=>z.Until+1e-8<Clock);
        }
    }
}
