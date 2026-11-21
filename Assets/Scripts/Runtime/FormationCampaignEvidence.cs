using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    /// <summary>Opt-in production-path Player verification.</summary>
    public sealed partial class FormationCampaignEvidence : MonoBehaviour
    {
        [Serializable] sealed class Check {public string name;public bool passed;}
        [Serializable] sealed class Report
        {
            public bool passed;public string error,scope;
            public int assertions,exceptionCount,battles;public string node;
            public List<Check> checks=new List<Check>();public List<string> exceptions=new List<string>();
            public List<DefenseStage> defenseStages = new List<DefenseStage>();
            public List<SynergyStage> synergyStages = new List<SynergyStage>();
            public SnowBossReplay snowBoss;
            public SupernovaBossReplay supernovaBoss;
        }
        [Serializable] sealed class DefenseStage
        {
            public string id, side, outcome;
            public int initialHealth, remaining, alliedDeaths;
            public double battleSeconds;
            public List<string> filmedCasts = new List<string>();
            public List<string> observedSkillHits = new List<string>();
        }
        [Serializable] sealed class SynergyStage
        {
            public string id, node, outcome, settlement, golem;
            public int tier, skillCasts, remaining;
            public double firstContact, battleSeconds;
            public float guardHealthLoss;
            public string[] events;
        }
        static bool started;
        readonly Report result=new Report{scope="Production v0.7 choices, shop, deployments, equipment, ground rewards and the first three battles on the joined board. Left preparation uses the same tile scale as combat. Screenshots come from the Windows Player. Accelerated battles use normal simulation time through the campaign controller."};
        FormationBattleController controller;string directory;bool done;
        internal static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationCampaignSmokeTest")>=0 || DefenseRequested || SynergyRequested || SnowBossRequested || SupernovaBossRequested || MapVariantsRequested;
        static bool DefenseRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationDefenseSmokeTest")>=0;
        static bool DefenseSkillProbe=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationDefenseSkillProbe")>=0;
        static bool SynergyRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationSynergySmokeTest")>=0;
        static bool ReserveRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationReserveEvidence")>=0;
        static bool StrategyHistoryRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationStrategyHistoryEvidence")>=0;
        static bool OpeningDeliveryRequested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationOpeningDeliveryEvidence")>=0;
        const string RewardDropEvidenceFlag = "-formationRewardDropEvidence";
        static bool RewardDropEvidenceRequested => Array.IndexOf(Environment.GetCommandLineArgs(), RewardDropEvidenceFlag) >= 0;
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
            var fallback = MapVariantsRequested ? "artifacts/formation-map-variants/player" :
                SupernovaBossRequested ? "artifacts/formation-supernova-boss/player" :
                SnowBossRequested ? "artifacts/formation-snow-boss/player" : SynergyRequested ? "artifacts/formation-synergy-trials/player" :
                DefenseRequested ? "artifacts/formation-defense/player" : "artifacts/formation-combat-view/player";
            directory=Path.GetFullPath(at>=0&&at+1<args.Length?args[at+1]:fallback);Directory.CreateDirectory(directory);
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
        IEnumerator CaptureInterfaceClip(string name, int frames, string flag = "-formationUiClips",
            Func<bool> continueRecording = null, Action<int> beforeFrame = null)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), flag) < 0) yield break;
            string target = Path.Combine(directory, name);
            Directory.CreateDirectory(target);
            double start = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < frames; i++)
            {
                while (Time.realtimeSinceStartupAsDouble < start + i / 24d) yield return null;
                if (continueRecording != null && !continueRecording()) break;
                yield return new WaitForEndOfFrame();
                beforeFrame?.Invoke(i);
                ScreenCapture.CaptureScreenshot(Path.Combine(target, i.ToString("D3") + ".png"));
            }
            yield return null;
        }
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
        static float TileScreenWidth(int column)
        {
            var cell = new Vector2(column, FormationBattleModel.Height / 2);
            return Vector2.Distance(FormationBattleController.CellScreen(cell),FormationBattleController.CellScreen(cell+Vector2.right));
        }
        IEnumerator WaitForScoutingView(bool defenders)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(!controller.CameraTransitioning && controller.ViewingDefenders == defenders,
                defenders ? "Camera settled on the guard formation" : "Camera settled on the editable formation");
        }
        IEnumerator PanScoutingView(bool defenders, bool clickArrow)
        {
            float start = controller.battleCamera.transform.position.x;
            if (clickArrow)
                yield return ClickStrategyUi(FormationBattleController.FormationScoutRect.center);
            else
                controller.SetFormationView(defenders);
            Assert(controller.ViewingDefenders == defenders && controller.CameraTransitioning && !controller.CanEditFormationView,
                defenders ? "Scouting begins the rightward pan" : "Returning begins the leftward pan");
            double deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (Mathf.Abs(controller.battleCamera.transform.position.x - start) <= .02f &&
                controller.CameraTransitioning && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            float moving = controller.battleCamera.transform.position.x;
            Assert(controller.CameraTransitioning, "The board has a rendered intermediate position");
            yield return CaptureInterfaceClip(defenders ? "scout-pan-right" : "scout-pan-left", 16);
            yield return WaitForScoutingView(defenders);
            float end = controller.battleCamera.transform.position.x;
            Assert(defenders ? start < moving && moving < end : end < moving && moving < start,
                "Preparation camera travels through the intervening board cells");
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
            if (MapVariantsRequested)
            {
                yield return VerifyMapVariants();
                yield break;
            }
            if (SupernovaBossRequested)
            {
                yield return VerifySupernovaBossTrial();
                yield break;
            }
            if (SnowBossRequested)
            {
                yield return VerifySnowBossTrial();
                yield break;
            }
            if (SynergyRequested)
            {
                yield return VerifySynergyTrials();
                yield break;
            }
            if (DefenseRequested)
            {
                yield return VerifyDefenses();
                yield break;
            }
            if (StrategyHistoryRequested)
            {
                result.scope = "Visible Windows Player with actual mouse and keyboard input. Two real fights reach 1-3; ordinary settlement and node transition with fixture wins between later checkpoints reach 2-2 and 3-2. Covers reveal, single reroll, preselection, confirmation, history, click-through protection and fresh challenge reset.";
                yield return VerifyStrategyHistory();
                yield break;
            }
            if (OpeningDeliveryRequested)
            {
                result.scope = "Visible Windows Player through actual automatic opening delivery, preplan reveal and physical keyboard and mouse confirmation. Covers normal startup and opening again from a fresh challenge.";
                yield return VerifyOpeningDelivery();
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-formationStrategyEvidence") >= 0)
            {
                result.scope = "Production strategy panels on the authored board, with three seeded runs through two actual auto battles each. Choice confirmation is driven through the campaign API.";
                yield return VerifyStrategyVariants();
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-formationItemArtEvidence") >= 0)
            {
                result.scope = "Formal Windows board and real badge choice, inventory and equipment UI. The recipe materials are fixed test grants.";
                yield return VerifyItemArt();
                yield break;
            }
            Assert(controller.Shop.Gold==8&&controller.Shop.Level==3,"Independent random starter has 8 gold and level 3");
            Assert(FormationCampaignRules.AllUnits.Length==46&&controller.Shop.Roles.Count==46,"Formal shop contains exactly the 46 approved identities");
            Assert(FormationBattleModel.Width==22&&FormationBattleModel.Height==8&&FormationBattleModel.DeploymentColumns==10,
                "The joined board has a 10 by 8 deployment area and a 10 by 8 guard area");
            yield return WaitForOpeningDelivery("01-opening-delivery-and-preplan");
            if (ReserveRequested)
            {
                yield return VerifyReserveOverflow();
                yield break;
            }
            var original=controller.Campaign.Choices.Select(c=>c.Id).ToArray();
            Assert(controller.Campaign.CanRerollChoice,"Preplan has one free whole-group reroll");
            Assert(controller.Campaign.RerollChoice(),"Reroll preplan");
            Assert(!controller.Campaign.Choices.Any(c=>original.Contains(c.Id)),"Preplan reroll excludes all original options");
            Assert(!controller.Campaign.CanRerollChoice,"Preplan cannot be rerolled twice");
            yield return Frame("02-investment-environment");yield return ResolveChoices();
            Assert(!controller.MarketVisible,"Opening choices leave recruitment drawer collapsed");
            DeployOpeningRecruits();
            yield return Market(false);controller.SetFormationView(false);yield return WaitForScoutingView(false);yield return ReadyArt();
            Assert(controller.CurrentFormationCameraSafeRect==FormationBattleController.LeftPreparationCameraSafeRect,"Preparation uses its close fixed camera frame");
            Assert(controller.FormationPreparationUiVisible&&!controller.CombatViewActive,"Preparation displays the operating layer");
            Assert(Enumerable.Range(0,11).All(i=>FormationBattleController.FormationLineupRect(i)==Rect.zero),"No second screen lineup rail remains");
            Assert(controller.FormationDisplayedInterest==Math.Min(controller.Shop.InterestCap,controller.Shop.Gold/controller.Shop.InterestDivisor),"Coin HUD shows the production interest forecast");
            Assert(FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles)
                .All(rule => SynergyBadgePresentation.IconFor(rule.Name)!=null), "All 18 production trait emblem textures load in Player");
            Assert(FormationCampaignRules.AllComponents.Concat(FormationCampaignRules.AllItems)
                .All(item => FormationBattleController.SupplyItemArt(item.Id)!=null), "All 27 approved equipment textures load in Player");
            Assert(new[]{"白","蓝","金","彩"}.All(color=>FormationBattleController.SupplyOrbArt(color)!=null),
                "All four reward orb textures load in Player");
            yield return Frame("03-left-preparation");
            Assert(controller.FormationRouteNodeRect(0).width>0&&controller.FormationRouteNodeRect(6).width>0,
                "Progress rail presents current and encounter nodes from the real first plane");
            controller.ToggleFormationRouteNode(6);
            yield return Frame("03-node-encounter-detail");
            controller.ToggleFormationRouteNode(6);
            var inspecting=controller.Model.Units.First(unit=>unit.Enabled);
            var actorPoint=controller.FormationPieceRect(inspecting.Instance).center;
            var expected=controller.Shop.Bench.First(piece=>piece.Instance==inspecting.Instance).Id;
            var beforeInspect=inspecting.Spawn;
            controller.ProcessPointer(EventType.MouseDown,actorPoint);
            controller.ProcessPointer(EventType.MouseUp,actorPoint);
            Assert(controller.FormationInspectedCardId==expected&&inspecting.Spawn==beforeInspect,
                "Clicking a real deployed body opens its inspection without moving the unit");
            yield return Frame("03-board-role-detail");
            controller.ProcessPointer(EventType.MouseDown,actorPoint);
            controller.ProcessPointer(EventType.MouseUp,actorPoint);
            Assert(controller.FormationInspectedCardId==null,"Clicking the same body dismisses its information");
            var cameraPose=controller.battleCamera.transform.position;controller.ToggleFormationMarket();yield return Market(true);
            Assert(controller.battleCamera.transform.position==cameraPose,"Opening drawer preserves the large board camera");
            yield return Frame("03-recruitment-drawer-open");
            yield return Market(false);
            bool clickScoutArrow = Array.IndexOf(Environment.GetCommandLineArgs(), "-formationScoutMouse") >= 0;
            if (clickScoutArrow) result.scope += " Scouting arrows use actual Windows mouse input.";
            yield return PanScoutingView(true, clickScoutArrow);
            int guardSampleColumn = FormationBattleModel.DeploymentColumns + 3;
            float scoutingTileWidth = TileScreenWidth(guardSampleColumn);
            yield return Frame("04-close-guard-area");
            yield return PanScoutingView(false, clickScoutArrow);
            string trait=FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles).First(t=>controller.Campaign.TraitCount(t.Id)>0).Id;
            Assert(controller.InspectFormationTrait(trait)&&controller.FormationTraitDetailsOpen&&controller.SelectedFormationTraitId==trait,"Open real lineup trait details");
            var spawns=controller.Model.Units.Select(u=>u.Spawn).ToArray();var detailPoint=FormationBattleController.FormationTraitDetailsRect.center;
            controller.ProcessPointer(EventType.MouseDown,detailPoint);controller.ProcessPointer(EventType.MouseDrag,detailPoint+Vector2.right*12);controller.ProcessPointer(EventType.MouseUp,detailPoint+Vector2.right*12);
            Assert(controller.Model.Units.Select(u=>u.Spawn).SequenceEqual(spawns),"Trait detail clicks cannot move the underlying army");
            yield return CaptureInterfaceClip("trait-detail-reveal", 8);
            yield return new WaitForSecondsRealtime(FormationBattleController.TraitRevealSeconds);
            yield return Frame("04-trait-members-and-tiers");
            Assert(controller.CloseFormationTraitDetails()&&!controller.FormationTraitDetailsOpen,"Close trait details through its production action");
            var activeTrait = controller.FormationTraitRows.First(row => row.Active);
            Assert(controller.InspectFormationTrait(activeTrait.Id) &&
                controller.SelectedFormationTrait.ActiveLevel == activeTrait.ActiveLevel,
                "The warm-gray details show the naturally attained starter tier");
            yield return new WaitForSecondsRealtime(FormationBattleController.TraitRevealSeconds);
            yield return Frame("04-trait-active-tier");
            Assert(controller.CloseFormationTraitDetails(), "Close the active trait panel before operating the bench");
            yield return VerifyStarterTraitActivation();
            for(int battle=0;battle<3;)
            {
                if(controller.Campaign.CurrentNode.Kind=="补给")
                {yield return ResolveChoices();Assert(controller.ContinueCampaign(),"Leave supply through normal node transition");continue;}
                yield return ResolveChoices();
                if(battle==2)yield return VerifyEquipmentDragCombination();
                yield return Market(true);
                // Buy affordable duplicates or a first useful new kind; no free-card or money injection.
                for(int slot=0;slot<5;slot++)
                {
                    string id=controller.Shop.Offers[slot];if(id==null)continue;
                    bool owned=controller.Shop.Bench.Any(p=>p.Id==id);
                    int price=controller.Shop.Definition(id).Cost;
                    if(controller.Shop.CanAfford(price)&&(owned||controller.Shop.Bench.Count<5))
                    {
                        bool canFit=controller.Shop.PreviewPurchase(slot).Success;
                        int beforeGold=controller.Shop.Gold;
                        var card=FormationBattleController.FormationOfferRect(slot).center;
                        controller.ProcessPointer(EventType.MouseDown,card);
                        controller.ProcessPointer(EventType.MouseUp,card);
                        if(canFit)
                            Assert(controller.Shop.Gold==beforeGold-price&&controller.Shop.Offers[slot]==null&&
                                controller.FormationInspectedCardId==null,"Click the shop card to recruit one copy without opening details");
                        else
                            Assert(controller.Shop.Gold==beforeGold&&controller.Shop.Offers[slot]==id&&
                                controller.FormationInspectedCardId==null,"A card that cannot fit stays on sale without charging or opening details");
                    }
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
                while (controller.Session.Seats.OverflowCount > 0)
                {
                    int overflow = controller.Session.Seats.At(false, ShopSeatLayout.ReserveCapacity);
                    Assert(controller.SellPiece(overflow).Success, "Sell an extra reward recruit to clear battle-blocking overflow");
                }
                var core=controller.Session.Seats.Lineup(controller.Shop.Bench).FirstOrDefault();
                foreach(var item in controller.Campaign.Inventory.ToArray())if(core!=null&&item.Key.Length<=2)controller.Campaign.Equip(item.Instance,core.Instance);
                yield return ReadyArt();controller.SetMarketVisible(false);
                float deploymentTileWidth = TileScreenWidth(3);
                controller.StartBattle();Assert(controller.Model.Phase==FormationPhase.Fighting,"Start real auto battle "+controller.Campaign.CurrentNode.Id);
                controller.Model.Paused=true;yield return null;
                Assert(controller.CombatViewActive&&!controller.FormationPreparationUiVisible,"Actual and paused battle hide traits, reserve, gold, XP and operating toolbar "+battle);
                float battleTileWidth = TileScreenWidth(guardSampleColumn);
                Assert(controller.CurrentFormationCameraSafeRect==FormationBattleController.BattleCameraSafeRect&&battleTileWidth>scoutingTileWidth*1.08f,"Battle fills the released screen with visibly larger guard tiles "+battle);
                Assert(Mathf.Abs(deploymentTileWidth - battleTileWidth) < .1f,
                    $"Deployment and battle tile widths match at {deploymentTileWidth:F2} / {battleTileWidth:F2} pixels in battle {battle + 1}");
                Assert(controller.Shop.Bench.All(piece=>(controller.ShopBenchModel(piece.Instance)==null||!controller.ShopBenchModel(piece.Instance).enabled)&&(controller.ShopEntranceModel(piece.Instance)==null||!controller.ShopEntranceModel(piece.Instance).enabled)),"Battle hides every real reserve and arrival renderer "+battle);
                controller.ToggleFormationMarket();controller.SetMarketVisible(true);
                Assert(!controller.MarketVisible&&!controller.InspectFormationTrait(trait)&&!controller.FormationTraitDetailsOpen,"Battle actions cannot reopen preparation windows "+battle);
                double pausedClock=controller.Model.Clock;controller.Model.Advance(2);yield return null;
                Assert(controller.Model.Clock==pausedClock&&controller.CombatViewActive&&!controller.FormationPreparationUiVisible,"Pause preserves battle clock and battle-only layout "+battle);
                if(battle==0)yield return Frame("05-combat-paused-no-operating-ui");
                controller.Model.Paused=false;
                if (battle == 0 && RewardDropEvidenceRequested) yield return VerifyBattleRewardDrop();
                if (battle == 2 && RewardDropEvidenceRequested)
                    Assert(controller.Campaign.GuardRewardCueLimit(1, controller.Model.InitialDefenderCount) == 0,
                        "Ordinary 1-3 combat has no reward drop cues, even at full progress");
                int guard=0;
                while(controller.Model.Phase==FormationPhase.Fighting||controller.Model.Phase==FormationPhase.Exiting)
                {
                    for(int i=0;i<12;i++)controller.Model.Advance(.25f);
                    if(++guard>3000)throw new InvalidOperationException("Battle failed to terminate");
                    if(guard==4)yield return Frame("05-combat-"+battle);
                    yield return null;
                }
                yield return null; // Production Update settles this actual outcome.
                if (RewardDropEvidenceRequested)
                    Assert(controller.ActiveBattleRewardDrops == 0, "No battle reward effect survives settlement");
                Assert(controller.Campaign.NodeSettled,"Battle receipt uses actual result");
                Assert(controller.Model.Phase==FormationPhase.Won||controller.Model.Phase==FormationPhase.Lost,"Battle has a legal terminal phase");
                Assert(!controller.CombatViewActive&&controller.FormationPreparationUiVisible,"Natural settlement restores the operating layout "+battle);
                result.battles=++battle;result.node=controller.Campaign.CurrentNode.Id;
                yield return Frame("06-result-"+battle);
                yield return ResolveChoices();
                if (battle == 1)
                {
                    Assert(controller.Campaign.PendingBalls.Count(ball=>ball.Component==null) == 4 && controller.Campaign.BallRewards.Count == 0,
                        "First real reward settlement drops four unopened balls without payouts");
                    if (RewardDropEvidenceRequested)
                    {
                        yield return new WaitForSecondsRealtime(.8f);
                        yield return Frame("06-first-reward-platform-scatter");
                    }
                    var dropped = controller.Campaign.PendingBalls.Select(ball=>ball.Id).ToArray();
                    Assert(controller.ContinueCampaign(),"Unopened ground rewards do not block the next node");
                    yield return WaitForScoutingView(false);
                    Assert(controller.Campaign.CurrentNode.Id=="1-2"&&controller.Campaign.PendingBalls.Select(ball=>ball.Id).SequenceEqual(dropped),
                        "Every original drop persists in the next preparation");
                    yield return Frame("06-carried-drops-1-2");
                    continue;
                }
                if(battle==2)yield return VerifyConcurrentGroundRewards();
                yield return ClaimGroundRewards(battle == 2);
                if (battle == 2)
                {
                    var balls = controller.Campaign.BallRewards;
                    Assert(balls.Count(ball=>ball.Node=="1-1"&&ball.Color.Length>0)==4&&
                        balls.Count(ball=>ball.Node=="1-2"&&ball.Color.Length>0)==5,"Claim all nine balls with their original nodes");
                    Assert(balls.All(ball => ball.Gold > 0 || ball.Refreshes > 0 || ball.Items.Count > 0 ||
                        ball.Recruit != null || ball.Choice != null), "Each reward animation has a real resolved payout");
                    yield return Frame("06-rewards-revealed");
                }
                if(controller.Campaign.Finished)break;
                Assert(controller.ContinueCampaign(),"Continue after production receipt");
                yield return null;
                Assert(controller.FormationPreparationUiVisible&&!controller.CombatViewActive,"The next node restores preparation UI after actual settlement "+battle);
            }
            Assert(result.battles==3,"First three real battles completed and settled");
            Assert(controller.Campaign.AugmentIds.Count==1,"First strategic readiness was selected before 1-3 combat");
            yield return Frame("07-post-three-battles");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-formationRewardRarityEvidence") >= 0)
            {
                foreach (string color in new[] {"白","蓝","蓝","金","白","彩"}) controller.Campaign.OpenBall(color);
                yield return new WaitForSecondsRealtime(.9f);
                yield return Frame("08-four-rarities-ground");
                yield return ClaimGroundRewards(true, "rarity");
                for(int attempt=0;controller.Campaign.PendingRewardChoices==0&&attempt<20;attempt++)
                {
                    controller.Campaign.OpenBall("彩");
                    yield return ClaimGroundRewards(false);
                }
                Assert(controller.Campaign.PendingRewardChoices>0&&!controller.Campaign.HasBlockingChoice,
                    "Opening an equipment or badge choice does not block preparation");
                Assert(controller.OpenGroundRewardChoices(),"Open the optional choice on demand");
                yield return Frame("09-deferred-reward-choice");
                Assert(controller.ChooseGroundRewardOption(0).Success,"Claim one queued option on demand");
            }
        }
        void DeployOpeningRecruits()
        {
            foreach (var piece in controller.Shop.Bench.Take(3).ToArray())
            {
                int seat = controller.Session.Seats.FirstEmpty(true, controller.Shop.PopulationCap);
                Assert(controller.MovePiece(piece.Instance, true, seat).Success, "Explicitly deploy opening recruit " + piece.Id);
            }
        }

        void Click(Vector2 point)
        {
            controller.ProcessPointer(EventType.MouseDown, point);
            controller.ProcessPointer(EventType.MouseUp, point);
        }

        void DragReserveOnBoard(int instance, Vector2Int cell)
        {
            var origin = controller.FormationPieceRect(instance);
            var target = FormationBattleController.CellScreen(cell);
            Assert(origin.width > 0 && !controller.Session.Seats.InLineup(instance) &&
                controller.Session.PreviewDeploy(instance, cell).Valid, "Visible reserve or overflow card has a legal board destination");
            controller.ProcessPointer(EventType.MouseDown, origin.center);
            controller.ProcessPointer(EventType.MouseDrag, target);
            controller.ProcessPointer(EventType.MouseUp, target);
            Assert(controller.Session.Seats.InLineup(instance) &&
                controller.Model.Units.Single(unit => unit.Instance == instance).Spawn == (Vector2)cell,
                "Dragging the visible card deploys it to the intended board cell");
        }

        IEnumerator VerifyReserveOverflow()
        {
            result.scope = "Actual authored Windows Player. Opening balls, selection, drag, shop and combat use production paths. Fixed reward grants fill the reserve after the random opening. Input is driven through game pointer handlers.";
            yield return ResolveChoices();
            yield return Market(false);
            controller.StartBattle();
            Assert(controller.Model.Phase == FormationPhase.Preparation && controller.Model.EnabledCount == 0,
                "After opening choices the empty board still requires deployment");
            yield return Frame("02-opening-recruits-on-reserve");

            int field = controller.Shop.Bench[0].Instance;
            int onBench = controller.Shop.Bench[1].Instance;
            var anchor = new Vector2Int(4, 3);
            DragReserveOnBoard(field, anchor);
            yield return ReadyArt();
            var body = controller.FormationPieceRect(field).center;
            var empty = FormationBattleController.CellScreen(new Vector2Int(1, 5));
            Click(body);
            Assert(controller.FormationInspectedCardId == controller.Shop.Bench[0].Id, "A board click selects and inspects the actor");
            Click(empty);
            Assert(controller.Model.Units.Single().Spawn == (Vector2)anchor, "Selecting an on-board actor and clicking empty ground leaves its spawn fixed");
            if (controller.FormationInspectedCardId != null) Click(body);
            var reserve = controller.FormationPieceRect(onBench).center;
            Click(reserve);
            Click(empty);
            Assert(!controller.Session.Seats.InLineup(onBench) && controller.Model.EnabledCount == 1,
                "Selecting a reserve card and clicking empty ground does not deploy it");
            if (controller.FormationInspectedCardId != null) Click(reserve);

            const int returnSeat = 5;
            var bench = FormationBattleController.FormationReserveRect(returnSeat).center;
            var gap = new Vector2(bench.x, FormationBattleController.FormationBenchRect.yMin - 14);
            controller.ProcessPointer(EventType.MouseDown, body);
            controller.ProcessPointer(EventType.MouseDrag, gap);
            Assert(controller.DragPreviewVisible && controller.DeploymentPreview == null &&
                controller.DeploymentGhost.sprite != null, "The held source idle frame remains visible in the gap above the bench");
            yield return Frame("03-held-above-reserve");
            controller.ProcessPointer(EventType.MouseDrag, bench);
            Assert(controller.DragPreviewVisible && controller.DeploymentPreview == null &&
                controller.DeploymentGhost.sprite != null, "The held source idle frame remains visible over the reserve slot");
            yield return Frame("04-held-over-reserve");
            controller.ProcessPointer(EventType.MouseUp, bench);
            Assert(controller.Session.Seats.At(false, returnSeat) == field && controller.Model.EnabledCount == 0 &&
                !controller.DragPreviewVisible, "Releasing the held model moves it into that reserve slot");
            DragReserveOnBoard(field, anchor);

            int offerSlot = Enumerable.Range(0, controller.Shop.Offers.Count).First(index =>
                controller.Shop.Offers[index] != null && controller.Shop.Bench.All(piece => piece.Id != controller.Shop.Offers[index]));
            string offerId = controller.Shop.Offers[offerSlot];
            var fixture = controller.Shop.Roles.Where(role => role.Cost <= 2 && role.Id != offerId &&
                controller.Shop.Bench.All(piece => piece.Id != role.Id)).Select(role => role.Id).ToArray();
            int slotsToFill = ShopSeatLayout.ReserveCapacity - (controller.Shop.Bench.Count - controller.Model.EnabledCount);
            foreach (string id in fixture.Take(slotsToFill))
                Assert(controller.Shop.Grant(id).Success, "Grant a fixed test recruit to fill the reserve");
            controller.Session.ApplyTo(controller.Model);
            Assert(controller.Session.Seats.ReserveFull && controller.Session.Seats.OverflowCount == 0,
                "The ten regular reserve seats can all be filled without overflow");
            yield return Market(true);
            int gold = controller.Shop.Gold;
            var offer = FormationBattleController.FormationOfferRect(offerSlot).center;
            Click(offer);
            Assert(!controller.Shop.PreviewPurchase(offerSlot).Success && controller.Shop.Gold == gold &&
                controller.Shop.Offers[offerSlot] == offerId, "A full reserve blocks the visible card purchase without spending gold");
            yield return Frame("05-full-reserve-no-overflow");
            yield return Market(false);

            var first = controller.Shop.Grant(fixture[slotsToFill]);
            var ticket = controller.Shop.ReserveRewardCandidate(fixture[slotsToFill + 1]);
            var second = controller.Shop.GrantReserved(ticket);
            controller.Session.ApplyTo(controller.Model);
            var firstRect = controller.FormationPieceRect(first.Instance);
            var secondRect = controller.FormationPieceRect(second.Instance);
            Assert(first.Success && second.Success && controller.Session.Seats.OverflowCount == 2 &&
                firstRect.width > 0 && secondRect.width > 0 && firstRect.center.x > 850 && secondRect.center.x > 850 &&
                firstRect.center.y < 400 && secondRect.center.y < 400 && !firstRect.Overlaps(secondRect),
                "Both awarded recruits occupy distinct visible positions at the upper right");
            yield return Frame("06-two-overflow-recruits");
            controller.StartBattle();
            Assert(controller.Model.Phase == FormationPhase.Preparation && controller.Model.EnabledCount == 1,
                "Overflow blocks combat even with a legal deployed actor");
            yield return Market(true);
            Click(offer);
            Assert(controller.Shop.Gold == gold && controller.Shop.Offers[offerSlot] == offerId,
                "An unresolved overflow also blocks card purchase");
            yield return Market(false);

            DragReserveOnBoard(second.Instance, new Vector2Int(6, 2));
            Assert(controller.Session.Seats.OverflowCount == 1, "An overflow recruit itself can deploy through the ordinary board drag");
            int regularSeat = controller.Session.Seats.For(onBench).Value.Index;
            DragReserveOnBoard(onBench, new Vector2Int(6, 5));
            Assert(controller.Session.Seats.OverflowCount == 0 && controller.Session.Seats.ReserveFull &&
                controller.Session.Seats.At(false, regularSeat) == first.Instance,
                "Deploying a reserve recruit automatically fills that vacancy with the remaining overflow recruit");
            yield return Frame("07-overflow-cleared-on-board-deployment");
            yield return Market(true);
            Click(offer);
            Assert(controller.Shop.Gold == gold && controller.Shop.Offers[offerSlot] == offerId,
                "Purchases remain blocked while all ten regular slots are still occupied");
            Assert(controller.SellPiece(first.Instance).Success && !controller.Session.Seats.ReserveFull,
                "One ordinary sale exposes a real reserve vacancy");
            int beforePurchase = controller.Shop.Gold;
            Click(offer);
            Assert(controller.Shop.Offers[offerSlot] == null &&
                controller.Shop.Gold == beforePurchase - controller.Shop.Definition(offerId).Cost &&
                controller.Session.Seats.At(false, regularSeat) > 0 &&
                controller.Shop.Bench.Any(piece => piece.Id == offerId && !controller.Session.Seats.InLineup(piece.Instance)),
                "The same offer becomes purchasable once a regular slot is available and lands in reserve");
            yield return Market(false);
            yield return ReadyArt();
            controller.StartBattle();
            Assert(controller.Model.Phase == FormationPhase.Fighting && controller.Session.Seats.ReserveFull &&
                controller.Session.Seats.OverflowCount == 0, "Ten regular reserve recruits allow battle once overflow is cleared");
            controller.Model.Paused = true;
            yield return Frame("08-combat-after-overflow-resolved");
        }

        IEnumerator VerifyStrategyVariants()
        {
            Assert(Resources.Load<Texture2D>("UI/Strategy/plates") != null &&
                Resources.Load<Texture2D>("UI/Strategy/props") != null &&
                Resources.Load<Texture2D>("UI/Strategy/veil") != null, "All strategy art loads in Player");
            var seeds = new Dictionary<string, int>();
            for (int seed = controller.seed; seed < controller.seed + 300 && seeds.Count < 3; seed++)
            {
                var run = CompleteSeededOpeningDelivery(seed);
                while (run.HasBlockingChoice) run.Choose(0);
                for (int i = 0; i < 2; i++)
                {
                    int defenders = run.CurrentNode.DefenderCount;
                    run.SettleBattle(false, 0, defenders, defenders, false);
                    if (!run.AdvanceNode()) break;
                }
                string tier = run.Choices.FirstOrDefault()?.Strategy?.Tier;
                if (!string.IsNullOrEmpty(tier) && !seeds.ContainsKey(tier)) seeds.Add(tier, seed);
            }
            Assert(seeds.Count == 3, "Found seeds for all three strategy tiers using the production draw");

            foreach (string tier in new[] { "银", "金", "彩" })
            {
                controller.seed = seeds[tier] - 1;
                controller.ResetDemo();
                yield return null;
                yield return WaitForOpeningDelivery();
                Assert(controller.Campaign.Choices.Count == 3 &&
                    controller.Campaign.Choices.All(option => option.Strategy?.Tier == ""), "Opening choices are ungraded for " + tier);
                if (tier == "银") yield return Frame("strategy-opening");
                while (controller.Campaign.HasBlockingChoice)
                    Assert(controller.ChooseCampaignOption(0).Success, "Resolve opening choice for " + tier);
                DeployOpeningRecruits();

                for (int i = 0; i < 2; i++)
                {
                    yield return ReadyArt();
                    controller.StartBattle();
                    Assert(controller.Model.Phase == FormationPhase.Fighting, "Start actual battle " + controller.Campaign.CurrentNode.Id + " for " + tier);
                    int ticks = 0;
                    while (controller.Model.Phase == FormationPhase.Fighting || controller.Model.Phase == FormationPhase.Exiting)
                    {
                        if (++ticks > 1600) throw new InvalidOperationException("Strategy verification battle timed out.");
                        for (int tick = 0; tick < 12; tick++) controller.Model.Advance(.25f);
                        yield return null;
                    }
                    result.battles++;
                    yield return null;
                    Assert(controller.ContinueCampaign(), "Advance after an actual battle for " + tier);
                    yield return null;
                }
                result.node = controller.Campaign.CurrentNode.Id;
                var choices = controller.Campaign.Choices;
                Assert(result.node == "1-3" && choices.Count == 3 && choices.All(option => option.Strategy?.Tier == tier),
                    "Production 1-3 opens three " + tier + " choices");
                yield return CaptureInterfaceClip("strategy-reveal-" + tier, 36);
                yield return new WaitForSecondsRealtime(.7f);
                yield return Frame("strategy-" + tier);
                if (tier == "银" && Array.IndexOf(Environment.GetCommandLineArgs(), "-formationStrategyMouse") >= 0)
                {
                    Debug.Log("FORMATION_STRATEGY_MOUSE_READY: choose one silver strategy in the live window");
                    double deadline = Time.realtimeSinceStartupAsDouble + 75;
                    while (controller.Campaign.AugmentIds.Count == 0 && Time.realtimeSinceStartupAsDouble < deadline)
                        yield return null;
                    Assert(controller.Campaign.AugmentIds.Count == 1 &&
                        choices.Any(option => controller.Campaign.HasAugment(option.Id)), "Live mouse confirmation applies one visible silver choice");
                    yield return Frame("strategy-mouse-confirmed");
                    continue;
                }
                Assert(controller.Campaign.RerollChoice() && !controller.Campaign.CanRerollChoice &&
                    controller.Campaign.Choices.All(option => option.Strategy?.Tier == tier), "One reroll preserves the round's tier " + tier);
                string chosen = controller.Campaign.Choices[0].Id;
                Assert(controller.ChooseCampaignOption(0).Success && controller.Campaign.HasAugment(chosen),
                    "Confirmation applies the real choice " + chosen);
            }
        }

        IEnumerator VerifyStrategyHistory()
        {
            var anchor = FormationBattleController.FormationStrategyHistoryAnchor.center;
            var firstCard = new Vector2(282, 398);
            var reroll = new Vector2(548, 628);
            var confirm = new Vector2(725, 628);
            var confirmed = new List<string>();
            var run = controller.Campaign;
            Assert(run.CurrentNode.Id == "1-1" && run.PreplanId == null && run.AugmentIds.Count == 0,
                "Fresh challenge has no confirmed strategy");
            yield return WaitForOpeningDelivery();
            yield return ResolveChoices();
            string preplan = run.PreplanId;
            Assert(!string.IsNullOrEmpty(preplan), "Opening choice commits a real preplan");
            DeployOpeningRecruits();
            yield return ReadyArt();
            yield return ShowStrategyHistory("02-opening-preplan-history", confirmed, preplan);
            yield return ClickStrategyUi(FormationBattleController.FormationStartRect.center);
            yield return WaitForStrategyUi(() => !controller.FormationStrategyHistoryOpen, "Outside click closes strategy history");
            Assert(controller.Model.Phase == FormationPhase.Preparation && controller.Model.Clock == 0,
                "The closing click cannot start the battle through strategy history");

            for (int i = 0; i < 2; i++)
            {
                yield return ReadyArt();
                controller.StartBattle();
                Assert(controller.Model.Phase == FormationPhase.Fighting, "Start real opening battle " + run.CurrentNode.Id);
                if (i == 0)
                {
                    controller.Model.Paused = true;
                    yield return ClickStrategyUi(anchor);
                    Assert(!controller.FormationStrategyHistoryOpen, "Paused combat cannot open preparation strategy history");
                    controller.Model.Paused = false;
                }
                int ticks = 0;
                while (controller.Model.Phase == FormationPhase.Fighting || controller.Model.Phase == FormationPhase.Exiting)
                {
                    if (++ticks > 1600) throw new InvalidOperationException("Strategy timeline battle timed out.");
                    for (int tick = 0; tick < 12; tick++) controller.Model.Advance(.25f);
                    yield return null;
                }
                result.battles++;
                yield return WaitForStrategyUi(() => run.NodeSettled, "Opening battle settles through production controller");
                Assert(controller.ContinueCampaign(), "Advance from actual opening battle " + (i + 1));
                if (i == 0) yield return null;
            }

            foreach (string node in new[] { "1-3", "2-2", "3-2" })
            {
                result.node = run.CurrentNode.Id;
                var options = run.Choices.ToArray();
                Assert(result.node == node && controller.Model.Phase == FormationPhase.Preparation && run.HasBlockingChoice &&
                    options.Length == 3 && options.All(option => !string.IsNullOrEmpty(option.Strategy?.Tier)) &&
                    options.Select(option => option.Strategy.Tier).Distinct().Count() == 1,
                    node + " automatically presents three same-tier strategies before battle");
                controller.StartBattle();
                Assert(controller.Model.Phase == FormationPhase.Preparation, node + " must confirm before battle");
                yield return CaptureInterfaceClip(node + "-reveal", 27);
                yield return new WaitForSecondsRealtime(.98f);
                yield return Frame(node + "-choices");
                if (node == "1-3")
                {
                    yield return MoveStrategyUiPointer(firstCard);
                    yield return Frame(node + "-hover");
                    yield return ClickStrategyUi(firstCard);
                    yield return Frame(node + "-preselected");
                    Assert(run.AugmentIds.Count == 0, "Preselection does not commit a strategy");
                    var previousDraw = run.Choices;
                    yield return ClickStrategyUi(reroll);
                    yield return CaptureInterfaceClip(node + "-reroll", 27);
                    yield return WaitForStrategyUi(() => !run.CanRerollChoice, "Live mouse uses one free whole-group reroll");
                    Assert(!ReferenceEquals(previousDraw, run.Choices) &&
                        run.Choices.All(option => option.Strategy.Tier == options[0].Strategy.Tier),
                        "Reroll redraws all alternatives within the same tier");
                    yield return new WaitForSecondsRealtime(.90f);
                    options = run.Choices.ToArray();
                    yield return ClickStrategyUi(reroll);
                    Assert(run.Choices.Select(option => option.Id).SequenceEqual(options.Select(option => option.Id)),
                        "Disabled reroll preserves all three replacements");
                    yield return ClickStrategyUi(confirm);
                    Assert(run.AugmentIds.Count == 0, "Reroll clears preselection and disables confirmation");
                }

                int card = node == "2-2" ? 1 : 0;
                if (node == "2-2") yield return PressStrategyUiKey(0x32);
                else yield return ClickStrategyUi(firstCard);
                Assert(run.AugmentIds.SequenceEqual(confirmed), node + " picked state leaves already committed IDs intact");
                if (node == "2-2") yield return PressStrategyUiKey(0x0d);
                else yield return ClickStrategyUi(confirm);
                yield return CaptureInterfaceClip(node + "-confirm", 22);
                confirmed.Add(options[card].Id);
                yield return WaitForStrategyUi(() => run.AugmentIds.SequenceEqual(confirmed), node + " live confirmation adds the picked strategy once");
                yield return ResolveChoices();
                yield return ShowStrategyHistory(node + "-history", confirmed, preplan);
                yield return ClickStrategyUi(anchor);
                yield return WaitForStrategyUi(() => !controller.FormationStrategyHistoryOpen, node + " closes by clicking the strategy strip again");
                if (node == "3-2") break;

                string nextCheckpoint = node == "1-3" ? "2-2" : "3-2";
                while (run.CurrentNode.Id != nextCheckpoint)
                {
                    if (run.CurrentNode.Kind != "补给")
                        Assert(run.SettleBattle(true, 1, run.CurrentNode.DefenderCount, 0, false) != null,
                            "Apply fixture win through settlement at " + run.CurrentNode.Id);
                    else yield return ResolveChoices();
                    Assert(controller.ContinueCampaign(), "Advance through the normal node transition to " + run.CurrentNode.Id);
                    if (run.CurrentNode.Id == "1-4")
                    {
                        yield return ShowStrategyHistory("1-4-persisted-history", confirmed, preplan);
                        yield return PressStrategyUiKey(0x1b);
                        yield return WaitForStrategyUi(() => !controller.FormationStrategyHistoryOpen, "Close persisted strategy history");
                    }
                }
            }

            controller.ResetDemo();
            Assert(!ReferenceEquals(run, controller.Campaign) && !controller.FormationStrategyHistoryOpen &&
                controller.Campaign.CurrentNode.Id == "1-1" && controller.Campaign.PreplanId == null && controller.Campaign.AugmentIds.Count == 0,
                "New challenge resets the strategy overlay and option history");
            yield return WaitForOpeningDelivery();
            Assert(controller.Campaign.PreplanId == null && controller.Campaign.AugmentIds.Count == 0,
                "The automatic opening and new unconfirmed preplan do not inherit prior strategies");
            yield return Frame("17-fresh-challenge-preplan");
        }

        IEnumerator ShowStrategyHistory(string screenshot, IReadOnlyList<string> augments, string preplan)
        {
            yield return ClickStrategyUi(FormationBattleController.FormationStrategyHistoryAnchor.center);
            yield return WaitForStrategyUi(() => controller.FormationStrategyHistoryOpen, "Live mouse opens " + screenshot);
            Assert(controller.Campaign.PreplanId == preplan && controller.Campaign.AugmentIds.SequenceEqual(augments),
                screenshot + " preserves the committed list: " +
                string.Join(", ", new[] { preplan }.Concat(augments).Where(id => id != null).Select(id => FormationCampaignRules.FindRecord(id).Name)));
            yield return Frame(screenshot);
        }

        IEnumerator WaitForStrategyUi(Func<bool> reached, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 4;
            while (!reached() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert(reached(), label);
        }

        [StructLayout(LayoutKind.Sequential)] struct StrategyCursorPoint { public int X, Y; }
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window, ref StrategyCursorPoint point);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
        [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);

        static IntPtr StrategyUiWindow()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer)
                throw new InvalidOperationException("Live strategy input requires the Windows Player.");
            IntPtr window = GetForegroundWindow();
            GetWindowThreadProcessId(window, out uint processId);
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return processId == (uint)process.Id ? window : IntPtr.Zero;
        }

        static IEnumerator WaitForStrategyUiWindow()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 4;
            while (StrategyUiWindow() == IntPtr.Zero)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    IntPtr foreground = GetForegroundWindow();
                    GetWindowThreadProcessId(foreground, out uint owner);
                    using var process = System.Diagnostics.Process.GetCurrentProcess();
                    throw new InvalidOperationException($"Live input needs the foreground Player. Foreground HWND {foreground}, owner PID {owner}; Player PID {process.Id}, focused={Application.isFocused}.");
                }
                yield return null;
            }
        }

        static IEnumerator MoveStrategyUiPointer(Vector2 logicalPoint)
        {
            yield return WaitForStrategyUiWindow();
            var window = StrategyUiWindow();
            var viewport = TacticalViewport.Current;
            Vector2 client = viewport.Offset + logicalPoint * viewport.Scale;
            var point = new StrategyCursorPoint { X = Mathf.RoundToInt(client.x), Y = Mathf.RoundToInt(client.y) };
            if (!ClientToScreen(window, ref point) || !SetCursorPos(point.X, point.Y))
                throw new InvalidOperationException("Could not position the cursor in the active Player.");
            yield return new WaitForSecondsRealtime(.06f);
        }

        static IEnumerator ClickStrategyUi(Vector2 point)
        {
            yield return MoveStrategyUiPointer(point);
            yield return WaitForStrategyUiWindow();
            mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            yield return new WaitForSecondsRealtime(.055f);
            mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            yield return null;
        }

        static IEnumerator PressStrategyUiKey(byte key)
        {
            yield return WaitForStrategyUiWindow();
            keybd_event(key, 0, 0, UIntPtr.Zero);
            yield return new WaitForSecondsRealtime(.055f);
            keybd_event(key, 0, 0x0002, UIntPtr.Zero);
            yield return null;
        }

        IEnumerator WaitForOpeningDelivery(string clip = null)
        {
            var run = controller.Campaign;
            Assert(run.OpeningRewardsPending && !run.HasBlockingChoice && !controller.Session.CanShop &&
                controller.Shop.Bench.Count == 0 && run.Inventory.Count == 0 && run.PendingBalls.Count == 0,
                "Automatic opening starts with reserved stock, no landed grants, no ground balls and no preplan");
            var recruits = run.OpeningRecruits.ToArray();
            string component = run.OpeningComponent;
            Assert(recruits.Length == 3 && recruits.Select(recruit => recruit.CardId).Distinct().Count() == 3 &&
                recruits.All(recruit => recruit.Star == 1 && controller.Shop.Definition(recruit.CardId).Cost is >= 1 and <= 3 &&
                    controller.Shop.ReservedRewards(recruit.CardId) == 1) &&
                FormationCampaignRules.AllComponents.Any(item => item.Id == component),
                "Three distinct 1–3 cost one-star starters occupy their original stock before delivery");
            if (clip != null) yield return CaptureInterfaceClip(clip, 54);
            yield return WaitForStrategyUi(() => !run.OpeningRewardsPending, "Normal updates complete opening delivery with no reward click");
            Assert(controller.Shop.Bench.Select(piece => piece.Id).SequenceEqual(recruits.Select(recruit => recruit.CardId)) &&
                controller.Shop.Bench.All(piece => piece.Star == 1 && controller.Session.Seats.For(piece.Instance) is { Lineup: false } &&
                    controller.Shop.ReservedRewards(piece.Id) == 0) && controller.Model.EnabledCount == 0 &&
                run.Inventory.Count == 1 && run.Inventory[0].Key == component && run.PendingBalls.Count == 0 && run.BallRewards.Count == 0,
                "Landing places the three recruits in reserve and the component in inventory while the board stays empty");
            Assert(run.Choices.Count == 3 && run.Choices.All(choice => choice.Strategy?.Tier == "") && !controller.Session.CanShop,
                "Landing immediately opens the original three ungraded preplans and keeps trading locked");
            controller.Shop.Validate();
        }

        IEnumerator VerifyOpeningDelivery()
        {
            var runs = new List<FormationCampaignRun>();
            const byte Enter = 0x0d, Escape = 0x1b;
            var confirmButton = new Vector2(725, 628);
            for (int round = 0; round < 2; round++)
            {
                var run = controller.Campaign;
                runs.Add(run);
                string prefix = (round + 1).ToString("D2");
                yield return WaitForOpeningDelivery(prefix + "-opening-delivery-and-preplan");
                yield return new WaitForSecondsRealtime(.97f);
                yield return Frame(prefix + "-preplan-choices");
                int pick = round == 0 ? 1 : 0;
                string id = run.Choices[pick].Id;
                if (round == 0) yield return PressStrategyUiKey(0x32);
                else yield return ClickStrategyUi(new Vector2(282, 398));
                Assert(run.PreplanId == null, "Opening preselection does not apply its effect");
                if (round == 0) yield return PressStrategyUiKey(Enter);
                else yield return ClickStrategyUi(confirmButton);
                yield return CaptureInterfaceClip(prefix + "-preplan-confirm", 20);
                yield return WaitForStrategyUi(() => run.PreplanId == id, "The live " + (round == 0 ? "keyboard" : "mouse") + " confirms the chosen preplan");
                yield return ResolveChoices();
                Assert(controller.Model.EnabledCount == 0 && controller.Session.CanShop,
                    "The preparation HUD returns with no automatic deployment");
                yield return Frame(prefix + "-opening-ready");
                yield return ShowStrategyHistory(prefix + "-opening-strategy-history", Array.Empty<string>(), id);
                yield return PressStrategyUiKey(Escape);
                yield return WaitForStrategyUi(() => !controller.FormationStrategyHistoryOpen, "Escape closes the preplan history");
                if (round == 0)
                {
                    yield return ClickStrategyUi(FormationBattleController.FormationCoinRect.center);
                    yield return WaitForStrategyUi(() => controller.MarketVisible && !controller.ShopPanelTransitioning,
                        "The ordinary coin HUD opens the recruitment drawer");
                    yield return ClickStrategyUi(FormationBattleController.FormationCoinRect.center);
                    yield return WaitForStrategyUi(() => !controller.MarketVisible && !controller.ShopPanelTransitioning,
                        "The ordinary coin HUD closes the recruitment drawer");
                    yield return ClickStrategyUi(new Vector2(1239, 38));
                    yield return ClickStrategyUi(new Vector2(ApprovedUi.Menu.center.x, 179));
                    yield return WaitForStrategyUi(() => !ReferenceEquals(run, controller.Campaign), "The system menu starts a fresh automatic opening");
                }
            }
            Assert(runs[0].PreplanId != null && controller.Campaign.AugmentIds.Count == 0,
                "The second opening keeps only strategies confirmed in the new challenge");
        }

        static FormationCampaignRun CompleteSeededOpeningDelivery(int seed)
        {
            var session = new FormationShopSession(seed, fullCampaign: true);
            if (!session.CompleteOpeningDelivery())
                throw new InvalidOperationException("Could not complete the seeded opening delivery.");
            return session.Run;
        }

        IEnumerator ClaimGroundRewards(bool capture, string clipPrefix = "reward-flight")
        {
            bool captured = false;
            int remaining = controller.Campaign.PendingBalls.Count;
            while (remaining > 0)
            {
                var drop = controller.Campaign.PendingBalls.First();
                double deadline = Time.realtimeSinceStartupAsDouble + 6;
                Vector2 position = default;
                while (!controller.TryGroundRewardPoint(drop.Id,out position) && Time.realtimeSinceStartupAsDouble < deadline)
                    yield return null;
                yield return new WaitForSecondsRealtime(.68f);
                bool projected = controller.TryGroundRewardPoint(drop.Id,out position);
                float frontRowY = FormationBattleController.CellScreen(Vector2.zero).y;
                Assert(projected && position.x > 285 && position.x < 735 &&
                    position.y > frontRowY && position.y < FormationBattleController.FormationBenchRect.yMin,
                    "Clickable 3D " + drop.Color + " ball sits between the deployment board and the reserve bench");
                int gold = controller.Shop.Gold, equipment = controller.Campaign.Inventory.Count;
                Assert(controller.ProcessFormationRewardPointer(EventType.MouseDown,position) &&
                    controller.ProcessFormationRewardPointer(EventType.MouseUp,position),"Pointer opens one real ground reward");
                Assert(controller.Shop.Gold == gold && controller.Campaign.Inventory.Count == equipment,
                    "Click does not grant gold or equipment before flight");
                bool recruits = controller.ActiveGroundRewardHasRecruits;
                bool twoStar = controller.ActiveGroundRewardHasTwoStar;
                if (capture && (clipPrefix == "rarity" ? drop.Color == "金" || drop.Color == "彩" : !captured || recruits))
                {
                    string clip = clipPrefix == "rarity" ? clipPrefix + (drop.Color == "金" ? "-gold" : "-prism") :
                        recruits ? (twoStar ? "recruit-two-star-" : "recruit-pack-")+drop.Id : clipPrefix;
                    captured = true;
                    yield return CaptureInterfaceClip(clip, 32);
                }
                deadline = Time.realtimeSinceStartupAsDouble + 6;
                while (controller.Campaign.PendingBalls.Any(ball => ball.Id == drop.Id) && Time.realtimeSinceStartupAsDouble < deadline)
                    yield return null;
                Assert(controller.Campaign.PendingBalls.Count == --remaining,"Arrival claims exactly one ball");
                yield return ResolveChoices();
            }
        }

        IEnumerator VerifyBattleRewardDrop()
        {
            result.scope += " The 1-1 defender fall and cosmetic reward drop are captured at normal speed.";
            var model = controller.Model;
            var run = controller.Campaign;
            int queued = run.PendingBalls.Count, claimed = run.BallRewards.Count;
            Assert(model.Speed == 1 && controller.BattleRewardDropsShown == 0,
                "Normal-speed reward battle begins without staged drops");
            double deadline = Time.realtimeSinceStartupAsDouble + 100;
            while (model.Defenders.All(guard => guard.LastDeathAt < 0) &&
                model.Phase == FormationPhase.Fighting && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert(model.Defenders.Any(guard => !guard.Alive && guard.LastDeathAt >= 0),
                "A defender naturally falls on the authored 1-1 board");
            Assert(controller.ActiveBattleRewardDrops == 0, "Reward drop waits for the defender's fall");

            string target = Path.Combine(directory, "05-guard-reward-drop");
            Directory.CreateDirectory(target);
            double start = Time.realtimeSinceStartupAsDouble;
            double openedAt = -1;
            int landingFrame = -1, clearedFrame = -1;
            for (int frame = 0; frame < 54; frame++)
            {
                while (Time.realtimeSinceStartupAsDouble < start + frame / 24d) yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(target, frame.ToString("D3") + ".png"));
                bool active = controller.ActiveBattleRewardDrops > 0;
                if (active && openedAt < 0) openedAt = model.Clock;
                if (active && landingFrame < 0 && model.Clock - openedAt >= .38) landingFrame = frame;
                if (landingFrame >= 0 && !active && clearedFrame < 0 && controller.CombatViewActive)
                    clearedFrame = frame;
            }
            yield return null;
            Assert(openedAt >= 0 && landingFrame >= 0 && clearedFrame >= 0,
                "Drop lands and clears while the battle camera is still active");
            File.Copy(Path.Combine(target, landingFrame.ToString("D3") + ".png"),
                Path.Combine(directory, "05-guard-reward-landing.png"), true);
            File.Copy(Path.Combine(target, clearedFrame.ToString("D3") + ".png"),
                Path.Combine(directory, "05-guard-reward-cleared.png"), true);
            Assert(controller.BattleRewardDropsShown <= model.Defenders.Count(guard => guard.LastDeathAt >= 0),
                "The visible drop count does not exceed the fallen defender count");
            Assert(run.PendingBalls.Count == queued && run.BallRewards.Count == claimed && !run.NodeSettled,
                "Fallen-guard presentation neither queues nor claims a reward during combat");
        }

        IEnumerator VerifyConcurrentGroundRewards()
        {
            yield return new WaitForSecondsRealtime(.45f);
            int untouched = 0;
            Vector2 untouchedPoint = default;
            if (RewardDropEvidenceRequested)
            {
                yield return new WaitForSecondsRealtime(.25f);
                yield return Frame("06-reward-platform-scatter");
                untouched = controller.Campaign.PendingBalls[2].Id;
                Assert(controller.TryGroundRewardPoint(untouched, out untouchedPoint), "A third reward has a selectable position before other pickups");
            }
            var drops=controller.Campaign.PendingBalls.Take(2).ToArray();
            Assert(drops.Length==2,"Two real carried rewards are available for independent opening");
            int count=controller.Campaign.PendingBalls.Count, gold=controller.Shop.Gold, items=controller.Campaign.Inventory.Count;
            foreach(var drop in drops)
            {
                Assert(controller.TryGroundRewardPoint(drop.Id,out var point)&&
                    controller.ProcessFormationRewardPointer(EventType.MouseDown,point)&&
                    controller.ProcessFormationRewardPointer(EventType.MouseUp,point)&&
                    !controller.TryGroundRewardPoint(drop.Id,out _),"Distinct reward starts without waiting for the other animation");
            }
            Assert(controller.Campaign.PendingBalls.Count==count&&controller.Shop.Gold==gold&&controller.Campaign.Inventory.Count==items,
                "Both reward flights start without an early payout");
            yield return CaptureInterfaceClip("06-concurrent-reward-flights",26);
            double until=Time.realtimeSinceStartupAsDouble+5;
            while(controller.Campaign.PendingBalls.Any(ball=>drops.Any(drop=>drop.Id==ball.Id))&&
                Time.realtimeSinceStartupAsDouble<until)yield return null;
            Assert(controller.Campaign.PendingBalls.Count==count-2,"Exactly two independently started drops have been delivered");
            if (untouched != 0)
                Assert(controller.TryGroundRewardPoint(untouched, out var remainingPoint) &&
                    Vector2.Distance(remainingPoint, untouchedPoint) < 4,
                    "The untouched reward keeps its position as its neighbors are picked up");
        }

        IEnumerator VerifyItemArt()
        {
            foreach (var trait in FormationCampaignRun.BadgeTraits)
            {
                string name = FormationCampaignRules.FindRecord(trait).Name;
                string id = SynergyBadgePresentation.IconId(name);
                var art = FormationBattleController.SupplyItemArt("徽章:" + trait);
                Assert(art != null && art == Resources.Load<Texture2D>("UI/FormationSupplies/Badges/" + id) &&
                    art != SynergyBadgePresentation.IconFor(name) && art.width == 256 && art.height == 256,
                    "Badge equipment loads its own approved texture: " + name);
            }
            Assert(FormationBattleController.SupplyItemArt("蓝宝石") is { name: "blue-gem", width: 256, height: 256 } &&
                FormationBattleController.SupplyItemArt("赤宝石") is { name: "red-gem", width: 256, height: 256 },
                "Red and blue gems load their approved textures");

            int badgeSeed = Enumerable.Range(controller.seed, 100).First(seed =>
            {
                var run = CompleteSeededOpeningDelivery(seed);
                return run.Choices.Any(option => option.Id == "P13");
            });
            controller.seed = badgeSeed - 1;
            controller.ResetDemo();
            yield return null;
            yield return WaitForOpeningDelivery();
            int preplan = Array.FindIndex(controller.Campaign.Choices.ToArray(), option => option.Id == "P13");
            Assert(controller.ChooseCampaignOption(preplan).Success &&
                controller.Campaign.Choices.All(option => FormationCampaignRun.BadgeTrait(option.Id) != null),
                "External Recruitment opens its real emblem choice");
            yield return Frame("01-badge-choice");
            Assert(controller.ChooseCampaignOption(0).Success, "Claim a badge from the real choice");
            DeployOpeningRecruits();
            yield return ReadyArt();

            var red = controller.Campaign.AddItem("赤宝石", false);
            var blue = controller.Campaign.AddItem("蓝宝石", false);
            var component = controller.Campaign.AddItem("A", false);
            Assert(controller.TryEquipmentTilePoint(blue.Instance, out var source), "Blue gem appears in the equipment rail");
            Assert(controller.TryEquipmentTilePoint(component.Instance, out var target) &&
                controller.TryEquipmentTilePoint(red.Instance, out _), "Red gem and the recipe component appear in the equipment rail");
            yield return Frame("02-gems-and-badge-in-rail");

            Assert(controller.ProcessFormationEquipmentPointer(EventType.MouseDown, source) &&
                controller.ProcessFormationEquipmentPointer(EventType.MouseDrag, target), "Drag blue gem onto Steel Blade");
            yield return Frame("03-badge-recipe-hover");
            Assert(controller.ProcessFormationEquipmentPointer(EventType.MouseUp, target) &&
                controller.EquipmentCombinationPending, "Badge recipe opens real confirmation");
            yield return Frame("04-badge-recipe-confirmation");
            yield return CaptureInterfaceClip("badge-recipe-confirmation", 12);
            Assert(controller.ConfirmFormationEquipmentCombination(), "Confirm the blue gem recipe");
            var badge = controller.Campaign.Inventory.Single(item => item.Key == "徽章:连击:A");
            Assert(FormationBattleController.SupplyItemArt(badge.Key) == FormationBattleController.SupplyItemArt("徽章:连击"),
                "Crafted and reward forms share the same Combo badge texture");
            var wearer = controller.Shop.Bench.First(piece => controller.Session.Seats.InLineup(piece.Instance) &&
                !controller.Campaign.HasTrait(piece, "连击"));
            Assert(controller.TryEquipmentTilePoint(badge.Instance, out source), "Crafted badge appears in inventory");
            target = controller.FormationPieceRect(wearer.Instance).center;
            Assert(controller.ProcessFormationEquipmentPointer(EventType.MouseDown, source) &&
                controller.ProcessFormationEquipmentPointer(EventType.MouseDrag, target) &&
                controller.ProcessFormationEquipmentPointer(EventType.MouseUp, target) && badge.Owner == wearer.Instance,
                "Equip the crafted badge by dragging it onto a real deployed unit");
            yield return Frame("05-badge-equipped");

            foreach (string trait in FormationCampaignRun.BadgeTraits)
            {
                string key = "徽章:" + trait;
                if (!controller.Campaign.Inventory.Any(item => item.Key == key))
                    controller.Campaign.AddItem(key, false);
            }
            var inventory = controller.Campaign.Inventory;
            Assert(FormationCampaignRun.BadgeTraits.All(trait => inventory.Any(item => item.Key == "徽章:" + trait)),
                "All approved badge variants are present in the same equipment rail");
            const int pageSize = 7;
            for (int first = 0; first < inventory.Count; first += pageSize)
            {
                int last = Math.Min(first + pageSize, inventory.Count) - 1;
                controller.RevealEquipmentSlot(last);
                Assert(controller.TryEquipmentTilePoint(inventory[first].Instance, out _) &&
                    controller.TryEquipmentTilePoint(inventory[last].Instance, out _), "Badge rail sample page is visible");
                yield return Frame("06-badge-rail-page-" + (first / pageSize + 1));
            }
        }

        IEnumerator VerifyEquipmentDragCombination()
        {
            var items=controller.Campaign.Inventory;
            var pair=(from left in items from right in items
                where left.Instance<right.Instance&&controller.Campaign.PreviewCombination(left.Instance,right.Instance,out _).Success
                select (left,right)).FirstOrDefault();
            Assert(pair.left!=null&&pair.right!=null,"Real combat rewards supplied a compatible equipment pair");
            int first=pair.left.Instance, second=pair.right.Instance;
            Assert(controller.Campaign.PreviewCombination(first,second,out string resultKey).Success&&
                controller.TryEquipmentTilePoint(first,out _)&&controller.TryEquipmentTilePoint(second,out _),
                "Both owned components expose a reachable inventory rail tile");
            controller.TryEquipmentTilePoint(first,out var source);
            controller.TryEquipmentTilePoint(second,out var target);
            Assert(controller.ProcessFormationEquipmentPointer(EventType.MouseDown,source)&&
                controller.ProcessFormationEquipmentPointer(EventType.MouseDrag,target)&&
                !controller.EquipmentCombinationPending&&controller.Campaign.Inventory.Count==items.Count,
                "Hovering a valid recipe previews it without consuming any materials");
            yield return Frame("08-equipment-drag-preview");
            Assert(controller.ProcessFormationEquipmentPointer(EventType.MouseUp,target)&&
                controller.EquipmentCombinationPending,"Dropping a recipe waits for explicit confirmation");
            yield return Frame("08-equipment-confirmation");
            Assert(controller.CancelFormationEquipmentInteraction()&&
                controller.Campaign.Inventory.Any(item=>item.Instance==first)&&controller.Campaign.Inventory.Any(item=>item.Instance==second),
                "Cancel preserves both materials");
            controller.ProcessFormationEquipmentPointer(EventType.MouseDown,source);
            controller.ProcessFormationEquipmentPointer(EventType.MouseDrag,target);
            controller.ProcessFormationEquipmentPointer(EventType.MouseUp,target);
            Assert(controller.ConfirmFormationEquipmentCombination()&&
                controller.Campaign.Inventory.Count==items.Count-1&&controller.Campaign.Inventory.Any(item=>item.Key==resultKey),
                "Confirm makes the exact recipe and consumes just the two selected materials");
            yield return Frame("08-equipment-result");
        }

        IEnumerator VerifyStarterTraitActivation()
        {
            var view = controller.FormationTraitRows.FirstOrDefault(row => row.Active);
            Assert(view != null, "Fixed seed has a naturally active starter trait");
            var actors = controller.Model.Units.Where(unit => unit.Enabled).ToArray();
            var member = actors.First(unit => controller.Shop.Bench.Any(piece => piece.Instance == unit.Instance &&
                controller.Campaign.HasTrait(piece, view.Id)));
            int reserve = Enumerable.Range(0, 10).First(index => controller.Session.Seats.At(false, index) == 0);
            Assert(controller.MovePiece(member.Instance, false, reserve).Success, "Move a real trait member onto the reserve bench");
            yield return null;
            Assert(controller.Campaign.TraitCount(view.Id) < view.ActiveLevel, "Reserve member drops the actual trait tier");
            var from = controller.FormationPieceRect(member.Instance).center;
            var to = FormationBattleController.CellScreen(member.Spawn);
            controller.ProcessPointer(EventType.MouseDown, from);
            controller.ProcessPointer(EventType.MouseDrag, to);
            controller.ProcessPointer(EventType.MouseUp, to);
            yield return null;
            var pulse = controller.ActiveFormationTraitPulse?.Activations.FirstOrDefault();
            Assert(pulse != null && pulse.Name == view.Name && pulse.Tier == view.ActiveLevel &&
                pulse.Members.Count == controller.Campaign.TraitCount(view.Id) &&
                pulse.Members.All(id => controller.Session.Seats.Lineup(controller.Shop.Bench)
                    .Any(piece => piece.Id == id && controller.Campaign.HasTrait(piece, view.Id))),
                "Dragging the member back activates the previous tier and only highlights its true deployed members");
            yield return CaptureInterfaceClip("trait-activation", 46);
        }

        IEnumerator VerifySynergyTrials()
        {
            result.scope = "Authored FormationBattle Windows Player. All current synergy presets, normal controller selection and combat, timed simulation, outcome and retry. Battle observations are sampled without changing actor or defense stats.";
            var rules = FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles).Select(rule => rule.Id);
            Assert(FormationSynergyTrial.All.Select(trial => trial.Trait.Id).OrderBy(id => id).SequenceEqual(rules.OrderBy(id => id)),
                "Every current synergy has one preset");
            foreach (var trial in FormationSynergyTrial.All)
            {
                string id = trial.Trait.Id;
                string imagePrefix = $"{result.battles + 1:00}-{id}";
                controller.SelectSynergyTrial(id);
                var run = controller.Campaign;
                var model = controller.Model;
                int tier = trial.Trait.Levels.Max();
                result.node = id;
                Assert(run.SynergyTrial == trial && controller.Shop.UnlimitedGold && !run.HasBlockingChoice,
                    id + " enters a fresh ready-to-play trial");
                Assert(model.EnabledCount == trial.Roster.Count && model.Defenders.Count == run.CurrentNode.DefenderCount &&
                    model.ActiveSynergies[id] == tier, id + " deploys the actual top tier and real guards");
                yield return ReadyArt();
                yield return Frame(imagePrefix + "-preparation");
                controller.StartBattle();
                Assert(model.Phase == FormationPhase.Fighting && model.ActiveSynergies[id] == tier, id + " starts combat");
                var stage = new SynergyStage { id = id, node = run.CurrentNode.Id, tier = tier,
                    golem = model.Summons.FirstOrDefault(unit => unit.ArtId == "GOLEM")?.Name };
                while (model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting)
                {
                    if (model.Clock > model.LimitSeconds + .01) throw new InvalidOperationException(id + " exceeded the battle clock");
                    model.Paused = false;
                    model.Advance(1f);
                    if (stage.firstContact == 0 && model.EncounterProgress > 0)
                    {
                        stage.firstContact = model.Clock;
                        yield return Frame(imagePrefix + "-contact");
                    }
                    yield return null;
                }
                Assert(stage.firstContact > 0 && model.Clock > 0 && model.Clock <= model.LimitSeconds + .01 &&
                    (model.Phase == FormationPhase.Won || model.Phase == FormationPhase.Lost), id + " engages and reaches a timed result");
                stage.battleSeconds = model.Clock;
                stage.outcome = model.Phase.ToString();
                stage.remaining = model.RemainingDefenders;
                stage.guardHealthLoss = model.EncounterProgress;
                stage.skillCasts = model.SkillCasts.Count;
                stage.events = model.BattleEvents.Take(12).ToArray();
                stage.settlement = run.LastTrialProgress;
                result.synergyStages.Add(stage);
                result.battles++;
                yield return Frame(imagePrefix + "-result");
                if (id != "整合" && id != "泥岩") continue;

                Assert(!string.IsNullOrEmpty(stage.settlement), id + " displays actual settlement progress");
                int progress = id == "整合" ? run.Hope : run.MudrockXP;
                controller.ResetDemo();
                Assert(controller.Campaign == run && controller.Model != model && controller.Model.Phase == FormationPhase.Preparation &&
                    progress == (id == "整合" ? run.Hope : run.MudrockXP), id + " retries the same trial and preserves progression");
            }
            controller.SelectPracticeBattle(0);
            Assert(controller.Campaign.IsPractice && controller.Campaign.SynergyTrial == null && controller.Campaign.HasBlockingChoice,
                "Difficulty practice opens independently with its normal starting choices");
        }

        IEnumerator VerifyDefenses()
        {
            bool probe = DefenseSkillProbe;
            result.scope = probe
                ? "Targeted live skill diagnostics in the actual practice nodes. One real healer is placed on staged walkable cells and given diagnostic health to survive. Mon3tr is pre-wounded for real healing actions. All casts, contacts, damage and timing run in the battle model. Samples terminate on observed skills, not victory."
                : "Normal FormationBattle Windows Player, production practice-node selection and legal preset attacker cards, ordinary simulation and battle outcomes, scout and skill frames. Presets are test fixtures, not natural shop progression.";
            var args = Environment.GetCommandLineArgs();
            int nodeFlag = Array.IndexOf(args, "-formationDefenseNodes");
            string nodeList = probe ? "2-4,2-5,3-2,3-4" : nodeFlag >= 0 && nodeFlag + 1 < args.Length ? args[nodeFlag + 1] : "1-6,2-4,2-5,3-4,3-7";
            foreach (string id in nodeList.Split(',').Select(part => part.Trim()).Distinct())
            {
                int index = Array.FindIndex(FormationCampaignRules.Nodes, node => node.Id == id && node.DefenderCount > 0);
                Assert(index >= 0, "Practice target is a real battle node: " + id);
                controller.SelectPracticeBattle(index);
                Assert(controller.Campaign.IsPractice && controller.Campaign.CurrentNode.Id == id,
                    "Normal practice command selected " + id);
                yield return ResolveChoices();
                StageDefenseEvidenceRoster(controller.Campaign.CurrentNode.Plane, probe);
                yield return ReadyArt();

                var model = controller.Model;
                var defenders = model.Defenders.ToArray();
                var entry = new DefenseStage
                {
                    id = id,
                    side = defenders.Min(d => d.Position.y) < 4 ? "north" : "south",
                    initialHealth = Mathf.RoundToInt(defenders.Sum(d => d.MaxHealth)),
                };
                result.defenseStages.Add(entry);
                result.node = id;
                Assert(defenders.Length == controller.Campaign.CurrentNode.DefenderCount &&
                    defenders.Select(d => d.ArtId).Distinct().Count() == defenders.Length &&
                    defenders.Select(d => d.Position).Distinct().Count() == defenders.Length &&
                    defenders.Max(d => d.Position.x) - defenders.Min(d => d.Position.x) < 5 &&
                    defenders.Max(d => d.Position.y) - defenders.Min(d => d.Position.y) < 4,
                    id + " scouted a single compact legal roster");
                Assert(defenders.Where(d => d.SkillCell.HasValue).All(d =>
                    model.Terrain.IsWalkable(d.SkillCell.Value) && defenders.All(guard => guard.Position != (Vector2)d.SkillCell.Value)),
                    id + " reserved skill cells remain clear");
                controller.SetFormationView(true);
                yield return WaitForScoutingView(true);
                yield return Frame(id + "-scout-" + entry.side);
                controller.SetFormationView(false);
                yield return WaitForScoutingView(false);
                controller.StartBattle();
                Assert(model.Phase == FormationPhase.Fighting, id + " starts on the normal battle command");
                var probeGuard = probe ? StageDefenseSkillProbe(model, id) : null;
                string capture = probe ? "probe-" + id : id;
                yield return null;

                var tracked = probe ? new[] { probeGuard } : model.Defenders.Where(d => FormationCampaignRules.DefenderByArtId(d.ArtId)?.Rarity == 6).ToArray();
                var skillHits = new HashSet<string>();
                var recorded = new HashSet<string>();
                while ((model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting) &&
                    (!probe || recorded.Count == 0 || skillHits.Count == 0))
                {
                    if (model.Clock >= (probe ? 40 : 240)) throw new InvalidOperationException(id + " exceeded the fixture simulation budget");
                    model.Paused = false;
                    for (int step = 0; step < 24 && (model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting); step++)
                    {
                        model.Advance(.025f);
                        foreach (var guard in tracked)
                        {
                            bool castObserved = guard.ArtId switch
                            {
                                "D22" => model.GuardEffect(guard)?.Kind == "firewall",
                                "D23" => model.GuardTransformed(guard),
                                "D28" => model.GuardChessPieces(guard).Any(piece => piece.Kind == "chess_hit"),
                                _ => guard.LastSkillAt >= 0 || model.GuardEffect(guard) != null,
                            };
                            bool firstHit = model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill) && skillHits.Add(guard.ArtId);
                            if (castObserved && recorded.Add(guard.ArtId))
                            {
                                entry.filmedCasts.Add(guard.ArtId + "@" + model.Clock.ToString("0.000"));
                                yield return Frame(capture + "-" + guard.ArtId + "-skill");
                                if (id == "1-6" && guard.ArtId == "D22")
                                    yield return CaptureInterfaceClip(id + "-D22-normal-speed", 48, "-formationDefenseClips");
                                else if (probe)
                                    yield return CaptureInterfaceClip(capture + "-" + guard.ArtId + "-normal-speed", 36, "-formationDefenseClips");
                            }
                            if (firstHit) yield return Frame(capture + "-" + guard.ArtId + "-contact");
                        }
                    }
                    yield return null;
                }
                if (probe)
                    Assert(recorded.Contains(probeGuard.ArtId) && skillHits.Contains(probeGuard.ArtId), id + " witnessed real " + probeGuard.ArtId + " cast and contact");
                else
                    Assert(model.Phase == FormationPhase.Won || model.Phase == FormationPhase.Lost, id + " has a normal battle result");
                if (id == "1-6") Assert(recorded.Contains("D22"), "1-6 Yu actually deployed the fire wall");
                entry.observedSkillHits.AddRange(skillHits.OrderBy(guard => guard));
                entry.outcome = probe ? "Diagnostic skill observed" : model.Phase.ToString();
                entry.remaining = model.RemainingDefenders;
                entry.alliedDeaths = model.Units.Sum(unit => unit.Deaths);
                entry.battleSeconds = model.Clock;
                result.battles++;
                if (!probe) yield return Frame(id + "-result");
            }
        }

        FormationDefender StageDefenseSkillProbe(FormationBattleModel model, string node)
        {
            string artId = node switch { "2-4" => "D23", "2-5" => "D27", "3-2" => "D26", _ => "D28" };
            var guard = model.Defenders.Single(d => d.ArtId == artId);
            var target = model.Units.Single(unit => unit.Enabled);
            Vector2 cell;
            if (artId == "D23")
            {
                guard.Health *= .25f;
                Vector2 landing = guard.SkillCell.Value;
                cell = landing + (landing.y < 4 ? Vector2.down : Vector2.up);
            }
            else if (artId == "D28") cell = (Vector2)(model.FindWangCrossCenter(guard) ?? throw new InvalidOperationException("No legal Wang cross on " + node));
            else cell = guard.Position + Vector2.left;
            Assert(model.Terrain.IsWalkable(cell) && model.Defenders.All(other =>
                (other.Position - cell).sqrMagnitude >= .23f), node + " diagnostic target is in an open position");
            target.Position = cell;
            target.MaxHealth = target.Health = 1000000;
            return guard;
        }

        void StageDefenseEvidenceRoster(int plane, bool probe)
        {
            var pieces = probe ? new[] { ("B10", 1) } : plane == 1
                ? new[] { ("M27", 2), ("M11", 2), ("M21", 2), ("M12", 2), ("M25", 1) }
                : new[] { ("M20", 2), ("M28", 3), ("B10", 2), ("M11", 3), ("WIS", 1) };
            controller.Shop.Preset(pieces.Select(p => new ShopTrialModel.Piece
                { Id = FormationCampaignRules.Find(p.Item1).CardId, Star = p.Item2 }), Array.Empty<string>(), pieces.Length, 0, 0);
            controller.Session.ApplyTo(controller.Model);
            var allies = controller.Shop.Bench.ToArray();
            float targetRow = controller.Model.Defenders.Average(guard => guard.Position.y);
            foreach (var piece in allies)
            {
                var anchors = from x in Enumerable.Range(0, FormationBattleModel.DeploymentColumns).Reverse()
                              from y in Enumerable.Range(0, FormationBattleModel.Height).OrderBy(row => Mathf.Abs(row - targetRow))
                              select new Vector2Int(x, y);
                var anchor = anchors.First(cell =>
                {
                    var preview = controller.Session.PreviewDeploy(piece.Instance, cell);
                    return preview.Valid && preview.SwapInstance == 0;
                });
                Assert(controller.Session.TryDeploy(piece.Instance, anchor).Success, "Place real preset card " + piece.Id + " on " + anchor);
            }
        }

        void Finish(Exception error)
        {
            done=true;result.error=error?.ToString();result.exceptionCount=result.exceptions.Count;result.assertions=result.checks.Count;
            result.passed=error==null&&result.exceptionCount==0;
            string report = MapVariantsRequested ? "map-variants-smoke.json" : SupernovaBossRequested ? "supernova-boss-smoke.json" :
                SnowBossRequested ? "snow-boss-smoke.json" : SynergyRequested ? "synergy-smoke.json" :
                DefenseRequested ? DefenseSkillProbe ? "defense-skill-probe.json" : "defense-smoke.json" :
                OpeningDeliveryRequested ? "opening-delivery-smoke.json" : StrategyHistoryRequested ? "strategy-history-smoke.json" :
                ReserveRequested ? "reserve-smoke.json" : "campaign-smoke.json";
            File.WriteAllText(Path.Combine(directory,report),JsonUtility.ToJson(result,true));
            string marker = MapVariantsRequested ? "FORMATION_MAP_VARIANTS_SMOKE_" : SupernovaBossRequested ? "FORMATION_SUPERNOVA_BOSS_SMOKE_" :
                SynergyRequested ? "FORMATION_SYNERGY_SMOKE_" : DefenseRequested ? "FORMATION_DEFENSE_SMOKE_" : "FORMATION_CAMPAIGN_SMOKE_";
            Debug.Log(marker+(result.passed?"PASS":"FAIL: "+result.error));
            Application.Quit(result.passed?0:1);
        }
    }
}
