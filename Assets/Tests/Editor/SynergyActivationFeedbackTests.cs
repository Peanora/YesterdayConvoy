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
        static SynergySummary Row(string tag, int count)
        {
            var entry = SynergyTrialCatalog.Entries.Single(item => item.Tag == tag);
            return Rows(entry.Members.Take(count).ToArray()).Single(item => item.Name == tag);
        }
        static SynergySummary[] ReunionRows(int count) => Rows(Reunion.Take(count).ToArray()).Where(row => row.Name == "整合运动").ToArray();

        [Test] public void RestoringSceneObservationStaysQuietButKeepsRealUpgradesDowngradesAndReactivation()
        {
            var feedback=new SynergyActivationFeedback();
            feedback.RestoreObserved(ReunionRows(4));
            Assert.That(feedback.Pending,Is.False,"Scene arrival restores observation, never an old pulse queue.");
            int revision=feedback.Revision;
            Assert.That(feedback.Observe(ReunionRows(4)),Is.False);Assert.That(feedback.Revision,Is.EqualTo(revision));
            Assert.That(feedback.Observe(ReunionRows(2)),Is.True);Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(2));
            feedback.Advance(10);
            Assert.That(feedback.Observe(ReunionRows(4)),Is.True);Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(4));
            Assert.That(feedback.Observe(ReunionRows(1)),Is.True);Assert.That(feedback.Pending,Is.False);
            Assert.That(feedback.Observe(ReunionRows(2)),Is.True);Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(2));
        }

        [Test] public void FreshChallengeClearsRestoredObservationAndAllowsItsFirstRealActivation()
        {
            var feedback=new SynergyActivationFeedback();feedback.RestoreObserved(ReunionRows(2));
            Assert.That(feedback.Observe(ReunionRows(2)),Is.False);
            feedback.Clear();
            Assert.That(feedback.Observe(ReunionRows(2)),Is.True);Assert.That(feedback.Pending,Is.True);
        }

        [Test] public void OnlyCrossingAnActualThresholdCreatesAnotherBatch()
        {
            var feedback = new SynergyActivationFeedback();
            Assert.That(feedback.Observe(ReunionRows(1)), Is.False);
            foreach (int count in new[] { 2, 3, 4, 5, 6, 7, 8 })
            {
                int previous = feedback.Revision;
                bool crossed = count == 2 || count == 4 || count == 6 || count == 8;
                var rows = ReunionRows(count);
                Assert.That(feedback.Observe(rows), Is.EqualTo(crossed));
                Assert.That(feedback.Revision, Is.EqualTo(previous + (crossed ? 1 : 0)));
                if (crossed) Assert.That(Pulse(feedback, "整合运动").Tier, Is.EqualTo(count));
                Assert.That(feedback.Observe(rows.Concat(rows).ToArray()), Is.False,
                    "Reordering or repeating the same observed row cannot add another cue.");
                feedback.Advance(10);
                Assert.That(feedback.Pending, Is.False);
            }
        }

        [Test] public void SameCountFormationReplacementChangesTheActualTierAndUsesOnlyCurrentMembers()
        {
            string[] two={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07"};
            string[] three=two.Where(id=>id!="CARD_M20").Concat(new[]{"CARD_M11"}).ToArray();
            Func<string[],SynergySummary[]> reunion=ids=>Rows(ids).Where(row=>row.Name=="整合运动").ToArray();
            var feedback=new SynergyActivationFeedback();feedback.Observe(reunion(two));feedback.Advance(.1f);
            Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(6));int revision=feedback.Revision;
            Assert.That(feedback.Observe(reunion(three)),Is.True);Assert.That(feedback.Revision,Is.EqualTo(revision+1));
            var eight=Pulse(feedback,"整合运动");Assert.That(eight.Tier,Is.EqualTo(8));Assert.That(eight.Members.Count,Is.EqualTo(8));
            Assert.That(eight.ContainsMember("CARD_M11"),Is.True);Assert.That(eight.ContainsMember("CARD_M20"),Is.False);
            Assert.That(SynergyBadgePresentation.GradeFor(eight.Name,eight.Tier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(feedback.Observe(reunion(two)),Is.True);Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(6));
            Assert.That(feedback.PendingCount,Is.EqualTo(1),"The old prismatic pulse must not survive a formation downgrade.");
        }

        [Test] public void ANewKindWithinOneFormationDoesNotPretendToCrossTheSixKindTier()
        {
            string[] native={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20"};
            var feedback=new SynergyActivationFeedback();
            feedback.Observe(Rows(native.Take(4).ToArray()).Where(row=>row.Name=="整合运动").ToArray());feedback.Advance(.1f);
            int revision=feedback.Revision;
            Assert.That(feedback.Observe(Rows(native.Take(6).ToArray()).Where(row=>row.Name=="整合运动").ToArray()),Is.False);
            Assert.That(feedback.Revision,Is.EqualTo(revision));Assert.That(Pulse(feedback,"整合运动").Tier,Is.EqualTo(4));
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
            Assert.That(feedback.Observe(Rows()), Is.True);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Observe(Rows(Reunion[0], Reunion[2])), Is.True,
                "Rows with zero members disappear from SynergySummary and still must rearm.");
            Assert.That(feedback.Revision, Is.EqualTo(revision + 1));
        }

        [Test] public void SimultaneousSynergiesUseVisualGradeThenBuiltinOrderEvenWithReversedInput()
        {
            var feedback = new SynergyActivationFeedback();
            string[] native = { "CARD_M03", "CARD_M15", "CARD_M16", "CARD_M17", "CARD_M18", "CARD_M19", "CARD_M20" };
            var guerrilla = SynergySummary.From(native.Concat(new[] { "CARD_M07", "CARD_M08" }), new[] { "CARD_M07", "CARD_M08" }).Single(row => row.Name == "游击队");
            var rows = new[] { Row("整合运动", 6), Row("雪怪小队", 5), Row("感染生物", 6), guerrilla,
                Row("泥岩小队", 5), Row("牧群", 4), Row("深池", 2), Row("海嗣", 2), Row("萨卡兹", 6) }.Reverse().ToArray();
            Assert.That(feedback.Observe(rows), Is.True);
            Assert.That(feedback.Revision, Is.EqualTo(9));
            Assert.That(feedback.PendingCount, Is.EqualTo(9));
            string[] expected = { "游击队", "整合运动", "感染生物", "泥岩小队", "萨卡兹", "雪怪小队", "牧群", "深池", "海嗣" };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(feedback.Active.Activations.Count, Is.EqualTo(1));
                var pulse = feedback.Active.Activations[0];
                Assert.That(pulse.Name, Is.EqualTo(expected[i]));
                Assert.That(feedback.Active.Sequence, Is.EqualTo(i + 1));
                CollectionAssert.AreEqual(rows.Single(row => row.Name == pulse.Name).Members, pulse.Members);
                Assert.That(feedback.Active.AddedMembers.All(pulse.ContainsMember), Is.True);
                feedback.Advance(SynergyActivationFeedback.Lifetime);
                Assert.That(feedback.Active, Is.Null);
                if (i == expected.Length - 1) break;
                Assert.That(feedback.Pending, Is.True);
                Assert.That(feedback.QueuedCount, Is.EqualTo(expected.Length - i - 1));
                Assert.That(feedback.SecondsToTransition, Is.EqualTo(SynergyActivationFeedback.Gap).Within(.00001f));
                feedback.Advance(SynergyActivationFeedback.Gap);
            }
            Assert.That(feedback.Pending, Is.False);
            Assert.That(feedback.Observe(rows), Is.False, "Completing the queue does not rearm an unchanged lineup.");
            // In particular, Gold 泥岩5 precedes Gold 萨卡兹6 despite its lower numeric threshold;
            // Silver 雪怪5 and Silver 牧群4 are both below every Gold entry.
        }

        [Test] public void TransferFromEightToNineStillActivatesWhenBothGradesArePrismatic()
        {
            string[] native = { "CARD_M03", "CARD_M15", "CARD_M16", "CARD_M17", "CARD_M18", "CARD_M19", "CARD_M20" };
            var ids = native.Concat(new[] { "CARD_M07", "CARD_M08" }).ToArray();
            var feedback = new SynergyActivationFeedback();
            Assert.That(feedback.Observe(SynergySummary.From(ids, new[] { "CARD_M07" })), Is.True);
            var eight = Pulse(feedback, "游击队");
            Assert.That(eight.Tier, Is.EqualTo(8));
            feedback.Advance(30);
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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ANewMudrockTierImmediatelyReplacesTheScheduleAndDoesNotReplayAnAlreadyShownSarkaz(int sarkazProgress)
        {
            var feedback = new SynergyActivationFeedback();
            feedback.Observe(new[] { Row("萨卡兹", 4), Row("泥岩小队", 4) });
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("泥岩小队"));
            if (sarkazProgress == 0) feedback.Advance(.3f);
            else
            {
                feedback.Advance(SynergyActivationFeedback.Lifetime + SynergyActivationFeedback.Gap);
                Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("萨卡兹"));
                feedback.Advance(sarkazProgress == 1 ? .3f : SynergyActivationFeedback.Lifetime);
            }
            Assert.That(feedback.HasShown("萨卡兹", 4), Is.EqualTo(sarkazProgress > 0));
            int revision = feedback.Revision;
            Assert.That(feedback.Observe(new[] { Row("泥岩小队", 5), Row("萨卡兹", 4) }), Is.True);
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("泥岩小队"));
            Assert.That(feedback.Active.Activations.Single().Tier, Is.EqualTo(5));
            Assert.That(feedback.Active.Age, Is.Zero, "The new Gold tier starts immediately, without an old tail or gap.");
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(revision));
            Assert.That(feedback.HasShown("泥岩小队", 5), Is.False);
            Assert.That(feedback.PendingCount, Is.EqualTo(sarkazProgress == 0 ? 2 : 1));
            if (sarkazProgress == 0)
            {
                feedback.Advance(SynergyActivationFeedback.Lifetime + SynergyActivationFeedback.Gap);
                Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("萨卡兹"));
                Assert.That(feedback.Active.Activations.Single().Tier, Is.EqualTo(4));
                Assert.That(feedback.HasShown("萨卡兹", 4), Is.False, "Age zero has not displayed the pending trait yet.");
                feedback.Advance(SynergyActivationFeedback.Lifetime);
            }
            else Assert.That(feedback.Advance(10), Is.False, "A partly or fully shown unchanged Sarkaz4 is not replayed.");
            Assert.That(feedback.Pending, Is.False);
        }

        [Test] public void AnUnstartedBlockedItemSurvivesRebuildWithLatestMembersAndItsOriginalArrivalMarkers()
        {
            var feedback = new SynergyActivationFeedback();
            var mudrock = Row("泥岩小队", 4);
            feedback.Observe(new[] { mudrock, Row("萨卡兹", 4) });
            int sequence = feedback.Active.Sequence;
            feedback.Advance(100, true);
            feedback.Advance(0);
            Assert.That(feedback.HasShown("泥岩小队", 4), Is.False);
            Assert.That(feedback.HasShown("萨卡兹", 4), Is.False);
            var sarkaz = SynergyTrialCatalog.Find("sarkaz").Members.ToArray();
            string[] latestSarkaz = { sarkaz[0], sarkaz[1], sarkaz[2], sarkaz[4] };
            var latest = Rows(latestSarkaz).Single(row => row.Name == "萨卡兹");
            Assert.That(feedback.Observe(new[] { latest, Row("泥岩小队", 5) }), Is.True);
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(sequence));
            CollectionAssert.AreEqual(Row("泥岩小队", 5).Members, feedback.Active.Activations.Single().Members);
            Assert.That(mudrock.Members.All(feedback.Active.IsAddedMember), Is.True,
                "Previously observed purchase IDs still block their not-yet-started replacement until arrival.");
            feedback.Advance(SynergyActivationFeedback.Lifetime + SynergyActivationFeedback.Gap);
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("萨卡兹"));
            CollectionAssert.AreEqual(latestSarkaz, feedback.Active.Activations.Single().Members);
            CollectionAssert.AreEquivalent(latestSarkaz, feedback.Active.AddedMembers,
                "Retain original pending additions, discard departed members and merge truly new IDs.");
            Assert.That(feedback.HasShown("萨卡兹", 4), Is.False);
            feedback.Advance(.01f);
            Assert.That(feedback.HasShown("萨卡兹", 4), Is.True);
        }

        [Test] public void AChangeDuringTheClearGapImmediatelyReordersWithoutWaitingTheOldGap()
        {
            var feedback = new SynergyActivationFeedback();
            feedback.Observe(new[] { Row("泥岩小队", 4), Row("萨卡兹", 4) });
            feedback.Advance(SynergyActivationFeedback.Lifetime + .1f);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Pending, Is.True);
            Assert.That(feedback.SecondsToTransition, Is.LessThan(SynergyActivationFeedback.Gap));
            int revision = feedback.Revision;
            feedback.Observe(new[] { Row("泥岩小队", 5), Row("萨卡兹", 4) });
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("泥岩小队"));
            Assert.That(feedback.Active.Activations.Single().Tier, Is.EqualTo(5));
            Assert.That(feedback.Active.Age, Is.Zero);
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(revision));
            Assert.That(feedback.SecondsToTransition, Is.EqualTo(SynergyActivationFeedback.Lifetime));
            Assert.That(feedback.PendingCount, Is.EqualTo(2));
        }

        [Test] public void PauseFreezesBothActiveItemsAndGapsAndLargeStepsRetainTheirRemainder()
        {
            var feedback = new SynergyActivationFeedback();
            var rows = new[] { Row("整合运动", 6), Row("雪怪小队", 5), Row("海嗣", 4) };
            feedback.Observe(rows);
            feedback.Advance(.4f);
            Assert.That(feedback.Advance(8, true), Is.True);
            Assert.That(feedback.Active.Age, Is.EqualTo(.4f).Within(.00001f));
            feedback.Advance(SynergyActivationFeedback.Lifetime - feedback.Active.Age);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Pending, Is.True);
            float gap = feedback.SecondsToTransition;
            feedback.Advance(8, true);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.SecondsToTransition, Is.EqualTo(gap));
            feedback.Advance(gap + SynergyActivationFeedback.Lifetime + SynergyActivationFeedback.Gap + .2f);
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("海嗣"));
            Assert.That(feedback.Active.Age, Is.EqualTo(.2f).Within(.00001f));
            Assert.That(feedback.PendingCount, Is.EqualTo(1));
            Assert.That(feedback.QueuedCount, Is.Zero);
            Assert.That(feedback.SecondsToTransition, Is.EqualTo(SynergyActivationFeedback.Lifetime - .2f).Within(.00001f));
            Assert.That(feedback.Advance(100), Is.False);
            Assert.That(feedback.SecondsToTransition, Is.Zero);
        }

        [Test] public void RapidTierChangesKeepOnlyTheLatestTierAndAnInactiveRowDoesNotInterrupt()
        {
            var feedback = new SynergyActivationFeedback();
            feedback.Observe(ReunionRows(6));
            feedback.Advance(.2f);
            Assert.That(feedback.HasShown("整合运动", 6), Is.True);
            int sequence = feedback.Active.Sequence;
            Assert.That(feedback.Observe(ReunionRows(4)), Is.True);
            Assert.That(feedback.Active.Activations.Single().Tier, Is.EqualTo(4));
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(sequence));
            Assert.That(feedback.Active.Age, Is.Zero);
            Assert.That(feedback.HasShown("整合运动", 4), Is.False);
            Assert.That(feedback.Observe(ReunionRows(2)), Is.True);
            Assert.That(feedback.Active.Activations.Single().Tier, Is.EqualTo(2));
            Assert.That(feedback.PendingCount, Is.EqualTo(1), "An obsolete unstarted Silver entry cannot remain queued.");
            Assert.That(feedback.Observe(ReunionRows(1)), Is.True);
            Assert.That(feedback.Pending, Is.False);
            Assert.That(feedback.SecondsToTransition, Is.Zero);
            Assert.That(feedback.Observe(ReunionRows(2)), Is.True);
            feedback.Advance(.01f);
            Assert.That(feedback.HasShown("整合运动", 2), Is.True);
            sequence = feedback.Active.Sequence;
            float age = feedback.Active.Age;
            // Tier-zero rows can appear/disappear as isolated units enter or leave the lineup.
            var inactiveSnow = Row("雪怪小队", 1);
            Assert.That(feedback.Observe(ReunionRows(2).Concat(new[] { inactiveSnow }).ToArray()), Is.False);
            Assert.That(feedback.Observe(ReunionRows(2)), Is.False);
            Assert.That(feedback.Active.Sequence, Is.EqualTo(sequence));
            Assert.That(feedback.Active.Age, Is.EqualTo(age));
        }

        [Test] public void StageClearDoesNotResurrectAnUnshownTraitWhenAnUnrelatedTierChanges()
        {
            var feedback = new SynergyActivationFeedback();
            var reunion = ReunionRows(2);
            feedback.Observe(reunion);
            int sequence = feedback.Active.Sequence;
            Assert.That(feedback.HasShown("整合运动", 2), Is.False);
            feedback.Clear(false);
            Assert.That(feedback.Pending, Is.False);
            Assert.That(feedback.QueuedCount, Is.Zero);
            Assert.That(feedback.Observe(reunion), Is.False);
            var current = reunion.Concat(new[] { Row("雪怪小队", 2) }).ToArray();
            Assert.That(feedback.Observe(current), Is.True);
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("雪怪小队"));
            Assert.That(feedback.PendingCount, Is.EqualTo(1),
                "A cleared old positive tier is not a candidate merely because it is still positive and unshown.");
            feedback.Advance(.01f);
            Assert.That(feedback.HasShown("雪怪小队", 2), Is.True);
            feedback.Clear(false);
            Assert.That(feedback.HasShown("雪怪小队", 2), Is.True);
            feedback.Clear();
            Assert.That(feedback.Observe(current), Is.True);
            Assert.That(feedback.HasShown("雪怪小队", 2), Is.False);
            Assert.That(feedback.Active.Sequence, Is.GreaterThan(sequence));
            Assert.That(feedback.PendingCount, Is.EqualTo(2));
            feedback.Clear();
            Assert.That(feedback.Pending, Is.False);
            Assert.That(feedback.SecondsToTransition, Is.Zero);
        }

        [Test] public void ASubRemainingFloatStepThatRoundsToTheEndpointCannotLeaveAZeroLengthActiveItem()
        {
            var feedback = new SynergyActivationFeedback();
            feedback.Observe(new[] { Row("整合运动", 6), Row("雪怪小队", 5) });
            feedback.Advance(SynergyActivationFeedback.Lifetime - .0000001f);
            float remaining = feedback.SecondsToTransition;
            Assert.That(remaining, Is.GreaterThan(0));
            // At this magnitude the smaller positive step rounds Age to Lifetime. The controller
            // uses SecondsToTransition to split UI steps and must never see a stuck zero boundary.
            feedback.Advance(remaining * .75f);
            Assert.That(feedback.Active, Is.Null);
            Assert.That(feedback.Pending, Is.True);
            Assert.That(feedback.SecondsToTransition, Is.EqualTo(SynergyActivationFeedback.Gap));
            feedback.Advance(feedback.SecondsToTransition);
            Assert.That(feedback.Active.Activations.Single().Name, Is.EqualTo("雪怪小队"));
            Assert.That(feedback.Active.Age, Is.Zero);
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
