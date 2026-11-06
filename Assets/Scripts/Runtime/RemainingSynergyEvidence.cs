using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyRemainingSynergies()
        {
            bool pool=shop.Roles.Count==42&&shop.Roles.Count(r=>r.Cost==5)==7&&SynergySummary.From(shop.Roles.Select(r=>r.Id)).All(s=>s.Implemented);
            SetSandboxRoster(4);StartLoopNextBattle();Battle.Begin();var r=Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M21");Battle.TryDamageUnit(r.EntityId,100000,CombatDamageType.Physical);Tick(.05f);
            bool herd=Battle.HerdTier==4&&Battle.Units.Any(u=>Battle.IsHerdHost(u.EntityId)&&u.SynergySupport&&!Battle.UnitCombat(u).HasSkill);ReplaySandbox();
            SetSandboxRoster(5);StartLoopNextBattle();Battle.Begin();long deepRequest=0;
            foreach(var id in new[]{"CARD_M29","CARD_M30","CARD_M31","CARD_M32"})Battle.TryDeployRole(Battle.RunId,++deepRequest,Battle.Entry,id);
            for(int i=0;i<2600&&Battle.DeepCharges==0&&Loop.CanDeploy;i++){Tick(.1f);if(i%20==0)yield return null;}
            bool deep=Battle.DeepTier==4&&Battle.DeepCharges>0;ReplaySandbox();
            SetSandboxRoster(6);SetRemainingChoices("CARD_M37",MarineAdaptation.Hunt,MarineAdaptation.Infiltration);StartLoopNextBattle();Battle.Begin();
            var target=Battle.FindUnit(Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M37").EntityId);long request=10;
            foreach(var id in new[]{"CARD_M19","CARD_M24","CARD_M26","CARD_M27"}){var u=Battle.FindUnit(Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,id).EntityId);Battle.TryDamageUnit(u.EntityId,100000,CombatDamageType.Physical);}
            bool contract=Battle.ContractPoints==4&&UseContract()&&Battle.ContractAttackBonus(target)>.39f&&target.SynergyShield>0;ReplaySandbox();
            SetSandboxRoster(7);SetRemainingChoices("CARD_M33",MarineAdaptation.Infiltration,MarineAdaptation.Hunt);StartLoopNextBattle();Battle.Begin();
            var sea=Battle.FindUnit(Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_M33").EntityId);float armor=Battle.DefenderArmor(Battle.Defender);Battle.TryDamageUnit(sea.EntityId,sea.MaxHealth*.16f,CombatDamageType.Physical);
            bool marine=Battle.CreeperPulses.Count>0&&Battle.DefenderArmor(Battle.Defender)<armor;
            Battle.FinishForTesting(Battle.RunId,true);Tick(.01f);int xp=Loop.MarineExperience;ReplaySandbox();marine&=xp>0&&Battle.MarineExperience==xp;
            Debug.Log($"REMAINING_SYNERGY_PLAYER_CHECK: pool={pool}; herd={herd}; nativeDeep={deep}; contract={contract}; marineGrowth={marine}");
            Application.Quit(pool&&herd&&deep&&contract&&marine?0:1);
        }
    }
}
