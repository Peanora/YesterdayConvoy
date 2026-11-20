using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        bool marketVisible,shopOddsVisible,finalRewardClaimed,formationMenuOpen;
        int shopDragInstance,selectedPiece,shopInputBlockedFrame=-1;
        int pressedFormationOffer = -1;
        int overflowPage;
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
        // Internal population seats remain in the model; deployed units have no second screen row.
        public static Rect FormationLineupRect(int index)=>Rect.zero;
        public static Rect FormationBenchRect => new Rect(115, 627, 1050, 86);
        public static Rect FormationReserveRect(int index) => new Rect(120 + 104.3f * index, 643, 102, 69);
        const int OverflowColumns = 4, OverflowPageSize = 8;
        int OverflowCount => Session?.Seats.OverflowCount ?? 0;
        int OverflowPages => Mathf.Max(1, (OverflowCount + OverflowPageSize - 1) / OverflowPageSize);
        int OverflowFirst => ShopSeatLayout.ReserveCapacity + Mathf.Min(overflowPage, OverflowPages - 1) * OverflowPageSize;
        int VisibleOverflowCount => Mathf.Clamp(ShopSeatLayout.ReserveCapacity + OverflowCount - OverflowFirst, 0, OverflowPageSize);
        Rect FormationOverflowPanel => OverflowCount == 0 ? Rect.zero :
            new Rect(914, 247, 264, 27 + 63 * ((VisibleOverflowCount + OverflowColumns - 1) / OverflowColumns));
        Rect FormationOverflowRect(int index)
        {
            int display = index - OverflowFirst;
            if (display < 0 || display >= OverflowPageSize) return Rect.zero;
            return new Rect(918 + 65 * (display % OverflowColumns), 270 + 63 * (display / OverflowColumns), 62, 60);
        }
        Rect FormationReserveOrOverflowRect(int index) => index < ShopSeatLayout.ReserveCapacity
            ? FormationReserveRect(index) : FormationOverflowRect(index);
        const float MarketHeaderHeight = 32, MarketCardHeight = 120, MarketInset = 10;
        public static Rect FormationMarketRect => new Rect(188, 77, 1012, MarketHeaderHeight + MarketCardHeight + MarketInset);
        public static Rect FormationOfferRect(int index)
        {
            var market = FormationMarketRect;
            return new Rect(market.x + MarketInset + 174 * index, market.y + MarketHeaderHeight, 166, MarketCardHeight);
        }
        static Rect FormationRefreshRect
        {
            get
            {
                var market = FormationMarketRect;
                return new Rect(market.xMax - 132, market.y + MarketHeaderHeight, 122, 29);
            }
        }
        public static Rect FormationExperienceRect => new Rect(8, 627, 104, 86);
        public static Rect FormationCoinRect => new Rect(1168, 646, 104, 67);
        public static Rect FormationInterestRect => new Rect(1168, 627, 104, 19);
        public static Rect FormationStartRect => new Rect(1150, 567, 122, 48);
        public int FormationDisplayedInterest=>Shop==null||Campaign?.CurrentNode.Kind=="补给"?0:
            Math.Min(Shop.FormationEconomy?Shop.InterestCap:5,Shop.Gold/(Shop.FormationEconomy?Shop.InterestDivisor:10));
        static Rect FormationOddsRect=>ApprovedShopPresentation.OddsPanelRect;
        static Rect FormationSellLeft=>ApprovedUi.SellLeft;
        static Rect FormationSellRight=>ApprovedUi.SellRight;
        bool CanTrade=>Ready&&Session!=null&&Session.CanShop&&Model.Phase==FormationPhase.Preparation;
        public Rect FormationPieceRect(int instance)
        {
            var seat=Session?.Seats.For(instance);
            if(!seat.HasValue)return Rect.zero;
            if(!seat.Value.Lineup)return FormationReserveOrOverflowRect(seat.Value.Index);
            var unit=Model?.Units.FirstOrDefault(u=>u.Instance==instance);
            return unit==null||!FormationActorVisibleInView(unit)?Rect.zero:FormationActorScreenRect(unit);
        }
        public Rect ShopPieceRect(int index)
        {
            if(CombatViewActive||Shop==null||index<0||index>=Shop.Bench.Count)return Rect.zero;
            var seat=Session.Seats.For(Shop.Bench[index].Instance);
            return seat.HasValue&&!seat.Value.Lineup&&seat.Value.Index<ShopSeatLayout.ReserveCapacity?
                FormationReserveRect(seat.Value.Index):Rect.zero;
        }
        Rect FormationFlowPieceRect(int index)
        {
            if(Shop==null||index<0||index>=Shop.Bench.Count)return Rect.zero;
            return FormationRecruitmentTargetRect(Shop.Bench[index].Instance);
        }
        public Rect FormationRecruitmentTargetRect(int instance)
        {
            if(CombatViewActive)return Rect.zero;
            var seat=Session?.Seats.For(instance);
            if(!seat.HasValue)return Rect.zero;
            if(!seat.Value.Lineup)return FormationReserveOrOverflowRect(seat.Value.Index);
            var unit=Model?.Units.FirstOrDefault(u=>u.Instance==instance);
            if(unit==null||!FormationActorVisibleInView(unit))return Rect.zero;
            var foot=CellScreen(unit.SpawnCenter);
            return new Rect(foot.x-46,foot.y-55,92,68);
        }
        void EnsureOriginalShop()
        {
            if(approvedShop!=null)return;
            formationCoin=new Texture2D(2,2,TextureFormat.RGBA32,false);
            formationCoin.LoadImage(File.ReadAllBytes(ArtSourcePaths.Resolve("ShopTrial/originium.png")));
            formationStar=ApprovedShopPresentation.CreateShopStar();
            approvedShop=new ApprovedShopPresentation(new ApprovedShopPresentation.Host
            {
                Parent=transform,Shop=()=>Shop,Font=()=>ApprovedUi.Font(null),Preparing=()=>CanTrade,
                Loading=()=>!Ready,Visible=()=>marketVisible,StoreVisible=v=>marketVisible=v,SetVisible=SetMarketVisible,CanCollapse=()=>true,ReserveOnly=()=>true,
                MarketRect=()=>FormationMarketRect,OfferRect=FormationOfferRect,FlowPieceRect=FormationFlowPieceRect,
                ReserveDeck=()=>FormationBenchRect,ReserveSlot=FormationReserveRect,ReserveFee=FormationFeeColor,
                ReserveFull=()=>Session.Seats.ReserveFull,
                ReserveSelection=instance=>selectedPiece==instance,
                OddsVisible=()=>shopOddsVisible,StoreOdds=v=>shopOddsVisible=v,
                Dragging=()=>pointerHeld&&Vector2.Distance(formationShopPointer,pointerStart)>6&&!DragPreviewVisible,
                DragInstance=()=>shopDragInstance,LineupSeatCount=()=>Mathf.Max(10,Shop.PopulationCap),Pointer=()=>formationShopPointer,PieceRect=ShopPieceRect,
                InLineup=instance=>Session.Seats.InLineup(instance),Portrait=id=>FormationUnitArt.For(id)?.Portrait,
                IdleFrame=FormationShopIdleFrame,ModelHeight=id=>FormationUnitArt.For(id)?.Height??1,
                Tags=id=>Campaign==null?CampaignBalance.Tags(id):FormationCampaignRules.Find(id)?.Factions.Concat(FormationCampaignRules.Find(id).Styles).ToArray()??Array.Empty<string>(),
                Coin=()=>formationCoin,Star=()=>formationStar,Purchase=(index,origin)=>BuyOfferAt(index,origin),
                Refresh=()=>RefreshMarket(),BuyXP=()=>BuyExperience(),ToggleLock=()=>Session.SetLocked(!Shop.Locked),
                OpenOdds=OpenFormationOdds,
                CloseOverlays=()=>{shopOddsVisible=false;CancelFormationDrag();},DrawDropTarget=DrawFormationDropTarget,
                LineupHint="自由拖放位置 · 重叠交换 · 站位跨关保留"
            });
            approvedShop.ResetShopPresentation();approvedShop.EnsureShopAura();approvedShop.SyncShopBenchModels();
        }
        public void SetMarketVisible(bool visible)
        {
            visible=visible&&Model!=null&&Model.Phase==FormationPhase.Preparation&&Campaign?.OpeningRewardsPending!=true;
            if(visible)
            {
                CloseFormationTraitDetails();
                CloseFormationInspection();
                CloseFormationRouteDetails();
                CloseFormationEquipmentDetails();
                CancelFormationEquipmentInteraction();
                rewardHistoryOpen=false;
            }
            if(approvedShop==null)marketVisible=visible;else approvedShop.AnimateShopVisibility(visible);
            shopOddsVisible=false;CancelFormationDrag();
        }
        public void ToggleFormationMarket()=>SetMarketVisible(!MarketVisible);
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
        bool FormationShopBlocksPoint(Vector2 point)=>CampaignInteractionBlocked||shopOddsVisible||formationMenuOpen||
            FormationTraitBlocksPoint(point)||FormationSupplyBlocksPoint(point)||FormationInspectionBlocksPoint(point)||
            OperationsBlocksPoint(point)||FormationOverflowPanel.Contains(point)||!BoardInputRect.Contains(point)||
            Campaign!=null&&point.y<FormationMarketRect.yMin||
            (marketVisible||ShopPanelTransitioning)&&FormationMarketRect.Contains(point)||
            Campaign==null&&formationInspectedTag!=null&&new Rect(200,244,380,112).Contains(point);
        void CancelFormationDrag(){pointerHeld=false;shopDragInstance=0;pressedFormationOffer=-1;ClearDeploymentPreview();}
        void SelectFirstLineup()
        {
            if(Model==null)return;
            var selected=Model.Units.FirstOrDefault(u=>u.Instance==selectedPiece);
            if(selected==null&&selectedPiece>0&&Shop.Bench.Any(p=>p.Instance==selectedPiece)){SelectedSlot=-1;return;}
            selected=selected??Model.Units.FirstOrDefault();SelectedSlot=selected?.Slot??-1;selectedPiece=selected?.Instance??0;
        }
        void SelectShopPiece(int instance){selectedPiece=instance;SelectedSlot=Model.Units.FindIndex(u=>u.Instance==instance);}
        bool FindFormationReserveSeat(Vector2 point,out int seat)
        {
            if(FormationBenchRect.Contains(point))
            {
                seat = Mathf.Clamp(Mathf.FloorToInt((point.x - FormationBenchRect.x) / 104.3f),
                    0, ShopSeatLayout.ReserveCapacity - 1);
                return true;
            }
            seat=-1;return false;
        }
        int FormationOverflowSeatAt(Vector2 point)
        {
            if (FormationOverflowPanel.Contains(point))
                for (int index = OverflowFirst; index < OverflowFirst + VisibleOverflowCount; index++)
                    if (FormationOverflowRect(index).Contains(point)) return index;
            return -1;
        }
        bool FormationShopPointerDown(Vector2 point)
        {
            if(shopOddsVisible){shopInputBlockedFrame=Time.frameCount;if(!FormationOddsRect.Contains(point))shopOddsVisible=false;CancelFormationDrag();return true;}
            if ((marketVisible || ShopPanelTransitioning) && FormationMarketRect.Contains(point))
            {
                CancelFormationDrag();
                if (marketVisible && !ShopPanelTransitioning && CanTrade)
                    for (int slot = 0; slot < Shop.Offers.Count; slot++)
                        if (Shop.Offers[slot] != null && FormationOfferRect(slot).Contains(point))
                        {
                            pressedFormationOffer = slot;
                            break;
                        }
                return true;
            }
            if(!FindFormationReserveSeat(point,out int seat))
            {
                if (!FormationOverflowPanel.Contains(point)) return false;
                seat = FormationOverflowSeatAt(point);
            }
            int instance=Session.Seats.At(false,seat);
            if(instance>0){SelectShopPiece(instance);shopDragInstance=instance;PrepareDeploymentGrab(instance);}else CancelFormationDrag();
            return true;
        }
        bool FormationShopPointerUp(Vector2 point)
        {
            if(shopDragInstance<=0)return FormationShopBlocksPoint(point);
            if(FormationSellLeft.Contains(point)||FormationSellRight.Contains(point)){SellPiece(shopDragInstance);return true;}
            if(FindFormationReserveSeat(point,out int seat)){MovePiece(shopDragInstance,false,seat);return true;}
            return FormationShopBlocksPoint(point);
        }
        void ReleaseFormationOffer(Vector2 point)
        {
            int slot = pressedFormationOffer;
            pressedFormationOffer = -1;
            if (!marketVisible || ShopPanelTransitioning || shopOddsVisible || !CanTrade ||
                !FormationOfferRect(slot).Contains(point) || Vector2.Distance(point, pointerStart) > 6) return;
            var preview = Shop.PreviewPurchase(slot);
            if (Session.Seats.ReserveFull) notice = "备战席已满，请先上阵或出售角色。";
            else if (preview.Success && Shop.CanAfford(preview.Value)) BuyOffer(slot);
        }
        void PlaceFormationPiece(int instance,Vector2Int cell)
        {
            ShopAction(()=>Session.TryDeploy(instance,cell));
        }
        void DrawFormationPortrait(Rect rect,string id)=>ApprovedShopPresentation.DrawTestPortrait(rect,FormationUnitArt.For(id)?.Portrait);
        void OpenFormationOdds()
        {
            CancelFormationDrag();
            shopOddsVisible = true;
        }

        void DrawFormationMarket()
        {
            if (!marketVisible && !ShopPanelTransitioning) return;
            var panel = FormationMarketRect;
            float offset = -(1 - ShopPanelVisibility) * panel.height;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && marketVisible && !ShopPanelTransitioning;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0, offset, panel.width, panel.height), new Color(.10f, .15f, .19f, .94f));
                Fill(new Rect(0, offset, panel.width, MarketHeaderHeight - 4), ApprovedUi.Paper);
                EconomyLabel(new Rect(10, offset, 80, MarketHeaderHeight - 4), "招募商店", 14, ApprovedUi.Ink, true);
                EconomyLabel(new Rect(99, offset, 225, MarketHeaderHeight - 4), "点击卡牌购买", 11, ApprovedUi.Muted);
                if (EconomyButton(new Rect(panel.width - 96, offset + 4, 86, 20),
                    "收起  Esc", "关闭招募商店", ApprovedUi.Paper, ApprovedUi.Ink, 11))
                    SetMarketVisible(false);

                for (int slot = 0; slot < 5; slot++)
                {
                    var card = FormationOfferRect(slot);
                    card.position -= panel.position;
                    card.y += offset;
                    DrawFormationOffer(slot, card);
                }

                var control = FormationRefreshRect;
                control.position += new Vector2(-panel.x, offset - panel.y);
                string refresh = Shop.UnlimitedGold ? "免费刷新" : Shop.FreeRefreshes > 0
                    ? $"免费刷新  {Shop.FreeRefreshes}" : $"刷新  {Shop.RefreshPrice}金";
                bool trayEnabled = GUI.enabled;
                GUI.enabled = trayEnabled && CanTrade && !Shop.Locked &&
                    (Shop.UnlimitedGold || Shop.FreeRefreshes > 0 || Shop.CanAfford(Shop.RefreshPrice));
                if (EconomyButton(control, refresh, "返还未购买商品，再按当前牌库刷新", ApprovedUi.Gold, ApprovedUi.Ink, 12))
                    RefreshMarket();
                control.y += 35;
                GUI.enabled = trayEnabled && CanTrade;
                if (EconomyButton(control, Shop.Locked ? "已锁定 / 解锁" : "锁定商店", "锁定后过节点不自动刷新商店",
                    Shop.Locked ? ApprovedUi.Gold : ApprovedUi.Paper, ApprovedUi.Ink, 12))
                    Session.SetLocked(!Shop.Locked);
                control.y += 35;
                GUI.enabled = trayEnabled;
                if (EconomyButton(control, "招募概率", "查看各等级招募概率及当前库存影响",
                    ApprovedUi.Paper, ApprovedUi.Ink, 12))
                    OpenFormationOdds();
            }
            finally
            {
                GUI.EndGroup();
                GUI.enabled = enabled;
            }
        }

        void DrawFormationOffer(int slot, Rect card)
        {
            string id = Shop.Offers[slot];
            if (id == null)
            {
                Fill(card, new Color(.52f, .60f, .65f, .50f));
                ApprovedUi.Border(card, new Color(.78f, .84f, .88f, .55f));
                EconomyLabel(card, "空缺", 12, EconomyMuted, false, TextAnchor.MiddleCenter);
                return;
            }

            var role = Shop.Definition(id);
            var preview = Shop.PreviewPurchase(slot);
            bool affordable = Shop.CanAfford(role.Cost);
            bool reserveFull = Session.Seats.ReserveFull;
            bool hovered = GUI.enabled && CanTrade && affordable && !reserveFull && preview.Success && card.Contains(Event.current.mousePosition);
            approvedShop.DrawLoopCostBackground(card, role.Cost);
            DrawFormationPortrait(new Rect(card.x + card.width * .34f, card.y + 5, card.width * .64f, 108), id);
            Fill(new Rect(card.x, card.y + 73, card.width, card.height - 73), new Color(.09f, .15f, .20f, .91f));
            if (!affordable || reserveFull)
                Fill(card, new Color(.16f, .16f, .16f, .60f));
            if (hovered) ApprovedUi.Border(card, EconomyIce, pressedFormationOffer == slot ? 2 : 1);
            DrawFormationCoin(new Rect(card.x + 11, card.y + 12, 13, 13));
            EconomyLabel(new Rect(card.x + 29, card.y + 5, 36, 26), role.Cost.ToString(), 23, EconomyInk, true);

            int held = Shop.Bench.Where(p => p.Id == id).Sum(p => ShopTrialModel.Copies(p.Star));
            var status = new Rect(card.xMax - 65, card.y + 6, 59, 17);
            if (!affordable || reserveFull || !preview.Success)
            {
                Fill(status, EconomyPanel);
                EconomyLabel(status, reserveFull || !preview.Success ? "备战已满" : "金币不足", 10, EconomyMuted, true, TextAnchor.MiddleCenter);
            }
            else if (preview.FinalStar > 1)
                approvedShop.DrawApprovedUpgradeCue(status, preview.FinalStar);
            else if (held > 0)
            {
                Fill(status, ApprovedUi.Paper);
                EconomyLabel(status, "持有 " + held, 10, ApprovedUi.Ink, true, TextAnchor.MiddleCenter);
            }

            EconomyLabel(new Rect(card.x + 10, card.y + 74, card.width - 20, 22), role.Name,
                role.Name.Length > 9 ? 12 : 14, EconomyInk, true);

            var definition = FormationCampaignRules.Find(id);
            string tags = definition == null ? "" : string.Join(" / ", definition.Factions.Concat(definition.Styles).Take(2));
            EconomyLabel(new Rect(card.x + 10, card.yMax - 24, card.width - 20, 19), tags,
                tags.Length > 9 ? 9 : 10, EconomyMuted);
        }
        void DrawFormationResources(bool preparing)=>DrawFormationEconomyHud(preparing);
        void DrawFormationOdds()=>approvedShop.DrawShopOdds(formationShopPointer);
        void DrawFormationOverflow()
        {
            overflowPage = Mathf.Min(overflowPage, OverflowPages - 1);
            if (OverflowCount == 0) return;
            var panel = FormationOverflowPanel;
            Fill(panel, FormationPreparationPalette.Recess);
            ApprovedUi.Border(panel, FormationPreparationPalette.RustEdge);
            EconomyLabel(new Rect(panel.x + 7, panel.y + 2, 164, 19),
                $"备战溢出  {OverflowCount}", 12, FormationPreparationPalette.Text, true);
            if (OverflowPages > 1)
            {
                if (EconomyButton(new Rect(panel.xMax - 78, panel.y + 3, 19, 17), "‹", "上一页",
                    FormationPreparationPalette.Surface, FormationPreparationPalette.Text))
                    overflowPage = Mathf.Max(0, overflowPage - 1);
                EconomyLabel(new Rect(panel.xMax - 55, panel.y + 2, 30, 19), $"{overflowPage + 1}/{OverflowPages}",
                    10, FormationPreparationPalette.Text, false, TextAnchor.MiddleCenter);
                if (EconomyButton(new Rect(panel.xMax - 23, panel.y + 3, 19, 17), "›", "下一页",
                    FormationPreparationPalette.Surface, FormationPreparationPalette.Text))
                    overflowPage = Mathf.Min(OverflowPages - 1, overflowPage + 1);
            }
            var first = OverflowFirst;
            for (int index = first; index < first + VisibleOverflowCount; index++)
            {
                int instance = Session.Seats.At(false, index);
                var piece = Shop.Bench.FirstOrDefault(card => card.Instance == instance);
                if (piece == null) continue;
                var rect = FormationOverflowRect(index);
                var definition = Shop.Definition(piece.Id);
                Fill(rect, Color.Lerp(FormationPreparationPalette.Rust, FormationFeeColor(definition.Cost), .17f));
                if (!(pointerHeld && instance == shopDragInstance && DragPreviewVisible))
                    DrawFormationPortrait(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, 46), piece.Id);
                ApprovedUi.Border(rect, selectedPiece == instance ? EconomyIce : FormationPreparationPalette.RustEdge);
                EconomyLabel(new Rect(rect.x + 3, rect.y, 10, 13), definition.Cost.ToString(), 10, EconomyInk, true);
                approvedShop.DrawShopStars(new Rect(rect.xMax - 4 - piece.Star * 8, rect.y + 2, piece.Star * 8, 8),
                    piece.Star, new Color(1,.94f,.72f));
                Fill(new Rect(rect.x + 1, rect.yMax - 14, rect.width - 2, 13), FormationPreparationPalette.Recess);
                EconomyLabel(new Rect(rect.x + 2, rect.yMax - 14, rect.width - 4, 13), definition.Name,
                    definition.Name.Length > 6 ? 8 : 10, EconomyInk, false, TextAnchor.MiddleCenter);
            }
        }
        void DrawFormationPreparation()
        {
            approvedShop.DrawShopBench(approvedShop.LoopText(12,TextAnchor.MiddleCenter));
            if (Model.Phase == FormationPhase.Preparation) DrawFormationOverflow();
            if (Campaign != null && Model.Phase == FormationPhase.Preparation)
            {
                foreach (var piece in Shop.Bench)
                {
                    var slot = Session.Seats.For(piece.Instance);
                    if (!slot.HasValue || slot.Value.Lineup) continue;
                    var reserve = FormationReserveOrOverflowRect(slot.Value.Index);
                    if (reserve.width > 0)
                    {
                        bool regular = slot.Value.Index < ShopSeatLayout.ReserveCapacity;
                        DrawEquippedItemStrip(piece.Instance, reserve.position + new Vector2(4, regular ? 37 : 33), regular ? 17 : 12);
                    }
                }
            }
            if(Model.Phase!=FormationPhase.Preparation||marketVisible||ShopPanelTransitioning)return;
            bool supply=Campaign?.CurrentNode.Kind=="补给";
            bool enabled=GUI.enabled;GUI.enabled=enabled&&(supply||RosterArtReady)&&!CampaignInteractionBlocked;
            var start = FormationStartRect;
            bool overflowBlocked = !supply && OverflowCount > 0;
            GUI.enabled = GUI.enabled && openingGroundBall == null && !overflowBlocked;
            if (EconomyButton(start, supply ? "继续行军  ›" :
                overflowBlocked ? "先处理溢出" : RosterArtReady ? "开始进攻  ›" : "准备动作…",
                overflowBlocked ? "上阵或出售角色，清空溢出区后开战" :
                supply ? "补给结束后前往下一节点" : "锁定本场阵容并发起进攻", FormationPreparationPalette.Rust, FormationPreparationPalette.Text, 15))
            {
                if (supply) ContinueCampaign();
                else StartBattle();
            }
            Fill(new Rect(start.x, start.y, 3, start.height), GUI.enabled ? FormationPreparationPalette.RustEdge : FormationPreparationPalette.Line);
            GUI.enabled=enabled;
        }
        void DrawFormationDropTarget()
        {
            if(!pointerHeld||shopDragInstance<=0||Vector2.Distance(formationShopPointer,pointerStart)<=6||
                !FindFormationReserveSeat(formationShopPointer,out int seat))return;
            ApprovedUi.Border(FormationReserveRect(seat),new Color(.86f,.83f,.72f,.8f),1);
        }
        void DrawFormationSaleTargets()
        {
            var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);if(piece==null||!CanTrade)return;
            var quote=Shop.Quote(shopDragInstance);if(!quote.Success)return;
            foreach(var rect in new[]{FormationSellLeft,FormationSellRight})
            {
                bool hit=rect.Contains(formationShopPointer);
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
