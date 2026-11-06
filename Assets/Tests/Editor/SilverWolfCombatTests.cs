using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class SilverWolfCombatTests
    {
        static ManualBattle Create(int seed)
        {
            var config=new ManualBattleSettings(SilverWolfCombat.CardId,"wolf",100,2,1,2,.1f,5000,1,1,1,1,1);
            var combat=new EngagementSettings(unitRange:4,unitInterval:1.9f,attackDuration:1.7f,attackWindup:.80f,
                initialSP:28,requiredSP:28,spPerSecond:1,skillAmmo:3,skillAttack:100,skillInterval:4.6f,skillRange:5);
            var guards=new[]{new DefenderSettings("near","a",null,Vector3.right,2000,1,0,.1f,100),
                new DefenderSettings("far","b",null,Vector3.right*20,3000,1,999,.1f,100)};
            var b=new ManualBattle(config,new[]{Vector3.zero,Vector3.right*100},Vector3.right,combat,roster:guards);
            typeof(ManualBattle).GetField("wolfRolls",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,new System.Random(seed));
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);return b;
        }
        static void Cast(ManualBattle b)=>Assert.That(b.TryActivateSkill(b.RunId,2,b.Units[0].EntityId),Is.EqualTo(SkillFailure.None));

        [TestCase(WolfBoxKind.Sword)][TestCase(WolfBoxKind.Bomb)][TestCase(WolfBoxKind.Bean)]
        public void BoxRollsOnceOnAttackAndSettlesAtItsOwnImpact(WolfBoxKind box)
        {
            int seed=Enumerable.Range(0,100).First(s=>new System.Random(s).Next(3)+1==(int)box);
            var b=Create(seed);var u=b.Units[0];var pos=u.Motion.Position;int dp=b.DP;
            Cast(b);Cast(b);Assert.That(b.SkillCasts,Is.EqualTo(1));
            b.Advance(1.42f); Assert.That(b.CombatShots.Any(s=>!s.FromDefender),Is.False);
            b.SetPaused(true);b.Advance(10);Assert.That(u.Motion.Position,Is.EqualTo(pos));b.SetPaused(false);
            b.Advance(.07f);var shot=b.CombatShots.Single(s=>!s.FromDefender);Assert.That(shot.WolfBox,Is.EqualTo(box));
            Assert.That(u.SkillAmmo,Is.EqualTo(2));Assert.That(shot.Applied,Is.False);
            b.Advance((float)(shot.ImpactAt-b.Clock+.02));Assert.That(shot.Applied,Is.True);
            Assert.That(b.FindDefender("near").Health,Is.EqualTo(1950).Within(.01));
            Assert.That(b.FindDefender("far").Health,Is.EqualTo(2950).Within(.01));
            Assert.That(b.DP,Is.EqualTo(dp+(box==WolfBoxKind.Bomb?2:0)));
            Assert.That(u.SkillSP,Is.EqualTo(box==WolfBoxKind.Bean?3:0).Within(.01));
            b.Advance(1.3f);
            Assert.That(b.FindDefender("far").Health,Is.EqualTo(box==WolfBoxKind.Sword?2930:2950).Within(.01));
            Assert.That(shot.WolfBonusTargetId,Is.EqualTo(box==WolfBoxKind.Sword?"far":null));
        }
        [Test] public void RepeatedDrawsAreLegalAndTheFinalBoxFinishesBeforeNormalCombatResumes()
        {
            int seed=Enumerable.Range(0,200).First(s=>{var r=new System.Random(s);return r.Next(3)==r.Next(3);});
            var b=Create(seed);Cast(b);b.Advance(11);var u=b.Units[0];
            var boxes=b.CombatShots.Where(s=>s.WolfBox!=WolfBoxKind.None).ToArray();
            Assert.That(boxes.Length,Is.EqualTo(3));Assert.That(boxes[0].WolfBox,Is.EqualTo(boxes[1].WolfBox));
            Assert.That(b.IsSkillActionPlaying(u),Is.True);Assert.That(u.SkillAmmo,Is.Zero);
            b.Advance(4.6f);Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));
            b.Restart();Assert.That(b.CombatShots,Is.Empty);Assert.That(b.Units,Is.Empty);
        }
        [Test] public void CastingDuringUppercutPreservesItsRecoveryAndQueuedTransformation()
        {
            var b=Create(4);b.Advance(.3f);var u=b.Units[0];Cast(b);
            Assert.That(u.WolfTransformAt,Is.EqualTo(u.LastAttackTime+1.7).Within(.001));Assert.That(SilverWolfCombat.Transforming(u,b.Clock),Is.False);
            b.Advance(1.5f);Assert.That(SilverWolfCombat.Transforming(u,b.Clock),Is.True);
            Assert.That(b.CombatShots.Count(s=>!s.FromDefender),Is.EqualTo(1));
        }
    }
}
