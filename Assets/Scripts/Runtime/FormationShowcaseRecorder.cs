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
            public string id,kind,name,file,notes,error,audioFile;
            public string[] units,activeSynergies;
            public List<Stage> stages=new List<Stage>();
            public int frames,skillCasts,hits,soundCues,unreadableSounds;
            public float duration,movedDistance;
        }
        [Serializable] public sealed class Manifest
        {
            public string sourceCommit,scope="当前保存棋盘与生产v0.7动作/技能/特效的GPU实录。预置一星编队、右侧起点及高生命靶；片中标注跳过的等待。主角色数值/技力不改，不是自然对局或原作完整还原。";
            public int width=1280,height=720,fps=24;
            public bool audio=false,completed,onePerRole;
            public List<Clip> clips=new List<Clip>();
            public string error;
        }
        sealed class Plan
        {
            internal string Id,Kind,Name;
            internal string[] Units;
            internal int Cost=3,Skill=3;
            internal bool Phase,WalkOnly;
            internal string GuardArt,SummonArt;
            internal bool SnowBoss;
        }
        static FormationShowcaseRecorder current;
        FormationBattleController controller;
        Manifest manifest=new Manifest();
        Process encoder;
        string directory,ffmpeg,phase="准备素材",encoderError="",runtimeError;
        Clip clip;
        bool writing,done;
        const float Step=1f/24;
        const int ReviewAudioRate=44100;
        readonly List<(int sample,FormationBattleController.FormationAudioCue cue)> reviewSounds =
            new List<(int,FormationBattleController.FormationAudioCue)>();
        readonly List<FormationBattleController.FormationAudioCue> audioBuffer = new List<FormationBattleController.FormationAudioCue>();
        readonly Dictionary<AudioClip,float[]> audioSamples = new Dictionary<AudioClip,float[]>();
        readonly HashSet<int> reviewActionIds = new HashSet<int>();
        readonly HashSet<FormationHit> reviewHits = new HashSet<FormationHit>();
        double reviewAudioThrough;
        bool RecordAudio=>Array.IndexOf(Environment.GetCommandLineArgs(),"-showcaseSound")>=0;
        bool RoleVideos=>Array.IndexOf(Environment.GetCommandLineArgs(),"-showcaseRoleVideos")>=0;
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
            if(RoleVideos)
            {
                foreach(int number in Enumerable.Range(1,12).Concat(Enumerable.Range(21,10)))
                {
                    string id="D"+number.ToString("D2");
                    var guard=FormationCampaignRules.DefenderByArtId(id);
                    yield return new Plan{Id=id,GuardArt=id,Kind="defender",Name=guard.Name,Units=new[]{"B10"}};
                }
                yield return new Plan{Id="P14H",SummonArt="P14H",Kind="summon",Name="卡德霍猎犬",Units=new[]{"B14"}};
                yield return new Plan{Id="GOLEM",SummonArt="GOLEM",Kind="summon",Name="泥岩巨像",Units=new[]{"M27","M42"}};
                yield break;
            }
            foreach(int cost in new[]{4,5})yield return new Plan{Id="TAL"+cost,Kind="variant",Name="塔露拉 · "+cost+"费",Units=new[]{"TAL"},Cost=cost};
            foreach(int skill in new[]{1,2})yield return new Plan{Id="WIS"+skill,Kind="variant",Name="维维美 · 技能"+skill,Units=new[]{"WIS"},Skill=skill};
            foreach(string id in new[]{"M20","M32","M14","M30","B04","B08","B13","B24"})
                yield return new Plan{Id=id+"-phase",Kind="variant",Name=FormationCampaignRules.Find(id).Name+" · 阶段/返场",Units=new[]{id},Phase=true};
            foreach(var tag in FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles))
            {
                string[] units=FormationCampaignRules.AllUnits.Where(u=>u.Factions.Contains(tag.Id)||u.Styles.Contains(tag.Id)).OrderBy(u=>u.Cost).Take(10).Select(u=>u.Id).ToArray();
                if(units.Length>0)yield return new Plan{Id="tag-"+tag.Id,Kind="synergy",Name=tag.Name,Units=units};
            }
            foreach(var unit in FormationCampaignRules.AllUnits)
                yield return new Plan{Id=unit.Id+"-walk",Kind="variant",Name=unit.Name+" · 步态素材",Units=new[]{unit.Id},WalkOnly=true};
        }
        IEnumerator Record()
        {
            directory=Path.GetFullPath(Argument("-showcaseDir","artifacts/formation-showcase"));Directory.CreateDirectory(Path.Combine(directory,"clips"));
            ffmpeg=Argument("-showcaseFFmpeg","ffmpeg");
            string manifestPath=Path.Combine(directory,"manifest.json");
            string source=Argument("-showcaseSource","unspecified");
            if(Argument("-showcaseResume","0")=="1"&&File.Exists(manifestPath))
            {
                manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                if(manifest.sourceCommit!=source||manifest.audio!=RecordAudio||manifest.onePerRole!=RoleVideos)
                    throw new InvalidOperationException("Existing review belongs to different content, audio or playlist settings.");
            }
            manifest.sourceCommit=source;manifest.completed=false;manifest.error=null;
            manifest.audio=RecordAudio;manifest.onePerRole=RoleVideos;
            if(RoleVideos)manifest.scope="同版 Windows Player 正式棋盘的逐角色 GPU 实录。使用对照靶与合法预置阵容，段间可跳过等待。守军在本作固守，缺少的动作会标明。音轨来自战斗音效事件与实际采用的音频资源。";
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while((controller=Object.FindAnyObjectByType<FormationBattleController>())==null||!controller.Ready)
            {if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Saved formation scene did not become ready.");yield return null;}
            string only=Argument("-showcaseOnly","");var selected=new HashSet<string>(only.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries));
            var plans=Plans().ToArray();int order=0;
            foreach(var plan in plans)
            {
                order++;if(selected.Count>0&&!selected.Contains(plan.Id))continue;
                var existing=manifest.clips.FirstOrDefault(c=>c.id==plan.Id);
                if(existing!=null&&string.IsNullOrEmpty(existing.error)&&existing.frames>0&&existing.duration>0&&File.Exists(Path.Combine(directory,existing.file))&&
                    (!RecordAudio||!string.IsNullOrEmpty(existing.audioFile)&&File.Exists(Path.Combine(directory,existing.audioFile))))continue;
                if(existing!=null)manifest.clips.Remove(existing);
                clip=new Clip{id=plan.Id,kind=plan.Kind,name=plan.Name,units=plan.Units,file="clips/"+order.ToString("D2")+"-"+SafeId(plan.Id)+".mp4"};
                manifest.clips.Add(clip);writing=false;phase="准备素材";Save();
                reviewSounds.Clear();
                Debug.Log("FORMATION_SHOWCASE_START: "+order+"/"+plans.Length+" "+plan.Id);
                clip.notes=Notes(plan)+(plan.GuardArt==null?" "+controller.ShowcaseFixtureNote:"");
                foreach(var part in ReviewParts(plan))
                {
                    reviewActionIds.Clear();reviewHits.Clear();
                    yield return Prepare(part);
                    yield return Capture(part);
                    if(!RoleVideos)
                    {
                        clip.skillCasts=controller.Model.SkillCasts.Count;
                        clip.hits=controller.Model.Hits.Count;
                    }
                }
                EndEncoder();
                if(RecordAudio)WriteReviewSound();
                clip.duration=clip.frames/(float)manifest.fps;
                Save();Debug.Log("FORMATION_SHOWCASE_CLIP_PASS: "+clip.id+";frames="+clip.frames+";casts="+clip.skillCasts+";hits="+clip.hits+";cues="+clip.soundCues);
                yield return null;
            }
        }

        IEnumerator AdvanceUntil(Func<bool> reached,float seconds)
        {
            double limit=controller.Model.Clock+seconds;
            while(!reached()&&controller.Model.Clock<limit&&controller.Model.Phase==FormationPhase.Fighting)
            {
                for(int i=0;i<12&&!reached()&&controller.Model.Clock<limit&&controller.Model.Phase==FormationPhase.Fighting;i++)Advance(Step);
                yield return null;
            }
        }

        IEnumerator RecordGuard(Plan plan)
        {
            var model=controller.Model;
            var guard=model.Defenders[0];
            string id=plan.GuardArt;
            if(plan.SnowBoss)
            {
                yield return Segment("终局形态 · 霜涛覆岭与接触",10);
                model.QueueShowcaseSnowBossTransition();
                yield return Segment("实际受击触发转阶段 · 冻结与群山俯首",12);
                if(guard.SnowBossPhase!=2)clip.notes+=" 本段未观察到终局二阶段。";
                yield break;
            }
            clip.notes+=" 守军在现役规则中固定站位，源目录没有独立行走片段。靶只供触发动作、伤害或治疗。";
            yield return Segment("固守站位 · 本作守军不行走",1.5f,freezeSimulation:true);
            if(id=="D30")
            {
                clip.notes+=" 玛恩纳保留在守军资源库，现役关卡不配置且没有普通攻击；后两段为源技能动作的实机检视。";
                foreach(string key in new[]{"skill_begin","skill_attack"})
                {
                    float seconds=controller.ShowcaseGuardMotionDuration(guard,key);
                    if(seconds<=0)continue;
                    model.PreviewShowcaseGuardMotion(key,seconds);
                    yield return Segment(key=="skill_begin"?"源技能起手 · 动作检视":"源技能挥击 · 动作检视",Mathf.Max(1.25f,seconds/FormationBattleController.FormationVisualRate+.3f));
                }
                yield break;
            }
            string basic=id=="D22"?"入距即展开火墙 · 墙体与普通攻击":id=="D28"?"执棋开场 · 地面棋子触发":
                id=="D27"?"常规守军形态 · 无独立普通攻击":guard.Healer?"普通治疗":"普通攻击与接触";
            yield return Segment(basic,id=="D22"?8:4);
            double previous=guard.LastSkillAt;
            bool hasImmediateSkill=id=="D22"||id=="D28";
            if(!hasImmediateSkill)
            {
                phase="跳过部分自然等待";
                yield return AdvanceUntil(()=>guard.LastSkillAt>previous,30);
            }
            string action=id=="D23"?"六次真实治疗后转形进攻":id=="D28"?"棋子触发后续 · 区域受击":
                id=="D22"?"火墙后的普攻与收势":guard.Healer?"技能治疗及反馈":"技能、持续层与接触";
            yield return Segment(action,id=="D23"?12:id=="D21"||id=="D24"||id=="D27"?10:6);
            if(guard.LastSkillAt<0)clip.notes+=" 预置对局未观察到技能触发。";
        }

        IEnumerator RecordSummon(Plan plan)
        {
            var model=controller.Model;
            FormationFighter summon=null;
            yield return AdvanceUntil(()=> (summon=model.Summons.FirstOrDefault(u=>u.ArtId==plan.SummonArt))!=null,40);
            if(summon==null)throw new InvalidOperationException("Production summon did not appear for "+plan.Id);
            clip.notes+=" 由生产技能或羁绊生成，使用实际移动、索敌和普攻。当前召唤物没有独立主动技能。";
            if(plan.Id=="GOLEM")
            {
                summon.Position=new Vector2(16,3.2f);
                clip.notes+=" 巨像从右侧展示起点实际接敌。";
            }
            yield return Segment("真实召唤后 · 行走、接敌与普攻",plan.Id=="GOLEM"?10:9);
        }

        IEnumerable<Plan> ReviewParts(Plan plan)
        {
            yield return plan;
            if(!RoleVideos)yield break;
            if(plan.Id=="TAL")
                foreach(int cost in new[]{4,5})yield return new Plan{Id="TAL"+cost,Kind="variant",Name=plan.Name,Units=plan.Units,Cost=cost};
            if(plan.Id=="WIS")
                foreach(int skill in new[]{1,2})yield return new Plan{Id="WIS"+skill,Kind="variant",Name=plan.Name,Units=plan.Units,Skill=skill};
            if(new[]{"M20","M32","M14","M30","B04","B08","B13","B24"}.Contains(plan.Id))
                yield return new Plan{Id=plan.Id+"-phase",Kind="variant",Name=plan.Name,Units=plan.Units,Phase=true};
            if(plan.Id=="D27")
                yield return new Plan{Id="D27-boss",GuardArt="D27",Kind="defender",Name=plan.Name,Units=new[]{"M11"},SnowBoss=true};
        }

        IEnumerator Prepare(Plan plan)
        {
            yield return controller.PrepareShowcaseRoleFixture(plan.Units,plan.Cost,plan.Skill,plan.GuardArt,plan.SnowBoss,RoleVideos);
            controller.BeginShowcaseFixture();
            controller.Model.Paused=false;
            clip.activeSynergies=controller.Model.ActiveSynergies.Where(p=>p.Value>0).Select(p=>p.Key+" "+p.Value).ToArray();
        }

        IEnumerator Capture(Plan plan)
        {
                if(plan.GuardArt!=null) { yield return RecordGuard(plan); yield break; }
                if(plan.SummonArt!=null) { yield return RecordSummon(plan); yield break; }
                if(plan.WalkOnly)
                {
                    clip.notes+=" 步态素材单独演示：程序按角色速度往返位移，生产Renderer读取原走路动画；不是自然AI接敌。无walk源片的角色仍显示待机。";
                    controller.Model.AdvanceShowcaseWalk(0);
                    yield return Segment("步态素材演示 · 程序往返位移，非自然AI",6,-1,true);
                }
                else if(plan.Phase)yield return RecordPhase(plan);
                else
                {
                    bool supportingVariant=RoleVideos&&plan.Kind=="variant";
                    if(!supportingVariant)yield return Segment("走路与接敌 · 当前棋盘原比例",5);
                    var actor=controller.Model.Units.First();
                    bool healer=plan.Units.Any(id=>FormationCampaignRules.Find(id).Effect.Contains("治疗")||FormationCampaignRules.Find(id).Effect.Contains("回复生命"));
                    if(healer)controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.025f,1.5f);
                    if(!supportingVariant)
                    {
                        if(RoleVideos&&plan.Id!="M33"&&plan.Id!="B21")
                            yield return AdvanceUntil(()=>controller.Model.PresentationActions.Any(action=>action.ActorId==actor.Id&&!action.Skill),25);
                        yield return Segment(plan.Id=="M33"||plan.Id=="B21"?"固有表现 / 本作无普通攻击":healer&&
                            FormationCampaignRules.Find(plan.Units[0]).DamageType=="治疗"?"普通治疗":"普攻 / 常驻被动",plan.Id=="WIS"?6:3);
                    }
                    if(plan.Id=="M33")
                    {
                        // The crawler is slow and has no active skill. Wait until its
                        // real movement puts a target inside the retaliation radius.
                        phase="跳过慢速接敌等待，随后真实受击反击";
                        double approachLimit=controller.Model.Clock+150;
                        while(controller.Model.Defenders.Min(d=>Vector2.Distance(d.Position,actor.Position))>1.8f&&controller.Model.Clock<approachLimit)
                        {for(int i=0;i<12;i++)Advance(Step);yield return null;}
                        controller.SetShowcaseTargetPressure(actor.Armor+actor.MaxHealth*.2f,1.5f);
                    }
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
                    string variant=plan.Cost!=3?"塔露拉 "+plan.Cost+"费 · ":plan.Skill!=3?"维维美技能 "+plan.Skill+" · ":"";
                    if(plan.Id=="TAL5")
                        yield return Segment(variant+"火雨起手、持续、结束与恢复普攻",14);
                    else if(plan.Id=="B21")
                    {
                        controller.SetShowcaseTargetPressureInRange(actor.Armor+actor.MaxHealth*.025f,.85f,.85f);
                        clip.notes+=" 首靶使用 .85 格普通攻击，检验落爪后对岁本体的实际伤害。";
                        yield return Segment("落爪、撼山河与守军反击 · 自然技力触发",16.5f);
                    }
                    else
                        yield return Segment(variant+(active==null?"被动 / 联动持续展示":"技能与命中 · 自然技力触发"),8,plan.Id=="M33"?2.1f:-1);
                    if(plan.Id=="tag-接棒"||plan.Id=="tag-剧场")
                    {
                        controller.SetShowcaseTargetPressure(controller.Model.Units.Max(u=>u.MaxHealth)*3+controller.Model.Units.Max(u=>u.Armor),.8f);
                        clip.notes+=" 羁绊条件夹具：首靶短暂高攻击，实际死亡/低血后观察接棒或剧场，不修改我方生命。";
                        yield return Segment("羁绊条件夹具 · 真实低血 / 阵亡触发",3,1.8f);
                        controller.SetShowcaseTargetPressure(0);
                        yield return Segment("羁绊返场 / 联动后续",9);
                    }
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
        IEnumerator Segment(string label,float seconds,float stopPressureAt=-1,bool walkPreview=false,bool freezeSimulation=false)
        {
            phase=label;var stage=new Stage{label=label,start=clip.frames/(float)manifest.fps,duration=seconds};clip.stages.Add(stage);
            reviewAudioThrough=controller.Model.Clock-.00001;
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
                if(!walkPreview&&!freezeSimulation)
                {
                    if(RecordAudio)CaptureReviewSound();
                    if(RoleVideos)ObserveCapturedActions();
                }
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
                if(walkPreview){controller.Model.AdvanceShowcaseWalk(Step);controller.SyncActors();}
                else if(!freezeSimulation)Advance(Step);
            }
            writing=false;
        }
        void CaptureReviewSound()
        {
            double at=controller.Model.Clock;
            audioBuffer.Clear();
            controller.CollectFormationAudio(reviewAudioThrough,at,audioBuffer,includeSnowBoss:true);
            foreach(var cue in audioBuffer)
            {
                int sample=(int)Math.Round((clip.frames/(double)manifest.fps+cue.At-at)*ReviewAudioRate);
                reviewSounds.Add((Math.Max(0,sample),cue));
            }
            reviewAudioThrough=at;
        }

        void ObserveCapturedActions()
        {
            var model=controller.Model;
            int[] ids=clip.kind=="defender"?new[]{model.Defenders[0].Id}:clip.kind=="summon"?
                model.Summons.Where(actor=>actor.ArtId==clip.id).Select(actor=>actor.Id).ToArray():
                model.Units.Where(actor=>actor.Enabled).Select(actor=>actor.Id).ToArray();
            foreach(var action in model.PresentationActions)
                if(action.Skill&&!action.ProjectileOnly&&ids.Contains(action.ActorId)&&reviewActionIds.Add(action.Sequence))clip.skillCasts++;
            foreach(var hit in model.Hits)
                if(ids.Contains(hit.FromId)&&reviewHits.Add(hit))clip.hits++;
        }

        void WriteReviewSound()
        {
            int count=Mathf.RoundToInt(clip.frames*ReviewAudioRate/(float)manifest.fps);
            var mixed=new float[count];
            for(int eventIndex=0;eventIndex<reviewSounds.Count;eventIndex++)
            {
                var sound=reviewSounds[eventIndex];
                var cue=sound.cue;
                if(cue.Clip==null||cue.Pitch<=0)continue;
                if(!audioSamples.TryGetValue(cue.Clip,out var samples))
                {
                    samples=null;
                    if(cue.Clip.loadState==AudioDataLoadState.Loaded&&cue.Clip.loadType==AudioClipLoadType.DecompressOnLoad)
                    {
                        var channels=new float[cue.Clip.samples*cue.Clip.channels];
                        if(cue.Clip.GetData(channels,0))
                        {
                            samples=new float[cue.Clip.samples];
                            for(int sample=0;sample<samples.Length;sample++)
                                for(int channel=0;channel<cue.Clip.channels;channel++)samples[sample]+=channels[sample*cue.Clip.channels+channel]/cue.Clip.channels;
                        }
                    }
                    audioSamples.Add(cue.Clip,samples);
                }
                if(samples==null){clip.unreadableSounds++;continue;}
                double stride=cue.Clip.frequency*cue.Pitch/ReviewAudioRate;
                int end=count;
                if(cue.ReplacesEarlier)
                    for(int next=eventIndex+1;next<reviewSounds.Count;next++)
                        if(reviewSounds[next].cue.Clip==cue.Clip){end=Math.Min(end,reviewSounds[next].sample);break;}
                for(int i=0;i+sound.sample<end;i++)
                {
                    double position=i*stride;
                    int index=(int)position;
                    if(index>=samples.Length)break;
                    mixed[i+sound.sample]+=Mathf.Lerp(samples[index],samples[Math.Min(index+1,samples.Length-1)],(float)(position-index))*cue.Gain;
                }
                clip.soundCues++;
            }
            clip.audioFile="audio/"+Path.GetFileNameWithoutExtension(clip.file)+".wav";
            string path=Path.Combine(directory,clip.audioFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var file=new BinaryWriter(File.Create(path)))
            {
                file.Write(new[]{(byte)'R',(byte)'I',(byte)'F',(byte)'F'});file.Write(36+count*2);
                file.Write(new[]{(byte)'W',(byte)'A',(byte)'V',(byte)'E',(byte)'f',(byte)'m',(byte)'t',(byte)' '});
                file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(ReviewAudioRate);file.Write(ReviewAudioRate*2);
                file.Write((short)2);file.Write((short)16);
                file.Write(new[]{(byte)'d',(byte)'a',(byte)'t',(byte)'a'});file.Write(count*2);
                foreach(float value in mixed)file.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));
            }
            if(clip.unreadableSounds>0)clip.notes+=" 部分音频资源无法读出，未补造替代声。";
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
            string result="基础动画2×，攻速正常影响攻击动作，战斗规则1×。";
            if(plan.Units.Length==1&&plan.Units[0]=="WIS")result+=" 移动使用已认可的19帧走路；施放使用愚人节皮肤。";
            if(plan.Units.Length==1&&plan.Units[0]=="B14")result+=" 正式战斗以真实瞬移接敌；步态独立栏目不作为本体接敌凭证。";
            if(plan.Id=="B11"||plan.Id=="B21")result+=" 原身体资源没有独立步态片段。";
            if(plan.Kind=="synergy")result+=" 羁绊展示真实数值/行为；同频、接棒、提词、迅攻、复苏复用原作公共反馈，并非原作专属羁绊特效。";
            if(plan.Id=="tag-整合")result+=" 希望属于战后经济，本片无希望奖励结算。";
            if(plan.Id=="tag-海嗣")result+=" 溟痕使用原有动态原生材质，准备期选格与战斗地面层分开。";
            if(plan.Id=="M33")result+=" 该角色没有普攻；后段用真实受击触发反击。";
            if(plan.Id=="B10")result+=" 坎诺特普攻为治疗，经济技能不能以单场自然对局证明。";
            if(plan.Id=="B26")result+=" 易的梁由实际技能按附属视觉显示。";
            if(plan.Id=="D28")result+=" 棋子是望技能的场上附属物，放在本视频内。";
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
                ApprovedUi.Fill(new Rect(0,0,1280,65),new Color(.035f,.055f,.07f,1));
                GUI.Label(new Rect(18,6,590,28),clip.name+"   "+(clip.kind=="synergy"?"羁绊联动":"角色动作"),ApprovedUi.Text(null,22,TextAnchor.MiddleLeft,Color.white,650));
                GUI.Label(new Rect(20,37,760,22),phase,ApprovedUi.Text(null,13,TextAnchor.MiddleLeft,new Color(.5f,.85f,.88f)));
                FormationActor actor=clip.kind=="defender"?controller.Model.Defenders.FirstOrDefault():
                    clip.kind=="summon"?controller.Model.Summons.FirstOrDefault(u=>u.ArtId==clip.id):controller.Model.Units.FirstOrDefault();
                string stats=actor==null?"":string.Format("技力 {0:0}/{1:0}   施法 {2}   接触 {3}",actor.SkillCharge,actor.SkillRequired,
                    RoleVideos?clip.skillCasts:controller.Model.SkillCasts.Count,RoleVideos?clip.hits:controller.Model.Hits.Count);
                GUI.Label(new Rect(795,8,465,25),stats,ApprovedUi.Text(null,15,TextAnchor.MiddleRight,Color.white));
                GUI.Label(new Rect(795,36,465,22),string.Join(" · ",clip.activeSynergies??Array.Empty<string>()),ApprovedUi.Text(null,11,TextAnchor.MiddleRight,new Color(.9f,.77f,.45f)));
                ApprovedUi.Fill(new Rect(12,674,1256,37),new Color(.035f,.055f,.07f,.92f));
                GUI.Label(new Rect(22,678,1234,27),"正式棋盘实机 · 预置阵容与展示靶 · 段间可跳过等待 · "+
                    (RecordAudio?"战斗音效事件录音":"无音轨"),ApprovedUi.Text(null,12,TextAnchor.MiddleLeft,Color.white));
            }
            finally{GUI.matrix=matrix;GUI.depth=depth;}
        }
    }
}
