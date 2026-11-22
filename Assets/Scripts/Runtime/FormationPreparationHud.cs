using UnityEngine;

namespace YesterdayConvoy
{
    internal static class FormationPreparationPalette
    {
        internal static readonly Color Surface = new Color32(40, 43, 39, 255);
        internal static readonly Color Raised = new Color32(48, 51, 45, 255);
        internal static readonly Color Recess = new Color32(31, 34, 30, 255);
        internal static readonly Color Hover = new Color32(59, 62, 54, 255);
        internal static readonly Color Selected = new Color32(65, 68, 57, 255);
        internal static readonly Color Line = new Color32(83, 87, 76, 255);
        internal static readonly Color Edge = new Color32(119, 118, 106, 255);
        internal static readonly Color Text = new Color32(238, 234, 224, 255);
        internal static readonly Color Muted = new Color32(194, 196, 182, 255);
        internal static readonly Color Amber = new Color32(227, 182, 101, 255);
        internal static readonly Color Rust = new Color32(137, 62, 48, 255);
        internal static readonly Color RustEdge = new Color32(223, 170, 131, 255);
        internal static readonly Color Plate = new Color32(228, 224, 211, 255);
        internal static readonly Color Ink = new Color32(36, 40, 32, 255);
        internal static readonly Color PlateMuted = new Color32(82, 87, 75, 255);
        internal static readonly Color PlateRule = new Color32(181, 180, 165, 255);
        internal static readonly Color PlateInset = new Color32(216, 214, 200, 255);
        internal static readonly Color PlateActive = new Color32(234, 212, 172, 255);
        internal static readonly Color BronzeEdge = new Color32(145, 100, 62, 255);
        internal static readonly Color BronzeInk = new Color32(97, 60, 27, 255);
    }

    public sealed partial class FormationBattleController
    {
        static readonly Color EconomyPanel = new Color32(23, 27, 48, 255);
        static readonly Color EconomyInk = new Color32(242, 246, 255, 255);
        static readonly Color EconomyMuted = new Color32(199, 208, 232, 255);
        static readonly Color EconomyIce = new Color32(167, 217, 255, 255);

        internal static Rect ReunionHopePanelRect => new Rect(FormationExperienceRect.x,
            FormationExperienceRect.yMin - 77, FormationTraitSidebarRect.width + 8, 72);

        internal bool ReunionHopePanelVisible => Campaign != null && Model.Phase == FormationPhase.Preparation &&
            (!Campaign.IsPractice || Campaign.ShowsHopeRewards) &&
            Model.ActiveSynergies.TryGetValue("整合", out int tier) && tier > 0;

        void DrawReunionHopePanel()
        {
            var panel = ReunionHopePanelRect;
            int hope = Campaign.Hope;
            int milestone = Campaign.NextHopeMilestone;
            bool selected = formationInspectedTag == "整合";
            bool interactive = GUI.enabled && !(pointerHeld && shopDragInstance > 0);
            bool hovered = interactive && panel.Contains(Event.current.mousePosition);
            var accent = FormationPreparationPalette.Amber;
            Fill(panel, selected ? FormationPreparationPalette.Selected
                : hovered ? FormationPreparationPalette.Hover : FormationPreparationPalette.Surface);
            ApprovedUi.Border(panel, selected || hovered ? accent : FormationPreparationPalette.Edge);
            Fill(new Rect(panel.x, panel.y, 3, panel.height), accent);
            EconomyLabel(new Rect(panel.x + 11, panel.y + 5, 114, 21), "整合运动", 14, FormationPreparationPalette.Text, true);
            EconomyLabel(new Rect(panel.xMax - 32, panel.y + 5, 21, 21), selected ? "−" : "›",
                17, accent, false, TextAnchor.MiddleRight);
            EconomyLabel(new Rect(panel.x + 11, panel.y + 27, 37, 28), "希望", 13, FormationPreparationPalette.Muted, true);
            EconomyLabel(new Rect(panel.x + 45, panel.y + 24, 61, 32), hope.ToString(), 25, accent, true);
            EconomyLabel(new Rect(panel.x + 106, panel.y + 28, panel.width - 117, 27),
                milestone == 0 ? "已达成" : $"/ {milestone}", 13, FormationPreparationPalette.Text, false, TextAnchor.MiddleRight);
            var track = new Rect(panel.x + 11, panel.yMax - 13, panel.width - 22, 5);
            Fill(track, FormationPreparationPalette.Recess);
            float progress = milestone == 0 ? 1f : Mathf.Clamp01((float)hope / milestone);
            Fill(new Rect(track.x, track.y, track.width * progress, track.height), accent);
            if (interactive && TraitHit(panel, "查看整合运动的希望奖励"))
            {
                if (selected) CloseFormationTraitDetails();
                else InspectFormationTrait("整合");
            }
        }

        internal static Color FormationFeeColor(int cost) => cost switch
        {
            2 => new Color32(44, 132, 124, 255),
            3 => new Color32(83, 111, 209, 255),
            4 => new Color32(129, 99, 217, 255),
            5 => new Color32(211, 168, 80, 255),
            _ => new Color32(128, 134, 151, 255)
        };

        void DrawFormationEconomyHud(bool preparing)
        {
            bool enabled = GUI.enabled;
            var xp = FormationExperienceRect;
            bool maxLevel = Shop.Level >= ShopTrialModel.Odds.Length;
            bool canBuyExperience = enabled && preparing && CanTrade && !maxLevel && Shop.CanAfford(4);
            bool xpHovered = canBuyExperience && xp.Contains(Event.current.mousePosition);
            var xpAccent = maxLevel ? FormationPreparationPalette.Amber : FormationPreparationPalette.Muted;
            Fill(xp, xpHovered ? FormationPreparationPalette.Hover : FormationPreparationPalette.Surface);
            ApprovedUi.Border(xp, xpHovered || maxLevel ? xpAccent : FormationPreparationPalette.Edge);
            Fill(new Rect(xp.x, xp.y, xp.width, 2), xpAccent);
            EconomyLabel(new Rect(xp.x + 7, xp.y + 6, xp.width - 14, 14), "团队等级", 10, FormationPreparationPalette.Muted);
            EconomyLabel(new Rect(xp.x + 7, xp.y + 20, 42, 27), Shop.Level + "级", 20, FormationPreparationPalette.Text, true);
            EconomyLabel(new Rect(xp.x + 44, xp.y + 23, xp.width - 51, 22),
                maxLevel ? "已满级" : $"{Shop.XP}/{Shop.NextLevelXP}", 11, xpAccent, false, TextAnchor.MiddleRight);
            var track = new Rect(xp.x + 7, xp.y + 51, xp.width - 14, 2);
            Fill(track, FormationPreparationPalette.Line);
            float progress = maxLevel ? 1 : Mathf.Clamp01((float)Shop.XP / Shop.NextLevelXP);
            Fill(new Rect(track.x, track.y, track.width * progress, track.height), xpAccent);
            if (!maxLevel)
            {
                string xpPrice = Shop.UnlimitedGold ? "免费" : "4金";
                EconomyLabel(new Rect(xp.x + 5, xp.y + 58, xp.width - 10, 22),
                    $"经验 +{Shop.PurchasedXP}  {xpPrice}", 11,
                    canBuyExperience ? FormationPreparationPalette.Text : FormationPreparationPalette.Muted,
                    true, TextAnchor.MiddleCenter);
                GUI.enabled = canBuyExperience;
                if (TraitHit(xp, "购买团队经验")) BuyExperience();
            }
            GUI.enabled = enabled && preparing;

            var coin = FormationCoinRect;
            var interest = FormationInterestRect;
            var wallet = Rect.MinMaxRect(coin.xMin, interest.yMin, coin.xMax, coin.yMax);
            Color face = marketVisible ? FormationPreparationPalette.Selected : FormationPreparationPalette.Surface;
            Color gold = FormationPreparationPalette.Amber;
            Fill(wallet, Event.current != null && GUI.enabled && wallet.Contains(Event.current.mousePosition)
                ? FormationPreparationPalette.Hover : face);
            ApprovedUi.Border(wallet, FormationPreparationPalette.Edge);
            Fill(new Rect(wallet.x, wallet.y, wallet.width, 2), gold);
            EconomyLabel(new Rect(interest.x + 7, interest.y + 4, interest.width - 14, interest.height - 4),
                Shop.UnlimitedGold ? "无限金币" : $"预计利息 +{FormationDisplayedInterest}", 10, FormationPreparationPalette.Muted);
            DrawFormationCoin(new Rect(coin.x + 8, coin.y + 11, 19, 19));
            EconomyLabel(new Rect(coin.x + 33, coin.y + 5, coin.width - 40, 32),
                Shop.UnlimitedGold ? "∞" : Shop.Gold.ToString(), 26, gold, true);
            Fill(new Rect(coin.x + 7, coin.y + 37, coin.width - 14, 1), FormationPreparationPalette.Line);
            EconomyLabel(new Rect(coin.x + 8, coin.y + 38, coin.width - 17, 20),
                marketVisible ? "收起商店" : "招募商店", 11, FormationPreparationPalette.Text, true);
            if (GUI.Button(wallet, new GUIContent(string.Empty,
                "招募商店 · 点击或按 Tab 切换"), GUIStyle.none))
                ToggleFormationMarket();
            GUI.enabled = enabled;
        }

        void DrawFormationCoin(Rect rect)
        {
            if (formationCoin != null)
                GUI.DrawTexture(rect, formationCoin, ScaleMode.ScaleToFit, true);
        }

        bool EconomyButton(Rect rect, string text, string tooltip, Color face, Color ink, int size = 12)
        {
            bool hovered = GUI.enabled && Event.current != null && rect.Contains(Event.current.mousePosition);
            Fill(rect, hovered ? Color.Lerp(face, Color.white, .17f) : GUI.enabled ? face : Color.Lerp(face, EconomyPanel, .45f));
            EconomyLabel(rect, text, size, GUI.enabled ? ink : Color.Lerp(ink, EconomyMuted, .54f), true, TextAnchor.MiddleCenter);
            return GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
        }

        void EconomyLabel(Rect rect, string value, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft)
            => GUI.Label(rect, value, approvedShop.LoopText(size, alignment, color, bold ? 650 : 400));
    }
}
