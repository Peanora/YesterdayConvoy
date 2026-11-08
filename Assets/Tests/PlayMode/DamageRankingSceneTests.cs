using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DamageRankingSceneTests
    {
        ManualBattleController controller;
        float oldScale;
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
            Assert.Fail("The saved sandbox did not finish preparing its art and panels.");
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<1800;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);yield return Ready();Time.timeScale=0;
        }
        void Deploy(string id)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0),id);
            Vector2 from=controller.ShopPieceRect(index).center,to=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(controller.Battle.Entry));
            int count=controller.Battle.Units.Count(u=>!u.SynergySupport);
            controller.ProcessShopPointer(EventType.MouseDown,from);controller.ProcessShopPointer(EventType.MouseDrag,to);controller.ProcessShopPointer(EventType.MouseUp,to);
            Assert.That(controller.Battle.Units.Count(u=>!u.SynergySupport),Is.EqualTo(count+1),id+": "+controller.Feedback);
        }
        IEnumerator Engage()
        {
            Assert.That(controller.StartLoopNextBattle(),Is.True);yield return Ready();controller.Battle.Begin();controller.Battle.SetPaused(true);
            foreach(string id in new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M14"})Deploy(id);
            controller.Battle.SetPaused(false);
            for(int i=0;i<2400&&controller.Battle.Phase==BattlePhase.Running&&controller.Battle.DamageRanking.Count(r=>r.Damage>0)<2;i++)
            {controller.Tick(.05f);if(i%20==0)yield return null;}
            Assert.That(controller.Battle.DamageRanking.Count(r=>r.Damage>0),Is.GreaterThanOrEqualTo(2),"At least two real combat sources must contribute.");
        }
        [UnityTest] public IEnumerator NaturalCombatProducesSortedRealDamageAndPauseRetainsIt()
        {
            yield return Load();Assert.That(controller.DamageRankingVisible,Is.True);Assert.That(controller.Battle.TotalPlayerDamage,Is.Zero);
            yield return Engage();var battle=controller.Battle;var rows=battle.DamageRanking.ToArray();
            Assert.That(rows.Select(r=>r.Damage),Is.Ordered.Descending);
            Assert.That(rows.Sum(r=>r.Damage),Is.EqualTo(battle.TotalPlayerDamage).Within(.01f));
            Assert.That(rows.All(r=>r.Damage>0&&!float.IsNaN(r.Damage)&&!float.IsInfinity(r.Damage)),Is.True);
            battle.SetPaused(true);double clock=battle.Clock;float total=battle.TotalPlayerDamage;controller.Tick(5);yield return null;
            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(battle.TotalPlayerDamage,Is.EqualTo(total));
            Assert.That(controller.ShopMapPointAvailable(ManualBattleController.DamageRankingPanelRect.center),Is.False);
            Assert.That(ManualBattleController.DefenderInspectorRect.Overlaps(ManualBattleController.DamageRankingPanelRect),Is.False,"Defender details must leave the persistent ranking visible.");
            var skilled=battle.Units.Where(u=>u.Active&&battle.UnitCombat(u).HasSkill).ToArray();Assert.That(skilled,Is.Not.Empty);
            foreach(var unit in skilled)
            {
                controller.OpenSkillMenu(unit.EntityId);Assert.That(controller.SkillMenuOpen,Is.True);
                Assert.That(controller.SkillPopupRect.Overlaps(ManualBattleController.DamageRankingPanelRect),Is.False,unit.CardId+" skill popup must not sit beneath the ranking.");
                Assert.That(controller.SkillCastRect.Overlaps(ManualBattleController.DamageRankingPanelRect),Is.False,unit.CardId+" skill button must remain reachable.");
                controller.CloseSkillMenu();
            }
        }
        [UnityTest] public IEnumerator ReplayingTheSandboxStartsANewEmptyDamageRanking()
        {
            yield return Load();yield return Engage();var previous=controller.Battle;
            Assert.That(previous.TotalPlayerDamage,Is.GreaterThan(0));
            typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
            yield return Ready();Assert.That(controller.Battle,Is.Not.SameAs(previous));Assert.That(controller.Battle.TotalPlayerDamage,Is.Zero);
            Assert.That(controller.Battle.DamageRanking,Is.Empty);Assert.That(controller.Battle.Units,Is.Empty);Assert.That(controller.DamageRankingVisible,Is.True);
        }
    }
}
