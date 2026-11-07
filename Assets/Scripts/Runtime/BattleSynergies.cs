using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class ShatterEvent
    {
        public Vector3 Position;public double At;public float Radius;public int Hits;
    }
    public sealed partial class ManualBattle
    {
        public int ReunionTier {get;private set;}
        public int ReunionCount {get;private set;}
        public int ReunionFormationCount {get;private set;}
        public float ReunionAverageStar {get;private set;}=1;
        public int ReunionMusterLimit=>ReunionRules.MusterLimit(ReunionTier);
        public int ReunionMusterRequirement=>ReunionRules.KindsPerMuster(ReunionTier);
        public IReadOnlyList<string> MusterMemberIds=>Array.AsReadOnly(musterKinds.OrderBy(id=>id,StringComparer.Ordinal).ToArray());
        public int SnowTier {get;private set;}
        public int MusterUsed {get;private set;}
        public int Shatters {get;private set;}
        public float ShatterDamage {get;private set;}
        public IReadOnlyList<ShatterEvent> ShatterEvents=>shatterEvents.AsReadOnly();
        readonly List<ShatterEvent> shatterEvents=new List<ShatterEvent>();
        readonly HashSet<string> musterKinds=new HashSet<string>();
        readonly Dictionary<string,Dictionary<string,double>> frozenHits=new Dictionary<string,Dictionary<string,double>>();
        readonly Dictionary<string,double> shatterCooldown=new Dictionary<string,double>();
        readonly Dictionary<string,double> frozenHitEpoch=new Dictionary<string,double>();
        readonly Dictionary<string,int> synergyStars=new Dictionary<string,int>();
        readonly HashSet<string> guerrillaMembers=new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<string> guerrillaMemberIds=Array.Empty<string>();
        public IReadOnlyList<string> GuerrillaMemberIds=>guerrillaMemberIds;
        public bool IsGuerrillaMember(string id)=>id!=null&&guerrillaMembers.Contains(id);
        readonly HashSet<string> mudrockMembers=new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<string> mudrockMemberIds=Array.Empty<string>();
        public IReadOnlyList<string> MudrockMemberIds=>mudrockMemberIds;
        public bool IsMudrockMember(string id)=>id!=null&&mudrockMembers.Contains(id);
        public int MudrockCrewCount=>mudrockMembers.Count;
        public bool GuaranteedSupply {get;private set;}
        public void ConfigureSupply(){if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException();GuaranteedSupply=true;}
        public bool CompleteSupply(){if(!GuaranteedSupply||Phase!=BattlePhase.Ready)return false;Phase=BattlePhase.Won;IsPaused=false;return true;}
        bool synergyEnabled,reunionRanged;
        public void ConfigureSynergies(IEnumerable<ShopTrialModel.Piece> lineup,bool ranged=false,InfectionForm form=InfectionForm.Swarm,InfectionForm second=InfectionForm.Shell,int golemExperience=0,string contract=null,MarineAdaptation adaptation=MarineAdaptation.Barrier,MarineAdaptation secondAdaptation=MarineAdaptation.Hunt,int marineExperience=0,IEnumerable<string> extraGuerrillaMembers=null,IEnumerable<string> extraMudrockMembers=null)
        {
            if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException("Snapshot before deployment");
            // Validate and materialize external enumerables before mutating any existing snapshot.
            var pieces=lineup?.ToArray()??throw new ArgumentNullException(nameof(lineup));
            if(pieces.Any(p=>p==null||string.IsNullOrWhiteSpace(p.Id)||p.Star<1||
                p.Star>(CampaignBalance.Profiles.TryGetValue(p.Id,out var profile)?ShopTrialModel.MaxStarForCost(profile.Cost):3)))
                throw new ArgumentException("Invalid lineup.",nameof(lineup));
            if(golemExperience<0||!Enum.IsDefined(typeof(InfectionForm),form)||!Enum.IsDefined(typeof(InfectionForm),second))throw new ArgumentException("Invalid golem experience or infection form.");
            var lineupIds=new HashSet<string>(pieces.Select(p=>p.Id),StringComparer.Ordinal);
            var extras=new HashSet<string>(StringComparer.Ordinal);
            foreach(string id in extraGuerrillaMembers??Enumerable.Empty<string>())
            {
                if(string.IsNullOrWhiteSpace(id)||!CampaignBalance.Supports(id)||!lineupIds.Contains(id))
                    throw new ArgumentException("Extra guerrilla members must be known cards in this formation.",nameof(extraGuerrillaMembers));
                if(!CampaignBalance.Guerrilla(id))extras.Add(id);
            }
            if(extras.Count>2)throw new ArgumentException("At most two non-native guerrilla kinds may be assigned.",nameof(extraGuerrillaMembers));
            var mudrockExtras=new HashSet<string>(StringComparer.Ordinal);
            foreach(string id in extraMudrockMembers??Enumerable.Empty<string>())
            {
                if(string.IsNullOrWhiteSpace(id)||!CampaignBalance.Supports(id)||!lineupIds.Contains(id))
                    throw new ArgumentException("Extra Mudrock members must be known cards in this formation.",nameof(extraMudrockMembers));
                if(!CampaignBalance.MudrockCrew(id))mudrockExtras.Add(id);
            }
            if(mudrockExtras.Count>1)throw new ArgumentException("At most one non-native Mudrock kind may be assigned.",nameof(extraMudrockMembers));
            var members=pieces.Select(p=>p.Id).Where(id=>CampaignBalance.Guerrilla(id)||extras.Contains(id)).Distinct(StringComparer.Ordinal).ToArray();
            var crew=pieces.Select(p=>p.Id).Where(id=>CampaignBalance.MudrockCrew(id)||mudrockExtras.Contains(id)).Distinct(StringComparer.Ordinal).ToArray();
            synergyStars.Clear();foreach(var p in pieces)if(CampaignBalance.Tags(p.Id).Length>0)
            {synergyStars.TryGetValue(p.Id,out int previous);synergyStars[p.Id]=Math.Max(previous,p.Star);}
            guerrillaMembers.Clear();foreach(string id in members)guerrillaMembers.Add(id);
            guerrillaMemberIds=Array.AsReadOnly(members);
            mudrockMembers.Clear();foreach(string id in crew)mudrockMembers.Add(id);
            mudrockMemberIds=Array.AsReadOnly(crew);
            var reunion=synergyStars.Keys.Where(CampaignBalance.Reunion).ToArray();
            ReunionCount=reunion.Length;ReunionFormationCount=ReunionRules.FormationCount(reunion);
            ReunionAverageStar=reunion.Length==0?1:(float)reunion.Average(id=>synergyStars[id]);
            ReunionTier=ReunionRules.TierForCount(ReunionCount,ReunionFormationCount);
            int snow=synergyStars.Keys.Count(CampaignBalance.Snow);SnowTier=snow>=5?5:snow>=2?2:0;
            reunionRanged=ranged;synergyEnabled=true;ConfigureAdvancedSynergies(form,second,golemExperience);ConfigureRemainingSynergies(contract,adaptation,secondAdaptation,marineExperience);ResetSynergies();
        }
        void ResetSynergies(){ResetSnowstorms();ResetRemainingSynergies();ResetAdvancedSynergies();musterKinds.Clear();frozenHits.Clear();frozenHitEpoch.Clear();shatterCooldown.Clear();shatterEvents.Clear();MusterUsed=Shatters=0;ShatterDamage=0;}
        void OnMusterDeployment(DeployedUnitState trigger)
        {
            if(!synergyEnabled||trigger.SynergySupport||ReunionTier==0||!CampaignBalance.Reunion(trigger.CardId)||!synergyStars.ContainsKey(trigger.CardId))return;
            if(MusterUsed>=ReunionMusterLimit)return;
            musterKinds.Add(trigger.CardId);if(musterKinds.Count<ReunionMusterRequirement)return;
            musterKinds.Clear();MusterUsed++;
            int count=ReunionRules.SupportsPerMuster(ReunionTier);
            for(int i=0;i<count;i++)
            {
                var lane=lanes.First(l=>l.Id==trigger.RouteId);
                bool archer=ReunionTier>=6?i==1:ReunionTier==4&&reunionRanged;
                var stats=ReunionRules.ForSupport(ReunionTier,archer,ReunionAverageStar);
                var u=new DeployedUnitState($"run-{RunId}/support-{++sequence}",archer?"M08_ARCHER":"M07_SOLDIER",new RouteMotion(lane.Points,.7f),stats.Health,Clock)
                    {CardId=archer?"CARD_M08":"CARD_M07",RouteId=lane.Id,ExitId=lane.ExitId,SynergySupport=true,ReunionEliteSupport=ReunionTier==8,ReunionEntryShieldPending=ReunionTier==8};
                var combat=new EngagementSettings(unitAttack:stats.Attack,unitArmor:stats.Armor,unitRange:stats.Range,unitInterval:1.8f,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:!archer);
                var settings=new ManualBattleSettings(u.CardId,u.UnitId,1,1,1,1,1,u.MaxHealth,.7f,1,1,1,1);
                u.Profile=new BasicRosterProfile(settings,combat,true);units.Add(u);
            }
        }
        void TryActivateReunionEntryShield(DeployedUnitState unit)
        {
            if(!unit.Active||!unit.ReunionEliteSupport||!unit.ReunionEntryShieldPending)return;
            unit.ReunionEntryShieldPending=false;
            unit.SynergyShield=Mathf.Floor(unit.MaxHealth*.2f+.5f);
            unit.SynergyShieldUntil=Clock+5;
        }
        void OnSynergyDirectHit(DeployedUnitState source,DefenderState target)
        {
            if(!synergyEnabled||SnowTier==0||source.SynergySupport||target.FrozenUntil<=Clock)return;
            if(shatterCooldown.TryGetValue(target.EntityId,out var until)&&Clock<until)return;
            if(!frozenHits.TryGetValue(target.EntityId,out var hits))frozenHits[target.EntityId]=hits=new Dictionary<string,double>();
            if(!frozenHitEpoch.TryGetValue(target.EntityId,out var epoch)||epoch!=target.FrozenUntil){hits.Clear();frozenHitEpoch[target.EntityId]=target.FrozenUntil;}
            foreach(var key in hits.Where(p=>Clock-p.Value>2.5).Select(p=>p.Key).ToArray())hits.Remove(key);
            hits[source.CardId]=Clock;if(hits.Count<2)return;
            hits.Clear();shatterCooldown[target.EntityId]=Clock+(SnowTier==5?3:4);
            if(SnowTier==2)target.FrozenUntil=target.ColdUntil=Clock;
            float avg=(float)synergyStars.Where(p=>CampaignBalance.Snow(p.Key)).Average(p=>p.Value);
            float attack=(SnowTier==2?50:120)*(1+.35f*(avg-1)),radius=SnowTier==2?.9f:1.5f;
            var ev=new ShatterEvent{Position=target.Position,At=Clock,Radius=radius};shatterEvents.Add(ev);Shatters++;
            foreach(var d in defenders.Where(d=>d.Alive&&GroundDistance(d.Position,target.Position)<=radius).OrderBy(d=>GroundDistance(d.Position,target.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).Take(SnowTier==2?2:5).ToArray())
            {
                float before=d.Health;DamageDefender(d,attack,CombatDamageType.Arts,true);ShatterDamage+=before-d.Health;ev.Hits++;
            }
            OnSnowstormShatter();
        }
    }
}
