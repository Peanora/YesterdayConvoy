using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public enum MarineAdaptation { Barrier, Hunt, Infiltration }
    public sealed class DeepSeed
    {
        public Vector3 Position {get;internal set;}
        public double At {get;internal set;}
        public bool Spent {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        public int HerdTier {get;private set;}
        public int DeepTier {get;private set;}
        public int SarkazTier {get;private set;}
        public int MarineTier {get;private set;}
        public int HerdUsed {get;private set;}
        public int DeepDetonations {get;private set;}
        public int DeepCharges=>deepSeeds.Count(s=>!s.Spent);
        public int ContractPoints {get;private set;}
        public int ContractUses {get;private set;}
        public string ContractCard {get;private set;}
        // Compatibility reads for old callers: marine growth and adaptation effects have been retired.
        public int MarineExperience=>0;
        public int MarineStage=>1;
        public int MarineGrowthEarned=>0;
        public MarineAdaptation Adaptation {get;private set;}
        public MarineAdaptation SecondAdaptation {get;private set;}
        public IReadOnlyList<DeepSeed> DeepSeeds=>deepSeeds.AsReadOnly();
        sealed class FireBurn {internal double Until,LastTick;internal float Power;}
        readonly List<DeepSeed> deepSeeds=new List<DeepSeed>();
        readonly HashSet<string> deepDeaths=new HashSet<string>();
        readonly HashSet<string> herdDeaths=new HashSet<string>();
        readonly Queue<DeployedUnitState> herdQueue=new Queue<DeployedUnitState>();
        readonly Dictionary<string,double> herdLifetimes=new Dictionary<string,double>(),contractWindows=new Dictionary<string,double>();
        readonly Dictionary<string,HerdSpiritState> herdSpirits=new Dictionary<string,HerdSpiritState>(),herdSources=new Dictionary<string,HerdSpiritState>();
        readonly Dictionary<string,FireBurn> deepBurns=new Dictionary<string,FireBurn>();
        readonly Dictionary<string,int> contractKinds=new Dictionary<string,int>();
        readonly Dictionary<long,string> contractRequests=new Dictionary<long,string>();
        readonly HashSet<long> deepRequests=new HashSet<long>();
        readonly HashSet<string> contractedEntities=new HashSet<string>();
        static bool Tag(string id,string name)=>CampaignBalance.Tags(id).Contains(name);
        int MemberTier(string tag,int high,int middle=0)
        {
            int n=synergyStars.Keys.Count(id=>Tag(id,tag));return n>=high?high:middle>0&&n>=middle?middle:n>=2?2:0;
        }
        void ConfigureRemainingSynergies(string contract,MarineAdaptation adaptation,MarineAdaptation second,int experience)
        {
            if(experience<0||!Enum.IsDefined(typeof(MarineAdaptation),adaptation)||!Enum.IsDefined(typeof(MarineAdaptation),second))throw new ArgumentException();
            HerdTier=MemberTier("牧群",4);DeepTier=MemberTier("深池",4);SarkazTier=MemberTier("萨卡兹",6,4);MarineTier=MemberTier("海嗣",4);
            ContractCard=contract!=null&&synergyStars.ContainsKey(contract)?contract:synergyStars.Keys.FirstOrDefault();
            Adaptation=adaptation;SecondAdaptation=second==adaptation?(MarineAdaptation)(((int)adaptation+1)%3):second;
        }
        void ResetRemainingSynergies()
        {
            deepSeeds.Clear();deepDeaths.Clear();herdQueue.Clear();herdDeaths.Clear();herdLifetimes.Clear();herdSpirits.Clear();herdSources.Clear();contractWindows.Clear();deepBurns.Clear();
            contractKinds.Clear();contractRequests.Clear();deepRequests.Clear();contractedEntities.Clear();ResetMarineTide();
            HerdUsed=DeepDetonations=ContractPoints=ContractUses=0;
        }
        void OnRemainingDeath(DeployedUnitState u)
        {
            if(!synergyEnabled||HerdTier==0||u.SynergySupport||HerdUsed>=(HerdTier==4?5:3)||!CampaignBalance.Profiles.TryGetValue(u.CardId,out var p)||p.Cost==5||!herdDeaths.Add(u.EntityId))return;
            HerdUsed++;herdQueue.Enqueue(u);
        }
        void FlushHerd()
        {
            while(herdQueue.Count>0)
            {
                var original=herdQueue.Dequeue();var source=UnitCombat(original);var lane=lanes.First(l=>l.Id==original.RouteId);
                float ratio=HerdTier==4?.45f:.35f;bool ranged=HerdTier==4&&!source.Melee;
                var motion=new RouteMotion(lane.Points,.65f);motion.Advance((float)(original.Motion.DistanceTravelled/.65));
                var u=new DeployedUnitState($"run-{RunId}/host-{++sequence}",original.UnitId,motion,original.MaxHealth*ratio,Clock)
                    {CardId=original.CardId,RouteId=original.RouteId,ExitId=original.ExitId,SynergySupport=true};
                var spirit=new HerdSpiritState(original.EntityId,u.EntityId,original.DeathTime);
                // The replacement already counts as pending force, but cannot fight or move until it rises.
                // Reuse the same simulation-clock gate as existing rebirth instead of adding a visual-only lock.
                u.IsReviving=true;u.ReviveAt=spirit.ReadyAt;u.IsMoving=false;u.DeathTime=spirit.StartedAt;
                float attack=source.UnitAttack*ratio*(ranged?.85f:1);
                var c=new EngagementSettings(unitAttack:attack,unitArmor:HerdTier==4?6:4,unitRange:ranged?2.5f:.75f,unitInterval:Mathf.Max(2,source.AttackDuration),
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:!ranged,
                    attackDuration:source.AttackDuration,attackWindup:source.AttackWindup,canAttack:source.CanAttack);
                var basis=original.Profile?.Unit??Settings;
                var s=new ManualBattleSettings(u.CardId,u.UnitId,Settings.InitialDP,Settings.InitialDefense,1,1,1,u.MaxHealth,.65f,1,basis.TowerRange,basis.TowerDamage,basis.ShotInterval);
                u.Profile=new BasicRosterProfile(s,c,true);units.Add(u);herdLifetimes[u.EntityId]=spirit.ReadyAt+(HerdTier==4?12:10);
                herdSpirits.Add(u.EntityId,spirit);herdSources.Add(original.EntityId,spirit);
            }
        }
        public bool IsHerdHost(string entity)=>herdLifetimes.ContainsKey(entity);
        public HerdSpiritState HerdSpiritFor(string entityId)=>entityId!=null&&herdSpirits.TryGetValue(entityId,out var spirit)?spirit:null;
        public HerdSpiritState HerdSpiritFrom(string sourceId)=>sourceId!=null&&herdSources.TryGetValue(sourceId,out var spirit)?spirit:null;
        public double HerdRemaining(string entity)=>herdLifetimes.TryGetValue(entity,out var until)?Math.Max(0,until-Math.Max(Clock,herdSpirits[entity].ReadyAt)):0;
        void StepRemainingSynergies(float step)
        {
            FlushHerd();
            StepMarineTide(step);
            foreach(var pair in herdLifetimes)
            {
                var u=FindUnit(pair.Key);if(u==null||!u.Active||Clock<pair.Value)continue;
                u.Health=0;u.Resolution=UnitResolution.Dead;u.DeathTime=Clock;u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;
            }
            foreach(var pair in deepBurns.ToArray())
            {
                var d=FindDefender(pair.Key);var burn=pair.Value;
                if(d==null||!d.Alive){deepBurns.Remove(pair.Key);continue;}
                float elapsed=(float)Math.Max(0,Math.Min(Clock,burn.Until)-burn.LastTick);
                burn.LastTick=Clock;
                if(elapsed>0)
                {
                    DamageDefender(d,burn.Power,CombatDamageType.Arts,true,damageScale:elapsed);
                    // Only this trait's DOT may pass the torch; detonations and other derived damage cannot.
                    if(!d.Alive)OnDeepKill(d,true);
                }
                if(!d.Alive||Clock>=burn.Until)deepBurns.Remove(pair.Key);
            }
        }
        public double DeepBurnRemaining(string defenderId)=>defenderId!=null&&deepBurns.TryGetValue(defenderId,out var burn)?Math.Max(0,burn.Until-Clock):0;
        public float DeepBurnPower(string defenderId)=>DeepBurnRemaining(defenderId)>0?deepBurns[defenderId].Power:0;
        bool HasPendingDeepBurn=>deepBurns.Any(p=>p.Value.Until>Clock&&FindDefender(p.Key)?.Alive==true);
        bool DeepBurning(DefenderState d)=>d.BurnUntil>Clock||deepBurns.TryGetValue(d.EntityId,out var burn)&&burn.Until>Clock;
        float DeepScale=>1+.25f*((float)synergyStars.Where(p=>Tag(p.Key,"深池")).Select(p=>p.Value).DefaultIfEmpty(1).Average()-1);
        bool IsDeepHit(DeployedUnitState source)=>synergyEnabled&&DeepTier>0&&source!=null&&!source.SynergySupport&&Tag(source.CardId,"深池");
        void ApplyDeepBurn(DefenderState d)
        {
            if(!d.Alive)return;
            float power=(DeepTier==4?25:15)*DeepScale;
            if(!deepBurns.TryGetValue(d.EntityId,out var burn))deepBurns[d.EntityId]=burn=new FireBurn{LastTick=Clock};
            burn.Power=Mathf.Max(burn.Power,power);burn.Until=Clock+4;
        }
        void OnDeepKill(DefenderState d,bool burning)
        {
            if(!synergyEnabled||DeepTier==0||!burning||deepSeeds.Count>=(DeepTier==4?6:4)||!deepDeaths.Add(d.EntityId))return;
            deepSeeds.Add(new DeepSeed{Position=d.Position,At=Clock});
            foreach(var other in defenders.Where(o=>o.Alive&&GroundDistance(o.Position,d.Position)<=(DeepTier==4?2.2f:1.8f)).OrderBy(o=>GroundDistance(o.Position,d.Position)).Take(DeepTier==4?3:2))ApplyDeepBurn(other);
        }
        public bool TryDetonateDeep(int runId,long request)
        {
            if(runId!=RunId||Phase!=BattlePhase.Running||IsPaused||DeepTier<4)return false;
            if(deepRequests.Contains(request))return true;
            if(DeepDetonations>=2)return false;
            var seed=deepSeeds.FirstOrDefault(s=>!s.Spent&&defenders.Any(d=>d.Alive&&GroundDistance(s.Position,d.Position)<=1.8f));
            if(seed==null)return false;
            var targets=defenders.Where(d=>d.Alive&&GroundDistance(seed.Position,d.Position)<=1.8f).OrderBy(d=>GroundDistance(seed.Position,d.Position)).Take(4).ToArray();
            deepRequests.Add(request);seed.Spent=true;DeepDetonations++;
            foreach(var d in targets)DamageDefender(d,180*DeepScale,CombatDamageType.Arts,true);return true;
        }
        void OnContractDamage(DeployedUnitState u,float healthBefore)
        {
            if(!synergyEnabled||SarkazTier==0||healthBefore<=u.MaxHealth*.4f||u.SynergySupport||u.CardId==ContractCard||u.Health>u.MaxHealth*.4f||ContractUses>=(SarkazTier==2?1:2)||!contractedEntities.Add(u.EntityId))return;
            contractKinds.TryGetValue(u.CardId,out int n);if(n>=2)return;contractKinds[u.CardId]=n+1;ContractPoints=Math.Min(4,ContractPoints+1);
        }
        public bool TryActivateContract(int runId,long request,string entity)
        {
            if(runId!=RunId||Phase!=BattlePhase.Running||IsPaused)return false;
            if(contractRequests.TryGetValue(request,out var prior))return prior==entity;
            var u=FindUnit(entity);
            if(SarkazTier==0||ContractPoints<4||ContractUses>=(SarkazTier==2?1:2)||u==null||!u.Active||u.IsReviving||u.SynergySupport||u.CardId!=ContractCard)return false;
            ContractPoints=0;ContractUses++;contractRequests[request]=entity;
            double until=Clock+(SarkazTier==2?6:SarkazTier==4?8:10);contractWindows[entity]=until;
            u.SynergyShield=Mathf.Max(u.SynergyShield,u.MaxHealth*(SarkazTier==2?.15f:SarkazTier==4?.25f:.30f));u.SynergyShieldUntil=Math.Max(u.SynergyShieldUntil,until);
            if(SarkazTier==6)foreach(var other in units.Where(o=>o.Active&&!o.IsReviving&&!o.SynergySupport&&o!=u))
            {other.SynergyShield=Mathf.Max(other.SynergyShield,Mathf.Min(200,u.MaxHealth*.1f));other.SynergyShieldUntil=Math.Max(other.SynergyShieldUntil,until);}
            return true;
        }
        public float ContractAttackBonus(DeployedUnitState u)=>contractWindows.TryGetValue(u.EntityId,out var until)&&until>Clock?(SarkazTier==2?.2f:SarkazTier==4?.3f:.4f):0;
    }
}
