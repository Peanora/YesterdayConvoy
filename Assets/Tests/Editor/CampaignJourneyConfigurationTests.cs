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
            Assert.That(journey.stages.Select(s => s.baseGold), Is.EqualTo(new[] { 10, 12, 12, 14, 18, 22, 16, 18, 20, 24, 20, 24, 28 }));
            Assert.That(journey.stages.Select(s => s.naturalXP), Is.EqualTo(new[] { 2, 2, 2, 3, 3, 4, 3, 4, 4, 5, 5, 6, 0 }));
        }

        [TestCase(3, 260)] [TestCase(4, 400)] [TestCase(6, 882)] [TestCase(7, 1050)]
        [TestCase(8, 1580)] [TestCase(10, 1840)] [TestCase(11, 2670)] [TestCase(13, 4158)]
        public void StageHpTotalsUseRoundedAdoptedTierPanels(int stage, int expected)
        {
            var entry = CampaignJourneyCatalog.Stage(stage);
            Assert.That(entry.encounter.defenders.Sum(d => d.health), Is.EqualTo(expected));
            Assert.That(entry.encounter, Is.Not.SameAs(entry.map.encounter));
            Assert.That(entry.encounter.attackWaves, Is.Null);
            Assert.That(entry.encounter.defenders.Length, Is.EqualTo(entry.guardTiers.Length));
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
