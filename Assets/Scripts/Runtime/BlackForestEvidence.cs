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
        [Serializable] sealed class BlackForestEvidenceResult
        {
            public bool passed;
            public string scene, error, naturalOutcome;
            public string scope="Production Black Forest V2 encounter and its default eight-member lineup. Production shop pointer handlers, normal star profiles, finite stock, paused deployment and natural combat. The automated deployment/skill policy is a diagnostic, not human play or a full challenge balance verdict. No injected damage, forced victory or changed encounter values.";
            public string deploymentPolicy="Deploy each default member once: M18/M11 at R1; M16/M06/M13/M14 at R2; M19/M12 at R3. Activate every legal ready player skill. Observe up to 300 simulation seconds; victory is recorded but is not a pass requirement.";
            public string[] checks, startingLineup;
            public int screenWidth, screenHeight, defendersAtStart, sixStarDefenders, deployed, survivingDefenders,
                breached, remainingDefense, remainingDP, defenderHits, playerHits, healingShots, defenderSkillCasts, playerSkillCasts;
            public double observedSeconds;
            public float directDamageToDefenders, observedPlayerHealthLoss, observedDefenderHealthRestored;
        }

        IEnumerator BlackForestEvidenceReady()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while((ArtLoading||ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Black Forest evidence preparation timed out.");
            yield return null;
        }

        IEnumerator BlackForestEvidenceCapture(string directory,string name,Action<string,bool> check)
        {
            yield return new WaitForEndOfFrame();
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(screen==null)throw new InvalidOperationException("Black Forest screenshot was unavailable: "+name);
            try
            {
                var pixels=screen.GetPixels32();
                bool visible=false;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/2048))
                    if(pixels[i].r>20||pixels[i].g>20||pixels[i].b>20){visible=true;break;}
                check("visible-capture-"+name,visible&&screen.width>0&&screen.height>0);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),screen.EncodeToPNG());
            }
            finally{Destroy(screen);}
            yield return null;
        }

        bool BlackForestEvidenceStockConserved()=>shop!=null&&shop.Roles.All(role=>
            shop.Available(role.Id)>=0&&shop.Capacity(role.Id)==ShopTrialModel.StockForCost(role.Cost)&&
            shop.Available(role.Id)+shop.Offers.Count(id=>id==role.Id)+
            shop.Bench.Where(piece=>piece.Id==role.Id).Sum(piece=>ShopTrialModel.Copies(piece.Star))==shop.Capacity(role.Id));

        bool BlackForestEvidenceDrag(string cardId,Vector3 entry)
        {
            int index=shop.Bench.ToList().FindIndex(piece=>piece.Id==cardId&&IsLineupPiece(piece));
            if(index<0||Camera.main==null)return false;
            var origin=ShopPieceRect(index);
            if(origin.width<=0||origin.height<=0)return false;
            var target=GUIPosition(Camera.main.WorldToScreenPoint(entry));
            int before=Battle.Units.Count(unit=>!unit.SynergySupport);
            ProcessShopPointer(EventType.MouseDown,origin.center);
            ProcessShopPointer(EventType.MouseDrag,target);
            ProcessShopPointer(EventType.MouseUp,target);
            return Battle.Units.Count(unit=>!unit.SynergySupport)==before+1&&
                Battle.Units.Last(unit=>!unit.SynergySupport).CardId==cardId;
        }

        bool BlackForestEvidenceNormalProfiles()
        {
            foreach(var piece in shop.Bench.Where(IsLineupPiece))
            {
                var expected=CampaignBalance.Build(shopDefinitions[piece.Id],piece.Star,BlackForestChallengePlan.InitialDP,BlackForestChallengePlan.Defense);
                var actual=Battle.FindCard(piece.Id);
                if(actual==null||actual.Settings.Health!=expected.Unit.Health||actual.Settings.DeploymentCost!=expected.Unit.DeploymentCost||
                    actual.Settings.Cooldown!=expected.Unit.Cooldown||actual.Settings.DeploymentLimit!=expected.Unit.DeploymentLimit)return false;
            }
            return true;
        }

        IEnumerator BlackForestEvidenceChecks(string directory,BlackForestEvidenceResult result,Action<string,bool> check)
        {
            yield return BlackForestEvidenceReady();
            var source=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(source==null)throw new InvalidOperationException("Black Forest production encounter was not found.");
            string sourceBefore=JsonUtility.ToJson(source);
            string[] sixStar={"D23","D29","D28","D30","D24","D25"};
            var terrain=FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None);
            var terrainSet=new HashSet<BlackForestTile>(terrain);
            var field=FindAnyObjectByType<AuthoredBattlefield>();
            result.defendersAtStart=Battle.Defenders.Count;
            result.sixStarDefenders=Battle.Defenders.Count(guard=>sixStar.Contains(guard.Settings.ArtId));
            result.startingLineup=shop.Bench.Where(IsLineupPiece).Select(piece=>piece.Id+"/"+piece.Star+"star").ToArray();
            check("production-scene-and-independent-challenge",SceneManager.GetActiveScene().name==BlackForestLayout.SceneName&&BlackForestChallengeActive&&source.mapId==BlackForestLayout.Id&&!source.terrainOnly);
            check("saved-238-tiles-and-native-map",terrain.Length==238&&field!=null&&field.GetComponentsInChildren<Collider>(true).Length==0);
            check("three-native-red-two-native-blue-gates",field!=null&&
                field.GetComponentsInChildren<Transform>(true).Count(t=>t.name.Contains(" / Native tile_start"))==3&&
                field.GetComponentsInChildren<Transform>(true).Count(t=>t.name.Contains(" / Native tile_end"))==2);
            check("nine-defenders-six-distinct-six-stars",result.defendersAtStart==9&&result.sixStarDefenders==6&&
                sixStar.All(id=>Battle.Defenders.Count(guard=>guard.Settings.ArtId==id)==1));
            check("production-defender-values-without-sandbox-boost",source.defenders.Length==9&&source.defenders.All(saved=>
            {
                var guard=Battle.FindDefender(saved.id);
                return guard!=null&&guard.MaxHealth==saved.health&&guard.Settings.Attack==saved.attack&&guard.Position==saved.position;
            }));
            check("level-eight-default-eight-member-lineup",Loop.CanShop&&shop.Level==8&&shopLineup.Count==8&&
                shop.Bench.Where(IsLineupPiece).Select(piece=>piece.Id).SequenceEqual(BlackForestChallengePlan.StartingIds));
            check("fifty-dp-six-defense-normal-card-profiles",Battle.DP==50&&Battle.Defense==6&&Battle.Settings.InitialDefense==6&&BlackForestEvidenceNormalProfiles());
            check("free-preparation-with-finite-conserved-stock",shop.UnlimitedGold&&BlackForestEvidenceStockConserved()&&
                shop.Bench.All(piece=>shop.Available(piece.Id)<shop.Capacity(piece.Id)));
            check("no-retired-stronghold-map-rules",!Battle.StrongholdMapEnabled&&!Battle.StrongholdChallenge&&Battle.StrongholdPortalTrips==0&&Battle.StrongholdInfectionTouches==0);
            check("three-fixed-routes-and-shared-target-doors",Battle.Lanes.Count==3&&
                Battle.Lanes.Select(lane=>lane.Id+"/"+lane.ExitId).SequenceEqual(new[]{"R1/B1","R2/B2","R3/B1"}));
            yield return BlackForestEvidenceCapture(directory,"01-production-preparation",check);

            check("production-start-battle",StartLoopNextBattle());
            yield return BlackForestEvidenceReady();
            var firstLane=Battle.Lanes[0];
            int firstDP=Battle.DP;
            var firstCard=Battle.FindCard("CARD_M18");
            int firstUses=firstCard.RemainingUses;
            check("production-pointer-r1-deployment",BlackForestEvidenceDrag("CARD_M18",firstLane.Entry));
            check("r1-starts-running-and-charges-once",Battle.Phase==BattlePhase.Running&&Battle.DP==firstDP-firstCard.Settings.DeploymentCost&&firstCard.RemainingUses==firstUses-1);
            Battle.SetPaused(true);
            string[] pausedCards={"CARD_M16","CARD_M19"};
            for(int lane=1;lane<3;lane++)
            {
                string cardId=pausedCards[lane-1];var card=Battle.FindCard(cardId);
                int dp=Battle.DP,uses=card.RemainingUses;
                check("production-pointer-paused-r"+(lane+1)+"-deployment",BlackForestEvidenceDrag(cardId,Battle.Lanes[lane].Entry));
                var deployed=Battle.Units.LastOrDefault(unit=>!unit.SynergySupport&&unit.CardId==cardId);
                check("paused-r"+(lane+1)+"-route-cost-use-and-cd",deployed!=null&&deployed.RouteId==Battle.Lanes[lane].Id&&
                    deployed.ExitId==Battle.Lanes[lane].ExitId&&Battle.IsPaused&&Battle.DP==dp-card.Settings.DeploymentCost&&
                    card.RemainingUses==uses-1&&Battle.CardCooldown(cardId)>0);
            }
            var pausedUnits=Battle.Units.ToArray();var positions=pausedUnits.Select(unit=>unit.Motion.Position).ToArray();
            var health=pausedUnits.Select(unit=>unit.Health).ToArray();var sp=pausedUnits.Select(unit=>unit.SkillSP).ToArray();
            var cooldowns=Battle.Cards.Select(card=>Battle.CardCooldown(card.Settings.CardId)).ToArray();
            var defenderSP=Battle.Defenders.Select(guard=>guard.SkillSP).ToArray();double pausedClock=Battle.Clock;
            int beforeRepeat=Battle.DP,unitsBeforeRepeat=Battle.Units.Count;
            check("paused-repeat-does-not-bypass-card-cooldown",!BlackForestEvidenceDrag("CARD_M16",Battle.Lanes[1].Entry)&&
                Battle.DP==beforeRepeat&&Battle.Units.Count==unitsBeforeRepeat);
            Tick(5);
            check("pause-freezes-clock-position-health-sp-and-deployment-cd",Battle.Clock==pausedClock&&Battle.IsPaused&&
                pausedUnits.Select((unit,i)=>unit.Motion.Position==positions[i]&&unit.Health==health[i]&&unit.SkillSP==sp[i]).All(same=>same)&&
                Battle.Cards.Select((card,i)=>Battle.CardCooldown(card.Settings.CardId)==cooldowns[i]).All(same=>same)&&
                Battle.Defenders.Select((guard,i)=>guard.SkillSP==defenderSP[i]).All(same=>same));
            yield return BlackForestEvidenceCapture(directory,"02-three-gates-paused-deployment",check);

            var previous=Battle;
            check("production-encounter-restart-accepted",RestartCampaignEncounter());
            yield return BlackForestEvidenceReady();
            check("encounter-restart-restores-nine-defenders-and-fifty-dp",!ReferenceEquals(previous,Battle)&&Battle.Phase==BattlePhase.Ready&&
                Battle.Clock==0&&Battle.Units.Count==0&&Battle.Defenders.Count==9&&Battle.Defenders.All(guard=>guard.Alive&&guard.Health==guard.MaxHealth)&&
                Battle.DP==50&&Battle.Defense==6&&!Battle.IsPaused&&BlackForestEvidenceNormalProfiles());
            check("restart-preserves-exact-authored-terrain",terrainSet.SetEquals(FindObjectsByType<BlackForestTile>()));
            yield return BlackForestEvidenceCapture(directory,"03-encounter-restarted",check);

            string[] deploymentOrder={"CARD_M18","CARD_M16","CARD_M19","CARD_M06","CARD_M11","CARD_M12","CARD_M13","CARD_M14"};
            int[] laneForCard={0,1,2,1,0,2,1,1};
            for(int i=0;i<deploymentOrder.Length;i++)
                check("natural-default-pointer-deploy-"+deploymentOrder[i],BlackForestEvidenceDrag(deploymentOrder[i],Battle.Lanes[laneForCard[i]].Entry));
            var combatBattle=Battle;
            check("deployed-attacks-retain-normal-star-profiles",combatBattle.Units.Where(unit=>!unit.SynergySupport).All(unit=>
            {
                var piece=shop.Bench.Single(item=>item.Id==unit.CardId&&IsLineupPiece(item));
                return Math.Abs(combatBattle.UnitCombat(unit).UnitAttack-CampaignBalance.Build(shopDefinitions[unit.CardId],piece.Star,50,6).Combat.UnitAttack)<.0001f;
            }));
            Battle.SetPaused(false);SimulationSpeed=1;
            bool exchangeCapture=false,healingCapture=false,skillCapture=false;
            for(int step=0;step<6000&&combatBattle.Phase==BattlePhase.Running;step++)
            {
                var guards=combatBattle.Defenders.ToArray();
                var guardHealth=guards.Select(guard=>guard.Health).ToArray();var guardMax=guards.Select(guard=>guard.MaxHealth).ToArray();
                var units=combatBattle.Units.ToArray();var unitHealth=units.Select(unit=>unit.Health).ToArray();var unitMax=units.Select(unit=>unit.MaxHealth).ToArray();
                foreach(var unit in units)
                    if(unit.Active&&combatBattle.CheckSkill(unit.EntityId)==SkillFailure.None){SelectSkillUnit(unit.EntityId);ActivateSelectedSkill();}
                Tick(.05f);
                for(int i=0;i<guards.Length;i++)
                    if(guards[i].MaxHealth==guardMax[i])result.observedDefenderHealthRestored+=Math.Max(0,guards[i].Health-guardHealth[i]);
                for(int i=0;i<units.Length;i++)
                    if(units[i].MaxHealth==unitMax[i])result.observedPlayerHealthLoss+=Math.Max(0,unitHealth[i]-units[i].Health);
                if(!exchangeCapture&&combatBattle.CombatShots.Any(shot=>!shot.FromDefender&&shot.Applied&&shot.DamageDealt>0)&&result.observedPlayerHealthLoss>0)
                {exchangeCapture=true;yield return BlackForestEvidenceCapture(directory,"04-natural-damage-exchange",check);}
                if(!healingCapture&&result.observedDefenderHealthRestored>0&&combatBattle.CombatShots.Any(shot=>shot.Healing&&shot.Applied))
                {healingCapture=true;yield return BlackForestEvidenceCapture(directory,"05-natural-defender-healing",check);}
                if(!skillCapture&&combatBattle.Defenders.Any(guard=>guard.SkillCasts>0)&&combatBattle.SkillCasts>0)
                {skillCapture=true;yield return BlackForestEvidenceCapture(directory,"06-natural-skills",check);}
                if(step%4==0)yield return null;
            }
            result.naturalOutcome=combatBattle.Phase.ToString();result.observedSeconds=combatBattle.Clock;
            result.deployed=combatBattle.Units.Count(unit=>!unit.SynergySupport);
            result.survivingDefenders=combatBattle.Defenders.Count(guard=>guard.Alive);
            result.breached=combatBattle.Breaches;result.remainingDefense=combatBattle.Defense;result.remainingDP=combatBattle.DP;
            result.defenderHits=combatBattle.CombatShots.Count(shot=>shot.FromDefender&&!shot.Healing&&shot.Applied);
            result.playerHits=combatBattle.CombatShots.Count(shot=>!shot.FromDefender&&shot.Applied&&shot.DamageDealt>0);
            result.healingShots=combatBattle.CombatShots.Count(shot=>shot.Healing&&shot.Applied);
            result.defenderSkillCasts=combatBattle.Defenders.Sum(guard=>guard.SkillCasts);result.playerSkillCasts=combatBattle.SkillCasts;
            result.directDamageToDefenders=combatBattle.CombatShots.Where(shot=>!shot.FromDefender&&shot.Applied).Sum(shot=>Math.Max(0,shot.DamageDealt));
            check("natural-two-way-health-damage",result.directDamageToDefenders>0&&result.observedPlayerHealthLoss>0);
            check("natural-defender-healing-observed",result.observedDefenderHealthRestored>0&&result.healingShots>0);
            check("natural-defender-and-manual-player-skills-observed",result.defenderSkillCasts>0&&result.playerSkillCasts>0);
            check("deployed-units-retain-entry-route-and-target",combatBattle.Units.Where(unit=>!unit.SynergySupport).All(unit=>
                combatBattle.Lanes.Any(lane=>lane.Id==unit.RouteId&&lane.ExitId==unit.ExitId)));
            check("finite-stock-remains-conserved-after-natural-combat",BlackForestEvidenceStockConserved());
            Debug.Log("BLACK_FOREST_NATURAL_RESULT: outcome="+result.naturalOutcome+"; seconds="+result.observedSeconds+
                "; defenderDamage="+result.directDamageToDefenders+"; observedHealing="+result.observedDefenderHealthRestored+
                "; remainingDefense="+result.remainingDefense+"; remainingDP="+result.remainingDP+"; victoryIsNotPassRequirement=True");
            yield return BlackForestEvidenceCapture(directory,"07-natural-observation-result",check);

            RestartCurrentRun();
            yield return BlackForestEvidenceReady();
            check("full-run-restart-keeps-production-challenge",BlackForestChallengeActive&&Loop.CanShop&&shop.Level==8&&
                Battle.Clock==0&&Battle.DP==50&&Battle.Defense==6&&Battle.Units.Count==0&&Battle.Defenders.Count==9&&
                Battle.Defenders.All(guard=>guard.Alive&&guard.Health==guard.MaxHealth&&guard.SkillCasts==0)&&
                !Battle.StrongholdMapEnabled&&BlackForestEvidenceNormalProfiles()&&BlackForestEvidenceStockConserved());
            check("full-restart-preserves-238-authored-tiles",terrainSet.SetEquals(FindObjectsByType<BlackForestTile>()));
            check("production-source-asset-was-not-mutated",JsonUtility.ToJson(source)==sourceBefore);
            yield return BlackForestEvidenceCapture(directory,"08-production-restarted-preparation",check);
        }

        public IEnumerator VerifyBlackForest()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"BlackForestEvidence");
            Directory.CreateDirectory(directory);
            var result=new BlackForestEvidenceResult{scene=SceneManager.GetActiveScene().name,screenWidth=Screen.width,screenHeight=Screen.height};
            var checks=new List<string>();bool valid=true;float previousTimeScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("BLACK_FOREST_V2 "+name+"="+ok);};
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
            var stack=new Stack<IEnumerator>();stack.Push(BlackForestEvidenceChecks(directory,result,check));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    result.error=failure.ToString();valid=false;Debug.LogException(failure);
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),result.error);break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            Time.timeScale=previousTimeScale;CancelShopDrag();result.passed=valid&&string.IsNullOrEmpty(result.error);result.checks=checks.ToArray();
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("BLACK_FOREST_CHECK: passed="+result.passed+"; checks="+checks.Count+"; naturalOutcome="+result.naturalOutcome+"; noInjectedDamage=True; noForcedVictory=True");
            Application.Quit(result.passed?0:1);
        }
    }
}
