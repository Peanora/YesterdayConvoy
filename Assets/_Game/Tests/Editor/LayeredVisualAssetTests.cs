using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class LayeredVisualAssetTests
    {
        [Test]
        public void FloatingVisualReusesSharedProtocolWithoutBipedJointsOrGameplayComponents()
        {
            FloatingVisualTools.ValidateAssets();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FloatingVisualTools.PrefabPath);
            Assert.That(prefab.GetComponent<DemoSpriteVisual>().Presentation,Is.TypeOf<FloatingVisualPresentation>());
            Assert.That(prefab.GetComponent<BipedVisualRig>(),Is.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true).Length,Is.Zero);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true).Length,Is.Zero);
            Assert.That(prefab.GetComponentInChildren<InitialDemo>(true),Is.Null);
        }

        [Test]
        public void VisualPrefabUsesPersistentSeparatePartsAndOriginalFacePixels()
        {
            LayeredCharacterTools.ValidateAssets();
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LayeredCharacterTools.PrefabPath);
            var rig=prefab.GetComponent<BipedVisualRig>();
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(DemoArtTools.CardPath);
            Assert.That(rig.Front.Head.Renderer.sprite.texture,Is.SameAs(card.BattleSprite.texture));
            foreach(var view in rig.Views)
            {
                Assert.That(view.Parts.Count,Is.EqualTo(13));
                foreach(var part in view.Parts)
                {
                    Assert.That(AssetDatabase.Contains(part.Renderer.sprite),Is.True);
                    Assert.That(part.Renderer.transform.localScale,Is.EqualTo(Vector3.one),"Parts fit through authored pivots/PPU, not arbitrary runtime scales.");
                }
            }
        }

        [Test]
        public void AuthoredLimbEndsContainOpaqueJointOverlap()
        {
            var rig=AssetDatabase.LoadAssetAtPath<GameObject>(LayeredCharacterTools.PrefabPath).GetComponent<BipedVisualRig>();
            var textures=new Dictionary<Texture2D,Texture2D>();
            try
            {
                foreach(var view in rig.Views)
                {
                    Check(view.ThighL,view.Root.TransformPoint(view.KneeL));
                    Check(view.ThighR,view.Root.TransformPoint(view.KneeR));
                    Check(view.BootL,view.Root.TransformPoint(view.AnkleL));
                    Check(view.BootR,view.Root.TransformPoint(view.AnkleR));
                    Check(view.ArmL,view.HandL.Bone.position);
                    Check(view.ArmR,view.HandR.Bone.position);
                }
            }
            finally {foreach(var texture in textures.Values) Object.DestroyImmediate(texture);}
            void Check(BipedVisualRig.Part part,Vector3 end)
            {
                Sprite sprite=part.Renderer.sprite;
                if(!textures.TryGetValue(sprite.texture,out Texture2D cpu))
                {
                    cpu=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    cpu.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
                    textures.Add(sprite.texture,cpu);
                }
                foreach(Vector3 world in new[]{part.Bone.position,end})
                {
                    Vector3 local=part.Renderer.transform.InverseTransformPoint(world);
                    Vector2 pixel=sprite.rect.min+sprite.pivot+new Vector2(local.x,local.y)*sprite.pixelsPerUnit;
                    Assert.That(cpu.GetPixel(Mathf.RoundToInt(pixel.x),Mathf.RoundToInt(pixel.y)).a,Is.GreaterThan(0.5f),part.Bone.name+" needs opaque coverage at each joint.");
                }
            }
        }
    }
}
