using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class SynergyBadgeSceneTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;float oldScale;
        [SetUp]public void Setup()=>oldScale=Time.timeScale;
        [TearDown]public void Cleanup(){if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();Time.timeScale=oldScale;}
        object Invoke(string name,params object[] args)=>typeof(ManualBattleController).GetMethod(name,Hidden).Invoke(controller,args);
        void Field(string name,object value)=>typeof(ManualBattleController).GetField(name,Hidden).SetValue(controller,value);
        Rect Rectangle(string property)=>(Rect)typeof(ManualBattleController).GetProperty(property,Hidden).GetValue(controller);
        Rect RowRectangle(string method,int index)=>(Rect)Invoke(method,index);
        Rect PartRectangle(string method,Rect row)=>(Rect)Invoke(method,row);
        SynergySummary[] Rows=>(SynergySummary[])typeof(ManualBattleController).GetProperty("CurrentSynergies",Hidden).GetValue(controller);
        IEnumerator Wait()
        {
            for(int i=0;i<1800&&(controller.ArtLoading||controller.ShopPanelTransitioning);i++)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
        }
        IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            for(int i=0;i<1800;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.CampaignActive&&!controller.ArtLoading)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.CampaignActive,Is.True);yield return Wait();Time.timeScale=0;
        }

        [Test]public void NineImportedMasksUploadToGpuWithTransparencyAndDifferentSilhouettes()
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(GraphicsDeviceType.Null),"This test requires the GPU PlayMode run.");
            var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(64,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var pixels=new Texture2D(64,64,TextureFormat.RGBA32,false,true);var shapes=new HashSet<int>();
            try
            {
                foreach(var entry in SynergyTrialCatalog.Entries)
                {
                    var icon=SynergyBadgePresentation.IconFor(entry.Tag);Assert.That(icon,Is.Not.Null,entry.Tag);
                    Graphics.Blit(icon,target);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();
                    var data=pixels.GetPixels32();Assert.That(data[0].a,Is.Zero,entry.Tag+" must not upload an opaque square background.");
                    Assert.That(data.Count(pixel=>pixel.a>180),Is.GreaterThan(8),entry.Tag+" must have visible glyph pixels.");
                    Assert.That(data.Count(pixel=>pixel.a<10),Is.GreaterThan(512),entry.Tag+" must retain transparent cutouts and margins.");
                    int shape=17;unchecked{foreach(var pixel in data)shape=shape*31+(pixel.a>90?1:0);}
                    Assert.That(shapes.Add(shape),Is.True,entry.Tag+" cannot silently use another synergy's fallback silhouette.");
                }
                var plates=Enum.GetValues(typeof(SynergyVisualGrade)).Cast<SynergyVisualGrade>().Select(SynergyBadgePresentation.FrameFor).Concat(new[]{SynergyBadgePresentation.CounterFor()});
                foreach(var plate in plates)
                {
                    Assert.That(plate,Is.Not.Null);Graphics.Blit(plate,target);RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();var data=pixels.GetPixels32();
                    Assert.That(data[0].a,Is.LessThan(32),"Plate/counter corners remain transparent; a faint shadow is allowed.");
                    Assert.That(data.Count(pixel=>pixel.a>180),Is.GreaterThan(8),"A plate must upload a real visible body.");
                }
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);}
        }

        [UnityTest]public IEnumerator SavedCampaignAndSandboxKeepRealGradesAndModalInputProtection()
        {
            yield return Load("Campaign");Assert.That(controller.SynergySandbox,Is.False);
            Assert.That(Rows.Length,Is.GreaterThan(0));Assert.That(Rows.All(row=>row.Tier==0&&SynergyBadgePresentation.GradeFor(row.Name,row.Tier)==SynergyVisualGrade.Inactive),Is.True);
            yield return Load("SynergyTrial");Assert.That(controller.SynergySandbox,Is.True);
            foreach(int count in new[]{2,4,6,8})
            {
                yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)SynergyTrialCatalog.Find("reunion").Members.Take(count).ToArray());
                var row=Rows.Single(item=>item.Name=="整合运动");
                var expected=count==2?SynergyVisualGrade.Bronze:count==4?SynergyVisualGrade.Silver:count==6?SynergyVisualGrade.Gold:SynergyVisualGrade.Prismatic;
                Assert.That(row.Tier,Is.EqualTo(count));Assert.That(SynergyBadgePresentation.GradeFor(row.Name,row.Tier),Is.EqualTo(expected));controller.Shop.Validate();
            }
            string[] twoFormations={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07"};
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)twoFormations);
            var limited=Rows.Single(item=>item.Name=="整合运动");
            Assert.That(limited.Count,Is.EqualTo(8));Assert.That(limited.Tier,Is.EqualTo(6));
            Assert.That(controller.Battle.ReunionCount,Is.EqualTo(8));Assert.That(controller.Battle.ReunionFormationCount,Is.EqualTo(2));
            Assert.That(controller.Battle.ReunionTier,Is.EqualTo(limited.Tier),"The live sidebar and battle must agree when population is sufficient but formations are missing.");
            string[] ids={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)ids);
            Assert.That(controller.SetGuerrillaTransfer("CARD_M07",true),Is.True);Assert.That(controller.Battle.GuerrillaTier,Is.EqualTo(8));
            Assert.That(SynergyBadgePresentation.GradeFor("游击队",Rows.Single(row=>row.Name=="游击队").Tier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);Assert.That(controller.Battle.GuerrillaTier,Is.EqualTo(9));
            Assert.That(SynergyBadgePresentation.GradeFor("游击队",Rows.Single(row=>row.Name=="游击队").Tier),Is.EqualTo(SynergyVisualGrade.Prismatic));
            Assert.That(controller.StartLoopNextBattle(),Is.True);controller.SetShopVisible(false);yield return Wait();
            var entry=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(controller.Battle.Entry));
            Assert.That(controller.ShopMapPointAvailable(entry),Is.True);int dp=controller.Battle.DP,owned=controller.Shop.Bench.Count;
            // Modal state is an explicit test fixture; actual opening/closing clicks are verified
            // independently by the visible Player driver, not claimed by this reflection check.
            Field("inspectedSynergy","游击队");Assert.That(controller.ShopMapPointAvailable(entry),Is.False);
            var piece=controller.ShopPieceRect(0).center;
            controller.ProcessShopPointer(EventType.MouseDown,piece);controller.ProcessShopPointer(EventType.MouseDrag,entry);controller.ProcessShopPointer(EventType.MouseUp,entry);
            Assert.That(controller.Battle.Units,Is.Empty);Assert.That(controller.Battle.DP,Is.EqualTo(dp));Assert.That(controller.Shop.Bench.Count,Is.EqualTo(owned));
            Field("inspectedSynergy",null);Assert.That(controller.ShopMapPointAvailable(entry),Is.True);controller.Shop.Validate();
        }

        [UnityTest]public IEnumerator DenseLegalLineupScrollsActionsWithinTheSameBlockedViewport()
        {
            yield return Load("SynergyTrial");
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)SynergyTrialCatalog.Find("reunion").Members.ToArray());
            Assert.That(controller.StartLoopNextBattle(),Is.True);controller.SetShopVisible(false);yield return Wait();
            var viewport=Rectangle("CampaignQuickActionsRect");
            Assert.That((float)Invoke("CampaignQuickActionsContentHeight"),Is.LessThanOrEqualTo(viewport.height),"The default eight-kind trial needs no scrolling.");
            Invoke("ReplaySandbox");
            string[] dense={"CARD_M03","CARD_M19","CARD_M21","CARD_M24","CARD_M29","CARD_M30","CARD_M33","CARD_M34","CARD_M02","CARD_M07"};
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)dense);
            Assert.That(controller.StartLoopNextBattle(),Is.True);controller.SetShopVisible(false);yield return Wait();
            controller.Battle.Begin();controller.Battle.SetPaused(true);Field("inspectedSynergy",null);
            viewport=Rectangle("CampaignQuickActionsRect");float content=(float)Invoke("CampaignQuickActionsContentHeight");
            Assert.That(content,Is.GreaterThan(viewport.height));Assert.That(viewport.yMax,Is.LessThan(552));controller.Shop.Validate();
            // Geometry fixture only; actual scroll-wheel input is checked in the visible Player.
            Field("campaignQuickActionsScroll",new Vector2(0,content-viewport.height));
            var returnButton=new Rect(viewport.x,viewport.y+content-26-(content-viewport.height),157,26);
            Assert.That(returnButton.yMin,Is.GreaterThanOrEqualTo(viewport.yMin));Assert.That(returnButton.yMax,Is.EqualTo(viewport.yMax));
            Assert.That((bool)Invoke("CampaignOverlayBlocksPoint",returnButton.center),Is.True);
            Assert.That(controller.ShopMapPointAvailable(returnButton.center),Is.False);
            var below=new Vector2(viewport.center.x,viewport.yMax+2);
            Assert.That((bool)Invoke("CampaignOverlayBlocksPoint",below),Is.False,"The clipped offscreen content must not extend the blocked map area.");
        }

        [UnityTest]public IEnumerator NineTagLayoutPreservesSupplementsScrollHitAreasAndReachableBattleActions()
        {
            yield return Load("SynergyTrial");
            string[] ids={"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14","CARD_M03","CARD_M26","CARD_M21","CARD_M29","CARD_M33"};
            yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)ids);
            Assert.That(controller.Shop.Level,Is.EqualTo(10));Assert.That(controller.Shop.Bench.Count,Is.EqualTo(10));Assert.That(Rows.Length,Is.EqualTo(9));
            controller.Shop.Validate();var viewport=Rectangle("SynergyRowsViewportRect");
            Assert.That(viewport.yMax,Is.LessThanOrEqualTo(controller.CampaignConstructToggleRect.yMin));
            Assert.That(controller.CampaignConstructToggleRect.yMax,Is.LessThanOrEqualTo(552));
            for(int i=0;i<Rows.Length;i++)
            {
                var row=RowRectangle("SynergyRowContentRect",i);
                var name=PartRectangle("SynergyNameRect",row);var thresholds=PartRectangle("SynergyThresholdRect",row);
                var supplement=PartRectangle("SynergySupplementRect",row);var count=PartRectangle("SynergyCountRect",row);
                Assert.That(count.xMax,Is.LessThanOrEqualTo(name.xMin),"The count bubble cannot cover the name.");
                Assert.That(name.yMax,Is.LessThanOrEqualTo(thresholds.yMin));
                Assert.That(thresholds.yMax,Is.LessThanOrEqualTo(supplement.yMin));
                Assert.That(supplement.yMax,Is.LessThanOrEqualTo(row.yMax+1),"Extra information stays in its own line.");
                Assert.That(thresholds.width,Is.GreaterThanOrEqualTo(86),"The five-part guerrilla sequence must fit without dropping a threshold.");
            }
            Assert.That(controller.StartLoopNextBattle(),Is.True);controller.SetShopVisible(false);yield return Wait();controller.Battle.Begin();yield return null;
            Assert.That(controller.Battle.SnowTier,Is.EqualTo(5));Assert.That(controller.Battle.InfectionTier,Is.EqualTo(2));
            Assert.That((bool)typeof(ManualBattleController).GetProperty("SnowstormChargeHudVisible",Hidden).GetValue(controller),Is.True);
            viewport=Rectangle("SynergyRowsViewportRect");Assert.That(viewport.height,Is.LessThanOrEqualTo(4*48+1));
            var quick=Rectangle("CampaignQuickActionsRect");Assert.That(controller.CampaignConstructToggleRect.yMax,Is.LessThanOrEqualTo(quick.yMin));
            Assert.That(quick.yMax,Is.LessThanOrEqualTo(552),"The full quick-action area must stay above the deployment bar.");
            var lastContent=RowRectangle("SynergyRowContentRect",Rows.Length-1);
            // Geometry fixture only: the independent Player requires a real OS scroll gesture.
            Field("campaignSynergyScroll",new Vector2(0,Mathf.Max(0,lastContent.yMax-viewport.height)));
            var lastScreen=RowRectangle("SynergyRowScreenRect",Rows.Length-1);
            Assert.That(lastScreen.yMin,Is.GreaterThanOrEqualTo(viewport.yMin-1));Assert.That(lastScreen.yMax,Is.LessThanOrEqualTo(viewport.yMax+1));
            Assert.That(viewport.Contains(lastScreen.center),Is.True);Assert.That(RowRectangle("SynergyRowScreenRect",0).yMax,Is.LessThan(viewport.yMin));
            var returnButton=new Rect(quick.x,quick.y+26,quick.width,26);
            Assert.That(returnButton.yMax,Is.LessThanOrEqualTo(552));Assert.That(viewport.Overlaps(returnButton),Is.False);
        }
    }
}
