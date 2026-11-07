using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyRemainingSynergies()
        {
            bool pool=shop.Roles.Count==43&&shop.Roles.Count(r=>r.Cost==5)==8&&SynergySummary.From(shop.Roles.Select(r=>r.Id)).All(s=>s.Implemented);
            SetSandboxRoster(4);StartLoopNextBattle();Battle.Begin();var r=Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M21");Battle.TryDamageUnit(r.EntityId,100000,CombatDamageType.Physical);Tick(.05f);
            bool herd=Battle.HerdTier==4&&Battle.Units.Any(u=>Battle.IsHerdHost(u.EntityId)&&u.SynergySupport&&!Battle.UnitCombat(u).HasSkill);ReplaySandbox();
            SetSandboxRoster(5);StartLoopNextBattle();Battle.Begin();long deepRequest=0;
            foreach(var id in new[]{"CARD_M29","CARD_M30","CARD_M31","CARD_M32"})Battle.TryDeployRole(Battle.RunId,++deepRequest,Battle.Entry,id);
            for(int i=0;i<2600&&Battle.DeepCharges==0&&Loop.CanDeploy;i++){Tick(.1f);if(i%20==0)yield return null;}
            bool deep=Battle.DeepTier==4&&Battle.DeepCharges>0;ReplaySandbox();
            SetSandboxRoster(6);
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            yield return null;
            bool contractChoice=!ArtLoading&&!ShopPanelTransitioning&&SetRemainingChoices("CARD_M37",MarineAdaptation.Hunt,MarineAdaptation.Infiltration)&&PreparedContractCard=="CARD_M37";
            StartLoopNextBattle();Battle.Begin();
            var target=Battle.FindUnit(Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M37").EntityId);
            bool contract=contractChoice&&Battle.ContractPoints==0&&!Battle.CanActivateContract&&Battle.ContractAttackBonus(target)>.89f&&Battle.ContractShieldRemaining(target)>0&&double.IsPositiveInfinity(Battle.ContractRemaining);ReplaySandbox();
            SetSandboxRoster(7);StartLoopNextBattle();Battle.Begin();
            var sea=Battle.FindUnit(Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M33").EntityId);float armor=Battle.DefenderArmor(Battle.Defender);Battle.TryDamageUnit(sea.EntityId,sea.MaxHealth*.16f,CombatDamageType.Physical);
            bool marine=Battle.CreeperPulses.Count>0&&Battle.DefenderArmor(Battle.Defender)==armor&&Battle.MarineCells.Count==1;
            Battle.FinishForTesting(Battle.RunId,true);Tick(.01f);ReplaySandbox();marine&=Loop.MarineExperience==0&&Battle.MarineCells.Count==0;
            Debug.Log($"REMAINING_SYNERGY_PLAYER_CHECK: pool={pool}; herd={herd}; nativeDeep={deep}; contract={contract}; marineTerrain={marine}");
            Application.Quit(pool&&herd&&deep&&contract&&marine?0:1);
        }
    }
}
