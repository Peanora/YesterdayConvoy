using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Explicit inventory fixtures only. No battle tier, damage, stock or order field is edited.
        static readonly string[] SynergyBadgeGuerrillaRoster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        static readonly string[] SynergyBadgeNineTagRoster={"CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14","CARD_M03","CARD_M26","CARD_M21","CARD_M29","CARD_M33"};
        [Serializable]sealed class SynergyBadgeGuiStep {public int id;public string name,action;public float x,y,width,height,scrollY;}
        int synergyBadgeGuiSequence;

        IEnumerator PrepareSynergyBadgeEvidenceRoster(string[] ids)
        {
            if(!SynergySandbox||!Loop.CanShop)throw new InvalidOperationException("Badge fixture requires sandbox preparation.");
            var cards=ids.Select(id=>shopDefinitions[id].Card).ToArray();
            yield return Prepare(Plan(shopMapCopy.defenders,cards,true),()=>
            {
                shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),Math.Max(8,ids.Length),0,100);
                shopLineup.Clear();shopLineup.AddRange(ids);SyncShopDeck(false);SetShopVisible(true);
                inspectedSynergy=null;campaignConstructOpen=false;campaignSynergyScroll=Vector2.zero;
            },"准备羁绊图标验收夹具");
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Badge preparation timed out.");
            shop.Validate();
        }

        Rect SynergyBadgeEvidenceRow(string name)
        {
            int index=Array.FindIndex(CurrentSynergies,row=>row.Name==name);
            if(index<0)throw new InvalidOperationException("Expected synergy is absent from real sidebar: "+name);
            return SynergyRowScreenRect(index);
        }

        static int SynergyBadgePlateScreenHues(Texture2D screen,Rect diamond,Rect glyph,Rect counter,
            out int upperSamples,out float upperMeanGreenMinusRed,out float upperMeanBlueMinusRed)
        {
            var viewport=new TacticalViewport(screen.width,screen.height);
            int left=Mathf.Clamp(Mathf.CeilToInt(viewport.Offset.x+diamond.x*viewport.Scale),0,screen.width-1);
            int right=Mathf.Clamp(Mathf.FloorToInt(viewport.Offset.x+diamond.xMax*viewport.Scale),left+1,screen.width);
            int bottom=Mathf.Clamp(Mathf.CeilToInt(screen.height-viewport.Offset.y-diamond.yMax*viewport.Scale),0,screen.height-1);
            int top=Mathf.Clamp(Mathf.FloorToInt(screen.height-viewport.Offset.y-diamond.y*viewport.Scale),bottom+1,screen.height);
            var hues=new HashSet<int>();
            upperSamples=0;float upperGreenSum=0,upperBlueSum=0;
            for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)
            {
                var point=viewport.FromTopLeft(new Vector2(x+.5f,screen.height-y-.5f));
                float radius=Mathf.Abs(point.x-diamond.center.x)/(diamond.width*.5f)+Mathf.Abs(point.y-diamond.center.y)/(diamond.height*.5f);
                if(radius>.88f||glyph.Contains(point)||counter.Contains(point))continue;
                Color pixel=screen.GetPixel(x,y);
                float vertical=(point.y-diamond.yMin)/diamond.height;
                // The approved V2 is mint at the top. The previous pink plate was also
                // multicolored, so hue diversity alone cannot establish the correct asset.
                if(vertical>=.06f&&vertical<=.18f)
                {
                    upperSamples++;upperGreenSum+=pixel.g-pixel.r;upperBlueSum+=pixel.b-pixel.r;
                }
                Color.RGBToHSV(pixel,out float h,out float saturation,out float value);
                // Sample the colored plate, not the white glyph, count text, shadow or scene.
                if(saturation>.14f&&value>.38f)hues.Add(Mathf.Min(11,Mathf.FloorToInt(h*12)));
            }
            upperMeanGreenMinusRed=upperSamples>0?upperGreenSum/upperSamples:0;
            upperMeanBlueMinusRed=upperSamples>0?upperBlueSum/upperSamples:0;
            return hues.Count;
        }

        static int SynergyBadgeWhiteGlyphPixels(Texture2D screen,Rect glyph,Texture2D source)
        {
            var old=RenderTexture.active;var target=RenderTexture.GetTemporary(64,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var mask=new Texture2D(64,64,TextureFormat.RGBA32,false,true);int neutral=0;
            try
            {
                Graphics.Blit(source,target);RenderTexture.active=target;mask.ReadPixels(new Rect(0,0,64,64),0,0);mask.Apply();
                var viewport=new TacticalViewport(screen.width,screen.height);
                int left=Mathf.Clamp(Mathf.CeilToInt(viewport.Offset.x+glyph.x*viewport.Scale),0,screen.width-1);
                int right=Mathf.Clamp(Mathf.FloorToInt(viewport.Offset.x+glyph.xMax*viewport.Scale),left+1,screen.width);
                int bottom=Mathf.Clamp(Mathf.CeilToInt(screen.height-viewport.Offset.y-glyph.yMax*viewport.Scale),0,screen.height-1);
                int top=Mathf.Clamp(Mathf.FloorToInt(screen.height-viewport.Offset.y-glyph.y*viewport.Scale),bottom+1,screen.height);
                for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)
                {
                    var point=viewport.FromTopLeft(new Vector2(x+.5f,screen.height-y-.5f));
                    var texel=mask.GetPixelBilinear((point.x-glyph.x)/glyph.width,1-(point.y-glyph.y)/glyph.height);
                    if(texel.a<.94f||texel.maxColorComponent<.7f)continue;
                    Color.RGBToHSV(screen.GetPixel(x,y),out _,out float saturation,out float value);
                    if(saturation<.12f&&value>.78f)neutral++;
                }
            }
            finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);Destroy(mask);}
            return neutral;
        }

        IEnumerator SynergyBadgeEvidenceFrame(string directory,string file,Action<string,bool> check,string prismaticName=null)
        {
            yield return new WaitForEndOfFrame();
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(screen==null)throw new InvalidOperationException("Full Player screenshot was unavailable.");
            try
            {
                File.WriteAllBytes(Path.Combine(directory,file+".png"),screen.EncodeToPNG());
                check(file+"FullScreen",screen.width==Screen.width&&screen.height==Screen.height&&screen.width>=1280&&screen.height>=720);
                if(prismaticName!=null)
                {
                    var row=SynergyBadgeEvidenceRow(prismaticName);
                    int hues=SynergyBadgePlateScreenHues(screen,SynergyBadgeRect(row),SynergyGlyphRect(row),SynergyCountRect(row),
                        out int upperSamples,out float upperGreenMinusRed,out float upperBlueMinusRed);
                    int whites=SynergyBadgeWhiteGlyphPixels(screen,SynergyGlyphRect(row),SynergyBadgePresentation.IconFor(prismaticName));
                    check(file+"ActualPlateHasAtLeastThreeHueFamilies",hues>=3);
                    check(file+"ActualUpperPlateUsesMintV2",upperSamples>=8&&upperGreenMinusRed>.08f&&upperBlueMinusRed>.03f);
                    check(file+"OriginalGlyphRemainsVisibleWhite",whites>=2);
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "SYNERGY_BADGE_SCREEN_PLATE_HUES {0}={1}; whiteGlyphPixels={2}; upperSamples={3}; meanGMinusR={4:F4}; meanBMinusR={5:F4}",
                        file,hues,whites,upperSamples,upperGreenMinusRed,upperBlueMinusRed));
                }
            }
            finally {Destroy(screen);}
            yield return null;
        }

        IEnumerator SynergyBadgeGuiClick(string directory,string name,Rect rect,Func<bool> accepted,string action="click",float scrollY=0)
        {
            int id=++synergyBadgeGuiSequence;Vector2 point=TacticalViewport.Current.ToScreen(rect.center);
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new SynergyBadgeGuiStep
                {id=id,name=name,action=action,scrollY=scrollY,x=point.x,y=Screen.height-point.y,width=Screen.width,height=Screen.height},true));
            // An external operator clicks the actual IMGUI control, then acknowledges this id.
            // This driver never assigns the modal or scroll position, or dispatches a GUI event.
            double deadline=Time.realtimeSinceStartupAsDouble+300;
            for(;;)
            {
                string ack=Path.Combine(directory,"gui-ack.txt");
                bool acknowledged=false;
                try {acknowledged=File.Exists(ack)&&File.ReadAllText(ack).Trim()==id.ToString();}
                catch(IOException)
                {
                    // An external writer may briefly hold or atomically replace this file.
                    // Retry next frame; the real GUI state and original deadline still apply.
                }
                if(acknowledged&&accepted())break;
                if(Time.realtimeSinceStartupAsDouble>deadline)throw new InvalidOperationException("Actual badge UI click timed out: "+name);
                yield return null;
            }
            yield return null;yield return null;
        }

        internal IEnumerator VerifySynergyBadgeCampaign(string directory,Action<string,bool> check)
        {
            diagnostic=true;yield return PrepareCampaign(false,81271);Time.timeScale=0;
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            check("realCampaignSceneAndStartingInventory",SceneManager.GetActiveScene().name=="Campaign"&&CampaignActive&&!SynergySandbox&&shopLineup.Count==1);
            check("unactivatedStarterUsesInactiveGrade",CurrentSynergies.Length>0&&CurrentSynergies.All(row=>row.Tier==0&&SynergyBadgePresentation.GradeFor(row.Name,row.Tier)==SynergyVisualGrade.Inactive));
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-01-campaign-inactive",check);
        }

        internal IEnumerator VerifySynergyBadgeSandbox(string directory,Action<string,bool> check)
        {
            diagnostic=true;yield return PrepareCampaign(true,81272);Time.timeScale=0;
            check("realSandboxScene",SceneManager.GetActiveScene().name=="SynergyTrial"&&SynergySandbox);
            var icons=SynergyTrialCatalog.Entries.Select(entry=>SynergyBadgePresentation.IconFor(entry.Tag)).ToArray();
            check("nineDistinctRealResourcesLoad",icons.All(icon=>icon!=null&&icon.width==256&&icon.height==256)&&icons.Distinct().Count()==9);
            foreach(int count in new[]{2,4,6,8})
            {
                yield return PrepareSynergyBadgeEvidenceRoster(SynergyTrialCatalog.Find("reunion").Members.Take(count).ToArray());
                var row=CurrentSynergies.Single(summary=>summary.Name=="整合运动");
                var expected=count==2?SynergyVisualGrade.Bronze:count==4?SynergyVisualGrade.Silver:count==6?SynergyVisualGrade.Gold:SynergyVisualGrade.Prismatic;
                check("real"+count+"KindRosterUses"+expected,row.Count==count&&row.Tier==count&&SynergyBadgePresentation.GradeFor(row.Name,row.Tier)==expected);
                check("stockConservedAt"+count,shop.Roles.All(role=>shop.Available(role.Id)+shop.Bench.Where(piece=>piece.Id==role.Id).Sum(piece=>piece.Star==3?9:piece.Star==2?3:1)+shop.Offers.Count(id=>id==role.Id)==9));
                yield return SynergyBadgeEvidenceFrame(directory,count==2?"diamond-02-bronze":count==4?"diamond-03-silver":count==6?"diamond-04-gold":"diamond-04b-reunion-eight-prismatic",check,count==8?"整合运动":null);
            }
            yield return PrepareSynergyBadgeEvidenceRoster(SynergyBadgeGuerrillaRoster);
            var native=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("sevenNativeKindsStayGoldAtSixThreshold",native.Count==7&&native.Tier==6&&SynergyBadgePresentation.GradeFor(native.Name,native.Tier)==SynergyVisualGrade.Gold);
            check("firstLegalTransferMakesEight",SetGuerrillaTransfer("CARD_M07",true)&&Battle.GuerrillaTier==8);
            var eight=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("eightIsPrismatic",eight.Count==8&&eight.Tier==8&&SynergyBadgePresentation.GradeFor(eight.Name,eight.Tier)==SynergyVisualGrade.Prismatic);
            yield return null;yield return null;
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-05-eight-prismatic",check,"游击队");
            check("secondLegalTransferMakesNine",SetGuerrillaTransfer("CARD_M08",true)&&Battle.GuerrillaTier==9);
            var nine=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("nineKeepsPrismatic",nine.Count==9&&nine.Tier==9&&SynergyBadgePresentation.GradeFor(nine.Name,nine.Tier)==SynergyVisualGrade.Prismatic);
            yield return null;yield return null;
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-06-nine-prismatic",check,"游击队");
            yield return SynergyBadgeGuiClick(directory,"open-guerrilla-details",SynergyBadgeEvidenceRow("游击队"),()=>inspectedSynergy=="游击队");
            check("actualSidebarClickOpensRealDetails",inspectedSynergy=="游击队"&&CampaignUiModalOpen);
            int gold=shop.Gold,dp=Battle.DP,owned=shop.Bench.Count;
            check("detailsBlockMap",!ShopMapPointAvailable(new Vector2(420,420)));
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-07-real-details-open",check);
            yield return SynergyBadgeGuiClick(directory,"close-guerrilla-details",new Rect(803,102,53,28),()=>inspectedSynergy==null);
            check("actualCloseKeepsInventoryAndBattle",inspectedSynergy==null&&!CampaignUiModalOpen&&shop.Gold==gold&&Battle.DP==dp&&shop.Bench.Count==owned&&Battle.GuerrillaTier==9);
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-08-real-details-closed",check,"游击队");

            // Ten legitimate one-star cards cover all nine tags without exceeding level/bench
            // capacity. Snow five and infection two exercise their extra information rows.
            yield return PrepareSynergyBadgeEvidenceRoster(SynergyBadgeNineTagRoster);
            check("legalTenKindInventoryCoversNineRealRows",shop.Level==10&&shopLineup.Count==10&&CurrentSynergies.Length==9&&shop.Bench.Count==10);
            check("preparationConstructionDoesNotCoverDeploymentBar",CampaignConstructToggleRect.yMax<=552&&SynergyRowsViewportRect.yMax<=CampaignConstructToggleRect.yMin);
            check("snowAndInfectionKeepCompleteThresholdSequences",CurrentSynergies.Single(row=>row.Name=="雪怪小队").Thresholds.SequenceEqual(new[]{2,5})&&CurrentSynergies.Single(row=>row.Name=="感染生物").Thresholds.SequenceEqual(new[]{2,4,6}));
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-09-nine-rows-preparation",check);
            if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin nine-row sidebar fixture.");
            SetShopVisible(false);for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            Battle.Begin();yield return null;yield return null;
            check("nineRowBattleReservesFourRowViewport",CurrentSynergies.Length==9&&SynergyRowsViewportRect.height<=4*48+1&&SynergyRowsViewportRect.height<SynergyRowContentRect(8).yMax);
            check("snowChargeSublineUsesLiveBattleState",Battle.SnowTier==5&&SnowstormChargeHudVisible&&Battle.InfectionTier==2);
            check("constructionAndQuickActionsStayBeforeDeploymentBar",CampaignConstructToggleRect.yMax<=CampaignQuickActionsRect.yMin&&CampaignQuickActionsRect.yMax<=552&&SynergyRowsViewportRect.yMax<=CampaignConstructToggleRect.yMin);
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-10-nine-rows-battle-top",check);
            string lastName=CurrentSynergies.Last().Name;float beforeScroll=campaignSynergyScroll.y;
            yield return SynergyBadgeGuiClick(directory,"scroll-nine-rows-to-bottom",SynergyRowsViewportRect,()=>
            {
                var last=SynergyBadgeEvidenceRow(lastName);var viewport=SynergyRowsViewportRect;
                return campaignSynergyScroll.y>beforeScroll+.5f&&last.yMin>=viewport.yMin-1&&last.yMax<=viewport.yMax+1;
            },"scroll",640);
            check("actualOsScrollReachesLastOfNine",campaignSynergyScroll.y>beforeScroll+.5f);
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-11-nine-rows-battle-bottom",check);
            yield return SynergyBadgeGuiClick(directory,"open-scrolled-last-details",SynergyBadgeEvidenceRow(lastName),()=>inspectedSynergy==lastName);
            check("actualScrolledLastRowOpensCorrectDetails",inspectedSynergy==lastName&&CampaignUiModalOpen);
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-12-scrolled-last-details",check);
            yield return SynergyBadgeGuiClick(directory,"close-scrolled-last-details",new Rect(803,102,53,28),()=>inspectedSynergy==null);
            // Only reunion/snow/infection are activated in this fixture. The quick panel has
            // one 22px muster/shatter line, followed by the normal end-verification button.
            var quick=CampaignQuickActionsRect;var returnButton=new Rect(quick.x,quick.y+26,quick.width,26);
            check("normalEndVerificationButtonIsReachable",returnButton.yMax<=552&&!SynergyRowsViewportRect.Overlaps(returnButton));
            yield return SynergyBadgeGuiClick(directory,"return-to-preparation-quick-action",returnButton,()=>Loop.CanShop);
            check("actualQuickActionReturnsWithoutLosingInventory",Loop.CanShop&&shop.Bench.Count==10&&shopLineup.Count==10&&CurrentSynergies.Length==9);
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            check("returnedPreparationTransitionCompleted",!ArtLoading&&!ShopPanelTransitioning);
            yield return SynergyBadgeEvidenceFrame(directory,"diamond-13-returned-preparation",check);
        }
    }

    public sealed class SynergyBadgeSmokeDriver:MonoBehaviour
    {
        public static bool Requested=>Debug.isDebugBuild&&Environment.GetCommandLineArgs().Contains("-synergyBadgeSmokeTest");
        static bool started;
        [Serializable]sealed class Result
        {
            public bool passed;
            public int width,height;
            public string fixture="Real Campaign/SynergyTrial scenes; explicit legal one-star Shop.Preset inventory and normal transfer operations. No tier/order/damage field edits. gui-step.json waits for external real GUI clicks. Screenshots include full OnGUI; no natural balance/whole-campaign claim.";
            public string[] checks;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Requested)return;started=true;
            var go=new GameObject("Explicit synergy badge Player verification");DontDestroyOnLoad(go);go.AddComponent<SynergyBadgeSmokeDriver>();
        }
        static IEnumerator Scene(string name,Action<ManualBattleController> ready)
        {
            yield return SceneManager.LoadSceneAsync(name);
            for(int i=0;i<1800;i++)
            {
                var controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&!controller.ArtLoading){yield return null;ready(controller);yield break;}
                yield return null;
            }
            throw new InvalidOperationException("Badge scene startup timed out: "+name);
        }
        static IEnumerator Run(string directory,Action<string,bool> check)
        {
            ManualBattleController controller=null;
            yield return Scene("Campaign",value=>controller=value);
            yield return controller.VerifySynergyBadgeCampaign(directory,check);
            controller=null;yield return Scene("SynergyTrial",value=>controller=value);
            yield return controller.VerifySynergyBadgeSandbox(directory,check);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/synergy-badge-player");Directory.CreateDirectory(directory);
            var result=new Result{width=Screen.width,height=Screen.height};var checks=new List<string>();bool passed=true,complete=true;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("SYNERGY_BADGE_EVENT "+name+"="+value);};
            float oldScale=Time.timeScale;var stack=new Stack<IEnumerator>();stack.Push(Run(directory,check));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    complete=false;while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                    Debug.LogException(failure);File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            Time.timeScale=oldScale;result.passed=passed&&complete;result.checks=checks.ToArray();
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("SYNERGY_BADGE_PLAYER_CHECK passed="+result.passed);yield return null;Application.Quit(result.passed?0:1);
        }
    }
}
