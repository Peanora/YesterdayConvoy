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
        public IEnumerator RestoredOrangeFlamesChangeGpuPixelsAndFreezeWithoutChangingDamage()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var method=typeof(ManualBattleController).GetMethod("VerifyFireRainV12",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            yield return (IEnumerator)method.Invoke(c,new object[]{"artifacts/fire-orange-gpu",false});
        }

        [UnityTest]
        public IEnumerator BoughtTalulahSweepsTheAuthoredBattleShopMapAndClearsOnRestart()
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
                c.Tick(3.2f);yield return null;
                var flames=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.enabled&&s.name=="Horizontal fire stream").ToArray();
                Assert.That(flames.Length,Is.GreaterThan(5));
                Assert.That(flames.All(s=>s.sharedMaterial.shader.name=="YesterdayConvoy/FireRainAdditive"),Is.True);
                var camera=Camera.main;
                var ys=flames.Select(s=>camera.WorldToViewportPoint(s.transform.position).y).ToArray();
                Assert.That(ys.Max()-ys.Min(),Is.GreaterThan(.4f),"Waves must cover map depth instead of retaining the small preview's five rows.");
                Directory.CreateDirectory("artifacts/fire-orange-gpu");
                typeof(ManualBattleController).GetMethod("PatriotCapture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"artifacts/fire-orange-gpu/fire-orange-battle-shop.png"});
                c.RestartBattle();yield return null;
                Assert.That(Object.FindObjectsByType<SpriteRenderer>().Any(s=>s.enabled&&s.name=="Horizontal fire stream"),Is.False);
            }
            finally{Time.timeScale=old;}
        }

        [UnityTest]
        public IEnumerator MapCoverageUpdatesWhenAnAuthoredSurfaceExpandsWhileTheSimulationIsPaused()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            float old=Time.timeScale;Time.timeScale=0;GameObject added=null;
            try
            {
                c.SwitchTrial(true);while(c.ArtLoading)yield return null;
                c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);c.Tick(4.05f);
                c.SelectSkillUnit(c.Battle.Units[0].EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                c.Tick(3.2f);yield return null;
                var fire=Object.FindAnyObjectByType<FireRainView>();
                var boundsField=typeof(FireRainView).GetField("groundScreenBounds",BindingFlags.Instance|BindingFlags.NonPublic);
                var rowsField=typeof(FireRainView).GetField("rowCount",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(boundsField,Is.Not.Null);Assert.That(rowsField,Is.Not.Null);
                var before=(Rect)boundsField.GetValue(fire);int rows=(int)rowsField.GetValue(fire);
                Assert.That(before.height,Is.GreaterThan(0));Assert.That(fire.VisibleStreams,Is.GreaterThan(0));
                double clock=c.Battle.Clock;c.Battle.SetPaused(true);
                var surface=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Cell ")).OrderByDescending(r=>r.bounds.max.z).First();
                // Authored scene renderers may be statically batched; cloning one copies its
                // baked bounds. A fresh runtime surface gives this test real changing geometry.
                added=GameObject.CreatePrimitive(PrimitiveType.Cube);added.name="Cell adaptive fire coverage test";
                SceneManager.MoveGameObjectToScene(added,fire.gameObject.scene);
                added.transform.position=new Vector3(surface.bounds.center.x,surface.bounds.max.y,surface.bounds.max.z+8);
                added.transform.localScale=new Vector3(2,.1f,2);
                Assert.That(added.GetComponent<MeshRenderer>().bounds.max.z,Is.GreaterThan(surface.bounds.max.z+4));
                yield return null;yield return null;
                var after=(Rect)boundsField.GetValue(fire);
                Assert.That(after.height,Is.GreaterThan(before.height+1),"Coverage must follow newly enabled authored ground without restarting the battle.");
                Assert.That((int)rowsField.GetValue(fire),Is.GreaterThan(rows),"A deeper map needs more flame rows instead of stretching six fixed rows.");
                Assert.That(c.Battle.Clock,Is.EqualTo(clock),"Refreshing geometry must not advance gameplay.");
                var streams=Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name=="Horizontal fire stream").ToArray();
                Assert.That(streams.Length,Is.GreaterThan(0));
                Assert.That(streams.All(r=>r.sharedMaterial.shader.name=="YesterdayConvoy/FireRainAdditive"),Is.True);
            }
            finally{if(added!=null)Object.Destroy(added);Time.timeScale=old;}
        }
    }
}
