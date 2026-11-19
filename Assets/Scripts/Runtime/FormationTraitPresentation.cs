using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color TraitPanel = new Color(.095f, .145f, .18f, 1);
        static readonly Color TraitCard = new Color(.145f, .205f, .24f, 1);
        static readonly Color TraitInk = new Color(.91f, .94f, .95f, 1);
        static readonly Color TraitMuted = new Color(.60f, .68f, .72f, 1);
        static readonly Color TraitLine = new Color(.27f, .36f, .40f, 1);
        static readonly Color TraitActive = new Color(.89f, .76f, .46f, 1);
        static readonly Color TraitDetailBackground = new Color(.080f, .100f, .114f, .94f);
        const float TraitRowHeight = 45;
        const float TraitBadgeSize = 34;
        internal const float TraitRevealSeconds = .18f;
        const int TraitMemberColumns = 4;
        const float TraitMemberRowHeight = 92;

        bool formationAllTraits;
        Vector2 formationTraitDetailScroll;
        double formationTraitOpenedAt;

        public static Rect FormationTraitSidebarRect => new Rect(16, 85, 151, 482);
        public static Rect FormationTraitDetailsRect => new Rect(174, 85, 380, 482);
        static Rect FormationTraitListRect => new Rect(16, 109, 151, 458);
        public string SelectedFormationTraitId => formationInspectedTag;
        public bool FormationTraitDetailsOpen => Campaign != null && SelectedFormationTrait != null;
        public FormationTraitView SelectedFormationTrait => FormationTraitView.For(Campaign, Shop, formationInspectedTag);
        public FormationTraitView[] FormationTraitRows => FormationTraitView.List(Campaign, Shop, formationAllTraits);

        public bool InspectFormationTrait(string id)
        {
            if (Campaign == null || CombatViewActive || Model.Phase != FormationPhase.Preparation ||
                CampaignInteractionBlocked || formationMenuOpen || shopOddsVisible || FormationTraitView.For(Campaign, Shop, id) == null)
                return false;

            if (formationInspectedTag != id)
            {
                formationTraitDetailScroll = Vector2.zero;
                formationTraitOpenedAt = Time.unscaledTimeAsDouble;
            }
            SetMarketVisible(false);
            CloseFormationInspection();
            CloseFormationRouteDetails();
            formationInspectedTag = id;
            return true;
        }

        public bool CloseFormationTraitDetails()
        {
            if (formationInspectedTag == null) return false;
            formationInspectedTag = null;
            formationTraitDetailScroll = Vector2.zero;
            CancelFormationDrag();
            return true;
        }

        public bool FormationTraitBlocksPoint(Vector2 point) => Campaign != null &&
            (FormationTraitSidebarRect.Contains(point) || FormationTraitDetailsOpen && FormationTraitDetailsRect.Contains(point));

        static GUIStyle TraitText(int size, Color color, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var style = ApprovedUi.Text(null, size, align, color, bold ? 650 : 400);
            style.wordWrap = true;
            style.clipping = TextClipping.Clip;
            return style;
        }

        static void TraitLabel(Rect rect, string text, int size = 12, Color? color = null, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
            => GUI.Label(rect, text, TraitText(size, color ?? TraitInk, bold, align));

        static bool TraitHit(Rect rect, string tooltip = null) => GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);

        static Color TraitTint(Color color)
        {
            color.a *= GUI.color.a;
            return color;
        }

        void DrawFormationTraitSidebar()
        {
            var rows = FormationTraitRows;
            var panel = FormationTraitSidebarRect;
            var heading = new Rect(panel.x, panel.y, panel.width, 22);
            DrawFloatingTraitLabel(new Rect(heading.x + 7, heading.y, 82, heading.height), "军阵羁绊", 12, TraitInk, true);
            var toggle = new Rect(heading.xMax - 43, heading.y, 40, heading.height);
            TraitLabel(toggle, formationAllTraits ? "本局" : "全部", 11, formationAllTraits ? TraitActive : TraitMuted, true, TextAnchor.MiddleCenter);
            if (TraitHit(toggle, "切换全部羁绊与本局羁绊"))
            {
                formationAllTraits = !formationAllTraits;
                campaignTraitScroll = Vector2.zero;
                CancelFormationDrag();
            }

            var list = FormationTraitListRect;
            bool pointerInList = list.Contains(Event.current.mousePosition);
            float height = rows.Length * TraitRowHeight;
            float width = list.width - (height > list.height ? 16 : 0);
            campaignTraitScroll = GUI.BeginScrollView(list, campaignTraitScroll, new Rect(0, 0, width, Mathf.Max(list.height, height)));
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var bounds = new Rect(0, i * TraitRowHeight, width, TraitRowHeight - 3);
                bool selected = formationInspectedTag == row.Id;
                bool hovered = GUI.enabled && pointerInList && bounds.Contains(Event.current.mousePosition);
                int control = GUIUtility.GetControlID(FocusType.Passive, bounds);
                bool clicked = HandleTraitRowClick(control, hovered);
                bool pressed = hovered && GUIUtility.hotControl == control;

                DrawFloatingTraitEmblem(new Vector2(23, bounds.y + 20), row, selected, hovered, pressed);
                DrawFloatingTraitLabel(new Rect(49, bounds.y + 3, bounds.width - 53, 19), row.Name, 13, TraitInk, true);
                DrawFloatingTraitThresholds(new Rect(49, bounds.y + 23, bounds.width - 53, 15), row);
                if (clicked)
                {
                    if (selected) CloseFormationTraitDetails();
                    else InspectFormationTrait(row.Id);
                }
            }
            if (rows.Length == 0)
                TraitLabel(new Rect(8, 2, width - 16, 40), "上阵角色后显示羁绊", 11, TraitMuted);
            GUI.EndScrollView();
        }

        static bool HandleTraitRowClick(int control, bool hovered)
        {
            var input = Event.current;
            switch (input.GetTypeForControl(control))
            {
                case EventType.MouseDown when hovered && input.button == 0 && GUIUtility.hotControl == 0:
                    GUIUtility.hotControl = control;
                    input.Use();
                    break;
                case EventType.MouseUp when input.button == 0 && GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    input.Use();
                    return hovered;
            }
            return false;
        }

        static void DrawFloatingTraitEmblem(Vector2 center, FormationTraitView row, bool selected, bool hovered, bool pressed)
        {
            float stroke = pressed ? 2.2f : selected ? 1.7f : hovered ? 1.15f : 0;
            if (stroke > 0)
            {
                // Draw the interaction rim behind the authored plate and its count dot.
                float half = TraitBadgeSize / (2 * Mathf.Sqrt(2)) + stroke - .25f;
                var rim = new Rect(center.x - half, center.y - half, 2 * half, 2 * half);
                var tint = Color.Lerp(SynergyBadgePresentation.ColorFor(row.Grade), Color.white, .45f);
                tint.a = pressed ? .98f : selected ? .87f : .68f;
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(45, center);
                try { ApprovedUi.Border(rim, tint, stroke); }
                finally { GUI.matrix = matrix; }
            }

            var badge = new Rect(center.x - TraitBadgeSize * .5f, center.y - TraitBadgeSize * .5f, TraitBadgeSize, TraitBadgeSize);
            SynergyBadgePresentation.DrawBadge(badge, row.Name, row.Grade, row.Count, null);
        }

        static void DrawFloatingTraitThresholds(Rect rect, FormationTraitView row)
        {
            var levels = row.Rule.Levels;
            float step = Mathf.Min(18, rect.width / levels.Length);
            for (int i = 0; i < levels.Length; i++)
            {
                int level = levels[i];
                bool active = level == row.ActiveLevel;
                Color ink = active ? SynergyBadgePresentation.ColorFor(row.Grade) : level < row.ActiveLevel ? EconomyIce : EconomyMuted;
                DrawFloatingTraitLabel(new Rect(rect.x + i * step, rect.y, step - 2, rect.height),
                    level.ToString(), 10, ink, active);
                if (i < levels.Length - 1)
                    DrawFloatingTraitLabel(new Rect(rect.x + (i + 1) * step - 5, rect.y, 5, rect.height),
                        "/", 9, EconomyMuted, false);
            }
        }

        static void DrawFloatingTraitLabel(Rect rect, string text, int size, Color ink, bool bold)
        {
            var shadow = rect;
            shadow.y += 1;
            TraitLabel(shadow, text, size, new Color(.02f, .035f, .065f, .9f), bold);
            TraitLabel(rect, text, size, ink, bold);
        }

        void DrawCampaignTraitOverlay()
        {
            if (Model.Phase != FormationPhase.Preparation) { CloseFormationTraitDetails(); return; }
            if (Campaign == null || CampaignInteractionBlocked || formationMenuOpen || shopOddsVisible) return;
            var view = SelectedFormationTrait;
            if (view == null) return;

            var panel = FormationTraitDetailsRect;
            float progress = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - formationTraitOpenedAt) / TraitRevealSeconds));
            float reveal = 1 - (1 - progress) * (1 - progress) * (1 - progress);
            var previous = GUI.color;
            GUI.BeginGroup(panel);
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Mathf.Lerp(.20f, 1, progress));
            try
            {
                DrawFormationTraitDetails(view, new Rect(Mathf.Lerp(-13, 0, reveal), 0, panel.width, panel.height));
            }
            finally
            {
                GUI.color = previous;
                GUI.EndGroup();
            }
        }

        void DrawFormationTraitDetails(FormationTraitView view, Rect panel)
        {
            Color accent = view.Active ? SynergyBadgePresentation.ColorFor(view.Grade) : TraitMuted;
            Fill(panel, TraitTint(TraitDetailBackground));
            ApprovedUi.Border(panel, TraitTint(TraitLine));
            Fill(new Rect(panel.x + 1, panel.y + 1, 3, 61), TraitTint(accent));
            SynergyBadgePresentation.DrawBadge(new Rect(panel.x + 15, panel.y + 13, TraitBadgeSize, TraitBadgeSize), view.Name, view.Grade, view.Count, null);
            TraitLabel(new Rect(panel.x + 62, panel.y + 8, 180, 25), view.Name, 20, null, true);
            TraitLabel(new Rect(panel.x + 62, panel.y + 34, 272, 20),
                (view.IsFaction ? "阵营" : "流派") + " · 已上阵 " + view.Count + " 人" +
                (view.NextLevel > 0 ? " · 距下档 " + view.MissingForNext + " 人" : " · 最高档"), 12, TraitMuted);
            TraitLabel(new Rect(panel.xMax - 133, panel.y + 10, 88, 22),
                view.Active ? view.ActiveLevel + " 人档已激活" : "未激活", 11, accent, true, TextAnchor.MiddleRight);
            var close = new Rect(panel.xMax - 35, panel.y + 8, 26, 28);
            TraitLabel(close, "×", 23, TraitMuted, false, TextAnchor.MiddleCenter);
            if (TraitHit(close, "关闭羁绊详情")) { CloseFormationTraitDetails(); return; }

            Fill(new Rect(panel.x + 15, panel.y + 61, panel.width - 30, 1), TraitTint(TraitLine));
            var body = new Rect(panel.x + 15, panel.y + 71, panel.width - 24, panel.height - 83);
            float width = body.width - 17;
            string effect = view.Rule.Effect + (string.IsNullOrWhiteSpace(view.Rule.Extra) ? "" : "\n\n" + view.Rule.Extra);
            var effectStyle = TraitText(13, TraitInk, false, TextAnchor.UpperLeft);
            float effectHeight = effectStyle.CalcHeight(new GUIContent(effect), width);
            float membersHeight = Mathf.Ceil(view.Members.Count / (float)TraitMemberColumns) * TraitMemberRowHeight;
            bool hasBadgeMembers = view.BadgeMembers.Count > 0;
            float badgeHeight = hasBadgeMembers
                ? 30 + Mathf.Ceil(view.BadgeMembers.Count / (float)TraitMemberColumns) * TraitMemberRowHeight
                : 0;
            float contentHeight = effectHeight + 67 + membersHeight + badgeHeight;
            formationTraitDetailScroll = GUI.BeginScrollView(body, formationTraitDetailScroll,
                new Rect(0, 0, width, Mathf.Max(body.height, contentHeight)));

            GUI.Label(new Rect(0, 0, width, effectHeight), effect, effectStyle);
            float y = effectHeight + 10;
            float tierStep = width / view.Rule.Levels.Length;
            for (int i = 0; i < view.Rule.Levels.Length; i++)
            {
                int level = view.Rule.Levels[i];
                var tier = new Rect(i * tierStep, y, tierStep - 5, 23);
                bool current = level == view.ActiveLevel;
                Fill(tier, TraitTint(current ? accent : new Color(.17f, .22f, .25f, .80f)));
                string status = current ? " · 当前" : level < view.ActiveLevel ? " · 达成" : "";
                TraitLabel(tier, level + " 人" + status, 11, current ? TraitPanel : TraitMuted, current, TextAnchor.MiddleCenter);
            }

            y += 33;
            Fill(new Rect(0, y, width, 1), TraitTint(TraitLine));
            TraitLabel(new Rect(0, y + 2, width, 22), "羁绊成员", 12, TraitMuted, true);
            y += 24;
            DrawFormationTraitMembers(view.Members, y, width);
            if (hasBadgeMembers)
            {
                y += membersHeight;
                TraitLabel(new Rect(0, y + 2, width, 24), "徽章成员 · 上阵后计入人数", 12, TraitActive, true);
                DrawFormationTraitMembers(view.BadgeMembers, y + 30, width);
            }
            GUI.EndScrollView();
        }

        static void DrawFormationTraitMembers(IReadOnlyList<FormationTraitMemberView> members, float y, float width)
        {
            float step = width / TraitMemberColumns;
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var tile = new Rect(i % TraitMemberColumns * step, y + i / TraitMemberColumns * TraitMemberRowHeight,
                    step - 5, TraitMemberRowHeight - 5);
                bool lineup = member.State == FormationTraitMemberState.Lineup;
                bool owned = member.State != FormationTraitMemberState.Missing;
                Fill(tile, TraitTint(new Color(.12f, .16f, .18f, .84f)));
                Fill(new Rect(tile.x, tile.y + 45, tile.width, 2), TraitTint(ApprovedUi.Tier(member.Cost)));
                if (lineup) ApprovedUi.Border(tile, TraitTint(TraitActive));

                Color previous = GUI.color;
                GUI.color = owned ? previous : TraitTint(new Color(.57f, .61f, .65f, .85f));
                ApprovedShopPresentation.DrawTestPortrait(new Rect(tile.x + 13, tile.y + 3, tile.width - 26, 41),
                    FormationUnitArt.For(member.CardId)?.Portrait);
                GUI.color = previous;
                TraitLabel(new Rect(tile.x + 5, tile.y + 2, 16, 16), member.Cost.ToString(), 10, TraitMuted, true);
                TraitLabel(new Rect(tile.x + 3, tile.y + 47, tile.width - 6, 26), member.Name,
                    11, owned ? TraitInk : TraitMuted, false, TextAnchor.MiddleCenter);
                TraitLabel(new Rect(tile.x + 2, tile.y + 73, tile.width - 4, 14), member.StateText,
                    10, lineup ? TraitActive : TraitMuted, lineup, TextAnchor.MiddleCenter);
            }
        }
    }
}
