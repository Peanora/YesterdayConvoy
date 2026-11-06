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
        [Serializable] sealed class InfectionClick {public int id;public string name;public float x,y,width,height;}
        [Serializable] sealed class InfectionEvidenceResult {public bool passed;public int changedPixels;public string[] checks;}
        int infectionClickSequence;
        Rect InfectionEvidenceRow
        {
            get {int i=Array.FindIndex(CurrentSynergies,s=>s.Name=="感染生物");return new Rect(SynergyListRect.x,SynergyListRect.y+28+i*46,139,41);}
        }
        IEnumerator InfectionGuiClick(string directory,string name,Rect rect,Func<bool> accepted)
        {
            var viewport=TacticalViewport.Current;Vector2 screen=viewport.ToScreen(rect.center);
            int id=++infectionClickSequence;
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new InfectionClick{id=id,name=name,x=screen.x,y=Screen.height-screen.y,width=Screen.width,height=Screen.height}));
            double deadline=Time.realtimeSinceStartupAsDouble+300;
            while(true)
            {
                string ack=Path.Combine(directory,"gui-ack.txt");
                if(File.Exists(ack)&&File.ReadAllText(ack).Trim()==id.ToString()&&accepted())break;
                if(Time.realtimeSinceStartupAsDouble>deadline)
                {
                    File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new InfectionEvidenceResult{passed=false,checks=new[]{"GUI click timeout: "+name}},true));
                    Application.Quit(1);throw new InvalidOperationException("GUI click timeout: "+name);
                }
                yield return null;
            }
            yield return null;yield return null;
        }
        static int InfectionPixels(Renderer[] renderers)
        {
            var camera=Camera.main;var before=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(640,360,24);var image=new Texture2D(640,360,TextureFormat.RGB24,false);
            var enabled=renderers.Select(r=>r.enabled).ToArray();
            Color32[] Capture(){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();}
            try
            {
                camera.targetTexture=target;var a=Capture();foreach(var r in renderers)r.enabled=false;var b=Capture();int changed=0;
                for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>12)changed++;
                return changed;
            }
            finally
            {for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];camera.targetTexture=before;RenderTexture.active=active;target.Release();Destroy(target);Destroy(image);}
        }
        internal IEnumerator VerifyInfectionFeedback(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var checks=new List<string>();bool passed=true,complete=false;int pixels=0;float scale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("INFECTION_EVENT "+name+"="+value);};
            try
            {
                yield return new WaitForSecondsRealtime(1);Time.timeScale=0;
                check("authoredInfectionTrial",Battle.InfectionTier==6&&Battle.InfectionShape==InfectionForm.Burst);
                check("initialSixTierOpensChoice",inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"choose-swarm",InfectionPrimaryRect(InfectionForm.Swarm),()=>Battle.InfectionShape==InfectionForm.Swarm&&inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"choose-shell",InfectionPrimaryRect(InfectionForm.Shell),()=>Battle.InfectionShape==InfectionForm.Shell&&inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"choose-burst",InfectionPrimaryRect(InfectionForm.Burst),()=>Battle.InfectionShape==InfectionForm.Burst&&inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"choose-secondary-shell",InfectionSecondaryRect(InfectionForm.Shell),()=>Battle.InfectionSecondShape==InfectionForm.Shell);
                check("realGuiSelectsAllThreeAndDistinctSecondary",Battle.InfectionShape==InfectionForm.Burst&&Battle.InfectionSecondShape==InfectionForm.Shell);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"shape-choice-player.png"));yield return null;
                yield return InfectionGuiClick(directory,"close-preparation-panel",InfectionCloseRect,()=>inspectedSynergy==null);
                var fullLineup=shopLineup.ToArray();
                shopLineup.Remove("CARD_M04");shopLineup.Remove("CARD_M06");SyncShopDeck(false);
                check("droppingToFourDoesNotPrompt",inspectedSynergy==null);
                yield return InfectionGuiClick(directory,"open-four-tier-panel",InfectionEvidenceRow,()=>inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"locked-four-tier-secondary",InfectionSecondaryRect(InfectionForm.Swarm),()=>Battle.InfectionSecondShape==InfectionForm.Shell);
                check("fourTierSecondaryGuiIsLocked",Battle.InfectionTier==4&&Battle.InfectionSecondShape==InfectionForm.Shell);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"shape-four-tier-player.png"));yield return null;
                yield return InfectionGuiClick(directory,"close-four-tier-panel",InfectionCloseRect,()=>inspectedSynergy==null);
                shopLineup.Clear();shopLineup.AddRange(fullLineup);SyncShopDeck(false);
                check("regainingSixPromptsAgain",Battle.InfectionTier==6&&inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"close-regained-six-panel",InfectionCloseRect,()=>inspectedSynergy==null);
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot start infection trial.");
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                Battle.Begin();Battle.SetPaused(true);
                yield return InfectionGuiClick(directory,"open-locked-panel",InfectionEvidenceRow,()=>inspectedSynergy=="感染生物");
                yield return InfectionGuiClick(directory,"locked-swarm-cannot-change",InfectionPrimaryRect(InfectionForm.Swarm),()=>Battle.InfectionShape==InfectionForm.Burst);
                check("battleChoiceIsLocked",!SetCampaignForms(InfectionForm.Swarm,InfectionForm.Shell)&&Battle.InfectionShape==InfectionForm.Burst);
                yield return InfectionGuiClick(directory,"close-battle-panel",InfectionCloseRect,()=>inspectedSynergy==null);
                Battle.SetPaused(false);

                // Explicit local fixture: use a valid point on the saved route, two nearby durable
                // defenders and distant others. Hound has no intrinsic explosion; only real final death is used.
                var receipt=Battle.TryDeployRole(Battle.RunId,88001,Battle.Entry,"CARD_M02");
                if(!receipt.Success)throw new InvalidOperationException("Hound fixture failed to deploy.");
                var unit=Battle.FindUnit(receipt.EntityId);
                for(int i=0;i<80&&!unit.Motion.HasArrived;i++)
                {
                    var p=Camera.main.WorldToViewportPoint(unit.Motion.Position);
                    if(i>0&&p.x>.35&&p.x<.8&&p.y>.32&&p.y<.7)break;
                    unit.Motion.Advance((float)(unit.Motion.TotalLength/100/unit.Motion.Speed));
                }
                Vector3 position=unit.Motion.Position;
                for(int i=0;i<Battle.Defenders.Count;i++)
                {
                    var guard=Battle.Defenders[i];guard.Health=guard.MaxHealth=100000;
                    guard.NextAttackTime=1000;guard.SkillSP=-1000;
                    guard.Position=position+new Vector3(i==0?-.5f:i==1?.5f:10+i,0,.3f);
                }
                check("finalDeathSchedulesOneSourcedBurst",Battle.TryDamageUnit(unit.EntityId,1000000,CombatDamageType.Arts)&&Battle.InfectionBursts.Count==1&&Battle.InfectionBursts[0].SourceId==unit.EntityId&&Battle.DeathBlasts.Count==0);
                Tick(.1f);yield return null;yield return null;
                var burst=Battle.InfectionBursts.Single();Battle.SetPaused(true);double clock=Battle.Clock;
                Tick(1);yield return null;
                check("pauseKeepsPendingBlastUnresolved",Battle.Clock==clock&&!burst.Resolved&&burst.Hits.Count==0);
                Battle.SetPaused(false);Tick(.18f);yield return null;yield return null;
                check("actualTwoHitsAndDamage",burst.Resolved&&burst.Hits.Count==2&&Math.Abs(burst.TotalDamage-170)<.01&&burst.Hits.All(h=>Math.Abs(h.Damage-85)<.01));
                var renderers=combatView.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r.name.StartsWith("Infection burst /",StringComparison.Ordinal)||r.name=="Original VFX / common/common_firemagic_hit_01")).ToArray();
                pixels=InfectionPixels(renderers);check("originalFireAndFeedbackHaveGpuPixels",renderers.Length>1&&pixels>80);
                check("damageUsesRealFeedback",Battle.CombatFeedback.Count(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost&&burst.Hits.Any(h=>h.EntityId==e.TargetId))==2);
                Battle.SetPaused(true);clock=Battle.Clock;float total=burst.TotalDamage;Tick(2);yield return null;
                check("pauseDoesNotRepeatDamage",Battle.Clock==clock&&burst.TotalDamage==total&&burst.Hits.Count==2);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"infection-burst-player.png"));yield return null;
                Battle.SetPaused(false);loopTestSettlementOpen=true;check("explicitTerminalFixture",TryTestSettlement(false));clock=Battle.Clock;
                Tick(1.5f);yield return null;yield return null;
                check("terminalTailClearsWithoutMoreCombat",Battle.Clock==clock&&!combatView.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.name.StartsWith("Infection burst /",StringComparison.Ordinal)));
                ReplaySandbox();yield return null;yield return null;
                check("replayClearsEventsAndKeepsSelection",Battle.InfectionBursts.Count==0&&Battle.InfectionShape==InfectionForm.Burst);
                complete=true;
            }
            finally
            {
                Time.timeScale=scale;passed&=complete;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new InfectionEvidenceResult{passed=passed,changedPixels=pixels,checks=checks.ToArray()},true));
                Debug.Log("INFECTION_PLAYER_CHECK passed="+passed);Application.Quit(passed?0:1);
            }
        }
    }
    public sealed class InfectionSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-infectionSmokeTest"))return;
            started=true;var go=new GameObject("Infection explicit Player verification");DontDestroyOnLoad(go);go.AddComponent<InfectionSmokeDriver>();
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync("InfectionTrial");ManualBattleController c=null;
            for(int i=0;i<1800;i++){c=FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.CampaignActive&&!c.ArtLoading)break;yield return null;}
            if(c==null)throw new InvalidOperationException("Infection trial did not load.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            yield return c.VerifyInfectionFeedback(at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/infection-player"));
        }
    }
}
