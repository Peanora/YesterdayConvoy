using System;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public sealed class AttackWaveSettings
    {
        public string laneId;
        [Min(0)] public int dp=16;
        [Min(.1f)] public float deploymentSeconds=45;
    }

    [CreateAssetMenu(menuName="Yesterday Convoy/Attack Wave Scenario")]
    public sealed class AttackWaveScenario:ScriptableObject
    {
        public AttackWaveSettings[] waves=Array.Empty<AttackWaveSettings>();
        [Min(.1f)] public float finalAdvanceSeconds=120;

        // Authoring data is read-only in play. All counters belong to ManualBattle.
        public BattleWavePlan Snapshot()
        {
            if(waves==null||waves.Length==0)throw new InvalidOperationException("An attack scenario requires deployment waves.");
            return new BattleWavePlan(Array.ConvertAll(waves,w=>w==null
                ?throw new InvalidOperationException("Missing authored attack wave.")
                :new BattleWaveDefinition(w.laneId,w.dp,w.deploymentSeconds)),finalAdvanceSeconds);
        }
    }
}
