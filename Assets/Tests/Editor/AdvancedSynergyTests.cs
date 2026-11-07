using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class AdvancedSynergyTests
    {
        static readonly string[] Infection={"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,5,8,2,.1f,200,1,1,1,1,1);
        static EngagementSettings Combat(MonsterEffectSettings effects=default)=>new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:3,unitInterval:2,defenderHealth:10000,defenderAttack:1,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:effects);
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        static ManualBattle Battle(string[] ids,InfectionForm form=InfectionForm.Swarm,InfectionForm second=InfectionForm.Shell,int xp=0,bool far=false,MonsterEffectSettings effects=default)
        {
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,new Vector3(10,0,0)},far?new Vector3(50,0,50):new Vector3(.6f,0,0),Combat(effects));
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(effects),true)));b.ConfigureSynergies(ids.Select(Piece),false,form,second,xp);b.Begin();return b;
        }
        [Test]public void SidebarCountsDistinctFormationMembersAndIncludesPendingTags()
        {
            var rows=SynergySummary.From(new[]{"CARD_M03","CARD_M03","CARD_M15","CARD_M26","CARD_M37"});
            Assert.That(rows.Single(r=>r.Name=="游击队").Count,Is.EqualTo(2));Assert.That(rows.Single(r=>r.Name=="游击队").Tier,Is.EqualTo(2));
            Assert.That(rows.Single(r=>r.Name=="感染生物").Count,Is.EqualTo(1));Assert.That(rows.Single(r=>r.Name=="感染生物").Tier,Is.Zero);
            Assert.That(rows.Single(r=>r.Name=="萨卡兹").Count,Is.EqualTo(2));Assert.That(rows.Single(r=>r.Name=="萨卡兹").Implemented,Is.True);
            Assert.That(rows.Any(r=>r.Name=="雪怪小队"),Is.False);Assert.That(rows.All(r=>!string.IsNullOrEmpty(r.Effect)),Is.True);
        }
        [Test]public void ApprovedRosterIncludesSilverWolfAndKeepsBossTags()
        {
            Assert.That(CampaignBalance.Profiles.Count,Is.EqualTo(43));
            Assert.That(CampaignBalance.Profiles.Count(p=>p.Value.Cost==5),Is.EqualTo(8));
            Assert.That(CampaignBalance.Profiles[SilverWolfCombat.CardId].Cost,Is.EqualTo(5));
            Assert.That(CampaignBalance.Supports("CARD_M20"));
            Assert.That(CampaignBalance.Tags("CARD_M25"),Is.EqualTo(new[]{"牧群","整合运动"}));
            Assert.That(CampaignBalance.Supports("CARD_M32"));
            Assert.That(CampaignBalance.Reunion("CARD_M33"),Is.False);Assert.That(CampaignBalance.MudrockCrew("CARD_M26"));Assert.That(CampaignBalance.Tags("CARD_M26"),Does.Contain("萨卡兹"));
        }
        [Test]public void SwarmPaysOnceAndOnlyPrimaryCanDamageGate()
        {
            var b=Battle(Infection,far:true);var r=b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);Assert.That(r.Success);
            Assert.That(b.Units.Count,Is.EqualTo(3));Assert.That(b.DP,Is.EqualTo(98));Assert.That(b.FindCard(Infection[0]).RemainingUses,Is.EqualTo(7));
            Assert.That(b.Units.All(u=>Math.Abs(u.MaxHealth-110)<.01));Assert.That(b.Units.Count(u=>!u.SynergySupport),Is.EqualTo(1));
            b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);Assert.That(b.Units.Count,Is.EqualTo(3));
            b.Advance(12);Assert.That(b.Defense,Is.EqualTo(4));Assert.That(b.Units.All(u=>u.Resolution==UnitResolution.Breached));
        }
        [Test]public void SplitIntrinsicBlastBudgetDoesNotMultiplyAndSecondaryHasNoCold()
        {
            var e=new MonsterEffectSettings(blastAttack:75,blastRadius:1.2f,coldDuration:2);
            var b=Battle(Infection,effects:e);b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M04");
            Assert.That(b.Units.Sum(u=>b.UnitCombat(u).Effects.BlastAttack),Is.EqualTo(75).Within(.001));
            Assert.That(b.Units.Where(u=>u.SynergySupport).All(u=>b.UnitCombat(u).Effects.ColdDuration==0));
        }
        [Test]public void BurstTriggersOnceAfterFinalDeathAndDoesNotGenerateOrders()
        {
            var b=Battle(Infection.Take(2).ToArray(),InfectionForm.Burst);b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);
            var u=b.Units[0];b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);Assert.That(b.HasPendingDeathBlast);
            b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.Advance(.3f);
            Assert.That(b.InfectionBursts.Count,Is.EqualTo(1));Assert.That(b.InfectionBursts[0].Resolved);Assert.That(b.Defender.Health,Is.EqualTo(9965));Assert.That(b.Orders,Is.Zero);
        }
        [Test]public void ShellAndSixTierSecondaryUseDifferentStrengths()
        {
            var b=Battle(Infection,InfectionForm.Shell,InfectionForm.Burst);b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);
            Assert.That(b.Units.Count,Is.EqualTo(1));Assert.That(b.Units[0].SynergyShield,Is.EqualTo(70));Assert.That(b.UnitCombat(b.Units[0]).UnitArmor,Is.EqualTo(3));
            b.TryDamageUnit(b.Units[0].EntityId,10000,CombatDamageType.Physical);Assert.That(b.InfectionBursts[0].Attack,Is.EqualTo(35));
        }
        [Test]public void OrdersUseDirectContactsOnceAndDoNotRecursivelyGenerateOrResetAttackCycles()
        {
            var ids=new[]{"CARD_M15","CARD_M16","CARD_M07","CARD_M08"};var b=Battle(ids);
            for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);EarnQuietContacts(b);
            Assert.That(b.Orders,Is.EqualTo(4));int attacks=b.Units.Sum(u=>u.OrdinaryAttacksFired);
            Assert.That(b.TryGuerrillaOrder(b.RunId,100));Assert.That(b.Orders,Is.Zero);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            int strikes=b.OrderStrikes;Assert.That(b.TryGuerrillaOrder(b.RunId,100));Assert.That(b.OrderStrikes,Is.EqualTo(strikes));
            var volley=b.LatestGuerrillaOrder;Assert.That(volley,Is.Not.Null);Assert.That(volley.SourceIds.Count,Is.EqualTo(strikes));
            Assert.That(b.CombatShots.Any(s=>s.SynergyOrder),Is.False,"A military order must not also emit the old single-target shot.");
            b.Advance(.3f);Assert.That(volley.Resolved,Is.False);
            b.Advance(2.51f);Assert.That(volley.Resolved,Is.True);Assert.That(volley.CompletedStrikes,Is.EqualTo(5));Assert.That(volley.Hits.Count,Is.EqualTo(1));
            Assert.That(b.Orders,Is.Zero);Assert.That(b.Units.Sum(u=>u.OrdinaryAttacksFired),Is.EqualTo(attacks));
            float health=b.Defender.Health;Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);b.Advance(.1f);
            Assert.That(b.LatestGuerrillaOrder,Is.SameAs(volley));Assert.That(b.Defender.Health,Is.EqualTo(health));
            Assert.That(b.TryGuerrillaOrder(b.RunId,101),Is.False,"No points remain; OrdersUsed is now statistics, not a cast cap.");Assert.That(b.OrdersUsed,Is.EqualTo(1));
        }
        [Test]public void NoTargetOrPauseCannotSpendOrders()
        {
            var ids=new[]{"CARD_M15","CARD_M16","CARD_M07","CARD_M08"};var b=Battle(ids);for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);EarnQuietContacts(b);
            b.SetPaused(true);Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.False);Assert.That(b.Orders,Is.EqualTo(4));
            Assert.That(b.LatestGuerrillaOrder,Is.Null);
            b.SetPaused(false);foreach(var u in b.Units.Where(u=>!u.SynergySupport))b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);
            Assert.That(b.TryGuerrillaOrder(b.RunId,101),Is.False);Assert.That(b.Orders,Is.EqualTo(4));
        }
        [Test]public void HalfOrdersRequireFreshParticipantsAndDoNotDoubleStrikeFirstHalf()
        {
            var ids=new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};var b=Battle(ids);
            for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);EarnQuietContacts(b);Assert.That(b.Orders,Is.EqualTo(6));
            Assert.That(b.TryGuerrillaOrder(b.RunId,100,true));Assert.That(b.HalfOrderPending);Assert.That(b.Orders,Is.EqualTo(3));
            var first=b.LatestGuerrillaOrder;
            Assert.That(b.TryGuerrillaOrder(b.RunId,101,true),Is.False);Assert.That(b.Orders,Is.EqualTo(3));Assert.That(b.TryGuerrillaOrder(b.RunId,102),Is.False);
            b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M15");b.Advance(.3f);
            Assert.That(b.TryGuerrillaOrder(b.RunId,103,true),Is.False,"The back half cannot overlap the front-half presentation.");
            Assert.That(b.TryGuerrillaOrder(b.RunId,100,true),Is.True,"The committed front-half request remains idempotent while busy.");
            b.Advance(GuerrillaOrderVolley.PresentationDuration-first.Age+.001f);
            Assert.That(b.TryGuerrillaOrder(b.RunId,103,true));
            Assert.That(b.HalfOrderPending,Is.False);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            var second=b.LatestGuerrillaOrder;
            Assert.That(first.SourceIds.Count+second.SourceIds.Count,Is.EqualTo(7));Assert.That(first.SourceIds.Intersect(second.SourceIds),Is.Empty);
            Assert.That(b.OrderStrikes,Is.EqualTo(7));Assert.That(b.CombatShots.Any(s=>s.SynergyOrder),Is.False);
            b.Advance(.91f);Assert.That(first.Resolved,Is.True);Assert.That(second.Resolved,Is.False);Assert.That(second.CompletedStrikes,Is.EqualTo(1));
            b.Advance(1.90f);Assert.That(second.Resolved,Is.True);Assert.That(first.Hits.Count,Is.EqualTo(1));Assert.That(second.Hits.Count,Is.EqualTo(1));
        }

        static void EarnQuietContacts(ManualBattle b)
        {
            // Launch ordinary projectiles, then temporarily keep their sources inside an
            // unfinished ordinary action so automatic full orders cannot claim this fixture.
            // The projectiles land normally and generate every point; no order/HP is injected.
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            b.Advance(.01f,0);var attackTimes=b.Units.ToDictionary(u=>u.EntityId,u=>u.LastAttackTime);
            for(int i=0;i<40;i++)
            {
                foreach(var unit in b.Units)
                {
                    typeof(DeployedUnitState).GetField("NextAttackTime",hidden).SetValue(unit,1000d);
                    typeof(DeployedUnitState).GetProperty("LastAttackTime").SetValue(unit,1000d);
                }
                b.Advance(.01f,0);
            }
            foreach(var unit in b.Units)typeof(DeployedUnitState).GetProperty("LastAttackTime").SetValue(unit,attackTimes.TryGetValue(unit.EntityId,out double at)?at:double.NegativeInfinity);
            Assert.That(b.LatestGuerrillaOrder,Is.Null,"The quiet fixture holds eligibility, not military-order income.");
            Assert.That(b.CombatShots.Any(s=>!s.Resolved),Is.False);
        }

        static EngagementSettings VolleyCombat(float attack,CombatDamageType type,float resistance)=>new EngagementSettings(
            unitAttack:attack,unitRange:3,unitInterval:100,defenderHealth:10000,defenderAttack:1,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
            effects:new MonsterEffectSettings(damageType:type,resistance:resistance));
        static ManualBattle VolleyBattle(bool mixed=true,float nearArmor=5)
        {
            var ids=new[]{"CARD_M15","CARD_M16","CARD_M07","CARD_M08"};
            var guards=new[]{
                new DefenderSettings("near","Near",null,new Vector3(.6f,0,0),10000,1,nearArmor,.1f,100,canAttack:false),
                new DefenderSettings("other-lane","Other lane",null,new Vector3(3,0,12),10000,1,10,.1f,100,canAttack:false),
                new DefenderSettings("far","Far",null,new Vector3(40,0,40),10000,1,0,.1f,100,canAttack:false),
                new DefenderSettings("reserve","Reserve",null,new Vector3(1,0,1),10000,1,0,.1f,100,canAttack:false,reserve:true)};
            var lanes=new[]{new DeploymentLane("north","blue-north",new[]{Vector3.zero,new Vector3(10,0,0)}),
                new DeploymentLane("south","blue-south",new[]{new Vector3(0,0,12),new Vector3(10,0,12)})};
            var b=new ManualBattle(Unit(ids[0]),lanes[0].Points,guards[0].Position,VolleyCombat(20,CombatDamageType.Physical,25),roster:guards,deploymentLanes:lanes);
            b.ConfigureCardLoadout(ids.Skip(1).Select((id,index)=>new BasicRosterProfile(Unit(id),
                VolleyCombat(mixed&&index%2==0?40:20,mixed&&index%2==0?CombatDamageType.Arts:CombatDamageType.Physical,25),true)));
            b.ConfigureSynergies(ids.Select(Piece));b.Begin();b.ConfigureDefenderHighGround("far",true);
            for(int i=0;i<ids.Length;i++)Assert.That(b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]).Success,Is.True);
            EarnQuietContacts(b);Assert.That(b.Orders,Is.EqualTo(4),"Orders must come from real ordinary hits.");return b;
        }

        [Test]public void GlobalOrderHitsEveryLivingLaneAndHighGroundWithMixedBudgetsAndRealFeedback()
        {
            var b=VolleyBattle();var before=b.Defenders.Select(d=>d.Health).ToArray();int feedback=b.CombatFeedback.Count;
            Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);var order=b.LatestGuerrillaOrder;
            Assert.That(order.SourceIds.Count,Is.EqualTo(4));Assert.That(b.OrderStrikes,Is.EqualTo(4));Assert.That(order.Hits,Is.Empty);
            b.Advance(.89f,.89f);Assert.That(order.Resolved,Is.False);CollectionAssert.AreEqual(before,b.Defenders.Select(d=>d.Health));
            b.Advance(.01f,.01f);Assert.That(order.Resolved,Is.False);Assert.That(order.CompletedStrikes,Is.EqualTo(1));
            Assert.That(order.Hits.Single(h=>h.TargetId=="near").Damage,Is.EqualTo(7.5f).Within(.01));
            b.Advance(1.90f,1.90f);Assert.That(order.Resolved,Is.True);Assert.That(order.Cancelled,Is.False);
            CollectionAssert.AreEquivalent(new[]{"near","other-lane","far"},order.Hits.Select(h=>h.TargetId));
            // Each defender receives 2*(20*.6-armor) plus 2*(40*.6*.75), independently.
            Assert.That(order.Hits.Single(h=>h.TargetId=="near").Damage,Is.EqualTo(50).Within(.01));
            Assert.That(order.Hits.Single(h=>h.TargetId=="other-lane").Damage,Is.EqualTo(40).Within(.01));
            Assert.That(order.Hits.Single(h=>h.TargetId=="far").Damage,Is.EqualTo(60).Within(.01));
            Assert.That(order.Strikes.Count,Is.EqualTo(5));
            for(int i=0;i<5;i++)
            {
                var strike=order.Strikes[i];Assert.That(strike.Index,Is.EqualTo(i));Assert.That(strike.At,Is.EqualTo(new[]{.9f,1.2f,1.5f,1.8f,2.8f}[i]));
                Assert.That(strike.Hits.Single(h=>h.TargetId=="near").Damage,Is.EqualTo(i==4?20:7.5f).Within(.01));
            }
            Assert.That(b.FindDefender("reserve").Health,Is.EqualTo(before[3]));
            foreach(var hit in order.Hits)
            {
                Assert.That(hit.WorldPosition,Is.EqualTo(b.FindDefender(hit.TargetId).Position));
                float applied=b.CombatFeedback.Skip(feedback).Where(e=>e.TargetType==CombatFeedbackTarget.Defender&&e.TargetId==hit.TargetId&&e.Kind==CombatFeedbackKind.HealthLost).Sum(e=>e.Amount);
                Assert.That(applied,Is.EqualTo(hit.Damage).Within(.001));
            }
            var after=b.Defenders.Select(d=>d.Health).ToArray();Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);b.Advance(.3f,.3f);
            CollectionAssert.AreEqual(after,b.Defenders.Select(d=>d.Health));Assert.That(b.LatestGuerrillaOrder,Is.SameAs(order));Assert.That(b.Orders,Is.Zero);
            Assert.That(b.CombatShots.Any(s=>s.SynergyOrder),Is.False);
        }

        [Test]public void SamePresentationTimeResolvesAllFiveStrikesRegardlessOfOneOrTwoSpeedSimulation()
        {
            var one=VolleyBattle();var two=VolleyBattle();
            Assert.That(one.TryGuerrillaOrder(one.RunId,100));Assert.That(two.TryGuerrillaOrder(two.RunId,100));
            double first=one.Clock,second=two.Clock;
            one.Advance(.89f,.89f);two.Advance(1.78f,.89f);
            Assert.That(one.Clock-first,Is.EqualTo(.89).Within(.0001));Assert.That(two.Clock-second,Is.EqualTo(1.78).Within(.0001));
            Assert.That(one.LatestGuerrillaOrder.Age,Is.EqualTo(two.LatestGuerrillaOrder.Age).Within(.00001));
            Assert.That(one.LatestGuerrillaOrder.Resolved||two.LatestGuerrillaOrder.Resolved,Is.False);
            one.Advance(.01f,.01f);two.Advance(.02f,.01f);
            Assert.That(one.LatestGuerrillaOrder.CompletedStrikes,Is.EqualTo(1));Assert.That(two.LatestGuerrillaOrder.CompletedStrikes,Is.EqualTo(1));
            one.Advance(1.9f,1.9f);two.Advance(3.8f,1.9f);
            Assert.That(one.LatestGuerrillaOrder.Age,Is.EqualTo(2.8f).Within(.000001f));
            Assert.That(two.LatestGuerrillaOrder.Age,Is.EqualTo(2.8f).Within(.000001f));
            Assert.That(one.LatestGuerrillaOrder.Resolved&&two.LatestGuerrillaOrder.Resolved,Is.True);
            CollectionAssert.AreEqual(one.LatestGuerrillaOrder.Hits.Select(h=>h.Damage),two.LatestGuerrillaOrder.Hits.Select(h=>h.Damage));
        }

        [Test]public void PauseTerminalAndRestartCannotApplyOrRetainAnUnfinishedOrder()
        {
            var b=VolleyBattle();Assert.That(b.TryGuerrillaOrder(b.RunId,100));var order=b.LatestGuerrillaOrder;b.Advance(.4f,.4f);
            var health=b.Defenders.Select(d=>d.Health).ToArray();float age=order.Age;double clock=b.Clock;
            b.SetPaused(true);b.Advance(10,5);Assert.That(order.Age,Is.EqualTo(age));Assert.That(order.Resolved,Is.False);Assert.That(b.Clock,Is.EqualTo(clock));CollectionAssert.AreEqual(health,b.Defenders.Select(d=>d.Health));
            b.SetPaused(false);typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{b.RunId,false});b.Advance(10,10);
            Assert.That(order.Cancelled&&order.Resolved,Is.True);Assert.That(order.Hits,Is.Empty);Assert.That(order.Age,Is.EqualTo(age));CollectionAssert.AreEqual(health,b.Defenders.Select(d=>d.Health));
            b.Restart();Assert.That(b.LatestGuerrillaOrder,Is.Null);Assert.That(b.Orders,Is.Zero);Assert.That(b.OrderStrikes,Is.Zero);
            b.Begin();b.Advance(2,2);Assert.That(b.LatestGuerrillaOrder,Is.Null);
        }

        [TestCase(false)][TestCase(true)]public void OnlySurvivingParticipantsContributeAndAllDeadCancels(bool killAll)
        {
            var b=VolleyBattle(false);Assert.That(b.TryGuerrillaOrder(b.RunId,100));var order=b.LatestGuerrillaOrder;
            foreach(string id in killAll?order.SourceIds:order.SourceIds.Take(1).ToArray())Assert.That(b.TryDamageUnit(id,100000,CombatDamageType.Arts),Is.True);
            var before=b.Defenders.Select(d=>d.Health).ToArray();b.Advance(2.81f,2.81f);Assert.That(order.Resolved,Is.True);
            if(killAll){Assert.That(order.Cancelled,Is.True);Assert.That(order.Hits,Is.Empty);CollectionAssert.AreEqual(before,b.Defenders.Select(d=>d.Health));}
            else {Assert.That(order.Cancelled,Is.False);Assert.That(order.Hits.Single(h=>h.TargetId=="near").Damage,Is.EqualTo(21).Within(.01));}
        }
        [Test]public void StrikeBoundariesKeepFinalFortyPercentAndMinimumDamageBudgetWithoutRenderSideEffects()
        {
            var b=VolleyBattle(false,100);Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);var order=b.LatestGuerrillaOrder;
            var start=b.Defenders.ToDictionary(d=>d.EntityId,d=>d.Health);
            for(int i=0;i<5;i++)
            {
                float at=new[]{.9f,1.2f,1.5f,1.8f,2.8f}[i];float step=at-order.Age-.005f;
                b.Advance(step,step);Assert.That(order.CompletedStrikes,Is.EqualTo(i));
                b.Advance(.005f,.005f);Assert.That(order.CompletedStrikes,Is.EqualTo(i+1));
                var hit=order.Strikes[i].Hits.Single(h=>h.TargetId=="near");
                Assert.That(hit.Damage,Is.EqualTo(i==4?1.6f:.6f).Within(.01),"Four sources each retain one full minimum-damage budget, divided after mitigation.");
                Assert.That(order.Resolved,Is.EqualTo(i==4));
            }
            Assert.That(order.Hits.Single(h=>h.TargetId=="near").Damage,Is.EqualTo(4).Within(.01));
            Assert.That(start["near"]-b.FindDefender("near").Health,Is.EqualTo(order.Hits.Single(h=>h.TargetId=="near").Damage));
            Assert.That(b.Orders,Is.Zero);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            var immutableFirst=order.Strikes[0].Hits.Select(h=>h.Damage).ToArray();b.Advance(1,1);
            CollectionAssert.AreEqual(immutableFirst,order.Strikes[0].Hits.Select(h=>h.Damage));Assert.That(order.CompletedStrikes,Is.EqualTo(5));
        }
        [Test]public void DeathBetweenStrikesAndTerminalCancellationRetainOnlyPreviouslyAppliedLosses()
        {
            var b=VolleyBattle();Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);var order=b.LatestGuerrillaOrder;b.Advance(.91f,.91f);
            var removed=b.FindDefender("far");typeof(DefenderState).GetProperty("Health").SetValue(removed,0f);
            b.Advance(.30f,.30f);Assert.That(order.CompletedStrikes,Is.EqualTo(2));
            Assert.That(order.Strikes[1].Hits.Any(h=>h.TargetId==removed.EntityId),Is.False);
            Assert.That(order.Hits.Single(h=>h.TargetId==removed.EntityId).Damage,Is.EqualTo(order.Strikes[0].Hits.Single(h=>h.TargetId==removed.EntityId).Damage));
            var totals=order.Hits.Select(h=>h.Damage).ToArray();var hp=b.Defenders.Select(d=>d.Health).ToArray();
            typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{b.RunId,false});b.Advance(10,10);
            Assert.That(order.Cancelled&&order.Resolved,Is.True);Assert.That(order.CompletedStrikes,Is.EqualTo(2));
            CollectionAssert.AreEqual(totals,order.Hits.Select(h=>h.Damage));CollectionAssert.AreEqual(hp,b.Defenders.Select(d=>d.Health));
        }
        [Test]public void LosingEverySourceAfterFirstStrikeCancelsOnlyTheRemainderAndStillKeepsItsPresentationBusy()
        {
            var b=VolleyBattle(false);Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.True);var order=b.LatestGuerrillaOrder;b.Advance(.91f,.91f);
            foreach(string id in order.SourceIds)Assert.That(b.TryDamageUnit(id,100000,CombatDamageType.Arts),Is.True);
            var hp=b.Defenders.Select(d=>d.Health).ToArray();var totals=order.Hits.Select(h=>h.Damage).ToArray();
            b.Advance(.30f,.30f);Assert.That(order.Cancelled&&order.Resolved,Is.True);Assert.That(order.CompletedStrikes,Is.EqualTo(1));
            Assert.That(b.GuerrillaOrderBusy,Is.True);CollectionAssert.AreEqual(hp,b.Defenders.Select(d=>d.Health));CollectionAssert.AreEqual(totals,order.Hits.Select(h=>h.Damage));
            b.Advance(3,3);Assert.That(order.Age,Is.EqualTo(GuerrillaOrderVolley.PresentationDuration));Assert.That(b.GuerrillaOrderBusy,Is.False);
            Assert.That(order.CompletedStrikes,Is.EqualTo(1));Assert.That(b.LatestGuerrillaOrder,Is.SameAs(order));
        }
        [Test]public void PausedGolemDeploymentIsAtomicLimitedAndKeepsFullAttack()
        {
            var b=Battle(new[]{"CARD_M26","CARD_M27"},xp:100);b.SetPaused(true);
            Assert.That(b.GolemStage,Is.EqualTo(2));Assert.That(b.TryDeployGolem(b.RunId,10,b.Entry).Failure,Is.EqualTo(DeployFailure.Paused));
            var r=b.TryDeployGolem(b.RunId,10,b.Entry,true);Assert.That(r.Success);var u=b.FindUnit(r.EntityId);
            Assert.That(b.DP,Is.EqualTo(95));Assert.That(b.TryDeployGolem(b.RunId,10,b.Entry,true).EntityId,Is.EqualTo(r.EntityId));Assert.That(b.Units.Count,Is.EqualTo(1));
            Assert.That(b.TryDeployGolem(b.RunId,11,b.Entry,true).Failure,Is.EqualTo(DeployFailure.NoUses));
            Assert.That(b.UnitCombat(u).AttackDuration,Is.EqualTo(5.166667f));b.Advance(20);Assert.That(b.Clock,Is.Zero);
        }
        [Test]public void GolemGrowthIsAutomaticWithoutContactsAndSettlesOnce()
        {
            var b=Battle(new[]{"CARD_M26","CARD_M27"});Assert.That(b.Units,Is.Empty);
            Assert.That(b.GolemGrowthEarned,Is.Zero);Win(b);Assert.That(b.GolemGrowthEarned,Is.EqualTo(3));
            var e=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=1}});e.Preset(new[]{Piece("A")},Array.Empty<string>(),1,0,10);
            var run=new BattleShopRun(e,1,false,true);run.Observe(BattlePhase.Won);run.OpenShop();run.PrepareBattle();run.Observe(BattlePhase.Won);run.OpenShop();run.PrepareBattle();Assert.That(run.Observe(b.Phase,b.GolemGrowthEarned,golemCapacity:b.GolemCapacity));Assert.That(run.GolemExperience,Is.EqualTo(3));Assert.That(run.Observe(b.Phase,b.GolemGrowthEarned,golemCapacity:b.GolemCapacity),Is.False);Assert.That(run.GolemExperience,Is.EqualTo(3));
        }
        static void Win(ManualBattle battle)=>typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,new object[]{battle.RunId,true});
        static BasicRosterProfile Real(string id)
        {
            var data=UnityEditor.AssetDatabase.FindAssets("t:ManualBattleDefinition").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(d=>d.Card.CardId==id);
            return CampaignBalance.Build(data,1,100,5);
        }
        static ManualBattle Mixed(params string[] ids)
        {
            var profiles=ids.Distinct().Select(Real).ToArray();var first=profiles[0];
            var b=new ManualBattle(first.Unit,new[]{Vector3.zero,new Vector3(20,0,0)},new Vector3(50,0,50),first.Combat);
            b.ConfigureCardLoadout(profiles.Skip(1));b.ConfigureSynergies(ids.Distinct().Select(Piece));b.Begin();return b;
        }
        [Test]public void MixedFlamechaserAndCreeperUseOwnIntrinsicState()
        {
            var b=Mixed("CARD_M07","CARD_M30","CARD_M33");b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M07");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M30");b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M33");
            var soldier=b.Units.First(u=>u.CardId=="CARD_M07");var flame=b.Units.First(u=>u.CardId=="CARD_M30");var creeper=b.Units.First(u=>u.CardId=="CARD_M33");
            b.TryDamageUnit(flame.EntityId,100000,CombatDamageType.Arts);Assert.That(flame.Active);Assert.That(flame.FlamePhase,Is.EqualTo(FlamechaserPhase.Transforming));
            b.TryDamageUnit(creeper.EntityId,200,CombatDamageType.Arts);Assert.That(creeper.CreeperRetaliations,Is.EqualTo(1));Assert.That(soldier.CreeperRetaliations,Is.Zero);Assert.That(soldier.FlamePhase,Is.EqualTo(FlamechaserPhase.Soldier));
            Assert.That(soldier.Health,Is.EqualTo(soldier.MaxHealth));
        }
        [Test]public void PhalanxDoesNotGiveItsArmorMechanicToOtherSpecies()
        {
            var b=Mixed("CARD_M29","CARD_M07");b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M29");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M07");b.Advance(2.01f);b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M07");
            var soldiers=b.Units.Where(u=>u.CardId=="CARD_M07").ToArray();Assert.That(soldiers.Length,Is.EqualTo(2));foreach(var u in soldiers)Assert.That(b.UnitArmor(u),Is.EqualTo(4));
        }
    }
}
