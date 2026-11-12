using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class PramanixPersistentRingSceneTests
    {
        static readonly string[] Keys={"sbell2_skill_03_buff_01","sbell2_skill_03_range","sbell2_skill_03_flow"};
        const int Layer=29;
        readonly struct Pixels
        {
            internal readonly int Visible;
            internal readonly double Light;
            internal Pixels(Color32[] pixels)
            {
                int visible=0;double light=0;
                foreach(var p in pixels)
                {
                    if(p.r+p.g+p.b>12)visible++;
                    light+=(.2126*p.r+.7152*p.g+.0722*p.b)/255;
                }
                Visible=visible;Light=light;
            }
        }
        static Pixels[] Capture(GameObject root,Camera camera,RenderTexture target,Texture2D read)
        {
            var all=root.GetComponentsInChildren<SpriteRenderer>();var result=new Pixels[Keys.Length];
            for(int i=0;i<Keys.Length;i++)
            {
                string name="Original VFX / defender/D27/"+Keys[i];
                var field=all.SingleOrDefault(r=>r.enabled&&r.name==name);
                Assert.That(field,Is.Not.Null,"The actual director must retain "+Keys[i]+" throughout the skill.");
                // Only one genuine persistent layer reaches this camera. Attack/hit pulses,
                // snow tiles and other scene renderers cannot conceal a disappearing ring.
                foreach(var r in all)r.gameObject.layer=r==field?Layer:0;
                camera.Render();RenderTexture.active=target;
                read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();
                result[i]=new Pixels(read.GetPixels32());
            }
            foreach(var r in all)r.gameObject.layer=0;
            return result;
        }
        static IEnumerator AdvanceTo(ManualBattle battle,DefenderState caster,double age)
        {
            // Land just AFTER the boundary so floating-point rounding cannot sample
            // the final pre-wrap frame and falsely accept the old zero-age loop.
            double target=caster.SkillStartedAt+age+.00001;
            for(int tick=0;tick<200&&battle.Clock<target-.000001;tick++)
            {battle.Advance((float)Math.Min(.05,target-battle.Clock));yield return null;}
            Assert.That(battle.Clock-caster.SkillStartedAt,Is.EqualTo(age+.00001).Within(.0001));
            Assert.That(battle.DefenderSkillActive(caster),Is.True);
            yield return null;
        }
        static void AssertContinuous(Pixels[] before,Pixels[] after,double age)
        {
            for(int i=0;i<Keys.Length;i++)
            {
                string label=Keys[i]+" at skill age "+age;
                Debug.Log("PRAMANIX_RING_GPU "+label+" pixels="+before[i].Visible+"->"+after[i].Visible+
                    "; totalLight="+before[i].Light.ToString("F3")+"->"+after[i].Light.ToString("F3"));
                Assert.That(before[i].Visible,Is.GreaterThan(20),label+" requires a visible pre-boundary sample.");
                Assert.That(after[i].Visible,Is.GreaterThan(20),label+" cannot restart from a dark initial frame.");
                Assert.That(after[i].Visible,Is.GreaterThanOrEqualTo(before[i].Visible*.65),label+" loses too much visible area at wrap.");
                Assert.That(after[i].Light,Is.GreaterThanOrEqualTo(before[i].Light*.65),label+" loses too much total light at wrap.");
            }
        }
        static void AssertOpaqueBodyRemainsUnchanged(GameObject root,Camera camera,RenderTexture target,Texture2D read,Vector3 foot)
        {
            var all=root.GetComponentsInChildren<SpriteRenderer>();var layers=all.Select(r=>r.gameObject.layer).ToArray();
            var enabled=all.Select(r=>r.enabled).ToArray();
            var fields=all.Where(r=>r.enabled&&Keys.Any(key=>r.name=="Original VFX / defender/D27/"+key)).ToArray();
            Assert.That(fields.Length,Is.EqualTo(3));
            var texture=new Texture2D(4,6,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point};
            texture.SetPixels32(Enumerable.Repeat(new Color32(142,78,37,255),24).ToArray());texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,4,6),new Vector2(.5f,0),3);
            var material=new Material(Resources.Load<Shader>("TacticalActor"));
            var bodyObject=new GameObject("Opaque actor body GPU mask",typeof(SpriteRenderer));bodyObject.layer=Layer;
            var body=bodyObject.GetComponent<SpriteRenderer>();body.sprite=sprite;body.sharedMaterial=material;
            body.transform.SetPositionAndRotation(foot,camera.transform.rotation);
            body.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,foot);
            Assert.That(body.sortingOrder,Is.GreaterThan(1024));
            Color32[] Read()
            {
                camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();
                return read.GetPixels32();
            }
            bool Same(Color32 a,Color32 b)=>a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;
            try
            {
                foreach(var r in all)r.gameObject.layer=fields.Contains(r)?Layer:0;
                body.enabled=false;var fieldsOnly=Read();
                foreach(var r in fields)r.enabled=false;
                body.enabled=true;var bodyOnly=Read();
                foreach(var r in fields)r.enabled=true;
                var combined=Read();
                var center=camera.WorldToViewportPoint(foot+camera.transform.up);
                int centerX=Mathf.Clamp((int)(center.x*target.width),0,target.width-1);
                int centerY=Mathf.Clamp((int)(center.y*target.height),0,target.height-1);
                var opaqueColor=bodyOnly[centerY*target.width+centerX];
                Assert.That(opaqueColor.r+opaqueColor.g+opaqueColor.b,Is.GreaterThan(30),"The opaque body itself must render.");
                int mask=0,potentialOverlap=0,changedBody=0,visibleOutside=0;
                for(int i=0;i<bodyOnly.Length;i++)
                {
                    if(Same(bodyOnly[i],opaqueColor))
                    {
                        mask++;
                        if(fieldsOnly[i].r+fieldsOnly[i].g+fieldsOnly[i].b>12)potentialOverlap++;
                        if(!Same(bodyOnly[i],combined[i]))changedBody++;
                    }
                    else if(Math.Abs(combined[i].r-bodyOnly[i].r)+Math.Abs(combined[i].g-bodyOnly[i].g)+Math.Abs(combined[i].b-bodyOnly[i].b)>12)
                        visibleOutside++;
                }
                Debug.Log("PRAMANIX_BODY_GPU mask="+mask+"; potentialFxOverlap="+potentialOverlap+"; changedBody="+changedBody+"; visibleFxOutside="+visibleOutside);
                Assert.That(mask,Is.GreaterThan(100));
                Assert.That(potentialOverlap,Is.GreaterThan(20),"The body must genuinely overlap visible native light for this occlusion check to mean anything.");
                Assert.That(changedBody,Is.Zero,"Persistent ground light must leave every opaque body-mask pixel unchanged.");
                Assert.That(visibleOutside,Is.GreaterThan(20),"The native effects remain visible around the body; they were not globally hidden.");
            }
            finally
            {
                for(int i=0;i<all.Length;i++){all[i].enabled=enabled[i];all[i].gameObject.layer=layers[i];}
                body.enabled=false;Object.Destroy(bodyObject);Object.Destroy(material);Object.Destroy(sprite);Object.Destroy(texture);
            }
        }

        [UnityTest]public IEnumerator NaturalSkillKeepsAllThreePersistentLayersBrightAcrossBothLoopBoundaries()
        {
            Assume.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            float priorTime=Time.timeScale;var priorActive=RenderTexture.active;var priorMain=Camera.main;
            GameObject root=null,cameraObject=null;RenderTexture target=null;Texture2D read=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                priorMain=Camera.main;Assert.That(priorMain,Is.Not.Null);
                foreach(string key in Keys)yield return OriginalCombatVfxArt.Get("defender/D27/"+key).Sequence.Warm();
                priorMain.tag="Untagged";
                cameraObject=new GameObject("Isolated real Pramanix persistent-layer camera");
                var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.enabled=true;
                camera.orthographic=true;camera.orthographicSize=4.5f;camera.aspect=1;camera.cullingMask=1<<Layer;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.transform.position=new Vector3(0,8,-5);camera.transform.LookAt(Vector3.zero);
                target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                read=new Texture2D(256,256,TextureFormat.RGBA32,false,true);camera.targetTexture=target;
                var skill=new DefenderSkillSettings(DefenderSkillKind.PramanixSnow,20,20,1,15,effectRadius:20);
                var guard=new DefenderSettings("persistent-snow","persistent-snow","D27",Vector3.zero,100000,1,0,20,2,
                    windup:.15f,actionDuration:.5f,skill:skill,canAttack:false);
                var battle=new ManualBattle(new ManualBattleSettings("ring-card","ring-unit",30,1,20,1,.1f,100000,.1f,1,1,1,1),
                    new[]{Vector3.right*2,Vector3.right*2+Vector3.forward*100},Vector3.zero,
                    new EngagementSettings(unitAttack:1,unitRange:.01f,unitInterval:100),roster:new[]{guard});
                root=new GameObject("Natural D27 persistent-ring fixture");
                root.AddComponent<DefenderSkillVfxView>().Bind(()=>battle,()=>battle.Clock);
                battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
                for(int tick=0;tick<20&&battle.Defender.SkillCasts==0;tick++){battle.Advance(.05f);yield return null;}
                var caster=battle.Defender;Assert.That(caster.SkillCasts,Is.EqualTo(1),"The skill must begin through normal SP/threat logic.");
                yield return AdvanceTo(battle,caster,2.95);var firstBefore=Capture(root,camera,target,read);
                yield return AdvanceTo(battle,caster,3);var firstAfter=Capture(root,camera,target,read);
                AssertContinuous(firstBefore,firstAfter,3);
                yield return AdvanceTo(battle,caster,4.95);var secondBefore=Capture(root,camera,target,read);
                yield return AdvanceTo(battle,caster,5);var secondAfter=Capture(root,camera,target,read);
                AssertContinuous(secondBefore,secondAfter,5);
                double clock=battle.Clock;battle.SetPaused(true);
                for(int i=0;i<3;i++){battle.Advance(1);yield return null;}
                var paused=Capture(root,camera,target,read);Assert.That(battle.Clock,Is.EqualTo(clock));
                for(int i=0;i<Keys.Length;i++)
                {Assert.That(paused[i].Visible,Is.EqualTo(secondAfter[i].Visible));Assert.That(paused[i].Light,Is.EqualTo(secondAfter[i].Light).Within(.001));}
                AssertOpaqueBodyRemainsUnchanged(root,camera,target,read,caster.Position);
            }
            finally
            {
                if(priorMain!=null)priorMain.tag="MainCamera";
                if(cameraObject!=null){cameraObject.GetComponent<Camera>().targetTexture=null;Object.Destroy(cameraObject);}
                RenderTexture.active=priorActive;
                if(target!=null){target.Release();Object.Destroy(target);}if(read!=null)Object.Destroy(read);
                if(root!=null)Object.Destroy(root);Time.timeScale=priorTime;
            }
            yield return null;
        }
    }
}
