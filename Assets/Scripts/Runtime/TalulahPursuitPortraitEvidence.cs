using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        const string TalulahPursuitCard="CARD_TALULAH_TRIAL";
        static readonly string[] TalulahPursuitRoster={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M08",TalulahPursuitCard};
        [Serializable]sealed class TalulahPursuitPortraitResult
        {
            public bool passed;
            public string fixture="Explicit Level9 nine-kind inventory, seven native guerrillas plus M08 and Talulah. Real prepared transfers, Begin grant, M03 deployment and one military-order transaction. Same-age view.Play controls render original/legacy portrait choices without another resource transaction; these extra diagnostic plays are not extra military orders.";
            public string actualPortrait,expectedPortrait,legacySerializedPortrait,assignedExtensionSprite;
            public string[] checks,slots;
            public int talulahSlot,visiblePortraits,regionPixels,actualVsOriginalPixelDiff,actualVsLegacyPixelDiff;
            public int ordersBefore,ordersAfter,actualMilitaryOrders,actualPlayCount,diagnosticControlPlays;
            public float actualPeakAge;
        }
        static Texture2D TalulahPursuitReadback(GuerrillaOrderView view,string path)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new InvalidOperationException("Portrait evidence requires an actual GPU.");
            var target=new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32);
            var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try
            {
                target.Create();view.RenderTo(target);RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());return image;
            }
            catch{Destroy(image);throw;}
            finally{RenderTexture.active=previous;target.Release();Destroy(target);}
        }
        static bool TalulahPursuitInside(Vector2 point,Vector2[] polygon)
        {
            float sign=0;
            for(int i=0;i<polygon.Length;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Length];
                float cross=(b.x-a.x)*(point.y-a.y)-(b.y-a.y)*(point.x-a.x);
                if(Mathf.Abs(cross)<.001f)continue;
                if(sign==0)sign=Mathf.Sign(cross);else if(sign*cross<0)return false;
            }
            return true;
        }
        static int TalulahPursuitDifference(Texture2D first,Texture2D second,int slot,out int sampled)
        {
            var a=first.GetPixels32();var b=second.GetPixels32();int different=0;sampled=0;
            // An inset polygon excludes neighbouring panes and their antialiased borders.
            // All images are complete production RenderTo composites at exactly the same age.
            var polygon=GuerrillaOrderGeometry.Polygon(slot,.12f,.94f,-16,16);
            int loX=Mathf.Max(0,Mathf.FloorToInt(polygon.Min(p=>p.x))),hiX=Mathf.Min(first.width,Mathf.CeilToInt(polygon.Max(p=>p.x)));
            int loY=Mathf.Max(0,Mathf.FloorToInt(polygon.Min(p=>p.y))),hiY=Mathf.Min(first.height,Mathf.CeilToInt(polygon.Max(p=>p.y)));
            for(int y=loY;y<hiY;y++)for(int x=loX;x<hiX;x++)
            {
                if(!TalulahPursuitInside(new Vector2(x+.5f,y+.5f),polygon))continue;
                sampled++;int i=(first.height-1-y)*first.width+x;
                if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a)>3)different++;
            }
            return different;
        }
        void TalulahPursuitControl(GuerrillaOrderView view,string[] members,Dictionary<string,Sprite> portraits,string[] assignments,GuerrillaOrderVolley volley,float age)
        {view.Play(members,portraits,assignments);view.BindVolley(volley);view.AdvancePresentation(age);}
        internal IEnumerator VerifyTalulahPursuitPortrait(string directory)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var result=new TalulahPursuitPortraitResult();var checks=new List<string>();bool passed=true,complete=false;float oldScale=Time.timeScale;
            Texture2D actual=null,originalControl=null,legacyControl=null;var view=GetComponent<GuerrillaOrderView>();
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);passed&=value;Debug.Log("TALULAH_PURSUIT_PORTRAIT_EVENT "+name+"="+value);};
            try
            {
                yield return new WaitForSecondsRealtime(1);Time.timeScale=0;
                if(!SynergySandbox||!Loop.CanShop)throw new InvalidOperationException("Portrait evidence requires GuerrillaTrial preparation.");
                var cards=TalulahPursuitRoster.Select(id=>shopDefinitions[id].Card).ToArray();
                yield return Prepare(Plan(shopMapCopy.defenders,cards,true),()=>
                {
                    shop.Preset(TalulahPursuitRoster.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
                    shopLineup.Clear();shopLineup.AddRange(TalulahPursuitRoster);SyncShopDeck(false);SetShopVisible(true);inspectedSynergy=null;
                },"准备塔露拉追击头像验证");
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                var card=shopDefinitions[TalulahPursuitCard].Card;
                var expected=OriginalTalulahArt.For(card)?.Portrait;
                var legacy=(Sprite)typeof(CardDefinition).GetField("portrait",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(card);
                result.actualPortrait=card.Portrait!=null?card.Portrait.name:null;result.expectedPortrait=expected!=null?expected.name:null;result.legacySerializedPortrait=legacy!=null?legacy.name:null;
                check("publicPortraitIsTheOriginalTalulahSprite",expected!=null&&card.Portrait==expected&&card.Portrait.name=="TalulahOriginal/portrait");
                check("legacySerializedSpriteIsADistinctControl",legacy!=null&&expected!=null&&legacy!=expected&&card.Portrait!=legacy);
                if(expected==null||legacy==null)throw new InvalidOperationException("Both original and legacy portrait controls must exist.");
                check("twoRealPreparationTransfersReachNineTier",SetGuerrillaTransfer("CARD_M08",true)&&SetGuerrillaTransfer(TalulahPursuitCard,true)&&Battle.GuerrillaTier==9&&Battle.GuerrillaMemberIds.Count==9);
                if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot enter pursuit portrait battle.");
                while(ArtLoading||ShopPanelTransitioning)yield return null;
                Battle.Begin();DurableGuerrillaHighTierEvidenceGuards();if(SimulationSpeed!=1)ToggleSpeed();result.ordersBefore=Battle.Orders;
                var receipt=Battle.TryDeployRole(Battle.RunId,++requestSequence,Battle.Entry,"CARD_M03");
                check("realRemoteOrdinarySourceDeployedBeforeAttacking",receipt.Success&&Battle.FindUnit(receipt.EntityId).OrdinaryAttacksFired==0&&!Battle.FindUnit(receipt.EntityId).HasCombatTarget);
                if(!receipt.Success)throw new InvalidOperationException("M03 did not deploy.");
                check("exactlyOneRealOrderSpendsStartingTwo",UseCampaignOrder(false)&&result.ordersBefore==2&&Battle.Orders==0&&Battle.OrdersUsed==1&&view.PlayCount==1);
                var volley=Battle.LatestGuerrillaOrder;Tick(.65f);yield return null;yield return null;
                var members=Battle.GuerrillaMemberIds.ToArray();var assignments=GuerrillaTransferIds.ToArray();result.slots=view.SlotMembers.ToArray();
                result.talulahSlot=Array.IndexOf(result.slots,TalulahPursuitCard);result.actualPeakAge=view.Age;result.visiblePortraits=view.VisiblePortraitCount;result.actualPlayCount=view.PlayCount;
                var extensions=(Sprite[])typeof(GuerrillaOrderView).GetField("extensions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                var assigned=result.talulahSlot>=7?extensions[result.talulahSlot-7]:null;result.assignedExtensionSprite=assigned!=null?assigned.name:null;
                check("actualNinthSlotUsesOriginalSpriteAndNinePortraitsAreVisible",result.talulahSlot==8&&assigned==expected&&assigned!=legacy&&view.VisiblePortraitCount==9&&Math.Abs(view.Age-.65f)<.0001f);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"talulah-pursuit-peak.png"));yield return null;
                actual=TalulahPursuitReadback(view,Path.Combine(directory,"talulah-pursuit-actual-layer.png"));
                var actualExtras=members.Where(id=>!CampaignBalance.Guerrilla(id)).ToDictionary(id=>id,id=>shopDefinitions[id].Card.Portrait);
                var originalExtras=new Dictionary<string,Sprite>(actualExtras);originalExtras[TalulahPursuitCard]=expected;
                TalulahPursuitControl(view,members,originalExtras,assignments,volley,result.actualPeakAge);
                originalControl=TalulahPursuitReadback(view,Path.Combine(directory,"talulah-pursuit-original-control.png"));
                var legacyExtras=new Dictionary<string,Sprite>(actualExtras);legacyExtras[TalulahPursuitCard]=legacy;
                TalulahPursuitControl(view,members,legacyExtras,assignments,volley,result.actualPeakAge);
                legacyControl=TalulahPursuitReadback(view,Path.Combine(directory,"talulah-pursuit-legacy-control.png"));
                result.actualVsOriginalPixelDiff=TalulahPursuitDifference(actual,originalControl,result.talulahSlot,out int sampled);result.regionPixels=sampled;
                result.actualVsLegacyPixelDiff=TalulahPursuitDifference(actual,legacyControl,result.talulahSlot,out int legacySampled);
                check("actualTalulahPaneMatchesOriginalControlPixels",sampled>1000&&result.actualVsOriginalPixelDiff==0);
                check("actualTalulahPaneDiffersFromLegacyControlPixels",legacySampled==sampled&&result.actualVsLegacyPixelDiff>500);
                // Leave the actual selected sprite visible. Control Play calls never spend orders.
                TalulahPursuitControl(view,members,actualExtras,assignments,volley,result.actualPeakAge);yield return null;
                result.ordersAfter=Battle.Orders;result.actualMilitaryOrders=Battle.OrdersUsed;result.diagnosticControlPlays=view.PlayCount-result.actualPlayCount;
                check("diagnosticControlsDoNotCreateMoreMilitaryOrders",Battle.Orders==0&&Battle.OrdersUsed==1&&ReferenceEquals(volley,Battle.LatestGuerrillaOrder)&&result.diagnosticControlPlays==3&&!volley.Resolved);
                complete=true;
            }
            finally
            {
                if(actual!=null)Destroy(actual);if(originalControl!=null)Destroy(originalControl);if(legacyControl!=null)Destroy(legacyControl);
                Time.timeScale=oldScale;result.passed=passed&&complete;result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("TALULAH_PURSUIT_PORTRAIT_PLAYER_CHECK passed="+result.passed);Application.Quit(result.passed?0:1);
            }
        }
    }
    public sealed class TalulahPursuitPortraitSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-talulahPursuitPortraitSmokeTest"))return;
            started=true;var go=new GameObject("Talulah pursuit portrait explicit Player verification");DontDestroyOnLoad(go);go.AddComponent<TalulahPursuitPortraitSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;yield return null;yield return SceneManager.LoadSceneAsync("GuerrillaTrial");
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++)
            {controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&controller.SynergySandbox&&!controller.ArtLoading)break;yield return null;}
            if(controller==null||controller.ArtLoading)throw new InvalidOperationException("GuerrillaTrial did not load for Talulah portrait evidence.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/talulah-pursuit-portrait-player");
            var stack=new Stack<IEnumerator>();stack.Push(controller.VerifyTalulahPursuitPortrait(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
        }
    }
}
