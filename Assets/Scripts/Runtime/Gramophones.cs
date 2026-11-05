using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum GramophoneOwner { Neutral, Monsters, Defenders }
    public sealed class GramophoneSettings
    {
        public string Id {get;}
        public Vector3 Position {get;}
        public float InitialCharge {get;}
        public float Range {get;}
        public GramophoneSettings(string id,Vector3 position,float initialCharge=50,float range=3)
        {
            if(string.IsNullOrWhiteSpace(id)||!Finite(position.x)||!Finite(position.y)||!Finite(position.z)||!Finite(initialCharge)||initialCharge<0||initialCharge>100||!Finite(range)||range<=0)
                throw new ArgumentException("Invalid gramophone configuration.");
            Id=id;Position=position;InitialCharge=initialCharge;Range=range;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    }
    public sealed class GramophoneState
    {
        public GramophoneSettings Settings {get;}
        public GramophoneOwner Owner {get;internal set;}
        public float Charge {get;internal set;}
        public double ChangedAt {get;internal set;}=double.NegativeInfinity;
        public int Captures {get;internal set;}
        internal double NextAttack,NextHeal;
        internal GramophoneState(GramophoneSettings spec){Settings=spec;Charge=spec.InitialCharge;Owner=Charge<=0?GramophoneOwner.Monsters:Charge>=100?GramophoneOwner.Defenders:GramophoneOwner.Neutral;}
    }
    public sealed class MudrockCaptureCast
    {
        public string SourceId {get;internal set;}
        public string TargetId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public double StartedAt {get;internal set;}
        public double ImpactAt=>StartedAt+.8666667;
        public bool Resolved {get;internal set;}
        public bool Cancelled {get;internal set;}
    }
    public sealed class GramophoneAction
    {
        public string DeviceId {get;internal set;}
        public string TargetId {get;internal set;}
        public Vector3 From {get;internal set;}
        public Vector3 To {get;internal set;}
        public GramophoneOwner Owner {get;internal set;}
        public bool Healing {get;internal set;}
        public double StartedAt {get;internal set;}
        public bool Resolved {get;internal set;}
        public bool Applied {get;internal set;}
        public float Amount {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        public const float MudrockRefreshDuration=1.3333334f;
        readonly List<GramophoneSettings> gramophoneSpecs=new List<GramophoneSettings>();
        readonly List<GramophoneState> gramophones=new List<GramophoneState>();
        readonly List<MudrockCaptureCast> mudrockCaptures=new List<MudrockCaptureCast>();
        readonly List<GramophoneAction> gramophoneActions=new List<GramophoneAction>();
        public IReadOnlyList<GramophoneState> Gramophones=>gramophones.AsReadOnly();
        public IReadOnlyList<MudrockCaptureCast> MudrockCaptures=>mudrockCaptures.AsReadOnly();
        public IReadOnlyList<GramophoneAction> GramophoneActions=>gramophoneActions.AsReadOnly();
        public void ConfigureGramophones(IEnumerable<GramophoneSettings> configuration)
        {
            if(Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Configure devices before battle.");
            if(configuration==null)throw new ArgumentNullException(nameof(configuration));
            var copy=configuration.ToArray();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var spec in copy)if(spec==null||!ids.Add(spec.Id))throw new ArgumentException("Duplicate or null gramophone.");
            gramophoneSpecs.Clear();gramophoneSpecs.AddRange(copy);ResetGramophones();
        }
        void ResetGramophones()
        {gramophones.Clear();foreach(var spec in gramophoneSpecs)gramophones.Add(new GramophoneState(spec));mudrockCaptures.Clear();gramophoneActions.Clear();}
        GramophoneState ChooseGramophone(DeployedUnitState source)=>gramophones.Where(g=>g.Owner!=GramophoneOwner.Monsters)
            .OrderBy(g=>GroundDistance(source.Motion.Position,g.Settings.Position)).ThenBy(g=>g.Settings.Id,StringComparer.Ordinal).FirstOrDefault();
        void StartMudrockCapture(DeployedUnitState unit)
        {
            var target=ChooseGramophone(unit);
            unit.MudrockCaptureStartedAt=Clock;unit.IsMoving=unit.HasCombatTarget=false;unit.NextAttackTime=Math.Max(unit.NextAttackTime,Clock+1.5);
            mudrockCaptures.Add(new MudrockCaptureCast{SourceId=unit.EntityId,TargetId=target.Settings.Id,Position=unit.Motion.Position,StartedAt=Clock});
        }
        void ChangeGramophone(GramophoneState g,GramophoneOwner owner)
        {
            if(g.Owner==owner)return;
            g.Owner=owner;g.ChangedAt=Clock;g.Captures++;g.NextAttack=Clock+.966667;g.NextHeal=Clock+5;
        }
        void StepGramophones(float step)
        {
            foreach(var cast in mudrockCaptures)
            {
                if(cast.Resolved)continue;
                var source=FindUnit(cast.SourceId);
                if(source==null||!source.Active||source.IsReviving){cast.Resolved=cast.Cancelled=true;continue;}
                if(Clock+1e-6<cast.ImpactAt)continue;
                cast.Resolved=true;var g=gramophones.Find(x=>x.Settings.Id==cast.TargetId);
                if(g!=null){g.Charge=0;ChangeGramophone(g,GramophoneOwner.Monsters);}
            }
            // Original faction mapping: nearby defending operators recharge their control.
            // Ordinary attacking monsters do not receive an invented proximity-capture ability.
            foreach(var g in gramophones)
            {
                int adjacent=defenders.Count(d=>d.Alive&&Mathf.Abs(d.Position.x-g.Settings.Position.x)+Mathf.Abs(d.Position.z-g.Settings.Position.z)<=1.05f);
                g.Charge=Mathf.Min(100,g.Charge+adjacent*step);
                if(g.Charge>=100)ChangeGramophone(g,GramophoneOwner.Defenders);
            }
            foreach(var action in gramophoneActions)
            {
                if(action.Resolved||Clock+1e-6<action.StartedAt+.55)continue;
                action.Resolved=true;var g=gramophones.Find(x=>x.Settings.Id==action.DeviceId);
                if(g==null||g.Owner!=action.Owner)continue;
                bool unitTarget=(action.Owner==GramophoneOwner.Monsters)==action.Healing;
                if(unitTarget)
                {
                    var target=FindUnit(action.TargetId);if(target==null||!target.Active||target.IsReviving)continue;
                    if(action.Healing){action.Amount=HealUnit(target,target.MaxHealth*.1f);action.Applied=action.Amount>0;}
                    else {action.Amount=target.MaxHealth*.05f;action.Applied=TryDamageUnit(target.EntityId,action.Amount,CombatDamageType.Arts);}
                }
                else
                {
                    var target=FindDefender(action.TargetId);if(target==null||!target.Alive)continue;
                    float before=target.Health;
                    if(action.Healing){RestoreDefenderHealth(target,target.MaxHealth*.1f);}else DamageDefender(target,target.MaxHealth*.05f,CombatDamageType.Arts);
                    action.Amount=Mathf.Abs(target.Health-before);action.Applied=action.Amount>0;
                }
            }
            foreach(var g in gramophones)
            {
                if(g.Owner==GramophoneOwner.Neutral)continue;
                if(Clock+1e-6>=g.NextAttack&&ScheduleGramophone(g,false))g.NextAttack=Clock+1.5;
                if(Clock+1e-6>=g.NextHeal&&ScheduleGramophone(g,true))g.NextHeal=Clock+5;
            }
        }
        bool ScheduleGramophone(GramophoneState g,bool healing)
        {
            bool unitTarget=(g.Owner==GramophoneOwner.Monsters)==healing;string id;Vector3 point;
            if(unitTarget)
            {
                var target=units.Where(u=>u.Active&&!u.IsReviving&&!u.FuelDying&&!StrongholdInTransit(u.EntityId)&&GroundDistance(u.Motion.Position,g.Settings.Position)<=g.Settings.Range&&(!healing||u.Health<u.MaxHealth))
                    .OrderBy(u=>healing?u.Health/u.MaxHealth:-u.MaxHealth).ThenBy(u=>u.EntityId,StringComparer.Ordinal).FirstOrDefault();
                if(target==null)return false;id=target.EntityId;point=target.Motion.Position;
            }
            else
            {
                var target=defenders.Where(d=>d.Alive&&GroundDistance(d.Position,g.Settings.Position)<=g.Settings.Range&&(!healing||d.Health<d.MaxHealth))
                    .OrderBy(d=>healing?d.Health/d.MaxHealth:-d.MaxHealth).ThenBy(d=>d.EntityId,StringComparer.Ordinal).FirstOrDefault();
                if(target==null)return false;id=target.EntityId;point=target.Position;
            }
            gramophoneActions.Add(new GramophoneAction{DeviceId=g.Settings.Id,TargetId=id,From=g.Settings.Position,To=point,Owner=g.Owner,Healing=healing,StartedAt=Clock});return true;
        }
    }
}
