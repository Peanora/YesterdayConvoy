using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ApprovedUiSceneTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static ManualBattleController Controller=>Object.FindAnyObjectByType<ManualBattleController>();
        static IEnumerator Load(string name="MonsterTrial")
        {
            yield return SceneManager.LoadSceneAsync(name);
            for(int i=0;i<900;i++)
            {
                var c=Controller;
                if(c!=null&&!c.ArtLoading&&(name!="SynergyTrial"||c.CampaignActive)){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("UI scene startup timed out: "+name);
        }
        static Vector2 PiecePoint(ManualBattleController c,int instance)
        {
            int index=c.Shop.Bench.ToList().FindIndex(p=>p.Instance==instance);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));return c.ShopPieceRect(index).center;
        }
        static Vector2 Gate(ManualBattleController c)=>TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
        static void Drag(ManualBattleController c,Vector2 from,Vector2 to)
        {
            c.ProcessShopPointer(EventType.MouseDown,from);c.ProcessShopPointer(EventType.MouseDrag,to);c.ProcessShopPointer(EventType.MouseUp,to);
        }
        static void SetPrivate(ManualBattleController c,string name,object value)
        {
            var field=typeof(ManualBattleController).GetField(name,Private);Assert.That(field,Is.Not.Null,name);field.SetValue(c,value);
        }
        [TearDown]public void RestoreClock(){Time.timeScale=1;}

        [UnityTest]public IEnumerator EitherEdgeSellsOneInstanceAndRefundsOnlyOnce()
        {
            yield return Load();var c=Controller;
            foreach(var edge in new[]{ManualBattleController.ShopSellRect.center,ManualBattleController.ShopRightSaleDropRect.center})
            {
                c.EnterShopTrial();var bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.True);
                int gold=c.Shop.Gold,stock=c.Shop.Available(bought.Id),value=c.Shop.Quote(bought.Instance).Value;
                Drag(c,PiecePoint(c,bought.Instance),edge);
                Assert.That(c.Shop.Bench.Any(p=>p.Instance==bought.Instance),Is.False);
                Assert.That(c.Shop.Gold,Is.EqualTo(gold+value));Assert.That(c.Shop.Available(bought.Id),Is.EqualTo(stock+1));
                c.ProcessShopPointer(EventType.MouseUp,edge);
                Assert.That(c.SellShopCard(bought.Instance).Success,Is.False,"A repeated release/request cannot refund the sold instance twice.");
                Assert.That(c.Shop.Gold,Is.EqualTo(gold+value));c.Shop.Validate();
            }
            c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator ReturningCancellingOrDroppingBelowTheBenchDoesNotSell()
        {
            yield return Load();var c=Controller;c.EnterShopTrial();var bought=c.PurchaseShopCard(1);
            Assert.That(bought.Success,Is.True);int gold=c.Shop.Gold,stock=c.Shop.Available(bought.Id),count=c.Shop.Bench.Count;
            Vector2 origin=PiecePoint(c,bought.Instance);
            c.ProcessShopPointer(EventType.MouseDown,origin);c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopSellRect.center);
            c.ProcessShopPointer(EventType.MouseUp,origin);
            Assert.That(c.Shop.Bench.Any(p=>p.Instance==bought.Instance),Is.True,"Dragging back cancels sale.");
            c.ProcessShopPointer(EventType.MouseDown,origin);c.ProcessShopPointer(EventType.MouseDrag,ManualBattleController.ShopRightSaleDropRect.center);
            c.CancelShopDrag();c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopRightSaleDropRect.center);
            Drag(c,origin,new Vector2(640,718));
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Available(bought.Id),Is.EqualTo(stock));
            Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));Assert.That(c.Battle.Units,Is.Empty,"The former bottom sale region is not a deployment gate.");
            c.Shop.Validate();c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator PausedDeploymentConsumesBattleDPWithoutSellingTheCard()
        {
            yield return Load();var c=Controller;c.EnterShopTrial();var bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.True);
            c.Battle.Begin();c.Battle.SetPaused(true);int gold=c.Shop.Gold,dp=c.Battle.DP,stock=c.Shop.Available(bought.Id);
            Drag(c,PiecePoint(c,bought.Instance),Gate(c));
            Assert.That(c.Battle.Units.Count(u=>u.CardId==bought.Id),Is.EqualTo(1),c.Feedback+" / "+c.ShopFeedback);
            Assert.That(c.Battle.DP,Is.LessThan(dp));Assert.That(c.Battle.IsPaused,Is.True);Assert.That(c.Battle.Clock,Is.Zero);
            Drag(c,PiecePoint(c,bought.Instance),ManualBattleController.ShopRightSaleDropRect.center);
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Available(bought.Id),Is.EqualTo(stock));
            Assert.That(c.Shop.Bench.Any(p=>p.Instance==bought.Instance),Is.True,"Combat/paused deployment must never become a sale.");
            c.Tick(1);Assert.That(c.Battle.Clock,Is.Zero);c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator DamageRankingIsPassiveAndBlocksTheMapIndependentlyOfTheShop()
        {
            yield return Load();var c=Controller;c.EnterShopTrial();c.SetShopVisible(true);
            Vector2 row=ManualBattleController.ShopRecruitRowRect(1).center;
            Assert.That(c.DamageRankingVisible,Is.True);Assert.That(c.ShopMapPointAvailable(row),Is.False);
            int gold=c.Shop.Gold,count=c.Shop.Bench.Count;string[] offers=c.Shop.Offers.ToArray();
            c.ProcessShopPointer(EventType.MouseDown,row);c.ProcessShopPointer(EventType.MouseUp,row);
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));
            yield return new WaitForSecondsRealtime(.3f);
            string id=c.Shop.Offers[1];int cost=c.Shop.Definition(id).Cost;
            c.ProcessShopPointer(EventType.MouseDown,row);c.ProcessShopPointer(EventType.MouseUp,row);
            Assert.That(c.Shop.Offers,Is.EqualTo(offers),"The former recruit rows must no longer purchase offers.");
            Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));
            Vector2 top=ManualBattleController.ShopOfferRect(1).center;
            c.ProcessShopPointer(EventType.MouseDown,top);c.ProcessShopPointer(EventType.MouseUp,top);
            Assert.That(c.Shop.Offers[1],Is.Null);Assert.That(c.Shop.Gold,Is.EqualTo(gold-cost),"Top offers retain the existing atomic purchase path.");
            Assert.That(c.Shop.Bench.Count,Is.EqualTo(count+1));Assert.That(c.Battle.Units,Is.Empty);c.Shop.Validate();
            var bought=c.Shop.Bench.Single(p=>p.Id==id);int stock=c.Shop.Available(id);
            Drag(c,PiecePoint(c,bought.Instance),new Vector2(1242,400));
            Assert.That(c.Shop.Bench.Any(p=>p.Instance==bought.Instance),Is.True,"Dropping over the ranking's right edge must not fall through to the sale target.");
            Assert.That(c.Shop.Gold,Is.EqualTo(gold-cost));Assert.That(c.Shop.Available(id),Is.EqualTo(stock));
            c.SetShopVisible(false);Assert.That(c.ShopMapPointAvailable(row),Is.False);yield return new WaitForSecondsRealtime(.22f);
            Assert.That(c.DamageRankingVisible,Is.True);Assert.That(c.ShopMapPointAvailable(row),Is.False,"The ranking remains visible when the shop is folded.");
            c.Battle.Begin();c.Battle.SetPaused(true);gold=c.Shop.Gold;count=c.Battle.Units.Count;
            c.ProcessShopPointer(EventType.MouseDown,row);c.ProcessShopPointer(EventType.MouseUp,row);
            Assert.That(c.ShopMapPointAvailable(row),Is.False);Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Battle.Units.Count,Is.EqualTo(count));
            c.LeaveShopTrial();yield return null;
        }
        [Test]public void ThreeRealStaticFontsLoadWithChineseDigitsAndNoVariableFontTable()
        {
            var fonts=new Font[3];string[] weights={"Regular","Semibold","Bold"};
            for(int n=0;n<weights.Length;n++)
            {
                string name="YesterdayConvoyUI-"+weights[n];fonts[n]=Resources.Load<Font>("UI/"+name);Assert.That(fonts[n],Is.Not.Null,name);
                foreach(char glyph in "商店备战羁绊霜星0123456789+-/()")Assert.That(fonts[n].HasCharacter(glyph),Is.True,name+" missing "+glyph);
                // PlayMode runs in the Editor: inspect the actual imported source, not a fallback font name.
                string path=Path.Combine(Application.dataPath,"Fonts/Resources/UI/"+name+".ttf");Assert.That(File.Exists(path),Is.True,path);
                byte[] data=File.ReadAllBytes(path);Assert.That(data.Length,Is.GreaterThan(12));int tables=data[4]*256+data[5];
                Assert.That(data.Length,Is.GreaterThanOrEqualTo(12+tables*16));
                for(int i=0;i<tables;i++)Assert.That(Encoding.ASCII.GetString(data,12+i*16,4),Is.Not.EqualTo("fvar"),name+" must be a static font.");
            }
            Assert.That(fonts.Distinct().Count(),Is.EqualTo(3),"Weights must load separate real font assets.");
        }
        [UnityTest]public IEnumerator RepeatedShopAndLineupActionsKeepCameraAndMapTransformsStable()
        {
            yield return Load();var c=Controller;c.EnterShopTrial();yield return null;
            var camera=Camera.main;var rect=camera.rect;float fov=camera.fieldOfView;var position=camera.transform.position;var rotation=camera.transform.rotation;
            var map=Object.FindAnyObjectByType<StrongholdMapEnvironment>();Assert.That(map,Is.Not.Null);
            var mapPosition=map.transform.position;var mapScale=map.transform.localScale;var mapRotation=map.transform.rotation;
            int geometry=map.GetComponentsInChildren<Renderer>(true).Length;
            for(int i=0;i<3;i++)
            {
                c.SetShopVisible(true);yield return new WaitForSecondsRealtime(.26f);c.SetShopVisible(false);yield return new WaitForSecondsRealtime(.2f);
                int instance=c.Shop.Bench[0].Instance;Vector2 slot=PiecePoint(c,instance);
                c.ProcessShopPointer(EventType.MouseDown,slot);c.ProcessShopPointer(EventType.MouseUp,slot);
                slot=PiecePoint(c,instance);c.ProcessShopPointer(EventType.MouseDown,slot);c.ProcessShopPointer(EventType.MouseUp,slot);
            }
            var bought=c.PurchaseShopCard(1);Assert.That(bought.Success,Is.True);Assert.That(c.SellShopCard(bought.Instance).Success,Is.True);
            Assert.That(camera.rect,Is.EqualTo(rect));Assert.That(camera.fieldOfView,Is.EqualTo(fov).Within(.001f));
            Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.0001f));Assert.That(Quaternion.Angle(camera.transform.rotation,rotation),Is.LessThan(.0001f));
            Assert.That(map.transform.position,Is.EqualTo(mapPosition));Assert.That(map.transform.localScale,Is.EqualTo(mapScale));Assert.That(map.transform.rotation,Is.EqualTo(mapRotation));
            Assert.That(map.GetComponentsInChildren<Renderer>(true).Length,Is.EqualTo(geometry));c.LeaveShopTrial();yield return null;
        }
        [UnityTest]public IEnumerator FoldedConstructionReleasesTheOldMapAreaWhileBothModalsBlockInput()
        {
            yield return Load("SynergyTrial");var c=Controller;Assert.That(c.StartLoopNextBattle(),Is.True);c.SetShopVisible(false);yield return new WaitForSecondsRealtime(.22f);
            var oldPanelPoint=new Vector2(420,420);Assert.That(c.ShopMapPointAvailable(oldPanelPoint),Is.True,"The folded 405/580px construction panels no longer cover the map.");
            int gold=c.Shop.Gold,count=c.Shop.Bench.Count;string[] offers=c.Shop.Offers.ToArray();
            foreach(string modal in new[]{"campaignConstructOpen","inspectedSynergy"})
            {
                SetPrivate(c,modal,modal=="campaignConstructOpen"?(object)true:"整合运动");
                Assert.That(c.ShopMapPointAvailable(oldPanelPoint),Is.False);Assert.That(c.ShopMapPointAvailable(Gate(c)),Is.False);
                Drag(c,c.ShopPieceRect(0).center,Gate(c));
                c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.ShopOfferRect(0).center);
                c.ProcessShopPointer(EventType.MouseUp,ManualBattleController.ShopOfferRect(0).center);
                Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.Shop.Gold,Is.EqualTo(gold));Assert.That(c.Shop.Bench.Count,Is.EqualTo(count));CollectionAssert.AreEqual(offers,c.Shop.Offers);
                SetPrivate(c,modal,modal=="campaignConstructOpen"?(object)false:null);
                Assert.That(c.ShopMapPointAvailable(oldPanelPoint),Is.True);
            }
            c.LeaveShopTrial();yield return null;
        }
    }
}
