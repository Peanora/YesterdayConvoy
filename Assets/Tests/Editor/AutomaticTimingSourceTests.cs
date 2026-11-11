using System;
using System.Linq;
using NUnit.Framework;
namespace YesterdayConvoy.Editor.Tests
{
    public sealed class AutomaticTimingSourceTests
    {
        static AutomaticMonsterTiming Skill(string card,int phase,string id,int region=1)=>AutomaticMonsterTimings.For(card,phase,region).Single(t=>t.Id==id);
        [Test]public void OriginalAttackRecoveryCountsNeverBecomeSecondsOrTrialEightSp()
        {
            foreach(var sample in new[]{("CARD_B08",1,"grantFire",10),("CARD_B08",3,"grantFire",5),("CARD_B25",1,"judgement",5),("CARD_B25",2,"judgement",4)})
            {var t=Skill(sample.Item1,sample.Item2,sample.Item3);Assert.That(t.AttackSP,Is.EqualTo(sample.Item4));Assert.That(t.SPCost,Is.EqualTo(sample.Item4));Assert.That(t.InitialSP,Is.Zero);Assert.That(t.Initial,Is.Zero);Assert.That(t.Interval,Is.Zero);}
            var potion=Skill("CARD_B10",1,"potion");Assert.That(potion.AttackSP,Is.Zero);Assert.That(potion.SPCost,Is.EqualTo(23));Assert.That(potion.InitialSP,Is.Zero);Assert.That(potion.Initial,Is.EqualTo(23));
        }
        [Test]public void SourceSlashValuesAndSourcePhaseOnlySkillsRemainDistinct()
        {
            Assert.That(Skill("CARD_B07",1,"sacrifice").Interval,Is.EqualTo(70));Assert.That(Skill("CARD_B07",2,"sacrifice").Interval,Is.EqualTo(60));
            Assert.That(Skill("CARD_B07",1,"dominate").Initial,Is.EqualTo(5));Assert.That(Skill("CARD_B07",2,"dominate").Initial,Is.Zero);
            Assert.That(Skill("CARD_B08",1,"blind").Initial,Is.EqualTo(30));Assert.That(Skill("CARD_B08",3,"blind").Initial,Is.EqualTo(40));
            Assert.That(AutomaticMonsterTimings.For("CARD_B08",2),Is.Empty);Assert.That(AutomaticMonsterTimings.For("CARD_B13",1),Is.Empty);Assert.That(AutomaticMonsterTimings.For("CARD_B22",1),Is.Empty);
            Assert.That(Skill("CARD_B15",2,"sow").SourceStatus,Is.EqualTo(AutomaticTimingSourceStatus.Adapted));
            Assert.That(Skill("CARD_B23",1,"seek").Condition,Is.EqualTo(AutomaticSkillCondition.CannonAvailable));Assert.That(AutomaticMonsterTimings.For("CARD_B23",2).Any(t=>t.Id=="seek"),Is.False);
        }
        [Test]public void AmiyaCounterAndOneCastPerOriginalModeAreNotFabricatedPeriodicCooldowns()
        {
            foreach(int region in new[]{1,2,3}){var t=Skill("CARD_B12",1,"survival",region);Assert.That(t.SPCost,Is.EqualTo(180));Assert.That(t.MaximumCasts,Is.EqualTo(1));Assert.That(t.StartsAfterAction,Is.True);}
            Assert.That(Skill("CARD_B12",2,"survival").SPCost,Is.EqualTo(280));Assert.That(AutomaticMonsterTimings.For("CARD_B12",1,4),Is.Empty);
            var crown=Skill("CARD_B12",2,"blackCrown");Assert.That(crown.Initial,Is.Zero);Assert.That(crown.Interval,Is.Zero);Assert.That(crown.SPCost,Is.Zero);Assert.That(crown.Condition,Is.EqualTo(AutomaticSkillCondition.BlackCrownCharge));
        }
        [Test]public void SuiUsesItsActualTwentyOfFiftySpAndKeepsCompositeHitsOutOfTheScheduler()
        {
            var t=Skill("CARD_B21",1,"insomnia");Assert.That(t.InitialSP,Is.EqualTo(20));Assert.That(t.SPCost,Is.EqualTo(50));Assert.That(t.RecoveryRate,Is.EqualTo(1));Assert.That(t.Initial,Is.EqualTo((t.SPCost-t.InitialSP)/t.RecoveryRate));Assert.That(t.Condition,Is.EqualTo(AutomaticSkillCondition.AwayModeTarget));Assert.That(t.StartsAfterAction,Is.True);
            Assert.That(AutomaticMonsterTimings.For("CARD_B21",1).Any(s=>s.Id=="mountains"||s.Id=="awaken"),Is.False);
        }
        [Test]public void ContractsCoverEveryNewCardWithoutThresholdLoopsDuplicateIdsOrTextureReads()
        {
            int loads=ArtMemoryCache.TextureLoads;string[] tasks={"B01","B03","B04","B05","B07","B08","B09","B10","B11","B12","B13","B14","B15","B21","B22","B23","B24","B25","B26"};
            string[] eventOnly={"rage","flee","killWindow","pierce","obituary","deathKnock","awaken","annihilation","folly","mountains","sacrament","echoes","penalty","absorb","deployMarked","domainBurst","eternal"};
            foreach(var task in tasks)
            {
                int count=0;for(int phase=1;phase<=3;phase++)
                {var rows=AutomaticMonsterTimings.For("CARD_"+task,phase);count+=rows.Count;Assert.That(rows.Select(t=>t.Id).Distinct().Count(),Is.EqualTo(rows.Count));foreach(var t in rows){Assert.That(eventOnly,Does.Not.Contain(t.Id));Assert.That(t.Initial,Is.GreaterThanOrEqualTo(0));Assert.That(t.Interval,Is.GreaterThanOrEqualTo(0));Assert.That(t.Interval>0||t.AttackSP>0||t.Condition==AutomaticSkillCondition.BlackCrownCharge,Is.True);}}
                Assert.That(count,Is.GreaterThan(0),task);
            }
            foreach(var core in new[]{"CARD_M25","CARD_M32","CARD_TALULAH_TRIAL","CARD_M20","CARD_M14","CARD_M28","CARD_WISADEL_APRIL"})Assert.That(AutomaticMonsterTimings.For(core,1),Is.Empty,core);
            Assert.That(AutomaticMonsterTimings.For("CARD_B01",0),Is.Empty);Assert.That(AutomaticMonsterTimings.For(null,1),Is.Empty);Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }
    }
}
