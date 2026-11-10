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
        [Serializable] sealed class BalanceFirstPassStageResult
        {
            public int stage,defenderHits,playerHits;
            public float defenderHealth,defenderAttack,defenderRange,firstShotDistance;
            public double firstExchangeAt;
        }
        [Serializable] sealed class BalanceFirstPassResult
        {
            public bool passed;
            public string scene;
            public string scope="Saved ReunionTrial and explicit finite-stock Campaign stage 7/8/11 fixtures; production card pointer handlers and natural combat. Stages and inventories are fixtures, not a natural thirteen-stage playthrough. No injected battle damage, forced victory, badge drops or OS mouse claim.";
            public string[] checks;
            public BalanceFirstPassStageResult[] stages;
        }
        readonly List<BalanceFirstPassStageResult> balanceFirstPassStages=new List<BalanceFirstPassStageResult>();

        IEnumerator BalanceFirstPassReady()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while((ArtLoading||ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Balance fixture art/panel preparation timed out.");
            yield return null;
        }
        IEnumerator BalanceFirstPassCapture(string directory,string name)
        {
            if(directory==null)yield break;
            yield return new WaitForEndOfFrame();
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(screen==null)throw new InvalidOperationException("Balance evidence screenshot was unavailable.");
            try{File.WriteAllBytes(Path.Combine(directory,name+".png"),screen.EncodeToPNG());}
            finally{Destroy(screen);}
            yield return null;
        }
        static ShopTrialModel.Piece BalancePiece(string id,int star)=>new ShopTrialModel.Piece{Id=id,Star=star};
        static float BalanceGroundDistance(Vector3 a,Vector3 b){var offset=a-b;offset.y=0;return offset.magnitude;}

        // Only inventory/encounter selection is preset. All battle snapshots are rebuilt by production code.
        internal IEnumerator PrepareBalanceFirstPassFixture(int stage,bool addOne=false)
        {
            yield return BalanceFirstPassReady();
            Time.timeScale=0;diagnostic=true;campaignConstructOpen=false;inspectedSynergy=null;
            if(stage==0&&(!SynergySandbox||!Loop.CanShop))throw new InvalidOperationException("The first balance fixture requires saved sandbox preparation.");
            if(stage!=0)StartCampaign(false,61108+stage);
            var pieces=stage==0?new[]{BalancePiece("CARD_M07",2),BalancePiece("CARD_M08",4),BalancePiece("CARD_M11",2),
                BalancePiece("CARD_M12",1),BalancePiece("CARD_M29",2),BalancePiece("CARD_M30",1)}:
                new[]{BalancePiece("CARD_M07",3),BalancePiece("CARD_M08",3)};
            if(stage==0&&addOne)pieces=pieces.Concat(new[]{BalancePiece("CARD_M02",1)}).ToArray();
            var guards=stage==0?activeSynergyTrial.CopyDefenders():CampaignPlan.Guards(stage,false);
            var cards=pieces.Select(p=>shopDefinitions[p.Id].Card).ToArray();
            yield return Prepare(Plan(guards,cards,true),()=>
            {
                shop.Preset(pieces,Array.Empty<string>(),8,0,100);
                shopLineup.Clear();shopLineup.AddRange(pieces.Select(p=>p.Id));
                seatedShop=null;ClearShopFlows();
                if(stage!=0)
                {
                    Loop=new BattleShopRun(shop,61108+stage,false,true,false);
                    // Explicit encounter fixture, never a forced battle result or reward receipt.
                    typeof(BattleShopRun).GetProperty(nameof(BattleShopRun.Stage)).SetValue(Loop,stage);
                }
                ConfigureLoopStage();SetShopVisible(stage==0);
            },"准备首轮数值验收");
            yield return BalanceFirstPassReady();shop.Validate();
        }
        bool BalanceFirstPassDrag(string id,Vector3 entry)
        {
            int index=shop.Bench.ToList().FindIndex(p=>p.Id==id&&IsLineupPiece(p));
            if(index<0||Camera.main==null)return false;
            var origin=ShopPieceRect(index);if(origin.width<=0)return false;
            var destination=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(entry));
            int count=Battle.Units.Count(u=>!u.SynergySupport);
            ProcessShopPointer(EventType.MouseDown,origin.center);ProcessShopPointer(EventType.MouseDrag,destination);ProcessShopPointer(EventType.MouseUp,destination);
            return Battle.Units.Count(u=>!u.SynergySupport)==count+1&&Battle.Units.Last(u=>!u.SynergySupport).CardId==id;
        }
        bool BalanceFirstPassStockConserved()=>shop.Roles.All(r=>shop.Available(r.Id)+shop.Offers.Count(id=>id==r.Id)+
            shop.Bench.Where(p=>p.Id==r.Id).Sum(p=>ShopTrialModel.Copies(p.Star))==shop.Capacity(r.Id));

        internal IEnumerator BalanceFirstPassTrialChecks(Action<string,bool> check,string directory=null)
        {
            check("saved-reunion-scene-and-authored-map",SceneManager.GetActiveScene().name=="ReunionTrial"&&
                activeSynergyTrial?.trialId=="reunion"&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
            yield return PrepareBalanceFirstPassFixture(0);
            check("finite-stock-four-star-fixture",BalanceFirstPassStockConserved()&&shop.Available("CARD_M08")==3);
            check("selected-member-total-stars",Battle.ReunionTotalStars==9&&Battle.SnowTotalStars==3&&Battle.DeepTotalStars==3&&
                Battle.ReunionTier==4&&Battle.SnowTier==2&&Battle.DeepTier==2);
            check("total-star-snapshot-multipliers",Math.Abs(Battle.ReunionStarMultiplier-1.5f)<.0001f&&
                Math.Abs(Battle.SnowStarMultiplier-(float)Math.Pow(1.5,.4))<.0001f&&
                Math.Abs(Battle.DeepStarMultiplier-(float)Math.Pow(1.5,1d/3))<.0001f);
            check("new-basic-values-in-sandbox-snapshots",Battle.FindCard("CARD_M07").Settings.Health==1436&&
                Battle.FindCard("CARD_M08").Settings.Health==2124&&CampaignBalance.Profiles["CARD_M08"].Attack==27&&
                Math.Abs(CampaignBalance.AttackScale(1,4)-3)<.0001f);
            float priorScale=Battle.ReunionStarMultiplier;
            yield return PrepareBalanceFirstPassFixture(0,true);
            check("adding-one-star-member-cannot-dilute-same-tier",Battle.ReunionTier==4&&Battle.ReunionCount==5&&
                Battle.ReunionTotalStars==10&&Battle.ReunionStarMultiplier>priorScale&&Battle.SnowTotalStars==3&&Battle.DeepTotalStars==3);
            yield return PrepareBalanceFirstPassFixture(0);
            check("sandbox-guerrilla-transfer-remains-available",SetGuerrillaTransfer("CARD_M07",true)&&SetGuerrillaTransfer("CARD_M07",false));
            check("sandbox-mudrock-transfer-remains-available",SetMudrockTransfer("CARD_M08",true)&&SetMudrockTransfer("CARD_M08",false));
            yield return BalanceFirstPassReady();
            yield return BalanceFirstPassCapture(directory,"01-total-stars-and-four-star-preparation");
            if(directory!=null)
            {
                inspectedSynergy="雪怪小队";
                yield return BalanceFirstPassCapture(directory,"01b-snow-total-stars-detail");
                inspectedSynergy="深池";
                yield return BalanceFirstPassCapture(directory,"01c-deep-total-stars-detail");
                inspectedSynergy=null;
            }
            if(!StartLoopNextBattle())throw new InvalidOperationException("Balance trial could not begin.");
            yield return BalanceFirstPassReady();Battle.Begin();Battle.SetPaused(true);
            int dp=Battle.DP,uses=Battle.FindCard("CARD_M08").RemainingUses;
            check("production-pointer-four-star-deploy",BalanceFirstPassDrag("CARD_M08",Battle.Entry));
            var archer=Battle.Units.Last(u=>!u.SynergySupport&&u.CardId=="CARD_M08");
            check("deployed-four-star-new-attack-and-real-cost",Math.Abs(Battle.UnitCombat(archer).UnitAttack-162)<.001f&&
                archer.MaxHealth==2124&&Battle.DP==dp-5&&Battle.FindCard("CARD_M08").RemainingUses==uses-1);
            double clock=Battle.Clock;var position=archer.Motion.Position;float health=archer.Health;
            Tick(5);
            check("pause-freezes-real-unit-and-clock",Battle.Clock==clock&&archer.Motion.Position==position&&archer.Health==health);
            check("deployment-does-not-add-synergy-stars",Battle.ReunionTotalStars==9&&Battle.SnowTotalStars==3&&Battle.DeepTotalStars==3);
            yield return BalanceFirstPassCapture(directory,"02-paused-production-deployment");
            var previous=Battle;
            check("restart-accepted-before-settlement",RestartCampaignEncounter());
            yield return BalanceFirstPassReady();
            check("restart-rebuilds-without-stacking",!ReferenceEquals(previous,Battle)&&Battle.Units.Count==0&&Battle.Clock==0&&
                Battle.DP==100&&Battle.ReunionTotalStars==9&&Math.Abs(Battle.ReunionStarMultiplier-1.5f)<.0001f&&
                Battle.FindCard("CARD_M07").Settings.Health==1436&&Battle.FindCard("CARD_M08").Settings.Health==2124);
        }

        internal IEnumerator BalanceFirstPassStageChecks(int stage,Action<string,bool> check,string directory=null)
        {
            yield return PrepareBalanceFirstPassFixture(stage);
            string prefix="stage-"+stage+"-",id="campaign-ranged-"+stage;
            var guard=Battle.FindDefender(id);
            float expectedHealth=stage==7?220:stage==8?260:340,expectedAttack=stage==7?24:stage==8?30:48;
            check(prefix+"formal-finite-stock-fixture",CampaignActive&&!SynergySandbox&&!shop.UnlimitedGold&&Loop.Stage==stage&&BalanceFirstPassStockConserved());
            check(prefix+"formal-free-transfers-are-rejected",!SetGuerrillaTransfer("CARD_M07",true)&&!SetMudrockTransfer("CARD_M08",true)&&
                GuerrillaTransferIds.Count==0&&MudrockTransferIds.Count==0);
            check(prefix+"actual-d07-profile",guard!=null&&guard.Settings.ArtId=="D07"&&guard.MaxHealth==expectedHealth&&
                guard.Settings.Attack==expectedAttack&&Math.Abs(guard.Settings.Range-3.2f)<.001f&&guard.Settings.BlockCount==0&&
                guard.Settings.DamageType==CombatDamageType.Physical&&!guard.Settings.Skill.Enabled);
            if(guard==null)throw new InvalidOperationException("The Campaign fixture did not create "+id);
            var visual=FindObjectsByType<DefenderRosterView>(FindObjectsSortMode.None).Select(v=>v.FindVisual(id)).FirstOrDefault(v=>v!=null);
            check(prefix+"saved-map-d07-model-visible",visual!=null&&visual.enabled&&visual.sprite!=null&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
            check(prefix+"formal-unit-values-without-sandbox-boost",Battle.FindCard("CARD_M07").Settings.Health==615&&Battle.FindCard("CARD_M08").Settings.Health==375);
            yield return BalanceFirstPassCapture(directory,"03-"+prefix+"d07-ready");
            var lane=Battle.Lanes.OrderBy(l=>l.Points.Min(p=>BalanceGroundDistance(p,guard.Position))).ThenBy(l=>BalanceGroundDistance(l.Entry,guard.Position)).First();
            Battle.Begin();Battle.SetPaused(true);
            check(prefix+"production-pointer-tank-deploy",BalanceFirstPassDrag("CARD_M07",lane.Entry));
            check(prefix+"production-pointer-ranged-deploy",BalanceFirstPassDrag("CARD_M08",lane.Entry));
            Battle.SetPaused(false);
            bool defenderHit=false,playerHit=false;
            for(int step=0;step<1600&&Battle.Phase==BattlePhase.Running&&!(defenderHit&&playerHit);step++)
            {
                Tick(.05f);
                defenderHit=Battle.CombatShots.Any(s=>s.FromDefender&&s.SourceId==id&&s.Applied);
                playerHit=Battle.CombatShots.Any(s=>!s.FromDefender&&s.TargetId==id&&s.Applied);
                if(step%4==0)yield return null;
            }
            check(prefix+"natural-two-way-combat",defenderHit&&playerHit&&guard.Health<guard.MaxHealth);
            var first=Battle.CombatShots.FirstOrDefault(s=>s.FromDefender&&s.SourceId==id);
            float distance=first==null?0:BalanceGroundDistance(first.Origin,first.Destination);
            check(prefix+"natural-ranged-shot-distance",first!=null&&distance>1.1f&&distance<=3.25f);
            balanceFirstPassStages.Add(new BalanceFirstPassStageResult{stage=stage,defenderHealth=guard.MaxHealth,defenderAttack=guard.Settings.Attack,
                defenderRange=guard.Settings.Range,firstShotDistance=distance,firstExchangeAt=Battle.Clock,
                defenderHits=Battle.CombatShots.Count(s=>s.FromDefender&&s.SourceId==id&&s.Applied),playerHits=Battle.CombatShots.Count(s=>!s.FromDefender&&s.TargetId==id&&s.Applied)});
            Battle.SetPaused(true);double clock=Battle.Clock;float remaining=guard.Health;Tick(3);
            check(prefix+"pause-stops-exchange",Battle.Clock==clock&&guard.Health==remaining);
            yield return BalanceFirstPassCapture(directory,"04-"+prefix+"natural-ranged-exchange");
            var previous=Battle;
            check(prefix+"restart-before-result",RestartCampaignEncounter());yield return BalanceFirstPassReady();
            check(prefix+"restart-keeps-exact-unboosted-values",!ReferenceEquals(previous,Battle)&&Battle.Clock==0&&Battle.Units.Count==0&&
                Battle.FindDefender(id)?.MaxHealth==expectedHealth&&Battle.FindCard("CARD_M07").Settings.Health==615&&BalanceFirstPassStockConserved());
        }

        internal IEnumerator VerifyBalanceFirstPass(string directory)
        {
            Directory.CreateDirectory(directory);var result=new BalanceFirstPassResult{scene=SceneManager.GetActiveScene().name};
            var checks=new List<string>();bool valid=true,completed=false;float previousScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("BALANCE_FIRST_PASS "+name+"="+ok);};
            balanceFirstPassStages.Clear();
            try
            {
                diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
                yield return BalanceFirstPassTrialChecks(check,directory);
                foreach(int stage in new[]{7,8,11})yield return BalanceFirstPassStageChecks(stage,check,directory);
                completed=true;
            }
            finally
            {
                Time.timeScale=previousScale;CancelShopDrag();result.passed=valid&&completed;result.checks=checks.ToArray();result.stages=balanceFirstPassStages.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("BALANCE_FIRST_PASS_PLAYER_PASS="+result.passed+"; checks="+checks.Count);
            }
        }
    }

    public sealed class BalanceFirstPassSmokeDriver:MonoBehaviour
    {
        static bool installed;
        public static bool Requested=>Debug.isDebugBuild&&Environment.GetCommandLineArgs().Contains("-balanceFirstPassSmokeTest");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(installed||!Requested)return;installed=true;
            var go=new GameObject("Balance first pass explicit verification");DontDestroyOnLoad(go);go.AddComponent<BalanceFirstPassSmokeDriver>();
        }
        IEnumerator Verify(string directory)
        {
            Application.runInBackground=true;yield return null;
            if(SceneManager.GetActiveScene().name!="ReunionTrial")yield return SceneManager.LoadSceneAsync("ReunionTrial");
            double deadline=Time.realtimeSinceStartupAsDouble+600;ManualBattleController controller=null;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||controller.ArtLoading||controller.ShopPanelTransitioning)throw new InvalidOperationException("Saved balance trial startup timed out.");
            yield return controller.VerifyBalanceFirstPass(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-balanceEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.GetFullPath("artifacts/balance-first-pass-player");
            Directory.CreateDirectory(directory);var stack=new Stack<IEnumerator>();stack.Push(Verify(directory));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            bool passed=File.Exists(Path.Combine(directory,"result.json"))&&File.ReadAllText(Path.Combine(directory,"result.json")).Contains("\"passed\": true");
            Application.Quit(passed?0:1);
        }
    }
}
