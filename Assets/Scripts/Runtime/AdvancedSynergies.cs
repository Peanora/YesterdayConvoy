using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public enum InfectionForm { Swarm, Shell, Burst }
    public sealed class InfectionBurst
    {
        public Vector3 Position;public double At;public float Attack,Radius;public bool Resolved;
    }
    public sealed partial class ManualBattle
    {
        public int InfectionTier {get;private set;}
        public int GuerrillaTier {get;private set;}
        public int MudrockTier {get;private set;}
        public int Orders {get;private set;}
        public int OrdersUsed {get;private set;}
        public int OrderStrikes {get;private set;}
        public bool HalfOrderPending {get;private set;}
        public int GolemExperience {get;private set;}
        public int GolemStage=>GolemExperience>=10?3:GolemExperience>=4?2:1;
        public int GolemCost=>GolemStage+3;
        public bool GolemDeployed {get;private set;}
        public int GolemGrowthEarned=>Phase==BattlePhase.Won&&MudrockTier>=2?Math.Min(2,crewContacts.Count)+(golemContact?2:0):0;
        public InfectionForm InfectionShape {get;private set;}
        public InfectionForm InfectionSecondShape {get;private set;}
        readonly HashSet<string> orderEntities=new HashSet<string>(),crewContacts=new HashSet<string>(),halfEntities=new HashSet<string>();
        readonly Dictionary<string,int> orderKinds=new Dictionary<string,int>();
        readonly Dictionary<long,bool> orderRequests=new Dictionary<long,bool>();
        readonly List<InfectionBurst> infectionBursts=new List<InfectionBurst>();
        public IReadOnlyList<InfectionBurst> InfectionBursts=>infectionBursts.AsReadOnly();
        bool golemContact;
        void ConfigureAdvancedSynergies(InfectionForm form,InfectionForm second,int experience)
        {
            if(experience<0||!Enum.IsDefined(typeof(InfectionForm),form)||!Enum.IsDefined(typeof(InfectionForm),second))throw new ArgumentException();
            int n=synergyStars.Keys.Count(CampaignBalance.Infected);InfectionTier=n>=6?6:n>=4?4:n>=2?2:0;
            n=synergyStars.Keys.Count(CampaignBalance.Guerrilla);GuerrillaTier=n>=6?6:n>=4?4:n>=2?2:0;
            MudrockTier=synergyStars.Keys.Count(CampaignBalance.MudrockCrew)>=2?2:0;
            InfectionShape=form;InfectionSecondShape=second==form?(InfectionForm)(((int)form+1)%3):second;GolemExperience=experience;
        }
        void ResetAdvancedSynergies()
        {
            orderEntities.Clear();crewContacts.Clear();halfEntities.Clear();orderKinds.Clear();orderRequests.Clear();infectionBursts.Clear();
            Orders=OrdersUsed=OrderStrikes=0;HalfOrderPending=GolemDeployed=golemContact=false;
        }
        int FormTier(InfectionForm form)=>InfectionTier==0?0:InfectionShape==form?InfectionTier:InfectionTier==6&&InfectionSecondShape==form?2:0;
        void OnInfectionDeployment(DeployedUnitState primary)
        {
            if(!synergyEnabled||InfectionTier==0||primary.SynergySupport||!CampaignBalance.Infected(primary.CardId))return;
            var basis=primary.Profile?.Unit??Settings;var combat=UnitCombat(primary);
            float health=primary.MaxHealth;int swarm=FormTier(InfectionForm.Swarm),count=swarm==6?3:swarm>0?2:1;
            float fraction=swarm==4?.7f:swarm>0?.55f:1;
            for(int i=0;i<count;i++)
            {
                var body=primary;
                if(i>0)
                {
                    var lane=lanes.First(l=>l.Id==primary.RouteId);
                    body=new DeployedUnitState($"run-{RunId}/split-{++sequence}",primary.UnitId,new RouteMotion(lane.Points,basis.Speed),health,Clock)
                    {CardId=primary.CardId,RouteId=primary.RouteId,ExitId=primary.ExitId,SynergySupport=true};units.Add(body);
                }
                body.MaxHealth=body.Health=health*fraction;
                var effects=CampaignBalance.CopyEffects(combat.Effects,1,1f/count,i>0);
                int shell=FormTier(InfectionForm.Shell);
                float armor=shell==6?3:shell==4?2:0;
                body.Profile=new BasicRosterProfile(basis,CampaignBalance.CopyCombat(combat,combat.UnitAttack*fraction,combat.UnitArmor+armor,effects),true);
                if(count>1)body.VisualOffset=Vector3.right*((i-(count-1)*.5f)*.25f);
                if(shell>0){body.SynergyShield=body.MaxHealth*(shell==2?.18f:shell==4?.30f:.35f);body.SynergyShieldUntil=Clock+6;}
            }
            int burst=FormTier(InfectionForm.Burst);
            if(burst>0)
            {
                int star=synergyStars[primary.CardId];primary.InfectionBlast=(burst==2?35:burst==4?65:85)*(1+.35f*(star-1));
                primary.InfectionBlastRadius=burst==2?1:burst==4?1.2f:1.3f;
            }
        }
        void OnAdvancedSynergyDeath(DeployedUnitState unit)
        {
            if(unit.InfectionBlast<=0||unit.InfectionBlastDone)return;
            unit.InfectionBlastDone=true;infectionBursts.Add(new InfectionBurst{Position=unit.Motion.Position,At=Clock+.2,Attack=unit.InfectionBlast,Radius=unit.InfectionBlastRadius});
        }
        void ResolveInfectionBursts()
        {
            foreach(var burst in infectionBursts)
            {
                if(burst.Resolved||Clock<burst.At)continue;burst.Resolved=true;
                foreach(var d in defenders.Where(d=>d.Alive&&GroundDistance(d.Position,burst.Position)<=burst.Radius).OrderBy(d=>GroundDistance(d.Position,burst.Position)).Take(burst.Radius<=1?2:3).ToArray())DamageDefender(d,burst.Attack,CombatDamageType.Arts,true);
            }
        }
        void RecordSynergyContact(DeployedUnitState source)
        {
            if(!synergyEnabled||source==null)return;
            if(source.Golem){golemContact=true;return;}
            if(source.SynergySupport)return;
            if(CampaignBalance.MudrockCrew(source.CardId))crewContacts.Add(source.CardId);
            if(GuerrillaTier==0||!orderEntities.Add(source.EntityId))return;
            orderKinds.TryGetValue(source.CardId,out int n);if(n>=2)return;
            orderKinds[source.CardId]=n+1;Orders++;
        }
        bool CanReceiveOrder(DeployedUnitState u)
        {
            if(!u.Active||u.IsReviving||u.SynergySupport||u.SkillAmmo>0||StrongholdInTransit(u.EntityId)||IsSkillActionPlaying(u)||u.FuelDying||u.PillarStunnedUntil>Clock||u.FearUntil>Clock||u.LureUntil>Clock||u.ShackleStunnedUntil>Clock||u.SprayStunnedUntil>Clock)return false;
            var c=UnitCombat(u);
            if(c.Effects.Flamerazer&&!u.IsBlocked||c.Effects.Harvester&&!u.IsBlocked)return false;
            if(!c.CanAttack||Clock<u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate||c.Effects.FrostNova&&Clock<u.IceRingStartedAt+3||FirstSpeakerCasting(u))return false;
            if(u.Spray!=null&&!u.Spray.Ended||u.Shackle!=null&&!u.Shackle.Ended||c.Effects.Harvester&&u.HarvesterPhase!=HarvesterPhase.Combat||c.Effects.Flamechaser&&u.FlamePhase!=FlamechaserPhase.Soldier)return false;
            return ChooseDefender(u,c.UnitRange)!=null;
        }
        public bool TryGuerrillaOrder(int runId,long requestId,bool half=false)
        {
            if(runId!=RunId||Phase!=BattlePhase.Running||IsPaused)return false;
            if(orderRequests.TryGetValue(requestId,out bool prior))return prior==half;
            int cap=GuerrillaTier==2?1:2,cost=half?3:GuerrillaTier==2?4:GuerrillaTier==4?5:6;
            if(GuerrillaTier==0||OrdersUsed>=cap||half&&GuerrillaTier!=6||!half&&HalfOrderPending||Orders<cost)return false;
            var targets=units.Where(u=>CanReceiveOrder(u)&&(!half||!HalfOrderPending||!halfEntities.Contains(u.EntityId))).Take(half?6:GuerrillaTier==6?12:GuerrillaTier==4?8:6).ToArray();
            if(targets.Length==0)return false;
            Orders-=cost;orderRequests[requestId]=half;
            bool completing=half&&HalfOrderPending;
            foreach(var u in targets)
            {
                var c=UnitCombat(u);var d=ChooseDefender(u,c.UnitRange);float power=GuerrillaTier==2?.6f:GuerrillaTier==4?.8f:.9f;
                u.LastAttackTime=Clock;u.LastAttackEmpowered=false;u.IsMoving=false;
                combatShots.Add(new CombatShot(u.EntityId,d.EntityId,false,false,u.Motion.Position,d.Position,Clock+c.AttackWindup/u.LastAttackRate,c.ProjectileTime,c.UnitAttack*UnitAttackMultiplier(u)*power){SynergyOrder=true});OrderStrikes++;
                if(half&&!completing)halfEntities.Add(u.EntityId);
                if(GuerrillaTier>=4&&c.Melee){u.SynergyShield=Mathf.Max(u.SynergyShield,u.MaxHealth*(GuerrillaTier==4?.10f:.15f));u.SynergyShieldUntil=Clock+(GuerrillaTier==4?3:5);}
            }
            if(!half||completing){OrdersUsed++;HalfOrderPending=false;halfEntities.Clear();}else HalfOrderPending=true;
            return true;
        }
        public DeploymentReceipt TryDeployGolem(int runId,long requestId,Vector3 position,bool allowWhilePaused=false)
        {
            if(runId!=RunId)return new DeploymentReceipt(DeployFailure.StaleRun);
            if(committed.TryGetValue(requestId,out var old))return FindUnit(old.EntityId)?.Golem==true?old:new DeploymentReceipt(DeployFailure.StaleRun);
            if(Phase!=BattlePhase.Running)return new DeploymentReceipt(Phase==BattlePhase.Ready?DeployFailure.NotStarted:DeployFailure.Finished);
            if(IsPaused&&!allowWhilePaused)return new DeploymentReceipt(DeployFailure.Paused);
            var lane=FindEntryLane(position);if(lane==null)return new DeploymentReceipt(DeployFailure.WrongEntry);
            if(MudrockTier<2||GolemDeployed)return new DeploymentReceipt(DeployFailure.NoUses);
            if(DP<GolemCost)return new DeploymentReceipt(DeployFailure.NoDP);
            int stage=GolemStage;float hp=stage==1?360:stage==2?550:800,attack=stage==1?60:stage==2?84:116;
            var spec=new ManualBattleSettings(CampaignBalance.GolemCardId,"SYNERGY_GOLEM",Settings.InitialDP,Settings.InitialDefense,1,GolemCost,1,hp,.45f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:attack,unitArmor:stage==1?8:stage==2?10:12,unitRange:1.1f,unitInterval:5.2f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,projectileTime:.02f,hasSkill:false,melee:true,attackDuration:5.166667f,attackWindup:2.746667f);
            var u=new DeployedUnitState($"run-{RunId}/golem-{++sequence}",spec.UnitId,new RouteMotion(lane.Points,spec.Speed),hp,Clock)
                {CardId=spec.CardId,Profile=new BasicRosterProfile(spec,c,true),RouteId=lane.Id,ExitId=lane.ExitId,SynergySupport=true,Golem=true};
            units.Add(u);DP-=GolemCost;GolemDeployed=true;var receipt=new DeploymentReceipt(DeployFailure.None,u.EntityId);committed.Add(requestId,receipt);return receipt;
        }
        void OnGolemHit(DeployedUnitState u,DefenderState target)
        {
            if(!u.Golem||GolemStage<2||u.OrdinaryAttacksFired%3!=0||u.LastGolemProc==u.OrdinaryAttacksFired)return;
            u.LastGolemProc=u.OrdinaryAttacksFired;DamageDefender(target,GolemStage==2?80:120,CombatDamageType.Physical,true);
            if(GolemStage==3)foreach(var d in defenders.Where(d=>d!=target&&d.Alive&&GroundDistance(d.Position,target.Position)<=1).Take(2).ToArray())DamageDefender(d,100,CombatDamageType.Physical,true);
        }
    }
}
