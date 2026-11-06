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
        [Serializable]sealed class HerdSpiritResult {public bool passed;public string[] checks;public string deathSource;}
        static DemoSpriteVisual HerdEvidenceVisual(string id)=>GameObject.Find(id)?.GetComponentInChildren<DemoSpriteVisual>(true);
        static float HerdEvidenceTime(DemoSpriteVisual visual)
        {var properties=new MaterialPropertyBlock();visual.Renderer.GetPropertyBlock(properties);return properties.GetFloat("_SpiritTime");}
        void HerdEvidenceAdvance(double at)
        {while(Battle.Clock+.00001<at&&Battle.Phase==BattlePhase.Running&&!Battle.IsPaused)Tick((float)Math.Min(.025,at-Battle.Clock));}
        IEnumerator HerdEvidenceFrame(string directory,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image==null)throw new InvalidOperationException("Herd spirit capture requires a visible graphics Player.");
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());Destroy(image);yield return null;
        }
        static int HerdEvidencePixels(SpriteRenderer renderer)
        {
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(640,360,24);var image=new Texture2D(640,360,TextureFormat.RGB24,false);
            var material=renderer.sharedMaterial;var ordinary=new Material(Shader.Find("Sprites/Default"));
            Color32[] Capture(){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();}
            try
            {
                camera.targetTexture=target;var blue=Capture();renderer.sharedMaterial=ordinary;var original=Capture();int changed=0;
                for(int i=0;i<blue.Length;i++)if(Math.Abs(blue[i].r-original[i].r)+Math.Abs(blue[i].g-original[i].g)+Math.Abs(blue[i].b-original[i].b)>18)changed++;
                return changed;
            }
            finally{renderer.sharedMaterial=material;camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(image);Destroy(ordinary);}
        }
        IEnumerator HerdEvidenceMotion(string directory)
        {
            if(!StartLoopNextBattle())throw new InvalidOperationException("Motion capture could not enter the saved trial.");
            Battle.Begin();Battle.SetPaused(false);
            var receipt=Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Entry,"CARD_M21");
            if(!receipt.Success)throw new InvalidOperationException("Motion capture deployment failed.");
            var source=Battle.FindUnit(receipt.EntityId);Tick(.01f);yield return null;
            for(int i=0;i<1800&&source.Active;i++){Tick(.05f);if(i%5==0)yield return null;}
            Tick(.001f);var spirit=Battle.HerdSpiritFrom(source.EntityId);
            if(spirit==null)throw new InvalidOperationException("Natural motion capture death did not convert.");
            string frames=Path.Combine(directory,"motion");Directory.CreateDirectory(frames);
            for(int frame=0;frame<91;frame++)
            {HerdEvidenceAdvance(spirit.StartedAt+frame/30.0);yield return HerdEvidenceFrame(frames,frame.ToString("D4"));}
            Debug.Log("HERD_SPIRIT_MOTION_FRAMES=91; fps=30; source=actual Player simulation frames; audio=none");
        }
        internal IEnumerator VerifyHerdSpirit(string directory)
        {
            diagnostic=true;Application.runInBackground=true;Directory.CreateDirectory(directory);
            var checks=new List<string>();bool passed=true,complete=false;float previous=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("HERD_SPIRIT_EVENT "+name+"="+value);};
            try
            {
                Time.timeScale=0;check("savedHerdTrialReady",SynergySandbox&&Battle.HerdTier==4&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
                if(!StartLoopNextBattle())throw new InvalidOperationException("Herd trial could not enter combat.");Battle.Begin();Battle.SetPaused(false);
                var deployment=Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Entry,"CARD_M21");check("normalM21Deployment",deployment.Success);
                var source=Battle.FindUnit(deployment.EntityId);Tick(.01f);yield return HerdEvidenceFrame(directory,"01-original");
                for(int tick=0;tick<1800&&source.Active;tick++){Tick(.05f);if(tick%5==0)yield return null;}
                check("naturalDefenderFinalDeath",source.Resolution==UnitResolution.Dead&&source.Health<=0);
                Tick(.001f);yield return null;var state=Battle.HerdSpiritFrom(source.EntityId);
                if(state==null)throw new InvalidOperationException("Actual defender kill did not produce a linked herd spirit.");
                var host=Battle.FindUnit(state.EntityId);var original=HerdEvidenceVisual(source.EntityId);var spirit=HerdEvidenceVisual(host.EntityId);
                if(original==null||spirit==null)throw new InvalidOperationException("Source and spirit actors were not both created.");
                Vector3 position=host.Motion.Position;int attacks=host.OrdinaryAttacksFired;
                check("sameIdentityAndRoute",host.CardId==source.CardId&&host.RouteId==source.RouteId&&host.Motion.Position==source.Motion.Position);
                HerdEvidenceAdvance(state.StartedAt+.22);yield return null;
                check("oneVisibleBodyWhileFalling",original.FrameState=="HerdFall"&&original.Renderer.enabled&&spirit.FrameState=="HerdForming"&&!spirit.Renderer.enabled);
                yield return HerdEvidenceFrame(directory,"02-falling");
                HerdEvidenceAdvance(state.StartedAt+.85);yield return null;
                check("blueRiseReplacesOldBody",original.FrameState=="HerdGone"&&!original.Renderer.enabled&&spirit.FrameState=="HerdRise"&&spirit.Renderer.enabled&&spirit.Renderer.sharedMaterial.shader.name=="YesterdayConvoy/Herd Spirit");
                var effect=GameObject.Find(host.EntityId).GetComponentInChildren<HerdSpiritVisual>(true);
                check("riseLocksMovementAttackAndDamage",host.IsReviving&&host.Motion.Position==position&&host.OrdinaryAttacksFired==attacks&&!Battle.TryDamageUnit(host.EntityId,100000,CombatDamageType.Physical));
                yield return HerdEvidenceFrame(directory,"03-rising");
                Battle.SetPaused(true);double clock=Battle.Clock;int frame=spirit.FrameIndex;float shaderTime=HerdEvidenceTime(spirit);
                var mesh=effect.MistRenderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;Tick(3);yield return null;
                check("pauseFreezesRiseShaderAndMist",Battle.Clock==clock&&spirit.FrameIndex==frame&&HerdEvidenceTime(spirit)==shaderTime&&vertices.SequenceEqual(mesh.vertices));
                Battle.SetPaused(false);HerdEvidenceAdvance(state.ReadyAt+.08);yield return null;
                check("readyResumesOriginalActionsAndKeepsFullLifetime",!host.IsReviving&&(host.IsMoving||host.HasCombatTarget)&&Battle.HerdRemaining(host.EntityId)>11.8);
                check("spectralBodyAndMistVisible",effect.Opacity>0&&effect.MistRenderer.enabled&&spirit.Renderer.enabled);
                int pixels=HerdEvidencePixels(spirit.Renderer);check("actualGpuBodyDiffersFromOriginal",pixels>20);Debug.Log("HERD_SPIRIT_GPU_CHANGED_PIXELS="+pixels);
                yield return HerdEvidenceFrame(directory,"04-active-spirit");
                // Explicit final-damage fixture isolates the dissolve after the natural conversion above.
                float opacity=effect.Opacity;check("activeSpiritCanBeDefeated",Battle.TryDamageUnit(host.EntityId,100000,CombatDamageType.Physical));Tick(.18f);yield return null;
                check("finalDeathDissolvesWithoutConvertingAgain",effect.Opacity<opacity&&Battle.HerdUsed==1);
                yield return HerdEvidenceFrame(directory,"05-dissolving");
                Battle.SetPaused(true);float paused=effect.Opacity;Tick(3);yield return null;check("pauseFreezesDissolve",effect.Opacity==paused);
                Battle.SetPaused(false);Tick(1);yield return null;yield return null;
                check("dissolveReleasesActor",GameObject.Find(host.EntityId)==null);yield return HerdEvidenceFrame(directory,"06-dissolved");
                check("normalEncounterRestart",RestartCampaignEncounter());while(ArtLoading)yield return null;yield return null;
                check("restartClearsBodiesAndConversionLinks",ViewCount==0&&Battle.Units.Count==0&&Battle.HerdSpiritFrom(source.EntityId)==null&&FindObjectsByType<HerdSpiritVisual>(FindObjectsSortMode.None).Length==0);
                yield return HerdEvidenceFrame(directory,"07-restarted");
                if(Environment.GetCommandLineArgs().Contains("-herdSpiritMotionEvidence"))yield return HerdEvidenceMotion(directory);
                complete=true;
            }
            finally
            {
                Time.timeScale=previous;var result=new HerdSpiritResult{passed=passed&&complete,checks=checks.ToArray(),deathSource="First conversion: actual defender damage. Dissolve: explicit fatal-damage fixture."};
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("HERD_SPIRIT_PLAYER_CHECK passed="+result.passed);
                Application.Quit(result.passed?0:1);
            }
        }
    }

    public sealed class HerdSpiritSmokeDriver:MonoBehaviour
    {
        static bool started;
        [Serializable]sealed class Failure {public bool passed;public string error;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-herdSpiritSmokeTest"))return;
            started=true;var go=new GameObject("Herd spirit Player verification");DontDestroyOnLoad(go);go.AddComponent<HerdSpiritSmokeDriver>();
        }
        IEnumerator LoadAndVerify(string directory)
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("HerdTrial");
            double deadline=Time.realtimeSinceStartupAsDouble+600;ManualBattleController controller=null;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.CampaignActive&&controller.Loop!=null&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||!controller.Loop.CanShop||controller.ArtLoading)throw new InvalidOperationException("HerdTrial startup timed out.");
            yield return controller.VerifyHerdSpirit(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/herd-spirit-player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(LoadAndVerify(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());
                    if(!File.Exists(Path.Combine(directory,"result.json")))File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new Failure{passed=false,error=failure.ToString()},true));
                    Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
        }
    }
}
