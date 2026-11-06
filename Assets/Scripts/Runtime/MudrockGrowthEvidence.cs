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
        [Serializable]sealed class MudrockGrowthGuiStep {public int id;public string name;public float x,y,width,height;}
        [Serializable]sealed class MudrockGrowthRoundEvidence
        {
            public int round,before,raw,earned,after,capacity,stars,stageBefore,stageAfter;
            public string outcome;
        }
        [Serializable]sealed class MudrockGrowthEvidenceResult
        {
            public bool passed,requiredActualClicks;
            public string naturalPath="Default saved MudrockCrewTrial, zero initial XP, authored defenders and normal sandbox multipliers. Ordinary deployment, manual skills, combat resolution and commissions use production operations. No FinishForTesting or HP/attack fixture changes are used for the natural rounds.";
            public string presetPath="IV and V are explicitly selected validation presets, not claimed as naturally cultivated formations.";
            public string[] checks,screenshots;
            public MudrockGrowthRoundEvidence[] naturalRounds;
            public int naturallyReachedStage,naturalFinalXP,naturalFinalStars;
            public float firstAnimatedXP;
        }
        int mudrockGrowthGuiSequence;

        IEnumerator MudrockGrowthGuiClick(string directory,string name,Rect rect,Func<bool> accepted)
        {
            if(rect.width<=0||rect.height<=0)throw new InvalidOperationException("Mudrock GUI target is not visible: "+name);
            Vector2 point=TacticalViewport.Current.ToScreen(rect.center);int id=++mudrockGrowthGuiSequence;
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new MudrockGrowthGuiStep
                {id=id,name=name,x=point.x,y=Screen.height-point.y,width=Screen.width,height=Screen.height},true));
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
        IEnumerator MudrockGrowthFrame(string directory,string name,List<string> screenshots)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));screenshots.Add(name+".png");
            yield return null;
        }
        IEnumerator OpenMudrockGrowthEvidencePanel(string directory,bool actualClicks)
        {
            if(!actualClicks){OpenGolemTrainingPanel();yield return null;yield break;}
            yield return MudrockGrowthGuiClick(directory,"open-construction",CampaignConstructToggleRect,()=>campaignConstructOpen);
            yield return MudrockGrowthGuiClick(directory,"open-faction-and-cultivation",
                new Rect(CampaignConstructRect.x+184,CampaignConstructRect.y+45,160,28),()=>campaignConstructTab==1);
            yield return MudrockGrowthGuiClick(directory,"open-golem-cultivation",new Rect(712,246,120,28),()=>GolemTrainingPanelVisible);
        }
        IEnumerator RunNaturalMudrockGrowthRound(MudrockGrowthRoundEvidence record)
        {
            while(ArtLoading||ShopPanelTransitioning)yield return null;
            record.round=Loop.SandboxRound;record.before=Loop.GolemExperience;
            record.capacity=Battle.GolemCapacity;record.stars=Battle.MudrockTotalStars;
            record.raw=Battle.GolemBattleExperience;record.stageBefore=Battle.GolemStage;
            campaignConstructOpen=false;inspectedSynergy=null;
            if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin natural Mudrock round.");
            Battle.Begin();
            var ids=shopLineup.OrderByDescending(id=>id=="CARD_M28"?3:id=="CARD_M43"?2:id=="CARD_M27"?1:0).ToArray();
            int wave=0;
            for(int step=0;step<12000&&Loop.CanDeploy;step++)
            {
                if(step%40==0&&Battle.Phase==BattlePhase.Running)
                {
                    for(int i=0;i<ids.Length;i++)
                    {
                        var lane=Battle.Lanes[(i+wave)%Battle.Lanes.Count];
                        Battle.TryDeployRole(Battle.RunId,++requestSequence,lane.Points[0],ids[i]);
                    }
                    if(!Battle.GolemDeployed)Battle.TryDeployGolem(Battle.RunId,++requestSequence,Battle.Lanes[wave%Battle.Lanes.Count].Points[0]);
                    foreach(var unit in Battle.Units.Where(u=>u.Active&&Battle.IsSkillReady(u.EntityId)).ToArray())
                        Battle.TryActivateSkill(Battle.RunId,++requestSequence,unit.EntityId);
                    wave++;
                }
                Tick(.05f);if(step%10==0)yield return null;
            }
            record.outcome=Battle.Phase.ToString();record.after=Loop.GolemExperience;record.earned=Loop.LastGolemGrowth;
            record.stageAfter=GolemGrowth.Stage(record.after,record.capacity);
            if(Battle.Phase!=BattlePhase.Won)throw new InvalidOperationException("Natural Mudrock round did not win: "+record.outcome);
            // Finish the ordinary terminal presentation without changing the outcome or XP.
            for(int tail=0;tail<600&&DeathPresentationPending;tail++)
            {Tick(.1f);if(tail%10==0)yield return null;}
            if(DeathPresentationPending)throw new InvalidOperationException("Mudrock terminal presentation did not settle.");
            Tick(.1f);yield return null;
        }
        IEnumerator CommissionMudrockEvidenceStar(string id)
        {
            for(int purchase=0;purchase<2;purchase++)
            {
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                var receipt=CommissionCampaignCard(id);
                if(!receipt.Success)throw new InvalidOperationException("Natural cultivation commission failed: "+id+" / "+receipt.Message);
                yield return new WaitForSecondsRealtime(1.4f);
            }
            while(ArtLoading||ShopPanelTransitioning)yield return null;
        }
        internal IEnumerator VerifyMudrockGrowth(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var result=new MudrockGrowthEvidenceResult{requiredActualClicks=Environment.GetCommandLineArgs().Contains("-mudrockGrowthRequireClicks")};
            var checks=new List<string>();var screenshots=new List<string>();var rounds=new List<MudrockGrowthRoundEvidence>();
            bool passed=true,complete=false;float oldScale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("MUDROCK_GROWTH_EVENT "+name+"="+value);};
            try
            {
                Time.timeScale=0;while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("defaultTrialStartsAtZeroWithoutTransfer",activeSynergyTrial!=null&&activeSynergyTrial.trialId=="mudrock"&&Loop.GolemExperience==0&&Battle.GolemStage==1&&Battle.MudrockCrewCount==5&&Battle.MudrockTotalStars==5&&MudrockTransferIds.Count==0);
                check("defaultTrialOwnsNonNativeTransferCandidate",shop.Bench.Any(p=>p.Id=="CARD_M08"&&p.Star==1));
                yield return OpenMudrockGrowthEvidencePanel(directory,result.requiredActualClicks);
                yield return MudrockGrowthFrame(directory,"01-zero-xp-cultivation",screenshots);

                var first=new MudrockGrowthRoundEvidence();rounds.Add(first);yield return RunNaturalMudrockGrowthRound(first);
                check("firstNaturalWinAutomaticallyPromotesAndCaps",first.before==0&&first.raw==7&&first.earned==6&&first.after==6&&first.stageAfter==2&&Loop.Phase==BattleShopPhase.Complete);
                // The feedback uses unscaled presentation time, so paused simulation still shows it.
                yield return new WaitForSecondsRealtime(.25f);
                result.firstAnimatedXP=GolemDisplayedExperience;
                check("xpFeedbackReallyInterpolatesAndShowsPromotion",GolemFeedbackAnimating&&GolemFeedbackEarned==6&&GolemFeedbackBeforeStage==1&&GolemFeedbackAfterStage==2&&GolemDisplayedExperience>0&&GolemDisplayedExperience<6);
                yield return MudrockGrowthFrame(directory,"02-xp-gain-animation",screenshots);
                yield return new WaitForSecondsRealtime(1.6f);
                yield return MudrockGrowthFrame(directory,"03-natural-promotion-settlement",screenshots);
                int xp=Loop.GolemExperience;Tick(.05f);
                check("settlementCannotGrantXpTwice",Loop.GolemExperience==xp&&!Loop.Observe(Battle.Phase,Battle.GolemGrowthEarned,0,Battle.GolemCapacity));
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("returnPreparationRetainsXpAndAdvancesSandboxRoundOnly",Loop.GolemExperience==6&&Battle.GolemStage==2&&Loop.SandboxRound==2&&Loop.Stage==1);
                OpenGolemTrainingPanel();yield return MudrockGrowthFrame(directory,"04-returned-at-current-cap",screenshots);

                var second=new MudrockGrowthRoundEvidence();rounds.Add(second);yield return RunNaturalMudrockGrowthRound(second);
                check("secondNaturalWinAtCapEarnsZeroWithoutHiddenBank",second.before==6&&second.raw==7&&second.earned==0&&second.after==6&&second.stageAfter==2);
                yield return new WaitForSecondsRealtime(.2f);
                yield return MudrockGrowthFrame(directory,"05-zero-xp-at-cap-settlement",screenshots);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                yield return CommissionMudrockEvidenceStar("CARD_M26");yield return CommissionMudrockEvidenceStar("CARD_M27");
                check("realCommissionsMergeToTotalSevenAndUnlockCapacityThree",shop.Bench.Any(p=>p.Id=="CARD_M26"&&p.Star==2)&&shop.Bench.Any(p=>p.Id=="CARD_M27"&&p.Star==2)&&Battle.MudrockTotalStars==7&&Battle.GolemCapacity==3&&Loop.GolemExperience==6);
                OpenGolemTrainingPanel();yield return MudrockGrowthFrame(directory,"06-real-star-growth-unlocks-capacity",screenshots);
                var third=new MudrockGrowthRoundEvidence();rounds.Add(third);yield return RunNaturalMudrockGrowthRound(third);
                check("thirdNaturalWinUsesStarBonusAfterCapacityUnlock",third.before==6&&third.raw==8&&third.earned==8&&third.after==14&&third.capacity==3);
                yield return new WaitForSecondsRealtime(.25f);yield return MudrockGrowthFrame(directory,"07-unlocked-cap-xp-animation",screenshots);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                foreach(string id in new[]{"CARD_M42","CARD_M43","CARD_M28"})yield return CommissionMudrockEvidenceStar(id);
                check("realCommissionsAcrossAllCostsUnlockIvCapacity",Battle.MudrockCrewCount==5&&Battle.MudrockTotalStars==10&&Battle.GolemCapacity==4&&Loop.GolemExperience==14);
                var fourth=new MudrockGrowthRoundEvidence();rounds.Add(fourth);yield return RunNaturalMudrockGrowthRound(fourth);
                check("fourthNaturalWinReachesThirdStage",fourth.before==14&&fourth.raw==10&&fourth.earned==10&&fourth.after==24&&fourth.stageAfter==3);
                yield return new WaitForSecondsRealtime(.95f);yield return MudrockGrowthFrame(directory,"08-natural-iii-settlement",screenshots);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                var fifth=new MudrockGrowthRoundEvidence();rounds.Add(fifth);yield return RunNaturalMudrockGrowthRound(fifth);
                check("fifthNaturalWinReachesIvAndCapsAtThirty",fifth.before==24&&fifth.raw==10&&fifth.earned==6&&fifth.after==30&&fifth.stageAfter==4);
                yield return new WaitForSecondsRealtime(.95f);yield return MudrockGrowthFrame(directory,"09-natural-iv-settlement",screenshots);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                yield return CommissionMudrockEvidenceStar("CARD_M08");

                int gold=shop.Gold;
                if(result.requiredActualClicks)
                {
                    yield return MudrockGrowthGuiClick(directory,"open-transfer-construction",CampaignConstructToggleRect,()=>campaignConstructOpen);
                    yield return MudrockGrowthGuiClick(directory,"open-mudrock-transfers",MudrockTransferTabRect,()=>MudrockTransfersVisible);
                    yield return MudrockGrowthGuiClick(directory,"assign-mudrock-transfer",MudrockTransferButtonRect("CARD_M08"),()=>IsMudrockTransfer("CARD_M08"));
                }
                else
                {
                    campaignConstructOpen=true;campaignConstructTab=3;
                    if(!SetMudrockTransfer("CARD_M08",true))throw new InvalidOperationException("Free Mudrock designation failed.");
                    yield return null;
                }
                check("freeTransferCountsSixthMemberAndOwnStars",Battle.MudrockTier==6&&Battle.MudrockCrewCount==6&&Battle.MudrockTotalStars==12&&Battle.GolemCapacity==5&&Battle.GolemBattleExperience==12&&shop.Gold==gold&&Loop.GolemExperience==30);
                yield return MudrockGrowthFrame(directory,"10-free-sixth-member-transfer",screenshots);
                var sixth=new MudrockGrowthRoundEvidence();rounds.Add(sixth);yield return RunNaturalMudrockGrowthRound(sixth);
                check("sixthNaturalWinUsesSixMemberIncome",sixth.before==30&&sixth.raw==12&&sixth.earned==12&&sixth.after==42&&sixth.stageAfter==4);
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                var seventh=new MudrockGrowthRoundEvidence();rounds.Add(seventh);yield return RunNaturalMudrockGrowthRound(seventh);
                check("seventhNaturalWinReachesVAndCapsAtFortyEight",seventh.before==42&&seventh.raw==12&&seventh.earned==6&&seventh.after==48&&seventh.stageAfter==5);
                yield return new WaitForSecondsRealtime(.25f);yield return MudrockGrowthFrame(directory,"11-natural-v-upgrade-animation",screenshots);
                yield return new WaitForSecondsRealtime(.95f);yield return MudrockGrowthFrame(directory,"12-natural-v-settlement",screenshots);
                result.naturallyReachedStage=rounds.Max(r=>r.stageAfter);result.naturalFinalXP=Loop.GolemExperience;result.naturalFinalStars=Battle.MudrockTotalStars;
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("naturalVFormationAndXpSurviveReturnPreparation",Battle.GolemStage==5&&Loop.GolemExperience==48&&Battle.MudrockTotalStars==12&&IsMudrockTransfer("CARD_M08"));
                OpenGolemTrainingPanel();yield return MudrockGrowthFrame(directory,"13-natural-v-ready-formation",screenshots);
                campaignConstructOpen=false;if(!StartLoopNextBattle())throw new InvalidOperationException("Naturally cultivated V could not enter combat.");Battle.Begin();
                int naturalDP=Battle.DP;var naturalDeploy=Battle.TryDeployGolem(Battle.RunId,++requestSequence,Battle.Entry);var naturalGolem=Battle.FindUnit(naturalDeploy.EntityId);
                check("naturalVActualDeploymentHasPowerCostAndOneUse",naturalDeploy.Success&&naturalGolem!=null&&naturalGolem.MaxHealth==7500&&Battle.UnitCombat(naturalGolem).UnitAttack==2000&&Battle.DP==naturalDP-14&&!Battle.TryDeployGolem(Battle.RunId,++requestSequence,Battle.Entry).Success&&Battle.UnitControlImmune(naturalGolem));
                Tick(1.1f);yield return MudrockGrowthFrame(directory,"14-natural-v-actual-combat-body",screenshots);

                // These are separate, explicitly labeled combat baselines after natural growth passed.
                ReplaySandbox();while(ArtLoading||ShopPanelTransitioning)yield return null;
                campaignConstructOpen=false;PrepareSandboxRoster(10);while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("explicitIvPresetRetainsOriginalPower",Battle.GolemStage==4&&Loop.GolemExperience==30&&Battle.GolemCost==14);
                OpenGolemTrainingPanel();yield return MudrockGrowthFrame(directory,"15-explicit-iv-preset",screenshots);
                campaignConstructOpen=false;PrepareSandboxRoster(11);while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("explicitVPresetRequiresSixMembersAndTwelveStars",Battle.GolemStage==5&&Battle.GolemCapacity==5&&Battle.MudrockTier==6&&Battle.MudrockCrewCount==6&&Battle.MudrockTotalStars>=12&&Loop.GolemExperience==48);
                OpenGolemTrainingPanel();yield return MudrockGrowthFrame(directory,"16-explicit-v-preset",screenshots);
                campaignConstructOpen=false;if(!StartLoopNextBattle())throw new InvalidOperationException("V preset could not enter combat.");Battle.Begin();
                int dp=Battle.DP;var deployed=Battle.TryDeployGolem(Battle.RunId,++requestSequence,Battle.Entry);
                var golem=Battle.FindUnit(deployed.EntityId);
                check("vActualDeploymentHasPowerCostAndOneUse",deployed.Success&&golem!=null&&golem.MaxHealth==7500&&Battle.UnitCombat(golem).UnitAttack==2000&&Battle.DP==dp-14&&!Battle.TryDeployGolem(Battle.RunId,++requestSequence,Battle.Entry).Success&&Battle.UnitControlImmune(golem));
                Tick(1.1f);yield return MudrockGrowthFrame(directory,"17-explicit-v-actual-combat-body",screenshots);
                complete=true;
            }
            finally
            {
                Time.timeScale=oldScale;result.passed=passed&&complete;result.checks=checks.ToArray();result.screenshots=screenshots.ToArray();result.naturalRounds=rounds.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("MUDROCK_GROWTH_PLAYER_CHECK passed="+result.passed);Application.Quit(result.passed?0:1);
            }
        }
    }

    public sealed class MudrockGrowthSmokeDriver:MonoBehaviour
    {
        [Serializable]sealed class FailureResult {public bool passed;public string error;}
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-mudrockGrowthSmokeTest"))return;
            started=true;var go=new GameObject("Mudrock natural growth Player verification");DontDestroyOnLoad(go);go.AddComponent<MudrockGrowthSmokeDriver>();
        }
        IEnumerator LoadAndVerify(string directory)
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("MudrockCrewTrial");
            ManualBattleController controller=null;double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||!controller.Loop.CanShop||controller.ArtLoading)throw new InvalidOperationException("Mudrock trial did not load for cultivation verification.");
            yield return controller.VerifyMudrockGrowth(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/mudrock-growth-player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(LoadAndVerify(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());
                    if(!File.Exists(Path.Combine(directory,"result.json")))File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new FailureResult{passed=false,error=failure.ToString()},true));
                    Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
        }
    }
}
