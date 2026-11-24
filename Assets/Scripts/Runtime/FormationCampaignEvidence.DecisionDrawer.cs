using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignEvidence
    {
        const string DecisionDrawerEvidenceFlag = "-formationDecisionDrawerEvidence";
        static bool DecisionDrawerRequested => Array.IndexOf(Environment.GetCommandLineArgs(), DecisionDrawerEvidenceFlag) >= 0;
        bool decisionUsesForeground;

        IEnumerator VerifyDecisionDrawer()
        {
            result.scope = "Formal Windows Player. Uses fixed shop offers and legal grants for Tallulah, existing synergy presets for combat preparation, and direct reward-ball payouts to populate deferred choices. Screenshots and clips run at normal UI speed.";
            yield return WaitForOpeningDelivery();
            yield return ResolveChoices();

            var shop = controller.Shop;
            var talulah = shop.TalulahId;
            shop.Preset(new[] { new ShopTrialModel.Piece { Id = talulah, Star = 1 },
                new ShopTrialModel.Piece { Id = talulah, Star = 1 } }, new[] { talulah, talulah, talulah }, 3, 0, 30);
            yield return Market(true);
            Assert(controller.Session.Purchase(0).Success, "Recruiting a third copy opens the real three-cost evolution");
            yield return CheckTalulahDrawer(3);
            decisionUsesForeground = StrategyUiWindow() != IntPtr.Zero;
            result.scope += decisionUsesForeground ? " Uses live Windows mouse and keyboard input." :
                " The Player could not take foreground. Calls the production drawer and camera pointer handlers directly; this does not establish operating-system hit-testing or keyboard event routing.";
            yield return ConfirmDrawerEvidence();
            yield return EscapeDrawerEvidence();
            yield return UseDrawerEvidenceInput(FormationBattleController.FormationStartRect.center, controller.StartBattle);
            Assert(shop.PendingTalulahChoice == 3 && shop.Gold == 27 &&
                controller.Model.Phase == FormationPhase.Preparation && !controller.Session.CanShop &&
                controller.FormationDecisionSelectedIndex < 0,
                "Confirm without a choice, dismiss and attempt to start cannot bypass a mandatory choice");

            yield return PickTalulahRoute(0, "02-three-cost-upgrade-selected");
            Assert(shop.Definition(talulah).Cost == 4 && shop.Capacity(talulah) == 10 && shop.Gold == 27 &&
                shop.Bench.Single(piece => piece.Id == talulah).Star == 1 && shop.Offers[1] == talulah &&
                shop.PreviewPurchase(1).Value == 4 && !shop.TalulahLocked,
                "Three-cost upgrade uses no extra gold and reprices the same shop offer to four with stock ten");
            shop.Validate();
            yield return CaptureInterfaceClip("02-three-cost-retract", 12);
            yield return WaitForStrategyUi(() => !controller.FormationDecisionVisible, "Confirming the first choice returns to the board");
            yield return new WaitForSecondsRealtime(.25f);
            yield return Frame("02-return-to-board");

            Assert(controller.Session.Purchase(1).Success && controller.Session.Purchase(2).Success,
                "Both repriced copies can be recruited through the normal transaction");
            yield return CheckTalulahDrawer(4);
            yield return PickTalulahRoute(0, "03-four-cost-upgrade-selected", true);
            Assert(shop.Definition(talulah).Cost == 5 && shop.Capacity(talulah) == 9 && shop.Gold == 19 &&
                shop.Bench.Single(piece => piece.Id == talulah).Star == 1 && !shop.TalulahLocked,
                "Four-cost upgrade returns the same unit to one star at five cost with stock nine");
            Assert(shop.GrantAtLeast(talulah, 2).Success && shop.PendingTalulahChoice == 0,
                "Reaching two stars at five cost never queues an evolution");
            shop.Validate();

            for (int fee = 3; fee <= 4; fee++)
            {
                var previous = controller.Campaign;
                controller.ResetDemo();
                Assert(!ReferenceEquals(previous, controller.Campaign), "Restart creates a new campaign for the lock fixture");
                yield return WaitForStrategyUi(() => controller.FormationDecisionSelectedIndex < 0,
                    "Restart clears previous selection before the " + fee + "-cost lock fixture");
                yield return WaitForOpeningDelivery();
                yield return ResolveChoices();
                shop = controller.Shop;
                talulah = shop.TalulahId;
                if (fee == 4)
                {
                    Assert(shop.GrantAtLeast(talulah, 2).Success && controller.ChooseCampaignOption(0).Success,
                        "Advance to four cost through the production commit to set up the alternate branch");
                }
                Assert(shop.GrantAtLeast(talulah, 2).Success, "A normal grant can also trigger the " + fee + "-cost choice");
                yield return CheckTalulahDrawer(fee, $"0{fee + 1}-{fee}-cost-lock");
                yield return PickTalulahRoute(1, $"0{fee + 1}-{fee}-cost-lock-selected");
                var locked = shop.Bench.Single(piece => piece.Id == talulah);
                Assert(shop.Definition(talulah).Cost == fee && locked.Star == 2 && shop.TalulahLocked && shop.TalulahEnhanced,
                    $"The {fee}-cost talent is permanent and keeps two stars");
                Assert(controller.Session.Sell(locked.Instance).Success && shop.GrantAtLeast(talulah, 2).Success &&
                    shop.PendingTalulahChoice == 0 && shop.Definition(talulah).Cost == fee && shop.TalulahEnhanced,
                    $"Selling and obtaining another two-star does not reset the {fee}-cost talent");
                shop.Validate();
            }

            yield return VerifyCampaignRewardDecisions();
            yield return VerifySarkazDecision();
            yield return VerifyResonanceDecision();
            yield return VerifyHitchAndSeaDecisions();
            yield return VerifyWisadelDecision();
        }

        IEnumerator UseDrawerEvidenceInput(Vector2 point, Action dispatch)
        {
            if (decisionUsesForeground) yield return ClickStrategyUi(point);
            else { dispatch(); yield return null; }
        }

        IEnumerator ConfirmDrawerEvidence(bool keyboard = false)
        {
            if (decisionUsesForeground && keyboard) yield return PressStrategyUiKey(0x0d);
            else yield return UseDrawerEvidenceInput(FormationBattleController.FormationDecisionConfirmRect.center,
                () => controller.ConfirmFormationDecisionCard());
        }

        IEnumerator EscapeDrawerEvidence()
        {
            if (decisionUsesForeground) yield return PressStrategyUiKey(0x1b);
            else { controller.TryDismissFormationDecision(); yield return null; }
        }

        IEnumerator ActivateDrawerEvidenceCard(int index, bool keyboard = false)
        {
            if (decisionUsesForeground && keyboard) yield return PressStrategyUiKey((byte)(0x31 + index));
            else yield return UseDrawerEvidenceInput(controller.FormationDecisionOptionRect(index).center,
                () => controller.ActivateFormationDecisionCard(index));
        }

        IEnumerator VerifyCampaignRewardDecisions()
        {
            int seed = Enumerable.Range(controller.seed, 100).First(value =>
                CompleteSeededOpeningDelivery(value).Choices.Any(option => option.Id == "P13"));
            controller.seed = seed - 1;
            controller.ResetDemo();
            yield return WaitForOpeningDelivery();
            var run = controller.Campaign;
            int preplan = Array.FindIndex(run.Choices.ToArray(), option => option.Id == "P13");
            Assert(controller.ChooseCampaignOption(preplan).Success && run.HasBlockingChoice,
                "Selecting External Recruitment creates its mandatory follow-up badge choice");
            string badge = run.Choices[0].Id;
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey?.StartsWith("required:0:", StringComparison.Ordinal) == true,
                "A non-Tallulah mandatory choice also opens the top drawer");
            yield return new WaitForSecondsRealtime(.45f);
            yield return Frame("06-required-badge");
            yield return SelectDecisionCard(0);
            yield return ConfirmDrawerEvidence();
            yield return WaitForStrategyUi(() => !run.HasBlockingChoice && run.Inventory.Any(item => item.Key == badge),
                "The mandatory badge confirmation grants the highlighted badge");

            for (int attempt = 0; attempt < 30 && run.PendingRewardChoices < 2; attempt++)
            {
                run.OpenBall("彩");
                int ball = run.PendingBalls.Last().Id;
                if (run.BeginBall(ball) == null || run.CompleteBall(ball) == null)
                    throw new InvalidOperationException("Could not resolve the fixture reward ball.");
            }
            Assert(run.PendingRewardChoices == 2 && !run.HasBlockingChoice,
                "Legitimate reward payouts queue two optional choices without blocking preparation");
            Vector2 rewardEntry = new Vector2(1151, 99);
            yield return new WaitForSecondsRealtime(.25f);
            yield return UseDrawerEvidenceInput(rewardEntry, () => controller.OpenGroundRewardChoices());
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey?.StartsWith("reward:", StringComparison.Ordinal) == true,
                "The reward HUD opens deferred choices in the top drawer");
            yield return new WaitForSecondsRealtime(.45f);
            yield return EscapeDrawerEvidence();
            yield return WaitForStrategyUi(() => !controller.FormationDecisionVisible && run.PendingRewardChoices == 2,
                "Leaving the optional drawer does not consume either choice");
            yield return new WaitForSecondsRealtime(.25f);
            yield return UseDrawerEvidenceInput(rewardEntry, () => controller.OpenGroundRewardChoices());
            yield return WaitForStrategyUi(() => controller.FormationDecisionVisible, "Deferred choices can be reopened");
            yield return new WaitForSecondsRealtime(.45f);
            yield return Frame("07-deferred-rewards");
            yield return SelectDecisionCard(0);
            yield return ConfirmDrawerEvidence();
            yield return WaitForStrategyUi(() => controller.FormationDecisionVisible && run.PendingRewardChoices == 1 &&
                controller.FormationDecisionSelectedIndex < 0, "The next deferred reward appears without carrying over a selection");
            yield return SelectDecisionCard(0);
            yield return ConfirmDrawerEvidence();
            yield return WaitForStrategyUi(() => run.PendingRewardChoices == 0 && !controller.FormationDecisionVisible,
                "Confirming the final deferred choice returns to preparation");
        }

        IEnumerator CheckTalulahDrawer(int fee, string screenshot = null)
        {
            Assert(controller.Shop.PendingTalulahChoice == fee &&
                controller.Campaign.Choices.Select(choice => choice.Id).SequenceEqual(new[] { "tal-up", "tal-enhance" }),
                $"The campaign has exactly the two {fee}-cost evolution choices");
            yield return WaitForStrategyUi(() => controller.FormationDecisionVisible, $"The {fee}-cost drawer opens automatically");
            yield return CaptureInterfaceClip($"{screenshot ?? $"{fee}-cost"}-expand", 14);
            yield return new WaitForSecondsRealtime(.5f);
            Assert(controller.FormationDecisionSelectedIndex < 0, $"The {fee}-cost drawer starts unselected");
            yield return Frame(screenshot ?? $"0{fee - 2}-{fee}-cost-unselected");
        }

        IEnumerator PickTalulahRoute(int index, string screenshot, bool keyboard = false)
        {
            int fee = controller.Shop.PendingTalulahChoice;
            yield return ActivateDrawerEvidenceCard(index, keyboard);
            yield return WaitForStrategyUi(() => controller.FormationDecisionSelectedIndex == index,
                "Selecting a visible alternative only highlights that card");
            Assert(controller.Shop.PendingTalulahChoice == fee && controller.Campaign.TalulahCost == fee,
                "Preselection does not mutate the underlying role");
            yield return Frame(screenshot);
            yield return ConfirmDrawerEvidence(keyboard);
            yield return WaitForStrategyUi(() => controller.Shop.PendingTalulahChoice == 0,
                "Confirm resolves the highlighted option through the real controller transaction");
        }

        static readonly Vector2 TacticalDecisionAnchor = new Vector2(1125, 38);

        IEnumerator OpenTacticalDecision(int index, string choiceKey)
        {
            yield return ClickTacticalMenuOption(index);
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey?.StartsWith(choiceKey, StringComparison.Ordinal) == true,
                "The menu opens " + choiceKey + " in the same drawer");
            yield return new WaitForSecondsRealtime(.45f);
        }

        IEnumerator ClickTacticalMenuOption(int index)
        {
            if (controller.FormationDecisionKey != "tactical-menu")
                yield return UseDrawerEvidenceInput(TacticalDecisionAnchor, controller.OpenFormationTacticalDecisions);
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey == "tactical-menu",
                "The top 搭档 button opens the common drawer's navigation");
            yield return new WaitForSecondsRealtime(.45f);
            for (int attempt = 0; decisionUsesForeground && attempt < 3 && controller.FormationDecisionOptionRect(index).height < 22; attempt++)
            {
                yield return MoveStrategyUiPointer(new Vector2(650, 250));
                mouse_event(0x0800, 0, 0, unchecked((uint)-480), UIntPtr.Zero);
                yield return new WaitForSecondsRealtime(.15f);
            }
            var card = controller.FormationDecisionOptionRect(index);
            Assert(card.height >= 22, "Menu scrolling exposes tactic " + index);
            yield return ActivateDrawerEvidenceCard(index);
        }

        IEnumerator SelectDecisionCard(int index)
        {
            var card = controller.FormationDecisionOptionRect(index);
            Assert(card.height > 0, "The option card has a rendered hit target");
            yield return ActivateDrawerEvidenceCard(index);
            yield return WaitForStrategyUi(() => controller.FormationDecisionSelectedIndex == index,
                "Clicking a card preselects that option");
        }

        IEnumerator ConfirmTacticalDecision(Func<bool> applied, string description)
        {
            yield return ConfirmDrawerEvidence();
            yield return WaitForStrategyUi(applied, description);
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey == "tactical-menu",
                "Confirming a tactical choice returns to the shared navigation");
        }

        IEnumerator VerifySarkazDecision()
        {
            controller.SelectSynergyTrial("萨卡兹");
            yield return WaitForScoutingView(false);
            var run = controller.Campaign;
            Assert(run.TraitCount("萨卡兹") == 6, "The real Sarkaz preset fields six members");
            int initialPact = run.SarkazPact;
            var member = controller.Session.Seats.Lineup(controller.Shop.Bench).Last();
            int seat = controller.Session.Seats.For(member.Instance).Value.Index;
            int reserve = controller.Session.Seats.FirstEmpty(false, ShopSeatLayout.ReserveCapacity);
            Assert(controller.Session.Move(member.Instance, false, reserve).Success && run.TraitCount("萨卡兹") == 5,
                "Benching a preset member lowers the faction to five before choosing a pact");

            yield return OpenTacticalDecision(8, "tactical:sarkaz:5");
            yield return Frame("08-sarkaz-double-locked");
            yield return ActivateDrawerEvidenceCard(2);
            Assert(controller.FormationDecisionSelectedIndex < 0 && run.SarkazPact == initialPact,
                "Dual pact cannot be selected with five members");
            yield return SelectDecisionCard(1);
            Assert(run.SarkazPact == initialPact, "Previewing a pact preserves the active pact");
            yield return ConfirmTacticalDecision(() => run.SarkazPact == 1 && !run.SarkazBloodPact,
                "Confirming Heavy Pact changes the actual campaign setting");
            yield return EscapeDrawerEvidence();
            yield return WaitForStrategyUi(() => !controller.FormationDecisionVisible, "Escape returns from optional tactical navigation");
            yield return new WaitForSecondsRealtime(.25f);

            Assert(controller.Session.Move(member.Instance, true, seat).Success && run.TraitCount("萨卡兹") == 6,
                "Restoring the sixth deployed member unlocks Dual Pact");
            yield return OpenTacticalDecision(8, "tactical:sarkaz:6");
            yield return SelectDecisionCard(2);
            yield return Frame("09-sarkaz-dual-selected");
            yield return ConfirmTacticalDecision(() => run.SarkazPact == 2 && run.SarkazBloodPact,
                "Confirming Dual Pact applies the six-member option");
        }

        IEnumerator VerifyResonanceDecision()
        {
            controller.SelectSynergyTrial("同频");
            yield return WaitForScoutingView(false);
            var run = controller.Campaign;
            var members = controller.Session.Seats.Lineup(controller.Shop.Bench).Where(piece => run.HasTrait(piece, "同频")).ToArray();
            Assert(members.Length >= 2 && run.ResonanceLeaderInstance == 0 && run.ResonancePartnerInstance == 0,
                "The resonance trial begins with automatic leader and partner");
            yield return OpenTacticalDecision(2, "tactical:resonance-lead:");
            yield return SelectDecisionCard(1);
            yield return Frame("10-resonance-leader-selected");
            yield return ConfirmTacticalDecision(() => run.ResonanceLeaderInstance == members[0].Instance,
                "The confirmed resonance leader is stored by deployed role instance");
            yield return OpenTacticalDecision(3, "tactical:resonance-partner:");
            yield return SelectDecisionCard(1);
            yield return ConfirmTacticalDecision(() => run.ResonancePartnerInstance == members[1].Instance,
                "Partner options omit the assigned leader and commit the distinct member");
        }

        IEnumerator VerifyWisadelDecision()
        {
            controller.SelectSynergyTrial("连击");
            yield return WaitForScoutingView(false);
            var run = controller.Campaign;
            var wisadel = controller.Session.Seats.Lineup(controller.Shop.Bench)
                .Single(piece => piece.Id == FormationCampaignRules.Find("WIS").CardId);
            yield return OpenTacticalDecision(9, "tactical:wisadel");
            yield return SelectDecisionCard(1);
            Assert(run.WisadelSkill == 3, "Previewing Wisadel S2 leaves the default S3 intact");
            yield return Frame("13-wisadel-skill-selected");
            yield return ConfirmTacticalDecision(() => run.WisadelSkill == 2, "Confirming Wisadel S2 changes the actual skill");
            yield return EscapeDrawerEvidence();
            yield return WaitForStrategyUi(() => !controller.FormationDecisionVisible, "Close the optional drawer before combat");
            yield return new WaitForSecondsRealtime(.25f); // Allow the drawer to finish retracting before start.
            yield return ReadyArt();
            controller.StartBattle();
            Assert(controller.Model.Phase == FormationPhase.Fighting &&
                Mathf.Approximately(controller.Model.Units.Single(unit => unit.Instance == wisadel.Instance).SkillRequired, 24),
                "The real combat actor enters with the selected S2's 24-SP threshold");
            yield return UseDrawerEvidenceInput(TacticalDecisionAnchor, controller.OpenFormationTacticalDecisions);
            Assert(!controller.FormationDecisionVisible, "Combat cannot reopen the preparation drawer");
        }

        IEnumerator VerifyHitchAndSeaDecisions()
        {
            controller.SelectSynergyTrial("便车");
            yield return WaitForScoutingView(false);
            var run = controller.Campaign;
            var lineup = controller.Session.Seats.Lineup(controller.Shop.Bench).ToArray();
            var passenger = lineup.First(piece => run.HasTrait(piece, "便车") && FormationCampaignRules.Find(piece.Id)?.Size != "重");
            var carrier = lineup.First(piece => FormationCampaignRules.Find(piece.Id)?.Size == "重");
            yield return OpenTacticalDecision(10, "tactical:hitch:");
            yield return ActivateDrawerEvidenceCard(0);
            yield return WaitForStrategyUi(() => controller.FormationDecisionKey?.StartsWith("tactical:hitch-slot:0:", StringComparison.Ordinal) == true,
                "Selecting the first ride group opens the actual passenger and carrier pairs");
            yield return new WaitForSecondsRealtime(.45f);
            yield return SelectDecisionCard(1);
            Assert(run.RidePairs.Count == 0, "Previewing a ride pair leaves auto assignment intact");
            yield return Frame("11-hitch-pair-selected");
            yield return ConfirmDrawerEvidence();
            yield return WaitForStrategyUi(() => run.RidePairs.TryGetValue(passenger.Instance, out var id) && id == carrier.Instance,
                "Confirming a ride pair stores both live deployment instances");

            controller.SelectSynergyTrial("海嗣");
            yield return WaitForScoutingView(false);
            run = controller.Campaign;
            yield return ClickTacticalMenuOption(11);
            yield return WaitForStrategyUi(() => !controller.FormationDecisionVisible && controller.ViewingDefenders,
                "Sea placement exits the drawer and pans to the playable guard tiles");
            yield return WaitForScoutingView(true);
            yield return new WaitForSecondsRealtime(.25f);
            var cell = new Vector2Int(FormationBattleModel.DeploymentColumns + 4, FormationBattleModel.Height / 2);
            var point = FormationBattleController.CellScreen(cell);
            Assert(FormationBattleController.BoardInputRect.Contains(point), "The chosen guard tile projects into the real board hit area");
            yield return UseDrawerEvidenceInput(point, () => controller.ProcessPointer(EventType.MouseUp, point));
            yield return WaitForStrategyUi(() => run.SeaCellIds.SequenceEqual(new[] { cell.x + cell.y * FormationBattleModel.Width }),
                "The Sea editor pointer handler records the projected legal tile");
            yield return Frame("12-sea-manual-tile");
        }
    }
}
