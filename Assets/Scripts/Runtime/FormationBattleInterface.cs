using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color Dark=new Color(.045f,.075f,.105f,.96f),Panel=new Color(.09f,.14f,.18f,.96f);
        static readonly Color Cyan=new Color(.28f,.85f,.88f),Gold=new Color(1,.79f,.38f),White=new Color(.9f,.94f,.96f),Muted=new Color(.55f,.67f,.73f);
        GUIStyle Text(int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)=>ApprovedUi.Text(null,size,align,color,size>=22?650:400);
        static void Fill(Rect rect,Color color)=>ApprovedUi.Fill(rect,color);
        void Label(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)=>GUI.Label(rect,text,Text(size,color??White,align));
        bool Button(Rect rect,string text,bool active=true,bool accent=false)
        {
            bool old=GUI.enabled;GUI.enabled=old&&active;
            Fill(rect,active?(accent?new Color(.15f,.43f,.46f):new Color(.14f,.22f,.28f)):new Color(.09f,.12f,.15f));
            ApprovedUi.Border(rect,accent?Cyan:new Color(.28f,.39f,.44f));
            bool clicked=GUI.Button(rect,text,new GUIStyle(GUIStyle.none){font=ApprovedUi.Font(null,500),fontSize=14,alignment=TextAnchor.MiddleCenter,
                normal={textColor=active?White:Muted},hover={textColor=Cyan},active={textColor=Gold}});
            GUI.enabled=old;return clicked;
        }
        public static Rect RosterRect(int slot)=>FormationLineupRect(slot);
        void OnGUI()
        {
            var old=GUI.matrix;bool oldEnabled=GUI.enabled;GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                GUI.enabled=oldEnabled&&!shopOddsVisible&&shopInputBlockedFrame!=Time.frameCount;
                Fill(new Rect(0,0,1280,65),Dark);Label(new Rect(20,7,365,29),"昨日圆车  /  矩形攻坚",24);
                if(!Ready){Fill(new Rect(0,65,1280,655),Dark);Label(new Rect(150,250,980,80),loading,23,Cyan,TextAnchor.MiddleCenter);return;}
                bool preparing=Model.Phase==FormationPhase.Preparation;
                string phase=preparing?"战前整备":Model.Phase==FormationPhase.Fighting?"自动交战":Model.Phase==FormationPhase.Exiting?"守军肃清 · 前往蓝门":Model.Phase==FormationPhase.Won?"突破成功":"时间耗尽";
                Label(new Rect(405,9,520,26),$"第 {Model.Round} / 5 关  ·  {phase}",18,Cyan);
                Label(new Rect(22,40,855,19),notice,12,Muted);
                Label(new Rect(890,43,370,18),$"守军 {Model.Defenders.Count(d=>d.Alive)} / {Model.Defenders.Count}    剩余 {Model.TimeRemaining:0.0} 秒",13,Gold,TextAnchor.MiddleRight);
                if(Button(new Rect(944,9,137,29),MarketVisible?"收起商店 Tab":"打开商店 Tab",preparing))SetMarketVisible(!MarketVisible);
                if(Button(new Rect(1091,9,80,29),soundEnabled?"音效：开":"音效：关"))soundEnabled=!soundEnabled;
                if(Button(new Rect(1181,9,87,29),"原玩法"))SceneManager.LoadScene("Campaign");
                DrawWorldLabels();
                if(preparing)DrawFormationPreparation();else DrawFormationCombatRoster();
                if(preparing&&MarketVisible)DrawFormationMarket();
                if(shopDragInstance>0&&pointerHeld&&Vector2.Distance(TacticalViewport.Current.FromScreen(Input.mousePosition),pointerStart)>6)DrawFormationSaleTargets();
                if(Model.Paused&&(Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting))
                {Fill(new Rect(480,72,320,35),Dark);Label(new Rect(480,72,320,35),"已暂停  ·  空格或继续按钮恢复",15,Gold,TextAnchor.MiddleCenter);}
                if(Model.Phase==FormationPhase.Won||Model.Phase==FormationPhase.Lost)DrawSettlement();
                GUI.enabled=oldEnabled;if(shopOddsVisible)DrawFormationOdds();
            }
            finally{GUI.enabled=oldEnabled;GUI.matrix=old;TacticalViewport.Current.DrawBars();}
        }
        void DrawFormationCombatRoster()
        {
            Fill(new Rect(0,565,1280,155),Dark);DrawFormationResources(false);
            for(int i=0;i<Model.Units.Count;i++)
            {
                var u=Model.Units[i];var r=new Rect(188+i*125,580,119,125);Fill(r,Panel);ApprovedUi.Border(r,new Color(.22f,.33f,.39f));
                DrawFormationPortrait(new Rect(r.x+4,r.y+5,48,61),"CARD_"+u.ArtId);
                Label(new Rect(r.x+53,r.y+7,62,18),u.Name,u.Name.Length>5?9:11);
                Label(new Rect(r.x+54,r.y+29,60,18),new string('★',u.Star),11,Gold);
                Label(new Rect(r.x+7,r.y+71,110,18),u.Alive?$"生命 {u.Health:0}/{u.MaxHealth:0}":$"复活 {u.RespawnRemaining:0.0}秒",11,u.Alive?Cyan:Gold);
                Label(new Rect(r.x+7,r.y+91,110,16),$"伤害 {u.DamageDone:0}  阵亡 {u.Deaths}",10,Muted);
                Label(new Rect(r.x+7,r.y+109,110,15),$"技能 {u.SkillCharge:0}/{u.SkillRequired:0}",10,Muted);
            }
            if(Button(new Rect(1142,579,126,34),Model.Paused?"继续":"暂停"))Model.Paused=!Model.Paused;
            if(Button(new Rect(1142,621,126,34),Model.Speed>1?"速度 2×":"速度 1×"))Model.Speed=Model.Speed>1?1:2;
            if(Button(new Rect(1142,666,126,34),"返回整备"))Retry();
        }
        void DrawWorldLabels()
        {
            foreach(var actor in Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(Model.Defenders))
            {
                if(!actor.Alive)continue;
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(WorldPoint(actor.Position)+Vector3.up*1.22f));
                if(p.y<BattleRect.yMin+5||p.y>BattleRect.yMax-8)continue;
                bool ally=actor is FormationFighter;
                Fill(new Rect(p.x-23,p.y,46,5),new Color(.015f,.025f,.03f,.9f));
                Fill(new Rect(p.x-22,p.y+1,44*Mathf.Clamp01(actor.Health/actor.MaxHealth),3),ally?Cyan:new Color(1,.42f,.32f));
                Label(new Rect(p.x-65,p.y-18,130,18),actor.Name,11,White,TextAnchor.MiddleCenter);
            }
            if(Model.Phase==FormationPhase.Preparation)
                foreach(var u in Model.Units.Where(u=>u.Enabled))
                {
                    var p=CellScreen(u.Spawn);Label(new Rect(p.x-24,p.y-6,48,22),new string('★',u.Star),11,Gold,TextAnchor.MiddleCenter);
                    if(u.Slot==SelectedSlot)ApprovedUi.Border(new Rect(p.x-27,p.y-14,54,30),Cyan,2);
                }
            foreach(var h in Model.Hits)
            {
                double age=Model.Clock-h.At;if(age<0||age>.8)continue;
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(WorldPoint(h.To)+Vector3.up*(1.35f+(float)age*.35f)));
                if(p.y<BattleRect.yMin+5||p.y>BattleRect.yMax-8)continue;
                Label(new Rect(p.x-48,p.y-10,96,24),(h.Heal?"+":"")+h.Amount.ToString("0"),h.Skill?20:15,h.Heal?Cyan:h.Skill?Gold:White,TextAnchor.MiddleCenter);
            }
        }
        void DrawSettlement()
        {
            bool won=Model.Phase==FormationPhase.Won;Fill(new Rect(380,172,520,324),Dark);ApprovedUi.Border(new Rect(380,172,520,324),won?Cyan:Gold,2);
            Label(new Rect(405,191,470,45),won?"防线肃清 · 蓝门突破":"进攻未完成",29,won?Cyan:Gold,TextAnchor.MiddleCenter);
            Label(new Rect(410,245,460,32),won?$"剩余 {Model.TimeRemaining:0.0} 秒":$"仍有 {Model.Defenders.Count(d=>d.Alive)} 名守军，或未及时进门",16,White,TextAnchor.MiddleCenter);
            var top=Model.Units.OrderByDescending(u=>u.DamageDone).FirstOrDefault();
            if(top!=null)Label(new Rect(410,287,460,25),$"本关输出：{top.Name} {top.DamageDone:0}  ·  全队阵亡 {Model.Units.Sum(u=>u.Deaths)} 次",15,Muted,TextAnchor.MiddleCenter);
            Label(new Rect(410,327,460,43),won?"领取通关金币与经验，带着已有军阵继续整备。":"返回同关调整军阵；重试保留购买成果，不重复发奖。",14,Muted,TextAnchor.MiddleCenter);
            if(Button(new Rect(410,400,220,47),"同关整备重试"))Retry();
            if(Button(new Rect(650,400,220,47),Model.Round<5?"领取奖励 · 下一关":finalRewardClaimed?"开始新挑战":"领取奖励 · 完成挑战",won,true))
            {
                if(Model.Round<5)NextRound();else if(finalRewardClaimed)ResetDemo();
                else {var reward=Session.SettleWin(Model.Round);finalRewardClaimed=reward!=null;notice=reward!=null?$"最终奖励 +{reward.Total}金币、+{reward.Experience}经验；新挑战前保留购买记录。":"奖励尚未结算。";}
            }
        }
    }
}
