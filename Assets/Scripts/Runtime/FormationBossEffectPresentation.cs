using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        FormationBattleModel formationFxModel;
        readonly Dictionary<int,double> formationRageBorn=new Dictionary<int,double>();
        internal void PresentFormationNativeEffects()
        {
            if(Model==null||actorRoot==null||battleCamera==null)return;
            if(formationFxModel!=Model){formationFxModel=Model;formationRageBorn.Clear();}
            if(formationEffects==null)formationEffects=new BossRosterFxLayer(actorRoot);
            foreach(var view in formationAuxBodies.Values)if(view!=null)view.enabled=false;
            formationEffects.Begin();double now=Model.Clock;
            foreach(var actor in Model.Units.Where(u=>u.Enabled))
            {
                if(!FormationActorVisibleInView(actor)||!formationMonsterArt.TryGetValue(actor.ArtId,out var source))continue;
                var bindings=FormationBindings(source.NativeTask);
                int phase=source.NativeTask=="B08"&&actor.NativePhase>0?3:actor.NativePhase+1;
                bool legacyCore=new[]{"TALULAH_TRIAL","M14","M20","M25","M28","M32"}.Contains(actor.ArtId);
                float scale=ActorPresentationHeight(actor)/Math.Max(.001f,source.Height);
                if(!legacyCore)
                {
                    foreach(var action in Model.PresentationActions.Where(a=>a.ActorId==actor.Id&&!a.Cancelled))
                    {
                        double end=action.EndAt;
                        if(action.At>now||now>Math.Max(end,action.ContactAt)+6)continue;
                        foreach(var binding in bindings.Where(b=>(b.phase<0||b.phase==phase)&&FormationBindingMatches(b,source,actor,action.Skill||action.Heal,action.Animation)&&FormationBindingDirection(b,actor)))
                        {
                            if(binding.kind=="AttackStart"||binding.kind=="SkillStart"||binding.kind=="StateStart")
                            {
                                if(action.ProjectileOnly)continue;
                                if(!actor.Alive||binding.finishOnEnd&&now>end)continue;
                                // These v0.7 abilities are friendly support. Their original
                                // hostile projectile/target layers must not hit a defender.
                                if((action.Skill||action.Heal)&&FormationFriendlySkill(source.NativeTask)&&binding.attach!="owner")continue;
                                if(action.Skill&&source.NativeTask=="B13")continue;
                                var point=binding.attach=="target"||binding.attach=="ground"?action.To:actor.Position;
                                ShowFormationBoundEffect(binding,source.NativeTask,(now-action.At)*action.Rate,point,actor,scale,action.To-action.From);
                            }
                            else if(binding.kind=="ProjectileSpawn"&&now>=action.LaunchedAt&&now<action.ContactAt)
                            {
                                if(action.Heal||action.Skill&&FormationFriendlySkill(source.NativeTask))continue;
                                float t=Mathf.Clamp01((float)((now-action.LaunchedAt)/Math.Max(.0001,action.ContactAt-action.LaunchedAt)));
                                var point=Vector2.Lerp(action.From,action.To,t);
                                float arc=action.ProjectileRaiseHeight*4*t*(1-t);
                                ShowFormationBoundEffect(binding,source.NativeTask,(now-action.LaunchedAt)*action.Rate,point,actor,scale,action.To-action.From,arc);
                            }
                            else if(binding.kind=="StateEnd"&&!action.ProjectileOnly&&now>=end)
                                ShowFormationBoundEffect(binding,source.NativeTask,(now-end)*action.Rate,actor.Position,actor,scale,actor.Facing);
                        }
                    }
                    foreach(var binding in bindings.Where(b=>(b.phase<0||b.phase==phase)&&FormationBindingDirection(b,actor)))
                    {
                        if(binding.kind=="PhaseStart"&&actor.NativePhaseChangedAt>=0)
                            ShowFormationBoundEffect(binding,source.NativeTask,(now-actor.NativePhaseChangedAt)*FormationVisualRate,actor.Position,actor,scale,actor.Facing);
                        else if(binding.kind=="Death"&&!actor.Alive)
                            ShowFormationBoundEffect(binding,source.NativeTask,(now-actor.LastDeathAt)*FormationVisualRate,actor.Position,actor,scale,actor.Facing);
                    }
                    if(source.NativeTask=="B22"&&actor.Alive&&Model.PresentationStack(actor,"ramp")>=5)
                    {
                        if(!formationRageBorn.TryGetValue(actor.Id,out var born))formationRageBorn[actor.Id]=born=now;
                        foreach(var binding in bindings.Where(b=>b.kind=="StateStart"&&b.skill=="rage"))
                            ShowFormationBoundEffect(binding,source.NativeTask,(now-born)*FormationVisualRate,actor.Position,actor,scale,actor.Facing);
                    }
                }
                if(source.NativeTask=="B10"&&actor.Alive)
                {
                    foreach(var target in Model.Units.Where(u=>u.Enabled&&u.Alive&&Model.PresentationBuff(u,"can_attack")>0))
                    {
                        double began=Model.LegacyBuffUntil(target,"can_attack")-10;
                        foreach(var binding in bindings.Where(b=>b.kind=="SkillStart"&&b.attach=="target"&&b.nativeSkill=="spray"))
                            ShowFormationBoundEffect(binding,source.NativeTask,(now-began)*FormationVisualRate,target.Position,actor,scale,target.Position-actor.Position);
                    }
                }
            }
            foreach(var hit in Model.Hits)
            {
                double age=(now-hit.At)*FormationVisualRate;if(age<0||age>6||!FormationPointVisibleInView(hit.To))continue;
                var actor=Model.Units.FirstOrDefault(u=>u.Id==hit.FromId);
                FormationUnitArt source=null;if(actor!=null)formationMonsterArt.TryGetValue(actor.ArtId,out source);
                bool displayed=actor!=null&&FormationHasLegacyImpact(actor.ArtId,hit.Skill);
                if(actor?.ArtId=="B26"&&hit.Skill)PresentFormationBeams(actor,hit.To,now-hit.At);
                if(!displayed&&actor!=null&&source!=null&&!hit.Heal)
                {
                    int phase=source.NativeTask=="B08"&&actor.NativePhase>0?3:actor.NativePhase+1;
                    var action=Model.PresentationActions.LastOrDefault(a=>a.ActorId==actor.Id&&a.Skill==hit.Skill&&a.At<=hit.At+.001&&!a.Cancelled);
                    int index=Model.Hits.Where(h=>h.FromId==hit.FromId&&h.Skill==hit.Skill&&h.At>=(action?.At??hit.At)-.001).Select(h=>h.At).Distinct().OrderBy(t=>t).TakeWhile(t=>t<hit.At-.001).Count();
                    foreach(var binding in FormationBindings(source.NativeTask).Where(b=>b.kind=="Impact"&&(b.phase<0||b.phase==phase)&&(b.hitIndex<0||b.hitIndex==index)&&FormationBindingDirection(b,actor)&&FormationBindingMatches(b,source,actor,hit.Skill,action?.Animation??actor.SkillAnimation)))
                        displayed|=ShowFormationBoundEffect(binding,source.NativeTask,age,hit.To,actor,ActorPresentationHeight(actor)/Math.Max(.001f,source.Height),hit.To-hit.From);
                }
                if(!displayed)
                {
                    string key=hit.Heal?"common/common_heal_hit_01":actor!=null&&FormationCampaignRules.Find(actor.ArtId)?.DamageType=="法"?"common/common_magic_hit_01":"common/common_cuting_hit_01";
                    var original=OriginalCombatVfxArt.Get(key);
                    if(original.Sequence.Ready)ShowFormationClip(new BossRosterFxArt.Clip(original.Sequence,original.HdrRange,0,original.Loop),age,hit.To,actor,1,false);
                }
            }
            formationEffects.End();PresentFormationLegacyEffects();
        }
        static bool FormationBindingDirection(BossRosterFxArt.Binding binding,FormationActor actor)
            =>binding.direction!="left"&&binding.direction!="right"||binding.direction==(actor.Facing.x<0?"left":"right");
        static bool FormationFriendlySkill(string task)=>task=="B09"||task=="B10"||task=="B11"||task=="B13";
        bool FormationBindingMatches(BossRosterFxArt.Binding binding,FormationUnitArt source,FormationFighter actor,bool skill,string animation)
        {
            if(!skill)return binding.skill=="attack"||binding.nativeSkill=="attack";
            string native;
            switch(source.NativeTask)
            {
                case "B04":native=actor.NativePhase>0?"circleattack2":"circleattack";break;
                case "B07":native=actor.NativePhase>0?"chain_2":"chain_1";break;
                case "B08":native=actor.NativePhase>0?"burn_tile_phase_2":"burn_tile";break;
                case "B09":native=actor.NativePhase>0?"control2":"control";break;
                case "B11":native=actor.NativePhase>0?"deathmarks2":"deathmarks1";break;
                case "B12":native=actor.NativePhase>0?"deepbreaths4":"deepbreaths1";break;
                case "B13":native=actor.NativePhase>1?"m2rangeprojectile":actor.NativePhase>0?"m1rangeprojectile":"melee";break;
                case "B21":
                    string pose="cast";double age=0;bool loop=false;
                    SelectNativeFormationMotion(source,actor,Model.Clock,false,ref pose,ref age,ref loop);
                    native=pose.Contains("Loop")?"threestrikes":"bombard";break;
                case "B23":native=actor.NativePhase>0?"m1multirangedattack":"m0multirangedattack";break;
                case "B25":native=actor.NativePhase>0?"shootgun_2":"shootgun_1";break;
                default:native=FormationNativeSkill(source.NativeTask,animation);break;
            }
            return string.Equals(binding.skill,native,StringComparison.OrdinalIgnoreCase)||string.Equals(binding.nativeSkill,native,StringComparison.OrdinalIgnoreCase);
        }
        bool ShowFormationBoundEffect(BossRosterFxArt.Binding binding,string task,double age,Vector2 point,FormationActor actor,float scale,Vector2 direction,float lift=0)
        {
            var clip=BossRosterFxArt.Get(BossRosterFxArt.Key(task,binding.key));
            if(!clip.Sequence.Ready||!FormationPointVisibleInView(point))return false;
            bool flip=actor.Facing.x<0&&binding.direction!="left"&&binding.direction!="right"&&binding.attach!="projectile";
            var position=binding.attach=="owner"?FormationDisplayPoint(actor):ActorWorldPoint(point);
            position+=battleCamera.transform.right*(binding.offsetX*scale*(flip?-1:1))+battleCamera.transform.up*(binding.offsetY*scale)+Vector3.up*(binding.offsetZ*scale+lift+.025f);
            bool ground=binding.attach=="ground";
            int order=5000-Mathf.RoundToInt(point.y*50);
            return formationEffects.Show(clip,age,position,battleCamera,flip,binding.loop,new Vector3(direction.x,0,direction.y),binding.attach=="projectile",ground?-3:6,0,scale,order,ground);
        }
    }
}
