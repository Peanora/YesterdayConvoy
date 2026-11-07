using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        ShopTrialModel seatedShop;
        ShopSeatLayout shopSeats=new ShopSeatLayout();
        bool ShopSeatsEditable=>shop!=null&&Battle!=null&&Battle.Phase==BattlePhase.Ready&&
            (Loop==null||Loop.CanShop)&&!ArtLoading&&!CampaignUiModalOpen&&!loopMenuOpen&&!loopTestSettlementOpen;
        public static Rect ShopLineupRect(int index)=>ApprovedUi.LineupSlot(index);
        void SyncShopSeats()
        {
            if(!ReferenceEquals(seatedShop,shop)){seatedShop=shop;shopSeats=new ShopSeatLayout();}
            shopSeats.Synchronize(shop.Bench,shopLineup,shop.Level);
            shopLineup.Clear();shopLineup.AddRange(shopSeats.Lineup(shop.Bench).Select(p=>p.Id));
        }
        ShopTrialModel.Piece ShopLineupPiece(string id)=>shop?.Bench.FirstOrDefault(p=>p.Id==id&&shopSeats.InLineup(p.Instance));
        void CompactShopLineupSeats(){if(shop!=null)shopSeats.CompactLineup();}
        bool ShopSeatAt(Vector2 point,out bool lineup,out int index)
        {
            for(int i=0;i<10;i++)
            {
                if(ApprovedUi.LineupSlot(i).Contains(point)){lineup=true;index=i;return true;}
                if(ApprovedUi.ReserveSlot(i).Contains(point)){lineup=false;index=i;return true;}
            }
            lineup=false;index=-1;return false;
        }
        public bool MoveShopCard(int instance,bool lineup,int index)
        {
            if(!ShopSeatsEditable)return false;
            if(!shopSeats.TryMove(shop.Bench,shop.Level,instance,lineup,index,out var failure))
            {ShopNotice(failure);return false;}
            shopLineup.Clear();shopLineup.AddRange(shopSeats.Lineup(shop.Bench).Select(p=>p.Id));
            SyncShopDeck(false);return true;
        }
        void ToggleShopSeat(ShopTrialModel.Piece piece)
        {
            bool destination=!IsLineupPiece(piece);
            int slot=shopSeats.FirstEmpty(destination,destination?shop.Level:10);
            if(slot<0){ShopNotice(destination?"编队位已满，拖到已有角色交换，或先升级":"备战席已满，拖到已有角色交换");return;}
            MoveShopCard(piece.Instance,destination,slot);
        }
        void DrawShopSeatDropTarget()
        {
            if(!shopMoving||shopDrag<0||!ShopSeatsEditable||!ShopSeatAt(shopPointer,out bool lineup,out int slot))return;
            bool unlocked=!lineup||slot<shop.Level;
            var rect=lineup?ApprovedUi.LineupSlot(slot):ApprovedUi.ReserveSlot(slot);
            ShopBorder(rect,unlocked?new Color(.78f,.96f,.91f,.95f):new Color(.95f,.45f,.37f,.95f),2);
            int target=shopSeats.At(lineup,slot);
            string text=!unlocked?"未解锁":target==0?"松开放置":target==shopDrag?"原位置":"松开交换";
            GUI.Label(new Rect(rect.x,rect.y-19,rect.width,18),text,LoopText(10,TextAnchor.MiddleCenter,Color.white));
        }
    }
}
