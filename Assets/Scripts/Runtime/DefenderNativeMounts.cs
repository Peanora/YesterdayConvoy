using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Attachment data uses the exact displayed body frame, including its retiming,
    // front/back selection and source-origin pivot. It never advances another clock.
    internal static class DefenderNativeMounts
    {
        [Serializable] sealed class Manifest { public MountData nativeMounts; }
        [Serializable] sealed class MountData { public int version;public Binding[] bindings;public MountClip[] clips; }
        [Serializable] sealed class Binding { public string effect,point;public bool keepOriginRotation,followScale,followScaleSign;public float scaleMultiplier=1; }
        [Serializable] sealed class MountClip { public string key;public int count;public float pixelScale,originX,originY,skeletonDataScale;public Point[] points; }
        [Serializable] sealed class Point { public string point,bone;public int stride;public float[] values; }
        sealed class Data
        {
            internal readonly Dictionary<string,Binding> Bindings=new Dictionary<string,Binding>(StringComparer.OrdinalIgnoreCase);
            internal readonly Dictionary<string,MountClip> Clips=new Dictionary<string,MountClip>(StringComparer.Ordinal);
        }
        static readonly Dictionary<string,Data> Cache=new Dictionary<string,Data>(StringComparer.Ordinal);
        static bool Supported(string id)=>id=="D23"||id=="D26"||id=="D30"||id=="D32"||id=="D33";

        internal static bool RequiresMount(string artId,string rawEffect)
        {
            if(string.IsNullOrEmpty(artId)||string.IsNullOrEmpty(rawEffect))return false;
            string id=artId.TrimEnd('B');if(!Supported(id))return false;
            return Load(id).Bindings.ContainsKey(rawEffect.Substring(rawEffect.LastIndexOf('/')+1));
        }

        internal static bool TryResolve(string artId,string rawEffect,SpriteRenderer body,out Vector3 position,out float rollDegrees)
            =>TryResolve(artId,rawEffect,body,out position,out rollDegrees,out _);

        internal static bool TryResolve(string artId,string rawEffect,SpriteRenderer body,out Vector3 position,out float rollDegrees,out Vector2 localScale)
        {
            position=body!=null?body.transform.position:Vector3.zero;rollDegrees=0;localScale=Vector2.one;
            if(body==null||body.sprite==null||string.IsNullOrEmpty(artId)||string.IsNullOrEmpty(rawEffect))return false;
            string id=artId.TrimEnd('B');if(!Supported(id))return false;
            string[] frameName=body.sprite.name.Split('/');
            if(frameName.Length!=3||frameName[0].TrimEnd('B')!=id||
                !int.TryParse(frameName[2],NumberStyles.None,CultureInfo.InvariantCulture,out int frame))return false;
            string variant=frameName[0];if(variant!=id&&variant!=id+"B")return false;
            var data=Load(variant);string effect=rawEffect.Substring(rawEffect.LastIndexOf('/')+1);
            // Only effects with an original FollowMountPoint binding appear in this table.
            if(!data.Bindings.TryGetValue(effect,out var binding)||!data.Clips.TryGetValue(frameName[1],out var clip)||frame<0||frame>=clip.count)return false;
            Point point=null;foreach(var candidate in clip.points)if(candidate.point==binding.point){point=candidate;break;}
            if(point==null)return false;
            int stride=point.stride>0?point.stride:3,offset=frame*stride;float ppu=body.sprite.pixelsPerUnit,sign=body.flipX?-1:1;
            // Older approved flipbooks retain a measured foot-origin correction.
            var local=new Vector3((point.values[offset]*clip.pixelScale+clip.originX)/ppu*sign,(point.values[offset+1]*clip.pixelScale+clip.originY)/ppu,0);
            position=body.transform.TransformPoint(local);
            if(!binding.keepOriginRotation)rollDegrees=point.values[offset+2]*sign;
            if(binding.followScale)
            {
                if(stride<5||clip.skeletonDataScale<=0)return false;
                // Replace the source .27 model-parent scale by the equivalent baked
                // body scale k/.01. Spine data .01 already scales bone positions.
                float gain=clip.pixelScale/ppu/clip.skeletonDataScale*binding.scaleMultiplier;
                float x=point.values[offset+3]*gain,y=point.values[offset+4]*gain;
                localScale=binding.followScaleSign?new Vector2(x,y):new Vector2(Mathf.Abs(x),Mathf.Abs(y));
            }
            return true;
        }

        static Data Load(string variant)
        {
            if(Cache.TryGetValue(variant,out var cached))return cached;
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("DefenderArt/"+variant+"/manifest.json")));
            var source=manifest?.nativeMounts;var data=new Data();
            if(source==null){Cache.Add(variant,data);return data;}
            if(source.version!=1||source.bindings==null||source.clips==null)throw new InvalidDataException("Invalid original defender attachment table: "+variant);
            foreach(var binding in source.bindings)
            {
                if(string.IsNullOrEmpty(binding.effect)||string.IsNullOrEmpty(binding.point)||data.Bindings.ContainsKey(binding.effect))throw new InvalidDataException("Invalid original defender attachment binding: "+variant);
                data.Bindings.Add(binding.effect,binding);
            }
            foreach(var clip in source.clips)
            {
                if(string.IsNullOrEmpty(clip.key)||clip.count<=0||clip.pixelScale<=0||float.IsNaN(clip.pixelScale)||float.IsInfinity(clip.pixelScale)||float.IsNaN(clip.originX)||float.IsInfinity(clip.originX)||float.IsNaN(clip.originY)||float.IsInfinity(clip.originY)||clip.points==null||data.Clips.ContainsKey(clip.key))throw new InvalidDataException("Invalid original defender attachment clip: "+variant);
                foreach(var point in clip.points)
                {
                    int stride=point.stride>0?point.stride:3;
                    if((stride!=3&&stride!=5)||string.IsNullOrEmpty(point.point)||string.IsNullOrEmpty(point.bone)||point.values==null||point.values.Length!=clip.count*stride)throw new InvalidDataException("Invalid original defender bone track: "+variant+"/"+clip.key);
                    foreach(float value in point.values)if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidDataException("Non-finite original defender bone track: "+variant+"/"+clip.key);
                }
                data.Clips.Add(clip.key,clip);
            }
            Cache.Add(variant,data);return data;
        }
    }
}
