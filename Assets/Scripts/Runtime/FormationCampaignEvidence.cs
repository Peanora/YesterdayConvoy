using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    /// <summary>Opt-in production-path Player verification. No actor HP, damage, stock or outcomes are injected.</summary>
    public sealed class FormationCampaignEvidence : MonoBehaviour
    {
        [Serializable] sealed class Check {public string name;public bool passed;}
        [Serializable] sealed class Report
        {
            public bool passed;public string error,scope;
            public int assertions,exceptionCount,battles;public string node;
            public List<Check> checks=new List<Check>();public List<string> exceptions=new List<string>();
        }
        static bool started;
        readonly Report result=new Report{scope="Actual v0.7 finite-economy campaign controller, production choices/shop/equipment and first three battles, smaller left deployment area, close fixed preparation views and enlarged battle-only view, coin-operated recruitment drawer, reserve-only bench and direct board deployment, with trait details and actual GPU screenshots. Battle and paused battle hide preparation UI and standing reserve models. Accelerated simulation uses normal model time. No forced victories or actor/stat injection. This is not a full natural campaign win-rate or OS mouse test."};
        FormationBattleController controller;string directory;bool done;
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationCampaignSmokeTest")>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>started=false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            if(!Requested||started)return;started=true;Application.runInBackground=true;
            var go=new GameObject("Full formation campaign verification");DontDestroyOnLoad(go);go.AddComponent<FormationCampaignEvidence>();
        }
        void Awake()=>Application.logMessageReceived+=Observe;
        void OnDestroy()=>Application.logMessageReceived-=Observe;
        void Observe(string text,string stack,LogType kind)
        {if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)result.exceptions.Add(kind+": "+text+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationEvidenceDir");
            directory=Path.GetFullPath(at>=0&&at+1<args.Length?args[at+1]:"artifacts/formation-combat-view/player");Directory.CreateDirectory(directory);
            return Guard(Verify());
        }
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0&&!done)
            {
                object next=null;Exception failure=null;bool more=false;
                try {if(result.exceptions.Count>0)throw new InvalidOperationException(result.exceptions[0]);more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null){Finish(failure);yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator child){stack.Push(child);continue;}
                yield return next;
            }
            if(!done)Finish(null);
        }
        void Assert(bool value,string name)
        {result.checks.Add(new Check{name=name,passed=value});if(!value)throw new InvalidOperationException(name);}
        IEnumerator Frame(string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}
        IEnumerator ReadyArt()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(!controller.RosterArtReady&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert(controller.RosterArtReady,"Every current actor has its actual original animation package");
        }
        IEnumerator Market(bool visible)
        {
            controller.SetMarketVisible(visible);double deadline=Time.realtimeSinceStartupAsDouble+3;
            while(controller.ShopPanelTransitioning&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert(controller.MarketVisible==visible&&!controller.ShopPanelTransitioning,"Preparation recruitment drawer settles "+(visible?"open":"closed"));
            yield return null;
        }
        bool EmptyCell(int instance,out Vector2Int cell)
        {
            for(int column=0;column<FormationBattleModel.DeploymentColumns;column++)for(int row=0;row<FormationBattleModel.Height;row++)
            {
                var candidate=new Vector2Int(column,row);var preview=controller.Session.PreviewDeploy(instance,candidate);
                if(preview.Valid&&preview.SwapInstance==0){cell=candidate;return true;}
            }
            cell=default;return false;
        }
        float GuardTileScreenWidth()
        {
            var cell=new Vector2(FormationBattleModel.DeploymentColumns+3,FormationBattleModel.Height*.5f);
            return Vector2.Distance(FormationBattleController.CellScreen(cell),FormationBattleController.CellScreen(cell+Vector2.right));
        }
        IEnumerator ResolveChoices()
        {
            int safety=0;
            while(controller.Campaign.HasBlockingChoice)
            {
                if(++safety>80)throw new InvalidOperationException("Choice queue did not terminate");
                var choices=controller.Campaign.Choices.ToArray();
                int pick=Array.FindIndex(choices,x=>x.Id=="P04"||x.Id=="P12"||x.Id=="P11");if(pick<0)pick=0;
                var transaction=controller.ChooseCampaignOption(pick);Assert(transaction.Success,"Choose production option: "+choices[pick].Name);
                yield return null;
            }
        }
        IEnumerator Verify()
        {
            yield return null;yield return null;
            if(SceneManager.GetActiveScene().name!=FormationBattleController.SceneName)
                yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {controller=FindAnyObjectByType<FormationBattleController>();if(controller!=null&&controller.Ready)break;yield return null;}
            Assert(controller!=null&&controller.Ready,"Full campaign controller became ready");
            Assert(controller.Campaign!=null&&!controller.Shop.UnlimitedGold,"Default formation mode uses finite v0.7 economy");
            Assert(controller.Shop.Gold==8&&controller.Shop.Level==3,"Independent random starter has 8 gold and level 3");
            Assert(controller.Shop.Bench.Count==3&&controller.Shop.Bench.Select(p=>p.Id).Distinct().Count()==3,"Three distinct random low-cost starter cards");
            Assert(FormationCampaignRules.AllUnits.Length==46&&controller.Shop.Roles.Count==46,"Formal shop contains exactly the 46 approved identities");
            Assert(FormationBattleModel.Width==24&&FormationBattleModel.Height==8&&FormationBattleModel.DeploymentColumns==10,"Smaller 10 by 8 left deployment area retains the 12 by 8 guard half");
            yield return Frame("01-opening-choice");
            Assert(controller.ChooseCampaignOption(0).Success,"Choose starter component");yield return null;
            var original=controller.Campaign.Choices.Select(c=>c.Id).ToArray();
            Assert(controller.Campaign.CanRerollChoice,"Preplan has one free whole-group reroll");
            Assert(controller.Campaign.RerollChoice(),"Reroll preplan");
            Assert(!controller.Campaign.Choices.Any(c=>original.Contains(c.Id)),"Preplan reroll excludes all original options");
            Assert(!controller.Campaign.CanRerollChoice,"Preplan cannot be rerolled twice");
            yield return Frame("02-investment-environment");yield return ResolveChoices();
            Assert(!controller.MarketVisible,"Opening choices leave recruitment drawer collapsed");
            yield return Market(false);controller.SetFormationView(false);yield return ReadyArt();
            Assert(controller.CurrentFormationCameraSafeRect==FormationBattleController.PreparationCameraSafeRect,"Preparation uses its close fixed camera frame");
            Assert(controller.FormationPreparationUiVisible&&!controller.CombatViewActive,"Preparation displays the operating layer");
            Assert(Enumerable.Range(0,11).All(i=>FormationBattleController.FormationLineupRect(i)==Rect.zero),"No second screen lineup rail remains");
            Assert(controller.FormationDisplayedInterest==Math.Min(controller.Shop.InterestCap,controller.Shop.Gold/controller.Shop.InterestDivisor),"Coin HUD shows the production interest forecast");
            yield return Frame("03-left-preparation");
            var cameraPose=controller.battleCamera.transform.position;controller.ToggleFormationMarket();yield return Market(true);
            Assert(controller.battleCamera.transform.position==cameraPose,"Opening drawer preserves the large board camera");
            yield return Frame("03-recruitment-drawer-open");yield return Market(false);
            controller.SetFormationView(true);yield return null;float scoutingTileWidth=GuardTileScreenWidth();yield return Frame("04-close-guard-area");controller.SetFormationView(false);
            string trait=FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles).First(t=>controller.Campaign.TraitCount(t.Id)>0).Id;
            Assert(controller.InspectFormationTrait(trait)&&controller.FormationTraitDetailsOpen&&controller.SelectedFormationTraitId==trait,"Open real lineup trait details");
            var spawns=controller.Model.Units.Select(u=>u.Spawn).ToArray();var detailPoint=FormationBattleController.FormationTraitDetailsRect.center;
            controller.ProcessPointer(EventType.MouseDown,detailPoint);controller.ProcessPointer(EventType.MouseDrag,detailPoint+Vector2.right*12);controller.ProcessPointer(EventType.MouseUp,detailPoint+Vector2.right*12);
            Assert(controller.Model.Units.Select(u=>u.Spawn).SequenceEqual(spawns),"Trait detail clicks cannot move the underlying army");
            yield return Frame("04-trait-members-and-tiers");
            Assert(controller.CloseFormationTraitDetails()&&!controller.FormationTraitDetailsOpen,"Close trait details through its production action");
            for(int battle=0;battle<3;)
            {
                if(controller.Campaign.CurrentNode.Kind=="补给")
                {yield return ResolveChoices();Assert(controller.ContinueCampaign(),"Leave supply through normal node transition");continue;}
                yield return ResolveChoices();
                yield return Market(true);
                // Buy affordable duplicates or a first useful new kind; no free-card or money injection.
                for(int slot=0;slot<5;slot++)
                {
                    string id=controller.Shop.Offers[slot];if(id==null)continue;
                    bool owned=controller.Shop.Bench.Any(p=>p.Id==id);
                    if(controller.Shop.CanAfford(controller.Shop.Definition(id).Cost)&&(owned||controller.Shop.Bench.Count<5))controller.BuyOffer(slot);
                    yield return ResolveChoices();
                }
                if(controller.Shop.Gold>=14&&controller.Shop.Level<5)controller.BuyExperience();
                yield return Market(false);
                foreach(var p in controller.Shop.Bench.ToArray())
                    if(!controller.Session.Seats.InLineup(p.Instance)&&controller.Model.EnabledCount<controller.Shop.PopulationCap&&!controller.Model.Units.Any(u=>u.ArtId==FormationUnitCatalog.Find(p.Id).ArtId)&&EmptyCell(p.Instance,out var cell))
                    {
                        var origin=controller.FormationPieceRect(p.Instance).center;var destination=FormationBattleController.CellScreen(cell);
                        controller.ProcessPointer(EventType.MouseDown,origin);controller.ProcessPointer(EventType.MouseDrag,destination);controller.ProcessPointer(EventType.MouseUp,destination);
                        Assert(controller.Session.Seats.InLineup(p.Instance),"Purchased reserve deploys directly onto an actual board cell");
                    }
                var core=controller.Session.Seats.Lineup(controller.Shop.Bench).FirstOrDefault();
                foreach(var item in controller.Campaign.Inventory.ToArray())if(core!=null&&item.Key.Length<=2)controller.Campaign.Equip(item.Instance,core.Instance);
                yield return ReadyArt();controller.SetMarketVisible(false);
                controller.StartBattle();Assert(controller.Model.Phase==FormationPhase.Fighting,"Start real auto battle "+controller.Campaign.CurrentNode.Id);
                controller.Model.Paused=true;yield return null;
                Assert(controller.CombatViewActive&&!controller.FormationPreparationUiVisible,"Actual and paused battle hide traits, reserve, gold, XP and operating toolbar "+battle);
                Assert(controller.CurrentFormationCameraSafeRect==FormationBattleController.BattleCameraSafeRect&&GuardTileScreenWidth()>scoutingTileWidth*1.08f,"Battle fills the released screen with visibly larger guard tiles "+battle);
                Assert(controller.Shop.Bench.All(piece=>(controller.ShopBenchModel(piece.Instance)==null||!controller.ShopBenchModel(piece.Instance).enabled)&&(controller.ShopEntranceModel(piece.Instance)==null||!controller.ShopEntranceModel(piece.Instance).enabled)),"Battle hides every real reserve and arrival renderer "+battle);
                controller.ToggleFormationMarket();controller.SetMarketVisible(true);
                Assert(!controller.MarketVisible&&!controller.InspectFormationTrait(trait)&&!controller.FormationTraitDetailsOpen,"Battle actions cannot reopen preparation windows "+battle);
                double pausedClock=controller.Model.Clock;controller.Model.Advance(2);yield return null;
                Assert(controller.Model.Clock==pausedClock&&controller.CombatViewActive&&!controller.FormationPreparationUiVisible,"Pause preserves battle clock and battle-only layout "+battle);
                if(battle==0)yield return Frame("05-combat-paused-no-operating-ui");
                controller.Model.Paused=false;
                int guard=0;
                while(controller.Model.Phase==FormationPhase.Fighting||controller.Model.Phase==FormationPhase.Exiting)
                {
                    for(int i=0;i<12;i++)controller.Model.Advance(.25f);
                    if(++guard>3000)throw new InvalidOperationException("Battle failed to terminate");
                    if(guard==4)yield return Frame("05-combat-"+battle);
                    yield return null;
                }
                yield return null; // Production Update settles this actual outcome.
                Assert(controller.Campaign.NodeSettled,"Battle receipt uses actual result");
                Assert(controller.Model.Phase==FormationPhase.Won||controller.Model.Phase==FormationPhase.Lost,"Battle has a legal terminal phase");
                Assert(!controller.CombatViewActive&&controller.FormationPreparationUiVisible,"Natural settlement restores the operating layout "+battle);
                result.battles=++battle;result.node=controller.Campaign.CurrentNode.Id;
                yield return Frame("06-result-"+battle);
                yield return ResolveChoices();
                if(controller.Campaign.Finished)break;
                Assert(controller.ContinueCampaign(),"Continue after production receipt");
                yield return null;
                Assert(controller.FormationPreparationUiVisible&&!controller.CombatViewActive,"The next node restores preparation UI after actual settlement "+battle);
            }
            Assert(result.battles==3,"First three real battles completed and settled");
            Assert(controller.Campaign.AugmentIds.Count==1,"First strategic readiness was selected before 1-3 combat");
            yield return Frame("07-post-three-battles");
        }
        void Finish(Exception error)
        {
            done=true;result.error=error?.ToString();result.exceptionCount=result.exceptions.Count;result.assertions=result.checks.Count;
            result.passed=error==null&&result.exceptionCount==0;File.WriteAllText(Path.Combine(directory,"campaign-smoke.json"),JsonUtility.ToJson(result,true));
            Debug.Log(result.passed?"FORMATION_CAMPAIGN_SMOKE_PASS":"FORMATION_CAMPAIGN_SMOKE_FAIL: "+result.error);
            Application.Quit(result.passed?0:1);
        }
    }
}
