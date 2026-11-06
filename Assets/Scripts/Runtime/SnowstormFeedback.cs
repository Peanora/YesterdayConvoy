using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static readonly Color SnowstormBlue=new Color(.27f,.67f,.81f);
        static readonly Color SnowstormInk=new Color(.13f,.43f,.56f);
        static readonly Color SnowstormPaper=new Color(.85f,.94f,.97f,.97f);
        static Rect SnowstormBarRect=>new Rect(528,64,256,37);
        static Rect SnowstormStartRect=>new Rect(440,118,432,59);
        bool SnowstormHudVisible=>CampaignActive&&Loop.CanDeploy&&Battle!=null&&Battle.Phase==BattlePhase.Running&&
            !loopMenuOpen&&!loopTestSettlementOpen&&!shopVisible&&!ShopPanelTransitioning&&!CampaignUiModalOpen;
        bool SnowstormHudActive=>SnowstormHudVisible&&Battle.SnowstormActive;
        bool SnowstormChargeHudVisible=>SnowstormHudVisible&&Battle.SnowTier==5&&!Battle.SnowstormActive;
        int SnowstormColdPulses=>Math.Min(4,Battle.SnowstormPulses.Count(p=>p.At>=Battle.SnowstormStartedAt&&p.At<Battle.SnowstormEndsAt));

        void DrawSnowstormFeedback()
        {
            // Phase is part of the gate: the simulation clock intentionally stops at settlement.
            // There is no retained UI state to leak into a new battle, menu or preparation screen.
            if(!SnowstormHudVisible||double.IsNegativeInfinity(Battle.SnowstormStartedAt))return;
            Rect bar=SnowstormBarRect;
            if(Battle.SnowstormActive)
            {
                SnowstormFill(bar,ApprovedUi.Paper);
                SnowstormFill(new Rect(bar.x,bar.y,3,bar.height),SnowstormBlue);
                DrawSnowstormMark(new Vector2(bar.x+21,bar.y+18),8);
                GUI.Label(new Rect(bar.x+39,bar.y+3,116,28),"暴风雪",CampaignText(16,weight:600));
                GUI.Label(new Rect(bar.x+159,bar.y+3,85,28),$"{Battle.SnowstormRemaining:0.0}s",CampaignText(18,TextAnchor.MiddleRight,SnowstormInk,600));
                DrawSnowstormRemaining(new Rect(bar.x+9,bar.yMax-4,bar.width-18,2));

                double age=Battle.Clock-Battle.SnowstormStartedAt;
                if(age>=0&&age<1.08)
                {
                    float alpha=Mathf.Min(Mathf.Clamp01((float)age/.08f),Mathf.Clamp01((1.08f-(float)age)/.26f));
                    Color previous=GUI.color;
                    try
                    {
                        GUI.color=new Color(previous.r,previous.g,previous.b,previous.a*alpha);
                        Rect start=SnowstormStartRect;SnowstormFill(start,ApprovedUi.Paper);
                        SnowstormFill(new Rect(start.x,start.y,3,start.height),SnowstormBlue);
                        DrawSnowstormMark(new Vector2(start.x+28,start.y+29),13);
                        GUI.Label(new Rect(start.x+58,start.y+4,364,29),"雪怪小队 · 暴风雪",CampaignText(21,weight:600));
                        GUI.Label(new Rect(start.x+58,start.y+33,364,21),$"全场寒潮已触发 · 持续{ManualBattle.SnowstormDuration:0}秒",CampaignText(12,color:SnowstormInk));
                    }
                    finally {GUI.color=previous;}
                }
                return;
            }

            // Only natural weather expiry gets this short message. This never clears target cold/freeze.
            double endedAge=Battle.Clock-Battle.SnowstormEndsAt;
            if(endedAge<0||endedAge>=.85)return;
            Color old=GUI.color;
            try
            {
                GUI.color=new Color(old.r,old.g,old.b,old.a*(1-Mathf.SmoothStep(0,1,(float)(endedAge/.85))));
                SnowstormFill(bar,ApprovedUi.Paper);
                DrawSnowstormMark(new Vector2(bar.x+30,bar.y+18),7);
                GUI.Label(new Rect(bar.x+48,bar.y+3,bar.width-61,29),"暴风雪消散",CampaignText(14,color:SnowstormInk,weight:600));
            }
            finally {GUI.color=old;}
        }

        void DrawSnowstormRow(Rect row)
        {
            // Keep the original row height and scroll view: nine tags still leave every button accessible.
            GUI.Label(new Rect(row.x+9,row.y+22,65,16),$"寒潮{SnowstormColdPulses}/4",CampaignText(10,color:SnowstormInk));
            GUI.Label(new Rect(row.xMax-62,row.y+22,54,16),$"{Battle.SnowstormRemaining:0.0}秒",CampaignText(10,TextAnchor.MiddleRight,SnowstormInk,600));
            DrawSnowstormRemaining(new Rect(row.x+9,row.yMax-3,row.width-18,2));
        }

        void DrawSnowstormChargeRow(Rect row)
        {
            GUI.Label(new Rect(row.x+9,row.y+22,65,16),$"寒意{Battle.SnowstormCharge}/{ManualBattle.SnowstormChargeRequired}",CampaignText(10,color:SnowstormInk));
            GUI.Label(new Rect(row.xMax-62,row.y+22,54,16),"碎冰蓄积",CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
            Rect line=new Rect(row.x+9,row.yMax-3,row.width-18,2);
            Fill(line,ApprovedUi.Rule);
            Fill(new Rect(line.x,line.y,line.width*Mathf.Clamp01((float)Battle.SnowstormCharge/ManualBattle.SnowstormChargeRequired),line.height),SnowstormBlue);
        }

        void DrawSnowstormRemaining(Rect line)
        {
            Fill(line,ApprovedUi.Rule);
            Fill(new Rect(line.x,line.y,line.width*Mathf.Clamp01((float)(Battle.SnowstormRemaining/ManualBattle.SnowstormDuration)),line.height),SnowstormBlue);
        }

        static void SnowstormFill(Rect rect,Color color)
        {
            // The shared Fill replaces GUI.color, so bake the notice fade into the panel's own alpha.
            color.a*=GUI.color.a;Fill(rect,color);
        }

        static void DrawSnowstormMark(Vector2 center,float radius)
        {
            Color ink=SnowstormBlue;ink.a*=GUI.color.a;
            for(int i=0;i<6;i++)
            {
                float angle=(i*60-90)*Mathf.Deg2Rad;
                var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var across=new Vector2(-direction.y,direction.x);
                ShopLine(center,center+direction*radius,ink,1.2f);
                var branch=center+direction*(radius*.55f);
                ShopLine(branch,center+direction*(radius*.80f)+across*(radius*.22f),ink,1.1f);
                ShopLine(branch,center+direction*(radius*.80f)-across*(radius*.22f),ink,1.1f);
            }
        }
    }
}
