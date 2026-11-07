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
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);}
        }

        [UnityTest]public IEnumerator SavedCampaignAndSandboxKeepRealGradesAndModalInputProtection()
        {
            yield return Load("Campaign");Assert.That(controller.SynergySandbox,Is.False);
            Assert.That(Rows.Length,Is.GreaterThan(0));Assert.That(Rows.All(row=>row.Tier==0&&SynergyBadgePresentation.GradeFor(row.Name,row.Tier)==SynergyVisualGrade.Inactive),Is.True);
            yield return Load("SynergyTrial");Assert.That(controller.SynergySandbox,Is.True);
            foreach(int count in new[]{2,4,6})
            {
                yield return (IEnumerator)Invoke("PrepareSynergyBadgeEvidenceRoster",(object)SynergyTrialCatalog.Find("reunion").Members.Take(count).ToArray());
                var row=Rows.Single(item=>item.Name=="整合运动");
                var expected=count==2?SynergyVisualGrade.Bronze:count==4?SynergyVisualGrade.Silver:SynergyVisualGrade.Gold;
                Assert.That(row.Tier,Is.EqualTo(count));Assert.That(SynergyBadgePresentation.GradeFor(row.Name,row.Tier),Is.EqualTo(expected));controller.Shop.Validate();
            }
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
    }
}
