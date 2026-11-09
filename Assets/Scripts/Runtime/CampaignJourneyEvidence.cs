using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Programmatic production input only. Never changes combat values or source assets.
    public static class CampaignJourneyEvidenceProbe
    {
        public static IEnumerator Host(Action<ManualBattleController> receive)=>WaitHost(receive,true);
        public static IEnumerator InitialHost(Action<ManualBattleController> receive)=>WaitHost(receive,false);
        static IEnumerator WaitHost(Action<ManualBattleController> receive,bool selection)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+240;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                var active=SceneManager.GetActiveScene();
                if(c!=null&&c.gameObject.scene==active&&(!selection||active.name=="MonsterTrial")&&c.MonsterTests.Count>0&&!c.ArtLoading&&!c.JourneyActive&&!c.ShopTrialActive)
                {receive(c);yield break;}
                yield return null;
            }
            throw new InvalidOperationException("Campaign journey host did not initialize.");
        }
        public static IEnumerator Ready(int stage,Action<ManualBattleController> receive)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+240;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.gameObject.scene==SceneManager.GetActiveScene()&&c.JourneyActive&&c.Loop.Stage==stage&&c.JourneyMap!=null&&
                    SceneManager.GetActiveScene().name==c.JourneyMap.sceneName&&!c.ArtLoading&&!c.ShopPanelTransitioning&&c.Battle!=null&&c.Battle.CampaignV2Stage==stage)
                {
                    // The ordinary form chooser remains enabled. Verification accepts
                    // the existing default through the same public close path as the UI.
                    if(c.CampaignOverlayVisible){c.CloseCampaignOverlay();yield return null;continue;}
                    receive(c);yield return null;yield break;
                }
                yield return null;
            }
            throw new InvalidOperationException("Campaign journey preparation timed out at stage "+stage+"; actual scene="+SceneManager.GetActiveScene().name);
        }
        public static bool StockConserved(ShopTrialModel shop)
        {
            try{shop.Validate();return shop.Roles.All(r=>shop.Available(r.Id)+shop.Offers.Count(id=>id==r.Id)+shop.Bench.Where(p=>p.Id==r.Id).Sum(p=>ShopTrialModel.Copies(p.Star))==shop.Capacity(r.Id));}
            catch(InvalidOperationException){return false;}
        }
        public static string State(ShopTrialModel shop,bool commission=true)=>
            shop.Gold+"/"+shop.Level+"/"+shop.XP+"/"+shop.Locked+"/"+(commission?shop.CommissionUsed.ToString():"ignored")+";"+
            string.Join(",",shop.Offers.Select(id=>id??"-"))+";"+string.Join(",",shop.Bench.Select(p=>p.Instance+":"+p.Id+":"+p.Star))+";"+
            string.Join(",",shop.Roles.Select(r=>r.Id+":"+shop.Available(r.Id)));
        public static int[] Lineup(ManualBattleController c)=>c.Battle.Cards.Select(card=>
            c.Shop.Bench.Single(p=>p.Id==card.Settings.CardId&&c.Shop.IsLineupInstance(p.Instance)).Instance).ToArray();
        public static bool Drag(ManualBattleController c,string id,string routeId,out string entity)
        {
            entity=null;var piece=c.Shop.Bench.FirstOrDefault(p=>p.Id==id&&c.Shop.IsLineupInstance(p.Instance));
            var lane=c.Battle.Lanes.FirstOrDefault(l=>l.Id==routeId);if(piece==null||lane==null||Camera.main==null)return false;
            int index=c.Shop.Bench.ToList().FindIndex(p=>p.Instance==piece.Instance);var before=new HashSet<string>(c.Battle.Units.Select(u=>u.EntityId));
            Rect origin=c.ShopPieceRect(index);var target=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(lane.Entry));
            if(c.Battle.WavesEnabled)
            {
                if(routeId!=c.Battle.CurrentWaveLaneId)return false;
                c.ProcessShopPointer(EventType.MouseDown,origin.center);c.ProcessShopPointer(EventType.MouseUp,origin.center);
            }
            else {c.ProcessShopPointer(EventType.MouseDown,origin.center);c.ProcessShopPointer(EventType.MouseDrag,target);c.ProcessShopPointer(EventType.MouseUp,target);}
            var added=c.Battle.Units.Where(u=>!before.Contains(u.EntityId)&&!u.SynergySupport&&!u.Golem&&u.CardId==id).ToArray();
            if(added.Length==1){entity=added[0].EntityId;return true;}
            Debug.Log("JOURNEY_DRAG_REJECTED: "+id+" route="+routeId+" phase="+c.Battle.Phase+" loop="+c.Loop.Phase+" dp="+c.Battle.DP+
                " overlay="+c.CampaignOverlayVisible+" origin="+origin+" target="+target+" failure="+c.Battle.CheckCardDeployment(id,lane.Entry,true)+" feedback="+c.ShopFeedback);return false;
        }
        public static IEnumerator FirstCombat(Action<ManualBattleController> receive)
        {
            ManualBattleController c=null;yield return Ready(1,value=>c=value);
            c.ClaimCampaignSupply();if(!c.OpenLoopShop())throw new InvalidOperationException("Initial supply did not open first combat.");
            yield return Ready(2,receive);
        }
    }

    // JsonUtility-compatible copy of the Editor search protocol. It deliberately
    // has no Editor dependency, so the same legal command witness can run in Player.
    [Serializable]public sealed class CampaignJourneyReplayReport
    {public string version,status;public string[] roleOrder;public float observationSeconds;public CampaignJourneyReplayAttempt[] attempts;}
    [Serializable]public sealed class CampaignJourneyReplayAttempt
    {public int seed,completedStages,failedStage;public string economyPolicy,gift,error;public string[] gifts;public bool won;public CampaignJourneyReplayRound[] rounds;}
    [Serializable]public sealed class CampaignJourneyReplayRound
    {
        public int stage,openingGold,openingLevel,closingGold,closingLevel,closingXP,baseReward,interest,streakReward,experienceReward,golemXP;
        public bool supply,won;public string[] initialOffers;public CampaignJourneyReplayPiece[] lineup;public CampaignJourneyReplayShopCommand[] shopCommands;public CampaignJourneyReplayFight fight;
        public string sceneName,mapId,encounterId,contractCard,infectionPrimary,infectionSecondary,marinePrimary,marineSecondary;public bool reunionRanged;
    }
    [Serializable]public sealed class CampaignJourneyReplayPiece {public string id;public int instance,star;}
    [Serializable]public sealed class CampaignJourneyReplayShopCommand
    {public string kind,id,primaryForm,secondaryForm;public int slot=-1,instance,goldBefore,goldAfter,levelAfter,xpAfter;public int[] lineupInstances;public string[] lineupIds;public bool locked;}
    [Serializable]public sealed class CampaignJourneyReplayFight
    {public bool won,timedObservation;public string phase,policy;public double seconds;public int remainingDefense,dp,breaches,deployments,portalTrips;public float damage;public CampaignJourneyReplayBattleAction[] actions;}
    [Serializable]public sealed class CampaignJourneyReplayBattleAction
    {public string kind,cardId,routeId,entityId;public long request;public double at;}

    public sealed class CampaignJourneySmokeDriver:MonoBehaviour
    {
        [Serializable]public sealed class Check {public string name,detail;public bool passed;}
        [Serializable]public sealed class RoundResult
        {public int stage,deployments,remainingDefense,closingGold,closingLevel,closingXP,portalTrips,breaches,golemXP,healthLoss,health;public string scene,mapId,outcome;public double seconds,settlementPresentationSeconds;public float damage;public bool supply,passed,settlementVisible;public ActionResult[] actions;}
        [Serializable]public sealed class ActionResult
        {public string kind,cardId,routeId,modelEntity,actualEntity;public double modelAt,actualAt;public bool passed;}
        [Serializable]public sealed class Result
        {
            public bool passed,naturalJourneyCompleted;public int seed=44,width,height,completedStages,combatScenes;public string mode="preparation-smoke",error,replayFile,modelPolicy;
            public string verificationScope="Production BeginJourney, authored scenes, true finite Shop transactions, pointer deployment and public skill input. Default form prompts are accepted programmatically through the production close API; no OS mouse input is claimed. No HP/damage injection or forced battle outcome. A short preparation smoke is distinct from a naturally completed twelve-stage replay.";
            public Check[] checks;public RoundResult[] rounds;
        }
        [Serializable]sealed class SavedFixtureReport {public SavedFixtureCase[] cases;}
        [Serializable]sealed class SavedFixtureCase
        {public string name;public int stage,formationLevel;public CampaignJourneyReplayPiece[] lineup;public CampaignJourneyReplayFight[] fights;}
        static bool started;
        public static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-campaignJourneySmokeTest");
        public static string Argument(string name,string fallback=null)
        {var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,name);return index>=0&&index+1<args.Length?args[index+1]:fallback;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Prepare(){if(Requested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Begin()
        {if(started||!Requested||!Debug.isDebugBuild)return;started=true;var host=new GameObject("Campaign journey verification");DontDestroyOnLoad(host);host.AddComponent<CampaignJourneySmokeDriver>();}
        readonly List<Check> checks=new List<Check>();readonly List<RoundResult> rounds=new List<RoundResult>();
        Result result;string directory;ManualBattleController controller;
        long auxiliaryRequest=1L<<50;
        bool fixtureBattleCaptured;
        void CheckResult(string name,bool passed,string detail=null)
        {checks.Add(new Check{name=name,passed=passed,detail=detail});Debug.Log("CAMPAIGN_JOURNEY_CHECK "+name+"="+passed+(detail==null?"":"; "+detail));}
        void Require(string name,bool passed,string detail=null)
        {CheckResult(name,passed,detail);if(!passed)throw new InvalidOperationException(name+(detail==null?"":"; "+detail));}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null)throw new InvalidOperationException("Visible Player frame unavailable: "+name);
            try
            {
                var pixels=frame.GetPixels32();int bright=0,samples=0;byte lo=255,hi=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096)){byte v=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));lo=Math.Min(lo,v);hi=Math.Max(hi,v);samples++;if(v>24)bright++;}
                CheckResult("visible-frame-"+name,frame.width>0&&frame.height>0&&bright>samples/20&&hi-lo>12);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),frame.EncodeToPNG());
            }
            finally{Destroy(frame);}
            yield return null;
        }
        IEnumerator ShortFlow()
        {
            yield return CampaignJourneyEvidenceProbe.InitialHost(c=>controller=c);controller.BeginJourney(44);
            yield return CampaignJourneyEvidenceProbe.Ready(1,c=>controller=c);
            var shop=controller.Shop;var loop=controller.Loop;
            Require("fresh-challenge-defaults-to-muted-one-times",controller.Muted&&controller.SimulationSpeed==1&&controller.CombatView.GetComponent<AudioSource>().mute);
            Require("ordinary-three-low-cost-distinct-gifts",CampaignJourneySession.Current.Seed==44&&shop.Level==1&&shop.Gold==CampaignJourneyEconomy.InitialGold&&shop.JourneyEconomy&&!shop.UnlimitedGold&&shop.Bench.Count==3&&shop.Bench.Select(p=>p.Id).Distinct().Count()==3&&shop.Bench.All(p=>p.Star==1&&shop.Definition(p.Id).Cost<=2));
            Require("initial-reward-opens-real-shop-without-silver-wolf",loop.CanShop&&loop.TotalStages==12&&shop.Offers.Any(id=>id!=null)&&shop.Roles.All(r=>r.Id!=SilverWolfCombat.CardId));
            Require("supply-has-no-defenders-or-simulated-clock",controller.Battle.Defenders.Count==0&&controller.Battle.Clock==0&&controller.Battle.CampaignV2Enabled);
            yield return Capture("00-initial-reward-three-gifts");
            var reserve=shop.Bench.First(p=>!shop.IsLineupInstance(p.Instance));
            int index=shop.Bench.ToList().FindIndex(p=>p.Instance==reserve.Instance);string economy=CampaignJourneyEvidenceProbe.State(shop);
            var origin=controller.ShopPieceRect(index);var target=ManualBattleController.ShopLineupRect(0);
            controller.ProcessShopPointer(EventType.MouseDown,origin.center);controller.ProcessShopPointer(EventType.MouseDrag,target.center);controller.ProcessShopPointer(EventType.MouseUp,target.center);
            yield return CampaignJourneyEvidenceProbe.Ready(1,c=>controller=c);
            Require("reward-pointer-swap-preserves-stock-and-money",shop.IsLineupInstance(reserve.Instance)&&CampaignJourneyEvidenceProbe.State(shop)==economy&&CampaignJourneyEvidenceProbe.StockConserved(shop)&&controller.Battle.Units.Count==0);
            yield return Capture("00-reward-lineup-swapped");controller.ToggleSpeed();controller.ClaimCampaignSupply();int gold=shop.Gold,xp=shop.XP;controller.ClaimCampaignSupply();
            Require("reward-receipt-is-idempotent",controller.Loop.Phase==BattleShopPhase.Settlement&&shop.Gold==gold&&shop.XP==xp&&CampaignJourneySession.Current.CompletedRounds.Count==1);
            Require("advance-reward-to-first-combat",controller.OpenLoopShop());yield return CampaignJourneyEvidenceProbe.Ready(2,c=>controller=c);
            Require("shop-and-run-survive-real-scene-change",ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop));
            Require("cross-stage-keeps-two-times-and-muted-sound",controller.SimulationSpeed==2&&controller.Muted);
            controller.ToggleMute();Require("sound-can-be-enabled-manually",!controller.Muted);
            controller.ToggleMute();Require("verification-restores-quiet-sound-setting",controller.Muted);
            yield return ShopInterfaceProbe("early");
            int slot=Enumerable.Range(0,5).First(i=>shop.Offers[i]!=null);var purchase=controller.PurchaseShopCard(slot);Require("real-shop-purchase",purchase.Success,purchase.Message);
            yield return CampaignJourneyEvidenceProbe.Ready(2,c=>controller=c);
            var commission=controller.CommissionCampaignCard(purchase.Id);Require("real-finite-commission",commission.Success&&shop.CommissionUsed,commission.Message);
            Require("real-experience-purchase",controller.UpgradeJourneyShop().Success);yield return CampaignJourneyEvidenceProbe.Ready(2,c=>controller=c);
            Require("production-shop-lock",controller.LockJourneyShop(true)&&shop.Locked);
            Require("normal-first-combat-v2-values",controller.Battle.WavesEnabled&&controller.Battle.DP==10&&controller.Battle.Defense==1&&controller.Battle.Defenders.Count==1&&controller.Battle.Cards.All(c=>c.Settings.Cooldown==WaveDeploymentCadence.ForCard(c.Settings.CardId,shop.Definition(c.Settings.CardId).Cost)));
            Require("finite-stock-is-conserved",CampaignJourneyEvidenceProbe.StockConserved(shop));yield return Capture("01-first-combat-preparation");
            Require("prepare-first-combat",controller.StartLoopNextBattle());yield return CampaignJourneyEvidenceProbe.Ready(2,c=>controller=c);
            string card=controller.Battle.Cards[0].Settings.CardId;Require("production-pointer-deployment",CampaignJourneyEvidenceProbe.Drag(controller,card,controller.Battle.CurrentWaveLaneId,out _));
            controller.Battle.SetPaused(true);double clock=controller.Battle.Clock;double cd=controller.Battle.CardCooldown(card);controller.Tick(5);
            Require("pause-freezes-combat-clock-and-deployment-cooldown",controller.Battle.Clock==clock&&controller.Battle.CardCooldown(card)==cd);
            yield return RouteGuideProbe();
            string before=CampaignJourneyEvidenceProbe.State(shop);Require("production-current-encounter-restart",controller.RestartCampaignEncounter());yield return CampaignJourneyEvidenceProbe.Ready(2,c=>controller=c);
            Require("restart-keeps-shop-and-seed-without-reroll",ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop)&&CampaignJourneyEvidenceProbe.State(shop)==before&&controller.Battle.Clock==0&&controller.Battle.Units.Count==0&&controller.Battle.DP==10&&CampaignJourneySession.Current.Seed==44);
            Require("restart-keeps-two-times-and-chosen-sound",controller.SimulationSpeed==2&&controller.Muted);
            yield return Capture("02-current-encounter-restarted");controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Require("exit-clears-session-and-restores-selector",CampaignJourneySession.Current==null&&SceneManager.GetActiveScene().name=="MonsterTrial"&&controller.TestSelectionOpen&&!controller.JourneyActive&&!controller.ShopTrialActive);
            yield return Capture("03-returned-to-selection");
        }
        IEnumerator RouteGuideProbe()
        {
            var battle=controller.Battle;
            controller.ProcessShopPointer(EventType.MouseMove,new Vector2(20,700));
            var initial=controller.WaveRouteGuideStrokes();
            Require("route-default-is-current-wave-white-line-and-flow",initial.Count>0&&initial.Any(s=>!s.Pulse)&&initial.Any(s=>s.Pulse)&&initial.All(s=>s.Open&&s.LaneId==battle.CurrentWaveLaneId));
            yield return Capture("route-default-paused");
            controller.Tick(3);var frozen=controller.WaveRouteGuideStrokes();
            Require("route-dashes-freeze-with-paused-battle",initial.Count==frozen.Count&&initial.Select((s,i)=>
                s.Projection.From==frozen[i].Projection.From&&s.Projection.To==frozen[i].Projection.To).All(same=>same));
            var other=battle.Lanes.First(l=>l.Id!=battle.CurrentWaveLaneId);
            controller.ProcessShopPointer(EventType.MouseMove,TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(other.Entry)));
            var preview=controller.WaveRouteGuideStrokes();
            Require("route-hover-replaces-instead-of-stacking",preview.Count>0&&preview.Any(s=>!s.Pulse)&&preview.Any(s=>s.Pulse)&&preview.All(s=>s.LaneId==other.Id&&!s.Open));
            yield return Capture("route-other-entry-hover");
            controller.ProcessShopPointer(EventType.MouseMove,new Vector2(20,700));
            Require("route-hover-leave-restores-current-wave",controller.WaveRouteGuideStrokes().All(s=>s.LaneId==battle.CurrentWaveLaneId));
            controller.Battle.SetPaused(false);
            int frames=Array.IndexOf(Environment.GetCommandLineArgs(),"-routeFlowFrames")>=0?28:1;
            for(int i=0;i<frames;i++)
            {
                if(frames>1)yield return Capture("route-flow-"+i.ToString("D2"));
                controller.Tick(frames>1?.1f:.25f);
            }
            var moved=controller.WaveRouteGuideStrokes();
            Require("route-dashes-flow-forward-on-production-clock",battle.Clock>0&&moved.Count>0&&
                (initial.Count!=moved.Count||initial.Select((s,i)=>Vector2.Distance(s.Projection.From,moved[i].Projection.From)>.01f||
                    Vector2.Distance(s.Projection.To,moved[i].Projection.To)>.01f).Any(different=>different)));
            battle.SetPaused(true);yield return Capture("route-flow-end-paused");
        }
        IEnumerator ShopInterfaceProbe(string suffix)
        {
            string before=CampaignJourneyEvidenceProbe.State(controller.Shop);double clock=controller.Battle.Clock;
            var lineup=CampaignJourneyEvidenceProbe.Lineup(controller);
            Require("shop-probability-real-entry-"+suffix,controller.OpenShopProbabilityDetails()&&controller.ShopProbabilityVisible);
            Require("shop-probability-ten-real-rows-"+suffix,Enumerable.Range(1,10).All(level=>ShopProbabilityUi.Baseline(level).Sum()==100));
            int index=controller.Shop.Bench.ToList().FindIndex(p=>controller.Shop.IsLineupInstance(p.Instance));
            var origin=controller.ShopPieceRect(index).center;var destination=ManualBattleController.ShopBenchRect(9).center;
            controller.ProcessShopPointer(EventType.MouseDown,origin);controller.ProcessShopPointer(EventType.MouseDrag,destination);controller.ProcessShopPointer(EventType.MouseUp,destination);
            Require("probability-modal-blocks-roster-input-"+suffix,lineup.SequenceEqual(CampaignJourneyEvidenceProbe.Lineup(controller))&&CampaignJourneyEvidenceProbe.State(controller.Shop)==before);
            yield return Capture("ui-"+suffix+"-all-level-probabilities");controller.CloseCampaignOverlay();
            var summaries=SynergySummary.From(controller.Battle.Cards.Select(c=>c.Settings.CardId),campaignV2:true);
            var summary=summaries.OrderByDescending(s=>s.Tier).ThenByDescending(s=>s.Count).First();
            Require("synergy-real-entry-"+suffix,controller.OpenSynergyDetails(summary.Name)&&controller.SynergyDetailsName==summary.Name);
            var detail=controller.CurrentSynergyDetail;
            Require("synergy-complete-members-and-current-tier-"+suffix,detail!=null&&!string.IsNullOrEmpty(detail.common)&&
                detail.members.Count(m=>m.selected)==summary.Members.Length&&detail.members.Any(m=>!m.selected)&&
                detail.members.All(m=>m.selected==summary.Members.Contains(m.id))&&
                detail.tiers.Count(t=>t.active)==(summary.Tier>0?1:0)&&detail.tiers.Where(t=>t.active).All(t=>t.tier==summary.Tier));
            yield return Capture("ui-"+suffix+"-synergy-members-and-tiers");controller.CloseCampaignOverlay();
            Require("information-overlays-do-not-mutate-economy-"+suffix,CampaignJourneyEvidenceProbe.State(controller.Shop)==before&&controller.Battle.Clock==clock&&!controller.CampaignOverlayVisible);
        }
        IEnumerator Run()
        {
            yield return ShortFlow();
            result.completedStages=2;
            string fixture=Argument("-campaignJourneyFixtureFile");
            if(!string.IsNullOrEmpty(fixture)){yield return SavedStageFixture(Path.GetFullPath(fixture));yield break;}
            string replay=Argument("-campaignJourneyReplayFile");
            if(!string.IsNullOrEmpty(replay))yield return Replay(Path.GetFullPath(replay));
        }
        IEnumerator WaitForSettlement(string key,RoundResult observed=null,float maximumPresentationSeconds=10)
        {
            var battle=controller.Battle;var shop=controller.Shop;var loop=controller.Loop;
            double clock=battle.Clock,presentation=controller.PresentationClock;string economy=CampaignJourneyEvidenceProbe.State(shop);
            var phase=battle.Phase;var loopPhase=loop.Phase;var reward=loop.Reward;
            int dp=battle.DP,defense=battle.Defense,breaches=battle.Breaches,receipts=CampaignJourneySession.Current.CompletedRounds.Count,health=loop.Health;
            float damage=battle.TotalPlayerDamage;
            Require(key+"-has-a-natural-terminal-result",phase==BattlePhase.Won||phase==BattlePhase.Lost);
            // Battle.Advance does not simulate a terminal result. Tick advances the
            // production death/skill tail and GUI presentation while gameplay stays fixed.
            if(controller.SimulationSpeed!=1)controller.ToggleSpeed();
            for(int frame=0;frame<Mathf.CeilToInt(maximumPresentationSeconds*20)&&!controller.SettlementVisible;frame++)
            {controller.Tick(.05f);yield return null;}
            yield return null;
            if(observed!=null){observed.settlementVisible=controller.SettlementVisible;observed.settlementPresentationSeconds=controller.PresentationClock-presentation;}
            Require(key+"-production-settlement-panel-visible",controller.SettlementVisible&&controller.PresentationClock-presentation<=maximumPresentationSeconds+.001);
            Require(key+"-presentation-keeps-clock-economy-and-result",ReferenceEquals(battle,controller.Battle)&&ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop)&&
                battle.Clock==clock&&CampaignJourneyEvidenceProbe.State(shop)==economy&&battle.Phase==phase&&loop.Phase==loopPhase&&loop.Health==health&&ReferenceEquals(loop.Reward,reward)&&
                battle.DP==dp&&battle.Defense==defense&&battle.Breaches==breaches&&battle.TotalPlayerDamage==damage&&CampaignJourneySession.Current.CompletedRounds.Count==receipts);
        }
        void SaveProgress()
        {result.checks=checks.ToArray();result.rounds=rounds.ToArray();File.WriteAllText(Path.Combine(directory,"journey-result.json"),JsonUtility.ToJson(result,true));}
        IEnumerator SavedStageFixture(string file)
        {
            Require("fixture-is-development-smoke-only",Debug.isDebugBuild&&Requested);
            result.mode="saved-stage-fixture";result.naturalJourneyCompleted=false;result.completedStages=result.combatScenes=0;
            result.replayFile=file;rounds.Clear();fixtureBattleCaptured=false;
            result.verificationScope="DEBUG saved-stage fixture: finite-stock Preset with zero gold and an explicit high-star lineup, followed by production pointer/skill actions and a natural battle result on the authored scene. This does not demonstrate natural card acquisition or a full twelve-stage clear. No health/damage injection, forced victory, free refresh or source-asset mutation. Existing opening-flow checks run first; no OS mouse input is claimed.";
            SaveProgress();Require("fixture-file-exists",File.Exists(file));
            string name=Argument("-campaignJourneyFixtureName","talulah-three-star-core-reference");
            int number=int.Parse(Argument("-campaignJourneyFixtureStage","12"),System.Globalization.CultureInfo.InvariantCulture);
            var report=JsonUtility.FromJson<SavedFixtureReport>(File.ReadAllText(file));
            var fixture=report?.cases?.SingleOrDefault(c=>c.name==name&&c.stage==number);
            Require("fixture-exact-saved-case-exists",fixture!=null);
            var fight=fixture.fights?.FirstOrDefault(f=>f.won&&!f.timedObservation&&f.actions!=null);
            Require("fixture-has-natural-winning-action-trace",fight!=null);
            Require("fixture-lineup-is-explicit-finite-stock",fixture.lineup!=null&&fixture.lineup.Length>0&&fixture.lineup.Length<=fixture.formationLevel&&
                fixture.lineup.Select(p=>p.id).Distinct().Count()==fixture.lineup.Length&&fixture.formationLevel>=1&&fixture.formationLevel<=10&&
                fixture.lineup.Select(p=>p.instance).SequenceEqual(Enumerable.Range(1,fixture.lineup.Length)));
            var definitions=controller.MonsterTests.Where(o=>o.Playable&&CampaignBalance.ShopEligible(o.Id)).Select(o=>o.Definition).ToArray();
            var shop=new ShopTrialModel(definitions.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),73191);
            shop.EnableJourneyEconomy();
            shop.Preset(fixture.lineup.Select(p=>new ShopTrialModel.Piece{Id=p.id,Star=p.star}),Array.Empty<string>(),fixture.formationLevel,0,0);
            Require("fixture-real-inventory-reserves-stock",shop.SetLineup(fixture.lineup.Select(p=>p.id),shop.Bench.Select(p=>p.Instance))&&CampaignJourneyEvidenceProbe.StockConserved(shop));
            var session=CampaignJourneySession.Begin(73191);var loop=new BattleShopRun(shop,73191,campaign:true,journeyV2:true);
            typeof(BattleShopRun).GetProperty(nameof(BattleShopRun.Stage)).GetSetMethod(true).Invoke(loop,new object[]{number});
            session.Shop=shop;session.Loop=loop;session.Lineup.AddRange(fixture.lineup.Select(p=>p.id));
            session.Contract=shop.Bench.Where(p=>CampaignBalance.Tags(p.Id).Contains("萨卡兹")).OrderByDescending(p=>p.Star).FirstOrDefault()?.Id;
            session.Commission=fixture.lineup[0].id;
            var stage=CampaignJourneyCatalog.Stage(number);
            Require("fixture-target-is-authored-combat",!stage.IsSupply&&stage.encounter!=null);
            string source=JsonUtility.ToJson(CampaignJourneyCatalog.Current),encounter=JsonUtility.ToJson(stage.encounter),waves=JsonUtility.ToJson(stage.encounter.attackWaves);
            SceneManager.LoadScene(session.Map.sceneName);yield return CampaignJourneyEvidenceProbe.Ready(number,c=>controller=c);
            result.seed=73191;result.modelPolicy=name+" / "+fight.policy;result.combatScenes=1;
            var observed=new RoundResult{stage=number,supply=false,scene=SceneManager.GetActiveScene().name,mapId=controller.JourneyMap.id};rounds.Add(observed);SaveProgress();
            Require("fixture-loads-one-real-scene-without-free-shop",ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop)&&!loop.CanShop&&
                shop.Gold==0&&shop.Offers.All(id=>id==null)&&controller.Battle.Phase==BattlePhase.Ready&&controller.Muted&&controller.SimulationSpeed==1);
            Require("fixture-preserves-exact-star-and-lineup-order",CampaignJourneyEvidenceProbe.Lineup(controller).SequenceEqual(fixture.lineup.Select(p=>p.instance))&&
                fixture.lineup.All(p=>shop.Bench.Any(b=>b.Id==p.id&&b.Instance==p.instance&&b.Star==p.star)));
            Require("fixture-has-twelve-actual-strengthened-defenders",controller.Battle.Defenders.Count==12&&stage.encounter.defenders.Length==12&&
                stage.encounter.defenders.All(g=>controller.Battle.FindDefender(g.id)?.MaxHealth==g.health&&controller.Battle.FindDefender(g.id)?.Settings.Attack==g.attack&&controller.Battle.FindDefender(g.id)?.Position==g.position));
            Require("fixture-uses-authored-low-dp-and-production-costs",controller.Battle.WavesEnabled&&controller.Battle.DP==stage.encounter.WaveSnapshot().Waves[0].DP&&
                controller.Battle.Cards.All(c=>c.Settings.DeploymentCost==CampaignV2Balance.StatValue(c.Settings.CardId,shop.Bench.Single(p=>p.Id==c.Settings.CardId).Star,"dp")&&
                    c.Settings.Cooldown==WaveDeploymentCadence.ForCard(c.Settings.CardId,shop.Definition(c.Settings.CardId).Cost)));
            Require("fixture-contains-actual-three-star-talulah",shop.Bench.Any(p=>p.Id=="CARD_TALULAH_TRIAL"&&p.Star==3));
            var reunion=SynergySummary.From(fixture.lineup.Select(p=>p.id),campaignV2:true).Single(s=>s.Name=="整合运动");
            Require("fixture-reunion-eight-member-bonus-description",controller.Battle.ReunionTier==8&&reunion.Effect.Contains("最大生命/攻击＋40%"));
            yield return Capture("fixture-stage-"+number+"-prepared");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ManualBattleController).GetField("campaignConstructTab",flags).SetValue(controller,4);
            typeof(ManualBattleController).GetField("campaignConstructOpen",flags).SetValue(controller,true);
            yield return Capture("fixture-stage-"+number+"-defender-list-top");
            typeof(ManualBattleController).GetField("journeyGuardScroll",flags).SetValue(controller,new Vector2(0,10000));
            yield return Capture("fixture-stage-"+number+"-defender-list-bottom");controller.CloseCampaignOverlay();
            var trace=new CampaignJourneyReplayRound{stage=number,won=true,lineup=fixture.lineup,fight=fight};
            yield return Fight(trace,observed);
            Require("fixture-resolves-through-real-victory-and-reward",controller.Battle.Phase==BattlePhase.Won&&loop.LastBattleWon&&session.CompletedRounds.Count==1&&
                loop.Reward!=null&&loop.Reward.Base==stage.baseGold&&loop.Reward.Interest==0&&CampaignJourneyEvidenceProbe.StockConserved(shop));
            observed.closingGold=shop.Gold;observed.closingLevel=shop.Level;observed.closingXP=shop.XP;observed.health=loop.Health;observed.healthLoss=loop.LastHealthLoss;observed.golemXP=loop.GolemExperience;
            yield return WaitForSettlement("fixture-stage-"+number,observed,60);
            Require("fixture-source-configuration-remains-unchanged",JsonUtility.ToJson(CampaignJourneyCatalog.Current)==source&&JsonUtility.ToJson(stage.encounter)==encounter&&JsonUtility.ToJson(stage.encounter.attackWaves)==waves);
            observed.passed=true;result.completedStages=1;SaveProgress();yield return Capture("fixture-stage-"+number+"-natural-settlement");
            controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
        }
        int SeatOf(int instance)
        {
            int bench=controller.Shop.Bench.ToList().FindIndex(p=>p.Instance==instance);if(bench<0)return -1;
            Rect rect=controller.ShopPieceRect(bench);
            for(int i=0;i<10;i++)if(rect==ManualBattleController.ShopLineupRect(i))return i;
            return -1;
        }
        IEnumerator SetLineup(int stage,int[] instances)
        {
            instances??=Array.Empty<int>();
            Require("stage-"+stage+"-lineup-request-valid",instances.Length>0&&instances.Length<=controller.Shop.Level&&instances.Distinct().Count()==instances.Length&&instances.All(id=>controller.Shop.Bench.Any(p=>p.Instance==id)));
            if(CampaignJourneyEvidenceProbe.Lineup(controller).SequenceEqual(instances))yield break;
            Require("stage-"+stage+"-lineup-edit-in-preparation",controller.Loop.CanShop&&controller.Battle.Phase==BattlePhase.Ready);
            // Replace a weaker copy by directly swapping with the same-name seat.
            // All moves use the ordinary seat transaction and its capacity checks.
            foreach(int instance in instances)
            {
                var wanted=controller.Shop.Bench.Single(p=>p.Instance==instance);
                var other=controller.Shop.Bench.FirstOrDefault(p=>p.Id==wanted.Id&&p.Instance!=instance&&controller.Shop.IsLineupInstance(p.Instance));
                if(other==null)continue;
                int seat=SeatOf(other.Instance);Require("stage-"+stage+"-same-name-seat-swap",seat>=0&&controller.MoveShopCard(instance,true,seat));
                yield return CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);
            }
            for(int i=0;i<instances.Length;i++)
            {
                Require("stage-"+stage+"-move-instance-"+instances[i],controller.MoveShopCard(instances[i],true,i),controller.ShopFeedback);
                yield return CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);
            }
            foreach(var extra in controller.Shop.Bench.Where(p=>controller.Shop.IsLineupInstance(p.Instance)&&!instances.Contains(p.Instance)).ToArray())
            {
                int free=Enumerable.Range(0,10).Where(i=>!controller.Shop.Bench.Select((p,j)=>controller.ShopPieceRect(j)).Contains(ManualBattleController.ShopBenchRect(i))).DefaultIfEmpty(-1).First();
                Require("stage-"+stage+"-move-unused-instance-"+extra.Instance,free>=0&&controller.MoveShopCard(extra.Instance,false,free));
                yield return CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);
            }
            Require("stage-"+stage+"-exact-lineup-order",CampaignJourneyEvidenceProbe.Lineup(controller).SequenceEqual(instances));
        }
        IEnumerator ShopCommand(int stage,int index,CampaignJourneyReplayShopCommand command)
        {
            yield return CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);var shop=controller.Shop;
            string key="stage-"+stage+"-shop-"+index+"-"+command.kind;
            Require(key+"-opening-gold",shop.Gold==command.goldBefore,"actual="+shop.Gold+" expected="+command.goldBefore);
            ShopTrialModel.Result transaction=null;bool success=false;
            switch(command.kind)
            {
                case "Upgrade":transaction=controller.UpgradeJourneyShop();success=transaction.Success;break;
                case "Refresh":transaction=controller.RefreshJourneyShop();success=transaction.Success;break;
                case "Purchase":
                    Require(key+"-expected-offer",command.slot>=0&&command.slot<5&&shop.Offers[command.slot]==command.id);
                    transaction=controller.PurchaseShopCard(command.slot);success=transaction.Success;break;
                case "Commission":transaction=controller.CommissionCampaignCard(command.id);success=transaction.Success;break;
                case "Sell":transaction=controller.SellShopCard(command.instance);success=transaction.Success;break;
                case "SetLineup":yield return SetLineup(stage,command.lineupInstances);success=true;break;
                case "SetContract":success=controller.TryAssignSarkazContract(command.id);break;
                case "SetForms":success=Enum.TryParse<InfectionForm>(command.primaryForm,out var primary)&&Enum.TryParse<InfectionForm>(command.secondaryForm,out var secondary)&&controller.SetCampaignForms(primary,secondary);break;
                case "Lock":success=controller.LockJourneyShop(command.locked);break;
                default:throw new InvalidOperationException("Unsupported replay shop command: "+command.kind);
            }
            Require(key+"-accepted",success,transaction?.Message??controller.ShopFeedback);
            if(transaction!=null&&command.instance>0)Require(key+"-instance-identity",transaction.Instance==command.instance);
            yield return CampaignJourneyEvidenceProbe.Ready(stage,c=>controller=c);
            Require(key+"-exact-economy-and-stock",shop.Gold==command.goldAfter&&shop.Level==command.levelAfter&&shop.XP==command.xpAfter&&CampaignJourneyEvidenceProbe.StockConserved(shop),
                "actual="+shop.Gold+"/"+shop.Level+"/"+shop.XP+" expected="+command.goldAfter+"/"+command.levelAfter+"/"+command.xpAfter);
        }
        IEnumerator RestartProbe(CampaignJourneyReplayRound trace)
        {
            var first=(trace.fight.actions??Array.Empty<CampaignJourneyReplayBattleAction>()).FirstOrDefault(a=>a.kind=="Deploy");
            Require("replay-restart-probe-has-real-first-deployment",first!=null);
            var shop=controller.Shop;var loop=controller.Loop;string state=CampaignJourneyEvidenceProbe.State(shop);
            var tiles=new HashSet<ApprovedMapTile>(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None));
            Require("replay-preflight-pointer-deployment",CampaignJourneyEvidenceProbe.Drag(controller,first.cardId,first.routeId,out _));
            var battle=controller.Battle;battle.SetPaused(true);double clock=battle.Clock;
            var units=battle.Units.ToArray();var positions=units.Select(u=>u.Motion.Position).ToArray();var health=units.Select(u=>u.Health).ToArray();var sp=units.Select(u=>u.SkillSP).ToArray();
            var cd=battle.Cards.Select(c=>battle.CardCooldown(c.Settings.CardId)).ToArray();controller.Tick(5);
            Require("replay-pause-freezes-all-entities-and-cooldowns",battle.Clock==clock&&units.Select((u,i)=>u.Motion.Position==positions[i]&&u.Health==health[i]&&u.SkillSP==sp[i]).All(v=>v)&&
                battle.Cards.Select((c,i)=>battle.CardCooldown(c.Settings.CardId)==cd[i]).All(v=>v));
            yield return Capture("stage-03-replay-pause-probe");Require("replay-production-current-encounter-restart",controller.RestartCampaignEncounter());
            yield return CampaignJourneyEvidenceProbe.Ready(trace.stage,c=>controller=c);
            Require("replay-restart-preserves-seeded-shop-without-reroll",ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop)&&CampaignJourneyEvidenceProbe.State(shop)==state&&
                controller.Battle.Clock==0&&controller.Battle.Units.Count==0&&controller.Battle.DP==CampaignJourneyCatalog.Stage(trace.stage).encounter.WaveSnapshot().Waves[0].DP&&tiles.SetEquals(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None)));
        }
        bool GolemPointer(string routeId,out string entity)
        {
            entity=null;var lane=controller.Battle.Lanes.FirstOrDefault(l=>l.Id==routeId);if(lane==null||Camera.main==null)return false;
            var before=new HashSet<string>(controller.Battle.Units.Select(u=>u.EntityId));var target=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(lane.Entry));
            controller.ProcessShopPointer(EventType.MouseDown,ManualBattleController.GolemCardRect.center);
            if(controller.Battle.WavesEnabled)controller.ProcessShopPointer(EventType.MouseUp,ManualBattleController.GolemCardRect.center);
            else {controller.ProcessShopPointer(EventType.MouseDrag,target);controller.ProcessShopPointer(EventType.MouseUp,target);}
            var added=controller.Battle.Units.Where(u=>!before.Contains(u.EntityId)&&u.Golem).ToArray();if(added.Length!=1)return false;entity=added[0].EntityId;return true;
        }
        IEnumerator Fight(CampaignJourneyReplayRound trace,RoundResult observed)
        {
            var battle=controller.Battle;var actions=trace.fight.actions??Array.Empty<CampaignJourneyReplayBattleAction>();
            var identities=new Dictionary<string,string>(StringComparer.Ordinal);var actual=new List<ActionResult>();int next=0,step=0;
            if(controller.SimulationSpeed!=1)controller.ToggleSpeed();battle.SetPaused(false);
            // A diagnostic restart recreates Ready. Use the same public start
            // command before replaying a legitimate delayed first wave action.
            if(battle.Phase==BattlePhase.Ready&&battle.WavesEnabled)controller.StartBattle();
            int maximumSteps=(int)Math.Ceiling((trace.fight.seconds+1)*20)+4;
            while((battle.Phase==BattlePhase.Ready||battle.Phase==BattlePhase.Running)&&step<maximumSteps)
            {
                while(next<actions.Length&&actions[next].at<=battle.Clock+.00001)
                {
                    var action=actions[next];var item=new ActionResult{kind=action.kind,cardId=action.cardId,routeId=action.routeId,modelEntity=action.entityId,modelAt=action.at,actualAt=battle.Clock};actual.Add(item);observed.actions=actual.ToArray();
                    Require("stage-"+trace.stage+"-action-"+next+"-exact-clock",Math.Abs(battle.Clock-action.at)<.001);
                    string entity=null;bool accepted=false;
                    switch(action.kind)
                    {
                        case "Deploy":accepted=CampaignJourneyEvidenceProbe.Drag(controller,action.cardId,action.routeId,out entity);break;
                        case "Golem":accepted=GolemPointer(action.routeId,out entity);break;
                        case "Skill":
                            Require("stage-"+trace.stage+"-skill-entity-was-really-deployed",!string.IsNullOrEmpty(action.entityId)&&identities.TryGetValue(action.entityId,out entity));
                            controller.SelectSkillUnit(entity);accepted=controller.ActivateSelectedSkill()==SkillFailure.None;controller.CloseSkillMenu();break;
                        case "DeepDetonate":accepted=battle.TryDetonateDeep(battle.RunId,++auxiliaryRequest);break;
                        default:throw new InvalidOperationException("Unsupported replay battle action: "+action.kind);
                    }
                    item.actualEntity=entity;item.passed=accepted;Require("stage-"+trace.stage+"-action-"+next+"-"+action.kind,accepted,controller.ShopFeedback);
                    if(action.kind=="Deploy"||action.kind=="Golem")
                    {
                        Require("stage-"+trace.stage+"-deployed-entity-route",battle.FindUnit(entity)?.RouteId==action.routeId&&
                            (action.kind=="Golem"||battle.FindUnit(entity)?.CardId==action.cardId));identities.Add(action.entityId,entity);
                        if(result.mode=="saved-stage-fixture"&&action.cardId=="CARD_TALULAH_TRIAL")
                        {
                            var talulah=battle.FindUnit(entity);
                            float bonus=CampaignV2Balance.SynergyValue("reunion",battle.ReunionTier,"memberStatBonus");
                            float baseHealth=talulah.Profile?.Unit.Health??battle.Settings.Health;
                            Require("fixture-deployed-talulah-receives-reunion-health-and-attack",Math.Abs(talulah.MaxHealth-baseHealth*(1+bonus))<.01f&&battle.UnitAttackMultiplier(talulah)+.0001f>=1+bonus);
                        }
                    }
                    next++;
                }
                Require("stage-"+trace.stage+"-production-combat-clock-started",battle.Phase!=BattlePhase.Ready,"The production wave start or an actual deployment must begin combat before replay advances the clock.");
                if(battle.Phase!=BattlePhase.Running)break;
                controller.Tick(.05f);step++;
                if(result.mode=="saved-stage-fixture"&&!fixtureBattleCaptured&&battle.Clock>=15&&battle.Phase==BattlePhase.Running)
                {fixtureBattleCaptured=true;yield return Capture("fixture-stage-"+trace.stage+"-talulah-fire-rain");}
                if(step%4==0)yield return null;
            }
            observed.actions=actual.ToArray();observed.outcome=battle.Phase.ToString();observed.seconds=battle.Clock;observed.deployments=battle.Units.Count(u=>!u.SynergySupport);
            observed.remainingDefense=battle.Defense;observed.damage=battle.TotalPlayerDamage;observed.portalTrips=battle.ApprovedPortalTrips;observed.breaches=battle.Breaches;
            Require("stage-"+trace.stage+"-all-recorded-actions-replayed",next==actions.Length);
            Require("stage-"+trace.stage+"-natural-recorded-outcome",battle.Phase==(trace.fight.won?BattlePhase.Won:BattlePhase.Lost)&&controller.Loop.Phase==(trace.stage==CampaignJourneyDefinition.StageCount?BattleShopPhase.Complete:BattleShopPhase.Settlement),
                "actual phase="+battle.Phase+" defense="+battle.Defense+" seconds="+battle.Clock+" damage="+battle.TotalPlayerDamage);
            Require("stage-"+trace.stage+"-matches-model-result",Math.Abs(battle.Clock-trace.fight.seconds)<.11&&battle.Defense==trace.fight.remainingDefense&&battle.DP==trace.fight.dp&&
                battle.Breaches==trace.fight.breaches&&observed.deployments==trace.fight.deployments&&battle.ApprovedPortalTrips==trace.fight.portalTrips&&Math.Abs(battle.TotalPlayerDamage-trace.fight.damage)<.2f);
        }
        IEnumerator Replay(string file)
        {
            result.mode="full-natural-replay";result.replayFile=file;result.completedStages=0;SaveProgress();
            Require("model-trace-file-exists",File.Exists(file));var report=JsonUtility.FromJson<CampaignJourneyReplayReport>(File.ReadAllText(file));
            var witness=report?.attempts?.FirstOrDefault(a=>a.won&&a.completedStages==CampaignJourneyDefinition.StageCount||a.failedStage==CampaignJourneyDefinition.StageCount&&a.rounds?.Length==CampaignJourneyDefinition.StageCount);
            Require("model-trace-has-a-complete-natural-witness",witness!=null);
            Require("model-trace-has-twelve-ordered-stages",witness.rounds!=null&&witness.rounds.Select(r=>r.stage).SequenceEqual(Enumerable.Range(1,CampaignJourneyDefinition.StageCount))&&witness.rounds.All(r=>r.fight!=null&&!r.fight.timedObservation&&(r.stage==CampaignJourneyDefinition.StageCount||r.won&&r.fight.won)));
            result.seed=witness.seed;result.modelPolicy=witness.economyPolicy;controller.BeginJourney(witness.seed);yield return CampaignJourneyEvidenceProbe.Ready(1,c=>controller=c);
            Require("model-role-order-equals-production-pool",report.roleOrder.SequenceEqual(controller.Shop.Roles.Select(r=>r.Id)));
            Require("seed-produces-the-same-random-gift",witness.gifts!=null&&controller.Shop.Bench.Select(p=>p.Id).SequenceEqual(witness.gifts));
            var shop=controller.Shop;var loop=controller.Loop;var scenes=new HashSet<string>(StringComparer.Ordinal);
            string source=JsonUtility.ToJson(CampaignJourneyCatalog.Current);
            var encounterSources=CampaignJourneyCatalog.Current.stages.Where(s=>!s.IsSupply).ToDictionary(s=>s.stage,s=>JsonUtility.ToJson(s.encounter));
            var waveSources=CampaignJourneyCatalog.Current.stages.Where(s=>!s.IsSupply).ToDictionary(s=>s.stage,s=>JsonUtility.ToJson(s.encounter.attackWaves));
            foreach(var trace in witness.rounds)
            {
                yield return CampaignJourneyEvidenceProbe.Ready(trace.stage,c=>controller=c);var stage=controller.JourneyStage;var map=controller.JourneyMap;
                var observed=new RoundResult{stage=trace.stage,supply=stage.IsSupply,scene=SceneManager.GetActiveScene().name,mapId=map.id};rounds.Add(observed);SaveProgress();
                Require("stage-"+trace.stage+"-ordinary-scene-and-session",ReferenceEquals(shop,controller.Shop)&&ReferenceEquals(loop,controller.Loop)&&observed.scene==trace.sceneName&&map.id==trace.mapId&&
                    controller.Battle.CampaignV2Enabled&&controller.Battle.CampaignV2Stage==trace.stage&&!controller.SynergySandbox&&!controller.ApprovedMapActive);
                Require("stage-"+trace.stage+"-saved-terrain-present",Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length==map.width*map.height);
                Require("stage-"+trace.stage+"-exact-opening-shop",shop.Gold==trace.openingGold&&shop.Level==trace.openingLevel&&trace.initialOffers!=null&&
                    shop.Offers.Count==trace.initialOffers.Length&&shop.Offers.Select(id=>id??string.Empty).SequenceEqual(trace.initialOffers.Select(id=>id??string.Empty)));
                var commands=trace.shopCommands??Array.Empty<CampaignJourneyReplayShopCommand>();
                for(int i=0;i<commands.Length;i++)yield return ShopCommand(trace.stage,i,commands[i]);
                var selected=CampaignJourneyEvidenceProbe.Lineup(controller);
                Require("stage-"+trace.stage+"-exact-purchased-lineup",selected.SequenceEqual(trace.lineup.Select(p=>p.instance))&&trace.lineup.All(p=>shop.Bench.Any(a=>a.Instance==p.instance&&a.Id==p.id&&a.Star==p.star)));
                Require("stage-"+trace.stage+"-normal-forms-and-contract",controller.Battle.InfectionShape.ToString()==trace.infectionPrimary&&controller.Battle.InfectionSecondShape.ToString()==trace.infectionSecondary&&
                    controller.Battle.Adaptation.ToString()==trace.marinePrimary&&controller.Battle.SecondAdaptation.ToString()==trace.marineSecondary&&
                    (controller.PreparedContractCard??"")==(trace.contractCard??""));
                Require("stage-"+trace.stage+"-authored-wave-budget-and-defense",controller.Battle.DP==(stage.IsSupply?0:stage.encounter.WaveSnapshot().Waves[0].DP)&&controller.Battle.Defense==stage.defense&&controller.Battle.WavesEnabled==!stage.IsSupply&&!controller.Battle.StrongholdMapEnabled);
                Require("stage-"+trace.stage+"-exact-authored-guards",controller.Battle.Defenders.Count==(stage.encounter?.defenders.Length??0)&&
                    (stage.encounter==null||stage.encounter.defenders.All(g=>controller.Battle.FindDefender(g.id)?.MaxHealth==g.health&&controller.Battle.FindDefender(g.id)?.Position==g.position)));
                if(trace.stage==CampaignJourneyDefinition.StageCount)yield return ShopInterfaceProbe("final");
                if(controller.Loop.CanShop)Require("stage-"+trace.stage+"-production-prepare",controller.StartLoopNextBattle());
                yield return CampaignJourneyEvidenceProbe.Ready(trace.stage,c=>controller=c);
                int goldBeforeResult=shop.Gold,xpBeforeResult=shop.XP,levelBeforeResult=shop.Level,healthBeforeResult=loop.Health;
                if(stage.IsSupply)
                {
                    controller.ClaimCampaignSupply();observed.outcome=controller.Battle.Phase.ToString();observed.seconds=controller.Battle.Clock;observed.remainingDefense=controller.Battle.Defense;
                    Require("stage-"+trace.stage+"-ordinary-noncombat-supply",controller.Battle.Phase==BattlePhase.Won&&controller.Battle.Units.Count==0&&controller.Battle.Clock==0&&controller.Battle.Defenders.Count==0);
                }
                else
                {
                    scenes.Add(observed.scene);result.combatScenes=scenes.Count;yield return Capture("stage-"+trace.stage.ToString("00")+"-prepared");
                    if(trace.stage==2)yield return RestartProbe(trace);
                    yield return Fight(trace,observed);
                }
                var reward=controller.Loop.Reward;observed.closingGold=shop.Gold;observed.closingLevel=shop.Level;observed.closingXP=shop.XP;observed.golemXP=loop.GolemExperience;
                if(trace.won)
                    Require("stage-"+trace.stage+"-exact-natural-reward-and-stock",reward!=null&&reward.Base==trace.baseReward&&reward.Interest==trace.interest&&reward.Streak==trace.streakReward&&reward.Experience==trace.experienceReward&&
                        shop.Gold==trace.closingGold&&shop.Level==trace.closingLevel&&shop.XP==trace.closingXP&&loop.GolemExperience==trace.golemXP&&CampaignJourneyEvidenceProbe.StockConserved(shop));
                else
                    Require("stage-"+trace.stage+"-surviving-setback-base-and-interest-without-streak-or-xp",reward!=null&&!reward.Won&&reward.Base==stage.baseGold&&reward.Interest==Math.Min(5,goldBeforeResult/10)*CampaignJourneyEconomy.RewardGoldMultiplier&&reward.Streak==0&&reward.Experience==0&&
                        shop.Gold==goldBeforeResult+stage.baseGold+Math.Min(5,goldBeforeResult/10)*CampaignJourneyEconomy.RewardGoldMultiplier&&shop.Level==levelBeforeResult&&shop.XP==xpBeforeResult&&shop.WinStreak==0&&CampaignJourneyEvidenceProbe.StockConserved(shop));
                observed.health=loop.Health;observed.healthLoss=loop.LastHealthLoss;
                int expectedLoss=trace.won?0:CampaignJourneyHealth.CalculateLoss(controller.Battle.Defense,controller.Battle.Settings.InitialDefense,stage.stage);
                Require("stage-"+trace.stage+"-health-matches-real-gate-progress",loop.LastBattleWon==trace.won&&loop.LastHealthLoss==expectedLoss&&loop.Health==healthBeforeResult-expectedLoss&&
                    CampaignJourneySession.Current.CompletedRounds.Last().healthLoss==expectedLoss&&CampaignJourneySession.Current.CompletedRounds.Last().remainingHealth==loop.Health);
                yield return WaitForSettlement("stage-"+trace.stage+"-settlement",observed);
                observed.passed=true;result.completedStages=trace.stage;SaveProgress();yield return Capture("stage-"+trace.stage.ToString("00")+"-natural-settlement");
                if(trace.stage<CampaignJourneyDefinition.StageCount)Require("stage-"+trace.stage+"-advance-production",controller.OpenLoopShop());
            }
            Require("twelve-stage-natural-completion",CampaignJourneySession.Current.Ended&&CampaignJourneySession.Current.Won&&loop.Phase==BattleShopPhase.Complete&&
                CampaignJourneySession.Current.CompletedRounds.Count==CampaignJourneyDefinition.StageCount&&CampaignJourneySession.Current.CompletedRounds.Count(r=>!r.supply)==8&&scenes.Count==8);
            Require("source-journey-and-encounters-unchanged",JsonUtility.ToJson(CampaignJourneyCatalog.Current)==source&&encounterSources.All(p=>JsonUtility.ToJson(CampaignJourneyCatalog.Stage(p.Key).encounter)==p.Value)&&waveSources.All(p=>JsonUtility.ToJson(CampaignJourneyCatalog.Stage(p.Key).encounter.attackWaves)==p.Value));
            result.naturalJourneyCompleted=true;yield return WaitForSettlement("twelve-stage-completion");yield return Capture("04-twelve-stage-natural-completion");
            controller.ExitJourney();yield return null;yield return CampaignJourneyEvidenceProbe.Host(c=>controller=c);yield return null;
            Require("completed-run-exit-clears-session",CampaignJourneySession.Current==null&&controller.TestSelectionOpen&&!controller.ShopTrialActive&&Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length==0);
            SaveProgress();
        }
        IEnumerator Start()
        {
            float previousScale=Time.timeScale;Time.timeScale=0;
            directory=Path.GetFullPath(Argument("-campaignJourneyEvidenceDir",Path.Combine(Application.persistentDataPath,"CampaignJourneyEvidence")));Directory.CreateDirectory(directory);
            result=new Result{width=Screen.width,height=Screen.height};var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null){result.error=failure.ToString();Debug.LogException(failure);while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            Time.timeScale=previousScale;result.checks=checks.ToArray();result.rounds=rounds.ToArray();result.passed=string.IsNullOrEmpty(result.error)&&checks.Count>0&&checks.All(c=>c.passed)&&(result.mode!="full-natural-replay"||result.naturalJourneyCompleted);
            File.WriteAllText(Path.Combine(directory,"journey-result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("CAMPAIGN_JOURNEY_RESULT: passed="+result.passed+" mode="+result.mode+" naturalJourneyCompleted="+result.naturalJourneyCompleted+" checks="+checks.Count);Application.Quit(result.passed?0:1);
        }
    }
}
