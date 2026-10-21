using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class CurrentPresentationTests
    {
        private float originalScale;
        [SetUp] public void Setup() => originalScale=Time.timeScale;
        [TearDown] public void TearDown() => Time.timeScale=originalScale;
        [UnityTest]
        public IEnumerator GroundPaintStaysBehindComicActorAt720And1440WhileSolidsStillOcclude()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Requires a real graphics device; a null-device pass cannot verify occlusion.");
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("InitialDemo");
            yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            c.SwitchTrial(false);
            c.SelectCard(true); Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            var unit=c.Battle.Units[0]; unit.Motion.Advance(2.72f/unit.Motion.Speed); c.Tick(0);
            yield return null;
            Vector3 foot=unit.Motion.Position;
            var visual=GameObject.Find(unit.EntityId).GetComponentInChildren<DemoSpriteVisual>();
            Camera camera = Camera.main;
            int oldMask = camera.cullingMask;
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            float oldFieldOfView = camera.fieldOfView;
            Color oldBackground = camera.backgroundColor;
            CameraClearFlags oldFlags = camera.clearFlags;
            RenderTexture oldTarget = camera.targetTexture;
            var foregroundMaterial = new Material(Shader.Find("Unlit/Color")) { color = Color.blue };
            GameObject paint = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject solid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var rearPlatform = new Renderer[3];
            string[] platformNames = {"Cell -4,0","Platform cap -4,0","Warning lip -4,0"};
            int[] platformLayers = new int[3];
            for(int i=0;i<rearPlatform.Length;i++)
            {
                rearPlatform[i]=GameObject.Find("Industrial Map/"+platformNames[i]).GetComponent<Renderer>();
                platformLayers[i]=rearPlatform[i].gameObject.layer;
                rearPlatform[i].gameObject.layer=31;
            }
            RenderTexture target = null;
            try
            {
                // Isolate the real walking cutouts and the map's actual yellow material.
                // This tests final pixels, rather than inferring visibility from sortingOrder alone.
                foreach (Renderer part in visual.GetComponentsInChildren<Renderer>(true)) part.gameObject.layer = 31;
                paint.layer = solid.layer = 31;
                paint.transform.rotation = solid.transform.rotation = camera.transform.rotation;
                paint.transform.localScale = solid.transform.localScale = new Vector3(4,4,1);
                Vector3 center = foot + camera.transform.up * 0.85f;
                paint.transform.position = center + camera.transform.forward * 0.3f;
                solid.transform.position = center - camera.transform.forward * 0.3f;
                Renderer paintRenderer = paint.GetComponent<Renderer>(), solidRenderer = solid.GetComponent<Renderer>();
                paintRenderer.sharedMaterial = GameObject.Find("Industrial Map/Warning lip -4,0").GetComponent<Renderer>().sharedMaterial;
                solidRenderer.sharedMaterial = foregroundMaterial;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographicSize = 3;
                // Keep the fixture's former three-unit half-height when the actual camera is perspective.
                if(!camera.orthographic) camera.fieldOfView=2*Mathf.Atan(3f/20)*Mathf.Rad2Deg;
                camera.transform.position = foot + camera.transform.up * 0.2f - camera.transform.forward * 20;
                int firstOrder = 0;
                foreach (int height in new[]{720,1440})
                {
                    target = new RenderTexture(height*16/9,height,24,RenderTextureFormat.ARGB32);
                    camera.targetTexture = target;
                    paintRenderer.enabled = solidRenderer.enabled = false;
                    foreach(Renderer part in rearPlatform)part.enabled=false;
                    yield return null; // Let DemoSpriteVisual update for this actual render target resolution.
                    Color32[] reference = ReadCameraPixels(camera,target);
                    paintRenderer.enabled = true;
                    foreach(Renderer part in rearPlatform)part.enabled=true;
                    Color32[] withPaint = ReadCameraPixels(camera,target);
                    solidRenderer.enabled = true;
                    Color32[] withSolid = ReadCameraPixels(camera,target);
                    int opaque = 0, bleed = 0, occluded = 0;
                    for (int i=0;i<reference.Length;i++)
                    {
                        if(reference[i].a < 254) continue;
                        opaque++;
                        if(Mathf.Abs(reference[i].r-withPaint[i].r)>5 || Mathf.Abs(reference[i].g-withPaint[i].g)>5 || Mathf.Abs(reference[i].b-withPaint[i].b)>5) bleed++;
                        if(withSolid[i].b>250 && withSolid[i].r<5 && withSolid[i].g<5) occluded++;
                    }
                    Debug.Log($"GROUND_SORT_PIXELS: height={height}; opaque={opaque}; paintBleed={bleed}; solidOccluded={occluded}; actorOrder={visual.Renderer.sortingOrder}.");
                    Assert.That(opaque,Is.GreaterThan(1000),"The fixture must actually render the authored walking character.");
                    Assert.That(bleed,Is.LessThan(opaque*0.01f),"Yellow paint and the actual rear platform must not cut through the character.");
                    Assert.That(occluded,Is.GreaterThan(opaque*0.99f),"A real opaque foreground must still occlude the character; do not disable depth testing.");
                    if(height==720) firstOrder=visual.Renderer.sortingOrder;
                    else Assert.That(visual.Renderer.sortingOrder,Is.EqualTo(firstOrder),"Resolution must not change the character's world sorting order.");
                    camera.targetTexture = oldTarget;
                    target.Release();
                    Object.Destroy(target);
                    target = null;
                }
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.cullingMask = oldMask;
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
                camera.fieldOfView = oldFieldOfView;
                camera.backgroundColor = oldBackground;
                camera.clearFlags = oldFlags;
                if(target!=null) { target.Release(); Object.Destroy(target); }
                Object.Destroy(paint);
                Object.Destroy(solid);
                Object.Destroy(foregroundMaterial);
                for(int i=0;i<rearPlatform.Length;i++)
                {
                    rearPlatform[i].enabled=true;
                    rearPlatform[i].gameObject.layer=platformLayers[i];
                }
            }
        }

        private static Color32[] ReadCameraPixels(Camera camera,RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            var pixels = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                return pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(pixels);
            }
        }

    }
}
