using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ChenDawnstreakTests
    {
        static ManualBattle New(float health=100000,float attack=10,float skillSeconds=20,float rate=1,float armor=0,float resistance=0)
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.ChenDawnstreak,1,1,1,skillSeconds,attackSpeedMultiplier:rate,effectRadius:3.5f,targets:3);
            var caster=new DefenderSettings("D32","赤刃明霄陈","D32",Vector3.forward,100000,attack,0,1.5f,1.25f,1,
                damageType:CombatDamageType.Arts,windup:13f/30,actionDuration:37f/30,skill:skill);
            var battle=new ManualBattle(new ManualBattleSettings("test","unit",30,1,12,1,.1f,health,.01f,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:1,unitArmor:armor,unitInterval:10,unitRange:1,effects:new MonsterEffectSettings(unitResistance:resistance)),roster:new[]{caster});
            battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);return battle;
        }
        [Test]public void CorrectCharacterUsesTimedSkillGroundBlockingAndOriginalThreeHitEvents()
        {
            var b=New();b.Advance(1.3f);var d=b.Defender;
            Assert.That(d.Settings.Skill.Kind,Is.EqualTo(DefenderSkillKind.ChenDawnstreak));
            Assert.That(b.DefenderGroundBlockCapacity(d),Is.EqualTo(1));Assert.That(b.SeventhUsesAmmo(d),Is.False);Assert.That(d.Ammo,Is.Zero);
            var group=b.CombatShots.Where(s=>s.FromDefender&&s.Empowered&&s.ChenDawnWaveSequence==0).GroupBy(s=>s.DefenderActionStartedAt).First().OrderBy(s=>s.StartedAt).ToArray();
            Assert.That(group,Has.Length.EqualTo(3));
            for(int hit=0;hit<3;hit++){Assert.That(group[hit].StartedAt-group[hit].DefenderActionStartedAt,Is.EqualTo((16+2*hit)/30.0/1.13).Within(.00001));Assert.That(group[hit].ChenDawnHitIndex,Is.EqualTo(hit));Assert.That(group[hit].Attack,Is.EqualTo(20.34f).Within(.0001));Assert.That(group[hit].DamageType,Is.EqualTo(CombatDamageType.Arts));}
            Assert.That(b.SeventhDefenderEvents.Any(e=>e.Kind==SeventhDefenderEventKind.StickyGround),Is.False);
        }
        [Test]public void EachAttackKeepsTheOriginalThreeEventOffsetsAtItsOwnSpeedSnapshot()
        {
            var b=New(rate:2);b.Advance(1);var group=b.CombatShots.Where(s=>s.FromDefender&&s.Empowered&&s.ChenDawnWaveSequence==0).GroupBy(s=>s.DefenderActionStartedAt).First().OrderBy(s=>s.StartedAt).ToArray();
            Assert.That(group,Has.Length.EqualTo(3));
            for(int hit=0;hit<3;hit++){Assert.That(group[hit].DefenderActionRate,Is.EqualTo(2.26f).Within(.0001));Assert.That(group[hit].StartedAt-group[hit].DefenderActionStartedAt,Is.EqualTo((16+2*hit)/60.0/1.13).Within(.00001));}
            double at=group[0].StartedAt;b.Defender.LastActionTime=999;b.Defender.LastActionRate=5;Assert.That(group[0].StartedAt,Is.EqualTo(at));Assert.That(group[0].DefenderActionRate,Is.EqualTo(2.26f).Within(.0001));
        }
        [Test]public void DragonUsesRealMovementAndOneHitPerEnemyBeforeAnyTurn()
        {
            var b=New();b.Advance(1);var wave=b.ChenDawnWaves.Single();
            Assert.That(wave.Active,Is.True);Assert.That(wave.Position.z,Is.EqualTo(1-(float)(b.Clock-wave.StartedAt)*1.5f).Within(.0001));
            Assert.That(wave.Turns,Is.Zero);Assert.That(wave.Hits,Is.EqualTo(1));
            Assert.That(b.CombatShots.Count(s=>s.ChenDawnWaveSequence==wave.Sequence&&s.Applied),Is.EqualTo(1));
            Assert.That(wave.History.Count,Is.GreaterThan(3));
            var point=wave.Position;int history=wave.History.Count,hits=wave.Hits;b.SetPaused(true);b.Advance(20);
            Assert.That(wave.Position,Is.EqualTo(point));Assert.That(wave.History.Count,Is.EqualTo(history));Assert.That(wave.Hits,Is.EqualTo(hits));
        }
        [Test]public void AuthoredHighlandTurnsDragonClockwiseAndReenablesActualContacts()
        {
            var b=New();b.ConfigureChenDawnTerrain(new[]{new Bounds(new Vector3(0,0,-1),new Vector3(1,10,1))},new Bounds(Vector3.zero,new Vector3(4,10,4)));
            b.Advance(1.4f);var wave=b.ChenDawnWaves.Single();
            Assert.That(wave.Turns,Is.EqualTo(1));Assert.That(wave.Direction,Is.EqualTo(Vector3.left));Assert.That(wave.Position.z,Is.GreaterThanOrEqualTo(-.28f));
            Assert.That(wave.Hits,Is.EqualTo(2));Assert.That(b.CombatShots.Count(s=>s.ChenDawnWaveSequence==wave.Sequence&&s.Applied),Is.EqualTo(2));
            Assert.That(wave.History.Any(p=>p.Turn),Is.True);
            Assert.That(b.SeventhDefenderEvents.Count(e=>e.Kind==SeventhDefenderEventKind.DawnWaveTurned),Is.EqualTo(1));
        }
        [Test]public void CornersKeepEveryQuarterTurnAndBothEmitterHeadingsWithoutDiagonalCuts()
        {
            var b=New();b.ConfigureChenDawnTerrain(new[]{new Bounds(new Vector3(0,0,-1),new Vector3(1,10,1))},new Bounds(Vector3.zero,new Vector3(2,10,4)));
            b.Advance(3);var wave=b.ChenDawnWaves.Single();var turns=b.SeventhDefenderEvents.Where(e=>e.Kind==SeventhDefenderEventKind.DawnWaveTurned).ToArray();
            Assert.That(turns.Length,Is.GreaterThan(1));Assert.That(wave.Turns,Is.EqualTo(turns.Length));
            Vector3 direction=wave.History[0].Direction;
            foreach(var turn in turns)
            {
                Assert.That(turn.Direction,Is.EqualTo(new Vector3(direction.z,0,-direction.x)),"A collision emits one clockwise quarter-turn, never a skipped 180/270-degree heading.");
                direction=turn.Direction;
            }
            for(int i=1;i<wave.History.Count;i++)
            {
                var previous=wave.History[i-1];var point=wave.History[i];var delta=point.Position-previous.Position;
                Assert.That(Math.Abs(delta.x)<.00001f||Math.Abs(delta.z)<.00001f,Is.True,"A path segment must not cut diagonally through a corner.");
                Assert.That(delta.magnitude,Is.LessThanOrEqualTo((float)(point.At-previous.At)*1.5f+.00002f));
                if(point.Turn){Assert.That(point.At,Is.EqualTo(previous.At).Within(.0000001));Assert.That(point.Position,Is.EqualTo(previous.Position));Assert.That(Vector3.Dot(point.Direction,previous.Direction),Is.EqualTo(0));}
            }
        }
        [Test]public void HighlandContactUsesExactSourceClearanceAcrossDifferentCallerFrameSizes()
        {
            foreach(float chunk in new[]{.05f,.017f})
            {
                var b=New();b.ConfigureChenDawnTerrain(new[]{new Bounds(new Vector3(0,0,-1),new Vector3(1,10,1))},new Bounds(Vector3.zero,new Vector3(4,10,4)));
                for(int i=0;i<Mathf.CeilToInt(1.6f/chunk);i++)b.Advance(chunk);
                var wave=b.ChenDawnWaves.Single();var turn=b.SeventhDefenderEvents.First(e=>e.Kind==SeventhDefenderEventKind.DawnWaveTurned);
                Assert.That(turn.Position.z,Is.EqualTo(-.25f).Within(.00001));Assert.That(turn.At-wave.StartedAt,Is.EqualTo(1.25/1.5).Within(.00001));
            }
        }
        [Test]public void EnclosedProjectileDoesNotSpinInPlaceOrRepeatedlyResetItsHitSet()
        {
            var b=New();b.ConfigureChenDawnTerrain(Array.Empty<Bounds>(),new Bounds(Vector3.forward,new Vector3(.2f,10,.2f)));
            b.Advance(2);var wave=b.ChenDawnWaves.Single();Assert.That(wave.Position,Is.EqualTo(Vector3.forward));Assert.That(wave.Turns,Is.Zero);Assert.That(wave.Hits,Is.EqualTo(1));
            Assert.That(b.SeventhDefenderEvents.Any(e=>e.Kind==SeventhDefenderEventKind.DawnWaveTurned),Is.False);
        }
        [Test]public void DragonSelectsCurrentHealthFractionWithOriginalMinimumThenClearsAtSkillEndAndRestart()
        {
            var b=New(10000,10,2);b.Advance(.7f);var first=b.CombatShots.Single(s=>s.ChenDawnWaveSequence>0);
            Assert.That(first.Applied,Is.True);Assert.That(first.DamageDealt,Is.EqualTo(600).Within(.001));
            b.Advance(2);Assert.That(b.ChenDawnWaves.Single().Active,Is.False);Assert.That(b.ChenDawnWaves.Single().EndedAt,Is.EqualTo(b.Defender.SkillStartedAt+2).Within(.0001));
            Assert.That(b.SeventhDefenderEvents.Any(e=>e.Kind==SeventhDefenderEventKind.DawnWaveEnded),Is.True);
            b.Restart();Assert.That(b.ChenDawnWaves,Is.Empty);Assert.That(b.SeventhDefenderEvents,Is.Empty);Assert.That(b.CombatShots,Is.Empty);
            var minimum=New(1000,100);minimum.Advance(.7f);Assert.That(minimum.CombatShots.Single(s=>s.ChenDawnWaveSequence>0).DamageDealt,Is.EqualTo(621.5f).Within(.001));
        }
        [Test]public void SkillRangeUsesOriginalCellsAndStaysFacingItsActivationDirection()
        {
            var b=New();b.Advance(.1f);var d=b.Defender;var unit=b.Units[0];Assert.That(b.ChenDawnFacing(d),Is.EqualTo(Vector3.back));
            Assert.That(b.ChenDawnInRange(d,unit,true),Is.True);
            unit.Motion.Advance(300); // Explicit route fixture moves behind the fixed-facing caster.
            Assert.That(b.ChenDawnInRange(d,unit,true),Is.False);Assert.That(b.ChenDawnFacing(d),Is.EqualTo(Vector3.back));
        }
        [TestCase(10000,0,CombatDamageType.Arts)][TestCase(0,100,CombatDamageType.Physical)]
        public void WeaknessTalentChoosesHigherTheoreticalDamageAtRealSettlement(float armor,float resistance,CombatDamageType expected)
        {
            var b=New(armor:armor,resistance:resistance);b.Advance(1.3f);
            var hits=b.CombatShots.Where(s=>s.FromDefender&&s.Applied).ToArray();Assert.That(hits,Is.Not.Empty);
            Assert.That(hits.All(s=>s.DamageType==expected),Is.True);Assert.That(hits.All(s=>s.DamageDealt>0),Is.True);
        }
        [Test]public void QuietPeriodTalentHealsAndDodgesOneRealAttackWithoutGrantingStackedDodge()
        {
            var b=New();b.Advance(1);var d=b.Defender;d.Health-=100;
            double readyAt=d.ChenDawnNextTalentAt;float before=d.Health;
            b.Advance((float)(readyAt-b.Clock-.01));Assert.That(d.ChenDawnDodgeReady,Is.False);
            b.Advance(.03f);Assert.That(d.ChenDawnDodgeReady,Is.True);Assert.That(d.Health-before,Is.InRange(3.38f,18.09f));
            float protectedHealth=d.Health;
            for(int i=0;i<200&&double.IsNegativeInfinity(d.ChenDawnLastDodgeAt);i++)b.Advance(.05f);
            Assert.That(d.ChenDawnLastDodgeAt,Is.GreaterThan(readyAt));Assert.That(d.ChenDawnDodgeReady,Is.False);Assert.That(d.Health,Is.EqualTo(protectedHealth));
            Assert.That(b.CombatShots.Any(s=>!s.FromDefender&&s.Resolved&&!s.Applied&&s.DamageDealt==0),Is.True);
            // Explicit damage fixture after consuming the dodge checks the quiet-period reset.
            typeof(ManualBattle).GetMethod("DamageDefenderFrom",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(b,new object[]{d,10f,CombatDamageType.Arts,null,false,1f});
            Assert.That(d.Health,Is.LessThan(protectedHealth));Assert.That(d.ChenDawnNextTalentAt,Is.EqualTo(b.Clock+7));
            b.Restart();Assert.That(b.Defender.ChenDawnDodgeReady,Is.False);Assert.That(b.Defender.ChenDawnNextTalentAt,Is.EqualTo(7));
        }
    }
}
