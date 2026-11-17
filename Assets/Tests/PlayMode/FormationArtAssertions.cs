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
