using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly FormationCampaignRules.DataRecord[] FormationTraitRules =
            FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles).ToArray();
        static readonly Dictionary<string, FormationCampaignRules.DataRecord> FormationTraitsByName =
            FormationTraitRules.ToDictionary(rule => rule.Name, StringComparer.Ordinal);
        static readonly Dictionary<string, int> FormationTraitOrder = FormationTraitRules
            .Select((rule, index) => (rule.Name, index)).ToDictionary(entry => entry.Name, entry => entry.index, StringComparer.Ordinal);
        readonly SynergyActivationFeedback formationTraitFeedback =
            new SynergyActivationFeedback(FormationTraitGrade, FormationTraitOrder);
        readonly List<(int instance, string identity)> observedTraitInputs = new List<(int, string)>();
        readonly List<(int instance, string identity)> currentTraitInputs = new List<(int, string)>();
        readonly List<ShopTrialModel.Piece> traitLineup = new List<ShopTrialModel.Piece>();
        FormationCampaignRun traitFeedbackRun;
        AudioSource formationTraitAudio;
        int formationTraitSoundSequence = -1;
        internal SynergyActivationFeedback.Batch ActiveFormationTraitPulse => formationTraitFeedback.Active;

        static SynergyVisualGrade FormationTraitGrade(string name, int tier) =>
            FormationTraitView.GradeFor(FormationTraitsByName.GetValueOrDefault(name), tier);

        bool FormationTraitAnimationBlocked => CampaignInteractionBlocked || formationMenuOpen || shopOddsVisible ||
            ViewingDefenders || MarketVisible || ShopPanelTransitioning || rewardHistoryOpen;

        void TickFormationTraitFeedback(float delta)
        {
            if (!ReferenceEquals(traitFeedbackRun, Campaign))
            {
                traitFeedbackRun = Campaign;
                observedTraitInputs.Clear();
                formationTraitFeedback.Clear();
            }
            if (Campaign == null || Model.Phase != FormationPhase.Preparation)
            {
                formationTraitFeedback.Clear(false);
                if (formationTraitAudio != null) formationTraitAudio.Stop();
                return;
            }

            traitLineup.Clear();
            currentTraitInputs.Clear();
            foreach (var piece in Shop.Bench)
            {
                if (!Shop.IsLineupInstance(piece.Instance)) continue;
                traitLineup.Add(piece);
                currentTraitInputs.Add((piece.Instance, piece.Id));
            }
            foreach (var item in Campaign.AllEquipment)
                if (item.Owner != 0 && Shop.IsLineupInstance(item.Owner) && item.Key.StartsWith("徽章:", StringComparison.Ordinal))
                    currentTraitInputs.Add((item.Owner, item.Key));

            if (!currentTraitInputs.SequenceEqual(observedTraitInputs))
            {
                var rows = FormationTraitRules.Select(rule =>
                {
                    string[] members = traitLineup.Where(piece => Campaign.HasTrait(piece, rule.Id))
                        .Select(piece => piece.Id).Distinct().ToArray();
                    int tier = rule.Levels.Where(level => level <= members.Length).DefaultIfEmpty(0).Max();
                    return new SynergySummary { Name = rule.Name, Tier = tier, Members = members };
                }).ToArray();
                formationTraitFeedback.Observe(rows);
                observedTraitInputs.Clear();
                observedTraitInputs.AddRange(currentTraitInputs);
            }

            var batch = formationTraitFeedback.Active;
            if (batch?.Sequence != formationTraitSoundSequence && formationTraitAudio != null)
                formationTraitAudio.Stop();
            if (batch != null && FormationTraitAnimationBlocked)
            {
                if (formationTraitAudio != null && formationTraitAudio.isPlaying) formationTraitAudio.Pause();
            }
            else if (batch != null && batch.Sequence != formationTraitSoundSequence)
            {
                formationTraitSoundSequence = batch.Sequence;
                var clip = Resources.Load<AudioClip>("SynergyActivation/bond-original");
                if (soundEnabled && clip != null)
                {
                    if (formationTraitAudio == null)
                    {
                        formationTraitAudio = gameObject.AddComponent<AudioSource>();
                        formationTraitAudio.playOnAwake = false;
                        formationTraitAudio.spatialBlend = 0;
                        formationTraitAudio.volume = .32f;
                    }
                    formationTraitAudio.Stop();
                    formationTraitAudio.clip = clip;
                    formationTraitAudio.Play();
                }
            }
            else if (batch != null && formationTraitAudio != null && soundEnabled) formationTraitAudio.UnPause();

            if (formationTraitAudio != null) formationTraitAudio.mute = !soundEnabled;
            if (!FormationTraitAnimationBlocked)
                formationTraitFeedback.Advance(Mathf.Min(delta, .1f));
        }

        void DrawFormationTraitActivation()
        {
            var batch = formationTraitFeedback.Active;
            if (Campaign == null || Model.Phase != FormationPhase.Preparation || FormationTraitAnimationBlocked ||
                batch == null || Event.current.type != EventType.Repaint) return;
            var pulse = batch.Activations[0];
            var grade = FormationTraitGrade(pulse.Name, pulse.Tier);
            foreach (var actor in Model.Units)
            {
                if (!actor.Enabled || !actor.Alive || !FormationActorVisibleInView(actor)) continue;
                var piece = traitLineup.Find(member => member.Instance == actor.Instance);
                if (piece == null || !pulse.ContainsMember(piece.Id)) continue;
                var body = FormationActorScreenRect(actor);
                // The old pulse fit a card. Anchor it at the current feet, and cap
                // the box so a large animation frame cannot cover adjacent rows.
                var foot = TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(FormationDisplayPoint(actor)));
                float width = Mathf.Clamp(body.width, 60, 112);
                float height = Mathf.Clamp(body.height, 68, 120);
                // The original glow composes rotation matrices in canvas coordinates.
                // Keep that coordinate space so rotated segments remain around the body.
                var bounds = new Rect(
                    Mathf.Clamp(foot.x - width * .5f, BoardInputRect.xMin, BoardInputRect.xMax - width),
                    Mathf.Clamp(foot.y - height + 6, BoardInputRect.yMin + 36, BoardInputRect.yMax - height),
                    width, height);
                SynergyActivationPresentation.DrawCard(bounds, piece.Id, batch, grade);
            }
        }
    }
}
