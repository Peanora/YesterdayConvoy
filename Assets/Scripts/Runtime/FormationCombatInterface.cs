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
            string node = Campaign?.IsStandaloneBossTrial == true ? "首领试玩" : Campaign?.CurrentNode.Id ?? Model.Round.ToString();
            Label(new Rect(28,15,207,22),node+" · "+(Model.Phase==FormationPhase.Exiting?"前往蓝门":"自动攻坚"),16,White);
            Label(new Rect(28,36,207,17),"目标：清空守军并进入蓝门",12,White);

            var status=new Rect(486,12,308,44);
            Fill(status,surface);ApprovedUi.Border(status,rule);
            Label(new Rect(status.x+13,17,104,31),"守军  "+Model.RemainingDefenders,19,Cyan);
            Fill(new Rect(status.x+126,22,1,25),rule);
            Label(new Rect(status.x+142,17,status.width-156,31),double.IsPositiveInfinity(Model.TimeRemaining)?"不限时":Model.TimeRemaining.ToString("0.0")+" 秒",22,White,TextAnchor.MiddleRight);
            if(Button(new Rect(1174,12,42,36),Model.Paused?"继续":"暂停"))Model.Paused=!Model.Paused;

            DrawFormationRespawnCountdowns(surface, rule);
        }

        void DrawFormationRespawnCountdowns(Color surface, Color rule)
        {
            int row = 0;
            foreach (var unit in Model.Units)
            {
                if (!unit.Enabled || unit.Alive || unit.RespawnRemaining <= 0) continue;
                if (row == 0) Label(new Rect(16, 69, 218, 24), "复活倒计时", 15, White);
                var panel = new Rect(16, 95 + row * 34, 218, 32);
                Fill(panel, surface);
                Fill(new Rect(panel.x, panel.y, 2, panel.height), rule);
                DrawFormationPortrait(new Rect(panel.x + 4, panel.y + 2, 28, 28), unit.ArtId);
                Label(new Rect(panel.x + 39, panel.y, 121, 32), unit.Name, 13, White);
                Label(new Rect(panel.x + 160, panel.y, 51, 32), unit.RespawnRemaining.ToString("0.0") + " 秒", 15, Gold, TextAnchor.MiddleRight);
                row++;
            }
        }

        void DrawFormationPauseOverlay()
        {
            if(!Model.Paused)return;
            Fill(new Rect(508,308,264,61),new Color(.90f,.93f,.95f,.94f));
            GUI.Label(new Rect(522,313,236,51),"暂停中",approvedShop.LoopText(24,TextAnchor.MiddleCenter,null,600));
        }
    }
}
