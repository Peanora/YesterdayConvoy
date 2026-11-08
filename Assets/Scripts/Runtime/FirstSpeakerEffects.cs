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
        internal float Neural,FirstDelay=3,Interval=.5f;
        internal int MaximumPulses=5;
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
        public bool FirstSpeakerCasting(DeployedUnitState u)=>EngagementEnabled&&UnitCombat(u).Effects.FirstSpeaker&&u!=null&&u.Active&&Clock<u.BubbleStartedAt+V2Skill(u,"bubble","actionDuration",5);

        bool StepFirstSpeaker(DeployedUnitState u)
        {
            if(FirstSpeakerCasting(u)){u.IsMoving=u.HasCombatTarget=false;return true;}
            if(Clock+1e-6<Math.Max(u.SpawnTime+V2Skill(u,"bubble","initialCooldown",10),u.NextBubbleAt))return false;
            var target=ChooseDefender(u,UnitCombat(u).UnitRange);if(target==null)return false;
            u.BubbleStartedAt=Clock;u.NextBubbleAt=Clock+V2Skill(u,"bubble","startToStartCooldown",25);u.NextAttackTime=Clock+V2Skill(u,"bubble","actionDuration",5);
            u.IsMoving=u.HasCombatTarget=false;
            bubbles.Add(new CorrosiveBubble{SourceId=u.EntityId,Origin=u.Motion.Position,Anchor=target.Position,
                StartedAt=Clock,Attack=NativeSkillDamage(u,"bubble","damagePerPulse",UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*.6f),
                Neural=V2Skill(u,"bubble","neuralPerPulse",UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*.6f),
                FirstDelay=V2Skill(u,"bubble","firstPulseDelay",3),Interval=V2Skill(u,"bubble","pulseInterval",.5f),MaximumPulses=(int)V2Skill(u,"bubble","pulses",5)});
            return true;
        }
        static bool InBubbleCross(Vector3 position,Vector3 anchor)
        {
            // The target cell plus its four cardinal neighbours, not a four-tile circle.
            int x=Mathf.RoundToInt(position.x-anchor.x);
            int z=Mathf.RoundToInt(position.z-anchor.z);
            return Math.Abs(x)+Math.Abs(z)<=1;
        }
        void ResolveCorrosiveBubbles()
        {
            foreach(var bubble in bubbles)
            {
                while(bubble.Pulses<bubble.MaximumPulses&&Clock+1e-6>=bubble.StartedAt+bubble.FirstDelay+bubble.Pulses*bubble.Interval)
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
                    var source=FindUnit(bubble.SourceId);
                    DamageDefender(target,bubble.Attack,CombatDamageType.Physical,source:source);
                    ApplyNeuralDamage(target,bubble.Neural,source);
                }
            }
        }
    }
}
