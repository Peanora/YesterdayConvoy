using System;
using System.Collections;
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
            public int frozenUnits, freezeShells, snowCells;
            public float startingHealth, restoredHealth, scoutPpu, s2Ppu, s3Ppu;
            public string outcome;
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

            if (useMouse) yield return ClickStrategyUi(FormationBattleController.FormationStartRect.center);
            else controller.StartBattle();
            Assert(model.Phase == FormationPhase.Fighting && model.Speed == 1, "Boss trial starts at the ordinary simulation rate");
            guard = model.Defenders.Single();
            yield return ObserveSnowBossUntil(() => model.PresentationActions.Any(action => action.ActorId == guard.Id && action.Skill),
                "Phase 1 performs its S2 animation");
            replay.secondSkillAttackAt = model.Clock;
            yield return CaptureInterfaceClip("s2-cast-and-contact", 37, SnowBossClipsFlag);
            yield return ObserveSnowBossUntil(() => model.SnowBossSnow(guard).Count > 0 &&
                model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill && hit.Amount > 0), "S2 damage and snow originate on the battlefield");
            replay.snowCells = model.SnowBossSnow(guard).Count;
            var body = SnowBossBody(guard);
            replay.s2Ppu = body == null ? 0 : body.sprite.pixelsPerUnit;
            Assert(Mathf.Approximately(replay.s2Ppu, 249.6f), "S2 uses the HD body in battle");
            yield return Frame("04-s2-snow-tiles");
            yield return ObserveSnowBossUntil(() => SnowBossPlayerIntervals(model, guard, false).Length >= 2,
                "S2 repeats at its unmodified source interval");
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
                    Vector3.Distance(shell.transform.position, controller.ActorWorldPoint(actor)) < .35f)),
                "Boss and each in-view frozen unit have an aligned visible freeze shell");
            yield return Frame("05-active-global-freeze");
            yield return CaptureInterfaceClip("phase-break-freeze", 28, SnowBossClipsFlag, () => model.SnowBossFreezeRemaining(frozen[0]) > .3);
            Assert(positions.SequenceEqual(frozen.Select(unit => unit.Position)), "Frozen opponents hold their positions across rendered frames");
            Assert(heldBossFrame != null && SnowBossBody(guard)?.sprite == heldBossFrame &&
                model.SnowBossTransitionActive(guard), "The boss's source pose holds during its own freeze");
            model.Paused = true;
            double pausedAt = model.Clock;
            replay.freezeSecondsRemaining = model.SnowBossFreezeRemaining(frozen[0]);
            yield return Frame("05-paused-global-freeze");
            yield return new WaitForSecondsRealtime(.3f);
            Assert(replay.freezeSecondsRemaining > 0 && model.Clock == pausedAt &&
                model.SnowBossFreezeRemaining(frozen[0]) == replay.freezeSecondsRemaining, "Pause holds the transition and the freeze timer");
            model.Paused = false;

            yield return ObserveSnowBossUntil(() => guard.SnowBossPhase == 2 && !model.SnowBossTransitionActive(guard) &&
                model.PresentationActions.Any(action => action.ActorId == guard.Id && action.Skill && action.At > replay.transitionAt),
                "Phase 2 begins a new S3 cast after the freeze");
            replay.thirdSkillAttackAt = model.Clock;
            replay.restoredHealth = guard.Health;
            Assert(guard.Health > guard.MaxHealth * .95f, "Phase 2 opens with its own health bar");
            Assert(!SnowBossShells().Any(), "All transition freeze shells clear after the thaw");
            yield return CaptureInterfaceClip("s3-cast-and-contact", 44, SnowBossClipsFlag);
            yield return ObserveSnowBossUntil(() => model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill &&
                hit.Amount > 0 && hit.At >= replay.thirdSkillAttackAt - .05), "S3 applies damage through a real hit");
            body = SnowBossBody(guard);
            replay.s3Ppu = body == null ? 0 : body.sprite.pixelsPerUnit;
            Assert(Mathf.Approximately(replay.s3Ppu, 249.6f), "S3 uses the HD body in battle");
            yield return Frame("06-s3-impact");
            yield return ObserveSnowBossUntil(() => SnowBossPlayerIntervals(model, guard, true).Length >= 2,
                "S3 repeats at its unmodified source interval");
            replay.s3StrikeIntervals = SnowBossPlayerIntervals(model, guard, true);

            yield return ObserveSnowBossUntil(() => model.RemainingDefenders == 0, "The supplied roster removes the second bar by ordinary combat");
            Assert(model.Phase == FormationPhase.Exiting && !model.BossAlive, "Only the second clear opens the route to the blue gate");
            replay.clearedAt = model.Clock;
            yield return Frame("07-second-bar-cleared");
            yield return ObserveSnowBossUntil(() => model.Phase == FormationPhase.Won, "A real deployed body reaches the blue gate");
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

        SpriteRenderer SnowBossBody(FormationDefender guard) => controller.GetComponentsInChildren<SpriteRenderer>()
            .SingleOrDefault(renderer => renderer.enabled && renderer.sprite != null && renderer.name == guard.Name);

        SpriteRenderer[] SnowBossShells() => controller.GetComponentsInChildren<SpriteRenderer>()
            .Where(renderer => renderer.enabled && renderer.sprite != null &&
                renderer.name.StartsWith(FormationBattleController.PramanixIceRendererPrefix, StringComparison.Ordinal)).ToArray();

        static double[] SnowBossPlayerIntervals(FormationBattleModel model, FormationDefender guard, bool thirdSkill)
        {
            var actions = model.PresentationActions.Where(action => action.ActorId == guard.Id && !action.Cancelled &&
                (thirdSkill ? action.Animation?.StartsWith("skill_attack", StringComparison.Ordinal) == true : action.Animation == "skill_2_attack"))
                .ToArray();
            double expectedRate = thirdSkill ? 2.6 : 2;
            double expectedInterval = thirdSkill ? 46d / 30 : 2;
            return actions.Zip(actions.Skip(1), (a, b) => (earlier: a, later: b, interval: b.At - a.At))
                .Where(pair => Math.Abs(pair.earlier.Rate - expectedRate) < .0001 && Math.Abs(pair.later.Rate - expectedRate) < .0001 &&
                    Math.Abs(pair.interval - expectedInterval) <= .025001)
                .Select(pair => pair.interval).ToArray();
        }

        IEnumerator ObserveSnowBossUntil(Func<bool> reached, string check)
        {
            var model = controller.Model;
            double deadline = Time.realtimeSinceStartupAsDouble + model.LimitSeconds + 10;
            while (!reached() && controller.CombatViewActive && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(reached(), $"{check} ({model.Phase}, {model.Clock:0.000}s)");
        }
    }
}
