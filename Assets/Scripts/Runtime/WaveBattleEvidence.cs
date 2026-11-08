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
        [Serializable] sealed class WaveEvidenceCheckpoint
        {
            public string name, lane, phase;
            public double clock, remaining;
            public int wave, budget, dp, defense;
            public float damage;
            public string[] cards, units, defenders;
        }
        [Serializable] sealed class WaveEvidenceRun
        {
            public string policy, outcome, referenceOutcome;
            public double seconds, referenceSeconds, secondsDeltaFromModel;
            public int referenceSeed=-1;
            public int deployments, breaches, defense, dp, guardsAlive, playerHits, defenderHits, healingShots,
                defenderSkillCasts, playerSkillCasts;
            public float damage, pursuitDamage, directShotDamage, observedPlayerHealthLoss, referenceDamage, damageDeltaFromModel;
            public bool timedOut, manualSkillUnitObserved, playerHealthLossSampled, requiresVictory, modelOutcomeAndTimeMatch;
            public string[] deploymentLog, ranking;
        }
        [Serializable] sealed class WaveEvidenceResult
        {
            public bool passed;
            public string error, scene;
            public string scope="Saved Black Forest encounter, ordinary finite-stock lineup, production card down/up handlers and natural combat. Sparse deployment observes wave boundaries and the deadline; a separate exact replay of fixed model policy seed 20261109 checks its natural winning result. No injected damage, modified HP, forced result or claim of OS mouse input.";
            public int screenWidth, screenHeight;
            public string[] checks;
            public WaveEvidenceCheckpoint[] checkpoints;
            public WaveEvidenceRun[] runs;
        }

        public static string WaveEvidenceDirectory()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-waveEvidenceDir");
            if(at<0)at=Array.IndexOf(args,"-demoEvidenceDir");
            return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"WaveBattleEvidence");
        }

        IEnumerator WaveEvidenceReady()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while((ArtLoading||ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Wave evidence preparation timed out.");
            // The default infection form is accepted only by this diagnostic.
            if(inspectedSynergy=="感染生物")inspectedSynergy=null;
            CloseSkillMenu();CloseDefenderInspector();yield return null;
        }

        IEnumerator WaveEvidenceCapture(string directory,string name,Action<string,bool> check)
        {
            yield return new WaitForEndOfFrame();
            var screen=ScreenCapture.CaptureScreenshotAsTexture();
            if(screen==null)throw new InvalidOperationException("Wave screenshot was unavailable: "+name);
            try
            {
                var pixels=screen.GetPixels32();int visible=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/2048))
                    if(pixels[i].r>20||pixels[i].g>20||pixels[i].b>20)visible++;
                check("visible-capture-"+name,visible>32&&screen.width==Screen.width&&screen.height==Screen.height);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),screen.EncodeToPNG());
            }
            finally{Destroy(screen);}
            yield return null;
        }

        bool WaveEvidenceClick(string cardId)
        {
            int index=shop.Bench.ToList().FindIndex(piece=>piece.Id==cardId&&IsLineupPiece(piece));
            if(index<0)return false;
            Rect rect=ShopPieceRect(index);if(rect.width<=0||rect.height<=0)return false;
            int before=Battle.Units.Count(unit=>!unit.SynergySupport);
            ProcessShopPointer(EventType.MouseDown,rect.center);ProcessShopPointer(EventType.MouseUp,rect.center);
            return Battle.Units.Count(unit=>!unit.SynergySupport)==before+1&&
                Battle.Units.Last(unit=>!unit.SynergySupport).CardId==cardId;
        }

        WaveEvidenceCheckpoint WaveEvidenceSnapshot(string name)=>new WaveEvidenceCheckpoint
        {
            name=name,lane=Battle.CurrentWaveLaneId,phase=Battle.Phase.ToString(),clock=Battle.Clock,
            remaining=Battle.BattleTimeRemaining,wave=Battle.CurrentWaveIndex,budget=Battle.CurrentWaveBudget,
            dp=Battle.DP,defense=Battle.Defense,damage=Battle.TotalPlayerDamage,
            cards=Battle.Cards.Select(card=>card.Settings.CardId+"/uses="+card.RemainingUses+"/cd="+Battle.CardCooldown(card.Settings.CardId).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
            units=Battle.Units.Select(unit=>unit.EntityId+"/"+unit.CardId+"/"+unit.Resolution+"/hp="+unit.Health+"/sp="+unit.SkillSP+"/route="+unit.RouteId+"/position="+unit.Motion.Position).ToArray(),
            defenders=Battle.Defenders.Select(guard=>guard.EntityId+"/alive="+guard.Alive+"/hp="+guard.Health+"/sp="+guard.SkillSP+"/casts="+guard.SkillCasts).ToArray()
        };

        bool WaveEvidenceOnlyCurrentGate()=>Battle.Lanes.Count(lane=>Battle.CanDeployOnLane(lane.Id))==1&&
            Battle.Lanes.All(lane=>Battle.CanDeployOnLane(lane.Id)==(lane.Id==Battle.CurrentWaveLaneId));

        bool WaveEvidenceMapFits()
        {
            var safe=ShopMapSafeRect;safe.xMin-=2;safe.xMax+=2;safe.yMin-=2;safe.yMax+=2;
            return ShopMapPlayablePoints().All(world=>
            {
                var point=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(world));
                return safe.Contains(point)&&!DamageRankingPanelRect.Contains(point);
            });
        }
        bool WaveEvidenceRoutesMatchMotion()
        {
            var strokes=WaveRouteGuideStrokes();if(strokes.Count==0)return false;
            foreach(var stroke in strokes)
            {
                var lane=Battle.Lanes.Single(item=>item.Id==stroke.LaneId);var p=stroke.Projection;
                var motion=new RouteMotion(lane.Points,1);
                motion.ApproachDistance(p.Source.FromDistance,1000);
                if(Vector3.Distance(motion.Position,p.Source.From)>.001f)return false;
                motion.ApproachDistance(p.Source.ToDistance,1000);
                if(Vector3.Distance(motion.Position,p.Source.To)>.001f)return false;
                Vector2 from=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(p.Source.From));
                Vector2 to=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(p.Source.To));
                if(Vector2.Distance(from,p.From)>.1f||Vector2.Distance(to,p.To)>.1f||p.Source.ToDistance<=p.Source.FromDistance)return false;
            }
            return strokes.Where(s=>s.Open).All(s=>s.LaneId==Battle.CurrentWaveLaneId);
        }

        IEnumerator WaveEvidenceAdvanceTo(double target)
        {
            int step=0;
            while(Battle.Phase==BattlePhase.Running&&Battle.Clock+0.000001<target)
            {
                Tick((float)Math.Min(.05,target-Battle.Clock));
                if(++step%4==0)yield return null;
            }
        }

        void WaveEvidenceRecord(WaveEvidenceRun run,List<string> deployments)
        {
            run.outcome=Battle.Phase.ToString();run.seconds=Battle.Clock;run.timedOut=Battle.TimedOut;
            run.deployments=Battle.Units.Count(unit=>!unit.SynergySupport);run.breaches=Battle.Breaches;
            run.defense=Battle.Defense;run.dp=Battle.DP;run.guardsAlive=Battle.Defenders.Count(guard=>guard.Alive);
            run.damage=Battle.TotalPlayerDamage;run.pursuitDamage=Battle.TotalGuerrillaDamage;run.playerSkillCasts=Battle.SkillCasts;
            run.defenderSkillCasts=Battle.Defenders.Sum(guard=>guard.SkillCasts);
            run.playerHits=Battle.CombatShots.Count(shot=>!shot.FromDefender&&shot.Applied&&shot.DamageDealt>0);
            run.defenderHits=Battle.CombatShots.Count(shot=>shot.FromDefender&&!shot.Healing&&shot.Applied&&shot.DamageDealt>0);
            run.healingShots=Battle.CombatShots.Count(shot=>shot.Healing&&shot.Applied);
            run.directShotDamage=Battle.CombatShots.Where(shot=>!shot.FromDefender&&shot.Applied).Sum(shot=>Math.Max(0,shot.DamageDealt));
            run.manualSkillUnitObserved=Battle.Units.Any(unit=>!unit.SynergySupport&&Battle.UnitCombat(unit).HasSkill);
            if(run.referenceSeed>=0)
            {
                run.secondsDeltaFromModel=run.seconds-run.referenceSeconds;run.damageDeltaFromModel=run.damage-run.referenceDamage;
                run.modelOutcomeAndTimeMatch=run.outcome==run.referenceOutcome&&Math.Abs(run.secondsDeltaFromModel)<.15;
            }
            run.deploymentLog=deployments.ToArray();
            run.ranking=Battle.DamageRanking.Select(row=>row.CardId+":"+row.Damage.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"/pursuit="+row.GuerrillaDamage.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Debug.Log($"WAVE_BATTLE_NATURAL: policy={run.policy}; outcome={run.outcome}; time={run.seconds:F3}; timedOut={run.timedOut}; deployments={run.deployments}; damage={run.damage}; breaches={run.breaches}; defense={run.defense}; skills={run.playerSkillCasts}/{run.defenderSkillCasts}; requiresVictory={run.requiresVictory}; referenceSeed={run.referenceSeed}; modelTimeDelta={run.secondsDeltaFromModel:F4}; modelDamageDelta={run.damageDeltaFromModel:F4}");
        }

        IEnumerator WaveEvidenceChecks(string directory,WaveEvidenceResult result,Action<string,bool> check)
        {
            yield return WaveEvidenceReady();SimulationSpeed=1;
            var source=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(source==null||source.WaveSnapshot()==null)throw new InvalidOperationException("Saved Black Forest wave configuration is missing.");
            string sourceBefore=JsonUtility.ToJson(source);
            string waveSourceBefore=JsonUtility.ToJson(source.attackWaves);
            var terrain=new HashSet<BlackForestTile>(FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None));
            var checkpoints=new List<WaveEvidenceCheckpoint>();var runs=new List<WaveEvidenceRun>();
            result.scene=SceneManager.GetActiveScene().name;
            check("saved-forest-nine-defenders-and-238-persistent-tiles",result.scene==BlackForestLayout.SceneName&&terrain.Count==238&&
                BlackForestChallengeActive&&Battle.Defenders.Count==9&&!source.terrainOnly);
            check("three-45-second-budgets-16-16-18-and-120-second-final-advance",Battle.WavesEnabled&&Battle.WaveCount==3&&
                Battle.WavePlan.Waves.Select(w=>w.LaneId).SequenceEqual(new[]{"R1","R2","R3"})&&
                Battle.WavePlan.Waves.Select(w=>w.DP).SequenceEqual(new[]{16,16,18})&&
                Battle.WavePlan.Waves.All(w=>w.DeploymentSeconds==45)&&Battle.WavePlan.FinalAdvanceSeconds==120&&Battle.WavePlan.TotalDuration==255);
            check("cadence-five-cost-bands",Enumerable.Range(1,5).Select(cost=>WaveDeploymentCadence.ForCard("generic",cost)).SequenceEqual(new[]{6f,10f,16f,24f,45f}));
            check("cadence-four-identity-exceptions",WaveDeploymentCadence.ForCard("CARD_M04",1)==14&&WaveDeploymentCadence.ForCard("CARD_M06",2)==16&&
                WaveDeploymentCadence.ForCard("CARD_M17",3)==20&&WaveDeploymentCadence.ForCard("CARD_M38",3)==24);
            check("all-live-card-profiles-use-wave-cadence",Battle.Cards.All(card=>
                card.Settings.Cooldown==WaveDeploymentCadence.ForCard(card.Settings.CardId,CampaignBalance.Profiles[card.Settings.CardId].Cost)));
            check("preparation-first-budget-one-open-gate-no-countdown",Battle.Phase==BattlePhase.Ready&&Battle.Clock==0&&Battle.DP==16&&
                Battle.CurrentWaveIndex==0&&Battle.CurrentWaveLaneId=="R1"&&WaveEvidenceOnlyCurrentGate());
            check("damage-ranking-retained-and-blocks-world-input",DamageRankingVisible&&!ShopMapPointAvailable(DamageRankingPanelRect.center));
            check("complete-forest-and-defenders-fit-left-of-ranking",WaveEvidenceMapFits());
            check("route-lines-match-motion-and-root-canvas",WaveEvidenceRoutesMatchMotion());
            inspectedSynergy="感染生物";
            check("ranking-hidden-while-infection-modal-owns-map",CampaignUiModalOpen&&!DamageRankingVisible&&!ShopMapPointAvailable(ShopMapSafeRect.center));
            inspectedSynergy=null;
            check("ranking-restored-after-modal-closes",DamageRankingVisible);
            check("finite-stock-reservations-conserved",BlackForestEvidenceStockConserved());
            yield return WaveEvidenceCapture(directory,"01-wave-preparation",check);

            check("production-start-battle",StartLoopNextBattle());yield return WaveEvidenceReady();
            var first=Battle.FindCard("CARD_M07");int firstDP=Battle.DP,firstUses=first.RemainingUses;
            check("one-card-down-up-deploys-once",WaveEvidenceClick("CARD_M07"));
            check("first-click-current-gate-and-single-charge",Battle.Phase==BattlePhase.Running&&
                Battle.DP==firstDP-first.Settings.DeploymentCost&&first.RemainingUses==firstUses-1&&
                Battle.Units.Last(unit=>!unit.SynergySupport).RouteId=="R1");
            Battle.SetPaused(true);
            var second=Battle.FindCard("CARD_M08");int pausedDP=Battle.DP,pausedUses=second.RemainingUses;
            check("paused-card-click-immediately-deploys",WaveEvidenceClick("CARD_M08")&&Battle.IsPaused&&
                Battle.DP==pausedDP-second.Settings.DeploymentCost&&second.RemainingUses==pausedUses-1&&Battle.CardCooldown("CARD_M08")>0);
            int repeatDP=Battle.DP,repeatUses=second.RemainingUses,repeatCount=Battle.Units.Count;
            check("same-card-second-click-respects-cooldown",!WaveEvidenceClick("CARD_M08")&&Battle.DP==repeatDP&&
                second.RemainingUses==repeatUses&&Battle.Units.Count==repeatCount);
            check("other-gates-rejected-by-live-model",Battle.Lanes.Where(lane=>lane.Id!="R1").All(lane=>
                Battle.CheckCardDeployment("CARD_M08",lane.Entry,true)==DeployFailure.WaveEntryClosed));
            var pausedSnapshot=WaveEvidenceSnapshot("paused-before");
            var positions=Battle.Units.Select(unit=>unit.Motion.Position).ToArray();var health=Battle.Units.Select(unit=>unit.Health).ToArray();
            var sp=Battle.Units.Select(unit=>unit.SkillSP).ToArray();var guardSP=Battle.Defenders.Select(guard=>guard.SkillSP).ToArray();
            var cooldowns=Battle.Cards.Select(card=>Battle.CardCooldown(card.Settings.CardId)).ToArray();
            var pausedRoutes=WaveRouteGuideStrokes();
            Tick(5);
            var afterRoutes=WaveRouteGuideStrokes();
            check("pause-freezes-visible-route-flow",pausedRoutes.Count==afterRoutes.Count&&pausedRoutes.Select((stroke,i)=>
                stroke.Projection.From==afterRoutes[i].Projection.From&&stroke.Projection.To==afterRoutes[i].Projection.To).All(same=>same));
            check("pause-freezes-wave-clock-entities-sp-and-card-cd",Battle.Clock==pausedSnapshot.clock&&Battle.BattleTimeRemaining==pausedSnapshot.remaining&&
                Battle.Units.Select((unit,i)=>unit.Motion.Position==positions[i]&&unit.Health==health[i]&&unit.SkillSP==sp[i]).All(same=>same)&&
                Battle.Defenders.Select((guard,i)=>guard.SkillSP==guardSP[i]).All(same=>same)&&
                Battle.Cards.Select((card,i)=>Battle.CardCooldown(card.Settings.CardId)==cooldowns[i]).All(same=>same));
            yield return WaveEvidenceCapture(directory,"02-one-click-paused",check);Battle.SetPaused(false);

            // Only these sparse, legal deployments are used for the deadline observation.
            var sparseLog=new List<string>{"0.000/CARD_M07/R1","0.000/CARD_M08/R1 (paused)"};
            foreach(double boundary in new[]{45d,90d})
            {
                yield return WaveEvidenceAdvanceTo(boundary-.1);
                string cardId=boundary==45?"CARD_M07":"CARD_M08";
                bool deployed=WaveEvidenceClick(cardId);
                check("legal-pre-boundary-click-"+boundary,deployed);
                if(deployed)sparseLog.Add(Battle.Clock.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"/"+cardId+"/"+Battle.CurrentWaveLaneId);
                var cards=Battle.Cards.ToArray();var uses=cards.Select(card=>card.RemainingUses).ToArray();
                var beforeCD=cards.Select(card=>Battle.CardCooldown(card.Settings.CardId)).ToArray();
                var units=Battle.Units.ToArray();var guards=Battle.Defenders.ToArray();var dead=guards.Where(guard=>!guard.Alive).ToArray();
                int guardCasts=guards.Sum(guard=>guard.SkillCasts);float damage=Battle.TotalPlayerDamage;double before=Battle.Clock;
                checkpoints.Add(WaveEvidenceSnapshot("before-"+boundary));
                yield return WaveEvidenceAdvanceTo(boundary+.01);
                double advanced=Battle.Clock-before;
                int expectedWave=boundary==45?1:2,expectedBudget=boundary==45?16:18;
                check("boundary-"+boundary+"-replaces-dp-and-opens-next-gate",Battle.CurrentWaveIndex==expectedWave&&
                    Battle.DP==expectedBudget&&Battle.CurrentWaveBudget==expectedBudget&&WaveEvidenceOnlyCurrentGate());
                check("boundary-"+boundary+"-keeps-card-use-and-continuous-cd",cards.All(card=>ReferenceEquals(card,Battle.FindCard(card.Settings.CardId)))&&
                    cards.Select((card,i)=>card.RemainingUses==uses[i]&&Math.Abs(Battle.CardCooldown(card.Settings.CardId)-Math.Max(0,beforeCD[i]-advanced))<.002).All(same=>same));
                check("boundary-"+boundary+"-preserves-unit-guard-identities-and-history",units.All(unit=>Battle.Units.Any(current=>ReferenceEquals(unit,current)))&&
                    guards.All(guard=>Battle.Defenders.Any(current=>ReferenceEquals(guard,current)))&&dead.All(guard=>!guard.Alive)&&
                    Battle.Defenders.Sum(guard=>guard.SkillCasts)>=guardCasts&&Battle.TotalPlayerDamage>=damage);
                checkpoints.Add(WaveEvidenceSnapshot("after-"+boundary));
                check("route-lines-follow-live-wave-"+boundary,WaveEvidenceRoutesMatchMotion());
                yield return WaveEvidenceCapture(directory,boundary==45?"03-wave-two":"04-wave-three",check);
            }
            yield return WaveEvidenceAdvanceTo(135.01);
            check("at-135-all-deployment-closes-with-120-second-advance",Battle.FinalAdvanceActive&&!Battle.DeploymentWindowOpen&&
                Battle.CurrentWaveIndex==2&&Battle.CurrentWaveBudget==0&&Battle.CurrentWaveLaneId==null&&Battle.DP==0&&
                Battle.Lanes.All(lane=>!Battle.CanDeployOnLane(lane.Id))&&Math.Abs(Battle.WaveRemaining-119.99)<.02);
            int finalUnits=Battle.Units.Count,finalUses=first.RemainingUses;
            check("final-advance-click-cannot-spend-or-deploy",!WaveEvidenceClick("CARD_M07")&&Battle.DP==0&&Battle.Units.Count==finalUnits&&
                first.RemainingUses==finalUses&&Battle.CheckCardDeployment("CARD_M07",Battle.Lanes[2].Entry)==DeployFailure.DeploymentWindowEnded);
            checkpoints.Add(WaveEvidenceSnapshot("final-advance"));
            yield return WaveEvidenceCapture(directory,"05-final-advance-no-deployment",check);
            yield return WaveEvidenceAdvanceTo(254.99);
            check("natural-sparse-battle-remains-running-before-deadline",Battle.Phase==BattlePhase.Running&&!Battle.TimedOut&&Battle.BattleTimeRemaining>0&&Battle.BattleTimeRemaining<.02);
            yield return WaveEvidenceAdvanceTo(255.01);
            check("255-second-hard-deadline-loses-with-positive-defense",Battle.Phase==BattlePhase.Lost&&Battle.TimedOut&&Battle.Defense>0&&
                Math.Abs(Battle.Clock-255)<.001&&Battle.BattleTimeRemaining==0);
            var sparse=new WaveEvidenceRun{policy="Sparse: two cards at t0, M07 at 44.9, M08 at 89.9; no further deployments or damage injection."};
            WaveEvidenceRecord(sparse,sparseLog);runs.Add(sparse);checkpoints.Add(WaveEvidenceSnapshot("deadline"));
            yield return WaveEvidenceCapture(directory,"06-real-timeout",check);

            RestartCurrentRun();yield return WaveEvidenceReady();SimulationSpeed=1;
            check("restart-restores-wave-one-dp-cards-guards-and-damage",Battle.Phase==BattlePhase.Ready&&Battle.Clock==0&&Battle.CurrentWaveIndex==0&&
                Battle.DP==16&&Battle.CurrentWaveLaneId=="R1"&&!Battle.TimedOut&&!Battle.FinalAdvanceActive&&Battle.Units.Count==0&&Battle.Defense==6&&
                Battle.Cards.All(card=>card.RemainingUses==card.Settings.DeploymentLimit&&Battle.CardCooldown(card.Settings.CardId)==0)&&
                Battle.Defenders.Count==9&&Battle.Defenders.All(guard=>guard.Alive&&guard.Health==guard.MaxHealth&&guard.SkillCasts==0)&&Battle.TotalPlayerDamage==0);
            check("restart-keeps-exact-saved-terrain",terrain.SetEquals(FindObjectsByType<BlackForestTile>()));
            check("production-start-strategy-attempt",StartLoopNextBattle());yield return WaveEvidenceReady();
            var attempt=new WaveEvidenceRun{policy="Exact legal deployment replay of fixed model seed 20261109 (targeted-0-pairs-and-delays), using the unchanged default lineup and normal production card clicks.",
                playerHealthLossSampled=true,requiresVictory=true,referenceSeed=20261109,referenceOutcome="Won",referenceSeconds=193.4,referenceDamage=7255.08984375f};
            double[] plannedTimes={0,.25,6.25,6.5,12.5,50,50.25,56.25,56.5,62.5,100,100.25,106.25,106.5,112.5,112.75};
            string[] plannedIds={"M07","M08","M07","M08","M07","M16","M08","M08","M15","M16","M15","M08","M15","M08","M15","M08"};
            var log=new List<string>();int cursor=0;
            bool correctLanes=true,clockStarted=false,combatCapture=false,deadClickChecked=false;
            for(int step=0;step<5400&&(Battle.Phase==BattlePhase.Ready||Battle.Phase==BattlePhase.Running);step++)
            {
                if(cursor<plannedTimes.Length&&Battle.Clock+.000001>=plannedTimes[cursor])
                {
                    string id="CARD_"+plannedIds[cursor],expectedLane=plannedTimes[cursor]<45?"R1":plannedTimes[cursor]<90?"R2":"R3";
                    var lane=Battle.Lanes.Single(item=>item.Id==expectedLane);var failure=Battle.CheckCardDeployment(id,lane.Entry);
                    bool legal=Battle.DeploymentWindowOpen&&Battle.CurrentWaveLaneId==expectedLane&&
                        (failure==DeployFailure.None||failure==DeployFailure.NotStarted)&&Math.Abs(Battle.Clock-plannedTimes[cursor])<.001;
                    bool clicked=legal&&WaveEvidenceClick(id);
                    check("seed-20261109-legal-planned-click-"+(cursor+1),clicked);
                    if(clicked)
                    {
                        correctLanes&=Battle.Units.Last(unit=>!unit.SynergySupport).RouteId==expectedLane;
                        log.Add(Battle.Clock.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"/"+id+"/"+expectedLane+"/dp="+Battle.DP+"/uses="+Battle.FindCard(id).RemainingUses);
                        clockStarted=true;
                    }
                    cursor++;
                }
                var units=Battle.Units.ToArray();var hp=units.Select(unit=>unit.Health).ToArray();var max=units.Select(unit=>unit.MaxHealth).ToArray();
                foreach(var unit in units)
                    if(unit.Active&&Battle.CheckSkill(unit.EntityId)==SkillFailure.None){SelectSkillUnit(unit.EntityId);ActivateSelectedSkill();}
                CloseSkillMenu();Tick(.05f);
                for(int i=0;i<units.Length;i++)if(units[i].MaxHealth==max[i])attempt.observedPlayerHealthLoss+=Math.Max(0,hp[i]-units[i].Health);
                if(!combatCapture&&Battle.TotalPlayerDamage>0&&attempt.observedPlayerHealthLoss>0)
                {combatCapture=true;yield return WaveEvidenceCapture(directory,"07-natural-two-way-combat",check);}
                if(!deadClickChecked&&Battle.Phase==BattlePhase.Running)
                    foreach(var guard in Battle.Defenders.Where(item=>!item.Alive))
                    {
                        Vector2 point=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(guard.Position+Camera.main.transform.up*.75f));
                        if(!ShopMapPointAvailable(point))continue;
                        ProcessShopPointer(EventType.MouseDown,point);ProcessShopPointer(EventType.MouseUp,point);
                        check("naturally-dead-defender-is-not-inspectable",InspectedDefenderId!=guard.EntityId&&
                            (InspectedDefenderId==null||Battle.FindDefender(InspectedDefenderId).Alive));
                        CloseDefenderInspector();deadClickChecked=true;break;
                    }
                if(step%4==0)yield return null;
                if(!clockStarted&&step>100)throw new InvalidOperationException("The production card click did not start the strategy attempt.");
            }
            WaveEvidenceRecord(attempt,log);runs.Add(attempt);
            check("strategy-replays-sixteen-planned-single-clicks",cursor==16&&log.Count==16&&attempt.deployments==16);
            check("strategy-uses-legal-clicks-and-current-wave-gates",attempt.deployments>0&&correctLanes&&
                Battle.Units.Where(unit=>!unit.SynergySupport).All(unit=>Battle.Lanes.Any(lane=>lane.Id==unit.RouteId&&lane.ExitId==unit.ExitId)));
            check("strategy-observes-natural-two-way-damage-and-defender-skills",attempt.damage>0&&attempt.observedPlayerHealthLoss>0&&attempt.defenderSkillCasts>0);
            check("strategy-reaches-actual-terminal-result",Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost);
            check("seed-20261109-reproduces-natural-model-win-and-time",attempt.outcome=="Won"&&attempt.defense==0&&!attempt.timedOut&&attempt.modelOutcomeAndTimeMatch);
            check("ranking-remains-correct-after-combat",DamageRankingVisible&&Math.Abs(Battle.DamageRanking.Sum(row=>row.Damage)-Battle.TotalPlayerDamage)<.1f&&
                Battle.DamageRanking.Select(row=>row.Damage).SequenceEqual(Battle.DamageRanking.Select(row=>row.Damage).OrderByDescending(damage=>damage)));
            check("natural-dead-footprint-check-executed",deadClickChecked);
            check("pursuit-subtotal-is-conserved-without-double-counting",Math.Abs(Battle.DamageRanking.Sum(row=>row.GuerrillaDamage)-Battle.TotalGuerrillaDamage)<.1f&&
                Battle.DamageRanking.All(row=>row.GuerrillaDamage>=0&&row.GuerrillaDamage<=row.Damage+.01f));
            check("finite-stock-conserved-after-wave-combat",BlackForestEvidenceStockConserved());
            yield return WaveEvidenceCapture(directory,"08-natural-strategy-result",check);
            RestartCurrentRun();yield return WaveEvidenceReady();
            check("full-restart-preserves-saved-map-and-wave-source",terrain.SetEquals(FindObjectsByType<BlackForestTile>())&&
                JsonUtility.ToJson(source)==sourceBefore&&JsonUtility.ToJson(source.attackWaves)==waveSourceBefore);
            yield return WaveEvidenceCapture(directory,"09-restarted-preparation",check);
            result.checkpoints=checkpoints.ToArray();result.runs=runs.ToArray();
        }

        public IEnumerator VerifyWaveBattle()
        {
            string directory=WaveEvidenceDirectory();Directory.CreateDirectory(directory);
            var result=new WaveEvidenceResult{screenWidth=Screen.width,screenHeight=Screen.height};
            var checks=new List<string>();bool passed=true;float previousTimeScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);passed&=ok;Debug.Log("WAVE_BATTLE_CHECK "+name+"="+ok);};
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
            var stack=new Stack<IEnumerator>();stack.Push(WaveEvidenceChecks(directory,result,check));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    result.error=failure.ToString();passed=false;Debug.LogException(failure);
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                    File.WriteAllText(Path.Combine(directory,"exception.txt"),result.error);break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            CancelShopDrag();Time.timeScale=previousTimeScale;result.passed=passed&&string.IsNullOrEmpty(result.error);result.checks=checks.ToArray();
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("WAVE_BATTLE_RESULT: passed="+result.passed+"; checks="+checks.Count+"; noInjectedDamage=True; noForcedVictory=True");
            Application.Quit(result.passed?0:1);
        }
    }

    public sealed class WaveBattleSmokeDriver:MonoBehaviour
    {
        static bool started;
        public static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-waveBattleSmokeTest"||arg=="-blackForestSmokeTest");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare(){if(Requested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(started||!Debug.isDebugBuild||!Requested)return;
            started=true;var go=new GameObject("Wave battle verification");DontDestroyOnLoad(go);go.AddComponent<WaveBattleSmokeDriver>();
        }
        IEnumerator Start()
        {
            ManualBattleController controller=null;double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(SceneManager.GetActiveScene().name==BlackForestLayout.SceneName&&controller!=null&&!controller.ArtLoading&&
                    controller.BlackForestChallengeActive&&controller.Battle!=null&&controller.Battle.WavesEnabled)break;
                yield return null;
            }
            if(controller==null||!controller.BlackForestChallengeActive||controller.Battle==null||!controller.Battle.WavesEnabled)
            {
                string directory=ManualBattleController.WaveEvidenceDirectory();Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"result.json"),"{\"passed\":false,\"error\":\"Production Black Forest wave startup timed out\"}");
                Debug.LogError("WAVE_BATTLE_RESULT: production wave startup timed out");Application.Quit(1);yield break;
            }
            yield return controller.VerifyWaveBattle();
        }
    }
}
