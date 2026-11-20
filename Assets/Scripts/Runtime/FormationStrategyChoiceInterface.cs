using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float StrategyCardWidth = 340, StrategyCardHeight = 362, StrategyCardGap = 18;
        const float StrategyDealLead = .12f, StrategyDealGap = .10f, StrategyDealSeconds = .35f;
        const float StrategyPickSeconds = .23f, StrategyConfirmSeconds = .21f;
        static readonly Color StrategyLight = new Color32(246, 247, 244, 255);
        static readonly Color StrategyMuted = new Color32(193, 208, 215, 255);
        static readonly Color StrategyGold = new Color32(239, 203, 131, 255);
        static readonly Color StrategySelection = new Color32(160, 238, 228, 255);
        static readonly Color[] StrategyInk = {
            new Color32(35, 44, 46, 255), new Color32(28, 47, 62, 255),
            new Color32(54, 41, 29, 255), new Color32(37, 39, 66, 255)
        };
        static readonly Color[] StrategyStamps = {
            new Color32(139, 98, 48, 255), new Color32(60, 92, 115, 255),
            new Color32(125, 77, 30, 255), new Color32(89, 81, 142, 255)
        };
        static readonly Color[] StrategyGlints = {
            StrategyLight, new Color32(177, 220, 241, 255), StrategyGold, new Color32(201, 182, 255, 255)
        };
        const string StrategyNumber = @"\d-\d|[+＋×−]?\d+(?:\.\d+)?(?:%|XP|SP|金币|金|秒|级|件|格|次|名|张)?";
        static readonly Regex StrategyNumbers = new Regex(StrategyNumber);
        static readonly Regex StrategyTextGroups = new Regex(
            "(?:" + StrategyNumber + @"|[A-Za-z]+|[（【《“]*.)[，。；、！？：）】》”]*");
        static Texture2D strategyPlates, strategyProps, strategyVeil;
        IReadOnlyList<FormationCampaignRun.ChoiceOption> displayedStrategyOptions;
        (GUIContent Content, GUIStyle Style)[] strategyDescriptions;
        int selectedStrategyCard = -1;
        double strategyOpenedAt, strategyDealtAt, strategyPickedAt;
        double? strategyConfirmedAt;

        bool DrawStrategyChoice()
        {
            var run = Campaign;
            var options = run?.HasBlockingChoice == true ? run.Choices : null;
            if (options == null || options.Count == 0 || options[0].Strategy == null)
            {
                displayedStrategyOptions = null;
                strategyDescriptions = null;
                strategyConfirmedAt = null;
                return false;
            }

            if (strategyPlates == null) strategyPlates = Resources.Load<Texture2D>("UI/Strategy/plates");
            if (strategyProps == null) strategyProps = Resources.Load<Texture2D>("UI/Strategy/props");
            if (strategyVeil == null) strategyVeil = Resources.Load<Texture2D>("UI/Strategy/veil");

            string tier = options[0].Strategy.Tier;
            int column = tier switch { "银" => 1, "金" => 2, "彩" => 3, _ => 0 };
            bool preplan = column == 0;
            double now = Time.unscaledTimeAsDouble;
            if (!ReferenceEquals(options, displayedStrategyOptions))
            {
                if (displayedStrategyOptions == null || displayedStrategyOptions[0].Strategy.Tier != tier)
                    strategyOpenedAt = now;
                displayedStrategyOptions = options;
                selectedStrategyCard = -1;
                strategyDealtAt = now;
                strategyConfirmedAt = null;
                strategyDescriptions = new (GUIContent, GUIStyle)[options.Count];
                for (int i = 0; i < options.Count; i++)
                    strategyDescriptions[i] = FitStrategyDescription(options[i].Description, StrategyInk[column]);
            }
            string title = preplan ? "选择战略预案" : "选择战略战备";
            string node = preplan ? (run.IsPractice ? "开局" : "1-1 开局") : run.CurrentNode.Id + " 战前";
            string count = options.Count == 3 ? "三选一" : options.Count == 2 ? "二选一" : "一项可选";
            float open = preplan ? 1 : StrategyEaseOut(StrategyProgress(now - strategyOpenedAt, .24f));
            DrawStrategyBackdrop(title, node, preplan ? count : tier + "档" + count, open);

            double dealElapsed = now - strategyDealtAt;
            float dealEnd = StrategyDealLead + (options.Count - 1) * StrategyDealGap + StrategyDealSeconds;
            bool acceptingInput = preplan || dealElapsed >= dealEnd && !strategyConfirmedAt.HasValue;
            float confirmProgress = strategyConfirmedAt.HasValue ? StrategyProgress(now - strategyConfirmedAt.Value, StrategyConfirmSeconds) : 0;

            var key = Event.current;
            bool confirmFromKey = false;
            if (acceptingInput && GUI.enabled && key.type == EventType.KeyDown && !key.alt && !key.control && !key.command)
            {
                int index = (int)key.keyCode - (int)KeyCode.Alpha1;
                if (index >= 0 && index < options.Count)
                {
                    SelectStrategyCard(index, now);
                    key.Use();
                }
                else if (selectedStrategyCard >= 0 && (key.keyCode == KeyCode.Return || key.keyCode == KeyCode.KeypadEnter))
                {
                    confirmFromKey = true;
                    key.Use();
                }
            }

            float start = (1280 - options.Count * StrategyCardWidth - (options.Count - 1) * StrategyCardGap) * .5f;
            bool enabled = GUI.enabled;
            for (int i = 0; i < options.Count; i++)
            {
                var rect = new Rect(start + i * (StrategyCardWidth + StrategyCardGap), 217, StrategyCardWidth, StrategyCardHeight);
                GUI.enabled = enabled && acceptingInput;
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) SelectStrategyCard(i, now);
                GUI.enabled = enabled;
                bool selected = i == selectedStrategyCard;
                if (preplan)
                    DrawStrategyCard(rect, options[i], strategyDescriptions[i], column, i, selected);
                else
                {
                    float deal = StrategyProgress(dealElapsed - StrategyDealLead - i * StrategyDealGap, StrategyDealSeconds);
                    float pick = selected ? StrategyProgress(now - strategyPickedAt, StrategyPickSeconds) : 1;
                    DrawDealtStrategyCard(rect, options[i], strategyDescriptions[i], column, i, selected, deal, pick, confirmProgress);
                }
            }

            var color = GUI.color;
            GUI.color *= new Color(1, 1, 1, preplan ? 1 : StrategyProgress(dealElapsed - dealEnd + .20f, .20f));
            bool reroll = DrawStrategyAction(new Rect(468, 606, 160, 44),
                "免费重抽   " + (run.CanRerollChoice ? "1 / 1" : "0 / 1"), run.CanRerollChoice && acceptingInput);
            bool confirm = DrawStrategyAction(new Rect(638, 606, 174, 44), preplan ? "确认预案" : "确认战备",
                selectedStrategyCard >= 0 && acceptingInput, true) || confirmFromKey;
            GUI.color = color;
            if (reroll)
            {
                if (run.RerollChoice()) selectedStrategyCard = -1;
                return true;
            }
            if (confirm && !preplan) strategyConfirmedAt = now;
            if ((confirm && preplan) || (confirmProgress >= 1 && Event.current.type == EventType.Repaint))
            {
                ChooseCampaignOption(selectedStrategyCard);
                selectedStrategyCard = -1;
                strategyConfirmedAt = null;
            }
            return true;
        }

        void SelectStrategyCard(int index, double now)
        {
            if (selectedStrategyCard == index) return;
            selectedStrategyCard = index;
            strategyPickedAt = now;
        }

        static float StrategyProgress(double elapsed, float duration) => Mathf.Clamp01((float)(elapsed / duration));
        static float StrategyEaseOut(float progress) => 1 - (1 - progress) * (1 - progress) * (1 - progress);

        void DrawStrategyBackdrop(string title, string node, string subtitle, float open)
        {
            var color = GUI.color;
            GUI.color *= new Color(1, 1, 1, Mathf.Lerp(.45f, 1, open));
            var screen = new Rect(0, 0, 1280, 720);
            if (strategyVeil != null) GUI.DrawTexture(screen, strategyVeil);
            else Fill(screen, StrategyTint(new Color(.09f, .14f, .18f, .95f)));

            Fill(new Rect(0, 0, 1280, 58), StrategyTint(new Color32(21, 31, 38, 244)));
            Fill(new Rect(0, 57, 1280, 1), StrategyTint(new Color32(168, 201, 208, 36)));
            StrategyLabel(new Rect(27, 16, 16, 24), "◆", 13, StrategyGold, 700);
            StrategyLabel(new Rect(51, 16, 120, 24), "昨日圆车", 13, StrategyLight, 700);
            GUI.color = color * new Color(1, 1, 1, open);
            float lift = (1 - open) * 10;
            Fill(new Rect(448 - 98 * open, 132 + lift, 98 * open, 1), StrategyTint(new Color32(182, 203, 212, 96)));
            Fill(new Rect(832, 132 + lift, 98 * open, 1), StrategyTint(new Color32(182, 203, 212, 96)));
            Fill(new Rect(443, 132 + lift, 5, 6), StrategyTint(StrategyGold));
            Fill(new Rect(832, 132 + lift, 5, 6), StrategyTint(StrategyGold));
            StrategyLabel(new Rect(449, 98 + lift, 382, 67), title, 47, StrategyLight, 700, TextAnchor.MiddleCenter);
            StrategyLabel(new Rect(450, 168 + lift, 380, 25), "<color=#EFCB83><b>" + node + "</b></color>　/　" + subtitle,
                14, StrategyMuted, 400, TextAnchor.MiddleCenter, true);
            GUI.color = color;
        }

        void DrawDealtStrategyCard(Rect rect, FormationCampaignRun.ChoiceOption option,
            (GUIContent Content, GUIStyle Style) description, int column, int index, bool selected,
            float deal, float pick, float confirm)
        {
            float arrive = StrategyEaseOut(deal);
            float pulse = Mathf.Sin(pick * Mathf.PI);
            float shrink = StrategyEaseOut(confirm);
            float scale = 1 + .018f * pulse + (selected ? .035f : -.05f) * shrink;
            float alpha = Mathf.Clamp01(deal * 2.7f) * (1 - (selected ? .45f : .88f) * confirm);
            var matrix = GUI.matrix;
            var color = GUI.color;
            rect.y += (1 - arrive) * 45 + (selected ? -9 : 16) * shrink;
            GUIUtility.ScaleAroundPivot(new Vector2(Mathf.Lerp(.56f, 1, arrive) * scale, scale), rect.center);
            GUI.color *= new Color(1, 1, 1, alpha);
            DrawStrategyCard(rect, option, description, column, index, selected);

            float shine = Mathf.Max(Mathf.Sin(deal * Mathf.PI) * .70f, pulse * .55f, selected ? confirm * .72f : 0);
            if (shine > .01f)
            {
                var glint = StrategyGlints[column];
                glint.a = shine;
                Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 15, 2), StrategyTint(glint));
                Fill(new Rect(rect.x + 2, rect.y + 2, 2, 38), StrategyTint(glint));
            }
            GUI.color = color;
            GUI.matrix = matrix;
        }

        static Color StrategyTint(Color color)
        {
            color.a *= GUI.color.a;
            return color;
        }

        void DrawStrategyCard(Rect rect, FormationCampaignRun.ChoiceOption option,
            (GUIContent Content, GUIStyle Style) description, int column, int index, bool selected)
        {
            if (strategyPlates != null)
            {
                if (selected) Fill(new Rect(rect.x - 2, rect.y - 2, rect.width - 18, rect.height + 4), StrategyTint(new Color32(160, 238, 228, 34)));
                GUI.DrawTextureWithTexCoords(rect, strategyPlates, StrategyTile(column, selected ? 1 : 0, 4, 2));
            }
            else
            {
                Fill(rect, StrategyTint(ApprovedUi.Paper));
                ApprovedUi.Border(rect, StrategyTint(selected ? StrategySelection : ApprovedUi.Rule), selected ? 2 : 1);
            }

            var record = option.Strategy;
            var ink = StrategyInk[column];
            int art = StrategyIllustration(record.Id, record.Kind);
            if (strategyProps != null)
            {
                var picture = new Rect(rect.center.x - 109, rect.y - 6, 218, 218);
                GUI.DrawTextureWithTexCoords(picture, strategyProps, StrategyTile(art % 3, art / 3, 3, 2));
            }
            string kind = column == 0 ? record.Kind switch { "经济" => "军费", "装备" => "军械", _ => record.Kind } : record.Kind;
            StrategyLabel(new Rect(rect.x + 16, rect.y + 12, 140, 22), "◆ " + kind, 12, StrategyStamps[column], 650);
            StrategyLabel(new Rect(rect.x + 20, rect.y + 214, 300, 41), option.Name, 29, ink, 700);

            GUI.Label(new Rect(rect.x + 20, rect.y + 263, 300, 73), description.Content, description.Style);
            StrategyLabel(new Rect(rect.x + 20, rect.yMax - 26, 28, 21), (index + 1).ToString(), 11, StrategyStamps[column]);
            if (selected) StrategyLabel(new Rect(rect.xMax - 34, rect.yMax - 30, 24, 21), "✓", 15, StrategyLight, 700, TextAnchor.MiddleCenter);
        }

        static (GUIContent, GUIStyle) FitStrategyDescription(string text, Color ink)
        {
            var style = ApprovedUi.Text(null, 15, TextAnchor.UpperLeft, ink);
            style.richText = true;
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            var groups = StrategyTextGroups.Matches(text);
            var content = new GUIContent();
            // Keep closing punctuation and numeric values together when forming Chinese lines.
            for (int size = 15; size >= 12; size--)
            {
                style.fontSize = size;
                var lines = new StringBuilder();
                string line = "";
                foreach (Match group in groups)
                {
                    string word = StrategyNumbers.Replace(group.Value, "<b>$&</b>");
                    content.text = line + word;
                    if (line.Length > 0 && style.CalcSize(content).x > 300)
                    {
                        lines.AppendLine(line);
                        line = word.TrimStart();
                    }
                    else line = content.text;
                }
                content.text = lines.Append(line).ToString();
                if (style.CalcSize(content).y <= 73) break;
            }
            return (content, style);
        }

        static Rect StrategyTile(int x, int y, int columns, int rows) =>
            new Rect(x / (float)columns, (rows - y - 1) / (float)rows, 1f / columns, 1f / rows);

        static int StrategyIllustration(string id, string kind) => id switch
        {
            "P07" or "P08" or "ES6" or "EG3" or "EP3" or "EP5" or "CP6" => 1,
            "P03" or "P05" or "ES5" or "EG4" => 2,
            "P12" or "CS1" or "CS2" or "CS6" or "CG3" or "CG5" => 3,
            "P02" or "P06" or "P14" or "ES4" or "EG6" or "EP6" or "CS5" or "CG4" => 4,
            _ => kind == "经济" ? 0 : 5
        };

        static void StrategyLabel(Rect rect, string value, int size, Color color, int weight = 400,
            TextAnchor align = TextAnchor.MiddleLeft, bool rich = false)
        {
            var style = ApprovedUi.Text(null, size, align, color, weight);
            style.richText = rich;
            GUI.Label(rect, value, style);
        }

        static bool DrawStrategyAction(Rect rect, string text, bool available, bool confirm = false)
        {
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && available;
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var face = confirm && available ? (hover ? new Color32(249, 220, 161, 255) : StrategyGold)
                : hover ? new Color32(40, 60, 73, 255) : new Color32(27, 41, 51, 255);
            Fill(rect, StrategyTint(face));
            ApprovedUi.Border(rect, StrategyTint(confirm && available ? StrategyGold : new Color32(108, 129, 139, 184)));
            StrategyLabel(rect, text, 14, !available ? StrategyMuted : confirm ? StrategyInk[2] : StrategyLight,
                650, TextAnchor.MiddleCenter);
            bool click = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = enabled;
            return click;
        }
    }
}
