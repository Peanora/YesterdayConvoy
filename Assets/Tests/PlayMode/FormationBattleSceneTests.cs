using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YesterdayConvoy.Tests
{
    public sealed class FormationBattleSceneTests
    {
        FormationBattleController controller;
        float previousScale;
        [SetUp] public void SaveClock()=>previousScale=Time.timeScale;
        [TearDown] public void RestoreClock(){if(controller!=null&&controller.Model!=null)controller.Model.Paused=true;Time.timeScale=previousScale;}
        IEnumerator Ready()
        {
            FormationShopSession.StartNew(20261117);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<FormationBattleController>();
                if(controller!=null&&controller.Ready&&controller.Model!=null)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready,Is.True,"Existing animation packages must finish preparing.");
            Assert.That(controller.Model,Is.Not.Null);yield return Market(false);
        }
        IEnumerator Market(bool visible)
        {
            controller.SetMarketVisible(visible);
            double deadline=Time.realtimeSinceStartupAsDouble+3;
            while(controller.ShopPanelTransitioning&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.ShopPanelTransitioning,Is.False,"The original shop slide must finish before testing uncovered map input.");
            yield return null;
        }
        static Vector2 StagePoint(FormationBattleController c,Vector3 world)
        {
            var point=c.ShopModelCamera.WorldToViewportPoint(world);
            return new Vector2(point.x*1280,(1-point.y)*720);
        }
        static Bounds IndexedGateBounds(Transform root,MeshRenderer[] layers)
        {
            bool found=false;var bounds=new Bounds();
            foreach(var layer in layers)
            {
                var mesh=layer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
                foreach(int index in mesh.triangles)
                {
                    var point=root.InverseTransformPoint(layer.transform.TransformPoint(vertices[index]));
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            if(!found)throw new InvalidOperationException("The native gate has no rendered triangles.");
            return bounds;
        }
        static MeshFilter[] Terrain()=>Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(m=>m.gameObject.scene==SceneManager.GetActiveScene()&&m.sharedMesh!=null).ToArray();
        static bool OutsideBoard(Bounds bounds)=>bounds.max.x<=-7+.01f||bounds.min.x>=7-.01f||
            bounds.max.z<=-4+.01f||bounds.min.z>=4-.01f;
        void Drag(Vector2 from,Vector2 to)
        {
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(from));
            controller.ProcessPointer(EventType.MouseDrag,FormationBattleController.CellScreen(to));
            controller.ProcessPointer(EventType.MouseUp,FormationBattleController.CellScreen(to));
        }
        [UnityTest] public IEnumerator AuthoredRectangleAndOriginalActorsRemainVisibleAndSurviveRetry()
        {
            yield return Ready();
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo("Assets/Scenes/FormationBattle.unity"));
            var meshes=Terrain();Assert.That(meshes.Length,Is.GreaterThan(20),"The saved independent scene must contain terrain before combat.");
            var positions=meshes.ToDictionary(m=>m,m=>m.transform.position);
            var cam=controller.battleCamera;Assert.That(cam,Is.Not.Null);
            Assert.That(cam.rect,Is.EqualTo(TacticalViewport.Current.CameraRect(new Rect(0,0,1280,720))),"The native map uses the full virtual screen under the shared original shop.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y,0)),Is.LessThan(.1f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.z,0)),Is.LessThan(.1f));
            var sprites=controller.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&r.gameObject.layer!=31).ToArray();
            Assert.That(sprites.Length,Is.GreaterThanOrEqualTo(controller.Model.Units.Count(u=>u.Enabled)+controller.Model.Defenders.Count));
            Assert.That(sprites.All(s=>s.sprite.texture!=null),Is.True);
            foreach(var actor in controller.Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(controller.Model.Defenders))
            {
                var projected=cam.WorldToScreenPoint(FormationBattleController.WorldPoint(actor.Position));
                var point=TacticalViewport.Current.FromScreen(projected);
                Assert.That(projected.z,Is.GreaterThan(0),actor.Name+" is behind the camera.");
                Assert.That(point.x,Is.InRange(180,1012),actor.Name+" must stay inside the native battlefield, clear of the right controls.");
                Assert.That(point.y,Is.InRange(65,490),actor.Name+" feet must stay above the original lineup platform.");
            }
            yield return new WaitForEndOfFrame();
            var screenshot=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(screenshot,Is.Not.Null);
            try
            {
                Assert.That(screenshot.width,Is.EqualTo(Screen.width));Assert.That(screenshot.height,Is.EqualTo(Screen.height));
                var pixels=screenshot.GetPixels32();int visible=0,samples=0;byte low=255,high=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {byte v=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));low=Math.Min(low,v);high=Math.Max(high,v);if(v>24)visible++;samples++;}
                Assert.That(visible,Is.GreaterThan(samples/20));Assert.That(high-low,Is.GreaterThan(16));
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/formation-demo"));Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory,"playmode-preparation.png"),screenshot.EncodeToPNG());
            }
            finally{Object.Destroy(screenshot);}
            controller.StartBattle();controller.Model.Advance(.5f);yield return null;controller.Retry();yield return null;
            Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(controller.Model.Clock,Is.Zero);
            CollectionAssert.AreEquivalent(meshes,Terrain());
            Assert.That(Terrain().All(m=>m.transform.position==positions[m]),Is.True,"Retry must preserve saved terrain objects and edits.");
            Debug.Log("FORMATION_SCENE_CHECK savedMeshes="+meshes.Length+" originalSprites="+sprites.Length+" screenshot=playmode-preparation.png");
        }
        [UnityTest] public IEnumerator PreparationPointerSwapsOccupiedCellsRejectsEnemyHalfAndLocksAtStart()
        {
            yield return Ready();var m=controller.Model;
            var first=m.Units.First(u=>u.Enabled);var second=m.Units.First(u=>u.Enabled&&u.Slot!=first.Slot);
            Vector2 a=first.Spawn,b=second.Spawn;int enabled=m.Units.Count(u=>u.Enabled);
            Vector2 head=FormationBattleController.CellScreen(first.Spawn)+new Vector2(0,-35);
            controller.ProcessPointer(EventType.MouseDown,head);controller.ProcessPointer(EventType.MouseUp,head);
            Assert.That(first.Spawn,Is.EqualTo(a),"Clicking the visible character head must select without relocating its ground cell.");
            Assert.That(controller.SelectedSlot,Is.EqualTo(first.Slot));
            Drag(a,b);Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            Assert.That(controller.SelectedSlot,Is.EqualTo(first.Slot));
            Drag(first.Spawn,new Vector2(10,4));Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(first.Spawn));
            controller.ProcessPointer(EventType.MouseDrag,new Vector2(640,610));controller.ProcessPointer(EventType.MouseUp,new Vector2(640,610));
            Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(m.Units.Count(u=>u.Enabled),Is.EqualTo(enabled));
            Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(m.Clock,Is.Zero);
            controller.StartBattle();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Fighting));
            first=m.Units.Single(u=>u.Instance==first.Instance);second=m.Units.Single(u=>u.Instance==second.Instance);
            Drag(first.Spawn,second.Spawn);Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            controller.Retry();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));
            Assert.That(m.Units.Single(u=>u.Instance==first.Instance).Spawn,Is.EqualTo(b));
            Assert.That(m.Units.Single(u=>u.Instance==second.Instance).Spawn,Is.EqualTo(a));
        }
        [UnityTest] public IEnumerator PauseFreezesSharedCombatClockAndConfiguredDeadlineUsesRealLossPath()
        {
            yield return Ready();var m=controller.Model;controller.StartBattle();m.Advance(.25f);m.Paused=true;
            double clock=m.Clock,remaining=m.TimeRemaining;
            var positions=m.Units.Select(u=>u.Position).ToArray();var hp=m.Units.Select(u=>u.Health).ToArray();
            var charge=m.Units.Select(u=>u.SkillCharge).ToArray();var revive=m.Units.Select(u=>u.RespawnRemaining).ToArray();
            m.Advance(2);yield return new WaitForSecondsRealtime(.12f);
            Assert.That(m.Clock,Is.EqualTo(clock));Assert.That(m.TimeRemaining,Is.EqualTo(remaining));
            CollectionAssert.AreEqual(positions,m.Units.Select(u=>u.Position));CollectionAssert.AreEqual(hp,m.Units.Select(u=>u.Health));
            CollectionAssert.AreEqual(charge,m.Units.Select(u=>u.SkillCharge));CollectionAssert.AreEqual(revive,m.Units.Select(u=>u.RespawnRemaining));
            controller.Retry();m.LimitSeconds=.1;controller.StartBattle();m.Advance(.2f);
            Assert.That(m.Phase,Is.EqualTo(FormationPhase.Lost),"A deliberately shortened configured limit must use the ordinary timeout transition.");
            Assert.That(m.TimeRemaining,Is.LessThanOrEqualTo(.001));
            controller.Retry();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(m.Clock,Is.Zero);
        }
        [UnityTest] public IEnumerator SavedNativeCourtyardKeepsSourceLayeredTransparentGatesAtFullCellSize()
        {
            yield return Ready();
            var scene=SceneManager.GetActiveScene();
            Assert.That(scene.path,Is.EqualTo("Assets/Scenes/FormationBattle.unity"));
            var roots=scene.GetRootGameObjects();
            var courtyard=roots.FirstOrDefault(r=>r.name=="LS5 native formation courtyard");
            Assert.That(courtyard,Is.Not.Null,"The original LS-5 courtyard must be part of the saved independent scene.");
            Assert.That(courtyard.GetComponentsInChildren<MeshRenderer>(true).Length,Is.GreaterThan(0));
#if UNITY_EDITOR
            Assert.That(courtyard.GetComponentsInChildren<MeshFilter>(true).Any(filter=>filter.sharedMesh!=null&&AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/Art/Environment/FormationNative/",StringComparison.Ordinal)),Is.True,"The saved environment must reference imported LS-5 source geometry.");
#endif
            var transforms=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var red=transforms.Where(t=>t.name.StartsWith("Red gate row ",StringComparison.Ordinal)).ToArray();
            var blue=transforms.Where(t=>t.name.StartsWith("Blue gate row ",StringComparison.Ordinal)).ToArray();
            CollectionAssert.AreEquivalent(Enumerable.Range(0,8).Select(row=>"Red gate row "+row).ToArray(),red.Select(t=>t.name).ToArray());
            CollectionAssert.AreEquivalent(Enumerable.Range(0,8).Select(row=>"Blue gate row "+row).ToArray(),blue.Select(t=>t.name).ToArray());
            for(int row=0;row<8;row++)
            {
                foreach(bool isRed in new[]{true,false})
                {
                    string name=(isRed?"Red":"Blue")+" gate row "+row;
                    var gate=(isRed?red:blue).Single(t=>t.name==name);
                    var expected=FormationBattleController.WorldPoint(new Vector2(isRed?6:13,row));
                    Assert.That(Vector3.Distance(gate.position,expected),Is.LessThan(.0001f),name+" must align with its existing logical row.");
                    Assert.That(gate.localScale,Is.EqualTo(Vector3.one),name+" must keep the source gate's one-cell scale.");
                    Assert.That(gate.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True,name+" must leave passage open without enabled collision barriers.");
                    var renderers=gate.GetComponentsInChildren<MeshRenderer>(true);
                    Assert.That(renderers.Length,Is.EqualTo(4),name+" must retain all four original transparent mesh layers.");
                    // The source back-effect plane extends to z=1.192505; all four layers together
                    // are 1.692505 deep. The actual two frame layers remain a precise one-cell cube.
                    var frames=renderers.Where(r=>r.name.EndsWith("_01_add_01",StringComparison.Ordinal)||r.name.EndsWith("_01_ab_01",StringComparison.Ordinal)).ToArray();
                    Assert.That(frames.Length,Is.EqualTo(2),"Keep both original additive/alpha frame layers.");
                    var frameBounds=IndexedGateBounds(gate,frames);
                    Assert.That(Vector3.Distance(frameBounds.size,Vector3.one),Is.LessThan(.0001f),name+" rendered frame must keep its exact one-cell dimensions.");
                    Assert.That(Mathf.Abs(frameBounds.center.x)+Mathf.Abs(frameBounds.center.z),Is.LessThan(.0001f),"The frame must stay centred over its cell.");
                    foreach(var renderer in renderers)
                    {
                        var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        Assert.That(mesh,Is.Not.Null);Assert.That(mesh.name,Does.StartWith("Gate-"));
                        Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
                        Assert.That(mesh.colors32.Length,Is.EqualTo(0).Or.EqualTo(mesh.vertexCount),"Keep the source colour stream when present; the imported native gate meshes currently have no colour channel.");
                        foreach(var material in renderer.sharedMaterials)
                        {
                            Assert.That(material,Is.Not.Null);Assert.That(material.shader.name,Is.EqualTo("YesterdayConvoy/FormationNativeGate"));
                            Assert.That(material.shader.isSupported,Is.True);
                            Assert.That(material.renderQueue,Is.GreaterThanOrEqualTo(3000));Assert.That(material.mainTexture,Is.Not.Null);
                            Assert.That(material.GetFloat("_DstBlend"),Is.EqualTo(1).Or.EqualTo(10));
                            Assert.That(material.HasProperty("_TintColor"),Is.True,"The board gate must preserve the preview's explicit colour treatment.");
                            var tint=isRed?new Color(.45f,.11f,.13f,.75f):new Color(.22f,.42f,.65f,.65f);
                            Assert.That(Vector4.Distance(material.GetColor("_TintColor"),tint),Is.LessThan(.0001f));
#if UNITY_EDITOR
                            Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith("Assets/Art/Environment/FormationBoard/"),"The formation appearance must be a persistent local material rather than altering the original map's shared material.");
#endif
                        }
                    }
#if UNITY_EDITOR
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/BlackForestNative/NativeForest.prefab");
                    Assert.That(source,Is.Not.Null);
                    var sourceGate=source.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(isRed?"R1 / Native tile_start":"B1 / Native tile_end"));
                    var sourceLayers=sourceGate.GetComponentsInChildren<MeshRenderer>(true);
                    CollectionAssert.AreEquivalent(sourceLayers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh),renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh),"Reuse actual source meshes with original UVs and colour-channel presence; do not invent alpha data for the test.");
                    foreach(var actualLayer in renderers)
                    {
                        var sourceLayer=sourceLayers.Single(r=>r.name==actualLayer.name);
                        Assert.That(actualLayer.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(sourceLayer.GetComponent<MeshFilter>().sharedMesh));
                        Assert.That(actualLayer.sharedMaterials.Length,Is.EqualTo(sourceLayer.sharedMaterials.Length));
                        for(int index=0;index<sourceLayer.sharedMaterials.Length;index++)
                        {
                            var original=sourceLayer.sharedMaterials[index];var adapted=actualLayer.sharedMaterials[index];
                            Assert.That(adapted,Is.Not.SameAs(original),"Appearance changes must not mutate another map's material.");
                            Assert.That(original.shader.name,Is.EqualTo("YesterdayConvoy/NativeGate"));
                            Assert.That(adapted.mainTexture,Is.SameAs(original.mainTexture),"Keep the original gate texture instead of replacing the source geometry with a drawn substitute.");
                            Assert.That(adapted.GetTextureScale("_MainTex"),Is.EqualTo(original.GetTextureScale("_MainTex")));
                            Assert.That(adapted.GetTextureOffset("_MainTex"),Is.EqualTo(original.GetTextureOffset("_MainTex")));
                            Assert.That(adapted.GetFloat("_DstBlend"),Is.EqualTo(original.GetFloat("_DstBlend")));
                        }
                    }
                    var sourceBounds=IndexedGateBounds(sourceGate,sourceLayers);var actualBounds=IndexedGateBounds(gate,renderers);
                    Assert.That(Vector3.Distance(actualBounds.size,sourceBounds.size),Is.LessThan(.0001f),"Retain the original back-effect extent rather than shrinking the complete gate to one cell.");
                    Assert.That(Vector3.Distance(actualBounds.center,sourceBounds.center),Is.LessThan(.0001f),"Preserve the source placement of all four rendered layers.");
#endif
                }
            }
            var tiles=transforms.Where(t=>t.name.StartsWith("Tile ",StringComparison.Ordinal)&&t.gameObject.activeInHierarchy).ToArray();
            Assert.That(tiles.Length,Is.EqualTo(112));
            Assert.That(Vector3.Distance(FormationBattleController.WorldPoint(Vector2.up)-FormationBattleController.WorldPoint(Vector2.zero),Vector3.forward),Is.LessThan(.0001f),"Native cells use unit row spacing, without the previous 0.8 compression.");
            foreach(var tile in tiles)
            {
                var coordinates=tile.name.Substring(5).Split(',');
                var expected=FormationBattleController.WorldPoint(new Vector2(int.Parse(coordinates[0]),int.Parse(coordinates[1])));
                var renderer=tile.GetComponent<MeshRenderer>();Assert.That(renderer,Is.Not.Null);
                Assert.That(Vector2.Distance(new Vector2(renderer.bounds.center.x,renderer.bounds.center.z),new Vector2(expected.x,expected.z)),Is.LessThan(.001f));
                Assert.That(renderer.bounds.max.y,Is.EqualTo(.12f).Within(.001f),"The saved playable board must remain flat at the logical actor feet, even with raised decorative platforms nearby.");
                Assert.That(tile.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
                foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material.shader.name,Is.EqualTo("YesterdayConvoy/FormationNativeSurface"));
                    Assert.That(material.shader.isSupported,Is.True);
                    Assert.That(material.GetTexture("_NativeAmbient"),Is.Not.Null,"The saved material must carry its source RGBM ambient texture into the Player.");
                    Assert.That(material.GetTexture("_NativeReflection"),Is.InstanceOf<Cubemap>(),"The preview reflection must be a saved runtime dependency.");
#if UNITY_EDITOR
                    Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith("Assets/Art/Environment/FormationBoard/"));
                    Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_NativeAmbient")),Does.StartWith("Assets/Art/Environment/FormationNative/"));
                    Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_NativeReflection")),Does.StartWith("Assets/Art/Environment/FormationBoard/"));
#endif
                }
            }
            Assert.That(tiles.Select(t=>t.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_CheckerTint")).Distinct().Count(),Is.GreaterThanOrEqualTo(2),"The saved board must retain its two visible checker tones.");
            var perimeter=courtyard.transform.Find("Original LS-5 raised perimeter");Assert.That(perimeter,Is.Not.Null);
            var raised=perimeter.GetComponentsInChildren<MeshRenderer>(true);Assert.That(raised.Length,Is.EqualTo(44));
            Assert.That(raised.All(r=>OutsideBoard(r.bounds)),Is.True,"Raised decoration must not occupy the flat fighting board or deployment cells.");
            Assert.That(perimeter.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
            Debug.Log("FORMATION_GATE_SCENE_CHECK: 112 flat checker cells, 44 raised perimeter parts, source gate geometry with independent preview materials at columns 6/13.");
        }
        [UnityTest] public IEnumerator RealMarketPurchaseUsesGoldAndBattleLocksTransactions()
        {
            yield return Ready();var shop=controller.Shop;var session=controller.Session;
            Assert.That(shop.Level,Is.EqualTo(3));Assert.That(shop.Gold,Is.EqualTo(12));
            Assert.That(controller.Model.EnabledCount,Is.EqualTo(3));
            Assert.That(controller.ShopPresentation,Is.Not.Null);
            foreach(int seatIndex in new[]{0,3,9})
            {
                Assert.That(FormationBattleController.FormationLineupRect(seatIndex),Is.EqualTo(ManualBattleController.ShopLineupRect(seatIndex)));
                Assert.That(FormationBattleController.FormationReserveRect(seatIndex),Is.EqualTo(ManualBattleController.ShopBenchRect(seatIndex)));
            }
            yield return Market(true);
            int offer=Enumerable.Range(0,5).First(i=>shop.Offers[i]=="CARD_M09"||shop.Offers[i]=="CARD_M02");
            string id=shop.Offers[offer];int cost=shop.Definition(id).Cost;
            var result=controller.BuyOffer(offer);
            Assert.That(result.Success,Is.True);Assert.That(shop.Gold,Is.EqualTo(12-cost));
            Assert.That(shop.Offers[offer],Is.Null);shop.Validate();
            Assert.That(shop.Bench.Any(p=>p.Instance==result.Instance),Is.True);
            Assert.That(controller.ShopArrivalCount,Is.GreaterThan(0),"The shared original recruitment flow must accompany a real purchase.");
            var entering=controller.ShopEntranceModel(result.Instance);Assert.That(entering,Is.Not.Null);
            Assert.That(Vector2.Distance(StagePoint(controller,entering.transform.position),FormationBattleController.FormationOfferRect(offer).center),Is.LessThan(8),"The original standing model must enter from its purchased offer.");
            yield return Market(true);yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(controller.ShopModelTarget,Is.Not.Null);Assert.That(controller.ShopModelTarget.width,Is.EqualTo(1920));Assert.That(controller.ShopModelTarget.height,Is.EqualTo(1080));
            Assert.That(controller.ShopModelCamera.targetTexture,Is.SameAs(controller.ShopModelTarget));
            var standing=controller.ShopBenchModel(result.Instance);Assert.That(standing,Is.Not.Null);Assert.That(standing.sprite,Is.Not.Null);
            Assert.That(standing.gameObject.layer,Is.EqualTo(31));Assert.That(standing.sprite.name,Does.StartWith(id.Substring(5)+"/idle/"));
            var seat=controller.FormationPieceRect(result.Instance);
            Assert.That(Vector2.Distance(StagePoint(controller,standing.transform.position),new Vector2(seat.center.x,seat.y+55)),Is.LessThan(2),"A real actor must stand on the original platform foot position.");
            var initialFrame=standing.sprite;bool animated=false;double idleDeadline=Time.realtimeSinceStartupAsDouble+1.5;
            Time.timeScale=0;
            while(Time.realtimeSinceStartupAsDouble<idleDeadline&&!animated){yield return null;animated=standing.sprite!=initialFrame;}
            Assert.That(animated,Is.True,"The shared stage must play actual idle frames while simulation is paused.");
            Time.timeScale=previousScale;
            var before=controller.Model.Units.ToDictionary(u=>u.Instance,u=>u.Spawn);int coveredGold=shop.Gold;
            var covered=FormationBattleController.FormationOfferRect(0).center;
            controller.ProcessPointer(EventType.MouseDown,covered);controller.ProcessPointer(EventType.MouseUp,covered);
            Assert.That(controller.Model.Units.All(u=>before[u.Instance]==u.Spawn),Is.True,"The original market must own covered pointer events.");
            Assert.That(shop.Gold,Is.EqualTo(coveredGold));
            yield return new WaitForEndOfFrame();
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/formation-shop"));Directory.CreateDirectory(dir);
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Path.Combine(dir,"playmode-market.png"),frame.EncodeToPNG());}finally{Object.Destroy(frame);}
            yield return Market(false);controller.StartBattle();int gold=shop.Gold;
            Assert.That(controller.RefreshMarket().Success,Is.False);Assert.That(controller.BuyExperience().Success,Is.False);
            Assert.That(controller.BuyOffer(Enumerable.Range(0,5).First(i=>shop.Offers[i]!=null)).Success,Is.False);
            Assert.That(controller.SellPiece(result.Instance).Success,Is.False);
            Assert.That(shop.Gold,Is.EqualTo(gold));Assert.That(session.CanShop,Is.False);
        }
        [UnityTest] public IEnumerator PurchasedReservePointerDropIntoUnlockedSeatAddsTheActualFighter()
        {
            yield return Ready();var shop=controller.Shop;
            int offer=Enumerable.Range(0,5).First(i=>shop.Offers[i]=="CARD_M09"||shop.Offers[i]=="CARD_M02");
            var receipt=controller.BuyOffer(offer);Assert.That(receipt.Success,Is.True);
            Assert.That(controller.BuyExperience().Success,Is.True);Assert.That(controller.BuyExperience().Success,Is.True);
            Assert.That(shop.Level,Is.EqualTo(4));Assert.That(controller.Model.EnabledCount,Is.EqualTo(3));
            var seat=controller.Session.Seats.For(receipt.Instance).Value;
            Vector2 from=FormationBattleController.FormationReserveRect(seat.Index).center;
            Vector2 to=FormationBattleController.FormationLineupRect(3).center;
            controller.ProcessPointer(EventType.MouseDown,from);controller.ProcessPointer(EventType.MouseDrag,to);controller.ProcessPointer(EventType.MouseUp,to);
            Assert.That(controller.Session.Seats.InLineup(receipt.Instance),Is.True);
            Assert.That(controller.Model.Units.Count,Is.EqualTo(4));
            Assert.That(controller.Model.Units.Single(u=>u.Instance==receipt.Instance).ArtId,Is.EqualTo(receipt.Id.Substring(5)));
            controller.Shop.Validate();
        }
    }
}
