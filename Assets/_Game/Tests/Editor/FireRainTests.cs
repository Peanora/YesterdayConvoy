using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FireRainTests
    {
        private static ManualBattle New(float hp=210)
        {
            var settings=new ManualBattleSettings("TALULAH","TALULAH",12,1,1,2,2,1000,.75f,1,3,1,1);
            return new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*30},Vector3.right*5,
                new EngagementSettings(unitRange:1,defenderHealth:hp,defenderRange:.1f,initialSP:12,
                    skillAttack:48,skillInterval:6,projectileTime:.65f,blockCount:0,fireRain:true),
                new[]{Vector3.left*8,Vector3.forward*20});
        }
        private static DeployedUnitState Deploy(ManualBattle b)
        { b.Begin(); Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True); return b.Units[0]; }
        [Test]
        public void SixWavesDamageEveryDefenderOutsideAttackRangeAndResumeAfterFullAction()
        {
            var b=New(); var u=Deploy(b); int dp=b.DP;
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(b.SkillCasts,Is.EqualTo(1)); Assert.That(b.DP,Is.EqualTo(dp));
            b.Advance(1.2f); foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(165));
            b.Advance(4.7f); foreach(var d in b.Defenders) Assert.That(d.Alive,Is.False);
            Assert.That(u.Motion.DistanceTravelled,Is.Zero); Assert.That(b.IsSkillActionPlaying(u),Is.True);
            Assert.That(u.SkillSP,Is.Zero); b.Advance(.2f);
            Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(0)); Assert.That(u.SkillSP,Is.GreaterThan(0));
        }
        [Test]
        public void ExactlySixWavesSettleBeforeTheCastEndsAndDoNotRepeat()
        {
            var b=New(1000); var u=Deploy(b); b.TryActivateSkill(b.RunId,2,u.EntityId);
            b.Advance(6); Assert.That(b.CombatShots.Count,Is.EqualTo(18));
            foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(730));
            b.Advance(3); Assert.That(b.CombatShots.Count,Is.EqualTo(18));
            foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(730));
        }
        [Test]
        public void PauseFreezesWavesAndRestartRejectsOldRunRequests()
        {
            var b=New(1000); var u=Deploy(b); b.TryActivateSkill(b.RunId,2,u.EntityId); b.Advance(.4f);
            b.SetPaused(true); double clock=b.Clock; int shots=b.CombatShots.Count; int ammo=u.SkillAmmo;
            b.Advance(20); Assert.That(b.Clock,Is.EqualTo(clock)); Assert.That(b.CombatShots.Count,Is.EqualTo(shots));
            Assert.That(u.SkillAmmo,Is.EqualTo(ammo)); foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(1000));
            b.SetPaused(false); b.Advance(.8f); foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(955));
            int run=b.RunId; b.Restart(); Assert.That(b.CombatShots,Is.Empty); Assert.That(b.Units,Is.Empty);
            foreach(var d in b.Defenders) Assert.That(d.Health,Is.EqualTo(1000));
            Assert.That(b.TryActivateSkill(run,2,u.EntityId),Is.EqualTo(SkillFailure.StaleRun));
        }
    }
}
