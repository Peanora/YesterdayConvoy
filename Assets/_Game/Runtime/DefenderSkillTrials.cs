using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        private bool defenderMenuOpen;
        public bool DefenderSkillTrialActive=>defenderTrial!=null&&!string.IsNullOrEmpty(defenderTrial.defenderTestId);
        public string DefenderSkillTrialId=>DefenderSkillTrialActive?defenderTrial.defenderTestId:null;
        public static readonly string[] DefenderBatch1Ids={"D02","D05","D06","D11"};
        public bool TryEnterDefenderSkillTrial(string id)
        {
            if(Array.IndexOf(DefenderBatch1Ids,id)<0)return false;
            var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);
            if(data==null)return false;
            defenderMenuOpen=false;return EnterDefense(data);
        }
        public static string DefenderSkillName(DefenderSkillKind kind)=>kind==DefenderSkillKind.CardiganRecovery?"生命回复·α型":
            kind==DefenderSkillKind.MidnightEnchant?"武器附魔·α型":kind==DefenderSkillKind.PlumeSwiftStrike?"迅捷打击·α型":kind==DefenderSkillKind.AnselRange?"治疗范围强化":"无主动技能";
        private void DrawDefenderSelection()
        {
            Fill(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f));
            GUI.Label(new Rect(54,48,880,64),"守军技能测试 · 第一批 4 / 22",menuTitle);
            GUI.Label(new Rect(56,120,1150,46),"守军会自动选择开技能时机。拖动底部怪物卡进攻；技能效果、技力与剩余时间显示在守军上方。",menuBody);
            if(GUI.Button(new Rect(960,48,260,55),"返回怪物选择",buttonStyle))defenderMenuOpen=false;
            int i=0;
            foreach(string id in DefenderBatch1Ids)
            {
                var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);if(data==null)continue;
                var selected=data.defenders[0];float x=56+i++*298;var art=DefenderArt.Load(selected.artId);
                Fill(new Rect(x,190,278,416),new Color(.065f,.10f,.13f));
                DrawTestPortrait(new Rect(x+16,206,246,212),art.Portrait);
                GUI.Label(new Rect(x+20,430,240,35),selected.displayName,menuCardName);
                GUI.Label(new Rect(x+20,474,240,64),DefenderSkillName(selected.skill.kind)+"\n"+(selected.role==DefenderRole.Medic?"高台医疗 · 基础治疗与扩范围":"地面守军 · 阻挡与技能交战"),menuSmall);
                if(GUI.Button(new Rect(x+20,550,238,40),"进入技能测试",buttonStyle))TryEnterDefenderSkillTrial(id);
            }
            GUI.Label(new Rect(56,634,1150,44),"空格暂停；暂停时也能拖动部署。重新开始会重置双方生命、部署次数和技能。",menuBody);
        }
        private IEnumerator VerifyDefenderSkillTrial(string id,string directory)
        {
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
