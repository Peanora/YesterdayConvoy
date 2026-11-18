using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Explicit local review tool. It never installs during an ordinary launch.
    public sealed class FormationShowcaseRecorder : MonoBehaviour
    {
        [Serializable] public sealed class Stage {public string label;public float start,duration;}
        [Serializable] public sealed class Clip
        {
            public string id,kind,name,file,notes,error;
            public string[] units,activeSynergies;
            public List<Stage> stages=new List<Stage>();
            public int frames,skillCasts,hits;
            public float duration,movedDistance;
        }
        [Serializable] public sealed class Manifest
        {
            public string sourceCommit,scope="当前保存棋盘与生产v0.7动作/技能/特效的GPU实录。预置一星编队、右侧起点及高生命靶；片中标注跳过的等待。主角色数值/技力不改，不是自然对局或原作完整还原。";
            public int width=1280,height=720,fps=24;
            public bool audio=false,completed;
            public List<Clip> clips=new List<Clip>();
            public string error;
        }
        sealed class Plan
        {
            internal string Id,Kind,Name;
            internal string[] Units;
            internal int Cost=3,Skill=3;
            internal bool Phase;
        }
        static FormationShowcaseRecorder current;
        FormationBattleController controller;
        Manifest manifest=new Manifest();
        Process encoder;
        string directory,ffmpeg,phase="准备素材",encoderError="",runtimeError;
        Clip clip;
        bool writing,done;
        const float Step=1f/24;
        public static bool Drives(FormationBattleController target)=>current!=null&&current.controller==target&&!current.done;
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-formationShowcase")>=0;
        static string Argument(string key,string fallback)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>current=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            if(!Requested||current!=null)return;
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var host=new GameObject("Opt-in formation video review");DontDestroyOnLoad(host);current=host.AddComponent<FormationShowcaseRecorder>();
        }
        void OnEnable()=>Application.logMessageReceived+=Observe;
        void OnDisable()=>Application.logMessageReceived-=Observe;
        void Observe(string text,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeError=text+"\n"+stack;}
        IEnumerator Start()=>Guard(Record());
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0&&!done)
            {
                object next=null;bool more=false;Exception failure=null;
                try {if(runtimeError!=null)throw new InvalidOperationException(runtimeError);more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null){Finish(failure);yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator child){stack.Push(child);continue;}
                yield return next;
            }
            if(!done)Finish(null);
        }
        IEnumerable<Plan> Plans()
        {
            foreach(var unit in FormationCampaignRules.AllUnits)
                yield return new Plan{Id=unit.Id,Kind="monster",Name=unit.Name,Units=new[]{unit.Id}};
            foreach(int cost in new[]{4,5})yield return new Plan{Id="TAL"+cost,Kind="variant",Name="塔露拉 · "+cost+"费",Units=new[]{"TAL"},Cost=cost};
            foreach(int skill in new[]{1,2})yield return new Plan{Id="WIS"+skill,Kind="variant",Name="维维美 · 技能"+skill,Units=new[]{"WIS"},Skill=skill};
            foreach(string id in new[]{"M20","M32","M14","M30","B04","B08","B13","B24"})
                yield return new Plan{Id=id+"-phase",Kind="variant",Name=FormationCampaignRules.Find(id).Name+" · 阶段/返场",Units=new[]{id},Phase=true};
            foreach(var tag in FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles))
            {
                string[] units=FormationCampaignRules.AllUnits.Where(u=>u.Factions.Contains(tag.Id)||u.Styles.Contains(tag.Id)).OrderBy(u=>u.Cost).Take(10).Select(u=>u.Id).ToArray();
                if(units.Length>0)yield return new Plan{Id="tag-"+tag.Id,Kind="synergy",Name=tag.Name,Units=units};
            }
        }
        IEnumerator Record()
        {
            directory=Path.GetFullPath(Argument("-showcaseDir","artifacts/formation-showcase"));Directory.CreateDirectory(Path.Combine(directory,"clips"));
            ffmpeg=Argument("-showcaseFFmpeg","ffmpeg");
            string manifestPath=Path.Combine(directory,"manifest.json");
            if(Argument("-showcaseResume","0")=="1"&&File.Exists(manifestPath))manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            manifest.sourceCommit=Argument("-showcaseSource","unspecified");manifest.completed=false;manifest.error=null;
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while((controller=Object.FindAnyObjectByType<FormationBattleController>())==null||!controller.Ready)
            {if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Saved formation scene did not become ready.");yield return null;}
            string only=Argument("-showcaseOnly","");var selected=new HashSet<string>(only.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries));
            var plans=Plans().ToArray();int order=0;
            foreach(var plan in plans)
            {
                order++;if(selected.Count>0&&!selected.Contains(plan.Id))continue;
                var existing=manifest.clips.FirstOrDefault(c=>c.id==plan.Id);
                if(existing!=null&&existing.error==null&&existing.frames>0&&File.Exists(Path.Combine(directory,existing.file)))continue;
                if(existing!=null)manifest.clips.Remove(existing);
                clip=new Clip{id=plan.Id,kind=plan.Kind,name=plan.Name,units=plan.Units,file="clips/"+order.ToString("D2")+"-"+SafeId(plan.Id)+".mp4"};
                manifest.clips.Add(clip);writing=false;phase="准备素材";Save();
                Debug.Log("FORMATION_SHOWCASE_START: "+order+"/"+plans.Length+" "+plan.Id);
                yield return controller.PrepareShowcaseFixture(plan.Units,plan.Cost,plan.Skill);
                controller.BeginShowcaseFixture();controller.Model.Paused=false;
                clip.activeSynergies=controller.Model.ActiveSynergies.Where(p=>p.Value>0).Select(p=>p.Key+" "+p.Value).ToArray();
                clip.notes=Notes(plan)+" "+controller.ShowcaseFixtureNote;
                if(plan.Phase)yield return RecordPhase(plan);
                else
                {
                    yield return Segment("走路与接敌 · 当前棋盘原比例",5);
                    yield return Segment("普攻 / 常驻被动",3);
                    var actor=controller.Model.Units.First();
                    bool healer=plan.Units.Any(id=>FormationCampaignRules.Find(id).Effect.Contains("治疗")||FormationCampaignRules.Find(id).Effect.Contains("回复生命"));
                    if(healer)controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.025f,1.5f);
                    if(plan.Id=="M33")controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.2f,1.5f);
                    var active=controller.Model.Units.Where(u=>u.SkillRequired>0).OrderByDescending(u=>u.Attack).FirstOrDefault();
                    if(active!=null)
                    {
                        phase="跳过自然回转等待（未改技力）";
                        double limit=controller.Model.Clock+150;
                        while(active.SkillCharge<active.SkillRequired-1&&controller.Model.Clock<limit)
                        {
                            for(int i=0;i<12&&active.SkillCharge<active.SkillRequired-1;i++)Advance(Step);
                            yield return null;
                        }
                        if(controller.Model.Clock>=limit)clip.notes+=" 此夹具中主动技能未能等到就绪，未伪造触发。";
                    }
                    yield return Segment(active==null?"被动 / 联动持续展示":"技能与命中 · 自然技力触发",8,plan.Id=="M33"?2.1f:-1);
                    if(plan.Id=="tag-接棒"||plan.Id=="tag-剧场")
                    {
                        controller.SetShowcaseTargetPressure(controller.Model.Units.Max(u=>u.MaxHealth)*3+controller.Model.Units.Max(u=>u.Armor),.8f);
                        clip.notes+=" 羁绊条件夹具：首靶短暂高攻击，实际死亡/低血后观察接棒或剧场，不修改我方生命。";
                        yield return Segment("羁绊条件夹具 · 真实低血 / 阵亡触发",3,1.8f);
                        controller.SetShowcaseTargetPressure(0);
                        yield return Segment("羁绊返场 / 联动后续",9);
                    }
                }
                EndEncoder();
                clip.duration=clip.frames/(float)manifest.fps;clip.skillCasts=controller.Model.SkillCasts.Count;clip.hits=controller.Model.Hits.Count;
                Save();Debug.Log("FORMATION_SHOWCASE_CLIP_PASS: "+clip.id+";frames="+clip.frames+";casts="+clip.skillCasts+";hits="+clip.hits);
                yield return null;
            }
        }
        IEnumerator RecordPhase(Plan plan)
        {
            yield return Segment("阶段前走路 / 普攻",5);
            var actor=controller.Model.Units.First();
            if(plan.Units[0]=="B08")
            {
                phase="跳过等待：赐火后才有原生返场条件";
                double limit=controller.Model.Clock+150;
                while(controller.Model.SkillCasts.Count==0&&controller.Model.Clock<limit)
                {for(int i=0;i<12&&controller.Model.SkillCasts.Count==0;i++)Advance(Step);yield return null;}
            }
            controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.6f,.8f);
            clip.notes+=" 阶段夹具：首靶提高攻击力，实际受伤/致死触发原生阶段；返场后重置显示起点以核对二阶段走路。";
            yield return Segment("阶段夹具 · 靶攻击触发受伤/返场",3,plan.Units[0]=="B04"?.5f:2.1f);
            controller.SetShowcaseTargetPressure(0);
            yield return Segment("原生阶段 / 返场等待",5);
            if(plan.Units[0]=="B13")
            {
                controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.6f,.8f);
                yield return Segment("再次受击 · 第三阶段返场",3,2.1f);
                controller.SetShowcaseTargetPressure(0);
                yield return Segment("第三阶段演出 / 等待",5);
            }
            actor.Position=new Vector2(12,3);actor.Facing=Vector2.right;
            yield return Segment("阶段后重新接敌 · 检查走路与技能",7);
        }
        void Advance(float seconds)
        {
            var model=controller.Model;var positions=model.Units.ToDictionary(u=>u.Id,u=>u.Position);
            model.Paused=false;model.Advance(seconds);controller.RestoreShowcaseTargetHealth();
            if(clip!=null)foreach(var u in model.Units)if(positions.TryGetValue(u.Id,out var before))clip.movedDistance+=Vector2.Distance(before,u.Position);
            controller.SyncActors();
        }
        IEnumerator Segment(string label,float seconds,float stopPressureAt=-1)
        {
            phase=label;var stage=new Stage{label=label,start=clip.frames/(float)manifest.fps,duration=seconds};clip.stages.Add(stage);
            int count=Mathf.RoundToInt(seconds*manifest.fps);
            for(int frame=0;frame<count;frame++)
            {
                if(stopPressureAt>=0&&frame==Mathf.RoundToInt(stopPressureAt*manifest.fps))controller.SetShowcaseTargetPressure(0);
                // Read production streaming windows at this exact battle time, without
                // advancing while an atlas page is loading. The encoded replay stays 1x.
                double deadline=Time.realtimeSinceStartupAsDouble+180;
                while(!controller.RosterArtReady)
                {if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Animation window stalled for "+clip.id);yield return null;}
                controller.SyncActors();writing=true;yield return new WaitForEndOfFrame();
                var frameTexture=ScreenCapture.CaptureScreenshotAsTexture();
                if(frameTexture==null)throw new InvalidOperationException("GPU screenshot unavailable.");
                try
                {
                    if(frameTexture.width!=manifest.width||frameTexture.height!=manifest.height)throw new InvalidOperationException("Video needs a 1280x720 Player; actual="+frameTexture.width+"x"+frameTexture.height);
                    string pixel=frameTexture.format==TextureFormat.RGB24?"rgb24":frameTexture.format==TextureFormat.RGBA32?"rgba":null;
                    if(pixel==null)throw new InvalidOperationException("Unsupported capture format "+frameTexture.format);
                    if(encoder==null)StartEncoder(pixel);
                    byte[] raw=frameTexture.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(raw,0,raw.Length);
                    if(frame==0)
                    {string png=Path.Combine(directory,Path.GetFileNameWithoutExtension(clip.file)+"-"+(clip.stages.Count-1)+".png");File.WriteAllBytes(png,frameTexture.EncodeToPNG());}
                    clip.frames++;
                }
                finally{Object.Destroy(frameTexture);}
                Advance(Step);
            }
            writing=false;
        }
        void StartEncoder(string pixel)
        {
            encoderError="";
            var info=new ProcessStartInfo{FileName=ffmpeg,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
            info.Arguments="-hide_banner -loglevel error -y -f rawvideo -pixel_format "+pixel+" -video_size 1280x720 -framerate 24 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 21 -pix_fmt yuv420p -movflags +faststart \""+Path.Combine(directory,clip.file)+"\"";
            encoder=new Process{StartInfo=info};encoder.ErrorDataReceived+=(sender,args)=>{if(args.Data!=null)encoderError+=args.Data+"\n";};
            if(!encoder.Start())throw new InvalidOperationException("Could not start FFmpeg.");encoder.BeginErrorReadLine();
        }
        void EndEncoder()
        {
            if(encoder==null)return;
            encoder.StandardInput.Close();
            if(!encoder.WaitForExit(60000)){encoder.Kill();throw new TimeoutException("Video encoder did not finish.");}
            int exit=encoder.ExitCode;encoder.Dispose();encoder=null;
            if(exit!=0)throw new InvalidOperationException("FFmpeg failed: "+encoderError);
        }
        static string SafeId(string id)=>string.Concat(id.Select(c=>char.IsLetterOrDigit(c)||c=='-'?c:'_'));
        static string Notes(Plan plan)
        {
            string result="本作当前实机原速，未修正已知表现；普攻按本作短动作窗口播放。无音轨。";
            if(plan.Units.Length==1&&new[]{"B11","B14","WIS","B21"}.Contains(plan.Units[0]))result+=" 源包没有走路片，当前以待机滑行。";
            if(plan.Kind=="synergy")result+=" 多数羁绊目前只有数值/行为联动，没有独立战场特效；请看实际触发与重叠。";
            if(plan.Id=="tag-整合")result+=" 希望属于战后经济，本片无希望奖励结算。";
            if(plan.Id=="tag-海嗣")result+=" 溟痕当前为静态青色格。";
            if(plan.Id=="M33")result+=" 该角色没有普攻；后段用真实受击触发反击。";
            if(plan.Id=="B10")result+=" 坎诺特普攻为治疗，经济技能不能以单场自然对局证明。";
            return result;
        }
        void Save(){if(directory!=null)File.WriteAllText(Path.Combine(directory,"manifest.json"),JsonUtility.ToJson(manifest,true));}
        void Finish(Exception error)
        {
            writing=false;
            try{EndEncoder();}catch(Exception encodeError){if(error==null)error=encodeError;}
            if(error!=null){manifest.error=error.ToString();if(clip!=null)clip.error=manifest.error;}
            manifest.completed=error==null;Save();done=true;
            Debug.Log(error==null?"FORMATION_SHOWCASE_PASS: "+manifest.clips.Count+" clips":"FORMATION_SHOWCASE_FAIL: "+error);
            Application.Quit(error==null?0:1);
        }
        void OnGUI()
        {
            if(controller==null||!writing||clip==null)return;
            var matrix=GUI.matrix;int depth=GUI.depth;GUI.matrix=TacticalViewport.Current.Matrix;GUI.depth=-1000;
            try
            {
                ApprovedUi.Fill(new Rect(0,0,1280,65),new Color(.035f,.055f,.07f,.97f));
                GUI.Label(new Rect(18,6,590,28),clip.name+"   "+(clip.kind=="synergy"?"羁绊联动":"角色动作"),ApprovedUi.Text(null,22,TextAnchor.MiddleLeft,Color.white,650));
                GUI.Label(new Rect(20,37,760,22),phase,ApprovedUi.Text(null,13,TextAnchor.MiddleLeft,new Color(.5f,.85f,.88f)));
                var actor=controller.Model.Units.FirstOrDefault();
                string stats=actor==null?"":string.Format("技力 {0:0}/{1:0}   施法 {2}   命中 {3}",actor.SkillCharge,actor.SkillRequired,controller.Model.SkillCasts.Count,controller.Model.Hits.Count);
                GUI.Label(new Rect(795,8,465,25),stats,ApprovedUi.Text(null,15,TextAnchor.MiddleRight,Color.white));
                GUI.Label(new Rect(795,36,465,22),string.Join(" · ",clip.activeSynergies??Array.Empty<string>()),ApprovedUi.Text(null,11,TextAnchor.MiddleRight,new Color(.9f,.77f,.45f)));
                ApprovedUi.Fill(new Rect(12,674,1256,37),new Color(.035f,.055f,.07f,.92f));
                GUI.Label(new Rect(22,678,1234,27),"当前实机展示夹具｜一星原数值、无装备、高生命靶｜未修改技力；段间跳过等待｜无音轨",ApprovedUi.Text(null,12,TextAnchor.MiddleLeft,Color.white));
            }
            finally{GUI.matrix=matrix;GUI.depth=depth;}
        }
    }
}
