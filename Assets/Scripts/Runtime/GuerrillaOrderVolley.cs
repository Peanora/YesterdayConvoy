using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>One defender's actual HP loss from the complete military-order volley.</summary>
    public sealed class GuerrillaOrderHit
    {
        public string TargetId {get;}
        public Vector3 WorldPosition {get;}
        public float Damage {get;}
        internal GuerrillaOrderHit(string targetId,Vector3 position,float damage)
        {TargetId=targetId;WorldPosition=position;Damage=damage;}
    }

    /// <summary>A committed order. Its cue is advanced by the model's presentation delta, never by rendering.</summary>
    public sealed class GuerrillaOrderVolley
    {
        public const float ImpactDelay=.9f;
        public int RunId {get;}
        public int Sequence {get;}
        public double StartedAt {get;}
        public float Age {get;internal set;}
        public bool Resolved {get;internal set;}
        public bool Cancelled {get;internal set;}
        public IReadOnlyList<string> SourceIds {get;}
        public IReadOnlyList<GuerrillaOrderHit> Hits=>visibleHits;
        readonly List<GuerrillaOrderHit> hits=new List<GuerrillaOrderHit>();
        readonly ReadOnlyCollection<GuerrillaOrderHit> visibleHits;
        internal readonly Contribution[] Contributions;
        internal readonly struct Contribution
        {
            internal readonly string SourceId;
            internal readonly float Attack;
            internal Contribution(string id,float attack){SourceId=id;Attack=attack;}
        }
        internal GuerrillaOrderVolley(int run,int sequence,double at,IEnumerable<Contribution> contributions)
        {
            RunId=run;Sequence=sequence;StartedAt=at;Contributions=contributions.ToArray();
            SourceIds=System.Array.AsReadOnly(Contributions.Select(p=>p.SourceId).ToArray());visibleHits=hits.AsReadOnly();
        }
        internal void RecordHit(DefenderState target,float before)
        {hits.Add(new GuerrillaOrderHit(target.EntityId,target.Position,Mathf.Max(0,before-target.Health)));}
    }

    public sealed partial class ManualBattle
    {
        readonly List<GuerrillaOrderVolley> guerrillaOrders=new List<GuerrillaOrderVolley>();
        public GuerrillaOrderVolley LatestGuerrillaOrder {get;private set;}
        void ResetGuerrillaOrders(){CancelGuerrillaOrders();guerrillaOrders.Clear();LatestGuerrillaOrder=null;}
        void CancelGuerrillaOrders()
        {
            foreach(var order in guerrillaOrders)if(!order.Resolved){order.Cancelled=true;order.Resolved=true;}
        }
        void CommitGuerrillaOrder(DeployedUnitState[] participants,float power)
        {
            var budget=participants.Select(u=>new GuerrillaOrderVolley.Contribution(u.EntityId,UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*power));
            var order=new GuerrillaOrderVolley(RunId,guerrillaOrders.Count+1,Clock,budget);
            guerrillaOrders.Add(order);LatestGuerrillaOrder=order;
        }
        void AdvanceGuerrillaOrders(float presentationDelta)
        {
            if(Phase!=BattlePhase.Running||IsPaused)return;
            foreach(var order in guerrillaOrders)
            {
                if(order.Resolved)continue;
                order.Age=Mathf.Min(GuerrillaOrderVolley.ImpactDelay,order.Age+presentationDelta);
                if(order.Age+.000001f<GuerrillaOrderVolley.ImpactDelay)continue;
                // Resolve once, even if a hit removes the final combat participant.
                order.Resolved=true;
                var sources=order.Contributions.Where(p=>
                {
                    var unit=FindUnit(p.SourceId);
                    return unit!=null&&unit.Active&&!unit.IsReviving&&!StrongholdInTransit(unit.EntityId);
                }).ToArray();
                if(sources.Length==0){order.Cancelled=true;continue;}
                // Targets are snapshotted at impact. High ground, distance and other lanes
                // do not exempt a living defender from this explicitly global attack.
                var targets=defenders.Where(d=>d.Alive).ToArray();
                var health=targets.Select(d=>d.Health).ToArray();
                foreach(var target in targets)foreach(var contribution in sources)
                {
                    if(!target.Alive)break;
                    var source=FindUnit(contribution.SourceId);var combat=UnitCombat(source);
                    float attack=contribution.Attack*(target.FrozenUntil>Clock?Mathf.Max(1,combat.Effects.FrozenAttackMultiplier):1);
                    DamageDefender(target,attack,source.AltarEmpowered?CombatDamageType.Arts:combat.Effects.DamageType,true,null);
                }
                // Snapshotting all HP before the volley also records real redirected losses.
                for(int i=0;i<targets.Length;i++)order.RecordHit(targets[i],health[i]);
            }
        }
    }
}
