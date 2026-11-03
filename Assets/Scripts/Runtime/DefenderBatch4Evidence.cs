using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool Batch4SkillObserved(string id)
        {
            var d=Battle.FindDefender(id);if(d==null||d.SkillCasts==0)return false;
            switch(id)
            {
                case "D21":return d.FearApplications>0&&Battle.CombatShots.Any(s=>s.SourceId==id&&s.Drone&&s.Empowered&&s.Applied);
                case "D22":return d.BurnApplications>0&&d.SkillHealthRestored>0&&d.ErasedProjectiles>0;
                case "D29":return d.AuraHeals>0&&d.SkillHealthRestored>0&&Battle.Units.Any(u=>u.Active&&Battle.UnitArtsVulnerability(u)>1&&Battle.DefenderAuraMoveMultiplier(u)<1);
                case "D30":return Battle.CombatShots.Where(s=>s.SourceId==id&&s.Applied).GroupBy(s=>s.StartedAt).Any(g=>g.Select(s=>s.TargetId).Distinct().Count()>=2);
                default:return false;
            }
        }
        private IEnumerator VerifyBatch4Skill(string id,string directory)
        {
            Time.timeScale=0;
            if(directory!=null){ReturnToMonsterTests();defenderMenuOpen=batch4Tab=true;yield return TacticalFrame(directory,"six-star-selection");}
            bool entered=TryEnterDefenderSkillTrial(id);yield return null;yield return null;
            var d=Battle.FindDefender(id);var views=FindAnyObjectByType<DefenderRosterView>();var visual=views.FindVisual(id);
            bool models=views.Count==Battle.Defenders.Count&&visual.sprite.name.StartsWith(id);
            var idle=visual.sprite;Tick(.73f);yield return null;bool preparation=Battle.Clock==0&&visual.sprite!=idle;
            if(directory!=null)yield return TacticalFrame(directory,id+"-ready");
            StartBattle();Battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool deployed=ProcessPointerUp(gate).Success;
            double clock=Battle.Clock;float sp=d.SkillSP;Tick(10);yield return null;bool paused=Battle.Clock==clock&&d.SkillSP==sp;
            Battle.SetPaused(false);bool effect=false,assault=false,cast=false,captured=false;int extras=0;
            for(int i=0;i<16000&&Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.1f);
                if(extras<4&&i>10+extras*18)
                {
                    int slot=id=="D22"?3:2;
                    ProcessPointerDown(ScreenPosition(CardSlot(slot).center));ProcessPointerMove(gate);
                    if(ProcessPointerUp(gate).Success)extras++;
                }
                effect|=Batch4SkillObserved(id);
                if(effect)
                {
                    if(!captured&&directory!=null){yield return TacticalFrame(directory,id+"-skill");captured=true;}
                    if(!assault){ProcessPointerDown(ScreenPosition(CardSlot(0).center));ProcessPointerMove(gate);assault=ProcessPointerUp(gate).Success;}
                    var unit=Battle.Units.FirstOrDefault(u=>u.Active&&Battle.IsSkillReady(u.EntityId));
                    if(unit!=null){SelectSkillUnit(unit.EntityId);cast|=ActivateSelectedSkill()==SkillFailure.None;}
                }
                if(i%100==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null)yield return TacticalFrame(directory,id+"-result");
            Debug.Log($"BATCH4_DETAIL: {id}; casts={d.SkillCasts}; drones={d.DroneHits}; fears={d.FearApplications}; burns={d.BurnApplications}; erased={d.ErasedProjectiles}; heal={d.SkillHealthRestored}; aura={d.AuraHeals}; kills={d.LiberatorKills}; clock={Battle.Clock}; health={d.Health}.");
            RestartBattle();d=Battle.FindDefender(id);bool reset=d.SkillCasts==0&&d.SkillSP==d.Settings.Skill.InitialSP&&d.Health==d.MaxHealth&&Battle.Units.Count==0&&Battle.DefenderDrones.Count==0;
            ReturnToMonsterTests();yield return null;yield return null;bool restored=TestSelectionOpen&&!DefenderSkillTrialActive&&!TacticalMapActive;
            Debug.Log($"BATCH4_SKILL: id={id}; entered={entered}; models={models}; preparation={preparation}; deployed={deployed}; paused={paused}; effect={effect}; assault={assault}; cast={cast}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&models&&preparation&&deployed&&paused&&effect&&assault&&cast&&won&&reset&&restored?0:1);
        }
    }
}
