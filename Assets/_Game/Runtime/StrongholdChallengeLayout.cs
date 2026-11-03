using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // An explicitly authored expansion, not a transcription of the original battlefield.
    public static class StrongholdChallengeLayout
    {
        public const string Id="stronghold04-challenge";
        public static bool IsStronghold(string id)=>id==Id||id==StrongholdMapLayout.Id;
        public static char Tile(int col,int row)
        {
            if(col<2||col>18||row< -1||row>7)return '#';
            if((row==6||row==0)&&new[]{6,7,13,14}.Contains(col))return 'H';
            if(row==7)return col==4?'E':'.';
            if(row== -1)return col==16?'E':'.';
            if(row==6||row==0)return '.';
            char tile=StrongholdMapLayout.Tile(col,row);
            return tile=='E'?'#':tile=='b'?'.':tile;
        }
        static Vector3[] Points(params int[] p)=>Enumerable.Range(0,p.Length/2).Select(i=>StrongholdMapLayout.Cell(p[2*i],p[2*i+1])).ToArray();
        public static TacticalLane[] Lanes()=>new[]{
            new TacticalLane{id="北门 · 左翼突破",exitId="东南目标",points=Points(10,7,10,6,10,4,8,4,4,4,4,2,3,2,3,1,10,5,10,3,12,3,16,3,16,-1)},
            new TacticalLane{id="南门 · 右翼突破",exitId="西北目标",points=Points(10,-1,10,0,12,0,12,2,12,4,16,4,16,2,17,2,17,1,10,5,10,6,8,6,8,7,4,7)}};
        // Portal endpoints are identified geometrically, never by a brittle segment number.
        public static int PortalSource(System.Collections.Generic.IReadOnlyList<Vector3> points)
        {
            for(int i=0;i<points.Count-1;i++)if(Mathf.Abs(points[i].z+2)<.1f&&Mathf.Abs(Mathf.Abs(points[i].x)-7)<.1f)return i;
            return 6;
        }
        public static DefenderTrialDefinition Create(DefenderTrialDefinition template)
        {
            var result=UnityEngine.Object.Instantiate(template);result.name=Id;result.mapId=Id;
            result.mapTitle="盟约#04 · 六星双翼挑战（扩建版）";result.autumnRuins=false;result.lanes=Lanes();result.route=result.lanes[0].points;
            result.raised=Array.Empty<Vector2Int>();
            string[] ids={"D21","D22","D27","D28","D29","D30"};
            var positions=new[]{StrongholdMapLayout.Cell(7,6,.76f),StrongholdMapLayout.Cell(4,3),StrongholdMapLayout.Cell(13,0,.76f),StrongholdMapLayout.Cell(14,6,.76f),StrongholdMapLayout.Cell(16,3),StrongholdMapLayout.Cell(10,4)};
            float[] hp={720,1100,650,720,900,950},attack={9,16,10,10,24,12},armor={2,12,3,4,10,10},range={2.6f,.9f,2.3f,2.6f,.9f,.9f};
            result.defenders=ids.Select((id,i)=>{
                var source=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);
                if(source==null)throw new InvalidOperationException("Missing completed defender: "+id);
                var d=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(source.defenders[0]));
                d.position=positions[i];d.health=hp[i];d.attack=attack[i];d.armor=armor[i];d.range=range[i];return d;
            }).ToArray();
            return result;
        }
        public static ManualBattleSettings Settings(ManualBattleSettings s)=>new ManualBattleSettings(s.CardId,s.UnitId,24,2,4,s.DeploymentCost,s.Cooldown,s.Health*3.6f,s.Speed,s.GateDamage,s.TowerRange,s.TowerDamage,s.ShotInterval);
    }
}
