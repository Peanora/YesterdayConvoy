using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        Camera shopModelCamera=>OriginalShopPresentation.ModelCamera;
        RenderTexture shopModelTarget=>OriginalShopPresentation.ModelTarget;
        public float ShopPanelVisibility=>OriginalShopPresentation.ShopPanelVisibility;
        public bool ShopPanelTransitioning=>OriginalShopPresentation.ShopPanelTransitioning;
        public int ShopArrivalCount=>OriginalShopPresentation.ShopArrivalCount;
        public SpriteRenderer ShopBenchModel(int instance)=>OriginalShopPresentation.ShopBenchModel(instance);
        public SpriteRenderer ShopEntranceModel(int instance)=>OriginalShopPresentation.ShopEntranceModel(instance);
        void AnimateShopVisibility(bool visible)=>OriginalShopPresentation.AnimateShopVisibility(visible);
        void ResetShopPresentation()=>OriginalShopPresentation.ResetShopPresentation();
        void SyncShopBenchModels()=>OriginalShopPresentation.SyncShopBenchModels();
        void BeginShopArrival(ShopTrialModel.Result result)=>OriginalShopPresentation.BeginShopArrival(result);
        void ClearShopBenchModels()=>OriginalShopPresentation.ClearShopBenchModels();
        void DrawShopBench(GUIStyle center)=>OriginalShopPresentation.DrawShopBench(center);
        void DrawShopWalkway(Rect first,int unlocked,bool lineup)=>OriginalShopPresentation.DrawShopWalkway(first,unlocked,lineup);
        void LateUpdate(){if(!ArtLoading&&ShopTrialActive)OriginalShopPresentation.Tick();}
    }
}
