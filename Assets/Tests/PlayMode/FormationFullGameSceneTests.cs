using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationFullGameSceneTests
    {
        FormationBattleController controller;
        [TearDown] public void Freeze(){if(controller?.Model!=null)controller.Model.Paused=true;}

        delegate bool RewardPoint(int dropId, out Vector2 point);

        internal static IEnumerator ClaimOpeningRewards(FormationBattleController board)
        {
            const BindingFlags nonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(FormationBattleController);
            var tryRewardPoint = (RewardPoint)type.GetMethod("TryGroundRewardPoint", nonPublic)
                .CreateDelegate(typeof(RewardPoint), board);
            var rewardPointer = (Func<EventType, Vector2, bool>)type.GetMethod("ProcessFormationRewardPointer", nonPublic)
                .CreateDelegate(typeof(Func<EventType, Vector2, bool>), board);
            var run = board.Campaign;
            var drops = run.PendingBalls.ToArray();
            Assert.That(drops.Length, Is.EqualTo(2));
            Assert.That(drops.All(drop => drop.IsOpening), Is.True);
            Assert.That(board.Shop.Bench, Is.Empty);
            Assert.That(board.Model.Units, Is.Empty);
            Assert.That(run.Inventory, Is.Empty);

            foreach (var drop in drops.Reverse())
            {
                Assert.That(run.Choices, Is.Empty, "The preplan appears after both rewards arrive.");
                Assert.That(board.Session.CanShop, Is.False, "Opening grants reserve stock before trading starts.");
                double deadline = Time.realtimeSinceStartupAsDouble + 6;
                while (!tryRewardPoint(drop.Id, out _) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Assert.That(tryRewardPoint(drop.Id, out var point), Is.True, "The opening reward must be reachable on the real board.");
                int received = board.Shop.Bench.Count + run.Inventory.Count;
                Assert.That(rewardPointer(EventType.MouseDown, point), Is.True);
                Assert.That(rewardPointer(EventType.MouseUp, point), Is.True);
                Assert.That(board.Shop.Bench.Count + run.Inventory.Count, Is.EqualTo(received), "Opening grants land after the flight.");
                deadline = Time.realtimeSinceStartupAsDouble + 6;
                while (board.Shop.Bench.Count + run.Inventory.Count == received && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Assert.That(board.Shop.Bench.Count + run.Inventory.Count, Is.GreaterThan(received));
                Assert.That(run.PendingBalls.Any(item => item.Id == drop.Id), Is.False);
            }

            Assert.That(board.Shop.Gold, Is.EqualTo(8));
            Assert.That(board.Shop.Bench.Select(piece => piece.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(board.Shop.Bench.All(piece => piece.Star == 1 && board.Shop.Definition(piece.Id).Cost == 1), Is.True);
            Assert.That(board.Model.EnabledCount, Is.EqualTo(3));
            Assert.That(run.Inventory.Count, Is.EqualTo(1));
            Assert.That(new[] { "A", "B", "C", "D", "E", "F" }, Does.Contain(run.Inventory.Single().Key));
            Assert.That(run.Choices.Count, Is.EqualTo(3));
            Assert.That(run.ChoiceTitle, Does.Contain("战略预案"));
            board.Shop.Validate();
        }

        [UnityTest]
        public IEnumerator DifficultyPracticeSwitchesRealDefensesAndKeepsItsOwnArmy()
        {
            FormationShopSession.StartNew(20261117,fullCampaign:true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();
                if(controller!=null&&controller.Ready)break;
                yield return null;
            }
            Assert.That(controller?.Ready,Is.True);
            const BindingFlags commands=BindingFlags.Instance|BindingFlags.NonPublic;
            var select=typeof(FormationBattleController).GetMethod("SelectPracticeBattle",commands);
            var nodes=FormationCampaignRules.Nodes;
            select.Invoke(controller,new object[]{0});
            Assert.That(controller.Campaign.IsPractice,Is.True);
            Assert.That(controller.Shop.UnlimitedGold,Is.True);
            var session=controller.Session;
            for(int i=0;controller.Campaign.HasBlockingChoice;i++)
            {
                Assert.That(i,Is.LessThan(15));
                Assert.That(controller.ChooseCampaignOption(0).Success,Is.True);
            }

            int gold=controller.Shop.Gold;
            for(int i=0;i<3;i++)Assert.That(controller.BuyExperience().Success,Is.True);
            Assert.That(controller.RefreshMarket().Success,Is.True);
            int offer=Array.FindIndex(controller.Shop.Offers.ToArray(),id=>id!=null);
            Assert.That(offer,Is.GreaterThanOrEqualTo(0));
            Assert.That(controller.BuyOffer(offer).Success,Is.True);
            Assert.That(controller.Shop.Gold,Is.EqualTo(gold));
            var deployed=controller.Model.Units.Select(u=>(u.Instance,u.Spawn)).ToArray();
            var collection=controller.Shop.Bench.Select(p=>(p.Instance,p.Id,p.Star)).ToArray();
            var starter=controller.Campaign.Inventory.First();
            Assert.That(controller.Campaign.Equip(starter.Instance,deployed[0].Instance).Success,Is.True);

            foreach(int index in Enumerable.Range(0,nodes.Length))
            {
                int previous=controller.Campaign.NodeIndex;
                select.Invoke(controller,new object[]{index});
                if(nodes[index].DefenderCount==0)
                {
                    Assert.That(controller.Campaign.NodeIndex,Is.EqualTo(previous));
                    continue;
                }
                Assert.That(controller.Campaign.CurrentNode.Id,Is.EqualTo(nodes[index].Id));
                Assert.That(controller.Model.Round,Is.EqualTo(index+1));
                Assert.That(controller.Model.Defenders.Count,Is.EqualTo(nodes[index].DefenderCount));
                Assert.That(controller.Model.LimitSeconds,Is.EqualTo(nodes[index].TimeLimit));
            }
            Assert.That(controller.Session,Is.SameAs(session));
            Assert.That(controller.Campaign.CurrentNode.Id,Is.EqualTo("3-7"));
            Assert.That(controller.Model.BossAlive,Is.True);
            Assert.That(controller.Model.Units.Select(u=>(u.Instance,u.Spawn)),Is.EqualTo(deployed));
            Assert.That(controller.Shop.Bench.Select(p=>(p.Instance,p.Id,p.Star)),Is.EqualTo(collection));
            Assert.That(controller.Campaign.Equipment(deployed[0].Instance).Select(x=>x.Instance),Does.Contain(starter.Instance));
            var defense=controller.Model.Defenders.Select(d=>(d.ArtId,d.Position,d.MaxHealth)).ToArray();
            Assert.That(controller.Model.StartBattle(),Is.True);
            controller.Model.Advance(151);
            Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Lost).Or.EqualTo(FormationPhase.Won));
            yield return null;
            Assert.That(controller.Campaign.NodeSettled,Is.False);
            controller.ResetDemo();
            Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation));
            Assert.That(controller.Model.Clock,Is.Zero);
            Assert.That(controller.Model.Defenders.Select(d=>(d.ArtId,d.Position,d.MaxHealth)),Is.EqualTo(defense));

            typeof(FormationBattleController).GetMethod("StartNewFormationChallenge",commands).Invoke(controller,new object[]{true});
            Assert.That(controller.Campaign.CurrentNode.Id,Is.EqualTo("1-1"));
            Assert.That(controller.Shop.UnlimitedGold,Is.False);
            Assert.That(controller.Shop.Gold,Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator ApprovedFiniteChallengeUsesRealChoicesEquipmentAndCurrentBoard()
        {
            FormationShopSession.StartNew(20261117,fullCampaign:true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {controller=UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();if(controller!=null&&controller.Ready)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready,Is.True);
            Assert.That(controller.Campaign,Is.Not.Null);Assert.That(controller.Shop.UnlimitedGold,Is.False);
            Assert.That(controller.Shop.Gold,Is.EqualTo(8));Assert.That(controller.Shop.Level,Is.EqualTo(3));
            Assert.That(controller.Shop.Roles.Count,Is.EqualTo(46));
            controller.StartBattle();Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation),"The opening drops must be collected before combat.");
            yield return ClaimOpeningRewards(controller);
            controller.StartBattle();Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation),"The preplan must be confirmed before combat.");
            var old=controller.Campaign.Choices.Select(c=>c.Id).ToArray();
            Assert.That(controller.Campaign.RerollChoice(),Is.True);
            Assert.That(controller.Campaign.Choices.Any(c=>old.Contains(c.Id)),Is.False);
            int safety=0;
            while(controller.Campaign.HasBlockingChoice)
            {Assert.That(++safety,Is.LessThan(15));Assert.That(controller.ChooseCampaignOption(0).Success,Is.True);yield return null;}
            Assert.That(controller.Session.CanShop,Is.True);
            var piece=controller.Session.Seats.Lineup(controller.Shop.Bench).First();
            var item=controller.Campaign.Inventory.First();
            Assert.That(controller.Campaign.Equip(item.Instance,piece.Instance).Success,Is.True);
            controller.Session.ApplyTo(controller.Model);
            Assert.That(controller.Campaign.Equipment(piece.Instance).Count,Is.EqualTo(1));
            Assert.That(controller.Model.Units.Count,Is.EqualTo(3));
            Assert.That(controller.Model.Defenders.Count,Is.EqualTo(3));
            Assert.That(double.IsPositiveInfinity(controller.Model.LimitSeconds),Is.True,"Opening reward battles must have no deadline.");
            Assert.That(controller.terrain.regions.Length,Is.EqualTo(4));
            Assert.That(FormationBattleModel.Width,Is.EqualTo(24));
            Assert.That(FormationBattleModel.Height,Is.EqualTo(8));
            Assert.That(FormationBattleModel.DeploymentColumns,Is.EqualTo(10));
            controller.SetMarketVisible(false);controller.SetFormationView(true);yield return null;
            Assert.That(controller.MarketVisible,Is.False,"The finite preparation shop can collapse to reveal the normal sized board.");
            Assert.That(controller.ViewingDefenders,Is.True);
            controller.SetFormationView(false);yield return null;
            Assert.That(controller.ViewingDefenders,Is.False);
        }

        delegate bool ItemTilePoint(int instance, out Vector2 point);
        delegate bool EquippedSlot(int instance, out Rect slot);

        [UnityTest]
        public IEnumerator EquipmentRailCanInspectAndDragToFieldAndBench()
        {
            FormationShopSession.StartNew(20261117, fullCampaign: true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                controller = UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();
                if (controller != null && controller.Ready) break;
                yield return null;
            }
            Assert.That(controller?.Ready, Is.True);
            yield return ClaimOpeningRewards(controller);
            for (int i = 0; controller.Campaign.HasBlockingChoice; i++)
            {
                Assert.That(i, Is.LessThan(15));
                Assert.That(controller.ChooseCampaignOption(0).Success, Is.True);
            }

            var startingArmy = controller.Session.Seats.Lineup(controller.Shop.Bench).ToArray();
            int onBoard = startingArmy[0].Instance;
            int inReserve = startingArmy[1].Instance;
            int reserveSlot = controller.Session.Seats.FirstEmpty(false, 10);
            Assert.That(controller.MovePiece(inReserve, false, reserveSlot).Success, Is.True);
            controller.SetMarketVisible(false);
            controller.SetFormationView(false);
            deadline = Time.realtimeSinceStartupAsDouble + 600;
            while ((!controller.RosterArtReady || controller.ShopPanelTransitioning) &&
                   Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.RosterArtReady, Is.True);
            Assert.That(controller.ShopPanelTransitioning, Is.False);

            // Grant fixed samples to exercise the UI independently of the random reward pool.
            var fieldItem = controller.Campaign.AddItem("AB");
            var reserveItem = controller.Campaign.AddItem("D");
            var gem = controller.Campaign.AddItem("赤宝石");
            var originalPositions = controller.Model.Units.Select(unit => (unit.Instance, unit.Spawn)).ToArray();
            int gold = controller.Shop.Gold;

            const BindingFlags nonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(FormationBattleController);
            var equipmentPointer = (Func<EventType, Vector2, bool>) type
                .GetMethod("ProcessFormationEquipmentPointer", nonPublic)
                .CreateDelegate(typeof(Func<EventType, Vector2, bool>), controller);
            var equipmentTilePoint = (ItemTilePoint) type.GetMethod("TryEquipmentTilePoint", nonPublic)
                .CreateDelegate(typeof(ItemTilePoint), controller);
            var inspectedItem = type.GetProperty("FormationInspectedItemId", nonPublic);
            var detailsRect = type.GetProperty("FormationEquipmentDetailsRect", nonPublic);

            void DragItem(int item, Vector2 destination)
            {
                Assert.That(equipmentTilePoint(item, out var source), Is.True, "The item must be visible on the equipment rail.");
                Assert.That(equipmentPointer(EventType.MouseDown, source), Is.True);
                Assert.That(equipmentPointer(EventType.MouseDrag, destination), Is.True);
                Assert.That(equipmentPointer(EventType.MouseUp, destination), Is.True);
            }

            Assert.That(equipmentTilePoint(fieldItem.Instance, out var railPoint), Is.True);
            Assert.That(equipmentPointer(EventType.MouseDown, railPoint), Is.True);
            Assert.That(equipmentPointer(EventType.MouseUp, railPoint), Is.True);
            Assert.That(inspectedItem.GetValue(controller), Is.EqualTo(fieldItem.Instance));
            Assert.That(((Rect) detailsRect.GetValue(controller)).width, Is.GreaterThan(0));
            Assert.That(fieldItem.Owner, Is.Zero, "Inspecting an inventory item must leave it unequipped.");

            var bodyPoint = controller.FormationPieceRect(onBoard).center;
            Assert.That(FormationBattleController.BoardInputRect.Contains(bodyPoint), Is.True);
            DragItem(fieldItem.Instance, bodyPoint);
            Assert.That(fieldItem.Owner, Is.EqualTo(onBoard));
            Assert.That(equipmentTilePoint(fieldItem.Instance, out _), Is.False);

            var reservePoint = FormationBattleController.FormationReserveRect(reserveSlot).center;
            var emptySlot = controller.Session.Seats.FirstEmpty(false, 10);
            DragItem(reserveItem.Instance, FormationBattleController.FormationReserveRect(emptySlot).center);
            Assert.That(reserveItem.Owner, Is.Zero, "An empty reserve seat cannot receive equipment.");
            DragItem(gem.Instance, reservePoint);
            Assert.That(gem.Owner, Is.Zero, "A gem is a crafting material and must stay in inventory.");
            DragItem(reserveItem.Instance, reservePoint);
            Assert.That(reserveItem.Owner, Is.EqualTo(inReserve));
            Assert.That(controller.Model.Units.Select(unit => (unit.Instance, unit.Spawn)), Is.EqualTo(originalPositions));
            Assert.That(controller.Shop.Gold, Is.EqualTo(gold));

            controller.ProcessPointer(EventType.MouseDown, reservePoint);
            controller.ProcessPointer(EventType.MouseUp, reservePoint);
            Assert.That(controller.FormationInspectedCardId, Is.EqualTo(startingArmy[1].Id));
            var tryEquippedSlot = (EquippedSlot) type.GetMethod("TryFormationInspectedEquipmentSlot", nonPublic)
                .CreateDelegate(typeof(EquippedSlot), controller);
            Assert.That(tryEquippedSlot(reserveItem.Instance, out var equippedSlot), Is.True);
            Assert.That(tryEquippedSlot(fieldItem.Instance, out _), Is.False);
            type.GetMethod("ToggleFormationEquipmentDetails", nonPublic)
                .Invoke(controller, new object[] { reserveItem.Instance, equippedSlot });
            Assert.That(inspectedItem.GetValue(controller), Is.EqualTo(reserveItem.Instance));
            Assert.That(((Rect) detailsRect.GetValue(controller)).width, Is.GreaterThan(0));
            Assert.That(controller.FormationInspectedCardId, Is.EqualTo(startingArmy[1].Id));

            controller.StartBattle();
            Assert.That(controller.Model.Phase, Is.EqualTo(FormationPhase.Fighting));
            Assert.That(inspectedItem.GetValue(controller), Is.EqualTo(0));
            Assert.That(equipmentPointer(EventType.MouseDown, railPoint), Is.False);
            Assert.That(controller.Campaign.Equipment(onBoard).Single().Instance, Is.EqualTo(fieldItem.Instance));
            Assert.That(controller.Campaign.Equipment(inReserve).Single().Instance, Is.EqualTo(reserveItem.Instance));
        }
    }
}
