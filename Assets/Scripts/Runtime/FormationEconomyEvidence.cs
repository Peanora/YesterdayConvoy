using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class FormationShopSession
    {
        internal static FormationShopSession EconomyFixture(int seed)
        {
            var session=StartPractice(seed,Array.FindIndex(FormationCampaignRules.Nodes,n=>n.Id=="2-7"));
            session.Run.ResetBalanceFixture();session.Shop.EnableFormationEconomy();
            var ids=new[]{"M17","M16","B05","B01","B22","M38","WIS"}.Select(id=>FormationCampaignRules.Find(id).CardId).ToArray();
            session.Shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),9,0,100);
            session.Seats=new ShopSeatLayout(true);session.lineup.Clear();session.lineup.AddRange(ids);
            session.instancePositions.Clear();session.kindPositions.Clear();session.Synchronize();session.Shop.Refresh(true);
            return session;
        }
    }
    public sealed partial class FormationBattleController
    {
        internal IEnumerator OpenEconomyFixture()
        {
            seed=20261128;Session=FormationShopSession.EconomyFixture(seed);
            Model=new FormationBattleModel(seed,SelectFormationTerrain(Session.Run));Session.ApplyTo(Model);
            ResetFormationPreparation("经济试玩预设：9级、100金、6名启动角色和一张五费；非自然运营开局。");
            foreach(string id in RequiredFormationArtIds().ToArray())yield return WarmFormationArt(id);
            double deadline=Time.realtimeSinceStartupAsDouble+90;
            while((!RosterArtReady||CampaignInteractionBlocked)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(!RosterArtReady||CampaignInteractionBlocked)throw new InvalidOperationException("Economic fixture art or UI not ready");
            RefreshFormationAnimationProfiles();SyncActors();SetMarketVisible(true);
        }
    }
    /// <summary>Opt-in rendered player evidence. All earning, purchases and fusion use the live session.</summary>
    public sealed class FormationEconomyEvidence : MonoBehaviour
    {
        public static bool Drives {get;private set;}
        static bool Requested=>Environment.GetCommandLineArgs().Contains("-formationEconomySmoke");
        [Serializable] sealed class Report
        {
            public string scope="Rendered Windows Player, actual session purchases/sales/refreshes and real nine-copy fusion. Explicit 9-level/100-gold/six-body economic fixture, not natural campaign. Fixed 2-7 map; no equipment or forced damage.";
            public bool passed,battleWon;public int refreshes,gold,layersF,layersM,checks;public float seconds;public List<string> exceptions=new List<string>();
        }
        readonly Report report=new Report();string directory;
        FormationBattleController controller;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            Drives=false;if(!Requested)return;
            Drives=true;var go=new GameObject("Finite economy evidence");DontDestroyOnLoad(go);go.AddComponent<FormationEconomyEvidence>();
        }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string trace,LogType type){if(type==LogType.Exception)report.exceptions.Add(message);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationEvidenceDir");
            directory=Path.GetFullPath(at>=0?args[at+1]:"artifacts/economy-v09/player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Work());
            while(stack.Count>0)
            {
                bool next;object current=null;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception error){report.exceptions.Add(error.ToString());Save();Application.Quit(1);yield break;}
                if(!next){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        void Check(bool value,string message){report.checks++;if(!value)throw new InvalidOperationException(message);}
        void Save()=>File.WriteAllText(Path.Combine(directory,"economy-smoke.json"),JsonUtility.ToJson(report,true));
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;
        }
        IEnumerator Work()
        {
            Application.runInBackground=true;FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
            if(SceneManager.GetActiveScene().name!=FormationBattleController.SceneName)yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            while(((controller=FindAnyObjectByType<FormationBattleController>())==null||!controller.Ready)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Check(controller!=null&&controller.Ready,"Actual scene failed to become ready");
            yield return controller.OpenEconomyFixture();
            var shop=controller.Shop;var economy=controller.Campaign.Economy;
            Check(!shop.UnlimitedGold&&shop.Gold==100,"Fixture must have finite cash");
            Check(controller.Campaign.TraitCount("远见")==3&&controller.Campaign.TraitCount("奇迹")==3,"Economic member counts");
            yield return Shot("01-start");
            string target=FormationCampaignRules.Find("WIS").CardId;
            var keepers=shop.Bench.Where(p=>p.Id!=target).Select(p=>p.Id).ToArray();int milestone=0;
            for(int step=0;step<1000;step++)
            {
                for(int slot=0;slot<5;slot++)
                {
                    string id=shop.Offers[slot];if(id==null||keepers.Contains(id))continue;
                    int expectedPrice=shop.Price(id);
                    var result=controller.Session.Purchase(slot);if(!result.Success)continue;
                    Check(result.Value==expectedPrice,"Actual charged price matches preview");
                    if(id!=target)Check(controller.Session.Sell(result.Instance).Success,"Actual session sale failed");
                }
                if(economy.ForesightLayers>=150&&milestone==0)
                {milestone=1;Check(shop.Price(target)==4,"Global five-cost discount");yield return Shot("02-global-discount");}
                if(economy.MiracleLayers>=274&&milestone==1)
                {milestone=2;Check(economy.FreeChance==1,"Mature miracle chance");yield return Shot("03-miracle-chain");}
                if(shop.Bench.Any(p=>p.Id==target&&p.Star==3))break;
                Check(controller.Session.Refresh().Success,"Finite economic engine ran out of cash");report.refreshes++;yield return null;
            }
            Check(shop.Bench.Any(p=>p.Id==target&&p.Star==3),"Target did not fuse from nine actual copies");
            Check(shop.Available(target)==0,"Five-cost nine-copy stock exhausted exactly");shop.Validate();
            yield return Shot("04-five-cost-three-star");
            report.gold=shop.Gold;report.layersF=economy.ForesightLayers;report.layersM=economy.MiracleLayers;Save();
            controller.SetMarketVisible(false);yield return new WaitForSecondsRealtime(.6f);
            controller.StartBattle();
            Check(controller.Model.Phase==FormationPhase.Fighting,"Actual battle could not start: "+controller.BalanceStartState);
            while(controller.FormationBattleOpeningActive)yield return null;
            var model=controller.Model;bool shot=false;
            while(model.Phase==FormationPhase.Fighting||model.Phase==FormationPhase.Exiting)
            {
                for(int i=0;i<20&&(model.Phase==FormationPhase.Fighting||model.Phase==FormationPhase.Exiting);i++)
                {controller.RefreshBalanceTiming();model.Advance(.025f);}
                yield return null;
                if(!shot&&model.Clock>=10){shot=true;yield return Shot("05-real-combat");}
            }
            report.battleWon=model.Phase==FormationPhase.Won;report.seconds=(float)model.Clock;
            Check(report.battleWon,"Three-star five-cost fixture did not enter blue gate");
            Check(report.exceptions.Count==0,"Runtime script exception");report.passed=true;Save();
            Debug.Log("ECONOMY_SMOKE_PASS: "+report.checks+" checks, "+report.refreshes+" refreshes, "+shop.Gold+" gold");Application.Quit(0);
        }
    }
}
