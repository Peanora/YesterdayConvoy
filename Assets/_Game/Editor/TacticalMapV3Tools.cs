using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class TacticalMapV3Tools
    {
        [MenuItem("Yesterday Convoy/Tactical Maps/Apply Approved V3")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach(string id in new[]{"city","ruins"})
            {
                var map=Resources.Load<DefenderTrialDefinition>("MapTrials/"+id);if(map==null)throw new InvalidOperationException("Missing V2 map.");
                var route=new HashSet<Vector2Int>();
                foreach(var lane in map.lanes)for(int i=1;i<lane.points.Length;i++)
                {
                    var a=lane.points[i-1];var b=lane.points[i];int length=Mathf.RoundToInt(Vector3.Distance(a,b));
                    for(int j=0;j<=length;j++){var p=Vector3.Lerp(a,b,j/(float)length);route.Add(new Vector2Int(Mathf.RoundToInt(p.x+8.5f),Mathf.RoundToInt(5-p.z)));}
                }
                var high=new List<Vector2Int>();for(int y=0;y<11;y++)for(int x=0;x<18;x++)if(!route.Contains(new Vector2Int(x,y)))high.Add(new Vector2Int(x,y));
                map.raised=high.ToArray();
                foreach(var d in map.defenders)if(d.position.y>.2f)d.position.y=.76f;
                map.rosterCards=new[]{Resources.Load<ManualBattleDefinition>("MonsterTests/OriginiumSlug"),Resources.Load<ManualBattleDefinition>("MonsterTests/M02"),Resources.Load<ManualBattleDefinition>("MonsterTests/M05")};
                foreach(var c in map.rosterCards){if(c==null)throw new InvalidOperationException("Missing trial companion.");_=new BasicRosterProfile(c.Snapshot(),c.Engagement.Snapshot());}
                foreach(var d in map.defenders){_=DefenderArt.Load(d.artId);_=DefenderArt.Load(d.artId+"B");}
                EditorUtility.SetDirty(map);
                Debug.Log("TACTICAL_V3_MAP_PASS: "+id+"; ground="+route.Count+"; raised="+high.Count+"; 4-card deck; 8 rear variants.");
            }
            AssetDatabase.SaveAssets();TacticalMapV2Tools.Validate();
        }
    }
}
