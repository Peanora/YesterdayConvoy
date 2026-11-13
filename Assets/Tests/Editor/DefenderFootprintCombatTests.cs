using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderFootprintCombatTests
    {
        static ManualBattle Battle(DefenderSkillKind kind,Vector3 point,float oldRadius=.9f)
        {
            var skill=new DefenderSkillSettings(kind,1,1,1,30,effectRadius:oldRadius,targets:3,damageAmplification:1.55f,moveMultiplier:.4f,healFraction:.35f);
            var guard=new DefenderSettings("caster","caster","D25",Vector3.zero,100000,50,0,.9f,1,windup:.1f,actionDuration:.8f,skill:skill);
            var battle=new ManualBattle(new ManualBattleSettings("boundary","target",20,1,6,1,.1f,100000,.001f,1,.1f,.1f,1),
                new[]{point,point+Vector3.right*100},Vector3.zero,new EngagementSettings(unitAttack:0,unitRange:.1f,canAttack:false),roster:new[]{guard});
            battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);return battle;
        }
        // These positions expose both classes of the old radial error: a marked
        // outer corner rejected by the radius and an unmarked corner accepted.
        [TestCase(DefenderSkillKind.HoshigumaInferno,2.25f,1.25f,true)]
        [TestCase(DefenderSkillKind.HoshigumaInferno,2.25f,2.25f,false)]
        [TestCase(DefenderSkillKind.MlynarGlory,3.25f,.25f,true)]
        [TestCase(DefenderSkillKind.MlynarGlory,3.25f,1.25f,false)]
        [TestCase(DefenderSkillKind.AngelinaWishes,2.25f,2.25f,true)]
        [TestCase(DefenderSkillKind.AngelinaWishes,3.25f,2.25f,false)]
        [TestCase(DefenderSkillKind.ClemensiaCorrosion,3.25f,.25f,true)]
        [TestCase(DefenderSkillKind.ClemensiaCorrosion,2.25f,1.25f,false)]
        public void ActualSkillSelectionAndDamageRespectTheMarkedCells(DefenderSkillKind kind,float x,float z,bool inside)
        {
            var b=Battle(kind,new Vector3(x,0,z),10);b.Advance(4);
            bool hit=b.CombatShots.Any(s=>s.FromDefender&&(s.Empowered||s.DefenderSkillWasActive==true)&&s.Applied&&s.DamageDealt>0);
            Assert.That(hit,Is.EqualTo(inside));
            Assert.That(b.Defender.SkillCasts>0,Is.EqualTo(inside),"An unmarked target must not start an otherwise empty skill.");
        }
        [TestCase(3.25f,.25f,true)]
        [TestCase(2.25f,2.25f,false)]
        public void SariaHealingCastDoesNotApplyDebuffsBeyondItsVisibleDiamond(float x,float z,bool inside)
        {
            var b=Battle(DefenderSkillKind.SariaCalcification,new Vector3(x,0,z),10);
            b.Defender.Health-=100;b.Advance(1.5f);
            Assert.That(b.DefenderSkillActive(b.Defender),Is.True,"The wounded caster provides a real healing reason to start.");
            Assert.That(b.UnitArtsVulnerability(b.Units[0])>1,Is.EqualTo(inside));
            Assert.That(b.DefenderAuraMoveMultiplier(b.Units[0])<1,Is.EqualTo(inside));
        }
        [Test]public void PausedAndRestartedBattleRetainsThenReacquiresItsOwnFieldFacing()
        {
            var b=Battle(DefenderSkillKind.MlynarGlory,new Vector3(3,0,0));b.Advance(2);
            Assert.That(b.DefenderSkillActive(b.Defender),Is.True);
            var direction=b.DefenderSkillFacing(b.Defender);b.SetPaused(true);b.Advance(10);
            Assert.That(b.DefenderSkillFacing(b.Defender),Is.EqualTo(direction));
            b.Restart();Assert.That(b.Defender.SkillCasts,Is.Zero);Assert.That(b.DefenderRangeContains(b.Defender,Vector3.right*3),Is.False);
        }
        [Test]public void KitchenHealFieldDoesNotExpandItsUnchangedMeleeAttackReach()
        {
            var b=Battle(DefenderSkillKind.YuKitchen,Vector3.right*3,10);b.Advance(4);
            Assert.That(b.DefenderSkillActive(b.Defender),Is.True,"The actual firewall may activate with a distant target.");
            Assert.That(b.DefenderRangeContains(b.Defender,b.Units[0].Motion.Position),Is.False);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender&&s.Applied&&s.DamageDealt>0),Is.False);
        }
    }
}
