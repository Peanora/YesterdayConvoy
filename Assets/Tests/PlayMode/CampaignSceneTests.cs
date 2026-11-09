using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class CampaignSceneTests
    {
        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            for(int i=0;i<900;i++)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.CampaignActive&&!c.ArtLoading){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("Campaign startup timed out");
        }
        [UnityTest]public IEnumerator SandboxHasAuthoredMapUnlimitedShopAndRealFourStarStats()
        {
            yield return Load("SynergyTrial");var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.SynergySandbox);Assert.That(c.ShopVisible);Assert.That(c.Battle.ReunionTier,Is.EqualTo(6));Assert.That(c.Battle.SnowTier,Is.EqualTo(5));
            Assert.That(Object.FindAnyObjectByType<AuthoredBattlefield>(),Is.Not.Null);
            int gold=c.Shop.Gold;
            for(int i=0;i<8;i++)
            {
                if(c.Shop.Available("CARD_M11")>0)Assert.That(c.CommissionCampaignCard("CARD_M11").Success);
                else {int slot=Enumerable.Range(0,5).First(n=>c.Shop.Offers[n]=="CARD_M11");Assert.That(c.PurchaseShopCard(slot).Success);}
            }
            var state=c.Battle.FindCard("CARD_M11");Assert.That(state.Settings.Health,Is.EqualTo(405*4));Assert.That(state.Settings.DeploymentCost,Is.EqualTo(4));Assert.That(state.Settings.DeploymentLimit,Is.EqualTo(8));
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count(p=>p.Id=="CARD_M11"),Is.EqualTo(1));
            for(int i=0;i<18;i++)
            {
                if(c.Shop.Available("CARD_M11")>0)Assert.That(c.CommissionCampaignCard("CARD_M11").Success);
                else {int slot=Enumerable.Range(0,5).First(n=>c.Shop.Offers[n]=="CARD_M11");Assert.That(c.PurchaseShopCard(slot).Success);}
            }
            Assert.That(c.Shop.Bench.Single(p=>p.Id=="CARD_M11").Star,Is.EqualTo(4));
            Assert.That(c.Battle.FindCard("CARD_M11").Settings.Health,Is.EqualTo(574*4));
            Assert.That(c.Battle.FindCard("CARD_M11").Settings.DeploymentCost,Is.EqualTo(5));
            Assert.That(c.CommissionCampaignCard("CARD_M11").Success,Is.False);Assert.That(c.StartLoopNextBattle());
            Assert.That(c.CommissionCampaignCard("CARD_M11").Success,Is.False);yield return null;
            c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator PausedDragDeploymentCreatesVisibleSoldiersWithoutAdvancingClock()
        {
            yield return Load("SynergyTrial");var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();c.Battle.SetPaused(true);yield return new WaitForSecondsRealtime(.3f);
            foreach(string id in new[]{"CARD_M07","CARD_M08","CARD_M11"})
            {
                int i=c.Shop.Bench.ToList().FindIndex(p=>p.Id==id);var screen=Camera.main.WorldToScreenPoint(c.Battle.Entry);var gate=TacticalViewport.Current.FromScreen(screen);
                c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(i).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                Assert.That(c.Battle.Units.Any(u=>u.CardId==id),Is.True,id+" / gate="+gate+" / "+c.Feedback+" / "+c.ShopFeedback);
            }
            Assert.That(c.Battle.MusterUsed,Is.EqualTo(1));Assert.That(c.Battle.Units.Count(u=>u.SynergySupport),Is.EqualTo(2));Assert.That(c.Battle.DP,Is.EqualTo(94));
            Assert.That(c.Battle.Clock,Is.Zero);c.Tick(2);Assert.That(c.Battle.Clock,Is.Zero);yield return null;
            Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(5));
            Assert.That(c.RestartCampaignEncounter());Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.MusterUsed,Is.Zero);c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator TwoOpeningSupplyStopsPayExactlyOnceAndKeepDefense()
        {
            yield return Load("Campaign");ManualBattleController c=null;yield return CampaignJourneyEvidenceProbe.Ready(1,value=>c=value);
            Assert.That(c.Loop.RewardStage);int defense=c.Battle.Defense;c.ClaimCampaignSupply();
            Assert.That(c.Battle.Defense,Is.EqualTo(defense));Assert.That(c.Shop.Gold,Is.EqualTo(8));c.ClaimCampaignSupply();Assert.That(c.Shop.Gold,Is.EqualTo(8));
            Assert.That(c.OpenLoopShop());yield return CampaignJourneyEvidenceProbe.Ready(2,value=>c=value);
            Assert.That(c.StartLoopNextBattle());Assert.That(c.Loop.RewardStage);
            c.ClaimCampaignSupply();Assert.That(c.Shop.Gold,Is.EqualTo(12));Assert.That(c.OpenLoopShop());
            yield return CampaignJourneyEvidenceProbe.Ready(3,value=>c=value);Assert.That(c.Loop.Stage,Is.EqualTo(3));Assert.That(c.Loop.RewardStage,Is.False);
            c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator NaturalFrozenShatterAndMusterCanClearSandboxWithoutForcedVictory()
        {
            yield return Load("SynergyTrial");var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.StartLoopNextBattle());
            c.Battle.Begin();long request=10000;
            c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,"CARD_M12");
            c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,"CARD_M11");
            bool extraCaster=false,fullWave=false;
            for(int tick=0;tick<2600&&c.Loop.CanDeploy;tick++)
            {
                if(!extraCaster&&c.Battle.Clock>=3){extraCaster=c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,"CARD_M12").Success;}
                if(!fullWave&&c.Battle.Shatters>0)
                {
                    fullWave=true;foreach(string id in new[]{"CARD_M07","CARD_M08","CARD_M13","CARD_M06"})c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,id);
                }
                if(fullWave&&tick%50==0)foreach(string id in new[]{"CARD_M11","CARD_M12","CARD_M07","CARD_M08"})c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,id);
                c.Tick(.1f);if(tick%10==0)yield return null;
            }
            Assert.That(c.Battle.Shatters,Is.GreaterThan(0));Assert.That(c.Battle.MusterUsed,Is.GreaterThan(0));
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),$"time={c.Battle.Clock}, DP={c.Battle.DP}, left={c.Battle.Defense}");c.LeaveShopTrial();
        }
    }
}
