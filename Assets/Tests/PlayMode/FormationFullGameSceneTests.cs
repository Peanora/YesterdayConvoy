using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationFullGameSceneTests
    {
        FormationBattleController controller;
        [TearDown] public void Freeze(){if(controller?.Model!=null)controller.Model.Paused=true;}

        [UnityTest]
        public IEnumerator ApprovedFiniteChallengeUsesRealChoicesEquipmentAndCurrentBoard()
        {
            FormationShopSession.StartNew(20261117,fullCampaign:true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {controller=UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();if(controller!=null&&controller.Ready)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready,Is.True);
            Assert.That(controller.Campaign,Is.Not.Null);Assert.That(controller.Shop.UnlimitedGold,Is.False);
            Assert.That(controller.Shop.Gold,Is.EqualTo(8));Assert.That(controller.Shop.Level,Is.EqualTo(3));
            Assert.That(controller.Shop.Roles.Count,Is.EqualTo(46));
            Assert.That(controller.Shop.Bench.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(3));
            Assert.That(controller.Campaign.HasBlockingChoice,Is.True);
            controller.StartBattle();Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation),"Pending opening decisions must block actual combat.");
            Assert.That(controller.ChooseCampaignOption(0).Success,Is.True);
            yield return null;
            var old=controller.Campaign.Choices.Select(c=>c.Id).ToArray();
            Assert.That(controller.Campaign.RerollChoice(),Is.True);
            Assert.That(controller.Campaign.Choices.Any(c=>old.Contains(c.Id)),Is.False);
            int safety=0;
            while(controller.Campaign.HasBlockingChoice)
            {Assert.That(++safety,Is.LessThan(15));Assert.That(controller.ChooseCampaignOption(0).Success,Is.True);yield return null;}
            Assert.That(controller.Session.CanShop,Is.True);
            var piece=controller.Session.Seats.Lineup(controller.Shop.Bench).First();
            var item=controller.Campaign.Inventory.First();
            Assert.That(controller.Campaign.Equip(item.Instance,piece.Instance).Success,Is.True);
            controller.Session.ApplyTo(controller.Model);
            Assert.That(controller.Campaign.Equipment(piece.Instance).Count,Is.EqualTo(1));
            Assert.That(controller.Model.Units.Count,Is.EqualTo(3));
            Assert.That(controller.Model.Defenders.Count,Is.EqualTo(3));
            Assert.That(double.IsPositiveInfinity(controller.Model.LimitSeconds),Is.True,"Opening reward battles must have no deadline.");
            Assert.That(controller.terrain.regions.Length,Is.EqualTo(4));
            Assert.That(FormationBattleModel.Width,Is.EqualTo(26));
            Assert.That(FormationBattleModel.Height,Is.EqualTo(8));
            Assert.That(FormationBattleModel.DeploymentColumns,Is.EqualTo(12));
            controller.SetMarketVisible(false);controller.SetFormationView(true);yield return null;
            Assert.That(controller.MarketVisible,Is.False,"The finite preparation shop can collapse to reveal the normal sized board.");
            Assert.That(controller.ViewingDefenders,Is.True);
            controller.SetFormationView(false);yield return null;
            Assert.That(controller.ViewingDefenders,Is.False);
        }
    }
}
