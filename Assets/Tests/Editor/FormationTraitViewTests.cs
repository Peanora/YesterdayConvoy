using System;
using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationTraitViewTests
    {
        static (ShopTrialModel shop, FormationCampaignRun run) Create(params string[] ids)
        {
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u => new ShopTrialModel.Role { Id = u.CardId, Name = u.Name, Cost = u.Cost }), 29);
            shop.EnableFormationEconomy(); shop.Preset(Array.Empty<ShopTrialModel.Piece>(), Array.Empty<string>(), 8, 0, 8);
            var run = new FormationCampaignRun(shop, 29);
            int safety = 0;
            while (run.HasBlockingChoice) { Assert.That(++safety, Is.LessThan(50)); Assert.That(run.Choose(0).Success, Is.True); }
            shop.Preset(ids.Select(id => new ShopTrialModel.Piece { Id = FormationCampaignRules.Find(id).CardId, Star = 1 }), Array.Empty<string>(), 8, 0, 8);
            return (shop, run);
        }
        static void Lineup(ShopTrialModel shop, params int[] indices)
        {
            var selected = indices.Select(i => shop.Bench[i]).ToArray();
            Assert.That(shop.SetLineup(selected.Select(p => p.Id), selected.Select(p => p.Instance)), Is.True);
        }

        [Test]
        public void NativeRosterShowsMissingReserveAndLineupWithoutCountingDuplicateCopies()
        {
            var (shop, run) = Create("M21", "M21", "M25"); Lineup(shop, 0);
            var initial = FormationTraitView.For(run, shop, "复苏");
            Assert.That(initial.Count, Is.EqualTo(1)); Assert.That(initial.ActiveLevel, Is.Zero); Assert.That(initial.NextLevel, Is.EqualTo(2));
            Assert.That(initial.Members.Count(m => m.CardId == "CARD_M21"), Is.EqualTo(1));
            Assert.That(initial.Members.Single(m => m.CardId == "CARD_M21").State, Is.EqualTo(FormationTraitMemberState.Lineup));
            Assert.That(initial.Members.Single(m => m.CardId == "CARD_M25").State, Is.EqualTo(FormationTraitMemberState.Reserve));
            Assert.That(initial.Members.Any(m => m.State == FormationTraitMemberState.Missing), Is.True);
            Lineup(shop, 0, 2);
            var active = FormationTraitView.For(run, shop, "复苏");
            Assert.That(active.Count, Is.EqualTo(2)); Assert.That(active.ActiveLevel, Is.EqualTo(2)); Assert.That(active.NextLevel, Is.EqualTo(4));
            Assert.That(initial.Count, Is.EqualTo(1), "The earlier read-only view is a snapshot, not a second mutable lineup.");
        }

        [Test]
        public void BadgeRosterUsesRealEquipmentAndOnlyItsOnFieldOwnerCounts()
        {
            var (shop, run) = Create("M21", "B22"); Lineup(shop, 0, 1);
            var badge = run.AddItem("徽章:复苏");
            Assert.That(run.Equip(badge.Instance, shop.Bench[1].Instance).Success, Is.True);
            var active = FormationTraitView.For(run, shop, "复苏");
            Assert.That(active.Count, Is.EqualTo(2)); Assert.That(active.Active, Is.True);
            Assert.That(active.BadgeMembers.Single().CardId, Is.EqualTo("CARD_B22"));
            Assert.That(active.BadgeMembers.Single().FromBadge, Is.True);
            Assert.That(active.Members.All(m => m.CardId != "CARD_B22"), Is.True);
            Lineup(shop, 0);
            var reserve = FormationTraitView.For(run, shop, "复苏");
            Assert.That(reserve.Count, Is.EqualTo(1)); Assert.That(reserve.BadgeMembers.Single().State, Is.EqualTo(FormationTraitMemberState.Reserve));
            Assert.That(run.Unequip(badge.Instance).Success, Is.True);
            Assert.That(FormationTraitView.For(run, shop, "复苏").BadgeMembers, Is.Empty);
        }

        [Test]
        public void FormationColorsUseV07TierOrderInsteadOfLegacyMemberThresholds()
        {
            var reunion = FormationCampaignRules.FindRecord("整合");
            Assert.That(reunion.Levels.Select(level => FormationTraitView.GradeFor(reunion, level)),
                Is.EqualTo(new[] { SynergyVisualGrade.Bronze, SynergyVisualGrade.Silver, SynergyVisualGrade.Gold, SynergyVisualGrade.Prismatic }));
            var mudrock = FormationCampaignRules.FindRecord("泥岩");
            Assert.That(FormationTraitView.GradeFor(mudrock, 3), Is.EqualTo(SynergyVisualGrade.Silver));
            Assert.That(FormationTraitView.GradeFor(mudrock, 2), Is.EqualTo(SynergyVisualGrade.Bronze));
            Assert.That(FormationTraitView.GradeFor(mudrock, 0), Is.EqualTo(SynergyVisualGrade.Inactive));
        }

        [Test]
        public void RewardPresentationRecordsActualPayoutWithoutReplayingSettlement()
        {
            var (shop, run) = Create("M21");
            int gold = shop.Gold, refreshes = shop.FreeRefreshes, items = run.AllEquipment.Count;
            run.OpenBall("彩");
            FormationCampaignRunTests.CollectDrops(run);
            var reward = run.BallRewards.Single();
            Assert.That(reward.Color, Is.EqualTo("彩"));
            Assert.That(reward.Gold, Is.EqualTo(shop.Gold - gold));
            Assert.That(reward.Refreshes, Is.EqualTo(shop.FreeRefreshes - refreshes));
            Assert.That(reward.Items, Is.EqualTo(run.AllEquipment.Skip(items).Select(item => item.Key)));
            Assert.That(reward.Choice, Is.EqualTo(run.RewardChoiceTitle));
            while (run.HasBlockingChoice) Assert.That(run.Choose(0).Success, Is.True);
            Assert.That(run.SettleBattle(true, 1, 3, 0, false), Is.Not.Null);
            Assert.That(run.PendingBalls.Count(ball=>ball.Component==null), Is.EqualTo(2));
            Assert.That(run.SettleBattle(true, 1, 3, 0, false), Is.Null);
            FormationCampaignRunTests.CollectDrops(run);
            Assert.That(run.BallRewards.Count(ball=>ball.Color.Length>0), Is.EqualTo(3));
        }

        [Test]
        public void FullCatalogKeepsLockedTraitsAndCurrentTalulahCostWithoutMutatingRun()
        {
            var (shop, run) = Create("TAL", "M21"); Lineup(shop, 0, 1);
            int gold = shop.Gold, hope = run.Hope; var owned = shop.Bench.Select(p => p.Instance).ToArray();
            Assert.That(shop.Grant(shop.TalulahId, 2).Success, Is.True);
            Assert.That(shop.ResolveTalulah(true).Success, Is.True);
            var view = FormationTraitView.For(run, shop, "整合");
            Assert.That(view.Members.Single(m => m.CardId == shop.TalulahId).Cost, Is.EqualTo(4));
            var all = FormationTraitView.List(run, shop, true);
            Assert.That(all.Length, Is.EqualTo(FormationCampaignRules.AllFactions.Length + FormationCampaignRules.AllStyles.Length));
            Assert.That(all.Any(t => t.Count == 0 && !t.Active), Is.True);
            Assert.That(all.Select(t => t.Id).Distinct().Count(), Is.EqualTo(all.Length));
            Assert.That(FormationTraitView.List(run, shop).All(t => t.Count > 0), Is.True);
            Assert.That(FormationTraitView.For(run, shop, "不存在"), Is.Null);
            Assert.That(shop.Gold, Is.EqualTo(gold)); Assert.That(run.Hope, Is.EqualTo(hope));
            Assert.That(shop.Bench.Any(p => p.Instance == owned[1]), Is.True);
        }
    }
}
