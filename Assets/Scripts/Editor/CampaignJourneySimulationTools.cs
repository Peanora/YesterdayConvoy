using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    // A legal-command search over the real runtime models. Failed deployment
    // branches are separate candidate runs, never an in-game retry/refund.
    public static class CampaignJourneySimulationTools
    {
        [Serializable] public sealed class ShopCommand
        {
            public string kind,id,primaryForm,secondaryForm;public int slot=-1,instance,goldBefore,goldAfter,levelAfter,xpAfter;
            public int[] lineupInstances;public string[] lineupIds;
        }
        [Serializable] public sealed class BattleAction
        {public string kind,cardId,routeId,entityId;public long request;public double at;}
        [Serializable] public sealed class Piece
        {public string id;public int instance,star;}
        [Serializable] public sealed class Fight
        {
            public string policy,phase;public bool won,timedObservation;public double seconds;public int remainingDefense,dp,breaches,deployments,portalTrips;
            public float damage,remainingDefenderHP,heatDamage,defenderHealing;public string[] survivingDefenders;public BattleAction[] actions;
        }
        [Serializable] public sealed class Round
        {
            public int stage,openingGold,openingLevel,closingGold,closingLevel,closingXP,baseReward,interest,streakReward,experienceReward,golemXP;
            public bool supply,won;public string[] initialOffers;public Piece[] lineup;public ShopCommand[] shopCommands;public Fight fight;
            public int deploymentPoliciesTried;public string diagnosis;
            public string sceneName,mapId,encounterId,contractCard;
            public string infectionPrimary="Swarm",infectionSecondary="Shell",marinePrimary="Barrier",marineSecondary="Hunt";
            public bool reunionRanged;
        }
        [Serializable] public sealed class Attempt
        {public int seed,completedStages,failedStage;public string economyPolicy,gift,error;public string[] gifts;public bool won;public Round[] rounds;}
        [Serializable] public sealed class Report
        {
            public string version="actual-runtime-campaign-v2-twelve-stage-search-3",status,createdUtc;
            public string[] roleOrder;public float observationSeconds;public Attempt[] attempts;
            public string evidenceBoundary="Real finite Journey shop/inventory/XP/ManualBattle and persisted wave encounters. Scenario wave deadlines are actual gameplay; the observation horizon never forces a result. Counterfactual deployment branches are search, not gameplay retries. A fixed-seed witness is not a player win rate or a balance guarantee. No state injection, free rerolls, unlimited gold, or forced victory.";
        }
        [Serializable] public sealed class BoundaryCase
        {
            public string name; public int stage,baseCopyCost,wins,formationLevel,firstDeploymentDP;
            public bool expectedWeak,zeroSynergies;public Piece[] lineup;public Fight[] fights;
        }
        [Serializable] public sealed class BoundaryReport
        {
            public string version="campaign-v2-wave-boundary-1",createdUtc;
            public string evidenceBoundary="Finite-stock battle fixtures, not seeded acquisitions or full-run witnesses. Strong references list base-copy prices only, without promising shop hits. Every fight uses real ManualBattle, current-wave entry, actual DP/uses/CD/skills/deadline. No stat injection or forced outcome. A bounded policy failure is not a proof of impossibility.";
            public bool allWeakCasesFailed,finalBossWeakCasesFailed;public BoundaryCase[] cases;
        }
        [Serializable] public sealed class TimingReceipt
        {
            public int sourceIndex,originalWave,actualWave=-1;public long request;
            public string kind,cardId,routeId,currentLane,entityId,failure="NotReached";
            public double originalAt,scheduledAt,executedAt=-1;public bool accepted,sameWaveAndLane;
        }
        [Serializable] public sealed class TimingCase
        {
            public int stage,originalWave=-1,accepted,rejected;public bool strictBaseline,allActionsAccepted,sameWaveAndLane;
            public double originalGroupAt=-1,waveEnd=-1,originalMargin=-1,advanceSeconds;
            public string infectionPrimary,infectionSecondary,contractCard;public Piece[] lineup;
            public Fight fight;public TimingReceipt[] receipts;
        }
        [Serializable] public sealed class TimingReport
        {
            public string version="campaign-v2-timing-margins-1",createdUtc,sourceTrace,status,error;public int seed;
            public string evidenceBoundary="Strict original-trace terminal replay, then independent earlier-only probes of each wave's final simultaneous Deploy group. Wave-opening groups are skipped because moving them earlier leaves their original window. Purchased inventory, lineup, forms, lanes, every other action time, and all runtime values remain fixed. Commands use the real APIs and 0.05-second ticks; rejected commands are recorded without retries. This is a fixed-witness sensitivity audit, not a win-rate estimate.";
            public string[] skippedGroups;public TimingCase[] cases;
        }
        sealed class EconomyPolicy
        {internal string Name;internal int[] Levels,Cards,Refresh;internal int MaximumCost=5;internal bool CommissionGift,ReunionFocus,HighCostTransition;}
        sealed class Context
        {
            internal int Seed;internal ShopTrialModel Shop;internal BattleShopRun Run;
            internal Dictionary<string,ManualBattleDefinition> Definitions;internal List<string> Lineup=new List<string>();
            internal string Contract,Gift;internal int MaximumCost=5,Stage;internal bool ReunionFocus,HighCostTransition;
            internal InfectionForm Form=InfectionForm.Swarm,Second=InfectionForm.Shell;
            internal string[] FocusPriority=Array.Empty<string>();
        }
        static readonly EconomyPolicy[] Policies={
            new EconomyPolicy{Name="balanced",Levels=new[]{2,3,4,4,5,5,6,6,7,7,7,8},Cards=new[]{1,2,2,2,3,3,3,3,4,4,4,5},Refresh=new[]{0,0,0,0,2,0,2,0,2,2,2,2}},
            new EconomyPolicy{Name="low-cost-stars",MaximumCost=3,Levels=new[]{2,3,4,4,5,5,5,5,6,6,7,7},Cards=new[]{1,2,3,3,4,4,4,5,5,5,5,8},Refresh=new[]{0,0,0,0,2,2,2,2,2,2,2,2}},
            new EconomyPolicy{Name="level-transition",Levels=new[]{2,3,4,4,5,5,6,6,7,7,8,8},Cards=new[]{1,2,2,2,2,3,3,3,3,4,4,6},Refresh=new[]{0,0,0,0,0,0,2,0,2,2,2,2}},
            new EconomyPolicy{Name="gift-three-star-level7",CommissionGift=true,Levels=new[]{2,3,4,4,5,5,6,6,7,7,7,7},Cards=new[]{2,3,3,0,4,3,4,0,5,4,0,10},Refresh=new[]{0,0,0,0,2,0,2,0,2,0,0,2}},
            new EconomyPolicy{Name="formation-reunion",ReunionFocus=true,MaximumCost=4,Levels=new[]{2,3,4,4,5,5,6,6,6,6,7,7},Cards=new[]{2,3,3,0,4,4,4,0,6,5,0,10},Refresh=new[]{0,0,0,0,2,0,2,0,2,0,0,2}},
            // These are legal spending targets, not injected levels or promised shop hits.
            // Full-cap cash forecasts keep room for the actual 116-gold XP cost to ten.
            new EconomyPolicy{Name="late-ten-high-cost",HighCostTransition=true,Levels=new[]{1,3,4,4,5,5,6,6,7,8,8,10},Cards=new[]{1,2,3,0,5,5,6,0,10,10,0,18},Refresh=new[]{0,0,0,0,2,2,2,0,4,4,0,8}},
            new EconomyPolicy{Name="level-ten-pair-transition",HighCostTransition=true,Levels=new[]{1,3,4,4,5,6,6,7,8,8,9,10},Cards=new[]{1,2,3,0,4,4,6,0,8,8,0,15},Refresh=new[]{0,0,0,0,2,2,2,0,4,4,0,8}}
        };

        [MenuItem("Yesterday Convoy/Campaign Journey/Search Natural V2 Runs")]
        public static void Search()
        {
            CampaignJourneyCatalog.Current.Validate();
            int[] seeds=Argument("-journeySearchSeeds","73191,20261108,31016,44").Split(',').Select(int.Parse).ToArray();
            int limit=int.Parse(Argument("-journeySearchLimit","20"),CultureInfo.InvariantCulture);
            int variants=int.Parse(Argument("-journeyDeploymentVariants","42"),CultureInfo.InvariantCulture);
            float observation=float.Parse(Argument("-journeyObservationSeconds","300"),CultureInfo.InvariantCulture);
            bool allSeeds=bool.Parse(Argument("-journeySearchAllSeeds","false"));
            var policyNames=Argument("-journeyEconomyPolicies",string.Join(",",Policies.OrderByDescending(p=>p.HighCostTransition).Select(p=>p.Name))).Split(',');
            var policies=policyNames.Select(name=>Policies.SingleOrDefault(p=>p.Name==name)).Where(p=>p!=null).ToArray();
            if(policies.Length==0)throw new ArgumentException("No known Journey economy policies selected.");
            string output=Argument("-journeySearchOutput","artifacts/campaign-v2/model-search.json");
            var definitions=Definitions();var attempts=new List<Attempt>();
            var report=new Report{createdUtc=DateTime.UtcNow.ToString("O"),observationSeconds=observation,roleOrder=definitions.Keys.ToArray()};
            foreach(int seed in seeds)
            {
                foreach(var policy in policies)
                {
                    if(attempts.Count>=limit||!allSeeds&&attempts.Any(a=>a.won))break;
                    var attempt=Simulate(seed,policy,definitions,observation,variants);attempts.Add(attempt);
                    report.attempts=attempts.ToArray();report.status=attempts.Any(a=>a.won)?"fixed-seed legal full-run witness found":"search in progress; no full witness yet";
                    Save(output,report);
                    Debug.Log("JOURNEY_NATURAL_SEARCH: seed="+seed+" policy="+policy.Name+" completed="+attempt.completedStages+" won="+attempt.won+" failedAt="+attempt.failedStage+" error="+attempt.error);
                }
                if(attempts.Count>=limit||!allSeeds&&attempts.Any(a=>a.won))break;
            }
            report.attempts=attempts.ToArray();report.status=attempts.Any(a=>a.won)?"fixed-seed legal full-run witness found":"no full-run witness within this bounded search";Save(output,report);
            Debug.Log("JOURNEY_NATURAL_SEARCH_COMPLETE: "+report.status+"; "+Path.GetFullPath(output));
        }
        public static void AuthorAndEvaluate()
        {
            CampaignJourneyTools.Author();Search();EvaluateBuildBoundaries();
        }
        [MenuItem("Yesterday Convoy/Campaign Journey/Evaluate Wave Build Boundaries")]
        public static void EvaluateBuildBoundaries()
        {
            CampaignJourneyCatalog.Current.Validate();var definitions=Definitions();
            int variants=Math.Max(1,Math.Min(42,int.Parse(Argument("-journeyBoundaryVariants","42"),CultureInfo.InvariantCulture)));
            int[] stages=Argument("-journeyBoundaryStages","5,12").Split(',').Select(int.Parse).ToArray();
            var fixtures=new[]{
                new {Name="two-one-star-one-cost-no-synergy",Weak=true,Ids=new[]{"CARD_ORIGINIUM_SLUG","CARD_M07"},Stars=new[]{1,1}},
                new {Name="three-one-star-one-two-cost-no-synergy",Weak=true,Ids=new[]{"CARD_M07","CARD_M04","CARD_M29"},Stars=new[]{1,1,1}},
                new {Name="three-one-star-two-cost-no-synergy",Weak=true,Ids=new[]{"CARD_M05","CARD_M26","CARD_M29"},Stars=new[]{1,1,1}},
                new {Name="grown-six-mixed-reference",Weak=false,Ids=new[]{"CARD_M31","CARD_M18","CARD_M07","CARD_M08","CARD_M37","CARD_M12"},Stars=new[]{2,1,3,2,2,1}},
                new {Name="grown-seven-low-cost-synergy-reference",Weak=false,Ids=new[]{"CARD_M07","CARD_M08","CARD_M09","CARD_M15","CARD_M10","CARD_M05","CARD_M03"},Stars=new[]{3,2,2,2,2,2,2}},
                new {Name="talulah-one-star-eight-member-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M18","CARD_M17","CARD_M03","CARD_M15","CARD_M06","CARD_M12","CARD_M11"},Stars=new[]{1,1,1,1,1,1,1,1}},
                new {Name="talulah-two-star-core-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M18","CARD_M17","CARD_M03","CARD_M15","CARD_M06","CARD_M12","CARD_M11"},Stars=new[]{2,2,1,1,1,1,1,1}},
                new {Name="talulah-two-star-reunion-guerrilla-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M06","CARD_M11","CARD_M12","CARD_M03","CARD_M17","CARD_M18","CARD_M19"},Stars=new[]{2,2,2,2,2,2,2,2}},
                new {Name="talulah-two-star-reunion-snow-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14","CARD_M17","CARD_M18"},Stars=new[]{2,2,2,2,2,2,2,2}},
                new {Name="talulah-three-star-core-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M18","CARD_M17","CARD_M03","CARD_M15","CARD_M06","CARD_M12","CARD_M11"},Stars=new[]{3,2,1,1,1,1,1,1}},
                new {Name="patriot-two-star-guerrilla-reference",Weak=false,Ids=new[]{"CARD_M20","CARD_M18","CARD_M17","CARD_M16","CARD_M19","CARD_M03","CARD_M11","CARD_M12"},Stars=new[]{2,2,2,2,2,2,2,2}},
                new {Name="frostnova-two-star-snow-reference",Weak=false,Ids=new[]{"CARD_M14","CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M18","CARD_M17","CARD_M07"},Stars=new[]{2,2,3,2,2,2,2,3}},
                new {Name="mephisto-two-star-host-reference",Weak=false,Ids=new[]{"CARD_M25","CARD_M21","CARD_M22","CARD_M23","CARD_M18","CARD_M17","CARD_M12","CARD_M11"},Stars=new[]{2,3,2,2,2,2,2,3}},
                new {Name="mudrock-two-star-crew-reference",Weak=false,Ids=new[]{"CARD_M28","CARD_M27","CARD_M26","CARD_M42","CARD_M43","CARD_M19","CARD_M37","CARD_M18"},Stars=new[]{2,2,2,2,2,2,2,2}},
                new {Name="level-ten-ten-kind-high-star-reference",Weak=false,Ids=new[]{"CARD_TALULAH_TRIAL","CARD_M20","CARD_M14","CARD_M25","CARD_M28","CARD_M18","CARD_M17","CARD_M12","CARD_M11","CARD_M19"},Stars=new[]{2,2,2,2,2,2,2,2,3,2}}
            };
            var results=new List<BoundaryCase>();
            var requested=Argument("-journeyBoundaryNames","").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries);
            foreach(int number in stages)foreach(var fixture in fixtures.Where(f=>requested.Length==0||requested.Contains(f.Name)))
            {
                var stage=CampaignJourneyCatalog.Stage(number);if(stage.IsSupply)throw new ArgumentException("Build boundaries require combat stages.");
                var shop=new ShopTrialModel(definitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),73191);
                shop.EnableJourneyEconomy();
                int level=Math.Max(8,fixture.Ids.Length);
                shop.Preset(fixture.Ids.Select((id,i)=>new ShopTrialModel.Piece{Id=id,Star=fixture.Stars[i]}),Array.Empty<string>(),level,0,0);
                if(!shop.SetLineup(fixture.Ids,shop.Bench.Select(p=>p.Instance)))throw new InvalidOperationException("Invalid finite-stock fixture.");
                bool zero=fixture.Ids.SelectMany(CampaignBalance.Tags).GroupBy(t=>t).All(g=>g.Count()<2);
                if(fixture.Weak&&!zero)throw new InvalidOperationException("A no-synergy weak fixture acquired a synergy: "+fixture.Name);
                var context=new Context{Seed=73191,Shop=shop,Run=new BattleShopRun(shop,73191,campaign:true,journeyV2:true),Definitions=definitions,Lineup=fixture.Ids.ToList()};
                context.Contract=shop.Bench.Where(p=>CampaignBalance.Tags(p.Id).Contains("萨卡兹")).OrderByDescending(p=>p.Star).FirstOrDefault()?.Id;
                var fights=new List<Fight>();
                for(int variant=0;variant<variants;variant++)fights.Add(FightBattle(context,CreateBattle(context,stage),variant,300));
                var result=new BoundaryCase{name=fixture.Name,stage=number,expectedWeak=fixture.Weak,zeroSynergies=zero,
                    baseCopyCost=shop.Bench.Sum(p=>ShopTrialModel.Copies(p.Star)*shop.Definition(p.Id).Cost),formationLevel=level,
                    firstDeploymentDP=shop.Bench.Sum(p=>CampaignV2Balance.Build(definitions[p.Id],p.Star,50,stage.defense,waveMode:true).Unit.DeploymentCost),
                    lineup=shop.Bench.Select(p=>new Piece{id=p.Id,instance=p.Instance,star=p.Star}).ToArray(),wins=fights.Count(f=>f.won),fights=fights.ToArray()};
                results.Add(result);shop.Validate();
                Debug.Log("JOURNEY_BUILD_BOUNDARY: stage="+number+" fixture="+fixture.Name+" wins="+result.wins+"/"+variants+" bestDefense="+fights.Min(f=>f.remainingDefense));
            }
            var finalWeak=results.Where(r=>r.expectedWeak&&r.stage==CampaignJourneyDefinition.StageCount).ToArray();
            var report=new BoundaryReport{createdUtc=DateTime.UtcNow.ToString("O"),cases=results.ToArray(),allWeakCasesFailed=results.Where(r=>r.expectedWeak).All(r=>r.wins==0),
                finalBossWeakCasesFailed=finalWeak.Length>0&&finalWeak.All(r=>r.wins==0)};
            string path=Path.GetFullPath(Argument("-journeyBoundaryOutput","artifacts/campaign-retune/build-boundaries.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
            Debug.Log("JOURNEY_BUILD_BOUNDARIES_COMPLETE: allWeakCasesFailed="+report.allWeakCasesFailed+" finalBossWeakCasesFailed="+report.finalBossWeakCasesFailed+"; "+path);
        }
        static string Argument(string key,string fallback)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        static void Save(string path,Report report)
        {string absolute=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(absolute));File.WriteAllText(absolute,JsonUtility.ToJson(report,true));}
        static Dictionary<string,ManualBattleDefinition> Definitions()
        {
            // Identical insertion order to ManualBattleController.BuildMonsterTestCatalog.
            var ordered=new[]{AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/ManualDemo.asset"),
                AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset")}
                .Concat(Resources.LoadAll<ManualBattleDefinition>("MonsterTests").OrderBy(d=>d.name,StringComparer.Ordinal));
            var result=new Dictionary<string,ManualBattleDefinition>(StringComparer.Ordinal);
            foreach(var data in ordered)
            {
                if(data==null||data.Card==null||!data.Card.HasFrameAnimation||!CampaignBalance.ShopEligible(data.Card.CardId))continue;
                if(!result.ContainsKey(data.Card.CardId))result.Add(data.Card.CardId,data);
            }
            if(result.Count!=42)throw new InvalidOperationException("Natural simulation needs the same 42 eligible card definitions: "+result.Count);
            return result;
        }
        static Attempt Simulate(int seed,EconomyPolicy policy,Dictionary<string,ManualBattleDefinition> definitions,float observation,int variants)
        {
            var result=new Attempt{seed=seed,economyPolicy=policy.Name};var rounds=new List<Round>();
            try
            {
                var shop=new ShopTrialModel(definitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
                shop.EnableJourneyEconomy();var gifts=CampaignJourneyEconomy.StartingCards(shop.Roles,seed);
                string gift=gifts[0];result.gift=gift;result.gifts=gifts;
                shop.Preset(gifts.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),1,0,CampaignJourneyEconomy.InitialGold);
                var context=new Context{Seed=seed,Shop=shop,Run=new BattleShopRun(shop,seed,campaign:true,journeyV2:true),Definitions=definitions,MaximumCost=policy.MaximumCost,Gift=gift,ReunionFocus=policy.ReunionFocus,HighCostTransition=policy.HighCostTransition};context.Lineup.Add(gift);
                context.Run.BeginJourneyPreparation();
                for(int number=1;number<=CampaignJourneyDefinition.StageCount;number++)
                {
                    var stage=CampaignJourneyCatalog.Stage(number);
                    context.Stage=number;
                    var savedMap=stage.map??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=stage.stage&&s.map!=null).map;
                    var round=new Round{stage=number,supply=stage.IsSupply,openingGold=shop.Gold,openingLevel=shop.Level,initialOffers=shop.Offers.ToArray(),
                        sceneName=savedMap.sceneName,mapId=savedMap.id,encounterId=stage.encounter==null?"supply":stage.encounter.name};rounds.Add(round);
                    var commands=new List<ShopCommand>();
                    if(context.Run.CanShop)Prepare(context,policy,number,commands);
                    SelectLineup(context,commands);
                    context.Contract=Selected(context).Where(p=>CampaignBalance.Tags(p.Id).Contains("萨卡兹")).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).FirstOrDefault()?.Id;
                    if(context.Contract!=null&&context.Run.CanShop)commands.Add(Command(shop,"SetContract",shop.Gold,context.Contract));
                    round.contractCard=context.Contract;
                    round.lineup=Selected(context).Select(p=>new Piece{id=p.Id,instance=p.Instance,star=p.Star}).ToArray();round.shopCommands=commands.ToArray();
                    if(context.Run.CanShop&&!context.Run.PrepareBattle())throw new InvalidOperationException("Actual run rejected preparation.");
                    ManualBattle winning=null;Fight best=null;int tried=0;
                    if(stage.IsSupply)
                    {
                        winning=CreateBattle(context,stage);winning.Begin();best=Describe(winning,"guaranteed supply",new List<BattleAction>(),false);tried=1;
                    }
                    else
                    {
                        int count=Math.Max(1,Math.Min(variants,42));
                        for(int variant=0;variant<count;variant++)
                        {
                            var battle=CreateBattle(context,stage);var fight=FightBattle(context,battle,variant,observation);tried++;
                            if(best==null||Better(fight,best))best=fight;
                            if(fight.won){winning=battle;best=fight;break;}
                        }
                    }
                    round.fight=best;round.deploymentPoliciesTried=tried;round.won=best!=null&&best.won;
                    if(winning==null||!round.won)
                    {
                        result.failedStage=number;round.diagnosis="No winning deployment branch within bound; not a proof of impossibility. Remaining guards: "+string.Join(",",best?.survivingDefenders??Array.Empty<string>());
                        round.closingGold=shop.Gold;round.closingLevel=shop.Level;round.closingXP=shop.XP;break;
                    }
                    if(!context.Run.Observe(winning.Phase,winning.GolemGrowthEarned,golemCapacity:winning.GolemCapacity,remainingDefense:winning.Defense,initialDefense:winning.Settings.InitialDefense))throw new InvalidOperationException("Actual run rejected its natural result.");
                    round.baseReward=context.Run.Reward.Base;round.interest=context.Run.Reward.Interest;round.streakReward=context.Run.Reward.Streak;round.experienceReward=context.Run.Reward.Experience;
                    round.closingGold=shop.Gold;round.closingLevel=shop.Level;round.closingXP=shop.XP;round.golemXP=context.Run.GolemExperience;
                    result.completedStages=number;
                    if(number<CampaignJourneyDefinition.StageCount&&!context.Run.OpenShop())throw new InvalidOperationException("Actual run rejected the next free shop.");
                }
                result.won=context.Run.Phase==BattleShopPhase.Complete;shop.Validate();
            }
            catch(Exception error){result.error=error.ToString();if(result.failedStage==0)result.failedStage=rounds.LastOrDefault()?.stage??1;}
            result.rounds=rounds.ToArray();return result;
        }
        static bool Better(Fight a,Fight b)=>a.won&&!b.won||a.won==b.won&&(a.remainingDefense<b.remainingDefense||a.remainingDefense==b.remainingDefense&&a.remainingDefenderHP<b.remainingDefenderHP);
        static List<ShopTrialModel.Piece> Selected(Context c)=>c.Lineup.Select(id=>c.Shop.Bench.Where(p=>p.Id==id).OrderByDescending(p=>c.Shop.IsLineupInstance(p.Instance)).ThenByDescending(p=>p.Star).ThenBy(p=>p.Instance).First()).ToList();
        static ShopCommand Command(ShopTrialModel shop,string kind,int before,string id=null,int slot=-1,int instance=0)=>
            new ShopCommand{kind=kind,id=id,slot=slot,instance=instance,goldBefore=before,goldAfter=shop.Gold,levelAfter=shop.Level,xpAfter=shop.XP};
        static void Prepare(Context c,EconomyPolicy policy,int stage,List<ShopCommand> commands)
        {
            var shop=c.Shop;int i=stage-1;
            while(shop.Level<policy.Levels[i]&&shop.Gold>=4)
            {int before=shop.Gold;if(!shop.Upgrade().Success)break;commands.Add(Command(shop,"Upgrade",before));}
            int cards=policy.Cards[i],refresh=policy.Refresh[i];
            if(policy.CommissionGift&&!shop.CommissionUsed&&!shop.Bench.Any(p=>p.Id==c.Gift&&p.Star>=3)&&cards>=shop.Definition(c.Gift).Cost+2&&shop.Gold>=shop.Definition(c.Gift).Cost+2)
            {
                int before=shop.Gold;var receipt=shop.Commission(c.Gift);
                if(receipt.Success){cards-=receipt.Value;commands.Add(Command(shop,"Commission",before,c.Gift,instance:receipt.Instance));SelectLineup(c,commands);}
            }
            Buy(c,stage,ref cards,commands);
            Commission(c,stage,ref cards,commands);
            while(refresh>=2&&shop.Gold>=2)
            {
                int before=shop.Gold;if(!shop.Refresh().Success)break;refresh-=2;commands.Add(Command(shop,"Refresh",before));
                Buy(c,stage,ref cards,commands);Commission(c,stage,ref cards,commands);
            }
            if(stage==CampaignJourneyDefinition.StageCount)
            {
                // No next battle to finance; use the real remaining cash on useful
                // purchases/search. This promises no particular shop hit.
                for(int turn=0;turn<64&&shop.Gold>0;turn++)
                {
                    int flexible=shop.Gold;Buy(c,stage,ref flexible,commands);Commission(c,stage,ref flexible,commands);
                    if(shop.Gold<2)break;
                    int before=shop.Gold;if(!shop.Refresh().Success)break;commands.Add(Command(shop,"Refresh",before));
                }
            }
        }
        static void Buy(Context c,int stage,ref int budget,List<ShopCommand> commands)
        {
            for(int turn=0;turn<5;turn++)
            {
                int availableBudget=budget;
                var choices=Enumerable.Range(0,5).Where(i=>c.Shop.Offers[i]!=null&&c.Shop.Definition(c.Shop.Offers[i]).Cost<=availableBudget&&c.Shop.CanAfford(c.Shop.Definition(c.Shop.Offers[i]).Cost))
                    .Select(i=>new{Slot=i,Id=c.Shop.Offers[i],Score=PurchaseScore(c,c.Shop.Offers[i],stage)}).Where(o=>o.Score>0).OrderByDescending(o=>o.Score).ThenBy(o=>o.Slot).ToArray();
                bool bought=false;
                foreach(var offer in choices)
                {
                    SelectLineup(c,commands);int before=c.Shop.Gold;var receipt=c.Shop.Purchase(offer.Slot);if(!receipt.Success)continue;
                    budget-=receipt.Value;commands.Add(Command(c.Shop,"Purchase",before,offer.Id,offer.Slot,receipt.Instance));SelectLineup(c,commands);bought=true;break;
                }
                if(!bought)break;
            }
        }
        static void Commission(Context c,int stage,ref int budget,List<ShopCommand> commands)
        {
            if(c.Shop.CommissionUsed)return;
            int availableBudget=budget;
            var choices=c.Shop.Bench.Select(p=>p.Id).Distinct().Where(id=>c.Shop.Definition(id).Cost<=3&&c.Shop.Definition(id).Cost+2<=availableBudget&&c.Shop.Gold>=c.Shop.Definition(id).Cost+2)
                .Where(id=>c.Shop.Bench.Count(p=>p.Id==id&&p.Star==1)>=2)
                .OrderByDescending(id=>PurchaseScore(c,id,stage)).ThenBy(id=>id,StringComparer.Ordinal).ToArray();
            foreach(var id in choices)
            {
                int before=c.Shop.Gold;var receipt=c.Shop.Commission(id);if(!receipt.Success)continue;
                budget-=receipt.Value;commands.Add(Command(c.Shop,"Commission",before,id,instance:receipt.Instance));SelectLineup(c,commands);return;
            }
        }
        static float PurchaseScore(Context c,string id,int stage)
        {
            var owned=c.Shop.Bench.Where(p=>p.Id==id).ToArray();int cost=c.Shop.Definition(id).Cost;
            if(cost>c.MaximumCost)return -1;
            int desired=cost==1&&stage>=9?4:cost==1&&stage>=5?3:2;
            if(c.ReunionFocus)desired=id=="CARD_M02"||id=="CARD_M03"?1:stage>=5&&(id=="CARD_M07"||id=="CARD_M08")?3:2;
            if(c.HighCostTransition)desired=cost==1?3:2;
            if(owned.Any(p=>p.Star>=desired))return -1;
            var source=CampaignV2Balance.Build(c.Definitions[id],1,50,6);
            float score=Power(source)/Math.Max(1,cost);
            if(owned.Count(p=>p.Star==1)>=2)score+=100;
            else if(owned.Length>0)score+=25;
            if(c.Lineup.Contains(id))score+=15;
            if(c.ReunionFocus)score+=FocusedPurchaseBonus(c,id);
            if(c.HighCostTransition&&stage>=5)
            {
                // Buy real pairs toward two stars; avoid filling reserve with every boss kind.
                int highKinds=c.Shop.Bench.Where(p=>c.Shop.Definition(p.Id).Cost>=4).Select(p=>p.Id).Distinct().Count();
                if(cost>=4)
                {
                    if(owned.Length==0&&highKinds>=5)return -1;
                    score+=60+(owned.Length>0?50:0)+HighCostRoleBonus(id);
                }
                else score*=.65f;
            }
            if(cost==5&&stage<8&&!c.HighCostTransition)score*=.4f;
            if(owned.Length==0&&c.Shop.Bench.Select(p=>p.Id).Distinct().Count()>=c.Shop.Level+3)return -1;
            return score;
        }
        static float HighCostRoleBonus(string id)
        {
            switch(id)
            {
                case "CARD_TALULAH_TRIAL":return 25;
                case "CARD_M18":return 20;
                case "CARD_M25":return 18;
                case "CARD_M20":case "CARD_M14":case "CARD_M28":return 15;
                default:return 0;
            }
        }
        static bool FocusedFront(string id)=>id=="CARD_M07"||id=="CARD_M10"||id=="CARD_M18"||id=="CARD_M19";
        static bool FocusedRunner(string id)=>id=="CARD_M02"||id=="CARD_M03";
        static float FocusedPurchaseBonus(Context c,string id)
        {
            string[] owned=c.Shop.Bench.Select(p=>p.Id).Distinct().ToArray();bool fresh=!owned.Contains(id);
            if(!CampaignBalance.Reunion(id))return -45;
            float score=0;int target=c.Shop.Level>=6?6:4;
            if(fresh&&owned.Count(CampaignBalance.Reunion)<target)score+=85;
            if(fresh&&ReunionRules.TierFor(owned.Concat(new[]{id}))>ReunionRules.TierFor(owned))score+=40;
            if(!owned.Any(FocusedFront)&&FocusedFront(id))score+=45;
            if(!owned.Any(FocusedRunner)&&FocusedRunner(id))score+=35;
            if(!owned.Any(HasRanged)&&HasRanged(id))score+=40;
            if(!owned.Any(HasArts)&&HasArts(id))score+=45;
            if(c.Stage>=5&&CampaignBalance.Guerrilla(id)&&owned.Count(CampaignBalance.Guerrilla)<4)score+=30;
            if(id=="CARD_M07"||id=="CARD_M08")score+=20;
            return score;
            bool HasRanged(string member){var p=CampaignV2Balance.Build(c.Definitions[member],1,50,6);return p.Combat.CanAttack&&!p.Combat.Melee;}
            bool HasArts(string member){var p=CampaignV2Balance.Build(c.Definitions[member],1,50,6);return p.Combat.CanAttack&&p.Combat.Effects.DamageType==CombatDamageType.Arts;}
        }
        static float Power(BasicRosterProfile profile)
        {
            var u=profile.Unit;var c=profile.Combat;float score=u.Health*.014f+(c.CanAttack?c.UnitAttack/c.UnitInterval:0)*(c.Effects.DamageType==CombatDamageType.Arts?1.12f:1);
            if(c.Effects.TacticalCommander)score+=25;if(c.Effects.Healer)score+=30;if(c.Effects.Mandragora)score+=30;if(c.Effects.Shackler)score+=20;
            if(c.HasSkill)score+=c.SkillAttack*.035f;if(c.Effects.ArtsBarrier||c.Effects.Mudrock)score*=1.18f;
            return score;
        }
        static void SelectLineup(Context c,List<ShopCommand> commands)
        {
            var available=c.Shop.Bench.GroupBy(p=>p.Id).Select(g=>g.OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First()).ToList();
            var chosen=new List<ShopTrialModel.Piece>();
            while(chosen.Count<c.Shop.Level&&available.Count>0)
            {
                float current=Traits(chosen.Select(p=>p.Id));
                var next=available.OrderByDescending(p=>Power(CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6))+Traits(chosen.Select(o=>o.Id).Concat(new[]{p.Id}))-current+
                    (c.ReunionFocus?FocusedLineupBonus(c,chosen,p.Id):0)+(c.HighCostTransition&&c.Shop.Definition(p.Id).Cost>=4?(p.Star>=2?45:20):0))
                    .ThenBy(p=>p.Instance).First();chosen.Add(next);available.Remove(next);
            }
            if(!c.Shop.SetLineup(chosen.Select(p=>p.Id),chosen.Select(p=>p.Instance)))throw new InvalidOperationException("Actual shop rejected selected lineup/reserve capacity.");
            string[] ids=chosen.Select(p=>p.Id).ToArray();int[] instances=chosen.Select(p=>p.Instance).ToArray();
            var prior=commands.LastOrDefault(p=>p.kind=="SetLineup");
            if(prior==null||!prior.lineupInstances.SequenceEqual(instances))commands.Add(new ShopCommand{kind="SetLineup",lineupIds=ids,lineupInstances=instances,goldBefore=c.Shop.Gold,goldAfter=c.Shop.Gold,levelAfter=c.Shop.Level,xpAfter=c.Shop.XP});
            c.Lineup=ids.ToList();
        }
        static float FocusedLineupBonus(Context c,List<ShopTrialModel.Piece> chosen,string id)
        {
            string[] ids=chosen.Select(p=>p.Id).ToArray();float score=0;
            if(CampaignBalance.Reunion(id)&&ids.Count(CampaignBalance.Reunion)<(c.Shop.Level>=6?6:4))score+=60;
            if(ReunionRules.TierFor(ids.Concat(new[]{id}))>ReunionRules.TierFor(ids))score+=40;
            if(FocusedFront(id)&&!ids.Any(FocusedFront))score+=45;
            if(FocusedRunner(id)&&!ids.Any(FocusedRunner))score+=30;
            else if(FocusedRunner(id)&&ids.Any(FocusedRunner))score-=45;
            if(CampaignBalance.Guerrilla(id)&&ids.Count(CampaignBalance.Guerrilla)<4&&c.Stage>=5)score+=25;
            bool ranged=IsRanged(id);if(ranged&&!ids.Any(IsRanged))score+=40;
            if(IsArts(id)&&!ids.Any(IsArts))score+=65;
            return score;
            bool IsRanged(string member){var p=CampaignV2Balance.Build(c.Definitions[member],1,50,6);return p.Combat.CanAttack&&!p.Combat.Melee;}
            bool IsArts(string member){var p=CampaignV2Balance.Build(c.Definitions[member],1,50,6);return p.Combat.CanAttack&&p.Combat.Effects.DamageType==CombatDamageType.Arts;}
        }
        static float Traits(IEnumerable<string> ids)
        {
            string[] members=ids.Distinct().ToArray();
            var tags=members.SelectMany(CampaignBalance.Tags).GroupBy(t=>t).ToDictionary(g=>g.Key,g=>g.Count());float score=0;
            foreach(var pair in tags)
            {
                int count=pair.Value;if(count<2)continue;
                score+=pair.Key=="整合运动"?ReunionRules.TierFor(members)*6:
                    pair.Key=="萨卡兹"?count>=6?45:count>=4?30:18:count>=4?24:12;
            }
            return score;
        }
        // Replays the already-purchased run prefix, then searches preparation
        // alternatives without drawing another shop or manufacturing a result.
        public static void FocusAndContinue()
        {
            CampaignJourneyCatalog.Current.Validate();var definitions=Definitions();
            string input=Argument("-journeyFocusedTrace","artifacts/campaign-v2/model-search.json");
            int seed=int.Parse(Argument("-journeyFocusedSeed","73191"),CultureInfo.InvariantCulture);
            string name=Argument("-journeyFocusedPolicy","formation-reunion");
            int target=int.Parse(Argument("-journeyFocusedStage","9"),CultureInfo.InvariantCulture);
            int limit=Math.Max(1,int.Parse(Argument("-journeyFocusedLineups","24"),CultureInfo.InvariantCulture));
            int variants=Math.Max(1,Math.Min(72,int.Parse(Argument("-journeyFocusedVariants","72"),CultureInfo.InvariantCulture)));
            string output=Argument("-journeyFocusedOutput","artifacts/campaign-retune/focused-run.json");
            var source=JsonUtility.FromJson<Report>(File.ReadAllText(Path.GetFullPath(input)));
            var original=source.attempts.Single(a=>a.seed==seed&&a.economyPolicy==name);
            var result=new Attempt{seed=seed,economyPolicy=name,gift=original.gift,gifts=original.gifts};var rounds=new List<Round>();
            var report=new Report{version="actual-runtime-campaign-v2-twelve-stage-focused-4",createdUtc=DateTime.UtcNow.ToString("O"),roleOrder=definitions.Keys.ToArray(),observationSeconds=300};
            try
            {
                if(!source.roleOrder.SequenceEqual(report.roleOrder))throw new InvalidOperationException("Focused replay role order drift.");
                var policy=Policies.Single(p=>p.Name==name);
                var c=ReplayPreparationPrefix(original,target,definitions);
                c.ReunionFocus=policy.ReunionFocus;c.HighCostTransition=policy.HighCostTransition;c.MaximumCost=policy.MaximumCost;c.Stage=target;
                rounds.AddRange(original.rounds.Where(r=>r.stage<target));result.completedStages=target-1;
                var round=JsonUtility.FromJson<Round>(JsonUtility.ToJson(original.rounds.Single(r=>r.stage==target)));rounds.Add(round);
                var commands=round.shopCommands.ToList();var baseline=Selected(c).Select(p=>p.Copy()).ToArray();
                var purchases=new List<ShopTrialModel.Piece>();
                foreach(string id in Argument("-journeyFocusedBuyOffered","").Split(',').Where(id=>!string.IsNullOrWhiteSpace(id)))
                {
                    int slot=Enumerable.Range(0,c.Shop.Offers.Count).Where(i=>c.Shop.Offers[i]==id).DefaultIfEmpty(-1).First();
                    if(slot<0)throw new InvalidOperationException("Focused requested card is not in the existing shop: "+id);
                    int before=c.Shop.Gold;var receipt=c.Shop.Purchase(slot);
                    if(!receipt.Success)throw new InvalidOperationException("Focused offered purchase failed: "+receipt.Message);
                    commands.Add(Command(c.Shop,"Purchase",before,id,slot,receipt.Instance));purchases.Add(c.Shop.Bench.Single(p=>p.Instance==receipt.Instance));
                }
                if(purchases.Count>0)
                {
                    c.FocusPriority=purchases.Select(p=>p.Id).Distinct().ToArray();var preferred=new List<ShopTrialModel.Piece>(purchases);
                    void AddRole(Func<ShopTrialModel.Piece,bool> role)
                    {var candidate=baseline.Where(p=>!preferred.Any(q=>q.Id==p.Id)&&role(p)).OrderByDescending(p=>Power(CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6))).FirstOrDefault();if(candidate!=null)preferred.Add(candidate);}
                    AddRole(p=>CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6).Combat.Effects.DamageType==CombatDamageType.Arts);
                    AddRole(p=>{var combat=CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6).Combat;return combat.CanAttack&&!combat.Melee&&combat.Effects.DamageType==CombatDamageType.Physical;});
                    AddRole(p=>FocusedRunner(p.Id));
                    foreach(var piece in baseline.OrderByDescending(p=>Power(CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6))))if(!preferred.Any(p=>p.Id==piece.Id))preferred.Add(piece);
                    baseline=preferred.Take(c.Shop.Level).Select(p=>p.Copy()).ToArray();
                }
                var forms=Argument("-journeyFocusedForms",c.Form.ToString()).Split(',').Select(value=>(InfectionForm)Enum.Parse(typeof(InfectionForm),value)).ToArray();
                if(forms.Length==0||forms.Any(f=>!Enum.IsDefined(typeof(InfectionForm),f)))throw new ArgumentException("Invalid focused infection form.");
                ShopTrialModel.Piece[] bestPieces=baseline;Fight best=null;ManualBattle winning=null;int tried=0;
                InfectionForm bestForm=c.Form,bestSecond=c.Second;
                for(int branch=0;branch<2&&winning==null;branch++)
                {
                    ApplyFocusedLineup(c,baseline);
                    if(branch==1)
                    {
                        if(!bool.Parse(Argument("-journeyFocusedUpgrade","true"))||c.Shop.Level>=7||c.Shop.Gold<4)break;
                        while(c.Shop.Level<7&&c.Shop.Gold>=4)
                        {int before=c.Shop.Gold;var receipt=c.Shop.Upgrade();if(!receipt.Success)break;commands.Add(Command(c.Shop,"Upgrade",before));}
                    }
                    int lineupIndex=0;
                    foreach(var lineup in FocusedLineups(c,baseline,limit))
                    {
                        ApplyFocusedLineup(c,lineup);
                        bool infected=lineup.Count(p=>CampaignBalance.Infected(p.Id))>=2;
                        foreach(var form in infected?forms:forms.Take(1))
                        {
                            c.Form=form;c.Second=form==InfectionForm.Swarm?InfectionForm.Shell:InfectionForm.Swarm;
                            for(int variant=0;variant<variants;variant++)
                            {
                                var battle=CreateBattle(c,CampaignJourneyCatalog.Stage(target));var fight=FightBattle(c,battle,variant,300);tried++;
                                if(best==null||Better(fight,best)){best=fight;bestPieces=lineup.Select(p=>p.Copy()).ToArray();bestForm=c.Form;bestSecond=c.Second;}
                                if(!fight.won)continue;winning=battle;best=fight;bestPieces=lineup.Select(p=>p.Copy()).ToArray();bestForm=c.Form;bestSecond=c.Second;break;
                            }
                            if(winning!=null)break;
                        }
                        lineupIndex++;if(lineupIndex%4==0)Debug.Log("JOURNEY_FOCUSED_PROGRESS: stage="+target+" branch="+branch+" lineup="+lineupIndex+" tried="+tried+" bestDefense="+best?.remainingDefense+" bestHP="+best?.remainingDefenderHP);
                        if(winning!=null)break;
                    }
                }
                ApplyFocusedLineup(c,bestPieces);c.Form=bestForm;c.Second=bestSecond;var selected=Selected(c);
                var command=Command(c.Shop,"SetLineup",c.Shop.Gold);command.lineupIds=c.Lineup.ToArray();command.lineupInstances=selected.Select(p=>p.Instance).ToArray();commands.Add(command);
                if(c.Contract!=null)commands.Add(Command(c.Shop,"SetContract",c.Shop.Gold,c.Contract));
                var formCommand=Command(c.Shop,"SetForms",c.Shop.Gold);formCommand.primaryForm=c.Form.ToString();formCommand.secondaryForm=c.Second.ToString();commands.Add(formCommand);
                round.shopCommands=commands.ToArray();round.lineup=selected.Select(p=>new Piece{id=p.Id,instance=p.Instance,star=p.Star}).ToArray();round.contractCard=c.Contract;
                round.infectionPrimary=c.Form.ToString();round.infectionSecondary=c.Second.ToString();
                round.fight=best;round.deploymentPoliciesTried=tried;round.won=winning!=null;
                round.diagnosis="Replayed purchased prefix; counterfactual one/two-slot or star-choice preparation search. Optional named existing offers are purchased through actual transactions; no shop draw added. Final SetLineup/contract/forms recorded; only a winning branch earns a reward.";
                if(winning==null)
                {round.closingGold=c.Shop.Gold;round.closingLevel=c.Shop.Level;round.closingXP=c.Shop.XP;result.failedStage=target;}
                else
                {
                    if(!c.Run.PrepareBattle())throw new InvalidOperationException("Focused preparation was rejected.");
                    ObserveRound(c,round,winning);result.completedStages=target;
                    if(target<CampaignJourneyDefinition.StageCount&&!c.Run.OpenShop())throw new InvalidOperationException("Focused victory did not open the next free shop.");
                    ContinueRun(c,policy,target+1,rounds,result);
                    result.won=c.Run.Phase==BattleShopPhase.Complete;
                }
                c.Shop.Validate();
            }
            catch(Exception error){result.error=error.ToString();if(result.failedStage==0)result.failedStage=rounds.LastOrDefault()?.stage??target;}
            result.rounds=rounds.ToArray();report.attempts=new[]{result};report.status=result.won?"fixed-seed legal full-run witness found":"focused continuation incomplete; see failed stage and error";Save(output,report);
            Debug.Log("JOURNEY_FOCUSED_COMPLETE: seed="+seed+" completed="+result.completedStages+" failedAt="+result.failedStage+" won="+result.won+" error="+result.error+"; "+Path.GetFullPath(output));
        }
        [MenuItem("Yesterday Convoy/Campaign Journey/Audit Winning Timing Margins")]
        public static void TimingMargins()
        {
            CampaignJourneyCatalog.Current.Validate();var definitions=Definitions();
            string input=Argument("-journeyTimingTrace","artifacts/campaign-retune/focused-twelfth.json");
            string output=Path.GetFullPath(Argument("-journeyTimingOutput","artifacts/campaign-retune/timing-margins.json"));
            var source=JsonUtility.FromJson<Report>(File.ReadAllText(Path.GetFullPath(input)));
            var original=source.attempts.Single(a=>a.seed==73191&&a.won&&a.completedStages==CampaignJourneyDefinition.StageCount);
            var report=new TimingReport{createdUtc=DateTime.UtcNow.ToString("O"),sourceTrace=Path.GetFullPath(input),seed=original.seed,status="in progress"};
            var cases=new List<TimingCase>();var skipped=new List<string>();
            try
            {
                if(!source.roleOrder.SequenceEqual(definitions.Keys))throw new InvalidOperationException("Timing replay role order drift.");
                foreach(int number in new[]{9,CampaignJourneyDefinition.StageCount})
                {
                    var round=original.rounds.Single(r=>r.stage==number);var stage=CampaignJourneyCatalog.Stage(number);
                    var context=ReplayPreparationPrefix(original,number,definitions);
                    if(context.Run.CanShop&&!context.Run.PrepareBattle())throw new InvalidOperationException("Timing preparation rejected.");
                    var baseline=ReplayFight(context,stage,round.fight);
                    cases.Add(new TimingCase{stage=number,strictBaseline=true,allActionsAccepted=true,sameWaveAndLane=true,
                        accepted=round.fight.actions.Length,lineup=round.lineup,infectionPrimary=round.infectionPrimary,infectionSecondary=round.infectionSecondary,contractCard=round.contractCard,
                        fight=Describe(baseline,"strict original-trace timing baseline",round.fight.actions.ToList(),false),receipts=Array.Empty<TimingReceipt>()});
                    double start=0;
                    for(int wave=0;wave<baseline.WaveCount;wave++)
                    {
                        double end=start+baseline.WavePlan.Waves[wave].DeploymentSeconds;
                        var group=round.fight.actions.Select((action,index)=>new{action,index})
                            .Where(a=>a.action.kind=="Deploy"&&a.action.at>=start-.0000001&&a.action.at<end-.0000001)
                            .GroupBy(a=>a.action.at).OrderByDescending(g=>g.Key).FirstOrDefault();
                        if(group==null||group.Key<=start+.0000001)
                            skipped.Add("stage="+number+" wave="+(wave+1)+": "+(group==null?"no Deploy group":"final Deploy group is the wave opener; no earlier legal time"));
                        else foreach(double advance in new[]{.5,1.0})
                        {
                            var probe=ReplayTimingFight(context,stage,round.fight,group.Select(a=>a.index).ToArray(),advance);
                            probe.originalWave=wave+1;probe.originalGroupAt=group.Key;probe.waveEnd=end;probe.originalMargin=end-group.Key;
                            probe.lineup=round.lineup;probe.infectionPrimary=round.infectionPrimary;probe.infectionSecondary=round.infectionSecondary;probe.contractCard=round.contractCard;
                            cases.Add(probe);
                        }
                        start=end;
                    }
                    context.Shop.Validate();
                }
                report.status="completed";
            }
            catch(Exception error){report.status="failed";report.error=error.ToString();throw;}
            finally
            {
                report.cases=cases.ToArray();report.skippedGroups=skipped.ToArray();Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output,JsonUtility.ToJson(report,true));
            }
            Debug.Log("JOURNEY_TIMING_MARGINS_COMPLETE: "+cases.Count+" cases; "+output);
        }
        static int TimingWaveAt(BattleWavePlan plan,double at)
        {
            double end=0;for(int i=0;i<plan.Waves.Count;i++){end+=plan.Waves[i].DeploymentSeconds;if(at<end-.0000001)return i+1;}
            return -1;
        }
        static TimingCase ReplayTimingFight(Context c,CampaignJourneyStage stage,Fight source,int[] shifted,double advance)
        {
            var battle=CreateBattle(c,stage);var entities=new Dictionary<string,string>();var shiftedSet=new HashSet<int>(shifted);
            var receipts=source.actions.Select((action,index)=>new TimingReceipt{sourceIndex=index,kind=action.kind,cardId=action.cardId,routeId=action.routeId,
                entityId=action.entityId,request=action.request,originalAt=action.at,scheduledAt=action.at-(shiftedSet.Contains(index)?advance:0),originalWave=TimingWaveAt(battle.WavePlan,action.at)}).ToArray();
            foreach(var receipt in receipts.Where(r=>r.scheduledAt<0))receipt.failure="ScheduledBeforeBattleBegin";
            var schedule=receipts.Where(r=>r.scheduledAt>=0).OrderBy(r=>r.scheduledAt).ThenBy(r=>r.sourceIndex).ToArray();
            var acceptedActions=new List<BattleAction>();int cursor=0;battle.Begin();
            while(battle.Phase==BattlePhase.Running&&battle.Clock<300)
            {
                while(cursor<schedule.Length&&schedule[cursor].scheduledAt<=battle.Clock+.00001)
                {
                    var r=schedule[cursor++];r.executedAt=battle.Clock;r.actualWave=battle.FinalAdvanceActive?-1:battle.CurrentWaveIndex+1;r.currentLane=battle.CurrentWaveLaneId;
                    r.sameWaveAndLane=r.actualWave==r.originalWave;
                    if(r.kind=="Deploy"||r.kind=="Golem")
                    {
                        var lane=battle.Lanes.SingleOrDefault(l=>l.Id==r.routeId);r.sameWaveAndLane&=r.currentLane==r.routeId;
                        if(lane==null)r.failure="UnknownRecordedLane";
                        else
                        {
                            var receipt=r.kind=="Deploy"?battle.TryDeployRole(battle.RunId,r.request,lane.Entry,r.cardId):battle.TryDeployGolem(battle.RunId,r.request,lane.Entry);
                            r.accepted=receipt.Success;r.failure=receipt.Failure.ToString();if(r.accepted)entities[r.entityId]=receipt.EntityId;
                        }
                    }
                    else if(r.kind=="Skill")
                    {
                        var failure=entities.TryGetValue(r.entityId,out string entity)?battle.TryActivateSkill(battle.RunId,r.request,entity):SkillFailure.MissingEntity;
                        r.accepted=failure==SkillFailure.None;r.failure=failure.ToString();
                    }
                    else if(r.kind=="DeepDetonate")
                    {r.accepted=battle.TryDetonateDeep(battle.RunId,r.request);r.failure=r.accepted?"None":"TryDetonateDeepRejected";}
                    else throw new InvalidOperationException("Unknown timing battle action: "+r.kind);
                    if(r.accepted)acceptedActions.Add(new BattleAction{kind=r.kind,cardId=r.cardId,routeId=r.routeId,entityId=r.entityId,request=r.request,at=battle.Clock});
                }
                battle.Advance(.05f);
            }
            foreach(var receipt in schedule.Skip(cursor))receipt.failure=battle.Phase==BattlePhase.Running?"ObservationBoundReachedBeforeAction":"BattleEndedBeforeScheduledAction";
            return new TimingCase{stage=stage.stage,advanceSeconds=advance,accepted=receipts.Count(r=>r.accepted),rejected=receipts.Count(r=>!r.accepted),
                allActionsAccepted=receipts.All(r=>r.accepted),sameWaveAndLane=receipts.Where(r=>r.kind=="Deploy"||r.kind=="Golem").All(r=>r.sameWaveAndLane),
                fight=Describe(battle,"fixed-trace earlier-group timing probe",acceptedActions,battle.Phase==BattlePhase.Running),receipts=receipts};
        }
        static Context ReplayPreparationPrefix(Attempt source,int target,Dictionary<string,ManualBattleDefinition> definitions)
        {
            var shop=new ShopTrialModel(definitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),source.seed);shop.EnableJourneyEconomy();
            var gifts=CampaignJourneyEconomy.StartingCards(shop.Roles,source.seed);string gift=gifts[0];
            if(gift!=source.gift||source.gifts==null||!gifts.SequenceEqual(source.gifts))throw new InvalidOperationException("Focused replay starting gifts drift; a current twelve-stage trace is required.");
            shop.Preset(gifts.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),1,0,CampaignJourneyEconomy.InitialGold);
            var c=new Context{Seed=source.seed,Shop=shop,Run=new BattleShopRun(shop,source.seed,campaign:true,journeyV2:true),Definitions=definitions,Gift=gift};c.Lineup.Add(gift);
            c.Run.BeginJourneyPreparation();
            foreach(var round in source.rounds.Where(r=>r.stage<=target))
            {
                c.Stage=round.stage;
                if(shop.Gold!=round.openingGold||shop.Level!=round.openingLevel||!shop.Offers.Select(id=>id??"").SequenceEqual(round.initialOffers.Select(id=>id??"")))throw new InvalidOperationException("Focused opening ledger or offers drift at "+round.stage);
                foreach(var cmd in round.shopCommands)
                {
                    if(shop.Gold!=cmd.goldBefore)throw new InvalidOperationException("Focused command opening cash drift.");
                    ShopTrialModel.Result receipt=null;
                    switch(cmd.kind)
                    {
                        case "Upgrade":receipt=shop.Upgrade();break;
                        case "Refresh":receipt=shop.Refresh();break;
                        case "Purchase":if(shop.Offers[cmd.slot]!=cmd.id)throw new InvalidOperationException("Focused purchased offer drift.");receipt=shop.Purchase(cmd.slot);break;
                        case "Commission":receipt=shop.Commission(cmd.id);break;
                        case "SetLineup":if(!shop.SetLineup(cmd.lineupIds,cmd.lineupInstances))throw new InvalidOperationException("Focused source lineup rejected.");c.Lineup=cmd.lineupIds.ToList();break;
                        case "SetContract":c.Contract=cmd.id;break;
                        case "SetForms":
                            if(!Enum.TryParse(cmd.primaryForm,out InfectionForm primary)||!Enum.TryParse(cmd.secondaryForm,out InfectionForm secondary)||
                                !Enum.IsDefined(typeof(InfectionForm),primary)||!Enum.IsDefined(typeof(InfectionForm),secondary)||primary==secondary)
                                throw new InvalidOperationException("Focused source form choice is invalid or not normalized.");
                            c.Form=primary;c.Second=secondary;break;
                        default:throw new InvalidOperationException("Unknown focused source command: "+cmd.kind);
                    }
                    if(receipt!=null&&(!receipt.Success||(cmd.kind=="Purchase"||cmd.kind=="Commission")&&receipt.Instance!=cmd.instance))throw new InvalidOperationException("Focused source transaction drift: "+cmd.kind);
                    if(shop.Gold!=cmd.goldAfter||shop.Level!=cmd.levelAfter||shop.XP!=cmd.xpAfter)throw new InvalidOperationException("Focused source transaction ledger drift.");
                }
                if(!Selected(c).Select(p=>p.Id+":"+p.Instance+":"+p.Star).SequenceEqual(round.lineup.Select(p=>p.id+":"+p.instance+":"+p.star)))throw new InvalidOperationException("Focused source star or seat drift.");
                c.Contract=string.IsNullOrEmpty(round.contractCard)?null:round.contractCard;
                if(c.Form.ToString()!=round.infectionPrimary||c.Second.ToString()!=round.infectionSecondary)
                    throw new InvalidOperationException("Focused source forms lack matching legal command history at "+round.stage);
                if(round.stage==target)return c;
                if(c.Run.CanShop&&!c.Run.PrepareBattle())throw new InvalidOperationException("Focused prefix preparation rejected.");
                var battle=ReplayFight(c,CampaignJourneyCatalog.Stage(round.stage),round.fight);
                if(battle.Phase!=BattlePhase.Won||!round.won)throw new InvalidOperationException("Focused prefix did not naturally win stage "+round.stage);
                if(!c.Run.Observe(battle.Phase,battle.GolemGrowthEarned,golemCapacity:battle.GolemCapacity,remainingDefense:battle.Defense,initialDefense:battle.Settings.InitialDefense))throw new InvalidOperationException("Focused prefix settlement rejected.");
                if(shop.Gold!=round.closingGold||shop.Level!=round.closingLevel||shop.XP!=round.closingXP||c.Run.GolemExperience!=round.golemXP)throw new InvalidOperationException("Focused prefix reward or golem XP drift.");
                if(!c.Run.OpenShop())throw new InvalidOperationException("Focused prefix free shop rejected.");
            }
            throw new InvalidOperationException("Focused target stage is absent from the source trace.");
        }
        static ManualBattle ReplayFight(Context c,CampaignJourneyStage stage,Fight source)
        {
            var battle=CreateBattle(c,stage);var map=new Dictionary<string,string>();int index=0;battle.Begin();
            while(battle.Phase==BattlePhase.Running&&battle.Clock<300)
            {
                while(index<source.actions.Length&&source.actions[index].at<=battle.Clock+.00001)
                {
                    var action=source.actions[index++];
                    if(action.kind=="Deploy"||action.kind=="Golem")
                    {
                        var lane=battle.Lanes.Single(l=>l.Id==action.routeId);
                        var receipt=action.kind=="Deploy"?battle.TryDeployRole(battle.RunId,action.request,lane.Entry,action.cardId):battle.TryDeployGolem(battle.RunId,action.request,lane.Entry);
                        if(!receipt.Success)throw new InvalidOperationException("Focused prefix deployment rejected at "+battle.Clock);map[action.entityId]=receipt.EntityId;
                    }
                    else if(action.kind=="Skill")
                    {if(!map.TryGetValue(action.entityId,out string entity)||battle.TryActivateSkill(battle.RunId,action.request,entity)!=SkillFailure.None)throw new InvalidOperationException("Focused prefix skill rejected.");}
                    else if(action.kind=="DeepDetonate")
                    {if(!battle.TryDetonateDeep(battle.RunId,action.request))throw new InvalidOperationException("Focused prefix detonation rejected.");}
                    else throw new InvalidOperationException("Unknown focused prefix battle action.");
                }
                battle.Advance(.05f);
            }
            if(index!=source.actions.Length)throw new InvalidOperationException("Focused prefix ended before all recorded actions.");
            if(battle.Phase.ToString()!=source.phase||Math.Abs(battle.Clock-source.seconds)>.001||battle.DP!=source.dp||battle.Defense!=source.remainingDefense||
                battle.Breaches!=source.breaches||battle.ApprovedPortalTrips!=source.portalTrips||
                Math.Abs(battle.TotalPlayerDamage-source.damage)>Math.Max(.05,Math.Abs(source.damage)*.00001))
                throw new InvalidOperationException("Focused prefix combat terminal drift: stage="+stage.stage+" clock="+battle.Clock+" expected="+source.seconds+" damage="+battle.TotalPlayerDamage+" expected="+source.damage);
            return battle;
        }
        static void ApplyFocusedLineup(Context c,ShopTrialModel.Piece[] pieces)
        {
            if(!c.Shop.SetLineup(pieces.Select(p=>p.Id),pieces.Select(p=>p.Instance)))throw new InvalidOperationException("Focused lineup is not legal for this inventory/level.");
            c.Lineup=pieces.Select(p=>p.Id).ToList();c.Contract=pieces.Where(p=>CampaignBalance.Tags(p.Id).Contains("萨卡兹")).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).FirstOrDefault()?.Id;
        }
        static IEnumerable<ShopTrialModel.Piece[]> FocusedLineups(Context c,ShopTrialModel.Piece[] baseline,int limit)
        {
            var result=new List<ShopTrialModel.Piece[]>();var keys=new HashSet<string>();
            var alternatives=c.Shop.Bench.GroupBy(p=>p.Id+":"+p.Star).Select(g=>g.OrderBy(p=>p.Instance).First()).ToArray();
            void Add(IEnumerable<ShopTrialModel.Piece> candidate)
            {
                var p=candidate.ToArray();if(p.Length>c.Shop.Level||p.Select(v=>v.Id).Distinct().Count()!=p.Length||c.Shop.Bench.Count-p.Length>10)return;
                string key=string.Join("|",p.OrderBy(v=>v.Id,StringComparer.Ordinal).Select(v=>v.Id+":"+v.Star));if(keys.Add(key))result.Add(p);
            }
            Add(baseline);
            foreach(var added in alternatives)Add(baseline.Concat(new[]{added}));
            for(int i=0;i<baseline.Length;i++)foreach(var replacement in alternatives){var p=(ShopTrialModel.Piece[])baseline.Clone();p[i]=replacement;Add(p);}
            for(int i=0;i<baseline.Length;i++)for(int j=i+1;j<baseline.Length;j++)foreach(var a in alternatives)foreach(var b in alternatives)
            {var p=(ShopTrialModel.Piece[])baseline.Clone();p[i]=a;p[j]=b;Add(p);}
            float Score(ShopTrialModel.Piece[] p)=>Traits(p.Select(v=>v.Id))*3+p.Sum(v=>Power(CampaignV2Balance.Build(c.Definitions[v.Id],v.Star,50,6)))+p.Count(v=>c.FocusPriority.Contains(v.Id))*80;
            return result.Take(1).Concat(result.Skip(1).OrderByDescending(Score)).Take(limit).ToArray();
        }
        static void ObserveRound(Context c,Round round,ManualBattle battle)
        {
            if(!c.Run.Observe(battle.Phase,battle.GolemGrowthEarned,golemCapacity:battle.GolemCapacity,remainingDefense:battle.Defense,initialDefense:battle.Settings.InitialDefense))throw new InvalidOperationException("Natural continuation settlement rejected.");
            round.baseReward=c.Run.Reward.Base;round.interest=c.Run.Reward.Interest;round.streakReward=c.Run.Reward.Streak;round.experienceReward=c.Run.Reward.Experience;
            round.closingGold=c.Shop.Gold;round.closingLevel=c.Shop.Level;round.closingXP=c.Shop.XP;round.golemXP=c.Run.GolemExperience;
        }
        static void ContinueRun(Context c,EconomyPolicy policy,int start,List<Round> rounds,Attempt result)
        {
            for(int number=start;number<=CampaignJourneyDefinition.StageCount;number++)
            {
                c.Stage=number;var stage=CampaignJourneyCatalog.Stage(number);var savedMap=stage.map??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=number&&s.map!=null).map;
                var round=new Round{stage=number,supply=stage.IsSupply,openingGold=c.Shop.Gold,openingLevel=c.Shop.Level,initialOffers=c.Shop.Offers.ToArray(),sceneName=savedMap.sceneName,mapId=savedMap.id,encounterId=stage.encounter==null?"supply":stage.encounter.name,
                    infectionPrimary=c.Form.ToString(),infectionSecondary=c.Second.ToString()};rounds.Add(round);
                var commands=new List<ShopCommand>();Prepare(c,policy,number,commands);SelectLineup(c,commands);
                c.Contract=Selected(c).Where(p=>CampaignBalance.Tags(p.Id).Contains("萨卡兹")).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).FirstOrDefault()?.Id;
                if(c.Contract!=null)commands.Add(Command(c.Shop,"SetContract",c.Shop.Gold,c.Contract));round.contractCard=c.Contract;
                round.lineup=Selected(c).Select(p=>new Piece{id=p.Id,instance=p.Instance,star=p.Star}).ToArray();round.shopCommands=commands.ToArray();
                if(!c.Run.PrepareBattle())throw new InvalidOperationException("Natural continuation preparation rejected.");
                ManualBattle winner=null;Fight best=null;int tried=0;
                if(stage.IsSupply){winner=CreateBattle(c,stage);winner.Begin();best=Describe(winner,"guaranteed supply",new List<BattleAction>(),false);tried=1;}
                else for(int variant=0;variant<42;variant++)
                {var battle=CreateBattle(c,stage);var fight=FightBattle(c,battle,variant,300);tried++;if(best==null||Better(fight,best))best=fight;if(fight.won){winner=battle;best=fight;break;}}
                round.fight=best;round.deploymentPoliciesTried=tried;round.won=winner!=null;
                if(winner==null){round.closingGold=c.Shop.Gold;round.closingLevel=c.Shop.Level;round.closingXP=c.Shop.XP;result.failedStage=number;break;}
                ObserveRound(c,round,winner);result.completedStages=number;if(number<CampaignJourneyDefinition.StageCount&&!c.Run.OpenShop())throw new InvalidOperationException("Natural continuation free shop rejected.");
            }
        }
        static ManualBattle CreateBattle(Context c,CampaignJourneyStage stage)
        {
            var map=stage.map??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=stage.stage&&s.map!=null).map;
            var encounter=stage.encounter??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=stage.stage&&s.encounter!=null).encounter;
            var wavePlan=stage.IsSupply?null:stage.encounter.WaveSnapshot();
            int initialDP=wavePlan?.Waves[0].DP??stage.dp;
            var selected=Selected(c);var profiles=selected.Select(p=>CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,initialDP,Math.Max(1,stage.defense),waveMode:wavePlan!=null)).ToArray();
            var first=profiles[0];var battle=new ManualBattle(first.Unit,encounter.route,Vector3.zero,first.Combat,roster:stage.IsSupply?Array.Empty<DefenderSettings>():stage.encounter.Snapshot(),
                deploymentLanes:encounter.LaneSnapshot(),allowEmptyRoster:stage.IsSupply);
            battle.ConfigureCardLoadout(profiles.Skip(1));battle.ConfigureCampaignV2(stage.stage);
            battle.ConfigureSynergies(selected.Select(p=>p.Copy()),form:c.Form,second:c.Second,golemExperience:c.Run.GolemExperience,contract:c.Contract);
            battle.ConfigureMarineTerrain(map.WalkablePoints(),map.MarineOrigin);battle.ConfigureGramophones(Array.Empty<GramophoneSettings>());
            if(stage.IsSupply)battle.ConfigureSupply(true);
            foreach(var d in battle.Defenders)battle.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
            battle.ConfigureApprovedMapEffects(map.mechanics);
            if(wavePlan!=null)battle.ConfigureWaves(wavePlan);
            return battle;
        }
        static Fight FightBattle(Context c,ManualBattle battle,int variant,float observation)
        {
            int style=variant%6,delayMode=variant/6;
            var selected=Selected(c);var profiles=selected.ToDictionary(p=>p.Id,p=>CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,battle.DP,battle.Defense,waveMode:battle.WavesEnabled));
            string[] order=selected.OrderByDescending(p=>style==1?profiles[p.Id].Unit.Speed/Math.Max(1,profiles[p.Id].Unit.DeploymentCost):
                style==2?Power(profiles[p.Id])/Math.Max(1,profiles[p.Id].Unit.DeploymentCost):style==4?profiles[p.Id].Unit.DeploymentCost:profiles[p.Id].Unit.Health/Math.Max(1,profiles[p.Id].Unit.DeploymentCost))
                .ThenBy(p=>p.Instance).Select(p=>p.Id).ToArray();
            string runner=c.ReunionFocus&&style==5?selected.Where(p=>p.Star==1&&(FocusedRunner(p.Id)||p.Id=="CARD_M15"))
                .OrderByDescending(p=>profiles[p.Id].Unit.Speed).ThenBy(p=>p.Instance).FirstOrDefault()?.Id:null;
            var runnerWaves=new HashSet<int>();
            long request=0;double nextDeploy=0;int wave=-1;var actions=new List<BattleAction>();battle.Begin();
            while(battle.Phase==BattlePhase.Running&&battle.Clock<observation)
            {
                if(wave!=battle.CurrentWaveIndex){wave=battle.CurrentWaveIndex;nextDeploy=battle.Clock+WaveOpeningDelay(delayMode,wave,battle);}
                var lane=battle.Lanes.FirstOrDefault(l=>l.Id==battle.CurrentWaveLaneId);
                if(lane!=null&&battle.DeploymentWindowOpen&&battle.Clock+1e-8>=nextDeploy)
                {
                    var waveOrder=style==5&&wave%2==1?order.Reverse():order.AsEnumerable();
                    foreach(string id in waveOrder)
                    {
                        bool reserveRunner=runner!=null&&!runnerWaves.Contains(wave);
                        if(reserveRunner&&id==runner&&battle.Defenders.Any(d=>d.Alive)&&battle.WaveRemaining>10)continue;
                        if(reserveRunner&&id!=runner&&battle.DP-profiles[id].Unit.DeploymentCost<profiles[runner].Unit.DeploymentCost)continue;
                        if(battle.CheckCardDeployment(id,lane.Entry)!=DeployFailure.None)continue;
                        long command=++request;var receipt=battle.TryDeployRole(battle.RunId,command,lane.Entry,id);if(!receipt.Success)continue;
                        actions.Add(new BattleAction{kind="Deploy",cardId=id,routeId=lane.Id,entityId=receipt.EntityId,request=command,at=battle.Clock});
                        if(id==runner)runnerWaves.Add(wave);
                        if(style==3){nextDeploy=battle.Clock+1.2;break;}
                    }
                    if(battle.MudrockTier>=2&&!battle.GolemDeployed&&battle.DP>=battle.GolemCost)
                    {
                        long command=++request;var receipt=battle.TryDeployGolem(battle.RunId,command,lane.Entry);
                        if(receipt.Success)actions.Add(new BattleAction{kind="Golem",routeId=lane.Id,entityId=receipt.EntityId,request=command,at=battle.Clock});
                    }
                }
                foreach(var u in battle.Units.ToArray())if(u.Active&&battle.CheckSkill(u.EntityId)==SkillFailure.None)
                {
                    long command=++request;if(battle.TryActivateSkill(battle.RunId,command,u.EntityId)==SkillFailure.None)
                        actions.Add(new BattleAction{kind="Skill",cardId=u.CardId,entityId=u.EntityId,request=command,at=battle.Clock});
                }
                if(battle.DeepTier>=4){long command=++request;if(battle.TryDetonateDeep(battle.RunId,command))actions.Add(new BattleAction{kind="DeepDetonate",request=command,at=battle.Clock});}
                battle.Advance(.05f);
            }
            string delays=string.Join(",",Enumerable.Range(0,battle.WaveCount).Select(i=>WaveOpeningDelay(delayMode,i,battle).ToString(CultureInfo.InvariantCulture)));
            return Describe(battle,"current-wave-lane;style="+style+";delayMode="+delayMode+";waveDelays="+delays+(runner==null?"":";reserveRunner="+runner),actions,battle.Phase==BattlePhase.Running);
        }
        static double WaveOpeningDelay(int mode,int wave,ManualBattle battle)
        {
            switch(mode)
            {
                case 0:return 0;
                case 1:return 4;
                case 2:return 8;
                case 3:return wave==0?4:0;
                case 4:return wave==0?8:0;
                case 5:return wave==1?4:0;
                case 6:return wave==1?8:0;
                // Focused search only: keep the normal 42-policy matrix unchanged.
                case 7:return wave==battle.WaveCount-1?Math.Max(0,battle.WavePlan.Waves[wave].DeploymentSeconds-.1):0;
                case 8:return wave==1?Math.Max(0,battle.WavePlan.Waves[wave].DeploymentSeconds-.1):0;
                case 9:return wave==0?Math.Max(0,battle.WavePlan.Waves[wave].DeploymentSeconds-.1):0;
                case 10:return wave==battle.WaveCount-1?Math.Max(0,battle.WavePlan.Waves[wave].DeploymentSeconds-6):0;
                case 11:return wave==battle.WaveCount-1?Math.Max(0,battle.WavePlan.Waves[wave].DeploymentSeconds-16):0;
                default:throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
        static Fight Describe(ManualBattle b,string policy,List<BattleAction> actions,bool observedTimeout)=>new Fight{
            policy=policy,phase=b.Phase.ToString(),won=b.Phase==BattlePhase.Won,timedObservation=observedTimeout,seconds=b.Clock,remainingDefense=b.Defense,dp=b.DP,breaches=b.Breaches,
            deployments=b.Units.Count(u=>!u.SynergySupport),damage=b.TotalPlayerDamage,remainingDefenderHP=b.Defenders.Where(d=>d.Alive).Sum(d=>d.Health),
            survivingDefenders=b.Defenders.Where(d=>d.Alive).Select(d=>d.EntityId).ToArray(),portalTrips=b.ApprovedPortalTrips,heatDamage=b.ApprovedHeatDamageToUnits,
            defenderHealing=b.Defenders.Sum(d=>d.SkillHealthRestored),actions=actions.ToArray()};
    }
}
