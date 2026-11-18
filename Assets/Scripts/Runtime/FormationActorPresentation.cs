using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        readonly Dictionary<string,FormationUnitArt> formationMonsterArt=new Dictionary<string,FormationUnitArt>();
        readonly Dictionary<int,float> formationLabelHeights=new Dictionary<int,float>();
        readonly HashSet<string> warmedFormationArt=new HashSet<string>();
        readonly HashSet<string> warmingFormationArt=new HashSet<string>();
        readonly HashSet<string> warmingFormationWindows=new HashSet<string>();
        sealed class ShopIdleUse {internal FormationUnitArt Source;internal double AnimationTime,UsedAt;}
        readonly Dictionary<string,ShopIdleUse> formationShopIdleUses=new Dictionary<string,ShopIdleUse>();
        readonly Dictionary<string,Sprite> formationShopLastIdle=new Dictionary<string,Sprite>();
        public bool RosterArtReady=>Model!=null&&RequiredFormationArtIds().All(FormationArtAvailable);

        bool FormationArtAvailable(string id)
        {
            if(!warmedFormationArt.Contains(id))return false;
            if(!id.StartsWith("D",StringComparison.Ordinal))return formationMonsterArt.TryGetValue(id,out var source)&&source.IsReady&&FormationInitialMotionKeys(source,id).All(key=>source.WindowReady(key,0,false))&&FormationEffectSequences(source).All(c=>c.Ready);
            return art.TryGetValue(id,out var defender)&&defender.Clips.Values.Where(c=>c.Key=="idle"||c.Key=="walk"||c.Key.StartsWith("attack",StringComparison.Ordinal)||c.Key=="death"||c.Key=="heal").All(c=>c.Ready);
        }

        IEnumerable<string> RequiredFormationArtIds()
        {
            if(Model==null)yield break;
            foreach(var id in Model.Units.Where(u=>u.Enabled).Select(u=>u.ArtId).Concat(Model.Defenders.Select(d=>d.ArtId)).Distinct())yield return id;
            foreach(var id in Model.Summons.Select(u=>u.ArtId).Distinct())yield return id;
            var team=Model.Units.Where(u=>u.Enabled).Select(u=>FormationCampaignRules.Find(u.ArtId)).Where(s=>s!=null).ToArray();
            if(team.Any(s=>s.Id=="B14"))yield return "P14H";
            if(team.Any(s=>s.Id=="B26"))yield return "P26";
            if(team.Count(s=>s.Factions.Contains("泥岩"))>=2)yield return "GOLEM";
        }
        void EnsureFormationArt()
        {
            var required=RequiredFormationArtIds().ToList();
            var dragged=Shop?.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);
            if(pointerHeld&&dragged!=null)required.Add(FormationUnitCatalog.Find(dragged.Id).ArtId);
            pinned.Clear();
            foreach(string id in required.Distinct())
            {
                string root=id.StartsWith("D",StringComparison.Ordinal)?"DefenderArt/"+id:FormationUnitArt.For(id)?.Root;
                if(!string.IsNullOrEmpty(root))pinned.Add(root);
                if(!id.StartsWith("D",StringComparison.Ordinal))PinFormationEffects(FormationUnitArt.For(id));
            }
            foreach(var request in formationShopIdleUses.ToArray())
            {
                if(Time.unscaledTimeAsDouble-request.Value.UsedAt>1){formationShopIdleUses.Remove(request.Key);continue;}
                foreach(string owner in request.Value.Source.WindowOwners("idle",request.Value.AnimationTime,true))
                    if(!pinned.Contains(owner))pinned.Add(owner);
            }
            ArtMemoryCache.Pin(pinned);
            foreach(string id in required.Distinct())
                if(!FormationArtAvailable(id)&&!warmingFormationArt.Contains(id))StartCoroutine(WarmFormationArt(id));
        }
        IEnumerator WarmFormationArt(string id)
        {
            if(FormationArtAvailable(id)||!warmingFormationArt.Add(id))yield break;
            loading="准备原有动作  "+id;
            if(id.StartsWith("D",StringComparison.Ordinal))
            {
                string root="DefenderArt/"+id;var source=ArtCatalog.Read(root);art[id]=source;
                if(!pinned.Contains(root))pinned.Add(root);ArtMemoryCache.Pin(pinned);
                foreach(var clip in source.Clips.Values)
                    if(clip.Key=="idle"||clip.Key=="walk"||clip.Key.StartsWith("attack",StringComparison.Ordinal)||clip.Key=="death"||clip.Key=="heal")yield return clip.Warm();
            }
            else
            {
                var source=FormationUnitArt.For(id);
                if(source==null)throw new InvalidOperationException("Missing formation art: "+id);
                formationMonsterArt[id]=source;
                if(!string.IsNullOrEmpty(source.Root)&&!pinned.Contains(source.Root))pinned.Add(source.Root);
                PinFormationEffects(source);
                ArtMemoryCache.Pin(pinned);yield return source.Warm();
                foreach(string key in FormationInitialMotionKeys(source,id))yield return source.WarmWindow(key,0,false);
                foreach(var clip in FormationEffectSequences(source))if(!clip.Ready)yield return clip.Warm();
            }
            warmedFormationArt.Add(id);warmingFormationArt.Remove(id);
        }
        void PinFormationEffects(FormationUnitArt source)
        {
            if(source==null)return;
            foreach(var sequence in FormationEffectSequences(source))
                if(!pinned.Contains(sequence.CacheKey))pinned.Add(sequence.CacheKey);
        }
        public Sprite FormationShopIdleFrame(string id,double time)
        {
            var source=FormationUnitArt.For(id);if(source==null||!source.HasAction("idle"))return null;
            if(double.IsNaN(time)||double.IsInfinity(time)||time<0)time=0;
            time*=FormationVisualRate;
            double normalized=time/source.Duration("idle");normalized=(normalized-Math.Floor(normalized))*source.Duration("idle");
            string useKey=id+"/"+Math.Floor(normalized*2);
            formationShopIdleUses[useKey]=new ShopIdleUse{Source=source,AnimationTime=time,UsedAt=Time.unscaledTimeAsDouble};
            if(!source.WindowReady("idle",time,true))
            {
                string request="shop/"+id+"/idle";
                if(warmingFormationWindows.Add(request))StartCoroutine(WarmFormationWindow(source,"idle",time,true,request));
            }
            var frame=source.Frame("idle",time,true);
            if(frame!=null&&frame.texture!=null){formationShopLastIdle[id]=frame;return frame;}
            if(formationShopLastIdle.TryGetValue(id,out var previous)&&previous!=null&&previous.texture!=null)return previous;
            formationShopLastIdle.Remove(id);return null;
        }
        public float ActorPresentationHeight(FormationActor actor)
        {
            if(actor is FormationFighter)return FormationUnitCatalog.Find(actor.ArtId)?.VisualHeight??1.12f;
            return art.TryGetValue(actor.ArtId,out var source)&&source.Height>0?source.Height:1.12f;
        }

        Rect FormationActorScreenRect(FormationFighter actor)
        {
            if(bodies.TryGetValue(actor.Id,out var body)&&body!=null&&body.sprite!=null)
            {
                var bounds=body.sprite.bounds;float left=float.MaxValue,right=float.MinValue,top=float.MaxValue,bottom=float.MinValue;
                foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})
                {
                    var world=body.transform.TransformPoint(new Vector3(body.flipX?-x:x,y,0));
                    var point=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(world));
                    left=Mathf.Min(left,point.x);right=Mathf.Max(right,point.x);top=Mathf.Min(top,point.y);bottom=Mathf.Max(bottom,point.y);
                }
                return Rect.MinMaxRect(left-5,top-5,right+5,bottom+6);
            }
            var foot=CellScreen(actor.SpawnCenter);return new Rect(foot.x-28,foot.y-75,56,82);
        }
        void PresentFormationActor(FormationActor actor,bool defender)
        {
            if(!bodies.TryGetValue(actor.Id,out var body))
            {
                body=new GameObject(actor.Name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                body.transform.SetParent(actorRoot,false);body.sharedMaterial=actorMaterial;bodies[actor.Id]=body;
                ActorGrounding.Attach(body,ActorPresentationHeight(actor));
            }
            if(PresentFormationSuiBody(actor,body))return;
            double now=Model.Clock,deathAge=now-actor.LastDeathAt;
            bool returning=actor is FormationFighter returningFighter&&returningFighter.NativePhaseReturningUntil>now;
            float deathDuration=(defender?art.TryGetValue(actor.ArtId,out var da)&&da.Clips.TryGetValue("death",out var dd)?dd.Duration:.65f:formationMonsterArt.TryGetValue(actor.ArtId,out var deathSource)?deathSource.Duration("death"):.65f)/FormationVisualRate;
            body.enabled=FormationActorVisibleInView(actor)&&FormationArtAvailable(actor.ArtId)&&(actor.Alive||returning||deathAge>=0&&deathAge<deathDuration);
            if(!body.enabled)return;
            double visualStarted=actor.PresentationDuration>0?actor.PresentationStartedAt:actor.LastActionAt;
            double visualDuration=actor.PresentationDuration>0?actor.PresentationDuration:Math.Max(0,actor.ActionUntil-actor.LastActionAt);
            bool acting=actor.Alive&&now<visualStarted+visualDuration&&actor.LastActionAt>=0;
            if(actor.Alive&&actor.ArtId=="B21"&&formationMonsterArt.TryGetValue(actor.ArtId,out var channelSource)&&
                FormationChannel(channelSource,actor,out _,out _,out var channelEnd))
            {
                double busy=Model.PresentationBusyUntil(actor);
                if(busy>0&&now<busy+channelSource.Duration(channelEnd)/(actor.PresentationRate>0?actor.PresentationRate:FormationVisualRate))acting=true;
            }
            bool casting=acting&&actor.LastSkillAt>=actor.LastActionAt-.0001;
            string key=!actor.Alive?"death":casting&&actor.ArtId=="B21"?"cast":actor.IsMoving?"walk":casting?"cast":acting?"attack":"idle";
            float actionRate=actor.PresentationDuration>0&&actor.PresentationRate>0?actor.PresentationRate:FormationVisualRate;
            double age=key=="death"?deathAge*FormationVisualRate:key=="attack"||key=="cast"?(now-visualStarted)*actionRate:
                (Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now)*FormationVisualRate;
            float sourceHeight,labelTop=0,actionScale=1;
            if(!defender)
            {
                var source=formationMonsterArt[actor.ArtId];sourceHeight=source.Height;
                bool loop=key!="attack"&&key!="cast"&&key!="death";
                if(source.NativeTask!=null)
                {
                    SelectNativeFormationMotion(source,actor,now,returning,ref key,ref age,ref loop);
                    if(source.NativeTask=="B14"&&actor.Alive&&!acting)
                        SelectFormationCadejoMotion(actor,source,now,ref key,ref age,ref loop);
                    string request=actor.ArtId+"/"+key;
                    if(!source.WindowReady(key,age,loop)&&warmingFormationWindows.Add(request))
                        StartCoroutine(WarmFormationWindow(source,key,age,loop,request));
                    var nativeFrame=source.Frame(key,age,loop);
                    // A newly entered phase can request an uncached page. Retain the last real
                    // source frame while the same production cache prepares that window.
                    if(nativeFrame!=null)body.sprite=nativeFrame;
                    actionScale=source.DisplayScaleFor(key);
                }
                else
                {
                bool sourceTimed=false;
                if(actor is FormationFighter phaseFighter&&actor.ArtId=="M30")
                {
                    if(returning)
                    {
                        double tail=phaseFighter.NativePhaseReturningUntil-now;
                        key=tail<source.Duration("extra_revive")/FormationVisualRate?"extra_revive":"extra_idle_2";
                        age=tail<source.Duration("extra_revive")/FormationVisualRate?source.Duration("extra_revive")-tail*FormationVisualRate:(now-phaseFighter.NativePhaseChangedAt)*FormationVisualRate;
                        sourceTimed=true;
                    }
                    else if(phaseFighter.NativePhase>0)
                        key=key=="idle"?"extra_idle_2":key=="walk"?"extra_move_2":key=="death"?"extra_die_2":key;
                }
                if(casting&&actor.ArtId=="M31"&&source.HasAction("extra_skill_front_start"))
                {
                    double elapsed=(now-visualStarted)*actionRate,remaining=(visualStarted+visualDuration-now)*actionRate;
                    double begin=source.Duration("extra_skill_front_start"),end=source.Duration("extra_skill_front_end");
                    key=elapsed<begin?"extra_skill_front_start":remaining<end?"extra_skill_front_end":"extra_skill_front_loop";
                    age=elapsed<begin?elapsed:remaining<end?end-remaining:elapsed-begin;sourceTimed=true;
                }
                if(!source.HasAction(key))
                {
                    if(key=="death"){body.enabled=false;return;}
                    key=casting&&source.HasAction("skill")?"skill":source.HasOrdinaryAttack?"attack":"idle";
                }
                body.sprite=source.Frame(key,age,key!="attack"&&key!="cast"&&key!="skill"&&key!="death"&&key!="extra_die_2"&&key!="extra_revive"&&key!="extra_skill_front_start"&&key!="extra_skill_front_end");
                }
                labelTop=source.PresentationTop(key);
            }
            else
            {
                var source=art[actor.ArtId];sourceHeight=source.Height;labelTop=source.Height;
                if(key=="cast")key=source.Clips.ContainsKey("skill")?"skill":source.Clips.ContainsKey("attack")?"attack":"idle";
                if(!source.Clips.TryGetValue(key,out var clip))clip=source.Clips["idle"];
                if(key=="attack")
                {
                    source.Clips.TryGetValue("attack_start",out var begin);source.Clips.TryGetValue("attack_end",out var end);
                    double at=age;
                    if(begin!=null&&at<begin.Duration){clip=begin;age=at;}
                    else if(at<(begin?.Duration??0)+clip.Duration)age=at-(begin?.Duration??0);
                    else {age=at-(begin?.Duration??0)-clip.Duration;clip=end??clip;}
                }
                int frame=(int)(Math.Max(0,age)*clip.Fps);
                frame=key=="death"||key=="attack"||key=="skill"?Mathf.Clamp(frame,0,clip.Count-1):frame%clip.Count;body.sprite=clip.Frames[frame];
            }
            body.enabled=body.sprite!=null;if(!body.enabled)return;
            body.flipX=actor.Facing.x<-.01f;body.transform.position=FormationDisplayPoint(actor);body.transform.rotation=battleCamera.transform.rotation;
            float scale=ActorPresentationHeight(actor)/Mathf.Max(.5f,sourceHeight>0?sourceHeight:1.4f);
            body.transform.localScale=Vector3.one*(scale*actionScale);
            formationLabelHeights[actor.Id]=(labelTop>0?labelTop:sourceHeight)*scale;
            body.sortingOrder=5000-Mathf.RoundToInt(actor.Position.y*50)+(actor is FormationFighter rider&&Model.PresentationCarrier(rider)!=null?8:0);
            Color color=Color.white;
            if(actor is FormationFighter fighter&&fighter.Instance==shopDragInstance&&DragPreviewVisible)color.a=.25f;
            body.color=color;
        }

        IEnumerator WarmFormationWindow(FormationUnitArt source,string key,double age,bool loop,string request)
        {yield return source.WarmWindow(key,age,loop);warmingFormationWindows.Remove(request);}

        IEnumerable<string> FormationInitialMotionKeys(FormationUnitArt source,string id)
        {
            if(source.NativeTask==null)yield break;
            var actor=Model.Units.FirstOrDefault(u=>u.ArtId==id);
            if(actor==null)yield break;
            yield return FormationSourceAction(source,actor,true);
            if(FormationAttackChannel(source,actor,out var attackBegin,out var attackCycle,out var attackEnd))
            {yield return attackBegin;yield return attackCycle;yield return attackEnd;}
            if(FormationChannel(source,actor,out var begin,out var cycle,out var end))
            {yield return begin;yield return cycle;yield return end;}
            if(source.NativeTask=="WIS_APRIL_G9")
            {
                yield return "Attack_B";yield return "Attack_C";
                if(Campaign?.WisadelSkill==2){yield return "Skill_2_Loop";yield return "Skill_2_Idle";yield return "Skill_2_End";}
            }
            if(source.NativeTask=="B14") { yield return "Skill_2_Begin";yield return "Skill_2_End"; }
        }

        void SelectFormationCadejoMotion(FormationActor actor,FormationUnitArt source,double now,
            ref string key,ref double age,ref bool loop)
        {
            var blink=Model.CadejoBlinks.LastOrDefault(c=>c.ActorId==actor.Id&&!c.Cancelled);
            if(blink==null)return;
            bool arriving=now>=blink.ArrivedAt;
            string clip=arriving?"Skill_2_End":"Skill_2_Begin";
            double elapsed=(now-(arriving?blink.ArrivedAt:blink.StartedAt))*FormationVisualRate;
            if(elapsed<0||elapsed>=source.Duration(clip))return;
            key=clip;age=elapsed;loop=false;
        }

        void SelectNativeFormationMotion(FormationUnitArt source,FormationActor actor,double now,bool returning,
            ref string key,ref double age,ref bool loop)
        {
            string task=source.NativeTask;
            int phase=actor is FormationFighter fighter?fighter.NativePhase:0;
            if(returning)
            {
                var returningUnit=(FormationFighter)actor;
                if(task=="B17"||task=="B13")
                {
                    string[] parts=task=="B17"?new[]{"revive_1","revive_2","revive_3"}:new[]{"A_Revive_1","A_Revive_2","A_Revive_3"};
                    double total=parts.Sum(p=>(double)source.Duration(p));
                    double at=Math.Max(0,now-returningUnit.NativePhaseChangedAt)*FormationVisualRate;
                    foreach(string part in parts)
                    {
                        key=part;age=Math.Min(at,source.Duration(part));
                        if(at<source.Duration(part))break;at-=source.Duration(part);
                    }
                    loop=false;return;
                }
                key=FirstNativeAction(source,task=="B18"?new[]{"Skill_2"}:task=="B06"?new[]{"Reborn"}:
                    task=="B17"?new[]{"revive_1","revive_3"}:task=="B08"?new[]{"A_Die_Start"}:
                    task=="B24"?new[]{"A_Skill_2"}:new[]{"Revive1","Revive_1","A_Revive_1","cast","death"});
                double duration=Math.Max(.001,returningUnit.NativePhaseReturningUntil-returningUnit.NativePhaseChangedAt);
                age=Math.Max(0,now-returningUnit.NativePhaseChangedAt)*FormationVisualRate;loop=false;return;
            }
            bool action=key=="attack"||key=="cast";
            if(key=="attack"&&PresentFormationChannel(source,actor,now,ref key,ref age,ref loop,false))return;
            if(task=="B20"&&actor.SkillAnimation=="Skill_2_Begin"&&now-actor.LastSkillAt<8)
            {
                if(key=="attack")key="Skill_2_Loop";
                else if(key=="idle")key="Skill_2_Idle";
            }
            if(task=="WIS_APRIL_G9")
            {
                if((key=="idle"||key=="walk"||key=="attack")&&Campaign?.WisadelSkill==2)
                {
                    var mode=Model.PresentationActions.LastOrDefault(a=>a.ActorId==actor.Id&&a.Skill&&!a.ProjectileOnly&&!a.Cancelled&&a.Animation=="Skill_2_Begin");
                    double ended=(mode?.ContactAt??double.NegativeInfinity)+8;
                    if(now>=ended&&now<ended+source.Duration("Skill_2_End")/FormationVisualRate&&actor.LastActionAt<ended)
                    {key="Skill_2_End";age=(now-ended)*FormationVisualRate;loop=false;return;}
                }
                if(Model.PresentationBuff(actor,"wisadel")>0)
                {
                    if(key=="attack")key="Skill_2_Loop";
                    else if(key=="idle")key="Skill_2_Idle";
                }
                else if(key=="attack")key=new[]{"Attack_A","Attack_B","Attack_C"}[(Model.PresentationActions.LastOrDefault(a=>a.ActorId==actor.Id&&!a.Skill)?.Sequence??0)%3];
            }
            if(key=="attack"&&!source.HasOrdinaryAttack&&FormationCampaignRules.Find(actor.ArtId)?.DamageType=="治疗")key="cast";
            if(task=="B20"&&key=="cast"&&actor.SkillAnimation=="Skill_3_Begin")
            {
                double elapsed=now-actor.LastSkillAt,remaining=actor.ActionUntil-now;
                double begin=Math.Min(.25,source.Duration("Skill_3_Begin")),end=Math.Min(.3,source.Duration("Skill_3_End"));
                if(elapsed<begin){key="Skill_3_Begin";age=elapsed/begin*source.Duration(key);loop=false;}
                else if(remaining<end){key="Skill_3_End";age=(end-remaining)/end*source.Duration(key);loop=false;}
                else{key="Skill_3_Loop";age=(elapsed-begin)/.7*source.Duration(key);loop=true;}
                return;
            }
            if(key=="cast")
            {
                if(PresentFormationChannel(source,actor,now,ref key,ref age,ref loop))return;
                string supplied=actor.PresentationAction;
                if(!string.IsNullOrEmpty(supplied)&&source.HasAction(supplied))key=supplied;
                else if(task=="B02")key="attack";
                else if(task=="B04")key=phase>0?"B_Skill2":"A_Skill2";
                else if(task=="B06")key=phase>0?"C2_Skill_2":"C1_Skill_2";
                else if(task=="B07")key=phase>0?"Skill_b_1":"Skill_a_1";
                else if(task=="B08")key=phase>0?"B_Skill_1":"A_Skill_1";
                else if(task=="B09")key=phase>0?"B_Skill_2":"A_Skill_2";
                else if(task=="B11")key=phase>0?"B_Skill_1":"A_Skill_1";
                else if(task=="B13")key=phase>0?"B_Skill_Begin_1":"A_Attack";
                else if(task=="B15"||task=="B24")key=phase>0?"B_Skill_1":"A_Skill_1";
                else if(task=="B16")key=Campaign!=null&&Campaign.TalulahCost<=3?(phase>0?"Skill_1_B":"Skill_1_A"):(phase>0?"Skill_2_B":"Skill_2_A");
                else if(task=="B18")key="Skill_1";
                else if(task=="B19")key="Skill_1";
                else if(task=="B20")key="Skill_3_Begin";
                else if(task=="B21")key="Skill_Begin_A";
            }
            else if(phase>0)
            {
                if(task=="B06")key=key=="death"?"C2_Die_1":"C2_"+(key=="walk"?"Move":Title(key))+"_1";
                else if(task=="B07")key=key=="death"?"Die_2":(key=="walk"?"Move":Title(key))+"_b";
                else if(task=="B13")key=(key=="death"?"B_Die_":key=="walk"?"B_Move_":"B_Idle_")+(phase>1?"2":"1");
                else if(task=="B23")key="B_"+(key=="walk"?"Move":key=="attack"?"Attack_1":Title(key));
                else if(task=="B16")key=key=="death"?"Die":(key=="walk"?"Move":Title(key))+"_B";
                else if(task=="B17")key=key=="death"?"Die":(key=="walk"?"Move":Title(key))+"_2";
                else if(task=="B08"||task=="B04"||task=="B09"||task=="B11"||task=="B12"||task=="B15"||task=="B23"||task=="B24"||task=="B25")
                    key="B_"+(key=="walk"?"Move":Title(key));
            }
            if(!source.HasAction(key))key=FirstNativeAction(source,action?new[]{"cast","attack","idle"}:new[]{key,"idle"});
            // This approved 50 ms GIF walk cycles from actual movement distance,
            // matching the earlier 0.75-unit/s reference stride.
            if(source.UsesApprovedWalk(key)&&actor is FormationFighter walking)
                age=walking.GroundDistanceTravelled/.75;
            if(action)
            {
                double started=actor.LastActionAt,duration=Math.Max(.001,actor.ActionUntil-started);
                if(actor.PresentationDuration>0&&actor.PresentationStartedAt>=actor.LastActionAt-.0001)
                {started=actor.PresentationStartedAt;duration=actor.PresentationDuration;}
                age=Math.Max(0,now-started)*(actor.PresentationRate>0?actor.PresentationRate:FormationVisualRate);loop=false;
            }
        }
        static string Title(string text)=>string.IsNullOrEmpty(text)?text:char.ToUpperInvariant(text[0])+text.Substring(1);
        static string FirstNativeAction(FormationUnitArt source,IEnumerable<string> keys)
        {foreach(string key in keys)if(source.HasAction(key))return key;return "idle";}

        BossRosterFxLayer formationEffects;
        readonly Dictionary<string,SpriteRenderer> formationAuxBodies=new Dictionary<string,SpriteRenderer>();
        readonly Dictionary<string,BossRosterFxArt.Binding[]> formationEffectBindings=new Dictionary<string,BossRosterFxArt.Binding[]>();
        static string FormationNativeSkill(string task,string action=null)
        {
            if(task=="B20")return action=="Skill_1"?"s1":action=="Skill_2_Begin"?"s2":"s3";
            switch(task)
            {
                case "B01":return "c4";case "B04":return "circleattack";case "B05":return "shining";
                case "B07":return "chain_1";case "B08":return "roar";case "B09":return "replace";
                case "B10":return "spray";case "B11":return "annihilation";case "B12":return "deepbreaths1";
                case "B13":return "m1attack";case "B14":return "multi";case "B15":return "summonkingdom";
                case "B18":return "iceRing";case "B20":return "s3";case "B21":return "threestrikes";
                case "B22":return "attack";case "B23":return "m0multirangedattack";
                case "B24":return "ignitehand";case "B25":return "shootgun_1";case "B26":return "summonenemy";
                default:return "attack";
            }
        }
        BossRosterFxArt.Binding[] FormationBindings(string task)
        {
            if(string.IsNullOrEmpty(task)||!task.StartsWith("B",StringComparison.Ordinal))return Array.Empty<BossRosterFxArt.Binding>();
            if(formationEffectBindings.TryGetValue(task,out var prior))return prior;
            prior=BossRosterFxArt.Bindings(task).ToArray();
            formationEffectBindings[task]=prior;return prior;
        }
        IEnumerable<ArtSequence> FormationEffectSequences(FormationUnitArt source)
        {
            var seen=new HashSet<string>();
            foreach(var actor in Model?.Units??Enumerable.Empty<FormationFighter>())
                if(formationMonsterArt.TryGetValue(actor.ArtId,out var same)&&same==source)
                    foreach(var sequence in FormationLegacySequences(actor.ArtId))
                        if(seen.Add(sequence.CacheKey))yield return sequence;
            foreach(var binding in FormationBindings(source.NativeTask))
            {
                var clip=BossRosterFxArt.Get(BossRosterFxArt.Key(source.NativeTask,binding.key));
                if(seen.Add(clip.Sequence.CacheKey))yield return clip.Sequence;
            }
            if(source.NativeTask=="B16")
                foreach(string key in new[]{"ignite-A-caster","ignite-A-target","ignite-B-caster","ignite-B-target"})
                {var sequence=BossRosterSupplementArt.Talulah(key).Sequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
        }

        void PresentFormationBeams(FormationFighter owner,Vector2 collision,double age)
        {
            if(!FormationArtAvailable("P26")||!formationMonsterArt.TryGetValue("P26",out var source))return;
            string action=source.HasAction("cast")?"cast":"idle";
            double sourceAge=age*FormationVisualRate;
            if(sourceAge>=source.Duration(action))return;
            if(!source.WindowReady(action,sourceAge,false))
            {
                string request="P26/beam";
                if(warmingFormationWindows.Add(request))StartCoroutine(WarmFormationWindow(source,action,sourceAge,false,request));
            }
            var frame=source.Frame(action,sourceAge,false);if(frame==null)return;
            for(int side=-1;side<=1;side+=2)
            {
                string key=owner.Id+"/beam/"+side;
                if(!formationAuxBodies.TryGetValue(key,out var view)||view==null)
                {
                    view=new GameObject("Original Yi beam "+side,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    view.transform.SetParent(actorRoot,false);view.sharedMaterial=actorMaterial;formationAuxBodies[key]=view;
                }
                var point=collision+new Vector2(side*1.8f*(1-Mathf.Clamp01((float)age/.25f)),0);
                view.enabled=FormationPointVisibleInView(point);view.sprite=frame;view.flipX=side>0;
                view.transform.SetPositionAndRotation(ActorWorldPoint(point),battleCamera.transform.rotation);
                view.transform.localScale=Vector3.one;
                view.color=Color.white;
                view.sortingOrder=5005-Mathf.RoundToInt(point.y*50);
            }
        }
        internal void DisposeFormationNativeEffects()
        {
            DisposeFormationLegacyEffects();
            formationEffects?.Dispose();formationEffects=null;
            foreach(var view in formationAuxBodies.Values)if(view!=null)Destroy(view.gameObject);
            formationAuxBodies.Clear();
            formationShopIdleUses.Clear();formationShopLastIdle.Clear();
        }
    }
}
