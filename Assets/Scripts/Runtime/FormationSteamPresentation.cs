using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        [Serializable]sealed class SteamMounts{public SteamMountClip[] clips;}
        [Serializable]sealed class SteamMountClip{public string name;public SteamMountFrame[] frames;}
        [Serializable]sealed class SteamMountFrame{public SteamBone[] bones;}
        [Serializable]sealed class SteamBone{public string name;public float x,y,angle;}
        SteamMounts steamMounts;
        double SteamRebirthAge(FormationActor actor,double now)=>(now-actor.LastDeathAt)*8/Math.Max(.01,Model.ReviveSeconds);
        SteamBone SteamBoneAt(string clip,string bone,double sourceTime)
        {
            if(steamMounts==null)
            {
                var json=Resources.Load<TextAsset>("SteamKnight/mounts");
                if(json==null)throw new InvalidOperationException("Missing retained Steam Knight source mounts.");
                steamMounts=JsonUtility.FromJson<SteamMounts>(json.text);
            }
            var motion=steamMounts.clips.First(item=>item.name==clip);
            return motion.frames[Mathf.Clamp((int)(sourceTime*60),0,motion.frames.Length-1)].bones.First(item=>item.name==bone);
        }
        void SteamPose(FormationActor actor,double now,out string clip,out double age,out bool loop)
        {
            loop=false;age=0;
            if(!actor.Alive)
            {
                bool reviving=actor is FormationFighter{RespawnRemaining:>0}&&Model.Phase==FormationPhase.Fighting;
                clip=reviving?"Die_A":"Die_B";age=reviving?SteamRebirthAge(actor,now):(now-actor.LastDeathAt)*FormationVisualRate;return;
            }
            double elapsed=(now-actor.PresentationStartedAt)*Math.Max(.01,actor.PresentationRate);
            bool casting=actor.LastSkillAt>=actor.LastActionAt-.0001&&now<actor.PresentationStartedAt+actor.PresentationDuration;
            if(casting)
            {
                if(elapsed<5){clip="Skill_1_Begin_A";age=elapsed;}
                else if(elapsed<7.5){clip="Skill_1_Attack_A";age=elapsed-5;}
                else if(elapsed<8.7){clip="Skill_2_Begin_A";age=elapsed-7.5;}
                else if(elapsed<11.7){clip="Skill_2_Begin_A";age=1.1999;}
                else{clip="Skill_2_End_A";age=elapsed-11.7;}
                return;
            }
            if(actor.LastActionAt>=0&&now<actor.PresentationStartedAt+actor.PresentationDuration)
            {clip="Attack_A";age=elapsed;return;}
            double born=actor.LastDeathAt>=0?actor.LastDeathAt+Model.ReviveSeconds:0;
            if(Model.Phase==FormationPhase.Fighting&&now>=born&&now-born<.5&&Model.SteamTier>=2)
            {clip="Boost_A";age=(now-born)*2;return;}
            bool boosted=Model.SteamTier>=2||actor is FormationFighter{Deaths:>0};
            clip=actor.IsMoving?(boosted?"Move_B":"Move_A"):(boosted?"Idle_B":"Idle_A");
            loop=true;age=Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now*FormationVisualRate;
        }
        void SelectFormationSteamMotion(FormationActor actor,double now,ref string key,ref double age,ref bool loop)
            =>SteamPose(actor,now,out key,out age,out loop);
        float SteamFlightHeight(FormationActor actor,double now)
        {
            SteamPose(actor,now,out var motion,out var age,out _);
            if(!motion.StartsWith("Skill_2_",StringComparison.Ordinal))return 0;
            return Math.Max(0,SteamBoneAt(motion,"Chest_Buff",age).y-1.491193f);
        }
        Vector3 SteamMount(FormationActor actor,string bone,double now)
        {
            SteamPose(actor,now,out var motion,out var age,out bool loop);
            var source=FormationUnitArt.For(actor.ArtId);if(loop&&source.Duration(motion)>0)age%=source.Duration(motion);
            var point=SteamBoneAt(motion,bone,age);
            return ActorWorldPoint(actor)+battleCamera.transform.right*(point.x*(actor.Facing.x<0?-1:1))+battleCamera.transform.up*point.y;
        }
        bool SteamFx(string key,double sourceAge,FormationActor actor,Vector2 cell,Vector3 position,bool loop=false,int order=28)
        {
            if(sourceAge<0||!BossRosterFxArt.TryGet("boss/STEAM/"+key,out var clip)||!clip.Sequence.Ready)return false;
            return formationEffects.Show(clip,sourceAge,position,battleCamera,actor.Facing.x<0,loop,Vector3.zero,false,order);
        }
        void PresentFormationSteamEffects(FormationFighter actor)
        {
            if(Model.Phase==FormationPhase.Preparation)return;
            double now=Model.Clock;var foot=ActorWorldPoint(actor);
            if(!actor.Alive)
            {
                if(actor.RespawnRemaining<=0||Model.Phase!=FormationPhase.Fighting)return;
                double age=SteamRebirthAge(actor,now);
                SteamFx("reborn_01",age,actor,actor.Position,foot);
                SteamFx("reborn_02",age,actor,actor.Position,foot);
                SteamFx("reborn_head_01",age,actor,actor.Position,SteamMount(actor,"Head",now));
                SteamFx("reborn_presure_buff_01",age,actor,actor.Position,SteamMount(actor,"Common2",now),true,-3);
                return;
            }
            if(Model.SteamTier>=2)
            {
                var point=SteamMount(actor,"Common2",now);
                SteamFx("presure_buff_01",now*2,actor,actor.Position,point,true,-3);
                SteamFx("shield_01",now*2,actor,actor.Position,point,true,18);
            }
            foreach(var action in Model.PresentationActions.Where(a=>a.ActorId==actor.Id&&!a.Cancelled&&!a.ProjectileOnly&&now>=a.At&&now<=a.EndAt+1))
            {
                double t=(now-action.At)*action.Rate;
                if(!action.Skill)
                {
                    if(now<=action.EndAt)SteamFx("attack_weapon_01",t,actor,actor.Position,SteamMount(actor,"Sword_A",now));
                    SteamFx("attack_start_01",(now-action.LaunchedAt)*action.Rate,actor,actor.Position,foot);
                    continue;
                }
                if(t<=5.7)
                {
                    SteamFx("skill_01_start_01",t,actor,actor.Position,foot);
                    SteamFx("skill_01_start_02",t,actor,actor.Position,SteamMount(actor,"Weapon_Ea",now));
                    if(t>=2.5)SteamFx("presure_buff_01",t-2.5,actor,actor.Position,SteamMount(actor,"Common2",now),true,-3);
                }
                if(t>=5&&t<=7.5)
                {
                    SteamFx("skill_01_start_weapon_01",t-5,actor,actor.Position,SteamMount(actor,"Weapon_Eb",now));
                    SteamFx("skill_01_start_weapon_02",t-5,actor,actor.Position,SteamMount(actor,"Common2",now));
                }
                SteamFx("skill_01_start_03",t-5.7,actor,actor.Position,foot);
                SteamFx("skill_02_end_01",t-5.7,actor,actor.Position,foot+Vector3.right*(actor.Facing.x<0?-.55f:.55f));
                if(Model.SteamTier<3||t<7.5)continue;
                double flight=t-7.5;
                SteamFx("skill_02_begin_01",flight,actor,actor.Position,SteamMount(actor,"Common2",now));
                SteamFx("skill_02_begin_02",flight,actor,actor.Position,foot);
                if(flight>=1.2&&flight<=4.2)
                    foreach(var target in Model.Defenders.Where(target=>target.Alive))
                    {
                        var point=ActorWorldPoint(target.Position)+Vector3.up*.025f;
                        SteamFx("skill_02_marker_01",flight-1.2,actor,target.Position,point,true);
                        SteamFx("skill_02_range_01",flight-1.2,actor,target.Position,point,true);
                    }
                SteamFx("skill_02_end_01",flight-5.05,actor,actor.Position,foot);
            }
        }
        bool PresentFormationSteamHit(FormationHit hit,FormationFighter actor)
        {
            if(hit.Secondary||hit.Heal||hit.Amount<=0&&!hit.Absorbed)return false;
            string key=!hit.Skill?"attack_hit_01":hit.DamageType==1?"skill_02_hit_01":"skill_01_hit_01";
            var target=FormationFeedbackActor(hit.ToId);
            var point=key=="skill_02_hit_01"?ActorWorldPoint(hit.To)+Vector3.up*.025f:FormationFeedbackContactPoint(target,hit.To,.48f);
            return SteamFx(key,(Model.Clock-hit.At)*(hit.Action?.Rate??2),actor,hit.To,point,false,36);
        }
        Vector3 SteamImpactCameraOffset()
        {
            if(Model==null||Model.Phase!=FormationPhase.Fighting||!UseFormationBattleCamera)return Vector3.zero;
            float offset=0;
            foreach(var hit in Model.Hits.Where(hit=>hit.SourceArtId=="STEAM"&&!hit.Secondary&&!hit.Heal&&hit.Amount>0&&Model.Clock>=hit.At&&Model.Clock-hit.At<.11).GroupBy(hit=>hit.At).Select(group=>group.First()))
            {
                float age=(float)(Model.Clock-hit.At);offset+=Mathf.Sin(age*200)*Mathf.Exp(-age*44)*(hit.Skill?.065f:.035f);
            }
            return new Vector3(Mathf.Clamp(offset,-.07f,.07f),Mathf.Abs(offset)*.3f,0);
        }
    }
}
