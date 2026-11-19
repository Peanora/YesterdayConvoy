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
        static readonly Rect RewardRail = new Rect(1011, 66, 252, 62);
        static readonly Rect EquipmentRail = new Rect(1011, 134, 252, 121);
        Vector2 equipmentRailScroll;
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
                rewardHistoryOpen = false;
                rewardHistorySelected = -1;
                equipmentRailScroll = Vector2.zero;
                campaignSelectedItem = campaignCombineItem = campaignSelectedPiece = 0;
            }
            TickGroundRewards(delta);
        }

        bool FormationSupplyBlocksPoint(Vector2 point) => Campaign != null &&
            (RewardRail.Contains(point) || EquipmentRail.Contains(point) ||
             rewardHistoryOpen && RewardHistoryRect.Contains(point));

        void OpenFormationInventory(int item = 0)
        {
            if (Model.Phase != FormationPhase.Preparation) return;
            SetMarketVisible(false);
            CloseFormationTraitDetails();
            CloseCampaignPanels();
            campaignInventoryOpen = true;
            campaignItemScroll = Vector2.zero;
            campaignSelectedItem = item;
            campaignCombineItem = 0;
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
            DrawRewardRail();
            var inventory = Campaign.Inventory;
            Fill(EquipmentRail, TraitPanel);
            ApprovedUi.Border(EquipmentRail, TraitLine);
            TraitLabel(new Rect(1020, 138, 150, 20), "装备  " + inventory.Count, 14, TraitInk, true);
            var manage = new Rect(1188, 139, 66, 18);
            TraitLabel(manage, "装备与合成 ›", 10, TraitMuted, false, TextAnchor.MiddleRight);
            if (Model.Phase == FormationPhase.Preparation && TraitHit(manage, "打开装备与合成")) OpenFormationInventory();

            var grid = new Rect(1019, 163, 237, 84);
            int rows = Mathf.Max(2, Mathf.CeilToInt(inventory.Count / 5f));
            if (openingGroundBall != null) rows = Mathf.Max(rows, Mathf.CeilToInt((inventory.Count + groundPayout.Items.Count) / 5f));
            var scroll = GUI.BeginScrollView(grid, equipmentRailScroll, new Rect(0, 0, 220, rows * 42));
            if (openingGroundBall == null) equipmentRailScroll = scroll;
            for (int i = 0; i < rows * 5; i++)
            {
                var slot = new Rect(i % 5 * 44, i / 5 * 42 + 1, 40, 39);
                Fill(slot, TraitCard);
                ApprovedUi.Border(slot, TraitLine);
                if (i >= inventory.Count) continue;
                var item = inventory[i];
                DrawSupplyItem(new Rect(slot.x + 3, slot.y + 2, 34, 34), item.Key);
                if (Model.Phase == FormationPhase.Preparation && TraitHit(slot, item.Name))
                    OpenFormationInventory(item.Instance);
            }
            GUI.EndScrollView();
        }

        void DrawRewardRail()
        {
            Fill(RewardRail, TraitPanel);
            ApprovedUi.Border(RewardRail, TraitLine);
            var balls = Campaign.BallRewards;
            TraitLabel(new Rect(1020, 73, 225, 20), "地图战利品", 14, TraitInk, true);
            string text = HasGroundRewards ? "红门右侧 · " + Campaign.PendingBalls.Count + " 颗待领取" :
                balls.Count > 0 ? "已开 " + balls.Count + " 个 · 查看记录" : "奖励球掉落后可在红门右侧领取";
            TraitLabel(new Rect(1020, 95, 230, 24), text, 11, TraitMuted);
            if (balls.Count > 0 && TraitHit(RewardRail, "查看本局奖励球记录"))
            {
                rewardHistoryOpen = !rewardHistoryOpen;
                if (rewardHistoryOpen) rewardHistorySelected = balls.Count - 1;
                CancelFormationDrag();
            }
            if (rewardHistoryOpen) DrawRewardHistory();
        }

        static Rect RewardHistoryRect => new Rect(760, 67, 244, 178);
        void DrawRewardHistory()
        {
            var bounds = RewardHistoryRect;
            var balls = Campaign.BallRewards;
            int index = Mathf.Clamp(rewardHistorySelected, 0, balls.Count - 1);
            var ball = balls[index];
            Fill(bounds, TraitPanel);
            ApprovedUi.Border(bounds, TraitLine);
            TraitLabel(new Rect(bounds.x + 12, bounds.y + 8, 180, 24), "奖励球记录", 15, TraitInk, true);
            var close = new Rect(bounds.xMax - 32, bounds.y + 8, 22, 22);
            TraitLabel(close, "×", 17, TraitMuted, false, TextAnchor.MiddleCenter);
            if (TraitHit(close)) rewardHistoryOpen = false;
            var icon = SupplyOrbArt(ball.Color);
            if (icon != null) GUI.DrawTexture(new Rect(bounds.x + 12, bounds.y + 37, 40, 40), icon, ScaleMode.ScaleToFit, true);
            TraitLabel(new Rect(bounds.x + 59, bounds.y + 37, 172, 28), ball.Node + " · " + ball.Color + "球", 13, BallAccent(ball.Color), true);
            TraitLabel(new Rect(bounds.x + 14, bounds.y + 76, 216, 63), BallRewardText(ball), 12, TraitInk);
            if (Button(new Rect(bounds.x + 14, bounds.yMax - 32, 38, 23), "‹", index > 0)) rewardHistorySelected--;
            if (Button(new Rect(bounds.xMax - 52, bounds.yMax - 32, 38, 23), "›", index < balls.Count - 1)) rewardHistorySelected++;
            TraitLabel(new Rect(bounds.x + 62, bounds.yMax - 32, 120, 23), (index + 1) + " / " + balls.Count, 11, TraitMuted, false, TextAnchor.MiddleCenter);
        }
    }
}
