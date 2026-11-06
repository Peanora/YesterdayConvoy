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
        [Serializable] sealed class ApprovedUiResult { public bool passed; public string[] checks; }
        IEnumerator ApprovedUiFrame(string folder,string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
        }
        // Explicit developer pointer fixture; it never runs during an ordinary challenge.
        public IEnumerator VerifyApprovedUi()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string folder=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/ui-v5-player");Directory.CreateDirectory(folder);
            var checks=new List<string>();bool passed=true;float oldScale=Time.timeScale;diagnostic=true;Application.runInBackground=true;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);passed&=ok;Debug.Log("APPROVED_UI_EVENT "+name+"="+ok);};
            try
            {
                yield return PrepareCampaign(true,73191);Time.timeScale=0;
                string talulah=talulahDefinition.Card.CardId;
                string[] ids={"CARD_M14","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M08","CARD_M05","CARD_M28"};
                var pieces=ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=id=="CARD_M14"?2:1}).ToList();
                pieces.AddRange(new[]{new ShopTrialModel.Piece{Id="CARD_M11",Star=1},new ShopTrialModel.Piece{Id="CARD_M20",Star=2}});
                shop.Preset(pieces,new[]{talulah,"CARD_M18","CARD_M13","CARD_M05","CARD_M11"},8,12,38);shopLineup.Clear();shopLineup.AddRange(ids);SyncShopDeck(false);SetShopVisible(true);
                yield return new WaitForSecondsRealtime(.8f);
                check("threeStaticFontFaces",new[]{"Regular","Semibold","Bold"}.All(face=>Resources.Load<Font>("UI/YesterdayConvoyUI-"+face)!=null));
                check("sameFiveOffers",shop.Offers.Count==5&&shop.Offers[4]=="CARD_M11");
                check("recruitListBlocksWorld",!ShopMapPointAvailable(ShopRecruitRowRect(4).center));
                check("fullScreenMap",Camera.main.rect==TacticalViewport.Current.CameraRect(new Rect(0,0,1280,720)));
                yield return ApprovedUiFrame(folder,"01-shop-and-cost-platforms");
                int gold=shop.Gold;ProcessShopPointer(EventType.MouseDown,ShopRecruitRowRect(4).center);ProcessShopPointer(EventType.MouseUp,ShopRecruitRowRect(4).center);
                check("sidebarSingleMerge",shop.Offers[4]==null&&shop.Bench.Count(p=>p.Id=="CARD_M11")==1&&shop.Bench.Single(p=>p.Id=="CARD_M11").Star==2&&shop.Gold==gold);
                yield return new WaitForSecondsRealtime(.30f);yield return ApprovedUiFrame(folder,"02-silver-merge-flow");
                yield return new WaitForSecondsRealtime(1.3f);
                var sold=shop.Bench.Single(p=>p.Id=="CARD_M20");int stock=shop.Available(sold.Id),slot=shop.Bench.ToList().FindIndex(p=>p.Instance==sold.Instance);
                var quote=shop.Quote(sold.Instance);ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,ShopSellRightRect.center);
                yield return ApprovedUiFrame(folder,"03-right-sale-quote");ProcessShopPointer(EventType.MouseUp,ShopSellRightRect.center);
                check("rightSellOnce",quote.Value==14&&!shop.Bench.Any(p=>p.Instance==sold.Instance)&&shop.Available(sold.Id)==stock+3);
                ProcessShopPointer(EventType.MouseUp,ShopSellRightRect.center);check("repeatReleaseNoRefund",shop.Available(sold.Id)==stock+3);
                var retained=shop.Bench.First(p=>p.Id=="CARD_M08");slot=shop.Bench.ToList().FindIndex(p=>p.Instance==retained.Instance);int count=shop.Bench.Count;
                ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,ShopSellRect.center);CancelShopDrag();ProcessShopPointer(EventType.MouseUp,ShopSellRect.center);
                check("cancelSaleKeepsCard",shop.Bench.Count==count&&shop.Bench.Any(p=>p.Instance==retained.Instance));
                check("nextBattle",StartLoopNextBattle());Battle.Begin();Battle.SetPaused(true);yield return null;yield return new WaitForSecondsRealtime(.3f);
                slot=shop.Bench.ToList().FindIndex(p=>p.Instance==retained.Instance);var entry=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
                check("entryNotCovered",ShopMapPointAvailable(entry));int dp=Battle.DP;
                ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,entry);ProcessShopPointer(EventType.MouseUp,entry);
                check("pausedDragDeployment",Battle.IsPaused&&Battle.Clock==0&&Battle.DP<dp&&Battle.Units.Any(u=>u.CardId==retained.Id));
                yield return ApprovedUiFrame(folder,"04-paused-battle-ui");
                File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new ApprovedUiResult{passed=passed,checks=checks.ToArray()},true));
                Debug.Log("APPROVED_UI_CHECK passed="+passed);
            }
            finally {Time.timeScale=oldScale;diagnostic=false;}
            yield return null;Application.Quit(passed?0:1);
        }
    }
    public sealed class ApprovedUiSmokeDriver : MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(started||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-approvedUiSmokeTest")<0)return;
            started=true;var go=new GameObject("Approved V5 UI verification");DontDestroyOnLoad(go);go.AddComponent<ApprovedUiSmokeDriver>();
        }
        IEnumerator Start()
        {
            ManualBattleController controller=null;
            for(int i=0;i<1200;i++){controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&!controller.ArtLoading)break;yield return null;}
            if(controller==null){Debug.LogError("APPROVED_UI_CHECK controller missing");Application.Quit(1);yield break;}
            yield return controller.VerifyApprovedUi();
        }
    }
}
