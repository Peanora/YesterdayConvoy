using System;
using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationCampaignRunTests
    {
        static ShopTrialModel Shop(int seed=17)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(x=>new ShopTrialModel.Role{Id=x.CardId,Name=x.Name,Cost=x.Cost}),seed);
            shop.EnableFormationEconomy();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,8);return shop;
        }
        static void ResolveChoices(FormationCampaignRun run)
        {int safety=0;while(run.HasBlockingChoice){Assert.That(++safety,Is.LessThan(100));Assert.That(run.Choose(0).Success,Is.True);}}
        internal static void CollectDrops(FormationCampaignRun run)
        {
            ResolveChoices(run);
            foreach (var drop in run.PendingBalls.ToArray())
            {
                Assert.That(run.BeginBall(drop.Id), Is.Not.Null);
                Assert.That(run.CompleteBall(drop.Id), Is.Not.Null);
                ResolveChoices(run);
            }
        }
        [Test]
        public void GroundDropsRequireAnOpeningAndPayExactlyOnceOnArrival()
        {
            var shop = Shop(39); var run = new FormationCampaignRun(shop,39);
            ResolveChoices(run);
            Assert.That(run.SettleBattle(true,1,3,0,false),Is.Not.Null);
            Assert.That(run.PendingBalls.Count,Is.EqualTo(4));
            Assert.That(run.OpenedBalls,Is.Empty);
            Assert.That(run.AdvanceNode(),Is.False);
            var drop = run.PendingBalls.Last();
            int gold = shop.Gold, gear = run.Inventory.Count, count = shop.Bench.Count;
            Assert.That(run.CompleteBall(drop.Id),Is.Null);
            var payout = run.BeginBall(drop.Id);
            Assert.That(payout,Is.Not.Null);
            Assert.That(run.BeginBall(drop.Id),Is.Null);
            Assert.That(run.BeginBall(run.PendingBalls[0].Id),Is.Null);
            Assert.That(shop.Gold,Is.EqualTo(gold));
            Assert.That(run.Inventory.Count,Is.EqualTo(gear));
            Assert.That(shop.Bench.Count,Is.EqualTo(count));
            Assert.That(run.CompleteBall(drop.Id).Color,Is.EqualTo(drop.Color));
            Assert.That(shop.Gold,Is.EqualTo(gold+payout.Gold));
            Assert.That(run.Inventory.Count,Is.EqualTo(gear+payout.Items.Count));
            Assert.That(run.CompleteBall(drop.Id),Is.Null);
            Assert.That(run.BeginBall(drop.Id),Is.Null);
            CollectDrops(run);
            Assert.That(run.BallRewards.Count,Is.EqualTo(4));
            Assert.That(run.Inventory.Sum(item=>item.Key.Length==1?1:item.Key.Length==2?2:0),Is.GreaterThanOrEqualTo(2));
            Assert.That(run.AdvanceNode(),Is.True);
        }
        [Test]
        public void FormationEconomyKeepsLegacySeparateAndUsesFullExperienceCurve()
        {
            var shop=Shop();Assert.That(shop.NextLevelXP,Is.EqualTo(6));shop.AddExperience(128);Assert.That(shop.Level,Is.EqualTo(8));Assert.That(shop.XP,Is.Zero);
            shop.AddExperience(80);Assert.That(shop.Level,Is.EqualTo(9));shop.AddExperience(100);Assert.That(shop.Level,Is.EqualTo(10));
            var legacy=new ShopTrialModel(shop.Roles,17);legacy.EnableJourneyEconomy();legacy.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,8);
            Assert.That(legacy.FormationEconomy,Is.False);Assert.That(legacy.NextLevelXP,Is.EqualTo(CampaignJourneyEconomy.RequiredXP(3)));
        }
        [Test]
        public void TalulahChoiceBlocksTradeAndRepricesExistingOffersWithNewStock()
        {
            var shop=Shop();string id=FormationCampaignRules.Find("TAL").CardId;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=id,Star=1},new ShopTrialModel.Piece{Id=id,Star=1}},new[]{id,id,id},3,0,80);
            Assert.That(shop.Purchase(0).Success,Is.True);Assert.That(shop.PendingTalulahChoice,Is.EqualTo(3));
            int gold=shop.Gold;Assert.That(shop.Purchase(1).Success,Is.False);Assert.That(shop.Refresh().Success,Is.False);Assert.That(shop.Upgrade().Success,Is.False);Assert.That(shop.Gold,Is.EqualTo(gold));
            Assert.That(shop.ResolveTalulah(true).Success,Is.True);Assert.That(shop.Definition(id).Cost,Is.EqualTo(4));Assert.That(shop.Capacity(id),Is.EqualTo(10));
            Assert.That(shop.Offers[1],Is.EqualTo(id));Assert.That(shop.PreviewPurchase(1).Value,Is.EqualTo(4));Assert.That(shop.Bench.Single().Star,Is.EqualTo(1));shop.Validate();
        }
        [Test]
        public void TalulahEnhancementIsPermanentAcrossSaleAndReassembly()
        {
            var shop=Shop();string id=shop.TalulahId;shop.AddGold(100);
            Assert.That(shop.Grant(id,2).Success,Is.True);Assert.That(shop.ResolveTalulah(false).Success,Is.True);
            Assert.That(shop.TalulahEnhanced,Is.True);Assert.That(shop.Bench.Single().Star,Is.EqualTo(2));
            shop.Sell(shop.Bench.Single().Instance);shop.Grant(id,2);Assert.That(shop.PendingTalulahChoice,Is.Zero);Assert.That(shop.Definition(id).Cost,Is.EqualTo(3));shop.Validate();
        }
        [Test]
        public void LossStillPaysFiveGoldTwoExperienceAndInterestExactlyOnce()
        {
            var shop=Shop();shop.AddGold(42);var loss=shop.SettleRound("loss",false);
            Assert.That(loss.Base,Is.EqualTo(5));Assert.That(loss.Interest,Is.EqualTo(5));Assert.That(loss.Experience,Is.EqualTo(2));
            Assert.That(shop.Gold,Is.EqualTo(60));Assert.That(shop.XP,Is.EqualTo(2));shop.SettleRound("loss",false);Assert.That(shop.Gold,Is.EqualTo(60));
        }
        [Test]
        public void PreplanRerollCannotChangeStartingCardsShopOrEquipmentOptions()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,17);Assert.That(run.Choices.Count,Is.EqualTo(3));run.Choose(0);
            var initial=run.Choices.Select(x=>x.Id).ToArray();var offers=shop.Offers.ToArray();var inventory=run.Inventory.Select(x=>x.Key).ToArray();
            Assert.That(run.RerollChoice(),Is.True);Assert.That(run.Choices.Any(x=>initial.Contains(x.Id)),Is.False);Assert.That(run.RerollChoice(),Is.False);
            Assert.That(shop.Offers,Is.EqualTo(offers));Assert.That(run.Inventory.Select(x=>x.Key),Is.EqualTo(inventory));
        }
        [Test]
        public void EveryFixedRecipeAndGemBadgeConsumesExactlyTwoInputs()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,29);ResolveChoices(run);
            foreach(var record in FormationCampaignRules.AllItems)
            {var a=run.AddItem(record.Id[0].ToString());var b=run.AddItem(record.Id[1].ToString());Assert.That(run.Combine(a.Instance,b.Instance).Success,Is.True);Assert.That(run.AllEquipment.Any(i=>i.Key==record.Id),Is.True);}
            var gem=run.AddItem("蓝宝石");var armor=run.AddItem("D");Assert.That(run.Combine(gem.Instance,armor.Instance).Success,Is.True);
            Assert.That(run.AllEquipment.Any(i=>i.Key=="徽章:接棒:D"),Is.True);Assert.That(run.AllEquipment.Any(i=>i.Instance==gem.Instance||i.Instance==armor.Instance),Is.False);
        }
        [Test]
        public void TwentyBattleSettlementsAndThreeSuppliesCompleteOneChallengeWithoutReplayRewards()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,64);ResolveChoices(run);int battles=0,supplies=0;
            while(!run.Finished)
            {
                ResolveChoices(run);
                if(run.CurrentNode.Kind=="补给"){supplies++;Assert.That(run.NodeSettled,Is.True);}
                else
                {
                    battles++;run.LockBattleLineup();var receipt=run.SettleBattle(true,1,5,0,false);Assert.That(receipt.Experience,Is.EqualTo(2));
                    int gold=shop.Gold;Assert.That(run.SettleBattle(true,1,5,0,false),Is.Null);Assert.That(shop.Gold,Is.EqualTo(gold));CollectDrops(run);
                }
                shop.Validate();if(!run.Finished)Assert.That(run.AdvanceNode(),Is.True);
            }
            Assert.That(battles,Is.EqualTo(20));Assert.That(supplies,Is.EqualTo(3));Assert.That(run.Won,Is.True);Assert.That(run.AdvanceNode(),Is.False);
        }
        [Test]
        public void InventoryHasNoArtificialCapacityAndHighestStarRewardsDoNotCreateStock()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,91);ResolveChoices(run);for(int i=0;i<1000;i++)run.AddItem("A");Assert.That(run.Inventory.Count,Is.GreaterThanOrEqualTo(1000));
            string id=FormationCampaignRules.Find("B01").CardId;shop.Grant(id,2);int copies=shop.Available(id);Assert.That(shop.GrantAtLeast(id,3).Success,Is.True);
            Assert.That(shop.Available(id),Is.EqualTo(copies-6));Assert.That(shop.Bench.Where(p=>p.Id==id).Single().Star,Is.EqualTo(3));shop.Validate();
        }
        [Test]
        public void FourStarSaleUsesDocumentedTwoGoldDiscountAndReturnsTwentySevenCopies()
        {
            var shop=Shop();string id=FormationCampaignRules.Find("SLUG").CardId;shop.Grant(id,4);var piece=shop.Bench.Single();
            Assert.That(shop.Quote(piece.Instance).Value,Is.EqualTo(25));Assert.That(shop.Available(id),Is.EqualTo(3));
            shop.Sell(piece.Instance);Assert.That(shop.Available(id),Is.EqualTo(30));shop.Validate();
        }
        [Test]
        public void ReachingMaximumStarReturnsOtherVisibleCopiesAndStopsOfferingThatRole()
        {
            var shop=Shop();string id=FormationCampaignRules.Find("SLUG").CardId;
            shop.Preset(new[]{3,3,2,2,1,1}.Select(star=>new ShopTrialModel.Piece{Id=id,Star=star}),new[]{id,id,id},3,0,8);
            Assert.That(shop.Purchase(0).FinalStar,Is.EqualTo(4));Assert.That(shop.Offers.All(x=>x!=id),Is.True);Assert.That(shop.Available(id),Is.EqualTo(3));shop.Validate();
        }
        [Test]
        public void EncounterRewardRequiresProgressStrictlyAboveOneQuarter()
        {
            FormationCampaignRun Prepare()
            {
                var shop=Shop(82);var run=new FormationCampaignRun(shop,82);
                while(run.CurrentNode.Id!="1-7")
                {ResolveChoices(run);if(run.CurrentNode.Kind!="补给")run.SettleBattle(true,1,5,0,false);CollectDrops(run);Assert.That(run.AdvanceNode(),Is.True);}
                ResolveChoices(run);return run;
            }
            var exact=Prepare();var above=Prepare();
            exact.SettleBattle(false,.25f,5,5,false);above.SettleBattle(false,.25001f,5,5,false);
            Assert.That(exact.PendingBalls,Is.Empty);Assert.That(above.PendingBalls,Is.Not.Empty);
            CollectDrops(above);
            string Snapshot(FormationCampaignRun run)=>run.Shop.Gold+"/"+string.Join(",",run.AllEquipment.Select(x=>x.Key))+"/"+string.Join(",",run.Shop.Bench.Select(x=>x.Id+":"+x.Star))+"/"+run.PendingDeliveries;
            Assert.That(Snapshot(exact),Is.Not.EqualTo(Snapshot(above)));Assert.That(exact.SquadHP,Is.EqualTo(above.SquadHP));
        }
        [Test]
        public void ContractTalulahChoicePausesThenResumesTheNodesAutomaticRefresh()
        {
            var shop=Shop(17);var run=new FormationCampaignRun(shop,17);ResolveChoices(run);
            string tal=shop.TalulahId;shop.Preset(new[]{new ShopTrialModel.Piece{Id=tal,Star=1}},Array.Empty<string>(),3,0,80);
            // Fixture starts directly before the first contract payout, without depending on rare augment rolls.
            typeof(FormationCampaignRun).GetProperty("NodeIndex").GetSetMethod(true).Invoke(run,new object[]{2});
            typeof(FormationCampaignRun).GetMethod("ApplyAugment",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,new object[]{"EG3"});
            int target=run.Choices.Select((x,i)=>new{x,i}).Single(x=>x.x.Id==tal).i;run.Choose(target);
            Assert.That(shop.Bench.Count(x=>x.Id==tal),Is.EqualTo(2));run.SettleBattle(true,1,5,0,false);shop.Refresh(true);shop.Purchase(0);
            Assert.That(shop.Offers.Count(x=>x!=null),Is.EqualTo(4));Assert.That(run.AdvanceNode(),Is.True);
            Assert.That(shop.PendingTalulahChoice,Is.EqualTo(3));Assert.That(shop.Offers.Count(x=>x!=null),Is.EqualTo(4));
            Assert.That(run.Choose(0).Success,Is.True);Assert.That(shop.Offers.Count(x=>x!=null),Is.EqualTo(5));shop.Validate();
        }
        [Test]
        public void FourMudrockMembersCannotFarmCultivationAboveNinetyNineExperience()
        {
            var shop=Shop(33);var run=new FormationCampaignRun(shop,33);ResolveChoices(run);
            shop.Preset(new[]{"M27","M42","M28","B01"}.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=id=="B01"?1:3}),Array.Empty<string>(),4,0,80);
            shop.SetLineup(shop.Bench.Select(p=>p.Id),shop.Bench.Select(p=>p.Instance));
            var badge=run.AddItem("徽章:泥岩");var fourth=shop.Bench.Single(p=>FormationCampaignRules.Find(p.Id).Id=="B01");Assert.That(run.Equip(badge.Instance,fourth.Instance).Success,Is.True);
            while(!run.Finished)
            {ResolveChoices(run);if(run.CurrentNode.Kind!="补给")run.SettleBattle(true,1,5,0,false);CollectDrops(run);if(!run.Finished)Assert.That(run.AdvanceNode(),Is.True);}
            Assert.That(run.MudrockXP,Is.EqualTo(99));
        }
        static void FixtureNode(FormationCampaignRun run,int index)=>typeof(FormationCampaignRun).GetProperty("NodeIndex").GetSetMethod(true).Invoke(run,new object[]{index});
        static void FixtureAugment(FormationCampaignRun run,string id)=>typeof(FormationCampaignRun).GetMethod("ApplyAugment",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,new object[]{id});
        static FormationCampaignRun SupplyFixture(bool doubleSupply=false,bool inspect=false)
        {
            var shop=Shop(88);var run=new FormationCampaignRun(shop,88);ResolveChoices(run);
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,80);FixtureNode(run,3);
            if(doubleSupply)FixtureAugment(run,"EP5");if(inspect)FixtureAugment(run,"ES6");
            run.SettleBattle(true,1,5,0,false);Assert.That(run.AdvanceNode(),Is.True);Assert.That(run.CurrentNode.Id,Is.EqualTo("1-5"));return run;
        }
        [Test]
        public void ReservedLastCopyCannotBeStolenAndItsClaimAndReleaseAreIdempotent()
        {
            var shop=Shop();string id=FormationCampaignRules.Find("M28").CardId;
            shop.Preset(Enumerable.Range(0,3).Select(_=>new ShopTrialModel.Piece{Id=id,Star=2}),Array.Empty<string>(),3,0,80);
            Assert.That(shop.Available(id),Is.EqualTo(1));var ticket=shop.ReserveRewardCandidate(id);
            Assert.That(ticket,Is.Not.Null);Assert.That(shop.Available(id),Is.Zero);Assert.That(shop.ReservedRewards(id),Is.EqualTo(1));
            Assert.That(shop.Grant(id).Success,Is.False);Assert.That(shop.GrantReserved(ticket).Success,Is.True);
            Assert.That(shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(10));
            Assert.That(shop.GrantReserved(ticket).Success,Is.False);Assert.That(shop.ReleaseReservation(ticket),Is.False);shop.Validate();
            string other=FormationCampaignRules.Find("M25").CardId;int before=shop.Available(other);var unselected=shop.ReserveRewardCandidate(other);
            Assert.That(shop.ReleaseReservation(unselected),Is.True);Assert.That(shop.ReleaseReservation(unselected),Is.False);Assert.That(shop.Available(other),Is.EqualTo(before));
            var otherShop=Shop();var foreign=otherShop.ReserveRewardCandidate(other);Assert.That(shop.GrantReserved(foreign).Success,Is.False);Assert.That(shop.ReleaseReservation(foreign),Is.False);shop.Validate();otherShop.Validate();
        }
        [Test]
        public void FullBenchKeepsSelectedReservedCopyUntilSpaceIsReleased()
        {
            var shop=Shop();var ids=FormationCampaignRules.AllUnits.Take(10).Select(x=>x.CardId).ToArray();string target=FormationCampaignRules.Find("M28").CardId;
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),3,0,80);
            var ticket=shop.ReserveRewardCandidate(target);int available=shop.Available(target);
            Assert.That(shop.GrantReserved(ticket).Success,Is.False);Assert.That(shop.IsReservationActive(ticket),Is.True);Assert.That(shop.Available(target),Is.EqualTo(available));shop.Validate();
            shop.Sell(shop.Bench.First().Instance);Assert.That(shop.GrantReserved(ticket).Success,Is.True);Assert.That(shop.Available(target),Is.EqualTo(available));Assert.That(shop.IsReservationActive(ticket),Is.False);shop.Validate();
        }
        [Test]
        public void TalulahBothCostUpgradesIncludeOutstandingRewardReservations()
        {
            var shop=Shop();string id=shop.TalulahId;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=id,Star=1},new ShopTrialModel.Piece{Id=id,Star=1}},new[]{id,id},3,0,80);
            var first=shop.ReserveRewardCandidate(id);shop.Purchase(0);Assert.That(shop.PendingTalulahChoice,Is.EqualTo(3));
            Assert.That(shop.ResolveTalulah(true).Success,Is.True);Assert.That(shop.Capacity(id),Is.EqualTo(10));Assert.That(shop.Available(id),Is.EqualTo(7));
            var second=shop.ReserveRewardCandidate(id);Assert.That(shop.GrantReserved(first).Success,Is.True);shop.Purchase(1);Assert.That(shop.PendingTalulahChoice,Is.EqualTo(4));
            Assert.That(shop.ResolveTalulah(true).Success,Is.True);Assert.That(shop.Capacity(id),Is.EqualTo(9));Assert.That(shop.ReservedRewards(id),Is.EqualTo(1));Assert.That(shop.Available(id),Is.EqualTo(7));
            Assert.That(shop.GrantReserved(second).Success,Is.True);Assert.That(shop.Available(id),Is.EqualTo(7));Assert.That(shop.Bench.Count(p=>p.Id==id),Is.EqualTo(2));shop.Validate();
        }
        [Test]
        public void DisplayedSupplyReservationSurvivesAnotherPendingDeliveryForItsLastCopy()
        {
            var run=SupplyFixture();var shop=run.Shop;Assert.That(shop.RewardReservations.Count,Is.EqualTo(5));
            int selected=Array.FindIndex(run.Choices.ToArray(),option=>option.Id!="TAL");string id=FormationCampaignRules.Find(run.Choices[selected].Id).CardId;
            var extra=new System.Collections.Generic.List<ShopTrialModel.RewardReservation>();
            while(shop.Available(id)>0)extra.Add(shop.ReserveRewardCandidate(id));
            run.Give(id);run.OnShopChanged();Assert.That(run.PendingDeliveries,Is.EqualTo(1));Assert.That(shop.Available(id),Is.Zero);
            Assert.That(run.Choose(selected).Success,Is.True);Assert.That(run.NodeSettled,Is.True);Assert.That(shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(1));
            Assert.That(run.PendingDeliveries,Is.EqualTo(1));Assert.That(shop.RewardReservations.Count,Is.EqualTo(extra.Count));
            shop.ReleaseReservation(extra[0]);run.OnShopChanged();Assert.That(run.PendingDeliveries,Is.Zero);Assert.That(shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(2));shop.Validate();
        }
        [Test]
        public void DoubleSupplyAndInspectionShareFiveTicketsAndReturnOnlyUnselectedCopies()
        {
            var run=SupplyFixture(true,true);var shop=run.Shop;var chosen=new System.Collections.Generic.HashSet<string>();
            Assert.That(shop.RewardReservations.Count,Is.EqualTo(5));
            for(int i=0;i<2;i++)
            {
                int selected=Array.FindIndex(run.Choices.ToArray(),option=>option.Id!="TAL");chosen.Add(run.Choices[selected].Id);run.Choose(selected);
                Assert.That(shop.RewardReservations.Count,Is.EqualTo(4-i));Assert.That(run.NodeSettled,Is.False);shop.Validate();
            }
            Assert.That(run.ChoiceTitle,Does.StartWith("入库抽检"));Assert.That(run.Choices.Count,Is.EqualTo(3));Assert.That(run.Choices.Any(x=>chosen.Contains(x.Id)),Is.False);
            int last=Array.FindIndex(run.Choices.ToArray(),option=>option.Id!="TAL");chosen.Add(run.Choices[last].Id);run.Choose(last);
            Assert.That(shop.RewardReservations.Count,Is.Zero);Assert.That(run.NodeSettled,Is.True);Assert.That(shop.Bench.Select(p=>FormationCampaignRules.Find(p.Id).Id),Is.EquivalentTo(chosen));shop.Validate();
        }
        [Test]
        public void SupplyWaitsForContractTalulahChoiceBeforeReservingCandidates()
        {
            var shop=Shop(17);var run=new FormationCampaignRun(shop,17);ResolveChoices(run);string tal=shop.TalulahId;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=tal,Star=1}},Array.Empty<string>(),3,0,80);FixtureNode(run,3);FixtureAugment(run,"EG3");
            int selected=run.Choices.Select((option,index)=>new{option,index}).Single(x=>x.option.Id==tal).index;run.Choose(selected);
            run.SettleBattle(true,1,5,0,false);Assert.That(run.AdvanceNode(),Is.True);Assert.That(shop.PendingTalulahChoice,Is.EqualTo(3));Assert.That(shop.RewardReservations.Count,Is.Zero);
            Assert.That(run.Choose(0).Success,Is.True);Assert.That(run.ChoiceTitle,Does.StartWith("补给"));Assert.That(shop.RewardReservations.Count,Is.EqualTo(5));Assert.That(run.NodeSettled,Is.False);shop.Validate();
        }
        [Test]
        public void SelectedSupplyWithFullReserveWaitsAcrossNodesWithoutReleasingItsTicket()
        {
            var run=SupplyFixture();var shop=run.Shop;
            foreach(var role in FormationCampaignRules.AllUnits.Where(x=>x.Cost==1))Assert.That(shop.Grant(role.CardId).Success,Is.True);
            foreach(string id in new[]{"M14","M28"})Assert.That(shop.Grant(FormationCampaignRules.Find(id).CardId).Success,Is.True);
            Assert.That(shop.Bench.Count,Is.EqualTo(10));int selected=Array.FindIndex(run.Choices.ToArray(),x=>x.Id!="TAL");string target=FormationCampaignRules.Find(run.Choices[selected].Id).CardId;
            run.Choose(selected);Assert.That(run.PendingDeliveries,Is.EqualTo(1));Assert.That(shop.ReservedRewards(target),Is.EqualTo(1));Assert.That(shop.RewardReservations.Count,Is.EqualTo(1));
            Assert.That(run.AdvanceNode(),Is.True);Assert.That(run.PendingDeliveries,Is.EqualTo(1));Assert.That(shop.ReservedRewards(target),Is.EqualTo(1));shop.Validate();
            shop.Sell(shop.Bench.First().Instance);run.OnShopChanged();Assert.That(run.PendingDeliveries,Is.Zero);Assert.That(shop.RewardReservations.Count,Is.Zero);Assert.That(shop.Bench.Any(p=>p.Id==target),Is.True);shop.Validate();
        }
    }
}
