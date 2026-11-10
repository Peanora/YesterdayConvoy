using System;
using System.Collections;
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
            if(v2)battle.ConfigureCampaignV2(12);return battle;
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

        static (ManualBattle battle,DefenderState guard,DeployedUnitState unit) StartedGuard(int stage,string id)
        {
            var data=CampaignJourneyCatalog.Stage(stage);string original=JsonUtility.ToJson(data.encounter);
            var placement=data.CopyDefenders().Single(p=>p.id==id);var battle=DefenderModel(stage,new[]{placement});var unit=Deploy(battle);
            var guard=battle.Defenders[0];
            for(int i=0;i<2400&&guard.SkillCasts==0;i++)battle.Advance(.05f);
            Assert.That(guard.SkillCasts,Is.EqualTo(1),"The authored "+id+" skill must activate naturally.");
            Assert.That(JsonUtility.ToJson(data.encounter),Is.EqualTo(original));
            return(battle,guard,unit);
        }
        static void SetState(object target,string name,object value)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var property=target.GetType().GetProperty(name,flags);
            if(property!=null)property.SetValue(target,value);else target.GetType().GetField(name,flags).SetValue(target,value);
        }

        [TestCase(10)][TestCase(12)]public void FormalLapplandUsesAuthoredContactFearRampCapAndAreaDamage(int stage)
        {
            var(b,d,u)=StartedGuard(stage,"D21");var data=CampaignJourneyCatalog.Stage(stage);
            float Value(string key)=>data.GuardValue(d.EntityId,key,float.NaN);
            for(int i=0;i<800&&d.FearApplications==0;i++)b.Advance(.01f);
            Assert.That(d.FearApplications,Is.GreaterThan(0));Assert.That(u.FearUntil-b.Clock,Is.EqualTo(Value("droneContactFear")).Within(.02));
            b.Advance(10);
            Assert.That(b.DefenderDrones.Count,Is.GreaterThan(0));Assert.That(b.DefenderDrones.Max(v=>v.Ramp),Is.EqualTo(Value("droneRampCap")).Within(.001));
            SetState(u,"Health",10000f);SetState(u,"NextDroneAreaAt",b.Clock);
            float health=u.Health;Call(b,"StepDefenderDrones",0f);
            Assert.That(health-u.Health,Is.EqualTo(b.DefenderSkillAttack(d)*Value("droneAreaAttackFraction")).Within(.02),"Area damage must use its own budget, independently of the direct drone shot.");
        }

        [TestCase(10)][TestCase(12)]public void FormalSilverAshUsesAuthoredBudgetAndVulnerabilityOnActualHits(int stage)
        {
            var(b,d,u)=StartedGuard(stage,"D26");var data=CampaignJourneyCatalog.Stage(stage);
            float Value(string key)=>data.GuardValue(d.EntityId,key,float.NaN);
            Assert.That(b.DefenderBudget,Is.EqualTo(Value("independentBudgetInitial")+Value("budgetInstant")).Within(.05));
            for(int i=0;i<600&&u.VulnerableUntil<=b.Clock;i++)b.Advance(.01f);
            Assert.That(u.VulnerableUntil,Is.GreaterThan(b.Clock));Assert.That(u.Vulnerability,Is.EqualTo(Value("vulnerability")));
            Assert.That(u.VulnerableUntil-b.Clock,Is.EqualTo(Value("vulnerabilitySeconds")).Within(.02));
            float elapsed=(float)(b.Clock-d.SkillStartedAt);
            Assert.That(b.DefenderBudget,Is.EqualTo(Value("independentBudgetInitial")+Value("budgetInstant")+Value("budgetDuringSkillTotal")*elapsed/d.Settings.Skill.Duration).Within(.05));
            b.SetPaused(true);float budget=b.DefenderBudget;double clock=b.Clock;b.Advance(20);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.DefenderBudget,Is.EqualTo(budget));
        }

        [TestCase(9)][TestCase(12)]public void FormalPramanixUsesAuthoredLureSnowDamageAndSlowWithoutRepeatEntryDamage(int stage)
        {
            var(b,d,u)=StartedGuard(stage,"D27");var data=CampaignJourneyCatalog.Stage(stage);
            float Value(string key)=>data.GuardValue(d.EntityId,key,float.NaN);
            Assert.That(u.LureUntil-b.Clock,Is.EqualTo(Value("lureDuration")).Within(.02));
            ((IList)typeof(ManualBattle).GetField("snowCells",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b)).Clear();
            SetState(d,"NextSnowAt",b.Clock);SetState(u,"Health",10000f);
            float health=u.Health;int hits=d.SnowHits;Call(b,"StepSixthDefenders",0f);
            Assert.That(d.SnowHits-hits,Is.GreaterThan(0));
            Assert.That(health-u.Health,Is.EqualTo((d.SnowHits-hits)*d.Settings.Attack*Value("snowEntryAttackFraction")).Within(.02));
            Assert.That(d.NextSnowAt-b.Clock,Is.EqualTo(Value("snowIntervalSkill")).Within(.001));
            Assert.That(u.SnowMove,Is.EqualTo(1-Value("snowSlowPerLayer")).Within(.001));
            health=u.Health;Call(b,"StepSixthDefenders",0f);Assert.That(u.Health,Is.EqualTo(health),"Remaining on a snow cell must not repeat its entry damage.");
        }

        [TestCase(9)][TestCase(12)]public void FormalWangUsesAuthoredAmmunitionExtraPiecesAndTrapDamage(int stage)
        {
            var(b,d,u)=StartedGuard(stage,"D28");var data=CampaignJourneyCatalog.Stage(stage);
            float Value(string key)=>data.GuardValue(d.EntityId,key,float.NaN);
            Assert.That(d.Ammo,Is.InRange((int)(Value("ammo")-Value("extraPiecesPerPlacement")),(int)Value("ammo")));
            ((IList)typeof(ManualBattle).GetField("chessPieces",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b)).Clear();
            SetState(d,"NextSixthAt",b.Clock+100);SetState(u,"Health",10000f);
            int ammo=d.Ammo;float health=u.Health;int triggered=d.ChessTriggers;
            Call(b,"AddChess",d,u.Motion.Position,true);
            Assert.That(ammo-d.Ammo,Is.EqualTo((int)Value("extraPiecesPerPlacement")));
            Assert.That(b.ChessPieces.Count,Is.EqualTo(1+(int)Value("extraPiecesPerPlacement")));
            Call(b,"StepSixthDefenders",0f);Assert.That(d.ChessTriggers-triggered,Is.EqualTo(1));
            Assert.That(health-u.Health,Is.EqualTo(d.Settings.Attack*Value("trapAttackMultiplier")).Within(.02));
            ((IList)typeof(ManualBattle).GetField("chessPieces",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b)).Clear();
            SetState(d,"NextSixthAt",b.Clock);Call(b,"StepSixthDefenders",0f);
            Assert.That(d.NextSixthAt-b.Clock,Is.EqualTo(Value("placementInterval")).Within(.001));
        }

        [TestCase(10)][TestCase(12)]public void FormalExusiaiKeepsShootingWhenTheSkillUsesOrdinaryRange(int stage)
        {
            var(b,d,u)=StartedGuard(stage,"D24");int ammo=d.Ammo;float health=u.Health;
            Assert.That(b.DefenderRange(d),Is.GreaterThan(0),"A zero radius means ordinary range, not a disabled gun.");
            for(int i=0;i<400&&!b.CombatShots.Any(s=>s.FromDefender&&s.Empowered&&s.SourceId==d.EntityId&&s.Applied);i++)b.Advance(.01f);
            var shots=b.CombatShots.Where(s=>s.FromDefender&&s.Empowered&&s.SourceId==d.EntityId&&s.Applied).ToArray();
            Assert.That(shots,Is.Not.Empty);Assert.That(u.Health,Is.LessThan(health),"An applied defender shell must damage its actual target.");Assert.That(d.Ammo,Is.LessThan(ammo));
        }

        [TestCase(9,1.45f,1f,14,7)][TestCase(12,1.5f,1.25f,16,8)]
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
            var guards=CampaignJourneyCatalog.Stage(12).CopyDefenders().Where(p=>p.artId=="D29"||p.artId=="D24").ToArray();
            var battle=DefenderModel(12,guards);var source=battle.Defenders.Single(d=>d.Settings.ArtId=="D29");
            var target=battle.Defenders.Single(d=>d.Settings.ArtId=="D24");float sp=target.SkillSP;
            Call(battle,"HealDefender",source,target,100f);Assert.That(target.SkillSP,Is.EqualTo(sp));
            Call(battle,"ApplyDefenderLoss",target,10f);Call(battle,"HealDefender",source,target,100f);
            Assert.That(target.SkillSP,Is.EqualTo(sp+1));
        }
    }
}
