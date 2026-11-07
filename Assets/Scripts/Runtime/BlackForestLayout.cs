using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Coordinates in the approved draft: columns A-Q, rows 01-14 north to south.
    public static class BlackForestLayout
    {
        public const string Id="BLACKFOREST_CROSSING",SceneName="BlackForestCrossing",Resource="Maps/BlackForestCrossing";
        public const int Width=17,Height=14;
        static readonly string[] tiles={"######.##########","######.##########",".....H.H.....HHH#","#HHH.H.H.HHH.HHH#","#H.....H.H.......","#H.H.HHH.H.H.H.H#","#H.......H...H.H#","#H.H.#HgH#.g.g.H#","#H.........H.H.H#","#H.g.#Hg.#Hg.H.H#","...H.HHH.HHH.....","#HHH.#HH.#HH.HHH#","#HHH.........HHH#","#################"};
        static readonly string[][] points={
            new[]{"A03","E03","E07","I07","I03","M03","M11","O11","O05","Q05"},
            new[]{"A11","C11","C07","E07","E13","I13","I09","K09","K05","O05","O11","Q11"},
            new[]{"G01","G05","C05","C09","I09","I13","M13","M07","K07","K05","Q05"}};
        public static Vector3 Cell(int col,int row,float height=0)=>new Vector3(col-9,height,7.5f-row);
        public static Vector3 Cell(string coordinate)=>Cell(coordinate[0]-'A'+1,int.Parse(coordinate.Substring(1)));
        public static char Tile(int col,int row)=>col<1||col>Width||row<1||row>Height?'#':tiles[row-1][col-1];
        public static bool Walkable(int col,int row)=>Tile(col,row)=='.'||Tile(col,row)=='g';
        public static BlackForestSurface Surface(int col,int row)
        {
            if(Tile(col,row)=='H')return BlackForestSurface.HighGround;
            if(!Walkable(col,row))return BlackForestSurface.Inaccessible;
            if(col==3&&row>=5&&row<=10||row==5&&col>=4&&col<=6)return BlackForestSurface.Blackflow;
            if(row==13&&col>=5&&col<=8||col==5&&row==12)return BlackForestSurface.Amber;
            if(col==11&&row>=5&&row<=7||col==13&&row>=7&&row<=9)return BlackForestSurface.RootVein;
            return BlackForestSurface.Moss;
        }
        public static TacticalLane[] Lanes()
        {
            var result=new TacticalLane[3];
            for(int i=0;i<3;i++){
                var p=new Vector3[points[i].Length];for(int j=0;j<p.Length;j++)p[j]=Cell(points[i][j]);
                result[i]=new TacticalLane{id="R"+(i+1),exitId=i==1?"B2":"B1",points=p};
            }
            return result;
        }
        public static void Validate()
        {
            int i=0;foreach(var lane in Lanes()){
                var visited=new HashSet<Vector2Int>();float length=0;Vector3 previous=lane.points[0];
                for(int n=0;n<lane.points.Length;n++){
                    var p=lane.points[n];int x=Mathf.RoundToInt(p.x+9),y=Mathf.RoundToInt(7.5f-p.z);
                    if(n==0){visited.Add(new Vector2Int(x,y));continue;}
                    int u=Mathf.RoundToInt(previous.x+9),v=Mathf.RoundToInt(7.5f-previous.z);
                    if(u!=x&&v!=y)throw new InvalidOperationException("Diagonal forest route");
                    while(u!=x||v!=y){u+=Math.Sign(x-u);v+=Math.Sign(y-v);if(!Walkable(u,v)||!visited.Add(new Vector2Int(u,v)))throw new InvalidOperationException("Forest route crosses blocked terrain or itself");length++;}
                    previous=p;
                }
                if(length!=new[]{38,40,42}[i++])throw new InvalidOperationException("Forest route length drift");
            }
        }
    }
    public enum BlackForestSurface{Moss,Blackflow,Amber,RootVein,HighGround,Inaccessible}
}
