using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        bool marketVisible=true,shopOddsVisible,finalRewardClaimed,formationMenuOpen;
        int shopDragInstance,selectedPiece,shopInputBlockedFrame=-1;
        Vector2 formationShopPointer;
        Texture2D formationCoin,formationStar;
        ApprovedShopPresentation approvedShop;
        public ApprovedShopPresentation ShopPresentation=>approvedShop;
        public bool MarketVisible=>marketVisible;
        public float ShopPanelVisibility=>approvedShop?.ShopPanelVisibility??(marketVisible?1:0);
        public bool ShopPanelTransitioning=>approvedShop?.ShopPanelTransitioning??false;
        public int ShopArrivalCount=>approvedShop?.ShopArrivalCount??0;
        public RenderTexture ShopModelTarget=>approvedShop?.ModelTarget;
        public Camera ShopModelCamera=>approvedShop?.ModelCamera;
        public SpriteRenderer ShopBenchModel(int instance)=>approvedShop?.ShopBenchModel(instance);
        public SpriteRenderer ShopEntranceModel(int instance)=>approvedShop?.ShopEntranceModel(instance);
        public static Rect FormationLineupRect(int index)=>ApprovedUi.LineupSlot(index);
        public static Rect FormationReserveRect(int index)=>ApprovedUi.ReserveSlot(index);
        public static Rect FormationMarketRect=>ApprovedUi.ShopPanel;
        public static Rect FormationOfferRect(int index)=>ApprovedUi.Offer(index);
        static Rect FormationOddsRect=>ApprovedShopPresentation.OddsPanelRect;
        static Rect FormationSellLeft=>ApprovedUi.SellLeft;
        static Rect FormationSellRight=>ApprovedUi.SellRight;
        bool CanTrade=>Ready&&Session!=null&&Session.CanShop&&Model.Phase==FormationPhase.Preparation;
        public Rect FormationPieceRect(int instance)
        {
            var seat=Session?.Seats.For(instance);
            return !seat.HasValue?Rect.zero:seat.Value.Lineup?FormationLineupRect(seat.Value.Index):FormationReserveRect(seat.Value.Index);
        }
        public Rect ShopPieceRect(int index)=>Shop==null||index<0||index>=Shop.Bench.Count?Rect.zero:
            Model.Phase==FormationPhase.Preparation?FormationPieceRect(Shop.Bench[index].Instance):Rect.zero;
        void EnsureOriginalShop()
        {
            if(approvedShop!=null)return;
            formationCoin=new Texture2D(2,2,TextureFormat.RGBA32,false);
            formationCoin.LoadImage(File.ReadAllBytes(ArtSourcePaths.Resolve("ShopTrial/originium.png")));
            formationStar=ApprovedShopPresentation.CreateShopStar();
            approvedShop=new ApprovedShopPresentation(new ApprovedShopPresentation.Host
            {
                Parent=transform,Shop=()=>Shop,Font=()=>ApprovedUi.Font(null),Preparing=()=>CanTrade,
                Loading=()=>!Ready,Visible=()=>marketVisible,StoreVisible=v=>marketVisible=v,SetVisible=SetMarketVisible,
                OddsVisible=()=>shopOddsVisible,StoreOdds=v=>shopOddsVisible=v,
                Dragging=()=>pointerHeld&&Vector2.Distance(formationShopPointer,pointerStart)>6&&!DragPreviewVisible,
                DragInstance=()=>shopDragInstance,Pointer=()=>formationShopPointer,PieceRect=ShopPieceRect,
                InLineup=instance=>Session.Seats.InLineup(instance),Portrait=id=>FormationUnitArt.For(id)?.Portrait,
                IdleFrame=(id,time)=>FormationUnitArt.For(id)?.Frame("idle",time,true),Tags=CampaignBalance.Tags,
                Coin=()=>formationCoin,Star=()=>formationStar,Purchase=(index,origin)=>BuyOfferAt(index,origin),
                Refresh=()=>RefreshMarket(),BuyXP=()=>BuyExperience(),ToggleLock=()=>Session.SetLocked(!Shop.Locked),
                OpenOdds=()=>{CancelFormationDrag();shopOddsVisible=true;},
                CloseOverlays=()=>{shopOddsVisible=false;CancelFormationDrag();},DrawDropTarget=DrawFormationDropTarget,
                LineupHint="自由拖放位置 · 重叠交换 · 站位跨关保留"
            });
            approvedShop.ResetShopPresentation();approvedShop.EnsureShopAura();approvedShop.SyncShopBenchModels();
        }
        public void SetMarketVisible(bool visible)
        {
            visible=visible&&Model!=null&&Model.Phase==FormationPhase.Preparation;
            if(approvedShop==null)marketVisible=visible;else approvedShop.AnimateShopVisibility(visible);
            shopOddsVisible=false;CancelFormationDrag();
        }
        ShopTrialModel.Result ShopAction(Func<ShopTrialModel.Result> action,bool arrival=false,Vector2 origin=default)
        {
            if(!CanTrade)return new ShopTrialModel.Result{Message="仅战前整备时可以交易。"};
            EnsureOriginalShop();Session.SavePositions(Model);
            var snapshot=arrival?approvedShop.CaptureShopFlowSnapshot():null;
            var result=action();notice=result.Message;
            if(result.Success)
            {
                Session.ApplyTo(Model);
                if(result.Instance>0&&Shop.Bench.Any(piece=>piece.Instance==result.Instance))selectedPiece=result.Instance;
                SelectFirstLineup();SyncActors();approvedShop.SyncShopBenchModels();
                if(arrival){approvedShop.BeginShopArrival(result);approvedShop.BeginShopFlow(snapshot,result,origin);}
            }
            return result;
        }
        ShopTrialModel.Result BuyOfferAt(int index,Vector2 origin)=>ShopAction(()=>Session.Purchase(index),true,origin);
        public ShopTrialModel.Result BuyOffer(int index)=>BuyOfferAt(index,FormationOfferRect(index).center);
        public ShopTrialModel.Result RefreshMarket()=>ShopAction(()=>Session.Refresh());
        public ShopTrialModel.Result BuyExperience()=>ShopAction(()=>Session.BuyExperience());
        public ShopTrialModel.Result SellPiece(int instance)=>ShopAction(()=>Session.Sell(instance));
        public ShopTrialModel.Result MovePiece(int instance,bool lineup,int seat)=>ShopAction(()=>Session.Move(instance,lineup,seat));
        bool FormationShopBlocksPoint(Vector2 point)=>shopOddsVisible||formationMenuOpen||point.y<65||point.y>=480||
            point.x<188||ApprovedUi.RecruitList.Contains(point)||(marketVisible||ShopPanelTransitioning)&&FormationMarketRect.Contains(point)||
            formationInspectedTag!=null&&new Rect(200,244,380,112).Contains(point);
        void CancelFormationDrag(){pointerHeld=false;pendingClickDeployment=false;dragSlot=-1;shopDragInstance=0;ClearDeploymentPreview();}
        void SelectFirstLineup()
        {
            if(Model==null)return;
            var selected=Model.Units.FirstOrDefault(u=>u.Instance==selectedPiece);
            if(selected==null&&selectedPiece>0&&Shop.Bench.Any(p=>p.Instance==selectedPiece)){SelectedSlot=-1;return;}
            selected=selected??Model.Units.FirstOrDefault();SelectedSlot=selected?.Slot??-1;selectedPiece=selected?.Instance??0;
        }
        void SelectShopPiece(int instance){selectedPiece=instance;SelectedSlot=Model.Units.FindIndex(u=>u.Instance==instance);}
        bool FindFormationSeat(Vector2 point,out bool lineup,out int seat)
        {
            for(int i=0;i<10;i++)
            {
                if(FormationLineupRect(i).Contains(point)){lineup=true;seat=i;return true;}
                if(FormationReserveRect(i).Contains(point)){lineup=false;seat=i;return true;}
            }
            lineup=false;seat=-1;return false;
        }
        bool FormationShopPointerDown(Vector2 point)
        {
            if(formationMenuOpen){shopInputBlockedFrame=Time.frameCount;if(!ApprovedUi.Menu.Contains(point))formationMenuOpen=false;CancelFormationDrag();return true;}
            if(shopOddsVisible){shopInputBlockedFrame=Time.frameCount;if(!FormationOddsRect.Contains(point))shopOddsVisible=false;CancelFormationDrag();return true;}
            if((marketVisible||ShopPanelTransitioning)&&FormationMarketRect.Contains(point)){CancelFormationDrag();return true;}
            if(!FindFormationSeat(point,out bool lineup,out int seat))return false;
            int instance=Session.Seats.At(lineup,seat);
            if(instance>0){SelectShopPiece(instance);shopDragInstance=instance;PrepareDeploymentGrab(instance);}else CancelFormationDrag();
            return true;
        }
        bool FormationShopPointerUp(Vector2 point)
        {
            if(shopDragInstance<=0)return FormationShopBlocksPoint(point);
            if(!ApprovedUi.RecruitList.Contains(point)&&(FormationSellLeft.Contains(point)||FormationSellRight.Contains(point))){SellPiece(shopDragInstance);return true;}
            if(FindFormationSeat(point,out bool lineup,out int seat)){MovePiece(shopDragInstance,lineup,seat);return true;}
            return FormationShopBlocksPoint(point);
        }
        void PlaceReserve(int instance,Vector2Int cell)
        {
            ShopAction(()=>Session.TryDeploy(instance,cell));
        }
        void DrawFormationPortrait(Rect rect,string id)=>ApprovedShopPresentation.DrawTestPortrait(rect,FormationUnitArt.For(id)?.Portrait);
        void DrawFormationMarket()=>approvedShop.DrawApprovedShopMarket(CanTrade);
        void DrawFormationResources(bool preparing)=>approvedShop.DrawShopResourceHud(preparing);
        void DrawFormationOdds()=>approvedShop.DrawShopOdds(formationShopPointer);
        void DrawFormationPreparation()
        {
            approvedShop.DrawShopBench(approvedShop.LoopText(12,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(1142,624,120,18),"开战后锁定本场编队",approvedShop.LoopText(10,TextAnchor.MiddleCenter,Color.white));
            bool enabled=GUI.enabled;GUI.enabled=enabled&&RosterArtReady;
            if(approvedShop.LoopButton(new Rect(1142,643,120,51),RosterArtReady?"开始进攻 ▶":"准备动作…",true))StartBattle();
            GUI.enabled=enabled;
        }
        void DrawFormationDropTarget()
        {
            if(!pointerHeld||shopDragInstance<=0||Vector2.Distance(formationShopPointer,pointerStart)<=6||
                !FindFormationSeat(formationShopPointer,out bool lineup,out int seat))return;
            bool unlocked=!lineup||seat<Shop.Level;
            var r=lineup?FormationLineupRect(seat):FormationReserveRect(seat);
            ApprovedUi.Border(r,unlocked?new Color(.86f,.83f,.72f,.8f):new Color(.86f,.43f,.36f,.85f),1);
        }
        void DrawFormationSaleTargets()
        {
            var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);if(piece==null||!CanTrade)return;
            var quote=Shop.Quote(shopDragInstance);if(!quote.Success)return;
            foreach(var rect in new[]{FormationSellLeft,FormationSellRight})
            {
                bool hit=rect.Contains(formationShopPointer)&&!ApprovedUi.RecruitList.Contains(formationShopPointer);
                var ink=hit?new Color(.46f,.32f,.21f):new Color(.28f,.38f,.46f);
                Fill(rect,hit?new Color(.92f,.86f,.79f,.97f):new Color(.85f,.89f,.91f,.95f));
                ApprovedUi.Border(rect,hit?new Color(.71f,.54f,.34f):ApprovedUi.Rule,hit?2:1);
                float y=rect.center.y-88;
                GUI.Label(new Rect(rect.x+7,y,rect.width-14,30),"出售",approvedShop.LoopText(20,TextAnchor.MiddleCenter,ink,600));
                GUI.Label(new Rect(rect.x+7,y+39,rect.width-14,24),"松开出售",approvedShop.LoopText(14,TextAnchor.MiddleCenter,ink,600));
                GUI.Label(new Rect(rect.x+7,y+68,rect.width-14,22),quote.Message,approvedShop.LoopText(quote.Message.Length>6?9:11,TextAnchor.MiddleCenter,ink,500));
                GUI.Label(new Rect(rect.x+7,y+94,rect.width-14,18),quote.FinalStar+"星",approvedShop.LoopText(10,TextAnchor.MiddleCenter,ApprovedUi.Muted));
                GUI.Label(new Rect(rect.x+7,y+119,rect.width-14,40),"+"+quote.Value,approvedShop.LoopText(28,TextAnchor.MiddleCenter,new Color(.56f,.43f,.23f),700));
                GUI.Label(new Rect(rect.x+7,y+159,rect.width-14,18),"金币",approvedShop.LoopText(10,TextAnchor.MiddleCenter,new Color(.56f,.43f,.23f)));
                GUI.Label(new Rect(rect.x+7,y+194,rect.width-14,18),"拖回或Esc取消",approvedShop.LoopText(9,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            }
        }
    }
}
