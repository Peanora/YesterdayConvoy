using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Editor.Tests
{
    public sealed class AutomaticMonsterSkillTests
    {
        static ManualBattle Boss(string task,out DeployedUnitState unit,int phase=1)
        {
            Assert.That(BossRosterCatalog.TryGet("CARD_"+task,out var e),Is.True);
            var settings=new ManualBattleSettings(e.CardId,e.UnitId,100,5,3,1,.2f,10000,.01f,1,30,1,10000);
            var combat=new EngagementSettings(unitAttack:10,unitArmor:0,unitRange:e.Range,unitInterval:e.Interval,defenderHealth:1000000,defenderAttack:.01f,defenderArmor:0,defenderRange:3,defenderInterval:10000,initialSP:0,requiredSP:8,spPerSecond:1,skillAmmo:1,skillAttack:10,skillInterval:4.8f,skillRange:3,attackDuration:e.AttackDuration,attackWindup:e.Windup,canAttack:task!="B10"&&task!="B21");
            var guards=new[]{new DefenderSettings("near","near",null,new Vector3(.6f,0,0),1000000,.01f,0,3,10000,1),new DefenderSettings("rear","rear",null,new Vector3(1.8f,0,.3f),1000000,.01f,0,3,10000,0)};
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.right,combat,roster:guards);b.ConfigureBossRosterMatrixFeed(false);b.EnableAutomaticSkills();b.Begin();unit=b.FindUnit(b.TryDeploy(b.RunId,1,b.Entry).EntityId);b.BossRoster(unit).Phase=phase;return b;
        }
        [TestCase("B01",1)][TestCase("B03",1)][TestCase("B04",1)][TestCase("B05",1)][TestCase("B07",1)][TestCase("B08",1)][TestCase("B09",1)][TestCase("B10",1)][TestCase("B11",1)][TestCase("B12",1)][TestCase("B13",2)][TestCase("B14",1)][TestCase("B15",1)][TestCase("B21",1)][TestCase("B22",2)][TestCase("B23",1)][TestCase("B24",1)][TestCase("B25",1)][TestCase("B26",1)]
        public void EveryNewBossStartsARealAutomaticAbilityWithoutAnyCastCommand(string task,int phase)
        {
            var b=Boss(task,out var u,phase);for(int i=0;i<4800&&u.Active&&!b.AutomaticMonsterCasts.Any(c=>c.EntityId==u.EntityId);i++)b.Advance(.05f);
            Assert.That(b.AutomaticMonsterCasts.Any(c=>c.EntityId==u.EntityId),Is.True,task+": "+b.AutomaticSkillStatus(u));
            Assert.That(b.BossRosterEvents.Any(e=>e.EntityId==u.EntityId&&e.Kind=="SkillStart"),Is.True);
        }
        static ManualBattle W(out DeployedUnitState unit)
        {
            var settings=new ManualBattleSettings("CARD_B01","W",100,5,3,1,.2f,10000,.01f,1,30,1,10000);
            var combat=new EngagementSettings(unitAttack:10,unitArmor:0,unitRange:2.5f,unitInterval:4,defenderHealth:100000,defenderAttack:.01f,defenderArmor:0,defenderRange:3,defenderInterval:10000,initialSP:0,requiredSP:8,spPerSecond:1,skillAmmo:1,skillAttack:10,skillInterval:4.8f,skillRange:3,attackDuration:1.3f,attackWindup:.3f);
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*100},Vector3.right,combat);b.EnableAutomaticSkills();b.Begin();unit=b.FindUnit(b.TryDeploy(b.RunId,1,b.Entry).EntityId);unit.NextAttackTime=10000;return b;
        }
        [Test]public void WAutomaticallyInstallsAfterNineSecondsAndRepeatsOnItsSourceCooldown()
        {
            var b=W(out var u);b.Advance(8.95f);Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Single().At,Is.EqualTo(9).Within(.051));
            float hp=b.Defender.Health;b.Advance(3.9f);Assert.That(b.Defender.Health,Is.LessThan(hp));
            b.Advance(16.9f);Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(1));b.Advance(.3f);
            Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(2));Assert.That(b.AutomaticMonsterCasts[1].At-b.AutomaticMonsterCasts[0].At,Is.EqualTo(21).Within(.051));
        }
        [Test]public void AutomaticWWaitsForALegalTargetWithoutSpendingItsReadyCooldown()
        {
            var b=W(out var u);b.Defender.Position=Vector3.right*50;b.Advance(12);Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.Defender.Position=u.Motion.Position+Vector3.right;b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(1));
        }
        [Test]public void WSourceHalfHealthResetAndPauseArePerEntityAndRestartClearsCasts()
        {
            var b=W(out var u);b.Advance(1);b.TryDamageUnit(u.EntityId,u.MaxHealth*.6f,CombatDamageType.Physical);b.SetPaused(true);b.Advance(30);Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.SetPaused(false);b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Single().At,Is.LessThan(1.2));
            b.Restart();Assert.That(b.AutomaticMonsterCasts,Is.Empty);Assert.That(b.AutomaticSkillsEnabled,Is.True);
        }
        [Test]public void SuiUsesItsTwentyOfFiftySourceSpAndStopsRecoveringWhilePresent()
        {
            var b=Boss("B21",out var u);b.Advance(29.9f);Assert.That(b.AutomaticMonsterCasts,Is.Empty);
            b.Advance(.3f);Assert.That(b.AutomaticMonsterCasts.Single().At,Is.EqualTo(30).Within(.051));
            b.Advance(25);Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(1));
            b.Advance(45);Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(2));Assert.That(b.AutomaticMonsterCasts[1].At,Is.GreaterThanOrEqualTo(99.9));
        }
        [TestCase("B03",.85f)][TestCase("B11",2.5f)]
        public void AReadyShortRangeAbilityApproachesItsTriggerAlongTheExistingRoute(string task,float distance)
        {
            var b=Boss(task,out var u);for(int i=0;i<b.Defenders.Count;i++)b.Defenders[i].Position=Vector3.right*(distance+i*.2f);
            b.Advance(70);Assert.That(b.AutomaticMonsterCasts.Any(e=>e.EntityId==u.EntityId),Is.True,"A unit must not remain permanently outside its ready skill condition at the ordinary attack boundary.");
        }
        [Test]public void WitchSecondFormForcesItsEntryEdictThenUsesTheRegularCooldown()
        {
            var b=Boss("B11",out var u,2);b.Advance(.1f);Assert.That(b.AutomaticMonsterCasts.Single().SkillId,Is.EqualTo("edict"));
            Assert.That(b.AutomaticMonsterCasts.Single().At,Is.LessThan(.1));b.Advance(45);Assert.That(b.AutomaticMonsterCasts.Count(e=>e.SkillId=="edict"),Is.EqualTo(1));
        }
        [Test]public void SaintOnlyReturnsOnRealLethalAndHasTwoReturnsPerMode()
        {
            var b=Boss("B13",out var u,2);b.Advance(1);Assert.That(b.AutomaticMonsterCasts.Any(e=>e.SkillId=="sacrament"),Is.False);
            for(int i=0;i<4;i++)
            {
                Assert.That(b.TryDamageUnit(u.EntityId,u.MaxHealth*10,CombatDamageType.Physical),Is.True);Assert.That(b.BossRoster(u).Transitioning,Is.True);
                b.Advance(3.1f);Assert.That(u.Active,Is.True);Assert.That(u.Health,Is.EqualTo(u.MaxHealth));Assert.That(b.BossRoster(u).Phase,Is.EqualTo(i%2==0?3:2));
            }
            Assert.That(b.AutomaticMonsterCasts.Count(e=>e.SkillId=="sacrament"),Is.EqualTo(4));
            b.TryDamageUnit(u.EntityId,u.MaxHealth*10,CombatDamageType.Physical);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
        }
        [Test]public void StatusAndChargeDisplayDoNotChangeTargetsObjectsOrSkillResources()
        {
            var b=Boss("B08",out var u);b.Advance(1);string target=u.CombatTargetId;int objects=b.BossRosterObjects.Count,casts=b.AutomaticMonsterCasts.Count;var sp=b.BossRoster(u).SP.ToArray();
            for(int i=0;i<10;i++){b.AutomaticSkillStatus(u);b.AutomaticSkillChargeRatio(u);}
            Assert.That(u.CombatTargetId,Is.EqualTo(target));Assert.That(b.BossRosterObjects.Count,Is.EqualTo(objects));Assert.That(b.AutomaticMonsterCasts.Count,Is.EqualTo(casts));Assert.That(b.BossRoster(u).SP.ToArray(),Is.EqualTo(sp));
        }
    }
}
