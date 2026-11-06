using System;
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
        internal static void WarmFor(int snowTier)
        {
            // The battle snapshot owns the tier. Future emblems must not depend on five specific art roots.
            if(snowTier>=5)Warm();
        }
    }
}
