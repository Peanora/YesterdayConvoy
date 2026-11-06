using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    // Explicit development-only fixture. Ordinary scene startup never runs these inputs.
    public sealed class SandboxFeaturesEvidence : MonoBehaviour
    {
        [Serializable] sealed class Result { public bool passed; public string[] checks; }
        readonly List<string> checks=new List<string>();
        bool passed=true;
        string directory;
        void Check(string name,bool value)
        { checks.Add(name+"="+value);passed&=value;Debug.Log("SANDBOX_FEATURE_EVENT "+name+"="+value); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(!Debug.isDebugBuild||!(args.Contains("-sandboxFeaturesSmokeTest")||args.Contains("-talulahManualRainSmokeTest"))||FindAnyObjectByType<SandboxFeaturesEvidence>()!=null)return;
            var go=new GameObject("Sandbox feature verification");DontDestroyOnLoad(go);go.AddComponent<SandboxFeaturesEvidence>();
        }
        IEnumerator Frame(string name)
        {
            // A visible standalone Player is required; hidden-window black frames are not evidence.
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/sandbox-features-player");Directory.CreateDirectory(directory);
            yield return Run();
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new Result{passed=passed,checks=checks.ToArray()},true));
            Debug.Log("SANDBOX_FEATURE_CHECK passed="+passed);Time.timeScale=1;Application.Quit(passed?0:1);
        }
        IEnumerator Run()
        {
            yield return SceneManager.LoadSceneAsync("TalulahPhaseTrial");yield return null;yield return null;
            var c=FindAnyObjectByType<ManualBattleController>();
            for(int n=0;n<1800&&(c.ArtLoading||c.TestSelectionOpen||c.Definition.Card.CardId!=OriginalTalulahArt.CardId);n++)yield return null;
            Time.timeScale=0;Check("talulahTrialReady",!c.ArtLoading&&!c.TestSelectionOpen&&c.Definition.Card.CardId==OriginalTalulahArt.CardId);
            c.SelectCard(true);Check("talulahNaturalDeployment",c.TryDeployAt(c.Battle.Entry).Success);
            var unit=c.Battle.Units.First();var seen=new HashSet<TalulahPhase>();bool pauseChecked=false,manualChecked=false,damageSeen=false;
            for(int i=0;i<9000&&unit.TalulahPhase!=TalulahPhase.Dead;i++)
            {
                c.Tick(.05f);damageSeen|=c.Battle.CombatFeedback.Any(e=>e.Kind==CombatFeedbackKind.HealthLost);
                var phase=unit.TalulahPhase;
                if(seen.Add(phase))
                {
                    Debug.Log("TALULAH_NATURAL_PHASE "+phase+" at="+c.Battle.Clock+" HP="+unit.Health);
                    if(phase==TalulahPhase.FirstFall||phase==TalulahPhase.FinalFall)yield return Frame("talulah-"+phase);
                }
                if(phase==TalulahPhase.FirstRain&&!pauseChecked)
                {
                    c.Tick(3);c.Battle.SetPaused(true);double clock=c.Battle.Clock;int waves=unit.TalulahAutomaticWavesFired;c.Tick(5);
                    Check("phaseRainPause",c.Battle.Clock==clock&&unit.TalulahAutomaticWavesFired==waves);yield return Frame("talulah-first-rain-paused");
                    c.Battle.SetPaused(false);pauseChecked=true;
                }
                if(phase==TalulahPhase.Second&&!manualChecked&&c.Battle.Clock>unit.TalulahPhaseStartedAt+3&&c.Battle.IsSkillReady(unit.EntityId))
                {
                    Check("secondPhaseManualSkill",c.Battle.TryActivateSkill(c.Battle.RunId,50001,unit.EntityId)==SkillFailure.None);
                    c.Tick(1.25f);yield return Frame("talulah-second-form-manual");
                    var hand=FindObjectsByType<SpriteRenderer>().FirstOrDefault(r=>r.enabled&&r.name=="Talulah native / ignite-B-caster");
                    Check("manualBHandVisible",hand!=null);var first=hand?.sprite;
                    c.Tick(.25f);yield return Frame("talulah-second-form-manual-progress");
                    Check("manualBHandAnimates",hand!=null&&hand.enabled&&hand.sprite!=first);
                    c.Battle.SetPaused(true);double clock=c.Battle.Clock;var paused=hand?.sprite;
                    c.Tick(5);yield return null;Check("manualBHandPause",c.Battle.Clock==clock&&hand!=null&&hand.sprite==paused);
                    c.Battle.SetPaused(false);c.Tick(.25f);yield return null;
                    Check("manualBHandResumes",hand!=null&&hand.enabled&&hand.sprite!=paused);
                    c.Tick(.4f);yield return Frame("talulah-second-form-manual-dissipated");
                    Check("manualBHandEndsWhileRainContinues",!FindObjectsByType<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Talulah native / ignite-B-caster")&&
                        c.Battle.IsSkillActionPlaying(unit)&&FindAnyObjectByType<FireRainView>().VisibleStreams>0);
                    c.Tick(3.95f);yield return null;
                    Check("manualBRainEndsAtSixSeconds",!c.Battle.IsSkillActionPlaying(unit)&&unit.SkillAmmo==0&&FindAnyObjectByType<FireRainView>().VisibleStreams==0);
                    manualChecked=true;
                }
                if(i%10==0)yield return null;
            }
            Check("secondPhaseManualChecked",manualChecked);
            Check("naturalTwoFallsAndFinalDeath",seen.Contains(TalulahPhase.FirstFall)&&seen.Contains(TalulahPhase.FirstRain)&&seen.Contains(TalulahPhase.Second)&&seen.Contains(TalulahPhase.FinalFall)&&seen.Contains(TalulahPhase.FinalRain)&&unit.TalulahPhase==TalulahPhase.Dead&&unit.Resolution==UnitResolution.Dead);
            Check("phaseActualDamageJournal",damageSeen);c.Tick(.4f);yield return Frame("talulah-final-death");c.Tick(2);yield return Frame("talulah-settlement");
            if(Environment.GetCommandLineArgs().Contains("-talulahManualRainSmokeTest"))yield break;
            foreach(string scene in new[]{"ReunionTrial","SnowTrial","InfectionTrial","GuerrillaTrial","MudrockCrewTrial","HerdTrial","DeepPoolTrial","MarineTrial","SarkazTrial"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
                c=FindAnyObjectByType<ManualBattleController>();var boot=FindAnyObjectByType<CampaignBootstrap>();
                yield return c.PrepareCampaign(true,44,boot.trialDefinition);Time.timeScale=0;
                Check(scene+"/configured",boot.trialDefinition!=null&&c.SynergySandbox&&c.Shop.UnlimitedGold&&c.Shop.Level==boot.trialDefinition.initialLevel);
                c.Shop.Validate();Check(scene+"/members",boot.trialDefinition.initialMembers.All(m=>c.Shop.Bench.Any(p=>p.Id==m.cardId&&p.Star==m.star)));
                Check(scene+"/start",c.StartLoopNextBattle());
                yield return new WaitForSecondsRealtime(.3f);
                var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));int before=c.Battle.Units.Count;
                var card=c.ShopPieceRect(0).center;c.ProcessShopPointer(EventType.MouseDown,card);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
                Check(scene+"/drag",c.Battle.Units.Count>before);
                if(scene=="SnowTrial")
                {
                    var d=c.Battle.Defenders.First(x=>x.Alive);c.Battle.TryApplyCold(d.EntityId,5);c.Battle.TryApplyCold(d.EntityId,5);c.Tick(.05f);
                    Check("frozenActualStateAndJournal",d.FrozenUntil>c.Battle.Clock&&c.Battle.CombatFeedback.Any(e=>e.Status==CombatFeedbackStatus.Frozen));
                    c.Battle.SetPaused(true);yield return Frame("snow-frozen-feedback");c.Battle.SetPaused(false);
                }
                for(int tick=0;tick<60;tick++){c.Tick(.05f);if(tick%10==0)yield return null;}
                Check(scene+"/savedTerrain",FindAnyObjectByType<AuthoredBattlefield>()!=null);c.Shop.Validate();
                if(scene=="MudrockCrewTrial")Check("cultivationNotGifted",c.Battle.GolemExperience==0);
            }
            yield return Frame("sarkaz-trial");
        }
    }
}
