using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    internal sealed class OriginalNativeFxTag:MonoBehaviour
    {
        public string Key,InstanceId;
        public double SampleAge;
        public bool Stopped;
    }
    // A native instance is addressed by a real event identity, never a draw-list index.
    internal sealed class OriginalNativeFxLayer:IDisposable
    {
        sealed class UvBinding {internal Transform Node;internal OriginalNativeFxCatalog.UV Data;internal Material Material;internal Vector2 Main,Second;internal double Delay;}
        sealed class RotationBinding {internal Transform Target;internal int Axis;internal bool Directional,LeftDefault,FourDirections;internal float MainAxisAngle;internal Quaternion Initial;}
        sealed class SortingBinding {internal Renderer Renderer;internal int Offset;}
        sealed class DelayBinding {internal Transform Target;internal float At;internal bool Active;}
        sealed class FinishBinding {internal Transform Node;internal FinishAnimation Data;}
        [Serializable] sealed class FinishAnimation
        {public string _triggerName;public int _isTrigger,_triggerValue,_isInt,_needStopParticleSystemWhenRecycle;}
        [Serializable] sealed class UvRotation
        {
            public string _propertyName="_Rotation";
            public int _rotateTex1,_rotateTex2,_rotateTex3,_rotateTex4;
            public float angle1,_angle2,_angle3,_angle4;
        }
        sealed class Instance
        {
            internal string Key,Id;internal OriginalNativeFxCatalog.Effect Definition;internal GameObject Root;internal OriginalNativeFxTag Tag;
            internal OriginalNativeFxPose Pose;internal bool Used,Stopped;internal double Age,Clock,Rate=1,StopClock,Tail,FinishDelay,StopAge=double.PositiveInfinity;
            internal ParticleSystem[] Particles;internal Animator[] Animators;internal Animation[] Legacy;
            internal Transform[] Nodes;internal readonly HashSet<ParticleSystem> Started=new HashSet<ParticleSystem>();
            internal readonly List<Material> Materials=new List<Material>();internal readonly List<UvBinding> Uv=new List<UvBinding>();
            internal readonly List<RotationBinding> Rotations=new List<RotationBinding>();
            internal readonly List<SortingBinding> Sorting=new List<SortingBinding>();
            internal readonly List<DelayBinding> Delays=new List<DelayBinding>();
            internal readonly List<FinishBinding> FinishAnimations=new List<FinishBinding>();
            internal readonly List<OriginalNativeFxTrail> Trails=new List<OriginalNativeFxTrail>();
            internal IReadOnlyList<OriginalNativeFxPathPoint> Path;
        }
        readonly Transform parent;readonly Dictionary<string,Instance> instances=new Dictionary<string,Instance>(StringComparer.Ordinal);
        readonly Dictionary<string,double> finished=new Dictionary<string,double>(StringComparer.Ordinal);
        readonly List<string> remove=new List<string>();double clock;Camera camera;bool disposed;
        public double PendingUntil {get;private set;}=double.NegativeInfinity;
        public int VisibleCount {get;private set;}
        internal OriginalNativeFxLayer(Transform parent){this.parent=parent;}
        internal void Begin(double clock,Camera camera)
        {
            if(disposed)throw new ObjectDisposedException(nameof(OriginalNativeFxLayer));this.clock=clock;this.camera=camera;VisibleCount=0;PendingUntil=double.NegativeInfinity;
            foreach(var instance in instances.Values)instance.Used=false;
            remove.Clear();foreach(var pair in finished)if(pair.Value<=clock)remove.Add(pair.Key);foreach(string id in remove)finished.Remove(id);
        }
        internal bool TryShow(string key,string instanceId,double age,OriginalNativeFxPose pose,bool loop=false,double stopAge=double.PositiveInfinity)
            =>Show(key,instanceId,age,pose,loop,stopAge,null,null);
        internal bool TryShowPath(string key,string instanceId,double age,IReadOnlyList<OriginalNativeFxPathPoint> worldHistory,OriginalNativeFxPose pose,bool loop=true,double stopAge=double.PositiveInfinity)
            =>Show(key,instanceId,age,pose,loop,stopAge,worldHistory,null);
        internal bool TrySetLine(string key,string instanceId,double age,IReadOnlyList<Vector3> worldPoints,OriginalNativeFxPose pose,bool loop=false,double stopAge=double.PositiveInfinity)
            =>Show(key,instanceId,age,pose,loop,stopAge,null,worldPoints);
        bool Show(string key,string id,double age,OriginalNativeFxPose pose,bool loop,double stopAge,IReadOnlyList<OriginalNativeFxPathPoint> path,IReadOnlyList<Vector3> line)
        {
            if(camera==null||age<0||double.IsNaN(age)||double.IsInfinity(age))return false;
            if(!OriginalNativeFxCatalog.TryPrepared(key,out var definition,out var prefab))return false;
            if(string.IsNullOrEmpty(id))throw new ArgumentException("A stable real-event identity is required.",nameof(id));
            if(finished.ContainsKey(id))return false;
            if(OriginalNativeFxCatalog.TryGetPlacement(key,out var placement)&&placement.MaxLifetime>0)stopAge=Math.Min(stopAge,placement.MaxLifetime);
            if(instances.TryGetValue(id,out var instance)&&(instance.Key!=key||age+1e-6<instance.Age))
            {Destroy(instance);instances.Remove(id);instance=null;}
            if(instance==null){instance=Create(definition,prefab,id);instances.Add(id,instance);}
            instance.Used=true;instance.Path=path;instance.Pose=pose;instance.StopAge=stopAge;
            if(pose.Scale==default)instance.Pose.Scale=Vector3.one;
            if(pose.BasisRotation==default)instance.Pose.BasisRotation=Quaternion.identity;
            double delta=age-instance.Age;if(clock>instance.Clock+1e-7&&delta>1e-7)instance.Rate=delta/(clock-instance.Clock);
            ApplyPose(instance,instance.Pose);
            int order=DemoSpriteVisual.GetSortingOrder(camera,pose.Position);
            foreach(var sorting in instance.Sorting)sorting.Renderer.sortingOrder=order+32+sorting.Offset;
            if(!instance.Stopped&&age>=stopAge)Stop(instance);
            double target=instance.Stopped?Math.Min(age,instance.Age+Math.Max(0,clock-instance.Clock)*instance.Rate):age;
            double sample=instance.Age;
            // Native systems are paused between explicit shared-simulation steps.
            while(sample+1e-7<target)
            {
                float step=(float)Math.Min(1.0/60,target-sample);sample+=step;Evaluate(instance,sample,step);
            }
            if(target==0||delta<=1e-7)Evaluate(instance,target,0);
            instance.Age=target;instance.Clock=clock;instance.Tag.SampleAge=target;
            if(!loop&&!instance.Stopped&&instance.Particles.Length>0&&instance.Particles.All(p=>!p.main.loop))
            {
                double lastDelay=instance.Delays.Count==0?0:instance.Delays.Max(d=>(double)d.At);
                foreach(var n in instance.Definition.nodes??Array.Empty<OriginalNativeFxCatalog.Node>())lastDelay=Math.Max(lastDelay,Math.Max(0,n.delay)+n.delayToPlay);
                if(target>lastDelay+.001&&instance.Particles.All(p=>!p.IsAlive(false)))Stop(instance);
            }
            foreach(var trail in instance.Trails)trail.Present(clock,path,instance.Pose,camera,!instance.Stopped);
            if(line!=null)foreach(var renderer in instance.Root.GetComponentsInChildren<LineRenderer>(true))
            {renderer.useWorldSpace=true;renderer.positionCount=line.Count;for(int i=0;i<line.Count;i++)renderer.SetPosition(i,line[i]);}
            foreach(var renderer in instance.Root.GetComponentsInChildren<Renderer>(true))if(renderer.enabled&&renderer.gameObject.activeInHierarchy)VisibleCount++;
            return true;
        }
        Instance Create(OriginalNativeFxCatalog.Effect definition,GameObject prefab,string id)
        {
            var root=Object.Instantiate(prefab,parent);root.name="Original Native FX / "+definition.key+" / "+id;root.SetActive(false);
            var instance=new Instance{Key=definition.key,Id=id,Definition=definition,Root=root,Clock=clock};
            instance.Tag=root.AddComponent<OriginalNativeFxTag>();instance.Tag.Key=definition.key;instance.Tag.InstanceId=id;
            instance.Nodes=(definition.nodes??Array.Empty<OriginalNativeFxCatalog.Node>()).Select(n=>Resolve(root.transform,n.indices)).ToArray();
            var byId=new Dictionary<long,Transform>();for(int i=0;i<instance.Nodes.Length;i++)if(definition.nodes[i].transformPathId!=0)byId[definition.nodes[i].transformPathId]=instance.Nodes[i];
            foreach(var descriptor in definition.renderers??Array.Empty<OriginalNativeFxCatalog.Renderer>())
            {
                var node=Resolve(root.transform,descriptor.indices);var renderer=node.GetComponents<Renderer>().FirstOrDefault(r=>r.GetType().Name==descriptor.kind);
                if(renderer==null)throw new InvalidDataException("Original native renderer missing: "+definition.key+" / "+descriptor.path);
                var originals=renderer.sharedMaterials;
                // Unity normalizes some old PS renderers' duplicated inactive
                // trail slots to one material. Reuse only the exact serialized
                // identity; a missing distinct source material remains an error.
                if(renderer is ParticleSystemRenderer&&originals.Length<descriptor.materials.Length)
                {
                    var expanded=new Material[descriptor.materials.Length];Array.Copy(originals,expanded,originals.Length);bool complete=true;
                    for(int slot=originals.Length;slot<expanded.Length;slot++)
                    {
                        var source=descriptor.materials[slot];if(source==null)continue;
                        int match=Array.FindIndex(descriptor.materials,0,originals.Length,m=>m!=null&&m.sourceId==source.sourceId);
                        if(match<0||originals[match]==null){complete=false;break;}expanded[slot]=originals[match];
                    }
                    if(complete)originals=expanded;
                }
                if(originals.Length!=descriptor.materials.Length)throw new InvalidDataException("Original native material slot mismatch: "+definition.key+" / "+descriptor.path+" / "+descriptor.kind+" source="+descriptor.materials.Length+" runtime="+originals.Length+" main=["+string.Join(",",originals.Select(m=>m!=null?m.name:"null"))+"] trail="+(renderer is ParticleSystemRenderer psr?psr.trailMaterial?.name:"n/a"));
                var materials=new Material[originals.Length];
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var source=descriptor.materials[slot];if(originals[slot]==null||source==null)continue;
                    var material=new Material(originals[slot]){name="Original Native / "+source.sourceId};material.shader=OriginalNativeFxCatalog.ShaderFor(source);
                    // Unsupported Android shaders return zero from GetFloat/GetColor on
                    // D3D. Restore their actual serialized values, not reflected zeros.
                    foreach(var p in source.floats??Array.Empty<OriginalNativeFxCatalog.Float>())material.SetFloat(p.name,p.value);
                    foreach(var p in source.colors??Array.Empty<OriginalNativeFxCatalog.Colour>())material.SetColor(p.name,p.value);
                    foreach(var p in source.textures??Array.Empty<OriginalNativeFxCatalog.Texture>()){material.SetTextureScale(p.name,p.scale);material.SetTextureOffset(p.name,p.offset);}
                    material.shaderKeywords=source.keywords??Array.Empty<string>();if(source.renderQueue>=0)material.renderQueue=source.renderQueue;
                    materials[slot]=material;instance.Materials.Add(material);
                }
                renderer.sharedMaterials=materials;
                instance.Sorting.Add(new SortingBinding{Renderer=renderer,Offset=descriptor.sortingOrder});
                if(renderer is TrailRenderer nativeTrail)
                {instance.Tail=Math.Max(instance.Tail,nativeTrail.time);instance.Trails.Add(new OriginalNativeFxTrail(nativeTrail,descriptor.path));}
            }
            for(int i=0;i<instance.Nodes.Length;i++)
            {
                var data=definition.nodes[i];var node=instance.Nodes[i];
                foreach(var uv in data.uv??Array.Empty<OriginalNativeFxCatalog.UV>())foreach(var renderer in node.GetComponents<Renderer>())foreach(var material in renderer.sharedMaterials)
                    if(material!=null)instance.Uv.Add(new UvBinding{Node=node,Data=uv,Material=material,Main=material.GetTextureOffset("_MainTex"),Second=uv.useSecondMap!=0?material.GetTextureOffset(uv.secondMapName):Vector2.zero});
                foreach(var script in data.scripts??Array.Empty<OriginalNativeFxCatalog.Script>())if(script.type=="FxUVRotation"&&ScriptEnabled(script.fields))
                {
                    var rotation=JsonUtility.FromJson<UvRotation>(script.fields);
                    int[] active={rotation._rotateTex1,rotation._rotateTex2,rotation._rotateTex3,rotation._rotateTex4};
                    float[] angles={rotation.angle1,rotation._angle2,rotation._angle3,rotation._angle4};
                    foreach(var renderer in node.GetComponents<Renderer>())foreach(var material in renderer.sharedMaterials)if(material!=null)
                    {
                        material.SetFloat("_HG_UV_ROTATION",1);
                        for(int channel=0;channel<4;channel++)
                        {
                            float radians=active[channel]!=0?angles[channel]*Mathf.Deg2Rad:0;
                            float cosine=Mathf.Cos(radians),sine=Mathf.Sin(radians);
                            material.SetVector((rotation._propertyName??"_Rotation")+channel,new Vector4(cosine,sine,-sine,cosine));
                        }
                    }
                }
                foreach(var script in data.scripts??Array.Empty<OriginalNativeFxCatalog.Script>())if(script.type=="OnFinishAnime"&&ScriptEnabled(script.fields))
                    instance.FinishAnimations.Add(new FinishBinding{Node=node,Data=JsonUtility.FromJson<FinishAnimation>(script.fields)});
                foreach(var script in data.scripts??Array.Empty<OriginalNativeFxCatalog.Script>())if((script.type=="ParticleEffect"||script.type=="ProjectileEffect")&&ScriptEnabled(script.fields))
                {
                    var driver=JsonUtility.FromJson<OriginalNativeFxCatalog.Particle>(script.fields);
                    foreach(var delay in driver._subFXDelayConfigs??Array.Empty<OriginalNativeFxCatalog.SubDelay>())
                        if(delay.transform!=null&&byId.TryGetValue(delay.transform.m_PathID,out var delayed))instance.Delays.Add(new DelayBinding{Target=delayed,At=delay.delayTime+driver._delayToPlay,Active=delayed.gameObject.activeSelf});
                    if(driver._rotateType==1&&driver._rotationY!=null&&byId.TryGetValue(driver._rotationY.m_PathID,out var y))instance.Rotations.Add(new RotationBinding{Target=y,Axis=1,LeftDefault=driver._leftIsDefault!=0});
                    if((driver._rotateType==2||script.type=="ProjectileEffect")&&driver._rotationZ!=null&&byId.TryGetValue(driver._rotationZ.m_PathID,out var z))instance.Rotations.Add(new RotationBinding{Target=z,Axis=2,Directional=true,FourDirections=script.type=="ParticleEffect"&&driver._rotateType==2,MainAxisAngle=MainAxisAngle(driver._mainDir),Initial=z.localRotation});
                    if(script.type=="ProjectileEffect")
                    {
                        var directional=instance.Nodes.Where(t=>t.name=="rotation_z").OrderBy(t=>Depth(t)).FirstOrDefault();
                        if(directional!=null&&!instance.Rotations.Any(r=>r.Target==directional))instance.Rotations.Add(new RotationBinding{Target=directional,Axis=2,Directional=true});
                    }
                    instance.FinishDelay=Math.Max(instance.FinishDelay,driver._delayToFinish);
                }
            }
            foreach(var binding in instance.Uv)
                foreach(var delay in instance.Delays)if(binding.Node==delay.Target||binding.Node.IsChildOf(delay.Target))binding.Delay=Math.Max(binding.Delay,delay.At);
            instance.Particles=root.GetComponentsInChildren<ParticleSystem>(true);instance.Animators=root.GetComponentsInChildren<Animator>(true);instance.Legacy=root.GetComponentsInChildren<Animation>(true);
            foreach(var ps in instance.Particles){ps.useAutoRandomSeed=false;ps.randomSeed=StableSeed(id);ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);instance.Tail=Math.Max(instance.Tail,Math.Max(0,ps.main.startLifetime.constantMax));}
            root.SetActive(true);
            foreach(var animator in instance.Animators)if(animator.enabled){animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);animator.speed=0;}
            foreach(var animation in instance.Legacy)animation.Stop();
            return instance;
        }
        static uint StableSeed(string id){unchecked{uint value=2166136261;foreach(char c in id)value=(value^c)*16777619;return value==0?1:value;}}
        [Serializable] sealed class Enabled {public int m_Enabled=1;}
        static bool ScriptEnabled(string json)=>JsonUtility.FromJson<Enabled>(json).m_Enabled!=0;
        static int Depth(Transform t){int depth=0;while(t.parent!=null){depth++;t=t.parent;}return depth;}
        // Torappu.SharedConsts.Direction: UP=0, RIGHT=1, DOWN=2, LEFT=3.
        static float MainAxisAngle(int direction)=>direction==0?90:direction==2?-90:direction==3?180:0;
        static Transform Resolve(Transform root,int[] path){foreach(int i in path??Array.Empty<int>())root=root.GetChild(i);return root;}
        static void ApplyPose(Instance instance,OriginalNativeFxPose pose)
        {
            instance.Root.transform.SetPositionAndRotation(pose.Position,pose.BasisRotation);instance.Root.transform.localScale=pose.Scale;
            var local=Quaternion.Inverse(pose.BasisRotation)*pose.Direction;float angle=Mathf.Atan2(local.y,local.x)*Mathf.Rad2Deg;
            foreach(var rotation in instance.Rotations)
            {
                if(rotation.Axis==1)rotation.Target.localRotation=Quaternion.Euler(0,pose.FlipX!=rotation.LeftDefault?180:0,0);
                else if(rotation.FourDirections)
                {
                    float delta=Mathf.Round(Mathf.DeltaAngle(rotation.MainAxisAngle,angle)/90)*90;
                    rotation.Target.localRotation=rotation.Initial*Quaternion.Euler(0,0,delta);
                }
                else rotation.Target.localRotation=Quaternion.Euler(0,0,angle);
            }
        }
        void Evaluate(Instance instance,double age,float step)
        {
            for(int i=0;i<instance.Nodes.Length;i++)
            {
                var data=instance.Definition.nodes[i];if(data.delay>=0||data.delayToPlay>0)instance.Nodes[i].gameObject.SetActive(data.active&&age+1e-6>=Math.Max(0,data.delay)+data.delayToPlay);
            }
            foreach(var delay in instance.Delays)delay.Target.gameObject.SetActive(delay.Active&&age+1e-7>=delay.At);
            foreach(var animator in instance.Animators)if(animator.enabled&&animator.gameObject.activeInHierarchy&&step>0){animator.speed=1;animator.Update(step);animator.speed=0;}
            foreach(var animation in instance.Legacy)if(animation.enabled&&animation.gameObject.activeInHierarchy&&animation.clip!=null)
            {var clip=animation.clip;clip.SampleAnimation(animation.gameObject,clip.wrapMode==WrapMode.Loop&&clip.length>0?Mathf.Repeat((float)age,clip.length):Mathf.Min((float)age,clip.length));}
            foreach(var ps in instance.Particles)if(ps.gameObject.activeInHierarchy)
            {
                if(!instance.Stopped&&instance.Started.Add(ps)){ps.Play(false);ps.Pause(false);}
                ps.Simulate(step,false,false,false);ps.Pause(false);
            }
            foreach(var material in instance.Materials){float t=(float)age;material.SetVector("_OriginalFxTime",new Vector4(t/20,t,t*2,t*3));material.SetFloat("_OriginalFxTimeEnabled",1);}
            foreach(var binding in instance.Uv)if(binding.Node.gameObject.activeInHierarchy)
            {
                var uv=binding.Data;float t=(float)Math.Max(0,age-binding.Delay);
                if(uv.protectMainUV==0)binding.Material.SetTextureOffset("_MainTex",(uv.keepInitOffset!=0?binding.Main:Vector2.zero)+new Vector2(uv.xspeed,uv.yspeed)*t);
                if(uv.useSecondMap!=0&&uv.protectSecondUV==0)binding.Material.SetTextureOffset(uv.secondMapName,(uv.keepInitOffset!=0?binding.Second:Vector2.zero)+new Vector2(uv.secondXSpeed,uv.secondYSpeed)*t);
            }
        }
        void Stop(Instance instance)
        {
            if(instance.Stopped)return;instance.Stopped=true;instance.StopClock=clock;instance.Tag.Stopped=true;
            instance.Tail=instance.FinishDelay;
            bool finishAnimation=false;
            foreach(var binding in instance.FinishAnimations)
            {
                var data=binding.Data;if(string.IsNullOrEmpty(data._triggerName))continue;
                foreach(var animator in binding.Node.GetComponentsInChildren<Animator>(true))
                {
                    if(!animator.enabled||!animator.parameters.Any(p=>p.name==data._triggerName))continue;
                    if(data._isTrigger!=0)animator.SetTrigger(data._triggerName);
                    else if(data._isInt!=0)animator.SetInteger(data._triggerName,data._triggerValue);
                    else animator.SetBool(data._triggerName,data._triggerValue!=0);
                    finishAnimation=true;
                }
            }
            foreach(var trail in instance.Trails)instance.Tail=Math.Max(instance.Tail,trail.Lifetime);
            foreach(var ps in instance.Particles)
            {
                int count=ps.particleCount;if(count==0)continue;var particles=new ParticleSystem.Particle[count];count=ps.GetParticles(particles);
                for(int i=0;i<count;i++)instance.Tail=Math.Max(instance.Tail,particles[i].remainingLifetime);
            }
            foreach(var ps in instance.Particles)ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
            // An explicit source finish animator owns its renderer fade. Its
            // manager's positive delay is the recycle boundary, not a bake window.
            if(finishAnimation&&instance.FinishDelay>0)instance.Tail=instance.FinishDelay;
            if(!finishAnimation)foreach(var renderer in instance.Root.GetComponentsInChildren<MeshRenderer>(true))if(!renderer.name.StartsWith("Original native trail mesh",StringComparison.Ordinal))renderer.enabled=false;
        }
        internal void End()
        {
            remove.Clear();foreach(var pair in instances)
            {
                var instance=pair.Value;
                if(!instance.Used&&!instance.Stopped)Stop(instance);
                if(!instance.Used&&instance.Stopped)
                {
                    double delta=Math.Max(0,clock-instance.Clock);instance.Age+=delta*instance.Rate;instance.Clock=clock;Evaluate(instance,instance.Age,(float)(delta*instance.Rate));instance.Tag.SampleAge=instance.Age;
                    foreach(var trail in instance.Trails)trail.Present(clock,null,instance.Pose,camera,false);
                }
                if(instance.Stopped)
                {
                    double until=instance.StopClock+Math.Min(12,instance.Tail/Math.Max(.0001,instance.Rate));
                    if(clock>=until){remove.Add(pair.Key);continue;}PendingUntil=Math.Max(PendingUntil,until);
                }
            }
            foreach(var id in remove){Destroy(instances[id]);instances.Remove(id);finished[id]=clock+30;}
        }
        static void Destroy(Instance instance){instance.Root.SetActive(false);foreach(var trail in instance.Trails)trail.Dispose();foreach(var material in instance.Materials)Object.Destroy(material);Object.Destroy(instance.Root);}
        internal void Clear(){foreach(var instance in instances.Values)Destroy(instance);instances.Clear();finished.Clear();VisibleCount=0;PendingUntil=double.NegativeInfinity;}
        public void Dispose(){if(disposed)return;Clear();disposed=true;}
    }
}
