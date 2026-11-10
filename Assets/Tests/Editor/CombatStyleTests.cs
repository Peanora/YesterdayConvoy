using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class CombatStyleTests
    {
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,10,20,1,.1f,1000,.1f,1,1,1,1);
        static EngagementSettings Combat(string id,MonsterEffectSettings effects=default,bool skill=false)
            =>new EngagementSettings(unitAttack:100,unitArmor:0,unitRange:20,unitInterval:2,projectileTime:.05f,
                initialSP:0,requiredSP:skill?100:0,spPerSecond:skill?.001f:0,skillAmmo:skill?2:0,skillAttack:skill?100:0,
                skillInterval:skill?2:0,skillRange:skill?20:0,hasSkill:skill,melee:CombatStyleRules.For(id)==CombatStyle.Assault||CombatStyleRules.For(id)==CombatStyle.Bulwark||CombatStyleRules.For(id)==CombatStyle.Frenzy,effects:id=="CARD_M25"?new MonsterEffectSettings(healer:true):effects,canAttack:id!="CARD_M25");
        static ManualBattle Create(string[] ids,float armor=0,bool guardsAttack=false,float guardAttack=10,MonsterEffectSettings effects=default,bool cluster=false,bool enabled=true,int guardBlocks=10)
        {
            var lanes=Enumerable.Range(0,10).Select(i=>new DeploymentLane("red"+i,"blue"+i,new[]{new Vector3(0,0,i*1.4f),new Vector3(100,0,i*1.4f)})).ToArray();
            var guards=new[]{new DefenderSettings("main","主目标",null,Vector3.right,100000,guardAttack,armor,20,1,blocks:guardBlocks,blockRadius:10,canAttack:guardsAttack)}.ToList();
            if(cluster){guards.Add(new DefenderSettings("near","近目标",null,new Vector3(1.5f,0,0),100000,1,0,.1f,100,canAttack:false));guards.Add(new DefenderSettings("far","远目标",null,new Vector3(4,0,0),100000,1,0,.1f,100,canAttack:false));}
            var b=new ManualBattle(Unit(ids[0]),lanes[0].Points,Vector3.right,Combat(ids[0],effects,ids[0]=="CARD_WISADEL_APRIL"),roster:guards,deploymentLanes:lanes);
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(id,effects,id=="CARD_WISADEL_APRIL"),true)));
            b.ConfigureSynergies(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),form:InfectionForm.Shell,combatStyles:enabled);b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,int lane=0,long request=1)
        {var receipt=b.TryDeployRole(b.RunId,request,b.Lanes[lane].Entry,id);Assert.That(receipt.Success,Is.True,receipt.Failure.ToString());return b.FindUnit(receipt.EntityId);}
        static string[] Members(CombatStyle style,int n)=>CombatStyleRules.Find(style).Members.Take(n).ToArray();

        [Test]public void FortyTwoKnownCardsHaveOneStyleAndSilverWolfRemainsDeferred()
        {
            var ids=CombatStyleRules.Definitions.SelectMany(d=>d.Members).ToArray();Assert.That(ids.Length,Is.EqualTo(42));Assert.That(ids.Distinct().Count(),Is.EqualTo(42));
            CollectionAssert.AreEquivalent(CampaignBalance.Profiles.Keys.Where(CampaignBalance.ShopEligible),ids);
            Assert.That(CombatStyleRules.For("CARD_M41"),Is.EqualTo(CombatStyle.None));Assert.That(CombatStyleRules.For("UNKNOWN"),Is.EqualTo(CombatStyle.None));
        }
        [TestCase(CombatStyle.Assault,2)][TestCase(CombatStyle.Assault,4)][TestCase(CombatStyle.Assault,6)]
        [TestCase(CombatStyle.Marksman,2)][TestCase(CombatStyle.Marksman,4)][TestCase(CombatStyle.Marksman,6)]
        [TestCase(CombatStyle.Mystic,2)][TestCase(CombatStyle.Mystic,4)][TestCase(CombatStyle.Mystic,6)]
        [TestCase(CombatStyle.Bulwark,2)][TestCase(CombatStyle.Bulwark,4)][TestCase(CombatStyle.Bulwark,6)]
        [TestCase(CombatStyle.Frenzy,2)][TestCase(CombatStyle.Frenzy,4)][TestCase(CombatStyle.Frenzy,6)]
        [TestCase(CombatStyle.Coordination,2)][TestCase(CombatStyle.Coordination,4)]
        public void FormationAndDetailAgreeOnHighestTierWithoutChangingFactionTags(CombatStyle style,int tier)
        {
            var ids=Members(style,tier);var b=Create(ids);Assert.That(b.StyleTier(style),Is.EqualTo(tier));
            var summary=SynergySummary.From(ids.Concat(ids),includeCombatStyles:true).Single(s=>s.Name==CombatStyleRules.Find(style).Name);
            Assert.That(summary.Tier,Is.EqualTo(tier));Assert.That(summary.Count,Is.EqualTo(tier));
            var ui=SynergyDetailUiModel.Build(summary,true,CampaignBalance.Profiles.Keys);Assert.That(ui.tiers.Single(t=>t.active).tier,Is.EqualTo(tier));Assert.That(ui.members.Count(m=>m.selected),Is.EqualTo(tier));Assert.That(ui.members.All(m=>!m.transfer),Is.True);
            Assert.That(ui.members.Length,Is.EqualTo(CombatStyleRules.Find(style).Members.Count));Assert.That(CampaignBalance.Tags(ids[0]),Does.Not.Contain(summary.Name));
        }
        [TestCase(2,.2f)][TestCase(4,.35f)][TestCase(6,.5f)]public void AssaultFirstHitMarksThenPhysicalArmorIsIgnoredOnlyForDirectDamage(int tier,float ignored)
        {
            var ids=Members(CombatStyle.Assault,tier);var b=Create(ids,armor:40);var u=Deploy(b,ids[0]);b.Advance(.2f);
            Assert.That(b.StyleRuptureRemaining(b.Defender),Is.GreaterThan(0));var first=b.CombatShots.First(s=>!s.FromDefender&&s.Applied);
            Assert.That(first.DamageDealt,Is.EqualTo(first.Attack-40).Within(.01));b.Advance(2.1f);
            var second=b.CombatShots.Where(s=>!s.FromDefender&&s.Applied).Skip(1).First();Assert.That(second.DamageDealt,Is.EqualTo(second.Attack-40*(1-ignored)).Within(.02));
            float hp=b.Defender.Health;typeof(ManualBattle).GetMethod("DamageDefenderFrom",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{b.Defender,100f,CombatDamageType.Physical,u,true,1f});
            Assert.That(hp-b.Defender.Health,Is.EqualTo(60).Within(.01),"Derived packets do not borrow armor penetration.");
        }
        [Test]public void AssaultCannotMarkAnUnblockedRangedGuard()
        {
            var b=Create(new[]{"CARD_M13","CARD_M37"},guardBlocks:0);Deploy(b,"CARD_M37");b.Advance(4.3f);
            Assert.That(b.CombatShots.Count(s=>!s.FromDefender&&s.Applied),Is.EqualTo(3));Assert.That(b.StyleRuptureRemaining(b.Defender),Is.Zero);
        }
        [Test]public void TwoMarksmanKindsExposeButRepeatedEntitiesOfOneKindDoNot()
        {
            var b=Create(new[]{"CARD_M08","CARD_M11"});Deploy(b,"CARD_M08");b.Advance(.11f);Deploy(b,"CARD_M08",1,2);b.Advance(.2f);
            Assert.That(b.StyleExposureRemaining(b.Defender),Is.Zero);Deploy(b,"CARD_M11",2,3);b.Advance(.2f);Assert.That(b.StyleExposureRemaining(b.Defender),Is.GreaterThan(3));
            b.Advance(2.1f);var shots=b.CombatShots.Where(s=>!s.FromDefender&&s.Applied).ToArray();Assert.That(shots.Last().DamageDealt,Is.EqualTo(shots.Last().Attack*1.06f).Within(.02));
            Assert.That(b.CombatStatuses.Any(s=>s.Status==CombatFeedbackStatus.Exposed),Is.True);
        }
        [Test]public void MysticCountsActionsNotSplashTargetsAndKeepsItsRadius()
        {
            var b=Create(new[]{"CARD_M09","CARD_M42"},effects:new MonsterEffectSettings(attackTargets:3),cluster:true);Deploy(b,"CARD_M09");b.Advance(.2f);
            Assert.That(b.StylePulses,Is.Zero);Assert.That(b.StyleMysticProgress(b.Units[0]),Is.EqualTo(1));b.Advance(4.1f);
            Assert.That(b.StylePulses,Is.EqualTo(1));Assert.That(b.StyleMysticProgress(b.Units[0]),Is.Zero);
            Assert.That(b.FindDefender("near").Health,Is.EqualTo(100000-300-40).Within(.02));Assert.That(b.FindDefender("far").Health,Is.EqualTo(100000-300).Within(.02));
            Assert.That(b.CombatFeedback.Any(e=>e.Status==CombatFeedbackStatus.ArcanePulse),Is.True);Assert.That(b.Orders,Is.Zero);
        }
        [Test]public void BulwarkStartsAfterSurvivingTheFirstRealHitSharesAndNeverStacks()
        {
            var b=Create(new[]{"CARD_M07","CARD_M10","CARD_M08"},guardsAttack:true);var a=Deploy(b,"CARD_M07");var ally=Deploy(b,"CARD_M08",1,2);b.Advance(.2f);
            Assert.That(b.StyleWards,Is.EqualTo(1));Assert.That(b.StyleWardRemaining(a),Is.EqualTo(a.MaxHealth*.08f).Within(.01));Assert.That(b.StyleWardRemaining(ally),Is.EqualTo(ally.MaxHealth*.08f).Within(.01));
            float hp=a.Health;Assert.That(b.TryDamageUnit(a.EntityId,20,CombatDamageType.Physical));Assert.That(a.Health,Is.EqualTo(hp));Assert.That(b.StyleWardRemaining(a),Is.EqualTo(a.MaxHealth*.08f-20).Within(.01));
            b.SetPaused(true);double clock=b.Clock;float shield=b.StyleWardRemaining(a);b.Advance(99);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.StyleWardRemaining(a),Is.EqualTo(shield));
            b.SetPaused(false);b.Advance(5.1f);Assert.That(b.StyleWardRemaining(a),Is.Zero);Assert.That(b.StyleWards,Is.EqualTo(1));
        }
        [Test]public void LethalFirstHitDoesNotGrantBulwarkOrRage()
        {
            var b=Create(new[]{"CARD_M07","CARD_M10"},guardsAttack:true,guardAttack:100000);var u=Deploy(b,"CARD_M07");b.Advance(.2f);Assert.That(u.Active,Is.False);Assert.That(b.StyleWards,Is.Zero);
            b=Create(new[]{"CARD_M02","CARD_M15"});u=Deploy(b,"CARD_M02");b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.Zero);
        }
        [TestCase(2,.15f,.08f)][TestCase(4,.25f,.12f)][TestCase(6,.35f,.16f)]public void FrenzyUsesOnePrimaryTargetAndAnActionWideHealingLimit(int tier,float attackBonus,float leech)
        {
            var ids=Members(CombatStyle.Frenzy,tier);var b=Create(ids,effects:new MonsterEffectSettings(attackTargets:3),cluster:true);var u=Deploy(b,ids[0]);
            float normal=b.UnitAttackMultiplier(u);b.TryDamageUnit(u.EntityId,u.MaxHealth*.8f,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.EqualTo(1));Assert.That(b.UnitAttackMultiplier(u),Is.EqualTo(normal+attackBonus).Within(.001));
            float hp=u.Health;b.Advance(.2f);var first=b.CombatShots.First(s=>!s.FromDefender&&s.Applied);
            Assert.That(u.Health-hp,Is.EqualTo(Math.Min(first.DamageDealt*leech,u.MaxHealth*.05f)).Within(.02),"Secondary targets cannot multiply healing.");
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.EqualTo(1));b.SetPaused(true);double rage=b.StyleRageRemaining(u);b.Advance(100);Assert.That(b.StyleRageRemaining(u),Is.EqualTo(rage));
        }
        [Test]public void CoordinationRequiresDistinctOriginalKindsAndCanOnlyRestoreSixteenSPPerRecipient()
        {
            var ids=Members(CombatStyle.Coordination,4).Concat(new[]{"CARD_WISADEL_APRIL"}).ToArray();var b=Create(ids);var receiver=Deploy(b,"CARD_WISADEL_APRIL",5,1);
            Deploy(b,ids[0],0,2);b.Advance(.11f);Deploy(b,ids[0],1,3);b.Advance(.2f);Assert.That(b.StyleCoordinations,Is.Zero);
            Deploy(b,ids[1],2,4);b.Advance(.2f);Assert.That(b.StyleCoordinations,Is.EqualTo(1));Assert.That(b.StyleSPGranted,Is.EqualTo(8));
            b.TryDamageUnit(receiver.EntityId,300,CombatDamageType.Physical);
            Deploy(b,ids[2],3,5);Deploy(b,ids[3],4,6);b.Advance(.2f);Assert.That(b.StyleCoordinations,Is.EqualTo(2));Assert.That(b.StyleSPGranted,Is.EqualTo(16));Assert.That(receiver.SkillSP,Is.EqualTo(16).Within(.01));
            Assert.That(b.Units.Single(x=>x.CardId=="CARD_M25").HealingDone,Is.GreaterThan(0));
            b.Advance(10);Assert.That(b.StyleCoordinations,Is.EqualTo(2));Assert.That(b.StyleSPGranted,Is.EqualTo(16));Assert.That(receiver.SkillAmmo,Is.Zero);
            b.Restart();Assert.That(b.StyleCoordinations,Is.Zero);Assert.That(b.StyleSPGranted,Is.Zero);Assert.That(b.StyleTier(CombatStyle.Coordination),Is.EqualTo(4));Assert.That(b.CombatFeedback,Is.Empty);
        }
        [Test]public void CoordinationClampsAtRequiredSPAndCannotBankItsOverflowOrReissueFromOldSources()
        {
            var b=Create(new[]{"CARD_M05","CARD_M17","CARD_WISADEL_APRIL"});var receiver=Deploy(b,"CARD_WISADEL_APRIL",2,1);
            typeof(DeployedUnitState).GetProperty("SkillSP").SetValue(receiver,99f);
            Deploy(b,"CARD_M05",0,2);Deploy(b,"CARD_M17",1,3);b.Advance(.2f);
            Assert.That(receiver.SkillSP,Is.EqualTo(100));Assert.That(b.StyleSPGranted,Is.InRange(.99f,1f));
            Assert.That(b.TryActivateSkill(b.RunId,777,receiver.EntityId),Is.EqualTo(SkillFailure.None));Assert.That(receiver.SkillSP,Is.Zero);
            b.Advance(2.1f);Assert.That(b.StyleCoordinations,Is.EqualTo(1));Assert.That(receiver.SkillSP,Is.LessThan(.01f));
        }
        [Test]public void ExpiredFrenzyDoesNotRetriggerAfterNativeRevival()
        {
            var b=Create(new[]{"CARD_M02","CARD_M15"},effects:new MonsterEffectSettings(frostNova:true));var u=Deploy(b,"CARD_M02");
            b.TryDamageUnit(u.EntityId,u.MaxHealth*.8f,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.EqualTo(1));b.Advance(8.2f);Assert.That(b.StyleRageRemaining(u),Is.Zero);
            b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);Assert.That(u.IsReviving,Is.True);b.Advance(5.2f);Assert.That(u.IsReviving,Is.False);
            b.TryDamageUnit(u.EntityId,u.MaxHealth*.6f,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.EqualTo(1));Assert.That(b.StyleRageRemaining(u),Is.Zero);
        }
        [Test]public void WaveChangesPreserveFrenzyEntityTimersAndSnapshotInsteadOfRearmingThem()
        {
            var b=Create(new[]{"CARD_M02","CARD_M15"});b.Restart();
            b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("red0",16,1),new BattleWaveDefinition("red1",12,1)},100));b.Begin();
            var u=Deploy(b,"CARD_M02");b.TryDamageUnit(u.EntityId,u.MaxHealth*.8f,CombatDamageType.Physical);b.Advance(1.2f);
            Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));Assert.That(b.Units.Single(),Is.SameAs(u));Assert.That(b.StyleRages,Is.EqualTo(1));Assert.That(b.StyleTier(CombatStyle.Frenzy),Is.EqualTo(2));
            Assert.That(b.StyleRageRemaining(u),Is.EqualTo(6.8).Within(.01));b.SetPaused(true);b.Advance(10);Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));Assert.That(b.StyleRageRemaining(u),Is.EqualTo(6.8).Within(.01));
        }
        [Test]public void OldFactionOnlyFixturesRemainIsolatedAndNewScenesUseRealInventory()
        {
            var b=Create(new[]{"CARD_M02","CARD_M15"},enabled:false);var u=Deploy(b,"CARD_M02");b.TryDamageUnit(u.EntityId,u.MaxHealth*.6f,CombatDamageType.Physical);Assert.That(b.StyleRages,Is.Zero);Assert.That(b.CombatStylesEnabled,Is.False);
            Assert.That(SynergyTrialCatalog.StyleEntries.Count,Is.EqualTo(6));
            foreach(var entry in SynergyTrialCatalog.StyleEntries)
            {
                var data=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(entry.DefinitionPath);Assert.That(data,Is.Not.Null,entry.DefinitionPath);data.Validate();
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.ScenePath),Is.Not.Null);
                var summary=SynergySummary.From(data.initialMembers.Select(m=>m.cardId),includeCombatStyles:true).Single(s=>s.Name==entry.Tag);Assert.That(summary.Tier,Is.EqualTo(entry.MaximumTier));
                var shop=new ShopTrialModel(CampaignBalance.Profiles.Select(p=>new ShopTrialModel.Role{Id=p.Key,Name=p.Key,Cost=p.Value.Cost}),44);shop.EnableCampaign(true);shop.Preset(data.StartingPieces(),Array.Empty<string>(),8,0,10);shop.Validate();Assert.That(shop.UnlimitedGold,Is.True);
                foreach(var p in data.initialMembers)Assert.That(shop.Available(p.cardId),Is.EqualTo(shop.Capacity(p.cardId)-1));
            }
        }
    }
}
