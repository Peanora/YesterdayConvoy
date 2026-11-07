using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [Serializable] sealed class ShopRosterResult
        {
            public bool passed;
            public string scope="Saved ReunionTrial, finite-stock fixture, production purchase and pointer handlers. Captures real Player rendering; no OS mouse or long-run balance claim.";
            public string[] checks;
            public float fourStarHealth,fourStarAttack;
        }
        internal IEnumerator VerifyShopRoster(string directory)
        {
            Directory.CreateDirectory(directory);
            var result=new ShopRosterResult();var checks=new List<string>();bool valid=true;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("ROSTER_PLAYER "+name+"="+ok);};
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
            yield return ReunionEvidenceReady();campaignConstructOpen=false;inspectedSynergy=null;
            shop.Preset(new[]{
                new ShopTrialModel.Piece{Id="CARD_M08",Star=3},new ShopTrialModel.Piece{Id="CARD_M08",Star=3},
                new ShopTrialModel.Piece{Id="CARD_M08",Star=2},new ShopTrialModel.Piece{Id="CARD_M08",Star=2},
                new ShopTrialModel.Piece{Id="CARD_M08",Star=1},new ShopTrialModel.Piece{Id="CARD_M08",Star=1},
                new ShopTrialModel.Piece{Id="CARD_M07",Star=1},new ShopTrialModel.Piece{Id="CARD_M09",Star=1},
                new ShopTrialModel.Piece{Id="CARD_M02",Star=1}},new[]{"CARD_M08"},8,0,100);
            shopLineup.Clear();shopLineup.AddRange(new[]{"CARD_M08","CARD_M07","CARD_M09"});
            seatedShop=null;ClearShopFlows();SyncShopDeck(false);SetShopVisible(true);
            yield return ReunionEvidenceReady();
            check("five-cost-capacities",shop.Roles.All(r=>shop.Capacity(r.Id)==new[]{0,30,25,18,10,9}[r.Cost]));
            int survivor=ShopLineupPiece("CARD_M08").Instance;
            check("purchase-preview-four-star",shop.PreviewPurchase(0).FinalStar==4);
            var purchase=PurchaseShopCard(0);
            check("real-three-step-merge-keeps-field-instance",purchase.Success&&purchase.FinalStar==4&&purchase.Instance==survivor&&shop.Bench.Count==4&&shop.Available("CARD_M08")==3);
            yield return new WaitForSecondsRealtime(3.2f);
            yield return ReunionEvidenceCapture(directory,"01-four-star-with-three-copies-left");
            Func<string,Rect> rect=id=>ShopPieceRect(shop.Bench.ToList().FindIndex(p=>p.Id==id));
            Action<string,Vector2> drag=(id,to)=>{ProcessShopPointer(EventType.MouseDown,rect(id).center);ProcessShopPointer(EventType.MouseDrag,to);ProcessShopPointer(EventType.MouseUp,to);};
            int gold=shop.Gold;string stock=string.Join(",",shop.Roles.Select(r=>shop.Available(r.Id)));
            drag("CARD_M08",ShopLineupRect(7).center);
            check("pointer-retains-interior-gap",rect("CARD_M08")==ShopLineupRect(7)&&rect("CARD_M07")==ShopLineupRect(1));
            drag("CARD_M07",ShopLineupRect(2).center);
            check("pointer-swaps-occupied-field-seats",rect("CARD_M07")==ShopLineupRect(2)&&rect("CARD_M09")==ShopLineupRect(1));
            drag("CARD_M02",ShopLineupRect(2).center);
            check("pointer-cross-row-swap",rect("CARD_M02")==ShopLineupRect(2)&&rect("CARD_M07")==ShopBenchRect(0));
            yield return ReunionEvidenceCapture(directory,"02-free-gaps-and-cross-row-swap");
            var before=rect("CARD_M07");
            ProcessShopPointer(EventType.MouseDown,before.center);ProcessShopPointer(EventType.MouseDrag,ShopBenchRect(8).center);
            ShopPointerEvent(EventType.MouseDown,Vector2.zero,1);ProcessShopPointer(EventType.MouseUp,ShopBenchRect(8).center);
            check("cancel-keeps-source-seat",rect("CARD_M07")==before);
            drag("CARD_M07",ShopBenchRect(8).center);
            check("pointer-reserve-empty-slot",rect("CARD_M07")==ShopBenchRect(8));
            check("moves-do-not-spend-or-change-stock",shop.Gold==gold&&string.Join(",",shop.Roles.Select(r=>shop.Available(r.Id)))==stock);
            yield return ReunionEvidenceCapture(directory,"03-reserve-position-persists");
            check("start-compacts-in-original-order",StartLoopNextBattle()&&shopLineup.SequenceEqual(new[]{"CARD_M09","CARD_M02","CARD_M08"}));
            yield return ReunionEvidenceReady();Battle.Begin();Battle.SetPaused(true);
            check("reserve-gap-survives-start",shopSeats.For(shop.Bench.Single(p=>p.Id=="CARD_M07").Instance)?.Index==8);
            check("field-compacted-to-zero-one-two",shopLineup.Select((id,i)=>shopSeats.For(ShopLineupPiece(id).Instance)?.Index==i).All(x=>x));
            check("four-star-profile-in-real-battle",Math.Abs(Battle.FindCard("CARD_M08").Settings.Health-2124)<.01f);
            check("four-star-pointer-deploys-while-paused",ReunionEvidenceDrag("CARD_M08"));
            var unit=Battle.Units.Last(u=>u.CardId=="CARD_M08");result.fourStarHealth=unit.MaxHealth;result.fourStarAttack=Battle.UnitCombat(unit).UnitAttack;
            check("normal-progression-health-attack-and-dp",unit.MaxHealth==2124&&Math.Abs(result.fourStarAttack-134.4f)<.01f&&Battle.DP==95&&Battle.Clock==0);
            shop.Validate();check("final-stock-conserved",shop.Roles.All(r=>shop.Available(r.Id)+shop.Offers.Count(id=>id==r.Id)+shop.Bench.Where(p=>p.Id==r.Id).Sum(p=>ShopTrialModel.Copies(p.Star))==shop.Capacity(r.Id)));
            yield return ReunionEvidenceCapture(directory,"04-compacted-roster-and-four-star-deployment");
            result.passed=valid;result.checks=checks.ToArray();File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Time.timeScale=1;
        }
    }
}
