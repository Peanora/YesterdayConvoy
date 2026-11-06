using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MudrockGrowthTests
    {
        static readonly string[] Crew={"CARD_M26","CARD_M27","CARD_M42","CARD_M43","CARD_M28"};
        static ShopTrialModel.Piece P(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        static ManualBattle Fixture(ShopTrialModel.Piece[] pieces,int xp=0,string[] transfers=null,bool begin=true)
        {
            ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,5,8,2,.1f,200,.5f,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:1,unitRange:3,unitInterval:2,defenderHealth:10000,defenderAttack:1,
                defenderRange:.1f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            var ids=pieces.Select(p=>p.Id).Distinct().ToArray();
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.forward*50},Vector3.right*.5f,combat);
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),combat,true)));
            b.ConfigureSynergies(pieces,golemExperience:xp,extraMudrockMembers:transfers);if(begin)b.Begin();return b;
        }
        static void Finish(ManualBattle b,bool won)=>typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{b.RunId,won});
        static ShopTrialModel Economy()
        {
            var shop=new ShopTrialModel(Crew.Select(id=>new ShopTrialModel.Role{Id=id,Name=id,Cost=CampaignBalance.Profiles[id].Cost}));
            shop.EnableCampaign();shop.Preset(Crew.Select(id=>P(id)),Array.Empty<string>(),8,0,40);shop.SetLineup(Crew);return shop;
        }

        [TestCase(0,1,1)][TestCase(5,5,1)][TestCase(6,5,2)][TestCase(15,5,2)]
        [TestCase(16,5,3)][TestCase(29,5,3)][TestCase(30,5,4)][TestCase(47,5,4)]
        [TestCase(48,5,5)][TestCase(100,2,2)][TestCase(48,3,3)]
        public void ExperienceLevelsRespectThresholdsAndCurrentFormationCapacity(int xp,int capacity,int stage)
            =>Assert.That(GolemGrowth.Stage(xp,capacity),Is.EqualTo(stage));

        [TestCase(0,0,0,1)][TestCase(1,3,0,1)][TestCase(2,2,2,2)][TestCase(3,9,2,2)]
        [TestCase(4,6,4,2)][TestCase(4,7,4,3)][TestCase(5,9,5,3)][TestCase(5,10,5,4)]
        [TestCase(6,6,6,2)][TestCase(6,11,6,4)][TestCase(6,12,6,5)]
        public void MemberCountAndTotalStarsSetSeparateTierAndGrowthCapacity(int members,int stars,int tier,int capacity)
        {
            Assert.That(GolemGrowth.TierForCount(members),Is.EqualTo(tier));
            Assert.That(GolemGrowth.Capacity(members,stars),Is.EqualTo(capacity));
        }
        [TestCase(0,3,0)][TestCase(2,2,3)][TestCase(2,5,4)][TestCase(4,7,7)][TestCase(5,10,10)][TestCase(6,12,12)][TestCase(6,18,15)]
        public void AutomaticBattleExperienceUsesTheTierAndTotalStars(int tier,int stars,int expected)
            =>Assert.That(GolemGrowth.BattleXP(tier,stars),Is.EqualTo(expected));

        [Test]public void PartialAwardsStopAtCurrentCapWithoutSavingOverflowOrReducingHistory()
        {
            Assert.That(GolemGrowth.Award(5,2,3),Is.EqualTo(1));
            Assert.That(GolemGrowth.Award(6,2,3),Is.Zero);
            Assert.That(GolemGrowth.Stage(6,3),Is.EqualTo(2),"Raising capacity alone cannot grant experience.");
            Assert.That(GolemGrowth.Award(6,3,7),Is.EqualTo(7));
            Assert.That(GolemGrowth.Award(13,3,7),Is.EqualTo(3));
            Assert.That(GolemGrowth.Award(48,2,3),Is.Zero,"Temporarily lowering capacity never subtracts historical XP.");
            Assert.That(GolemGrowth.Stage(48,5),Is.EqualTo(5));
        }
        [Test]public void NegativeExperienceAndInvalidCapacityCannotCreateGrowth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>GolemGrowth.Award(-1,2,3));
            Assert.Throws<ArgumentOutOfRangeException>(()=>GolemGrowth.Award(0,2,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>GolemGrowth.Award(0,6,3));
        }
        [Test]public void ThreeStarMemberCanCompensateOneStarAndSnapshotsDeduplicateByKind()
        {
            var pieces=Crew.Select((id,i)=>P(id,i==0?3:i==4?1:2)).ToArray();
            var b=Fixture(pieces,30);
            Assert.That(b.MudrockCrewCount,Is.EqualTo(5));Assert.That(b.MudrockTotalStars,Is.EqualTo(10));
            Assert.That(b.GolemCapacity,Is.EqualTo(4));Assert.That(b.GolemStage,Is.EqualTo(4));
            var duplicate=Fixture(new[]{P(Crew[0]),P(Crew[0],3),P(Crew[1])});
            Assert.That(duplicate.MudrockCrewCount,Is.EqualTo(2));Assert.That(duplicate.MudrockTotalStars,Is.EqualTo(4));
            Assert.That(duplicate.GolemBattleExperience,Is.EqualTo(4));
        }
        [TestCase(true)][TestCase(false)]public void EmptyBattleAutomaticallyAwardsGrowthOnEitherTerminalResult(bool won)
        {
            var b=Fixture(Crew.Take(2).Select(id=>P(id)).ToArray());
            Assert.That(b.Units,Is.Empty);Assert.That(b.GolemGrowthEarned,Is.Zero);
            Finish(b,won);Assert.That(b.GolemGrowthEarned,Is.EqualTo(3));
            Assert.That(b.Units,Is.Empty,"The award must not acquire a hidden deployment/contact requirement.");
        }
        [Test]public void InvalidTransferSnapshotsAreRejectedBeforeChangingAnyPreparedGrowthState()
        {
            var original=Crew.Take(2).Select(id=>P(id)).ToArray();var b=Fixture(original,begin:false);
            var proposed=original.Concat(new[]{P("CARD_M07"),P("CARD_M08")}).ToArray();
            Assert.Throws<ArgumentException>(()=>b.ConfigureSynergies(proposed,golemExperience:48,extraMudrockMembers:new[]{"CARD_M07","CARD_M08"}));
            Assert.That(b.GolemExperience,Is.Zero);Assert.That(b.MudrockCrewCount,Is.EqualTo(2));Assert.That(b.MudrockTotalStars,Is.EqualTo(2));
            Assert.Throws<ArgumentException>(()=>b.ConfigureSynergies(original,extraMudrockMembers:new[]{"CARD_M07"}));
            Assert.That(b.GolemCapacity,Is.EqualTo(2));Assert.That(b.IsMudrockMember("CARD_M07"),Is.False);
        }
        [Test]public void DeathAndAdditionalCopiesDoNotChangeThePreparedMemberStarsOrCapacity()
        {
            var b=Fixture(Crew.Select(id=>P(id,2)).Concat(new[]{P("CARD_M07",2)}).ToArray(),48,new[]{"CARD_M07"});
            Assert.That(b.MudrockTier,Is.EqualTo(6));Assert.That(b.MudrockCrewCount,Is.EqualTo(6));Assert.That(b.MudrockTotalStars,Is.EqualTo(12));
            Assert.That(b.GolemCapacity,Is.EqualTo(5));Assert.That(b.GolemStage,Is.EqualTo(5));
            var first=b.TryDeployRole(b.RunId,1,b.Entry,Crew[0]);Assert.That(first.Success,Is.True);
            b.Advance(.2f);Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,Crew[0]).Success,Is.True);
            Assert.That(b.TryDamageUnit(first.EntityId,10000,CombatDamageType.Arts),Is.True);
            Assert.That(b.MudrockCrewCount,Is.EqualTo(6));Assert.That(b.MudrockTotalStars,Is.EqualTo(12));
            Assert.That(b.GolemCapacity,Is.EqualTo(5));Assert.That(b.GolemBattleExperience,Is.EqualTo(12));
        }
        [Test]public void SupplyAndInterruptedBattleAwardNothingAndAUniqueTerminalResultSettlesOnlyOnce()
        {
            var run=new BattleShopRun(Economy(),92,campaign:true);
            Assert.That(run.RestartEncounter(),Is.True);Assert.That(run.GolemExperience,Is.Zero);
            for(int supply=0;supply<2;supply++)
            {
                Assert.That(run.RewardStage,Is.True);Assert.That(run.Observe(BattlePhase.Won,12,golemCapacity:5),Is.True);
                Assert.That(run.LastGolemGrowth,Is.Zero);Assert.That(run.GolemExperience,Is.Zero);
                Assert.That(run.OpenShop(),Is.True);Assert.That(run.PrepareBattle(),Is.True);
            }
            Assert.That(run.RewardStage,Is.False);Assert.That(run.Observe(BattlePhase.Won,3,golemCapacity:2),Is.True);
            Assert.That(run.LastGolemGrowth,Is.EqualTo(3));Assert.That(run.GolemExperience,Is.EqualTo(3));
            Assert.That(run.Observe(BattlePhase.Won,3,golemCapacity:2),Is.False);Assert.That(run.GolemExperience,Is.EqualTo(3));
            Assert.That(new BattleShopRun(Economy(),93,campaign:true).GolemExperience,Is.Zero);
        }
        [Test]public void PreparedEarlyGrowthLeavesBothUltimateStagesPlayableBeforeTheFinalBattle()
        {
            int xp=0;
            // Only actual battle nodes award XP: 3,4,6,7,8,10. Supply nodes are excluded.
            foreach(var award in new[]{new[]{2,2},new[]{2,2},new[]{4,7},new[]{6,12},new[]{6,12}})
                xp+=GolemGrowth.Award(xp,GolemGrowth.Capacity(award[0],award[1]),GolemGrowth.BattleXP(GolemGrowth.TierForCount(award[0]),award[1]));
            Assert.That(xp,Is.EqualTo(37));Assert.That(GolemGrowth.Stage(xp,5),Is.EqualTo(4),"Original ultimate is available for encounter 10.");
            xp+=GolemGrowth.Award(xp,5,12);Assert.That(xp,Is.EqualTo(48));
            Assert.That(GolemGrowth.Stage(xp,5),Is.EqualTo(5),"Six-member ultimate is available for encounters 11 and 13.");
        }
    }
}
