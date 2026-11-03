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
        float shopCapturedGoldPeak;
        public void ProcessShopPointer(EventType type,Vector2 guiPoint)
        {
            ShopPointerEvent(type,guiPoint);
        }
        public IEnumerator VerifyShopMap()
        {
            Application.runInBackground=true;Time.timeScale=0;
            string[] args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-demoEvidenceDir");string dir=arg>=0&&arg+1<args.Length?args[arg+1]:null;
            yield return null;yield return null;
            bool layout=ShopTrialActive&&TacticalMapId=="city"&&ShopOfferRect(0).yMax<ShopBenchRect(0).y&&Camera.main.rect.height>.7f;
            float magnification=Mathf.Tan(shopPreviousFieldOfView*.5f*Mathf.Deg2Rad)/Mathf.Tan(Camera.main.fieldOfView*.5f*Mathf.Deg2Rad);
            bool framing=ShopMapPlayablePoints().All(p=>ShopBattlefieldRect.Contains(GUIPosition(Camera.main.WorldToScreenPoint(p))));
            bool flatBench=ShopBenchRect(0).height<80;
            yield return InteractionFrame(dir,"shop-map-ready");
            var viewport=Camera.main.rect;float fov=Camera.main.fieldOfView;var position=Camera.main.transform.position;
            int initialGold=Shop.Gold;var initialOffers=Shop.Offers.ToArray();
            SetShopVisible(true);bool covered=!ShopMapPointAvailable(ShopOfferRect(0).center);
            if(Array.IndexOf(args,"-shopPresentationEvidence")>=0)yield return CaptureShopPresentation(dir,"shop-opening",8);
            yield return new WaitForSecondsRealtime(.3f);
            yield return InteractionFrame(dir,"shop-map-shop-open");SetShopVisible(false);
            bool transitionSafe=!ShopMapPointAvailable(ShopOfferRect(0).center);
            if(Array.IndexOf(args,"-shopPresentationEvidence")>=0)yield return CaptureShopPresentation(dir,"shop-closing",8);
            yield return new WaitForSecondsRealtime(.22f);
            bool overlay=covered&&!ShopVisible&&ShopMapPointAvailable(ShopOfferRect(0).center)&&Camera.main.rect==viewport&&Camera.main.fieldOfView==fov&&Camera.main.transform.position==position&&Shop.Gold==initialGold&&Shop.Offers.SequenceEqual(initialOffers);
            EnterShopTrial(1);yield return null;yield return null;
            int gold=Shop.Gold;var denied=PurchaseShopCard(1);bool fullReject=!denied.Success&&Shop.Gold==gold&&Shop.Bench.Count==10;
            bool rankPreview=Shop.PreviewPurchase(0).FinalStar==2&&Shop.Gold==gold&&Shop.Bench.Count==10;
            yield return InteractionFrame(dir,"shop-map-silver-ready");
            if(Array.IndexOf(args,"-shopRankMotionEvidence")>=0)yield return CaptureShopRankMotion(dir,"shop-rank-silver");
            var two=PurchaseShopCard(0);bool fullTwo=two.Success&&two.FinalStar==2&&Shop.Bench.Count==9&&Shop.Gold==gold-1;
            bool arrivals=ShopArrivalCount>0;
            bool localEntry=ShopEntranceModel(two.Instance)!=null&&Mathf.Abs(ShopEntranceModel(two.Instance).transform.localPosition.x-ShopBenchModel(two.Instance).transform.localPosition.x)<.001f;
            yield return new WaitForSecondsRealtime(.9f);
            bool benchEffectsExpire=ShopBenchPromotionRemaining(two.Instance)>0;
            bool noNotice=string.IsNullOrEmpty(ShopFeedback);
            yield return InteractionFrame(dir,"shop-map-full-two-star");
            yield return new WaitForSecondsRealtime(1.1f);
            benchEffectsExpire&=ShopBenchPromotionRemaining(two.Instance)==0&&Shop.Bench.Single(p=>p.Instance==two.Instance).Star==2;
            yield return InteractionFrame(dir,"shop-map-two-star-settled");
            EnterShopTrial(2);yield return null;yield return null;
            gold=Shop.Gold;rankPreview&=Shop.PreviewPurchase(0).FinalStar==3&&Shop.Gold==gold;
            yield return InteractionFrame(dir,"shop-map-gold-ready");
            if(Array.IndexOf(args,"-shopRankMotionEvidence")>=0)yield return CaptureShopRankMotion(dir,"shop-rank-gold");
            var three=PurchaseShopCard(0);bool fullThree=three.Success&&three.FinalStar==3&&Shop.Bench.Count==7&&Shop.Available("CARD_M08")==0&&Shop.Gold==gold-1;
            arrivals&=ShopArrivalCount>0;
            localEntry&=ShopEntranceModel(three.Instance)!=null&&Mathf.Abs(ShopEntranceModel(three.Instance).transform.localPosition.x-ShopBenchModel(three.Instance).transform.localPosition.x)<.001f;
            bool presentationEvidence=Array.IndexOf(args,"-shopPresentationEvidence")>=0;
            if(presentationEvidence){yield return CaptureShopPresentation(dir,"shop-three-star-arrival",36);benchEffectsExpire&=shopCapturedGoldPeak>.5f;}
            else {yield return new WaitForSecondsRealtime(.9f);benchEffectsExpire&=ShopBenchPromotionRemaining(three.Instance)>0;}
            noNotice&=string.IsNullOrEmpty(ShopFeedback);
            if(!presentationEvidence)yield return InteractionFrame(dir,"shop-map-full-three-star");
            yield return new WaitForSecondsRealtime(1.1f);
            benchEffectsExpire&=ShopBenchPromotionRemaining(three.Instance)==0&&Shop.Bench.Single(p=>p.Instance==three.Instance).Star==3;
            yield return InteractionFrame(dir,"shop-map-three-star-settled");
            int instance=Shop.Bench.First(p=>p.Id=="CARD_M08").Instance;
            var origin=ShopBenchRect(0).center;
            ProcessShopPointer(EventType.MouseDown,origin);ProcessShopPointer(EventType.MouseDrag,ShopSellRect.center);ProcessShopPointer(EventType.MouseUp,origin);
            bool cancelSafe=Shop.Gold==gold-1&&Shop.Bench.Count==7;
            origin=new Vector2(ShopBenchRect(0).center.x,ShopBenchRect(0).yMax-2);
            Vector2 bottomSale=new Vector2(ShopSellRect.center.x,ShopSellRect.yMax-2);
            ProcessShopPointer(EventType.MouseDown,origin);ProcessShopPointer(EventType.MouseDrag,bottomSale);
            yield return InteractionFrame(dir,"shop-map-sell-preview");ProcessShopPointer(EventType.MouseUp,bottomSale);
            bool sale=Shop.Gold==gold+7&&Shop.Available("CARD_M08")==9&&!Shop.Bench.Any(p=>p.Instance==instance);
            EnterShopTrial();yield return null;yield return null;
            var offers=Shop.Offers.ToArray();var upgrade=Shop.Upgrade();SyncShopDeck();bool level=upgrade.Success&&Shop.Level==6&&Shop.XP==2&&offers.SequenceEqual(Shop.Offers);
            Shop.Locked=true;gold=Shop.Gold;bool locked=!Shop.Refresh().Success&&Shop.Gold==gold;Shop.Locked=false;bool refresh=Shop.Refresh().Success&&Shop.Gold==gold-2;
            Vector2 entry=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Entry));bool gateVisible=ShopBattlefieldRect.Contains(entry);
            origin=ShopBenchRect(0).center;ProcessShopPointer(EventType.MouseDown,origin);ProcessShopPointer(EventType.MouseDrag,entry);ProcessShopPointer(EventType.MouseUp,entry);
            bool deployed=Battle.Units.Count==1&&Battle.Units[0].CardId=="CARD_M08"&&Battle.Phase==BattlePhase.Running;
            gold=Shop.Gold;bool frozen=!PurchaseShopCard(0).Success&&Shop.Gold==gold;
            for(int i=0;i<120;i++)Tick(.05f);
            yield return InteractionFrame(dir,"shop-map-battle");
            bool simulation=Battle.Clock>0&&Battle.Units.Count==1&&Battle.Units[0].Motion.Position!=Battle.Entry;
            Shop.Validate();LeaveShopTrial();yield return null;yield return null;
            bool exit=TestSelectionOpen&&!ShopTrialActive&&!TacticalMapActive&&Camera.main.rect==new Rect(0,0,1,1);
            bool pass=layout&&framing&&overlay&&flatBench&&fullReject&&rankPreview&&noNotice&&benchEffectsExpire&&transitionSafe&&arrivals&&localEntry&&fullTwo&&fullThree&&cancelSafe&&sale&&level&&locked&&refresh&&gateVisible&&deployed&&frozen&&simulation&&exit;
            Debug.Log($"SHOP_MAP_CHECK: layout={layout}; framing={framing}; overlay={overlay}; flatBench={flatBench}; magnification={magnification:0.00}; fullReject={fullReject}; rankPreview={rankPreview}; noNotice={noNotice}; fullTwo={fullTwo}; fullThree={fullThree}; cancel={cancelSafe}; sale={sale}; level={level}; lock={locked}; refresh={refresh}; gateVisible={gateVisible}; deployed={deployed}; frozen={frozen}; simulation={simulation}; exit={exit}.");
            Debug.Log($"SHOP_BENCH_EFFECTS_EXPIRE: {benchEffectsExpire}");
            Debug.Log($"SHOP_PRESENTATION_CHECK: closingBlocksMap={transitionSafe}; purchaseArrivals={arrivals}; reserveCellEntry={localEntry}");
            Time.timeScale=1;Application.Quit(pass?0:1);
        }
        IEnumerator CaptureShopRankMotion(string directory,string prefix)
        {
            for(int i=0;i<16;i++){yield return new WaitForSecondsRealtime(.08f);yield return InteractionFrame(directory,prefix+"-"+i.ToString("00"));}
        }
        IEnumerator CaptureShopPresentation(string directory,string prefix,int count)
        {
            if(directory==null)yield break;
            Directory.CreateDirectory(directory);var frames=new List<Texture2D>();int peak=-1;shopCapturedGoldPeak=0;
            try
            {
                // Capture first; encode after the short animation so PNG compression cannot skip its middle frames.
                for(int i=0;i<count;i++)
                {
                    yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                    float remaining=ShopBenchPromotionRemaining(shopPromotedInstance),score=Mathf.Sin(Mathf.Clamp01((1-remaining)/.12f)*Mathf.PI*.5f)*remaining;
                    if(score>shopCapturedGoldPeak){shopCapturedGoldPeak=score;peak=i;}
                    yield return new WaitForSecondsRealtime(.03f);
                }
                for(int i=0;i<frames.Count;i++)File.WriteAllBytes(Path.Combine(directory,prefix+"-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());
                if(prefix=="shop-three-star-arrival"&&peak>=0)File.WriteAllBytes(Path.Combine(directory,"shop-map-full-three-star.png"),frames[peak].EncodeToPNG());
            }
            finally{foreach(var frame in frames)Destroy(frame);}
        }
    }
}
