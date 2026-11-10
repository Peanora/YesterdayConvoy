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
        float ApplyNeuralDamage(DefenderState d,float amount,DeployedUnitState source=null)
            =>ApplyNeuralDamageAttributed(d,amount,source,source);
        float ApplyNeuralDamageAttributed(DefenderState d,float amount,DeployedUnitState source,DeployedUnitState creditSource)
        {
            if(!d.Alive||amount<=0||d.NeuralUntil>Clock)return 0;
            if(source!=null)OnMarineContact(source);
            float multiplier=source==null?1:MarineNeuralMultiplier(source);
            float applied=Mathf.Min(amount*multiplier,Mathf.Max(0,100-d.NeuralAccumulated));
            MarineBonusNeuralDealt+=Mathf.Max(0,applied-amount);
            d.NeuralAccumulated+=applied;
            if(d.NeuralAccumulated<100-.00001f)return applied;
            d.NeuralAccumulated=0;d.NeuralUntil=Clock+10;d.LastNeuralBurstAt=Clock;d.NeuralBursts++;
            d.BlockedCount=0;SourcedTrueDamage(d,100,creditSource);
            return applied;
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
                pulse.TargetsHit++;DamageDefender(d,UnitCombat(u).UnitAttack,CombatDamageType.Arts,source:u);
                ApplyNeuralDamage(d,UnitCombat(u).UnitAttack,u);
            }
        }
    }
}
