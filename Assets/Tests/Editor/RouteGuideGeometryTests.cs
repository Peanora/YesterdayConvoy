using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class RouteGuideGeometryTests
    {
        static DeploymentLane[] AuthoredLanes()=>Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource).LaneSnapshot();
        static void Same(Vector3 actual,Vector3 expected,string message="")=>Assert.That(Vector3.Distance(actual,expected),Is.LessThan(.001f),message);

        [Test]public void EveryGuideEndpointUsesTheSameRouteDistanceAsAReallyDeployedEntity()
        {
            foreach(var lane in AuthoredLanes())
            {
                var settings=new ManualBattleSettings("CARD","unit",10,6,3,1,1,100,1,1,1,1,1);
                var battle=new ManualBattle(settings,lane.Points,Vector3.one*1000,null,null,null,new[]{lane});
                battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,lane.Entry).Success,Is.True);
                var motion=battle.Units.Single().Motion;
                var whole=RouteGuideGeometry.FullPath(lane.Points);
                Assert.That(whole.Count,Is.EqualTo(lane.Points.Count-1));
                Same(whole[0].From,lane.Entry);Same(whole[whole.Count-1].To,lane.Exit);
                Assert.That(whole.Sum(s=>s.ToDistance-s.FromDistance),Is.EqualTo(motion.TotalLength).Within(.00001));
                foreach(var span in whole.Concat(RouteGuideGeometry.Pulses(lane.Points,1.37)))
                {
                    motion.ApproachDistance(span.FromDistance,1000);Same(span.From,motion.Position,lane.Id+" / from");
                    motion.ApproachDistance(span.ToDistance,1000);Same(span.To,motion.Position,lane.Id+" / to");
                    Assert.That(span.ToDistance,Is.GreaterThan(span.FromDistance));
                }
            }
        }

        [Test]public void AMovingPulseSplitsAtTheRealCornerWithoutCuttingAcrossIt()
        {
            var points=new[]{Vector3.zero,Vector3.right,new Vector3(1,0,2)};
            var spans=RouteGuideGeometry.Pulses(points,1);
            int before=spans.FindIndex(s=>Math.Abs(s.FromDistance-.8)<.00001);
            Assert.That(before,Is.GreaterThanOrEqualTo(0));
            var a=spans[before];var b=spans[before+1];
            Same(a.To,Vector3.right);Same(a.To,b.From);
            Assert.That(a.EdgeIndex,Is.Zero);Assert.That(b.EdgeIndex,Is.EqualTo(1));
            Assert.That(a.Head,Is.False);Assert.That(b.Head,Is.True);
            Assert.That(a.ToDistance-a.FromDistance+b.ToDistance-b.FromDistance,Is.EqualTo(RouteGuideGeometry.PulseLength).Within(.00001));
            Assert.That(a.From.z,Is.Zero);Assert.That(b.To.x,Is.EqualTo(1));
        }

        [Test]public void PulsesMoveTowardTheExitAndStayIdenticalWhenTheBattleClockIsPaused()
        {
            var lane=AuthoredLanes()[0];
            var a=RouteGuideGeometry.Pulses(lane.Points,0).First();
            var b=RouteGuideGeometry.Pulses(lane.Points,.1).First();
            Assert.That(b.FromDistance-a.FromDistance,Is.EqualTo(.1*RouteGuideGeometry.PulseSpeed).Within(.00001));
            Assert.That(b.ToDistance-a.ToDistance,Is.EqualTo(.1*RouteGuideGeometry.PulseSpeed).Within(.00001));
            var settings=new ManualBattleSettings("CARD","unit",10,6,3,1,1,100,1,1,1,1,1);
            var battle=new ManualBattle(settings,lane.Points,Vector3.one*1000,null,null,null,new[]{lane});
            battle.Begin();battle.Advance(.25f);battle.SetPaused(true);
            var before=RouteGuideGeometry.Pulses(lane.Points,battle.Clock);
            battle.Advance(30);var after=RouteGuideGeometry.Pulses(lane.Points,battle.Clock);
            Assert.That(after.Count,Is.EqualTo(before.Count));
            for(int i=0;i<before.Count;i++){Same(after[i].From,before[i].From);Same(after[i].To,before[i].To);}
        }

        [TestCase(1280,720)][TestCase(1600,900)][TestCase(1024,640)][TestCase(900,1000)]
        public void ProjectionUsesTheRootCanvasOnceAtEveryWindowScale(int width,int height)
        {
            var view=new TacticalViewport(width,height);
            Func<Vector3,Vector3> project=p=>{
                Vector2 screen=view.ToScreen(new Vector2(640+p.x*24,350-p.z*18));
                return new Vector3(screen.x,screen.y,10);
            };
            var clip=new Rect(0,62,1280,488);
            foreach(var lane in AuthoredLanes())foreach(var span in RouteGuideGeometry.FullPath(lane.Points))
            {
                Assert.That(RouteGuideGeometry.TryProject(span,project,view,clip,out var actual),Is.True);
                Same(actual.From,new Vector2(640+span.From.x*24,350-span.From.z*18));
                Same(actual.To,new Vector2(640+span.To.x*24,350-span.To.z*18));
                Same(actual.Source.From,span.From);Same(actual.Source.To,span.To);
            }
        }

        [TestCase(1280,720)][TestCase(1600,900)][TestCase(1024,640)][TestCase(900,1000)]
        public void RotatedStrokeMatrixKeepsBothProjectedEndpointsOnTheRootCanvas(int width,int height)
        {
            var view=new TacticalViewport(width,height);var from=new Vector2(350,80);
            foreach(var to in new[]{new Vector2(350,430),new Vector2(900,80),new Vector2(180,460),new Vector2(700,200)})
            {
                var matrix=RouteGuideGeometry.StrokeMatrix(view.Matrix,from,to);
                Same(matrix.MultiplyPoint3x4(Vector3.zero),view.Matrix.MultiplyPoint3x4(from));
                Same(matrix.MultiplyPoint3x4(new Vector3(Vector2.Distance(from,to),0,0)),view.Matrix.MultiplyPoint3x4(to));
            }
        }

        [Test]public void ClippingPreservesTheCanvasOriginAndWorksInBothTravelDirections()
        {
            var clip=new Rect(0,62,1280,488);
            Vector2 a=new Vector2(-10,52),b=new Vector2(20,82);
            Assert.That(RouteGuideGeometry.TryClip(clip,ref a,ref b),Is.True);
            Same(a,new Vector2(0,62));Same(b,new Vector2(20,82));
            a=new Vector2(20,82);b=new Vector2(-10,52);
            Assert.That(RouteGuideGeometry.TryClip(clip,ref a,ref b),Is.True);
            Same(a,new Vector2(20,82));Same(b,new Vector2(0,62));
            a=new Vector2(500,0);b=new Vector2(500,100);
            Assert.That(RouteGuideGeometry.TryClip(clip,ref a,ref b),Is.True);
            Same(a,new Vector2(500,62));Same(b,new Vector2(500,100));
            a=new Vector2(10,10);b=new Vector2(100,10);
            Assert.That(RouteGuideGeometry.TryClip(clip,ref a,ref b),Is.False);
        }

        [Test]public void AClippedOffPulseHeadIsNotDrawnAtTheCanvasBoundary()
        {
            var view=new TacticalViewport(1280,720);
            Func<Vector3,Vector3> project=p=>new Vector3(p.x,720-p.y,10);
            var span=new RouteGuideSpan(new Vector3(10,100,0),new Vector3(40,100,0),0,30,0,true);
            Assert.That(RouteGuideGeometry.TryProject(span,project,view,new Rect(0,62,30,488),out var actual),Is.True);
            Same(actual.From,new Vector2(10,100));Same(actual.To,new Vector2(30,100));
            Assert.That(actual.HeadVisible,Is.False);
        }
    }
}
