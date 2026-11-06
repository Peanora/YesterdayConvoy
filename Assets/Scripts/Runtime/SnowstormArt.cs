using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Three small original alpha textures; not HDR-baked character clips or a new global atlas cache.
    internal static class SnowstormArt
    {
        internal static Texture2D Snow,Fog,Wind;
        internal static AudioClip Bed,Gust;
        internal static void Warm()
        {
            if(Snow!=null&&Fog!=null&&Wind!=null&&Bed!=null&&Gust!=null)return;
            var snow=Resources.Load<Texture2D>("SnowstormWeather/snow");
            var fog=Resources.Load<Texture2D>("SnowstormWeather/fog");
            var wind=Resources.Load<Texture2D>("SnowstormWeather/wind");
            var bed=Resources.Load<AudioClip>("SnowstormWeather/wind-loop");
            var gust=Resources.Load<AudioClip>("SnowstormWeather/gust");
            if(snow==null||fog==null||wind==null||bed==null||gust==null)throw new InvalidOperationException("Missing snowstorm weather resources.");
            Snow=snow;Fog=fog;Wind=wind;Bed=bed;Gust=gust;
        }
        internal static void WarmFor(IEnumerable<ArtSequence> plan)
        {
            var roles=new HashSet<string>(StringComparer.Ordinal);
            foreach(var clip in plan)foreach(string id in new[]{"M06","M11","M12","M13","M14"})
                if(clip.Root=="MonsterCombatArt/"+id||clip.Root.StartsWith("MonsterCombatArt/"+id+"/",StringComparison.Ordinal))roles.Add(id);
            if(roles.Count==5)Warm();
        }
    }
}
