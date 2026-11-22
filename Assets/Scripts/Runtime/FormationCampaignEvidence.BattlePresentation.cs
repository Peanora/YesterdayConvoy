using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignEvidence
    {
        const string BattlePresentationFlag = "-formationBattlePresentationEvidence";
        static bool BattlePresentationRequested =>
            Array.IndexOf(Environment.GetCommandLineArgs(), BattlePresentationFlag) >= 0;

        IEnumerator VerifyBattlePresentation()
        {
            result.scope = "Visible formal campaign. Starts 1-1 and 1-3 through the production opening action and continues after the win. Uses physical mouse input when the Player owns the foreground, otherwise invokes the same controller actions. 1-1 is natural speed. 1-2 and most of 1-3 fast-forward the real model; the end of 1-3 and both settlements play in real time.";
            yield return WaitForOpeningDelivery();
            yield return ResolveChoices();
            DeployOpeningRecruits();
            yield return ReadyArt();
            Assert(controller.Campaign.CurrentNode.Id == "1-1", "The battle opens on the first formal node");
            yield return CaptureBattleOpening("01-opening-win");

            double deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (IsBattleRunning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert(controller.Model.Phase == FormationPhase.Won && controller.Model.RemainingDefenders == 0,
                "Unmodified 1-1 combat clears the garrison and reaches the blue gate");
            yield return CaptureBattleSettlement("02-victory", 100);
            Assert(controller.Campaign.SquadHP == 100 && controller.Session.LastReward.Base == 5,
                "The actual reward victory costs no team life and has five base gold");
            if (StrategyUiWindow() != IntPtr.Zero)
                yield return ClickStrategyUi(controller.CampaignSettlementContinueRect.center);
            else
                Assert(controller.ContinueCampaign(), "The continue action succeeds after the revealed receipt");
            Assert(controller.Model.Phase == FormationPhase.Preparation && controller.Campaign.CurrentNode.Id == "1-2",
                "Continuing after the revealed receipt enters the next node's preparation");
            result.battles++;

            yield return ReadyArt();
            controller.StartBattle();
            var middle = controller.Model;
            for (int slice = 0; IsBattleRunning && slice < 250; slice++)
            {
                for (int step = 0; step < 12 && IsBattleRunning; step++) middle.Advance(.25f);
                yield return null;
            }
            Assert(middle.Phase == FormationPhase.Won, "The same real team wins reward node 1-2");
            yield return WaitForStrategyUi(() => controller.Campaign.NodeSettled, "1-2 settles through the campaign controller");
            Assert(controller.ContinueCampaign() && controller.Campaign.CurrentNode.Id == "1-3",
                "The next timed battle is the regular 1-3 defense");
            result.battles++;
            yield return ResolveChoices();

            // Keep a single starter unmodified. A timeout here uses the normal garrison,
            // time limit, damage calculation and settlement rather than a result fixture.
            foreach (var piece in controller.Session.Seats.Lineup(controller.Shop.Bench).Skip(1).ToArray())
            {
                int free = controller.Session.Seats.FirstEmpty(false, ShopSeatLayout.ReserveCapacity);
                Assert(controller.MovePiece(piece.Instance, false, free).Success,
                    "Return one starter to reserve before the loss example");
            }
            yield return ReadyArt();
            Assert(controller.Model.EnabledCount == 1, "Only one starter is deployed for the loss example");
            yield return CaptureBattleOpening("03-opening-loss");

            var timed = controller.Model;
            for (int slice = 0; IsBattleRunning && timed.Clock < timed.LimitSeconds - 1.1 && slice < 120; slice++)
            {
                for (int step = 0; step < 12 && IsBattleRunning && timed.Clock < timed.LimitSeconds - 1.1; step++)
                    timed.Advance(.25f);
                yield return null;
            }
            Assert(IsBattleRunning && timed.Clock < timed.LimitSeconds && timed.RemainingDefenders > 0,
                "Final timeout seconds play live against the actual surviving garrison");
            yield return WaitForStrategyUi(() => timed.Phase == FormationPhase.Lost, "1-3 times out under the real battle update");
            yield return CaptureBattleSettlement("04-defeat", 100);
            int loss = 3 + Mathf.CeilToInt(6f * timed.RemainingDefenders / timed.InitialDefenderCount);
            Assert(controller.Campaign.SquadHP == 100 - loss,
                $"Loss applies the actual {loss} life deduction once with {timed.RemainingDefenders}/{timed.InitialDefenderCount} guards left");
            int gold = controller.Shop.Gold;
            Assert(controller.Session.SettleBattle(false, timed.EncounterProgress,
                    timed.InitialDefenderCount, timed.RemainingDefenders, timed.BossAlive) == null &&
                controller.Shop.Gold == gold && controller.Campaign.SquadHP == 100 - loss,
                "Repeated settlement cannot credit income or deduct life twice");
            result.battles++;
        }

        bool IsBattleRunning => controller.Model.Phase is FormationPhase.Fighting or FormationPhase.Exiting;

        IEnumerator CaptureBattleOpening(string folder)
        {
            controller.SetFormationView(false);
            yield return WaitForScoutingView(false);
            float left = controller.battleCamera.transform.position.x;
            var starts = controller.Model.Units.Where(unit => unit.Enabled)
                .Select(unit => unit.Position).ToArray();
            yield return Frame(folder + "-preparation");
            if (StrategyUiWindow() != IntPtr.Zero)
                yield return ClickStrategyUi(FormationBattleController.FormationStartRect.center);
            else
                Assert(controller.StartFormationBattleOpening(),
                    "The normal start-button action accepts the legal deployment");
            Assert(controller.FormationBattleOpeningActive && controller.Model.Phase == FormationPhase.Fighting,
                "The start action enters the opening with the combat model ready");
            Assert(!controller.StartFormationBattleOpening(), "A repeated start cannot restart the active opening");
            bool movedDuringOpening = false;
            bool combatAdvancedDuringOpening = false;
            bool sawRunningCombat = false;
            yield return CaptureInterfaceClip(folder, 82, BattlePresentationFlag, beforeFrame: _ =>
            {
                if (controller.FormationBattleOpeningActive)
                {
                    float x = controller.battleCamera.transform.position.x;
                    movedDuringOpening |= x > left + .25f && x < left + 8;
                    combatAdvancedDuringOpening |= controller.Model.Clock > .0001 ||
                        !starts.SequenceEqual(controller.Model.Units.Where(unit => unit.Enabled).Select(unit => unit.Position));
                }
                else sawRunningCombat |= controller.Model.Clock > .0001;
            });
            Assert(movedDuringOpening && controller.battleCamera.transform.position.x > left + 5,
                "The camera crosses intermediate board positions before reaching the right pose");
            Assert(!combatAdvancedDuringOpening && sawRunningCombat && !controller.FormationBattleOpeningActive,
                "Units and clock stay at their starting values through the opening, then real combat advances");
        }

        IEnumerator CaptureBattleSettlement(string folder, int previousLife)
        {
            yield return WaitForStrategyUi(() => controller.Campaign.NodeSettled && controller.CampaignSettlementResultVisible,
                "The ordinary battle outcome opens a real campaign settlement");
            bool lifeAlone = false;
            bool ledgerAlone = false;
            bool actions = false;
            bool invalidOrder = false;
            yield return CaptureInterfaceClip(folder, 106, BattlePresentationFlag, beforeFrame: _ =>
            {
                bool life = controller.CampaignSettlementLifeVisible;
                bool ledger = controller.CampaignSettlementLedgerVisible;
                bool canContinue = controller.CampaignSettlementActionsVisible;
                lifeAlone |= life && !ledger && !canContinue;
                ledgerAlone |= life && ledger && !canContinue;
                actions |= canContinue;
                invalidOrder |= (ledger && !life) || (canContinue && !ledger);
            });
            Assert(lifeAlone && ledgerAlone && actions && !invalidOrder,
                "Health has a separate visible beat, then the ledger, then actions");
            var run = controller.Campaign;
            var snapshot = run.LastBattleSettlement;
            var receipt = controller.Session.LastReward;
            Assert(snapshot != null && snapshot.NodeId == run.CurrentNode.Id &&
                snapshot.SquadHPBefore == previousLife && snapshot.SquadHPAfter == run.SquadHP &&
                snapshot.Won == (controller.Model.Phase == FormationPhase.Won),
                "The health animation reads both real team-life values and the real outcome");
            Assert(receipt is { Experience: 2 } && snapshot.Experience >= receipt.Experience &&
                snapshot.BaseGold - snapshot.DebtPaid == receipt.Base &&
                snapshot.Interest == receipt.Interest && snapshot.Streak == receipt.Streak &&
                snapshot.Gold + controller.Model.BattleGold == controller.Shop.Gold - receipt.GoldBefore &&
                controller.CampaignSettlementContinueRect.width > 0,
                "The displayed income categories reconcile with the actual wallet and XP award");
        }
    }
}
