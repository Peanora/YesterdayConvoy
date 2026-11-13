using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Opt-in development-Player observation. Enters the authored trials and uses
    // their finite cards; never grants health/SP, forces damage or issues a skill.
    public sealed partial class ManualBattleController
    {
        [Serializable] internal sealed class SixStarCase
        {
            public string id,scene;public bool passed;public int casts,deployments,bodyPixels,skillPixels,steadyPixels,skillBodyPixels,monsterPixels,hitPixels,shots,damageShots,dawnHeadPixels,dawnTrailPixels,dawnTurns,dawnHits;
            public double clock;public string outcome;public string[] checks,sourceKeys;
            public int isolatedSkillPixels,isolatedBodyPixels,continuousFrames,particleSystems,particles,meshVertices,clockMaterials,clockPropertyGaps,dawnTrailLayers,silverAshFieldPeak,silverAshWavePixels;
            public bool nativePauseVerified,skillEndCovered,terminalCovered,tailCovered,actionFramesChanged;
            public string captureCoverage;public string[] nativePaths,materialSources,dawnLayerPixels;
            public double captureStart,captureEnd;
        }
        [Serializable] internal sealed class SixStarResult
        {
            public bool passed,completed;public string graphics;public SixStarCase[] cases;public string[] errors;
            public string scope="Authored finite defender trials with production selection/pointer deployment and natural automatic SP. D27 retains its approved sprite assertions. Other roles use tag-owned original Mesh/PS geometry, isolated body/FX camera pixels, native particle/vertex/material-clock pause snapshots, continuous 10fps camera frames and restart. Capture labels distinguish skill end, natural terminal and bounded active skill; none imply original-client visual equivalence. No health/SP/target/damage/skill/result injection.";
        }
        static SpriteRenderer[] SixStarFx(string id)=>Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null&&r.sprite.name.StartsWith("OriginalVfx/defender/"+id+"/",StringComparison.Ordinal)).ToArray();
        static bool SixStarSelected(string id,string key)
        {
            string token=id=="D21"?"whitw2_skill_03_":id=="D22"?"yu_skill3_":id=="D23"?"monstr_skill_03_":id=="D24"?"angel2_skill_3_":id=="D25"?"hsgma2_skill_03_":id=="D26"?"svash2_skill_03_":id=="D27"?"sbell2_skill_03_":id=="D28"?"wang_skill_03_":id=="D29"?"demkni_skill_03_":id=="D30"?"mlynar_skill_03_":id=="D32"?"chen3_skill_03_":id=="D33"?"aglna2_skill_03_":id=="D34"?"clemnt_skill_03_":"kalts2_skill_02_";
            return key.Contains("/"+token)||id=="D28"&&key.EndsWith("/wang_passive_01_buff_02",StringComparison.Ordinal);
        }
        static bool SixStarNewHit(string id,string key)=>id=="D32"?key.Contains("/chen3_skill_03_hit_"):key.EndsWith(id=="D33"?"/aglna2_skill_03_hit_01":id=="D34"?"/clemnt_skill_03_hit_01":"/kalts2_skill_02_range_damage_01",StringComparison.Ordinal);
        static string SixStarFrames(IEnumerable<SpriteRenderer> fx)=>string.Join("|",fx.OrderBy(r=>r.GetEntityId().ToString()).Select(r=>r.GetEntityId()+"/"+r.sprite.GetEntityId()+"/"+r.transform.position+"/"+r.transform.lossyScale));
        static int SixStarPixels(Renderer[] renderers,string directory,string label,bool save,Renderer[] hidden=null)
        {
            var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var target=new RenderTexture(640,360,24);var a=new Texture2D(640,360,TextureFormat.RGB24,false);var b=new Texture2D(640,360,TextureFormat.RGB24,false);
            var states=(renderers??Array.Empty<Renderer>()).Concat(hidden??Array.Empty<Renderer>()).Where(r=>r!=null).Distinct().ToDictionary(r=>r,r=>r.enabled);
            try
            {
                foreach(var r in hidden??Array.Empty<Renderer>())if(r!=null)r.enabled=false;
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;a.ReadPixels(new Rect(0,0,640,360),0,0);a.Apply();
                foreach(var r in renderers)r.enabled=false;
                camera.Render();b.ReadPixels(new Rect(0,0,640,360),0,0);b.Apply();var first=a.GetPixels32();var second=b.GetPixels32();int pixels=0;
                for(int i=0;i<first.Length;i++)if(Math.Abs(first[i].r-second[i].r)+Math.Abs(first[i].g-second[i].g)+Math.Abs(first[i].b-second[i].b)>12)pixels++;
                if(save){File.WriteAllBytes(Path.Combine(directory,label+".png"),a.EncodeToPNG());File.WriteAllBytes(Path.Combine(directory,label+"-without.png"),b.EncodeToPNG());}
                return pixels;
            }
            finally
            {
                foreach(var state in states)if(state.Key!=null)state.Key.enabled=state.Value;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;target.Release();Destroy(target);Destroy(a);Destroy(b);
            }
        }
        static OriginalNativeFxTag[] SixStarNativeTags(string id,bool includeInactive=false)=>Object.FindObjectsByType<OriginalNativeFxTag>(includeInactive?FindObjectsInactive.Include:FindObjectsInactive.Exclude,FindObjectsSortMode.None)
            .Where(t=>t.Key!=null&&t.Key.StartsWith("defender/"+id+"/",StringComparison.Ordinal)).ToArray();
        static Renderer[] SixStarNativeRenderers(IEnumerable<OriginalNativeFxTag> tags)=>tags.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true))
            .Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy).Distinct().ToArray();
        static Renderer[] SixStarAllNativeRenderers()=>SixStarNativeRenderers(Object.FindObjectsByType<OriginalNativeFxTag>(FindObjectsSortMode.None));
        static Renderer[] SixStarActorRenderers(ManualBattle battle)
        {
            var roster=Object.FindAnyObjectByType<DefenderRosterView>();
            return Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Select(v=>(Renderer)v.Renderer)
                .Concat(battle.Defenders.Select(d=>(Renderer)roster?.FindVisual(d.EntityId))).Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy).Distinct().ToArray();
        }
        static bool SixStarNativeSelected(string id,string key)=>SixStarSelected(id,key)||id=="D28"&&key.Contains("/token_wang_skill_03_");
        static bool SixStarNativeHit(string id,string key)
        {
            if(id=="D29")return key.EndsWith("/common_heal_hit_01",StringComparison.Ordinal);
            if(id=="D26")return key.EndsWith("/svash2_skill_03_hit_01",StringComparison.Ordinal); // Actual travelling bird collisions own this original hit.
            return SixStarNativeSelected(id,key)&&(key.Contains("hit")||id=="D35"&&(key.Contains("range_damage")||key.Contains("range_heal")));
        }
        static bool SixStarInteraction(ManualBattle battle,DefenderState defender,double since)=>
            battle.CombatShots.Any(s=>s.SourceId==defender.EntityId&&s.Applied&&s.ImpactAt>=since&&(s.DamageDealt>0||s.Healing))||
            defender.SkillHealthRestored>0||defender.AuraHeals>0||defender.BurnApplications>0||defender.FearApplications>0||defender.ChessTriggers>0||defender.SnowHits>0;
        static readonly HashSet<string> SixStarTimeShaders=new HashSet<string>(StringComparer.Ordinal)
        {"YesterdayConvoyProbe/OriginalRamDisturb","YesterdayConvoyProbe/OriginalL2DRamDisturbCustomData","YesterdayConvoyProbe/OriginalDissolveCustomData","YesterdayConvoyProbe/OriginalDisturbDisturb","YesterdayConvoyProbe/OriginalDissolveAddUVTween","YesterdayConvoyProbe/OriginalDisturbAnchor","YesterdayConvoyProbe/OriginalDissolveABUVTween"};
        [Serializable] sealed class SixStarNativeFreeze
        {
            public string particlesDigest,geometryDigest,materialsDigest,tagsDigest;
            public int particleSystems,particles,vertices,unreadableStaticMeshes,clockMaterials,clockPropertyGaps;
            public bool clocksExplicit=true;
        }
        static string SixStarDigest(Action<BinaryWriter> write)
        {
            using(var bytes=new MemoryStream())
            {using(var writer=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true))write(writer);using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes.ToArray())).Replace("-","").ToLowerInvariant();}
        }
        static void SixStarWrite(BinaryWriter writer,Vector2 v){writer.Write(v.x);writer.Write(v.y);}
        static void SixStarWrite(BinaryWriter writer,Vector3 v){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
        static void SixStarWrite(BinaryWriter writer,Vector4 v){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);writer.Write(v.w);}
        static void SixStarWrite(BinaryWriter writer,Quaternion q){writer.Write(q.x);writer.Write(q.y);writer.Write(q.z);writer.Write(q.w);}
        static void SixStarWrite(BinaryWriter writer,Color c){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
        static void SixStarWriteMesh(BinaryWriter writer,Mesh mesh,SixStarNativeFreeze state)
        {
            writer.Write(mesh!=null);if(mesh==null)return;
            writer.Write(mesh.name);writer.Write(mesh.vertexCount);writer.Write(mesh.subMeshCount);writer.Write(mesh.isReadable);
            if(!mesh.isReadable){state.unreadableStaticMeshes++;return;}
            var vertices=mesh.vertices;writer.Write(vertices.Length);state.vertices+=vertices.Length;
            foreach(var vertex in vertices)SixStarWrite(writer,vertex);
            var uv=mesh.uv;writer.Write(uv.Length);foreach(var value in uv)SixStarWrite(writer,value);
            var colors=mesh.colors;writer.Write(colors.Length);foreach(var value in colors)SixStarWrite(writer,value);
            var triangles=mesh.triangles;writer.Write(triangles.Length);foreach(int index in triangles)writer.Write(index);
        }
        static SixStarNativeFreeze SixStarSnapshot(string id)
        {
            var state=new SixStarNativeFreeze();var tags=SixStarNativeTags(id).OrderBy(t=>t.GetEntityId().ToString(),StringComparer.Ordinal).ToArray();
            state.tagsDigest=SixStarDigest(writer=>
            {
                writer.Write(tags.Length);
                foreach(var tag in tags)
                {
                    writer.Write(tag.GetEntityId().ToString());writer.Write(tag.Key);writer.Write(tag.InstanceId??"");writer.Write(tag.SampleAge);writer.Write(tag.Stopped);
                    foreach(var node in tag.GetComponentsInChildren<Transform>(true).OrderBy(t=>t.GetEntityId().ToString(),StringComparer.Ordinal))
                    {writer.Write(node.GetEntityId().ToString());writer.Write(node.gameObject.activeSelf);SixStarWrite(writer,node.localPosition);SixStarWrite(writer,node.localRotation);SixStarWrite(writer,node.localScale);}
                }
            });
            state.particlesDigest=SixStarDigest(writer=>
            {
                foreach(var ps in tags.SelectMany(t=>t.GetComponentsInChildren<ParticleSystem>(true)).OrderBy(p=>p.GetEntityId().ToString(),StringComparer.Ordinal))
                {
                    state.particleSystems++;writer.Write(ps.GetEntityId().ToString());writer.Write(ps.time);writer.Write(ps.randomSeed);writer.Write(ps.isPaused);writer.Write(ps.isPlaying);writer.Write(ps.isEmitting);
                    var particles=new ParticleSystem.Particle[ps.particleCount];int count=ps.GetParticles(particles);state.particles+=count;writer.Write(count);
                    for(int i=0;i<count;i++)
                    {
                        var p=particles[i];SixStarWrite(writer,p.position);SixStarWrite(writer,p.velocity);SixStarWrite(writer,p.rotation3D);SixStarWrite(writer,p.angularVelocity3D);SixStarWrite(writer,p.axisOfRotation);
                        SixStarWrite(writer,p.startSize3D);SixStarWrite(writer,p.GetCurrentSize3D(ps));SixStarWrite(writer,(Color)p.startColor);SixStarWrite(writer,(Color)p.GetCurrentColor(ps));writer.Write(p.startLifetime);writer.Write(p.remainingLifetime);writer.Write(p.randomSeed);
                    }
                    if(ps.customData.enabled)foreach(var channel in new[]{ParticleSystemCustomData.Custom1,ParticleSystemCustomData.Custom2})
                    {var custom=new List<Vector4>();ps.GetCustomParticleData(custom,channel);writer.Write((int)channel);writer.Write(custom.Count);foreach(var value in custom)SixStarWrite(writer,value);}
                }
            });
            state.geometryDigest=SixStarDigest(writer=>
            {
                var baked=new Mesh();
                try
                {
                    foreach(var r in tags.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).OrderBy(r=>r.GetEntityId().ToString(),StringComparer.Ordinal))
                    {
                        writer.Write(r.GetEntityId().ToString());writer.Write(r.GetType().Name);writer.Write(r.enabled);writer.Write(r.gameObject.activeInHierarchy);SixStarWrite(writer,r.transform.position);SixStarWrite(writer,r.transform.rotation);SixStarWrite(writer,r.transform.lossyScale);
                        var filter=r.GetComponent<MeshFilter>();if(filter!=null)SixStarWriteMesh(writer,filter.sharedMesh,state);
                        if(r is ParticleSystemRenderer particles&&r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<ParticleSystem>().particleCount>0)
                        {
                            baked.Clear();particles.BakeMesh(baked,Camera.main,ParticleSystemBakeMeshOptions.Default);SixStarWriteMesh(writer,baked,state);
                            if(r.GetComponent<ParticleSystem>().trails.enabled){baked.Clear();particles.BakeTrailsMesh(baked,Camera.main,ParticleSystemBakeMeshOptions.Default);SixStarWriteMesh(writer,baked,state);}
                        }
                        if(r is LineRenderer line){writer.Write(line.positionCount);for(int i=0;i<line.positionCount;i++)SixStarWrite(writer,line.GetPosition(i));}
                        if(r is TrailRenderer trail){writer.Write(trail.emitting);writer.Write(trail.positionCount);var points=new Vector3[trail.positionCount];trail.GetPositions(points);foreach(var point in points)SixStarWrite(writer,point);}
                    }
                }
                finally{Destroy(baked);}
            });
            state.materialsDigest=SixStarDigest(writer=>
            {
                foreach(var material in tags.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().OrderBy(m=>m.GetEntityId().ToString(),StringComparer.Ordinal))
                {
                    writer.Write(material.GetEntityId().ToString());writer.Write(material.shader.name);writer.Write(material.renderQueue);
                    foreach(var keyword in material.shaderKeywords.OrderBy(k=>k,StringComparer.Ordinal))writer.Write(keyword);
                    bool needsTime=SixStarTimeShaders.Contains(material.shader.name),clock=material.HasProperty("_OriginalFxTime")&&material.HasProperty("_OriginalFxTimeEnabled");
                    if(needsTime&&clock){state.clockMaterials++;state.clocksExplicit&=material.GetFloat("_OriginalFxTimeEnabled")>.5f;}
                    else if(needsTime)state.clockPropertyGaps++;
                    for(int i=0;i<material.shader.GetPropertyCount();i++)
                    {
                        string name=material.shader.GetPropertyName(i);var type=material.shader.GetPropertyType(i);writer.Write(name);writer.Write((int)type);
                        switch(type)
                        {
                            case UnityEngine.Rendering.ShaderPropertyType.Color:SixStarWrite(writer,material.GetColor(name));break;
                            case UnityEngine.Rendering.ShaderPropertyType.Vector:SixStarWrite(writer,material.GetVector(name));break;
                            case UnityEngine.Rendering.ShaderPropertyType.Float:case UnityEngine.Rendering.ShaderPropertyType.Range:writer.Write(material.GetFloat(name));break;
                            case UnityEngine.Rendering.ShaderPropertyType.Texture:writer.Write(material.GetTexture(name)?.GetEntityId().ToString()??"");SixStarWrite(writer,material.GetTextureScale(name));SixStarWrite(writer,material.GetTextureOffset(name));break;
                        }
                    }
                }
            });
            return state;
        }
        [Serializable] sealed class SixStarPauseReport {public SixStarNativeFreeze before,after;public double clock;public bool particles,geometry,materials,tags,combat;}
        [Serializable] sealed class SixStarContinuousFrame
        {
            public int index,casts,appliedShots;public double clock,presentation,skillAge,actionAt;public string phase,bodyFrame,file;public string[] effects;
        }
        [Serializable] sealed class SixStarContinuousReport
        {
            public string id,coverage;public int fps=10,width=640,height=360;public bool includesGui=false;
            public string scope="Actual camera-rendered PNG sequence at one 0.1-second simulation step per frame; includes up to two seconds of pre-roll. Paused observations saved separately. Natural finite trial only; no original-client visual-equality claim.";
            public SixStarContinuousFrame[] frames;
        }
        sealed class SixStarContinuousCapture:IDisposable
        {
            sealed class Pending {internal byte[] Png;internal SixStarContinuousFrame Frame;}
            readonly string id,directory;readonly RenderTexture target=new RenderTexture(640,360,24);readonly Texture2D image=new Texture2D(640,360,TextureFormat.RGB24,false);
            readonly Queue<Pending> pre=new Queue<Pending>();readonly List<SixStarContinuousFrame> frames=new List<SixStarContinuousFrame>();bool recording;
            internal int Count=>frames.Count;
            internal double Start=>frames.Count>0?frames[0].presentation:0;
            internal double End=>frames.Count>0?frames[frames.Count-1].presentation:0;
            internal SixStarContinuousCapture(string id,string parent){this.id=id;directory=Path.Combine(parent,id+"-continuous");Directory.CreateDirectory(directory);}
            Pending Read(ManualBattle battle,DefenderState defender,SpriteRenderer body,double presentation,string phase)
            {
                var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                try
                {
                    camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();
                    return new Pending{Png=image.EncodeToPNG(),Frame=new SixStarContinuousFrame{clock=battle.Clock,presentation=presentation,skillAge=defender.SkillCasts>0?battle.Clock-defender.SkillStartedAt:-1,casts=defender.SkillCasts,actionAt=defender.LastActionTime,phase=phase,bodyFrame=body?.sprite?.name??"",appliedShots=battle.CombatShots.Count(s=>s.SourceId==id&&s.Applied),effects=SixStarNativeTags(id).Select(t=>t.Key).Distinct().OrderBy(k=>k,StringComparer.Ordinal).ToArray()}};
                }
                finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;}
            }
            void Write(Pending pending)
            {
                pending.Frame.index=frames.Count;pending.Frame.file="frame-"+frames.Count.ToString("D6")+".png";File.WriteAllBytes(Path.Combine(directory,pending.Frame.file),pending.Png);frames.Add(pending.Frame);
            }
            internal void Sample(ManualBattle battle,DefenderState defender,SpriteRenderer body,double presentation,bool started,string phase)
            {
                var pending=Read(battle,defender,body,presentation,phase);
                if(!started){pre.Enqueue(pending);while(pre.Count>20)pre.Dequeue();return;}
                if(!recording){recording=true;while(pre.Count>0)Write(pre.Dequeue());}
                Write(pending);
            }
            internal void SaveObservation(ManualBattle battle,DefenderState defender,SpriteRenderer body,double presentation,string label)
            {File.WriteAllBytes(Path.Combine(directory,label+".png"),Read(battle,defender,body,presentation,label).Png);}
            internal void Finish(string coverage)=>File.WriteAllText(Path.Combine(directory,"timeline.json"),JsonUtility.ToJson(new SixStarContinuousReport{id=id,coverage=coverage,frames=frames.ToArray()},true));
            public void Dispose(){target.Release();Destroy(target);Destroy(image);pre.Clear();}
        }
        static float SixStarPathDistance(Vector3 point,IReadOnlyList<ChenDawnWavePoint> path)
        {
            float nearest=float.PositiveInfinity;
            for(int i=1;i<path.Count;i++)
            {
                Vector3 a=path[i-1].Position,segment=path[i].Position-a;float t=segment.sqrMagnitude>1e-10f?Mathf.Clamp01(Vector3.Dot(point-a,segment)/segment.sqrMagnitude):0;
                nearest=Mathf.Min(nearest,Vector3.Distance(point,a+segment*t));
            }
            return nearest;
        }
        void SixStarDawnProbe(SixStarCase item,string directory,Renderer[] actors,Dictionary<string,int> layerPixels)
        {
            var all=SixStarAllNativeRenderers();
            foreach(var tag in SixStarNativeTags("D32").Where(t=>t.Key.Contains("/chen3_skill_03_trail_01")))
            {
                var wave=Battle.ChenDawnWaves.FirstOrDefault(w=>tag.InstanceId=="dawn-wave/"+w.Sequence);
                if(wave==null||wave.History.Count<2||!OriginalNativeFxCatalog.TryPrepared(tag.Key,out var definition,out _))continue;
                var descriptors=(definition.renderers??Array.Empty<OriginalNativeFxCatalog.Renderer>()).Where(r=>r.kind=="TrailRenderer").ToArray();
                if(descriptors.Length!=6)continue;
                var rendered=SixStarNativeRenderers(new[]{tag});var tails=new List<Renderer>();int valid=0;
                foreach(var descriptor in descriptors)
                {
                    var tail=rendered.OfType<MeshRenderer>().FirstOrDefault(r=>r.name=="Original native trail mesh / "+descriptor.path);
                    var mesh=tail?.GetComponent<MeshFilter>()?.sharedMesh;
                    if(tail==null||mesh==null||!mesh.isReadable||mesh.vertexCount<4)continue;
                    var materials=tail.sharedMaterials;
                    if(materials.Length!=descriptor.materials.Length||materials.Where((m,i)=>m==null||m.name!="Original Native / "+descriptor.materials[i].sourceId).Any())continue;
                    var source=tail.transform.parent.GetComponent<TrailRenderer>();if(source==null||source.emitting||source.enabled)continue;
                    float margin=(source.transform.position-wave.Position).magnitude+.1f;var vertices=mesh.vertices;bool follows=true;Vector3 first=default;float span=0;
                    for(int i=0;i+1<vertices.Length;i+=2)
                    {
                        var center=tail.transform.TransformPoint((vertices[i]+vertices[i+1])*.5f);if(i==0)first=center;span=Mathf.Max(span,Vector3.Distance(first,center));
                        follows&=SixStarPathDistance(center,wave.History)<=margin;
                    }
                    if(!follows||span<=.05f)continue;
                    valid++;tails.Add(tail);
                    if(!layerPixels.TryGetValue(descriptor.path,out int pixels)||pixels<=0)
                    {
                        var hidden=actors.Concat(all.Where(r=>r!=tail)).ToArray();
                        pixels=SixStarPixels(new[]{tail},directory,"D32-source-trail-layer-"+Array.IndexOf(descriptors,descriptor),true,hidden);
                        layerPixels[descriptor.path]=Math.Max(layerPixels.TryGetValue(descriptor.path,out int prior)?prior:0,pixels);
                    }
                }
                item.dawnTrailLayers=Math.Max(item.dawnTrailLayers,valid);
                if(valid==6&&item.dawnTrailPixels<=4)item.dawnTrailPixels=Math.Max(item.dawnTrailPixels,SixStarPixels(tails.ToArray(),directory,"D32-original-six-mesh-path",true,actors.Concat(all.Except(tails)).ToArray()));
                var heads=rendered.Except(tails).Where(r=>!(r is TrailRenderer)).ToArray();
                if(heads.Length>0&&item.dawnHeadPixels<=4)item.dawnHeadPixels=Math.Max(item.dawnHeadPixels,SixStarPixels(heads,directory,"D32-native-dragon-head",true,actors.Concat(all.Except(heads)).ToArray()));
            }
        }
        IEnumerator VerifyNativeSixStarCase(SixStarCase item,string directory)
        {
            string id=item.id;var checks=new List<string>();bool pass=true;
            void Check(string name,bool value){checks.Add(name+"="+value);pass&=value;Debug.Log("SIXSTAR_CHECK "+id+" "+name+"="+value);}
            var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Check("authored-trial-exists",data!=null);
            if(data==null){item.checks=checks.ToArray();yield break;}
            var cards=new[]{data.battle.Card}.Concat((data.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(c=>c.Card));bool entered=false;
            yield return Prepare(Plan(data.defenders,cards,data.Tactical),()=>entered=TryEnterDefenderSkillTrial(id),"原生六星实机检查 "+id);
            yield return null;yield return null;Check("production-selection",entered&&DefenderSkillTrialId==id);
            var battle=Battle;var defender=battle.FindDefender(id);var body=Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(id);
            Check("body-and-native-director",defender!=null&&body!=null&&DefenderVfxArt.SupportsDefender(defender));
            Check("actual-graphics-device",SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null);
            if(id=="D32")Check("correct-fire-chen-identity-ground-sword-skill",defender!=null&&defender.Settings.Name=="赤刃明霄陈"&&defender.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak&&defender.Settings.BlockCount==1&&defender.Position.y<.1f);
            if(!entered||defender==null||body==null){item.checks=checks.ToArray();ReturnToMonsterTests();yield return null;yield break;}
            Tick(4);yield return null;Check("preparation-animation-only",battle.Clock==0&&defender.SkillCasts==0&&defender.SkillSP==defender.Settings.Skill.InitialSP);
            item.scene=SceneManager.GetActiveScene().name;item.bodyPixels=SixStarPixels(new[]{body},directory,id+"-body",true,SixStarAllNativeRenderers());Check("isolated-body-actual-gpu",item.bodyPixels>20);
            StartBattle();battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(battle.Entry);
            bool Deploy(){ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool accepted=ProcessPointerUp(gate).Success;if(accepted)item.deployments++;return accepted;}
            Check("paused-pointer-deploy",Deploy());double pausedClock=battle.Clock;float sp=defender.SkillSP;Tick(3);yield return null;
            Check("paused-clock-and-sp",battle.Clock==pausedClock&&defender.SkillSP==sp);battle.SetPaused(false);
            var seen=new HashSet<string>(StringComparer.Ordinal);var paths=new HashSet<string>(StringComparer.Ordinal);var materialSources=new HashSet<string>(StringComparer.Ordinal);
            var firstInstances=new HashSet<string>(StringComparer.Ordinal);var bodyFrames=new HashSet<string>(StringComparer.Ordinal);var dawnLayers=new Dictionary<string,int>(StringComparer.Ordinal);
            int extras=0,probes=0,hitProbes=0,dawnProbes=0,firstCastCount=0;bool captured=false,steady=false,batch7Observed=false;double firstCast=double.NaN,firstEnded=double.NaN;
            using(var capture=new SixStarContinuousCapture(id,directory))
            {
                for(int tick=0;tick<2000&&battle.Phase==BattlePhase.Running;tick++)
                {
                    Tick(.1f);yield return null;
                    if(extras<5&&tick>20+extras*20){if(Deploy())extras++;}
                    if(defender.SkillCasts>0&&double.IsNaN(firstCast)){firstCast=defender.SkillStartedAt;firstCastCount=defender.SkillCasts;}
                    if(Array.IndexOf(DefenderBatch7Ids,id)>=0)batch7Observed|=Batch7SkillObserved(id);
                    bool started=!double.IsNaN(firstCast),active=battle.DefenderSkillActive(defender)||battle.IsFifthActive(defender);
                    bool firstActive=started&&defender.SkillCasts==firstCastCount&&active;
                    var tags=SixStarNativeTags(id);var all=SixStarNativeRenderers(tags);var actors=SixStarActorRenderers(battle);
                    if(id=="D26"&&firstActive)item.silverAshFieldPeak=Math.Max(item.silverAshFieldPeak,tags.Count(t=>!t.Stopped&&t.Key.EndsWith("/svash2_skill_03_range_01",StringComparison.Ordinal)));
                    foreach(var tag in tags)
                    {
                        seen.Add(tag.Key);foreach(var renderer in tag.GetComponentsInChildren<Renderer>(true)){paths.Add(tag.Key+" / "+renderer.name);foreach(var material in renderer.sharedMaterials)if(material!=null)materialSources.Add(material.name+" / "+material.shader.name);}
                        if(firstActive&&SixStarNativeSelected(id,tag.Key))firstInstances.Add(tag.InstanceId);
                    }
                    if(firstActive&&body.sprite!=null)bodyFrames.Add(body.sprite.name);
                    if(started&&!firstActive&&double.IsNaN(firstEnded)){firstEnded=PresentationClock;item.skillEndCovered=defender.Alive&&(!active||defender.SkillCasts>firstCastCount);}
                    capture.Sample(battle,defender,body,PresentationClock,started,!started?"pre-skill":firstActive?"skill-active":battle.Phase==BattlePhase.Running?"skill-tail":"natural-terminal");
                    var selected=SixStarNativeRenderers(tags.Where(t=>SixStarNativeSelected(id,t.Key)));
                    if(started&&selected.Length>0&&probes<32&&(tick%3==0||!captured))
                    {
                        item.skillPixels=Math.Max(item.skillPixels,SixStarPixels(selected,directory,id+"-skill",!captured));
                        int isolated=SixStarPixels(selected,directory,id+"-skill-isolated-no-actors",!captured,actors.Concat(SixStarAllNativeRenderers().Except(selected)).ToArray());
                        item.isolatedSkillPixels=Math.Max(item.isolatedSkillPixels,isolated);probes++;
                        if(item.skillPixels>20&&isolated>20)
                        {
                            item.skillBodyPixels=Math.Max(item.skillBodyPixels,SixStarPixels(new[]{body},directory,id+"-body-during-skill",item.skillBodyPixels<=20));
                            item.isolatedBodyPixels=Math.Max(item.isolatedBodyPixels,SixStarPixels(new[]{body},directory,id+"-body-isolated-no-fx",item.isolatedBodyPixels<=20,SixStarAllNativeRenderers()));
                            var monsters=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Where(v=>v.EntityId!=null&&battle.FindUnit(v.EntityId)?.Active==true&&v.Renderer!=null&&v.Renderer.enabled).Select(v=>(Renderer)v.Renderer).ToArray();
                            if(monsters.Length>0)item.monsterPixels=Math.Max(item.monsterPixels,SixStarPixels(monsters,directory,id+"-monsters-during-skill",item.monsterPixels<=20));
                            if(!captured){captured=true;yield return TacticalFrame(directory,id+"-visible-player");}
                            if(!item.nativePauseVerified)
                            {
                                var before=SixStarSnapshot(id);
                                if(before.vertices>0&&(before.particles>0||before.particleSystems==0))
                                {
                                    battle.SetPaused(true);double clock=battle.Clock;var health=battle.Units.Select(u=>u.Health).ToArray();var positions=battle.Units.Select(u=>u.Motion.Position).ToArray();
                                    var guardHealth=battle.Defenders.Select(d=>d.Health).ToArray();var guardPositions=battle.Defenders.Select(d=>d.Position).ToArray();var bodyFrame=body.sprite;
                                    capture.SaveObservation(battle,defender,body,PresentationClock,"pause-before");Tick(2);yield return null;yield return null;yield return null;
                                    var after=SixStarSnapshot(id);capture.SaveObservation(battle,defender,body,PresentationClock,"pause-after");
                                    var pause=new SixStarPauseReport{before=before,after=after,clock=clock,particles=before.particlesDigest==after.particlesDigest,geometry=before.geometryDigest==after.geometryDigest,materials=before.materialsDigest==after.materialsDigest,tags=before.tagsDigest==after.tagsDigest,
                                        combat=battle.Clock==clock&&health.SequenceEqual(battle.Units.Select(u=>u.Health))&&positions.SequenceEqual(battle.Units.Select(u=>u.Motion.Position))&&guardHealth.SequenceEqual(battle.Defenders.Select(d=>d.Health))&&guardPositions.SequenceEqual(battle.Defenders.Select(d=>d.Position))&&body.sprite==bodyFrame};
                                    File.WriteAllText(Path.Combine(directory,id+"-native-pause.json"),JsonUtility.ToJson(pause,true));
                                    Check("native-pause-particle-data-and-age",pause.particles);Check("native-pause-actual-vertices-and-transforms",pause.geometry&&pause.tags);
                                    Check("native-pause-source-material-values-and-clock",pause.materials&&before.clockPropertyGaps==0&&before.clocksExplicit);
                                    Check("active-native-pause-freezes-combat-and-body",pause.combat);
                                    item.particleSystems=before.particleSystems;item.particles=before.particles;item.meshVertices=before.vertices;item.clockMaterials=before.clockMaterials;item.clockPropertyGaps=before.clockPropertyGaps;
                                    item.nativePauseVerified=pause.particles&&pause.geometry&&pause.materials&&pause.tags&&pause.combat&&before.clockPropertyGaps==0&&before.clocksExplicit;battle.SetPaused(false);
                                }
                            }
                        }
                    }
                    if(started&&!steady&&battle.Clock-firstCast>=3.1&&selected.Length>0)
                    {
                        item.steadyPixels=Math.Max(item.steadyPixels,SixStarPixels(selected,directory,id+"-skill-steady",true,actors.Concat(SixStarAllNativeRenderers().Except(selected)).ToArray()));
                        if(item.steadyPixels>20){steady=true;if(id=="D22")Check("mature-original-wall-real-event",battle.DefenderWallActive(defender)&&tags.Any(t=>t.Key.Contains("/yu_skill3_range_01")));yield return TacticalFrame(directory,id+"-steady-visible-player");}
                    }
                    if(started&&hitProbes<40&&SixStarInteraction(battle,defender,firstCast))
                    {
                        var hits=SixStarNativeRenderers(tags.Where(t=>SixStarNativeHit(id,t.Key)));
                        if(hits.Length>0){item.hitPixels=Math.Max(item.hitPixels,SixStarPixels(hits,directory,id+"-target-hit-isolated",item.hitPixels==0,actors.Concat(SixStarAllNativeRenderers().Except(hits)).ToArray()));hitProbes++;}
                    }
                    if(id=="D26"&&started&&item.silverAshWavePixels<=4)
                    {
                        var waves=SixStarNativeRenderers(tags.Where(t=>t.Key.Contains("/svash2_skill_03_trail_")));
                        if(waves.Length>0)item.silverAshWavePixels=System.Math.Max(item.silverAshWavePixels,SixStarPixels(waves,directory,id+"-emitted-wave-isolated",true,actors.Concat(SixStarAllNativeRenderers().Except(waves)).ToArray()));
                    }
                    if(id=="D32"&&started)
                    {
                        foreach(var wave in battle.ChenDawnWaves){item.dawnTurns=Math.Max(item.dawnTurns,wave.Turns);item.dawnHits=Math.Max(item.dawnHits,wave.Hits);}
                        if(dawnProbes<32&&(item.dawnTrailLayers<6||item.dawnHeadPixels<=4||item.dawnTrailPixels<=4||dawnLayers.Count<6||dawnLayers.Values.Any(p=>p<=0)))
                        {SixStarDawnProbe(item,directory,actors,dawnLayers);dawnProbes++;}
                    }
                    if(started&&battle.Phase!=BattlePhase.Running)break;
                    if(started&&!double.IsNaN(firstEnded)&&PresentationClock-firstEnded>.2&&!SixStarNativeTags(id).Any(t=>firstInstances.Contains(t.InstanceId)))
                    {item.tailCovered=true;item.captureCoverage=item.skillEndCovered?"first-skill-ended-with-real-tail":"first-skill-interrupted-with-real-tail";break;}
                    if(started&&PresentationClock-firstCast>=60){item.captureCoverage="bounded-active-or-long-tail-60s";break;}
                }
                if(battle.Phase!=BattlePhase.Running&&!double.IsNaN(firstCast))
                {
                    item.terminalCovered=true;
                    for(int tail=0;tail<150&&PresentationClock+.05<NativeVfxPendingUntil;tail++)
                    {Tick(.1f);yield return null;capture.Sample(battle,defender,body,PresentationClock,true,"natural-terminal-tail");}
                    item.tailCovered=PresentationClock+.1>=NativeVfxPendingUntil;item.captureCoverage="natural-terminal"+(item.tailCovered?"-with-real-tail":"-tail-bounded");
                }
                if(string.IsNullOrEmpty(item.captureCoverage))item.captureCoverage=double.IsNaN(firstCast)?"no-natural-cast-before-guard":"guard-bound-before-skill-end";
                capture.Finish(item.captureCoverage);item.continuousFrames=capture.Count;item.captureStart=capture.Start;item.captureEnd=capture.End;
            }
            item.actionFramesChanged=bodyFrames.Count>1;item.casts=defender.SkillCasts;item.clock=battle.Clock;item.outcome=battle.Phase.ToString();
            item.shots=battle.CombatShots.Count(s=>s.SourceId==id);item.damageShots=battle.CombatShots.Count(s=>s.SourceId==id&&s.Applied&&s.DamageDealt>0);
            Check("natural-auto-skill",item.casts>0);Check("actual-native-selected-fx-gpu-isolated-from-body",item.skillPixels>20&&item.isolatedSkillPixels>20&&item.nativePauseVerified);
            Check("mature-selected-fx-gpu-after-startup",steady&&item.steadyPixels>20);Check("body-and-monsters-remain-visible-during-selected-fx",item.skillBodyPixels>20&&item.monsterPixels>20&&item.isolatedBodyPixels>20);
            Check("continuous-real-camera-sequence-with-action-change",item.continuousFrames>20&&item.actionFramesChanged);Check("real-skill-damage-or-healing-interaction",!double.IsNaN(firstCast)&&SixStarInteraction(battle,defender,firstCast));Check("real-target-hit-or-heal-native-gpu",item.hitPixels>0&&(id!="D26"||item.damageShots>0));
            if(id=="D26")Check("original-emitted-self-or-partner-wave-actual-gpu",item.silverAshWavePixels>4);
            if(Array.IndexOf(DefenderBatch7Ids,id)>=0)Check("new-skill-real-interaction",batch7Observed);
            if(id=="D32")Check("original-six-trail-source-material-path-vertices-and-gpu",item.dawnHeadPixels>4&&item.dawnTrailPixels>4&&item.dawnTrailLayers==6&&dawnLayers.Count==6&&dawnLayers.Values.All(p=>p>0)&&item.dawnTurns>0&&item.dawnHits>0);
            item.dawnLayerPixels=dawnLayers.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+"="+p.Value).ToArray();
            var priorNativeIds=new HashSet<UnityEngine.EntityId>(SixStarNativeTags(id,true).Select(t=>t.GetEntityId()));
            RestartBattle();yield return null;yield return null;var reset=Battle.FindDefender(id);
            var resetTags=SixStarNativeTags(id,true);
            // Lappland has an original ordinary-mode owner token at preparation.
            // Require a fresh frame-zero idle instance; prior skill/hit instances
            // must all disappear, without deleting that legitimate source idle art.
            bool cleanNative=resetTags.Length==0||id=="D21"&&resetTags.Length==1&&resetTags.All(t=>
                t.Key=="defender/D21/whitw2_token_01"&&!t.Stopped&&t.SampleAge==0&&!priorNativeIds.Contains(t.GetEntityId()));
            Check("restart-clears-prior-native-instances-and-restores-source-idle",Battle.Clock==0&&Battle.Units.Count==0&&reset.SkillCasts==0&&reset.Health==reset.MaxHealth&&cleanNative&&SixStarFx(id).Length==0);
            ReturnToMonsterTests();yield return null;yield return null;Check("returns-to-normal-selection",TestSelectionOpen&&!DefenderSkillTrialActive);
            if(id=="D26")Check("original-bird-aura-at-most-two-held-fields-not-per-attack",item.silverAshFieldPeak>0&&item.silverAshFieldPeak<=2);
            item.sourceKeys=seen.OrderBy(s=>s,StringComparer.Ordinal).ToArray();item.nativePaths=paths.OrderBy(s=>s,StringComparer.Ordinal).ToArray();item.materialSources=materialSources.OrderBy(s=>s,StringComparer.Ordinal).ToArray();item.checks=checks.ToArray();item.passed=pass;Debug.Log("SIXSTAR_CASE "+JsonUtility.ToJson(item));
        }
        internal IEnumerator VerifySixStarRoster(string directory,string only=null)
        {
            Directory.CreateDirectory(directory);diagnostic=true;Application.runInBackground=true;
            var results=new List<SixStarCase>();var errors=new List<string>();var result=new SixStarResult{graphics=SystemInfo.graphicsDeviceName};
            Application.LogCallback listener=(condition,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(condition);};Application.logMessageReceived+=listener;
            float previous=Time.timeScale;Time.timeScale=0;
            try
            {
                var ids=DefenderBatch4Ids.Concat(DefenderBatch5Ids).Concat(DefenderBatch6Ids).Concat(DefenderBatch7Ids).Where(id=>only==null||id==only).ToArray();
                foreach(string id in ids)
                {
                    var item=new SixStarCase{id=id};results.Add(item);var checks=new List<string>();bool pass=true;
                    if(id!="D27")
                    {
                        yield return VerifyNativeSixStarCase(item,directory);
                        File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new SixStarResult{cases=results.ToArray(),errors=errors.ToArray()},true));
                        continue;
                    }
                    void Check(string name,bool value){checks.Add(name+"="+value);pass&=value;Debug.Log("SIXSTAR_CHECK "+id+" "+name+"="+value);}
                    var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);Check("authored-trial-exists",data!=null);if(data==null){item.checks=checks.ToArray();continue;}
                    var cards=new[]{data.battle.Card}.Concat((data.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(c=>c.Card));bool entered=false;
                    yield return Prepare(Plan(data.defenders,cards,data.Tactical),()=>entered=TryEnterDefenderSkillTrial(id),"六星实机检查 "+id);
                    yield return null;yield return null;Check("production-selection",entered&&DefenderSkillTrialId==id);
                    var battle=Battle;var defender=battle.FindDefender(id);var body=Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(id);
                    Check("body-and-native-director",defender!=null&&body!=null&&DefenderVfxArt.SupportsDefender(defender));
                    if(id=="D32")Check("correct-fire-chen-identity-ground-sword-skill",defender!=null&&defender.Settings.Name=="赤刃明霄陈"&&defender.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak&&defender.Settings.BlockCount==1&&defender.Position.y<.1f);
                    if(!entered||defender==null||body==null){item.checks=checks.ToArray();item.passed=false;ReturnToMonsterTests();yield return null;continue;}
                    Tick(4);yield return null;Check("preparation-animation-only",battle.Clock==0&&defender.SkillCasts==0&&defender.SkillSP==defender.Settings.Skill.InitialSP);
                    item.scene=SceneManager.GetActiveScene().name;
                    if(body!=null){item.bodyPixels=SixStarPixels(new[]{body},directory,id+"-body",true);Check("actual-body-gpu",item.bodyPixels>20);}
                    StartBattle();battle.SetPaused(true);Vector2 gate=Camera.main.WorldToScreenPoint(battle.Entry);
                    bool Deploy(){ProcessPointerDown(ScreenPosition(CardSlot(2).center));ProcessPointerMove(gate);bool accepted=ProcessPointerUp(gate).Success;if(accepted)item.deployments++;return accepted;}
                    Check("paused-pointer-deploy",Deploy());double pausedClock=battle.Clock;float sp=defender.SkillSP;Tick(3);yield return null;
                    Check("paused-clock-and-sp",battle.Clock==pausedClock&&defender.SkillSP==sp);battle.SetPaused(false);
                    var seen=new HashSet<string>();int extras=0,probes=0,hitProbes=0;bool pausedFx=false,captured=false,steady=false;double firstCast=double.NaN;
                    for(int tick=0;tick<2000&&battle.Phase==BattlePhase.Running;tick++)
                    {
                        Tick(.1f);yield return null;
                        if(extras<5&&tick>20+extras*20){if(Deploy())extras++;}
                        if(defender.SkillCasts>0&&double.IsNaN(firstCast))firstCast=battle.Clock;
                        var allFx=SixStarFx(id);foreach(var renderer in allFx)seen.Add(renderer.name);
                        var fx=allFx.Where(r=>SixStarSelected(id,r.name)).ToArray();
                        if(!steady&&!double.IsNaN(firstCast)&&battle.Clock-firstCast>=3.1&&fx.Length>0)
                        {
                            int pixels=SixStarPixels(fx,directory,id+"-skill-steady",true);item.steadyPixels=Math.Max(item.steadyPixels,pixels);
                            if(pixels>20)
                            {
                                steady=true;
                                if(id=="D22")Check("mature-wall-has-real-active-model-and-original-side-layers",battle.DefenderWallActive(defender)&&fx.Any(r=>r.name.Contains("/yu_skill3_range_01")));
                                yield return TacticalFrame(directory,id+"-steady-visible-player");
                            }
                        }
                        if(Array.IndexOf(DefenderBatch7Ids,id)>=0&&defender.SkillCasts>0&&hitProbes<24)
                        {
                            var hits=allFx.Where(r=>SixStarNewHit(id,r.name)).ToArray();
                            if(hits.Length>0&&battle.CombatShots.Any(s=>s.SourceId==id&&s.Applied&&s.DamageDealt>0))
                            {int pixels=SixStarPixels(hits,directory,id+"-target-hit",item.hitPixels==0);hitProbes++;item.hitPixels=Math.Max(item.hitPixels,pixels);}
                        }
                        if(defender.SkillCasts>0&&fx.Length>0&&probes<24&&(tick%3==0||!captured))
                        {
                            int pixels=SixStarPixels(fx,directory,id+"-skill",!captured);probes++;item.skillPixels=Math.Max(item.skillPixels,pixels);
                            if(pixels>20&&(item.skillBodyPixels<=20||item.monsterPixels<=20))
                            {
                                item.skillBodyPixels=Math.Max(item.skillBodyPixels,SixStarPixels(new[]{body},directory,id+"-body-during-skill",item.skillBodyPixels<=20));
                                var monsters=Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Where(v=>v.EntityId!=null&&battle.FindUnit(v.EntityId)?.Active==true&&v.Renderer!=null&&v.Renderer.enabled).Select(v=>v.Renderer).ToArray();
                                if(monsters.Length>0)item.monsterPixels=Math.Max(item.monsterPixels,SixStarPixels(monsters,directory,id+"-monsters-during-skill",item.monsterPixels<=20));
                            }
                            if(pixels>20&&!captured){captured=true;yield return TacticalFrame(directory,id+"-visible-player");}
                            if(pixels>20&&!pausedFx)
                            {
                                battle.SetPaused(true);double clock=battle.Clock;var health=battle.Units.Select(u=>u.Health).ToArray();var positions=battle.Units.Select(u=>u.Motion.Position).ToArray();
                                var guardHealth=battle.Defenders.Select(d=>d.Health).ToArray();var guardPositions=battle.Defenders.Select(d=>d.Position).ToArray();var bodyFrame=body.sprite;
                                string frame=SixStarFrames(SixStarFx(id));Tick(2);yield return null;
                                Check("active-native-pause-freezes-frames-and-combat",battle.Clock==clock&&health.SequenceEqual(battle.Units.Select(u=>u.Health))&&positions.SequenceEqual(battle.Units.Select(u=>u.Motion.Position))&&
                                    guardHealth.SequenceEqual(battle.Defenders.Select(d=>d.Health))&&guardPositions.SequenceEqual(battle.Defenders.Select(d=>d.Position))&&body.sprite==bodyFrame&&SixStarFrames(SixStarFx(id))==frame);
                                pausedFx=true;battle.SetPaused(false);
                            }
                        }
                        if(captured&&pausedFx&&!double.IsNaN(firstCast)&&battle.Clock-firstCast>8&&(Array.IndexOf(DefenderBatch7Ids,id)<0||item.hitPixels>0))break;
                    }
                    item.casts=defender.SkillCasts;item.clock=battle.Clock;item.outcome=battle.Phase.ToString();
                    item.shots=battle.CombatShots.Count(s=>s.SourceId==id);item.damageShots=battle.CombatShots.Count(s=>s.SourceId==id&&s.Applied&&s.DamageDealt>0);
                    Check("natural-auto-skill",item.casts>0);Check("actual-original-skill-gpu",item.skillPixels>20&&pausedFx);
                    Check("mature-selected-fx-gpu-after-startup",steady&&item.steadyPixels>20);
                    Check("body-and-monsters-remain-visible-during-selected-fx",item.skillBodyPixels>20&&item.monsterPixels>20);
                    if(Array.IndexOf(DefenderBatch7Ids,id)>=0){Check("new-skill-real-interaction",Batch7SkillObserved(id));Check("real-selected-target-hit-gpu",item.hitPixels>0);}
                    RestartBattle();yield return null;yield return null;var reset=Battle.FindDefender(id);
                    Check("restart-clears-real-combat-and-native-effects",Battle.Clock==0&&Battle.Units.Count==0&&reset.SkillCasts==0&&reset.Health==reset.MaxHealth&&SixStarFx(id).Length==0);
                    ReturnToMonsterTests();yield return null;yield return null;Check("returns-to-normal-selection",TestSelectionOpen&&!DefenderSkillTrialActive);
                    item.sourceKeys=seen.OrderBy(s=>s).ToArray();item.checks=checks.ToArray();item.passed=pass;Debug.Log("SIXSTAR_CASE "+JsonUtility.ToJson(item));
                    File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new SixStarResult{cases=results.ToArray(),errors=errors.ToArray()},true));
                }
                result.completed=true;
            }
            finally
            {
                Time.timeScale=previous;Application.logMessageReceived-=listener;result.cases=results.ToArray();result.errors=errors.ToArray();result.passed=result.completed&&results.Count>0&&results.All(r=>r.passed)&&errors.Count==0;
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("SIXSTAR_ROSTER_RESULT passed="+result.passed+" cases="+results.Count+" errors="+errors.Count);Application.Quit(result.passed?0:1);
            }
        }
    }
    public sealed class SixStarRosterSmokeDriver:MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(started||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-sixStarRosterSmokeTest"))return;started=true;
            var host=new GameObject("Explicit six-star roster Player observer");DontDestroyOnLoad(host);host.AddComponent<SixStarRosterSmokeDriver>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;CampaignJourneySession.Clear();
            if(SceneManager.GetActiveScene().name!="MonsterTrial")yield return SceneManager.LoadSceneAsync("MonsterTrial");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir"),role=Array.IndexOf(args,"-sixStarOnly");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"SixStarRosterEvidence");
            ManualBattleController controller=null;
            for(int i=0;i<7200;i++)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.Battle!=null&&controller.MonsterTests.Count>0&&!controller.ArtLoading)break;
                yield return null;
            }
            if(controller==null||controller.Battle==null||controller.ArtLoading)
            {Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"result.json"),"{\"passed\":false,\"error\":\"Controller/art preparation did not become ready\"}");Application.Quit(1);yield break;}
            yield return controller.VerifySixStarRoster(directory,role>=0&&role+1<args.Length?args[role+1]:null);
        }
    }
}
