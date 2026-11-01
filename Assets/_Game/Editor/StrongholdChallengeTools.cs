using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class StrongholdChallengeTools
    {
        public static void Balance()
        {
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/shop-map"));Directory.CreateDirectory(dir);
            string[][] teams={new[]{"M08"},new[]{"M02"},new[]{"M18"},new[]{"M08","M02","M05","M18","M17"},new[]{"M16","M05","M18","M17","M19"},new[]{"M02","M05","M18","M17","M19"}};
            var lines=new System.Collections.Generic.List<string>{"team,spacing,delay,laneMode,phase,time,used,breaches,dp,aliveDefenders,casts"};
            int wins=0;var random=new System.Random(4126);
            for(int sample=0;sample<180;sample++)
            {
                string[] team=teams[sample<6?sample:3+sample%3];float spacing=new[]{.8f,1.2f,2f,3f}[sample%4],delay=new[]{0,10,24,45,65}[sample%5];int mode=sample%3;
                var orders=new System.Collections.Generic.List<StrongholdChallengePlan.Order>();
                for(int i=0;i<28;i++)orders.Add(new StrongholdChallengePlan.Order{time=i==0?0:delay+i*spacing,card="CARD_"+team[sample<6?i%team.Length:random.Next(team.Length)],lane=mode==0?i%2:mode==1?0:1});
                var plan=new StrongholdChallengePlan.Plan{orders=orders.ToArray()};var b=StrongholdChallengePlan.Run(plan);
                lines.Add($"{string.Join('+',team)},{spacing},{delay},{mode},{b.Phase},{b.Clock:F2},{b.Units.Count},{b.Breaches},{b.DP},{b.Defenders.Count(d=>d.Alive)},{b.Defenders.Sum(d=>d.SkillCasts)}");
                if(b.Phase==BattlePhase.Won)
                {
                    wins++;if(wins<=3)File.WriteAllText(Path.Combine(dir,"challenge-win-"+wins+".json"),JsonUtility.ToJson(plan,true));
                    if(wins==1)
                    {
                        var accepted=new StrongholdChallengePlan.Plan{orders=b.Units.Select(u=>new StrongholdChallengePlan.Order{time=(float)u.SpawnTime,card=u.CardId,lane=b.Lanes[0].Id==u.RouteId?0:1}).ToArray()};
                        var replay=StrongholdChallengePlan.Run(accepted);
                        if(replay.Phase!=BattlePhase.Won)throw new InvalidOperationException("Accepted orders must replay without failed requests.");
                        File.WriteAllText("Assets/_Game/Data/Resources/StrongholdChallengeSolution.json",JsonUtility.ToJson(accepted,true));
                        Debug.Log($"CHALLENGE_REFERENCE: {replay.Phase}; time={replay.Clock}; units={replay.Units.Count}; dp={replay.DP}; trips={replay.StrongholdPortalTrips}");
                    }
                }
            }
            File.WriteAllLines(Path.Combine(dir,"challenge-balance.csv"),lines);Debug.Log("CHALLENGE_BALANCE: wins="+wins+" /180; "+string.Join("; ",lines.Take(7)));
        }
    }
}
