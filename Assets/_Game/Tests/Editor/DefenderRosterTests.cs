using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderRosterTests
    {
        private static ManualBattle Battle(DefenderSettings[] roster,float attack=30,float interval=.5f,float range=5)
        {
            var settings=new ManualBattleSettings("card","unit",20,1,6,1,.1f,10000,1,1,1,1,1);
            var battle=new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*20},Vector3.one,
                new EngagementSettings(unitAttack:attack,unitInterval:interval,unitRange:range),roster:roster);
            battle.Begin(); return battle;
        }
        private static DefenderSettings Spec(string id,Vector3 position,float health=100,int blocks=0,
            DefenderRole role=DefenderRole.Fighter,float range=3,float windup=0,float action=0,float interval=1)
            => new DefenderSettings(id,id,"D01",position,health,12,3,range,interval,blocks,role,windup:windup,actionDuration:action);

        [Test] public void RosterIsSnapshotAndRestartRestoresEachDifferentMaximum()
        {
            var specs=new[]{Spec("tank",Vector3.forward,240,3),Spec("high",Vector3.right,90)};
            var b=Battle(specs); specs[0]=Spec("changed",Vector3.zero,1);
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(.5f);
            Assert.That(b.FindDefender("tank").Health,Is.LessThan(240));
            Assert.That(b.Defenders[0].MaxHealth,Is.EqualTo(240)); Assert.That(b.Defenders[1].MaxHealth,Is.EqualTo(90));
            b.Restart(); Assert.That(b.Defenders.Select(d=>d.Health),Is.EqualTo(new[]{240f,90f}));
            Assert.That(b.Defenders.All(d=>d.TargetId==null && double.IsNegativeInfinity(d.LastActionTime)),Is.True);
        }
        [Test] public void PerDefenderBlockCapacityLetsOverflowMovePastGroundUnit()
        {
            var b=Battle(new[]{Spec("ground",Vector3.forward,1000,2,range:.01f),Spec("high",new Vector3(0,.5f,4),1000,0,range:.01f)},attack:1,range:.01f);
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(.7f);
            b.TryDeploy(b.RunId,2,b.Entry); b.Advance(.7f);
            b.TryDeploy(b.RunId,3,b.Entry); b.Advance(2);
            Assert.That(b.Defenders[0].BlockedCount,Is.EqualTo(2)); Assert.That(b.Defenders[1].BlockedCount,Is.Zero);
            Assert.That(b.Units[2].Motion.DistanceTravelled,Is.GreaterThan(b.Units[0].Motion.DistanceTravelled));
        }
        [Test] public void MedicHealsWoundedAlliesWithoutDamagingMonstersAndPauseFreezesPendingHealing()
        {
            var b=Battle(new[]{Spec("tank",Vector3.forward,200,3),Spec("medic",new Vector3(1,0,1),100,role:DefenderRole.Medic)});
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(.4f);
            Assert.That(b.CombatShots.Any(s=>s.Healing),Is.True);
            double clock=b.Clock; float hp=b.Defender.Health; b.SetPaused(true); b.Advance(20);
            Assert.That(b.Clock,Is.EqualTo(clock)); Assert.That(b.Defender.Health,Is.EqualTo(hp));
            b.SetPaused(false); b.Advance(.3f);
            Assert.That(b.CombatShots.Any(s=>s.Healing && s.Applied),Is.True);
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="medic" && !s.Healing),Is.False);
            Assert.That(b.Defenders.All(d=>d.Health<=d.MaxHealth),Is.True);
        }
        [Test] public void PendingHealingDoesNotReviveAnAllyKilledDuringWindup()
        {
            var b=Battle(new[]{Spec("tank",Vector3.forward,45),Spec("medic",new Vector3(1,0,1),100,role:DefenderRole.Medic,windup:1,action:1,interval:2)});
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(2);
            Assert.That(b.FindDefender("tank").Alive,Is.False);
            Assert.That(b.CombatShots.Any(s=>s.Healing && s.TargetId=="tank" && s.Resolved && !s.Applied),Is.True);
        }
        [Test] public void FullHealthMedicDoesNotAttackAndDeadMedicCannotFinishHealing()
        {
            var b=Battle(new[]{Spec("medic",Vector3.forward,1,role:DefenderRole.Medic)});
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(.4f);
            Assert.That(b.Defender.Alive,Is.False);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender),Is.False);
        }
        [Test] public void InvalidProfilesAndDuplicateIdsAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>Spec("bad",new Vector3(float.NaN,0,0)));
            Assert.Throws<ArgumentException>(()=>Spec("bad",Vector3.zero,windup:2,action:1));
            Assert.Throws<ArgumentException>(()=>Battle(new[]{Spec("same",Vector3.zero),Spec("same",Vector3.one)}));
        }
        [Test] public void GuardianHealingRangeDoesNotExtendItsMeleeAttackRange()
        {
            var guard=new DefenderSettings("guardian","guard","D03",new Vector3(2,0,1),180,15,8,.85f,1,3,
                DefenderRole.Guardian,healingRange:3);
            var b=Battle(new[]{Spec("tank",Vector3.forward,200,3),guard});
            b.TryDeploy(b.RunId,1,b.Entry); b.Advance(1);
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="guardian" && s.Healing && s.Applied),Is.True);
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="guardian" && !s.Healing),Is.False);
        }
    }
}
