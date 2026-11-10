using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class CampaignJourneyConfigurationTests
    {
        [Test] public void FormalJourneyStartsAtProcurementAndHasFourSupplyReceiptsAndEightApprovedMapBattles()
        {
            var journey = CampaignJourneyCatalog.Current; Assert.That(journey, Is.Not.Null); Assert.DoesNotThrow(journey.Validate);
            Assert.That(journey.stages.Length, Is.EqualTo(CampaignJourneyDefinition.StageCount));
            Assert.That(journey.Stage(1).title, Is.EqualTo("集结采购"));
            Assert.That(journey.stages.Where(s => s.IsSupply).Select(s => s.stage), Is.EqualTo(new[] { 1, 4, 8, 11 }));
            Assert.That(journey.stages.Where(s => !s.IsSupply).Select(s => s.map.index), Is.EqualTo(new[] { 7, 1, 2, 3, 4, 6, 5, 8 }));
            Assert.That(journey.stages.Where(s => s.IsSupply).All(s => s.dp == 0 && s.defense == 0 && s.encounter == null), Is.True);
            Assert.That(journey.stages.Select(s => s.baseGold), Is.EqualTo(new[] { 8, 10, 10, 10, 12, 10, 10, 12, 14, 12, 14, 14 }));
            Assert.That(journey.stages.All(s => s.naturalXP == 4), Is.True);
        }

        [Test] public void SavedJourneyMatchesCurrentAuthoringWithoutRewritingSandboxAssets()
        {
            Assert.DoesNotThrow(CampaignJourneyTools.Validate);
        }

        [Test] public void BattlesKeepIndependentResourcesAndFinitePositiveCombatPanels()
        {
            var battles = CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply).ToArray();
            var assets = battles.SelectMany(s => new Object[] { s.encounter, s.encounter.attackWaves }).ToArray();
            Assert.That(assets.All(AssetDatabase.Contains), Is.True);
            var paths = assets.Select(AssetDatabase.GetAssetPath).ToArray();
            Assert.That(paths.Distinct().Count(), Is.EqualTo(paths.Length));
            var guids = paths.Select(path => AssetDatabase.AssetPathToGUID(path)).ToArray();
            Assert.That(guids.All(guid => !string.IsNullOrEmpty(guid)), Is.True);
            Assert.That(guids.Distinct().Count(), Is.EqualTo(guids.Length));
            foreach (var entry in battles)
            {
                Assert.That(entry.encounter, Is.Not.SameAs(entry.map.encounter));
                Assert.That(entry.encounter.attackWaves, Is.Not.Null);
                Assert.That(entry.dp, Is.EqualTo(entry.encounter.WaveSnapshot().Waves.Sum(w => w.DP)));
                Assert.That(entry.encounter.defenders.Length, Is.EqualTo(entry.guardTiers.Length));
                foreach (var guard in entry.encounter.defenders)
                {
                    Assert.That(guard.health > 0 && !float.IsNaN(guard.health) && !float.IsInfinity(guard.health), Is.True, guard.id);
                    Assert.That(guard.attack >= 0 && !float.IsNaN(guard.attack) && !float.IsInfinity(guard.attack), Is.True, guard.id);
                    Assert.That(guard.armor >= 0 && !float.IsNaN(guard.armor) && !float.IsInfinity(guard.armor), Is.True, guard.id);
                }
            }
        }

        [Test] public void FirstBossUsesIntermittentSelfRecoveryAndFinalSariaKeepsHerHealingMechanic()
        {
            var first = CampaignJourneyCatalog.Stage(5); var cardigan = first.encounter.defenders.First();
            Assert.That(cardigan.id, Is.EqualTo("D02"));
            Assert.That(cardigan.armor, Is.EqualTo(15));
            Assert.That(cardigan.skill.kind, Is.EqualTo(DefenderSkillKind.CardiganRecovery));
            Assert.That(first.GuardValue("D02", "healFraction", -1), Is.EqualTo(.3f));
            Assert.That(first.GuardValue("D02", "requiredSP", -1), Is.EqualTo(24));
            Assert.That(CampaignJourneyCatalog.Stage(12).encounter.defenders.Any(d => d.id == "D29" && d.skill.kind == DefenderSkillKind.SariaCalcification), Is.True);
        }

        [Test] public void DefenderCountGrowsGraduallyAndFirstReachesEightAtTheSecondBoss()
        {
            var battles = CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply).OrderBy(s => s.stage).ToArray();
            Assert.That(battles.First().encounter.defenders.Length, Is.EqualTo(1));
            Assert.That(battles.Where(s => s.stage < 9).All(s => s.encounter.defenders.Length < 8), Is.True);
            Assert.That(battles.First(s => s.encounter.defenders.Length >= 8).stage, Is.EqualTo(9));
            Assert.That(battles.Single(s => s.stage == 9).encounter.defenders.Length, Is.EqualTo(8));
            Assert.That(battles.Last().encounter.defenders.Length, Is.EqualTo(12));
            for (int i = 1; i < battles.Length; i++)
                Assert.That(battles[i].encounter.defenders.Length - battles[i - 1].encounter.defenders.Length, Is.InRange(1, 3),
                    "A later encounter adds a bounded number of defenders: stage " + battles[i].stage);
            Assert.That(battles.Select(s => s.encounter.attackWaves.waves.Length), Is.EqualTo(new[] { 1, 1, 2, 2, 2, 3, 3, 3 }));
            Assert.That(battles.Select(s => s.encounter.attackWaves).Distinct().Count(), Is.EqualTo(8));
            foreach (var stage in battles)
            {
                Assert.That(stage.encounter.WaveSnapshot().TotalDuration, Is.InRange(80, 210));
                Assert.That(stage.encounter.attackWaves.waves.All(w => stage.encounter.lanes.Any(l => l.id == w.laneId)), Is.True);
                Assert.That(stage.encounter.attackWaves, Is.Not.SameAs(stage.map.encounter.attackWaves));
                Assert.That(stage.encounter.defenders.Select(d => d.id).Distinct().Count(), Is.EqualTo(stage.encounter.defenders.Length));
                Assert.That(stage.encounter.defenders.Select(d => d.position).Distinct().Count(), Is.EqualTo(stage.encounter.defenders.Length));
                Assert.That(stage.guardTuning.Select(g => g.id), Is.EquivalentTo(stage.encounter.defenders.Select(d => d.id)));
            }
            var final = battles.Last();
            Assert.That(final.encounter.defenders.Select(d => d.id), Does.Contain("D25").And.Contain("D29").And.Contain("D24"));
            Assert.That(final.encounter.defenders.Single(d => d.id == "D24").position, Is.EqualTo(final.map.Cell(7, 7)));
        }

        [Test] public void BossTierMechanicProvenancePreservesDifferentDebtAndProtectionWindows()
        {
            var second = CampaignJourneyCatalog.Stage(9); var final = CampaignJourneyCatalog.Stage(12);
            Assert.That(second.GuardValue("D25", "debtMaxHealthMultiplier", -1), Is.EqualTo(1));
            Assert.That(second.GuardValue("D25", "finalProtectionSeconds", -1), Is.EqualTo(7));
            Assert.That(second.GuardValue("D25", "healthMultiplier", -1), Is.EqualTo(1.45f));
            Assert.That(final.GuardValue("D25", "debtMaxHealthMultiplier", -1), Is.EqualTo(1.25f));
            Assert.That(final.GuardValue("D25", "finalProtectionSeconds", -1), Is.EqualTo(8));
            Assert.That(final.GuardValue("D25", "healthMultiplier", -1), Is.EqualTo(1.5f));
        }

        [Test] public void RuntimeGuardCopyCannotChangeTheFormalAssetOrOriginalMapSandbox()
        {
            var stage = CampaignJourneyCatalog.Stage(12); float before = stage.encounter.defenders[0].health;
            float sandbox = stage.map.encounter.defenders[0].health;
            var copy = stage.CopyDefenders(); copy[0].health = 1; copy[0].skill.attackMultiplier = 999;
            Assert.That(stage.encounter.defenders[0].health, Is.EqualTo(before));
            Assert.That(stage.encounter.defenders[0].skill.attackMultiplier, Is.Not.EqualTo(999));
            Assert.That(stage.map.encounter.defenders[0].health, Is.EqualTo(sandbox));
        }

        [Test] public void ExpandedMidAndLateWavesCanDeployTwoStarTalulahAndOfferThreeStarWindows()
        {
            int coreCost = Mathf.RoundToInt(CampaignV2Balance.StatValue("CARD_TALULAH_TRIAL", 2, "dp"));
            int threeStarCost = Mathf.RoundToInt(CampaignV2Balance.StatValue("CARD_TALULAH_TRIAL", 3, "dp"));
            foreach (var stage in CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply && s.stage >= 5))
            {
                foreach (var wave in stage.encounter.WaveSnapshot().Waves)
                    Assert.That(wave.DP, Is.GreaterThanOrEqualTo(coreCost),
                        "A two-star five-cost core must fit after the deployment-cost reduction: stage " + stage.stage);
                Assert.That(stage.encounter.WaveSnapshot().Waves.Max(w => w.DP), Is.GreaterThanOrEqualTo(threeStarCost));
            }
        }

        [Test] public void LaterBudgetsGrowFromOneAndAHalfToDoubleWhileTutorialBudgetsStayFixed()
        {
            Assert.That(CampaignJourneyCatalog.Stage(2).dp, Is.EqualTo(10));
            Assert.That(CampaignJourneyCatalog.Stage(3).dp, Is.EqualTo(12));
            Assert.That(CampaignJourneyCatalog.Stage(4).dp, Is.Zero, "The fourth node is a supply receipt.");
            int[] stages = { 5, 6, 7, 9, 10, 12 }, originalBudgets = { 14, 16, 18, 20, 22, 24 };
            float previous = 0;
            for (int i = 0; i < stages.Length; i++)
            {
                float ratio = (float)CampaignJourneyCatalog.Stage(stages[i]).dp / originalBudgets[i];
                Assert.That(ratio, Is.InRange(1.5f, 2f), "stage " + stages[i]);
                Assert.That(ratio, Is.GreaterThan(previous), "Each later encounter has a larger proportional increase.");
                previous = ratio;
            }
            Assert.That(CampaignJourneyCatalog.Stage(5).dp, Is.EqualTo(21));
            Assert.That(CampaignJourneyCatalog.Stage(12).dp, Is.EqualTo(48));
        }

    }
}
