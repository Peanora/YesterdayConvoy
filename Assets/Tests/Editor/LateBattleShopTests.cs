using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class LateBattleShopTests
    {
        internal static ManualBattle Create(int stage,string extra=null)
        {
            var map=StrongholdChallengeLayout.Create(Resources.Load<DefenderTrialDefinition>("Maps/BattleShop"));
            map.defenders=BattleShopStages.Defenders(stage,true);
            var ids=new[]{"M08","M02","M17","M18","M05"}.Concat(extra==null?new string[0]:new[]{extra}).Distinct();
            var data=ids.Select(id=>Resources.Load<ManualBattleDefinition>("MonsterTests/"+id)).ToArray();
            var first=data[0];var b=new ManualBattle(BattleShopStages.Settings(first.Snapshot(),true),map.route,first.Engagement.DefenderPosition,first.Engagement.Snapshot(),first.Engagement.ExtraDefenders,map.Snapshot(),map.LaneSnapshot());
            b.ConfigureCardLoadout(data.Skip(1).Select(d=>new BasicRosterProfile(BattleShopStages.Settings(d.Snapshot(),true),d.Engagement.Snapshot())));
            b.ConfigureStrongholdMap(true);foreach(var d in b.Defenders)b.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
            Object.DestroyImmediate(map);return b;
        }
        [TestCase(1)][TestCase(2)]public void BothSixStarStagesCanBeWonWithRealMixedWaves(int stage)
        {
            var b=Create(stage);var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
            b.Begin();int index=0;long request=0;
            for(int tick=0;tick<2400&&b.Phase==BattlePhase.Running;tick++)
            {
                while(index<plan.orders.Length&&plan.orders[index].time<=b.Clock+.001)
                {var order=plan.orders[index++];Assert.That(b.TryDeployRole(b.RunId,++request,b.Lanes[order.lane].Entry,order.card).Success,Is.True);}
                b.Advance(.1f);
            }
            TestContext.WriteLine($"stage={stage},phase={b.Phase},clock={b.Clock},guards={b.Defenders.Count(d=>d.Alive)},casts={string.Join(",",b.Defenders.Select(d=>d.SkillCasts))}");
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.Defenders.Count,Is.EqualTo(6));
            Assert.That(b.Defenders.All(d=>d.Settings.Skill.Enabled),Is.True);
            Assert.That(b.Lanes.All(l=>b.StrongholdExitBroken(l.ExitId)),Is.True);
        }
        [Test]public void SecondStageChangesDefenseCoverageWithoutMutatingAuthoredSkills()
        {
            var first=BattleShopStages.Defenders(1,true);var second=BattleShopStages.Defenders(2,true);
            Assert.That(first.Zip(second,(a,b)=>a.position!=b.position).Count(v=>v),Is.EqualTo(4));
            for(int i=0;i<6;i++){Assert.That(first[i].skill.kind,Is.EqualTo(second[i].skill.kind));Assert.That(first[i].health,Is.EqualTo(second[i].health));}
        }
    }
}
