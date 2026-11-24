using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignEvidence
    {
        const string MakotoBossClipsFlag = "-formationMakotoBossClips";
        const string MakotoOpeningMusic = "master-of-shadow-reload";
        const string MakotoLaterMusic = "its-going-down-now";
        static bool MakotoBossRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-formationMakotoBossSmokeTest") >= 0;

        [Serializable]
        sealed class MakotoBossReplay
        {
            public double[] phaseAt = new double[3], transitionAt = new double[2], firstAttackAt = new double[2],
                firstContactAt = new double[2], firstDamageAt = new double[2];
            public float[] phaseMaximum = new float[3], firstDamage = new float[2];
            public int[] firstDamageType = new int[2];
            public string[] firstAttack = new string[2], renderedAttackSprites = new string[2];
            public double guardChangeStartAt, guardChangeEndAt, guardFormAt, firstHealAt, clearedAt, endAt;
            public float firstHeal, totalHealing, thanatosUnslowedRate;
            public double thanatosUnslowedContactSeconds, thanatosShortestInterval;
            public string scoutSprite, outcome;
            public List<string> musicClips = new List<string>();
            public List<double> musicStartedAt = new List<double>();
            public List<int> musicStartedPhase = new List<int>();
            public List<long> musicStartedUtcTicks = new List<long>();
            public int musicPausedSample, musicBeforeGuardSample, musicAfterGuardSample;
            public long audioCaptureUtcTicks;
            public double audioCaptureSeconds;
        }

        IEnumerator VerifyMakotoBossTrial()
        {
            var args = Environment.GetCommandLineArgs();
            keepSnowBossCaptureRunning = Array.IndexOf(args, "-formationMakotoBossCaptureInBackground") >= 0;
            result.scope = "Rendered Windows Player at normal speed. Boss trial selection, combat, and retry use production controller " +
                "commands and the supplied preset. No actor stats, damage, or battle outcomes are injected.";
            controller.SelectMakotoBossTrial();
            var run = controller.Campaign;
            var model = controller.Model;
            result.node = run.CurrentNode.Id;
            Assert(run.IsMakotoBossTrial && result.node == FormationCampaignRules.MakotoBossNodeId &&
                model.EnabledCount == 8 && !run.HasBlockingChoice, "The standalone trial opens with its editable eight-unit preset");
            var guard = model.Defenders.Single();
            Assert(guard.IsMakotoBoss && guard.MakotoBossPhase == 1 && model.BossAlive &&
                model.Terrain.CanPlaceDefender(guard.Position), "The sole Defender starts on a legal tile in phase one");
            var replay = result.makotoBoss = new MakotoBossReplay();

            yield return ReadyArt();
            controller.SetFormationView(true);
            yield return WaitForScoutingView(true);
            replay.scoutSprite = MakotoBossBody(guard)?.sprite.name;
            Assert(replay.scoutSprite?.StartsWith(guard.ArtId + "/", StringComparison.Ordinal) == true ||
                replay.scoutSprite?.StartsWith(guard.ArtId + "B/", StringComparison.Ordinal) == true,
                "Scouting renders a sprite from the Makoto skin catalog");
            yield return Frame("01-makoto-scout");
            controller.SetFormationView(false);
            yield return WaitForScoutingView(false);

            FormationSnowBossAudioEvidence audioCapture = null;
            if (Array.IndexOf(args, "-formationMakotoBossAudioEvidence") >= 0)
            {
                var listener = FindAnyObjectByType<AudioListener>();
                Assert(listener != null, "The Player has an active audio listener to capture");
                audioCapture = listener.gameObject.AddComponent<FormationSnowBossAudioEvidence>();
                audioCapture.Begin();
            }

            controller.StartBattle();
            Assert(model.Phase == FormationPhase.Fighting && model.Speed == 1, "The trial starts with the ordinary battle clock");
            guard = model.Defenders.Single();
            yield return ObserveMakotoUntil(() => controller.BossPhaseMusic is { isPlaying: true } music &&
                music.clip?.name == MakotoOpeningMusic, "Master of Shadow -Reload- starts with phase one", 2);
            var bossMusic = controller.BossPhaseMusic;
            Assert(model.Clock <= .5 && bossMusic.loop && !bossMusic.mute &&
                controller.BossPhaseVoice?.isPlaying != true && controller.BossPhaseVoiceStarts.Count == 0,
                "The opening plays on the shared loop source without a dialogue line");
            StartCoroutine(RecordMakotoMusic(model, guard, bossMusic, replay));
            if (Array.IndexOf(args, "-formationMakotoBossAudioMuteInput") >= 0)
            {
                var soundButton = new Vector2(ApprovedUi.Menu.center.x, ApprovedUi.Menu.y + 59);
                yield return ClickStrategyUi(new Vector2(1243, 30));
                yield return ClickStrategyUi(soundButton);
                Assert(bossMusic.mute, "The system menu sound toggle mutes the active boss music");
                yield return ClickStrategyUi(soundButton);
                Assert(!bossMusic.mute && bossMusic.isPlaying && bossMusic.clip?.name == MakotoOpeningMusic,
                    "The sound toggle brings back the same opening track");
                yield return ClickStrategyUi(new Vector2(1243, 30));
            }

            string[][] attackKeys =
            {
                new[] { "makoto_s3_human_attack_a", "makoto_s3_human_attack_b" },
                new[] { "makoto_s3_attack_a", "makoto_s3_attack_b" }
            };
            for (int phase = 1; phase <= 3; phase++)
            {
                int index = phase - 1;
                yield return ObserveMakotoUntil(() => guard.MakotoBossPhase == phase && !model.MakotoBossTransitionActive(guard),
                    "Normal combat enters phase " + phase);
                replay.phaseAt[index] = model.Clock;
                replay.phaseMaximum[index] = guard.MaxHealth;
                Assert(guard.Health > guard.MaxHealth * .85f, "A fresh health bar opens phase " + phase);
                if (phase == 2)
                {
                    yield return ObserveMakotoUntil(() => controller.BossPhaseMusic == bossMusic &&
                        bossMusic.isPlaying && bossMusic.clip?.name == MakotoLaterMusic,
                        "It's Going Down Now starts as phase two becomes active", 2);
                    Assert(model.Clock - replay.phaseAt[index] <= .5 && bossMusic.loop,
                        "The second loop begins within half a second of the phase opening");
                }
                if (phase == 3)
                {
                    replay.musicAfterGuardSample = bossMusic.timeSamples;
                    Assert(controller.BossPhaseMusic == bossMusic && bossMusic.isPlaying &&
                        bossMusic.clip?.name == MakotoLaterMusic && replay.musicBeforeGuardSample > 0 &&
                        replay.musicAfterGuardSample > replay.musicBeforeGuardSample,
                        "It's Going Down Now continues through the stage-three change without restarting");
                    break;
                }
                yield return ObserveMakotoAttack(guard, attackKeys[index], phase, replay);

                yield return ObserveMakotoUntil(() =>
                {
                    if (phase == 2) MeasureThanatosCadence(guard, attackKeys[index], replay);
                    return model.MakotoBossTransitionActive(guard);
                }, "Lethal damage closes phase " + phase);
                if (phase == 2)
                {
                    Assert(Math.Abs(replay.thanatosUnslowedRate - 4.4f) < 1e-4 &&
                        Math.Abs(replay.thanatosUnslowedContactSeconds - .8 / 4.4) < 1e-5,
                        "An unslowed Thanatos attack plays at 4.4× with the original contact offset");
                }
                replay.transitionAt[index] = model.Clock;
                Assert(model.BossAlive && model.RemainingDefenders == 1 && guard.Health == 0 &&
                    model.Phase == FormationPhase.Fighting &&
                    !model.Hits.Any(hit => hit.ToId == guard.Id && hit.Killed),
                    "The empty bar holds the gate closed until the next phase");
                if (phase == 1)
                {
                    Assert(bossMusic.clip?.name == MakotoOpeningMusic && bossMusic.isPlaying,
                        "Master of Shadow -Reload- remains active during the first empty health bar");
                    checkingSnowBossPause = true;
                    model.Paused = true;
                    double pausedAt = model.Clock;
                    yield return Frame("03-phase-one-transition-paused");
                    var pausedPose = MakotoBossBody(guard)?.sprite;
                    yield return ObserveMakotoUntil(() => !bossMusic.isPlaying, "The active track pauses", 2);
                    // Wait for the audio thread's current block to drain before measuring the paused playhead.
                    yield return new WaitForSecondsRealtime(.15f);
                    replay.musicPausedSample = bossMusic.timeSamples;
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert(replay.musicPausedSample > 0 && bossMusic.timeSamples == replay.musicPausedSample &&
                        bossMusic.clip?.name == MakotoOpeningMusic && !bossMusic.isPlaying,
                        "Pause holds the opening track and its sample position");
                    Assert(model.Clock == pausedAt && model.MakotoBossTransitionActive(guard) &&
                        MakotoBossBody(guard)?.sprite == pausedPose, "Pause freezes the transition clock and visible pose");
                    checkingSnowBossPause = false;
                    model.Paused = false;
                    yield return ObserveMakotoUntil(() => bossMusic.clip?.name == MakotoOpeningMusic &&
                        bossMusic.isPlaying && bossMusic.timeSamples > replay.musicPausedSample,
                        "Resuming the battle advances the paused opening track", 2);
                }
                else
                {
                    replay.musicBeforeGuardSample = bossMusic.timeSamples;
                    StartCoroutine(CaptureInterfaceClip("phase-three-form-change-and-guard", 112, MakotoBossClipsFlag));
                    yield return Frame("05-phase-two-transition");
                }
            }

            Assert(replay.phaseMaximum.SequenceEqual(new[] { 180000f, 240000f, 225000f }),
                "The three standalone bars use the increased life pool");
            FormationPresentationAction guardChangeBegin = null, guardChangeEnd = null;
            FormationHit heal = null;
            bool offensiveMotionInGuard = false, damageInGuard = false;
            void ObserveGuardForm()
            {
                guardChangeBegin ??= model.PresentationActions.FirstOrDefault(action => action.ActorId == guard.Id &&
                    action.Animation == "makoto_s3_change_begin" && !action.Cancelled);
                guardChangeEnd ??= model.PresentationActions.FirstOrDefault(action => action.ActorId == guard.Id &&
                    action.Animation == "makoto_s3_change_end" && !action.Cancelled);
                double guardStarted = guardChangeBegin?.At ?? replay.phaseAt[2];
                offensiveMotionInGuard |= model.PresentationActions.Any(action => action.ActorId == guard.Id &&
                    !action.Cancelled && action.At + 1e-5 >= guardStarted && action.Animation?.Contains("_attack") == true);
                damageInGuard |= model.Hits.Any(hit => hit.FromId == guard.Id && !hit.Heal &&
                    hit.Amount > 0 && hit.At + 1e-5 >= guardStarted);
                heal ??= model.Hits.FirstOrDefault(hit => hit.Heal && hit.Amount > 0 && hit.FromId == guard.Id &&
                    hit.ToId == guard.Id && hit.Action?.Animation == "makoto_s3_heal" && hit.At >= guardStarted);
            }
            yield return ObserveMakotoUntil(() =>
            {
                ObserveGuardForm();
                return model.MakotoBossRecovering(guard) && guardChangeBegin != null && guardChangeEnd != null;
            }, "Phase three opens with the Orpheus Kai guard transformation");
            replay.guardChangeStartAt = guardChangeBegin.At;
            replay.guardChangeEndAt = guardChangeEnd.At;
            replay.guardFormAt = model.Clock;
            Assert(replay.guardChangeEndAt >= replay.guardChangeStartAt, "The guard transformation plays in order");
            yield return Frame("07-phase-three-orpheus-kai");
            yield return ObserveMakotoUntil(() =>
            {
                ObserveGuardForm();
                return heal != null;
            }, "Orpheus Kai restores actual health on the battle clock");
            replay.firstHealAt = heal.At;
            replay.firstHeal = heal.Amount;
            Assert(heal.At + 1e-5 >= heal.Action.ContactAt, "The recovery pulse applies after its presentation contact");
            yield return Frame("08-final-form-heal");

            yield return ObserveMakotoUntil(() =>
            {
                ObserveGuardForm();
                return model.RemainingDefenders == 0;
            }, "The supplied formation clears all three bars");
            replay.clearedAt = model.Clock;
            Assert(!offensiveMotionInGuard && !damageInGuard, "Orpheus Kai holds guard form without offensive motions or damage");
            Assert(!model.BossAlive && model.Phase == FormationPhase.Exiting, "Final clear opens the exit without settling early");
            Assert(bossMusic.isPlaying && bossMusic.clip?.name == MakotoLaterMusic,
                "The phase-two track continues on the way to the blue gate");
            yield return Frame("09-third-bar-cleared");
            yield return ObserveMakotoUntil(() => model.Phase == FormationPhase.Won, "A surviving attacker reaches the blue gate");
            replay.endAt = model.Clock;
            replay.totalHealing = guard.HealingDone;
            Assert(replay.totalHealing > 0 && replay.totalHealing <= 6400.01f, "Guard healing remains capped at 6,400");
            replay.outcome = model.Phase.ToString();
            Assert(replay.clearedAt < replay.endAt && replay.endAt < model.LimitSeconds &&
                model.Units.Any(unit => unit.Enabled && unit.Alive && unit.Position.x >= FormationBattleModel.Width - 1 - .0001f),
                "Victory comes from a surviving original body arriving before the time limit");
            Assert(!bossMusic.isPlaying && controller.BossPhaseVoice?.isPlaying != true,
                "Boss audio stops at settlement");
            Assert(replay.musicClips.SequenceEqual(new[] { MakotoOpeningMusic, MakotoLaterMusic }) &&
                replay.musicStartedPhase.SequenceEqual(new[] { 1, 2 }) && replay.musicStartedAt[1] >= replay.transitionAt[0] &&
                controller.BossPhaseVoiceStarts.Count == 0,
                "The fight plays exactly the two tracks in order with one switch and no dialogue");
            if (audioCapture != null)
            {
                replay.audioCaptureUtcTicks = audioCapture.Save(Path.Combine(directory, "makoto-battle-mix.wav"), out replay.audioCaptureSeconds);
                Assert(replay.audioCaptureUtcTicks > 0 && replay.audioCaptureSeconds >= model.Clock * .85,
                    "The Player listener records the game mix throughout the fight");
            }
            result.battles++;
            yield return Frame("10-trial-victory");

            var roster = model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star)).ToArray();
            controller.ResetDemo();
            var reset = controller.Model.Defenders.Single();
            Assert(controller.Campaign == run && controller.Model != model && controller.Model.Phase == FormationPhase.Preparation &&
                reset.IsMakotoBoss && reset.MakotoBossPhase == 1 && reset.Health == replay.phaseMaximum[0] &&
                controller.Model.Clock == 0 && roster.SequenceEqual(controller.Model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star))),
                "Retry restores the first health bar and preserves the formation");
            yield return null;
            Assert(!bossMusic.isPlaying && controller.BossPhaseVoice?.isPlaying != true,
                "Retry preparation stays silent until the next start");
        }

        IEnumerator RecordMakotoMusic(FormationBattleModel model, FormationDefender guard, AudioSource music, MakotoBossReplay replay)
        {
            AudioClip last = null;
            while (controller.Model == model && controller.CombatViewActive)
            {
                if (music.isPlaying && music.clip != null && music.clip != last)
                {
                    last = music.clip;
                    replay.musicClips.Add(last.name);
                    replay.musicStartedAt.Add(model.Clock);
                    replay.musicStartedPhase.Add(guard.MakotoBossPhase);
                    replay.musicStartedUtcTicks.Add(DateTime.UtcNow.Ticks);
                }
                yield return null;
            }
        }

        IEnumerator ObserveMakotoAttack(FormationDefender guard, string[] keys, int phase, MakotoBossReplay replay)
        {
            var model = controller.Model;
            FormationPresentationAction move = null;
            yield return ObserveMakotoUntil(() =>
            {
                move = model.PresentationActions.FirstOrDefault(action => action.ActorId == guard.Id && !action.Cancelled &&
                    !action.ProjectileOnly && keys.Contains(action.Animation));
                return move != null;
            }, "Phase " + phase + " starts its signature action");
            StartCoroutine(CaptureInterfaceClip(phase == 1 ? "phase-one-human-swords" : "phase-two-thanatos-kai", 58, MakotoBossClipsFlag));
            FormationHit contact = null;
            yield return ObserveMakotoUntil(() =>
            {
                contact = model.Hits.FirstOrDefault(hit => hit.FromId == guard.Id && !hit.Heal && hit.Amount > 0 &&
                    hit.At >= move.At && keys.Contains(hit.Action?.Animation));
                return contact != null;
            }, "Phase " + phase + " applies damage from its signature action");
            int index = phase - 1;
            replay.firstAttack[index] = contact.Action.Animation;
            replay.firstAttackAt[index] = contact.Action.At;
            replay.firstContactAt[index] = contact.Action.ContactAt;
            replay.firstDamageAt[index] = contact.At;
            replay.firstDamage[index] = contact.Amount;
            replay.firstDamageType[index] = contact.DamageType;
            replay.renderedAttackSprites[index] = MakotoBossBody(guard)?.sprite.name;
            Assert(contact.At + 1e-5 >= contact.Action.ContactAt, "Phase " + phase + " damage occurs after contact");
            if (phase == 2)
                Assert(contact.Skill && contact.DamageType is 0 or 1,
                    "Thanatos Kai resolves its weak-point hit to physical or Arts damage");
            yield return Frame((2 * phase).ToString("00") + "-phase-" + phase + "-contact");
        }

        void MeasureThanatosCadence(FormationDefender guard, string[] keys, MakotoBossReplay replay)
        {
            FormationPresentationAction previous = null;
            foreach (var action in controller.Model.PresentationActions)
            {
                if (action.ActorId != guard.Id || action.Cancelled || action.ProjectileOnly || !keys.Contains(action.Animation)) continue;
                if (previous != null && Math.Abs(previous.Rate - 4.4f) < 1e-4)
                {
                    double interval = action.At - previous.At;
                    if (replay.thanatosShortestInterval <= 0 || interval < replay.thanatosShortestInterval)
                        replay.thanatosShortestInterval = interval;
                }
                if (replay.thanatosUnslowedRate == 0 && Math.Abs(action.Rate - 4.4f) < 1e-4)
                {
                    replay.thanatosUnslowedRate = action.Rate;
                    replay.thanatosUnslowedContactSeconds = action.ContactAt - action.At;
                }
                previous = action;
            }
        }

        IEnumerator ObserveMakotoUntil(Func<bool> reached, string description, double? timeoutSeconds = null)
        {
            var model = controller.Model;
            double deadline = Time.realtimeSinceStartupAsDouble + (timeoutSeconds ?? model.LimitSeconds + 10);
            while (!reached() && model.Phase != FormationPhase.Won && model.Phase != FormationPhase.Lost &&
                Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(reached(), $"{description} ({model.Phase}, {model.Clock:0.000}s)");
        }

        SpriteRenderer MakotoBossBody(FormationDefender guard) => controller.GetComponentsInChildren<SpriteRenderer>()
            .FirstOrDefault(renderer => renderer.enabled && renderer.sprite != null && renderer.name == guard.Name);
    }
}
