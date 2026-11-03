using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class SixthDefenderTests
    {
        static ManualBattle New(DefenderSkillKind kind,bool reserve=false,float interval=2)
        {
            var s=new DefenderSkillSettings(kind,1,1,1,kind==DefenderSkillKind.WangGambit?99999:35,attackMultiplier:1.7f,attackSpeedMultiplier:1.3f,effectRadius:10);
            var roster=new[]{new DefenderSettings("caster","caster","D26",Vector3.right*2,10000,20,0,10,interval,actionDuration:interval,skill:s),
                new DefenderSettings("hi","hi","D04",Vector3.forward,10000,1,0,1,100,reserve:reserve,reserveCost:20,relayPosition:Vector3.forward),
                new DefenderSettings("lo","lo","D01",Vector3.back,10000,1,0,1,100,reserve:reserve,reserveCost:8)};
            var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,12,1,.1f,100000,1,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:1,unitInterval:100,unitRange:.1f),roster:roster);return b;
        }
        static void Begin(ManualBattle b){b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);}
        [Test]public void SilverAshSwapsReserveCostsDeploysPartnerAndKeepsPlayerDPSeparate()
        {var b=New(DefenderSkillKind.SilverAshReign,true);Begin(b);b.Advance(1);Assert.That(b.Defender.CostsExchanged,Is.True);Assert.That(b.FindDefender("hi").ReserveCost,Is.EqualTo(8));Assert.That(b.FindDefender("lo").ReserveCost,Is.EqualTo(20));Assert.That(b.Defender.PartnerId,Is.EqualTo("hi"));Assert.That(b.Defender.Reinforcements,Is.EqualTo(1));Assert.That(b.DefenderBudget,Is.GreaterThan(28));Assert.That(b.DP,Is.EqualTo(29));b.Restart();Assert.That(b.DefenderBudget,Is.EqualTo(30));Assert.That(b.FindDefender("hi").ReserveCost,Is.EqualTo(20));}
        [Test]public void SilverAshAppliedLineAttackCreatesTimedVulnerabilityAndFreezesOnPause()
        {var b=New(DefenderSkillKind.SilverAshReign);Begin(b);b.Advance(1);var u=b.Units[0];Assert.That(b.CombatShots.Any(x=>x.Empowered&&x.Applied),Is.True);Assert.That(b.SixthVulnerability(u),Is.GreaterThan(1));b.SetPaused(true);double clock=b.Clock;b.Advance(10);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.SixthVulnerability(u),Is.GreaterThan(1));}
        [Test]public void PramanixLureActuallyChangesRouteProgressAndSnowLayersStayBounded()
        {var b=New(DefenderSkillKind.PramanixSnow);b.Begin();b.Advance(5);b.TryDeploy(b.RunId,1,b.Entry);b.Advance(.5f);var u=b.Units[0];Assert.That(u.LureUntil,Is.GreaterThan(b.Clock));Assert.That(u.Motion.DistanceTravelled,Is.LessThan(.06));b.Advance(8);Assert.That(b.SnowCells.Any(c=>c.Layers>0),Is.True);Assert.That(b.SnowCells.All(c=>c.Layers<=5),Is.True);b.Restart();Assert.That(b.SnowCells,Is.Empty);}
        [Test]public void WangConsumesAmmoTriggersRealChessDamageAndResetsStock()
        {var b=New(DefenderSkillKind.WangGambit);Begin(b);b.Advance(12);Assert.That(b.Defender.ChessTriggers,Is.GreaterThan(0));Assert.That(b.Defender.Ammo,Is.LessThan(20));Assert.That(b.Units[0].Health,Is.LessThan(b.Units[0].MaxHealth));Assert.That(b.Defender.ChessStock,Is.InRange(0,7));b.Restart();Assert.That(b.ChessPieces,Is.Empty);Assert.That(b.Defender.ChessStock,Is.EqualTo(7));}
        [Test]public void ProjectionAndApproachRespectCornersAndNeverTeleportAcrossWalls()
        {var m=new RouteMotion(new[]{Vector3.zero,Vector3.right*2,Vector3.right*2+Vector3.forward*3},1);double target=m.ProjectDistance(Vector3.right*3+Vector3.forward*2);Assert.That(target,Is.EqualTo(4).Within(.000001));m.ApproachDistance(target,1);Assert.That(m.Position,Is.EqualTo(Vector3.right));m.ApproachDistance(target,2);Assert.That(m.Position,Is.EqualTo(Vector3.right*2+Vector3.forward));m.ApproachDistance(0,1);Assert.That(m.Position,Is.EqualTo(Vector3.right*2));}
        [Test]public void ContinuousAttacksCannotStarveReadySkillAtFloatTimeBoundary()
        {var b=New(DefenderSkillKind.SilverAshReign,interval:1.2f);Begin(b);for(int i=0;i<100;i++)b.Advance(.1f);Assert.That(b.Defender.SkillCasts,Is.GreaterThan(0));}
        [Test]public void IgnoringResistanceCannotCreateNegativeResistanceBonusDamage()
        {var b=New(DefenderSkillKind.PramanixSnow);Begin(b);var u=b.Units[0];float hp=u.Health;Assert.That(b.TryDamageUnit(u.EntityId,100,CombatDamageType.Arts,10),Is.True);Assert.That(hp-u.Health,Is.EqualTo(100));}
        [Test]public void SixthAssetsContainRealS3AndPramanixHasNoFabricatedOrdinaryAttack()
        {foreach(var id in ManualBattleController.DefenderBatch6Ids){var d=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Assert.That(d,Is.Not.Null);Assert.That(d.Snapshot()[0].Skill.Enabled,Is.True);Assert.That(DefenderArt.Load(id)["skill_attack"].Frames.Length,Is.GreaterThan(1));}Assert.That(DefenderArt.Load("D27").ActionDuration,Is.Zero);}
    }
}
