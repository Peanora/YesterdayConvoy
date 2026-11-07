using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    // These tests verify real preparation transactions and presentation/audio state.
    // Full OnGUI pixel evidence and real OS pointer clicks belong to the visible Player run.
    public sealed class SynergyActivationSceneTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Trio = { "CARD_M07", "CARD_M08", "CARD_M09" };
        ManualBattleController controller;
        float oldScale;

        [SetUp] public void Setup() => oldScale = Time.timeScale;
        [TearDown] public void Cleanup()
        {
            if (controller != null && controller.ShopTrialActive) controller.LeaveShopTrial();
            Time.timeScale = oldScale;
        }

        object Invoke(string name, params object[] args) =>
            typeof(ManualBattleController).GetMethod(name, Hidden).Invoke(controller, args);
        T Field<T>(string name) => (T)typeof(ManualBattleController).GetField(name, Hidden).GetValue(controller);
        void SetField(string name, object value) =>
            typeof(ManualBattleController).GetField(name, Hidden).SetValue(controller, value);
        SynergyActivationFeedback Feedback => Field<SynergyActivationFeedback>("synergyActivationFeedback");
        AudioSource Source => Field<AudioSource>("synergyActivationSource");
        int SoundCount => Field<int>("synergyActivationSoundPlayCount");
        SynergySummary[] Rows => (SynergySummary[])typeof(ManualBattleController)
            .GetProperty("CurrentSynergies", Hidden).GetValue(controller);

        IEnumerator Load(string scene = "SynergyTrial")
        {
            yield return SceneManager.LoadSceneAsync(scene);
            for (int i = 0; i < 1800; i++)
            {
                controller = Object.FindAnyObjectByType<ManualBattleController>();
                if (controller != null && controller.CampaignActive && !controller.ArtLoading && !controller.ShopPanelTransitioning) break;
                yield return null;
            }
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.CampaignActive && !controller.ArtLoading, Is.True);
            Time.timeScale = 0;
        }

        IEnumerator PrepareTrio()
        {
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster", (object)Trio);
            // Complete any pre-existing modal as an explicit fixture, never by changing tier fields.
            SetField("inspectedSynergy", null);
            SetField("campaignConstructOpen", false);
            for (int i = 0; i < 240 && Feedback.Pending; i++) controller.Tick(.1f);
            controller.Tick(.001f);
            Assert.That(Feedback.Pending, Is.False);
            Assert.That(Rows.Single(row => row.Name == "整合运动").Tier, Is.EqualTo(2));
        }

        void Click(string id)
        {
            int index = controller.Shop.Bench.ToList().FindIndex(piece => piece.Id == id);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), id);
            Vector2 point = controller.ShopPieceRect(index).center;
            controller.ProcessShopPointer(EventType.MouseDown, point);
            controller.ProcessShopPointer(EventType.MouseUp, point);
        }

        void CrossReunionByRealClicks()
        {
            Click(Trio[0]); // Three to two: same tier.
            Click(Trio[1]); // Two to one: below the first threshold.
            Assert.That(controller.Battle.ReunionTier, Is.Zero);
            Click(Trio[0]); // One to two: a real upward crossing.
            Assert.That(controller.Battle.ReunionTier, Is.EqualTo(2));
            Assert.That(Feedback.Active, Is.Not.Null);
        }

        [UnityTest] public IEnumerator RealLineupClicksActivateOnceAndARebuiltSameTierBattleStaysQuiet()
        {
            yield return Load();
            yield return PrepareTrio();
            int revision = Feedback.Revision, sounds = SoundCount;
            CrossReunionByRealClicks();
            Assert.That(Feedback.Revision, Is.EqualTo(revision + 1));
            var pulse = Feedback.Active.Activations.Single(item => item.Name == "整合运动");
            CollectionAssert.AreEquivalent(new[] { Trio[0], Trio[2] }, pulse.Members);
            Assert.That(pulse.ContainsMember(Trio[1]), Is.False, "A reserve card receives no lineup emblem.");
            controller.Tick(.1f);
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            var battle = controller.Battle;
            Assert.That(controller.SetCampaignForms(InfectionForm.Shell, InfectionForm.Burst), Is.True);
            Assert.That(controller.Battle, Is.Not.SameAs(battle), "Configuration refresh really rebuilt the battle.");
            controller.Tick(.1f);
            Assert.That(Feedback.Revision, Is.EqualTo(revision + 1));
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            controller.Shop.Validate();
        }

        [UnityTest] public IEnumerator RealPurchaseWaitsForTheExistingArrivalBeforeFeedbackAndAudio()
        {
            yield return Load();yield return PrepareTrio();
            controller.Shop.Preset(new[]{new ShopTrialModel.Piece{Id=Trio[0],Star=1}},new[]{Trio[1],Trio[1]},8,0,100);
            var lineup=Field<System.Collections.Generic.List<string>>("shopLineup");lineup.Clear();lineup.Add(Trio[0]);
            Invoke("SyncShopDeck",false,false);
            Assert.That(controller.Battle.ReunionTier,Is.Zero);
            int sounds=SoundCount;
            var result=controller.PurchaseShopCard(0);
            Assert.That(result.Success,Is.True);Assert.That(controller.Battle.ReunionTier,Is.EqualTo(2));
            Assert.That(Feedback.Active,Is.Not.Null);Assert.That(Feedback.Active.IsAddedMember(Trio[1]),Is.True);
            controller.Tick(.1f);
            Assert.That(Feedback.Active.Age,Is.Zero,"The real model joins before its purchase portrait reaches the slot.");
            Assert.That(SoundCount,Is.EqualTo(sounds));
            yield return new WaitForSecondsRealtime(.7f);
            controller.Tick(.1f);
            Assert.That(Feedback.Active.Age,Is.GreaterThan(0));
            Assert.That(SoundCount,Is.EqualTo(sounds+1));
            Assert.That(Source.clip,Is.SameAs(Resources.Load<AudioClip>("SynergyActivation/bond-original")));
            int sequence=Feedback.Active.Sequence,revision=Feedback.Revision;
            float age=Feedback.Active.Age;
            var duplicate=controller.PurchaseShopCard(1);
            Assert.That(duplicate.Success,Is.True);
            controller.Tick(.1f);
            Assert.That(Feedback.Active.Age,Is.GreaterThan(age),"A later same-kind purchase must not re-arm arrival waiting for a pulse already on screen.");
            Assert.That(Feedback.Active.Sequence,Is.EqualTo(sequence));
            Assert.That(Feedback.Revision,Is.EqualTo(revision));
            Assert.That(SoundCount,Is.EqualTo(sounds+1));
            controller.Shop.Validate();
        }

        [UnityTest] public IEnumerator RealInfectionPromptDefersItsPulseAndAudioUntilModalCompletion()
        {
            yield return Load("InfectionTrial");
            Assert.That(controller.Battle.InfectionTier, Is.EqualTo(6));
            Assert.That(Field<string>("inspectedSynergy"), Is.EqualTo("感染生物"));
            Assert.That(Feedback.Active, Is.Not.Null);
            float age = Feedback.Active.Age;
            int sounds = SoundCount;
            controller.Tick(.7f);
            Assert.That(Feedback.Active.Age, Is.EqualTo(age));
            Assert.That(SoundCount, Is.EqualTo(sounds));
            Assert.That(Source == null || !Source.isPlaying, Is.True);
            // Modal completion fixture only; real opening/closing pointer actions are Player evidence.
            SetField("inspectedSynergy", null);
            controller.Tick(.1f);
            Assert.That(Feedback.Active.Age, Is.GreaterThan(age));
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            controller.Tick(.1f);
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
        }

        [UnityTest] public IEnumerator PreparationMenuFreezesThePulseAndOriginalAudioHonorsTheExistingMuteState()
        {
            yield return Load();
            yield return PrepareTrio();
            CrossReunionByRealClicks();
            int sounds = SoundCount;
            // Combat pause is intentionally unavailable in Ready; use its real preparation
            // blocking state rather than silently changing that established battle rule.
            SetField("loopMenuOpen", true);
            float age = Feedback.Active.Age;
            controller.Tick(.6f);
            Assert.That(Feedback.Active.Age, Is.EqualTo(age));
            Assert.That(SoundCount, Is.EqualTo(sounds));
            // Isolated mute-state fixture. The existing menu's real click remains a Player check.
            var mute = typeof(ManualBattleController).GetProperty("Muted");
            mute.GetSetMethod(true).Invoke(controller, new object[] { true });
            SetField("loopMenuOpen", false);
            controller.Tick(.1f);
            Assert.That(Source, Is.Not.Null);
            Assert.That(Source.clip, Is.SameAs(Resources.Load<AudioClip>("SynergyActivation/bond-original")));
            Assert.That(Source.clip, Is.Not.Null, "The runtime must use the delivered original cue.");
            Assert.That(Source.mute, Is.True);
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            mute.GetSetMethod(true).Invoke(controller, new object[] { false });
            controller.Tick(.04f);
            Assert.That(Source.mute, Is.False);
            age = Feedback.Active.Age;
            SetField("loopMenuOpen", true);
            controller.Tick(.5f);
            Assert.That(Feedback.Active.Age, Is.EqualTo(age));
            Assert.That(Source.isPlaying, Is.False);
            SetField("loopMenuOpen", false);
            controller.Tick(.05f);
            Assert.That(Feedback.Active.Age, Is.GreaterThan(age));
            Assert.That(SoundCount, Is.EqualTo(sounds + 1), "Resume does not replay the beginning of the cue.");
        }

        [UnityTest] public IEnumerator StartingBattleAndLeavingClearPresentationWhileNormalReplayKeepsTheTierQuiet()
        {
            yield return Load();
            yield return PrepareTrio();
            CrossReunionByRealClicks();
            controller.Tick(.1f);
            int revision = Feedback.Revision, sounds = SoundCount, owned = controller.Shop.Bench.Count;
            Assert.That(controller.StartLoopNextBattle(), Is.True);
            controller.Tick(.1f);
            Assert.That(Feedback.Active, Is.Null);
            Assert.That(Source == null || !Source.isPlaying, Is.True);
            Assert.That(controller.Battle.ReunionTier, Is.EqualTo(2));
            Assert.That(controller.Shop.Bench.Count, Is.EqualTo(owned));
            Invoke("ReplaySandbox");
            controller.Tick(.1f);
            Assert.That(Feedback.Active, Is.Null, "An unchanged returned formation is not a new threshold crossing.");
            Assert.That(Feedback.Revision, Is.EqualTo(revision));
            Assert.That(SoundCount, Is.EqualTo(sounds));
            controller.StartCampaign(true, 2718);
            Assert.That(Feedback.Active, Is.Not.Null, "A new challenge observes its initial real formation afresh.");
            Assert.That(Feedback.Revision, Is.GreaterThan(revision));
            controller.LeaveShopTrial();
            Assert.That(Feedback.Active, Is.Null);
            Assert.That(Feedback.Pending, Is.False);
            Assert.That(Source == null || !Source.isPlaying, Is.True);
        }

        [UnityTest] public IEnumerator RealSimultaneousCrossingsPlayByGradeThenBuiltinOrderWithSilentPausedGaps()
        {
            yield return Load();
            string[] reunion = SynergyTrialCatalog.Find("reunion").Members.ToArray();
            const string newcomer = "CARD_M16", unrelated = "CARD_M33";
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster", (object)reunion.Concat(new[] { unrelated }).ToArray());
            Click(newcomer);
            Invoke("ClearSynergyActivationFeedback", false);controller.Tick(.001f);
            Assert.That(controller.Battle.ReunionTier, Is.EqualTo(4));
            Assert.That(controller.Battle.GuerrillaTier, Is.Zero);
            int sounds = SoundCount, revision = Feedback.Revision;

            Click(newcomer);
            Assert.That(controller.Battle.ReunionTier, Is.EqualTo(6));
            Assert.That(controller.Battle.GuerrillaTier, Is.EqualTo(2));
            Assert.That(Feedback.Revision, Is.EqualTo(revision + 2));
            Assert.That(Feedback.PendingCount, Is.EqualTo(2));
            var first = Feedback.Active;
            Assert.That(first.Activations.Count, Is.EqualTo(1), "A shared card never receives two simultaneous emblems.");
            Assert.That(first.Activations[0].Name, Is.EqualTo("整合运动"));
            Assert.That(SynergyBadgePresentation.GradeFor(first.Activations[0].Name, first.Activations[0].Tier), Is.EqualTo(SynergyVisualGrade.Gold));
            CollectionAssert.AreEquivalent(reunion, first.Activations[0].Members);
            Assert.That(first.Activations[0].ContainsMember(unrelated), Is.False);
            controller.Tick(.12f);
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            controller.Tick(first.Duration - first.Age + .01f);
            Assert.That(Feedback.Active, Is.Null);
            Assert.That(Feedback.Pending, Is.True);
            Assert.That(Source == null || !Source.isPlaying, Is.True);
            float remainingGap = Feedback.SecondsToTransition;
            Assert.That(remainingGap, Is.GreaterThan(.2f).And.LessThanOrEqualTo(SynergyActivationFeedback.Gap));

            SetField("loopMenuOpen", true);controller.Tick(3);
            Assert.That(Feedback.Active, Is.Null);
            Assert.That(Feedback.SecondsToTransition, Is.EqualTo(remainingGap).Within(.0001f));
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            SetField("loopMenuOpen", false);controller.Tick(remainingGap * .5f);
            Assert.That(Feedback.Active, Is.Null, "Half of the remaining clear interval cannot start the next emblem.");
            Assert.That(SoundCount, Is.EqualTo(sounds + 1));
            controller.Tick(Feedback.SecondsToTransition + .12f);
            var second = Feedback.Active;
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Sequence, Is.GreaterThan(first.Sequence));
            Assert.That(second.Activations.Count, Is.EqualTo(1));
            Assert.That(second.Activations[0].Name, Is.EqualTo("游击队"));
            Assert.That(SynergyBadgePresentation.GradeFor(second.Activations[0].Name, second.Activations[0].Tier), Is.EqualTo(SynergyVisualGrade.Bronze));
            CollectionAssert.AreEquivalent(new[] { "CARD_M15", "CARD_M16" }, second.Activations[0].Members);
            Assert.That(SoundCount, Is.EqualTo(sounds + 2));
            controller.Tick(second.Duration - second.Age + .01f);
            Assert.That(Feedback.Pending, Is.False);

            // A separate legal pair crosses two bronze thresholds together. Its current
            // summary order is not a second source of ordering rules in the test.
            string[] pair = { "CARD_M03", "CARD_M06", unrelated };
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster", (object)pair);
            Click(pair[0]);Invoke("ClearSynergyActivationFeedback", false);controller.Tick(.001f);
            int tiedSounds = SoundCount;
            Click(pair[0]);
            Assert.That(Feedback.PendingCount, Is.EqualTo(2));
            Assert.That(Feedback.Active.Activations.Single().Name, Is.EqualTo("整合运动"));
            Assert.That(Field<string>("inspectedSynergy"), Is.EqualTo("感染生物"));
            // Complete the real infection prompt as an explicit fixture; OS clicks are Player evidence.
            SetField("inspectedSynergy", null);
            var tiedFirst = Feedback.Active;controller.Tick(.12f);
            Assert.That(SoundCount, Is.EqualTo(tiedSounds + 1));
            controller.Tick(tiedFirst.Duration - tiedFirst.Age + SynergyActivationFeedback.Gap + .12f);
            Assert.That(Feedback.Active.Activations.Count, Is.EqualTo(1));
            Assert.That(Feedback.Active.Activations[0].Name, Is.EqualTo("感染生物"));
            Assert.That(Feedback.Active.Sequence, Is.GreaterThan(tiedFirst.Sequence));
            Assert.That(SoundCount, Is.EqualTo(tiedSounds + 2));
            controller.Shop.Validate();
        }
    }
}
