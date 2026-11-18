using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    // Read-only migration of the existing, approved native effects. This owns no
    // damage, healing, skill casts or phase transitions. Ordinary play still uses
    // the v0.7 mechanics; a visual clip never creates an original-game mechanic.
    public sealed partial class FormationBattleController
    {
        sealed class LegacyFxView
        {
            internal SpriteRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        sealed class LegacyPatriotPage {internal Texture2D Texture;internal Sprite Sprite;internal int LastUse;}
        sealed class LegacyComicBurst {internal LineRenderer Ink,Light;}
        [Serializable] sealed class LegacyPatriotCatalog {public int fps;public LegacyPatriotClip[] clips;}
        [Serializable] sealed class LegacyPatriotClip {public string name;public int frames;public string[] pages;public float worldSize,anchorX,anchorY;}
        [Serializable] sealed class LegacyTalulahManifest {public int fps;public LegacyTalulahClip[] clips;}
        [Serializable] sealed class LegacyTalulahClip
        {public string key;public int count,width,height,columns;public float ppu,pivotX,pivotY,duration,hdrRange;public LegacyTalulahPage[] pages;}
        [Serializable] sealed class LegacyTalulahPage {public string file;public int count;}
        sealed class LegacyTalulahFrames {internal LegacyTalulahClip Data;internal Sprite[] Frames;}
        readonly List<LegacyFxView> legacyFxViews=new List<LegacyFxView>();
        readonly Dictionary<string,LegacyPatriotPage> legacyPatriotPages=new Dictionary<string,LegacyPatriotPage>();
        readonly Dictionary<string,LegacyTalulahFrames> legacyTalulahFrames=new Dictionary<string,LegacyTalulahFrames>();
        readonly Dictionary<int,float> legacyPreviousShield=new Dictionary<int,float>();
        readonly Dictionary<int,double> legacyShieldBroken=new Dictionary<int,double>();
        readonly Dictionary<int,double> legacyShieldRefreshed=new Dictionary<int,double>();
        readonly Dictionary<int,Vector3> legacyFlightOrigins=new Dictionary<int,Vector3>();
        readonly Dictionary<Vector2Int,double> legacySeaBorn=new Dictionary<Vector2Int,double>();
        readonly List<MeshRenderer> legacySeaTiles=new List<MeshRenderer>();
        readonly List<LegacyComicBurst> legacyComicBursts=new List<LegacyComicBurst>();
        readonly Dictionary<FormationHit,int> legacyComicFaceIds=new Dictionary<FormationHit,int>();
        LegacyPatriotCatalog legacyPatriotCatalog;
        LegacyTalulahManifest legacyTalulahManifest;
        Material legacyHdrMaterial,legacyPremultMaterial,legacyPatriotMaterial,legacySeaMaterial,legacyComicMaterial;
        Mesh legacySeaMesh;
        FormationBattleModel legacyObservedModel;
        double legacyLastClock=-1;
        int legacyFxUsed,legacyComicUsed,legacyComicFaceSequence;
        const float LegacyVisualRate=2;

        // Only source sequences used by the actual owned role enter the bounded
        // streaming preload. Existing Resources packages remain Resources-owned.
        internal IEnumerable<ArtSequence> FormationLegacySequences(string artId)
        {
            foreach(var sequence in FormationSynergyEffectSequences())yield return sequence;
            if(artId=="WISADEL_APRIL")
                foreach(var sequence in WisadelAprilFxArt.Sequences(Campaign?.WisadelSkill??3))yield return sequence;
            string card=FormationUnitCatalog.Find(artId)?.CardId??("CARD_"+artId);
            var keys=MonsterOriginalVfx.RequiredKeysForCards(new[]{card});
            foreach(var sequence in OriginalCombatVfxArt.SequencesFor(keys))yield return sequence;
            if(artId=="M20")yield return OriginalCombatVfxArt.Get("common/common_ignite_defense").Sequence;
            if(artId=="M11")
                foreach(string key in new[]{"common/common_gongjian_trail_01","common/common_hit_01"})yield return OriginalCombatVfxArt.Get(key).Sequence;
            if(artId=="M32")yield return OriginalCombatVfxArt.Get("common/common_ignite_support").Sequence;
            if(artId=="M38")
                foreach(string key in new[]{"common/common_magic_trail_01_m","common/common_magic_hit_01_m"})yield return OriginalCombatVfxArt.Get(key).Sequence;
            if(artId=="TALULAH_TRIAL")
                foreach(string key in new[]{"attack-B-caster","attack-B-hit","ambient-B-foot","movement-B-foot","ambient-B-head","ambient-B-wing",
                    "first_transition_start","first_transition_end","final_transition_start","final_transition_end","ignite-A-caster","ignite-A-target",
                    "ignite-B-caster","ignite-B-target","explode-A-caster","explode-B-caster","target-burn-loop","original-energy-stream","original-fire-impact"})
                    yield return BossRosterSupplementArt.Talulah(key).Sequence;
        }
        internal bool FormationHasLegacyImpact(string artId,bool skill)
        {
            if(artId=="WISADEL_APRIL")return true;
            if(artId=="TALULAH_TRIAL"||artId=="M25"||artId=="M28"||artId=="M32"||artId=="M20")return true;
            switch(artId)
            {
                case "M11":case "M12":case "M14":case "M16":case "M26":case "M42":case "M30":case "M31":case "M33":case "M34":case "M36":case "M38":return true;
                default:return false;
            }
        }
        float LegacyScale(FormationActor actor)
        {var source=FormationUnitArt.For(actor.ArtId);return ActorPresentationHeight(actor)/Mathf.Max(.5f,source?.Height??1.4f);}
        static int LegacyOrder(Vector2 cell,int offset)=>5000-Mathf.RoundToInt(cell.y*50)+offset;
        Vector3 LegacyOwnerPoint(FormationActor actor,Vector2 snapshot)
        {return ActorWorldPoint(snapshot)+(FormationDisplayPoint(actor)-ActorWorldPoint(actor.Position));}
        void EnsureLegacyMaterials()
        {
            if(legacyHdrMaterial==null)
            {
                var shader=Resources.Load<Shader>("TalulahNative");
                if(shader==null)throw new InvalidOperationException("Missing existing native HDR shader.");
                legacyHdrMaterial=new Material(shader){name="Formation / restored native HDR effects"};
            }
        }
        void LegacyDraw(Sprite sprite,Material material,Vector3 position,Vector2 ground,float scale,int order,bool flip=false,float hdr=1,float frame=-1,bool flat=false,float roll=0)
        {
            if(sprite==null||battleCamera==null||!FormationPointVisibleInView(ground))return;
            if(legacyFxUsed==legacyFxViews.Count)
            {
                var renderer=new GameObject("Restored original formation effect",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(actorRoot,false);legacyFxViews.Add(new LegacyFxView{Renderer=renderer});
            }
            var view=legacyFxViews[legacyFxUsed++];var r=view.Renderer;
            r.name="Formation original / "+sprite.name;r.enabled=true;r.sprite=sprite;r.sharedMaterial=material;r.flipX=flip;r.color=Color.white;
            var rotation=battleCamera.transform.rotation*Quaternion.Euler(0,0,roll);var stretch=Vector3.one*scale;
            if(flat)
            {
                var forward=Vector3.ProjectOnPlane(battleCamera.transform.up,Vector3.up).normalized;
                float projection=Mathf.Max(.001f,Mathf.Abs(Vector3.Dot(battleCamera.transform.up,forward)));
                rotation=Quaternion.LookRotation(-Vector3.up,forward)*Quaternion.Euler(0,0,roll);stretch.y/=projection;
            }
            r.transform.SetPositionAndRotation(position,rotation);r.transform.localScale=stretch;r.sortingOrder=LegacyOrder(ground,order);
            view.Properties.Clear();view.Properties.SetFloat("_HdrRange",hdr);if(frame>=0)view.Properties.SetFloat("_Frame",frame);r.SetPropertyBlock(view.Properties);
        }
        void LegacyOriginal(string raw,double age,Vector2 cell,int order=24,bool loop=false,bool flat=false,float scale=1,Vector3? explicitPosition=null,Vector3? direction=null)
        {
            if(age<0||!FormationPointVisibleInView(cell))return;
            string key=raw.Contains("/")?raw:"monster/"+raw;
            if(!OriginalCombatVfxArt.TryGet(key,out var clip)||!clip.Sequence.Ready)return;
            if(loop&&!clip.Loop)return; // Never invent looping from a one-shot capture.
            float roll=0;
            if(direction.HasValue&&clip.Directional)
            {
                var vector=direction.Value;float x=Vector3.Dot(vector,battleCamera.transform.right),y=Vector3.Dot(vector,battleCamera.transform.up);
                if(x*x+y*y>1e-8)roll=Mathf.Atan2(y,x)*Mathf.Rad2Deg-clip.ForwardDegrees;
            }
            LegacyDraw(clip.At(age,loop),legacyHdrMaterial,explicitPosition??(ActorWorldPoint(cell)+Vector3.up*.025f),cell,scale,order,hdr:clip.HdrRange,flat:flat,roll:roll);
        }
        void LegacyOriginalBeam(string key,double age,Vector2 from,Vector2 to,float lift=1.05f,bool loop=true,Vector3? explicitStart=null)
        {
            if(age<0||!FormationPointVisibleInView(from)||!OriginalCombatVfxArt.TryGet("monster/"+key,out var clip)||!clip.Sequence.Ready||loop&&!clip.Loop)return;
            var start=explicitStart??(ActorWorldPoint(from)+battleCamera.transform.up*lift);var end=ActorWorldPoint(to)+battleCamera.transform.up*.8f;
            Vector3 delta=end-start;float x=Vector3.Dot(delta,battleCamera.transform.right),y=Vector3.Dot(delta,battleCamera.transform.up);
            var sprite=clip.At(age,loop);if(sprite==null)return;
            float angle=Mathf.Atan2(y,x)*Mathf.Rad2Deg-clip.ForwardDegrees;
            LegacyDraw(sprite,legacyHdrMaterial,start,from,1,28,hdr:clip.HdrRange,roll:angle);
            if(legacyFxUsed>0)legacyFxViews[legacyFxUsed-1].Renderer.transform.localScale=new Vector3(new Vector2(x,y).magnitude/3.005432f,1,1);
        }
        void LegacyPremult(Sprite sprite,Vector2 cell,float scale,int order=24,bool flip=false,Vector3? point=null)
        {
            if(legacyPremultMaterial==null)
            {
                var source=Resources.Load<Material>("MudrockVfx/OriginalEffect");
                if(source==null)throw new InvalidOperationException("Missing existing original premultiplied material.");
                legacyPremultMaterial=new Material(source){name="Formation / restored original premultiplied FX"};
            }
            LegacyDraw(sprite,legacyPremultMaterial,point??ActorWorldPoint(cell),cell,scale,order,flip);
        }
        void LegacyMudrock(string key,double age,Vector2 cell,float scale,int order=24,bool loop=false,bool flip=false,Vector3? point=null)
        {var sprite=MudrockVfxArt.Load(key)?.At(age,loop);LegacyPremult(sprite,cell,scale,order,flip,point);}
        void LegacyMudrockBoth(string key,double age,Vector2 cell,float scale,bool loop=false,bool flip=false,Vector3? ownerPoint=null)
        {LegacyMudrock(key+"_ground",age,cell,scale,-4,loop,flip);LegacyMudrock(key+"_over",age,cell,scale,12,loop,flip,ownerPoint);}

        LegacyTalulahFrames LegacyTalulahCore(string key)
        {
            if(legacyTalulahFrames.TryGetValue(key,out var cached))return cached;
            if(legacyTalulahManifest==null)
            {
                var text=Resources.Load<TextAsset>("TalulahNative/manifest");
                if(text==null)throw new InvalidDataException("Missing approved Talulah native core.");
                legacyTalulahManifest=JsonUtility.FromJson<LegacyTalulahManifest>(text.text);
            }
            var d=legacyTalulahManifest.clips.First(c=>c.key==key);var frames=new List<Sprite>();
            foreach(var page in d.pages)
            {
                var texture=Resources.Load<Texture2D>("TalulahNative/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null)throw new InvalidDataException("Missing Talulah native page "+page.file);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%d.columns*d.width,texture.height-(i/d.columns+1)*d.height,d.width,d.height),new Vector2(d.pivotX,d.pivotY),d.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="TalulahNative/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=d.count)throw new InvalidDataException("Incomplete Talulah native core "+key);
            cached=new LegacyTalulahFrames{Data=d,Frames=frames.ToArray()};legacyTalulahFrames[key]=cached;return cached;
        }
        void LegacyTalulah(string key,double age,Vector2 cell,FormationActor actor,int order=12,bool loop=false,Vector3? position=null)
        {
            if(age<0||!FormationPointVisibleInView(cell))return;Sprite frame;float hdr;
            if(key=="attack-A-caster"||key=="attack-A-hit"||key=="ambient-A-foot"||key=="movement-A-foot"||key=="firecast-A-hand")
            {
                var clip=LegacyTalulahCore(key);if(!loop&&age>=clip.Data.duration)return;
                if(loop)age%=clip.Data.duration;frame=clip.Frames[Mathf.Clamp((int)(age*legacyTalulahManifest.fps),0,clip.Frames.Length-1)];hdr=clip.Data.hdrRange;
            }
            else
            {
                var clip=BossRosterSupplementArt.Talulah(key);if(!clip.Sequence.Ready)return;
                // These five existing phase ambient samples are source loops although the
                // older supplemental adapter listed only the later orb/burn loop keys.
                bool sourceLoop=loop&&(clip.Loop||key.StartsWith("ambient-",StringComparison.Ordinal)||key.StartsWith("movement-",StringComparison.Ordinal));
                if(sourceLoop)age%=clip.Duration;
                frame=clip.At(age,false);hdr=clip.HdrRange;
            }
            LegacyDraw(frame,legacyHdrMaterial,position??(ActorWorldPoint(cell)+battleCamera.transform.up*.12f),cell,LegacyScale(actor),order,actor.Facing.x<0,hdr);
        }
        void LegacyPatriot(string suffix,double age,Vector2 cell,FormationActor actor,int order=12,bool loop=false,Vector3? point=null)
        {
            if(age<0||!FormationPointVisibleInView(cell))return;
            if(legacyPatriotCatalog==null)
            {
                var text=Resources.Load<TextAsset>("PatriotVfx/catalog");
                if(text==null)throw new InvalidDataException("Missing approved Patriot VFX catalog.");
                legacyPatriotCatalog=JsonUtility.FromJson<LegacyPatriotCatalog>(text.text);
            }
            var clip=legacyPatriotCatalog.clips.FirstOrDefault(c=>c.name=="patrt_"+suffix);if(clip==null)return;
            double duration=clip.frames/(double)legacyPatriotCatalog.fps;if(loop)age%=duration;
            int index=(int)(age*legacyPatriotCatalog.fps);if(index>=clip.frames)return;
            string pageName=clip.pages[index/16];
            if(!legacyPatriotPages.TryGetValue(pageName,out var page))
            {
                var texture=Resources.Load<Texture2D>("PatriotVfx/"+pageName);if(texture==null)throw new InvalidDataException("Missing Patriot page "+pageName);
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(clip.anchorX,clip.anchorY),texture.width,0,SpriteMeshType.FullRect);
                sprite.name="Patriot/"+suffix+"/"+pageName;sprite.hideFlags=HideFlags.DontSave;
                page=new LegacyPatriotPage{Texture=texture,Sprite=sprite};legacyPatriotPages[pageName]=page;
            }
            page.LastUse=Time.frameCount;
            if(legacyPatriotMaterial==null)legacyPatriotMaterial=new Material(Resources.Load<Shader>("PatriotVfx/PatriotPortable"));
            float scale=LegacyScale(actor);var position=point??LegacyOwnerPoint(actor,cell);
            float lift=Mathf.Max(4,clip.worldSize*clip.anchorY*Mathf.Max(0,battleCamera.transform.up.y)/Mathf.Max(.1f,-battleCamera.transform.forward.y)+.1f);
            LegacyDraw(page.Sprite,legacyPatriotMaterial,position-battleCamera.transform.forward*(lift*scale),cell,clip.worldSize*scale,order,actor.Facing.x<0,frame:index%16);
        }

        void PresentLegacyAction(FormationPresentationAction action)
        {
            var actor=Model.Units.FirstOrDefault(u=>u.Id==action.ActorId);if(actor==null||!actor.Enabled)return;
            double age=(Model.Clock-action.At)*action.Rate;if(age<0)return;
            string id=actor.ArtId;float scale=LegacyScale(actor);bool flip=actor.Facing.x<0;
            var origin=LegacyOwnerPoint(actor,action.From);string key=null;
            if(id=="WISADEL_APRIL"){PresentOriginalWisadelAction(action,actor);return;}
            if(action.ProjectileOnly)return;
            if(id=="TALULAH_TRIAL")
            {
                if(action.Skill)
                {
                    string caster=Campaign.TalulahCost<=3?actor.NativePhase>0?"ignite-B-caster":"ignite-A-caster":
                        actor.NativePhase>0?"explode-B-caster":"explode-A-caster";
                    LegacyTalulah(caster,age,action.From,actor,position:origin+battleCamera.transform.up*.12f);
                }
                else LegacyTalulah(actor.NativePhase>0?"attack-B-caster":"attack-A-caster",age,action.From,actor,position:origin+battleCamera.transform.up*.12f);
                return;
            }
            if(id=="M20")
            {
                if(!action.Skill)LegacyPatriot(actor.NativePhase>0?"attack_02_start":"attack_01_start",age,action.From,actor,
                    point:origin+battleCamera.transform.up*(actor.NativePhase>0?1.1f:.92f)*scale);
                else LegacyOriginal("common/common_ignite_defense",age,action.From,14,scale:scale,explicitPosition:origin);
                return;
            }
            if(id=="M25")
            {
                var hand=MephistoVfxArt.Hand("Attack",(int)(age*30));
                var position=origin+battleCamera.transform.right*(flip?-hand.x:hand.x)*scale+battleCamera.transform.up*hand.y*scale;
                LegacyPremult(MephistoVfxArt.Load("attack_01_start").At(age),action.From,.779296875f*scale,12,flip,position);return;
            }
            if(id=="M32")
            {
                // The current support skill grants shields; do not fabricate the old
                // damaging gaze or a pillar collapse from this cast.
                if(action.Skill)
                {LegacyOriginal("common/common_ignite_support",age,action.From,14,scale:scale,explicitPosition:origin);return;}
                string phase=actor.NativePhase>0?"C2":"C1";
                var hand=MandragoraVfxArt.Mount(phase+"_Attack"+(actor.NativePhase>0?"_1":""),age,false);
                var position=origin+battleCamera.transform.right*(flip?-hand.x:hand.x)*scale+battleCamera.transform.up*hand.y*scale;
                string prefix=actor.NativePhase>0?"c2_attack_01":"c1_attack";
                LegacyPremult(MandragoraVfxArt.Load(prefix+"_start_01").At(age),action.From,.95f*scale,12,flip,position);
                LegacyPremult(MandragoraVfxArt.Load(prefix+"_start_02").At(age),action.From,.95f*scale,13,flip,position);return;
            }
            if(id=="M28")
            {LegacyMudrock("attackStart_over",age,action.From,scale,12,false,flip,origin);return;}
            switch(id)
            {
                case "M12":key=action.Skill?"snmage_skill_01_start":"snmage_attack_01_start";break;
                case "M14":key=action.Skill?"firstar_skill_01_buff":actor.NativePhase>0?"firstar_attack_02_start":"firstar_attack_01_start";break;
                case "M26":case "M42":key="enemy_mdsnpr_attack_01_start";break;
                case "M30":key="enemy_duskls_attack_start";break;
                case "M36":key=action.Skill?"enemy_dsmant_skill_01_start":"enemy_dsmant_attack_01_start";break;
                case "M34":if(action.Skill)key="enemy_dslntc_skill_begin_buff";break;
            }
            if(key!=null)LegacyOriginal(key,age,action.From,12,flat:true,scale:scale);
            var firstApplied=action.Skill&&(id=="M31"||id=="M38")?Model.Hits.Where(h=>h.FromId==actor.Id&&h.Skill&&h.At>=action.At).OrderBy(h=>h.At).FirstOrDefault():null;
            if(action.Skill&&id=="M31"&&firstApplied!=null&&Model.Clock<action.EndAt)
                LegacyOriginalBeam("enemy_duspfr_2_attack_01_front_start2",(Model.Clock-firstApplied.At)*LegacyVisualRate,actor.Position,firstApplied.To,.95f*scale,
                    explicitStart:FormationDisplayPoint(actor)+battleCamera.transform.up*.95f*scale);
            if(action.Skill&&id=="M38"&&firstApplied!=null&&Model.Clock<firstApplied.At+3)
            {
                double appliedAge=(Model.Clock-firstApplied.At)*LegacyVisualRate;
                LegacyOriginal("dmage_skill_01_start",appliedAge,actor.Position,22,scale:scale,explicitPosition:FormationDisplayPoint(actor));
                LegacyOriginalBeam("dmage_skill_01_line",appliedAge,actor.Position,firstApplied.To,1.1f*scale,
                    explicitStart:FormationDisplayPoint(actor)+battleCamera.transform.up*1.1f*scale);
                LegacyOriginal("dmage_skill_01_buff",appliedAge,firstApplied.To,32,true,true,scale);
            }
            // Recorded launch/contact fields own projectile flight. Never draw a
            // diagnostic line, or keep a projectile after its actual contact.
            string trail=null;
            switch(id)
            {
                case "M11":if(!action.Skill)trail="common/common_gongjian_trail_01";break;
                case "M12":trail="snmage_attack_01_trail";break;case "M14":if(!action.Skill)trail="firstar_attack_01_trail";break;
                case "M16":trail="sotisp_attack_01_trail";break;case "M26":case "M42":trail="enemy_mdsnpr_attack_01_trail";break;
                case "M36":if(!action.Skill)trail="enemy_dsmant_attack_01_trail";break;
                case "M38":if(!action.Skill)trail="common/common_magic_trail_01_m";break;
            }
            if(!action.Cancelled&&trail!=null&&Model.Clock>=action.LaunchedAt&&action.ContactAt>action.LaunchedAt&&Model.Clock<action.ContactAt)
            {
                var arrowTarget = id == "M11" ? Model.Defenders.FirstOrDefault(d => d.Id == action.TargetId) : null;
                if(!legacyFlightOrigins.TryGetValue(action.Sequence,out var start))
                {
                    start = id == "M11" ? FormationFeedbackContactPoint(actor, action.From, .55f) : origin + battleCamera.transform.up * .75f * scale;
                    legacyFlightOrigins[action.Sequence] = start;
                }
                var end = id == "M11" ? FormationFeedbackContactPoint(arrowTarget, action.To, .52f) : ActorWorldPoint(action.To) + battleCamera.transform.up * .8f;
                float progress=Mathf.Clamp01((float)((Model.Clock-action.LaunchedAt)/(action.ContactAt-action.LaunchedAt)));
                var cell=Vector2.Lerp(action.From,action.To,progress);
                bool loop=OriginalCombatVfxArt.TryGet(trail.Contains("/")?trail:"monster/"+trail,out var projectile)&&projectile.Loop;
                LegacyOriginal(trail,(Model.Clock-action.LaunchedAt)*action.Rate,cell,20,loop,scale:scale,
                    explicitPosition:Vector3.Lerp(start,end,progress),direction:end-start);
            }
            else if(!action.Cancelled&&trail!=null&&action.ContactAt<=action.LaunchedAt&&Model.Clock>=action.ContactAt&&Model.Clock<action.ContactAt+.18)
            {
                // Current instant-hit v0.7 actions have no independent flight interval.
                // Show the native projectile's contact remnant at the real endpoint;
                // do not fabricate a flight during the actor's windup.
                LegacyOriginal(trail,(Model.Clock-action.ContactAt)*action.Rate,action.To,20,scale:scale,
                    explicitPosition:ActorWorldPoint(action.To)+battleCamera.transform.up*.8f,direction:ActorWorldPoint(action.To)-origin);
            }
        }

        void PresentLegacyHit(FormationHit hit)
        {
            var actor=Model.Units.FirstOrDefault(u=>u.Id==hit.FromId);if(actor==null||!FormationHasLegacyImpact(actor.ArtId,hit.Skill))return;
            double age=(Model.Clock-hit.At)*LegacyVisualRate;if(age<0)return;string id=actor.ArtId;float scale=LegacyScale(actor);
            if(id=="WISADEL_APRIL"&&!hit.Heal)
            {if(WisadelAprilFxArt.Available)PresentOriginalWisadelHit(hit,actor,age);else PresentLegacyWisadelHit(hit,actor,age);return;}
            if(id=="TALULAH_TRIAL")
            {
                bool directIgnition=hit.Skill&&Campaign.TalulahCost<=3&&Model.PresentationActions.Any(a=>a.ActorId==actor.Id&&a.Skill&&!a.ProjectileOnly&&Math.Abs(a.ContactAt-hit.At)<.03);
                string effect=directIgnition?actor.NativePhase>0?"ignite-B-target":"ignite-A-target":hit.Skill?"original-fire-impact":actor.NativePhase>0?"attack-B-hit":"attack-A-hit";
                LegacyTalulah(effect,age,hit.To,actor,35,position:ActorWorldPoint(hit.To)+battleCamera.transform.up*.65f*scale);return;
            }
            if(id=="M25"&&hit.Heal)
            {LegacyPremult(MephistoVfxArt.Load("attack_01_hit").At(age),hit.To,.779296875f*scale,24);return;}
            if(id=="M32"&&!hit.Heal)
            {LegacyPremult(MandragoraVfxArt.Load(actor.NativePhase>0?"c2_attack_01_hit":"c1_attack_hit").At(age),hit.To,.8f*scale,50,point:ActorWorldPoint(hit.To)+battleCamera.transform.up*.85f*scale);return;}
            if(id=="M28"&&!hit.Heal)
            {LegacyMudrockBoth(hit.Skill?"attack3":"attack1",age,hit.To,scale,flip:actor.Facing.x<0);return;}
            if(id=="M20"&&!hit.Heal)
            {LegacyPatriot(actor.NativePhase>0?"attack_02_hit":"attack_01_hit",age,hit.To,actor,50,point:ActorWorldPoint(hit.To)+battleCamera.transform.up*.7f*scale);return;}
            if(hit.Heal)return;
            string key=null;
            switch(id)
            {
                case "M11":key="common/common_hit_01";break;
                case "M12":key="snmage_attack_01_hit";break;
                // The real snow zone exclusively owns its area layer. A single
                // AoE must not spawn another full range sprite on every victim.
                case "M14":if(!hit.Skill)key="firstar_attack_01_hit";break;
                case "M16":key="sotisp_attack_01_hit";break;case "M26":case "M42":key="enemy_mdsnpr_attack_01_hit";break;
                case "M30":key="enemy_duskls_attack_hit";break;case "M31":key=Math.Abs(hit.At-actor.LastDeathAt)<.05?"enemy_duspfr_2_die_01_hit":"enemy_duspfr_2_attack_01_hit";break;
                case "M34":key="enemy_dslntc_attack_01_hit";break;case "M36":key=hit.Skill?"enemy_dsmant_skill_01_hit":"enemy_dsmant_attack_01_hit";break;
                case "M38":if(!hit.Skill)key="common/common_magic_hit_01_m";break;
                case "M33":break; // Counter caster pulses come from their real LastReflect timestamp below.
            }
            if(key!=null)
            {
                bool ground=id=="M14"&&hit.Skill||id=="M36"&&hit.Skill||id=="M31"&&Math.Abs(hit.At-actor.LastDeathAt)<.05;
                Vector3? point = ground ? (Vector3?)null : ActorWorldPoint(hit.To) + battleCamera.transform.up * .8f * scale;
                if (id == "M11") point = FormationFeedbackContactPoint(Model.Defenders.FirstOrDefault(d => d.Id == hit.ToId), hit.To, .52f);
                LegacyOriginal(key,age,hit.To,35,flat:ground,scale:scale,explicitPosition:point);
            }
        }

        // The old approved April-skin presentation used these exact 16-point
        // black/yellow geometric bursts plus the six user-GIF expression crops.
        // PrepareImpactFace only selected a crop; PresentShot enabled it after a
        // real empowered impact. Reuse that material here, never invent S1/S2 art.
        void PresentLegacyWisadelHit(FormationHit hit,FormationFighter actor,double age)
        {
            double duration=hit.Skill?.55:.18;
            if(age<0||age>=duration||!FormationPointVisibleInView(hit.To))return;
            if(legacyComicMaterial==null)legacyComicMaterial=new Material(Shader.Find("Sprites/Default")){name="Formation / approved April comic burst"};
            float scale=LegacyScale(actor),progress=(float)(age/duration),alpha=1-progress;
            float radius=(float)((hit.Skill?.28:.05)+age*(hit.Skill?1.3:.8))*scale;
            if(hit.Skill&&Campaign!=null&&Campaign.WisadelSkill==3)radius=ViviCombat.SkillSplashRadius*Mathf.Lerp(.55f,1,progress)*scale;
            var center=ActorWorldPoint(hit.To)+battleCamera.transform.up*.8f;
            if(legacyComicUsed==legacyComicBursts.Count)
            {
                var burst=new LegacyComicBurst();
                foreach(bool ink in new[]{true,false})
                {
                    var line=new GameObject(ink?"April comic / approved black ink":"April comic / approved yellow burst",typeof(LineRenderer)).GetComponent<LineRenderer>();
                    line.transform.SetParent(actorRoot,false);line.sharedMaterial=legacyComicMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=16;
                    if(ink)burst.Ink=line;else burst.Light=line;
                }
                legacyComicBursts.Add(burst);
            }
            var view=legacyComicBursts[legacyComicUsed++];int order=LegacyOrder(hit.To,50);
            foreach(bool ink in new[]{true,false})
            {
                var line=ink?view.Ink:view.Light;line.enabled=true;line.sortingOrder=order+(ink?0:1);
                line.widthMultiplier=(hit.Skill?(ink?.13f:.075f):(ink?.06f:.025f))*scale;
                var color=ink?new Color(.035f,.025f,.025f,alpha):new Color(1,.83f,.34f,alpha);line.startColor=line.endColor=color;
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8,r=radius*(i%2==0?1.25f:.64f);
                    line.SetPosition(i,center+(battleCamera.transform.right*Mathf.Cos(angle)+battleCamera.transform.up*Mathf.Sin(angle))*r);
                }
            }
            if(!hit.Skill)return;
            var card=formationCards.FirstOrDefault(c=>c!=null&&c.CardId=="CARD_WISADEL_APRIL");
            var faces=card?.ComicImpactFaces;
            if(faces==null||faces.Count!=6||faces.Any(face=>face==null))throw new InvalidDataException("The approved April skin needs its six existing GIF expression sprites.");
            if(!legacyComicFaceIds.TryGetValue(hit,out int faceIndex))legacyComicFaceIds[hit]=faceIndex=legacyComicFaceSequence++%faces.Count;
            int before=legacyFxUsed;
            LegacyDraw(faces[faceIndex],legacyComicMaterial,center,hit.To,(.84f+(float)age*.56f)*scale,52);
            if(legacyFxUsed>before)legacyFxViews[legacyFxUsed-1].Renderer.color=new Color(1,1,1,alpha);
        }

        void PresentLegacyOwner(FormationFighter actor)
        {
            if(actor.ArtId=="WISADEL_APRIL"){PresentOriginalWisadelOwner(actor);return;}
            double now=Model.Clock;float scale=LegacyScale(actor);string id=actor.ArtId;bool returning=actor.NativePhaseReturningUntil>now;
            double ambientAge=(Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now)*LegacyVisualRate;
            double phaseAge=(now-actor.NativePhaseChangedAt)*LegacyVisualRate;
            if(id=="TALULAH_TRIAL")
            {
                if(actor.Alive)
                {
                    LegacyTalulah(actor.NativePhase>0?"ambient-B-foot":"ambient-A-foot",ambientAge,actor.Position,actor,-4,true);
                    if(actor.IsMoving)LegacyTalulah(actor.NativePhase>0?"movement-B-foot":"movement-A-foot",ambientAge,actor.Position,actor,-3,true);
                    if(actor.NativePhase>0){LegacyTalulah("ambient-B-head",ambientAge,actor.Position,actor,7,true,FormationDisplayPoint(actor));LegacyTalulah("ambient-B-wing",ambientAge,actor.Position,actor,-5,true,FormationDisplayPoint(actor));}
                }
                if(returning)LegacyTalulah("first_transition_start",phaseAge,actor.Position,actor,6,position:FormationDisplayPoint(actor));
                else if(actor.NativePhase>0)LegacyTalulah("first_transition_end",phaseAge,actor.Position,actor,6,position:FormationDisplayPoint(actor));
            }
            if(id=="M14"&&(returning||actor.NativePhase>0))
                foreach(string key in new[]{"firstar_buff_01_start","firstar_buff_02_start","firstar_buff_03_start"})LegacyOriginal(key,phaseAge,actor.Position,24,flat:true,scale:scale);
            if(id=="M20")
            {
                if(returning){LegacyPatriot("buff_02_start",phaseAge,actor.Position,actor);LegacyPatriot("buff_02_start_02",phaseAge-.2,actor.Position,actor);}
                else if(actor.NativePhase>0)
                    foreach(string key in new[]{"buff_04_start","buff_04_start_02","buff_04_start_03","buff_04_start_04"})LegacyPatriot(key,phaseAge,actor.Position,actor);
                if(!actor.Alive&&!returning)LegacyPatriot("die_01_start",(now-actor.LastDeathAt)*LegacyVisualRate,actor.Position,actor);
            }
            if(id=="M32"&&returning)
            {
                foreach(string key in new[]{"reborn_start_01","reborn_loop_01","reborn_loop_02"})LegacyPremult(MandragoraVfxArt.Load(key).At(phaseAge,key.Contains("loop")),actor.Position,.95f*scale,12);
                LegacyPremult(MandragoraVfxArt.Load("reborn_end_01").At(phaseAge-4.5),actor.Position,.95f*scale,12);
                LegacyPremult(MandragoraVfxArt.Load("reborn_end_02").At(phaseAge-4.5),actor.Position,.95f*scale,13);
            }
            if(id=="M30"&&returning)
            {LegacyOriginal("enemy_duskls_revive_2",phaseAge,actor.Position,22,flat:true,scale:scale);LegacyOriginal("enemy_duskls_idle_2",phaseAge,actor.Position,20,true,true,scale);LegacyOriginal("enemy_duskls_revive_3",phaseAge,actor.Position,23,flat:true,scale:scale);LegacyOriginal("enemy_duskls_revive_4",phaseAge,actor.Position,24,flat:true,scale:scale);}
            if(id=="M31"&&!actor.Alive&&!returning)LegacyOriginal("enemy_duspfr_2_die_01",(now-actor.LastDeathAt)*LegacyVisualRate,actor.Position,30,flat:true,scale:scale);
            if(id=="M33")
            {
                double reflect=(now-Model.LegacyReflectAt(actor))*LegacyVisualRate;
                LegacyOriginal("enemy_dsurch_01_start",reflect,actor.Position,24,flat:true,scale:scale);
                LegacyOriginal("enemy_dsurch_01_start_02",reflect,actor.Position,25,flat:true,scale:scale);
            }
            if(id=="M34"&&actor.Alive&&Model.LegacyAwake(actor))LegacyOriginal("enemy_dslntc_buff_01",now*LegacyVisualRate,actor.Position,22,true,true,scale);
            if(id=="M28")
            {
                float previous=legacyPreviousShield.TryGetValue(actor.Id,out var old)?old:actor.Shield;
                if(previous>0&&actor.Shield<=0)legacyShieldBroken[actor.Id]=now;
                if(actor.Shield>previous+.001f)legacyShieldRefreshed[actor.Id]=now;
                legacyPreviousShield[actor.Id]=actor.Shield;
                if(actor.Alive&&actor.Shield>0)LegacyMudrockBoth("shield1",now*LegacyVisualRate,actor.Position,scale,true,actor.Facing.x<0,FormationDisplayPoint(actor));
                if(legacyShieldBroken.TryGetValue(actor.Id,out var broken))LegacyMudrockBoth("shieldBreak",(now-broken)*LegacyVisualRate,actor.Position,scale,flip:actor.Facing.x<0,ownerPoint:FormationDisplayPoint(actor));
                if(legacyShieldRefreshed.TryGetValue(actor.Id,out var refreshed))LegacyMudrock("shieldRefresh_over",(now-refreshed)*LegacyVisualRate,actor.Position,scale,12,false,actor.Facing.x<0,FormationDisplayPoint(actor));
            }
            double healUntil=Model.LegacyBuffUntil(actor,"heal_received");
            if(actor.Alive&&healUntil>0)
            {
                bool live=healUntil>now;double age=(now-(live?healUntil-4:healUntil))*LegacyVisualRate;
                LegacyPremult(MephistoVfxArt.Load(live?"buff_01_start":"buff_01_end").At(age,live),actor.Position,.779296875f*scale,20,point:FormationDisplayPoint(actor)+battleCamera.transform.up*.8f*scale);
            }
            if(actor.Alive&&Model.LegacyMandragoraShield(actor))
            {
                LegacyPremult(MandragoraVfxArt.Load("c1_skill_02_buff_01").At(now*LegacyVisualRate,true),actor.Position,.95f*scale,-4,point:FormationDisplayPoint(actor));
            }
        }

        internal void PresentFormationLegacyEffects()
        {
            if(Model==null||actorRoot==null||battleCamera==null)return;
            if(legacyObservedModel!=Model||Model.Clock<legacyLastClock)
            {legacyObservedModel=Model;legacyPreviousShield.Clear();legacyShieldBroken.Clear();legacyShieldRefreshed.Clear();legacyFlightOrigins.Clear();legacyComicFaceIds.Clear();legacyComicFaceSequence=0;legacySeaBorn.Clear();ResetFormationSynergyEffects();}
            legacyLastClock=Model.Clock;
            EnsureLegacyMaterials();legacyFxUsed=0;legacyComicUsed=0;
            foreach(var action in Model.PresentationActions)PresentLegacyAction(action);
            foreach(var actor in Model.Units.Where(u=>u.Enabled))PresentLegacyOwner(actor);
            foreach(var hit in Model.Hits)PresentLegacyHit(hit);
            foreach(var defender in Model.Defenders)
            {
                double at=Model.LegacyNerveBurstAt(defender);
                if(at>=0)LegacyOriginal("common/common_nerve_injury_01_hit",(Model.Clock-at)*LegacyVisualRate,defender.Position,38);
            }
            // Fire/snow domains are real v0.7 state, not guessed from damage counters.
            formationTalulahRain?.BeginFrame();
            foreach(var zone in Model.LegacyZones())
            {
                var source=Model.Units.FirstOrDefault(u=>u.Id==zone.SourceId);if(source==null)continue;
                double age=(Model.Clock-zone.At)*LegacyVisualRate;
                if(source.ArtId=="TALULAH_TRIAL"&&zone.Kind=="burn")
                {
                    if (Campaign.TalulahCost == 5)
                    {
                        PresentFormationTalulahRain(zone);
                        continue;
                    }
                    LegacyTalulah("target-burn-loop",age,zone.Center,source,-2,true);
                    if(Campaign.TalulahCost>=4)
                    {
                        // Original energy-stream sample: repeat emissions only during
                        // the actual burn zone, oriented to the actor's facing.
                        var stream=BossRosterSupplementArt.Talulah("original-energy-stream");
                        double streamAge=age%stream.Duration;
                        int rows=Mathf.FloorToInt(zone.Radius);
                        for(int row=-rows;row<=rows;row++)
                        {
                            float y=zone.Center.y+row;
                            if(y<0||y>FormationBattleModel.Height-1)continue;
                            float span=Mathf.Sqrt(Mathf.Max(0,zone.Radius*zone.Radius-row*row));
                            var cell=new Vector2(Mathf.Lerp(zone.Center.x-span,zone.Center.x+span,(float)(streamAge/stream.Duration)),y);
                            if(cell.x>FormationBattleModel.Width-.5f)continue;
                            LegacyTalulah("original-energy-stream",streamAge,cell,source,20);
                        }
                    }
                }
                else if(source.ArtId=="M14"&&zone.Kind=="snow")LegacyOriginal("firstar_skill_01_range",age,zone.Center,24,flat:true,scale:LegacyScale(source));
            }
            formationTalulahRain?.EndFrame();
            PresentFormationSynergyEffects();
            for(int i=legacyFxUsed;i<legacyFxViews.Count;i++)legacyFxViews[i].Renderer.enabled=false;
            for(int i=legacyComicUsed;i<legacyComicBursts.Count;i++)legacyComicBursts[i].Ink.enabled=legacyComicBursts[i].Light.enabled=false;
            foreach(var old in legacyComicFaceIds.Keys.Where(h=>Model.Clock-h.At>.55/LegacyVisualRate+.1).ToArray())legacyComicFaceIds.Remove(old);
            PresentLegacySea();
            foreach(string key in legacyPatriotPages.Where(p=>Time.frameCount-p.Value.LastUse>8).Select(p=>p.Key).ToArray())
            {
                var page=legacyPatriotPages[key];foreach(var view in legacyFxViews)if(view.Renderer.sprite==page.Sprite)view.Renderer.sprite=null;
                Destroy(page.Sprite);Resources.UnloadAsset(page.Texture);legacyPatriotPages.Remove(key);
            }
        }

        void PresentLegacySea()
        {
            bool active=Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting;
            int used=0;
            if(active&&Model.SeaCells.Count>0)
            {
                if(legacySeaMaterial==null)
                {
                    legacySeaMaterial=new Material(Resources.Load<Shader>("MarineTide")){name="Formation / original Nethersea brand"};
                    legacySeaMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_01"));
                    legacySeaMaterial.SetTexture("_DetailTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_04"));
                    legacySeaMaterial.SetTexture("_VeinTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_03"));
                    legacySeaMaterial.SetTexture("_FlowTex",Resources.Load<Texture2D>("MarineTide/flow_43_2"));
                    legacySeaMesh=new Mesh{name="Formation original sea surface cell"};legacySeaMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
                    legacySeaMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};legacySeaMesh.triangles=new[]{0,2,1,0,3,2};legacySeaMesh.RecalculateBounds();
                }
                var cells=new HashSet<Vector2Int>(Model.SeaCells.Select(Vector2Int.RoundToInt));
                foreach(var cell in cells)
                {
                    if(!FormationPointVisibleInView(cell))continue;
                    if(used==legacySeaTiles.Count)
                    {
                        var renderer=new GameObject("Formation original Nethersea cell",typeof(MeshRenderer),typeof(MeshFilter)).GetComponent<MeshRenderer>();
                        renderer.transform.SetParent(actorRoot,false);renderer.sharedMaterial=legacySeaMaterial;renderer.GetComponent<MeshFilter>().sharedMesh=legacySeaMesh;
                        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;legacySeaTiles.Add(renderer);
                    }
                    var tile=legacySeaTiles[used++];tile.enabled=true;tile.transform.position=ActorWorldPoint(cell)+Vector3.up*.025f;
                    if(!legacySeaBorn.TryGetValue(cell,out var born))legacySeaBorn[cell]=born=Model.Clock;
                    var properties=new MaterialPropertyBlock();float age=(float)((Model.Clock-born)*LegacyVisualRate);
                    properties.SetFloat("_MarineTime",(float)(Model.Clock*LegacyVisualRate));properties.SetFloat("_Growth",Mathf.SmoothStep(0,1,age/.7f));
                    properties.SetFloat("_Active",1);properties.SetFloat("_Highlight",0);properties.SetFloat("_Seed",0);properties.SetFloat("_BornAge",age);
                    properties.SetVector("_Edges",new Vector4(cells.Contains(cell+Vector2Int.left)?0:1,cells.Contains(cell+Vector2Int.right)?0:1,cells.Contains(cell+Vector2Int.down)?0:1,cells.Contains(cell+Vector2Int.up)?0:1));
                    tile.SetPropertyBlock(properties);
                }
            }
            for(int i=used;i<legacySeaTiles.Count;i++)legacySeaTiles[i].enabled=false;
        }
        internal void DisposeFormationLegacyEffects()
        {
            formationTalulahRain?.Dispose();
            formationTalulahRain = null;
            ResetFormationSynergyEffects();
            foreach(var view in legacyFxViews)if(view.Renderer!=null){view.Renderer.enabled=false;Destroy(view.Renderer.gameObject);}legacyFxViews.Clear();
            foreach(var page in legacyPatriotPages.Values){Destroy(page.Sprite);Resources.UnloadAsset(page.Texture);}legacyPatriotPages.Clear();
            foreach(var clip in legacyTalulahFrames.Values)foreach(var sprite in clip.Frames)Destroy(sprite);legacyTalulahFrames.Clear();
            foreach(var tile in legacySeaTiles)if(tile!=null){tile.enabled=false;Destroy(tile.gameObject);}legacySeaTiles.Clear();
            foreach(var burst in legacyComicBursts)
            {if(burst.Ink!=null){burst.Ink.enabled=false;Destroy(burst.Ink.gameObject);}if(burst.Light!=null){burst.Light.enabled=false;Destroy(burst.Light.gameObject);}}
            legacyComicBursts.Clear();legacyComicFaceIds.Clear();legacyComicFaceSequence=0;legacyComicUsed=0;
            if(legacyHdrMaterial!=null)Destroy(legacyHdrMaterial);if(legacyPremultMaterial!=null)Destroy(legacyPremultMaterial);if(legacyPatriotMaterial!=null)Destroy(legacyPatriotMaterial);
            if(legacySeaMaterial!=null)Destroy(legacySeaMaterial);if(legacySeaMesh!=null)Destroy(legacySeaMesh);
            if(legacyComicMaterial!=null)Destroy(legacyComicMaterial);legacyComicMaterial=null;
            legacyHdrMaterial=legacyPremultMaterial=legacyPatriotMaterial=legacySeaMaterial=null;legacySeaMesh=null;
            legacyPreviousShield.Clear();legacyShieldBroken.Clear();legacyShieldRefreshed.Clear();legacyFlightOrigins.Clear();legacySeaBorn.Clear();legacyObservedModel=null;legacyLastClock=-1;
        }
    }

    public sealed partial class FormationBattleModel
    {
        internal sealed class LegacyZoneSnapshot {internal int SourceId;internal string Kind;internal Vector2 Center;internal double At,Until;internal float Radius;}
        internal IEnumerable<LegacyZoneSnapshot> LegacyZones()
        {foreach(var zone in zones)if(zone.Until>Clock)yield return new LegacyZoneSnapshot{SourceId=zone.Source.Id,Kind=zone.Kind,Center=zone.Center,At=zone.StartedAt,Until=zone.Until,Radius=zone.Radius};}
        internal double LegacyReflectAt(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)?state.LastReflect:double.NegativeInfinity;
        internal bool LegacyAwake(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Stacks.TryGetValue("awake",out var value)&&value>0;
        internal double LegacyBuffUntil(FormationActor actor,string key)=>combat.TryGetValue(actor.Id,out var state)&&state.Buffs.TryGetValue(key,out var buff)?buff.Until:double.NegativeInfinity;
        internal double LegacyNerveBurstAt(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Buffs.TryGetValue("nerve_lock",out var buff)?buff.Until-4:double.NegativeInfinity;
        internal bool LegacyMandragoraShield(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Shields.Any(s=>s.Until>Clock&&s.Amount>0&&s.Key=="mandragora");
    }
}
