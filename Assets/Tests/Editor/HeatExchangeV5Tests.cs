using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    /// <summary>Acceptance contract for the approved saved V5 courtyard, not a terrain generator.</summary>
    public sealed class HeatExchangeV5Tests
    {
        static readonly string[] Rows={
            "GGGGHHHGGGGGGH","GGGGHHHHHHHHGH","GGHHHHHHHHGGGG","GGHHGGGGGGEHGH","GGGGGGGEGGGHGH",
            "GHGGGGHHHHGHGH","GHGGEGHHHHGHGH","GGGGGGGGGGGHGH","HHGGGGGGGGGGGH","HHHHHHHHHHHHHH"};
        static readonly string[] PreviousRows={
            "HHHHHHHGGGGGGH","HHHHHHHHHHHHGH","HHHHHHHHHHGGGG","HHHHGGGGGGEHGH","HHHHGGGEGGGHGH",
            "HHHHGGHHHHGHGH","HHHHEGHHHHGHGH","GGGGGGGGGGGHGH","HHGGGGGGGGGGGH","HHHHHHHHHHHHHH"};
        [Serializable] sealed class Source { public Layout[] maps; }
        [Serializable] sealed class Layout { public int index;public string[] rows; }
        static ApprovedMapDefinition Map()=>Resources.Load<ApprovedMapDefinition>("ApprovedMaps/Map05");

        [Test] public void ApprovedCourtyardProfileAndAuthoringSourceKeepTwentyTwoLoweredCellsAndSixSmallPlatformCells()
        {
            var map=Map();Assert.That(map,Is.Not.Null);Assert.That(map.id,Is.EqualTo("approved-map-05"));
            CollectionAssert.AreEqual(Rows,map.gridRows);
            var source=JsonUtility.FromJson<Source>(File.ReadAllText("Assets/Data/ApprovedMaps/layouts.json"));
            CollectionAssert.AreEqual(Rows,source.maps.Single(m=>m.index==5).rows);
            int changed=0,retainedHigh=0;
            for(int row=0;row<10;row++)for(int col=0;col<14;col++)
            {
                if(Rows[row][col]!=PreviousRows[row][col])
                {
                    changed++;Assert.That(col<4&&row<7,Is.True,"Only the approved upper-left court changes height.");
                    Assert.That(PreviousRows[row][col],Is.EqualTo('H'));Assert.That(Rows[row][col],Is.EqualTo('G'));
                }
                if(col<4&&row<7&&Rows[row][col]=='H')retainedHigh++;
            }
            Assert.That(changed,Is.EqualTo(22));Assert.That(retainedHigh,Is.EqualTo(6));
            Assert.That(map.platformHeight,Is.EqualTo(.72f));
            var lanes=map.encounter.LaneSnapshot();Assert.That(lanes.Length,Is.EqualTo(2));
            Assert.That(lanes[0].Id,Is.EqualTo("R1"));Assert.That(lanes[0].Entry,Is.EqualTo(map.Cell(13,2)));
            Assert.That(lanes[1].Id,Is.EqualTo("R2"));Assert.That(lanes[1].Entry,Is.EqualTo(map.Cell(7,0)));
            foreach(var lane in lanes)
            {
                Assert.That(lane.ExitId,Is.EqualTo("B1"));Assert.That(lane.Points.Last(),Is.EqualTo(map.Cell(0,7)));
                foreach(var point in lane.Points)
                    Assert.That(map.WalkablePoints().Any(p=>(p-point).sqrMagnitude<.000001f),Is.True,"Original lane crosses non-ground.");
            }
            Assert.That(map.mechanics.heatPumps.Length,Is.EqualTo(3));
            var d=map.encounter.defenders.Single(g=>g.artId=="D27");
            Assert.That(d.position,Is.EqualTo(new Vector3(-4.5f,.72f,1.5f)));
            Assert.That(d.blocks,Is.Zero);Assert.That(d.skill.kind,Is.EqualTo(DefenderSkillKind.PramanixSnow));
            CollectionAssert.AreEquivalent(new[]{"D01","D04","D07","D08","D27"},map.encounter.defenders.Select(g=>g.artId));
        }

        [Test] public void SavedSceneHasEditableGridGatesHeatSignalsAndPersistentRenderableDependencies()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var map=Map();EditorSceneManager.OpenScene("Assets/Scenes/Map05HeatExchange.unity",OpenSceneMode.Single);
                var field=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(field,Is.Not.Null);
                Assert.That(Object.FindAnyObjectByType<ApprovedMapBootstrap>().map,Is.SameAs(map));
                var tiles=field.GetComponentsInChildren<ApprovedMapTile>(true);
                Assert.That(tiles.Length,Is.EqualTo(140));Assert.That(tiles.Select(t=>t.cell).Distinct().Count(),Is.EqualTo(140));
                foreach(var tile in tiles)
                {
                    Assert.That(tile.kind,Is.EqualTo(Rows[tile.cell.y][tile.cell.x].ToString()),tile.name);
                    Assert.That((tile.transform.position-map.Cell(tile.cell.x,tile.cell.y)).sqrMagnitude,Is.LessThan(.000001f),tile.name);
                }
                Assert.That(field.GetComponentsInChildren<Collider>(true),Is.Empty);
                var names=field.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
                Assert.That(names.Count(n=>n.EndsWith(" / Red entry",StringComparison.Ordinal)),Is.EqualTo(2));
                Assert.That(names.Count(n=>n.EndsWith(" / Blue exit",StringComparison.Ordinal)),Is.EqualTo(1));
                CollectionAssert.AreEquivalent(new[]{0,1,2},field.GetComponentsInChildren<ApprovedHeatMarker>(true).Select(m=>m.index));
                var meshes=field.GetComponentsInChildren<MeshFilter>(true);Assert.That(meshes.Length,Is.GreaterThan(140));
                foreach(var mesh in meshes)
                {Assert.That(mesh.sharedMesh,Is.Not.Null,mesh.name);Assert.That(AssetDatabase.Contains(mesh.sharedMesh),Is.True,mesh.name);}
                foreach(var renderer in field.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material,Is.Not.Null,renderer.name);Assert.That(AssetDatabase.Contains(material),Is.True,renderer.name);
                    Assert.That(material.shader,Is.Not.Null,renderer.name);Assert.That(material.shader.isSupported,Is.True,renderer.name);
                    foreach(var id in material.GetTexturePropertyNames())if(material.GetTexture(id)!=null)
                        Assert.That(AssetDatabase.Contains(material.GetTexture(id)),Is.True,renderer.name+" references temporary texture");
                }
            }
            finally
            {
                if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }

        [Test] public void PramanixOriginalCasterHitAndFieldResourcesExistAtTheirUncompressedSourceDurations()
        {
            string[] raw={"sbell2_skill_03_start_01","sbell2_skill_03_hit","sbell2_skill_03_buff_01","sbell2_skill_03_range","sbell2_skill_03_flow","sbell2_buff_01_hit_01","sbell2_ground_01","sbell2_ground_02","sbell2_ground_03"};
            var required=DefenderVfxArt.RequiredKeysForArtIds(new[]{"D27"}).ToArray();
            foreach(string name in raw)
            {
                string key="defender/D27/"+name;Assert.That(required,Does.Contain(key));
                Assert.That(OriginalCombatVfxArt.TryGet(key,out var clip),Is.True,key);
                foreach(var page in clip.Sequence.Pages)
                    Assert.That(File.Exists(ArtSourcePaths.Resolve(clip.Sequence.Root+"/"+page.file)),Is.True,key+" / "+page.file);
                if(name=="sbell2_skill_03_start_01")Assert.That(clip.Duration,Is.EqualTo(49f/30f).Within(.0001f));
                if(name=="sbell2_skill_03_hit")Assert.That(clip.Duration,Is.EqualTo(1.5f).Within(.0001f));
            }
        }
    }
}
