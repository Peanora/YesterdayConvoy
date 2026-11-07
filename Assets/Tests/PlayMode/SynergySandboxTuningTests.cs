using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class SynergySandboxTuningTests
    {
        float scale;
        [SetUp]public void Setup(){scale=Time.timeScale;}
        [TearDown]public void Cleanup(){Time.timeScale=scale;}

        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            for(int i=0;i<1200;i++)
            {
                var controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading){Time.timeScale=0;yield break;}
                yield return null;
            }
            Assert.Fail("Sandbox did not finish preparing");
        }

        [UnityTest]public IEnumerator MixedRosterBoostsSurviveRebuildWithoutChangingAuthoredOrNormalStats()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(c.SetSandboxRoster(2));Assert.That(c.Shop.Roles.Count,Is.EqualTo(43));
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/M20");
            var baseline=CampaignBalance.Build(data,1,100,6);float authored=data.Snapshot().Health;
            var ordinaryGuard=CampaignPlan.Guards(1,true)[0];
            Assert.That(c.Battle.Defenders[0].MaxHealth,Is.EqualTo(ordinaryGuard.health*12));
            Assert.That(c.Battle.Defenders[0].Settings.Attack,Is.EqualTo(ordinaryGuard.attack*8));
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();c.Battle.SetPaused(true);
            var receipt=c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_M20",true);
            Assert.That(receipt.Success);var unit=c.Battle.FindUnit(receipt.EntityId);var combat=c.Battle.UnitCombat(unit);
            Assert.That(unit.MaxHealth,Is.EqualTo(baseline.Unit.Health*4));
            Assert.That(combat.UnitAttack,Is.EqualTo(baseline.Combat.UnitAttack*2));
            Assert.That(combat.Effects.Patriot);Assert.That(combat.HasSkill,Is.False);
            Assert.That(c.Battle.DP,Is.EqualTo(100-baseline.Unit.DeploymentCost));c.Tick(1);Assert.That(c.Battle.Clock,Is.Zero);
            Assert.That(c.RestartCampaignEncounter());
            Assert.That(c.Battle.Defenders[0].MaxHealth,Is.EqualTo(ordinaryGuard.health*12),"Restart must not compound the test multiplier");
            Assert.That(data.Snapshot().Health,Is.EqualTo(authored));
            Assert.That(CampaignBalance.Build(data,1,100,6).Unit.Health,Is.EqualTo(baseline.Unit.Health));
            c.StartCampaign(false,44);
            Assert.That(c.Battle.Settings.Health,Is.EqualTo(CampaignBalance.Profiles[c.Battle.Settings.CardId].HP));
            Assert.That(c.Battle.Defenders[0].Settings.Attack,Is.EqualTo(CampaignPlan.Guards(1,false)[0].attack));
            c.LeaveShopTrial();
        }

        [UnityTest]public IEnumerator SkillDamageAndHealingBoostKeepResourcesAndOriginalUltimateBenchmark()
        {
            yield return Load();var c=Object.FindAnyObjectByType<ManualBattleController>();
            var frost=Resources.Load<ManualBattleDefinition>("MonsterTests/M14");var baseline=CampaignBalance.Build(frost,1,100,6);
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();
            var unit=c.Battle.FindUnit(c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_M14").EntityId);
            var combat=c.Battle.UnitCombat(unit);
            Assert.That(combat.SkillAttack,Is.EqualTo(baseline.Combat.SkillAttack*2));
            Assert.That(combat.RequiredSP,Is.EqualTo(baseline.Combat.RequiredSP));
            Assert.That(combat.SkillInterval,Is.EqualTo(baseline.Combat.SkillInterval));
            c.StartCampaign(true,44);Assert.That(c.SetSandboxRoster(4));Assert.That(c.StartLoopNextBattle());c.Battle.Begin();
            var healer=c.Battle.FindUnit(c.Battle.TryDeployRole(c.Battle.RunId,1,c.Battle.Entry,"CARD_M25").EntityId);
            Assert.That(c.Battle.UnitCombat(healer).Effects.Healer);Assert.That(c.Battle.UnitCombat(healer).CanAttack,Is.False);
            Assert.That(c.Battle.UnitCombat(healer).UnitAttack,Is.EqualTo(CampaignBalance.Profiles["CARD_M25"].Attack*2));
            c.StartCampaign(true,44);Assert.That(c.SetSandboxRoster(10));Assert.That(c.Battle.Defenders.Count,Is.EqualTo(6));
            var reference=BattleShopStages.Defenders(2,true);
            Assert.That(c.Battle.Defenders[0].MaxHealth,Is.EqualTo(reference[0].health));
            Assert.That(c.StartLoopNextBattle());c.Battle.Begin();
            var golem=c.Battle.FindUnit(c.Battle.TryDeployGolem(c.Battle.RunId,1,c.Battle.Entry).EntityId);
            Assert.That(golem.MaxHealth,Is.EqualTo(6000));Assert.That(c.Shop.UnlimitedGold);c.LeaveShopTrial();
        }
    }
}
