using System;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        BossRosterEffectView bossRosterEffects;
        GUIStyle bossDescriptionStyle;
        string selectedViviSourceSkill="s3",selectedBossSourceVariant="TL-10";
        static string BossRosterMenuDescription(string id)
        {
            switch(id)
            {
                case "CARD_B01":return "延时C4 / 半血多目标\n独立技力与延迟引爆";
                case "CARD_M25":return "群体治疗 / 自然回复光环\n自动支援受伤友军，不普攻";
                case "CARD_M32":return "石盾 / 支柱 / 自动凝视\n复起进入悬浮形态";
                case "CARD_TALULAH_TRIAL":return "自动点燃 / 引爆与能量聚合\n被击倒后触发阶段火雨";
                case "CARD_M20":return "行军四连击 / 复起\n二形献祭与自动投枪";
                case "CARD_M14":return "冬痕黑冰 / 冰环\n复起后的寒潮与保护";
                case "CARD_M28":return "逐次蓄力 / 法术屏障\n自动占据合法留声机";
                case "CARD_B03":return "闪越阻挡者\n消失 / 现身重新接敌";
                case "CARD_B04":return "速杀位移 / 肆虐风雪\n复起与二阶段技能封锁";
                case "CARD_B05":return "烟雾隐匿 / 照明反隐\n闪现与真实炮击";
                case "CARD_B07":return "焦竭锁链 / 灼燃爆裂\n献祭逐火者获得护盾";
                case "CARD_B08":return "地火 / 旧日之火\n慢舞回血与终舞形态";
                case "CARD_B09":return "摄影区域 / 连续攻击\n摄影机封印与破封";
                case "CARD_B10":return "药剂支援 / 睡眠治疗\n无敌、不阻挡、不普攻";
                case "CARD_B11":return "慵钝之印 / 君王诏制\n复起后的全场威胁";
                case "CARD_B12":return "存续封印 / 黑冠真伤\n移动创意与区域推进";
                case "CARD_B13":return "复起远程形态\n真伤祭仪与离场返场";
                case "CARD_B14":return "痛吼 / 行于荒原\n猎犬、痛楚与真实波次";
                case "CARD_B15":return "国度 / 惘核减伤\n血量阈值讣告与搭档联动";
                case "CARD_B21":return "现身砸击 / 三次全场攻击\n半血觉醒与离场阶段";
                case "CARD_B22":return "双段攻击 / 狂躁减伤\n逃跑回场与伤害追踪投石";
                case "CARD_B23":return "十次齐射 / 蚀损减甲\n盐炮与二阶段闪越";
                case "CARD_B24":return "待部署标记 / 碰撞火球\n殆因减伤与阈值丧钟";
                case "CARD_B25":return "有限基质 / 多段审判\n吸收超限与瘫痪反制";
                case "CARD_B26":return "明晦切换 / 护盾\n实体梁与异属性碰撞";
                case "CARD_WISADEL_APRIL":return "准备阶段选择S1 / S2 / S3\n超新星原动作与魂灵之影";
                default:return null;
            }
        }
        System.Collections.Generic.IReadOnlyList<BossRosterSkillInfo> DisplayedBossSkills(DeployedUnitState u)=>BossRosterCatalog.Contains(u?.CardId)?Battle.BossRosterSkills(u):Battle.BossCoreSkills(u);
        void ConfigureBossPreparation()
        {
            if(Battle.Phase!=BattlePhase.Ready)return;
            Battle.ConfigureBossCoreViviSkill(selectedViviSourceSkill);
            if(!CampaignActive&&(definition.Card.CardId=="CARD_B15"||definition.Card.CardId=="CARD_B24"))Battle.ConfigureBossRosterSourceVariant(selectedBossSourceVariant);
        }
        void DrawBossPreparation()
        {
            if(Battle.Phase!=BattlePhase.Ready)return;
            bool vivi=false;foreach(var card in Battle.Cards)if(ViviCombat.IsVivi(card.Settings.CardId))vivi=true;
            if(vivi)
            {
                GUI.Label(new Rect(20,132,110,28),"维维美技能",smallStyle);
                string[] names={"定点清算","饱和复仇","爆裂黎明"};
                for(int i=0;i<3;i++){string id="s"+(i+1);if(GUI.Button(new Rect(130+i*116,130,110,30),(selectedViviSourceSkill==id?"● ":"")+names[i],buttonStyle)){selectedViviSourceSkill=id;Battle.ConfigureBossCoreViviSkill(id);}}
            }
            if(!CampaignActive&&(definition.Card.CardId=="CARD_B15"||definition.Card.CardId=="CARD_B24"))
            {
                GUI.Label(new Rect(20,132,110,28),"原关卡形态",smallStyle);
                if(GUI.Button(new Rect(130,130,140,30),(selectedBossSourceVariant=="TL-10"?"● ":"")+"普通",buttonStyle)){selectedBossSourceVariant="TL-10";Battle.ConfigureBossRosterSourceVariant(selectedBossSourceVariant);}
                if(GUI.Button(new Rect(278,130,180,30),(selectedBossSourceVariant=="TL-S-4"?"● ":"")+"进阶 · 三阈值",buttonStyle)){selectedBossSourceVariant="TL-S-4";Battle.ConfigureBossRosterSourceVariant(selectedBossSourceVariant);}
            }
        }
        void SyncBossRosterEffects()
        {
            if(bossRosterEffects==null){bossRosterEffects=gameObject.AddComponent<BossRosterEffectView>();bossRosterEffects.Bind(()=>Battle,()=>PresentationClock);bossRosterEffects.UseCombatShotFallback=false;}
            if(Battle==null)return;
            foreach(var e in Battle.BossRosterEvents)
                bossRosterEffects.Receive(e.Id,e.ActivationId,e.RunId,e.EntityId,e.CardId,e.Kind,e.SkillKey,e.NativeSkillKey,e.Animation,e.Phase,e.HitIndex,e.At,e.EndAt,e.Position,e.TargetPosition,e.TargetId,e.Applied,e.Cancelled,e.Region);
        }
        double BossRosterDeathTail(DeployedUnitState unit)
        {
            var art=BossRosterArt.For(unit?.CardId);if(art==null)return 0;
            if(Battle.BossRosterFrame(unit,out string raw,out _,out _)&&art.HasClip(raw))return art[raw].Duration;
            return art.DeathDuration;
        }
        bool IsBossRosterSelection=>Battle!=null&&DisplayedBossSkills(Battle.FindUnit(selectedEntityId)).Count>0;
        Rect BossSkillButton(int index){var panel=SkillPopupRect;bool auto=Battle.AutomaticSkillsEnabled;return new Rect(panel.x+12,panel.y+42+index*(auto?72:57),panel.width-24,auto?65:50);}
        static string BossSkillAvailability(SkillFailure result)
        {switch(result){case SkillFailure.None:return "点击施放";case SkillFailure.Paused:return "暂停中";case SkillFailure.NotReady:return "技力不足或形态未解锁";case SkillFailure.AlreadyActive:return "动作进行中";case SkillFailure.NoTarget:return "没有合法目标";case SkillFailure.MissingEntity:return "角色已退场";default:return "暂不可施放";}}
        bool TryBossRosterPopupPointer(Vector2 position)
        {
            if(Battle?.AutomaticSkillsEnabled==true)return true;
            if(!IsBossRosterSelection)return false;
            var unit=Battle.FindUnit(selectedEntityId);var skills=DisplayedBossSkills(unit);
            for(int i=0;i<skills.Count;i++)if(BossSkillButton(i).Contains(position))
            {
                var result=BossRosterCatalog.Contains(unit.CardId)?Battle.TryActivateBossRosterSkill(Battle.RunId,++requestSequence,unit.EntityId,skills[i].SkillId):Battle.TryActivateBossCoreSkill(Battle.RunId,++requestSequence,unit.EntityId);
                Feedback=result==SkillFailure.None?skills[i].Name+"已施放":skills[i].Name+"："+BossSkillAvailability(result);
                if(result==SkillFailure.None)CloseSkillMenu();return true;
            }
            return true;
        }
        void DrawBossRosterPopup()
        {
            var unit=Battle.FindUnit(selectedEntityId);var skills=DisplayedBossSkills(unit);var panel=SkillPopupRect;
            if(bossDescriptionStyle==null)bossDescriptionStyle=new GUIStyle(smallStyle){wordWrap=true};
            Fill(panel,new Color(.055f,.075f,.1f,.98f));Fill(new Rect(panel.x,panel.y,panel.width,3),ReadyGold);
            GUI.Label(new Rect(panel.x+12,panel.y+9,panel.width-24,27),CardForUnit(unit).DisplayName+(Battle.AutomaticSkillsEnabled?" · 自动技能":" · 技能"),textStyle);
            for(int i=0;i<skills.Count;i++)
            {
                var s=skills[i];var rect=BossSkillButton(i);Fill(rect,!Battle.AutomaticSkillsEnabled&&s.Ready?new Color(.35f,.43f,.28f):new Color(.18f,.23f,.27f));
                GUI.Label(new Rect(rect.x+7,rect.y+3,rect.width-14,22),new GUIContent(s.Name+(Battle.AutomaticSkillsEnabled?"":"  "+s.SP.ToString("0")+"/"+s.MaxSP.ToString("0")),s.Description),smallStyle);
                GUI.Label(new Rect(rect.x+7,rect.y+26,rect.width-14,Battle.AutomaticSkillsEnabled?36:22),Battle.AutomaticSkillsEnabled?Battle.AutomaticSkillStatus(unit,s.SkillId):Battle.IsPaused?"暂停中":s.Ready?"点击施放":BossSkillAvailability(s.Failure),Battle.AutomaticSkillsEnabled?bossDescriptionStyle:smallStyle);
            }
            GUI.Label(new Rect(panel.x+12,panel.yMax-26,panel.width-24,22),Battle.AutomaticSkillsEnabled?"独立自动冷却 · 点空白关闭":"实体独立技力 · 点空白取消",smallStyle);
            if(!string.IsNullOrEmpty(GUI.tooltip))
            {
                if(bossDescriptionStyle==null)bossDescriptionStyle=new GUIStyle(smallStyle){wordWrap=true};
                float dx=panel.xMax+10;if(dx+300>1264)dx=Mathf.Max(16,panel.x-310);
                var details=new Rect(dx,Mathf.Clamp(panel.y,78,402),300,144);Fill(details,new Color(.055f,.075f,.1f,.98f));
                GUI.Label(new Rect(details.x+12,details.y+10,276,124),GUI.tooltip,bossDescriptionStyle);
            }
        }
    }
}
