using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class WangChessTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static ManualBattle Started()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.WangGambit,0,1,1,30);
            var guard=new DefenderSettings("wang","wang","D28",Vector3.zero,100000,20,0,10,100,
                actionDuration:.1f,skill:skill,canAttack:false);
            var entry=Vector3.right*2;
            var combat=new EngagementSettings(unitAttack:0,canAttack:false,initialSP:0,requiredSP:0,spPerSecond:0,
                skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false);
            var battle=new ManualBattle(new ManualBattleSettings("card","target",10,1,1,1,.1f,100000,.001f,1,1,1,1),
                new[]{entry,entry+Vector3.forward*100},Vector3.one*100,combat,roster:new[]{guard});
            battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
            Assert.That(battle.RemainingUses,Is.Zero);Assert.That(battle.Defender.SkillSP,Is.Zero);
            battle.Advance(.5f);Assert.That(battle.Defender.SkillCasts,Is.Zero);
            for(int i=0;i<200&&battle.Defender.SkillCasts==0;i++)battle.Advance(.01f);
            Assert.That(battle.Defender.SkillCasts,Is.EqualTo(1),"Use natural SP recovery and one finite deployment.");
            Assert.That(battle.DefenderSkillActive(battle.Defender),Is.True);
            Assert.That(battle.TryDeploy(battle.RunId,2,battle.Entry).Failure,Is.EqualTo(DeployFailure.NoUses));
            // Isolate placement boundaries after the real skill starts. This only
            // removes pre-existing stones and disables the next automatic placement.
            Pieces(battle).Clear();Impacts(battle).Clear();battle.Defender.NextSixthAt=battle.Clock+100;
            return battle;
        }
        static IList Pieces(ManualBattle battle)=>(IList)typeof(ManualBattle).GetField("chessPieces",Private).GetValue(battle);
        static IList Impacts(ManualBattle battle)=>(IList)typeof(ManualBattle).GetField("wangChessImpacts",Private).GetValue(battle);
        static bool Add(ManualBattle battle,Vector3 position,bool consume=true)=>(bool)typeof(ManualBattle)
            .GetMethod("AddChess",Private).Invoke(battle,new object[]{battle.Defender,position,consume});
        static void To(ManualBattle battle,double at){if(at>battle.Clock)battle.Advance((float)(at-battle.Clock));}
        static Vector3 Contact(ManualBattle battle)=>new Vector3(2,.04f,0);
        static void Present(AdvancedDefenderView view,SixthCell cell,double clock,Camera camera)=>typeof(AdvancedDefenderView)
            .GetMethod("PresentChess",Private).Invoke(view,new object[]{cell,clock,camera});

        [Test]public void PhysicalAndVirtualStonesKeepTheirOwnBirthWaitAndOnlyTheTokenHasABody()
        {
            var b=Started();Assert.That(Add(b,Contact(b)),Is.True);
            var physical=b.ChessPieces.Single(c=>!c.VirtualStone);var virtuals=b.ChessPieces.Where(c=>c.VirtualStone).ToArray();
            Assert.That(virtuals.Length,Is.EqualTo(3));
            Assert.That(physical.AvailableAt-physical.CreatedAt,Is.EqualTo(.1).Within(.000001));
            foreach(var stone in virtuals)Assert.That(stone.AvailableAt-stone.CreatedAt,Is.EqualTo(.25).Within(.000001));
            Assert.That(Add(b,new Vector3(4,.04f,2),false),Is.True);
            var instant=b.ChessPieces.Last();Assert.That(instant.VirtualStone,Is.True);
            Assert.That(instant.AvailableAt-instant.CreatedAt,Is.EqualTo(.25).Within(.000001));
            var viewObject=new GameObject("Wang body fixture");var cameraObject=new GameObject("Wang camera fixture");
            try
            {
                var view=viewObject.AddComponent<AdvancedDefenderView>();var camera=cameraObject.AddComponent<Camera>();
                physical.Active=false;Present(view,physical,physical.CreatedAt+.205,camera);physical.Active=true;
                foreach(var stone in b.ChessPieces.Where(c=>c.VirtualStone))Present(view,stone,stone.CreatedAt+.205,camera);
                var body=viewObject.GetComponentsInChildren<SpriteRenderer>(true).Single();
                Assert.That(body.color,Is.EqualTo(Color.white),"An unlinked token retains the original body color.");
                Assert.That(body.sprite,Is.SameAs(DefenderArt.Load("D28Chess").StartAt(.205)),"Birth age belongs to the model, not the first rendered frame.");
                To(b,physical.CreatedAt+.09);Assert.That(b.WangChessImpacts,Is.Empty);Assert.That(physical.Time,Is.Zero);
                To(b,physical.CreatedAt+.11);Assert.That(physical.Time,Is.GreaterThan(0));
                Assert.That(virtuals.All(c=>c.Time==0),Is.True,"Virtual stones cannot trigger during their .25s mark delay.");
                Present(view,physical,b.Clock,camera);Assert.That(body.enabled,Is.False,"The source Explode withdraws the body without using its unused Attack clip.");
            }
            finally
            {
                var view=viewObject.GetComponent<AdvancedDefenderView>();
                if(view!=null)
                {
                    var field=typeof(AdvancedDefenderView).GetField("material",Private);var mat=(Material)field.GetValue(view);
                    field.SetValue(view,null);if(mat!=null)UnityEngine.Object.DestroyImmediate(mat);
                }
                UnityEngine.Object.DestroyImmediate(viewObject);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]public void ExtraStonesUseDistinctFourNeighboursInsideTheShownMaskAndChargeOnlySuccessfulBuilds()
        {
            var b=Started();var facing=b.DefenderSkillFacing(b.Defender);var side=new Vector3(-facing.z,0,facing.x);
            foreach(var center in new[]{b.Defender.Position+facing*4+side*2,b.Defender.Position+facing*2})
            {
                Pieces(b).Clear();int ammo=b.Defender.Ammo;
                Assert.That(Add(b,new Vector3(center.x,.04f,center.z)),Is.True);
                var token=b.ChessPieces.Single(c=>!c.VirtualStone);var stones=b.ChessPieces.Where(c=>c.VirtualStone).ToArray();
                Assert.That(stones.Length,Is.EqualTo(center==b.Defender.Position+facing*2?3:2),"The marked corner has only two legal adjacent tiles.");
                Assert.That(stones.Select(c=>c.Position).Distinct().Count(),Is.EqualTo(stones.Length));
                foreach(var stone in stones)
                {
                    var delta=stone.Position-token.Position;
                    Assert.That(Mathf.Abs(delta.x)+Mathf.Abs(delta.z),Is.EqualTo(1).Within(.000001),"Children must be immediate orthogonal neighbours, not a world-Z chain.");
                    Assert.That(b.DefenderSkillContains(b.Defender,stone.Position),Is.True);
                }
                Assert.That(ammo-b.Defender.Ammo,Is.EqualTo(stones.Length));
            }
            int before=b.Defender.Ammo,count=b.ChessPieces.Count;
            Assert.That(Add(b,b.Defender.Position+facing*5),Is.False);Assert.That(b.Defender.Ammo,Is.EqualTo(before));Assert.That(b.ChessPieces.Count,Is.EqualTo(count));
        }

        [Test]public void PauseFreezesBirthAndRealImpactAndRestartClearsCellsAndTargetEvents()
        {
            var b=Started();Assert.That(Add(b,Contact(b)),Is.True);var cells=b.ChessPieces.ToArray();
            var token=cells.Single(c=>!c.VirtualStone);double clock=b.Clock;float health=b.Units[0].Health;int ammo=b.Defender.Ammo;
            b.SetPaused(true);b.Advance(10);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(cells.All(c=>c.Time==0),Is.True);Assert.That(b.WangChessImpacts,Is.Empty);
            Assert.That(b.Units[0].Health,Is.EqualTo(health));Assert.That(b.Defender.Ammo,Is.EqualTo(ammo));
            b.SetPaused(false);To(b,token.AvailableAt+.01);
            Assert.That(token.DamageResolved,Is.True);Assert.That(b.Units[0].Health,Is.LessThan(health));
            var impact=b.WangChessImpacts.Single();
            Assert.That(impact.RunId,Is.EqualTo(b.RunId));Assert.That(impact.SourceId,Is.EqualTo(b.Defender.EntityId));
            Assert.That(impact.TargetId,Is.EqualTo(b.Units[0].EntityId));Assert.That(impact.At,Is.EqualTo(token.Time));
            Assert.That(impact.Empowered,Is.True);Assert.That(Vector3.Distance(impact.Position,b.Units[0].Motion.Position),Is.LessThan(.001f));
            b.Advance(.02f);Assert.That(b.WangChessImpacts.Count,Is.EqualTo(1),"A settled token must not report the same target twice.");
            clock=b.Clock;b.SetPaused(true);b.Advance(10);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.WangChessImpacts.Count,Is.EqualTo(1));
            int oldRun=b.RunId;b.Restart();Assert.That(b.RunId,Is.GreaterThan(oldRun));
            Assert.That(b.ChessPieces,Is.Empty);Assert.That(b.WangChessImpacts,Is.Empty);Assert.That(b.RemainingUses,Is.EqualTo(1));Assert.That(b.Defender.SkillCasts,Is.Zero);
        }
    }
}
