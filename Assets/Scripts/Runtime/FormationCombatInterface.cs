using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        void RefreshFormationOperatingVisibility()
        {
            if(!Ready)return;
            if(CombatViewActive)
            {
                if(marketVisible||shopOddsVisible)SetMarketVisible(false);
                CloseFormationTraitDetails();
                if(campaignInventoryOpen||campaignFormationOpen||campaignRouteOpen||campaignScoutingOpen)CloseCampaignPanels();
                campaignSeaPainting=false;
            }
            var stage=approvedShop?.ModelStage;
            if(stage!=null&&stage.activeSelf!=FormationPreparationUiVisible)
            {
                stage.SetActive(FormationPreparationUiVisible);
                approvedShop.Tick();
            }
        }

        void DrawFormationCombatHud()
        {
            var surface=new Color(.055f,.085f,.11f,.91f);
            var rule=new Color(.29f,.38f,.43f,.8f);
            var phase=new Rect(16,12,232,44);
            Fill(phase,surface);ApprovedUi.Border(phase,rule);
            string node=Campaign?.CurrentNode.Id??Model.Round.ToString();
            Label(new Rect(28,15,207,22),node+" · "+(Model.Phase==FormationPhase.Exiting?"前往蓝门":"自动攻坚"),16,White);
            Label(new Rect(28,37,207,15),"全清守军后，由存活本体进入蓝门",10,Muted);

            var status=new Rect(486,12,308,44);
            Fill(status,surface);ApprovedUi.Border(status,rule);
            Label(new Rect(499,17,104,31),"守军  "+Model.RemainingDefenders,19,Cyan);
            Fill(new Rect(612,22,1,25),rule);
            Label(new Rect(628,17,152,31),double.IsPositiveInfinity(Model.TimeRemaining)?"不限时":Model.TimeRemaining.ToString("0.0")+" 秒",22,White,TextAnchor.MiddleRight);
            if(Button(new Rect(1174,12,42,36),Model.Paused?"继续":"暂停"))Model.Paused=!Model.Paused;
        }

        void DrawFormationPauseOverlay()
        {
            if(!Model.Paused)return;
            Fill(new Rect(508,308,264,61),new Color(.90f,.93f,.95f,.94f));
            GUI.Label(new Rect(522,313,236,29),"暂停中",approvedShop.LoopText(24,TextAnchor.MiddleCenter,null,600));
            GUI.Label(new Rect(522,342,236,18),"空格或右上继续按钮恢复",approvedShop.LoopText(11,TextAnchor.MiddleCenter,ApprovedUi.Muted));
        }
    }
}
