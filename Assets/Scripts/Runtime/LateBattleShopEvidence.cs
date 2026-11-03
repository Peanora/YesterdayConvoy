using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyLateBattleShop()
        {
            Time.timeScale=0;Application.runInBackground=true;StartLateBattleShopLoop(73);
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0?args[at+1]:null;
            var economy=shop;bool won=true,carry=true,paused=false,purchased=false,reset=true,viewport=true;
            string bought=null;yield return InteractionFrame(dir,"late-ready");
            for(int stage=1;stage<=2;stage++)
            {
                if(stage==2)
                {
                    carry&=OpenLoopShop();yield return new WaitForSecondsRealtime(.35f);
                    for(int roll=0;roll<10&&!purchased;roll++)
                    {
                        int slot=Enumerable.Range(0,5).Where(i=>shop.Offers[i]=="CARD_M15").DefaultIfEmpty(-1).First();
                        if(slot>=0){bought=shop.Offers[slot];purchased=PurchaseShopCard(slot).Success;}
                        else if(!shop.Refresh().Success)break;
                    }
                    yield return new WaitForSecondsRealtime(.85f);yield return InteractionFrame(dir,"late-shop");
                    carry&=StartLoopNextBattle();yield return new WaitForSecondsRealtime(.25f);
                }
                reset&=Battle.Clock==0&&Battle.Units.Count==0&&Battle.DP==24;carry&=ReferenceEquals(economy,shop);
                viewport&=ShopMapPlayablePoints().All(p=>CurrentShopBattlefieldRect.Contains(GUIPosition(Camera.main.WorldToScreenPoint(p))));
                var plan=JsonUtility.FromJson<StrongholdChallengePlan.Plan>(Resources.Load<TextAsset>("StrongholdChallengeSolution").text);
                int index=0;
                for(int tick=0;tick<2600&&Loop.CanDeploy;tick++)
                {
                    double clock=Battle.Phase==BattlePhase.Ready?0:Battle.Clock;
                    while(index<plan.orders.Length&&plan.orders[index].time<=clock+.001)
                    {
                        var order=plan.orders[index++];string card=stage==2&&bought!=null&&index==2?bought:order.card;
                        int bench=shop.Bench.ToList().FindIndex(p=>p.Id==card);
                        var gate=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Lanes[order.lane].Entry));
                        ProcessShopPointer(EventType.MouseDown,ShopPieceRect(bench).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
                        if(stage==1&&index==1){Battle.SetPaused(true);double pausedClock=Battle.Clock;Tick(10);paused=Battle.Clock==pausedClock;Battle.SetPaused(false);}
                    }
                    Tick(.1f);if(tick==600)yield return InteractionFrame(dir,"late-combat-"+stage);if(tick%100==0)yield return null;
                }
                won&=Battle.Phase==BattlePhase.Won;if(stage==2)carry&=Battle.Units.Any(u=>u.CardId==bought);
                yield return InteractionFrame(dir,stage==1?"late-settlement":"late-complete");
                int gold=shop.Gold;Tick(20);carry&=shop.Gold==gold;
                if(!won)break;
            }
            bool complete=Loop.Phase==BattleShopPhase.Complete;
            RestartBattle();bool restart=Loop.LateGame&&shop.Level==7&&shop.Gold==30&&shop.Bench.Count==5&&Battle.Units.Count==0;
            LeaveShopTrial();bool exit=TestSelectionOpen&&!ShopTrialActive;
            Debug.Log($"LATE_BATTLE_SHOP_CHECK: won={won}; complete={complete}; pause={paused}; purchased={purchased}; carry={carry}; reset={reset}; framing={viewport}; restart={restart}; exit={exit}");
            Time.timeScale=1;Application.Quit(won&&complete&&paused&&purchased&&carry&&reset&&viewport&&restart&&exit?0:1);
        }
    }
}
