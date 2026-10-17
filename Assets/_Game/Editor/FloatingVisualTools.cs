using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class FloatingVisualTools
    {
        public const string PrefabPath="Assets/_Game/Prefabs/Characters/AshWispVisual.prefab";
        public const string SpritePath="Assets/_Game/Art/Characters/Monsters/Normal/ash-wisp.png";
        private const string ShadowPath="Assets/_Game/Art/Characters/Monsters/Normal/WispSprites.asset";
        [MenuItem("Yesterday Convoy/Animation/Build Second Body Sample")]
        public static void Build()
        {
            LayeredCharacterTools.Build();
            if(!File.Exists(PrefabPath)) CreatePrefab();
            InitializeEditorRestPose();
            var scene=EditorSceneManager.OpenScene(LayeredCharacterTools.LabScene);
            var lab=UnityEngine.Object.FindAnyObjectByType<AnimationLab>();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform root=GameObject.Find("Floating sample")?.transform;
            if(root==null)
            {
                root=new GameObject("Floating sample").transform;
                PrefabUtility.InstantiatePrefab(prefab,root);
            }
            var values=new SerializedObject(lab);
            values.FindProperty("floating").objectReferenceValue=root.GetComponentInChildren<DemoSpriteVisual>(true);
            values.ApplyModifiedPropertiesWithoutUndo();
            if(root.position==Vector3.zero && lab.Biped.transform.parent.position==Vector3.zero)
            {
                root.position=new Vector3(-2.6f,0.04f,-1);
                lab.Biped.transform.parent.position=new Vector3(-5.4f,0.04f,-1);
                root.GetComponentInChildren<DemoSpriteVisual>().transform.rotation=Camera.main.transform.rotation;
                lab.Biped.transform.rotation=Camera.main.transform.rotation;
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ValidateAssets();
            Debug.Log("SECOND_BODY_PASS: biped and floating prefabs share the same route motion inputs.");
        }
        private static void CreatePrefab()
        {
            Sprite sprite=DemoArtTools.ImportSprite(SpritePath,1.2f);
            var library=AssetDatabase.LoadAssetAtPath<LayeredSpriteLibrary>(ShadowPath);
            if(library==null) {library=ScriptableObject.CreateInstance<LayeredSpriteLibrary>();AssetDatabase.CreateAsset(library,ShadowPath);}
            Texture2D circle=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/VFX/Common/particle-circle-05.png");
            Sprite shadow=Sprite.Create(circle,new Rect(0,0,circle.width,circle.height),new Vector2(0.5f,0.5f),circle.width,0,SpriteMeshType.FullRect);
            shadow.name="Ground shadow from Kenney circle";AssetDatabase.AddObjectToAsset(shadow,library);library.AddAuthoredSprite(shadow);
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            var owner=new GameObject("Ash Wisp Visual",typeof(SpriteRenderer),typeof(DemoSpriteVisual),typeof(FloatingVisualPresentation));
            try
            {
                var source=owner.GetComponent<SpriteRenderer>();source.sprite=sprite;source.enabled=false;
                var body=new GameObject("Floating body",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                body.transform.SetParent(owner.transform,false);body.sprite=sprite;
                var shade=new GameObject("Ground shadow",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                shade.transform.SetParent(owner.transform,false);shade.sprite=shadow;
                shade.transform.localScale=new Vector3(0.7f,0.4f,1);
                var presentation=new SerializedObject(owner.GetComponent<FloatingVisualPresentation>());
                presentation.FindProperty("body").objectReferenceValue=body;presentation.FindProperty("shadow").objectReferenceValue=shade;presentation.ApplyModifiedPropertiesWithoutUndo();
                var bridge=new SerializedObject(owner.GetComponent<DemoSpriteVisual>());
                bridge.FindProperty("sprite").objectReferenceValue=source;
                bridge.FindProperty("presentation").objectReferenceValue=owner.GetComponent<FloatingVisualPresentation>();bridge.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(owner,PrefabPath);
            }
            finally {UnityEngine.Object.DestroyImmediate(owner);}
        }
        private static void InitializeEditorRestPose()
        {
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                bool changed=false;
                Transform body=root.transform.Find("Floating body"),shade=root.transform.Find("Ground shadow");
                if(body.localPosition==Vector3.zero) {body.localPosition=new Vector3(0,0.28f,0);changed=true;}
                if(shade.localRotation==Quaternion.identity) {shade.localRotation=Quaternion.Euler(90,0,0);changed=true;}
                var renderer=shade.GetComponent<SpriteRenderer>();
                if(renderer.color==Color.white) {renderer.color=new Color(0,0,0,0.25f);changed=true;}
                if(changed) PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void ValidateAssets()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(prefab==null || prefab.GetComponent<FloatingVisualPresentation>()==null || prefab.GetComponent<BipedVisualRig>()!=null)
                throw new InvalidOperationException("Missing independent floating visual prefab.");
        }
    }
}
