using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class TrialDeckTests
    {
        static ManualBattleSettings Settings(string id,string unit,int dp,int uses,int cost,float cd)=>new ManualBattleSettings(id,unit,dp,3,uses,cost,cd,100,.75f,1,1,1,1);
        static EngagementSettings Minion(float shred=0)=>new EngagementSettings(unitAttack:12,unitRange:5,defenderAttack:1,defenderInterval:100,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:new MonsterEffectSettings(armorShred:shred));
        static ManualBattle New(int dp=8,int baseUses=2,Vector3? target=null)
        {
            var lanes=new[]{new DeploymentLane("R1","B1",new[]{Vector3.zero,Vector3.forward*20}),new DeploymentLane("R2","B1",new[]{Vector3.right*3,Vector3.forward*20})};
            var b=new ManualBattle(Settings("CARD_T","T",dp,baseUses,2,2),lanes[0].Points,target??new Vector3(1000,0,0),new EngagementSettings(defenderHealth:2000,defenderAttack:1,defenderInterval:100,blockCount:0),deploymentLanes:lanes);
            b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Settings("CARD_M02","M02_HOUND",dp,3,1,3),Minion()),new BasicRosterProfile(Settings("CARD_M05","M05_ACID",dp,2,1,1),Minion(2))});
            return b;
        }
        [Test]public void PausedCardsPayOwnCostsAndHaveIndependentCooldownsUsesAndReplayIdentity()
        {
            var b=New();b.Begin();b.SetPaused(true);
            var main=b.TryDeploy(b.RunId,1,b.Entry,true);Assert.That(main.Success,Is.True);
            var small=b.TryDeployRole(b.RunId,2,b.Lanes[1].Entry,"CARD_M02",true);Assert.That(small.Success,Is.True);
            Assert.That(b.DP,Is.EqualTo(5));Assert.That(b.FindCard("CARD_T").RemainingUses,Is.EqualTo(1));Assert.That(b.FindCard("CARD_M02").RemainingUses,Is.EqualTo(2));
            Assert.That(b.CardCooldown("CARD_T"),Is.EqualTo(2));Assert.That(b.CardCooldown("CARD_M02"),Is.EqualTo(3));
            Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M05",true).Failure,Is.EqualTo(DeployFailure.StaleRun));
            Assert.That(b.TryDeployRole(b.RunId,2,b.Lanes[1].Entry,"CARD_M02",true).EntityId,Is.EqualTo(small.EntityId));
            b.Advance(10);Assert.That(b.Clock,Is.Zero);Assert.That(b.DP,Is.EqualTo(5));Assert.That(b.Units.Count,Is.EqualTo(2));
            b.Restart();Assert.That(b.DP,Is.EqualTo(8));Assert.That(b.FindCard("CARD_M02").RemainingUses,Is.EqualTo(3));Assert.That(b.CardCooldown("CARD_M02"),Is.Zero);
        }
        [Test]public void CheapCardCanUseLastDPWithoutInheritingBaseSkill()
        {
            var b=New(dp:1);b.Begin();Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Failure,Is.EqualTo(DeployFailure.NoDP));
            var result=b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M02");Assert.That(result.Success,Is.True);Assert.That(b.DP,Is.Zero);
            b.Advance(5);var u=b.FindUnit(result.EntityId);Assert.That(u.SkillSP,Is.Zero);Assert.That(u.SkillAmmo,Is.Zero);
            Assert.That(b.IsSkillReady(u.EntityId),Is.False);Assert.That(b.TryActivateSkill(b.RunId,10,u.EntityId),Is.EqualTo(SkillFailure.NoSkill));
            Assert.That(b.TryDeployRole(b.RunId,3,b.Lanes[1].Entry,"CARD_M05").Failure,Is.EqualTo(DeployFailure.NoDP));
            Assert.That(b.FindCard("CARD_M05").RemainingUses,Is.EqualTo(2));
        }
        [Test]public void AcidShredComesFromRealAttackingProfileAndInvalidDropsSpendNothing()
        {
            var b=New(target:new Vector3(1,0,1));b.Begin();
            Assert.That(b.TryDeployRole(b.RunId,1,new Vector3(50,0,50),"CARD_M05").Failure,Is.EqualTo(DeployFailure.WrongEntry));
            Assert.That(b.DP,Is.EqualTo(8));Assert.That(b.CardCooldown("CARD_M05"),Is.Zero);
            Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);b.Advance(1);Assert.That(b.Defender.ArmorReduction,Is.Zero);
            Assert.That(b.TryDeployRole(b.RunId,2,b.Lanes[1].Entry,"CARD_M05").Success,Is.True);b.Advance(1);
            Assert.That(b.Defender.ArmorReduction,Is.GreaterThanOrEqualTo(2));
            Assert.That(b.TryDeployRole(b.RunId,3,b.Entry,"missing").Failure,Is.EqualTo(DeployFailure.MissingCard));
        }
    }
}
