using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class AdvancedSynergyTests
    {
        static readonly string[] Infection={"CARD_ORIGINIUM_SLUG","CARD_M02","CARD_M03","CARD_M04","CARD_M05","CARD_M06"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,100,5,8,2,.1f,200,1,1,1,1,1);
        static EngagementSettings Combat(MonsterEffectSettings effects=default)=>new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:3,unitInterval:2,defenderHealth:10000,defenderAttack:1,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,effects:effects);
        static ShopTrialModel.Piece Piece(string id)=>new ShopTrialModel.Piece{Id=id,Star=1};
        static ManualBattle Battle(string[] ids,InfectionForm form=InfectionForm.Swarm,InfectionForm second=InfectionForm.Shell,int xp=0,bool far=false,MonsterEffectSettings effects=default)
        {
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,new Vector3(10,0,0)},far?new Vector3(50,0,50):new Vector3(.6f,0,0),Combat(effects));
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(effects),true)));b.ConfigureSynergies(ids.Select(Piece),false,form,second,xp);b.Begin();return b;
        }
        [Test]public void SidebarCountsDistinctFormationMembersAndIncludesPendingTags()
        {
            var rows=SynergySummary.From(new[]{"CARD_M03","CARD_M03","CARD_M15","CARD_M26","CARD_M37"});
            Assert.That(rows.Single(r=>r.Name=="游击队").Count,Is.EqualTo(2));Assert.That(rows.Single(r=>r.Name=="游击队").Tier,Is.EqualTo(2));
            Assert.That(rows.Single(r=>r.Name=="感染生物").Count,Is.EqualTo(1));Assert.That(rows.Single(r=>r.Name=="感染生物").Tier,Is.Zero);
            Assert.That(rows.Single(r=>r.Name=="萨卡兹").Count,Is.EqualTo(2));Assert.That(rows.Single(r=>r.Name=="萨卡兹").Implemented,Is.True);
            Assert.That(rows.Any(r=>r.Name=="雪怪小队"),Is.False);Assert.That(rows.All(r=>!string.IsNullOrEmpty(r.Effect)),Is.True);
        }
        [Test]public void FortyCardsIncludeApprovedMandragoraAndKeepNativeTags()
        {
            Assert.That(CampaignBalance.Profiles.Count,Is.EqualTo(40));
            Assert.That(CampaignBalance.Profiles.Count(p=>p.Value.Cost==5),Is.EqualTo(5));
            foreach(string id in new[]{"CARD_M20","CARD_M25","CARD_SILVER_WOLF"})Assert.That(CampaignBalance.Supports(id),Is.False);
            Assert.That(CampaignBalance.Supports("CARD_M32"));
            Assert.That(CampaignBalance.Reunion("CARD_M33"),Is.False);Assert.That(CampaignBalance.MudrockCrew("CARD_M26"));Assert.That(CampaignBalance.Tags("CARD_M26"),Does.Contain("萨卡兹"));
        }
        [Test]public void SwarmPaysOnceAndOnlyPrimaryCanDamageGate()
        {
            var b=Battle(Infection,far:true);var r=b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);Assert.That(r.Success);
            Assert.That(b.Units.Count,Is.EqualTo(3));Assert.That(b.DP,Is.EqualTo(98));Assert.That(b.FindCard(Infection[0]).RemainingUses,Is.EqualTo(7));
            Assert.That(b.Units.All(u=>Math.Abs(u.MaxHealth-110)<.01));Assert.That(b.Units.Count(u=>!u.SynergySupport),Is.EqualTo(1));
            b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);Assert.That(b.Units.Count,Is.EqualTo(3));
            b.Advance(12);Assert.That(b.Defense,Is.EqualTo(4));Assert.That(b.Units.All(u=>u.Resolution==UnitResolution.Breached));
        }
        [Test]public void SplitIntrinsicBlastBudgetDoesNotMultiplyAndSecondaryHasNoCold()
        {
            var e=new MonsterEffectSettings(blastAttack:75,blastRadius:1.2f,coldDuration:2);
            var b=Battle(Infection,effects:e);b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M04");
            Assert.That(b.Units.Sum(u=>b.UnitCombat(u).Effects.BlastAttack),Is.EqualTo(75).Within(.001));
            Assert.That(b.Units.Where(u=>u.SynergySupport).All(u=>b.UnitCombat(u).Effects.ColdDuration==0));
        }
        [Test]public void BurstTriggersOnceAfterFinalDeathAndDoesNotGenerateOrders()
        {
            var b=Battle(Infection.Take(2).ToArray(),InfectionForm.Burst);b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);
            var u=b.Units[0];b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);Assert.That(b.HasPendingDeathBlast);
            b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.Advance(.3f);
            Assert.That(b.InfectionBursts.Count,Is.EqualTo(1));Assert.That(b.InfectionBursts[0].Resolved);Assert.That(b.Defender.Health,Is.EqualTo(9965));Assert.That(b.Orders,Is.Zero);
        }
        [Test]public void ShellAndSixTierSecondaryUseDifferentStrengths()
        {
            var b=Battle(Infection,InfectionForm.Shell,InfectionForm.Burst);b.TryDeployRole(b.RunId,1,b.Entry,Infection[0]);
            Assert.That(b.Units.Count,Is.EqualTo(1));Assert.That(b.Units[0].SynergyShield,Is.EqualTo(70));Assert.That(b.UnitCombat(b.Units[0]).UnitArmor,Is.EqualTo(3));
            b.TryDamageUnit(b.Units[0].EntityId,10000,CombatDamageType.Physical);Assert.That(b.InfectionBursts[0].Attack,Is.EqualTo(35));
        }
        [Test]public void OrdersUseDirectContactsOnceAndDoNotRecursivelyGenerateOrResetAttackCycles()
        {
            var ids=new[]{"CARD_M15","CARD_M16","CARD_M07","CARD_M08"};var b=Battle(ids);
            for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);b.Advance(.4f);
            Assert.That(b.Orders,Is.EqualTo(4));int attacks=b.Units.Sum(u=>u.OrdinaryAttacksFired);
            Assert.That(b.TryGuerrillaOrder(b.RunId,100));Assert.That(b.Orders,Is.Zero);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            int strikes=b.OrderStrikes;Assert.That(b.TryGuerrillaOrder(b.RunId,100));Assert.That(b.OrderStrikes,Is.EqualTo(strikes));
            b.Advance(.3f);Assert.That(b.Orders,Is.Zero);Assert.That(b.Units.Sum(u=>u.OrdinaryAttacksFired),Is.EqualTo(attacks));
            Assert.That(b.TryGuerrillaOrder(b.RunId,101),Is.False);Assert.That(b.OrdersUsed,Is.EqualTo(1));
        }
        [Test]public void NoTargetOrPauseCannotSpendOrders()
        {
            var ids=new[]{"CARD_M15","CARD_M16","CARD_M07","CARD_M08"};var b=Battle(ids);for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);b.Advance(.4f);
            b.SetPaused(true);Assert.That(b.TryGuerrillaOrder(b.RunId,100),Is.False);Assert.That(b.Orders,Is.EqualTo(4));
            b.SetPaused(false);foreach(var u in b.Units.Where(u=>!u.SynergySupport))b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);
            Assert.That(b.TryGuerrillaOrder(b.RunId,101),Is.False);Assert.That(b.Orders,Is.EqualTo(4));
        }
        [Test]public void HalfOrdersRequireFreshParticipantsAndDoNotDoubleStrikeFirstHalf()
        {
            var ids=new[]{"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};var b=Battle(ids);
            for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,i+1,b.Entry,ids[i]);b.Advance(.4f);Assert.That(b.Orders,Is.EqualTo(6));
            Assert.That(b.TryGuerrillaOrder(b.RunId,100,true));Assert.That(b.HalfOrderPending);Assert.That(b.Orders,Is.EqualTo(3));
            Assert.That(b.TryGuerrillaOrder(b.RunId,101,true),Is.False);Assert.That(b.Orders,Is.EqualTo(3));Assert.That(b.TryGuerrillaOrder(b.RunId,102),Is.False);
            b.TryDeployRole(b.RunId,30,b.Entry,"CARD_M15");b.Advance(.3f);Assert.That(b.TryGuerrillaOrder(b.RunId,103,true));
            Assert.That(b.HalfOrderPending,Is.False);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            var strikes=b.CombatShots.Where(s=>s.SynergyOrder).ToArray();Assert.That(strikes.Length,Is.EqualTo(7));Assert.That(strikes.Select(s=>s.SourceId).Distinct().Count(),Is.EqualTo(7));
        }
        [Test]public void PausedGolemDeploymentIsAtomicLimitedAndKeepsFullAttack()
        {
            var b=Battle(new[]{"CARD_M26","CARD_M27"},xp:100);b.SetPaused(true);
            Assert.That(b.GolemStage,Is.EqualTo(1));Assert.That(b.TryDeployGolem(b.RunId,10,b.Entry).Failure,Is.EqualTo(DeployFailure.Paused));
            var r=b.TryDeployGolem(b.RunId,10,b.Entry,true);Assert.That(r.Success);var u=b.FindUnit(r.EntityId);
            Assert.That(b.DP,Is.EqualTo(96));Assert.That(b.TryDeployGolem(b.RunId,10,b.Entry,true).EntityId,Is.EqualTo(r.EntityId));Assert.That(b.Units.Count,Is.EqualTo(1));
            Assert.That(b.TryDeployGolem(b.RunId,11,b.Entry,true).Failure,Is.EqualTo(DeployFailure.NoUses));
            Assert.That(b.UnitCombat(u).AttackDuration,Is.EqualTo(5.166667f));b.Advance(20);Assert.That(b.Clock,Is.Zero);
        }
        [Test]public void GolemGrowthRequiresRealContactsAndSettlesOnce()
        {
            var b=Battle(new[]{"CARD_M26","CARD_M27"});b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M26");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M27");b.TryDeployGolem(b.RunId,3,b.Entry);b.Advance(3.3f);
            Assert.That(b.GolemGrowthEarned,Is.Zero);Win(b);Assert.That(b.GolemGrowthEarned,Is.EqualTo(3));
            var e=new ShopTrialModel(new[]{new ShopTrialModel.Role{Id="A",Name="A",Cost=1}});e.Preset(new[]{Piece("A")},Array.Empty<string>(),1,0,10);
            var run=new BattleShopRun(e,1,false,true);run.Observe(BattlePhase.Won);run.OpenShop();run.PrepareBattle();run.Observe(BattlePhase.Won);run.OpenShop();run.PrepareBattle();Assert.That(run.Observe(b.Phase,b.GolemGrowthEarned));Assert.That(run.GolemExperience,Is.EqualTo(3));Assert.That(run.Observe(b.Phase,b.GolemGrowthEarned),Is.False);Assert.That(run.GolemExperience,Is.EqualTo(3));
            var empty=Battle(new[]{"CARD_M26","CARD_M27"});Win(empty);Assert.That(empty.GolemGrowthEarned,Is.Zero);
        }
        static void Win(ManualBattle battle)=>typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,new object[]{battle.RunId,true});
        static BasicRosterProfile Real(string id)
        {
            var data=UnityEditor.AssetDatabase.FindAssets("t:ManualBattleDefinition").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(d=>d.Card.CardId==id);
            return CampaignBalance.Build(data,1,100,5);
        }
        static ManualBattle Mixed(params string[] ids)
        {
            var profiles=ids.Distinct().Select(Real).ToArray();var first=profiles[0];
            var b=new ManualBattle(first.Unit,new[]{Vector3.zero,new Vector3(20,0,0)},new Vector3(50,0,50),first.Combat);
            b.ConfigureCardLoadout(profiles.Skip(1));b.ConfigureSynergies(ids.Distinct().Select(Piece));b.Begin();return b;
        }
        [Test]public void MixedFlamechaserAndCreeperUseOwnIntrinsicState()
        {
            var b=Mixed("CARD_M07","CARD_M30","CARD_M33");b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M07");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M30");b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M33");
            var soldier=b.Units.First(u=>u.CardId=="CARD_M07");var flame=b.Units.First(u=>u.CardId=="CARD_M30");var creeper=b.Units.First(u=>u.CardId=="CARD_M33");
            b.TryDamageUnit(flame.EntityId,100000,CombatDamageType.Arts);Assert.That(flame.Active);Assert.That(flame.FlamePhase,Is.EqualTo(FlamechaserPhase.Transforming));
            b.TryDamageUnit(creeper.EntityId,200,CombatDamageType.Arts);Assert.That(creeper.CreeperRetaliations,Is.EqualTo(1));Assert.That(soldier.CreeperRetaliations,Is.Zero);Assert.That(soldier.FlamePhase,Is.EqualTo(FlamechaserPhase.Soldier));
            Assert.That(soldier.Health,Is.EqualTo(soldier.MaxHealth));
        }
        [Test]public void PhalanxDoesNotGiveItsArmorMechanicToOtherSpecies()
        {
            var b=Mixed("CARD_M29","CARD_M07");b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M29");b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M07");b.Advance(2.01f);b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M07");
            var soldiers=b.Units.Where(u=>u.CardId=="CARD_M07").ToArray();Assert.That(soldiers.Length,Is.EqualTo(2));foreach(var u in soldiers)Assert.That(b.UnitArmor(u),Is.EqualTo(4));
        }
    }
}
