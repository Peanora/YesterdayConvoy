using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed class BattleWaveDefinition
    {
        public string LaneId { get; }
        public int DP { get; }
        public float DeploymentSeconds { get; }

        public BattleWaveDefinition(string laneId,int dp,float deploymentSeconds)
        {
            if(string.IsNullOrWhiteSpace(laneId))throw new ArgumentException("A wave requires a deployment lane.",nameof(laneId));
            if(dp<0)throw new ArgumentOutOfRangeException(nameof(dp));
            if(float.IsNaN(deploymentSeconds)||float.IsInfinity(deploymentSeconds)||deploymentSeconds<=0)
                throw new ArgumentOutOfRangeException(nameof(deploymentSeconds));
            LaneId=laneId;DP=dp;DeploymentSeconds=deploymentSeconds;
        }
    }

    /// <summary>Immutable deployment windows followed by a deployment-free final advance.</summary>
    public sealed class BattleWavePlan
    {
        public IReadOnlyList<BattleWaveDefinition> Waves { get; }
        public float FinalAdvanceSeconds { get; }
        public double TotalDuration { get; }

        public BattleWavePlan(IEnumerable<BattleWaveDefinition> waves,float finalAdvanceSeconds)
        {
            if(waves==null)throw new ArgumentNullException(nameof(waves));
            var copy=waves.ToArray();
            if(copy.Length==0||copy.Any(w=>w==null))throw new ArgumentException("At least one valid wave is required.",nameof(waves));
            if(float.IsNaN(finalAdvanceSeconds)||float.IsInfinity(finalAdvanceSeconds)||finalAdvanceSeconds<=0)
                throw new ArgumentOutOfRangeException(nameof(finalAdvanceSeconds));
            Waves=Array.AsReadOnly(copy);FinalAdvanceSeconds=finalAdvanceSeconds;
            TotalDuration=copy.Sum(w=>(double)w.DeploymentSeconds)+finalAdvanceSeconds;
        }
    }
}
