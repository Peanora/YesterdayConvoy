using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopMapSceneTests
    {
        [UnityTest]public IEnumerator NewlyPurchasedDogDeploysWithItsOwnCardAndProfile()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;yield return null;
            var bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.True);Assert.That(bought.Id,Is.EqualTo("CARD_M02"));
            int slot=c.Shop.Bench.ToList().FindIndex(p=>p.Instance==bought.Instance);
            Vector2 screen=Camera.main.WorldToScreenPoint(c.Battle.Entry),point=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
            c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.ShopBenchRect(slot).center);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1),c.ShopFeedback);var unit=c.Battle.Units.Single();
            Assert.That(unit.CardId,Is.EqualTo("CARD_M02"));Assert.That(c.CardForUnit(unit).CardId,Is.EqualTo("CARD_M02"));Assert.That(c.Battle.UnitCombat(unit).HasSkill,Is.False);
            c.LeaveShopTrial();yield return null;yield return null;
        }
        [UnityTest]public IEnumerator FullBenchMergeIsConfirmedAndMapAssetsStayUnmodified()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();var original=Resources.Load<DefenderTrialDefinition>("MapTrials/city");var originalCard=original.battle;
            var viewport=Camera.main.rect;float originalFov=Camera.main.fieldOfView;
            foreach(int preset in new[]{1,2})
            {
                c.EnterShopTrial(preset);yield return null;yield return null;
                float framedFov=Camera.main.fieldOfView;
                Assert.That(framedFov,Is.LessThan(originalFov/1.2f),"The shop map must be visibly larger than the old camera framing.");
                foreach(var lane in original.lanes)foreach(var point in lane.points)
                {
                    Vector2 screen=Camera.main.WorldToScreenPoint(point),gui=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
                    Assert.That(ManualBattleController.ShopBattlefieldRect.Contains(gui),Is.True,"A route or gate was cropped by the enlarged camera.");
                }
                Assert.That(c.Shop.Bench.Count,Is.EqualTo(10));int gold=c.Shop.Gold;
                Assert.That(c.PurchaseShopCard(1).Success,Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
                Assert.That(c.PurchaseShopCard(0).NeedsConfirmation,Is.True);Assert.That(c.Shop.Bench.Count,Is.EqualTo(10));
                var result=c.PurchaseShopCard(0,true);Assert.That(result.Success,Is.True);Assert.That(result.FinalStar,Is.EqualTo(preset==1?2:3));
                Assert.That(c.Shop.Bench.Count,Is.EqualTo(preset==1?9:7));Assert.That(c.Shop.Gold,Is.EqualTo(gold-1));c.Shop.Validate();
                Assert.That(Camera.main.fieldOfView,Is.EqualTo(framedFov).Within(.001f),"Buying or merging must not accumulate camera zoom.");
                Assert.That(original.battle,Is.SameAs(originalCard));Assert.That(ManualBattleController.ShopOfferRect(0).yMax,Is.LessThan(ManualBattleController.ShopBenchRect(0).y));
                c.LeaveShopTrial();yield return null;yield return null;Assert.That(Camera.main.rect,Is.EqualTo(viewport));Assert.That(Camera.main.fieldOfView,Is.EqualTo(originalFov));Assert.That(c.TestSelectionOpen,Is.True);
            }
        }
        [UnityTest]public IEnumerator PurchasedArcherDeploysOnCityRouteAndBattleFreezesTrading()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;yield return null;
            c.PurchaseShopCard(0);Assert.That(c.Shop.Bench.Single().Star,Is.EqualTo(2));
            Vector2 screen=Camera.main.WorldToScreenPoint(c.Battle.Entry),point=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
            Assert.That(ManualBattleController.ShopBattlefieldRect.Contains(point),Is.True,"The first red gate must remain inside the playable viewport.");
            var start=ManualBattleController.ShopBenchRect(0).center;
            c.ProcessShopPointer(EventType.MouseDown,start);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1),c.ShopFeedback+" / "+c.Feedback+" / phase="+c.Battle.Phase);Assert.That(c.Battle.Units[0].CardId,Is.EqualTo("CARD_M08"));
            int gold=c.Shop.Gold;Assert.That(c.PurchaseShopCard(0).Success,Is.False);Assert.That(c.SellShopCard(c.Shop.Bench[0].Instance).Success,Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
            c.LeaveShopTrial();yield return null;yield return null;
        }
    }
}
