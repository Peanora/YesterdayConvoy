using UnityEngine;

namespace YesterdayConvoy
{
    // Client-area dimensions exclude the OS title bar and resize border.
    public static class WindowAspectGeometry
    {
        const float Aspect=16f/9;
        public static Vector2Int Fit(int width,int height)
        {
            width=Mathf.Max(1,width);height=Mathf.Max(1,height);
            return width*9L<=height*16L?new Vector2Int(width,Mathf.Max(1,Mathf.FloorToInt(width/Aspect))):new Vector2Int(Mathf.Max(1,Mathf.FloorToInt(height*Aspect)),height);
        }
        public static RectInt Resize(RectInt proposal,Vector2Int frame,int edge,Vector2Int current)
        {
            int w=Mathf.Max(1,proposal.width-frame.x),h=Mathf.Max(1,proposal.height-frame.y);
            bool corner=edge==4||edge==5||edge==7||edge==8;
            bool fromHeight=edge==3||edge==6||corner&&Mathf.Abs(h-current.y)/(float)Mathf.Max(1,current.y)>Mathf.Abs(w-current.x)/(float)Mathf.Max(1,current.x);
            if(fromHeight){h=Mathf.Max(360,h);w=Mathf.RoundToInt(h*Aspect);}
            else {w=Mathf.Max(640,w);h=Mathf.RoundToInt(w/Aspect);}
            int width=w+frame.x,height=h+frame.y;
            bool left=edge==1||edge==4||edge==7,top=edge==3||edge==4||edge==5;
            return new RectInt(left?proposal.xMax-width:proposal.x,top?proposal.yMax-height:proposal.y,width,height);
        }
    }
}
