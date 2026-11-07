using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class FourStarBalanceTests
    {
        static ManualBattleDefinition Definition(string id) => AssetDatabase.FindAssets("t:ManualBattleDefinition")
            .Select(g=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .First(d=>d!=null&&d.Card.CardId==id);

        [Test] public void EveryOneCostRoleBuildsAtFourStarsWithoutNewMechanicsOrShorterActionWindows()
        {
            var ids=CampaignBalance.Profiles.Where(p=>p.Value.Cost==1).Select(p=>p.Key).ToArray();
            Assert.That(ids.Length,Is.GreaterThan(1));
            foreach(string id in ids)
            {
                var data=Definition(id);var source=data.Snapshot();var authored=data.Engagement.Snapshot();
                var three=CampaignBalance.Build(data,3,50,5);var four=CampaignBalance.Build(data,4,50,5);
                Assert.That(four.Unit.Health,Is.GreaterThan(three.Unit.Health),id);
                Assert.That(four.Combat.UnitAttack,Is.GreaterThan(three.Combat.UnitAttack),id);
                Assert.That(four.Combat.UnitArmor-three.Combat.UnitArmor,Is.EqualTo(2),id);
                Assert.That(four.Unit.DeploymentCost,Is.EqualTo(5),id);
                Assert.That(four.Unit.DeploymentLimit,Is.EqualTo(three.Unit.DeploymentLimit),id);
                Assert.That(four.Unit.GateDamage,Is.EqualTo(three.Unit.GateDamage),"Four stars retain the existing three-star breach bonus: "+id);
                Assert.That(four.Unit.Cooldown,Is.EqualTo(three.Unit.Cooldown),id);
                Assert.That(four.Unit.Speed,Is.EqualTo(three.Unit.Speed),id);
                Assert.That(four.Combat.UnitRange,Is.EqualTo(three.Combat.UnitRange),id);
                Assert.That(four.Combat.UnitInterval,Is.EqualTo(three.Combat.UnitInterval),id);
                Assert.That(four.Combat.AttackDuration,Is.EqualTo(authored.AttackDuration),id);
                Assert.That(four.Combat.AttackWindup,Is.EqualTo(authored.AttackWindup),id);
                Assert.That(four.Combat.HasSkill,Is.EqualTo(authored.HasSkill),id);
                Assert.That(four.Combat.Effects.TalulahSecondPhase,Is.False,id);
                Assert.That(data.Snapshot().Health,Is.EqualTo(source.Health),"Building a star profile cannot write the source asset.");
            }
        }

        [Test] public void OneCostFourStarsContinueTheLastOrdinaryIncrement()
        {
            Assert.That(CampaignBalance.HPScale(1,4),Is.EqualTo(4.25f));
            Assert.That(CampaignBalance.AttackScale(1,4),Is.EqualTo(2.8f));
            Assert.That(CampaignBalance.AbilityScale(1,4),Is.EqualTo(3.8f));
            var piece=CampaignBalance.Build(Definition("CARD_M07"),4,50,5);
            Assert.That(piece.Unit.Health,Is.EqualTo(744));
            Assert.That(piece.Combat.UnitAttack,Is.EqualTo(67.2f).Within(.001f));
            Assert.That(piece.Combat.UnitArmor,Is.EqualTo(10));
            Assert.That(piece.Unit.DeploymentLimit,Is.EqualTo(8));
            Assert.That(piece.Unit.GateDamage,Is.EqualTo(2));
        }

        [Test] public void CostsTwoThroughFiveCannotCreateFourStarCombatProfiles()
        {
            foreach(var entry in CampaignBalance.Profiles.Where(p=>p.Value.Cost>1))
            {
                var data=Definition(entry.Key);
                Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignBalance.Build(data,4,50,5),entry.Key);
                Assert.DoesNotThrow(()=>CampaignBalance.Build(data,3,50,5),entry.Key);
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignBalance.Build(Definition("CARD_M07"),5,50,5));
        }

        static ManualBattle Duel(BasicRosterProfile profile,float health,float armor)
        {
            var position=new Vector3(.25f,0,0);
            var guard=new DefenderSettings("balance-guard","Balance guard",null,position,health,1,armor,6,2,canAttack:false);
            var battle=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.right*100},position,profile.Combat,roster:new[]{guard});
            battle.Begin();Assert.That(battle.TryDeployRole(battle.RunId,1,battle.Entry,profile.Unit.CardId).Success);
            return battle;
        }

        [TestCase("CARD_M02",400,4)]
        [TestCase("CARD_M07",400,4)]
        [TestCase("CARD_M07",1200,18)]
        [TestCase("CARD_M08",1200,18)]
        [TestCase("CARD_M09",1200,18)]
        [TestCase("CARD_M07",10,4)]
        [TestCase("CARD_M07",25,100)]
        public void OrdinaryHighArmorAndThresholdTargetsUseRealCombatInsteadOfAStatOnlyEstimate(string id,float health,float armor)
        {
            var data=Definition(id);
            var three=Duel(CampaignBalance.Build(data,3,100,5),health,armor);
            var four=Duel(CampaignBalance.Build(data,4,100,5),health,armor);
            for(int i=0;i<6000&&(three.Defenders[0].Alive||four.Defenders[0].Alive);i++)
            {
                if(three.Defenders[0].Alive)three.Advance(.05f);
                if(four.Defenders[0].Alive)four.Advance(.05f);
            }
            Assert.That(three.Defenders[0].Alive,Is.False,id+" baseline must finish");
            Assert.That(four.Defenders[0].Alive,Is.False,id+" four-star must finish");
            int oldHits=three.CombatShots.Count(s=>!s.FromDefender&&s.Applied),newHits=four.CombatShots.Count(s=>!s.FromDefender&&s.Applied);
            Assert.That(newHits,Is.LessThanOrEqualTo(oldHits));
            Assert.That(four.Defenders[0].DeathTime,Is.LessThanOrEqualTo(three.Defenders[0].DeathTime+.001));
            if(health==10||armor==100)Assert.That(newHits,Is.EqualTo(oldHits),"Discrete one-hit and minimum-damage thresholds need not improve their attack count.");
            TestContext.WriteLine($"FOUR_STAR_COMBAT {id} hp={health} armor={armor}: three={oldHits} hits/{three.Defenders[0].DeathTime:0.00}s; four={newHits} hits/{four.Defenders[0].DeathTime:0.00}s");
        }

        [Test] public void FourStarSynergySnapshotAndPausedMusterUseActualStarsWithoutAdditionalKinds()
        {
            var primary=CampaignBalance.Build(Definition("CARD_M07"),4,100,5);
            var secondary=CampaignBalance.Build(Definition("CARD_M08"),4,100,5);
            var battle=Duel(primary,100000,0);
            battle.Restart();battle.ConfigureCardLoadout(new[]{secondary});
            battle.ConfigureSynergies(new[]{new ShopTrialModel.Piece{Id="CARD_M07",Star=4},new ShopTrialModel.Piece{Id="CARD_M08",Star=4}});
            Assert.That(battle.ReunionCount,Is.EqualTo(2));Assert.That(battle.ReunionTier,Is.EqualTo(2));Assert.That(battle.ReunionAverageStar,Is.EqualTo(4));
            Assert.Throws<ArgumentException>(()=>battle.ConfigureSynergies(new[]{new ShopTrialModel.Piece{Id="CARD_M10",Star=4}}));
            Assert.That(battle.ReunionAverageStar,Is.EqualTo(4),"Rejected snapshot cannot replace the valid one.");
            battle.Begin();battle.SetPaused(true);
            Assert.That(battle.TryDeployRole(battle.RunId,1,battle.Entry,"CARD_M07",true).Success);
            Assert.That(battle.TryDeployRole(battle.RunId,2,battle.Entry,"CARD_M08",true).Success);
            var support=battle.Units.Single(u=>u.SynergySupport);
            Assert.That(support.MaxHealth,Is.EqualTo(200));Assert.That(battle.UnitCombat(support).UnitAttack,Is.EqualTo(35));
            Assert.That(battle.DP,Is.EqualTo(90));battle.Advance(20);
            Assert.That(battle.Clock,Is.Zero);Assert.That(battle.Units.All(u=>u.OrdinaryAttacksFired==0));
            battle.Restart();Assert.That(battle.Units,Is.Empty);Assert.That(battle.ReunionAverageStar,Is.EqualTo(4));Assert.That(battle.MusterUsed,Is.Zero);
        }
    }
}
