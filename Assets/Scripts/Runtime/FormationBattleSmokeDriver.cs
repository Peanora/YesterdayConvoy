using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Opt-in verification only. Ordinary play never creates this component.
    public sealed class FormationBattleSmokeDriver : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name; public bool passed; }
        [Serializable] public sealed class Snapshot
        {
            public string name,phase;
            public int round,aliveGuards,deaths,reviving,skillHits;
            public double clock,remaining;
            public float damage;
        }
        [Serializable] public sealed class Result
        {
            public bool passed;
            public string error,scene;
            public string scope="Accelerated replay through production finite-stock shop transactions, shared original A shop presentation and standing-model stage, saved warm grey concrete board, transparent native gates, guard platforms and cover, left preparation and right scouting/battle cameras, real drag preview and terrain-aware formation combat with actual Player screenshots. An affordable new kind is selected from the actual offers, using paid refresh only if needed. The initial three-person lineup observes its ordinary first encounter; the purchased reserve later joins through a real population upgrade and pointer drop. Current roster animation packages are awaited before combat. A configured short deadline and legal single-soldier lineup exercise timeout and revival. No HP, damage, skill charge, guard position or result is injected. This does not establish human interaction, full campaign economy, all catalogue character kits, exact original-game appearance or final visual acceptance.";
            public bool acceleratedReplay=true;
            public int width,height,assertions,exceptionCount,savedMeshes;
            public string[] exceptions;
            public Check[] checks;
            public Snapshot[] checkpoints;
        }

        static bool started;
        readonly List<Check> checks=new List<Check>();
        readonly List<Snapshot> snapshots=new List<Snapshot>();
        readonly List<string> exceptions=new List<string>();
        readonly Result result=new Result();
        FormationBattleController controller;
        int recruitOffer;
        string directory;
        bool finished;

        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationSmokeTest")>=0;
        public static string EvidenceDirectory()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationEvidenceDir");
            return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.GetFullPath(Path.Combine("artifacts","formation-demo"));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetLaunch()=>started=false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Prepare(){if(Requested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(started||!Requested)return;
            started=true;var host=new GameObject("Formation Player verification");DontDestroyOnLoad(host);host.AddComponent<FormationBattleSmokeDriver>();
        }
        void Awake()=>Application.logMessageReceived+=ObserveLog;
        void OnDestroy()=>Application.logMessageReceived-=ObserveLog;
        void ObserveLog(string message,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)
                exceptions.Add(type+": "+message+"\n"+stack);
        }
        IEnumerator Start()
        {
            directory=EvidenceDirectory();
            return Guard(Verify());
        }
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0&&!finished)
            {
                object yielded=null;bool move=false;Exception failure=null;
                try
                {
                    if(exceptions.Count>0)throw new InvalidOperationException("Unexpected runtime error: "+exceptions[0]);
                    move=stack.Peek().MoveNext();if(move)yielded=stack.Peek().Current;
                }
                catch(Exception e){failure=e;}
                if(failure!=null){Finish(failure.ToString());yield break;}
                if(!move){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(yielded is IEnumerator nested){stack.Push(nested);continue;}
                yield return yielded;
            }
            if(!finished)Finish(null);
        }
        void CheckThat(string name,bool passed)
        {
            checks.Add(new Check{name=name,passed=passed});Debug.Log("FORMATION_CHECK "+name+"="+passed);
            if(!passed)throw new InvalidOperationException("Formation verification failed: "+name);
        }
        void SnapshotAt(string name)
        {
            var m=controller.Model;
            snapshots.Add(new Snapshot{name=name,phase=m.Phase.ToString(),round=m.Round,clock=m.Clock,remaining=m.TimeRemaining,
                aliveGuards=m.Defenders.Count(d=>d.Alive),deaths=m.Units.Sum(u=>u.Deaths),reviving=m.Units.Count(u=>u.Enabled&&!u.Alive),
                skillHits=m.Hits.Count(h=>h.Skill&&!h.Heal),damage=m.Units.Sum(u=>u.DamageDone)});
        }
        IEnumerator Capture(string name)
        {
            controller.SyncActors();
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null)throw new InvalidOperationException("Visible graphics capture unavailable: "+name);
            try
            {
                var pixels=frame.GetPixels32();int samples=0,visible=0;byte lo=255,hi=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {
                    byte value=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));
                    lo=Math.Min(lo,value);hi=Math.Max(hi,value);samples++;if(value>24)visible++;
                }
                CheckThat("visible-frame-"+name,frame.width==Screen.width&&frame.height==Screen.height&&visible>samples/20&&hi-lo>16);
                Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),frame.EncodeToPNG());
            }
            finally{Destroy(frame);}
            SnapshotAt(name);yield return null;
        }
        static MeshFilter[] Terrain()=>Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(m=>m.gameObject.scene==SceneManager.GetActiveScene()&&m.sharedMesh!=null).ToArray();
        static bool OutsideBoard(Bounds bounds)=>bounds.max.x<=-7+.01f||bounds.min.x>=7-.01f||
            bounds.max.z<=-4+.01f||bounds.min.z>=4-.01f;
        static bool FlatBoardCell(Transform tile)
        {
            var coordinates=tile.name.Substring(5).Split(',');
            if(coordinates.Length!=2||!int.TryParse(coordinates[0],out int column)||!int.TryParse(coordinates[1],out int row))return false;
            var expected=FormationBattleController.WorldPoint(new Vector2(column,row));var renderer=tile.GetComponent<MeshRenderer>();
            if(renderer==null||Vector2.Distance(new Vector2(renderer.bounds.center.x,renderer.bounds.center.z),new Vector2(expected.x,expected.z))>.001f||Mathf.Abs(renderer.bounds.max.y-.12f)>.001f||tile.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))return false;
            return renderer.sharedMaterials.Length>0&&renderer.sharedMaterials.All(material=>material!=null&&material.shader.name=="YesterdayConvoy/FormationNativeSurface"&&material.shader.isSupported&&material.HasProperty("_ConcreteMode")&&material.GetFloat("_ConcreteMode")>.5f&&material.mainTexture!=null&&material.mainTexture.name=="city-concrete");
        }
        bool SavedDefenderTerrain()
        {
            var root=SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(go=>go.name=="Defender terrain");
            if(root==null||controller.terrain==null||!controller.terrain.transform.IsChildOf(root.transform)||controller.Model.Terrain==null)return false;
            var regions=controller.terrain.regions;
            var footprints=new[]{new RectInt(11,1,2,2),new RectInt(11,5,2,2),new RectInt(8,1,2,1),new RectInt(9,4,2,1)};
            if(regions==null||regions.Length!=footprints.Length||!footprints.All(cells=>regions.Count(r=>r.cells==cells)==1))return false;
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            foreach(var region in regions)
            {
                bool platform=region.cells.height==2;
                if(region.allowDefender!=platform||Mathf.Abs(region.height-(platform ? .48f : .36f))>.001f)return false;
                var cell=new Vector2(region.cells.xMin,region.cells.yMin);var foot=controller.ActorWorldPoint(cell);
                if(Mathf.Abs(controller.Model.Terrain.HeightAt(cell)-region.height)>.001f||controller.Model.Terrain.CanPlaceDefender(cell)!=region.allowDefender||
                    !renderers.Any(r=>foot.x>=r.bounds.min.x-.01f&&foot.x<=r.bounds.max.x+.01f&&foot.z>=r.bounds.min.z-.01f&&foot.z<=r.bounds.max.z+.01f&&Mathf.Abs(r.bounds.max.y-foot.y)<.02f))return false;
            }
            return controller.Model.Terrain.CanTravel(new Vector2(6,3),new Vector2(13,3))&&controller.Model.Defenders.All(d=>controller.Model.Terrain.CanPlaceDefender(d.Position));
        }
        bool ActorFeetMatchTerrain()
        {
            var bodies=controller.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&r.gameObject.layer!=31).ToArray();
            var actors=controller.ViewingDefenders?controller.Model.Defenders.Where(d=>d.Alive).Cast<FormationActor>():controller.Model.Units.Where(u=>u.Enabled&&u.Alive).Cast<FormationActor>();
            var safe=FormationBattleController.FormationCameraSafeRect;
            return actors.All(actor=>
            {
                var foot=controller.ActorWorldPoint(actor);
                return bodies.Any(r=>r.name==actor.Name&&Vector3.Distance(r.transform.position,foot)<.001f)&&Mathf.Abs(foot.y-(.12f+controller.Model.Terrain.HeightAt(actor.Position)))<.001f&&
                    new[]{foot,foot+controller.battleCamera.transform.up*controller.ActorPresentationHeight(actor)}.All(world=>
                    {var projected=controller.battleCamera.WorldToScreenPoint(world);var point=TacticalViewport.Current.FromScreen(projected);return projected.z>0&&point.x>=safe.xMin-1&&point.x<=safe.xMax+1&&point.y>=safe.yMin-1&&point.y<=safe.yMax+1;});
            });
        }
        static string Formation(FormationBattleModel model)=>string.Join(";",model.Units.OrderBy(u=>u.Instance).Select(u=>$"{u.Instance}:{u.ArtId}:{u.Spawn.x},{u.Spawn.y}:{u.Star}:{u.Enabled}"));
        static string GuardLayout(FormationBattleModel model)=>string.Join(";",model.Defenders.Select(d=>$"{d.ArtId}:{d.Position.x},{d.Position.y}"));
        IEnumerator Market(bool visible)
        {
            controller.SetMarketVisible(visible);double deadline=Time.realtimeSinceStartupAsDouble+3;
            while(controller.ShopPanelTransitioning&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(controller.ShopPanelTransitioning)throw new InvalidOperationException("The shared original shop visibility transition did not finish.");
            yield return null;
        }
        IEnumerator RosterReady()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(!controller.RosterArtReady&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(!controller.RosterArtReady)throw new InvalidOperationException("The actual current roster animation packages did not finish preparing.");
        }
        IEnumerator View(bool defenders)
        {
            controller.SetFormationView(defenders);double deadline=Time.realtimeSinceStartupAsDouble+3;
            while(controller.CameraTransitioning&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(controller.CameraTransitioning||controller.ViewingDefenders!=defenders)throw new InvalidOperationException("The focused camera did not reach the requested half.");
            yield return null;
        }
        IEnumerator ChooseRecruit()
        {
            for(int attempt=0;attempt<3;attempt++)
            {
                var owned=controller.Session.Seats.Lineup(controller.Shop.Bench).Select(p=>p.Id).ToArray();
                recruitOffer=Enumerable.Range(0,5).Where(i=>controller.Shop.Offers[i]!=null&&!owned.Contains(controller.Shop.Offers[i])&&controller.Shop.Definition(controller.Shop.Offers[i]).Cost<=controller.Shop.Gold).DefaultIfEmpty(-1).First();
                if(recruitOffer>=0)yield break;
                CheckThat("paid-refresh-to-find-new-kind-"+attempt,controller.Shop.Gold>=3&&controller.RefreshMarket().Success);yield return null;
            }
            throw new InvalidOperationException("The actual finite shop did not offer an affordable new kind within the paid refresh budget.");
        }
        Vector2 StagePoint(Vector3 world)
        {
            var point=controller.ShopModelCamera.WorldToViewportPoint(world);
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
        static bool NativeGate(Transform gate,Vector2 cell)
        {
            if(gate==null||Vector3.Distance(gate.position,FormationBattleController.WorldPoint(cell))>.0001f||gate.localScale!=Vector3.one||gate.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))return false;
            var layers=gate.GetComponentsInChildren<MeshRenderer>(true);if(layers.Length!=4)return false;
            var frames=layers.Where(r=>r.name.EndsWith("_01_add_01",StringComparison.Ordinal)||r.name.EndsWith("_01_ab_01",StringComparison.Ordinal)).ToArray();
            if(frames.Length!=2)return false;
            // The fourth original transparent back layer extends beyond the cell. Check the actual
            // indexed frame geometry, not the combined FX bounds, without rescaling either.
            var frameBounds=IndexedGateBounds(gate,frames);
            if(Vector3.Distance(frameBounds.size,Vector3.one)>.0001f||Mathf.Abs(frameBounds.center.x)+Mathf.Abs(frameBounds.center.z)>.0001f)return false;
            foreach(var layer in layers)
            {
                var mesh=layer.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null||!mesh.name.StartsWith("Gate-",StringComparison.Ordinal)||!mesh.isReadable||mesh.uv.Length!=mesh.vertexCount||mesh.colors32.Length!=0&&mesh.colors32.Length!=mesh.vertexCount)return false;
                foreach(var material in layer.sharedMaterials)
                {
                    if(material==null||material.shader.name!="YesterdayConvoy/FormationNativeGate"||!material.shader.isSupported||material.renderQueue<3000||material.mainTexture==null||
                        !material.mainTexture.name.StartsWith("Gate-",StringComparison.Ordinal)||material.GetFloat("_DstBlend")!=1&&material.GetFloat("_DstBlend")!=10||!material.HasProperty("_TintColor"))return false;
                    var tint=cell.x==6?new Color(.45f,.11f,.13f,.60f):new Color(.22f,.42f,.65f,.35f);
                    if(Vector4.Distance(material.GetColor("_TintColor"),tint)>.0001f)return false;
                }
            }
            return true;
        }
        void Drag(Vector2 from,Vector2 to)
        {
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(from));
            controller.ProcessPointer(EventType.MouseDrag,FormationBattleController.CellScreen(to));
            controller.ProcessPointer(EventType.MouseUp,FormationBattleController.CellScreen(to));
        }
        IEnumerator Verify()
        {
            Directory.CreateDirectory(directory);double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<FormationBattleController>();
                if(controller!=null&&controller.Ready&&controller.Model!=null&&SceneManager.GetActiveScene().name==FormationBattleController.SceneName)break;
                yield return null;
            }
            CheckThat("ordinary-startup-reaches-ready-scene",controller!=null&&controller.Ready&&controller.Model!=null&&SceneManager.GetActiveScene().name==FormationBattleController.SceneName);
            yield return RosterReady();yield return View(false);yield return null;
            result.scene=SceneManager.GetActiveScene().name;result.width=Screen.width;result.height=Screen.height;
            var m=controller.Model;var terrain=Terrain();var terrainSet=new HashSet<MeshFilter>(terrain);result.savedMeshes=terrain.Length;
            CheckThat("authored-rectangle-has-saved-meshes",terrain.Length>20&&SceneManager.GetActiveScene().path=="Assets/Scenes/FormationBattle.unity");
            var cam=controller.battleCamera;
            CheckThat("native-map-keeps-full-virtual-camera-and-forward-view",cam!=null&&cam.rect==TacticalViewport.Current.CameraRect(new Rect(0,0,1280,720))&&Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y,0))<.1f&&Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.z,0))<.1f);
            var nativeRoot=SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(go=>go.name=="LS5 native formation courtyard");
            CheckThat("saved-native-ls5-root-and-112-unit-spaced-tiles",nativeRoot!=null&&nativeRoot.GetComponentsInChildren<MeshFilter>(true).Any(filter=>filter.sharedMesh!=null&&filter.sharedMesh.name.Contains(" / source "))&&SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).Count(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Tile ",StringComparison.Ordinal))==112&&Vector3.Distance(FormationBattleController.WorldPoint(Vector2.up)-FormationBattleController.WorldPoint(Vector2.zero),Vector3.forward)<.0001f);
            var nativeTransforms=nativeRoot.GetComponentsInChildren<Transform>(true);
            CheckThat("source-native-gates-keep-four-transparent-full-cell-layers",Enumerable.Range(0,8).All(row=>NativeGate(nativeTransforms.SingleOrDefault(t=>t.name=="Red gate row "+row),new Vector2(6,row))&&NativeGate(nativeTransforms.SingleOrDefault(t=>t.name=="Blue gate row "+row),new Vector2(13,row))));
            var boardCells=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray();
            CheckThat("saved-base-board-uses-matte-concrete-and-persistent-texture",boardCells.Length==112&&boardCells.All(FlatBoardCell));
            var perimeter=nativeTransforms.SingleOrDefault(t=>t.name=="Original LS-5 raised perimeter");
            CheckThat("saved-raised-perimeter-leaves-fighting-board-clear",perimeter!=null&&perimeter.GetComponentsInChildren<MeshRenderer>(true).Length==44&&perimeter.GetComponentsInChildren<MeshRenderer>(true).All(r=>OutsideBoard(r.bounds))&&!perimeter.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled));
            CheckThat("saved-guard-platforms-and-cover-match-combat-layout",SavedDefenderTerrain());
            CheckThat("original-character-sprites-visible",Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.sprite!=null&&r.gameObject.layer!=31&&r.transform.IsChildOf(controller.transform))>=m.Units.Count(u=>u.Enabled)+m.Defenders.Count);
            CheckThat("left-preparation-shows-complete-owned-army",!controller.ViewingDefenders&&controller.CanEditFormationView&&ActorFeetMatchTerrain());
            CheckThat("preparation-clock-is-frozen",m.Phase==FormationPhase.Preparation&&m.Clock==0);
            CheckThat("real-shop-starts-with-finite-gold-and-three-owned-lineup-pieces",controller.Shop.Gold==12&&controller.Shop.Level==3&&controller.Shop.Bench.Count==3&&m.EnabledCount==3);
            CheckThat("shared-original-A-presentation-and-platform-geometry",controller.ShopPresentation!=null&&new[]{0,3,9}.All(i=>FormationBattleController.FormationLineupRect(i)==ManualBattleController.ShopLineupRect(i)&&FormationBattleController.FormationReserveRect(i)==ManualBattleController.ShopBenchRect(i)));
            yield return Market(true);yield return Capture("player-market");
            var beforeCovered=m.Units.ToDictionary(u=>u.Instance,u=>u.Spawn);int coveredGold=controller.Shop.Gold;
            var covered=FormationBattleController.FormationOfferRect(0).center;
            controller.ProcessPointer(EventType.MouseDown,covered);controller.ProcessPointer(EventType.MouseUp,covered);
            CheckThat("original-A-market-blocks-map-pointer-through",m.Units.All(u=>beforeCovered[u.Instance]==u.Spawn)&&controller.Shop.Gold==coveredGold);
            yield return ChooseRecruit();int offer=recruitOffer;
            string purchased=controller.Shop.Offers[offer];int price=controller.Shop.Definition(purchased).Cost;
            int beforePurchase=controller.Shop.Gold;
            var receipt=controller.BuyOffer(offer);
            CheckThat("actual-offer-purchase-consumes-gold-stock-and-offer",receipt.Success&&controller.Shop.Gold==beforePurchase-price&&controller.Shop.Offers[offer]==null&&controller.Shop.Bench.Any(p=>p.Instance==receipt.Instance));
            var entering=controller.ShopEntranceModel(receipt.Instance);
            CheckThat("original-recruitment-standing-model-enters-from-offer",controller.ShopArrivalCount>0&&entering!=null&&controller.ShopModelCamera!=null&&Vector2.Distance(StagePoint(entering.transform.position),FormationBattleController.FormationOfferRect(offer).center)<8);
            controller.Shop.Validate();yield return new WaitForSecondsRealtime(1.1f);
            var standing=controller.ShopBenchModel(receipt.Instance);string purchasedArt=FormationUnitCatalog.Find(purchased).ArtId;
            double standingDeadline=Time.realtimeSinceStartupAsDouble+600;
            while((standing==null||standing.sprite==null||!standing.sprite.name.StartsWith(purchasedArt+"/idle/",StringComparison.Ordinal))&&Time.realtimeSinceStartupAsDouble<standingDeadline)
            {yield return null;standing=controller.ShopBenchModel(receipt.Instance);}
            var standingRect=controller.FormationPieceRect(receipt.Instance);
            CheckThat("original-standing-model-stage-and-platform-foot",controller.ShopModelTarget!=null&&controller.ShopModelTarget.width==1920&&controller.ShopModelTarget.height==1080&&controller.ShopModelCamera.targetTexture==controller.ShopModelTarget&&standing!=null&&standing.sprite!=null&&standing.gameObject.layer==31&&standing.sprite.name.StartsWith(purchasedArt+"/idle/",StringComparison.Ordinal)&&Vector2.Distance(StagePoint(standing.transform.position),new Vector2(standingRect.center.x,standingRect.y+55))<2);
            var idle=standing.sprite;bool idleChanged=false;double idleDeadline=Time.realtimeSinceStartupAsDouble+1.5;float oldScale=Time.timeScale;Time.timeScale=0;
            while(!idleChanged&&Time.realtimeSinceStartupAsDouble<idleDeadline){yield return null;idleChanged=standing.sprite!=idle;}
            Time.timeScale=oldScale;CheckThat("standing-stage-plays-real-idle-during-simulation-pause",idleChanged);
            yield return Capture("player-purchased");yield return Market(false);yield return View(false);
            yield return Capture("player-preparation");

            var preparationSpawns=m.Units.Select(u=>u.Spawn).ToArray();double scoutClock=m.Clock;
            var shopCameraPose=controller.ShopModelCamera.transform.position;
            yield return View(true);
            CheckThat("right-scouting-shows-guards-without-changing-preparation",controller.ViewingDefenders&&!controller.CanEditFormationView&&ActorFeetMatchTerrain()&&m.Clock==scoutClock&&m.Units.Select(u=>u.Spawn).SequenceEqual(preparationSpawns)&&controller.ShopModelCamera.transform.position==shopCameraPose);
            yield return Capture("player-defender-scouting");yield return View(false);

            var first=m.Units.Single(u=>u.Enabled&&u.ArtId=="M07");var second=m.Units.Single(u=>u.Enabled&&u.ArtId=="M08");
            int firstInstance=first.Instance,secondInstance=second.Instance;
            Vector2 firstSpawn=first.Spawn,secondSpawn=second.Spawn;
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(firstSpawn));
            controller.ProcessPointer(EventType.MouseDrag,FormationBattleController.CellScreen(secondSpawn));yield return null;
            CheckThat("pointer-preview-shows-real-idle-ghost-and-swap-footprint",controller.DragPreviewVisible&&controller.DeploymentPreview.Valid&&controller.DeploymentPreview.SwapInstance==secondInstance&&controller.DeploymentPreview.Cells==new RectInt(Vector2Int.RoundToInt(secondSpawn),first.Footprint)&&controller.DeploymentGhost!=null&&controller.DeploymentGhost.sprite!=null&&controller.DeploymentGhost.sprite.name.StartsWith(first.ArtId+"/idle/",StringComparison.Ordinal)&&controller.DeploymentGhost.color.a>0&&controller.DeploymentGhost.color.a<1&&m.Units.Single(u=>u.Instance==firstInstance).Spawn==firstSpawn);
            yield return Capture("player-deployment-preview");
            controller.ProcessPointer(EventType.MouseUp,FormationBattleController.CellScreen(secondSpawn));
            first=m.Units.Single(u=>u.Instance==firstInstance);second=m.Units.Single(u=>u.Instance==secondInstance);
            CheckThat("pointer-drag-swaps-occupied-cells",first.Spawn==secondSpawn&&second.Spawn==firstSpawn);
            string placed=Formation(m);Drag(first.Spawn,new Vector2(10,4));
            CheckThat("pointer-enemy-half-drop-is-atomic",Formation(m)==placed&&m.Clock==0&&m.Phase==FormationPhase.Preparation);
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(first.Spawn));
            controller.ProcessPointer(EventType.MouseDrag,new Vector2(640,610));controller.ProcessPointer(EventType.MouseUp,new Vector2(640,610));
            CheckThat("pointer-hud-drop-keeps-placement",Formation(m)==placed);
            Drag(first.Spawn,second.Spawn);
            first=m.Units.Single(u=>u.Instance==firstInstance);second=m.Units.Single(u=>u.Instance==secondInstance);
            CheckThat("pointer-swap-can-be-reversed",first.Spawn==firstSpawn&&second.Spawn==secondSpawn);
            yield return Capture("player-arrangement");

            yield return RosterReady();string formation=Formation(m);controller.StartBattle();
            CheckThat("production-start-enters-fighting",m.Phase==FormationPhase.Fighting&&m.Units.Count(u=>u.Enabled&&u.Alive)>0);
            CheckThat("production-battle-selects-right-camera",controller.ViewingDefenders&&!controller.CanEditFormationView);
            int battleGold=controller.Shop.Gold;
            CheckThat("battle-rejects-shop-purchase-refresh-and-xp",!controller.BuyOffer(1).Success&&!controller.RefreshMarket().Success&&!controller.BuyExperience().Success&&controller.Shop.Gold==battleGold);
            Drag(first.Spawn,second.Spawn);CheckThat("battle-pointer-cannot-rearrange",Formation(m)==formation);
            m.Advance(.1f);m.Paused=true;double clock=m.Clock,remaining=m.TimeRemaining;
            var positions=m.Units.Select(u=>u.Position).ToArray();var hp=m.Units.Select(u=>u.Health).ToArray();var sp=m.Units.Select(u=>u.SkillCharge).ToArray();
            m.Advance(2);yield return new WaitForSecondsRealtime(.12f);
            CheckThat("pause-freezes-clock-countdown-positions-hp-and-sp",m.Clock==clock&&m.TimeRemaining==remaining&&m.Units.Select(u=>u.Position).SequenceEqual(positions)&&m.Units.Select(u=>u.Health).SequenceEqual(hp)&&m.Units.Select(u=>u.SkillCharge).SequenceEqual(sp));
            yield return View(true);yield return Capture("player-paused");m.Paused=false;
            bool battleFrame=false,skillFrame=false,clearOfTerrain=true;int steps=0;
            while((m.Phase==FormationPhase.Fighting||m.Phase==FormationPhase.Exiting)&&steps++<12000)
            {
                var beforeStep=m.Units.Where(u=>u.Enabled&&u.Alive).ToDictionary(u=>u.Id,u=>u.Position);
                m.Paused=false;m.Advance(.025f);
                clearOfTerrain&=m.Units.Where(u=>u.Enabled&&u.Alive).All(u=>m.Terrain.IsWalkable(u.Position)&&(!beforeStep.TryGetValue(u.Id,out var before)||m.Terrain.CanTravel(before,u.Position)));
                if(!battleFrame&&m.Clock>=3){battleFrame=true;yield return Capture("player-battle");}
                if(!skillFrame&&m.Hits.Any(h=>h.Skill&&!h.Heal)){skillFrame=true;yield return Capture("player-skill");}
                if(steps%60==0)yield return null;
            }
            CheckThat("default-first-encounter-finishes-without-injected-result",m.Phase==FormationPhase.Won||m.Phase==FormationPhase.Lost);
            CheckThat("default-first-encounter-wins",m.Phase==FormationPhase.Won);
            CheckThat("automatic-production-combat-causes-real-damage",m.Units.Sum(u=>u.DamageDone)>0&&m.Defenders.All(d=>!d.Alive));
            CheckThat("production-fighters-go-around-saved-platforms-and-cover",clearOfTerrain);
            yield return Capture("player-victory");

            int round=m.Round;int beforeReward=controller.Shop.Gold;controller.NextRound();yield return Market(false);yield return RosterReady();yield return View(false);
            CheckThat("next-round-keeps-formation",m.Round==round+1&&m.Phase==FormationPhase.Preparation&&Formation(m)==formation);
            CheckThat("next-round-returns-camera-to-left-preparation",!controller.ViewingDefenders&&controller.CanEditFormationView);
            CheckThat("won-round-grants-real-shop-reward-once",controller.Shop.Gold>beforeReward);
            CheckThat("real-experience-purchase-unlocks-fourth-population",controller.BuyExperience().Success&&controller.Shop.Level>=4);
            var reserve=controller.FormationPieceRect(receipt.Instance).center;var lineup=FormationBattleController.FormationLineupRect(3).center;
            controller.ProcessPointer(EventType.MouseDown,reserve);controller.ProcessPointer(EventType.MouseDrag,lineup);controller.ProcessPointer(EventType.MouseUp,lineup);
            CheckThat("actual-purchased-reserve-drops-on-original-platform-and-enters-lineup",controller.Session.Seats.InLineup(receipt.Instance)&&m.Units.Any(u=>u.Instance==receipt.Instance&&u.ArtId==FormationUnitCatalog.Find(receipt.Id).ArtId)&&m.EnabledCount==4);
            var deploymentCells=m.Units.Where(u=>u.Enabled).Select(u=>u.DeploymentCells).ToArray();
            CheckThat("recruited-lineup-keeps-complete-footprints-in-bounds-without-overlap",deploymentCells.All(cells=>cells.xMin>=0&&cells.xMax<=FormationBattleModel.DeploymentColumns&&cells.yMin>=0&&cells.yMax<=FormationBattleModel.Height)&&!deploymentCells.SelectMany((cells,i)=>deploymentCells.Skip(i+1).Select(other=>cells.Overlaps(other))).Any(overlap=>overlap));
            formation=Formation(m);controller.Shop.Validate();
            yield return RosterReady();string guards=GuardLayout(m);controller.StartBattle();m.Advance(1);controller.Retry();yield return View(false);
            CheckThat("same-round-retry-keeps-defender-layout-and-formation",m.Phase==FormationPhase.Preparation&&m.Clock==0&&GuardLayout(m)==guards&&Formation(m)==formation);
            CheckThat("retry-preserves-authored-terrain-instances",terrainSet.SetEquals(Terrain()));
            yield return Capture("player-next-preparation");

            // Explicitly shorter authored configuration exercises the real deadline, not a forced result.
            yield return RosterReady();m.LimitSeconds=.1;controller.StartBattle();m.Advance(.2f);
            CheckThat("configured-short-deadline-produces-real-loss",m.Phase==FormationPhase.Lost&&m.TimeRemaining<=.001);
            yield return Capture("player-timeout-configured");

            controller.ResetDemo();m=controller.Model;yield return Market(false);yield return View(false);yield return RosterReady();
            int soldierInstance=m.Units.Single(u=>u.ArtId=="M07").Instance;
            foreach(var unit in m.Units.Where(u=>u.Instance!=soldierInstance).ToArray())
                CheckThat("legal-bench-move-"+unit.ArtId,controller.MovePiece(unit.Instance,false,controller.Session.Seats.FirstEmpty(false,10)).Success);
            yield return RosterReady();controller.StartBattle();var soldier=m.Units.Single(u=>u.Instance==soldierInstance);steps=0;
            while(soldier.Deaths==0&&m.Phase==FormationPhase.Fighting&&steps++<12000)
            {m.Paused=false;m.Advance(.025f);if(steps%60==0)yield return null;}
            CheckThat("legal-single-soldier-lineup-naturally-dies",soldier.Deaths>0&&!soldier.Alive&&soldier.RespawnRemaining>0);
            m.Paused=true;clock=m.Clock;remaining=m.TimeRemaining;float revive=soldier.RespawnRemaining;
            m.Advance(2);yield return new WaitForSecondsRealtime(.12f);
            CheckThat("pause-freezes-real-revival-countdown",m.Clock==clock&&m.TimeRemaining==remaining&&soldier.RespawnRemaining==revive&&!soldier.Alive);
            yield return Capture("player-revival-paused");m.Paused=false;steps=0;
            while(!soldier.Alive&&m.Phase==FormationPhase.Fighting&&steps++<2000)
            {m.Paused=false;m.Advance(.025f);if(steps%60==0)yield return null;}
            CheckThat("natural-revival-returns-near-saved-deployment-point",soldier.Alive&&soldier.Deaths>0&&Vector2.Distance(soldier.Position,soldier.Spawn)<.25f&&soldier.Health>0);
            yield return Capture("player-revived");
            CheckThat("no-unexpected-runtime-errors",exceptions.Count==0);
            controller.ResetDemo();CheckThat("new-challenge-resets-round-clock-and-deaths",controller.Model.Round==1&&controller.Model.Phase==FormationPhase.Preparation&&controller.Model.Clock==0&&controller.Model.Units.All(u=>u.Deaths==0));
        }
        void Finish(string error)
        {
            if(finished)return;finished=true;result.error=error;result.assertions=checks.Count;result.exceptionCount=exceptions.Count;
            result.exceptions=exceptions.ToArray();result.checks=checks.ToArray();result.checkpoints=snapshots.ToArray();
            result.passed=error==null&&exceptions.Count==0&&checks.Count>0&&checks.All(c=>c.passed);
            try{Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"formation-smoke.json"),JsonUtility.ToJson(result,true));}
            catch(Exception e){result.passed=false;Debug.Log("FORMATION_RESULT_WRITE_FAILED "+e);}
            Debug.Log("FORMATION_SMOKE_RESULT passed="+result.passed+" assertions="+checks.Count+" exceptions="+exceptions.Count+" error="+error);
            Application.Quit(result.passed?0:2);
        }
    }
}
