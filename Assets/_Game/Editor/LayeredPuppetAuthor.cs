using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Rig = YesterdayConvoy.BipedVisualRig.View;
using Part = YesterdayConvoy.BipedVisualRig.Part;

namespace YesterdayConvoy.Editor
{
    // Source canvas coordinates are authoring data only. Runtime reads the saved prefab.
    internal sealed class LayeredPuppetAuthor
    {
        private readonly Transform parent;
        private readonly LayeredSpriteLibrary library;
        private readonly Dictionary<Part, Bounds> sourceBounds = new Dictionary<Part, Bounds>();
        public LayeredPuppetAuthor(Transform parent, LayeredSpriteLibrary library) { this.parent = parent; this.library = library; }

        internal sealed class Atlas
        {
            public Texture2D Texture;
            public Color32[] Pixels;
            public Rect[] Regions;
            public int Width, Height;
            public Atlas(string path)
            {
                Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var cpu = new Texture2D(2,2,TextureFormat.RGBA32,false);
                cpu.LoadImage(File.ReadAllBytes(path));
                Width=cpu.width; Height=cpu.height; Pixels=cpu.GetPixels32();
                UnityEngine.Object.DestroyImmediate(cpu);
                var seen = new bool[Pixels.Length];
                var boxes = new List<Rect>();
                var queue = new Queue<int>();
                for (int p=0;p<Pixels.Length;p++)
                {
                    if (seen[p] || Pixels[p].a<32) continue;
                    seen[p]=true; queue.Enqueue(p);
                    int minX=Width,maxX=0,minY=Height,maxY=0,count=0;
                    while(queue.Count>0)
                    {
                        int at=queue.Dequeue(), x=at%Width,y=at/Width; count++;
                        minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
                        if(x>0) Visit(at-1); if(x+1<Width) Visit(at+1);
                        if(y>0) Visit(at-Width); if(y+1<Height) Visit(at+Width);
                    }
                    if(count>500) boxes.Add(Rect.MinMaxRect(minX,minY,maxX+1,maxY+1));
                }
                if(boxes.Count!=12) throw new InvalidOperationException(path+": expected 12 detached components, found "+boxes.Count);
                Regions=boxes.OrderBy(b=>Mathf.Clamp((int)((Height-b.center.y)*3/Height),0,2)).ThenBy(b=>b.xMin).ToArray();
                for(int row=0;row<3;row++)
                    if(boxes.Count(b=>Mathf.Clamp((int)((Height-b.center.y)*3/Height),0,2)==row)!=4)
                        throw new InvalidOperationException(path+": component layout is not 4 by 3.");
                void Visit(int n) { if(!seen[n] && Pixels[n].a>=32) {seen[n]=true;queue.Enqueue(n);} }
            }
            public Vector2 CenterAt(Rect rect,float fraction)
            {
                int y=Mathf.Clamp(Mathf.RoundToInt(rect.yMin+rect.height*fraction),0,Height-1);
                float sum=0,weight=0;
                for(int yy=Math.Max(0,y-2);yy<=Math.Min(Height-1,y+2);yy++)
                    for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                        if(Pixels[yy*Width+x].a>=32) {sum+=x;weight++;}
                return new Vector2(weight>0?sum/weight:rect.center.x,y);
            }
        }

        private Part Cut(Rig rig,Sprite source,Vector2 anchor,string name,Vector2 pivot,params float[] points)
        {
            if(name=="Coat flap") return null; // The new torso already includes the complete coat.
            Transform bone=new GameObject(name).transform;
            bone.SetParent(rig.Root,false); bone.localPosition=Pixel(rig,anchor,pivot);
            var art=new GameObject("Artwork",typeof(SpriteRenderer)).transform;
            art.SetParent(bone,false);
            var part=new Part {Bone=bone,Renderer=art.GetComponent<SpriteRenderer>()};
            rig.Parts.Add(part);
            Vector2[] vertices=new Vector2[points.Length/2];
            for(int i=0;i<vertices.Length;i++) vertices[i]=Pixel(rig,pivot,new Vector2(points[i*2],points[i*2+1]));
            var bounds=new Bounds(vertices[0],Vector3.zero);
            foreach(Vector2 vertex in vertices) bounds.Encapsulate(vertex);
            sourceBounds.Add(part,bounds);
            if(name=="Head and hair")
            {
                Vector2 texturePivot=new Vector2(pivot.x/1145f,1-pivot.y/1374f);
                Sprite head=Sprite.Create(source.texture,new Rect(0,0,source.texture.width,source.texture.height),texturePivot,rig.Ppu,0,SpriteMeshType.FullRect);
                head.name=rig.Root.name+" head";
                // Sprite vertices retain the original texture pixels; no new face is generated.
                for(int i=0;i<vertices.Length;i++)
                    vertices[i]=new Vector2((Mathf.Clamp(points[i*2]/1145f*source.texture.width,0.05f,source.texture.width-0.05f)-texturePivot.x*source.texture.width)/rig.Ppu,
                        (Mathf.Clamp((1-points[i*2+1]/1374f)*source.texture.height,0.05f,source.texture.height-0.05f)-texturePivot.y*source.texture.height)/rig.Ppu);
                rig.HeadVertices = vertices;
                rig.HeadTriangles = Triangulate(vertices.ToList());
                head.OverrideGeometry(vertices.Select(v=>v*rig.Ppu+head.pivot).ToArray(),rig.HeadTriangles.Select(x=>(ushort)x).ToArray());
                AssetDatabase.AddObjectToAsset(head,library);library.AddAuthoredSprite(head);
                part.Renderer.sprite=head;
            }
            return part;
        }

        // Repair existing authored assets without replacing sprites, GUIDs or joint transforms.
        internal static void RestoreHeadOutline(Rig rig)
        {
            float[] points;
            if (rig.Root.name.StartsWith("Front")) points = new float[] {0,0,1145,0,1145,970,954,970,954,870,870,880,815,845,610,845,552,860,552,920,414,950,0,960};
            else if (rig.Root.name.StartsWith("Rear quarter")) points = new float[] {0,0,1145,0,1145,820,901,820,847,852,752,819,650,856,550,900,0,925};
            else if (rig.Root.name.StartsWith("Side")) points = new float[] {0,0,1145,0,1145,820,905,820,855,835,790,795,720,800,660,845,570,875,0,925};
            else points = new float[] {0,0,1145,0,1145,960,930,960,935,880,800,890,780,865,711,862,700,850,560,850,514,878,425,910,0,960};
            Sprite source = rig.Head.Renderer.sprite;
            rig.HeadVertices = new Vector2[points.Length / 2];
            for (int i = 0; i < rig.HeadVertices.Length; i++)
                rig.HeadVertices[i] = new Vector2(Mathf.Clamp(points[2*i] / 1145f * source.texture.width,0.05f,source.texture.width-0.05f) - source.pivot.x,
                    Mathf.Clamp((1 - points[2*i+1] / 1374f) * source.texture.height,0.05f,source.texture.height-0.05f) - source.pivot.y) / rig.Ppu;
            rig.HeadTriangles = Triangulate(rig.HeadVertices.ToList());
        }

        public void Finish(Rig rig,Atlas atlas,bool rear)
        {
            Fit(rig.Body,0,Vector2.zero);
            Fit(rig.ArmL,1,(Vector2)(rig.HandL.Bone.position-rig.ArmL.Bone.position));
            Fit(rig.HandL,2,new Vector2(0,-0.14f));
            Fit(rig.ArmR,3,(Vector2)(rig.HandR.Bone.position-rig.ArmR.Bone.position));
            Fit(rig.HandR,4,new Vector2(0,-0.14f));
            Fit(rig.ThighL,5,rig.KneeL-rig.HipL);
            Fit(rig.BootL,6,rig.AnkleL-rig.KneeL);
            Fit(rig.ShoeL,7,rig.FootL-rig.AnkleL);
            Fit(rig.ThighR,8,rig.KneeR-rig.HipR);
            Fit(rig.BootR,9,rig.AnkleR-rig.KneeR);
            Fit(rig.ShoeR,10,rig.FootR-rig.AnkleR);
            Fit(rig.Tail,11,Vector2.zero);
            rig.Tail.Renderer.flipX=rear;
            void Fit(Part part,int slot,Vector2 joint)
            {
                Rect rect=atlas.Regions[slot];
                Bounds target=sourceBounds[part];
                if(slot==0) rect=new Rect(rect.xMin+rect.width*0.15f,rect.yMin,rect.width*0.7f,rect.height);
                if(slot==6 || slot==9) rect=new Rect(rect.xMin,rect.yMin+rect.height*0.24f,rect.width,rect.height*0.76f);
                Vector2 pivot; float ppu; float rotation=0; Vector3 position=Vector3.zero;
                if(slot==0 || slot==11)
                {
                    pivot=new Vector2(0.5f,0.5f);
                    ppu=rect.height/target.size.y;position=target.center;
                }
                else if(slot==7 || slot==10)
                {
                    Vector2 ground=atlas.CenterAt(rect,0.03f);
                    ppu=rect.height*0.72f/Mathf.Abs(joint.y);
                    Vector2 ankle=ground-new Vector2(joint.x,joint.y)*ppu;
                    pivot=new Vector2((ankle.x-rect.xMin)/rect.width,(ankle.y-rect.yMin)/rect.height);
                }
                else
                {
                    Vector2 proximal=atlas.CenterAt(rect,0.84f), distal=atlas.CenterAt(rect,0.16f);
                    ppu=Vector2.Distance(proximal,distal)/joint.magnitude;
                    pivot=new Vector2((proximal.x-rect.xMin)/rect.width,(proximal.y-rect.yMin)/rect.height);
                    rotation=Vector2.SignedAngle(distal-proximal,joint);
                }
                // Native sub-sprites keep one atlas, individual pivots/PPU and closed overlap ends.
                Sprite sprite=Sprite.Create(atlas.Texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);
                sprite.name=rig.Root.name+" "+part.Bone.name;
                AssetDatabase.AddObjectToAsset(sprite,library);library.AddAuthoredSprite(sprite);
                part.Renderer.sprite=sprite;
                part.Renderer.transform.localPosition=position;
                part.Renderer.transform.localRotation=Quaternion.Euler(0,0,rotation);
            }
        }

        public Rig CreateTurnRig(Sprite sprite, bool quarter)
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

        public Rig CreateRig(Sprite sprite, bool rear)
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

    }
}
