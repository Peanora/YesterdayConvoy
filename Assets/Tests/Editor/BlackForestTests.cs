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
            var source=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            var data=UnityEngine.Object.Instantiate(source);
            try{
                data.terrainOnly=true;data.defenders=Array.Empty<DefenderPlacement>();
                Assert.Throws<ArgumentException>(()=>new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),null,data.Snapshot(),data.LaneSnapshot()));
                var battle=new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),null,data.Snapshot(),data.LaneSnapshot(),allowEmptyRoster:true);
                Assert.That(battle.Defenders,Is.Empty);battle.Restart();Assert.That(battle.Defenders,Is.Empty);
            }finally{UnityEngine.Object.DestroyImmediate(data);}
            Assert.That(source.terrainOnly,Is.False);Assert.That(source.defenders.Length,Is.EqualTo(9));
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
            Assert.That(data,Is.Not.Null);Assert.That(data.terrainOnly,Is.False);Assert.That(data.Snapshot().Length,Is.EqualTo(9));
            var tiles=UnityEngine.Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None);Assert.That(tiles.Length,Is.EqualTo(238));
            foreach(var type in new[]{BlackForestSurface.Blackflow,BlackForestSurface.Amber,BlackForestSurface.RootVein})Assert.That(tiles.Count(t=>t.surface==type),Is.GreaterThan(0));
            foreach(var r in UnityEngine.Object.FindAnyObjectByType<AuthoredBattlefield>().GetComponentsInChildren<Renderer>())Assert.That(AssetDatabase.Contains(r.sharedMaterial),Is.True,r.name);
            Assert.That(GameSceneCatalog.BuildPaths(),Does.Contain(BlackForestMapTools.ScenePath));
        }
        [Test]public void SixStarFormationUsesLegalTerrainAndPreservesItsSelectedSkillTiming()
        {
            var data=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);var guards=data.Snapshot();
            Assert.That(guards.Length,Is.EqualTo(9));Assert.That(guards.Select(d=>d.Id).Distinct().Count(),Is.EqualTo(9));
            foreach(var guard in guards)
            {
                int col=Mathf.RoundToInt(guard.Position.x+9),row=Mathf.RoundToInt(7.5f-guard.Position.z);
                bool legal=guard.Position.y>.5f?BlackForestLayout.Tile(col,row)=='H':BlackForestLayout.Walkable(col,row);
                Assert.That(legal,Is.True,guard.Name+" must occupy its authored ground type.");
                if(guard.Position.y>.5f&&guard.ArtId!="D23")Assert.That(guard.BlockCount,Is.Zero,guard.Name+" cannot block from a platform.");
            }
            var six=new[]{"D23","D29","D28","D30","D24","D25"};
            Assert.That(guards.Count(d=>six.Contains(d.ArtId)),Is.EqualTo(6));
            foreach(string id in six)
            {
                var guard=guards.Single(d=>d.ArtId==id);var original=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id).defenders[0].skill.Snapshot();
                Assert.That(guard.Skill.Kind,Is.EqualTo(original.Kind),id);
                Assert.That(guard.Skill.InitialSP,Is.EqualTo(original.InitialSP),id);
                Assert.That(guard.Skill.RequiredSP,Is.EqualTo(original.RequiredSP),id);
                Assert.That(guard.Skill.Duration,Is.EqualTo(original.Duration),id);
            }
            var mon3tr=guards.Single(d=>d.ArtId=="D23");Assert.That(mon3tr.Skill.AttackRecovery,Is.True);
            int relayCol=Mathf.RoundToInt(mon3tr.RelayPosition.x+9),relayRow=Mathf.RoundToInt(7.5f-mon3tr.RelayPosition.z);
            Assert.That(BlackForestLayout.Walkable(relayCol,relayRow),Is.True,"Mon3tr must transfer to existing ground.");
            Assert.That(guards.Any(d=>d.Reserve),Is.False,"The sniper must not silently add an unreviewed reinforcement.");
        }
        [Test]public void MedicalSupportStaysLocalAndResetCopiesDoNotEditTheAuthoredSkills()
        {
            var source=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);string original=JsonUtility.ToJson(source);
            var guards=BlackForestChallengePlan.CopyDefenders();var byArt=guards.ToDictionary(d=>d.artId);
            var mon3tr=byArt["D23"];var saria=byArt["D29"];var mlynar=byArt["D30"];var ansel=guards.Single(d=>d.role==DefenderRole.Medic&&d.artId!="D23");
            Func<Vector3,Vector3,float> distance=(a,b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
            Assert.That(distance(mon3tr.position,saria.position),Is.LessThanOrEqualTo(2),"Chain healing needs an actual second target within its jump radius.");
            Assert.That(distance(mon3tr.position,saria.position),Is.LessThanOrEqualTo(mon3tr.healingRange));
            Assert.That(distance(saria.position,mlynar.position),Is.GreaterThan(Mathf.Max(saria.healingRange,saria.skill.effectRadius)));
            Assert.That(distance(mon3tr.position,mlynar.position),Is.GreaterThan(mon3tr.healingRange));
            Assert.That(distance(ansel.position,mlynar.position),Is.GreaterThan(ansel.healingRange+ansel.skill.healRangeBonus));
            Assert.That(distance(byArt["D25"].position,mlynar.position),Is.GreaterThan(byArt["D25"].skill.effectRadius),"The last stand must not also protect Mlynar.");
            mon3tr.health=1;mon3tr.skill.initialSP=0;mon3tr.position=Vector3.one*100;
            Assert.That(JsonUtility.ToJson(source),Is.EqualTo(original),"A reset snapshot must deeply clone the mutable skill objects.");
            var fresh=BlackForestChallengePlan.CopyDefenders();Assert.That(fresh.Single(d=>d.artId=="D23").health,Is.GreaterThan(1));
        }
        [Test]public void PreparedLateGameCardsReserveRealStockAndReturnIndependentInstances()
        {
            var pieces=BlackForestChallengePlan.StartingPieces();Assert.That(pieces.Length,Is.EqualTo(BlackForestChallengePlan.InitialLevel));
            Assert.That(pieces.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(pieces.Length));
            var roles=CampaignBalance.Profiles.Select(p=>new ShopTrialModel.Role{Id=p.Key,Name=p.Key,Cost=p.Value.Cost});
            var shop=new ShopTrialModel(roles,61108);shop.EnableCampaign(true);shop.Preset(pieces,Array.Empty<string>(),BlackForestChallengePlan.InitialLevel,0,10);
            for(int refresh=0;refresh<3;refresh++)
            {
                Assert.That(shop.Refresh(true).Success,Is.True);
                foreach(var role in shop.Roles)
                {
                    int owned=shop.Bench.Where(p=>p.Id==role.Id).Sum(p=>ShopTrialModel.Copies(p.Star));
                    Assert.That(shop.Available(role.Id)+owned+shop.Offers.Count(id=>id==role.Id),Is.EqualTo(shop.Capacity(role.Id)),role.Id);
                }
                shop.Validate();
            }
            string first=pieces[0].Id;int star=pieces[0].Star;pieces[0].Star=1;
            Assert.That(BlackForestChallengePlan.StartingPieces().Single(p=>p.Id==first).Star,Is.EqualTo(star));
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
