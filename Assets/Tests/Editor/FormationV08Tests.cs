using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationV08Tests
    {
        static FormationBattleModel Fixture(out FormationCampaignRun run,string[] ids,int star=1,int talulahFee=3)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(spec=>new ShopTrialModel.Role{Id=spec.CardId,Name=spec.Name,Cost=spec.Id=="TAL"?talulahFee:spec.Cost}),81);
            shop.EnableFormationEconomy();run=new FormationCampaignRun(shop,81);int safety=0;while(run.HasBlockingChoice){Assert.That(++safety,Is.LessThan(100));Assert.That(run.Choose(0).Success,Is.True);}
            string[] cards=ids.Select(id=>FormationCampaignRules.Find(id).CardId).ToArray();
            shop.Preset(cards.Select(id=>new ShopTrialModel.Piece{Id=id,Star=star}),Array.Empty<string>(),10,0,1000);
            Assert.That(shop.SetLineup(cards,shop.Bench.Select(piece=>piece.Instance)),Is.True);
            var model=new FormationBattleModel(81);model.BindCampaign(run);
            Assert.That(model.ApplyRoster(shop.Bench.Select((piece,index)=>new FormationRosterEntry{Instance=piece.Instance,Star=piece.Star,ArtId=piece.Id,Spawn=new Vector2(index%5*2,index/5*3)}),14),Is.True);
            return model;
        }
        static object Call(FormationBattleModel model,string method,params object[] args) =>
            typeof(FormationBattleModel).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(info=>info.Name==method&&info.GetParameters().Length==args.Length).Invoke(model,args);
        static void Step(FormationBattleModel model,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.025f);i++)model.Advance(.025f);}
        static FormationDefender Dummy(FormationBattleModel model,float hp=100000000)
        {
            model.Defenders.RemoveRange(1,model.Defenders.Count-1);var guard=model.Defenders[0];guard.Position=new Vector2(15,3);
            guard.MaxHealth=hp;guard.Attack=guard.Armor=guard.MagicResistance=0;guard.Range=.01f;guard.Healer=false;return guard;
        }
        static void Badge(FormationCampaignRun run,string trait,int instance) => Assert.That(run.Equip(run.AddItem("徽章:"+trait).Instance,instance).Success,Is.True);

        [TestCase(1)][TestCase(3)]
        public void SteamNativeContactDelaysDamageAndLocksTheWholeApprovedSequence(int tier)
        {
            var model=Fixture(out var run,tier==3?new[]{"STEAM","B01","M03"}:new[]{"STEAM"});
            if(tier==3){Badge(run,"蒸汽",run.Shop.Bench[1].Instance);Badge(run,"蒸汽",run.Shop.Bench[2].Instance);model.BindCampaign(run);}
            var guard=Dummy(model);Assert.That(model.StartBattle(),Is.True);
            var machine=model.Units.First();machine.Position=new Vector2(14.5f,3);machine.SkillCharge=machine.SkillRequired;
            var hits=new System.Collections.Generic.HashSet<FormationHit>();
            void Tick(){model.Advance(.025f);hits.UnionWith(model.Hits.Where(hit=>hit.FromId==machine.Id&&hit.Skill&&!hit.Secondary));}
            Tick();var action=model.PresentationActions.Single(a=>a.ActorId==machine.Id&&a.Skill);
            Assert.That(action.ContactAt-action.At,Is.EqualTo(2.85).Within(.001));
            while(model.Clock<action.ContactAt-.03)Tick();Assert.That(hits,Is.Empty,"Charging must not deal premature damage.");
            double frozen=model.Clock;model.Paused=true;Step(model,4);Assert.That(model.Clock,Is.EqualTo(frozen));Assert.That(hits,Is.Empty);model.Paused=false;
            while(model.Clock<action.EndAt-.01)Tick();
            int Type(FormationHit hit)=>(int)typeof(FormationHit).GetProperty("DamageType",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hit);
            var physical=hits.Where(hit=>Type(hit)==0).OrderBy(hit=>hit.At).ToArray();
            var arts=hits.Where(hit=>Type(hit)==1).OrderBy(hit=>hit.At).ToArray();
            Assert.That(physical.Length,Is.EqualTo(3));Assert.That(arts.Length,Is.EqualTo(tier==3?3:0));
            for(int index=0;index<3;index++)Assert.That(physical[index].At-action.ContactAt,Is.EqualTo(index*.1).Within(.026));
            Assert.That(model.PresentationActions.Any(a=>a.ActorId==machine.Id&&!a.Skill&&a.At>action.At&&a.At<action.EndAt),Is.False);
            Assert.That(action.EndAt-action.At,Is.EqualTo(tier==3?6.6:3.75).Within(.001));
            Assert.That((double)typeof(FormationActor).GetField("ActionUntil",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(machine),Is.GreaterThanOrEqualTo(action.EndAt-.001));
            if(tier==3)for(int index=0;index<3;index++)Assert.That(arts[index].At-action.At,Is.EqualTo(4.6+index*.4).Within(.026));
        }

        [Test]
        public void SteamDeathCancelsChargedStrikeAndRetryRestoresItsDriverArt()
        {
            var model=Fixture(out _,new[]{"STEAM"});var guard=Dummy(model);model.StartBattle();var machine=model.Units.Single();
            machine.Position=new Vector2(14.5f,3);machine.SkillCharge=machine.SkillRequired;Step(model,.1f);
            Assert.That(model.PresentationActions.Any(a=>a.Skill),Is.True);
            Call(model,"CombatDamage",guard,machine,100000000f,2,false,false,false);Step(model,7);
            Assert.That(model.Hits.Any(hit=>hit.FromId==machine.Id&&hit.Skill),Is.False);
            Assert.That(model.Retry(),Is.True);Assert.That(model.Units.Single().ArtId,Is.EqualTo("STEAM"));
        }

        [TestCase("M20")][TestCase("WIS")][TestCase("B08")][TestCase("B10")][TestCase("B11")][TestCase("B12")][TestCase("B13")][TestCase("B21")]
        public void ThreeStarFiveCostReplacesGenericStatsAndStartsFull(string id)
        {
            var model=Fixture(out var run,new[]{id},3);var spec=FormationCampaignRules.Find(id);Assert.That(model.StartBattle(),Is.True);var unit=model.Units.Single();
            Assert.That(unit.Attack,Is.EqualTo(spec.Attack*10).Within(.01));
            Assert.That(unit.MaxHealth,Is.EqualTo(spec.Health*18*(id=="M20"?4:1)).Within(.1));
            Assert.That(unit.Armor,Is.EqualTo(spec.Armor+50).Within(.01));Assert.That(unit.MagicResistance,Is.EqualTo(Math.Min(80,spec.MagicResistance+30)).Within(.01));
            Assert.That(unit.SkillCharge,Is.EqualTo(unit.SkillRequired));Assert.That((bool)Call(model,"ControlImmune",unit),Is.True);
        }
        [TestCase(3,false)][TestCase(4,false)][TestCase(5,true)]
        public void TalulahThreeStarOnlyFiveFeeUsesFinalRules(int fee,bool final)
        {
            var model=Fixture(out _,new[]{"TAL"},3,fee);Assert.That(model.StartBattle(),Is.True);var unit=model.Units.Single();
            Assert.That(unit.Attack,Is.EqualTo((fee==5?560:fee==4?310:190)*(final?10:3.24f)).Within(.01));
            Assert.That((bool)Call(model,"ControlImmune",unit),Is.EqualTo(final));
            if(final)Assert.That(unit.MaxHealth,Is.EqualTo(2800*18));
        }
        [TestCase("整合")][TestCase("雪怪")][TestCase("泥岩")][TestCase("深池")][TestCase("萨卡兹")][TestCase("海嗣")][TestCase("伊比利亚")][TestCase("蒸汽")]
        public void TransferableFactionsRequireExactlyTwoBadgesForHighestTier(string id)
        {
            int natural=FormationCampaignRules.AllUnits.Count(unit=>unit.Factions.Contains(id));
            Assert.That(FormationCampaignRules.FindRecord(id).Levels.Last(),Is.EqualTo(natural+2));
            Assert.That(FormationCampaignRun.BadgeTraits,Does.Contain(id));
        }
        [Test]
        public void CrownRecipesCountInInventoryAndTripleCrownNeverPaysTwice()
        {
            Fixture(out var run,new[]{"B01"});var shop=run.Shop;
            var crown=run.Combine(run.AddItem("赤宝石").Instance,run.AddItem("赤宝石").Instance);Assert.That(crown.Success,Is.True);
            Assert.That(run.CrownPopulation,Is.EqualTo(1));Assert.That(shop.PopulationCap,Is.EqualTo(11));
            Assert.That(run.Combine(run.AddItem("蓝宝石").Instance,run.AddItem("蓝宝石").Instance).Success,Is.True);
            Assert.That(run.Combine(run.AddItem("赤宝石").Instance,run.AddItem("蓝宝石").Instance).Success,Is.True);
            int owner=shop.Bench.Single().Instance,before=shop.Gold;
            foreach(var item in run.AllEquipment.Where(item=>FormationCampaignRun.CrownKeys.Contains(item.Key)).ToArray())Assert.That(run.Equip(item.Instance,owner).Success,Is.True);
            Assert.That(run.TripleCrownUnlocked,Is.True);Assert.That(shop.Gold,Is.EqualTo(before+300));
            foreach(var item in run.AllEquipment.Where(item=>FormationCampaignRun.CrownKeys.Contains(item.Key)).ToArray()){run.Unequip(item.Instance);run.Equip(item.Instance,owner);}
            Assert.That(shop.Gold,Is.EqualTo(before+300));Assert.That(shop.PopulationCap,Is.EqualTo(13));
            run.AddItem("CROWN");run.AddItem("AEGIS");Assert.That(shop.PopulationCap,Is.EqualTo(14));Assert.That(shop.Level,Is.EqualTo(10));
            Assert.That(FormationCampaignRules.AllItems.Length,Is.EqualTo(21));
        }
        [Test]
        public void ThreeIdenticalCrownsCannotUnlockIncome()
        {
            Fixture(out var run,new[]{"B01"});int owner=run.Shop.Bench.Single().Instance;
            Assert.That(run.Equip(run.AddItem("CROWN").Instance,owner).Success,Is.True);
            Assert.That(run.Equip(run.AddItem("CROWN").Instance,owner).Success,Is.False);
            run.AddItem("CROWN");Assert.That(run.TripleCrownUnlocked,Is.False);
        }
        [Test]
        public void TradeIsOncePerPlaneAndModeSwitchIsLimitedAndLocked()
        {
            var model=Fixture(out var run,new[]{"B10","WIS"});int gold=run.Shop.Gold;
            Assert.That(run.MilitaryTrade(2).Success,Is.True);Assert.That(run.Shop.Gold,Is.EqualTo(gold-8));Assert.That(run.FreeRefreshes,Is.GreaterThanOrEqualTo(4));
            Assert.That(run.MilitaryTrade(1).Success,Is.False);run.OnShopChanged();Assert.That(run.MilitaryTradeAvailable,Is.False);
            Assert.That(run.SelectWisadelMode(1),Is.True);Assert.That(run.SelectWisadelMode(2),Is.True);Assert.That(run.SelectWisadelMode(3),Is.False);
            Assert.That(model.StartBattle(),Is.True);Assert.That(run.SelectWisadelMode(1),Is.False);model.Retry();Assert.That(run.SelectWisadelMode(1),Is.True);
            Assert.That(run.MilitaryTradeAvailable,Is.False);
        }
        [Test]
        public void FiveCostControlImmunityAndSaintLethalSavePrecedeDeath()
        {
            var model=Fixture(out _,new[]{"B13","B01"},3);var enemy=Dummy(model);model.StartBattle();var soldier=model.Units.Last();var saint=model.Units.First();
            Call(model,"GiveBuff",saint,"stun",1f,4d);Assert.That((float)Call(model,"Buff",saint,"stun"),Is.Zero);
            int deaths=soldier.Deaths;Call(model,"CombatDamage",enemy,soldier,1000000f,2,false,false,false);
            Assert.That(soldier.Alive,Is.True);Assert.That(soldier.Health,Is.EqualTo(soldier.MaxHealth));Assert.That(soldier.Deaths,Is.EqualTo(deaths));
            Call(model,"CombatDamage",enemy,soldier,1000000f,2,false,false,false);Assert.That(soldier.Alive,Is.False);
        }
        [Test]
        public void SteamDriversProduceOneBodyAndNoHiddenDriverDeathOrReturn()
        {
            var model=Fixture(out var run,new[]{"STEAM","B01","M03"});var enemy=Dummy(model);
            Badge(run,"蒸汽",run.Shop.Bench[1].Instance);Badge(run,"蒸汽",run.Shop.Bench[2].Instance);model.BindCampaign(run);
            float hp=model.Units.Sum(unit=>unit.MaxHealth),attack=model.Units.Max(unit=>unit.Attack)+(model.Units.Sum(unit=>unit.Attack)-model.Units.Max(unit=>unit.Attack))*.5f;
            Assert.That(model.StartBattle(),Is.True);var machine=model.Units.Single(unit=>unit.Alive);
            Assert.That(machine.ArtId,Is.EqualTo("STEAM"));Assert.That(machine.MaxHealth,Is.EqualTo(hp*1.6f*2).Within(.1));Assert.That(machine.Attack,Is.EqualTo(attack*2).Within(.1));
            Step(model,1);Assert.That(model.Units.Count(unit=>unit.Alive),Is.EqualTo(1));Assert.That(model.Units.Skip(1).All(unit=>unit.Deaths==0&&unit.RespawnRemaining==0),Is.True);
            Call(model,"CombatDamage",enemy,machine,100000000f,2,false,false,false);Step(model,8.1f);
            Assert.That(machine.Alive,Is.True);Assert.That(model.Units.Count(unit=>unit.Alive),Is.EqualTo(1));
            model.Retry();Assert.That(model.Units.Count(unit=>unit.Alive),Is.EqualTo(3));
        }
        [Test]
        public void SeaSixCoversInitialBodyCellsWithDeterministicPaths()
        {
            var model=Fixture(out var run,new[]{"M33","M34","M35","M36","B01","M03"});
            Badge(run,"海嗣",run.Shop.Bench[4].Instance);Badge(run,"海嗣",run.Shop.Bench[5].Instance);model.BindCampaign(run);model.RefreshSeaPreview();
            var first=model.SeaCells.ToArray();foreach(var unit in model.Units)Assert.That(first,Does.Contain(unit.Spawn));
            model.RefreshSeaPreview();Assert.That(model.SeaCells,Is.EqualTo(first));Assert.That(first.Distinct().Count(),Is.EqualTo(first.Length));
        }
        [Test]
        public void MudrockFourCannotReachFinalCultivation()
        {
            var model=Fixture(out var run,new[]{"M42","M27","M28","B01"},3);Badge(run,"泥岩",run.Shop.Bench.Last().Instance);
            typeof(FormationCampaignRun).GetProperty("MudrockXP").SetValue(run,79);
            typeof(FormationCampaignRun).GetMethod("AwardMudrockExperience",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,null);
            Assert.That(run.MudrockXP,Is.EqualTo(79));model.BindCampaign(run);model.StartBattle();Assert.That(model.Summons.Single().Name,Is.EqualTo("泥岩巨像 4"));
        }
        [Test]
        public void SuiThreeStarClearsThenPhysicallyEntersGate()
        {
            var model=Fixture(out var run,new[]{"B21"},3);var enemy=Dummy(model,10000);Assert.That(run.SelectSuiCentre(new Vector2Int(2,3)),Is.True);model.StartBattle();
            var sui=model.Units.Single();sui.Position=new Vector2(14,3);Step(model,5);
            Assert.That(enemy.Alive,Is.False);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Exiting));
            Step(model,60);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void DeepSevenBurnIsOneLockedPercentTickAndAppliesToReinforcements()
        {
            var model=Fixture(out var run,new[]{"M29","M30","M31","M32","B07","B01","M03"});
            Badge(run,"深池",run.Shop.Bench[5].Instance);Badge(run,"深池",run.Shop.Bench[6].Instance);model.BindCampaign(run);
            var enemy=Dummy(model);model.StartBattle();foreach(var unit in model.Units)typeof(FormationActor).GetField("ActionUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(unit,1000d);
            float start=enemy.Health;Step(model,1.05f);Assert.That(start-enemy.Health,Is.EqualTo(1000000).Within(10));
            enemy.MaxHealth*=2;start=enemy.Health;Step(model,1);Assert.That(start-enemy.Health,Is.EqualTo(1000000).Within(10));
            Assert.That((float)Call(model,"Buff",enemy,"eternal_heal_down"),Is.EqualTo(.5f));
            var reinforcement=new FormationDefender{Id=999,Alive=true,Health=10000,MaxHealth=10000,Position=new Vector2(20,7)};model.Defenders.Add(reinforcement);
            Step(model,1);Assert.That(reinforcement.Health,Is.EqualTo(9900).Within(.1));
        }
        [Test]
        public void SnowSixWindUsesBattleClockAndDoesNotFreezeWithoutSecondCold()
        {
            var model=Fixture(out var run,new[]{"M11","M12","M13","M14","B01","M03"});
            Badge(run,"雪怪",run.Shop.Bench[4].Instance);Badge(run,"雪怪",run.Shop.Bench[5].Instance);model.BindCampaign(run);
            var enemy=Dummy(model);model.StartBattle();foreach(var unit in model.Units)typeof(FormationActor).GetField("ActionUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(unit,1000d);
            Step(model,1.9f);Assert.That((float)Call(model,"Buff",enemy,"cold"),Is.Zero);Step(model,.2f);
            Assert.That((float)Call(model,"Buff",enemy,"cold"),Is.GreaterThan(0));Assert.That((float)Call(model,"Buff",enemy,"freeze"),Is.Zero);
            Call(model,"Cold",enemy);Assert.That((float)Call(model,"Buff",enemy,"freeze"),Is.GreaterThan(0));
            model.Paused=true;double clock=model.Clock;Step(model,20);Assert.That(model.Clock,Is.EqualTo(clock));model.Paused=false;
            Step(model,8);Assert.That(model.BattleEvents.Count(entry=>entry.Contains("雪怪寒风")),Is.EqualTo(2));
        }
        [Test]
        public void SaintTwelveSegmentsCountEachAreaSegmentOnceAndNeverRecurse()
        {
            var model=Fixture(out _,new[]{"B13","M03"},3);var first=Dummy(model);model.StartBattle();
            var second=new FormationDefender{Id=999,Alive=true,Health=10000000,MaxHealth=10000000,Position=first.Position};model.Defenders.Add(second);
            var saint=model.Units[0];var recipient=model.Units[1];
            Assert.That((bool)Call(model,"TryV08Skill",saint,first,null,null),Is.True);
            float bonus=(float)Call(model,"Buff",recipient,"saint_damage");
            for(int index=0;index<15;index++)Call(model,"NativeSegment",recipient,new[]{first,second},1f,2,true,0f,true,true);
            Assert.That(recipient.DamageDone,Is.EqualTo(30+bonus*24).Within(30));
        }
        [Test]
        public void IberiaExecutionUsesStrictSeparateBossThresholdAndExcludesSummons()
        {
            var model=Fixture(out var run,new[]{"B23","B25"});run.SelectIberia(2,0);var enemy=Dummy(model,100);model.StartBattle();var owner=model.Units[0];
            enemy.Health=12;Assert.That((bool)Call(model,"V08Execute",owner,enemy),Is.False);
            enemy.Health=11.99f;Assert.That((bool)Call(model,"V08Execute",owner,enemy),Is.True);
            var state=Call(model,"State",enemy);state.GetType().GetField("Boss").SetValue(state,true);
            enemy.Health=5;Assert.That((bool)Call(model,"V08Execute",owner,enemy),Is.False);
            enemy.Health=4.99f;model.Summons.Add(owner);Assert.That((bool)Call(model,"V08Execute",owner,enemy),Is.False);model.Summons.Remove(owner);
            Assert.That((bool)Call(model,"V08Execute",owner,enemy),Is.True);
        }
        [Test]
        public void CrownAndCannottNodeIncomeUsePersistentUniqueNodeKeys()
        {
            Fixture(out var run,new[]{"B10"},3);int owner=run.Shop.Bench.Single().Instance;
            foreach(string key in FormationCampaignRun.CrownKeys)run.Equip(run.AddItem(key).Instance,owner);
            int gold=run.Shop.Gold;run.NotifyCannottThreeStar(owner);run.NotifyCannottThreeStar(owner);Assert.That(run.Shop.Gold,Is.EqualTo(gold+300));
            foreach(var item in run.AllEquipment.ToArray())run.Unequip(item.Instance);
            typeof(FormationCampaignRun).GetProperty("NodeIndex").SetValue(run,1);
            var enter=typeof(FormationCampaignRun).GetMethod("EnterV08Node",BindingFlags.Instance|BindingFlags.NonPublic);enter.Invoke(run,null);enter.Invoke(run,null);
            Assert.That(run.Shop.Gold,Is.EqualTo(gold+500));
        }

        [TestCase("TAL",150f)][TestCase("B08",200f)][TestCase("B12",200f)][TestCase("B21",200f)]
        public void ThreeStarFiveCostOpeningTrueDamageUsesTheNewBudget(string id,float coefficient)
        {
            var model=Fixture(out _,new[]{id},3,5);var enemy=Dummy(model);model.StartBattle();var unit=model.Units.Single();unit.Position=new Vector2(14.5f,3);
            Assert.That((bool)Call(model,"CastCampaignSkill",unit,enemy,false),Is.True);
            var hits=new System.Collections.Generic.HashSet<FormationHit>();
            for(int index=0;index<140;index++){model.Advance(.025f);hits.UnionWith(model.Hits.Where(hit=>hit.FromId==unit.Id&&hit.ToId==enemy.Id&&hit.Skill&&(int)typeof(FormationHit).GetProperty("DamageType",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hit)==2));}
            Assert.That(hits.Sum(hit=>hit.Amount),Is.EqualTo(unit.Attack*coefficient).Within(50));
        }

        [Test]
        public void PatriotPhaseBlastUsesRestoredLiveAuraHealthExactlyOnce()
        {
            var model=Fixture(out _,new[]{"M20"},3);var enemy=Dummy(model);model.StartBattle();var patriot=model.Units.Single();float health=patriot.MaxHealth;
            Call(model,"CombatDamage",enemy,patriot,100000000f,2,false,false,false);
            Assert.That(patriot.Alive,Is.False);Assert.That(patriot.MaxHealth,Is.EqualTo(health/4).Within(.1));Step(model,4.1f);
            Assert.That(patriot.Alive,Is.True);Assert.That(patriot.MaxHealth,Is.EqualTo(health).Within(.1));
            Assert.That(patriot.DamageDone,Is.EqualTo(health*10).Within(10));
        }
        [Test]
        public void NewFactionTopTierIsPrismaticWithoutPromotingUnchangedStyles()
        {
            Assert.That((int)FormationTraitView.GradeFor(FormationCampaignRules.FindRecord("雪怪"),6),Is.EqualTo(4));
            Assert.That((int)FormationTraitView.GradeFor(FormationCampaignRules.FindRecord("迅攻"),6),Is.EqualTo(3));
        }
        [Test]
        public void HiddenMainCarryRedirectsToMechBeforeAnyUnrelatedHighAttackBody()
        {
            var model=Fixture(out var run,new[]{"STEAM","B01","M03","B12"});
            Badge(run,"蒸汽",run.Shop.Bench[1].Instance);Badge(run,"蒸汽",run.Shop.Bench[2].Instance);model.BindCampaign(run);
            run.MainCarryInstance=run.Shop.Bench[1].Instance;model.StartBattle();var outsider=model.Units.Last();outsider.Attack=1000000;
            var carry=(FormationFighter)typeof(FormationBattleModel).GetProperty("Carry",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(model);
            Assert.That(carry,Is.SameAs(model.Units.First()));Assert.That(carry.ArtId,Is.EqualTo("STEAM"));
        }
        [Test]
        public void EveryV08TraitAndPopulationItemHasPackagedIconArt()
        {
            foreach(var trait in FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles))Assert.That(SynergyBadgePresentation.IconFor(trait.Name),Is.Not.Null,trait.Name);
            var lookup=typeof(FormationBattleController).GetMethod("SupplyItemArt",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var item in FormationCampaignRules.AllPopulationItems)Assert.That(lookup.Invoke(null,new object[]{item.Id}),Is.Not.Null,item.Name);
        }
    }
}
