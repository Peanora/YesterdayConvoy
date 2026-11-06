using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static void FireRequire(bool condition,string text){if(!condition)throw new InvalidOperationException("FIRE_RAIN_ORANGE: "+text);}
        static SpriteRenderer[] OrangeFireSprites()=>FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&(r.name=="Horizontal fire stream"||r.name=="Turbulent flame tail"||r.name=="Airborne fire tongue"||r.name=="Fire ember")).ToArray();
        // Keep the existing development-player switch compatible while verifying the restored presentation.
        IEnumerator VerifyFireRainV12(string directory,bool quit)
        {
            Directory.CreateDirectory(directory);float old=Time.timeScale;Time.timeScale=0;
            try
            {
                SwitchTrial(true);SelectCard(true);while(ArtLoading)yield return null;
                FireRequire(TryDeployAt(Battle.Entry).Success,"Existing Talulah deployment failed");Tick(4.05f);
                SelectSkillUnit(Battle.Units[0].EntityId);FireRequire(ActivateSelectedSkill()==SkillFailure.None,"Manual cast rejected");
                var unit=Battle.Units[0];double start=unit.FireRainStartedAt;
                Tick(1.25f);yield return null;
                var fire=FindAnyObjectByType<FireRainView>();var cam=Camera.main;
                FireRequire(fire.VisibleStreams>0,"Restored orange waves did not appear");
                var waves=OrangeFireSprites().Where(s=>s.name=="Horizontal fire stream").ToArray();
                FireRequire(waves.Length>0&&waves.All(s=>s.sharedMaterial.shader.name=="YesterdayConvoy/FireRainAdditive"),"Expected the early orange flames, not native pointed projectiles");
                FireRequire(OrangeFireSprites().All(s=>s.sharedMaterial.shader.isSupported),"Unsupported shader");
                var active=PatriotCapture(Path.Combine(directory,"fire-orange-early.png"));
                var effects=OrangeFireSprites();foreach(var s in effects)s.enabled=false;
                var glows=FindObjectsByType<MeshRenderer>().Where(r=>r.enabled&&r.name=="Fire reflected glow").ToArray();foreach(var g in glows)g.enabled=false;
                var hidden=PatriotCapture(null);foreach(var s in effects)s.enabled=true;foreach(var g in glows)g.enabled=true;
                double delta=active.Zip(hidden,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))).Average();
                FireRequire(delta>.05,"Orange flames changed no actual GPU pixels");
                var stream=waves[0];var position=stream.transform.position;var frame=stream.sprite;var cameraPosition=cam.transform.position;
                Battle.SetPaused(true);var paused=PatriotCapture(null);Tick(10);yield return null;
                var frozen=PatriotCapture(null);
                FireRequire(Battle.Clock==start+1.25&&stream.sprite==frame&&stream.transform.position==position,"Pause advanced orange flames");
                FireRequire(paused.SequenceEqual(frozen),"Pause advanced screen/heat pixels");
                FireRequire(cam.transform.position==cameraPosition,"Screen shake moved the logical camera");
                Battle.SetPaused(false);Tick(.2f);yield return null;
                FireRequire(stream.sprite!=frame&&cam.WorldToViewportPoint(stream.transform.position).x>cam.WorldToViewportPoint(position).x,"Flame is static or moving backwards");
                Tick(1.55f);yield return null;
                int peak=fire.VisibleStreams;FireRequire(peak>0,"Flames disappeared during the cast");
                PatriotCapture(Path.Combine(directory,"fire-orange-peak.png"));
                Tick(3.15f);yield return null;
                FireRequire(!Battle.IsSkillActionPlaying(unit)&&unit.Motion.DistanceTravelled>0,"Six-second cast duration changed");
                int hits=Battle.CombatShots.Count(s=>s.FireRain);
                FireRequire(unit.SkillAmmo==0&&hits>0&&hits<=18&&Battle.Defenders.All(d=>!d.Alive),"Original six waves did not complete");
                for(int i=0;i<400&&(fire.VisibleStreams>0||OrangeFireSprites().Any());i++){Tick(.05f);yield return null;}
                FireRequire(Battle.CombatShots.Count(s=>s.FireRain)==hits,"Afterglow added damage events");
                FireRequire(OrangeFireSprites().Length==0,"Natural completion left orange flames alive");
                RestartBattle();yield return null;FireRequire(fire.VisibleStreams==0&&OrangeFireSprites().Length==0,"Restart left fire alive");
                SwitchTrial(false);yield return null;FireRequire(OrangeFireSprites().Length==0,"Trial switch left fire alive");
                File.WriteAllText(Path.Combine(directory,"fire-orange-result.json"),"{\"passed\":true,\"peakStreams\":"+peak+",\"gpuDelta\":"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"damageEvents\":"+hits+",\"castSeconds\":6}");
                Debug.Log("FIRE_RAIN_ORANGE_PASS: restored CC0 flames, animated frames, pause, six-second cast, damage and cleanup");
            }
            finally{Time.timeScale=old;}
            if(quit)Application.Quit(0);
        }
    }
}
