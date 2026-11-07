using System.Collections.Generic;
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
        static readonly Color InactiveColor = new Color(.48f, .53f, .58f);
        static readonly Color BronzeColor = new Color(.84f, .57f, .34f);
        static readonly Color SilverColor = new Color(.83f, .89f, .95f);
        static readonly Color GoldColor = new Color(1f, .82f, .43f);
        static readonly Color[] PrismaticColors =
        {
            new Color(1f, .79f, .88f),
            new Color(1f, .93f, .69f),
            new Color(.73f, .94f, .98f),
            new Color(.78f, .82f, 1f),
            new Color(.92f, .77f, .98f)
        };

        // Tier is the activated threshold from SynergySummary, never the raw member count.
        public static SynergyVisualGrade GradeFor(string name, int tier)
        {
            switch (name)
            {
                case "整合运动":
                case "感染生物":
                case "萨卡兹":
                    return tier >= 6 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "雪怪小队":
                    return tier >= 5 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "游击队":
                    return tier >= 8 ? SynergyVisualGrade.Prismatic :
                        tier >= 6 ? SynergyVisualGrade.Gold :
                        tier >= 4 ? SynergyVisualGrade.Silver :
                        tier >= 2 ? SynergyVisualGrade.Bronze : SynergyVisualGrade.Inactive;
                case "泥岩小队":
                    return tier >= 5 ? SynergyVisualGrade.Gold :
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
                    float position = Mathf.Clamp01(vertical) * (PrismaticColors.Length - 1);
                    int index = Mathf.Min(Mathf.FloorToInt(position), PrismaticColors.Length - 2);
                    return Color.Lerp(PrismaticColors[index], PrismaticColors[index + 1], position - index);
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
