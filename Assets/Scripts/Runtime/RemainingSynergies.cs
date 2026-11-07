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
        public int MarineExperience {get;private set;}
        public int MarineStage=>MarineExperience>=7?3:MarineExperience>=3?2:1;
        public int MarineGrowthEarned=>Phase==BattlePhase.Won&&MarineTier>0?Math.Min(MarineTier==4?3:2,marineContacts.Count):0;
        public MarineAdaptation Adaptation {get;private set;}
        public MarineAdaptation SecondAdaptation {get;private set;}
        public IReadOnlyList<DeepSeed> DeepSeeds=>deepSeeds.AsReadOnly();
        sealed class FireBurn {internal double Until,LastTick;internal float Power;}
        sealed class Penetration {internal double Until;internal float Armor;}
        readonly List<DeepSeed> deepSeeds=new List<DeepSeed>();
        readonly HashSet<string> deepDeaths=new HashSet<string>();
        readonly HashSet<string> herdDeaths=new HashSet<string>();
        readonly Queue<DeployedUnitState> herdQueue=new Queue<DeployedUnitState>();
        readonly Dictionary<string,double> herdLifetimes=new Dictionary<string,double>(),contractWindows=new Dictionary<string,double>();
        readonly Dictionary<string,FireBurn> deepBurns=new Dictionary<string,FireBurn>();
        readonly Dictionary<string,Penetration> marinePenetration=new Dictionary<string,Penetration>();
        readonly Dictionary<string,int> marineBarrierHits=new Dictionary<string,int>(),contractKinds=new Dictionary<string,int>();
        readonly Dictionary<long,string> contractRequests=new Dictionary<long,string>();
        readonly HashSet<long> deepRequests=new HashSet<long>();
        readonly HashSet<string> contractedEntities=new HashSet<string>(),marineContacts=new HashSet<string>(),marineProtected=new HashSet<string>(),marineFirstHits=new HashSet<string>();
        bool marineMealUsed;
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
            Adaptation=adaptation;SecondAdaptation=second==adaptation?(MarineAdaptation)(((int)adaptation+1)%3):second;MarineExperience=experience;
        }
        void ResetRemainingSynergies()
        {
            deepSeeds.Clear();deepDeaths.Clear();herdQueue.Clear();herdDeaths.Clear();herdLifetimes.Clear();contractWindows.Clear();deepBurns.Clear();marinePenetration.Clear();
            marineBarrierHits.Clear();contractKinds.Clear();contractRequests.Clear();deepRequests.Clear();contractedEntities.Clear();marineContacts.Clear();marineProtected.Clear();marineFirstHits.Clear();
            HerdUsed=DeepDetonations=ContractPoints=ContractUses=0;marineMealUsed=false;
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
                float attack=source.UnitAttack*ratio*(ranged?.85f:1);
                var c=new EngagementSettings(unitAttack:attack,unitArmor:HerdTier==4?6:4,unitRange:ranged?2.5f:.75f,unitInterval:Mathf.Max(2,source.AttackDuration),
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:!ranged,
                    attackDuration:source.AttackDuration,attackWindup:source.AttackWindup,canAttack:source.CanAttack);
                var basis=original.Profile?.Unit??Settings;
                var s=new ManualBattleSettings(u.CardId,u.UnitId,Settings.InitialDP,Settings.InitialDefense,1,1,1,u.MaxHealth,.65f,1,basis.TowerRange,basis.TowerDamage,basis.ShotInterval);
                u.Profile=new BasicRosterProfile(s,c,true);units.Add(u);herdLifetimes[u.EntityId]=Clock+(HerdTier==4?12:10);
            }
        }
        public bool IsHerdHost(string entity)=>herdLifetimes.ContainsKey(entity);
        public double HerdRemaining(string entity)=>herdLifetimes.TryGetValue(entity,out var until)?Math.Max(0,until-Clock):0;
        void StepRemainingSynergies(float step)
        {
            FlushHerd();
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
        int AdaptationStage(MarineAdaptation type)=>MarineTier==0?0:Adaptation==type?MarineStage:MarineTier==4&&MarineStage>=2&&SecondAdaptation==type?1:0;
        void OnMarineContact(DeployedUnitState u)
        {if(synergyEnabled&&MarineTier>0&&!u.SynergySupport&&Tag(u.CardId,"海嗣"))marineContacts.Add(u.CardId);}
        float MarineIncoming(DeployedUnitState u,bool ranged)
        {
            if(!synergyEnabled||!ranged||u.SynergySupport)return 1;
            if(marineProtected.Remove(u.EntityId))return .8f;
            int stage=AdaptationStage(MarineAdaptation.Barrier);if(stage==0||!Tag(u.CardId,"海嗣"))return 1;
            marineBarrierHits.TryGetValue(u.EntityId,out int count);if(count>=(stage==1?1:2))return 1;marineBarrierHits[u.EntityId]=count+1;
            if(stage==3&&count==0)
            {
                var ally=units.Where(o=>o!=u&&o.Active&&!o.IsReviving&&!o.SynergySupport&&!Tag(o.CardId,"海嗣")&&GroundDistance(o.Motion.Position,u.Motion.Position)<=1.5f).OrderBy(o=>GroundDistance(o.Motion.Position,u.Motion.Position)).FirstOrDefault();
                if(ally!=null)marineProtected.Add(ally.EntityId);
            }
            return stage==3?.6f:.7f;
        }
        float MarineHunt(DeployedUnitState u,DefenderState d)
        {
            int stage=AdaptationStage(MarineAdaptation.Hunt);
            return stage>0&&u!=null&&!u.SynergySupport&&Tag(u.CardId,"海嗣")&&d.Health<d.MaxHealth*.35f?1+(stage==1?.12f:stage==2?.18f:.25f):1;
        }
        void OnMarineHit(DeployedUnitState u,DefenderState d,bool killed)
        {
            if(u.SynergySupport||!Tag(u.CardId,"海嗣"))return;
            int stage=AdaptationStage(MarineAdaptation.Infiltration);
            if(stage>0&&marineFirstHits.Add(u.EntityId))
            {
                float armor=stage==1?3:stage==2?5:7;
                if(!marinePenetration.TryGetValue(d.EntityId,out var p))marinePenetration[d.EntityId]=p=new Penetration();
                p.Armor=Mathf.Max(p.Armor,armor);p.Until=Math.Max(p.Until,Clock+(stage==1?4:5));
            }
            if(killed&&!marineMealUsed&&AdaptationStage(MarineAdaptation.Hunt)==3)
            {
                marineMealUsed=true;foreach(var ally in units.Where(o=>o.Active&&!o.IsReviving&&!o.SynergySupport&&GroundDistance(o.Motion.Position,d.Position)<=1.5f).OrderBy(o=>GroundDistance(o.Motion.Position,d.Position)).Take(2))HealUnit(ally,ally.MaxHealth*.05f);
            }
        }
        float MarineArmorReduction(DefenderState d)=>marinePenetration.TryGetValue(d.EntityId,out var p)&&p.Until>Clock?p.Armor:0;
    }
}
