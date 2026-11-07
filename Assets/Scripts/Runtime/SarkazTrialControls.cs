using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static readonly int[] SarkazTrialTiers={2,4,6,8,9};
        static readonly string[] SarkazTrialMembers={"CARD_M37","CARD_M38","CARD_M26","CARD_M27","CARD_M19","CARD_M24","CARD_M42","CARD_M43","CARD_WISADEL_APRIL"};

        // Accepts an explicit sandbox preset. Art and the actual finite-stock formation
        // are prepared together; callers wait for ArtLoading before entering battle.
        public bool SetSarkazTierPreset(int tier)
        {
            if(!SynergySandbox||!Loop.CanShop||Battle.Phase!=BattlePhase.Ready||ArtLoading||Array.IndexOf(SarkazTrialTiers,tier)<0)return false;
            var ids=SarkazTrialMembers.Take(tier).ToArray();
            if(ids.Any(id=>!shopDefinitions.ContainsKey(id)))return false;
            var cards=ids.Select(id=>shopDefinitions[id].Card);
            if(ids.Count(CampaignBalance.MudrockCrew)>=2)cards=cards.Concat(new[]{GolemCard});
            QueueArt(Plan(shopMapCopy.defenders,cards,true),()=>
            {
                CompleteGolemExperienceFeedback();
                shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),Math.Max(tier,Math.Max(8,shop.Level)),0,10);
                ResetGuerrillaTransfers();ResetMudrockTransfers();
                Loop.PresetGolemGrowth(0);ResetGolemExperienceFeedback();golemBenchmark=false;golemTrainingTab=false;
                contractCard=ids[0];contractTargetIndex=0;shopLineup.Clear();shopLineup.AddRange(ids);commissionTarget=ids[0];shop.Refresh(true);
                ConfigureLoopStage();SetShopVisible(true);
                ShopNotice($"萨卡兹{tier}档试验预置 · 全员1星 · 真实九张库存");
            },$"准备萨卡兹{tier}档");
            return true;
        }

        public static Rect SarkazTierPresetRect(int tier)
        {
            int index=Array.IndexOf(SarkazTrialTiers,tier);if(index<0)return Rect.zero;
            return new Rect(CampaignConstructRect.x+30+index*110,CampaignConstructRect.y+253,104,27);
        }

        void DrawSarkazPreparation()
        {
            GUI.Label(new Rect(694,352,550,25),"萨卡兹 · 众魂之契 · "+Battle.SarkazTier+"档",CampaignText(15,weight:600));
            var hint=CampaignText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);hint.wordWrap=true;
            GUI.Label(new Rect(694,382,550,48),Battle.SarkazTier>=9?
                "众魂归乡：承契者与全体萨卡兹攻击永久+150%；部署及每20秒获60%生命护盾。全体萨卡兹生命+60%。":
                "开战即可承契，无需低血或阵亡充能；发动起冷却20秒，本场不限次数。承契卡全部正常实体统一受益，6档开始共享给同胞。",hint);
            if(SynergySandbox)
            {
                bool enabled=GUI.enabled;GUI.enabled=enabled&&!ArtLoading;
                for(int i=0;i<SarkazTrialTiers.Length;i++)
                {
                    int tier=SarkazTrialTiers[i];
                    if(LoopButton(new Rect(694+i*110,439,104,27),tier+"档预置",Battle.SarkazTier==tier))SetSarkazTierPreset(tier);
                }
                GUI.enabled=enabled;
                GUI.Label(new Rect(694,472,550,17),"试验预置重置编队为1星，按真实库存占库；9档自动升至至少9级。",CampaignText(10,color:ApprovedUi.Muted));
            }
            else GUI.Label(new Rect(694,439,550,45),"按战前编队的不同种类锁档，无须把九种全部部署。\n护盾按各自生命计算；暂停冻结，巨像、援军与转化体不参与。",hint);
        }
    }
}
