using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class WaveBattleTools
    {
        [Serializable] sealed class PolicyResult
        {
            public string policy,outcome;
            public double seconds;
            public bool timedOut,legalGateHistory=true;
            public int deployments,breaches,defense,remainingDP,guardsAlive,playerSkillCasts,defenderSkillCasts;
            public float totalDamage;
            public string[] deploymentLog,damageRanking;
        }
        [Serializable] sealed class PolicyReport
        {
            public string scope="Saved nine-defender Black Forest map and normal ordinary lineup with production wave budgets, deployment cadence, card limits, synergies and combat. Fixed deterministic policies only: no forced result, injected damage, human-play claim or win-rate estimate.";
            public string generatedUtc,unityVersion;
            public float step=.05f;
            public bool sourceConfigurationUnchanged;
            public PolicyResult[] results;
        }
        [Serializable] sealed class StrategyCase
        {
            public int seed;
            public string name;
            public string[] priorityPerWave;
            public float[] delayPerWave;
            public int batchSize;
            public float batchPause;
            public PolicyResult result;
        }
        [Serializable] sealed class StrategySearchReport
        {
            public string scope="Thirty-six reproducible fixed policies with the unchanged ordinary eight-card formation, nine saved defenders, three real 16/16/18 DP wave budgets, real cooldowns and finite deployment uses. Priority subsets only choose which existing cards to deploy; all eight remain in the formation. No changed combat parameter, damage injection, forced result or win-rate estimate. A t0 opening card starts the production-equivalent battle clock; later cards obey each wave delay and batch pause.";
            public string generatedUtc,unityVersion;
            public int baseSeed=20261108,caseCount,winningCases;
            public float step=.05f;
            public bool sourceConfigurationUnchanged;
            public int firstWinningSeed=-1;
            public StrategyCase[] cases;
        }

        static ManualBattle Create(DefenderTrialDefinition encounter,ShopTrialModel.Piece[] pieces)
        {
            var data=pieces.Select(piece=>Resources.Load<ManualBattleDefinition>("MonsterTests/"+piece.Id.Substring("CARD_".Length))).ToArray();
            if(data.Any(definition=>definition==null))throw new InvalidOperationException("A wave policy starter is missing its production definition.");
            var stock=new ShopTrialModel(data.Select(definition=>new ShopTrialModel.Role{
                Id=definition.Card.CardId,Name=definition.Card.DisplayName,Cost=definition.Card.RecruitCost}),61108);
            stock.EnableCampaign(true);stock.Preset(pieces,Array.Empty<string>(),BlackForestChallengePlan.InitialLevel,0,10);stock.Validate();
            var profiles=data.Select((definition,i)=>CampaignBalance.Build(definition,pieces[i].Star,
                BlackForestChallengePlan.InitialDP,BlackForestChallengePlan.Defense,waveMode:true)).ToArray();
            var first=profiles[0];
            var battle=new ManualBattle(first.Unit,encounter.route,data[0].Engagement.DefenderPosition,first.Combat,null,
                BlackForestChallengePlan.CopyDefenders().Select(guard=>guard.Snapshot()).ToArray(),encounter.LaneSnapshot());
            battle.ConfigureCardLoadout(profiles.Skip(1));battle.ConfigureWaves(encounter.WaveSnapshot());battle.ConfigureSynergies(pieces);
            var ground=new List<Vector3>();
            for(int row=1;row<=BlackForestLayout.Height;row++)for(int col=1;col<=BlackForestLayout.Width;col++)
                if(BlackForestLayout.Walkable(col,row))ground.Add(BlackForestLayout.Cell(col,row));
            battle.ConfigureMarineTerrain(ground,new Vector2(0,.5f));
            foreach(var guard in battle.Defenders)battle.ConfigureDefenderHighGround(guard.EntityId,guard.Position.y>.5f);
            return battle;
        }

        static PolicyResult Run(DefenderTrialDefinition encounter,ShopTrialModel.Piece[] pieces,string policy,int delay,bool cheapFirst)
        {
            var battle=Create(encounter,pieces);battle.Begin();long request=0;int cursor=0;
            var log=new List<string>();var result=new PolicyResult{policy=policy};
            var ids=(cheapFirst?pieces.OrderBy(piece=>CampaignBalance.Profiles[piece.Id].Cost).ThenBy(piece=>piece.Id,StringComparer.Ordinal):pieces.AsEnumerable()).Select(piece=>piece.Id).ToArray();
            double next=delay;
            for(int step=0;step<5200&&battle.Phase==BattlePhase.Running;step++)
            {
                double waveStart=0;
                for(int wave=0;wave<battle.CurrentWaveIndex;wave++)waveStart+=battle.WavePlan.Waves[wave].DeploymentSeconds;
                // Ordinary play begins its clock with the first deployment, so even a delayed policy opens at t0.
                bool opener=log.Count==0&&battle.Clock==0;
                if(battle.DeploymentWindowOpen&&(opener||battle.Clock+.000001>=next&&battle.Clock+.000001>=waveStart+delay))
                {
                    next=battle.Clock+.25;
                    for(int tried=0;tried<ids.Length;tried++)
                    {
                        string id=ids[cursor];cursor=(cursor+1)%ids.Length;
                        var lane=battle.Lanes.First(item=>item.Id==battle.CurrentWaveLaneId);
                        if(battle.CheckCardDeployment(id,lane.Entry)!=DeployFailure.None)continue;
                        var receipt=battle.TryDeployRole(battle.RunId,++request,lane.Entry,id);
                        if(!receipt.Success)throw new InvalidOperationException("A validated wave policy deployment failed.");
                        result.legalGateHistory&=battle.FindUnit(receipt.EntityId).RouteId==lane.Id;
                        log.Add(battle.Clock.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"/"+id+"/"+lane.Id+"/dp="+battle.DP);break;
                    }
                }
                foreach(var unit in battle.Units.ToArray())if(unit.Active&&battle.CheckSkill(unit.EntityId)==SkillFailure.None)
                    battle.TryActivateSkill(battle.RunId,++request,unit.EntityId);
                battle.Advance(.05f,.05f);
            }
            result.outcome=battle.Phase.ToString();result.seconds=battle.Clock;result.timedOut=battle.TimedOut;
            result.deployments=battle.Units.Count(unit=>!unit.SynergySupport);result.breaches=battle.Breaches;
            result.defense=battle.Defense;result.remainingDP=battle.DP;result.guardsAlive=battle.Defenders.Count(guard=>guard.Alive);
            result.playerSkillCasts=battle.SkillCasts;result.defenderSkillCasts=battle.Defenders.Sum(guard=>guard.SkillCasts);
            result.totalDamage=battle.TotalPlayerDamage;result.deploymentLog=log.ToArray();
            result.damageRanking=battle.DamageRanking.Select(row=>row.CardId+":"+row.Damage.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return result;
        }

        [MenuItem("Yesterday Convoy/Black Forest/Evaluate Wave Policies")]
        public static void Evaluate()
        {
            var encounter=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(encounter==null||encounter.WaveSnapshot()==null)throw new InvalidOperationException("Saved Black Forest wave scenario is missing.");
            string before=JsonUtility.ToJson(encounter),wavesBefore=JsonUtility.ToJson(encounter.attackWaves);
            var pieces=BlackForestChallengePlan.StartingPieces();
            var results=new[]{Run(encounter,pieces,"ordinary-round-robin-eager",0,false),
                Run(encounter,pieces,"ordinary-cheap-first-round-robin-eager",0,true),Run(encounter,pieces,"ordinary-opener-then-wait-10s-each-wave",10,false)};
            var report=new PolicyReport{generatedUtc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,results=results,
                sourceConfigurationUnchanged=before==JsonUtility.ToJson(encounter)&&wavesBefore==JsonUtility.ToJson(encounter.attackWaves)};
            if(!report.sourceConfigurationUnchanged)throw new InvalidOperationException("Wave evaluation mutated its source configuration.");
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","wave-blackforest"));Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"model-strategies.json"),JsonUtility.ToJson(report,true));
            foreach(var result in results)Debug.Log($"WAVE_POLICY: {result.policy}; {result.outcome}; time={result.seconds:F3}; timedOut={result.timedOut}; deployed={result.deployments}; damage={result.totalDamage}; breaches={result.breaches}; guards={result.guardsAlive}; defense={result.defense}; legalGates={result.legalGateHistory}");
            Debug.Log("WAVE_POLICY_EVALUATION_PASS: three fixed policies; no win-rate claim; sourceUnchanged=True");
        }

        static StrategyCase FixedCase(int index,string name,string[] priorities,int batchSize,float pause,float[] delays)
            =>new StrategyCase{seed=20261108+index,name=name,priorityPerWave=priorities,delayPerWave=delays,batchSize=batchSize,batchPause=pause};

        static PolicyResult RunStrategy(DefenderTrialDefinition encounter,ShopTrialModel.Piece[] pieces,StrategyCase policy)
        {
            var priorities=policy.priorityPerWave.Select(wave=>wave.Split(',').Select(id=>id.StartsWith("CARD_",StringComparison.Ordinal)?id:"CARD_"+id).ToArray()).ToArray();
            if(priorities.Length!=3||policy.delayPerWave.Length!=3||priorities.Any(wave=>wave.Length==0||wave.Distinct().Count()!=wave.Length||wave.Any(id=>!pieces.Any(piece=>piece.Id==id))))
                throw new InvalidOperationException("A fixed wave strategy names an invalid priority or formation card.");
            var battle=Create(encounter,pieces);battle.Begin();long request=0;int waveIndex=-1,inBatch=0;
            double next=0;var log=new List<string>();var result=new PolicyResult{policy=policy.name};
            for(int step=0;step<5200&&battle.Phase==BattlePhase.Running;step++)
            {
                int wave=battle.CurrentWaveIndex;double waveStart=0;
                for(int i=0;i<wave;i++)waveStart+=battle.WavePlan.Waves[i].DeploymentSeconds;
                if(wave!=waveIndex){waveIndex=wave;inBatch=0;next=waveStart+policy.delayPerWave[wave];}
                bool opener=log.Count==0&&battle.Clock==0;
                if(battle.DeploymentWindowOpen&&(opener||battle.Clock+.000001>=next))
                {
                    next=battle.Clock+.25;
                    var lane=battle.Lanes.First(item=>item.Id==battle.CurrentWaveLaneId);
                    foreach(string id in priorities[wave])
                    {
                        if(battle.CheckCardDeployment(id,lane.Entry)!=DeployFailure.None)continue;
                        var receipt=battle.TryDeployRole(battle.RunId,++request,lane.Entry,id);
                        if(!receipt.Success)throw new InvalidOperationException("A validated search deployment failed.");
                        result.legalGateHistory&=battle.FindUnit(receipt.EntityId).RouteId==lane.Id;
                        log.Add(battle.Clock.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"/"+id+"/"+lane.Id+"/dp="+battle.DP+"/uses="+battle.FindCard(id).RemainingUses);
                        if(++inBatch>=policy.batchSize){inBatch=0;next=Math.Max(next,battle.Clock+policy.batchPause);}
                        // At t0 only the opener may bypass a configured first-wave delay.
                        if(opener)next=Math.Max(next,policy.delayPerWave[0]);
                        break;
                    }
                }
                foreach(var unit in battle.Units.ToArray())if(unit.Active&&battle.CheckSkill(unit.EntityId)==SkillFailure.None)
                    battle.TryActivateSkill(battle.RunId,++request,unit.EntityId);
                battle.Advance(.05f,.05f);
            }
            result.outcome=battle.Phase.ToString();result.seconds=battle.Clock;result.timedOut=battle.TimedOut;
            result.deployments=battle.Units.Count(unit=>!unit.SynergySupport);result.breaches=battle.Breaches;
            result.defense=battle.Defense;result.remainingDP=battle.DP;result.guardsAlive=battle.Defenders.Count(guard=>guard.Alive);
            result.playerSkillCasts=battle.SkillCasts;result.defenderSkillCasts=battle.Defenders.Sum(guard=>guard.SkillCasts);
            result.totalDamage=battle.TotalPlayerDamage;result.deploymentLog=log.ToArray();
            result.damageRanking=battle.DamageRanking.Select(row=>row.CardId+":"+row.Damage.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return result;
        }

        [MenuItem("Yesterday Convoy/Black Forest/Search Fixed Wave Strategies")]
        public static void EvaluateStrategies()
        {
            var encounter=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(encounter==null||encounter.WaveSnapshot()==null)throw new InvalidOperationException("Saved Black Forest wave scenario is missing.");
            string before=JsonUtility.ToJson(encounter),wavesBefore=JsonUtility.ToJson(encounter.attackWaves);
            var pieces=BlackForestChallengePlan.StartingPieces();var cases=new List<StrategyCase>();
            // Small firepower/frontline subsets permit waiting for a preferred card instead of immediately spending on every fallback.
            string[][] fixedPriorities={
                new[]{"M07,M08,M16,M15","M16,M08,M15,M07","M15,M08,M07,M16"},
                new[]{"M08,M16,M07","M16,M08,M10","M08,M15,M07"},
                new[]{"M10,M07,M08,M16","M16,M08,M15,M38","M08,M15,M07,M04"},
                new[]{"M16,M08,M07,M15","M08,M16,M10,M07","M15,M07,M08,M16"},
                new[]{"M07,M15,M08","M08,M16,M38","M15,M08,M16"},
                new[]{"M08,M07,M16","M16,M15,M08","M07,M15,M08"}
            };
            for(int i=0;i<fixedPriorities.Length;i++)
            {
                cases.Add(FixedCase(cases.Count,"targeted-"+i+"-eager-priority",fixedPriorities[i],99,0,new[]{0f,0,0}));
                cases.Add(FixedCase(cases.Count,"targeted-"+i+"-pairs-and-delays",fixedPriorities[i],2,6,new[]{0f,5,10}));
            }
            for(int i=0;i<24;i++)
            {
                int seed=20261108+cases.Count;var random=new System.Random(seed);var priorities=new string[3];
                for(int wave=0;wave<3;wave++)
                {
                    string[] ids=pieces.Select(piece=>piece.Id).ToArray();
                    for(int position=ids.Length-1;position>0;position--){int swap=random.Next(position+1);string value=ids[position];ids[position]=ids[swap];ids[swap]=value;}
                    priorities[wave]=string.Join(",",ids.Take(3+random.Next(4)).Select(id=>id.Substring("CARD_".Length)));
                }
                cases.Add(new StrategyCase{seed=seed,name="seeded-wave-priorities-"+seed,priorityPerWave=priorities,
                    delayPerWave=new[]{0f,5f*random.Next(3),5f*random.Next(3)},batchSize=2+random.Next(3),batchPause=new[]{0f,6,10,16}[random.Next(4)]});
            }
            foreach(var policy in cases)
            {
                policy.result=RunStrategy(encounter,pieces,policy);
                Debug.Log($"WAVE_STRATEGY: seed={policy.seed}; {policy.name}; {policy.result.outcome}; time={policy.result.seconds:F3}; deployed={policy.result.deployments}; breaches={policy.result.breaches}; defense={policy.result.defense}; guards={policy.result.guardsAlive}; legalGates={policy.result.legalGateHistory}");
            }
            var winner=cases.FirstOrDefault(policy=>policy.result.outcome==BattlePhase.Won.ToString());
            var report=new StrategySearchReport{generatedUtc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,
                caseCount=cases.Count,winningCases=cases.Count(policy=>policy.result.outcome==BattlePhase.Won.ToString()),
                firstWinningSeed=winner?.seed??-1,cases=cases.ToArray(),
                sourceConfigurationUnchanged=before==JsonUtility.ToJson(encounter)&&wavesBefore==JsonUtility.ToJson(encounter.attackWaves)};
            if(!report.sourceConfigurationUnchanged)throw new InvalidOperationException("Wave strategy search mutated its source configuration.");
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","wave-blackforest"));Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"model-strategy-search.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"WAVE_STRATEGY_SEARCH_COMPLETE: cases={report.caseCount}; fixedPolicyWins={report.winningCases}; firstWinningSeed={report.firstWinningSeed}; sourceUnchanged=True; originalThreePolicyReportPreserved=True");
        }

        [MenuItem("Yesterday Convoy/Black Forest/Build Wave Trial")]
        public static void BuildWindows()
        {
            var encounter=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(encounter==null||encounter.WaveSnapshot()==null)throw new InvalidOperationException("Saved wave scenario is missing.");
            PreparedArtTools.PrepareAll();InitialDemoTools.Validate();
            const string directory="Builds/Windows-WaveBattle";Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=GameSceneCatalog.BuildPaths(),locationPathName=directory+"/YesterdayConvoy.exe",
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Wave build failed: "+report.summary.result);
            Debug.Log("WAVE_BATTLE_BUILD_PASS: "+report.summary.totalSize+" bytes; "+report.summary.totalErrors+" errors.");
        }
    }
}
