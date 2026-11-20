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
    }
}
