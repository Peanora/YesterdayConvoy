using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        bool FormationAttackChannel(FormationUnitArt source,FormationActor actor,out string begin,out string cycle,out string end)
        {
            begin=cycle=end=null;int phase=(actor as FormationFighter)?.NativePhase??0;
            if(source.NativeTask!="B13"||phase<=0)return false;
            string stage=phase>1?"2":"1";
            begin="B_Attack_Begin_"+stage;cycle="B_Attack_Loop_"+stage;end="B_Attack_End_"+stage;
            return source.HasAction(begin)&&source.HasAction(cycle)&&source.HasAction(end);
        }
        bool FormationChannel( FormationUnitArt source,FormationActor actor,out string begin,out string cycle,out string end)
        {
            begin=cycle=end=null;int phase=(actor as FormationFighter)?.NativePhase??0;
            if(actor.ArtId=="M31")
            {
                begin="extra_skill_front_start";cycle="extra_skill_front_loop";end="extra_skill_front_end";
                return source.HasAction(begin)&&source.HasAction(cycle)&&source.HasAction(end);
            }
            switch(source.NativeTask)
            {
                case "B12":var p=phase>0?"B":"A";begin=p+"_Skill_Begin";cycle=p+"_Skill_Loop";end=p+"_Skill_End";break;
                case "B13":if(phase==0)return false;var stage=phase>1?"2":"1";begin="B_Skill_Begin_"+stage;cycle="B_Skill_Loop_"+stage;end="B_Skill_End_"+stage;break;
                case "B21":var side=actor.Facing.x<0?"B":"A";begin="Skill_Begin_"+side;cycle="Skill_Loop_"+side;end="Skill_End_"+side;break;
                case "B23":var form=phase>0?"B":"A";begin=form+"_Skill_2_Begin";cycle=form+"_Skill_2_Loop";end=form+"_Skill_2_End";break;
                case "B25":begin="A_Skill_1_Begin";cycle="A_Skill_1_Loop";end="A_Skill_1_End";break;
                case "B26":begin="Skill_2_Begin";cycle="Skill_2_Loop";end="Skill_2_End";break;
                case "WIS_APRIL_G9":
                    if(Campaign?.WisadelSkill!=3)return false;
                    begin="Skill_3_Begin";cycle="Skill_3_Loop";end="Skill_3_End";break;
                default:return false;
            }
            return source.HasAction(begin)&&source.HasAction(cycle)&&source.HasAction(end);
        }
        float FormationSourceDuration(FormationUnitArt source,FormationActor actor,bool skill,string key)
        {
            if(skill&&source.NativeTask=="STEAM")return Model.SteamSourceDuration;
            if(!skill&&FormationAttackChannel(source,actor,out var attackBegin,out var attackCycle,out var attackEnd))
                return source.Duration(attackBegin)+source.Duration(attackCycle)+source.Duration(attackEnd);
            if(skill&&FormationChannel(source,actor,out var begin,out var cycle,out var end))
            {
                if(source.NativeTask=="WIS_APRIL_G9")
                    return source.Duration(begin)+((actor as FormationFighter)?.Star>=3?11:5)*FormationBattleModel.WisadelShellInterval*FormationVisualRate+
                        source.ContactTime(cycle)+FormationBattleModel.WisadelShellMinimumFlight*FormationVisualRate+source.Duration(end);
                return Mathf.Max(source.Duration(begin)+source.Duration(cycle)+source.Duration(end),(FormationCampaignRules.Find(actor.ArtId)?.ActionSeconds??0)*FormationVisualRate);
            }
            return source.Duration(key);
        }
        float FormationSourceContact(FormationUnitArt source,FormationActor actor,bool skill,string key)
        {
            if(skill&&source.NativeTask=="STEAM")return FormationBattleModel.SteamSourceContact;
            if(actor.ArtId=="B21"&&skill)return source.ContactTime(SuiClawMotion);
            string begin,cycle,end;
            bool channel=skill?FormationChannel(source,actor,out begin,out cycle,out end):FormationAttackChannel(source,actor,out begin,out cycle,out end);
            if(!channel)return source.ContactTime(key);
            float offset=0;
            foreach(var part in new[]{begin,cycle,end})
            {
                float contact=source.ContactTime(part);
                if(!float.IsNaN(contact))return offset+contact;
                offset+=source.Duration(part);
            }
            return float.NaN;
        }
        bool PresentFormationChannel(FormationUnitArt source,FormationActor actor,double now,ref string key,ref double age,ref bool loop,bool skill=true)
        {
            string begin,cycle,end;
            bool channel=skill?FormationChannel(source,actor,out begin,out cycle,out end):FormationAttackChannel(source,actor,out begin,out cycle,out end);
            if(!channel)return false;
            double rate=actor.PresentationRate>0?actor.PresentationRate:FormationVisualRate;
            double elapsed=Math.Max(0,now-actor.PresentationStartedAt)*rate;
            double window=Math.Max(actor.PresentationDuration*rate,FormationSourceDuration(source,actor,skill,begin));
            if(source.NativeTask=="B21"&&actor.Id<0)window=Math.Max(window,(Model.PresentationBusyUntil(actor)-actor.PresentationStartedAt)*rate+source.Duration(end));
            double beginDuration=source.Duration(begin),endDuration=source.Duration(end);
            if(elapsed<beginDuration){key=begin;age=elapsed;loop=false;}
            else if(elapsed>=window-endDuration){key=end;age=Math.Max(0,elapsed-(window-endDuration));loop=false;}
            else
            {
                key=cycle;age=elapsed-beginDuration;loop=true;
                // Restart from the same source windup for each actual shell.
                if(source.NativeTask=="WIS_APRIL_G9")
                {
                    int intervals=(actor as FormationFighter)?.Star>=3?11:5;
                    double cadence=FormationBattleModel.WisadelShellInterval*rate;
                    age-=Math.Min(intervals,Math.Floor(age/cadence))*cadence;loop=false;
                }
            }
            return true;
        }
    }
    public sealed partial class FormationBattleModel
    {
        internal double PresentationBusyUntil(FormationActor actor)=>State(actor).BusyUntil;
    }
}
