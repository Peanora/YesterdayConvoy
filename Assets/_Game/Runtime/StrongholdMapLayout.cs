using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // Factual tile transcription of act1autochess_m04, rows 5..1, columns 2..18.
    // Runtime geometry is ours; no original map texture or client file is shipped.
    public static class StrongholdMapLayout
    {
        public const string Id="stronghold04";
        public static readonly string[] Rows={"#h.###.xOx.###.h#","#h..a..xxx..a..h#","#h..a..xxx..a..h#","E..###..O..###..E","#Ibbbbb###bbbbbI#"};
        public static Vector3 Cell(int col,int row,float height=.04f)=>new Vector3(col-10,height,row-3);
        public static char Tile(int col,int row)=>col<2||col>18||row<1||row>5?'#':Rows[5-row][col-2];
        public static Vector3[] InfectionTiles=>new[]{Cell(6,4),Cell(6,3),Cell(14,4),Cell(14,3)};
        static Vector3[] Points(params int[] cells)=>Enumerable.Range(0,cells.Length/2).Select(i=>Cell(cells[i*2],cells[i*2+1])).ToArray();
        public static TacticalLane[] Lanes()=>new[]{
            new TacticalLane{id="南门 · 右翼后传送",exitId="西侧蓝门",points=Points(10,2,12,2,12,4,16,4,16,2,17,2,17,1,10,5,10,4,8,4,8,3,4,3,4,2,2,2)},
            new TacticalLane{id="北门 · 左翼后传送",exitId="东侧蓝门",points=Points(10,5,10,4,8,4,4,4,4,2,3,2,3,1,10,5,10,3,12,3,16,3,16,2,18,2)}};
        public static DefenderTrialDefinition Create(DefenderTrialDefinition template)
        {
            var result=UnityEngine.Object.Instantiate(template);result.name=Id;result.mapId=Id;
            result.mapTitle="卫戍协议·盟约 #04 / 活性源石";result.autumnRuins=false;result.lanes=Lanes();result.route=result.lanes[0].points;
            result.raised=Array.Empty<Vector2Int>();
            var positions=new[]{Cell(5,3),Cell(15,4),Cell(3,4),Cell(17,3),Cell(6,4),Cell(14,3),Cell(7,4),Cell(13,3)};
            result.defenders=template.defenders.Select((d,i)=>{
                var copy=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(d));copy.position=positions[i];return copy;
            }).ToArray();
            return result;
        }
        // Trial scale only. All authored character assets and other maps retain their values.
        public static ManualBattleSettings TrialSettings(ManualBattleSettings s)=>new ManualBattleSettings(s.CardId,s.UnitId,40,2,8,s.DeploymentCost,s.Cooldown,s.Health*4,s.Speed,s.GateDamage,s.TowerRange,s.TowerDamage,s.ShotInterval);
    }
}
