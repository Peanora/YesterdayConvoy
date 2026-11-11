using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        BossArenaDefinition bossArena;
        public bool BossArenaActive=>bossArena!=null;
        public float ArenaVisualScale=>bossArena?.bossVisualScale??1;
        public void BindBossArena(BossArenaDefinition data)=>bossArena=data??throw new ArgumentNullException(nameof(data));
        public void DeployArenaReadySquad()
        {
            if(!BossArenaActive||Battle==null)return;
            int count=Battle.DeployReadyArenaSquad(true);SyncViews();
            ShopNotice(count>0?$"再部署 {count} 个编队成员":"没有已就绪的成员");
        }
        public void OpenArenaPreparation()
        {
            if(!BossArenaActive||Loop==null)return;
            Loop.SandboxPreparation();ConfigureLoopStage();SetShopVisible(true);CloseSkillMenu();
        }
        public void FrameBossArena()
        {
            var c=Camera.main;c.rect=TacticalViewport.Current.CameraRect(new Rect(0,0,1280,720));
            c.transform.rotation=Quaternion.Euler(60,0,0);c.fieldOfView=24;
            c.transform.position=new Vector3(0,1.9f,.2f)-c.transform.forward*17;
        }
        void DrawArenaFight()
        {
            EnsureStyles();var e=Event.current;var point=TacticalViewport.Current.FromTopLeft(e.mousePosition);
            bool field=point.y>130&&point.y<548&&point.x>190&&point.x<1120;
            if(field&&ShopPointerEvent(e.type,point,e.button))e.Use();
            Matrix4x4 matrix=GUI.matrix;Color old=GUI.color;GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                DrawArenaHeader();
                foreach(var unit in Battle.Units)
                {
                    if(!unit.Active)continue;
                    var p=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit)));
                    Fill(new Rect(p.x-25,p.y,50,5),new Color(.02f,.04f,.06f,.9f));
                    Fill(new Rect(p.x-24,p.y+1,48*unit.Health/unit.MaxHealth,3),new Color(.4f,.9f,.55f));
                    if(Battle.UnitCombat(unit).HasSkill)DrawSkillResourceBar(p.x,p.y+7,unit);
                }
                DrawInteractionOverlay();DrawArenaRoster();
                if(Battle.ActiveCount==0&&Battle.Phase==BattlePhase.Running)
                {
                    double next=double.PositiveInfinity;foreach(var card in Battle.Cards)next=Math.Min(next,Battle.ArenaRedeploymentRemaining(card.Settings.CardId));
                    GUI.Label(new Rect(380,402,560,66),next>0?$"部队重整中 · 最早 {next:0} 秒后可再部署\nBoss已受伤害保留":"部队已就绪 · 点击整队再部署",LoopText(19,TextAnchor.MiddleCenter,Color.white,600));
                }
                if(Battle.IsPaused)GUI.Label(new Rect(500,260,280,60),"暂停中",LoopText(27,TextAnchor.MiddleCenter,Color.white,700));
                if(Battle.Phase==BattlePhase.Won&&!DeathPresentationPending)DrawArenaResult();
                if(Time.unscaledTime<shopMessageUntil&&!string.IsNullOrEmpty(shopMessage))
                    GUI.Label(new Rect(350,518,650,28),shopMessage,LoopText(14,TextAnchor.MiddleCenter,Color.white));
            }
            finally{GUI.matrix=matrix;GUI.color=old;GUI.enabled=true;}
        }
        void DrawArenaResult()
        {
            Fill(new Rect(350,202,580,266),new Color(.035f,.055f,.075f,.97f));
            GUI.Label(new Rect(380,222,520,48),"攻擂成功",LoopText(34,TextAnchor.MiddleCenter,new Color(1,.86f,.53f),700));
            GUI.Label(new Rect(380,282,520,55),$"击败维什戴尔 · {bossArena.skinName}\n耗时 {Battle.Clock/60:00}:{Battle.Clock%60:00}   累计部署 {Battle.ArenaDeployments}",LoopText(17,TextAnchor.MiddleCenter,Color.white));
            if(LoopButton(new Rect(430,370,420,50),"返回整备，再次攻擂",true))OpenArenaPreparation();
        }
        void DrawArenaHeader()
        {
            var boss=Battle.ArenaBoss;if(boss==null)return;
            Color red=Battle.ArenaEnraged?new Color(1,.28f,.12f):new Color(.91f,.21f,.26f);
            Fill(new Rect(0,0,1280,126),new Color(.025f,.035f,.05f,.96f));
            GUI.Label(new Rect(32,12,180,28),"昨日圆车 / 攻擂",LoopText(17,TextAnchor.MiddleLeft,Color.white,600));
            GUI.Label(new Rect(258,8,650,34),"BOSS   维什戴尔 · "+bossArena.skinName,LoopText(23,TextAnchor.MiddleLeft,Color.white,700));
            GUI.Label(new Rect(258,40,680,22),Battle.ArenaEnraged?"Ⅱ  爆裂黎明 · 攻速提升 "+bossArena.enragedAttackSpeed.ToString("0.#")+"×":"Ⅰ  爆裂黎明 · 低于50%生命进入加速阶段",LoopText(12,TextAnchor.MiddleLeft,red,600));
            var bar=new Rect(258,70,750,18);Fill(new Rect(bar.x-2,bar.y-2,bar.width+4,bar.height+4),new Color(.5f,.55f,.59f));
            Fill(bar,new Color(.12f,.15f,.18f));Fill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(boss.Health/boss.MaxHealth),bar.height),red);
            for(int i=1;i<10;i++)Fill(new Rect(bar.x+bar.width*i/10,bar.y,1,bar.height),new Color(.02f,.03f,.04f,.65f));
            Fill(new Rect(bar.center.x-1,bar.y-4,2,bar.height+8),Color.white);
            GUI.Label(new Rect(258,92,750,25),$"{boss.Health:0} / {boss.MaxHealth:0}     {boss.Health/boss.MaxHealth:P1}",LoopText(13,TextAnchor.MiddleLeft,Color.white,600));
            GUI.Label(new Rect(32,56,190,50),$"{Battle.Clock/60:00}:{Battle.Clock%60:00}   场上 {Battle.ActiveCount}\n阵亡后 {bossArena.redeploymentSeconds:0} 秒再部署",LoopText(13,TextAnchor.MiddleLeft,new Color(.8f,.85f,.9f)));
            if(LoopButton(new Rect(1030,12,70,34),SimulationSpeed+"×"))ToggleSpeed();
            if(LoopButton(new Rect(1106,12,70,34),Battle.IsPaused?"继续":"暂停"))Battle.SetPaused(!Battle.IsPaused);
            if(LoopButton(new Rect(1182,12,74,34),"整备"))OpenArenaPreparation();
            if(LoopButton(new Rect(1030,56,100,34),Muted?"开启音效":"关闭音效"))ToggleMute();
            if(LoopButton(new Rect(1142,56,114,34),"返回模式"))SceneManager.LoadScene("MonsterTrial");
        }
        void DrawArenaRoster()
        {
            Fill(new Rect(0,552,1280,168),new Color(.025f,.04f,.06f,.95f));
            GUI.Label(new Rect(22,557,175,34),"整队出击 / 无DP",LoopText(15,TextAnchor.MiddleLeft,Color.white,600));
            GUI.Label(new Rect(206,552,900,24),"在场成员不能重复部署 · 阵亡后开始待部署计时 · 就绪后点击卡牌或整队再部署",LoopText(12,TextAnchor.MiddleLeft,new Color(.75f,.83f,.88f)));
            for(int i=0;i<shopLineup.Count;i++)
            {
                string id=shopLineup[i];var card=shopDefinitions[id].Card;Rect rect=LoopRosterRect(i);
                DrawLoopCostBackground(rect,card.RecruitCost);DrawTestPortrait(new Rect(rect.x+2,rect.y+2,rect.width-4,91),OriginalTalulahArt.CardPortrait(card));
                bool onField=Battle.ArenaCardOnField(id);double cd=Battle.ArenaRedeploymentRemaining(id);
                if(onField||cd>0)Fill(new Rect(rect.x,rect.y,rect.width,96),new Color(.03f,.05f,.08f,.65f));
                GUI.Label(new Rect(rect.x,rect.y+33,rect.width,28),onField?"作战中":cd>0?$"{cd:0}s":"可再部署",LoopText(14,TextAnchor.MiddleCenter,Color.white,600));
                Fill(new Rect(rect.x,rect.yMax-27,rect.width,27),new Color(.12f,.18f,.23f));
                GUI.Label(new Rect(rect.x+4,rect.yMax-27,rect.width-8,27),card.DisplayName+new string('★',ShopLineupPiece(id)?.Star??1),LoopText(10,TextAnchor.MiddleLeft,Color.white));
                GUI.enabled=Battle.Phase==BattlePhase.Running&&!onField&&cd<=1e-6;
                if(GUI.Button(rect,GUIContent.none,GUIStyle.none))
                {
                    var result=Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Lanes[i%Battle.Lanes.Count].Entry,id,true);
                    if(result.Success)SyncViews();
                }
                GUI.enabled=true;
            }
            GUI.enabled=Battle.Phase==BattlePhase.Running;
            if(LoopButton(new Rect(1125,593,136,54),"整队再部署",true))DeployArenaReadySquad();GUI.enabled=true;
            GUI.Label(new Rect(1125,655,136,40),"暂停冻结计时\n空格 暂停 / 继续",LoopText(12,TextAnchor.MiddleCenter,new Color(.75f,.83f,.88f)));
        }
    }
}
