using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyAdvancedSynergies()
        {
            bool pool=shop.Roles.Count==39&&shop.Roles.Count(r=>r.Cost==5)==4;
            SetSandboxRoster(1);StartLoopNextBattle();Battle.Begin();Battle.TryDeployRole(Battle.RunId,1,Battle.Entry,"CARD_ORIGINIUM_SLUG");
            bool split=Battle.InfectionTier==6&&Battle.Units.Count==3&&Battle.DP==98;
            Battle.SetPaused(true);Tick(1);bool paused=Battle.Clock==0;Battle.SetPaused(false);ReplaySandbox();
            SetSandboxRoster(2);StartLoopNextBattle();Battle.Begin();long request=1000;
            foreach(var piece in shop.Bench)Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,piece.Id);
            bool army=false;
            for(int i=0;i<400&&Loop.CanDeploy;i++){Tick(.1f);if(Battle.Orders>=6&&UseCampaignOrder(false)){army=true;break;}if(i%20==0)yield return null;}
            army&=Battle.OrdersUsed==1&&Battle.OrderStrikes>0;ReplaySandbox();
            SetSandboxRoster(3);StartLoopNextBattle();Battle.Begin();Battle.SetPaused(true);
            var receipt=Battle.TryDeployGolem(Battle.RunId,++request,Battle.Entry,true);Tick(1);
            bool golem=receipt.Success&&Battle.DP==96&&Battle.Clock==0&&Battle.UnitCombat(Battle.FindUnit(receipt.EntityId)).AttackDuration>5.16f;
            Battle.SetPaused(false);foreach(var piece in shop.Bench)Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,piece.Id);
            for(int i=0;i<2600&&Loop.CanDeploy;i++)
            {
                if(i>0&&i%50==0)foreach(var piece in shop.Bench)Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,piece.Id);
                Tick(.1f);if(i%20==0)yield return null;
            }
            bool growth=Battle.Phase==BattlePhase.Won&&Loop.GolemExperience==3;ReplaySandbox();growth&=Battle.GolemStage==1;
            Debug.Log($"ADVANCED_SYNERGY_PLAYER_CHECK: pool={pool}; split={split}; army={army}; golem={golem}; paused={paused}; naturalGrowth={growth}");
            Application.Quit(pool&&split&&army&&golem&&paused&&growth?0:1);
        }
    }
}
