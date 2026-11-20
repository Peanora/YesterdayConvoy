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
        void DrawShopResourceHud(bool preparing)=>OriginalShopPresentation.DrawShopResourceHud(preparing);
        void DrawShopOdds(Vector2 point)=>OriginalShopPresentation.DrawShopOdds(point);
    }
}
