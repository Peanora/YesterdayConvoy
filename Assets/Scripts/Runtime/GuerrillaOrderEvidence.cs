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
            public int plays,sounds;
            public string[] impactTargets;
            public float[] impactDamage;
            public float totalDamage;
        }

        void AdvanceGuerrillaEvidenceTo(GuerrillaOrderView view,float targetAge)
        {
            float remaining=targetAge-view.Age;
            if(remaining<-.0001f)throw new InvalidOperationException("Guerrilla evidence already passed target age "+targetAge+": "+view.Age);
            if(remaining<=0)return;
            float rate=Mathf.Min(1,EffectiveSimulationSpeed);
            if(rate<=0)throw new InvalidOperationException("Cannot advance guerrilla evidence while paused.");
            Tick(remaining/rate);
        }

        bool GuerrillaLayerCoversGui(GuerrillaOrderView view)
        {
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            var bounds=view.ScreenBounds;
            var target=new RenderTexture(Mathf.RoundToInt(bounds.width),Mathf.RoundToInt(bounds.height),0,RenderTextureFormat.ARGB32);
            var reference=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;bool covered=true;
            try
            {
                target.Create();view.RenderTo(target);
                RenderTexture.active=target;reference.ReadPixels(new Rect(0,0,target.width,target.height),0,0);reference.Apply();
                // Actual screenshot pixels at header, gold/pause controls, health bars,
                // central notice and bottom cards must match the full V2 composition.
                foreach(var point in new[]{new Vector2(45,22),new Vector2(1050,26),new Vector2(1190,25),
                    new Vector2(700,100),new Vector2(640,345),new Vector2(380,640),new Vector2(650,620)})
                {
                    var a=(Color32)screen.GetPixel(Mathf.RoundToInt(bounds.x+point.x/1280*bounds.width),
                        Screen.height-1-Mathf.RoundToInt(bounds.y+point.y/720*bounds.height));
                    var b=(Color32)reference.GetPixel(Mathf.RoundToInt(point.x/1280*target.width),
                        target.height-1-Mathf.RoundToInt(point.y/720*target.height));
                    int delta=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
                    // Its background is intentionally translucent. Allow the maximum residual
                    // contribution of the covered scene; opaque portrait pixels remain strict.
                    int tolerance=18+3*(255-b.a);
                    if(b.a<230||delta>tolerance){covered=false;Debug.Log("GUERRILLA_LAYER_PIXEL "+point+" screen="+a+" expected="+b+" delta="+delta+" tolerance="+tolerance);}
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
                if(Battle.Orders>=6)
                {
                    // Only isolate this measurement after real deployment/contact earned its
                    // points. Drain already-fired ordinary shots before accepting the wave;
                    // no pending ordinary damage can then pollute the actual HP comparison.
                    foreach(var unit in Battle.Units)unit.NextAttackTime=Battle.Clock+1000;
                    if(Battle.CombatShots.All(shot=>shot.Resolved)&&UseCampaignOrder(false))yield break;
                }
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
            string[] impactTargets=null;float[] impactDamage=null;float totalDamage=0;
            try
            {
                yield return new WaitForSecondsRealtime(1);Time.timeScale=0;
                if(Environment.GetCommandLineArgs().Contains("-guerrillaOrderRenderProbe"))
                {
                    // Focused reproduction: no deployments, combat or order charging.
                    SetSandboxRoster(2);while(ArtLoading)yield return null;
                    StartLoopNextBattle();while(ArtLoading||ShopPanelTransitioning)yield return null;
                    Battle.Begin();view.Play(new[]{"CARD_M15","CARD_M16","CARD_M03","CARD_M20","CARD_M19","CARD_M18","CARD_M17"});
                    Tick(.65f);yield return null;yield return null;
                    slots=view.SlotMembers.ToArray();
                    yield return new WaitForEndOfFrame();
                    check("isolatedActualFullScreenLayerCoversHud",GuerrillaLayerCoversGui(view));
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"render-probe.png"));yield return null;
                    complete=true;yield break;
                }
                check("failureDoesNotPlay",!UseCampaignOrder(false)&&view.PlayCount==0);
                var charge=ChargeGuerrillaForEvidence(false);
                while(charge.MoveNext())yield return charge.Current;
                slots=view.SlotMembers.ToArray();
                var volley=Battle.LatestGuerrillaOrder;
                var guardHealth=Battle.Defenders.Where(d=>d.Alive).ToDictionary(d=>d.EntityId,d=>d.Health);
                check("realOrderSuccessPlaysOnce",view.PlayCount==1&&Battle.OrdersUsed==1&&volley!=null&&!volley.Resolved&&!volley.Cancelled&&volley.SourceIds.Count>0);
                check("allSevenFormationMembersAndTwoEmptyExtensions",slots.Take(7).All(id=>id!=null)&&slots.Skip(7).All(id=>id==null));
                check("undeployedPatriotStillDisplayed",slots.Contains("CARD_M20")&&Battle.Units.All(u=>u.CardId!="CARD_M20"));
                check("resourcesAndShaderAvailable",view.AssetsReady&&view.PrepareMaterial().shader.isSupported);
                int initialSounds=view.SoundPlayCount;
                AdvanceGuerrillaEvidenceTo(view,.22f);yield return null;yield return null;
                var entering=view.GeometryVertices.ToArray();var offsets=view.PaneOffsets.ToArray();
                check("independentPanesEnterFromOutside",offsets.Take(7).Any(p=>p.sqrMagnitude>100)&&view.VisiblePaneCount>0&&view.SoundPlayCount==initialSounds);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"converging.png"));yield return null;
                AdvanceGuerrillaEvidenceTo(view,.65f);yield return null;yield return null;
                check("sevenPortraitsGatherWithTwoReservedSlots",view.VisiblePaneCount==9&&view.VisiblePortraitCount==7&&view.VisibleFragmentCount==0&&
                    view.PaneOffsets.All(p=>p.sqrMagnitude<.01f)&&!entering.SequenceEqual(view.GeometryVertices));
                yield return new WaitForEndOfFrame();
                check("actualFullScreenLayerCoversHud",GuerrillaLayerCoversGui(view));
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"full-formation-peak.png"));yield return null;
                AdvanceGuerrillaEvidenceTo(view,.85f);yield return null;
                check("originalSoundAndActualWaveWaitForBreakup",view.SoundPlayCount==initialSounds&&!volley.Resolved&&volley.Hits.Count==0&&
                    guardHealth.All(p=>Battle.FindDefender(p.Key).Health==p.Value));
                AdvanceGuerrillaEvidenceTo(view,1.05f);yield return null;yield return null;
                check("portraitGlassFracturesAndOriginalSoundStartsOnce",view.VisibleFragmentCount>30&&view.SoundPlayCount==initialSounds+1);
                impactTargets=volley.Hits.Select(h=>h.TargetId).ToArray();impactDamage=volley.Hits.Select(h=>h.Damage).ToArray();totalDamage=impactDamage.Sum();
                check("fullFieldWaveDealsRecordedActualHealthLoss",volley.Resolved&&!volley.Cancelled&&volley.Hits.Count==guardHealth.Count&&
                    volley.Hits.All(h=>h.Damage>0&&guardHealth.ContainsKey(h.TargetId)&&Math.Abs(guardHealth[h.TargetId]-Battle.FindDefender(h.TargetId).Health-h.Damage)<.01f));
                Battle.SetPaused(true);Tick(0);yield return null;
                double clock=PresentationClock;float age=view.Age;var frozen=view.GeometryVertices.ToArray();
                var sound=GetComponents<AudioSource>().Single(s=>s.clip==Resources.Load<AudioClip>("GuerrillaOrder/followup-original"));
                int sample=sound.timeSamples;
                Tick(3);yield return new WaitForSecondsRealtime(.06f);
                check("pauseFreezesActualVerticesAndOriginalAudio",PresentationClock==clock&&view.Age==age&&view.SoundPaused&&
                    sample==sound.timeSamples&&frozen.SequenceEqual(view.GeometryVertices));
                check("pausedFailureDoesNotReplay",!UseCampaignOrder(false)&&view.PlayCount==1);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"fracture-paused.png"));yield return null;
                Battle.SetPaused(false);ToggleSpeed();clock=Battle.Clock;age=view.Age;Tick(.05f);yield return null;
                check("doubleSpeedKeepsFixedAnimationAndOriginalPitch",Math.Abs(Battle.Clock-clock-.1)<.0001&&Math.Abs(view.Age-age-.05f)<.0001f&&
                    Math.Abs(view.Age-1.10f)<.0001f&&Math.Abs(sound.pitch-1)<.0001f&&view.SoundPlayCount==initialSounds+1);ToggleSpeed();
                check("fragmentsDriftAndDissolveIndependently",view.VisibleFragmentCount>0&&!frozen.SequenceEqual(view.GeometryVertices));
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"dissolving.png"));yield return null;
                AdvanceGuerrillaEvidenceTo(view,1.30f);yield return null;yield return null;
                check("visibleGlobalImpactFeedbackUsesActualTotals",view.VisibleImpactTargetCount==volley.Hits.Count&&Math.Abs(view.DisplayedDamage-totalDamage)<.01f);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"full-field-hit-damage.png"));yield return null;
                AdvanceGuerrillaEvidenceTo(view,2.20f);yield return null;yield return null;
                check("portraitsDisappearBeforeLightTail",view.Playing&&view.VisiblePortraitCount==0&&view.VisibleFragmentCount==0);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"light-tail.png"));yield return null;
                AdvanceGuerrillaEvidenceTo(view,2.61f);yield return null;yield return null;
                check("completeMotionClears",!view.Playing&&view.GeometryVertices.Count==0&&view.SoundPlayCount==initialSounds+1);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"effect-ended.png"));yield return null;
                ReplaySandbox();while(ArtLoading)yield return null;yield return null;
                check("replayClearsEffect",!view.Playing&&double.IsNegativeInfinity(view.PendingUntil));
                charge=ChargeGuerrillaForEvidence(true);
                while(charge.MoveNext())yield return charge.Current;
                var terminalVolley=Battle.LatestGuerrillaOrder;
                check("removedFormationMemberStaysEmpty",view.SlotMembers[4]==null&&view.SlotMembers[3]=="CARD_M20"&&view.SlotMembers[7]==null&&view.SlotMembers[8]==null);
                AdvanceGuerrillaEvidenceTo(view,.65f);yield return null;yield return null;
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"missing-member-peak.png"));yield return null;
                check("missingPortraitDoesNotDeployPatriot",view.VisiblePortraitCount==6&&Battle.Units.All(u=>u.CardId!="CARD_M20"));
                AdvanceGuerrillaEvidenceTo(view,.72f);yield return null;
                int terminalSounds=view.SoundPlayCount;
                loopTestSettlementOpen=true;check("explicitTerminalFixture",TryTestSettlement(false));
                clock=Battle.Clock;var health=Battle.Units.Select(u=>u.Health).Concat(Battle.Defenders.Select(d=>d.Health)).ToArray();
                Tick(.30f);yield return null;yield return null;
                check("terminalCrossesSoundCueWithoutMoreCombat",view.Playing&&view.VisibleFragmentCount>30&&Battle.Clock==clock&&!SettlementVisible&&
                    view.SoundPlayCount==terminalSounds+1&&terminalVolley.Cancelled&&terminalVolley.Resolved&&terminalVolley.Hits.Count==0&&
                    health.SequenceEqual(Battle.Units.Select(u=>u.Health).Concat(Battle.Defenders.Select(d=>d.Health))));
                Tick(1.20f);yield return null;
                check("terminalKeepsOnlyLightTail",view.Playing&&view.VisiblePortraitCount==0&&Battle.Clock==clock);
                Tick(.50f);yield return null;yield return null;
                check("terminalMotionEnds",!view.Playing&&Battle.Clock==clock&&view.GeometryVertices.Count==0);
                Tick(.20f);yield return null;
                check("terminalTailCannotReappear",!view.Playing&&SettlementVisible&&view.SoundPlayCount==terminalSounds+1&&Battle.Clock==clock&&
                    health.SequenceEqual(Battle.Units.Select(u=>u.Health).Concat(Battle.Defenders.Select(d=>d.Health))));
                if(Environment.GetCommandLineArgs().Contains("-guerrillaOrderCaptureFrames"))
                {
                    ReplaySandbox();while(ArtLoading)yield return null;
                    charge=ChargeGuerrillaForEvidence(false);
                    while(charge.MoveNext())yield return charge.Current;
                    bool captureAtDoubleSpeed=Environment.GetCommandLineArgs().Contains("-guerrillaOrderCaptureDoubleSpeed");
                    if(captureAtDoubleSpeed&&SimulationSpeed!=2)ToggleSpeed();
                    string frames=Path.Combine(directory,"frames");Directory.CreateDirectory(frames);
                    int captureSounds=view.SoundPlayCount;
                    var capturedVolley=Battle.LatestGuerrillaOrder;
                    // Actual Player-rendered simulation frames, not interpolated concept art.
                    // Six simulation-zero lead frames, then the complete 2.6-second effect and clear tail.
                    for(int frame=0;frame<204;frame++)
                    {
                        if(frame>=6)Tick(1f/60);
                        if(frame==84&&captureAtDoubleSpeed)
                            check("doubleSpeedCaptureKeepsFullCinematicDuration",SimulationSpeed==2&&view.Playing&&Math.Abs(view.Age-79f/60)<.001f);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.Combine(frames,frame.ToString("D3")+".png"));yield return null;
                    }
                    check("capturedCompleteLiveMotion",!view.Playing&&view.GeometryVertices.Count==0&&view.SoundPlayCount==captureSounds+1&&
                        capturedVolley.Resolved&&!capturedVolley.Cancelled&&capturedVolley.Hits.Count>0);
                }
                complete=true;
            }
            finally
            {
                Time.timeScale=scale;passed&=complete;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new GuerrillaOrderEvidenceResult
                    {passed=passed,checks=checks.ToArray(),slots=slots,plays=view!=null?view.PlayCount:0,sounds=view!=null?view.SoundPlayCount:0,
                        impactTargets=impactTargets,impactDamage=impactDamage,totalDamage=totalDamage},true));
                Debug.Log("GUERRILLA_ORDER_PLAYER_CHECK passed="+passed);Application.Quit(passed?0:1);
            }
        }
    }

    public sealed class GuerrillaOrderSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||GuerrillaAutoFiveSmokeDriver.Requested||!Environment.GetCommandLineArgs().Contains("-guerrillaOrderSmokeTest"))return;
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
