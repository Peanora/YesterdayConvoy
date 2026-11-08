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
            public string kind,id;public int slot=-1,instance,goldBefore,goldAfter,levelAfter,xpAfter;
            public int[] lineupInstances;public string[] lineupIds;
        }
        [Serializable] public sealed class BattleAction
        {public string kind,cardId,routeId,entityId;public long request;public double at;}
        [Serializable] public sealed class Piece
        {public string id;public int instance,star;}
        [Serializable] public sealed class Fight
        {
            public string policy,phase;public bool won,timedObservation;public double seconds;public int remainingDefense,dp,breaches,deployments,portalTrips;
            public float damage,remainingDefenderHP,heatDamage;public string[] survivingDefenders;public BattleAction[] actions;
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
        {public int seed,completedStages,failedStage;public string economyPolicy,gift,error;public bool won;public Round[] rounds;}
        [Serializable] public sealed class Report
        {
            public string version="actual-runtime-campaign-v2-search-1",status,createdUtc;
            public string[] roleOrder;public float observationSeconds;public Attempt[] attempts;
            public string evidenceBoundary="Real shop/inventory/XP/ManualBattle and persisted campaign encounters. Counterfactual deployment branches are search, not gameplay retries. A fixed-seed witness is not a player win rate or a balance guarantee. No state injection, free rerolls, unlimited gold, forced victory, or gameplay timeout.";
        }
        sealed class EconomyPolicy
        {internal string Name;internal int[] Levels,Cards,Refresh;}
        sealed class Context
        {
            internal int Seed;internal ShopTrialModel Shop;internal BattleShopRun Run;
            internal Dictionary<string,ManualBattleDefinition> Definitions;internal List<string> Lineup=new List<string>();
            internal string Contract;
        }
        static readonly EconomyPolicy[] Policies={
            new EconomyPolicy{Name="balanced",Levels=new[]{1,2,3,4,5,6,7,7,8,8,8,9,9},Cards=new[]{0,2,3,5,6,8,8,10,10,12,14,16,20},Refresh=new[]{0,0,2,2,2,4,4,4,4,4,4,6,6}},
            new EconomyPolicy{Name="low-cost-stars",Levels=new[]{1,2,3,4,5,6,6,6,7,7,8,8,8},Cards=new[]{0,3,4,6,8,10,10,12,12,12,14,14,16},Refresh=new[]{0,2,4,4,6,8,8,8,6,6,4,4,4}},
            new EconomyPolicy{Name="level-transition",Levels=new[]{1,2,3,4,5,6,7,8,8,9,9,9,10},Cards=new[]{0,2,3,4,4,6,6,8,8,10,10,12,16},Refresh=new[]{0,0,0,0,0,0,2,2,2,4,4,4,4}}
        };

        [MenuItem("Yesterday Convoy/Campaign Journey/Search Natural V2 Runs")]
        public static void Search()
        {
            CampaignJourneyCatalog.Current.Validate();
            int[] seeds=Argument("-journeySearchSeeds","73191,20261108,31016").Split(',').Select(int.Parse).ToArray();
            int limit=int.Parse(Argument("-journeySearchLimit","9"),CultureInfo.InvariantCulture);
            int variants=int.Parse(Argument("-journeyDeploymentVariants","18"),CultureInfo.InvariantCulture);
            float observation=float.Parse(Argument("-journeyObservationSeconds","300"),CultureInfo.InvariantCulture);
            string output=Argument("-journeySearchOutput","artifacts/campaign-v2/model-search.json");
            var definitions=Definitions();var attempts=new List<Attempt>();
            var report=new Report{createdUtc=DateTime.UtcNow.ToString("O"),observationSeconds=observation,roleOrder=definitions.Keys.ToArray()};
            foreach(int seed in seeds)
            {
                foreach(var policy in Policies)
                {
                    if(attempts.Count>=limit||attempts.Any(a=>a.won))break;
                    var attempt=Simulate(seed,policy,definitions,observation,variants);attempts.Add(attempt);
                    report.attempts=attempts.ToArray();report.status=attempts.Any(a=>a.won)?"fixed-seed legal full-run witness found":"search in progress; no full witness yet";
                    Save(output,report);
                    Debug.Log("JOURNEY_NATURAL_SEARCH: seed="+seed+" policy="+policy.Name+" completed="+attempt.completedStages+" won="+attempt.won+" failedAt="+attempt.failedStage+" error="+attempt.error);
                }
                if(attempts.Count>=limit||attempts.Any(a=>a.won))break;
            }
            report.attempts=attempts.ToArray();report.status=attempts.Any(a=>a.won)?"fixed-seed legal full-run witness found":"no full-run witness within this bounded search";Save(output,report);
            Debug.Log("JOURNEY_NATURAL_SEARCH_COMPLETE: "+report.status+"; "+Path.GetFullPath(output));
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
                if(data==null||data.Card==null||!data.Card.HasFrameAnimation||!CampaignBalance.Supports(data.Card.CardId))continue;
                if(!result.ContainsKey(data.Card.CardId))result.Add(data.Card.CardId,data);
            }
            if(result.Count!=43)throw new InvalidOperationException("Natural simulation needs the same 43 playable card definitions: "+result.Count);
            return result;
        }
        static Attempt Simulate(int seed,EconomyPolicy policy,Dictionary<string,ManualBattleDefinition> definitions,float observation,int variants)
        {
            var result=new Attempt{seed=seed,economyPolicy=policy.Name};var rounds=new List<Round>();
            try
            {
                var shop=new ShopTrialModel(definitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
                shop.EnableCampaign(false);var low=shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ToArray();
                string gift=low[new System.Random(seed).Next(low.Length)].Id;result.gift=gift;
                shop.Preset(new[]{new ShopTrialModel.Piece{Id=gift,Star=1}},Array.Empty<string>(),1,0,10);
                var context=new Context{Seed=seed,Shop=shop,Run=new BattleShopRun(shop,seed,campaign:true,journeyV2:true),Definitions=definitions};context.Lineup.Add(gift);
                for(int number=1;number<=13;number++)
                {
                    var stage=CampaignJourneyCatalog.Stage(number);
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
                        int count=Math.Min(variants,stage.encounter.lanes.Length*6);
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
                    if(!context.Run.Observe(winning.Phase,winning.GolemGrowthEarned,golemCapacity:winning.GolemCapacity))throw new InvalidOperationException("Actual run rejected its natural result.");
                    round.baseReward=context.Run.Reward.Base;round.interest=context.Run.Reward.Interest;round.streakReward=context.Run.Reward.Streak;round.experienceReward=context.Run.Reward.Experience;
                    round.closingGold=shop.Gold;round.closingLevel=shop.Level;round.closingXP=shop.XP;round.golemXP=context.Run.GolemExperience;
                    result.completedStages=number;
                    if(number<13&&!context.Run.OpenShop())throw new InvalidOperationException("Actual run rejected the next free shop.");
                }
                result.won=context.Run.Phase==BattleShopPhase.Complete;shop.Validate();
            }
            catch(Exception error){result.error=error.ToString();if(result.failedStage==0)result.failedStage=rounds.LastOrDefault()?.stage??1;}
            result.rounds=rounds.ToArray();return result;
        }
        static bool Better(Fight a,Fight b)=>a.won&&!b.won||a.won==b.won&&(a.remainingDefense<b.remainingDefense||a.remainingDefense==b.remainingDefense&&a.remainingDefenderHP<b.remainingDefenderHP);
        static List<ShopTrialModel.Piece> Selected(Context c)=>c.Lineup.Select(id=>c.Shop.Bench.Where(p=>p.Id==id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First()).ToList();
        static ShopCommand Command(ShopTrialModel shop,string kind,int before,string id=null,int slot=-1,int instance=0)=>
            new ShopCommand{kind=kind,id=id,slot=slot,instance=instance,goldBefore=before,goldAfter=shop.Gold,levelAfter=shop.Level,xpAfter=shop.XP};
        static void Prepare(Context c,EconomyPolicy policy,int stage,List<ShopCommand> commands)
        {
            var shop=c.Shop;int i=stage-1;
            while(shop.Level<policy.Levels[i]&&shop.Gold>=4)
            {int before=shop.Gold;if(!shop.Upgrade().Success)break;commands.Add(Command(shop,"Upgrade",before));}
            int cards=policy.Cards[i],refresh=policy.Refresh[i];
            Buy(c,stage,ref cards,commands);
            Commission(c,stage,ref cards,commands);
            while(refresh>=2&&shop.Gold>=2)
            {
                int before=shop.Gold;if(!shop.Refresh().Success)break;refresh-=2;commands.Add(Command(shop,"Refresh",before));
                Buy(c,stage,ref cards,commands);Commission(c,stage,ref cards,commands);
            }
            if(stage==13)
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
            int desired=cost==1&&stage>=6?3:2;
            if(owned.Any(p=>p.Star>=desired))return -1;
            var source=CampaignV2Balance.Build(c.Definitions[id],1,50,6);
            float score=Power(source)/Math.Max(1,cost);
            if(owned.Count(p=>p.Star==1)>=2)score+=100;
            else if(owned.Length>0)score+=25;
            if(c.Lineup.Contains(id))score+=15;
            if(cost==5&&stage<9)score*=.4f;
            if(id=="CARD_M41")score*=.4f; // Deferred candidate numbers never become a progression prerequisite.
            if(owned.Length==0&&c.Shop.Bench.Select(p=>p.Id).Distinct().Count()>=c.Shop.Level+3)return -1;
            return score;
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
                var next=available.OrderByDescending(p=>Power(CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,50,6))+Traits(chosen.Select(o=>o.Id).Concat(new[]{p.Id}))-current)
                    .ThenBy(p=>p.Instance).First();chosen.Add(next);available.Remove(next);
            }
            if(!c.Shop.SetLineup(chosen.Select(p=>p.Id),chosen.Select(p=>p.Instance)))throw new InvalidOperationException("Actual shop rejected selected lineup/reserve capacity.");
            string[] ids=chosen.Select(p=>p.Id).ToArray();int[] instances=chosen.Select(p=>p.Instance).ToArray();
            var prior=commands.LastOrDefault(p=>p.kind=="SetLineup");
            if(prior==null||!prior.lineupInstances.SequenceEqual(instances))commands.Add(new ShopCommand{kind="SetLineup",lineupIds=ids,lineupInstances=instances,goldBefore=c.Shop.Gold,goldAfter=c.Shop.Gold,levelAfter=c.Shop.Level,xpAfter=c.Shop.XP});
            c.Lineup=ids.ToList();
        }
        static float Traits(IEnumerable<string> ids)
        {
            var tags=ids.Distinct().SelectMany(CampaignBalance.Tags).GroupBy(t=>t).ToDictionary(g=>g.Key,g=>g.Count());float score=0;
            foreach(var pair in tags)
            {
                int count=pair.Value;if(count<2)continue;
                score+=pair.Key=="整合运动"?count>=8?52:count>=6?38:count>=4?24:14:
                    pair.Key=="萨卡兹"?count>=6?45:count>=4?30:18:count>=4?24:12;
            }
            return score;
        }
        static ManualBattle CreateBattle(Context c,CampaignJourneyStage stage)
        {
            var map=stage.map??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=stage.stage&&s.map!=null).map;
            var encounter=stage.encounter??CampaignJourneyCatalog.Current.stages.First(s=>s.stage>=stage.stage&&s.encounter!=null).encounter;
            var selected=Selected(c);var profiles=selected.Select(p=>CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,stage.dp,Math.Max(1,stage.defense))).ToArray();
            var first=profiles[0];var battle=new ManualBattle(first.Unit,encounter.route,Vector3.zero,first.Combat,roster:stage.IsSupply?Array.Empty<DefenderSettings>():stage.encounter.Snapshot(),
                deploymentLanes:encounter.LaneSnapshot(),allowEmptyRoster:stage.IsSupply);
            battle.ConfigureCardLoadout(profiles.Skip(1));battle.ConfigureCampaignV2(stage.stage);
            battle.ConfigureSynergies(selected.Select(p=>p.Copy()),golemExperience:c.Run.GolemExperience,contract:c.Contract);
            battle.ConfigureMarineTerrain(map.WalkablePoints(),map.MarineOrigin);battle.ConfigureGramophones(Array.Empty<GramophoneSettings>());
            if(stage.IsSupply)battle.ConfigureSupply(true);
            foreach(var d in battle.Defenders)battle.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
            battle.ConfigureApprovedMapEffects(map.mechanics);return battle;
        }
        static Fight FightBattle(Context c,ManualBattle battle,int variant,float observation)
        {
            int laneIndex=variant%battle.Lanes.Count,style=variant/battle.Lanes.Count;
            var selected=Selected(c);var profiles=selected.ToDictionary(p=>p.Id,p=>CampaignV2Balance.Build(c.Definitions[p.Id],p.Star,battle.DP,battle.Defense));
            string[] order=selected.OrderByDescending(p=>style==1?profiles[p.Id].Unit.Speed/Math.Max(1,profiles[p.Id].Unit.DeploymentCost):
                style==2?Power(profiles[p.Id])/Math.Max(1,profiles[p.Id].Unit.DeploymentCost):profiles[p.Id].Unit.Health/Math.Max(1,profiles[p.Id].Unit.DeploymentCost))
                .ThenBy(p=>p.Instance).Select(p=>p.Id).ToArray();
            long request=0;double nextDeploy=0;var actions=new List<BattleAction>();battle.Begin();
            while(battle.Phase==BattlePhase.Running&&battle.Clock<observation)
            {
                bool hold=style==4&&battle.DP<battle.Settings.InitialDP/2&&battle.Clock<35&&battle.ActiveCount>=2;
                if(!hold&&battle.Clock+1e-8>=nextDeploy)
                {
                    foreach(string id in order)
                    {
                        var lane=battle.Lanes[style==5?(Array.IndexOf(order,id)+laneIndex)%battle.Lanes.Count:laneIndex];
                        if(battle.CheckCardDeployment(id,lane.Entry)!=DeployFailure.None)continue;
                        long command=++request;var receipt=battle.TryDeployRole(battle.RunId,command,lane.Entry,id);if(!receipt.Success)continue;
                        actions.Add(new BattleAction{kind="Deploy",cardId=id,routeId=lane.Id,entityId=receipt.EntityId,request=command,at=battle.Clock});
                        if(style==3){nextDeploy=battle.Clock+1.2;break;}
                    }
                    if(battle.MudrockTier>=2&&!battle.GolemDeployed&&battle.DP>=battle.GolemCost)
                    {
                        var lane=battle.Lanes[laneIndex];long command=++request;var receipt=battle.TryDeployGolem(battle.RunId,command,lane.Entry);
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
            return Describe(battle,"lane="+battle.Lanes[laneIndex].Id+";style="+style,actions,battle.Phase==BattlePhase.Running);
        }
        static Fight Describe(ManualBattle b,string policy,List<BattleAction> actions,bool observedTimeout)=>new Fight{
            policy=policy,phase=b.Phase.ToString(),won=b.Phase==BattlePhase.Won,timedObservation=observedTimeout,seconds=b.Clock,remainingDefense=b.Defense,dp=b.DP,breaches=b.Breaches,
            deployments=b.Units.Count(u=>!u.SynergySupport),damage=b.TotalPlayerDamage,remainingDefenderHP=b.Defenders.Where(d=>d.Alive).Sum(d=>d.Health),
            survivingDefenders=b.Defenders.Where(d=>d.Alive).Select(d=>d.EntityId).ToArray(),portalTrips=b.ApprovedPortalTrips,heatDamage=b.ApprovedHeatDamageToUnits,actions=actions.ToArray()};
    }
}
