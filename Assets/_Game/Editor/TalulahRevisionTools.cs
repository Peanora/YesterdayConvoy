using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class TalulahRevisionTools
    {
        private const string Root=TalulahTools.Root+"V2/";
        public const string ArtPath="Assets/_Game/Data/Cards/TalulahArt.asset";
        [MenuItem("Yesterday Convoy/Initial Demo/Apply Talulah Video Revision")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            string[] names={"head","torso","cape","skirt","thigh-left","thigh-right","boot-left","boot-right","sword","upper-arm-left","upper-arm-right","forearm-left","forearm-right"};
            float[] heights={.83f,.50f,.94f,.23f,.29f,.29f,.38f,.38f,.55f,.20f,.20f,.23f,.23f};
            RectInt[] regions={new RectInt(0,0,470,480),new RectInt(475,0,360,480),new RectInt(840,0,456,480),
                new RectInt(0,500,432,310),new RectInt(432,500,432,310),new RectInt(864,500,432,310),
                new RectInt(0,810,432,404),new RectInt(432,810,408,404),new RectInt(840,810,456,404)};
            var body=new Texture2D(2,2); body.LoadImage(File.ReadAllBytes(Root+"talulah-parts-source.png"));
            var arms=new Texture2D(2,2); arms.LoadImage(File.ReadAllBytes(Root+"talulah-arms-source.png"));
            for(int i=0;i<13;i++)
            {
                RectInt box=i<9 ? regions[i] : new RectInt((i-9)%2*577,(i-9)/2*681,577,(i-9)/2==0 ? 681 : 682);
                Normalize(i<9 ? body : arms,box,Root+names[i]+".png",heights[i]);
            }
            UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(arms); AssetDatabase.Refresh();
            Sprite[] parts=new Sprite[13];
            for(int i=0;i<13;i++)
            {
                Vector2 pivot=i==0 || i==1 ? new Vector2(.5f,.02f) : i==8 ? new Vector2(.86f,.84f) : new Vector2(.5f,.96f);
                parts[i]=Single(Root+names[i]+".png",256,pivot);
            }
            var portrait=Single(Root+"talulah-portrait-v2.png",1363/1.8f,new Vector2(.5f,.02f));
            var fire=Sheet("Assets/_Game/Art/VFX/Skills/Talulah/fire-stream-cc0.png",8,4,256,"fire");
            var hit=Sheet("Assets/_Game/Art/VFX/Skills/Talulah/fire-impact-cc0.png",4,4,128,"impact");
            var art=AssetDatabase.LoadAssetAtPath<TalulahArtDefinition>(ArtPath);
            if(art==null) { art=ScriptableObject.CreateInstance<TalulahArtDefinition>(); AssetDatabase.CreateAsset(art,ArtPath); }
            var data=new SerializedObject(art); Frames(data,"parts",parts); Frames(data,"fireFrames",fire); Frames(data,"impactFrames",hit);
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(art);
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(TalulahTools.CardPath); data=new SerializedObject(card);
            data.FindProperty("portrait").objectReferenceValue=portrait; data.FindProperty("battleSprite").objectReferenceValue=portrait;
            data.FindProperty("articulatedArt").objectReferenceValue=art;
            foreach(string field in new[]{"walkFrames","attackFrames","skillFrames"}) Frames(data,field,new[]{portrait});
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(card); AssetDatabase.SaveAssets(); Validate();
        }
        private static void Normalize(Texture2D source,RectInt region,string path,float height)
        {
            int minX=region.xMax,minY=source.height-region.y,maxX=0,maxY=0;
            for(int y=Mathf.Max(0,source.height-region.yMax);y<source.height-region.y;y++)
                for(int x=region.x;x<region.xMax;x++) if(source.GetPixel(x,y).a>.18f)
                { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
            if(maxX<=minX || maxY<=minY) throw new InvalidOperationException("Empty part: "+path);
            int visibleH=Mathf.RoundToInt(height*256),visibleW=Mathf.RoundToInt(visibleH*(maxX-minX+1f)/(maxY-minY+1));
            var output=new Texture2D(visibleW+4,visibleH+4,TextureFormat.RGBA32,false); output.SetPixels(new Color[(visibleW+4)*(visibleH+4)]);
            for(int y=0;y<visibleH;y++) for(int x=0;x<visibleW;x++)
                output.SetPixel(x+2,y+2,source.GetPixelBilinear((minX+(x+.5f)*(maxX-minX+1f)/visibleW)/source.width,
                    (minY+(y+.5f)*(maxY-minY+1f)/visibleH)/source.height));
            output.Apply(); byte[] bytes=output.EncodeToPNG();
            if(!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) File.WriteAllBytes(path,bytes);
            UnityEngine.Object.DestroyImmediate(output);
        }
        private static Sprite Single(string path,float ppu,Vector2 pivot)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single; Configure(importer,ppu);
            var s=new TextureImporterSettings(); importer.ReadTextureSettings(s); s.spriteAlignment=9;s.spritePivot=pivot;
            importer.SetTextureSettings(s); importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Sprite[] Sheet(string path,int columns,int rows,float ppu,string prefix)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple; Configure(importer,ppu);
            var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
            int w=texture.width/columns,h=texture.height/rows;UnityEngine.Object.DestroyImmediate(texture);
            var cells=new SpriteMetaData[columns*rows];
            for(int i=0;i<cells.Length;i++) cells[i]=new SpriteMetaData{name=prefix+"-"+i.ToString("00"),rect=new Rect(i%columns*w,(rows-1-i/columns)*h,w,h),alignment=9,pivot=new Vector2(.5f,.5f)};
#pragma warning disable 0618
            importer.spritesheet=cells;
#pragma warning restore 0618
            importer.SaveAndReimport(); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        }
        private static void Configure(TextureImporter importer,float ppu)
        {
            importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Bilinear; importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048; importer.wrapMode=TextureWrapMode.Clamp;
        }
        private static void Frames(SerializedObject data,string field,Sprite[] sprites)
        { var p=data.FindProperty(field);p.arraySize=sprites.Length;for(int i=0;i<sprites.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=sprites[i]; }
        public static void Validate()
        {
            var art=AssetDatabase.LoadAssetAtPath<TalulahArtDefinition>(ArtPath);
            if(art==null || !art.Complete || art.Parts.Any(p=>p==null) || art.FireFrames.Any(p=>p==null) || art.ImpactFrames.Any(p=>p==null))
                throw new InvalidOperationException("Incomplete Talulah revision.");
            if(AssetDatabase.LoadAssetAtPath<CardDefinition>(TalulahTools.CardPath).ArticulatedArt!=art) throw new InvalidOperationException("Talulah art is not bound.");
            InitialDemoTools.Validate();
            Debug.Log("TALULAH_REVISION_PASS: 13 separate parts at 256PPU; 32 CC0 fire frames; 16 impact frames; original battle values/map/camera preserved.");
        }
    }
}
