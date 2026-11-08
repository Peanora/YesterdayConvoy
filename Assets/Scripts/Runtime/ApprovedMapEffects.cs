using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public sealed class ApprovedMapMechanics
    {
        public ApprovedPortal[] portals=Array.Empty<ApprovedPortal>();
        public ApprovedHeatPump[] heatPumps=Array.Empty<ApprovedHeatPump>();
    }

    [Serializable]
    public sealed class ApprovedPortal
    {
        public string routeId;
        public int sourcePointIndex,destinationPointIndex;
        public float transitSeconds=1.5f;
    }

    [Serializable]
    public sealed class ApprovedHeatPump
    {
        public Vector3 position;
        public float radius=.6f,period=10,warning=1.5f,active=2,dps=12,phase;
    }

    public sealed partial class ManualBattle
    {
        sealed class PortalSnapshot
        {
            internal string Route;
            internal int SourcePoint,DestinationPoint;
            internal double Source,Destination;
            internal float Duration;
        }
        readonly struct HeatSnapshot
        {
            internal readonly Vector3 Position;
            internal readonly float Radius,Period,Warning,Active,Dps,Phase;
            internal HeatSnapshot(ApprovedHeatPump p)
            {Position=p.position;Radius=p.radius;Period=p.period;Warning=p.warning;Active=p.active;Dps=p.dps;Phase=p.phase;}
        }
        sealed class PortalTransit
        {
            internal int Portal;
            internal double Until;
        }
        PortalSnapshot[] approvedPortals=Array.Empty<PortalSnapshot>();
        HeatSnapshot[] approvedHeatPumps=Array.Empty<HeatSnapshot>();
        readonly Dictionary<string,PortalTransit> approvedTransit=new Dictionary<string,PortalTransit>();
        readonly Dictionary<string,int> approvedPortalWaiting=new Dictionary<string,int>();
        readonly Dictionary<string,HashSet<int>> approvedPassed=new Dictionary<string,HashSet<int>>();
        public int ApprovedPortalTrips {get;private set;}
        public float ApprovedHeatDamageToUnits {get;private set;}
        public float ApprovedHeatDamageToDefenders {get;private set;}
        public int ApprovedHeatPumpCount=>approvedHeatPumps.Length;
        public Vector3 ApprovedHeatPosition(int index)=>approvedHeatPumps[index].Position;
        public float ApprovedHeatRadius(int index)=>approvedHeatPumps[index].Radius;
        public bool ApprovedInTransit(string unitId)=>unitId!=null&&approvedTransit.ContainsKey(unitId);
        public bool ApprovedWaitingAtPortal(string unitId)=>unitId!=null&&approvedPortalWaiting.ContainsKey(unitId);
        public float ApprovedTransitRemaining(string unitId)=>unitId!=null&&approvedTransit.TryGetValue(unitId,out var state)?(float)Math.Max(0,state.Until-Clock):0;

        // Segment i connects Points[i] to Points[i+1]. Underground connectors
        // are omitted from surface route feedback and from walkable terrain checks.
        public bool ApprovedPortalSegment(string routeId,int segmentIndex)
        {
            foreach(var p in approvedPortals)
                if(p.Route==routeId&&segmentIndex>=p.SourcePoint&&segmentIndex<p.DestinationPoint)return true;
            return false;
        }

        public void ConfigureApprovedMapEffects(ApprovedMapMechanics config)
        {
            if(Phase!=BattlePhase.Ready||units.Count!=0||!EngagementEnabled)
                throw new InvalidOperationException("Configure authored map effects before deployment.");
            if(config==null)throw new ArgumentNullException(nameof(config));
            if(config.portals==null||config.heatPumps==null)throw new ArgumentException("Map effect arrays cannot be null.");
            var portals=new PortalSnapshot[config.portals.Length];
            for(int i=0;i<portals.Length;i++)
            {
                var p=config.portals[i];
                if(p==null||string.IsNullOrWhiteSpace(p.routeId)||!ApprovedFinite(p.transitSeconds)||p.transitSeconds<=0)
                    throw new ArgumentException("Invalid authored portal.");
                DeploymentLane lane=null;foreach(var l in lanes)if(l.Id==p.routeId){lane=l;break;}
                if(lane==null||p.sourcePointIndex<0||p.destinationPointIndex<=p.sourcePointIndex||p.destinationPointIndex>=lane.Points.Count)
                    throw new ArgumentException("Portal endpoints must be ordered points on its existing route.");
                double source=0,destination=0;
                for(int n=1;n<=p.destinationPointIndex;n++)
                {destination+=Vector3.Distance(lane.Points[n-1],lane.Points[n]);if(n==p.sourcePointIndex)source=destination;}
                if(destination-source<=.000001)throw new ArgumentException("Portal endpoints must be distinct.");
                for(int n=0;n<i;n++)
                    if(portals[n].Route==p.routeId&&source<portals[n].Destination&&destination>portals[n].Source)
                        throw new ArgumentException("Portal spans cannot overlap on one route.");
                portals[i]=new PortalSnapshot{Route=p.routeId,SourcePoint=p.sourcePointIndex,DestinationPoint=p.destinationPointIndex,Source=source,Destination=destination,Duration=p.transitSeconds};
            }
            var pumps=new HeatSnapshot[config.heatPumps.Length];
            for(int i=0;i<pumps.Length;i++)
            {
                var p=config.heatPumps[i];if(p==null)throw new ArgumentException("Null heat pump.");
                foreach(float value in new[]{p.position.x,p.position.y,p.position.z,p.radius,p.period,p.warning,p.active,p.dps,p.phase})
                    if(!ApprovedFinite(value))throw new ArgumentException("Non-finite heat pump value.");
                if(p.radius<=0||p.period<=0||p.warning<0||p.active<=0||p.dps<=0||p.phase<0||(double)p.warning+p.active>p.period)
                    throw new ArgumentException("Invalid heat pump timing or damage.");
                pumps[i]=new HeatSnapshot(p);
            }
            // Commit only after validating every record. Runtime owns all snapshots.
            approvedPortals=portals;approvedHeatPumps=pumps;ResetApprovedMapEffects();
        }

        static bool ApprovedFinite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        void ResetApprovedMapEffects()
        {
            approvedTransit.Clear();approvedPortalWaiting.Clear();approvedPassed.Clear();ApprovedPortalTrips=0;
            ApprovedHeatDamageToUnits=ApprovedHeatDamageToDefenders=0;
        }

        // 0 idle, 1 warning, 2 active. The same simulation clock drives visuals
        // and the exact active-time overlap used for damage below.
        public float ApprovedHeatState(int index)
        {
            var p=approvedHeatPumps[index];double elapsed=Clock-p.Phase;if(elapsed<0)return 0;
            double age=elapsed-Math.Floor(elapsed/p.Period)*p.Period;
            return age<p.Warning?1:age<p.Warning+p.Active?2:0;
        }
        static double ApprovedHeatIntegral(HeatSnapshot p,double time)
        {
            double elapsed=time-p.Phase;if(elapsed<=0)return 0;
            double cycles=Math.Floor(elapsed/p.Period),age=elapsed-cycles*p.Period;
            return cycles*p.Active+Math.Min(p.Active,Math.Max(0,age-p.Warning));
        }
        void StepApprovedHeatPumps(float step)
        {
            foreach(var p in approvedHeatPumps)
            {
                float damage=(float)((ApprovedHeatIntegral(p,Clock)-ApprovedHeatIntegral(p,Math.Max(0,Clock-step)))*p.Dps);
                if(damage<=0)continue;
                foreach(var u in units)
                {
                    if(!u.Active||u.IsReviving||StrongholdInTransit(u.EntityId)||UnitInvulnerable(u)||GroundDistance(u.Motion.Position,p.Position)>p.Radius)continue;
                    float before=u.Health;TrueDamage(u,damage);ApprovedHeatDamageToUnits+=Math.Max(0,before-u.Health);
                }
                foreach(var d in defenders)
                {
                    if(!d.Alive||GroundDistance(d.Position,p.Position)>p.Radius)continue;
                    float before=d.Health;TrueDamage(d,damage);ApprovedHeatDamageToDefenders+=Math.Max(0,before-d.Health);
                }
            }
        }

        bool ApprovedPortalPassed(DeployedUnitState u,int index)=>approvedPassed.TryGetValue(u.EntityId,out var passed)&&passed.Contains(index);
        void HoldApprovedTransit(DeployedUnitState u)
        {
            if(u.IsBlocked&&u.BlockerId!=null)
            {var d=FindDefender(u.BlockerId);if(d!=null)d.BlockedCount=Math.Max(0,d.BlockedCount-1);}
            u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;
        }
        bool StepApprovedTransit(DeployedUnitState u)
        {
            if(!approvedTransit.TryGetValue(u.EntityId,out var state))return false;
            HoldApprovedTransit(u);
            if(Clock+1e-8<state.Until)return true;
            var p=approvedPortals[state.Portal];
            u.Motion.ApproachDistance(p.Destination,(float)(Math.Abs(p.Destination-u.Motion.DistanceTravelled)/u.Motion.Speed+.001));
            approvedTransit.Remove(u.EntityId);
            if(!approvedPassed.TryGetValue(u.EntityId,out var passed))approvedPassed[u.EntityId]=passed=new HashSet<int>();
            passed.Add(state.Portal);ApprovedPortalTrips++;
            // Exit consumes this simulation step; next step re-evaluates blockers,
            // targets and actions at the exit, with no travel-time catch-up jump.
            return true;
        }
        bool TryBeginApprovedTransit(DeployedUnitState u)
        {
            if(u.IsReviving||IsSkillActionPlaying(u)||Clock<u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate)return false;
            for(int i=0;i<approvedPortals.Length;i++)
            {
                var p=approvedPortals[i];
                if(p.Route!=u.RouteId||ApprovedPortalPassed(u,i)||Math.Abs(u.Motion.DistanceTravelled-p.Source)>.00001)continue;
                if(ApprovedOutgoingPending(u))
                {approvedPortalWaiting[u.EntityId]=i;HoldApprovedTransit(u);return true;}
                BeginApprovedTransit(u,i);return true;
            }
            return false;
        }
        bool ApprovedOutgoingPending(DeployedUnitState u)=>combatShots.Exists(s=>!s.Resolved&&s.SourceId==u.EntityId);
        bool StepApprovedPortalWait(DeployedUnitState u)
        {
            if(!approvedPortalWaiting.TryGetValue(u.EntityId,out int index))return false;
            HoldApprovedTransit(u);
            if(u.IsReviving||IsSkillActionPlaying(u)||Clock<u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate||ApprovedOutgoingPending(u))return true;
            approvedPortalWaiting.Remove(u.EntityId);BeginApprovedTransit(u,index);return true;
        }
        void BeginApprovedTransit(DeployedUnitState u,int index)
        {
            approvedTransit[u.EntityId]=new PortalTransit{Portal=index,Until=Clock+approvedPortals[index].Duration};
            HoldApprovedTransit(u);
            // Outgoing attacks have already resolved at their original ImpactAt.
            // Only attacks aimed at the entering unit are now interrupted.
            foreach(var shot in combatShots)
                if(!shot.Resolved&&shot.TargetId==u.EntityId)shot.Resolved=true;
        }
        // Clamp all ordinary/forced route travel at a tunnel mouth. Reverse
        // control may retreat to the exit but cannot walk through its void span.
        void ClampApprovedRouteMotion(DeployedUnitState u,double before)
        {
            double after=u.Motion.DistanceTravelled,target=after;
            for(int i=0;i<approvedPortals.Length;i++)
            {
                var p=approvedPortals[i];if(p.Route!=u.RouteId)continue;
                if(!ApprovedPortalPassed(u,i)&&before<=p.Source+.00001&&after>p.Source)target=Math.Min(target,p.Source);
                // Replacements such as Herd spirits inherit route progress, not
                // the predecessor's transit history; their exit boundary is identical.
                if(before>=p.Destination-.00001&&after<p.Destination)target=Math.Max(target,p.Destination);
            }
            if(Math.Abs(after-target)>.000000001)
                u.Motion.ApproachDistance(target,(float)(Math.Abs(after-target)/u.Motion.Speed+.001));
        }
    }
}
