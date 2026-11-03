using System;
using UnityEngine;
namespace YesterdayConvoy
{
    // Trial tuning; same approved large board, independently configured basic defenders.
    public static class BattleShopStages
    {
        public static DefenderPlacement[] Defenders(int stage,bool lateGame=false)
        {
            if(stage<1||stage>2)throw new ArgumentOutOfRangeException(nameof(stage));
            if(lateGame)
            {
                var map=StrongholdChallengeLayout.Create(Resources.Load<DefenderTrialDefinition>("MapTrials/city"));
                var guards=map.defenders;
                if(stage==2)
                {
                    // Switch the supporting ground wings and the two northern high-ground roles.
                    // Geometry, portals and complete authored skill configurations stay intact.
                    Vector3 ground=guards[1].position;guards[1].position=guards[4].position;guards[4].position=ground;
                    Vector3 platform=guards[0].position;guards[0].position=guards[3].position;guards[3].position=platform;
                }
                if(Application.isPlaying)UnityEngine.Object.Destroy(map);else UnityEngine.Object.DestroyImmediate(map);
                return guards;
            }
            return new[]{Guard("loop-left","D01","米格鲁",4,3,stage==1?45:95),Guard("loop-right","D04","玫兰莎",16,3,stage==1?40:85)};
        }
        static DefenderPlacement Guard(string id,string art,string name,int col,int row,float hp)=>new DefenderPlacement{
            id=id,artId=art,displayName=name,position=StrongholdMapLayout.Cell(col,row),health=hp,attack=4,armor=1,range=.85f,
            interval=2,windup=.3f,actionDuration=1.2f,blocks=1};
        public static ManualBattleSettings Settings(ManualBattleSettings s,bool lateGame=false)=>lateGame?StrongholdChallengeLayout.Settings(s):new ManualBattleSettings(s.CardId,s.UnitId,12,2,6,s.DeploymentCost,s.Cooldown,Mathf.Max(240,s.Health*3),s.Speed,s.GateDamage,s.TowerRange,s.TowerDamage,s.ShotInterval);
    }
}
