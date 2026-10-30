using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MultiGateTests
    {
        static DeploymentLane Lane(string id,Vector3 start,Vector3 end,string exit="B1")=>new DeploymentLane(id,exit,new[]{start,end});
        static ManualBattle New(DeploymentLane[] lanes,float speed=1,float cooldown=.01f)=>new ManualBattle(
            new ManualBattleSettings("C","U",20,3,10,2,cooldown,100,speed,1,1,1,1),lanes[0].Points,new Vector3(1000,0,0),deploymentLanes:lanes);
        [Test] public void SeparateGatesKeepOwnRouteAndShareDefenseExactlyOnce()
        {
            var b=New(new[]{Lane("R1",Vector3.zero,new Vector3(0,0,3)),Lane("R2",new Vector3(3,0,0),new Vector3(3,0,4),"B2")});b.Begin();
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[0].Entry).Success,Is.True);
            b.Advance(.02f);
            Assert.That(b.TryDeploy(b.RunId,2,b.Lanes[1].Entry).Success,Is.True);
            b.Advance(3.1f);Assert.That(b.Breaches,Is.EqualTo(1));Assert.That(b.Defense,Is.EqualTo(2));
            Assert.That(b.Units[0].RouteId,Is.EqualTo("R1"));Assert.That(b.Units[1].ExitId,Is.EqualTo("B2"));
            b.Advance(1);Assert.That(b.Breaches,Is.EqualTo(2));Assert.That(b.Defense,Is.EqualTo(1));
            b.Advance(5);Assert.That(b.Breaches,Is.EqualTo(2));Assert.That(b.Defense,Is.EqualTo(1));
        }
        [Test] public void PausedDeploymentCannotEvadeSharedCooldownAndReplayCannotChangeGate()
        {
            var b=New(new[]{Lane("R1",Vector3.zero,Vector3.forward*10),Lane("R2",Vector3.right*3,Vector3.forward*10)},cooldown:2);b.Begin();b.SetPaused(true);
            var first=b.TryDeploy(b.RunId,1,b.Entry,true);Assert.That(first.Success,Is.True);
            Assert.That(b.TryDeploy(b.RunId,1,b.Entry,true).EntityId,Is.EqualTo(first.EntityId));
            Assert.That(b.TryDeploy(b.RunId,1,b.Lanes[1].Entry,true).Failure,Is.EqualTo(DeployFailure.StaleRun));
            Assert.That(b.TryDeploy(b.RunId,2,b.Lanes[1].Entry,true).Failure,Is.EqualTo(DeployFailure.CoolingDown));
            b.Advance(10);Assert.That(b.Clock,Is.Zero);Assert.That(b.DP,Is.EqualTo(18));Assert.That(b.Units[0].Motion.DistanceTravelled,Is.Zero);
            b.SetPaused(false);b.Advance(2.1f);b.SetPaused(true);
            Assert.That(b.TryDeploy(b.RunId,3,b.Lanes[1].Entry,true).Success,Is.True);
        }
        [Test] public void BusyGateDoesNotBlockOtherEntryAndInvalidDropIsAtomic()
        {
            var b=New(new[]{Lane("R1",Vector3.zero,Vector3.forward*10),Lane("R2",Vector3.right*3,Vector3.forward*10)});b.Begin();
            b.TryDeploy(b.RunId,1,b.Entry);
            b.Advance(.02f);
            Assert.That(b.TryDeploy(b.RunId,2,b.Entry).Failure,Is.EqualTo(DeployFailure.EntryBusy));
            Assert.That(b.TryDeploy(b.RunId,3,b.Lanes[1].Entry).Success,Is.True);
            Assert.That(b.TryDeploy(b.RunId,4,new Vector3(float.NaN,0,0)).Failure,Is.EqualTo(DeployFailure.WrongEntry));
            Assert.That(b.Units.Count,Is.EqualTo(2));Assert.That(b.DP,Is.EqualTo(16));
        }
        [Test] public void LanesCopyInputAndRejectAmbiguousGateBindings()
        {
            var p=new[]{Vector3.zero,Vector3.forward*10};var lane=new DeploymentLane("R1","B1",p);p[1]=Vector3.back;
            Assert.That(lane.Exit,Is.EqualTo(Vector3.forward*10));
            Assert.Throws<ArgumentException>(()=>New(new[]{lane,Lane("R1",Vector3.right*3,Vector3.forward*10)}));
            Assert.Throws<ArgumentException>(()=>New(new[]{lane,Lane("R2",Vector3.right*.2f,Vector3.forward*10)}));
            Assert.Throws<ArgumentException>(()=>New(new[]{lane,Lane("R2",Vector3.right*3,Vector3.forward*9)}));
        }
    }
}
