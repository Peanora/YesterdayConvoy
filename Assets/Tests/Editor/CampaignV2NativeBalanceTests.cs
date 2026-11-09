using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    // Isolated numerical/phase fixtures, not natural campaign completion evidence.
    public sealed class CampaignV2NativeBalanceTests
    {
        static ManualBattleDefinition Definition(string id)=>id=="CARD_TALULAH_TRIAL"?
            AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset"):
            Resources.Load<ManualBattleDefinition>("MonsterTests/"+id.Substring(5));
        static ManualBattle Model(string id,int star=1,bool v2=true,Vector3[] positions=null,int blocks=1)
        {
            var profile=CampaignV2Balance.Build(Definition(id),star,50,6);
            positions??=new[]{Vector3.right*.4f};
            var guards=positions.Select((p,i)=>new DefenderSettings("g"+i,"fixture",null,p,100000,1,0,.001f,100,blocks,canAttack:false)).ToArray();
            var battle=new ManualBattle(profile.Unit,new[]{Vector3.zero,Vector3.forward*100},positions[0],profile.Combat,roster:guards);
            if(v2)battle.ConfigureCampaignV2(13);return battle;
        }
        static DeployedUnitState Deploy(ManualBattle battle)
        {battle.Begin();var receipt=battle.TryDeploy(battle.RunId,1,battle.Entry);Assert.That(receipt.Success,Is.True);return battle.FindUnit(receipt.EntityId);}
        static void Call(ManualBattle battle,string method,params object[] args)=>typeof(ManualBattle).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,args);

        [Test]public void NonAttackingZeroAttackIsRepresentedExactlyAndAttackersStillRejectZero()
        {
            var profile=CampaignV2Balance.Build(Definition("CARD_M33"),1,50,6);
            Assert.That(profile.Combat.UnitAttack,Is.Zero);Assert.That(profile.Combat.CanAttack,Is.False);
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(unitAttack:0,canAttack:true));
        }

        [TestCase(1,8,180)][TestCase(2,16,360)][TestCase(3,40,900)]
        public void PatriotIndependentImmolationAndSpearUseSkillGrowthNotMarchDamage(int star,float pulse,float spear)
        {
            var battle=Model("CARD_M20",star,positions:new[]{Vector3.right*.4f,Vector3.right*5});
            battle.ConfigureDefenderHighGround(battle.Defenders[1].EntityId,true);var unit=Deploy(battle);
            battle.TryDamageUnit(unit.EntityId,1000000,CombatDamageType.Arts);
            Assert.That(unit.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));
            Assert.That(unit.ImmolationDamage,Is.EqualTo(pulse).Within(.01f));
            battle.SetPaused(true);battle.Advance(10);Assert.That(unit.ImmolationTicks,Is.EqualTo(1));battle.SetPaused(false);
            for(int i=0;i<1600&&unit.Spear==null;i++)battle.Advance(.05f);
            Assert.That(unit.Spear,Is.Not.Null);Assert.That(unit.Spear.Shot.Attack,Is.EqualTo(spear).Within(.001f));
            battle.Advance(2);Assert.That(unit.Spear.Shot.Applied,Is.True);Assert.That(unit.Spear.Shot.DamageDealt,Is.EqualTo(spear).Within(.01f));
        }

        [TestCase(1,120)][TestCase(2,240)][TestCase(3,600)]
        public void MandragoraShieldAndSevenGazeTicksUseTheirIndependentBudgets(int star,float tick)
        {
            var battle=Model("CARD_M32",star);var unit=Deploy(battle);float before=unit.Health;
            battle.TryDamageUnit(unit.EntityId,100,CombatDamageType.Arts);
            Assert.That(before-unit.Health,Is.EqualTo(100*.65f*.55f).Within(.01f));
            battle.Advance(12.3f);var gaze=battle.Gazes.Single();Assert.That(gaze.Ticks,Is.EqualTo(7));
            Assert.That(gaze.DamageDealt,Is.EqualTo(7*tick).Within(.05f));
        }

        [Test]public void FirstSpeakerBubbleFindsHalfCellNeighbourAndSeparatesPhysicalDamageFromNeuralMeter()
        {
            var battle=Model("CARD_M36",positions:new[]{new Vector3(.5f,0,.5f),new Vector3(1.5f,0,.5f)},blocks:0);
            Deploy(battle);battle.Advance(13.2f);var bubble=battle.CorrosiveBubbles.Single();
            Assert.That(bubble.Attack,Is.EqualTo(60));Assert.That(bubble.Pulses,Is.EqualTo(1));
            Assert.That(bubble.TargetIds.Single(),Is.EqualTo("g1"),"The real adjacent half-grid cell has the least neural accumulation.");
            Assert.That(battle.Defenders[1].NeuralAccumulated,Is.EqualTo(20));
            Assert.That(100000-battle.Defenders[1].Health,Is.EqualTo(60).Within(.01f));
            battle.Advance(2);Assert.That(bubble.Pulses,Is.EqualTo(5));
        }

        [TestCase(1,20)][TestCase(2,33)][TestCase(3,54)]
        public void ShacklerFinishesExactlyTwelveTicksAndDoesNotRecast(int star,float tick)
        {
            var battle=Model("CARD_M38",star);Deploy(battle);battle.Advance(12.2f);
            var shackle=battle.Shackles.Single();Assert.That(shackle.Ticks,Is.EqualTo(12));Assert.That(shackle.Ended,Is.True);
            Assert.That(shackle.DamageDealt,Is.EqualTo(12*tick).Within(.05f));battle.Advance(15);Assert.That(battle.Shackles.Count,Is.EqualTo(1));
        }

        [Test]public void SprayAndFuelHaveIndependentDamageAndBurnBudgets()
        {
            var battle=Model("CARD_M31");var unit=Deploy(battle);battle.Advance(.6f);
            Assert.That(battle.FlameSprays.Single().Ticks,Is.EqualTo(1));
            Assert.That(battle.FlameSprays.Single().DamageDealt,Is.EqualTo(42).Within(.01f));
            Assert.That(battle.Defenders[0].BurnAccumulated,Is.EqualTo(9));float before=battle.Defenders[0].Health;
            battle.TryDamageUnit(unit.EntityId,1000000,CombatDamageType.Arts);battle.Advance(.2f);
            Assert.That(battle.FuelExplosions.Single().Resolved,Is.True);Assert.That(before-battle.Defenders[0].Health,Is.EqualTo(250).Within(.01f));
        }

        [Test]public void HarvesterWakesNaturallyAtThirtySecondsAndUsesSixfoldMotion()
        {
            var battle=Model("CARD_M34",positions:new[]{Vector3.right*50},blocks:0);var unit=Deploy(battle);
            battle.Advance(29.9f);Assert.That(unit.HarvesterPhase,Is.EqualTo(HarvesterPhase.Dormant));
            battle.Advance(.2f);Assert.That(unit.HarvesterPhase,Is.EqualTo(HarvesterPhase.Waking));
            battle.Advance(1.1f);Assert.That(unit.HarvesterPhase,Is.EqualTo(HarvesterPhase.Combat));
            Assert.That(battle.UnitMoveMultiplier(unit),Is.EqualTo(6));Assert.That(unit.Health,Is.LessThan(unit.MaxHealth));
        }

        static ManualBattle DefenderModel(int stage,DefenderPlacement[] placements)
        {
            var settings=new ManualBattleSettings("CARD_M07","fixture",50,6,8,1,2,1000000,.001f,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:0,canAttack:false,initialSP:0,requiredSP:0,spPerSecond:0,
                skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            var origin=placements[0].position+Vector3.right*.4f;
            var battle=new ManualBattle(settings,new[]{origin,origin+Vector3.forward*100},Vector3.one*1000,combat,roster:placements.Select(p=>p.Snapshot()).ToArray());
            battle.ConfigureCampaignV2(stage);return battle;
        }

        [TestCase(10,1.45f,1f,14,7)][TestCase(13,1.5f,1.25f,16,8)]
        public void HoshigumaUsesAuthoredHpDebtAndFiniteFinalProtection(int stage,float hpMultiplier,float debtMultiplier,float closeAfter,float protection)
        {
            var placement=CampaignJourneyCatalog.Stage(stage).CopyDefenders().Single(p=>p.artId=="D25");
            var battle=DefenderModel(stage,new[]{placement});Deploy(battle);var guard=battle.Defenders[0];
            for(int i=0;i<1000&&guard.SkillCasts==0;i++)battle.Advance(.05f);
            Assert.That(guard.SkillCasts,Is.EqualTo(1));Assert.That(guard.MaxHealth,Is.EqualTo(placement.health*hpMultiplier).Within(.01f));
            for(int i=0;i<500&&guard.ProtectedHits==0;i++)battle.Advance(.05f);
            Assert.That(guard.SkillActiveUntil-guard.SkillStartedAt,Is.EqualTo(closeAfter).Within(.06));
            Assert.That(guard.ProtectionUntil-guard.SkillActiveUntil,Is.EqualTo(protection).Within(.001));
            Call(battle,"ApplyDefenderLoss",guard,guard.MaxHealth*100);Assert.That(guard.Alive,Is.True);
            battle.Advance(protection+.1f);Assert.That(guard.Departed,Is.True);
            var debtBattle=DefenderModel(stage,new[]{placement});var debtor=debtBattle.Defenders[0];
            Call(debtBattle,"ApplyDefenderLoss",debtor,debtor.Health+debtor.MaxHealth*(debtMultiplier-.01f));Assert.That(debtor.Alive,Is.True);
            Call(debtBattle,"ApplyDefenderLoss",debtor,debtor.MaxHealth*.02f);Assert.That(debtor.Alive,Is.False);
        }

        [Test]public void SariaOnlyGrantsSkillPointsForEffectiveHealingInV2()
        {
            var guards=CampaignJourneyCatalog.Stage(13).CopyDefenders().Where(p=>p.artId=="D29"||p.artId=="D24").ToArray();
            var battle=DefenderModel(13,guards);var source=battle.Defenders.Single(d=>d.Settings.ArtId=="D29");
            var target=battle.Defenders.Single(d=>d.Settings.ArtId=="D24");float sp=target.SkillSP;
            Call(battle,"HealDefender",source,target,100f);Assert.That(target.SkillSP,Is.EqualTo(sp));
            Call(battle,"ApplyDefenderLoss",target,10f);Call(battle,"HealDefender",source,target,100f);
            Assert.That(target.SkillSP,Is.EqualTo(sp+1));
        }
    }
}
