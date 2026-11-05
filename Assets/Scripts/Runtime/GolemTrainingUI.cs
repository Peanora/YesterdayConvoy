using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool golemTrainingTab,golemBenchmark;
        public bool InvestCampaignGolem(int amount)
        {
            if(!CampaignActive||!Loop.CanShop||Battle.Phase!=BattlePhase.Ready||Battle.MudrockTier<2)return false;
            bool ok=Loop.InvestGolem(amount);
            if(ok){SyncShopDeck(false);ShopNotice($"巨像投入+{amount}，累计{Loop.GolemInvestment}；本关间余{Loop.GolemInvestmentRemaining}");}
            else ShopNotice("投入失败：金币不足或超过本关间10金币上限");
            return ok;
        }
        void DrawGolemTraining()
        {
            GUI.Label(new Rect(694,313,550,25),$"巨像 {Battle.GolemStage} · 小队{Battle.MudrockTier}档 · 培养{Loop.GolemExperience} · 累计投入{Loop.GolemInvestment}",LoopText(13));
            GUI.Label(new Rect(694,343,550,25),GolemGrowth.Requirements(Battle.GolemStage),LoopText(13));
            bool enabled=GUI.enabled;GUI.enabled=enabled&&Battle.MudrockTier>=2;
            foreach(int amount in new[]{1,5,10})
            {
                GUI.enabled=enabled&&Battle.MudrockTier>=2&&Loop.GolemInvestmentRemaining>=amount&&shop.CanAfford(amount);
                if(LoopButton(new Rect(694+(amount==1?0:amount==5?148:296),375,138,30),$"投入 {amount} 金币"))InvestCampaignGolem(amount);
            }
            GUI.enabled=enabled;
            GUI.Label(new Rect(694,410,550,24),$"本关间可再投入{Loop.GolemInvestmentRemaining}；正式胜利且两种成员交战+3，4档巨像交战再+1。",LoopText(11));
            if(SynergySandbox)
            {
                GUI.Label(new Rect(694,436,550,20),"验证预置：直接设定培养/投入，不是正式流程获得",LoopText(11));
                for(int i=0;i<4;i++)if(LoopButton(new Rect(694+i*137,461,129,26),new[]{"I 胚体","II 成长","III 巨像","IV 终极"}[i]))PrepareSandboxRoster(i==0?3:i+7);
            }
            else GUI.Label(new Rect(694,444,550,42),"培养和投入跨关保留，卖卡不返还；新挑战清零。\nIV要求5位泥岩小队全部至少二星。",LoopText(12));
        }
    }
}
