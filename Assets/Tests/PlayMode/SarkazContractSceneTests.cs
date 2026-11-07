using System.Collections;
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
                int owned=c.Shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>p.Star==3?9:p.Star==2?3:1);
                Assert.That(c.Shop.Available(role.Id)+owned+c.Shop.Offers.Count(id=>id==role.Id),Is.EqualTo(9),role.Id);
            }
            c.Shop.Validate();
        }
        static void Replay(ManualBattleController c)=>typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);

        [UnityTest]public IEnumerator FiveRealPresetsKeepFiniteStockAndPausedDragThenExposeManualOrPermanentContract()
        {
            yield return SceneManager.LoadSceneAsync("SarkazTrial");yield return Ready();var c=Controller;
            int[] tiers={2,4,6,8,9};float[] hp={1.1f,1.2f,1.3f,1.4f,1.6f},attack={.35f,.60f,.90f,1.20f,1.50f},shield={.20f,.30f,.40f,.50f,.60f};
            for(int i=0;i<tiers.Length;i++)
            {
                Assert.That(c.SetSarkazTierPreset(tiers[i]),Is.True);yield return Ready();
                Assert.That(c.Battle.SarkazTier,Is.EqualTo(tiers[i]));Assert.That(c.Shop.Bench.Count,Is.EqualTo(tiers[i]));Assert.That(c.Shop.Level,Is.GreaterThanOrEqualTo(tiers[i]));AssertPool(c);
                Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();c.Battle.SetPaused(true);yield return Ready();
                var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
                int slot=c.Shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M37");Assert.That(slot,Is.GreaterThanOrEqualTo(0));
                int dp=c.Battle.DP;var card=c.Battle.FindCard("CARD_M37");int uses=card.RemainingUses;
                c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(slot).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                var u=c.Battle.Units.Single(o=>o.CardId=="CARD_M37"&&!o.SynergySupport);
                Assert.That(u.MaxHealth,Is.EqualTo(card.Settings.Health*hp[i]).Within(.02f));Assert.That(c.Battle.DP,Is.EqualTo(dp-card.Settings.DeploymentCost));Assert.That(card.RemainingUses,Is.EqualTo(uses-1));
                Assert.That(c.UseContract(),Is.False,"Pause must not allow manual activation.");double clock=c.Battle.Clock;c.Tick(10);Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                c.Battle.SetPaused(false);int afterDeploy=c.Battle.DP;float sp=u.SkillSP;
                Assert.That(c.UseContract(),Is.EqualTo(tiers[i]<9));
                Assert.That(c.Battle.ContractAttackBonus(u),Is.EqualTo(attack[i]).Within(.001f));Assert.That(c.Battle.ContractShieldRemaining(u),Is.EqualTo(u.MaxHealth*shield[i]).Within(.02f));
                Assert.That(c.Battle.DP,Is.EqualTo(afterDeploy));Assert.That(u.SkillSP,Is.EqualTo(sp));Assert.That(c.Battle.ContractPoints,Is.Zero);
                Assert.That(c.Battle.ContractUses,Is.EqualTo(tiers[i]<9?1:0));
                if(tiers[i]==9)Assert.That(c.Battle.Units.Count(o=>!o.SynergySupport),Is.EqualTo(1),"The ninth tier does not demand nine deployments.");
                c.Battle.SetPaused(true);yield return null;Replay(c);yield return Ready();
            }
            c.LeaveShopTrial();yield return null;
        }
    }
}
