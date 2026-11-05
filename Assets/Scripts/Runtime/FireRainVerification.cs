using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static void FireRequire(bool condition,string text){if(!condition)throw new InvalidOperationException("FIRE_RAIN_V12: "+text);}
        static SpriteRenderer[] NativeFireSprites()=>FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Talulah ")).ToArray();
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
                FireRequire(fire.VisibleStreams==5,"First visual front should have five aligned rows");
                var waves=NativeFireSprites().Where(s=>s.name=="Talulah native fire stream").ToArray();
                FireRequire(waves.Select(s=>Mathf.Round(s.transform.position.x*100)).Distinct().Count()==1,"Rows are not aligned");
                FireRequire(NativeFireSprites().Any(s=>s.name=="Talulah ground flame"),"Ground did not ignite");
                FireRequire(NativeFireSprites().All(s=>s.sharedMaterial.shader.isSupported),"Unsupported shader");
                var active=PatriotCapture(Path.Combine(directory,"fire-v12-early.png"));
                var effects=NativeFireSprites();foreach(var s in effects)s.enabled=false;
                var heat=FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Talulah ground heat");heat.enabled=false;
                var hidden=PatriotCapture(null);foreach(var s in effects)s.enabled=true;heat.enabled=true;
                double delta=active.Zip(hidden,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))).Average();
                FireRequire(delta>.05,"Native fire changed no actual GPU pixels");
                var stream=waves[0];var position=stream.transform.position;var frame=stream.sprite;var cameraPosition=cam.transform.position;
                Battle.SetPaused(true);var paused=PatriotCapture(null);Tick(10);yield return null;
                var frozen=PatriotCapture(null);
                FireRequire(Battle.Clock==start+1.25&&stream.sprite==frame&&stream.transform.position==position,"Pause advanced native fire");
                FireRequire(paused.SequenceEqual(frozen),"Pause advanced screen/heat pixels");
                FireRequire(cam.transform.position==cameraPosition,"Screen shake moved the logical camera");
                Battle.SetPaused(false);Tick(.2f);yield return null;
                FireRequire(stream.sprite!=frame&&stream.transform.position.x>position.x,"Native flame is static or moving backwards");
                Tick(4.7f);yield return null;
                FireRequire(!Battle.IsSkillActionPlaying(unit)&&unit.Motion.DistanceTravelled>0,"Six-second cast duration changed");
                int hits=Battle.CombatShots.Count(s=>s.FireRain);
                FireRequire(unit.SkillAmmo==0&&hits>0&&hits<=18&&Battle.Defenders.All(d=>!d.Alive),"Original six waves did not complete");
                PatriotCapture(Path.Combine(directory,"fire-v12-ground.png"));
                int ground=NativeFireSprites().Count(s=>s.name=="Talulah ground flame");FireRequire(ground>50,"Ground coverage is incomplete");
                for(int i=0;i<400&&(fire.VisibleStreams>0||NativeFireSprites().Any());i++){Tick(.05f);yield return null;}
                FireRequire(Battle.CombatShots.Count(s=>s.FireRain)==hits,"Afterglow added damage events");
                FireRequire(NativeFireSprites().Length==0&&!heat.enabled,"Natural completion left native fire alive");
                RestartBattle();yield return null;FireRequire(fire.VisibleStreams==0&&NativeFireSprites().Length==0,"Restart left fire alive");
                SwitchTrial(false);yield return null;FireRequire(NativeFireSprites().Length==0,"Trial switch left fire alive");
                File.WriteAllText(Path.Combine(directory,"fire-v12-result.json"),"{\"passed\":true,\"groundFlames\":"+ground+",\"gpuDelta\":"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"damageEvents\":"+hits+",\"castSeconds\":6}");
                Debug.Log("FIRE_RAIN_V12_PASS: native frames, aligned rows, ground coverage, pause, six-second cast, damage, tail and cleanup");
            }
            finally{Time.timeScale=old;}
            if(quit)Application.Quit(0);
        }
    }
}
