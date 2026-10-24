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
            ReturnToMonsterTests();
            int selectionIndex=0;
            foreach(var option in monsterTests)
            {
                if(option.Id=="CARD_"+task) break;
                if(option.Playable) selectionIndex++;
            }
            playableScroll=new Vector2(0,selectionIndex/3*364);
            yield return InteractionFrame(directory,task+"-selection");
            TryEnterMonsterTest("CARD_"+task);StartBattle();Battle.SetPaused(true);
            ProcessPointerDown(ScreenPosition(RosterSlot.center));
            Vector2 entry=Camera.main.WorldToScreenPoint(Battle.Entry);ProcessPointerMove(entry);
            bool paused=ProcessPointerUp(entry).Success;var unit=Battle.Units[0];Tick(2);
            paused &= Battle.Clock==0 && unit.Motion.DistanceTravelled==0;
            yield return InteractionFrame(directory,task+"-paused");
            Battle.SetPaused(false);Tick(.35f);yield return InteractionFrame(directory,task+"-walk");
            bool snowFixture=Array.IndexOf(Environment.GetCommandLineArgs(),"-snowEffectFixture")>=0 && Battle.CombatSettings.Effects.FrozenAttackMultiplier>1;
            if(snowFixture) { Battle.TryApplyCold(Battle.Defender.EntityId,30);Battle.TryApplyCold(Battle.Defender.EntityId,30); }
            bool bonusCaptured=false,bonusValid=!snowFixture;
            bool attacked=false,hit=false,canAttack=Battle.CombatSettings.CanAttack;
            bool passiveMoving=false;
            for(int i=0;i<2400 && unit.Active;i++)
            {
                Tick(.025f);yield return null;
                if(!attacked && !double.IsNegativeInfinity(unit.LastAttackTime))
                { attacked=true;Tick(.3f);yield return InteractionFrame(directory,task+"-attack"); }
                if(snowFixture && !bonusCaptured)
                    foreach(var shot in Battle.CombatShots) if(!shot.FromDefender && shot.Applied)
                    {
                        bonusCaptured=true;bonusValid=shot.FrozenBonusApplied && Mathf.Abs(shot.DamageDealt-
                            Mathf.Max(1,shot.Attack*Battle.CombatSettings.Effects.FrozenAttackMultiplier-Battle.DefenderArmor(Battle.Defender)))<.01f;
                        yield return InteractionFrame(directory,task+"-frozen-bonus");break;
                    }
                hit |= unit.Health<Battle.Settings.Health;
                passiveMoving |= !canAttack && unit.IsMoving && !unit.HasCombatTarget && unit.Health<Battle.Settings.Health;
            }
            bool resolved=!unit.Active;
            if(unit.Resolution==UnitResolution.Dead) { Tick(.2f);yield return InteractionFrame(directory,task+"-death"); }
            if(Battle.HasPendingDeathBlast)
            {
                Tick(1.05f);yield return InteractionFrame(directory,task+"-blast");
            }
            bool frozen=true;
            if(task=="M06")
            {
                RestartBattle();SelectCard(true);var firstIce=TryDeployAt(Battle.Entry);Tick(1.1f);
                SelectCard(true);var secondIce=TryDeployAt(Battle.Entry);
                var one=Battle.FindUnit(firstIce.EntityId);var two=Battle.FindUnit(secondIce.EntityId);
                for(int i=0;i<1600 && (one.Active || two.Active);i++) { Tick(.025f);yield return null; }
                Tick(1.05f);frozen=Battle.Defender.FrozenUntil>Battle.Clock;
                yield return InteractionFrame(directory,task+"-frozen");
            }
            RestartBattle();SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.1f);
            SelectCard(true);TryDeployAt(Battle.Entry);Tick(1.1f);SelectCard(true);bool three=TryDeployAt(Battle.Entry).Success;
            yield return InteractionFrame(directory,task+"-group");
            bool groupAttackCaptured=false;
            for(int i=0;i<1600 && Battle.Phase==BattlePhase.Running;i++)
            {
                Tick(.025f);yield return null;
                if(!groupAttackCaptured)
                    foreach(var shot in Battle.CombatShots) if(!shot.FromDefender)
                    {
                        attacked=true;groupAttackCaptured=true;
                        Tick(Battle.CombatSettings.AttackWindup+.08f);
                        yield return InteractionFrame(directory,task+"-attack");break;
                    }
            }
            bool won=Battle.Phase==BattlePhase.Won && Battle.Breaches>0;
            yield return InteractionFrame(directory,task+"-result");
            bool noSkill=Battle.SkillCasts==0 && !Battle.CombatSettings.HasSkill;
            ReturnToMonsterTests();yield return null;
            bool clean=ViewCount==0 && Battle.Units.Count==0 && Battle.CombatShots.Count==0 && Battle.Clock==0;
            Debug.Log($"MONSTER_COMBAT_CHECK: {task}; entered={entered}; paused={paused}; attacked={attacked}; hit={hit}; resolved={resolved}; three={three}; won={won}; noSkill={noSkill}; clean={clean}; frozen={frozen}.");
            bool attackPolicy=canAttack ? attacked : !attacked && passiveMoving;
            Debug.Log($"MONSTER_ATTACK_POLICY_CHECK: {task}; canAttack={canAttack}; policy={attackPolicy}.");
            if(snowFixture) Debug.Log($"SNOW_FIXTURE_CHECK: {task}; explicitFrozenTarget=True; bonus={bonusValid}.");
            Time.timeScale=1;Application.Quit(entered && paused && attackPolicy && resolved && three && won && noSkill && clean && frozen && bonusValid ? 0 : 1);
        }
    }
}
