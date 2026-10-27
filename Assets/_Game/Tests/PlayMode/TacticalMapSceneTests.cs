using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class TacticalMapSceneTests
    {
        [UnityTest] public IEnumerator BothMapsCanResolveCombatSkillVictoryAndReset()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in new[]{"city","ruins"})
                {
                    Assert.That(c.TryEnterTacticalMap(id),Is.True);c.StartBattle();bool skill=false,attacked=false;
                    for(int i=0;i<6000 && c.Battle.Phase==BattlePhase.Running;i++)
                    {
                        if(c.Battle.CooldownRemaining<=0 && c.Battle.RemainingUses>0 && c.Battle.ActiveCount<4)
                        {var lane=c.Battle.Lanes[(12-c.Battle.RemainingUses)%c.Battle.Lanes.Count];c.SelectCard(true);c.TryDeployAt(lane.Entry);}
                        foreach(var u in c.Battle.Units)if(c.Battle.IsSkillReady(u.EntityId)){c.SelectSkillUnit(u.EntityId);skill|=c.ActivateSelectedSkill()==SkillFailure.None;}
                        c.Tick(.1f);attacked|=c.Battle.CombatShots.Count>0;if(i%100==0)yield return null;
                    }
                    Assert.That(attacked && skill,Is.True,id);Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won),id);
                    Assert.That(c.Battle.Defense,Is.Zero);c.RestartBattle();Assert.That(c.Battle.Units.Count,Is.Zero);
                    foreach(var d in c.Battle.Defenders)Assert.That(d.Health,Is.EqualTo(d.MaxHealth));
                    c.ReturnToMonsterTests();yield return null;yield return null;
                }
            }
            finally{Time.timeScale=previous;}
        }
        [UnityTest] public IEnumerator AllSevenGatesAcceptPausedDragAndRepeatedMenuReturnsRestoreScene()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();var camera=Camera.main;
                Vector3 original=camera.transform.position;Quaternion rotation=camera.transform.rotation;
                foreach(string id in new[]{"city","ruins","city"})
                {
                    Assert.That(c.TryEnterTacticalMap(id),Is.True);yield return null;yield return null;
                    Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
                    Assert.That(c.Battle.Defenders.Count,Is.EqualTo(8));
                    Assert.That(Object.FindAnyObjectByType<TacticalMapEnvironment>(),Is.Not.Null);
                    foreach(var lane in c.Battle.Lanes)
                    {
                        c.RestartBattle();c.StartBattle();c.Battle.SetPaused(true);
                        Vector2 screen=camera.WorldToScreenPoint(lane.Entry);float guiY=720-screen.y*720/Screen.height;
                        Assert.That(guiY,Is.InRange(70,540),id+"/"+lane.Id+" must clear HUD");
                        c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));c.ProcessPointerMove(screen);
                        Assert.That(c.ProcessPointerUp(screen).Success,Is.True,id+"/"+lane.Id);
                        Assert.That(c.Battle.Units[0].RouteId,Is.EqualTo(lane.Id));
                        Assert.That(c.Battle.Units[0].ExitId,Is.EqualTo(lane.ExitId));
                        Assert.That(c.Battle.DP,Is.EqualTo(22));c.Tick(10);
                        Assert.That(c.Battle.Clock,Is.Zero);Assert.That(c.Battle.IsPaused,Is.True);
                    }
                    c.ReturnToMonsterTests();yield return null;yield return null;
                    Assert.That(c.TestSelectionOpen,Is.True);Assert.That(camera.transform.position,Is.EqualTo(original));
                    Assert.That(Object.FindAnyObjectByType<TacticalMapEnvironment>(),Is.Null);
                    Assert.That(c.Battle.Lanes.Count,Is.EqualTo(1));
                }
                Assert.That(c.TryEnterTacticalMap("ruins"),Is.True);yield return null;
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
                Assert.That(Camera.main.transform.position,Is.EqualTo(original));
                Assert.That(Object.FindAnyObjectByType<TacticalMapEnvironment>(),Is.Null);
            }
            finally {Time.timeScale=previous;}
        }
    }
}
