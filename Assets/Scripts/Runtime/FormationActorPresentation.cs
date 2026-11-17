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
            if(!id.StartsWith("D",StringComparison.Ordinal))return formationMonsterArt.TryGetValue(id,out var source)&&source.IsReady&&FormationEffectSequences(source).All(c=>c.Ready);
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
        public float ActorPresentationHeight(FormationActor actor)=>actor is FormationFighter?
            FormationUnitCatalog.Find(actor.ArtId)?.VisualHeight??1.12f:1.12f;

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
            double now=Model.Clock,deathAge=now-actor.LastDeathAt;
            bool returning=actor is FormationFighter returningFighter&&returningFighter.NativePhaseReturningUntil>now;
            float deathDuration=defender?.65f:formationMonsterArt.TryGetValue(actor.ArtId,out var deathSource)?deathSource.Duration("death"):.65f;
            body.enabled=FormationActorVisibleInView(actor)&&FormationArtAvailable(actor.ArtId)&&(actor.Alive||returning||deathAge>=0&&deathAge<deathDuration);
            if(!body.enabled)return;
            bool acting=actor.Alive&&now<actor.ActionUntil&&actor.LastActionAt>=0;
            bool casting=acting&&actor.LastSkillAt>=actor.LastActionAt-.0001;
            string key=!actor.Alive?"death":actor.IsMoving?"walk":casting?"cast":acting?"attack":"idle";
            double age=key=="death"?deathAge:key=="attack"||key=="cast"?now-actor.LastActionAt:
                Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now;
            float sourceHeight;
            if(!defender)
            {
                var source=formationMonsterArt[actor.ArtId];sourceHeight=source.Height;
                bool loop=key!="attack"&&key!="cast"&&key!="death";
                if(source.NativeTask!=null)
                {
                    SelectNativeFormationMotion(source,actor,now,returning,ref key,ref age,ref loop);
                    string request=actor.ArtId+"/"+key;
                    if(!source.WindowReady(key,age,loop)&&warmingFormationWindows.Add(request))
                        StartCoroutine(WarmFormationWindow(source,key,age,loop,request));
                    var nativeFrame=source.Frame(key,age,loop);
                    // A newly entered phase can request an uncached page. Retain the last real
                    // source frame while the same production cache prepares that window.
                    if(nativeFrame!=null)body.sprite=nativeFrame;
                }
                else
                {
                bool sourceTimed=false;
                if(actor is FormationFighter phaseFighter&&actor.ArtId=="M30")
                {
                    if(returning)
                    {
                        double tail=phaseFighter.NativePhaseReturningUntil-now;
                        key=tail<source.Duration("extra_revive")?"extra_revive":"extra_idle_2";
                        age=tail<source.Duration("extra_revive")?source.Duration("extra_revive")-tail:now-phaseFighter.NativePhaseChangedAt;
                        sourceTimed=true;
                    }
                    else if(phaseFighter.NativePhase>0)
                        key=key=="idle"?"extra_idle_2":key=="walk"?"extra_move_2":key=="death"?"extra_die_2":key;
                }
                if(casting&&actor.ArtId=="M31"&&source.HasAction("extra_skill_front_start"))
                {
                    double elapsed=now-actor.LastSkillAt,remaining=actor.ActionUntil-now;
                    double begin=source.Duration("extra_skill_front_start"),end=source.Duration("extra_skill_front_end");
                    key=elapsed<begin?"extra_skill_front_start":remaining<end?"extra_skill_front_end":"extra_skill_front_loop";
                    age=elapsed<begin?elapsed:remaining<end?end-remaining:elapsed-begin;sourceTimed=true;
                }
                if(!source.HasAction(key))
                {
                    if(key=="death"){body.enabled=false;return;}
                    key=casting&&source.HasAction("skill")?"skill":casting&&source.HasAction("attack")?"attack":"idle";
                }
                if(!sourceTimed&&(key=="attack"||key=="cast"||key=="skill"))age=Math.Min(.999999,Math.Max(0,age/Math.Max(.001,actor.ActionUntil-actor.LastActionAt)))*source.Duration(key);
                body.sprite=source.Frame(key,age,key!="attack"&&key!="cast"&&key!="skill"&&key!="death"&&key!="extra_die_2"&&key!="extra_revive"&&key!="extra_skill_front_start"&&key!="extra_skill_front_end");
                }
            }
            else
            {
                var source=art[actor.ArtId];sourceHeight=source.Height;
                if(!source.Clips.TryGetValue(key,out var clip))clip=source.Clips["idle"];
                if(key=="attack")
                {
                    double progress=Math.Min(.999999,Math.Max(0,age/Math.Max(.001,actor.ActionUntil-actor.LastActionAt)));
                    source.Clips.TryGetValue("attack_start",out var begin);source.Clips.TryGetValue("attack_end",out var end);
                    double at=progress*(clip.Duration+(begin?.Duration??0)+(end?.Duration??0));
                    if(begin!=null&&at<begin.Duration){clip=begin;age=at;}
                    else if(at<(begin?.Duration??0)+clip.Duration)age=at-(begin?.Duration??0);
                    else {age=at-(begin?.Duration??0)-clip.Duration;clip=end??clip;}
                }
                int frame=(int)(Math.Max(0,age)*(clip.Count/clip.Duration));
                frame=key=="death"||key=="attack"?Mathf.Clamp(frame,0,clip.Count-1):frame%clip.Count;body.sprite=clip.Frames[frame];
            }
            body.enabled=body.sprite!=null;if(!body.enabled)return;
            body.flipX=actor.Facing.x<-.01f;body.transform.position=ActorWorldPoint(actor);body.transform.rotation=battleCamera.transform.rotation;
            float scale=ActorPresentationHeight(actor)/Mathf.Max(.5f,sourceHeight>0?sourceHeight:1.4f);
            // This approved comic package has a single standing pose. A small presentation-only
            // breath keeps preparation live without inventing a missing frame animation.
            float breath=!defender&&actor.ArtId=="WISADEL_APRIL"&&Model.Phase==FormationPhase.Preparation?1+Mathf.Sin(Time.unscaledTime*2.6f)*.007f:1;
            body.transform.localScale=new Vector3(scale,scale*breath,scale);
            body.sortingOrder=5000-Mathf.RoundToInt(actor.Position.y*50);
            bool skill=now-actor.LastSkillAt<.45;
            Color color=now-actor.LastHitAt<.13?new Color(1,.5f,.42f):skill?new Color(1,.89f,.58f):Color.white;
            if(actor is FormationFighter fighter&&fighter.Instance==shopDragInstance&&DragPreviewVisible)color.a=.25f;
            body.color=color;
        }

        IEnumerator WarmFormationWindow(FormationUnitArt source,string key,double age,bool loop,string request)
        {yield return source.WarmWindow(key,age,loop);warmingFormationWindows.Remove(request);}

        static void SelectNativeFormationMotion(FormationUnitArt source,FormationActor actor,double now,bool returning,
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
                    double at=Math.Max(0,now-returningUnit.NativePhaseChangedAt)/Math.Max(.001,returningUnit.NativePhaseReturningUntil-returningUnit.NativePhaseChangedAt)*total;
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
                age=Math.Max(0,now-returningUnit.NativePhaseChangedAt)/duration*source.Duration(key);loop=false;return;
            }
            bool action=key=="attack"||key=="cast";
            if(task=="B20"&&actor.SkillAnimation=="Skill_2_Begin"&&now-actor.LastSkillAt<8)
            {
                if(key=="attack")key="Skill_2_Loop";
                else if(key=="idle")key="Skill_2_Idle";
            }
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
                string supplied=actor.PresentationAction;
                if(!string.IsNullOrEmpty(supplied)&&source.HasAction(supplied))key=supplied;
                else if(task=="B02")key="attack";
                else if(task=="B06")key=phase>0?"C2_Skill_2":"C1_Skill_2";
                else if(task=="B08")key=phase>0?"B_Skill_1":"A_Skill_1";
                else if(task=="B13")key=phase>0?"B_Skill_Begin_1":"A_Attack";
                else if(task=="B16")key=phase>0?"Skill_1_B":"Skill_1_A";
                else if(task=="B18")key="Skill_1";
                else if(task=="B19")key="Skill_1";
                else if(task=="B20")key="Skill_3_Begin";
                else if(task=="B21")key="Skill_Begin_A";
            }
            else if(phase>0)
            {
                if(task=="B06")key=key=="death"?"C2_Die_1":"C2_"+Title(key)+"_1";
                else if(task=="B13")key=key=="death"?"B_Die_1":key=="walk"?"B_Move_1":key=="attack"?"B_Attack_Loop_1":"B_Idle_1";
                else if(task=="B16")key=key=="death"?"Die":Title(key)+"_B";
                else if(task=="B17")key=key=="death"?"Die":Title(key)+"_2";
                else if(task=="B08"||task=="B04"||task=="B09"||task=="B11"||task=="B12"||task=="B15"||task=="B23"||task=="B24"||task=="B25")
                    key="B_"+Title(key);
            }
            if(!source.HasAction(key))key=FirstNativeAction(source,action?new[]{"cast","attack","idle"}:new[]{key,"idle"});
            if(action)
            {
                double started=actor.LastActionAt,duration=Math.Max(.001,actor.ActionUntil-started);
                if(actor.PresentationDuration>0&&actor.PresentationStartedAt>=actor.LastActionAt-.0001)
                {started=actor.PresentationStartedAt;duration=actor.PresentationDuration;}
                age=Math.Min(.999999,Math.Max(0,(now-started)/duration))*source.Duration(key);loop=false;
            }
        }
        static string Title(string text)=>string.IsNullOrEmpty(text)?text:char.ToUpperInvariant(text[0])+text.Substring(1);
        static string FirstNativeAction(FormationUnitArt source,IEnumerable<string> keys)
        {foreach(string key in keys)if(source.HasAction(key))return key;return "idle";}

        BossRosterFxLayer formationEffects;
        readonly Dictionary<string,SpriteRenderer> formationAuxBodies=new Dictionary<string,SpriteRenderer>();
        readonly Dictionary<string,BossRosterFxArt.Binding[]> formationEffectBindings=new Dictionary<string,BossRosterFxArt.Binding[]>();
        static readonly string[] FormationCommonEffects={"common/common_cuting_hit_01","common/common_magic_hit_01",
            "common/common_heal_start_01","common/common_heal_hit_01","common/common_enemy_die_01"};
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
            string nativeSkill=FormationNativeSkill(task);
            prior=BossRosterFxArt.Bindings(task).Where(b=>b.skill=="attack"||b.skill==nativeSkill||b.nativeSkill==nativeSkill||task=="B20"&&(b.skill=="s1"||b.skill=="s2"||b.skill=="s3")||
                b.kind=="Death"||b.skill=="reborn"||b.nativeSkill=="reborn").ToArray();
            formationEffectBindings[task]=prior;return prior;
        }
        IEnumerable<ArtSequence> FormationEffectSequences(FormationUnitArt source)
        {
            var seen=new HashSet<string>();
            foreach(string key in FormationCommonEffects)
            {var sequence=OriginalCombatVfxArt.Get(key).Sequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
            foreach(var binding in FormationBindings(source.NativeTask))
            {
                var clip=BossRosterFxArt.Get(BossRosterFxArt.Key(source.NativeTask,binding.key));
                if(seen.Add(clip.Sequence.CacheKey))yield return clip.Sequence;
            }
            if(source.NativeTask=="B16")
                foreach(string key in new[]{"ignite-A-caster","ignite-A-target","ignite-B-caster","ignite-B-target"})
                {var sequence=BossRosterSupplementArt.Talulah(key).Sequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
        }
        internal void PresentFormationNativeEffects()
        {
            if(Model==null||actorRoot==null||battleCamera==null)return;
            if(formationEffects==null)formationEffects=new BossRosterFxLayer(actorRoot);
            foreach(var view in formationAuxBodies.Values)if(view!=null)view.enabled=false;
            formationEffects.Begin();double now=Model.Clock;
            foreach(var actor in Model.Units.Where(u=>u.Enabled))
            {
                if(!FormationActorVisibleInView(actor)||!formationMonsterArt.TryGetValue(actor.ArtId,out var source))continue;
                float scale=ActorPresentationHeight(actor)/Math.Max(.5f,source.Height);
                var bindings=FormationBindings(source.NativeTask);
                int phase=source.NativeTask=="B08"&&actor.NativePhase>0?3:actor.NativePhase+1;
                if(actor.Alive)
                {
                    bool casting=actor.LastSkillAt>=actor.LastActionAt-.0001;
                    double age=now-actor.LastActionAt;
                    if(age>=0&&age<2.5)
                    {
                        var starts=bindings.Where(b=>(b.kind=="AttackStart"||b.kind=="SkillStart")&&b.attach=="owner"&&
                            (b.phase<0||b.phase==phase)&&MatchesFormationEffectSkill(b,source.NativeTask,casting,actor.SkillAnimation));
                        foreach(var binding in starts.Take(3))ShowFormationEffect(binding,source.NativeTask,age,actor.Position,actor,scale);
                    }
                    if(source.NativeTask=="B16"&&now-actor.LastSkillAt>=0&&now-actor.LastSkillAt<2)
                    {
                        var clip=BossRosterSupplementArt.Talulah(actor.NativePhase>0?"ignite-B-caster":"ignite-A-caster");
                        ShowFormationClip(clip,now-actor.LastSkillAt,actor.Position,actor,scale,false);
                    }
                }
                else
                {
                    double age=now-actor.LastDeathAt;
                    bool returning=actor.NativePhaseReturningUntil>now;
                    foreach(var binding in bindings.Where(b=>returning?b.skill=="reborn"||b.nativeSkill=="reborn":b.kind=="Death").Take(2))
                        ShowFormationEffect(binding,source.NativeTask,age,actor.Position,actor,scale);
                }
            }
            // Every impact is driven by an applied gameplay hit. One ranged line or an
            // animation preview cannot create an extra hit, heal, or synergy event.
            foreach(var hit in Model.Hits)
            {
                double age=now-hit.At;if(age<0||age>1.6||!FormationPointVisibleInView(hit.To))continue;
                var actor=Model.Units.FirstOrDefault(u=>u.Id==hit.FromId);
                FormationUnitArt source=null;
                if(actor!=null)formationMonsterArt.TryGetValue(actor.ArtId,out source);
                if(actor?.ArtId=="B26"&&hit.Skill&&age<.65)
                    PresentFormationBeams(actor,hit.To,age);
                bool displayed=false;
                if(actor!=null&&source!=null&&!hit.Heal)
                {
                    float scale=ActorPresentationHeight(actor)/Math.Max(.5f,source.Height);
                    foreach(var binding in FormationBindings(source.NativeTask).Where(b=>b.kind=="Impact"&&
                        (b.phase<0||b.phase==(source.NativeTask=="B08"&&actor.NativePhase>0?3:actor.NativePhase+1))&&MatchesFormationEffectSkill(b,source.NativeTask,hit.Skill,actor.SkillAnimation)).Take(2))
                        displayed|=ShowFormationEffect(binding,source.NativeTask,age,hit.To,actor,scale);
                    if(source.NativeTask=="B16"&&hit.Skill)
                        displayed|=ShowFormationClip(BossRosterSupplementArt.Talulah(actor.NativePhase>0?"ignite-B-target":"ignite-A-target"),age,hit.To,actor,scale,false);
                }
                if(!displayed)
                {
                    string key=hit.Heal?"common/common_heal_hit_01":actor!=null&&FormationCampaignRules.Find(actor.ArtId)?.DamageType=="法"?
                        "common/common_magic_hit_01":"common/common_cuting_hit_01";
                    var original=OriginalCombatVfxArt.Get(key);
                    if(original.Sequence.Ready)
                        ShowFormationClip(new BossRosterFxArt.Clip(original.Sequence,original.HdrRange,0,original.Loop),age,hit.To,actor,1,false);
                }
            }
            formationEffects.End();
        }
        static bool MatchesFormationEffectSkill(BossRosterFxArt.Binding binding,string task,bool skill,string action=null)
        {
            if(!skill)return binding.skill=="attack";
            string native=FormationNativeSkill(task,action);
            return binding.skill==native||binding.nativeSkill==native;
        }
        bool ShowFormationEffect(BossRosterFxArt.Binding binding,string task,double age,Vector2 point,FormationActor actor,float scale)
        {
            if(!BossRosterFxArt.TryGet(BossRosterFxArt.Key(task,binding.key),out var clip))return false;
            return ShowFormationClip(clip,age,point,actor,scale,binding.loop);
        }
        bool ShowFormationClip(BossRosterFxArt.Clip clip,double age,Vector2 point,FormationActor actor,float scale,bool loop)
        {
            if(clip==null||!clip.Sequence.Ready||!FormationPointVisibleInView(point))return false;
            Vector3 position=ActorWorldPoint(point)+Vector3.up*.12f;
            bool result=formationEffects.Show(clip,age,position,battleCamera,actor!=null&&actor.Facing.x<0,loop,
                actor!=null?new Vector3(actor.Facing.x,0,actor.Facing.y):Vector3.right,false,24,0,scale);
            return result;
        }
        void PresentFormationBeams(FormationFighter owner,Vector2 collision,double age)
        {
            if(!FormationArtAvailable("P26")||!formationMonsterArt.TryGetValue("P26",out var source))return;
            string action=source.HasAction("cast")?"cast":"idle";
            double sourceAge=age/.65*source.Duration(action);
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
                view.transform.localScale=Vector3.one*(1.15f/Math.Max(.5f,source.Height));
                view.color=new Color(1,1,1,Mathf.Clamp01((float)(.65-age)/.2f));
                view.sortingOrder=5005-Mathf.RoundToInt(point.y*50);
            }
        }
        internal void DisposeFormationNativeEffects()
        {
            formationEffects?.Dispose();formationEffects=null;
            foreach(var view in formationAuxBodies.Values)if(view!=null)Destroy(view.gameObject);
            formationAuxBodies.Clear();
            formationShopIdleUses.Clear();formationShopLastIdle.Clear();
        }
    }
}
