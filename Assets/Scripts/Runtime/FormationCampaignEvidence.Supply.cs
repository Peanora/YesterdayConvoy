using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignEvidence
    {
        const string SupplyEvidenceFlag = "-formationSupplyEvidence";
        static bool SupplyRequested => Array.IndexOf(Environment.GetCommandLineArgs(), SupplyEvidenceFlag) >= 0;

        static int OrdinarySupplyFixtureChoice(FormationCampaignRun run)
            => Array.FindIndex(run.Choices.ToArray(), option => option.Id != "P07" && option.Id != "EP5" && option.Id != "ES6");

        static bool HasMixedSupplyDraw(int seed)
        {
            var run = CompleteSeededOpeningDelivery(seed);
            while (run.CurrentNode.Id != "1-5")
            {
                while (run.HasBlockingChoice)
                {
                    int choice = OrdinarySupplyFixtureChoice(run);
                    if (choice < 0 || !run.Choose(choice).Success) return false;
                }
                if (run.SettleBattle(true, 1, run.CurrentNode.DefenderCount, 0, false) == null || !run.AdvanceNode())
                    return false;
            }

            return run.SupplyPicksRemaining == 1 && run.Choices.Count == 5 &&
                run.Choices.All(option => option.SupplyUnit != null) &&
                run.Choices.Select(option => run.Shop.Definition(option.SupplyUnit.CardId).Cost).Distinct().Count() == 2;
        }

        IEnumerator VerifySupplyChoice()
        {
            result.scope = "Windows Player with the production 1-5 supply, normal UI input and actual stock. Earlier battles use fixture settlements through normal node transitions.";
            result.node = "1-5";
            int seed = Enumerable.Range(controller.seed, 64).FirstOrDefault(HasMixedSupplyDraw);
            Assert(seed != 0, "Seeded production draw contains both 2-cost and 3-cost offers");
            controller.seed = seed - 1;
            controller.ResetDemo();
            yield return null;
            yield return WaitForOpeningDelivery();

            var run = controller.Campaign;
            while (run.CurrentNode.Id != "1-5")
            {
                while (run.HasBlockingChoice)
                {
                    int choice = OrdinarySupplyFixtureChoice(run);
                    Assert(choice >= 0 && controller.ChooseCampaignOption(choice).Success,
                        "Resolve regular fixture choices before " + run.CurrentNode.Id);
                }
                Assert(run.SettleBattle(true, 1, run.CurrentNode.DefenderCount, 0, false) != null,
                    "Settle the progression fixture at " + run.CurrentNode.Id);
                if (run.CurrentNode.Id == "1-4") yield return Frame("00-board-before-supply");
                Assert(controller.ContinueCampaign(), "Advance to " + run.CurrentNode.Id + " through the real controller");
            }

            bool captureFrames = Array.IndexOf(Environment.GetCommandLineArgs(), "-formationUiClips") >= 0;
            if (captureFrames) yield return CaptureInterfaceClip("01-supply-reveal", 42);
            else yield return new WaitForSecondsRealtime(1.8f);
            var shop = controller.Shop;
            var options = run.Choices.ToArray();
            Assert(run.SupplyPicksRemaining == 1 && options.Length == 5 &&
                options.All(option => option.SupplyUnit != null && option.SupplyComponent != null),
                "The modal receives five real one-card and one-component packages");
            var ids = options.Select(option => option.SupplyUnit.CardId).ToArray();
            var costs = ids.Select(id => shop.Definition(id).Cost).ToArray();
            Assert(ids.Distinct().Count() == 5 && costs.Contains(2) && costs.Contains(3) &&
                ids.All(id => shop.ReservedRewards(id) == 1),
                "The five distinct 2–3 cost candidates each reserve a single card copy");
            Assert(options.All(option => FormationBattleController.SupplyItemArt(option.SupplyComponent) != null),
                "All five live component icons load in Player");
            Assert(run.HasBlockingChoice && !run.NodeSettled && !controller.Session.CanShop && !controller.ContinueCampaign(),
                "Supply blocks trading and progression until a package is confirmed");

            int selected = Array.IndexOf(costs, 3);
            string selectedId = ids[selected];
            string selectedComponent = options[selected].SupplyComponent;
            int UnownedOutsideRewards(string id) => shop.Available(id) + shop.Offers.Count(offer => offer == id);
            var available = ids.ToDictionary(id => id, UnownedOutsideRewards);
            int ownedCopies = shop.Bench.Sum(piece => ShopTrialModel.Copies(piece.Star));
            int selectedCopies = shop.Bench.Where(piece => piece.Id == selectedId).Sum(piece => ShopTrialModel.Copies(piece.Star));
            var itemInstances = run.Inventory.Select(item => item.Instance).ToHashSet();
            int gold = shop.Gold, xp = shop.XP;

            yield return Frame("02-supply-open");
            yield return PressStrategyUiKey(0x0d);
            Assert(!run.NodeSettled && run.HasBlockingChoice && ids.All(id => shop.ReservedRewards(id) == 1),
                "Enter without a selected package keeps the supply open");

            yield return ClickStrategyUi(new Vector2(218.24f + selected * 210.88f, 350));
            Assert(!run.NodeSettled && run.HasBlockingChoice && ids.All(id => shop.ReservedRewards(id) == 1) &&
                shop.Bench.Sum(piece => ShopTrialModel.Copies(piece.Star)) == ownedCopies &&
                run.Inventory.All(item => itemInstances.Contains(item.Instance)),
                "Clicking the card selects its whole package without committing either reward");
            yield return Frame("03-supply-selected");

            yield return ClickStrategyUi(new Vector2(640, 637));
            if (captureFrames) yield return CaptureInterfaceClip("04-supply-confirm", 27);
            yield return WaitForStrategyUi(() => run.CurrentNode.Id == "1-6" && !run.HasBlockingChoice,
                "The production confirmation automatically reaches the next battle without another click");
            var delivered = run.Inventory.Where(item => !itemInstances.Contains(item.Instance)).ToArray();
            Assert(delivered.Length == 1 && delivered[0].Key == selectedComponent &&
                shop.Bench.Sum(piece => ShopTrialModel.Copies(piece.Star)) == ownedCopies + 1 &&
                shop.Bench.Where(piece => piece.Id == selectedId).Sum(piece => ShopTrialModel.Copies(piece.Star)) == selectedCopies + 1,
                "Exactly one copy of the selected character and its displayed component reach the player");
            Assert(shop.RewardReservations.Count == 0 &&
                ids.All(id => UnownedOutsideRewards(id) == available[id] + (id == selectedId ? 0 : 1)) &&
                shop.Gold == gold && shop.XP == xp,
                "Unchosen stock returns once and the supply neither charges nor awards gold or XP");
            shop.Validate();
            Assert(controller.Model.Phase == FormationPhase.Preparation,
                "Automatic progression leaves the next node ready for normal deployment");
            yield return Frame("05-next-battle-preparation");
            yield return new WaitForSecondsRealtime(.65f);
        }
    }
}
