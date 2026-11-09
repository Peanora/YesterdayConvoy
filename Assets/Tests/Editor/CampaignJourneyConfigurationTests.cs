using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class CampaignJourneyConfigurationTests
    {
        [Test] public void FormalJourneyHasFiveRealSupplyReceiptsAndEightApprovedMapBattles()
        {
            var journey = CampaignJourneyCatalog.Current; Assert.That(journey, Is.Not.Null); Assert.DoesNotThrow(journey.Validate);
            Assert.That(journey.stages.Where(s => s.IsSupply).Select(s => s.stage), Is.EqualTo(new[] { 1, 2, 5, 9, 12 }));
            Assert.That(journey.stages.Where(s => !s.IsSupply).Select(s => s.map.index), Is.EqualTo(new[] { 7, 1, 2, 3, 4, 6, 5, 8 }));
            Assert.That(journey.stages.Where(s => s.IsSupply).All(s => s.dp == 0 && s.defense == 0 && s.encounter == null), Is.True);
            Assert.That(journey.stages.Select(s => s.baseGold), Is.EqualTo(new[] { 3, 4, 5, 5, 5, 6, 5, 5, 6, 7, 6, 7, 7 }));
            Assert.That(journey.stages.All(s => s.naturalXP == 2), Is.True);
        }

        [TestCase(3, 390)] [TestCase(4, 570)] [TestCase(6, 1290)] [TestCase(7, 1184)]
        [TestCase(8, 1608)] [TestCase(10, 2380)] [TestCase(11, 2358)] [TestCase(13, 4580)]
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
            var first = CampaignJourneyCatalog.Stage(6); var cardigan = first.encounter.defenders.First();
            Assert.That(cardigan.id, Is.EqualTo("D02"));
            Assert.That(cardigan.health, Is.EqualTo(870)); Assert.That(cardigan.attack, Is.EqualTo(60)); Assert.That(cardigan.armor, Is.EqualTo(15));
            Assert.That(cardigan.skill.kind, Is.EqualTo(DefenderSkillKind.CardiganRecovery));
            Assert.That(first.GuardValue("D02", "healFraction", -1), Is.EqualTo(.3f));
            Assert.That(first.GuardValue("D02", "requiredSP", -1), Is.EqualTo(24));
            Assert.That(CampaignJourneyCatalog.Stage(13).encounter.defenders.Any(d => d.id == "D29" && d.skill.kind == DefenderSkillKind.SariaCalcification), Is.True);
        }

        [Test] public void ProgressionUsesFifteenGuardsAndFiniteIndependentWaveWindows()
        {
            var battles = CampaignJourneyCatalog.Current.stages.Where(s => !s.IsSupply).ToArray();
            Assert.That(battles.Sum(s => s.encounter.defenders.Length), Is.EqualTo(15));
            Assert.That(battles.Select(s => s.dp), Is.EqualTo(new[] { 10, 12, 14, 16, 18, 20, 22, 24 }));
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
            var second = CampaignJourneyCatalog.Stage(10); var final = CampaignJourneyCatalog.Stage(13);
            Assert.That(second.GuardValue("D25", "debtMaxHealthMultiplier", -1), Is.EqualTo(1));
            Assert.That(second.GuardValue("D25", "finalProtectionSeconds", -1), Is.EqualTo(7));
            Assert.That(second.GuardValue("D25", "healthMultiplier", -1), Is.EqualTo(1.45f));
            Assert.That(final.GuardValue("D25", "debtMaxHealthMultiplier", -1), Is.EqualTo(1.25f));
            Assert.That(final.GuardValue("D25", "finalProtectionSeconds", -1), Is.EqualTo(8));
            Assert.That(final.GuardValue("D25", "healthMultiplier", -1), Is.EqualTo(1.5f));
        }

        [Test] public void RuntimeGuardCopyCannotChangeTheFormalAssetOrOriginalMapSandbox()
        {
            var stage = CampaignJourneyCatalog.Stage(13); float before = stage.encounter.defenders[0].health;
            float sandbox = stage.map.encounter.defenders[0].health;
            var copy = stage.CopyDefenders(); copy[0].health = 1; copy[0].skill.attackMultiplier = 999;
            Assert.That(stage.encounter.defenders[0].health, Is.EqualTo(before));
            Assert.That(stage.encounter.defenders[0].skill.attackMultiplier, Is.Not.EqualTo(999));
            Assert.That(stage.map.encounter.defenders[0].health, Is.EqualTo(sandbox));
        }
    }
}
