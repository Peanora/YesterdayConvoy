using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        void EnsureShopAura()=>OriginalShopPresentation.EnsureShopAura();
        void DrawShopRankPortrait(Rect rect,Sprite portrait,int star,float burst=0)=>OriginalShopPresentation.DrawShopRankPortrait(rect,portrait,star,burst);
        void DrawShopRankBadge(Rect rect,int star,bool animate=true)=>OriginalShopPresentation.DrawShopRankBadge(rect,star,animate);
    }
}
