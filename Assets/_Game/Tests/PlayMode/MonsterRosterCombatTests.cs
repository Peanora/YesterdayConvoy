using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterRosterCombatTests
    {
        private ManualBattleController c;
        private float previous;
        [SetUp] public void Setup() { previous=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=previous; }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
            c=Object.FindAnyObjectByType<ManualBattleController>();
        }
        private string[] Ids() => c.MonsterTests.Where(x=>x.Playable && x.Id.StartsWith("CARD_M")).Select(x=>x.Id).ToArray();
        private DeployedUnitState Deploy()
        { c.SelectCard(true);var r=c.TryDeployAt(c.Battle.Entry);Assert.That(r.Success,Is.True);return c.Battle.FindUnit(r.EntityId); }

        [UnityTest] public IEnumerator RegisteredRosterCanDeployFreezeAttackWinAndRestartIndependently()
        {
            yield return Load();Assert.That(Ids().Length,Is.EqualTo(37));
            for(int i=11;i<=38;i++) Assert.That(Ids(),Does.Contain("CARD_M"+i),"Approved batch must be registered, not silently pending.");
            foreach(string id in Ids())
            {
                Assert.That(c.TryEnterMonsterTest(id),Is.True);var unit=Deploy();yield return null;
                var view=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.transform.parent.name==unit.EntityId);
                Assert.That(view.Renderer.sprite.name,Does.StartWith(id.Substring(5)+"/"));
                var position=unit.Motion.Position;c.Battle.SetPaused(true);int frame=view.FrameIndex;c.Tick(3);yield return null;
                Assert.That(unit.Motion.Position,Is.EqualTo(position));Assert.That(view.FrameIndex,Is.EqualTo(frame));
                Assert.That(unit.SkillSP,Is.Zero);Assert.That(c.Battle.CheckSkill(unit.EntityId),Is.EqualTo(SkillFailure.NoSkill));
                c.Battle.SetPaused(false);c.Tick(1.1f);yield return null;Deploy();c.Tick(1.1f);yield return null;Deploy();
                c.ToggleSpeed();bool attacked=false;
                for(int i=0;i<1200 && c.Battle.Phase==BattlePhase.Running;i++)
                { c.Tick(.03f);yield return null;attacked|=c.Battle.CombatShots.Any(s=>!s.FromDefender)||c.Battle.FlameSprays.Any(s=>s.Ticks>0); }
                Assert.That(attacked,Is.EqualTo(c.Battle.CombatSettings.CanAttack),id);Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),id);
                Assert.That(c.Battle.Breaches,Is.GreaterThan(0),id);Assert.That(c.Battle.SkillCasts,Is.Zero);
                c.ReturnToMonsterTests();yield return null;Assert.That(c.ViewCount,Is.Zero);Assert.That(c.Battle.CombatShots,Is.Empty);
                c.TryEnterMonsterTest(id);Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.DP,Is.EqualTo(12));
                Assert.That(c.Battle.Defender.Health,Is.EqualTo(c.Battle.CombatSettings.DefenderHealth));
                Debug.Log("ROSTER_COMBAT_PASS: "+id);
            }
        }

        [UnityTest] public IEnumerator EveryRegisteredArtPlaysActualDeathWithoutMovingItsLogicalRoot()
        {
            yield return Load();
            foreach(string id in Ids())
            {
                c.TryEnterMonsterTest(id);var card=c.Definition.Card;
                var settings=new ManualBattleSettings(id,card.Unit.UnitId,12,1,1,1,1,1,card.Unit.MoveSpeed,1,2,1,1);
                var combat=new EngagementSettings(unitAttack:1,unitRange:1,defenderHealth:1000,defenderAttack:100,defenderRange:5,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,canAttack:c.Battle.CombatSettings.CanAttack);
                var battle=new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*4},Vector3.forward,combat);
                battle.Begin();var receipt=battle.TryDeploy(battle.RunId,1,battle.Entry);var unit=battle.FindUnit(receipt.EntityId);
                var root=new GameObject("Isolated death "+id);var child=new GameObject("Approved death",typeof(SpriteRenderer),typeof(DemoSpriteVisual));child.transform.SetParent(root.transform,false);
                double presentation=0;var view=child.GetComponent<DemoSpriteVisual>();view.Bind(battle,unit,card,()=>presentation);
                battle.Advance(.4f);presentation=battle.Clock;yield return null;
                Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(view.FrameState,Is.EqualTo("Death"),id);
                Assert.That(view.Renderer.sprite.name,Does.StartWith(id.Substring(5)+"/death/"));
                var position=unit.Motion.Position;int index=view.FrameIndex;presentation+=.2;yield return null;
                Assert.That(view.FrameIndex,Is.GreaterThan(index));Assert.That(unit.Motion.Position,Is.EqualTo(position));
                Object.Destroy(root);yield return null;
            }
        }
    }
}
