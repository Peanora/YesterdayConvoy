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
        // Portrait interiors in the approved V2 radial composition, in top-left coordinates.
        static readonly Vector2[] Probes={new Vector2(.325f,.298f),new Vector2(.235f,.435f),new Vector2(.220f,.72f),
            new Vector2(.50f,.24f),new Vector2(.70f,.27f),new Vector2(.815f,.445f),new Vector2(.755f,.68f)};
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

        IEnumerator LoadFormation(string sceneName="SynergyTrial")
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
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

        IEnumerator StartRunning()
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
        }

        IEnumerator EarnAutomaticOrder(bool isolateOrdinaryDamage=false)
        {
            // Strict HP/terminal cases use the real eight-member preparation path. Starting
            // two points plus one M03 contact are enough, without creating a multi-source
            // backlog of long-windup attacks that could outlive the first pursuit cue.
            if(isolateOrdinaryDamage)Assert.That(controller.SetGuerrillaTransfer("CARD_M08",true),Is.True);
            yield return StartRunning();var battle=controller.Battle;
            if(isolateOrdinaryDamage)
            {
                Assert.That(battle.GuerrillaTier,Is.EqualTo(8));Assert.That(battle.Orders,Is.EqualTo(2));
                // Delay independent devices before contact, so there are no fresh healing
                // actions competing with the first ordinary hit or later strike windows.
                foreach(var device in battle.Gramophones)
                {
                    typeof(GramophoneState).GetField("NextAttack",PrivateInstance).SetValue(device,battle.Clock+1000);
                    typeof(GramophoneState).GetField("NextHeal",PrivateInstance).SetValue(device,battle.Clock+1000);
                }
            }
            var cards=isolateOrdinaryDamage?new[]{"CARD_M03"}:controller.Shop.Bench.Select(p=>p.Id).Where(id=>id!="CARD_M20"&&battle.FindCard(id)!=null).ToArray();
            // Inventory and durable guards are explicit fixtures. Deployment, ordinary contact,
            // per-entity income and the automatic full-order transaction remain production paths.
            foreach(string card in cards)
            {
                Assert.That(battle.TryDeployRole(battle.RunId,++deploymentRequest,battle.Entry,card).Success,Is.True,"Real deployment: "+card);
            }
            for(int i=0;i<2400&&battle.LatestGuerrillaOrder==null&&battle.Phase==BattlePhase.Running;i++)
            {controller.Tick(.02f);if(i%10==0)yield return null;}
            Assert.That(battle.LatestGuerrillaOrder,Is.Not.Null,"Real ordinary contact must automatically spend the full order.");
            Assert.That(battle.OrdersUsed,Is.EqualTo(1));
            if(isolateOrdinaryDamage)
            {
                Assert.That(battle.Units.Count,Is.EqualTo(1));Assert.That(battle.Orders,Is.Zero);
                foreach(var unit in battle.Units)typeof(DeployedUnitState).GetField("NextAttackTime",PrivateInstance).SetValue(unit,battle.Clock+1000);
                // Freeze only independent device actions for the strict five-window HP fixture;
                // existing projectiles/heals drain naturally. No order, HP loss or hit is invented.
                foreach(var device in battle.Gramophones)
                {
                    typeof(GramophoneState).GetField("NextAttack",PrivateInstance).SetValue(device,battle.Clock+1000);
                    typeof(GramophoneState).GetField("NextHeal",PrivateInstance).SetValue(device,battle.Clock+1000);
                }
                for(int i=0;i<80&&(battle.CombatShots.Any(shot=>!shot.Resolved)||battle.GramophoneActions.Any(action=>!action.Resolved));i++)
                {controller.Tick(.01f);if(i%10==0)yield return null;}
                Assert.That(battle.CombatShots.All(shot=>shot.Resolved)&&battle.GramophoneActions.All(action=>action.Resolved),Is.True);
                Assert.That(battle.LatestGuerrillaOrder.Age,Is.LessThan(.85f),"The measurement fixture must finish before the first pursuit strike.");
            }
            Assert.That(battle.OrderStrikes,Is.GreaterThan(0));
            Assert.That(battle.Units.Any(u=>u.CardId=="CARD_M20"),Is.False);
            Assert.That(battle.CombatShots.Any(shot=>!shot.FromDefender&&!shot.SynergyOrder&&shot.Applied),Is.True);
        }

        static Color32[] Capture(GuerrillaOrderView view)
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Requires actual GPU rendering.");
            var target=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32);
            var image=new Texture2D(640,360,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;
            try
            {
                target.Create();RenderTexture.active=target;GL.Clear(false,true,Color.clear);
                view.RenderTo(target);
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();return image.GetPixels32();
            }
            finally{RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
        static int ChangedPixels(Color32[] a,Color32[] b)
        {
            int changed=0;
            for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a)>24)changed++;
            return changed;
        }
        static bool Cleared(Color32[] pixels)=>pixels.All(p=>p.a==0);
        void AdvanceTo(GuerrillaOrderView view,float age)
        {
            float delta=age-view.Age;Assert.That(delta,Is.GreaterThanOrEqualTo(-.0001f));
            controller.Tick(Mathf.Max(0,delta)/Mathf.Min(1,controller.EffectiveSimulationSpeed));
        }
        static void PresentationTo(GuerrillaOrderView view,float age)
        {
            float delta=age-view.Age;Assert.That(delta,Is.GreaterThanOrEqualTo(-.0001f));
            view.AdvancePresentation(Mathf.Max(0,delta));
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

        [UnityTest]public IEnumerator RealCommittedOrderStartsOnceFromFormationAndFailuresStayQuiet()
        {
            yield return LoadFormation();var view=controller.GetComponent<GuerrillaOrderView>();
            Assert.That(controller.UseCampaignOrder(false),Is.False);Assert.That(controller.UseCampaignOrder(true),Is.False);
            Assert.That(view.PlayCount,Is.Zero);
            yield return EarnAutomaticOrder();
            Assert.That(view.PlayCount,Is.EqualTo(1));Assert.That(view.Playing,Is.True);
            CollectionAssert.AreEqual(Originals,view.SlotMembers.Take(7));
            Assert.That(view.SlotMembers.Skip(7).All(string.IsNullOrEmpty),Is.True);
            Assert.That(view.ScreenBounds.width,Is.GreaterThan(Screen.width*.5f).And.LessThanOrEqualTo(Screen.width+.1f));
            Assert.That(view.ScreenBounds.height,Is.GreaterThan(Screen.height*.5f).And.LessThanOrEqualTo(Screen.height+.1f));
            Assert.That(view.ScreenBounds.width/view.ScreenBounds.height,Is.EqualTo(16f/9).Within(.001f));
            Assert.That(view.Remaining,Is.EqualTo(GuerrillaOrderVolley.PresentationDuration-view.Age).Within(.0001));
            var battle=controller.Battle;
            int strikes=battle.OrderStrikes,orders=battle.Orders,dp=battle.DP;
            Assert.That(controller.UseCampaignOrder(false),Is.False,"An automatic wave owns its complete cinematic tail; another manual transaction cannot replace it.");
            controller.Tick(0);Capture(view);Capture(view);
            Assert.That(view.PlayCount,Is.EqualTo(1));Assert.That(battle.OrderStrikes,Is.EqualTo(strikes));Assert.That(battle.Orders,Is.EqualTo(orders));Assert.That(battle.DP,Is.EqualTo(dp));
            battle.SetPaused(true);Assert.That(controller.UseCampaignOrder(false),Is.False);Assert.That(controller.UseCampaignOrder(true),Is.False);Assert.That(view.PlayCount,Is.EqualTo(1));
            Assert.That(battle.Units.Any(u=>u.CardId=="CARD_M20"),Is.False,"Patriot is a visible formation member, not an invented order recipient.");
            Assert.That(battle.LatestGuerrillaOrder,Is.Not.Null);
            Assert.That(battle.LatestGuerrillaOrder.SourceIds.All(id=>battle.FindUnit(id)?.CardId!="CARD_M20"),Is.True);
            Assert.That(battle.CombatShots.Any(s=>s.SynergyOrder),Is.False,"The committed volley replaces the old per-target order shots.");
        }

        [UnityTest]public IEnumerator GeometryConvergesAndAudioIgnoresBattleDoubleSpeedWhilePauseAndSkillSlowMotionRemain()
        {
            yield return LoadFormation();
            // A normal FrostNova deployment provides the real skill-menu slow-motion control.
            // Direct Play calls below exercise presentation boundaries independently of faction eligibility.
            Assert.That(controller.SetSandboxRoster(0),Is.True);yield return StartRunning();
            var battle=controller.Battle;var receipt=battle.TryDeployRole(battle.RunId,++deploymentRequest,battle.Entry,"CARD_M14");Assert.That(receipt.Success,Is.True);
            var view=controller.GetComponent<GuerrillaOrderView>();Assert.That(Cleared(Capture(view)),Is.True);view.Play(Originals);
            Assert.That(view.SoundPlayCount,Is.Zero);
            PresentationTo(view,.20f);var arriving=Capture(view);var arrivalVertices=view.GeometryVertices.ToArray();var arrivalOffsets=view.PaneOffsets.ToArray();
            PresentationTo(view,.65f);var gathered=Capture(view);var gatheredVertices=view.GeometryVertices.ToArray();
            Assert.That(view.VisiblePaneCount,Is.EqualTo(9));Assert.That(view.VisiblePortraitCount,Is.EqualTo(7));Assert.That(view.VisibleFragmentCount,Is.Zero);
            var previousModelView=GL.modelview;
            try
            {
                GL.modelview=Matrix4x4.TRS(new Vector3(640,900,-20),Quaternion.Euler(20,0,15),new Vector3(2,-3,1));
                var inheritedModelView=GL.modelview;
                CollectionAssert.AreEqual(gathered,Capture(view),"Screen-space composition must ignore inherited IMGUI/camera transforms.");
                Assert.That(GL.modelview,Is.EqualTo(inheritedModelView),"RenderTo must restore its caller's model-view state.");
            }
            finally{GL.modelview=previousModelView;}
            var clipCommands=new UnityEngine.Rendering.CommandBuffer();
            try
            {
                clipCommands.EnableScissorRect(new Rect(0,0,640,22));Graphics.ExecuteCommandBuffer(clipCommands);
                CollectionAssert.AreEqual(gathered,Capture(view),"A previous GUI clip must not crop the effect to a top strip.");
            }
            finally{clipCommands.Clear();clipCommands.DisableScissorRect();Graphics.ExecuteCommandBuffer(clipCommands);clipCommands.Release();}
            Assert.That(ChangedPixels(arriving,gathered),Is.GreaterThan(1000));Assert.That(arrivalVertices.SequenceEqual(gatheredVertices),Is.False,"Opaque portrait geometry must move toward the centre.");
            Assert.That(arrivalOffsets.Take(7).Any(p=>p.sqrMagnitude>100),Is.True);Assert.That(view.PaneOffsets.All(p=>p.sqrMagnitude<.01f),Is.True,"Every pane must arrive at its fixed radial slot.");
            PresentationTo(view,.85f);Capture(view);Assert.That(view.SoundPlayCount,Is.Zero,"The approved original sound belongs to breakup, not entrance.");
            PresentationTo(view,1.10f);var fractured=Capture(view);yield return null;
            Assert.That(view.VisibleFragmentCount,Is.GreaterThan(30));
            Assert.That(gatheredVertices.SequenceEqual(view.GeometryVertices),Is.False,"Actual textured fragments still move as the battle is revealed.");
            Assert.That(ChangedPixels(gathered,fractured),Is.GreaterThan(1000),"Breakup must alter the actual opaque geometry, not only the whole-frame alpha.");
            Assert.That(view.SoundPlayCount,Is.EqualTo(1));
            var originalSound=Resources.Load<AudioClip>("GuerrillaOrder/followup-original");Assert.That(originalSound,Is.Not.Null);
            var sound=controller.GetComponents<AudioSource>().Single(s=>s.clip==originalSound);
            Assert.That(sound.loop,Is.False);Capture(view);Capture(view);Assert.That(view.SoundPlayCount,Is.EqualTo(1));

            battle.SetPaused(true);Capture(view);yield return null;
            var pausedVertices=view.GeometryVertices.ToArray();var pausedImage=Capture(view);int sample=sound.timeSamples;float age=view.Age;
            double clock=battle.Clock;Assert.That(view.SoundPaused,Is.True);
            view.AdvancePresentation(3);yield return new WaitForSecondsRealtime(.05f);
            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(view.Age,Is.EqualTo(age));
            CollectionAssert.AreEqual(pausedVertices,view.GeometryVertices);CollectionAssert.AreEqual(pausedImage,Capture(view));
            Assert.That(sound.timeSamples,Is.EqualTo(sample),"The actual original AudioSource must freeze with the vertices.");
            battle.SetPaused(false);controller.ToggleSpeed();view.AdvancePresentation(.05f);Capture(view);yield return null;
            Assert.That(view.Age-age,Is.EqualTo(.05).Within(.0001));Assert.That(sound.pitch,Is.EqualTo(1).Within(.001f));
            Assert.That(view.SoundPaused,Is.False);Assert.That(view.SoundPlayCount,Is.EqualTo(1));
            controller.ToggleSpeed();controller.OpenSkillMenu(receipt.EntityId);Assert.That(controller.SkillMenuOpen,Is.True);
            age=view.Age;view.AdvancePresentation(.03f);Capture(view);yield return null;
            Assert.That(view.Age-age,Is.EqualTo(.03).Within(.0001));Assert.That(sound.pitch,Is.EqualTo(.03f).Within(.001f));controller.CloseSkillMenu();
            PresentationTo(view,1.70f);var drifting=Capture(view);Assert.That(ChangedPixels(fractured,drifting),Is.GreaterThan(1000));
            PresentationTo(view,2.20f);Capture(view);Assert.That(view.VisiblePortraitCount,Is.Zero);Assert.That(view.VisibleFragmentCount,Is.Zero);Assert.That(view.Playing,Is.True,"The portraits are gone while the five-strike cinematic continues.");
            PresentationTo(view,GuerrillaOrderVolley.PresentationDuration+.01f);Assert.That(Cleared(Capture(view)),Is.True);Assert.That(view.Playing,Is.False);Assert.That(view.SoundPlayCount,Is.EqualTo(1));
        }

        [UnityTest]public IEnumerator NormalFormationRemovalAndFutureMembersKeepTheirFixedRadialSlotsInActualGpuOutput()
        {
            yield return LoadFormation();ClickPiece("CARD_M15");
            Assert.That(controller.Battle.FindCard("CARD_M15"),Is.Null,"Use the normal preparation click to move the card to reserve.");
            Assert.That(controller.Shop.Bench.Any(p=>p.Id=="CARD_M15"),Is.True,"Removing a formation member cannot sell the card.");
            yield return EarnAutomaticOrder();var view=controller.GetComponent<GuerrillaOrderView>();
            Assert.That(view.SlotMembers[0],Is.Null);CollectionAssert.AreEqual(Originals.Skip(1),view.SlotMembers.Skip(1).Take(6));
            AdvanceTo(view,.65f);var actualMissing=Capture(view);
            view.Play(Originals);PresentationTo(view,.65f);var full=Capture(view);
            if(Difference(full,actualMissing,Probes[0])<=20)
            {
                var failure=new Texture2D(640,360,TextureFormat.RGBA32,false);failure.SetPixels32(full);failure.Apply();
                System.IO.Directory.CreateDirectory("artifacts");System.IO.File.WriteAllBytes("artifacts/guerrilla-render-failure.png",failure.EncodeToPNG());Object.Destroy(failure);
            }
            Assert.That(Difference(full,actualMissing,Probes[0]),Is.GreaterThan(20),"The removed warrior must disappear from real output pixels.");
            for(int missing=0;missing<7;missing++)
            {
                view.Play(Originals.Where((id,i)=>i!=missing));PresentationTo(view,.65f);var masked=Capture(view);
                Assert.That(Difference(full,masked,Probes[missing]),Is.GreaterThan(20),"Absent baked portrait must be masked: "+Originals[missing]);
                Assert.That(MaximumBrightness(masked,Probes[missing]),Is.LessThan(100),"An absent portrait's interior must contain only the dark empty panel, with no retained face detail.");
                for(int other=0;other<7;other++)if(other!=missing)
                    Assert.That(Difference(full,masked,Probes[other]),Is.Zero,"A gap cannot move or alter another member's portrait.");
            }
            var extras=new Dictionary<string,Sprite>{{"FUTURE_A",Solid(Color.magenta)},{"FUTURE_B",Solid(Color.cyan)},{"FUTURE_C",Solid(Color.green)}};
            view.Play(Originals.Concat(new[]{"FUTURE_C","FUTURE_B","FUTURE_A","CARD_M15"}),extras);PresentationTo(view,.65f);
            Assert.That(view.SlotMembers.Count,Is.EqualTo(9));CollectionAssert.AreEqual(Originals,view.SlotMembers.Take(7));
            CollectionAssert.AreEqual(new[]{"FUTURE_A","FUTURE_B"},view.SlotMembers.Skip(7));
            var extended=Capture(view);
            Assert.That(Difference(full,extended,new Vector2(.61f,.77f)),Is.GreaterThan(20));
            Assert.That(Difference(full,extended,new Vector2(.39f,.77f)),Is.GreaterThan(20));
            foreach(var probe in Probes)Assert.That(Difference(full,extended,probe),Is.Zero,"Extensions cannot displace original members.");
            yield return null;
        }

        [UnityTest]public IEnumerator OneAndTwoBattleSpeedHaveIdenticalElapsedTimeGeometryPixelsAndOriginalSoundRate()
        {
            yield return LoadFormation();yield return StartRunning();
            var view=controller.GetComponent<GuerrillaOrderView>();
            view.Play(Originals);PresentationTo(view,.65f);var oneSpeed=Capture(view);var oneVertices=view.GeometryVertices.ToArray();
            controller.ToggleSpeed();Assert.That(controller.EffectiveSimulationSpeed,Is.EqualTo(2));
            view.Play(Originals);PresentationTo(view,.65f);
            CollectionAssert.AreEqual(oneSpeed,Capture(view));CollectionAssert.AreEqual(oneVertices,view.GeometryVertices);
            Assert.That(view.Remaining,Is.EqualTo(GuerrillaOrderVolley.PresentationDuration-.65f).Within(.0001));
            PresentationTo(view,1.10f);Capture(view);yield return null;
            foreach(var source in controller.GetComponents<AudioSource>().Where(source=>source.clip==Resources.Load<AudioClip>("GuerrillaOrder/followup-original")||source.clip==Resources.Load<AudioClip>("GuerrillaOrder/five-slash-charge")))
                Assert.That(source.pitch,Is.EqualTo(1).Within(.001));
            Assert.That(view.Playing,Is.True,"Two-times battle speed cannot finish the cinematic early.");
            PresentationTo(view,GuerrillaOrderVolley.PresentationDuration+.01f);Capture(view);
            Assert.That(view.Playing,Is.False);Assert.That(Cleared(Capture(view)),Is.True);Assert.That(view.SoundPlaying||view.SlashSoundPlaying,Is.False);
        }

        [UnityTest]public IEnumerator RealVolleyWaitsForThePresentationImpactThenDamagesEveryAliveGuardAndRendersThoseExactHits()
        {
            yield return LoadFormation("GuerrillaTrial");yield return EarnAutomaticOrder(true);
            var battle=controller.Battle;var view=controller.GetComponent<GuerrillaOrderView>();var volley=battle.LatestGuerrillaOrder;
            Assert.That(volley.Resolved||volley.Cancelled,Is.False);Assert.That(volley.CompletedStrikes,Is.Zero);
            var guards=battle.Defenders.Where(guard=>guard.Alive).ToArray();Assert.That(guards.Length,Is.GreaterThanOrEqualTo(3));
            var participants=volley.SourceIds.Select(battle.FindUnit).ToArray();var distant=guards.Last();
            var farPoints=new[]{new Vector3(6,.04f,-3),new Vector3(-6,.04f,-3),new Vector3(6,.04f,3),new Vector3(-6,.04f,3)};
            var far=farPoints.OrderByDescending(point=>participants.Min(unit=>GroundDistance(point,unit.Motion.Position)-battle.UnitCombat(unit).UnitRange)).First();
            typeof(DefenderState).GetProperty("Position").SetValue(distant,far);
            Assert.That(participants.All(unit=>GroundDistance(distant.Position,unit.Motion.Position)>battle.UnitCombat(unit).UnitRange+.1f),Is.True);
            var initial=guards.ToDictionary(guard=>guard.EntityId,guard=>guard.Health);
            AdvanceTo(view,.85f);Capture(view);Assert.That(volley.Hits,Is.Empty);
            foreach(var guard in guards)Assert.That(guard.Health,Is.EqualTo(initial[guard.EntityId]));
            var departed=guards.First(guard=>guard!=distant);typeof(DefenderState).GetProperty("Health").SetValue(departed,0f);
            guards=guards.Where(guard=>guard.Alive).ToArray();
            battle.SetPaused(true);controller.Tick(2);Capture(view);Assert.That(volley.Age,Is.EqualTo(.85f).Within(.0001));battle.SetPaused(false);
            controller.ToggleSpeed();double clock=battle.Clock;controller.Tick(.04f);
            Assert.That(battle.Clock-clock,Is.EqualTo(.08).Within(.0001));Assert.That(view.Age,Is.EqualTo(.89f).Within(.0001));
            float firstDamage=0;
            for(int index=0;index<GuerrillaOrderVolley.StrikeCount;index++)
            {
                if(index==4)
                {
                    AdvanceTo(view,2.4f);var charge=Capture(view);float chargeTotal=volley.Hits.Sum(hit=>hit.Damage);
                    Assert.That(volley.CompletedStrikes,Is.EqualTo(4));Assert.That(volley.Resolved,Is.False);
                    Assert.That(view.DisplayedStrikeCount,Is.EqualTo(4));Assert.That(view.ChargeProgress,Is.GreaterThan(0));
                    Assert.That(charge.Any(pixel=>pixel.a>0),Is.True,"Charging must contribute real full-screen pixels.");
                    battle.SetPaused(true);controller.Tick(1);CollectionAssert.AreEqual(charge,Capture(view));
                    Assert.That(volley.Hits.Sum(hit=>hit.Damage),Is.EqualTo(chargeTotal));battle.SetPaused(false);
                }
                AdvanceTo(view,GuerrillaOrderVolley.StrikeTime(index)-.01f);
                Assert.That(volley.CompletedStrikes,Is.EqualTo(index));
                var before=guards.ToDictionary(guard=>guard.EntityId,guard=>guard.Health);
                AdvanceTo(view,GuerrillaOrderVolley.StrikeTime(index)+.01f);var impact=Capture(view);
                Assert.That(volley.CompletedStrikes,Is.EqualTo(index+1));var strike=volley.Strikes[index];
                Assert.That(strike.Index,Is.EqualTo(index));Assert.That(strike.At,Is.EqualTo(GuerrillaOrderVolley.StrikeTime(index)));
                Assert.That(strike.Weight,Is.EqualTo(index==4?.4f:.15f));
                CollectionAssert.AreEquivalent(guards.Select(guard=>guard.EntityId),strike.Hits.Select(hit=>hit.TargetId));
                foreach(var hit in strike.Hits)
                {
                    Assert.That(hit.Damage,Is.GreaterThan(0));
                    Assert.That(before[hit.TargetId]-battle.FindDefender(hit.TargetId).Health,Is.EqualTo(hit.Damage).Within(.01),"Each strike records actual HP loss: "+hit.TargetId);
                    Assert.That((hit.WorldPosition-battle.FindDefender(hit.TargetId).Position).sqrMagnitude,Is.LessThan(.00001f));
                }
                Assert.That(strike.Hits.Any(hit=>hit.TargetId==departed.EntityId),Is.False);
                Assert.That(view.DisplayedStrikeCount,Is.EqualTo(index+1));Assert.That(view.VisibleImpactTargetCount,Is.EqualTo(guards.Length));
                Assert.That(view.DisplayedDamage,Is.EqualTo(volley.Hits.Sum(hit=>hit.Damage)).Within(.01));
                view.BindVolley(null);var withoutHits=Capture(view);Assert.That(ChangedPixels(impact,withoutHits),Is.GreaterThan(100));
                view.BindVolley(volley);CollectionAssert.AreEqual(impact,Capture(view));
                foreach(var hit in strike.Hits)Assert.That(before[hit.TargetId]-battle.FindDefender(hit.TargetId).Health,Is.EqualTo(hit.Damage).Within(.01),"Rendering cannot deal damage.");
                float amount=strike.Hits.Sum(hit=>hit.Damage);if(index==0)firstDamage=amount;
                if(index==4)Assert.That(amount,Is.GreaterThan(firstDamage*2.5f),"The40% finish must exceed the15% opening cut.");
                Assert.That(volley.Resolved,Is.EqualTo(index==4));
            }
            Assert.That(volley.Cancelled,Is.False);Assert.That(battle.CombatShots.Any(shot=>shot.SynergyOrder),Is.False);
            AdvanceTo(view,2.82f);Capture(view);Assert.That(view.ScreenEffect.Holding,Is.True);AssertBattlefieldGpu(view,true);
            AdvanceTo(view,2.91f);Capture(view);Assert.That(view.ScreenEffect.Holding,Is.False);
            Assert.That(view.ScreenEffect.Offset.sqrMagnitude,Is.GreaterThan(.01f));AssertBattlefieldGpu(view,false);
            var finalHealth=guards.Select(guard=>guard.Health).ToArray();
            AdvanceTo(view,GuerrillaOrderVolley.PresentationDuration+.01f);Capture(view);
            CollectionAssert.AreEqual(finalHealth,guards.Select(guard=>guard.Health));Assert.That(view.Playing,Is.False);
            Assert.That(view.ScreenEffect.Offset,Is.EqualTo(Vector2.zero));Assert.That(view.ScreenEffect.Zoom,Is.EqualTo(1));Assert.That(view.ScreenEffect.Roll,Is.Zero);
            Assert.That(view.SoundPlaying||view.SlashSoundPlaying,Is.False);
        }
        static Color32[] ReadTarget(RenderTexture target)
        {
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=previous;Object.Destroy(image);}
        }
        static void AssertBattlefieldGpu(GuerrillaOrderView view,bool holding)
        {
            var camera=Camera.main;var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var source=new RenderTexture(640,360,24,RenderTextureFormat.ARGB32);
            var first=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32);var second=new RenderTexture(640,360,0,RenderTextureFormat.ARGB32);
            try
            {
                source.Create();first.Create();second.Create();camera.targetTexture=source;camera.Render();
                var input=ReadTarget(source);view.ScreenEffect.ApplyTo(source,first);var applied=ReadTarget(first);
                if(holding)
                {
                    // Replace only this test's input RT; the held output must retain the actual
                    // battlefield image, proving the hold is implemented in the GPU path.
                    Graphics.Blit(Texture2D.whiteTexture,source);view.ScreenEffect.ApplyTo(source,second);
                    CollectionAssert.AreEqual(applied,ReadTarget(second));Assert.That(ChangedPixels(applied,ReadTarget(source)),Is.GreaterThan(1000));
                }
                else Assert.That(ChangedPixels(input,applied),Is.GreaterThan(100),"The real battlefield pixels must be transformed by the recoil shader.");
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                foreach(var target in new[]{source,first,second}){target.Release();Object.Destroy(target);}
            }
        }

        static float GroundDistance(Vector3 a,Vector3 b)
        {Vector3 delta=a-b;delta.y=0;return delta.magnitude;}

        [UnityTest]public IEnumerator TerminalBeforeImpactCancelsActualVolleyButLetsTheIndependentVisualTailFinishWithoutFakeDamage()
        {
            yield return LoadFormation();yield return EarnAutomaticOrder(true);
            var battle=controller.Battle;var view=controller.GetComponent<GuerrillaOrderView>();var volley=battle.LatestGuerrillaOrder;
            AdvanceTo(view,.85f);var health=battle.Defenders.ToDictionary(d=>d.EntityId,d=>d.Health);double clock=battle.Clock;
            Assert.That(volley.Resolved||volley.Cancelled,Is.False);
            var finish=typeof(ManualBattle).GetMethod("FinishForTesting",PrivateInstance);
            Assert.That((bool)finish.Invoke(battle,new object[]{battle.RunId,false}),Is.True);controller.Tick(0);
            Assert.That(view.Playing,Is.True);Assert.That(controller.SettlementVisible,Is.False);
            controller.Tick(.95f);Capture(view);
            Assert.That(volley.Cancelled,Is.True);Assert.That(volley.Hits.Count,Is.Zero);Assert.That(battle.Clock,Is.EqualTo(clock));
            Assert.That(view.VisibleImpactTargetCount,Is.Zero);Assert.That(view.DisplayedDamage,Is.Zero);
            Assert.That(view.DisplayedStrikeCount,Is.Zero);Assert.That(view.ChargeProgress,Is.Zero);
            Assert.That(view.SoundPlayCount,Is.Zero);Assert.That(view.SoundPlaying||view.SlashSoundPlaying,Is.False,"Cancellation before the first real strike cannot emit a later original cue or future slash.");
            foreach(var guard in battle.Defenders)Assert.That(guard.Health,Is.EqualTo(health[guard.EntityId]));
            controller.Tick(GuerrillaOrderVolley.PresentationDuration-view.Age+.01f);Assert.That(view.Playing,Is.False);Assert.That(Cleared(Capture(view)),Is.True);
            Assert.That(controller.SettlementVisible,Is.True);Assert.That(volley.Hits.Count,Is.Zero);
        }

        Sprite Solid(Color color)
        {
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(color,1024).ToArray());texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f);owned.Add(texture);owned.Add(sprite);return sprite;
        }

        [UnityTest]public IEnumerator RestartClearsImmediatelyAndTerminalPresentationFinishesWithoutMoreCombat()
        {
            yield return LoadFormation();yield return StartRunning();var view=controller.GetComponent<GuerrillaOrderView>();
            view.Play(Originals);PresentationTo(view,1.1f);Capture(view);Assert.That(view.SoundPlayCount,Is.EqualTo(1));
            controller.RestartBattle();for(int i=0;i<1200&&controller.ArtLoading;i++)yield return null;yield return null;
            Assert.That(view.Playing,Is.False);Assert.That(view.SlotMembers.All(string.IsNullOrEmpty),Is.True);Assert.That(Cleared(Capture(view)),Is.True);Assert.That(view.SoundPlaying||view.SlashSoundPlaying,Is.False);
            if(view.ScreenEffect!=null){Assert.That(view.ScreenEffect.Offset,Is.EqualTo(Vector2.zero));Assert.That(view.ScreenEffect.Zoom,Is.EqualTo(1));}
            Assert.That(controller.SetSandboxRoster(2),Is.True);yield return StartRunning();
            view.Play(Originals);PresentationTo(view,1.1f);Capture(view);
            var battle=controller.Battle;int sounds=view.SoundPlayCount;double clock=battle.Clock;var health=battle.Defenders.Select(d=>d.Health).ToArray();
            // Explicit terminal-state fixture isolates the already emitted visual tail from win conditions.
            var finish=typeof(ManualBattle).GetMethod("FinishForTesting",PrivateInstance);
            Assert.That((bool)finish.Invoke(battle,new object[]{battle.RunId,false}),Is.True);controller.Tick(0);
            Assert.That(controller.SettlementVisible,Is.False);Assert.That(view.Playing,Is.True);
            controller.Tick(1.1f);Capture(view);
            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(view.VisiblePortraitCount,Is.Zero);Assert.That(view.Playing,Is.True);
            CollectionAssert.AreEqual(health,battle.Defenders.Select(d=>d.Health));Assert.That(view.SoundPlayCount,Is.EqualTo(sounds));
            controller.Tick(GuerrillaOrderVolley.PresentationDuration-view.Age+.01f);yield return null;
            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(view.Playing,Is.False);Assert.That(Cleared(Capture(view)),Is.True);Assert.That(controller.SettlementVisible,Is.True);
            controller.Tick(.2f);yield return null;
            Assert.That(view.Playing,Is.False);Assert.That(Cleared(Capture(view)),Is.True);Assert.That(view.SoundPlayCount,Is.EqualTo(sounds),"A completed terminal tail cannot restart when its pending interval disappears.");
        }
    }
}
