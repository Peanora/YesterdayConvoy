using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const string SnowBossAudioRoot = "SnowBoss/";
        static readonly string[] SnowBossEffectKeys =
        {
            "p_skill_swlprdbl_h", "p_atk_swlprdbl_h", "p_atk_swlprdblrs_h", "p_imp_swlprdbl_n",
            "p_skill_swlprdbl_s", "p_atk_swlprdbl_s", "p_imp_swlprdbl_s", "p_skill_swlprdbl", "p_imp_swcvr", "p_imp_swcvr_h"
        };
        readonly Dictionary<string, AudioClip> snowBossEffectClips = new Dictionary<string, AudioClip>();
        FormationBattleModel snowBossAudioBattle;
        FormationDefender snowBossAudioGuard;
        AudioSource snowBossEffects;
        double lastSnowBossEffectAt;
        int snowBossPreviousSnowMass;
        bool snowBossEffectsLoaded, snowBossAudioPaused;

        void WarmSnowBossAudio()
        {
            if (snowBossEffectsLoaded) return;
            snowBossEffectsLoaded = true;
            foreach (string key in SnowBossEffectKeys)
            {
                var clip = Resources.Load<AudioClip>(SnowBossAudioRoot + key);
                if (clip != null) snowBossEffectClips.Add(key, clip);
            }
        }

        void UpdateSnowBossAudio()
        {
            if (!CombatViewActive)
            {
                if (snowBossAudioBattle != null) StopSnowBossAudio();
                return;
            }

            if (snowBossAudioBattle != Model)
            {
                StopSnowBossAudio();
                snowBossAudioBattle = Model;
                snowBossAudioGuard = Model.Defenders.FirstOrDefault(guard => guard.IsSnowBoss);
                if (snowBossAudioGuard == null) return;

                WarmSnowBossAudio();
                if (snowBossEffectClips.Count > 0)
                    snowBossEffects ??= CreateBossAudioSource(loop: false, volume: .31f);
            }

            if (snowBossAudioGuard == null || snowBossEffects == null) return;
            snowBossEffects.mute = !soundEnabled;
            if (Model.Paused != snowBossAudioPaused)
            {
                snowBossAudioPaused = Model.Paused;
                if (snowBossAudioPaused) snowBossEffects.Pause();
                else snowBossEffects.UnPause();
            }
            if (!snowBossAudioPaused) PlaySnowBossEffects();
        }

        void PlaySnowBossEffects()
        {
            double now = Model.Clock;
            foreach (var action in Model.PresentationActions)
            {
                if (action.ActorId != snowBossAudioGuard.Id || action.Cancelled) continue;
                if (action.At > lastSnowBossEffectAt && action.At <= now)
                {
                    PlaySnowBossEffect(action.Animation switch
                    {
                        "skill_2_begin" => "p_skill_swlprdbl_h",
                        "skill_2_attack" => "p_atk_swlprdbl_h",
                        "skill_begin" => "p_skill_swlprdbl_s",
                        "skill_attack" or "skill_attack_b" or "skill_attack_c" => "p_atk_swlprdbl_s",
                        _ => null
                    });
                }
                if (action.ContactAt <= lastSnowBossEffectAt || action.ContactAt > now) continue;
                if (action.Animation == "skill_2_attack")
                {
                    PlaySnowBossEffect("p_atk_swlprdblrs_h");
                    if (Model.Hits.Any(hit => hit.Action == action)) PlaySnowBossEffect("p_imp_swlprdbl_n");
                }
                else if ((action.Animation is "skill_attack" or "skill_attack_b" or "skill_attack_c") &&
                    Model.Hits.Any(hit => hit.Action == action))
                    PlaySnowBossEffect("p_imp_swlprdbl_s");
            }
            if (snowBossAudioGuard.SnowBossTransitionStartedAt > 0 &&
                snowBossAudioGuard.SnowBossTransitionStartedAt > lastSnowBossEffectAt)
                PlaySnowBossEffect("p_skill_swlprdbl");
            if (Model.Hits.Any(hit => hit.FromId == snowBossAudioGuard.Id &&
                hit.At > lastSnowBossEffectAt && hit.Action?.Animation == "SnowBossS2Tick"))
                PlaySnowBossEffect("p_imp_swcvr_h");
            int snowMass = Model.SnowBossSnow(snowBossAudioGuard).Values.Sum();
            if (snowMass > snowBossPreviousSnowMass) PlaySnowBossEffect("p_imp_swcvr");
            snowBossPreviousSnowMass = snowMass;
            lastSnowBossEffectAt = now;
        }

        void PlaySnowBossEffect(string key)
        {
            if (key != null && snowBossEffectClips.TryGetValue(key, out var clip)) snowBossEffects.PlayOneShot(clip);
        }

        static string SnowBossContactCue(FormationHit hit) => hit.Action?.Animation switch
        {
            "skill_2_attack" => "p_imp_swlprdbl_n",
            "skill_attack" or "skill_attack_b" or "skill_attack_c" => "p_imp_swlprdbl_s",
            "SnowBossS2Tick" => "p_imp_swcvr_h",
            _ => null
        };

        bool HasSnowBossImpactAudio(FormationHit hit)
        {
            if (snowBossAudioBattle != Model || hit.FromId != snowBossAudioGuard?.Id) return false;
            string key = SnowBossContactCue(hit);
            return key != null && snowBossEffectClips.ContainsKey(key);
        }

        void CollectShowcaseSnowBossAudio(double after, double through, List<FormationAudioCue> destination)
        {
            var guard = Model.Defenders.FirstOrDefault(actor => actor.IsSnowBoss);
            if (guard == null) return;
            WarmSnowBossAudio();
            foreach (var action in Model.PresentationActions)
            {
                if (action.ActorId != guard.Id || action.Cancelled) continue;
                Add(action.At, action.Animation switch
                {
                    "skill_2_begin" => "p_skill_swlprdbl_h",
                    "skill_2_attack" => "p_atk_swlprdbl_h",
                    "skill_begin" => "p_skill_swlprdbl_s",
                    "skill_attack" or "skill_attack_b" or "skill_attack_c" => "p_atk_swlprdbl_s",
                    _ => null
                }, 3);
                if (action.Animation == "skill_2_attack")
                    Add(action.ContactAt, "p_atk_swlprdblrs_h", 3);
            }
            foreach (var hit in Model.Hits)
                if (hit.FromId == guard.Id) Add(hit.At, SnowBossContactCue(hit), 4);
            if (guard.SnowBossTransitionStartedAt > 0)
                Add(guard.SnowBossTransitionStartedAt, "p_skill_swlprdbl", 4);

            void Add(double at, string key, int priority)
            {
                if (key != null && FormationAudioInWindow(at, after, through) && snowBossEffectClips.TryGetValue(key, out var clip))
                    AddFormationAudio(destination, new FormationAudioCue(at, guard.Id, clip, .31f, 1, priority));
            }
        }

        void StopSnowBossAudio()
        {
            if (snowBossEffects != null) snowBossEffects.Stop();
            snowBossAudioBattle = null;
            snowBossAudioGuard = null;
            snowBossAudioPaused = false;
            lastSnowBossEffectAt = -1;
            snowBossPreviousSnowMass = 0;
        }

    }
}
