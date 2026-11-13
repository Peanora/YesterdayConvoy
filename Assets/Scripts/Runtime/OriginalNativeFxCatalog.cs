using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    internal struct OriginalNativeFxPose
    {
        public Vector3 Position,Scale,Direction;
        public Quaternion BasisRotation;
        public bool FlipX;
        public OriginalNativeFxPose(Vector3 position,Quaternion basisRotation,Vector3 direction,bool flipX=false,Vector3 scale=default)
        {Position=position;BasisRotation=basisRotation;Direction=direction;FlipX=flipX;Scale=scale==default?Vector3.one:scale;}
    }
    internal readonly struct OriginalNativeFxPathPoint
    {
        public readonly Vector3 Position;public readonly double At;
        public OriginalNativeFxPathPoint(Vector3 position,double at){Position=position;At=at;}
    }
    internal struct OriginalNativeFxPlacement
    {
        public bool HasParticleDriver,HasProjectileDriver,HoldByOwner,UseBodyRotation,IsLoop,HasFollowMountPoint,KeepOriginRotation,FollowScale,FollowScaleSign,UsePlaybackSpeed,OverwriteHeight;
        public int SpawnLocation,RotateType,MountPoint;
        public float HeightOffset,ScaleMultiplier,LimitedPlaybackSpeed,MaxLifetime;
        public string BackEffect;
    }
    internal static class OriginalNativeFxCatalog
    {
        [Serializable] internal sealed class Manifest {public int version;public Bundle[] bundles;public Effect[] effects;}
        [Serializable] internal sealed class Bundle {public string id,resource,sha256;public long bytes;public string[] dependencies;}
        [Serializable] internal sealed class Effect {public string key,bundle,asset,sourcePrefab;public bool loop;public float duration;public Node[] nodes;public Renderer[] renderers;}
        [Serializable] internal sealed class Node {public int[] indices;public long transformPathId;public string path;public bool active,isLoop;public float delay=-1,delayToPlay,delayToFinish;public UV[] uv;public Script[] scripts;}
        [Serializable] internal sealed class Script {public string type,fields;}
        [Serializable] internal sealed class UV {public int keepInitOffset,protectMainUV,useSecondMap,protectSecondUV;public float xspeed,yspeed,secondXSpeed,secondYSpeed;public string secondMapName;}
        [Serializable] internal sealed class Renderer {public int[] indices;public string path,kind;public int sortingOrder;public Material[] materials;}
        [Serializable] internal sealed class Material {public string sourceId,name,shader,resourceShader;public int renderQueue=-1;public string[] keywords;public Float[] floats;public Colour[] colors;public Texture[] textures;}
        [Serializable] internal sealed class Float {public string name;public float value;}
        [Serializable] internal sealed class Colour {public string name;public Color value;}
        [Serializable] internal sealed class Texture {public string name;public Vector2 scale,offset;}
        [Serializable] internal sealed class Reference {public int m_FileID;public long m_PathID;}
        [Serializable] internal sealed class Particle
        {
            public int _spawnLocation,_rotateType,_mainDir,_leftIsDefault,_useBodyRotation,_holdByOwner,_isLoop,_usePlaybackSpeed,_overwriteHeight;
            public float _heightOffset,_delayToPlay,_delayToFinish,_limitedPlaybackSpeed=2,_maxLifetime=-1;
            public Reference _rotationY,_rotationZ,_flipZ,_bodyTransofrm;
            public SubDelay[] _subFXDelayConfigs;
        }
        [Serializable] internal sealed class SubDelay {public Reference transform;public float delayTime;}
        [Serializable] internal sealed class BackSwitcher {public string _backEffect;}
        [Serializable] internal sealed class Mount {public int _mountPointType,_keepOriginRotation,_followScale,_followScaleSign;public float _scaleMultiplier=1;}
        static readonly Dictionary<string,Effect> effects=new Dictionary<string,Effect>(StringComparer.Ordinal);
        static readonly Dictionary<string,Bundle> sources=new Dictionary<string,Bundle>(StringComparer.Ordinal);
        static readonly Dictionary<string,AssetBundle> loaded=new Dictionary<string,AssetBundle>(StringComparer.Ordinal);
        static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>(StringComparer.Ordinal);
        static bool read;
        static void Read()
        {
            if(read)return;var text=Resources.Load<TextAsset>("NativeDefenders/Catalog");if(text==null)return;
            var data=JsonUtility.FromJson<Manifest>(text.text);Resources.UnloadAsset(text);
            if(data==null||data.version!=1||data.effects==null||data.bundles==null)throw new InvalidDataException("Invalid original native effect catalog.");
            foreach(var b in data.bundles){if(string.IsNullOrEmpty(b.id)||string.IsNullOrEmpty(b.resource)||b.resource.Contains("..")||b.resource.Contains("\\")||sources.ContainsKey(b.id))throw new InvalidDataException("Invalid native bundle record.");sources.Add(b.id,b);}
            foreach(var e in data.effects)
            {
                if(string.IsNullOrEmpty(e.key)||!e.key.StartsWith("defender/",StringComparison.Ordinal)||e.key.StartsWith("defender/D27/",StringComparison.Ordinal)||effects.ContainsKey(e.key)||!sources.ContainsKey(e.bundle)||string.IsNullOrEmpty(e.asset))throw new InvalidDataException("Invalid native effect record: "+e?.key);
                // JsonUtility can materialize a JSON null array element as an
                // empty class. Restore that original empty material slot before
                // any schema check, expansion or shader/texture restoration.
                foreach(var renderer in e.renderers??Array.Empty<Renderer>())
                    for(int slot=0;slot<(renderer.materials?.Length??0);slot++)
                    {
                        var m=renderer.materials[slot];
                        if(m!=null&&m.sourceId==null&&m.name==null&&m.shader==null&&m.resourceShader==null&&m.keywords==null&&m.floats==null&&m.colors==null&&m.textures==null)renderer.materials[slot]=null;
                    }
                effects.Add(e.key,e);
            }
            read=true;
        }
        internal static bool Supports(string key){Read();return key!=null&&effects.ContainsKey(key);}
        internal static bool TryGetPlacement(string key,out OriginalNativeFxPlacement placement)
        {
            placement=default;Read();if(key==null||!effects.TryGetValue(key,out var effect))return false;
            placement.IsLoop=effect.loop;placement.ScaleMultiplier=1;
            foreach(var node in effect.nodes??Array.Empty<Node>())foreach(var script in node.scripts??Array.Empty<Script>())
            {
                if(script.type=="ParticleEffect")
                {
                    var p=JsonUtility.FromJson<Particle>(script.fields);placement.HasParticleDriver=true;placement.SpawnLocation=p._spawnLocation;placement.RotateType=p._rotateType;placement.HoldByOwner=p._holdByOwner!=0;placement.UseBodyRotation=p._useBodyRotation!=0;placement.IsLoop=p._isLoop!=0;placement.HeightOffset=p._heightOffset;placement.OverwriteHeight=p._overwriteHeight!=0;placement.UsePlaybackSpeed=p._usePlaybackSpeed!=0;placement.LimitedPlaybackSpeed=p._limitedPlaybackSpeed;placement.MaxLifetime=p._maxLifetime;
                }
                else if(script.type=="ProjectileEffect")placement.HasProjectileDriver=true;
                else if(script.type=="SwitchableBackEffect")placement.BackEffect=JsonUtility.FromJson<BackSwitcher>(script.fields)._backEffect;
                else if(script.type=="FollowMountPoint")
                {
                    var m=JsonUtility.FromJson<Mount>(script.fields);placement.HasFollowMountPoint=true;placement.MountPoint=m._mountPointType;placement.KeepOriginRotation=m._keepOriginRotation!=0;placement.FollowScale=m._followScale!=0;placement.FollowScaleSign=m._followScaleSign!=0;placement.ScaleMultiplier=m._scaleMultiplier;
                }
            }
            return true;
        }
        internal static IEnumerator Prepare(IEnumerable<string> keys)
        {
            Read();var requested=(keys??Array.Empty<string>()).Where(Supports).Distinct().Select(k=>effects[k]).ToArray();
            var needed=new HashSet<string>(StringComparer.Ordinal);
            void Include(string id){if(!needed.Add(id))return;foreach(var dep in sources[id].dependencies??Array.Empty<string>())Include(dep);}
            foreach(var effect in requested)Include(effect.bundle);
            foreach(var id in needed)
            {
                if(loaded.ContainsKey(id))continue;var source=sources[id];var resource=Resources.LoadAsync<TextAsset>(source.resource);yield return resource;
                var text=resource.asset as TextAsset;if(text==null)throw new FileNotFoundException("Missing original native bundle resource: "+source.resource);
                var bytes=text.bytes;if(source.bytes>0&&bytes.LongLength!=source.bytes)throw new InvalidDataException("Original native bundle length mismatch: "+id);
                var request=AssetBundle.LoadFromMemoryAsync(bytes);yield return request;Resources.UnloadAsset(text);
                if(request.assetBundle==null)throw new InvalidDataException("Original native bundle load failed: "+id);loaded.Add(id,request.assetBundle);
            }
            foreach(var effect in requested)
            {
                string identity=effect.bundle+"/"+effect.asset;if(prefabs.ContainsKey(identity))continue;
                var request=loaded[effect.bundle].LoadAssetAsync<GameObject>(effect.asset);yield return request;
                var prefab=request.asset as GameObject;if(prefab==null)throw new InvalidDataException("Missing original native prefab: "+effect.key);prefabs.Add(identity,prefab);
            }
        }
        internal static bool TryPrepared(string key,out Effect effect,out GameObject prefab)
        {
            Read();prefab=null;return effects.TryGetValue(key,out effect)&&prefabs.TryGetValue(effect.bundle+"/"+effect.asset,out prefab);
        }
        internal static Shader ShaderFor(Material material)
        {
            if(!string.IsNullOrEmpty(material.resourceShader))
            {
                var shader=Resources.Load<Shader>(material.resourceShader);if(shader!=null&&shader.isSupported)return shader;
                throw new InvalidDataException("Missing or unsupported original native shader adapter: "+material.shader+" -> "+material.resourceShader);
            }
            string builtin=material.shader=="Legacy Shaders/Particles/Alpha Blended Premultiply"?"BuiltinPremultiply":material.shader=="Particles/Standard Unlit"?"BuiltinStandardUnlit":null;
            var reference=builtin==null?null:Resources.Load<UnityEngine.Material>("NativeDefenders/Shaders/"+builtin);
            if(reference!=null&&reference.shader!=null&&reference.shader.isSupported)return reference.shader;
            throw new InvalidDataException("Missing original native builtin shader reference: "+material.shader);
        }
    }
}
