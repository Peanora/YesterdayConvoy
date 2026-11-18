using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // This identity is the original April skin. B20 remains the distinct sale#14
    // roster identity; its clips must never be used as this card's fallback.
    internal static class WisadelAprilFxArt
    {
        internal const string Task="WIS_APRIL_G9";
        static readonly string[] S1={"skill_01_start","skill_01_start_02","skill_01_trail","skill_01_hit","skill_01_hit_02"};
        static readonly string[] S2={"skill_02_buff","skill_02_start","skill_02_hit","skill_02_hit_02"};
        static readonly string[] S3={"skill_03_buff_f","skill_03_start_game#9","skill_03_trail_game#9",
            "skill_03_hit_game#9","skill_03_hit_02_game#9","skill_03_hit_03_game#9"};
        internal static BossRosterFxArt.Clip Get(string suffix)=>BossRosterFxArt.Get("boss/"+Task+"/wisdel_"+suffix);
        internal static bool Available=>BossRosterFxArt.TryGet("boss/"+Task+"/wisdel_skill_01_start",out _);
        internal static IEnumerable<ArtSequence> Sequences(int skill)
        {
            if(!Available)yield break;
            var selected=skill==1?S1:skill==2?S2:S3;
            foreach(string key in selected.Concat(S1).Concat(S2).Concat(S3).Distinct())
                yield return Get(key).Sequence;
        }
    }

    public sealed partial class FormationBattleController
    {
        IReadOnlyList<Sprite> wisadelShellFaces;
        readonly List<LineRenderer> wisadelOrdinaryTrails=new List<LineRenderer>();
        int wisadelOrdinaryTrailsUsed;

        // The approved M01 shot in EngagementView starts at the comic launch
        // anchor and uses a narrow amber-to-white trail with a shallow arc.
        // Drive the same geometry from this board's actual projectile interval.
        void PresentApprovedWisadelOrdinaryTrail(FormationPresentationAction action,FormationFighter actor)
        {
            double now=Model.Clock;
            if(now<action.LaunchedAt||now>=action.ContactAt)return;
            float progress=(float)((now-action.LaunchedAt)/(action.ContactAt-action.LaunchedAt));
            if(!FormationPointVisibleInView(Vector2.Lerp(action.From,action.To,progress)))return;

            var start=LegacyOwnerPoint(actor,action.From)+battleCamera.transform.up*.75f;
            var end=ApprovedWisadelComicImpactPoint(action.To);
            float side=Vector3.Dot(end-start,battleCamera.transform.right)<0?-1:1;
            start+=battleCamera.transform.right*(side*.45f);
            var head=Vector3.Lerp(start,end,progress)+battleCamera.transform.up*(Mathf.Sin(progress*Mathf.PI)*.12f);

            if(wisadelOrdinaryTrailsUsed==wisadelOrdinaryTrails.Count)
            {
                if(legacyComicMaterial==null)
                    legacyComicMaterial=new Material(Shader.Find("Sprites/Default")){name="Formation / approved April comic burst"};
                var trail=new GameObject("April comic / approved ordinary shot",typeof(LineRenderer)).GetComponent<LineRenderer>();
                trail.transform.SetParent(actorRoot,false);
                trail.sharedMaterial=legacyComicMaterial;
                trail.useWorldSpace=true;
                trail.positionCount=2;
                trail.widthMultiplier=.035f;
                trail.startColor=new Color(.95f,.68f,.3f);
                trail.endColor=Color.white;
                wisadelOrdinaryTrails.Add(trail);
            }
            var view=wisadelOrdinaryTrails[wisadelOrdinaryTrailsUsed++];
            view.SetPosition(0,Vector3.Lerp(start,head,.65f));
            view.SetPosition(1,head);
            view.sortingOrder=LegacyOrder(action.To,50);
            view.enabled=true;
        }

        Vector3 ApprovedWisadelComicImpactPoint(Vector2 cell)=>ActorWorldPoint(cell)+battleCamera.transform.up*.8f;

        bool UsesApprovedWisadelOrdinaryImpact(FormationHit hit) =>
            !hit.Skill&&!hit.Heal&&(Campaign?.WisadelSkill!=2||!WisadelEmpoweredAt(hit.FromId,hit.At));

        SpriteRenderer LegacyWisadel(string suffix,double age,Vector2 cell,int order=24,bool loop=false,
            bool flip=false,Vector3? point=null,Vector3? direction=null)
        {
            if(age<0||!WisadelAprilFxArt.Available)return null;
            var clip=WisadelAprilFxArt.Get(suffix);if(!clip.Sequence.Ready||loop&&!clip.Loop)return null;
            float roll=0;
            if(direction.HasValue)
            {
                var delta=direction.Value;float x=Vector3.Dot(delta,battleCamera.transform.right),y=Vector3.Dot(delta,battleCamera.transform.up);
                if(x*x+y*y>1e-8)roll=Mathf.Atan2(y,x)*Mathf.Rad2Deg;
            }
            // Native-to-game scale .0045/.0027 is already in the atlas PPU.
            return LegacyDraw(clip.At(age,loop),legacyHdrMaterial,point??ActorWorldPoint(cell),cell,1,order,flip,clip.HdrRange,roll:roll);
        }
        Vector3 LegacyWisadelMuzzle(FormationActor actor,FormationPresentationAction action)
        {
            // Original front F_Muzzle bone at the exact OnAttack marker, not
            // BoneFollower's uninitialised (1,1) prefab placeholder. Body sampling
            // does not contain the FaceSwitcher position, so do not add it again.
            bool down=action.Animation!=null&&action.Animation.IndexOf("Down",StringComparison.Ordinal)>=0;
            Vector2 muzzle=down?new Vector2(.3375f,.1125f):new Vector2(.45f,.45f);
            if(actor.Facing.x<0)muzzle.x=-muzzle.x;
            return LegacyOwnerPoint(actor,action.From)+battleCamera.transform.right*muzzle.x+battleCamera.transform.up*muzzle.y;
        }
        void PresentOriginalWisadelAction(FormationPresentationAction action,FormationFighter actor)
        {
            if(action.Cancelled||action.Heal)return;
            double age=(Model.Clock-action.At)*action.Rate;if(age<0)return;
            int skill=action.Skill?WisadelSkillForAction(action):(Campaign?.WisadelSkill??3);bool flip=actor.Facing.x<0;
            bool empoweredAuto=!action.Skill&&skill==2&&WisadelEmpoweredAt(actor.Id,action.LaunchedAt);
            if(!action.Skill&&!empoweredAuto)
            {
                PresentApprovedWisadelOrdinaryTrail(action,actor);
                return;
            }
            if(!WisadelAprilFxArt.Available)return;
            string cast=action.Skill?(skill==1?"skill_01_start":null):"skill_02_start";
            // Each true S3 shell has a source loop action. S1 is one source cast
            // with multiple applied segments, so its projectile cues do not
            // restart the Skill_1 caster effect.
            if(action.ProjectileOnly)
            {
                cast=skill==3?"skill_03_start_game#9":null;
                // Child.At is the true Loop start, not the parent mode-start.
                // Native FxDelay=.5 remains inside the sampled start clip.
                age=(Model.Clock-action.At)*action.Rate;
                if(age<0)return;
            }
            if(cast!=null)LegacyWisadel(cast,age,action.From,12,flip:flip,point:LegacyOwnerPoint(actor,action.From));
            if(action.Skill&&skill==1&&!action.ProjectileOnly)
                LegacyWisadel("skill_01_start_02",age,action.From,14,flip:flip,point:LegacyOwnerPoint(actor,action.From));
            // A cancelled/unlaunched cue cannot leave a fake projectile behind.
            if(action.Cancelled||Model.Clock<action.LaunchedAt||Model.Clock>=action.ContactAt||action.ContactAt<=action.LaunchedAt)return;
            // The parent skill action is the mode-start, not an extra seventh
            // shell. Its real child cues exclusively own S1/S3 flight.
            if(action.Skill&&!action.ProjectileOnly)return;
            if(empoweredAuto)return; // Original S2 has no moving main projectile.
            string trail=skill==3?"skill_03_trail_game#9":"skill_01_trail";
            if(!legacyFlightOrigins.TryGetValue(action.Sequence,out var start))legacyFlightOrigins[action.Sequence]=start=LegacyWisadelMuzzle(actor,action);
            var end=FormationFeedbackContactPoint(FormationFeedbackActor(action.TargetId),action.To,.55f);
            float progress=Mathf.Clamp01((float)((Model.Clock-action.LaunchedAt)/(action.ContactAt-action.LaunchedAt)));
            var cell=Vector2.Lerp(action.From,action.To,progress);
            var point=Vector3.Lerp(start,end,progress);
            var beam=LegacyWisadel(trail,(Model.Clock-action.LaunchedAt)*action.Rate,cell,26,point:point,direction:end-start);
            if(beam!=null&&skill==3)
            {
                // The source trail was sampled for a much longer flight. Keep its
                // painted width, but cap the rear extent at the actual launch point.
                Vector3 travelled=Vector3.ProjectOnPlane(point-start,battleCamera.transform.forward);
                float originalRear=beam.sprite.pivot.x/beam.sprite.pixelsPerUnit;
                beam.transform.localScale=new Vector3(Mathf.Clamp(travelled.magnitude/originalRear,.06f,1f),1,1);
            }
        }
        void PresentWisadelShellImpactFace(FormationHit hit,FormationPresentationAction shell,Vector3 point)
        {
            if(hit.Secondary||shell==null||!shell.ProjectileOnly||shell.Cancelled||shell.Animation!="Skill_3_Loop")return;
            float age=(float)(Model.Clock-hit.At);
            // The hit flash leads the cut-in. The large expression opens at the
            // stop point, peaks quickly, then leaves the existing star field visible.
            const float enter=.08f, fade=.34f, end=.45f;
            if(age<enter||age>=end)return;
            wisadelShellFaces??=formationCards.FirstOrDefault(c=>c!=null&&c.CardId==ViviCombat.CardId)?.ComicImpactFaces;
            if(wisadelShellFaces==null||wisadelShellFaces.Count==0)return;
            var sprite=wisadelShellFaces[shell.ProjectileIndex%wisadelShellFaces.Count];
            if(sprite==null)return;
            if(legacyComicMaterial==null)
                legacyComicMaterial=new Material(Shader.Find("Sprites/Default")){name="Formation / approved April comic burst"};
            float opening=Mathf.InverseLerp(enter,enter+.10f,age);
            float height=Mathf.Lerp(1.65f,2.65f,1-Mathf.Pow(1-opening,3));
            height-=.18f*Mathf.InverseLerp(enter+.13f,end,age);
            float side=Vector3.Dot(ActorWorldPoint(shell.To)-ActorWorldPoint(shell.From),battleCamera.transform.right)<0?-1:1;
            float roll=side*Mathf.Lerp(14f,-5f,Mathf.InverseLerp(enter,fade,age));
            var face=LegacyDraw(sprite,legacyComicMaterial,point,hit.To,height/sprite.bounds.size.y,58,roll:roll);
            if(face==null)return;
            face.name="Formation Wisadel / S3 impact face";
            face.color=new Color(1,1,1,1-Mathf.InverseLerp(fade,end,age));
        }
        void PresentOriginalWisadelHit(FormationHit hit,FormationFighter actor,double age)
        {
            int skill=hit.Skill&&hit.Action!=null?WisadelSkillForAction(hit.Action):(Campaign?.WisadelSkill??3);
            string hitKey=hit.Skill?(skill==1?"skill_01_hit":skill==3?"skill_03_hit_game#9":null):
                "skill_02_hit";
            if(hitKey==null)return;
            var contact=FormationFeedbackContactPoint(FormationFeedbackActor(hit.ToId),hit.To,.55f);
            LegacyWisadel(hitKey,age,hit.To,36,point:contact);
            // Source _effectsWhenStop is a ground explosion, while OnHit is the
            // separate target layer. v0.7 has no additional bomb/token damage.
            // AoE hits share one true projectile stop. Do not multiply its ground
            // explosion by the number of victims hit in the same impact.
            var first=Model.Hits.FirstOrDefault(h=>h.FromId==hit.FromId&&h.Skill==hit.Skill&&!h.Heal&&
                (hit.Action!=null?ReferenceEquals(h.Action,hit.Action):Math.Abs(h.At-hit.At)<1e-5));
            if(!ReferenceEquals(first,hit))return;
            var projectile=hit.Action;
            Vector2 center=projectile?.To??hit.To;
            string stop=hit.Skill?(skill==1?"skill_01_hit_02":"skill_03_hit_02_game#9"):
                "skill_02_hit_02";
            LegacyWisadel(stop,age,center,30);
            // The first applied hit is the primary victim of this shell. The
            // same expression stays here through the opening of the explosion.
            if(hit.Skill&&skill==3)PresentWisadelShellImpactFace(hit,projectile,contact);
        }
        static int WisadelSkillForAction(FormationPresentationAction action)
        {
            if(action.Animation?.StartsWith("Skill_1",StringComparison.Ordinal)==true)return 1;
            if(action.Animation?.StartsWith("Skill_2",StringComparison.Ordinal)==true)return 2;
            return 3;
        }
        bool WisadelEmpoweredAt(int actorId,double at)
        {
            // Preserve the real S2 grant interval for already applied hits even
            // after its buff expires. S2's v0.7 grant is exactly 8 simulation sec.
            return Model.PresentationActions.Any(a=>a.ActorId==actorId&&a.Skill&&!a.ProjectileOnly&&!a.Cancelled&&
                a.ContactAt<=at+1e-6&&at<a.ContactAt+8);
        }
        void PresentOriginalWisadelOwner(FormationFighter actor)
        {
            if(!actor.Alive||!WisadelAprilFxArt.Available)return;
            int skill=Campaign?.WisadelSkill??3;double now=Model.Clock;
            if(skill==2)
            {
                double until=Model.LegacyBuffUntil(actor,"wisadel");
                if(until>now)LegacyWisadel("skill_02_buff",(now-(until-8))*LegacyVisualRate,actor.Position,10,true,
                    actor.Facing.x<0,FormationDisplayPoint(actor));
            }
            else if(skill==3)
            {
                var cast=Model.PresentationActions.LastOrDefault(a=>a.ActorId==actor.Id&&a.Skill&&!a.ProjectileOnly);
                // The true parent already exists during Begin/first windup,
                // before its first contact callback assigns BusyUntil.
                bool channel=cast!=null&&!cast.Cancelled&&now>=cast.At&&
                    (now<cast.ContactAt||Model.LegacyWisadelChannelUntil(actor)>now);
                if(channel)
                {
                    LegacyWisadel("skill_03_buff_f",(now-cast.At)*LegacyVisualRate,actor.Position,10,true,
                        actor.Facing.x<0,FormationDisplayPoint(actor));
                    // Before the first child cue is created, the already real
                    // parent mode-start identifies its first Loop start exactly.
                    if(!Model.PresentationActions.Any(a=>a.ParentSequence==cast.Sequence&&a.ProjectileOnly))
                    {
                        double loopAge=(now-cast.At)*cast.Rate-.833333313;
                        LegacyWisadel("skill_03_start_game#9",loopAge,actor.Position,12,flip:actor.Facing.x<0,point:FormationDisplayPoint(actor));
                        var aimed=FormationFeedbackActor(cast.TargetId);
                        if(aimed!=null&&aimed.Alive)
                            LegacyWisadel("skill_03_hit_03_game#9",loopAge,aimed.Position,32,
                                point:FormationFeedbackContactPoint(aimed,aimed.Position,.55f));
                    }
                }
            }
        }
    }
    public sealed partial class FormationBattleModel
    {
        internal double LegacyWisadelChannelUntil(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)?state.BusyUntil:double.NegativeInfinity;
    }
}
