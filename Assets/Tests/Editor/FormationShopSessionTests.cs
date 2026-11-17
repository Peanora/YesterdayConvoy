using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationShopSessionTests
    {
        static FormationBattleModel Bind(FormationShopSession session)
        {
            var model = new FormationBattleModel(session.Seed);
            session.ApplyTo(model); return model;
        }

        static void AssertStock(ShopTrialModel shop)
        {
            shop.Validate();
            foreach (var role in shop.Roles)
                Assert.That(shop.Available(role.Id) + shop.Offers.Count(id => id == role.Id) + shop.Bench.Where(piece => piece.Id == role.Id).Sum(piece => ShopTrialModel.Copies(piece.Star)), Is.EqualTo(shop.Capacity(role.Id)), role.Id);
        }

        static string ShopSnapshot(ShopTrialModel shop) => shop.Gold + ":" + shop.Level + ":" + shop.XP + ":" + shop.Locked + "|" +
            string.Join(",", shop.Offers) + "|" + string.Join(",", shop.Bench.Select(piece => piece.Instance + ":" + piece.Id + ":" + piece.Star)) + "|" +
            string.Join(",", shop.Roles.Select(role => role.Id + ":" + shop.Available(role.Id)));

        static void FinishFixture(FormationBattleModel model)
        {
            Assert.That(model.StartBattle(), Is.True);
            foreach (var defender in model.Defenders) { defender.Health = 0; defender.Alive = false; }
            model.Units.First().Position = new Vector2(13, 2);
            model.Advance(.025f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void StartingGrantOccupiesRealStockAndOnlyOwnedLineupBecomesCombatBodies()
        {
            var session = new FormationShopSession(20261117);
            var model = Bind(session);
            Assert.That(session.Shop.Gold, Is.EqualTo(12)); Assert.That(session.Shop.Level, Is.EqualTo(3));
            Assert.That(session.Shop.Bench.Count, Is.EqualTo(3)); Assert.That(model.Units.Count, Is.EqualTo(3));
            Assert.That(session.Seats.Lineup(session.Shop.Bench).Select(piece => piece.Id), Is.EquivalentTo(new[] { "CARD_M07", "CARD_M10", "CARD_M08" }));
            Assert.That(model.Capacity, Is.EqualTo(session.Shop.Level));
            Assert.That(model.Units.All(unit => unit.Enabled && session.Shop.IsLineupInstance(unit.Instance)), Is.True);
            Assert.That(model.Units.Select(unit => unit.Id).Intersect(model.Defenders.Select(defender => defender.Id)), Is.Empty);
            Assert.That(session.Shop.Roles.Single(role => role.Id == "CARD_M10").Cost, Is.EqualTo(2));
            Assert.That(session.Shop.Roles.Single(role => role.Id == "CARD_M17").Cost, Is.EqualTo(3));
            AssertStock(session.Shop);
        }

        [Test]
        public void PurchasePaysGoldAndReservesStayOutOfCombatUntilMovedIntoALevelSeat()
        {
            var session = new FormationShopSession(20261117); var model = Bind(session);
            string offer = session.Shop.Offers[0]; int gold = session.Shop.Gold;
            var purchase = session.Purchase(0);
            Assert.That(purchase.Success, Is.True); Assert.That(session.Shop.Gold, Is.EqualTo(gold - session.Shop.Definition(offer).Cost));
            Assert.That(session.Shop.Offers[0], Is.Null);
            session.ApplyTo(model);
            Assert.That(model.Units.Count, Is.EqualTo(3));
            Assert.That(session.Seats.InLineup(purchase.Instance), Is.False);
            Assert.That(session.Move(purchase.Instance, true, 3).Success, Is.False, "Level 3 has no fourth lineup seat.");
            Assert.That(session.BuyExperience().Success, Is.True);
            Assert.That(session.BuyExperience().Success, Is.True);
            session.ApplyTo(model);
            Assert.That(model.Capacity, Is.EqualTo(session.Shop.Level));
            Assert.That(session.Shop.Level, Is.EqualTo(4));
            // A duplicate kind remains subject to one-name-per-lineup even after gaining population.
            bool duplicate = session.Seats.Lineup(session.Shop.Bench).Any(piece => piece.Id == offer);
            Assert.That(session.Move(purchase.Instance, true, 3).Success, Is.EqualTo(!duplicate));
            AssertStock(session.Shop);
        }

        [Test]
        public void FailedTransactionsAndAllCombatTimeShopActionsLeaveTheEconomyUnchanged()
        {
            var session = new FormationShopSession(51); var model = Bind(session);
            model.StartBattle(); string before = ShopSnapshot(session.Shop);
            Assert.That(session.Purchase(0).Success, Is.False);
            Assert.That(session.Refresh().Success, Is.False);
            Assert.That(session.BuyExperience().Success, Is.False);
            Assert.That(session.Sell(session.Shop.Bench[0].Instance).Success, Is.False);
            Assert.That(session.Move(session.Shop.Bench[0].Instance, false, 0).Success, Is.False);
            Assert.That(session.SetLocked(true), Is.False);
            Assert.That(ShopSnapshot(session.Shop), Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(() => session.ApplyTo(model));
            Assert.That(session.Retry(), Is.True);
            session.SetLocked(true); before = ShopSnapshot(session.Shop);
            Assert.That(session.Refresh().Success, Is.False);
            Assert.That(session.Purchase(9).Success, Is.False);
            Assert.That(ShopSnapshot(session.Shop), Is.EqualTo(before));
        }

        [Test]
        public void RealCascadeMergeKeepsFieldInstanceAndSpawnThroughFourStars()
        {
            var session = new FormationShopSession(71);
            session.Shop.Preset(new[]
            {
                new ShopTrialModel.Piece { Id = "CARD_M08", Star = 3 }, new ShopTrialModel.Piece { Id = "CARD_M08", Star = 3 },
                new ShopTrialModel.Piece { Id = "CARD_M08", Star = 2 }, new ShopTrialModel.Piece { Id = "CARD_M08", Star = 2 },
                new ShopTrialModel.Piece { Id = "CARD_M08", Star = 1 }, new ShopTrialModel.Piece { Id = "CARD_M08", Star = 1 }
            }, new[] { "CARD_M08" }, level: 3, xp: 0, gold: 12);
            var model = Bind(session); var original = model.Units.Single();
            Assert.That(model.TryPlace(original.Slot, 1, 7), Is.True); session.SavePositions(model);
            int instance = original.Instance; float beforeHealth = original.MaxHealth, beforeAttack = original.Attack;
            var purchase = session.Purchase(0);
            Assert.That(purchase.Success, Is.True); Assert.That(purchase.FinalStar, Is.EqualTo(4));
            Assert.That(purchase.Instance, Is.EqualTo(instance)); Assert.That(session.Shop.Gold, Is.EqualTo(11));
            session.ApplyTo(model);
            Assert.That(model.Units.Single().Instance, Is.EqualTo(instance));
            Assert.That(model.Units.Single().Star, Is.EqualTo(4));
            Assert.That(model.Units.Single().Spawn, Is.EqualTo(new Vector2(1, 7)));
            Assert.That(model.Units.Single().MaxHealth, Is.GreaterThan(beforeHealth));
            Assert.That(model.Units.Single().Attack, Is.GreaterThan(beforeAttack));
            Assert.That(session.Shop.Available("CARD_M08"), Is.EqualTo(3));
            AssertStock(session.Shop);
        }

        [Test]
        public void SellReturnsCopiesAndReplacementOfSameKindKeepsItsPriorGridCell()
        {
            var session = new FormationShopSession(83);
            session.Shop.Preset(new[]
            {
                new ShopTrialModel.Piece { Id = "CARD_M07", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M07", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M10", Star = 1 }
            }, Array.Empty<string>(), level: 3, xp: 0, gold: 12);
            var model = Bind(session); var soldier = model.Units.Single(unit => unit.ArtId == "M07");
            model.TryPlace(soldier.Slot, 0, 6); session.SavePositions(model);
            int oldInstance = soldier.Instance, available = session.Shop.Available("CARD_M07");
            Assert.That(session.Sell(oldInstance).Success, Is.True);
            session.ApplyTo(model);
            var replacement = model.Units.Single(unit => unit.ArtId == "M07");
            Assert.That(replacement.Instance, Is.Not.EqualTo(oldInstance));
            Assert.That(replacement.Spawn, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(session.Shop.Available("CARD_M07"), Is.EqualTo(available + 1));
            Assert.That(session.Shop.Gold, Is.EqualTo(13));
            AssertStock(session.Shop);
        }

        [Test]
        public void RetryKeepsOffersGoldAndRandomStateAndCannotProduceRewards()
        {
            var session = new FormationShopSession(113); var control = new FormationShopSession(113);
            var model = Bind(session); Bind(control);
            string before = ShopSnapshot(session.Shop);
            model.StartBattle(); model.Advance(2);
            Assert.That(session.SettleWin(1), Is.Null);
            Assert.That(session.Retry(), Is.True);
            Assert.That(ShopSnapshot(session.Shop), Is.EqualTo(before));
            Assert.That(session.Round, Is.EqualTo(1)); Assert.That(model.Clock, Is.Zero);
            Assert.That(session.Refresh().Success, Is.True); Assert.That(control.Refresh().Success, Is.True);
            Assert.That(ShopSnapshot(session.Shop), Is.EqualTo(ShopSnapshot(control.Shop)), "Retry must not consume or reset the shop RNG.");
        }

        [Test]
        public void VictoryRewardIsIdempotentAndRoundAdvancePreservesCardsPositionsAndLockedOffers()
        {
            var session = new FormationShopSession(20261117); var model = Bind(session);
            var soldier = model.Units.Single(unit => unit.ArtId == "M07");
            model.TryPlace(soldier.Slot, 1, 6); session.SavePositions(model);
            session.SetLocked(true); string offers = string.Join(",", session.Shop.Offers);
            FinishFixture(model);
            int goldBefore = session.Shop.Gold;
            var reward = session.SettleWin(1);
            Assert.That(reward, Is.Not.Null); Assert.That(session.Shop.Gold, Is.EqualTo(goldBefore + reward.Total));
            int paidGold = session.Shop.Gold, paidXP = session.Shop.XP;
            Assert.That(session.SettleWin(1), Is.SameAs(reward));
            Assert.That(session.Shop.Gold, Is.EqualTo(paidGold)); Assert.That(session.Shop.XP, Is.EqualTo(paidXP));
            Assert.That(session.Retry(), Is.True); FinishFixture(model);
            Assert.That(session.SettleWin(1), Is.SameAs(reward)); Assert.That(session.Shop.Gold, Is.EqualTo(paidGold));
            Assert.That(session.AdvanceRound(), Is.True);
            Assert.That(session.Round, Is.EqualTo(2)); Assert.That(model.Round, Is.EqualTo(2));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            Assert.That(model.Units.Single(unit => unit.Instance == soldier.Instance).Spawn, Is.EqualTo(new Vector2(1, 6)));
            Assert.That(string.Join(",", session.Shop.Offers), Is.EqualTo(offers));
            var rebound = Bind(session);
            Assert.That(rebound.Round, Is.EqualTo(2)); Assert.That(rebound.Units.Count, Is.EqualTo(3));
            AssertStock(session.Shop);
        }

        [Test]
        public void RosterImportRejectsInvalidSnapshotsAtomicallyAndSupportsRealPopulationAndStars()
        {
            var model = new FormationBattleModel();
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry { Instance = 201, ArtId = "M08", Star = 4, Spawn = new Vector2(2, 3) } }, 10), Is.True);
            Assert.That(model.Units.Single().Id, Is.LessThan(-1)); Assert.That(model.Capacity, Is.EqualTo(10));
            Assert.That(model.Units.Single().Star, Is.EqualTo(4));
            var imported = model.Units.Single();
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry { Instance = 202, ArtId = "M10", Star = 4, Spawn = new Vector2(2, 3) } }, 10), Is.False);
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry { Instance = 202, ArtId = "M10", Star = 1, Spawn = new Vector2(6, 3) } }, 10), Is.False);
            Assert.That(model.Units.Single(), Is.SameAs(imported));
            model.StartBattle();
            Assert.That(model.ApplyRoster(Array.Empty<FormationRosterEntry>(), 3), Is.False);
            model.Retry(); Assert.That(model.ApplyRoster(Array.Empty<FormationRosterEntry>(), 3), Is.True);
            Assert.That(model.Units.Count, Is.Zero); Assert.That(model.StartBattle(), Is.False);
        }

        [Test]
        public void NewChallengeCreatesFreshStockMoneyAndLineupWithoutChangingOldSession()
        {
            var old = FormationShopSession.StartNew(301); var model = Bind(old);
            old.Purchase(0); old.ApplyTo(model);
            string before = ShopSnapshot(old.Shop);
            var fresh = FormationShopSession.StartNew(302);
            Assert.That(FormationShopSession.Current, Is.SameAs(fresh));
            Assert.That(fresh.Shop.Gold, Is.EqualTo(12)); Assert.That(fresh.Shop.Bench.Count, Is.EqualTo(3));
            Assert.That(fresh.Round, Is.EqualTo(1)); Assert.That(ShopSnapshot(old.Shop), Is.EqualTo(before));
            AssertStock(fresh.Shop);
        }
    }
}
