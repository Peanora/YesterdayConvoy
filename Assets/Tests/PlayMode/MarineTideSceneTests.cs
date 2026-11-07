using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class MarineTideSceneTests
    {
        float previousScale;
        [SetUp]public void Setup(){previousScale=Time.timeScale;Time.timeScale=1;}
        [TearDown]public void Cleanup()
        {
            var controller=Controller;
            if(controller!=null&&controller.CampaignActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }

        static ManualBattleController Controller=>Object.FindAnyObjectByType<ManualBattleController>();
        static MarineTideView View=>Object.FindAnyObjectByType<MarineTideView>();
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("MarineTrial");
            for(int i=0;i<1500;i++)
            {
                var c=Controller;
                if(c!=null&&c.CampaignActive&&c.Loop!=null&&c.Loop.CanShop&&!c.ArtLoading&&!c.ShopPanelTransitioning)
                {Time.timeScale=0;yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("The saved MarineTrial did not finish preparing.");
        }

        static void BeginAndDeploySavedRoster(ManualBattleController c)
        {
            CollectionAssert.AreEquivalent(new[]{"CARD_M33","CARD_M34","CARD_M35","CARD_M36"},c.Shop.Bench.Select(p=>p.Id));
            Assert.That(c.Battle.MarineTier,Is.EqualTo(4));
            Assert.That(c.Battle.MarineCells,Is.Empty,"Preparing or merely owning the roster cannot seed ground.");
            Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();
            long request=0;
            // Actual card deployment, DP and normal authored north/south routes are used in every case.
            // Spread needs time to become visible; sandbox snapshots already apply their normal endurance multipliers.
            foreach(var id in new[]{"CARD_M35","CARD_M33","CARD_M34","CARD_M36"})
            {
                var lane=c.Battle.Lanes[id=="CARD_M34"?1:0];
                var receipt=c.Battle.TryDeployRole(c.Battle.RunId,++request,lane.Entry,id);
                Assert.That(receipt.Success,Is.True,id);c.Tick(0);
            }
            Assert.That(c.Battle.Units.Count(u=>!u.SynergySupport),Is.EqualTo(4));
            Assert.That(c.Battle.MarineCells,Is.Empty,"Dropping cards at the red gates must not count as real contact.");
        }

        static IEnumerator AdvanceUntilSeed(ManualBattleController c)
        {
            for(int i=0;i<1000&&c.Battle.MarineCells.Count==0&&c.Battle.Phase==BattlePhase.Running;i++)
            {c.Tick(.025f);if(i%20==0)yield return null;}
            Assert.That(c.Battle.MarineCells.Count,Is.GreaterThan(0),"An authored defender must actually attack, or be hit by, a deployed marine.");
            Assert.That(c.Battle.MarineSeededKinds,Is.GreaterThan(0));
            Assert.That(View,Is.Not.Null);View.Present();yield return null;
        }

        static void Advance(ManualBattleController c,float seconds)
        {
            int steps=Mathf.CeilToInt(seconds/.025f);
            for(int i=0;i<steps;i++)c.Tick(Mathf.Min(.025f,seconds-i*.025f));
        }

        [UnityTest]public IEnumerator SavedFourMarineRosterNaturallySeedsSpreadsAndAmplifiesNeuralBursts()
        {
            yield return Load();var c=Controller;BeginAndDeploySavedRoster(c);
            bool amplified=false;int largestCoverage=0;
            for(int i=0;i<3200&&c.Battle.Phase==BattlePhase.Running;i++)
            {
                c.Tick(.025f);largestCoverage=Mathf.Max(largestCoverage,c.Battle.MarineCells.Count);
                amplified|=c.Battle.Units.Any(u=>c.Battle.MarineNeuralMultiplier(u)>1);
                if(c.Battle.MarineSpreadCount>0&&c.Battle.NeuralBursts>0&&c.Battle.MarineBonusNeuralDealt>0&&c.Battle.MarineSeededKinds>=2)break;
                if(i%20==0)yield return null;
            }
            yield return null;View.Present();
            Assert.That(c.Battle.MarineSeededKinds,Is.GreaterThanOrEqualTo(2),"Different normally deployed species must make real contact.");
            Assert.That(c.Battle.MarineSpreadCount,Is.GreaterThan(0),"A real simulation interval must grow neighbouring authored floor.");
            Assert.That(largestCoverage,Is.LessThanOrEqualTo(16));
            Assert.That(amplified,Is.True);Assert.That(c.Battle.MarineBonusNeuralDealt,Is.GreaterThan(0));
            Assert.That(c.Battle.NeuralBursts,Is.GreaterThan(0),"Use the real shared neural accumulator, never a test-only burst trigger.");
            Assert.That(c.Battle.Defenders.Any(d=>d.NeuralBursts>0&&d.Health<d.MaxHealth),Is.True);
            Assert.That(View.VisibleTileCount,Is.EqualTo(c.Battle.MarineCells.Count));
            Assert.That(c.Battle.MarineCells.Select(t=>t.Cell).Distinct().Count(),Is.EqualTo(c.Battle.MarineCells.Count));
            foreach(var high in c.Battle.Defenders.Where(d=>d.HighGround))
            {
                var cell=new Vector2Int(Mathf.FloorToInt(high.Position.x+.5f),Mathf.FloorToInt(high.Position.z+.5f));
                Assert.That(c.Battle.MarineCells.Any(t=>t.Cell==cell),Is.False,"An authored high platform must not become an infected ground tile.");
            }
        }

        [UnityTest]public IEnumerator OriginalTexturesAndCoverageInspectionChangeTheActualSavedGameCamera()
        {
            yield return Load();var c=Controller;BeginAndDeploySavedRoster(c);yield return AdvanceUntilSeed(c);
            Advance(c,1);c.Battle.SetPaused(true);yield return null;
            var view=View;view.Present();
            Assert.That(view.SourceTexture,Is.Not.Null);Assert.That(view.SourceTexture.name,Is.EqualTo("Special_juntan_01"));
            Assert.That(view.SourceTexture.width,Is.EqualTo(256));Assert.That(view.SourceTexture.height,Is.EqualTo(256));
            var renderer=view.GroundRenderers.First(r=>r.enabled);var material=renderer.sharedMaterial;
            Assert.That(material.shader.isSupported,Is.True);
            Assert.That(material.GetTexture("_MainTex"),Is.SameAs(view.SourceTexture));
            foreach(var slot in new[]{"_DetailTex","_VeinTex","_FlowTex"})
                Assert.That(material.GetTexture(slot),Is.InstanceOf<Texture2D>(),slot+" must resolve the original supporting texture.");
            Assert.That(Camera.main,Is.Not.Null);
            view.PresentationVisible=false;view.Present();var without=Capture(Camera.main);
            view.PresentationVisible=true;view.Present();var ordinary=Capture(Camera.main);
            Assert.That(ChangedPixels(without,ordinary,8),Is.GreaterThan(60),"Actual game-camera pixels must show source art on the floor; renderer counts alone are insufficient.");
            Assert.That(c.ToggleMarineCoverage(),Is.True);view.Present();
            Assert.That(c.MarineCoverageHighlighted,Is.True);Assert.That(view.HighlightCoverage,Is.True);
            var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);Assert.That(properties.GetFloat("_Highlight"),Is.EqualTo(1));
            var inspected=Capture(Camera.main);
            Assert.That(ChangedPixels(ordinary,inspected,8),Is.GreaterThan(30),"The inspection action must visibly emphasize the actual covered cells.");
            Assert.That(c.ToggleMarineCoverage(),Is.True);view.Present();
            Assert.That(c.MarineCoverageHighlighted,Is.False);Assert.That(view.HighlightCoverage,Is.False);
            Assert.That(ChangedPixels(ordinary,Capture(Camera.main),2),Is.Zero,"Closing inspection while paused must restore the same real-camera floor.");
        }

        [UnityTest]public IEnumerator PauseFreezesOrganicShaderAndSpreadWhileRestartClearsCoverageAndInspection()
        {
            yield return Load();var c=Controller;BeginAndDeploySavedRoster(c);yield return AdvanceUntilSeed(c);
            Advance(c,1);var view=View;view.Present();
            Assert.That(c.ToggleMarineCoverage(),Is.True);view.Present();
            c.Battle.SetPaused(true);yield return null;view.Present();
            double clock=c.Battle.Clock;double spreadRemaining=c.Battle.MarineSpreadRemaining;
            var cells=c.Battle.MarineCells.Select(t=>t.Cell).ToArray();
            var renderer=view.GroundRenderers.First(r=>r.enabled);var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
            float shaderTime=properties.GetFloat("_MarineTime");float growth=properties.GetFloat("_Growth");
            var before=Capture(Camera.main);
            c.Tick(9);yield return null;yield return null;view.Present();renderer.GetPropertyBlock(properties);
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.MarineSpreadRemaining,Is.EqualTo(spreadRemaining));
            CollectionAssert.AreEqual(cells,c.Battle.MarineCells.Select(t=>t.Cell).ToArray());
            Assert.That(properties.GetFloat("_MarineTime"),Is.EqualTo(shaderTime));Assert.That(properties.GetFloat("_Growth"),Is.EqualTo(growth));
            Assert.That(ChangedPixels(before,Capture(Camera.main),2),Is.Zero,"Clock-driven source-texture distortion and growth must freeze in actual camera output.");
            c.Battle.SetPaused(false);Advance(c,.4f);view.Present();renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_MarineTime"),Is.GreaterThan(shaderTime));
            Assert.That(c.RestartCampaignEncounter(),Is.True);c.Tick(0);yield return null;view.Present();
            Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Ready));Assert.That(c.Battle.MarineCells,Is.Empty);
            Assert.That(c.Battle.MarineSeededKinds,Is.Zero);Assert.That(c.Battle.MarineSpreadCount,Is.Zero);
            Assert.That(c.Battle.MarineBonusNeuralDealt,Is.Zero);Assert.That(c.Battle.MarineGroundNeuralDealt,Is.Zero);
            Assert.That(view.VisibleTileCount,Is.Zero);Assert.That(view.GroundRenderers.All(r=>!r.enabled),Is.True);
            Assert.That(c.MarineCoverageHighlighted,Is.False);Assert.That(view.HighlightCoverage,Is.False);
        }

        static int ChangedPixels(Color32[] a,Color32[] b,int threshold)
        {
            Assert.That(a.Length,Is.EqualTo(b.Length));int count=0;
            for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>threshold)count++;
            return count;
        }

        static Color32[] Capture(Camera camera)
        {
            var target=new RenderTexture(640,360,24);var image=new Texture2D(640,360,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previous;
                target.Release();Object.Destroy(target);Object.Destroy(image);
            }
        }
    }
}
