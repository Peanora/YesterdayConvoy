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
    /// <summary>
    /// Explicit development-Player observation of the normal Map05 entry and deployed units.
    /// Never loads a scene, creates terrain, grants SP/HP, injects damage or forces a result.
    /// </summary>
    public sealed class HeatExchangeV5Evidence : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name;public bool passed; }
        [Serializable] public sealed class Result
        {
            public bool passed,castObserved,shotObserved,heatWarningObserved,heatActiveObserved;
            public string error,scene,graphics,naturalOutcome;
            public string scope="Normal -approvedMap 5 startup, saved V5 geometry, production start/pointer deployment using the existing finite starter cards, natural D27 skill and actual original FX GPU difference, pause and encounter restart. Programmatic input is not OS mouse input. No scene load, SP/HP changes, forced target/skill/damage/result, or natural campaign/balance claim.";
            public int width,height,savedTiles,deployments,pramanixCasts,pramanixSnowHits,nativeFxRenderers,gpuChangedPixels,flowGpuChangedPixels;
            public double simulationSeconds;
            public Check[] checks;
        }
        const string MapId="approved-map-05",SceneName="Map05HeatExchange";
        static bool installed;
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-heatExchangeV5SmokeTest")>=0;
        static string DirectoryPath()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-heatExchangeV5EvidenceDir");
            return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.Combine(Application.persistentDataPath,"HeatExchangeV5Evidence");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare(){if(Requested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(installed||!Requested||!Debug.isDebugBuild)return;installed=true;
            var host=new GameObject("Explicit heat exchange V5 Player verification");DontDestroyOnLoad(host);host.AddComponent<HeatExchangeV5Evidence>();
        }
        static SpriteRenderer[] NativeFx()=>Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null&&r.sprite.name.StartsWith("OriginalVfx/defender/D27/",StringComparison.Ordinal)).ToArray();
        static string FrameState(IEnumerable<SpriteRenderer> renderers)=>string.Join("|",renderers.OrderBy(r=>r.GetEntityId().ToString()).Select(r=>
            r.GetEntityId()+"/"+r.enabled+"/"+(r.sprite==null?"none":r.sprite.GetEntityId().ToString())+"/"+r.transform.position+"/"+r.transform.rotation+"/"+r.transform.lossyScale));
        static IEnumerator Capture(string directory,string name,Action<string,bool> check)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null)throw new InvalidOperationException("No actual Player frame: "+name);
            try
            {
                var pixels=frame.GetPixels32();int samples=0,bright=0;byte lo=255,hi=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {byte v=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));lo=Math.Min(lo,v);hi=Math.Max(hi,v);samples++;if(v>24)bright++;}
                check("visible-player-frame-"+name,frame.width>0&&frame.height>0&&bright>samples/20&&hi-lo>12);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),frame.EncodeToPNG());
            }
            finally { Destroy(frame); }
            yield return null;
        }
        static int CompareNativeGpu(SpriteRenderer[] fx,string directory,string label="03-native-fx")
        {
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(960,540,24);var a=new Texture2D(960,540,TextureFormat.RGB24,false);var b=new Texture2D(960,540,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;a.ReadPixels(new Rect(0,0,960,540),0,0);a.Apply();
                foreach(var renderer in fx)renderer.enabled=false;
                camera.Render();b.ReadPixels(new Rect(0,0,960,540),0,0);b.Apply();
                var first=a.GetPixels32();var second=b.GetPixels32();int changed=0;
                for(int i=0;i<first.Length;i++)if(Math.Abs(first[i].r-second[i].r)+Math.Abs(first[i].g-second[i].g)+Math.Abs(first[i].b-second[i].b)>12)changed++;
                File.WriteAllBytes(Path.Combine(directory,label+"-camera.png"),a.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(directory,label+"-without-camera.png"),b.EncodeToPNG());return changed;
            }
            finally
            {
                foreach(var renderer in fx)if(renderer!=null)renderer.enabled=true;
                camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(a);Destroy(b);
            }
        }
        IEnumerator Observe(Result result,string directory,Action<string,bool> check)
        {
            var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-approvedMap");
            if(arg<0||arg+1>=args.Length||args[arg+1]!="5")throw new InvalidOperationException("Use -approvedMap 5 with -heatExchangeV5SmokeTest; this observer does not load the map itself.");
            ManualBattleController c=null;yield return ApprovedMapEvidenceProbe.Ready(MapId,value=>c=value);Time.timeScale=0;
            var map=c.ApprovedMap;var battle=c.Battle;var d=battle.Defenders.Single(g=>g.Settings.ArtId=="D27");
            var terrain=Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None);var saved=new HashSet<ApprovedMapTile>(terrain);
            string source=JsonUtility.ToJson(map),encounter=JsonUtility.ToJson(map.encounter);
            result.scene=SceneManager.GetActiveScene().name;result.savedTiles=terrain.Length;
            check("ordinary-map-five-route-and-saved-terrain",result.scene==SceneName&&map.id==MapId&&terrain.Length==140&&c.ApprovedMapActive);
            check("original-four-and-pramanix-at-approved-small-platform",battle.Defenders.Count==5&&d.Position==new Vector3(-4.5f,.72f,1.5f)&&d.Settings.BlockCount==0);
            check("unchanged-normal-starting-cards-and-resources",battle.DP==50&&battle.Defense==6&&ApprovedMapEvidenceProbe.StockConserved(c)&&ApprovedMapEvidenceProbe.NormalProfiles(c));
            check("original-two-routes-and-three-heat-pumps",battle.Lanes.Count==2&&battle.Lanes[0].Id=="R1"&&battle.Lanes[1].Id=="R2"&&battle.ApprovedHeatPumpCount==3);
            yield return Capture(directory,"01-preparation",check);
            check("production-start-battle",c.StartLoopNextBattle());yield return ApprovedMapEvidenceProbe.Ready(MapId,value=>c=value);
            battle=c.Battle;d=battle.Defenders.Single(g=>g.Settings.ArtId=="D27");
            var pieces=c.Shop.Bench.Where(p=>battle.FindCard(p.Id)!=null).ToArray();
            if(pieces.Length==0)throw new InvalidOperationException("No actual prepared starter cards.");
            var first=battle.FindCard(pieces[0].Id);int dp=battle.DP,uses=first.RemainingUses;
            check("production-pointer-deployment",ApprovedMapEvidenceProbe.Drag(c,pieces[0].Id,battle.Lanes[0].Entry));
            check("normal-deployment-cost-and-finite-use",battle.DP==dp-first.Settings.DeploymentCost&&first.RemainingUses==uses-1);
            battle.SetPaused(true);double clock=battle.Clock;float sp=d.SkillSP;var positions=battle.Units.Select(u=>u.Motion.Position).ToArray();
            c.Tick(5);yield return null;
            check("pause-keeps-clock-sp-and-deployed-position",battle.Clock==clock&&d.SkillSP==sp&&positions.SequenceEqual(battle.Units.Select(u=>u.Motion.Position)));
            yield return Capture(directory,"02-paused-deployment",check);
            battle.SetPaused(false);if(c.SimulationSpeed!=1)c.ToggleSpeed();
            // A finite ordinary policy: additional starters are deployed only through legal
            // pointer input, to R1, without skills or attack/HP/SP overrides.
            var attempted=new HashSet<string>{pieces[0].Id};bool capturedHeat=false;
            for(int step=0;step<2400&&battle.Phase==BattlePhase.Running;step++)
            {
                if(step%30==0)
                {
                    foreach(var piece in pieces)
                    {
                        if(attempted.Contains(piece.Id))continue;
                        var card=battle.FindCard(piece.Id);
                        if(card==null||card.RemainingUses<=0||battle.DP<card.Settings.DeploymentCost)continue;
                        if(ApprovedMapEvidenceProbe.Drag(c,piece.Id,battle.Lanes[0].Entry))attempted.Add(piece.Id);
                        break;
                    }
                }
                c.Tick(.05f);yield return null;
                result.heatWarningObserved|=Enumerable.Range(0,3).Any(i=>battle.ApprovedHeatState(i)==1);
                result.heatActiveObserved|=Enumerable.Range(0,3).Any(i=>battle.ApprovedHeatState(i)==2);
                if(!capturedHeat&&result.heatActiveObserved){capturedHeat=true;yield return Capture(directory,"02-heat-pump-active",check);}
                result.castObserved|=d.SkillCasts>0;
                result.shotObserved|=battle.CombatShots.Any(s=>s.FromDefender&&s.SourceId==d.EntityId&&s.Empowered&&s.Applied);
                var fx=NativeFx();
                if(!result.castObserved||!result.shotObserved||fx.Length==0)continue;
                battle.SetPaused(true);yield return null;fx=NativeFx();
                if(fx.Length==0){battle.SetPaused(false);continue;}
                result.nativeFxRenderers=fx.Length;clock=battle.Clock;sp=d.SkillSP;
                var health=battle.Units.Select(u=>u.Health).ToArray();
                var body=Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(d.EntityId);
                var observed=body==null?fx:fx.Concat(new[]{body}).ToArray();string frozen=FrameState(observed);
                var heat=Enumerable.Range(0,3).Select(battle.ApprovedHeatState).ToArray();
                c.Tick(8);yield return null;yield return null;
                check("natural-pramanix-skill-and-launched-empowered-shot",result.castObserved&&result.shotObserved);
                check("pause-freezes-native-frames-body-sp-hp-and-heat",battle.Clock==clock&&d.SkillSP==sp&&FrameState(observed)==frozen&&
                    health.SequenceEqual(battle.Units.Select(u=>u.Health))&&heat.SequenceEqual(Enumerable.Range(0,3).Select(battle.ApprovedHeatState)));
                result.gpuChangedPixels=CompareNativeGpu(fx,directory);
                check("actual-original-fx-change-gpu-pixels",result.gpuChangedPixels>20);
                var flow=fx.Where(r=>r.name.EndsWith("/sbell2_skill_03_flow",StringComparison.Ordinal)).ToArray();
                check("one-owner-flow-at-real-skill-age",flow.Length==1&&flow[0].sprite==OriginalCombatVfxArt.Get("defender/D27/sbell2_skill_03_flow").At(clock-d.SkillStartedAt,true));
                result.flowGpuChangedPixels=CompareNativeGpu(flow,directory,"03-flow");
                check("approved-looping-flow-reaches-gpu",result.flowGpuChangedPixels>20);
                check("gpu-observation-does-not-advance-or-damage-battle",battle.Clock==clock&&health.SequenceEqual(battle.Units.Select(u=>u.Health)));
                yield return Capture(directory,"03-pramanix-original-skill",check);break;
            }
            result.simulationSeconds=battle.Clock;result.naturalOutcome=battle.Phase.ToString();
            result.deployments=battle.Units.Count(u=>!u.SynergySupport);result.pramanixCasts=d.SkillCasts;result.pramanixSnowHits=d.SnowHits;
            check("natural-d27-cast-observed-with-native-fx",result.castObserved&&result.shotObserved&&result.nativeFxRenderers>0);
            check("heat-pump-warning-and-active-observed",result.heatWarningObserved&&result.heatActiveObserved);
            check("production-encounter-restart",c.RestartCampaignEncounter());yield return ApprovedMapEvidenceProbe.Ready(MapId,value=>c=value);
            var reset=c.Battle;var fresh=reset.Defenders.Single(g=>g.Settings.ArtId=="D27");
            check("restart-clears-clock-units-d27-skill-and-native-fx",reset.Clock==0&&reset.Units.Count==0&&fresh.SkillCasts==0&&
                fresh.SkillSP==fresh.Settings.Skill.InitialSP&&NativeFx().Length==0);
            check("restart-keeps-saved-terrain-and-source-configurations",saved.SetEquals(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None))&&
                source==JsonUtility.ToJson(map)&&encounter==JsonUtility.ToJson(map.encounter));
            check("restart-resets-three-pump-phases",reset.ApprovedHeatPumpCount==3&&reset.ApprovedHeatState(0)==1&&reset.ApprovedHeatState(1)==0&&reset.ApprovedHeatState(2)==0);
            yield return Capture(directory,"04-restarted-map",check);
        }
        IEnumerator Start()
        {
            string directory=DirectoryPath();Directory.CreateDirectory(directory);
            var result=new Result{width=Screen.width,height=Screen.height,graphics=SystemInfo.graphicsDeviceType.ToString()};
            var checks=new List<Check>();Action<string,bool> check=(name,ok)=>{checks.Add(new Check{name=name,passed=ok});Debug.Log("HEAT_EXCHANGE_V5_CHECK "+name+"="+ok);};
            float previous=Time.timeScale;var stack=new Stack<IEnumerator>();stack.Push(Observe(result,directory,check));
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null)
                {
                    result.error=failure.ToString();Debug.LogException(failure);
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            Time.timeScale=previous;result.checks=checks.ToArray();result.passed=string.IsNullOrEmpty(result.error)&&checks.Count>0&&checks.All(c=>c.passed);
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("HEAT_EXCHANGE_V5_RESULT passed="+result.passed+"; checks="+checks.Count+"; cast="+result.castObserved+"; nativePixels="+result.gpuChangedPixels);
            Application.Quit(result.passed?0:1);
        }
    }
}
