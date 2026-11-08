using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public enum InfectionForm { Swarm, Shell, Burst }
    public sealed class InfectionBurstHit
    {
        public string EntityId {get;}
        public Vector3 Position {get;}
        public float Damage {get;}
        internal InfectionBurstHit(string entityId,Vector3 position,float damage)
        {EntityId=entityId;Position=position;Damage=damage;}
    }
    public sealed class InfectionBurst
    {
        public Vector3 Position;public double At;public float Attack,Radius;public int Targets;public bool Resolved;
        public string SourceId {get;internal set;}
        public string CardId {get;internal set;}
        public float TotalDamage {get;private set;}
        readonly List<InfectionBurstHit> hits=new List<InfectionBurstHit>();
        IReadOnlyList<InfectionBurstHit> visibleHits;
        public IReadOnlyList<InfectionBurstHit> Hits=>visibleHits??(visibleHits=hits.AsReadOnly());
        internal void RecordHit(string entityId,Vector3 position,float damage)
        {hits.Add(new InfectionBurstHit(entityId,position,damage));TotalDamage+=damage;}
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
        public int MudrockTotalStars {get;private set;}
        public int GolemCapacity=>GolemGrowth.Capacity(MudrockCrewCount,MudrockTotalStars);
        public int GolemStage=>GolemGrowth.Stage(GolemExperience,GolemCapacity);
        public int GolemCost=>(int)V2Summon(GolemStage,"dp",GolemGrowth.Cost(GolemStage));
        public bool GolemDeployed {get;private set;}
        public int GolemBattleExperience=>GolemGrowth.BattleXP(MudrockTier,MudrockTotalStars);
        public int GolemGrowthEarned=>(Phase==BattlePhase.Won||Phase==BattlePhase.Lost)&&!GuaranteedSupply?GolemGrowth.Award(GolemExperience,GolemCapacity,GolemBattleExperience):0;
        public InfectionForm InfectionShape {get;private set;}
        public InfectionForm InfectionSecondShape {get;private set;}
        readonly HashSet<string> orderEntities=new HashSet<string>(),halfEntities=new HashSet<string>();
        readonly Dictionary<string,int> orderKinds=new Dictionary<string,int>();
        readonly Dictionary<long,bool> orderRequests=new Dictionary<long,bool>();
        readonly List<InfectionBurst> infectionBursts=new List<InfectionBurst>();
        public IReadOnlyList<InfectionBurst> InfectionBursts=>infectionBursts.AsReadOnly();
        readonly List<ShatterEvent> golemPulses=new List<ShatterEvent>();
        public IReadOnlyList<ShatterEvent> GolemPulses=>golemPulses.AsReadOnly();
        internal GuerrillaRules.Rule CurrentGuerrillaRule
        {
            get
            {
                var old=GuerrillaRules.ForTier(GuerrillaTier);
                if(!CampaignV2Enabled||GuerrillaTier==0)return old;
                int participants=GuerrillaTier>=8?int.MaxValue:(int)V2Synergy("guerrilla",GuerrillaTier,"participantLimit",old.ParticipantLimit);
                return new GuerrillaRules.Rule(GuerrillaTier,(int)V2Synergy("guerrilla",GuerrillaTier,"fullOrderCost",old.Cost),
                    (int)V2Synergy("guerrilla",GuerrillaTier,"initialOrders",old.InitialOrders),participants,
                    V2Synergy("guerrilla",GuerrillaTier,"attackMultiplier",old.AttackMultiplier),
                    V2Synergy("guerrilla",GuerrillaTier,"halfAllowed",old.AllowsHalf?1:0)>0,
                    V2Synergy("guerrilla",GuerrillaTier,"expandedEligibility",old.ExpandedEligibility?1:0)>0);
            }
        }
        void ConfigureAdvancedSynergies(InfectionForm form,InfectionForm second,int experience)
        {
            if(experience<0||!Enum.IsDefined(typeof(InfectionForm),form)||!Enum.IsDefined(typeof(InfectionForm),second))throw new ArgumentException();
            int n=synergyStars.Keys.Count(CampaignBalance.Infected);InfectionTier=n>=6?6:n>=4?4:n>=2?2:0;
            GuerrillaTier=GuerrillaRules.TierForCount(guerrillaMembers.Count);
            MudrockTier=GolemGrowth.TierForCount(MudrockCrewCount);
            MudrockTotalStars=mudrockMembers.Sum(id=>synergyStars[id]);
            InfectionShape=form;InfectionSecondShape=second==form?(InfectionForm)(((int)form+1)%3):second;GolemExperience=experience;
        }
        void ResetAdvancedSynergies()
        {
            ResetGuerrillaOrders();
            golemPulses.Clear();orderEntities.Clear();halfEntities.Clear();orderKinds.Clear();orderRequests.Clear();infectionBursts.Clear();
            Orders=OrdersUsed=OrderStrikes=0;HalfOrderPending=GolemDeployed=false;
        }
        int FormTier(InfectionForm form)=>InfectionTier==0?0:InfectionShape==form?InfectionTier:InfectionTier==6&&InfectionSecondShape==form?(int)V2Synergy("infection",InfectionTier,"secondaryTier",2):0;
        void OnInfectionDeployment(DeployedUnitState primary)
        {
            if(!synergyEnabled||InfectionTier==0||primary.SynergySupport||!CampaignBalance.Infected(primary.CardId))return;
            var basis=primary.Profile?.Unit??Settings;var combat=UnitCombat(primary);
            float health=primary.MaxHealth;int swarm=FormTier(InfectionForm.Swarm),count=(int)V2Synergy("infection",swarm,"swarmBodies",swarm==6?3:swarm>0?2:1);
            float fraction=V2Synergy("infection",swarm,"swarmHPFraction",swarm==4?.7f:swarm>0?.55f:1);
            float attackFraction=V2Synergy("infection",swarm,"swarmAttackFraction",fraction);
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
                float armor=V2Synergy("infection",shell,"shellArmorAdd",shell==6?3:shell==4?2:0);
                body.Profile=new BasicRosterProfile(basis,CampaignBalance.CopyCombat(combat,combat.UnitAttack*attackFraction,combat.UnitArmor+armor,effects),true);
                if(count>1)body.VisualOffset=Vector3.right*((i-(count-1)*.5f)*.25f);
                if(shell>0){body.SynergyShield=body.MaxHealth*V2Synergy("infection",shell,"shellShieldFraction",shell==2?.18f:shell==4?.30f:.35f);body.SynergyShieldUntil=Clock+V2Synergy("infection",shell,"shellShieldSeconds",6);}
            }
            int burst=FormTier(InfectionForm.Burst);
            if(burst>0)
            {
                int star=synergyStars[primary.CardId];primary.InfectionBlast=V2Synergy("infection",burst,"burstBaseArts",burst==2?35:burst==4?65:85)*(1+.35f*(star-1));
                primary.InfectionBlastRadius=V2Synergy("infection",burst,"burstRadius",burst==2?1:burst==4?1.2f:1.3f);
            }
        }
        void OnAdvancedSynergyDeath(DeployedUnitState unit)
        {
            if(unit.InfectionBlast<=0||unit.InfectionBlastDone)return;
            int tier=FormTier(InfectionForm.Burst);
            unit.InfectionBlastDone=true;infectionBursts.Add(new InfectionBurst{SourceId=unit.EntityId,CardId=unit.CardId,Position=unit.Motion.Position,At=Clock+V2Synergy("infection",tier,"burstDelaySeconds",.2f),Attack=unit.InfectionBlast,Radius=unit.InfectionBlastRadius,Targets=(int)V2Synergy("infection",tier,"burstTargets",unit.InfectionBlastRadius<=1?2:3)});
        }
        void ResolveInfectionBursts()
        {
            foreach(var burst in infectionBursts)
            {
                if(burst.Resolved||Clock<burst.At)continue;burst.Resolved=true;
                foreach(var d in defenders.Where(d=>d.Alive&&GroundDistance(d.Position,burst.Position)<=burst.Radius).OrderBy(d=>GroundDistance(d.Position,burst.Position)).Take(burst.Targets).ToArray())
                {
                    float before=d.Health;DamageDefenderFrom(d,burst.Attack,CombatDamageType.Arts,FindUnit(burst.SourceId),true);
                    burst.RecordHit(d.EntityId,d.Position,Mathf.Max(0,before-d.Health));
                }
            }
        }
        void RecordSynergyContact(DeployedUnitState source)
        {
            if(!synergyEnabled||source==null)return;
            if(source.Golem)return;
            if(source.SynergySupport)return;
            if(GuerrillaTier==0||!orderEntities.Add(source.EntityId))return;
            orderKinds.TryGetValue(source.CardId,out int n);if(n>=2)return;
            orderKinds[source.CardId]=n+1;Orders++;
        }
        bool CanReceiveOrder(DeployedUnitState u)
        {
            if(!u.Active||u.IsReviving||TalulahTransitionLocked(u)||u.SynergySupport||u.SkillAmmo>0||StrongholdInTransit(u.EntityId)||IsSkillActionPlaying(u)||u.FuelDying||u.PillarStunnedUntil>Clock||u.FearUntil>Clock||u.LureUntil>Clock||u.ShackleStunnedUntil>Clock||u.SprayStunnedUntil>Clock)return false;
            var c=UnitCombat(u);
            bool expanded=CurrentGuerrillaRule.ExpandedEligibility;
            if(c.Effects.Flamerazer&&!u.IsBlocked||c.Effects.Harvester&&!u.IsBlocked)return false;
            if(!c.CanAttack||!expanded&&Clock<u.LastAttackTime+UnitActionDuration(u)/u.LastAttackRate||c.Effects.FrostNova&&Clock<u.IceRingStartedAt+3||FirstSpeakerCasting(u))return false;
            if(u.Spray!=null&&!u.Spray.Ended||u.Shackle!=null&&!u.Shackle.Ended||c.Effects.Harvester&&u.HarvesterPhase!=HarvesterPhase.Combat||c.Effects.Flamechaser&&u.FlamePhase!=FlamechaserPhase.Soldier)return false;
            return expanded||ChooseDefender(u,c.UnitRange)!=null;
        }
        public bool TryGuerrillaOrder(int runId,long requestId,bool half=false)
        {
            if(runId!=RunId||Phase!=BattlePhase.Running||IsPaused)return false;
            if(orderRequests.TryGetValue(requestId,out bool prior))return prior==half;
            if(!TryCommitGuerrillaOrder(half))return false;
            orderRequests[requestId]=half;return true;
        }
        void TryAutomaticGuerrillaOrder()
        {
            // A pending front half belongs to the player. Automatic full orders cannot
            // consume its remaining points or replace its participant exclusion set.
            if(!HalfOrderPending)TryCommitGuerrillaOrder(false);
        }
        bool TryCommitGuerrillaOrder(bool half)
        {
            if(Phase!=BattlePhase.Running||IsPaused||GuerrillaOrderBusy)return false;
            var rule=CurrentGuerrillaRule;int cost=half&&rule.AllowsHalf?(int)V2Synergy("guerrilla",GuerrillaTier,"halfOrderCost",3):rule.Cost;
            if(GuerrillaTier==0||half&&!rule.AllowsHalf||!half&&HalfOrderPending||Orders<cost||!defenders.Any(d=>d.Alive))return false;
            var targets=units.Where(u=>CanReceiveOrder(u)&&(!half||!HalfOrderPending||!halfEntities.Contains(u.EntityId))).Take(half?(int)V2Synergy("guerrilla",GuerrillaTier,"halfParticipantLimit",6):rule.ParticipantLimit).ToArray();
            if(targets.Length==0)return false;
            Orders-=cost;
            bool completing=half&&HalfOrderPending;
            CommitGuerrillaOrder(targets,rule.AttackMultiplier);
            foreach(var u in targets)
            {
                var c=UnitCombat(u);
                if(!rule.ExpandedEligibility){u.LastAttackTime=Clock;u.LastAttackEmpowered=false;u.IsMoving=false;}
                OrderStrikes++;
                if(half&&!completing)halfEntities.Add(u.EntityId);
                if(GuerrillaTier>=4&&c.Melee){u.SynergyShield=Mathf.Max(u.SynergyShield,u.MaxHealth*V2Synergy("guerrilla",GuerrillaTier,"meleeShieldFraction",GuerrillaTier==4?.10f:.15f));u.SynergyShieldUntil=Clock+V2Synergy("guerrilla",GuerrillaTier,"shieldSeconds",GuerrillaTier==4?3:5);}
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
            var waveFailure=CheckWaveDeployment(lane.Id);if(waveFailure!=DeployFailure.None)return new DeploymentReceipt(waveFailure);
            if(MudrockTier<2||GolemDeployed)return new DeploymentReceipt(DeployFailure.NoUses);
            if(DP<GolemCost)return new DeploymentReceipt(DeployFailure.NoDP);
            int stage=GolemStage;float hp=V2Summon(stage,"hp",stage==1?360:stage==2?550:stage==3?1800:stage==4?6000:7500),attack=V2Summon(stage,"attack",stage==1?60:stage==2?84:stage==3?240:stage==4?1600:2000);
            // Supports never damage the gate; the legacy Settings constructor keeps a positive placeholder.
            var spec=new ManualBattleSettings(CampaignBalance.GolemCardId,"SYNERGY_GOLEM",Settings.InitialDP,Settings.InitialDefense,1,GolemCost,1,hp,V2Summon(0,"speed",.45f),1,1,1,1);
            var c=new EngagementSettings(unitAttack:attack,unitArmor:V2Summon(stage,"armor",stage==1?8:stage==2?10:stage==3?20:60),unitRange:V2Summon(stage,"range",stage>=4?2.2f:1.1f),unitInterval:V2Summon(0,"intervalSeconds",5.2f),
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,projectileTime:V2Summon(0,"projectileSeconds",.02f),hasSkill:false,melee:true,attackDuration:V2Summon(0,"attackDurationSeconds",5.166667f),attackWindup:V2Summon(0,"windupSeconds",2.746667f),
                effects:new MonsterEffectSettings(unitResistance:V2Summon(stage,"resistance",stage>=4?60:stage==3?30:0),attackTargets:(int)V2Summon(stage,"targets",stage>=4?3:1)));
            var u=new DeployedUnitState($"run-{RunId}/golem-{++sequence}",spec.UnitId,new RouteMotion(lane.Points,spec.Speed),hp,Clock)
                {CardId=spec.CardId,Profile=new BasicRosterProfile(spec,c,true),RouteId=lane.Id,ExitId=lane.ExitId,SynergySupport=true,Golem=true};
            units.Add(u);DP-=GolemCost;GolemDeployed=true;var receipt=new DeploymentReceipt(DeployFailure.None,u.EntityId);committed.Add(requestId,receipt);return receipt;
        }
        void OnGolemHit(DeployedUnitState u,DefenderState target)
        {
            if(!u.Golem)return;
            if(GolemStage>=4&&u.LastGolemHeal!=u.OrdinaryAttacksFired)
            {u.LastGolemHeal=u.OrdinaryAttacksFired;HealUnit(u,u.MaxHealth*V2Summon(GolemStage,"healPerSuccessfulAttackFraction",.08f));}
            if(GolemStage<2||u.OrdinaryAttacksFired%3!=0||u.LastGolemProc==u.OrdinaryAttacksFired)return;
            u.LastGolemProc=u.OrdinaryAttacksFired;
            if(GolemStage>=4)
            {
                golemPulses.Add(new ShatterEvent{Position=u.Motion.Position,At=Clock,Radius=6});
                foreach(var d in defenders.Where(d=>d.Alive).ToArray())DamageDefenderFrom(d,UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*V2Summon(GolemStage,"globalThirdHitArtsFraction",.8f),CombatDamageType.Arts,u,true);
            }
            else
            {
                DamageDefenderFrom(target,V2Summon(GolemStage,"thirdHitPrimaryPhysical",GolemStage==2?80:120),CombatDamageType.Physical,u,true);
                if(GolemStage==3)foreach(var d in defenders.Where(d=>d!=target&&d.Alive&&GroundDistance(d.Position,target.Position)<=V2Summon(GolemStage,"thirdHitSideRadius",1)).Take((int)V2Summon(GolemStage,"thirdHitSideTargets",2)).ToArray())DamageDefenderFrom(d,V2Summon(GolemStage,"thirdHitSidePhysical",100),CombatDamageType.Physical,u,true);
            }
        }
    }
}
