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
        const string SnowBossClipsFlag = "-formationSnowBossClips";
        static bool SnowBossRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-formationSnowBossSmokeTest") >= 0;

        [Serializable]
        sealed class SnowBossReplay
        {
            public double secondSkillAttackAt, transitionAt, freezeSecondsRemaining, thirdSkillAttackAt, clearedAt, endAt;
            public double[] s2StrikeIntervals, s3StrikeIntervals;
            public int frozenUnits, freezeShells, snowCells, lateSnowCells, lateSnowCenterLayers, s2DistinctRenderedFrames, s3DistinctRenderedFrames;
            public int[] voiceStarts;
            public long audioCaptureUtcTicks;
            public double audioCaptureSeconds;
            public float startingHealth, restoredHealth, scoutPpu, s2Ppu, s3Ppu;
            public string musicClip, outcome;
        }

        IEnumerator VerifySnowBossTrial()
        {
            bool useMouse = Array.IndexOf(Environment.GetCommandLineArgs(), "-formationSnowBossMouse") >= 0;
            result.scope = "Rendered standalone Player using the production Snow Boss trial, its supplied roster and ordinary unscaled game updates. No actor stats, locations, damage or timing are injected. " +
                (useMouse ? "Menu and start use native mouse input. " : "Menu selection and start call their production controller commands. ") +
                "The formal 3-7 defense is checked separately through the difficulty selector.";
            if (useMouse)
            {
                yield return ClickStrategyUi(new Vector2(1239, 38));
                yield return Frame("01-menu");
                yield return ClickStrategyUi(new Vector2(ApprovedUi.Menu.center.x, 290));
            }
            else
                controller.SelectPramanixBossTrial();

            var run = controller.Campaign;
            var model = controller.Model;
            Assert(run.IsPramanixBossTrial && run.CurrentNode.Id == "3-7" && model.EnabledCount > 0 &&
                !run.HasBlockingChoice, "Dedicated trial opens immediately with the legal prebuilt army");
            var guard = model.Defenders.Single();
            result.node = run.CurrentNode.Id;
            Assert(guard.ArtId == "D27" && guard.IsSnowBoss && guard.SnowBossPhase == 1 &&
                model.Terrain.CanPlaceDefender(guard.Position) && model.BossAlive, "Solo six-star boss occupies a legal board tile");
            var replay = result.snowBoss = new SnowBossReplay { startingHealth = guard.MaxHealth };
            yield return ReadyArt();
            controller.SetFormationView(true);
            yield return WaitForScoutingView(true);
            var scoutBody = SnowBossBody(guard);
            replay.scoutPpu = scoutBody == null ? 0 : scoutBody.sprite.pixelsPerUnit;
            Assert(Mathf.Approximately(replay.scoutPpu, 249.6f) && scoutBody.transform.localScale.x > 3,
                "Scout uses the native high-resolution giant body");
            Assert(!SnowBossShells().Any(), "No freeze shell is visible during scouting");
            yield return Frame("02-giant-boss-scout");
            controller.SetFormationView(false);
            yield return WaitForScoutingView(false);
            yield return Frame("03-trial-roster");

            FormationSnowBossAudioEvidence audioCapture = null;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), SnowBossClipsFlag) >= 0)
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
            Assert(model.Phase == FormationPhase.Fighting && model.Speed == 1, "Boss trial starts at the ordinary simulation rate");
            guard = model.Defenders.Single();
            yield return ObserveSnowBossAudio(1);
            replay.musicClip = controller.SnowBossMusic.clip.name;
            yield return CaptureSnowBossClip("battle-start-voice", 59);
            yield return ObserveSnowBossUntil(() => model.PresentationActions.Any(action => action.ActorId == guard.Id && action.Skill),
                "Phase 1 performs its S2 animation");
            replay.secondSkillAttackAt = model.Clock;
            yield return CaptureSnowBossClip("s2-cast-and-contact", 37);
            yield return ObserveSnowBossUntil(() => model.SnowBossSnow(guard).Count > 0 &&
                model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill && hit.Amount > 0), "S2 damage and snow originate on the battlefield");
            replay.snowCells = model.SnowBossSnow(guard).Count;
            Assert(replay.snowCells >= 7 && SnowBossGroundIsLegal(model, guard) && SnowBossGroundRendered(model, guard),
                "S2 renders original snowfall across real walkable tiles around the boss");
            var body = SnowBossBody(guard);
            replay.s2Ppu = body == null ? 0 : body.sprite.pixelsPerUnit;
            Assert(Mathf.Approximately(replay.s2Ppu, 249.6f), "S2 uses the HD body in battle");
            yield return Frame("04-s2-snow-tiles");
            yield return ObserveSnowBossDirectImpact(guard, false);
            yield return ObserveSnowBossMotion(guard, false, count => replay.s2DistinctRenderedFrames = count);
            yield return ObserveSnowBossUntil(() => SnowBossPlayerIntervals(model, guard, false).Length >= 2,
                "S2 repeats at the faster combat interval with its source motion rate");
            replay.s2StrikeIntervals = SnowBossPlayerIntervals(model, guard, false);

            yield return ObserveSnowBossUntil(() => model.SnowBossTransitionActive(guard), "First lethal hit starts the phase transition");
            replay.transitionAt = model.Clock;
            var frozen = model.Units.Concat(model.Summons).Where(unit => unit.Alive).ToArray();
            replay.frozenUnits = frozen.Length;
            Assert(frozen.Length > 0 && frozen.All(unit => model.SnowBossFreezeRemaining(unit) > 0),
                "All living attackers and summons are frozen together");
            Assert(model.BossAlive && model.RemainingDefenders == 1 && guard.Health == 0 &&
                model.SnowBossSnow(guard).Count == 0, "Transition empties the first bar and clears S2 tiles without clearing the board");
            var positions = frozen.Select(unit => unit.Position).ToArray();
            var heldBossFrame = SnowBossBody(guard)?.sprite;
            var frozenActors = frozen.Cast<FormationActor>().Append(guard).Where(controller.FormationActorVisibleInView).ToArray();
            var shells = SnowBossShells();
            replay.freezeShells = shells.Length;
            Assert(frozenActors.Length > 1 && frozenActors.Contains(guard) && frozenActors.All(actor =>
                shells.Any(shell => shell.name == FormationBattleController.PramanixIceRendererPrefix + actor.Id &&
                    Vector3.Distance(shell.transform.position, controller.ActorWorldPoint(actor)) < .35f &&
                    shell.sprite.texture.name.Replace('\\', '/').Contains(actor == guard ? "/buff_frozen-" : "/buff_frozen_to_walk-"))),
                "Boss and each in-view frozen unit have their aligned native freeze shell");
            yield return Frame("05-active-global-freeze");
            yield return CaptureSnowBossClip("phase-break-freeze", 28, () => model.SnowBossFreezeRemaining(frozen[0]) > .3);
            Assert(positions.SequenceEqual(frozen.Select(unit => unit.Position)), "Frozen opponents hold their positions across rendered frames");
            Assert(heldBossFrame != null && SnowBossBody(guard)?.sprite == heldBossFrame &&
                model.SnowBossTransitionActive(guard), "The boss's source pose holds during its own freeze");
            model.Paused = true;
            double pausedAt = model.Clock;
            replay.freezeSecondsRemaining = model.SnowBossFreezeRemaining(frozen[0]);
            yield return Frame("05-paused-global-freeze");
            int pausedMusicAt = controller.SnowBossMusic.timeSamples;
            yield return new WaitForSecondsRealtime(.3f);
            Assert(replay.freezeSecondsRemaining > 0 && model.Clock == pausedAt &&
                model.SnowBossFreezeRemaining(frozen[0]) == replay.freezeSecondsRemaining, "Pause holds the transition and the freeze timer");
            Assert(!controller.SnowBossMusic.isPlaying && controller.SnowBossMusic.timeSamples == pausedMusicAt &&
                controller.SnowBossVoiceStarts.Count == 1, "Pause holds the music playhead without replaying the first voice");
            model.Paused = false;

            yield return ObserveSnowBossUntil(() => guard.SnowBossPhase == 2 && !model.SnowBossTransitionActive(guard) &&
                model.PresentationActions.Any(action => action.ActorId == guard.Id && action.Skill && action.At > replay.transitionAt),
                "Phase 2 begins a new S3 cast after the freeze");
            replay.thirdSkillAttackAt = model.Clock;
            replay.restoredHealth = guard.Health;
            Assert(guard.Health > guard.MaxHealth * .95f, "Phase 2 opens with its own health bar");
            Assert(!SnowBossShells().Any(), "All transition freeze shells clear after the thaw");
            yield return ObserveSnowBossAudio(2);
            yield return CaptureSnowBossClip("s3-cast-and-contact", 59);
            yield return ObserveSnowBossUntil(() => model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill &&
                hit.Amount > 0 && hit.At >= replay.thirdSkillAttackAt - .05), "S3 applies damage through a real hit");
            body = SnowBossBody(guard);
            replay.s3Ppu = body == null ? 0 : body.sprite.pixelsPerUnit;
            Assert(Mathf.Approximately(replay.s3Ppu, 249.6f), "S3 uses the HD body in battle");
            yield return Frame("06-s3-impact");
            yield return ObserveSnowBossDirectImpact(guard, true);
            yield return ObserveSnowBossMotion(guard, true, count => replay.s3DistinctRenderedFrames = count);
            yield return ObserveSnowBossUntil(() => SnowBossPlayerIntervals(model, guard, true).Length >= 2,
                "S3 repeats at the faster combat interval with its source motion rate");
            replay.s3StrikeIntervals = SnowBossPlayerIntervals(model, guard, true);
            yield return ObserveSnowBossUntil(() => model.Clock >= replay.thirdSkillAttackAt + 18,
                "Second phase runs beyond the former skill expiry and recast times");
            var lateSnow = model.SnowBossSnow(guard);
            replay.lateSnowCells = lateSnow.Count;
            lateSnow.TryGetValue(Vector2Int.RoundToInt(guard.Position), out replay.lateSnowCenterLayers);
            Assert(guard.SnowBossPhase == 2 && replay.lateSnowCells >= 12 && replay.lateSnowCenterLayers == 5 &&
                SnowBossGroundIsLegal(model, guard) && SnowBossGroundRendered(model, guard) && model.GuardSkillActive(guard) &&
                double.IsPositiveInfinity(model.GuardEffect(guard).EndsAt),
                "S3 keeps its full-depth legal snowfall around the boss without timing out");
            Assert(model.PresentationActions.Any(action => action.ActorId == guard.Id && !action.Cancelled &&
                action.Animation?.StartsWith("skill_attack", StringComparison.Ordinal) == true && action.At >= model.Clock - 2) &&
                !model.PresentationActions.Any(action => action.ActorId == guard.Id && action.Animation == "skill_begin"),
                "Late S3 continues to attack without repeating the startup");
            yield return Frame("06-late-s3-snowfall");
            yield return CaptureSnowBossClip("s3-late-permanent-snowfall", 44);

            yield return ObserveSnowBossUntil(() => model.RemainingDefenders == 0, "The supplied roster removes the second bar by ordinary combat");
            Assert(model.Phase == FormationPhase.Exiting && !model.BossAlive, "Only the second clear opens the route to the blue gate");
            replay.clearedAt = model.Clock;
            yield return Frame("07-second-bar-cleared");
            yield return ObserveSnowBossUntil(() => model.Phase == FormationPhase.Won, "A real deployed body reaches the blue gate");
            replay.voiceStarts = controller.SnowBossVoiceStarts.ToArray();
            Assert(replay.voiceStarts.Length == 2 && replay.voiceStarts.Distinct().Count() == 2 &&
                !controller.SnowBossMusic.isPlaying && !controller.SnowBossVoice.isPlaying,
                "Exactly two distinct dialogue lines play in the fight and both audio channels stop at settlement");
            if (audioCapture != null)
                replay.audioCaptureUtcTicks = audioCapture.Save(Path.Combine(directory, "battle-mix.wav"), out replay.audioCaptureSeconds);
            replay.outcome = model.Phase.ToString();
            replay.endAt = model.Clock;
            result.battles++;
            yield return Frame("08-trial-victory");

            var roster = model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star)).ToArray();
            controller.ResetDemo();
            var resetGuard = controller.Model.Defenders.Single();
            Assert(controller.Campaign == run && model != controller.Model && controller.Model.Phase == FormationPhase.Preparation &&
                resetGuard.SnowBossPhase == 1 && resetGuard.Health == replay.startingHealth && controller.Model.Clock == 0 &&
                roster.SequenceEqual(controller.Model.Units.Select(unit => (unit.Instance, unit.Spawn, unit.Star))),
                "Trial retry restores phase one and preserves the edited roster");
            int finalNode = Array.FindIndex(FormationCampaignRules.Nodes, node => node.Id == "3-7");
            controller.SelectPracticeBattle(finalNode);
            var formalGuard = controller.Model.Defenders.Single();
            Assert(!controller.Campaign.IsPramanixBossTrial && controller.Campaign.CurrentNode.Id == "3-7" &&
                formalGuard.IsSnowBoss && formalGuard.ArtId == resetGuard.ArtId && formalGuard.Position == resetGuard.Position &&
                formalGuard.MaxHealth == resetGuard.MaxHealth, "Formal 3-7 uses the same giant two-phase defense");
        }

        IEnumerator CaptureSnowBossClip(string name, int frames, Func<bool> continueRecording = null)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), SnowBossClipsFlag) < 0) yield break;
            using var times = File.CreateText(Path.Combine(directory, name + "-utc-ticks.txt"));
            yield return CaptureInterfaceClip(name, frames, SnowBossClipsFlag, continueRecording,
                _ => times.WriteLine(DateTime.UtcNow.Ticks));
        }

        SpriteRenderer SnowBossBody(FormationDefender guard) => controller.GetComponentsInChildren<SpriteRenderer>()
            .SingleOrDefault(renderer => renderer.enabled && renderer.sprite != null && renderer.name == guard.Name);

        SpriteRenderer[] SnowBossShells() => controller.GetComponentsInChildren<SpriteRenderer>()
            .Where(renderer => renderer.enabled && renderer.sprite != null &&
                renderer.name.StartsWith(FormationBattleController.PramanixIceRendererPrefix, StringComparison.Ordinal)).ToArray();

        static bool SnowBossGroundIsLegal(FormationBattleModel model, FormationDefender guard) =>
            model.SnowBossSnow(guard).Keys.All(cell => model.Terrain.IsWalkable(cell) && model.Terrain.HeightAt(cell) == 0);

        bool SnowBossGroundRendered(FormationBattleModel model, FormationDefender guard)
        {
            var sprites = controller.GetComponentsInChildren<SpriteRenderer>();
            return model.SnowBossSnow(guard).Where(pair => controller.FormationPointVisibleInView(pair.Key)).All(pair =>
            {
                var cell = pair.Key;
                string texture = pair.Value == 1 ? "sbell2_ground_01" : pair.Value < 5 ? "sbell2_ground_02" : "sbell2_ground_03";
                return sprites.Any(renderer => renderer.enabled && renderer.sprite != null &&
                    renderer.name == FormationBattleController.PramanixSnowRendererPrefix + cell.x + "," + cell.y &&
                    renderer.sprite.name.Contains(texture) &&
                    Vector3.Distance(renderer.transform.position, controller.ActorWorldPoint(cell)) < .15f);
            });
        }

        IEnumerator ObserveSnowBossDirectImpact(FormationDefender guard, bool thirdSkill)
        {
            var model = controller.Model;
            string skill = thirdSkill ? "S3" : "S2";
            string clip = thirdSkill ? "sbell2_skill_03_hit" : "sbell2_skill_01_hit";
            yield return ObserveSnowBossUntil(() =>
            {
                var directHits = model.Hits.Where(hit => hit.FromId == guard.Id && hit.Skill && hit.Amount > 0 &&
                    hit.Action != null && IsSnowBossStrike(hit.Action, thirdSkill) &&
                    Math.Abs(hit.At - hit.Action.ContactAt) <= .026 &&
                    model.Clock - hit.At >= .08 && model.Clock - hit.At <= .25 &&
                    controller.FormationPointVisibleInView(hit.To)).ToArray();
                if (directHits.Length == 0) return false;
                var sprites = controller.GetComponentsInChildren<SpriteRenderer>();
                return directHits.Any(hit => sprites.Any(renderer => renderer.enabled && renderer.sprite != null &&
                    renderer.name == FormationBattleController.PramanixImpactRendererPrefix + hit.ToId &&
                    renderer.sprite.name.Contains(clip) && SnowBossImpactOverTarget(renderer, hit)));
            }, skill + " renders the native direct-impact layer above a target damaged at the source contact", 8);
            yield return Frame(thirdSkill ? "06-s3-native-direct-impact" : "04-s2-native-direct-impact");
        }

        bool SnowBossImpactOverTarget(SpriteRenderer renderer, FormationHit hit)
        {
            var camera = Camera.main;
            var foot = camera.WorldToViewportPoint(controller.ActorWorldPoint(hit.To));
            var impact = camera.WorldToViewportPoint(renderer.transform.position);
            int targetOrder = 5000 - Mathf.RoundToInt(hit.To.y * 50);
            return impact.z > 0 && Mathf.Abs(impact.x - foot.x) < .03f &&
                impact.y - foot.y > .01f && impact.y - foot.y < .25f && renderer.sortingOrder > targetOrder;
        }

        IEnumerator ObserveSnowBossAudio(int phase)
        {
            yield return ObserveSnowBossUntil(() => controller.SnowBossVoiceStarts.Count == phase &&
                controller.SnowBossVoice != null && controller.SnowBossVoice.isPlaying,
                phase == 1 ? "One original voice starts with the battle" : "A different original voice starts on entry to phase two");
            var music = controller.SnowBossMusic;
            var voice = controller.SnowBossVoice;
            var starts = controller.SnowBossVoiceStarts;
            Assert(music != null && music.loop && music.isPlaying && music.clip != null && music.clip.name == "battle" &&
                music.clip.length > 170 && music.clip.channels == 2 && !music.mute && music.volume > 0,
                "The selected original event battle soundtrack is actively looping");
            Assert(starts.Distinct().Count() == phase && starts.All(line => line >= 1 && line <= 4) &&
                voice.clip != null && voice.clip.name == "voice-" + starts.Last() && !voice.loop && !voice.mute && voice.volume > 0,
                "Playback uses the expected distinct non-looping original voice asset");
            int musicAt = music.timeSamples;
            int voiceAt = voice.timeSamples;
            yield return new WaitForSecondsRealtime(.15f);
            Assert(music.timeSamples > musicAt && voice.timeSamples > voiceAt,
                "The live voice and music playback positions both advance");
        }

        IEnumerator ObserveSnowBossMotion(FormationDefender guard, bool thirdSkill, Action<int> report)
        {
            var model = controller.Model;
            FormationPresentationAction attack = null;
            yield return ObserveSnowBossUntil(() =>
            {
                attack = model.PresentationActions.LastOrDefault(action => action.ActorId == guard.Id && !action.Cancelled &&
                    model.Clock - action.At <= .05 && IsSnowBossStrike(action, thirdSkill));
                return attack != null;
            }, thirdSkill ? "S3 starts another genuine rendered swing" : "S2 starts another genuine rendered swing");
            var frames = new HashSet<Sprite>();
            bool blended = false;
            var properties = new MaterialPropertyBlock();
            while (controller.CombatViewActive && !attack.Cancelled && model.Clock < attack.EndAt)
            {
                var body = SnowBossBody(guard);
                if (body != null && body.sprite.name.Contains("/" + attack.Animation + "/"))
                {
                    frames.Add(body.sprite);
                    body.GetPropertyBlock(properties);
                    float weight = properties.GetFloat("_PoseWeight");
                    blended |= weight > 0 && weight < 1;
                }
                yield return null;
            }
            Assert(frames.Count >= 6 && blended, "The giant body advances through original frames with live pose interpolation");
            report(frames.Count);
        }

        static bool IsSnowBossStrike(FormationPresentationAction action, bool thirdSkill) =>
            thirdSkill ? action.Animation?.StartsWith("skill_attack", StringComparison.Ordinal) == true : action.Animation == "skill_2_attack";

        static double[] SnowBossPlayerIntervals(FormationBattleModel model, FormationDefender guard, bool thirdSkill)
        {
            var actions = model.PresentationActions.Where(action => action.ActorId == guard.Id && !action.Cancelled &&
                IsSnowBossStrike(action, thirdSkill))
                .ToArray();
            double expectedRate = thirdSkill ? 2.6 : 2;
            double expectedInterval = thirdSkill ? 35d / 30 : 1.5;
            return actions.Zip(actions.Skip(1), (a, b) => (earlier: a, later: b, interval: b.At - a.At))
                .Where(pair => Math.Abs(pair.earlier.Rate - expectedRate) < .0001 && Math.Abs(pair.later.Rate - expectedRate) < .0001 &&
                    Math.Abs(pair.interval - expectedInterval) <= .025001)
                .Select(pair => pair.interval).ToArray();
        }

        IEnumerator ObserveSnowBossUntil(Func<bool> reached, string check, double? timeoutSeconds = null)
        {
            var model = controller.Model;
            double deadline = Time.realtimeSinceStartupAsDouble + (timeoutSeconds ?? model.LimitSeconds + 10);
            while (!reached() && controller.CombatViewActive && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(reached(), $"{check} ({model.Phase}, {model.Clock:0.000}s)");
        }
    }
}
