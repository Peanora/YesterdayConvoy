using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Imports only the supplied character frames; never rebuilds the map or overwrites combat values.</summary>
    public static class ComicArtTools
    {
        private const string Root="Assets/_Game/Art/Characters/Monsters/Boss/WisadelComic/";
        [MenuItem("Yesterday Convoy/Initial Demo/Apply Supplied Comic Frames")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(DemoArtTools.CardPath);
            var data=new SerializedObject(card);
            var idle=Import(Root+"idle/idle-000.png",new Vector2(.5f,1f/292));
            var portrait=Import("Assets/_Game/Art/Cards/Portraits/wisadel-comic-idle.png",new Vector2(.5f,.5f));
            SetFrames(data,"walkFrames",Root+"walk",new Vector2(139f/276,8f/327),19);
            SetFrames(data,"attackFrames",Root+"attack",new Vector2(200.5f/350,19f/311),28);
            SetFrames(data,"skillFrames",Root+"skill",new Vector2(256.5f/426,57f/380),100);
            SetFrames(data,"comicImpactFaces","Assets/_Game/Art/VFX/Attacks/WisadelComic",new Vector2(.5f,.5f),6);
            data.FindProperty("portrait").objectReferenceValue=portrait;
            data.FindProperty("battleSprite").objectReferenceValue=idle;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(card); AssetDatabase.SaveAssets();
            Validate(card);
        }
        private static Sprite Import(string path,Vector2 pivot)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=286/1.65f;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom; settings.spritePivot=pivot;
            settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.isReadable=false;
            importer.filterMode=FilterMode.Bilinear; importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void SetFrames(SerializedObject data,string field,string folder,Vector2 pivot,int count)
        {
            string[] files=Directory.GetFiles(folder,"*.png"); Array.Sort(files,StringComparer.Ordinal);
            if(files.Length!=count) throw new InvalidOperationException("Frame count mismatch: "+folder);
            var array=data.FindProperty(field); array.arraySize=count;
            for(int i=0;i<count;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=Import(files[i].Replace('\\','/'),pivot);
        }
        public static void Validate(CardDefinition card)
        {
            if(!card.HasFrameAnimation || card.WalkFrames.Count!=19 || card.AttackFrames.Count!=28 || card.SkillFrames.Count!=100 || card.ComicImpactFaces.Count!=6)
                throw new InvalidOperationException("Supplied comic animation references are incomplete.");
            foreach(var sequence in new[]{card.WalkFrames,card.AttackFrames,card.SkillFrames,card.ComicImpactFaces})
                foreach(var sprite in sequence) if(sprite==null) throw new InvalidOperationException("Missing supplied frame.");
            Debug.Log("COMIC_ART_PASS: idle still; walk19; attack28; skill100; face6; fixed pivots/PPU; source alpha.");
        }
    }
}
