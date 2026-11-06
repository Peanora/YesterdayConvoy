using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class InfectionFeedbackSceneTests
    {
        float scale;
        ManualBattleController controller;
        SynergyTrialDefinition trial;
        DefenderTrialDefinition encounter;
        [SetUp]public void Setup()=>scale=Time.timeScale;
        [TearDown]public void Cleanup()
        {if(controller!=null)controller.LeaveShopTrial();Time.timeScale=scale;if(trial!=null)Object.Destroy(trial);if(encounter!=null)Object.Destroy(encounter);}
        static Renderer[] InfectionRenderers()=>Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r=>r.enabled&&(r.name.StartsWith("Infection burst / ")||r.name=="Original VFX / common/common_firemagic_hit_01")).ToArray();
        static int Pixels(Renderer[] renderers)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            var camera=Camera.main;var before=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(640,360,24);
            var image=new Texture2D(640,360,TextureFormat.RGB24,false);
            Color32[] Capture(){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();}
            try
            {
                camera.targetTexture=target;var a=Capture();foreach(var r in renderers)r.enabled=false;var b=Capture();int changed=0;
                for(int i=0;i<a.Length;i++)if(System.Math.Abs(a[i].r-b[i].r)+System.Math.Abs(a[i].g-b[i].g)+System.Math.Abs(a[i].b-b[i].b)>12)changed++;
                return changed;
            }
            finally{foreach(var r in renderers)r.enabled=true;camera.targetTexture=before;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
        [UnityTest]public IEnumerator RealBurstShowsActualHitsFreezesWithPauseAndCleansItsTerminalTail()
        {
            yield return SceneManager.LoadSceneAsync("InfectionTrial");
            for(int i=0;i<1200;i++){controller=Object.FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.CampaignActive&&!controller.ArtLoading)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.ArtLoading,Is.False);Time.timeScale=0;
            var original=Object.FindAnyObjectByType<CampaignBootstrap>().trialDefinition;
            var originalHealth=original.encounter.defenders.Select(d=>d.health).ToArray();
            trial=Object.Instantiate(original);encounter=Object.Instantiate(original.encounter);trial.encounter=encounter;
            encounter.defenders=original.CopyDefenders();Vector3 entry=encounter.lanes[0].points[0];
            // Runtime-only durable defenders isolate the presentation from the authored difficulty.
            for(int i=0;i<encounter.defenders.Length;i++)
            {var d=encounter.defenders[i];d.health=100000;d.attack=1;d.range=.05f;d.noOrdinaryAttack=true;d.skill=null;if(i<2)d.position=entry+new Vector3(i==0?-.5f:.5f,0,.3f);}
            yield return controller.PrepareCampaign(true,65,trial);
            Assert.That(controller.SetCampaignForms(InfectionForm.Burst,InfectionForm.Shell),Is.True);
            Assert.That(controller.StartLoopNextBattle(),Is.True);while(controller.ArtLoading||controller.ShopPanelTransitioning)yield return null;
            var b=controller.Battle;b.Begin();var receipt=b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M02");Assert.That(receipt.Success,Is.True);
            Assert.That(b.TryDamageUnit(receipt.EntityId,1000000,CombatDamageType.Arts),Is.True);controller.Tick(.1f);yield return null;yield return null;
            var view=Object.FindAnyObjectByType<InfectionBurstFeedback>();Assert.That(view,Is.Not.Null);Assert.That(view.VisibleMarkers,Is.GreaterThan(0));
            var burst=b.InfectionBursts.Single();Assert.That(burst.Resolved,Is.False);
            b.SetPaused(true);double clock=b.Clock;controller.Tick(10);yield return null;
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(burst.Hits,Is.Empty);
            b.SetPaused(false);controller.Tick(.18f);yield return null;yield return null;
            Assert.That(burst.Hits.Count,Is.EqualTo(2));Assert.That(burst.TotalDamage,Is.EqualTo(170).Within(.01));
            Assert.That(view.VisibleBursts,Is.EqualTo(1));Assert.That(view.LastVisibleHitCount,Is.EqualTo(2));Assert.That(view.VisibleLabels,Is.EqualTo(1));
            var renderers=InfectionRenderers();Assert.That(renderers.Length,Is.GreaterThan(1));clock=b.Clock;
            Assert.That(Pixels(renderers),Is.GreaterThan(50));Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(burst.TotalDamage,Is.EqualTo(170).Within(.01));
            var markers=renderers.OfType<LineRenderer>().ToArray();var points=markers.Select(r=>{var p=new Vector3[r.positionCount];r.GetPositions(p);return p;}).ToArray();
            b.SetPaused(true);controller.Tick(5);yield return null;
            for(int i=0;i<markers.Length;i++){var p=new Vector3[markers[i].positionCount];markers[i].GetPositions(p);CollectionAssert.AreEqual(points[i],p);}
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(burst.Hits.Count,Is.EqualTo(2));b.SetPaused(false);
            var finish=typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That((bool)finish.Invoke(b,new object[]{b.RunId,false}),Is.True);controller.Tick(.4f);yield return null;yield return null;
            Assert.That(view.VisibleLabels,Is.GreaterThan(0),"A real blast already emitted before settlement retains its short label tail.");
            controller.Tick(1);yield return null;yield return null;
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(view.VisibleMarkers,Is.Zero);Assert.That(view.VisibleLabels,Is.Zero);
            typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);yield return null;yield return null;
            Assert.That(controller.Battle.InfectionBursts,Is.Empty);Assert.That(view.VisibleMarkers,Is.Zero);
            CollectionAssert.AreEqual(originalHealth,original.encounter.defenders.Select(d=>d.health));
        }
    }
}
