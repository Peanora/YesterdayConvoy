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
    }
}
