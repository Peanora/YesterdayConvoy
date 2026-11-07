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
    public sealed class HerdSpiritSceneTests
    {
        float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()=>Time.timeScale=previousScale;
        static ManualBattleController Controller=>Object.FindAnyObjectByType<ManualBattleController>();
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("HerdTrial");
            for(int i=0;i<1500;i++)
            {
                var c=Controller;
                if(c!=null&&c.CampaignActive&&c.Loop!=null&&c.Loop.CanShop&&!c.ArtLoading&&!c.ShopPanelTransitioning)
                {Time.timeScale=0;yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("The saved HerdTrial did not finish preparing.");
        }
        static DemoSpriteVisual Visual(DeployedUnitState unit)
        {
            var actor=GameObject.Find(unit.EntityId);Assert.That(actor,Is.Not.Null,unit.EntityId);
            var result=actor.GetComponentInChildren<DemoSpriteVisual>(true);Assert.That(result,Is.Not.Null);return result;
        }
        static HerdSpiritVisual Effect(DeployedUnitState unit)
        {
            var effect=GameObject.Find(unit.EntityId).GetComponentInChildren<HerdSpiritVisual>(true);
            Assert.That(effect,Is.Not.Null);return effect;
        }
        static void AdvanceTo(ManualBattleController c,double at)
        {while(c.Battle.Clock+0.00001<at)c.Tick((float)System.Math.Min(.025,at-c.Battle.Clock));}
        static DeployedUnitState Deploy(ManualBattleController c,string id,long request)
        {
            var receipt=c.Battle.TryDeployRole(c.Battle.RunId,request,c.Battle.Entry,id);
            Assert.That(receipt.Success,Is.True);c.Tick(0);return c.Battle.FindUnit(receipt.EntityId);
        }
        static float SpiritTime(DemoSpriteVisual visual)
        {var properties=new MaterialPropertyBlock();visual.Renderer.GetPropertyBlock(properties);return properties.GetFloat("_SpiritTime");}
        static Vector3[] MistVertices(HerdSpiritVisual effect)
        {
            var filter=effect.MistRenderer.GetComponent<MeshFilter>();
            Assert.That(filter,Is.Not.Null);return filter.sharedMesh.vertices;
        }

        [UnityTest]public IEnumerator ActualDefenderKillFallsRisesAndPausesWithoutMovingOrAttacking()
        {
            yield return Load();var c=Controller;Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();
            var source=Deploy(c,"CARD_M21",1);yield return null;
            for(int i=0;i<1800&&source.Active;i++)
            {c.Tick(.05f);if(i%10==0)yield return null;}
            Assert.That(source.Resolution,Is.EqualTo(UnitResolution.Dead),"This case requires an actual defender kill, not injected damage.");
            c.Tick(.001f);yield return null;
            var state=c.Battle.HerdSpiritFrom(source.EntityId);Assert.That(state,Is.Not.Null);
            var host=c.Battle.FindUnit(state.EntityId);var original=Visual(source);var spirit=Visual(host);
            Assert.That(c.Battle.HerdSpiritFor(host.EntityId),Is.SameAs(state));
            Assert.That(host.Motion.Position,Is.EqualTo(source.Motion.Position));
            Assert.That(original.FrameState,Is.EqualTo("HerdFall"));Assert.That(original.Renderer.enabled,Is.True);
            Assert.That(spirit.FrameState,Is.EqualTo("HerdForming"));Assert.That(spirit.Renderer.enabled,Is.False);
            Assert.That(host.IsReviving,Is.True);Vector3 position=host.Motion.Position;int hits=host.OrdinaryAttacksFired;
            Assert.That(c.Battle.TryDamageUnit(host.EntityId,100000,CombatDamageType.Physical),Is.False);
            int fallingFrame=original.FrameIndex;AdvanceTo(c,state.StartedAt+.35);yield return null;
            Assert.That(original.FrameIndex,Is.GreaterThan(fallingFrame),"The old body must play real death frames.");
            AdvanceTo(c,state.StartedAt+.82);yield return null;
            Assert.That(original.Renderer.enabled,Is.False);Assert.That(original.FrameState,Is.EqualTo("HerdGone"));
            Assert.That(spirit.Renderer.enabled,Is.True);Assert.That(spirit.FrameState,Is.EqualTo("HerdRise"));
            Assert.That(spirit.Renderer.sharedMaterial.shader.name,Is.EqualTo("YesterdayConvoy/Herd Spirit"));
            var effect=Effect(host);Assert.That(effect.Opacity,Is.GreaterThan(0));Assert.That(effect.MistRenderer.enabled,Is.True);
            c.Battle.SetPaused(true);double clock=c.Battle.Clock;int frame=spirit.FrameIndex;float shaderTime=SpiritTime(spirit);
            var mist=MistVertices(effect);c.Tick(5);yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(spirit.FrameIndex,Is.EqualTo(frame));
            Assert.That(SpiritTime(spirit),Is.EqualTo(shaderTime));CollectionAssert.AreEqual(mist,MistVertices(effect));
            Assert.That(host.Motion.Position,Is.EqualTo(position));Assert.That(host.OrdinaryAttacksFired,Is.EqualTo(hits));
            c.Battle.SetPaused(false);AdvanceTo(c,state.StartedAt+1.05);yield return null;
            Assert.That(spirit.FrameIndex,Is.LessThan(frame),"Rising must progress from the fallen pose toward the standing pose.");
            Assert.That(host.Motion.Position,Is.EqualTo(position));Assert.That(host.OrdinaryAttacksFired,Is.EqualTo(hits));
            AdvanceTo(c,state.ReadyAt+.05);yield return null;
            Assert.That(host.IsReviving,Is.False);Assert.That(spirit.FrameState,Is.Not.EqualTo("HerdRise"));
            Assert.That(host.IsMoving||host.HasCombatTarget,Is.True);Assert.That(c.Battle.HerdRemaining(host.EntityId),Is.GreaterThan(11.8));
            c.LeaveShopTrial();yield return null;
        }

        [UnityTest]public IEnumerator SpiritShaderTransmitsBothLightAndDarkBackdropsAndKeepsBlueIdentity()
        {
            yield return Load();var c=Controller;Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();
            var source=Deploy(c,"CARD_M21",1);yield return null;
            Assert.That(c.Battle.TryDamageUnit(source.EntityId,100000,CombatDamageType.Physical),Is.True);c.Tick(.01f);yield return null;
            var state=c.Battle.HerdSpiritFrom(source.EntityId);Assert.That(state,Is.Not.Null);
            var host=c.Battle.FindUnit(state.EntityId);AdvanceTo(c,state.ReadyAt+.05);yield return null;c.Battle.SetPaused(true);
            var visual=Visual(host);var properties=new MaterialPropertyBlock();visual.Renderer.GetPropertyBlock(properties);
            var root=new GameObject("Isolated herd shader GPU fixture");var cameraObject=new GameObject("Isolated herd shader camera");
            var ordinary=new Material(Shader.Find("Sprites/Default"));
            try
            {
                root.layer=31;var sprite=root.AddComponent<SpriteRenderer>();sprite.sprite=visual.Renderer.sprite;sprite.SetPropertyBlock(properties);
                root.transform.position=new Vector3(1000,-sprite.sprite.bounds.center.y,0);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
                camera.orthographicSize=Mathf.Max(.5f,sprite.sprite.bounds.size.y*.6f);camera.aspect=1;camera.cullingMask=1<<31;
                camera.transform.position=new Vector3(1000,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.allowHDR=false;
                sprite.sharedMaterial=ordinary;var normalDark=Capture(camera,.12f);var normalLight=Capture(camera,.72f);
                sprite.sharedMaterial=visual.Renderer.sharedMaterial;var blueDark=Capture(camera,.12f);var blueLight=Capture(camera,.72f);
                int opaque=0,transmitted=0,blue=0;
                for(int i=0;i<normalDark.Length;i++)
                {
                    if(Difference(normalDark[i],normalLight[i])>6||Mathf.Abs(normalDark[i].r-31)+Mathf.Abs(normalDark[i].g-31)+Mathf.Abs(normalDark[i].b-31)<40)continue;
                    opaque++;if(Difference(blueDark[i],blueLight[i])>24)transmitted++;
                    if(blueDark[i].b>blueDark[i].r+8&&blueDark[i].g>blueDark[i].r+5)blue++;
                }
                Assert.That(opaque,Is.GreaterThan(150),"Select opaque original-body pixels rather than counting transparent background.");
                Assert.That(transmitted,Is.GreaterThan(opaque/8),"The new body must transmit the backdrop through formerly opaque pixels.");
                Assert.That(blue,Is.GreaterThan(opaque/5),"Blue must remain visible on the dark ground, not merely lower original alpha.");
                // Isolate natural expiry from enemy damage; the actual defender kill is covered above.
                foreach(var defender in c.Battle.Defenders)
                    typeof(DefenderState).GetField("NextAttackTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(defender,double.PositiveInfinity);
                c.Battle.SetPaused(false);AdvanceTo(c,state.ReadyAt+11.85);yield return null;
                Assert.That(host.Active,Is.True);var effect=Effect(host);float beforeExpiry=effect.Opacity;
                Assert.That(beforeExpiry,Is.GreaterThan(0));AdvanceTo(c,state.ReadyAt+12.03);yield return null;
                Assert.That(host.Active,Is.False);Assert.That(effect.Opacity,Is.LessThanOrEqualTo(beforeExpiry),"Expiry must never flash a dissolved spirit back to full opacity.");
                Assert.That(effect.Opacity,Is.Zero.Within(.001));
                var grounding=visual.GetComponent<ActorGrounding>();
                if(grounding!=null)Assert.That(grounding.Shadow.color.a,Is.Zero.Within(.001),"Dissolved ghosts cannot retain a solid contact shadow.");
            }
            finally{Object.Destroy(root);Object.Destroy(cameraObject);Object.Destroy(ordinary);c.LeaveShopTrial();}
            yield return null;
        }
        static int Difference(Color32 a,Color32 b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        static Color32[] Capture(Camera camera,float gray)
        {
            var target=new RenderTexture(256,256,24);var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try{camera.backgroundColor=new Color(gray,gray,gray);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();return texture.GetPixels32();}
            finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }

        [UnityTest]public IEnumerator IndependentConversionsDissolveOnceAndRestartClearsTheirVisualsAndLinks()
        {
            yield return Load();var c=Controller;Assert.That(c.StartLoopNextBattle(),Is.True);c.Battle.Begin();
            var a=Deploy(c,"CARD_M21",1);var b=Deploy(c,"CARD_M22",2);yield return null;
            c.Battle.TryDamageUnit(a.EntityId,100000,CombatDamageType.Physical);c.Battle.TryDamageUnit(b.EntityId,100000,CombatDamageType.Physical);c.Tick(.01f);yield return null;
            var first=c.Battle.HerdSpiritFrom(a.EntityId);var second=c.Battle.HerdSpiritFrom(b.EntityId);
            Assert.That(first,Is.Not.Null);Assert.That(second,Is.Not.Null);Assert.That(first.EntityId,Is.Not.EqualTo(second.EntityId));
            var host=c.Battle.FindUnit(first.EntityId);AdvanceTo(c,first.ReadyAt+.05);yield return null;
            Assert.That(c.Battle.HerdUsed,Is.EqualTo(2));var effect=Effect(host);float opacity=effect.Opacity;
            Assert.That(c.Battle.TryDamageUnit(host.EntityId,100000,CombatDamageType.Physical),Is.True);c.Tick(.2f);yield return null;
            Assert.That(effect.Opacity,Is.LessThan(opacity));Assert.That(c.Battle.HerdUsed,Is.EqualTo(2),"Spirit death cannot consume another conversion.");
            c.Battle.SetPaused(true);float pausedOpacity=effect.Opacity;c.Tick(3);yield return null;Assert.That(effect.Opacity,Is.EqualTo(pausedOpacity));
            c.Battle.SetPaused(false);c.Tick(1);yield return null;yield return null;
            Assert.That(GameObject.Find(host.EntityId),Is.Null,"A completed dissolve must release its actor.");
            typeof(ManualBattle).GetMethod("FinishForTesting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.Battle,new object[]{c.Battle.RunId,false});
            double clock=c.Battle.Clock;c.Tick(2);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));
            typeof(ManualBattleController).GetMethod("ReplaySandbox",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
            while(c.ArtLoading)yield return null;yield return null;
            Assert.That(c.Battle.Units,Is.Empty);Assert.That(c.ViewCount,Is.Zero);Assert.That(c.Battle.HerdSpiritFrom(a.EntityId),Is.Null);
            Assert.That(c.Battle.HerdSpiritFor(second.EntityId),Is.Null);Assert.That(Object.FindObjectsByType<HerdSpiritVisual>(FindObjectsSortMode.None),Is.Empty);
            c.LeaveShopTrial();yield return null;
        }
    }
}
