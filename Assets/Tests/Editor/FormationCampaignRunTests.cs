using System;
using System.Linq;
using System.Reflection;
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
                if (run.Shop.Bench.Count(piece => !run.Shop.IsLineupInstance(piece.Instance)) >= ShopSeatLayout.ReserveCapacity) break;
                Assert.That(run.BeginBall(drop.Id), Is.Not.Null);
                Assert.That(run.CompleteBall(drop.Id), Is.Not.Null);
                ResolveChoices(run);
            }
        }

        internal static bool CompleteOpeningDelivery(FormationShopSession session)
        {
            var completion = typeof(FormationShopSession).GetMethod("CompleteOpeningDelivery",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)completion.Invoke(session, null);
        }

        [Test]
        public void OpeningDeliveryPreservesReservedStockAndOpensOnePreplan()
        {
            string Open(int seed)
            {
                var session = new FormationShopSession(seed, fullCampaign: true);
                var run = session.Run;
                var shop = session.Shop;
                Assert.That(run.OpeningRewardsPending, Is.True);
                Assert.That(run.HasBlockingChoice || session.CanShop, Is.False);
                Assert.That(shop.Bench, Is.Empty);
                Assert.That(run.Inventory, Is.Empty);
                Assert.That(run.PendingBalls, Is.Empty);
                var ids = shop.RewardReservations.OrderBy(ticket => ticket.Token).Select(ticket => ticket.Id).ToArray();
                Assert.That(ids.Distinct().Count(), Is.EqualTo(3));
                foreach (string id in ids)
                {
                    Assert.That(shop.Definition(id).Cost, Is.InRange(1, 3));
                    Assert.That(shop.Available(id) + shop.Offers.Count(offer => offer == id), Is.EqualTo(shop.Capacity(id) - 1));
                }
                Assert.That(run.Choose(0).Success, Is.False);
                Assert.That(run.SettleBattle(true, 1, 3, 0, false), Is.Null);
                Assert.That(run.AdvanceNode(), Is.False);
                Assert.That(CompleteOpeningDelivery(session), Is.True);
                Assert.That(CompleteOpeningDelivery(session), Is.False);
                Assert.That(run.OpeningRewardsPending, Is.False);
                Assert.That(run.Choices.Count, Is.EqualTo(3));
                Assert.That(run.ChoiceTitle, Does.Contain("战略预案"));
                Assert.That(shop.Bench.Select(piece => piece.Id), Is.EqualTo(ids));
                Assert.That(shop.Bench.All(piece => piece.Star == 1 && !session.Seats.InLineup(piece.Instance)), Is.True);
                Assert.That(shop.Gold, Is.EqualTo(8));
                Assert.That(shop.Level, Is.EqualTo(3));
                Assert.That(shop.RewardReservations, Is.Empty);
                Assert.That(run.PendingBalls, Is.Empty);
                Assert.That(run.BallRewards, Is.Empty);
                string equipment = run.Inventory.Single().Key;
                Assert.That(FormationCampaignRules.AllComponents.Select(item => item.Id), Does.Contain(equipment));
                shop.Validate();
                return equipment + " / " + string.Join(",", ids);
            }

            string first = Open(17);
            Assert.That(Open(17), Is.EqualTo(first));
            Assert.That(Enumerable.Range(17, 10).Select(Open).Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void OpeningCostDrawIsWeightedAcrossSeedsWithoutRepeatingIdentity()
        {
            const int seeds = 1000, draws = seeds * 3;
            var counts = new int[3];
            for (int seed = 0; seed < seeds; seed++)
            {
                var shop = new FormationShopSession(seed, fullCampaign: true).Shop;
                var ids = shop.RewardReservations.Select(ticket => ticket.Id).ToArray();
                Assert.That(ids.Distinct().Count(), Is.EqualTo(3));
                foreach (string id in ids) counts[shop.Definition(id).Cost - 1]++;
            }
            Assert.That(counts[0] / (double)draws, Is.EqualTo(.75).Within(.035));
            Assert.That(counts[1] / (double)draws, Is.EqualTo(.20).Within(.03));
            Assert.That(counts[2] / (double)draws, Is.EqualTo(.05).Within(.016));
        }

        [Test]
        public void GroundDropsRequireAnOpeningAndPayExactlyOnceOnArrival()
        {
            var shop = Shop(39); var run = new FormationCampaignRun(shop,39);
            ResolveChoices(run);
            Assert.That(run.SettleBattle(true,1,3,0,false),Is.Not.Null);
            Assert.That(run.PendingBalls.Count(ball=>ball.Component==null),Is.EqualTo(2));
            Assert.That(run.OpenedBalls,Is.Empty);
            var drops = run.PendingBalls.Select(ball=>ball.Id).ToArray();
            Assert.That(run.AdvanceNode(),Is.True);
            Assert.That(run.PendingBalls.Select(ball=>ball.Id),Is.EqualTo(drops));
            var drop = run.PendingBalls.Last();
            var other = run.PendingBalls.First();
            int gold = shop.Gold, gear = run.Inventory.Count, count = shop.Bench.Count;
            Assert.That(run.CompleteBall(drop.Id),Is.Null);
            var payout = run.BeginBall(drop.Id);
            Assert.That(payout,Is.Not.Null);
            Assert.That(run.BeginBall(drop.Id),Is.Null);
            var otherPayout = run.BeginBall(other.Id);
            Assert.That(otherPayout,Is.Not.Null);
            Assert.That(run.BeginBall(other.Id),Is.Null);
            Assert.That(shop.Gold,Is.EqualTo(gold));
            Assert.That(run.Inventory.Count,Is.EqualTo(gear));
            Assert.That(shop.Bench.Count,Is.EqualTo(count));
            Assert.That(run.CompleteBall(other.Id)?.Node,Is.EqualTo("1-1"));
            Assert.That(run.CompleteBall(other.Id),Is.Null);
            Assert.That(run.PendingBalls.Any(ball=>ball.Id==drop.Id),Is.True);
            Assert.That(shop.Gold,Is.EqualTo(gold+otherPayout.Gold));
            Assert.That(run.Inventory.Count,Is.EqualTo(gear+otherPayout.Items.Count));
            var receipt = run.CompleteBall(drop.Id);
            Assert.That(receipt.Color,Is.EqualTo(drop.Color));
            Assert.That(receipt.Node,Is.EqualTo("1-1"));
            Assert.That(shop.Gold,Is.EqualTo(gold+otherPayout.Gold+payout.Gold));
            Assert.That(run.Inventory.Count,Is.EqualTo(gear+otherPayout.Items.Count+payout.Items.Count));
            Assert.That(run.CompleteBall(drop.Id),Is.Null);
            Assert.That(run.BeginBall(drop.Id),Is.Null);
            CollectDrops(run);
            Assert.That(run.BallRewards.Count,Is.EqualTo(drops.Length));
            Assert.That(run.BallRewards.Count(reward=>reward.Color.Length>0),Is.EqualTo(2));
            while(run.PendingRewardChoices>0) Assert.That(run.ChooseReward(0).Success,Is.True);
            Assert.That(run.Inventory.Sum(item=>item.Key.Length==1?1:item.Key.Length==2?2:0),Is.GreaterThanOrEqualTo(2));
        }
        [Test]
        public void RewardBallRemainsOnBoardUntilAReserveSeatIsFree()
        {
            var shop = Shop();
            var run = new FormationCampaignRun(shop, 17);
            ResolveChoices(run);
            shop.Preset(FormationCampaignRules.AllUnits.Take(ShopSeatLayout.ReserveCapacity)
                .Select(unit => new ShopTrialModel.Piece { Id = unit.CardId, Star = 1 }),
                Array.Empty<string>(), 3, 0, 8);
            run.OpenBall("白");
            var drop = run.PendingBalls.Single();
            Assert.That(run.BeginBall(drop.Id), Is.Null);
            Assert.That(run.PendingBalls.Single(), Is.SameAs(drop));
            Assert.That(run.CompleteBall(drop.Id), Is.Null);
            Assert.That(shop.Sell(shop.Bench[0].Instance).Success, Is.True);
            Assert.That(run.BeginBall(drop.Id), Is.Not.Null);
            Assert.That(run.CompleteBall(drop.Id), Is.Not.Null);
        }

        [TestCase(true, 5)]
        [TestCase(false, 3)]
        public void OpeningRewardBattleGrantsVictoryHopeOnlyOnAWin(bool won, int expected)
        {
            var shop = Shop();
            var run = new FormationCampaignRun(shop, 17);
            ResolveChoices(run);
            string[] ids = FormationCampaignRules.AllUnits.Where(unit => unit.Factions.Contains("整合"))
                .Take(3).Select(unit => unit.CardId).ToArray();
            Assert.That(ids, Has.Length.EqualTo(3));
            shop.Preset(ids.Select(id => new ShopTrialModel.Piece { Id = id, Star = 1 }),
                Array.Empty<string>(), 3, 0, 8);
            Assert.That(shop.SetLineup(ids, shop.Bench.Select(piece => piece.Instance)), Is.True);
            run.LockBattleLineup();
            Assert.That(run.SettleBattle(won, 1, 3, 0, false), Is.Not.Null);
            Assert.That(run.Hope, Is.EqualTo(expected));
        }

        [Test]
        public void RewardPacksGrantSeveralDistinctCardsAndTwoStarUsesThreeCopies()
        {
            foreach (string color in new[]{"白","蓝"})
            {
                bool multi=false, twoStar=false;
                for(int seed=1;seed<=120&&(!multi||!twoStar);seed++)
                {
                    var shop=Shop(seed);var run=new FormationCampaignRun(shop,seed);ResolveChoices(run);
                    shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,8);
                    run.OpenBall(color);
                    var drop=run.PendingBalls.Single();
                    var payout=run.BeginBall(drop.Id);
                    if(payout.Recruits.Count==0)continue;
                    Assert.That(payout.Recruits.Select(recruit=>recruit.CardId).Distinct().Count(),Is.EqualTo(payout.Recruits.Count));
                    var before=payout.Recruits.ToDictionary(recruit=>recruit.CardId,recruit=>shop.Available(recruit.CardId));
                    Assert.That(run.CompleteBall(drop.Id),Is.Not.Null);
                    Assert.That(shop.Bench.Count,Is.EqualTo(payout.Recruits.Count));
                    foreach(var recruit in payout.Recruits)
                    {
                        Assert.That(shop.Bench.Single(piece=>piece.Id==recruit.CardId).Star,Is.EqualTo(recruit.Star));
                        Assert.That(shop.Available(recruit.CardId),Is.EqualTo(before[recruit.CardId]-ShopTrialModel.Copies(recruit.Star)));
                    }
                    shop.Validate();
                    multi|=payout.Recruits.Count>=3;twoStar|=payout.Recruits.Any(recruit=>recruit.Star==2);
                }
                Assert.That(multi&&twoStar,Is.True,color);
            }
        }
        [Test]
        public void DeferredBallChoiceDoesNotBlockProgressAndCanBeClaimedOnLaterNode()
        {
            var shop=Shop(7);var run=new FormationCampaignRun(shop,7);ResolveChoices(run);
            for(int attempt=0;attempt<80&&run.PendingRewardChoices==0;attempt++)
            {
                run.OpenBall("彩");
                var drop=run.PendingBalls.Single();
                run.BeginBall(drop.Id);run.CompleteBall(drop.Id);
            }
            Assert.That(run.PendingRewardChoices,Is.EqualTo(1));
            Assert.That(run.HasBlockingChoice,Is.False);
            var options=run.RewardChoices.Select(option=>option.Id).ToArray();
            run.SettleBattle(true,1,3,0,false);
            Assert.That(run.AdvanceNode(),Is.True);
            Assert.That(run.RewardChoices.Select(option=>option.Id),Is.EqualTo(options));
            Assert.That(run.ChooseReward(0).Success,Is.True);
            Assert.That(run.ChooseReward(0).Success,Is.False);
        }
        [Test]
        public void TwoStarRewardWaitsForEnoughStockAndDeliversAtTheOriginalStar()
        {
            var shop=Shop(29);var run=new FormationCampaignRun(shop,29);ResolveChoices(run);
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,8);
            FormationCampaignRun.RewardRecruit twoStar=null;
            for(int index=0;index<100&&twoStar==null;index++)
            {
                run.OpenBall("白");
                var drop=run.PendingBalls.Single();
                twoStar=run.BeginBall(drop.Id).Recruits.FirstOrDefault(recruit=>recruit.Star==2);
                if(twoStar==null)
                {
                    run.CompleteBall(drop.Id);
                    foreach(var piece in shop.Bench.ToArray()) Assert.That(shop.Sell(piece.Instance).Success,Is.True);
                    continue;
                }
                var reservations=Enumerable.Range(0,shop.Available(twoStar.CardId)-2)
                    .Select(_=>shop.ReserveRewardCandidate(twoStar.CardId)).ToArray();
                Assert.That(reservations,Is.All.Not.Null);
                var reward=run.CompleteBall(drop.Id);
                Assert.That(reward.Recruit,Does.Contain("待领"));
                Assert.That(run.PendingDeliveries,Is.EqualTo(1));
                Assert.That(shop.Bench,Is.Empty);
                Assert.That(shop.Available(twoStar.CardId),Is.EqualTo(2));
                Assert.That(shop.ReleaseReservation(reservations[0]),Is.True);
                run.OnShopChanged();
                Assert.That(run.PendingDeliveries,Is.Zero);
                Assert.That(shop.Bench.Single().Star,Is.EqualTo(2));
                Assert.That(shop.Available(twoStar.CardId),Is.Zero);
                shop.Validate();
            }
            Assert.That(twoStar,Is.Not.Null);
        }
        [Test]
        public void GroundEquipmentFloorCountsUnopenedDropsAcrossBothSettlements()
        {
            bool foundSeparateDrop=false;
            for(int seed=1;seed<=40;seed++)
            {
                var shop=Shop(seed);var run=new FormationCampaignRun(shop,seed);ResolveChoices(run);
                int Value(string key)=>key.Length==1&&"ABCDEF".Contains(key)?1:FormationCampaignRules.AllItems.Any(item=>item.Id==key)?2:0;
                int initialCount=run.AllEquipment.Count, initial=run.AllEquipment.Sum(item=>Value(item.Key));
                run.SettleBattle(true,1,3,0,false);
                foundSeparateDrop|=run.PendingBalls.Any(drop=>drop.Component!=null&&Value(drop.Component)>0);
                var original=run.PendingBalls.Select(drop=>drop.Id).ToArray();
                Assert.That(run.AdvanceNode(),Is.True);
                run.SettleBattle(true,1,3,0,false);
                Assert.That(run.AllEquipment.Count,Is.EqualTo(initialCount));
                Assert.That(run.PendingBalls.Take(original.Length).Select(drop=>drop.Id),Is.EqualTo(original));
                var drops=run.PendingBalls.ToArray();
                CollectDrops(run);
                while(run.PendingRewardChoices>0)
                {
                    Assert.That(run.ChooseReward(0).Success,Is.True);
                }
                int BallValue(FormationCampaignRun.BallReward reward)=>reward.Items.Sum(Value)+
                    (reward.Choice?.Contains("进阶装备")==true?2:reward.Choice?.Contains("基础装备")==true?1:0);
                int originalLoot=run.BallRewards.Where(reward=>reward.Color.Length>0).Sum(BallValue);
                int firstLoot=run.BallRewards.Where(reward=>reward.Color.Length>0&&reward.Node=="1-1").Sum(BallValue);
                int preplanDrop=run.PreplanId=="P08"?1:0;
                int firstFloor=preplanDrop+Math.Max(0,2-initial-firstLoot-preplanDrop);
                string trace = "seed "+seed+" plan "+run.PreplanId+" initial "+initial+" firstLoot "+firstLoot+
                    " first: "+string.Join(" | ",run.BallRewards.Where(reward=>reward.Node=="1-1").Select(reward=>reward.Color+"/"+string.Join(",",reward.Items)+"/"+reward.Choice));
                Assert.That(drops.Count(drop=>drop.Component!=null&&Value(drop.Component)>0&&drop.Node=="1-1"),Is.EqualTo(firstFloor),trace);
                Assert.That(drops.Count(drop=>drop.Component!=null&&Value(drop.Component)>0&&drop.Node=="1-2"),
                    Is.EqualTo(Math.Max(0,3-initial-originalLoot-firstFloor)));
            }
            Assert.That(foundSeparateDrop,Is.True);
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
        public void FourMudrockMembersCannotFarmCultivationAboveSeventyNineExperience()
        {
            var shop=Shop(33);var run=new FormationCampaignRun(shop,33);ResolveChoices(run);
            shop.Preset(new[]{"M27","M42","M28","B01"}.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=id=="B01"?1:3}),Array.Empty<string>(),4,0,80);
            shop.SetLineup(shop.Bench.Select(p=>p.Id),shop.Bench.Select(p=>p.Instance));
            var badge=run.AddItem("徽章:泥岩");var fourth=shop.Bench.Single(p=>FormationCampaignRules.Find(p.Id).Id=="B01");Assert.That(run.Equip(badge.Instance,fourth.Instance).Success,Is.True);
            while(!run.Finished)
            {ResolveChoices(run);if(run.CurrentNode.Kind!="补给")run.SettleBattle(true,1,5,0,false);CollectDrops(run);if(!run.Finished)Assert.That(run.AdvanceNode(),Is.True);}
            Assert.That(run.MudrockXP,Is.EqualTo(79));
        }
        static void FixtureNode(FormationCampaignRun run,int index)=>typeof(FormationCampaignRun).GetProperty("NodeIndex").GetSetMethod(true).Invoke(run,new object[]{index});
        static void FixtureAugment(FormationCampaignRun run,string id)=>typeof(FormationCampaignRun).GetMethod("ApplyAugment",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,new object[]{id});
        static FormationCampaignRun SupplyFixture(bool doubleSupply=false,bool inspect=false,bool prepaid=false)
        {
            var shop=Shop(88);var run=new FormationCampaignRun(shop,88);ResolveChoices(run);
            if (prepaid)
            {
                typeof(FormationCampaignRun).GetMethod("ApplyPreplan", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(run, new object[] { "P07" });
                ResolveChoices(run);
            }
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),3,0,80);FixtureNode(run,3);
            if(doubleSupply)FixtureAugment(run,"EP5");if(inspect)FixtureAugment(run,"ES6");
            run.SettleBattle(true,1,5,0,false);Assert.That(run.AdvanceNode(),Is.True);Assert.That(run.CurrentNode.Id,Is.EqualTo("1-5"));return run;
        }

        static T SupplyDisplayMember<T>(object source, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var property = source.GetType().GetProperty(name, flags);
            if (property != null) return (T)property.GetValue(source);
            var field = source.GetType().GetField(name, flags);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(source);
        }

        [Test]
        public void DisplayedSupplyPackMatchesItsReservedCardAndGrantedComponent()
        {
            var run = SupplyFixture();
            var shop = run.Shop;
            var options = run.Choices.ToArray();
            Assert.That(options.Length, Is.EqualTo(5));
            Assert.That(SupplyDisplayMember<int>(run, "SupplyPicksRemaining"), Is.EqualTo(1));
            Assert.That(SupplyDisplayMember<bool>(run, "SupplyEquipmentWithheld"), Is.False);
            var units = options.Select(option => SupplyDisplayMember<FormationCampaignRules.UnitDefinition>(option, "SupplyUnit")).ToArray();
            var components = options.Select(option => SupplyDisplayMember<string>(option, "SupplyComponent")).ToArray();
            Assert.That(units, Is.All.Not.Null);
            Assert.That(units.Select(unit => unit.Id), Is.EqualTo(options.Select(option => option.Id)));
            Assert.That(units.Select(unit => unit.CardId), Is.EquivalentTo(shop.RewardReservations.Select(ticket => ticket.Id)));
            Assert.That(units.Select(unit => shop.Definition(unit.CardId).Cost), Is.All.InRange(2, 3));
            Assert.That(components.Distinct(), Is.SubsetOf(FormationCampaignRules.AllComponents.Select(item => item.Id)));

            const int selected = 2;
            var ownedItems = run.Inventory.Select(item => item.Instance).ToArray();
            var displayedUnit = units[selected];
            int displayedCost = shop.Definition(displayedUnit.CardId).Cost;
            Assert.That(run.Choose(selected).Success, Is.True);
            var recruit = shop.Bench.Single();
            Assert.That(recruit.Id, Is.EqualTo(displayedUnit.CardId));
            Assert.That(recruit.Star, Is.EqualTo(1));
            Assert.That(shop.Definition(recruit.Id).Cost, Is.EqualTo(displayedCost));
            Assert.That(run.Inventory.Where(item => !ownedItems.Contains(item.Instance)).Select(item => item.Key),
                Is.EqualTo(new[] { components[selected] }));
            Assert.That(SupplyDisplayMember<int>(run, "SupplyPicksRemaining"), Is.Zero);
            Assert.That(run.NodeSettled, Is.True);
            shop.Validate();
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
        public void FullBenchClaimsReservedRewardIntoOverflowWithoutSpendingStockTwice()
        {
            var shop=Shop();var ids=FormationCampaignRules.AllUnits.Take(10).Select(x=>x.CardId).ToArray();string target=FormationCampaignRules.Find("M28").CardId;
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),3,0,80);
            var ticket=shop.ReserveRewardCandidate(target);int available=shop.Available(target);
            Assert.That(shop.GrantReserved(ticket).Success,Is.True);
            Assert.That(shop.Bench.Count,Is.EqualTo(ShopSeatLayout.ReserveCapacity + 1));
            Assert.That(shop.Available(target),Is.EqualTo(available));
            Assert.That(shop.IsReservationActive(ticket),Is.False);
            Assert.That(shop.GrantReserved(ticket).Success,Is.False);
            shop.Validate();
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
        [TestCase(false)]
        [TestCase(true)]
        public void DoubleSupplyAndInspectionShareFiveTicketsAndReturnOnlyUnselectedCopies(bool prepaid)
        {
            var run = SupplyFixture(doubleSupply: true, inspect: true, prepaid: prepaid);
            var shop = run.Shop;
            var chosen = new System.Collections.Generic.HashSet<string>();
            Assert.That(shop.RewardReservations.Count, Is.EqualTo(5));
            for (int i = 0; i < 2; i++)
            {
                bool withhold = prepaid && i == 0;
                Assert.That(SupplyDisplayMember<int>(run, "SupplyPicksRemaining"), Is.EqualTo(2 - i));
                Assert.That(SupplyDisplayMember<bool>(run, "SupplyEquipmentWithheld"), Is.EqualTo(withhold));
                int selected = Array.FindIndex(run.Choices.ToArray(), option => option.Id != "TAL");
                var option = run.Choices[selected];
                var component = SupplyDisplayMember<string>(option, "SupplyComponent");
                var ownedItems = run.Inventory.Select(item => item.Instance).ToArray();
                chosen.Add(option.Id);
                Assert.That(run.Choose(selected).Success, Is.True);
                Assert.That(run.Inventory.Where(item => !ownedItems.Contains(item.Instance)).Select(item => item.Key),
                    Is.EqualTo(withhold ? Array.Empty<string>() : new[] { component }));
                Assert.That(shop.RewardReservations.Count, Is.EqualTo(4 - i));
                Assert.That(run.NodeSettled, Is.False);
                shop.Validate();
            }
            Assert.That(run.ChoiceTitle, Does.StartWith("入库抽检"));
            Assert.That(run.Choices.Count, Is.EqualTo(3));
            Assert.That(run.Choices.Any(option => chosen.Contains(option.Id)), Is.False);
            Assert.That(SupplyDisplayMember<int>(run, "SupplyPicksRemaining"), Is.EqualTo(1));
            Assert.That(SupplyDisplayMember<bool>(run, "SupplyEquipmentWithheld"), Is.False);
            Assert.That(run.Choices.Select(option => SupplyDisplayMember<string>(option, "SupplyComponent")), Is.All.Null);
            Assert.That(run.Choices.Select(option => SupplyDisplayMember<FormationCampaignRules.UnitDefinition>(option, "SupplyUnit")?.Id),
                Is.EqualTo(run.Choices.Select(option => option.Id)));
            int gearCount = run.Inventory.Count;
            int last = Array.FindIndex(run.Choices.ToArray(), option => option.Id != "TAL");
            chosen.Add(run.Choices[last].Id);
            Assert.That(run.Choose(last).Success, Is.True);
            Assert.That(run.Inventory.Count, Is.EqualTo(gearCount));
            Assert.That(shop.RewardReservations.Count, Is.Zero);
            Assert.That(run.NodeSettled, Is.True);
            Assert.That(shop.Bench.Select(p => FormationCampaignRules.Find(p.Id).Id), Is.EquivalentTo(chosen));
            shop.Validate();
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
        public void SelectedSupplyWithFullReserveCarriesOwnedOverflowAcrossNodes()
        {
            var run=SupplyFixture();var shop=run.Shop;
            foreach(var role in FormationCampaignRules.AllUnits.Where(x=>x.Cost==1))Assert.That(shop.Grant(role.CardId).Success,Is.True);
            foreach(string id in new[]{"M14","M28"})Assert.That(shop.Grant(FormationCampaignRules.Find(id).CardId).Success,Is.True);
            Assert.That(shop.Bench.Count,Is.EqualTo(10));int selected=Array.FindIndex(run.Choices.ToArray(),x=>x.Id!="TAL");string target=FormationCampaignRules.Find(run.Choices[selected].Id).CardId;
            Assert.That(run.Choose(selected).Success,Is.True);
            Assert.That(run.PendingDeliveries,Is.Zero);
            Assert.That(shop.RewardReservations,Is.Empty);
            var recruit = shop.Bench.Last();
            Assert.That(recruit.Id,Is.EqualTo(target));
            Assert.That(shop.Bench.Count,Is.EqualTo(ShopSeatLayout.ReserveCapacity + 1));
            Assert.That(run.AdvanceNode(),Is.True);
            Assert.That(shop.Bench.Last(),Is.SameAs(recruit));
            Assert.That(shop.Bench.Count,Is.EqualTo(ShopSeatLayout.ReserveCapacity + 1));
            shop.Validate();
        }
    }
}
