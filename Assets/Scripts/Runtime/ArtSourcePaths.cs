using System;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Stable logical names keep animation/cache identities independent of source-folder layout.
    public static class ArtSourcePaths
    {
        static readonly string[,] ImageRoots = {
            {"DefenderArt", "Art/Characters/Defenders/Flipbooks"},
            {"MonsterCombatArt", "Art/Characters/Monsters/Flipbooks"},
            {"TalulahOriginal", "Art/Characters/Monsters/Boss/TalulahOriginal"},
            {"OriginiumSlugOriginal", "Art/Characters/Monsters/OriginiumSlugOriginal"},
            {"OriginalCombatVfx", "Art/VFX/OriginalCombat"},
            {"TalulahUnboundVfx", "Art/VFX/Skills/Talulah/Unbound"},
            {"ShopTrial", "Art/UI/Shop"},
            {"ApprovedMonsterArt", "Art/Characters/Monsters/Previews/ApprovedMonsterArt"},
            {"ArcherOriginal", "Art/Characters/Monsters/Previews/ArcherOriginal"}
        };

        static string Normalize(string relative)
        {
            if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative))throw new ArgumentException("Art path must be relative.",nameof(relative));
            relative=relative.Replace('\\','/');
            foreach(string part in relative.Split('/'))
                if(part.Length==0||part=="."||part==".."||part.Contains(":"))throw new ArgumentException("Invalid art path.",nameof(relative));
            return relative;
        }

        public static string Resolve(string logicalRelative)
        {
            string relative=Normalize(logicalRelative);
            if(!Application.isEditor)return Path.Combine(Application.streamingAssetsPath,relative);
            if(relative.EndsWith(".json",StringComparison.OrdinalIgnoreCase))
                return Path.Combine(Application.dataPath,"Data/ArtCatalogs",relative);
            return ImageDirectory(relative);
        }

        public static string ImageDirectory(string logicalRoot)
        {
            string relative=Normalize(logicalRoot);
            if(!Application.isEditor)return Path.Combine(Application.streamingAssetsPath,relative);
            for(int i=0;i<ImageRoots.GetLength(0);i++)
            {
                string prefix=ImageRoots[i,0];
                if(relative==prefix||relative.StartsWith(prefix+"/",StringComparison.Ordinal))
                    return Path.Combine(Application.dataPath,ImageRoots[i,1]+relative.Substring(prefix.Length));
            }
            throw new ArgumentException("Unknown art package: "+relative,nameof(logicalRoot));
        }

        public static string ManifestDirectory(string logicalRoot)
        {
            string relative=Normalize(logicalRoot);
            return Path.Combine(Application.isEditor?Path.Combine(Application.dataPath,"Data/ArtCatalogs"):Application.streamingAssetsPath,relative);
        }

        public static bool IsSourceTexture(string assetPath)
        {
            if(assetPath==null||!assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return false;
            for(int i=0;i<ImageRoots.GetLength(0);i++)
                if(assetPath.StartsWith("Assets/"+ImageRoots[i,1]+"/",StringComparison.Ordinal))return true;
            return false;
        }
    }
}
