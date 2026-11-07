using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    /// <summary>Live glass/portrait motion with a pause-aware cinematic clock, independent of combat speed.</summary>
    public sealed partial class GuerrillaOrderView : MonoBehaviour
    {
        public const float FadeInDuration=.16f, HoldDuration=.74f, FadeOutDuration=1.70f;
        public const float Duration=GuerrillaOrderGeometry.Duration, SoundCueAt=.90f;
        public const int SlotCount=9;
        readonly string[] slots=new string[SlotCount];
        readonly Sprite[] extensions=new Sprite[2];
        readonly Vector2[] offsets=new Vector2[SlotCount];
        readonly List<Vector3> geometry=new List<Vector3>(24000);
        ReadOnlyCollection<string> visibleSlots;
        ReadOnlyCollection<Vector2> visibleOffsets;
        ReadOnlyCollection<Vector3> visibleGeometry;
        ManualBattleController owner; ManualBattle observed; int run=-1;
        float elapsed;
        GuerrillaOrderVolley volley;
        bool active,pausedSound,cueTriggered,pausedSlashSound,slashCueTriggered,dirty=true;
        float builtAt=float.NaN;
        int builtStrikes=-1;bool builtCancelled;
        Material material; Texture2D approved; Font captionFont;
        RenderTexture frame,accumulation;
        CommandBuffer renderCommands;
        AudioClip followup,slashTrack; AudioSource sound,slashSound;
        GuerrillaOrderScreenEffect screenEffect;
        Batch behind,panes,front,caption;
        readonly Batch[] extras=new Batch[2];
        int paneCount,portraitCount,fragmentCount,outerFragmentCount;
        int impactTargetCount;
        float displayedDamage;
        static readonly Vector2 C=GuerrillaOrderGeometry.Center;
        static float S(float value)=>GuerrillaOrderGeometry.Smooth(value);
        static float Out(float value)=>GuerrillaOrderGeometry.Out(value);
        static float Rand(int seed)=>GuerrillaOrderGeometry.LightRandom(seed);
        static float Backdrop(float time)=>S(time/.16f)*(1-S((time-.91f)/.28f));
        static Color Rgba(float r,float g,float b,float a=1)=>new Color(r/255,g/255,b/255,a);

        sealed class Batch
        {
            internal readonly Mesh Mesh;
            internal readonly List<Vector3> Vertices=new List<Vector3>(4096);
            readonly List<Color> colors=new List<Color>(4096);
            readonly List<Vector2> uvs=new List<Vector2>(4096),flags=new List<Vector2>(4096);
            readonly List<int> indices=new List<int>(6144);
            readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
            internal Texture Texture;
            internal bool AlphaOnly;
            internal Batch(string name)
            {Mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};Mesh.MarkDynamic();}
            internal void Clear(){Vertices.Clear();colors.Clear();uvs.Clear();flags.Clear();indices.Clear();}
            internal void Polygon(Vector2[] points,Color[] tint,Vector2[] uv=null)
            {
                int start=Vertices.Count;
                for(int i=0;i<points.Length;i++)
                {Vertices.Add(new Vector3(points[i].x,points[i].y,0));colors.Add(tint[i]);uvs.Add(uv==null?Vector2.zero:uv[i]);flags.Add(new Vector2(uv==null?0:1,AlphaOnly?1:0));}
                for(int i=1;i<points.Length-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);}
            }
            internal void Fill(Vector2[] p,Color color,Vector2[] uv=null)
            {var colors=new Color[p.Length];for(int i=0;i<colors.Length;i++)colors[i]=color;Polygon(p,colors,uv);}
            internal void Rect(Rect r,Color color)
            {Fill(new[]{new Vector2(r.x,r.y),new Vector2(r.xMax,r.y),new Vector2(r.xMax,r.yMax),new Vector2(r.x,r.yMax)},color);}
            internal void Upload()
            {Mesh.Clear();if(Vertices.Count==0)return;Mesh.SetVertices(Vertices);Mesh.SetColors(colors);Mesh.SetUVs(0,uvs);Mesh.SetUVs(1,flags);Mesh.SetTriangles(indices,0);Mesh.RecalculateBounds();}
            internal void Draw(CommandBuffer commands,Material material)
            {
                if(Vertices.Count==0)return;
                properties.SetTexture("_MainTex",Texture==null?Texture2D.whiteTexture:Texture);
                commands.DrawMesh(Mesh,Matrix4x4.identity,material,0,0,properties);
            }
            internal void Dispose(){if(Mesh!=null)UnityEngine.Object.Destroy(Mesh);}
        }

        public IReadOnlyList<string> SlotMembers{get{CheckIdentity();return visibleSlots??(visibleSlots=Array.AsReadOnly(slots));}}
        public int PlayCount{get;private set;}
        public int SoundPlayCount{get;private set;}
        public int SlashSoundPlayCount{get;private set;}
        public bool Playing{get{CheckIdentity();return active&&CanPresent&&CurrentAge<Duration;}}
        public float Age{get{CheckIdentity();return active?CurrentAge:0;}}
        // Compatibility envelope: darkening only. Character panes have their own geometry and individual alpha.
        public float Opacity=>Playing?Backdrop(Age):0;
        // Compatibility estimate only. Terminal waiting checks Playing directly, without mixing time domains.
        public double PendingUntil{get{CheckIdentity();return Playing?owner.PresentationClock+Remaining:double.NegativeInfinity;}}
        public float Remaining=>Playing?Mathf.Max(0,Duration-CurrentAge):0;
        public bool SoundPaused{get{CheckIdentity();return Playing&&sound!=null&&pausedSound;}}
        public bool SoundPlaying{get{CheckIdentity();return Playing&&!pausedSound&&sound!=null&&sound.isPlaying;}}
        public bool SlashSoundPaused{get{CheckIdentity();return Playing&&slashSound!=null&&pausedSlashSound;}}
        public bool SlashSoundPlaying{get{CheckIdentity();return Playing&&!pausedSlashSound&&slashSound!=null&&slashSound.isPlaying;}}
        public bool AssetsReady=>approved!=null&&material!=null&&material.shader.isSupported&&followup!=null&&slashTrack!=null;
        public Rect ScreenBounds{get{var v=TacticalViewport.Current;return new Rect(v.Offset.x,v.Offset.y,1280*v.Scale,720*v.Scale);}}
        public int VisiblePaneCount{get{BuildGeometry();return paneCount;}}
        public int VisiblePortraitCount{get{BuildGeometry();return portraitCount;}}
        public int VisibleFragmentCount{get{BuildGeometry();return fragmentCount;}}
        public int VisibleOuterFragmentCount{get{BuildGeometry();return outerFragmentCount;}}
        public int VisibleImpactTargetCount{get{BuildGeometry();return impactTargetCount;}}
        public float DisplayedDamage{get{BuildGeometry();return displayedDamage;}}
        public int DisplayedStrikeCount{get{BuildGeometry();return displayedStrikeCount;}}
        public float ChargeProgress=>Playing&&CanCharge(Age)?S((Age-ChargeStart)/(FinalStrikeAt-ChargeStart)):0;
        public GuerrillaOrderScreenEffect ScreenEffect=>screenEffect;
        public IReadOnlyList<Vector3> GeometryVertices{get{BuildGeometry();return visibleGeometry??(visibleGeometry=geometry.AsReadOnly());}}
        public IReadOnlyList<Vector2> PaneOffsets{get{BuildGeometry();return visibleOffsets??(visibleOffsets=Array.AsReadOnly(offsets));}}
        float CurrentAge=>elapsed;
        bool CanPresent=>isActiveAndEnabled&&owner!=null&&owner.isActiveAndEnabled&&owner.Battle!=null&&!owner.TestSelectionOpen&&!owner.ArtLoading;

        public void Initialize(ManualBattleController controller,Font font)
        {
            if(controller==null)throw new ArgumentNullException(nameof(controller));
            Clear();owner=controller;Font.textureRebuilt-=OnFontTextureRebuilt;captionFont=font;Font.textureRebuilt+=OnFontTextureRebuilt;
            observed=owner.Battle;run=observed?.RunId??-1;PrepareMaterial();
        }
        public void Play(IEnumerable<string> members,IReadOnlyDictionary<string,Sprite> extraPortraits=null,IReadOnlyList<string> extensionSlotIds=null)
        {
            if(members==null)throw new ArgumentNullException(nameof(members));
            CheckIdentity();if(owner==null||owner.Battle==null)return;
            Clear();var unique=new HashSet<string>(members.Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
            for(int i=0;i<7;i++){string id=GuerrillaOrderGeometry.Definition(i).CardId;if(unique.Remove(id))slots[i]=id;}
            int extension=0;
            var assigned=extensionSlotIds==null?unique.OrderBy(id=>id,StringComparer.Ordinal):extensionSlotIds.Where(id=>!string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal);
            foreach(string id in assigned)
            {
                if(extension==2)break;
                // A selected transfer kept in reserve leaves its own glass slot empty.
                if(unique.Contains(id))
                {
                    slots[7+extension]=id;
                    if(extraPortraits!=null&&extraPortraits.TryGetValue(id,out var image))extensions[extension]=image;
                }
                extension++;
            }
            observed=owner.Battle;run=observed.RunId;elapsed=0;active=true;PlayCount++;dirty=true;PrepareMaterial();
        }
        public void BindVolley(GuerrillaOrderVolley committedVolley)
        {volley=committedVolley;if(volley!=null)elapsed=Mathf.Clamp(volley.Age,0,Duration);dirty=true;}
        public void AdvancePresentation(float delta)
        {
            CheckIdentity();
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            if(active&&CanPresent&&!owner.Battle.IsPaused)
            {
                // Automatic orders may commit inside a simulation substep. The model already
                // owns their elapsed presentation time, so never add the whole frame twice.
                elapsed=Mathf.Min(Duration,volley!=null&&owner.Battle.Phase==BattlePhase.Running?volley.Age:elapsed+delta);
            }
            UpdateScreenEffect();
        }
        /// <summary>The production mesh shader. Use RenderTo for the complete multi-layer composition.</summary>
        public Material PrepareMaterial()
        {
            if(material!=null)return material;
            approved=Resources.Load<Texture2D>("GuerrillaOrder/approved-cut-in");
            var shader=Resources.Load<Shader>("GuerrillaOrder/GuerrillaOrder");
            followup=Resources.Load<AudioClip>("GuerrillaOrder/followup-original");
            slashTrack=Resources.Load<AudioClip>("GuerrillaOrder/five-slash-charge");
            if(approved==null||shader==null||followup==null||slashTrack==null)throw new InvalidOperationException("Missing approved guerrilla illustration, mesh shader or follow-up/strike sounds.");
            material=new Material(shader){name="Guerrilla V2 / premultiplied mesh composition",hideFlags=HideFlags.DontSave,mainTexture=approved};
            behind=new Batch("Guerrilla V2 / backdrop and attraction");panes=new Batch("Guerrilla V2 / native portraits and glass");
            front=new Batch("Guerrilla V2 / crystal beams and shards");caption=new Batch("Guerrilla V2 / native caption");
            caption.AlphaOnly=true;
            for(int i=0;i<2;i++)extras[i]=new Batch("Guerrilla V2 / future-member glass "+i);
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.loop=false;sound.spatialBlend=0;sound.volume=.55f;sound.clip=followup;
            slashSound=gameObject.AddComponent<AudioSource>();slashSound.playOnAwake=false;slashSound.loop=false;slashSound.spatialBlend=0;slashSound.volume=1;slashSound.clip=slashTrack;
            return material;
        }
        void CheckIdentity()
        {
            if(owner==null)return;
            var battle=owner.Battle;
            if(!ReferenceEquals(observed,battle)||run!=(battle?.RunId??-1))
            {Clear();observed=battle;run=battle?.RunId??-1;}
        }
        void Clear()
        {
            active=false;elapsed=0;volley=null;pausedSound=cueTriggered=pausedSlashSound=slashCueTriggered=false;dirty=true;builtAt=float.NaN;builtStrikes=-1;
            Array.Clear(slots,0,slots.Length);Array.Clear(extensions,0,extensions.Length);Array.Clear(offsets,0,offsets.Length);
            paneCount=portraitCount=fragmentCount=outerFragmentCount=impactTargetCount=displayedStrikeCount=0;displayedDamage=0;geometry.Clear();
            if(sound!=null)sound.Stop();if(slashSound!=null)slashSound.Stop();screenEffect?.ClearPresentation();
        }
        void LateUpdate()
        {
            CheckIdentity();UpdateSound();UpdateScreenEffect();
            if(!Playing)return;
            Rect bounds=ScreenBounds;
            EnsureTexture(ref frame,Mathf.Max(1,Mathf.RoundToInt(bounds.width)),Mathf.Max(1,Mathf.RoundToInt(bounds.height)),"Guerrilla V2 / foreground frame");
            RenderTo(frame);
        }
        void OnFontTextureRebuilt(Font rebuilt){if(rebuilt==captionFont)dirty=true;}
        void UpdateSound()
        {
            if(!Playing)
            {
                if(sound!=null)sound.Stop();if(slashSound!=null)slashSound.Stop();pausedSound=pausedSlashSound=false;return;
            }
            bool firstPermitted=volley==null||volley.CompletedStrikes>0;
            if(UpdateTrack(sound,followup,Age-SoundCueAt,firstPermitted?followup.length:0,ref cueTriggered,ref pausedSound))SoundPlayCount++;
            float until=slashTrack.length;
            if(volley!=null&&volley.Cancelled)
            {
                // The track has silent gaps, four short slashes and one charge/final window.
                // A cancelled fourth strike may keep its tail, never start a fresh charge.
                float[] allowedEnds={0,1.09f,1.39f,1.69f,ChargeStart,3.38f};
                until=Mathf.Min(until,allowedEnds[Mathf.Clamp(volley.CompletedStrikes,0,5)]);
            }
            if(UpdateTrack(slashSound,slashTrack,Age,until,ref slashCueTriggered,ref pausedSlashSound))SlashSoundPlayCount++;
        }
        bool UpdateTrack(AudioSource source,AudioClip clip,float relative,float until,ref bool triggered,ref bool paused)
        {
            if(source==null||clip==null)return false;
            source.mute=owner.Muted;source.pitch=Mathf.Clamp(owner.GuerrillaPresentationRate,.03f,1);
            if(relative<0)return false;
            if(relative>=until){source.Stop();paused=false;return false;}
            bool started=false;
            if(!triggered)
            {
                triggered=true;source.timeSamples=Mathf.Clamp(Mathf.RoundToInt(relative*clip.frequency),0,clip.samples-1);
                source.Play();started=true;
            }
            if(owner.Battle.IsPaused)
            {if(!paused&&source.isPlaying){source.Pause();paused=true;}return started;}
            if(paused){source.UnPause();paused=false;}
            int sample=Mathf.Clamp(Mathf.RoundToInt(relative*clip.frequency),0,clip.samples-1);
            if(source.isPlaying&&Math.Abs(source.timeSamples-sample)>clip.frequency*.045f)source.timeSamples=sample;
            return started;
        }
        void UpdateScreenEffect()
        {
            if(!Playing){screenEffect?.ClearPresentation();return;}
            var camera=Camera.main;if(camera==null)return;
            if(screenEffect==null||screenEffect.gameObject!=camera.gameObject)
            {
                if(screenEffect!=null)Destroy(screenEffect);
                screenEffect=camera.GetComponent<GuerrillaOrderScreenEffect>()??camera.gameObject.AddComponent<GuerrillaOrderScreenEffect>();
            }
            screenEffect.Present(Age,volley,CanCharge(Age)?ChargeProgress:0);
        }

        void BuildGeometry()
        {
            CheckIdentity();PrepareMaterial();bool show=Playing;float time=show?Age:-1;
            int strikes=volley?.CompletedStrikes??0;bool cancelled=volley?.Cancelled??false;
            if(!dirty&&builtAt==time&&builtStrikes==strikes&&builtCancelled==cancelled)return;
            dirty=false;builtAt=time;builtStrikes=strikes;builtCancelled=cancelled;
            behind.Clear();panes.Clear();front.Clear();caption.Clear();foreach(var b in extras)b.Clear();
            paneCount=portraitCount=fragmentCount=outerFragmentCount=impactTargetCount=displayedStrikeCount=0;displayedDamage=0;geometry.Clear();Array.Clear(offsets,0,offsets.Length);
            behind.Texture=panes.Texture=front.Texture=approved;
            if(show)
            {
                Scenery(time);ChargeBackdrop(time);Attraction(time);Portraits(time);Crystal(time);BurstAndTail(time);Caption(time);BattlefieldImpact(time);
            }
            foreach(var b in Batches()){b.Upload();geometry.AddRange(b.Vertices);}
        }
        IEnumerable<Batch> Batches()
        {yield return behind;yield return panes;yield return extras[0];yield return extras[1];yield return front;yield return caption;}
        void Portraits(float time)
        {
            // The approved five-strike draft reveals the real targets during the first slash.
            // Small outer shards retain the long tail; large portrait fragments clear sooner.
            if(time>=1.19f)return;
            for(int slot=0;slot<SlotCount;slot++)
            {
                var entry=GuerrillaOrderGeometry.Entrance(slot,time);offsets[slot]=entry.Offset;if(!entry.Visible)continue;
                bool picture=slots[slot]!=null&&(slot<7||extensions[slot-7]!=null);
                Batch batch=slot>=7&&picture?extras[slot-7]:panes;
                Texture image=approved;Rect source=GuerrillaOrderGeometry.Definition(slot).Source;Vector2 anchor=GuerrillaOrderGeometry.Definition(slot).Anchor;
                if(slot>=7&&picture)
                {
                    var sprite=extensions[slot-7];image=sprite.texture;
                    source=new Rect(sprite.rect.x,image.height-sprite.rect.yMax,sprite.rect.width,sprite.rect.height);
                    anchor=source.position+new Vector2(source.width*.5f,source.height*.35f);
                }
                batch.Texture=image;
                float breakAt=GuerrillaOrderGeometry.BreakAt(slot);
                var mapping=new GuerrillaOrderGeometry.Mapping(slot,picture?source:new Rect(0,0,1,1),picture?anchor:new Vector2(.5f,.5f),Mathf.Min(time,breakAt));
                if(time<breakAt)
                {PaintPane(batch,slot,GuerrillaOrderGeometry.Polygon(slot),entry,picture,source,image,mapping);paneCount++;if(picture)portraitCount++;}
                else
                {
                    bool portraitVisible=false;
                    for(int i=0;i<12;i++)
                    {
                        var pose=GuerrillaOrderGeometry.Fragment(slot,i,time);if(!pose.Visible)continue;
                        PaintPane(batch,slot,GuerrillaOrderGeometry.Triangle(slot,i),pose,picture,source,image,mapping);fragmentCount++;portraitVisible|=picture;
                    }
                    if(portraitVisible)portraitCount++;
                }
            }
        }
        void PaintPane(Batch b,int slot,Vector2[] original,GuerrillaOrderGeometry.Pose pose,bool picture,Rect source,Texture image,GuerrillaOrderGeometry.Mapping map)
        {
            // Keep the drifting glass tail, but reveal the actual battlefield at the strike.
            float alpha=pose.Alpha*(1-S((CurrentAge-.90f)/.29f));
            var points=new Vector2[original.Length];var colors=new Color[points.Length];var uv=picture?new Vector2[points.Length]:null;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=pose.Apply(original[i]);float radius=new Vector2((original[i].x-C.x)/(1920*.49f),(original[i].y-C.y)/(1080*.48f)).magnitude;
                colors[i]=Color.Lerp(Rgba(15,20,29,.88f),Rgba(8,14,24,.94f),radius);colors[i].a*=alpha;
                if(picture)uv[i]=map.Uv(original[i],source,image);
            }
            b.Polygon(points,colors);
            if(picture)b.Fill(points,new Color(1,1,1,alpha),uv);
            Color edge=picture?Rgba(221,235,255,.72f):Rgba(105,134,165,.28f);
            edge.a*=alpha*(.55f+.45f*pose.Scale);
            for(int i=0;i<points.Length;i++)Line(b,points[i],points[(i+1)%points.Length],edge,(picture?1.8f:1)*pose.Scale,picture?8*pose.Scale:2*pose.Scale);
        }

        void Scenery(float t)
        {
            float amount=Backdrop(t);
            behind.Rect(new Rect(0,0,1920,1080),Rgba(3,9,20,amount*.96f));
            if(amount<=0)return;
            Glow(behind,C,1100,Rgba(26,48,79,amount*.23f));
            for(int i=0;i<105;i++)
            {
                float size=.5f+Rand(i+500)*1.7f;
                behind.Rect(new Rect(Rand(i+2)*1920,Rand(i+300)*1080,size,size),Rgba(136,194,232,amount*(.025f+Rand(i+800)*.14f)*(1-S((t-2.1f)/.5f))));
            }
        }
        void Attraction(float t)
        {
            if(t<0||t>.68f)return;float envelope=S(t/.10f)*(1-S((t-.46f)/.22f));
            for(int i=0;i<36;i++)
            {
                float a=Rand(i+61)*Mathf.PI*2,p=Mathf.Clamp01((t-Rand(i+100)*.15f)/(.22f+Rand(i+120)*.13f));
                float r=(1-Out(p))*(700+Rand(i+201)*650)+18,stretch=20+90*(1-p);
                Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a)*.66f),point=C+direction*r;
                Line(behind,point,point+direction*stretch,Rgba(143,216,255,envelope*(.15f+.6f*p)),.8f+Rand(i+300)*2,10);
            }
        }
        void Crystal(float t)
        {
            float build=S(t/.18f),breaking=S((t-.88f)/.7f),fade=1-S((t-.90f)/.29f);
            float impact=Mathf.Exp(-Mathf.Pow((t-.49f)/.045f,2)),burst=Mathf.Exp(-Mathf.Pow((t-.98f)/.10f,2));
            float radius=(8+23*build+impact*21+burst*22)*fade;
            Glow(front,C,180+impact*160+burst*320,Rgba(77,183,255,(.45f+impact*.5f+burst*.35f)*fade));
            for(int k=0;k<8;k++)
            {
                float angle=.2f+Mathf.Min(t,.9f)*.7f+breaking*1.1f+k*Mathf.PI/4;
                var shape=new[]{new Vector2(0,-radius*2.15f),new Vector2(radius*.36f,-radius*.24f),new Vector2(0,radius*.48f),new Vector2(-radius*.36f,-radius*.24f)};
                for(int j=0;j<shape.Length;j++)shape[j]=Rotate(shape[j],angle)+C;
                front.Fill(shape,k%2==1?Rgba(215,244,255,.8f*fade):Rgba(97,179,255,.75f*fade));
                for(int j=0;j<shape.Length;j++)Line(front,shape[j],shape[(j+1)%shape.Length],Rgba(238,252,255,.75f*fade),.9f,10);
            }
            float rays=(.35f+impact*.4f+burst*.45f)*fade;
            float[] angles={-.10f,.08f,1.15f,-.87f},lengths={270,160,85,190},widths={1.2f,.8f,.7f,1};
            for(int i=0;i<angles.Length;i++)
            {Vector2 d=new Vector2(Mathf.Cos(angles[i]),Mathf.Sin(angles[i]))*lengths[i]*(.3f+build);Line(front,C-d,C+d,Rgba(202,242,255,rays),widths[i],14);}
        }
        void BurstAndTail(float t)
        {
            float age=t-.9f;if(age<0||age>1.8f)return;
            float wave=Out(age/.38f),opacity=1-S(age/.8f);
            if(opacity>.001f)
            {
                Vector2 previous=Vector2.zero;
                for(int i=0;i<=80;i++)
                {
                    float a=i*Mathf.PI*2/80;
                    Vector2 point=C+Rotate(new Vector2(Mathf.Cos(a)*(55+wave*930),Mathf.Sin(a)*(20+wave*350)),-.16f);
                    if(i>0)Line(front,previous,point,Rgba(106,182,255,opacity*.42f),1.8f,4);previous=point;
                }
            }
            for(int i=0;i<92;i++)
            {
                float delay=Rand(i+901)*.13f,p=Mathf.Clamp01((age-delay)/(1.1f+Rand(i+2000)*.6f));if(p<=0||p>=1)continue;
                float a=Rand(i+1000)*Mathf.PI*2,distance=(50+Rand(i+1100)*1130)*Out(p),spin=(Rand(i+1300)-.5f)*6*p,size=(2+Rand(i+1200)*11)*(1-p);
                Vector2 origin=C+new Vector2(Mathf.Cos(a)*distance,Mathf.Sin(a)*distance*.65f+35*p*p);
                var triangle=new[]{new Vector2(size*2,0),new Vector2(-size,-size*.45f),new Vector2(-size*.5f,size*.65f)};
                for(int k=0;k<3;k++)triangle[k]=origin+Rotate(triangle[k],a+spin);
                front.Fill(triangle,i%3!=0?Rgba(196,234,255,Mathf.Pow(1-p,1.2f)*.65f):Rgba(76,156,232,Mathf.Pow(1-p,1.2f)*.65f));
                outerFragmentCount++;
            }
        }
        void Caption(float t)
        {
            float alpha=S((t-.43f)/.13f)*(1-S((t-.95f)/.25f));if(alpha<=0||captionFont==null)return;
            Text("游击队",28,FontStyle.Bold,new Vector2(C.x,C.y+108),Rgba(234,245,252,alpha));
            Text("协同进攻",15,FontStyle.Normal,new Vector2(C.x,C.y+137),Rgba(159,199,223,alpha));
            caption.Texture=captionFont.material.mainTexture;
        }
        static string DamageNumber(float damage)=>damage<10?damage.ToString("0.#"):Math.Round(damage,MidpointRounding.AwayFromZero).ToString("0");
        void Text(string value,int size,FontStyle style,Vector2 baseline,Color color)
        {
            if(captionFont==null)return;
            captionFont.RequestCharactersInTexture(value,size,style);
            float width=0;foreach(char ch in value)if(captionFont.GetCharacterInfo(ch,out var glyph,size,style))width+=glyph.advance;
            float x=baseline.x-width*.5f;
            foreach(char ch in value)
            {
                if(!captionFont.GetCharacterInfo(ch,out var g,size,style))continue;
                var p=new[]{new Vector2(x+g.minX,baseline.y-g.maxY),new Vector2(x+g.maxX,baseline.y-g.maxY),new Vector2(x+g.maxX,baseline.y-g.minY),new Vector2(x+g.minX,baseline.y-g.minY)};
                caption.Fill(p,color,new[]{g.uvTopLeft,g.uvTopRight,g.uvBottomRight,g.uvBottomLeft});x+=g.advance;
            }
        }
        static Vector2 Rotate(Vector2 v,float radians)
        {float c=Mathf.Cos(radians),s=Mathf.Sin(radians);return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);}
        static void Line(Batch batch,Vector2 a,Vector2 b,Color color,float width=1,float blur=0)
        {
            Vector2 direction=b-a;if(direction.sqrMagnitude<.000001f||color.a<=.00001f)return;
            if(blur>0)
            {
                Color soft=color;soft.a*=.09f;Line(batch,a,b,soft,width+blur*1.5f);
                soft=color;soft.a*=.16f;Line(batch,a,b,soft,width+blur*.65f);
            }
            Vector2 n=new Vector2(-direction.y,direction.x).normalized*Mathf.Max(.02f,width)*.5f;
            batch.Fill(new[]{a+n,b+n,b-n,a-n},color);
        }
        static void Glow(Batch batch,Vector2 center,float radius,Color tint)
        {
            if(radius<=0||tint.a<=0)return;
            float[] rings={0,.18f,.5f,1};float[] opacity={.65f,.33f,.09f,0};
            for(int ring=0;ring<3;ring++)for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                Vector2 da=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),db=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                var points=new[]{center+da*radius*rings[ring],center+da*radius*rings[ring+1],center+db*radius*rings[ring+1],center+db*radius*rings[ring]};
                Color inner=tint,outer=tint;inner.a*=opacity[ring];outer.a*=opacity[ring+1];batch.Polygon(points,new[]{inner,outer,outer,inner});
            }
        }

        /// <summary>Render the same complete live composition used by IMGUI into a caller-owned texture.</summary>
        public void RenderTo(RenderTexture target)
        {
            if(target==null)throw new ArgumentNullException(nameof(target));PrepareMaterial();UpdateSound();UpdateScreenEffect();BuildGeometry();
            if(!target.IsCreated())target.Create();
            RenderTexture previous=RenderTexture.active;
            try
            {
                if(renderCommands==null)renderCommands=new CommandBuffer{name="Guerrilla V2 / isolated screen composition"};
                renderCommands.Clear();
                if(!Playing)
                {
                    renderCommands.SetRenderTarget(target);renderCommands.SetViewport(new Rect(0,0,target.width,target.height));
                    renderCommands.DisableScissorRect();renderCommands.ClearRenderTarget(false,true,Color.clear);
                    Graphics.ExecuteCommandBuffer(renderCommands);return;
                }
                EnsureTexture(ref accumulation,target.width,target.height,"Guerrilla V2 / premultiplied layers");
                renderCommands.SetRenderTarget(accumulation);
                renderCommands.SetViewport(new Rect(0,0,target.width,target.height));
                renderCommands.DisableScissorRect();
                renderCommands.ClearRenderTarget(false,true,Color.clear);
                renderCommands.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.Ortho(0,1920,1080,0,-1,1));
                foreach(var b in Batches())b.Draw(renderCommands,material);
                // Resolve before IMGUI. Its viewport/clip state must never be used for the mesh pass.
                renderCommands.Blit(accumulation,target,material,1);
                Graphics.ExecuteCommandBuffer(renderCommands);
            }
            finally{RenderTexture.active=previous;}
        }
        void OnGUI()
        {
            // GUI.depth belongs to this behaviour. Restoring it at return undoes its foreground order.
            GUI.depth=-2000;
            if(Event.current.type!=EventType.Repaint||!Playing||frame==null)return;
            Rect bounds=ScreenBounds;Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;
            try{GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;GUI.DrawTexture(bounds,frame,ScaleMode.StretchToFill,true);}
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        static void EnsureTexture(ref RenderTexture texture,int width,int height,string name)
        {
            if(texture!=null&&texture.width==width&&texture.height==height)return;
            Release(ref texture);
            texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32){name=name,hideFlags=HideFlags.DontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.Create();
        }
        static void Release(ref RenderTexture texture)
        {if(texture==null)return;texture.Release();UnityEngine.Object.Destroy(texture);texture=null;}
        void OnDisable()=>Clear();
        void OnDestroy()
        {
            Clear();Font.textureRebuilt-=OnFontTextureRebuilt;Release(ref frame);Release(ref accumulation);
            renderCommands?.Release();renderCommands=null;
            if(material!=null)Destroy(material);if(sound!=null)Destroy(sound);if(slashSound!=null)Destroy(slashSound);if(screenEffect!=null)Destroy(screenEffect);
            behind?.Dispose();panes?.Dispose();front?.Dispose();caption?.Dispose();foreach(var b in extras)b?.Dispose();
        }
    }
}
