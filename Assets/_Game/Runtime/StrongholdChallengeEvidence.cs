using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyStrongholdChallenge()
        {
            Application.runInBackground=true;Time.timeScale=0;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0?args[at+1]:null;
            yield return null;yield return null;
            bool layout=Battle.StrongholdChallenge&&Battle.Defenders.Count==6&&Battle.Defenders.Count(d=>d.HighGround)==3;
            bool framed=ShopMapPlayablePoints().All(p=>ShopBattlefieldRect.Contains(GUIPosition(Camera.main.WorldToScreenPoint(p))));
            yield return InteractionFrame(dir,"challenge-ready");
            var d=Battle.Defenders[0];Vector2 click=GUIPosition(Camera.main.WorldToScreenPoint(d.Position+Camera.main.transform.up*.75f));
            ProcessShopPointer(EventType.MouseDown,click);ProcessShopPointer(EventType.MouseUp,click);
            bool inspector=DefenderInspectorVisible&&InspectedDefenderId==d.EntityId;yield return InteractionFrame(dir,"challenge-inspector");CloseDefenderInspector();
            var origin=ShopBenchRect(0).center;var gate=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Lanes[0].Entry));
            ProcessShopPointer(EventType.MouseDown,origin);ProcessShopPointer(EventType.MouseDrag,gate);Tick(0);yield return null;
            bool route=FindAnyObjectByType<StrongholdMapEnvironment>().RouteVisible;yield return InteractionFrame(dir,"challenge-route");CancelShopDrag();Tick(0);
            bool routeClear=!FindAnyObjectByType<StrongholdMapEnvironment>().RouteVisible;
            bool purchases=true;foreach(int slot in new[]{1,2,3,4})purchases&=PurchaseShopCard(slot).Success;
            yield return new WaitForSecondsRealtime(.9f);
            var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
            int index=0;bool deployed=true,paused=false,inspectorLive=false;double nextFrame=24;
            for(int step=0;step<2500&&(Battle.Phase==BattlePhase.Ready||Battle.Phase==BattlePhase.Running);step++)
            {
                while(index<plan.orders.Length&&plan.orders[index].time<=Battle.Clock+.001)
                {
                    var order=plan.orders[index++];int seat=shop.Bench.ToList().FindIndex(p=>p.Id==order.card);
                    int count=Battle.Units.Count;var from=ShopBenchRect(seat).center;var to=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Lanes[order.lane].Entry));
                    ProcessShopPointer(EventType.MouseDown,from);ProcessShopPointer(EventType.MouseDrag,to);ProcessShopPointer(EventType.MouseUp,to);
                    deployed&=Battle.Units.Count==count+1;
                }
                Tick(.1f);
                if(!paused&&Battle.Clock>20){Battle.SetPaused(true);double clock=Battle.Clock;Tick(3);paused=Battle.Clock==clock;Battle.SetPaused(false);}
                if(Battle.Clock>=nextFrame)
                {
                    var target=Battle.Defenders.FirstOrDefault(p=>p.Alive&&Battle.DefenderSkillActive(p));
                    if(target!=null){inspectedDefender=target.EntityId;inspectorLive|=DefenderInspectorVisible;}
                    yield return InteractionFrame(dir,"challenge-combat-"+nextFrame.ToString("0"));CloseDefenderInspector();nextFrame+=26;
                }
                if(step%80==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won&&Battle.Lanes.All(l=>Battle.StrongholdExitBroken(l.ExitId));
            Debug.Log($"CHALLENGE_PLAYER_RESULT: phase={Battle.Phase}; time={Battle.Clock}; units={Battle.Units.Count}; dp={Battle.DP}; breaches={Battle.Breaches}; portals={Battle.StrongholdPortalTrips}; skills="+string.Join(",",Battle.Defenders.Select(p=>p.EntityId+":"+p.SkillCasts)));
            yield return InteractionFrame(dir,"challenge-won");
            EnterShopTrial(0,StrongholdChallengeLayout.Id);yield return null;bool reset=Battle.Defenders.Count(p=>p.HighGround)==3&&Battle.Units.Count==0&&Battle.Defense==2;
            EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;bool legacy=Battle.Defenders.Count==8&&!Battle.StrongholdChallenge;
            Debug.Log($"STRONGHOLD_CHALLENGE_CHECK: layout={layout}; framed={framed}; inspector={inspector}; route={route}; routeClear={routeClear}; purchases={purchases}; dragDeploy={deployed}; pause={paused}; liveInspector={inspectorLive}; won={won}; reset={reset}; legacy={legacy}");
            Time.timeScale=1;Application.Quit(layout&&framed&&inspector&&route&&routeClear&&purchases&&deployed&&paused&&inspectorLive&&won&&reset&&legacy?0:1);
        }
    }
}
