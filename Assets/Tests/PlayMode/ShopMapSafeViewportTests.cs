using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class ShopMapSafeViewportTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float previousScale;

        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        void SetSynergyModal(string value)=>typeof(ManualBattleController).GetField("inspectedSynergy",Hidden).SetValue(controller,value);
        bool ModalOpen=>(bool)typeof(ManualBattleController).GetProperty("CampaignUiModalOpen",Hidden).GetValue(controller);

        static void AssertProjectedInside(Vector3 world,Rect safe,Rect ranking,string subject)
        {
            Vector3 screen=Camera.main.WorldToScreenPoint(world);
            Assert.That(screen.z,Is.GreaterThan(0),subject+" must be in front of the real camera.");
            Vector2 point=TacticalViewport.Current.FromScreen(screen);
            Assert.That(point.x,Is.InRange(safe.xMin-2,safe.xMax+2),subject+" horizontal projection "+point);
            Assert.That(point.y,Is.InRange(safe.yMin-2,safe.yMax+2),subject+" vertical projection "+point);
            Assert.That(ranking.Contains(point),Is.False,subject+" must not lie beneath the damage ranking.");
        }

        [UnityTest]public IEnumerator SavedForestTilesAndDefenderHeadsAvoidRankingAndInfectionModalOwnsTheOverlay()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);yield return null;
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            do
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.BlackForestChallengeActive&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }while(Time.realtimeSinceStartupAsDouble<deadline);
            Assert.That(controller,Is.Not.Null);Assert.That(controller.CampaignActive&&controller.BlackForestChallengeActive,Is.True);
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            Time.timeScale=0;SetSynergyModal(null);
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            deadline=Time.realtimeSinceStartupAsDouble+180;
            while((controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);SetSynergyModal(null);

            Rect safe=ManualBattleController.ShopMapSafeRect,ranking=ManualBattleController.DamageRankingPanelRect;
            Assert.That(safe,Is.EqualTo(new Rect(200,66,797,478)));
            Assert.That(ranking,Is.EqualTo(new Rect(1011,262,252,224)));
            Assert.That(safe.Overlaps(ranking),Is.False);Assert.That(ranking.xMin-safe.xMax,Is.EqualTo(14));
            int tiles=0;
            for(int row=1;row<=BlackForestLayout.Height;row++)for(int col=1;col<=BlackForestLayout.Width;col++)
            {
                char tile=BlackForestLayout.Tile(col,row);if(tile=='#')continue;
                tiles++;Vector3 center=BlackForestLayout.Cell(col,row,tile=='H'?.72f:0);
                foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})
                    AssertProjectedInside(center+new Vector3(x,0,z),safe,ranking,$"Tile {col},{row} corner {x},{z}");
            }
            Assert.That(tiles,Is.GreaterThan(0));Assert.That(controller.Battle.Defenders.Count,Is.EqualTo(9));
            foreach(var guard in controller.Battle.Defenders)
            {
                AssertProjectedInside(guard.Position,safe,ranking,guard.EntityId+" feet");
                AssertProjectedInside(guard.Position+Camera.main.transform.up*1.65f,safe,ranking,guard.EntityId+" head");
            }

            Assert.That(ModalOpen,Is.False);Assert.That(controller.DamageRankingVisible,Is.True);
            Assert.That(controller.ShopMapPointAvailable(safe.center),Is.True,"An uncovered map point proves this is an active deployment UI.");
            Assert.That(controller.ShopMapPointAvailable(ranking.center),Is.False,"Visible ranking must intercept battlefield input.");
            int dp=controller.Battle.DP,units=controller.Battle.Units.Count;
            controller.ProcessShopPointer(EventType.MouseDown,ranking.center);controller.ProcessShopPointer(EventType.MouseUp,ranking.center);
            Assert.That(controller.Battle.DP,Is.EqualTo(dp));Assert.That(controller.Battle.Units.Count,Is.EqualTo(units));
            SetSynergyModal("感染生物");
            Assert.That(ModalOpen,Is.True,"The infection fixture must enter the production CampaignUiModalOpen condition.");
            Assert.That(controller.DamageRankingVisible,Is.False,"Ranking must not paint over the infection modal.");
            Assert.That(controller.ShopMapPointAvailable(safe.center),Is.False,"The modal still owns battlefield input when ranking is hidden.");
            SetSynergyModal(null);
            Assert.That(ModalOpen,Is.False);Assert.That(controller.DamageRankingVisible,Is.True);
            Assert.That(controller.ShopMapPointAvailable(ranking.center),Is.False);Assert.That(controller.ShopMapPointAvailable(safe.center),Is.True);
            Debug.Log($"SHOP_MAP_SAFE_VIEWPORT_CHECK: {tiles*4} saved tile corners; 18 defender feet/head points; 14px ranking gap; real camera projection and infection modal visibility verified.");
        }
    }
}
