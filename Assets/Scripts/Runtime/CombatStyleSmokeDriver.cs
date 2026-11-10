using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    // Opt-in verification uses the same saved scenes, pointer deployment and combat as normal play.
    // It never injects HP, damage, skills, shop stock, deaths or victory.
    public sealed class CombatStyleSmokeDriver:MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name;public bool passed;}
        [Serializable] public sealed class Report
        {
            public string scope="Six saved combat-style scenes; programmatic production pointer/skill APIs; not human or OS-mouse playtesting.";
            public List<Check> checks=new List<Check>();
            public bool passed;
            public void Check(string name,bool condition){checks.Add(new Check{name=name,passed=condition});Debug.Log("COMBAT_STYLE_CHECK "+name+"="+condition);}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("-combatStylesSmokeTest"))return;
            var host=new GameObject("Combat Style Verification");DontDestroyOnLoad(host);host.AddComponent<CombatStyleSmokeDriver>().StartCoroutine(RunPlayer());
        }
        static IEnumerator RunPlayer()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string dir=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("Logs/combat-styles-player");Directory.CreateDirectory(dir);
            var report=new Report();yield return Exercise(report,args.Contains("-combatStylesScreens")?dir:null);
            report.passed=report.checks.Count>0&&report.checks.All(c=>c.passed);File.WriteAllText(Path.Combine(dir,"result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("COMBAT_STYLE_PLAYER_RESULT "+report.passed+" checks="+report.checks.Count);Application.Quit(report.passed?0:1);
        }
        static IEnumerator Capture(string folder,string name,Report report)
        {
            if(folder==null)yield break;
            Directory.CreateDirectory(folder);yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();bool visible=false;
            if(image!=null)
            {
                var pixels=image.GetPixels32();int bright=0,samples=0;byte lo=255,hi=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {byte v=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));lo=Math.Min(lo,v);hi=Math.Max(hi,v);samples++;if(v>24)bright++;}
                visible=image.width>0&&image.height>0&&bright>samples/20&&hi-lo>12;
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
            }
            report.Check("visible-frame-"+name,visible);
        }
        public static IEnumerator Exercise(Report report,string captureFolder=null)
        {
            float oldTime=Time.timeScale;
            foreach(var entry in SynergyTrialCatalog.StyleEntries)
            {
                Time.timeScale=1;yield return SceneManager.LoadSceneAsync(entry.SceneName);ManualBattleController c=null;
                for(int i=0;i<1800;i++)
                {c=FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.CampaignActive&&!c.ArtLoading&&!c.ShopPanelTransitioning)break;yield return null;}
                bool ready=c!=null&&c.CampaignActive&&!c.ArtLoading;report.Check(entry.Id+"-saved-scene-ready",ready);if(!ready)continue;
                Time.timeScale=0;var b=c.Battle;var data=FindAnyObjectByType<CampaignBootstrap>().trialDefinition;
                string original=JsonUtility.ToJson(data),encounter=JsonUtility.ToJson(data.encounter);
                var style=CombatStyleRules.Find(entry.Tag).Style;
                report.Check(entry.Id+"-snapshot-and-real-shop",b.CombatStylesEnabled&&b.StyleTier(style)==entry.MaximumTier&&c.Shop.UnlimitedGold&&c.Shop.Level==8&&b.DP==100);
                c.Shop.Validate();report.Check(entry.Id+"-inspector-map",FindAnyObjectByType<AuthoredBattlefield>()!=null);
                yield return Capture(captureFolder,entry.Id+"-preparation",report);
                report.Check(entry.Id+"-details-open",c.OpenSynergyDetails(entry.Tag));yield return Capture(captureFolder,entry.Id+"-details",report);c.CloseCampaignOverlay();
                report.Check(entry.Id+"-begin",c.StartLoopNextBattle());b=c.Battle;b.Begin();
                string[] deploy=style==CombatStyle.Assault?new[]{"CARD_M37"}:style==CombatStyle.Marksman?new[]{"CARD_M08","CARD_M11"}:
                    style==CombatStyle.Mystic?new[]{"CARD_M10","CARD_M09"}:style==CombatStyle.Bulwark?new[]{"CARD_M07","CARD_M08"}:
                    style==CombatStyle.Frenzy?new[]{"CARD_M02"}:new[]{"CARD_WISADEL_APRIL","CARD_M14","CARD_M05","CARD_M17"};
                int placed=0;bool triggered=false;string route=b.Lanes[0].Id;long skillRequest=10000;
                for(int step=0;step<1200&&b.Phase==BattlePhase.Running;step++)
                {
                    if(c.ArtLoading){yield return null;continue;}
                    if(placed<deploy.Length&&b.CheckCardDeployment(deploy[placed],b.Lanes[0].Entry,true)==DeployFailure.None&&CampaignJourneyEvidenceProbe.Drag(c,deploy[placed],route,out _))placed++;
                    foreach(var u in b.Units.Where(u=>u.Active&&!u.SynergySupport).ToArray())if(b.UnitCombat(u).HasSkill)b.TryActivateSkill(b.RunId,++skillRequest,u.EntityId);
                    c.Tick(.1f);
                    triggered=style==CombatStyle.Assault?b.Defenders.Any(d=>b.StyleRuptureRemaining(d)>0):style==CombatStyle.Marksman?b.Defenders.Any(d=>b.StyleExposureRemaining(d)>0):
                        style==CombatStyle.Mystic?b.StylePulses>0:style==CombatStyle.Bulwark?b.StyleWards>0:style==CombatStyle.Frenzy?b.StyleRages>0:b.StyleCoordinations>0;
                    if(triggered&&placed==deploy.Length)break;if(step%10==0)yield return null;
                }
                Debug.Log("COMBAT_STYLE_OBSERVED "+entry.Id+" clock="+b.Clock+" phase="+b.Phase+" pulses="+b.StylePulses+" wards="+b.StyleWards+" rage="+b.StyleRages+" coordination="+b.StyleCoordinations);
                report.Check(entry.Id+"-normal-pointer-deployment",placed==deploy.Length);report.Check(entry.Id+"-natural-combat-trigger",triggered);
                b.SetPaused(true);double clock=b.Clock;float[] health=b.Units.Select(u=>u.Health).ToArray();var tiers=CombatStyleRules.Definitions.Select(d=>b.StyleTier(d.Style)).ToArray();
                c.Tick(5);report.Check(entry.Id+"-pause-freezes",b.Clock==clock&&health.SequenceEqual(b.Units.Select(u=>u.Health)));
                report.Check(entry.Id+"-fixed-snapshot",tiers.SequenceEqual(CombatStyleRules.Definitions.Select(d=>b.StyleTier(d.Style))));
                yield return Capture(captureFolder,entry.Id+"-battle",report);
                report.Check(entry.Id+"-restart",c.RestartCampaignEncounter());
                report.Check(entry.Id+"-restart-clears-procs",c.Battle.Clock==0&&c.Battle.Units.Count==0&&c.Battle.StylePulses==0&&c.Battle.StyleWards==0&&c.Battle.StyleRages==0&&c.Battle.StyleCoordinations==0&&c.Battle.StyleTier(style)==entry.MaximumTier);
                report.Check(entry.Id+"-source-assets-unchanged",JsonUtility.ToJson(data)==original&&JsonUtility.ToJson(data.encounter)==encounter);
                c.Shop.Validate();c.LeaveShopTrial();yield return null;
            }
            Time.timeScale=oldTime;report.passed=report.checks.Count>0&&report.checks.All(c=>c.passed);
        }
    }
}
