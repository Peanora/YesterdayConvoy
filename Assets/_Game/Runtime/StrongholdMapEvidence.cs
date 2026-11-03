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
        public IEnumerator VerifyStrongholdMap()
        {
            Application.runInBackground=true;Time.timeScale=0;
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:null;
            yield return null;yield return null;
            bool layout=TacticalMapId==StrongholdMapLayout.Id&&Battle.Lanes.Count==2&&Battle.Defenders.Count==8&&FindAnyObjectByType<StrongholdMapEnvironment>()!=null;
            bool framed=ShopMapPlayablePoints().All(p=>ShopBattlefieldRect.Contains(GUIPosition(Camera.main.WorldToScreenPoint(p))));
            var fx=FindAnyObjectByType<StrongholdMapVfx>();
            bool surfaces=FindAnyObjectByType<StrongholdMapEnvironment>().NormalMappedSurfaceCount>=3;
            bool energy=fx!=null&&fx.VertexCount>0;
            yield return InteractionFrame(dir,"stronghold-ready");
            if(Array.IndexOf(args,"-strongholdEffectsEvidence")>=0)yield return CaptureStrongholdVfx(dir,"stronghold-source-fx",24,false);
            SetShopVisible(true);yield return new WaitForSecondsRealtime(.3f);yield return InteractionFrame(dir,"stronghold-shop");
            bool purchases=true;foreach(int i in new[]{1,2,3,4})purchases&=PurchaseShopCard(i).Success;
            SetShopVisible(false);yield return new WaitForSecondsRealtime(.9f);
            var origin=ShopBenchRect(0).center;Vector2 gate=GUIPosition(Camera.main.WorldToScreenPoint(Battle.Lanes[0].Entry));
            ProcessShopPointer(EventType.MouseDown,origin);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            bool drag=Battle.Units.Count==1&&Battle.Phase==BattlePhase.Running;
            Battle.SetPaused(true);double clock=Battle.Clock;Tick(2);bool paused=Battle.Clock==clock;Battle.SetPaused(false);
            fx.Present();double fxClock=fx.VisualClock;Battle.SetPaused(true);yield return new WaitForSecondsRealtime(.12f);
            bool fxPaused=fx.VisualClock==fxClock;Battle.SetPaused(false);
            bool infected=false,portal=false,combat=false;double nextFrame=6;long request=5000;
            bool portalFrame=false;
            for(int step=0;step<15000&&Battle.Phase==BattlePhase.Running;step++)
            {
                if(Battle.ActiveCount<9)
                    for(int index=0;index<shopDeck.Count;index++)
                    {
                        var card=shopDeck[index].Card.CardId;var lane=Battle.Lanes[(step/17+index)%2];
                        Battle.TryDeployRole(Battle.RunId,++request,lane.Entry,card);
                    }
                Tick(.1f);fx.Present();infected|=Battle.StrongholdInfectionTouches>0;portal|=Battle.StrongholdPortalTrips>0;combat|=Battle.CombatShots.Count>0;
                if(!portalFrame&&Battle.Units.Any(u=>Battle.StrongholdInTransit(u.EntityId)))
                {
                    portalFrame=true;yield return InteractionFrame(dir,"stronghold-portal-wait");
                    if(Array.IndexOf(args,"-strongholdEffectsEvidence")>=0)yield return CaptureStrongholdVfx(dir,"stronghold-portal-fx",28,true);
                }
                if(Battle.Clock>=nextFrame){yield return InteractionFrame(dir,"stronghold-combat-"+nextFrame.ToString("0"));nextFrame=nextFrame<18?nextFrame+6:99999;}
                if(step%100==0)yield return null;
            }
            bool won=Battle.Phase==BattlePhase.Won;
            bool portalFeedback=fx.PortalPulseCount>0;
            Debug.Log($"STRONGHOLD_SIMULATION: phase={Battle.Phase}; time={Battle.Clock}; units={Battle.Units.Count}; breaches={Battle.Breaches}; trips={Battle.StrongholdPortalTrips}; touches={Battle.StrongholdInfectionTouches}; damage={Battle.StrongholdInfectionDamage}");
            yield return InteractionFrame(dir,"stronghold-result");
            Tick(1.2f);fx.Present();bool effectsEnded=fx.TransientPulseCount==0;
            EnterShopTrial(2,StrongholdMapLayout.Id);yield return null;var merged=PurchaseShopCard(0);
            bool merge=merged.Success&&merged.FinalStar==3&&TacticalMapId==StrongholdMapLayout.Id;
            EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;
            bool reset=Battle.StrongholdPortalTrips==0&&Battle.StrongholdInfectionTouches==0&&Battle.Units.Count==0&&Battle.Defenders.All(d=>d.Health==d.MaxHealth);
            EnterShopTrial();yield return null;bool isolation=!Battle.StrongholdMapEnabled&&TacticalMapId=="city";
            EnterShopTrial(0,StrongholdMapLayout.Id);yield return null;LeaveShopTrial();yield return null;yield return null;
            bool exit=TestSelectionOpen&&!ShopTrialActive&&FindAnyObjectByType<StrongholdMapEnvironment>()==null;
            bool pass=layout&&framed&&purchases&&drag&&paused&&infected&&portal&&combat&&won&&merge&&reset&&isolation&&exit&&surfaces&&energy&&fxPaused&&portalFeedback&&effectsEnded;
            Debug.Log($"STRONGHOLD_MAP_CHECK: layout={layout}; framed={framed}; purchases={purchases}; drag={drag}; paused={paused}; infected={infected}; portal={portal}; combat={combat}; won={won}; merge={merge}; reset={reset}; isolation={isolation}; exit={exit}");
            Debug.Log($"STRONGHOLD_VISUAL_CHECK: texturedSurfaces={surfaces}; batchedEnergy={energy}; pause={fxPaused}; portalFeedback={portalFeedback}; effectsEnded={effectsEnded}");
            Time.timeScale=1;Application.Quit(pass?0:1);
        }
        IEnumerator CaptureStrongholdVfx(string dir,string prefix,int count,bool simulate)
        {
            if(dir==null)yield break;Directory.CreateDirectory(dir);var frames=new List<Texture2D>();
            try
            {
                for(int i=0;i<count;i++)
                {
                    if(simulate)Tick(.13f);yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                    yield return new WaitForSecondsRealtime(.04f);
                }
                for(int i=0;i<frames.Count;i++)File.WriteAllBytes(Path.Combine(dir,prefix+"-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());
            }
            finally{foreach(var frame in frames)if(frame!=null)Destroy(frame);}
        }
    }
}
