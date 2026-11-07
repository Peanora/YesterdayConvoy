using System;
using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class SynergyActivationFeedbackTests
    {
        static readonly string[] Reunion = SynergyTrialCatalog.Find("reunion").Members.ToArray();
        static SynergySummary[] Rows(params string[] ids) => SynergySummary.From(ids);
        static SynergyActivationFeedback.Pulse Pulse(SynergyActivationFeedback feedback, string name) =>
            feedback.Active.Activations.Single(pulse => pulse.Name == name);

        [Test] public void OnlyCrossingAnActualThresholdCreatesAnotherBatch()
        {
            var feedback = new SynergyActivationFeedback();
            Assert.That(feedback.Observe(Rows(Reunion.Take(1).ToArray())), Is.False);
            foreach (int count in new[] { 2, 3, 4, 5, 6 })
            {
                int previous = feedback.Revision;
                bool crossed = count == 2 || count == 4 || count == 6;
                Assert.That(feedback.Observe(Rows(Reunion.Take(count).ToArray())), Is.EqualTo(crossed));
                Assert.That(feedback.Revision, Is.EqualTo(previous + (crossed ? 1 : 0)));
                Assert.That(Pulse(feedback, "整合运动").Tier, Is.EqualTo(count >= 6 ? 6 : count >= 4 ? 4 : 2));
                Assert.That(feedback.Observe(Rows(Reunion.Take(count).ToArray())), Is.False);
            }
        }

        [Test] public void SameTierReplacementPrunesTheOldMemberAndAnAbsentRowRearmsTheThreshold()
        {
            var feedback = new SynergyActivationFeedback();
            Assert.That(feedback.Observe(Rows(Reunion[0], Reunion[1])), Is.True);
            int revision = feedback.Revision;
            feedback.Advance(.2f);
            Assert.That(feedback.Observe(Rows(Reunion[0], Reunion[2])), Is.False);
            Assert.That(feedback.Revision, Is.EqualTo(revision));
            Assert.That(Pulse(feedback, "整合运动").ContainsMember(Reunion[0]), Is.True);
            Assert.That(Pulse(feedback, "整合运动").ContainsMember(Reunion[1]), Is.False);
            Assert.That(Pulse(feedback, "整合运动").ContainsMember(Reunion[2]), Is.False,
                "A same-tier newcomer does not inherit an older activation's pulse.");
            Assert.That(feedback.Active.IsAddedMember(Reunion[2]), Is.False);
            Assert.That(feedback.Active.Age, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(feedback.Observe(Rows()), Is.False);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Observe(Rows(Reunion[0], Reunion[2])), Is.True,
                "Rows with zero members disappear from SynergySummary and still must rearm.");
            Assert.That(feedback.Revision, Is.EqualTo(revision + 1));
        }

        [Test] public void SimultaneousSynergiesShareOneBatchWithTheirOwnMemberSets()
        {
            var feedback = new SynergyActivationFeedback();
            var snow = SynergyTrialCatalog.Find("snow").Members.ToArray();
            var ids = snow.Concat(new[] { Reunion[0] }).ToArray();
            Assert.That(feedback.Observe(Rows(ids)), Is.True);
            Assert.That(feedback.Revision, Is.EqualTo(1));
            Assert.That(Pulse(feedback, "雪怪小队").Tier, Is.EqualTo(5));
            Assert.That(Pulse(feedback, "整合运动").Tier, Is.EqualTo(6));
            CollectionAssert.AreEquivalent(snow, Pulse(feedback, "雪怪小队").Members);
            Assert.That(Pulse(feedback, "雪怪小队").ContainsMember(Reunion[0]), Is.False);
            Assert.That(Pulse(feedback, "整合运动").ContainsMember(Reunion[0]), Is.True);
            Assert.That(feedback.Active.AddedMembers.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(feedback.Observe(Rows(ids.Concat(new[] { ids[0] }).ToArray())), Is.False,
                "A duplicate owned card cannot create another synergy kind or another audio batch.");
        }

        [Test] public void TransferFromEightToNineStillActivatesWhenBothGradesArePrismatic()
        {
            string[] native = { "CARD_M03", "CARD_M15", "CARD_M16", "CARD_M17", "CARD_M18", "CARD_M19", "CARD_M20" };
            var ids = native.Concat(new[] { "CARD_M07", "CARD_M08" }).ToArray();
            var feedback = new SynergyActivationFeedback();
            Assert.That(feedback.Observe(SynergySummary.From(ids, new[] { "CARD_M07" })), Is.True);
            var eight = Pulse(feedback, "游击队");
            Assert.That(eight.Tier, Is.EqualTo(8));
            int previous = feedback.Revision;
            Assert.That(feedback.Observe(SynergySummary.From(ids, new[] { "CARD_M07", "CARD_M08" })), Is.True);
            var nine = Pulse(feedback, "游击队");
            Assert.That(nine.Tier, Is.EqualTo(9));
            Assert.That(feedback.Revision, Is.EqualTo(previous + 1));
            Assert.That(SynergyBadgePresentation.GradeFor(eight.Name, eight.Tier),
                Is.EqualTo(SynergyBadgePresentation.GradeFor(nine.Name, nine.Tier)));
            Assert.That(nine.ContainsMember("CARD_M08"), Is.True);
            Assert.That(feedback.Active.AddedMembers, Is.Empty,
                "Designating an already prepared card is not a new-card entrance.");
        }

        [Test] public void PauseAndStageCleanupPreserveObservationUntilANewChallenge()
        {
            var feedback = new SynergyActivationFeedback();
            var rows = Rows(Reunion[0], Reunion[1]);
            Assert.That(feedback.Observe(rows), Is.True);
            int first = feedback.Active.Sequence;
            Assert.That(feedback.Advance(.4f), Is.True);
            Assert.That(feedback.Advance(8, true), Is.True);
            Assert.That(feedback.Active.Age, Is.EqualTo(.4f).Within(.0001f));
            feedback.Clear(false);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Observe(rows), Is.False, "Returning to preparation at the same tier is quiet.");
            feedback.Clear();
            Assert.That(feedback.Observe(rows), Is.True);
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(first));
            Assert.That(feedback.Advance(SynergyActivationFeedback.Lifetime + .01f), Is.False);
            Assert.That(feedback.Active, Is.Null);
        }
    }
}
