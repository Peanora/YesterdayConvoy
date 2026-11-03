using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class StrongholdChallengeTests
    {
        [TestCase(-1f)][TestCase(1f)]public void ReferenceWavesAllowASecondOfTimingTolerance(float offset)
        {
            var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
            foreach(var order in plan.orders)if(order.time>0)order.time+=offset;
            Assert.That(StrongholdChallengePlan.Run(plan).Phase,Is.EqualTo(BattlePhase.Won));
        }
        [Test]public void MixedReferencePlanWinsAndTheSameOrdersOnOneWingCannotWin()
        {
            var source=Resources.Load<TextAsset>("StrongholdChallengeSolution");Assert.That(source,Is.Not.Null);
            var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(source.text);var b=StrongholdChallengePlan.Run(plan);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.DP,Is.EqualTo(0));Assert.That(b.Units.Count,Is.EqualTo(14));
            Assert.That(b.Lanes.All(l=>b.StrongholdExitBroken(l.ExitId)),Is.True);
            foreach(var order in plan.orders)order.lane=0;
            var oneWing=StrongholdChallengePlan.Run(plan);Assert.That(oneWing.Phase,Is.Not.EqualTo(BattlePhase.Won));
        }
        [TestCase("M02")][TestCase("M08")][TestCase("M18")]public void FourCopiesOfOneRoleCannotBeatSixStarDefense(string role)
        {
            var plan=new StrongholdChallengePlan.Plan{orders=Enumerable.Range(0,4).Select(i=>new StrongholdChallengePlan.Order{time=i*3,card="CARD_"+role,lane=i%2}).ToArray()};
            var b=StrongholdChallengePlan.Run(plan);Assert.That(b.Phase,Is.Not.EqualTo(BattlePhase.Won));Assert.That(b.ActiveCount,Is.Zero);
        }
        [Test]public void ExpandedRoutesAvoidWallsPlatformsAndBarricades()
        {
            foreach(var lane in StrongholdChallengeLayout.Lanes())
            {
                Assert.That(Mathf.Abs(lane.points[0].z),Is.EqualTo(4));
                int portal=StrongholdChallengeLayout.PortalSource(lane.points);
                Assert.That(StrongholdChallengeLayout.Tile(Mathf.RoundToInt(lane.points[portal].x+10),1),Is.EqualTo('I'));
                for(int i=1;i<lane.points.Length;i++)
                {
                    if(i==portal+1)continue;
                    Vector3 a=lane.points[i-1],b=lane.points[i];Assert.That(a.x==b.x||a.z==b.z,Is.True);
                    int distance=Mathf.RoundToInt(Vector3.Distance(a,b));
                    for(int j=0;j<=distance;j++)
                    {
                        var p=Vector3.Lerp(a,b,j/(float)distance);int col=Mathf.RoundToInt(p.x+10),row=Mathf.RoundToInt(p.z+3);
                        Assert.That(StrongholdChallengeLayout.Tile(col,row),Is.Not.EqualTo('#'),$"wall {col},{row}");
                        Assert.That(StrongholdChallengeLayout.Tile(col,row),Is.Not.EqualTo('H'),$"high ground {col},{row}");
                        Assert.That(row==5&&new[]{3,4,8,12,16,17}.Contains(col),Is.False);
                    }
                }
            }
        }
        [Test]public void CompletedSixStarSkillsAreClonedWithoutChangingTheirAssets()
        {
            string[] ids={"D21","D22","D27","D28","D29","D30"};
            var originals=ids.Select(id=>Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id)).ToArray();
            var json=originals.Select(JsonUtility.ToJson).ToArray();
            var map=StrongholdChallengeLayout.Create(Resources.Load<DefenderTrialDefinition>("Maps/BattleShop"));
            try
            {
                Assert.That(map.Snapshot().Length,Is.EqualTo(6));
                for(int i=0;i<6;i++)
                {
                    Assert.That(map.defenders[i].skill.kind,Is.EqualTo(originals[i].defenders[0].skill.kind));
                    Assert.That(JsonUtility.ToJson(originals[i]),Is.EqualTo(json[i]));
                    var p=map.defenders[i].position;char tile=StrongholdChallengeLayout.Tile(Mathf.RoundToInt(p.x+10),Mathf.RoundToInt(p.z+3));
                    Assert.That(tile=='H',Is.EqualTo(p.y>.5f));
                }
            }
            finally{Object.DestroyImmediate(map);}
        }
        static ManualBattle Model(float range=.75f)
        {
            var combat=new EngagementSettings(unitAttack:30,unitArmor:0,unitRange:range,unitInterval:1,defenderHealth:1000,defenderAttack:1,defenderArmor:0,defenderRange:.01f,defenderInterval:1,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:range<1);
            return new ManualBattle(new ManualBattleSettings("CARD_M08","M08_ARCHER",24,2,8,1,.1f,1000,1,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},new Vector3(.4f,.76f,0),combat);
        }
        [TestCase(.75f,false)][TestCase(3f,true)]public void OnlyRangedUnitsCanAttackRaisedDefender(float range,bool expected)
        {
            var b=Model(range);b.ConfigureStrongholdMap(true);b.ConfigureDefenderHighGround(b.Defender.EntityId,true);b.Begin();b.TryDeploy(b.RunId,1,b.Entry);b.Advance(2);
            Assert.That(b.CombatShots.Any(s=>!s.FromDefender),Is.EqualTo(expected));
            b.Restart();Assert.That(b.Defender.HighGround,Is.True,"Restart must preserve terrain rules.");
        }
        [Test]public void TwoBreachesAtTheSameExitCannotCompleteBothObjectives()
        {
            var combat=Model().CombatSettings;
            var lanes=new[]{new DeploymentLane("A","left",new[]{Vector3.zero,Vector3.forward}),new DeploymentLane("B","right",new[]{Vector3.right*10,Vector3.right*10+Vector3.forward})};
            var b=new ManualBattle(new ManualBattleSettings("CARD_M08","M08_ARCHER",10,2,4,1,.1f,1000,10,1,1,1,1),lanes[0].Points,Vector3.one*100,combat,deploymentLanes:lanes);
            b.ConfigureStrongholdMap(true);b.Begin();
            for(int i=0;i<2;i++){Assert.That(b.TryDeploy(b.RunId,i+1,lanes[0].Entry).Success,Is.True);b.Advance(.3f);}
            Assert.That(b.Defense,Is.EqualTo(1));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(b.TryDeploy(b.RunId,3,lanes[1].Entry).Success,Is.True);b.Advance(.3f);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));
            b.Restart();Assert.That(b.StrongholdExitBroken("left"),Is.False);
        }
    }
}
