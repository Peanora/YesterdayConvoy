using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [Serializable]sealed class MarineEvidenceResult
        {
            public bool passed,actualClick;public string[] checks;
            public string sourceTexture,outcome;public int tiles,bursts,differencePixels;
            public float firstSeed,firstSpread,firstBurst,bonusNeural,groundNeural;
            public string scope="Saved MarineTrial with its existing sandbox multipliers. Production deployment, costs, CD, attacks, retaliation, neural burst and terrain. No injected HP/damage/XP or forced victory. Coverage button may require an actual OS click. Rendering comparison freezes the simulation and changes only visibility of the new ground layer.";
        }
        [Serializable]sealed class MarineGuiStep {public int id;public string name;public float x,y,width,height;}

        IEnumerator MarineEvidenceCapture(string directory,string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}

        IEnumerator MarineEvidenceClick(string directory)
        {
            yield return new WaitForEndOfFrame();
            var point=TacticalViewport.Current.ToScreen(MarineCoverageToggleRect.center);
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new MarineGuiStep{id=1,name="view-marine-coverage",x=point.x,y=Screen.height-point.y,width=Screen.width,height=Screen.height},true));
            double deadline=Time.realtimeSinceStartupAsDouble+300;
            while(!MarineCoverageHighlighted)
            {if(Time.realtimeSinceStartupAsDouble>deadline)throw new InvalidOperationException("Actual coverage-button click timed out.");yield return null;}
        }

        public IEnumerator VerifyMarineTide(string directory)
        {
            var result=new MarineEvidenceResult{firstSeed=-1,firstSpread=-1,firstBurst=-1};var checks=new List<string>();
            bool valid=true,completed=false;float previousScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("MARINE_PLAYER "+name+"="+ok);};
            try
            {
                diagnostic=true;Application.runInBackground=true;
                Time.timeScale=0;while(ArtLoading||ShopPanelTransitioning)yield return null;
                check("saved-four-member-trial",activeSynergyTrial?.trialId=="marine"&&Battle.MarineTier==4&&Battle.MarineCells.Count==0);
                check("start-normal-battle",StartLoopNextBattle());Battle.Begin();Battle.SetPaused(false);
                var view=combatView.GetComponent<MarineTideView>();check("bound-ground-view",view!=null);
                string[] ids={"CARD_M35","CARD_M33","CARD_M34","CARD_M36"};long request=0;int wave=0;
                bool capturedSeed=false,capturedSpread=false,capturedBurst=false,verifiedVisual=false;
                for(int step=0;step<8000&&Battle.Phase==BattlePhase.Running;step++)
                {
                    if(step%40==0)
                    {
                        for(int i=0;i<ids.Length;i++)Battle.TryDeployRole(Battle.RunId,++request,Battle.Lanes[(wave+i)%Battle.Lanes.Count].Points[0],ids[i]);
                        wave++;
                    }
                    Tick(.05f);
                    if(step%10==0)yield return null;
                    if(!capturedSeed&&Battle.MarineCells.Count>0)
                    {result.firstSeed=(float)Battle.Clock;capturedSeed=true;yield return MarineEvidenceCapture(directory,"01-first-contact-seed");}
                    if(!capturedBurst&&Battle.NeuralBursts>0)
                    {result.firstBurst=(float)Battle.Clock;capturedBurst=true;yield return MarineEvidenceCapture(directory,"02-native-neural-burst");}
                    if(!capturedSpread&&Battle.MarineSpreadCount>0)
                    {result.firstSpread=(float)Battle.Clock;capturedSpread=true;yield return MarineEvidenceCapture(directory,"03-spreading-ground");}
                    if(!verifiedVisual&&capturedSpread&&Battle.Clock>result.firstSpread+.8f)
                    {
                        verifiedVisual=true;Battle.SetPaused(true);yield return null;view.Present();
                        check("original-texture-loaded",view.SourceTexture!=null&&view.SourceTexture.name=="Special_juntan_01");
                        result.sourceTexture=view.SourceTexture?.name;
                        check("visible-ground-cells",view.VisibleTileCount==Battle.MarineCells.Count&&view.VisibleTileCount>1);
                        double clock=Battle.Clock,remaining=Battle.MarineSpreadRemaining;int count=Battle.MarineCells.Count;
                        Tick(3);yield return null;view.Present();
                        check("pause-freezes-clock-and-growth",Battle.Clock==clock&&Battle.MarineSpreadRemaining==remaining&&Battle.MarineCells.Count==count);
                        result.actualClick=Environment.GetCommandLineArgs().Contains("-marineRequireClicks");
                        if(result.actualClick)yield return MarineEvidenceClick(directory);else check("coverage-toggle-operation",ToggleMarineCoverage());
                        yield return MarineEvidenceCapture(directory,"04-paused-coverage-view");
                        check("coverage-highlight-and-neural-hud",MarineCoverageHighlighted&&view.HighlightCoverage&&MarineNeuralHudCount>0);
                        var withGround=ScreenCapture.CaptureScreenshotAsTexture();view.PresentationVisible=false;view.Present();
                        yield return new WaitForEndOfFrame();var withoutGround=ScreenCapture.CaptureScreenshotAsTexture();
                        var a=withGround.GetPixels32();var b=withoutGround.GetPixels32();int changed=0;
                        for(int pixel=0;pixel<a.Length;pixel++)if(Math.Abs(a[pixel].r-b[pixel].r)+Math.Abs(a[pixel].g-b[pixel].g)+Math.Abs(a[pixel].b-b[pixel].b)>18)changed++;
                        result.differencePixels=changed;Destroy(withGround);Destroy(withoutGround);
                        check("actual-screen-ground-contribution",changed>150);view.PresentationVisible=true;view.Present();
                        ToggleMarineCoverage();Battle.SetPaused(false);
                    }
                }
                check("natural-seed-spread-and-burst",capturedSeed&&capturedSpread&&capturedBurst);
                check("native-neural-amplification",Battle.MarineBonusNeuralDealt>0);
                check("ground-cap-respected",Battle.MarineCells.Count<=16);
                result.tiles=Battle.MarineCells.Count;result.bursts=Battle.NeuralBursts;result.outcome=Battle.Phase.ToString();
                result.bonusNeural=Battle.MarineBonusNeuralDealt;result.groundNeural=Battle.MarineGroundNeuralDealt;
                check("naturally-resolved",Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost);
                yield return MarineEvidenceCapture(directory,"05-natural-outcome");
                ReplaySandbox();yield return null;view.Present();
                check("next-battle-clears-ground-and-highlight",Battle.MarineCells.Count==0&&view.VisibleTileCount==0&&!MarineCoverageHighlighted&&!view.HighlightCoverage);
                check("no-cross-battle-marine-xp",Loop.MarineExperience==0&&Loop.LastMarineGrowth==0);
                yield return MarineEvidenceCapture(directory,"06-next-battle-clean");completed=true;
            }
            finally
            {
                Time.timeScale=previousScale;result.passed=valid&&completed;result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("MARINE_TIDE_PLAYER_PASS="+result.passed);
            }
        }
    }

    public sealed class MarineTideSmokeDriver:MonoBehaviour
    {
        static bool installed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(installed||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-marineTideSmokeTest"))return;
            installed=true;var go=new GameObject("Marine Tide Player verification");DontDestroyOnLoad(go);go.AddComponent<MarineTideSmokeDriver>();
        }
        IEnumerator Verify(string directory)
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("MarineTrial");
            double deadline=Time.realtimeSinceStartupAsDouble+600;ManualBattleController controller=null;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||!controller.Loop.CanShop||controller.ArtLoading)throw new InvalidOperationException("MarineTrial startup timed out.");
            yield return controller.VerifyMarineTide(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/marine-tide-player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Verify(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            bool passed=File.ReadAllText(Path.Combine(directory,"result.json")).Contains("\"passed\": true");Application.Quit(passed?0:1);
        }
    }
}
