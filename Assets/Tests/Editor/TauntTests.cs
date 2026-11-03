using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class TauntTests
    {
        static ManualBattleSettings S(string role)=>new ManualBattleSettings("CARD_"+role,role+"_TEST",30,1,12,1,.1f,1000,1,1,1,1,1);
        static EngagementSettings C(int taunt=0,float range=100,int blocks=0)=>new EngagementSettings(unitAttack:1,unitRange:.001f,defenderAttack:1,defenderHealth:10000,defenderRange:range,blockCount:blocks,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:new MonsterEffectSettings(tauntLevel:taunt));
        static ManualBattle Create(float range=100,int blocks=0,int taunt=1)
        {
            var b=new ManualBattle(S("M15"),new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*3,C(range:range,blocks:blocks));
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M18"),C(taunt))});b.Begin();return b;
        }
        [Test]public void HigherTauntRetargetsLivingOriginalTargetAndDeathRestoresIt()
        {
            var b=Create();var first=b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.4f);Assert.That(b.Defenders[0].TargetId,Is.EqualTo(first.EntityId));
            var shield=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M18");Assert.That(shield.Success,Is.True);b.Advance(.05f);
            Assert.That(b.Defenders[0].TargetId,Is.EqualTo(shield.EntityId));Assert.That(b.FindUnit(first.EntityId).Active,Is.True);
            b.TryDamageUnit(shield.EntityId,10000,CombatDamageType.Arts);b.Advance(.05f);Assert.That(b.Defenders[0].TargetId,Is.EqualTo(first.EntityId));
        }
        [Test]public void HigherTauntMustEnterActualDefenderRange()
        {
            var b=Create(range:2);var first=b.TryDeploy(b.RunId,1,b.Entry);b.Advance(1.2f);
            var shield=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M18");b.Advance(.5f);Assert.That(b.Defenders[0].TargetId,Is.EqualTo(first.EntityId));
            b.Advance(1);Assert.That(b.Defenders[0].TargetId,Is.EqualTo(shield.EntityId));
        }
        [Test]public void BlockingDefenderKeepsItsBlockedTargetDespiteUnblockedShield()
        {
            var b=Create(range:4,blocks:1);var first=b.TryDeploy(b.RunId,1,b.Entry);b.Advance(3);
            Assert.That(b.Defenders[0].BlockedCount,Is.EqualTo(1));
            Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M18").Success,Is.True);b.Advance(.1f);Assert.That(b.Defenders[0].TargetId,Is.EqualTo(first.EntityId));
        }
        [Test]public void DefaultPriorityPreservesExistingValidTarget()
        {
            var b=Create(taunt:0);var first=b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.4f);b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M18");b.Advance(.1f);
            Assert.That(b.Defenders[0].TargetId,Is.EqualTo(first.EntityId));Assert.That(new MonsterEffectSettings().TauntLevel,Is.Zero);
        }
        [Test]public void InvalidPriorityIsRejected()
        {Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(tauntLevel:3));Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(tauntLevel:-3));}
    }
}
