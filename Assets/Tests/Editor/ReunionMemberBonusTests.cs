using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    // Isolated combat/transaction fixtures; these do not claim a natural campaign clear.
    public sealed class ReunionMemberBonusTests
    {
        const string Talulah="CARD_TALULAH_TRIAL";
        static readonly string[] Members={Talulah,"CARD_M07","CARD_M08","CARD_M09","CARD_M15","CARD_M16","CARD_M11","CARD_M21"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,10,20,2,.1f,1000,.1f,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:100,unitArmor:0,unitRange:3,unitInterval:100,
            defenderHealth:1000000,defenderAttack:1,defenderArmor:0,defenderRange:.001f,defenderInterval:100,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
            projectileTime:.05f,hasSkill:false,attackDuration:.3f,attackWindup:.1f);
        static ManualBattle Make(string[] ids,bool v2=true,InfectionForm form=InfectionForm.Swarm,string contract=null,bool configure=true)
        {
            var guard=new DefenderSettings("guard","Guard",null,Vector3.right*.5f,1000000,1,0,.001f,100,canAttack:false);
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.right*10000},guard.Position,Combat(),roster:new[]{guard});
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(),true)));
            if(v2)b.ConfigureCampaignV2();
            if(configure)b.ConfigureSynergies(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),form:form,contract:contract);
            b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,long request=1,bool paused=false)
        {
            var receipt=b.TryDeployRole(b.RunId,request,b.Entry,id,paused);
            Assert.That(receipt.Success,Is.True,receipt.Failure.ToString());return b.FindUnit(receipt.EntityId);
        }

        [TestCase(2,true,1100,110)] [TestCase(4,true,1200,120)]
        [TestCase(6,true,1300,130)] [TestCase(8,true,1400,140)]
        [TestCase(2,false,1100,110)] [TestCase(4,false,1200,120)]
        [TestCase(6,false,1300,130)] [TestCase(8,false,1400,140)]
        public void LockedFormationBoostsTalulahMaximumHealthAndRealOrdinaryDamage(int count,bool v2,float hp,float damage)
        {
            var b=Make(Members.Take(count).ToArray(),v2);var unit=Deploy(b,Talulah);
            Assert.That(b.ReunionTier,Is.EqualTo(count));Assert.That(unit.Health,Is.EqualTo(hp).Within(.001f));
            Assert.That(unit.MaxHealth,Is.EqualTo(hp).Within(.001f));b.Advance(.5f);
            var hit=b.CombatShots.Single(s=>s.SourceId==unit.EntityId&&s.Applied&&!s.FromDefender);
            Assert.That(hit.DamageDealt,Is.EqualTo(damage).Within(.01f));
            Assert.That(b.Defender.Health,Is.EqualTo(1000000-damage).Within(.01f));
        }

        [Test]public void EnoughMembersWithoutTheRequiredFormationsUseOnlyTheFallbackBonus()
        {
            var guerrilla=new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20"};
            var b=Make(guerrilla);var unit=Deploy(b,"CARD_M15");
            Assert.That(b.ReunionTier,Is.EqualTo(4));Assert.That(unit.MaxHealth,Is.EqualTo(1200).Within(.001f));
            Assert.That(b.UnitAttackMultiplier(unit),Is.EqualTo(1.2f).Within(.001f));
        }

        [TestCase(true,100,16)] [TestCase(false,80,14)]
        public void MusterSupportAndNonMembersDoNotReceiveAnotherMemberBonus(bool v2,float supportHP,float supportAttack)
        {
            var b=Make(new[]{"CARD_M07","CARD_M08","CARD_M37"},v2);
            Deploy(b,"CARD_M07");Deploy(b,"CARD_M08",2);var outsider=Deploy(b,"CARD_M37",3);
            var support=b.Units.Single(u=>u.SynergySupport);
            Assert.That(support.MaxHealth,Is.EqualTo(supportHP).Within(.001f));
            Assert.That(b.UnitCombat(support).UnitAttack*b.UnitAttackMultiplier(support),Is.EqualTo(supportAttack).Within(.001f));
            Assert.That(outsider.MaxHealth,Is.EqualTo(1000));Assert.That(b.UnitAttackMultiplier(outsider),Is.EqualTo(1));
        }

        [Test]public void MissingOrInactiveSynergyDoesNotChangeAnOtherwiseIdenticalProfile()
        {
            var unconfigured=Make(new[]{"CARD_M07","CARD_M08"},configure:false);
            var single=Make(new[]{Talulah});
            foreach(var b in new[]{unconfigured,single})
            {
                var unit=Deploy(b,b.Settings.CardId);Assert.That(b.ReunionTier,Is.Zero);
                Assert.That(unit.MaxHealth,Is.EqualTo(1000));Assert.That(b.UnitAttackMultiplier(unit),Is.EqualTo(1));
            }
        }

        [Test]public void PausedAndRepeatedDeploymentsAndRestartNeverStackOrRefillTheBonus()
        {
            var b=Make(new[]{"CARD_M07","CARD_M08"});b.SetPaused(true);var first=Deploy(b,"CARD_M07",paused:true);
            double clock=b.Clock;int dp=b.DP,uses=b.FindCard("CARD_M07").RemainingUses;
            b.Advance(30);var duplicate=Deploy(b,"CARD_M07",paused:true);
            Assert.That(duplicate.EntityId,Is.EqualTo(first.EntityId));Assert.That(b.Clock,Is.EqualTo(clock));
            Assert.That(b.DP,Is.EqualTo(dp));Assert.That(b.FindCard("CARD_M07").RemainingUses,Is.EqualTo(uses));
            b.SetPaused(false);Assert.That(b.TryDamageUnit(first.EntityId,10,CombatDamageType.Arts),Is.True);
            Deploy(b,"CARD_M07");Assert.That(first.Health,Is.EqualTo(1090).Within(.001f),"Replaying an accepted request cannot heal.");
            b.Advance(.11f);var second=Deploy(b,"CARD_M07",2);
            Assert.That(second.MaxHealth,Is.EqualTo(1100).Within(.001f));Assert.That(first.MaxHealth,Is.EqualTo(second.MaxHealth));
            Assert.That(b.TryDamageUnit(first.EntityId,100000,CombatDamageType.Arts),Is.True);
            Assert.That(b.ReunionTier,Is.EqualTo(2),"Death does not recalculate the locked formation.");
            int oldRun=b.RunId;b.Restart();b.Begin();var fresh=Deploy(b,"CARD_M07");
            Assert.That(b.RunId,Is.Not.EqualTo(oldRun));Assert.That(fresh.MaxHealth,Is.EqualTo(1100).Within(.001f));
            Assert.That(b.UnitAttackMultiplier(fresh),Is.EqualTo(1.1f).Within(.001f));
        }

        [TestCase(true,1180,1.35f)] [TestCase(false,1200,1.45f)]
        public void SarkazAndReunionMemberAdditionsShareOneAdditiveStatBudget(bool v2,float hp,float attackMultiplier)
        {
            var b=Make(new[]{"CARD_M19","CARD_M20"},v2,contract:"CARD_M19");var unit=Deploy(b,"CARD_M19");
            Assert.That(b.ReunionTier,Is.EqualTo(2));Assert.That(b.SarkazTier,Is.EqualTo(2));
            Assert.That(unit.MaxHealth,Is.EqualTo(hp).Within(.001f));
            Assert.That(b.UnitAttackMultiplier(unit),Is.EqualTo(attackMultiplier).Within(.001f));
        }

        [Test]public void InfectionCopiesInheritTheAlreadyScaledHealthOnceButCannotAddTheirOwnBonus()
        {
            var b=Make(new[]{"CARD_M02","CARD_M03","CARD_M04","CARD_M05"});var primary=Deploy(b,"CARD_M02");
            Assert.That(b.InfectionTier,Is.EqualTo(4));Assert.That(b.ReunionTier,Is.EqualTo(2));
            var secondary=b.Units.Single(u=>u.SynergySupport);
            Assert.That(primary.MaxHealth,Is.EqualTo(715).Within(.001f));Assert.That(secondary.MaxHealth,Is.EqualTo(715).Within(.001f));
            Assert.That(b.UnitCombat(primary).UnitAttack*b.UnitAttackMultiplier(primary),Is.EqualTo(71.5f).Within(.001f));
            Assert.That(b.UnitCombat(secondary).UnitAttack*b.UnitAttackMultiplier(secondary),Is.EqualTo(65).Within(.001f));
        }

        [Test]public void HerdSpiritKeepsItsInheritedHealthFractionWithoutAnotherMemberBonus()
        {
            var b=Make(new[]{"CARD_M21","CARD_M22"});var primary=Deploy(b,"CARD_M21");
            Assert.That(primary.MaxHealth,Is.EqualTo(1100).Within(.001f));
            Assert.That(b.TryDamageUnit(primary.EntityId,100000,CombatDamageType.Arts),Is.True);b.Advance(.01f);
            var spirit=b.Units.Single(u=>u.SynergySupport);
            Assert.That(spirit.MaxHealth,Is.EqualTo(330).Within(.001f));
            Assert.That(b.UnitCombat(spirit).UnitAttack*b.UnitAttackMultiplier(spirit),Is.EqualTo(30).Within(.001f));
        }

        static ManualBattle ProductionTalulah(int star,bool v2,out BasicRosterProfile profile)
        {
            var data=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset");
            Assert.That(data,Is.Not.Null);
            profile=v2?CampaignV2Balance.Build(data,star,100,10):CampaignBalance.Build(data,star,100,10);
            var guard=new DefenderSettings("guard","Guard",null,Vector3.right*500,1000000,1,0,.001f,100,canAttack:false);
            var b=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.right*10000},guard.Position,profile.Combat,roster:new[]{guard});
            b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Unit("CARD_M07"),Combat(),true)});
            if(v2)b.ConfigureCampaignV2();
            b.ConfigureSynergies(new[]{new ShopTrialModel.Piece{Id=Talulah,Star=star},new ShopTrialModel.Piece{Id="CARD_M07",Star=1}});
            b.Begin();return b;
        }

        [TestCase(true)] [TestCase(false)]
        public void RealThreeStarTalulahRestoresHerSecondLifeWithOneBonusAndAutomaticRainUsesAttackModifiers(bool v2)
        {
            var b=ProductionTalulah(3,v2,out var profile);var unit=Deploy(b,Talulah);
            Assert.That(unit.MaxHealth,Is.EqualTo(profile.Unit.Health*1.1f).Within(.01f));
            Assert.That(b.TryDamageUnit(unit.EntityId,1000000,CombatDamageType.Arts),Is.True);b.Advance(7.01f);
            Assert.That(unit.TalulahAutomaticWavesFired,Is.EqualTo(1));
            float wave=v2?CampaignV2Balance.SkillValue(Talulah,3,"threeStarPhase","firstRainDamagePerTargetPerWave"):profile.Combat.UnitAttack;
            Assert.That(1000000-b.Defender.Health,Is.EqualTo(wave*1.1f).Within(.1f));
            b.Advance(26);Assert.That(unit.TalulahPhase,Is.EqualTo(TalulahPhase.Second));
            float secondHP=v2?CampaignV2Balance.SkillValue(Talulah,3,"threeStarPhase","secondFormHp"):profile.Unit.Health*1.5f;
            Assert.That(unit.MaxHealth,Is.EqualTo(secondHP*1.1f).Within(.01f));Assert.That(unit.Health,Is.EqualTo(unit.MaxHealth));
            Assert.That(b.UnitAttackMultiplier(unit),Is.EqualTo(1.65f).Within(.001f));
        }

        [Test]public void ProductionManualFireRainKeepsItsFixedSixWaveBudget()
        {
            var b=ProductionTalulah(1,true,out var profile);var unit=Deploy(b,Talulah);
            float charge=Math.Max(0,profile.Combat.RequiredSP-profile.Combat.InitialSP)/profile.Combat.SPPerSecond;
            b.Advance(charge+.1f);Assert.That(b.TryActivateSkill(b.RunId,100,unit.EntityId),Is.EqualTo(SkillFailure.None));
            b.Advance(profile.Combat.SkillInterval+profile.Combat.ProjectileTime+.1f);
            var waves=b.CombatShots.Where(s=>s.SourceId==unit.EntityId&&s.FireRain&&s.Applied).ToArray();
            Assert.That(waves.Length,Is.EqualTo(6));Assert.That(waves.All(s=>Math.Abs(s.Attack-profile.Combat.SkillAttack)<.001f),Is.True);
            Assert.That(waves.Sum(s=>s.DamageDealt),Is.EqualTo(profile.Combat.SkillAttack*6).Within(.1f));
        }

        [TestCase(true)] [TestCase(false)]
        public void MemberDetailsShowEveryApprovedTierAndExplainTheSupportBoundary(bool v2)
        {
            var summary=SynergySummary.From(Members,campaignV2:v2).Single(s=>s.Name=="整合运动");
            var detail=SynergyDetailUiModel.Build(summary,v2,CampaignBalance.Profiles.Keys);
            foreach(var tier in detail.tiers)
            {
                Assert.That(tier.text,Does.Contain("＋"+(tier.tier*5)+"%"),tier.text);
                Assert.That(tier.text,Does.Contain("成员"),tier.text);
            }
            Assert.That(summary.Effect,Does.Contain("五费"));Assert.That(summary.Effect,Does.Contain("不额外叠加"));
            Assert.That(detail.tiers.Count(t=>t.active),Is.EqualTo(1));Assert.That(detail.tiers.Single(t=>t.active).tier,Is.EqualTo(8));
        }
    }
}
