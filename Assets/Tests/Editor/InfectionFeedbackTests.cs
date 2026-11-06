using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class InfectionFeedbackTests
    {
        static readonly string[] Infected={"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,5,8,1,.1f,200,1,1,1,1,1);
        static DefenderSettings Guard(string id,float distance,float health=1000,float armor=0)=>
            new DefenderSettings(id,id,"D07",Vector3.right*distance,health,1,armor,.1f,10,canAttack:false);
        static ManualBattle Battle(DefenderSettings[] guards,int members=6,int star=1,float resistance=0,
            InfectionForm form=InfectionForm.Burst,InfectionForm second=InfectionForm.Shell,MonsterEffectSettings? effects=null,string card="CARD_M02")
        {
            var combat=new EngagementSettings(unitAttack:1,unitArmor:0,unitRange:.1f,unitInterval:2,
                defenderHealth:1000,defenderAttack:1,defenderArmor:0,defenderRange:.1f,defenderInterval:10,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                canAttack:false,effects:effects??new MonsterEffectSettings(resistance:resistance));
            var b=new ManualBattle(Unit(card),new[]{Vector3.zero,Vector3.forward*50},Vector3.right*50,combat,null,guards);
            b.ConfigureCardLoadout(Infected.Where(id=>id!=card).Select(id=>new BasicRosterProfile(Unit(id),combat,true)));
            b.ConfigureSynergies(new[]{card}.Concat(Infected.Where(id=>id!=card)).Take(members).Select(id=>new ShopTrialModel.Piece{Id=id,Star=star}),form:form,second:second);
            b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,long request,string id="CARD_M02")
        {
            var receipt=b.TryDeployRole(b.RunId,request,b.Entry,id);Assert.That(receipt.Success,Is.True);
            return b.FindUnit(receipt.EntityId);
        }
        static void Defeat(ManualBattle b,DeployedUnitState unit)=>Assert.That(b.TryDamageUnit(unit.EntityId,10000,CombatDamageType.Arts),Is.True);

        [Test]public void EventKeepsItsRealSourceAndPostResistanceHealthLossIncludingLethalTruncation()
        {
            var b=Battle(new[]{Guard("fragile",.25f,20),Guard("durable",.5f),Guard("outside",5)},star:3,resistance:40);
            var source=Deploy(b,1);Defeat(b,source);var burst=b.InfectionBursts.Single();
            Assert.That(burst.SourceId,Is.EqualTo(source.EntityId));Assert.That(burst.CardId,Is.EqualTo("CARD_M02"));
            Assert.That(burst.Position,Is.EqualTo(source.Motion.Position));Assert.That(burst.At,Is.EqualTo(.2).Within(.000001));
            Assert.That(burst.Hits,Is.Empty);Assert.That(burst.TotalDamage,Is.Zero);Assert.That(burst.Resolved,Is.False);
            b.Advance(.25f);
            Assert.That(burst.Resolved,Is.True);Assert.That(burst.Attack,Is.EqualTo(85*1.7f).Within(.001));
            Assert.That(burst.Hits.Select(h=>h.EntityId),Is.EqualTo(new[]{"fragile","durable"}));
            Assert.That(burst.Hits[0].Position,Is.EqualTo(Vector3.right*.25f));
            Assert.That(burst.Hits[0].Damage,Is.EqualTo(20));Assert.That(burst.Hits[1].Damage,Is.EqualTo(85*1.7f*.6f).Within(.001));
            var actualLosses=b.CombatFeedback.Where(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost).ToArray();
            Assert.That(actualLosses.Length,Is.EqualTo(2));
            Assert.That(burst.TotalDamage,Is.EqualTo(actualLosses.Sum(e=>e.Amount)).Within(.001));
            Assert.That(burst.Hits.All(h=>h.Damage>=0),Is.True);
            Assert.Throws<NotSupportedException>(()=>((IList<InfectionBurstHit>)burst.Hits).Clear());
        }

        [TestCase(2,1f,2,35f)]
        [TestCase(4,1.2f,3,65f)]
        [TestCase(6,1.3f,3,85f)]
        public void CapUsesNearestLivingTargetsAndLaterBurstIncludesExactBoundaryButNotOutside(int tier,float radius,int cap,float attack)
        {
            var guards=Enumerable.Range(0,cap).Select(i=>Guard("near-"+i,radius*(i+1)/(cap+1),1))
                .Concat(new[]{Guard("boundary",radius),Guard("outside",radius+.01f)}).ToArray();
            var b=Battle(guards,members:tier);Defeat(b,Deploy(b,1));b.Advance(.25f);
            var first=b.InfectionBursts.Single();
            Assert.That(first.Radius,Is.EqualTo(radius));Assert.That(first.Attack,Is.EqualTo(attack));
            Assert.That(first.Hits.Select(h=>h.EntityId),Is.EqualTo(Enumerable.Range(0,cap).Select(i=>"near-"+i)));
            Assert.That(first.Hits.Count,Is.EqualTo(cap));Assert.That(first.TotalDamage,Is.EqualTo(cap));
            Defeat(b,Deploy(b,2));b.Advance(.25f);
            var second=b.InfectionBursts.Last();
            Assert.That(second.Hits.Count,Is.EqualTo(1));Assert.That(second.Hits[0].EntityId,Is.EqualTo("boundary"));
            Assert.That(second.TotalDamage,Is.EqualTo(attack).Within(.001));
            Assert.That(b.FindDefender("outside").Health,Is.EqualTo(1000));
            Assert.That(b.CombatFeedback.Count(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost),Is.EqualTo(cap+1));
        }

        [Test]public void PauseDuplicateDeathAndRestartCannotCreateDuplicateOrCarryOverResults()
        {
            var b=Battle(new[]{Guard("target",.5f)},members:2);var source=Deploy(b,1);Defeat(b,source);
            var burst=b.InfectionBursts.Single();var observed=burst.Hits;int run=b.RunId;
            Assert.That(b.TryDamageUnit(source.EntityId,10000,CombatDamageType.Arts),Is.False);
            b.Advance(.1f);b.SetPaused(true);double clock=b.Clock;b.Advance(30);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(burst.Resolved,Is.False);Assert.That(observed,Is.Empty);
            b.SetPaused(false);b.Advance(.11f);Assert.That(observed.Count,Is.EqualTo(1));Assert.That(burst.TotalDamage,Is.EqualTo(35));
            float health=b.FindDefender("target").Health;b.Advance(1);
            Assert.That(b.InfectionBursts.Count,Is.EqualTo(1));Assert.That(observed.Count,Is.EqualTo(1));Assert.That(b.FindDefender("target").Health,Is.EqualTo(health));
            b.Restart();Assert.That(b.RunId,Is.Not.EqualTo(run));Assert.That(b.InfectionBursts,Is.Empty);Assert.That(observed.Count,Is.EqualTo(1));
            b.Begin();var fresh=Deploy(b,1);Assert.That(fresh.EntityId,Is.Not.EqualTo(source.EntityId));Defeat(b,fresh);b.Advance(.25f);
            var next=b.InfectionBursts.Single();Assert.That(next.SourceId,Is.EqualTo(fresh.EntityId));Assert.That(next.Hits.Count,Is.EqualTo(1));Assert.That(next.TotalDamage,Is.EqualTo(35));
        }

        [Test]public void EmptyBlastIsAResolvedZeroHitEventInsteadOfInventingNearbyVictims()
        {
            var b=Battle(new[]{Guard("outside",5)},members:2);Defeat(b,Deploy(b,1));b.Advance(.25f);
            var burst=b.InfectionBursts.Single();Assert.That(burst.Resolved,Is.True);Assert.That(burst.Hits,Is.Empty);Assert.That(burst.TotalDamage,Is.Zero);
            Assert.That(b.CombatFeedback.Any(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.Kind==CombatFeedbackKind.HealthLost),Is.False);
        }

        [Test]public void IntrinsicPhysicalBlastRemainsSeparateFromRecordedSynergyArtsDamage()
        {
            var effects=new MonsterEffectSettings(blastAttack:90,blastRadius:1.6f,blastDelay:1,coldDuration:10);
            var b=Battle(new[]{Guard("target",.5f,1000,10)},members:2,effects:effects,card:"CARD_M04");
            Defeat(b,Deploy(b,1,"CARD_M04"));b.Advance(.25f);
            var burst=b.InfectionBursts.Single();Assert.That(burst.TotalDamage,Is.EqualTo(35));Assert.That(burst.Hits.Count,Is.EqualTo(1));
            Assert.That(b.FindDefender("target").Health,Is.EqualTo(965));Assert.That(b.FindDefender("target").ColdUntil,Is.LessThan(0));
            Assert.That(b.DeathBlasts.Count,Is.EqualTo(1));Assert.That(b.DeathBlasts[0].Resolved,Is.False);
            b.Advance(.8f);
            Assert.That(b.FindDefender("target").Health,Is.EqualTo(885));Assert.That(b.FindDefender("target").ColdUntil,Is.GreaterThan(b.Clock));
            Assert.That(burst.TotalDamage,Is.EqualTo(35));Assert.That(burst.Hits.Count,Is.EqualTo(1));Assert.That(b.DeathBlasts[0].Resolved,Is.True);
        }

        [Test]public void SwarmSplitDeathsCannotDuplicateThePrimarySynergyBurstOrItsResult()
        {
            var b=Battle(new[]{Guard("target",.5f)},form:InfectionForm.Swarm,second:InfectionForm.Burst);
            var primary=Deploy(b,1);Assert.That(b.Units.Count,Is.EqualTo(3));
            foreach(var unit in b.Units.ToArray())Defeat(b,unit);
            Assert.That(b.InfectionBursts.Count,Is.EqualTo(1));b.Advance(.25f);
            var burst=b.InfectionBursts.Single();Assert.That(burst.SourceId,Is.EqualTo(primary.EntityId));Assert.That(burst.TotalDamage,Is.EqualTo(35));
            Assert.That(burst.Hits.Count,Is.EqualTo(1));Assert.That(b.Orders,Is.Zero);Assert.That(b.FindDefender("target").Health,Is.EqualTo(965));
        }
    }
}
