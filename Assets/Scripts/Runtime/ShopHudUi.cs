using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public static class ShopProbabilityUi
    {
        public static int[] Baseline(int level)
        {
            if(level<1||level>10)throw new ArgumentOutOfRangeException(nameof(level));
            return (int[])ShopTrialModel.Odds[level-1].Clone();
        }
        // Refresh first returns displayed stock, then draws five times without replacement.
        public static float[] FirstDraw(ShopTrialModel shop,int level)
        {
            var weights=Baseline(level);var stock=new int[5];
            foreach(var role in shop.Roles)stock[role.Cost-1]+=shop.Available(role.Id)+shop.Offers.Count(id=>id==role.Id);
            int total=weights.Where((w,i)=>stock[i]>0).Sum();
            return weights.Select((w,i)=>total==0||stock[i]==0?0:100f*w/total).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        bool shopOddsOpen;
        public static Rect ShopResourceHudRect=>new Rect(12,581,164,124);
        public static Rect ShopOddsPanelRect=>new Rect(306,105,670,498);
        public static Rect ShopOddsButtonRect=>new Rect(868,73,271,22);
        public bool ShopProbabilityVisible=>shopOddsOpen;
        void CloseShopUiOverlays(){shopOddsOpen=false;infectionChoiceRequested=false;inspectedSynergy=null;campaignConstructOpen=false;synergyRulesScroll=synergyMembersScroll=Vector2.zero;}
        void OpenShopOdds(){CloseCampaignOverlay();CancelShopDrag();CloseSkillMenu();shopOddsOpen=true;}
        public bool OpenShopProbabilityDetails(){if(!ShopTrialActive||shop==null)return false;OpenShopOdds();return true;}
        void DrawShopResourceHud(bool preparing)
        {
            var r=ShopResourceHudRect;Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            Fill(new Rect(r.x,r.y,3,r.height),ApprovedUi.Gold);
            if(shopCoin!=null)GUI.DrawTexture(new Rect(r.x+12,r.y+13,18,18),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(r.x+37,r.y+6,43,29),"金币",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            string gold=shop.UnlimitedGold?"∞":shop.Gold.ToString();
            GUI.Label(new Rect(r.x+76,r.y+4,77,32),gold,LoopText(gold.Length>4?19:27,TextAnchor.MiddleRight,new Color(.45f,.34f,.16f),700));
            Fill(new Rect(r.x+12,r.y+42,r.width-24,1),ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+12,r.y+48,64,22),"等级 "+shop.Level,LoopText(15,TextAnchor.MiddleLeft,null,600));
            GUI.Label(new Rect(r.x+72,r.y+50,80,18),shop.Level==10?"已达上限":$"{shop.XP}/{shop.NextLevelXP} 经验",LoopText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
            Fill(new Rect(r.x+12,r.y+76,r.width-24,4),new Color(.65f,.72f,.77f));
            Fill(new Rect(r.x+12,r.y+76,(r.width-24)*(shop.Level==10?1:Mathf.Clamp01((float)shop.XP/shop.NextLevelXP)),4),new Color(.29f,.48f,.63f));
            bool enabled=GUI.enabled;GUI.enabled=enabled&&preparing&&!ShopPanelTransitioning;
            if(LoopButton(new Rect(r.x+12,r.y+89,r.width-24,25),"买经验 +4 · 4金币"))
            {if(JourneyActive)UpgradeJourneyShop();else {var result=shop.Upgrade();if(result.Success){shopUpgradeAt=Time.unscaledTime;SyncShopDeck();}ShopNotice(result.Message);}}
            GUI.enabled=enabled;
        }
        void DrawShopOdds(Vector2 point)
        {
            if(!shopOddsOpen||shop==null)return;
            var e=Event.current;var r=ShopOddsPanelRect;
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){shopOddsOpen=false;e.Use();return;}
            if(e.type==EventType.MouseDown&&e.button==0&&!r.Contains(point)){shopOddsOpen=false;e.Use();return;}
            Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+22,r.y+15,r.width-85,29),"招募概率",LoopText(23,TextAnchor.MiddleLeft,null,700));
            if(LoopButton(new Rect(r.xMax-54,r.y+15,32,28),"×")){shopOddsOpen=false;return;}
            GUI.Label(new Rect(r.x+22,r.y+49,r.width-44,23),"等级决定费用档；同费用内按剩余牌数加权抽取",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            const float rowHeight=28;float top=r.y+84;float col=(r.width-143)/5;
            GUI.Label(new Rect(r.x+24,top,85,24),"队伍等级",LoopText(11,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            for(int cost=1;cost<=5;cost++)GUI.Label(new Rect(r.x+119+(cost-1)*col,top,col,24),cost+"费",LoopText(12,TextAnchor.MiddleCenter,ApprovedUi.Tier(cost),600));
            for(int level=1;level<=10;level++)
            {
                float y=top+26+(level-1)*rowHeight;bool current=level==shop.Level;
                if(current){Fill(new Rect(r.x+20,y,r.width-40,rowHeight),new Color(.75f,.82f,.85f));Fill(new Rect(r.x+20,y,3,rowHeight),ApprovedUi.Gold);}
                var ink=current?ApprovedUi.Ink:ApprovedUi.Muted;var weights=ShopProbabilityUi.Baseline(level);
                GUI.Label(new Rect(r.x+24,y,85,rowHeight),level+(current?"  当前":""),LoopText(12,TextAnchor.MiddleCenter,ink,current?700:400));
                for(int cost=0;cost<5;cost++)GUI.Label(new Rect(r.x+119+cost*col,y,col,rowHeight),weights[cost]+"%",LoopText(13,TextAnchor.MiddleCenter,ink,current?600:400));
            }
            float footer=top+26+10*rowHeight+13;var actual=ShopProbabilityUi.FirstDraw(shop,shop.Level);
            GUI.Label(new Rect(r.x+24,footer,r.width-48,21),"按当前库存返还商品后，首抽费用档概率： "+string.Join("  /  ",actual.Select(v=>v.ToString("0.#")+"%")),LoopText(11,TextAnchor.MiddleLeft,null,600));
            var text=LoopText(11,TextAnchor.UpperLeft,ApprovedUi.Muted);text.wordWrap=true;
            GUI.Label(new Rect(r.x+24,footer+27,r.width-48,51),"上表是基础权重。已解锁费用档完全缺货时剔除该档，其余有库存档按原权重归一；每抽一张库存都会变化。五格商品占库，刷新先返库；库存不足五张时按实际数量展示。",text);
        }
    }
}
