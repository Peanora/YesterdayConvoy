using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FrostSealSceneTests
    {
        float previous;
        [SetUp]public void Setup()=>previous=Time.timeScale;
        [TearDown]public void TearDown()=>Time.timeScale=previous;
        [UnityTest]public IEnumerator ActualFrostNovaChargesCastsOriginalFramesFreezesAndClearsOnRestart()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterMonsterTest("CARD_M14"),Is.True);
            c.SelectCard(true);Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);var unit=c.Battle.Units[0];
            Assert.That(unit.SkillSP,Is.EqualTo(20));c.Tick(12.05f);Assert.That(c.Battle.IsSkillReady(unit.EntityId),Is.True);
            c.SelectSkillUnit(unit.EntityId);Assert.That(c.SkillName,Is.EqualTo("冰封"));Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
            var point=unit.Motion.Position;c.Tick(.5f);yield return null;
            var visual=Object.FindObjectsByType<DemoSpriteVisual>().Single(v=>v.transform.parent.name==unit.EntityId);
            Assert.That(visual.FrameState,Is.EqualTo("FrostSeal"));Assert.That(visual.Renderer.sprite.name,Does.StartWith("M14/extra_skill_1/"));int frame=visual.FrameIndex;
            c.Tick(.3f);yield return null;Assert.That(visual.FrameIndex,Is.GreaterThan(frame));
            c.Tick(.65f);yield return null;Assert.That(c.Battle.FrostSeals[0].FrozenTargets.Count,Is.EqualTo(1));
            Assert.That(c.Battle.Defender.FrozenUntil,Is.GreaterThan(c.Battle.Clock));Assert.That(unit.Motion.Position,Is.EqualTo(point));
            var view=Object.FindAnyObjectByType<FrostSealView>();var cast=c.Battle.FrostSeals.Single();
            Assert.That(cast.SourceId,Is.EqualTo(unit.EntityId));Assert.That(cast.Resolved&&!cast.Cancelled,Is.True);
            var target=c.Battle.FindDefender(cast.FrozenTargets.Single());
            var casterStart=NativeLayer(view,"firstar_skill_02_start");
            var targetIce=NativeLayer(view,"firstar_skill_02_range_02");
            Assert.That(Vector3.Distance(casterStart.transform.position,cast.Position),Is.LessThan(.0001f),"Caster layers stay on the actual source.");
            Assert.That(Vector3.Distance(targetIce.transform.position,target.Position),Is.LessThan(.0001f),"Black ice belongs to the frozen target, not the caster.");
            Assert.That(targetIce.transform.position,Is.Not.EqualTo(casterStart.transform.position));
            Assert.That(targetIce.sharedMaterial.shader.name,Is.EqualTo("YesterdayConvoy/TalulahNative"));
            CaptureEvidence(Camera.main);
            var layers=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).ToArray();
            var frozenFrames=layers.Select(r=>r.sprite).ToArray();var frozenPositions=layers.Select(r=>r.transform.position).ToArray();
            double clock=c.Battle.Clock,presentation=c.PresentationClock,freezeUntil=target.FrozenUntil;int pausedBodyFrame=visual.FrameIndex;
            var camera=Camera.main;var cameraPosition=camera.transform.position;
            c.Battle.SetPaused(true);c.Tick(10);yield return null;Assert.That(c.Battle.Clock,Is.EqualTo(clock));
            Assert.That(c.PresentationClock,Is.EqualTo(presentation));Assert.That(target.FrozenUntil,Is.EqualTo(freezeUntil));Assert.That(visual.FrameIndex,Is.EqualTo(pausedBodyFrame));
            for(int i=0;i<layers.Length;i++)
            {Assert.That(layers[i].sprite,Is.SameAs(frozenFrames[i]));Assert.That(layers[i].transform.position,Is.EqualTo(frozenPositions[i]));}
            Assert.That(camera.transform.position,Is.EqualTo(cameraPosition));
            c.Battle.SetPaused(false);c.Tick(1.6f);yield return null;Assert.That(c.Battle.IsSkillActionPlaying(unit),Is.False);
            CaptureEvidence(Camera.main);
            Assert.That(visual.FrameState,Is.Not.EqualTo("FrostSeal"));Assert.That(target.FrozenUntil,Is.GreaterThan(c.Battle.Clock));
            Assert.That(NativeLayer(view,"firstar_skill_02_range_02").transform.position,Is.EqualTo(target.Position),"The target's original ice remains after the caster action finishes.");
            c.Tick(3.7f);yield return null;Assert.That(target.FrozenUntil,Is.LessThanOrEqualTo(c.Battle.Clock));
            Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.sprite!=null&&r.sprite.name.StartsWith("OriginalVfx/monster/firstar_skill_02_",System.StringComparison.Ordinal)),Is.False,"The completed manual cast leaves no caster/black-ice layer; a later real automatic ice ring is allowed.");
            c.RestartBattle();yield return null;Assert.That(view.VisibleEffects,Is.Zero);Assert.That(c.Battle.FrostSeals,Is.Empty);
        }

        static SpriteRenderer NativeLayer(FrostSealView view,string rawKey)=>view.GetComponentsInChildren<SpriteRenderer>()
            .Single(r=>r.enabled&&r.sprite!=null&&r.sprite.name.StartsWith("OriginalVfx/monster/"+rawKey+"/",System.StringComparison.Ordinal));

        static void CaptureEvidence(Camera camera)
        {
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldRect=camera.rect;
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../artifacts"));
                System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"frostnova-actual.png"),image.EncodeToPNG());
            }
            finally{camera.targetTexture=oldTarget;camera.rect=oldRect;RenderTexture.active=oldActive;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }

        [UnityTest]public IEnumerator NaturalLateShopRollCanBuyFrostNovaAndDragHerIntoTheSecondBattle()
        {
            yield return SceneManager.LoadSceneAsync("BattleShop");yield return null;yield return null;Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();c.StartLateBattleShopLoop(73);
            const string id="CARD_M14";Assert.That(c.Shop.Roles.Any(r=>r.Id==id&&r.Cost==5),Is.True);Assert.That(c.Shop.Available(id),Is.EqualTo(9));
            // Search a deterministic normal refresh seed; do not inject a FrostNova offer into the game.
            int seed=0;var guarantee=typeof(ShopTrialModel).GetProperty("GuaranteedRefreshOffer",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int candidate=1;candidate<4000&&seed==0;candidate++)
            {
                var model=new ShopTrialModel(c.Shop.Roles,candidate);
                model.Preset(c.Shop.Bench,System.Array.Empty<string>(),7,0,30);
                guarantee.SetValue(model,"CARD_TALULAH_TRIAL");model.Refresh(true);
                if(model.Offers.Contains(id))seed=candidate;
            }
            Assert.That(seed,Is.GreaterThan(0));c.StartLateBattleShopLoop(seed);
            typeof(ManualBattleController).GetField("loopTestSettlementOpen",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,true);
            Assert.That((bool)typeof(ManualBattleController).GetMethod("TryTestSettlement",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{true}),Is.True);
            Assert.That(c.OpenLoopShop(),Is.True);yield return new WaitForSecondsRealtime(.35f);
            Assert.That(c.Shop.Offers[0],Is.EqualTo("CARD_TALULAH_TRIAL"));int slot=c.Shop.Offers.ToList().IndexOf(id);Assert.That(slot,Is.GreaterThan(0));
            int gold=c.Shop.Gold;var receipt=c.PurchaseShopCard(slot);Assert.That(receipt.Success,Is.True);Assert.That(c.Shop.Gold,Is.EqualTo(gold-5));c.Shop.Validate();
            yield return new WaitForSecondsRealtime(.85f);Assert.That(c.ShopBenchModel(receipt.Instance).sprite.name,Does.StartWith("M14/idle/"));
            typeof(ManualBattleController).GetMethod("PrepareNextLoopBattle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
            while(c.ArtLoading)yield return null;
            Assert.That(c.Loop.CanDeploy,Is.True);yield return new WaitForSecondsRealtime(.3f);
            int bench=c.Shop.Bench.ToList().FindIndex(p=>p.Id==id);var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Lanes[0].Entry));
            c.ProcessShopPointer(EventType.MouseDown,c.ShopPieceRect(bench).center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            var u=c.Battle.Units.Single();Assert.That(u.CardId,Is.EqualTo(id));Assert.That(c.Battle.UnitCombat(u).FrostSeal,Is.True);
            Assert.That(u.SkillSP,Is.EqualTo(20));c.Tick(12.05f);Assert.That(c.Battle.IsSkillReady(u.EntityId),Is.True);
            c.SelectSkillUnit(u.EntityId);Assert.That(c.SkillName,Is.EqualTo("冰封"));Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
            c.Tick(1.5f);yield return null;Assert.That(Object.FindAnyObjectByType<FrostSealView>().VisibleEffects,Is.GreaterThan(0));
            c.RestartBattle();Assert.That(c.Shop.Bench.Any(p=>p.Id==id),Is.False);
            c.StartBattleShopLoop(seed);Assert.That(c.Shop.Roles.Any(r=>r.Id==id),Is.False);c.LeaveShopTrial();
        }
    }
}
