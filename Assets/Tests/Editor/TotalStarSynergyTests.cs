using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class TotalStarSynergyTests
    {
        static readonly string[] Reunion={"CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M15","CARD_M16","CARD_M11","CARD_M21"};
        static readonly string[] Snow={"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"};
        static readonly string[] Deep={"CARD_M29","CARD_M30","CARD_M31","CARD_M32"};
        static readonly MethodInfo Damage=typeof(ManualBattle).GetMethod("DamageDefender",BindingFlags.Instance|BindingFlags.NonPublic);
        static ShopTrialModel.Piece P(string id,int star=1)=>new ShopTrialModel.Piece{Id=id,Star=star};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,10,20,2,.1f,10000,.01f,1,1,1,1);
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:1,unitArmor:0,unitRange:3,unitInterval:.25f,
            defenderHealth:10000,defenderAttack:1,defenderArmor:0,defenderRange:.1f,blockCount:0,
            initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
            hasSkill:false,projectileTime:.05f);
        static ManualBattle Battle(params ShopTrialModel.Piece[] snapshot)
        {
            var ids=snapshot.Select(p=>p.Id).Distinct().ToArray();
            var guard=new DefenderSettings("total-star-guard","guard",null,Vector3.right*.5f,10000,1,0,.1f,10,canAttack:false);
            var b=new ManualBattle(Unit(ids[0]),new[]{Vector3.zero,Vector3.forward*50},guard.Position,Combat(),roster:new[]{guard});
            b.ConfigureCardLoadout(ids.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(),true)));
            b.ConfigureSynergies(snapshot);b.Begin();return b;
        }
        static DeployedUnitState Deploy(ManualBattle b,string id,long request,bool paused=false)
        {
            var receipt=b.TryDeployRole(b.RunId,request,b.Entry,id,paused);
            Assert.That(receipt.Success,Is.True,id+": "+receipt.Failure);return b.FindUnit(receipt.EntityId);
        }
        static DeployedUnitState[] Muster(ManualBattle b,string[] ids)
        {
            b.SetPaused(true);
            for(int i=0;i<ReunionRules.KindsPerMuster(b.ReunionTier);i++)Deploy(b,ids[i],i+1,true);
            // Infection splits also use SynergySupport; only these two unit IDs are Reunion reinforcements.
            return b.Units.Where(u=>u.SynergySupport&&(u.UnitId=="M07_SOLDIER"||u.UnitId=="M08_ARCHER")).ToArray();
        }
        static float NextShatter(ManualBattle b)
        {
            int count=b.Shatters;float before=b.ShatterDamage;
            for(int i=0;i<400&&b.Shatters==count;i++)
            {
                b.TryApplyCold(b.Defender.EntityId,3.5f);b.TryApplyCold(b.Defender.EntityId,3.5f);b.Advance(.025f);
            }
            Assert.That(b.Shatters,Is.EqualTo(count+1));return b.ShatterDamage-before;
        }
        static float Ignite(ManualBattle b)
        {
            var source=Deploy(b,Deep[0],1);
            Damage.Invoke(b,new object[]{b.Defender,1f,CombatDamageType.Arts,false,source,1f});
            return b.DeepBurnPower(b.Defender.EntityId);
        }

        [Test]public void AdditionalOneStarMemberImprovesSameTierReunionSupportInsteadOfDilutingIt()
        {
            var pair=Battle(P("CARD_M07",3),P("CARD_M08",3));
            var three=Battle(P("CARD_M07",3),P("CARD_M08",3),P("CARD_M09"));
            var before=Muster(pair,new[]{"CARD_M07","CARD_M08"}).Single();
            var after=Muster(three,new[]{"CARD_M07","CARD_M08"}).Single();
            Assert.That(pair.ReunionTier,Is.EqualTo(2));Assert.That(three.ReunionTier,Is.EqualTo(2));
            Assert.That(pair.ReunionTotalStars,Is.EqualTo(6));Assert.That(three.ReunionTotalStars,Is.EqualTo(7));
            Assert.That(before.MaxHealth,Is.EqualTo(139));Assert.That(after.MaxHealth,Is.EqualTo(150));
            Assert.That(pair.UnitCombat(before).UnitAttack,Is.EqualTo(24));Assert.That(three.UnitCombat(after).UnitAttack,Is.EqualTo(26));
            three.Advance(20);Assert.That(three.Clock,Is.Zero);Assert.That(three.ReunionTotalStars,Is.EqualTo(7));
            three.Restart();Assert.That(three.ReunionTotalStars,Is.EqualTo(7));Assert.That(three.MusterUsed,Is.Zero);
        }

        [Test]public void SnapshotCountsAllSelectedKindsOnceAndIgnoresOutsidersOrLaterPieceEdits()
        {
            var selected=P("CARD_M07",4);
            var b=Battle(selected,P("CARD_M07"),P("CARD_M08",3),P("CARD_M09"),P("CARD_M26",3));
            Assert.That(b.ReunionCount,Is.EqualTo(3));Assert.That(b.ReunionTotalStars,Is.EqualTo(8));
            Assert.That(b.SnowTotalStars,Is.Zero);Assert.That(b.DeepTotalStars,Is.Zero);
            selected.Star=1;Assert.That(b.ReunionTotalStars,Is.EqualTo(8),"The battle owns a value snapshot, not mutable shop pieces.");
            var support=Muster(b,new[]{"CARD_M07","CARD_M08"}).Single();
            Assert.That(support.MaxHealth,Is.EqualTo(160));Assert.That(b.UnitCombat(support).UnitAttack,Is.EqualTo(28));
        }

        [TestCase(3,4)][TestCase(5,6)][TestCase(7,8)]
        public void AddingOneMemberAcrossARealThresholdImprovesTheFirstMusterBudget(int previous,int next)
        {
            var low=Battle(Reunion.Take(previous).Select(id=>P(id)).ToArray());
            var high=Battle(Reunion.Take(next).Select(id=>P(id)).ToArray());
            var a=Muster(low,Reunion);var b=Muster(high,Reunion);
            Assert.That(high.ReunionTier,Is.GreaterThan(low.ReunionTier));
            Assert.That(high.ReunionTotalStars,Is.EqualTo(next));
            Assert.That(b.Sum(u=>u.MaxHealth),Is.GreaterThan(a.Sum(u=>u.MaxHealth)));
            Assert.That(b.Sum(u=>high.UnitCombat(u).UnitAttack),Is.GreaterThan(a.Sum(u=>low.UnitCombat(u).UnitAttack)));
        }

        [Test]public void EightOneCostFourStarsUseThirtyTwoTotalStarsWithoutCountingSplitsOrReinforcements()
        {
            var ids=new[]{"CARD_M02","CARD_M03","CARD_M07","CARD_M08","CARD_M09","CARD_M11","CARD_M15","CARD_M21"};
            var b=Battle(ids.Select(id=>P(id,4)).ToArray());var support=Muster(b,ids);
            Assert.That(b.ReunionTier,Is.EqualTo(8));Assert.That(b.ReunionTotalStars,Is.EqualTo(32));
            Assert.That(b.ReunionStarMultiplier,Is.EqualTo(2));Assert.That(support.Length,Is.EqualTo(2));
            var melee=support.Single(u=>u.CardId=="CARD_M07");var ranged=support.Single(u=>u.CardId=="CARD_M08");
            Assert.That(melee.MaxHealth,Is.EqualTo(400));Assert.That(b.UnitCombat(melee).UnitAttack,Is.EqualTo(72));
            Assert.That(ranged.MaxHealth,Is.EqualTo(280));Assert.That(b.UnitCombat(ranged).UnitAttack,Is.EqualTo(76));
            Assert.That(b.ReunionTotalStars,Is.EqualTo(32));Assert.That(b.SnowTotalStars,Is.EqualTo(4));
        }

        [Test]public void AdditionalOneStarSnowMemberImprovesActualTwoKindShatterAtTheSameTier()
        {
            var pair=Battle(P("CARD_M11",3),P("CARD_M12",3));
            var three=Battle(P("CARD_M11",3),P("CARD_M12",3),P("CARD_M06"));
            foreach(var b in new[]{pair,three}){Deploy(b,"CARD_M11",1);Deploy(b,"CARD_M12",2);}
            Assert.That(pair.SnowTier,Is.EqualTo(2));Assert.That(three.SnowTier,Is.EqualTo(2));
            Assert.That(pair.SnowTotalStars,Is.EqualTo(6));Assert.That(three.SnowTotalStars,Is.EqualTo(7));
            float before=NextShatter(pair),after=NextShatter(three);
            Assert.That(before,Is.EqualTo(77.59228f).Within(.002));
            Assert.That(after,Is.EqualTo(82.52722f).Within(.002));Assert.That(after,Is.GreaterThan(before));
        }

        [Test]public void FourStarSnowMemberFeedsTheSameTotalStarSnapshotToShatterAndWeather()
        {
            var b=Battle(Snow.Select(id=>P(id,id=="CARD_M11"?4:3)).ToArray());
            Assert.That(b.SnowTier,Is.EqualTo(5));Assert.That(b.SnowTotalStars,Is.EqualTo(16));
            Deploy(b,"CARD_M11",1);Deploy(b,"CARD_M12",2);
            Assert.That(NextShatter(b),Is.EqualTo(191.09149f).Within(.003));
            NextShatter(b);NextShatter(b);Assert.That(b.SnowstormActive,Is.True);
            foreach(var u in b.Units.Where(u=>!u.SynergySupport&&u.Active).ToArray())
                Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.True);
            float health=b.Defender.Health;b.Advance(.1f);
            Assert.That(b.SnowstormPulses.Count,Is.EqualTo(1));
            Assert.That(health-b.Defender.Health,Is.EqualTo(414.03156f).Within(.003));
            Assert.That(b.SnowTotalStars,Is.EqualTo(16),"Entity deaths cannot change the locked total.");
        }

        [Test]public void AdditionalOneStarDeepMemberImprovesTheSameTierAndFourthMemberUnlocksTheNewBudget()
        {
            var pair=Battle(P(Deep[0],3),P(Deep[1],3));
            var three=Battle(P(Deep[0],3),P(Deep[1],3),P(Deep[2]));
            var four=Battle(P(Deep[0],3),P(Deep[1],3),P(Deep[2]),P(Deep[3]));
            Assert.That(pair.DeepTier,Is.EqualTo(2));Assert.That(three.DeepTier,Is.EqualTo(2));Assert.That(four.DeepTier,Is.EqualTo(4));
            Assert.That(pair.DeepTotalStars,Is.EqualTo(6));Assert.That(three.DeepTotalStars,Is.EqualTo(7));Assert.That(four.DeepTotalStars,Is.EqualTo(8));
            float a=Ignite(pair),b=Ignite(three),c=Ignite(four);
            Assert.That(a,Is.EqualTo(21.633743f).Within(.001));Assert.That(b,Is.EqualTo(22.774418f).Within(.001));
            Assert.That(c,Is.EqualTo(31.498026f).Within(.001));Assert.That(b,Is.GreaterThan(a));Assert.That(c,Is.GreaterThan(b));
            four.Restart();Assert.That(four.DeepTotalStars,Is.EqualTo(8));Assert.That(four.DeepBurnRemaining(four.Defender.EntityId),Is.Zero);
            Assert.Throws<ArgumentException>(()=>four.ConfigureSynergies(new[]{P(Deep[0],4),P(Deep[1])}));
            Assert.That(four.DeepTotalStars,Is.EqualTo(8),"A rejected four-star non-one-cost snapshot cannot replace the valid one.");
        }
    }
}
