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
        [Serializable] sealed class NativeVfxResult { public bool passed; public int changedPixels; public string[] checks; }
        internal IEnumerator VerifyNativeVfx(string id,string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var checks=new List<string>();bool passed=true,complete=false;int changed=0;float scale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("NATIVE_VFX_EVENT "+name+"="+value);};
            try
            {
                Time.timeScale=0;
                var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);
                if(data==null)throw new InvalidOperationException("Missing native VFX trial: "+id);
                yield return Prepare(Plan(data.defenders,new[]{data.battle.Card},data.Tactical),()=>TryEnterDefenderSkillTrial(id),"准备原特效切片");
                var probe=Instantiate(data);probe.name="Explicit centered native VFX fixture";probe.mapId="";
                var shift=new Vector3(0,.35f,0)-probe.defenders.First(d=>d.id==id).position;
                foreach(var placement in probe.defenders)placement.position+=shift;
                probe.route=new[]{new Vector3(-5.8f,.35f,0),new Vector3(5.8f,.35f,0)};
                defenderTrial=probe;
                var baseline=Definition.Snapshot();var combat=Definition.Engagement.Snapshot();
                var unitSettings=new ManualBattleSettings(baseline.CardId,baseline.UnitId,100,baseline.InitialDefense,1,baseline.DeploymentCost,baseline.Cooldown,
                    100000,baseline.Speed,baseline.GateDamage,baseline.TowerRange,baseline.TowerDamage,baseline.ShotInterval);
                ClearViews();Battle=new ManualBattle(unitSettings,probe.route,probe.defenders[0].position,combat,roster:probe.Snapshot());EnableCombatView();
                yield return new WaitForSecondsRealtime(2.5f);
                SelectCard(true);check("durableFixtureDeployed",TryDeployAt(Battle.Entry).Success);
                var defender=Battle.FindDefender(id);check("originalDirectorOwnsRole",DefenderVfxArt.SupportsDefender(defender));
                SpriteRenderer[] native=null;
                Func<bool> actualEvent=()=>id=="D22"
                    ? defender.SkillCasts>0&&Battle.DefenderWallActive(defender)&&Battle.Clock>defender.SkillActionUntil+.65
                    : Battle.CombatShots.Any(s=>s.FromDefender&&s.SourceId==id&&Battle.Clock-s.StartedAt>.45&&Battle.Clock-s.StartedAt<.9);
                for(int i=0;i<4000;i++)
                {
                    Tick(.05f);yield return null;
                    native=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.sprite!=null&&r.sharedMaterial!=null&&
                        r.sharedMaterial.name.StartsWith("Original combat effects / HDR baked",StringComparison.Ordinal)&&
                        r.sprite.name.StartsWith("OriginalVfx/defender/"+id+"/",StringComparison.Ordinal)).ToArray();
                    if(native.Length>0&&actualEvent())break;
                }
                check("realEventAndNativeSprites",native!=null&&native.Length>0&&actualEvent());
                check("nativeShaderAndUnitScale",native!=null&&native.Length>0&&native.All(r=>r.sharedMaterial.shader.isSupported&&r.transform.localScale==Vector3.one));
                Battle.SetPaused(true);double clock=Battle.Clock;float health=Battle.Units[0].Health;Tick(2);yield return null;
                check("pauseFreezesGameplay",Battle.Clock==clock&&Battle.Units[0].Health==health);
                var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
                var target=new RenderTexture(640,360,24);var withFx=new Texture2D(640,360,TextureFormat.RGB24,false);var withoutFx=new Texture2D(640,360,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture=target;camera.Render();RenderTexture.active=target;withFx.ReadPixels(new Rect(0,0,640,360),0,0);withFx.Apply();
                    foreach(var renderer in native)renderer.enabled=false;
                    camera.Render();withoutFx.ReadPixels(new Rect(0,0,640,360),0,0);withoutFx.Apply();
                    var first=withFx.GetPixels32();var second=withoutFx.GetPixels32();
                    for(int i=0;i<first.Length;i++)if(Math.Abs(first[i].r-second[i].r)+Math.Abs(first[i].g-second[i].g)+Math.Abs(first[i].b-second[i].b)>12)changed++;
                    File.WriteAllBytes(Path.Combine(directory,id+"-native-camera.png"),withFx.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(directory,id+"-without-native-camera.png"),withoutFx.EncodeToPNG());
                }
                finally
                {
                    foreach(var renderer in native)if(renderer!=null)renderer.enabled=true;
                    camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(withFx);Destroy(withoutFx);
                }
                check("gpuPixelsActuallyChange",changed>20);check("renderingDoesNotApplyDamage",Battle.Clock==clock&&Battle.Units[0].Health==health);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,id+"-native-full.png"));yield return null;yield return null;
                RestartBattle();Tick(0);yield return null;yield return null;
                check("restartClearsUnits",Battle.Units.Count==0&&Battle.Clock==0);complete=true;
            }
            finally
            {
                Time.timeScale=scale;passed&=complete;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new NativeVfxResult{passed=passed,changedPixels=changed,checks=checks.ToArray()},true));
                Debug.Log("NATIVE_VFX_CHECK passed="+passed+"; pixels="+changed);Application.Quit(passed?0:1);
            }
        }
    }
    public sealed class NativeVfxSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-nativeVfxSmokeTest"))return;
            started=true;var go=new GameObject("Original VFX slice verification");DontDestroyOnLoad(go);go.AddComponent<NativeVfxSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;yield return null;
            var controller=FindAnyObjectByType<ManualBattleController>();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string path=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/native-vfx-player");
            int role=Array.IndexOf(args,"-nativeVfxSmokeTest");string id=role+1<args.Length?args[role+1]:"D27";
            yield return controller.VerifyNativeVfx(id,path);
        }
    }
}
