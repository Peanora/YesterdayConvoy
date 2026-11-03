using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FieldAnimationSceneTests
    {
        [UnityTest] public IEnumerator SixNativeIntrosFreezeOnPauseAndReturnToIdleWithoutMovingActors()
        {
            float previous=Time.timeScale;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();
                foreach(string id in new[]{"D21","D22","D27","D28","D29","D30"})
                {
                    Assert.That(c.TryEnterDefenderSkillTrial(id),Is.True);yield return null;
                    var view=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual(id);
                    c.Tick(.25f);yield return null;Assert.That(view.sprite.name,Does.Contain("/start/"),id);
                    var frame=view.sprite;var position=view.transform.position;int dp=c.Battle.DP;
                    c.StartBattle();c.Battle.SetPaused(true);c.Tick(3);yield return null;
                    Assert.That(view.sprite,Is.SameAs(frame));Assert.That(view.transform.position,Is.EqualTo(position));
                    Assert.That(c.Battle.DP,Is.EqualTo(dp));Assert.That(c.Battle.Clock,Is.Zero);
                    c.Battle.SetPaused(false);c.Tick(1.2f);yield return null;
                    Assert.That(view.sprite.name,Does.Contain("/idle/"),id);Assert.That(view.transform.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(Quaternion.Angle(view.transform.rotation,Camera.main.transform.rotation),Is.LessThan(.01));
                    c.RestartBattle();yield return null;
                    var restarted=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual(id);
                    Assert.That(restarted.sprite.name,Does.Contain("/start/"));
                    c.ReturnToMonsterTests();yield return null;yield return null;
                }
                c.StartLateBattleShopLoop(73);c.Tick(1.3f);yield return null;
                CaptureCamera("field-late-idle");c.LeaveShopTrial();
            }
            finally{Time.timeScale=previous;}
        }

        [UnityTest] public IEnumerator RealChessEventsPlayOriginalSpritesFreezeAndCleanUpOnRestart()
        {
            float previous=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("InitialDemo");yield return null;Time.timeScale=0;
                var skill=new DefenderSkillSettings(DefenderSkillKind.WangGambit,1,1,1,99999,effectRadius:10);
                var b=new ManualBattle(new ManualBattleSettings("c","u",30,1,12,1,.1f,100000,1,1,1,1,1),new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                    new EngagementSettings(unitAttack:1,unitInterval:100,unitRange:.1f),roster:new[]{new DefenderSettings("wang","wang","D28",Vector3.right*2,10000,20,0,10,2,actionDuration:.5f,skill:skill)});
                root=new GameObject("Wang native visual test");root.AddComponent<AdvancedDefenderView>().Bind(()=>b);
                b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);
                bool native=false,trigger=false,paused=false,captured=false;
                for(int i=0;i<150;i++)
                {
                    b.Advance(.1f);yield return null;
                    var rendered=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null).ToArray();
                    native|=rendered.Any(r=>r.sprite.name.StartsWith("D28Chess/"));
                    trigger|=rendered.Any(r=>r.sprite.name.Contains("/attack/"));
                    if(!captured&&rendered.Any(r=>r.sprite.name.Contains("/attack/"))){CaptureCamera("field-native-chess");captured=true;}
                    if(!paused&&rendered.Length>0)
                    {
                        b.SetPaused(true);var frames=rendered.Select(r=>r.sprite).ToArray();double clock=b.Clock;b.Advance(5);yield return null;
                        Assert.That(b.Clock,Is.EqualTo(clock));for(int k=0;k<rendered.Length;k++)Assert.That(rendered[k].sprite,Is.SameAs(frames[k]));
                        b.SetPaused(false);paused=true;
                    }
                }
                Assert.That(native&&trigger&&paused,Is.True,"native="+native+" trigger="+trigger+" paused="+paused+" cells="+b.ChessPieces.Count+" triggers="+b.Defender.ChessTriggers+" casts="+b.Defender.SkillCasts+" clock="+b.Clock+" phase="+b.Phase+" sprites="+string.Join(",",root.GetComponentsInChildren<SpriteRenderer>().Select(r=>r.sprite==null?"null":r.sprite.name)));
                Assert.That(b.Defender.ChessTriggers,Is.GreaterThan(0));
                b.Restart();yield return null;yield return null;
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(),Is.Empty);
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=previous;}
        }

        static void CaptureCamera(string name)
        {
            var camera=Camera.main;var before=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.aspect=1280f/720;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            }
            finally{camera.targetTexture=before;camera.aspect=aspect;RenderTexture.active=active;Object.Destroy(image);Object.Destroy(target);}
        }
    }
}
