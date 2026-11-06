using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static string AdaptationName(MarineAdaptation value)=>value==MarineAdaptation.Barrier?"屏障":value==MarineAdaptation.Hunt?"猎食":"渗透";
        public bool SetRemainingChoices(string contract,MarineAdaptation form,MarineAdaptation second)
        {
            if(!CampaignActive||!Loop.CanShop||Battle.Phase!=BattlePhase.Ready||!shopLineup.Contains(contract)||!Enum.IsDefined(typeof(MarineAdaptation),form)||!Enum.IsDefined(typeof(MarineAdaptation),second))return false;
            contractCard=contract;marineForm=form;marineSecond=form==second?(MarineAdaptation)(((int)form+1)%3):second;contractTargetIndex=0;SyncShopDeck(false);return true;
        }
        void DrawRemainingPreparation()
        {
            if(!CampaignActive)return;
            var rect=new Rect(680,270,580,224);Fill(rect,ApprovedUi.Paper);
            GUI.Label(new Rect(694,278,300,26),Loop.CanShop?"构筑选择 · 开战后锁定":"构筑选择 · 本场只读",CampaignText(15,weight:600));
            if(LoopButton(new Rect(1000,277,116,28),"阵营选择"))golemTrainingTab=false;
            if(LoopButton(new Rect(1124,277,120,28),"巨像培养"))golemTrainingTab=true;
            bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&Loop.CanShop;
            try
            {
                if(golemTrainingTab){DrawGolemTraining();return;}
                string selected=shopLineup.Contains(contractCard)?contractCard:shopLineup.FirstOrDefault();
                if(selected==null)return;
                if(LoopButton(new Rect(694,313,550,31),"承契卡："+shop.Definition(selected).Name+" · 点击切换"))
                    SetRemainingChoices(shopLineup[(shopLineup.IndexOf(selected)+1)%shopLineup.Count],marineForm,marineSecond);
                if(LoopButton(new Rect(694,352,268,31),"海嗣主适应："+AdaptationName(marineForm)))SetRemainingChoices(selected,(MarineAdaptation)(((int)marineForm+1)%3),marineSecond);
                if(LoopButton(new Rect(976,352,268,31),"第二适应："+AdaptationName(marineSecond)))SetRemainingChoices(selected,marineForm,(MarineAdaptation)(((int)marineSecond+1)%3));
                GUI.Label(new Rect(694,390,550,25),$"海嗣培养 XP{Loop.MarineExperience} · 阶段{Battle.MarineStage}（0 / 3 / 7）",CampaignText(13));
                var hint=CampaignText(12,TextAnchor.UpperLeft);hint.wordWrap=true;
                GUI.Label(new Rect(694,421,550,66),"4海嗣且培养进入II阶段后，副适应取I阶段效果。集齐深池四名成员可解锁火种引爆。转化体沿用原角色动作，不复制主动或固有能力。",hint);
            }
            finally {GUI.enabled=oldEnabled;}
        }
        DeployedUnitState[] ContractTargets()=>Battle.Units.Where(u=>u.Active&&!u.IsReviving&&!u.SynergySupport&&u.CardId==Battle.ContractCard).ToArray();
        public bool UseContract()
        {
            var targets=ContractTargets();if(targets.Length==0)return false;
            return Battle.TryActivateContract(Battle.RunId,++requestSequence,targets[contractTargetIndex%targets.Length].EntityId);
        }
        float DrawRemainingBattleActions(float x,float y)
        {
            var targets=ContractTargets();bool oldEnabled=GUI.enabled;
            bool enabled=oldEnabled&&Loop.CanDeploy&&Battle.Phase==BattlePhase.Running&&!Battle.IsPaused;
            if(Battle.SarkazTier>0)
            {
                GUI.enabled=enabled&&targets.Length>0;
                string name=targets.Length==0?"未部署":$"承契 {contractTargetIndex%targets.Length+1}/{targets.Length}";
                if(LoopButton(new Rect(x,y,76,25),name))contractTargetIndex++;
                GUI.enabled=GUI.enabled&&Battle.ContractPoints>=4;
                if(LoopButton(new Rect(x+81,y,76,25),$"爆发 {Battle.ContractPoints}/4"))UseContract();
                y+=29;
            }
            if(Battle.DeepTier>0)
            {
                GUI.enabled=enabled&&Battle.DeepTier==4;
                if(LoopButton(new Rect(x,y,157,25),Battle.DeepTier<4?$"火种 {Battle.DeepCharges} · 4档引爆":$"引爆火种 {Battle.DeepCharges}"))Battle.TryDetonateDeep(Battle.RunId,++requestSequence);
                y+=29;
            }
            GUI.enabled=oldEnabled;
            if(Battle.HerdTier>0)
            {
                Fill(new Rect(x,y,157,19),ApprovedUi.Paper);GUI.Label(new Rect(x+7,y,143,19),$"牧群转化 {Battle.HerdUsed}/{(Battle.HerdTier==4?5:3)}",CampaignText(11));y+=21;
            }
            if(Battle.MarineTier>0)
            {
                Fill(new Rect(x,y,157,19),ApprovedUi.Paper);GUI.Label(new Rect(x+7,y,143,19),$"海嗣 {AdaptationName(Battle.Adaptation)} {Battle.MarineStage} · {Loop.MarineExperience}XP",CampaignText(10));y+=21;
            }
            return y;
        }
    }
}
