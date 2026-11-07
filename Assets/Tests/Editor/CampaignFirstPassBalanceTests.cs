using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class CampaignFirstPassBalanceTests
    {
        static ManualBattleDefinition Definition(string id)
        {
            // Defender fixtures reuse the Talulah card with different combat budgets. Use the same
            // definition as the saved main scenes, rather than an arbitrary asset sharing its card ID.
            var data=id=="CARD_TALULAH_TRIAL"
                ?AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset")
                :Resources.Load<ManualBattleDefinition>("MonsterTests/"+id.Substring(5));
            Assert.That(data,Is.Not.Null,id);Assert.That(data.Card.CardId,Is.EqualTo(id));
            return data;
        }

        static ManualBattle Battle(BasicRosterProfile profile,DefenderSettings guard)
        {
            var battle=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.right*100},guard.Position,
                profile.Combat,roster:new[]{guard});
            battle.Begin();
            Assert.That(battle.TryDeployRole(battle.RunId,1,battle.Entry,profile.Unit.CardId).Success);
            return battle;
        }

        [Test] public void RegularSoldierAndArcherOfferDifferentTradesFromTheirOtherFormationPeers()
        {
            var soldierData=Definition("CARD_M07");var archerData=Definition("CARD_M08");
            string soldierSource=EditorJsonUtility.ToJson(soldierData),archerSource=EditorJsonUtility.ToJson(archerData);
            foreach(int star in new[]{1,2,3,4})
            {
                var soldier=CampaignBalance.Build(soldierData,star,100,5);
                var runner=CampaignBalance.Build(Definition("CARD_M15"),star,100,5);
                var archer=CampaignBalance.Build(archerData,star,100,5);
                var snow=CampaignBalance.Build(Definition("CARD_M11"),star,100,5);
                Assert.That(soldier.Unit.Health,Is.GreaterThan(runner.Unit.Health));
                Assert.That(soldier.Combat.UnitAttack,Is.LessThan(runner.Combat.UnitAttack));
                Assert.That(soldier.Unit.Speed,Is.LessThan(runner.Unit.Speed));
                Assert.That(soldier.Unit.DeploymentCost,Is.EqualTo(runner.Unit.DeploymentCost));
                Assert.That(archer.Combat.UnitAttack,Is.GreaterThan(snow.Combat.UnitAttack));
                Assert.That(archer.Unit.Health,Is.LessThan(snow.Unit.Health));
                Assert.That(archer.Combat.UnitAttack,Is.LessThan(snow.Combat.UnitAttack*snow.Combat.Effects.FrozenAttackMultiplier));
                Assert.That(archer.Combat.UnitInterval,Is.EqualTo(snow.Combat.UnitInterval));
                Assert.That(archer.Unit.DeploymentCost,Is.EqualTo(snow.Unit.DeploymentCost));
            }
            Assert.That(EditorJsonUtility.ToJson(soldierData),Is.EqualTo(soldierSource));
            Assert.That(EditorJsonUtility.ToJson(archerData),Is.EqualTo(archerSource));
        }

        [TestCase(1,80f)]
        [TestCase(2,140f)]
        [TestCase(3,240f)]
        public void ManualRainBudgetIsCampaignOnlyAndKeepsItsAuthoredCycle(int star,float expected)
        {
            var data=Definition("CARD_TALULAH_TRIAL");var authored=data.Engagement.Snapshot();
            string source=EditorJsonUtility.ToJson(data.Engagement);
            var ambiguous=AssetDatabase.FindAssets("t:ManualBattleDefinition")
                .Select(g=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .First(d=>d!=null&&d.Card.CardId==data.Card.CardId);
            TestContext.WriteLine($"TALULAH_SOURCE_SELECTION broadFirst={AssetDatabase.GetAssetPath(ambiguous)}; broadBudget={ambiguous.Engagement.Snapshot().SkillAttack}; campaign={AssetDatabase.GetAssetPath(data)}; campaignBudget={authored.SkillAttack}");
            var campaign=CampaignBalance.Build(data,star,100,5).Combat;
            var legacy=ShopStarProfiles.Build(data.Snapshot(),authored,5,star).Combat;
            Assert.That(campaign.SkillAttack,Is.EqualTo(expected));
            Assert.That(legacy.SkillAttack,Is.EqualTo(authored.SkillAttack*CampaignBalance.AbilityScale(5,star)).Within(.001f));
            Assert.That(data.Engagement.Snapshot().SkillAttack,Is.EqualTo(48));
            Assert.That(campaign.Effects.DamageType,Is.EqualTo(CombatDamageType.Physical));
            Assert.That(campaign.SkillAmmo,Is.EqualTo(authored.SkillAmmo));
            Assert.That(campaign.SkillInterval,Is.EqualTo(authored.SkillInterval));
            Assert.That(campaign.ProjectileTime,Is.EqualTo(authored.ProjectileTime));
            Assert.That(campaign.InitialSP,Is.EqualTo(authored.InitialSP));
            Assert.That(campaign.RequiredSP,Is.EqualTo(authored.RequiredSP));
            Assert.That(campaign.SPPerSecond,Is.EqualTo(authored.SPPerSecond));
            Assert.That(campaign.Effects.TalulahSecondPhase,Is.EqualTo(star==3));
            Assert.That(EditorJsonUtility.ToJson(data.Engagement),Is.EqualTo(source));
        }

        [TestCase(1,0,80f)]
        [TestCase(1,40,80f)]
        [TestCase(2,0,140f)]
        [TestCase(2,40,140f)]
        [TestCase(3,0,240f)]
        [TestCase(3,40,240f)]
        public void ManualRainDealsSixRealPhysicalWavesOutsideOrdinaryRangeAndRespectsPause(int star,float armor,float perWave)
        {
            var profile=CampaignBalance.Build(Definition("CARD_TALULAH_TRIAL"),star,100,5);
            var guard=new DefenderSettings("far-rain-guard","Far rain guard",null,Vector3.right*60,1000000,1,armor,.1f,2,canAttack:false);
            var battle=Battle(profile,guard);var unit=battle.Units.Single();
            for(int i=0;i<1000&&!battle.IsSkillReady(unit.EntityId);i++)battle.Advance(.05f);
            Assert.That(battle.TryActivateSkill(battle.RunId,2,unit.EntityId),Is.EqualTo(SkillFailure.None));
            double clock=battle.Clock;float health=battle.Defender.Health;
            battle.SetPaused(true);battle.Advance(10);
            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(battle.Defender.Health,Is.EqualTo(health));
            battle.SetPaused(false);
            for(int i=0;i<122;i++)battle.Advance(.05f);
            var waves=battle.CombatShots.Where(s=>s.FireRain&&s.Applied).ToArray();
            Assert.That(waves.Length,Is.EqualTo(6));
            Assert.That(waves.All(s=>Math.Abs(s.Attack-perWave)<.001f));
            Assert.That(health-battle.Defender.Health,Is.EqualTo(6*Mathf.Max(1,perWave-armor)).Within(.1f));
            Assert.That(unit.OrdinaryAttacksFired,Is.Zero,"The remote fixture cannot mix ordinary hits into the rain budget.");
        }

        [Test] public void ThreeStarAutomaticRainStillUsesOrdinaryAttackAndArtsDamage()
        {
            var profile=CampaignBalance.Build(Definition("CARD_TALULAH_TRIAL"),3,100,5);
            var guard=new DefenderSettings("automatic-rain-guard","Automatic rain guard",null,Vector3.right*60,1000000,1,40,.1f,2,canAttack:false);
            var battle=Battle(profile,guard);var unit=battle.Units.Single();float health=battle.Defender.Health;
            Assert.That(battle.TryDamageUnit(unit.EntityId,100000,CombatDamageType.Arts));
            for(int i=0;i<141;i++)battle.Advance(.05f);
            Assert.That(unit.TalulahAutomaticWavesFired,Is.EqualTo(1));
            Assert.That(health-battle.Defender.Health,Is.EqualTo(profile.Combat.UnitAttack).Within(.1f));
            Assert.That(profile.Combat.SkillAttack,Is.EqualTo(240));
        }

        [TestCase(7,220f,24f,3f)]
        [TestCase(8,260f,30f,4f)]
        [TestCase(11,340f,48f,6f)]
        public void LateOrdinaryStagesAddOnlyTheAuthoredSecondPositionRangedGuard(int stage,float health,float attack,float armor)
        {
            var source=Resources.Load<DefenderTrialDefinition>("DefenderTrials/D07");string before=EditorJsonUtility.ToJson(source);
            var authored=source.defenders.Single(d=>d.artId=="D07");
            var guards=CampaignPlan.Guards(stage,false);Assert.That(guards.Length,Is.EqualTo(2));
            var melee=guards[0];var ranged=guards[1];
            Assert.That(melee.id,Is.EqualTo("loop-left"));
            Assert.That(melee.health,Is.EqualTo(90+stage*32));Assert.That(melee.attack,Is.EqualTo(7+stage*1.2f));
            Assert.That(melee.armor,Is.EqualTo(stage));Assert.That(melee.range,Is.EqualTo(1.1f));Assert.That(melee.blocks,Is.EqualTo(2));
            Assert.That(ranged.id,Is.EqualTo("campaign-ranged-"+stage));Assert.That(ranged.artId,Is.EqualTo("D07"));
            Assert.That(ranged.position,Is.EqualTo(BattleShopStages.Defenders(1,false)[1].position));
            Assert.That(ranged.health,Is.EqualTo(health));Assert.That(ranged.attack,Is.EqualTo(attack));Assert.That(ranged.armor,Is.EqualTo(armor));
            Assert.That(ranged.range,Is.EqualTo(3.2f));Assert.That(ranged.interval,Is.EqualTo(2));Assert.That(ranged.blocks,Is.Zero);
            Assert.That(ranged.windup,Is.EqualTo(authored.windup));Assert.That(ranged.actionDuration,Is.EqualTo(authored.actionDuration));
            Assert.That(ranged.skill,Is.Null);Assert.That(ranged.criticalChance,Is.Zero);
            Assert.DoesNotThrow(()=>ranged.Snapshot());
            Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(before));
        }

        [TestCase(3)]
        [TestCase(4)]
        public void EarlyOrdinaryStagesRetainTheirOneMeleeGuard(int stage)
        {
            var guard=CampaignPlan.Guards(stage,false).Single();
            Assert.That(guard.artId,Is.EqualTo("D01"));Assert.That(guard.position,Is.EqualTo(BattleShopStages.Defenders(1,false)[0].position));
            Assert.That(guard.health,Is.EqualTo(90+stage*32));Assert.That(guard.attack,Is.EqualTo(7+stage*1.2f));
            Assert.That(guard.armor,Is.EqualTo(stage));Assert.That(guard.range,Is.EqualTo(1.1f));Assert.That(guard.interval,Is.EqualTo(2));
        }

        [TestCase(7)]
        [TestCase(8)]
        [TestCase(11)]
        public void SandboxStagesKeepTheirExistingTwoBasicGuards(int stage)
        {
            var guards=CampaignPlan.Guards(stage,true);
            Assert.That(guards.Select(g=>g.artId),Is.EqualTo(new[]{"D01","D04"}));
            Assert.That(guards.All(g=>g.health==950&&g.attack==5&&g.armor==8&&g.range==1.1f));
        }

        [Test] public void NewRangedGuardNaturallyTradesShotsWithAnOrdinaryArcher()
        {
            var guard=CampaignPlan.Guards(7,false)[1].Snapshot();
            var profile=CampaignBalance.Build(Definition("CARD_M08"),1,100,5);
            var battle=Battle(profile,guard);
            for(int i=0;i<160;i++)battle.Advance(.05f);
            Assert.That(battle.CombatShots.Any(s=>s.FromDefender&&s.Applied),Is.True);
            Assert.That(battle.CombatShots.Any(s=>!s.FromDefender&&s.Applied),Is.True);
            Assert.That(battle.Units.Single().Health,Is.LessThan(profile.Unit.Health));
            Assert.That(battle.Defender.Health,Is.LessThan(guard.Health));
        }
    }
}
