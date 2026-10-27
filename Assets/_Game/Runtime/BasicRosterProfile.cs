using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    // Restricted companion profiles for the independent guerrilla trial, not a shop/formation system.
    public sealed class BasicRosterProfile
    {
        public ManualBattleSettings Unit {get;}
        public EngagementSettings Combat {get;}
        public BasicRosterProfile(ManualBattleSettings unit,EngagementSettings combat)
        {
            string[] allowed={"CARD_M02","CARD_M03","CARD_M05","CARD_ORIGINIUM_SLUG","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};
            if(!allowed.Contains(unit.CardId)||unit.CardId!="CARD_ORIGINIUM_SLUG"&&!unit.UnitId.StartsWith(unit.CardId.Substring(5)+"_",StringComparison.Ordinal)||combat.HasSkill||!combat.CanAttack)
                throw new ArgumentException("Only authored basic guerrilla trial roles are supported.");
            var e=combat.Effects;
            if(e.Patriot||e.FrostNova||e.ArtsBarrier||e.Mudrock||e.Phalanx||e.Healer||e.Flamechaser||e.Flamerazer||e.Mandragora||e.NervousCreeper||e.Harvester||e.FirstSpeaker||e.Shackler||
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
            if(snapshot.Any(p=>p==null||p.Unit.GateDamage!=Settings.GateDamage)||snapshot.Select(p=>p.Unit.CardId).Distinct().Count()!=snapshot.Length)
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
