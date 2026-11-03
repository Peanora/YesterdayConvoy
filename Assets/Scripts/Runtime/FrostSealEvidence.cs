using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Explicit development-Player verification; never runs in an ordinary play session.
        public IEnumerator VerifyFrostSeal()
        {
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
            var args=System.Environment.GetCommandLineArgs();int dir=System.Array.IndexOf(args,"-demoEvidenceDir");
            string directory=dir>=0&&dir+1<args.Length?args[dir+1]:null;
            int seed=0;
            for(int candidate=1;candidate<4000&&seed==0;candidate++)
            {
                var probe=new ShopTrialModel(shop.Roles,candidate);probe.Preset(shop.Bench,System.Array.Empty<string>(),7,0,30);
                probe.GuaranteedRefreshOffer=talulahDefinition.Card.CardId;probe.Refresh(true);
                if(probe.Offers.Contains("CARD_M14"))seed=candidate;
            }
            bool valid=seed>0;if(!valid){Debug.LogError("FROST_SEAL_PLAYER: no seeded normal shop offer");Application.Quit(1);yield break;}
            StartLateBattleShopLoop(seed);loopTestSettlementOpen=true;valid&=TryTestSettlement(true)&&OpenLoopShop();
            yield return InteractionFrame(directory,"frost-shop");
            int slot=shop.Offers.ToList().IndexOf("CARD_M14"),gold=shop.Gold;
            valid&=slot>0&&PurchaseShopCard(slot).Success&&shop.Gold==gold-5;
            valid&=StartLoopNextBattle();yield return new WaitForSecondsRealtime(.4f);
            int bench=shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M14");var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Lanes[0].Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(bench).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            var unit=Battle.Units.FirstOrDefault();valid&=unit!=null&&unit.CardId=="CARD_M14";
            if(unit==null){Debug.LogError("FROST_SEAL_PLAYER: drag failed");Application.Quit(1);yield break;}
            Tick(12.05f);SelectSkillUnit(unit.EntityId);valid&=Battle.IsSkillReady(unit.EntityId)&&ActivateSelectedSkill()==SkillFailure.None;
            var position=unit.Motion.Position;Tick(.5f);yield return InteractionFrame(directory,"frost-windup");
            Tick(.7f);yield return InteractionFrame(directory,"frost-burst");
            valid&=Battle.FrostSeals.Count==1&&Battle.FrostSeals[0].Resolved&&unit.Motion.Position==position;
            var effect=GetComponent<FrostSealView>();valid&=effect.VisibleEffects>0;
            Battle.SetPaused(true);double clock=Battle.Clock;Tick(10);yield return InteractionFrame(directory,"frost-paused");
            valid&=Battle.Clock==clock&&effect.VisibleEffects>0&&unit.Motion.Position==position;
            Battle.SetPaused(false);Tick(2.05f);valid&=!Battle.IsSkillActionPlaying(unit)&&unit.SkillSP>0;
            Tick(3.3f);yield return InteractionFrame(directory,"frost-clear");valid&=effect.VisibleEffects==0;
            RestartBattle();yield return null;valid&=Battle.FrostSeals.Count==0&&effect.VisibleEffects==0&&!shop.Bench.Any(p=>p.Id=="CARD_M14");
            Debug.Log("FROST_SEAL_PLAYER: seededShop="+seed+"; purchase/drag/manualCast/pause/actionLock/tail/restart="+valid);
            Application.Quit(valid?0:1);
        }
    }
}
