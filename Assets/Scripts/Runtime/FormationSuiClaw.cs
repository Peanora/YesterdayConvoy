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
        internal bool Resolved, Landed, Flip;
    }

    public sealed partial class FormationBattleModel
    {
        const double SuiClawWarningSeconds = .72;
        internal const double SuiClawPlantedSeconds = 1.20;
        internal const double SuiClawTailSeconds = SuiClawPlantedSeconds + .25;
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
                Center=center, WarningAt=Clock, ImpactAt=impactAt, Flip=owner.Facing.x<0
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
        // Retain the existing gameplay contact clock. The landing body instead
        // comes from Bombard, which transitions into the original planted idle.
        const string SuiClawMotion = "Skill_Loop_A";
        const string SuiClawBegin = "Skill_Begin_A";
        const string SuiClawIdle = "Skill_Idle_A";
        const string SuiClawEnd = "Skill_End_A";
        const string SuiClawImpactArt = "enemy_dyshhj_skill_01_hit_01";
        const double SuiClawAirbornePose = 3d / 60;
        const double SuiClawDescentSeconds = .44;
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
            if(!formationMonsterArt.TryGetValue(actor.ArtId,out var source))
                return;
            double age=Model.Clock-cue.ImpactAt;
            double fallTime=Math.Min(SuiClawDescentSeconds,Math.Max(.001,cue.ImpactAt-cue.WarningAt));
            if(age < -fallTime)
                return;
            var contact=source.ContactTime(SuiClawBegin);
            if(float.IsNaN(contact))
                return;
            double beginAge=contact+age*cue.Cast.Rate;
            double beginDuration=source.Duration(SuiClawBegin);
            string motion=SuiClawBegin;
            double sourceAge=Math.Max(SuiClawAirbornePose,beginAge);
            if(age>=FormationBattleModel.SuiClawPlantedSeconds)
            {
                motion=SuiClawEnd;
                sourceAge=(age-FormationBattleModel.SuiClawPlantedSeconds)*FormationVisualRate;
            }
            else if(beginAge>=beginDuration)
            {
                motion=SuiClawIdle;
                sourceAge=beginAge-beginDuration;
            }
            var frame=source.Frame(motion,sourceAge,false);
            if(frame==null)
            {
                var request="B21/claw/"+motion;
                if(warmingFormationWindows.Add(request))
                    StartCoroutine(WarmFormationWindow(source,motion,sourceAge,false,request));
                return;
            }

            // The source takes over the last approach from the airborne silhouette,
            // presses into its grounded pose, and supplies its own lift-off.
            double sourceApproach=(contact-SuiClawAirbornePose)/cue.Cast.Rate;
            float approach=Mathf.Clamp01((float)((age+fallTime)/Math.Max(.001,fallTime-sourceApproach)));
            float lift=4.65f*(1-approach*approach);
            float scale=Mathf.Min(ActorPresentationHeight(actor),SuiClawCombatHeight)/source.Height;
            // At contact and throughout the planted idle, the middle talon lies
            // about 28 pixels left and 36 below the shared atlas pivot.
            float toeX=28f*scale/frame.pixelsPerUnit;
            float toeY=36f*scale/frame.pixelsPerUnit;
            var root=ActorWorldPoint(cue.Center)+Vector3.up*(lift+.04f)+
                battleCamera.transform.right*(cue.Flip ? -toeX : toeX)+battleCamera.transform.up*toeY;
            var body=SuiClawRenderer(actor.Id+"/sui/descent","Sui falling claw");
            body.sharedMaterial=actorMaterial;
            body.enabled=true;
            body.sprite=frame;
            body.flipX=cue.Flip;
            body.transform.SetPositionAndRotation(root,battleCamera.transform.rotation);
            body.transform.localScale=Vector3.one*scale;
            body.sortingOrder=5022-Mathf.RoundToInt(cue.Center.y*50);
            body.color=Color.white;
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
                if(shadow!=null)
                {
                    float anticipation=Mathf.InverseLerp((float)cue.WarningAt,(float)cue.ImpactAt,(float)Model.Clock);
                    float pressure=Mathf.Clamp01((float)(age/.20));
                    float fade=1-Mathf.InverseLerp((float)FormationBattleModel.SuiClawPlantedSeconds,
                        (float)FormationBattleModel.SuiClawTailSeconds,(float)age);
                    DrawSuiClawShadow(actor.Id+"/sui/impact",shadow,cue.Center,2.15f+anticipation*1.10f-pressure*.40f,
                        new Color(.12f,.10f,.08f,(.48f+.47f*anticipation-.33f*pressure)*fade));
                }
                if(cue.Resolved && cue.Landed)
                    DrawSuiClawLanding(cue,age);
            }
        }

        void DrawSuiClawLanding(FormationSuiClawStrike cue,double age)
        {
            var hit=BossRosterFxArt.Get(BossRosterFxArt.Key("B21",SuiClawImpactArt));
            var foot=ActorWorldPoint(cue.Center)+Vector3.up*.04f;
            int order=5000-Mathf.RoundToInt(cue.Center.y*50);
            // The first sample is blank. Keep the full visible contact burst and
            // its dust tail at the ground anchor that the talon actually touches.
            if(hit.Sequence.Ready)
                formationEffects.Show(hit,1d/30+age*FormationVisualRate,foot,battleCamera,cue.Flip,false,
                    Vector3.right,false,23,0,.72f,order);
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
