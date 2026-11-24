using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        ApprovedShopPresentation originalShopPresentation;
        ApprovedShopPresentation OriginalShopPresentation=>originalShopPresentation??(originalShopPresentation=new ApprovedShopPresentation(new ApprovedShopPresentation.Host
        {
            Parent=transform,Shop=()=>shop,Font=()=>scene!=null?scene.InterfaceFont:null,
            Preparing=()=>shop!=null&&Battle!=null&&Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop),
            FlowActive=()=>Battle!=null&&(Loop!=null?Loop.CanShop:Battle.Phase==BattlePhase.Ready),
            Loading=()=>ArtLoading,Visible=()=>shopVisible,StoreVisible=v=>shopVisible=v,SetVisible=SetShopVisible,
            OddsVisible=()=>shopOddsOpen,StoreOdds=v=>shopOddsOpen=v,
            Dragging=()=>shopMoving,DragInstance=()=>shopDrag,Pointer=()=>shopPointer,
            PieceRect=ShopPieceRect,InLineup=instance=>shopSeats.InLineup(instance),
            Portrait=id=>shopDefinitions.TryGetValue(id,out var d)?OriginalTalulahArt.CardPortrait(d.Card):null,
            IdleFrame=OriginalShopIdleFrame,Tags=ShopTags,Coin=()=>shopCoin,Star=()=>shopStar,
            Purchase=(index,origin)=>{shopPurchaseOrigin=origin;PurchaseShopCard(index);},
            Refresh=()=>{if(JourneyActive)RefreshJourneyShop();else{var result=shop.Refresh();if(result.Success)shopRefreshAt=Time.unscaledTime;ShopNotice(result.Message);}},
            BuyXP=()=>{if(JourneyActive)UpgradeJourneyShop();else{var result=shop.Upgrade();if(result.Success){shopUpgradeAt=Time.unscaledTime;SyncShopDeck();}ShopNotice(result.Message);}},
            ToggleLock=()=>{if(JourneyActive)LockJourneyShop(!shop.Locked);else shop.Locked=!shop.Locked;},
            OpenOdds=()=>OpenShopProbabilityDetails(),CloseOverlays=CloseShopUiOverlays,DrawDropTarget=DrawShopSeatDropTarget
        }));
        Sprite OriginalShopIdleFrame(string id,double time)
        {
            if(id==talulahDefinition.Card.CardId)return OriginalTalulahArt.For(talulahDefinition.Card)?["idle"].At(time,true,out _)??talulahDefinition.Card.BattleSprite;
            var local=LocalMonsterArt.For(id);if(local!=null)return local["idle"].At(time,true,out _);
            var slug=OriginalSlugArt.For(id);return slug==null?null:slug["idle"].At(time,true,out _);
        }
    }
}
