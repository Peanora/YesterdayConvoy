using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        string inspectedDefender;
        public static Rect DefenderInspectorRect=>new Rect(681,110,316,410);
        public bool DefenderInspectorVisible=>ShopTrialActive&&Battle.Defenders.Any(d=>d.EntityId==inspectedDefender);
        public string InspectedDefenderId=>inspectedDefender;
        public void CloseDefenderInspector()=>inspectedDefender=null;
        bool InspectShopDefender(Vector2 point)
        {
            var candidate=Battle.Defenders.Select(d=>new{Defender=d,Foot=GUIPosition(Camera.main.WorldToScreenPoint(d.Position)),Head=GUIPosition(Camera.main.WorldToScreenPoint(d.Position+Camera.main.transform.up*1.5f))})
                .Where(p=>new Rect(p.Foot.x-23,p.Head.y-8,46,p.Foot.y-p.Head.y+20).Contains(point))
                .OrderBy(p=>Vector2.Distance(point,(p.Foot+p.Head)*.5f)).FirstOrDefault();
            if(candidate==null)return false;
            inspectedDefender=candidate.Defender.EntityId;return true;
        }
        DeploymentLane ShopDragLane()
        {
            if(!ShopTrialActive||shopDrag<0||!shopMoving||!ShopMapPointAvailable(shopPointer))return null;
            return PointerGround(ScreenPosition(shopPointer),out var world)?Battle.FindEntryLane(world):null;
        }
        static string SkillSummary(DefenderSkillKind kind)
        {
            switch(kind)
            {
                case DefenderSkillKind.LapplandHavoc:return "三架浮游单元全场追击；恐惧与减速。普通攻击逐渐强化单元伤害。";
                case DefenderSkillKind.YuKitchen:return "提高攻击、防御和生命上限；布置火墙并回复生命。存活守军达到四名时提供全体回复。";
                case DefenderSkillKind.SariaCalcification:return "停止普通治疗，持续群体治疗；范围内敌人减速，并承受更多法术伤害。";
                case DefenderSkillKind.MlynarGlory:return "平时蓄力、不攻击也不阻挡。开启后扩大范围，对最多五个目标爆发攻击，随后重新蓄力。";
                case DefenderSkillKind.PramanixSnow:return "平时不普攻。技能布置雪域，减速并造成范围伤害，带有牵引诱饵。";
                case DefenderSkillKind.WangGambit:return "积蓄棋子；技能消耗弹药，棋子追击、连锁并布置陷阱。";
                default:return "技能按现有角色配置自动触发。";
            }
        }
        void DrawDefenderInspector(GUIStyle label,GUIStyle button)
        {
            if(!DefenderInspectorVisible)return;var d=Battle.Defenders.First(p=>p.EntityId==inspectedDefender);var r=DefenderInspectorRect;
            var tint=d.HighGround?new Color(.35f,.85f,1):new Color(1,.73f,.36f);
            var center=GUIPosition(Camera.main.WorldToScreenPoint(d.Position));
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;float radius=Battle.DefenderRange(d);
                var p=d.Position+new Vector3(Mathf.Cos(a)*radius,.08f,Mathf.Sin(a)*radius);
                var q=d.Position+new Vector3(Mathf.Cos(b)*radius,.08f,Mathf.Sin(b)*radius);
                ShopLine(GUIPosition(Camera.main.WorldToScreenPoint(p)),GUIPosition(Camera.main.WorldToScreenPoint(q)),new Color(tint.r,tint.g,tint.b,.65f),1);
            }
            ShopBorder(new Rect(center.x-20,center.y-8,40,16),tint,2);
            Fill(r,new Color(.025f,.055f,.075f,.97f));ShopBorder(r,tint,1);
            var text=new GUIStyle(label){fontSize=14,wordWrap=true,alignment=TextAnchor.UpperLeft};
            GUI.Label(new Rect(r.x+16,r.y+14,255,30),d.Settings.Name+"  ★★★★★★",new GUIStyle(text){fontSize=17,fontStyle=FontStyle.Bold,normal={textColor=tint}});
            if(GUI.Button(new Rect(r.xMax-37,r.y+10,27,27),"×",button)){CloseDefenderInspector();return;}
            GUI.Label(new Rect(r.x+16,r.y+50,284,40),d.HighGround?"高台 · 不阻挡\n地面近战无法攻击，远程可攻击":"地面 · 阻挡 "+(Battle.DefenderCanBlock(d)?d.Settings.BlockCount:0),text);
            GUI.Label(new Rect(r.x+16,r.y+98,284,66),$"生命  {d.Health:0} / {d.MaxHealth:0}\n攻击  {Battle.DefenderSkillAttack(d):0.#}    防御  {Battle.DefenderArmor(d):0.#}\n范围  {Battle.DefenderRange(d):0.#}    攻击间隔  {Battle.DefenderEffectiveInterval(d):0.##}秒",text);
            Fill(new Rect(r.x+16,r.y+171,284,1),new Color(.3f,.42f,.47f));
            var skill=d.Settings.Skill;
            GUI.Label(new Rect(r.x+16,r.y+182,284,28),DefenderSkillName(skill.Kind),new GUIStyle(text){fontSize=17,fontStyle=FontStyle.Bold,normal={textColor=new Color(1,.83f,.45f)}});
            string status=!d.Alive?"已退场":Battle.DefenderSkillActive(d)?$"发动中 · 剩余 {Mathf.Max(0,(float)(d.SkillActiveUntil-Battle.Clock)):0.0}秒":$"技力 {d.SkillSP:0.#} / {skill.RequiredSP:0.#}";
            GUI.Label(new Rect(r.x+16,r.y+216,284,22),status+$"   ·   发动 {d.SkillCasts} 次",text);
            string detail=skill.Kind==DefenderSkillKind.WangGambit?$"弹药 {d.Ammo} · 储棋 {d.ChessStock}":skill.Kind==DefenderSkillKind.MlynarGlory?$"蓄力 {Battle.DefenderCharge(d)*100:0}% · "+(Battle.DefenderCanAttack(d)?"可攻击":"蓄力待机"):"技能自动发动 · 可暂停查看";
            GUI.Label(new Rect(r.x+16,r.y+239,284,22),detail,new GUIStyle(text){fontSize=12});
            GUI.Label(new Rect(r.x+16,r.y+260,284,112),SkillSummary(skill.Kind),text);
            GUI.Label(new Rect(r.x+16,r.y+379,284,24),"实时数值 · 点击空地关闭",new GUIStyle(text){fontSize=12,normal={textColor=new Color(.6f,.74f,.8f)}});
        }
        void DrawShopRouteLabels(GUIStyle label)
        {
            if(TacticalMapId==BlackForestLayout.Id){DrawBlackForestRouteLabels(label);return;}
            if(!StrongholdChallengeLayout.IsStronghold(TacticalMapId))return;
            var selected=ShopDragLane();
            foreach(var lane in Battle.Lanes)
            {
                var p=GUIPosition(Camera.main.WorldToScreenPoint(lane.Entry));
                GUI.Label(new Rect(p.x-74,p.y+18,148,20),lane.Id,new GUIStyle(label){fontSize=12,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.52f,.4f)}});
                var end=GUIPosition(Camera.main.WorldToScreenPoint(lane.Points[lane.Points.Count-1]));
                GUI.Label(new Rect(end.x-65,end.y+12,130,22),lane.ExitId+(Battle.StrongholdExitBroken(lane.ExitId)?" ✓":" · 待突破"),new GUIStyle(label){fontSize=12,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.4f,.85f,1)}});
            }
            if(Loop==null&&Battle.StrongholdChallenge&&selected==null&&ShopPanelVisibility<.01f)
            {
                Fill(new Rect(17,545,490,47),new Color(.02f,.05f,.07f,.84f));
                GUI.Label(new Rect(25,548,474,42),Loop==null?"两座蓝门各突破一次 · 点击守军查看详情\n高台只能被远程攻击 · 观察技能后再出兵":"两座蓝门各突破一次 · 拖动下方怪物到红门\n胜利后结算金币，再购买和调整下一关阵容",new GUIStyle(label){fontSize=13,alignment=TextAnchor.MiddleLeft});
            }
            if(selected==null)return;
            int portal=StrongholdChallengeLayout.PortalSource(selected.Points);
            for(int i=1;i<selected.Points.Count;i++)
            {
                if(i==portal+1)continue;
                Vector3 a=selected.Points[i-1],b=selected.Points[i];Vector2 p=GUIPosition(Camera.main.WorldToScreenPoint(Vector3.Lerp(a,b,.6f)+Vector3.up*.12f));
                Vector2 dir=(GUIPosition(Camera.main.WorldToScreenPoint(b))-GUIPosition(Camera.main.WorldToScreenPoint(a))).normalized;
                Vector2 wing=new Vector2(-dir.y,dir.x)*4;Color c=i<=portal?new Color(1,.72f,.2f):new Color(.3f,.9f,1);
                ShopLine(p-dir*7+wing,p,c);ShopLine(p-dir*7-wing,p,c);
            }
            var source=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[portal]));
            var destination=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[portal+1]));
            GUI.Label(new Rect(source.x-65,source.y-38,130,26),"① 传送 · 等待3秒",new GUIStyle(label){fontSize=13,alignment=TextAnchor.MiddleCenter});
            GUI.Label(new Rect(destination.x-55,destination.y-33,110,24),"② 从此处继续",new GUIStyle(label){fontSize=13,alignment=TextAnchor.MiddleCenter});
            Fill(new Rect(330,550,620,32),new Color(.02f,.06f,.08f,.95f));
            GUI.Label(new Rect(330,550,620,32),selected.Id+" → "+selected.ExitId+"   金色：传送前 / 青色：传送后",new GUIStyle(label){fontSize=14,alignment=TextAnchor.MiddleCenter});
        }
    }
}
