using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class DedicatedSynergySceneTests
    {
        static ManualBattleController Controller=>Object.FindAnyObjectByType<ManualBattleController>();
        static IEnumerator Load(SynergyTrialCatalog.Entry entry)
        {
            yield return SceneManager.LoadSceneAsync(entry.SceneName);
            for(int frame=0;frame<1200;frame++)
            {
                if(Controller!=null&&Controller.CampaignActive&&!Controller.ArtLoading){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("Dedicated scene startup timed out: "+entry.SceneName);
        }
        static void Replay(ManualBattleController c)=>typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
        static void AssertPool(ManualBattleController c)
        {
            Assert.That(c.Shop.Roles.Count,Is.EqualTo(42));Assert.That(c.Shop.UnlimitedGold,Is.True);
            foreach(var role in c.Shop.Roles)
            {
                int owned=c.Shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>p.Star==3?9:p.Star==2?3:1);
                Assert.That(c.Shop.Available(role.Id)+owned+c.Shop.Offers.Count(id=>id==role.Id),Is.EqualTo(9),role.Id);
            }
            c.Shop.Validate();
        }
        [UnityTest]public IEnumerator NineSavedScenesLoadTheRequestedFormationAndKeepConfigurationOnRestart()
        {
            foreach(var entry in SynergyTrialCatalog.Entries)
            {
                yield return Load(entry);var c=Controller;var bootstrap=Object.FindAnyObjectByType<CampaignBootstrap>();var data=bootstrap.trialDefinition;
                Assert.That(data,Is.Not.Null);Assert.That(data.trialId,Is.EqualTo(entry.Id));
                Assert.That(Object.FindAnyObjectByType<AuthoredBattlefield>(),Is.Not.Null);Assert.That(Object.FindObjectsByType<CampaignBootstrap>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                Assert.That(Object.FindAnyObjectByType<ShopMapBootstrap>(),Is.Null);Assert.That(c.ShopVisible,Is.True);Assert.That(c.Loop.CanShop,Is.True);
                Assert.That(c.Shop.Level,Is.EqualTo(data.initialLevel));Assert.That(c.Shop.Bench.Select(p=>p.Id),Is.EquivalentTo(data.initialMembers.Select(m=>m.cardId)));
                Assert.That(SynergySummary.From(c.Shop.Bench.Select(p=>p.Id)).Single(s=>s.Name==entry.Tag).Tier,Is.EqualTo(entry.Id=="mudrock"?5:entry.MaximumTier));
                if(entry.Id=="mudrock")
                {
                    Assert.That(c.Loop.GolemExperience,Is.Zero);Assert.That(c.MudrockTransferIds,Is.Empty);
                    Assert.That(c.SetMudrockTransfer("CARD_M08",true),Is.True);Assert.That(c.Battle.MudrockTier,Is.EqualTo(6));
                    Assert.That(c.Battle.GolemStage,Is.EqualTo(1),"The sixth starter is a normal card, not a gifted level.");
                }
                Assert.That(c.Battle.DP,Is.EqualTo(100));AssertPool(c);
                var baseline=data.encounter.defenders.Select(d=>d.health).ToArray();
                for(int i=0;i<baseline.Length;i++)Assert.That(c.Battle.Defenders[i].MaxHealth,Is.EqualTo(baseline[i]*12));
                Assert.That(c.StartLoopNextBattle(),Is.True);Assert.That(c.RestartCampaignEncounter(),Is.True);
                for(int i=0;i<baseline.Length;i++)Assert.That(c.Battle.Defenders[i].MaxHealth,Is.EqualTo(baseline[i]*12),"Restart cannot reset to the shared guards or multiply HP twice.");
                Replay(c);Assert.That(c.Loop.CanShop,Is.True);int gold=c.Shop.Gold;
                Assert.That(c.Shop.Refresh(true).Success,Is.True);int offer=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]!=null);
                Assert.That(c.PurchaseShopCard(offer).Success,Is.True);Assert.That(c.Shop.Gold,Is.EqualTo(gold));AssertPool(c);
                CollectionAssert.AreEqual(baseline,data.encounter.defenders.Select(d=>d.health));c.LeaveShopTrial();yield return null;
            }
        }
        [UnityTest]public IEnumerator InspectorOverridesLevelStarsAndDefendersWithoutChangingTheSourceDefinition()
        {
            yield return Load(SynergyTrialCatalog.Find("snow"));var c=Controller;var original=Object.FindAnyObjectByType<CampaignBootstrap>().trialDefinition;
            var edited=Object.Instantiate(original);var encounter=Object.Instantiate(original.encounter);edited.encounter=encounter;
            edited.initialLevel=7;edited.initialMembers=new[]{new SynergyTrialStartingCard{cardId="CARD_M11",star=2},new SynergyTrialStartingCard{cardId="CARD_M12",star=1}};
            float sourceHealth=original.encounter.defenders[0].health;encounter.defenders[0].health=321;
            yield return c.PrepareCampaign(true,44,edited);
            Assert.That(c.Shop.Level,Is.EqualTo(7));Assert.That(c.Shop.Bench.Single(p=>p.Id=="CARD_M11").Star,Is.EqualTo(2));AssertPool(c);
            Assert.That(c.Battle.Defenders[0].MaxHealth,Is.EqualTo(321*12));Assert.That(c.StartLoopNextBattle(),Is.True);Assert.That(c.RestartCampaignEncounter(),Is.True);
            Assert.That(c.Battle.Defenders[0].MaxHealth,Is.EqualTo(321*12));Assert.That(original.encounter.defenders[0].health,Is.EqualTo(sourceHealth));
            c.LeaveShopTrial();Object.Destroy(edited);Object.Destroy(encounter);yield return null;
        }
        [UnityTest,Category("DedicatedSynergyNatural")]public IEnumerator InfectionAndHerdCanTriggerFromActualDefenderAttacks()
        {
            foreach(string id in new[]{"infection","herd"})
            {
                yield return Load(SynergyTrialCatalog.Find(id));var c=Controller;Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();
                string card=id=="infection"?"CARD_ORIGINIUM_SLUG":"CARD_M21";
                var deployment=c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,card);Assert.That(deployment.Success,Is.True);
                bool triggered=false;
                for(int i=0;i<900&&c.Loop.CanDeploy;i++)
                {
                    c.Tick(.1f);triggered=id=="infection"?c.Battle.InfectionBursts.Count>0:c.Battle.HerdUsed>0;
                    if(triggered)break;if(i%10==0)yield return null;
                }
                Assert.That(triggered,Is.True,id+" must be observable without injecting damage or forced death.");
                Assert.That(c.Battle.FindUnit(deployment.EntityId).Health,Is.LessThanOrEqualTo(0));c.LeaveShopTrial();yield return null;
            }
        }
    }
}
