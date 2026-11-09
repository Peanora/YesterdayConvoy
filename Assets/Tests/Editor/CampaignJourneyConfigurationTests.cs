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
            Assert.That(journey.stages.Select(s => s.baseGold), Is.EqualTo(new[] { 4, 5, 5, 5, 6, 5, 5, 6, 7, 6, 7, 7 }));
            Assert.That(journey.stages.All(s => s.naturalXP == 2), Is.True);
        }

        [TestCase(2, 390)] [TestCase(3, 570)] [TestCase(5, 1290)] [TestCase(6, 1184)]
        [TestCase(7, 1608)] [TestCase(9, 2380)] [TestCase(10, 2358)] [TestCase(12, 4580)]
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
            Assert.That(cardigan.health, Is.EqualTo(870)); Assert.That(cardigan.attack, Is.EqualTo(60)); Assert.That(cardigan.armor, Is.EqualTo(15));
            Assert.That(cardigan.skill.kind, Is.EqualTo(DefenderSkillKind.CardiganRecovery));
            Assert.That(first.GuardValue("D02", "healFraction", -1), Is.EqualTo(.3f));
            Assert.That(first.GuardValue("D02", "requiredSP", -1), Is.EqualTo(24));
            Assert.That(CampaignJourneyCatalog.Stage(12).encounter.defenders.Any(d => d.id == "D29" && d.skill.kind == DefenderSkillKind.SariaCalcification), Is.True);
        }

        [Test] public void ProgressionUsesFifteenGuardsAndFiniteIndependentWaveWindows()
        {
            var battles = CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply).ToArray();
            Assert.That(battles.Sum(s => s.encounter.defenders.Length), Is.EqualTo(15));
            Assert.That(battles.Select(s => s.dp), Is.EqualTo(new[] { 14, 18, 32, 36, 40, 54, 60, 66 }));
            Assert.That(battles.Select(s => s.encounter.attackWaves.waves.Length), Is.EqualTo(new[] { 1, 1, 2, 2, 2, 3, 3, 3 }));
            Assert.That(battles.Select(s => s.encounter.attackWaves).Distinct().Count(), Is.EqualTo(8));
            foreach (var stage in battles)
            {
                Assert.That(stage.encounter.WaveSnapshot().TotalDuration, Is.InRange(80, 210));
                Assert.That(stage.encounter.attackWaves.waves.All(w => stage.encounter.lanes.Any(l => l.id == w.laneId)), Is.True);
                Assert.That(stage.encounter.attackWaves, Is.Not.SameAs(stage.map.encounter.attackWaves));
            }
            var final = battles.Last();
            Assert.That(final.encounter.defenders.Select(d => d.id), Is.EqualTo(new[] { "D25", "D29", "D24" }));
            Assert.That(final.encounter.defenders.Last().position, Is.EqualTo(final.map.Cell(7, 7)));
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

        [Test] public void MidAndLateWavesCanDeployThreeStarTalulahWithAOneStarCommander()
        {
            int coreCost = Mathf.RoundToInt(CampaignV2Balance.StatValue("CARD_TALULAH_TRIAL", 3, "dp"));
            int supportCost = Mathf.RoundToInt(CampaignV2Balance.StatValue("CARD_M17", 1, "dp"));
            foreach (var stage in CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply && s.stage >= 5))
                foreach (var wave in stage.encounter.WaveSnapshot().Waves)
                    Assert.That(wave.DP, Is.GreaterThanOrEqualTo(coreCost + supportCost),
                        "A high-star five-cost core and its commander must fit in the same wave: stage " + stage.stage);
        }
    }
}
