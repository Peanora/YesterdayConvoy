using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class GuerrillaOrderSceneTests
    {
        static readonly string[] Originals={"CARD_M15","CARD_M16","CARD_M03","CARD_M20","CARD_M19","CARD_M18","CARD_M17"};
        // Interior probes from the approved seven-panel composition, in top-left image coordinates.
        static readonly Vector2[] Probes={new Vector2(.18f,.23f),new Vector2(.19f,.55f),new Vector2(.17f,.85f),
            new Vector2(.50f,.35f),new Vector2(.86f,.23f),new Vector2(.80f,.60f),new Vector2(.85f,.82f)};
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float previousScale;
        long deploymentRequest;
        readonly List<Object> owned=new List<Object>();

        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
            foreach(var item in owned)if(item!=null)Object.Destroy(item);
            owned.Clear();
        }

        IEnumerator LoadFormation()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<1200;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.SynergySandbox&&!controller.ArtLoading,Is.True);
            Time.timeScale=0;
            Assert.That(controller.SetSandboxRoster(2),Is.True);
            for(int i=0;i<1200&&(controller.ArtLoading||controller.ShopPanelTransitioning);i++)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            Assert.That(controller.Shop.Bench.Any(p=>p.Id=="CARD_M20"),Is.True);
            var view=controller.GetComponent<GuerrillaOrderView>();
            Assert.That(view,Is.Not.Null);
            for(int i=0;i<1200&&!view.AssetsReady;i++)yield return null;
            Assert.That(view.AssetsReady,Is.True,"The real cut-in texture, shader and sound must load.");
        }

        void ClickPiece(string id)
        {
            int index=controller.Shop.Bench.ToList().FindIndex(p=>p.Id==id);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var point=controller.ShopPieceRect(index).center;
            controller.ProcessShopPointer(EventType.MouseDown,point);
            controller.ProcessShopPointer(EventType.MouseUp,point);
        }

        IEnumerator EarnAndUseOrder()
        {
            Assert.That(controller.StartLoopNextBattle(),Is.True);
            for(int i=0;i<1200&&(controller.ArtLoading||controller.ShopPanelTransitioning);i++)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            var battle=controller.Battle;battle.Begin();
            // Explicit durable/non-attacking runtime fixture: deployment, targeting, ordinary hits,
            // per-entity military-order credit and spending all remain real game operations.
            foreach(var defender in battle.Defenders)
            {
                typeof(DefenderState).GetProperty("Health").SetValue(defender,1000000f);
                typeof(DefenderState).GetProperty("MaxHealth").SetValue(defender,1000000f);
                typeof(DefenderState).GetField("NextAttackTime",PrivateInstance).SetValue(defender,1000000d);
            }
            var cards=controller.Shop.Bench.Select(p=>p.Id).Where(id=>id!="CARD_M20"&&battle.FindCard(id)!=null).ToArray();
            // Patriot remains in the formation but is intentionally never deployed.
            // A second entity of an ordinary card can provide its second legitimate contact point.
            for(int pass=0;pass<2&&battle.Orders<6;pass++)foreach(string card in cards)
            {
                bool deployed=false;
                for(int tick=0;tick<180&&battle.Phase==BattlePhase.Running;tick++)
                {
                    var result=battle.TryDeployRole(battle.RunId,++deploymentRequest,battle.Entry,card);
                    if(result.Success){deployed=true;break;}
                    controller.Tick(.05f);if(tick%10==0)yield return null;
                }
                Assert.That(deployed,Is.True,"Natural deployment failed: "+card);
                controller.Tick(.5f);yield return null;
            }
            bool used=false;
            for(int i=0;i<1600&&battle.Phase==BattlePhase.Running;i++)
            {
                if(battle.Orders>=6&&controller.UseCampaignOrder(false)){used=true;break;}
                controller.Tick(.05f);if(i%10==0)yield return null;
            }
            Assert.That(used,Is.True,"Ordinary combat must earn and successfully spend a full military order.");
            Assert.That(battle.OrderStrikes,Is.GreaterThan(0));
            Assert.That(battle.Units.Any(u=>u.CardId=="CARD_M20"),Is.False);
            Assert.That(battle.CombatShots.Any(s=>!s.FromDefender&&!s.SynergyOrder&&s.Applied),Is.True);
        }

        static Color32[] Blit(GuerrillaOrderView view)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Requires actual GPU rendering.");
            var material=view.PrepareMaterial();Assert.That(material,Is.Not.Null);Assert.That(material.mainTexture,Is.Not.Null);
            var target=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32);
            var image=new Texture2D(640,360,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;
            try
            {
                target.Create();RenderTexture.active=target;GL.Clear(false,true,Color.clear);
                Graphics.Blit(material.mainTexture,target,material);
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();
            }
            finally{RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
        static int Difference(Color32[] a,Color32[] b,Vector2 topLeft,float radius=.018f)
        {
            int result=0;
            for(int y=Mathf.Max(0,(int)((1-topLeft.y-radius)*360));y<Mathf.Min(360,(int)((1-topLeft.y+radius)*360));y++)
                for(int x=Mathf.Max(0,(int)((topLeft.x-radius)*640));x<Mathf.Min(640,(int)((topLeft.x+radius)*640));x++)
                {
                    int i=y*640+x;
                    if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a)>24)result++;
                }
            return result;
        }
        static int MaximumBrightness(Color32[] pixels,Vector2 topLeft)
        {
            int result=0;
            for(int y=(int)((1-topLeft.y-.018f)*360);y<(int)((1-topLeft.y+.018f)*360);y++)
                for(int x=(int)((topLeft.x-.018f)*640);x<(int)((topLeft.x+.018f)*640);x++)
                {var color=pixels[y*640+x];result=Math.Max(result,Math.Max(color.r,Math.Max(color.g,color.b)));}
            return result;
        }

        [UnityTest]public IEnumerator RealOrderUsesFormationSnapshotAndSharesPauseSpeedFadeAndRestart()
        {
            yield return LoadFormation();var view=controller.GetComponent<GuerrillaOrderView>();
            Assert.That(controller.UseCampaignOrder(false),Is.False);Assert.That(controller.UseCampaignOrder(true),Is.False);
            Assert.That(view.PlayCount,Is.Zero);
            yield return EarnAndUseOrder();
            Assert.That(view.PlayCount,Is.EqualTo(1));Assert.That(view.Playing,Is.True);
            CollectionAssert.AreEqual(Originals,view.SlotMembers.Take(7));
            Assert.That(view.SlotMembers.Skip(7).All(string.IsNullOrEmpty),Is.True);
            Assert.That(view.ScreenBounds.width,Is.GreaterThan(Screen.width*.5f).And.LessThanOrEqualTo(Screen.width+.1f));
            Assert.That(view.ScreenBounds.height,Is.GreaterThan(Screen.height*.5f).And.LessThanOrEqualTo(Screen.height+.1f));
            Assert.That(view.ScreenBounds.width/view.ScreenBounds.height,Is.EqualTo(16f/9).Within(.001f));
            double duration=view.PendingUntil-controller.PresentationClock;
            Assert.That(duration,Is.GreaterThan(.3).And.LessThan(2));
            controller.Tick((float)duration*.05f);yield return null;
            Assert.That(view.Opacity,Is.GreaterThan(0).And.LessThan(1));
            controller.Battle.SetPaused(true);controller.Tick(0);yield return null;
            double clock=controller.Battle.Clock;var age=view.Age;float opacity=view.Opacity;
            Assert.That(view.SoundPaused,Is.True);
            var sound=controller.GetComponents<AudioSource>().Single(s=>s.clip!=null&&s.clip.name=="shing");
            int sample=sound.timeSamples;
            controller.Tick(3);yield return new WaitForSecondsRealtime(.05f);
            Assert.That(controller.Battle.Clock,Is.EqualTo(clock));Assert.That(view.Age,Is.EqualTo(age));Assert.That(view.Opacity,Is.EqualTo(opacity));
            Assert.That(sound.timeSamples,Is.EqualTo(sample),"The actual AudioSource must pause with the cut-in.");
            Assert.That(controller.UseCampaignOrder(true),Is.False);Assert.That(view.PlayCount,Is.EqualTo(1));
            controller.Battle.SetPaused(false);controller.ToggleSpeed();controller.Tick(.025f);yield return null;
            Assert.That(view.Age-age,Is.EqualTo(.05).Within(.0001));Assert.That(view.SoundPaused,Is.False);
            controller.ToggleSpeed();
            float peak=view.Opacity;bool faded=false;
            for(int i=0;i<40&&view.Playing;i++)
            {
                controller.Tick((float)duration/30);peak=Mathf.Max(peak,view.Opacity);
                if(view.Opacity>0&&peak>.9f&&view.Opacity<peak-.1f)faded=true;
                yield return null;
            }
            Assert.That(peak,Is.GreaterThan(.9f));Assert.That(faded,Is.True);Assert.That(view.Playing,Is.False);Assert.That(view.Opacity,Is.Zero);
            controller.RestartBattle();for(int i=0;i<1200&&controller.ArtLoading;i++)yield return null;yield return null;
            Assert.That(controller.Battle.Orders,Is.Zero);Assert.That(view.Playing,Is.False);Assert.That(view.SoundPlaying,Is.False);
        }

        [UnityTest]public IEnumerator NormalFormationRemovalLeavesItsFixedSlotAndGpuHidesEveryAbsentPortrait()
        {
            yield return LoadFormation();ClickPiece("CARD_M15");
            Assert.That(controller.Battle.FindCard("CARD_M15"),Is.Null,"Use the normal preparation click to move the card to reserve.");
            Assert.That(controller.Shop.Bench.Any(p=>p.Id=="CARD_M15"),Is.True,"Removing a formation member cannot sell the card.");
            yield return EarnAndUseOrder();var view=controller.GetComponent<GuerrillaOrderView>();
            Assert.That(view.SlotMembers[0],Is.Null);CollectionAssert.AreEqual(Originals.Skip(1),view.SlotMembers.Skip(1).Take(6));
            controller.Tick(.2f);var actualMissing=Blit(view);
            view.Play(Originals);controller.Tick(.2f);var full=Blit(view);
            Assert.That(Difference(full,actualMissing,Probes[0]),Is.GreaterThan(20),"The removed warrior must disappear from real output pixels.");
            for(int missing=0;missing<7;missing++)
            {
                view.Play(Originals.Where((id,i)=>i!=missing));controller.Tick(.2f);var masked=Blit(view);
                Assert.That(Difference(full,masked,Probes[missing]),Is.GreaterThan(20),"Absent baked portrait must be masked: "+Originals[missing]);
                Assert.That(MaximumBrightness(masked,Probes[missing]),Is.LessThan(80),"An absent portrait's interior must contain only the dark empty panel, with no retained face detail.");
                for(int other=0;other<7;other++)if(other!=missing)
                    Assert.That(Difference(full,masked,Probes[other]),Is.Zero,"A gap cannot move or alter another member's portrait.");
            }
            yield return null;
        }

        Sprite Solid(Color color)
        {
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(color,1024).ToArray());texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f);owned.Add(texture);owned.Add(sprite);return sprite;
        }

        [UnityTest]public IEnumerator FutureMembersUseOnlyTwoExtensionsAndDoNotDisplaceOriginals()
        {
            yield return LoadFormation();Assert.That(controller.StartLoopNextBattle(),Is.True);
            while(controller.ArtLoading||controller.ShopPanelTransitioning)yield return null;controller.Battle.Begin();
            var view=controller.GetComponent<GuerrillaOrderView>();
            var extras=new Dictionary<string,Sprite>{{"FUTURE_A",Solid(Color.magenta)},{"FUTURE_B",Solid(Color.cyan)},{"FUTURE_C",Solid(Color.green)}};
            view.Play(Originals);controller.Tick(.2f);var baseline=Blit(view);
            view.Play(Originals.Concat(extras.Keys),extras);controller.Tick(.2f);
            Assert.That(view.SlotMembers.Count,Is.EqualTo(9));CollectionAssert.AreEqual(Originals,view.SlotMembers.Take(7));
            CollectionAssert.AreEqual(new[]{"FUTURE_A","FUTURE_B"},view.SlotMembers.Skip(7));
            var extended=Blit(view);
            Assert.That(Difference(baseline,extended,new Vector2(.025f,.5f)),Is.GreaterThan(20));
            Assert.That(Difference(baseline,extended,new Vector2(.975f,.5f)),Is.GreaterThan(20));
            foreach(var probe in Probes)Assert.That(Difference(baseline,extended,probe),Is.Zero,"Extensions cannot displace original members.");
            yield return null;
        }
    }
}
