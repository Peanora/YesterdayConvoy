using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    internal sealed class ArtCatalog
    {
        [Serializable] sealed class Manifest {public int version;public string task,cardId;public float ppu,referenceSpeed,height,attackContact;public bool noAttackClip,noOrdinaryAttack,rearVariant,auxiliary;public Item[] clips;}
        [Serializable] sealed class Item {public string key;public int count,cell,cellWidth,cellHeight,columns;public float duration,fps,pivotX,pivotY,pixelsPerUnit;public ArtSequence.Page[] pages;}
        static readonly Dictionary<string,ArtCatalog> catalogs=new Dictionary<string,ArtCatalog>();
        public readonly Dictionary<string,ArtSequence> Clips=new Dictionary<string,ArtSequence>();
        public string Root;public float Ppu,Speed,Height,Contact;public bool Rear,Auxiliary;
        public static ArtCatalog Read(string root)
        {
            root=root.Replace('\\','/');
            if(catalogs.TryGetValue(root,out var found))return found;
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve(root+"/manifest.json")));
            if(data==null||data.version!=1||data.ppu<=0||data.clips==null)throw new InvalidDataException("Invalid art manifest: "+root);
            string id=Path.GetFileName(root);
            if((root.StartsWith("DefenderArt/")||root.StartsWith("MonsterCombatArt/"))&&data.task!=id)throw new InvalidDataException("Art ID mismatch: "+root);
            if(root=="TalulahOriginal"&&(data.cardId!="CARD_TALULAH_TRIAL"||data.clips.Length!=4)||root=="OriginiumSlugOriginal"&&(data.cardId!="CARD_ORIGINIUM_SLUG"||data.clips.Length!=6))throw new InvalidDataException("Original art identity mismatch.");
            found=new ArtCatalog{Root=root,Ppu=data.ppu,Speed=data.referenceSpeed,Height=data.height,Contact=data.attackContact,Rear=data.rearVariant,Auxiliary=data.auxiliary};
            found.Add(root,data.clips);
            string[] required=root.StartsWith("DefenderArt/")?data.auxiliary?new[]{"start","idle","attack"}:data.noOrdinaryAttack?data.rearVariant?new[]{"idle"}:new[]{"idle","death"}:data.rearVariant?new[]{"idle","attack"}:new[]{"idle","attack","death"}:root.StartsWith("MonsterCombatArt/")?data.noAttackClip?new[]{"idle","walk","death"}:new[]{"idle","walk","attack","death"}:root=="TalulahOriginal"?new[]{"idle","walk","attack","cast"}:new[]{"idle","walk","start","stop","attack","death"};
            foreach(string key in required)if(!found.Clips.ContainsKey(key))throw new InvalidDataException("Missing art clip: "+root+"/"+key);
            if(found.Clips.ContainsKey("attack_start")!=found.Clips.ContainsKey("attack_end"))throw new InvalidDataException("Incomplete attack phases.");
            catalogs.Add(root,found);return found;
        }
        public void Extras(string[] wanted)
        {
            if(wanted.Length==0)return;
            string root=Root+"/ExtraClips";var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve(root+"/manifest.json")));
            foreach(var item in data.clips)if(Array.IndexOf(wanted,item.key)>=0&&!Clips.ContainsKey(item.key))Add(root,new[]{item});
            foreach(string key in wanted)if(!Clips.ContainsKey(key))throw new InvalidDataException("Missing required clip: "+key);
        }
        void Add(string root,Item[] items)
        {
            foreach(var item in items)
            {
                if(string.IsNullOrEmpty(item.key)||item.count<=0||item.count>600||item.duration<=0||float.IsNaN(item.duration)||item.pages==null||item.pages.Length==0||item.columns<=0||item.columns>16||item.pivotX<0||item.pivotX>1||item.pivotY<0||item.pivotY>1)throw new InvalidDataException("Invalid art clip: "+root);
                foreach(var page in item.pages)
                {
                    if(string.IsNullOrEmpty(page.file)||Path.GetFileName(page.file)!=page.file||page.count<=0||page.width<=0||page.height<=0||page.width>2048||page.height>2048)throw new InvalidDataException("Invalid art page.");
                    string file=ArtSourcePaths.Resolve(root+"/"+page.file);
                    if(!File.Exists(file)&&!File.Exists(file+".ycart"))throw new FileNotFoundException("Art page is missing",file);
                }
                Clips.Add(item.key,new ArtSequence{Root=root,Key=item.key,Name=Path.GetFileName(Root)+"/"+item.key,Count=item.count,Duration=item.duration,Fps=item.fps,Ppu=item.pixelsPerUnit>0?item.pixelsPerUnit:Ppu,PivotX=item.pivotX,PivotY=item.pivotY,Width=item.cell>0?item.cell:item.cellWidth,Height=item.cell>0?item.cell:item.cellHeight,Columns=item.columns,Pages=item.pages});
            }
        }
        public Sprite Portrait
        {
            get
            {
                string key=Root+"/portrait";if(ArtMemoryCache.TryGet(key,out var value))return value.Frames[0];
                value=new ArtMemoryCache.Resource{Owner="portraits"};
                try
                {
                    // Prepared thumbnails are cropped from the exact old portrait, including M02-M10 idle crops.
                    string file=Path.Combine(Root,"card-preview.png");
                    if(!File.Exists(ArtSourcePaths.Resolve(file)+".ycart")&&!File.Exists(Path.Combine(Application.dataPath,"../Library/PreparedArt",file+".ycart")))file=Path.Combine(Root,"portrait.png");
                    var texture=ArtMemoryCache.Texture(file,value);var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0),Ppu,0,SpriteMeshType.FullRect);
                    sprite.name=Path.GetFileName(Root)+"/portrait";value.Objects.Add(sprite);value.Frames=new[]{sprite};ArtMemoryCache.Put(key,value);return sprite;
                }
                catch{ArtMemoryCache.Dispose(value);throw;}
            }
        }
    }
}
