using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum CampaignJourneyStageType { Supply, Battle, Boss }

    [Serializable]
    public sealed class CampaignJourneyNumber
    {
        public string key;
        public float value;
    }

    [Serializable]
    public sealed class CampaignJourneyGuardTuning
    {
        public string id;
        public int tier;
        public float hpFactor = 1;
        public CampaignJourneyNumber[] parameters = Array.Empty<CampaignJourneyNumber>();
        public float Value(string key, float legacy)
        {
            var parameter = parameters?.FirstOrDefault(p => p.key == key);
            return parameter == null ? legacy : parameter.value;
        }
    }

    [Serializable]
    public sealed class CampaignJourneyStage
    {
        public int stage;
        public string title, tip;
        public ApprovedMapDefinition map;
        public DefenderTrialDefinition encounter;
        public CampaignJourneyStageType type;
        public int dp, defense, baseGold, naturalXP;
        public int[] guardTiers = Array.Empty<int>();
        public float[] guardHPFactors = Array.Empty<float>();
        public CampaignJourneyGuardTuning[] guardTuning = Array.Empty<CampaignJourneyGuardTuning>();
        public bool IsSupply => type == CampaignJourneyStageType.Supply;
        public DefenderPlacement[] CopyDefenders() => encounter == null ? Array.Empty<DefenderPlacement>() :
            Array.ConvertAll(encounter.defenders, d => JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(d)));
        public float GuardValue(string id, string key, float legacy) => guardTuning?.FirstOrDefault(g => g.id == id)?.Value(key, legacy) ?? legacy;
    }

    [CreateAssetMenu(menuName = "Yesterday Convoy/Campaign Journey V2")]
    public sealed class CampaignJourneyDefinition : ScriptableObject
    {
        public const int StageCount = 12;
        public CampaignJourneyStage[] stages = Array.Empty<CampaignJourneyStage>();
        public CampaignJourneyStage Stage(int oneBased)
        {
            if (oneBased < 1 || oneBased > StageCount) throw new ArgumentOutOfRangeException(nameof(oneBased));
            var entry = stages?.FirstOrDefault(s => s.stage == oneBased);
            if (entry == null) throw new InvalidOperationException("Campaign journey stage missing: " + oneBased);
            return entry;
        }
        public void Validate()
        {
            if (stages == null || stages.Length != StageCount || stages.Any(s => s == null) || stages.Select(s => s.stage).Distinct().Count() != StageCount)
                throw new InvalidOperationException("Exactly twelve distinct campaign stages required.");
            int[] supplies = { 1, 4, 8, 11 }, bosses = { 5, 9, 12 };
            int[] maps = { 7, 1, 2, 3, 4, 6, 5, 8 }; int mapIndex = 0;
            foreach (int number in Enumerable.Range(1, StageCount))
            {
                var entry = Stage(number);
                if (string.IsNullOrWhiteSpace(entry.title) || entry.baseGold < 0 || entry.naturalXP < 0)
                    throw new InvalidOperationException("Incomplete journey stage: " + number);
                if (entry.IsSupply)
                {
                    if (!supplies.Contains(number) || entry.dp != 0 || entry.defense != 0 || entry.encounter != null || entry.map != null || entry.guardTiers.Length != 0)
                        throw new InvalidOperationException("Supply must be a noncombat receipt: " + number);
                    continue;
                }
                if (supplies.Contains(number) || entry.type != (bosses.Contains(number) ? CampaignJourneyStageType.Boss : CampaignJourneyStageType.Battle) ||
                    entry.map == null || entry.map.index != maps[mapIndex++] || entry.encounter == null || entry.encounter == entry.map.encounter ||
                    entry.encounter.mapId != entry.map.id || entry.dp <= 0 || entry.defense <= 0 || entry.encounter.attackWaves == null)
                    throw new InvalidOperationException("Invalid independent campaign encounter: " + number);
                _ = entry.encounter.Snapshot(); _ = entry.encounter.LaneSnapshot();
                var plan=entry.encounter.WaveSnapshot();
                if(plan.Waves.Sum(w=>w.DP)!=entry.dp || plan.Waves.Any(w=>w.DP<=0 || !entry.encounter.lanes.Any(l=>l.id==w.LaneId)))
                    throw new InvalidOperationException("Campaign wave budget/lane drift: " + number);
                int count = entry.encounter.defenders.Length;
                if (entry.guardTiers.Length != count || entry.guardHPFactors.Length != count || entry.guardTuning.Length != count ||
                    entry.guardTiers.Any(t => t < 1 || t > 3)) throw new InvalidOperationException("Missing defender tier provenance: " + number);
            }
            if (mapIndex != 8) throw new InvalidOperationException("Eight campaign battles required.");
        }
    }

    public static class CampaignJourneyCatalog
    {
        public static CampaignJourneyDefinition Current => Resources.Load<CampaignJourneyDefinition>("CampaignV2/Journey");
        public static CampaignJourneyStage Stage(int number)
        {
            var journey = Current;
            if (journey == null) throw new InvalidOperationException("Persistent CampaignV2/Journey asset missing.");
            return journey.Stage(number);
        }
    }
}
