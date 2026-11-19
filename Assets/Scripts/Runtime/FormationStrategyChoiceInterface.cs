using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float StrategyCardWidth = 340, StrategyCardHeight = 362, StrategyCardGap = 18;
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
        const string StrategyNumber = @"\d-\d|[+＋×−]?\d+(?:\.\d+)?(?:%|XP|SP|金币|金|秒|级|件|格|次|名|张)?";
        static readonly Regex StrategyNumbers = new Regex(StrategyNumber);
        static readonly Regex StrategyTextGroups = new Regex(
            "(?:" + StrategyNumber + @"|[A-Za-z]+|[（【《“]*.)[，。；、！？：）】》”]*");
        static Texture2D strategyPlates, strategyProps, strategyVeil;
        IReadOnlyList<FormationCampaignRun.ChoiceOption> displayedStrategyOptions;
        (GUIContent Content, GUIStyle Style)[] strategyDescriptions;
        int selectedStrategyCard = -1;

        bool DrawStrategyChoice()
        {
            var run = Campaign;
            var options = run?.HasBlockingChoice == true ? run.Choices : null;
            if (options == null || options.Count == 0 || options[0].Strategy == null)
            {
                displayedStrategyOptions = null;
                strategyDescriptions = null;
                return false;
            }

            if (strategyPlates == null) strategyPlates = Resources.Load<Texture2D>("UI/Strategy/plates");
            if (strategyProps == null) strategyProps = Resources.Load<Texture2D>("UI/Strategy/props");
            if (strategyVeil == null) strategyVeil = Resources.Load<Texture2D>("UI/Strategy/veil");

            string tier = options[0].Strategy.Tier;
            int column = tier switch { "银" => 1, "金" => 2, "彩" => 3, _ => 0 };
            if (!ReferenceEquals(options, displayedStrategyOptions))
            {
                displayedStrategyOptions = options;
                selectedStrategyCard = -1;
                strategyDescriptions = new (GUIContent, GUIStyle)[options.Count];
                for (int i = 0; i < options.Count; i++)
                    strategyDescriptions[i] = FitStrategyDescription(options[i].Description, StrategyInk[column]);
            }
            bool preplan = column == 0;
            string title = preplan ? "选择战略预案" : "选择战略战备";
            string node = preplan ? (run.IsPractice ? "开局" : "1-1 开局") : run.CurrentNode.Id + " 战前";
            string count = options.Count == 3 ? "三选一" : options.Count == 2 ? "二选一" : "一项可选";
            DrawStrategyBackdrop(title, node, preplan ? count : tier + "档" + count);

            var key = Event.current;
            bool confirmFromKey = false;
            if (GUI.enabled && key.type == EventType.KeyDown && !key.alt && !key.control && !key.command)
            {
                int index = (int)key.keyCode - (int)KeyCode.Alpha1;
                if (index >= 0 && index < options.Count)
                {
                    selectedStrategyCard = index;
                    key.Use();
                }
                else if (selectedStrategyCard >= 0 && (key.keyCode == KeyCode.Return || key.keyCode == KeyCode.KeypadEnter))
                {
                    confirmFromKey = true;
                    key.Use();
                }
            }

            float start = (1280 - options.Count * StrategyCardWidth - (options.Count - 1) * StrategyCardGap) * .5f;
            for (int i = 0; i < options.Count; i++)
            {
                var rect = new Rect(start + i * (StrategyCardWidth + StrategyCardGap), 217, StrategyCardWidth, StrategyCardHeight);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) selectedStrategyCard = i;
                DrawStrategyCard(rect, options[i], strategyDescriptions[i], column, i, i == selectedStrategyCard);
            }

            bool canReroll = run.CanRerollChoice;
            if (DrawStrategyAction(new Rect(468, 606, 160, 44), "免费重抽   " + (canReroll ? "1 / 1" : "0 / 1"), canReroll))
            {
                if (run.RerollChoice()) selectedStrategyCard = -1;
                return true;
            }
            if (DrawStrategyAction(new Rect(638, 606, 174, 44), preplan ? "确认预案" : "确认战备", selectedStrategyCard >= 0, true) || confirmFromKey)
            {
                ChooseCampaignOption(selectedStrategyCard);
                selectedStrategyCard = -1;
            }
            return true;
        }

        void DrawStrategyBackdrop(string title, string node, string subtitle)
        {
            var screen = new Rect(0, 0, 1280, 720);
            if (strategyVeil != null) GUI.DrawTexture(screen, strategyVeil);
            else Fill(screen, new Color(.09f, .14f, .18f, .95f));

            Fill(new Rect(0, 0, 1280, 58), new Color32(21, 31, 38, 244));
            Fill(new Rect(0, 57, 1280, 1), new Color32(168, 201, 208, 36));
            StrategyLabel(new Rect(27, 16, 16, 24), "◆", 13, StrategyGold, 700);
            StrategyLabel(new Rect(51, 16, 120, 24), "昨日圆车", 13, StrategyLight, 700);
            Fill(new Rect(350, 132, 98, 1), new Color32(182, 203, 212, 96));
            Fill(new Rect(832, 132, 98, 1), new Color32(182, 203, 212, 96));
            Fill(new Rect(443, 132, 5, 6), StrategyGold);
            Fill(new Rect(832, 132, 5, 6), StrategyGold);
            StrategyLabel(new Rect(449, 98, 382, 67), title, 47, StrategyLight, 700, TextAnchor.MiddleCenter);
            StrategyLabel(new Rect(450, 168, 380, 25), "<color=#EFCB83><b>" + node + "</b></color>　/　" + subtitle,
                14, StrategyMuted, 400, TextAnchor.MiddleCenter, true);
        }

        void DrawStrategyCard(Rect rect, FormationCampaignRun.ChoiceOption option,
            (GUIContent Content, GUIStyle Style) description, int column, int index, bool selected)
        {
            if (strategyPlates != null)
            {
                if (selected) Fill(new Rect(rect.x - 2, rect.y - 2, rect.width - 18, rect.height + 4), new Color32(160, 238, 228, 34));
                GUI.DrawTextureWithTexCoords(rect, strategyPlates, StrategyTile(column, selected ? 1 : 0, 4, 2));
            }
            else
            {
                Fill(rect, ApprovedUi.Paper);
                ApprovedUi.Border(rect, selected ? StrategySelection : ApprovedUi.Rule, selected ? 2 : 1);
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
            Fill(rect, face);
            ApprovedUi.Border(rect, confirm && available ? StrategyGold : new Color32(108, 129, 139, 184));
            StrategyLabel(rect, text, 14, !available ? StrategyMuted : confirm ? StrategyInk[2] : StrategyLight,
                650, TextAnchor.MiddleCenter);
            bool click = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = enabled;
            return click;
        }
    }
}
