using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum CombatFeedbackTarget { Monster, Defender }
    public enum CombatFeedbackKind { HealthLost, HealthRestored, ShieldAbsorbed, StatusApplied }
    public enum CombatFeedbackStatus { None, Cold, Frozen, Hindered, Fear, Stunned, Silenced, Burning, Neural, Vulnerable, Shackled, Lured, Shielded }

    /// <summary>An immutable applied result, never a proposed attack or a frame-to-frame HP estimate.</summary>
    public readonly struct CombatFeedbackEvent
    {
        public readonly int RunId;
        public readonly long Sequence;
        public readonly double At;
        public readonly CombatFeedbackTarget TargetType;
        public readonly string TargetId;
        public readonly Vector3 WorldPosition;
        public readonly CombatFeedbackKind Kind;
        public readonly float Amount;
        public readonly CombatFeedbackStatus Status;
        public CombatFeedbackEvent(int runId,long sequence,double at,CombatFeedbackTarget targetType,string targetId,
            Vector3 position,CombatFeedbackKind kind,float amount,CombatFeedbackStatus status=CombatFeedbackStatus.None)
        {RunId=runId;Sequence=sequence;At=at;TargetType=targetType;TargetId=targetId;WorldPosition=position;Kind=kind;Amount=amount;Status=status;}
    }

    public readonly struct CombatStatusState
    {
        public readonly CombatFeedbackTarget TargetType;
        public readonly string TargetId;
        public readonly Vector3 WorldPosition;
        public readonly CombatFeedbackStatus Status;
        public readonly double Until;
        internal CombatStatusState(CombatFeedbackTarget type,string id,Vector3 position,CombatFeedbackStatus status,double until)
        {TargetType=type;TargetId=id;WorldPosition=position;Status=status;Until=until;}
    }

    /// <summary>Bounded, ordered and read-only to consumers. One journal belongs to one model, not a global singleton.</summary>
    internal sealed class CombatFeedbackJournal
    {
        internal const int Capacity=256;
        readonly List<CombatFeedbackEvent> events=new List<CombatFeedbackEvent>(Capacity);
        readonly ReadOnlyCollection<CombatFeedbackEvent> visible;
        int run;
        long sequence;
        internal CombatFeedbackJournal(){visible=events.AsReadOnly();}
        internal IReadOnlyList<CombatFeedbackEvent> Events=>visible;
        internal void Reset(int runId){run=runId;sequence=0;events.Clear();}
        internal void Add(double at,CombatFeedbackTarget type,string id,Vector3 position,CombatFeedbackKind kind,float amount,CombatFeedbackStatus status=CombatFeedbackStatus.None)
        {
            if(string.IsNullOrEmpty(id)||double.IsNaN(at)||double.IsInfinity(at))return;
            if(kind!=CombatFeedbackKind.StatusApplied&&(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0))return;
            if(events.Count==Capacity)events.RemoveAt(0);
            events.Add(new CombatFeedbackEvent(run,++sequence,at,type,id,position,kind,amount,status));
        }
    }

    public sealed partial class ManualBattle
    {
        readonly CombatFeedbackJournal combatFeedback=new CombatFeedbackJournal();
        readonly List<CombatStatusState> feedbackStatuses=new List<CombatStatusState>();
        readonly Dictionary<string,double> feedbackStatusUntil=new Dictionary<string,double>();
        readonly HashSet<string> feedbackStatusSeen=new HashSet<string>();
        readonly List<string> feedbackStatusRemoved=new List<string>();
        ReadOnlyCollection<CombatStatusState> visibleFeedbackStatuses;
        public IReadOnlyList<CombatFeedbackEvent> CombatFeedback=>combatFeedback.Events;
        public IReadOnlyList<CombatStatusState> CombatStatuses=>visibleFeedbackStatuses??(visibleFeedbackStatuses=feedbackStatuses.AsReadOnly());
        void ResetCombatFeedback()
        {combatFeedback.Reset(RunId);feedbackStatuses.Clear();feedbackStatusUntil.Clear();feedbackStatusSeen.Clear();feedbackStatusRemoved.Clear();}
        void RecordUnitHealthLoss(DeployedUnitState u,float before,Vector3 position)
            =>combatFeedback.Add(Clock,CombatFeedbackTarget.Monster,u.EntityId,position,CombatFeedbackKind.HealthLost,Mathf.Max(0,before-u.Health));
        void RecordDefenderHealthLoss(DefenderState d,float before,Vector3 position)
            =>combatFeedback.Add(Clock,CombatFeedbackTarget.Defender,d.EntityId,position,CombatFeedbackKind.HealthLost,Mathf.Max(0,before-d.Health));
        void RecordUnitShieldAbsorption(DeployedUnitState u,float amount)
            =>combatFeedback.Add(Clock,CombatFeedbackTarget.Monster,u.EntityId,u.Motion.Position,CombatFeedbackKind.ShieldAbsorbed,amount);
        // Centralizes ONLY actual defender healing; keeps caller-specific SP/statistics untouched.
        float RestoreDefenderHealth(DefenderState d,float amount)
        {
            if(d==null||!d.Alive||d.DebtLife||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return 0;
            float gained=Mathf.Min(amount,Mathf.Max(0,d.MaxHealth-d.Health));
            if(gained<=0)return 0;
            d.Health+=gained;
            combatFeedback.Add(Clock,CombatFeedbackTarget.Defender,d.EntityId,d.Position,CombatFeedbackKind.HealthRestored,gained);
            return gained;
        }
        void AddFeedbackStatus(CombatFeedbackTarget type,string id,Vector3 position,CombatFeedbackStatus status,double until)
        {
            if(until<=Clock)return;
            string key=((int)type)+"/"+id+"/"+(int)status;
            feedbackStatuses.Add(new CombatStatusState(type,id,position,status,until));feedbackStatusSeen.Add(key);
            // Refreshing an already-active duration is not another application popup.
            if(!feedbackStatusUntil.TryGetValue(key,out double previous)||previous<=Clock)
                combatFeedback.Add(Clock,type,id,position,CombatFeedbackKind.StatusApplied,0,status);
            feedbackStatusUntil[key]=until;
        }
        // Called once per bounded model step, and immediately by the direct Cold API.
        // Reads real timers and immunity. No presentation code creates or prolongs a status.
        void RefreshCombatFeedbackStatuses()
        {
            feedbackStatuses.Clear();feedbackStatusSeen.Clear();
            foreach(var u in units)
            {
                if(!u.Active||u.IsReviving||TalulahTransitionLocked(u)||StrongholdInTransit(u.EntityId))continue;
                var p=u.Motion.Position;bool immune=UnitControlImmune(u);
                if(!immune)
                {
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Hindered,u.HinderedUntil);
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Fear,u.FearUntil);
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Lured,u.LureUntil);
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Stunned,Math.Max(u.PillarStunnedUntil,Math.Max(u.ShackleStunnedUntil,u.SprayStunnedUntil)));
                }
                AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Silenced,Math.Max(u.ShackleSilencedUntil,u.SpraySilencedUntil));
                AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Burning,u.ScorchUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Vulnerable,u.VulnerableUntil);
                if(ContractShieldRemaining(u)>.00001f)
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Shielded,contractWards[u.EntityId].Until);
                else if(u.SynergyShield>.00001f&&u.SynergyShieldUntil>Clock)
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Shielded,u.SynergyShieldUntil);
                else if(u.ArtsBarrierRemaining>.00001f)
                    AddFeedbackStatus(CombatFeedbackTarget.Monster,u.EntityId,p,CombatFeedbackStatus.Shielded,double.PositiveInfinity);
            }
            foreach(var d in defenders)
            {
                if(!d.Alive)continue;var p=d.Position;
                if(d.FrozenUntil>Clock)AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Frozen,d.FrozenUntil);
                else AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Cold,d.ColdUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Hindered,d.FrostSlowUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Neural,d.NeuralUntil);
                AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Burning,Math.Max(d.BurnUntil,Clock+DeepBurnRemaining(d.EntityId)));
                if(DefenderShackled(d))
                {
                    double until=Clock;
                    foreach(var s in shackles)if(s.TargetId==d.EntityId&&ShackleActive(s))until=Math.Max(until,s.StartedAt+20);
                    AddFeedbackStatus(CombatFeedbackTarget.Defender,d.EntityId,p,CombatFeedbackStatus.Shackled,until);
                }
            }
            feedbackStatusRemoved.Clear();
            foreach(var pair in feedbackStatusUntil)if(!feedbackStatusSeen.Contains(pair.Key))feedbackStatusRemoved.Add(pair.Key);
            foreach(string key in feedbackStatusRemoved)feedbackStatusUntil.Remove(key);
        }
    }
}
