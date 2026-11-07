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
        static readonly string[] HighTierEvidenceRoster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        [Serializable]sealed class GuerrillaHighTierGuiStep {public int id;public string name;public float x,y,width,height;}
        [Serializable]sealed class GuerrillaHighTierEvidenceResult
        {
            public bool passed;
            public string fixture="Explicit Level9 nine-kind Shop.Preset; saved defender positions/routes, HP1000000, attack delayed and skill SP disabled. Military orders, deployments, ordinary contact income, spending and damage use production operations. Screenshots and optional frames are actual Player renders; no system audio recording is claimed.";
            public string[] checks,slots,impactTargets;
            public float[] healthBefore,healthAfter,impactDamage;
            public float totalDamage;
            public int tier,eightTierOrdersAfterContact,nineTierCasts,earnedOrders,plays,sounds;
        }
        int highTierGuiSequence;
        IEnumerator GuerrillaHighTierGuiClick(string directory,string name,Rect rect,Func<bool> accepted)
        {
            if(rect.width<=0||rect.height<=0)throw new InvalidOperationException("Guerrilla GUI target is not visible: "+name);
            var point=TacticalViewport.Current.ToScreen(rect.center);int id=++highTierGuiSequence;
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new GuerrillaHighTierGuiStep
                {id=id,name=name,x=point.x,y=Screen.height-point.y,width=Screen.width,height=Screen.height},true));
            // The external operator clicks the actual Player and then acknowledges this id.
            // This coroutine never dispatches a synthetic GUI event or calls the row action.
            double deadline=Time.realtimeSinceStartupAsDouble+300;
            for(;;)
            {
                string ack=Path.Combine(directory,"gui-ack.txt");
                if(File.Exists(ack)&&File.ReadAllText(ack).Trim()==id.ToString()&&accepted())break;
                if(Time.realtimeSinceStartupAsDouble>deadline)throw new InvalidOperationException("Actual Player GUI click timed out: "+name);
                yield return null;
            }
            yield return null;yield return null;
        }
        IEnumerator PrepareGuerrillaHighTierEvidenceRoster()
        {
            if(!SynergySandbox||!Loop.CanShop)throw new InvalidOperationException("High-tier inventory fixture requires sandbox preparation.");
            var cards=HighTierEvidenceRoster.Select(id=>shopDefinitions[id].Card).ToArray();
            yield return Prepare(Plan(shopMapCopy.defenders,cards,true),()=>
            {
                shop.Preset(HighTierEvidenceRoster.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
                shopLineup.Clear();shopLineup.AddRange(HighTierEvidenceRoster);SyncShopDeck(false);SetShopVisible(true);inspectedSynergy=null;
            },"准备高档游击队验收夹具");
            while(ArtLoading||ShopPanelTransitioning)yield return null;
        }
        void DurableGuerrillaHighTierEvidenceGuards()
        {
            foreach(var d in Battle.Defenders)
            {d.Health=d.MaxHealth=1000000;d.NextAttackTime=Battle.Clock+1000;d.SkillSP=-1000;}
        }
        IEnumerator DeployGuerrillaHighTierEvidence(string id)
        {
            for(int i=0;i<240&&Battle.Phase==BattlePhase.Running;i++)
            {
                if(Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Entry,id).Success)yield break;
                Tick(.05f);if(i%10==0)yield return null;
            }
            throw new InvalidOperationException("High-tier real deployment failed: "+id);
        }
        void StopGuerrillaHighTierEvidenceOrdinaryAttacks()
        {foreach(var u in Battle.Units)u.NextAttackTime=Battle.Clock+1000;}
        IEnumerator GuerrillaHighTierEvidenceFrame(string directory,string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}
        internal IEnumerator VerifyGuerrillaHighTier(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var result=new GuerrillaHighTierEvidenceResult();var checks=new List<string>();bool passed=true,complete=false;
            float oldScale=Time.timeScale;var view=GetComponent<GuerrillaOrderView>();
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("GUERRILLA_HIGH_TIER_EVENT "+name+"="+value);};
            try
            {
                yield return new WaitForSecondsRealtime(1);Time.timeScale=0;
                yield return PrepareGuerrillaHighTierEvidenceRoster();
                check("explicitNineKindInventoryWithinNineCopyStock",shop.Level==9&&shopLineup.Count==9&&HighTierEvidenceRoster.All(id=>shop.Bench.Count(p=>p.Id==id)==1&&shop.Available(id)>=0));
                yield return GuerrillaHighTierGuiClick(directory,"open-construction",CampaignConstructToggleRect,()=>campaignConstructOpen);
                yield return GuerrillaHighTierGuiClick(directory,"open-guerrilla-transfers",GuerrillaTransferTabRect,()=>GuerrillaTransfersVisible);
                yield return GuerrillaHighTierGuiClick(directory,"assign-first-transfer",GuerrillaTransferButtonRect("CARD_M07"),()=>IsGuerrillaTransfer("CARD_M07")&&Battle.GuerrillaTier==8);
                check("actualGuiFirstTransferMakesEightTier",PreparedGuerrillaTransferIds.SequenceEqual(new[]{"CARD_M07"})&&Battle.Orders==0);
                yield return GuerrillaHighTierEvidenceFrame(directory,"prepare-eight-tier");
                yield return GuerrillaHighTierGuiClick(directory,"assign-second-transfer",GuerrillaTransferButtonRect("CARD_M08"),()=>IsGuerrillaTransfer("CARD_M08")&&Battle.GuerrillaTier==9);
                check("actualGuiSecondTransferMakesNineTier",GuerrillaTransferIds.Count==2&&PreparedGuerrillaTransferIds.Count==2&&Battle.GuerrillaMemberIds.Count==9);
                yield return GuerrillaHighTierEvidenceFrame(directory,"prepare-nine-tier");
                yield return GuerrillaHighTierGuiClick(directory,"close-construction",new Rect(CampaignConstructRect.xMax-69,CampaignConstructRect.y+10,53,27),()=>!campaignConstructOpen);

                // Eight-tier boundary: starting two points are insufficient until one actual contact.
                check("cancelSecondTransferInPreparation",SetGuerrillaTransfer("CARD_M08",false)&&Battle.GuerrillaTier==8);
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin eight-tier fixture.");
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("readyHasNoRepeatedStartingGrant",Battle.Orders==0);Battle.Begin();DurableGuerrillaHighTierEvidenceGuards();
                yield return DeployGuerrillaHighTierEvidence("CARD_M03");int beforePlays=view.PlayCount;
                check("eightTierEarlyCastCannotSpendTwo",Battle.Orders==2&&!UseCampaignOrder(false)&&Battle.Orders==2&&view.PlayCount==beforePlays);
                for(int i=0;i<1800&&Battle.Orders<3&&Battle.Phase==BattlePhase.Running;i++){Tick(.05f);if(i%10==0)yield return null;}
                result.eightTierOrdersAfterContact=Battle.Orders;
                check("eightTierOneRealContactSuppliesThirdPoint",Battle.Orders==3&&Battle.CombatShots.Any(s=>!s.FromDefender&&!s.SynergyOrder&&s.Applied));
                StopGuerrillaHighTierEvidenceOrdinaryAttacks();
                for(int i=0;i<160&&Battle.CombatShots.Any(s=>!s.Resolved);i++){Tick(.05f);if(i%10==0)yield return null;}
                check("eightTierFullOnlyCostsThreeAtOnePointEightAttack",!UseCampaignOrder(true)&&Battle.Orders==3&&UseCampaignOrder(false)&&Battle.Orders==0&&GuerrillaRules.ForTier(Battle.GuerrillaTier).AttackMultiplier==1.8f);
                Tick(2.61f);yield return null;

                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("sameShopReplayKeepsTransferSelection",IsGuerrillaTransfer("CARD_M07")&&!IsGuerrillaTransfer("CARD_M08"));
                check("secondTransferRestoresNineTier",SetGuerrillaTransfer("CARD_M08",true)&&Battle.GuerrillaTier==9);
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin nine-tier fixture.");
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                Battle.Begin();DurableGuerrillaHighTierEvidenceGuards();
                if(SimulationSpeed!=2)ToggleSpeed();
                check("nineTierStartsTwoAndEmptySourceDoesNotSpend",Battle.Orders==2&&!UseCampaignOrder(false)&&Battle.Orders==2);
                yield return DeployGuerrillaHighTierEvidence("CARD_M03");
                check("sourceHasNotAttackedOrAcquiredLocalTarget",Battle.Units.Single().OrdinaryAttacksFired==0&&!Battle.Units.Single().HasCombatTarget);
                StopGuerrillaHighTierEvidenceOrdinaryAttacks();
                check("nineTierRemoteEarlyCastSpendsExactlyTwo",UseCampaignOrder(false)&&Battle.Orders==0&&GuerrillaRules.ForTier(9).AttackMultiplier==3);
                var volley=Battle.LatestGuerrillaOrder;var hp=Battle.Defenders.Where(d=>d.Alive).ToDictionary(d=>d.EntityId,d=>d.Health);
                result.slots=view.SlotMembers.ToArray();
                check("combatTransferSnapshotCannotChange",!SetGuerrillaTransfer("CARD_M07",false)&&Battle.IsGuerrillaMember("CARD_M07"));
                AdvanceGuerrillaEvidenceTo(view,.65f);yield return null;yield return null;
                check("sevenFixedNativeSlotsAndTwoActualTransferPortraits",view.VisiblePortraitCount==9&&view.SlotMembers.Count==9&&view.SlotMembers[7]=="CARD_M07"&&view.SlotMembers[8]=="CARD_M08");
                yield return GuerrillaHighTierEvidenceFrame(directory,"nine-portrait-peak");
                AdvanceGuerrillaEvidenceTo(view,.85f);
                check("globalWaveWaitsForActualPointNineImpact",!volley.Resolved&&volley.Hits.Count==0&&hp.All(p=>Battle.FindDefender(p.Key).Health==p.Value));
                Battle.SetPaused(true);float frozen=view.Age;double clock=Battle.Clock;Tick(2);yield return null;
                check("preImpactPauseFreezesBothClocksAndHp",view.Age==frozen&&Battle.Clock==clock&&!volley.Resolved&&hp.All(p=>Battle.FindDefender(p.Key).Health==p.Value));
                Battle.SetPaused(false);AdvanceGuerrillaEvidenceTo(view,.89f);
                check("immediatePreImpactWindowStillHasOriginalHp",!volley.Resolved&&hp.All(p=>Battle.FindDefender(p.Key).Health==p.Value));
                // The authored defender-owned gramophone may heal the damage later. Record
                // real HP immediately around the wave, without disabling the actual device.
                AdvanceGuerrillaEvidenceTo(view,.91f);
                result.impactTargets=volley.Hits.Select(h=>h.TargetId).ToArray();result.impactDamage=volley.Hits.Select(h=>h.Damage).ToArray();
                result.healthBefore=result.impactTargets.Select(id=>hp[id]).ToArray();result.healthAfter=result.impactTargets.Select(id=>Battle.FindDefender(id).Health).ToArray();result.totalDamage=result.impactDamage.Sum();
                check("nineTierEveryLivingDefenderActuallyLosesRecordedHp",volley.Resolved&&!volley.Cancelled&&volley.Hits.Count==hp.Count&&volley.Hits.All(h=>h.Damage>0&&Math.Abs(hp[h.TargetId]-Battle.FindDefender(h.TargetId).Health-h.Damage)<.01f));
                AdvanceGuerrillaEvidenceTo(view,1.05f);yield return null;yield return null;
                var sound=GetComponents<AudioSource>().Single(s=>s.clip==Resources.Load<AudioClip>("GuerrillaOrder/followup-original"));int sounds=view.SoundPlayCount;
                Battle.SetPaused(true);Tick(0);int samples=sound.timeSamples;Tick(3);yield return new WaitForSecondsRealtime(.06f);
                check("originalResourceAudioActuallyPauses",view.SoundPaused&&sound.timeSamples==samples&&Math.Abs(sound.volume-.55f)<.0001f);
                Battle.SetPaused(false);clock=Battle.Clock;float age=view.Age;Tick(.05f);yield return null;
                check("twoTimesCombatKeepsOneTimesMotionAndOriginalPitch",SimulationSpeed==2&&Math.Abs(Battle.Clock-clock-.1)<.0001&&Math.Abs(view.Age-age-.05f)<.0001f&&Math.Abs(sound.pitch-1)<.0001f&&view.SoundPlayCount==sounds);
                AdvanceGuerrillaEvidenceTo(view,1.3f);yield return null;yield return null;
                check("globalNumbersUseRealHitTotals",view.VisibleImpactTargetCount==volley.Hits.Count&&Math.Abs(view.DisplayedDamage-result.totalDamage)<.01f);
                yield return GuerrillaHighTierEvidenceFrame(directory,"nine-tier-global-hit");
                AdvanceGuerrillaEvidenceTo(view,2.61f);Battle.Begin();SyncShopDeck(false);
                check("sameBattleBeginOrDeckSyncCannotRefillStartingGrant",Battle.Orders==0&&ReferenceEquals(volley,Battle.LatestGuerrillaOrder));

                // Restore normal attacks and deploy distinct ordinary kinds. No Orders field,
                // contact-credit collection or hit record is edited to reach the fifth cast.
                foreach(var u in Battle.Units)u.NextAttackTime=0;
                foreach(string id in HighTierEvidenceRoster.Where(id=>id!="CARD_M03"&&id!="CARD_M20"))
                {yield return DeployGuerrillaHighTierEvidence(id);Tick(.2f);}
                foreach(string id in new[]{"CARD_M07","CARD_M08"}){yield return DeployGuerrillaHighTierEvidence(id);Tick(.2f);}
                for(int i=0;i<2400&&Battle.OrdersUsed<5&&Battle.Phase==BattlePhase.Running;i++)
                {if(Battle.Orders>=2&&UseCampaignOrder(false))Tick(2.61f);else Tick(.05f);if(i%10==0)yield return null;}
                check("fifthFullOrderSucceedsFromActualFiniteContactIncome",Battle.OrdersUsed>=5&&Battle.CombatShots.Any(s=>!s.FromDefender&&!s.SynergyOrder&&s.Applied));
                StopGuerrillaHighTierEvidenceOrdinaryAttacks();
                for(int i=0;i<160&&Battle.CombatShots.Any(s=>!s.Resolved);i++){Tick(.05f);if(i%10==0)yield return null;}
                for(int i=0;i<12&&Battle.Orders>=2;i++){if(!UseCampaignOrder(false))break;Tick(2.61f);yield return null;}
                int remaining=Battle.Orders;beforePlays=view.PlayCount;
                check("noCastCapStillRejectsInsufficientBalance",remaining<2&&!UseCampaignOrder(false)&&Battle.Orders==remaining&&view.PlayCount==beforePlays);
                result.nineTierCasts=Battle.OrdersUsed;result.earnedOrders=Battle.Orders+2*Battle.OrdersUsed-2;
                check("ordersNeverRecursivelyManufactureMoreIncome",result.earnedOrders>=8&&result.earnedOrders<=2*HighTierEvidenceRoster.Length);

                check("realEncounterRestart",RestartCampaignEncounter());while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("newEncounterReadyStillZeroAndTransfersPersist",Battle.Orders==0&&Battle.GuerrillaTier==9&&PreparedGuerrillaTransferIds.Count==2);
                Battle.Begin();Battle.Begin();DurableGuerrillaHighTierEvidenceGuards();
                check("newEncounterGetsExactlyTwoOnce",Battle.Orders==2);
                if(Environment.GetCommandLineArgs().Contains("-guerrillaHighTierCaptureFrames"))
                {
                    // A fresh real nine-source order gives one 2x-header, 1x-cinematic capture.
                    foreach(string id in HighTierEvidenceRoster){yield return DeployGuerrillaHighTierEvidence(id);StopGuerrillaHighTierEvidenceOrdinaryAttacks();}
                    if(SimulationSpeed!=2)ToggleSpeed();
                    check("captureCommitsAllNineOrdinarySources",UseCampaignOrder(false)&&Battle.LatestGuerrillaOrder.SourceIds.Count==9);
                    var captured=Battle.LatestGuerrillaOrder;int soundCount=view.SoundPlayCount;
                    string frames=Path.Combine(directory,"frames");Directory.CreateDirectory(frames);
                    for(int frame=0;frame<204;frame++)
                    {
                        if(frame>=6)Tick(1f/60);
                        if(frame==84)check("twoTimesHeaderDoesNotShortenCapturedMotion",SimulationSpeed==2&&view.Playing&&Math.Abs(view.Age-79f/60)<.001f);
                        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,frame.ToString("D3")+".png"));yield return null;
                    }
                    check("capturedLiveNineSourceWaveCompletesOnce",captured.Resolved&&!captured.Cancelled&&captured.Hits.Count>0&&!view.Playing&&view.SoundPlayCount==soundCount+1);
                }
                result.tier=Battle.GuerrillaTier;complete=true;
            }
            finally
            {
                Time.timeScale=oldScale;result.passed=passed&&complete;result.checks=checks.ToArray();result.plays=view!=null?view.PlayCount:0;result.sounds=view!=null?view.SoundPlayCount:0;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("GUERRILLA_HIGH_TIER_PLAYER_CHECK passed="+result.passed);Application.Quit(result.passed?0:1);
            }
        }
    }
    public sealed class GuerrillaHighTierSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||GuerrillaAutoFiveSmokeDriver.Requested||!Environment.GetCommandLineArgs().Contains("-guerrillaHighTierSmokeTest"))return;
            started=true;var go=new GameObject("Guerrilla high-tier explicit Player verification");DontDestroyOnLoad(go);go.AddComponent<GuerrillaHighTierSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("GuerrillaTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            if(controller==null||controller.ArtLoading)throw new InvalidOperationException("Guerrilla trial did not load for high-tier verification.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/guerrilla-high-tier-player");
            // Flatten nested iterators so an exception in a helper cannot leave a waiting
            // evidence parent coroutine alive without writing its failure result.
            var stack=new Stack<IEnumerator>();stack.Push(controller.VerifyGuerrillaHighTier(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                    Debug.LogException(failure);Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
        }
    }
}
