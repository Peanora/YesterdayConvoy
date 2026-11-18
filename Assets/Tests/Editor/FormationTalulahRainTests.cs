using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationTalulahRainTests
    {
        [Test]
        public void FiveCostRainCancelsPendingAttackThenResumesAfterTheBurnExpires()
        {
            const int seed = 71;
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(unit => new ShopTrialModel.Role
            { Id = unit.CardId, Name = unit.Name, Cost = unit.Cost }), seed);
            shop.EnableFormationEconomy();
            shop.Preset(System.Array.Empty<ShopTrialModel.Piece>(), System.Array.Empty<string>(), 10, 0, 80);
            var run = new FormationCampaignRun(shop, seed);
            for (int choice = 0; run.HasBlockingChoice; choice++)
            {
                Assert.That(choice, Is.LessThan(32));
                Assert.That(run.Choose(0).Success, Is.True);
            }

            string card = FormationCampaignRules.Find("TAL").CardId;
            for (int cost = 3; cost < 5; cost++)
            {
                Assert.That(shop.GrantAtLeast(card, 2).Success, Is.True);
                Assert.That(shop.ResolveTalulah(true).Success, Is.True);
            }
            Assert.That(run.TalulahCost, Is.EqualTo(5));
            shop.Preset(new[] { new ShopTrialModel.Piece { Id = card, Star = 1 } }, System.Array.Empty<string>(), 10, 0, 80);
            var model = new FormationBattleModel(seed);
            model.BindCampaign(run);
            var piece = shop.Bench.Single();
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry
            { Instance = piece.Instance, ArtId = card, Spawn = new Vector2(8, 3) } }, 10), Is.True);
            model.SetAnimationProfile(card, new FormationAnimationProfile
            {
                AttackDuration = 2.3333333f, AttackContact = 1.0333333f,
                SkillDuration = 1.9666667f, SkillContact = 1.0333333f, BaseRate = 2
            });

            model.Defenders.RemoveRange(1, model.Defenders.Count - 1);
            var guard = model.Defenders.Single();
            guard.Position = new Vector2(15, 3);
            guard.MaxHealth = 1000000;
            guard.Attack = guard.Armor = guard.MagicResistance = 0;
            guard.Range = .01f;
            guard.Healer = false;
            Assert.That(model.StartBattle(), Is.True);
            var talulah = model.Units.Single();
            talulah.Position = new Vector2(14.5f, 3);
            talulah.SkillCharge = talulah.SkillRequired - .40f;

            var hits = new HashSet<FormationHit>();
            void Step()
            {
                model.Advance(.025f);
                hits.UnionWith(model.Hits.Where(hit => hit.FromId == talulah.Id));
            }
            Step();
            var pending = model.PresentationActions.Single(action => action.ActorId == talulah.Id && !action.Skill);
            for (int frame = 0; frame < 44; frame++) Step();

            var cast = model.PresentationActions.Single(action => action.ActorId == talulah.Id && action.Skill);
            Assert.That(cast.At, Is.LessThan(pending.ContactAt));
            Assert.That(pending.Cancelled, Is.True);
            double rainEnd = hits.Where(hit => hit.Action == cast && hit.Skill).Min(hit => hit.At) + 8;
            Assert.That(rainEnd - cast.ContactAt, Is.InRange(8d, 8.026d));
            double pausedAt = model.Clock;
            float chargeBeforeBurn = talulah.SkillCharge;
            model.Paused = true;
            model.Advance(.5f);
            Assert.That(model.Clock, Is.EqualTo(pausedAt));
            Assert.That(talulah.SkillCharge, Is.EqualTo(chargeBeforeBurn));
            model.Paused = false;
            while (model.Clock < rainEnd - .001) Step();

            Assert.That(talulah.SkillCharge, Is.GreaterThan(chargeBeforeBurn));
            Assert.That(hits.Count(hit => hit.Skill && hit.Action == cast && !hit.Secondary), Is.EqualTo(6));
            Assert.That(hits.Count(hit => hit.Skill && hit.Secondary), Is.EqualTo(8));
            Assert.That(hits.Any(hit => !hit.Skill), Is.False);
            Assert.That(model.PresentationActions.Any(action => action.ActorId == talulah.Id && !action.Skill && !action.Cancelled), Is.False);

            for (int frame = 0; frame < 30; frame++) Step();
            var resumed = hits.Single(hit => !hit.Skill);
            Assert.That(resumed.Action.At, Is.GreaterThan(rainEnd));
        }
    }
}
