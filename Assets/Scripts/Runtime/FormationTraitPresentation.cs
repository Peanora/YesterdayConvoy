using System;
using System.Linq;
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
        bool formationAllTraits;
        Vector2 formationTraitDetailScroll;
        public static Rect FormationTraitSidebarRect => new Rect(16, 66, 165, 490);
        public static Rect FormationTraitDetailsRect => new Rect(194, 246, 650, 310);
        static Rect FormationTraitListRect => new Rect(22, 126, 153, 421);
        public string SelectedFormationTraitId => formationInspectedTag;
        public bool FormationTraitDetailsOpen => Campaign != null && SelectedFormationTrait != null;
        public FormationTraitView SelectedFormationTrait => FormationTraitView.For(Campaign, Shop, formationInspectedTag);
        public FormationTraitView[] FormationTraitRows => FormationTraitView.List(Campaign, Shop, formationAllTraits);

        public bool InspectFormationTrait(string id)
        {
            if (Campaign == null || Model.Phase == FormationPhase.Won || Model.Phase == FormationPhase.Lost || CampaignInteractionBlocked || formationMenuOpen || shopOddsVisible || FormationTraitView.For(Campaign, Shop, id) == null) return false;
            if (formationInspectedTag != id) formationTraitDetailScroll = Vector2.zero;
            SetMarketVisible(false); formationInspectedTag = id; CancelFormationDrag(); return true;
        }
        public bool CloseFormationTraitDetails()
        {
            if (formationInspectedTag == null) return false;
            formationInspectedTag = null; formationTraitDetailScroll = Vector2.zero; CancelFormationDrag(); return true;
        }
        public bool FormationTraitBlocksPoint(Vector2 point) => Campaign != null &&
            (FormationTraitSidebarRect.Contains(point) || FormationTraitDetailsOpen && FormationTraitDetailsRect.Contains(point));

        GUIStyle TraitText(int size = 12, Color? color = null, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var style = ApprovedUi.Text(null, size, align, color ?? TraitInk, bold ? 650 : 400);
            style.wordWrap = true; style.clipping = TextClipping.Clip; return style;
        }
        void TraitLabel(Rect rect, string text, int size = 12, Color? color = null, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
            => GUI.Label(rect, text, TraitText(size, color, bold, align));
        bool TraitHit(Rect rect, string tooltip = null) => GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);

        void DrawFormationTraitSidebar()
        {
            var rows = FormationTraitRows;
            var panel = FormationTraitSidebarRect;
            Fill(panel, TraitPanel); ApprovedUi.Border(panel, TraitLine);
            TraitLabel(new Rect(28, 77, 100, 22), "军阵羁绊", 16, null, true);
            var toggle = new Rect(137, 80, 31, 19);
            Fill(toggle, formationAllTraits ? TraitActive : TraitCard);
            TraitLabel(toggle, formationAllTraits ? "收起" : "全部", 10, formationAllTraits ? TraitPanel : TraitMuted, false, TextAnchor.MiddleCenter);
            if (TraitHit(toggle, "查看全部阵营与流派")) { formationAllTraits = !formationAllTraits; campaignTraitScroll = Vector2.zero; CancelFormationDrag(); }
            TraitLabel(new Rect(28, 103, 140, 16), rows.Count(t => t.Active) + "项激活 · 点击查看成员", 10, TraitMuted);
            Fill(new Rect(27, 121, 143, 1), TraitLine);
            campaignTraitScroll = GUI.BeginScrollView(FormationTraitListRect, campaignTraitScroll, new Rect(0, 0, 133, Mathf.Max(421, rows.Length * 57)));
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i]; var r = new Rect(0, i * 57, 132, 52);
                bool selected = formationInspectedTag == row.Id;
                Fill(r, selected ? new Color(.23f, .31f, .34f, 1) : row.Active ? TraitCard : TraitPanel);
                if (selected) ApprovedUi.Border(r, TraitActive);
                var count = new Rect(r.x + 4, r.y + 7, 29, 33);
                Fill(count, row.Active ? TraitActive : new Color(.22f, .28f, .31f, 1));
                TraitLabel(count, row.Count.ToString(), 20, row.Active ? TraitPanel : TraitMuted, true, TextAnchor.MiddleCenter);
                TraitLabel(new Rect(r.x + 40, r.y + 4, 89, 22), row.Name, row.Name.Length > 5 ? 12 : 13, row.Active ? TraitInk : TraitMuted, true);
                float x = r.x + 40;
                foreach (int level in row.Rule.Levels)
                {
                    bool reached = row.Count >= level;
                    var chip = new Rect(x, r.y + 30, 18, 15);
                    Fill(chip, reached ? row.ActiveLevel == level ? TraitActive : new Color(.37f, .42f, .39f, 1) : new Color(.15f, .20f, .23f, 1));
                    TraitLabel(chip, level.ToString(), 10, reached ? row.ActiveLevel == level ? TraitPanel : TraitInk : TraitMuted, reached, TextAnchor.MiddleCenter);
                    x += 21;
                }
                if (TraitHit(r, row.Active ? row.ActiveLevel + "人档已激活" : "还差" + row.MissingForNext + "名上阵成员"))
                { if (selected) CloseFormationTraitDetails(); else InspectFormationTrait(row.Id); }
            }
            if (rows.Length == 0) TraitLabel(new Rect(7, 15, 120, 65), "上阵相同羁绊的不同角色\n即可点亮军阵加成", 11, TraitMuted);
            GUI.EndScrollView();
        }

        void DrawCampaignTraitOverlay()
        {
            if (Model.Phase == FormationPhase.Won || Model.Phase == FormationPhase.Lost) { CloseFormationTraitDetails(); return; }
            if (Campaign == null || CampaignInteractionBlocked || formationMenuOpen || shopOddsVisible) return;
            var view = SelectedFormationTrait; if (view == null) return;
            var r = FormationTraitDetailsRect;
            Fill(new Rect(r.x + 5, r.y + 5, r.width, r.height), new Color(0, 0, 0, .4f));
            Fill(r, TraitPanel); ApprovedUi.Border(r, TraitLine);
            Fill(new Rect(r.x, r.y, 4, r.height), view.Active ? TraitActive : TraitLine);
            TraitLabel(new Rect(r.x + 18, r.y + 12, 260, 27), view.Name, 21, null, true);
            TraitLabel(new Rect(r.x + 18, r.y + 42, 285, 19), (view.IsFaction ? "阵营羁绊" : "流派羁绊") + " · 仅上阵的不同角色计数", 11, TraitMuted);
            var status = new Rect(r.xMax - 226, r.y + 14, 176, 25);
            Fill(status, view.Active ? TraitActive : TraitCard);
            TraitLabel(status, view.Active ? view.ActiveLevel + "人档已激活" : "尚未激活", 12, view.Active ? TraitPanel : TraitMuted, true, TextAnchor.MiddleCenter);
            var close = new Rect(r.xMax - 37, r.y + 12, 24, 24);
            TraitLabel(close, "×", 23, TraitMuted, false, TextAnchor.MiddleCenter);
            if (TraitHit(close, "关闭羁绊详情")) { CloseFormationTraitDetails(); return; }
            TraitLabel(new Rect(r.xMax - 244, r.y + 44, 225, 18), view.NextLevel > 0 ? "下档 " + view.NextLevel + " 人 · 还差 " + view.MissingForNext + " 人" : "已达到最高档位", 11, TraitMuted, false, TextAnchor.MiddleRight);
            float tierX = r.x + 18;
            foreach (int level in view.Rule.Levels)
            {
                var tier = new Rect(tierX, r.y + 71, 83, 24); bool reached = view.Count >= level;
                Fill(tier, reached ? view.ActiveLevel == level ? TraitActive : new Color(.32f, .39f, .38f, 1) : TraitCard);
                TraitLabel(tier, level + "人" + (view.ActiveLevel == level ? " · 当前" : reached ? " · 达成" : " · 未达"), 10,
                    view.ActiveLevel == level ? TraitPanel : reached ? TraitInk : TraitMuted, reached, TextAnchor.MiddleCenter);
                tierX += 89;
            }
            TraitLabel(new Rect(r.xMax - 154, r.y + 72, 134, 22), "已上阵 " + view.Count + " 人", 13, view.Active ? TraitActive : TraitMuted, true, TextAnchor.MiddleRight);
            Fill(new Rect(r.x + 17, r.y + 105, r.width - 34, 1), TraitLine);
            var body = new Rect(r.x + 17, r.y + 114, r.width - 27, r.height - 124);
            const float contentWidth = 602;
            float membersHeight = Mathf.Ceil(view.Members.Count / 6f) * 94;
            float badgesHeight = view.BadgeMembers.Count > 0 ? 27 + Mathf.Ceil(view.BadgeMembers.Count / 6f) * 94 : 0;
            string effect = view.Rule.Effect + (string.IsNullOrWhiteSpace(view.Rule.Extra) ? "" : "\n\n" + view.Rule.Extra);
            var effectStyle = TraitText(12, TraitInk, false, TextAnchor.UpperLeft);
            float effectHeight = Mathf.Max(38, effectStyle.CalcHeight(new GUIContent(effect), contentWidth - 6));
            float contentHeight = 31 + membersHeight + badgesHeight + 35 + effectHeight + 12;
            formationTraitDetailScroll = GUI.BeginScrollView(body, formationTraitDetailScroll, new Rect(0, 0, contentWidth, Mathf.Max(body.height, contentHeight)));
            TraitLabel(new Rect(1, 0, contentWidth, 22), "天然成员  " + view.Members.Count + "人", 12, TraitMuted, true);
            float y = 28; DrawFormationTraitMembers(view.Members, y); y += membersHeight;
            if (view.BadgeMembers.Count > 0)
            {
                TraitLabel(new Rect(1, y, contentWidth, 22), "徽章成员 · 上阵后计入人数", 12, TraitActive, true);
                y += 27; DrawFormationTraitMembers(view.BadgeMembers, y); y += badgesHeight - 27;
            }
            TraitLabel(new Rect(1, y + 3, contentWidth, 24), "羁绊效果", 13, TraitActive, true);
            GUI.Label(new Rect(1, y + 34, contentWidth - 6, effectHeight), effect, effectStyle);
            GUI.EndScrollView();
        }

        void DrawFormationTraitMembers(System.Collections.Generic.IReadOnlyList<FormationTraitMemberView> members, float y)
        {
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i]; var r = new Rect(i % 6 * 100, y + i / 6 * 94, 92, 88);
                bool onField = member.State == FormationTraitMemberState.Lineup, owned = member.State != FormationTraitMemberState.Missing;
                Fill(r, TraitCard); Fill(new Rect(r.x, r.y, r.width, 3), ApprovedUi.Tier(member.Cost));
                var portrait = new Rect(r.x + 20, r.y + 5, 52, 42);
                Color old = GUI.color; GUI.color = owned ? Color.white : new Color(.47f, .51f, .54f, 1);
                ApprovedShopPresentation.DrawTestPortrait(portrait, FormationUnitArt.For(member.CardId)?.Portrait); GUI.color = old;
                TraitLabel(new Rect(r.x + 5, r.y + 5, 17, 18), member.Cost.ToString(), 11, owned ? TraitInk : TraitMuted, true);
                TraitLabel(new Rect(r.x + 4, r.y + 47, r.width - 8, 23), member.Name, member.Name.Length > 7 ? 9 : 10, owned ? TraitInk : TraitMuted, false, TextAnchor.MiddleCenter);
                var state = new Rect(r.x + 5, r.y + 70, r.width - 10, 15);
                if (onField) Fill(state, new Color(.29f, .39f, .38f, 1));
                TraitLabel(state, member.StateText, 9, onField ? TraitActive : TraitMuted, onField, TextAnchor.MiddleCenter);
                if (onField) ApprovedUi.Border(r, new Color(.60f, .60f, .40f, 1));
            }
        }
    }
}
