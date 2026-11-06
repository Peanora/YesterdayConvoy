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
        public const float SnowstormDuration=8;
        public const int SnowstormChargeRequired=3;
        readonly List<SnowstormPulse> snowstormPulses=new List<SnowstormPulse>();
        readonly Dictionary<string,double> stormFreezeProtection=new Dictionary<string,double>();
        double snowstormStartedAt=double.NegativeInfinity;
        int snowstormNextPulse;
        public IReadOnlyList<SnowstormPulse> SnowstormPulses=>snowstormPulses.AsReadOnly();
        public double SnowstormStartedAt=>snowstormStartedAt;
        public double SnowstormEndsAt=>snowstormStartedAt+SnowstormDuration;
        public double SnowstormRemaining=>SnowstormActive?Math.Max(0,SnowstormEndsAt-Clock):0;
        public bool SnowstormActive=>SnowTier==5&&Clock<SnowstormEndsAt;
        public int SnowstormCharge {get;private set;}
        void ResetSnowstorms(){snowstormPulses.Clear();stormFreezeProtection.Clear();snowstormStartedAt=double.NegativeInfinity;snowstormNextPulse=0;SnowstormCharge=0;}
        void OnSnowstormShatter()
        {
            // Count completed synergy shatters, not individual hits, victims or a particular character's skill.
            // Shatters inside the weather cannot charge another storm from its own control effects.
            if(!synergyEnabled||SnowTier!=5||SnowstormActive)return;
            if(++SnowstormCharge<SnowstormChargeRequired)return;
            StartSnowstorm();
        }
        void StartSnowstorm()
        {
            if(!synergyEnabled||SnowTier!=5||SnowstormActive)return;
            SnowstormCharge=0;
            snowstormStartedAt=Clock;snowstormNextPulse=0;
        }
        void StepSnowstorms()
        {
            if(SnowTier!=5||double.IsNegativeInfinity(snowstormStartedAt))return;
            while(snowstormNextPulse*2<SnowstormDuration&&Clock+1e-6>=snowstormStartedAt+snowstormNextPulse*2)
            {
                var pulse=new SnowstormPulse{At=snowstormStartedAt+snowstormNextPulse++*2};snowstormPulses.Add(pulse);
                float power=1+.35f*((float)synergyStars.Where(p=>CampaignBalance.Snow(p.Key)).Average(p=>p.Value)-1);
                foreach(var d in defenders.Where(d=>d.Alive))
                {
                    pulse.targets.Add(d.EntityId); // Weather remains visible even when control is resisted/protected.
                    // Control protection never negates damage. Derived damage cannot produce another shatter.
                    DamageDefender(d,(60+d.MaxHealth*.02f)*power,CombatDamageType.Arts,derived:true);
                    if(!d.Alive)continue;
                    if(d.FrozenUntil>Clock||stormFreezeProtection.TryGetValue(d.EntityId,out double until)&&Clock<until)continue;
                    bool cold=d.ColdUntil>Clock;
                    if(!TryApplyCold(d.EntityId,3.5f))continue;
                    // Only the extra storm respects this additional protection; it does not rewrite character skills.
                    if(cold&&d.FrozenUntil>Clock)stormFreezeProtection[d.EntityId]=d.FrozenUntil+3;
                }
            }
        }
    }
}
