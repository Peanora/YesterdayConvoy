using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Fixed-policy comparisons against the saved encounter; never edits game assets.</summary>
    public static class BlackForestRosterTools
    {
        const float Step=.05f, Horizon=300;
        static readonly int[] SplitLanes={0,1,2,1,0,2,1,1};

        [Serializable] sealed class TrialResult
        {
            public string roster,strategy,outcome,stopReason;
            public string[] lineup;
            public double time,followupStartedAt=-1;
            public int dp,defense,breaches,guardAlive,policyDeployments,ordinaryEntities,supportEntities,activeEntities;
            public int playerSkillCasts,defenderSkillCasts,musters,orders,orderParticipants,shatters,weatherPulses;
            public int reunionKinds,reunionTier,reunionStars,snowTier,snowStars,guerrillaTier,infectionTier;
            public float defenderBaseResistance;
        }
        [Serializable] sealed class ComparisonReport
        {
            public string scope="Real ManualBattle with the saved nine-defender Black Forest encounter, normal Campaign profiles, 50 DP and six shared defense. Fixed deterministic deployment/skill policies only; these runs are not human play, win rates, or a proof that every ordinary roster can win. No injected HP/damage, forced results, sandbox stat multipliers, or retired Stronghold map rules.";
            public string policy="Every case snapshots the complete eight-card formation. first-three-R1/R2/R3 deploy only its first three cards once at time zero on that lane. full-eight-split-once deploys all eight at time zero on lanes 0,1,2,1,0,2,1,1. full-eight-split-budget adds at most one legal round-robin deployment every 0.25 seconds on the same per-card lane. full-eight-split-delayed-followup waits until every defender is dead or naturally departed before adding legal round-robin deployments to R1 every 0.25 seconds. Followups respect actual DP, uses and cooldown. Every case attempts all legal ready manual skills and advances at 1x for at most 300 simulation seconds.";
            public string generatedUtc,unityVersion;
            public float step=Step,horizon=Horizon;
            public bool sourceConfigurationUnchanged;
            public TrialResult[] results;
        }

        static ShopTrialModel.Piece Piece(string id,int star)=>new ShopTrialModel.Piece{Id="CARD_"+id,Star=star};
        static ShopTrialModel.Piece[] OldPieces()=>new[]{
            Piece("M18",2),Piece("M16",2),Piece("M19",2),Piece("M06",2),
            Piece("M11",3),Piece("M12",2),Piece("M13",2),Piece("M14",1)
        };

        static ManualBattle Create(DefenderTrialDefinition encounter,ShopTrialModel.Piece[] pieces)
        {
            var definitions=pieces.Select(piece=>
            {
                string resource="MonsterTests/"+piece.Id.Substring("CARD_".Length);
                var definition=Resources.Load<ManualBattleDefinition>(resource);
                if(definition==null||definition.Card.CardId!=piece.Id)throw new InvalidOperationException("Missing roster definition: "+resource);
                return definition;
            }).ToArray();
            // Validate real copy reservation as the playable preparation shop does.
            var shop=new ShopTrialModel(definitions.Select(definition=>new ShopTrialModel.Role{
                Id=definition.Card.CardId,Name=definition.Card.DisplayName,Cost=definition.Card.RecruitCost}),61108);
            shop.EnableCampaign(true);shop.Preset(pieces,Array.Empty<string>(),BlackForestChallengePlan.InitialLevel,0,10);shop.Validate();
            var profiles=definitions.Select((definition,i)=>CampaignBalance.Build(definition,pieces[i].Star,
                BlackForestChallengePlan.TotalDP,BlackForestChallengePlan.Defense)).ToArray();
            var guards=BlackForestChallengePlan.CopyDefenders().Select(guard=>guard.Snapshot()).ToArray();
            var first=profiles[0];
            var battle=new ManualBattle(first.Unit,encounter.route,definitions[0].Engagement.DefenderPosition,
                first.Combat,null,guards,encounter.LaneSnapshot());
            battle.ConfigureCardLoadout(profiles.Skip(1));
            battle.ConfigureSynergies(pieces);
            var ground=new List<Vector3>();
            for(int row=1;row<=BlackForestLayout.Height;row++)for(int col=1;col<=BlackForestLayout.Width;col++)
                if(BlackForestLayout.Walkable(col,row))ground.Add(BlackForestLayout.Cell(col,row));
            battle.ConfigureMarineTerrain(ground,new Vector2(0,.5f));
            foreach(var guard in battle.Defenders)battle.ConfigureDefenderHighGround(guard.EntityId,guard.Position.y>.5f);
            return battle;
        }

        static bool Deploy(ManualBattle battle,string id,int lane,ref long request)
        {
            var entry=battle.Lanes[lane].Entry;
            if(battle.CheckCardDeployment(id,entry)!=DeployFailure.None)return false;
            return battle.TryDeployRole(battle.RunId,++request,entry,id).Success;
        }

        static TrialResult Run(DefenderTrialDefinition encounter,string roster,ShopTrialModel.Piece[] pieces,int singleLane,bool spendBudget,bool delayedFollowup=false)
        {
            var battle=Create(encounter,pieces);battle.Begin();long request=0;
            int deployments=0,count=singleLane>=0?3:pieces.Length;
            for(int i=0;i<count;i++)
            {
                if(!Deploy(battle,pieces[i].Id,singleLane>=0?singleLane:SplitLanes[i],ref request))
                    throw new InvalidOperationException("The fixed opening could not deploy "+pieces[i].Id);
                deployments++;
            }
            int cursor=0;double nextDeployment=.25,followupStartedAt=-1;
            for(int frame=0;frame<6000&&battle.Phase==BattlePhase.Running;frame++)
            {
                if((spendBudget||delayedFollowup&&battle.Defenders.All(guard=>!guard.Alive))&&battle.Clock+.000001>=nextDeployment)
                {
                    nextDeployment=delayedFollowup?battle.Clock+.25:nextDeployment+.25;
                    for(int attempt=0;attempt<pieces.Length;attempt++)
                    {
                        int index=cursor;cursor=(cursor+1)%pieces.Length;
                        if(!Deploy(battle,pieces[index].Id,delayedFollowup?0:SplitLanes[index],ref request))continue;
                        if(delayedFollowup&&followupStartedAt<0)followupStartedAt=battle.Clock;
                        deployments++;break;
                    }
                }
                foreach(var unit in battle.Units.ToArray())
                    if(unit.Active&&battle.CheckSkill(unit.EntityId)==SkillFailure.None)
                        battle.TryActivateSkill(battle.RunId,++request,unit.EntityId);
                battle.Advance(Step,Step);
            }
            return new TrialResult{
                roster=roster,strategy=singleLane>=0?"first-three-R"+(singleLane+1):delayedFollowup?"full-eight-split-delayed-followup":spendBudget?"full-eight-split-budget":"full-eight-split-once",
                outcome=battle.Phase.ToString(),stopReason=battle.Phase==BattlePhase.Running?"300-second-horizon":"natural-terminal-result",
                lineup=pieces.Select(piece=>piece.Id+"/"+piece.Star+"star").ToArray(),time=battle.Clock,followupStartedAt=followupStartedAt,dp=battle.DP,defense=battle.Defense,
                breaches=battle.Breaches,guardAlive=battle.Defenders.Count(guard=>guard.Alive),policyDeployments=deployments,
                ordinaryEntities=battle.Units.Count(unit=>!unit.SynergySupport),supportEntities=battle.Units.Count(unit=>unit.SynergySupport),activeEntities=battle.ActiveCount,
                playerSkillCasts=battle.SkillCasts,defenderSkillCasts=battle.Defenders.Sum(guard=>guard.SkillCasts),musters=battle.MusterUsed,
                orders=battle.OrdersUsed,orderParticipants=battle.OrderStrikes,shatters=battle.Shatters,weatherPulses=battle.SnowstormPulses.Count,
                reunionKinds=battle.ReunionCount,reunionTier=battle.ReunionTier,reunionStars=battle.ReunionTotalStars,
                snowTier=battle.SnowTier,snowStars=battle.SnowTotalStars,guerrillaTier=battle.GuerrillaTier,infectionTier=battle.InfectionTier,
                defenderBaseResistance=battle.CombatSettings.Effects.Resistance
            };
        }

        [MenuItem("Yesterday Convoy/Black Forest/Compare Starting Rosters")]
        public static void Evaluate()
        {
            var encounter=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(encounter==null)throw new InvalidOperationException("Missing saved Black Forest encounter.");
            string before=JsonUtility.ToJson(encounter);
            var results=new List<TrialResult>();
            foreach(bool old in new[]{true,false})
            {
                string name=old?"old-completed-eight":"ordinary-eight";
                var pieces=old?OldPieces():BlackForestChallengePlan.StartingPieces();
                for(int lane=0;lane<3;lane++)results.Add(Run(encounter,name,pieces,lane,false));
                results.Add(Run(encounter,name,pieces,-1,false));
                results.Add(Run(encounter,name,pieces,-1,true));
                results.Add(Run(encounter,name,pieces,-1,false,true));
            }
            var report=new ComparisonReport{generatedUtc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,
                sourceConfigurationUnchanged=before==JsonUtility.ToJson(encounter),results=results.ToArray()};
            if(!report.sourceConfigurationUnchanged)throw new InvalidOperationException("The comparison mutated the source encounter.");
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","blackforest-feedback","roster-comparison.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
            foreach(var row in results)Debug.Log($"BLACK_FOREST_ROSTER: {row.roster}; {row.strategy}; {row.outcome}; time={row.time:F2}; dp={row.dp}; defense={row.defense}; breaches={row.breaches}; guards={row.guardAlive}; deployments={row.policyDeployments}; skills={row.playerSkillCasts}/{row.defenderSkillCasts}; muster={row.musters}; orders={row.orders}; shatters={row.shatters}; weather={row.weatherPulses}");
            Debug.Log("BLACK_FOREST_ROSTER_COMPARISON: "+path+"; fixed policies only, not win rates; sourceUnchanged=True");
        }
    }
}
