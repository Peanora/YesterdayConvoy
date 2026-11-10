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
    public sealed class CampaignJourneySceneTests
    {
        ManualBattleController controller;float previousScale;
        [SetUp]public void Setup(){previousScale=Time.timeScale;Time.timeScale=0;CampaignJourneySession.Clear();}
        [TearDown]public void Cleanup(){CampaignJourneySession.Clear();Time.timeScale=previousScale;}
        IEnumerator Begin()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);
            controller.BeginJourney(44);yield return CampaignJourneyEvidenceProbe.Ready(1,c=>controller=c);
        }
        IEnumerator Ready(int stage)=>CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);
        SynergyActivationFeedback ActivationFeedback=>(SynergyActivationFeedback)typeof(ManualBattleController)
            .GetField("synergyActivationFeedback",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
        void SetMuted(bool value){if(controller.Muted!=value)controller.ToggleMute();}
        void RebuildPreparation()=>typeof(ManualBattleController).GetMethod("SyncShopDeck",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(controller,new object[]{false,false});

        [UnityTest]public IEnumerator SceneChangesKeepChosenSpeedAndSoundWithoutReplayingUnchangedSynergies()
        {
            yield return Begin();
            Assert.That(controller.Muted,Is.True,"A fresh challenge starts with sound effects disabled.");
            Assert.That(controller.SimulationSpeed,Is.EqualTo(1));
            Assert.That(controller.CombatView.GetComponent<AudioSource>().mute,Is.True);
            // A minimal inventory fixture isolates the cross-scene presentation state;
            // seat edits, supply settlement and authored scene loads use production APIs.
            controller.Shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M07",Star=1},new ShopTrialModel.Piece{Id="CARD_M08",Star=1}},Array.Empty<string>(),2,0,5);
            RebuildPreparation();
            Assert.That(controller.MoveShopCard(controller.Shop.Bench[0].Instance,true,0),Is.True);
            Assert.That(controller.MoveShopCard(controller.Shop.Bench[1].Instance,true,1),Is.True);
            yield return Ready(1);
            Assert.That(controller.Battle.ReunionTier,Is.EqualTo(2));Assert.That(ActivationFeedback.Pending,Is.True);
            for(int i=0;i<80&&ActivationFeedback.Pending;i++)controller.Tick(.1f);
            Assert.That(ActivationFeedback.Pending,Is.False);
            controller.ToggleSpeed();SetMuted(false);
            Assert.That(controller.SimulationSpeed,Is.EqualTo(2));Assert.That(controller.Muted,Is.False);
            var previous=controller;var session=CampaignJourneySession.Current;
            controller.ClaimCampaignSupply();Assert.That(controller.OpenLoopShop(),Is.True);yield return Ready(2);
            Assert.That(controller,Is.Not.SameAs(previous),"Even the same map is loaded through a new scene controller.");
            Assert.That(CampaignJourneySession.Current,Is.SameAs(session));
            Assert.That(controller.SimulationSpeed,Is.EqualTo(2));Assert.That(controller.Muted,Is.False);
            Assert.That(controller.Battle.ReunionTier,Is.EqualTo(2));Assert.That(ActivationFeedback.Pending,Is.False,
                "Unchanged activation tiers must not replay when the next scene reconstructs the battle.");
            int revision=ActivationFeedback.Revision;
            RebuildPreparation();Assert.That(ActivationFeedback.Revision,Is.EqualTo(revision));Assert.That(ActivationFeedback.Pending,Is.False);
            var second=controller.Shop.Bench.Single(p=>p.Id=="CARD_M08");
            Assert.That(controller.MoveShopCard(second.Instance,false,3),Is.True);Assert.That(controller.Battle.ReunionTier,Is.Zero);
            Assert.That(controller.MoveShopCard(second.Instance,true,1),Is.True);Assert.That(controller.Battle.ReunionTier,Is.EqualTo(2));
            Assert.That(ActivationFeedback.Pending,Is.True,"A real loss and reactivation must still create fresh feedback.");
            Assert.That(controller.StartLoopNextBattle(),Is.True);controller.Tick(.01f);
            Assert.That(controller.RestartCampaignEncounter(),Is.True);yield return Ready(2);
            Assert.That(controller.SimulationSpeed,Is.EqualTo(2));Assert.That(controller.Muted,Is.False);Assert.That(ActivationFeedback.Pending,Is.False);
            controller.BeginJourney(99);yield return Ready(1);
            Assert.That(CampaignJourneySession.Current,Is.Not.SameAs(session));Assert.That(controller.Muted,Is.True);
            Assert.That(controller.SimulationSpeed,Is.EqualTo(1),"A fresh challenge resets the combat speed independently of cross-stage restoration.");
            controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Assert.That(controller.Muted,Is.True,"Leaving the challenge creates a fresh quiet selector session.");
        }

        IEnumerator NaturalEmptyBattle()
        {
            if(controller.Battle.Phase==BattlePhase.Ready)controller.StartBattle();
            Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Running));
            int maximumSteps=(int)Math.Ceiling(controller.Battle.WavePlan.TotalDuration*4)+16;
            for(int step=0;step<maximumSteps&&controller.Battle.Phase==BattlePhase.Running;step++)
            {
                controller.Tick(.25f);
                if(step%40==0)yield return null;
            }
            // With no deployed units the natural loss occurs when the final
            // deployment window closes; it need not wait for the full deadline.
            Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Lost));
            Assert.That(controller.Battle.FinalAdvanceActive,Is.True);
            Assert.That(controller.Battle.Units,Is.Empty);
        }

        [UnityTest]public IEnumerator NaturalEmptyDefeatsConsumeHealthOnceAndContinueAcrossScenesUntilZero()
        {
            yield return Begin();yield return CampaignJourneyEvidenceProbe.FirstCombat(c=>controller=c);
            var session=CampaignJourneySession.Current;var shop=controller.Shop;var loop=controller.Loop;
            Assert.That(loop.Health,Is.EqualTo(100));Assert.That(loop.MaxHealth,Is.EqualTo(100));
            int[] damageLimits={20,20,0,30,25,30};
            int expectedHealth=100;
            for(int stage=2;stage<=7;stage++)
            {
                yield return Ready(stage);
                Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));
                Assert.That(CampaignJourneySession.Current,Is.SameAs(session));
                Assert.That(loop.Health,Is.EqualTo(expectedHealth),"Scene changes must retain challenge health.");
                Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(stage);
                if(stage==4)
                {
                    controller.ClaimCampaignSupply();controller.ClaimCampaignSupply();
                    Assert.That(loop.LastHealthLoss,Is.Zero);Assert.That(loop.Health,Is.EqualTo(expectedHealth));
                    Assert.That(loop.LastBattleWon,Is.True);Assert.That(loop.Phase,Is.EqualTo(BattleShopPhase.Settlement));
                }
                else
                {
                    if(stage==3)
                    {
                        controller.Battle.SetPaused(true);controller.Tick(30);
                        Assert.That(loop.Health,Is.EqualTo(80));Assert.That(controller.Battle.Clock,Is.Zero);
                        Assert.That(controller.RestartCampaignEncounter(),Is.True);yield return Ready(stage);
                        Assert.That(loop.Health,Is.EqualTo(80),"Restarting combat cannot restore challenge health.");
                        Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Ready));
                    }
                    int gold=shop.Gold,xp=shop.XP,level=shop.Level;
                    var definition=CampaignJourneyCatalog.Stage(stage);
                    yield return NaturalEmptyBattle();
                    int loss=Math.Min(expectedHealth,damageLimits[stage-2]);
                    expectedHealth-=loss;
                    Assert.That(loop.Health,Is.EqualTo(expectedHealth));Assert.That(loop.LastHealthLoss,Is.EqualTo(loss));
                    Assert.That(loop.LastBattleWon,Is.False);Assert.That(loop.LastInitialDefense,Is.EqualTo(definition.defense));
                    Assert.That(loop.LastRemainingDefense,Is.EqualTo(definition.defense));
                    Assert.That(loop.Reward.Won,Is.False);Assert.That(loop.Reward.Base,Is.EqualTo(definition.baseGold));
                    Assert.That(loop.Reward.Interest,Is.EqualTo(2*Math.Min(5,gold/10)));Assert.That(loop.Reward.Streak,Is.Zero);
                    Assert.That(loop.Reward.Experience,Is.EqualTo(4));
                    int beforeXP=Enumerable.Range(1,level-1).Sum(CampaignJourneyEconomy.RequiredXP)+xp;
                    int afterXP=Enumerable.Range(1,shop.Level-1).Sum(CampaignJourneyEconomy.RequiredXP)+shop.XP;
                    Assert.That(afterXP,Is.EqualTo(Math.Min(138,beforeXP+4)),"Defeats, including the fatal one, grant four experience.");
                    Assert.That(shop.Gold,Is.EqualTo(gold+loop.Reward.Base+loop.Reward.Interest));
                    var summary=session.CompletedRounds.Last();
                    Assert.That(summary.stage,Is.EqualTo(stage));Assert.That(summary.won,Is.False);
                    Assert.That(summary.healthLoss,Is.EqualTo(loss));Assert.That(summary.remainingHealth,Is.EqualTo(expectedHealth));
                    int settledGold=shop.Gold,settledXP=shop.XP,settledLevel=shop.Level,receipts=session.CompletedRounds.Count;
                    controller.Tick(5);controller.Tick(5);
                    Assert.That(loop.Health,Is.EqualTo(expectedHealth));Assert.That(shop.Gold,Is.EqualTo(settledGold));
                    Assert.That(shop.XP,Is.EqualTo(settledXP));Assert.That(shop.Level,Is.EqualTo(settledLevel));
                    Assert.That(session.CompletedRounds.Count,Is.EqualTo(receipts),"Repeated terminal ticks cannot settle twice.");
                }
                Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);
                if(stage<7)
                {
                    Assert.That(expectedHealth,Is.GreaterThan(0));Assert.That(loop.Phase,Is.EqualTo(BattleShopPhase.Settlement));
                    Assert.That(session.Ended,Is.False);Assert.That(controller.OpenLoopShop(),Is.True);
                }
            }
            Assert.That(expectedHealth,Is.Zero);Assert.That(loop.Phase,Is.EqualTo(BattleShopPhase.Failed));
            Assert.That(session.Ended,Is.True);Assert.That(session.Won,Is.False);
            Assert.That(controller.OpenLoopShop(),Is.False);Assert.That(controller.RestartCampaignEncounter(),Is.False);
            controller.BeginJourney(44);yield return Ready(1);
            Assert.That(CampaignJourneySession.Current,Is.Not.SameAs(session));Assert.That(controller.Loop,Is.Not.SameAs(loop));
            Assert.That(controller.Loop.Health,Is.EqualTo(100));Assert.That(controller.Loop.LastHealthLoss,Is.Zero);
            Assert.That(CampaignJourneySession.Current.CompletedRounds,Is.Empty);
            controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
        }

        [UnityTest]public IEnumerator OpeningRewardHasThreeLowCostGiftsFreeShopAndCarriesFiniteEconomyIntoFirstCombat()
        {
            yield return Begin();var shop=controller.Shop;var loop=controller.Loop;
            Assert.That(CampaignJourneySession.Current.Seed,Is.EqualTo(44));Assert.That(shop.Level,Is.EqualTo(1));Assert.That(shop.Gold,Is.EqualTo(5));
            Assert.That(shop.UnlimitedGold,Is.False);Assert.That(shop.Bench.Count,Is.EqualTo(3));Assert.That(shop.Bench.All(p=>p.Star==1),Is.True);
            Assert.That(shop.Bench.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(3));
            Assert.That(shop.Bench.All(p=>shop.Definition(p.Id).Cost>=1&&shop.Definition(p.Id).Cost<=2),Is.True);
            CollectionAssert.AreEqual(CampaignJourneyEconomy.StartingCards(shop.Roles,44),shop.Bench.Select(p=>p.Id));
            Assert.That(shop.Bench.Count(p=>shop.IsLineupInstance(p.Instance)),Is.EqualTo(1));
            Assert.That(shop.Bench.Count(p=>!shop.IsLineupInstance(p.Instance)),Is.EqualTo(2));
            Assert.That(shop.Offers.Count(id=>id!=null),Is.EqualTo(5));Assert.That(shop.Roles.Any(r=>r.Id==SilverWolfCombat.CardId),Is.False);
            Assert.That(shop.Roles.Count,Is.EqualTo(42));Assert.That(loop.TotalStages,Is.EqualTo(12));
            Assert.That(loop.CanShop,Is.True);Assert.That(loop.RewardStage,Is.True);Assert.That(controller.JourneyStage.title,Is.Not.EqualTo("出发登记"));
            Assert.That(controller.Battle.Defenders,Is.Empty);Assert.That(controller.Battle.CampaignV2Enabled,Is.True);
            controller.Tick(120);Assert.That(controller.Battle.Clock,Is.Zero);Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Ready));
            Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);
            Assert.That(controller.LockJourneyShop(true),Is.True);var lockedOffers=shop.Offers.ToArray();
            controller.ClaimCampaignSupply();int gold=shop.Gold,xp=shop.XP;controller.ClaimCampaignSupply();
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));Assert.That(CampaignJourneySession.Current.CompletedRounds.Count,Is.EqualTo(1));
            Assert.That(gold,Is.EqualTo(13));Assert.That(shop.Level,Is.EqualTo(2));Assert.That(loop.Phase,Is.EqualTo(BattleShopPhase.Settlement));
            string earned=CampaignJourneyEvidenceProbe.State(shop,false);
            Assert.That(controller.OpenLoopShop(),Is.True);yield return Ready(2);
            Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));Assert.That(shop.Level,Is.EqualTo(2));
            Assert.That(CampaignJourneyEvidenceProbe.State(shop,false),Is.EqualTo(earned));CollectionAssert.AreEqual(lockedOffers,shop.Offers);
            Assert.That(shop.Locked,Is.True);Assert.That(shop.CommissionUsed,Is.False);
            int slot=Enumerable.Range(0,5).First(i=>shop.Offers[i]!=null);string expected=shop.Offers[slot];int before=shop.Gold;
            var purchase=controller.PurchaseShopCard(slot);Assert.That(purchase.Success,Is.True,purchase.Message);Assert.That(purchase.Id,Is.EqualTo(expected));
            Assert.That(shop.Gold,Is.EqualTo(before-shop.Definition(expected).Cost));yield return Ready(2);
            var commission=controller.CommissionCampaignCard(expected);Assert.That(commission.Success,Is.True,commission.Message);Assert.That(shop.CommissionUsed,Is.True);
            before=shop.Gold;Assert.That(controller.CommissionCampaignCard(expected).Success,Is.False);Assert.That(shop.Gold,Is.EqualTo(before));
            Assert.That(controller.UpgradeJourneyShop().Success,Is.True);yield return Ready(2);Assert.That(controller.LockJourneyShop(true),Is.True);
            var offers=shop.Offers.ToArray();string prepared=CampaignJourneyEvidenceProbe.State(shop);
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(2);
            CollectionAssert.AreEqual(offers,shop.Offers);Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(prepared));
            Assert.That(controller.Battle.DP,Is.EqualTo(10));Assert.That(controller.Battle.Defense,Is.EqualTo(1));Assert.That(controller.Battle.Defender.MaxHealth,Is.EqualTo(CampaignJourneyCatalog.Stage(2).encounter.defenders[0].health));
            Assert.That(controller.Battle.Defenders.Count,Is.EqualTo(1));Assert.That(controller.Battle.CampaignV2Enabled,Is.True);Assert.That(controller.Battle.CampaignV2Stage,Is.EqualTo(2));
            Assert.That(controller.Battle.WavesEnabled,Is.True);Assert.That(controller.Battle.StrongholdMapEnabled,Is.False);
            Assert.That(controller.Battle.CurrentWaveLaneId,Is.EqualTo("R2"));Assert.That(controller.Battle.CanDeployOnLane("R1"),Is.False);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Map07QuarantineCourt"));Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);
            foreach(var piece in shop.Bench.Where(p=>shop.IsLineupInstance(p.Instance)))
            {
                Assert.That(controller.Battle.FindCard(piece.Id).Settings.Health,Is.EqualTo(CampaignV2Balance.StatValue(piece.Id,piece.Star,"hp")));
            }
            controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Assert.That(CampaignJourneySession.Current,Is.Null);Assert.That(controller.TestSelectionOpen,Is.True);Assert.That(controller.ShopTrialActive,Is.False);
        }

        [UnityTest]public IEnumerator CurrentEncounterRestartPreservesSeededShopAndPauseAndExitClearOnlyCombatSession()
        {
            yield return Begin();yield return CampaignJourneyEvidenceProbe.FirstCombat(c=>controller=c);var shop=controller.Shop;var loop=controller.Loop;
            Assert.That(controller.UpgradeJourneyShop().Success,Is.True);yield return Ready(2);
            Assert.That(controller.LockJourneyShop(true),Is.True);var commission=controller.CommissionCampaignCard(shop.Bench[0].Id);
            Assert.That(commission.Success,Is.True,commission.Message);yield return Ready(2);
            string state=CampaignJourneyEvidenceProbe.State(shop);var tiles=UnityEngine.Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None);
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(2);
            Assert.That(controller.OpenShopProbabilityDetails(),Is.True);Assert.That(controller.ShopProbabilityVisible,Is.True);
            var rect=controller.ShopPieceRect(shop.Bench.ToList().FindIndex(p=>shop.IsLineupInstance(p.Instance)));
            controller.ProcessShopPointer(EventType.MouseDown,rect.center);controller.ProcessShopPointer(EventType.MouseUp,rect.center);
            Assert.That(controller.Battle.Units,Is.Empty,"The probability modal must block wave card input.");
            Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(state));controller.CloseCampaignOverlay();
            string card=controller.Battle.Cards[0].Settings.CardId;Assert.That(CampaignJourneyEvidenceProbe.Drag(controller,card,controller.Battle.CurrentWaveLaneId,out _),Is.True,controller.ShopFeedback);
            controller.Battle.SetPaused(true);double clock=controller.Battle.Clock,cooldown=controller.Battle.CardCooldown(card);var unit=controller.Battle.Units.First();Vector3 position=unit.Motion.Position;float hp=unit.Health,sp=unit.SkillSP;
            controller.Tick(30);Assert.That(controller.Battle.Clock,Is.EqualTo(clock));Assert.That(controller.Battle.CardCooldown(card),Is.EqualTo(cooldown));
            Assert.That(unit.Motion.Position,Is.EqualTo(position));Assert.That(unit.Health,Is.EqualTo(hp));Assert.That(unit.SkillSP,Is.EqualTo(sp));
            Assert.That(controller.RestartCampaignEncounter(),Is.True);yield return Ready(2);
            Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(state));
            Assert.That(controller.Battle.Units,Is.Empty);Assert.That(controller.Battle.Clock,Is.Zero);Assert.That(controller.Battle.DP,Is.EqualTo(10));Assert.That(controller.Battle.Defense,Is.EqualTo(1));
            Assert.That(controller.Battle.IsPaused,Is.False);Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(controller.Loop.Stage,Is.EqualTo(2));
            Assert.That(CampaignJourneySession.Current.Seed,Is.EqualTo(44));CollectionAssert.AreEquivalent(tiles,UnityEngine.Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None));
            Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Assert.That(CampaignJourneySession.Current,Is.Null);Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MonsterTrial"));
            Assert.That(controller.TestSelectionOpen,Is.True);Assert.That(controller.JourneyActive,Is.False);Assert.That(controller.ShopTrialActive,Is.False);
        }
    }
}
