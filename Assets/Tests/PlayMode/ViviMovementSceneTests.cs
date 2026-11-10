using System.Collections;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ViviMovementSceneTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityTest]public IEnumerator MixedJourneyShotsUseTheirOwnImpactArtAndRetainAlreadyAppliedTails()
        {
            float old=Time.timeScale;CampaignJourneySession.Clear();Time.timeScale=0;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");
                ManualBattleController c=null;yield return CampaignJourneyEvidenceProbe.Host(value=>c=value);
                string[] ids={"CARD_ORIGINIUM_SLUG","CARD_WISADEL_APRIL","CARD_TALULAH_TRIAL"};
                var definitions=c.MonsterTests.Where(o=>o.Playable&&CampaignBalance.ShopEligible(o.Id))
                    .Select(o=>(ManualBattleDefinition)typeof(MonsterTestOption).GetProperty("Definition",Private).GetValue(o)).ToArray();
                var shop=new ShopTrialModel(definitions.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),44);
                shop.EnableJourneyEconomy();shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),3,0,0);
                Assert.That(shop.SetLineup(ids,shop.Bench.Select(p=>p.Instance)),Is.True);
                var session=CampaignJourneySession.Begin(44);var loop=new BattleShopRun(shop,44,campaign:true,journeyV2:true);
                typeof(BattleShopRun).GetProperty("Stage").SetValue(loop,12);
                typeof(CampaignJourneySession).GetProperty("Shop").SetValue(session,shop);
                typeof(CampaignJourneySession).GetProperty("Loop").SetValue(session,loop);
                ((List<string>)typeof(CampaignJourneySession).GetField("Lineup",Private).GetValue(session)).AddRange(ids);
                yield return SceneManager.LoadSceneAsync(session.Map.sceneName);yield return CampaignJourneyEvidenceProbe.Ready(12,value=>c=value);
                Assert.That(c.Definition.Card.CardId,Is.EqualTo(ids[0]));
                Assert.That(c.Definition.Card.ComicImpactFaces==null||c.Definition.Card.ComicImpactFaces.Count==0,Is.True);
                // Explicit presentation fixture on the saved Journey map. Durable passive targets
                // isolate real shot events without changing an asset or claiming a natural clear.
                var profiles=ids.Select(id=>CampaignV2Balance.Build(definitions.Single(d=>d.Card.CardId==id),1,100,100)).ToArray();
                var guard=c.Battle.Defenders.First();var point=guard.Position;
                var battle=new ManualBattle(profiles[0].Unit,new[]{point+Vector3.left*2,point+Vector3.right*7},point,profiles[0].Combat,
                    roster:new[]{new DefenderSettings("mixed-fx-target","Mixed FX target",guard.Settings.ArtId,point,50000,1,0,.01f,100)});
                battle.ConfigureCardLoadout(profiles.Skip(1));battle.ConfigureCampaignV2(12);
                typeof(ManualBattleController).GetMethod("ClearViews",Private).Invoke(c,null);
                typeof(ManualBattleController).GetProperty("Battle").SetValue(c,battle);
                typeof(ManualBattleController).GetMethod("EnableCombatView",Private).Invoke(c,null);
                battle.Begin();
                var receipt=battle.TryDeployRole(battle.RunId,1,battle.Entry,ids[1]);Assert.That(receipt.Success,Is.True);
                var vivi=battle.FindUnit(receipt.EntityId);
                for(int i=0;i<2400&&!battle.IsSkillReady(vivi.EntityId);i++)c.Tick(.05f);
                c.SelectSkillUnit(vivi.EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                CombatShot shell=null;
                for(int i=0;i<400;i++){c.Tick(.01f);shell=battle.CombatShots.FirstOrDefault(s=>s.SourceId==vivi.EntityId&&s.Empowered&&s.Applied);if(shell!=null)break;}
                Assert.That(shell,Is.Not.Null);c.Tick(.08f);yield return null;
                var face=c.CombatView.GetComponentsInChildren<SpriteRenderer>().SingleOrDefault(r=>r.enabled&&r.name=="Comic expression");
                Assert.That(face,Is.Not.Null,"A low-cost first card must not remove Vivi's actual impact expression.");
                Assert.That(c.CardForUnit(vivi).ComicImpactFaces,Does.Contain(face.sprite));
                var visible=CaptureMixedImpact("vivi-mixed-impact.png");face.enabled=false;
                var hidden=CaptureMixedImpact(null);face.enabled=true;
                Assert.That(visible.Zip(hidden,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)).Average(),Is.GreaterThan(.005),"The source-specific stamp must change actual camera pixels.");
                var sprite=face.sprite;var position=face.transform.position;double clock=battle.Clock;
                battle.SetPaused(true);c.Tick(5);yield return null;
                Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(face.enabled,Is.True);Assert.That(face.sprite,Is.SameAs(sprite));Assert.That(face.transform.position,Is.EqualTo(position));
                // The impact has already happened. Caster death must not erase its visible tail.
                Set(vivi,"Health",0f);Set(vivi,"Resolution",UnitResolution.Dead);Set(vivi,"DeathTime",battle.Clock);
                yield return null;Assert.That(face.enabled,Is.True);Assert.That(face.sprite,Is.SameAs(sprite));
                battle.SetPaused(false);c.Tick(.6f);yield return null;Assert.That(face.enabled,Is.False);
                receipt=battle.TryDeployRole(battle.RunId,2,battle.Entry,ids[2]);Assert.That(receipt.Success,Is.True);
                var talulah=battle.FindUnit(receipt.EntityId);
                for(int i=0;i<2400&&!battle.IsSkillReady(talulah.EntityId);i++)c.Tick(.05f);
                c.SelectSkillUnit(talulah.EntityId);Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None));
                CombatShot rain=null;
                for(int i=0;i<300;i++){c.Tick(.01f);rain=battle.CombatShots.FirstOrDefault(s=>s.SourceId==talulah.EntityId&&s.FireRain&&s.Applied);if(rain!=null)break;}
                Assert.That(rain,Is.Not.Null);c.Tick(.08f);yield return null;
                var impactFrames=c.CardForUnit(talulah).ArticulatedArt.ImpactFrames;
                Assert.That(c.CombatView.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Comic expression"&&impactFrames.Contains(r.sprite)),Is.True,"Talulah's rain impact must use her own art in the same mixed lineup.");
                CaptureMixedImpact("talulah-mixed-impact.png");
                typeof(EngagementView).GetMethod("ClearTransient",Private).Invoke(c.CombatView,null);
                Assert.That(c.CombatView.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Comic expression"),Is.False);
            }
            finally{CampaignJourneySession.Clear();Time.timeScale=old;}
        }
        static Color32[] CaptureMixedImpact(string name)
        {
            var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                if(name!=null){string folder="artifacts/campaign-feedback-2026-11-10";Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToPNG());}
                return texture.GetPixels32();
            }
            finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }
        static void Set(DeployedUnitState u,string name,object value)=>typeof(DeployedUnitState).GetProperty(name).SetValue(u,value);
        [UnityTest]public IEnumerator BothVisualModesMirrorRetreatAndKeepTheGifWalkingForwardDuringFear()
        {
            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            var frames=Object.FindAnyObjectByType<ManualBattleController>().Definition.Card;
            foreach(bool upright in new[]{false,true})
            {
                var settings=new ManualBattleSettings(frames.CardId,"VIVI",100,1,1,2,.1f,1000,.75f,1,1,1,1);
                var combat=new EngagementSettings(unitRange:.01f,defenderRange:.01f,blockCount:0);
                var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.right*10},Vector3.forward*100,combat);b.Begin();
                var receipt=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(receipt.Success,Is.True);var u=b.FindUnit(receipt.EntityId);
                var root=new GameObject("Vivi logical root");var child=new GameObject("Vivi visual",typeof(SpriteRenderer));child.transform.SetParent(root.transform,false);
                var visual=child.AddComponent<DemoSpriteVisual>();visual.WorldUpright=upright;visual.Bind(b,u,frames);
                try
                {
                    b.Advance(.2f);root.transform.position=u.Motion.Position;yield return null;
                    Assert.That(visual.Renderer.flipX,Is.False);int frame=visual.FrameIndex;double distance=u.Motion.DistanceTravelled;
                    Set(u,"FearUntil",b.Clock+10);b.Advance(.05f);root.transform.position=u.Motion.Position;yield return null;
                    Assert.That(u.Motion.DistanceTravelled,Is.LessThan(distance));Assert.That(visual.Renderer.flipX,Is.True,"The visual mirrors in both the single trial and tactical maps.");
                    Assert.That(visual.FrameIndex,Is.GreaterThan(frame),"Retreat accumulates positive step distance instead of rewinding GIF frames.");
                    frame=visual.FrameIndex;b.SetPaused(true);var position=root.transform.position;yield return null;yield return null;
                    Assert.That(visual.FrameIndex,Is.EqualTo(frame));Assert.That(visual.Renderer.flipX,Is.True);
                    Assert.That(root.transform.position,Is.EqualTo(position));Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));
                    b.SetPaused(false);Set(u,"FearUntil",double.NegativeInfinity);b.Advance(.05f);yield return null;
                    Assert.That(visual.Renderer.flipX,Is.False);
                }
                finally{Object.Destroy(root);}
                yield return null;
            }
        }
    }
}
