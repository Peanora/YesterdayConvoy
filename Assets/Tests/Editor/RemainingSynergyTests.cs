using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class RemainingSynergyTests
    {
        static readonly string[] Herd={"CARD_M21","CARD_M22","CARD_M23","CARD_M24"};
        static readonly string[] Sea={"CARD_M33","CARD_M34","CARD_M35","CARD_M36","CARD_M07"};
        static readonly string[] Sark={"CARD_M19","CARD_M24","CARD_M26","CARD_M27","CARD_M37","CARD_M38"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,10,8,2,.1f,200,1,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:1,unitInterval:2,defenderHealth:500,defenderAttack:1,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,attackDuration:.3f,attackWindup:.1f);
        static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        static ManualBattle Make(string[] ids,MarineAdaptation main=MarineAdaptation.Barrier,int xp=0,string contract=null,string[] snapshot=null)
        {
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,new Vector3(100,0,0)},new Vector3(50,0,0),Combat(),new[]{new Vector3(50.5f,0,0),new Vector3(51,0,0)});
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(),true)));
            b.ConfigureSynergies((snapshot??ids).Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),contract:contract,adaptation:main,secondAdaptation:MarineAdaptation.Hunt,marineExperience:xp);b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,long request)=>b.FindUnit(b.TryDeployRole(b.RunId,request,b.Entry,id).EntityId);
        static void Hit(ManualBattle b,DefenderState d,float attack,bool derived=false,DeployedUnitState source=null)=>Call(b,"DamageDefender",d,attack,CombatDamageType.Arts,derived,source,1f);
        [Test]public void AllNineTraitsHaveRuntimeThresholdsAndAllAvailableMembersAreInPool()
        {
            var rows=SynergySummary.From(CampaignBalance.Profiles.Keys);Assert.That(rows.Length,Is.EqualTo(9));Assert.That(rows.All(r=>r.Implemented));
            Assert.That(rows.Single(r=>r.Name=="深池").Count,Is.EqualTo(4));Assert.That(rows.Single(r=>r.Name=="深池").Tier,Is.EqualTo(4));
            Assert.That(CampaignBalance.Supports("CARD_M32"));Assert.That(CampaignBalance.Supports("CARD_M25"));Assert.That(CampaignBalance.Profiles.Count,Is.EqualTo(43));
        }
        [Test]public void HerdContinuesAtOriginalProgressAndCannotConvertAgain()
        {
            var b=Make(Herd);var u=Deploy(b,Herd[0],1);b.Advance(.2f);double distance=u.Motion.DistanceTravelled;
            b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical);Assert.That(b.HasPendingDeathBlast);b.Advance(.02f);
            var host=b.Units.Single(o=>b.IsHerdHost(o.EntityId));Assert.That(host.Motion.DistanceTravelled,Is.EqualTo(distance).Within(.02));
            Assert.That(host.MaxHealth,Is.EqualTo(90));Assert.That(host.SynergySupport);Assert.That(b.UnitCombat(host).HasSkill,Is.False);Assert.That(b.UnitCombat(host).Effects.BlastAttack,Is.Zero);
            b.TryDamageUnit(host.EntityId,1000,CombatDamageType.Physical);b.Advance(.02f);Assert.That(b.HerdUsed,Is.EqualTo(1));Assert.That(b.Units.Count,Is.EqualTo(2));
        }
        [Test]public void NativeFlamerazerFinalExplosionCanConvertOnlyAfterFinalDeath()
        {
            var p=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M31"),1,100,10);
            var b=new ManualBattle(p.Unit,new[]{Vector3.zero,new Vector3(100,0,0)},new Vector3(50,0,0),p.Combat);
            b.ConfigureSynergies(Herd.Concat(new[]{"CARD_M31"}).Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}));b.Begin();
            var u=b.FindUnit(b.TryDeploy(b.RunId,1,b.Entry).EntityId);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);
            Assert.That(u.FuelDying);Assert.That(b.HerdUsed,Is.Zero);b.Advance(.2f);Assert.That(b.HerdUsed,Is.EqualTo(1));Assert.That(b.Units.Count(v=>b.IsHerdHost(v.EntityId)),Is.EqualTo(1));
        }
        [Test]public void HerdLifetimeFreezesDuringPauseAndExpiresWithoutNewDeathEffects()
        {
            var b=Make(Herd.Take(2).ToArray());var u=Deploy(b,Herd[0],1);b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical);b.Advance(.02f);var host=b.Units.Single(o=>o.SynergySupport);
            b.SetPaused(true);double remaining=b.HerdRemaining(host.EntityId);b.Advance(20);Assert.That(b.HerdRemaining(host.EntityId),Is.EqualTo(remaining));
            b.SetPaused(false);b.Advance((float)(HerdSpiritState.FallDuration+HerdSpiritState.RiseDuration+10.1));Assert.That(host.Active,Is.False);Assert.That(b.HerdUsed,Is.EqualTo(1));Assert.That(b.DeepCharges,Is.Zero);
        }
        [Test]public void HerdCapsConversionsAndFiveCostsNeverConsumeSlots()
        {
            var ids=Herd.Concat(new[]{"CARD_M14"}).ToArray();var b=Make(ids);var frost=Deploy(b,"CARD_M14",1);b.TryDamageUnit(frost.EntityId,1000,CombatDamageType.Physical);Assert.That(b.HerdUsed,Is.Zero);
            for(int i=0;i<6;i++){var u=Deploy(b,Herd[0],10+i);Assert.That(u,Is.Not.Null);b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical);b.Advance(.11f);}
            Assert.That(b.HerdUsed,Is.EqualTo(5));
        }
        [Test]public void DeepSpreadHasCorrectDotBudgetAndNonDeepDerivedKillsNeverCreateSeeds()
        {
            var b=Make(new[]{"CARD_M29","CARD_M30"});var source=Deploy(b,"CARD_M29",1);Hit(b,b.Defenders[0],1000,source:source);
            Assert.That(b.DeepCharges,Is.EqualTo(1));float hp=b.Defenders[1].Health;b.Advance(1);Assert.That(hp-b.Defenders[1].Health,Is.EqualTo(15).Within(.4));
            Hit(b,b.Defenders[1],1000,true);Assert.That(b.DeepCharges,Is.EqualTo(1));Assert.That(b.TryDetonateDeep(b.RunId,1),Is.False);
            Assert.That(b.DeepBurnRemaining(b.Defenders[2].EntityId),Is.EqualTo(3).Within(.06));
        }
        [Test]public void DeepFourCanDetonateAtomicallyWithApprovedMandragoraInShop()
        {
            var b=Make(new[]{"CARD_M29","CARD_M30","CARD_M31"},snapshot:new[]{"CARD_M29","CARD_M30","CARD_M31","CARD_M32"});Assert.That(b.DeepTier,Is.EqualTo(4));
            var source=Deploy(b,"CARD_M29",1);Hit(b,b.Defenders[0],1000,source:source);Hit(b,b.Defenders[1],1000,source:source);
            Assert.That(b.DeepCharges,Is.EqualTo(2));b.SetPaused(true);Assert.That(b.TryDetonateDeep(b.RunId,1),Is.False);Assert.That(b.DeepCharges,Is.EqualTo(2));b.SetPaused(false);
            float health=b.Defenders[2].Health;Assert.That(b.TryDetonateDeep(b.RunId,1));Assert.That(health-b.Defenders[2].Health,Is.EqualTo(180));
            Assert.That(b.TryDetonateDeep(b.RunId,1));Assert.That(b.DeepCharges,Is.EqualTo(1));Assert.That(b.DeepDetonations,Is.EqualTo(1));
            Assert.That(b.TryDetonateDeep(b.RunId-1,2),Is.False);Assert.That(CampaignBalance.Supports("CARD_M32"));
        }
        [Test]public void ContractOpensWithoutHealthLossAndSharesAtSixMembers()
        {
            var b=Make(Sark,contract:"CARD_M37");var target=Deploy(b,"CARD_M37",1);
            var other=Deploy(b,Sark[0],2);
            Assert.That(target.Health,Is.EqualTo(260).Within(.001f));Assert.That(b.ContractPoints,Is.Zero);
            b.SetPaused(true);Assert.That(b.TryActivateContract(b.RunId,100,target.EntityId),Is.False);b.SetPaused(false);
            Assert.That(b.TryActivateContract(b.RunId,100,target.EntityId));Assert.That(b.ContractPoints,Is.Zero);Assert.That(b.ContractUses,Is.EqualTo(1));
            Assert.That(b.ContractAttackBonus(target),Is.EqualTo(.9f).Within(.001f));Assert.That(b.ContractShieldRemaining(target),Is.EqualTo(104).Within(.001f));
            Assert.That(b.TryActivateContract(b.RunId,100,target.EntityId));Assert.That(b.ContractUses,Is.EqualTo(1));
            Assert.That(b.ContractAttackBonus(other),Is.EqualTo(.45f).Within(.001f));Assert.That(b.ContractShieldRemaining(other),Is.EqualTo(52).Within(.001f));
            b.Advance(12.1f);Assert.That(b.ContractAttackBonus(target),Is.Zero);Assert.That(b.ContractShieldRemaining(target),Is.Zero);
        }
        [Test]public void DamageDoesNotChargeContractOrBypassItsCooldown()
        {
            var b=Make(Sark,contract:"CARD_M37");var target=Deploy(b,"CARD_M37",1);var other=Deploy(b,Sark[0],2);
            Assert.That(b.TryActivateContract(b.RunId,20,target.EntityId));
            Call(b,"ApplyUnitHealthDamage",other,160f);b.TryDamageUnit(other.EntityId,1,CombatDamageType.Physical);Call(b,"HealUnit",other,200f);
            Assert.That(b.ContractPoints,Is.Zero);Assert.That(b.ContractCooldownRemaining,Is.EqualTo(20).Within(.001));
            Assert.That(b.TryActivateContract(b.RunId,21),Is.False);Assert.That(b.ContractUses,Is.EqualTo(1));
        }
        [TestCase(MarineAdaptation.Barrier)] [TestCase(MarineAdaptation.Hunt)] [TestCase(MarineAdaptation.Infiltration)]
        public void RetiredMarineParametersCannotRestoreOldEffectsOrExperience(MarineAdaptation choice)
        {
            var b=Make(Sea,choice,7);var u=Deploy(b,Sea[0],1);float armor=b.DefenderArmor(b.Defender);
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical,ranged:true);Assert.That(u.Health,Is.EqualTo(100));
            Hit(b,b.Defender,340);float health=b.Defender.Health;Hit(b,b.Defender,20,source:u);
            Assert.That(health-b.Defender.Health,Is.EqualTo(20));Assert.That(b.DefenderArmor(b.Defender),Is.EqualTo(armor));
            Call(b,"FinishForTesting",b.RunId,true);Assert.That(b.MarineGrowthEarned,Is.Zero);Assert.That(b.MarineExperience,Is.Zero);
        }
    }
}
