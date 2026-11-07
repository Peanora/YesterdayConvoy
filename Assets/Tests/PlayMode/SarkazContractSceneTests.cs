using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class SarkazContractSceneTests
    {
        const BindingFlags State=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static ManualBattleController Controller=>Object.FindAnyObjectByType<ManualBattleController>();
        static IEnumerator Ready()
        {
            for(int i=0;i<1800;i++)
            {
                if(Controller!=null&&Controller.SynergySandbox&&!Controller.ArtLoading&&!Controller.ShopPanelTransitioning){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("The real Sarkaz trial did not finish loading its cards and shop panel.");
        }
        static void AssertPool(ManualBattleController c)
        {
            foreach(var role in c.Shop.Roles)
            {
                int owned=c.Shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>ShopTrialModel.Copies(p.Star));
                Assert.That(c.Shop.Available(role.Id)+owned+c.Shop.Offers.Count(id=>id==role.Id),Is.EqualTo(c.Shop.Capacity(role.Id)),role.Id);
            }
            c.Shop.Validate();
        }
        static void Replay(ManualBattleController c)=>typeof(ManualBattleController).GetMethod("ReplaySandbox",State).Invoke(c,null);
        static Rect CardRect(ManualBattleController c,string id)
        {
            int index=c.Shop.Bench.ToList().FindIndex(p=>p.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0),id);
            var rect=c.ShopPieceRect(index);Assert.That(rect.width,Is.GreaterThan(0),id);return rect;
        }
        static string ShopState(ManualBattleController c)
        {
            var lineup=(IEnumerable<string>)typeof(ManualBattleController).GetField("shopLineup",State).GetValue(c);
            return c.Shop.Gold+"|"+string.Join(",",c.Shop.Offers)+"|"+string.Join(",",c.Shop.Bench.Select(p=>p.Instance+":"+p.Id+":"+p.Star))+"|"+
                string.Join(",",c.Shop.Roles.Select(r=>r.Id+":"+c.Shop.Available(r.Id)))+"|"+string.Join(",",lineup);
        }
        static void DragBadge(ManualBattleController c,Vector2 target)
        {
            var badge=c.SarkazContractBadgeRect;Assert.That(badge.width,Is.GreaterThan(0));
            c.ProcessShopPointer(EventType.MouseDown,badge.center);Assert.That(c.SarkazContractBadgeDragging,Is.True);
            c.ProcessShopPointer(EventType.MouseDrag,target);c.ProcessShopPointer(EventType.MouseUp,target);
            Assert.That(c.SarkazContractBadgeDragging,Is.False);
        }

        [UnityTest]public IEnumerator FiveRealPresetsGrantPermanentContractAndOneEntryShieldDuringNormalPausedDrag()
        {
            yield return SceneManager.LoadSceneAsync("SarkazTrial");yield return Ready();var c=Controller;
            int[] tiers={2,4,6,8,9};float[] hp={1.1f,1.2f,1.3f,1.4f,1.6f},attack={.35f,.60f,.90f,1.20f,1.50f},shield={.20f,.30f,.40f,.50f,.60f};
            for(int i=0;i<tiers.Length;i++)
            {
                Assert.That(c.SetSarkazTierPreset(tiers[i]),Is.True);yield return Ready();
                Assert.That(c.Battle.SarkazTier,Is.EqualTo(tiers[i]));Assert.That(c.Shop.Bench.Count,Is.EqualTo(tiers[i]));Assert.That(c.Shop.Level,Is.GreaterThanOrEqualTo(tiers[i]));AssertPool(c);
                Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();c.Battle.SetPaused(true);yield return Ready();
                var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
                int dp=c.Battle.DP;var card=c.Battle.FindCard("CARD_M37");int uses=card.RemainingUses;
                c.ProcessShopPointer(EventType.MouseDown,CardRect(c,"CARD_M37").center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                var u=c.Battle.Units.Single(o=>o.CardId=="CARD_M37"&&!o.SynergySupport);
                Assert.That(u.MaxHealth,Is.EqualTo(card.Settings.Health*hp[i]).Within(.02f));Assert.That(c.Battle.DP,Is.EqualTo(dp-card.Settings.DeploymentCost));Assert.That(card.RemainingUses,Is.EqualTo(uses-1));
                Assert.That(c.Battle.ContractAttackBonus(u),Is.EqualTo(attack[i]).Within(.001f));Assert.That(c.Battle.ContractShieldRemaining(u),Is.EqualTo(u.MaxHealth*shield[i]).Within(.02f));
                Assert.That(c.UseContract(),Is.False);Assert.That(c.Battle.CanActivateContract,Is.False);Assert.That(c.Battle.ContractUses,Is.Zero);Assert.That(c.Battle.ContractCooldownRemaining,Is.Zero);
                double clock=c.Battle.Clock;float entryShield=c.Battle.ContractShieldRemaining(u);c.Tick(100);
                Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.ContractShieldRemaining(u),Is.EqualTo(entryShield));
                c.Battle.SetPaused(false);int afterDeploy=c.Battle.DP;float sp=u.SkillSP;Assert.That(c.UseContract(),Is.False);
                Assert.That(c.Battle.DP,Is.EqualTo(afterDeploy));Assert.That(u.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.ContractPoints,Is.Zero);
                Assert.That(c.Battle.Units.Count(o=>!o.SynergySupport),Is.EqualTo(1),"No tier requires deploying the whole formation to activate the owner.");
                c.Battle.SetPaused(true);yield return null;Replay(c);yield return Ready();Assert.That(c.Battle.Units,Is.Empty);
            }
            c.LeaveShopTrial();yield return null;
        }

        [UnityTest]public IEnumerator BadgePointerAssignsASarkazCardAndRejectsAnOutsiderWithoutBuyingSellingOrChangingFormation()
        {
            yield return SceneManager.LoadSceneAsync("SarkazTrial");yield return Ready();var c=Controller;
            Assert.That(c.SetSandboxRoster(6),Is.True);yield return Ready();
            var before=ShopState(c);Assert.That(SynergyBadgePresentation.IconFor("萨卡兹"),Is.Not.Null);
            DragBadge(c,CardRect(c,"CARD_M37").center);Assert.That(c.PreparedContractCard,Is.EqualTo("CARD_M37"));Assert.That(c.Battle.ContractCard,Is.EqualTo("CARD_M37"));
            Assert.That(ShopState(c),Is.EqualTo(before));AssertPool(c);
            DragBadge(c,CardRect(c,"CARD_M18").center);
            Assert.That(c.ShopFeedback,Is.EqualTo("无法使羁绊外的角色成为承契者"));Assert.That(c.PreparedContractCard,Is.EqualTo("CARD_M37"));Assert.That(ShopState(c),Is.EqualTo(before));AssertPool(c);
            c.ProcessShopPointer(EventType.MouseUp,CardRect(c,"CARD_M18").center);Assert.That(c.PreparedContractCard,Is.EqualTo("CARD_M37"));Assert.That(ShopState(c),Is.EqualTo(before));
            DragBadge(c,CardRect(c,"CARD_M38").center);Assert.That(c.PreparedContractCard,Is.EqualTo("CARD_M38"));Assert.That(ShopState(c),Is.EqualTo(before));
            Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();c.Battle.SetPaused(true);yield return Ready();
            c.ProcessShopPointer(EventType.MouseDown,c.SarkazContractBadgeRect.center);Assert.That(c.SarkazContractBadgeDragging,Is.False);Assert.That(c.ShopFeedback,Does.Contain("锁定"));
            Assert.That(c.TryAssignSarkazContract("CARD_M37"),Is.False);Assert.That(c.PreparedContractCard,Is.EqualTo("CARD_M38"));
            c.LeaveShopTrial();yield return null;
        }

        [UnityTest]public IEnumerator CancelledOrModalBlockedBadgeGestureCannotChangeOwnerOrLeakIntoOrdinaryCardInput()
        {
            yield return SceneManager.LoadSceneAsync("SarkazTrial");yield return Ready();var c=Controller;Assert.That(c.SetSandboxRoster(6),Is.True);yield return Ready();
            string owner=c.PreparedContractCard;string before=ShopState(c);var target=CardRect(c,"CARD_M37").center;
            c.ProcessShopPointer(EventType.MouseDown,c.SarkazContractBadgeRect.center);Assert.That(c.SarkazContractBadgeDragging,Is.True);
            c.ProcessShopPointer(EventType.MouseDrag,target);c.CancelShopDrag();Assert.That(c.SarkazContractBadgeDragging,Is.False);c.ProcessShopPointer(EventType.MouseUp,target);
            Assert.That(c.PreparedContractCard,Is.EqualTo(owner));Assert.That(ShopState(c),Is.EqualTo(before));
            DragBadge(c,ManualBattleController.ShopOfferRect(0).center);Assert.That(c.PreparedContractCard,Is.EqualTo(owner));Assert.That(ShopState(c),Is.EqualTo(before),"Dropping the badge onto a product cannot purchase it.");
            var inspected=typeof(ManualBattleController).GetField("inspectedSynergy",State);inspected.SetValue(c,"萨卡兹");
            c.ProcessShopPointer(EventType.MouseDown,c.SarkazContractBadgeRect.center);c.ProcessShopPointer(EventType.MouseDrag,target);c.ProcessShopPointer(EventType.MouseUp,target);
            Assert.That(c.SarkazContractBadgeDragging,Is.False);Assert.That(c.PreparedContractCard,Is.EqualTo(owner));Assert.That(ShopState(c),Is.EqualTo(before));
            inspected.SetValue(c,null);AssertPool(c);c.LeaveShopTrial();yield return null;
        }
    }
}
