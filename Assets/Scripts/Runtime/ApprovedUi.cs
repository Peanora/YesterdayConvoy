using UnityEngine;

namespace YesterdayConvoy
{
    // V5 uses the existing 1280x720 input space. The approved browser artwork was 1920x1080.
    internal static class ApprovedUi
    {
        internal static readonly Color Paper=new Color(.902f,.922f,.933f,.96f);
        internal static readonly Color Ink=new Color(.145f,.20f,.26f);
        internal static readonly Color Muted=new Color(.38f,.46f,.53f);
        internal static readonly Color Gold=new Color(.88f,.77f,.45f);
        internal static readonly Color Rule=new Color(.66f,.74f,.79f);
        static Font regular,medium,bold;
        static int scrollDragControl;
        static Vector2 scrollDragOrigin, scrollDragPosition;
        internal static Font Font(Font fallback,int weight=400)
        {
            if(regular==null)regular=Resources.Load<Font>("UI/YesterdayConvoyUI-Regular");
            if(medium==null)medium=Resources.Load<Font>("UI/YesterdayConvoyUI-Semibold");
            if(bold==null)bold=Resources.Load<Font>("UI/YesterdayConvoyUI-Bold");
            return (weight>=650?bold:weight>=500?medium:regular)??fallback;
        }
        internal static GUIStyle Text(Font fallback,int size,TextAnchor alignment,Color color,int weight=400)
        {
            return new GUIStyle(GUI.skin.label){font=Font(fallback,weight),fontSize=size,fontStyle=FontStyle.Normal,
                alignment=alignment,normal={textColor=color},padding=new RectOffset(0,0,0,0)};
        }
        internal static Rect ShopPanel=>new Rect(188,69,1074,169);
        internal static Rect Offer(int index)=>new Rect(188+index*190.5f,105,182.5f,120);
        internal static Rect RecruitList=>new Rect(1011,262,252,224);
        internal static Rect LineupSlot(int index,int count=10)
        {float step=942f/Mathf.Max(10,count);return new Rect(188+index*step,516,step-.2f,68);}
        internal static Rect ReserveSlot(int index)=>new Rect(188+index*94.2f,627,94,68);
        internal static Rect SellLeft=>new Rect(0,65,100,643);
        internal static Rect SellRight=>new Rect(1180,65,100,643);
        internal static Rect Menu=>new Rect(1010,66,252,270);
        internal static Color Tier(int cost)=>cost==5?new Color(.67f,.52f,.22f):cost==4?new Color(.51f,.36f,.62f):
            cost==3?new Color(.23f,.43f,.68f):cost==2?new Color(.28f,.51f,.37f):new Color(.43f,.51f,.57f);
        internal static void Fill(Rect rect,Color color)
        {
            var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
        }
        internal static void Border(Rect rect,Color color,float width=1)
        {
            Fill(new Rect(rect.x,rect.y,rect.width,width),color);Fill(new Rect(rect.x,rect.yMax-width,rect.width,width),color);
            Fill(new Rect(rect.x,rect.y,width,rect.height),color);Fill(new Rect(rect.xMax-width,rect.y,width,rect.height),color);
        }

        internal static Vector2 BeginDragScrollView(Rect viewport, Vector2 scroll, Rect content,
            GUIStyle verticalScrollbar = null)
        {
            var scrollbar = verticalScrollbar ?? GUI.skin.verticalScrollbar;
            int control = GUIUtility.GetControlID(FocusType.Passive, viewport);
            var input = Event.current;
            float limit = Mathf.Max(0, content.height - viewport.height);
            if (GUI.enabled && limit > 0 && input.button == 0)
            {
                var grabArea = viewport;
                grabArea.xMax -= scrollbar.fixedWidth + scrollbar.margin.left;
                switch (input.type)
                {
                    case EventType.MouseDown when GUIUtility.hotControl == 0 && grabArea.Contains(input.mousePosition):
                        scrollDragControl = control;
                        scrollDragOrigin = input.mousePosition;
                        scrollDragPosition = scroll;
                        break;
                    case EventType.MouseDrag when scrollDragControl == control:
                        float movement = input.mousePosition.y - scrollDragOrigin.y;
                        if (GUIUtility.hotControl == control || Mathf.Abs(movement) > 5)
                        {
                            // Take the drag only after the threshold so buttons keep ordinary clicks.
                            GUIUtility.hotControl = control;
                            scroll.y = Mathf.Clamp(scrollDragPosition.y - movement, 0, limit);
                            input.Use();
                        }
                        break;
                    case EventType.MouseUp when scrollDragControl == control:
                        if (GUIUtility.hotControl == control)
                        {
                            GUIUtility.hotControl = 0;
                            input.Use();
                        }
                        scrollDragControl = 0;
                        break;
                }
            }
            return GUI.BeginScrollView(viewport, scroll, content, false, false, GUIStyle.none, scrollbar);
        }

        internal static void ReleaseScrollDrag()
        {
            if (scrollDragControl == 0) return;
            if (GUIUtility.hotControl == scrollDragControl) GUIUtility.hotControl = 0;
            scrollDragControl = 0;
        }
    }
}
