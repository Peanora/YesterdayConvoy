using System;
using System.Collections;
using System.Linq;
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

        [UnityTest]public IEnumerator OrdinarySuppliesKeepRealEconomyFiniteInventoryLockedOffersAndV2FirstCombat()
        {
            yield return Begin();var shop=controller.Shop;var loop=controller.Loop;
            Assert.That(CampaignJourneySession.Current.Seed,Is.EqualTo(44));Assert.That(shop.Level,Is.EqualTo(1));Assert.That(shop.Gold,Is.EqualTo(5));
            Assert.That(shop.UnlimitedGold,Is.False);Assert.That(shop.Bench.Count,Is.EqualTo(1));Assert.That(shop.Bench[0].Star,Is.EqualTo(1));
            Assert.That(shop.Definition(shop.Bench[0].Id).Cost,Is.EqualTo(1));Assert.That(shop.Offers.All(id=>id==null),Is.True);
            Assert.That(controller.Battle.Defenders,Is.Empty);Assert.That(controller.Battle.CampaignV2Enabled,Is.True);
            controller.Tick(120);Assert.That(controller.Battle.Clock,Is.Zero);Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Ready));
            controller.ClaimCampaignSupply();int gold=shop.Gold,xp=shop.XP;controller.ClaimCampaignSupply();
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(shop.XP,Is.EqualTo(xp));Assert.That(CampaignJourneySession.Current.CompletedRounds.Count,Is.EqualTo(1));
            Assert.That(controller.OpenLoopShop(),Is.True);yield return Ready(2);
            Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));Assert.That(shop.Level,Is.EqualTo(2));
            int slot=Enumerable.Range(0,5).First(i=>shop.Offers[i]!=null);string expected=shop.Offers[slot];int before=shop.Gold;
            var purchase=controller.PurchaseShopCard(slot);Assert.That(purchase.Success,Is.True,purchase.Message);Assert.That(purchase.Id,Is.EqualTo(expected));
            Assert.That(shop.Gold,Is.EqualTo(before-shop.Definition(expected).Cost));yield return Ready(2);
            var commission=controller.CommissionCampaignCard(expected);Assert.That(commission.Success,Is.True,commission.Message);Assert.That(shop.CommissionUsed,Is.True);
            before=shop.Gold;Assert.That(controller.CommissionCampaignCard(expected).Success,Is.False);Assert.That(shop.Gold,Is.EqualTo(before));
            Assert.That(controller.UpgradeJourneyShop().Success,Is.True);yield return Ready(2);Assert.That(controller.LockJourneyShop(true),Is.True);
            var offers=shop.Offers.ToArray();Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(2);controller.ClaimCampaignSupply();
            string earned=CampaignJourneyEvidenceProbe.State(shop,false);Assert.That(controller.OpenLoopShop(),Is.True);yield return Ready(3);
            Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));Assert.That(CampaignJourneyEvidenceProbe.State(shop,false),Is.EqualTo(earned));
            CollectionAssert.AreEqual(offers,shop.Offers);Assert.That(shop.Locked,Is.True);Assert.That(shop.CommissionUsed,Is.False);
            Assert.That(controller.Battle.DP,Is.EqualTo(10));Assert.That(controller.Battle.Defense,Is.EqualTo(1));Assert.That(controller.Battle.Defender.MaxHealth,Is.GreaterThan(260));
            Assert.That(controller.Battle.Defenders.Count,Is.EqualTo(1));Assert.That(controller.Battle.CampaignV2Enabled,Is.True);Assert.That(controller.Battle.CampaignV2Stage,Is.EqualTo(3));
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
            Assert.That(controller.UpgradeJourneyShop().Success,Is.True);yield return Ready(3);
            Assert.That(controller.LockJourneyShop(true),Is.True);var commission=controller.CommissionCampaignCard(shop.Bench[0].Id);
            Assert.That(commission.Success,Is.True,commission.Message);yield return Ready(3);
            string state=CampaignJourneyEvidenceProbe.State(shop);var tiles=UnityEngine.Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None);
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready(3);
            Assert.That(controller.OpenShopProbabilityDetails(),Is.True);Assert.That(controller.ShopProbabilityVisible,Is.True);
            var rect=controller.ShopPieceRect(shop.Bench.ToList().FindIndex(p=>shop.IsLineupInstance(p.Instance)));
            controller.ProcessShopPointer(EventType.MouseDown,rect.center);controller.ProcessShopPointer(EventType.MouseUp,rect.center);
            Assert.That(controller.Battle.Units,Is.Empty,"The probability modal must block wave card input.");
            Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(state));controller.CloseCampaignOverlay();
            string card=controller.Battle.Cards[0].Settings.CardId;Assert.That(CampaignJourneyEvidenceProbe.Drag(controller,card,controller.Battle.CurrentWaveLaneId,out _),Is.True,controller.ShopFeedback);
            controller.Battle.SetPaused(true);double clock=controller.Battle.Clock,cooldown=controller.Battle.CardCooldown(card);var unit=controller.Battle.Units.First();Vector3 position=unit.Motion.Position;float hp=unit.Health,sp=unit.SkillSP;
            controller.Tick(30);Assert.That(controller.Battle.Clock,Is.EqualTo(clock));Assert.That(controller.Battle.CardCooldown(card),Is.EqualTo(cooldown));
            Assert.That(unit.Motion.Position,Is.EqualTo(position));Assert.That(unit.Health,Is.EqualTo(hp));Assert.That(unit.SkillSP,Is.EqualTo(sp));
            Assert.That(controller.RestartCampaignEncounter(),Is.True);yield return Ready(3);
            Assert.That(controller.Shop,Is.SameAs(shop));Assert.That(controller.Loop,Is.SameAs(loop));Assert.That(CampaignJourneyEvidenceProbe.State(shop),Is.EqualTo(state));
            Assert.That(controller.Battle.Units,Is.Empty);Assert.That(controller.Battle.Clock,Is.Zero);Assert.That(controller.Battle.DP,Is.EqualTo(10));Assert.That(controller.Battle.Defense,Is.EqualTo(1));
            Assert.That(controller.Battle.IsPaused,Is.False);Assert.That(controller.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(controller.Loop.Stage,Is.EqualTo(3));
            Assert.That(CampaignJourneySession.Current.Seed,Is.EqualTo(44));CollectionAssert.AreEquivalent(tiles,UnityEngine.Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None));
            Assert.That(CampaignJourneyEvidenceProbe.StockConserved(shop),Is.True);controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Assert.That(CampaignJourneySession.Current,Is.Null);Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MonsterTrial"));
            Assert.That(controller.TestSelectionOpen,Is.True);Assert.That(controller.JourneyActive,Is.False);Assert.That(controller.ShopTrialActive,Is.False);
        }
    }
}
