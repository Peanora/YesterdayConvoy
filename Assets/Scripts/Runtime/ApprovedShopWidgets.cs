using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public static class ApprovedShopWidgets
    {
        public sealed class RankingRow
        {
            public string CardId,Name;
            public float Damage;
            public Sprite Portrait;
            public bool Shared,Pursuit,Golem;
        }
        static void Fill(Rect r,Color c)=>ApprovedUi.Fill(r,c);
        static void ShopBorder(Rect r,Color c,float w=1)=>ApprovedUi.Border(r,c,w);
        static string RankingDamageText(float n)=>n.ToString(n>0&&n<1?"0.##":"N0",CultureInfo.InvariantCulture);
        public static Rect SynergyBounds(int count,bool preparing)
        {
            int rows=Mathf.Min(count,preparing?9:4);float top=preparing&&rows==9?60:65;
            return new Rect(12,top,174,26+rows*48);
        }
        public static void DrawSynergies(SynergySummary[] rows,bool preparing,Font font,ref Vector2 scroll,
            Action<SynergySummary> click,Action<Rect,SynergySummary> supplement,Func<SynergySummary,string> name,bool effectsEnabled=true)
        {
            var bounds=SynergyBounds(rows.Length,preparing);
            Fill(new Rect(0,bounds.y,bounds.xMax,bounds.height),new Color(.065f,.11f,.165f,.95f));
            GUI.Label(new Rect(bounds.x+1,bounds.y,55,22),"羁绊",ApprovedUi.Text(font,12,TextAnchor.MiddleLeft,new Color(.92f,.95f,.98f),600));
            GUI.Label(new Rect(bounds.x+65,bounds.y,bounds.width-70,22),effectsEnabled?rows.Count(s=>s.Tier>0)+"条已激活":"效果未迁移",ApprovedUi.Text(font,10,TextAnchor.MiddleRight,new Color(.61f,.70f,.80f)));
            Fill(new Rect(bounds.x,bounds.y+22,bounds.width,1),new Color(.54f,.65f,.77f,.17f));
            if(rows.Length==0)return;
            var viewport=new Rect(bounds.x,bounds.y+26,bounds.width,bounds.height-26);
            float width=rows.Length*48>viewport.height?156:174;
            scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,width,Mathf.Max(1,rows.Length*48)),false,false);
            try
            {
                for(int i=0;i<rows.Length;i++)
                {
                    var s=rows[i];var row=new Rect(0,i*48,width,48);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))click?.Invoke(s);
                    bool active=effectsEnabled&&s.Tier>0;var ink=active?new Color(.95f,.965f,.98f):new Color(.65f,.70f,.77f);
                    SynergyBadgePresentation.DrawBadge(new Rect(row.x+2,row.y+1,44,44),s.Name,effectsEnabled?s.Tier:0,s.Count,font);
                    GUI.Label(new Rect(row.x+58,row.y,width-64,20),name(s),ApprovedUi.Text(font,14,TextAnchor.MiddleLeft,ink,600));
                    SynergyBadgePresentation.DrawThresholds(new Rect(row.x+56,row.y+20,width-62,15),s.Thresholds,effectsEnabled?s.Tier:0,font);
                    supplement?.Invoke(row,s);
                }
            }
            finally{GUI.EndScrollView();}
        }
        public static void DrawRanking(IReadOnlyList<RankingRow> rows,float total,string state,Font font,ref Vector2 scroll,ref GUIStyle scrollbar)
        {
            GUIStyle LoopText(int size=15,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null,int weight=400)
                =>ApprovedUi.Text(font,size,alignment,color??ApprovedUi.Ink,weight);
            Rect panel=ApprovedUi.RecruitList;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0,0,panel.width,panel.height),ApprovedUi.Paper);
                GUI.Label(new Rect(10,5,118,25),"伤害排行",LoopText(14,TextAnchor.MiddleLeft,null,700));

                GUI.Label(new Rect(150,8,92,20),state,LoopText(9,TextAnchor.MiddleRight,ApprovedUi.Muted));
                GUI.Label(new Rect(10,30,230,18),"总伤害  "+RankingDamageText(total),LoopText(10,TextAnchor.MiddleLeft,null,600));
                Fill(new Rect(8,50,panel.width-16,1),ApprovedUi.Rule);
                var viewport=new Rect(6,54,panel.width-12,160);
                bool scrolling=rows.Count>5;
                if(scrollbar==null)scrollbar=new GUIStyle(GUI.skin.verticalScrollbar){fixedWidth=8,margin=new RectOffset(4,0,0,0)};
                float width=viewport.width-(scrolling?18:0);
                scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,width,Mathf.Max(viewport.height,rows.Count*32)),false,false,GUIStyle.none,scrollbar);
                try
                {
                    float highest=rows.Count>0?rows[0].Damage:0;
                    for(int i=0;i<rows.Count;i++)
                    {
                        var entry=rows[i];var row=new Rect(0,i*32,width,30);
                        bool shared=entry.Shared;
                        bool pursuit=entry.Pursuit;
                        Color accent=i==0&&entry.Damage>0?new Color(.69f,.51f,.22f):shared||pursuit?new Color(.55f,.43f,.63f):new Color(.28f,.50f,.66f);
                        Fill(row,new Color(.95f,.97f,.98f,.85f));
                        GUI.Label(new Rect(2,row.y,16,29),(i+1).ToString(),LoopText(10,TextAnchor.MiddleCenter,entry.Damage>0?accent:ApprovedUi.Muted,600));
                        bool golem=entry.Golem;
                        string name=entry.Name;
                        if(entry.Portrait!=null)ApprovedShopPresentation.DrawTestPortrait(new Rect(20,row.y,28,30),entry.Portrait);
                        else GUI.Label(new Rect(20,row.y,28,29),pursuit?"游":golem?"援":"效",LoopText(15,TextAnchor.MiddleCenter,accent));
                        var text=LoopText(name.Length>10?9:11,TextAnchor.MiddleLeft,null,600);text.clipping=TextClipping.Clip;
                        GUI.Label(new Rect(53,row.y+1,width-118,16),new GUIContent(name,name+"："+RankingDamageText(entry.Damage)),text);
                        GUI.Label(new Rect(width-64,row.y+1,60,16),RankingDamageText(entry.Damage),LoopText(11,TextAnchor.MiddleRight,null,600));
                        float barWidth=width-98;
                        Fill(new Rect(53,row.y+22,barWidth,4),new Color(.79f,.84f,.87f));
                        if(highest>0)Fill(new Rect(53,row.y+22,barWidth*entry.Damage/highest,4),accent);
                        float share=total>0?entry.Damage/total:0;
                        GUI.Label(new Rect(width-42,row.y+16,38,13),share.ToString("P0",CultureInfo.InvariantCulture),LoopText(8,TextAnchor.MiddleRight,ApprovedUi.Muted));
                    }
                }
                finally{GUI.EndScrollView();}
                // A compact caption stays below the five visible rows at both supported window sizes.
                GUI.Label(new Rect(10,214,panel.width-20,10),scrolling?"滚轮查看更多 · 仅计实际扣血":"本场累计 · 同名合并 · 仅计实际扣血",LoopText(7,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                ShopBorder(new Rect(0,0,panel.width,panel.height),ApprovedUi.Rule,1);
            }
            finally{GUI.EndGroup();}
        }
    }
}
