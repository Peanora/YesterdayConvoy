using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FacilityEffectTests
    {
        static ManualBattle Create(string role="M07_TEST",MonsterEffectSettings effects=default,Vector3? guard=null,float armor=0,float unitRange=.1f,Vector3[] extra=null)
        {
            var c=new EngagementSettings(unitAttack:1,unitArmor:armor,unitRange:unitRange,unitInterval:10,defenderHealth:2000,
                defenderAttack:1,defenderArmor:0,defenderRange:.1f,defenderInterval:10,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:effects);
            return new ManualBattle(new ManualBattleSettings("CARD_"+role,role,20,1,10,1,.1f,1000,.001f,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*100},guard??new Vector3(1,0,0),c,extra);
        }
        static DeployedUnitState Start(ManualBattle b){b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void AltarBypassesArmorAndArtsBarrierAndPauseRestartAreAuthoritative()
        {
            var b=Create(effects:new MonsterEffectSettings(artsBarrier:true,unitResistance:100),armor:9999);
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Altar,Vector3.zero,2,1,100)});var u=Start(b);
            float hp=u.Health,barrier=u.ArtsBarrierRemaining;b.Advance(1.01f);
            Assert.That(u.Health,Is.EqualTo(hp-100));Assert.That(u.ArtsBarrierRemaining,Is.EqualTo(barrier));Assert.That(b.AltarHits,Is.EqualTo(2));
            b.SetPaused(true);double clock=b.Clock;b.Advance(99);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.AltarHits,Is.EqualTo(2));
            b.Restart();Assert.That(b.AltarHits,Is.Zero);Assert.That(b.Facilities.Single().Activations,Is.Zero);Assert.That(b.Units,Is.Empty);
        }
        [Test]public void SarkazAltarInteractionIsPermanentButDoesNotLeakToOrdinarySoldiers()
        {
            foreach(string role in new[]{"M19_TEST","M20_TEST","M07_TEST"})
            {
                var b=Create(role);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Altar,Vector3.zero,2,1,100)});var u=Start(b);float hp=u.Health;b.Advance(1.01f);
                Assert.That(u.AltarEmpowered,Is.EqualTo(role.StartsWith("M19")));
                Assert.That(u.Health,Is.EqualTo(role.StartsWith("M07")?hp-100:hp));
            }
        }
        [Test]public void SealingKillsOccupantRegardlessOfHealthAndRemainsUntilRestart()
        {
            var tile=new Vector3(1,0,0);var b=Create("M14_TEST",new MonsterEffectSettings(frostNova:true),tile);
            b.ConfigureFacilities(Array.Empty<FacilitySettings>(),new[]{tile,new Vector3(0,0,1)});Start(b);b.Advance(30.1f);
            Assert.That(b.SealedTileCount,Is.EqualTo(2));Assert.That(b.IsTileSealed(tile),Is.True);Assert.That(b.Defender.Alive,Is.False);
            b.SetPaused(true);b.Advance(40);Assert.That(b.SealedTileCount,Is.EqualTo(2));b.Restart();Assert.That(b.SealedTileCount,Is.Zero);Assert.That(b.Defender.Alive,Is.True);
        }
        [Test]public void SealingIsReproducibleAndDoesNotExistWithoutConfiguredMapTiles()
        {
            var a=Create(effects:new MonsterEffectSettings(frostNova:true));Start(a);a.Advance(31);Assert.That(a.SealedTileCount,Is.Zero);
            var b=Create(effects:new MonsterEffectSettings(frostNova:true));var cells=new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
            b.ConfigureFacilities(Array.Empty<FacilitySettings>(),cells);Start(b);b.Advance(31);var first=b.SealedTiles.OrderBy(v=>v.x).ThenBy(v=>v.y).ToArray();
            b.Restart();Start(b);b.Advance(31);Assert.That(b.SealedTiles.OrderBy(v=>v.x).ThenBy(v=>v.y),Is.EqualTo(first));
        }
        [Test]public void GramophoneContestCancelsAndMudrockConvertsNearbyDeviceAfterTwentySeconds()
        {
            var b=Create("M28_TEST",new MonsterEffectSettings(mudrock:true));
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Gramophone,Vector3.zero,4,1,1,3)});Start(b);b.Advance(19);
            Assert.That(b.Facilities[0].Owner,Is.EqualTo(FacilityOwner.Neutral));Assert.That(b.Facilities[0].Capture,Is.Zero.Within(.001));
            b.Advance(1.1f);Assert.That(b.Facilities[0].Owner,Is.EqualTo(FacilityOwner.Attackers));Assert.That(b.GramophoneHits,Is.GreaterThan(0));
            b.Restart();Assert.That(b.Facilities[0].Owner,Is.EqualTo(FacilityOwner.Neutral));Assert.That(b.FacilityHealing,Is.Zero);
        }
        [Test]public void GramophoneCaptureAttacksOpponentAndHealsLivingFriendlyWithoutOverheal()
        {
            var b=Create(guard:new Vector3(3,0,0));
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Gramophone,Vector3.zero,4,1,1,1)});var u=Start(b);
            b.TryDamageUnit(u.EntityId,200,CombatDamageType.Physical);b.Advance(5.1f);
            Assert.That(b.Facilities[0].Owner,Is.EqualTo(FacilityOwner.Attackers));Assert.That(b.Defender.Health,Is.LessThan(2000));
            Assert.That(b.AttackerGramophoneHits,Is.GreaterThan(0));
            Assert.That(b.FacilityHealing,Is.EqualTo(100).Within(.01));Assert.That(u.Health,Is.EqualTo(900).Within(.01));
        }
        [Test]public void BurningReedsRemoveCamouflageAndBurstThenExtinguishAndResistIgnition()
        {
            var b=Create(guard:Vector3.zero);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.zero,.5f,1,6)});Start(b);
            Assert.That(b.DefenderCamouflaged(b.Defender),Is.True);Assert.That(b.TryIgniteReeds(Vector3.zero),Is.True);Assert.That(b.DefenderCamouflaged(b.Defender),Is.False);
            b.Advance(18);Assert.That(b.BurnBursts,Is.EqualTo(1));Assert.That(b.Defender.BurnUntil,Is.GreaterThan(b.Clock));
            b.Advance(2.1f);Assert.That(b.Facilities[0].Burning,Is.False);Assert.That(b.TryIgniteReeds(Vector3.zero),Is.False);
            b.Advance(5.1f);Assert.That(b.TryIgniteReeds(Vector3.zero),Is.True);b.Restart();Assert.That(b.Facilities[0].Burning,Is.False);Assert.That(b.BurnBursts,Is.Zero);
        }
        [Test]public void FireSpreadsOnlyToOrthogonalNeighborAfterFifteenSeconds()
        {
            var b=Create(guard:new Vector3(20,0,20));
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.zero),new FacilitySettings(FacilityKind.Reeds,Vector3.right),new FacilitySettings(FacilityKind.Reeds,new Vector3(1,0,1))});Start(b);
            b.TryIgniteReeds(Vector3.zero,.1f);b.Advance(14.9f);Assert.That(b.Ignitions,Is.EqualTo(1));b.Advance(.2f);
            Assert.That(b.Facilities[1].Burning,Is.True);Assert.That(b.Facilities[2].Burning,Is.False);
            b.SetPaused(true);Assert.That(b.TryIgniteReeds(new Vector3(1,0,1)),Is.False);
        }
        [Test]public void MapIgnitionUsesTheNormalEntryAndInvalidSettingsCannotReplaceConfiguration()
        {
            var b=Create("M30_TEST");b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.zero)});Start(b);b.Advance(.1f);Assert.That(b.Ignitions,Is.Zero);
            Assert.That(b.TryIgniteReeds(Vector3.zero),Is.True);Assert.That(b.Ignitions,Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(()=>b.ConfigureFacilities(Array.Empty<FacilitySettings>()));
            Assert.Throws<ArgumentException>(()=>new FacilitySettings(FacilityKind.Altar,Vector3.zero,interval:0));
            Assert.Throws<ArgumentException>(()=>new FacilitySettings(FacilityKind.Altar,new Vector3(float.NaN,0,0)));
        }
        [Test]public void ThrowerPrioritizesProtocolEntryGuardWhileOrdinarySoldierKeepsNearestTarget()
        {
            foreach(string role in new[]{"M26_TEST","M07_TEST"})
            {
                var b=Create(role,guard:new Vector3(.2f,0,0),unitRange:3,extra:new[]{new Vector3(2,0,0)});
                b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Gramophone,new Vector3(2.5f,0,0))});Start(b);b.Advance(.3f);
                var shot=b.CombatShots.First(s=>!s.FromDefender);
                Assert.That(shot.TargetId,Is.EqualTo(role.StartsWith("M26")?"kroos-1":"kroos"));
            }
        }
    }
}
