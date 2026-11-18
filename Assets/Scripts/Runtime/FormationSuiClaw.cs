using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    internal sealed class FormationSuiClawStrike
    {
        internal FormationPresentationAction Cast;
        internal int Wave, LifeVersion;
        internal Vector2 Center;
        internal double WarningAt, ImpactAt;
        internal bool Resolved, Landed;
    }

    public sealed partial class FormationBattleModel
    {
        const double SuiClawWarningSeconds = .72;
        internal const double SuiClawTailSeconds = .60;
        readonly List<FormationSuiClawStrike> suiClawStrikes = new List<FormationSuiClawStrike>();

        FormationSuiClawStrike PrepareSuiClawStrike(FormationFighter owner, FormationPresentationAction cast,
            int wave, Vector2 center, double impactAt)
        {
            // Casts are cleared by the existing round reset. Physical damage still
            // goes through the original skill transaction and its ordered queue.
            suiClawStrikes.RemoveAll(strike => strike.ImpactAt + SuiClawTailSeconds < Clock ||
                !presentationActions.Contains(strike.Cast));
            var cue = new FormationSuiClawStrike
            {
                Cast=cast, Wave=wave, LifeVersion=State(owner).LifeVersion,
                Center=center, WarningAt=Clock, ImpactAt=impactAt
            };
            suiClawStrikes.Add(cue);
            return cue;
        }

        void SettleSuiClawStrike(FormationSuiClawStrike strike, Vector2 center, bool landed)
        {
            strike.Center = center;
            strike.ImpactAt = Clock;
            strike.Resolved = true;
            strike.Landed = landed;
        }

        void ScheduleSuiClawWave(FormationFighter owner, FormationPresentationAction cast,
            Vector2 openingCenter, int wave, double impactAt)
        {
            FormationSuiClawStrike cue = null;
            int life = State(owner).LifeVersion;
            Schedule(impactAt - SuiClawWarningSeconds, () =>
            {
                if(Phase!=FormationPhase.Fighting || !owner.Alive || State(owner).LifeVersion!=life)
                    return;
                var anchor = Defenders.Where(defender => defender.Alive)
                    .OrderBy(defender => (defender.Position-openingCenter).sqrMagnitude)
                    .ThenBy(defender => defender.Id).FirstOrDefault();
                if(anchor!=null)
                    cue=PrepareSuiClawStrike(owner,cast,wave,anchor.Position,impactAt);
            });
            ScheduleOwned(owner, impactAt, () =>
            {
                var victims = Defenders.Where(defender => defender.Alive).ToArray();
                if(victims.Length>0)
                {
                    if(cue==null)
                        cue=PrepareSuiClawStrike(owner,cast,wave,victims[0].Position,impactAt);
                    SettleSuiClawStrike(cue,cue.Center,true);
                }
                else if(cue!=null)
                    SettleSuiClawStrike(cue,cue.Center,false);
                NativeSegment(owner,victims,owner.Attack*6,0,true,0,true);
            });
        }

        internal FormationSuiClawStrike ActiveSuiClawStrike(FormationActor owner)
        {
            for(int index=suiClawStrikes.Count-1;index>=0;index--)
            {
                var strike=suiClawStrikes[index];
                if(strike.Cast.ActorId!=owner.Id || strike.Cast.Cancelled || !presentationActions.Contains(strike.Cast))
                    continue;
                if(strike.Resolved)
                {
                    if(strike.Landed && Clock<strike.ImpactAt+SuiClawTailSeconds)
                        return strike;
                }
                else if(owner.Alive && strike.LifeVersion==State(owner).LifeVersion && Clock<strike.ImpactAt+StepSeconds)
                    return strike;
            }
            return null;
        }
    }

    public sealed partial class FormationBattleController
    {
        const string SuiClawMotion = "Skill_Loop_A";
        const string SuiClawImpactArt = "enemy_dyshhj_skill_02_hit_01";
        const string SuiClawGroundArt = "enemy_dyshhj_skill_02_range_01";
        // The source ground clip contains three pulses. Use the final pulse's
        // first bright frame and its decay for this one gameplay impact.
        const double SuiClawGroundContact = 60d / 30;
        const double SuiClawDescentSeconds = .44;
        const double SuiClawPlantedSeconds = .20;
        const float SuiClawCombatHeight = 3.8f;

        bool PresentFormationSuiBody(FormationActor actor, SpriteRenderer body)
        {
            if(Campaign==null || !CombatViewActive || actor.ArtId!="B21")
                return false;

            // Keep identification anchored to the actual unit. The claw can land
            // on a distant target while the real unit is being blocked or hit.
            formationLabelHeights[actor.Id]=.44f;
            body.enabled=false;
            return true;
        }

        void DrawSuiClawDescent(FormationActor actor,FormationSuiClawStrike cue)
        {
            if(!actor.Alive || !formationMonsterArt.TryGetValue(actor.ArtId,out var source))
                return;
            double age=Model.Clock-cue.ImpactAt;
            double fallTime=Math.Min(SuiClawDescentSeconds,Math.Max(.001,cue.ImpactAt-cue.WarningAt));
            if(age < -fallTime)
                return;
            var contact=source.ContactTime(SuiClawMotion);
            if(float.IsNaN(contact))
                return;
            // Follow the authored first stroke at source speed. Keep its planted
            // pose for the recoil rather than sampling the clip's next strike.
            var strokeAge=age*cue.Cast.Rate;
            var sourceAge=contact+Math.Min(strokeAge,.15);
            if(!source.WindowReady(SuiClawMotion,sourceAge,false) && warmingFormationWindows.Add("B21/claw"))
                StartCoroutine(WarmFormationWindow(source,SuiClawMotion,sourceAge,false,"B21/claw"));
            var frame=source.Frame(SuiClawMotion,sourceAge,false);
            if(frame==null)
                return;

            float fall=Mathf.Clamp01((float)(1+age/fallTime));
            float retreat=Mathf.Clamp01((float)((age-SuiClawPlantedSeconds)/(FormationBattleModel.SuiClawTailSeconds-SuiClawPlantedSeconds)));
            float lift=4.65f*(1-fall*fall)+3.6f*retreat*retreat;
            float scale=Mathf.Min(ActorPresentationHeight(actor),SuiClawCombatHeight)/source.Height;
            bool flip=actor.Facing.x<0;
            // During the four authored frames following contact, the middle talon
            // extends from (-50,-13) to (-28,-38) around the source pivot.
            float toeExtension=Mathf.Clamp01((float)(strokeAge/(4d/60)));
            float toeX=Mathf.Lerp(50,28,toeExtension)*scale/frame.pixelsPerUnit;
            float toeY=Mathf.Lerp(13,38,toeExtension)*scale/frame.pixelsPerUnit;
            var root=ActorWorldPoint(cue.Center)+Vector3.up*(lift+.04f)+
                battleCamera.transform.right*(flip ? -toeX : toeX)+battleCamera.transform.up*toeY;
            var body=SuiClawRenderer(actor.Id+"/sui/descent","Sui falling claw");
            body.sharedMaterial=actorMaterial;
            body.enabled=true;
            body.sprite=frame;
            body.flipX=flip;
            body.transform.SetPositionAndRotation(root,battleCamera.transform.rotation);
            body.transform.localScale=Vector3.one*scale;
            body.sortingOrder=5022-Mathf.RoundToInt(cue.Center.y*50);
            body.color=new Color(1,1,1,Mathf.SmoothStep(.22f,1,fall*5)*(1-retreat*retreat));
        }

        void PresentFormationSuiClawEffects()
        {
            if(Campaign==null || !CombatViewActive)
                return;
            foreach(var actor in Model.Units)
            {
                if(actor.ArtId!="B21" || !actor.Enabled || !bodies.TryGetValue(actor.Id,out var body))
                    continue;
                var contactShadow=body.GetComponent<ActorGrounding>();
                var shadow=contactShadow?.Shadow?.sprite;
                if(actor.Alive && shadow!=null && FormationPointVisibleInView(actor.Position))
                    DrawSuiClawShadow(actor.Id+"/sui/anchor",shadow,actor.Position,.92f,new Color(.19f,.26f,.27f,.48f));

                var cue=Model.ActiveSuiClawStrike(actor);
                if(cue==null || !FormationPointVisibleInView(cue.Center))
                    continue;
                DrawSuiClawDescent(actor,cue);
                double age=Model.Clock-cue.ImpactAt;
                if(shadow!=null && age<.20)
                {
                    float anticipation=Mathf.InverseLerp((float)cue.WarningAt,(float)cue.ImpactAt,(float)Model.Clock);
                    float fade=1-Mathf.Clamp01((float)(age/.20));
                    DrawSuiClawShadow(actor.Id+"/sui/impact",shadow,cue.Center,2.15f+anticipation*1.10f,
                        new Color(.12f,.10f,.08f,(.48f+.47f*anticipation)*fade));
                }
                if(cue.Resolved && cue.Landed)
                    DrawSuiClawLanding(actor,cue.Center,age);
            }
        }

        void DrawSuiClawLanding(FormationActor actor,Vector2 center,double age)
        {
            var ground=BossRosterFxArt.Get(BossRosterFxArt.Key("B21",SuiClawGroundArt));
            var hit=BossRosterFxArt.Get(BossRosterFxArt.Key("B21",SuiClawImpactArt));
            var foot=ActorWorldPoint(center)+Vector3.up*.04f;
            int order=5000-Mathf.RoundToInt(center.y*50);
            double sourceAge=age*FormationVisualRate;
            bool flip=actor.Facing.x<0;
            if(ground.Sequence.Ready)
                formationEffects.Show(ground,SuiClawGroundContact+sourceAge,foot,battleCamera,flip,false,Vector3.right,
                    false,-5,0,.38f,order,ground:true);
            if(hit.Sequence.Ready)
                formationEffects.Show(hit,sourceAge,foot+battleCamera.transform.up*.16f,battleCamera,flip,false,
                    Vector3.right,false,4,0,1.2f,order);
        }

        void DrawSuiClawShadow(string key, Sprite sprite, Vector2 cell, float width, Color color)
        {
            var view=SuiClawRenderer(key,"Sui claw shadow");
            view.sprite=sprite;
            view.enabled=true;
            view.transform.SetPositionAndRotation(ActorWorldPoint(cell)+Vector3.up*.016f,Quaternion.Euler(90,0,0));
            view.transform.localScale=new Vector3(width,width*.64f,1);
            view.sortingOrder=4988-Mathf.RoundToInt(cell.y*50);
            view.color=color;
        }
        SpriteRenderer SuiClawRenderer(string key,string label)
        {
            if(!formationAuxBodies.TryGetValue(key,out var view) || view==null)
            {
                view=new GameObject(label,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                view.transform.SetParent(actorRoot,false);
                formationAuxBodies[key]=view;
            }
            return view;
        }
    }
}
