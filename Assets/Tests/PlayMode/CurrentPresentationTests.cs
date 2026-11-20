using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class CurrentPresentationTests
    {
        [UnityTest]
        public IEnumerator BoardGroundStaysBehindItsActorAt720And1440WhileSolidsStillOcclude()
        {
            Assume.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(GraphicsDeviceType.Null));
            FormationShopSession.StartNew(20261117);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            Assert.That(controller,Is.Not.Null);
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while((!controller.Ready||!controller.RosterArtReady)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.Ready&&controller.RosterArtReady,Is.True);

            var actor=controller.Model.Units.First(unit=>unit.Enabled);
            var visual=controller.GetComponentsInChildren<SpriteRenderer>()
                .Single(renderer=>renderer.enabled&&renderer.name==actor.Name&&FormationArtAssertions.IsActualIdleFrame(actor.ArtId,renderer.sprite));
            var tile=GameObject.Find("Tile 05,06");
            Assert.That(tile,Is.Not.Null);
            var boardSurface=tile.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(boardSurface,Is.Not.Null);

            var actorFixture=new GameObject("Board actor depth fixture");
            var cameraFixture=new GameObject("Board camera depth fixture");
            var paint=GameObject.CreatePrimitive(PrimitiveType.Quad);
            var solid=GameObject.CreatePrimitive(PrimitiveType.Quad);
            var opaqueBlue=new Material(Shader.Find("Unlit/Color")){color=Color.blue};
            var body=actorFixture.AddComponent<SpriteRenderer>();
            var camera=cameraFixture.AddComponent<Camera>();
            RenderTexture target=null;
            try
            {
                // Isolate the actual board sprite, material and property block from its live HUD.
                actorFixture.layer=paint.layer=solid.layer=31;
                body.sprite=visual.sprite;
                body.sharedMaterial=visual.sharedMaterial;
                body.color=visual.color;
                body.flipX=visual.flipX;
                body.sortingOrder=visual.sortingOrder;
                var properties=new MaterialPropertyBlock();
                visual.GetPropertyBlock(properties);
                body.SetPropertyBlock(properties);
                body.transform.SetPositionAndRotation(visual.transform.position,visual.transform.rotation);
                body.transform.localScale=visual.transform.lossyScale;

                camera.CopyFrom(controller.battleCamera);
                camera.enabled=false;
                camera.cullingMask=1<<31;
                camera.rect=new Rect(0,0,1,1);
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=Color.clear;
                camera.transform.rotation=controller.battleCamera.transform.rotation;
                camera.orthographicSize=3;
                if(!camera.orthographic)camera.fieldOfView=2*Mathf.Atan(3f/20)*Mathf.Rad2Deg;
                Vector3 center=body.transform.position+camera.transform.up*.8f;
                camera.transform.position=center-camera.transform.forward*20;

                paint.transform.SetPositionAndRotation(center+camera.transform.forward*4,camera.transform.rotation);
                solid.transform.SetPositionAndRotation(center-camera.transform.forward*4,camera.transform.rotation);
                paint.transform.localScale=solid.transform.localScale=new Vector3(4,4,1);
                var paintRenderer=paint.GetComponent<Renderer>();
                var solidRenderer=solid.GetComponent<Renderer>();
                paintRenderer.sharedMaterial=boardSurface;
                solidRenderer.sharedMaterial=opaqueBlue;
                foreach(int height in new[]{720,1440})
                {
                    target=new RenderTexture(height*16/9,height,24,RenderTextureFormat.ARGB32);
                    camera.targetTexture=target;
                    paintRenderer.enabled=solidRenderer.enabled=false;
                    var reference=ReadCameraPixels(camera,target);
                    paintRenderer.enabled=true;
                    var withGround=ReadCameraPixels(camera,target);
                    solidRenderer.enabled=true;
                    var withSolid=ReadCameraPixels(camera,target);
                    int opaque=0,bleed=0,occluded=0;
                    for(int i=0;i<reference.Length;i++)
                    {
                        if(reference[i].a<254)continue;
                        opaque++;
                        if(Math.Abs(reference[i].r-withGround[i].r)>5||Math.Abs(reference[i].g-withGround[i].g)>5||
                            Math.Abs(reference[i].b-withGround[i].b)>5)bleed++;
                        if(withSolid[i].b>250&&withSolid[i].r<5&&withSolid[i].g<5)occluded++;
                    }
                    Assert.That(opaque,Is.GreaterThan(1000),"The fixture renders the authored sprite.");
                    Assert.That(bleed,Is.LessThan(opaque*.01f),"Rear board ground must not clip the character.");
                    Assert.That(occluded,Is.GreaterThan(opaque*.99f),"Opaque foreground must still occlude the character.");
                    camera.targetTexture=null;
                    target.Release();
                    Object.Destroy(target);
                    target=null;
                }
            }
            finally
            {
                camera.targetTexture=null;
                if(target!=null){target.Release();Object.Destroy(target);}
                Object.Destroy(actorFixture);
                Object.Destroy(cameraFixture);
                Object.Destroy(paint);
                Object.Destroy(solid);
                Object.Destroy(opaqueBlue);
            }
        }

        static Color32[] ReadCameraPixels(Camera camera,RenderTexture target)
        {
            var previous=RenderTexture.active;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try
            {
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                return pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active=previous;
                Object.Destroy(pixels);
            }
        }
    }
}
