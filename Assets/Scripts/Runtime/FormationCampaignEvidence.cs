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
        IEnumerator CaptureInterfaceClip(string name, int frames)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-formationUiClips") < 0) yield break;
            string target = Path.Combine(directory, name);
            Directory.CreateDirectory(target);
            double start = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < frames; i++)
            {
                while (Time.realtimeSinceStartupAsDouble < start + i / 24d) yield return null;
                yield return new WaitForEndOfFrame();
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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-formationStrategyEvidence") >= 0)
            {
                result.scope = "Production strategy panels on the authored board, with three seeded runs through two actual auto battles each. Choice confirmation is driven through the campaign API.";
                yield return VerifyStrategyVariants();
                yield break;
            }
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
            int offer=Enumerable.Range(0,controller.Shop.Offers.Count).First(index=>controller.Shop.Offers[index]!=null);
            string offerId=controller.Shop.Offers[offer];int goldBeforeInspect=controller.Shop.Gold;
            controller.ToggleFormationOfferInspection(offer,offerId,controller.Shop.Definition(offerId).Cost,
                FormationBattleController.FormationOfferRect(offer));
            Assert(controller.FormationInspectedCardId==offerId&&controller.Shop.Gold==goldBeforeInspect,
                "Inspecting a live shop offer neither purchases it nor changes the price");
            yield return Frame("03-shop-offer-detail");
            yield return Market(false);
            controller.SetFormationView(true);yield return null;float scoutingTileWidth=GuardTileScreenWidth();yield return Frame("04-close-guard-area");controller.SetFormationView(false);
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
                if (battle == 1)
                {
                    Assert(controller.Campaign.PendingBalls.Count(ball=>ball.Component==null) == 4 && controller.Campaign.BallRewards.Count == 0,
                        "First real reward settlement drops four unopened balls without payouts");
                    var dropped = controller.Campaign.PendingBalls.Select(ball=>ball.Id).ToArray();
                    Assert(controller.ContinueCampaign(),"Unopened ground rewards do not block the next node");
                    yield return null;
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
        IEnumerator VerifyStrategyVariants()
        {
            Assert(Resources.Load<Texture2D>("UI/Strategy/plates") != null &&
                Resources.Load<Texture2D>("UI/Strategy/props") != null &&
                Resources.Load<Texture2D>("UI/Strategy/veil") != null, "All strategy art loads in Player");
            var seeds = new Dictionary<string, int>();
            for (int seed = controller.seed; seed < controller.seed + 300 && seeds.Count < 3; seed++)
            {
                var run = new FormationShopSession(seed, fullCampaign: true).Run;
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
                Assert(controller.ChooseCampaignOption(0).Success, "Choose starter equipment for " + tier);
                Assert(controller.Campaign.Choices.Count == 3 &&
                    controller.Campaign.Choices.All(option => option.Strategy?.Tier == ""), "Opening choices are ungraded for " + tier);
                if (tier == "银") yield return Frame("strategy-opening");
                while (controller.Campaign.HasBlockingChoice)
                    Assert(controller.ChooseCampaignOption(0).Success, "Resolve opening choice for " + tier);

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
                yield return Frame("strategy-" + tier);
                Assert(controller.Campaign.RerollChoice() && !controller.Campaign.CanRerollChoice &&
                    controller.Campaign.Choices.All(option => option.Strategy?.Tier == tier), "One reroll preserves the round's tier " + tier);
                string chosen = controller.Campaign.Choices[0].Id;
                Assert(controller.ChooseCampaignOption(0).Success && controller.Campaign.HasAugment(chosen),
                    "Confirmation applies the real choice " + chosen);
            }
        }
        IEnumerator ClaimGroundRewards(bool capture, string clipPrefix = "reward-opening")
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
                Assert(projected && position.x > 860 && position.x < 1115 && position.y > 120 && position.y < 610,
                    "Clickable 3D " + drop.Color + " ball sits in the visible red-gate reward bay");
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

        IEnumerator VerifyConcurrentGroundRewards()
        {
            yield return new WaitForSecondsRealtime(.45f);
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
        void Finish(Exception error)
        {
            done=true;result.error=error?.ToString();result.exceptionCount=result.exceptions.Count;result.assertions=result.checks.Count;
            result.passed=error==null&&result.exceptionCount==0;File.WriteAllText(Path.Combine(directory,"campaign-smoke.json"),JsonUtility.ToJson(result,true));
            Debug.Log(result.passed?"FORMATION_CAMPAIGN_SMOKE_PASS":"FORMATION_CAMPAIGN_SMOKE_FAIL: "+result.error);
            Application.Quit(result.passed?0:1);
        }
    }
}
