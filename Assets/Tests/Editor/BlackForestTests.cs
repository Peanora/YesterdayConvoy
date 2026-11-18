using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

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
        [Test]public void PausingAtACrossingDoesNotAdvanceOrChangeLane()
        {
            var b=Model();b.Begin();b.TryDeploy(b.RunId,1,b.Lanes[1].Entry);b.Advance(2);
            var u=b.Units[0];Vector3 at=u.Motion.Position;double clock=b.Clock;b.SetPaused(true);b.Advance(60);
            Assert.That(u.Motion.Position,Is.EqualTo(at));Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.RouteId,Is.EqualTo("R2"));
            b.SetPaused(false);b.Advance(10);Assert.That(u.ExitId,Is.EqualTo("B2"));Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Breached));
        }
        [Test]public void PreparedLateGameCardsReserveRealStockAndReturnIndependentInstances()
        {
            var pieces=BlackForestChallengePlan.StartingPieces();Assert.That(pieces.Length,Is.EqualTo(BlackForestChallengePlan.InitialLevel));
            Assert.That(pieces.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(pieces.Length));
            Assert.That(pieces.Count(p=>p.Star==2),Is.EqualTo(3));Assert.That(pieces.Count(p=>p.Star==1),Is.EqualTo(5));
            Assert.That(pieces.All(p=>CampaignBalance.Profiles[p.Id].Cost<=3),Is.True,"The ordinary start must not include a five-cost carry.");
            Assert.That(pieces.Count(p=>CampaignBalance.Snow(p.Id)),Is.Zero,"Do not pre-assemble the five-Snow global weather build.");
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
    }
}
