using System;
using System.Collections.Generic;
using System.Linq;
namespace YesterdayConvoy
{
    public sealed class SnowstormPulse
    {
        public double At {get;internal set;}
        public IReadOnlyList<string> Targets=>targets.AsReadOnly();
        internal readonly List<string> targets=new List<string>();
    }
    public sealed partial class ManualBattle
    {
        readonly List<SnowstormPulse> snowstormPulses=new List<SnowstormPulse>();
        readonly Dictionary<string,double> stormFreezeProtection=new Dictionary<string,double>();
        double snowstormStartedAt=double.NegativeInfinity;
        int snowstormNextPulse;
        public IReadOnlyList<SnowstormPulse> SnowstormPulses=>snowstormPulses.AsReadOnly();
        public bool SnowstormActive=>SnowTier==5&&Clock<snowstormStartedAt+8;
        void ResetSnowstorms(){snowstormPulses.Clear();stormFreezeProtection.Clear();snowstormStartedAt=double.NegativeInfinity;snowstormNextPulse=0;}
        void StartSnowstorm(DeployedUnitState unit)
        {
            if(!synergyEnabled||SnowTier!=5||unit.CardId!="CARD_M14"||SnowstormActive)return;
            snowstormStartedAt=Clock;snowstormNextPulse=0;
        }
        void StepSnowstorms()
        {
            if(SnowTier!=5||double.IsNegativeInfinity(snowstormStartedAt))return;
            while(snowstormNextPulse<4&&Clock+1e-6>=snowstormStartedAt+snowstormNextPulse*2)
            {
                var pulse=new SnowstormPulse{At=snowstormStartedAt+snowstormNextPulse++*2};snowstormPulses.Add(pulse);
                foreach(var d in defenders.Where(d=>d.Alive))
                {
                    if(d.FrozenUntil>Clock||stormFreezeProtection.TryGetValue(d.EntityId,out double until)&&Clock<until)continue;
                    bool cold=d.ColdUntil>Clock;
                    if(!TryApplyCold(d.EntityId,3.5f))continue;
                    pulse.targets.Add(d.EntityId);
                    // Only the extra storm respects this additional protection; it does not rewrite character skills.
                    if(cold&&d.FrozenUntil>Clock)stormFreezeProtection[d.EntityId]=d.FrozenUntil+3;
                }
            }
        }
    }
}
