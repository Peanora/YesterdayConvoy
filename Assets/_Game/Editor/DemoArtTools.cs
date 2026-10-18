using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DemoArtTools
    {
        private const string Art = "Assets/_Game/Art/";
        public const string CardPath = "Assets/_Game/Data/Cards/WisadelApril.asset";

        [MenuItem("Yesterday Convoy/Initial Demo/Apply Approved Art")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            Sprite portrait = ImportSprite(Art + "Cards/Portraits/wisadel-april-approved.png", 0);
            Sprite battle = ImportSprite(Art + "Characters/Monsters/Boss/wisadel-april-battle.png", 1.65f);
            ImportSprite(Art + "Characters/Monsters/Boss/wisadel-rear.png", 1.65f);
            ImportSprite(Art + "Characters/Monsters/Boss/wisadel-side.png", 1.65f);
            ImportSprite(Art + "Characters/Monsters/Boss/wisadel-rear-quarter.png", 1.65f);
            Sprite cannon = ImportSprite(Art + "Characters/Defenders/grumpy-cannon.png", 2.2f);
            Sprite red = ImportSprite(Art + "Environment/Portal/rune-gate-red.png", 2.1f);
            Sprite blue = ImportSprite(Art + "Environment/Portal/rune-gate-blue.png", 2.1f);
            string floorPath = Art + "Environment/Tilesets/slate-floor.png";
            var floorImporter = (TextureImporter)AssetImporter.GetAtPath(floorPath);
            floorImporter.textureType = TextureImporterType.Default;
            floorImporter.filterMode = FilterMode.Point;
            floorImporter.wrapMode = TextureWrapMode.Repeat;
            floorImporter.mipmapEnabled = true;
            floorImporter.npotScale = TextureImporterNPOTScale.None;
            floorImporter.maxTextureSize = 2048;
            floorImporter.textureCompression = TextureImporterCompression.Uncompressed;
            floorImporter.SaveAndReimport();
            Texture2D floor = AssetDatabase.LoadAssetAtPath<Texture2D>(floorPath);

            Directory.CreateDirectory("Assets/_Game/Data/Cards");
            Directory.CreateDirectory("Assets/_Game/Prefabs");
            AssetDatabase.Refresh();
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(CardPath);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDefinition>();
                var values = new SerializedObject(card);
                values.FindProperty("cardId").stringValue = "CARD_WISADEL_APRIL";
                values.FindProperty("displayName").stringValue = "维什戴尔";
                values.FindProperty("recruitCost").intValue = 5;
                values.FindProperty("unit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnitDefinition>(UnitConfigurationTools.SampleFolder + "/U01.asset");
                values.FindProperty("portrait").objectReferenceValue = portrait;
                values.FindProperty("battleSprite").objectReferenceValue = battle;
                values.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(card, CardPath);
            }
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(InitialDemoTools.ScenePath, OpenSceneMode.Single);
            // Opening a scene may unload assets that only had temporary editor references.
            card = AssetDatabase.LoadAssetAtPath<CardDefinition>(CardPath);
            battle = card.BattleSprite;
            cannon = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Characters/Defenders/grumpy-cannon.png");
            red = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Environment/Portal/rune-gate-red.png");
            blue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Environment/Portal/rune-gate-blue.png");
            var demo = UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            Camera.main.transform.position = new Vector3(0, 14, -21);
            Camera.main.transform.LookAt(new Vector3(0, 0, 0.5f));
            var data = new SerializedObject(demo);
            Transform monster = (Transform)data.FindProperty("monster").objectReferenceValue;
            ReplaceVisual(monster, battle, demo);
            ReplaceVisual((Transform)data.FindProperty("tower").objectReferenceValue, cannon, null);
            ReplaceVisual((Transform)data.FindProperty("redGate").objectReferenceValue, red, null);
            ReplaceVisual((Transform)data.FindProperty("blueGate").objectReferenceValue, blue, null);
            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/NotoSansSC-Regular.otf");
            data.FindProperty("card").objectReferenceValue = card;
            data.FindProperty("interfaceFont").objectReferenceValue = font;
            data.ApplyModifiedPropertiesWithoutUndo();
            SetGround("DemoFloor", floor, new Color(0.65f, 0.68f, 0.78f));
            SetGround("DemoPath", floor, new Color(1.35f, 1.22f, 1.08f));
            Camera.main.backgroundColor = new Color(0.065f, 0.065f, 0.09f);
            if (Camera.main.transform.Find("Five Cost Card") == null)
                CreateCardView(Camera.main, card, font);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            if (GameObject.Find(TacticalMapTools.RootName) != null)
            {
                TacticalMapTools.Apply();
                return;
            }
            Validate();
        }

        public static void Validate()
        {
            InitialDemoTools.Validate();
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(CardPath);
            var demo = UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var view = UnityEngine.Object.FindAnyObjectByType<DemoCardView>();
            if (card == null || card.RecruitCost != 5 || card.Unit == null || card.Portrait == null || card.BattleSprite == null || demo.Card != card || view == null)
                throw new InvalidOperationException($"Approved art references: card={card != null}; cost={card?.RecruitCost}; unit={card?.Unit != null}; portrait={card?.Portrait != null}; battle={card?.BattleSprite != null}; demoCard={demo.Card == card}; view={view != null}.");
            var visuals = UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>();
            if (visuals.Length != 4) throw new InvalidOperationException("Expected one unit, cannon and two portal sprites.");
            foreach (var visual in visuals)
            {
                if (visual.Renderer == null || visual.Renderer.sprite == null)
                    throw new InvalidOperationException("Missing battlefield sprite on " + visual.name);
                var values = new SerializedObject(visual);
                if (values.FindProperty("movingDemo").objectReferenceValue == null) continue;
                if (values.FindProperty("rearPose").objectReferenceValue == null)
                    throw new InvalidOperationException("Missing rear puppet texture.");
                if (values.FindProperty("sidePose").objectReferenceValue == null || values.FindProperty("rearQuarterPose").objectReferenceValue == null)
                    throw new InvalidOperationException("Missing intermediate turning views.");
            }
            if (Vector3.Dot(Camera.main.transform.right, Vector3.right) < 0.999f)
                throw new InvalidOperationException("Board camera must face forward without sideways yaw or roll.");
            Debug.Log("DEMO_ART_PASS: approved portrait; recruitCost=5; 4 battlefield visuals; 4-direction cutout views; forward-facing oblique camera.");
            if (GameObject.Find(TacticalMapTools.RootName) != null) TacticalMapTools.ValidatePresentation();
        }

        private static Sprite ImportSprite(string path, float visibleHeight)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = source.GetPixels32();
            int minX = source.width, maxX = -1, minY = source.height, maxY = -1;
            for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                    if (pixels[y * source.width + x].a > 16)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) throw new InvalidOperationException("Empty sprite: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = visibleHeight > 0 ? (maxY - minY + 1) / visibleHeight : 512;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = visibleHeight > 0 ? new Vector2((minX + maxX + 1) / (2f * source.width), minY / (float)source.height) : new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            UnityEngine.Object.DestroyImmediate(source);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ReplaceVisual(Transform owner, Sprite sprite, InitialDemo movingDemo)
        {
            Transform previous = owner.Find("Series Sprite");
            foreach (MeshRenderer renderer in owner.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;
            var visual = previous != null ? previous.gameObject : new GameObject("Series Sprite", typeof(SpriteRenderer), typeof(DemoSpriteVisual));
            visual.transform.SetParent(owner, false);
            visual.transform.rotation = Camera.main.transform.rotation;
            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = DemoSpriteVisual.GetSortingOrder(Camera.main, owner.position);
            var values = new SerializedObject(visual.GetComponent<DemoSpriteVisual>());
            values.FindProperty("sprite").objectReferenceValue = spriteRenderer;
            values.FindProperty("movingDemo").objectReferenceValue = movingDemo;
            if (movingDemo != null)
            {
                values.FindProperty("rearPose").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Characters/Monsters/Boss/wisadel-rear.png");
                values.FindProperty("sidePose").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Characters/Monsters/Boss/wisadel-side.png");
                values.FindProperty("rearQuarterPose").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Characters/Monsters/Boss/wisadel-rear-quarter.png");
            }
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetGround(string name, Texture2D texture, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/" + name + ".mat");
            material.mainTexture = texture;
            material.color = color;
            material.SetFloat("_Glossiness", 0.03f);
            EditorUtility.SetDirty(material);
        }

        private static Material HudMaterial(string name, Color color)
        {
            string path = "Assets/_Game/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Unlit/Color")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateCardView(Camera camera, CardDefinition card, Font font)
        {
            Transform previous = camera.transform.Find("Five Cost Card");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = new GameObject("Five Cost Card", typeof(DemoCardView));
            root.transform.SetParent(camera.transform, false);
            float pixel = 2 * camera.orthographicSize / 720;
            Material gold = HudMaterial("CardGold", new Color(0.86f, 0.64f, 0.30f));
            Material dark = HudMaterial("CardDark", new Color(0.08f, 0.08f, 0.12f));
            HudQuad(root.transform, "Gold Border", new Rect(22, 240, 184, 254), pixel, 2.15f, gold);
            HudQuad(root.transform, "Card Body", new Rect(25, 243, 178, 248), pixel, 2.12f, dark);
            var image = new GameObject("Approved Portrait", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            image.transform.SetParent(root.transform, false);
            image.transform.localPosition = new Vector3((114 - 640) * pixel, (360 - 337) * pixel, 2.05f);
            image.sprite = card.Portrait;
            image.sortingOrder = 5000;
            image.transform.localScale = Vector3.one * (164 * pixel / card.Portrait.bounds.size.x);
            HudQuad(root.transform, "Cost Badge", new Rect(162, 248, 36, 38), pixel, 1.99f, gold);
            TextMesh cost = HudText(root.transform, "Cost", "5", 169, 247, 30, pixel, new Color(0.10f, 0.09f, 0.10f), font);
            TextMesh title = HudText(root.transform, "Name", card.DisplayName, 39, 431, 23, pixel, new Color(0.95f, 0.85f, 0.65f), font);
            TextMesh caption = HudText(root.transform, "Recruit Cost", "招募费 5  ·  角色卡", 39, 466, 14, pixel, new Color(0.65f, 0.69f, 0.76f), font);
            var data = new SerializedObject(root.GetComponent<DemoCardView>());
            data.FindProperty("card").objectReferenceValue = card;
            data.FindProperty("font").objectReferenceValue = font;
            data.FindProperty("portrait").objectReferenceValue = image;
            data.FindProperty("title").objectReferenceValue = title;
            data.FindProperty("cost").objectReferenceValue = cost;
            data.FindProperty("caption").objectReferenceValue = caption;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/_Game/Prefabs/FiveCostCard.prefab", InteractionMode.AutomatedAction);
        }

        private static void HudQuad(Transform parent, string name, Rect rect, float pixel, float depth, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3((rect.center.x - 640) * pixel, (360 - rect.center.y) * pixel, depth);
            quad.transform.localScale = new Vector3(rect.width * pixel, rect.height * pixel, 1);
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static TextMesh HudText(Transform parent, string name, string content, float x, float y, int size, float pixel, Color color, Font font)
        {
            var text = new GameObject(name, typeof(TextMesh)).GetComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.localPosition = new Vector3((x - 640) * pixel, (360 - y) * pixel, 1.9f);
            text.text = content;
            text.font = font;
            text.fontSize = 48;
            text.characterSize = pixel * 10 * size / 48;
            text.anchor = TextAnchor.UpperLeft;
            text.color = color;
            return text;
        }
    }
}
