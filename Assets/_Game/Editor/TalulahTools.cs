using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Creates only the dedicated trial assets and its main-scene selector binding.</summary>
    public static class TalulahTools
    {
        public const string Root="Assets/_Game/Art/Characters/Monsters/Boss/TalulahComic/";
        public const string LevelPath="Assets/_Game/Data/Battles/TalulahTrial.asset";
        public const string CardPath="Assets/_Game/Data/Cards/Talulah.asset";

        [MenuItem("Yesterday Convoy/Initial Demo/Prepare Talulah Fire Rain Trial")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(LevelPath)!=null)
            { Bind(); Validate(); return; } // Preserve designer-edited trial values on repeated setup.
            var sheet=(TextureImporter)AssetImporter.GetAtPath(Root+"talulah-actions.png");
            sheet.textureType=TextureImporterType.Sprite; sheet.spriteImportMode=SpriteImportMode.Multiple;
            Configure(sheet,362/1.8f);
            // Exact 1448x1086 source, equal 362x362 cells; no resampling or hand-drawn replacement.
            var cells=new SpriteMetaData[12];
            for(int i=0;i<12;i++) cells[i]=new SpriteMetaData { name="talulah-"+i.ToString("00"),
                rect=new Rect(i%4*362,(2-i/4)*362,362,362), alignment=9,pivot=new Vector2(.65f,.025f) };
#pragma warning disable 0618
            sheet.spritesheet=cells;
#pragma warning restore 0618
            sheet.SaveAndReimport();
            var frames=AssetDatabase.LoadAllAssetsAtPath(Root+"talulah-actions.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            if(frames.Length!=12) throw new InvalidOperationException("Expected twelve Talulah poses.");
            var portrait=(TextureImporter)AssetImporter.GetAtPath(Root+"talulah-portrait.png");
            portrait.textureType=TextureImporterType.Sprite; portrait.spriteImportMode=SpriteImportMode.Single;
            Configure(portrait,1364/1.8f); portrait.SaveAndReimport();
            var card=Get<CardDefinition>(CardPath); var unit=Get<UnitDefinition>("Assets/_Game/Data/Units/Talulah.asset");
            Set(unit,"unitId","TALULAH"); Set(unit,"displayName","塔露拉"); Set(unit,"moveSpeed",.75f); Set(unit,"gateDamage",1);
            Set(card,"cardId","CARD_TALULAH_TRIAL"); Set(card,"displayName","塔露拉"); Set(card,"recruitCost",5); Set(card,"unit",unit);
            Set(card,"portrait",AssetDatabase.LoadAssetAtPath<Sprite>(Root+"talulah-portrait.png")); Set(card,"battleSprite",frames[8]);
            Frames(card,"walkFrames",frames.Take(8).ToArray()); Frames(card,"attackFrames",new[]{frames[8],frames[9],frames[8]});
            Frames(card,"skillFrames",new[]{frames[10],frames[11]}); Frames(card,"comicImpactFaces",new Sprite[0]);
            var combat=Get<EngagementDefinition>("Assets/_Game/Data/Battles/TalulahFireRain.asset");
            Set(combat,"kroosSprite",AssetDatabase.LoadAssetAtPath<Sprite>(EngagementTools.KroosPath));
            Set(combat,"defenderPosition",new Vector3(-1,.35f,1)); Set(combat,"fireRain",true);
            Set(combat,"unitAttack",18f); Set(combat,"unitArmor",3f); Set(combat,"unitRange",3.2f);
            Set(combat,"unitInterval",1.7f); Set(combat,"defenderHealth",210f); Set(combat,"defenderAttack",24f);
            Set(combat,"defenderRange",3.2f); Set(combat,"defenderInterval",1f); Set(combat,"blockCount",0);
            Set(combat,"initialSP",8f); Set(combat,"requiredSP",12f); Set(combat,"spPerSecond",1f);
            Set(combat,"skillAmmo",6); Set(combat,"skillAttack",48f); Set(combat,"skillInterval",6f);
            Set(combat,"projectileTime",.65f);
            var extra=new SerializedObject(combat); var positions=extra.FindProperty("extraDefenders"); positions.arraySize=2;
            positions.GetArrayElementAtIndex(0).vector3Value=new Vector3(1,.35f,-2);
            positions.GetArrayElementAtIndex(1).vector3Value=new Vector3(3,.35f,2);
            extra.ApplyModifiedPropertiesWithoutUndo();
            var level=Get<ManualBattleDefinition>(LevelPath);
            Set(level,"levelId","TALULAH_FIRE_RAIN_TRIAL"); Set(level,"card",card); Set(level,"engagement",combat);
            Set(level,"unitHealth",180f); Set(level,"initialDP",12); Set(level,"initialDefense",1); Set(level,"deploymentLimit",1);
            AssetDatabase.SaveAssets();
            Bind();
            Validate();
        }

        public static void Bind()
        {
            EditorSceneManager.OpenScene(InitialDemoTools.ScenePath);
            var controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
            var level=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(LevelPath);
            if(level==null) throw new InvalidOperationException("Trial asset was not saved.");
            var data=new SerializedObject(controller);
            data.FindProperty("talulahDefinition").objectReferenceValue=level;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene); EditorSceneManager.SaveScene(controller.gameObject.scene);
            EditorSceneManager.OpenScene(InitialDemoTools.ScenePath);
            controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
            if(new SerializedObject(controller).FindProperty("talulahDefinition").objectReferenceValue==null)
                throw new InvalidOperationException("Scene trial reference did not survive save/reload.");
            Debug.Log("TALULAH_SCENE_BIND_PASS: scene reference saved and reloaded.");
        }

        private static void Configure(TextureImporter importer,float ppu)
        {
            importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Bilinear;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        }
        private static T Get<T>(string path) where T:ScriptableObject
        {
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value==null) { Directory.CreateDirectory(Path.GetDirectoryName(path)); value=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); }
            return value;
        }
        private static void Set(UnityEngine.Object target,string field,object value)
        {
            var data=new SerializedObject(target); var p=data.FindProperty(field);
            if(value is string s) p.stringValue=s;
            else if(value is int i) p.intValue=i;
            else if(value is float f) p.floatValue=f;
            else if(value is bool b) p.boolValue=b;
            else if(value is Vector3 v) p.vector3Value=v;
            else p.objectReferenceValue=(UnityEngine.Object)value;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Frames(CardDefinition card,string field,Sprite[] sprites)
        {
            var data=new SerializedObject(card); var array=data.FindProperty(field); array.arraySize=sprites.Length;
            for(int i=0;i<sprites.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=sprites[i];
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(card);
        }
        public static void Validate()
        {
            var level=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(LevelPath);
            if(level==null || !level.Card.HasFrameAnimation || level.Card.WalkFrames.Count!=8 || level.Engagement.ExtraDefenders.Count!=2 || !level.Engagement.FireRain)
                throw new InvalidOperationException("Incomplete Talulah trial.");
            _=level.Snapshot(); _=level.Engagement.Snapshot();
            InitialDemoTools.Validate();
            Debug.Log("TALULAH_ASSET_PASS: transparent 12-cell comic sheet, walk8/attack3/cast2, separate card/config, three defenders; original map/camera preserved.");
        }
    }
}
