using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class SarkazContractTests
    {
        static readonly string[] Members={"CARD_M37","CARD_M38","CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M24","CARD_M19","CARD_M20","CARD_M28","CARD_WISADEL_APRIL"};
        const BindingFlags State=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,1000,.1f,1,1,1,1);
        static EngagementSettings Combat(bool near,MonsterEffectSettings effects=default)=>new EngagementSettings(
            unitAttack:100,unitArmor:0,unitRange:2,unitInterval:100,defenderHealth:1000000,defenderAttack:1,
            defenderArmor:0,defenderRange:.01f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,
            skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,attackDuration:.3f,
            attackWindup:.1f,canAttack:near,effects:effects);
        static ManualBattle Make(int count,string contract="CARD_M37",bool near=false,string[] members=null,string[] extra=null,
            string nativeId=null,MonsterEffectSettings nativeEffects=default)
        {
            var ids=(members??Members.Take(count)).Concat(extra??new[]{"CARD_M07"}).Distinct().ToArray();
            EngagementSettings For(string id)=>Combat(near,id==nativeId?nativeEffects:default);
            var position=near?Vector3.right*.5f:new Vector3(500,0,50);
            var guard=new DefenderSettings("guard","Guard",null,position,1000000,1,0,.01f,100,canAttack:false);
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.right*10000},position,For(ids[0]),roster:new[]{guard});
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),For(id),true)));
            b.ConfigureSynergies(ids.Select(Piece),contract:contract);b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,long request)
        {
            var receipt=b.TryDeployRole(b.RunId,request,b.Entry,id);Assert.That(receipt.Success,Is.True,receipt.Failure.ToString());
            return b.FindUnit(receipt.EntityId);
        }
        static void Set(DeployedUnitState u,string property,object value)=>typeof(DeployedUnitState).GetProperty(property,State).SetValue(u,value);

        [TestCase(1,0)][TestCase(2,2)][TestCase(3,2)][TestCase(4,4)][TestCase(5,4)]
        [TestCase(6,6)][TestCase(7,6)][TestCase(8,8)][TestCase(9,9)][TestCase(10,9)][TestCase(11,9)]
        public void TiersUseFormationKindsRatherThanDeployedBodies(int count,int expected)
        {
            var b=Make(count);Assert.That(b.SarkazTier,Is.EqualTo(expected));Assert.That(b.Units,Is.Empty);
            var first=Deploy(b,"CARD_M37",1);b.Advance(.02f);Deploy(b,"CARD_M37",2);
            Assert.That(b.SarkazTier,Is.EqualTo(expected));b.TryDamageUnit(first.EntityId,100000,CombatDamageType.Physical);Assert.That(b.SarkazTier,Is.EqualTo(expected));
            var summary=SynergySummary.From(Members.Take(count)).Single(row=>row.Name=="萨卡兹");
            CollectionAssert.AreEqual(new[]{2,4,6,8,9},summary.Thresholds);Assert.That(summary.Tier,Is.EqualTo(expected));
        }

        [TestCase(2,1100,.35f,220)][TestCase(4,1200,.60f,360)][TestCase(6,1300,.90f,520)]
        [TestCase(8,1400,1.20f,700)][TestCase(9,1600,1.50f,960)]
        public void DeploymentImmediatelyGrantsApprovedStatsAndOneShieldThatAbsorbsRealDamage(int count,float hp,float attack,float shield)
        {
            var b=Make(count);var u=Deploy(b,"CARD_M37",1);
            Assert.That(u.MaxHealth,Is.EqualTo(hp).Within(.001f));Assert.That(u.Health,Is.EqualTo(hp).Within(.001f));
            Assert.That(b.CanActivateContract,Is.False);Assert.That(b.TryActivateContract(b.RunId,100,u.EntityId),Is.False);
            Assert.That(b.ContractAttackBonus(u),Is.EqualTo(attack).Within(.001f));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(shield).Within(.001f));
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);Assert.That(u.Health,Is.EqualTo(hp).Within(.001f));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(shield-100).Within(.001f));
            b.TryDamageUnit(u.EntityId,shield-90,CombatDamageType.Arts);Assert.That(b.ContractShieldRemaining(u),Is.Zero);Assert.That(u.Health,Is.EqualTo(hp-10).Within(.001f));
            b.Advance(65);Assert.That(b.ContractAttackBonus(u),Is.EqualTo(attack).Within(.001f));Assert.That(b.ContractShieldRemaining(u),Is.Zero,"No tier periodically refills its entry shield.");
        }

        [Test]public void TwoCardFormationNeedsNeitherManualActivationNorSacrifice()
        {
            var b=Make(2,extra:Array.Empty<string>());var u=Deploy(b,"CARD_M37",1);
            Assert.That(b.ContractPoints,Is.Zero);Assert.That(b.ContractUses,Is.Zero);Assert.That(b.ContractCooldownRemaining,Is.Zero);
            Assert.That(u.Health,Is.EqualTo(u.MaxHealth));Assert.That(b.ContractAttackBonus(u),Is.EqualTo(.35f).Within(.001f));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(220).Within(.001f));
            Assert.That(b.Units.Count,Is.EqualTo(1));Assert.That(double.IsPositiveInfinity(b.ContractRemaining),Is.True);
        }

        [Test]public void EveryNormallyDeployedCopyGetsItsOwnPermanentBonusAndOneEntryShield()
        {
            var b=Make(2);var first=Deploy(b,"CARD_M37",1);b.TryDamageUnit(first.EntityId,100,CombatDamageType.Physical);
            b.Advance(.02f);var second=Deploy(b,"CARD_M37",2);b.Advance(61);var later=Deploy(b,"CARD_M37",3);
            foreach(var u in new[]{first,second,later})Assert.That(b.ContractAttackBonus(u),Is.EqualTo(.35f).Within(.001f));
            Assert.That(b.ContractShieldRemaining(first),Is.EqualTo(120).Within(.001f));Assert.That(b.ContractShieldRemaining(second),Is.EqualTo(220).Within(.001f));Assert.That(b.ContractShieldRemaining(later),Is.EqualTo(220).Within(.001f));
            var replay=b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M37");Assert.That(replay.EntityId,Is.EqualTo(later.EntityId));
            b.TryDamageUnit(later.EntityId,100,CombatDamageType.Physical);b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M37");
            Assert.That(b.ContractShieldRemaining(later),Is.EqualTo(120).Within(.001f));Assert.That(b.Units.Count,Is.EqualTo(3));
        }

        [TestCase(2,0,0)][TestCase(4,0,0)][TestCase(6,.45f,260)][TestCase(8,1.20f,700)][TestCase(9,1.50f,960)]
        public void SharingUsesEachRecipientOwnMaximumAndNeverIncludesOutsiders(int count,float bonus,float shield)
        {
            var b=Make(count);var core=Deploy(b,"CARD_M37",1);var other=Deploy(b,"CARD_M38",2);var outsider=Deploy(b,"CARD_M07",3);
            Assert.That(b.ContractAttackBonus(other),Is.EqualTo(bonus).Within(.001f));Assert.That(b.ContractShieldRemaining(other),Is.EqualTo(shield).Within(.001f));
            Assert.That(outsider.MaxHealth,Is.EqualTo(1000));Assert.That(b.ContractAttackBonus(outsider),Is.Zero);Assert.That(b.ContractShieldRemaining(outsider),Is.Zero);Assert.That(b.ContractAttackBonus(core),Is.GreaterThan(0));
        }

        [Test]public void LegacyNonSarkazConfigurationCannotMakeAnOutsiderTheContractOwner()
        {
            var b=Make(6,contract:"CARD_M07");var outsider=Deploy(b,"CARD_M07",1);var sarkaz=Deploy(b,"CARD_M37",2);
            Assert.That(b.ContractCard,Is.EqualTo("CARD_M37"));Assert.That(outsider.MaxHealth,Is.EqualTo(1000));Assert.That(b.IsContractBeneficiary(outsider),Is.False);
            Assert.That(b.ContractAttackBonus(outsider),Is.Zero);Assert.That(b.ContractShieldRemaining(outsider),Is.Zero);
            Assert.That(b.ContractAttackBonus(sarkaz),Is.EqualTo(.9f).Within(.001f));Assert.That(b.ContractShieldRemaining(sarkaz),Is.EqualTo(520).Within(.001f));
        }

        [Test]public void AFormationWithoutSarkazNeverCreatesAContractOwnerOrEntryBuff()
        {
            var b=Make(0,contract:"CARD_M07",members:new[]{"CARD_M07","CARD_M08"},extra:Array.Empty<string>());var outsider=Deploy(b,"CARD_M07",1);
            Assert.That(b.SarkazTier,Is.Zero);Assert.That(b.ContractCard,Is.Null);Assert.That(b.ContractAttackBonus(outsider),Is.Zero);Assert.That(b.ContractShieldRemaining(outsider),Is.Zero);
        }

        [Test]public void SharedSarkazReceivesItsEntryEffectEvenBeforeTheOwnerIsDeployed()
        {
            var b=Make(6);var other=Deploy(b,"CARD_M38",1);
            Assert.That(b.ContractAttackBonus(other),Is.EqualTo(.45f).Within(.001f));Assert.That(b.ContractShieldRemaining(other),Is.EqualTo(260).Within(.001f));
            Assert.That(b.TryActivateContract(b.RunId,100,other.EntityId),Is.False);Assert.That(b.ContractUses,Is.Zero);
        }

        [Test]public void ManualRequestsCannotRefillTheShieldConsumeResourcesOrIntroduceACooldown()
        {
            var b=Make(2);var u=Deploy(b,"CARD_M37",1);int dp=b.DP,uses=b.FindCard("CARD_M37").RemainingUses;float sp=u.SkillSP;
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);b.SetPaused(true);double at=b.Clock;b.Advance(100);
            Assert.That(b.Clock,Is.EqualTo(at));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(120).Within(.001f));Assert.That(b.ContractAttackBonus(u),Is.EqualTo(.35f).Within(.001f));
            foreach(int request in new[]{100,100,101})Assert.That(b.TryActivateContract(b.RunId,request),Is.False);
            b.SetPaused(false);b.Advance(61);Assert.That(b.TryActivateContract(b.RunId-1,102,u.EntityId),Is.False);Assert.That(b.TryActivateContract(b.RunId,103,u.EntityId),Is.False);
            Assert.That(b.ContractUses,Is.Zero);Assert.That(b.ContractCooldownRemaining,Is.Zero);Assert.That(b.ContractNextShieldRemaining,Is.Zero);Assert.That(b.ContractShieldRefreshRemaining(u),Is.Zero);
            Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(120).Within(.001f));Assert.That(b.DP,Is.EqualTo(dp));Assert.That(b.FindCard("CARD_M37").RemainingUses,Is.EqualTo(uses));Assert.That(u.SkillSP,Is.EqualTo(sp));
        }

        [Test]public void AnotherShieldExpiryNeitherExpiresNorRefillsTheIndependentEntryShield()
        {
            var b=Make(2);var u=Deploy(b,"CARD_M37",1);Set(u,"SynergyShield",900f);Set(u,"SynergyShieldUntil",4d);
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(120).Within(.001f));Assert.That(u.SynergyShield,Is.EqualTo(900));
            b.Advance(8.1f);b.TryDamageUnit(u.EntityId,150,CombatDamageType.Physical);
            Assert.That(b.ContractShieldRemaining(u),Is.Zero);Assert.That(u.Health,Is.EqualTo(1070).Within(.001f));Assert.That(b.ContractAttackBonus(u),Is.EqualTo(.35f).Within(.001f));
        }

        [TestCase(2,220,.35f)][TestCase(9,960,1.5f)]
        public void ConsumedEntryShieldsNeverRegenerateAtTheFormerTwentySecondBoundary(int count,float initial,float attack)
        {
            var b=Make(count);var u=Deploy(b,"CARD_M37",1);b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical);
            b.Advance(19);Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(initial-100).Within(.001f));b.Advance(1.01f);Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(initial-100).Within(.001f));
            b.Advance(45);Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(initial-100).Within(.001f));Assert.That(b.ContractAttackBonus(u),Is.EqualTo(attack).Within(.001f));
        }

        [TestCase(2,135)][TestCase(9,250)]
        public void DeploymentAutomaticallyBoostsRealOrdinaryHitsWithOnlyOneBodyInPlay(int count,float damage)
        {
            var b=Make(count,near:true);var u=Deploy(b,"CARD_M37",1);b.Advance(.5f);
            Assert.That(b.Units.Count(o=>!o.SynergySupport),Is.EqualTo(1));var shot=b.CombatShots.Single(s=>s.SourceId==u.EntityId&&!s.FromDefender);
            Assert.That(shot.Applied,Is.True);Assert.That(shot.DamageDealt,Is.EqualTo(damage).Within(.001f));Assert.That(b.ContractUses,Is.Zero);
        }

        [Test]public void GolemAndSameSpeciesHerdSpiritCannotBorrowTheSarkazContract()
        {
            var b=Make(9,extra:new[]{"CARD_M21","CARD_M22","CARD_M23"});var source=Deploy(b,"CARD_M24",1);
            var receipt=b.TryDeployGolem(b.RunId,2,b.Entry);Assert.That(receipt.Success,Is.True);var golem=b.FindUnit(receipt.EntityId);
            Assert.That(golem.MaxHealth,Is.EqualTo(360));Assert.That(b.ContractAttackBonus(golem),Is.Zero);Assert.That(b.ContractShieldRemaining(golem),Is.Zero);
            b.TryDamageUnit(source.EntityId,100000,CombatDamageType.Physical);b.Advance(1.5f);
            var spirit=b.Units.Single(u=>b.IsHerdHost(u.EntityId));Assert.That(spirit.SynergySupport,Is.True);Assert.That(spirit.MaxHealth,Is.EqualTo(720).Within(.001f));
            Assert.That(b.ContractAttackBonus(spirit),Is.Zero);Assert.That(b.ContractShieldRemaining(spirit),Is.Zero);
        }

        [TestCase(2,1100,.35f)][TestCase(9,1600,1.5f)]
        public void NativePatriotRebirthKeepsPermanentAttackButCannotGrantAnotherEntryShield(int count,float hp,float attack)
        {
            string[] members=count==2?new[]{"CARD_M20","CARD_M37"}:null;
            var b=Make(count,contract:"CARD_M20",members:members,nativeId:"CARD_M20",nativeEffects:new MonsterEffectSettings(patriot:true));var u=Deploy(b,"CARD_M20",1);
            b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(25);
            Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));Assert.That(u.Health,Is.Zero);Assert.That(b.ContractShieldRemaining(u),Is.Zero);Assert.That(b.ContractAttackBonus(u),Is.Zero);
            b.Advance(35.2f);Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Destruction));Assert.That(u.Health,Is.EqualTo(hp).Within(.001f));
            Assert.That(b.ContractShieldRemaining(u),Is.Zero);Assert.That(b.ContractAttackBonus(u),Is.EqualTo(attack).Within(.001f));Assert.That(b.ContractUses,Is.Zero);
        }

        [Test]public void SarkazHealthPersistsThroughMudrockNativeBarrierBreakAndRefresh()
        {
            var b=Make(2,members:new[]{"CARD_M28","CARD_M37"},nativeId:"CARD_M28",nativeEffects:new MonsterEffectSettings(mudrock:true));var u=Deploy(b,"CARD_M28",1);
            Assert.That(u.MaxHealth,Is.EqualTo(1650).Within(.001f));b.TryDamageUnit(u.EntityId,400,CombatDamageType.Arts);
            Assert.That(u.ArtsBarrierRemaining,Is.Zero);Assert.That(u.MaxHealth,Is.EqualTo(1100).Within(.001f));float health=u.Health;
            b.Advance(18.1f);Assert.That(u.BarrierRefreshes,Is.EqualTo(1));Assert.That(u.MaxHealth,Is.EqualTo(1650).Within(.001f));
            Assert.That(u.Health,Is.LessThanOrEqualTo(health));b.TryDamageUnit(u.EntityId,400,CombatDamageType.Arts);Assert.That(u.MaxHealth,Is.EqualTo(1100).Within(.001f));
        }

        [Test]public void NativeInvulnerabilityRejectsFacilityTrueDamageBeforeSpendingTheContractWard()
        {
            var b=Make(9,contract:"CARD_M20",nativeId:"CARD_M20",nativeEffects:new MonsterEffectSettings(patriot:true));var u=Deploy(b,"CARD_M20",1);
            Set(u,"PatriotInvulnerableUntil",b.Clock+10);Assert.That(b.UnitInvulnerable(u),Is.True);float hp=u.Health,shield=b.ContractShieldRemaining(u);
            var hit=typeof(ManualBattle).GetMethod("TrueDamage",State,null,new[]{typeof(DeployedUnitState),typeof(float)},null);Assert.That(hit,Is.Not.Null);hit.Invoke(b,new object[]{u,100f});
            Assert.That(u.Health,Is.EqualTo(hp));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(shield));Set(u,"PatriotInvulnerableUntil",double.NegativeInfinity);hit.Invoke(b,new object[]{u,100f});
            Assert.That(u.Health,Is.EqualTo(hp));Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(shield-100).Within(.001f));
        }

        [Test]public void RestartClearsEntryShieldStateAndNewDeploymentGrantsExactlyOneNewShield()
        {
            var b=Make(2);var old=Deploy(b,"CARD_M37",1);b.TryDamageUnit(old.EntityId,100,CombatDamageType.Physical);int previousRun=b.RunId;
            b.Restart();Assert.That(b.ContractUses,Is.Zero);Assert.That(b.ContractCooldownRemaining,Is.Zero);Assert.That(b.Units,Is.Empty);Assert.That(b.SarkazTier,Is.EqualTo(2));Assert.That(b.ContractShieldRemaining(old),Is.Zero);
            b.Begin();var next=Deploy(b,"CARD_M37",1);Assert.That(next.Health,Is.EqualTo(1100).Within(.001f));Assert.That(b.ContractShieldRemaining(next),Is.EqualTo(220).Within(.001f));
            Assert.That(b.TryActivateContract(previousRun,100),Is.False);Assert.That(b.TryActivateContract(b.RunId,100),Is.False);Assert.That(b.ContractUses,Is.Zero);
        }
    }
}
