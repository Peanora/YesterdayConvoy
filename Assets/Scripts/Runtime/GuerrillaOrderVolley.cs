using System;
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

    /// <summary>One completed global strike, with immutable records of its actual HP losses.</summary>
    public sealed class GuerrillaOrderStrike
    {
        public int Index {get;}
        public float At {get;}
        public float Weight {get;}
        public IReadOnlyList<GuerrillaOrderHit> Hits {get;}
        internal GuerrillaOrderStrike(int index,IEnumerable<GuerrillaOrderHit> hits)
        {Index=index;At=GuerrillaOrderVolley.StrikeTime(index);Weight=GuerrillaOrderVolley.StrikeWeight(index);Hits=Array.AsReadOnly(hits.ToArray());}
    }

    /// <summary>A committed order. Its cues advance in presentation time, never by rendering.</summary>
    public sealed class GuerrillaOrderVolley
    {
        public const float ImpactDelay=.9f;
        public const float PresentationDuration=3.55f;
        public const int StrikeCount=5;
        public static float StrikeTime(int index)
        {
            switch(index)
            {case 0:return ImpactDelay;case 1:return 1.2f;case 2:return 1.5f;case 3:return 1.8f;case 4:return 2.8f;default:throw new ArgumentOutOfRangeException(nameof(index));}
        }
        public static float StrikeWeight(int index)
        {if(index<0||index>=StrikeCount)throw new ArgumentOutOfRangeException(nameof(index));return index==4?.4f:.15f;}
        public int RunId {get;}
        public int Sequence {get;}
        public double StartedAt {get;}
        // Keep the accumulated clock in double precision. Re-rounding every .05-second
        // combat substep to float can otherwise delay the exact 2.8-second final cue.
        double elapsed;
        public float Age=>(float)elapsed;
        public bool Resolved {get;internal set;}
        public bool Cancelled {get;internal set;}
        public IReadOnlyList<string> SourceIds {get;}
        public IReadOnlyList<GuerrillaOrderHit> Hits=>visibleHits;
        public int CompletedStrikes=>strikes.Count;
        public IReadOnlyList<GuerrillaOrderStrike> Strikes=>visibleStrikes;
        readonly List<GuerrillaOrderHit> hits=new List<GuerrillaOrderHit>();
        readonly List<GuerrillaOrderStrike> strikes=new List<GuerrillaOrderStrike>();
        readonly ReadOnlyCollection<GuerrillaOrderHit> visibleHits;
        readonly ReadOnlyCollection<GuerrillaOrderStrike> visibleStrikes;
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
            SourceIds=Array.AsReadOnly(Contributions.Select(p=>p.SourceId).ToArray());visibleHits=hits.AsReadOnly();visibleStrikes=strikes.AsReadOnly();
        }
        internal void AdvanceAge(float delta)=>elapsed=Math.Min(PresentationDuration,elapsed+delta);
        internal void RecordStrike(DefenderState[] targets,float[] before)
        {
            var losses=new List<GuerrillaOrderHit>(targets.Length);
            for(int i=0;i<targets.Length;i++)
            {
                var target=targets[i];float damage=Mathf.Max(0,before[i]-target.Health);
                losses.Add(new GuerrillaOrderHit(target.EntityId,target.Position,damage));
                int previous=hits.FindIndex(h=>h.TargetId==target.EntityId);
                if(previous<0)hits.Add(new GuerrillaOrderHit(target.EntityId,target.Position,damage));
                else hits[previous]=new GuerrillaOrderHit(target.EntityId,target.Position,hits[previous].Damage+damage);
            }
            strikes.Add(new GuerrillaOrderStrike(CompletedStrikes,losses));
        }
    }

    public sealed partial class ManualBattle
    {
        readonly List<GuerrillaOrderVolley> guerrillaOrders=new List<GuerrillaOrderVolley>();
        public GuerrillaOrderVolley LatestGuerrillaOrder {get;private set;}
        // The final strike resolves before its recoil/light tail finishes. Both manual and
        // automatic transactions respect that tail, including a cancelled batch's visuals.
        public bool GuerrillaOrderBusy=>LatestGuerrillaOrder!=null&&LatestGuerrillaOrder.Age<GuerrillaOrderVolley.PresentationDuration;
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
                order.AdvanceAge(presentationDelta);
                if(order.Resolved)continue;
                while(!order.Resolved&&order.CompletedStrikes<GuerrillaOrderVolley.StrikeCount&&
                    order.Age+.000001f>=GuerrillaOrderVolley.StrikeTime(order.CompletedStrikes))
                    ResolveGuerrillaStrike(order);
            }
        }
        void ResolveGuerrillaStrike(GuerrillaOrderVolley order)
        {
            var sources=order.Contributions.Where(p=>
            {
                var unit=FindUnit(p.SourceId);
                return unit!=null&&unit.Active&&!unit.IsReviving&&!StrongholdInTransit(unit.EntityId);
            }).ToArray();
            if(sources.Length==0){order.Cancelled=order.Resolved=true;return;}
            // Targets are snapshotted for each strike. High ground, distance and other lanes
            // do not exempt a living defender from this explicitly global attack.
            var targets=defenders.Where(d=>d.Alive).ToArray();
            if(targets.Length==0){order.Cancelled=order.Resolved=true;return;}
            var health=targets.Select(d=>d.Health).ToArray();
            float weight=GuerrillaOrderVolley.StrikeWeight(order.CompletedStrikes);
            foreach(var target in targets)foreach(var contribution in sources)
            {
                if(!target.Alive)break;
                var source=FindUnit(contribution.SourceId);var combat=UnitCombat(source);
                float attack=contribution.Attack*(target.FrozenUntil>Clock?Mathf.Max(1,combat.Effects.FrozenAttackMultiplier):1);
                // Split damage after armor/resistance and the original minimum hit. Scaling
                // Attack instead would charge armor five times and change the total budget.
                DamageDefenderFrom(target,attack,source.AltarEmpowered?CombatDamageType.Arts:combat.Effects.DamageType,source,true,weight);
            }
            // Snapshotting all HP before the volley also records real redirected losses.
            order.RecordStrike(targets,health);
            order.Resolved=order.CompletedStrikes==GuerrillaOrderVolley.StrikeCount;
        }
    }
}
