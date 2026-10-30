using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        Texture2D shopAura;
        static Color ShopRankColor(int star,float alpha=1)=>star==3?new Color(1,.78f,.28f,alpha):new Color(.81f,.87f,.96f,alpha);
        void EnsureShopAura()
        {
            if(shopAura!=null)return;
            shopAura=new Texture2D(96,96,TextureFormat.RGBA32,false){name="Shop rank aura",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color[96*96];
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                float radius=new Vector2((x-47.5f)/47.5f,(y-47.5f)/47.5f).magnitude;
                float alpha=Mathf.Pow(Mathf.Clamp01(1-radius),1.8f);
                pixels[y*96+x]=new Color(1,1,1,alpha);
            }
            shopAura.SetPixels(pixels);shopAura.Apply();
        }
        void DrawShopRankPortrait(Rect rect,Sprite portrait,int star,float burst=0)
        {
            if(star>1)
            {
                float time=Time.unscaledTime,pulse=.5f+.5f*Mathf.Sin(time*3.4f);
                Color previous=GUI.color;GUI.color=ShopRankColor(star,.32f+.12f*pulse+.32f*burst);
                GUI.DrawTexture(new Rect(rect.x-8,rect.y-7,rect.width+16,rect.height+14),shopAura,ScaleMode.StretchToFill,true);
                GUI.color=ShopRankColor(star,.18f+.1f*pulse+.12f*burst);
                float spread=rect.width>70?2:1.2f;
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;Rect outline=rect;outline.position+=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*spread;DrawTestPortrait(outline,portrait);
                }
                GUI.color=previous;
                Vector2 center=rect.center;int orbits=star==3?3:2;
                for(int orbit=0;orbit<orbits;orbit++)
                {
                    float head=time*(star==3?1.45f:1.1f)+orbit*Mathf.PI*2/orbits;
                    for(int segment=0;segment<13;segment++)
                    {
                        float a=head-segment*.055f,b=a-.055f;
                        Vector2 radius=new Vector2(rect.width*.47f,rect.height*.44f);
                        Vector2 from=center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),radius),to=center+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),radius);
                        ShopLine(from,to,ShopRankColor(star,(1-segment/13f)*(.55f+.3f*burst)),segment<3?2:1);
                    }
                }
                if(burst>0)
                {
                    float radius=1+(1-burst)*.45f;
                    Rect ring=new Rect(center.x-rect.width*.52f*radius,center.y-rect.height*.48f*radius,rect.width*1.04f*radius,rect.height*.96f*radius);
                    ShopBorder(ring,ShopRankColor(star,burst*.7f),1);
                }
            }
            DrawTestPortrait(rect,portrait);
        }
        void DrawShopRankBadge(Rect rect,int star,bool animate=true)
        {
            float wave=animate?.5f+.5f*Mathf.Sin(Time.unscaledTime*(star==3?4.6f:4f)):1;
            Fill(rect,new Color(.025f,.045f,.055f,.82f));
            ShopBorder(rect,ShopRankColor(star,.25f+.3f*wave),1);
            if(animate)
            {
                Color previous=GUI.color;GUI.color=ShopRankColor(star,.2f+.25f*wave);
                GUI.DrawTexture(new Rect(rect.x-4,rect.y-8,rect.width+8,rect.height+16),shopAura,ScaleMode.StretchToFill,true);GUI.color=previous;
            }
            DrawShopStars(new Rect(rect.x+3,rect.y+2,rect.width-6,rect.height-4),star,ShopRankColor(star,.5f+.5f*wave));
        }
    }
}
