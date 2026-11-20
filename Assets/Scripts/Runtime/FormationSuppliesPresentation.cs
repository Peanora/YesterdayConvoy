using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Dictionary<string, Texture2D> SupplyTextures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, string> OrbKeys = new Dictionary<string, string>
        {
            {"白", "white"}, {"蓝", "blue"}, {"金", "gold"}, {"彩", "prismatic"}
        };
        static readonly Rect RewardChoiceRect = new Rect(1104, 88, 94, 22);
        static readonly Rect RewardPageRect = new Rect(1104, 116, 94, 22);
        static readonly Rect EquipmentTilesViewport = new Rect(1206, 103, 65, 440);
        static readonly Rect EquipmentCombinationRect = new Rect(854, 223, 330, 197);
        const float EquipmentTileWidth = 47, EquipmentTileHeight = 44, EquipmentTilePitch = 50;
        Vector2 equipmentRailScroll;
        Vector2 equipmentPointerStart, equipmentPointer;
        int equipmentPressedItem, equipmentCombinationFirst, equipmentCombinationSecond;
        bool equipmentDragging;
        int inspectedEquipment;
        double equipmentDetailsOpenedAt;
        Rect equipmentDetailsAnchor;
        Vector2 equipmentDetailsScroll;
        FormationCampaignRun rewardPresentationRun;
        int rewardHistorySelected = -1;
        bool rewardHistoryOpen;

        internal static Texture2D SupplyItemArt(string key)
        {
            string trait = FormationCampaignRun.BadgeTrait(key);
            if (trait != null)
                return SynergyBadgePresentation.IconFor(FormationCampaignRules.FindRecord(trait)?.Name);
            return key != null && key.Length <= 2 ? SupplyArt("Items/" + key) : null;
        }

        internal static Texture2D SupplyOrbArt(string color)
            => color != null && OrbKeys.TryGetValue(color, out var id) ? SupplyArt("Orbs/" + id) : null;

        static Texture2D SupplyArt(string path)
        {
            if (!SupplyTextures.TryGetValue(path, out var texture))
            {
                texture = Resources.Load<Texture2D>("UI/FormationSupplies/" + path);
                SupplyTextures[path] = texture;
            }
            return texture;
        }

        void TickFormationRewards(float delta)
        {
            if (!ReferenceEquals(rewardPresentationRun, Campaign))
            {
                rewardPresentationRun = Campaign;
                ClearGroundRewards();
                groundRewardPage = 0;
                rewardHistoryOpen = false;
                rewardHistorySelected = -1;
                equipmentRailScroll = Vector2.zero;
                CancelFormationEquipmentInteraction();
                CloseFormationEquipmentDetails();
                campaignSelectedItem = campaignCombineItem = campaignSelectedPiece = 0;
            }
            if (!CanUseEquipmentTiles)
            {
                CancelFormationEquipmentInteraction();
                CloseFormationEquipmentDetails();
            }
            else if (inspectedEquipment > 0 && InspectedEquipment == null) CloseFormationEquipmentDetails();
            TickGroundRewards(delta);
        }

        bool FormationSupplyBlocksPoint(Vector2 point) => Campaign != null &&
            (Campaign.PendingRewardChoices>0 && RewardChoiceRect.Contains(point) ||
             GroundRewardPages>1 && RewardPageRect.Contains(point) ||
             EquipmentTileAt(point)!=null || FormationEquipmentDetailsRect.Contains(point) ||
             EquipmentCombinationPending && EquipmentCombinationRect.Contains(point) ||
             rewardHistoryOpen && RewardHistoryRect.Contains(point));

        bool CanUseEquipmentTiles => Campaign != null && Model.Phase == FormationPhase.Preparation &&
            !CampaignInteractionBlocked && !formationMenuOpen && !shopOddsVisible && !rewardHistoryOpen;

        internal bool EquipmentCombinationPending => equipmentCombinationFirst > 0;

        FormationCampaignRun.ItemInstance InspectedEquipment => inspectedEquipment == 0 ? null :
            Campaign?.AllEquipment.FirstOrDefault(item => item.Instance == inspectedEquipment);

        internal int FormationInspectedItemId => CanUseEquipmentTiles && InspectedEquipment != null ? inspectedEquipment : 0;

        internal Rect FormationEquipmentDetailsRect
        {
            get
            {
                if (FormationInspectedItemId == 0) return Rect.zero;
                const float width = 308, height = 181, gap = 8;
                float rightEdge = EquipmentTilesViewport.xMin - gap;
                float x = rightEdge - width;
                if (InspectedEquipment.Owner > 0 && FormationInspectionAvailable)
                {
                    var role = FormationInspectionRect;
                    x = role.xMax + gap + width <= rightEdge ? role.xMax + gap : role.xMin - gap - width;
                }
                float top = MarketVisible || ShopPanelTransitioning ? FormationMarketRect.yMax + gap : 85;
                return new Rect(x, Mathf.Clamp(equipmentDetailsAnchor.y, top, FormationExperienceRect.yMin - height - gap), width, height);
            }
        }

        bool CloseFormationEquipmentDetails()
        {
            if (inspectedEquipment == 0) return false;
            inspectedEquipment = 0;
            equipmentDetailsScroll = Vector2.zero;
            return true;
        }

        internal void ToggleFormationEquipmentDetails(int itemInstance, Rect anchor)
        {
            if (!CanUseEquipmentTiles) return;
            int previous = inspectedEquipment;
            CloseFormationEquipmentDetails();
            if (previous == itemInstance) return;
            var item = Campaign.AllEquipment.FirstOrDefault(entry => entry.Instance == itemInstance);
            if (item == null || item.Owner != 0 && item.Owner != formationInspectedInstance) return;
            CancelFormationDrag();
            CloseFormationRouteDetails();
            CloseFormationTraitDetails();
            inspectedEquipment = item.Instance;
            equipmentDetailsAnchor = anchor;
            equipmentDetailsOpenedAt = Time.unscaledTimeAsDouble;
        }

        internal bool CancelFormationEquipmentInteraction()
        {
            bool handled = equipmentPressedItem > 0 || EquipmentCombinationPending;
            equipmentPressedItem = equipmentCombinationFirst = equipmentCombinationSecond = 0;
            equipmentDragging = false;
            return handled;
        }

        internal bool ProcessFormationEquipmentPointer(EventType type, Vector2 point)
        {
            if (!CanUseEquipmentTiles) return false;
            if (type == EventType.MouseDown)
            {
                if (EquipmentCombinationPending && EquipmentCombinationRect.Contains(point)) return false;
                CancelFormationEquipmentInteraction();
                if (FormationTraitBlocksPoint(point) || FormationInspectionBlocksPoint(point) ||
                    OperationsBlocksPoint(point) || FormationEquipmentDetailsRect.Contains(point)) return false;
                var pressed = EquipmentTileAt(point);
                if (pressed == null) return false;
                CloseFormationInspection();
                CloseFormationRouteDetails();
                CloseFormationTraitDetails();
                equipmentPressedItem = pressed.Instance;
                equipmentPointerStart = equipmentPointer = point;
                CancelFormationDrag();
                return true;
            }
            if (equipmentPressedItem == 0) return false;
            equipmentPointer = point;
            equipmentDragging |= Vector2.Distance(point, equipmentPointerStart) > 6;
            if (equipmentDragging) CloseFormationEquipmentDetails();
            if (type == EventType.MouseUp)
            {
                int source = equipmentPressedItem;
                bool dragged = equipmentDragging;
                equipmentPressedItem = 0;
                equipmentDragging = false;
                var target = EquipmentTileAt(point);
                if (!dragged && target?.Instance == source)
                    ToggleFormationEquipmentDetails(source, EquipmentTileOnScreen(EquipmentTileIndex(point)));
                else if (dragged && target != null && PreviewEquipmentPair(source, target.Instance, out _))
                {
                    equipmentCombinationFirst = source;
                    equipmentCombinationSecond = target.Instance;
                }
                else if (dragged && target == null && CanTrade)
                {
                    int owner = FormationEquipmentDropTarget(point);
                    if (owner > 0 && Campaign.Inventory.Any(item => item.Instance == source))
                        CampaignAction(Campaign.Equip(source, owner));
                }
            }
            return true;
        }

        int FormationEquipmentDropTarget(Vector2 point)
        {
            if (FormationSupplyBlocksPoint(point) || FormationTraitBlocksPoint(point) ||
                FormationInspectionBlocksPoint(point) || OperationsBlocksPoint(point)) return 0;
            if (FindFormationSeat(point, out bool lineup, out int seat)) return Session.Seats.At(lineup, seat);
            return FormationUnitAt(point)?.Instance ?? 0;
        }

        bool PreviewEquipmentPair(int first,int second,out string key)
        {
            key = null;
            if (first == second || first <= 0 || second <= 0) return false;
            var inventory = Campaign.Inventory;
            return inventory.Any(item=>item.Instance==first) && inventory.Any(item=>item.Instance==second) &&
                Campaign.PreviewCombination(first,second,out key).Success;
        }

        internal bool ConfirmFormationEquipmentCombination()
        {
            if (!CanUseEquipmentTiles || !EquipmentCombinationPending ||
                !PreviewEquipmentPair(equipmentCombinationFirst,equipmentCombinationSecond,out _))
            {
                CancelFormationEquipmentInteraction();
                return false;
            }
            var result = Campaign.Combine(equipmentCombinationFirst,equipmentCombinationSecond);
            CancelFormationEquipmentInteraction();
            CampaignAction(result);
            return result.Success;
        }

        static Rect EquipmentTile(int index) => new Rect(0, index * EquipmentTilePitch, EquipmentTileWidth, EquipmentTileHeight);

        Rect EquipmentTileOnScreen(int index)
        {
            var slot = EquipmentTile(index);
            slot.position += EquipmentTilesViewport.position - equipmentRailScroll;
            return slot;
        }

        Vector2 EquipmentFlightTarget(int index)
        {
            Vector2 target = EquipmentTileOnScreen(index).center;
            target.y = Mathf.Clamp(target.y,EquipmentTilesViewport.yMin+EquipmentTileHeight/2,
                EquipmentTilesViewport.yMax-EquipmentTileHeight/2);
            return target;
        }

        int EquipmentTileIndex(Vector2 point) =>
            Mathf.FloorToInt((point.y - EquipmentTilesViewport.y + equipmentRailScroll.y) / EquipmentTilePitch);

        FormationCampaignRun.ItemInstance EquipmentTileAt(Vector2 point)
        {
            if (!EquipmentTilesViewport.Contains(point)) return null;
            int index = EquipmentTileIndex(point);
            var inventory = Campaign.Inventory;
            return index >= 0 && index < inventory.Count && EquipmentTileOnScreen(index).Contains(point) ? inventory[index] : null;
        }

        internal bool TryEquipmentTilePoint(int itemInstance,out Vector2 point)
        {
            point = default;
            var inventory = Campaign?.Inventory;
            if (inventory == null) return false;
            for (int index = 0; index < inventory.Count; index++)
            {
                if (inventory[index].Instance != itemInstance) continue;
                point = EquipmentTileOnScreen(index).center;
                return EquipmentTilesViewport.Contains(point);
            }
            return false;
        }

        void RevealEquipmentSlot(int index)
        {
            equipmentRailScroll.y = Mathf.Max(equipmentRailScroll.y,EquipmentTile(index).yMax-EquipmentTilesViewport.height);
        }

        void OpenFormationInventory()
        {
            if (Model.Phase != FormationPhase.Preparation) return;
            CancelFormationEquipmentInteraction();
            SetMarketVisible(false);
            CloseFormationTraitDetails();
            CloseFormationInspection();
            CloseFormationRouteDetails();
            CloseCampaignPanels();
            campaignInventoryOpen = true;
            campaignItemScroll = Vector2.zero;
            campaignSelectedItem = 0;
            campaignCombineItem = 0;
        }

        bool OpenFormationRewardHistory()
        {
            if (Campaign == null || CombatViewActive || Campaign.HasBlockingChoice || Campaign.BallRewards.Count == 0)
                return false;
            CancelFormationEquipmentInteraction();
            CloseCampaignPanels();
            CloseFormationTraitDetails();
            CloseFormationInspection();
            CloseFormationRouteDetails();
            SetMarketVisible(false);
            rewardHistoryOpen = true;
            rewardHistorySelected = Campaign.BallRewards.Count - 1;
            return true;
        }

        void DrawSupplyItem(Rect rect, string key)
        {
            var art = SupplyItemArt(key);
            if (art != null)
            {
                GUI.DrawTexture(rect, art, ScaleMode.ScaleToFit, true);
                return;
            }
            string symbol = key == "赤宝石" ? "◆" : key == "蓝宝石" ? "◆" : key == "复制器" ? "▣" : "?";
            Color color = key == "赤宝石" ? new Color(.94f, .44f, .43f) :
                key == "蓝宝石" ? new Color(.39f, .76f, 1f) : TraitInk;
            TraitLabel(rect, symbol, Mathf.Clamp(Mathf.RoundToInt(rect.height * .58f), 10, 25), color, true, TextAnchor.MiddleCenter);
        }

        void DrawEquippedItemSlot(Rect slot, FormationCampaignRun.ItemInstance item, bool active = false)
        {
            Fill(slot, OperationsRail);
            ApprovedUi.Border(slot, active ? OperationsGold : OperationsEdge);
            if (item != null)
                DrawSupplyItem(new Rect(slot.x + 2, slot.y + 2, slot.width - 4, slot.height - 4), item.Key);
        }

        void DrawEquippedItemStrip(int owner, Vector2 center, float size, bool alignLeft = false)
        {
            if (Campaign == null || owner == shopDragInstance && DragPreviewVisible) return;
            var equipment = Campaign.Equipment(owner);
            if (equipment.Count == 0) return;
            float pitch = size + 2;
            float x = alignLeft ? center.x : center.x - (pitch * equipment.Count - 2) * .5f;
            for (int i = 0; i < equipment.Count; i++)
                DrawEquippedItemSlot(new Rect(x + pitch * i, center.y, size, size), equipment[i]);
        }

        static Color BallAccent(string color) => color switch
        {
            "蓝" => new Color(.37f, .69f, 1),
            "金" => new Color(1, .77f, .36f),
            "彩" => new Color(.65f, .95f, .89f),
            _ => new Color(.87f, .92f, .96f)
        };

        static string BallRewardText(FormationCampaignRun.BallReward ball)
        {
            var rewards = new List<string>();
            if (ball.Gold != 0) rewards.Add("金币 +" + ball.Gold);
            if (ball.Refreshes != 0) rewards.Add("刷新券 +" + ball.Refreshes);
            rewards.AddRange(ball.Items.GroupBy(k => k).Select(group =>
                FormationCampaignRun.ItemName(group.Key) + (group.Count() > 1 ? " ×" + group.Count() : "")));
            if (ball.Recruit != null) rewards.Add(ball.Recruit);
            if (ball.Choice != null) rewards.Add(ball.Choice + "（待选）");
            return string.Join(" · ", rewards);
        }

        void DrawFormationSupplies()
        {
            if (Campaign == null) return;
            DrawRewardControls();
            var inventory = Campaign.Inventory;
            int countIncludingFlights = inventory.Count + PendingGroundRewardItems;
            if (countIncludingFlights == 0) { equipmentRailScroll = Vector2.zero; return; }
            float contentHeight = Mathf.Max(EquipmentTilesViewport.height,countIncludingFlights * EquipmentTilePitch);
            equipmentRailScroll = GUI.BeginScrollView(EquipmentTilesViewport,equipmentRailScroll,
                new Rect(0,0,EquipmentTileWidth,contentHeight));
            int first = Mathf.Max(0,Mathf.FloorToInt(equipmentRailScroll.y/EquipmentTilePitch));
            int selected = FormationInspectedItemId;
            for (int i = first; i < inventory.Count && EquipmentTile(i).y < equipmentRailScroll.y + EquipmentTilesViewport.height; i++)
            {
                var slot = EquipmentTile(i);
                var item = inventory[i];
                bool highlight = item.Instance == equipmentPressedItem || item.Instance == equipmentCombinationFirst ||
                    item.Instance == equipmentCombinationSecond || item.Instance == selected ||
                    equipmentDragging && EquipmentTileOnScreen(i).Contains(equipmentPointer) &&
                    PreviewEquipmentPair(equipmentPressedItem,item.Instance,out _);
                Fill(slot,OperationsRail);
                ApprovedUi.Border(slot,highlight?OperationsGold:OperationsEdge,highlight?2:1);
                DrawSupplyItem(new Rect(slot.x+4,slot.y+3,39,38),item.Key);
                GUI.Label(slot,new GUIContent(string.Empty,item.Name),GUIStyle.none);
            }
            GUI.EndScrollView();
        }

        void DrawFormationEquipmentOverlay()
        {
            if (!CanUseEquipmentTiles) return;
            if (EquipmentCombinationPending)
            {
                if (PreviewEquipmentPair(equipmentCombinationFirst,equipmentCombinationSecond,out string key))
                    DrawEquipmentCombination(key,equipmentCombinationFirst,equipmentCombinationSecond,true);
                else CancelFormationEquipmentInteraction();
                return;
            }
            DrawFormationEquipmentDetails();
            if (!equipmentDragging) return;
            var source = Campaign.Inventory.FirstOrDefault(item=>item.Instance==equipmentPressedItem);
            if (source == null) return;
            var target = EquipmentTileAt(equipmentPointer);
            if (target != null && PreviewEquipmentPair(source.Instance,target.Instance,out string combined))
                DrawEquipmentCombination(combined,source.Instance,target.Instance,false);
            DrawSupplyItem(new Rect(equipmentPointer.x-21,equipmentPointer.y-23,42,42),source.Key);
        }

        void DrawFormationEquipmentDetails()
        {
            var bounds = FormationEquipmentDetailsRect;
            if (bounds.width <= 0) return;
            var item = InspectedEquipment;
            var panel = BeginFormationDetailReveal(bounds, equipmentDetailsOpenedAt, 13, out var previous);
            try
            {
                DrawFormationDetailFrame(panel);
                DrawSupplyItem(new Rect(panel.x + 13, panel.y + 10, 40, 40), item.Key);
                TraitLabel(new Rect(panel.x + 61, panel.y + 7, panel.width - 104, 27), item.Name, 19, OperationsInk, true);
                string category = FormationCampaignRun.BadgeTrait(item.Key) != null ? "羁绊徽章" : item.Key.Length switch
                {
                    1 => "基础装备",
                    2 => "进阶装备",
                    _ => "道具"
                };
                string status = item.Owner == 0 ? (category == "道具" ? "物品栏" : "未装备") : "已装备 · " + CampaignPieceName(item.Owner);
                TraitLabel(new Rect(panel.x + 61, panel.y + 34, panel.width - 74, 18),
                    category + "  /  " + status, 11, OperationsMuted);
                if (FormationDetailClosePressed(panel, "关闭装备详情")) { CloseFormationEquipmentDetails(); return; }

                var description = new GUIContent(CampaignItemDescription(item.Key));
                var style = TraitText(13, FormationPreparationPalette.Ink, false, TextAnchor.UpperLeft);
                var body = new Rect(panel.x + 13, panel.y + 73, panel.width - 22, panel.height - 83);
                float width = body.width - 17;
                float height = Mathf.Max(body.height, style.CalcHeight(description, width));
                equipmentDetailsScroll = GUI.BeginScrollView(body, equipmentDetailsScroll, new Rect(0, 0, width, height));
                GUI.Label(new Rect(0, 0, width, height), description, style);
                GUI.EndScrollView();
            }
            finally { EndFormationDetailReveal(previous); }
        }

        void DrawEquipmentCombination(string key,int first,int second,bool confirm)
        {
            var r = EquipmentCombinationRect;
            var inventory = Campaign.Inventory;
            var source = inventory.First(item=>item.Instance==first);
            var target = inventory.First(item=>item.Instance==second);
            Fill(r,OperationsSurface);
            ApprovedUi.Border(r,OperationsEdge);
            DrawSupplyItem(new Rect(r.x+14,r.y+12,40,40),key);
            TraitLabel(new Rect(r.x+64,r.y+11,r.width-77,22),FormationCampaignRun.ItemName(key),17,OperationsInk,true);
            TraitLabel(new Rect(r.x+64,r.y+34,r.width-77,18),source.Name+" + "+target.Name,11,OperationsMuted);
            Fill(new Rect(r.x+14,r.y+60,r.width-28,1),OperationsEdge);
            var text = TraitText(13,OperationsInk,false,TextAnchor.UpperLeft);
            text.wordWrap = true;
            GUI.Label(new Rect(r.x+14,r.y+69,r.width-28,83),CampaignItemDescription(key),text);
            if (!confirm) return;
            if (OperationsButton(new Rect(r.x+95,r.yMax-36,94,26),"取消"))
                CancelFormationEquipmentInteraction();
            if (EconomyButton(new Rect(r.x+198,r.yMax-36,117,26),"确认合成","消耗这两件装备",
                OperationsGold,OperationsSurface,13))
                ConfirmFormationEquipmentCombination();
        }

        void DrawRewardControls()
        {
            if (Campaign.PendingRewardChoices>0 && Button(RewardChoiceRect,"待选 "+Campaign.PendingRewardChoices,openingGroundBall==null))
                OpenGroundRewardChoices();
            if (GroundRewardPages>1)
            {
                var page = RewardPageRect;
                if (Button(new Rect(page.x,page.y,24,page.height),"‹",groundRewardPage>0)) groundRewardPage--;
                TraitLabel(new Rect(page.x+26,page.y,42,page.height),(groundRewardPage+1)+"/"+GroundRewardPages,11,
                    OperationsInk,false,TextAnchor.MiddleCenter);
                if (Button(new Rect(page.xMax-24,page.y,24,page.height),"›",groundRewardPage+1<GroundRewardPages)) groundRewardPage++;
            }
            if (rewardHistoryOpen) DrawRewardHistory();
        }

        static Rect RewardHistoryRect => new Rect(953, 222, 244, 178);
        void DrawRewardHistory()
        {
            var bounds = RewardHistoryRect;
            var balls = Campaign.BallRewards;
            int index = Mathf.Clamp(rewardHistorySelected, 0, balls.Count - 1);
            var ball = balls[index];
            Fill(bounds, OperationsSurface);
            ApprovedUi.Border(bounds, OperationsEdge);
            TraitLabel(new Rect(bounds.x + 12, bounds.y + 8, 180, 24), "奖励球记录", 15, TraitInk, true);
            var close = new Rect(bounds.xMax - 32, bounds.y + 8, 22, 22);
            TraitLabel(close, "×", 17, TraitMuted, false, TextAnchor.MiddleCenter);
            if (TraitHit(close)) rewardHistoryOpen = false;
            var icon = SupplyOrbArt(ball.Color);
            if (icon != null) GUI.DrawTexture(new Rect(bounds.x + 12, bounds.y + 37, 40, 40), icon, ScaleMode.ScaleToFit, true);
            TraitLabel(new Rect(bounds.x + 59, bounds.y + 37, 172, 28), ball.Node + " · " + (ball.Color.Length==0?"装备补足":ball.Color+"球"), 13, BallAccent(ball.Color), true);
            TraitLabel(new Rect(bounds.x + 14, bounds.y + 76, 216, 63), BallRewardText(ball), 12, TraitInk);
            if (Button(new Rect(bounds.x + 14, bounds.yMax - 32, 38, 23), "‹", index > 0)) rewardHistorySelected--;
            if (Button(new Rect(bounds.xMax - 52, bounds.yMax - 32, 38, 23), "›", index < balls.Count - 1)) rewardHistorySelected++;
            TraitLabel(new Rect(bounds.x + 62, bounds.yMax - 32, 120, 23), (index + 1) + " / " + balls.Count, 11, TraitMuted, false, TextAnchor.MiddleCenter);
        }
    }
}
