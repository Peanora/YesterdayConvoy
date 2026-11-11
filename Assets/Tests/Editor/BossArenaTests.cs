using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class BossArenaTests
    {
        static BasicRosterProfile Profile(string id,float hp=200,float attack=10)
        {
            var u=new ManualBattleSettings(id,id,0,1,1,5,45,hp,.75f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:attack,unitArmor:0,unitRange:5,unitInterval:1,defenderRange:6,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            return new BasicRosterProfile(u,c,true);
        }
        static ManualBattle Make(float hp=200,float bossHp=100000,float bossAttack=1,float cooldown=90)
        {
            var a=Profile("CARD_M07",hp);var b=Profile("CARD_M08",hp);
            var boss=new DefenderSettings(BossArenaRules.BossId,"维什戴尔",null,Vector3.right,bossHp,bossAttack,0,6,4,
                blocks:20,windup:.1f,actionDuration:.5f,splashRadius:1);
            var battle=new ManualBattle(a.Unit,new[]{Vector3.zero,Vector3.right*100},Vector3.right,a.Combat,roster:new[]{boss});
            battle.ConfigureCardLoadout(new[]{b});battle.ConfigureBossArena(new BossArenaRules(cooldown));return battle;
        }
        [Test]public void BeginDeploysWholeSquadOnceWithNoDPOrUsesConsumed()
        {
            var b=Make();b.Begin();Assert.That(b.ActiveCount,Is.EqualTo(2));Assert.That(b.DP,Is.Zero);
            Assert.That(b.Cards.All(c=>c.RemainingUses==1),Is.True);b.Begin();Assert.That(b.Units.Count,Is.EqualTo(2));
            Assert.That(b.DeployReadyArenaSquad(),Is.Zero);
            Assert.That(b.CheckCardDeployment("CARD_M07",b.Entry),Is.EqualTo(DeployFailure.EntryBusy));
        }
        [Test]public void NaturalDeathStartsOneCooldownAndAllDeadDoesNotLose()
        {
            var b=Make(hp:10,bossAttack:1000);b.Begin();b.Advance(.5f);
            Assert.That(b.ActiveCount,Is.Zero);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            double cd=b.ArenaRedeploymentRemaining("CARD_M07");Assert.That(cd,Is.InRange(89,90));
            b.Advance(10);Assert.That(b.ArenaRedeploymentRemaining("CARD_M07"),Is.EqualTo(cd-10).Within(.001));
            Assert.That(b.CheckCardDeployment("CARD_M07",b.Entry),Is.EqualTo(DeployFailure.CoolingDown));
        }
        [Test]public void PauseFreezesRedeploymentAndBossThenReadySquadCanDeployWhilePaused()
        {
            var b=Make(hp:10,bossAttack:1000,cooldown:60);b.Begin();b.Advance(.5f);b.SetPaused(true);
            double clock=b.Clock,cd=b.ArenaRedeploymentRemaining("CARD_M07");float health=b.ArenaBoss.Health;
            b.Advance(120);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.ArenaRedeploymentRemaining("CARD_M07"),Is.EqualTo(cd));
            Assert.That(b.ArenaBoss.Health,Is.EqualTo(health));b.SetPaused(false);b.Advance((float)cd+.1f);b.SetPaused(true);
            Assert.That(b.DeployReadyArenaSquad(true),Is.EqualTo(2));Assert.That(b.IsPaused,Is.True);Assert.That(b.DP,Is.Zero);
        }
        [Test]public void BelowHalfSpeedsRealShellsAndAnimationTogether()
        {
            var b=Make(hp:100000);b.Begin();b.Advance(8.2f);
            var normal=b.CombatShots.Where(s=>s.FromDefender).Select(s=>s.StartedAt).ToArray();
            Assert.That(normal[1]-normal[0],Is.EqualTo(4).Within(.051));
            typeof(DefenderState).GetProperty("Health").SetValue(b.ArenaBoss,b.ArenaBoss.MaxHealth*.49f);
            b.Advance(10);var fast=b.CombatShots.Where(s=>s.FromDefender&&s.StartedAt>12).Select(s=>s.StartedAt).ToArray();
            Assert.That(fast.Length,Is.GreaterThanOrEqualTo(2));Assert.That(fast[1]-fast[0],Is.EqualTo(2).Within(.051));
            Assert.That(b.ArenaBoss.LastActionRate,Is.EqualTo(2));Assert.That(b.ArenaEnragedAt,Is.LessThan(8.3));
        }
        [Test]public void KillingBossWinsWithoutBreachingAndRestartClearsArenaState()
        {
            var b=Make(bossHp:10);b.Begin();b.Advance(.5f);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.Breaches,Is.Zero);
            b.Restart();Assert.That(b.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(b.Units.Count,Is.Zero);
            Assert.That(b.ArenaBoss.Health,Is.EqualTo(10));Assert.That(b.ArenaDeployments,Is.Zero);b.Begin();Assert.That(b.ActiveCount,Is.EqualTo(2));
        }
        [Test]public void NonAttackerAtRouteEndStaysOnFieldAndCannotDisableTheBossByBreaching()
        {
            var u=new ManualBattleSettings("CARD_M17","M17",0,1,1,4,20,100000,1,1,1,1,1);
            var c=new EngagementSettings(unitAttack:0,unitRange:1,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,canAttack:false);
            var boss=new DefenderSettings(BossArenaRules.BossId,"Boss",null,Vector3.right,100000,10,0,7,1,blocks:0);
            var b=new ManualBattle(u,new[]{Vector3.zero,Vector3.right*2},Vector3.right,c,roster:new[]{boss});
            b.ConfigureCardLoadout(new BasicRosterProfile[0]);b.ConfigureBossArena(new BossArenaRules());b.Begin();b.Advance(4);
            Assert.That(b.Units.Single().Motion.HasArrived,Is.True);Assert.That(b.Units.Single().Active,Is.True);Assert.That(b.Breaches,Is.Zero);
            Assert.That(b.Defense,Is.EqualTo(1));Assert.That(b.ArenaCardOnField("CARD_M17"),Is.True);int shots=b.ShotsFired;b.Advance(3);
            Assert.That(b.ShotsFired,Is.GreaterThan(shots));Assert.That(b.ArenaRedeploymentRemaining("CARD_M17"),Is.Zero);
        }
    }
}
