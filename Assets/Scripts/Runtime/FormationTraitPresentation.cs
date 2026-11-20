using System.Collections.Generic;
using UnityEngine;
using PreparationPalette = YesterdayConvoy.FormationPreparationPalette;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color TraitPanel = new Color(.095f, .145f, .18f, 1);
        static readonly Color TraitCard = new Color(.145f, .205f, .24f, 1);
        static readonly Color TraitInk = new Color(.91f, .94f, .95f, 1);
        static readonly Color TraitMuted = new Color(.60f, .68f, .72f, 1);
        static readonly Color TraitSelectionEdge = new Color(1f, .95f, .86f);
        const float TraitRowHeight = 45;
        const float TraitBadgeSize = 34;
        const float TraitBadgePressSeconds = .065f;
        const float TraitBadgePulseSeconds = .23f;
        const float TraitBadgePressedScale = .89f;
        const float TraitBadgePressedOffset = 2f;
        internal const float TraitRevealSeconds = .18f;
        const int TraitMemberColumns = 4;
        const float TraitMemberRowHeight = 92;

        static readonly AnimationCurve TraitBadgeReboundScale = new AnimationCurve(
            new Keyframe(0f, TraitBadgePressedScale, 0f, 4.5f), new Keyframe(.086f, 1.08f),
            new Keyframe(.156f, .99f), new Keyframe(.22f, 1f));
        static readonly AnimationCurve TraitBadgeReboundOffset = new AnimationCurve(
            new Keyframe(0f, TraitBadgePressedOffset, 0f, -65f), new Keyframe(.086f, -1f), new Keyframe(.22f, 0f));

        bool formationAllTraits;
        Vector2 formationTraitDetailScroll;
        double formationTraitOpenedAt, formationTraitPressedAt, formationTraitPulsedAt;
        string formationTraitPulsedId;

        public static Rect FormationTraitSidebarRect => new Rect(16, 85, 151, 482);
        public static Rect FormationTraitDetailsRect => new Rect(174, 85, 380, 527);
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
            CloseFormationEquipmentDetails();
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
            var list = FormationTraitListRect;
            float height = rows.Length * TraitRowHeight;
            var heading = new Rect(panel.x, panel.y, panel.width, 22);
            DrawFloatingTraitLabel(new Rect(heading.x + 7, heading.y, 82, heading.height), "军阵羁绊", 12, TraitInk, true);
            var toggle = new Rect(heading.xMax - 43, heading.y, 40, heading.height);
            TraitLabel(toggle, formationAllTraits ? "本局" : "全部", 11, PreparationPalette.Muted, true, TextAnchor.MiddleCenter);
            if (TraitHit(toggle, "切换全部羁绊与本局羁绊"))
            {
                formationAllTraits = !formationAllTraits;
                campaignTraitScroll = Vector2.zero;
                CancelFormationDrag();
            }

            bool pointerInList = list.Contains(Event.current.mousePosition);
            float width = list.width - (height > list.height ? 16 : 0);
            campaignTraitScroll = GUI.BeginScrollView(list, campaignTraitScroll, new Rect(0, 0, width, Mathf.Max(list.height, height)));
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var bounds = new Rect(0, i * TraitRowHeight, width, TraitRowHeight - 3);
                bool selected = formationInspectedTag == row.Id;
                bool hovered = GUI.enabled && pointerInList && bounds.Contains(Event.current.mousePosition);
                int control = GUIUtility.GetControlID(row.Id.GetHashCode(), FocusType.Passive, bounds);
                bool clicked = HandleTraitRowClick(control, hovered);
                if (clicked && (selected ? CloseFormationTraitDetails() : InspectFormationTrait(row.Id)))
                {
                    formationTraitPulsedId = row.Id;
                    formationTraitPulsedAt = Time.unscaledTimeAsDouble;
                    selected = !selected;
                }

                bool pressed = hovered && GUIUtility.hotControl == control;
                DrawFloatingTraitEmblem(new Vector2(23, bounds.y + 20), row, selected, hovered, pressed);
                DrawFloatingTraitLabel(new Rect(49, bounds.y + 3, bounds.width - 53, 19), row.Name, 13, TraitInk, true);
                DrawFloatingTraitThresholds(new Rect(49, bounds.y + 23, bounds.width - 53, 15), row);
            }
            if (rows.Length == 0)
                TraitLabel(new Rect(8, 2, width - 16, 40), "上阵角色后显示羁绊", 11, PreparationPalette.Muted);
            GUI.EndScrollView();
        }

        bool HandleTraitRowClick(int control, bool hovered)
        {
            var input = Event.current;
            switch (input.GetTypeForControl(control))
            {
                case EventType.MouseDown when hovered && input.button == 0 && GUIUtility.hotControl == 0:
                    GUIUtility.hotControl = control;
                    formationTraitPressedAt = Time.unscaledTimeAsDouble;
                    formationTraitPulsedId = null;
                    input.Use();
                    break;
                case EventType.MouseUp when input.button == 0 && GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    input.Use();
                    return hovered;
            }
            return false;
        }

        void DrawFloatingTraitEmblem(Vector2 center, FormationTraitView row, bool selected, bool hovered, bool pressed)
        {
            float scale = 1f, offset = 0f, counterOffset = 0f;
            float pulseElapsed = formationTraitPulsedId == row.Id
                ? (float)(Time.unscaledTimeAsDouble - formationTraitPulsedAt) : TraitBadgePulseSeconds;
            if (pressed)
            {
                float remaining = 1f - Mathf.Clamp01((float)(Time.unscaledTimeAsDouble - formationTraitPressedAt) / TraitBadgePressSeconds);
                float amount = 1f - remaining * remaining * remaining;
                scale = Mathf.Lerp(1f, TraitBadgePressedScale, amount);
                offset = TraitBadgePressedOffset * amount;
                counterOffset = amount;
            }
            else if (pulseElapsed < TraitBadgePulseSeconds)
            {
                scale = TraitBadgeReboundScale.Evaluate(pulseElapsed);
                offset = TraitBadgeReboundOffset.Evaluate(pulseElapsed);
            }

            Vector2 faceCenter = center + Vector2.up * offset;
            if (selected || hovered)
            {
                var edge = TraitSelectionEdge;
                edge.a = pressed ? 1f : selected ? .94f : .65f;
                float stroke = pressed ? 1.6f : selected ? 1.25f : 1f;
                DrawTraitDiamond(faceCenter, TraitBadgeSize * scale + 3f, edge, stroke);
            }
            if (pulseElapsed < TraitBadgePulseSeconds)
            {
                float progress = pulseElapsed / TraitBadgePulseSeconds;
                var flash = TraitSelectionEdge;
                flash.a = .95f * Mathf.Pow(1f - progress, 1.6f);
                float diameter = Mathf.Lerp(33f, 40f, 1f - (1f - progress) * (1f - progress));
                DrawTraitDiamond(faceCenter, diameter, flash, 1.4f);
            }
            if (selected)
                DrawTraitDiamond(center - Vector2.right * 20.5f, 3.4f, TraitSelectionEdge, 0f);

            var badge = new Rect(center.x - TraitBadgeSize * .5f, center.y - TraitBadgeSize * .5f, TraitBadgeSize, TraitBadgeSize);
            SynergyBadgePresentation.DrawBadge(badge, row.Name, row.Grade, row.Count, null, scale, offset, counterOffset);
        }

        static void DrawTraitDiamond(Vector2 center, float diameter, Color tint, float stroke)
        {
            float half = diameter / (2f * Mathf.Sqrt(2f));
            var shape = new Rect(center.x - half, center.y - half, 2f * half, 2f * half);
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            try
            {
                if (stroke > 0f) ApprovedUi.Border(shape, tint, stroke);
                else Fill(shape, tint);
            }
            finally { GUI.matrix = matrix; }
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

            var panel = BeginFormationDetailReveal(FormationTraitDetailsRect, formationTraitOpenedAt, -13, out var previous);
            try { DrawFormationTraitDetails(view, panel); }
            finally { EndFormationDetailReveal(previous); }
        }

        static Rect BeginFormationDetailReveal(Rect bounds, double openedAt, float offset, out Color previous)
        {
            float progress = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - openedAt) / TraitRevealSeconds));
            float reveal = 1 - (1 - progress) * (1 - progress) * (1 - progress);
            previous = GUI.color;
            GUI.BeginGroup(bounds);
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Mathf.Lerp(.20f, 1, progress));
            return new Rect(Mathf.Lerp(offset, 0, reveal), 0, bounds.width, bounds.height);
        }

        static void EndFormationDetailReveal(Color previous)
        {
            GUI.color = previous;
            GUI.EndGroup();
        }

        static void DrawFormationDetailFrame(Rect panel)
        {
            Fill(panel, TraitTint(PreparationPalette.Plate));
            Fill(new Rect(panel.x, panel.y, panel.width, 60), TraitTint(PreparationPalette.Raised));
            Fill(new Rect(panel.x, panel.y + 59, panel.width, 2), TraitTint(PreparationPalette.Edge));
            ApprovedUi.Border(panel, TraitTint(PreparationPalette.PlateRule));
        }

        static bool FormationDetailClosePressed(Rect panel, string tooltip)
        {
            var close = new Rect(panel.xMax - 35, panel.y + 8, 26, 28);
            if (GUI.enabled && close.Contains(Event.current.mousePosition)) Fill(close, TraitTint(PreparationPalette.Hover));
            TraitLabel(close, "×", 23, PreparationPalette.Muted, false, TextAnchor.MiddleCenter);
            return TraitHit(close, tooltip);
        }

        void DrawFormationTraitDetails(FormationTraitView view, Rect panel)
        {
            DrawFormationDetailFrame(panel);
            SynergyBadgePresentation.DrawBadge(new Rect(panel.x + 15, panel.y + 13, TraitBadgeSize, TraitBadgeSize), view.Name, view.Grade, view.Count, null);
            TraitLabel(new Rect(panel.x + 62, panel.y + 8, 180, 25), view.Name, 20, PreparationPalette.Text, true);
            TraitLabel(new Rect(panel.x + 62, panel.y + 34, 272, 20),
                (view.IsFaction ? "阵营" : "流派") + " · 已上阵 " + view.Count + " 人" +
                (view.NextLevel > 0 ? " · 距下档 " + view.MissingForNext + " 人" : " · 最高档"), 11, PreparationPalette.Muted);
            TraitLabel(new Rect(panel.xMax - 133, panel.y + 10, 88, 22),
                view.Active ? view.ActiveLevel + " 人档已激活" : "未激活", 11,
                view.Active ? PreparationPalette.Amber : PreparationPalette.Muted, true, TextAnchor.MiddleRight);
            if (FormationDetailClosePressed(panel, "关闭羁绊详情")) { CloseFormationTraitDetails(); return; }

            var body = new Rect(panel.x + 13, panel.y + 69, panel.width - 19, panel.height - 78);
            float width = body.width - 17;
            var effect = new GUIContent(view.Rule.Effect);
            var extra = new GUIContent(view.Rule.Extra);
            var effectStyle = TraitText(13, PreparationPalette.Ink, false, TextAnchor.UpperLeft);
            var extraStyle = TraitText(11, PreparationPalette.PlateMuted, false, TextAnchor.UpperLeft);
            float effectHeight = effectStyle.CalcHeight(effect, width);
            float extraHeight = string.IsNullOrWhiteSpace(extra.text) ? 0 : 5 + extraStyle.CalcHeight(extra, width);
            float membersHeight = Mathf.Ceil(view.Members.Count / (float)TraitMemberColumns) * TraitMemberRowHeight;
            bool hasBadgeMembers = view.BadgeMembers.Count > 0;
            float badgeHeight = hasBadgeMembers
                ? 30 + Mathf.Ceil(view.BadgeMembers.Count / (float)TraitMemberColumns) * TraitMemberRowHeight
                : 0;
            const float tierHeight = 58;
            const float sectionHeadingHeight = 24;
            float contentHeight = tierHeight + 2 * sectionHeadingHeight + effectHeight + extraHeight + 9 + membersHeight + badgeHeight;
            formationTraitDetailScroll = GUI.BeginScrollView(body, formationTraitDetailScroll,
                new Rect(0, 0, width, Mathf.Max(body.height, contentHeight)));

            TraitLabel(new Rect(0, 0, width, 19), "激活档位", 12, PreparationPalette.PlateMuted, true);
            DrawFormationTraitTiers(view, width);
            float y = tierHeight;
            DrawFormationTraitSection(y, width, "羁绊效果");
            y += sectionHeadingHeight;
            GUI.Label(new Rect(0, y, width, effectHeight), effect, effectStyle);
            y += effectHeight;
            if (extraHeight > 0)
            {
                GUI.Label(new Rect(0, y + 5, width, extraHeight - 5), extra, extraStyle);
                y += extraHeight;
            }
            y += 9;
            DrawFormationTraitSection(y, width, "羁绊成员");
            y += sectionHeadingHeight;
            DrawFormationTraitMembers(view.Members, y, width);
            if (hasBadgeMembers)
            {
                y += membersHeight;
                DrawFormationTraitSection(y, width, "徽章成员 · 上阵后计入人数");
                DrawFormationTraitMembers(view.BadgeMembers, y + 30, width);
            }
            GUI.EndScrollView();
        }

        static void DrawFormationTraitTiers(FormationTraitView view, float width)
        {
            float tierStep = width / view.Rule.Levels.Length;
            for (int i = 0; i < view.Rule.Levels.Length; i++)
            {
                int level = view.Rule.Levels[i];
                var tier = new Rect(i * tierStep, 22, tierStep - 4, 26);
                bool current = level == view.ActiveLevel;
                bool achieved = level <= view.ActiveLevel;
                Fill(tier, TraitTint(achieved ? PreparationPalette.PlateActive : PreparationPalette.PlateInset));
                Fill(new Rect(tier.x, tier.yMax - 2, tier.width, 2), TraitTint(achieved ? PreparationPalette.BronzeEdge :
                    level == view.NextLevel ? PreparationPalette.Edge : PreparationPalette.PlateRule));
                string status = current ? " · 当前" : achieved ? " · 达成" : "";
                TraitLabel(tier, level + " 人" + status, 11, achieved ? PreparationPalette.BronzeInk : PreparationPalette.PlateMuted,
                    current, TextAnchor.MiddleCenter);
            }
        }

        static void DrawFormationTraitSection(float y, float width, string title)
        {
            Fill(new Rect(0, y, width - 4, 1), TraitTint(PreparationPalette.PlateRule));
            TraitLabel(new Rect(0, y + 2, width, 21), title, 12, PreparationPalette.PlateMuted, true);
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
                Fill(tile, TraitTint(lineup ? PreparationPalette.PlateActive : PreparationPalette.PlateInset));
                ApprovedUi.Border(tile, TraitTint(lineup ? PreparationPalette.BronzeEdge : PreparationPalette.PlateRule));

                Color previous = GUI.color;
                GUI.color = owned ? previous : TraitTint(new Color(.87f, .87f, .83f, .9f));
                ApprovedShopPresentation.DrawTestPortrait(new Rect(tile.x + 13, tile.y + 3, tile.width - 26, 41),
                    FormationUnitArt.For(member.CardId)?.Portrait);
                GUI.color = previous;
                TraitLabel(new Rect(tile.x + 5, tile.y + 2, 16, 16), member.Cost.ToString(), 10, PreparationPalette.PlateMuted, true);
                TraitLabel(new Rect(tile.x + 3, tile.y + 47, tile.width - 6, 26), member.Name,
                    11, owned ? PreparationPalette.Ink : PreparationPalette.PlateMuted, false, TextAnchor.MiddleCenter);
                TraitLabel(new Rect(tile.x + 2, tile.y + 73, tile.width - 4, 14), member.StateText,
                    10, lineup ? PreparationPalette.BronzeInk : PreparationPalette.PlateMuted, lineup, TextAnchor.MiddleCenter);
            }
        }
    }
}
