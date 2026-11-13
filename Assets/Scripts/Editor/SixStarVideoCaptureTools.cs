using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Explicit editor-only recording. Uses production selection/deployment and the
    // real simulation clock; no HP/SP/target, damage or skill injection.
    [InitializeOnLoad]
    public static class SixStarVideoCaptureTools
    {
        const string Key="YesterdayConvoy.SixStarVideo.";
        const string Encoder="C:/Users/31016/AppData/Local/Microsoft/WinGet/Links/ffmpeg.exe";
        const int Width=1280,Height=720,Fps=30;
        static readonly string[] AllRoles={"D32","D33","D34","D35","D21","D22","D23","D24","D25","D26","D27","D28","D29","D30"};
        static int Frames=>SessionState.GetInt(Key+"frames",240);
        static string[] RequestedRoles=>SessionState.GetString(Key+"roles",string.Join(",",AllRoles)).Split(',');
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static bool running;static string fatal;static Manifest report;
        [Serializable] sealed class Role
        {
            public string id,name,skill,file;public int frames,deployments,casts,selectedAppliedBefore,selectedAppliedAfter,interactions;
            public double firstClock,endClock,skillStartedAt;
            public float focusX,focusY,defenderX,defenderY,enemyX,enemyY;
            public bool passed;
        }
        [Serializable] sealed class Manifest
        {
            public bool passed;public string error,sourceHEAD,recorderSHA256,graphics;
            public int width=Width,height=Height,fps=Fps;public float secondsPerRole;
            public bool audio=false,captureContainsGUI=false;
            public string scope="Production MonsterTrial controller, normal defender selection/preparation, finite legal pointer deployments, natural SP/automatic skills, actual camera.Render at one simulation tick per video frame. Camera-only gameplay, no OnGUI; not OS input or a natural campaign clear.";
            public List<Role> roles=new List<Role>();
        }
        static SixStarVideoCaptureTools()
        {
            EditorApplication.playModeStateChanged+=StateChanged;
            EditorApplication.delayCall+=Resume;
        }
        public static void CaptureAll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start the capture from Edit Mode.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes before recording.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-sixStarVideoDir");
            if(at<0||at+1>=args.Length)throw new ArgumentException("Supply -sixStarVideoDir with an external output directory.");
            string directory=Path.GetFullPath(args[at+1]);
            if(directory.StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Video output must be outside Assets.");
            if(!File.Exists(Encoder))throw new FileNotFoundException("FFmpeg is unavailable.",Encoder);
            int onlyAt=Array.IndexOf(args,"-sixStarVideoOnly"),framesAt=Array.IndexOf(args,"-sixStarVideoFrames");
            string[] selected=onlyAt<0?AllRoles:onlyAt+1<args.Length?args[onlyAt+1].Split(','):Array.Empty<string>();
            if(selected.Length==0||selected.Any(id=>!AllRoles.Contains(id))||selected.Distinct().Count()!=selected.Length)throw new ArgumentException("Unknown or duplicate requested video role.");
            int frames=240;
            if(framesAt>=0&&(framesAt+1>=args.Length||!int.TryParse(args[framesAt+1],out frames)||frames<60||frames>1800))throw new ArgumentException("Video frames must be between 60 and 1800.");
            SessionState.SetString(Key+"roles",string.Join(",",selected));SessionState.SetInt(Key+"frames",frames);
            Directory.CreateDirectory(directory);SessionState.SetString(Key+"directory",directory);SessionState.SetBool(Key+"requested",true);SessionState.SetBool(Key+"exit",false);
            SessionState.SetString(Key+"source",ReadSourceHead());SessionState.SetString(Key+"phase","waiting");
            EditorSceneManager.OpenScene("Assets/Scenes/MonsterTrial.unity",OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)Resume();
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"exit",false))
            {int code=SessionState.GetInt(Key+"code",1);SessionState.SetBool(Key+"exit",false);EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
        }
        static void Resume()
        {
            if(!SessionState.GetBool(Key+"requested",false)||!EditorApplication.isPlaying||running)return;
            running=true;
            try
            {
                if(SessionState.GetString(Key+"phase","")=="recording")throw new InvalidOperationException("Unexpected domain reload interrupted a recording.");
                SessionState.SetString(Key+"phase","recording");
                // Transient Play Mode instances only. The saved scene is never changed.
                foreach(var startup in Object.FindObjectsByType<CampaignBootstrap>(FindObjectsSortMode.None)){startup.StopAllCoroutines();startup.enabled=false;}
                report=new Manifest{sourceHEAD=SessionState.GetString(Key+"source",""),graphics=SystemInfo.graphicsDeviceName,secondsPerRole=Frames/(float)Fps};
                string source=Path.Combine(Application.dataPath,"Scripts/Editor/SixStarVideoCaptureTools.cs");
                using(var hash=System.Security.Cryptography.SHA256.Create())report.recorderSHA256=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant();
                var controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller==null)throw new InvalidOperationException("Saved MonsterTrial controller is missing.");
                controller.StartCoroutine(Drive(Record(controller)));
            }
            catch(Exception error){Finish(error);}
        }
        // Flatten child iterators so their exceptions cannot silently stop a Unity
        // coroutine and leave the unattended editor/encoder hanging.
        static IEnumerator Drive(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);Exception failure=null;
            while(stack.Count>0&&failure==null)
            {
                object current=null;bool next=false;
                try
                {
                    if(fatal!=null)throw new InvalidOperationException(fatal);
                    next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;
                    else{(stack.Pop() as IDisposable)?.Dispose();continue;}
                }
                catch(Exception error){failure=error;break;}
                if(current is IEnumerator child){stack.Push(child);continue;}
                yield return current;
            }
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){if(failure==null)failure=cleanup;}
            Finish(failure);
        }
        static IEnumerator Record(ManualBattleController controller)
        {
            float oldScale=Time.timeScale;bool oldBackground=Application.runInBackground;
            Application.LogCallback listener=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)fatal=message+"\n"+trace;};
            Application.logMessageReceived+=listener;
            try
            {
                Application.runInBackground=true;Time.timeScale=0;
                typeof(ManualBattleController).GetField("diagnostic",Private).SetValue(controller,true);
                float until=Time.realtimeSinceStartup+180;
                while(controller.Battle==null||controller.MonsterTests.Count==0||controller.ArtLoading)
                {if(Time.realtimeSinceStartup>until)throw new TimeoutException("Controller preparation timed out.");yield return null;}
                // Controller.Start initializes this flag, so set it again after readiness.
                typeof(ManualBattleController).GetField("diagnostic",Private).SetValue(controller,true);
                foreach(string id in RequestedRoles)
                {
                    controller.ReturnToMonsterTests();yield return null;
                    typeof(ManualBattleController).GetMethod("PrepareDefenderEntry",Private).Invoke(controller,new object[]{id});
                    until=Time.realtimeSinceStartup+180;
                    while(controller.ArtLoading){if(Time.realtimeSinceStartup>until)throw new TimeoutException("Art preparation timed out: "+id);yield return null;}
                    if(controller.DefenderSkillTrialId!=id)throw new InvalidOperationException("Production role selection failed: "+id);
                    controller.Tick(4);yield return null;var battle=controller.Battle;var defender=battle.FindDefender(id);
                    if(defender==null)throw new InvalidOperationException("Selected defender is absent: "+id);
                    var role=new Role{id=id,name=defender.Settings.Name,skill=ManualBattleController.DefenderSkillName(defender.Settings.Skill.Kind),file=id+".mp4"};report.roles.Add(role);
                    controller.StartBattle();battle.SetPaused(false);Vector2 gate=Camera.main.WorldToScreenPoint(battle.Entry);
                    bool Deploy(){controller.ProcessPointerDown(ManualBattleController.ScreenPosition(controller.CardSlot(2).center));controller.ProcessPointerMove(gate);bool accepted=controller.ProcessPointerUp(gate).Success;if(accepted)role.deployments++;return accepted;}
                    if(!Deploy())throw new InvalidOperationException("Initial legal deployment failed: "+id);
                    for(int tick=0;tick<6000&&defender.SkillCasts==0&&battle.Phase==BattlePhase.Running;tick++)
                    {
                        controller.Tick(.1f);yield return null;
                        if(role.deployments<6&&tick>20+(role.deployments-1)*20)Deploy();
                    }
                    if(defender.SkillCasts==0||!defender.Alive)throw new InvalidOperationException("No natural skill cast before recording: "+id);
                    yield return RecordRole(controller,battle,defender,role);
                    Debug.Log("SIXSTAR_VIDEO_ROLE "+JsonUtility.ToJson(role));Save();
                }
            }
            finally{Application.logMessageReceived-=listener;Time.timeScale=oldScale;Application.runInBackground=oldBackground;}
        }
        static IEnumerator RecordRole(ManualBattleController controller,ManualBattle battle,DefenderState defender,Role role)
        {
            var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldRect=camera.rect;float oldAspect=camera.aspect;
            var target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);var read=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
            int SelectedHits()=>battle.CombatShots.Count(s=>s.SourceId==role.id&&s.Applied&&(s.Empowered||s.DefenderSkillWasActive==true));
            int Passive()=>defender.BurnApplications+defender.SnowHits+defender.ChessTriggers+defender.FearApplications;
            int passiveBefore=Passive();float healedBefore=defender.SkillHealthRestored;
            role.firstClock=battle.Clock;role.skillStartedAt=defender.SkillStartedAt;role.selectedAppliedBefore=SelectedHits();
            using(var encoder=new VideoEncoder(Path.Combine(SessionState.GetString(Key+"directory",""),role.file)))
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=Width/(float)Height;
                var enemy=battle.Units.FirstOrDefault(u=>u.Active);Vector3 enemyPoint=enemy?.Motion.Position??defender.Position;
                var center=(defender.Position+enemyPoint)*.5f+camera.transform.up*.5f;var focus=camera.WorldToViewportPoint(center);
                var guardView=camera.WorldToViewportPoint(defender.Position);var enemyView=camera.WorldToViewportPoint(enemyPoint);
                role.focusX=Width*focus.x;role.focusY=Height*(1-focus.y);role.defenderX=Width*guardView.x;role.defenderY=Height*(1-guardView.y);role.enemyX=Width*enemyView.x;role.enemyY=Height*(1-enemyView.y);
                for(int frame=0;frame<Frames;frame++)
                {
                    if(battle.Phase!=BattlePhase.Running)throw new InvalidOperationException("Battle ended before the continuous take completed: "+role.id);
                    controller.Tick(1f/Fps);yield return null;
                    if(frame==120&&role.id=="D32"&&Environment.GetCommandLineArgs().Contains("-chenDepthProbe"))
                        CaptureChenDepthProbe(camera,read,role);
                    camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,Width,Height),0,0);read.Apply();
                    encoder.Write(read.GetRawTextureData());role.frames++;
                }
                encoder.Complete();
            }
            finally
            {
                camera.targetTexture=oldTarget;camera.rect=oldRect;camera.aspect=oldAspect;RenderTexture.active=oldActive;target.Release();Object.Destroy(target);Object.Destroy(read);
            }
            role.endClock=battle.Clock;role.casts=defender.SkillCasts;role.selectedAppliedAfter=SelectedHits();
            role.interactions=role.selectedAppliedAfter-role.selectedAppliedBefore+Passive()-passiveBefore+(defender.SkillHealthRestored>healedBefore?1:0);
            role.passed=role.frames==Frames&&Math.Abs(role.endClock-role.firstClock-Frames/(double)Fps)<.002&&role.casts>0&&role.interactions>0;
            if(!role.passed)throw new InvalidOperationException("Continuous take validation failed: "+JsonUtility.ToJson(role));
        }
        static void CaptureChenDepthProbe(Camera camera,Texture2D read,Role role)
        {
            string directory=SessionState.GetString(Key+"directory","");
            var heads=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.name.Contains("/chen3_skill_03_trail_01_dir_")).ToArray();
            var blocks=heads.Select(r=>{var p=new MaterialPropertyBlock();r.GetPropertyBlock(p);return p;}).ToArray();
            var terrain=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&(r.name.StartsWith("Cell ",StringComparison.Ordinal)||r.name.StartsWith("Platform cap ",StringComparison.Ordinal))).ToArray();
            void Save(string label){camera.Render();RenderTexture.active=camera.targetTexture;read.ReadPixels(new Rect(0,0,Width,Height),0,0);read.Apply();File.WriteAllBytes(Path.Combine(directory,label+".png"),read.EncodeToPNG());}
            try
            {
                Save("probe-normal");
                for(int i=0;i<heads.Length;i++){var alternate=new MaterialPropertyBlock();heads[i].GetPropertyBlock(alternate);alternate.SetFloat("_GroundDepth",1);heads[i].SetPropertyBlock(alternate);}
                Save("probe-head-ground-depth");
                for(int i=0;i<heads.Length;i++)heads[i].SetPropertyBlock(blocks[i]);
                foreach(var r in terrain)r.enabled=false;Save("probe-without-floor");
                File.WriteAllLines(Path.Combine(directory,"probe-heads.txt"),heads.Select(r=>r.name+" position="+r.transform.position+" bounds="+r.bounds+" ppu="+r.sprite.pixelsPerUnit+" pivot="+r.sprite.pivot));
            }
            finally{for(int i=0;i<heads.Length;i++)heads[i].SetPropertyBlock(blocks[i]);foreach(var r in terrain)r.enabled=true;}
        }
        static void Save(){if(report!=null)File.WriteAllText(Path.Combine(SessionState.GetString(Key+"directory",""),"manifest.json"),JsonUtility.ToJson(report,true));}
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key+"requested",false);SessionState.SetBool(Key+"exit",true);
            bool passed=error==null&&report!=null&&report.roles.Count==RequestedRoles.Length&&report.roles.All(r=>r.passed);SessionState.SetInt(Key+"code",passed?0:1);
            if(report!=null){report.passed=passed;report.error=error?.ToString()??"";Save();}
            if(error!=null)Debug.LogError("SIXSTAR_VIDEO_FAILED "+error);else Debug.Log("SIXSTAR_VIDEO_COMPLETE passed="+passed);
            if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else EditorApplication.delayCall+=()=>EditorApplication.Exit(passed?0:1);
        }
        static string ReadSourceHead()
        {
            var start=new ProcessStartInfo("git","rev-parse HEAD"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetFullPath(Path.Combine(Application.dataPath,".."))};
            using(var process=Process.Start(start)){string result=process.StandardOutput.ReadToEnd().Trim();if(!process.WaitForExit(5000)||process.ExitCode!=0)throw new InvalidOperationException("Cannot resolve source HEAD.");return result;}
        }
        static string Quote(string value)
        {
            var result=new StringBuilder("\"");int slashes=0;
            foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='\"'){result.Append('\\',slashes*2+1);result.Append(c);}else{result.Append('\\',slashes);result.Append(c);}slashes=0;}
            return result.Append('\\',slashes*2).Append('"').ToString();
        }
        sealed class VideoEncoder:IDisposable
        {
            readonly Process process;readonly StringBuilder errors=new StringBuilder();bool completed;
            internal VideoEncoder(string file)
            {
                string args="-hide_banner -loglevel error -nostdin -y -f rawvideo -pixel_format rgba -video_size 1280x720 -framerate 30 -i pipe:0 -an -vf vflip -c:v libx264 -preset veryfast -crf 19 -pix_fmt yuv420p -threads 2 -movflags +faststart "+Quote(file);
                var start=new ProcessStartInfo(Encoder,args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardError=true};
                process=new Process{StartInfo=start};process.ErrorDataReceived+=(sender,e)=>{if(e.Data!=null)lock(errors)errors.AppendLine(e.Data);};
                if(!process.Start())throw new InvalidOperationException("FFmpeg did not start.");process.BeginErrorReadLine();
            }
            internal void Write(byte[] frame){if(process.HasExited)throw new InvalidOperationException("FFmpeg exited early: "+errors);process.StandardInput.BaseStream.Write(frame,0,frame.Length);}
            internal void Complete(){process.StandardInput.Close();if(!process.WaitForExit(30000)){process.Kill();throw new TimeoutException("FFmpeg finalization timed out.");}process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("FFmpeg failed: "+errors);completed=true;}
            public void Dispose(){if(!completed&&!process.HasExited){try{process.StandardInput.Close();}catch{}if(!process.WaitForExit(5000))process.Kill();}process.Dispose();}
        }
    }
}
