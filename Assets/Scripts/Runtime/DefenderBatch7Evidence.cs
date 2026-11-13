using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool Batch7SkillObserved(string id)
        {
            var d=Battle.FindDefender(id);if(d==null||d.SkillCasts==0)return false;
            switch(id)
            {
                case "D32":return d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak&&Battle.ChenDawnWaves.Any(w=>w.SourceId==id&&w.Active)&&Battle.CombatShots.Any(s=>s.SourceId==id&&s.ChenDawnWaveSequence>0&&s.Applied&&s.DamageDealt>0)&&Battle.CombatShots.Any(s=>s.SourceId==id&&s.Empowered&&s.ChenDawnWaveSequence==0&&s.Applied&&s.DamageDealt>0);
                case "D33":return d.Ammo<31&&Battle.CombatShots.Any(s=>s.SourceId==id&&s.Empowered&&s.Applied&&s.DamageDealt>0);
                case "D34":return d.SeventhBursts>0&&d.Ammo<10&&Battle.SeventhDefenderEvents.Any(e=>e.SourceId==id&&e.Kind==SeventhDefenderEventKind.CorrosionBurst);
                case "D35":return d.Ammo<10&&Battle.SeventhDefenderEvents.Any(e=>e.SourceId==id&&e.Kind==SeventhDefenderEventKind.MedicalDetonation)&&Battle.CombatShots.Any(s=>s.SourceId==id&&s.TrueDamage&&s.Applied&&s.DamageDealt>0);
                default:return false;
            }
        }
        IEnumerator VerifyBatch7Skill(string id,string directory)
        {
            Time.timeScale=0;
            if(directory!=null){ReturnToMonsterTests();defenderMenuOpen=batch7Tab=true;yield return TacticalFrame(directory,"six-star-batch7-selection");}
            bool entered=TryEnterDefenderSkillTrial(id);yield return null;yield return null;
            var d=Battle.FindDefender(id);var views=FindAnyObjectByType<DefenderRosterView>();bool models=d!=null&&views!=null&&views.FindVisual(id)!=null&&views.Count==Battle.Defenders.Count;
            if(directory!=null)yield return TacticalFrame(directory,id+"-ready");
            StartBattle();Battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool deployed=ProcessPointerUp(gate).Success;
            double clock=Battle.Clock;float sp=d.SkillSP;Tick(5);yield return null;bool paused=Battle.Clock==clock&&d.SkillSP==sp;
            Battle.SetPaused(false);bool effect=false,captured=false;int extras=0;
            for(int i=0;i<1800&&Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.1f);
                if(extras<5&&i>20+extras*20){ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);if(ProcessPointerUp(gate).Success)extras++;}
                effect|=Batch7SkillObserved(id);
                if(effect&&!captured&&directory!=null){yield return TacticalFrame(directory,id+"-skill");captured=true;}
                if(effect&&i>600)break;
                if(i%100==0)yield return null;
            }
            int casts=d.SkillCasts;RestartBattle();d=Battle.FindDefender(id);
            bool reset=d.SkillCasts==0&&d.SkillSP==d.Settings.Skill.InitialSP&&d.Health==d.MaxHealth&&Battle.Units.Count==0&&Battle.SeventhDefenderEvents.Count==0;
            ReturnToMonsterTests();yield return null;yield return null;bool restored=TestSelectionOpen&&!DefenderSkillTrialActive&&!TacticalMapActive;
            Debug.Log($"BATCH7_SKILL: id={id}; entered={entered}; models={models}; deployed={deployed}; paused={paused}; casts={casts}; effect={effect}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&models&&deployed&&paused&&effect&&reset&&restored?0:1);
        }
    }
}
