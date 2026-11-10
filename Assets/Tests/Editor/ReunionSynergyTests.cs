using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class ReunionSynergyTests
    {
        static readonly string[] Full = { "CARD_M07", "CARD_M08", "CARD_M09", "CARD_M10", "CARD_M15", "CARD_M16", "CARD_M11", "CARD_M21" };
        static readonly string[] Guerrilla = { "CARD_M03", "CARD_M15", "CARD_M16", "CARD_M17", "CARD_M18", "CARD_M19", "CARD_M20" };
        static ManualBattleSettings Settings(string id) => new ManualBattleSettings(id,id,100,10,20,2,.1f,10000,.01f,1,1,1,1);
        static EngagementSettings Combat() => new EngagementSettings(unitAttack:1,unitRange:.01f,defenderHealth:1000000,
            defenderAttack:1,defenderRange:.01f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,
            skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
        static ManualBattle Battle(string[] ids, bool ranged=false, int star=1)
        {
            var b=new ManualBattle(Settings(ids[0]),new[]{Vector3.zero,Vector3.right*200},Vector3.right*150,Combat());
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Settings(id),Combat(),true)));
            b.ConfigureSynergies(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=star}),ranged);
            b.Begin();return b;
        }
        static void Deploy(ManualBattle b,string id,ref long request)
        {
            Assert.That(b.TryDeployRole(b.RunId,++request,b.Entry,id).Success,Is.True,id);
            b.Advance(.11f);
        }

        [Test] public void ExistingRosterKeepsTagsButHasOneGameplayFormationPerKind()
        {
            var ids=CampaignBalance.Profiles.Keys.Where(CampaignBalance.Reunion).ToArray();
            CollectionAssert.AreEquivalent(new[]{6,5,7,4},Enumerable.Range(1,4).Select(g=>ids.Count(id=>ReunionRules.FormationOf(id)==g)));
            Assert.That(ReunionRules.FormationOf("CARD_M03"),Is.EqualTo(3));
            Assert.That(ReunionRules.FormationOf("CARD_M06"),Is.EqualTo(2));
            Assert.That(ReunionRules.FormationOf("CARD_M24"),Is.Zero,"Herd membership alone does not grant Reunion.");
            Assert.That(CampaignBalance.Tags("CARD_M03"),Does.Contain("感染生物"));
        }

        [Test] public void HighTiersRequireFormationsRatherThanOnlyEnoughPeople()
        {
            Assert.That(ReunionRules.TierFor(Guerrilla),Is.EqualTo(4));
            Assert.That(ReunionRules.TierFor(Guerrilla.Take(6).Concat(new[]{"CARD_M11","CARD_M12"})),Is.EqualTo(6));
            Assert.That(ReunionRules.TierFor(Guerrilla.Take(6).Concat(new[]{"CARD_M11","CARD_M21"})),Is.EqualTo(8));
            Assert.That(ReunionRules.TierFor(Full.Concat(Full)),Is.EqualTo(8),"Repeated kind IDs do not add people or formations.");
        }

        [TestCase(2,1,1,4)]
        [TestCase(4,2,2,12)]
        [TestCase(6,2,4,12)]
        [TestCase(8,3,6,18)]
        public void PaidDeploymentsProduceOnlyTheFiniteTierBudget(int tier,int rounds,int supports,int spent)
        {
            var b=Battle(Full.Take(tier).ToArray());long request=0;
            var cycle=Full.Take(ReunionRules.KindsPerMuster(tier)).ToArray();
            for(int round=0;round<rounds;round++)foreach(string id in cycle)Deploy(b,id,ref request);
            Assert.That(b.ReunionTier,Is.EqualTo(tier));Assert.That(b.MusterUsed,Is.EqualTo(rounds));
            Assert.That(b.Units.Count(u=>u.SynergySupport),Is.EqualTo(supports));Assert.That(b.DP,Is.EqualTo(100-spent));
            Assert.That(b.MusterMemberIds,Is.Empty);
            foreach(string id in cycle)Deploy(b,id,ref request);
            Assert.That(b.Units.Count(u=>u.SynergySupport),Is.EqualTo(supports));
            Assert.That(b.Units.Where(u=>u.SynergySupport).All(u=>u.RouteId==b.Lanes[0].Id),Is.True);
        }

        [Test] public void CanceledOutsideDuplicateAndRepeatedKindDeploymentsCannotFillMuster()
        {
            var b=Battle(Full.Take(4).Concat(new[]{"CARD_M05"}).ToArray());long request=0;
            Assert.That(b.TryDeployRole(b.RunId,++request,Vector3.back*100,"CARD_M07").Success,Is.False);
            Assert.That(b.MusterMemberIds,Is.Empty);
            Deploy(b,"CARD_M05",ref request);Assert.That(b.MusterMemberIds,Is.Empty);
            long committed=++request;
            Assert.That(b.TryDeployRole(b.RunId,committed,b.Entry,"CARD_M07").Success);
            Assert.That(b.TryDeployRole(b.RunId,committed,b.Entry,"CARD_M07").Success);
            b.Advance(.11f);Deploy(b,"CARD_M07",ref request);
            CollectionAssert.AreEquivalent(new[]{"CARD_M07"},b.MusterMemberIds);Assert.That(b.MusterUsed,Is.Zero);
            Deploy(b,"CARD_M08",ref request);Deploy(b,"CARD_M09",ref request);
            Assert.That(b.MusterUsed,Is.EqualTo(1));Assert.That(b.MusterMemberIds,Is.Empty);
            Deploy(b,"CARD_M07",ref request);Deploy(b,"CARD_M08",ref request);
            Assert.That(b.MusterUsed,Is.EqualTo(1),"The previous trigger did not carry over to the next wave.");
        }

        [Test] public void PauseAndRestartPreserveSnapshotButResetMusterAndEntities()
        {
            var b=Battle(Full);b.SetPaused(true);
            for(int i=0;i<3;i++)Assert.That(b.TryDeployRole(b.RunId,i+1,b.Entry,Full[i],true).Success);
            Assert.That(b.MusterUsed,Is.EqualTo(1));Assert.That(b.DP,Is.EqualTo(94));
            Assert.That(b.Units.Where(u=>u.SynergySupport).All(u=>u.ReunionEntryShieldPending&&u.SynergyShield==0));
            b.Advance(20);Assert.That(b.Clock,Is.Zero);Assert.That(b.ReunionTier,Is.EqualTo(8));
            int previousRun=b.RunId;b.Restart();Assert.That(b.RunId,Is.Not.EqualTo(previousRun));
            Assert.That(b.ReunionTier,Is.EqualTo(8));Assert.That(b.MusterUsed,Is.Zero);Assert.That(b.MusterMemberIds,Is.Empty);
            Assert.That(b.Units,Is.Empty);Assert.That(b.DP,Is.EqualTo(100));
        }

        [Test] public void FourTierChoiceAndTotalStarsCreateTheSpecifiedSupportProfiles()
        {
            var b=Battle(Full.Take(4).ToArray(),true,2);long request=0;
            foreach(string id in Full.Take(3))Deploy(b,id,ref request);
            var support=b.Units.Single(u=>u.SynergySupport);
            Assert.That(b.ReunionTotalStars,Is.EqualTo(8));
            Assert.That(support.CardId,Is.EqualTo("CARD_M08"));Assert.That(support.MaxHealth,Is.EqualTo(127));
            Assert.That(support.Profile.Combat.UnitAttack,Is.EqualTo(34));Assert.That(support.Profile.Combat.UnitArmor,Is.Zero);
            Assert.That(support.ReunionEliteSupport,Is.False);
            var half=ReunionRules.ForSupport(6,true,1.5f);
            Assert.That(half.Health,Is.EqualTo(135));Assert.That(half.Attack,Is.EqualTo(37));
            Assert.That(half.Armor,Is.EqualTo(1));
        }
    }
}
