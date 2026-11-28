using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationEconomicCampaignTests
    {
        // Stable IDs do not depend on the selected balance variant. Avoid repeated catalog scans in long simulations.
        static readonly Dictionary<string,string> cardIds=FormationCampaignRules.AllUnits.ToDictionary(u=>u.Id,u=>u.CardId);
        static string Card(string id)=>cardIds[id];
        static readonly string[] members={"M17","M16","B05","B01","B22","M38"};
        [SetUp] public void Begin()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
        [TearDown] public void End()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        static FormationEconomyV09 Economy(ShopTrialModel shop,int seed,int version=4)=>(FormationEconomyV09)Activator.CreateInstance(
            typeof(FormationEconomyV09),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{shop,seed,version},null);
        static ShopTrialModel Shop(int star=2)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(u=>new ShopTrialModel.Role{Id=u.CardId,Name=u.Name,Cost=u.Cost}),71);
            shop.EnableFormationEconomy();shop.Preset(members.Select(id=>new ShopTrialModel.Piece{Id=Card(id),Star=star}),Array.Empty<string>(),8,0,100);
            return shop;
        }
        static void Layers(FormationEconomyV09 e,int f,int m)
        {
            typeof(FormationEconomyV09).GetProperty("ForesightLayers").SetValue(e,f);
            typeof(FormationEconomyV09).GetProperty("MiracleLayers").SetValue(e,m);
        }
        [Test] public void ThresholdsKeepForesightTrainingButMiracleOnlyNeedsActivation()
        {
            var shop=Shop();var e=Economy(shop,71);Layers(e,99,324);Assert.That(e.MemberDiscountReady,Is.False);Assert.That(e.MiracleChainReady,Is.False);
            Layers(e,100,324);Assert.That(e.MemberDiscountReady,Is.True);Assert.That(e.GlobalDiscountReady,Is.False);
            Layers(e,325,325);Assert.That(e.GlobalDiscountReady&&e.MiracleChainReady,Is.True);
            shop.Sell(shop.Bench.Single(p=>p.Id==Card("M38")).Instance);Assert.That(e.MiracleChainReady,Is.True);
            shop.Sell(shop.Bench.Single(p=>p.Id==Card("B22")).Instance);Assert.That(e.MiracleChainReady,Is.False);Assert.That(e.FreeChance,Is.Zero);
            shop.Grant(Card("B22"),1);Assert.That(e.MiracleChainReady,Is.True);Assert.That(e.FreeChance,Is.EqualTo(1));
            shop.Sell(shop.Bench.Single(p=>p.Id==Card("B05")).Instance);Assert.That(e.GlobalDiscountReady,Is.False);
        }
        [Test] public void MiracleChancePassesEightyPercentContinuouslyAndReachesOneAt325WithOneStars()
        {
            var shop=Shop(1);var e=Economy(shop,71);
            foreach(int layer in new[]{0,100,253,300,324,325})
            {Layers(e,0,layer);Assert.That(e.FreeChance,Is.EqualTo(.1f+.9f*layer/325).Within(.000001f));}
            Layers(e,0,300);Assert.That(e.FreeChance,Is.GreaterThan(.9f));Assert.That(e.MiracleChainReady,Is.False);
            Layers(e,0,325);Assert.That(e.MiracleChainReady,Is.True);shop.FreeRefreshes=1000;
            for(int i=0;i<1000;i++)Assert.That(shop.Refresh().Success,Is.True);
            Assert.That(shop.FreeRefreshes,Is.EqualTo(1000));Assert.That(e.MiracleLayers,Is.EqualTo(325));
        }
        [Test] public void LayerOverflowPaysOnlyNewMilestonesAndCannotFarmAtFullLayers()
        {
            var shop=Shop(3);var e=Economy(shop,71);Layers(e,319,299);e.ObservePromotions();
            Assert.That(e.ForesightLayers,Is.EqualTo(325));Assert.That(e.MiracleLayers,Is.EqualTo(325));Assert.That(e.EarnedGold,Is.EqualTo(12));
            var p=shop.Bench.Single(x=>x.Id==Card("M17"));shop.SetLineup(new[]{p.Id},new[]{p.Instance});
            for(int n=0;n<10;n++)e.SettleCasts("cap-"+n,Enumerable.Range(0,100).Select(_=>new FormationBattleModel.CastEvent{Instance=p.Instance,UnitId="M17"}));
            Assert.That(e.ForesightLayers,Is.EqualTo(325));Assert.That(e.EarnedGold,Is.EqualTo(12));
        }
        [Test] public void PromotionPaysOncePerKindAcrossDuplicatesSellingRebuyingAndFourthStar()
        {
            var shop=Shop(3);var e=Economy(shop,71);e.ObservePromotions();Assert.That(e.ForesightLayers,Is.EqualTo(96));Assert.That(e.MiracleLayers,Is.EqualTo(96));
            for(int i=0;i<10;i++)e.ObservePromotions();Assert.That(e.ForesightLayers,Is.EqualTo(96));
            var old=shop.Bench.Single(p=>p.Id==Card("M17"));shop.Sell(old.Instance);Assert.That(shop.Grant(Card("M17"),4).Success,Is.True);
            e.ObservePromotions();Assert.That(e.ForesightLayers,Is.EqualTo(96));shop.Validate();
        }
        [Test] public void PurchaseThatCompletesNineCopiesPaysImmediatelyDespitePriorPurchaseThisNode()
        {
            var shop=Shop(1);var e=Economy(shop,71);e.BeginNode("1-4");
            for(int i=0;i<1000&&!shop.Bench.Any(p=>p.Id==Card("M17")&&p.Star>=3);i++)
            {
                shop.Refresh(true);for(int slot=0;slot<5;slot++)if(shop.Offers[slot]==Card("M17"))Assert.That(shop.Purchase(slot).Success,Is.True);
            }
            Assert.That(shop.Bench.Any(p=>p.Id==Card("M17")&&p.Star==3),Is.True);Assert.That(e.ForesightLayers,Is.EqualTo(34));shop.Validate();
        }
        [Test] public void ThreeStarNativeCastIncomeHasFirstCastBonusSixCastCapAndSettlementIdempotence()
        {
            var shop=Shop(3);var e=Economy(shop,71);var actor=shop.Bench.Single(p=>p.Id==Card("M17"));shop.SetLineup(new[]{actor.Id},new[]{actor.Instance});
            var casts=Enumerable.Range(0,100).Select(_=>new FormationBattleModel.CastEvent{Instance=actor.Instance,UnitId="M17"}).ToArray();
            e.SettleCasts("1-4",casts);Assert.That(e.ForesightLayers,Is.EqualTo(96+30));Assert.That(e.MiracleLayers,Is.EqualTo(96));
            e.SettleCasts("1-4",casts);Assert.That(e.ForesightLayers,Is.EqualTo(126));e.SettleCasts("1-6",casts);Assert.That(e.ForesightLayers,Is.EqualTo(156));
        }
        [Test] public void BenchDuplicatesCannotUpgradeTheOneStarActorsCastBonus()
        {
            var shop=Shop(1);Assert.That(shop.Grant(Card("M17"),3).Success,Is.True);var actor=shop.Bench.Single(p=>p.Id==Card("M17")&&p.Star==1);
            var e=Economy(shop,71);shop.SetLineup(new[]{actor.Id},new[]{actor.Instance});
            var casts=Enumerable.Range(0,8).Select(_=>new FormationBattleModel.CastEvent{Instance=actor.Instance,UnitId="M17"}).ToArray();
            e.SettleCasts("1-4",casts);Assert.That(e.ForesightLayers,Is.EqualTo(32+3+18));
        }
        [Test] public void RefreshesAndUnrecruitedCastsStillCannotSelfStart()
        {
            var shop=Shop(1);var e=Economy(shop,71);shop.FreeRefreshes=500;for(int i=0;i<500;i++)shop.Refresh();
            e.SettleCasts("1-1",new[]{new FormationBattleModel.CastEvent{Instance=-99,UnitId="M17"}});
            Assert.That(e.ForesightLayers+e.MiracleLayers,Is.Zero);Assert.That(e.EarnedGold,Is.Zero);Assert.That(shop.Gold,Is.EqualTo(100));
        }
        [Serializable] public sealed class PathResult
        {
            public string rules,route,assumption,wins="";public int seed,nodes,firstF=-1,firstM=-1,firstThree=-1,gold,level,foresight,miracle,spentXP,spentRefresh,spentCards,freeRefreshes;
            public bool survived,foBeforeFinal,miBeforeFinal,fiveThree;public List<string> promotions=new List<string>();
        }
        [Serializable] public sealed class CampaignReport
        {
            public string scope="Conditional full-route ECONOMY Monte Carlo, not measured combat win rates. Production starting delivery, all 23 real nodes, finite stock/odds/cash/XP, actual card fusions, reserve and population limits, reward choices and losses. Battle wins and casts are fixed per seed/node/card across rule versions; these are sampled assumptions, not combat simulation. No preset economic members, stars, layers, level or gold. Supplies/augments resolved by a fixed policy; no equipment combat benefit estimated. Bounded search policy and one target five-cost do not optimize all play styles.";
            public List<PathResult> runs=new List<PathResult>();
        }
        static readonly string[] allPriority={"M18","B22","M17","M38","M16","B05","B01","M25"};
        static void Lineup(ShopTrialModel shop,string[] wanted)
        {
            var chosen=shop.Bench.GroupBy(p=>p.Id).Select(g=>g.OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First())
                .OrderBy(p=>Array.IndexOf(allPriority,FormationCampaignRules.Find(p.Id).Id)<0?99:Array.IndexOf(allPriority,FormationCampaignRules.Find(p.Id).Id)).Take(shop.PopulationCap).ToArray();
            shop.SetLineup(chosen.Select(p=>p.Id),chosen.Select(p=>p.Instance));
        }
        static void Resolve(FormationCampaignRun run)
        {
            for(int pass=0;pass<80;pass++)
            {
                if(run.HasBlockingChoice)
                {bool selected=false;for(int i=0;i<run.Choices.Count;i++)if(run.Choose(i).Success){selected=true;break;}if(!selected)throw new InvalidOperationException("No affordable choice");continue;}
                if(run.PendingRewardChoices>0){if(!run.ChooseReward(0).Success)break;continue;}
                var ball=run.PendingBalls.FirstOrDefault();if(ball==null||run.BeginBall(ball.Id)==null)break;run.CompleteBall(ball.Id);
            }
        }
        static PathResult Simulate(int seed,string route,int scenario,int version)
        {
            var session=FormationShopSession.StartNew(seed,true);var shop=session.Shop;var run=session.Run;
            var e=Economy(shop,seed,version);typeof(FormationCampaignRun).GetField("<Economy>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(run,e);
            FormationCampaignRunTests.CompleteOpeningDelivery(session);Resolve(run);
            var wanted=(route=="dual"?members:route=="foresight"?members.Take(3):members.Skip(3)).Concat(new[]{"M18","M25"}).Select(Card).ToArray();
            var result=new PathResult{seed=seed,route=route,rules="R"+version,assumption=new[]{"low:1-3 casts; win .90/.65/.40","middle:2-4 casts; win .95/.80/.60","high:3-5 casts; win .98/.90/.80"}[scenario]};
            void Mark()
            {
                if(e.GlobalDiscountReady&&result.firstF<0)result.firstF=run.NodeIndex;
                if(e.MiracleChainReady&&result.firstM<0)result.firstM=run.NodeIndex;
                foreach(var p in shop.Bench.Where(p=>p.Star>=3&&members.Select(Card).Contains(p.Id)))if(!result.promotions.Contains(p.Id)){result.promotions.Add(p.Id);if(result.firstThree<0)result.firstThree=run.NodeIndex;}
            }
            for(int n=0;n<FormationCampaignRules.Nodes.Length&&!run.Finished;n++)
            {
                Resolve(run);foreach(var p in shop.Bench.Where(p=>!wanted.Contains(p.Id)&&p.Id!=Card("WIS")).ToArray())shop.Sell(p.Instance);Lineup(shop,wanted);
                var node=run.CurrentNode;
                if(node.Kind=="补给"){if(!run.AdvanceNode())break;continue;}
                int targetLevel=node.Plane==1?(n<3?3:n<5?4:5):node.Plane==2?(node.Id=="2-7"?7:6):node.Id is "3-4" or "3-5" or "3-6" or "3-7"?9:8;
                int floor=node.Plane==1?5:10;
                void Buy()
                {
                    for(int slot=0;slot<5;slot++)
                    {
                        string id=shop.Offers[slot];if(id==null)continue;bool keep=wanted.Contains(id);int goal=shop.Definition(id).Cost==1?9:3;
                        int held=shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star));
                        bool target=id==Card("WIS")&&shop.Level>=8;
                        if(keep&&held>=goal||!keep&&!target&&!e.GlobalDiscountReady)continue;
                        if(!e.GlobalDiscountReady&&shop.Gold-shop.Price(id)<floor)continue;
                        var bought=shop.Purchase(slot);if(!bought.Success)continue;result.spentCards+=bought.Value;run.OnPurchase(id);
                        if(!keep&&!target)shop.Sell(bought.Instance);Lineup(shop,wanted);Mark();
                    }
                }
                Buy();int limit=e.GlobalDiscountReady||e.MiracleChainReady?(node.Id=="3-7"?200:12):node.Plane==1?2:node.Plane==2?3:4;
                for(int roll=0;roll<limit;roll++)
                {
                    while(shop.Level<targetLevel&&shop.Gold>=floor+8){int before=shop.Gold;if(!shop.Upgrade().Success)break;result.spentXP+=before-shop.Gold;Lineup(shop,wanted);}
                    if(!e.GlobalDiscountReady&&shop.Gold<floor+4)break;
                    int old=shop.Gold,coupons=shop.FreeRefreshes;if(!shop.Refresh().Success)break;result.spentRefresh+=old-shop.Gold;if(coupons>0)result.freeRefreshes++;run.OnManualRefresh();Buy();
                    if(shop.Bench.Any(p=>p.Id==Card("WIS")&&p.Star==3))break;
                }
                Mark();if(node.Id=="3-7"){result.foBeforeFinal=e.GlobalDiscountReady;result.miBeforeFinal=e.MiracleChainReady;}
                float winChance=new[]{new[]{.90f,.65f,.40f},new[]{.95f,.80f,.60f},new[]{.98f,.90f,.80f}}[scenario][node.Plane-1];
                var outcomeRandom=new Random(unchecked(seed^0x72913^(run.NodeIndex*1877)));
                bool won=node.Kind=="奖励"||outcomeRandom.NextDouble()<winChance;result.wins+=won?"W":"L";
                var casts=new List<FormationBattleModel.CastEvent>();
                foreach(var p in shop.Bench.Where(p=>shop.IsLineupInstance(p.Instance)))
                {
                    int kind=Array.FindIndex(FormationCampaignRules.AllUnits,u=>u.CardId==p.Id);
                    var castRandom=new Random(unchecked(seed^(run.NodeIndex*1877)^(kind*911)));
                    int count=node.Kind=="奖励"?castRandom.Next(2):Math.Max(0,castRandom.Next(scenario+1,scenario+4)-(won?0:1));
                    for(int c=0;c<count;c++)casts.Add(new FormationBattleModel.CastEvent{Instance=p.Instance,UnitId=FormationCampaignRules.Find(p.Id).Id});
                }
                if(run.SettleBattle(won,won?1:.6f,node.DefenderCount,won?0:Math.Max(1,node.DefenderCount/2),!won&&node.Kind=="首领",casts)==null)throw new InvalidOperationException("Settlement blocked at "+node.Id);
                result.nodes++;Mark();Resolve(run);shop.Validate();if(!run.AdvanceNode())break;
            }
            result.survived=run.Won;result.gold=shop.Gold;result.level=shop.Level;result.foresight=e.ForesightLayers;result.miracle=e.MiracleLayers;result.fiveThree=shop.Bench.Any(p=>p.Id==Card("WIS")&&p.Star==3);return result;
        }
        [Test,Timeout(900000)] public void FullRouteEconomyMonteCarloIncludesAcquisitionCostsAndConditionalLosses()
        {
            var report=new CampaignReport();
            foreach(int version in new[]{3,4})foreach(string route in new[]{"foresight","miracle","dual"})foreach(int scenario in new[]{0,1,2})
                for(int seed=0;seed<100;seed++)
                {
                    report.runs.Add(Simulate(20261128+seed,route,scenario,version));
                    if(report.runs.Count%100==0)
                    {Directory.CreateDirectory("artifacts/economy-v09-r4");File.WriteAllText("artifacts/economy-v09-r4/full-route.json",UnityEngine.JsonUtility.ToJson(report,true));UnityEngine.Debug.Log("ECONOMY_ROUTE_PROGRESS "+report.runs.Count);}
                }
            Directory.CreateDirectory("artifacts/economy-v09-r4");File.WriteAllText("artifacts/economy-v09-r4/full-route.json",UnityEngine.JsonUtility.ToJson(report,true));
            foreach(var pair in report.runs.GroupBy(r=>(r.seed,r.route,r.assumption)))
            {var a=pair.First().wins;var b=pair.Last().wins;int common=Math.Min(a.Length,b.Length);Assert.That(a.Substring(0,common),Is.EqualTo(b.Substring(0,common)),"Paired battle inputs must match");}
            Assert.That(report.runs.Count,Is.EqualTo(1800));Assert.That(report.runs.All(r=>r.gold>=0&&r.level>=3&&r.nodes>0),Is.True);
        }
    }
}
