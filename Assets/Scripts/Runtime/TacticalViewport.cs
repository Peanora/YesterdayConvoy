using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>One 16:9 canvas for drawing, pointer positions and camera projection.</summary>
    public readonly struct TacticalViewport
    {
        public readonly float Width,Height,Scale;
        public readonly Vector2 Offset;
        public static TacticalViewport Current=>new TacticalViewport(Screen.width,Screen.height);
        public TacticalViewport(float width,float height)
        {
            Width=Mathf.Max(1,width);Height=Mathf.Max(1,height);
            Scale=Mathf.Min(Width/1280f,Height/720f);
            Offset=new Vector2((Width-1280*Scale)/2,(Height-720*Scale)/2);
        }
        public Matrix4x4 Matrix=>Matrix4x4.TRS(new Vector3(Offset.x,Offset.y,0),Quaternion.identity,new Vector3(Scale,Scale,1));
        public Vector2 FromTopLeft(Vector2 point)=>(point-Offset)/Scale;
        public Vector2 FromScreen(Vector2 point)=>FromTopLeft(new Vector2(point.x,Height-point.y));
        public Vector2 ToScreen(Vector2 point)=>new Vector2(Offset.x+point.x*Scale,Height-Offset.y-point.y*Scale);
        // Normalized camera rect uses bottom-left coordinates; layout uses top-left coordinates.
        public Rect CameraRect(Rect layout)=>new Rect((Offset.x+layout.x*Scale)/Width,(Height-Offset.y-layout.yMax*Scale)/Height,layout.width*Scale/Width,layout.height*Scale/Height);
        public void DrawBars()
        {
            Color old=GUI.color;GUI.color=Color.black;
            if(Offset.x>.01f){GUI.DrawTexture(new Rect(0,0,Offset.x,Height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Width-Offset.x,0,Offset.x,Height),Texture2D.whiteTexture);}
            if(Offset.y>.01f){GUI.DrawTexture(new Rect(0,0,Width,Offset.y),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Height-Offset.y,Width,Offset.y),Texture2D.whiteTexture);}
            GUI.color=old;
        }
    }
}
