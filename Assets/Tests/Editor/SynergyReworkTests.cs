using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class SynergyReworkTests
    {
        static readonly string[] Crew={"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"};
        static readonly string[] Snow={"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"};
        static ShopTrialModel.Piece P(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        static ManualBattleSettings U(string id)=>new ManualBattleSettings(id,id,100,5,8,2,.1f,200,.5f,1,1,1,1);
        static EngagementSettings C(MonsterEffectSettings effects=default)=>new EngagementSettings(unitAttack:1,unitArmor:0,unitRange:3,unitInterval:2,
            defenderHealth:10000,defenderAttack:1,defenderArmor:10,defenderRange:.1f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,
            skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:effects);
        static ManualBattle B(string[] ids,EngagementSettings? combat=null)
        {
            var b=new ManualBattle(U(ids[0]),new[]{Vector3.zero,Vector3.forward*50},Vector3.right*.5f,combat??C());
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(U(id),C(),true)));return b;
        }
        static ManualBattle SnowBattle(int members=5,int targets=1,bool deployAttackers=true,int star=1,float resistance=0,float firstTargetHealth=10000,float targetDistance=.5f)
        {
            var ordinary=new EngagementSettings(unitAttack:1,unitRange:3,unitInterval:.25f,defenderHealth:10000,defenderAttack:1,defenderRange:.1f,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,projectileTime:.05f,effects:new MonsterEffectSettings(resistance:resistance));
            var frost=new EngagementSettings(unitAttack:1,unitRange:.1f,unitInterval:2,defenderHealth:10000,defenderAttack:1,defenderRange:.1f,blockCount:0,
                initialSP:1,requiredSP:1,spPerSecond:1,skillAmmo:0,skillAttack:1,skillInterval:3,skillRange:.1f,hasSkill:true,canAttack:false,frostSeal:true,effects:new MonsterEffectSettings(frostNova:true));
            var guards=Enumerable.Range(0,targets).Select(i=>new DefenderSettings("snow-guard-"+i,"guard","D01",Vector3.right*(targetDistance+.2f*i),i==0?firstTargetHealth:10000,1,10,.1f,10,canAttack:false)).ToArray();
            var b=new ManualBattle(U("CARD_M11"),new[]{Vector3.zero,Vector3.forward*50},Vector3.right*.5f,ordinary,null,guards);
            var frostUnit=new ManualBattleSettings("CARD_M14","CARD_M14",100,5,8,2,.1f,200,.01f,1,1,1,1);
            b.ConfigureCardLoadout(new[]{new BasicRosterProfile(U("CARD_M12"),ordinary,true),new BasicRosterProfile(frostUnit,frost,true)});
            b.ConfigureSynergies(Snow.Take(members).Select(id=>P(id,star)));b.Begin();
            if(deployAttackers)
            {
                Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M11").Success,Is.True);
                Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12").Success,Is.True);
            }
            return b;
        }
        // Freeze is an explicit control fixture; only real ordinary projectiles can complete a shatter.
        static void NextSnowShatter(ManualBattle b)
        {
            int before=b.Shatters;
            for(int i=0;i<400&&b.Shatters==before;i++)
            {
                b.TryApplyCold(b.Defender.EntityId,3.5f);b.TryApplyCold(b.Defender.EntityId,3.5f);b.Advance(.025f);
            }
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(b.Shatters,Is.EqualTo(before+1),"Two distinct normal cards must actually complete exactly one new shatter.");
        }
        static void StopSnowAttackers(ManualBattle b)
        {
            foreach(var unit in b.Units.Where(u=>u.CardId!="CARD_M14"&&!u.SynergySupport&&u.Active).ToArray())
                Assert.That(b.TryDamageUnit(unit.EntityId,10000,CombatDamageType.Arts),Is.True);
        }
        [Test]public void InfectionAddsArtsOnceWithoutReplacingOrDuplicatingIntrinsicPhysicalBlast()
        {
            var b=B(new[]{"CARD_M04","CARD_M02"},C(new MonsterEffectSettings(blastAttack:90,blastRadius:1.6f,blastDelay:1)));
            b.ConfigureSynergies(new[]{P("CARD_M04"),P("CARD_M02")},form:InfectionForm.Burst);b.Begin();
            var u=b.FindUnit(b.TryDeploy(b.RunId,1,b.Entry).EntityId);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);
            b.Advance(.3f);Assert.That(b.Defender.Health,Is.EqualTo(9965));Assert.That(b.DeathBlasts.Count,Is.EqualTo(1));
            b.Advance(.8f);Assert.That(b.Defender.Health,Is.EqualTo(9885));Assert.That(b.InfectionBursts.Count,Is.EqualTo(1));Assert.That(b.Defender.ColdUntil,Is.LessThan(0));
        }
        [TestCase(4,2,false,65.975395f)]
        [TestCase(5,5,true,120)]
        public void SnowFourKeepsLowerTierAndFivePreservesFreeze(int count,int tier,bool frozen,float damage)
        {
            var b=B(new[]{"CARD_M11","CARD_M12"});var ids=Snow.Take(count);b.ConfigureSynergies(ids.Select(id=>P(id)));Assert.That(b.SnowTier,Is.EqualTo(tier));b.Begin();
            b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M11");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12");
            b.TryApplyCold(b.Defender.EntityId,3.5f);b.TryApplyCold(b.Defender.EntityId,3.5f);b.Advance(.5f);
            Assert.That(b.Shatters,Is.EqualTo(1));Assert.That(b.ShatterDamage,Is.EqualTo(damage).Within(.002f));Assert.That(b.Defender.FrozenUntil>b.Clock,Is.EqualTo(frozen));
        }
        [Test]public void FiveSnowCountsCompletedShattersOncePerEventAndThirdStartsWeatherWithoutDeployingFrostNova()
        {
            var b=SnowBattle(targets:3);Assert.That(b.SnowTier,Is.EqualTo(5));
            Assert.That(ManualBattle.SnowstormChargeRequired,Is.EqualTo(3));Assert.That(b.SnowstormCharge,Is.Zero);
            Assert.That(b.Units.Any(u=>u.CardId=="CARD_M14"),Is.False);
            NextSnowShatter(b);Assert.That(b.ShatterEvents[0].Hits,Is.EqualTo(3));Assert.That(b.SnowstormCharge,Is.EqualTo(1));Assert.That(b.SnowstormActive,Is.False);
            b.SetPaused(true);double clock=b.Clock;b.Advance(30);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.SnowstormCharge,Is.EqualTo(1));Assert.That(b.Shatters,Is.EqualTo(1));b.SetPaused(false);
            NextSnowShatter(b);Assert.That(b.SnowstormCharge,Is.EqualTo(2));Assert.That(b.SnowstormActive,Is.False);
            NextSnowShatter(b);Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormActive,Is.True);
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(b.Clock));Assert.That(b.SnowstormRemaining,Is.EqualTo(8));Assert.That(b.SkillCasts,Is.Zero);
        }
        [Test]public void RestartDiscardsPartialColdChargeBeforeNewCombat()
        {
            var b=SnowBattle();NextSnowShatter(b);Assert.That(b.SnowstormCharge,Is.EqualTo(1));
            b.Restart();Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.Shatters,Is.Zero);
            Assert.That(b.SnowstormActive,Is.False);Assert.That(b.SnowstormStartedAt,Is.EqualTo(double.NegativeInfinity));
            b.Begin();Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M11").Success,Is.True);
            Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M12").Success,Is.True);
            NextSnowShatter(b);Assert.That(b.SnowstormCharge,Is.EqualTo(1));Assert.That(b.SnowstormActive,Is.False);
        }
        [TestCase(1,0,260)]
        [TestCase(2,40,205.843234f)]
        [TestCase(3,40,242.08791f)]
        public void ShatterWeatherDealsFourArtsPulsesWithStarScalingResistanceFeedbackAndPauseOrRestart(int star,float resistance,float pulseDamage)
        {
            var b=SnowBattle(star:star,resistance:resistance);Assert.That(ManualBattle.SnowstormDuration,Is.EqualTo(8));
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(double.NegativeInfinity));
            Assert.That(b.SnowstormEndsAt,Is.EqualTo(double.NegativeInfinity));Assert.That(b.SnowstormRemaining,Is.Zero);
            for(int i=0;i<3;i++)NextSnowShatter(b);
            double started=b.SnowstormStartedAt;Assert.That(b.SnowstormEndsAt,Is.EqualTo(started+8));
            StopSnowAttackers(b);float health=b.Defender.Health;long watermark=b.CombatFeedback.Last().Sequence;
            b.Advance(.1f);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(1));Assert.That(b.SnowstormRemaining,Is.EqualTo(7.9).Within(.0001));
            Assert.That(health-b.Defender.Health,Is.EqualTo(pulseDamage).Within(.001));
            b.SetPaused(true);double time=b.Clock,remaining=b.SnowstormRemaining;b.Advance(30);
            Assert.That(b.Clock,Is.EqualTo(time));Assert.That(b.SnowstormRemaining,Is.EqualTo(remaining));Assert.That(b.SnowstormPulses.Count,Is.EqualTo(1));
            Assert.That(health-b.Defender.Health,Is.EqualTo(pulseDamage).Within(.001));b.SetPaused(false);
            b.Advance(7.85f);Assert.That(b.SnowstormActive,Is.True);Assert.That(b.SnowstormRemaining,Is.EqualTo(.05).Within(.0001));
            Assert.That(b.SnowstormPulses.Select(p=>p.At),Is.EqualTo(new double[]{started,started+2,started+4,started+6}));
            b.Advance(.1f);Assert.That(b.SnowstormActive,Is.False);Assert.That(b.SnowstormRemaining,Is.Zero);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(4));
            Assert.That(health-b.Defender.Health,Is.EqualTo(4*pulseDamage).Within(.005));
            var losses=b.CombatFeedback.Where(e=>e.Sequence>watermark&&e.TargetId==b.Defender.EntityId&&e.Kind==CombatFeedbackKind.HealthLost).ToArray();
            Assert.That(losses.Length,Is.EqualTo(4));Assert.That(losses.All(e=>Math.Abs(e.Amount-pulseDamage)<.001),Is.True,"Jump text must reflect each applied post-resistance pulse.");
            b.Advance(2);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(4));Assert.That(health-b.Defender.Health,Is.EqualTo(4*pulseDamage).Within(.005));
            Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.Shatters,Is.EqualTo(3),"Derived weather damage cannot create direct-hit shatters.");
            b.Restart();Assert.That(b.SnowstormPulses,Is.Empty);Assert.That(b.SnowstormActive,Is.False);
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(double.NegativeInfinity));Assert.That(b.SnowstormEndsAt,Is.EqualTo(double.NegativeInfinity));Assert.That(b.SnowstormRemaining,Is.Zero);Assert.That(b.SnowstormCharge,Is.Zero);
        }
        [Test]public void ShattersDuringWeatherCannotChargeOrRefreshItAndNextWeatherNeedsThreeFreshEvents()
        {
            var b=SnowBattle();for(int i=0;i<3;i++)NextSnowShatter(b);
            double first=b.SnowstormStartedAt;
            NextSnowShatter(b);Assert.That(b.SnowstormActive,Is.True);Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormStartedAt,Is.EqualTo(first));
            NextSnowShatter(b);Assert.That(b.SnowstormActive,Is.True);Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormEndsAt,Is.EqualTo(first+8));
            StopSnowAttackers(b);b.Advance((float)(b.SnowstormEndsAt-b.Clock)+.05f);
            Assert.That(b.SnowstormActive,Is.False);Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(4));
            Assert.That(b.TryDeployRole(b.RunId,20,b.Entry,"CARD_M11").Success,Is.True);
            Assert.That(b.TryDeployRole(b.RunId,21,b.Entry,"CARD_M12").Success,Is.True);
            for(int i=1;i<=2;i++){NextSnowShatter(b);Assert.That(b.SnowstormCharge,Is.EqualTo(i));Assert.That(b.SnowstormActive,Is.False);}
            NextSnowShatter(b);double second=b.Clock;
            Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormStartedAt,Is.EqualTo(second));Assert.That(second,Is.GreaterThan(first+8));Assert.That(b.SnowstormRemaining,Is.EqualTo(8));
            b.Advance(.1f);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(5));Assert.That(b.SnowstormPulses[4].At,Is.EqualTo(second));
        }
        [Test]public void WeatherSurvivesTriggeringUnitsDeathAndInterruptedFrostSealRevivalOrFinalDeath()
        {
            var b=SnowBattle();for(int i=0;i<3;i++)NextSnowShatter(b);double started=b.SnowstormStartedAt;
            StopSnowAttackers(b);var receipt=b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M14");Assert.That(receipt.Success,Is.True);var u=b.FindUnit(receipt.EntityId);
            Assert.That(b.TryActivateSkill(b.RunId,31,u.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts),Is.True);Assert.That(u.IsReviving,Is.True);
            b.Advance(5.1f);Assert.That(b.FrostSeals[0].Cancelled,Is.True);Assert.That(u.IsReviving,Is.False);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(3));
            Assert.That(b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts),Is.True);Assert.That(u.Active,Is.False);Assert.That(b.SnowstormActive,Is.True);
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(started));Assert.That(b.SnowstormEndsAt,Is.EqualTo(started+8));
            b.Advance(3);Assert.That(b.SnowstormPulses.Select(p=>p.At),Is.EqualTo(new double[]{started,started+2,started+4,started+6}));Assert.That(b.SnowstormRemaining,Is.Zero);
        }
        [TestCase(2,false)]
        [TestCase(4,false)]
        [TestCase(5,true)]
        public void FrostSealStartsWeatherOnlyAtFiveSnowAndAutomaticRingsNeverStartIt(int members,bool expected)
        {
            var b=SnowBattle(members,deployAttackers:false);Assert.That(b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M14").Success,Is.True);var u=b.Units[0];
            b.Advance(10);
            Assert.That(b.IceRings.Count,Is.GreaterThan(0));Assert.That(b.Shatters,Is.Zero);Assert.That(b.SnowstormCharge,Is.Zero);
            Assert.That(b.SnowstormPulses,Is.Empty);Assert.That(b.SnowstormActive,Is.False);Assert.That(b.SnowstormRemaining,Is.Zero);
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(double.NegativeInfinity));Assert.That(b.SnowstormEndsAt,Is.EqualTo(double.NegativeInfinity));
            // Use a newly deployed real entity, whose ring/cast animation is not already active.
            var receipt=b.TryDeployRole(b.RunId,31,b.Entry,"CARD_M14");Assert.That(receipt.Success,Is.True);var fresh=b.FindUnit(receipt.EntityId);
            Assert.That(b.TryActivateSkill(b.RunId,32,fresh.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(b.SnowstormActive,Is.EqualTo(expected));Assert.That(b.SnowstormCharge,Is.Zero);
            b.Advance(.1f);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(expected?1:0));
        }
        [TestCase(1)]
        [TestCase(2)]
        public void FrostSealConsumesPartialChargeButAnotherCastCannotRefreshAnActiveStorm(int partial)
        {
            var b=SnowBattle();for(int i=0;i<partial;i++)NextSnowShatter(b);
            Assert.That(b.SnowstormCharge,Is.EqualTo(partial));StopSnowAttackers(b);
            var first=b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M14");Assert.That(first.Success,Is.True);
            Assert.That(b.TryActivateSkill(b.RunId,31,first.EntityId),Is.EqualTo(SkillFailure.None));
            double started=b.Clock;Assert.That(b.SnowstormStartedAt,Is.EqualTo(started));Assert.That(b.SnowstormCharge,Is.Zero);
            b.Advance(4.1f);int pulses=b.SnowstormPulses.Count;Assert.That(pulses,Is.EqualTo(3));
            var second=b.TryDeployRole(b.RunId,32,b.Entry,"CARD_M14");Assert.That(second.Success,Is.True);
            Assert.That(b.TryActivateSkill(b.RunId,33,second.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(b.SnowstormStartedAt,Is.EqualTo(started));Assert.That(b.SnowstormEndsAt,Is.EqualTo(started+8));
            Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(pulses),"A second cast cannot clear or replace the existing weather journal.");
        }
        [Test]public void FreezeAndItsProtectionBlockControlButNeverBlockWeatherDamage()
        {
            // Keep the durable target beyond both personal Ice Seal and automatic-ring range.
            var b=SnowBattle(deployAttackers:false,targetDistance:4);
            var receipt=b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M14");Assert.That(receipt.Success,Is.True);
            Assert.That(b.TryActivateSkill(b.RunId,31,receipt.EntityId),Is.EqualTo(SkillFailure.None));
            b.Advance(2.05f);Assert.That(b.SnowstormPulses.Count,Is.EqualTo(2));
            Assert.That(b.Defender.FrozenUntil,Is.GreaterThan(b.Clock),"The second pulse freezes a target chilled by the first.");
            float before=b.Defender.Health;b.Advance(2);
            Assert.That(b.SnowstormPulses.Count,Is.EqualTo(3));Assert.That(before-b.Defender.Health,Is.EqualTo(260).Within(.001));
            Assert.That(b.Defender.ColdUntil,Is.LessThanOrEqualTo(b.Clock),"The third pulse respects post-freeze storm protection.");
            Assert.That(b.SnowstormCharge,Is.Zero);
        }
        [Test]public void WeatherCanKillAndLaterPulsesSkipDeadTargetsWithoutRepeatedLossFeedback()
        {
            var b=SnowBattle(targets:2,deployAttackers:false,firstTargetHealth:100);
            var receipt=b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M14");Assert.That(receipt.Success,Is.True);
            Assert.That(b.TryActivateSkill(b.RunId,31,receipt.EntityId),Is.EqualTo(SkillFailure.None));b.Advance(6.1f);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(b.SnowstormPulses.Count,Is.EqualTo(4));
            Assert.That(b.Defender.Alive,Is.False);Assert.That(b.Defender.Health,Is.Zero);
            Assert.That(b.SnowstormPulses.Take(2).All(p=>p.Targets.Contains(b.Defender.EntityId)),Is.True);
            Assert.That(b.SnowstormPulses.Skip(2).Any(p=>p.Targets.Contains(b.Defender.EntityId)),Is.False);
            var losses=b.CombatFeedback.Where(e=>e.TargetId==b.Defender.EntityId&&e.Kind==CombatFeedbackKind.HealthLost).ToArray();
            Assert.That(losses.Length,Is.EqualTo(2));Assert.That(losses.Sum(e=>e.Amount),Is.EqualTo(100).Within(.001));
        }
        [Test]public void TwoSnowMembersCannotChargeWeatherEvenAfterThreeActualShatters()
        {
            var b=SnowBattle(2);Assert.That(b.SnowTier,Is.EqualTo(2));
            for(int i=0;i<3;i++)NextSnowShatter(b);
            Assert.That(b.Shatters,Is.EqualTo(3));Assert.That(b.SnowstormCharge,Is.Zero);Assert.That(b.SnowstormPulses,Is.Empty);Assert.That(b.SnowstormActive,Is.False);
        }
        [TestCase(4,6000,1600,480,1280)]
        [TestCase(5,7500,2000,600,1600)]
        public void UltimateCleaveHealsOnceAndThirdHitGlobalPulseIsOncePerAttack(int stage,float health,float attack,float healing,float pulse)
        {
            var guards=Enumerable.Range(0,4).Select(i=>new DefenderSettings("g"+i,"guard","D01",new Vector3(.5f+i*.2f,0,0),50000,1,0,.1f,10,canAttack:false)).ToArray();
            var b=new ManualBattle(U("CARD_M26"),new[]{Vector3.zero,Vector3.forward*100},Vector3.right,C(),null,guards);
            b.ConfigureSynergies(Crew.Select(id=>P(id,2)).Concat(stage==5?new[]{P("CARD_M07",2)}:Array.Empty<ShopTrialModel.Piece>()),
                golemExperience:stage==5?48:30,extraMudrockMembers:stage==5?new[]{"CARD_M07"}:null);b.Begin();var u=b.FindUnit(b.TryDeployGolem(b.RunId,1,b.Entry).EntityId);
            Assert.That(b.GolemStage,Is.EqualTo(stage));Assert.That(u.MaxHealth,Is.EqualTo(health));Assert.That(b.UnitCombat(u).UnitAttack,Is.EqualTo(attack));
            Assert.That(b.UnitControlImmune(u));Assert.That(b.DP,Is.EqualTo(86));b.TryDamageUnit(u.EntityId,1100,CombatDamageType.Physical);float damaged=u.Health;
            b.Advance(3);Assert.That(b.Defenders.Count(d=>d.Health<50000),Is.EqualTo(3));Assert.That(u.Health,Is.EqualTo(damaged+healing));
            b.Advance(10.5f);Assert.That(b.GolemPulses.Count,Is.EqualTo(1));Assert.That(b.Defenders[3].Health,Is.EqualTo(50000-pulse));
            Assert.That(b.UnitCombat(u).AttackDuration,Is.EqualTo(5.166667f));Assert.That(b.UnitResistance(u),Is.EqualTo(60));
        }
        [TestCase(4)][TestCase(5)]public void UltimateAloneClearsTheCompleteSixStarDefenseWithRealSkillsAndMapRoutes(int stage)
        {
            var map=Resources.Load<DefenderTrialDefinition>("Maps/BattleShop");Assert.That(map.defenders.Length,Is.EqualTo(6));
            var b=new ManualBattle(U("CARD_M26"),map.route,Vector3.right,C(),null,map.Snapshot(),map.LaneSnapshot());
            b.ConfigureStrongholdMap(false);b.ConfigureSynergies(Crew.Select(id=>P(id,2)).Concat(stage==5?new[]{P("CARD_M07",2)}:Array.Empty<ShopTrialModel.Piece>()),
                golemExperience:stage==5?48:30,extraMudrockMembers:stage==5?new[]{"CARD_M07"}:null);
            foreach(var d in b.Defenders)b.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
            b.Begin();var u=b.FindUnit(b.TryDeployGolem(b.RunId,1,b.Entry).EntityId);Assert.That(u,Is.Not.Null);
            for(int i=0;i<12000&&u.Active&&b.Defenders.Any(d=>d.Alive);i++)b.Advance(.05f);
            Debug.Log("ULTIMATE_GOLEM_BENCHMARK: clock="+b.Clock+"; hp="+u.Health+"; alive="+string.Join(",",b.Defenders.Where(d=>d.Alive).Select(d=>d.Settings.Name+":"+d.Health))+"; skills="+b.Defenders.Sum(d=>d.SkillCasts));
            Assert.That(b.Defenders.All(d=>!d.Alive),Is.True);Assert.That(u.Health,Is.GreaterThan(0));Assert.That(b.Defenders.Sum(d=>d.SkillCasts),Is.GreaterThan(0));
        }
    }
}
