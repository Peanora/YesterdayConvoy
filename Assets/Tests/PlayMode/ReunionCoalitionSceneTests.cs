using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ReunionCoalitionSceneTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;float oldScale;
        [SetUp] public void Setup()=>oldScale=Time.timeScale;
        [TearDown] public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=oldScale;
        }
        IEnumerator Ready()
        {
            for(int i=0;i<1800;i++)
            {
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading&&!controller.ShopPanelTransitioning){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("The saved Reunion trial did not finish preparing its art and shop panel.");
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("ReunionTrial");
            for(int i=0;i<1800;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);yield return Ready();Time.timeScale=0;
        }
        IEnumerator Roster(string[] ids)
        {
            yield return (IEnumerator)typeof(ManualBattleController).GetMethod("PrepareSynergyBadgeEvidenceRoster",Hidden).Invoke(controller,new object[]{ids});
            yield return Ready();
        }
        void Replay()=>typeof(ManualBattleController).GetMethod("ReplaySandbox",Hidden).Invoke(controller,null);
        bool Drag(string id,Vector2? destination=null)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0),id);
            var origin=controller.ShopPieceRect(index);Assert.That(origin.width,Is.GreaterThan(0),id);
            var target=destination??TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(controller.Battle.Entry));
            int before=controller.Battle.Units.Count(u=>!u.SynergySupport);
            controller.ProcessShopPointer(EventType.MouseDown,origin.center);controller.ProcessShopPointer(EventType.MouseDrag,target);controller.ProcessShopPointer(EventType.MouseUp,target);
            return controller.Battle.Units.Count(u=>!u.SynergySupport)==before+1;
        }

        [UnityTest] public IEnumerator SavedTrialAndEveryTierUseProductionPausedDeploymentAndFiniteSupport()
        {
            yield return Load();var c=controller;
            Assert.That(c.Battle.ReunionCount,Is.EqualTo(8));Assert.That(c.Battle.ReunionFormationCount,Is.EqualTo(4));Assert.That(c.Battle.ReunionTier,Is.EqualTo(8));
            Assert.That(SynergyBadgePresentation.GradeFor("整合运动",c.Battle.ReunionTier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            foreach(int tier in new[]{2,4,6,8})
            {
                yield return Roster(SynergyTrialCatalog.Find("reunion").Members.Take(tier).ToArray());
                c.Shop.Validate();Assert.That(c.Battle.ReunionTier,Is.EqualTo(tier));
                Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();c.Battle.Begin();c.Battle.SetPaused(true);
                int before=c.Battle.DP,spent=0;
                foreach(string id in SynergyTrialCatalog.Find("reunion").Members.Take(c.Battle.ReunionMusterRequirement))
                {
                    var card=c.Battle.FindCard(id);int uses=card.RemainingUses;spent+=card.Settings.DeploymentCost;
                    Assert.That(Drag(id),Is.True);Assert.That(card.RemainingUses,Is.EqualTo(uses-1));
                }
                Assert.That(c.Battle.DP,Is.EqualTo(before-spent));Assert.That(c.Battle.MusterUsed,Is.EqualTo(1));Assert.That(c.Battle.MusterMemberIds,Is.Empty);
                var support=c.Battle.Units.Where(u=>u.SynergySupport).ToArray();Assert.That(support.Length,Is.EqualTo(tier>=6?2:1));
                Assert.That(support.All(u=>u.SynergyShield==0&&u.ReunionEntryShieldPending==(tier==8)),Is.True);
                c.Tick(10);Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(before-spent));
                var previous=c.Battle;Replay();yield return Ready();
                Assert.That(c.Battle,Is.Not.SameAs(previous));Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.MusterUsed,Is.Zero);
                Assert.That(c.Battle.ReunionTier,Is.EqualTo(tier));
            }
        }

        [UnityTest] public IEnumerator OutsidersInvalidReleasesAndDuplicateCardCooldownCannotFinishMuster()
        {
            yield return Load();var c=controller;
            yield return Roster(SynergyTrialCatalog.Find("reunion").Members.Take(4).Concat(new[]{"CARD_M05"}).ToArray());
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();c.Battle.Begin();c.Battle.SetPaused(true);
            Assert.That(Drag("CARD_M07"),Is.True);Assert.That(c.Battle.MusterMemberIds.Count,Is.EqualTo(1));
            int before=c.Battle.DP;Assert.That(Drag("CARD_M07"),Is.False);Assert.That(c.Battle.DP,Is.EqualTo(before));
            Assert.That(Drag("CARD_M05"),Is.True);Assert.That(c.Battle.MusterMemberIds.Count,Is.EqualTo(1));
            before=c.Battle.DP;Assert.That(Drag("CARD_M09",new Vector2(1270,10)),Is.False);Assert.That(c.Battle.DP,Is.EqualTo(before));
            Assert.That(c.Battle.MusterMemberIds.Count,Is.EqualTo(1));Assert.That(c.Battle.MusterUsed,Is.Zero);
            Assert.That(Drag("CARD_M08"),Is.True);Assert.That(c.Battle.MusterMemberIds.Count,Is.EqualTo(2));
            Assert.That(Drag("CARD_M09"),Is.True);Assert.That(c.Battle.MusterUsed,Is.EqualTo(1));Assert.That(c.Battle.Units.Count(u=>u.SynergySupport),Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator EliteWardWaitsForNaturalCombatAndPausesWithoutRefilling()
        {
            yield return Load();var c=controller;Assert.That(c.StartLoopNextBattle(),Is.True);yield return Ready();c.Battle.Begin();c.Battle.SetPaused(true);
            foreach(string id in SynergyTrialCatalog.Find("reunion").Members.Take(3))Assert.That(Drag(id),Is.True);
            var support=c.Battle.Units.Where(u=>u.ReunionEliteSupport).ToArray();Assert.That(support.Length,Is.EqualTo(2));
            var soldier=support.Single(u=>u.CardId=="CARD_M07");Assert.That(support.All(u=>u.ReunionEntryShieldPending&&u.SynergyShield==0),Is.True);
            c.Battle.SetPaused(false);c.Tick(.05f);
            Assert.That(soldier.ReunionEntryShieldPending,Is.True);Assert.That(soldier.Motion.DistanceTravelled,Is.GreaterThan(0));
            DeployedUnitState engaged=support.FirstOrDefault(u=>!u.ReunionEntryShieldPending);
            for(int i=0;i<1200&&engaged==null&&c.Battle.Phase==BattlePhase.Running;i++)
            {c.Tick(.05f);engaged=support.FirstOrDefault(u=>!u.ReunionEntryShieldPending);if(i%4==0)yield return null;}
            Assert.That(engaged,Is.Not.Null);Assert.That(engaged.SynergyShieldUntil,Is.GreaterThan(c.Battle.Clock));
            Assert.That(engaged.SynergyShield,Is.EqualTo(Mathf.Floor(engaged.MaxHealth*.2f+.5f)).Within(.001f));
            c.Battle.SetPaused(true);double clock=c.Battle.Clock,expiry=engaged.SynergyShieldUntil;float ward=engaged.SynergyShield;
            c.Tick(8);Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(engaged.SynergyShield,Is.EqualTo(ward));Assert.That(engaged.SynergyShieldUntil,Is.EqualTo(expiry));
            c.Battle.SetPaused(false);
            for(int i=0;i<180&&c.Battle.Clock<=expiry+.2&&c.Battle.Phase==BattlePhase.Running;i++){c.Tick(.05f);if(i%4==0)yield return null;}
            Assert.That(c.Battle.Clock,Is.GreaterThan(expiry));Assert.That(engaged.ReunionEntryShieldPending,Is.False);Assert.That(engaged.SynergyShieldUntil,Is.EqualTo(expiry));
        }
    }
}
