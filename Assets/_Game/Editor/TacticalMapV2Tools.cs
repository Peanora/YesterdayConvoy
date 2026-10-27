using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class TacticalMapV2Tools
    {
        const string Folder="Assets/_Game/Data/Resources/MapTrials";
        static Vector3 P(int x,int y)=>TacticalMapEnvironment.Cell(x,y,.04f);
        [MenuItem("Yesterday Convoy/Tactical Maps/Prepare V2")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            DefenderTrialTools.Prepare();
            foreach(string name in new[]{"city-concrete.png","ruins-sandstone.png","autumn-canopy.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Environment/TacticalV2/"+name);
                importer.textureType=TextureImporterType.Default; importer.alphaIsTransparency=name.StartsWith("autumn");
                importer.mipmapEnabled=true; importer.wrapMode=name.StartsWith("autumn")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
                importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Create("city","泵站回廊",false,new[]{new TacticalLane{id="R1",exitId="B1",points=new[]{P(0,2),P(3,2),P(3,5),P(7,5),P(7,1),P(11,1),P(11,4),P(9,4),P(9,8),P(14,8),P(14,5),P(17,5)}},new TacticalLane{id="R2",exitId="B1",points=new[]{P(0,10),P(5,10),P(5,7),P(7,7),P(7,1),P(11,1),P(11,4),P(9,4),P(9,8),P(14,8),P(14,5),P(17,5)}},new TacticalLane{id="R3",exitId="B2",points=new[]{P(6,10),P(8,10),P(8,9),P(16,9),P(16,3),P(14,3),P(14,1),P(12,1),P(12,0)}},new TacticalLane{id="R4",exitId="B2",points=new[]{P(17,1),P(17,3),P(15,3),P(15,5),P(13,5),P(13,7),P(11,7),P(11,3),P(12,3),P(12,0)}}},new[]{new Vector2Int(4,2),new Vector2Int(4,3),new Vector2Int(5,2),new Vector2Int(5,3),new Vector2Int(8,2),new Vector2Int(9,2),new Vector2Int(10,5),new Vector2Int(10,6),new Vector2Int(12,5),new Vector2Int(12,6),new Vector2Int(15,6),new Vector2Int(15,7),new Vector2Int(15,8)});
            Create("ruins","金叶环庭",true,new[]{new TacticalLane{id="R1",exitId="B1",points=new[]{P(1,1),P(1,8),P(5,8),P(5,5),P(3,5),P(3,3),P(8,3),P(8,1),P(12,1),P(12,4),P(10,4),P(10,7),P(15,7),P(15,2),P(17,2)}},new TacticalLane{id="R2",exitId="B2",points=new[]{P(7,10),P(11,10),P(11,9),P(6,9),P(6,6),P(4,6),P(4,3),P(2,3),P(2,4),P(0,4)}},new TacticalLane{id="R3",exitId="B1",points=new[]{P(17,9),P(10,9),P(10,8),P(8,8),P(8,5),P(10,5),P(10,7),P(15,7),P(15,2),P(17,2)}}},new[]{new Vector2Int(3,6),new Vector2Int(3,7),new Vector2Int(6,4),new Vector2Int(6,5),new Vector2Int(7,4),new Vector2Int(7,5),new Vector2Int(7,6),new Vector2Int(8,4),new Vector2Int(13,3),new Vector2Int(13,4),new Vector2Int(13,5),new Vector2Int(14,3),new Vector2Int(14,4),new Vector2Int(14,5)});
            AssetDatabase.SaveAssets(); Validate();
            AbilityFixtureTools.Generate();
        }
        static void Create(string id,string title,bool forest,TacticalLane[] lanes,Vector2Int[] raised)
        {
            string path=Folder+"/"+id+".asset";
            if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(path)!=null) return;
            var original=Resources.Load<DefenderTrialDefinition>("DefenderTrial");
            var data=UnityEngine.Object.Instantiate(original); data.name=id;
            data.mapId=id;data.mapTitle=title;data.autumnRuins=forest;data.lanes=lanes;data.route=lanes[0].points;data.raised=raised;
            data.floorTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Environment/TacticalV2/"+(forest?"ruins-sandstone.png":"city-concrete.png"));
            data.foliageTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Environment/TacticalV2/autumn-canopy.png");
            var level=UnityEngine.Object.Instantiate(original.battle);
            Set(level,"levelId","TACTICAL_V2_"+id.ToUpperInvariant()); Set(level,"initialDP",24);Set(level,"deploymentLimit",12);Set(level,"initialDefense",1);
            AssetDatabase.CreateAsset(level,"Assets/_Game/Data/Battles/Tactical-"+id+".asset");data.battle=level;
            Vector2Int[] spots=forest?new[]{new Vector2Int(1,6),new Vector2Int(10,6),new Vector2Int(15,4),new Vector2Int(3,7),new Vector2Int(7,4),new Vector2Int(13,5),new Vector2Int(7,6),new Vector2Int(14,3)}:
                new[]{new Vector2Int(3,4),new Vector2Int(9,6),new Vector2Int(14,6),new Vector2Int(5,3),new Vector2Int(8,2),new Vector2Int(12,6),new Vector2Int(10,6),new Vector2Int(15,8)};
            for(int i=0;i<8;i++)data.defenders[i].position=TacticalMapEnvironment.Cell(spots[i].x,spots[i].y,i<3?.04f:.46f);
            AssetDatabase.CreateAsset(data,path);EditorUtility.SetDirty(data);
        }
        public static void Validate()
        {
            foreach(string id in new[]{"city","ruins"})
            {
                var data=Resources.Load<DefenderTrialDefinition>("MapTrials/"+id);
                if(data==null||data.floorTexture==null||data.foliageTexture==null)throw new InvalidOperationException("Missing tactical map art/configuration.");
                var roster=data.Snapshot();foreach(var d in roster)_=DefenderArt.Load(d.ArtId);
                var b=new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),roster:roster,deploymentLanes:data.LaneSnapshot());
                if(b.Lanes.Count!=(id=="city"?4:3)||b.Lanes.Select(l=>l.ExitId).Distinct().Count()!=2)throw new InvalidOperationException("Wrong V2 gate counts.");
                foreach(var lane in b.Lanes) if(lane.Points.Count<9)throw new InvalidOperationException("Route too simple.");
            }
            Debug.Log("TACTICAL_V2_CONFIGURATION_PASS: two maps, seven fixed lanes, shared defense, runtime art bound.");
        }
        static void Set(UnityEngine.Object asset,string name,object value)
        { var s=new SerializedObject(asset);var p=s.FindProperty(name);if(value is string str)p.stringValue=str;else p.intValue=(int)value;s.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
