using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    // Read only the production source manifest. Do not rename source sprites to fit legacy tests.
    internal static class FormationArtAssertions
    {
        [Serializable] sealed class Manifest {public Alias[] aliases;public Clip[] clips;}
        [Serializable] sealed class Alias {public string key,source;}
        [Serializable] sealed class Clip {public string key,source;public Page[] pages;}
        [Serializable] sealed class Page {public string file;}
        sealed class Idle {internal string Prefix;internal HashSet<string> Textures;}
        static readonly Dictionary<string,Idle> expected=new Dictionary<string,Idle>();
        static readonly Dictionary<Sprite,Bounds> visibleBounds=new Dictionary<Sprite,Bounds>();
        static Texture2D cachedPixelTexture;
        static Color32[] cachedPixels;

        // Native animation cells deliberately retain transparent action space. Camera QA
        // measures painted pixels, rather than requiring that unused source space to fit.
        // GPU readback also works for the production non-readable atlas textures.
        internal static Bounds VisibleLocalBounds(Sprite sprite)
        {
            if(sprite==null||sprite.texture==null)throw new ArgumentNullException(nameof(sprite));
            if(visibleBounds.TryGetValue(sprite,out var existing))return existing;
            var texture=sprite.texture;
            if(cachedPixelTexture!=texture||cachedPixels==null)
            {
                var active=RenderTexture.active;
                var temporary=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                var readback=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true);
                try
                {
                    Graphics.Blit(texture,temporary);
                    RenderTexture.active=temporary;
                    readback.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0,false);
                    cachedPixels=readback.GetPixels32();cachedPixelTexture=texture;
                }
                finally
                {
                    RenderTexture.active=active;RenderTexture.ReleaseTemporary(temporary);
                    UnityEngine.Object.Destroy(readback);
                }
            }
            var rect=sprite.textureRect;
            int left=Mathf.RoundToInt(rect.x),bottom=Mathf.RoundToInt(rect.y);
            int right=left+Mathf.RoundToInt(rect.width),top=bottom+Mathf.RoundToInt(rect.height);
            int minX=right,minY=top,maxX=left-1,maxY=bottom-1;
            for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)
                if(cachedPixels[y*texture.width+x].a>8)
                {minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(maxX<minX||maxY<minY)throw new InvalidOperationException("No visible pixels in source sprite "+sprite.name);
            var offset=sprite.textureRectOffset-sprite.pivot;
            var min=new Vector3((minX-left+offset.x)/sprite.pixelsPerUnit,(minY-bottom+offset.y)/sprite.pixelsPerUnit,0);
            var max=new Vector3((maxX-left+1+offset.x)/sprite.pixelsPerUnit,(maxY-bottom+1+offset.y)/sprite.pixelsPerUnit,0);
            var bounds=new Bounds();bounds.SetMinMax(min,max);visibleBounds[sprite]=bounds;return bounds;
        }
        internal static bool IsActualIdleFrame(string id,Sprite sprite)
        {
            if(sprite==null||sprite.texture==null||sprite.texture.width<=0||sprite.texture.height<=0)return false;
            var source=FormationUnitArt.For(id);if(source==null||!source.HasAction("idle"))return false;
            if(source.Root==null)return source.Frame("idle",0,true)==sprite;
            if(!expected.TryGetValue(source.Root,out var idle))
            {
                var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve(source.Root+"/manifest.json")));
                bool native=source.Root.StartsWith("BossRoster/",StringComparison.Ordinal);
                string key=native?data.aliases.Single(alias=>alias.key=="idle").source:"idle";
                var clip=data.clips.Single(item=>item.key==key||item.source==key);
                // OriginalSlugArt deliberately names its shared sequence OriginiumSlug/idle
                // while the physical package is OriginiumSlugOriginal. Preserve that existing
                // explicit reader alias and still validate the precise package texture below.
                string legacyName=source.Root=="OriginiumSlugOriginal"?"OriginiumSlug":Path.GetFileName(source.Root);
                idle=new Idle{Prefix=native?source.Root+"/"+key:legacyName+"/idle",
                    Textures=new HashSet<string>(clip.pages.Select(page=>source.Root+"/"+page.file),StringComparer.Ordinal)};
                expected[source.Root]=idle;
            }
            string name=sprite.name;
            if(!name.StartsWith(idle.Prefix+"/",StringComparison.Ordinal)&&!name.StartsWith(idle.Prefix+"~",StringComparison.Ordinal))return false;
            return idle.Textures.Contains(sprite.texture.name.Replace('\\','/'));
        }
    }
}
