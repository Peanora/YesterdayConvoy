using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        internal readonly struct FormationAudioCue
        {
            internal readonly double At;
            internal readonly int ActorId;
            internal readonly AudioClip Clip;
            internal readonly float Gain, Pitch;
            internal readonly int Priority;
            // Original cue table: maxSoundAllowed=1, popOldest=true.
            internal bool ReplacesEarlier => Clip != null && Clip.name == "e_skill_ftrtalatk";

            internal FormationAudioCue(double at, int actorId, AudioClip clip, float gain, float pitch, int priority)
            {
                At = at;
                ActorId = actorId;
                Clip = clip;
                Gain = gain;
                Pitch = pitch;
                Priority = priority;
            }
        }

        [Serializable]
        sealed class FormationOriginalCue
        {
            public string artId, trigger, resource, animation;
            public int abilityIndex = -1, hitIndex = -1, nativePhase = -1, talulahCost;
            public float delay, minPitch = 1, maxPitch = 1, minVolume = 1, maxVolume = 1;
        }

        [Serializable]
        sealed class FormationOriginalCueBank
        {
            public int version;
            public FormationOriginalCue[] cues;
        }

        const int FormationVoiceLimit = 8;
        const float FormationAudioVolume = .36f;
        readonly List<FormationAudioCue> formationAudioFrame = new List<FormationAudioCue>(24);
        readonly Dictionary<string, List<FormationOriginalCue>> formationOriginalCues = new();
        readonly Dictionary<string, AudioClip> formationOriginalClips = new();
        readonly AudioSource[] formationCombatVoices = new AudioSource[FormationVoiceLimit];
        readonly int[] formationCombatVoicePriorities = new int[FormationVoiceLimit];
        bool formationCueBankLoaded, formationAudioPaused;
        FormationBattleModel formationAudioModel;
        float formationMaxCueDelay;

        void WarmFormationCombatAudio(string artId)
        {
            if (!formationCueBankLoaded)
            {
                formationCueBankLoaded = true;
                var source = Resources.Load<TextAsset>("FormationOriginal/cues");
                var bank = source != null ? JsonUtility.FromJson<FormationOriginalCueBank>(source.text) : null;
                if (bank?.version == 1 && bank.cues != null)
                    foreach (var cue in bank.cues)
                    {
                        if (string.IsNullOrEmpty(cue.artId) || string.IsNullOrEmpty(cue.resource)) continue;
                        if (!formationOriginalCues.TryGetValue(cue.artId, out var entries))
                            formationOriginalCues.Add(cue.artId, entries = new List<FormationOriginalCue>());
                        entries.Add(cue);
                        formationMaxCueDelay = Mathf.Max(formationMaxCueDelay, cue.delay);
                    }
            }
            if (formationOriginalCues.TryGetValue(artId, out var rows))
                foreach (var cue in rows)
                    if (!formationOriginalClips.ContainsKey(cue.resource))
                        formationOriginalClips.Add(cue.resource, Resources.Load<AudioClip>(cue.resource));
        }

        FormationOriginalCue FindFormationOriginalCue(string artId, string trigger, string animation, int hitIndex, int nativePhase = 0)
        {
            if (string.IsNullOrEmpty(artId)) return null;
            WarmFormationCombatAudio(artId);
            if (!formationOriginalCues.TryGetValue(artId, out var rows)) return null;
            int specificity = -1;
            FormationOriginalCue best = null;
            foreach (var cue in rows)
            {
                if (cue.trigger != trigger || formationOriginalClips[cue.resource] == null) continue;
                bool preciseMotion = !string.IsNullOrEmpty(cue.animation);
                bool preciseHit = cue.hitIndex >= 0, preciseVariant = cue.talulahCost > 0;
                if (preciseMotion && !string.Equals(cue.animation, animation, StringComparison.OrdinalIgnoreCase) ||
                    cue.abilityIndex >= 0 && !preciseMotion || cue.nativePhase >= 0 && cue.nativePhase != nativePhase ||
                    preciseHit && cue.hitIndex != hitIndex || preciseVariant && Campaign?.TalulahCost != cue.talulahCost) continue;
                int score = (preciseMotion ? 4 : 0) + (preciseHit ? 2 : 0) + (preciseVariant ? 1 : 0) + (cue.nativePhase >= 0 ? 8 : 0);
                if (score <= specificity) continue;
                best = cue;
                specificity = score;
            }
            return best;
        }

        static bool FormationAudioInWindow(double at, double after, double through) => at > after && at <= through;

        static void AddFormationAudio(List<FormationAudioCue> destination, FormationAudioCue cue)
        {
            if (cue.Clip == null) return;
            for (int i = 0; i < destination.Count; i++)
            {
                var previous = destination[i];
                if (previous.ActorId != cue.ActorId || previous.Clip != cue.Clip || Math.Abs(previous.At - cue.At) > .0001) continue;
                // One original contact sound per simultaneous area hit, independent of target count.
                if (cue.Priority > previous.Priority) destination[i] = cue;
                return;
            }
            destination.Add(cue);
        }

        void AddFormationOriginalAudio(List<FormationAudioCue> destination, FormationOriginalCue cue,
            double at, double after, double through, int actorId, int sequence, float gain, int priority)
        {
            if (cue == null) return;
            at += Math.Max(0, cue.delay);
            if (!FormationAudioInWindow(at, after, through)) return;
            uint salt = unchecked((uint)(sequence * 1664525 + actorId * 1013904223));
            salt ^= salt >> 16;
            float pitch = Mathf.Lerp(cue.minPitch, cue.maxPitch, (salt & 65535) / 65535f);
            float volume = Mathf.Lerp(cue.minVolume, cue.maxVolume, (salt >> 16) / 65535f);
            AddFormationAudio(destination, new FormationAudioCue(at, actorId, formationOriginalClips[cue.resource],
                Mathf.Clamp01(gain * volume), Mathf.Clamp(pitch, .2f, 3f), priority));
        }

        AudioClip FormationFallbackContactClip(ImpactStyle style)
        {
            if (impactSounds.TryGetValue(style, out var clip)) return clip;
            var samples = FormationImpactSamples(style);
            clip = AudioClip.Create("Formation " + style, samples.Length, 1, ImpactSampleRate, false);
            clip.SetData(samples, 0);
            impactSounds.Add(style, clip);
            return clip;
        }

        // Both the live mixer and the fixed-step video tool consume the same real
        // presentation actions and resolved contacts, including multihit timing.
        internal void CollectFormationAudio(double after, double through, List<FormationAudioCue> destination, bool includeSnowBoss = false)
        {
            if (Model == null || Model.Phase == FormationPhase.Preparation || after >= through) return;
            if (includeSnowBoss) CollectShowcaseSnowBossAudio(after, through, destination);
            double earliestAnchor = after - formationMaxCueDelay;
            foreach (var action in Model.PresentationActions)
            {
                if (action.At > through || Math.Max(action.LaunchedAt, action.At) < earliestAnchor) continue;
                var actor = FormationFeedbackActor(action.ActorId);
                if (actor == null || actor is FormationDefender { IsSnowBoss: true }) continue;
                if (!action.ProjectileOnly)
                    AddFormationOriginalAudio(destination, FindFormationOriginalCue(actor.ArtId,
                        action.Skill ? "skillStart" : "attackStart", action.Animation, -1, action.NativePhase),
                        action.At, after, through, actor.Id, action.Sequence, .29f, action.Skill ? 3 : 1);
                if (action.Cancelled || action.Skill && !action.ProjectileOnly && action.LaunchedAt == action.At)
                    continue;
                AddFormationOriginalAudio(destination, FindFormationOriginalCue(actor.ArtId,
                    action.Skill ? "skillRelease" : "attackRelease", action.Animation, action.ProjectileOnly ? action.ProjectileIndex : -1, action.NativePhase),
                    action.LaunchedAt, after, through, actor.Id, action.Sequence, .29f, action.Skill ? 3 : 2);
            }

            foreach (var hit in Model.Hits)
            {
                if (hit.At > through || hit.At < earliestAnchor || !IsAudibleFormationHit(hit)) continue;
                var actor = FormationFeedbackActor(hit.FromId);
                if (actor is FormationDefender { IsSnowBoss: true } &&
                    (includeSnowBoss ? SnowBossContactCue(hit) is string key && snowBossEffectClips.ContainsKey(key) : HasSnowBossImpactAudio(hit))) continue;
                string artId = hit.SourceArtId ?? actor?.ArtId;
                var cue = hit.Amount > 0 && !hit.Secondary
                    ? FindFormationOriginalCue(artId, hit.Skill ? "skillHit" : "attackHit", hit.Action?.Animation, hit.ImpactIndex, hit.Action?.NativePhase ?? 0)
                    : null;
                int priority = hit.Absorbed || hit.Killed || hit.ShowDamageNumber ? 4 : hit.Skill ? 3 : 2;
                if (cue != null)
                    AddFormationOriginalAudio(destination, cue, hit.At, after, through, hit.FromId,
                        hit.Action?.Sequence ?? (int)(hit.At * 40), FormationAudioVolume * (hit.Heal ? .58f : .91f), priority);
                else if (FormationAudioInWindow(hit.At, after, through))
                    AddFormationAudio(destination, new FormationAudioCue(hit.At, hit.FromId, FormationFallbackContactClip(FormationHitStyle(hit)),
                        FormationAudioVolume * (hit.Heal ? .46f : hit.Skill ? .78f : .61f), 1, priority));
                if (hit.Killed)
                {
                    var victim = FormationFeedbackActor(hit.ToId);
                    AddFormationOriginalAudio(destination, FindFormationOriginalCue(victim?.ArtId, "death", null, -1),
                        hit.At, after, through, hit.ToId, hit.Action?.Sequence ?? (int)(hit.At * 40), .28f, 3);
                }
            }
            destination.Sort((first, second) =>
            {
                int time = first.At.CompareTo(second.At);
                return time != 0 ? time : second.Priority.CompareTo(first.Priority);
            });
        }

        void UpdateFormationSounds()
        {
            bool preparing = Model.Phase == FormationPhase.Preparation;
            if (formationAudioModel != Model || preparing || lastSoundAt > Model.Clock)
            {
                if (formationAudioModel != null)
                    foreach (var source in formationCombatVoices) if (source != null) source.Stop();
                formationAudioModel = preparing ? null : Model;
                formationAudioPaused = false;
                lastSoundAt = -10;
                if (preparing) return;
            }
            if (formationAudioPaused != Model.Paused)
            {
                formationAudioPaused = Model.Paused;
                foreach (var source in formationCombatVoices)
                {
                    if (source == null) continue;
                    if (formationAudioPaused) source.Pause(); else source.UnPause();
                }
            }
            foreach (var source in formationCombatVoices) if (source != null) source.mute = !soundEnabled;
            if (Model.Paused) return;
            double now = Model.Clock;
            if (soundEnabled)
            {
                formationAudioFrame.Clear();
                CollectFormationAudio(Math.Max(lastSoundAt, now - .22), now, formationAudioFrame);
                foreach (var cue in formationAudioFrame) PlayFormationAudioCue(cue);
            }
            lastSoundAt = now;
        }

        void PlayFormationAudioCue(FormationAudioCue cue)
        {
            int chosen = cue.ReplacesEarlier ? Array.FindIndex(formationCombatVoices,
                voice => voice != null && voice.isPlaying && voice.clip == cue.Clip) : -1;
            int lowest = -1;
            for (int i = 0; chosen < 0 && i < formationCombatVoices.Length; i++)
            {
                var voice = formationCombatVoices[i];
                if (voice == null || !voice.isPlaying) { chosen = i; break; }
                if (lowest < 0 || formationCombatVoicePriorities[i] < formationCombatVoicePriorities[lowest])
                    lowest = i;
            }
            if (chosen < 0)
            {
                if (lowest < 0 || formationCombatVoicePriorities[lowest] >= cue.Priority) return;
                chosen = lowest;
            }
            var source = formationCombatVoices[chosen];
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
                formationCombatVoices[chosen] = source;
            }
            source.Stop();
            source.clip = cue.Clip;
            source.volume = cue.Gain;
            source.pitch = cue.Pitch;
            source.mute = !soundEnabled;
            formationCombatVoicePriorities[chosen] = cue.Priority;
            source.Play();
        }
    }
}
