using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class GuerrillaDamageDisplayTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] Members={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,10000,.001f,1,1,1,1);
        static EngagementSettings Combat(float attack)=>new EngagementSettings(unitAttack:attack,unitArmor:0,unitRange:.01f,unitInterval:1000,
            defenderRange:.001f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
            projectileTime:.01f,hasSkill:false);
        static ManualBattle Fixture(bool guerrilla=true)
        {
            var guard=new DefenderSettings("guard","guard",null,Vector3.right*10,10000,1,0,.001f,1000,canAttack:false);
            var b=new ManualBattle(Unit(Members[0]),new[]{Vector3.zero,Vector3.forward*1000},guard.Position,Combat(10),roster:new[]{guard});
            b.ConfigureCardLoadout(Members.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),Combat(30),true)));
            if(guerrilla)b.ConfigureSynergies(Members.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),extraGuerrillaMembers:new[]{"CARD_M07","CARD_M08"});
            return b;
        }
        static GuerrillaOrderVolley StartOrder(ManualBattle b,bool second=false)
        {
            b.Begin();Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M03").Success,Is.True);
            if(second)Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M07").Success,Is.True);
            b.Advance(.05f,0);Assert.That(b.LatestGuerrillaOrder,Is.Not.Null);
            Assert.That(b.TotalPlayerDamage,Is.Zero);return b.LatestGuerrillaOrder;
        }
        static void AssertDisplay(ManualBattle b)
        {
            var shown=b.DisplayDamageRanking;
            Assert.That(shown.Count(r=>r.CardId==ManualBattle.GuerrillaDamageCardId),Is.EqualTo(1));
            Assert.That(shown.Single(r=>r.CardId==ManualBattle.GuerrillaDamageCardId).Damage,Is.EqualTo(b.TotalGuerrillaDamage).Within(.001));
            Assert.That(shown.Sum(r=>r.Damage),Is.EqualTo(b.TotalPlayerDamage).Within(.01));
            Assert.That(shown.All(r=>r.Damage>=0),Is.True);
            Assert.That(shown.Select(r=>r.CardId).Distinct().Count(),Is.EqualTo(shown.Count));
            Assert.That(shown.Select(r=>r.Damage),Is.Ordered.Descending);
        }

        [Test]public void PureFiveStrikeDamageAppearsOnlyInOneGuerrillaRowAndKeepsTheRawContributors()
        {
            var b=Fixture();var order=StartOrder(b,true);b.Advance(2.8f,2.8f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(5));
            Assert.That(order.Hits.Sum(hit=>hit.Damage),Is.EqualTo(120).Within(.01));
            Assert.That(b.Defender.MaxHealth-b.Defender.Health,Is.EqualTo(120).Within(.01));
            Assert.That(b.DamageRanking.Single(r=>r.CardId=="CARD_M03").Damage,Is.EqualTo(30).Within(.01));
            Assert.That(b.DamageRanking.Single(r=>r.CardId=="CARD_M07").Damage,Is.EqualTo(90).Within(.01));
            var shown=b.DisplayDamageRanking;
            Assert.That(shown[0].CardId,Is.EqualTo(ManualBattle.GuerrillaDamageCardId));
            Assert.That(shown.Single(r=>r.CardId=="CARD_M03").Damage,Is.Zero);
            Assert.That(shown.Single(r=>r.CardId=="CARD_M07").Damage,Is.Zero);
            Assert.That(b.DamageRanking.Any(r=>r.CardId==ManualBattle.GuerrillaDamageCardId),Is.False);
            AssertDisplay(b);
        }

        [Test]public void ARealOrdinaryImpactAndSharedLossesAreSeparatedFromFiveStrikePursuitWithoutDoubleCounting()
        {
            var b=Fixture();var order=StartOrder(b);b.Advance(2.8f,2.8f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(5));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(30).Within(.01));
            var source=b.Units.Single();
            // Restore one reachable ordinary attack opportunity; targeting, launch and impact remain production code.
            typeof(DefenderState).GetProperty("Position").SetValue(b.Defender,source.Motion.Position+Vector3.right*.005f);
            b.Advance(.2f,0);
            var ordinary=b.CombatShots.Where(s=>!s.FromDefender&&s.Applied&&s.SourceId==source.EntityId).ToArray();
            Assert.That(ordinary,Has.Length.EqualTo(1));Assert.That(ordinary.Single().DamageDealt,Is.EqualTo(10).Within(.001));
            // Explicit unidentified sources exercise the existing shared HP-loss sink, including a pursuit portion.
            typeof(ManualBattle).GetMethod("DamageDefenderFrom",Hidden).Invoke(b,
                new object[]{b.Defender,11f,CombatDamageType.Physical,null,true,1f});
            typeof(ManualBattle).GetMethod("DamageGuerrillaDefenderFrom",Hidden).Invoke(b,
                new object[]{b.Defender,20f,CombatDamageType.Physical,null,.15f});
            Assert.That(b.Defender.MaxHealth-b.Defender.Health,Is.EqualTo(54).Within(.01));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(54).Within(.01));
            var shown=b.DisplayDamageRanking;
            Assert.That(shown.Select(r=>r.CardId),Is.EqualTo(new[]{ManualBattle.GuerrillaDamageCardId,ManualBattle.SharedDamageCardId,"CARD_M03"}));
            Assert.That(shown.Select(r=>r.Damage),Is.EqualTo(new[]{33f,11f,10f}).Within(.01));
            Assert.That(b.DamageRanking.Single(r=>r.CardId=="CARD_M03").Damage,Is.EqualTo(40).Within(.01));
            Assert.That(b.DamageRanking.Single(r=>r.CardId==ManualBattle.SharedDamageCardId).Damage,Is.EqualTo(14).Within(.01));
            AssertDisplay(b);
        }

        [Test]public void AGuerrillaFormationHasOneZeroRowBeforeAnyHitButOtherFormationsDoNot()
        {
            var b=Fixture();Assert.That(b.GuerrillaTier,Is.GreaterThan(0));Assert.That(b.DamageRanking,Is.Empty);
            Assert.That(b.DisplayDamageRanking,Has.Count.EqualTo(1));
            Assert.That(b.DisplayDamageRanking.Single().CardId,Is.EqualTo(ManualBattle.GuerrillaDamageCardId));
            Assert.That(b.DisplayDamageRanking.Single().Damage,Is.Zero);AssertDisplay(b);
            var ordinary=Fixture(false);Assert.That(ordinary.GuerrillaTier,Is.Zero);
            Assert.That(ordinary.DisplayDamageRanking,Is.Empty);
        }

        [Test]public void DisplaySnapshotsFreezeDuringPauseCarryAcrossWavesAndResetToTheZeroFormationRow()
        {
            var b=Fixture();b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("R1",16,1),new BattleWaveDefinition("R1",18,1)},10));
            var order=StartOrder(b,true);b.Advance(.9f,.9f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(1));
            var snapshot=b.DisplayDamageRanking;float first=b.TotalGuerrillaDamage;double clock=b.Clock;
            Assert.That(first,Is.GreaterThan(0));
            Assert.That(()=>((IList<BattleDamageEntry>)snapshot).Clear(),Throws.TypeOf<NotSupportedException>());
            b.SetPaused(true);b.Advance(15,15);
            Assert.That(b.Clock,Is.EqualTo(clock));
            Assert.That(b.DisplayDamageRanking.Select(r=>r.CardId),Is.EqualTo(snapshot.Select(r=>r.CardId)));
            Assert.That(b.DisplayDamageRanking.Select(r=>r.Damage),Is.EqualTo(snapshot.Select(r=>r.Damage)));
            b.SetPaused(false);b.Advance(1.9f,1.9f);
            Assert.That(b.FinalAdvanceActive,Is.True);Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));
            Assert.That(order.CompletedStrikes,Is.EqualTo(5));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(120).Within(.01));
            Assert.That(snapshot.Single(r=>r.CardId==ManualBattle.GuerrillaDamageCardId).Damage,Is.EqualTo(first));
            AssertDisplay(b);
            b.Restart();Assert.That(b.TotalPlayerDamage,Is.Zero);Assert.That(b.DamageRanking,Is.Empty);
            Assert.That(b.DisplayDamageRanking,Has.Count.EqualTo(1));
            Assert.That(b.DisplayDamageRanking.Single().Damage,Is.Zero);AssertDisplay(b);
        }
    }
}
