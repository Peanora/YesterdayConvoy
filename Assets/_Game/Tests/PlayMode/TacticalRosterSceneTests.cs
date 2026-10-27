using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class TacticalRosterSceneTests
    {
        static void AssertUndistorted(Transform visual)
        {
            var camera=Camera.main;
            var material=visual.GetComponent<SpriteRenderer>().sharedMaterial;
            Assert.That(material.shader.name,Is.EqualTo("YesterdayConvoy/Tactical Actor"));
            Assert.That(material.shader.isSupported,Is.True);
            Vector2 origin=camera.WorldToScreenPoint(visual.position);
            Vector2 right=(Vector2)camera.WorldToScreenPoint(visual.TransformPoint(Vector3.right))-origin;
            Vector2 up=(Vector2)camera.WorldToScreenPoint(visual.TransformPoint(Vector3.up))-origin;
            Assert.That(Mathf.Abs(right.y),Is.LessThan(.02f),"Horizontal sprite axis must stay horizontal at off-axis gates.");
            Assert.That(Mathf.Abs(up.x),Is.LessThan(.02f),"Vertical sprite axis must not lean toward the vanishing point.");
            Assert.That(right.magnitude/up.magnitude,Is.EqualTo(1).Within(.002f),"Preserve the authored pixel aspect ratio.");
        }

        [UnityTest]public IEnumerator PreparationIdlesAnimateWithoutAdvancingCombatAndRemainContinuousAtBegin()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in new[]{"city","ruins"})
                {
                    c.TryEnterTacticalMap(id);yield return null;
                    var guards=Object.FindAnyObjectByType<DefenderRosterView>();
                    var before=c.Battle.Defenders.Select(d=>guards.FindVisual(d.EntityId).sprite).ToArray();
                    int dp=c.Battle.DP;c.Tick(.73f);yield return null;
                    Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(c.Battle.Clock,Is.Zero);
                    Assert.That(c.Battle.DP,Is.EqualTo(dp));Assert.That(c.Battle.CombatShots,Is.Empty);Assert.That(c.Battle.Units,Is.Empty);
                    var moving=c.Battle.Defenders.Select(d=>guards.FindVisual(d.EntityId).sprite).ToArray();
                    Assert.That(moving.Where((s,i)=>s!=before[i]).Count(),Is.EqualTo(8),"All eight original idle clips advance before Begin.");
                    c.StartBattle();yield return null;
                    CollectionAssert.AreEqual(moving,c.Battle.Defenders.Select(d=>guards.FindVisual(d.EntityId).sprite).ToArray(),"Starting must not reset idle pose.");
                    c.Battle.SetPaused(true);c.Tick(3);yield return null;
                    CollectionAssert.AreEqual(moving,c.Battle.Defenders.Select(d=>guards.FindVisual(d.EntityId).sprite).ToArray(),"Battle pause freezes idle too.");
                    c.RestartBattle();yield return null;c.Tick(.43f);yield return null;
                    Assert.That(c.Battle.Clock,Is.Zero);Assert.That(guards.FindVisual(c.Battle.Defenders[0].EntityId).sprite,Is.Not.Null);
                    c.ReturnToMonsterTests();yield return null;yield return null;
                }
            }
            finally{Time.timeScale=previous;}
        }

        [UnityTest]public IEnumerator SpriteAspectAndAxesStayCorrectAtEveryGateAndGuardPosition()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in new[]{"city","ruins"})
                {
                    c.TryEnterTacticalMap(id);yield return null;
                    var guards=Object.FindAnyObjectByType<DefenderRosterView>();
                    foreach(var d in c.Battle.Defenders)AssertUndistorted(guards.FindVisual(d.EntityId).transform);
                    foreach(var lane in c.Battle.Lanes)
                    {
                        c.RestartBattle();c.StartBattle();c.Battle.SetPaused(true);c.SelectCard(true);
                        var receipt=c.TryDeployAt(lane.Entry,true);Assert.That(receipt.Success,Is.True);yield return null;
                        var visual=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.transform.root.name==receipt.EntityId);
                        AssertUndistorted(visual.transform);
                        Assert.That(visual.transform.localScale,Is.EqualTo(Vector3.one));
                        Assert.That(visual.transform.parent.position,Is.EqualTo(lane.Entry));
                        if(Mathf.Abs(c.Battle.Units[0].Motion.Direction.x)>.08f)
                            Assert.That(visual.Renderer.flipX,Is.EqualTo(c.Battle.Units[0].Motion.Direction.x<0),"Paused spawn faces its own route immediately.");
                    }
                    c.ReturnToMonsterTests();yield return null;yield return null;
                }
            }
            finally{Time.timeScale=previous;}
        }

        [UnityTest]public IEnumerator TalulahChangesHorizontalFacingAtRouteCornersWithoutRotatingLogicRoot()
        {
            float previous=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.TryEnterTacticalMap("city");
                var route=new[]{Vector3.zero,new Vector3(1,0,0),new Vector3(-1,0,0),new Vector3(-1,0,2)};
                var battle=new ManualBattle(c.Definition.Snapshot(),route,new Vector3(100,0,100),c.Battle.CombatSettings);
                battle.Begin();var receipt=battle.TryDeploy(battle.RunId,1,route[0]);Assert.That(receipt.Success,Is.True);
                var unit=battle.Units[0];root=new GameObject("Talulah facing test");
                var child=new GameObject("visual",typeof(SpriteRenderer),typeof(DemoSpriteVisual));child.transform.SetParent(root.transform,false);
                var visual=child.GetComponent<DemoSpriteVisual>();visual.WorldUpright=true;visual.Bind(battle,unit,c.Definition.Card);
                yield return null;Assert.That(visual.Renderer.flipX,Is.False);
                unit.Motion.Advance(1.1f/unit.Motion.Speed);yield return null;Assert.That(visual.Renderer.flipX,Is.True);
                unit.Motion.Advance(2.0f/unit.Motion.Speed);yield return null;Assert.That(visual.Renderer.flipX,Is.True,"Vertical movement keeps the last lateral facing.");
                Assert.That(root.transform.rotation,Is.EqualTo(Quaternion.identity));Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=previous;}
        }

        [UnityTest]public IEnumerator FourRealCardsDragDeployDuringPauseAndResetIndependentlyOnBothMaps()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in new[]{"city","ruins"})
                {
                    Assert.That(c.TryEnterTacticalMap(id),Is.True);yield return null;
                    Assert.That(c.RosterCount,Is.EqualTo(4));Assert.That(c.Battle.Cards.Count,Is.EqualTo(4));
                    var map=Resources.Load<DefenderTrialDefinition>("MapTrials/"+id);Assert.That(map.raised.Length,Is.EqualTo(id=="city"?110:116));
                    c.StartBattle();c.Battle.SetPaused(true);int expectedDP=24;
                    for(int i=0;i<4;i++)
                    {
                        if(i==3 && c.Battle.Lanes.Count==3){c.Battle.SetPaused(false);c.Tick(1);c.Battle.SetPaused(true);}
                        var data=c.RosterDefinition(i);Vector2 entry=Camera.main.WorldToScreenPoint(c.Battle.Lanes[i%c.Battle.Lanes.Count].Entry);
                        Vector2 card=c.CardSlot(i).center;
                        Assert.That(c.ProcessPointerEvent(EventType.MouseDown,new Vector2(card.x*Screen.width/1280,card.y*Screen.height/720),0),Is.True);
                        Assert.That(c.ProcessPointerEvent(EventType.MouseDrag,new Vector2(entry.x,Screen.height-entry.y),0),Is.True);
                        Assert.That(c.ProcessPointerEvent(EventType.MouseUp,new Vector2(entry.x,Screen.height-entry.y),0),Is.True,id+"/"+data.Card.DisplayName);
                        expectedDP-=data.Snapshot().DeploymentCost;Assert.That(c.Battle.DP,Is.EqualTo(expectedDP));
                        Assert.That(c.Battle.Units[i].CardId,Is.EqualTo(data.Card.CardId));
                        Assert.That(c.Battle.FindCard(data.Card.CardId).RemainingUses,Is.EqualTo(data.Snapshot().DeploymentLimit-1));
                    }
                    double clock=c.Battle.Clock;c.Tick(20);yield return null;
                    Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.Units.Count,Is.EqualTo(4));
                    var views=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None);
                    c.OpenSkillMenu(c.Battle.Units[0].EntityId);Assert.That(c.SkillMenuOpen,Is.True);
                    foreach(var unit in c.Battle.Units)
                    {
                        var visual=views.First(v=>v.transform.root.name==unit.EntityId);
                        Assert.That(visual.WorldUpright,Is.True);Assert.That(visual.Renderer.sprite,Is.Not.Null);
                        if(unit.CardId!=c.Definition.Card.CardId)
                        {Assert.That(unit.SkillSP,Is.Zero);Assert.That(c.Battle.IsSkillReady(unit.EntityId),Is.False);c.OpenSkillMenu(unit.EntityId);Assert.That(c.SkillMenuOpen,Is.False);}
                    }
                    var guards=Object.FindAnyObjectByType<DefenderRosterView>();
                    Assert.That(c.Battle.Defenders.Any(d=>guards.FacingBack(d.EntityId)),Is.True);
                    Assert.That(c.Battle.Defenders.Any(d=>!guards.FacingBack(d.EntityId)),Is.True);
                    foreach(var d in c.Battle.Defenders)
                    {var view=guards.FindVisual(d.EntityId);Assert.That(view.transform.position,Is.EqualTo(d.Position));Assert.That(view.sprite.name,Does.StartWith(d.Settings.ArtId+(guards.FacingBack(d.EntityId)?"B":"")+"/idle/"));}
                    c.RestartBattle();Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Battle.DP,Is.EqualTo(24));
                    foreach(var card in c.Battle.Cards){Assert.That(card.RemainingUses,Is.EqualTo(card.Settings.DeploymentLimit));Assert.That(c.Battle.CardCooldown(card.Settings.CardId),Is.Zero);}
                    c.ReturnToMonsterTests();yield return null;yield return null;Assert.That(c.Battle.Cards,Is.Empty);
                }
            }
            finally{Time.timeScale=previous;}
        }
        [UnityTest]public IEnumerator ARealGuardAttackKeepsItsFacingWhenTargetDiesMidAction()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();c.TryEnterTacticalMap("city");c.SelectCard(true);c.TryDeployAt(c.Battle.Entry);
                DefenderState actor=null;
                for(int i=0;i<100 && actor==null;i++)
                {c.Tick(.1f);yield return null;actor=c.Battle.Defenders.FirstOrDefault(d=>d.Alive && c.Battle.Clock-d.LastActionTime>=0 && c.Battle.Clock-d.LastActionTime<d.Settings.ActionDuration-.2f);}
                Assert.That(actor,Is.Not.Null);var guards=Object.FindAnyObjectByType<DefenderRosterView>();
                var shot=c.Battle.CombatShots.Last(s=>s.FromDefender && s.SourceId==actor.EntityId);
                bool back=guards.FacingBack(actor.EntityId),flip=guards.FindVisual(actor.EntityId).flipX;double started=actor.LastActionTime;
                if(Mathf.Abs(shot.Destination.x-actor.Position.x)>.08f)Assert.That(flip,Is.EqualTo(shot.Destination.x<actor.Position.x));
                Assert.That(shot.Healing,Is.False);c.Battle.TryDamageUnit(shot.TargetId,999999,CombatDamageType.Physical);c.Tick(.1f);yield return null;
                Assert.That(actor.LastActionTime,Is.EqualTo(started));Assert.That(guards.FacingBack(actor.EntityId),Is.EqualTo(back));Assert.That(guards.FindVisual(actor.EntityId).flipX,Is.EqualTo(flip));
            }
            finally{Time.timeScale=previous;}
        }
    }
}
