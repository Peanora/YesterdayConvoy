using System;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        public bool CampaignV2Enabled {get;private set;}
        public int CampaignV2Stage {get;private set;}
        public void ConfigureCampaignV2(int stage=0)
        {
            if(Phase!=BattlePhase.Ready||units.Count!=0||!EngagementEnabled)throw new InvalidOperationException("Configure campaign v2 before battle.");
            if(stage<0||stage>CampaignJourneyDefinition.StageCount)throw new ArgumentOutOfRangeException(nameof(stage));
            _=CampaignV2Balance.PlayerCount;CampaignV2Enabled=true;CampaignV2Stage=stage;
        }
        internal float V2Skill(DeployedUnitState u,string skillId,string key,float legacy)
        {
            if(!CampaignV2Enabled||u==null||u.SynergySupport||u.CardId==SilverWolfCombat.CardId)return legacy;
            return CampaignV2Balance.SkillValue(u.CardId,CampaignV2Balance.StarFor(u.Profile?.Unit??Settings),skillId,key);
        }
        internal float V2Synergy(string id,int tier,string key,float legacy)=>!CampaignV2Enabled||tier==0?legacy:CampaignV2Balance.SynergyValue(id,tier,key);
        internal float V2Summon(int stage,string key,float legacy)=>!CampaignV2Enabled?legacy:CampaignV2Balance.SummonValue(stage,key);
        internal float V2IndependentAttackMultiplier(DeployedUnitState unit)
        {
            if(!CampaignV2Enabled)return UnitAttackMultiplier(unit);
            // Final independent skill budgets exclude normal-attack phase/ramp
            // growth, but retain team command/contract modifiers exactly once.
            return (TacticalAttackMultiplier+ContractAttackBonus(unit)+ReunionMemberBonus(unit)-
                (IsPatriot(unit)&&unit.PatriotPhase!=PatriotPhase.March?V2Skill(unit,"dominion","attackBonus",.2f):0))*InfectionAttack(unit.EntityId);
        }
        void ApplyCampaignV2Deployment(DeployedUnitState unit)
        {
            if(!CampaignV2Enabled||unit.SynergySupport||unit.CardId==SilverWolfCombat.CardId)return;
            var combat=UnitCombat(unit);if(!combat.Effects.ArtsBarrier&&!combat.Effects.Mudrock)return;
            int star=CampaignV2Balance.StarFor(unit.Profile?.Unit??Settings);
            float Value(string key)=>CampaignV2Balance.SkillValue(unit.CardId,star,"artsBarrier",key);
            unit.MaxHealth=unit.Health=UnitBaseHealth(unit)*Value("maximumHpMultiplier");
            unit.ArtsBarrierLayerSize=Value("shieldPerLayer");unit.ArtsBarrierRemaining=Value("shieldTotal");
            unit.NextBarrierRefreshAt=Clock+(combat.Effects.Mudrock?V2Skill(unit,"refresh","initialDelay",17):17);
        }
        float V2TacticalAttackMultiplier=>1+units.Where(u=>u.Active&&!u.IsReviving&&UnitCombat(u).Effects.TacticalCommander)
            .Sum(u=>V2Skill(u,"tacticalCommand","attackBonus",.1f))+
            (PatriotPresent?units.Where(u=>u.Active&&IsPatriot(u)).Max(u=>V2Skill(u,"dominion","attackBonus",.2f)):0);
        float V2TacticalArmorBonus=>units.Where(u=>u.Active&&!u.IsReviving&&UnitCombat(u).Effects.TacticalCommander)
            .Sum(u=>V2Skill(u,"tacticalCommand","armorBonus",1))+
            (PatriotPresent?units.Where(u=>u.Active&&IsPatriot(u)).Max(u=>V2Skill(u,"dominion","armorBonus",2)):0);
    }
}
