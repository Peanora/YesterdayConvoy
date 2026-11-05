using System.Collections;
using System.Reflection;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FireRainV12Tests
    {
        [UnityTest]
        public IEnumerator NativeFireChangesGpuPixelsCoversGroundAndFreezesWithoutChangingDamage()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var method=typeof(ManualBattleController).GetMethod("VerifyFireRainV12",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            yield return (IEnumerator)method.Invoke(c,new object[]{"artifacts/fire-v12-gpu",false});
        }

        [UnityTest]
        public IEnumerator BoughtTalulahBurnsTheAuthoredBattleShopMapAndClearsOnRestart()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();while(c.ArtLoading)yield return null;
            float old=Time.timeScale;Time.timeScale=0;
            try
            {
                c.StartLateBattleShopLoop(73);yield return null;
                typeof(ManualBattleController).GetField("loopTestSettlementOpen",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,true);
                Assert.That((bool)typeof(ManualBattleController).GetMethod("TryTestSettlement",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{true}),Is.True);
                Assert.That(c.OpenLoopShop(),Is.True);yield return new WaitForSecondsRealtime(.35f);
                string id=c.Shop.Roles.Single(r=>r.Name=="塔露拉").Id;
                int slot=Enumerable.Range(0,5).First(i=>c.Shop.Offers[i]==id);
                Assert.That(c.PurchaseShopCard(slot).Success,Is.True);while(c.ArtLoading)yield return null;
                Assert.That(c.StartLoopNextBattle(),Is.True);yield return new WaitForSecondsRealtime(.3f);
                int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==id);
                var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[0].Entry));
                c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                Assert.That(c.Battle.Units.Count,Is.EqualTo(1));c.Tick(4.05f);
                var unit=c.Battle.Units[0];c.SelectSkillUnit(unit.EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                c.Tick(7.2f);yield return null;
                var floor=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.enabled&&s.name=="Talulah ground flame").ToArray();
                Assert.That(floor.Length,Is.GreaterThan(80));
                Assert.That(floor.Max(s=>s.transform.position.x)-floor.Min(s=>s.transform.position.x),Is.GreaterThan(14));
                Directory.CreateDirectory("artifacts/fire-v12-gpu");
                typeof(ManualBattleController).GetMethod("PatriotCapture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"artifacts/fire-v12-gpu/fire-v12-battle-shop.png"});
                c.RestartBattle();yield return null;
                Assert.That(Object.FindObjectsByType<SpriteRenderer>().Any(s=>s.enabled&&s.name.StartsWith("Talulah ground flame")),Is.False);
            }
            finally{Time.timeScale=old;}
        }
    }
}
