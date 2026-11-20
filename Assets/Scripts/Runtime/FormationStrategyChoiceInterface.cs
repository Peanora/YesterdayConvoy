using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float StrategyCardWidth = 340, StrategyCardHeight = 362, StrategyCardGap = 18;
        const float StrategyDealLead = .105f, StrategyDealGap = .092f, StrategyDealSeconds = .61f;
        const float StrategyPickSeconds = .23f, StrategyConfirmSeconds = .535f, StrategyRerollSeconds = .273f;
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
        static Texture2D strategyPlates, strategyProps, strategyVeil, strategyCardBack, strategyLightStreak;
        IReadOnlyList<FormationCampaignRun.ChoiceOption> displayedStrategyOptions;
        (GUIContent Content, GUIStyle Style)[] strategyDescriptions;
        int selectedStrategyCard = -1;
        double strategyOpenedAt, strategyDealtAt, strategyPickedAt;
        double? strategyConfirmedAt, strategyRerolledAt;
        float strategyDealLead;
        bool strategyConfirmToHistory;

        static void EnsureStrategyArt()
        {
            if (strategyPlates == null) strategyPlates = Resources.Load<Texture2D>("UI/Strategy/plates");
            if (strategyProps == null) strategyProps = Resources.Load<Texture2D>("UI/Strategy/props");
            if (strategyVeil == null) strategyVeil = Resources.Load<Texture2D>("UI/Strategy/veil");
        }

        bool DrawStrategyChoice()
        {
            var run = Campaign;
            var options = run?.HasBlockingChoice == true ? run.Choices : null;
            if (options == null || options.Count == 0 || options[0].Strategy == null)
            {
                displayedStrategyOptions = null;
                strategyDescriptions = null;
                strategyConfirmedAt = null;
                strategyRerolledAt = null;
                return false;
            }

            EnsureStrategyArt();

            string tier = options[0].Strategy.Tier;
            int column = tier switch { "银" => 1, "金" => 2, "彩" => 3, _ => 0 };
            bool preplan = column == 0;
            double now = Time.unscaledTimeAsDouble;
            if (!ReferenceEquals(options, displayedStrategyOptions))
            {
                bool opensPanel = displayedStrategyOptions == null || displayedStrategyOptions[0].Strategy.Tier != tier;
                if (opensPanel) strategyOpenedAt = now;
                strategyDealLead = opensPanel ? StrategyDealLead : 0;
                displayedStrategyOptions = options;
                selectedStrategyCard = -1;
                strategyDealtAt = now;
                strategyConfirmedAt = null;
                strategyRerolledAt = null;
                strategyDescriptions = new (GUIContent, GUIStyle)[options.Count];
                for (int i = 0; i < options.Count; i++)
                    strategyDescriptions[i] = FitStrategyDescription(options[i].Description, StrategyInk[column]);
            }
            string title = preplan ? "选择战略预案" : "选择战略战备";
            string node = preplan ? (run.IsPractice ? "开局" : "1-1 开局") : run.CurrentNode.Id + " 战前";
            string count = options.Count == 3 ? "三选一" : options.Count == 2 ? "二选一" : "一项可选";
            double openElapsed = now - strategyOpenedAt;
            double dealElapsed = now - strategyDealtAt;
            double confirmElapsed = strategyConfirmedAt.HasValue ? now - strategyConfirmedAt.Value : 0;
            double rerollElapsed = strategyRerolledAt.HasValue ? now - strategyRerolledAt.Value : 0;
            float dealEnd = strategyDealLead + (options.Count - 1) * StrategyDealGap + StrategyDealSeconds;
            bool acceptingInput = dealElapsed >= dealEnd && !strategyConfirmedAt.HasValue && !strategyRerolledAt.HasValue;
            DrawStrategyBackdrop(title, node, preplan ? count : tier + "档" + count, column, openElapsed, confirmElapsed);

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
                if (StrategyCardHit(rect, enabled && acceptingInput, out bool hovered, out bool pressed)) SelectStrategyCard(i, now);
                bool selected = i == selectedStrategyCard;
                double cardElapsed = dealElapsed - strategyDealLead - i * StrategyDealGap;
                float pick = selected ? StrategyProgress(now - strategyPickedAt, StrategyPickSeconds) : 1;
                DrawDealtStrategyCard(rect, options[i], strategyDescriptions[i], column, i, selected, hovered, pressed,
                    cardElapsed, pick, confirmElapsed, rerollElapsed);
            }

            var color = GUI.color;
            float actionsAlpha = StrategyProgress(dealElapsed - dealEnd + .14f, .14f)
                * (1 - StrategyProgress(confirmElapsed, .15f)) * (1 - StrategyProgress(rerollElapsed, .16f));
            GUI.color *= new Color(1, 1, 1, actionsAlpha);
            bool reroll = DrawStrategyAction(new Rect(468, 606, 160, 44),
                "免费重抽   " + (run.CanRerollChoice ? "1 / 1" : "0 / 1"), run.CanRerollChoice && acceptingInput);
            bool confirm = DrawStrategyAction(new Rect(638, 606, 174, 44), preplan ? "确认预案" : "确认战备",
                selectedStrategyCard >= 0 && acceptingInput, true, !preplan) || confirmFromKey;
            GUI.color = color;
            if (reroll)
            {
                strategyRerolledAt = now;
                return true;
            }
            if (rerollElapsed >= StrategyRerollSeconds && Event.current.type == EventType.Repaint)
            {
                run.RerollChoice();
                strategyRerolledAt = null;
            }
            if (confirm)
            {
                strategyConfirmedAt = now;
                strategyConfirmToHistory = FormationStrategyHistoryAvailable;
            }
            if (confirmElapsed >= StrategyConfirmSeconds && Event.current.type == EventType.Repaint)
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

        static bool StrategyCardHit(Rect rect, bool enable, out bool hovered, out bool pressed)
        {
            var input = Event.current;
            var hit = new Rect(rect.x, rect.y - 8, rect.width, rect.height + 8);
            int control = GUIUtility.GetControlID("StrategyChoiceCard".GetHashCode(), FocusType.Passive, rect);
            hovered = enable && hit.Contains(input.mousePosition);
            pressed = hovered && GUIUtility.hotControl == control;
            switch (input.GetTypeForControl(control))
            {
                case EventType.MouseDown when hovered && input.button == 0 && GUIUtility.hotControl == 0:
                    GUIUtility.hotControl = control;
                    input.Use();
                    break;
                case EventType.MouseUp when input.button == 0 && GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    pressed = false;
                    input.Use();
                    return hovered;
            }
            return false;
        }

        static float StrategyProgress(double elapsed, float duration) => Mathf.Clamp01((float)(elapsed / duration));
        static float StrategyEaseOut(float progress) => 1 - (1 - progress) * (1 - progress) * (1 - progress);

        void DrawStrategyBackdrop(string title, string node, string subtitle, int column, double elapsed, double confirm)
        {
            float veil = Mathf.Lerp(.45f, 1, StrategyEaseOut(StrategyProgress(elapsed, .28f)));
            float fade = StrategyEaseOut(StrategyProgress(confirm - .31f, .24f));
            float titleReveal = StrategyEaseOut(StrategyProgress(elapsed - .065f, .274f));
            var color = GUI.color;
            GUI.color *= new Color(1, 1, 1, veil * (1 - fade));
            var screen = new Rect(0, 0, 1280, 720);
            if (strategyVeil != null) GUI.DrawTexture(screen, strategyVeil);
            else Fill(screen, StrategyTint(new Color(.09f, .14f, .18f, .95f)));

            Fill(new Rect(0, 0, 1280, 58), StrategyTint(new Color32(21, 31, 38, 244)));
            Fill(new Rect(0, 57, 1280, 1), StrategyTint(new Color32(168, 201, 208, 36)));
            StrategyLabel(new Rect(27, 16, 16, 24), "◆", 13, StrategyGold, 700);
            StrategyLabel(new Rect(51, 16, 120, 24), "昨日圆车", 13, StrategyLight, 700);
            if (elapsed < .26f)
            {
                float scan = StrategyProgress(elapsed, .26f);
                float width = Mathf.Lerp(20, 1176, StrategyEaseOut(scan));
                float y = Mathf.Lerp(351, 326, scan);
                float flash = Mathf.Sin(scan * Mathf.PI);
                DrawStrategyStreak(new Rect(640 - width * .5f, y - 3, width, 8), StrategyGlints[column], flash * .18f);
                DrawStrategyStreak(new Rect(640 - width * .5f, y, width, 2), StrategyGlints[column], flash);
            }

            GUI.color = color * new Color(1, 1, 1, titleReveal * (1 - StrategyProgress(confirm, .25f)));
            float windowWidth = Mathf.Lerp(8, 590, titleReveal);
            float left = 640 - windowWidth * .5f;
            const float top = 94;
            GUI.BeginGroup(new Rect(left, top, windowWidth, 112));
            var rule = StrategyTint(new Color32(182, 203, 212, 96));
            Fill(new Rect(350 - left, 132 - top, 98, 1), rule);
            Fill(new Rect(832 - left, 132 - top, 98, 1), rule);
            Fill(new Rect(443 - left, 132 - top, 5, 6), StrategyTint(StrategyGold));
            Fill(new Rect(832 - left, 132 - top, 5, 6), StrategyTint(StrategyGold));
            StrategyLabel(new Rect(449 - left, 98 - top, 382, 67), title, 47, StrategyLight, 700, TextAnchor.MiddleCenter);
            StrategyLabel(new Rect(450 - left, 168 - top, 380, 25), "<color=#EFCB83><b>" + node + "</b></color>　/　" + subtitle,
                14, StrategyMuted, 400, TextAnchor.MiddleCenter, true);
            GUI.EndGroup();
            GUI.color = color;
        }

        void DrawDealtStrategyCard(Rect rect, FormationCampaignRun.ChoiceOption option,
            (GUIContent Content, GUIStyle Style) description, int column, int index, bool selected, bool hovered, bool pressed,
            double dealElapsed, float pick, double confirm, double reroll)
        {
            if (dealElapsed <= 0) return;
            float deal = StrategyProgress(dealElapsed, StrategyDealSeconds);
            float slide = Mathf.SmoothStep(0, 1, StrategyProgress(deal - .16f, .43f));
            float settle = StrategyEaseOut(StrategyProgress(deal - .59f, .41f));
            float fan = (rect.center.x - 640) / (StrategyCardWidth + StrategyCardGap);
            float scale = Mathf.Lerp(.61f, .97f, slide) + .03f * settle;
            float angle = Mathf.Lerp(fan * 5, fan * 2, slide) * (1 - settle);
            float alpha = StrategyEaseOut(StrategyProgress(deal, .23f));
            float facing = -Mathf.Cos(Mathf.PI * StrategyProgress(deal - .49f, .43f));
            rect.center = new Vector2(Mathf.Lerp(640 + fan * 10, rect.center.x + fan * 6, slide) - fan * 6 * settle,
                rect.center.y + Mathf.Lerp(49, -5, slide) * (1 - settle));

            float lift = selected ? -8 : hovered ? -6 : 0;
            float pickPulse = Mathf.Sin(pick * Mathf.PI);
            rect.y += pressed ? 3 : lift + (selected ? 10 * (1 - pick) - 5 * pickPulse : 0);
            scale *= pressed ? .98f : 1 + .019f * pickPulse;
            if (reroll > 0)
            {
                float gather = StrategyEaseOut(StrategyProgress(reroll - index * .024f, .225f));
                rect.center = Vector2.Lerp(rect.center, new Vector2(640 + fan * 4, 422), gather);
                angle += fan * 7 * gather;
                scale *= Mathf.Lerp(1, .61f, gather);
                alpha *= 1 - gather;
            }
            if (confirm > 0 && selected)
            {
                float windup = StrategyEaseOut(StrategyProgress(confirm, .198f));
                float fly = Mathf.SmoothStep(0, 1, StrategyProgress(confirm - .198f, StrategyConfirmSeconds - .198f));
                rect.y -= windup * 5;
                if (strategyConfirmToHistory)
                {
                    rect.center = Vector2.Lerp(rect.center, FormationStrategyHistoryAnchor.center, fly);
                    scale *= Mathf.Lerp(1 + .035f * windup, .23f, fly);
                    alpha *= Mathf.Lerp(1, .38f, fly);
                }
                else
                {
                    rect.y -= 26 * fly;
                    scale *= Mathf.Lerp(1 + .035f * windup, .86f, fly);
                    alpha *= 1 - fly;
                }
            }
            else if (confirm > 0)
            {
                float fold = StrategyEaseOut(StrategyProgress(confirm, .26f));
                float side = index < selectedStrategyCard ? -1 : 1;
                rect.position += new Vector2(side * 40, 36) * fold;
                angle += side * 7 * fold;
                scale *= Mathf.Lerp(1, .87f, fold);
                facing *= Mathf.Lerp(1, .54f, fold);
                alpha *= 1 - fold;
            }
            if (alpha <= .005f) return;

            var matrix = GUI.matrix;
            var color = GUI.color;
            if (angle != 0) GUIUtility.RotateAroundPivot(angle, rect.center);
            GUIUtility.ScaleAroundPivot(new Vector2(Mathf.Max(.025f, Mathf.Abs(facing)) * scale, scale), rect.center);
            GUI.color *= new Color(1, 1, 1, alpha);
            if (facing < 0) DrawStrategyCardBack(rect, StrategyGlints[column]);
            else
            {
                if (hovered && !selected)
                    Fill(new Rect(rect.x, rect.y - 1, rect.width - 21, 2), StrategyTint(new Color(.76f, .90f, .91f, .47f)));
                DrawStrategyCard(rect, option, description, column, index, selected);

                float shine = confirm > 0 && selected ? StrategyProgress(confirm, .26f)
                    : pick < 1 ? pick : StrategyProgress(dealElapsed - .504f, .26f);
                float highlight = Mathf.Sin(shine * Mathf.PI);
                if (highlight > .01f)
                {
                    float left = Mathf.Lerp(rect.x + 5, rect.xMax - 116, shine);
                    DrawStrategyStreak(new Rect(left, rect.y + 5, 91, rect.height - 22),
                        selected ? StrategySelection : StrategyGlints[column], highlight * (selected ? .30f : .22f));
                }
            }
            GUI.color = color;
            GUI.matrix = matrix;

            if (selected && strategyConfirmToHistory && confirm > .44f)
            {
                var anchor = FormationStrategyHistoryAnchor;
                float seam = StrategyProgress(confirm - .44f, .095f);
                DrawStrategyStreak(new Rect(anchor.x - 12, anchor.center.y - 1, anchor.width + 24, 2), StrategySelection, seam * .76f);
            }
        }

        static void DrawStrategyCardBack(Rect rect, Color accent)
        {
            if (strategyCardBack == null)
            {
                const int width = 170, height = 181, corner = 10;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    int edge = Mathf.Min(y, height - 1 - y);
                    int last = width - 1 - Mathf.Max(0, corner - edge);
                    for (int x = 0; x <= last; x++)
                    {
                        bool rim = edge == 0 || x == 0 || x == last;
                        bool grid = x % 9 == 0 || y % 9 == 0;
                        int shade = 24 + (width - x + y) / 16 + (grid ? 7 : 0);
                        pixels[y * width + x] = rim ? new Color32(111, 131, 137, 255)
                            : new Color32((byte)shade, (byte)(shade + 10), (byte)(shade + 15), 255);
                    }
                }
                strategyCardBack = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                strategyCardBack.SetPixels32(pixels);
                strategyCardBack.Apply(false, true);
            }
            GUI.DrawTexture(rect, strategyCardBack);
            ApprovedUi.Border(new Rect(rect.x + 16, rect.y + 16, rect.width - 35, rect.height - 32),
                StrategyTint(new Color(accent.r, accent.g, accent.b, .32f)));
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45, rect.center);
            ApprovedUi.Border(new Rect(rect.center.x - 30, rect.center.y - 30, 60, 60), StrategyTint(accent));
            Fill(new Rect(rect.center.x - 10, rect.center.y - 10, 20, 20), StrategyTint(accent));
            GUI.matrix = matrix;
        }

        static void DrawStrategyStreak(Rect rect, Color tint, float opacity)
        {
            if (strategyLightStreak == null)
            {
                const int width = 64;
                var pixels = new Color32[width];
                for (int i = 0; i < width; i++)
                {
                    float strength = Mathf.Sin(i / (float)(width - 1) * Mathf.PI);
                    pixels[i] = new Color32(255, 255, 255, (byte)(255 * strength * strength));
                }
                strategyLightStreak = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                strategyLightStreak.SetPixels32(pixels);
                strategyLightStreak.Apply(false, true);
            }
            var previous = GUI.color;
            tint.a *= previous.a * opacity;
            GUI.color = tint;
            GUI.DrawTexture(rect, strategyLightStreak);
            GUI.color = previous;
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

        static bool DrawStrategyAction(Rect rect, string text, bool available, bool confirm = false, bool strategy = false)
        {
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && available;
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color accent = strategy ? StrategySelection : StrategyGold;
            Color accentHover = strategy ? new Color32(210, 255, 240, 255) : new Color32(249, 220, 161, 255);
            Color face = confirm && available ? (hover ? accentHover : accent)
                : (Color)(hover ? new Color32(40, 60, 73, 255) : new Color32(27, 41, 51, 255));
            Fill(rect, StrategyTint(face));
            ApprovedUi.Border(rect, StrategyTint(confirm && available ? accent : (Color)new Color32(108, 129, 139, 184)));
            StrategyLabel(rect, text, 14, !available ? StrategyMuted : confirm ? StrategyInk[2] : StrategyLight,
                650, TextAnchor.MiddleCenter);
            bool click = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = enabled;
            return click;
        }
    }
}
