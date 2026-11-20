using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    internal sealed class FormationSuiClawStrike
    {
        internal FormationPresentationAction Cast;
        internal int Wave, LifeVersion, AnchorId;
        internal Vector2 Center, ReturnPoint;
        internal double WarningAt, ImpactAt, LiftAt;
        internal bool Resolved, Landed, Released, Flip;
        internal readonly List<FormationSuiStomp> Stomps = new List<FormationSuiStomp>();
    }

    internal sealed class FormationSuiStomp
    {
        internal double StartAt, EndAt;
        internal readonly List<double> ResolvedContacts = new List<double>();
    }

    public sealed partial class FormationBattleModel
    {
        const double SuiClawWarningSeconds = .72;
        const double SuiClawPlantedSeconds = 6;
        const double SuiClawFinalThreeStarSeconds = 9.25;
        internal const double SuiClawLiftSourceSeconds = .5;
        // Verified OnAttack events in the retained A/B Skill_Loop clips, in source seconds.
        static readonly double[] SuiStompContacts = {44d/60, 68d/60, 116d/60};
        const double SuiStompSourceSeconds = 3;
        const float SuiPeriodicMultiplier = 6;
        readonly List<FormationSuiClawStrike> suiClawStrikes = new List<FormationSuiClawStrike>();

        double SuiClawEnd(FormationSuiClawStrike strike) => strike.LiftAt + SuiClawLiftSourceSeconds / strike.Cast.Rate;

        FormationSuiClawStrike PrepareSuiClawStrike(FormationFighter owner, FormationPresentationAction cast,
            int wave, FormationDefender anchor, double impactAt, double plantedSeconds = SuiClawPlantedSeconds)
        {
            // Casts are cleared by the existing round reset. Physical damage still
            // goes through the original skill transaction and its ordered queue.
            suiClawStrikes.RemoveAll(strike => SuiClawEnd(strike) < Clock ||
                !presentationActions.Contains(strike.Cast));
            var cue = new FormationSuiClawStrike
            {
                Cast=cast, Wave=wave, LifeVersion=State(owner).LifeVersion,
                AnchorId=anchor?.Id??-1, Center=anchor?.Position??owner.Position,
                WarningAt=Clock, ImpactAt=impactAt, LiftAt=impactAt+plantedSeconds, Flip=owner.Facing.x<0
            };
            suiClawStrikes.Add(cue);
            return cue;
        }

        void SettleSuiClawStrike(FormationFighter owner, FormationSuiClawStrike strike, Vector2 center, bool landed)
        {
            strike.Center = center;
            strike.LiftAt += Clock - strike.ImpactAt;
            strike.ImpactAt = Clock;
            strike.Resolved = true;
            strike.Landed = landed;
            if (!landed) return;
            // Treat the visible claw as the real fighter during the hold. Existing
            // guard range, hit points, shields and death use that same position.
            strike.ReturnPoint = owner.Position;
            owner.Position = center;
            ScheduleOwned(owner, strike.LiftAt, () => ReleaseSuiClaw(owner, strike));
        }

        void ReleaseSuiClaw(FormationFighter owner, FormationSuiClawStrike strike)
        {
            if (strike.Released) return;
            strike.Released = true;
            owner.Position = strike.ReturnPoint;
        }

        void ReleaseSuiClawsOnClear()
        {
            foreach (var owner in Units.Where(unit => unit.ArtId == "B21" && unit.Alive))
            {
                var strike = ActiveSuiClawStrike(owner);
                if (strike == null || !strike.Landed || strike.Released) continue;
                // Keep the contact readable, then let a cleared board proceed to the
                // gate from the approach point, using the same path as other fighters.
                strike.LiftAt = Math.Min(strike.LiftAt, Math.Max(Clock, strike.ImpactAt + .45));
                ReleaseSuiClaw(owner, strike);
            }
        }

        void ScheduleSuiClawLanding(FormationFighter owner, FormationPresentationAction cast,
            Vector2 openingCenter, int wave, double impactAt, double plantedSeconds, Action<FormationSuiClawStrike> onLand)
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
                    cue=PrepareSuiClawStrike(owner,cast,wave,anchor,impactAt,plantedSeconds);
            });
            ScheduleOwned(owner, impactAt, () =>
            {
                if (Phase != FormationPhase.Fighting) return;
                var victims = Defenders.Where(defender => defender.Alive).ToArray();
                if(victims.Length>0)
                {
                    // Keep the warned guard when it actually takes the hit. If
                    // it died during descent, use the nearest actual victim.
                    var center=cue?.Center??openingCenter;
                    var anchor=victims.FirstOrDefault(defender=>defender.Id==cue?.AnchorId)??
                        victims.OrderBy(defender=>(defender.Position-center).sqrMagnitude).ThenBy(defender=>defender.Id).First();
                    if(cue==null)
                        cue=PrepareSuiClawStrike(owner,cast,wave,anchor,impactAt,plantedSeconds);
                    SettleSuiClawStrike(owner,cue,anchor.Position,true);
                    NativeSegment(owner,victims,owner.Attack*SuiPeriodicMultiplier,0,true,0,true);
                    onLand(cue);
                }
                else if(cue!=null)
                    SettleSuiClawStrike(owner,cue,cue.Center,false);
            });
        }

        void ScheduleSuiStomp(FormationFighter owner, FormationSuiClawStrike strike, double firstContact)
        {
            double start = firstContact - SuiStompContacts[0] / strike.Cast.Rate;
            var stomp = new FormationSuiStomp { StartAt=start, EndAt=start+SuiStompSourceSeconds/strike.Cast.Rate };
            strike.Stomps.Add(stomp);
            foreach (double contact in SuiStompContacts)
            {
                ScheduleOwned(owner, start + contact / strike.Cast.Rate, () =>
                {
                    if (Phase != FormationPhase.Fighting || strike.Released || RemainingDefenders == 0) return;
                    stomp.ResolvedContacts.Add(Clock);
                    NativeSegment(owner,Defenders,owner.Attack*SuiPeriodicMultiplier/SuiStompContacts.Length,0,true,0,true);
                });
            }
        }

        void StartSuiClawSkill(FormationFighter owner, FormationDefender target, FormationSuiClawStrike opening)
        {
            var state = State(owner);
            float chess = Stack(owner,"chess");
            state.Stacks["chess"] = 0;
            double firstContact = Clock;
            float radius = AreaRadius(owner,3);
            var victims = Nearby(target.Position,radius,100,target);
            if (victims.Length==0)
            {
                var next = Defenders.Where(defender=>defender.Alive)
                    .OrderBy(defender=>(defender.Position-target.Position).sqrMagnitude).ThenBy(defender=>defender.Id).FirstOrDefault();
                if (next!=null) victims=Nearby(next.Position,radius,100,next);
            }
            SettleSuiClawStrike(owner,opening,victims.FirstOrDefault()?.Position??target.Position,victims.Length>0);
            NativeSegment(owner,victims,owner.Attack*(18*(1+chess*.3f)),0,true,0,true);
            if (opening.Landed) ScheduleSuiStomp(owner,opening,firstContact+4);

            int lastLanding = owner.Star>=3 ? 16 : 8;
            for (int offset=8; offset<=lastLanding; offset+=8)
            {
                int wave = offset;
                bool longHold = wave==16;
                ScheduleSuiClawLanding(owner,opening.Cast,opening.Center,wave/4,firstContact+wave,
                    longHold?SuiClawFinalThreeStarSeconds:SuiClawPlantedSeconds, strike =>
                    {
                        ScheduleSuiStomp(owner,strike,firstContact+wave+4);
                        if (longHold) ScheduleSuiStomp(owner,strike,firstContact+wave+8);
                    });
            }
            // The cast remains active until the last claw has lifted. The wave
            // anchors still follow the four-second campaign cadence.
            state.BusyUntil = firstContact + lastLanding + (lastLanding==16?SuiClawFinalThreeStarSeconds:SuiClawPlantedSeconds)
                + SuiClawLiftSourceSeconds/opening.Cast.Rate;
            opening.Cast.EndAt = state.BusyUntil;
            ScheduleOwned(owner,state.BusyUntil,() =>
            {
                var yi=Named("B26");
                if(yi!=null&&Level("岁影")>=2)GiveBuff(yi,"yi_duration",4,999);
            });
        }

        internal FormationSuiClawStrike ActiveSuiClawStrike(FormationActor owner)
        {
            for(int index=suiClawStrikes.Count-1;index>=0;index--)
            {
                var strike=suiClawStrikes[index];
                if(strike.Cast.ActorId!=owner.Id || !owner.Alive || strike.LifeVersion!=State(owner).LifeVersion ||
                    strike.Cast.Cancelled || !presentationActions.Contains(strike.Cast))
                    continue;
                if(strike.Resolved)
                {
                    if(strike.Landed && Clock<SuiClawEnd(strike))
                        return strike;
                }
                else if(Phase==FormationPhase.Fighting && Clock<strike.ImpactAt+StepSeconds)
                    return strike;
            }
            return null;
        }

        Vector2 CampaignRiderPosition(FormationFighter carrier)
        {
            // A passenger stays on traversable ground during the claw's temporary
            // manifestation, even if the claw lands on an elevated guard tile.
            var strike = carrier.ArtId=="B21" ? ActiveSuiClawStrike(carrier) : null;
            return strike?.Landed==true && !strike.Released ? strike.ReturnPoint : carrier.Position;
        }
    }

    public sealed partial class FormationBattleController
    {
        const string SuiClawDescription = "显现：落爪对半径3格内守军造成1800%攻击力物伤。落地停留6秒并可被攻击。每4秒触发一波全场物伤，" +
            "重落爪为600%，「撼山河」连踏3次、每次200%。一二星3波，三星6波且最后一次停留9.25秒。无普攻。";
        // Retain the existing gameplay contact clock. The landing body instead
        // comes from Bombard, which transitions into the original planted idle.
        const string SuiClawMotion = "Skill_Loop_A";
        const string SuiClawBegin = "Skill_Begin_A";
        const string SuiClawIdle = "Skill_Idle_A";
        const string SuiClawEnd = "Skill_End_A";
        const string SuiClawImpactArt = "enemy_dyshhj_skill_01_hit_01";
        const string SuiStompRangeArt = "enemy_dyshhj_skill_02_range_01";
        const string SuiStompImpactArt = "enemy_dyshhj_skill_02_hit_01";
        const double SuiClawAirbornePose = 3d / 60;
        const double SuiClawDescentSeconds = .44;
        const float SuiClawCombatHeight = 3.8f;

        bool PresentFormationSuiBody(FormationActor actor, SpriteRenderer body)
        {
            if(Campaign==null || !CombatViewActive || actor.ArtId!="B21")
                return false;

            var claw=Model.ActiveSuiClawStrike(actor);
            formationLabelHeights[actor.Id]=claw?.Landed==true&&!claw.Released ? 1.35f : .44f;
            body.enabled=false;
            return true;
        }

        void DrawSuiClawBody(FormationActor actor,FormationSuiClawStrike cue,FormationSuiStomp stomp)
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
            bool loop=false;
            if(Model.Clock>=cue.LiftAt)
            {
                motion=SuiClawEnd;
                sourceAge=(Model.Clock-cue.LiftAt)*cue.Cast.Rate;
            }
            else if(stomp!=null)
            {
                motion=SuiClawMotion;
                sourceAge=(Model.Clock-stomp.StartAt)*cue.Cast.Rate;
            }
            else if(beginAge>=beginDuration)
            {
                motion=SuiClawIdle;
                sourceAge=beginAge-beginDuration;
                loop=true;
            }
            if(!source.WindowReady(motion,sourceAge,loop))
            {
                var request="B21/claw/"+motion;
                if(warmingFormationWindows.Add(request))
                    StartCoroutine(WarmFormationWindow(source,motion,sourceAge,loop,request));
            }
            var frame=source.Frame(motion,sourceAge,loop);
            if(frame==null)return;

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
                var cue=Model.ActiveSuiClawStrike(actor);
                if(actor.Alive && shadow!=null && FormationPointVisibleInView(actor.Position) &&
                    (cue==null || !cue.Landed || cue.Released))
                    DrawSuiClawShadow(actor.Id+"/sui/anchor",shadow,actor.Position,.92f,new Color(.19f,.26f,.27f,.48f));

                if(cue==null || !FormationPointVisibleInView(cue.Center))
                    continue;
                var stomp=cue.Stomps.FirstOrDefault(candidate=>Model.Clock>=candidate.StartAt && Model.Clock<candidate.EndAt);
                DrawSuiClawBody(actor,cue,stomp);
                double age=Model.Clock-cue.ImpactAt;
                if(shadow!=null)
                {
                    float anticipation=Mathf.InverseLerp((float)cue.WarningAt,(float)cue.ImpactAt,(float)Model.Clock);
                    float pressure=Mathf.Clamp01((float)(age/.20));
                    float fade=1-Mathf.Clamp01((float)((Model.Clock-cue.LiftAt)*cue.Cast.Rate/FormationBattleModel.SuiClawLiftSourceSeconds));
                    DrawSuiClawShadow(actor.Id+"/sui/impact",shadow,cue.Center,2.15f+anticipation*1.10f-pressure*.40f,
                        new Color(.12f,.10f,.08f,(.48f+.47f*anticipation-.33f*pressure)*fade));
                }
                if(cue.Resolved && cue.Landed)
                {
                    DrawSuiClawLanding(cue,age);
                    if(stomp!=null && Model.Clock<cue.LiftAt)
                        DrawSuiStompRange(cue,stomp);
                    DrawSuiStompImpacts(cue);
                }
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
                formationEffects.Show(hit,1d/30+age*cue.Cast.Rate,foot,battleCamera,cue.Flip,false,
                    Vector3.right,false,23,0,.72f,order);
        }

        void DrawSuiStompRange(FormationSuiClawStrike cue,FormationSuiStomp stomp)
        {
            var range=BossRosterFxArt.Get(BossRosterFxArt.Key("B21",SuiStompRangeArt));
            if(!range.Sequence.Ready)return;
            var point=ActorWorldPoint(cue.Center)+Vector3.up*.05f;
            int order=5000-Mathf.RoundToInt(cue.Center.y*50);
            formationEffects.Show(range,(Model.Clock-stomp.StartAt)*cue.Cast.Rate,point,battleCamera,cue.Flip,false,
                Vector3.right,false,23,0,.72f,order);
        }

        void DrawSuiStompImpacts(FormationSuiClawStrike cue)
        {
            var impact=BossRosterFxArt.Get(BossRosterFxArt.Key("B21",SuiStompImpactArt));
            if(!impact.Sequence.Ready)return;
            foreach(var hit in Model.Hits)
            {
                if(hit.Action!=cue.Cast || hit.Heal || !hit.Skill || !FormationPointVisibleInView(hit.To) ||
                    !cue.Stomps.Any(stomp=>stomp.ResolvedContacts.Contains(hit.At)))continue;
                var point=FormationFeedbackContactPoint(FormationFeedbackActor(hit.ToId),hit.To,.45f);
                if(formationEffects.Show(impact,1d/30+(Model.Clock-hit.At)*cue.Cast.Rate,point,battleCamera,cue.Flip,
                    false,Vector3.right,false,24,0,.72f))
                    formationImpactOwners.Add(hit);
            }
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
