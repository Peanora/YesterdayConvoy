using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Live V2 glass/portrait motion, driven solely by the existing presentation clock.</summary>
    public sealed class GuerrillaOrderView : MonoBehaviour
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
        double started=double.NegativeInfinity;
        bool active,pausedSound,cueTriggered,dirty=true;
        float builtAt=float.NaN;
        Material material; Texture2D approved; Font captionFont;
        RenderTexture frame,accumulation;
        AudioClip followup; AudioSource sound;
        Batch behind,panes,front,caption;
        readonly Batch[] extras=new Batch[2];
        int paneCount,portraitCount,fragmentCount,outerFragmentCount;
        static readonly Vector2 C=GuerrillaOrderGeometry.Center;
        static float S(float value)=>GuerrillaOrderGeometry.Smooth(value);
        static float Out(float value)=>GuerrillaOrderGeometry.Out(value);
        static float Rand(int seed)=>GuerrillaOrderGeometry.LightRandom(seed);
        static Color Rgba(float r,float g,float b,float a=1)=>new Color(r/255,g/255,b/255,a);

        sealed class Batch
        {
            internal readonly Mesh Mesh;
            internal readonly List<Vector3> Vertices=new List<Vector3>(4096);
            readonly List<Color> colors=new List<Color>(4096);
            readonly List<Vector2> uvs=new List<Vector2>(4096),flags=new List<Vector2>(4096);
            readonly List<int> indices=new List<int>(6144);
            internal Texture Texture;
            internal Batch(string name)
            {Mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};Mesh.MarkDynamic();}
            internal void Clear(){Vertices.Clear();colors.Clear();uvs.Clear();flags.Clear();indices.Clear();}
            internal void Polygon(Vector2[] points,Color[] tint,Vector2[] uv=null)
            {
                int start=Vertices.Count;
                for(int i=0;i<points.Length;i++)
                {Vertices.Add(new Vector3(points[i].x,points[i].y,0));colors.Add(tint[i]);uvs.Add(uv==null?Vector2.zero:uv[i]);flags.Add(new Vector2(uv==null?0:1,0));}
                for(int i=1;i<points.Length-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);}
            }
            internal void Fill(Vector2[] p,Color color,Vector2[] uv=null)
            {var colors=new Color[p.Length];for(int i=0;i<colors.Length;i++)colors[i]=color;Polygon(p,colors,uv);}
            internal void Rect(Rect r,Color color)
            {Fill(new[]{new Vector2(r.x,r.y),new Vector2(r.xMax,r.y),new Vector2(r.xMax,r.yMax),new Vector2(r.x,r.yMax)},color);}
            internal void Upload()
            {Mesh.Clear();if(Vertices.Count==0)return;Mesh.SetVertices(Vertices);Mesh.SetColors(colors);Mesh.SetUVs(0,uvs);Mesh.SetUVs(1,flags);Mesh.SetTriangles(indices,0);Mesh.RecalculateBounds();}
            internal void Draw(Material material)
            {if(Vertices.Count==0)return;material.mainTexture=Texture==null?Texture2D.whiteTexture:Texture;if(material.SetPass(0))Graphics.DrawMeshNow(Mesh,Matrix4x4.identity);}
            internal void Dispose(){if(Mesh!=null)UnityEngine.Object.Destroy(Mesh);}
        }

        public IReadOnlyList<string> SlotMembers{get{CheckIdentity();return visibleSlots??(visibleSlots=Array.AsReadOnly(slots));}}
        public int PlayCount{get;private set;}
        public int SoundPlayCount{get;private set;}
        public bool Playing{get{CheckIdentity();return active&&CanPresent&&CurrentAge<Duration;}}
        public float Age{get{CheckIdentity();return active?CurrentAge:0;}}
        // Compatibility envelope: darkening only. Character panes have their own geometry and individual alpha.
        public float Opacity=>Playing?S(Age/.16f)*(1-S((Age-1.7f)/.9f)):0;
        public double PendingUntil{get{CheckIdentity();return Playing?started+Duration:double.NegativeInfinity;}}
        public bool SoundPaused{get{CheckIdentity();return Playing&&sound!=null&&pausedSound;}}
        public bool SoundPlaying{get{CheckIdentity();return Playing&&!pausedSound&&sound!=null&&sound.isPlaying;}}
        public bool AssetsReady=>approved!=null&&material!=null&&material.shader.isSupported&&followup!=null;
        public Rect ScreenBounds{get{var v=TacticalViewport.Current;return new Rect(v.Offset.x,v.Offset.y,1280*v.Scale,720*v.Scale);}}
        public int VisiblePaneCount{get{BuildGeometry();return paneCount;}}
        public int VisiblePortraitCount{get{BuildGeometry();return portraitCount;}}
        public int VisibleFragmentCount{get{BuildGeometry();return fragmentCount;}}
        public int VisibleOuterFragmentCount{get{BuildGeometry();return outerFragmentCount;}}
        public IReadOnlyList<Vector3> GeometryVertices{get{BuildGeometry();return visibleGeometry??(visibleGeometry=geometry.AsReadOnly());}}
        public IReadOnlyList<Vector2> PaneOffsets{get{BuildGeometry();return visibleOffsets??(visibleOffsets=Array.AsReadOnly(offsets));}}
        float CurrentAge=>owner==null||owner.Battle==null?0:(float)Math.Max(0,owner.PresentationClock-started);
        bool CanPresent=>isActiveAndEnabled&&owner!=null&&owner.isActiveAndEnabled&&owner.Battle!=null&&!owner.TestSelectionOpen&&!owner.ArtLoading;

        public void Initialize(ManualBattleController controller,Font font)
        {
            if(controller==null)throw new ArgumentNullException(nameof(controller));
            Clear();owner=controller;Font.textureRebuilt-=OnFontTextureRebuilt;captionFont=font;Font.textureRebuilt+=OnFontTextureRebuilt;
            observed=owner.Battle;run=observed?.RunId??-1;PrepareMaterial();
        }
        public void Play(IEnumerable<string> members,IReadOnlyDictionary<string,Sprite> extraPortraits=null)
        {
            if(members==null)throw new ArgumentNullException(nameof(members));
            CheckIdentity();if(owner==null||owner.Battle==null)return;
            Clear();var unique=new HashSet<string>(members.Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
            for(int i=0;i<7;i++){string id=GuerrillaOrderGeometry.Definition(i).CardId;if(unique.Remove(id))slots[i]=id;}
            int extension=0;
            foreach(string id in unique.OrderBy(id=>id,StringComparer.Ordinal))
            {
                if(extension==2)break;slots[7+extension]=id;
                if(extraPortraits!=null&&extraPortraits.TryGetValue(id,out var image))extensions[extension]=image;
                extension++;
            }
            observed=owner.Battle;run=observed.RunId;started=owner.PresentationClock;active=true;PlayCount++;dirty=true;PrepareMaterial();
        }
        /// <summary>The production mesh shader. Use RenderTo for the complete multi-layer composition.</summary>
        public Material PrepareMaterial()
        {
            if(material!=null)return material;
            approved=Resources.Load<Texture2D>("GuerrillaOrder/approved-cut-in");
            var shader=Resources.Load<Shader>("GuerrillaOrder/GuerrillaOrder");
            followup=Resources.Load<AudioClip>("GuerrillaOrder/followup-original");
            if(approved==null||shader==null||followup==null)throw new InvalidOperationException("Missing approved guerrilla V2 illustration, mesh shader or follow-up sound.");
            material=new Material(shader){name="Guerrilla V2 / premultiplied mesh composition",hideFlags=HideFlags.DontSave,mainTexture=approved};
            behind=new Batch("Guerrilla V2 / backdrop and attraction");panes=new Batch("Guerrilla V2 / native portraits and glass");
            front=new Batch("Guerrilla V2 / crystal beams and shards");caption=new Batch("Guerrilla V2 / native caption");
            for(int i=0;i<2;i++)extras[i]=new Batch("Guerrilla V2 / future-member glass "+i);
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.loop=false;sound.spatialBlend=0;sound.volume=.55f;sound.clip=followup;
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
            active=false;started=double.NegativeInfinity;pausedSound=cueTriggered=false;dirty=true;builtAt=float.NaN;
            Array.Clear(slots,0,slots.Length);Array.Clear(extensions,0,extensions.Length);Array.Clear(offsets,0,offsets.Length);
            paneCount=portraitCount=fragmentCount=outerFragmentCount=0;geometry.Clear();
            if(sound!=null)sound.Stop();
        }
        void LateUpdate(){CheckIdentity();UpdateSound();}
        void OnFontTextureRebuilt(Font rebuilt){if(rebuilt==captionFont)dirty=true;}
        void UpdateSound()
        {
            if(!Playing){if(sound!=null)sound.Stop();pausedSound=false;return;}
            if(sound==null)return;
            float age=Age,relative=age-SoundCueAt;
            sound.mute=owner.Muted;sound.pitch=Mathf.Clamp(owner.EffectiveSimulationSpeed,.03f,2);
            if(!cueTriggered&&relative>=0)
            {
                cueTriggered=true;
                // Do not replay a past transient when the first observer arrives after its sound window.
                if(relative<followup.length)
                {
                    sound.timeSamples=Mathf.Clamp(Mathf.RoundToInt(relative*followup.frequency),0,followup.samples-1);
                    sound.Play();SoundPlayCount++;
                }
            }
            if(!cueTriggered||relative<0)return;
            if(relative>=followup.length){sound.Stop();pausedSound=false;return;}
            if(owner.Battle.IsPaused){if(!pausedSound&&sound.isPlaying){sound.Pause();pausedSound=true;}return;}
            if(pausedSound){sound.UnPause();pausedSound=false;}
            int sample=Mathf.Clamp(Mathf.RoundToInt(relative*followup.frequency),0,followup.samples-1);
            if(sound.isPlaying&&Math.Abs(sound.timeSamples-sample)>followup.frequency*.045f)sound.timeSamples=sample;
        }

        void BuildGeometry()
        {
            CheckIdentity();PrepareMaterial();bool show=Playing;float time=show?Age:-1;
            if(!dirty&&builtAt==time)return;
            dirty=false;builtAt=time;
            behind.Clear();panes.Clear();front.Clear();caption.Clear();foreach(var b in extras)b.Clear();
            paneCount=portraitCount=fragmentCount=outerFragmentCount=0;geometry.Clear();Array.Clear(offsets,0,offsets.Length);
            behind.Texture=panes.Texture=front.Texture=approved;
            if(show)
            {
                Scenery(time);Attraction(time);Portraits(time);Crystal(time);BurstAndTail(time);Caption(time);
                float white=Mathf.Exp(-Mathf.Pow((time-.975f)/.032f,2))*.28f;
                if(white>.005f)front.Rect(new Rect(0,0,1920,1080),Rgba(196,232,255,white));
            }
            foreach(var b in Batches()){b.Upload();geometry.AddRange(b.Vertices);}
        }
        IEnumerable<Batch> Batches()
        {yield return behind;yield return panes;yield return extras[0];yield return extras[1];yield return front;yield return caption;}
        void Portraits(float time)
        {
            if(time>=GuerrillaOrderGeometry.CharactersGoneAt)return;
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
            var points=new Vector2[original.Length];var colors=new Color[points.Length];var uv=picture?new Vector2[points.Length]:null;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=pose.Apply(original[i]);float radius=new Vector2((original[i].x-C.x)/(1920*.49f),(original[i].y-C.y)/(1080*.48f)).magnitude;
                colors[i]=Color.Lerp(Rgba(15,20,29,.88f),Rgba(8,14,24,.94f),radius);colors[i].a*=pose.Alpha;
                if(picture)uv[i]=map.Uv(original[i],source,image);
            }
            b.Polygon(points,colors);
            if(picture)b.Fill(points,new Color(1,1,1,pose.Alpha),uv);
            Color edge=picture?Rgba(221,235,255,.72f):Rgba(105,134,165,.28f);
            edge.a*=pose.Alpha*(.55f+.45f*pose.Scale);
            for(int i=0;i<points.Length;i++)Line(b,points[i],points[(i+1)%points.Length],edge,(picture?1.8f:1)*pose.Scale,picture?8*pose.Scale:2*pose.Scale);
        }

        void Scenery(float t)
        {
            float amount=S(t/.16f)*(1-S((t-1.7f)/.9f));
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
            float build=S(t/.18f),breaking=S((t-.88f)/.7f),fade=1-S((t-1.2f)/1.25f);
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
            for(int j=0;j<4;j++)
            {
                float p=Mathf.Clamp01((age-.025f*j)/.62f),a=-.31f+j*.19f,alpha=Mathf.Sin(p*Mathf.PI)*(1-S((age-.44f)/.7f));
                if(alpha<=0)continue;
                float length=180+Out(p)*1700,offset=Mathf.Sin(p*Mathf.PI)*40*(j-1.5f);
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*length,origin=C+new Vector2(0,offset);
                Line(front,origin-d,origin+d,Rgba(58,139,255,alpha*.28f),22*(1-p)+3,28);
                Line(front,origin-d,origin+d,Rgba(222,250,255,alpha*.75f),4*(1-p)+.5f,12);
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
        void Text(string value,int size,FontStyle style,Vector2 baseline,Color color)
        {
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
            if(target==null)throw new ArgumentNullException(nameof(target));PrepareMaterial();UpdateSound();BuildGeometry();
            if(!target.IsCreated())target.Create();
            RenderTexture previous=RenderTexture.active;
            try
            {
                if(!Playing){RenderTexture.active=target;GL.Clear(false,true,Color.clear);return;}
                EnsureTexture(ref accumulation,target.width,target.height,"Guerrilla V2 / premultiplied layers");
                RenderTexture.active=accumulation;GL.Clear(false,true,Color.clear);
                GL.PushMatrix();
                try
                {
                    // IMGUI and the last camera leave a non-identity model-view matrix.
                    // Pixel projection alone does not reset it; isolate this screen-space mesh.
                    GL.modelview=Matrix4x4.identity;GL.LoadPixelMatrix(0,1920,1080,0);
                    foreach(var b in Batches())b.Draw(material);
                }
                finally{GL.PopMatrix();}
                // The final IMGUI blend needs straight alpha. Undo layer accumulation's premultiplication once.
                Graphics.Blit(accumulation,target,material,1);
            }
            finally{RenderTexture.active=previous;}
        }
        void OnGUI()
        {
            // GUI.depth belongs to this behaviour. Restoring it at return undoes its foreground order.
            GUI.depth=-2000;
            if(Event.current.type!=EventType.Repaint||!Playing)return;
            Rect bounds=ScreenBounds;EnsureTexture(ref frame,Mathf.Max(1,Mathf.RoundToInt(bounds.width)),Mathf.Max(1,Mathf.RoundToInt(bounds.height)),"Guerrilla V2 / foreground frame");
            RenderTo(frame);Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;
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
            if(material!=null)Destroy(material);if(sound!=null)Destroy(sound);
            behind?.Dispose();panes?.Dispose();front?.Dispose();caption?.Dispose();foreach(var b in extras)b?.Dispose();
        }
    }
}
