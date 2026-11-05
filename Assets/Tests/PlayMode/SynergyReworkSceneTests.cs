using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class SynergyReworkSceneTests
    {
        float scale;
        [SetUp]public void Setup(){scale=Time.timeScale;}
        [TearDown]public void Cleanup(){Time.timeScale=scale;}
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<900;i++){var c=Object.FindAnyObjectByType<ManualBattleController>();if(c!=null&&c.SynergySandbox&&!c.ArtLoading){Time.timeScale=0;yield break;}yield return null;}
            Assert.Fail("Sandbox load timeout");
        }
        [UnityTest]public IEnumerator ApprovedLeadersHaveRealClipsAndMudrockKeepsCaptureInThePool()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(10));
            Assert.That(c.Shop.Roles.Count,Is.EqualTo(39));Assert.That(c.Battle.GolemStage,Is.EqualTo(4));Assert.That(c.Battle.MudrockTier,Is.EqualTo(5));
            Assert.That(c.Battle.Gramophones.Count,Is.EqualTo(3));Assert.That(Object.FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(6));Assert.That(c.Battle.Defenders.All(d=>d.Settings.Skill.Enabled));
            foreach(string id in new[]{"CARD_M42","CARD_M43"})
            {
                var card=Resources.Load<ManualBattleDefinition>("MonsterTests/"+id.Substring(5)).Card;Assert.That(card.Portrait,Is.Not.Null);Assert.That(card.HasFrameAnimation);
                Assert.That(card.BattleSprite,Is.Not.Null);
            }
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();var r=c.Battle.TryDeployRole(c.Battle.RunId,50,c.Battle.Entry,"CARD_M28");Assert.That(r.Success);
            var mudrock=c.Battle.FindUnit(r.EntityId);Assert.That(c.Battle.UnitCombat(mudrock).MudrockCapture);Assert.That(c.InvestCampaignGolem(1),Is.False);c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator StormWeatherStillRendersWhenFrozenTargetsCannotReceiveMoreControl()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(0));c.StartLoopNextBattle();c.Battle.Begin();
            var r=c.Battle.TryDeployRole(c.Battle.RunId,900,c.Battle.Entry,"CARD_M14");var u=c.Battle.FindUnit(r.EntityId);
            for(int i=0;i<250&&!c.Battle.IsSkillReady(u.EntityId);i++)c.Tick(.1f);
            foreach(var d in c.Battle.Defenders){c.Battle.TryApplyCold(d.EntityId,10);c.Battle.TryApplyCold(d.EntityId,10);}
            Assert.That(c.Battle.TryActivateSkill(c.Battle.RunId,901,u.EntityId),Is.EqualTo(SkillFailure.None));c.Tick(.7f);yield return null;yield return null;
            Assert.That(c.Battle.SnowstormPulses.Count,Is.EqualTo(1));Assert.That(c.Battle.SnowstormPulses[0].Targets.Count,Is.EqualTo(c.Battle.Defenders.Count));
            var view=c.GetComponent<FrostSealView>();Assert.That(view.VisibleEffects,Is.GreaterThan(0));
            var camera=Camera.main;var prior=camera.targetTexture;var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;
            var active=RenderTexture.active;var image=new Texture2D(640,360,TextureFormat.RGB24,false);
            var renderers=view.GetComponentsInChildren<SpriteRenderer>().Where(v=>v.enabled).ToArray();
            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var visible=image.GetPixels32();
            foreach(var renderer in renderers)renderer.enabled=false;
            camera.Render();image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var hidden=image.GetPixels32();
            int changed=0;for(int i=0;i<visible.Length;i++)if(System.Math.Abs(visible[i].r-hidden[i].r)+System.Math.Abs(visible[i].g-hidden[i].g)+System.Math.Abs(visible[i].b-hidden[i].b)>12)changed++;
            foreach(var renderer in renderers)renderer.enabled=true;
            camera.targetTexture=prior;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(image);
            Assert.That(changed,Is.GreaterThan(30),"Storm must contribute actual pixels above the authored terrain, not merely enable a renderer");c.LeaveShopTrial();
        }
        [UnityTest]public IEnumerator TrainingPresetAndInvestmentSurviveReadySnapshotAndPauseGolemDrag()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.SetSandboxRoster(8));Assert.That(c.Battle.GolemStage,Is.EqualTo(2));
            Assert.That(c.InvestCampaignGolem(10));Assert.That(c.Loop.GolemInvestment,Is.EqualTo(30));Assert.That(c.InvestCampaignGolem(1),Is.False);
            Assert.That(c.SetSandboxRoster(10));Assert.That(c.Battle.GolemStage,Is.EqualTo(4));Assert.That(c.StartLoopNextBattle());c.Battle.Begin();c.Battle.SetPaused(true);yield return null;
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(c.Battle.Entry));
            c.ProcessShopPointer(EventType.MouseDown,ManualBattleController.GolemCardRect.center);c.ProcessShopPointer(EventType.MouseDrag,gate);c.ProcessShopPointer(EventType.MouseUp,gate);
            var golem=c.Battle.Units.Single(u=>u.Golem);Assert.That(golem.MaxHealth,Is.EqualTo(6000));Assert.That(c.Battle.DP,Is.EqualTo(86));c.Tick(1);Assert.That(c.Battle.Clock,Is.Zero);
            yield return null;Assert.That(Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Any(v=>v.Renderer.sprite!=null&&v.Renderer.sprite.name.StartsWith("GOLEM/")));c.LeaveShopTrial();
        }
    }
}
