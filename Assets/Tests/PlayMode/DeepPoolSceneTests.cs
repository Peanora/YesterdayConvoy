using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DeepPoolSceneTests
    {
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("DeepPoolTrial");
            for(int i=0;i<1800;i++)
            {
                var c=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.SynergySandbox&&!c.ArtLoading&&!c.ShopPanelTransitioning)yield break;
                yield return null;
            }
            Assert.Fail("Deep-pool authored scene did not become ready.");
        }

        static bool Drag(ManualBattleController c,string card)
        {
            int slot=c.Shop.Bench.ToList().FindIndex(p=>p.Id==card);
            Assert.That(slot,Is.GreaterThanOrEqualTo(0));
            int before=c.Battle.Units.Count;
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(slot).center);
            c.ProcessShopPointer(EventType.MouseDrag,gate);
            c.ProcessShopPointer(EventType.MouseUp,gate);
            return c.Battle.Units.Count==before+1&&c.Battle.Units.Last().CardId==card;
        }

        [UnityTest] public IEnumerator SavedDeepPoolSceneConnectsOrdinaryDragOriginalSprayAndIndependentScorch()
        {
            float scale=Time.timeScale;
            ManualBattleController c=null;
            try
            {
                yield return Load();
                c=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
                Assert.That(UnityEngine.Object.FindAnyObjectByType<AuthoredBattlefield>(),Is.Not.Null);
                Assert.That(c.Battle.DeepTier,Is.EqualTo(4));
                Assert.That(c.StartLoopNextBattle());
                for(int i=0;i<1800&&c.ShopPanelTransitioning;i++)yield return null;
                Assert.That(c.ShopPanelTransitioning,Is.False);
                Time.timeScale=0;
                c.Battle.Begin();c.Battle.SetPaused(true);
                int dp=c.Battle.DP;
                Assert.That(Drag(c,"CARD_M31"));Assert.That(Drag(c,"CARD_M32"));
                int cost=c.Battle.FindCard("CARD_M31").Settings.DeploymentCost+c.Battle.FindCard("CARD_M32").Settings.DeploymentCost;
                Assert.That(c.Battle.DP,Is.EqualTo(dp-cost));
                c.Tick(1);yield return null;yield return null;
                Assert.That(c.Battle.Clock,Is.Zero);
                Assert.That(UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Any(v=>v.Renderer.enabled&&v.Renderer.sprite!=null&&v.Renderer.sprite.name.StartsWith("M31/",StringComparison.Ordinal)));
                Assert.That(UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Any(v=>v.Renderer.enabled&&v.Renderer.sprite!=null&&v.Renderer.sprite.name.StartsWith("M32/",StringComparison.Ordinal)));
                var mandra=c.CombatView!=null?c.CombatView.GetComponent<MandragoraEffectView>():null;
                Assert.That(mandra,Is.Not.Null);Assert.That(mandra.VisibleEffects,Is.GreaterThan(0));
                c.Battle.SetPaused(false);
                bool connected=false;
                for(int i=0;i<100&&!connected;i++)
                {
                    c.Tick(.05f);yield return null;
                    var spray=c.CombatView!=null?c.CombatView.GetComponent<FlamerazerEffectView>():null;
                    connected=c.Battle.FlameSprays.Any(s=>s.Ticks>0)&&spray!=null&&spray.VisibleEffects>0&&c.Battle.Defenders.Any(d=>c.Battle.DeepBurnRemaining(d.EntityId)>0);
                }
                Assert.That(connected,Is.True,"Real spray damage must connect to original VFX and independent deep scorch.");
                Assert.That(c.Battle.BurnBursts,Is.Zero,"The new scorch must start before original elemental burn reaches its threshold.");
                var target=c.Battle.Defenders.First(d=>c.Battle.DeepBurnRemaining(d.EntityId)>0);
                Assert.That(c.Battle.DeepBurnPower(target.EntityId),Is.EqualTo(25).Within(.01));
                c.Battle.SetPaused(true);
                double clock=c.Battle.Clock,remaining=c.Battle.DeepBurnRemaining(target.EntityId);
                float hp=target.Health;
                c.Tick(2);yield return null;
                Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                Assert.That(c.Battle.DeepBurnRemaining(target.EntityId),Is.EqualTo(remaining));
                Assert.That(target.Health,Is.EqualTo(hp));
            }
            finally
            {
                Time.timeScale=scale;
                if(c!=null)c.LeaveShopTrial();
            }
        }
    }
}
