using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyCampaign()
        {
            bool sandbox=SynergySandbox;
            var args=Environment.GetCommandLineArgs();int evidence=Array.IndexOf(args,"-demoEvidenceDir");
            string dir=evidence>=0&&evidence+1<args.Length?args[evidence+1]:null;
            if(dir!=null){Directory.CreateDirectory(dir);ScreenCapture.CaptureScreenshot(Path.Combine(dir,sandbox?"synergy-preparation.png":"campaign-opening.png"));yield return null;yield return null;}
            bool snapshot=CampaignActive&&(!sandbox||Battle.ReunionTier==6&&Battle.SnowTier==5);
            if(sandbox)
            {
                int gold=shop.Gold;for(int i=0;i<100;i++)shop.Refresh();
                snapshot&=shop.UnlimitedGold&&shop.Gold==gold;
                StartLoopNextBattle();Battle.Begin();
                var ids=shopLineup.Take(3).ToArray();long request=8000;
                foreach(var id in ids)Battle.TryDeployRole(Battle.RunId,++request,Battle.Entry,id);
                snapshot&=Battle.MusterUsed==1&&Battle.Units.Any(u=>u.SynergySupport);
                Battle.SetPaused(true);double clock=Battle.Clock;Battle.Advance(1);snapshot&=Battle.Clock==clock;Battle.SetPaused(false);
                ReplaySandbox();snapshot&=Loop.CanShop&&Battle.MusterUsed==0;
            }
            else
            {
                for(int stage=1;stage<=CampaignPlan.Count;stage++)
                {
                    int before=shop.Gold;
                    if(Loop.RewardStage)ClaimCampaignSupply();
                    else {Battle.FinishForTesting(Battle.RunId,true);StepBattleShopLoop();}
                    snapshot&=Loop.Reward!=null&&shop.Gold==before+Loop.Reward.Total;
                    if(stage<CampaignPlan.Count){snapshot&=OpenLoopShop();snapshot&=StartLoopNextBattle();}
                    yield return null;
                }
                snapshot&=Loop.Phase==BattleShopPhase.Complete;
            }
            Debug.Log($"CAMPAIGN_PLAYER_CHECK: sandbox={sandbox}; passed={snapshot}; stage={Loop.Stage}; gold={shop.Gold}");
            if(dir!=null){ScreenCapture.CaptureScreenshot(Path.Combine(dir,sandbox?"synergy-return.png":"campaign-complete.png"));yield return null;yield return null;}
            Application.Quit(snapshot?0:1);
        }
    }
}
