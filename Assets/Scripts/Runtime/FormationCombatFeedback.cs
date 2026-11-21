using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Chessboard feedback at a real release or contact. The rest of the game's
    // battle presenters keep their own art and sound paths.
    public sealed partial class FormationBattleController
    {
        internal enum ImpactStyle { Slash, Bite, Bolt, Fire, Shield, Blast, Magic, Heal, FireRain }
        readonly HashSet<FormationHit> formationImpactOwners = new HashSet<FormationHit>();
        readonly Dictionary<int, FormationHit> formationContactReactions = new Dictionary<int, FormationHit>();
        readonly HashSet<int> formationPulsingBodies = new HashSet<int>();
        MaterialPropertyBlock formationContactProperties;
        static readonly int FormationContactPulse = Shader.PropertyToID("_ContactPulse");

        FormationActor FormationFeedbackActor(int id)
        {
            foreach (var actor in Model.Units) if (actor.Id == id) return actor;
            foreach (var actor in Model.Defenders) if (actor.Id == id) return actor;
            foreach (var actor in Model.Summons) if (actor.Id == id) return actor;
            return null;
        }

        Vector3 FormationFeedbackContactPoint(FormationActor actor, Vector2 position, float bodyFraction)
        {
            float height = actor == null ? 1.2f : Mathf.Clamp(ActorPresentationHeight(actor), .65f, 3.3f);
            Vector3 foot = ActorWorldPoint(position);
            if (actor != null) foot += FormationDisplayPoint(actor) - ActorWorldPoint(actor.Position);
            return foot + battleCamera.transform.up * (height * bodyFraction) + Vector3.up * .025f;
        }

        internal ImpactStyle FormationHitStyle(FormationHit hit)
        {
            if (hit.Heal) return ImpactStyle.Heal;
            if (hit.Absorbed && hit.Amount < .001f) return ImpactStyle.Shield;
            string id = hit.SourceArtId ?? FormationFeedbackActor(hit.FromId)?.ArtId;
            switch (id)
            {
                case "D27":
                    return ImpactStyle.Magic;
                case "SLUG": case "M02": case "M03": case "P14H": case "B14": case "M35":
                    return hit.Skill && id == "B14" ? ImpactStyle.Blast : ImpactStyle.Bite;
                case "M08": case "M11": case "M16": case "D07": case "GOLEM":
                    return ImpactStyle.Bolt;
                case "TALULAH_TRIAL":
                    return IsFormationTalulahRainContact(hit) ? ImpactStyle.FireRain : ImpactStyle.Fire;
                case "M30": case "M31": case "B08": case "B24":
                    return ImpactStyle.Fire;
                case "M10": case "M18": case "M20": case "M27": case "M28": case "M29": case "M33":
                    return ImpactStyle.Shield;
                case "WISADEL_APRIL": case "B01": case "B05": case "B21": case "B23": case "B25": case "M42":
                    return hit.Skill || id == "B21" || id == "M42" ? ImpactStyle.Blast : ImpactStyle.Bolt;
                case "B03": case "B04": case "B22": case "M13": case "M21": case "M24": case "M34":
                    return ImpactStyle.Slash;
            }
            if (hit.Action != null && hit.Action.ContactAt > hit.Action.LaunchedAt + .001)
                return ImpactStyle.Bolt;
            return FormationCampaignRules.Find(id)?.DamageType == "法" ? ImpactStyle.Magic : ImpactStyle.Slash;
        }

        internal bool IsAudibleFormationHit(FormationHit hit) => !hit.Secondary || hit.Killed || hit.Absorbed;

        void CollectFormationContactReactions()
        {
            formationContactReactions.Clear();
            if (Campaign != null || Model.Phase == FormationPhase.Preparation) return;
            foreach (var hit in Model.Hits)
            {
                double age = Model.Clock - hit.At;
                if (hit.Heal || age < 0 || age >= .15 || !IsAudibleFormationHit(hit) || hit.Amount <= 0 && !hit.Absorbed) continue;
                if (!formationContactReactions.TryGetValue(hit.ToId, out var previous) ||
                    hit.At > previous.At || hit.At == previous.At && hit.Amount >= previous.Amount)
                    formationContactReactions[hit.ToId] = hit;
            }
        }

        void ApplyFormationContactReaction(FormationActor actor, SpriteRenderer body, bool frozen = false)
        {
            formationContactReactions.TryGetValue(actor.Id, out var hit);
            bool active = !frozen && body.enabled && hit != null;
            if (!active && !formationPulsingBodies.Remove(actor.Id)) return;
            Vector4 pulse = Vector4.zero;
            if (active && hit != null)
            {
                formationPulsingBodies.Add(actor.Id);
                float remaining = 1 - (float)(Model.Clock - hit.At) / .15f;
                bool absorbed = hit.Absorbed && hit.Amount < .001f;
                pulse = absorbed ? new Vector4(.56f, .81f, 1f, .18f) : new Vector4(1f, .85f, .72f,
                    hit.ShowDamageNumber || hit.Killed ? .30f : hit.Skill ? .22f : .13f);
                pulse.w *= remaining * remaining;
            }
            formationContactProperties ??= new MaterialPropertyBlock();
            body.GetPropertyBlock(formationContactProperties);
            formationContactProperties.SetVector(FormationContactPulse, pulse);
            body.SetPropertyBlock(formationContactProperties);
        }

        void DrawFormationContactFeedback()
        {
            if (Model == null || Model.Phase == FormationPhase.Preparation || !lineMaterial.SetPass(0)) return;
            double now = Model.Clock;
            GL.Begin(GL.TRIANGLES);
            foreach (var hit in Model.Hits)
            {
                if (hit.At > now || now-hit.At > .27 || !FormationPointVisibleInView(hit.To)) continue;
                string artId = hit.SourceArtId ?? FormationFeedbackActor(hit.FromId)?.ArtId;
                // The riftbeast's actual contact gets a brief directional cut in
                // addition to its existing source effects. Other authored impact
                // layers own their attacks without getting a duplicate flash.
                if (formationImpactOwners.Contains(hit) && artId != "B22" &&
                    (artId != "WISADEL_APRIL" || UsesApprovedWisadelOrdinaryImpact(hit))) continue;
                if (hit.Secondary && !hit.Killed && !hit.Absorbed) continue;
                float age = (float)(now - hit.At);
                var style = FormationHitStyle(hit);
                var target = FormationFeedbackActor(hit.ToId);
                Vector3 center = FormationFeedbackContactPoint(target,hit.To,hit.Heal ? .68f : .56f);
                DrawContactShape(hit,style,center,age,artId == "B22");
            }
            GL.End();
        }

        void DrawContactShape(FormationHit hit,ImpactStyle style,Vector3 center,float age,bool riftbeast)
        {
            Vector3 right = battleCamera.transform.right;
            Vector3 up = battleCamera.transform.up;
            Vector3 toward = ActorWorldPoint(hit.To)-ActorWorldPoint(hit.From);
            Vector2 direction = new Vector2(Vector3.Dot(right,toward),Vector3.Dot(up,toward));
            direction = direction.sqrMagnitude<.001f ? Vector2.right : direction.normalized;
            Vector3 along = right*direction.x+up*direction.y;
            Vector3 across = -right*direction.y+up*direction.x;
            float power = hit.Skill ? 1.18f : 1;
            float alpha = Mathf.Clamp01(1-age/.23f);
            if(alpha<=0)return;
            switch (style)
            {
                case ImpactStyle.Slash:
                    int strokes = riftbeast ? 3 : 1;
                    for(int i=0;i<strokes;i++)
                    {
                        float angle = (i-1)*.24f;
                        Vector3 axis = (along+across*angle).normalized;
                        Vector3 sweep = (across-along*angle).normalized;
                        var color = riftbeast ? new Color(1,.22f,.18f,alpha*.82f) : new Color(.99f,.86f,.70f,alpha*.65f);
                        Crescent(center+sweep*(i-(strokes-1)*.5f)*.15f,axis,sweep,(riftbeast?.55f:.46f)*power,.16f*power,age,color);
                    }
                    break;
                case ImpactStyle.Bite:
                    var fang = new Color(1,.83f,.69f,alpha*.73f);
                    float opening = Mathf.Lerp(.29f,.04f,Mathf.Clamp01(age/.105f));
                    ContactTriangle(center+across*opening-along*.29f,center+across*(opening+.07f)+along*.1f,center+along*.27f,fang);
                    ContactTriangle(center-across*opening-along*.29f,center-across*(opening+.07f)+along*.1f,center+along*.27f,fang);
                    break;
                case ImpactStyle.Bolt:
                    ContactSpark(center,along,across,.49f*power,.09f,new Color(1,.85f,.47f,alpha*.80f),age);
                    break;
                case ImpactStyle.Shield:
                    FeedbackRing(center,right,up,.14f+age*1.9f,.052f,new Color(.70f,.89f,1,alpha*.65f),6);
                    ContactSpark(center,along,across,.36f,.07f,new Color(.88f,.96f,1,alpha*.48f),age);
                    break;
                case ImpactStyle.Fire:
                case ImpactStyle.FireRain:
                    ContactSpark(center,up,along,.58f*power,.16f,new Color(1,.32f,.10f,alpha*.65f),age);
                    ContactSpark(center+right*.14f,up,along,.41f*power,.13f,new Color(1,.76f,.23f,alpha*.60f),age);
                    break;
                case ImpactStyle.Blast:
                    FeedbackRing(center,along,across,(.12f+age*2.4f)*power,.08f,new Color(1,.66f,.32f,alpha*.66f),10);
                    ContactSpark(center,along,across,.4f,.15f,new Color(1,.9f,.58f,alpha*.57f),age);
                    break;
                case ImpactStyle.Heal:
                    for(int i=-1;i<=1;i++)
                        ContactSpark(center+right*(i*.18f)+up*(.30f*age),up,right,.28f,.055f,new Color(.55f,1,.69f,alpha*.65f),age);
                    break;
                default:
                    FeedbackRing(center,right,up,.18f+age*1.6f,.057f,new Color(.75f,.58f,1,alpha*.68f),4);
                    ContactSpark(center,up,right,.41f,.10f,new Color(.92f,.78f,1,alpha*.65f),age);
                    break;
            }
        }

        static void ContactSpark(Vector3 center,Vector3 along,Vector3 across,float length,float width,Color color,float age)
        {
            Vector3 front = center + along * length * (.52f+age*2.3f);
            Vector3 tail = center - along * length * .45f;
            ContactTriangle(front,center+across*width,tail,color);
            ContactTriangle(front,tail,center-across*width,color);
        }

        static void FeedbackRing(Vector3 center,Vector3 right,Vector3 up,float radius,float width,Color color,int sides)
        {
            Vector3 previous = right;
            for (int i=1;i<=sides;i++)
            {
                float angle = i*(2*Mathf.PI/sides);
                Vector3 next = right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                ContactQuad(center+previous*radius,center+next*radius,center+next*(radius-width),center+previous*(radius-width),color);
                previous=next;
            }
        }

        static void Crescent(Vector3 center,Vector3 along,Vector3 across,float length,float width,float age,Color color)
        {
            Vector3 tail = center - across*length + along*.12f;
            Vector3 outer = tail,inner=tail;
            for(int i=1;i<=8;i++)
            {
                float fraction=i/8f;
                float taper=Mathf.Sin(fraction*Mathf.PI);
                Vector3 path = center+across*Mathf.Lerp(-length,length,fraction)+along*(.12f+taper*(.18f+age*.28f));
                Vector3 edge = path - along * width * taper;
                ContactQuad(outer,path,edge,inner,color);
                outer=path;
                inner=edge;
            }
        }

        static void ContactTriangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        { GL.Color(color);GL.Vertex(a);GL.Vertex(b);GL.Vertex(c); }

        static void ContactQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        { ContactTriangle(a,b,c,color);ContactTriangle(a,c,d,color); }
    }
}
