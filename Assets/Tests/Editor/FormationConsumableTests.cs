using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationConsumableTests
    {
        static string Card(string id)=>FormationCampaignRules.Find(id).CardId;
        static FormationCampaignRun Fixture(params (string Id,int Star)[] roster)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u=>new ShopTrialModel.Role{Id=u.CardId,Name=u.Name,Cost=u.Cost}),871);
            shop.EnableFormationEconomy();shop.Preset(roster.Select(p=>new ShopTrialModel.Piece{Id=Card(p.Id),Star=p.Star}),Array.Empty<string>(),8,0,100);
            var run=new FormationCampaignRun(shop,871);Resolve(run);return run;
        }
        static void Resolve(FormationCampaignRun run)
        {for(int i=0;run.HasBlockingChoice;i++){Assert.That(i,Is.LessThan(20));Assert.That(run.Choose(0).Success,Is.True);}}
        [Test] public void CopierAddsExactlyOneCurrentOneStarCardWithoutGoldOrEquipmentDuplication()
        {
            var run=Fixture(("M17",2));var shop=run.Shop;var target=shop.Bench.Single();int initialItems=run.AllEquipment.Count;
            var gear=run.AddItem("AA");run.Equip(gear.Instance,target.Instance);var tool=run.AddItem(FormationCampaignRun.CopierKey);
            int gold=shop.Gold,stock=shop.Available(target.Id),copies=shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star));
            Assert.That(run.UseItem(tool.Instance,target.Instance).Success,Is.True);
            Assert.That(shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(copies+1));Assert.That(shop.Bench.Any(p=>p.Star==1),Is.True);
            Assert.That(shop.Available(target.Id),Is.EqualTo(stock-1));Assert.That(shop.Gold,Is.EqualTo(gold));
            Assert.That(run.AllEquipment.Count,Is.EqualTo(initialItems+1));Assert.That(gear.Owner,Is.EqualTo(target.Instance));
            Assert.That(run.UseCopier(tool.Instance,target.Instance).Success,Is.False);shop.Validate();
        }
        [Test] public void FullBenchCopierCanMergeAndPreservesConsumedBodiesGear()
        {
            var ids=FormationCampaignRules.AllUnits.Where(u=>u.Id!="M17"&&u.Id!="TAL").Take(ShopSeatLayout.ReserveCapacity-2).Select(u=>(u.Id,1));
            var run=Fixture(new[]{("M17",1),("M17",1)}.Concat(ids).ToArray());var shop=run.Shop;
            var targets=shop.Bench.Where(p=>p.Id==Card("M17")).ToArray();var gear=run.AddItem("AA");run.Equip(gear.Instance,targets.Last().Instance);
            var result=run.UseCopier(run.AddItem(FormationCampaignRun.CopierKey).Instance,targets.Last().Instance);
            Assert.That(result.Success,Is.True);Assert.That(shop.Bench.Single(p=>p.Id==Card("M17")).Star,Is.EqualTo(2));
            Assert.That(gear.Owner,Is.EqualTo(result.Instance));Assert.That(shop.Bench.Count,Is.EqualTo(ShopSeatLayout.ReserveCapacity-1));shop.Validate();
        }
        [Test] public void FullBenchNonMergeFailsAtomicallyAndKeepsCopier()
        {
            var run=Fixture(FormationCampaignRules.AllUnits.Where(u=>u.Id!="TAL").Take(ShopSeatLayout.ReserveCapacity).Select(u=>(u.Id,1)).ToArray());var shop=run.Shop;
            var target=shop.Bench.First();var tool=run.AddItem(FormationCampaignRun.CopierKey);int stock=shop.Available(target.Id),gold=shop.Gold;
            var before=shop.Bench.Select(p=>(p.Instance,p.Id,p.Star)).ToArray();
            Assert.That(run.UseCopier(tool.Instance,target.Instance).Success,Is.False);
            Assert.That(shop.Bench.Select(p=>(p.Instance,p.Id,p.Star)),Is.EqualTo(before));Assert.That(shop.Available(target.Id),Is.EqualTo(stock));
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(run.Inventory.Contains(tool),Is.True);shop.Validate();
        }
        [Test] public void ExhaustedStockAndInvalidTargetDoNotConsumeOrQueueCopier()
        {
            var run=Fixture(("M28",1));var shop=run.Shop;var target=shop.Bench.Single();var tool=run.AddItem(FormationCampaignRun.CopierKey);
            while(shop.Available(target.Id)>0)Assert.That(shop.ReserveRewardCandidate(target.Id),Is.Not.Null);
            Assert.That(run.UseCopier(tool.Instance,target.Instance).Success,Is.False);Assert.That(run.PendingDeliveries,Is.Zero);
            Assert.That(run.UseCopier(tool.Instance,-1).Success,Is.False);Assert.That(run.Inventory.Contains(tool),Is.True);shop.Validate();
        }
        [Test] public void HighestStarCannotConsumeCopier()
        {
            var run=Fixture(("M17",4));var tool=run.AddItem(FormationCampaignRun.CopierKey);
            Assert.That(run.UseCopier(tool.Instance,run.Shop.Bench.Single().Instance).Success,Is.False);Assert.That(run.Inventory.Contains(tool),Is.True);
        }
        [TestCase(4)] [TestCase(5)] public void TalulahCopierFollowsCurrentCostAndCopiesOnlyOneBaseCard(int cost)
        {
            var run=Fixture(("TAL",1),("TAL",1));var shop=run.Shop;
            while(shop.Definition(shop.TalulahId).Cost<cost)
            {while(shop.PendingTalulahChoice==0)Assert.That(shop.Grant(shop.TalulahId).Success,Is.True);Assert.That(shop.ResolveTalulah(true).Success,Is.True);}
            var target=shop.Bench.First(p=>p.Id==shop.TalulahId);int before=shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star));
            Assert.That(run.UseCopier(run.AddItem(FormationCampaignRun.CopierKey).Instance,target.Instance).Success,Is.True);
            Assert.That(shop.Definition(shop.TalulahId).Cost,Is.EqualTo(cost));Assert.That(shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)),Is.EqualTo(before+1));shop.Validate();
        }
        [Test] public void RemoverReturnsAllEquipmentIncludingBadgeWithoutChangingIdentityStacksOrRecipes()
        {
            var run=Fixture(("M17",1));var target=run.Shop.Bench.Single();var initial=run.Inventory.ToArray();
            var gear=new[]{run.AddItem("AA"),run.AddItem("EE"),run.AddItem("徽章:海嗣")};gear[1].Stacks=7;
            foreach(var item in gear)Assert.That(run.Equip(item.Instance,target.Instance).Success,Is.True);
            var tool=run.AddItem(FormationCampaignRun.RemoverKey);var result=run.UseItem(tool.Instance,target.Instance);
            Assert.That(result.Success,Is.True);Assert.That(result.Value,Is.EqualTo(3));Assert.That(run.Equipment(target.Instance),Is.Empty);
            Assert.That(run.Inventory,Is.EquivalentTo(initial.Concat(gear)));Assert.That(gear[1].Stacks,Is.EqualTo(7));Assert.That(gear.Select(i=>i.Key),Is.EqualTo(new[]{"AA","EE","徽章:海嗣"}));
            Assert.That(run.UseRemover(tool.Instance,target.Instance).Success,Is.False);
        }
        [Test] public void EmptyOrInvalidRemoverTargetRetainsToolAndCannotEquipConsumables()
        {
            var run=Fixture(("M17",1));int target=run.Shop.Bench.Single().Instance;var tool=run.AddItem(FormationCampaignRun.RemoverKey);
            Assert.That(run.UseRemover(tool.Instance,target).Success,Is.False);Assert.That(run.UseRemover(tool.Instance,-1).Success,Is.False);
            Assert.That(run.Inventory.Contains(tool),Is.True);Assert.That(run.Equip(tool.Instance,target).Success,Is.False);
            Assert.That(run.Equip(run.AddItem(FormationCampaignRun.CopierKey).Instance,target).Success,Is.False);
        }
        [Test] public void FreeUnequipDirectTransfersAndCrossOwnerCombinationCannotBypassRemover()
        {
            var run=Fixture(("M17",1),("M16",1));var a=run.Shop.Bench[0];var b=run.Shop.Bench[1];
            var first=run.AddItem("A");var second=run.AddItem("B");run.Equip(first.Instance,a.Instance);run.Equip(second.Instance,b.Instance);
            Assert.That(run.Unequip(first.Instance).Success,Is.False);Assert.That(run.Equip(first.Instance,b.Instance).Success,Is.False);
            Assert.That(run.Combine(first.Instance,second.Instance).Success,Is.False);Assert.That(first.Owner,Is.EqualTo(a.Instance));
            run.AddItem(FormationCampaignRun.RemoverKey);Assert.That(run.Unequip(first.Instance).Success,Is.True);Assert.That(first.Owner,Is.Zero);
            Assert.That(run.Equip(first.Instance,b.Instance).Success,Is.True);
        }
        [Test] public void SellingReturnsGearWithoutToolAndRemoverPreservesCrownPopulationAndOneTimeRewards()
        {
            var run=Fixture(("M17",1));int target=run.Shop.Bench.Single().Instance;
            foreach(var key in FormationCampaignRun.CrownKeys)run.Equip(run.AddItem(key).Instance,target);
            int gold=run.Shop.Gold,population=run.Shop.PopulationCap;
            Assert.That(run.UseRemover(run.AddItem(FormationCampaignRun.RemoverKey).Instance,target).Success,Is.True);
            Assert.That(run.Shop.PopulationCap,Is.EqualTo(population));Assert.That(run.Shop.Gold,Is.EqualTo(gold));
            foreach(var item in run.Inventory.ToArray())run.Equip(item.Instance,target);
            Assert.That(run.Shop.Gold,Is.EqualTo(gold));Assert.That(run.Shop.Sell(target).Success,Is.True);run.OnShopChanged();
            Assert.That(run.AllEquipment.All(i=>i.Owner==0),Is.True);run.Shop.Validate();
        }
        [Test] public void EachRewardNodeGuaranteesOneExtraRemoverEvenWhenOwnedAndSettlementCannotRepeat()
        {
            var run=Fixture(("M17",1));run.AddItem(FormationCampaignRun.RemoverKey);
            foreach(string id in new[]{"1-1","1-2","1-8","2-6","3-6"})
            {
                int index=Array.FindIndex(FormationCampaignRules.Nodes,n=>n.Id==id);typeof(FormationCampaignRun).GetProperty("NodeIndex").SetValue(run,index);
                int before=run.Inventory.Count(i=>i.Key==FormationCampaignRun.RemoverKey);
                Assert.That(run.SettleBattle(true,1,3,0,false),Is.Not.Null);
                var drop=run.PendingBalls.Single(d=>d.Node==id&&d.Component==FormationCampaignRun.RemoverKey);
                Assert.That(run.BeginBall(drop.Id),Is.Not.Null);Assert.That(run.CompleteBall(drop.Id),Is.Not.Null);
                Assert.That(run.Inventory.Count(i=>i.Key==FormationCampaignRun.RemoverKey),Is.EqualTo(before+1));
                Assert.That(run.SettleBattle(true,1,3,0,false),Is.Null);Assert.That(run.CompleteBall(drop.Id),Is.Null);
            }
        }
        [Test] public void CopiedNinthCardAwardsThreeStarMilestoneWithoutPurchaseLayers()
        {
            FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
            try
            {
                var run=Fixture(("M17",2),("M17",2),("M17",1),("M17",1),("M16",1),("B05",1));
                var target=run.Shop.Bench.First(p=>p.Id==Card("M17"));
                Assert.That(run.UseCopier(run.AddItem(FormationCampaignRun.CopierKey).Instance,target.Instance).Success,Is.True);
                Assert.That(run.Shop.Bench.Single(p=>p.Id==target.Id).Star,Is.EqualTo(3));Assert.That(run.Economy.ForesightLayers,Is.EqualTo(32));
                run.OnShopChanged();Assert.That(run.Economy.ForesightLayers,Is.EqualTo(32));
            }
            finally{FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);}
        }
        [Test] public void DuplicateRecipeOnWearerCannotEjectGearWithoutRemover()
        {
            var run=Fixture(("M17",1));int owner=run.Shop.Bench.Single().Instance;
            var a=run.AddItem("A");var b=run.AddItem("A");var advanced=run.AddItem("AA");
            foreach(var item in new[]{a,b,advanced})Assert.That(run.Equip(item.Instance,owner).Success,Is.True);
            Assert.That(run.Combine(a.Instance,b.Instance).Success,Is.False);
            Assert.That(run.Equipment(owner),Is.EquivalentTo(new[]{a,b,advanced}));
            Assert.That(run.UseRemover(run.AddItem(FormationCampaignRun.RemoverKey).Instance,owner).Success,Is.True);
            Assert.That(run.Combine(a.Instance,b.Instance).Success,Is.True);
        }
    }
}
