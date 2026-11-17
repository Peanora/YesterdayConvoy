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
            public string scope="Accelerated replay through the production shop transactions, saved map, pointer handlers and formation combat with actual Player screenshots. One real offer is bought with starting gold. The initial three-person lineup observes its ordinary first encounter. A configured short deadline and legal single-soldier lineup exercise timeout and revival. No HP, damage, skill charge or result is injected. This does not establish human interaction, full campaign economy, all character kits or final visual acceptance.";
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
        static string Formation(FormationBattleModel model)=>string.Join(";",model.Units.Select(u=>$"{u.Slot}:{u.Spawn.x},{u.Spawn.y}:{u.Star}:{u.Enabled}"));
        static string GuardLayout(FormationBattleModel model)=>string.Join(";",model.Defenders.Select(d=>$"{d.ArtId}:{d.Position.x},{d.Position.y}"));
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
            yield return null;yield return null;
            result.scene=SceneManager.GetActiveScene().name;result.width=Screen.width;result.height=Screen.height;
            var m=controller.Model;var terrain=Terrain();var terrainSet=new HashSet<MeshFilter>(terrain);result.savedMeshes=terrain.Length;
            CheckThat("authored-rectangle-has-saved-meshes",terrain.Length>20&&SceneManager.GetActiveScene().path=="Assets/Scenes/FormationBattle.unity");
            var cam=controller.battleCamera;
            CheckThat("calibrated-forward-perspective-camera",cam!=null&&!cam.orthographic&&cam.transform.eulerAngles.x>30&&cam.transform.eulerAngles.x<65&&Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y,0))<.1f&&Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.z,0))<.1f);
            CheckThat("original-character-sprites-visible",Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.sprite!=null&&r.transform.IsChildOf(controller.transform))>=m.Units.Count(u=>u.Enabled)+m.Defenders.Count);
            CheckThat("preparation-clock-is-frozen",m.Phase==FormationPhase.Preparation&&m.Clock==0);
            CheckThat("real-shop-starts-with-finite-gold-and-three-owned-lineup-pieces",controller.Shop.Gold==12&&controller.Shop.Level==3&&controller.Shop.Bench.Count==3&&m.EnabledCount==3);
            controller.SetMarketVisible(true);yield return Capture("player-market");
            int offer=Enumerable.Range(0,5).First(i=>controller.Shop.Offers[i]!=null);
            string purchased=controller.Shop.Offers[offer];int price=controller.Shop.Definition(purchased).Cost;
            var receipt=controller.BuyOffer(offer);
            CheckThat("actual-offer-purchase-consumes-gold-stock-and-offer",receipt.Success&&controller.Shop.Gold==12-price&&controller.Shop.Offers[offer]==null&&controller.Shop.Bench.Any(p=>p.Instance==receipt.Instance));
            controller.Shop.Validate();yield return Capture("player-purchased");controller.SetMarketVisible(false);yield return null;
            yield return Capture("player-preparation");

            var first=m.Units.First(u=>u.Enabled);var second=m.Units.First(u=>u.Enabled&&u.Slot!=first.Slot);
            Vector2 firstSpawn=first.Spawn,secondSpawn=second.Spawn;
            Drag(firstSpawn,secondSpawn);
            CheckThat("pointer-drag-swaps-occupied-cells",first.Spawn==secondSpawn&&second.Spawn==firstSpawn);
            string placed=Formation(m);Drag(first.Spawn,new Vector2(10,4));
            CheckThat("pointer-enemy-half-drop-is-atomic",Formation(m)==placed&&m.Clock==0&&m.Phase==FormationPhase.Preparation);
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(first.Spawn));
            controller.ProcessPointer(EventType.MouseDrag,new Vector2(640,650));controller.ProcessPointer(EventType.MouseUp,new Vector2(640,650));
            CheckThat("pointer-hud-drop-keeps-placement",Formation(m)==placed);
            Drag(first.Spawn,second.Spawn);
            CheckThat("pointer-swap-can-be-reversed",first.Spawn==firstSpawn&&second.Spawn==secondSpawn);
            yield return Capture("player-arrangement");

            string formation=Formation(m);controller.StartBattle();
            CheckThat("production-start-enters-fighting",m.Phase==FormationPhase.Fighting&&m.Units.Count(u=>u.Enabled&&u.Alive)>0);
            int battleGold=controller.Shop.Gold;
            CheckThat("battle-rejects-shop-purchase-refresh-and-xp",!controller.BuyOffer(1).Success&&!controller.RefreshMarket().Success&&!controller.BuyExperience().Success&&controller.Shop.Gold==battleGold);
            Drag(first.Spawn,second.Spawn);CheckThat("battle-pointer-cannot-rearrange",Formation(m)==formation);
            m.Advance(.1f);m.Paused=true;double clock=m.Clock,remaining=m.TimeRemaining;
            var positions=m.Units.Select(u=>u.Position).ToArray();var hp=m.Units.Select(u=>u.Health).ToArray();var sp=m.Units.Select(u=>u.SkillCharge).ToArray();
            m.Advance(2);yield return new WaitForSecondsRealtime(.12f);
            CheckThat("pause-freezes-clock-countdown-positions-hp-and-sp",m.Clock==clock&&m.TimeRemaining==remaining&&m.Units.Select(u=>u.Position).SequenceEqual(positions)&&m.Units.Select(u=>u.Health).SequenceEqual(hp)&&m.Units.Select(u=>u.SkillCharge).SequenceEqual(sp));
            yield return Capture("player-paused");m.Paused=false;
            bool battleFrame=false,skillFrame=false;int steps=0;
            while((m.Phase==FormationPhase.Fighting||m.Phase==FormationPhase.Exiting)&&steps++<12000)
            {
                m.Paused=false;m.Advance(.05f);
                if(!battleFrame&&m.Clock>=3){battleFrame=true;yield return Capture("player-battle");}
                if(!skillFrame&&m.Hits.Any(h=>h.Skill&&!h.Heal)){skillFrame=true;yield return Capture("player-skill");}
                if(steps%60==0)yield return null;
            }
            CheckThat("default-first-encounter-finishes-without-injected-result",m.Phase==FormationPhase.Won||m.Phase==FormationPhase.Lost);
            CheckThat("default-first-encounter-wins",m.Phase==FormationPhase.Won);
            CheckThat("automatic-production-combat-causes-real-damage",m.Units.Sum(u=>u.DamageDone)>0&&m.Defenders.All(d=>!d.Alive));
            yield return Capture("player-victory");

            int round=m.Round;int beforeReward=controller.Shop.Gold;controller.NextRound();controller.SetMarketVisible(false);
            CheckThat("next-round-keeps-formation",m.Round==round+1&&m.Phase==FormationPhase.Preparation&&Formation(m)==formation);
            CheckThat("won-round-grants-real-shop-reward-once",controller.Shop.Gold>beforeReward);
            string guards=GuardLayout(m);controller.StartBattle();m.Advance(1);controller.Retry();
            CheckThat("same-round-retry-keeps-defender-layout-and-formation",m.Phase==FormationPhase.Preparation&&m.Clock==0&&GuardLayout(m)==guards&&Formation(m)==formation);
            CheckThat("retry-preserves-authored-terrain-instances",terrainSet.SetEquals(Terrain()));
            yield return Capture("player-next-preparation");

            // Explicitly shorter authored configuration exercises the real deadline, not a forced result.
            m.LimitSeconds=.1;controller.StartBattle();m.Advance(.2f);
            CheckThat("configured-short-deadline-produces-real-loss",m.Phase==FormationPhase.Lost&&m.TimeRemaining<=.001);
            yield return Capture("player-timeout-configured");

            controller.ResetDemo();m=controller.Model;controller.SetMarketVisible(false);
            int soldierInstance=m.Units.Single(u=>u.ArtId=="M07").Instance;
            foreach(var unit in m.Units.Where(u=>u.Instance!=soldierInstance).ToArray())
                CheckThat("legal-bench-move-"+unit.ArtId,controller.MovePiece(unit.Instance,false,controller.Session.Seats.FirstEmpty(false,10)).Success);
            controller.StartBattle();var soldier=m.Units.Single(u=>u.Instance==soldierInstance);steps=0;
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
