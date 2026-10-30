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
        [UnityTest]public IEnumerator ShopOverlayKeepsCameraAndInventoryStableAndBlocksCoveredMap()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;yield return null;
            Assert.That(c.ShopVisible,Is.False);Assert.That(ManualBattleController.ShopBenchRect(0).height,Is.LessThan(80));
            Assert.That(ManualBattleController.ShopBattlefieldRect.height,Is.GreaterThan(500));
            var viewport=Camera.main.rect;float fov=Camera.main.fieldOfView;var position=Camera.main.transform.position;
            var offers=c.Shop.Offers.ToArray();int gold=c.Shop.Gold,count=c.Shop.Bench.Count;
            Vector2 covered=ManualBattleController.ShopOfferRect(0).center;Assert.That(c.ShopMapPointAvailable(covered),Is.True);
            c.SetShopVisible(true);Assert.That(c.ShopVisible,Is.True);Assert.That(c.ShopMapPointAvailable(covered),Is.False);
            c.ProcessShopPointer(EventType.MouseDown,covered);Assert.That(c.Battle.Units.Count,Is.Zero);
            c.SetShopVisible(false);Assert.That(c.ShopMapPointAvailable(covered),Is.True);
            Assert.That(Camera.main.rect,Is.EqualTo(viewport));Assert.That(Camera.main.fieldOfView,Is.EqualTo(fov));Assert.That(Camera.main.transform.position,Is.EqualTo(position));
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));CollectionAssert.AreEqual(offers,c.Shop.Offers);
            c.EnterShopTrial(1);Assert.That(c.Shop.PreviewPurchase(0).FinalStar,Is.EqualTo(2));gold=c.Shop.Gold;c.SetShopVisible(false);
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(10));
            c.LeaveShopTrial();yield return null;yield return null;
        }
        [UnityTest]public IEnumerator ShopSlideReversesWhilePausedWithoutChangingInventoryOrLeakingModels()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;
            float previous=Time.timeScale;Time.timeScale=0;
            var offers=c.Shop.Offers.ToArray();int gold=c.Shop.Gold,count=c.Shop.Bench.Count;
            var model=c.ShopBenchModel(c.Shop.Bench[0].Instance);Assert.That(model,Is.Not.Null);var firstFrame=model.sprite;
            Vector2 covered=ManualBattleController.ShopOfferRect(0).center;
            c.SetShopVisible(true);yield return new WaitForSecondsRealtime(.08f);
            Assert.That(c.ShopPanelVisibility,Is.InRange(.01f,.99f));float opening=c.ShopPanelVisibility;
            c.SetShopVisible(false);Assert.That(c.ShopPanelVisibility,Is.EqualTo(opening).Within(.01f));Assert.That(c.ShopMapPointAvailable(covered),Is.False);
            yield return new WaitForSecondsRealtime(.025f);float closing=c.ShopPanelVisibility;
            c.SetShopVisible(true);Assert.That(c.ShopPanelVisibility,Is.EqualTo(closing).Within(.01f));yield return new WaitForSecondsRealtime(.3f);
            Assert.That(c.ShopPanelVisibility,Is.EqualTo(1));Assert.That(c.ShopPanelTransitioning,Is.False);
            Assert.That(model.sprite,Is.Not.SameAs(firstFrame),"Bench models should actually play battle idle frames while preparation is paused.");
            c.SetShopVisible(false);Assert.That(c.ShopMapPointAvailable(covered),Is.False);yield return new WaitForSecondsRealtime(.22f);
            Assert.That(c.ShopPanelVisibility,Is.Zero);Assert.That(c.ShopMapPointAvailable(covered),Is.True);
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));CollectionAssert.AreEqual(offers,c.Shop.Offers);
            Time.timeScale=previous;c.LeaveShopTrial();yield return null;Assert.That(model==null,Is.True);
        }
        [UnityTest]public IEnumerator NewlyPurchasedDogDeploysWithItsOwnCardAndProfile()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;yield return null;
            var bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.True);Assert.That(bought.Id,Is.EqualTo("CARD_M02"));
            Assert.That(c.ShopArrivalCount,Is.EqualTo(1));Assert.That(c.Battle.Units.Count,Is.Zero,"The purchase presentation must not deploy a battle unit.");
            var entering=c.ShopEntranceModel(bought.Instance);var bench=c.ShopBenchModel(bought.Instance);
            Assert.That(entering,Is.Not.Null);float startX=entering.transform.localPosition.x,startY=entering.transform.localPosition.y;
            Assert.That(startX,Is.EqualTo(bench.transform.localPosition.x).Within(.001f),"Entry starts inside its reserve cell, not in the shop.");
            Assert.That(startY,Is.GreaterThan(bench.transform.localPosition.y));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(entering.transform.localPosition.x,Is.EqualTo(startX).Within(.001f));
            Assert.That(entering.transform.localPosition.y,Is.LessThan(startY));Assert.That(entering.color.a,Is.GreaterThan(.5f));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(c.ShopArrivalCount,Is.Zero);Assert.That(c.ShopBenchModel(bought.Instance).sprite.name,Does.StartWith("M02/idle/"));
            var bottom=new Vector2(ManualBattleController.ShopBenchRect(2).center.x,ManualBattleController.ShopBenchRect(2).yMax-2);
            var sale=new Vector2(bottom.x,ManualBattleController.ShopSellRect.yMax-2);
            int gold=c.Shop.Gold;
            c.ProcessShopPointer(EventType.MouseDown,bottom);c.ProcessShopPointer(EventType.MouseUp,bottom);Assert.That(c.Shop.Gold,Is.EqualTo(gold),"A tap near the lower edge must not sell.");
            c.ProcessShopPointer(EventType.MouseDown,bottom);c.ProcessShopPointer(EventType.MouseDrag,sale);c.ProcessShopPointer(EventType.MouseUp,sale);
            Assert.That(c.Shop.Bench.Any(p=>p.Instance==bought.Instance),Is.False,"The lower edge must have enough room to drag down and sell.");
            bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.False,"The already purchased offer stays empty.");
            c.EnterShopTrial();bought=c.PurchaseShopCard(1);
            int slot=c.Shop.Bench.ToList().FindIndex(p=>p.Instance==bought.Instance);
            Vector2 screen=Camera.main.WorldToScreenPoint(c.Battle.Entry),point=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
            c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.ShopBenchRect(slot).center);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1),c.ShopFeedback);var unit=c.Battle.Units.Single();
            Assert.That(unit.CardId,Is.EqualTo("CARD_M02"));Assert.That(c.CardForUnit(unit).CardId,Is.EqualTo("CARD_M02"));Assert.That(c.Battle.UnitCombat(unit).HasSkill,Is.False);
            c.LeaveShopTrial();yield return null;yield return null;
        }
        [UnityTest]public IEnumerator FullBenchMergeIsImmediateAndMapAssetsStayUnmodified()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();var original=Resources.Load<DefenderTrialDefinition>("MapTrials/city");var originalCard=original.battle;
            var viewport=Camera.main.rect;float originalFov=Camera.main.fieldOfView;
            foreach(int preset in new[]{1,2})
            {
                c.EnterShopTrial(preset);yield return null;yield return null;
                float framedFov=Camera.main.fieldOfView;
                Assert.That(Camera.main.rect.height,Is.GreaterThan(.7f),"The map must occupy most of the screen, independent of the overlay.");
                foreach(var lane in original.lanes)foreach(var point in lane.points)
                {
                    Vector2 screen=Camera.main.WorldToScreenPoint(point),gui=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
                    Assert.That(ManualBattleController.ShopBattlefieldRect.Contains(gui),Is.True,"A route or gate was cropped by the enlarged camera.");
                }
                Assert.That(c.Shop.Bench.Count,Is.EqualTo(10));int gold=c.Shop.Gold;
                Assert.That(c.PurchaseShopCard(1).Success,Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
                Assert.That(c.Shop.PreviewPurchase(0).FinalStar,Is.EqualTo(preset==1?2:3));Assert.That(c.Shop.Bench.Count,Is.EqualTo(10));
                foreach(var piece in c.Shop.Bench)Assert.That(c.ShopBenchPromotionRemaining(piece.Instance),Is.Zero,"Existing ranked pieces must be static.");
                var result=c.PurchaseShopCard(0);Assert.That(result.Success,Is.True);Assert.That(result.FinalStar,Is.EqualTo(preset==1?2:3));Assert.That(string.IsNullOrEmpty(c.ShopFeedback),Is.True,"Successful promotion should only animate the piece, without a success notice.");
                Assert.That(c.ShopArrivalCount,Is.GreaterThan(0));
                float previousScale=Time.timeScale;Time.timeScale=0;
                yield return new WaitForSecondsRealtime(.8f);
                Assert.That(c.ShopBenchPromotionRemaining(result.Instance),Is.GreaterThan(0));
                yield return new WaitForSecondsRealtime(1.1f);
                Time.timeScale=previousScale;
                Assert.That(c.ShopBenchPromotionRemaining(result.Instance),Is.Zero,"Bench effects must finish even while gameplay is paused.");
                Assert.That(c.Shop.Bench.Single(p=>p.Instance==result.Instance).Star,Is.EqualTo(result.FinalStar),"Ending the effect must preserve the rank.");
                Assert.That(c.Shop.Bench.Count,Is.EqualTo(preset==1?9:7));Assert.That(c.Shop.Gold,Is.EqualTo(gold-1));c.Shop.Validate();
                Assert.That(Camera.main.fieldOfView,Is.EqualTo(framedFov).Within(.001f),"Buying or merging must not accumulate camera zoom.");
                Assert.That(original.battle,Is.SameAs(originalCard));Assert.That(ManualBattleController.ShopOfferRect(0).yMax,Is.LessThan(ManualBattleController.ShopBenchRect(0).y));
                c.LeaveShopTrial();yield return null;yield return null;Assert.That(Camera.main.rect,Is.EqualTo(viewport));Assert.That(Camera.main.fieldOfView,Is.EqualTo(originalFov));Assert.That(c.TestSelectionOpen,Is.True);
            }
        }
        [UnityTest]public IEnumerator RapidPurchasesAndSaleLeaveNoDuplicateEntranceModels()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();
            c.Shop.Preset(new ShopTrialModel.Piece[0],new[]{"CARD_M02","CARD_M02","CARD_M02"});
            for(int i=0;i<3;i++)Assert.That(c.PurchaseShopCard(i).Success,Is.True);
            var piece=c.Shop.Bench.Single();Assert.That(piece.Star,Is.EqualTo(2));Assert.That(c.ShopArrivalCount,Is.EqualTo(1));
            var entering=c.ShopEntranceModel(piece.Instance);Assert.That(entering,Is.Not.Null);
            Assert.That(c.SellShopCard(piece.Instance).Success,Is.True);Assert.That(c.ShopArrivalCount,Is.Zero);Assert.That(c.ShopBenchModel(piece.Instance),Is.Null);
            yield return null;Assert.That(entering==null,Is.True);Assert.That(c.Battle.Units.Count,Is.Zero);
            c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator PurchasedArcherDeploysOnCityRouteAndBattleFreezesTrading()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.EnterShopTrial();yield return null;yield return null;
            c.PurchaseShopCard(0);Assert.That(c.Shop.Bench.Single().Star,Is.EqualTo(2));
            c.SetShopVisible(true);
            Vector2 screen=Camera.main.WorldToScreenPoint(c.Battle.Entry),point=new Vector2(screen.x*1280/Screen.width,720-screen.y*720/Screen.height);
            Assert.That(ManualBattleController.ShopBattlefieldRect.Contains(point),Is.True,"The first red gate must remain inside the playable viewport.");
            var start=ManualBattleController.ShopBenchRect(0).center;
            c.ProcessShopPointer(EventType.MouseDown,start);c.ProcessShopPointer(EventType.MouseDrag,point);c.ProcessShopPointer(EventType.MouseUp,point);
            Assert.That(c.ShopVisible,Is.False,"Dragging a bench piece should uncover the map without moving the camera.");
            Assert.That(c.Battle.Units.Count,Is.EqualTo(1),c.ShopFeedback+" / "+c.Feedback+" / phase="+c.Battle.Phase);Assert.That(c.Battle.Units[0].CardId,Is.EqualTo("CARD_M08"));
            int gold=c.Shop.Gold;Assert.That(c.PurchaseShopCard(0).Success,Is.False);Assert.That(c.SellShopCard(c.Shop.Bench[0].Instance).Success,Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(gold));
            c.LeaveShopTrial();yield return null;yield return null;
        }
    }
}
