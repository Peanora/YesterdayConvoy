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
        IEnumerator SynergyActivationFrame(string directory,string name,Action<Texture2D> retain=null)
        {
            yield return new WaitForEndOfFrame();
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(screen==null)throw new InvalidOperationException("OnGUI screenshot unavailable.");
            File.WriteAllBytes(Path.Combine(directory,name+".png"),screen.EncodeToPNG());
            if(retain!=null)retain(screen);else Destroy(screen);
        }

        static int SynergyActivationChangedTopPixels(Texture2D peak,Texture2D clear,Rect card)
        {
            var viewport=new TacticalViewport(peak.width,peak.height);
            var topLeft=viewport.Offset+new Vector2(card.center.x-22,card.y-28)*viewport.Scale;
            // Sample above the character's possible idle silhouette, so idle frames cannot
            // masquerade as feedback on the unrelated card.
            var bottomRight=viewport.Offset+new Vector2(card.center.x+22,card.y-14)*viewport.Scale;
            int left=Mathf.Clamp(Mathf.FloorToInt(topLeft.x),0,peak.width-1),right=Mathf.Clamp(Mathf.CeilToInt(bottomRight.x),left+1,peak.width);
            int top=Mathf.Clamp(Mathf.FloorToInt(topLeft.y),0,peak.height-1),bottom=Mathf.Clamp(Mathf.CeilToInt(bottomRight.y),top+1,peak.height);
            int changed=0;
            for(int y=top;y<bottom;y++)for(int x=left;x<right;x++)
            {
                var a=peak.GetPixel(x,peak.height-y-1);var b=clear.GetPixel(x,clear.height-y-1);
                if(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)>.18f)changed++;
            }
            return changed;
        }

        static int[] SynergyActivationChangedFrameSides(Texture2D peak,Texture2D clear,Rect card)
        {
            // Each narrow strip lies on a real rectangular frame side, away from most idle art.
            // This is full OnGUI output, not a camera-only render of the board.
            var strips=new[]{
                new Rect(card.x+4,card.y-1,card.width-8,4),
                new Rect(card.xMax-3,card.y+8,4,card.height-16),
                new Rect(card.x+4,card.yMax-3,card.width-8,4),
                new Rect(card.x-1,card.y+8,4,card.height-16)};
            var viewport=new TacticalViewport(peak.width,peak.height);var counts=new int[strips.Length];
            for(int side=0;side<strips.Length;side++)
            {
                var r=strips[side];var a=viewport.Offset+r.min*viewport.Scale;var b=viewport.Offset+r.max*viewport.Scale;
                int left=Mathf.Clamp(Mathf.FloorToInt(a.x),0,peak.width-1),right=Mathf.Clamp(Mathf.CeilToInt(b.x),left+1,peak.width);
                int top=Mathf.Clamp(Mathf.FloorToInt(a.y),0,peak.height-1),bottom=Mathf.Clamp(Mathf.CeilToInt(b.y),top+1,peak.height);
                for(int y=top;y<bottom;y++)for(int x=left;x<right;x++)
                {
                    var p=peak.GetPixel(x,peak.height-y-1);var q=clear.GetPixel(x,clear.height-y-1);
                    if(Mathf.Abs(p.r-q.r)+Mathf.Abs(p.g-q.g)+Mathf.Abs(p.b-q.b)>.18f)counts[side]++;
                }
            }
            return counts;
        }

        IEnumerator VerifySynergyActivationQueue(string directory,Action<string,bool> check,string[] reunion,string unrelated)
        {
            const string newcomer="CARD_M16";
            yield return PrepareSynergyBadgeEvidenceRoster(reunion.Concat(new[]{unrelated}).ToArray());
            shopLineup.Remove(newcomer);SyncShopDeck(false);ClearSynergyActivationFeedback(false);
            inspectedSynergy=null;campaignConstructOpen=false;loopMenuOpen=false;
            int revision=synergyActivationFeedback.Revision,sounds=synergyActivationSoundPlayCount;
            int owned=shop.Bench.Count,gold=shop.Gold;
            check("queueUsesLegalSevenCardStockAtFiveReunionAndOneGuerrilla",owned==7&&shopLineup.Count==6&&Battle.ReunionTier==4&&Battle.GuerrillaTier==0);
            yield return SynergyActivationFrame(directory,"activation-v4-07-queue-before");
            int reserve=shop.Bench.ToList().FindIndex(piece=>piece.Id==newcomer);
            yield return SynergyBadgeGuiClick(directory,"queue-join-sixth-reunion-and-second-guerrilla",ShopPieceRect(reserve),()=>
                Battle.ReunionTier==6&&Battle.GuerrillaTier==2&&synergyActivationFeedback.Revision==revision+2);
            var first=synergyActivationFeedback.Active;
            if(first==null||first.Activations.Count!=1)throw new InvalidOperationException("Queue did not expose exactly one first pulse.");
            check("simultaneousCrossingsQueueGoldBeforeBronze",synergyActivationFeedback.PendingCount==2&&synergyActivationFeedback.QueuedCount==1&&
                first.Activations[0].Name=="整合运动"&&first.Activations[0].Tier==6&&
                SynergyBadgePresentation.GradeFor(first.Activations[0].Name,first.Activations[0].Tier)==SynergyVisualGrade.Gold);
            check("queuedGoldMembersAreExactlySixReunion",first.Activations[0].Members.Count==6&&reunion.All(first.Activations[0].ContainsMember)&&!first.Activations[0].ContainsMember(unrelated));
            check("queueFeedbackKeepsRealInventoryAndGold",shop.Bench.Count==owned&&shop.Gold==gold);
            Texture2D goldPeak=null,bronzePeak=null,gap=null,clear=null;
            try
            {
                Tick(Mathf.Max(0,.08f-synergyActivationFeedback.ActiveAge));
                yield return SynergyActivationFrame(directory,"activation-v4-08-rotation-early");
                Tick(Mathf.Max(0,.20f-synergyActivationFeedback.ActiveAge));
                yield return SynergyActivationFrame(directory,"activation-v4-09-rotation-middle");
                Tick(Mathf.Max(0,.40f-synergyActivationFeedback.ActiveAge));
                yield return SynergyActivationFrame(directory,"activation-v4-10-rotation-overshoot");
                Tick(Mathf.Max(0,.66f-synergyActivationFeedback.ActiveAge));
                yield return SynergyActivationFrame(directory,"activation-v4-11-rotation-settled",value=>goldPeak=value);
                check("goldRotationStagesKeepOnePulseAndOneOriginalCue",synergyActivationFeedback.Active!=null&&
                    synergyActivationFeedback.Active.Sequence==first.Sequence&&synergyActivationFeedback.Active.Activations.Count==1&&
                    synergyActivationSoundPlayCount==sounds+1&&synergyActivationSource!=null&&
                    synergyActivationSource.clip==Resources.Load<AudioClip>(SynergyActivationSoundResource));

                Tick(first.Duration-synergyActivationFeedback.ActiveAge+.01f);
                check("queueHasAVisibleSilentClearInterval",synergyActivationFeedback.Active==null&&synergyActivationFeedback.Pending&&
                    synergyActivationFeedback.SecondsToTransition>.20f&&synergyActivationFeedback.SecondsToTransition<=SynergyActivationFeedback.Gap&&
                    synergyActivationSoundPlayCount==sounds+1&&!synergyActivationSource.isPlaying);
                yield return SynergyActivationFrame(directory,"activation-v4-12-queue-clear-gap",value=>gap=value);
                float remainingGap=synergyActivationFeedback.SecondsToTransition;
                loopMenuOpen=true;Tick(.6f);yield return new WaitForSecondsRealtime(.12f);Tick(.6f);
                check("menuFreezesTheQueueInsideItsClearInterval",synergyActivationFeedback.Active==null&&synergyActivationFeedback.Pending&&
                    Mathf.Abs(synergyActivationFeedback.SecondsToTransition-remainingGap)<.0001f&&synergyActivationSoundPlayCount==sounds+1&&!synergyActivationSource.isPlaying);
                yield return SynergyActivationFrame(directory,"activation-v4-13-queue-gap-menu");
                yield return SynergyBadgeGuiClick(directory,"queue-close-menu-during-gap",new Rect(LoopMenuRect.xMax-36,LoopMenuRect.y+9,24,24),()=>!loopMenuOpen);
                Tick(remainingGap*.5f);
                check("halfTheRemainingGapDoesNotStartAnotherCue",synergyActivationFeedback.Active==null&&synergyActivationFeedback.Pending&&synergyActivationSoundPlayCount==sounds+1);
                Tick(synergyActivationFeedback.SecondsToTransition+.12f);
                var second=synergyActivationFeedback.Active;
                if(second==null||second.Activations.Count!=1)throw new InvalidOperationException("Queue did not expose exactly one second pulse.");
                var bronze=second.Activations[0];
                check("secondPulseHasIndependentSequenceAndOneBronzeCue",second.Sequence>first.Sequence&&bronze.Name=="游击队"&&bronze.Tier==2&&
                    SynergyBadgePresentation.GradeFor(bronze.Name,bronze.Tier)==SynergyVisualGrade.Bronze&&synergyActivationSoundPlayCount==sounds+2);
                check("onlyTwoRealGuerrillaCardsReceiveTheSecondPulse",bronze.Members.Count==2&&bronze.ContainsMember("CARD_M15")&&bronze.ContainsMember("CARD_M16")&&
                    reunion.Take(4).All(id=>!bronze.ContainsMember(id))&&!bronze.ContainsMember(unrelated));
                Tick(Mathf.Max(0,.66f-synergyActivationFeedback.ActiveAge));
                yield return SynergyActivationFrame(directory,"activation-v4-14-guerrilla-settled",value=>bronzePeak=value);
                Tick(second.Duration-synergyActivationFeedback.ActiveAge+.01f);
                check("bothQueuedPulsesAndAudioFinishWithoutExtraReplay",!synergyActivationFeedback.Pending&&synergyActivationFeedback.Active==null&&
                    synergyActivationSoundPlayCount==sounds+2&&!synergyActivationSource.isPlaying);
                yield return SynergyActivationFrame(directory,"activation-v4-15-queue-cleared",value=>clear=value);

                foreach(string id in reunion)
                {
                    var r=ApprovedUi.LineupSlot(shopLineup.IndexOf(id));
                    int top=SynergyActivationChangedTopPixels(goldPeak,clear,r);var sides=SynergyActivationChangedFrameSides(goldPeak,clear,r);
                    Debug.Log("SYNERGY_ACTIVATION_V4_GOLD_PIXELS "+id+" top="+top+" sides="+string.Join(",",sides));
                    check("goldOnGuiEmblemAndFourFrameSides_"+id,top>=8&&sides.All(pixels=>pixels>=4));
                    int bronzePixels=SynergyActivationChangedTopPixels(bronzePeak,clear,r);
                    if(bronze.ContainsMember(id))
                    {
                        var bronzeSides=SynergyActivationChangedFrameSides(bronzePeak,clear,r);
                        Debug.Log("SYNERGY_ACTIVATION_V4_BRONZE_PIXELS "+id+" top="+bronzePixels+" sides="+string.Join(",",bronzeSides));
                        check("bronzeOnGuiEmblemAndFourFrameSides_"+id,bronzePixels>=8&&bronzeSides.All(pixels=>pixels>=4));
                    }
                    else check("bronzeDoesNotReuseGoldMember_"+id,bronzePixels<=2);
                    check("clearIntervalHasNoLingeringEmblem_"+id,SynergyActivationChangedTopPixels(gap,clear,r)<=2);
                }
                var untouched=ApprovedUi.LineupSlot(shopLineup.IndexOf(unrelated));
                check("unrelatedMarineCardStaysUnlitAcrossBothPulses",SynergyActivationChangedTopPixels(goldPeak,clear,untouched)<=2&&
                    SynergyActivationChangedTopPixels(bronzePeak,clear,untouched)<=2);
            }
            finally
            {
                if(goldPeak!=null)Destroy(goldPeak);if(bronzePeak!=null)Destroy(bronzePeak);
                if(gap!=null)Destroy(gap);if(clear!=null)Destroy(clear);
            }
            shop.Validate();
        }

        internal IEnumerator VerifySynergyActivation(string directory,Action<string,bool> check)
        {
            diagnostic=true;Time.timeScale=0;Battle.SetPaused(false);
            string[] reunion=SynergyTrialCatalog.Find("reunion").Members.ToArray();
            const string newcomer="CARD_M10",unrelated="CARD_M33";
            yield return PrepareSynergyBadgeEvidenceRoster(reunion.Concat(new[]{unrelated}).ToArray());
            shopLineup.Remove(newcomer);SyncShopDeck(false);ClearSynergyActivationFeedback(false);
            inspectedSynergy=null;campaignConstructOpen=false;Battle.SetPaused(false);
            int owned=shop.Bench.Count,gold=shop.Gold,revision=synergyActivationFeedback.Revision,sounds=synergyActivationSoundPlayCount;
            check("legalSevenCardInventoryStartsAtFiveReunion",owned==7&&shopLineup.Count==6&&CurrentSynergies.Single(row=>row.Name=="整合运动").Count==5&&Battle.ReunionTier==4);
            yield return SynergyActivationFrame(directory,"activation-01-before");
            int reserve=shop.Bench.ToList().FindIndex(piece=>piece.Id==newcomer);
            yield return SynergyBadgeGuiClick(directory,"join-sixth-reunion-from-reserve",ShopPieceRect(reserve),()=>
                Battle.ReunionTier==6&&synergyActivationFeedback.Revision==revision+1);
            var batch=synergyActivationFeedback.Active;
            check("realClickCreatesOneGoldBatch",batch!=null&&batch.Activations.Count==1&&batch.Activations[0].Name=="整合运动"&&batch.Activations[0].Tier==6);
            check("onlySixCorrespondingKindsReceiveThePulse",batch!=null&&batch.Activations[0].Members.Count==6&&!batch.Activations[0].ContainsMember(unrelated));
            check("onlyNewlyJoinedKindIsMarkedAsAdded",batch!=null&&batch.AddedMembers.Count==1&&batch.IsAddedMember(newcomer));
            check("feedbackDoesNotSpendGoldOrChangeInventory",shop.Gold==gold&&shop.Bench.Count==owned);
            Tick(.12f);
            check("productionAudioPlaysOriginalResourceOnce",synergyActivationSource!=null&&synergyActivationSource.clip==Resources.Load<AudioClip>(SynergyActivationSoundResource)&&synergyActivationSoundPlayCount==sounds+1);
            Tick(.50f-synergyActivationFeedback.ActiveAge);
            Texture2D peak=null,clear=null;
            try
            {
                yield return SynergyActivationFrame(directory,"activation-02-peak",value=>peak=value);
                // Ready has no combat-pause command. A menu-open fixture exercises the actual
                // preparation blocker; an external real click closes that same native menu.
                loopMenuOpen=true;Tick(.6f);int sample=synergyActivationSource!=null?synergyActivationSource.timeSamples:0;
                float age=synergyActivationFeedback.ActiveAge;
                yield return new WaitForSecondsRealtime(.12f);Tick(.6f);
                check("preparationMenuFreezesProductionAgeAndAudioSamples",Mathf.Abs(age-.5f)<.0001f&&synergyActivationFeedback.ActiveAge==age&&
                    synergyActivationSource!=null&&!synergyActivationSource.isPlaying&&Math.Abs(synergyActivationSource.timeSamples-sample)<=1024);
                yield return SynergyActivationFrame(directory,"activation-03-paused");
                yield return SynergyBadgeGuiClick(directory,"close-preparation-menu",new Rect(LoopMenuRect.xMax-36,LoopMenuRect.y+9,24,24),()=>!loopMenuOpen);
                Tick(1.31f);
                check("pulseAndSoundClearAfterTheWindow",!synergyActivationFeedback.Pending&&synergyActivationFeedback.Active==null&&synergyActivationSource!=null&&!synergyActivationSource.isPlaying);
                yield return SynergyActivationFrame(directory,"activation-04-cleared",value=>clear=value);
                foreach(string id in reunion)
                {
                    int pixels=SynergyActivationChangedTopPixels(peak,clear,ApprovedUi.LineupSlot(shopLineup.IndexOf(id)));
                    Debug.Log("SYNERGY_ACTIVATION_PIXELS "+id+"="+pixels);
                    check("actualOnGuiTopEmblemVisible_"+id,pixels>=8);
                }
                int untouched=SynergyActivationChangedTopPixels(peak,clear,ApprovedUi.LineupSlot(shopLineup.IndexOf(unrelated)));
                Debug.Log("SYNERGY_ACTIVATION_PIXELS unrelated="+untouched);
                check("unrelatedCardHasNoOnGuiFeedback",untouched<=2);
            }
            finally{if(peak!=null)Destroy(peak);if(clear!=null)Destroy(clear);}
            SyncShopDeck(false);Tick(.1f);
            check("sameTierRebuildDoesNotReplay",synergyActivationFeedback.Revision==revision+1&&synergyActivationFeedback.Active==null&&synergyActivationSoundPlayCount==sounds+1);
            reserve=shop.Bench.ToList().FindIndex(piece=>piece.Id==newcomer);
            yield return SynergyBadgeGuiClick(directory,"remove-reunion-member",ShopPieceRect(reserve),()=>Battle.ReunionTier==4);
            check("downwardCrossingDoesNotPlay",synergyActivationSoundPlayCount==sounds+1&&synergyActivationFeedback.Active==null);
            reserve=shop.Bench.ToList().FindIndex(piece=>piece.Id==newcomer);
            yield return SynergyBadgeGuiClick(directory,"reach-six-reunion-again",ShopPieceRect(reserve),()=>
                Battle.ReunionTier==6&&synergyActivationFeedback.Revision==revision+2);
            Tick(.12f);
            check("regainingThresholdPlaysOneNewCue",synergyActivationSoundPlayCount==sounds+2&&synergyActivationFeedback.Active!=null);

            yield return VerifySynergyActivationQueue(directory,check,reunion,unrelated);

            var infectionMembers=SynergyTrialCatalog.Find("infection").Members.ToArray();
            yield return PrepareSynergyBadgeEvidenceRoster(infectionMembers);
            // The shared badge-layout fixture intentionally closes modals after setup. Reenter
            // the real threshold through ordinary lineup synchronization, which opens this prompt.
            string infectionNewcomer=infectionMembers[infectionMembers.Length-1];
            shopLineup.Remove(infectionNewcomer);SyncShopDeck(false);
            shopLineup.Add(infectionNewcomer);SyncShopDeck(false);
            check("realInfectionThresholdKeepsItsExistingPrompt",Battle.InfectionTier==6&&inspectedSynergy=="感染生物"&&synergyActivationFeedback.Active!=null);
            float blockedAge=synergyActivationFeedback.ActiveAge;int blockedSounds=synergyActivationSoundPlayCount;
            Tick(.7f);
            check("modalDefersThePulseAndCue",synergyActivationFeedback.ActiveAge==blockedAge&&synergyActivationSoundPlayCount==blockedSounds);
            yield return SynergyActivationFrame(directory,"activation-05-infection-prompt");
            yield return SynergyBadgeGuiClick(directory,"close-infection-prompt",InfectionCloseRect,()=>inspectedSynergy==null);
            Tick(.5f);
            check("closingThePromptReleasesTheOriginalCueOnce",synergyActivationFeedback.ActiveAge>=.5f&&synergyActivationSoundPlayCount==blockedSounds+1);
            yield return SynergyActivationFrame(directory,"activation-06-infection-visible");
            check("startingBattleStillUsesNormalRules",StartLoopNextBattle());Tick(.1f);
            check("battleTransitionClearsFeedbackAndSound",synergyActivationFeedback.Active==null&&synergyActivationSource!=null&&!synergyActivationSource.isPlaying);
            LeaveShopTrial();
            check("leavingClearsThePresentation",!synergyActivationFeedback.Pending&&synergyActivationFeedback.Active==null&&!synergyActivationSource.isPlaying);
        }
    }

    public sealed class SynergyActivationSmokeDriver:MonoBehaviour
    {
        public static bool Requested=>Debug.isDebugBuild&&Environment.GetCommandLineArgs().Contains("-synergyActivationSmokeTest");
        static bool started;
        [Serializable]sealed class Result{public bool passed;public int width,height;public string fixture="Legal inventory in real SynergyTrial; actual OS clicks acknowledged by existing GUI protocol; single and queued threshold crossings; full OnGUI rotating-stage and four-frame-side pixel evidence. No natural campaign/balance claim.";public string[] checks;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Requested)return;started=true;
            var go=new GameObject("Explicit synergy activation Player verification");DontDestroyOnLoad(go);go.AddComponent<SynergyActivationSmokeDriver>();
        }
        IEnumerator Run(string directory,Action<string,bool> check)
        {
            yield return SceneManager.LoadSceneAsync("SynergyTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||controller.ArtLoading||!controller.SynergySandbox)throw new InvalidOperationException("Activation Player startup timed out.");
            yield return controller.VerifySynergyActivation(directory,check);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/synergy-activation-player");Directory.CreateDirectory(directory);
            var checks=new List<string>();bool passed=true,complete=true;
            var result=new Result{width=Screen.width,height=Screen.height};float oldScale=Time.timeScale;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("SYNERGY_ACTIVATION_EVENT "+name+"="+value);};
            var stack=new Stack<IEnumerator>();stack.Push(Run(directory,check));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null)
                {complete=false;while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            Time.timeScale=oldScale;result.passed=passed&&complete;result.checks=checks.ToArray();
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("SYNERGY_ACTIVATION_PLAYER_CHECK passed="+result.passed);yield return null;Application.Quit(result.passed?0:1);
        }
    }
}
