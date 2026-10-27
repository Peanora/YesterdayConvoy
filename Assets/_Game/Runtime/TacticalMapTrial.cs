using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        private IEnumerator VerifyTacticalMap(string id,string directory)
        {
            Time.timeScale=0;
            bool entered=TryEnterTacticalMap(id);yield return null;yield return null;
            bool gates=Battle.Lanes.Count==(id=="city"?4:3),drag=true,paused=true,invalid=true;
            if(directory!=null)yield return TacticalFrame(directory,id+"-ready");
            foreach(var lane in Battle.Lanes)
            {
                RestartBattle();StartBattle();Battle.SetPaused(true);
                Vector2 gate=Camera.main.WorldToScreenPoint(lane.Entry);
                drag &= IsBattlefield(gate);
                ProcessPointerDown(ScreenPosition(RosterSlot.center));ProcessPointerMove(gate);
                var receipt=ProcessPointerUp(gate);
                drag &= receipt.Success && !Dragging && !Selected && Battle.Units.Count==1 && Battle.Units[0].RouteId==lane.Id && Battle.Units[0].ExitId==lane.ExitId;
                double clock=Battle.Clock;int dp=Battle.DP;Tick(5);
                paused &= Battle.IsPaused && Battle.Clock==clock && Battle.Units[0].Motion.DistanceTravelled==0;
                ProcessPointerDown(ScreenPosition(RosterSlot.center));ProcessPointerMove(ScreenPosition(new Vector2(640,400)));
                invalid &= !ProcessPointerUp(ScreenPosition(new Vector2(640,400))).Success && Battle.DP==dp;
            }
            RestartBattle();StartBattle();
            bool skill=false,attacked=false;double nextFrame=5;
            for(int i=0;i<6000 && Battle.Phase==BattlePhase.Running;i++)
            {
                if(Battle.CooldownRemaining<=0 && Battle.RemainingUses>0 && Battle.ActiveCount<4)
                {var lane=Battle.Lanes[(12-Battle.RemainingUses)%Battle.Lanes.Count];SelectCard(true);TryDeployAt(lane.Entry);}
                foreach(var unit in Battle.Units)
                    if(Battle.IsSkillReady(unit.EntityId)) {SelectSkillUnit(unit.EntityId);skill|=ActivateSelectedSkill()==SkillFailure.None;}
                Tick(.1f);attacked|=Battle.CombatShots.Count>0;
                if(directory!=null && Battle.Clock>=nextFrame)
                {yield return TacticalFrame(directory,id+"-combat-"+nextFrame.ToString("0"));nextFrame=nextFrame<10?nextFrame+5:999;}
                if(i%100==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null)yield return TacticalFrame(directory,id+"-result");
            RestartBattle();bool reset=Battle.Units.Count==0 && Battle.Defenders.Count==8;
            foreach(var d in Battle.Defenders)reset &= d.Health==d.MaxHealth;
            ReturnToMonsterTests();yield return null;yield return null;
            bool restored=TestSelectionOpen && !TacticalMapActive && GameObject.Find("Industrial Map")!=null && FindAnyObjectByType<TacticalMapEnvironment>()==null;
            Debug.Log($"TACTICAL_MAP_CHECK: id={id}; entered={entered}; gates={gates}; drag={drag}; paused={paused}; invalid={invalid}; attacked={attacked}; skill={skill}; won={won}; reset={reset}; restored={restored}.");
            Time.timeScale=1;Application.Quit(entered&&gates&&drag&&paused&&invalid&&attacked&&skill&&won&&reset&&restored?0:1);
        }
        private IEnumerator TacticalFrame(string directory,string name)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null){Debug.LogError("TACTICAL_CAPTURE_FAILED: a visible graphics window is required.");Time.timeScale=1;Application.Quit(1);yield break;}
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,name+".png"),frame.EncodeToPNG());Destroy(frame);
            yield return null;yield return null;
        }
    }
}
