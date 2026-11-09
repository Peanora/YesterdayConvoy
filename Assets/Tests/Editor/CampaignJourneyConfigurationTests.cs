using System.Linq;
using NUnit.Framework;
using UnityEngine;

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
            Assert.That(journey.stages.All(s => s.naturalXP == 2), Is.True);
        }

        [TestCase(2, 780)] [TestCase(3, 1420)] [TestCase(5, 8700)] [TestCase(6, 9732)]
        [TestCase(7, 12234)] [TestCase(9, 20440)] [TestCase(10, 28112)] [TestCase(12, 48300)]
        public void StageHpTotalsUseRoundedScopedStrengthening(int stage, int expected)
        {
            var entry = CampaignJourneyCatalog.Stage(stage);
            Assert.That(entry.encounter.defenders.Sum(d => d.health), Is.EqualTo(expected));
            Assert.That(entry.encounter, Is.Not.SameAs(entry.map.encounter));
            Assert.That(entry.encounter.attackWaves, Is.Not.Null);
            Assert.That(entry.dp, Is.EqualTo(entry.encounter.WaveSnapshot().Waves.Sum(w => w.DP)));
            Assert.That(entry.encounter.defenders.Length, Is.EqualTo(entry.guardTiers.Length));
        }

        [Test] public void FirstBossUsesIntermittentSelfRecoveryAndFinalSariaKeepsHerHealingMechanic()
        {
            var first = CampaignJourneyCatalog.Stage(5); var cardigan = first.encounter.defenders.First();
            Assert.That(cardigan.id, Is.EqualTo("D02"));
            Assert.That(cardigan.health, Is.EqualTo(2175)); Assert.That(cardigan.attack, Is.EqualTo(150)); Assert.That(cardigan.armor, Is.EqualTo(15));
            Assert.That(cardigan.skill.kind, Is.EqualTo(DefenderSkillKind.CardiganRecovery));
            Assert.That(first.GuardValue("D02", "healFraction", -1), Is.EqualTo(.3f));
            Assert.That(first.GuardValue("D02", "requiredSP", -1), Is.EqualTo(24));
            Assert.That(CampaignJourneyCatalog.Stage(12).encounter.defenders.Any(d => d.id == "D29" && d.skill.kind == DefenderSkillKind.SariaCalcification), Is.True);
        }

        [Test] public void ProgressionUsesSixtyGuardsAndRestoredFiniteIndependentWaveBudgets()
        {
            var battles = CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply).ToArray();
            Assert.That(battles.Sum(s => s.encounter.defenders.Length), Is.EqualTo(60));
            Assert.That(battles.Select(s => s.encounter.defenders.Length), Is.EqualTo(new[] { 1, 2, 8, 8, 9, 10, 10, 12 }));
            Assert.That(battles.Select(s => s.dp), Is.EqualTo(new[] { 10, 12, 14, 16, 18, 20, 22, 24 }));
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

        [Test] public void RestoredMidAndLateWavesCanDeployTwoStarTalulahAndOfferThreeStarWindows()
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

        [TestCase(2, "D01", 780, 80, 10)]
        [TestCase(3, "D05", 1140, 100, 8)]
        [TestCase(5, "D02", 2175, 150, 15)]
        [TestCase(6, "D03", 2160, 180, 16)]
        [TestCase(7, "D02", 2784, 180, 13)]
        [TestCase(9, "D25", 5040, 224, 24)]
        [TestCase(10, "D01", 6120, 400, 26)]
        [TestCase(12, "D25", 9100, 380, 28)]
        public void OriginalEncounterCoresReceiveScopedHealthAndAttackGrowthWithoutMultiplyingArmor(int stage, string id, float hp, float attack, float armor)
        {
            var guard = CampaignJourneyCatalog.Stage(stage).encounter.defenders.Single(d => d.id == id);
            Assert.That(guard.health, Is.EqualTo(hp));
            Assert.That(guard.attack, Is.EqualTo(attack).Within(.001f));
            Assert.That(guard.armor, Is.EqualTo(armor));
        }
    }
}
