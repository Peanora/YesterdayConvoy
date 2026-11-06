using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    // Authored companion profiles, plus the explicitly approved Talulah and FrostNova shop trials.
    public sealed class BasicRosterProfile
    {
        public ManualBattleSettings Unit {get;}
        public EngagementSettings Combat {get;}
        public BasicRosterProfile(ManualBattleSettings unit,EngagementSettings combat,bool campaign=false)
        {
            if(campaign){if(!CampaignBalance.Supports(unit.CardId)&&unit.CardId!=CampaignBalance.GolemCardId)throw new ArgumentException("Unsupported campaign card");Unit=unit;Combat=combat;return;}
            string[] allowed={"CARD_M02","CARD_M03","CARD_M05","CARD_M08","CARD_ORIGINIUM_SLUG","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};
            bool talulah=unit.CardId=="CARD_TALULAH_TRIAL"&&unit.UnitId=="TALULAH"&&combat.HasSkill&&combat.FireRain&&combat.CanAttack;
            bool frost=unit.CardId=="CARD_M14"&&combat.FrostSeal&&combat.Effects.FrostNova&&combat.HasSkill&&combat.CanAttack;
            bool mandra=unit.CardId=="CARD_M32"&&unit.UnitId.StartsWith("M32_",StringComparison.Ordinal)&&combat.Effects.Mandragora&&!combat.HasSkill&&combat.CanAttack;
            bool mephi=unit.CardId=="CARD_M25"&&unit.UnitId=="M25_MEPHISTO"&&combat.Effects.Healer&&!combat.HasSkill&&!combat.CanAttack;
            bool patriot=unit.CardId=="CARD_M20"&&unit.UnitId=="M20_PATRIOT"&&combat.Effects.Patriot&&!combat.HasSkill&&combat.CanAttack;
            if(!talulah&&!frost&&!mandra&&!mephi&&!patriot&&(!allowed.Contains(unit.CardId)||unit.CardId!="CARD_ORIGINIUM_SLUG"&&!unit.UnitId.StartsWith(unit.CardId.Substring(5)+"_",StringComparison.Ordinal)||combat.HasSkill||!combat.CanAttack))
                throw new ArgumentException("Only authored basic guerrilla trial roles are supported.");
            var e=combat.Effects;
            if(e.Patriot&&!patriot||e.FrostNova&&!frost||e.ArtsBarrier||e.Mudrock||e.Phalanx||e.Healer&&!mephi||e.Flamechaser||e.Flamerazer||e.Mandragora&&!mandra||e.NervousCreeper||e.Harvester||e.FirstSpeaker||e.Shackler||
                e.RegenerationFraction>0||e.LifeSteal>0||e.BlastAttack>0||e.ColdAttackEvery>0||e.FrozenAttackMultiplier>1||(e.ArmorShred>0&&unit.CardId!="CARD_M05")||e.AttackTargets>1)
                throw new ArgumentException("This roster supports basic unit profiles and tactical orders only.");
            Unit=unit;Combat=combat;
        }
    }
    public sealed partial class ManualBattle
    {
        readonly Dictionary<string,BasicRosterProfile> rosterProfiles=new Dictionary<string,BasicRosterProfile>();
        public EngagementSettings UnitCombat(DeployedUnitState u)=>u?.Profile?.Combat??CombatSettings;
        public void ConfigureRosterProfiles(IEnumerable<BasicRosterProfile> profiles)
        {
            if(!EngagementEnabled||Phase!=BattlePhase.Ready||units.Count!=0)throw new InvalidOperationException("Configure roster before deployment.");
            var snapshot=(profiles??throw new ArgumentNullException(nameof(profiles))).ToArray();
            if(snapshot.Any(p=>p==null)||snapshot.Select(p=>p.Unit.CardId).Distinct().Count()!=snapshot.Length)
                throw new ArgumentException("Duplicate, missing or incompatible roster profile.");
            rosterProfiles.Clear();foreach(var p in snapshot)rosterProfiles.Add(p.Unit.CardId,p);
        }
        public DeploymentReceipt TryDeployRole(int runId,long requestId,UnityEngine.Vector3 position,string cardId,bool allowWhilePaused=false)
        {
            if(cardId==Settings.CardId)return DeployProfile(runId,requestId,position,allowWhilePaused,null);
            if(cardId==null||!rosterProfiles.TryGetValue(cardId,out var profile))return new DeploymentReceipt(DeployFailure.MissingCard);
            return DeployProfile(runId,requestId,position,allowWhilePaused,profile);
        }
        public int TacticalCommanders=>units.Count(u=>u.Active&&!u.IsReviving&&UnitCombat(u).Effects.TacticalCommander);
        public bool HasTacticalCommand=>TacticalCommanders>0||PatriotPresent;
        public float TacticalAttackMultiplier=>1+TacticalCommanders*.1f+(PatriotPresent?.2f:0);
        public float TacticalArmorBonus=>TacticalCommanders+(PatriotPresent?2:0);
        public int UnitAttackTargets(DeployedUnitState u)=>Math.Max(Math.Max(1,UnitCombat(u).Effects.AttackTargets),UnitCombat(u).Effects.OrderSniper&&HasTacticalCommand?2:1);
    }
}
