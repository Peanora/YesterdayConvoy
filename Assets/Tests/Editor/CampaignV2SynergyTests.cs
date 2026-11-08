using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class CampaignV2SynergyTests
    {
        static readonly string[] Infection={"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"};
        static readonly string[] Snow={"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,10,8,2,.1f,200,1,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:3,unitInterval:2,
            defenderHealth:10000,defenderAttack:1,defenderRange:.01f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,
            skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,attackDuration:.3f,attackWindup:.1f);
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        static object Call(ManualBattle b,string method,params object[] args)=>typeof(ManualBattle).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,args);
        static ManualBattle Ready(string[] ids,bool v2=true)
        {
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.right*100},Vector3.right*50,Combat());
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(),true)));
            if(v2)b.ConfigureCampaignV2(13);return b;
        }
        static ManualBattle Make(string[] ids,bool v2=true,InfectionForm form=InfectionForm.Swarm)
        {var b=Ready(ids,v2);b.ConfigureSynergies(ids.Select(Piece),form:form);b.Begin();return b;}
        static DeployedUnitState Deploy(ManualBattle b,string id,long request=1)
        {var r=b.TryDeployRole(b.RunId,request,b.Entry,id);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}

        [Test]public void FormalDescriptionsUseTheSameSourceWhileOldSandboxDescriptionsRemainAvailable()
        {
            var formal=SynergySummary.From(CampaignBalance.Profiles.Keys,campaignV2:true);
            var legacy=SynergySummary.From(CampaignBalance.Profiles.Keys);
            Assert.That(formal.Length,Is.EqualTo(9));Assert.That(formal.All(s=>s.Effect.StartsWith("正式战役 v2")),Is.True);
            Assert.That(formal.Single(s=>s.Name=="感染生物").Effect,Does.Contain("65%生命/65%攻击"));
            Assert.That(formal.Single(s=>s.Name=="泥岩小队").Effect,Does.Contain("生命5200，攻击700"));
            Assert.That(formal.Single(s=>s.Name=="雪怪小队").Effect,Does.Contain("40+目标最大生命1.25%"));
            Assert.That(legacy.Single(s=>s.Name=="泥岩小队").Effect,Does.Contain("360/550/1800/6000/7500"));
            Assert.That(legacy.All(s=>!s.Effect.StartsWith("正式战役 v2")),Is.True);
        }

        [Test]public void SupplyCanShowZeroDefenseWithoutInventingACombatOrChangingLegacySupply()
        {
            var v2=Ready(new[]{"CARD_M07"});v2.ConfigureSupply(true);Assert.That(v2.Defense,Is.Zero);
            Assert.That(v2.CompleteSupply(),Is.True);Assert.That(v2.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(v2.CompleteSupply(),Is.False);
            var legacy=Ready(new[]{"CARD_M07"},false);legacy.ConfigureSupply();Assert.That(legacy.Defense,Is.EqualTo(10));
        }

        [TestCase(true,100,16)] [TestCase(false,80,14)]
        public void ReunionSupportUsesScopedPanelsAndKeepsTheOldTrial(bool v2,float hp,float attack)
        {
            var b=Make(new[]{"CARD_M07","CARD_M08"},v2);Deploy(b,"CARD_M07");Deploy(b,"CARD_M08",2);
            var support=b.Units.Single(u=>u.SynergySupport);Assert.That(support.MaxHealth,Is.EqualTo(hp));
            Assert.That(b.UnitCombat(support).UnitAttack,Is.EqualTo(attack));Assert.That(b.MusterUsed,Is.EqualTo(1));
        }

        [TestCase(4,.65f,2)] [TestCase(6,.5f,3)]
        public void SwarmUsesTheNewBodyBudgetWithoutMultiplyingDeployments(int count,float fraction,int bodies)
        {
            var b=Make(Infection.Take(count).ToArray());Deploy(b,Infection[0]);
            Assert.That(b.Units.Count,Is.EqualTo(bodies));Assert.That(b.Units.All(u=>Math.Abs(u.MaxHealth-200*fraction)<.001f),Is.True);
            Assert.That(b.Units.All(u=>Math.Abs(b.UnitCombat(u).UnitAttack-20*fraction)<.001f),Is.True);
            Assert.That(b.DP,Is.EqualTo(98));Assert.That(b.Units.Count(u=>!u.SynergySupport),Is.EqualTo(1));
        }

        [Test]public void InfectionShellAndBurstReadTheirOwnTierRatherThanTheSwarmBudget()
        {
            var shell=Make(Infection.Take(4).ToArray(),form:InfectionForm.Shell);var u=Deploy(shell,Infection[0]);
            Assert.That(u.SynergyShield,Is.EqualTo(56).Within(.001));Assert.That(shell.UnitCombat(u).UnitArmor,Is.EqualTo(2));
            var burst=Make(Infection.Take(4).ToArray(),form:InfectionForm.Burst);u=Deploy(burst,Infection[0]);
            burst.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);
            Assert.That(burst.InfectionBursts.Single().Attack,Is.EqualTo(70));Assert.That(burst.InfectionBursts.Single().Targets,Is.EqualTo(3));
        }

        [TestCase(2,.35f)] [TestCase(4,.5f)] [TestCase(6,.65f)]
        public void MilitaryOrderPanelIsScopedAndInvalidHalfOrderCannotReadAMissingTierField(int count,float multiplier)
        {
            var ids=new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"}.Take(count).ToArray();var b=Make(ids);
            var rule=(GuerrillaRules.Rule)typeof(ManualBattle).GetProperty("CurrentGuerrillaRule",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);
            Assert.That(rule.AttackMultiplier,Is.EqualTo(multiplier));Assert.That(b.TryGuerrillaOrder(b.RunId,91,true),Is.False);
            Assert.That(GuerrillaRules.ForTier(count).AttackMultiplier,Is.EqualTo(count==2?.6f:count==4?.8f:.9f));
        }

        [Test]public void ContractUsesNewHealthAttackAndOnePermanentShieldForTheHolder()
        {
            var b=Make(new[]{"CARD_M19","CARD_M24"});var u=Deploy(b,"CARD_M19");
            Assert.That(u.MaxHealth,Is.EqualTo(216).Within(.001));Assert.That(b.ContractAttackBonus(u),Is.EqualTo(.25f));
            Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(32.4f).Within(.001));
            Call(b,"ApplyContractDeployment",u);Assert.That(b.ContractShieldRemaining(u),Is.EqualTo(32.4f).Within(.001));
        }

        [Test]public void HerdReplacementUsesTheNewFractionAndArmorAndKeepsItsFiniteLifetime()
        {
            var b=Make(new[]{"CARD_M21","CARD_M22"});var u=Deploy(b,"CARD_M21");b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.Advance(.01f);
            var spirit=b.Units.Single(x=>x.SynergySupport);Assert.That(spirit.MaxHealth,Is.EqualTo(60).Within(.001));
            Assert.That(b.UnitCombat(spirit).UnitAttack,Is.EqualTo(6).Within(.001));Assert.That(b.UnitCombat(spirit).UnitArmor,Is.EqualTo(2));
            Assert.That(b.HerdRemaining(spirit.EntityId),Is.EqualTo(10).Within(.01));
        }

        [Test]public void DeepPoolDotAndMarineNativeDamageMultiplierUseTheAdoptedValues()
        {
            var deep=Make(new[]{"CARD_M29","CARD_M30"});Call(deep,"ApplyDeepBurn",deep.Defender);
            float health=deep.Defender.Health;deep.Advance(1);Assert.That(health-deep.Defender.Health,Is.EqualTo(12).Within(.05));
            var sea=Make(new[]{"CARD_M33","CARD_M34"});var u=Deploy(sea,"CARD_M33");Call(sea,"OnMarineContact",u);
            Assert.That(sea.MarineNeuralMultiplier(u),Is.EqualTo(1.35f));Assert.That(sea.MarineTileLimit,Is.EqualTo(8));
        }

        [Test]public void SnowShatterAndWeatherHaveSeparateNewDamageBudgets()
        {
            var b=Make(Snow);var first=Deploy(b,Snow[0]);var second=Deploy(b,Snow[1],2);
            b.TryApplyCold(b.Defender.EntityId,3.5f);b.TryApplyCold(b.Defender.EntityId,3.5f);
            float health=b.Defender.Health;Call(b,"OnSynergyDirectHit",first,b.Defender);Call(b,"OnSynergyDirectHit",second,b.Defender);
            Assert.That(health-b.Defender.Health,Is.EqualTo(100).Within(.001));
            Call(b,"StartSnowstorm");health=b.Defender.Health;Call(b,"StepSnowstorms");
            Assert.That(health-b.Defender.Health,Is.EqualTo(165).Within(.001));Assert.That(b.SnowstormPulses.Single().At,Is.EqualTo(0));
        }

        [TestCase(0,1,500,100,8,0,4)] [TestCase(6,2,900,170,10,0,5)]
        [TestCase(16,3,2200,380,18,20,8)] [TestCase(30,4,4000,520,22,30,14)] [TestCase(48,5,5200,700,26,40,16)]
        public void GolemStagesUsePersistentAdoptedPanelsAndOnlyOneDeployment(int xp,int stage,float hp,float attack,float armor,float resistance,int dp)
        {
            var ids=new[]{"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28","CARD_M07"};var b=Ready(ids);
            b.ConfigureSynergies(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=3}),golemExperience:xp,extraMudrockMembers:new[]{"CARD_M07"});b.Begin();
            Assert.That(b.GolemStage,Is.EqualTo(stage));var receipt=b.TryDeployGolem(b.RunId,1,b.Entry);Assert.That(receipt.Success,Is.True);
            var u=b.FindUnit(receipt.EntityId);var combat=b.UnitCombat(u);
            Assert.That(u.MaxHealth,Is.EqualTo(hp));Assert.That(combat.UnitAttack,Is.EqualTo(attack));Assert.That(combat.UnitArmor,Is.EqualTo(armor));
            Assert.That(combat.Effects.UnitResistance,Is.EqualTo(resistance));Assert.That(b.DP,Is.EqualTo(100-dp));
            Assert.That(b.TryDeployGolem(b.RunId,2,b.Entry).Success,Is.False);Assert.That(u.SynergySupport,Is.True);
        }
    }
}
