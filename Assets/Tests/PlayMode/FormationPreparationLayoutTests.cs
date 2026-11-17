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

        void AssertBoardBelowPinnedShop(bool defenders)
        {
            float first = defenders ? FormationBattleModel.DeploymentColumns + 1 : 0;
            float last = defenders ? FormationBattleModel.Width - 1 : FormationBattleModel.DeploymentColumns;
            for (float column = first; column <= last; column++)
                for (int row = 0; row < FormationBattleModel.Height; row++)
                    foreach (float dx in new[] { -.5f, .5f })
                        foreach (float dy in new[] { -.5f, .5f })
                        {
                            var point = Project(FormationBattleController.WorldPoint(new Vector2(column + dx, row + dy)));
                            Assert.That(point.y, Is.GreaterThan(FormationBattleController.FormationMarketRect.yMax + 3), "Every cell corner must be visibly below the pinned recruitment panel.");
                            Assert.That(FormationBattleController.BoardInputRect.Contains(point), Is.True, "The complete focused board must remain in the usable map area.");
                        }
            var actors = defenders ? controller.Model.Defenders.Cast<FormationActor>() : controller.Model.Units.Cast<FormationActor>();
            foreach (var actor in actors)
            {
                var head = Project(controller.ActorWorldPoint(actor) + controller.battleCamera.transform.up * controller.ActorPresentationHeight(actor));
                Assert.That(head.y, Is.GreaterThan(FormationBattleController.FormationMarketRect.yMax), actor.Name + " must not be hidden behind recruitment cards.");
                Assert.That(FormationBattleController.BoardInputRect.Contains(head), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator PinnedRecruitmentLeavesWholeCompactBoardUsableForRealPurchaseAndDeployment()
        {
            yield return OpenFinitePreparation();
            Assert.That(FormationBattleModel.Width * FormationBattleModel.Height, Is.EqualTo(154));
            controller.SetMarketVisible(false);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(controller.MarketVisible, Is.True, "Attempting to close recruitment during preparation must keep it open.");
            Assert.That(controller.ShopPanelVisibility, Is.EqualTo(1).Within(.001f));
            Assert.That(controller.ShopPanelTransitioning, Is.False);
            AssertBoardBelowPinnedShop(false);
            controller.SetFormationView(true); yield return null;
            AssertBoardBelowPinnedShop(true);
            controller.SetFormationView(false); yield return null;

            var owned = controller.Shop.Bench.Select(piece => piece.Id).ToArray();
            int offer = Enumerable.Range(0, controller.Shop.Offers.Count)
                .Where(index => controller.Shop.Offers[index] != null && !owned.Contains(controller.Shop.Offers[index]) &&
                    controller.Shop.Definition(controller.Shop.Offers[index]).Cost <= controller.Shop.Gold)
                .DefaultIfEmpty(-1).First();
            Assert.That(offer, Is.GreaterThanOrEqualTo(0), "The actual finite opening shop must provide an affordable new kind for this deterministic path.");
            int beforeGold = controller.Shop.Gold;
            int price = controller.Shop.Definition(controller.Shop.Offers[offer]).Cost;
            var bought = controller.BuyOffer(offer);
            Assert.That(bought.Success, Is.True); Assert.That(controller.Shop.Gold, Is.EqualTo(beforeGold - price));
            Assert.That(controller.Session.Seats.InLineup(bought.Instance), Is.False);
            var replaced = controller.Session.Seats.Lineup(controller.Shop.Bench).First();
            Assert.That(controller.MovePiece(replaced.Instance, false, controller.Session.Seats.FirstEmpty(false, 10)).Success, Is.True);
            var origin = controller.FormationPieceRect(bought.Instance).center;
            var destination = new Vector2Int(FormationBattleModel.DeploymentColumns - 2, 2);
            Assert.That(controller.Model.Units.All(unit => !unit.DeploymentCells.Contains(destination)), Is.True);
            var target = FormationBattleController.CellScreen(destination);
            controller.ProcessPointer(EventType.MouseDown, origin);
            controller.ProcessPointer(EventType.MouseDrag, target);
            Assert.That(controller.DeploymentPreview, Is.Not.Null);
            Assert.That(controller.DeploymentPreview.Valid, Is.True);
            controller.ProcessPointer(EventType.MouseUp, target);
            Assert.That(controller.Session.Seats.InLineup(bought.Instance), Is.True);
            Assert.That(controller.Model.Units.Single(unit => unit.Instance == bought.Instance).Spawn, Is.EqualTo((Vector2)destination));
            Assert.That(controller.MarketVisible, Is.True);
            yield return RosterReady();
            AssertBoardBelowPinnedShop(false);
            yield return Capture("playmode-compact-pinned-preparation");

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
            Assert.That(controller.MarketVisible, Is.False, "Combat still removes the preparation-only shop.");
            Assert.That(controller.ShopPanelVisibility, Is.EqualTo(0).Within(.001f));

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
        }

        [UnityTest]
        public IEnumerator TraitDetailsFollowOwnedReserveLineupAndBadgeActivationWithoutPointerLeakage()
        {
            yield return OpenFinitePreparation();
            var hound = controller.Shop.Bench.Single(piece => piece.Id == "CARD_M03");
            var host = controller.Shop.Bench.Single(piece => piece.Id == "CARD_M21");
            int houndSeat = controller.Session.Seats.For(hound.Instance).Value.Index;
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
                FormationBattleController.FormationTraitDetailsRect.Contains(new Vector2(
                    FormationBattleController.FormationLineupRect(controller.Session.Seats.For(unit.Instance).Value.Index).center.x, 530)));
            var obscuredSlot = controller.Session.Seats.For(obscured.Instance).Value.Index;
            var coveredOrigin = new Vector2(FormationBattleController.FormationLineupRect(obscuredSlot).center.x, 530);
            Assert.That(FormationBattleController.FormationLineupRect(obscuredSlot).Contains(coveredOrigin), Is.True);
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
                        preview.SwapInstance != 0 || preview.Cells == obscured.DeploymentCells) continue;
                    legalGroundTarget = candidate; foundTarget = true; break;
                }
            Assert.That(foundTarget, Is.True, "The covered-seat regression needs an actual legal destination underneath the trait panel.");
            controller.ProcessPointer(EventType.MouseDown, coveredOrigin);
            Assert.That(controller.SelectedSlot, Is.EqualTo(selectedBefore), "Clicking a roster seat through the trait panel must not select its hidden character.");
            controller.ProcessPointer(EventType.MouseDrag, outsideDetails);
            Assert.That(controller.DeploymentPreview, Is.Null, "A drag that starts over a covered roster seat cannot escape the panel and create a deployment preview.");
            controller.ProcessPointer(EventType.MouseDrag, legalGroundTarget);
            Assert.That(controller.DeploymentPreview, Is.Null);
            controller.ProcessPointer(EventType.MouseUp, legalGroundTarget);
            Assert.That(controller.Model.Units.All(unit => unit.Spawn == positions[unit.Instance]), Is.True);
            Assert.That(controller.SelectedSlot, Is.EqualTo(selectedBefore));

            Assert.That(controller.MovePiece(hound.Instance, false, controller.Session.Seats.FirstEmpty(false, 10)).Success, Is.True);
            yield return null;
            var reserve = controller.SelectedFormationTrait;
            Assert.That(reserve.Count, Is.EqualTo(1)); Assert.That(reserve.Active, Is.False);
            Assert.That(reserve.ActiveLevel, Is.Zero); Assert.That(reserve.NextLevel, Is.EqualTo(2));
            Assert.That(reserve.MissingForNext, Is.EqualTo(1));
            Assert.That(reserve.Members.Single(member => member.CardId == "CARD_M03").State, Is.EqualTo(FormationTraitMemberState.Reserve));

            var reserveSeat = controller.Session.Seats.For(hound.Instance).Value;
            var reserveOrigin = controller.FormationPieceRect(hound.Instance).center;
            var coveredDestination = new Vector2(FormationBattleController.FormationLineupRect(houndSeat).center.x, 530);
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(reserveOrigin), Is.False);
            Assert.That(FormationBattleController.FormationTraitDetailsRect.Contains(coveredDestination), Is.True);
            var reserveRoster = controller.Model.Units.Select(unit => unit.Instance).ToArray();
            var reservePositions = controller.Model.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
            controller.ProcessPointer(EventType.MouseDown, reserveOrigin);
            controller.ProcessPointer(EventType.MouseDrag, coveredDestination);
            controller.ProcessPointer(EventType.MouseUp, coveredDestination);
            Assert.That(controller.Session.Seats.For(hound.Instance).Value.Lineup, Is.False, "Dropping a reserve onto a roster seat covered by the trait panel must not move it into the lineup.");
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
            yield return Capture("playmode-trait-members-badge-and-reserve");

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
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/formation-layout"));
                Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), frame.EncodeToPNG());
            }
            finally { Object.Destroy(frame); }
        }
    }
}
