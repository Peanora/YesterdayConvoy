using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        private bool defenderMenuOpen;
        private bool originalEightTab=true,batch4Tab=true;
        public bool DefenderSkillTrialActive=>defenderTrial!=null&&!string.IsNullOrEmpty(defenderTrial.defenderTestId);
        public string DefenderSkillTrialId=>DefenderSkillTrialActive?defenderTrial.defenderTestId:null;
        public static readonly string[] DefenderBatch4Ids={"D21","D22","D29","D30"};
        public static readonly string[] DefenderBatch1Ids={"D02","D05","D06","D11"};
        public static readonly string[] OriginalEightIds={"D01","D03","D04","D07","D08","D09","D10","D12"};
        public bool TryEnterDefenderSkillTrial(string id)
        {
            if(Array.IndexOf(DefenderBatch1Ids,id)<0&&Array.IndexOf(OriginalEightIds,id)<0&&Array.IndexOf(DefenderBatch4Ids,id)<0)return false;
            var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);
            if(data==null)return false;
            defenderMenuOpen=false;return EnterDefense(data);
        }
        public static string DefenderSkillName(DefenderSkillKind kind)
        {
            switch(kind)
            {
                case DefenderSkillKind.CardiganRecovery:return "生命回复·α型";
                case DefenderSkillKind.MidnightEnchant:return "武器附魔·α型";
                case DefenderSkillKind.PlumeSwiftStrike:case DefenderSkillKind.OrchidSwiftStrike:return "迅捷打击·α型";
                case DefenderSkillKind.AnselRange:return "治疗范围强化";
                case DefenderSkillKind.BeagleGuard:return "防御力强化·α型";
                case DefenderSkillKind.SpotHealingMode:return "次级治疗模式";
                case DefenderSkillKind.MelanthaStrength:return "攻击力强化·α型";
                case DefenderSkillKind.KroosDoubleShot:return "二连射·自动";
                case DefenderSkillKind.StewardStrike:return "强力击·α型";
                case DefenderSkillKind.LavaChant:return "战术咏唱·α型";
                case DefenderSkillKind.HibiscusHealing:return "治疗强化·α型";
                case DefenderSkillKind.LapplandHavoc:return "终幕·浩劫";
                case DefenderSkillKind.YuKitchen:return "灶里乾坤";
                case DefenderSkillKind.SariaCalcification:return "钙质化";
                case DefenderSkillKind.MlynarGlory:return "未照耀的荣光";
                default:return "无主动技能";
            }
        }
        private Vector2 defenderMenuScroll;
        private void DrawDefenderSelection()
        {
            Fill(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f));
            GUI.Label(new Rect(54,48,880,64),"守军技能测试 · 16名可玩",menuTitle);
            GUI.Label(new Rect(56,120,1150,46),"守军会自动选择开技能时机。拖动底部怪物卡进攻；技能效果、技力与剩余时间显示在守军上方。",menuBody);
            if(GUI.Button(new Rect(960,48,260,55),"返回怪物选择",buttonStyle))defenderMenuOpen=false;
            if(GUI.Button(new Rect(56,153,280,32),"原八人防线",buttonStyle)){originalEightTab=true;batch4Tab=false;defenderMenuScroll=Vector2.zero;}
            if(GUI.Button(new Rect(354,153,280,32),"新增四名守军",buttonStyle)){originalEightTab=false;batch4Tab=false;defenderMenuScroll=Vector2.zero;}
            if(GUI.Button(new Rect(652,153,280,32),"六星第四批",buttonStyle)){batch4Tab=true;defenderMenuScroll=Vector2.zero;}
            int i=0;
            var ids=batch4Tab?DefenderBatch4Ids:originalEightTab?OriginalEightIds:DefenderBatch1Ids;
            defenderMenuScroll=GUI.BeginScrollView(new Rect(48,192,1184,424),defenderMenuScroll,new Rect(0,0,1160,Mathf.Ceil(ids.Length/4f)*420));
            foreach(string id in ids)
            {
                var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);if(data==null)continue;
                var selected=data.defenders[0];float x=8+i%4*292,y=i/4*420;i++;var art=DefenderArt.Load(selected.artId);
                Fill(new Rect(x,y,272,408),new Color(.065f,.10f,.13f));
                DrawTestPortrait(new Rect(x+16,y+16,240,204),art.Portrait);
                GUI.Label(new Rect(x+20,y+238,232,35),selected.displayName,menuCardName);
                GUI.Label(new Rect(x+20,y+282,232,64),DefenderSkillName(selected.skill.kind)+"\n"+(selected.role==DefenderRole.Medic?"高台医疗 · 治疗支援":selected.position.y>.2f?"高台守军 · 远程火力":"地面守军 · 阻挡与技能交战"),menuSmall);
                if(GUI.Button(new Rect(x+20,y+358,232,40),"进入技能测试",buttonStyle))TryEnterDefenderSkillTrial(id);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(56,634,1150,44),"空格暂停；暂停时也能拖动部署。重新开始会重置双方生命、部署次数和技能。",menuBody);
        }
        private IEnumerator VerifyDefenderSkillTrial(string id,string directory)
        {
            if(Array.IndexOf(DefenderBatch4Ids,id)>=0){yield return VerifyBatch4Skill(id,directory);yield break;}
            if(Array.IndexOf(OriginalEightIds,id)>=0){yield return VerifyOriginalEightSkill(id,directory);yield break;}
            Time.timeScale=0;
            if(directory!=null){ReturnToMonsterTests();yield return TacticalFrame(directory,"main-selection");defenderMenuOpen=true;yield return TacticalFrame(directory,"defender-selection");}
            bool entered=TryEnterDefenderSkillTrial(id);yield return null;yield return null;
            var selected=Battle.FindDefender(id);bool models=selected!=null&&FindAnyObjectByType<DefenderRosterView>()?.Count==Battle.Defenders.Count;
            var visual=FindAnyObjectByType<DefenderRosterView>().FindVisual(id);var idle=visual.sprite;Tick(.73f);yield return null;
            bool preparation=Battle.Clock==0&&visual.sprite!=idle;
            if(directory!=null)yield return TacticalFrame(directory,id+"-ready");
            StartBattle();Battle.SetPaused(true);var lane=Battle.Lanes[0];
            ProcessPointerDown(ScreenPosition(CardSlot(id=="D06"?2:0).center));ProcessPointerMove(Camera.main.WorldToScreenPoint(lane.Entry));
            bool deployed=ProcessPointerUp(Camera.main.WorldToScreenPoint(lane.Entry)).Success;
            double clock=Battle.Clock;float sp=selected.SkillSP;Tick(10);yield return null;
            bool paused=Battle.Clock==clock&&selected.SkillSP==sp;
            Battle.SetPaused(false);bool guardSkill=false,playerSkill=id=="D06",healed=false,arts=false,speed=false,captured=false;
            for(int i=0;i<16000&&Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.1f);
                guardSkill|=selected.SkillCasts>0;
                healed|=selected.SkillHealthRestored>0||Battle.CombatShots.Any(s=>s.SourceId==id&&s.Healing&&s.Applied);
                arts|=Battle.CombatShots.Any(s=>s.SourceId==id&&s.DamageType==CombatDamageType.Arts);
                speed|=selected.LastActionRate>1;
                if(guardSkill)
                {
                    if(!captured&&directory!=null){yield return TacticalFrame(directory,id+"-skill");captured=true;}
                    var unit=Battle.Units.FirstOrDefault(u=>u.Active&&Battle.IsSkillReady(u.EntityId));
                    if(unit!=null){SelectSkillUnit(unit.EntityId);playerSkill|=ActivateSelectedSkill()==SkillFailure.None;}
                }
                if(i%100==0)yield return null;
            }
            bool effect=id=="D02"?healed:id=="D05"?arts:id=="D06"?speed:healed;
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null)yield return TacticalFrame(directory,id+"-result");
            RestartBattle();selected=Battle.FindDefender(id);
            bool reset=selected.SkillCasts==0&&selected.SkillSP==selected.Settings.Skill.InitialSP&&selected.Health==selected.MaxHealth&&Battle.Units.Count==0;
            ReturnToMonsterTests();yield return null;yield return null;
            bool restored=TestSelectionOpen&&!DefenderSkillTrialActive&&!TacticalMapActive;
            Debug.Log($"DEFENDER_SKILL_TRIAL: id={id}; entered={entered}; models={models}; preparation={preparation}; deployed={deployed}; paused={paused}; skill={guardSkill}; effect={effect}; playerSkillOrNotRequired={playerSkill}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&models&&preparation&&deployed&&paused&&guardSkill&&effect&&playerSkill&&won&&reset&&restored?0:1);
        }
    }
}
