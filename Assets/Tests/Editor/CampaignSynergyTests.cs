using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class CampaignSynergyTests
    {
        static ShopTrialModel Economy(bool infinite=false)
        {
            var m=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=1},new ShopTrialModel.Role{Id="B",Name="B",Cost=1}});
            m.EnableCampaign(infinite);m.Preset(new[]{new ShopTrialModel.Piece{Id="A",Star=1}},Array.Empty<string>(),1,0,10);return m;
        }
        [Test]public void ThirteenStagesFiveSafeStopsThreeBossesAndIncomeBudget()
        {
            Assert.That(Enumerable.Range(1,13).Count(CampaignPlan.Supply),Is.EqualTo(5));
            Assert.That(new[]{6,10,13}.All(CampaignPlan.Boss));Assert.That(CampaignPlan.Supply(1)&&CampaignPlan.Supply(2));
            foreach(int boss in new[]{6,10,13})Assert.That(CampaignPlan.Supply(boss-1));
            Assert.That(CampaignPlan.BaseGold.Sum(),Is.EqualTo(230));
            int total=10;for(int i=0;i<13;i++)total+=CampaignPlan.BaseGold[i]+Math.Min(3,i)+3;
            Assert.That(total,Is.EqualTo(312)); // Example: three interest per encounter, not a guaranteed run.
        }
        [Test]public void CommissionIsFiniteAtomicAndCannotStealDisplayedStock()
        {
            var m=Economy();var one=m.Commission("A");Assert.That(one.Success);Assert.That(m.Gold,Is.EqualTo(7));
            Assert.That(m.Commission("A").Success,Is.False);m.BeginPreparation();
            Assert.That(m.Commission("A").Success);Assert.That(m.Bench.Single().Star,Is.EqualTo(2));
            Assert.That(m.Available("A"),Is.EqualTo(27));m.Validate();
            m.BeginPreparation();int gold=m.Gold;Assert.That(m.Commission("B").Success,Is.False);Assert.That(m.Gold,Is.EqualTo(gold));Assert.That(m.CommissionUsed,Is.False);
        }
        [Test]public void InfiniteCurrencyStillConservesThirtyCopiesAndMergesOneCostToFourStars()
        {
            var m=Economy(true);for(int i=0;i<26;i++)Assert.That(m.Commission("A").Success);
            Assert.That(m.Bench.Single().Star,Is.EqualTo(4));Assert.That(m.Gold,Is.EqualTo(10));Assert.That(m.Available("A"),Is.EqualTo(3));
            Assert.That(m.Commission("A").Success,Is.False);m.Validate();
            for(int i=0;i<500;i++)m.Refresh();Assert.That(m.Gold,Is.EqualTo(10));m.Validate();
        }
        [TestCase(4)][TestCase(5)]public void HighCostCommissionsAreSandboxOnlyAndConsumeRealStockThroughPromotion(int cost)
        {
            var roles=new[]{new ShopTrialModel.Role{Id="HIGH",Name="High",Cost=cost},new ShopTrialModel.Role{Id="OTHER",Name="Other",Cost=1}};
            var normal=new ShopTrialModel(roles);normal.EnableCampaign();normal.Preset(new[]{new ShopTrialModel.Piece{Id="HIGH",Star=1}},Array.Empty<string>(),8,0,100);
            Assert.That(normal.Commission("HIGH").Success,Is.False);Assert.That(normal.Gold,Is.EqualTo(100));Assert.That(normal.Available("HIGH"),Is.EqualTo(normal.Capacity("HIGH")-1));Assert.That(normal.CommissionUsed,Is.False);
            var sandbox=new ShopTrialModel(roles);sandbox.EnableCampaign(true);sandbox.Preset(new[]{new ShopTrialModel.Piece{Id="HIGH",Star=1}},Array.Empty<string>(),8,0,10);
            for(int copy=0;copy<8;copy++)Assert.That(sandbox.Commission("HIGH").Success,Is.True);
            Assert.That(sandbox.Bench.Single().Star,Is.EqualTo(3));Assert.That(sandbox.Available("HIGH"),Is.EqualTo(sandbox.Capacity("HIGH")-9));
            Assert.That(sandbox.Gold,Is.EqualTo(10));Assert.That(sandbox.Commission("HIGH").Success,Is.False);sandbox.Validate();
        }
        [Test]public void SandboxHighCostCommissionCannotTakeDisplayedStockOrPartiallyMutateAFullBench()
        {
            var roles=Enumerable.Range(0,11).Select(i=>new ShopTrialModel.Role{Id="R"+i,Name="R"+i,Cost=i==0?5:1}).ToArray();
            var shop=new ShopTrialModel(roles);shop.EnableCampaign(true);
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="R0",Star=2},new ShopTrialModel.Piece{Id="R0",Star=2}},new[]{"R0","R0","R0"},8,0,10);
            Assert.That(shop.Commission("R0").Success,Is.False,"Displayed cards already occupy the remaining three copies.");
            Assert.That(shop.Bench.Count,Is.EqualTo(2));Assert.That(shop.Available("R0"),Is.Zero);shop.Validate();
            shop.Preset(Enumerable.Range(0,10).Select(i=>new ShopTrialModel.Piece{Id="R"+i,Star=1}),Array.Empty<string>(),8,0,10);
            int stock=shop.Available("R0");var before=shop.Bench.Select(p=>p.Instance+":"+p.Id+":"+p.Star).ToArray();
            Assert.That(shop.Commission("R0").Success,Is.False);CollectionAssert.AreEqual(before,shop.Bench.Select(p=>p.Instance+":"+p.Id+":"+p.Star));
            Assert.That(shop.Available("R0"),Is.EqualTo(stock));Assert.That(shop.Gold,Is.EqualTo(10));Assert.That(shop.CommissionUsed,Is.False);shop.Validate();
        }
        [Test]public void SupplyHasNoCombatOrHPDeductionAndSettlementIsIdempotent()
        {
            var m=Economy();var loop=new BattleShopRun(m,1,false,true);var b=Battle();int hp=b.Defense;
            Assert.That(loop.RewardStage);b.ConfigureSupply();Assert.That(b.CompleteSupply());Assert.That(b.Defense,Is.EqualTo(hp));
            Assert.That(loop.Observe(b.Phase));int gold=m.Gold;Assert.That(loop.Observe(b.Phase),Is.False);Assert.That(m.Gold,Is.EqualTo(gold));
            Assert.That(loop.OpenShop());Assert.That(loop.RewardStage);Assert.That(loop.Stage,Is.EqualTo(2));
        }
        [Test]public void SupplyBeginCannotAccidentallyStartALosingFight()
        {
            var b=Battle();int hp=b.Defense;b.ConfigureSupply();b.Begin();b.Advance(100);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.Defense,Is.EqualTo(hp));Assert.That(b.Units,Is.Empty);Assert.That(b.Clock,Is.Zero);
        }
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,5,8,2,.1f,300,1,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:2,unitArmor:0,unitRange:3,unitInterval:1.8f,defenderHealth:2000,defenderAttack:1,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
        static ManualBattle Battle()=>new ManualBattle(Unit("CARD_M11"),new[]{Vector3.zero,new Vector3(10,0,0)},new Vector3(.5f,0,0),Combat());
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        [Test]public void SnapshotCountsKindsNotStarsAndPausedMusterSpendsOnlyNormalCards()
        {
            var b=Battle();b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Unit("CARD_M12"),Combat(),true),new BasicRosterProfile(Unit("CARD_M07"),Combat(),true)});
            b.ConfigureSynergies(new[]{Piece("CARD_M11"),Piece("CARD_M12"),Piece("CARD_M07")});Assert.That(b.ReunionTier,Is.EqualTo(2));Assert.That(b.SnowTier,Is.EqualTo(2));
            b.Begin();b.SetPaused(true);Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M11",true).Success);Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12",true).Success);
            Assert.That(b.MusterUsed,Is.EqualTo(1));Assert.That(b.Units.Count(u=>u.SynergySupport),Is.EqualTo(1));Assert.That(b.DP,Is.EqualTo(96));Assert.That(b.Clock,Is.Zero);
            b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12",true);Assert.That(b.Units.Count,Is.EqualTo(3));
            b.Advance(1);Assert.That(b.Clock,Is.Zero);b.Restart();Assert.That(b.MusterUsed,Is.Zero);Assert.That(b.SnowTier,Is.EqualTo(2));
        }
        [Test]public void TwoDifferentDirectSourcesShatterOnceAndPauseFreezesEvents()
        {
            var b=Battle();b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Unit("CARD_M12"),Combat(),true)});
            b.ConfigureSynergies(new[]{Piece("CARD_M11"),Piece("CARD_M12")});b.Begin();
            b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M11");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12");
            var d=b.Defenders[0];Assert.That(b.TryApplyCold(d.EntityId,3.5f));Assert.That(b.TryApplyCold(d.EntityId,3.5f));
            b.Advance(.5f);Assert.That(b.Shatters,Is.EqualTo(1));Assert.That(b.ShatterDamage,Is.GreaterThan(0));Assert.That(d.FrozenUntil,Is.LessThanOrEqualTo(b.Clock));
            b.SetPaused(true);double t=b.Clock;b.Advance(10);Assert.That(b.Clock,Is.EqualTo(t));Assert.That(b.Shatters,Is.EqualTo(1));
        }
        [Test]public void StarDPAndUsesDoNotMultiplyTogether()
        {
            Assert.That(CampaignPlan.DP(1,2,3),Is.EqualTo(4));Assert.That(CampaignPlan.DP(5,8,3),Is.EqualTo(12));
            Assert.That(CampaignPlan.Uses(1),Is.EqualTo(8));Assert.That(CampaignPlan.Uses(5),Is.EqualTo(2));
            Assert.That(CampaignBalance.HPScale(1,3),Is.EqualTo(3));Assert.That(CampaignBalance.AttackScale(1,3),Is.EqualTo(2.15f));
        }
        [Test]public void IndependentBenchAllowsTenReserveCardsBeyondSelectedLineup()
        {
            var m=new ShopTrialModel(Enumerable.Range(0,13).Select(i=>new ShopTrialModel.Role{Id="R"+i,Name="R"+i,Cost=1}));m.EnableCampaign(true);
            m.Preset(Enumerable.Range(0,10).Select(i=>new ShopTrialModel.Piece{Id="R"+i,Star=1}),new[]{"R10","R11","R12"},2,0,10);
            Assert.That(m.SetLineup(new[]{"R0","R1"}));Assert.That(m.Purchase(0).Success);Assert.That(m.Purchase(1).Success);
            Assert.That(m.Bench.Count,Is.EqualTo(12));Assert.That(m.Purchase(2).Success,Is.False);Assert.That(m.SetLineup(new[]{"R0"}),Is.False);m.Validate();
        }
        [Test]public void AllLaunchProfilesBuildAtEveryStarAndKeepAuthoredActionWindows()
        {
            var data=UnityEditor.AssetDatabase.FindAssets("t:ManualBattleDefinition").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(d=>d!=null&&CampaignBalance.Supports(d.Card.CardId)).GroupBy(d=>d.Card.CardId).Select(g=>g.First()).ToArray();
            Assert.That(data.Length,Is.EqualTo(CampaignBalance.Profiles.Count+BossRosterCatalog.Entries.Count));
            foreach(var d in data)for(int star=1;star<=3;star++)
            {
                var p=CampaignBalance.Build(d,star,50,5);Assert.That(p.Combat.UnitInterval,Is.GreaterThanOrEqualTo(p.Combat.AttackDuration));
                Assert.That(p.Combat.AttackDuration,Is.EqualTo(d.Engagement.Snapshot().AttackDuration));Assert.That(p.Unit.DeploymentLimit,Is.EqualTo(CampaignPlan.Uses(d.Card.RecruitCost)));
            }
        }
        [Test]public void CampaignBossesUseExistingDistinctCompleteSkillsWithoutMutatingSource()
        {
            var original=Resources.Load<DefenderTrialDefinition>("Maps/BattleShop");float hp=original.defenders[1].health;
            var a=CampaignPlan.Guards(6,false);var b=CampaignPlan.Guards(10,false);var c=CampaignPlan.Guards(13,false);
            Assert.That(a.Length,Is.EqualTo(1));Assert.That(b.Length,Is.EqualTo(2));Assert.That(c.Length,Is.EqualTo(3));
            Assert.That(a[0].skill.kind,Is.EqualTo(DefenderSkillKind.YuKitchen));Assert.That(b[1].skill.kind,Is.EqualTo(DefenderSkillKind.PramanixSnow));
            Assert.That(original.defenders[1].health,Is.EqualTo(hp));
        }
    }
}
