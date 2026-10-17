using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Small cutout rig for this prototype's authored character, not a general animation framework.</summary>
    public sealed class DemoWalkPuppet : IDisposable
    {
        private sealed class Part
        {
            public Transform Bone;
            public Vector2 Pivot;
            public Mesh Mesh;
            public MeshRenderer Renderer;
        }

        private sealed class Rig
        {
            public Transform Root;
            public readonly List<Part> Parts = new List<Part>();
            public Part Head, Body, Tail, ArmL, HandL, ArmR, HandR, ThighL, BootL, ShoeL, ThighR, BootR, ShoeR;
            public Vector2 HipL, KneeL, AnkleL, FootL, HipR, KneeR, AnkleR, FootR;
            public float Ppu;
        }

        private readonly Transform parent;
        private readonly Material material;
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

        public DemoWalkPuppet(Transform owner, Sprite frontSprite, Sprite backSprite, Sprite sideSprite, Sprite quarterSprite)
        {
            parent = owner;
            material = new Material(Shader.Find("Sprites/Default")) { name = "Demo Puppet (runtime)" };
            front = CreateRig(frontSprite, false);
            back = CreateRig(backSprite, true);
            side = CreateTurnRig(sideSprite, false);
            rearQuarter = CreateTurnRig(quarterSprite, true);
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
            float bob = 0.012f * Mathf.Cos(phase * Mathf.PI * 4);
            float lateral = Mathf.Clamp01(strideAxis.x);
            float crouch = rig == front ? Mathf.Lerp(0.085f, 0.065f, lateral) : 0.085f;
            rig.Root.localPosition = new Vector3(0, (-crouch + bob) * gaitWeight, 0);
            PoseLeg(rig, true, phase);
            PoseLeg(rig, false, Mathf.Repeat(phase + 0.5f, 1));
            Rotate(rig.Head, -wave * 1.4f);
            float pivot = Mathf.Sin(turnProgress * Mathf.PI) * Mathf.Sign(turnTarget - turnFrom);
            Rotate(rig.Body, wave * 1.1f + pivot * 2);
            Rotate(rig.Tail, Mathf.Sin((phase - 0.15f) * Mathf.PI * 2) * 7 * gaitWeight);
            Rotate(rig.ArmL, wave * 17);
            Rotate(rig.HandL, -5 * gaitWeight - Mathf.Max(0, wave) * 13);
            Rotate(rig.ArmR, -wave * 17);
            Rotate(rig.HandR, 5 * gaitWeight + Mathf.Max(0, -wave) * 13);
            for (int i = 0; i < rig.Parts.Count; i++)
            {
                rig.Parts[i].Renderer.enabled = rig.Parts[i] == rig.Head ? headVisible : bodyVisible;
                rig.Parts[i].Renderer.sortingOrder = sortingOrder + i;
                rig.Parts[i].Renderer.sharedMaterial.color = tint;
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

        private Rig CreateTurnRig(Sprite sprite, bool quarter)
        {
            var rig = new Rig { Ppu = sprite.pixelsPerUnit * 1374f / sprite.texture.height };
            rig.Root = new GameObject(quarter ? "Rear quarter cutout rig" : "Side cutout rig").transform;
            rig.Root.SetParent(parent, false);
            Vector2 anchor = new Vector2(720, 1335);
            rig.Tail = Cut(rig, sprite, anchor, "Tail", new Vector2(650,1080),
                190,965, 430,965, 450,1045, 650,1045, 680,1125, 520,1215, 275,1230, 200,1115);
            if (quarter)
            {
                rig.ArmR = Cut(rig,sprite,anchor,"Far upper arm",new Vector2(597,934), 545,885, 630,885, 660,945, 607,1008, 537,990);
                rig.HandR = Cut(rig,sprite,anchor,"Far forearm",new Vector2(564,1000), 530,965, 605,987, 615,1030, 581,1090, 540,1090, 501,1055);
                rig.ThighR = Cut(rig,sprite,anchor,"Far thigh",new Vector2(660,1090), 610,1050, 713,1068, 710,1150, 665,1210, 574,1180);
                rig.BootR = Cut(rig,sprite,anchor,"Far boot",new Vector2(615,1175), 553,1140, 682,1155, 701,1255, 570,1295, 535,1255);
                rig.ShoeR = Cut(rig,sprite,anchor,"Far shoe",new Vector2(615,1250), 541,1230, 690,1225, 704,1308, 670,1330, 533,1315);
                rig.ThighL = Cut(rig,sprite,anchor,"Near thigh",new Vector2(770,1090), 710,1050, 820,1035, 838,1145, 817,1200, 729,1190);
                rig.BootL = Cut(rig,sprite,anchor,"Near boot",new Vector2(785,1177), 718,1150, 834,1150, 855,1245, 822,1295, 719,1275);
                rig.ShoeL = Cut(rig,sprite,anchor,"Near shoe",new Vector2(790,1255), 718,1230, 844,1225, 881,1285, 867,1325, 717,1325);
                rig.Body = Cut(rig,sprite,anchor,"Torso",new Vector2(720,1070), 598,822, 733,810, 818,853, 816,940, 818,1045, 781,1107, 695,1085, 650,1100, 575,1070, 610,1000, 615,943, 569,919);
                rig.ArmL = Cut(rig,sprite,anchor,"Near upper arm",new Vector2(812,918), 764,870, 832,850, 881,932, 871,982, 807,1008, 775,953);
                rig.HandL = Cut(rig,sprite,anchor,"Near forearm",new Vector2(854,984), 821,960, 883,958, 914,1015, 914,1063, 862,1084, 818,1058, 802,1010);
                rig.Head = Cut(rig,sprite,anchor,"Head and hair",new Vector2(720,845), 0,0, 1145,0, 1145,820, 901,820, 847,852, 752,819, 650,856, 550,900, 0,925);
                rig.HipL=Pixel(rig,anchor,new Vector2(770,1090)); rig.KneeL=Pixel(rig,anchor,new Vector2(785,1177)); rig.AnkleL=Pixel(rig,anchor,new Vector2(790,1255)); rig.FootL=Pixel(rig,anchor,new Vector2(800,1315));
                rig.HipR=Pixel(rig,anchor,new Vector2(660,1090)); rig.KneeR=Pixel(rig,anchor,new Vector2(615,1175)); rig.AnkleR=Pixel(rig,anchor,new Vector2(615,1250)); rig.FootR=Pixel(rig,anchor,new Vector2(620,1315));
            }
            else
            {
                rig.ArmR = Cut(rig,sprite,anchor,"Far upper arm",new Vector2(796,928), 760,865, 820,870, 844,968, 790,1010, 763,969);
                rig.HandR = Cut(rig,sprite,anchor,"Far forearm",new Vector2(797,1004), 773,974, 832,975, 839,1098, 784,1107, 765,1058);
                rig.ThighR = Cut(rig,sprite,anchor,"Far thigh",new Vector2(775,1100), 736,1063, 809,1075, 799,1189, 721,1201, 710,1145);
                rig.BootR = Cut(rig,sprite,anchor,"Far boot",new Vector2(750,1180), 703,1150, 796,1151, 802,1265, 711,1290);
                rig.ShoeR = Cut(rig,sprite,anchor,"Far shoe",new Vector2(760,1250), 707,1228, 801,1229, 846,1284, 840,1330, 702,1328);
                rig.ThighL = Cut(rig,sprite,anchor,"Near thigh",new Vector2(715,1100), 640,1047, 754,1069, 751,1130, 701,1210, 604,1188);
                rig.BootL = Cut(rig,sprite,anchor,"Near boot",new Vector2(665,1180), 588,1151, 716,1167, 723,1250, 698,1305, 582,1278);
                rig.ShoeL = Cut(rig,sprite,anchor,"Near shoe",new Vector2(655,1250), 585,1227, 714,1229, 758,1295, 745,1351, 567,1343, 563,1294);
                rig.Body = Cut(rig,sprite,anchor,"Torso",new Vector2(720,1070), 653,810, 776,791, 831,858, 832,945, 818,989, 836,1043, 800,1113, 713,1114, 649,1060, 671,987, 675,914);
                rig.ArmL = Cut(rig,sprite,anchor,"Near upper arm",new Vector2(648,920), 596,850, 693,855, 718,930, 684,987, 577,970, 558,922);
                rig.HandL = Cut(rig,sprite,anchor,"Near forearm",new Vector2(622,987), 581,961, 682,982, 667,1020, 651,1091, 589,1115, 531,1080, 540,1020);
                rig.Head = Cut(rig,sprite,anchor,"Head and hair",new Vector2(720,840), 0,0, 1145,0, 1145,820, 905,820, 855,835, 790,795, 720,800, 660,845, 570,875, 0,925);
                rig.HipL=Pixel(rig,anchor,new Vector2(715,1100)); rig.KneeL=Pixel(rig,anchor,new Vector2(665,1180)); rig.AnkleL=Pixel(rig,anchor,new Vector2(655,1250)); rig.FootL=Pixel(rig,anchor,new Vector2(680,1335));
                rig.HipR=Pixel(rig,anchor,new Vector2(775,1100)); rig.KneeR=Pixel(rig,anchor,new Vector2(750,1180)); rig.AnkleR=Pixel(rig,anchor,new Vector2(760,1250)); rig.FootR=Pixel(rig,anchor,new Vector2(790,1315));
            }
            rig.HandL.Bone.SetParent(rig.ArmL.Bone, true);
            rig.HandR.Bone.SetParent(rig.ArmR.Bone, true);
            return rig;
        }

        private Rig CreateRig(Sprite sprite, bool rear)
        {
            var rig = new Rig { Ppu = sprite.pixelsPerUnit * 1374f / sprite.texture.height };
            rig.Root = new GameObject(rear ? "Rear cutout rig" : "Front cutout rig").transform;
            rig.Root.SetParent(parent, false);
            // Coordinates describe this character's source canvas (top-left origin), not gameplay data.
            Vector2 anchor = new Vector2(680, 1335);
            if (rear)
            {
                anchor = new Vector2(615,1335);
                rig.Tail = Cut(rig,sprite,anchor,"Tail",new Vector2(590,1080), 190,980, 395,980, 465,1110, 610,1050, 650,1120, 510,1225, 300,1225, 210,1140);
                rig.ArmR = Cut(rig,sprite,anchor,"Far upper arm",new Vector2(515,920), 449,870, 552,872, 569,940, 523,995, 443,981);
                rig.HandR = Cut(rig,sprite,anchor,"Far forearm",new Vector2(489,986), 454,965, 520,981, 513,1020, 532,1054, 507,1097, 460,1111, 416,1070, 431,1020);
                rig.ThighR = Cut(rig,sprite,anchor,"Far thigh",new Vector2(572,1095), 524,1063, 610,1083, 621,1135, 594,1200, 510,1200, 508,1140);
                rig.BootR = Cut(rig,sprite,anchor,"Far boot",new Vector2(558,1183), 490,1162, 597,1162, 613,1274, 476,1274, 472,1230);
                rig.ShoeR = Cut(rig,sprite,anchor,"Far shoe",new Vector2(549,1260), 480,1237, 612,1237, 615,1338, 476,1338, 472,1280);
                rig.ThighL = Cut(rig,sprite,anchor,"Near thigh",new Vector2(667,1095), 618,1063, 715,1060, 724,1140, 710,1195, 630,1195, 615,1140);
                rig.BootL = Cut(rig,sprite,anchor,"Near boot",new Vector2(672,1177), 624,1157, 716,1157, 730,1265, 617,1265);
                rig.ShoeL = Cut(rig,sprite,anchor,"Near shoe",new Vector2(677,1247), 620,1231, 724,1225, 762,1260, 755,1318, 637,1325, 617,1277);
                rig.Body = Cut(rig,sprite,anchor,"Torso",new Vector2(615,1075), 537,849, 662,849, 728,935, 714,1000, 719,1090, 625,1114, 590,1087, 514,1100, 507,1000, 508,940);
                rig.ArmL = Cut(rig,sprite,anchor,"Near upper arm",new Vector2(705,931), 669,870, 737,883, 780,957, 745,1008, 692,990, 660,935);
                rig.HandL = Cut(rig,sprite,anchor,"Near forearm",new Vector2(747,991), 718,966, 783,981, 789,1014, 814,1044, 814,1090, 785,1110, 740,1084, 716,1044);
                rig.Head = Cut(rig,sprite,anchor,"Head and hair",new Vector2(615,850), 0,0, 1145,0, 1145,960, 930,960, 935,880, 800,890, 780,865, 711,862, 700,850, 560,850, 514,878, 425,910, 0,960);
                rig.HandL.Bone.SetParent(rig.ArmL.Bone,true);
                rig.HandR.Bone.SetParent(rig.ArmR.Bone,true);
                rig.HipL=Pixel(rig,anchor,new Vector2(667,1095)); rig.KneeL=Pixel(rig,anchor,new Vector2(672,1177)); rig.FootL=Pixel(rig,anchor,new Vector2(696,1310));
                rig.HipR=Pixel(rig,anchor,new Vector2(572,1095)); rig.KneeR=Pixel(rig,anchor,new Vector2(558,1183)); rig.FootR=Pixel(rig,anchor,new Vector2(548,1328));
                rig.AnkleL=Pixel(rig,anchor,new Vector2(677,1247)); rig.AnkleR=Pixel(rig,anchor,new Vector2(549,1260));
                return rig;
            }
            rig.Tail = Cut(rig, sprite, anchor, "Tail", new Vector2(800, 1100),
                785,1093, 843,1080, 910,1000, 1065,1000, 1065,1130, 970,1220, 840,1220, 782,1160);
            Cut(rig, sprite, anchor, "Coat flap", new Vector2(525, 980), 485,940, 553,962, 539,1060, 491,1130, 420,1130, 433,1065);
            rig.ArmR = Cut(rig, sprite, anchor, "Far upper arm", new Vector2(775,958), 762,930, 805,950, 831,1004, 795,1031, 755,1003);
            rig.HandR = Cut(rig, sprite, anchor, "Far forearm", new Vector2(809,1012), 795,998, 836,1007, 864,1040, 864,1094, 825,1103, 785,1061);
            rig.ThighR = Cut(rig, sprite, anchor, "Far thigh", new Vector2(732,1101), 690,1080, 782,1080, 800,1141, 784,1193, 700,1193, 687,1143);
            rig.BootR = Cut(rig, sprite, anchor, "Far boot", new Vector2(744,1175), 699,1157, 792,1157, 800,1260, 688,1260, 688,1230);
            rig.ShoeR = Cut(rig,sprite,anchor,"Far shoe",new Vector2(747,1243), 698,1228, 800,1225, 864,1260, 864,1324, 730,1324, 688,1288);
            rig.ThighL = Cut(rig, sprite, anchor, "Near thigh", new Vector2(633,1097), 580,1075, 680,1094, 655,1184, 627,1204, 544,1180, 550,1140);
            rig.BootL = Cut(rig, sprite, anchor, "Near boot", new Vector2(589,1177), 529,1150, 635,1173, 646,1220, 630,1280, 492,1280, 492,1260, 507,1190);
            rig.ShoeL = Cut(rig,sprite,anchor,"Near shoe",new Vector2(562,1265), 490,1245, 638,1245, 650,1305, 635,1348, 482,1348);
            rig.Body = Cut(rig, sprite, anchor, "Torso", new Vector2(680,1080), 605,837, 746,840, 789,937, 768,981, 798,1033, 785,1114, 704,1120, 678,1094, 652,1116, 568,1090, 587,1022, 592,989, 575,946);
            rig.ArmL = Cut(rig, sprite, anchor, "Near upper arm", new Vector2(575,914), 522,863, 592,870, 620,934, 578,990, 490,987, 469,956, 490,910);
            rig.HandL = Cut(rig, sprite, anchor, "Near forearm", new Vector2(529,987), 492,970, 567,984, 556,1020, 572,1044, 560,1090, 534,1118, 486,1118, 460,1095, 465,1057, 478,1022);
            rig.Head = Cut(rig, sprite, anchor, "Head and hair", new Vector2(680,850), 0,0, 1145,0, 1145,970, 954,970, 954,870, 870,880, 815,845, 610,845, 552,860, 552,920, 414,950, 0,960);
            rig.HandL.Bone.SetParent(rig.ArmL.Bone, true);
            rig.HandR.Bone.SetParent(rig.ArmR.Bone, true);
            rig.HipL = Pixel(rig, anchor, new Vector2(633,1097));
            rig.KneeL = Pixel(rig, anchor, new Vector2(589,1177));
            rig.FootL = Pixel(rig, anchor, new Vector2(570,1330));
            rig.HipR = Pixel(rig, anchor, new Vector2(732,1101));
            rig.KneeR = Pixel(rig, anchor, new Vector2(744,1175));
            rig.FootR = Pixel(rig, anchor, new Vector2(780,1310));
            rig.AnkleL = Pixel(rig,anchor,new Vector2(562,1265)); rig.AnkleR=Pixel(rig,anchor,new Vector2(747,1243));
            return rig;
        }

        private static Vector2 Pixel(Rig rig, Vector2 anchor, Vector2 point) => new Vector2(point.x - anchor.x, anchor.y - point.y) / rig.Ppu;

        private Part Cut(Rig rig, Sprite sprite, Vector2 anchor, string name, Vector2 pivot, params float[] coordinates)
        {
            int count = coordinates.Length / 2;
            var vertices = new Vector3[count];
            var uv = new Vector2[count];
            var polygon = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                Vector2 source = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
                vertices[i] = Pixel(rig, pivot, source);
                uv[i] = new Vector2(source.x / 1145f, 1 - source.y / 1374f);
                polygon.Add(vertices[i]);
            }
            var mesh = new Mesh { name = "Cutout " + name, vertices = vertices, uv = uv };
            mesh.triangles = Triangulate(polygon);
            mesh.RecalculateBounds();
            var bone = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
            bone.SetParent(rig.Root, false);
            bone.localPosition = Pixel(rig, anchor, pivot);
            bone.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = bone.GetComponent<MeshRenderer>();
            // Each view keeps its texture; one instance material per view is disposed with the rig.
            renderer.sharedMaterial = new Material(material) { mainTexture = sprite.texture };
            var part = new Part { Bone = bone, Pivot = pivot, Mesh = mesh, Renderer = renderer };
            rig.Parts.Add(part);
            return part;
        }

        private static int[] Triangulate(List<Vector2> points)
        {
            var indices = new List<int>();
            for (int i = 0; i < points.Count; i++) indices.Add(i);
            float area = 0;
            for (int i = 0; i < points.Count; i++) area += Cross(points[i], points[(i + 1) % points.Count]);
            if (area < 0) indices.Reverse();
            var result = new List<int>();
            int guard = points.Count * points.Count;
            while (indices.Count > 2 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int a = indices[(i + indices.Count - 1) % indices.Count], b = indices[i], c = indices[(i + 1) % indices.Count];
                    if (Cross(points[b] - points[a], points[c] - points[b]) <= 0) continue;
                    bool occupied = false;
                    foreach (int p in indices)
                        if (p != a && p != b && p != c && Cross(points[b]-points[a], points[p]-points[a]) >= 0 && Cross(points[c]-points[b], points[p]-points[b]) >= 0 && Cross(points[a]-points[c], points[p]-points[c]) >= 0) { occupied = true; break; }
                    if (occupied) continue;
                    result.Add(a); result.Add(b); result.Add(c); indices.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new InvalidOperationException("Invalid puppet cutout polygon.");
            }
            return result.ToArray();
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public void Dispose()
        {
            foreach (Rig rig in new[] { front, back, side, rearQuarter })
            {
                foreach (Part part in rig.Parts)
                {
                    if (part.Mesh != null) UnityEngine.Object.Destroy(part.Mesh);
                    if (part.Renderer != null) UnityEngine.Object.Destroy(part.Renderer.sharedMaterial);
                }
                if (rig.Root != null) UnityEngine.Object.Destroy(rig.Root.gameObject);
            }
            UnityEngine.Object.Destroy(material);
        }
    }
}
