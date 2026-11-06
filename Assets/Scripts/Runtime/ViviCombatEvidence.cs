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
        [Serializable] sealed class ViviEvidenceResult { public bool passed; public string[] checks; }
        IEnumerator ViviEvidenceFrame(string folder,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
        }
        // This fixture is an explicit diagnostic. It changes runtime snapshots, never authored assets.
        internal IEnumerator VerifyViviCombatEvidence(string folder)
        {
            Directory.CreateDirectory(folder);diagnostic=true;Application.runInBackground=true;
            var checks=new List<string>();bool passed=true,completed=false;float oldScale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{passed&=value;checks.Add(name+"="+value);Debug.Log("VIVI_COMBAT_EVENT "+name+"="+value);};
            DefenderTrialDefinition trial=null;
            try
            {
                Time.timeScale=0;SwitchTrial(false);TestSelectionOpen=false;
                yield return new WaitForSecondsRealtime(2.5f); // Let the standalone splash finish before screen evidence.
                var basic=definition.Snapshot();var combat=definition.Engagement.Snapshot();
                var points=new[]{new Vector3(-4,.35f,0),new Vector3(-1,.35f,0),new Vector3(-4,.35f,0),new Vector3(3,.35f,0)};
                ClearViews();Battle=new ManualBattle(basic,points,new Vector3(0,.35f,100),combat);EnableCombatView();
                SelectCard(true);var receipt=TryDeployAt(Battle.Entry);check("walkDeployment",receipt.Success);
                if(!receipt.Success)yield break;
                var unit=Battle.FindUnit(receipt.EntityId);Tick(.5f);yield return null;
                var visual=FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.EntityId==unit.EntityId);
                check("walkRightFacesRight",unit.IsMoving&&unit.Motion.Direction.x>0&&!visual.Renderer.flipX);
                yield return ViviEvidenceFrame(folder,"01-walk-right");
                for(int n=0;n<400&&unit.Motion.Direction.x>=0;n++){Tick(.05f);if(n%10==0)yield return null;}
                Tick(.2f);yield return null;
                check("leftCornerMirrors",unit.IsMoving&&unit.Motion.Direction.x<0&&visual.Renderer.flipX);
                yield return ViviEvidenceFrame(folder,"02-corner-left");
                double distance=unit.Motion.DistanceTravelled;unit.FearUntil=Battle.Clock+10;Tick(.2f);yield return null;
                check("fearRetreatFacesRight",unit.Motion.DistanceTravelled<distance&&!visual.Renderer.flipX);
                yield return ViviEvidenceFrame(folder,"03-fear-retreat-right");
                Battle.SetPaused(true);double clock=Battle.Clock;int frame=visual.FrameIndex;bool flip=visual.Renderer.flipX;var position=unit.Motion.Position;
                Tick(5);yield return null;
                check("pauseFreezesWalk",Battle.Clock==clock&&unit.Motion.Position==position&&visual.FrameIndex==frame&&visual.Renderer.flipX==flip);
                Battle.SetPaused(false);

                // Three durable original Kroos models: all use the existing base armor (3).
                // The original 105 attack, six rounds, SP and interval are copied without asset mutation.
                trial=ScriptableObject.CreateInstance<DefenderTrialDefinition>();trial.name="Vivi explicit runtime splash fixture";
                trial.battle=definition;trial.route=new[]{new Vector3(-3,.35f,0),new Vector3(6,.35f,0)};
                trial.defenders=new[]{0f,1.8f,2.4f}.Select((x,i)=>new DefenderPlacement{
                    id="vivi-proof-"+i,displayName=i==0?"中心靶":i==1?"范围内1.8":"范围外2.4",artId="D07",position=new Vector3(x,.35f,0),
                    health=5000,attack=1,armor=3,range=.01f,interval=100,windup=0,actionDuration=0,blocks=0}).ToArray();
                yield return Prepare(Plan(trial.defenders,new[]{definition.Card},false),()=>{},"准备维维美范围命中证据");
                ClearViews();CancelInteraction();terminalPresentationElapsed=readyIdleElapsed=0;defenderTrial=trial;
                Battle=new ManualBattle(basic,trial.route,trial.defenders[0].position,combat,roster:trial.Snapshot());EnableCombatView();
                SelectCard(true);receipt=TryDeployAt(Battle.Entry);check("splashFixtureDeployment",receipt.Success);
                if(!receipt.Success)yield break;
                unit=Battle.FindUnit(receipt.EntityId);
                for(int n=0;n<200&&!Battle.IsSkillReady(unit.EntityId);n++){Tick(.05f);if(n%10==0)yield return null;}
                check("ordinaryShotPreserved",Battle.CombatShots.Any(s=>!s.FromDefender&&!s.Empowered&&s.Applied&&s.SplashRadius==0));
                check("ordinaryShotDidNotSplash",Battle.Defenders[1].Health==5000&&Battle.Defenders[2].Health==5000);
                float[] before=Battle.Defenders.Select(d=>d.Health).ToArray();long prior=Battle.CombatFeedback.Count==0?0:Battle.CombatFeedback.Last().Sequence;
                SelectSkillUnit(unit.EntityId);check("manualSkill",ActivateSelectedSkill()==SkillFailure.None);
                for(int n=0;n<100&&!Battle.CombatShots.Any(s=>!s.FromDefender&&s.Empowered);n++){Tick(.05f);yield return null;}
                var shell=Battle.CombatShots.LastOrDefault(s=>!s.FromDefender&&s.Empowered);
                check("oneShellLaunched",shell!=null&&unit.SkillAmmo==5);
                if(shell==null)yield break;
                for(int n=0;n<30&&!shell.Resolved;n++){Tick(.05f);yield return null;}
                float[] after=Battle.Defenders.Select(d=>d.Health).ToArray();
                check("actualWideDamage",shell.Applied&&before[0]-after[0]==102&&before[1]-after[1]==102&&after[2]==before[2]&&shell.SplashRadius==2.2f);
                var losses=Battle.CombatFeedback.Where(e=>e.Sequence>prior&&e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost).ToArray();
                check("twoRealDamagePopups",losses.Length==2&&losses.Sum(e=>e.Amount)==204&&shell.DamageDealt==204);
                check("singleRoundConsumed",unit.SkillAmmo==5&&Battle.CombatShots.Count(s=>!s.FromDefender&&s.Empowered)==1);
                Tick(.1f);Battle.SetPaused(true);yield return ViviEvidenceFrame(folder,"04-wide-skill-impact-paused");
                clock=Battle.Clock;int ammo=unit.SkillAmmo;Tick(5);yield return null;
                check("pauseFreezesSplash",Battle.Clock==clock&&unit.SkillAmmo==ammo&&Battle.Defenders.Select(d=>d.Health).SequenceEqual(after));
                completed=true;
            }
            finally
            {
                Time.timeScale=oldScale;passed&=completed;
                File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new ViviEvidenceResult{passed=passed,checks=checks.ToArray()},true));
                Debug.Log("VIVI_COMBAT_CHECK passed="+passed);
                // Trial asset is runtime-only; process exit owns cleanup, including early failures.
                Application.Quit(passed?0:1);
            }

        }
    }
    public sealed class ViviCombatSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-viviCombatSmokeTest"))return;
            started=true;var go=new GameObject("Vivi combat verification");DontDestroyOnLoad(go);go.AddComponent<ViviCombatSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;yield return null;
            ManualBattleController controller=null;
            for(int n=0;n<1200;n++){controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.Battle!=null&&!controller.ArtLoading)break;yield return null;}
            if(controller==null){Debug.LogError("VIVI_COMBAT_CHECK missing controller");Application.Quit(1);yield break;}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string folder=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/vivi-combat-player");
            yield return controller.VerifyViviCombatEvidence(folder);
        }
    }
}
