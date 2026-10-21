using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Exercises the same public input and battle path as the UI. No data or health cheats.
        private IEnumerator VerifySlug(string directory)
        {
            Time.timeScale=0;
            bool entered=TryEnterMonsterTest("CARD_ORIGINIUM_SLUG");
            if(!entered) { Debug.LogError("SLUG_CHECK: local approved media/configuration missing.");Application.Quit(1);yield break; }
            ReturnToMonsterTests();yield return InteractionFrame(directory,"slug-selection");
            TryEnterMonsterTest("CARD_ORIGINIUM_SLUG");StartBattle();Battle.SetPaused(true);
            Vector2 card=ScreenPosition(RosterSlot.center),entry=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(card);ProcessPointerMove(entry);
            bool pausedDeploy=ProcessPointerUp(entry).Success && Battle.IsPaused && Battle.DP==11 && Battle.RemainingUses==2;
            var unit=Battle.Units[0];Tick(2);bool frozen=Battle.Clock==0 && unit.Motion.DistanceTravelled==0;
            yield return InteractionFrame(directory,"slug-paused");
            Battle.SetPaused(false);
            for(int i=0;i<60;i++) { Tick(1f/60);yield return null; }
            bool walking=unit.Motion.DistanceTravelled>0;
            yield return InteractionFrame(directory,"slug-walk");
            OpenSkillMenu(unit.EntityId);bool noSkill=!SkillMenuOpen && Battle.CheckSkill(unit.EntityId)==SkillFailure.NoSkill && unit.SkillSP==0;
            bool attacked=false;
            for(int i=0;i<500 && unit.Active;i++)
            {
                Tick(.05f);yield return null;
                if(!attacked && !double.IsNegativeInfinity(unit.LastAttackTime))
                { attacked=true;Tick(.2f);yield return InteractionFrame(directory,"slug-attack"); }
            }
            bool died=unit.Resolution==UnitResolution.Dead && ViewCount==1;
            Tick(.2f);yield return InteractionFrame(directory,"slug-death");Tick(.35f);yield return null;
            bool removed=ViewCount==0;
            RestartBattle();bool reset=Battle.DP==12 && Battle.RemainingUses==3 && Battle.Units.Count==0;
            SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.05f);
            SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.05f);
            SelectCard(true);bool third=TryDeployAt(Battle.Entry).Success;
            yield return null;Tick(10);yield return InteractionFrame(directory,"slug-group");
            for(int i=0;i<900 && Battle.Phase==BattlePhase.Running;i++) { Tick(.05f);yield return null; }
            bool won=Battle.Phase==BattlePhase.Won && !Battle.Defender.Alive && Battle.Defense==0 && Battle.SkillCasts==0;
            yield return InteractionFrame(directory,"slug-victory");
            ReturnToMonsterTests();yield return null;
            bool clean=TestSelectionOpen && ViewCount==0 && Battle.Units.Count==0 && Battle.Clock==0;
            bool legacy=TryEnterMonsterTest(wisadelDefinition.Card.CardId) && Battle.CombatSettings.HasSkill;
            Debug.Log($"SLUG_CHECK: entered={entered}; pausedDeploy={pausedDeploy}; frozen={frozen}; walking={walking}; noSkill={noSkill}; attacked={attacked}; died={died}; removed={removed}; reset={reset}; third={third}; won={won}; clean={clean}; legacy={legacy}.");
            Time.timeScale=1;Application.Quit(entered && pausedDeploy && frozen && walking && noSkill && attacked && died && removed && reset && third && won && clean && legacy ? 0 : 1);
        }
    }
}
