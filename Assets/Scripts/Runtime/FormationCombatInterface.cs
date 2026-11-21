using System.Linq;
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
            string node = Campaign?.IsSupernovaBossTrial == true ? "首领试玩" : Campaign?.CurrentNode.Id ?? Model.Round.ToString();
            Label(new Rect(28,15,207,22),node+" · "+(Model.Phase==FormationPhase.Exiting?"前往蓝门":"自动攻坚"),16,White);
            Label(new Rect(28,37,207,15),"全清守军后，由存活本体进入蓝门",10,Muted);

            var boss = Model.Defenders.FirstOrDefault(guard => guard.Alive && guard.IsTwoPhaseBoss);
            var status=new Rect(boss == null ? 486 : 892,12, boss == null ? 308 : 268,44);
            Fill(status,surface);ApprovedUi.Border(status,rule);
            Label(new Rect(status.x+13,17,104,31),"守军  "+Model.RemainingDefenders,19,Cyan);
            Fill(new Rect(status.x+126,22,1,25),rule);
            Label(new Rect(status.x+142,17,status.width-156,31),double.IsPositiveInfinity(Model.TimeRemaining)?"不限时":Model.TimeRemaining.ToString("0.0")+" 秒",22,White,TextAnchor.MiddleRight);
            if (boss != null) DrawFormationBossGauge(boss, surface, rule);
            if(Button(new Rect(1174,12,42,36),Model.Paused?"继续":"暂停"))Model.Paused=!Model.Paused;
        }

        void DrawFormationBossGauge(FormationDefender boss, Color surface, Color rule)
        {
            var panel = new Rect(390, 12, 500, 56);
            Fill(panel, surface);
            ApprovedUi.Border(panel, rule);
            bool supernova = boss.IsSupernovaBoss;
            bool changing = supernova ? Model.SupernovaBossTransitionActive(boss) : Model.SnowBossTransitionActive(boss);
            bool firstPhase = boss.BossHealthPhase == 1;
            string skill = changing
                ? (supernova ? "转阶段 · 装填重炮" : "转阶段 · 全场冻结")
                : firstPhase
                    ? (supernova ? "一阶段 · 饱和复仇" : "一阶段 · 霜涛覆岭")
                    : (supernova ? "二阶段 · 爆裂黎明" : "二阶段 · 群山俯首");
            Color firstBar = supernova ? new Color(.99f, .74f, .41f) : new Color(.65f, .89f, .99f);
            Color reservedBar = supernova ? new Color(.65f, .38f, .31f) : new Color(.34f, .50f, .66f);
            Color secondBar = supernova ? new Color(1f, .51f, .36f) : new Color(.58f, .74f, 1f);
            Label(new Rect(panel.x + 14, panel.y + 3, 180, 26), boss.Name, 19);
            Label(new Rect(panel.x + 195, panel.y + 4, 291, 24), skill, 14, supernova ? firstBar : Cyan, TextAnchor.MiddleRight);
            float fraction = Mathf.Clamp01(boss.Health / boss.MaxHealth);
            var first = new Rect(panel.x + 14, panel.y + 34, 233, 10);
            var second = new Rect(panel.x + 253, panel.y + 34, 233, 10);
            Fill(first, rule);
            Fill(second, rule);
            if (firstPhase && !changing)
                Fill(new Rect(first.x, first.y, first.width * fraction, first.height), firstBar);
            float secondFraction = firstPhase || changing ? 1 : fraction;
            Fill(new Rect(second.x, second.y, second.width * secondFraction, second.height),
                firstPhase || changing ? reservedBar : secondBar);
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
