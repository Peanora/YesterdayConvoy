using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class SnowstormSceneTests
    {
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        float previousScale;
        ManualBattleController controller;

        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }

        IEnumerator StartRealStorm()
        {
            // Use the same authored sandbox and five-member roster as SynergyReworkSceneTests.
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<900;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);
            Assert.That(controller.SynergySandbox&&!controller.ArtLoading,Is.True,"Sandbox load timeout");
            Time.timeScale=0;
            Assert.That(controller.SetSandboxRoster(0),Is.True);
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            // The shop transition uses unscaled presentation time and must finish before HUD checks.
            for(int i=0;i<300&&controller.ShopPanelTransitioning;i++)yield return null;
            Assert.That(controller.ShopPanelTransitioning,Is.False);

            var battle=controller.Battle;
            Assert.That(battle.SnowTier,Is.EqualTo(5));
            battle.Begin();
            var receipt=battle.TryDeployRole(battle.RunId,9001,battle.Entry,"CARD_M14");
            Assert.That(receipt.Success,Is.True);
            var frost=battle.FindUnit(receipt.EntityId);
            Assert.That(frost,Is.Not.Null);
            for(int i=0;i<250&&!battle.IsSkillReady(frost.EntityId);i++)controller.Tick(.1f);
            Assert.That(battle.IsSkillReady(frost.EntityId),Is.True,"Charge SP through the actual simulation.");
            Assert.That(battle.TryActivateSkill(battle.RunId,9002,frost.EntityId),Is.EqualTo(SkillFailure.None));
            controller.Tick(.7f);
            yield return null;yield return null;
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(battle.SnowstormActive,Is.True);
            Assert.That(battle.SnowstormPulses.Count,Is.EqualTo(1));
        }

        static bool Hud(ManualBattleController owner,string name)
        {
            var property=typeof(ManualBattleController).GetProperty(name,PrivateInstance);
            Assert.That(property,Is.Not.Null,name);
            return (bool)property.GetValue(owner);
        }

        static MeshRenderer[] WeatherRenderers(SnowstormView view)=>view.GetComponentsInChildren<MeshRenderer>()
            .Where(r=>r.name.StartsWith("Snowstorm / ")).OrderBy(r=>r.name).ToArray();
        static Vector3[] Vertices(MeshRenderer renderer)=>renderer.GetComponent<MeshFilter>().sharedMesh.vertices;
        static float[] Health(ManualBattle battle)=>battle.Units.Select(u=>u.Health)
            .Concat(battle.Defenders.Select(d=>d.Health)).ToArray();

        static int WeatherPixelContribution(MeshRenderer[] renderers)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),
                "This is an actual GPU pixel check; a graphics device is required.");
            var camera=Camera.main;
            Assert.That(camera,Is.Not.Null);
            const int width=640;
            int height=Mathf.Max(1,Mathf.RoundToInt(width*(float)Screen.height/Screen.width));
            var target=new RenderTexture(width,height,24);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previousTarget=camera.targetTexture;
            var previousActive=RenderTexture.active;
            var enabled=renderers.Select(r=>r.enabled).ToArray();
            Color32[] Capture()
            {
                camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                return image.GetPixels32();
            }
            try
            {
                target.Create();camera.targetTexture=target;
                var visible=Capture();
                foreach(var renderer in renderers)renderer.enabled=false;
                var hidden=Capture();
                int changed=0;
                for(int i=0;i<visible.Length;i++)
                    if(System.Math.Abs(visible[i].r-hidden[i].r)+System.Math.Abs(visible[i].g-hidden[i].g)+
                        System.Math.Abs(visible[i].b-hidden[i].b)>12)changed++;
                return changed;
            }
            finally
            {
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                target.Release();Object.Destroy(target);Object.Destroy(image);
            }
        }

        [UnityTest]public IEnumerator RealStormDrawsPixelsFreezesAllGeometryAndExpiresAfterFourPulses()
        {
            yield return StartRealStorm();
            var battle=controller.Battle;
            var view=controller.GetComponent<SnowstormView>();
            Assert.That(view,Is.Not.Null);
            var renderers=WeatherRenderers(view);
            Assert.That(renderers.Length,Is.EqualTo(4));
            Assert.That(renderers.All(r=>r.enabled),Is.True);
            Assert.That(view.VisibleLayers,Is.EqualTo(4));
            Assert.That(view.Strength,Is.GreaterThan(.9f));
            Assert.That(view.WeatherScreenBounds.width,Is.GreaterThan(Screen.width*.3f));
            Assert.That(view.WeatherScreenBounds.height,Is.GreaterThan(Screen.height*.2f));
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.True);

            double clock=battle.Clock;
            var health=Health(battle);
            Assert.That(WeatherPixelContribution(renderers),Is.GreaterThan(500),
                "Disabling only the four weather batches must remove substantial actual camera pixels.");
            Assert.That(battle.Clock,Is.EqualTo(clock));
            CollectionAssert.AreEqual(health,Health(battle),"Rendering weather cannot change HP.");

            battle.SetPaused(true);
            double remaining=battle.SnowstormRemaining,presentation=controller.PresentationClock;
            float age=view.WeatherAge;
            var pausedVertices=renderers.Select(Vertices).ToArray();
            controller.Tick(3);
            yield return null;yield return null;
            Assert.That(battle.Clock,Is.EqualTo(clock));
            Assert.That(controller.PresentationClock,Is.EqualTo(presentation));
            Assert.That(battle.SnowstormRemaining,Is.EqualTo(remaining));
            Assert.That(view.WeatherAge,Is.EqualTo(age));
            Assert.That(battle.SnowstormPulses.Count,Is.EqualTo(1));
            for(int i=0;i<renderers.Length;i++)CollectionAssert.AreEqual(pausedVertices[i],Vertices(renderers[i]),renderers[i].name);
            CollectionAssert.AreEqual(health,Health(battle));

            battle.SetPaused(false);controller.Tick(.25f);
            yield return null;yield return null;
            Assert.That(battle.Clock-clock,Is.EqualTo(.25).Within(.0001));
            Assert.That(view.WeatherAge,Is.GreaterThan(age));
            Assert.That(renderers.Select((r,i)=>!Vertices(r).SequenceEqual(pausedVertices[i])).Any(changed=>changed),Is.True,
                "Resuming simulation must move the weather instead of holding a still frame.");

            controller.Tick((float)(battle.SnowstormEndsAt-battle.Clock+.001));
            yield return null;yield return null;
            Assert.That(battle.Phase,Is.EqualTo(BattlePhase.Running));
            Assert.That(battle.SnowstormActive,Is.False);
            Assert.That(battle.SnowstormPulses.Count,Is.EqualTo(4));
            for(int i=0;i<4;i++)Assert.That(battle.SnowstormPulses[i].At,
                Is.EqualTo(battle.SnowstormStartedAt+i*2).Within(.0001));
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.False);
            Assert.That(view.VisibleLayers,Is.EqualTo(4),"Natural expiry retains its short visual fade.");
            controller.Tick(.66f);
            yield return null;yield return null;
            Assert.That(view.VisibleLayers,Is.Zero);
            Assert.That(view.Strength,Is.Zero);
            Assert.That(renderers.Any(r=>r.enabled),Is.False);

            controller.RestartBattle();
            for(int i=0;i<900&&controller.ArtLoading;i++)yield return null;
            yield return null;yield return null;
            Assert.That(controller.ArtLoading,Is.False);
            Assert.That(controller.Battle.SnowstormPulses,Is.Empty);
            Assert.That(controller.Battle.SnowstormActive,Is.False);
            Assert.That(view.VisibleLayers,Is.Zero);
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.False);
        }

        [UnityTest]public IEnumerator SettlementHidesHudImmediatelyAndFadesWeatherWithoutAdvancingCombat()
        {
            yield return StartRealStorm();
            var battle=controller.Battle;
            var view=controller.GetComponent<SnowstormView>();
            Assert.That(view.VisibleLayers,Is.EqualTo(4));
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.True);
            // Explicit terminal-state fixture; this test makes no natural victory/defeat claim.
            var finish=typeof(ManualBattle).GetMethod("FinishForTesting",PrivateInstance);
            Assert.That(finish,Is.Not.Null);
            Assert.That((bool)finish.Invoke(battle,new object[]{battle.RunId,false}),Is.True);
            double clock=battle.Clock,remaining=battle.SnowstormRemaining;
            var health=Health(battle);
            Assert.That(battle.SnowstormActive,Is.True,"The stopped model clock intentionally remains inside the eight-second interval.");
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.False);
            Assert.That(Hud(controller,"SnowstormHudVisible"),Is.False);
            controller.Tick(0);
            yield return null;yield return null;
            float initial=view.Strength;
            Assert.That(initial,Is.GreaterThan(.9f));
            controller.Tick(.25f);
            yield return null;yield return null;
            Assert.That(view.Strength,Is.GreaterThan(0).And.LessThan(initial));
            controller.Tick(.26f);
            yield return null;yield return null;
            Assert.That(view.VisibleLayers,Is.Zero);
            Assert.That(view.Strength,Is.Zero);
            Assert.That(WeatherRenderers(view).Any(r=>r.enabled),Is.False);
            Assert.That(battle.Clock,Is.EqualTo(clock));
            Assert.That(battle.SnowstormRemaining,Is.EqualTo(remaining));
            Assert.That(battle.SnowstormPulses.Count,Is.EqualTo(1));
            CollectionAssert.AreEqual(health,Health(battle),"The terminal fade cannot apply more combat damage.");
            Assert.That(Hud(controller,"SnowstormHudActive"),Is.False);
            // A long presentation step can happen before the view observes the result at all.
            // The fade must start at the frozen combat result time, not that late observation.
            yield return StartRealStorm();battle=controller.Battle;view=controller.GetComponent<SnowstormView>();
            Assert.That((bool)finish.Invoke(battle,new object[]{battle.RunId,false}),Is.True);
            clock=battle.Clock;health=Health(battle);controller.Tick(.7f);
            yield return null;yield return null;
            Assert.That(view.VisibleLayers,Is.Zero,"A delayed first terminal frame cannot restart the weather fade.");
            Assert.That(Hud(controller,"SnowstormHudVisible"),Is.False);
            Assert.That(battle.Clock,Is.EqualTo(clock));CollectionAssert.AreEqual(health,Health(battle));
        }
    }
}
