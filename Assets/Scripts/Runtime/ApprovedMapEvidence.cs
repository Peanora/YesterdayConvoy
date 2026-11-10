using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Shared input/wait helpers only. This probe never changes combat values or authored assets.
    public static class ApprovedMapEvidenceProbe
    {
        public static IEnumerator Ready(string id,Action<ManualBattleController> receive)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+240;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                var controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.ApprovedMapActive&&controller.ApprovedMap!=null&&controller.ApprovedMap.id==id&&
                    !controller.ArtLoading&&!controller.ShopPanelTransitioning&&controller.Loop!=null&&controller.Battle!=null)
                {
                    // Keep the ordinary form prompt in the product. The verification
                    // accepts its configured default through the same public close path
                    // as the UI; this is programmatic input, not an OS mouse click.
                    if(controller.CampaignOverlayVisible){controller.CloseCampaignOverlay();yield return null;}
                    receive(controller);yield return null;yield break;
                }
                yield return null;
            }
            throw new InvalidOperationException("Approved-map preparation timed out: "+id);
        }

        public static bool Drag(ManualBattleController controller,string cardId,Vector3 entry)
        {
            if(controller==null||controller.Shop==null||controller.Battle==null)return false;
            int index=controller.Shop.Bench.ToList().FindIndex(piece=>piece.Id==cardId);
            if(index<0||Camera.main==null){Debug.Log("APPROVED_MAP_DRAG_REJECTED: missing card/camera; card="+cardId+"; index="+index);return false;}
            var origin=controller.ShopPieceRect(index);
            if(origin.width<=0||origin.height<=0){Debug.Log("APPROVED_MAP_DRAG_REJECTED: empty card rect; card="+cardId+"; index="+index+"; rect="+origin);return false;}
            var target=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(entry));
            int before=controller.Battle.Units.Count(unit=>!unit.SynergySupport);
            controller.ProcessShopPointer(EventType.MouseDown,origin.center);
            controller.ProcessShopPointer(EventType.MouseDrag,target);
            controller.ProcessShopPointer(EventType.MouseUp,target);
            bool deployed=controller.Battle.Units.Count(unit=>!unit.SynergySupport)==before+1;
            if(!deployed)Debug.Log("APPROVED_MAP_DRAG_REJECTED: card="+cardId+"; phase="+controller.Battle.Phase+"; dp="+controller.Battle.DP+
                "; canDeploy="+(controller.Loop==null||controller.Loop.CanDeploy)+"; overlay="+controller.CampaignOverlayVisible+
                "; paused="+controller.Battle.IsPaused+"; cardRect="+origin+"; target="+target+"; feedback="+controller.ShopFeedback);
            return deployed;
        }

        public static bool StockConserved(ManualBattleController controller)=>controller.Shop.Roles.All(role=>
            controller.Shop.Available(role.Id)>=0&&controller.Shop.Available(role.Id)+controller.Shop.Offers.Count(id=>id==role.Id)+
            controller.Shop.Bench.Where(piece=>piece.Id==role.Id).Sum(piece=>ShopTrialModel.Copies(piece.Star))==controller.Shop.Capacity(role.Id));

        public static bool NormalProfiles(ManualBattleController controller)
        {
            foreach(var piece in controller.Shop.Bench)
            {
                var actual=controller.Battle.FindCard(piece.Id);if(actual==null)continue;
                var definition=Resources.Load<ManualBattleDefinition>("MonsterTests/"+piece.Id.Substring("CARD_".Length));
                if(definition==null)return false;
                var expected=CampaignBalance.Build(definition,piece.Star,50,6);
                if(actual.Settings.Health!=expected.Unit.Health||actual.Settings.DeploymentCost!=expected.Unit.DeploymentCost||
                    actual.Settings.Cooldown!=expected.Unit.Cooldown||actual.Settings.DeploymentLimit!=expected.Unit.DeploymentLimit)return false;
            }
            return true;
        }
    }

    public sealed class ApprovedMapSmokeDriver:MonoBehaviour
    {
        [Serializable]public sealed class Check {public string name;public bool passed;}
        [Serializable]public sealed class MapResult
        {
            public string id,title,scene,error,naturalOutcome;
            public bool passed,portalTransitObserved,pumpWarningObserved,pumpActiveObserved;
            public int savedTiles,lanes,defenders,deployments,portalTrips,breaches;
            public double observedSimulationSeconds;
            public float observedPlayerDamage,observedDefenderDamage,heatDamageToUnits,heatDamageToDefenders;
            public Check[] checks;
        }
        [Serializable]public sealed class Result
        {
            public bool passed;
            public string error;
            public string scope="Eight saved approved-map scenes, production controller pointer deployment, finite inventory, normal Campaign profiles, pause/restart and visible Player frames. The diagnostic programmatically accepts the configured default infection form through the public production close path; ordinary play retains its prompt. Observation uses the ordinary starting cards and legal skills, without injected HP/damage or forced outcomes; no claim of human mouse input, natural campaign progression or final map balance. Portal/heat correctness also has separate configuration/model tests; an observation not reached in this short policy is reported explicitly.";
            public int width,height;
            public MapResult[] maps;
            public Check[] checks;
        }
        [Serializable]public sealed class LaunchResult
        {
            public bool passed;
            public string route,expectedScene,actualScene,error;
            public string scope="Independent observation of ordinary -approvedMaps or -approvedMap N startup. This driver never opens the menu or loads a scene, and does not bypass CampaignBootstrap. Direct-map preparation may programmatically accept its configured default form through the production close API. Screenshots are actual Player frames; no OS mouse interaction is claimed.";
            public int requestedIndex,width,height;
            public double routingSeconds;
            public Check[] checks;
        }
        static bool started;
        public static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-approvedMapsSmokeTest");
        public static bool LaunchRequested=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-approvedMapLaunchSmokeTest");
        public static string EvidenceDirectory()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-approvedMapEvidenceDir");
            return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"ApprovedMapEvidence");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare(){if(Requested||LaunchRequested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(started||!Requested&&!LaunchRequested||!Debug.isDebugBuild)return;
            started=true;var host=new GameObject("Approved-map verification");DontDestroyOnLoad(host);host.AddComponent<ApprovedMapSmokeDriver>();
        }

        static IEnumerator Capture(string directory,string name,Action<string,bool> check)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null)throw new InvalidOperationException("A visible Player window is required for "+name);
            try
            {
                var pixels=frame.GetPixels32();int samples=0,bright=0;byte lo=255,hi=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {byte value=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));lo=Math.Min(lo,value);hi=Math.Max(hi,value);samples++;if(value>24)bright++;}
                check("visible-frame-"+name,frame.width>0&&frame.height>0&&bright>samples/20&&hi-lo>12);
                Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),frame.EncodeToPNG());
            }
            finally{Destroy(frame);}
            yield return null;
        }

        static IEnumerator OneMap(ApprovedMapDefinition map,string directory,MapResult result)
        {
            var checks=new List<Check>();bool valid=true;
            Action<string,bool> check=(name,ok)=>{checks.Add(new Check{name=name,passed=ok});valid&=ok;Debug.Log("APPROVED_MAP_CHECK "+map.id+"/"+name+"="+ok);result.checks=checks.ToArray();result.passed=valid;};
            yield return SceneManager.LoadSceneAsync(map.sceneName);
            ManualBattleController controller=null;yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            Time.timeScale=0;
            string sourceBefore=JsonUtility.ToJson(map),encounterBefore=JsonUtility.ToJson(map.encounter);
            var terrain=Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None);
            var terrainSet=new HashSet<ApprovedMapTile>(terrain);
            var field=Object.FindAnyObjectByType<AuthoredBattlefield>();var battle=controller.Battle;
            result.id=map.id;result.title=map.title;result.scene=SceneManager.GetActiveScene().name;
            result.savedTiles=terrain.Length;result.lanes=battle.Lanes.Count;result.defenders=battle.Defenders.Count;
            check("saved-independent-scene-and-bootstrap",result.scene==map.sceneName&&controller.ApprovedMap==map&&controller.ApprovedMapActive);
            check("saved-grid-without-physics-terrain",field!=null&&terrain.Length==map.width*map.height&&field.GetComponentsInChildren<Collider>(true).Length==0);
            check("normal-level-eight-free-preparation",controller.Loop.CanShop&&controller.Shop.Level==8&&controller.Shop.UnlimitedGold&&
                controller.Shop.Bench.Count==8&&ApprovedMapEvidenceProbe.StockConserved(controller));
            check("normal-cards-and-fifty-dp-six-defense",battle.DP==50&&battle.Defense==6&&ApprovedMapEvidenceProbe.NormalProfiles(controller));
            check("normal-configured-defenders",battle.Defenders.Count==map.encounter.defenders.Length&&map.encounter.defenders.All(saved=>
            {var live=battle.FindDefender(saved.id);return live!=null&&live.MaxHealth==saved.health&&live.Settings.Attack==saved.attack&&live.Position==saved.position;}));
            check("fixed-lanes-without-unrelated-map-rules",!battle.WavesEnabled&&!battle.StrongholdMapEnabled&&
                battle.Lanes.Count==map.encounter.lanes.Length&&battle.Lanes.Select(lane=>lane.Id+"/"+lane.ExitId).SequenceEqual(map.encounter.lanes.Select(lane=>lane.id+"/"+lane.exitId)));
            var camera=Camera.main;
            check("forward-sixty-degree-camera",camera!=null&&Math.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.x,60))<.1f&&
                Math.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y,0))<.1f&&Math.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.z,0))<.1f);
            check("red-gates-visible-in-map-safe-region",camera!=null&&battle.Lanes.All(lane=>
            {var screen=camera.WorldToScreenPoint(lane.Entry);return screen.z>0&&ManualBattleController.ShopMapSafeRect.Contains(TacticalViewport.Current.FromScreen(screen));}));
            check("configured-portal-and-pump-scope",battle.ApprovedHeatPumpCount==(map.index==5?3:0)&&
                map.mechanics.portals.Length==(map.index==4?1:0));
            yield return Capture(directory,"01-preparation",check);

            check("production-start-battle",controller.StartLoopNextBattle());yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            var pieces=controller.Shop.Bench.Where(piece=>controller.Battle.FindCard(piece.Id)!=null).ToArray();
            battle=controller.Battle;string first=pieces[0].Id;var card=battle.FindCard(first);int dp=battle.DP,uses=card.RemainingUses;
            check("normal-pointer-deployment",ApprovedMapEvidenceProbe.Drag(controller,first,battle.Lanes[0].Entry));
            check("deployment-charges-cost-and-use-once",battle.DP==dp-card.Settings.DeploymentCost&&card.RemainingUses==uses-1&&battle.Phase==BattlePhase.Running);
            battle.SetPaused(true);string second=pieces[1].Id;var other=battle.FindCard(second);dp=battle.DP;uses=other.RemainingUses;
            check("paused-pointer-deployment",ApprovedMapEvidenceProbe.Drag(controller,second,battle.Lanes[battle.Lanes.Count-1].Entry));
            check("paused-deployment-charges-immediately",battle.IsPaused&&battle.DP==dp-other.Settings.DeploymentCost&&other.RemainingUses==uses-1&&battle.CardCooldown(second)>0);
            var units=battle.Units.ToArray();var positions=units.Select(unit=>unit.Motion.Position).ToArray();
            var health=units.Select(unit=>unit.Health).ToArray();var sp=units.Select(unit=>unit.SkillSP).ToArray();
            var cooldowns=battle.Cards.Select(item=>battle.CardCooldown(item.Settings.CardId)).ToArray();
            var heat=Enumerable.Range(0,battle.ApprovedHeatPumpCount).Select(battle.ApprovedHeatState).ToArray();double clock=battle.Clock;
            controller.Tick(5);
            check("pause-freezes-movement-hp-sp-card-cd-and-heat-clock",battle.Clock==clock&&
                units.Select((unit,i)=>unit.Motion.Position==positions[i]&&unit.Health==health[i]&&unit.SkillSP==sp[i]).All(same=>same)&&
                battle.Cards.Select((item,i)=>battle.CardCooldown(item.Settings.CardId)==cooldowns[i]).All(same=>same)&&
                Enumerable.Range(0,battle.ApprovedHeatPumpCount).Select(battle.ApprovedHeatState).SequenceEqual(heat));
            dp=battle.DP;int count=battle.Units.Count;
            check("paused-repeat-keeps-original-card-cooldown",!ApprovedMapEvidenceProbe.Drag(controller,second,battle.Lanes[0].Entry)&&battle.DP==dp&&battle.Units.Count==count);
            yield return Capture(directory,"02-paused-deployment",check);

            check("encounter-restart-accepted",controller.RestartCampaignEncounter());yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            check("restart-resets-battle-and-keeps-saved-terrain",terrainSet.SetEquals(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None))&&
                controller.Battle.Units.Count==0&&controller.Battle.Clock==0&&controller.Battle.DP==50&&controller.Battle.Defense==6&&controller.Battle.ApprovedPortalTrips==0);
            if(controller.Loop.CanShop){check("restart-start-battle",controller.StartLoopNextBattle());yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);}
            battle=controller.Battle;pieces=controller.Shop.Bench.Where(piece=>battle.FindCard(piece.Id)!=null).ToArray();
            for(int i=0;i<pieces.Length;i++)
                check("ordinary-policy-deploy-"+pieces[i].Id,ApprovedMapEvidenceProbe.Drag(controller,pieces[i].Id,battle.Lanes[i%battle.Lanes.Count].Entry));
            battle.SetPaused(false);if(controller.SimulationSpeed!=1)controller.ToggleSpeed();
            bool capturedPortal=false,capturedHeat=false;
            int steps=map.index==4?1800:240;
            for(int step=0;step<steps&&battle.Phase==BattlePhase.Running;step++)
            {
                foreach(var unit in battle.Units.ToArray())if(unit.Active&&battle.CheckSkill(unit.EntityId)==SkillFailure.None)
                {controller.SelectSkillUnit(unit.EntityId);controller.ActivateSelectedSkill();controller.CloseSkillMenu();}
                controller.Tick(.05f);
                result.portalTransitObserved|=battle.Units.Any(unit=>battle.ApprovedInTransit(unit.EntityId));
                result.pumpWarningObserved|=Enumerable.Range(0,battle.ApprovedHeatPumpCount).Any(i=>battle.ApprovedHeatState(i)==1);
                result.pumpActiveObserved|=Enumerable.Range(0,battle.ApprovedHeatPumpCount).Any(i=>battle.ApprovedHeatState(i)==2);
                if(!capturedPortal&&result.portalTransitObserved)
                {
                    capturedPortal=true;var transit=battle.Units.FirstOrDefault(unit=>battle.ApprovedInTransit(unit.EntityId));
                    if(transit!=null)
                    {
                        battle.SetPaused(true);float remaining=battle.ApprovedTransitRemaining(transit.EntityId);double pausedAt=battle.Clock;
                        controller.Tick(5);check("actual-portal-countdown-freezes-on-pause",battle.Clock==pausedAt&&battle.ApprovedTransitRemaining(transit.EntityId)==remaining);
                        yield return Capture(directory,"03-actual-underground-transit",check);battle.SetPaused(false);
                    }
                }
                if(!capturedHeat&&result.pumpActiveObserved){capturedHeat=true;yield return Capture(directory,"03-actual-heat-active",check);}
                if(step%4==0)yield return null;
            }
            result.naturalOutcome=battle.Phase.ToString();result.observedSimulationSeconds=battle.Clock;result.portalTrips=battle.ApprovedPortalTrips;
            result.deployments=battle.Units.Count(unit=>!unit.SynergySupport);result.breaches=battle.Breaches;
            result.observedPlayerDamage=battle.CombatShots.Where(shot=>!shot.FromDefender&&shot.Applied).Sum(shot=>Math.Max(0,shot.DamageDealt));
            result.observedDefenderDamage=battle.CombatShots.Where(shot=>shot.FromDefender&&!shot.Healing&&shot.Applied).Sum(shot=>Math.Max(0,shot.DamageDealt));
            result.heatDamageToUnits=battle.ApprovedHeatDamageToUnits;result.heatDamageToDefenders=battle.ApprovedHeatDamageToDefenders;
            check("observed-units-retain-fixed-route-and-exit",battle.Units.Where(unit=>!unit.SynergySupport).All(unit=>battle.Lanes.Any(lane=>lane.Id==unit.RouteId&&lane.ExitId==unit.ExitId)));
            check("ordinary-policy-retains-finite-stock",ApprovedMapEvidenceProbe.StockConserved(controller));
            yield return Capture(directory,"04-natural-observation",check);
            check("source-assets-unchanged",JsonUtility.ToJson(map)==sourceBefore&&JsonUtility.ToJson(map.encounter)==encounterBefore);
            controller.RestartBattle();yield return ApprovedMapEvidenceProbe.Ready(map.id,c=>controller=c);
            check("full-restart-keeps-map-and-clears-runtime-state",controller.Loop.CanShop&&controller.Battle.Units.Count==0&&controller.Battle.Clock==0&&
                controller.Battle.ApprovedPortalTrips==0&&controller.Battle.DP==50&&controller.Battle.Defense==6&&
                terrainSet.SetEquals(Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None))&&ApprovedMapEvidenceProbe.StockConserved(controller));
            yield return Capture(directory,"05-restarted-preparation",check);
            controller.LeaveShopTrial();yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            check("scene-exit-removes-only-new-map-tiles",Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length==0);
            result.checks=checks.ToArray();result.passed=valid;
        }

        IEnumerator Run(Result result,string directory,List<MapResult> maps,List<Check> checks)
        {
            var definitions=ApprovedMapCatalog.All;
            checks.Add(new Check{name="eight-map-definitions",passed=definitions.Length==8});
            foreach(var map in definitions)
            {
                var item=new MapResult{id=map.id,title=map.title,scene=map.sceneName};maps.Add(item);
                yield return OneMap(map,Path.Combine(directory,map.id),item);
                result.maps=maps.ToArray();result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            }
            checks.Add(new Check{name="all-eight-scenes-observed",passed=maps.Count==8});
            Action<string,bool> selectorCheck=(name,ok)=>{checks.Add(new Check{name=name,passed=ok});Debug.Log("APPROVED_MAP_SELECTOR_CHECK "+name+"="+ok);};
            // OneMap returns through the ordinary MonsterTrial scene. Let its Start and
            // any queued scene work finish before opening the production selection page.
            for(int frame=0;frame<3;frame++)yield return null;
            var selector=Object.FindAnyObjectByType<ManualBattleController>();
            selectorCheck("normal-monster-selection-without-stale-map-state",SceneManager.GetActiveScene().name=="MonsterTrial"&&
                selector!=null&&selector.TestSelectionOpen&&!selector.ApprovedMapActive&&!selector.ShopTrialActive&&
                Object.FindAnyObjectByType<ApprovedMapBootstrap>()==null&&Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length==0);
            if(selector==null)throw new InvalidOperationException("Normal MonsterTrial controller was unavailable after map traversal.");
            var menuMaps=ApprovedMapCatalog.All;
            selectorCheck("all-eight-selector-profiles-and-loaded-previews",menuMaps.Length==8&&
                menuMaps.Select(map=>map.index).SequenceEqual(Enumerable.Range(1,8))&&menuMaps.Select(map=>map.id).Distinct().Count()==8&&
                menuMaps.All(map=>map.encounter!=null&&map.preview!=null&&map.preview.width>0&&map.preview.height>0&&
                    map.encounter.mapId==map.id&&ApprovedMapCatalog.Find(map.id)==map));
            selector.OpenApprovedMapMenu();yield return null;yield return null;
            selectorCheck("production-eight-map-menu-remains-open-without-redirect",selector.ApprovedMapMenuOpen&&selector.TestSelectionOpen&&
                !selector.ArtLoading&&SceneManager.GetActiveScene().name=="MonsterTrial");
            yield return Capture(directory,"00-eight-map-selector",selectorCheck);
        }

        IEnumerator Start()
        {
            if(LaunchRequested){yield return LaunchEvidence();yield break;}
            string directory=EvidenceDirectory();Directory.CreateDirectory(directory);
            var result=new Result{width=Screen.width,height=Screen.height};var maps=new List<MapResult>();var checks=new List<Check>();
            float previousScale=Time.timeScale;Time.timeScale=0;
            var stack=new Stack<IEnumerator>();stack.Push(Run(result,directory,maps,checks));
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    result.error=failure.ToString();if(maps.Count>0){maps[maps.Count-1].error=result.error;maps[maps.Count-1].passed=false;}
                    Debug.LogException(failure);while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            Time.timeScale=previousScale;result.maps=maps.ToArray();result.checks=checks.ToArray();
            result.passed=string.IsNullOrEmpty(result.error)&&maps.Count==8&&maps.All(map=>map.passed)&&checks.All(check=>check.passed);
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("APPROVED_MAPS_RESULT: passed="+result.passed+"; maps="+maps.Count+"; checks="+(checks.Count+maps.Sum(map=>map.checks?.Length??0))+"; noInjectedDamage=True; noForcedVictory=True");
            Application.Quit(result.passed?0:1);
        }

        IEnumerator LaunchChecks(LaunchResult result,string directory,List<Check> checks)
        {
            Action<string,bool> check=(name,ok)=>{checks.Add(new Check{name=name,passed=ok});Debug.Log("APPROVED_MAP_LAUNCH_CHECK "+name+"="+ok);};
            var args=Environment.GetCommandLineArgs();bool selector=Array.IndexOf(args,"-approvedMaps")>=0;
            int at=Array.IndexOf(args,"-approvedMap"),index=0;
            bool direct=at>=0&&at+1<args.Length&&int.TryParse(args[at+1],out index);
            result.route=selector?"-approvedMaps":direct?"-approvedMap "+index:"missing ordinary launch argument";
            result.requestedIndex=selector?0:index;
            var map=selector?null:ApprovedMapCatalog.All.FirstOrDefault(item=>item.index==index);
            check("ordinary-startup-argument-without-traversal-driver",!Requested&&(selector||direct&&map!=null));
            if(Requested||!selector&&(!direct||map==null))throw new InvalidOperationException("Launch evidence requires -approvedMaps or -approvedMap N, without -approvedMapsSmokeTest.");
            result.expectedScene=selector?"MonsterTrial":map.sceneName;
            double start=Time.realtimeSinceStartupAsDouble,deadline=start+30;
            ManualBattleController controller=null;bool routed=false;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(SceneManager.GetActiveScene().name==result.expectedScene&&controller!=null)
                {
                    if(selector)routed=controller.TestSelectionOpen&&controller.ApprovedMapMenuOpen&&!controller.ArtLoading;
                    else
                    {
                        var bootstrap=Object.FindAnyObjectByType<ApprovedMapBootstrap>();
                        routed=bootstrap!=null&&bootstrap.map==map;
                    }
                    if(routed)break;
                }
                yield return null;
            }
            result.routingSeconds=Time.realtimeSinceStartupAsDouble-start;result.actualScene=SceneManager.GetActiveScene().name;
            check("production-launch-routed-within-thirty-seconds",routed&&result.routingSeconds<=30);
            if(!routed)throw new InvalidOperationException("Ordinary startup did not reach "+result.expectedScene+"; actual scene="+result.actualScene);
            var profiles=ApprovedMapCatalog.All;
            check("eight-available-profiles-and-loaded-previews",profiles.Length==8&&profiles.Select(item=>item.index).SequenceEqual(Enumerable.Range(1,8))&&
                profiles.All(item=>item.encounter!=null&&item.encounter.mapId==item.id&&item.preview!=null&&item.preview.width>0&&item.preview.height>0));
            if(selector)
            {
                yield return null;yield return null;
                check("ordinary-menu-open-without-driver-opening-it",SceneManager.GetActiveScene().name=="MonsterTrial"&&controller.TestSelectionOpen&&
                    controller.ApprovedMapMenuOpen&&!controller.ShopTrialActive&&!controller.ApprovedMapActive&&Object.FindAnyObjectByType<ApprovedMapBootstrap>()==null);
                yield return Capture(directory,"00-normal-launch-selector",check);
            }
            else
            {
                // Routing has already happened in production. Ready only waits for asset
                // preparation and accepts an existing default form; it performs no routing.
                yield return ApprovedMapEvidenceProbe.Ready(map.id,ready=>controller=ready);
                check("ordinary-direct-map-profile-and-preparation",SceneManager.GetActiveScene().name==map.sceneName&&controller.ApprovedMap==map&&
                    controller.ApprovedMapActive&&controller.Loop.CanShop&&controller.Shop.Level==8&&controller.Battle.DP==50&&controller.Battle.Defense==6&&
                    controller.Battle.Units.Count==0&&ApprovedMapEvidenceProbe.NormalProfiles(controller)&&ApprovedMapEvidenceProbe.StockConserved(controller));
                check("ordinary-direct-map-saved-terrain",Object.FindObjectsByType<ApprovedMapTile>(FindObjectsSortMode.None).Length==map.width*map.height);
                yield return Capture(directory,"01-normal-direct-map",check);
            }
            result.actualScene=SceneManager.GetActiveScene().name;
        }

        IEnumerator LaunchEvidence()
        {
            string directory=EvidenceDirectory();Directory.CreateDirectory(directory);
            var result=new LaunchResult{width=Screen.width,height=Screen.height};var checks=new List<Check>();
            var stack=new Stack<IEnumerator>();stack.Push(LaunchChecks(result,directory,checks));
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    result.error=failure.ToString();Debug.LogException(failure);
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            result.checks=checks.ToArray();result.passed=string.IsNullOrEmpty(result.error)&&checks.Count>0&&checks.All(check=>check.passed);
            File.WriteAllText(Path.Combine(directory,"launch-result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("APPROVED_MAP_LAUNCH_RESULT: passed="+result.passed+"; route="+result.route+"; scene="+result.actualScene+"; checks="+checks.Count+"; noForcedOpen=True; noDriverSceneLoad=True");
            Application.Quit(result.passed?0:1);
        }
    }
}
