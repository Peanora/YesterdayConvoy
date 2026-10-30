using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class FifthDefenderTests
    {
        static ManualBattle New(DefenderSkillKind kind,float damage=10,float hp=500)
        {
            var skill=new DefenderSkillSettings(kind,1,1,1,kind==DefenderSkillKind.HoshigumaInferno?32:25,attackMultiplier:2,effectRadius:5);
            var d=new DefenderSettings("d","d","D23",Vector3.forward,hp,20,0,5,1,3,skill:skill,relayPosition:Vector3.right);
            var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,10,1,.1f,100000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:damage,unitInterval:.5f,unitRange:5),roster:new[]{d});b.Begin();b.TryDeploy(b.RunId,1,b.Entry);return b;
        }
        [Test]public void ExusiaiConsumesExactlyFiveAmmoPerFiveSeparatelyResolvedHits()
        {var b=New(DefenderSkillKind.ExusiaiDelivery);b.Advance(1.3f);var shots=b.CombatShots.Where(x=>x.FromDefender&&x.Empowered).ToArray();Assert.That(shots.Length,Is.EqualTo(5));Assert.That(shots.All(x=>x.Applied),Is.True);Assert.That(b.Defender.Ammo,Is.EqualTo(45));}
        [Test]public void Mon3trLethalTransformedHitReturnsAliveAndPauseFreezesPosition()
        {var b=New(DefenderSkillKind.Mon3trMeltdown,damage:600,hp:500);b.Advance(.9f);Assert.That(b.Defender.Transfers,Is.GreaterThanOrEqualTo(2));Assert.That(b.Defender.Health,Is.GreaterThan(0));Assert.That(b.Defender.Position,Is.EqualTo(b.Defender.Settings.Position));b.SetPaused(true);var pos=b.Defender.Position;b.Advance(10);Assert.That(b.Defender.Position,Is.EqualTo(pos));}
        [Test]public void Mon3trSkillShotsBypassArmorAndResetClearsTransform()
        {var b=New(DefenderSkillKind.Mon3trMeltdown);b.Advance(2);Assert.That(b.CombatShots.Any(x=>x.TrueDamage&&x.Applied),Is.True);b.Restart();Assert.That(b.Defender.Transfers,Is.Zero);Assert.That(b.Defender.Position,Is.EqualTo(b.Defender.Settings.Position));}
        [Test]public void HoshigumaNegativeLifeIsFiniteAndCanBeDefeated()
        {var b=New(DefenderSkillKind.HoshigumaInferno,damage:300,hp:100);b.Advance(.5f);Assert.That(b.Defender.Alive,Is.True);Assert.That(b.Defender.DebtLife,Is.True);b.Advance(1);Assert.That(b.Defender.Alive,Is.False);}
        [Test]public void HoshigumaUsesTwoTargetsAndTwoArtsHitsThenEntersTimedLastStand()
        {var b=New(DefenderSkillKind.HoshigumaInferno,damage:1,hp:10000);b.Advance(2);Assert.That(b.CombatShots.Count(x=>x.FromDefender&&x.Empowered&&x.DamageType==CombatDamageType.Arts),Is.GreaterThanOrEqualTo(2));b.Advance(15);Assert.That(b.Defender.ProtectionUntil,Is.GreaterThan(b.Clock));b.Advance(12);Assert.That(b.Defender.Departed,Is.True);}
        [Test]public void FifthProfilesAndNativeActionsExist()
        {foreach(var id in ManualBattleController.DefenderBatch5Ids){var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Assert.That(data,Is.Not.Null);Assert.That(data.Snapshot()[0].Skill.Enabled,Is.True);Assert.That(DefenderArt.Load(id)["skill_attack"].Frames.Length,Is.GreaterThan(1));}}
        [Test]public void DeliveryActivatesOnlyAnUnusedReserveAndAddsSixSP()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.ExusiaiDelivery,1,1,1,100,effectRadius:5);
            var spare=new DefenderSkillSettings(DefenderSkillKind.BeagleGuard,0,100,1,10);
            var roster=new[]{new DefenderSettings("ex","ex","D24",Vector3.forward,10000,20,0,5,1,skill:s,relayPosition:Vector3.right),
                new DefenderSettings("reserve","reserve","D01",Vector3.back,100,10,0,1,1,skill:spare,reserve:true)};
            var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,10,1,.1f,100000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,new EngagementSettings(unitAttack:1),roster:roster);
            Assert.That(b.FindDefender("reserve").Alive,Is.False);b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.1f);
            Assert.That(b.FindDefender("reserve").Alive,Is.True);Assert.That(b.FindDefender("reserve").SkillSP,Is.GreaterThanOrEqualTo(6));Assert.That(b.Defender.Reinforcements,Is.EqualTo(1));
            b.Restart();Assert.That(b.FindDefender("reserve").Reserve,Is.True);
        }
        [Test]public void LastStandInterceptsActualLethalDamageAndThenLeaves()
        {
            var s=new DefenderSkillSettings(DefenderSkillKind.HoshigumaInferno,1,1,1,32,effectRadius:5);
            var roster=new[]{new DefenderSettings("tank","tank","D01",Vector3.forward*.1f,140,1,0,5,100),new DefenderSettings("hoshi","hoshi","D25",Vector3.right*2,10000,20,0,5,1,skill:s)};
            var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,10,1,.1f,100000,.1f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,new EngagementSettings(unitAttack:3,unitInterval:.5f,unitRange:5),roster:roster);
            b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(24);Assert.That(b.FindDefender("hoshi").ProtectedHits,Is.GreaterThan(1));Assert.That(b.FindDefender("hoshi").Health,Is.LessThan(b.FindDefender("hoshi").MaxHealth));Assert.That(b.FindDefender("tank").Alive,Is.True);Assert.That(b.FindDefender("tank").Health,Is.EqualTo(1));
            b.Advance(4);Assert.That(b.FindDefender("hoshi").Departed,Is.True);
        }
    }
}
