using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool OriginalEightSkillObserved(string id)
        {
            var d=Battle.FindDefender(id);if(d==null||d.SkillCasts==0)return false;
            switch(id)
            {
                case "D01":return Battle.DefenderArmor(d)>d.Settings.Armor;
                case "D03":case "D10":return Battle.CombatShots.Any(s=>s.SourceId==id&&s.Healing&&s.Applied&&s.Attack>d.Settings.Attack);
                case "D04":return Battle.CombatShots.Any(s=>s.SourceId==id&&!s.Healing&&s.Applied&&s.Attack>d.Settings.Attack);
                case "D07":return Battle.CombatShots.Count(s=>s.SourceId==id&&s.Empowered&&s.Applied)>=2;
                case "D08":return Battle.CombatShots.Any(s=>s.SourceId==id&&s.Empowered&&s.Applied&&s.DamageType==CombatDamageType.Arts);
                case "D09":return d.LastActionRate>1;
                case "D12":return Battle.Units.Any(u=>u.HinderedUntil>Battle.Clock)&&d.LastActionRate>1;
                default:return false;
            }
        }
        private IEnumerator VerifyOriginalEightSkill(string id,string directory)
        {
            Time.timeScale=0;
            if(directory!=null){ReturnToMonsterTests();defenderMenuOpen=true;yield return TacticalFrame(directory,"original-eight-selection");}
            bool entered=TryEnterDefenderSkillTrial(id);yield return null;yield return null;
            var d=Battle.FindDefender(id);var views=FindAnyObjectByType<DefenderRosterView>();var visual=views.FindVisual(id);
            bool models=views.Count==Battle.Defenders.Count&&visual.sprite.name.StartsWith(id);
            var idle=visual.sprite;Tick(.73f);yield return null;bool preparation=Battle.Clock==0&&visual.sprite!=idle;
            if(directory!=null)yield return TacticalFrame(directory,id+"-ready");
            StartBattle();Battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool deployed=ProcessPointerUp(gate).Success;
            double clock=Battle.Clock;float sp=d.SkillSP;Tick(10);yield return null;bool paused=Battle.Clock==clock&&d.SkillSP==sp;
            Battle.SetPaused(false);bool effect=false,assault=false,cast=false,captured=false;
            for(int i=0;i<18000&&Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.1f);effect|=OriginalEightSkillObserved(id);
                if(effect)
                {
                    if(!captured&&directory!=null){yield return TacticalFrame(directory,id+"-skill");captured=true;}
                    if(!assault)
                    {
                        ProcessPointerDown(ScreenPosition(CardSlot(0).center));ProcessPointerMove(gate);assault=ProcessPointerUp(gate).Success;
                    }
                    var unit=Battle.Units.FirstOrDefault(u=>u.Active&&Battle.IsSkillReady(u.EntityId));
                    if(unit!=null){SelectSkillUnit(unit.EntityId);cast|=ActivateSelectedSkill()==SkillFailure.None;}
                }
                if(i%100==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null)yield return TacticalFrame(directory,id+"-result");
            RestartBattle();d=Battle.FindDefender(id);bool reset=d.SkillCasts==0&&d.SkillSP==d.Settings.Skill.InitialSP&&d.Health==d.MaxHealth&&Battle.Units.Count==0;
            ReturnToMonsterTests();yield return null;yield return null;bool restored=TestSelectionOpen&&!DefenderSkillTrialActive&&!TacticalMapActive;
            Debug.Log($"ORIGINAL_EIGHT_SKILL: id={id}; entered={entered}; models={models}; preparation={preparation}; deployed={deployed}; paused={paused}; effect={effect}; assault={assault}; cast={cast}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&models&&preparation&&deployed&&paused&&effect&&assault&&cast&&won&&reset&&restored?0:1);
        }
    }
}
