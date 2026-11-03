using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyBattleShopLoop()
        {
            Time.timeScale=0;Application.runInBackground=true;StartBattleShopLoop(73);
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0?args[at+1]:null;
            var economy=shop;string starter=shop.Bench[0].Id,bought=null;
            bool won=true,paused=false,carry=true,purchase=false,reset=true;
            yield return InteractionFrame(dir,"loop-ready");
            for(int stage=1;stage<=2;stage++)
            {
                if(stage==2)
                {
                    carry&=OpenLoopShop();yield return new WaitForSecondsRealtime(.35f);
                    int slot=Enumerable.Range(0,5).First(i=>shop.Offers[i]!=null&&shop.Offers[i]!=starter);bought=shop.Offers[slot];purchase=PurchaseShopCard(slot).Success;
                    yield return new WaitForSecondsRealtime(.85f);yield return InteractionFrame(dir,"loop-shop");carry&=StartLoopNextBattle();
                    Debug.Log("LOOP_SHOP_CLOSING: "+ShopPanelTransitioning);
                    yield return new WaitForSecondsRealtime(.25f);
                }
                reset&=Battle.Clock==0&&Battle.Units.Count==0&&Battle.DP==12;carry&=ReferenceEquals(shop,economy);
                int sent=0;
                for(int tick=0;tick<5000&&Loop.CanDeploy;tick++)
                {
                    if(tick%30==0&&sent<6)
                    {
                        var gate=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Lanes[sent%2].Entry));int bench=sent%shop.Bench.Count;
                        int before=Battle.Units.Count;
                        ProcessShopPointer(EventType.MouseDown,ShopPieceRect(bench).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);if(Battle.Units.Count>before)sent++;
                        if(stage==1&&sent==1){Battle.SetPaused(true);double clock=Battle.Clock;Tick(10);paused=Battle.Clock==clock;Battle.SetPaused(false);}
                    }
                    Tick(.1f);if(tick==250)yield return InteractionFrame(dir,"loop-combat-"+stage);
                    if(tick%100==0)yield return null;
                }
                won&=Battle.Phase==BattlePhase.Won;
                if(stage==2)carry&=Battle.Units.Any(u=>u.CardId==bought);
                yield return InteractionFrame(dir,stage==1?"loop-settlement":"loop-complete");
                int gold=shop.Gold;Tick(20);carry&=shop.Gold==gold;
                if(!won)break;
            }
            bool complete=Loop.Phase==BattleShopPhase.Complete;
            RestartBattle();bool restart=shop.Gold==10&&shop.Level==1&&shop.Bench.Count==1&&Battle.Units.Count==0&&Loop.Stage==1;
            LeaveShopTrial();bool exit=TestSelectionOpen&&!ShopTrialActive&&Loop==null;
            Debug.Log($"BATTLE_SHOP_LOOP_CHECK: won={won}; complete={complete}; pause={paused}; purchased={purchase}; purchasedUnitInStage2={carry}; combatReset={reset}; restart={restart}; exit={exit}");
            Time.timeScale=1;Application.Quit(won&&complete&&paused&&purchase&&carry&&reset&&restart&&exit?0:1);
        }
    }
}
