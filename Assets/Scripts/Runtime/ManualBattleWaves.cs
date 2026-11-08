using System;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        const double WaveBoundaryTolerance=0.0000001;
        int currentWaveIndex;
        bool finalAdvanceActive;
        double waveEndsAt;

        public BattleWavePlan WavePlan { get; private set; }
        public bool WavesEnabled=>WavePlan!=null;
        public int CurrentWaveIndex=>currentWaveIndex;
        public int WaveCount=>WavePlan?.Waves.Count??0;
        public int CurrentWaveBudget=>WavesEnabled&&!finalAdvanceActive?WavePlan.Waves[currentWaveIndex].DP:0;
        public string CurrentWaveLaneId=>WavesEnabled&&!finalAdvanceActive?WavePlan.Waves[currentWaveIndex].LaneId:null;
        public bool DeploymentWindowOpen=>(Phase==BattlePhase.Ready||Phase==BattlePhase.Running)&&!finalAdvanceActive;
        public bool FinalAdvanceActive=>WavesEnabled&&finalAdvanceActive;
        public double WaveRemaining=>WavesEnabled?Math.Max(0,waveEndsAt-Clock):0;
        public double BattleTimeRemaining=>WavesEnabled?Math.Max(0,WavePlan.TotalDuration-Clock):0;
        public bool TimedOut { get; private set; }

        public void ConfigureWaves(BattleWavePlan plan)
        {
            if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Configure waves before battle begins.");
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            foreach(var wave in plan.Waves)
            {
                bool exists=false;
                foreach(var lane in lanes)if(lane.Id==wave.LaneId){exists=true;break;}
                if(!exists)throw new ArgumentException("Wave refers to an unknown deployment lane: "+wave.LaneId,nameof(plan));
            }
            WavePlan=plan;ResetWaves();
        }

        public bool CanDeployOnLane(string laneId)
        {
            if(!DeploymentWindowOpen)return false;
            foreach(var lane in lanes)
                if(lane.Id==laneId)return !WavesEnabled||laneId==CurrentWaveLaneId;
            return false;
        }

        void ResetWaves()
        {
            currentWaveIndex=0;finalAdvanceActive=false;TimedOut=false;
            waveEndsAt=WavesEnabled?WavePlan.Waves[0].DeploymentSeconds:0;
            if(WavesEnabled)DP=WavePlan.Waves[0].DP;
        }

        DeployFailure CheckWaveDeployment(string laneId)
        {
            if(!WavesEnabled)return DeployFailure.None;
            if(finalAdvanceActive)return DeployFailure.DeploymentWindowEnded;
            return laneId==CurrentWaveLaneId?DeployFailure.None:DeployFailure.WaveEntryClosed;
        }

        double LimitWaveStep(double requested)
        {
            if(!WavesEnabled)return requested;
            return Math.Min(requested,Math.Max(0,waveEndsAt-Clock));
        }

        void AdvanceWaveBoundaries()
        {
            if(!WavesEnabled||Defense==0)return;
            while(!finalAdvanceActive&&Clock+WaveBoundaryTolerance>=waveEndsAt)
            {
                if(Math.Abs(Clock-waveEndsAt)<=WaveBoundaryTolerance)Clock=waveEndsAt;
                if(currentWaveIndex+1<WaveCount)
                {
                    currentWaveIndex++;
                    waveEndsAt+=WavePlan.Waves[currentWaveIndex].DeploymentSeconds;
                    DP=WavePlan.Waves[currentWaveIndex].DP;
                }
                else
                {
                    finalAdvanceActive=true;DP=0;waveEndsAt=WavePlan.TotalDuration;
                }
            }
            if(finalAdvanceActive&&Math.Abs(Clock-waveEndsAt)<=WaveBoundaryTolerance)Clock=waveEndsAt;
        }

        bool WaveDeadlineReached=>WavesEnabled&&finalAdvanceActive&&Clock+WaveBoundaryTolerance>=WavePlan.TotalDuration;
    }
}
