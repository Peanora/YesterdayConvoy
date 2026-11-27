using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    // Versioned v0.8 regressions keep their original contract. New tests opt into each candidate explicitly.
    [SetUpFixture] public sealed class FormationBaselineTestScope
    {
        [OneTimeSetUp] public void Begin() => FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        [OneTimeTearDown] public void End() => FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
    }
    public sealed class FormationV09Tests
    {
        [SetUp] public void Begin()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
        [TearDown] public void End()=>FormationBalanceV09.Select(FormationBalanceV09.Variant.Baseline);
        static ShopTrialModel Shop(int seed=81)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(spec=>new ShopTrialModel.Role{Id=spec.CardId,Name=spec.Name,Cost=spec.Cost}),seed);
            shop.EnableFormationEconomy();return shop;
        }
        static object Call(object model,string name,params object[] args)=>model.GetType().GetMethods(BindingFlags.Instance|BindingFlags.NonPublic)
            .Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(model,args);
        static FormationBattleModel Fixture(string id,out FormationCampaignRun run,int star=3)
        {
            var shop=Shop();run=new FormationCampaignRun(shop,81);while(run.HasBlockingChoice)run.Choose(0);
            string card=FormationCampaignRules.Find(id).CardId;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=card,Star=star}},Array.Empty<string>(),10,0,100);
            Assert.That((bool)Call(run,"ApplyHero",FormationBalanceV09.Heroes.Single(hero=>hero.Extra==id).Id),Is.True);
            var model=new FormationBattleModel(81);model.BindCampaign(run);
            model.ApplyRoster(new[]{new FormationRosterEntry{Instance=shop.Bench[0].Instance,ArtId=card,Star=star,Spawn=Vector2.zero}},10);
            return model;
        }
        [TestCase("M11",2)][TestCase("M13",2)][TestCase("B22",12)][TestCase("M31",18)]
        public void SelectedHeroChangesActualSkillCost(string id,float expected)
        {
            var model=Fixture(id,out _);Assert.That(model.StartBattle(),Is.True);Assert.That(model.Units[0].SkillRequired,Is.EqualTo(expected).Within(.001));
        }
        [TestCase("M11")][TestCase("M21")][TestCase("B22")][TestCase("M13")][TestCase("M16")][TestCase("M27")][TestCase("M31")][TestCase("M42")][TestCase("B23")]
        public void EveryHeroCastsThroughProductionTimingWithoutNonfiniteStats(string id)
        {
            var model=Fixture(id,out _);var guard=model.Defenders[0];model.Defenders.RemoveRange(1,model.Defenders.Count-1);
            guard.MaxHealth=10000000;guard.Attack=0;guard.Armor=40;guard.Healer=false;guard.Position=new Vector2(12,3);
            model.StartBattle();var actor=model.Units[0];actor.Position=new Vector2(11,3);actor.SkillCharge=actor.SkillRequired;
            for(int step=0;step<1000;step++)model.Advance(.025f);
            Assert.That(model.SkillCasts.Count,Is.GreaterThan(0));Assert.That(actor.DamageDone,Is.GreaterThan(0));
            Assert.That(float.IsNaN(actor.Health)||float.IsInfinity(actor.Attack),Is.False);
            Assert.That(model.Retry(),Is.True);Assert.That(actor.DamageDone,Is.EqualTo(0));
        }
        [Test] public void OnlyOneHeroAndOneRealStockGift()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,81);while(run.HasBlockingChoice)run.Choose(0);
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M11",Star=1}},Array.Empty<string>(),6,0,100);
            int before=shop.Available("CARD_M11");Assert.That((bool)Call(run,"ApplyHero","H11"),Is.True);
            Assert.That(shop.Available("CARD_M11"),Is.EqualTo(before-1));Assert.That((bool)Call(run,"ApplyHero","H13"),Is.False);
            Assert.That((bool)Call(run,"ApplyHero","H11"),Is.False);shop.Validate();
        }
        [TestCase("M11",1)][TestCase("M13",2)][TestCase("M31",2)]
        public void RefreshInsuranceIsPaidFiniteAndStopsAtThreeStar(string id,int cap)
        {
            var shop=Shop();string card=FormationCampaignRules.Find(id).CardId;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=card,Star=1}},Array.Empty<string>(),7,0,200);
            typeof(ShopTrialModel).GetField("HeroRecruitId",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(shop,card);
            int Counter(string key)=>(int)typeof(ShopTrialModel).GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(shop);
            shop.Refresh(true);Assert.That(Counter("HeroRefreshes"),Is.Zero);
            shop.FreeRefreshes=1;shop.Refresh();Assert.That(Counter("HeroRefreshes"),Is.Zero);
            shop.Locked=true;Assert.That(shop.Refresh().Success,Is.False);Assert.That(Counter("HeroRefreshes"),Is.Zero);shop.Locked=false;
            for(int i=1;i<=20;i++){Assert.That(shop.Refresh().Success,Is.True);if(i%5==0&&i/5<=cap)Assert.That(shop.Offers[0],Is.EqualTo(card));}
            Assert.That(Counter("HeroGuarantees"),Is.EqualTo(cap));shop.Validate();
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=card,Star=3}},Array.Empty<string>(),7,0,200);
            for(int i=0;i<10;i++)shop.Refresh();Assert.That(Counter("HeroGuarantees"),Is.EqualTo(cap));
        }
        [Test] public void HeroOfferingRespectsOwnedTierPlaneAndPriorSelection()
        {
            var shop=Shop();var run=new FormationCampaignRun(shop,81);while(run.HasBlockingChoice)run.Choose(0);
            shop.Preset(new[]{"CARD_M11","CARD_M13","CARD_M31"}.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),7,0,100);
            string[] Eligible(string tier,int plane)=>((FormationCampaignRules.DataRecord[])Call(run,"EligibleHeroes",tier,plane)).Select(hero=>hero.Id).ToArray();
            Assert.That(Eligible("银",1),Is.EquivalentTo(new[]{"H11"}));Assert.That(Eligible("金",1),Is.EquivalentTo(new[]{"H13"}));
            Assert.That(Eligible("金",2),Is.EquivalentTo(new[]{"H13","H31"}));Assert.That(Eligible("彩",2),Is.Empty);Assert.That(Eligible("金",3),Is.Empty);
            Call(run,"ApplyHero","H11");Assert.That(Eligible("金",2),Is.Empty);
        }
        [Test]public void HostLeechUsesActualEnemyHealthDamageAndPerSecondCap()
        {
            var model=Fixture("M21",out _);model.StartBattle();var unit=model.Units[0];var guard=model.Defenders[0];
            unit.Health=unit.MaxHealth*.2f;guard.Health=guard.MaxHealth=10000000;
            Call(model,"GiveBuff",unit,"hero_leech",1f,6d);
            float before=unit.Health;
            Call(model,"CombatDamage",unit,guard,100000f,2,false,true,false);
            Assert.That(unit.Health-before,Is.EqualTo(unit.MaxHealth*.08f).Within(.01));
            before=unit.Health;Call(model,"CombatDamage",unit,guard,100000f,2,false,true,false);Assert.That(unit.Health,Is.EqualTo(before));
            Call(model,"CombatDamage",unit,guard,100000f,2,false,false,false);Assert.That(unit.Health,Is.EqualTo(before));
        }
        [Test]public void ShieldHeroStoresOnlyOwnNativeShieldAndClearsOnDeath()
        {
            var model=Fixture("M27",out _);model.StartBattle();var unit=model.Units[0];var guard=model.Defenders[0];
            Call(model,"Shield",unit,unit,unit.MaxHealth*.4f,8d,"native",false);
            Call(model,"CombatDamage",guard,unit,unit.MaxHealth*.8f,2,false,false,false);
            Assert.That((float)Call(model,"Stack",unit,"hero_shield"),Is.EqualTo(unit.MaxHealth*.1f).Within(.01));
            Call(model,"KillCampaignFighter",unit);Assert.That((float)Call(model,"Stack",unit,"hero_shield"),Is.Zero);
        }
        [Test]public void OnlyOneSameNameBodyReceivesHeroAndSummonsNeverReceiveIt()
        {
            var model=Fixture("M11",out _);var first=model.Units[0];
            var duplicate=new FormationFighter{Id=19,Instance=first.Instance+99,Enabled=true,ArtId=first.ArtId,Star=1};model.Units.Add(duplicate);
            Assert.That((bool)Call(model,"Hero",first,"H11"),Is.True);Assert.That((bool)Call(model,"Hero",duplicate,"H11"),Is.False);
            model.Summons.Add(first);Assert.That((bool)Call(model,"Hero",first,"H11"),Is.False);
        }
        [Test]public void GrowthLeavesFourFiveCostAndFiveCostJackpotIntact()
        {
            var methods=typeof(FormationCampaignRules).GetMethods(BindingFlags.NonPublic|BindingFlags.Static);
            var attack=methods.Single(method=>method.Name=="AttackStarMultiplier"&&method.GetParameters().Length==3);
            float Mult(string id,int star)=>(float)attack.Invoke(null,new object[]{FormationCampaignRules.Find(id),star,3});
            Assert.That(Mult("M11",3),Is.EqualTo(3.6f));Assert.That(Mult("M11",4),Is.EqualTo(6.5f));
            Assert.That(Mult("B04",3),Is.EqualTo(3.24f));Assert.That(Mult("WIS",3),Is.EqualTo(10));
        }
        [Test]public void HeroBecomesAutomaticCarryButManualCarryStillWins()
        {
            var model=Fixture("M11",out var run);model.StartBattle();var hero=model.Units[0];
            var stronger=new FormationFighter{Id=77,Instance=99,Enabled=true,Alive=true,ArtId="WISADEL_APRIL",Attack=9999};model.Units.Add(stronger);
            object Carry()=>typeof(FormationBattleModel).GetProperty("Carry",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(model);
            Assert.That(Carry(),Is.SameAs(hero));run.MainCarryInstance=99;Assert.That(Carry(),Is.SameAs(stronger));
        }
        [Test]public void TerminalBossUsesTunedTwoPhaseBudgetEvenWithoutAnOrdinaryFormationWing()
        {
            var model=Fixture("M11",out var run);
            typeof(FormationCampaignRun).GetProperty("NodeIndex").SetValue(run,Array.FindIndex(FormationCampaignRules.Nodes,node=>node.Id=="3-7"));
            model.BindCampaign(run);var boss=model.Defenders.Single();Assert.That(boss.IsSnowBoss,Is.True);
            Assert.That(boss.MaxHealth,Is.EqualTo(140000));model.StartBattle();
            Call(model,"CombatDamage",model.Units[0],boss,10000000f,2,true,true,false);
            Assert.That(boss.Alive,Is.True);for(int i=0;i<85;i++)model.Advance(.025f);
            Assert.That(boss.SnowBossPhase,Is.EqualTo(2));Assert.That(boss.MaxHealth,Is.EqualTo(140000));Assert.That(boss.Health,Is.GreaterThan(0));
        }
        [TestCase("M11",2f)][TestCase("M21",2f)][TestCase("B22",2f)]
        [TestCase("M13",1.5f)][TestCase("M16",1.5f)][TestCase("M27",1.5f)]
        [TestCase("M31",1.1f)][TestCase("M42",1.1f)][TestCase("B23",1.1f)]
        public void ThreeStarCapstoneStrengthensOnlyTheChosenBodyNotTheTwoStarOpening(string id,float expected)
        {
            var opening=Fixture(id,out _,2);opening.StartBattle();Assert.That((float)Call(opening,"HeroPromotion",opening.Units[0]),Is.EqualTo(1));
            var mature=Fixture(id,out _,3);mature.StartBattle();Assert.That((float)Call(mature,"HeroPromotion",mature.Units[0]),Is.EqualTo(expected).Within(.0001));
            var spec=FormationCampaignRules.Find(id);
            Assert.That(mature.Units[0].Attack,spec.Attack>0?Is.GreaterThan(spec.Attack*3.24f):Is.GreaterThan(0));
        }
        [Test]public void BeastCastGainsTemporaryShieldAndAttackSpeedButRetryResetsBoth()
        {
            var model=Fixture("B22",out _);var guard=model.Defenders[0];model.Defenders.RemoveRange(1,model.Defenders.Count-1);
            guard.MaxHealth=10000000;guard.Attack=0;guard.Healer=false;guard.Position=new Vector2(15,3);
            model.StartBattle();var unit=model.Units[0];unit.Position=new Vector2(14.5f,3);unit.SkillCharge=unit.SkillRequired;
            for(int step=0;step<20;step++)model.Advance(.025f);
            Assert.That(unit.Shield,Is.GreaterThan(0));Assert.That(unit.Interval,Is.LessThan(FormationCampaignRules.Find("B22").Interval));
            model.Retry();Assert.That(unit.Shield,Is.Zero);Assert.That(unit.Interval,Is.EqualTo(FormationCampaignRules.Find("B22").Interval).Within(.001));
        }
        [Test]public void FocusDoesNotExpireWhileTheOriginalSkillAnimationOwnsTheActorButIdleStillResetsIt()
        {
            var model=Fixture("M16",out _);var guard=model.Defenders[0];model.Defenders.RemoveRange(1,model.Defenders.Count-1);
            guard.MaxHealth=10000000;guard.Attack=0;guard.Healer=false;guard.Position=new Vector2(15,3);model.StartBattle();
            var unit=model.Units[0];unit.Position=new Vector2(14,3);
            model.SetAnimationProfile(unit.ArtId,new FormationAnimationProfile{AttackDuration=.6f,AttackContact=.3f,SkillDuration=5,SkillContact=1,BaseRate=1,HasOrdinaryAttack=true});
            for(int i=0;i<5;i++)Call(model,"HeroOrdinary",unit,guard);
            typeof(FormationActor).GetField("NextAttackAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(unit,100000d);
            unit.SkillCharge=unit.SkillRequired;Call(model,"CastCampaignSkill",unit,guard,false);
            while(model.Clock<4){unit.SkillCharge=0;model.Advance(.025f);}
            Assert.That((bool)Call(model,"HeroFocusExpired",unit),Is.False);Assert.That((float)Call(model,"Stack",unit,"hero_focus"),Is.EqualTo(5));
            while(model.Clock<7.2){unit.SkillCharge=0;model.Advance(.025f);}
            Assert.That((bool)Call(model,"HeroFocusExpired",unit),Is.True);Call(model,"HeroOrdinary",unit,guard);
            Assert.That((float)Call(model,"Stack",unit,"hero_focus"),Is.EqualTo(1));
        }
        [Test]public void GolemDoesNotCopyItsOwnersHeroCapstone()
        {
            var model=Fixture("M27",out var run);var shop=run.Shop;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M27",Star=3},new ShopTrialModel.Piece{Id="CARD_M28",Star=2}},Array.Empty<string>(),8,0,100);
            model.ApplyRoster(shop.Bench.Select((piece,index)=>new FormationRosterEntry{Instance=piece.Instance,ArtId=piece.Id,Star=piece.Star,Spawn=new Vector2(index*5,0)}),8);
            model.StartBattle();var golem=model.Summons.Single(unit=>unit.ArtId=="GOLEM");
            float original=FormationCampaignRules.Find("M27").Attack*3.5f;
            float other=FormationCampaignRules.Find("M28").Attack*1.8f;
            Assert.That(golem.Attack,Is.EqualTo((original+other)/2).Within(.01));
            Assert.That(model.Units[0].Attack,Is.GreaterThan(original));
        }
    }
}
