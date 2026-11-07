using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static readonly string[] AutoFiveRoster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M08","CARD_TALULAH_TRIAL"};
        [Serializable]sealed class AutoFiveStrikeEvidence
        {
            public int index;
            public float at,weight;
            public string[] targets;
            public float[] damage;
        }
        [Serializable]sealed class AutoFiveEvidenceResult
        {
            public bool passed;
            public string fixture="Explicit Development-only Level9 nine-kind inventory, seven native guerrillas plus M08 and Talulah; normal art Prepare, free transfer transactions, Begin grant and actual M03 deployment. Runtime guard HP1000, guard attacks and skill SP delayed, source ordinary attacks delayed, gramophone attacks/heals delayed to isolate HP. No military-order balance, contact credits, hits or damage are injected. Screenshots/optional 246-frame capture are actual Player renders; system audio recording is not claimed.";
            public string[] checks,slots;
            public string talulahPortrait;
            public AutoFiveStrikeEvidence[] strikes;
            public float totalDamage,maxDoubleSpeedAgeError;
            public int plays,sounds,slashSounds,frameCount,gpuContributionPixels;
        }
        IEnumerator PrepareAutoFiveInventory()
        {
            if(!SynergySandbox||!Loop.CanShop)throw new InvalidOperationException("Automatic five-strike fixture requires sandbox preparation.");
            var cards=AutoFiveRoster.Select(id=>shopDefinitions[id].Card).ToArray();
            yield return Prepare(Plan(shopMapCopy.defenders,cards,true),()=>
            {
                shop.Preset(AutoFiveRoster.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
                shopLineup.Clear();shopLineup.AddRange(AutoFiveRoster);SyncShopDeck(false);SetShopVisible(true);inspectedSynergy=null;
            },"准备自动五段游击验收");
            while(ArtLoading||ShopPanelTransitioning)yield return null;
            if(!SetGuerrillaTransfer("CARD_M08",true)||!SetGuerrillaTransfer("CARD_TALULAH_TRIAL",true))
                throw new InvalidOperationException("Prepared M08/Talulah transfers were rejected.");
        }
        IEnumerator BeginAutoFiveEvidence(int speed)
        {
            PrepareNextLoopBattle();while(ArtLoading||ShopPanelTransitioning)yield return null;
            if(Loop.CanShop||Battle.GuerrillaTier!=9||Battle.Orders!=0)throw new InvalidOperationException("Prepared nine-member encounter did not enter Ready cleanly.");
            Battle.Begin();DurableGuerrillaHighTierEvidenceGuards();
            // Enough HP for this single-source five-strike fixture, with visible health-bar steps.
            foreach(var guard in Battle.Defenders)guard.Health=guard.MaxHealth=1000;
            foreach(var device in Battle.Gramophones)device.NextAttack=device.NextHeal=Battle.Clock+1000;
            if(SimulationSpeed!=speed)ToggleSpeed();
        }
        DeployedUnitState DeployAutoFiveEvidence()
        {
            var receipt=Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Entry,"CARD_M03");
            if(!receipt.Success)throw new InvalidOperationException("Actual M03 deployment failed: "+receipt.Failure);
            StopGuerrillaHighTierEvidenceOrdinaryAttacks();return Battle.FindUnit(receipt.EntityId);
        }
        static Dictionary<string,float> AutoFiveHealth(ManualBattle battle)=>battle.Defenders.Where(d=>d.Alive).ToDictionary(d=>d.EntityId,d=>d.Health);
        static bool AutoFiveSameHealth(ManualBattle battle,Dictionary<string,float> health)=>health.All(p=>Mathf.Abs(battle.FindDefender(p.Key).Health-p.Value)<.001f);
        static bool AutoFiveRecordedHealth(ManualBattle battle,GuerrillaOrderVolley volley,Dictionary<string,float> initial)
        {
            return volley.Hits.Count==initial.Count&&volley.Hits.All(h=>initial.ContainsKey(h.TargetId)&&
                Mathf.Abs(initial[h.TargetId]-battle.FindDefender(h.TargetId).Health-h.Damage)<.01f);
        }
        static AudioSource AutoFiveTrack(GuerrillaOrderView view,string name)=>
            (AudioSource)typeof(GuerrillaOrderView).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        static Texture2D AutoFiveReadback(GuerrillaOrderView view,string path=null)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new InvalidOperationException("Automatic five-strike evidence requires a real GPU.");
            var target=new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32);var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;
            try
            {
                target.Create();view.RenderTo(target);RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                if(path!=null)File.WriteAllBytes(path,image.EncodeToPNG());return image;
            }
            catch{Destroy(image);throw;}
            finally{RenderTexture.active=previous;target.Release();Destroy(target);}
        }
        static int AutoFivePixelDifference(Color32[] first,Color32[] second)
        {
            int changed=0;for(int i=0;i<first.Length;i++)
                if(Math.Abs(first[i].r-second[i].r)+Math.Abs(first[i].g-second[i].g)+Math.Abs(first[i].b-second[i].b)+Math.Abs(first[i].a-second[i].a)>12)changed++;
            return changed;
        }
        internal IEnumerator VerifyGuerrillaAutoFive(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var result=new AutoFiveEvidenceResult();var checks=new List<string>();bool passed=true,complete=false;float previousScale=Time.timeScale;
            var view=GetComponent<GuerrillaOrderView>();
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("GUERRILLA_AUTO_FIVE_EVENT "+name+"="+value);};
            try
            {
                Time.timeScale=0;yield return PrepareAutoFiveInventory();yield return BeginAutoFiveEvidence(1);
                check("preparedNineKindsAndTwoRealTransfers",shop.Level==9&&shopLineup.Count==9&&Battle.GuerrillaMemberIds.Count==9&&PreparedGuerrillaTransferIds.Count==2);
                check("beginGrantsExactlyTwoOnce",Battle.Orders==2);Battle.Begin();check("repeatedBeginDoesNotRefill",Battle.Orders==2);
                Tick(.1f);check("fullBalanceWithoutSourceDoesNotAutoSpend",Battle.Orders==2&&Battle.LatestGuerrillaOrder==null&&view.PlayCount==0);
                var source=DeployAutoFiveEvidence();Tick(.05f);var volley=Battle.LatestGuerrillaOrder;
                check("realRemoteSourceAutomaticallyCommitsExactlyOnce",volley!=null&&volley.SourceIds.Count==1&&volley.SourceIds[0]==source.EntityId&&
                    Battle.Orders==0&&Battle.OrdersUsed==1&&view.PlayCount==1&&source.OrdinaryAttacksFired==0&&Battle.CombatShots.Count==0);
                if(volley==null)throw new InvalidOperationException("Automatic order was not committed.");
                var initial=AutoFiveHealth(Battle);check("fourSavedGuardsAreAlive",initial.Count==4);
                var camera=Camera.main;Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
                AdvanceGuerrillaEvidenceTo(view,.65f);yield return null;
                result.slots=view.SlotMembers.ToArray();int talulahSlot=Array.IndexOf(result.slots,"CARD_TALULAH_TRIAL");
                var expected=OriginalTalulahArt.For(shopDefinitions["CARD_TALULAH_TRIAL"].Card)?.Portrait;
                var portraits=(Sprite[])typeof(GuerrillaOrderView).GetField("extensions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                var assigned=talulahSlot>=7?portraits[talulahSlot-7]:null;result.talulahPortrait=assigned!=null?assigned.name:null;
                check("actualTransferPaneUsesOriginalTalulah",talulahSlot==8&&expected!=null&&assigned==expected&&assigned.name=="TalulahOriginal/portrait"&&view.VisiblePortraitCount==9);
                Color32[] peakPixels;var peak=AutoFiveReadback(view,Path.Combine(directory,"nine-portrait-layer.png"));try{peakPixels=peak.GetPixels32();}finally{Destroy(peak);}
                yield return GuerrillaHighTierEvidenceFrame(directory,"nine-portrait-peak");
                yield return new WaitForEndOfFrame();check("actualFullScreenLayerCoversHud",GuerrillaLayerCoversGui(view));

                for(int strike=0;strike<5;strike++)
                {
                    if(strike==4)
                    {
                        AdvanceGuerrillaEvidenceTo(view,2.4f);yield return null;var heldHp=AutoFiveHealth(Battle);
                        check("fourHitsThenRealChargeWithoutEarlyFifthDamage",volley.CompletedStrikes==4&&!volley.Resolved&&view.ChargeProgress>0&&view.DisplayedStrikeCount==4);
                        var beforePause=AutoFiveReadback(view,Path.Combine(directory,"charge-layer.png"));var chargePixels=beforePause.GetPixels32();Destroy(beforePause);
                        var fx=view.ScreenEffect;Vector2 offset=fx.Offset;float zoom=fx.Zoom,roll=fx.Roll;float age=view.Age;
                        Battle.SetPaused(true);Tick(1);yield return new WaitForSecondsRealtime(.06f);
                        var pauseImage=AutoFiveReadback(view);bool equal=chargePixels.SequenceEqual(pauseImage.GetPixels32());Destroy(pauseImage);
                        check("pausedChargeFreezesActualGpuAndScreenFx",view.Age==age&&equal&&fx.Offset==offset&&fx.Zoom==zoom&&fx.Roll==roll&&AutoFiveSameHealth(Battle,heldHp));
                        check("screenFeedbackNeverMovesLogicalCamera",camera.transform.position==cameraPosition&&camera.transform.rotation==cameraRotation);
                        Battle.SetPaused(false);yield return GuerrillaHighTierEvidenceFrame(directory,"final-charge");
                    }
                    float at=GuerrillaOrderVolley.StrikeTime(strike);AdvanceGuerrillaEvidenceTo(view,at-.01f);
                    var before=AutoFiveHealth(Battle);check("strike"+(strike+1)+"WaitsForItsOwnCue",volley.CompletedStrikes==strike);
                    AdvanceGuerrillaEvidenceTo(view,at+.01f);var hit=volley.Strikes.LastOrDefault();
                    check("strike"+(strike+1)+"ActuallyDamagesEverySavedGuard",volley.CompletedStrikes==strike+1&&hit!=null&&hit.Index==strike&&hit.Hits.Count==initial.Count&&
                        hit.Hits.All(h=>h.Damage>0&&Mathf.Abs(before[h.TargetId]-Battle.FindDefender(h.TargetId).Health-h.Damage)<.01f)&&AutoFiveRecordedHealth(Battle,volley,initial));
                    check("strike"+(strike+1)+"GpuCountersReadActualCumulativeLoss",view.DisplayedStrikeCount==strike+1&&view.VisibleImpactTargetCount==initial.Count&&Mathf.Abs(view.DisplayedDamage-volley.Hits.Sum(h=>h.Damage))<.01f);
                    if(strike==0)
                    {
                        yield return GuerrillaHighTierEvidenceFrame(directory,"first-real-global-hit");
                        AdvanceGuerrillaEvidenceTo(view,1.02f);yield return null;
                        var followup=AutoFiveTrack(view,"sound");var slash=AutoFiveTrack(view,"slashSound");
                        check("twoProductionAudioSourcesPlayOriginalAndStrikeTrack",followup!=null&&slash!=null&&followup.clip==Resources.Load<AudioClip>("GuerrillaOrder/followup-original")&&
                            slash.clip==Resources.Load<AudioClip>("GuerrillaOrder/five-slash-charge")&&followup.isPlaying&&slash.isPlaying&&followup.pitch==1&&slash.pitch==1);
                        Battle.SetPaused(true);Tick(0);yield return null;int firstSample=followup.timeSamples,secondSample=slash.timeSamples;
                        Tick(2);yield return new WaitForSecondsRealtime(.08f);
                        check("bothRealAudioSamplePositionsFreezeDuringPause",view.SoundPaused&&view.SlashSoundPaused&&followup.timeSamples==firstSample&&slash.timeSamples==secondSample);
                        Battle.SetPaused(false);Tick(0);
                    }
                    if(strike==1)
                    {
                        var beforeRender=AutoFiveHealth(Battle);int plays=view.PlayCount,sounds=view.SoundPlayCount,slashSounds=view.SlashSoundPlayCount;
                        var actual=AutoFiveReadback(view,Path.Combine(directory,"second-strike-layer.png"));var actualPixels=actual.GetPixels32();Destroy(actual);
                        view.BindVolley(null);var without=AutoFiveReadback(view);result.gpuContributionPixels=AutoFivePixelDifference(actualPixels,without.GetPixels32());Destroy(without);
                        view.BindVolley(volley);var repeated=AutoFiveReadback(view);bool unchanged=actualPixels.SequenceEqual(repeated.GetPixels32());Destroy(repeated);
                        check("realStrikeAndNumbersContributeProductionGpuPixels",result.gpuContributionPixels>100&&unchanged);
                        check("repeatedRenderingCannotDamageOrReplayTracks",AutoFiveSameHealth(Battle,beforeRender)&&view.PlayCount==plays&&view.SoundPlayCount==sounds&&view.SlashSoundPlayCount==slashSounds);
                    }
                }
                result.strikes=volley.Strikes.Select(s=>new AutoFiveStrikeEvidence{index=s.Index,at=s.At,weight=s.Weight,targets=s.Hits.Select(h=>h.TargetId).ToArray(),damage=s.Hits.Select(h=>h.Damage).ToArray()}).ToArray();
                result.totalDamage=volley.Hits.Sum(h=>h.Damage);
                check("finalFortyPercentPreservesOneOriginalMitigatedBudget",volley.Strikes[4].Hits.All(h=>
                {
                    float first=volley.Strikes[0].Hits.Single(p=>p.TargetId==h.TargetId).Damage;
                    float total=volley.Hits.Single(p=>p.TargetId==h.TargetId).Damage;
                    return Mathf.Abs(h.Damage-first*(.4f/.15f))<.4f&&Mathf.Abs(total-first/.15f)<.5f;
                }));
                check("finalStrikeResolvesOnceWithoutRecursiveIncome",volley.Resolved&&!volley.Cancelled&&Battle.Orders==0&&Battle.OrdersUsed==1&&view.PlayCount==1&&source.OrdinaryAttacksFired==0);
                check("finalImpactHasThreeFrameVisualHold",view.ScreenEffect!=null&&view.ScreenEffect.Holding);
                yield return GuerrillaHighTierEvidenceFrame(directory,"fifth-impact-hold");
                AdvanceGuerrillaEvidenceTo(view,2.87f);yield return null;
                check("finalImpactHasRealScreenRecoil",view.ScreenEffect!=null&&!view.ScreenEffect.Holding&&view.ScreenEffect.Offset.sqrMagnitude>.1f&&view.ScreenEffect.Zoom>1&&
                    camera.transform.position==cameraPosition&&camera.transform.rotation==cameraRotation);
                yield return GuerrillaHighTierEvidenceFrame(directory,"fifth-impact-recoil");
                AdvanceGuerrillaEvidenceTo(view,3.56f);yield return null;
                check("finishedBatchClearsSoundAndAllScreenTransforms",!view.Playing&&!view.SoundPlaying&&!view.SlashSoundPlaying&&view.ScreenEffect.Offset==Vector2.zero&&view.ScreenEffect.Zoom==1&&view.ScreenEffect.Roll==0&&!view.ScreenEffect.Holding);
                yield return GuerrillaHighTierEvidenceFrame(directory,"effect-ended");

                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("reopenClearsOrderViewAndPreservesPreparedTransferChoices",Battle.LatestGuerrillaOrder==null&&Battle.Orders==0&&!view.Playing&&view.SlotMembers.All(string.IsNullOrEmpty)&&PreparedGuerrillaTransferIds.Count==2);
                yield return BeginAutoFiveEvidence(2);int baselinePlays=view.PlayCount;source=DeployAutoFiveEvidence();Tick(.025f);var doubled=Battle.LatestGuerrillaOrder;
                if(doubled==null)throw new InvalidOperationException("Two-times automatic batch did not start.");
                var doubleInitial=AutoFiveHealth(Battle);check("doubleSpeedCommitsOnlyOneBatchAtZeroPresentationAge",view.PlayCount==baselinePlays+1&&Mathf.Abs(view.Age)<.0001f&&Battle.OrdersUsed==1&&Battle.Orders==0);
                bool capture=GuerrillaAutoFiveSmokeDriver.CaptureRequested;int observedStrikes=0;
                if(capture)
                {
                    string frames=Path.Combine(directory,"frames");Directory.CreateDirectory(frames);
                    for(int frame=0;frame<246;frame++)
                    {
                        double beforeClock=Battle.Clock;float beforeAge=view.Age;
                        if(frame>=6)Tick(1f/60);
                        if(frame==6)check("capturedCombatRunsTwoTimesWhilePresentationRunsOneTimes",Math.Abs(Battle.Clock-beforeClock-2f/60)<.0001&&Mathf.Abs(view.Age-beforeAge-1f/60)<.0001f);
                        float expectedAge=frame<6?0:(frame-5)/60f;
                        if(expectedAge<GuerrillaOrderVolley.PresentationDuration)result.maxDoubleSpeedAgeError=Mathf.Max(result.maxDoubleSpeedAgeError,Mathf.Abs(view.Age-expectedAge));
                        if(frame==44)
                        {
                            var peak2=AutoFiveReadback(view,Path.Combine(directory,"double-speed-portrait-layer.png"));
                            check("oneAndTwoSpeedPeakPixelsAreIdentical",peakPixels.SequenceEqual(peak2.GetPixels32()));Destroy(peak2);
                        }
                        if(doubled.CompletedStrikes!=observedStrikes)
                        {
                            observedStrikes=doubled.CompletedStrikes;
                            check("doubleSpeedActualStrike"+observedStrikes,doubled.Strikes.Last().Hits.Count==4&&AutoFiveRecordedHealth(Battle,doubled,doubleInitial));
                        }
                        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,frame.ToString("D3")+".png"));yield return null;result.frameCount++;
                    }
                }
                else
                {
                    double beforeClock=Battle.Clock;float beforeAge=view.Age;Tick(.04f);
                    check("combatRunsTwoTimesWhilePresentationRunsOneTimes",Math.Abs(Battle.Clock-beforeClock-.08)<.0001&&Mathf.Abs(view.Age-beforeAge-.04f)<.0001f);
                    AdvanceGuerrillaEvidenceTo(view,.65f);var peak2=AutoFiveReadback(view);check("oneAndTwoSpeedPeakPixelsAreIdentical",peakPixels.SequenceEqual(peak2.GetPixels32()));Destroy(peak2);
                    for(int i=0;i<5;i++)
                    {
                        AdvanceGuerrillaEvidenceTo(view,GuerrillaOrderVolley.StrikeTime(i)+.01f);
                        check("doubleSpeedActualStrike"+(i+1),doubled.CompletedStrikes==i+1&&doubled.Strikes.Last().Hits.Count==4&&AutoFiveRecordedHealth(Battle,doubled,doubleInitial));
                    }
                    AdvanceGuerrillaEvidenceTo(view,3.56f);
                }
                check("twoTimesCombatKeepsFullFiveStrikePresentationAndOneTimesPitch",SimulationSpeed==2&&result.maxDoubleSpeedAgeError<.001f&&doubled.CompletedStrikes==5&&doubled.Resolved&&!doubled.Cancelled&&
                    !view.Playing&&view.PlayCount==baselinePlays+1&&Battle.Orders==0&&source.OrdinaryAttacksFired==0&&AutoFiveTrack(view,"sound").pitch==1&&AutoFiveTrack(view,"slashSound").pitch==1);
                check("oneAndTwoSpeedFinalActualDamageAreEqual",Mathf.Abs(doubled.Hits.Sum(h=>h.Damage)-result.totalDamage)<.01f);

                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;yield return BeginAutoFiveEvidence(1);
                DeployAutoFiveEvidence();Tick(.05f);var interrupted=Battle.LatestGuerrillaOrder;AdvanceGuerrillaEvidenceTo(view,.91f);
                var terminalHp=AutoFiveHealth(Battle);var applied=interrupted.Hits.Select(h=>h.Damage).ToArray();
                // Explicit terminal fixture only: stop the encounter after one real strike.
                typeof(ManualBattle).GetProperty("Defense").SetValue(Battle,0);Tick(.01f);double terminalClock=Battle.Clock;
                AdvanceGuerrillaEvidenceTo(view,2.4f);yield return null;
                check("terminalCancelsOnlyFutureStrikesAndCharge",interrupted.Resolved&&interrupted.Cancelled&&interrupted.CompletedStrikes==1&&view.DisplayedStrikeCount==1&&view.ChargeProgress==0&&
                    AutoFiveSameHealth(Battle,terminalHp)&&applied.SequenceEqual(interrupted.Hits.Select(h=>h.Damage))&&Battle.Clock==terminalClock);
                Tick(4);yield return null;
                check("terminalVisualTailEndsWithoutNewDamage",!view.Playing&&SettlementVisible&&interrupted.CompletedStrikes==1&&AutoFiveSameHealth(Battle,terminalHp)&&Battle.Clock==terminalClock);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("finalRestartClearsAllVolleysSoundAndShake",Battle.LatestGuerrillaOrder==null&&Battle.Orders==0&&!view.Playing&&!view.SoundPlaying&&!view.SlashSoundPlaying&&
                    view.SlotMembers.All(string.IsNullOrEmpty)&&view.ScreenEffect.Offset==Vector2.zero&&view.ScreenEffect.Zoom==1);
                complete=true;
            }
            finally
            {
                Time.timeScale=previousScale;result.passed=passed&&complete;result.checks=checks.ToArray();result.plays=view!=null?view.PlayCount:0;
                result.sounds=view!=null?view.SoundPlayCount:0;result.slashSounds=view!=null?view.SlashSoundPlayCount:0;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("GUERRILLA_AUTO_FIVE_PLAYER_CHECK passed="+result.passed);
            }
        }
    }

    public sealed class GuerrillaAutoFiveSmokeDriver:MonoBehaviour
    {
        static bool started;
        public static bool Requested
        {
            get
            {
                var args=Environment.GetCommandLineArgs();return !args.Contains("-guerrillaOrderRenderProbe")&&
                    args.Any(a=>a=="-guerrillaAutoFiveSmokeTest"||a=="-guerrillaOrderSmokeTest"||a=="-guerrillaHighTierSmokeTest");
            }
        }
        public static bool CaptureRequested=>Environment.GetCommandLineArgs().Any(a=>a=="-guerrillaFiveCaptureFrames"||a=="-guerrillaOrderCaptureFrames"||a=="-guerrillaHighTierCaptureFrames");
        [Serializable]sealed class DriverResult {public bool passed;public string error;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Requested)return;
            started=true;var host=new GameObject("Guerrilla automatic five-strike explicit Player verification");DontDestroyOnLoad(host);host.AddComponent<GuerrillaAutoFiveSmokeDriver>();
        }
        IEnumerator Run(string directory)
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("GuerrillaTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            if(controller==null||controller.ArtLoading)throw new InvalidOperationException("GuerrillaTrial did not load for automatic five-strike evidence.");
            yield return controller.VerifyGuerrillaAutoFive(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("Artifacts/guerrilla-auto-five-player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Run(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){Debug.LogException(cleanup);}}
                    Debug.LogException(failure);File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());
                    string result=Path.Combine(directory,"result.json");
                    if(!File.Exists(result)||JsonUtility.FromJson<DriverResult>(File.ReadAllText(result)).passed)
                        File.WriteAllText(result,JsonUtility.ToJson(new DriverResult{passed=false,error=failure.Message},true));
                    Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            string json=File.ReadAllText(Path.Combine(directory,"result.json"));Application.Quit(JsonUtility.FromJson<DriverResult>(json).passed?0:1);
        }
    }
}
