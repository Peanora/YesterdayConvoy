using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class ShopStarProfileTests
    {
        static ManualBattleDefinition Definition(string id)=>AssetDatabase.FindAssets("t:ManualBattleDefinition")
            .Select(g=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .First(d=>d!=null&&d.Card.CardId==id);

        [TestCase(false)]
        [TestCase(true)]
        public void OneStarLegacyBattleShopKeepsEveryMappedUnitAndAuthoredCombatValue(bool lateGame)
        {
            foreach(string id in new[]{"CARD_M08","CARD_M02","CARD_M17","CARD_M18","CARD_M05"})
            {
                var data=Definition(id);var unit=BattleShopStages.Settings(data.Snapshot(),lateGame);var combat=data.Engagement.Snapshot();
                var profile=ShopStarProfiles.Build(unit,combat,data.Card.RecruitCost,1);
                Assert.That(profile.Unit,Is.EqualTo(unit),id);
                Assert.That(profile.Combat,Is.EqualTo(combat),id);
            }
        }

        [Test] public void FourStarLegacyProfileGrowsTheMappedBaselineAndRetainsItsMapAndActionRules()
        {
            var data=Definition("CARD_M08");var mapped=StrongholdChallengeLayout.Settings(data.Snapshot());var authored=data.Engagement.Snapshot();
            var four=ShopStarProfiles.Build(mapped,authored,1,4);
            Assert.That(four.Unit.Health,Is.EqualTo(Mathf.Floor(mapped.Health*4.25f+.5f)));
            Assert.That(four.Combat.UnitAttack,Is.EqualTo(authored.UnitAttack*3.0f).Within(.001f));
            Assert.That(four.Unit.DeploymentCost,Is.EqualTo(mapped.DeploymentCost+3));
            Assert.That(four.Combat.UnitArmor,Is.EqualTo(authored.UnitArmor+6));
            Assert.That(four.Unit.DeploymentLimit,Is.EqualTo(mapped.DeploymentLimit));
            Assert.That(four.Unit.InitialDP,Is.EqualTo(mapped.InitialDP));Assert.That(four.Unit.InitialDefense,Is.EqualTo(mapped.InitialDefense));
            Assert.That(four.Unit.GateDamage,Is.EqualTo(mapped.GateDamage+1));
            Assert.That(four.Unit.Speed,Is.EqualTo(mapped.Speed));Assert.That(four.Unit.Cooldown,Is.EqualTo(mapped.Cooldown));
            Assert.That(four.Combat.UnitRange,Is.EqualTo(authored.UnitRange));Assert.That(four.Combat.UnitInterval,Is.EqualTo(authored.UnitInterval));
            Assert.That(four.Combat.AttackDuration,Is.EqualTo(authored.AttackDuration));Assert.That(four.Combat.AttackWindup,Is.EqualTo(authored.AttackWindup));
            Assert.That(four.Combat.ProjectileTime,Is.EqualTo(authored.ProjectileTime));
            Assert.That(four.Combat.DefenderHealth,Is.EqualTo(authored.DefenderHealth));Assert.That(four.Combat.DefenderAttack,Is.EqualTo(authored.DefenderAttack));
            Assert.That(four.Combat.BlockCount,Is.EqualTo(authored.BlockCount));
            Assert.That(data.Snapshot().Health,Is.LessThan(four.Unit.Health),"The authored configuration remains its unscaled baseline.");
        }

        [Test] public void LegacyShopRejectsFourthStarsForExpensiveRolesAndRetainsThreeStarBreachBonus()
        {
            var data=Definition("CARD_M05");var mapped=BattleShopStages.Settings(data.Snapshot());var combat=data.Engagement.Snapshot();
            Assert.Throws<ArgumentOutOfRangeException>(()=>ShopStarProfiles.Build(mapped,combat,2,4));
            var low=Definition("CARD_M08");var basis=BattleShopStages.Settings(low.Snapshot());
            Assert.That(ShopStarProfiles.Build(basis,low.Engagement.Snapshot(),1,3).Unit.GateDamage,
                Is.EqualTo(ShopStarProfiles.Build(basis,low.Engagement.Snapshot(),1,4).Unit.GateDamage));
        }

        [Test] public void LegacyThreeStarFireRainKeepsTheAuthoredCycleAndDealsScaledSkillDamage()
        {
            var data=Definition("CARD_TALULAH_TRIAL");var mapped=BattleShopStages.Settings(data.Snapshot(),true);var original=data.Engagement.Snapshot();
            var profile=ShopStarProfiles.Build(mapped,original,5,3);var combat=profile.Combat;
            Assert.That(combat.SkillAttack,Is.EqualTo(original.SkillAttack*5));
            Assert.That(combat.FireRain,Is.EqualTo(original.FireRain));Assert.That(combat.FireRain,Is.True);
            Assert.That(combat.SkillAmmo,Is.EqualTo(original.SkillAmmo));Assert.That(combat.SkillInterval,Is.EqualTo(original.SkillInterval));
            Assert.That(combat.InitialSP,Is.EqualTo(original.InitialSP));Assert.That(combat.RequiredSP,Is.EqualTo(original.RequiredSP));
            Assert.That(combat.SPPerSecond,Is.EqualTo(original.SPPerSecond));Assert.That(combat.SkillRange,Is.EqualTo(original.SkillRange));
            Assert.That(combat.Effects.TalulahSecondPhase,Is.EqualTo(original.Effects.TalulahSecondPhase));
            var position=new Vector3(.25f,0,0);
            var guard=new DefenderSettings("rain-guard","Rain guard",null,position,1000000,1,0,6,2,canAttack:false);
            var battle=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.right*100},position,combat,roster:new[]{guard});
            battle.Begin();Assert.That(battle.TryDeployRole(battle.RunId,1,battle.Entry,profile.Unit.CardId).Success);
            var unit=battle.Units.Single();
            for(int i=0;i<1000&&unit.SkillSP<combat.RequiredSP;i++)battle.Advance(.05f);
            Assert.That(battle.TryActivateSkill(battle.RunId,2,unit.EntityId),Is.EqualTo(SkillFailure.None));
            for(int i=0;i<500&&!battle.CombatShots.Any(s=>!s.FromDefender&&s.Empowered&&s.Applied);i++)battle.Advance(.05f);
            var rain=battle.CombatShots.First(s=>!s.FromDefender&&s.Empowered&&s.Applied);
            Assert.That(rain.Attack,Is.EqualTo(original.SkillAttack*5).Within(.001f));
            Assert.That(rain.DamageDealt,Is.EqualTo(original.SkillAttack*5).Within(.1f));
        }

        [Test] public void LegacyEffectsAndTreatmentKeepTheirExistingCampaignCurves()
        {
            var slug=Definition("CARD_M04");var burst=slug.Engagement.Snapshot();
            var promoted=ShopStarProfiles.Build(BattleShopStages.Settings(slug.Snapshot()),burst,2,3).Combat;
            Assert.That(promoted.Effects.BlastAttack,Is.EqualTo(burst.Effects.BlastAttack*2.7f).Within(.001f));
            Assert.That(promoted.Effects.BlastRadius,Is.EqualTo(burst.Effects.BlastRadius));
            Assert.That(promoted.Effects.BlastDelay,Is.EqualTo(burst.Effects.BlastDelay));
            Assert.That(promoted.Effects.ColdDuration,Is.EqualTo(burst.Effects.ColdDuration));
            var healer=Definition("CARD_M25");var native=healer.Engagement.Snapshot();
            var high=ShopStarProfiles.Build(BattleShopStages.Settings(healer.Snapshot()),native,5,3).Combat;
            Assert.That(high.Effects.Healer,Is.True);Assert.That(high.CanAttack,Is.False);
            Assert.That(high.UnitAttack,Is.EqualTo(native.UnitAttack*CampaignBalance.AttackScale(5,3)).Within(.001f),
                "M25 has a positive campaign base attack, so its treatment budget retains the existing attack curve.");
        }
    }
}
