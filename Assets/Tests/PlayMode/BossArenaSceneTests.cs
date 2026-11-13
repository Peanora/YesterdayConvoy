using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace YesterdayConvoy.Tests
{
    public sealed class BossArenaSceneTests
    {
        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(BossArenaRules.SceneName);
            for(int i=0;i<1800;i++)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.CampaignActive&&!c.ArtLoading){yield return null;yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("Boss arena preparation timed out.");
        }
        static void Capture(string name)
        {
            if(Application.isBatchMode)return;
            var image=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(image,Is.Not.Null);
            try{Directory.CreateDirectory("artifacts/boss-arena/play");File.WriteAllBytes("artifacts/boss-arena/play/"+name+".png",image.EncodeToPNG());}
            finally{Object.Destroy(image);}
        }
        [UnityTest]public IEnumerator AuthoredCompactArenaStartsWholeFormationPausesAndReturnsToPreparation()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.BossArenaActive&&c.Loop.CanShop,Is.True);Assert.That(c.Battle.BossArenaEnabled,Is.True);
            var field=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(field,Is.Not.Null);
            Assert.That(field.GetComponentsInChildren<Renderer>().Length,Is.InRange(64,80));
            Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));Assert.That(c.Battle.ArenaBoss.Settings.ArtId,Is.EqualTo("D31"));
            Capture("01-preparation");Assert.That(c.StartLoopNextBattle());c.Battle.SetPaused(true);c.Tick(0);yield return null;
            foreach(var card in c.Battle.Cards)Assert.That(c.Battle.ArenaCardOnField(card.Settings.CardId),Is.True);
            Assert.That(c.Battle.ArenaDeployments,Is.EqualTo(8));Assert.That(c.Battle.DP,Is.Zero);
            var boss=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual(BossArenaRules.BossId);
            Assert.That(boss.enabled&&boss.sprite!=null,Is.True);Assert.That(boss.transform.localScale.x,Is.EqualTo(2.6f));
            double clock=c.Battle.Clock;float hp=c.Battle.ArenaBoss.Health;c.Tick(120);
            Assert.That(c.Battle.Clock,Is.EqualTo(clock));Assert.That(c.Battle.ArenaBoss.Health,Is.EqualTo(hp));
            c.DeployArenaReadySquad();Assert.That(c.Battle.ArenaDeployments,Is.EqualTo(8));Capture("02-squad-paused");
            var stars=c.Shop.Bench.Select(p=>p.Id+":"+p.Star).ToArray();c.OpenArenaPreparation();yield return null;
            Assert.That(c.Loop.CanShop&&c.ShopVisible,Is.True);Assert.That(c.Battle.Units.Count,Is.Zero);Assert.That(c.Battle.ArenaBoss.Health,Is.EqualTo(c.Battle.ArenaBoss.MaxHealth));
            Assert.That(c.Shop.Bench.Select(p=>p.Id+":"+p.Star),Is.EqualTo(stars));
        }
        [UnityTest]public IEnumerator RealCombatReachesHalfHealthRedeploysAndWinsWithNoDamageInjection()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();Time.timeScale=0;
            try
            {
                Assert.That(c.StartLoopNextBattle());bool half=false,cooling=false;long request=700000;
                for(int i=0;i<3600&&c.Battle.Phase==BattlePhase.Running;i++)
                {
                    c.Battle.DeployReadyArenaSquad();
                    foreach(var u in c.Battle.Units.ToArray())if(u.Active&&c.Battle.IsSkillReady(u.EntityId))c.Battle.TryActivateSkill(c.Battle.RunId,++request,u.EntityId);
                    c.Tick(.2f);
                    if(!cooling&&c.Battle.Cards.Any(card=>c.Battle.ArenaRedeploymentRemaining(card.Settings.CardId)>0))
                    {cooling=true;yield return null;yield return null;Capture("03-death-cooldown");}
                    if(!half&&c.Battle.ArenaEnraged)
                    {half=true;Assert.That(c.Battle.ArenaAttackSpeed,Is.EqualTo(2));yield return null;yield return null;Capture("04-half-health");}
                    if(i%20==0)yield return null;
                }
                Assert.That(half&&cooling,Is.True);Assert.That(c.Battle.Phase,Is.EqualTo(BattlePhase.Won));
                Assert.That(c.Battle.ArenaDeployments,Is.GreaterThan(8));Assert.That(c.Battle.Breaches,Is.Zero);Assert.That(c.Battle.DP,Is.Zero);
                c.Tick(2);yield return null;yield return null;Capture("05-victory");
            }
            finally{Time.timeScale=1;}
        }
    }
}
