using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [Serializable] sealed class SnowstormEvidenceResult {public bool passed;public int changedPixels;public string[] checks;}
        IEnumerator ChargeStormForEvidence(long request,string directory=null)
        {
            if(!SetSandboxRoster(0)||!StartLoopNextBattle())throw new InvalidOperationException("Cannot enter the five-member snow lineup.");
            while(ArtLoading||ShopPanelTransitioning)yield return null;
            Battle.Begin();
            // Explicit development fixture: keep one durable target by the entry, disable guard attacks,
            // and supply cold. Real ordinary projectiles from two different cards must cause all three shatters.
            var target=Battle.Defenders.First(d=>d.Alive);
            foreach(var guard in Battle.Defenders)
            {guard.Health=guard.MaxHealth=1000000;guard.NextAttackTime=Battle.Clock+1000;guard.SkillSP=-1000;guard.Position=Battle.Entry+Vector3.forward*100;}
            target.Position=Battle.Entry+Vector3.forward*.8f;
            foreach(string card in new[]{"CARD_M11","CARD_M12"})
            {
                var receipt=Battle.TryDeployRole(Battle.RunId,request++,Battle.Entry,card);
                if(!receipt.Success)throw new InvalidOperationException("Snowstorm fixture deployment failed: "+card);
            }
            for(int n=1;n<=3;n++)
            {
                for(int i=0;i<600&&Battle.Shatters<n;i++)
                {
                    if(target.FrozenUntil<=Battle.Clock){Battle.TryApplyCold(target.EntityId,10);Battle.TryApplyCold(target.EntityId,10);}
                    Tick(.05f);if(i%10==0)yield return null;
                }
                if(Battle.Shatters!=n||Battle.SnowstormCharge!=(n<3?n:0)||Battle.SnowstormActive!=(n==3))
                    throw new InvalidOperationException("Three-shatter trigger/progress did not match at "+n);
                if(n==2&&directory!=null)
                {
                    yield return null;yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory,"charge-two-of-three.png"));yield return null;yield return null;
                }
            }
            Tick(.45f);yield return null;yield return null;
        }
        internal IEnumerator VerifySnowstormWeather(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var checks=new List<string>();bool passed=true,complete=false;int changed=0;float scale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("SNOWSTORM_EVENT "+name+"="+value);};
            try
            {
                yield return new WaitForSecondsRealtime(2.5f);Time.timeScale=0;
                yield return ChargeStormForEvidence(60000,directory);
                var weather=GetComponent<SnowstormView>();
                var renderers=GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Snowstorm /",StringComparison.Ordinal)).ToArray();
                check("threeShattersWithoutDeployedFrostNova",Battle.SnowTier==5&&Battle.Shatters==3&&Battle.SnowstormActive&&Battle.SnowstormPulses.Count==1&&Battle.Units.All(u=>u.CardId!="CARD_M14"));
                check("chargeClearedAtWeatherStart",Battle.SnowstormCharge==0);
                check("fourBatchedWeatherLayers",weather.VisibleLayers==4&&renderers.Length==4&&renderers.All(r=>r.sharedMaterial.shader.isSupported));
                check("actualWeatherNotice",SnowstormHudActive&&SnowstormColdPulses==1);
                Battle.SetPaused(true);double clock=Battle.Clock,remaining=Battle.SnowstormRemaining;float health=Battle.Units[0].Health;
                var snow=renderers.Single(r=>r.name=="Snowstorm / snow").GetComponent<MeshFilter>().sharedMesh;var vertices=snow.vertices;
                Tick(2);yield return null;
                check("pauseFreezesSnowAndCountdown",Battle.Clock==clock&&Battle.SnowstormRemaining==remaining&&snow.vertices.SequenceEqual(vertices));
                var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
                var target=new RenderTexture(640,360,24);var a=new Texture2D(640,360,TextureFormat.RGB24,false);var b=new Texture2D(640,360,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture=target;camera.Render();RenderTexture.active=target;a.ReadPixels(new Rect(0,0,640,360),0,0);a.Apply();
                    foreach(var r in renderers)r.enabled=false;
                    camera.Render();b.ReadPixels(new Rect(0,0,640,360),0,0);b.Apply();
                    var x=a.GetPixels32();var y=b.GetPixels32();for(int i=0;i<x.Length;i++)if(Math.Abs(x[i].r-y[i].r)+Math.Abs(x[i].g-y[i].g)+Math.Abs(x[i].b-y[i].b)>12)changed++;
                    File.WriteAllBytes(Path.Combine(directory,"weather-camera.png"),a.EncodeToPNG());File.WriteAllBytes(Path.Combine(directory,"without-weather-camera.png"),b.EncodeToPNG());
                }
                finally
                {foreach(var r in renderers)r.enabled=true;camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(a);Destroy(b);}
                check("gpuWeatherActuallyVisible",changed>500);check("presentationDoesNotApplyDamage",Battle.Clock==clock&&Battle.Units[0].Health==health);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"weather-start-paused.png"));yield return null;yield return null;
                Battle.SetPaused(false);Tick(.17f);yield return null;
                check("resumeMovesSnow",!snow.vertices.SequenceEqual(vertices));
                ToggleSpeed();clock=Battle.Clock;Tick(.1f);yield return null;
                check("doubleSpeedUsesSameClock",Math.Abs(Battle.Clock-clock-.2)<.001);ToggleSpeed();
                check("weatherDoesNotAccumulateCharge",Battle.SnowstormCharge==0);
                Tick(2);yield return null;yield return null;
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"weather-running.png"));yield return null;yield return null;
                loopTestSettlementOpen=true;check("explicitEarlySettlementFixture",TryTestSettlement(false));
                clock=Battle.Clock;Tick(.7f);yield return null;yield return null;
                check("terminalClearsWeatherAndNotice",weather.VisibleLayers==0&&!SnowstormHudActive&&!SnowstormHudVisible&&Battle.Clock==clock);
                ReplaySandbox();yield return null;yield return null;
                check("replayClearsWeatherState",Battle.SnowstormPulses.Count==0&&double.IsNegativeInfinity(Battle.SnowstormStartedAt)&&weather.VisibleLayers==0);
                yield return ChargeStormForEvidence(61000);
                Tick(8.4f);yield return null;yield return null;
                check("eightSecondsFourPulsesAndTail",Battle.SnowstormPulses.Count==4&&!Battle.SnowstormActive&&weather.VisibleLayers==0);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"weather-ended.png"));yield return null;yield return null;
                complete=true;
            }
            finally
            {
                Time.timeScale=scale;passed&=complete;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new SnowstormEvidenceResult{passed=passed,changedPixels=changed,checks=checks.ToArray()},true));
                Debug.Log("SNOWSTORM_PLAYER_CHECK passed="+passed+"; pixels="+changed);Application.Quit(passed?0:1);
            }
        }
    }
    public sealed class SnowstormSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-snowstormSmokeTest"))return;
            started=true;var go=new GameObject("Snowstorm explicit Player verification");DontDestroyOnLoad(go);go.AddComponent<SnowstormSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("SnowTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++){controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/snowstorm-player");
            if(controller==null)throw new InvalidOperationException("Snowstorm trial did not load.");
            yield return controller.VerifySnowstormWeather(dir);
        }
    }
}
