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
        [Serializable] sealed class ReunionCoalitionResult
        {
            public bool passed;
            public string scene;
            public string scope="Saved ReunionTrial, finite-stock preparation fixtures and production card pointer deployment. Detail modal is set only for diagnostic screenshots, not an OS mouse click. No injected HP, damage, clock, shield or forced battle result.";
            public string[] checks;
            public float eliteSoldierHealth,eliteArcherHealth,eliteShield;
            public double firstEliteContact;
        }

        IEnumerator ReunionEvidenceReady()
        {
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Reunion trial preparation did not finish.");
            yield return null;
        }

        IEnumerator ReunionEvidenceCapture(string directory,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            yield return null;yield return null;
        }

        bool ReunionEvidenceDrag(string id,Vector2? destination=null)
        {
            int slot=shop.Bench.ToList().FindIndex(p=>p.Id==id&&IsLineupPiece(p));
            if(slot<0||Camera.main==null)return false;
            int before=Battle.Units.Count(u=>!u.SynergySupport);
            var gate=destination??TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);
            ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            return Battle.Units.Count(u=>!u.SynergySupport)==before+1&&Battle.Units.Last(u=>!u.SynergySupport).CardId==id;
        }

        internal IEnumerator VerifyReunionCoalition(string directory)
        {
            Directory.CreateDirectory(directory);var result=new ReunionCoalitionResult{scene=SceneManager.GetActiveScene().name};
            var checks=new List<string>();bool valid=true,completed=false;float previousScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("REUNION_PLAYER "+name+"="+ok);};
            try
            {
                diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
                yield return ReunionEvidenceReady();campaignConstructOpen=false;inspectedSynergy=null;
                check("saved-trial-eight-members-four-formations",activeSynergyTrial?.trialId=="reunion"&&SynergySandbox&&
                    FindAnyObjectByType<AuthoredBattlefield>()!=null&&Battle.ReunionTier==8&&Battle.ReunionCount==8&&Battle.ReunionFormationCount==4);
                var summary=CurrentSynergies.Single(s=>s.Name=="整合运动");
                check("four-thresholds-and-existing-prismatic-badge",summary.Thresholds.SequenceEqual(new[]{2,4,6,8})&&
                    SynergyBadgePresentation.GradeFor(summary.Name,summary.Tier)==SynergyVisualGrade.Prismatic);
                yield return ReunionEvidenceCapture(directory,"01-saved-eight-tier-preparation");
                inspectedSynergy="整合运动";
                yield return ReunionEvidenceCapture(directory,"02-eight-tier-formation-detail");inspectedSynergy=null;

                int[] tiers={2,4,6,8};
                foreach(int tier in tiers)
                {
                    string prefix="tier-"+tier+"-";
                    if(!Loop.CanShop){ReplaySandbox();yield return ReunionEvidenceReady();}
                    string[] ids=SynergyTrialCatalog.Find("reunion").Members.Take(tier).ToArray();
                    if(tier==4)ids=ids.Concat(new[]{"CARD_M05"}).ToArray();
                    yield return PrepareSynergyBadgeEvidenceRoster(ids);
                    reunionRanged=tier==4;SyncShopDeck(false);
                    check(prefix+"real-prebattle-snapshot",Battle.ReunionTier==tier&&Battle.ReunionCount==tier&&
                        Battle.ReunionFormationCount==ReunionRules.FormationCount(ids)&&Math.Abs(Battle.ReunionAverageStar-1)<.001f);
                    shop.Validate();check(prefix+"stock-stays-finite",shop.Roles.All(role=>shop.Available(role.Id)+
                        shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>p.Star==3?9:p.Star==2?3:1)+shop.Offers.Count(id=>id==role.Id)==9));
                    if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin Reunion tier "+tier+".");
                    yield return ReunionEvidenceReady();Battle.Begin();Battle.SetPaused(true);
                    int initialDP=Battle.DP,normalCost=0,required=Battle.ReunionMusterRequirement;
                    var cycle=SynergyTrialCatalog.Find("reunion").Members.Take(required).ToArray();
                    for(int i=0;i<cycle.Length;i++)
                    {
                        string id=cycle[i];var card=Battle.FindCard(id);int uses=card.RemainingUses;
                        check(prefix+"paid-pointer-deploy-"+id,ReunionEvidenceDrag(id));normalCost+=card.Settings.DeploymentCost;
                        check(prefix+"normal-card-use-"+id,card.RemainingUses==uses-1);
                        if(i==0)
                        {
                            int before=Battle.Units.Count,dp=Battle.DP;
                            check(prefix+"paused-cooldown-cannot-duplicate",!ReunionEvidenceDrag(id)&&Battle.Units.Count==before&&Battle.DP==dp&&Battle.MusterMemberIds.Count==1);
                            if(tier==4)
                            {
                                var outsider=Battle.FindCard("CARD_M05");
                                check(prefix+"outsider-deploys-without-filling-muster",ReunionEvidenceDrag("CARD_M05")&&Battle.MusterMemberIds.Count==1&&Battle.MusterUsed==0);
                                normalCost+=outsider.Settings.DeploymentCost;
                                before=Battle.Units.Count;dp=Battle.DP;
                                check(prefix+"invalid-release-cannot-fill-muster",!ReunionEvidenceDrag("CARD_M09",new Vector2(1270,10))&&Battle.Units.Count==before&&Battle.DP==dp&&Battle.MusterMemberIds.Count==1);
                            }
                        }
                    }
                    var supports=Battle.Units.Where(u=>u.SynergySupport).ToArray();
                    check(prefix+"one-finite-muster-and-cleared-progress",Battle.MusterUsed==1&&Battle.MusterMemberIds.Count==0&&supports.Length==(tier>=6?2:1));
                    check(prefix+"only-normal-cards-spend-dp",Battle.DP==initialDP-normalCost&&Battle.Clock==0);
                    check(prefix+"support-follows-trigger-route",supports.All(u=>u.RouteId==Battle.Units.Last(u=>!u.SynergySupport).RouteId));
                    check(prefix+"expected-near-ranged-composition",tier==2?supports.Length==1&&supports[0].CardId=="CARD_M07":
                        tier==4?supports.Length==1&&supports[0].CardId=="CARD_M08":supports.Count(u=>u.CardId=="CARD_M07")==1&&supports.Count(u=>u.CardId=="CARD_M08")==1);
                    bool stats=true;
                    foreach(var support in supports)
                    {
                        bool archer=support.CardId=="CARD_M08";var expected=ReunionRules.ForSupport(tier,archer);
                        stats&=support.MaxHealth==expected.Health&&support.Profile.Combat.UnitAttack==expected.Attack&&
                            support.Profile.Combat.UnitArmor==expected.Armor&&Math.Abs(support.Profile.Combat.UnitRange-expected.Range)<.001f&&
                            Math.Abs(support.Profile.Combat.UnitInterval-1.8f)<.001f;
                    }
                    check(prefix+"real-support-profiles",stats);
                    double clock=Battle.Clock;int resource=Battle.DP;var positions=supports.Select(u=>u.Motion.Position).ToArray();
                    Tick(3);
                    check(prefix+"pause-freezes-movement-and-time",Battle.Clock==clock&&Battle.DP==resource&&supports.Select((u,i)=>Vector3.Distance(u.Motion.Position,positions[i])<.0001f).All(x=>x));
                    check(prefix+"shield-not-spent-at-red-gate",supports.All(u=>u.SynergyShield==0&&u.ReunionEntryShieldPending==(tier==8)));
                    yield return ReunionEvidenceCapture(directory,"03-tier-"+tier+"-paused-muster");

                    if(tier==8)
                    {
                        var soldier=supports.Single(u=>u.CardId=="CARD_M07");var archer=supports.Single(u=>u.CardId=="CARD_M08");
                        result.eliteSoldierHealth=soldier.MaxHealth;result.eliteArcherHealth=archer.MaxHealth;
                        Battle.SetPaused(false);Tick(.05f);
                        check("elite-soldier-can-walk-with-unspent-ward",soldier.ReunionEntryShieldPending&&soldier.SynergyShield==0&&soldier.Motion.DistanceTravelled>0);
                        DeployedUnitState engaged=supports.FirstOrDefault(u=>!u.ReunionEntryShieldPending);
                        for(int step=0;step<1200&&engaged==null&&Battle.Phase==BattlePhase.Running;step++)
                        {Tick(.05f);engaged=supports.FirstOrDefault(u=>!u.ReunionEntryShieldPending);if(step%4==0)yield return null;}
                        check("natural-first-engagement-starts-elite-ward",engaged!=null&&engaged.SynergyShieldUntil>Battle.Clock&&engaged.SynergyShield<=Mathf.Floor(engaged.MaxHealth*.2f+.5f));
                        if(engaged!=null)
                        {
                            result.eliteShield=engaged.SynergyShield;result.firstEliteContact=Battle.Clock;
                            Battle.SetPaused(true);clock=Battle.Clock;float ward=engaged.SynergyShield;double expiry=engaged.SynergyShieldUntil;
                            Tick(8);
                            check("elite-ward-freezes-during-pause",Battle.Clock==clock&&engaged.SynergyShield==ward&&engaged.SynergyShieldUntil==expiry);
                            yield return ReunionEvidenceCapture(directory,"04-eight-tier-first-engagement-ward");
                            Battle.SetPaused(false);
                            for(int step=0;step<180&&Battle.Phase==BattlePhase.Running&&Battle.Clock<=expiry+.2;step++){Tick(.05f);if(step%4==0)yield return null;}
                            check("elite-ward-does-not-renew",!engaged.ReunionEntryShieldPending&&engaged.SynergyShieldUntil==expiry&&Battle.Clock>expiry);
                        }
                    }
                    Battle.SetPaused(false);var oldBattle=Battle;ReplaySandbox();yield return ReunionEvidenceReady();
                    check(prefix+"replay-clears-entities-muster-and-shields",!ReferenceEquals(Battle,oldBattle)&&Battle.Units.Count==0&&Battle.MusterUsed==0&&Battle.MusterMemberIds.Count==0&&Battle.Clock==0&&Battle.ReunionTier==tier);
                }
                completed=true;
            }
            finally
            {
                Time.timeScale=previousScale;inspectedSynergy=null;CancelShopDrag();result.passed=valid&&completed;result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("REUNION_COALITION_PLAYER_PASS="+result.passed+"; checks="+checks.Count);
            }
        }
    }

    // These flags are opt-in; ordinary startup never runs or quits a verification routine.
    public sealed class ReunionCoalitionSmokeDriver:MonoBehaviour
    {
        static bool installed;
        public static bool Requested=>Environment.GetCommandLineArgs().Any(a=>a=="-reunionCoalitionTest"||a=="-reunionTrial");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();bool smoke=Debug.isDebugBuild&&args.Contains("-reunionCoalitionTest");
            if(installed||(!smoke&&!args.Contains("-reunionTrial")))return;
            installed=true;var go=new GameObject("Reunion explicit verification");DontDestroyOnLoad(go);go.AddComponent<ReunionCoalitionSmokeDriver>();
        }
        IEnumerator Verify(string directory,bool smoke)
        {
            if(smoke)Application.runInBackground=true;
            yield return null;
            if(SceneManager.GetActiveScene().name!="ReunionTrial")yield return SceneManager.LoadSceneAsync("ReunionTrial");
            if(!smoke)yield break;
            double deadline=Time.realtimeSinceStartupAsDouble+600;ManualBattleController controller=null;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||!controller.Loop.CanShop||controller.ArtLoading||controller.ShopPanelTransitioning)throw new InvalidOperationException("ReunionTrial startup timed out.");
            yield return controller.VerifyReunionCoalition(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();bool smoke=Debug.isDebugBuild&&args.Contains("-reunionCoalitionTest");
            int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.GetFullPath("artifacts/reunion-player");
            if(smoke)Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Verify(directory,smoke));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    if(smoke){File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);}
                    yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            if(smoke)
            {
                bool passed=File.Exists(Path.Combine(directory,"result.json"))&&File.ReadAllText(Path.Combine(directory,"result.json")).Contains("\"passed\": true");
                Application.Quit(passed?0:1);
            }
            else Destroy(gameObject);
        }
    }
}
