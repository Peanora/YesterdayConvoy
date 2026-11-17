using UnityEngine;
using ShopFlowSnapshot=YesterdayConvoy.ApprovedShopPresentation.ShopFlowSnapshot;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        ShopFlowSnapshot CaptureShopFlowSnapshot()=>OriginalShopPresentation.CaptureShopFlowSnapshot();
        void BeginShopFlow(ShopFlowSnapshot snapshot,ShopTrialModel.Result result,Vector2 origin)
        {OriginalShopPresentation.BeginShopFlow(snapshot,result,origin);shopPromotedInstance=OriginalShopPresentation.PromotedInstance;shopPromotionAt=OriginalShopPresentation.PromotionAt;}
        void ClearShopFlows(){OriginalShopPresentation.ClearShopFlows();shopPromotedInstance=-1;shopPromotionAt=-100;}
        float ShopFlowPromotionRemaining(int instance)=>OriginalShopPresentation.ShopFlowPromotionRemaining(instance);
        int ShopDisplayedStar(ShopTrialModel.Piece piece)=>OriginalShopPresentation.ShopDisplayedStar(piece);
        void DrawShopFlows()=>OriginalShopPresentation.DrawShopFlows();
    }
}
