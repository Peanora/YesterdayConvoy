using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        internal const float FormationVisualRate=2f;
        FormationBattleModel animationProfileModel;
        readonly Dictionary<string,string> formationAnimationProfileKeys=new Dictionary<string,string>();

        void RefreshFormationAnimationProfiles()
        {
            if(Model==null)return;
            if(animationProfileModel!=Model){animationProfileModel=Model;formationAnimationProfileKeys.Clear();}
            foreach(var actor in Model.Units.Cast<FormationActor>().Concat(Model.Defenders).Concat(Model.Summons))
            {
                var unit=actor as FormationFighter;
                string signature=(unit?.NativePhase??0)+"/"+(unit?.Star??1)+"/"+(Campaign?.TalulahCost??3)+"/"+(Campaign?.WisadelSkill??3)+"/"+(actor.ArtId=="WISADEL_APRIL"&&Model.PresentationBuff(actor,"wisadel")>0);
                if(formationAnimationProfileKeys.TryGetValue(actor.ArtId,out var old)&&old==signature)continue;
                FormationAnimationProfile profile;
                if(actor.ArtId.StartsWith("D",StringComparison.Ordinal))
                {
                    if(!art.TryGetValue(FormationBodyArtId(actor),out var defender))continue;
                    defender.Clips.TryGetValue("attack",out var attack);
                    float duration=attack?.Duration??0;
                    if(attack!=null){if(defender.Clips.TryGetValue("attack_start",out var start))duration+=start.Duration;if(defender.Clips.TryGetValue("attack_end",out var end))duration+=end.Duration;}
                    profile=new FormationAnimationProfile{AttackDuration=duration,AttackContact=Mathf.Clamp(defender.Contact,0,duration),SkillDuration=duration,BaseRate=FormationVisualRate,HasOrdinaryAttack=attack!=null};
                }
                else
                {
                    var source=FormationUnitArt.For(actor.ArtId);if(source==null)continue;
                    string attack=FormationSourceAction(source,actor,false),skill=FormationSourceAction(source,actor,true);
                    if(!source.HasOrdinaryAttack&&FormationCampaignRules.Find(actor.ArtId)?.DamageType=="治疗")attack=source.HasAction("cast")?"cast":"idle";
                    float attackDuration=FormationSourceDuration(source,actor,false,attack),skillDuration=FormationSourceDuration(source,actor,true,skill);
                    float contact=FormationSourceContact(source,actor,false,attack),skillContact=FormationSourceContact(source,actor,true,skill);
                    // Missing source markers are explicitly kept as the established
                    // contact timing; no invented marker is labelled as original.
                    if(float.IsNaN(contact))contact=0;
                    if(float.IsNaN(skillContact))skillContact=0;
                    profile=new FormationAnimationProfile{AttackDuration=attackDuration,AttackContact=Mathf.Clamp(contact,0,attackDuration),SkillDuration=skillDuration,SkillContact=Mathf.Clamp(skillContact,0,skillDuration),BaseRate=FormationVisualRate,HasOrdinaryAttack=source.HasOrdinaryAttack};
                    if(FormationProjectileProfiles.TryFor(actor.ArtId,unit?.NativePhase??0,out var projectile))
                    {profile.ProjectileSpeed=projectile.Speed;profile.ProjectileRaiseHeight=projectile.RaiseHeight;profile.ProjectileDelay=projectile.Delay;profile.ProjectileMovement=projectile.MovementClass;profile.ProjectileNoRaiseThreshold=projectile.NoRaiseHeightThreshold;}
                    if(source.NativeTask=="WIS_APRIL_G9"&&(Campaign?.WisadelSkill==1||Campaign?.WisadelSkill==3))
                    {
                        bool third=Campaign.WisadelSkill==3;
                        profile.SkillProjectileSpeed=third?4.25f:10;
                        profile.SkillProjectileMovement="AdvancedMovement";
                        profile.SkillProjectileWindup=third?source.ContactTime("Skill_3_Loop"):0;
                    }
                    if(source.NativeTask=="WIS_APRIL_G9"&&Model.PresentationBuff(actor,"wisadel")>0)
                    {profile.ProjectileSpeed=0;profile.ProjectileMovement=null;profile.AttackFlightSeconds=0;}
                }
                Model.SetAnimationProfile(actor.ArtId,profile);formationAnimationProfileKeys[actor.ArtId]=signature;
            }
        }
        string FormationSourceAction(FormationUnitArt source,FormationActor actor,bool skill)
        {
            if(actor.ArtId=="B21"&&skill)return SuiClawMotion;
            if(source.NativeTask=="WIS_APRIL_G9")
                return skill?(Campaign?.WisadelSkill==1?"Skill_1":Campaign?.WisadelSkill==2?"Skill_2_Begin":"Skill_3_Begin"):
                    Model.PresentationBuff(actor,"wisadel")>0?"Skill_2_Loop":"Attack_A";
            string key=skill?"cast":"attack";double age=0;bool loop=false;
            if(source.NativeTask!=null)
            {
                var probe=new FormationFighter{ArtId=actor.ArtId,NativePhase=(actor as FormationFighter)?.NativePhase??0,Alive=true,LastActionAt=0,LastSkillAt=skill?0:-100,ActionUntil=1,PresentationDuration=1,PresentationStartedAt=0,PresentationRate=FormationVisualRate};
                probe.SkillAnimation=actor.ArtId=="B22"?"Attack_2":actor.ArtId=="WISADEL_APRIL"?(Campaign?.WisadelSkill==1?"Skill_1":Campaign?.WisadelSkill==2?"Skill_2_Begin":"Skill_3_Begin"):"skill";
                probe.PresentationAction=skill?probe.SkillAnimation:"attack";
                SelectNativeFormationMotion(source,probe,0,false,ref key,ref age,ref loop);
            }
            else if(!source.HasAction(key))
                key=skill&&source.HasAction("skill")?"skill":source.HasOrdinaryAttack?"attack":"idle";
            return source.HasAction(key)?key:"idle";
        }
        Vector3 FormationDisplayPoint(FormationActor actor)
        {
            var point=ActorWorldPoint(actor);
            if(actor is FormationFighter passenger&&Campaign!=null)
            {
                var carrier=Model.PresentationCarrier(passenger);
                if(carrier!=null)
                {
                    if(Vector2.Distance(carrier.Position,passenger.Position)<.15f)
                        point+=battleCamera.transform.up*Mathf.Min(.85f,ActorPresentationHeight(carrier)*.48f)-battleCamera.transform.right*.22f;
                }
            }
            return point;
        }
    }
    public sealed partial class FormationBattleModel
    {
        internal FormationFighter PresentationCarrier(FormationFighter passenger)
        {int id=State(passenger).RidingCarrier;return id==0?null:Units.FirstOrDefault(u=>u.Id==id&&u.Alive);}
        internal float PresentationStack(FormationActor actor,string key)=>Stack(actor,key);
        internal float PresentationBuff(FormationActor actor,string key)=>Buff(actor,key);
    }
}
