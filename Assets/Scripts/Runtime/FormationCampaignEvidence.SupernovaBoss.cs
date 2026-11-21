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
        const string SupernovaBossClipsFlag = "-formationSupernovaBossClips";
        static bool SupernovaBossRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-formationSupernovaBossSmokeTest") >= 0;

        [Serializable]
        sealed class SupernovaBossReplay
        {
            public double firstS2VolleyAt, firstS2HitAt, overloadAt, transitionAt, phaseTwoAt, firstS3ShellAt, firstS3HitAt, clearedAt, endAt;
            public float startingHealth, restoredHealth;
            public int s2RenderedBodyFrames, s3RenderedBodyFrames;
            public long audioCaptureUtcTicks;
            public double audioCaptureSeconds;
            public List<string> observedAudio = new List<string>();
            public string outcome;
        }

        IEnumerator VerifySupernovaBossTrial()
        {
            var args = Environment.GetCommandLineArgs();
            bool useMouse = Array.IndexOf(args, "-formationSupernovaBossMouse") >= 0;
            keepSnowBossCaptureRunning = Array.IndexOf(args, "-formationSupernovaBossCaptureInBackground") >= 0;
            result.scope = "Rendered Windows Player using the Supernova Boss trial with its supplied formation at ordinary game speed. " +
                (useMouse ? "Native mouse selects the trial and starts the fight. " : "Production controller commands select the trial and start the fight. ") +
                "No unit stats, position, damage or battle outcome are injected.";
            if (useMouse)
            {
                yield return ClickStrategyUi(new Vector2(1239, 38));
                yield return Frame("01-boss-menu");
                yield return ClickStrategyUi(new Vector2(ApprovedUi.Menu.center.x, 327));
            }
            else
                controller.SelectSupernovaBossTrial();

            var run = controller.Campaign;
            var model = controller.Model;
            result.node = run.CurrentNode.Id;
            Assert(run.IsSupernovaBossTrial && result.node == FormationCampaignRules.SupernovaBossNodeId &&
                model.EnabledCount == 8 && !run.HasBlockingChoice, "The standalone trial opens with an editable eight-unit preset");
            var guard = model.Defenders.Single();
            Assert(guard.ArtId == "D31" && guard.IsSupernovaBoss && !guard.IsSnowBoss && guard.SupernovaBossPhase == 1 &&
                model.Terrain.CanPlaceDefender(guard.Position) && model.BossAlive,
                "The sole Defender is Supernova Wis'adel on a legal tile with phase one ready");
            var replay = result.supernovaBoss = new SupernovaBossReplay { startingHealth = guard.MaxHealth };

            yield return ReadyArt();
            controller.SetFormationView(true);
            yield return WaitForScoutingView(true);
            var scoutBody = SupernovaBossBody(guard);
            Assert(IsSupernovaBodySprite(scoutBody?.sprite),
                "Scouting displays the prepared D31 skin body");
            yield return Frame("02-supernova-scout");
            controller.SetFormationView(false);
            yield return WaitForScoutingView(false);
            yield return Frame("03-preparation-roster");

            FormationSnowBossAudioEvidence audioCapture = null;
            if (Array.IndexOf(args, SupernovaBossClipsFlag) >= 0)
            {
                var listener = FindAnyObjectByType<AudioListener>();
                if (listener != null)
                {
                    audioCapture = listener.gameObject.AddComponent<FormationSnowBossAudioEvidence>();
                    audioCapture.Begin();
                }
            }
            if (useMouse) yield return ClickStrategyUi(FormationBattleController.FormationStartRect.center);
            else controller.StartBattle();
            Assert(model.Phase == FormationPhase.Fighting && model.Speed == 1, "Trial uses ordinary simulation speed");
            guard = model.Defenders.Single();

            yield return ObserveSupernovaAttack(guard, "supernova_s2_volley", "04-s2-rapid-fire", 61, false,
                new[] { "S2 muzzle", "S2 impact", "S2 impact ground" }, (move, hit, frames) =>
                {
                    replay.firstS2VolleyAt = move.At;
                    replay.firstS2HitAt = hit.At;
                    replay.s2RenderedBodyFrames = frames;
                });
            yield return ObserveSupernovaAttack(guard, "supernova_s2_overload", "05-s2-overload", 59, false,
                new[] { "S2 muzzle overload", "S2 impact" }, (move, _, _) => replay.overloadAt = move.At);

            yield return ObserveSupernovaUntil(() => model.SupernovaBossTransitionActive(guard), "First lethal damage starts the phase change");
            replay.transitionAt = model.Clock;
            Assert(model.BossAlive && guard.Health == 0 && guard.SupernovaBossPhase == 1 &&
                model.RemainingDefenders == 1 && model.Phase == FormationPhase.Fighting &&
                !model.Hits.Any(hit => hit.ToId == guard.Id && hit.Killed),
                "An empty first bar keeps the Boss alive and the exit closed");
            Assert(model.Units.Concat(model.Summons).All(unit => model.SnowBossFreezeRemaining(unit) == 0),
                "The phase change has no snow freeze on the attacking roster");

            checkingSnowBossPause = true;
            model.Paused = true;
            double pausedAt = model.Clock;
            var pausedPose = SupernovaBossBody(guard)?.sprite;
            yield return Frame("06-transition-paused");
            yield return new WaitForSecondsRealtime(.25f);
            Assert(model.Clock == pausedAt && model.SupernovaBossTransitionActive(guard) &&
                SupernovaBossBody(guard)?.sprite == pausedPose, "Pause freezes both phase change and the visible source pose");
            checkingSnowBossPause = false;
            model.Paused = false;

            yield return ObserveSupernovaUntil(() => guard.SupernovaBossPhase == 2 && !model.SupernovaBossTransitionActive(guard),
                "Boss reaches phase two naturally after resuming");
            replay.phaseTwoAt = model.Clock;
            replay.restoredHealth = guard.Health;
            Assert(guard.Health > guard.MaxHealth * .9f,
                "Phase two has its own health bar");
            yield return Frame("07-second-health-bar");

            yield return ObserveSupernovaAttack(guard, "supernova_s3_shell", "08-s3-shell-and-blast", 70, true,
                new[] { "S3 aim", "S3 muzzle", "S3 trail", "S3 impact", "S3 impact ground" }, (move, hit, frames) =>
                {
                    replay.firstS3ShellAt = move.At;
                    replay.firstS3HitAt = hit.At;
                    replay.s3RenderedBodyFrames = frames;
                });
            Assert(replay.firstS2VolleyAt < replay.firstS2HitAt && replay.firstS2HitAt < replay.overloadAt &&
                replay.overloadAt < replay.transitionAt && replay.transitionAt < replay.phaseTwoAt &&
                replay.phaseTwoAt <= replay.firstS3ShellAt && replay.firstS3ShellAt < replay.firstS3HitAt,
                "Combat and rendered effects follow the two-phase order");

            yield return ObserveSupernovaUntil(() => model.RemainingDefenders == 0, "The supplied formation defeats both phases in real combat");
            replay.clearedAt = model.Clock;
            Assert(model.Phase == FormationPhase.Exiting && !model.BossAlive, "Second clear opens the blue gate without immediately settling");
            yield return Frame("09-second-bar-cleared");
            yield return ObserveSupernovaUntil(() => model.Phase == FormationPhase.Won, "A surviving body enters the blue gate before timeout");
            replay.endAt = model.Clock;
            replay.outcome = model.Phase.ToString();
            if (audioCapture != null)
                replay.audioCaptureUtcTicks = audioCapture.Save(Path.Combine(directory, "battle-mix.wav"), out replay.audioCaptureSeconds);
            result.battles++;
            yield return Frame("10-trial-victory");

            var roster = model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star)).ToArray();
            controller.ResetDemo();
            var reset = controller.Model.Defenders.Single();
            Assert(controller.Campaign == run && controller.Model != model && controller.Model.Phase == FormationPhase.Preparation &&
                reset.IsSupernovaBoss && reset.SupernovaBossPhase == 1 && reset.Health == replay.startingHealth &&
                controller.Model.Clock == 0 && roster.SequenceEqual(controller.Model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star))),
                "Retry restores phase one and preserves the formation");
        }

        IEnumerator ObserveSupernovaAttack(FormationDefender guard, string animation, string clipName, int frames, bool travels,
            string[] layers, Action<FormationPresentationAction, FormationHit, int> record)
        {
            var model = controller.Model;
            FormationPresentationAction move = null;
            yield return ObserveSupernovaUntil(() =>
            {
                move = model.PresentationActions.LastOrDefault(action => action.ActorId == guard.Id &&
                    action.Animation == animation && !action.Cancelled && !action.ProjectileOnly && model.Clock - action.At <= .08);
                return move != null;
            }, animation + " begins using a phase-specific source motion");

            FormationHit hit = null;
            var bodyFrames = new HashSet<Sprite>();
            var seenLayers = new HashSet<string>();
            bool capturesFrames = Array.IndexOf(Environment.GetCommandLineArgs(), SupernovaBossClipsFlag) >= 0;
            double deadline = Time.realtimeSinceStartupAsDouble + frames / 24d;
            void Sample()
            {
                foreach (var renderer in controller.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (!renderer.enabled || renderer.sprite == null || !renderer.gameObject.activeInHierarchy) continue;
                    if (renderer.name == guard.Name && model.Clock <= move.EndAt && IsSupernovaBodySprite(renderer.sprite))
                        bodyFrames.Add(renderer.sprite);
                    foreach (string layer in layers)
                    {
                        if (renderer.name == FormationBattleController.SupernovaBossEffectPrefix + layer)
                            seenLayers.Add(layer);
                    }
                }
                hit ??= model.Hits.FirstOrDefault(entry => entry.FromId == guard.Id && entry.Skill && entry.Amount > 0 &&
                    (entry.Action == move || entry.Action?.ParentSequence == move.Sequence));
            }

            Sample();
            if (capturesFrames)
                yield return CaptureSupernovaClip(clipName, frames, _ => Sample());
            else
            {
                while (Time.realtimeSinceStartupAsDouble < deadline && controller.CombatViewActive)
                {
                    Sample();
                    yield return null;
                }
            }
            Assert(bodyFrames.Count >= 3, animation + " advances through multiple real rendered body frames");
            Assert(hit?.Action != null && (travels ? hit.Action.ProjectileMovement != null && hit.Action.ContactAt > hit.Action.LaunchedAt :
                hit.Action.ProjectileMovement == null && Math.Abs(hit.Action.ContactAt - hit.Action.LaunchedAt - .1 / hit.Action.Rate) < .000001) &&
                hit.At + .00001 >= hit.Action.ContactAt && hit.At <= hit.Action.ContactAt + .02501,
                animation + (travels ? " applies real damage only after projectile travel" : " applies real damage on the delayed source contact"));
            Assert(layers.All(seenLayers.Contains), animation + " renders " + string.Join(", ", layers) +
                " (observed " + string.Join(", ", seenLayers) + ")");
            var cues = new List<FormationBattleController.FormationAudioCue>();
            controller.CollectFormationAudio(move.At - .00001, model.Clock, cues);
            string[] bossCues = cues.Where(cue => cue.ActorId == guard.Id && cue.Clip != null && cue.SourceEvent != null)
                .Select(cue => cue.Clip.name).Distinct().ToArray();
            Assert(bossCues.Length > 0, animation + " resolves its original combat audio cues");
            result.supernovaBoss.observedAudio.Add(animation + " " + string.Join(", ", bossCues));
            record(move, hit, bodyFrames.Count);
        }

        IEnumerator ObserveSupernovaUntil(Func<bool> reached, string check)
        {
            var model = controller.Model;
            double deadline = Time.realtimeSinceStartupAsDouble + model.LimitSeconds + 10;
            while (!reached() && controller.CombatViewActive && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(reached(), $"{check} ({model.Phase}, {model.Clock:0.000}s)");
        }

        IEnumerator CaptureSupernovaClip(string name, int frames, Action<int> sample = null)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), SupernovaBossClipsFlag) < 0) yield break;
            using var timestamps = File.CreateText(Path.Combine(directory, name + "-utc-ticks.txt"));
            yield return CaptureInterfaceClip(name, frames, SupernovaBossClipsFlag, beforeFrame: frame =>
            {
                timestamps.WriteLine(DateTime.UtcNow.Ticks);
                sample?.Invoke(frame);
            });
        }

        SpriteRenderer SupernovaBossBody(FormationDefender guard) => controller.GetComponentsInChildren<SpriteRenderer>()
            .FirstOrDefault(renderer => renderer.enabled && renderer.sprite != null && renderer.name == guard.Name);

        static bool IsSupernovaBodySprite(Sprite sprite) => sprite != null &&
            (sprite.name.StartsWith("D31/", StringComparison.Ordinal) || sprite.name.StartsWith("D31B/", StringComparison.Ordinal));
    }
}
