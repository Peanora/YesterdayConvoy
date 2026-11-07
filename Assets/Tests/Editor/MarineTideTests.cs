using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MarineTideTests
    {
        static readonly string[] Sea={"CARD_M33","CARD_M34","CARD_M35","CARD_M36"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,10,10,1,.1f,1000,.001f,1,1,1,1);
        static EngagementSettings Combat(MonsterEffectSettings? effects=null,bool attacks=false,float attack=30,float interval=1000)=>new EngagementSettings(
            unitAttack:attack,unitArmor:0,unitRange:2.5f,unitInterval:interval,defenderHealth:10000,defenderAttack:1,
            defenderArmor:2,defenderRange:.001f,defenderInterval:1000,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,
            skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,projectileTime:.05f,hasSkill:false,canAttack:attacks,effects:effects??new MonsterEffectSettings());
        static IEnumerable<Vector3> Square(int radius=4)
        {for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)yield return new Vector3(x,.04f,z);}
        static ManualBattle Make(int kinds=4,IEnumerable<Vector3> terrain=null,EngagementSettings? combat=null,Vector3? guard=null,string first=null)
        {
            var ids=(first==null?Sea:new[]{first}.Concat(Sea.Where(id=>id!=first))).Take(kinds).ToList();
            var c=combat??Combat();var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.right*100},guard??Vector3.right*3,c);
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),c,true)));
            b.ConfigureSynergies(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}));
            b.ConfigureMarineTerrain(terrain??Square());return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,long request)
        {if(b.Phase==BattlePhase.Ready)b.Begin();var result=b.TryDeployRole(b.RunId,request,b.Entry,id);Assert.That(result.Success,Is.True);return b.FindUnit(result.EntityId);}
        static void Hurt(ManualBattle b,DeployedUnitState u,float damage=1)=>Assert.That(b.TryDamageUnit(u.EntityId,damage,CombatDamageType.Arts),Is.True);
        static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);
        static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);

        [Test]public void DeploymentWaitingSelfLossAndInvulnerableContactDoNotSeed()
        {
            var b=Make();var u=Deploy(b,Sea[0],1);b.Advance(.5f);Assert.That(b.MarineCells,Is.Empty);
            Call(b,"ApplyUnitHealthDamage",u,10f);Assert.That(b.MarineCells,Is.Empty);
            Set(u,"IsReviving",true);Assert.That(b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts),Is.False);Assert.That(b.MarineCells,Is.Empty);
            Set(u,"IsReviving",false);Set(u,"SynergyShield",100f);Set(u,"SynergyShieldUntil",100d);
            Hurt(b,u);Assert.That(b.MarineCells,Is.Empty,"A completely absorbed hit did not damage the marine.");
            Set(u,"SynergyShield",0f);Hurt(b,u);Assert.That(b.MarineCells.Count,Is.EqualTo(1));Assert.That(b.MarineSeededKinds,Is.EqualTo(1));
        }

        [Test]public void ContactSeedsOncePerKindAndSameCellRootsMerge()
        {
            var b=Make();var a=Deploy(b,Sea[0],1);Hurt(b,a);Hurt(b,a);var other=Deploy(b,Sea[1],2);Hurt(b,other);
            Assert.That(b.MarineSeededKinds,Is.EqualTo(2));Assert.That(b.MarineCells.Count,Is.EqualTo(1));Assert.That(b.MarineCells.Single().IsSeed,Is.True);
            b.Advance(.2f);var duplicate=Deploy(b,Sea[0],3);duplicate.Motion.Advance(2000);Hurt(b,duplicate);
            Assert.That(b.MarineSeededKinds,Is.EqualTo(2));Assert.That(b.MarineCells.Count,Is.EqualTo(1),"A second entity of one species cannot create a new root.");
        }

        [Test]public void ExternalPureDamageSeedsWithoutAnOrdinaryHitButRevivingContactDoesNot()
        {
            var b=Make(2,first:Sea[2]);var u=Deploy(b,Sea[2],1);
            Set(u,"IsReviving",true);Call(b,"ApplyPureUnitDamage",u,10f);Assert.That(u.Health,Is.EqualTo(1000));Assert.That(b.MarineCells,Is.Empty);
            Set(u,"IsReviving",false);Call(b,"ApplyPureUnitDamage",u,10f);Assert.That(u.Health,Is.EqualTo(990));
            Assert.That(b.MarineSeededKinds,Is.EqualTo(1));Assert.That(b.MarineCells.Single().Cell,Is.EqualTo(Vector2Int.zero));
            Assert.That(b.CombatShots,Is.Empty);Assert.That(b.CreeperPulses,Is.Empty);
            var fatal=Make(2,first:Sea[2]);var front=Deploy(fatal,Sea[2],1);
            Call(fatal,"ApplyPureUnitDamage",front,2000f);
            Assert.That(front.Active,Is.False);Assert.That(fatal.MarineCells.Count,Is.EqualTo(1),"Fatal true damage must record the same last contact as physical/arts hits.");
        }

        [TestCase(2,6,8)] [TestCase(4,4,16)]
        public void ThresholdSnapshotDeterminesSpeedAndCap(int kinds,float interval,int cap)
        {
            var b=Make(kinds);Assert.That(b.MarineTier,Is.EqualTo(kinds));Assert.That(b.MarineSpreadInterval,Is.EqualTo(interval));Assert.That(b.MarineTileLimit,Is.EqualTo(cap));
            var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(interval-.1f);Assert.That(b.MarineCells.Count,Is.EqualTo(1));
            b.Advance(.11f);Assert.That(b.MarineCells.Count,Is.EqualTo(5),"One tick adds the four neighbours, not recursive flood fill.");
            Assert.That(b.MarineSpreadCount,Is.EqualTo(4));Assert.That(b.MarineCells.Count(c=>c.IsSeed),Is.EqualTo(1));
        }

        [Test]public void TerrainIsExplicitAndCannotSpreadAcrossMissingCellsOrHighGround()
        {
            var terrain=new[]{Vector3.zero,Vector3.right,Vector3.right*3};var b=Make(terrain:terrain);var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(20);
            CollectionAssert.AreEquivalent(new[]{new Vector2Int(0,0),new Vector2Int(1,0)},b.MarineCells.Select(c=>c.Cell));
            Assert.Throws<InvalidOperationException>(()=>b.ConfigureMarineTerrain(Square()));
            var invalid=Make();Assert.Throws<ArgumentException>(()=>invalid.ConfigureMarineTerrain(new[]{new Vector3(float.NaN,0,0)}));
            var unknown=Make(terrain:Array.Empty<Vector3>());var v=Deploy(unknown,Sea[0],1);Hurt(unknown,v);Assert.That(unknown.MarineCells,Is.Empty);
        }

        [Test]public void CapPrioritizesTheBattlefrontAndReservesRoomForUnseededKinds()
        {
            var b=Make(2,guard:Vector3.right*3);var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(12.1f);
            Assert.That(b.MarineCells.Count,Is.EqualTo(7));Assert.That(b.MarineCells.Any(c=>c.Cell==new Vector2Int(2,0)),Is.True);
            Assert.That(b.MarineCells.Any(c=>c.Cell==new Vector2Int(-2,0)),Is.False);
            var last=Deploy(b,Sea[1],2);last.Motion.Advance(3000);Hurt(b,last);
            Assert.That(b.MarineCells.Count,Is.EqualTo(8));Assert.That(b.MarineCells.Single(c=>c.Cell==new Vector2Int(3,0)).IsSeed,Is.True);
            b.Advance(20);Assert.That(b.MarineCells.Count,Is.EqualTo(8));Assert.That(b.MarineSeededKinds,Is.EqualTo(2));
        }

        [Test]public void PauseAndMarineAbsenceFreezeSpreadWhileRedeploymentRestoresIt()
        {
            var b=Make();var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(1);double remaining=b.MarineSpreadRemaining;
            b.SetPaused(true);b.Advance(30);Assert.That(b.MarineSpreadRemaining,Is.EqualTo(remaining));Assert.That(b.MarineCells.Count,Is.EqualTo(1));
            var paused=b.TryDeployRole(b.RunId,2,b.Entry,Sea[1],true);Assert.That(paused.Success,Is.True);Assert.That(b.MarineSeededKinds,Is.EqualTo(1));
            b.SetPaused(false);Hurt(b,u,10000);Hurt(b,b.FindUnit(paused.EntityId),10000);Assert.That(b.MarineActive,Is.False);
            b.Advance(30);Assert.That(b.MarineSpreadRemaining,Is.EqualTo(remaining));Assert.That(b.MarineCells.Count,Is.EqualTo(1));
            var next=Deploy(b,Sea[0],3);Assert.That(b.MarineActive,Is.True);b.Advance((float)remaining+.05f);Assert.That(b.MarineCells.Count,Is.EqualTo(5));
            Assert.That(b.MarineNeuralMultiplier(next),Is.EqualTo(2));
        }

        [Test]public void GroundUsesRealCellOccupancyAndNeverDoublesWithMarineTier()
        {
            foreach(int tier in new[]{2,4})
            {
                var b=Make(tier,terrain:new[]{Vector3.zero},guard:new Vector3(.35f,0,.3f));var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(2);
                Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(6).Within(.001));Assert.That(b.MarineGroundNeuralDealt,Is.EqualTo(6).Within(.001));
                Assert.That(b.MarineBonusNeuralDealt,Is.Zero);Assert.That(b.Defender.Health,Is.EqualTo(10000));
            }
            var high=Make(terrain:new[]{Vector3.zero},guard:new Vector3(.35f,.76f,.3f));high.ConfigureDefenderHighGround(high.Defender.EntityId,true);
            var sea=Deploy(high,Sea[0],1);Hurt(high,sea);high.Advance(3);Assert.That(high.Defender.NeuralAccumulated,Is.Zero);Assert.That(high.MarineGroundNeuralDealt,Is.Zero);
        }

        [Test]public void FixedGroundPressureBurstsOnceAndRejectsAccumulationDuringExistingTenSecondLock()
        {
            var b=Make(terrain:new[]{Vector3.zero},guard:new Vector3(.25f,0,.25f));var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(34);
            Assert.That(b.Defender.NeuralBursts,Is.EqualTo(1));Assert.That(b.Defender.Health,Is.EqualTo(9900));
            Assert.That(b.MarineGroundNeuralDealt,Is.EqualTo(100).Within(.001));Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
            b.Advance(5);Assert.That(b.MarineGroundNeuralDealt,Is.EqualTo(100).Within(.001));Assert.That(b.Defender.NeuralBursts,Is.EqualTo(1));
        }

        [TestCase(2,45)] [TestCase(4,60)]
        public void CreeperFirstRealHitSeedsBeforeItsNativeRetaliation(int kinds,float expected)
        {
            var b=Make(kinds,combat:Combat(new MonsterEffectSettings(nervousCreeper:true)),guard:Vector3.right);var u=Deploy(b,Sea[0],1);Hurt(b,u,151);
            Assert.That(b.CreeperPulses.Count,Is.EqualTo(1));Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(expected));
            Assert.That(b.MarineBonusNeuralDealt,Is.EqualTo(expected-30));Assert.That(b.MarineGroundNeuralDealt,Is.Zero);
        }

        [Test]public void HarvesterAuraCanBeTheFirstEffectiveOutputAndReceivesOnlyNativeMultiplier()
        {
            var b=Make(2,terrain:new[]{Vector3.zero},combat:Combat(new MonsterEffectSettings(harvester:true),attack:40),guard:Vector3.right*2,first:Sea[1]);
            var u=Deploy(b,Sea[1],1);Hurt(b,u);b.Advance(2.1f);Assert.That(u.HarvesterAuraTicks,Is.EqualTo(1));
            Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(12).Within(.001));Assert.That(b.MarineBonusNeuralDealt,Is.EqualTo(4).Within(.001));
        }

        [Test]public void FirstSpeakerProjectileAndBubbleBothUseTheirActualSource()
        {
            var b=Make(2,terrain:new[]{Vector3.zero},combat:Combat(new MonsterEffectSettings(firstSpeaker:true),true),guard:Vector3.right,first:Sea[3]);
            Deploy(b,Sea[3],1);b.Advance(.2f);Assert.That(b.MarineSeededKinds,Is.EqualTo(1));Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(18).Within(.001));
            b.Advance(13);Assert.That(b.CorrosiveBubbles.Single().Pulses,Is.EqualTo(1));
            Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(45).Within(.001));Assert.That(b.MarineBonusNeuralDealt,Is.EqualTo(15).Within(.001));
        }

        [Test]public void BonusAccountingClipsAtThresholdAndDoesNotCountRejectedNativeHits()
        {
            var b=Make(combat:Combat(new MonsterEffectSettings(nervousCreeper:true),attack:80),guard:Vector3.right);var u=Deploy(b,Sea[0],1);Hurt(b,u,151);
            Assert.That(b.NeuralBursts,Is.EqualTo(1));Assert.That(b.MarineBonusNeuralDealt,Is.EqualTo(20));
            Hurt(b,u,151);Assert.That(b.NeuralBursts,Is.EqualTo(1));Assert.That(b.MarineBonusNeuralDealt,Is.EqualTo(20));
        }

        [Test]public void SupportDoesNotSeedOrReceiveBonusAndLeavingTheCellRemovesBonus()
        {
            var b=Make();var u=Deploy(b,Sea[0],1);Set(u,"SynergySupport",true);Hurt(b,u);Assert.That(b.MarineCells,Is.Empty);Assert.That(b.MarineNeuralMultiplier(u),Is.EqualTo(1));
            Set(u,"SynergySupport",false);Hurt(b,u);Assert.That(b.MarineNeuralMultiplier(u),Is.EqualTo(2));u.Motion.Advance(2000);
            Assert.That(b.MarineNeuralMultiplier(u),Is.EqualTo(1));Assert.That(b.MarineCells.Count,Is.EqualTo(1));
        }

        [Test]public void RestartClearsAllBattleStateButRetainsTerrainAndTier()
        {
            var b=Make(terrain:new[]{Vector3.zero,Vector3.right});var u=Deploy(b,Sea[0],1);Hurt(b,u);b.Advance(5);Assert.That(b.MarineCells.Count,Is.EqualTo(2));
            b.Restart();Assert.That(b.MarineCells,Is.Empty);Assert.That(b.MarineSeededKinds,Is.Zero);Assert.That(b.MarineSpreadCount,Is.Zero);
            Assert.That(b.MarineGroundNeuralDealt,Is.Zero);Assert.That(b.MarineBonusNeuralDealt,Is.Zero);Assert.That(b.MarineTier,Is.EqualTo(4));
            var next=Deploy(b,Sea[0],2);Hurt(b,next);b.Advance(5);Assert.That(b.MarineCells.Count,Is.EqualTo(2));
        }
    }
}
