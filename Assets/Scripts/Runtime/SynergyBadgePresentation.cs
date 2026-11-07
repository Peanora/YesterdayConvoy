using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum SynergyVisualGrade
    {
        Inactive,
        Bronze,
        Silver,
        Gold,
        Prismatic
    }

    /// <summary>Read-only icon and grade styling for the nine regular synergies.</summary>
    public static class SynergyBadgePresentation
    {
        const int PrismaticBands = 16;
        static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> frames = new Dictionary<string, Texture2D>();
        static Texture2D counter;
        static bool counterLoaded;
        static readonly Color InactiveColor = new Color(.48f, .53f, .58f);
        static readonly Color BronzeColor = new Color(.84f, .57f, .34f);
        static readonly Color SilverColor = new Color(.83f, .89f, .95f);
        static readonly Color GoldColor = new Color(1f, .82f, .43f);
        static readonly Color PastThresholdColor = new Color32(0xb7, 0xc3, 0xd2, 0xff);
        static readonly Color FutureThresholdColor = new Color32(0x64, 0x74, 0x8a, 0xff);
        static readonly Color ThresholdSlashColor = new Color32(0x50, 0x62, 0x7a, 0xff);
        static readonly Color[] PrismaticColors =
        {
            new Color32(0xb0, 0xf1, 0xdd, 0xff),
            new Color32(0x6f, 0xcb, 0xb7, 0xff),
            new Color32(0x72, 0xba, 0xc2, 0xff),
            new Color32(0x95, 0xb4, 0xce, 0xff),
            new Color32(0xb8, 0xa4, 0xcf, 0xff),
            new Color32(0xd3, 0xb8, 0xd1, 0xff),
            new Color32(0xed, 0xe3, 0xce, 0xff)
        };
        static readonly float[] PrismaticStops = { 0f, .22f, .45f, .64f, .83f, .94f, 1f };

        // Tier is the activated threshold from SynergySummary, never the raw member count.
        public static SynergyVisualGrade GradeFor(string name, int tier)
        {
            switch (name)
            {
                case "整合运动":
                    return tier >= 8 ? SynergyVisualGrade.Prismatic :
                        tier >= 6 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "感染生物":
                    return tier >= 6 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "雪怪小队":
                    return tier >= 5 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "游击队":
                case "萨卡兹":
                    return tier >= 8 ? SynergyVisualGrade.Prismatic :
                        tier >= 6 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "泥岩小队":
                    return tier >= 6 ? SynergyVisualGrade.Prismatic :
                        tier >= 5 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "牧群":
                case "深池":
                case "海嗣":
                    return tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                default:
                    return SynergyVisualGrade.Inactive;
            }
        }

        static string IconId(string name)
        {
            switch (name)
            {
                case "整合运动": return "reunion";
                case "雪怪小队": return "snow";
                case "感染生物": return "infection";
                case "游击队": return "guerrilla";
                case "泥岩小队": return "mudrock";
                case "牧群": return "herd";
                case "深池": return "deep-pool";
                case "海嗣": return "marine";
                case "萨卡兹": return "sarkaz";
                default: return null;
            }
        }

        public static Texture2D IconFor(string name)
        {
            string id = IconId(name);
            if (id == null) return null;
            if (!icons.TryGetValue(id, out var icon))
            {
                icon = Resources.Load<Texture2D>("UI/Synergies/" + id);
                icons.Add(id, icon);
            }
            return icon;
        }

        public static Texture2D FrameFor(SynergyVisualGrade grade)
        {
            string id;
            switch (grade)
            {
                case SynergyVisualGrade.Bronze: id = "bronze"; break;
                case SynergyVisualGrade.Silver: id = "silver"; break;
                case SynergyVisualGrade.Gold: id = "gold"; break;
                case SynergyVisualGrade.Prismatic: id = "prismatic"; break;
                default: id = "inactive"; break;
            }
            if (!frames.TryGetValue(id, out var frame))
            {
                frame = Resources.Load<Texture2D>("UI/Synergies/Frames/plate-" + id);
                frames.Add(id, frame);
            }
            return frame;
        }

        public static Texture2D CounterFor()
        {
            if (!counterLoaded)
            {
                counter = Resources.Load<Texture2D>("UI/Synergies/Frames/count-dot");
                counterLoaded = true;
            }
            return counter;
        }

        static Rect Centered(Vector2 center, float size)
            => new Rect(center.x - size * .5f, center.y - size * .5f, size, size);

        public static void DrawBadge(Rect diamondBounds, string name, int tier, int count, Font fallbackFont)
        {
            if (diamondBounds.width <= 0 || diamondBounds.height <= 0) return;
            float scale = Mathf.Min(diamondBounds.width, diamondBounds.height) / 44f;
            Vector2 center = diamondBounds.center;
            var grade = GradeFor(name, tier);
            Color previous = GUI.color;
            try
            {
                // The authored PNG already contains the metal, rim and shadow. Drawing it at
                // 52 units preserves its four-unit transparent/shadow margin around the 44-unit plate.
                GUI.color = new Color(1, 1, 1, previous.a);
                var frame = FrameFor(grade);
                if (frame != null) GUI.DrawTexture(Centered(center, 52f * scale), frame, ScaleMode.StretchToFill, true);

                var icon = IconFor(name);
                if (icon != null)
                {
                    // Identity glyphs stay white; grade is represented only by the backplate.
                    GUI.color = new Color(1, 1, 1, previous.a * (grade == SynergyVisualGrade.Inactive ? .48f : 1f));
                    GUI.DrawTexture(Centered(center, 28f * scale), icon, ScaleMode.StretchToFill, true);
                }

                GUI.color = new Color(1, 1, 1, previous.a);
                Vector2 counterCenter = center + new Vector2(15.84f, 15.84f) * scale;
                var dot = CounterFor();
                if (dot != null) GUI.DrawTexture(Centered(counterCenter, 24.8f * scale), dot, ScaleMode.StretchToFill, true);
                GUI.Label(Centered(counterCenter, 16.8f * scale), count.ToString(CultureInfo.InvariantCulture),
                    ApprovedUi.Text(fallbackFont, Mathf.Max(1, Mathf.RoundToInt(13f * scale)),
                        TextAnchor.MiddleCenter, Color.white, 600));
            }
            finally
            {
                GUI.color = previous;
            }
        }

        public static void DrawThresholds(Rect bounds, int[] thresholds, int activeTier, Font fallbackFont)
        {
            if (bounds.width <= 0 || bounds.height <= 0 || thresholds == null || thresholds.Length == 0) return;
            float preferredWidth = 8f + thresholds.Length * 10f + (thresholds.Length - 1) * 6f;
            // Normal sidebar rows use the full 12px type. A narrower caller still keeps every
            // threshold by uniformly reducing the spacing and type, rather than dropping values.
            float scale = Mathf.Min(1f, bounds.width / preferredWidth);
            int fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * scale));
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(13f / 255f, 23f / 255f, 37f / 255f, previous.a * .8f);
                GUI.DrawTexture(new Rect(bounds.x, bounds.y, preferredWidth * scale, bounds.height), Texture2D.whiteTexture);
                GUI.color = new Color(1, 1, 1, previous.a);
                float x = bounds.x + 4f * scale;
                for (int i = 0; i < thresholds.Length; i++)
                {
                    int value = thresholds[i];
                    Color ink = value == activeTier ? Color.white : value < activeTier ? PastThresholdColor : FutureThresholdColor;
                    GUI.Label(new Rect(x, bounds.y, 10f * scale, bounds.height), value.ToString(CultureInfo.InvariantCulture),
                        ApprovedUi.Text(fallbackFont, fontSize, TextAnchor.MiddleCenter, ink, value == activeTier ? 700 : 500));
                    x += 10f * scale;
                    if (i == thresholds.Length - 1) continue;
                    GUI.Label(new Rect(x, bounds.y, 6f * scale, bounds.height), "/",
                        ApprovedUi.Text(fallbackFont, fontSize, TextAnchor.MiddleCenter, ThresholdSlashColor, 500));
                    x += 6f * scale;
                }
            }
            finally
            {
                GUI.color = previous;
            }
        }

        // vertical is measured from the top of the displayed icon or strip.
        // The prismatic ramp is static: it needs no animation clock or generated textures.
        public static Color ColorFor(SynergyVisualGrade grade, float vertical = .5f)
        {
            switch (grade)
            {
                case SynergyVisualGrade.Bronze: return BronzeColor;
                case SynergyVisualGrade.Silver: return SilverColor;
                case SynergyVisualGrade.Gold: return GoldColor;
                case SynergyVisualGrade.Prismatic:
                    float position = Mathf.Clamp01(vertical);
                    for (int index = 0; index < PrismaticStops.Length - 1; index++)
                        if (position <= PrismaticStops[index + 1])
                            return Color.Lerp(PrismaticColors[index], PrismaticColors[index + 1],
                                Mathf.InverseLerp(PrismaticStops[index], PrismaticStops[index + 1], position));
                    return PrismaticColors[PrismaticColors.Length - 1];
                default: return InactiveColor;
            }
        }

        public static void DrawIcon(Rect rect, string name, int tier)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            var icon = IconFor(name);
            if (icon == null) return;
            DrawTinted(rect, icon, GradeFor(name, tier), true);
        }

        public static void DrawGradeStrip(Rect rect, SynergyVisualGrade grade)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            DrawTinted(rect, Texture2D.whiteTexture, grade, false);
        }

        static void DrawTinted(Rect rect, Texture2D texture, SynergyVisualGrade grade, bool icon)
        {
            Color previous = GUI.color;
            try
            {
                int bands = grade == SynergyVisualGrade.Prismatic ? PrismaticBands : 1;
                for (int i = 0; i < bands; i++)
                {
                    float from = (float)i / bands, to = (float)(i + 1) / bands;
                    Color tint = ColorFor(grade, (from + to) * .5f);
                    GUI.color = new Color(previous.r * tint.r, previous.g * tint.g,
                        previous.b * tint.b, previous.a * tint.a);
                    var band = new Rect(rect.x, rect.y + rect.height * from,
                        rect.width, rect.height * (to - from));
                    // IMGUI rectangles start at the top; texture UVs start at the bottom.
                    // Only the corresponding source slice is drawn, preserving transparent cutouts.
                    if (icon)
                        GUI.DrawTextureWithTexCoords(band, texture, new Rect(0, 1 - to, 1, to - from), true);
                    else
                        GUI.DrawTexture(band, texture);
                }
            }
            finally
            {
                GUI.color = previous;
            }
        }
    }
}
