using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class TacticalCommandTests
    {
        static ManualBattleSettings Settings(string role,float hp=1000,float speed=1)=>new ManualBattleSettings("CARD_"+role,role+"_TEST",30,1,12,1,.1f,hp,speed,1,1,1,1);
        static EngagementSettings Combat(float attack=30,float armor=3,float range=.01f,bool commander=false)=>new EngagementSettings(
            unitAttack:attack,unitArmor:armor,unitRange:range,unitInterval:2,defenderHealth:10000,defenderAttack:1,defenderRange:.001f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
            effects:new MonsterEffectSettings(tacticalCommander:commander));
        static ManualBattle Create()
        {
            var b=new ManualBattle(Settings("M15"),new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*30,Combat());
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(Settings("M17",hp:200,speed:2),Combat(26,1.2f,commander:true)),new BasicRosterProfile(Settings("M16",speed:3),Combat(34,4,2.5f))});return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,int request,string role)
        {var r=b.TryDeployRole(b.RunId,request,b.Entry,"CARD_"+role);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void RegisteredProfilesSpawnRealDifferentCardsWithOwnHealthSpeedAttackAndArmor()
        {
            var b=Create();b.Begin();var soldier=Deploy(b,1,"M15");b.Advance(.4f);var herald=Deploy(b,2,"M17");
            Assert.That(herald.CardId,Is.EqualTo("CARD_M17"));Assert.That(herald.UnitId,Is.EqualTo("M17_TEST"));Assert.That(herald.Health,Is.EqualTo(200));Assert.That(herald.Motion.Speed,Is.EqualTo(2));
            Assert.That(b.UnitCombat(herald).UnitAttack,Is.EqualTo(26));Assert.That(b.UnitArmor(herald),Is.EqualTo(2.2f).Within(.001));Assert.That(b.UnitArmor(soldier),Is.EqualTo(4));
            Assert.That(b.UnitAttackMultiplier(soldier),Is.EqualTo(1.1f).Within(.001));Assert.That(b.TacticalCommanders,Is.EqualTo(1));
        }
        [Test]public void EachLivingCommanderStacksIncludingSelfAndRealDeathImmediatelyRemovesOne()
        {
            var b=Create();b.Begin();var soldier=Deploy(b,1,"M15");b.Advance(.4f);var first=Deploy(b,2,"M17");b.Advance(.3f);var second=Deploy(b,3,"M17");
            Assert.That(b.TacticalCommanders,Is.EqualTo(2));Assert.That(b.UnitAttackMultiplier(soldier),Is.EqualTo(1.2f).Within(.001));Assert.That(b.UnitArmor(soldier),Is.EqualTo(5));
            b.TryDamageUnit(first.EntityId,10000,CombatDamageType.Arts);Assert.That(b.TacticalCommanders,Is.EqualTo(1));Assert.That(b.UnitArmor(soldier),Is.EqualTo(4));
            b.TryDamageUnit(second.EntityId,10000,CombatDamageType.Arts);Assert.That(b.UnitAttackMultiplier(soldier),Is.EqualTo(1));Assert.That(b.UnitArmor(soldier),Is.EqualTo(3));
        }
        [Test]public void ProfileDamageUsesItsArmorAndResistanceInsteadOfPrimaryCard()
        {
            var b=Create();b.Begin();var h=Deploy(b,1,"M17");b.TryDamageUnit(h.EntityId,12.2f,CombatDamageType.Physical);
            Assert.That(h.Health,Is.EqualTo(190).Within(.001));
        }
        [Test]public void UnknownCardEntryCooldownPauseAndDuplicateRequestsUseNormalDeploymentGuards()
        {
            var b=Create();b.Begin();Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M20").Failure,Is.EqualTo(DeployFailure.MissingCard));
            var h=Deploy(b,1,"M17");Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M17").EntityId,Is.EqualTo(h.EntityId));
            Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M16").Failure,Is.EqualTo(DeployFailure.StaleRun));
            Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M16").Failure,Is.EqualTo(DeployFailure.CoolingDown));b.Advance(.15f);
            Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M16").Failure,Is.EqualTo(DeployFailure.EntryBusy));
            b.SetPaused(true);Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M16").Failure,Is.EqualTo(DeployFailure.Paused));
        }
        [Test]public void RestartRemovesCommandersAndPreservesAuthoredRosterForNextRun()
        {
            var b=Create();b.Begin();Deploy(b,1,"M17");Assert.That(b.TacticalCommanders,Is.EqualTo(1));b.Restart();Assert.That(b.TacticalCommanders,Is.Zero);
            b.Begin();Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M16").Success,Is.True);
        }
        [Test]public void BreachingCommanderStopsProvidingOrdersWithoutADeath()
        {
            var b=new ManualBattle(Settings("M17",speed:10),new[]{Vector3.zero,Vector3.forward},Vector3.forward*30,Combat(commander:true));
            b.Begin();var h=Deploy(b,1,"M17");Assert.That(b.TacticalCommanders,Is.EqualTo(1));b.Advance(1);
            Assert.That(h.Resolution,Is.EqualTo(UnitResolution.Breached));Assert.That(b.TacticalCommanders,Is.Zero);
        }
        [Test]public void DefenderBlockCapacityComesFromTrialNotCompanionProfile()
        {
            var c=new EngagementSettings(unitRange:.01f,blockCount:1,blockRadius:1,defenderRange:.001f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            var b=new ManualBattle(Settings("M15"),new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*.5f,c);
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(Settings("M17"),Combat(commander:true))});b.Begin();var h=Deploy(b,1,"M17");b.Advance(.1f);
            Assert.That(h.IsBlocked,Is.True);Assert.That(b.Defender.BlockedCount,Is.EqualTo(1));
        }
                [Test]public void ProfileConfigurationIsReadyOnlyAndRejectsUnsupportedOrDuplicateRoles()
        {
            var b=Create();Assert.Throws<ArgumentException>(()=>new BasicRosterProfile(Settings("M20"),Combat()));
            var p=new BasicRosterProfile(Settings("M17"),Combat(commander:true));Assert.Throws<ArgumentException>(()=>b.ConfigureRosterProfiles(new[]{p,p}));
            b.Begin();Assert.Throws<InvalidOperationException>(()=>b.ConfigureRosterProfiles(new[]{p}));
        }
    }
}
