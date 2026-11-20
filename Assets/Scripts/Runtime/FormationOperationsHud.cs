using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color OperationsSurface = FormationPreparationPalette.Surface;
        static readonly Color OperationsRail = FormationPreparationPalette.Raised;
        static readonly Color OperationsEdge = FormationPreparationPalette.Line;
        static readonly Color OperationsInk = FormationPreparationPalette.Text;
        static readonly Color OperationsMuted = FormationPreparationPalette.Muted;
        static readonly Color OperationsFocus = new Color32(211, 207, 193, 255);
        static readonly Color OperationsGold = FormationPreparationPalette.Amber;
        static readonly Rect OperationsRouteRect = new Rect(210, 9, 787, 58);
        internal static readonly Rect FormationStrategyHistoryAnchor = new Rect(527, 76, 228, 27);
        static readonly Rect OperationsExitRect = new Rect(14, 20, 58, 36);
        static readonly Rect OperationsScoutRect = new Rect(83, 20, 113, 36);
        internal static readonly Rect FormationScoutRect = new Rect(1124, 435, 56, 58);
        static readonly Rect OperationsMenuRect = new Rect(1215, 20, 49, 36);
        static readonly int[][] OperationsPlanes = FormationCampaignRules.Nodes
            .Select((node, index) => new { node.Plane, Index = index })
            .GroupBy(node => node.Plane).OrderBy(group => group.Key)
            .Select(group => group.Select(node => node.Index).ToArray()).ToArray();
        int formationInspectedNode = -1;
        bool formationExitOpen, campaignScoutOwn;
        internal bool FormationStrategyHistoryOpen { get; private set; }
        Texture2D operationsNodeIcons;

        int[] OperationsPlaneNodes => OperationsPlanes[Campaign.CurrentNode.Plane - 1];
        bool FormationStrategyHistoryAvailable => Campaign != null && Campaign.SynergyTrial == null && !CombatViewActive &&
            !MarketVisible && !ShopPanelTransitioning;
        bool FormationScoutAvailable => Model?.Phase == FormationPhase.Preparation &&
            !CampaignInteractionBlocked && !formationMenuOpen && !shopOddsVisible && !rewardHistoryOpen;

        public Rect FormationRouteNodeRect(int nodeIndex)
        {
            if (Campaign == null || Campaign.SynergyTrial != null) return Rect.zero;
            var nodes = OperationsPlaneNodes;
            int slot = Array.IndexOf(nodes, nodeIndex);
            if (slot < 0) return Rect.zero;
            float width = (OperationsRouteRect.width - 91) / nodes.Length;
            return new Rect(OperationsRouteRect.x + 83 + slot * width, 12, width, 53);
        }

        Rect FormationRouteDetailsRect
        {
            get
            {
                var anchor = FormationRouteNodeRect(formationInspectedNode);
                if (anchor.width <= 0) return Rect.zero;
                return new Rect(Mathf.Clamp(anchor.center.x - 147, 15, 971),
                    MarketVisible ? FormationMarketRect.yMax + 6 : FormationStrategyHistoryAnchor.yMax + 8, 294, 113);
            }
        }

        bool CloseFormationRouteDetails()
        {
            if (formationInspectedNode < 0) return false;
            formationInspectedNode = -1;
            return true;
        }

        bool OperationsBlocksPoint(Vector2 point) =>
            (formationInspectedNode >= 0 && FormationRouteDetailsRect.Contains(point)) ||
            (FormationStrategyHistoryAvailable && FormationStrategyHistoryAnchor.Contains(point)) ||
            (FormationScoutAvailable && FormationScoutRect.Contains(point));

        void OperationsLabel(Rect rect, string value, int size, Color color, bool bold = false,
            TextAnchor align = TextAnchor.MiddleLeft) =>
            GUI.Label(rect, value, approvedShop.LoopText(size, align, color, bold ? 650 : 400));

        bool OperationsButton(Rect rect, string text, bool active = false)
        {
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            if (hover || active)
                Fill(rect, active ? FormationPreparationPalette.Selected : FormationPreparationPalette.Hover);
            OperationsLabel(rect, text, 13, GUI.enabled ? OperationsInk : OperationsMuted, false, TextAnchor.MiddleCenter);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        void DrawFormationScoutControl()
        {
            if (!FormationScoutAvailable) return;
            var rect = FormationScoutRect;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && openingGroundBall == null;
            bool hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Fill(rect, hovered ? FormationPreparationPalette.Hover : OperationsSurface);
            ApprovedUi.Border(rect, hovered ? OperationsGold : FormationPreparationPalette.Edge);
            var label = ViewingDefenders ? "返回布阵" : "查看守军";
            OperationsLabel(new Rect(rect.x, rect.y, rect.width, 36), ViewingDefenders ? "‹" : "›", 32,
                GUI.enabled ? OperationsGold : OperationsMuted, true, TextAnchor.MiddleCenter);
            OperationsLabel(new Rect(rect.x, rect.y + 35, rect.width, 17), label, 11,
                GUI.enabled ? OperationsInk : OperationsMuted, false, TextAnchor.MiddleCenter);
            if (GUI.Button(rect, new GUIContent(string.Empty, label + " [V]"), GUIStyle.none)) ToggleFormationView();
            GUI.enabled = enabled;
        }

        void DrawFormationOperationsHeader()
        {
            Fill(new Rect(0, 0, 1280, 77), OperationsSurface);
            Fill(new Rect(0, 75, 1280, 2), FormationPreparationPalette.Edge);
            if (OperationsButton(OperationsExitRect, "‹ 退出"))
            {
                CloseCampaignPanels();
                SetMarketVisible(false);
                CloseFormationTraitDetails();
                formationExitOpen = true;
            }

            var scout = OperationsScoutRect;
            bool scoutHover = GUI.enabled && scout.Contains(Event.current.mousePosition);
            if (scoutHover) Fill(scout, OperationsRail);
            Fill(new Rect(scout.x, scout.y + 2, 1, 32), OperationsEdge);
            OperationsLabel(new Rect(scout.x + 10, scout.y, 73, 20), "怪物信息", 12, OperationsInk, true);
            OperationsLabel(new Rect(scout.x + 10, scout.y + 19, 82, 16), "阵容与属性", 9, OperationsMuted);
            OperationsLabel(new Rect(scout.xMax - 26, scout.y, 23, 36),
                Model.RemainingDefenders.ToString(), 19, OperationsFocus, true, TextAnchor.MiddleRight);
            if (GUI.Button(scout, GUIContent.none, GUIStyle.none))
            {
                OpenFormationOperationsPanel();
                campaignScoutOwn = false;
                campaignScoutScroll = Vector2.zero;
                campaignScoutingOpen = true;
            }

            var trial = Campaign.SynergyTrial;
            var life = new Rect(1006, 20, 86, 36);
            Fill(new Rect(life.x, life.y + 2, 1, 32), OperationsEdge);
            if (trial != null || Campaign.IsPramanixBossTrial)
            {
                var panel = OperationsRouteRect;
                OperationsLabel(new Rect(panel.x + 12, panel.y + 7, panel.width - 24, 26),
                    trial == null ? "圣聆初雪 · 首领试玩" : trial.Trait.Name + " · 羁绊试玩", 18, OperationsGold, true);
                OperationsLabel(new Rect(panel.x + 12, panel.y + 35, panel.width - 24, 18), trial?.Hint ?? PramanixTrialHint, 12, OperationsInk);
                OperationsLabel(life, "无限金币", 12, OperationsMuted, false, TextAnchor.MiddleCenter);
            }
            else
            {
                DrawFormationNodeRail();
                OperationsLabel(new Rect(life.x + 8, life.y, 19, 35), "♥", 15, new Color32(226, 163, 150, 255));
                OperationsLabel(new Rect(life.x + 28, life.y, 61, 15), "队伍生命", 10, OperationsMuted);
                OperationsLabel(new Rect(life.x + 28, life.y + 13, 58, 24), Campaign.SquadHP.ToString(), 23, OperationsInk, true);
            }
            bool preparing = Model.Phase == FormationPhase.Preparation;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && preparing;
            if (OperationsButton(new Rect(1096, 20, 58, 36), "搭档"))
            { OpenFormationOperationsPanel(); campaignFormationOpen = true; }
            GUI.enabled = enabled;
            if (OperationsButton(new Rect(1155, 20, 59, 36), trial != null ? "换羁绊" : "记录"))
            {
                OpenFormationOperationsPanel();
                if (trial != null) OpenFormationTrialPicker(FormationMenuPage.SynergyTrials);
                else campaignRouteOpen = true;
            }
            Fill(new Rect(1154, 27, 1, 22), OperationsEdge);
            Fill(new Rect(1214, 27, 1, 22), OperationsEdge);
        }

        void OpenFormationOperationsPanel()
        {
            CloseCampaignPanels();
            CloseFormationTraitDetails();
            SetMarketVisible(false);
        }

        FormationCampaignRules.DataRecord FormationStrategyAt(int slot)
        {
            var augments = Campaign.AugmentIds;
            string id = slot == 0 ? Campaign.PreplanId : slot <= augments.Count ? augments[slot - 1] : null;
            return id == null ? null : FormationCampaignRules.FindRecord(id);
        }

        void DrawFormationStrategyHistoryToggle()
        {
            if (!FormationStrategyHistoryAvailable) return;
            var r = FormationStrategyHistoryAnchor;
            bool open = FormationStrategyHistoryOpen;
            bool hover = GUI.enabled && r.Contains(Event.current.mousePosition);
            Fill(r, open || hover ? OperationsRail : OperationsSurface);
            ApprovedUi.Border(r, open || hover ? OperationsFocus : OperationsEdge);
            for (int slot = 0; slot < 4; slot++)
            {
                var icon = new Rect(r.x + 8 + 28 * slot, r.y + 3, 24, 21);
                var strategy = FormationStrategyAt(slot);
                if (strategy == null)
                {
                    Fill(icon, FormationPreparationPalette.Hover);
                    ApprovedUi.Border(icon, OperationsEdge);
                    OperationsLabel(icon, "·", 11, OperationsMuted, false, TextAnchor.MiddleCenter);
                }
                else DrawFormationStrategyIcon(icon, strategy);
            }
            Fill(new Rect(r.x + 118, r.y + 5, 1, 17), OperationsEdge);
            var label = new Rect(r.x + 125, r.y, 76, r.height);
            OperationsLabel(label, "本局策略", 12, GUI.enabled ? OperationsInk : OperationsMuted, true, TextAnchor.MiddleCenter);
            OperationsLabel(new Rect(r.xMax - 22, r.y, 16, r.height), open ? "−" : "+", 13, OperationsGold,
                true, TextAnchor.MiddleCenter);
            if (!GUI.Button(r, GUIContent.none, GUIStyle.none)) return;
            if (!open) OpenFormationOperationsPanel();
            FormationStrategyHistoryOpen = !open;
        }

        void DrawFormationStrategyIcon(Rect icon, FormationCampaignRules.DataRecord strategy)
        {
            EnsureStrategyArt();
            int column = strategy.Tier switch { "银" => 1, "金" => 2, "彩" => 3, _ => 0 };
            if (strategyPlates != null)
                GUI.DrawTextureWithTexCoords(icon, strategyPlates, new Rect(column / 4f, .73f, .25f, .27f));
            else Fill(icon, StrategyGlints[column]);
            if (strategyProps != null)
            {
                int art = StrategyIllustration(strategy.Id, strategy.Kind);
                float side = Mathf.Min(icon.width, icon.height) * 1.12f;
                var image = new Rect(icon.center.x - side / 2, icon.center.y - side / 2, side, side);
                GUI.DrawTextureWithTexCoords(image, strategyProps, StrategyTile(art % 3, art / 3, 3, 2));
            }
            else OperationsLabel(icon, "◆", 12, StrategyInk[column], true, TextAnchor.MiddleCenter);
        }

        void DrawFormationStrategyHistory()
        {
            DrawFormationStrategyHistoryToggle();
            if (!FormationStrategyHistoryOpen) return;
            const float width = 510, contentWidth = 435;
            float left = FormationStrategyHistoryAnchor.center.x - width / 2;
            float top = FormationStrategyHistoryAnchor.yMax + 4;
            var body = CampaignText(13);
            body.normal.textColor = OperationsMuted;
            float height = 46;
            int count = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                var strategy = FormationStrategyAt(slot);
                if (strategy == null) continue;
                height += Mathf.Max(54, body.CalcHeight(new GUIContent(strategy.Effect), contentWidth) + 34);
                count++;
            }
            var panel = new Rect(left, top, width, height + (count == 0 ? 43 : 8));
            Fill(panel, OperationsSurface);
            ApprovedUi.Border(panel, OperationsEdge);
            Fill(new Rect(panel.x, panel.y, panel.width, 2), OperationsGold);
            OperationsLabel(new Rect(left + 14, top + 7, 370, 30), "本局已选策略", 16, OperationsInk, true);
            if (OperationsButton(new Rect(panel.xMax - 36, top + 6, 27, 28), "×"))
                FormationStrategyHistoryOpen = false;

            if (count == 0)
                OperationsLabel(new Rect(left + 14, top + 42, width - 28, 31), "本局还未确认任何策略。", 13, OperationsMuted);
            float y = top + 41;
            for (int slot = 0; slot < 4; slot++)
            {
                var strategy = FormationStrategyAt(slot);
                if (strategy == null) continue;
                var effect = new GUIContent(strategy.Effect);
                float rowHeight = Mathf.Max(54, body.CalcHeight(effect, contentWidth) + 34);
                Fill(new Rect(left + 12, y, width - 24, 1), OperationsEdge);
                DrawFormationStrategyIcon(new Rect(left + 13, y + 9, 39, 36), strategy);
                OperationsLabel(new Rect(left + 62, y + 6, 270, 22), strategy.Name, 15, OperationsInk, true);
                OperationsLabel(new Rect(panel.xMax - 147, y + 6, 133, 22),
                    slot == 0 ? "开局预案" : $"{strategy.Tier}档战备 · {strategy.Kind}", 11,
                    strategy.Tier == "银" ? StrategyGlints[1] : strategy.Tier == "彩" ? StrategyGlints[3] : OperationsGold,
                    false, TextAnchor.MiddleRight);
                GUI.Label(new Rect(left + 62, y + 29, contentWidth, rowHeight - 31), effect, body);
                y += rowHeight;
            }

            var input = Event.current;
            if (GUI.enabled && input.type == EventType.MouseUp && input.button == 0 &&
                !panel.Contains(input.mousePosition) && !FormationStrategyHistoryAnchor.Contains(input.mousePosition))
            {
                FormationStrategyHistoryOpen = false;
                input.Use();
            }
        }

        void DrawFormationNodeRail()
        {
            if (operationsNodeIcons == null)
                operationsNodeIcons = Resources.Load<Texture2D>("formation-node-icons");
            var panel = OperationsRouteRect;
            Fill(panel, OperationsSurface);
            Fill(new Rect(panel.x, panel.yMax - 1, panel.width, 1), OperationsEdge);
            OperationsLabel(new Rect(panel.x + 12, panel.y + 11, 66, 19), $"位面 {Campaign.CurrentNode.Plane}", 13, OperationsInk, true);
            OperationsLabel(new Rect(panel.x + 12, panel.y + 30, 66, 15),
                Campaign.IsPractice ? "难度试玩" : Model.Phase == FormationPhase.Preparation ? "整备阶段" : "本关结算",
                10, OperationsMuted);
            Fill(new Rect(panel.x + 73, panel.y + 13, 1, 32), OperationsEdge);
            var nodes = OperationsPlaneNodes;
            float first = FormationRouteNodeRect(nodes[0]).center.x, last = FormationRouteNodeRect(nodes[nodes.Length - 1]).center.x;
            Fill(new Rect(first, 25, last - first, 1), OperationsEdge);
            if (Campaign.NodeIndex > nodes[0])
                Fill(new Rect(first, 25, FormationRouteNodeRect(Campaign.NodeIndex).center.x - first, 1), OperationsGold);
            foreach (int index in nodes)
            {
                var node = FormationCampaignRules.Nodes[index];
                var r = FormationRouteNodeRect(index);
                var badge = new Rect(r.center.x - 15, 14, 30, 23);
                bool current = Campaign.NodeIndex == index, selected = formationInspectedNode == index;
                var tint = current ? OperationsSurface : NodeColor(node.Kind);
                Fill(badge, current ? OperationsGold : OperationsSurface);
                ApprovedUi.Border(badge, selected || GUI.enabled && r.Contains(Event.current.mousePosition)
                    ? OperationsFocus : current ? OperationsGold : FormationPreparationPalette.Edge);
                DrawNodeMark(badge.center, node.Kind, tint);
                string label = node.Kind == "战斗战备" ? "战备" : node.Kind;
                OperationsLabel(new Rect(r.x, 38, r.width, 15), label, 12,
                    current ? OperationsGold : OperationsInk, true, TextAnchor.MiddleCenter);
                OperationsLabel(new Rect(r.x, 52, r.width, 12), node.Id, 10,
                    OperationsMuted, false, TextAnchor.MiddleCenter);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) ToggleFormationRouteNode(index);
            }
        }

        internal void ToggleFormationRouteNode(int index)
        {
            if (FormationRouteNodeRect(index).width <= 0 || CampaignInteractionBlocked || formationMenuOpen || CombatViewActive) return;
            bool open = formationInspectedNode != index;
            CloseFormationInspection();
            CloseFormationTraitDetails();
            CancelFormationEquipmentInteraction();
            CloseFormationEquipmentDetails();
            CancelFormationDrag();
            formationInspectedNode = open ? index : -1;
        }

        static Color NodeColor(string kind) => kind switch
        {
            "奖励" => OperationsGold,
            "补给" => new Color32(151, 218, 219, 255),
            "遭遇" => new Color32(241, 179, 142, 255),
            "首领" => new Color32(240, 181, 203, 255),
            _ => OperationsInk
        };

        void DrawNodeMark(Vector2 center, string kind, Color tint)
        {
            if (operationsNodeIcons == null) return;
            int icon = kind switch
            {
                "遭遇" => 1,
                "奖励" => 2,
                "补给" => 3,
                "战斗战备" => 4,
                "首领" => 5,
                _ => 0
            };
            var uv = new Rect(icon % 3 / 3f, (1 - icon / 3) / 2f, 1f / 3, .5f);
            var previous = GUI.color;
            GUI.color = previous * tint;
            GUI.DrawTextureWithTexCoords(new Rect(center.x - 11, center.y - 11, 22, 22), operationsNodeIcons, uv);
            GUI.color = previous;
        }

        void DrawFormationRouteDetails()
        {
            if (Campaign == null || formationInspectedNode < 0) return;
            var r = FormationRouteDetailsRect;
            if (r.width <= 0) { CloseFormationRouteDetails(); return; }
            var node = FormationCampaignRules.Nodes[formationInspectedNode];
            Fill(r, OperationsSurface);
            ApprovedUi.Border(r, OperationsEdge);
            Fill(new Rect(r.x, r.y, 2, r.height), OperationsFocus);
            OperationsLabel(new Rect(r.x + 12, r.y + 5, 210, 17), formationInspectedNode == Campaign.NodeIndex
                ? "当前节点" : formationInspectedNode < Campaign.NodeIndex ? "已完成" : "未到达", 10, OperationsMuted);
            OperationsLabel(new Rect(r.x + 12, r.y + 21, 235, 24), $"{node.Id} · {node.Kind}", 16, OperationsInk, true);
            if (OperationsButton(new Rect(r.xMax - 31, r.y + 5, 23, 23), "×")) CloseFormationRouteDetails();
            Fill(new Rect(r.x + 12, r.y + 48, r.width - 24, 1), OperationsEdge);
            string time = float.IsPositiveInfinity(node.TimeLimit) ? "无时限" : $"{node.TimeLimit:0} 秒";
            string facts = node.Kind == "补给" ? "补给五选一" : $"{node.DefenderCount} 名守军 · {time}";
            OperationsLabel(new Rect(r.x + 12, r.y + 53, r.width - 24, 20), facts, 12, OperationsFocus, true);
            string description = node.Kind switch
            {
                "遭遇" => "开战前可选 4 档难度。",
                "战斗战备" => "开战前从三项战略战备中选一项。",
                "补给" => "领取本节点补给后继续行动。",
                _ => "清空守军后，存活的我方本体进入蓝门。"
            };
            OperationsLabel(new Rect(r.x + 12, r.y + 79, r.width - 24, 22), description, 11, OperationsMuted);
        }

        void DrawFormationExitConfirmation()
        {
            if (!formationExitOpen) return;
            Fill(BattleRect, new Color(.035f,.05f,.085f,.74f));
            var r = new Rect(421, 254, 438, 177);
            Fill(r, OperationsSurface);
            ApprovedUi.Border(r, OperationsEdge);
            OperationsLabel(new Rect(r.x + 20, r.y + 13, 370, 35), "退出游戏？", 23, OperationsInk, true);
            OperationsLabel(new Rect(r.x + 20, r.y + 60, 400, 31), "当前行动只保存在本次运行中。", 14, OperationsMuted);
            if (OperationsButton(new Rect(r.x + 196, r.yMax - 54, 106, 37), "继续整备")) formationExitOpen = false;
            if (OperationsButton(new Rect(r.x + 307, r.yMax - 54, 112, 37), "确认退出", true)) Application.Quit();
        }
    }
}
