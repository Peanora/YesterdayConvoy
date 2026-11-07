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
        [Serializable] sealed class DeepPoolEvidenceResult
        {
            public bool passed;
            public string scene;
            public double firstBurnAt=-1,firstSeedAt=-1,firstDetonationAt=-1;
            public float detonationDamage;
            public string[] checks;
        }

        IEnumerator DeepPoolEvidenceFrame(string directory,string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            yield return null;
            yield return null;
        }

        // Uses the same card down/drag/up path as ordinary play. No health, status or resource injection.
        bool DeepPoolEvidenceDrag(string cardId,int lane=0)
        {
            int slot=shop.Bench.ToList().FindIndex(p=>p.Id==cardId);
            if(slot<0||lane<0||lane>=Battle.Lanes.Count)return false;
            int before=Battle.Units.Count;
            Vector2 gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Lanes[lane].Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);
            ProcessShopPointer(EventType.MouseDrag,gate);
            ProcessShopPointer(EventType.MouseUp,gate);
            return Battle.Units.Count==before+1&&Battle.Units.Last().CardId==cardId;
        }

        internal IEnumerator VerifyDeepPool(string directory)
        {
            Directory.CreateDirectory(directory);
            diagnostic=true;
            Application.runInBackground=true;
            var result=new DeepPoolEvidenceResult{scene=SceneManager.GetActiveScene().name};
            var checks=new List<string>();
            bool passed=true,complete=false;
            float previousScale=Time.timeScale;
            Action<string,bool> check=(name,value)=>
            {
                checks.Add(name+"="+value);
                passed&=value;
                Debug.Log("DEEP_POOL_EVENT "+name+"="+value);
            };
            try
            {
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Deep-pool preparation timed out.");
                check("authoredDedicatedScene",SynergySandbox&&activeSynergyTrial!=null&&activeSynergyTrial.trialId=="deep-pool"&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
                check("fourDistinctMembers",Battle.DeepTier==4&&shopLineup.Count==4&&SynergyTrialCatalog.Find("deep-pool").Members.All(shopLineup.Contains));
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin the deep-pool encounter.");
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Deep-pool shop did not close.");
                Time.timeScale=0;
                Battle.Begin();
                Battle.SetPaused(true);
                int dp=Battle.DP;
                check("pausedOrdinaryDrag",DeepPoolEvidenceDrag("CARD_M32")&&Battle.Units.Count==1&&Battle.DP==dp-Battle.FindCard("CARD_M32").Settings.DeploymentCost);
                if(Battle.Units.Count!=1)throw new InvalidOperationException("Mandragora's ordinary drag deployment failed.");
                double clock=Battle.Clock;
                Tick(2);
                check("pausedDeploymentDoesNotAdvance",Battle.Clock==clock&&Battle.DeepCharges==0);
                yield return DeepPoolEvidenceFrame(directory,"01-paused-drag");
                Battle.SetPaused(false);

                bool burnSeen=false,detonated=false,nativeVisible=false;
                for(int tick=0;tick<2400&&Loop.CanDeploy&&!detonated;tick++)
                {
                    Tick(.05f);
                    if(!burnSeen&&Battle.Defenders.Any(d=>d.Alive&&Battle.DeepBurnRemaining(d.EntityId)>0))
                    {
                        burnSeen=true;result.firstBurnAt=Battle.Clock;
                        check("naturalScorchWithoutFlamerazerOrReeds",Battle.Units.All(u=>u.CardId=="CARD_M32")&&Battle.BurnBursts==0);
                        var target=Battle.Defenders.First(d=>d.Alive&&Battle.DeepBurnRemaining(d.EntityId)>0);
                        check("fourTierScorchPowerAndDuration",Math.Abs(Battle.DeepBurnPower(target.EntityId)-25)<.01f&&Battle.DeepBurnRemaining(target.EntityId)<=4.001);
                        Battle.SetPaused(true);
                        double remaining=Battle.DeepBurnRemaining(target.EntityId),pausedAt=Battle.Clock;
                        float health=target.Health;
                        Tick(2);
                        check("pauseFreezesScorchAndHealth",Battle.Clock==pausedAt&&Battle.DeepBurnRemaining(target.EntityId)==remaining&&target.Health==health);
                        check("pausedDetonationDoesNotSpend",!Battle.TryDetonateDeep(Battle.RunId,++requestSequence)&&Battle.DeepDetonations==0);
                        yield return DeepPoolEvidenceFrame(directory,"02-natural-scorch-paused");
                        Battle.SetPaused(false);
                    }
                    if(tick%5==0)
                    {
                        yield return null;
                        var native=CombatView!=null?CombatView.GetComponent<MandragoraEffectView>():null;
                        nativeVisible|=native!=null&&native.VisibleEffects>0;
                    }
                    if(Battle.DeepCharges==1&&Battle.DeepDetonations==0)
                    {
                        if(result.firstSeedAt<0)
                        {
                            result.firstSeedAt=Battle.Clock;
                            yield return DeepPoolEvidenceFrame(directory,"03-first-natural-seed");
                        }
                        float health=Battle.Defenders.Sum(d=>d.Health);
                        int seeds=Battle.DeepSeeds.Count;
                        long request=++requestSequence;
                        if(!Battle.TryDetonateDeep(Battle.RunId,request))continue;
                        result.firstDetonationAt=Battle.Clock;
                        result.detonationDamage=health-Battle.Defenders.Sum(d=>d.Health);
                        check("oneSeedDetonatesOnce",Battle.DeepCharges==0&&Battle.DeepDetonations==1);
                        check("detonationDealsRealDamageWithoutNewSeed",result.detonationDamage>0&&Battle.DeepSeeds.Count==seeds);
                        float after=Battle.Defenders.Sum(d=>d.Health);
                        check("sameRequestIsIdempotent",Battle.TryDetonateDeep(Battle.RunId,request)&&Battle.DeepDetonations==1&&Battle.Defenders.Sum(d=>d.Health)==after);
                        detonated=true;
                        yield return DeepPoolEvidenceFrame(directory,"04-single-seed-detonation");
                    }
                }
                check("naturalScorchObserved",burnSeen);
                check("naturalSeedAndSingleSeedDetonationObserved",detonated&&result.firstSeedAt>=0);
                check("mandragoraNativeVfxConnected",nativeVisible);

                // Start a fresh ordinary encounter to inspect the other original damage source as well.
                RestartCurrentRun();
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                yield return null;yield return null;
                check("restartClearsUnitsSeedsAndScorch",Battle.Phase==BattlePhase.Ready&&Battle.Clock==0&&Battle.Units.Count==0&&Battle.DeepSeeds.Count==0&&Battle.DeepDetonations==0&&Battle.Defenders.All(d=>Battle.DeepBurnRemaining(d.EntityId)==0));
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot restart the deep-pool encounter.");
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                Battle.Begin();Battle.SetPaused(true);
                check("flamerazerOrdinaryDrag",DeepPoolEvidenceDrag("CARD_M31"));
                Battle.SetPaused(false);
                bool spraySeen=false;
                for(int i=0;i<240&&!spraySeen&&Loop.CanDeploy;i++)
                {
                    Tick(.05f);yield return null;
                    var view=CombatView!=null?CombatView.GetComponent<FlamerazerEffectView>():null;
                    spraySeen=Battle.FlameSprays.Any(s=>s.Ticks>0)&&view!=null&&view.VisibleEffects>0&&Battle.Defenders.Any(d=>Battle.DeepBurnRemaining(d.EntityId)>0);
                }
                check("nativeSprayAndScorchConnected",spraySeen);
                Battle.SetPaused(true);
                yield return DeepPoolEvidenceFrame(directory,"05-native-spray-and-scorch");
                complete=true;
            }
            finally
            {
                Time.timeScale=previousScale;
                result.passed=passed&&complete;result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("DEEP_POOL_PLAYER_CHECK passed="+result.passed+"; scorchAt="+result.firstBurnAt+"; seedAt="+result.firstSeedAt+"; detonationDamage="+result.detonationDamage);
                Application.Quit(result.passed?0:1);
            }
        }
    }

    // Explicit opt-in only. The play shortcut loads the saved scene and leaves control with the player.
    public sealed class DeepPoolSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            bool smoke=Debug.isDebugBuild&&args.Contains("-deepPoolSmokeTest");
            if(started||(!smoke&&!args.Contains("-deepPoolTrial")))return;
            started=true;
            var go=new GameObject("Deep-pool explicit verification");
            DontDestroyOnLoad(go);go.AddComponent<DeepPoolSmokeDriver>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            bool smoke=Debug.isDebugBuild&&args.Contains("-deepPoolSmokeTest");
            if(smoke)Application.runInBackground=true;
            yield return null;
            if(SceneManager.GetActiveScene().name!="DeepPoolTrial")yield return SceneManager.LoadSceneAsync("DeepPoolTrial");
            if(!smoke){Destroy(gameObject);yield break;}
            int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.GetFullPath("artifacts/deep-pool-player");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||controller.ArtLoading||controller.ShopPanelTransitioning)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"result.json"),"{\"passed\":false,\"checks\":[\"dedicatedSceneReady=False\"]}");
                Debug.LogError("DEEP_POOL_PLAYER_CHECK dedicated scene did not become ready.");
                Application.Quit(1);yield break;
            }
            yield return controller.VerifyDeepPool(directory);
        }
    }
}
