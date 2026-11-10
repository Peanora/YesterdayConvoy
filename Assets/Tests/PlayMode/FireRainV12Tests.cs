using System.Collections;
using System.Collections.Generic;
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
        public IEnumerator ApprovedMapsShowOrangeRainOnTheSavedTerrainWithGpuPauseAndCleanupChecks()
        {
            float old=Time.timeScale;Time.timeScale=0;
            try
            {
                CampaignJourneySession.Clear();
                foreach(var map in ApprovedMapCatalog.All)
                {
                    yield return SceneManager.LoadSceneAsync(map.sceneName);
                    ManualBattleController c=null;
                    yield return ApprovedMapEvidenceProbe.Ready(map.id,value=>c=value);
                    // A finite-stock three-star fixture triggers the existing skill on
                    // each real saved map. It does not claim normal card acquisition.
                    const string talulah="CARD_TALULAH_TRIAL";
                    c.Shop.Preset(new[]{new ShopTrialModel.Piece{Id=talulah,Star=3}},System.Array.Empty<string>(),8,0,0);
                    var lineup=(List<string>)typeof(ManualBattleController).GetField("shopLineup",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
                    lineup.Clear();lineup.Add(talulah);
                    typeof(ManualBattleController).GetMethod("SyncShopDeck",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{false,false});
                    c.CloseCampaignOverlay();while(c.ArtLoading)yield return null;
                    typeof(ManualBattleController).GetMethod("PrepareNextLoopBattle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
                    yield return ApprovedMapEvidenceProbe.Ready(map.id,value=>c=value);
                    Assert.That(c.Loop.CanDeploy,Is.True);
                    Assert.That(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(map.width*map.height),map.id+" saved terrain must remain present");
                    Assert.That(Object.FindObjectsByType<MeshRenderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.name=="Textured ground slab"),Is.True);
                    Assert.That(ApprovedMapEvidenceProbe.Drag(c,talulah,c.Battle.Lanes[0].Entry),Is.True);
                    var unit=c.Battle.Units.Single(u=>u.CardId==talulah);
                    for(int i=0;i<200&&!c.Battle.IsSkillReady(unit.EntityId);i++)c.Tick(.05f);
                    c.SelectSkillUnit(unit.EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                    c.Tick(1.25f);yield return null;
                    var fire=Object.FindAnyObjectByType<FireRainView>();
                    Assert.That(fire.VisibleStreams,Is.GreaterThan(0),map.id+" orange rain must render on authored terrain");
                    string directory="artifacts/campaign-feedback-2026-11-10/fire-rain/"+map.id;Directory.CreateDirectory(directory);
                    var active=Capture(Path.Combine(directory,"orange-rain.png"));
                    // FireRainView shares the controller root with previews and other
                    // effects. Toggle only its own layers for the GPU/pause comparison.
                    var renderers=fire.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&
                        (r.name=="Horizontal fire stream"||r.name=="Turbulent flame tail"||
                         r.name=="Fire reflected glow"||r.name=="Airborne fire tongue"||r.name=="Fire ember")).ToArray();
                    foreach(var r in renderers)r.enabled=false;
                    var hidden=Capture(null);foreach(var r in renderers)r.enabled=true;
                    double delta=active.Zip(hidden,(a,b)=>(double)(System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b))).Average();
                    Assert.That(delta,Is.GreaterThan(.05),map.id+" fire must change actual GPU pixels");
                    c.Battle.SetPaused(true);double clock=c.Battle.Clock;var positions=renderers.Select(r=>r.transform.position).ToArray();
                    var frames=renderers.OfType<SpriteRenderer>().Select(r=>r.sprite).ToArray();
                    c.Tick(10);yield return null;
                    Assert.That(c.Battle.Clock,Is.EqualTo(clock));
                    CollectionAssert.AreEqual(positions,renderers.Select(r=>r.transform.position));
                    CollectionAssert.AreEqual(frames,renderers.OfType<SpriteRenderer>().Select(r=>r.sprite));
                    c.Battle.SetPaused(false);c.Tick(5);yield return null;
                    Assert.That(fire.VisibleStreams,Is.Zero,map.id+" completed cast must clear");
                    c.RestartBattle();yield return ApprovedMapEvidenceProbe.Ready(map.id,value=>c=value);
                    Assert.That(fire.VisibleStreams,Is.Zero);Assert.That(c.Battle.Units,Is.Empty);
                    Debug.Log("APPROVED_MAP_FIRE_RAIN_PASS: "+map.id+" gpuDelta="+delta);
                }
            }
            finally{Time.timeScale=old;CampaignJourneySession.Clear();}
        }

        static Color32[] Capture(string path)=>(Color32[])typeof(ManualBattleController)
            .GetMethod("PatriotCapture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path});

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
