using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // Gameplay formations partition the existing Reunion roster; other tags remain independent.
    public static class ReunionRules
    {
        public static int[] Thresholds => new[] { 2, 4, 6, 8 };

        public static int FormationOf(string id)
        {
            if (!CampaignBalance.Reunion(id)) return 0;
            if (CampaignBalance.Snow(id)) return 2;
            if (CampaignBalance.Guerrilla(id)) return 3;
            if (CampaignBalance.Tags(id).Contains("牧群")) return 4;
            return 1;
        }

        public static string FormationName(int formation)
        {
            switch (formation)
            {
                case 1: return "整合本队";
                case 2: return "雪怪小队";
                case 3: return "游击队";
                case 4: return "牧群";
                default: return "无整合编制";
            }
        }

        public static int FormationCount(IEnumerable<string> ids) => ids.Select(FormationOf).Where(f => f > 0).Distinct().Count();

        public static int TierFor(IEnumerable<string> ids)
        {
            var members = ids.Where(CampaignBalance.Reunion).Distinct(StringComparer.Ordinal).ToArray();
            return TierForCount(members.Length, FormationCount(members));
        }

        public static int TierForCount(int kinds, int formations) => kinds >= 8 && formations >= 3 ? 8 :
            kinds >= 6 && formations >= 2 ? 6 : kinds >= 4 ? 4 : kinds >= 2 ? 2 : 0;
        public static int RequiredFormations(int tier) => tier >= 8 ? 3 : tier >= 6 ? 2 : tier >= 2 ? 1 : 0;
        public static int KindsPerMuster(int tier) => tier == 2 ? 2 : tier >= 4 ? 3 : 0;
        public static int MusterLimit(int tier) => tier >= 8 ? 3 : tier >= 4 ? 2 : tier == 2 ? 1 : 0;
        public static int SupportsPerMuster(int tier) => tier >= 6 ? 2 : tier >= 2 ? 1 : 0;
        public static float StarScale(float averageStar) => 1 + .5f * (averageStar - 1);

        public readonly struct SupportStats
        {
            public readonly float Health, Attack, Armor, Range;
            public SupportStats(float health, float attack, float armor, float range)
            { Health = health; Attack = attack; Armor = armor; Range = range; }
        }

        public static SupportStats ForSupport(int tier, bool ranged, float averageStar = 1)
        {
            if (averageStar < 1 || averageStar > 3 || float.IsNaN(averageStar))
                throw new ArgumentOutOfRangeException(nameof(averageStar));
            float health, attack, armor;
            switch (tier)
            {
                case 2: health = 80; attack = 14; armor = 0; break;
                case 4: health = ranged ? 90 : 120; attack = ranged ? 24 : 22; armor = ranged ? 0 : 2; break;
                case 6: health = ranged ? 110 : 160; attack = ranged ? 30 : 28; armor = ranged ? 1 : 4; break;
                case 8: health = ranged ? 140 : 200; attack = ranged ? 38 : 36; armor = ranged ? 2 : 6; break;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
            float scale = StarScale(averageStar);
            return new SupportStats(Mathf.Floor(health * scale + .5f), Mathf.Floor(attack * scale + .5f), armor, ranged ? 2.7f : .75f);
        }
    }
}
