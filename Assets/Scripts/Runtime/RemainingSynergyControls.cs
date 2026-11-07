using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
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
                GUI.Label(new Rect(694,352,550,27),"海嗣 · 溟痕蔓延 · 每场重新展开",CampaignText(15,color:MarineInk,weight:600));
                var hint=CampaignText(12,TextAnchor.UpperLeft);hint.wordWrap=true;
                GUI.Label(new Rect(694,386,550,49),"每种海嗣首次有效接敌在脚下落种。2/4档每6/4秒扩散，覆盖至多8/16格；驻留溟痕的海嗣原生神经损伤×1.5/2。",hint);
                GUI.Label(new Rect(694,439,550,46),"地面守军踩溟痕每秒积累3点神经损伤。战斗中可点左侧「查看溟痕范围」观察覆盖；无存活海嗣时休眠。",hint);
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
                GUI.enabled=enabled&&Battle.DeepTier==4&&Battle.DeepCharges>0&&Battle.DeepDetonations<2;
                if(LoopButton(new Rect(x,y,157,25),Battle.DeepTier<4?$"火种 {Battle.DeepCharges} · 4档引爆":$"引爆1枚 · {Battle.DeepCharges}枚/{2-Battle.DeepDetonations}次"))Battle.TryDetonateDeep(Battle.RunId,++requestSequence);
                y+=29;
            }
            GUI.enabled=oldEnabled;
            if(Battle.HerdTier>0)
            {
                Fill(new Rect(x,y,157,19),ApprovedUi.Paper);GUI.Label(new Rect(x+7,y,143,19),$"牧群转化 {Battle.HerdUsed}/{(Battle.HerdTier==4?5:3)}",CampaignText(11));y+=21;
            }
            if(Battle.MarineTier>0)
            {
                y=DrawMarineBattleActions(x,y);
            }
            return y;
        }
    }
}
