using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        IEnumerator VerifyTacticalRoster(string id,string directory)
        {
            Time.timeScale=0;bool entered=TryEnterTacticalMap(id);yield return null;yield return null;
            bool cards=RosterCount==4 && Battle.Cards.Count==4,drag=true,independent=true,paused=true;
            var guards=FindAnyObjectByType<DefenderRosterView>();
            bool facing=Battle.Defenders.Any(d=>guards.FacingBack(d.EntityId)) && Battle.Defenders.Any(d=>!guards.FacingBack(d.EntityId));
            var idleBefore=Battle.Defenders.Select(d=>guards.FindVisual(d.EntityId).sprite).ToArray();
            Tick(.73f);yield return null;
            bool preparation=Battle.Clock==0 && Battle.Defenders.Select((d,i)=>guards.FindVisual(d.EntityId).sprite!=idleBefore[i]).All(changed=>changed);
            if(directory!=null)yield return TacticalFrame(directory,id+"-v3-ready");
            // Inspect the reported upper/side spawn distortion in the shipped Player at every gate.
            for(int laneIndex=0;laneIndex<Battle.Lanes.Count;laneIndex++)
            {
                RestartBattle();StartBattle();Battle.SetPaused(true);SelectCard(true);
                var spawn=TryDeployAt(Battle.Lanes[laneIndex].Entry,true);drag &= spawn.Success;yield return null;
                if(directory!=null)yield return TacticalFrame(directory,id+"-proportions-gate-"+laneIndex);
            }
            RestartBattle();
            StartBattle();Battle.SetPaused(true);int expected=Battle.DP;int[] order={3,0,1,2};string mainId=null;
            for(int j=0;j<4;j++)
            {
                if(j==3 && Battle.Lanes.Count==3){Battle.SetPaused(false);Tick(1);Battle.SetPaused(true);}
                int i=order[j];var data=RosterDefinition(i);var lane=Battle.Lanes[j%Battle.Lanes.Count];Vector2 gate=Camera.main.WorldToScreenPoint(lane.Entry);
                ProcessPointerDown(ScreenPosition(CardSlot(i).center));ProcessPointerMove(gate);var receipt=ProcessPointerUp(gate);
                expected-=data.Snapshot().DeploymentCost;
                drag &= receipt.Success && !Dragging && Battle.DP==expected && Battle.FindUnit(receipt.EntityId)?.CardId==data.Card.CardId;
                independent &= Battle.FindCard(data.Card.CardId).RemainingUses==data.Snapshot().DeploymentLimit-1;
                if(i==0)mainId=receipt.EntityId;
            }
            double clock=Battle.Clock;Tick(20);paused=Battle.IsPaused && Battle.Clock==clock && Battle.Units.Count==4;
            bool noSkill=true;foreach(var u in Battle.Units.Where(u=>u.CardId!=Definition.Card.CardId)){noSkill &= u.SkillSP==0 && !Battle.IsSkillReady(u.EntityId) && Battle.CheckSkill(u.EntityId)==SkillFailure.NoSkill;OpenSkillMenu(u.EntityId);noSkill &= !SkillMenuOpen;}
            if(directory!=null)yield return TacticalFrame(directory,id+"-v3-paused-mixed");
            Battle.SetPaused(false);bool cast=false,slow=false,attacked=false,acid=false;double next=5;int nextCard=1;
            for(int i=0;i<6000 && Battle.Phase==BattlePhase.Running;i++)
            {
                if(Battle.Clock>=3 && Battle.ActiveCount<8)
                {
                    int index=nextCard++%3+1;var data=RosterDefinition(index);var lane=Battle.Lanes[(int)Battle.Clock%Battle.Lanes.Count];
                    if(Battle.CheckCardDeployment(data.Card.CardId,lane.Entry)==DeployFailure.None)
                    {Vector2 gate=Camera.main.WorldToScreenPoint(lane.Entry);ProcessPointerDown(ScreenPosition(CardSlot(index).center));ProcessPointerMove(gate);ProcessPointerUp(gate);}
                }
                if(!cast && Battle.Clock>=12 && Battle.IsSkillReady(mainId))
                {
                    ProcessPointerDown(ScreenPosition(UnitScreenRect(mainId).center));
                    if(SkillMenuOpen && SelectedEntityId==mainId)
                    {double before=Battle.Clock;Tick(1);slow=Math.Abs(Battle.Clock-before-.03)<.0001;ProcessPointerDown(ScreenPosition(SkillCastRect.center));cast=Battle.SkillCasts>0 && !SkillMenuOpen;}
                }
                Tick(.1f);attacked|=Battle.CombatShots.Any(s=>s.FromDefender && !s.Healing);acid|=Battle.Defenders.Any(d=>d.ArmorReduction>0);
                foreach(var u in Battle.Units.Where(u=>u.CardId!=Definition.Card.CardId))noSkill &= u.SkillSP==0 && u.SkillAmmo==0 && !Battle.IsSkillReady(u.EntityId);
                if(directory!=null && Battle.Clock>=next){yield return TacticalFrame(directory,id+"-v3-combat-"+next.ToString("0"));next=next<15?next+5:999;}
                if(i%100==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null)yield return TacticalFrame(directory,id+"-v3-result");
            RestartBattle();bool reset=Battle.Units.Count==0 && Battle.DP==24 && Battle.Cards.All(c=>c.RemainingUses==c.Settings.DeploymentLimit && Battle.CardCooldown(c.Settings.CardId)==0);
            ReturnToMonsterTests();yield return null;yield return null;bool restored=TestSelectionOpen && !TacticalMapActive && Battle.Cards.Count==0 && FindAnyObjectByType<TacticalMapEnvironment>()==null;
            Debug.Log($"V3_ROSTER_CHECK: id={id}; entered={entered}; cards={cards}; drag={drag}; independent={independent}; paused={paused}; facing={facing}; preparation={preparation}; noSkill={noSkill}; attacked={attacked}; acid={acid}; slow={slow}; cast={cast}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&cards&&drag&&independent&&paused&&facing&&preparation&&noSkill&&attacked&&acid&&slow&&cast&&won&&reset&&restored?0:1);
        }
    }
}
