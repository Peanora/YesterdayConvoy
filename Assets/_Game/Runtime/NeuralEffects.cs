using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class CreeperPulse
    {
        public string SourceId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public double Time {get;internal set;}
        public int TargetsHit {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        readonly List<CreeperPulse> creeperPulses=new List<CreeperPulse>();
        public IReadOnlyList<CreeperPulse> CreeperPulses=>creeperPulses.AsReadOnly();
        public int NeuralBursts {get{int count=0;foreach(var d in defenders)count+=d.NeuralBursts;return count;}}
        void ApplyNeuralDamage(DefenderState d,float amount)
        {
            if(!d.Alive||amount<=0||d.NeuralUntil>Clock)return;
            d.NeuralAccumulated+=amount;
            if(d.NeuralAccumulated<100)return;
            d.NeuralAccumulated=0;d.NeuralUntil=Clock+10;d.LastNeuralBurstAt=Clock;d.NeuralBursts++;
            d.BlockedCount=0;TrueDamage(d,100);
        }
        void StepCreeperDamage(DeployedUnitState u)
        {
            float ratio=u.Health/u.MaxHealth;
            if(ratio+1e-6f>=u.NextRetaliationRatio)return;
            // A single damage event can cross several thresholds but only produces one stab.
            // Healing never resets the highest crossed stage; lethal damage never reaches this branch.
            u.NextRetaliationRatio=1-(Mathf.FloorToInt((1-ratio)/.15f)+1)*.15f;
            if(u.PillarStunnedUntil>Clock)return;
            u.CreeperRetaliations++;u.LastRetaliationAt=Clock;u.IsMoving=false;
            var pulse=new CreeperPulse{SourceId=u.EntityId,Position=u.Motion.Position,Time=Clock};creeperPulses.Add(pulse);
            foreach(var d in defenders)
            {
                if(!d.Alive||GroundDistance(d.Position,pulse.Position)>2)continue;
                pulse.TargetsHit++;DamageDefender(d,CombatSettings.UnitAttack,CombatDamageType.Arts);
                ApplyNeuralDamage(d,CombatSettings.UnitAttack);
            }
        }
    }
}
