using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationSteamSceneTests
    {
        FormationBattleController board;
        static object Invoke(FormationBattleController target,string name,params object[] args)=>
            typeof(FormationBattleController).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [TearDown]public void Freeze(){if(board?.Model!=null)board.Model.Paused=true;}

        [UnityTest]
        public IEnumerator OriginalBodyAndRealImpactsRenderOnTheSavedBoard()
        {
            FormationShopSession.StartNew(81,fullCampaign:true);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {board=UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();if(board?.Ready==true)break;yield return null;}
            Assert.That(board?.Ready,Is.True);
            string output=Path.GetFullPath("artifacts/steam-v2-integration/pixels");Directory.CreateDirectory(output);
            foreach(int tier in new[]{1,3})
            {
                yield return (IEnumerator)Invoke(board,"PrepareShowcaseRoleFixture",tier==3?new[]{"STEAM","B01","M03"}:new[]{"STEAM"},3,3,null,false,true,tier);
                Invoke(board,"BeginShowcaseFixture");board.Model.Paused=true;
                var machine=board.Model.Units.First();machine.Position=board.Model.Defenders.First().Position-Vector2.right*1.1f;machine.SkillCharge=machine.SkillRequired;
                void Advance(){board.Model.Paused=false;board.Model.Advance(.025f);board.Model.Paused=true;Invoke(board,"SyncActors");}
                Advance();var cast=board.Model.PresentationActions.Single(action=>action.ActorId==machine.Id&&action.Skill);
                Assert.That(machine.Facing.x,Is.GreaterThan(0),"The punch must face the target to the right.");
                Assert.That(cast.ContactAt-cast.At,Is.EqualTo(2.85).Within(.001));
                foreach(double at in tier==3?new[]{cast.ContactAt+.075,cast.At+4.7}:new[]{cast.ContactAt+.075})
                {
                    while(board.Model.Clock<at){Advance();yield return null;}
                    yield return null;Invoke(board,"SyncActors");yield return new WaitForEndOfFrame();
                    var effects=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                        .Where(r=>r.enabled&&r.name.StartsWith("Boss FX / boss/STEAM/",StringComparison.Ordinal)).ToArray();
                    Assert.That(effects,Is.Not.Empty);
                    string label="tier-"+tier+(at>cast.At+4?"-cannon":"-heavy");
                    var on=Capture(board.battleCamera);File.WriteAllBytes(Path.Combine(output,label+"-on.png"),on.EncodeToPNG());
                    foreach(var effect in effects)effect.enabled=false;
                    var off=Capture(board.battleCamera);File.WriteAllBytes(Path.Combine(output,label+"-off.png"),off.EncodeToPNG());
                    foreach(var effect in effects)effect.enabled=true;
                    var a=on.GetPixels32();var b=off.GetPixels32();int changed=0;
                    for(int pixel=0;pixel<a.Length;pixel++)
                        if(Math.Abs(a[pixel].r-b[pixel].r)+Math.Abs(a[pixel].g-b[pixel].g)+Math.Abs(a[pixel].b-b[pixel].b)>20)changed++;
                    Debug.Log("STEAM_PIXEL_EVIDENCE "+label+" changed="+changed);
                    Assert.That(changed,Is.GreaterThan(100),"The real camera must see the effect, not just a live renderer.");
                    UnityEngine.Object.Destroy(on);UnityEngine.Object.Destroy(off);
                    double frozen=board.Model.Clock;yield return null;yield return null;Assert.That(board.Model.Clock,Is.EqualTo(frozen));
                }
            }
        }
        static Texture2D Capture(Camera camera)
        {
            var prior=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;
            var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();return image;
            }
            finally{camera.targetTexture=prior;camera.rect=rect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
        }
    }
}
