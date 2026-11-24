using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationMakotoBossTests
    {
        const int Seed = 20261117;
        const float Step = .025f;
        static readonly BindingFlags ModelPrivate = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly MethodInfo Damage = typeof(FormationBattleModel).GetMethod("CombatDamage", ModelPrivate, null,
            new[] { typeof(FormationActor), typeof(FormationActor), typeof(float), typeof(int), typeof(bool), typeof(bool), typeof(bool) }, null);

        [Test]
        public void TrialPresetClearsAllThreeS3FormsAndReachesTheGate()
        {
            var model = CreateTrial(out var session);
            try
            {
                Assert.That(session.Run.IsMakotoBossTrial && model.EnabledCount == 8, Is.True);
                Assert.That(FormationCampaignRules.Nodes.Any(node => node.Id == session.Run.CurrentNode.Id), Is.False);
                Assert.That(model.Defenders.Single().MaxHealth, Is.EqualTo(180000f), "Scouting shows the raised opening bar");
                Assert.That(model.StartBattle(), Is.True);
                var guard = model.Defenders.Single();
                Assert.That(guard.IsMakotoBoss && model.Terrain.CanPlaceDefender(guard.Position), Is.True);
                var maximum = new float[3];
                var firstRealHit = new double[2];
                var breaks = new double[2];
                var swordMotions = new HashSet<string>();
                var thanatosActions = new Dictionary<int, FormationPresentationAction>();
                double guardFormOpened = 0, firstGuardHeal = 0, clear = 0;
                bool damagedByGuardForm = false;

                while (model.Phase is FormationPhase.Fighting or FormationPhase.Exiting)
                {
                    model.Advance(Step);
                    int phase = guard.MakotoBossPhase;
                    maximum[phase - 1] = guard.MaxHealth;
                    if (phase <= 2)
                    {
                        foreach (var hit in model.Hits)
                        {
                            if (hit.FromId != guard.Id || hit.Amount <= 0 || hit.Heal ||
                                !SwordBelongsToPhase(hit.Action?.Animation, phase)) continue;
                            swordMotions.Add(hit.Action.Animation);
                            if (firstRealHit[phase - 1] == 0) firstRealHit[phase - 1] = hit.At;
                        }
                    }
                    if (phase == 2)
                    {
                        foreach (var action in model.PresentationActions)
                            if (action.ActorId == guard.Id && !action.ProjectileOnly && SwordBelongsToPhase(action.Animation, 2))
                                thanatosActions.TryAdd(action.Sequence, action);
                    }
                    if (phase < 3 && breaks[phase - 1] == 0 && TransitionActive(model, guard))
                    {
                        breaks[phase - 1] = model.Clock;
                        Assert.That(guard.Health, Is.Zero);
                        Assert.That(model.Phase == FormationPhase.Fighting && model.BossAlive && model.RemainingDefenders == 1, Is.True);
                        model.Paused = true;
                        for (int tick = 0; tick < 10; tick++) model.Advance(Step);
                        Assert.That(model.Clock, Is.EqualTo(breaks[phase - 1]));
                        Assert.That(TransitionActive(model, guard), Is.True);
                        model.Paused = false;
                    }
                    if (phase == 3)
                    {
                        if (guardFormOpened == 0 && Recovering(model, guard)) guardFormOpened = model.Clock;
                        if (firstGuardHeal == 0)
                        {
                            var heal = model.Hits.FirstOrDefault(hit => hit.FromId == guard.Id && hit.ToId == guard.Id &&
                                hit.Heal && hit.Amount > 0 && hit.Action?.Animation == "makoto_s3_heal");
                            if (heal != null) firstGuardHeal = heal.At;
                        }
                        damagedByGuardForm |= model.Hits.Any(hit => hit.FromId == guard.Id && !hit.Heal && hit.Amount > 0 &&
                            hit.At > breaks[1]);
                    }
                    if (clear == 0 && model.RemainingDefenders == 0)
                    {
                        clear = model.Clock;
                        Assert.That(model.Phase, Is.EqualTo(FormationPhase.Exiting));
                    }
                }

                TestContext.WriteLine($"Makoto S3 preset: sword strikes at {string.Join(", ", firstRealHit)}; " +
                    $"breaks {string.Join(", ", breaks)}; guard {guardFormOpened:0.000}, first heal {firstGuardHeal:0.000}; clear {clear:0.000}; " +
                    $"result {model.Phase} at {model.Clock:0.000} with {guard.HealingDone:0} total healing.");
                Assert.That(maximum, Is.EqualTo(new[] { 180000f, 240000f, 225000f }));
                var thanatos = thanatosActions.Values.Where(action => !action.Cancelled).OrderBy(action => action.At).ToArray();
                var fullSpeed = thanatos.FirstOrDefault(action => Math.Abs(action.Rate - 4.4f) < 1e-4);
                Assert.That(fullSpeed, Is.Not.Null, "An unslowed Thanatos attack plays at source speed 2 × 2.2");
                Assert.That(fullSpeed.ContactAt - fullSpeed.At, Is.EqualTo(.8 / 4.4).Within(.00001));
                var fullSpeedGaps = thanatos.Zip(thanatos.Skip(1), (previous, next) => (previous, gap: next.At - previous.At))
                    .Where(pair => Math.Abs(pair.previous.Rate - 4.4f) < 1e-4).Select(pair => pair.gap).ToArray();
                Assert.That(fullSpeedGaps, Is.All.GreaterThanOrEqualTo(1.7 / 2.2 - 1e-5),
                    "Attack cadence never gets ahead of the nominal unslowed interval");
                TestContext.WriteLine($"Thanatos rate {fullSpeed.Rate:0.000}; windup {fullSpeed.ContactAt - fullSpeed.At:0.000}; " +
                    $"shortest adjacent start interval {fullSpeedGaps.DefaultIfEmpty(0).Min():0.000}.");
                Assert.That(firstRealHit[0], Is.InRange(1e-5, breaks[0]));
                Assert.That(firstRealHit[1], Is.InRange(breaks[0] + 1e-5, breaks[1]));
                Assert.That(swordMotions, Is.EquivalentTo(new[]
                {
                    "makoto_s3_human_attack_a", "makoto_s3_human_attack_b",
                    "makoto_s3_attack_a", "makoto_s3_attack_b"
                }));
                Assert.That(guardFormOpened, Is.GreaterThan(breaks[1]));
                Assert.That(firstGuardHeal, Is.GreaterThan(guardFormOpened).And.LessThan(clear));
                Assert.That(damagedByGuardForm, Is.False, "Makoto ceases all offensive contacts after switching to guard");
                Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
                Assert.That(model.Clock, Is.GreaterThan(clear).And.LessThan(model.LimitSeconds));
                Assert.That(model.Units.Any(unit => unit.Enabled && unit.Alive && unit.Position.x >= FormationBattleModel.Width - 1 - .0001f), Is.True);
                Assert.That(model.Retry(), Is.True);
                Assert.That(model.Defenders.Single().MakotoBossPhase, Is.EqualTo(1));
                Assert.That(model.PresentationActions, Is.Empty);
            }
            finally
            {
                FormationShopSession.StartNew(Seed, true);
            }
        }

        [Test]
        public void LastFormHealingCapsAndLowDamageRunsOutOfTime()
        {
            var model = CreateTrial(out _);
            try
            {
                Assert.That(model.StartBattle(), Is.True);
                var guard = model.Defenders.Single();
                var probe = model.Units.First(unit => unit.Enabled);
                foreach (var other in model.Units.Where(unit => unit != probe)) other.Enabled = false;
                probe.Health = probe.MaxHealth = 1000000;
                probe.Position = guard.Position + Vector2.right;
                Assert.That(model.Terrain.IsWalkable(probe.Position), Is.True);
                typeof(FormationActor).GetField("ActionUntil", ModelPrivate).SetValue(probe, double.PositiveInfinity);

                for (int phase = 1; phase < 3; phase++)
                {
                    Assert.That(ApplyTrueDamage(model, probe, guard, guard.MaxHealth * 2), Is.Positive);
                    Assert.That(TransitionActive(model, guard), Is.True);
                    Assert.That(ApplyTrueDamage(model, probe, guard, 10000), Is.Zero, "The open transition rejects incoming hits");
                    int next = phase + 1;
                    AdvanceUntil(model, () => guard.MakotoBossPhase == next && !TransitionActive(model, guard), 12);
                    Assert.That(guard.Health, Is.EqualTo(guard.MaxHealth), "The next bar starts full");
                }

                Assert.That(ApplyTrueDamage(model, probe, guard, 1000), Is.EqualTo(1000).Within(.01));
                float artsBeforeReduction = 1000 * (1 - guard.MagicResistance / 100);
                Assert.That(ApplyDamage(model, probe, guard, 1000, 1), Is.EqualTo(artsBeforeReduction * .88f).Within(.01));
                AdvanceUntil(model, () => Recovering(model, guard), 12);
                float healingAtRecovery = guard.HealingDone;
                float healthBeforeSustainedDamage = guard.Health;
                float damageBeforeSustainedDamage = probe.DamageDone;
                double checkHealingAt = model.Clock + 20;
                double nextShotAt = model.Clock + 1;
                while (model.Clock < checkHealingAt) AdvanceProbe();
                float healingTotal = guard.HealingDone;
                Assert.That(healingTotal - healingAtRecovery, Is.EqualTo(6400f).Within(.01));
                while (model.Phase == FormationPhase.Fighting) AdvanceProbe();

                Assert.That(guard.HealingDone, Is.EqualTo(healingTotal).Within(.01),
                    "The wounded Boss exhausts its healing even while the low-DPS attacker remains alive");
                Assert.That(probe.DamageDone - damageBeforeSustainedDamage, Is.GreaterThan(healthBeforeSustainedDamage),
                    "This damage would finish the final form within the remaining time if it did not heal");
                Assert.That(model.Phase, Is.EqualTo(FormationPhase.Lost));
                Assert.That(model.Clock, Is.EqualTo(model.LimitSeconds).Within(.001));
                Assert.That(model.BossAlive && guard.MakotoBossPhase == 3 && guard.Health > 0 && probe.Alive, Is.True);
                TestContext.WriteLine($"Last-form low-DPS probe: healing {healingTotal - healingAtRecovery:0}, " +
                    $"Boss HP {guard.Health:0}/{guard.MaxHealth:0} at {model.Clock:0.000}s.");

                void AdvanceProbe()
                {
                    model.Advance(Step);
                    if (model.Phase == FormationPhase.Fighting && model.Clock + 1e-8 >= nextShotAt)
                    {
                        nextShotAt += 1;
                        ApplyTrueDamage(model, probe, guard, 1520);
                    }
                }
            }
            finally
            {
                FormationShopSession.StartNew(Seed, true);
            }
        }

        static bool SwordBelongsToPhase(string motion, int phase) => phase switch
        {
            1 => motion is "makoto_s3_human_attack_a" or "makoto_s3_human_attack_b",
            2 => motion is "makoto_s3_attack_a" or "makoto_s3_attack_b",
            _ => false
        };

        static FormationBattleModel CreateTrial(out FormationShopSession session)
        {
            var starter = typeof(FormationShopSession).GetMethod("StartMakotoBossTrial", BindingFlags.Static | BindingFlags.NonPublic);
            session = (FormationShopSession)starter.Invoke(null, new object[] { Seed });
            var scene = EditorSceneManager.OpenPreviewScene(FormationBattleTools.ScenePath);
            try
            {
                var terrain = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FormationBattleController>(true)).Single().terrain.BuildLayout();
                var model = new FormationBattleModel(Seed, terrain);
                session.ApplyTo(model);
                return model;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void AdvanceUntil(FormationBattleModel model, Func<bool> condition, double seconds)
        {
            double deadline = model.Clock + seconds;
            while (model.Phase == FormationPhase.Fighting && !condition() && model.Clock < deadline)
                model.Advance(Step);
            Assert.That(condition(), Is.True, $"Expected transition or recovery by {deadline:0.000}s");
        }

        static bool TransitionActive(FormationBattleModel model, FormationDefender guard) =>
            (bool)typeof(FormationBattleModel).GetMethod("MakotoBossTransitionActive", ModelPrivate).Invoke(model, new object[] { guard });

        static bool Recovering(FormationBattleModel model, FormationDefender guard) =>
            (bool)typeof(FormationBattleModel).GetMethod("MakotoBossRecovering", ModelPrivate).Invoke(model, new object[] { guard });

        static float ApplyTrueDamage(FormationBattleModel model, FormationFighter probe, FormationDefender guard, float amount) =>
            ApplyDamage(model, probe, guard, amount, 2);

        static float ApplyDamage(FormationBattleModel model, FormationFighter probe, FormationDefender guard, float amount, int type) =>
            (float)Damage.Invoke(model, new object[] { probe, guard, amount, type, true, false, false });
    }
}
