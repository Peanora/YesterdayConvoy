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
        // Explicit inventory fixtures only. No battle tier, damage, stock or order field is edited.
        static readonly string[] SynergyBadgeGuerrillaRoster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        [Serializable]sealed class SynergyBadgeGuiStep {public int id;public string name;public float x,y,width,height;}
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
            var list=SynergyListRect;
            return new Rect(list.x,list.y+28+index*46-campaignSynergyScroll.y,list.width,41);
        }
        Rect SynergyBadgeEvidenceIcon(string name)
        {
            var row=SynergyBadgeEvidenceRow(name);
            return new Rect(row.x+7,row.y+2,20,20);
        }

        static int SynergyBadgeScreenHues(Texture2D screen,Rect logical)
        {
            var viewport=new TacticalViewport(screen.width,screen.height);
            int left=Mathf.Clamp(Mathf.CeilToInt(viewport.Offset.x+logical.x*viewport.Scale),0,screen.width-1);
            int right=Mathf.Clamp(Mathf.FloorToInt(viewport.Offset.x+logical.xMax*viewport.Scale),left+1,screen.width);
            int bottom=Mathf.Clamp(Mathf.CeilToInt(screen.height-viewport.Offset.y-logical.yMax*viewport.Scale),0,screen.height-1);
            int top=Mathf.Clamp(Mathf.FloorToInt(screen.height-viewport.Offset.y-logical.y*viewport.Scale),bottom+1,screen.height);
            var hues=new HashSet<int>();
            for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)
            {
                Color.RGBToHSV(screen.GetPixel(x,y),out float h,out float saturation,out float value);
                // Neutral backing and dim antialias fringes cannot manufacture a rainbow.
                if(saturation>.14f&&value>.38f)hues.Add(Mathf.Min(11,Mathf.FloorToInt(h*12)));
            }
            return hues.Count;
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
                    int hues=SynergyBadgeScreenHues(screen,SynergyBadgeEvidenceIcon(prismaticName));
                    check(file+"ActualIconHasAtLeastThreeHueFamilies",hues>=3);
                    Debug.Log("SYNERGY_BADGE_SCREEN_HUES "+file+"="+hues);
                }
            }
            finally {Destroy(screen);}
            yield return null;
        }

        IEnumerator SynergyBadgeGuiClick(string directory,string name,Rect rect,Func<bool> accepted)
        {
            int id=++synergyBadgeGuiSequence;Vector2 point=TacticalViewport.Current.ToScreen(rect.center);
            File.WriteAllText(Path.Combine(directory,"gui-step.json"),JsonUtility.ToJson(new SynergyBadgeGuiStep
                {id=id,name=name,x=point.x,y=Screen.height-point.y,width=Screen.width,height=Screen.height},true));
            // An external operator clicks the actual IMGUI control, then acknowledges this id.
            // This driver never assigns inspectedSynergy or dispatches a synthetic GUI event.
            double deadline=Time.realtimeSinceStartupAsDouble+300;
            for(;;)
            {
                string ack=Path.Combine(directory,"gui-ack.txt");
                if(File.Exists(ack)&&File.ReadAllText(ack).Trim()==id.ToString()&&accepted())break;
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
            yield return SynergyBadgeEvidenceFrame(directory,"01-campaign-inactive",check);
        }

        internal IEnumerator VerifySynergyBadgeSandbox(string directory,Action<string,bool> check)
        {
            diagnostic=true;yield return PrepareCampaign(true,81272);Time.timeScale=0;
            check("realSandboxScene",SceneManager.GetActiveScene().name=="SynergyTrial"&&SynergySandbox);
            var icons=SynergyTrialCatalog.Entries.Select(entry=>SynergyBadgePresentation.IconFor(entry.Tag)).ToArray();
            check("nineDistinctRealResourcesLoad",icons.All(icon=>icon!=null&&icon.width==256&&icon.height==256)&&icons.Distinct().Count()==9);
            foreach(int count in new[]{2,4,6})
            {
                yield return PrepareSynergyBadgeEvidenceRoster(SynergyTrialCatalog.Find("reunion").Members.Take(count).ToArray());
                var row=CurrentSynergies.Single(summary=>summary.Name=="整合运动");
                var expected=count==2?SynergyVisualGrade.Bronze:count==4?SynergyVisualGrade.Silver:SynergyVisualGrade.Gold;
                check("real"+count+"KindRosterUses"+expected,row.Count==count&&row.Tier==count&&SynergyBadgePresentation.GradeFor(row.Name,row.Tier)==expected);
                check("stockConservedAt"+count,shop.Roles.All(role=>shop.Available(role.Id)+shop.Bench.Where(piece=>piece.Id==role.Id).Sum(piece=>piece.Star==3?9:piece.Star==2?3:1)+shop.Offers.Count(id=>id==role.Id)==9));
                yield return SynergyBadgeEvidenceFrame(directory,count==2?"02-bronze":count==4?"03-silver":"04-gold",check);
            }
            yield return PrepareSynergyBadgeEvidenceRoster(SynergyBadgeGuerrillaRoster);
            var native=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("sevenNativeKindsStayGoldAtSixThreshold",native.Count==7&&native.Tier==6&&SynergyBadgePresentation.GradeFor(native.Name,native.Tier)==SynergyVisualGrade.Gold);
            check("firstLegalTransferMakesEight",SetGuerrillaTransfer("CARD_M07",true)&&Battle.GuerrillaTier==8);
            var eight=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("eightIsPrismatic",eight.Count==8&&eight.Tier==8&&SynergyBadgePresentation.GradeFor(eight.Name,eight.Tier)==SynergyVisualGrade.Prismatic);
            yield return null;yield return null;
            yield return SynergyBadgeEvidenceFrame(directory,"05-eight-prismatic",check,"游击队");
            check("secondLegalTransferMakesNine",SetGuerrillaTransfer("CARD_M08",true)&&Battle.GuerrillaTier==9);
            var nine=CurrentSynergies.Single(row=>row.Name=="游击队");
            check("nineKeepsPrismatic",nine.Count==9&&nine.Tier==9&&SynergyBadgePresentation.GradeFor(nine.Name,nine.Tier)==SynergyVisualGrade.Prismatic);
            yield return null;yield return null;
            yield return SynergyBadgeEvidenceFrame(directory,"06-nine-prismatic",check,"游击队");
            yield return SynergyBadgeGuiClick(directory,"open-guerrilla-details",SynergyBadgeEvidenceRow("游击队"),()=>inspectedSynergy=="游击队");
            check("actualSidebarClickOpensRealDetails",inspectedSynergy=="游击队"&&CampaignUiModalOpen);
            int gold=shop.Gold,dp=Battle.DP,owned=shop.Bench.Count;
            check("detailsBlockMap",!ShopMapPointAvailable(new Vector2(420,420)));
            yield return SynergyBadgeEvidenceFrame(directory,"07-real-details-open",check);
            yield return SynergyBadgeGuiClick(directory,"close-guerrilla-details",new Rect(803,102,53,28),()=>inspectedSynergy==null);
            check("actualCloseKeepsInventoryAndBattle",inspectedSynergy==null&&!CampaignUiModalOpen&&shop.Gold==gold&&Battle.DP==dp&&shop.Bench.Count==owned&&Battle.GuerrillaTier==9);
            yield return SynergyBadgeEvidenceFrame(directory,"08-real-details-closed",check,"游击队");
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
