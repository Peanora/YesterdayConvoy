using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        private DefenderTrialDefinition defenderTrial;
        private DefenseTrialMap defenderTrialMap;
        public bool DefenderTrialActive => defenderTrial!=null;
        public bool TryEnterDefenderTrial()
        {
            var data=Resources.Load<DefenderTrialDefinition>("DefenderTrial");
            if(data==null) { Feedback="八人防线配置缺失"; return false; }
            _=data.Snapshot(); foreach(var d in data.defenders) _=DefenderArt.Load(d.artId);
            LeaveDefenderTrial(); defenderTrial=data;
            defenderTrialMap=new GameObject("Eight defenders / industrial trial").AddComponent<DefenseTrialMap>();
            defenderTrialMap.Initialize(scene,data);
            SwitchTestDefinition(data.battle,true);
            Feedback="八人防线：拖动塔露拉至红门；满技力后点击角色释放火雨";
            return true;
        }
        private void LeaveDefenderTrial()
        {
            defenderTrial=null;
            if(defenderTrialMap!=null) { defenderTrialMap.Restore(); Destroy(defenderTrialMap.gameObject); defenderTrialMap=null; }
        }
        private IEnumerator VerifyDefenderTrial(string directory)
        {
            Time.timeScale=0; bool entered=TryEnterDefenderTrial() && Battle.Defenders.Count==8;
            yield return null; yield return null;
            bool models=FindAnyObjectByType<DefenderRosterView>()?.Count==8;
            if(directory!=null) yield return InteractionFrame(directory,"defenders-ready");
            StartBattle(); Battle.SetPaused(true);
            Vector2 entry=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(ScreenPosition(RosterSlot.center)); ProcessPointerMove(entry);
            bool deployed=ProcessPointerUp(entry).Success && !Dragging && !Selected;
            double clock=Battle.Clock; Tick(2);
            bool paused=Battle.Clock==clock && Battle.IsPaused && Battle.Units.Count==1;
            Battle.SetPaused(false);
            bool attacked=false,healed=false,skill=false; double nextEvidence=3;
            for(int i=0;i<1800 && Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.05f);
                var unit=Battle.Units.Count>0 ? Battle.Units[0] : null;
                if(unit!=null && Battle.IsSkillReady(unit.EntityId))
                { SelectSkillUnit(unit.EntityId); skill|=ActivateSelectedSkill()==SkillFailure.None; }
                foreach(var shot in Battle.CombatShots)
                { if(shot.FromDefender && !shot.Healing) attacked=true; if(shot.Healing && shot.Applied) healed=true; }
                if(directory!=null && Battle.Clock>=nextEvidence)
                { yield return InteractionFrame(directory,"defenders-"+nextEvidence.ToString("0")); nextEvidence+=3; if(nextEvidence>12) nextEvidence=999; }
            }
            bool won=Battle.Phase==BattlePhase.Won;
            RestartBattle(); bool reset=Battle.Defenders.Count==8 && Battle.Units.Count==0;
            foreach(var d in Battle.Defenders) reset &= d.Health==d.MaxHealth && d.TargetId==null;
            ReturnToMonsterTests(); yield return null; yield return null;
            bool restored=!DefenderTrialActive && GameObject.Find("Industrial Map")!=null && Battle.Defenders.Count==1;
            Debug.Log($"DEFENDER_TRIAL_CHECK: entered={entered}; models={models}; deployed={deployed}; paused={paused}; attacked={attacked}; healed={healed}; skill={skill}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1; Application.Quit(entered && models && deployed && paused && attacked && healed && skill && won && reset && restored ? 0 : 1);
        }
    }
}
