using System;
using UnityEngine;
using Rig = YesterdayConvoy.BipedVisualRig.View;
using Part = YesterdayConvoy.BipedVisualRig.Part;

namespace YesterdayConvoy
{
    /// <summary>Distance-driven biped poses over authored prefab parts; owns no runtime art or materials.</summary>
    public sealed class DemoWalkPuppet
    {
        private readonly Transform parent;
        private readonly Rig front, back, side, rearQuarter;
        private Rig active;
        private Vector2 leftContact, rightContact;
        private int facing;
        private float turnStarted = -1, lastTime, movementBlend, heading, turnFrom, turnTarget, turnProgress = 1;
        private bool initialized;
        private Vector2 strideAxis;
        private float gaitWeight;
        public const float Stride = 0.64f;
        public Vector3 LeftFoot => parent.TransformPoint(leftContact);
        public Vector3 RightFoot => parent.TransformPoint(rightContact);
        public bool ShowingBack => active == back;
        public int Facing => facing;
        public float MovementWeight => gaitWeight;
        public const float MaxLegStretch = 1.04f;
        public const float TurnDuration = 0.38f;
        public float Heading => heading;
        public bool IsTurning => turnProgress < 1;
        public string ViewName => active.Root.name;

        public void Reset()
        {
            initialized = false;
            movementBlend = gaitWeight = 0;
            parent.localScale = Vector3.one;
        }

        public void Hide()
        {
            front.Root.gameObject.SetActive(false);
            back.Root.gameObject.SetActive(false);
            side.Root.gameObject.SetActive(false);
            rearQuarter.Root.gameObject.SetActive(false);
        }

        public DemoWalkPuppet(Transform owner, BipedVisualRig rig)
        {
            if (rig == null || !rig.IsComplete) throw new ArgumentException("Biped visual prefab has incomplete authored parts.", nameof(rig));
            rig.PrepareHeadGeometry();
            parent = owner;
            front = rig.Front; back = rig.Back; side = rig.Side; rearQuarter = rig.RearQuarter;
            active = front;
        }

        public void Pose(float distance, float clock, Vector3 direction, bool moving, Color tint, int sortingOrder)
        {
            int requested = Mathf.Abs(direction.z) > Mathf.Abs(direction.x)
                ? direction.z > 0 ? 2 : 0 : direction.x < 0 ? 3 : 1;
            if (clock < lastTime) Reset();
            float delta = initialized ? Mathf.Max(0, clock - lastTime) : Mathf.Max(0, clock);
            Vector3 projected = parent.InverseTransformDirection(direction);
            // Depth travel is foreshortened on the billboard. A full lateral stride overextends
            // the shorter front leg and alternately clamps one shoe, producing a limp.
            Vector2 desiredAxis = new Vector2(Mathf.Abs(projected.x), projected.y * 0.35f);
            if (!initialized)
            {
                facing = requested;
                turnStarted = -1;
                heading = turnFrom = turnTarget = FacingAngle(requested);
                turnProgress = 1;
                strideAxis = desiredAxis;
                initialized = true;
            }
            if (requested != facing)
            {
                facing = requested;
                turnFrom = heading;
                turnTarget = heading + Mathf.DeltaAngle(heading, FacingAngle(requested));
                turnStarted = clock;
            }
            turnProgress = turnStarted < 0 ? 1 : Mathf.Clamp01((clock - turnStarted) / TurnDuration);
            heading = Mathf.Lerp(turnFrom, turnTarget, Mathf.SmoothStep(0, 1, turnProgress));
            float headHeading = Mathf.Lerp(turnFrom, turnTarget, Mathf.SmoothStep(0, 1, Mathf.Clamp01(turnProgress / 0.75f)));
            active = SelectView(heading);
            Rig headView = SelectView(headHeading);
            parent.localScale = Vector3.one;
            strideAxis = Vector2.Lerp(strideAxis, desiredAxis, 1 - Mathf.Exp(-delta / 0.045f));
            movementBlend = Mathf.MoveTowards(movementBlend, moving ? 1 : 0, delta / 0.18f);
            gaitWeight = Mathf.SmoothStep(0, 1, movementBlend);
            float phase = Mathf.Repeat(distance / Stride, 1);
            PoseRig(front, headView, headHeading, phase, tint, sortingOrder);
            PoseRig(side, headView, headHeading, phase, tint, sortingOrder);
            PoseRig(rearQuarter, headView, headHeading, phase, tint, sortingOrder);
            PoseRig(back, headView, headHeading, phase, tint, sortingOrder);
            lastTime = clock;
        }

        // Rightward travel keeps the approved three-quarter front pose. Side is a turn key pose.
        private static float FacingAngle(int direction) => direction == 1 ? 30 : direction * 90;

        private Rig SelectView(float angle)
        {
            float absolute = Mathf.Abs(Mathf.DeltaAngle(0, angle));
            return absolute < 48 ? front : absolute < 112 ? side : absolute < 157 ? rearQuarter : back;
        }

        private void PoseRig(Rig rig, Rig headView, float headHeading, float phase, Color tint, int sortingOrder)
        {
            bool bodyVisible = rig == active, headVisible = rig == headView;
            rig.Root.gameObject.SetActive(bodyVisible || headVisible);
            if (!bodyVisible && !headVisible) return;
            float viewAngle = bodyVisible ? heading : headHeading;
            rig.Root.localScale = new Vector3(Mathf.DeltaAngle(0, viewAngle) < 0 ? -1 : 1, 1, 1);
            float wave = Mathf.Sin(phase * Mathf.PI * 2) * gaitWeight;
            float bob = -0.012f * Mathf.Sin(phase * Mathf.PI * 4);
            float lateral = Mathf.Clamp01(strideAxis.x);
            float crouch = rig == front ? Mathf.Lerp(0.085f, 0.065f, lateral) : 0.085f;
            rig.Root.localPosition = new Vector3(0, (-crouch + bob) * gaitWeight, 0);
            PoseLeg(rig, true, phase);
            PoseLeg(rig, false, Mathf.Repeat(phase + 0.5f, 1));
            Rotate(rig.Head, -wave * 1.4f);
            float pivot = Mathf.Sin(turnProgress * Mathf.PI) * Mathf.Sign(turnTarget - turnFrom);
            Rotate(rig.Body, wave * 1.1f + pivot * 2);
            Rotate(rig.Tail, Mathf.Sin((phase - 0.15f) * Mathf.PI * 2) * 7 * gaitWeight);
            // Keep the authored arm/hand pose throughout the walking cycle.
            Rotate(rig.ArmL, 0);
            Rotate(rig.HandL, 0);
            Rotate(rig.ArmR, 0);
            Rotate(rig.HandR, 0);
            for (int i = 0; i < rig.Parts.Count; i++)
            {
                // Serializable Part values are copied on reload; compare Unity object references.
                rig.Parts[i].Renderer.enabled = rig.Parts[i].Renderer == rig.Head.Renderer ? headVisible : bodyVisible;
                rig.Parts[i].Renderer.sortingOrder = sortingOrder + i;
                rig.Parts[i].Renderer.color = tint;
            }
        }

        private void PoseLeg(Rig rig, bool left, float phase)
        {
            Vector2 hip = left ? rig.HipL : rig.HipR;
            Vector2 knee = left ? rig.KneeL : rig.KneeR;
            Vector2 foot = left ? rig.FootL : rig.FootR;
            Vector2 ankle = left ? rig.AnkleL : rig.AnkleR;
            Part thigh = left ? rig.ThighL : rig.ThighR;
            Part boot = left ? rig.BootL : rig.BootR;
            Part shoe = left ? rig.ShoeL : rig.ShoeR;
            float footPitch = 0;
            Vector2 target = foot;
            if (gaitWeight > 0)
            {
                // These short source legs cannot plant for 60% of this fast stride without stretching.
                const float stance = 0.45f;
                // Restore the original front rig's lateral stance; depth travel keeps equal floors.
                float floor = rig == front && !left ? 0.04f * Mathf.Clamp01(strideAxis.x) : 0;
                float x, y = floor - rig.Root.localPosition.y;
                if (phase < stance) x = Stride * (stance * 0.5f - phase);
                else
                {
                    float t = (phase - stance) / (1 - stance);
                    float h = t * t * (3 - 2 * t);
                    x = Mathf.Lerp(-Stride * stance * 0.5f, Stride * stance * 0.5f, h)
                        - Stride * (1 - stance) * (2 * t * t * t - 3 * t * t + t);
                    y += Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2) * 0.075f;
                    footPitch = Mathf.Sin(t * Mathf.PI * 2) * -15 * gaitWeight;
                }
                target = Vector2.Lerp(foot, new Vector2(hip.x + foot.x - ankle.x, y) + strideAxis * x, gaitWeight);
                // Small alternating repositioning steps support the body turn; the route never waits.
                float step = left ? turnProgress * 2 : turnProgress * 2 - 1;
                if (IsTurning && step > 0 && step < 1)
                    target.y += Mathf.Pow(Mathf.Sin(step * Mathf.PI), 2) * 0.035f * gaitWeight;
            }
            Quaternion footRotation = Quaternion.Euler(0,0,footPitch);
            Vector2 targetAnkle = target - (Vector2)(footRotation * (Vector3)(foot - ankle));
            float upper = Vector2.Distance(hip, knee), lower = Vector2.Distance(knee, ankle);
            Vector2 offset = targetAnkle - hip;
            float stretch = Mathf.Clamp(offset.magnitude / (upper + lower) + 0.0001f, 1, MaxLegStretch);
            upper *= stretch; lower *= stretch;
            float reach = Mathf.Clamp(offset.magnitude, Mathf.Abs(upper - lower) + 0.0001f, upper + lower - 0.0001f);
            Vector2 along = offset.normalized;
            // Constrain the endpoint as well as the bones, so a reach limit cannot detach a shoe.
            targetAnkle = hip + along * reach;
            target = targetAnkle + (Vector2)(footRotation * (Vector3)(foot - ankle));
            float cos = Mathf.Clamp((upper * upper + reach * reach - lower * lower) / (2 * upper * reach), -1, 1);
            Vector2 restLeg = ankle - hip;
            float bendSign = Mathf.Sign(restLeg.x * (knee.y - hip.y) - restLeg.y * (knee.x - hip.x));
            Vector2 bend = new Vector2(-along.y, along.x) * bendSign;
            Vector2 solvedKnee = hip + along * (upper * cos) + bend * (upper * Mathf.Sqrt(Mathf.Max(0, 1 - cos * cos)));
            thigh.Bone.localPosition = hip;
            float upperScale = Vector2.Distance(hip,solvedKnee) / Vector2.Distance(hip,knee);
            thigh.Bone.localScale = new Vector3(upperScale, upperScale, 1);
            thigh.Bone.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(knee - hip, solvedKnee - hip));
            boot.Bone.localPosition = solvedKnee;
            float lowerScale = Vector2.Distance(solvedKnee,targetAnkle) / Vector2.Distance(knee,ankle);
            boot.Bone.localScale = new Vector3(lowerScale, lowerScale, 1);
            boot.Bone.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(ankle - knee, targetAnkle - solvedKnee));
            shoe.Bone.localPosition = targetAnkle;
            shoe.Bone.localRotation = footRotation;
            Vector2 contact = target + (Vector2)rig.Root.localPosition;
            contact.x *= rig.Root.localScale.x;
            if (rig == active)
            {
                if (left) leftContact = contact; else rightContact = contact;
            }
        }

        private static void Rotate(Part part, float degrees) => part.Bone.localRotation = Quaternion.Euler(0, 0, degrees);

    }
}
