using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    [CreateAssetMenu(menuName="Yesterday Convoy/Approved Tactical Map")]
    public sealed class ApprovedMapDefinition : ScriptableObject
    {
        public int index,width=14,height=10;
        public string id,title,sceneName;
        public string[] gridRows;
        public float platformHeight=.72f;
        public DefenderTrialDefinition encounter;
        public Texture2D preview;
        public ApprovedMapMechanics mechanics=new ApprovedMapMechanics();
        public Vector2 MarineOrigin=>new Vector2((width%2==0)?.5f:0,(height%2==0)?.5f:0);
        public Vector3 Cell(int col,int row,float height=0)=>new Vector3(col-(width-1)*.5f,height,(this.height-1)*.5f-row);
        public char Tile(int col,int row)=>gridRows[row][col];
        public bool Walkable(int col,int row)=>col>=0&&row>=0&&col<width&&row<height&&Tile(col,row)!='H'&&Tile(col,row)!='P'&&Tile(col,row)!='W';
        public IEnumerable<Vector3> WalkablePoints()
        {for(int row=0;row<height;row++)for(int col=0;col<width;col++)if(Walkable(col,row))yield return Cell(col,row);}
        public IEnumerable<Vector3> FramePoints()
        {
            for(int row=0;row<height;row++)for(int col=0;col<width;col++)
            {
                var p=Cell(col,row,Tile(col,row)=='H'?platformHeight:0);
                foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})yield return p+new Vector3(x,0,z);
            }
        }
        public DefenderPlacement[] CopyDefenders()=>Array.ConvertAll(encounter.defenders,d=>JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(d)));
        public void Validate()
        {
            if(index<1||index>8||string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(sceneName)||
                width!=14||height!=10||gridRows==null||gridRows.Length!=height||gridRows.Any(r=>r==null||r.Length!=width)||encounter==null||encounter.mapId!=id||!encounter.Tactical)
                throw new InvalidOperationException("Incomplete approved map: "+name);
            _=encounter.Snapshot();_=encounter.LaneSnapshot();
        }
    }
    public static class ApprovedMapCatalog
    {
        public static ApprovedMapDefinition[] All=>Resources.LoadAll<ApprovedMapDefinition>("ApprovedMaps").OrderBy(m=>m.index).ToArray();
        public static ApprovedMapDefinition Find(string id)=>string.IsNullOrEmpty(id)?null:All.FirstOrDefault(m=>m.id==id);
        public static bool IsMap(string id)=>!string.IsNullOrEmpty(id)&&id.StartsWith("approved-map-",StringComparison.Ordinal)&&Find(id)!=null;
    }
    public static class ApprovedMapPlan
    {
        public const int InitialDP=50,Defense=6,InitialLevel=8;
        public static string[] StartingIds=>new[]{"CARD_M07","CARD_M08","CARD_M09","CARD_M10","CARD_M02","CARD_M03","CARD_M04","CARD_M05"};
        public static ShopTrialModel.Piece[] StartingPieces()=>StartingIds.Select((id,i)=>new ShopTrialModel.Piece{Id=id,Star=i<2?2:1}).ToArray();
    }
}
