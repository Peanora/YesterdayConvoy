using System;
using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Standalone player evidence uses normal input/deployment and the actual simulation.
        private IEnumerator VerifyMonsterCombat(string task, string directory)
        {
            Time.timeScale=0;
            bool entered=TryEnterMonsterTest("CARD_"+task);
            if(!entered) { Debug.LogError("MONSTER_COMBAT_CHECK: missing local media/configuration: "+task);Application.Quit(1);yield break; }
            ReturnToMonsterTests();yield return InteractionFrame(directory,task+"-selection");
            TryEnterMonsterTest("CARD_"+task);StartBattle();Battle.SetPaused(true);
            ProcessPointerDown(ScreenPosition(RosterSlot.center));
            Vector2 entry=Camera.main.WorldToScreenPoint(Battle.Entry);ProcessPointerMove(entry);
            bool paused=ProcessPointerUp(entry).Success;var unit=Battle.Units[0];Tick(2);
            paused &= Battle.Clock==0 && unit.Motion.DistanceTravelled==0;
            yield return InteractionFrame(directory,task+"-paused");
            Battle.SetPaused(false);Tick(.35f);yield return InteractionFrame(directory,task+"-walk");
            bool attacked=false,hit=false;
            for(int i=0;i<700 && unit.Active;i++)
            {
                Tick(.025f);yield return null;
                if(!attacked && !double.IsNegativeInfinity(unit.LastAttackTime))
                { attacked=true;Tick(.3f);yield return InteractionFrame(directory,task+"-attack"); }
                hit |= unit.Health<Battle.Settings.Health;
            }
            bool resolved=!unit.Active;
            if(unit.Resolution==UnitResolution.Dead) { Tick(.2f);yield return InteractionFrame(directory,task+"-death"); }
            RestartBattle();SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.1f);
            SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.1f);SelectCard(true);bool three=TryDeployAt(Battle.Entry).Success;
            yield return InteractionFrame(directory,task+"-group");
            for(int i=0;i<1600 && Battle.Phase==BattlePhase.Running;i++) { Tick(.025f);yield return null; }
            bool won=Battle.Phase==BattlePhase.Won && Battle.Breaches>0;
            yield return InteractionFrame(directory,task+"-result");
            bool noSkill=Battle.SkillCasts==0 && !Battle.CombatSettings.HasSkill;
            ReturnToMonsterTests();yield return null;
            bool clean=ViewCount==0 && Battle.Units.Count==0 && Battle.CombatShots.Count==0 && Battle.Clock==0;
            Debug.Log($"MONSTER_COMBAT_CHECK: {task}; entered={entered}; paused={paused}; attacked={attacked}; hit={hit}; resolved={resolved}; three={three}; won={won}; noSkill={noSkill}; clean={clean}.");
            Time.timeScale=1;Application.Quit(entered && paused && attacked && resolved && three && won && noSkill && clean ? 0 : 1);
        }
    }
}
