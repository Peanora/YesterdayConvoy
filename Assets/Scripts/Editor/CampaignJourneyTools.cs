using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Creates only formal campaign data; the eight authored scenes and sandbox assets are read-only.</summary>
    public static class CampaignJourneyTools
    {
        const string Sources = "Assets/Data/CampaignV2";
        const string Output = "Assets/Data/Resources/CampaignV2";
        public const string JourneyPath = Output + "/Journey.asset";
        [Serializable] sealed class Source { public StageSource[] stages; }
        [Serializable] sealed class StageSource
        {
            public int stage, mapIndex, dp, defense, baseGold, naturalXP;
            public string title, tip, type;
            public GuardSource[] guards;
            public AttackWaveSettings[] waves = Array.Empty<AttackWaveSettings>();
            public float finalAdvanceSeconds;
        }
        [Serializable] sealed class Cell { public int x, y; }
        [Serializable] sealed class GuardSource
        {
            public string id; public int tier; public float hpFactor;
            public float attackFactor = 1, armorBonus, rangeFactor = 1;
            public Cell cell;
        }
        [Serializable] sealed class DefenderSource { public Unit[] units; }
        [Serializable] sealed class Unit { public string id, name; public Panel[] stages; public Skill[] skills; }
        [Serializable] sealed class Panel
        {
            public int tier, blocks;
            public float hp, attack, armor, resistance, interval, range, windup, actionDuration, splashRadius, healingRange;
            public float rangedAttackMultiplier, criticalChance, criticalMultiplier, extraHealChance, blockRadius;
            public string damageType;
            public bool highGround, noOrdinaryAttack;
        }
        [Serializable] sealed class Skill { public string id; public Parameter[] parameters; }
        [Serializable] sealed class Parameter
        {
            public string key;
            public float tier1, tier2, tier3;
            public float At(int tier) => tier == 1 ? tier1 : tier == 2 ? tier2 : tier3;
        }
        sealed class AssetRename
        {
            public UnityEngine.Object asset;
            public string temporary, destination;
        }

        [MenuItem("Yesterday Convoy/Campaign V2/Author Formal Journey Data")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring campaign data.");
            var flow = Read<Source>("journey-authoring.json"); var defenders = Read<DefenderSource>("defenders.json");
            EnsureFolder(Output);
            var journey = AssetDatabase.LoadAssetAtPath<CampaignJourneyDefinition>(JourneyPath);
            if (journey == null) { journey = ScriptableObject.CreateInstance<CampaignJourneyDefinition>(); journey.name = "Journey"; AssetDatabase.CreateAsset(journey, JourneyPath); }
            PreserveEncounterGuids(journey, flow);
            journey.stages = flow.stages.OrderBy(s => s.stage).Select(stage => BuildStage(stage, defenders)).ToArray();
            EditorUtility.SetDirty(journey); AssetDatabase.SaveAssets(); Validate();
            Debug.Log("CAMPAIGN_JOURNEY_AUTHORED: 12 stages, 4 noncombat supply receipts, 8 independent wave encounters, 15 strengthened v2 defenders; existing map scenes and sandbox encounters untouched.");
        }

        static void PreserveEncounterGuids(CampaignJourneyDefinition journey, Source flow)
        {
            var existing = journey.stages?.Where(s => s != null && !s.IsSupply).ToArray() ?? Array.Empty<CampaignJourneyStage>();
            if (existing.Length == 0) return;
            var expected = flow.stages.Where(s => s.type != nameof(CampaignJourneyStageType.Supply)).ToArray();
            if (existing.Length != expected.Length || existing.Any(s => s.map == null || s.encounter == null || s.encounter.attackWaves == null) ||
                existing.Select(s => s.map.index).Distinct().Count() != existing.Length || expected.Any(s => existing.All(e => e.map.index != s.mapIndex)))
                throw new InvalidOperationException("Cannot preserve journey asset GUIDs: existing encounters do not match the authored map set.");
            var renames = new List<AssetRename>();
            foreach (var stage in expected)
            {
                var previous = existing.Single(s => s.map.index == stage.mapIndex);
                string destination = Output + "/Stage" + stage.stage.ToString("00");
                string temporary = Output + "/JourneyReindexMap" + stage.mapIndex.ToString("00");
                renames.Add(new AssetRename { asset = previous.encounter, temporary = temporary + "Battle.asset", destination = destination + "Battle.asset" });
                renames.Add(new AssetRename { asset = previous.encounter.attackWaves, temporary = temporary + "Waves.asset", destination = destination + "Waves.asset" });
            }
            var sources = new HashSet<string>(renames.Select(r => AssetDatabase.GetAssetPath(r.asset)), StringComparer.Ordinal);
            if (sources.Count != renames.Count || sources.Any(path => !path.StartsWith(Output + "/", StringComparison.Ordinal)))
                throw new InvalidOperationException("Cannot preserve journey asset GUIDs: encounters must be distinct assets inside the formal output folder.");
            if (renames.All(r => AssetDatabase.GetAssetPath(r.asset) == r.destination)) return;
            foreach (var rename in renames)
            {
                var temporary = AssetDatabase.LoadMainAssetAtPath(rename.temporary);
                var destination = AssetDatabase.LoadMainAssetAtPath(rename.destination);
                if (temporary != null && temporary != rename.asset || destination != null && !sources.Contains(AssetDatabase.GetAssetPath(destination)))
                    throw new InvalidOperationException("Cannot reindex journey assets without overwriting an unrelated asset: " + rename.destination);
            }
            // All old destinations are vacated first, so neighboring stage numbers
            // never overwrite each other. Object references and GUIDs survive both moves.
            foreach (var rename in renames) Move(rename.asset, rename.temporary);
            foreach (var rename in renames) Move(rename.asset, rename.destination);
        }

        static void Move(UnityEngine.Object asset, string destination)
        {
            string source = AssetDatabase.GetAssetPath(asset);
            if (source == destination) return;
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException("Journey asset reindex failed (" + source + " -> " + destination + "): " + error);
        }

        static CampaignJourneyStage BuildStage(StageSource source, DefenderSource roster)
        {
            var stage = new CampaignJourneyStage { stage = source.stage, title = source.title, tip = source.tip,
                type = (CampaignJourneyStageType)Enum.Parse(typeof(CampaignJourneyStageType), source.type),
                dp = source.dp, defense = source.defense, baseGold = source.baseGold, naturalXP = source.naturalXP };
            if (stage.IsSupply) return stage;
            var map = ApprovedMapCatalog.All.Single(m => m.index == source.mapIndex); map.Validate(); stage.map = map;
            string path = Output + "/Stage" + source.stage.ToString("00") + "Battle.asset";
            var encounter = AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(path);
            if (encounter == null) { encounter = ScriptableObject.CreateInstance<DefenderTrialDefinition>(); encounter.name = "Stage" + source.stage.ToString("00") + "Battle"; AssetDatabase.CreateAsset(encounter, path); }
            encounter.name = "Stage" + source.stage.ToString("00") + "Battle";
            encounter.battle = map.encounter.battle; encounter.route = (Vector3[])map.encounter.route.Clone();
            encounter.mapId = map.id; encounter.mapTitle = stage.title; encounter.autumnRuins = map.encounter.autumnRuins;
            encounter.lanes = Array.ConvertAll(map.encounter.lanes, l => new TacticalLane { id = l.id, exitId = l.exitId, points = (Vector3[])l.points.Clone() });
            encounter.surfaces = map.encounter.surfaces == null ? Array.Empty<Material>() : (Material[])map.encounter.surfaces.Clone();
            encounter.raised = map.encounter.raised == null ? Array.Empty<Vector2Int>() : (Vector2Int[])map.encounter.raised.Clone();
            encounter.floorTexture = map.encounter.floorTexture; encounter.foliageTexture = map.encounter.foliageTexture;
            encounter.rosterCards = Array.Empty<ManualBattleDefinition>(); encounter.defenderTestId = null; encounter.terrainOnly = false;
            if (source.waves == null || source.waves.Length == 0 || source.dp != source.waves.Sum(w => w.dp))
                throw new InvalidOperationException("Formal battle DP must equal its authored wave budgets: " + source.stage);
            string wavePath = Output + "/Stage" + source.stage.ToString("00") + "Waves.asset";
            var scenario = AssetDatabase.LoadAssetAtPath<AttackWaveScenario>(wavePath);
            if (scenario == null) { scenario = ScriptableObject.CreateInstance<AttackWaveScenario>(); scenario.name = "Stage" + source.stage.ToString("00") + "Waves"; AssetDatabase.CreateAsset(scenario, wavePath); }
            scenario.name = "Stage" + source.stage.ToString("00") + "Waves";
            scenario.waves = Array.ConvertAll(source.waves, w => new AttackWaveSettings { laneId = w.laneId, dp = w.dp, deploymentSeconds = w.deploymentSeconds });
            scenario.finalAdvanceSeconds = source.finalAdvanceSeconds;
            _ = scenario.Snapshot(); EditorUtility.SetDirty(scenario); encounter.attackWaves = scenario;
            encounter.defenders = source.guards.Select(g => BuildGuard(map, g, roster)).ToArray();
            stage.encounter = encounter; stage.guardTiers = source.guards.Select(g => g.tier).ToArray(); stage.guardHPFactors = source.guards.Select(g => g.hpFactor).ToArray();
            stage.guardTuning = source.guards.Select(g => {
                var unit = roster.units.Single(u => u.id == g.id);
                return new CampaignJourneyGuardTuning { id = g.id, tier = g.tier, hpFactor = g.hpFactor,
                    parameters = unit.skills.SelectMany(s => s.parameters).Select(p => new CampaignJourneyNumber { key = p.key, value = p.At(g.tier) }).ToArray() };
            }).ToArray();
            EditorUtility.SetDirty(encounter); return stage;
        }

        static DefenderPlacement BuildGuard(ApprovedMapDefinition map, GuardSource source, DefenderSource roster)
        {
            var data = roster.units.Single(u => u.id == source.id); var panel = data.stages.Single(p => p.tier == source.tier);
            if (!(source.hpFactor > 0) || !(source.attackFactor > 0) || !(source.rangeFactor > 0) ||
                float.IsInfinity(source.hpFactor) || float.IsInfinity(source.attackFactor) || float.IsInfinity(source.rangeFactor) ||
                float.IsNaN(source.armorBonus) || float.IsInfinity(source.armorBonus) || panel.armor + source.armorBonus < 0)
                throw new InvalidOperationException("Invalid scoped formal defender multiplier: " + source.id);
            if (panel.resistance != 0 || Mathf.Abs(panel.blockRadius - .65f) > .000001f)
                throw new InvalidOperationException("This defender requires a new explicit placement field; do not silently drop resistance/block radius: " + source.id);
            var original = Resources.Load<DefenderTrialDefinition>("DefenderTrials/" + source.id);
            if (original == null) throw new InvalidOperationException("Saved defender art/action configuration missing: " + source.id);
            var template = original.defenders.Single(d => d.id == source.id);
            var guard = JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(template));
            guard.id = source.id; guard.displayName = data.name; guard.artId = template.artId;
            guard.position = map.Cell(source.cell.x, source.cell.y, panel.highGround ? map.platformHeight : 0);
            if (panel.highGround != (map.Tile(source.cell.x, source.cell.y) == 'H') || !panel.highGround && !map.Walkable(source.cell.x, source.cell.y))
                throw new InvalidOperationException("Formal campaign placement does not match saved terrain: " + map.id + "/" + source.id);
            guard.health = Mathf.Floor(panel.hp * source.hpFactor + .5f); guard.attack = panel.attack * source.attackFactor; guard.armor = panel.armor + source.armorBonus;
            guard.range = panel.range * source.rangeFactor; guard.interval = panel.interval; guard.blocks = panel.blocks; guard.windup = panel.windup; guard.actionDuration = panel.actionDuration;
            guard.damageType = (CombatDamageType)Enum.Parse(typeof(CombatDamageType), panel.damageType); guard.splashRadius = panel.splashRadius; guard.healingRange = panel.healingRange;
            guard.noOrdinaryAttack = panel.noOrdinaryAttack; guard.rangedAttackMultiplier = panel.rangedAttackMultiplier;
            guard.criticalChance = panel.criticalChance; guard.criticalMultiplier = panel.criticalMultiplier; guard.extraHealChance = panel.extraHealChance; guard.reserve = false;
            if (data.skills.Length != 1) throw new InvalidOperationException("Map guard needs an explicit multi-skill adapter: " + source.id);
            var skill = data.skills[0]; guard.skill = new DefenderSkillDefinition { kind = (DefenderSkillKind)Enum.Parse(typeof(DefenderSkillKind), skill.id) };
            foreach (var parameter in skill.parameters)
            {
                // Generic fields are direct snapshots. Additional original-mechanic constants are
                // retained in stage.guardTuning and read only by the scoped formal-campaign adapter.
                var field = typeof(DefenderSkillDefinition).GetField(parameter.key, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) continue;
                float value = parameter.At(source.tier);
                if (field.FieldType == typeof(float)) field.SetValue(guard.skill, value);
                else if (field.FieldType == typeof(int)) field.SetValue(guard.skill, Mathf.RoundToInt(value));
                else throw new InvalidOperationException("Unsupported generic defender field type: " + parameter.key);
            }
            _ = guard.Snapshot(); return guard;
        }

        [MenuItem("Yesterday Convoy/Campaign V2/Validate Formal Journey Data")]
        public static void Validate()
        {
            var flow = Read<Source>("journey-authoring.json"); var roster = Read<DefenderSource>("defenders.json");
            var journey = AssetDatabase.LoadAssetAtPath<CampaignJourneyDefinition>(JourneyPath);
            if (journey == null) throw new InvalidOperationException("Persistent journey missing."); journey.Validate(); int guardCount = 0;
            foreach (var expected in flow.stages)
            {
                var actual = journey.Stage(expected.stage);
                if (actual.title != expected.title || actual.tip != expected.tip || actual.dp != expected.dp || actual.defense != expected.defense || actual.baseGold != expected.baseGold || actual.naturalXP != expected.naturalXP)
                    throw new InvalidOperationException("Journey progression drift: " + expected.stage);
                if (actual.IsSupply) continue;
                if (!AssetDatabase.Contains(actual.map) || !AssetDatabase.Contains(actual.encounter) || actual.map.index != expected.mapIndex)
                    throw new InvalidOperationException("Journey must reference persistent existing maps and independent encounters.");
                if (actual.encounter.defenders.Length != expected.guards.Length) throw new InvalidOperationException("Formal defender count drift.");
                if (actual.encounter.attackWaves == null || !AssetDatabase.Contains(actual.encounter.attackWaves) ||
                    actual.encounter.attackWaves == actual.map.encounter.attackWaves)
                    throw new InvalidOperationException("Formal battle requires an independent persistent wave scenario: " + expected.stage);
                var waves = actual.encounter.attackWaves;
                if (waves.waves.Length != expected.waves.Length || waves.finalAdvanceSeconds != expected.finalAdvanceSeconds ||
                    waves.waves.Where((w, i) => w.laneId != expected.waves[i].laneId || w.dp != expected.waves[i].dp || w.deploymentSeconds != expected.waves[i].deploymentSeconds).Any())
                    throw new InvalidOperationException("Formal wave plan drift: " + expected.stage);
                for (int i = 0; i < expected.guards.Length; i++)
                {
                    var source = expected.guards[i]; var guard = actual.encounter.defenders[i]; var target = BuildGuard(actual.map, source, roster);
                    if (JsonUtility.ToJson(guard) != JsonUtility.ToJson(target)) throw new InvalidOperationException("Adopted-v2 defender snapshot drift: stage " + expected.stage + "/" + source.id);
                    if (actual.guardTiers[i] != source.tier || actual.guardHPFactors[i] != source.hpFactor) throw new InvalidOperationException("Defender stage provenance drift.");
                    var parameters = roster.units.Single(u => u.id == source.id).skills.Single().parameters;
                    foreach (var p in parameters) if (Mathf.Abs(actual.GuardValue(source.id, p.key, float.NaN) - p.At(source.tier)) > .000001f || float.IsNaN(actual.GuardValue(source.id, p.key, float.NaN)))
                        throw new InvalidOperationException("Additional defender mechanic parameter missing: " + source.id + "/" + p.key);
                    guardCount++;
                }
                for (int lane = 0; lane < actual.map.encounter.lanes.Length; lane++)
                {
                    var a = actual.encounter.lanes[lane]; var b = actual.map.encounter.lanes[lane];
                    if (a.id != b.id || a.exitId != b.exitId || !a.points.SequenceEqual(b.points)) throw new InvalidOperationException("Journey changed the approved fixed map route.");
                }
            }
            if (guardCount != 15) throw new InvalidOperationException("Fifteen formal defender placements required.");
            Debug.Log("CAMPAIGN_JOURNEY_CONFIGURATION_PASS: 12 stages / 8 independent wave plans / 15 strengthened v2 defenders and preserved skill snapshots / preserved sandbox data / no scene writes.");
        }

        public static void BuildWindows() { Validate(); BaselineTools.BuildWindows(); }
        static T Read<T>(string file)
        {
            string path = Sources + "/" + file; if (!File.Exists(path)) throw new InvalidOperationException("Campaign authoring source missing: " + path);
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return; string parent = Path.GetDirectoryName(folder).Replace('\\', '/'); EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder)))) throw new InvalidOperationException("Cannot create campaign data folder.");
        }
    }
}
