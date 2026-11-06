using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Fixed geometry and poses from the approved project-external V2 motion study.</summary>
    public static class GuerrillaOrderGeometry
    {
        public const float Width = 1920, Height = 1080;
        public const float ImpactAt = .49f, ShatterAt = .90f, CharactersGoneAt = 2.10f, Duration = 2.60f;
        public static readonly Vector2 Center = new Vector2(960, 520);
        public readonly struct Pane
        {
            public readonly string CardId;
            public readonly float Angle, Radius, Delay, Rotation;
            public readonly Rect Source;
            public readonly Vector2 Anchor;
            internal Pane(string id, float angle, Rect source, Vector2 anchor, float radius, float delay, float rotation)
            { CardId=id; Angle=angle; Source=source; Anchor=anchor; Radius=radius; Delay=delay; Rotation=rotation; }
        }
        static readonly Pane[] panes =
        {
            new Pane("CARD_M15",230,new Rect(90,110,345,280),new Vector2(246,230),.56f,.035f,-7),
            new Pane("CARD_M16",190,new Rect(95,375,455,290),new Vector2(260,490),.55f,.065f,6),
            new Pane("CARD_M03",150,new Rect(145,666,345,255),new Vector2(326,770),.66f,.095f,-6),
            new Pane("CARD_M20",270,new Rect(650,25,500,470),new Vector2(958,255),.66f,0,5),
            new Pane("CARD_M19",310,new Rect(1245,52,400,360),new Vector2(1444,249),.66f,.02f,-6),
            new Pane("CARD_M18",350,new Rect(1120,405,385,230),new Vector2(1298,553),.66f,.05f,7),
            new Pane("CARD_M17",30,new Rect(1270,625,385,315),new Vector2(1520,695),.57f,.08f,-5),
            new Pane(null,70,default,default,.66f,.045f,6),
            new Pane(null,110,default,default,.66f,.055f,-6)
        };
        public static Pane Definition(int slot) => panes[slot];
        public static float Smooth(float v) { v=Mathf.Clamp01(v); return v*v*(3-2*v); }
        public static float Out(float v) => 1-Mathf.Pow(1-Mathf.Clamp01(v),3);
        public static float Random(int seed)
        { double r=Math.Sin(seed*127.1+311.7)*43758.5453123; return (float)(r-Math.Floor(r)); }
        public static float LightRandom(int seed)
        { double r=Math.Sin(seed*72.214+17.23)*43758.5453; return (float)(r-Math.Floor(r)); }
        public static Vector2 Point(float angle, float radius)
        { float a=angle*Mathf.Deg2Rad; return Center+new Vector2(Mathf.Cos(a)*Width*.49f,Mathf.Sin(a)*Height*.48f)*radius; }
        public static Vector2[] Polygon(int slot,float low=.055f,float high=1,float a=-18,float b=18)
        {
            float angle=panes[slot].Angle;
            return new[]{Point(angle+a,low),Point(angle+a,high),Point(angle+b,high),Point(angle+b,low)};
        }
        public static Vector2 Focus(int slot,float time)
        {
            var p=panes[slot]; float drift=Smooth((time-.59f)/.14f);
            return Point(p.Angle,p.Radius)+new Vector2(Mathf.Sin(time*3.1f+slot*1.61f)*1.8f,Mathf.Cos(time*2.7f+slot*.93f)*1.2f)*drift;
        }
        public readonly struct Pose
        {
            public readonly Vector2 Pivot, Offset;
            public readonly float Rotation, Scale, Alpha;
            public readonly bool Visible;
            public Pose(Vector2 pivot,Vector2 offset,float rotation,float scale=1,float alpha=1,bool visible=true)
            { Pivot=pivot; Offset=offset; Rotation=rotation; Scale=scale; Alpha=alpha; Visible=visible; }
            public Vector2 Apply(Vector2 point)
            {
                Vector2 d=(point-Pivot)*Scale; float a=Rotation*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);
                return Pivot+Offset+new Vector2(d.x*c-d.y*s,d.x*s+d.y*c);
            }
        }
        public static Pose Entrance(int slot,float time)
        {
            var p=panes[slot]; float progress=(time-.08f-p.Delay)/.30f,remaining=1-Out(progress),a=p.Angle*Mathf.Deg2Rad;
            float shockAge=time-ImpactAt,shock=shockAge>=0&&shockAge<.1f?Mathf.Sin(shockAge/.1f*Mathf.PI)*5:0;
            return new Pose(Center,new Vector2(Mathf.Cos(a)*(Width*.95f*remaining+shock),Mathf.Sin(a)*(Height*1.08f*remaining+shock)),p.Rotation*remaining,visible:progress>=0);
        }
        public static float BreakAt(int slot) => ShatterAt+panes[slot].Delay*.23f;
        public static Vector2[] Triangle(int slot,int fragment)
        {
            int cell=fragment/2,r=cell/3,a=cell%3;
            float[] radii={.055f,.50f,1},angles={-18,-6,6,18};
            var q=Polygon(slot,radii[r],radii[r+1],angles[a],angles[a+1]);
            return fragment%2==0?new[]{q[0],q[1],q[2]}:new[]{q[0],q[2],q[3]};
        }
        public static Pose Fragment(int slot,int fragment,float time)
        {
            int seed=slot*71+fragment*13+7; float breakAt=BreakAt(slot),delay=Random(seed)*.12f;
            float local=Mathf.Clamp01((time-breakAt-delay)/(CharactersGoneAt-breakAt-delay));
            float erode=Smooth((local-.12f)/.88f),scale=1-erode;
            var triangle=Triangle(slot,fragment); Vector2 pivot=(triangle[0]+triangle[1]+triangle[2])/3;
            float radial=panes[slot].Angle*Mathf.Deg2Rad+(Random(seed+2)-.5f)*.70f;
            float distance=(45+Random(seed+1)*190)*local*local,tangent=(Random(seed+3)-.5f)*140*local;
            return new Pose(pivot,new Vector2(Mathf.Cos(radial)*distance-Mathf.Sin(radial)*tangent,Mathf.Sin(radial)*distance+Mathf.Cos(radial)*tangent),
                (Random(seed+4)-.5f)*.65f*local*Mathf.Rad2Deg,scale,1-Smooth((local-.58f)/.42f),scale>.002f);
        }
        public readonly struct Mapping
        {
            public readonly Vector2 Origin;
            public readonly float Scale;
            public Mapping(int slot,Rect source,Vector2 anchor,float sampleTime)
            {
                var target=Focus(slot,sampleTime); var full=Polygon(slot);
                float loX=float.PositiveInfinity,loY=loX,hiX=float.NegativeInfinity,hiY=hiX;
                foreach(var p in full){loX=Mathf.Min(loX,p.x);loY=Mathf.Min(loY,p.y);hiX=Mathf.Max(hiX,p.x);hiY=Mathf.Max(hiY,p.y);}
                float ax=anchor.x-source.x,ay=anchor.y-source.y;
                Scale=Mathf.Max(Mathf.Max((target.x-loX+4)/Mathf.Max(ax,1),(hiX-target.x+4)/Mathf.Max(source.width-ax,1)),
                    Mathf.Max((target.y-loY+4)/Mathf.Max(ay,1),(hiY-target.y+4)/Mathf.Max(source.height-ay,1)));
                Origin=target-new Vector2(ax,ay)*Scale;
            }
            public Vector2 Uv(Vector2 point,Rect source,Texture texture)
            {
                Vector2 pixel=source.position+(point-Origin)/Scale;
                return new Vector2(pixel.x/texture.width,1-pixel.y/texture.height);
            }
        }
    }
}
