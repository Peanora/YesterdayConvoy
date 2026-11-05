using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    /// <summary>Visual lifecycle tests. Defeat is an explicit fixture, not a natural combat claim.</summary>
    public sealed class TalulahDeathPresentationTests
    {
        float previousScale;
        [SetUp] public void Setup() { previousScale=Time.timeScale; }
        [TearDown] public void Cleanup() { Time.timeScale=previousScale; }

        static DemoSpriteVisual Visual(DeployedUnitState unit)
        {
            var actor=GameObject.Find(unit.EntityId);
            Assert.That(actor,Is.Not.Null,"A normal deployment must create its actor before the death fixture.");
            var visual=actor.GetComponentInChildren<DemoSpriteVisual>();
            Assert.That(visual,Is.Not.Null);
            return visual;
        }

        static void DefeatFixture(ManualBattle battle,DeployedUnitState unit,bool endBattle)
        {
            // Invoke the same defeat path that combat damage uses. This fixture deliberately
            // avoids changing authored defender stats just to exercise a presentation tail.
            typeof(DeployedUnitState).GetProperty("Health").SetValue(unit,0f);
            var defeat=typeof(ManualBattle).GetMethod("DefeatUnit",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(defeat,Is.Not.Null);
            defeat.Invoke(battle,new object[]{unit});
            Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead));
            if(!endBattle)return;
            var finish=typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(finish,Is.Not.Null);
            Assert.That((bool)finish.Invoke(battle,new object[]{battle.RunId,false}),Is.True);
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Lost));
        }

        [UnityTest]
        public IEnumerator FinalDeathPlaysTheFullOriginalClipBeforeSettlementWithoutAdvancingCombat()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            c.SwitchTrial(true);while(c.ArtLoading)yield return null;
            c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            c.Tick(.1f);yield return null;
            var unit=c.Battle.Units.Single();var visual=Visual(unit);
            Assert.That(visual.Renderer.sprite.name,Does.StartWith("TalulahOriginal/"));
            Vector3 position=unit.Motion.Position;

            DefeatFixture(c.Battle,unit,true);c.Tick(0);yield return null;
            double combatClock=c.Battle.Clock;
            Assert.That(c.SettlementVisible,Is.False,"The result may be final while its death presentation is still visible.");
            Assert.That(c.ViewCount,Is.EqualTo(1));
            Assert.That(visual.FrameState,Is.EqualTo("Death"));
            Assert.That(visual.Renderer.sprite.name,Does.StartWith("TalulahOriginal/death/"));
            Sprite first=visual.Renderer.sprite;int firstIndex=visual.FrameIndex;

            c.Tick(.45f);yield return null;
            Assert.That(visual.FrameIndex,Is.GreaterThan(firstIndex));
            Assert.That(visual.Renderer.sprite,Is.Not.EqualTo(first),"Die must animate after the terminal combat clock stops.");
            Assert.That(unit.Motion.Position,Is.EqualTo(position));
            Assert.That(visual.transform.parent.position,Is.EqualTo(position));
            Assert.That(c.Battle.Clock,Is.EqualTo(combatClock));
            c.Tick(2.15f);yield return null;
            Assert.That(c.ViewCount,Is.EqualTo(1),"The original Die lasts 2.666667 seconds; a one-second cleanup truncates it.");
            Assert.That(c.SettlementVisible,Is.False);
            c.Tick(.10f);yield return null;
            Assert.That(c.ViewCount,Is.Zero);
            Assert.That(c.SettlementVisible,Is.True);
            Assert.That(c.Battle.Clock,Is.EqualTo(combatClock),"The death tail is presentation time, not more gameplay.");
            Assert.That(unit.Motion.Position,Is.EqualTo(position));
        }

        [UnityTest]
        public IEnumerator AShopBoughtTalulahKeepsHerOwnDeathTailPausesAndClearsOnRestart()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();while(c.ArtLoading)yield return null;
            Time.timeScale=0;c.StartLateBattleShopLoop(73);yield return null;
            typeof(ManualBattleController).GetField("loopTestSettlementOpen",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,true);
            Assert.That((bool)typeof(ManualBattleController).GetMethod("TryTestSettlement",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{true}),Is.True);
            Assert.That(c.OpenLoopShop(),Is.True);yield return new WaitForSecondsRealtime(.35f);
            string cardId=c.Shop.Roles.Single(r=>r.Name=="塔露拉").Id;
            int slot=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]==cardId);
            Assert.That(c.PurchaseShopCard(slot).Success,Is.True);while(c.ArtLoading)yield return null;
            Assert.That(c.StartLoopNextBattle(),Is.True);yield return new WaitForSecondsRealtime(.3f);
            int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==cardId);
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[0].Entry));
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);
            c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1));c.Tick(.1f);yield return null;
            var unit=c.Battle.Units[0];var visual=Visual(unit);
            Assert.That(c.CardForUnit(unit).CardId,Is.EqualTo("CARD_TALULAH_TRIAL"));
            Assert.That(c.Definition.Card.CardId,Is.Not.EqualTo("CARD_TALULAH_TRIAL"),"Exercise Talulah as a non-primary roster card.");
            Vector3 position=unit.Motion.Position;

            DefeatFixture(c.Battle,unit,false);c.Tick(0);yield return null;
            c.Tick(1.1f);yield return null;
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(c.ViewCount,Is.EqualTo(1),"A mixed roster must not use the primary monster's shorter death tail.");
            Assert.That(visual.FrameState,Is.EqualTo("Death"));
            Assert.That(visual.Renderer.sprite.name,Does.StartWith("TalulahOriginal/death/"));
            Assert.That(visual.FrameIndex,Is.GreaterThan(0));
            c.Battle.SetPaused(true);
            double clock=c.Battle.Clock,presentation=c.PresentationClock;int index=visual.FrameIndex;Sprite frame=visual.Renderer.sprite;
            c.Tick(10);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.PresentationClock,Is.EqualTo(presentation));
            Assert.That(visual.FrameIndex,Is.EqualTo(index));Assert.That(visual.Renderer.sprite,Is.EqualTo(frame));
            Assert.That(unit.Motion.Position,Is.EqualTo(position));
            c.RestartBattle();while(c.ArtLoading)yield return null;yield return null;
            Assert.That(c.ViewCount,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);
            Assert.That(c.SettlementVisible,Is.False);
            Assert.That(Object.FindObjectsByType<DemoSpriteVisual>().Any(v=>v.FrameState=="Death"),Is.False);
        }
    }
}
