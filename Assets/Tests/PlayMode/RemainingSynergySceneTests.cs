using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class RemainingSynergySceneTests
    {
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<900;i++){var c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading)yield break;yield return null;}
            Assert.Fail("Sandbox timeout");
        }
        static void Replay(ManualBattleController c)=>typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
        [UnityTest]public IEnumerator SynergyDetailBlocksDragBehindItWithoutSpendingResources()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();c.SetSandboxRoster(7);c.StartLoopNextBattle();c.Battle.Begin();yield return new WaitForSecondsRealtime(.35f);
            var field=typeof(ManualBattleController).GetField("inspectedSynergy",BindingFlags.Instance|BindingFlags.NonPublic);field.SetValue(c,"海嗣");
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Count,Is.Zero);Assert.That(c.Battle.DP,Is.EqualTo(100));field.SetValue(c,null);
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Any(u=>u.CardId=="CARD_M33"));yield return null;c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator FourAdditionalPresetsUseExistingCardsAndNormalPausedDrag()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();
            for(int preset=4;preset<8;preset++)
            {
                Assert.That(c.SetSandboxRoster(preset));int[] tiers={c.Battle.HerdTier,c.Battle.DeepTier,c.Battle.SarkazTier,c.Battle.MarineTier};Assert.That(tiers[preset-4],Is.EqualTo(new[]{4,2,6,4}[preset-4]));
                Assert.That(c.Shop.Roles.Count,Is.EqualTo(39));Assert.That(c.Shop.Roles.All(r=>r.Id!="CARD_M32"));
                string id=c.Shop.Bench[0].Id;Assert.That(c.StartLoopNextBattle());c.Battle.Begin();c.Battle.SetPaused(true);yield return new WaitForSecondsRealtime(.35f);
                var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));int before=c.Battle.DP;
                c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(0).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                Assert.That(c.Battle.Units.Any(u=>u.CardId==id&&!u.SynergySupport));Assert.That(c.Battle.DP,Is.EqualTo(before-c.Battle.FindCard(id).Settings.DeploymentCost));
                c.Tick(1);Assert.That(c.Battle.Clock,Is.Zero);Replay(c);
            }
            yield return null;c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator ActualHerdArtAndContractBurstRemainPlayable()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();c.SetSandboxRoster(4);c.StartLoopNextBattle();c.Battle.Begin();
            var receipt=c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_M21");c.Battle.TryDamageUnit(receipt.EntityId,100000,CombatDamageType.Physical);c.Tick(.05f);yield return null;
            var host=c.Battle.Units.Single(u=>c.Battle.IsHerdHost(u.EntityId));Assert.That(host.CardId,Is.EqualTo("CARD_M21"));Assert.That(c.Battle.UnitCombat(host).HasSkill,Is.False);
            Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Any(v=>v.Renderer.sprite!=null&&v.Renderer.sprite.name.StartsWith("M21/")));
            Replay(c);c.SetSandboxRoster(6);Assert.That(c.SetRemainingChoices("CARD_M37",MarineAdaptation.Hunt,MarineAdaptation.Infiltration));c.StartLoopNextBattle();c.Battle.Begin();
            Assert.That(c.SetRemainingChoices("CARD_M19",MarineAdaptation.Barrier,MarineAdaptation.Hunt),Is.False);
            var target=c.Battle.FindUnit(c.Battle.TryDeployRole(c.Battle.RunId,2,c.Battle.Entry,"CARD_M37").EntityId);
            long request=10;foreach(var id in new[]{"CARD_M19","CARD_M24","CARD_M26","CARD_M27"}){var u=c.Battle.FindUnit(c.Battle.TryDeployRole(c.Battle.RunId,++request,c.Battle.Entry,id).EntityId);c.Battle.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);}
            Assert.That(c.Battle.ContractPoints,Is.EqualTo(4));Assert.That(c.UseContract());Assert.That(c.Battle.ContractAttackBonus(target),Is.EqualTo(.4f));Assert.That(target.SynergyShield,Is.GreaterThan(0));yield return null;c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator NativeDeepSprayCreatesSeedsAndMarineRetaliationEarnsPersistentGrowth()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();c.SetSandboxRoster(5);c.StartLoopNextBattle();c.Battle.Begin();
            Assert.That(c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_M31").Success);
            for(int i=0;i<1600&&c.Battle.DeepCharges==0;i++){c.Tick(.1f);if(i%20==0)yield return null;}
            Assert.That(c.Battle.DeepCharges,Is.GreaterThan(0),"Native burn and normal kill must trigger the real deep-pool chain");
            Replay(c);c.SetSandboxRoster(7);c.SetRemainingChoices("CARD_M33",MarineAdaptation.Infiltration,MarineAdaptation.Hunt);c.StartLoopNextBattle();c.Battle.Begin();
            var u=c.Battle.FindUnit(c.Battle.TryDeployRole(c.Battle.RunId,2,c.Battle.Entry,"CARD_M33").EntityId);
            float armor=c.Battle.DefenderArmor(c.Battle.Defender);c.Battle.TryDamageUnit(u.EntityId,u.MaxHealth*.16f,CombatDamageType.Physical);
            Assert.That(c.Battle.CreeperPulses.Count,Is.GreaterThan(0));Assert.That(c.Battle.DefenderArmor(c.Battle.Defender),Is.LessThan(armor));
            typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.Battle,new object[]{c.Battle.RunId,true});c.Tick(.01f);
            Assert.That(c.Loop.LastMarineGrowth,Is.GreaterThan(0));int xp=c.Loop.MarineExperience;Replay(c);Assert.That(c.Battle.MarineExperience,Is.EqualTo(xp));yield return null;c.LeaveShopTrial();
        }
    }
}
