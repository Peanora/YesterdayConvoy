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
