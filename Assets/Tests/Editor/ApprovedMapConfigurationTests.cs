using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayConvoy.Editor;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ApprovedMapConfigurationTests
    {
        static ApprovedMapDefinition Map(int index)
        {
            var map=ApprovedMapCatalog.Find("approved-map-"+index.ToString("00"));
            Assert.That(map,Is.Not.Null,"Missing approved map "+index);return map;
        }
        static Vector2 Key(Vector3 point)=>new Vector2(point.x,point.z);
        static bool GroundContains(HashSet<Vector2> ground,Vector3 point)=>ground.Any(cell=>Vector2.Distance(cell,Key(point))<.001f);

        [Test]public void EightIndependentDefinitionsScenesAndPreviewsAreInTheDynamicBuildCatalog()
        {
            var maps=ApprovedMapCatalog.All;Assert.That(maps.Length,Is.EqualTo(8));
            CollectionAssert.AreEqual(Enumerable.Range(1,8),maps.Select(map=>map.index));
            Assert.That(maps.Select(map=>map.id).Distinct().Count(),Is.EqualTo(8));
            var sceneGuids=new HashSet<string>();var definitionGuids=new HashSet<string>();
            foreach(var map in maps)
            {
                Assert.That(map.id,Is.EqualTo("approved-map-"+map.index.ToString("00")));
                Assert.That(ApprovedMapCatalog.Find(map.id),Is.SameAs(map));Assert.That(ApprovedMapCatalog.IsMap(map.id),Is.True);
                Assert.That(map.width,Is.EqualTo(14));Assert.That(map.height,Is.EqualTo(10));
                Assert.That(map.gridRows.Length,Is.EqualTo(map.height));Assert.That(map.gridRows.All(row=>row.Length==map.width),Is.True);
                Assert.That(map.title,Is.Not.Empty);Assert.That(map.preview,Is.Not.Null);
                Assert.That(AssetDatabase.Contains(map.preview),Is.True,"Preview must be a committed Unity asset.");
                string scene="Assets/Scenes/"+map.sceneName+".unity";
                Assert.That(File.Exists(scene),Is.True,scene);Assert.That(GameSceneCatalog.BuildPaths(),Does.Contain(scene));
                Assert.That(sceneGuids.Add(AssetDatabase.AssetPathToGUID(scene)),Is.True,"Copied scenes require independent GUIDs.");
                Assert.That(definitionGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map))),Is.True);
                Assert.That(map.encounter,Is.Not.Null);Assert.That(map.encounter.mapId,Is.EqualTo(map.id));
                Assert.That(map.encounter.terrainOnly,Is.False);Assert.That(map.encounter.Snapshot(),Is.Not.Empty);
                Assert.That(map.encounter.attackWaves,Is.Null,"New map trials must not silently inherit the tree-sea wave rules.");
            }
            Assert.That(ApprovedMapCatalog.IsMap("not-an-approved-map"),Is.False);
        }

        [Test]public void AllTwentyFixedRoutesFollowGroundExceptTheirExplicitUndergroundConnector()
        {
            int routes=0;
            foreach(var map in ApprovedMapCatalog.All)
            {
                var ground=new HashSet<Vector2>(map.WalkablePoints().Select(Key));var lanes=map.encounter.LaneSnapshot();
                Assert.That(lanes.Select(lane=>lane.Id).Distinct().Count(),Is.EqualTo(lanes.Length));
                foreach(var lane in lanes)
                {
                    routes++;Assert.That(lane.Points.Count,Is.GreaterThanOrEqualTo(2));Assert.That(lane.ExitId,Is.Not.Empty);
                    for(int i=0;i<lane.Points.Count;i++)Assert.That(GroundContains(ground,lane.Points[i]),Is.True,map.id+" / "+lane.Id+" point "+i);
                    for(int segment=0;segment<lane.Points.Count-1;segment++)
                    {
                        bool portal=map.mechanics.portals.Any(p=>p.routeId==lane.Id&&segment>=p.sourcePointIndex&&segment<p.destinationPointIndex);
                        if(portal)continue;
                        Vector3 from=lane.Points[segment],to=lane.Points[segment+1];
                        Assert.That(Mathf.Abs(from.x-to.x)<.001f||Mathf.Abs(from.z-to.z)<.001f,Is.True,map.id+" diagonal surface route");
                        float length=Vector3.Distance(from,to);Assert.That(length,Is.GreaterThan(0));
                        Assert.That(length,Is.EqualTo(Mathf.Round(length)).Within(.001f));
                        int steps=Mathf.RoundToInt(length);
                        for(int step=0;step<=steps;step++)
                            Assert.That(GroundContains(ground,Vector3.Lerp(from,to,(float)step/steps)),Is.True,map.id+" / "+lane.Id+" crosses non-ground at segment "+segment);
                    }
                }
                CollectionAssert.AreEqual(map.encounter.route,lanes[0].Points,"Legacy first-route binding must match the first fixed lane.");
            }
            Assert.That(routes,Is.EqualTo(20));
        }

        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void EachSavedSceneHasEditableTilesPersistentAssetsAndItsOwnBootstrap(int index)
        {
            var map=Map(index);EditorSceneManager.OpenScene("Assets/Scenes/"+map.sceneName+".unity");
            var bootstrap=Object.FindAnyObjectByType<ApprovedMapBootstrap>();Assert.That(bootstrap,Is.Not.Null);
            Assert.That(bootstrap.map,Is.SameAs(map));var root=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(root,Is.Not.Null);
            var tiles=root.GetComponentsInChildren<ApprovedMapTile>(true);
            Assert.That(tiles.Length,Is.EqualTo(map.width*map.height));Assert.That(tiles.Select(tile=>tile.cell).Distinct().Count(),Is.EqualTo(tiles.Length));
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty,"Visual terrain must not introduce unapproved physics/line-of-sight rules.");
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
            {Assert.That(material,Is.Not.Null,renderer.name);Assert.That(AssetDatabase.Contains(material),Is.True,renderer.name);Assert.That(material.shader,Is.Not.Null);}
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {Assert.That(filter.sharedMesh,Is.Not.Null,filter.name);Assert.That(AssetDatabase.Contains(filter.sharedMesh),Is.True,filter.name);}
            Assert.That(Camera.main,Is.Not.Null);Assert.That(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x,60),Is.EqualTo(0).Within(.01f));
            Assert.That(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y,0),Is.EqualTo(0).Within(.01f));
        }

        [Test]public void OnlyTheMailMapHasAStablePortalAndOnlyTheHeatMapHasThreePhasedPumps()
        {
            foreach(var map in ApprovedMapCatalog.All)
            {
                Assert.That(map.mechanics,Is.Not.Null);Assert.That(map.mechanics.portals.Length,Is.EqualTo(map.index==4?1:0));
                Assert.That(map.mechanics.heatPumps.Length,Is.EqualTo(map.index==5?3:0));
                var saved=map.encounter;string before=JsonUtility.ToJson(map);
                var model=new ManualBattle(saved.battle.Snapshot(),saved.route,saved.battle.Engagement.DefenderPosition,
                    saved.battle.Engagement.Snapshot(),roster:saved.Snapshot(),deploymentLanes:saved.LaneSnapshot());
                model.ConfigureApprovedMapEffects(map.mechanics);
                foreach(var portal in map.mechanics.portals)
                {
                    Assert.That(portal.transitSeconds,Is.EqualTo(1.5f));
                    Assert.That(model.ApprovedPortalSegment(portal.routeId,portal.sourcePointIndex),Is.True);
                    Assert.That(model.ApprovedPortalSegment(portal.routeId,portal.destinationPointIndex),Is.False);
                }
                Assert.That(model.ApprovedHeatPumpCount,Is.EqualTo(map.mechanics.heatPumps.Length));
                foreach(var pump in map.mechanics.heatPumps)
                {
                    Assert.That(pump.period,Is.EqualTo(10));Assert.That(pump.warning,Is.EqualTo(1.5f));
                    Assert.That(pump.active,Is.EqualTo(2));Assert.That(pump.dps,Is.EqualTo(12));
                    Assert.That(map.WalkablePoints().Any(point=>Vector2.Distance(Key(point),Key(pump.position))<.001f),Is.True);
                }
                if(map.index==5)CollectionAssert.AreEqual(new[]{0f,3.3f,6.6f},map.mechanics.heatPumps.Select(pump=>pump.phase));
                model.Restart();Assert.That(model.ApprovedPortalTrips,Is.Zero);Assert.That(JsonUtility.ToJson(map),Is.EqualTo(before));
            }
        }

        [Test]public void RealMapGroundKeepsMarineContactOnItsHalfCellCentre()
        {
            // Isolated configuration fixture: a single contact checks coordinate conversion,
            // not real encounter difficulty or natural campaign progression.
            foreach(var map in ApprovedMapCatalog.All)
            {
                Assert.That(map.MarineOrigin,Is.EqualTo(new Vector2(.5f,.5f)));
                var lanes=map.encounter.LaneSnapshot();var origin=lanes[0].Entry;
                var settings=new ManualBattleSettings("CARD_M33","marine",100,6,2,1,.1f,1000,.001f,1,1,1,1);
                var combat=new EngagementSettings(unitRange:.001f,defenderRange:.001f,blockCount:0,initialSP:0,requiredSP:0,
                    spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,canAttack:false);
                var guard=new DefenderSettings("remote","remote",null,Vector3.one*1000,1000,1,0,.001f,100,0,canAttack:false);
                var battle=new ManualBattle(settings,lanes[0].Points,guard.Position,combat,roster:new[]{guard},deploymentLanes:lanes);
                battle.ConfigureMarineTerrain(map.WalkablePoints(),map.MarineOrigin);
                battle.ConfigureSynergies(new[]{new ShopTrialModel.Piece{Id="CARD_M33",Star=1},new ShopTrialModel.Piece{Id="CARD_M34",Star=1}});
                battle.Begin();var receipt=battle.TryDeploy(battle.RunId,1,origin);Assert.That(receipt.Success,Is.True);
                Assert.That(battle.TryDamageUnit(receipt.EntityId,1,CombatDamageType.Arts),Is.True);
                Assert.That(battle.MarineCells.Count,Is.EqualTo(1));
                Assert.That(Vector2.Distance(Key(battle.MarineCells[0].Position),Key(origin)),Is.LessThan(.001f),map.id);
                Assert.That(map.WalkablePoints().Any(point=>GroundContains(new HashSet<Vector2>{Key(battle.MarineCells[0].Position)},point)),Is.True);
            }
        }
    }
}
