using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        sealed class ContractWard { internal float Amount; internal double Until; }
        readonly Dictionary<string,ContractWard> contractWards=new Dictionary<string,ContractWard>();
        readonly Dictionary<string,double> contractNextWards=new Dictionary<string,double>();
        readonly Dictionary<long,string> contractRequests=new Dictionary<long,string>();
        readonly HashSet<string> contractWindowRecipients=new HashSet<string>();
        double contractUntil,contractReadyAt;
        public int ContractPoints=>0; // Retired compatibility read; damage no longer charges this trait.
        public int ContractUses {get;private set;}
        public double ContractCooldownRemaining=>Math.Max(0,contractReadyAt-Clock);
        public double ContractRemaining=>SarkazTier==9?double.PositiveInfinity:Math.Max(0,contractUntil-Clock);
        public double ContractNextShieldRemaining=>SarkazTier==9?units.Where(IsContractBeneficiary).Select(ContractShieldRefreshRemaining).DefaultIfEmpty(0).Min():0;
        public bool CanActivateContract=>synergyEnabled&&SarkazTier>0&&SarkazTier<9&&Phase==BattlePhase.Running&&!IsPaused&&ContractCooldownRemaining<=.000001&&units.Any(IsContractBeneficiary);
        public float SarkazHealthMultiplier(string cardId)=>synergyEnabled&&SarkazTier>0&&Tag(cardId,"萨卡兹")?1+(SarkazTier==2?.10f:SarkazTier==4?.20f:SarkazTier==6?.30f:SarkazTier==8?.40f:.60f):1;
        float UnitBaseHealth(DeployedUnitState u)=>(u.Profile?.Unit.Health??Settings.Health)*(u.SynergySupport?1:SarkazHealthMultiplier(u.CardId));
        bool ContractActionable(DeployedUnitState u)=>u!=null&&u.Active&&!u.SynergySupport&&!u.IsReviving&&!u.FuelDying&&!PatriotReborn(u)&&!TalulahTransitionLocked(u)&&u.MandraPhase!=MandragoraPhase.Reborn&&u.FlamePhase==FlamechaserPhase.Soldier;
        public bool IsContractBeneficiary(DeployedUnitState u)=>synergyEnabled&&SarkazTier>0&&ContractActionable(u)&&(u.CardId==ContractCard||SarkazTier>=6&&Tag(u.CardId,"萨卡兹"));
        float ContractShare(DeployedUnitState u)=>u.CardId==ContractCard||SarkazTier>=8?1:.5f;
        float ContractAttack=>SarkazTier==2?.35f:SarkazTier==4?.60f:SarkazTier==6?.90f:SarkazTier==8?1.20f:1.50f;
        float ContractShield=>SarkazTier==2?.20f:SarkazTier==4?.30f:SarkazTier==6?.40f:SarkazTier==8?.50f:.60f;
        double ContractDuration=>SarkazTier==2?8:SarkazTier==4?10:SarkazTier==6?12:15;
        public float ContractAttackBonus(DeployedUnitState u)=>IsContractBeneficiary(u)&&(SarkazTier==9||contractUntil>Clock)?ContractAttack*ContractShare(u):0;
        public float ContractShieldRemaining(DeployedUnitState u)=>ContractActionable(u)&&contractWards.TryGetValue(u.EntityId,out var ward)&&ward.Until>Clock?ward.Amount:0;
        public double ContractShieldRefreshRemaining(DeployedUnitState u)=>SarkazTier==9&&IsContractBeneficiary(u)&&contractNextWards.TryGetValue(u.EntityId,out var at)?Math.Max(0,at-Clock):0;
        void ResetContract()
        {
            contractWards.Clear();contractNextWards.Clear();contractRequests.Clear();contractWindowRecipients.Clear();contractUntil=contractReadyAt=0;ContractUses=0;
        }
        public bool TryActivateContract(int runId,long request,string entity=null)
        {
            if(runId!=RunId||Phase!=BattlePhase.Running||IsPaused)return false;
            // A retry acknowledges its original command without granting another shield or moving the cooldown.
            if(contractRequests.TryGetValue(request,out var prior))return prior==entity;
            if(!CanActivateContract)return false;
            if(entity!=null){var chosen=FindUnit(entity);if(!IsContractBeneficiary(chosen)||chosen.CardId!=ContractCard)return false;}
            contractRequests.Add(request,entity);ContractUses++;contractUntil=Clock+ContractDuration;contractReadyAt=Clock+20;contractWindowRecipients.Clear();
            foreach(var u in units)ApplyContractWindow(u);
            return true;
        }
        void GrantContractWard(DeployedUnitState u,double until)
        {
            float remaining=ContractShieldRemaining(u),amount=u.MaxHealth*ContractShield*ContractShare(u);
            contractWards[u.EntityId]=new ContractWard{Amount=Mathf.Max(remaining,amount),Until=until};
        }
        void ApplyContractWindow(DeployedUnitState u)
        {
            if(!IsContractBeneficiary(u))return;
            if(SarkazTier==9)
            {
                if(!contractNextWards.TryGetValue(u.EntityId,out var next)||Clock+.000001>=next)
                {GrantContractWard(u,double.PositiveInfinity);contractNextWards[u.EntityId]=Clock+20;}
            }
            else if(contractUntil>Clock&&contractWindowRecipients.Add(u.EntityId))GrantContractWard(u,contractUntil);
        }
        void StepContract(){if(!synergyEnabled||SarkazTier==0)return;foreach(var u in units)ApplyContractWindow(u);}
        float AbsorbContractShield(DeployedUnitState u,float damage)
        {
            float absorbed=Mathf.Min(damage,ContractShieldRemaining(u));
            if(absorbed>0){contractWards[u.EntityId].Amount-=absorbed;RecordUnitShieldAbsorption(u,absorbed);}
            return damage-absorbed;
        }
    }
}
