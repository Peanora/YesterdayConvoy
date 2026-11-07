using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum WolfBoxKind { None, Sword, Bomb, Bean }

    public static class SilverWolfCombat
    {
        public const string CardId="CARD_M41";
        public const double TransformDuration=1.43, SwordFinish=2.96;
        public static bool IsWolf(DeployedUnitState u)=>u?.CardId==CardId;
        public static bool Transforming(DeployedUnitState u,double clock)=>IsWolf(u)&&clock>=u.WolfTransformAt&&clock<u.WolfTransformAt+TransformDuration;
        public static double HitDelay(WolfBoxKind box)=>box==WolfBoxKind.Sword?1.76:box==WolfBoxKind.Bomb?2.54:2.29;
    }

    public sealed partial class ManualBattle
    {
        // Model-owned stream: display updates and opening a skill menu never roll a box.
        System.Random wolfRolls;
        void StartWolfShot(DeployedUnitState u,DefenderState target,EngagementSettings c)
        {
            if(wolfRolls==null)wolfRolls=new System.Random(Guid.NewGuid().GetHashCode());
            var box=(WolfBoxKind)(1+wolfRolls.Next(3));
            u.LastWolfBox=box;
            combatShots.Add(new CombatShot(u.EntityId,target.EntityId,false,true,u.Motion.Position,target.Position,
                Clock,(float)SilverWolfCombat.HitDelay(box),c.SkillAttack*UnitAttackMultiplier(u),damageType:CombatDamageType.Arts){WolfBox=box,WolfActionAt=Clock});
        }

        void ResolveWolfBox(CombatShot shot,DeployedUnitState u)
        {
            if(u==null||!u.Active||u.IsReviving||StrongholdInTransit(u.EntityId))return;
            int count=0;
            foreach(var d in defenders)if(d.Alive)count++;
            if(count==0)return;
            shot.Applied=true;
            foreach(var d in defenders)
            {
                if(!d.Alive)continue;
                float before=d.Health;
                DamageDefender(d,shot.Attack/count,CombatDamageType.Arts,source:u);
                shot.DamageDealt+=before-d.Health;
                OnSynergyDirectHit(u,d);
            }
            RecordSynergyContact(u);
            if(shot.WolfBox==WolfBoxKind.Bomb)DP+=2;
            if(shot.WolfBox==WolfBoxKind.Bean)u.SkillSP=Mathf.Min(UnitCombat(u).RequiredSP,u.SkillSP+3);
        }
        void FinishWolfSword(CombatShot shot)
        {
            shot.WolfFinished=true;
            var source=FindUnit(shot.SourceId);
            if(!shot.Applied||source==null||!source.Active||source.IsReviving||StrongholdInTransit(source.EntityId))return;
            DefenderState highest=null;
            foreach(var d in defenders)if(d.Alive&&(highest==null||d.Health>highest.Health))highest=d;
            if(highest==null)return;
            shot.WolfBonusTargetId=highest.EntityId;
            float before=highest.Health;TrueDamage(highest,shot.DamageDealt*.20f);shot.DamageDealt+=before-highest.Health;
        }
    }
}
