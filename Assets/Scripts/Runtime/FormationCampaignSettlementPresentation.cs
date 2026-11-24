using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float SettlementLifeAt = .30f;
        const float SettlementLedgerAt = 2f;
        const float SettlementActionsAt = 2.90f;
        static readonly Color SettlementPaper = new Color32(234, 232, 222, 255);
        static readonly Color SettlementInk = new Color32(36, 39, 36, 255);
        static readonly Color SettlementMuted = new Color32(86, 89, 80, 255);
        static readonly Color SettlementEdge = new Color32(164, 163, 151, 255);
        static readonly Color SettlementRule = new Color32(183, 181, 168, 255);
        static readonly Color SettlementHealth = new Color32(78, 105, 97, 255);
        static readonly Color SettlementTrack = new Color32(207, 205, 195, 255);
        static readonly Color SettlementDebit = new Color32(139, 68, 55, 255);
        static readonly Color SettlementIncome = new Color32(115, 86, 38, 255);
        static readonly Color SettlementAction = new Color32(212, 198, 162, 255);
        static readonly string[] SettlementBallColors = { "白", "蓝", "金", "彩" };

        float campaignSettlementElapsed;
        int campaignSettlementBattleGold;
        bool campaignSettlementHasLootRow;

        internal bool CampaignSettlementResultVisible => Campaign is { IsPractice: false, LastBattleSettlement: { } result } &&
            Model is { Phase: FormationPhase.Won or FormationPhase.Lost } &&
            result.NodeId == Campaign.CurrentNode.Id && !campaignResultSummaryOpen;
        internal float CampaignSettlementElapsed => CampaignSettlementResultVisible ? campaignSettlementElapsed : 0;
        internal bool CampaignSettlementLifeVisible => CampaignSettlementResultVisible && CampaignSettlementElapsed >= SettlementLifeAt;
        internal bool CampaignSettlementLedgerVisible => CampaignSettlementResultVisible && CampaignSettlementElapsed >= SettlementLedgerAt;
        internal bool CampaignSettlementActionsVisible => CampaignSettlementResultVisible && CampaignSettlementElapsed >= SettlementActionsAt;
        internal bool CampaignSettlementKeepsBattleView => CampaignSettlementResultVisible && !CampaignSettlementActionsVisible;
        internal Rect CampaignSettlementPanelRect
        {
            get
            {
                if (!CampaignSettlementResultVisible) return Rect.zero;
                float elapsed = CampaignSettlementElapsed;
                float expand = SettlementMotion(elapsed, 1.80f, 2.20f);
                float appear = SettlementMotion(elapsed, .05f, .30f);
                return new Rect(287, Mathf.Lerp(213, 112, expand) + 8 * (1 - appear),
                    706, Mathf.Lerp(250, campaignSettlementHasLootRow ? 505 : 455, expand));
            }
        }
        internal Rect CampaignSettlementContinueRect
        {
            get
            {
                var panel = CampaignSettlementPanelRect;
                return CampaignSettlementResultVisible ? new Rect(panel.xMax - 210, panel.yMax - 65, 175, 41) : Rect.zero;
            }
        }

        bool CampaignSettlementBlocksPoint(Vector2 point) => CampaignSettlementKeepsBattleView ||
            CampaignSettlementResultVisible && CampaignSettlementPanelRect.Contains(point);

        void StartCampaignSettlementPresentation(int battleGold)
        {
            campaignSettlementElapsed = 0;
            campaignSettlementBattleGold = battleGold;
            campaignSettlementHasLootRow = Campaign.LastBattleSettlement.Drops.Count > 0;
        }

        void TickCampaignSettlementPresentation()
        {
            if (!CampaignSettlementKeepsBattleView) return;
            // A hitch or a suspended window should not skip an entire visible beat.
            campaignSettlementElapsed = Mathf.Min(SettlementActionsAt,
                campaignSettlementElapsed + Mathf.Min(Time.unscaledDeltaTime, .10f));
        }

        static float SettlementMotion(float elapsed, float start, float end) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, elapsed));

        static Color SettlementFade(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);

        static void SettlementText(Rect rect, string text, int size, Color color, float alpha,
            int weight = 400, TextAnchor align = TextAnchor.MiddleLeft)
        {
            GUI.Label(rect, text, ApprovedUi.Text(null, size, align, SettlementFade(color, alpha), weight));
        }

        void DrawCampaignResult()
        {
            if (!CampaignSettlementResultVisible) return;
            var result = Campaign.LastBattleSettlement;
            float elapsed = CampaignSettlementElapsed;
            float appear = SettlementMotion(elapsed, .05f, .30f);
            Fill(new Rect(0, 0, 1280, 720), SettlementFade(new Color32(29, 31, 27, 143),
                SettlementMotion(elapsed, 0, .30f)));
            if (appear <= 0) return;

            var r = CampaignSettlementPanelRect;
            Fill(new Rect(r.x + 3, r.y + 6, r.width, r.height), SettlementFade(new Color32(7, 8, 5, 66), appear));
            Fill(r, SettlementFade(SettlementPaper, appear));
            ApprovedUi.Border(r, SettlementFade(SettlementEdge, appear));
            float x = r.x + 35, right = r.xMax - 35, y = r.y, width = right - x;
            string kind = Campaign.CurrentNode.Kind == "战斗战备" ? "战斗" : Campaign.CurrentNode.Kind;
            if (kind == "奖励") kind = "奖励战";
            SettlementText(new Rect(x, y + 17, width, 24), result.NodeId + "  ·  " + kind, 17, SettlementMuted, appear);
            SettlementText(new Rect(x, y + 41, width, 49), result.Won ? "蓝门突破" : "本关失利", 39, SettlementInk, appear, 700);
            string outcome = result.Won ? "本关胜利" : Model.RemainingDefenders > 0
                ? $"超时 · 守军 {Model.RemainingDefenders}/{Model.InitialDefenderCount}" : "超时 · 未进入蓝门";
            SettlementText(new Rect(x, y + 55, width, 30), outcome, 20, result.Won ? SettlementMuted : SettlementDebit,
                appear, align: TextAnchor.MiddleRight);
            Fill(new Rect(x, y + 101, width, 1), SettlementFade(SettlementRule, appear));

            DrawCampaignSettlementLife(result, x, right, y, elapsed);
            if (CampaignSettlementLedgerVisible)
                DrawCampaignSettlementLedger(result, x, right, y, elapsed);
            DrawCampaignSettlementAction(elapsed);
        }

        void DrawCampaignSettlementLife(FormationCampaignRun.BattleSettlement result, float x, float right, float y, float elapsed)
        {
            float before = SettlementMotion(elapsed, SettlementLifeAt, .46f);
            if (before <= 0) return;
            SettlementText(new Rect(x, y + 119, 250, 32), "队伍生命", 20, SettlementMuted, before, 500);
            SettlementText(new Rect(x, y + 173, 84, 48), result.SquadHPBefore.ToString(), 39, SettlementMuted, before);
            float after = SettlementMotion(elapsed, .42f, .60f);
            float debit = SettlementMotion(elapsed, .59f, 1.01f);
            float status = SettlementMotion(elapsed, .67f, 1.03f);
            float health = Mathf.Lerp(result.SquadHPBefore, result.SquadHPAfter, debit);
            SettlementText(new Rect(x + 84, y + 173, 60, 48), "→", 29, SettlementMuted, after);
            SettlementText(new Rect(x + 145, y + 144 + 5 * (1 - after), 160, 82), Mathf.RoundToInt(health).ToString(),
                72, SettlementInk, after, 700);
            float bar = x + 326, width = right - bar;
            Fill(new Rect(bar, y + 178, width, 8), SettlementFade(SettlementTrack, before));
            float current = width * Mathf.Clamp01(health / 100f);
            Fill(new Rect(bar, y + 178, current, 8), SettlementFade(SettlementHealth, before));
            int damage = result.SquadHPBefore - result.SquadHPAfter;
            if (damage > 0)
            {
                float original = width * Mathf.Clamp01(result.SquadHPBefore / 100f);
                float mark = 1 - SettlementMotion(elapsed, 1.22f, 1.62f);
                Fill(new Rect(bar + current, y + 178, original - current, 8), SettlementFade(SettlementDebit, mark));
            }
            string caption = damage == 0 ? "本关未扣除生命" : $"本关扣除 {damage} 点生命";
            SettlementText(new Rect(bar, y + 191, width, 29), caption, 18,
                damage == 0 ? SettlementMuted : SettlementDebit, status, 500);
        }

        void DrawCampaignSettlementLedger(FormationCampaignRun.BattleSettlement result, float x, float right,
            float y, float elapsed)
        {
            float reveal = SettlementMotion(elapsed, SettlementLedgerAt, 2.26f);
            float width = right - x;
            Fill(new Rect(x, y + 237, width, 1), SettlementFade(SettlementRule, reveal));
            SettlementText(new Rect(x, y + 246, width, 28), "本关收益", 20, SettlementInk, reveal, 500);
            DrawCampaignIncomeColumn(x, y, "基础金币", result.BaseGold, SettlementIncome, elapsed, 2.06f);
            DrawCampaignIncomeColumn(x + 223, y, "利息", result.Interest, SettlementMuted, elapsed, 2.18f);
            DrawCampaignIncomeColumn(x + 445, y, "经验", result.Experience, SettlementInk, elapsed, 2.30f);
            Fill(new Rect(x + 201, y + 287, 1, 56), SettlementFade(SettlementRule, reveal));
            Fill(new Rect(x + 423, y + 287, 1, 56), SettlementFade(SettlementRule, reveal));

            var extras = new List<string>(5);
            if (result.Streak > 0) extras.Add($"连胜 +{result.Streak}");
            if (result.DebtPaid > 0) extras.Add($"偿债 −{result.DebtPaid}");
            if (result.BonusGold > 0) extras.Add($"其他金币 +{result.BonusGold}");
            if (campaignSettlementBattleGold > 0) extras.Add($"战斗技能 +{campaignSettlementBattleGold}");
            if (extras.Count > 0) extras.Add($"实收金币 +{result.Gold + campaignSettlementBattleGold}");
            float details = SettlementMotion(elapsed, 2.35f, 2.63f);
            SettlementText(new Rect(x, y + 349, width, 20), string.Join("  ·  ", extras), 15, SettlementMuted, details);
            string progress = Campaign.CurrentNode.Kind == "遭遇" ? $"守军生命削减 {Model.EncounterProgress:P1}" : "";
            if (result.Hope > 0)
                progress = progress.Length > 0 ? $"希望 +{result.Hope}  ·  {progress}" : $"希望 +{result.Hope}";
            SettlementText(new Rect(x, y + 369, width, 20), progress, 15, SettlementMuted, details);

            if (!campaignSettlementHasLootRow)
            {
                if (HasGroundRewards)
                    SettlementText(new Rect(x, y + 396, 450, 25), "此前未领取的战利品可留到下次整备", 15,
                        SettlementMuted, details);
                return;
            }
            float loot = SettlementMotion(elapsed, 2.51f, 2.80f);
            Fill(new Rect(x, y + 394, width, 1), SettlementFade(SettlementRule, loot));
            SettlementText(new Rect(x, y + 399, width, 29), "战利品", 20, SettlementInk, loot, 500);
            foreach (string color in SettlementBallColors)
            {
                int count = result.Drops.Count(drop => drop.Color == color);
                if (count == 0) continue;
                var art = SupplyOrbArt(color);
                if (art != null)
                {
                    var old = GUI.color;
                    GUI.color = SettlementFade(old, loot);
                    GUI.DrawTexture(new Rect(x, y + 429, 25, 25), art, ScaleMode.ScaleToFit, true);
                    GUI.color = old;
                }
                SettlementText(new Rect(x + 31, y + 426, 58, 30), $"{color}球 ×{count}", 15, SettlementMuted, loot);
                x += 97;
            }
            int components = result.Drops.Count(drop => drop.Component != null);
            if (components > 0)
                SettlementText(new Rect(x, y + 426, 64, 30), $"散件 ×{components}", 15, SettlementMuted, loot);
            // Keep the button clear; four ball types and a component fit to its left.
            var panel = CampaignSettlementPanelRect;
            if (HasGroundRewards)
                SettlementText(new Rect(panel.x + 35, y + 463, 450, 25), "未领取的战利品可留到下次整备", 15, SettlementMuted, loot);
        }

        static void DrawCampaignIncomeColumn(float x, float y, string title, int value, Color color, float elapsed, float start)
        {
            float alpha = SettlementMotion(elapsed, start, start + .25f);
            float rise = 7 * (1 - alpha);
            SettlementText(new Rect(x, y + 274 + rise, 190, 30), title, 18, SettlementMuted, alpha);
            SettlementText(new Rect(x, y + 301 + rise, 190, 44), value > 0 ? $"+{value}" : value.ToString(), 37, color, alpha, 650);
        }

        void DrawCampaignSettlementAction(float elapsed)
        {
            float alpha = SettlementMotion(elapsed, 2.75f, SettlementActionsAt);
            if (alpha <= 0) return;
            var rect = CampaignSettlementContinueRect;
            string next = Campaign.HasBlockingChoice ? "先完成必要选择" : Campaign.Finished ? "查看最终结算" : "继续下一节点";
            bool wasEnabled = GUI.enabled;
            bool available = wasEnabled && CampaignSettlementActionsVisible && !Campaign.HasBlockingChoice && openingGroundBall == null;
            bool hover = available && rect.Contains(Event.current.mousePosition);
            var background = available ? (hover ? Color.Lerp(SettlementAction, SettlementPaper, .32f) : SettlementAction) : SettlementTrack;
            Fill(rect, SettlementFade(background, alpha));
            ApprovedUi.Border(rect, SettlementFade(SettlementEdge, alpha));
            SettlementText(new Rect(rect.x + 14, rect.y, rect.width - 36, rect.height), next, 18, SettlementInk, alpha, 650);
            SettlementText(new Rect(rect.xMax - 29, rect.y, 17, rect.height), "›", 23, SettlementInk, alpha, 650);
            GUI.enabled = available;
            bool pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = wasEnabled;
            if (!pressed) return;
            if (Campaign.Finished) campaignResultSummaryOpen = true;
            else ContinueCampaign();
        }
    }
}
