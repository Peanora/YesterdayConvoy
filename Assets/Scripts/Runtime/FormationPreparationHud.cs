using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color EconomyPanel = new Color32(23, 27, 48, 255);
        static readonly Color EconomyLine = new Color32(94, 104, 138, 255);
        static readonly Color EconomyInk = new Color32(242, 246, 255, 255);
        static readonly Color EconomyMuted = new Color32(199, 208, 232, 255);
        static readonly Color EconomyGold = new Color32(238, 200, 135, 255);
        static readonly Color EconomyIce = new Color32(167, 217, 255, 255);
        static readonly Color EconomyPaper = new Color32(230, 234, 250, 255);

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
            Fill(xp, new Color32(63, 81, 167, 255));
            Fill(new Rect(xp.x, xp.y, xp.width, 2), new Color32(185, 200, 255, 255));
            EconomyLabel(new Rect(xp.x + 7, xp.y + 6, xp.width - 14, 14), "团队等级", 10, EconomyInk);
            EconomyLabel(new Rect(xp.x + 7, xp.y + 20, 42, 27), Shop.Level + "级", 20, EconomyInk, true);
            EconomyLabel(new Rect(xp.x + 44, xp.y + 23, xp.width - 51, 22),
                Shop.Level == 10 ? "已满级" : $"{Shop.XP}/{Shop.NextLevelXP}", 11, EconomyInk, false, TextAnchor.MiddleRight);
            var track = new Rect(xp.x + 7, xp.y + 51, xp.width - 14, 2);
            Fill(track, new Color32(121, 139, 210, 255));
            float progress = Shop.Level == 10 ? 1 : Mathf.Clamp01((float)Shop.XP / Shop.NextLevelXP);
            Fill(new Rect(track.x, track.y, track.width * progress, track.height), EconomyIce);
            GUI.enabled = enabled && preparing && CanTrade && Shop.Level < 10 && Shop.CanAfford(4);
            string xpPrice = Shop.UnlimitedGold ? "免费" : "4金";
            if (EconomyButton(new Rect(xp.x + 5, xp.y + 58, xp.width - 10, 22),
                $"经验 +{Shop.PurchasedXP}  {xpPrice}", "购买团队经验",
                new Color32(43, 57, 124, 255), EconomyInk, 10))
                BuyExperience();
            GUI.enabled = enabled && preparing;

            var coin = FormationCoinRect;
            var interest = FormationInterestRect;
            var wallet = Rect.MinMaxRect(coin.xMin, interest.yMin, coin.xMax, coin.yMax);
            Color face = marketVisible ? new Color32(55, 59, 97, 255) : EconomyPaper;
            Color ink = marketVisible ? EconomyInk : new Color32(33, 38, 59, 255);
            Color gold = marketVisible ? EconomyGold : new Color32(117, 78, 35, 255);
            Fill(wallet, Event.current != null && GUI.enabled && wallet.Contains(Event.current.mousePosition)
                ? Color.Lerp(face, Color.white, .12f) : face);
            Fill(new Rect(wallet.x, wallet.y, wallet.width, 2), EconomyGold);
            EconomyLabel(new Rect(interest.x + 7, interest.y + 4, interest.width - 14, interest.height - 4),
                Shop.UnlimitedGold ? "无限金币" : $"预计利息 +{FormationDisplayedInterest}", 10, gold, true);
            DrawFormationCoin(new Rect(coin.x + 8, coin.y + 11, 19, 19));
            EconomyLabel(new Rect(coin.x + 33, coin.y + 5, coin.width - 40, 32),
                Shop.UnlimitedGold ? "∞" : Shop.Gold.ToString(), 26, gold, true);
            EconomyLabel(new Rect(coin.x + 8, coin.y + 38, coin.width - 17, 20),
                marketVisible ? "收起商店" : "招募商店", 11, ink, true);
            EconomyLabel(new Rect(coin.xMax - 28, coin.y + 38, 19, 20), "Tab", 9, ink, false, TextAnchor.MiddleRight);
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
