using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class WaveScenarioConfigurationTests
    {
        [Test]public void AuthoredForestPlanUsesThreeIndependentBudgetsAndALimitedFinalAdvance()
        {
            var map=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            var plan=map.WaveSnapshot();
            Assert.That(plan,Is.Not.Null);
            Assert.That(plan.Waves.Select(w=>w.LaneId),Is.EqualTo(new[]{"R1","R2","R3"}));
            Assert.That(plan.Waves.Select(w=>w.DP),Is.EqualTo(new[]{16,16,18}));
            Assert.That(plan.Waves.All(w=>w.DeploymentSeconds==45),Is.True);
            Assert.That(plan.FinalAdvanceSeconds,Is.EqualTo(120));
            Assert.That(plan.TotalDuration,Is.EqualTo(255));
            Assert.That(plan.Waves.Sum(w=>w.DP),Is.EqualTo(BlackForestChallengePlan.TotalDP));
        }

        [Test]public void AuthoredWaveEditsCannotMutateAnAlreadyCreatedBattlePlan()
        {
            var authored=ScriptableObject.CreateInstance<AttackWaveScenario>();
            try
            {
                authored.waves=new[]{new AttackWaveSettings{laneId="R1",dp=16,deploymentSeconds=45}};
                authored.finalAdvanceSeconds=120;
                var plan=authored.Snapshot();
                authored.waves[0].laneId="R3";authored.waves[0].dp=999;authored.waves[0].deploymentSeconds=1;authored.finalAdvanceSeconds=1;
                Assert.That(plan.Waves[0].LaneId,Is.EqualTo("R1"));
                Assert.That(plan.Waves[0].DP,Is.EqualTo(16));
                Assert.That(plan.TotalDuration,Is.EqualTo(165));
            }
            finally{UnityEngine.Object.DestroyImmediate(authored);}
        }

        [Test]public void EveryPlayableCampaignCardUsesItsWaveCadenceWithoutChangingUsesOrLegacyCadence()
        {
            var definitions=AssetDatabase.FindAssets("t:ManualBattleDefinition",new[]{"Assets/Data"})
                .Select(id=>AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(AssetDatabase.GUIDToAssetPath(id)))
                .Where(d=>d!=null&&d.Card!=null).ToArray();
            foreach(var entry in CampaignBalance.Profiles)
            {
                var data=definitions.FirstOrDefault(d=>d.Card.CardId==entry.Key);
                Assert.That(data,Is.Not.Null,entry.Key);
                int cost=entry.Value.Cost;
                foreach(int star in new[]{1,ShopTrialModel.MaxStarForCost(cost)})
                {
                    var legacy=CampaignBalance.Build(data,star,16,6);
                    var wave=CampaignBalance.Build(data,star,16,6,waveMode:true);
                    Assert.That(wave.Unit.Cooldown,Is.EqualTo(WaveDeploymentCadence.ForCard(entry.Key,cost)),entry.Key);
                    Assert.That(legacy.Unit.Cooldown,Is.EqualTo(new[]{0,2,3,4,5,8}[cost]),entry.Key);
                    Assert.That(wave.Unit.DeploymentLimit,Is.EqualTo(legacy.Unit.DeploymentLimit),entry.Key);
                    Assert.That(wave.Unit.Health,Is.EqualTo(legacy.Unit.Health),entry.Key);
                    Assert.That(wave.Unit.DeploymentCost,Is.EqualTo(legacy.Unit.DeploymentCost),entry.Key);
                }
            }
            Assert.That(WaveDeploymentCadence.ForCard("CARD_M04",2),Is.EqualTo(14));
            Assert.That(WaveDeploymentCadence.ForCard("CARD_M06",2),Is.EqualTo(16));
            Assert.That(WaveDeploymentCadence.ForCard("CARD_M17",3),Is.EqualTo(20));
            Assert.That(WaveDeploymentCadence.ForCard("CARD_M38",3),Is.EqualTo(24));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaveDeploymentCadence.ForCard("bad",0));
        }
    }
}
