using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class CorrosiveBubble
    {
        public string SourceId {get;internal set;}
        public Vector3 Origin {get;internal set;}
        public Vector3 Anchor {get;internal set;}
        public double StartedAt {get;internal set;}
        public int Pulses {get;internal set;}
        public float Attack {get;internal set;}
        readonly List<string> targets=new List<string>();
        public IReadOnlyList<string> TargetIds=>targets.AsReadOnly();
        internal void Record(string id)=>targets.Add(id);
        public string LastTargetId {get;internal set;}
        public double LastPulseAt {get;internal set;}=double.NegativeInfinity;
    }
    public sealed partial class ManualBattle
    {
        readonly List<CorrosiveBubble> bubbles=new List<CorrosiveBubble>();
        public IReadOnlyList<CorrosiveBubble> CorrosiveBubbles=>bubbles.AsReadOnly();
        public bool FirstSpeakerCasting(DeployedUnitState u)=>EngagementEnabled&&CombatSettings.Effects.FirstSpeaker&&u!=null&&u.Active&&Clock<u.BubbleStartedAt+5;

        bool StepFirstSpeaker(DeployedUnitState u)
        {
            if(FirstSpeakerCasting(u)){u.IsMoving=u.HasCombatTarget=false;return true;}
            if(Clock+1e-6<Math.Max(u.SpawnTime+10,u.NextBubbleAt))return false;
            var target=ChooseDefender(u,CombatSettings.UnitRange);if(target==null)return false;
            u.BubbleStartedAt=Clock;u.NextBubbleAt=Clock+25;u.NextAttackTime=Clock+5;
            u.IsMoving=u.HasCombatTarget=false;
            bubbles.Add(new CorrosiveBubble{SourceId=u.EntityId,Origin=u.Motion.Position,Anchor=target.Position,
                StartedAt=Clock,Attack=CombatSettings.UnitAttack*UnitAttackMultiplier(u)*.6f});
            return true;
        }
        static bool InBubbleCross(Vector3 position,Vector3 anchor)
        {
            // The target cell plus its four cardinal neighbours, not a four-tile circle.
            int x=Mathf.RoundToInt(position.x)-Mathf.RoundToInt(anchor.x);
            int z=Mathf.RoundToInt(position.z)-Mathf.RoundToInt(anchor.z);
            return Math.Abs(x)+Math.Abs(z)<=1;
        }
        void ResolveCorrosiveBubbles()
        {
            foreach(var bubble in bubbles)
            {
                while(bubble.Pulses<5&&Clock+1e-6>=bubble.StartedAt+3+bubble.Pulses*.5)
                {
                    bubble.Pulses++;DefenderState target=null;float least=float.PositiveInfinity;
                    foreach(var d in defenders)
                    {
                        if(!d.Alive||DefenderCamouflaged(d)||!InBubbleCross(d.Position,bubble.Anchor))continue;
                        float injury=d.NeuralUntil>Clock?float.PositiveInfinity:d.NeuralAccumulated;
                        if(target==null||injury<least){target=d;least=injury;}
                    }
                    if(target==null)continue;
                    bubble.LastTargetId=target.EntityId;bubble.LastPulseAt=Clock;bubble.Record(target.EntityId);
                    DamageDefender(target,bubble.Attack,CombatDamageType.Physical);
                    ApplyNeuralDamage(target,bubble.Attack);
                }
            }
        }
    }
}
