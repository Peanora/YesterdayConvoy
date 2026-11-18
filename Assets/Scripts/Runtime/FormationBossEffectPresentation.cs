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
        readonly HashSet<int> formationBeamActions=new HashSet<int>();
        const double PhaseArrivalSeconds = .65;
        internal void PresentFormationNativeEffects()
        {
            if(Model==null||actorRoot==null||battleCamera==null)return;
            if(formationFxModel!=Model){formationFxModel=Model;formationRageBorn.Clear();}
            if(formationEffects==null)formationEffects=new BossRosterFxLayer(actorRoot);
            foreach(var view in formationAuxBodies.Values)if(view!=null)view.enabled=false;
            formationImpactOwners.Clear();
            formationBeamActions.Clear();
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
                    string attackTrack=FormationOrdinaryEffectTrack(source,actor);
                    foreach(var action in Model.PresentationActions.Where(a=>a.ActorId==actor.Id&&!a.Cancelled))
                    {
                        if(actor.ArtId=="B21"&&action.Skill)continue;
                        double end=action.EndAt;
                        if(action.At>now||now>Math.Max(end,action.ContactAt)+6)continue;
                        foreach(var binding in bindings.Where(b=>(b.phase<0||b.phase==phase)&&FormationBindingMatches(b,source,actor,action.Skill||action.Heal,action.Animation,attackTrack)&&FormationBindingDirection(b,actor)))
                        {
                            if(binding.kind=="AttackStart"||binding.kind=="SkillStart"||binding.kind=="StateStart")
                            {
                                if(action.ProjectileOnly)continue;
                                if(!actor.Alive||(binding.finishOnEnd||binding.loop)&&now>end)continue;
                                // These v0.7 abilities are friendly support. Their original
                                // hostile projectile/target layers must not hit a defender.
                                if((action.Skill||action.Heal)&&FormationFriendlySkill(source.NativeTask)&&binding.attach!="owner")continue;
                                if(action.Skill&&source.NativeTask=="B13")continue;
                                var point=binding.attach=="target"||binding.attach=="ground"?action.To:actor.Position;
                                ShowFormationBoundEffect(binding,source.NativeTask,(now-action.At)*action.Rate,point,actor,scale,action.To-action.From);
                            }
                            else if(binding.kind=="ProjectileSpawn"&&now>=action.LaunchedAt)
                            {
                                if(action.Heal||action.Skill&&FormationFriendlySkill(source.NativeTask))continue;
                                double launchAge=(now-action.LaunchedAt)*action.Rate;
                                if(binding.attach=="owner")
                                {
                                    // A launch flash stays with its emitter, including
                                    // instantaneous actions with no projectile flight.
                                    if(actor.Alive&&(!(binding.finishOnEnd||binding.loop)||now<=end))
                                        ShowFormationBoundEffect(binding,source.NativeTask,launchAge,action.From,actor,scale,action.To-action.From,
                                            positionOverride:LegacyOwnerPoint(actor,action.From));
                                    continue;
                                }
                                if(binding.attach!="projectile"||now>=action.ContactAt)continue;
                                float t=Mathf.Clamp01((float)((now-action.LaunchedAt)/(action.ContactAt-action.LaunchedAt)));
                                var point=Vector2.Lerp(action.From,action.To,t);
                                float arc=action.ProjectileRaiseHeight*4*t*(1-t);
                                var start=FormationFeedbackContactPoint(actor,action.From,.58f);
                                var endPoint=FormationFeedbackContactPoint(FormationFeedbackActor(action.TargetId),action.To,.54f);
                                var flight=Vector3.Lerp(start,endPoint,t)+Vector3.up*arc;
                                ShowFormationBoundEffect(binding,source.NativeTask,launchAge,point,actor,scale,action.To-action.From,positionOverride:flight);
                            }
                            else if(binding.kind=="StateEnd"&&!action.ProjectileOnly&&now>=end)
                                ShowFormationBoundEffect(binding,source.NativeTask,(now-end)*action.Rate,actor.Position,actor,scale,actor.Facing);
                        }
                    }
                    foreach(var binding in bindings.Where(b=>(b.phase<0||b.phase==phase)&&FormationBindingDirection(b,actor)))
                    {
                        // StartBattle initializes this timestamp to zero. That is
                        // not a rebirth event and must not play invulnerability FX.
                        if(binding.kind=="PhaseStart"&&actor.NativePhaseChangedAt>0&&
                            (!binding.loop||now-actor.NativePhaseChangedAt<PhaseArrivalSeconds))
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
            PresentFormationCadejoBlinks(now);
            PresentFormationSuiClawEffects();
            foreach(var hit in Model.Hits)
            {
                double age=(now-hit.At)*FormationVisualRate;if(age<0||age>6||!FormationPointVisibleInView(hit.To))continue;
                var actor=Model.Units.FirstOrDefault(u=>u.Id==hit.FromId);
                FormationUnitArt source=null;if(actor!=null)formationMonsterArt.TryGetValue(actor.ArtId,out source);
                if(actor!=null&&FormationHasLegacyImpact(actor.ArtId,hit.Skill))formationImpactOwners.Add(hit);
                if(actor?.ArtId=="B26"&&hit.Skill&&!hit.Secondary&&hit.Action!=null&&formationBeamActions.Add(hit.Action.Sequence))
                    PresentFormationBeams(actor,hit.To,now-hit.At);
                if(actor?.ArtId=="B08"&&hit.Skill&&hit.Secondary&&!hit.Heal&&source!=null)
                    PresentFormationBurnContact(hit,actor,source,age);
                if(!formationImpactOwners.Contains(hit)&&actor!=null&&source!=null&&!hit.Heal&&!hit.Secondary&&actor.ArtId!="B21")
                {
                    int phase=source.NativeTask=="B08"&&actor.NativePhase>0?3:actor.NativePhase+1;
                    var action=hit.Action;
                    int index=hit.ImpactIndex;
                    string attackTrack=hit.Skill?null:FormationOrdinaryEffectTrack(source,actor);
                    foreach(var binding in FormationBindings(source.NativeTask).Where(b=>b.kind=="Impact"&&(b.phase<0||b.phase==phase)&&(b.hitIndex<0||b.hitIndex==index)&&FormationBindingDirection(b,actor)&&FormationBindingMatches(b,source,actor,hit.Skill,action?.Animation??actor.SkillAnimation,attackTrack)))
                    {
                        Vector3? contact=binding.attach=="target"&&Mathf.Abs(binding.offsetY)+Mathf.Abs(binding.offsetZ)<.001f?
                            FormationFeedbackContactPoint(FormationFeedbackActor(hit.ToId),hit.To,.55f):(Vector3?)null;
                        if(ShowFormationBoundEffect(binding,source.NativeTask,age,hit.To,actor,ActorPresentationHeight(actor)/Math.Max(.001f,source.Height),hit.To-hit.From,positionOverride:contact))
                            formationImpactOwners.Add(hit);
                    }
                }
            }
            formationEffects.End();PresentFormationLegacyEffects();
        }
        static bool FormationBindingDirection(BossRosterFxArt.Binding binding,FormationActor actor)
            =>binding.direction!="left"&&binding.direction!="right"||binding.direction==(actor.Facing.x<0?"left":"right");
        static bool FormationFriendlySkill(string task)=>task=="B09"||task=="B10"||task=="B11"||task=="B13";
        void PresentFormationBurnContact(FormationHit hit,FormationFighter actor,FormationUnitArt source,double age)
        {
            const string scorch="enemy_fthlgj_phase_1_skill_1_hit";
            var binding=FormationBindings(source.NativeTask).FirstOrDefault(item=>item.kind=="Impact"&&item.key==scorch);
            if(binding==null)return;
            Vector3 contact=ActorWorldPoint(hit.To)+battleCamera.transform.up*.18f;
            if(ShowFormationBoundEffect(binding,source.NativeTask,age,hit.To,actor,
                ActorPresentationHeight(actor)/Math.Max(.001f,source.Height)*.72f,hit.To-hit.From,positionOverride:contact))
                formationImpactOwners.Add(hit);
        }
        string FormationOrdinaryEffectTrack(FormationUnitArt source,FormationFighter actor)
        {
            if(source.NativeTask==null||!source.HasOrdinaryAttack)return null;
            string motion=FormationSourceAction(source,actor,false);
            return source.SourceAction(motion)??motion;
        }
        static bool FormationBindingAnimationMatches(string task,string authored,string track)
        {
            if(string.IsNullOrEmpty(authored)||authored=="None"||string.IsNullOrEmpty(track))return true;
            if(string.Equals(authored,track,StringComparison.OrdinalIgnoreCase))return true;
            // Several packages label both phase variants simply "Attack".
            if(authored=="Attack")return track.IndexOf("Attack",StringComparison.OrdinalIgnoreCase)>=0;
            // The captured second B23 phase shares the first phase's FX tracks.
            return task=="B23"&&authored.StartsWith("A_",StringComparison.Ordinal)&&track.StartsWith("B_",StringComparison.Ordinal)&&
                string.Equals(authored.Substring(2),track.Substring(2),StringComparison.Ordinal);
        }
        bool FormationBindingMatches(BossRosterFxArt.Binding binding,FormationUnitArt source,FormationFighter actor,bool skill,string animation,string attackTrack)
        {
            if(!skill)
            {
                if(binding.skill!="attack"&&binding.nativeSkill!="attack")return false;
                // B12 has three identical native regions for its original three
                // releases. The chessboard model emits only one ordinary bolt.
                if(source.NativeTask=="B12"&&binding.region>1)return false;
                return FormationBindingAnimationMatches(source.NativeTask,binding.animation,attackTrack);
            }
            string native;
            switch(source.NativeTask)
            {
                case "B04":native=actor.NativePhase>0?"circleattack2":"circleattack";break;
                case "B07":native=actor.NativePhase>0?"chain_2":"chain_1";break;
                case "B08":native=actor.NativePhase>0?"burn_tile_phase_2":"burn_tile";break;
                // The existing caster-only gesture is usable for this friendly
                // skill. Its original hostile trail and hit stay suppressed.
                case "B09":native=actor.NativePhase>0?"replace2":"replace";break;
                case "B11":native=actor.NativePhase>0?"deathmarks2":"deathmarks1";break;
                case "B12":native=actor.NativePhase>0?"deepbreaths4":"deepbreaths1";break;
                case "B13":native=actor.NativePhase>1?"m2rangeprojectile":actor.NativePhase>0?"m1rangeprojectile":"melee";break;
                case "B23":native=actor.NativePhase>0?"m1multirangedattack":"m0multirangedattack";break;
                case "B25":native=actor.NativePhase>0?"shootgun_2":"shootgun_1";break;
                default:native=FormationNativeSkill(source.NativeTask,animation);break;
            }
            if(source.NativeTask=="B09"&&binding.attach!="owner")return false;
            return string.Equals(binding.skill,native,StringComparison.OrdinalIgnoreCase)||string.Equals(binding.nativeSkill,native,StringComparison.OrdinalIgnoreCase);
        }
        bool ShowFormationBoundEffect(BossRosterFxArt.Binding binding,string task,double age,Vector2 point,FormationActor actor,float scale,Vector2 direction,Vector3? positionOverride=null,Vector2? facingOverride=null)
        {
            var clip=BossRosterFxArt.Get(BossRosterFxArt.Key(task,binding.key));
            if(!clip.Sequence.Ready||!FormationPointVisibleInView(point))return false;
            bool flip=(facingOverride??actor.Facing).x<0&&binding.direction!="left"&&binding.direction!="right"&&binding.attach!="projectile";
            var position=positionOverride??(binding.attach=="owner"?FormationDisplayPoint(actor):ActorWorldPoint(point));
            position+=battleCamera.transform.right*(binding.offsetX*scale*(flip?-1:1))+battleCamera.transform.up*(binding.offsetY*scale)+Vector3.up*(binding.offsetZ*scale+.025f);
            bool ground=binding.attach=="ground";
            int order=5000-Mathf.RoundToInt(point.y*50);
            return formationEffects.Show(clip,age,position,battleCamera,flip,binding.loop,new Vector3(direction.x,0,direction.y),binding.attach=="projectile",ground?-3:6,0,scale,order,ground);
        }

        void PresentFormationCadejoBlinks(double now)
        {
            foreach(var blink in Model.CadejoBlinks)
            {
                if(blink.Cancelled||now<blink.StartedAt||now>blink.StartedAt+.6)continue;
                var actor=FormationFeedbackActor(blink.ActorId);
                if(actor==null||!actor.Alive)continue;
                var position=now>=blink.ArrivedAt?blink.To:blink.From;
                // Keep the release's source time through the actual snap. Crossing
                // the seam therefore reveals the flash instead of replaying blank frames.
                double age=(now-blink.StartedAt)*FormationVisualRate;
                foreach(var bind in FormationBindings("B14").Where(b=>b.nativeSkill=="blink"&&!b.loop))
                    ShowFormationBoundEffect(bind,"B14",age,position,actor,1,blink.Facing,ActorWorldPoint(position),blink.Facing);
            }
        }
    }
}
