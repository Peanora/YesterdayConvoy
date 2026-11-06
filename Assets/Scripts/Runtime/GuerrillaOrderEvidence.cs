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
        [Serializable] sealed class GuerrillaOrderEvidenceResult
        {
            public bool passed;
            public string[] checks,slots;
            public int plays;
        }

        bool GuerrillaLayerCoversGui(GuerrillaOrderView view)
        {
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            var bounds=view.ScreenBounds;
            var target=new RenderTexture(Mathf.RoundToInt(bounds.width),Mathf.RoundToInt(bounds.height),0,RenderTextureFormat.ARGB32);
            var reference=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;bool covered=true;
            try
            {
                target.Create();Graphics.Blit(view.PrepareMaterial().mainTexture,target,view.PrepareMaterial());
                RenderTexture.active=target;reference.ReadPixels(new Rect(0,0,target.width,target.height),0,0);reference.Apply();
                // Actual screenshot pixels at header, gold/pause controls, health bars,
                // central notice and bottom cards must match the opaque cut-in itself.
                foreach(var point in new[]{new Vector2(45,22),new Vector2(1050,26),new Vector2(1190,25),
                    new Vector2(700,100),new Vector2(640,345),new Vector2(380,640),new Vector2(650,620)})
                {
                    var a=(Color32)screen.GetPixel(Mathf.RoundToInt(bounds.x+point.x/1280*bounds.width),
                        Screen.height-1-Mathf.RoundToInt(bounds.y+point.y/720*bounds.height));
                    var b=(Color32)reference.GetPixel(Mathf.RoundToInt(point.x/1280*target.width),
                        target.height-1-Mathf.RoundToInt(point.y/720*target.height));
                    int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
                    if(delta>15){covered=false;Debug.Log("GUERRILLA_LAYER_PIXEL "+point+" screen="+a+" expected="+b+" delta="+delta);}
                }
            }
            finally{RenderTexture.active=previous;target.Release();Destroy(target);Destroy(reference);Destroy(screen);}
            return covered;
        }

        IEnumerator ChargeGuerrillaForEvidence(bool missing)
        {
            if(!SetSandboxRoster(2))throw new InvalidOperationException("Cannot prepare guerrilla formation.");
            if(missing)
            {
                int index=shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M19");
                var point=ShopPieceRect(index).center;
                ProcessShopPointer(EventType.MouseDown,point);
                ProcessShopPointer(EventType.MouseUp,point);
                if(shopLineup.Contains("CARD_M19"))throw new InvalidOperationException("Normal formation click did not remove Sarkaz fighter.");
            }
            if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot start guerrilla battle.");
            while(ArtLoading||ShopPanelTransitioning)yield return null;
            Battle.Begin();
            // Explicit durable/non-attacking runtime fixture. Keep the authored guard
            // positions/routes; real deployments and ordinary contact still earn orders.
            foreach(var guard in Battle.Defenders)
            {
                guard.Health=guard.MaxHealth=1000000;
                guard.NextAttackTime=Battle.Clock+1000;guard.SkillSP=-1000;
            }
            long request=80000;
            var attackers=shopLineup.Where(id=>id!="CARD_M20"&&Battle.FindCard(id)!=null).ToArray();
            for(int pass=0;pass<2&&Battle.Orders<6;pass++)foreach(string id in attackers)
            {
                bool deployed=false;
                for(int i=0;i<180&&Battle.Phase==BattlePhase.Running;i++)
                {
                    if(Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,id).Success){deployed=true;break;}
                    Tick(.05f);if(i%10==0)yield return null;
                }
                if(!deployed)throw new InvalidOperationException("Normal deployment failed: "+id);
                Tick(.5f);yield return null;
            }
            for(int i=0;i<1600&&Battle.Phase==BattlePhase.Running;i++)
            {
                if(Battle.Orders>=6&&UseCampaignOrder(false))yield break;
                Tick(.05f);if(i%10==0)yield return null;
            }
            throw new InvalidOperationException("Real combat could not earn/spend a guerrilla order: phase="+Battle.Phase+" points="+Battle.Orders+" tier="+Battle.GuerrillaTier+" used="+Battle.OrdersUsed);
        }

        internal IEnumerator VerifyGuerrillaOrder(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            float scale=Time.timeScale;var checks=new List<string>();bool passed=true,complete=false;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("GUERRILLA_ORDER_EVENT "+name+"="+value);};
            var view=GetComponent<GuerrillaOrderView>();
            string[] slots=null;
            try
            {
                yield return new WaitForSecondsRealtime(1);Time.timeScale=0;
                check("failureDoesNotPlay",!UseCampaignOrder(false)&&view.PlayCount==0);
                var charge=ChargeGuerrillaForEvidence(false);
                while(charge.MoveNext())yield return charge.Current;
                slots=view.SlotMembers.ToArray();
                check("realOrderSuccessPlaysOnce",view.PlayCount==1&&Battle.OrdersUsed==1&&Battle.OrderStrikes>0);
                check("allSevenFormationMembersAndTwoEmptyExtensions",slots.Take(7).All(id=>id!=null)&&slots.Skip(7).All(id=>id==null));
                check("undeployedPatriotStillDisplayed",slots.Contains("CARD_M20")&&Battle.Units.All(u=>u.CardId!="CARD_M20"));
                check("resourcesAndShaderAvailable",view.AssetsReady&&view.PrepareMaterial().shader.isSupported);
                Tick(.07f);yield return null;yield return null;
                float inOpacity=view.Opacity;check("fadeInIsPartial",inOpacity>0&&inOpacity<1);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"fade-in.png"));yield return null;
                Battle.SetPaused(true);double clock=PresentationClock;float age=view.Age;
                Tick(3);yield return null;yield return null;
                check("pauseFreezesFadeAndAudio",PresentationClock==clock&&view.Age==age&&view.Opacity==inOpacity&&view.SoundPaused);
                check("pausedFailureDoesNotReplay",!UseCampaignOrder(false)&&view.PlayCount==1);
                Battle.SetPaused(false);Tick(.1f);yield return null;yield return null;
                check("peakReached",view.Opacity>.99f);
                yield return new WaitForEndOfFrame();
                check("actualFullScreenLayerCoversHud",GuerrillaLayerCoversGui(view));
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"full-formation-peak.png"));yield return null;
                ToggleSpeed();clock=PresentationClock;Tick(.05f);yield return null;
                check("doubleSpeedUsesPresentationClock",Math.Abs(PresentationClock-clock-.1)<.0001);ToggleSpeed();
                Tick(.25f);yield return null;yield return null;
                check("fadeOutIsPartial",view.Opacity>0&&view.Opacity<1);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"fade-out.png"));yield return null;
                Tick(.5f);yield return null;yield return null;
                check("effectClears",!view.Playing&&view.Opacity==0);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"effect-ended.png"));yield return null;
                ReplaySandbox();while(ArtLoading)yield return null;yield return null;
                check("replayClearsEffect",!view.Playing&&double.IsNegativeInfinity(view.PendingUntil));
                charge=ChargeGuerrillaForEvidence(true);
                while(charge.MoveNext())yield return charge.Current;
                check("removedFormationMemberStaysEmpty",view.SlotMembers[4]==null&&view.SlotMembers[3]=="CARD_M20"&&view.SlotMembers[7]==null&&view.SlotMembers[8]==null);
                Tick(.18f);yield return null;yield return null;
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"missing-member-peak.png"));yield return null;
                loopTestSettlementOpen=true;check("explicitTerminalFixture",TryTestSettlement(false));
                clock=Battle.Clock;Tick(.18f);yield return null;yield return null;
                check("terminalRetainsFadeWithoutCombat",view.Playing&&Battle.Clock==clock&&!SettlementVisible);
                Tick(5);yield return null;yield return null;
                check("terminalFadeEnds",!view.Playing&&Battle.Clock==clock);
                if(Environment.GetCommandLineArgs().Contains("-guerrillaOrderCaptureFrames"))
                {
                    ReplaySandbox();while(ArtLoading)yield return null;
                    charge=ChargeGuerrillaForEvidence(false);
                    while(charge.MoveNext())yield return charge.Current;
                    string frames=Path.Combine(directory,"frames");Directory.CreateDirectory(frames);
                    // Actual Player-rendered simulation frames, not interpolated concept art.
                    for(int frame=0;frame<66;frame++)
                    {
                        if(frame>=6)Tick(1f/60);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.Combine(frames,frame.ToString("D3")+".png"));yield return null;
                    }
                    check("capturedCompleteFade",!view.Playing);
                }
                complete=true;
            }
            finally
            {
                Time.timeScale=scale;passed&=complete;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new GuerrillaOrderEvidenceResult
                    {passed=passed,checks=checks.ToArray(),slots=slots,plays=view!=null?view.PlayCount:0},true));
                Debug.Log("GUERRILLA_ORDER_PLAYER_CHECK passed="+passed);Application.Quit(passed?0:1);
            }
        }
    }

    public sealed class GuerrillaOrderSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-guerrillaOrderSmokeTest"))return;
            started=true;var go=new GameObject("Guerrilla order explicit Player verification");DontDestroyOnLoad(go);go.AddComponent<GuerrillaOrderSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("GuerrillaTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            if(controller==null||controller.ArtLoading)throw new InvalidOperationException("Guerrilla trial did not load.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/guerrilla-order-player");
            yield return controller.VerifyGuerrillaOrder(directory);
        }
    }
}
