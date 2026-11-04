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
            if(!CampaignActive||!Loop.CanShop||inspectedSynergy!=null)return;
            var rect=new Rect(680,270,580,224);Fill(rect,LoopInk);
            GUI.Label(new Rect(694,278,550,26),"构筑选择 · 开战后锁定",LoopText(15));
            string selected=shopLineup.Contains(contractCard)?contractCard:shopLineup.FirstOrDefault();
            if(selected==null)return;
            if(LoopButton(new Rect(694,313,550,31),"承契卡："+shop.Definition(selected).Name+" · 点击切换"))
                SetRemainingChoices(shopLineup[(shopLineup.IndexOf(selected)+1)%shopLineup.Count],marineForm,marineSecond);
            if(LoopButton(new Rect(694,352,268,31),"海嗣主适应："+AdaptationName(marineForm)))SetRemainingChoices(selected,(MarineAdaptation)(((int)marineForm+1)%3),marineSecond);
            if(LoopButton(new Rect(976,352,268,31),"第二适应："+AdaptationName(marineSecond)))SetRemainingChoices(selected,marineForm,(MarineAdaptation)(((int)marineSecond+1)%3));
            GUI.Label(new Rect(694,390,550,25),$"海嗣培养 XP{Loop.MarineExperience} · 阶段{Battle.MarineStage}（0 / 3 / 7）",LoopText(13));
            var hint=LoopText(12);hint.wordWrap=true;
            GUI.Label(new Rect(694,421,550,66),"4海嗣且培养进入II阶段后，副适应取I阶段效果。深池当前有3名非五费，4档等待曼德拉完整技能后开放。转化体沿用原角色动作，不复制主动或固有能力。",hint);
        }
        DeployedUnitState[] ContractTargets()=>Battle.Units.Where(u=>u.Active&&!u.IsReviving&&!u.SynergySupport&&u.CardId==Battle.ContractCard).ToArray();
        public bool UseContract()
        {
            var targets=ContractTargets();if(targets.Length==0)return false;
            return Battle.TryActivateContract(Battle.RunId,++requestSequence,targets[contractTargetIndex%targets.Length].EntityId);
        }
        void DrawRemainingBattle(float y)
        {
            var targets=ContractTargets();bool enabled=Loop.CanDeploy&&Battle.Phase==BattlePhase.Running&&!Battle.IsPaused;
            GUI.enabled=enabled&&Battle.SarkazTier>0&&targets.Length>0;
            string name=targets.Length==0?"承契者未部署":$"承契目标 {contractTargetIndex%targets.Length+1}/{targets.Length}";
            if(LoopButton(new Rect(25,y+104,185,27),name))contractTargetIndex++;
            GUI.enabled=GUI.enabled&&Battle.ContractPoints>=4;
            if(LoopButton(new Rect(220,y+104,185,27),$"承契爆发 {Battle.ContractPoints}/4"))UseContract();
            GUI.enabled=enabled&&Battle.DeepTier==4;
            if(LoopButton(new Rect(25,y+136,185,27),Battle.DeepTier<4?$"火种 {Battle.DeepCharges} · 4档引爆":$"引爆火种 {Battle.DeepCharges}"))Battle.TryDetonateDeep(Battle.RunId,++requestSequence);
            GUI.enabled=true;
            GUI.Label(new Rect(220,y+136,185,27),$"牧群 {Battle.HerdUsed}/{(Battle.HerdTier==0?0:Battle.HerdTier==4?5:3)}",LoopText(12));
            GUI.Label(new Rect(25,y+164,388,24),Battle.MarineTier==0?"海嗣未激活":$"海嗣{Battle.MarineTier} · {AdaptationName(Battle.Adaptation)}{Battle.MarineStage}阶段 · XP{Loop.MarineExperience}",LoopText(12));
        }
    }
}
