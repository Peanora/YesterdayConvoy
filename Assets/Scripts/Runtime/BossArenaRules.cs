using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class BossArenaRules
    {
        public const string Id="wisadel-boss-arena", SceneName="WisadelBossArena", BossId="arena-wisadel";
        public readonly float RedeploymentSeconds, EnragedAttackSpeed;
        public BossArenaRules(float redeploymentSeconds=90,float enragedAttackSpeed=2)
        {
            if(float.IsNaN(redeploymentSeconds)||float.IsInfinity(redeploymentSeconds)||redeploymentSeconds<60||redeploymentSeconds>120||
                float.IsNaN(enragedAttackSpeed)||float.IsInfinity(enragedAttackSpeed)||enragedAttackSpeed<=1||enragedAttackSpeed>4)
                throw new ArgumentOutOfRangeException(nameof(redeploymentSeconds));
            RedeploymentSeconds=redeploymentSeconds;EnragedAttackSpeed=enragedAttackSpeed;
        }
    }

    public sealed partial class ManualBattle
    {
        BossArenaRules arenaRules;
        readonly Dictionary<string,double> arenaReadyAt=new Dictionary<string,double>();
        readonly HashSet<string> arenaDeployed=new HashSet<string>();
        long arenaRequest=long.MaxValue/2;
        public bool BossArenaEnabled=>arenaRules!=null;
        public DefenderState ArenaBoss=>BossArenaEnabled?FindDefender(BossArenaRules.BossId):null;
        public bool ArenaEnraged=>ArenaBoss!=null&&ArenaBoss.Alive&&ArenaBoss.Health<ArenaBoss.MaxHealth*.5f;
        public float ArenaAttackSpeed=>ArenaEnraged?arenaRules.EnragedAttackSpeed:1;
        public int ArenaDeployments {get;private set;}
        public double ArenaEnragedAt {get;private set;}=double.PositiveInfinity;
        public bool ArenaCardOnField(string id)=>BossArenaEnabled&&units.Any(u=>u.CardId==id&&!u.SynergySupport&&u.Active);
        public double ArenaRedeploymentRemaining(string id)=>BossArenaEnabled&&arenaReadyAt.TryGetValue(id,out var ready)?Math.Max(0,ready-Clock):0;
        public void ConfigureBossArena(BossArenaRules rules)
        {
            if(Phase!=BattlePhase.Ready||units.Count!=0||!HasCardLoadout||defenders.Count!=1||defenders[0].EntityId!=BossArenaRules.BossId)
                throw new InvalidOperationException("An arena requires a ready loadout and its single authored boss.");
            arenaRules=rules??throw new ArgumentNullException(nameof(rules));ResetBossArena();
        }
        void ResetBossArena()
        {arenaReadyAt.Clear();arenaDeployed.Clear();ArenaDeployments=0;ArenaEnragedAt=double.PositiveInfinity;arenaRequest=long.MaxValue/2;}
        DeployFailure CheckArenaDeployment(string id)
        {
            if(ArenaCardOnField(id))return DeployFailure.EntryBusy;
            return ArenaRedeploymentRemaining(id)>1e-6?DeployFailure.CoolingDown:DeployFailure.None;
        }
        void CommitArenaDeployment(string id)
        {arenaDeployed.Add(id);arenaReadyAt.Remove(id);ArenaDeployments++;}
        public int DeployReadyArenaSquad(bool allowWhilePaused=false)
        {
            if(!BossArenaEnabled||Phase!=BattlePhase.Running||IsPaused&&!allowWhilePaused)return 0;
            int count=0;
            for(int i=0;i<cards.Count;i++)
            {
                var lane=lanes[i%lanes.Count];
                if(TryDeployRole(RunId,++arenaRequest,lane.Entry,cards[i].Settings.CardId,allowWhilePaused).Success)count++;
            }
            return count;
        }
        void StepBossArena()
        {
            if(!BossArenaEnabled)return;
            if(ArenaEnraged&&double.IsPositiveInfinity(ArenaEnragedAt))ArenaEnragedAt=Clock;
            foreach(string id in arenaDeployed.ToArray())
                if(!ArenaCardOnField(id))
                {arenaReadyAt[id]=Clock+arenaRules.RedeploymentSeconds;arenaDeployed.Remove(id);}
        }
        bool StepArenaBossAttack(DefenderState d)
        {
            if(!BossArenaEnabled||d.EntityId!=BossArenaRules.BossId)return false;
            var target=FindUnit(d.TargetId);
            if(!CanDefenderTarget(target)||GroundDistance(target.Motion.Position,d.Position)>d.Settings.Range)
            {
                target=null;
                foreach(var unit in units)
                    if(CanDefenderTarget(unit)&&GroundDistance(unit.Motion.Position,d.Position)<=d.Settings.Range&&PreferDefenderTarget(d,unit,target))target=unit;
                d.TargetId=target?.EntityId;
            }
            if(target==null||Clock+1e-6<d.NextAttackTime)return true;
            d.LastActionTime=Clock;d.LastActionHealing=false;d.LastAttackSkill=true;d.LastActionRate=ArenaAttackSpeed*DefenderAttackRate(d);
            d.NextAttackTime=Clock+d.Settings.Interval/d.LastActionRate;
            ShotsFired++;LastShotTime=Clock;LastShotEntityId=target.EntityId;LastShotPosition=target.Motion.Position;
            combatShots.Add(new CombatShot(d.EntityId,target.EntityId,true,true,d.Position,target.Motion.Position,
                Clock+d.Settings.Windup/d.LastActionRate,CombatSettings.ProjectileTime,d.Settings.Attack,
                damageType:d.Settings.DamageType,splashRadius:d.Settings.SplashRadius));
            return true;
        }
    }
}
