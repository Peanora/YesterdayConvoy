using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        bool strongholdMap;
        public bool StrongholdChallenge {get;private set;}
        readonly HashSet<string> brokenExits=new HashSet<string>();
        public bool StrongholdExitBroken(string id)=>brokenExits.Contains(id);
        void RegisterStrongholdBreach(DeployedUnitState unit)
        {
            Breaches++;
            if(!StrongholdChallenge||brokenExits.Add(unit.ExitId))Defense=Math.Max(0,Defense-(unit.SynergySupport?0:unit.Profile?.Unit.GateDamage??Settings.GateDamage));
        }
        public float DefenderEffectiveInterval(DefenderState d)=>d.Settings.Interval/DefenderAttackRate(d);
        bool UnitCanReachDefender(DeployedUnitState u,DefenderState d)=>(!StrongholdChallenge&&!synergyEnabled)||!d.HighGround||!UnitCombat(u).Melee||u.Golem&&GolemStage>=4;
        readonly Dictionary<string,double> infectionUntil=new Dictionary<string,double>(),infectionTick=new Dictionary<string,double>();
        readonly Dictionary<string,double> portalUntil=new Dictionary<string,double>();
        readonly HashSet<string> portalPassed=new HashSet<string>();
        public int StrongholdPortalTrips {get;private set;}
        public int StrongholdInfectionTouches {get;private set;}
        public float StrongholdInfectionDamage {get;private set;}
        public bool StrongholdMapEnabled=>strongholdMap;
        public bool StrongholdInfected(string id)=>id!=null&&infectionUntil.TryGetValue(id,out double until)&&Clock<until;
        public bool StrongholdInTransit(string id)=>id!=null&&portalUntil.ContainsKey(id);
        public float StrongholdTransitRemaining(string id)=>id!=null&&portalUntil.TryGetValue(id,out double until)?(float)Math.Max(0,until-Clock):0;
        public void ConfigureStrongholdMap(bool challenge=false)
        {
            if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException("Configure map before deployment.");
            strongholdMap=true;StrongholdChallenge=challenge;ResetStrongholdMap();
        }
        void ResetStrongholdMap()
        {
            if(StrongholdChallenge)foreach(var d in defenders)d.HighGround=d.Position.y>.5f;
            brokenExits.Clear();infectionUntil.Clear();infectionTick.Clear();portalUntil.Clear();portalPassed.Clear();
            StrongholdPortalTrips=StrongholdInfectionTouches=0;StrongholdInfectionDamage=0;
        }
        float InfectionAttack(string id)=>StrongholdInfected(id)?1.2f:1;
        float InfectionRate(string id)=>StrongholdInfected(id)?1.2f:1;
        void TouchInfection(string id,Vector3 position)
        {
            if(StrongholdInfected(id))return;
            foreach(var tile in StrongholdMapLayout.InfectionTiles)
                if(Mathf.Abs(tile.x-position.x)<.5f&&Mathf.Abs(tile.z-position.z)<.5f)
                {infectionUntil[id]=Clock+300;infectionTick[id]=Clock+1;StrongholdInfectionTouches++;return;}
        }
        bool InfectionDue(string id)
        {
            if(!StrongholdInfected(id)||Clock+1e-6<infectionTick[id])return false;
            infectionTick[id]+=1;return true;
        }
        void StepStrongholdMap()
        {
            if(!strongholdMap)return;
            foreach(var u in units)
            {
                if(!u.Active||u.IsReviving)continue;
                if(!StrongholdInTransit(u.EntityId))TouchInfection(u.EntityId,u.Motion.Position);
                if(InfectionDue(u.EntityId)){float hp=u.Health;TrueDamage(u,1);StrongholdInfectionDamage+=hp-u.Health;}
            }
            foreach(var d in defenders)
            {
                if(!d.Alive)continue;TouchInfection(d.EntityId,d.Position);
                if(InfectionDue(d.EntityId)){float hp=d.Health;TrueDamage(d,1);StrongholdInfectionDamage+=hp-d.Health;}
            }
        }
        bool StepStrongholdPortal(DeployedUnitState u)
        {
            if(!strongholdMap||portalPassed.Contains(u.EntityId))return false;
            var lane=lanes.FirstOrDefault(l=>l.Id==u.RouteId);if(lane==null||lane.Points.Count<8)return false;
            int portal=StrongholdChallengeLayout.PortalSource(lane.Points);
            double source=0;for(int i=1;i<=portal;i++)source+=Vector3.Distance(lane.Points[i-1],lane.Points[i]);
            double destination=source+Vector3.Distance(lane.Points[portal],lane.Points[portal+1]);
            if(!portalUntil.TryGetValue(u.EntityId,out double until))
            {
                if(u.Motion.DistanceTravelled+1e-6<source)return false;
                u.Motion.ApproachDistance(source,(float)(Math.Abs(u.Motion.DistanceTravelled-source)/u.Motion.Speed+.01));
                portalUntil[u.EntityId]=Clock+3;u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;
                CancelUnitShots(u);return true;
            }
            if(Clock+1e-6<until)return true;
            u.Motion.ApproachDistance(destination,(float)((destination-source)/u.Motion.Speed+.01));
            portalUntil.Remove(u.EntityId);portalPassed.Add(u.EntityId);StrongholdPortalTrips++;
            u.NextAttackTime=Clock;return false;
        }
    }
}
