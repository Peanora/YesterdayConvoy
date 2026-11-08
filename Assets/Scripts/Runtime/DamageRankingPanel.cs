using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        struct DamageRankingRow
        {
            internal string CardId;
            internal float Damage;
        }
        readonly List<DamageRankingRow> damageRankingRows=new List<DamageRankingRow>();
        ManualBattle damageRankingBattle;
        Vector2 damageRankingScroll;
        int damageRankingFrame=-1;
        GUIStyle damageRankingScrollbar;

        void RefreshDamageRankingRows()
        {
            if(damageRankingBattle==Battle&&damageRankingFrame==Time.frameCount)return;
            if(damageRankingBattle!=Battle){damageRankingScroll=Vector2.zero;damageRankingBattle=Battle;}
            damageRankingFrame=Time.frameCount;damageRankingRows.Clear();
            foreach(var card in Battle.Cards)damageRankingRows.Add(new DamageRankingRow{CardId=card.Settings.CardId});
            if(damageRankingRows.Count==0)damageRankingRows.Add(new DamageRankingRow{CardId=Battle.Settings.CardId});
            foreach(var entry in Battle.DamageRanking)
            {
                int index=damageRankingRows.FindIndex(row=>row.CardId==entry.CardId);
                var row=new DamageRankingRow{CardId=entry.CardId,Damage=entry.Damage};
                if(index>=0)damageRankingRows[index]=row;else damageRankingRows.Add(row);
            }
            damageRankingRows.Sort((a,b)=>{int order=b.Damage.CompareTo(a.Damage);return order!=0?order:string.CompareOrdinal(a.CardId,b.CardId);});
        }
        static string RankingDamageText(float amount)=>amount.ToString("N0",CultureInfo.InvariantCulture);

        void DrawDamageRanking()
        {
            if(!DamageRankingVisible||Battle==null)return;
            RefreshDamageRankingRows();
            Rect panel=DamageRankingPanelRect;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0,0,panel.width,panel.height),ApprovedUi.Paper);
                GUI.Label(new Rect(10,5,118,25),"伤害排行",LoopText(14,TextAnchor.MiddleLeft,null,700));
                string state=Battle.Phase==BattlePhase.Ready?"待开战":Battle.IsPaused?"已暂停":Battle.Phase==BattlePhase.Running?"实时统计":"本场结算";
                GUI.Label(new Rect(150,8,92,20),state,LoopText(9,TextAnchor.MiddleRight,ApprovedUi.Muted));
                GUI.Label(new Rect(10,30,230,18),"总伤害  "+RankingDamageText(Battle.TotalPlayerDamage),LoopText(10,TextAnchor.MiddleLeft,null,600));
                Fill(new Rect(8,50,panel.width-16,1),ApprovedUi.Rule);
                var viewport=new Rect(6,54,panel.width-12,160);
                bool scrolling=damageRankingRows.Count>5;
                if(damageRankingScrollbar==null)damageRankingScrollbar=new GUIStyle(GUI.skin.verticalScrollbar){fixedWidth=8,margin=new RectOffset(4,0,0,0)};
                float width=viewport.width-(scrolling?18:0);
                damageRankingScroll=GUI.BeginScrollView(viewport,damageRankingScroll,new Rect(0,0,width,Mathf.Max(viewport.height,damageRankingRows.Count*32)),false,false,GUIStyle.none,damageRankingScrollbar);
                try
                {
                    float highest=damageRankingRows.Count>0?damageRankingRows[0].Damage:0;
                    for(int i=0;i<damageRankingRows.Count;i++)
                    {
                        var entry=damageRankingRows[i];var row=new Rect(0,i*32,width,30);
                        bool shared=entry.CardId==ManualBattle.SharedDamageCardId;
                        Color accent=i==0&&entry.Damage>0?new Color(.69f,.51f,.22f):shared?new Color(.55f,.43f,.63f):new Color(.28f,.50f,.66f);
                        Fill(row,new Color(.95f,.97f,.98f,.85f));
                        GUI.Label(new Rect(2,row.y,16,29),(i+1).ToString(),LoopText(10,TextAnchor.MiddleCenter,entry.Damage>0?accent:ApprovedUi.Muted,600));
                        bool golem=entry.CardId==CampaignBalance.GolemCardId;
                        string name=shared?"羁绊 / 场地":golem?"泥岩巨像":"支援单位";
                        if(shopDefinitions.TryGetValue(entry.CardId,out var role))
                        {
                            name=role.Card.DisplayName;
                            DrawTestPortrait(new Rect(20,row.y,28,30),OriginalTalulahArt.CardPortrait(role.Card));
                        }
                        else GUI.Label(new Rect(20,row.y,28,29),golem?"援":"效",LoopText(15,TextAnchor.MiddleCenter,accent));
                        var text=LoopText(name.Length>10?9:11,TextAnchor.MiddleLeft,null,600);text.clipping=TextClipping.Clip;
                        GUI.Label(new Rect(53,row.y+1,width-118,16),new GUIContent(name,name+"："+RankingDamageText(entry.Damage)),text);
                        GUI.Label(new Rect(width-64,row.y+1,60,16),RankingDamageText(entry.Damage),LoopText(11,TextAnchor.MiddleRight,null,600));
                        float barWidth=width-98;
                        Fill(new Rect(53,row.y+22,barWidth,4),new Color(.79f,.84f,.87f));
                        if(highest>0)Fill(new Rect(53,row.y+22,barWidth*entry.Damage/highest,4),accent);
                        float share=Battle.TotalPlayerDamage>0?entry.Damage/Battle.TotalPlayerDamage:0;
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
