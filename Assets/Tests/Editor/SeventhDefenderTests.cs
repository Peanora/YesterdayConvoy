using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class SeventhDefenderTests
    {
        static ManualBattle New(DefenderSkillKind kind,float attack=100)
        {
            var s=new DefenderSkillSettings(kind,1,1,1,99999,attackMultiplier:kind==DefenderSkillKind.ChenHolungday?1.7f:kind==DefenderSkillKind.AngelinaWishes?1.3f:kind==DefenderSkillKind.KaltsitEsperanta?2.25f:1,effectRadius:10);
            var d=new DefenderSettings("caster","caster","D32",Vector3.forward,10000,attack,0,10,1.6f,role:kind==DefenderSkillKind.KaltsitEsperanta?DefenderRole.Medic:DefenderRole.Fighter,windup:.4f,actionDuration:1.3f,skill:s,canAttack:kind!=DefenderSkillKind.KaltsitEsperanta);
            var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,12,1,.1f,100000,.01f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:10,unitArmor:0,unitInterval:.5f,unitRange:5),roster:new[]{d});
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);return b;
        }
        [Test]public void ChenSpendsTwoAmmoForTwoRealHitsAndStickyGroundDoesNotStack()
        {
            var b=New(DefenderSkillKind.ChenHolungday);b.Advance(3.5f);
            var shots=b.CombatShots.Where(x=>x.FromDefender&&x.Empowered).ToArray();
            Assert.That(shots.Length,Is.GreaterThanOrEqualTo(2));Assert.That(shots.Count(x=>x.Applied&&x.DamageDealt>0),Is.GreaterThanOrEqualTo(2));
            Assert.That(b.Defender.Ammo,Is.EqualTo(32-shots.Length));Assert.That(b.UnitArmor(b.Units[0]),Is.EqualTo(-20));
            Assert.That(b.UnitMoveMultiplier(b.Units[0]),Is.EqualTo(.6f).Within(.0001));
            b.SetPaused(true);double clock=b.Clock;int ammo=b.Defender.Ammo,events=b.SeventhDefenderEvents.Count;b.Advance(20);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.Defender.Ammo,Is.EqualTo(ammo));Assert.That(b.SeventhDefenderEvents.Count,Is.EqualTo(events));
        }
        [Test]public void AngelinaFlightPreventsNewGroundTargetingAndAmmoUsesRealAttacks()
        {
            var b=New(DefenderSkillKind.AngelinaWishes);b.Advance(2);float hp=b.Defender.Health;b.Advance(2);
            Assert.That(b.DefenderSkillActive(b.Defender),Is.True);Assert.That(b.Defender.Health,Is.EqualTo(hp));
            Assert.That(b.Defender.Ammo,Is.LessThan(31));Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.Empowered&&s.Applied),Is.True);
        }
        [Test]public void ClemensiaConsumesAmmoOnlyAtCorrosionBurstAndKeepsPermanentArmorLoss()
        {
            var b=New(DefenderSkillKind.ClemensiaCorrosion);b.Advance(1.5f);Assert.That(b.Defender.Ammo,Is.EqualTo(10));
            b.Advance(5);Assert.That(b.Defender.SeventhBursts,Is.EqualTo(1));Assert.That(b.Defender.Ammo,Is.EqualTo(9));
            Assert.That(b.UnitArmor(b.Units[0]),Is.EqualTo(-12));
            Assert.That(b.CombatShots.Any(s=>s.TrueDamage&&s.SeventhSecondary&&s.Applied),Is.True);
            Assert.That(b.SeventhDefenderEvents.Count(e=>e.Kind==SeventhDefenderEventKind.CorrosionBurst),Is.EqualTo(1));
        }
        [Test]public void EsperantaDetonatesAtImpactWithTrueDamageAndHinder()
        {
            var b=New(DefenderSkillKind.KaltsitEsperanta);b.Advance(1);
            Assert.That(b.SeventhDefenderEvents.Any(e=>e.Kind==SeventhDefenderEventKind.MedicalDetonation),Is.False);
            b.Advance(1);var hit=b.CombatShots.First(s=>s.FromDefender&&s.TrueDamage&&s.Applied);
            Assert.That(hit.DamageDealt,Is.GreaterThan(0));Assert.That(b.Defender.SkillHealthRestored,Is.GreaterThan(0));Assert.That(b.Units[0].HinderedUntil,Is.GreaterThan(b.Clock));
            Assert.That(b.UnitMoveMultiplier(b.Units[0]),Is.EqualTo(.2f).Within(.0001));
            Assert.That(hit.DefenderActionStartedAt,Is.LessThan(hit.ImpactAt));Assert.That(hit.DefenderActionRate,Is.EqualTo(1));
        }
        [TestCase(DefenderSkillKind.ChenHolungday)]
        [TestCase(DefenderSkillKind.AngelinaWishes)]
        [TestCase(DefenderSkillKind.ClemensiaCorrosion)]
        [TestCase(DefenderSkillKind.KaltsitEsperanta)]
        public void SeventhRestartClearsEventsAmmoCorrosionAndActionSnapshots(DefenderSkillKind kind)
        {
            var b=New(kind);b.Advance(6);int run=b.RunId;Assert.That(b.SeventhDefenderEvents,Is.Not.Empty);
            b.Restart();Assert.That(b.RunId,Is.Not.EqualTo(run));Assert.That(b.SeventhDefenderEvents,Is.Empty);Assert.That(b.CombatShots,Is.Empty);
            Assert.That(b.Defender.Ammo,Is.Zero);Assert.That(b.Defender.SeventhBursts,Is.Zero);Assert.That(b.Defender.SkillSP,Is.EqualTo(1));Assert.That(b.Defender.SkillCasts,Is.Zero);
        }
        [TestCase(true)][TestCase(false)]
        public void EsperantaReleasedMedicalUnitSurvivesSourceDeathButUnreleasedCastDoesNot(bool expectedHit)
        {
            var b=New(DefenderSkillKind.KaltsitEsperanta);
            for(int i=0;i<200&&!b.SeventhDefenderEvents.Any(e=>e.Kind==SeventhDefenderEventKind.HealingPulse);i++)b.Advance(.01f);
            var launch=b.SeventhDefenderEvents.First(e=>e.Kind==SeventhDefenderEventKind.HealingPulse);
            double deathAt=launch.At+(expectedHit?.05:-.05);
            Assert.That(deathAt,Is.GreaterThan(b.Clock).And.LessThan(launch.EndsAt));
            b.Advance((float)(deathAt-b.Clock));
            Assert.That(b.Clock>launch.At,Is.EqualTo(expectedHit),"The death must actually occur on the requested side of the real release event.");
            Assert.That(b.Clock,Is.LessThan(launch.EndsAt),"No impact may occur before this source-death boundary.");
            float health=b.Units[0].Health;b.Defender.Health=0;b.Defender.DeathTime=b.Clock;
            b.Advance((float)(launch.EndsAt-b.Clock+.6));
            Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.TrueDamage&&s.Applied),Is.EqualTo(expectedHit));
            Assert.That(b.Units[0].Health<health,Is.EqualTo(expectedHit));
        }
        [Test]public void ProjectileRetainsItsActionSnapshotAfterCasterRateChanges()
        {
            var b=New(DefenderSkillKind.ChenHolungday);b.Advance(1);var shot=b.CombatShots.First(s=>s.FromDefender);
            double at=shot.DefenderActionStartedAt;float rate=shot.DefenderActionRate;b.Defender.LastActionTime=999;b.Defender.LastActionRate=4;
            Assert.That(shot.DefenderActionStartedAt,Is.EqualTo(at));Assert.That(shot.DefenderActionRate,Is.EqualTo(rate));
        }
    }
}
