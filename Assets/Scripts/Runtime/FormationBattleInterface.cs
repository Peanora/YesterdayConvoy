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
            bool clicked=GUI.Button(rect,text,new GUIStyle(GUIStyle.none){font=ApprovedUi.Font(null,500),fontSize=15,alignment=TextAnchor.MiddleCenter,normal={textColor=active?White:Muted},hover={textColor=Cyan},active={textColor=Gold}});
            GUI.enabled=old;return clicked;
        }
        public static Rect RosterRect(int slot)=>new Rect(25+slot*158,565,148,132);
        void OnGUI()
        {
            var old=GUI.matrix;GUI.matrix=TacticalViewport.Current.Matrix;
            Fill(new Rect(0,0,1280,100),Dark);
            // Clear every area outside the inset camera, including pixels from its first full-screen frame.
            Fill(new Rect(0,100,1280,21),Dark);Fill(new Rect(0,121,24,411),Dark);Fill(new Rect(1256,121,24,411),Dark);
            Label(new Rect(25,12,700,34),"昨日圆车  /  矩形攻坚",27);
            Label(new Rect(27,49,780,25),"固定战场 · 站位继承 · 自动进攻 · 原位复活",15,Muted);
            if(!Ready)
            {
                Fill(new Rect(0,100,1280,620),Dark);Label(new Rect(150,250,980,80),loading,24,Cyan,TextAnchor.MiddleCenter);
                GUI.matrix=old;return;
            }
            bool preparing=Model.Phase==FormationPhase.Preparation;
            string phase=preparing?"战前整备":Model.Phase==FormationPhase.Fighting?"自动交战":Model.Phase==FormationPhase.Exiting?"清场完成 · 前往蓝门":Model.Phase==FormationPhase.Won?"突破成功":"时间耗尽";
            Label(new Rect(880,12,365,32),$"第 {Model.Round} / 5 关    {phase}",22,Cyan,TextAnchor.MiddleRight);
            Label(new Rect(876,48,368,29),$"守军 {Model.Defenders.Count(d=>d.Alive)} / {Model.Defenders.Count}    剩余 {Model.TimeRemaining:0.0} 秒",18,Gold,TextAnchor.MiddleRight);
            Label(new Rect(35,99,560,26),"我方布阵区  /  仅战前可调整",14,Cyan);
            Label(new Rect(785,99,450,26),"本关守军已公开  /  清空后开启蓝门",14,new Color(1,.6f,.5f),TextAnchor.MiddleRight);
            DrawWorldLabels();
            Fill(new Rect(0,532,1280,188),Dark);
            Label(new Rect(25,533,1090,27),notice,14,Muted);
            for(int i=0;i<Model.Units.Count;i++)DrawRoster(i,preparing);
            Fill(new Rect(980,565,275,132),Panel);
            int enabled=Model.Units.Count(u=>u.Enabled);
            Label(new Rect(992,571,250,24),$"上阵 {enabled} / {Model.Capacity}    免费体验整备",15,Cyan);
            if(preparing)
            {
                if(Button(new Rect(991,604,252,40),"开始进攻",enabled>0,true))StartBattle();
                if(Button(new Rect(991,652,122,32),"扩编 +1",Model.Capacity<6)){Model.Expand();notice="人口上限已增加，可让备战角色上阵。";}
                if(Button(new Rect(1121,652,122,32),"新挑战"))ResetDemo();
            }
            else
            {
                if(Button(new Rect(991,604,122,36),Model.Paused?"继续":"暂停"))Model.Paused=!Model.Paused;
                if(Button(new Rect(1121,604,122,36),Model.Speed>1?"速度 2×":"速度 1×"))Model.Speed=Model.Speed>1?1:2;
                if(Button(new Rect(991,652,252,32),"返回整备 · 同关重试"))Retry();
            }
            if(Button(new Rect(25,76,100,24),soundEnabled?"音效：开":"音效：关"))soundEnabled=!soundEnabled;
            if(Button(new Rect(139,76,130,24),"返回原玩法"))SceneManager.LoadScene("Campaign");
            if(Model.Paused&&Model.Phase!=FormationPhase.Won&&Model.Phase!=FormationPhase.Lost)
            {Fill(new Rect(480,107,320,38),Dark);Label(new Rect(480,107,320,38),"已暂停  ·  空格或继续按钮恢复",16,Gold,TextAnchor.MiddleCenter);}
            if(Model.Phase==FormationPhase.Won||Model.Phase==FormationPhase.Lost)DrawSettlement();
            GUI.matrix=old;TacticalViewport.Current.DrawBars();
        }
        void DrawRoster(int index,bool preparing)
        {
            var u=Model.Units[index];Rect r=RosterRect(index);
            Fill(r,index==SelectedSlot?new Color(.12f,.27f,.31f):Panel);ApprovedUi.Border(r,index==SelectedSlot?Cyan:new Color(.22f,.32f,.37f));
            if(GUI.Button(new Rect(r.x+3,r.y+3,r.width-6,30),"",GUIStyle.none))SelectedSlot=index;
            Label(new Rect(r.x+9,r.y+4,132,24),u.Name,15,u.Enabled?White:Muted);
            Label(new Rect(r.x+9,r.y+29,130,21),new string('★',u.Star)+(u.Enabled?"  上阵":"  备战"),14,Gold);
            if(preparing)
            {
                if(Button(new Rect(r.x+8,r.y+58,132,29),"升星（体验）",u.Star<3)){Model.Upgrade(index);notice="角色已升星，站位保留；免费按钮仅用于比较成长。";SyncActors();}
                if(Button(new Rect(r.x+8,r.y+94,132,29),u.Enabled?"移至备战":"加入军阵",u.Enabled||Model.Units.Count(v=>v.Enabled)<Model.Capacity)){Model.ToggleSlot(index);SelectedSlot=index;SyncActors();}
            }
            else
            {
                string state=u.Enabled?(u.Alive?$"生命 {u.Health:0}/{u.MaxHealth:0}":$"复活 {u.RespawnRemaining:0.0} 秒"):"未上阵";
                Label(new Rect(r.x+9,r.y+55,132,22),state,13,u.Alive?Cyan:Gold);
                Label(new Rect(r.x+9,r.y+79,132,20),$"伤害 {u.DamageDone:0}  阵亡 {u.Deaths}",12,Muted);
                Label(new Rect(r.x+9,r.y+103,132,20),$"技能 {u.SkillCharge:0}/{u.SkillRequired:0}",12,Muted);
            }
        }
        void DrawWorldLabels()
        {
            foreach(var actor in Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(Model.Defenders))
            {
                if(!actor.Alive)continue;
                var world=WorldPoint(actor.Position);var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(world+Vector3.up*1.22f));
                if(p.y<121||p.y>522)continue;
                bool ally=actor is FormationFighter;
                Fill(new Rect(p.x-23,p.y,46,5),new Color(.015f,.025f,.03f,.9f));
                Fill(new Rect(p.x-22,p.y+1,44*Mathf.Clamp01(actor.Health/actor.MaxHealth),3),ally?Cyan:new Color(1,.42f,.32f));
                Label(new Rect(p.x-65,p.y-18,130,18),actor.Name,11,White,TextAnchor.MiddleCenter);
            }
            if(Model.Phase==FormationPhase.Preparation)
            {
                foreach(var u in Model.Units.Where(u=>u.Enabled))
                {
                    var p=CellScreen(u.Spawn);Label(new Rect(p.x-24,p.y-6,48,22),new string('★',u.Star),11,Gold,TextAnchor.MiddleCenter);
                    if(u.Slot==SelectedSlot)ApprovedUi.Border(new Rect(p.x-27,p.y-14,54,30),Cyan,2);
                }
            }
            foreach(var h in Model.Hits)
            {
                double age=Model.Clock-h.At;if(age<0||age>.8)continue;
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(WorldPoint(h.To)+Vector3.up*(1.35f+(float)age*.35f)));
                if(p.y<115||p.y>527)continue;
                Label(new Rect(p.x-48,p.y-10,96,24),(h.Heal?"+":"")+h.Amount.ToString("0"),h.Skill?20:15,h.Heal?Cyan:h.Skill?Gold:White,TextAnchor.MiddleCenter);
            }
        }
        void DrawSettlement()
        {
            bool won=Model.Phase==FormationPhase.Won;
            Fill(new Rect(380,184,520,310),Dark);ApprovedUi.Border(new Rect(380,184,520,310),won?Cyan:Gold,2);
            Label(new Rect(405,203,470,45),won?"防线肃清 · 蓝门突破":"进攻未完成",30,won?Cyan:Gold,TextAnchor.MiddleCenter);
            Label(new Rect(410,257,460,32),won?$"剩余 {Model.TimeRemaining:0.0} 秒 · 至少一名角色已进入蓝门":$"仍有 {Model.Defenders.Count(d=>d.Alive)} 名守军，或未能及时进门",16,White,TextAnchor.MiddleCenter);
            var top=Model.Units.OrderByDescending(u=>u.DamageDone).First();
            Label(new Rect(410,302,460,25),$"本关输出：{top.Name}  {top.DamageDone:0}    全队阵亡 {Model.Units.Sum(u=>u.Deaths)} 次",16,Muted,TextAnchor.MiddleCenter);
            Label(new Rect(412,339,456,40),"回到整备调整站位、升星或扩编，再比较同一防线。",14,Muted,TextAnchor.MiddleCenter);
            if(Button(new Rect(412,399,218,48),"同关整备重试"))Retry();
            if(Button(new Rect(650,399,218,48),Model.Round<5?"下一关 · 保留军阵":"完成体验 · 新挑战",won,true))
            {if(Model.Round<5)NextRound();else ResetDemo();}
        }
    }
}
