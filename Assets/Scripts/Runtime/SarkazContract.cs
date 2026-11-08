using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class ContractWard { internal float Amount; internal double Until=double.PositiveInfinity; }
        readonly Dictionary<string,ContractWard> contractWards=new Dictionary<string,ContractWard>();
        readonly HashSet<string> contractRecipients=new HashSet<string>();
        // Compatibility reads for retired active-skill callers. Deployment is the only trigger.
        public int ContractPoints=>0;
        public int ContractUses=>0;
        public double ContractCooldownRemaining=>0;
        public double ContractRemaining=>SarkazTier>0?double.PositiveInfinity:0;
        public double ContractNextShieldRemaining=>0;
        public bool CanActivateContract=>false;
        public float SarkazHealthMultiplier(string cardId)=>synergyEnabled&&SarkazTier>0&&Tag(cardId,"萨卡兹")?1+V2Synergy("sarkaz",SarkazTier,"allSarkazHPAdd",SarkazTier==2?.10f:SarkazTier==4?.20f:SarkazTier==6?.30f:SarkazTier==8?.40f:.60f):1;
        float UnitBaseHealth(DeployedUnitState u)=>(u.Profile?.Unit.Health??Settings.Health)*(u.SynergySupport?1:SarkazHealthMultiplier(u.CardId));
        bool ContractActionable(DeployedUnitState u)=>u!=null&&u.Active&&!u.SynergySupport&&!u.IsReviving&&!u.FuelDying&&!PatriotReborn(u)&&!TalulahTransitionLocked(u)&&u.MandraPhase!=MandragoraPhase.Reborn&&u.FlamePhase==FlamechaserPhase.Soldier;
        public bool IsContractBeneficiary(DeployedUnitState u)=>synergyEnabled&&SarkazTier>0&&ContractActionable(u)&&Tag(u.CardId,"萨卡兹")&&(u.CardId==ContractCard||SarkazTier>=6);
        float ContractShare(DeployedUnitState u)=>u.CardId==ContractCard?1:V2Synergy("sarkaz",SarkazTier,"otherShareFraction",SarkazTier>=8?1:.5f);
        float ContractAttack=>V2Synergy("sarkaz",SarkazTier,"holderAttackAdd",SarkazTier==2?.35f:SarkazTier==4?.60f:SarkazTier==6?.90f:SarkazTier==8?1.20f:1.50f);
        float ContractShield=>V2Synergy("sarkaz",SarkazTier,"holderShieldMaxHPFraction",SarkazTier==2?.20f:SarkazTier==4?.30f:SarkazTier==6?.40f:SarkazTier==8?.50f:.60f);
        public float ContractAttackBonus(DeployedUnitState u)=>IsContractBeneficiary(u)?ContractAttack*ContractShare(u):0;
        public float ContractShieldRemaining(DeployedUnitState u)=>ContractActionable(u)&&contractWards.TryGetValue(u.EntityId,out var ward)?ward.Amount:0;
        public double ContractShieldRefreshRemaining(DeployedUnitState u)=>0;
        void ResetContract(){contractWards.Clear();contractRecipients.Clear();}
        public bool TryActivateContract(int runId,long request,string entity=null)=>false;
        void ApplyContractDeployment(DeployedUnitState u)
        {
            if(!IsContractBeneficiary(u)||!contractRecipients.Add(u.EntityId))return;
            contractWards[u.EntityId]=new ContractWard{Amount=u.MaxHealth*ContractShield*ContractShare(u)};
        }
        float AbsorbContractShield(DeployedUnitState u,float damage)
        {
            float absorbed=Mathf.Min(damage,ContractShieldRemaining(u));
            if(absorbed>0){contractWards[u.EntityId].Amount-=absorbed;RecordUnitShieldAbsorption(u,absorbed);}
            return damage-absorbed;
        }
    }
}
