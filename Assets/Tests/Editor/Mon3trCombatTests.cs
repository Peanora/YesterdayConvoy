using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class Mon3trCombatTests
    {
        static readonly string[] Cards={"CARD_M02","CARD_M07","CARD_M08","CARD_M09"};

        static DefenderPlacement Authored()
        {
            var map=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            Assert.That(map,Is.Not.Null);
            return map.defenders.Single(d=>d.id=="D23");
        }

        static DefenderSettings Mon3tr(bool ready=true,float attackSpeed=1)
        {
            // A detached runtime fixture uses the real tree-map stats and marked skill cells.
            // Only ready tests start at full SP; chain-healing tests retain authored 7/15 SP.
            var copy=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(Authored()));
            if(ready)copy.skill.initialSP=copy.skill.requiredSP;
            copy.skill.attackSpeedMultiplier=attackSpeed;
            return copy.Snapshot();
        }

        static EngagementSettings PlayerCombat(bool attacks=false)
            =>new EngagementSettings(unitAttack:attacks?2000:1,unitArmor:10000,unitRange:.75f,unitInterval:1000,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                hasSkill:false,canAttack:attacks,melee:true,effects:new MonsterEffectSettings(unitResistance:100));

        static ManualBattle Fixture(DefenderSettings defender,Vector3 entry,int count=1,float speed=.001f,
            bool attacks=false,DefenderSettings[] companions=null)
        {
            ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,100,20,1,.1f,10000,speed,1,1,1,1);
            var combat=PlayerCombat(attacks);
            var roster=new[]{defender}.Concat(companions??Array.Empty<DefenderSettings>()).ToArray();
            var b=new ManualBattle(Unit(Cards[0]),new[]{entry,entry+Vector3.forward*100},defender.Position,combat,roster:roster);
            if(count>1)b.ConfigureCardLoadout(Cards.Skip(1).Take(count-1).Select(id=>new BasicRosterProfile(Unit(id),combat,true)));
            foreach(var guard in b.Defenders)b.ConfigureDefenderHighGround(guard.EntityId,guard.Position.y>.5f);
            b.Begin();
            for(int i=0;i<count;i++)Assert.That(b.TryDeployRole(b.RunId,i+1,b.Entry,Cards[i]).Success,Is.True);
            return b;
        }

        static CombatShot[] Attacks(ManualBattle b)=>b.CombatShots.Where(s=>s.SourceId=="D23"&&s.FromDefender&&!s.Healing).ToArray();
        static void Health(DefenderState d,float hp)=>typeof(DefenderState).GetProperty("Health").SetValue(d,hp);

        [Test]public void ReadyMon3trDoesNotWasteMeltdownOnTheClosestPointOfAnUnreachableTreeFirstRoute()
        {
            var source=Mon3tr();var map=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            var route=map.LaneSnapshot().Single(l=>l.Id=="R1");
            var projection=new RouteMotion(route.Points,1);projection.Advance((float)projection.ProjectDistance(source.RelayPosition));
            Assert.That(source.Skill.EffectRadius,Is.EqualTo(.95f));
            Assert.That(Vector3.Distance(new Vector3(projection.Position.x,0,projection.Position.z),
                new Vector3(source.RelayPosition.x,0,source.RelayPosition.z)),Is.EqualTo(2).Within(.001));
            var b=Fixture(source,projection.Position);b.Advance(2);
            Assert.That(b.Defender.SkillSP,Is.EqualTo(source.Skill.RequiredSP));Assert.That(b.Defender.SkillCasts,Is.Zero);
            Assert.That(b.Defender.Transfers,Is.Zero);Assert.That(b.Defender.Position,Is.EqualTo(source.Position));
            Assert.That(b.DefenderCanAttack(b.Defender),Is.False);Assert.That(Attacks(b),Is.Empty);
            Assert.That(Authored().skill.initialSP,Is.EqualTo(7),"A test snapshot must not change the authored encounter.");
        }

        [Test]public void ApproachingARealRelayTargetAutomaticallyTransformsAndResolvesTheFirstTrueAttack()
        {
            var source=Mon3tr();var b=Fixture(source,source.RelayPosition+Vector3.back*3,speed:1);
            // The marked 3x3 footprint reaches 1.5 units; stop outside that edge.
            b.Advance(1.3f);Assert.That(b.Defender.SkillCasts,Is.Zero);Assert.That(b.DefenderCanAttack(b.Defender),Is.False);
            for(int i=0;i<50&&b.Defender.SkillCasts==0;i++)b.Advance(.05f);
            Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.Defender.Position,Is.EqualTo(source.RelayPosition));
            Assert.That(b.Defender.HighGround,Is.False);Assert.That(b.DefenderCanAttack(b.Defender),Is.True);
            b.Advance(1.5f);
            var shots=Attacks(b);Assert.That(shots.Length,Is.GreaterThan(0));Assert.That(shots.Any(s=>s.TrueDamage&&s.Applied),Is.True);
            Assert.That(b.FindUnit(b.Defender.TargetId),Is.Not.Null,"The skill form uses the same explicit enemy target as other attackers.");
            Assert.That(b.Units[0].Health,Is.EqualTo(10000-source.Attack*source.Skill.AttackMultiplier).Within(.01));
        }

        [Test]public void TheAuthoredMarkedFootprintStrikesAtMostThreeEnemiesAndKeepsAttackSelfHealing()
        {
            var source=Mon3tr();var b=Fixture(source,source.RelayPosition+Vector3.right*.5f,count:4);
            b.Advance(1.5f);var shots=Attacks(b);
            Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(shots.Length,Is.EqualTo(3));
            Assert.That(shots.All(s=>s.TrueDamage&&s.Applied),Is.True);Assert.That(shots.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(3));
            Assert.That(shots.All(s=>Math.Abs(s.Attack-source.Attack*3.8f)<.001),Is.True);
            Assert.That(b.Units.Count(u=>u.Health<10000),Is.EqualTo(3));
            Assert.That(b.Units.Where(u=>u.Health<10000).All(u=>Math.Abs(u.Health-(10000-source.Attack*3.8f))<.01),Is.True,
                "Actual true damage bypasses the fixture's 10000 armor and 100 resistance.");
            Assert.That(b.Defender.MaxHealth,Is.EqualTo(source.Health+500));
            Assert.That(b.Defender.SkillHealthRestored,Is.GreaterThan(0));
        }

        [TestCase(1,false)][TestCase(2,false)][TestCase(1,true)]
        public void ClawCadenceDamageEventAndPresentationShareTheActualAttackRate(float attackSpeed,bool cold)
        {
            var source=Mon3tr(attackSpeed:attackSpeed);
            var b=Fixture(source,source.RelayPosition+Vector3.right*.5f);
            if(cold)b.Defender.ColdUntil=100;
            var scheduled=new System.Collections.Generic.List<(double at,double next)>();
            double previous=double.NegativeInfinity;
            // ManualBattle.Advance uses a maximum .05-second step. Observe each
            // scheduled deadline separately from the next actual dispatch tick.
            for(int i=0;i<90;i++)
            {
                b.Advance(.05f);
                if(b.Defender.LastActionTime!=previous&&Attacks(b).Length>0)
                {previous=b.Defender.LastActionTime;scheduled.Add((previous,b.Defender.NextAttackTime));}
            }
            var attacks=Attacks(b).OrderBy(s=>s.DefenderActionStartedAt).ToArray();
            Assert.That(attacks.Length,Is.GreaterThanOrEqualTo(2));
            float rate=attackSpeed*(cold?.7f:1);
            foreach(var shot in attacks)
            {
                Assert.That(shot.DefenderActionRate,Is.EqualTo(rate).Within(.00001));
                Assert.That(shot.StartedAt-shot.DefenderActionStartedAt,Is.EqualTo((2.0/3)/rate).Within(.00001),
                    "The original claw OnAttack event is 20/30 of a source second.");
                Assert.That(shot.ImpactAt,Is.EqualTo(shot.StartedAt),"A source MeleeAttack has no healing-projectile flight delay.");
                Assert.That(shot.DefenderSkillWasActive,Is.True);
            }
            double period=1.35f/rate;
            Assert.That(scheduled.Count,Is.EqualTo(attacks.Length));
            foreach(var deadline in scheduled)
                Assert.That(deadline.next-deadline.at,Is.EqualTo(period).Within(.000001),"Scheduled source interval must remain exact before tick quantization.");
            for(int i=1;i<scheduled.Count;i++)
            {
                double delay=scheduled[i].at-scheduled[i-1].next;
                Assert.That(delay,Is.GreaterThanOrEqualTo(-.000001).And.LessThan(.050001),"Dispatch must occur on the first due .05-second tick.");
                double quantized=Math.Ceiling((period-.000001)/.05)*.05;
                Assert.That(scheduled[i].at-scheduled[i-1].at,Is.EqualTo(quantized).Within(.000001),
                    "Exactly aligned 1.35-second intervals must not drift to 1.40; 0.675 and cold 1.92857 quantize to 0.70 and 1.95.");
            }
            Assert.That(Authored().actionDuration,Is.EqualTo(2.833333f).Within(.00001),
                "The normal healing gesture and authored encounter remain unchanged.");
        }

        [Test]public void NormalFormKeepsThreeTargetChainHealingAndChargesOneSpPerActionWithoutTimeRecovery()
        {
            var source=Mon3tr(false);
            var allies=Enumerable.Range(1,3).Select(i=>new DefenderSettings("ally"+i,"ally"+i,null,source.Position+Vector3.right*(i*.2f),
                10000,1,0,.01f,1000,canAttack:false)).ToArray();
            var b=Fixture(source,source.RelayPosition+Vector3.back*10,companions:allies);
            for(int i=1;i<=3;i++)Health(b.FindDefender("ally"+i),i*1000);
            b.Advance(.7f);
            Assert.That(b.FindDefender("ally1").Health,Is.EqualTo(1000+source.Attack).Within(.001));
            Assert.That(b.FindDefender("ally2").Health,Is.EqualTo(2000+source.Attack*.75f).Within(.001));
            Assert.That(b.FindDefender("ally3").Health,Is.EqualTo(3000+source.Attack*.75f*.75f).Within(.001));
            Assert.That(b.Defender.SkillSP,Is.EqualTo(8));Assert.That(b.Defender.SkillCasts,Is.Zero);
            b.Advance(1);Assert.That(b.Defender.SkillSP,Is.EqualTo(8),"Elapsed time alone cannot recharge this healing-recovery skill.");
            b.Advance(23);Assert.That(b.Defender.SkillSP,Is.EqualTo(15));Assert.That(b.Defender.SkillCasts,Is.Zero);
            Assert.That(b.DefenderCanAttack(b.Defender),Is.False);Assert.That(Attacks(b),Is.Empty);
        }

        [Test]public void PausingFreezesTheSkillPositionAndItsPendingFirstImpact()
        {
            var source=Mon3tr();var b=Fixture(source,source.RelayPosition+Vector3.right*.5f);b.Advance(.8f);
            Assert.That(Attacks(b).Length,Is.EqualTo(1));Assert.That(Attacks(b)[0].Applied,Is.False);
            var position=b.Defender.Position;double clock=b.Clock,until=b.Defender.SkillActiveUntil;float hp=b.Units[0].Health;
            b.SetPaused(true);b.Advance(10);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.Defender.Position,Is.EqualTo(position));
            Assert.That(b.Defender.SkillActiveUntil,Is.EqualTo(until));Assert.That(b.Units[0].Health,Is.EqualTo(hp));
            var first=Attacks(b)[0];
            b.SetPaused(false);
            b.Advance((float)Math.Max(0,first.ImpactAt-b.Clock-.01));Assert.That(first.Applied,Is.False,"The real 20/30 event has not happened yet.");
            b.Advance(.06f);Assert.That(first.Applied,Is.True);Assert.That(b.Units[0].Health,Is.LessThan(hp));
        }

        [Test]public void ExpiryReturnsToTheOriginalHealingFormWithoutSchedulingFurtherSkillAttacks()
        {
            var source=Mon3tr();var b=Fixture(source,source.RelayPosition+Vector3.right*.5f);b.Advance(27);
            Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.Defender.Transfers,Is.EqualTo(2));
            Assert.That(b.Defender.Position,Is.EqualTo(source.Position));Assert.That(b.Defender.MaxHealth,Is.EqualTo(source.Health));
            Assert.That(b.Defender.HighGround,Is.True);Assert.That(b.DefenderCanAttack(b.Defender),Is.False);
            int launched=Attacks(b).Length;b.Advance(2);Assert.That(Attacks(b).Length,Is.EqualTo(launched));
            b.Restart();Assert.That(b.Defender.SkillSP,Is.EqualTo(source.Skill.InitialSP));Assert.That(b.Defender.Transfers,Is.Zero);
            Assert.That(b.DefenderCanAttack(b.Defender),Is.False);Assert.That(b.Units,Is.Empty);
        }

        [Test]public void ARealLethalIncomingHitStillReturnsMon3trAliveToItsOriginalPlatform()
        {
            var source=Mon3tr();var b=Fixture(source,source.RelayPosition+Vector3.right*.5f,attacks:true);b.Advance(.9f);
            Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.Defender.Transfers,Is.EqualTo(2));
            Assert.That(b.Defender.Position,Is.EqualTo(source.Position));Assert.That(b.Defender.Health,Is.EqualTo(1));
            Assert.That(b.Defender.Alive,Is.True);Assert.That(b.Defender.ProtectionUntil,Is.GreaterThan(b.Clock));
            Assert.That(b.DefenderCanAttack(b.Defender),Is.False);
        }
    }
}
