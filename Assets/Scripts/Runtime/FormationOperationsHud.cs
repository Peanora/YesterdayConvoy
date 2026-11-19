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
        static readonly Rect OperationsExitRect = new Rect(14, 20, 58, 36);
        static readonly Rect OperationsScoutRect = new Rect(83, 20, 113, 36);
        static readonly Rect OperationsMenuRect = new Rect(1215, 20, 49, 36);
        static readonly int[][] OperationsPlanes = FormationCampaignRules.Nodes
            .Select((node, index) => new { node.Plane, Index = index })
            .GroupBy(node => node.Plane).OrderBy(group => group.Key)
            .Select(group => group.Select(node => node.Index).ToArray()).ToArray();
        int formationInspectedNode = -1;
        bool formationExitOpen, campaignScoutOwn;

        int[] OperationsPlaneNodes => OperationsPlanes[Campaign.CurrentNode.Plane - 1];

        public Rect FormationRouteNodeRect(int nodeIndex)
        {
            if (Campaign == null) return Rect.zero;
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
                    MarketVisible ? FormationMarketRect.yMax + 6 : 83, 294, 113);
            }
        }

        bool CloseFormationRouteDetails()
        {
            if (formationInspectedNode < 0) return false;
            formationInspectedNode = -1;
            return true;
        }

        bool OperationsBlocksPoint(Vector2 point) =>
            formationInspectedNode >= 0 && FormationRouteDetailsRect.Contains(point);

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
            OperationsLabel(new Rect(scout.x + 10, scout.y + 19, 82, 16),
                ViewingDefenders ? "返回我方 [V]" : "查看防线 [V]", 9, OperationsMuted);
            OperationsLabel(new Rect(scout.xMax - 26, scout.y, 23, 36),
                Model.RemainingDefenders.ToString(), 19, OperationsFocus, true, TextAnchor.MiddleRight);
            if (GUI.Button(scout, GUIContent.none, GUIStyle.none))
            {
                OpenFormationOperationsPanel();
                campaignScoutOwn = false;
                campaignScoutScroll = Vector2.zero;
                campaignScoutingOpen = true;
            }

            DrawFormationNodeRail();
            var life = new Rect(1006, 20, 86, 36);
            Fill(new Rect(life.x, life.y + 2, 1, 32), OperationsEdge);
            OperationsLabel(new Rect(life.x + 8, life.y, 19, 35), "♥", 15, new Color32(226, 163, 150, 255));
            OperationsLabel(new Rect(life.x + 28, life.y, 61, 15), "队伍生命", 10, OperationsMuted);
            OperationsLabel(new Rect(life.x + 28, life.y + 13, 58, 24), Campaign.SquadHP.ToString(), 23, OperationsInk, true);
            bool preparing = Model.Phase == FormationPhase.Preparation;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && preparing;
            if (OperationsButton(new Rect(1096, 20, 58, 36), "搭档"))
            { OpenFormationOperationsPanel(); campaignFormationOpen = true; }
            GUI.enabled = enabled;
            if (OperationsButton(new Rect(1155, 20, 59, 36), "记录"))
            { OpenFormationOperationsPanel(); campaignRouteOpen = true; }
            Fill(new Rect(1154, 27, 1, 22), OperationsEdge);
            Fill(new Rect(1214, 27, 1, 22), OperationsEdge);
        }

        void OpenFormationOperationsPanel()
        {
            CloseCampaignPanels();
            CloseFormationTraitDetails();
            SetMarketVisible(false);
        }

        void DrawFormationNodeRail()
        {
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

        static void OperationsStroke(Vector2 from, Vector2 to, Color color, float width = 1)
        {
            var difference = to - from;
            var previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg, from);
            Fill(new Rect(from.x, from.y - width / 2, difference.magnitude, width), color);
            GUI.matrix = previous;
        }

        static void DrawNodeMark(Vector2 p, string kind, Color color)
        {
            if (kind == "奖励")
            {
                ApprovedUi.Border(new Rect(p.x - 6, p.y - 2, 12, 8), color);
                Fill(new Rect(p.x - 8, p.y - 4, 16, 2), color);
                Fill(new Rect(p.x - .75f, p.y - 5, 1.5f, 11), color);
                OperationsStroke(p + new Vector2(-4, -7), p + new Vector2(0, -4), color);
                OperationsStroke(p + new Vector2(4, -7), p + new Vector2(0, -4), color);
            }
            else if (kind == "补给")
            {
                Fill(new Rect(p.x - 6, p.y - 1, 12, 2), color);
                Fill(new Rect(p.x - 1, p.y - 6, 2, 12), color);
            }
            else if (kind == "遭遇")
            {
                OperationsStroke(p + new Vector2(0,-7), p + new Vector2(7,6), color, 1.5f);
                OperationsStroke(p + new Vector2(7,6), p + new Vector2(-7,6), color, 1.5f);
                OperationsStroke(p + new Vector2(-7,6), p + new Vector2(0,-7), color, 1.5f);
                Fill(new Rect(p.x - .7f, p.y - 2, 1.4f, 4), color);
                Fill(new Rect(p.x - .7f, p.y + 3, 1.4f, 1.4f), color);
            }
            else if (kind == "首领")
            {
                OperationsStroke(p + new Vector2(-8,-5), p + new Vector2(-6,5), color, 1.5f);
                OperationsStroke(p + new Vector2(8,-5), p + new Vector2(6,5), color, 1.5f);
                Fill(new Rect(p.x - 6, p.y + 4, 12, 2), color);
                OperationsStroke(p + new Vector2(-8,-5), p + new Vector2(-4,-1), color, 1.5f);
                OperationsStroke(p + new Vector2(-4,-1), p + new Vector2(0,-6), color, 1.5f);
                OperationsStroke(p + new Vector2(0,-6), p + new Vector2(4,-1), color, 1.5f);
                OperationsStroke(p + new Vector2(4,-1), p + new Vector2(8,-5), color, 1.5f);
            }
            else if (kind == "战斗战备")
            {
                ApprovedShopPresentation.DrawLoopShield(p - new Vector2(7, 8), color);
            }
            else
            {
                OperationsStroke(p + new Vector2(-6,-6), p + new Vector2(6,6), color, 2);
                OperationsStroke(p + new Vector2(6,-6), p + new Vector2(-6,6), color, 2);
                OperationsStroke(p + new Vector2(-6,2), p + new Vector2(-2,6), color, 1.5f);
                OperationsStroke(p + new Vector2(6,2), p + new Vector2(2,6), color, 1.5f);
            }
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
