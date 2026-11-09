using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class GuerrillaHighTierTests
    {
        static readonly string[] Native={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20"};
        static readonly string[] Other={"CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M21","CARD_M22","CARD_M23","CARD_M24"};
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        static string[] Extras(int tier)=>tier==9?Other.Take(2).ToArray():tier==8?Other.Take(1).ToArray():Array.Empty<string>();
        static ManualBattle Fixture(int tier,bool near=true,bool begin=true,float actionDuration=0,bool skill=false)
        {
            int natives=tier>=8?7:tier;
            var ids=Native.Take(natives).Concat(Other.Take(10-natives)).ToArray();
            ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,10000,1,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:10,unitArmor:0,unitRange:2,unitInterval:100,
                defenderHealth:1000000,defenderAttack:1,defenderArmor:0,defenderRange:.01f,blockCount:0,
                initialSP:skill?1:0,requiredSP:skill?1:0,spPerSecond:skill?1:0,skillAmmo:skill?2:0,
                skillAttack:skill?10:0,skillInterval:skill?1:0,skillRange:skill?2:0,hasSkill:skill,
                projectileTime:.01f,attackDuration:actionDuration,melee:true);
            var guards=new[]{new DefenderSettings("near","Near",null,near?new Vector3(.5f,0,0):new Vector3(40,0,40),1000000,1,0,.01f,100,canAttack:false),
                new DefenderSettings("far","Far",null,new Vector3(50,0,50),1000000,1,0,.01f,100,canAttack:false)};
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,new Vector3(20,0,0)},guards[0].Position,combat,roster:guards);
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),combat,true)));
            b.ConfigureSynergies(ids.Select(Piece),extraGuerrillaMembers:Extras(tier));
            Assert.That(b.GuerrillaTier,Is.EqualTo(tier));if(begin)b.Begin();return b;
        }
        static void EarnAllContacts(ManualBattle b,int copies=2)
        {
            long request=1;
            for(int copy=0;copy<copies;copy++)
            {
                foreach(var card in b.Cards)Assert.That(b.TryDeployRole(b.RunId,request++,b.Entry,card.Settings.CardId).Success,Is.True);
                b.Advance(.35f);
            }
            // Runtime-only quiet-combat fixture: points are earned by real ordinary hits;
            // suppress later ordinary attacks so volley/resource assertions remain unambiguous.
            foreach(var unit in b.Units)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).SetValue(unit,1000d);
            b.Advance(.05f);
        }

        [TestCase(2,5,0,6)][TestCase(4,4,0,8)][TestCase(6,3,2,12)]
        [TestCase(8,7,1,20)][TestCase(9,11,0,20)]
        public void AllTiersCanExceedFormerCastCapsUntilEarnedOrdersRunOut(int tier,int casts,int remaining,int participants)
        {
            var b=Fixture(tier);EarnAllContacts(b);
            int earned=tier>=8?22:20,cost=GuerrillaRules.ForTier(tier).Cost;
            Assert.That(b.Orders+cost*b.OrdersUsed,Is.EqualTo(earned),"Initial and contact points are conserved even when the first batch starts automatically.");
            var previous=b.LatestGuerrillaOrder;Assert.That(previous,Is.Not.Null);int lastSequence=previous.Sequence;
            for(int i=0;i<1000&&(b.OrdersUsed<casts||b.GuerrillaOrderBusy);i++)
            {
                b.Advance(.05f,.05f);
                if(b.LatestGuerrillaOrder.Sequence!=lastSequence)
                {
                    Assert.That(previous.Age,Is.EqualTo(GuerrillaOrderVolley.PresentationDuration));
                    Assert.That(previous.Resolved,Is.True);Assert.That(previous.CompletedStrikes,Is.EqualTo(5));
                    Assert.That(b.LatestGuerrillaOrder.Sequence,Is.EqualTo(lastSequence+1));
                    previous=b.LatestGuerrillaOrder;lastSequence=previous.Sequence;
                }
            }
            Assert.That(b.OrdersUsed,Is.EqualTo(casts));Assert.That(b.Orders,Is.EqualTo(remaining));
            var last=b.LatestGuerrillaOrder;Assert.That(last.SourceIds.Count,Is.EqualTo(participants));
            Assert.That(b.TryGuerrillaOrder(b.RunId,2000),Is.False);Assert.That(b.LatestGuerrillaOrder,Is.SameAs(last));Assert.That(b.OrdersUsed,Is.EqualTo(casts));
            Assert.That(b.CombatShots.Any(s=>s.SynergyOrder),Is.False);
            b.Advance(4,4);Assert.That(last.Resolved,Is.True);Assert.That(last.CompletedStrikes,Is.EqualTo(5));Assert.That(last.Hits.Count,Is.EqualTo(2));Assert.That(b.Orders,Is.EqualTo(remaining),"Global orders cannot recharge themselves.");
        }

        [Test]public void EightNeedsOneRealContactAndDoesNotResetAnUnfinishedOrdinaryAction()
        {
            var b=Fixture(8,actionDuration:1);Assert.That(b.Orders,Is.EqualTo(2));
            var receipt=b.TryDeployRole(b.RunId,1,b.Entry,Native[0]);Assert.That(receipt.Success,Is.True);
            Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.False);Assert.That(b.LatestGuerrillaOrder,Is.Null);
            b.Advance(.05f);var source=b.FindUnit(receipt.EntityId);Assert.That(b.Orders,Is.EqualTo(2));Assert.That(b.LatestGuerrillaOrder,Is.Null);
            Assert.That(b.Clock-source.LastAttackTime,Is.LessThan(1),"The real ordinary animation is still inside its action window.");
            double attack=source.LastAttackTime,next=(double)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).GetValue(source);
            int ordinary=source.OrdinaryAttacksFired;bool moving=source.IsMoving,empowered=source.LastAttackEmpowered;
            b.Advance(.05f);Assert.That(b.LatestGuerrillaOrder,Is.Not.Null);Assert.That(b.OrdersUsed,Is.EqualTo(1));Assert.That(b.Orders,Is.Zero);
            Assert.That(source.LastAttackTime,Is.EqualTo(attack));Assert.That(source.LastAttackEmpowered,Is.EqualTo(empowered));Assert.That(source.IsMoving,Is.EqualTo(moving));
            Assert.That((double)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).GetValue(source),Is.EqualTo(next));Assert.That(source.OrdinaryAttacksFired,Is.EqualTo(ordinary));
            Assert.That(source.SynergyShield,Is.EqualTo(source.MaxHealth*.15f));
            // This high-tier fixture has Reunion six: 10*1.3*1.8=23.4 per guard.
            b.Advance(2.81f,2.81f);Assert.That(b.LatestGuerrillaOrder.Hits.All(h=>Math.Abs(h.Damage-23.4f)<.2f),Is.True);
        }

        [Test]public void NineCanOpenFromOutsideAllAttackRangesWithoutStoppingTheMovingSource()
        {
            var b=Fixture(9,near:false);Assert.That(b.Orders,Is.EqualTo(2));
            var receipt=b.TryDeployRole(b.RunId,1,b.Entry,Native[0]);Assert.That(receipt.Success,Is.True);b.Advance(.05f,0);
            var source=b.FindUnit(receipt.EntityId);Assert.That(source.IsMoving,Is.True);Assert.That(source.OrdinaryAttacksFired,Is.Zero);
            Assert.That(b.TryGuerrillaOrder(b.RunId,100,true),Is.False,"Only the six-member tier can split orders.");
            Assert.That(b.LatestGuerrillaOrder,Is.Not.Null);Assert.That(b.OrdersUsed,Is.EqualTo(1));Assert.That(source.IsMoving,Is.True);Assert.That(b.Orders,Is.Zero);
            var volley=b.LatestGuerrillaOrder;b.Advance(.89f,.89f);Assert.That(volley.Hits,Is.Empty);b.Advance(.02f,.02f);
            Assert.That(volley.Hits.Count,Is.EqualTo(2));Assert.That(volley.Resolved,Is.False);Assert.That(volley.CompletedStrikes,Is.EqualTo(1));
            // Reunion six and the ninth-tier order give 10*1.3*3=39; first weight is .15.
            Assert.That(volley.Hits.All(h=>Math.Abs(h.Damage-5.85f)<.2f),Is.True);
            b.Advance(1.9f,1.9f);Assert.That(volley.Resolved,Is.True);Assert.That(volley.Hits.All(h=>Math.Abs(h.Damage-39)<.2f),Is.True);
        }

        [Test]public void HighTierStillRejectsSkillRevivalControlAndSupportOnlySources()
        {
            var skill=Fixture(9,near:false,skill:true);var active=skill.TryDeployRole(skill.RunId,1,skill.Entry,Native[0]);
            Assert.That(skill.TryActivateSkill(skill.RunId,2,active.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(skill.TryGuerrillaOrder(skill.RunId,100),Is.False);Assert.That(skill.Orders,Is.EqualTo(2));Assert.That(skill.LatestGuerrillaOrder,Is.Null);
            var controlled=Fixture(9,near:false);var receipt=controlled.TryDeployRole(controlled.RunId,1,controlled.Entry,Native[0]);var unit=controlled.FindUnit(receipt.EntityId);
            // Explicit state fixtures isolate the unchanged exclusion gates, not military-order income.
            typeof(DeployedUnitState).GetProperty("FearUntil").SetValue(unit,100d);
            Assert.That(controlled.TryGuerrillaOrder(controlled.RunId,100),Is.False);
            typeof(DeployedUnitState).GetProperty("FearUntil").SetValue(unit,double.NegativeInfinity);
            typeof(DeployedUnitState).GetProperty("IsReviving").SetValue(unit,true);
            Assert.That(controlled.TryGuerrillaOrder(controlled.RunId,101),Is.False);Assert.That(controlled.Orders,Is.EqualTo(2));
            var support=Fixture(9,near:false);long request=1;
            foreach(string id in Native.Take(3))Assert.That(support.TryDeployRole(support.RunId,request++,support.Entry,id).Success,Is.True);
            Assert.That(support.Units.Any(u=>u.SynergySupport),Is.True);
            foreach(var original in support.Units.Where(u=>!u.SynergySupport).ToArray())Assert.That(support.TryDamageUnit(original.EntityId,100000,CombatDamageType.Arts),Is.True);
            Assert.That(support.TryGuerrillaOrder(support.RunId,100),Is.False);Assert.That(support.Orders,Is.EqualTo(2));
        }

        [Test]public void AutomaticOrderWaitsForLiveEligibleSourcesAndResumeThenKeepsAllFiveCuesAndTail()
        {
            var b=Fixture(9,near:false);b.Advance(.1f,.1f);
            Assert.That(b.LatestGuerrillaOrder,Is.Null);Assert.That(b.Orders,Is.EqualTo(2),"A full balance cannot be spent into an empty field.");
            var receipt=b.TryDeployRole(b.RunId,1,b.Entry,Native[0]);Assert.That(receipt.Success,Is.True);var source=b.FindUnit(receipt.EntityId);
            b.SetPaused(true);double clock=b.Clock;b.Advance(3,3);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.LatestGuerrillaOrder,Is.Null);Assert.That(b.Orders,Is.EqualTo(2));
            b.SetPaused(false);typeof(DeployedUnitState).GetProperty("FearUntil").SetValue(source,100d);b.Advance(.1f,.1f);
            Assert.That(b.LatestGuerrillaOrder,Is.Null);Assert.That(b.Orders,Is.EqualTo(2));
            typeof(DeployedUnitState).GetProperty("FearUntil").SetValue(source,double.NegativeInfinity);b.Advance(.05f,.05f);
            var order=b.LatestGuerrillaOrder;Assert.That(order,Is.Not.Null);Assert.That(order.Age,Is.Zero,"The commit does not inherit elapsed time from the substep that found its source.");
            Assert.That(b.OrdersUsed,Is.EqualTo(1));Assert.That(b.Orders,Is.Zero);Assert.That(source.OrdinaryAttacksFired,Is.Zero);
            b.Advance(2.79f,2.79f);Assert.That(order.CompletedStrikes,Is.EqualTo(4));Assert.That(order.Resolved,Is.False);
            // Four 15% strikes total 60% of the 10*1.3*3=39 post-mitigation budget.
            foreach(var hit in order.Hits)Assert.That(hit.Damage,Is.EqualTo(23.4f).Within(.2f));
            b.Advance(.01f,.01f);Assert.That(order.CompletedStrikes,Is.EqualTo(5));Assert.That(order.Resolved,Is.True);
            foreach(var hit in order.Strikes[4].Hits)Assert.That(hit.Damage,Is.EqualTo(15.6f).Within(.2f));
            Assert.That(b.GuerrillaOrderBusy,Is.True,"The final real hit cannot permit an overlapping cinematic during its recoil.");
            b.Advance(.74f,.74f);Assert.That(b.GuerrillaOrderBusy,Is.True);b.Advance(.02f,.02f);Assert.That(b.GuerrillaOrderBusy,Is.False);
            Assert.That(b.LatestGuerrillaOrder,Is.SameAs(order));Assert.That(b.Orders,Is.Zero);Assert.That(b.OrdersUsed,Is.EqualTo(1));
        }

        [Test]public void SixMemberPendingHalfBlocksAutomaticFullUntilFreshBackHalfAndItsTailComplete()
        {
            var b=Fixture(6);long request=1;
            foreach(string id in Native.Take(3))Assert.That(b.TryDeployRole(b.RunId,request++,b.Entry,id).Success,Is.True);
            b.Advance(.15f);Assert.That(b.Orders,Is.EqualTo(3));Assert.That(b.LatestGuerrillaOrder,Is.Null);
            Assert.That(b.TryGuerrillaOrder(b.RunId,1000,true),Is.True);var first=b.LatestGuerrillaOrder;
            foreach(var card in b.Cards.Skip(3))Assert.That(b.TryDeployRole(b.RunId,request++,b.Entry,card.Settings.CardId).Success,Is.True);
            b.Advance(.35f);foreach(var unit in b.Units)typeof(DeployedUnitState).GetField("NextAttackTime",Hidden).SetValue(unit,1000d);
            Assert.That(b.Orders,Is.EqualTo(7));Assert.That(b.TryGuerrillaOrder(b.RunId,1001,true),Is.False);
            b.Advance(GuerrillaOrderVolley.PresentationDuration-first.Age+.01f);
            Assert.That(b.LatestGuerrillaOrder,Is.SameAs(first));Assert.That(b.HalfOrderPending,Is.True);Assert.That(b.Orders,Is.EqualTo(7));
            Assert.That(b.TryGuerrillaOrder(b.RunId,1002),Is.False);
            Assert.That(b.TryGuerrillaOrder(b.RunId,1003,true),Is.True);var second=b.LatestGuerrillaOrder;
            Assert.That(first.SourceIds.Intersect(second.SourceIds),Is.Empty);Assert.That(second.SourceIds.Count,Is.EqualTo(6));
            Assert.That(b.HalfOrderPending,Is.False);Assert.That(b.OrdersUsed,Is.EqualTo(1));Assert.That(b.Orders,Is.EqualTo(4));
            b.Advance(GuerrillaOrderVolley.PresentationDuration+.01f);Assert.That(second.Resolved,Is.True);
            Assert.That(b.LatestGuerrillaOrder,Is.SameAs(second),"Four remaining points cannot start a six-point full order.");
        }

        [Test]public void ExtraMembershipIsAnAtomicDistinctSnapshotAndReplacementKeepsOtherTags()
        {
            var b=Fixture(9,begin:false);var pieces=b.Cards.Select(c=>Piece(c.Settings.CardId)).ToArray();
            var previous=b.GuerrillaMemberIds;var tiers=new[]{b.ReunionTier,b.SnowTier,b.InfectionTier,b.MudrockTier,b.HerdTier,b.DeepTier,b.SarkazTier,b.MarineTier};
            foreach(var invalid in new[]{new[]{Other[0],Other[1],Other[2]},new[]{"CARD_M10"},new[]{"UNKNOWN"},new string[]{null}})
            {
                Assert.Throws<ArgumentException>(()=>b.ConfigureSynergies(pieces,extraGuerrillaMembers:invalid));
                Assert.That(b.GuerrillaTier,Is.EqualTo(9));CollectionAssert.AreEqual(previous,b.GuerrillaMemberIds);
                CollectionAssert.AreEqual(tiers,new[]{b.ReunionTier,b.SnowTier,b.InfectionTier,b.MudrockTier,b.HerdTier,b.DeepTier,b.SarkazTier,b.MarineTier});Assert.That(b.Orders,Is.Zero);
            }
            int enumerations=0;
            IEnumerable<ShopTrialModel.Piece> Once(){if(++enumerations!=1)throw new InvalidOperationException("Lineup enumerated twice");foreach(var p in pieces)yield return p;}
            var input=new List<string>{Other[0],Other[1],Other[1],Native[0],Native[0]};
            b.ConfigureSynergies(Once(),extraGuerrillaMembers:input);input.Clear();Assert.That(b.GuerrillaMemberIds.Count,Is.EqualTo(9));Assert.That(b.IsGuerrillaMember(Other[0]),Is.True);
            b.ConfigureSynergies(pieces.Concat(new[]{Piece(Native[0])}),extraGuerrillaMembers:new[]{Other[0]});
            Assert.That(b.GuerrillaTier,Is.EqualTo(8));Assert.That(b.GuerrillaMemberIds.Count,Is.EqualTo(8));Assert.That(b.IsGuerrillaMember(Other[1]),Is.False);
            b.ConfigureSynergies(pieces);Assert.That(b.GuerrillaTier,Is.EqualTo(6));Assert.That(b.GuerrillaMemberIds.Count,Is.EqualTo(7));Assert.That(previous.Count,Is.EqualTo(9));
            CollectionAssert.AreEqual(tiers,new[]{b.ReunionTier,b.SnowTier,b.InfectionTier,b.MudrockTier,b.HerdTier,b.DeepTier,b.SarkazTier,b.MarineTier});
        }

        [Test]public void InitialOrdersAreGrantedOnlyOnBeginAndRestartRetainsOnlyTheMembershipSnapshot()
        {
            var b=Fixture(9,begin:false);var pieces=b.Cards.Select(c=>Piece(c.Settings.CardId)).ToArray();
            for(int i=0;i<3;i++){b.ConfigureSynergies(pieces,extraGuerrillaMembers:Extras(9));Assert.That(b.Orders,Is.Zero);}
            b.Begin();Assert.That(b.Orders,Is.EqualTo(2));b.Begin();Assert.That(b.Orders,Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(()=>b.ConfigureSynergies(pieces,extraGuerrillaMembers:Extras(9)));Assert.That(b.Orders,Is.EqualTo(2));
            b.Restart();Assert.That(b.GuerrillaTier,Is.EqualTo(9));Assert.That(b.GuerrillaMemberIds.Count,Is.EqualTo(9));Assert.That(b.Orders,Is.Zero);
            b.Begin();Assert.That(b.Orders,Is.EqualTo(2));b.Restart();b.ConfigureSynergies(pieces);b.Begin();Assert.That(b.GuerrillaTier,Is.EqualTo(6));Assert.That(b.Orders,Is.Zero);
        }
    }
}
