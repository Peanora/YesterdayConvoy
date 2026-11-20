using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationPreparationLayoutTests
    {
        FormationBattleController controller;

        [TearDown]
        public void FreezeFixture()
        { if (controller?.Model != null) controller.Model.Paused = true; }

        IEnumerator OpenFinitePreparation()
        {
            FormationShopSession.StartNew(20261117, fullCampaign: true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                controller = Object.FindAnyObjectByType<FormationBattleController>();
                if (controller != null && controller.Ready) break;
                yield return null;
            }
            Assert.That(controller, Is.Not.Null); Assert.That(controller.Ready, Is.True);
            Assert.That(controller.Campaign, Is.Not.Null); Assert.That(controller.Shop.UnlimitedGold, Is.False);
            int choices = 0;
            while (controller.Campaign.HasBlockingChoice)
            {
                Assert.That(++choices, Is.LessThan(15));
                Assert.That(controller.ChooseCampaignOption(0).Success, Is.True);
                yield return null;
            }
            yield return RosterReady();
        }

        IEnumerator RosterReady()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (!controller.RosterArtReady && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.RosterArtReady, Is.True);
            yield return null;
        }

        Vector2 Project(Vector3 point) => TacticalViewport.Current.FromScreen(controller.battleCamera.WorldToScreenPoint(point));

        IEnumerator Market(bool visible)
        {
            controller.SetMarketVisible(visible);
            double deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.ShopPanelTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.MarketVisible, Is.EqualTo(visible));
            Assert.That(controller.ShopPanelVisibility, Is.EqualTo(visible ? 1 : 0).Within(.001f));
            yield return null;
        }

        Vector2Int FreeCell(int instance, bool underneathDetails = false)
        {
            for (int column = 0; column < FormationBattleModel.DeploymentColumns; column++)
                for (int row = 0; row < FormationBattleModel.Height; row++)
                {
                    var cell = new Vector2Int(column, row);
                    var point = FormationBattleController.CellScreen(cell);
                    var preview = controller.Session.PreviewDeploy(instance, cell);
                    if (preview.Valid && preview.SwapInstance == 0 && FormationBattleController.BoardInputRect.Contains(point) &&
                        (!underneathDetails || FormationBattleController.FormationTraitDetailsRect.Contains(point))) return cell;
                }
            throw new InvalidOperationException("No legal empty cell satisfies the real scene projection.");
        }

        void AssertNormalBoardUncovered(bool defenders)
        {
            float first = defenders ? FormationBattleModel.DeploymentColumns + 1 : 0;
            float last = defenders ? FormationBattleModel.Width - 1 : FormationBattleModel.DeploymentColumns;
            for (float column = first; column <= last; column++)
                for (int row = 0; row < FormationBattleModel.Height; row++)
                    foreach (float dx in new[] { -.5f, .5f })
                        foreach (float dy in new[] { -.5f, .5f })
                        {
                            var point = Project(FormationBattleController.WorldPoint(new Vector2(column + dx, row + dy)));
                            Assert.That(FormationBattleController.BoardInputRect.Contains(point), Is.True, "The complete focused board must remain in the usable map area.");
                        }
            var actors = defenders ? controller.Model.Defenders.Cast<FormationActor>() : controller.Model.Units.Cast<FormationActor>();
            foreach (var actor in actors)
            {
                var head = Project(controller.ActorWorldPoint(actor) + controller.battleCamera.transform.up * controller.ActorPresentationHeight(actor));
                Assert.That(FormationBattleController.BoardInputRect.Contains(head), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator CoinDrawerAndReserveOnlyLayoutSupportRealPurchaseUpgradeAndDirectDeployment()
        {
            yield return OpenFinitePreparation();
            Assert.That(FormationBattleModel.Width * FormationBattleModel.Height, Is.EqualTo(192));
            Assert.That(FormationBattleModel.DeploymentColumns, Is.EqualTo(10), "Only the left deployment area shrinks; the guard half keeps its twelve columns.");
            Assert.That(FormationBattleModel.Width - FormationBattleModel.DeploymentColumns - 2, Is.EqualTo(12));
            Assert.That(controller.FormationPreparationUiVisible, Is.True);
            Assert.That(controller.CombatViewActive, Is.False);
            Assert.That(controller.MarketVisible, Is.False, "Ordinary preparation starts with the coin drawer collapsed.");
            yield return Market(false);
            AssertNormalBoardUncovered(false);
            Assert.That(Enumerable.Range(0, 11).All(i => FormationBattleController.FormationLineupRect(i) == Rect.zero), Is.True);
            Assert.That(controller.Model.Units.All(u => controller.ShopBenchModel(u.Instance) == null || !controller.ShopBenchModel(u.Instance).enabled), Is.True, "Deployed actors must not have a duplicate lineup model in the bottom reserve stage.");
            var dock = FormationBattleController.FormationBenchRect;
            Assert.That(dock.height, Is.EqualTo(86));
            Assert.That(dock.yMin, Is.EqualTo(FormationBattleController.FormationExperienceRect.yMin));
            Assert.That(dock.yMin, Is.EqualTo(FormationBattleController.FormationInterestRect.yMin));
            Assert.That(dock.yMax, Is.LessThanOrEqualTo(720));
            var slots = Enumerable.Range(0, 10).Select(FormationBattleController.FormationReserveRect).ToArray();
            Assert.That(slots.All(slot => dock.Contains(slot.min) && dock.Contains(slot.max - Vector2.one)), Is.True);
            Assert.That(slots.Zip(slots.Skip(1), (left, right) => left.xMax < right.xMin).All(separated => separated), Is.True);
            Assert.That(FormationBattleController.FormationExperienceRect.xMax, Is.LessThan(FormationBattleController.FormationReserveRect(0).xMin));
            Assert.That(FormationBattleController.FormationCoinRect.xMin, Is.GreaterThan(FormationBattleController.FormationReserveRect(9).xMax));
            Assert.That(FormationBattleController.FormationInterestRect.yMax, Is.LessThanOrEqualTo(FormationBattleController.FormationCoinRect.yMin));
            Assert.That(controller.FormationDisplayedInterest, Is.EqualTo(Math.Min(controller.Shop.InterestCap, controller.Shop.Gold / controller.Shop.InterestDivisor)));
            controller.SetFormationView(true); yield return null;
            AssertNormalBoardUncovered(true);
            controller.SetFormationView(false); yield return null;
            var cameraPose = controller.battleCamera.transform.position;
            controller.ToggleFormationMarket();
            yield return Market(true);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(cameraPose), "Opening recruitment leaves the fixed camera pose unchanged.");
            var market = FormationBattleController.FormationMarketRect;
            Assert.That(market.yMin, Is.EqualTo(77));
            Assert.That(market.yMax, Is.LessThan(FormationBattleController.BoardInputRect.center.y));
            for (int index = 0; index < controller.Shop.Offers.Count; index++)
            {
                var card = FormationBattleController.FormationOfferRect(index);
                Assert.That(card.height, Is.EqualTo(120), "The top drawer uses the tall recruitment card format.");
                Assert.That(market.Contains(card.min) && market.Contains(card.max - Vector2.one), Is.True);
                Assert.That(card.Overlaps(dock), Is.False);
            }
            yield return Capture("playmode-currency-drawer-open");

            var owned = controller.Shop.Bench.Select(piece => piece.Id).ToArray();
            int offer = Enumerable.Range(0, controller.Shop.Offers.Count)
                .Where(index => controller.Shop.Offers[index] != null && !owned.Contains(controller.Shop.Offers[index]) &&
                    controller.Shop.Definition(controller.Shop.Offers[index]).Cost <= controller.Shop.Gold)
                .DefaultIfEmpty(-1).First();
            Assert.That(offer, Is.GreaterThanOrEqualTo(0), "The actual finite opening shop must provide an affordable new kind for this deterministic path.");
            int beforeGold = controller.Shop.Gold;
            string cardId = controller.Shop.Offers[offer];
            int price = controller.Shop.Definition(cardId).Cost;
            var cardRect = FormationBattleController.FormationOfferRect(offer);
            var coveredPositions = controller.Model.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
            controller.ProcessPointer(EventType.MouseDown, cardRect.center);
            controller.ProcessPointer(EventType.MouseDrag, cardRect.center + Vector2.right * 18);
            controller.ProcessPointer(EventType.MouseUp, cardRect.center);
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == coveredPositions[unit.Instance]), Is.True);
            Assert.That(controller.Shop.Offers[offer], Is.EqualTo(cardId), "Dragging the card must not purchase it.");

            var cardFooter = new Vector2(cardRect.center.x, cardRect.yMax - 12);
            controller.ProcessPointer(EventType.MouseDown, cardFooter);
            Assert.That(controller.Shop.Gold, Is.EqualTo(beforeGold));
            controller.ProcessPointer(EventType.MouseUp, cardFooter);
            Assert.That(controller.Shop.Gold, Is.EqualTo(beforeGold - price));
            Assert.That(controller.Shop.Offers[offer], Is.Null);
            Assert.That(controller.FormationInspectedCardId, Is.Null);
            var bought = controller.Shop.Bench.Single(piece => piece.Id == cardId);
            Assert.That(controller.Session.Seats.InLineup(bought.Instance), Is.False);
            controller.ProcessPointer(EventType.MouseDown, cardFooter);
            controller.ProcessPointer(EventType.MouseUp, cardFooter);
            Assert.That(controller.Shop.Gold, Is.EqualTo(beforeGold - price), "An empty slot cannot be purchased again.");
            Assert.That(controller.BuyExperience().Success, Is.True);
            Assert.That(controller.Shop.Level, Is.EqualTo(3));
            Assert.That(controller.Shop.XP, Is.EqualTo(4));
            Assert.That(controller.Shop.Gold, Is.EqualTo(beforeGold - price - 4), "The actual purchase and experience payment use the same finite gold account.");
            var replaced = controller.Session.Seats.Lineup(controller.Shop.Bench).First();
            Assert.That(controller.MovePiece(replaced.Instance, false, controller.Session.Seats.FirstEmpty(false, 10)).Success, Is.True);
            yield return Market(false);
            var origin = controller.FormationPieceRect(bought.Instance).center;
            var destination = FreeCell(bought.Instance);
            var target = FormationBattleController.CellScreen(destination);
            controller.ProcessPointer(EventType.MouseDown, origin);
            controller.ProcessPointer(EventType.MouseDrag, target);
            Assert.That(controller.DeploymentPreview, Is.Not.Null);
            Assert.That(controller.DeploymentPreview.Valid, Is.True);
            controller.ProcessPointer(EventType.MouseUp, target);
            Assert.That(controller.Session.Seats.InLineup(bought.Instance), Is.True);
            Assert.That(controller.Model.Units.Single(unit => unit.Instance == bought.Instance).Spawn, Is.EqualTo((Vector2)destination));
            Assert.That(controller.Model.EnabledCount, Is.EqualTo(3));
            Assert.That(controller.MarketVisible, Is.False);
            yield return RosterReady();
            AssertNormalBoardUncovered(false);
            var deployed = controller.Model.Units.Single(unit => unit.Instance == bought.Instance);
            var returnSeat = controller.Session.Seats.FirstEmpty(false, 10);
            var body = controller.FormationPieceRect(deployed.Instance).center;
            var reserveTarget = FormationBattleController.FormationReserveRect(returnSeat).center;
            controller.ProcessPointer(EventType.MouseDown, body);
            controller.ProcessPointer(EventType.MouseUp, body);
            Assert.That(controller.FormationInspectedCardId, Is.EqualTo(bought.Id),
                "A click on a real deployed body inspects that card without moving it.");
            Assert.That(controller.Model.Units.Single(unit => unit.Instance == bought.Instance).Spawn, Is.EqualTo((Vector2)destination));
            controller.ProcessPointer(EventType.MouseDown, body);
            controller.ProcessPointer(EventType.MouseUp, body);
            Assert.That(controller.FormationInspectedCardId, Is.Null, "Clicking the same body again closes its detail.");
            controller.ProcessPointer(EventType.MouseDown, body);
            controller.ProcessPointer(EventType.MouseDrag, reserveTarget);
            controller.ProcessPointer(EventType.MouseUp, reserveTarget);
            Assert.That(controller.FormationInspectedCardId, Is.Null, "Dragging a body to the bench must not open its detail.");
            Assert.That(controller.Session.Seats.InLineup(bought.Instance), Is.False, "The actual board body can return directly to a reserve slot.");
            Assert.That(controller.Model.EnabledCount, Is.EqualTo(2));
            controller.ProcessPointer(EventType.MouseDown, controller.FormationPieceRect(bought.Instance).center);
            controller.ProcessPointer(EventType.MouseDrag, target);
            controller.ProcessPointer(EventType.MouseUp, target);
            Assert.That(controller.Model.EnabledCount, Is.EqualTo(3));
            yield return RosterReady();
            yield return Capture("playmode-currency-reserve-only-preparation");

            string inspected = controller.FormationTraitRows.First().Id;
            Assert.That(controller.InspectFormationTrait(inspected), Is.True);
            Assert.That(controller.FormationTraitDetailsOpen, Is.True);
            controller.StartBattle(); controller.Model.Paused = true;
            Assert.That(controller.Model.Phase, Is.EqualTo(FormationPhase.Fighting));
            Assert.That(controller.FormationTraitDetailsOpen, Is.False, "Starting combat must dismiss the open preparation detail panel.");
            Assert.That(controller.SelectedFormationTraitId, Is.Null);
            Assert.That(controller.RefreshMarket().Success, Is.False);
            Assert.That(controller.BuyOffer(0).Success, Is.False);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(controller.CombatViewActive, Is.True, "Pause must retain the battle-only layout.");
            Assert.That(controller.FormationPreparationUiVisible, Is.False, "Traits, reserve slots, gold, XP and operating controls share the hidden preparation layer.");
            Assert.That(controller.InspectFormationTrait(inspected), Is.False);
            controller.ToggleFormationMarket(); controller.SetMarketVisible(true);
            Assert.That(controller.MarketVisible, Is.False, "Combat still removes the preparation-only shop.");
            Assert.That(controller.ShopPanelVisibility, Is.EqualTo(0).Within(.001f));
            Assert.That(controller.FormationTraitDetailsOpen, Is.False);
            Assert.That(controller.Shop.Bench.All(piece =>
                (controller.ShopBenchModel(piece.Instance) == null || !controller.ShopBenchModel(piece.Instance).enabled) &&
                (controller.ShopEntranceModel(piece.Instance) == null || !controller.ShopEntranceModel(piece.Instance).enabled)), Is.True,
                "Hidden reserve cards must not leave standing models or recruitment arrivals in the battle render.");
            yield return Capture("playmode-battle-only-paused");

            // Advance ordinary production simulation to its actual result. The reward node
            // remains unlimited; no actor statistics, phase, damage or outcome are injected.
            controller.Model.Paused = false;
            int battleFrames = 0;
            while (controller.Model.Phase == FormationPhase.Fighting || controller.Model.Phase == FormationPhase.Exiting)
            {
                Assert.That(++battleFrames, Is.LessThan(3000), "The natural opening reward battle must reach a legal terminal state.");
                for (int step = 0; step < 12; step++) controller.Model.Advance(.25f);
                yield return null;
            }
            Assert.That(controller.Model.Phase, Is.EqualTo(FormationPhase.Won).Or.EqualTo(FormationPhase.Lost));
            yield return null;
            Assert.That(controller.CombatViewActive, Is.False);
            Assert.That(controller.FormationPreparationUiVisible, Is.True, "The terminal result restores the preparation layer without allowing trait details over its receipt.");
            Assert.That(controller.Campaign.NodeSettled, Is.True);
            int rewards = 0;
            while (controller.Campaign.HasBlockingChoice)
            {
                Assert.That(++rewards, Is.LessThan(30));
                Assert.That(controller.ChooseCampaignOption(0).Success, Is.True);
                yield return null;
            }
            Assert.That(controller.InspectFormationTrait(inspected), Is.False, "A finished battle result must not be covered by a newly opened trait panel.");
            Assert.That(controller.FormationTraitDetailsOpen, Is.False);
            Assert.That(controller.Shop.Level, Is.EqualTo(4), "The paid four XP and ordinary battle's two XP unlock the fourth population through production settlement.");
            Assert.That(controller.ContinueCampaign(), Is.True);
            yield return null; yield return Market(false);
            Assert.That(controller.FormationPreparationUiVisible, Is.True);
            Assert.That(controller.CombatViewActive, Is.False);
            Assert.That(controller.ShopBenchModel(replaced.Instance), Is.Not.Null);
            Assert.That(controller.ShopBenchModel(replaced.Instance).enabled, Is.True, "The actual reserve model returns in the next preparation phase.");
            Assert.That(controller.Campaign.HasBlockingChoice, Is.False);
            var fourthCell = FreeCell(replaced.Instance);
            var fourthPoint = FormationBattleController.CellScreen(fourthCell);
            controller.ProcessPointer(EventType.MouseDown, controller.FormationPieceRect(replaced.Instance).center);
            controller.ProcessPointer(EventType.MouseDrag, fourthPoint);
            controller.ProcessPointer(EventType.MouseUp, fourthPoint);
            Assert.That(controller.Session.Seats.InLineup(replaced.Instance), Is.True);
            Assert.That(controller.Model.EnabledCount, Is.EqualTo(4), "New population is filled directly from reserve onto the board, without a screen lineup slot.");
            yield return RosterReady();
            yield return Capture("playmode-currency-four-population");
        }

        [UnityTest]
        public IEnumerator TraitDetailsFollowOwnedReserveLineupAndBadgeActivationWithoutPointerLeakage()
        {
            yield return OpenFinitePreparation();
            var hound = controller.Shop.Bench.Single(piece => piece.Id == "CARD_M03");
            var host = controller.Shop.Bench.Single(piece => piece.Id == "CARD_M21");
            int houndSeat = controller.Session.Seats.For(hound.Instance).Value.Index;
            // Put one real body under the inspector with a legal deployment transaction.
            // The normal sized camera must not make this regression depend on default spawn projection.
            var beast = controller.Shop.Bench.Single(piece => piece.Id == "CARD_B22");
            Assert.That(controller.Session.TryDeploy(beast.Instance, FreeCell(beast.Instance, underneathDetails: true)).Success, Is.True);
            yield return null;
            Assert.That(controller.FormationPieceRect(beast.Instance).Overlaps(FormationBattleController.FormationTraitDetailsRect), Is.True,
                "The input regression must cover an actual projected body, not an assumed screen lineup slot.");
            Assert.That(controller.InspectFormationTrait("迅攻"), Is.True);
            Assert.That(controller.SelectedFormationTraitId, Is.EqualTo("迅攻"));
            Assert.That(controller.FormationTraitDetailsOpen, Is.True);
            var initial = controller.SelectedFormationTrait;
            Assert.That(initial.Count, Is.EqualTo(2));
            Assert.That(initial.ActiveLevel, Is.EqualTo(2)); Assert.That(initial.NextLevel, Is.EqualTo(4));
            Assert.That(initial.MissingForNext, Is.EqualTo(2)); Assert.That(initial.Active, Is.True);
            Assert.That(initial.Members.Single(member => member.CardId == "CARD_M03").State, Is.EqualTo(FormationTraitMemberState.Lineup));
            Assert.That(initial.Members.Single(member => member.CardId == "CARD_M35").State, Is.EqualTo(FormationTraitMemberState.Missing));
            Assert.That(initial.BadgeMembers, Is.Empty);
            Assert.That(initial.Rule.Effect, Does.Contain("攻速+15/30/50%"));

            var positions = controller.Model.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
            var point = FormationBattleController.FormationTraitDetailsRect.center;
            controller.ProcessPointer(EventType.MouseDown, point);
            controller.ProcessPointer(EventType.MouseDrag, point + Vector2.right * 18);
            controller.ProcessPointer(EventType.MouseUp, point + Vector2.right * 18);
            Assert.That(controller.DeploymentPreview, Is.Null, "Trait inspection must consume covered map input instead of starting a deployment.");
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == positions[unit.Instance]), Is.True);
            Assert.That(controller.Model.Clock, Is.Zero);

            int selectedBefore = controller.SelectedSlot;
            var obscured = controller.Model.Units.First(unit => unit.Slot != selectedBefore &&
                controller.FormationPieceRect(unit.Instance).Overlaps(FormationBattleController.FormationTraitDetailsRect));
            var actorRect = controller.FormationPieceRect(obscured.Instance);
            var detailsRect = FormationBattleController.FormationTraitDetailsRect;
            var overlap = Rect.MinMaxRect(Mathf.Max(actorRect.xMin, detailsRect.xMin), Mathf.Max(actorRect.yMin, detailsRect.yMin),
                Mathf.Min(actorRect.xMax, detailsRect.xMax), Mathf.Min(actorRect.yMax, detailsRect.yMax));
            var coveredOrigin = overlap.center;
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(coveredOrigin), Is.True);
            var outsideDetails = new Vector2(FormationBattleController.BoardInputRect.xMax - 12, FormationBattleController.BoardInputRect.center.y);
            Assert.That(FormationBattleController.BoardInputRect.Contains(outsideDetails), Is.True);
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(outsideDetails), Is.False);
            Vector2 legalGroundTarget = default; bool foundTarget = false;
            for (int column = FormationBattleModel.DeploymentColumns - 1; column >= 0 && !foundTarget; column--)
                for (int row = 0; row < FormationBattleModel.Height; row++)
                {
                    var cell = new Vector2Int(column, row);
                    var candidate = FormationBattleController.CellScreen(cell);
                    var preview = controller.Session.PreviewDeploy(obscured.Instance, cell);
                    if (!FormationBattleController.BoardInputRect.Contains(candidate) || !preview.Valid ||
                        detailsRect.Contains(candidate) || controller.Model.Units.Any(unit => controller.FormationPieceRect(unit.Instance).Contains(candidate)) ||
                        preview.SwapInstance != 0 || preview.Cells == obscured.DeploymentCells) continue;
                    legalGroundTarget = candidate; foundTarget = true; break;
                }
            Assert.That(foundTarget, Is.True, "The covered-actor regression needs an empty legal destination outside the trait panel.");
            controller.ProcessPointer(EventType.MouseDown, coveredOrigin);
            Assert.That(controller.SelectedSlot, Is.EqualTo(selectedBefore), "Clicking a board body through the trait panel must not select its hidden character.");
            controller.ProcessPointer(EventType.MouseDrag, outsideDetails);
            Assert.That(controller.DeploymentPreview, Is.Null, "A drag that starts over a covered board body cannot escape the panel and create a deployment preview.");
            controller.ProcessPointer(EventType.MouseDrag, legalGroundTarget);
            Assert.That(controller.DeploymentPreview, Is.Null);
            controller.ProcessPointer(EventType.MouseUp, legalGroundTarget);
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == positions[unit.Instance]), Is.True);
            Assert.That(controller.SelectedSlot, Is.EqualTo(selectedBefore));

            controller.ProcessPointer(EventType.MouseDown, legalGroundTarget);
            controller.ProcessPointer(EventType.MouseUp, legalGroundTarget);
            Assert.That(controller.FormationTraitDetailsOpen, Is.False, "Clicking empty board space dismisses the trait panel.");
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == positions[unit.Instance]), Is.True,
                "Dismissing the panel must not move the selected piece to the clicked tile.");
            Assert.That(controller.InspectFormationTrait("迅攻"), Is.True);

            Assert.That(controller.MovePiece(hound.Instance, false, controller.Session.Seats.FirstEmpty(false, 10)).Success, Is.True);
            yield return null;
            var reserve = controller.SelectedFormationTrait;
            Assert.That(reserve.Count, Is.EqualTo(1)); Assert.That(reserve.Active, Is.False);
            Assert.That(reserve.ActiveLevel, Is.Zero); Assert.That(reserve.NextLevel, Is.EqualTo(2));
            Assert.That(reserve.MissingForNext, Is.EqualTo(1));
            Assert.That(reserve.Members.Single(member => member.CardId == "CARD_M03").State, Is.EqualTo(FormationTraitMemberState.Reserve));

            var reserveSeat = controller.Session.Seats.For(hound.Instance).Value;
            var reserveOrigin = controller.FormationPieceRect(hound.Instance).center;
            var coveredDestination = FormationBattleController.CellScreen(FreeCell(hound.Instance, underneathDetails: true));
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(reserveOrigin), Is.False);
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(coveredDestination), Is.True);
            var reserveRoster = controller.Model.Units.Select(unit => unit.Instance).ToArray();
            var reservePositions = controller.Model.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
            controller.ProcessPointer(EventType.MouseDown, reserveOrigin);
            controller.ProcessPointer(EventType.MouseDrag, coveredDestination);
            controller.ProcessPointer(EventType.MouseUp, coveredDestination);
            Assert.That(controller.Session.Seats.For(hound.Instance).Value.Lineup, Is.False, "Dropping a reserve onto board terrain covered by the trait panel must not deploy it.");
            Assert.That(controller.Session.Seats.For(hound.Instance).Value.Index, Is.EqualTo(reserveSeat.Index));
            CollectionAssert.AreEqual(reserveRoster, controller.Model.Units.Select(unit => unit.Instance));
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == reservePositions[unit.Instance]), Is.True);
            Assert.That(controller.DeploymentPreview, Is.Null);
            Assert.That(controller.SelectedFormationTrait.Count, Is.EqualTo(1));

            // This explicit badge fixture uses real inventory/equip transactions. It verifies
            // presentation of a legal transfer, not a natural gemstone/reward acquisition rate.
            var badge = controller.Campaign.AddItem("徽章:迅攻");
            Assert.That(controller.Campaign.Equip(badge.Instance, host.Instance).Success, Is.True);
            yield return null;
            var transferred = controller.SelectedFormationTrait;
            Assert.That(transferred.Count, Is.EqualTo(2)); Assert.That(transferred.ActiveLevel, Is.EqualTo(2));
            var extra = transferred.BadgeMembers.Single();
            Assert.That(extra.CardId, Is.EqualTo("CARD_M21")); Assert.That(extra.FromBadge, Is.True);
            Assert.That(extra.State, Is.EqualTo(FormationTraitMemberState.Lineup));
            Assert.That(transferred.Members.Any(member => member.CardId == "CARD_M21"), Is.False, "A transferred member must be visibly separate from the natural roster.");
            yield return Capture("playmode-currency-trait-members-badge-and-reserve");

            Assert.That(controller.Campaign.Unequip(badge.Instance).Success, Is.True);
            Assert.That(controller.SelectedFormationTrait.BadgeMembers, Is.Empty);
            Assert.That(controller.SelectedFormationTrait.Active, Is.False);
            Assert.That(controller.MovePiece(hound.Instance, true, houndSeat).Success, Is.True);
            yield return null;
            Assert.That(controller.SelectedFormationTrait.Count, Is.EqualTo(2));
            Assert.That(controller.SelectedFormationTrait.Members.Single(member => member.CardId == "CARD_M03").State, Is.EqualTo(FormationTraitMemberState.Lineup));
            Assert.That(controller.CloseFormationTraitDetails(), Is.True);
            Assert.That(controller.FormationTraitDetailsOpen, Is.False);
            Assert.That(controller.SelectedFormationTraitId, Is.Null);
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(frame, Is.Not.Null);
            try
            {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/formation-combat-view/playmode"));
                Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), frame.EncodeToPNG());
            }
            finally { Object.Destroy(frame); }
        }
    }
}
