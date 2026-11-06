using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class BlackForestTests
    {
        static ManualBattle Model()
        {
            var lanes=BlackForestLayout.Lanes().Select(l=>new DeploymentLane(l.id,l.exitId,l.points)).ToArray();
            return new ManualBattle(new ManualBattleSettings("CARD","unit",30,3,10,1,.1f,100,10,1,1,1,1),lanes[0].Points,Vector3.one*1000,null,null,null,lanes);
        }
        [Test]public void ThreeInterwovenLanesKeepTheirOwnExitAndConsumeOneSharedDefensePool()
        {
            BlackForestLayout.Validate();var b=Model();b.Begin();
            foreach(var lane in b.Lanes){Assert.That(b.TryDeploy(b.RunId,b.Units.Count+1,lane.Entry).Success,Is.True);b.Advance(.2f);}
            Assert.That(b.Units.Select(u=>u.RouteId),Is.EqualTo(new[]{"R1","R2","R3"}));
            b.Advance(10);
            Assert.That(b.Units.All(u=>u.Resolution==UnitResolution.Breached),Is.True);
            for(int i=0;i<3;i++){Assert.That(b.Units[i].ExitId,Is.EqualTo(i==1?"B2":"B1"));Assert.That(Vector3.Distance(b.Units[i].Motion.Position,b.Lanes[i].Exit),Is.LessThan(.001));Assert.That(b.Units[i].Health,Is.EqualTo(100));}
            Assert.That(b.Breaches,Is.EqualTo(3));Assert.That(b.Defense,Is.Zero);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));
            b.Restart();Assert.That(b.Defense,Is.EqualTo(3));Assert.That(b.Units,Is.Empty);
        }
        [Test]public void RepeatedUseOfOneExitCanWinWithoutBreakingTheOtherExit()
        {
            var b=Model();b.Begin();for(int i=0;i<3;i++){Assert.That(b.TryDeploy(b.RunId,i+1,b.Lanes[0].Entry).Success,Is.True);b.Advance(.2f);}b.Advance(10);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));Assert.That(b.Units.All(u=>u.ExitId=="B1"),Is.True);
        }
        [Test]public void EmptyTerrainRosterRequiresExplicitOptInAndDoesNotCreateAFallbackDefender()
        {
            var data=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            Assert.Throws<ArgumentException>(()=>new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),null,data.Snapshot(),data.LaneSnapshot()));
            var battle=new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),null,data.Snapshot(),data.LaneSnapshot(),allowEmptyRoster:true);
            Assert.That(battle.Defenders,Is.Empty);battle.Restart();Assert.That(battle.Defenders,Is.Empty);
        }
        [Test]public void PausingAtACrossingDoesNotAdvanceOrChangeLane()
        {
            var b=Model();b.Begin();b.TryDeploy(b.RunId,1,b.Lanes[1].Entry);b.Advance(2);
            var u=b.Units[0];Vector3 at=u.Motion.Position;double clock=b.Clock;b.SetPaused(true);b.Advance(60);
            Assert.That(u.Motion.Position,Is.EqualTo(at));Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.RouteId,Is.EqualTo("R2"));
            b.SetPaused(false);b.Advance(10);Assert.That(u.ExitId,Is.EqualTo("B2"));Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Breached));
        }
        [Test]public void AuthoredMapHasPersistentAssetsAndNoRuntimeTerrainGenerator()
        {
            EditorSceneManager.OpenScene(BlackForestMapTools.ScenePath);var data=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            Assert.That(data,Is.Not.Null);Assert.That(data.terrainOnly,Is.True);Assert.That(data.Snapshot(),Is.Empty);
            var tiles=UnityEngine.Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None);Assert.That(tiles.Length,Is.EqualTo(238));
            foreach(var type in new[]{BlackForestSurface.Blackflow,BlackForestSurface.Amber,BlackForestSurface.RootVein})Assert.That(tiles.Count(t=>t.surface==type),Is.GreaterThan(0));
            foreach(var r in UnityEngine.Object.FindAnyObjectByType<AuthoredBattlefield>().GetComponentsInChildren<Renderer>())Assert.That(AssetDatabase.Contains(r.sharedMaterial),Is.True,r.name);
            Assert.That(GameSceneCatalog.BuildPaths(),Does.Contain(BlackForestMapTools.ScenePath));
        }
        [Test]public void EditorTilePositionSurvivesSaveAndReopenInAnIndependentScene()
        {
            const string temporary="Assets/Scenes/BlackForestEditRoundtrip.unity";
            Assert.That(File.Exists(temporary),Is.False,"Never overwrite an existing editor scene.");
            try{
                Assert.That(AssetDatabase.CopyAsset(BlackForestMapTools.ScenePath,temporary),Is.True);
                var s=EditorSceneManager.OpenScene(temporary);var tile=UnityEngine.Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).First(t=>t.coordinate==new Vector2Int(8,8));
                Vector3 position=tile.transform.position+Vector3.up*.15f;tile.transform.position=position;EditorSceneManager.MarkSceneDirty(s);Assert.That(EditorSceneManager.SaveScene(s),Is.True);
                EditorSceneManager.OpenScene(BlackForestMapTools.ScenePath);EditorSceneManager.OpenScene(temporary);
                tile=UnityEngine.Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).First(t=>t.coordinate==new Vector2Int(8,8));Assert.That(tile.transform.position,Is.EqualTo(position));
            }finally{EditorSceneManager.OpenScene(BlackForestMapTools.ScenePath);AssetDatabase.DeleteAsset(temporary);}
        }
    }
}
