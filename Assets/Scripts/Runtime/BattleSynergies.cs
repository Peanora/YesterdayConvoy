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
        public bool GuaranteedSupply {get;private set;}
        public void ConfigureSupply(){if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException();GuaranteedSupply=true;}
        public bool CompleteSupply(){if(!GuaranteedSupply||Phase!=BattlePhase.Ready)return false;Phase=BattlePhase.Won;IsPaused=false;return true;}
        bool synergyEnabled,reunionRanged;
        public void ConfigureSynergies(IEnumerable<ShopTrialModel.Piece> lineup,bool ranged=false,InfectionForm form=InfectionForm.Swarm,InfectionForm second=InfectionForm.Shell,int golemExperience=0,string contract=null,MarineAdaptation adaptation=MarineAdaptation.Barrier,MarineAdaptation secondAdaptation=MarineAdaptation.Hunt,int marineExperience=0,int golemInvestment=0)
        {
            if(Phase!=BattlePhase.Ready||units.Count>0)throw new InvalidOperationException("Snapshot before deployment");
            synergyStars.Clear();foreach(var p in lineup)if(CampaignBalance.Tags(p.Id).Length>0)synergyStars[p.Id]=p.Star;
            int reunion=synergyStars.Keys.Count(CampaignBalance.Reunion),snow=synergyStars.Keys.Count(CampaignBalance.Snow);
            ReunionTier=reunion>=6?6:reunion>=4?4:reunion>=2?2:0;SnowTier=snow>=5?5:snow>=2?2:0;
            reunionRanged=ranged;synergyEnabled=true;ConfigureAdvancedSynergies(form,second,golemExperience,golemInvestment);ConfigureRemainingSynergies(contract,adaptation,secondAdaptation,marineExperience);ResetSynergies();
        }
        void ResetSynergies(){ResetSnowstorms();ResetRemainingSynergies();ResetAdvancedSynergies();musterKinds.Clear();frozenHits.Clear();frozenHitEpoch.Clear();shatterCooldown.Clear();shatterEvents.Clear();MusterUsed=Shatters=0;ShatterDamage=0;}
        void OnMusterDeployment(DeployedUnitState trigger)
        {
            if(!synergyEnabled||trigger.SynergySupport||ReunionTier==0||!synergyStars.ContainsKey(trigger.CardId))return;
            int cap=ReunionTier==2?1:ReunionTier==4?2:3;
            if(MusterUsed>=cap)return;
            musterKinds.Add(trigger.CardId);if(musterKinds.Count<(ReunionTier==2?2:3))return;
            musterKinds.Clear();MusterUsed++;
            int count=ReunionTier==2?1:2;
            for(int i=0;i<count;i++)
            {
                var lane=lanes.First(l=>l.Id==trigger.RouteId);
                bool archer=ReunionTier==6?i==1:ReunionTier==4&&reunionRanged;
                var u=new DeployedUnitState($"run-{RunId}/support-{++sequence}",archer?"M08_ARCHER":"M07_SOLDIER",new RouteMotion(lane.Points,.7f),ReunionTier==2?80:ReunionTier==4?100:120,Clock)
                    {CardId=archer?"CARD_M08":"CARD_M07",RouteId=lane.Id,ExitId=lane.ExitId,SynergySupport=true};
                var combat=new EngagementSettings(unitAttack:ReunionTier==2?14:ReunionTier==4?18:22,unitArmor:0,unitRange:archer?2.7f:.75f,unitInterval:1.8f,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:!archer);
                var settings=new ManualBattleSettings(u.CardId,u.UnitId,1,1,1,1,1,u.MaxHealth,.7f,1,1,1,1);
                if(archer)u.MaxHealth=u.Health=u.Health*.8f;
                if(ReunionTier==6){u.SynergyShield=u.MaxHealth*.15f;u.SynergyShieldUntil=Clock+4;}
                u.Profile=new BasicRosterProfile(settings,combat,true);units.Add(u);
            }
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
