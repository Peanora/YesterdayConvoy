using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationAnimationTimingTests
    {
        static FormationBattleModel Fixture(out FormationCampaignRun run, params string[] ids)
        {
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(p => new ShopTrialModel.Role { Id = p.CardId, Name = p.Name, Cost = p.Cost }), 71);
            shop.EnableFormationEconomy(); shop.Preset(Array.Empty<ShopTrialModel.Piece>(), Array.Empty<string>(), 10, 0, 80);
            run = new FormationCampaignRun(shop, 71);
            int choices = 0;
            while (run.HasBlockingChoice) { Assert.That(++choices, Is.LessThan(32)); Assert.That(run.Choose(0).Success, Is.True); }
            shop.Preset(ids.Select(id => new ShopTrialModel.Piece { Id = FormationCampaignRules.Find(id).CardId, Star = 1 }), Array.Empty<string>(), 10, 0, 80);
            var model = new FormationBattleModel(71); model.BindCampaign(run);
            Assert.That(model.ApplyRoster(shop.Bench.Select((piece, index) => new FormationRosterEntry
            { Instance = piece.Instance, Star = 1, ArtId = piece.Id, Spawn = new Vector2(index * 2, 2) }), 10), Is.True);
            model.Defenders.RemoveRange(1, model.Defenders.Count - 1);
            var target = model.Defenders[0]; target.Position = new Vector2(15, 3); target.MaxHealth = 100000000;
            target.Attack = 0; target.Range = .01f; target.Armor = 0; target.MagicResistance = 0; target.Healer = false;
            return model;
        }
        static void Enter(FormationBattleModel model)
        {
            Assert.That(model.StartBattle(), Is.True);
            foreach (var actor in model.Units) { actor.Position = new Vector2(14.5f, 3); actor.SkillCharge = 0; }
        }
        static void Advance(FormationBattleModel model, float seconds)
        { for (int index = 0; index < Mathf.CeilToInt(seconds / .025f); index++) model.Advance(.025f); }
        static FormationAnimationProfile Profile(float contact = 1, float duration = 2.4f) => new FormationAnimationProfile
        { AttackDuration = duration, AttackContact = contact, SkillDuration = 2, SkillContact = .6f, BaseRate = 2 };

        [Test]
        public void SourceContactReleasesDamageAndAttackRecoveryTogetherWithoutExtendingLogicalAction()
        {
            var model = Fixture(out _, "B22"); model.SetAnimationProfile("B22", Profile()); Enter(model);
            var actor = model.Units.Single(); float health = model.Defenders.Single().Health;
            Advance(model, .25f);
            var action = model.PresentationActions.Single();
            Assert.That(action.Rate, Is.EqualTo(2));
            Assert.That(action.ContactAt - action.At, Is.EqualTo(.5).Within(.0001));
            Assert.That(action.EndAt - action.At, Is.EqualTo(1.2).Within(.0001));
            Assert.That(actor.PresentationDuration, Is.EqualTo(1.2).Within(.0001));
            Assert.That(actor.PresentationMoveLockUntil - action.At, Is.LessThanOrEqualTo(.36001));
            Assert.That(model.Defenders.Single().Health, Is.EqualTo(health));
            Assert.That(actor.SkillCharge, Is.EqualTo(model.Clock).Within(.001), "Only normal time recovery is available before contact.");
            Advance(model, .3f);
            Assert.That(actor.DamageDone, Is.GreaterThan(0));
            Assert.That(actor.SkillCharge, Is.EqualTo(model.Clock + 1).Within(.001), "The ripper's one ordinary-action SP arrives with its real hit.");
            Assert.That(model.PresentationActions.Count, Is.EqualTo(1));
        }

        [Test]
        public void AttackSpeedScalesSourceRateButKeepsTheConfiguredAttackCadence()
        {
            var model = Fixture(out var run, "B09"); var actor = model.Units.Single();
            Assert.That(run.Equip(run.AddItem("B").Instance, actor.Instance).Success, Is.True);
            model.BindCampaign(run); model.SetAnimationProfile("B09", Profile(1.8f, 3)); Enter(model);
            Advance(model, 6);
            var actions = model.PresentationActions.Where(a => a.ActorId == actor.Id && !a.Skill && !a.ProjectileOnly).ToArray();
            Assert.That(actions.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(actions[0].Rate, Is.EqualTo(2 * 1.15f).Within(.001));
            Assert.That(actions[0].EndAt - actions[0].At, Is.EqualTo(3 / (2 * 1.15f)).Within(.001));
            foreach (var gap in actions.Skip(1).Zip(actions, (later, earlier) => later.At - earlier.At))
                Assert.That(gap, Is.InRange(2.4 / 1.15 - .001, 2.4 / 1.15 + .026));
        }

        [Test]
        public void OverlappingWindupsKeepTheirOwnTransactionsAndDoNotSlowFollowingAttacks()
        {
            var model = Fixture(out _, "B22"); model.SetAnimationProfile("B22", Profile(6, 8)); Enter(model);
            var actor = model.Units.Single(); Advance(model, 2);
            Assert.That(model.PresentationActions.Count, Is.EqualTo(2), "A four-second source body cannot increase a 1.5-second gameplay interval.");
            Assert.That(actor.DamageDone, Is.Zero);
            Advance(model, 2.6f);
            var ordinary = model.PresentationActions.Where(a => !a.Skill).ToArray();
            Assert.That(ordinary.Length, Is.EqualTo(4));
            Assert.That(ordinary[1].At - ordinary[0].At, Is.EqualTo(1.5).Within(.026));
            Assert.That(actor.SkillCharge, Is.EqualTo(model.Clock + 2).Within(.001));
            Assert.That(actor.DamageDone, Is.EqualTo(actor.Attack * 2).Within(.01));
            Assert.That(ordinary.All(a => !a.Cancelled), Is.True);
        }

        [Test]
        public void PauseFreezesQueuedContactAndItsPresentationClock()
        {
            var model = Fixture(out _, "B22"); model.SetAnimationProfile("B22", Profile()); Enter(model); Advance(model, .2f);
            double clock = model.Clock, contact = model.PresentationActions.Single().ContactAt; float charge = model.Units.Single().SkillCharge;
            model.Paused = true; Advance(model, 2);
            Assert.That(model.Clock, Is.EqualTo(clock)); Assert.That(model.Units.Single().SkillCharge, Is.EqualTo(charge));
            Assert.That(model.Units.Single().DamageDone, Is.Zero); Assert.That(model.PresentationActions.Single().ContactAt, Is.EqualTo(contact));
            model.Paused = false; Advance(model, .4f); Assert.That(model.Units.Single().DamageDone, Is.GreaterThan(0));
        }

        [Test]
        public void ADeadTargetCancelsItsUnreleasedHitAndDoesNotGrantOrdinaryAttackSP()
        {
            var model = Fixture(out _, "B22"); model.SetAnimationProfile("B22", Profile()); Enter(model); Advance(model, .2f);
            model.Defenders.Single().Health = 0; model.Defenders.Single().Alive = false;
            Advance(model, .4f);
            Assert.That(model.PresentationActions.Single().Cancelled, Is.True);
            Assert.That(model.Units.Single().DamageDone, Is.Zero);
            Assert.That(model.Units.Single().SkillCharge, Is.EqualTo(model.Clock).Within(.001));
        }

        [Test]
        public void ActualSourceDeathCancelsThePendingAttackFromThatLife()
        {
            var model = Fixture(out _, "B22"); model.SetAnimationProfile("B22", Profile());
            model.Defenders.Single().Attack = 100000; model.Defenders.Single().Range = 10; Enter(model);
            Advance(model, .7f);
            var actor = model.Units.Single();
            Assert.That(actor.Alive, Is.False);
            Assert.That(model.PresentationActions.Single(action => action.ActorId == actor.Id && !action.Skill).Cancelled, Is.True);
            Assert.That(actor.DamageDone, Is.Zero);
        }

        [Test]
        public void NoAttackSourceDoesNotInventEnemyAutosButRealHealersStillHealWounds()
        {
            var model = Fixture(out _, "B10");
            model.SetAnimationProfile("B10", new FormationAnimationProfile { HasOrdinaryAttack = false }); Enter(model); Advance(model, 1);
            var healer = model.Units.Single();
            Assert.That(model.PresentationActions, Is.Empty, "A full-health team without renewal has no ordinary healing windup.");
            healer.Health -= 200; Advance(model, .025f);
            Assert.That(model.PresentationActions.Single().Heal, Is.True);
            Assert.That(model.PresentationActions.Single().TargetId, Is.EqualTo(healer.Id));
            Assert.That(healer.HealingDone, Is.GreaterThan(0)); Assert.That(healer.DamageDone, Is.Zero);
            Assert.That(model.Hits.All(hit => hit.Heal), Is.True);
        }

        [Test]
        public void RenewalAllowsRealOverflowShieldWithoutInventingAnEnemyAttack()
        {
            var model = Fixture(out _, "M25", "M21");
            model.SetAnimationProfile("M25", new FormationAnimationProfile { HasOrdinaryAttack = false }); Enter(model); Advance(model, .025f);
            var healer = model.Units.First(u => u.ArtId == "M25");
            Assert.That(model.ActiveSynergies["复苏"], Is.EqualTo(2));
            Assert.That(model.PresentationActions.Any(action => action.ActorId == healer.Id && action.Heal), Is.True);
            Assert.That(healer.Shield, Is.GreaterThan(0)); Assert.That(healer.DamageDone, Is.Zero);
        }

        [Test]
        public void ExplicitNonAttackingProfileSuppressesOffensiveAutosWithoutDisablingNaturalSP()
        {
            var model = Fixture(out _, "B22");
            model.SetAnimationProfile("B22", new FormationAnimationProfile { HasOrdinaryAttack = false }); Enter(model); Advance(model, 1);
            Assert.That(model.PresentationActions, Is.Empty);
            Assert.That(model.Units.Single().DamageDone, Is.Zero);
            Assert.That(model.Units.Single().SkillCharge, Is.EqualTo(model.Clock).Within(.001));
        }

        [Test]
        public void ALongSourceSkillDoesNotReplaceTheConfiguredLogicLockOrSPRequirement()
        {
            var model = Fixture(out _, "B22"); var profile = Profile(); profile.SkillDuration = 10; profile.SkillContact = 4;
            model.SetAnimationProfile("B22", profile); Enter(model); var actor = model.Units.Single();
            actor.SkillCharge = actor.SkillRequired; float required = actor.SkillRequired; Advance(model, .025f);
            var skill = model.PresentationActions.Single(action => action.Skill);
            Assert.That(skill.EndAt - skill.At, Is.EqualTo(5).Within(.001));
            Assert.That(actor.PresentationMoveLockUntil - skill.At, Is.EqualTo(1).Within(.001));
            Assert.That(actor.SkillRequired, Is.EqualTo(required));
            Assert.That(model.SkillCasts.Count, Is.EqualTo(1));
        }

        [Test]
        public void MissingProfilePreservesImmediatePureModelCompatibility()
        {
            var model = Fixture(out _, "B22"); Enter(model); Advance(model, .025f);
            var action = model.PresentationActions.Single();
            Assert.That(action.ContactAt, Is.EqualTo(action.At)); Assert.That(model.Units.Single().DamageDone, Is.GreaterThan(0));
            Assert.That(action.Rate, Is.EqualTo(1));
        }

        [Test]
        public void KnownProjectileFlightSeparatesSourceReleaseFromItsActualImpact()
        {
            var model = Fixture(out _, "B22"); var profile = Profile(); profile.AttackFlightSeconds = .6f;
            model.SetAnimationProfile("B22", profile); Enter(model); Advance(model, .6f);
            var action = model.PresentationActions.Single();
            Assert.That(action.LaunchedAt - action.At, Is.EqualTo(.5).Within(.001));
            Assert.That(action.ContactAt - action.LaunchedAt, Is.EqualTo(.3).Within(.001));
            Assert.That(model.Units.Single().DamageDone, Is.Zero, "The real hit cannot precede its established flight.");
            Advance(model, .3f); Assert.That(model.Units.Single().DamageDone, Is.GreaterThan(0));
        }

        [Test]
        public void EstablishedProjectileSpeedUsesActualDistanceAndPreservesOriginalArcMetadata()
        {
            var model = Fixture(out _, "B22"); var profile = Profile();
            profile.ProjectileSpeed = 1; profile.ProjectileDelay = .2f; profile.ProjectileRaiseHeight = 1.25f; profile.ProjectileMovement = "Parabola";
            model.SetAnimationProfile("B22", profile); Enter(model); Advance(model, .65f);
            var action = model.PresentationActions.Single();
            Assert.That(action.LaunchedAt - action.At, Is.EqualTo(.6).Within(.001));
            Assert.That(action.ContactAt - action.LaunchedAt, Is.EqualTo(.25).Within(.001));
            Assert.That(action.ProjectileRaiseHeight, Is.EqualTo(1.25f)); Assert.That(action.ProjectileMovement, Is.EqualTo("Parabola"));
            Assert.That(model.Units.Single().DamageDone, Is.Zero);
            Advance(model, .3f); Assert.That(model.Units.Single().DamageDone, Is.GreaterThan(0));
        }

        [Test]
        public void CurrentCancellationPolicyAlsoCancelsAReleasedFlightWhenItsOwnerDies()
        {
            // This explicitly records the remaining release/dead-owner limitation. The
            // damage engine currently requires a living native attacker at impact.
            var model = Fixture(out _, "B22"); var profile = Profile(); profile.AttackFlightSeconds = .6f;
            model.SetAnimationProfile("B22", profile); Enter(model); Advance(model, .6f);
            var actor = model.Units.Single();
            Assert.That(model.Clock, Is.GreaterThan(model.PresentationActions.Single(action => action.ActorId == actor.Id && !action.Skill).LaunchedAt));
            model.Defenders.Single().Attack = 100000; model.Defenders.Single().Range = 10;
            Advance(model, .3f);
            Assert.That(actor.Alive, Is.False);
            Assert.That(model.PresentationActions.Single(action => action.ActorId == actor.Id && !action.Skill).Cancelled, Is.True);
            Assert.That(actor.DamageDone, Is.Zero);
        }

        [Test]
        public void OverlappingDelayedSkillsKeepTheirOriginalCastOrdinalAndAllFourSegments()
        {
            var model = Fixture(out _, "B22"); var profile = Profile();
            profile.HasOrdinaryAttack = false; profile.SkillDuration = 6; profile.SkillContact = 4;
            model.SetAnimationProfile("B22", profile); Enter(model); var actor = model.Units.Single();
            actor.SkillCharge = actor.SkillRequired; Advance(model, 1.1f);
            Assert.That(model.SkillCasts.Count, Is.EqualTo(1));
            Assert.That(actor.DamageDone, Is.Zero);
            actor.SkillCharge = actor.SkillRequired; Advance(model, 2.8f);
            Assert.That(model.SkillCasts.Count, Is.EqualTo(2));
            Assert.That(actor.DamageDone, Is.EqualTo(actor.Attack * 4 * (.55f + .57f)).Within(.02));
            Assert.That(model.Hits.Count(hit => hit.Skill && !hit.Heal && hit.FromId == actor.Id), Is.EqualTo(8));
        }

        [Test]
        public void PauseFreezesAQueuedNativeSkillAndItsSPAlreadySpentAtStart()
        {
            var model = Fixture(out _, "B22"); var profile = Profile();
            profile.HasOrdinaryAttack = false; profile.SkillDuration = 6; profile.SkillContact = 4;
            model.SetAnimationProfile("B22", profile); Enter(model); var actor = model.Units.Single();
            actor.SkillCharge = actor.SkillRequired; Advance(model, .2f);
            Assert.That(actor.SkillCharge, Is.LessThan(actor.SkillRequired));
            double clock = model.Clock; float charge = actor.SkillCharge;
            model.Paused = true; Advance(model, 3);
            Assert.That(model.Clock, Is.EqualTo(clock)); Assert.That(actor.SkillCharge, Is.EqualTo(charge)); Assert.That(actor.DamageDone, Is.Zero);
            model.Paused = false; Advance(model, 2); Assert.That(actor.DamageDone, Is.GreaterThan(0));
        }

        [Test]
        public void RealDeathCancelsAQueuedSkillWithoutRefundingItsStartedCast()
        {
            var model = Fixture(out _, "B22"); var profile = Profile();
            profile.SkillDuration = 6; profile.SkillContact = 4;
            model.SetAnimationProfile("B22", profile); model.Defenders.Single().Attack = 100000; model.Defenders.Single().Range = 10;
            Enter(model); var actor = model.Units.Single(); actor.SkillCharge = actor.SkillRequired; Advance(model, 2.2f);
            Assert.That(model.SkillCasts.Count, Is.EqualTo(1)); Assert.That(actor.Alive, Is.False);
            Assert.That(model.PresentationActions.Single(action => action.Skill).Cancelled, Is.True);
            Assert.That(actor.DamageDone, Is.Zero); Assert.That(actor.SkillCharge, Is.LessThan(actor.SkillRequired));
        }

        static FormationAnimationProfile WisadelProjectileProfile() => new FormationAnimationProfile
        {
            AttackDuration=2.1f, AttackContact=.8f, SkillDuration=10, SkillContact=1.36666667f,
            BaseRate=2, HasOrdinaryAttack=false, SkillProjectileSpeed=10, SkillProjectileMovement="AdvancedMovement", SkillProjectileWindup=.533333361f
        };

        [Test]
        public void WisadelThirdSkillReleasesAtTheEstablishedSourcePointAndHitsOnlyAfterFlight()
        {
            var model=Fixture(out _,"WIS");model.SetAnimationProfile("WIS",WisadelProjectileProfile());Enter(model);
            var actor=model.Units.Single();actor.SkillCharge=actor.SkillRequired;
            Advance(model,.7f);
            var cast=model.PresentationActions.Single(action=>action.ActorId==actor.Id&&action.Skill&&!action.ProjectileOnly);
            Assert.That(cast.LaunchedAt-cast.At,Is.EqualTo(1.36666667/2).Within(.00001));
            Assert.That(model.PresentationActions.Any(action=>action.ProjectileOnly),Is.False);
            Assert.That(actor.DamageDone,Is.Zero);
            Advance(model,.025f);
            var projectile=model.PresentationActions.Single(action=>action.ProjectileOnly);
            Assert.That(projectile.ParentSequence,Is.EqualTo(cast.Sequence));Assert.That(projectile.ProjectileIndex,Is.Zero);
            Assert.That(projectile.At-cast.At,Is.EqualTo(.83333331/2).Within(.00001));
            Assert.That(projectile.Animation,Is.EqualTo("Skill_3_Loop"));Assert.That(projectile.ProjectileMovement,Is.EqualTo("AdvancedMovement"));
            Assert.That(projectile.LaunchedAt,Is.EqualTo(cast.LaunchedAt).Within(.00001));
            Assert.That(projectile.ContactAt-projectile.LaunchedAt,Is.EqualTo(.5).Within(.00001));
            Assert.That(actor.DamageDone,Is.Zero,"Actual area damage must wait for the real flight.");
            Advance(model,(float)(projectile.ContactAt-model.Clock+.025));
            Assert.That(actor.DamageDone,Is.EqualTo(actor.Attack*4.5f).Within(.01));
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1));
        }

        [TestCase(1,6)]
        [TestCase(3,12)]
        public void WisadelEveryRealShellHasOneOrderedReleaseAndOneDamageWithoutStartingAnotherCast(int star,int shells)
        {
            var model=Fixture(out _,"WIS");var actor=model.Units.Single();actor.Star=star;
            model.SetAnimationProfile("WIS",WisadelProjectileProfile());Enter(model);actor.SkillCharge=actor.SkillRequired;
            var projectiles=new Dictionary<int,FormationPresentationAction>();
            for(int frame=0;frame<Mathf.CeilToInt((shells*1.1f+1.2f)/.025f);frame++)
            {
                model.Advance(.025f);
                foreach(var projectile in model.PresentationActions.Where(action=>action.ActorId==actor.Id&&action.ProjectileOnly))projectiles[projectile.Sequence]=projectile;
            }
            var ordered=projectiles.Values.OrderBy(action=>action.ProjectileIndex).ToArray();
            Assert.That(ordered.Length,Is.EqualTo(shells));
            CollectionAssert.AreEqual(Enumerable.Range(0,shells),ordered.Select(action=>action.ProjectileIndex));
            Assert.That(ordered.Select(action=>action.ParentSequence).Distinct().Count(),Is.EqualTo(1));
            foreach(var gap in ordered.Skip(1).Zip(ordered,(later,earlier)=>later.LaunchedAt-earlier.LaunchedAt))Assert.That(gap,Is.EqualTo(1.1).Within(.00001));
            Assert.That(ordered.All(action=>!action.Cancelled&&action.Skill),Is.True);
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1),"Projectile metadata must never create additional casts or consume more SP.");
            Assert.That(actor.DamageDone,Is.EqualTo(actor.Attack*4.5f*shells).Within(.1));
            var cast=model.PresentationActions.Single(action=>action.ActorId==actor.Id&&action.Skill&&!action.ProjectileOnly);
            Assert.That(actor.PresentationStartedAt,Is.EqualTo(cast.At));
        }

        [Test]
        public void PausingDuringWisadelFlightFreezesImpactAndDoesNotDuplicateTheRelease()
        {
            var model=Fixture(out _,"WIS");model.SetAnimationProfile("WIS",WisadelProjectileProfile());Enter(model);
            var actor=model.Units.Single();actor.SkillCharge=actor.SkillRequired;Advance(model,.725f);
            var projectile=model.PresentationActions.Single(action=>action.ProjectileOnly);double clock=model.Clock;
            model.Paused=true;Advance(model,2);
            Assert.That(model.Clock,Is.EqualTo(clock));Assert.That(actor.DamageDone,Is.Zero);
            Assert.That(model.PresentationActions.Count(action=>action.ProjectileOnly),Is.EqualTo(1));
            model.Paused=false;Advance(model,(float)(projectile.ContactAt-model.Clock+.025));
            Assert.That(actor.DamageDone,Is.EqualTo(actor.Attack*4.5f).Within(.01));
            Assert.That(model.PresentationActions.Single(action=>action.ProjectileOnly),Is.SameAs(projectile));
        }

        [Test]
        public void CurrentOwnerDeathPolicyCancelsWisadelReleasedFlightAndAllLaterShells()
        {
            var model=Fixture(out _,"WIS");model.SetAnimationProfile("WIS",WisadelProjectileProfile());Enter(model);
            var actor=model.Units.Single();actor.Position=new Vector2(13,3);actor.SkillCharge=actor.SkillRequired;Advance(model,.725f);
            var projectile=model.PresentationActions.Single(action=>action.ProjectileOnly);
            Assert.That(projectile.LaunchedAt,Is.LessThan(model.Clock));Assert.That(projectile.ContactAt,Is.GreaterThan(model.Clock));
            model.Defenders.Single().Attack=100000;model.Defenders.Single().Range=10;Advance(model,1);
            Assert.That(actor.Alive,Is.False);Assert.That(projectile.Cancelled,Is.True);Assert.That(actor.DamageDone,Is.Zero);
            Assert.That(model.PresentationActions.Count(action=>action.ProjectileOnly),Is.EqualTo(1));
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1));
        }

        [Test]
        public void WisadelFirstSkillRecordsOnlyItsRealSegmentsAndDoesNotInventUnknownFlight()
        {
            var model=Fixture(out var run,"WIS");run.WisadelSkill=1;
            Assert.That(model.Defenders.Count,Is.EqualTo(1));Assert.That(model.Defenders.Single().MaxHealth,Is.EqualTo(100000000));
            var profile=WisadelProjectileProfile();profile.SkillContact=.6f;profile.SkillProjectileSpeed=0;profile.SkillProjectileWindup=0;
            model.SetAnimationProfile("WIS",profile);Enter(model);var actor=model.Units.Single();actor.SkillCharge=actor.SkillRequired;Advance(model,1.5f);
            var projectiles=model.PresentationActions.Where(action=>action.ProjectileOnly).OrderBy(action=>action.ProjectileIndex).ToArray();
            Assert.That(projectiles.Length,Is.EqualTo(4));Assert.That(projectiles.All(action=>action.Animation=="Skill_1"&&action.ContactAt==action.LaunchedAt),Is.True);
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1));Assert.That(actor.DamageDone,Is.EqualTo(actor.Attack*2.4f*4).Within(.02));
        }

        [Test]
        public void WisadelFirstSkillDoesNotInventLaterShotsAfterItsOnlyWeakTargetDies()
        {
            var model=Fixture(out var run,"WIS");run.WisadelSkill=1;model.Defenders.Single().MaxHealth=10;
            var profile=WisadelProjectileProfile();profile.SkillContact=.6f;profile.SkillProjectileSpeed=0;profile.SkillProjectileWindup=0;
            model.SetAnimationProfile("WIS",profile);Enter(model);var actor=model.Units.Single();actor.SkillCharge=actor.SkillRequired;Advance(model,1.5f);
            Assert.That(model.Defenders.Single().Alive,Is.False);Assert.That(actor.DamageDone,Is.EqualTo(10));
            Assert.That(model.PresentationActions.Count(action=>action.ProjectileOnly),Is.EqualTo(1));
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1));
        }
    }
}
