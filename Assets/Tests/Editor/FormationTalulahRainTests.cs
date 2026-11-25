using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationTalulahRainTests
    {
        static FormationBattleModel RainBattle(FormationBattleTerrainLayout terrain = null, bool snowBoss = false)
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

            if (snowBoss) FormationCampaignCombatTests.ReachNode(run, "3-7", collectDrops: false);

            string card = FormationCampaignRules.Find("TAL").CardId;
            for (int cost = 3; cost < 5; cost++)
            {
                Assert.That(shop.GrantAtLeast(card, 2).Success, Is.True);
                Assert.That(shop.ResolveTalulah(true).Success, Is.True);
            }
            Assert.That(run.TalulahCost, Is.EqualTo(5));
            shop.Preset(new[] { new ShopTrialModel.Piece { Id = card, Star = 1 } }, System.Array.Empty<string>(), 10, 0, 80);
            var model = new FormationBattleModel(seed, terrain);
            model.BindCampaign(run);
            var piece = shop.Bench.Single();
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry
            { Instance = piece.Instance, ArtId = card, Spawn = new Vector2(8, 3) } }, 10), Is.True);
            model.SetAnimationProfile(card, new FormationAnimationProfile
            {
                AttackDuration = 2.3333333f, AttackContact = 1.0333333f,
                SkillDuration = 1.9666667f, SkillContact = 1.0333333f, BaseRate = 2
            });
            return model;
        }

        [Test]
        public void FiveCostRainCancelsPendingAttackThenResumesAfterTheBurnExpires()
        {
            var model = RainBattle();
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
            var burns = hits.Where(hit => hit.Skill && hit.Secondary).OrderBy(hit => hit.At).ToArray();
            Assert.That(burns.Length, Is.EqualTo(8));
            for (int i = 0; i < burns.Length; i++)
            {
                Assert.That(burns[i].At, Is.EqualTo(rainEnd - 7 + i).Within(.0001));
                Assert.That(burns[i].Amount, Is.EqualTo(talulah.Attack * .4f).Within(.1f));
                Assert.That(ShowsDamageNumber(burns[i]), Is.True, "The burn is real damage even below the high-hit cutoff.");
            }
            Assert.That(hits.Any(hit => !hit.Skill), Is.False);
            Assert.That(model.PresentationActions.Any(action => action.ActorId == talulah.Id && !action.Skill && !action.Cancelled), Is.False);

            for (int frame = 0; frame < 30; frame++) Step();
            var resumed = hits.Single(hit => !hit.Skill);
            Assert.That(resumed.Action.At, Is.GreaterThan(rainEnd));
        }

        [Test]
        public void GlobalRainStrikesAcrossAnObstacleAfterItsInitialTargetDies()
        {
            var terrain = new FormationBattleTerrainLayout(new[]
            {
                new FormationBattleTerrainRegion(new RectInt(13, 0, 1, FormationBattleModel.Height), .48f, false)
            });
            var model = RainBattle(terrain);
            model.Defenders.RemoveRange(2, model.Defenders.Count - 2);
            var initial = model.Defenders[0];
            var acrossWall = model.Defenders[1];
            initial.Position = new Vector2(11, 3);
            initial.MaxHealth = 1;
            acrossWall.Position = new Vector2(18, 3);
            acrossWall.MaxHealth = 1000000;
            Assert.That(model.StartBattle(), Is.True);
            var talulah = model.Units.Single();
            talulah.Position = new Vector2(11.5f, 3);
            talulah.SkillCharge = talulah.SkillRequired;
            Assert.That(terrain.TryApproach(talulah.Position, acrossWall.Position, talulah.Range, out _, out _), Is.False);

            model.Advance(2.5f);

            Assert.That(initial.Alive, Is.False);
            Assert.That(model.Hits.Count(hit => hit.FromId == talulah.Id && hit.ToId == acrossWall.Id &&
                hit.Action?.Skill == true && !hit.Secondary && hit.Amount > 0), Is.EqualTo(6));
        }

        [Test]
        public void SnowBossTransitionBlocksRainUntilTheSecondHealthPoolIsActive()
        {
            var model = RainBattle(snowBoss: true);
            var boss = model.Defenders.Single();
            Assert.That(model.StartBattle(), Is.True);
            var talulah = model.Units.Single();
            talulah.Position = boss.Position + Vector2.left;
            talulah.SkillCharge = talulah.SkillRequired;
            boss.Health = 1;
            for (int i = 0; i < 40 && !model.SnowBossTransitionActive(boss); i++) model.Advance(.025f);
            Assert.That(model.SnowBossTransitionActive(boss), Is.True);
            double transition = model.Clock;

            model.Advance(1.95f);
            Assert.That(model.Hits.Any(hit => hit.FromId == talulah.Id && hit.At > transition), Is.False);
            model.Advance(.10f);
            Assert.That(boss.SnowBossPhase, Is.EqualTo(2));
            var burn = model.Hits.Single(hit => hit.FromId == talulah.Id && hit.ToId == boss.Id && hit.At > transition && hit.Secondary);
            Assert.That(burn.Secondary && ShowsDamageNumber(burn), Is.True);
            Assert.That(burn.Amount, Is.GreaterThan(0));
            Assert.That(boss.Health, Is.LessThan(boss.MaxHealth));
        }

        static bool ShowsDamageNumber(FormationHit hit) => (bool)typeof(FormationHit)
            .GetProperty("ShowDamageNumber", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hit);
    }
}
