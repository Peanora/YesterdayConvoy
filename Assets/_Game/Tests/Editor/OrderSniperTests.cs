using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class OrderSniperTests
    {
        static ManualBattleSettings S(string role)=>new ManualBattleSettings("CARD_"+role,role+"_TEST",30,1,12,1,.1f,1000,1,1,1,1,1);
        static EngagementSettings C(bool commander=false,bool sniper=false,float range=2.5f)=>new EngagementSettings(unitAttack:10,unitRange:range,unitInterval:1,defenderHealth:10000,defenderRange:.001f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,projectileTime:.1f,
            effects:new MonsterEffectSettings(orderSniper:sniper,tacticalCommander:commander));
        static ManualBattle Create()
        {
            var b=new ManualBattle(S("M16"),new[]{Vector3.zero,Vector3.forward*100},new Vector3(-.5f,0,5),C(sniper:true),new[]{new Vector3(.5f,0,5)});
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M17"),C(commander:true,range:.001f))});return b;
        }
        [Test]public void CommandTurnsOneOrdinaryAttackIntoTwoDifferentRealHitsAndDeathRestoresOne()
        {
            var b=Create();b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);var u=b.FindUnit(r.EntityId);b.Advance(3);
            Assert.That(b.CombatShots.Count(s=>s.SourceId==u.EntityId&&!s.FromDefender),Is.EqualTo(1));
            r=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M17");Assert.That(r.Success,Is.True);var h=b.FindUnit(r.EntityId);b.Advance(1);
            var pairs=b.CombatShots.Where(s=>s.SourceId==u.EntityId&&!s.FromDefender).GroupBy(s=>s.StartedAt).Where(g=>g.Count()==2).ToArray();
            Assert.That(pairs.Length,Is.GreaterThan(0));Assert.That(pairs[0].Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(2));Assert.That(pairs[0].All(s=>s.Applied&&s.DamageDealt>0),Is.True);
            b.TryDamageUnit(h.EntityId,10000,CombatDamageType.Arts);double death=b.Clock;b.Advance(1.2f);
            Assert.That(b.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==u.EntityId&&s.StartedAt>death).GroupBy(s=>s.StartedAt).All(g=>g.Count()==1),Is.True);
        }
        [Test]public void CommandDoesNotCreateDuplicateShotsWhenOnlyOneDefenderInRange()
        {
            var b=new ManualBattle(S("M17"),new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*2,C(commander:true,range:.001f));
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M16"),C(sniper:true))});b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.4f);
            var r=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M16");Assert.That(r.Success,Is.True);b.Advance(.3f);
            Assert.That(b.CombatShots.Count(s=>!s.FromDefender&&s.SourceId==r.EntityId),Is.EqualTo(1));
        }
        [Test]public void ExtraOrdinaryTargetCannotSelectCamouflagedGuard()
        {
            var b=new ManualBattle(S("M16"),new[]{Vector3.zero,Vector3.forward*100},new Vector3(-1,0,5),C(sniper:true),new[]{new Vector3(1,0,5)});
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M17"),C(commander:true,range:.001f))});
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,new Vector3(1,0,5))});b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(3);
            Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M17").Success,Is.True);b.Advance(2);
            Assert.That(b.DefenderCamouflaged(b.Defenders[1]),Is.True);Assert.That(b.Defenders[1].Health,Is.EqualTo(10000));
            Assert.That(b.CombatShots.Any(s=>!s.FromDefender&&s.TargetId==b.Defenders[1].EntityId),Is.False);
        }
                [Test]public void SeveralCommandersStillLimitAnAttackToTwoOfThreeEligibleTargets()
        {
            var b=new ManualBattle(S("M17"),new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,C(commander:true,range:.001f),new[]{Vector3.forward+Vector3.left*.4f,Vector3.forward+Vector3.right*.4f});
            b.ConfigureRosterProfiles(new[]{new BasicRosterProfile(S("M16"),C(sniper:true))});b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.4f);b.TryDeploy(b.RunId,2,b.Entry);b.Advance(.4f);
            var r=b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M16");Assert.That(r.Success,Is.True);b.Advance(.3f);
            Assert.That(b.TacticalCommanders,Is.EqualTo(2));var shots=b.CombatShots.Where(s=>!s.FromDefender&&s.SourceId==r.EntityId).ToArray();Assert.That(shots.Length,Is.EqualTo(2));Assert.That(shots.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(2));Assert.That(b.FindUnit(r.EntityId).OrdinaryAttacksFired,Is.EqualTo(1));
        }
    }
}
