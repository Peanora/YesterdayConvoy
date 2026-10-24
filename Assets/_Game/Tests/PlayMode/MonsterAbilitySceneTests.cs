using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterAbilitySceneTests
    {
        private float previous;
        [SetUp] public void Setup() { previous=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=previous; }
        [UnityTest] public IEnumerator ActualFrostNovaShowsIceRingAndRevivesOnceWithoutInjectedState()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M14"),Is.True);
            c.SelectCard(true);var r=c.TryDeployAt(c.Battle.Entry);Assert.That(r.Success,Is.True);
            var unit=c.Battle.FindUnit(r.EntityId);bool ring=false,reviving=false,revived=false;
            for(int i=0;i<2000 && unit.Active;i++)
            {
                c.Tick(.1f);yield return null;
                var visual=Object.FindObjectsByType<DemoSpriteVisual>().FirstOrDefault(v=>v.transform.parent.name==unit.EntityId);
                if(visual!=null && visual.FrameState=="IceRing")
                {
                    ring=true;Assert.That(visual.Renderer.sprite,Is.Not.Null);
                    Assert.That(c.Battle.IceRings.Count,Is.GreaterThan(0));
                    Assert.That(c.Battle.Defender.FrostSlowUntil,Is.GreaterThan(c.Battle.Clock));
                }
                if(unit.IsReviving && !reviving)
                {
                    reviving=true;double clock=c.Battle.Clock;var position=unit.Motion.Position;
                    c.Battle.SetPaused(true);c.Tick(20);yield return null;
                    Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(unit.Motion.Position,Is.EqualTo(position));
                    c.Battle.SetPaused(false);
                }
                revived |= unit.ReviveUsed && !unit.IsReviving && unit.Health>0;
            }
            Assert.That(ring && reviving && revived,Is.True,$"Natural combat: ring={ring}, reviving={reviving}, revived={revived}, clock={c.Battle.Clock}, hp={unit.Health}.");
            c.RestartBattle();Assert.That(c.Battle.IceRings,Is.Empty);Assert.That(c.Battle.Units,Is.Empty);
            Assert.That(c.Battle.Defender.FrostSlowUntil,Is.LessThan(c.Battle.Clock));
        }
        [UnityTest] public IEnumerator TwoStaggeredIceDeathsFreezeTheActualDefenderAndPausePreservesIt()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M06"),Is.True);
            c.SelectCard(true);var first=c.TryDeployAt(c.Battle.Entry);Assert.That(first.Success,Is.True);
            c.Tick(1.1f);yield return null;c.SelectCard(true);var second=c.TryDeployAt(c.Battle.Entry);Assert.That(second.Success,Is.True);
            var one=c.Battle.FindUnit(first.EntityId);var two=c.Battle.FindUnit(second.EntityId);
            for(int i=0;i<1600 && (one.Active || two.Active);i++) { c.Tick(.025f);yield return null; }
            Assert.That(one.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(two.Resolution,Is.EqualTo(UnitResolution.Dead));
            c.Tick(1.05f);yield return null;Assert.That(c.Battle.Defender.Alive,Is.True);
            Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(c.Battle.Clock));
            c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);int shots=c.Battle.ShotsFired;
            c.Tick(4);yield return null;Assert.That(c.Battle.ShotsFired,Is.EqualTo(shots));
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(10);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(clock));
            c.RestartBattle();Assert.That(c.Battle.Defender.FrozenUntil,Is.LessThan(c.Battle.Clock));
        }

        [UnityTest] public IEnumerator AvailableSpecialMonstersApplyRealDamageAndVisibleEffectsThenClearState()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            var ids=c.MonsterTests.Where(x=>x.Playable && new[]{"CARD_M04","CARD_M05","CARD_M06","CARD_M09"}.Contains(x.Id)).Select(x=>x.Id).ToArray();
            Assert.That(ids.Length,Is.GreaterThan(0));
            foreach(string id in ids)
            {
                c.TryEnterMonsterTest(id);var e=c.Battle.CombatSettings.Effects;
                c.SelectCard(true);var receipt=c.TryDeployAt(c.Battle.Entry);Assert.That(receipt.Success,Is.True);
                var unit=c.Battle.FindUnit(receipt.EntityId);
                if(e.BlastAttack>0)
                {
                    for(int i=0;i<800 && unit.Active;i++) { c.Tick(.025f);yield return null; }
                    Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead),id);
                    Assert.That(c.Battle.HasPendingDeathBlast,Is.True);yield return null;
                    var view=Object.FindAnyObjectByType<MonsterDeathEffectView>();Assert.That(view.VisibleBlasts,Is.GreaterThan(0));
                    c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(10);yield return null;
                    Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.HasPendingDeathBlast,Is.True);
                    c.Battle.SetPaused(false);c.Tick(e.BlastDelay+.05f);yield return null;
                    Assert.That(c.Battle.DeathBlasts.Single().TargetsHit,Is.GreaterThan(0),id);
                    Assert.That(view.VisibleBlasts,Is.GreaterThan(0),"Resolved explosion must remain visibly distinct from death windup.");
                    if(e.ColdDuration>0) Assert.That(c.Battle.Defender.ColdUntil,Is.GreaterThan(c.Battle.Clock));
                }
                else
                {
                    float health=c.Battle.Defender.Health;
                    for(int i=0;i<800 && !c.Battle.CombatShots.Any(s=>!s.FromDefender && s.Applied);i++) { c.Tick(.025f);yield return null; }
                    Assert.That(c.Battle.Defender.Health,Is.LessThan(health),id);
                    if(e.ArmorShred>0) Assert.That(c.Battle.Defender.ArmorReduction,Is.EqualTo(e.ArmorShred));
                    if(e.DamageType==CombatDamageType.Arts)
                        Assert.That(health-c.Battle.Defender.Health,Is.EqualTo(c.Battle.CombatSettings.UnitAttack*(1-e.Resistance/100)).Within(.001));
                }
                c.ReturnToMonsterTests();yield return null;
                Assert.That(c.Battle.DeathBlasts,Is.Empty);Assert.That(c.Battle.Defender.ArmorReduction,Is.Zero);
                Assert.That(c.Battle.Defender.ColdUntil,Is.LessThan(c.Battle.Clock));Assert.That(c.ViewCount,Is.Zero);
                Assert.That(Object.FindAnyObjectByType<MonsterDeathEffectView>().VisibleBlasts,Is.Zero);
                Debug.Log("MONSTER_ABILITY_PASS: "+id);
            }
        }

        [UnityTest] public IEnumerator SnowSniperAndIcecleaverUseActualConfigAndApplyBonusOnlyToFrozenImpact()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            foreach(string id in new[]{"CARD_M11","CARD_M13"})
            {
            Assert.That(c.TryEnterMonsterTest(id),Is.True);
            var e=c.Battle.CombatSettings.Effects;Assert.That(e.FrozenAttackMultiplier,Is.EqualTo(id=="CARD_M11" ? 1.5f : 2.5f));
            c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            // Explicit status fixture: the sniper itself must never generate freeze.
            c.Battle.TryApplyCold(c.Battle.Defender.EntityId,30);c.Battle.TryApplyCold(c.Battle.Defender.EntityId,30);
            for(int i=0;i<1000 && !c.Battle.CombatShots.Any(s=>!s.FromDefender && s.Applied);i++) {c.Tick(.025f);yield return null;}
            var shot=c.Battle.CombatShots.First(s=>!s.FromDefender && s.Applied);
            Assert.That(shot.FrozenBonusApplied,Is.True);
            Assert.That(shot.DamageDealt,Is.EqualTo(id=="CARD_M11" ? 42 : 140).Within(.001));
            Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(c.Battle.Clock));
            c.RestartBattle();Assert.That(c.Battle.Defender.FrozenUntil,Is.LessThan(c.Battle.Clock));
            c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            for(int i=0;i<1000 && !c.Battle.CombatShots.Any(s=>!s.FromDefender && s.Applied);i++) {c.Tick(.025f);yield return null;}
            shot=c.Battle.CombatShots.First(s=>!s.FromDefender && s.Applied);
            Assert.That(shot.FrozenBonusApplied,Is.False);
            Assert.That(shot.DamageDealt,Is.EqualTo(id=="CARD_M11" ? 27 : 62).Within(.001));
            c.ReturnToMonsterTests();Assert.That(c.ViewCount,Is.Zero);
            }
        }

        [UnityTest] public IEnumerator TwoActualSnowCastersCountTheirOwnAttacksAndFreezeWithoutAnInjectedStatus()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M12"),Is.True);
            c.SelectCard(true);var first=c.TryDeployAt(c.Battle.Entry);Assert.That(first.Success,Is.True);c.Tick(1.1f);
            c.SelectCard(true);var second=c.TryDeployAt(c.Battle.Entry);Assert.That(second.Success,Is.True);
            for(int i=0;i<1600 && c.Battle.Defender.FrozenUntil<=c.Battle.Clock && c.Battle.Phase==BattlePhase.Running;i++) {c.Tick(.025f);yield return null;}
            Assert.That(c.Battle.Defender.Alive,Is.True);
            Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(c.Battle.Clock));
            foreach(string id in new[]{first.EntityId,second.EntityId})
            {
                var shots=c.Battle.CombatShots.Where(s=>!s.FromDefender && s.SourceId==id).ToArray();
                Assert.That(shots.Length,Is.GreaterThanOrEqualTo(3));
                Assert.That(shots[0].ColdDuration,Is.Zero);Assert.That(shots[1].ColdDuration,Is.Zero);
                Assert.That(shots[2].ColdDuration,Is.EqualTo(10));Assert.That(shots[2].Applied,Is.True);
            }
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;c.Tick(15);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));
            Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(clock));
            c.RestartBattle();Assert.That(c.Battle.Defender.ColdUntil,Is.LessThan(c.Battle.Clock));
            Assert.That(c.Battle.Defender.FrozenUntil,Is.LessThan(c.Battle.Clock));Assert.That(c.Battle.Units,Is.Empty);
            c.ReturnToMonsterTests();Assert.That(c.ViewCount,Is.Zero);
        }
    }
}
