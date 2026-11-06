using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    // Source art lives in Art/Data. Only current runtime derivatives enter the Player's StreamingAssets.
    public sealed class PreparedArtTools:IPreprocessBuildWithReport,IPostprocessBuildWithReport
    {
        public int callbackOrder=>-100;
        static bool verifiedThisSession;
        static string Cache=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt"));
        static readonly string[] Roots={"MonsterCombatArt","DefenderArt","TalulahOriginal","OriginiumSlugOriginal","TalulahUnboundVfx","OriginalCombatVfx"};
        static readonly string[] TalulahPhaseKeys={"attack-B-caster","attack-B-hit","ambient-B-foot","movement-B-foot","ambient-B-head","ambient-B-wing","first_transition_start","first_transition_end","final_transition_start","final_transition_end","ignite-B-caster"};
        public void OnPreprocessBuild(BuildReport report)=>Prepare(!verifiedThisSession);
        public void OnPostprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.StandaloneWindows64)return;
            ExportRuntimeArt(report.summary.outputPath);
        }
        [InitializeOnLoadMethod]static void InstallPreparation()
        {EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingEditMode)Prepare(false);};}
        [Serializable]sealed class Manifest {public int version;public string task;public float ppu;public bool rearVariant,auxiliary;public Clip[] clips;}
        [Serializable]sealed class Clip {public string key;public int cell,cellWidth,cellHeight;public Page[] pages;}
        [Serializable]sealed class Page {public string file;}
        sealed class Preview {public string Source;public Clip Idle;}
        sealed class Plan
        {
            public readonly SortedSet<string> Images=new SortedSet<string>(StringComparer.Ordinal);
            public readonly SortedDictionary<string,Preview> Previews=new SortedDictionary<string,Preview>(StringComparer.Ordinal);
            // Relative Player filename -> exact classified source or prepared-cache file.
            public readonly SortedDictionary<string,string> Files=new SortedDictionary<string,string>(StringComparer.Ordinal);
            public int Manifests;
        }
        static string RelativeTo(string folder,string file)
        {
            string prefix=Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string full=Path.GetFullPath(file);
            if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Art path escapes its package: "+file);
            return full.Substring(prefix.Length).Replace('\\','/');
        }
        static string PageRelative(string root,string file)
        {
            if(string.IsNullOrWhiteSpace(file)||Path.IsPathRooted(file)||file.Contains("\\")||!file.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid art page: "+file);
            foreach(string segment in file.Split('/'))
                if(segment.Length==0||segment=="."||segment=="..")throw new InvalidDataException("Invalid art page: "+file);
            return root+"/"+file;
        }
        static void RequireSource(string relative)
        {if(!File.Exists(ArtSourcePaths.Resolve(relative)))throw new FileNotFoundException("Required classified art source is missing: "+relative,ArtSourcePaths.Resolve(relative));}
        static void AddImage(Plan plan,string relative)
        {
            RequireSource(relative);
            if(plan.Images.Add(relative))plan.Files.Add(relative+".ycart",Path.Combine(Cache,relative+".ycart"));
        }
        static Plan CreatePlan()
        {
            var plan=new Plan();
            foreach(string logicalRoot in Roots)
            {
                string sourceRoot=ArtSourcePaths.ManifestDirectory(logicalRoot);
                if(!Directory.Exists(sourceRoot))throw new DirectoryNotFoundException("Required art catalog directory is missing: "+sourceRoot);
                string[] manifests=Directory.GetFiles(sourceRoot,"manifest.json",SearchOption.AllDirectories);
                if(manifests.Length==0)throw new InvalidDataException("Required art catalog has no manifests: "+logicalRoot);
                Array.Sort(manifests,StringComparer.Ordinal);
                foreach(string source in manifests)
                {
                    string manifest=logicalRoot+"/"+RelativeTo(sourceRoot,source);
                    string root=manifest.Substring(0,manifest.Length-"/manifest.json".Length);
                    var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(source));
                    if(data==null||data.version!=1||data.clips==null||data.clips.Length==0)throw new InvalidDataException("Invalid art manifest: "+manifest);
                    plan.Files.Add(manifest,source);plan.Manifests++;
                    var seen=new HashSet<string>(StringComparer.Ordinal);
                    var phaseKeys=logicalRoot=="TalulahUnboundVfx"?new HashSet<string>(TalulahPhaseKeys,StringComparer.Ordinal):null;
                    foreach(var clip in data.clips)
                    {
                        if(clip==null||string.IsNullOrWhiteSpace(clip.key)||!seen.Add(clip.key)||clip.pages==null||clip.pages.Length==0)
                            throw new InvalidDataException("Invalid art clip in "+manifest);
                        bool included=phaseKeys==null||phaseKeys.Contains(clip.key);
                        foreach(var page in clip.pages)
                        {
                            if(page==null)throw new InvalidDataException("Null art page in "+manifest);
                            string relative=PageRelative(root,page.file);
                            // Also validate unbound source pages; they remain source-only until explicitly bound.
                            RequireSource(relative);
                            if(included)AddImage(plan,relative);
                        }
                    }
                    if(phaseKeys!=null)
                        foreach(string key in phaseKeys)if(!seen.Contains(key))throw new InvalidDataException("Missing required Talulah phase VFX: "+key);
                    string portrait=root+"/portrait.png";
                    if(File.Exists(ArtSourcePaths.Resolve(portrait)))AddImage(plan,portrait);
                    if(data.ppu<=0||data.rearVariant||data.auxiliary||root.EndsWith("/ExtraClips",StringComparison.Ordinal))continue;
                    bool idleCrop=data.task!=null&&data.task.StartsWith("M",StringComparison.Ordinal)&&int.TryParse(data.task.Substring(1),out int n)&&n<=10;
                    var idle=Array.Find(data.clips,c=>c.key=="idle");
                    if(idleCrop)
                    {
                        if(idle==null||idle.cell<=0)throw new InvalidDataException("Missing idle portrait crop: "+root);
                        portrait=PageRelative(root,idle.pages[0].file);
                    }
                    if(!File.Exists(ArtSourcePaths.Resolve(portrait)))continue;
                    string preview=root+"/card-preview.png";
                    plan.Previews.Add(preview,new Preview{Source=portrait,Idle=idleCrop?idle:null});
                    plan.Files.Add(preview+".ycart",Path.Combine(Cache,preview+".ycart"));
                }
            }
            const string coin="ShopTrial/originium.png";
            RequireSource(coin);plan.Files.Add(coin,ArtSourcePaths.Resolve(coin));
            return plan;
        }
        // Read-only and deterministic: provenance JSON, loose PNGs and stale cache entries never enter this list.
        public static string[] RuntimeOutputFiles()
        {var plan=CreatePlan();var result=new string[plan.Files.Count];plan.Files.Keys.CopyTo(result,0);return result;}
        [MenuItem("Yesterday Convoy/Art/Validate Classified Runtime Sources")]
        public static void ValidateRuntimeSources()
        {
            var plan=CreatePlan();
            Debug.Log("CLASSIFIED_ART_SOURCE_PASS: manifests="+plan.Manifests+"; source_png="+plan.Images.Count+"; previews="+plan.Previews.Count+"; runtime_files="+plan.Files.Count+"; raw_ui_png=1; provenance_records_excluded=true");
        }
        [MenuItem("Yesterday Convoy/Art/Prepare Actor Textures")]
        public static void PrepareAll()=>Prepare(true);
        static void Prepare(bool strict)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();int changed=0;long hashed=0;var plan=CreatePlan();
            foreach(string relative in plan.Images)
            {
                string file=ArtSourcePaths.Resolve(relative),target=Path.Combine(Cache,relative+".ycart");
                var info=new FileInfo(file);string stamp=info.Length+"|"+info.LastWriteTimeUtc.Ticks;
                string record=File.Exists(target+".hash")?File.ReadAllText(target+".hash"):"";
                if(!strict&&File.Exists(target)&&record.EndsWith("\n"+stamp,StringComparison.Ordinal))continue;
                byte[] source=File.ReadAllBytes(file);hashed+=source.Length;string hash=Hash(source);
                if(File.Exists(target)&&(record==hash||record.StartsWith(hash+"\n",StringComparison.Ordinal))){File.WriteAllText(target+".hash",hash+"\n"+stamp);continue;}
                var texture=Decode(file);try{Write(target,texture);File.WriteAllText(target+".hash",hash+"\n"+stamp);changed++;}finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            foreach(var pair in plan.Previews)
            {
                string source=ArtSourcePaths.Resolve(pair.Value.Source),target=Path.Combine(Cache,pair.Key+".ycart");
                string hash=Hash(File.ReadAllBytes(source))+"-crop-v2|"+(pair.Value.Idle==null?"portrait":"idle-cell="+pair.Value.Idle.cell);
                if(File.Exists(target)&&File.Exists(target+".hash")&&File.ReadAllText(target+".hash")==hash)continue;
                var texture=Decode(source);Texture2D crop=null;
                try
                {
                    if(pair.Value.Idle!=null)
                    {
                        int cell=pair.Value.Idle.cell,lo=cell,hi=-1,left=cell,right=-1;var pixels=texture.GetPixels32();int bottom=texture.height-cell;
                        if(cell>texture.width||bottom<0)throw new InvalidDataException("Invalid idle portrait crop: "+pair.Key);
                        for(int y=0;y<cell;y++)for(int x=0;x<cell;x++)if(pixels[(bottom+y)*texture.width+x].a>8){lo=Math.Min(lo,y);hi=Math.Max(hi,y);left=Math.Min(left,x);right=Math.Max(right,x);}
                        if(hi<lo)throw new InvalidDataException("Empty idle portrait");
                        left=Math.Max(0,left-2);right=Math.Min(cell-1,right+2);lo=Math.Max(0,lo-2);hi=Math.Min(cell-1,hi+2);
                        int width=right-left+1,height=hi-lo+1;crop=new Texture2D(width,height,TextureFormat.RGBA32,false);var dst=new Color32[width*height];
                        for(int y=0;y<height;y++)Array.Copy(pixels,(bottom+lo+y)*texture.width+left,dst,y*width,width);crop.SetPixels32(dst);crop.Apply(false,false);
                    }
                    Write(target,crop??texture);File.WriteAllText(target+".hash",hash);changed++;
                }
                finally{if(crop!=null)UnityEngine.Object.DestroyImmediate(crop);UnityEngine.Object.DestroyImmediate(texture);}
            }
            if(strict)verifiedThisSession=true;
            Debug.Log("PREPARED_ART_PASS: source_png="+plan.Images.Count+"; previews="+plan.Previews.Count+"; rebuilt="+changed+"; hash_bytes="+hashed+"; strict="+strict+"; elapsed_ms="+watch.ElapsedMilliseconds);
        }
        static bool SameOrInside(string candidate,string folder)
        {
            string path=Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            string root=Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            return path.Equals(root,StringComparison.OrdinalIgnoreCase)||path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
        }
        static void RejectReparsePoints(string path)
        {
            for(var folder=new DirectoryInfo(path);folder!=null;folder=folder.Parent)
                if(folder.Exists&&(folder.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Runtime art output cannot contain a directory link: "+folder.FullName);
        }
        static string ExportDirectory(string executable)
        {
            if(string.IsNullOrWhiteSpace(executable)||!executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Expected the Windows Player executable path.",nameof(executable));
            string outputDir=Path.GetDirectoryName(Path.GetFullPath(executable));
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(SameOrInside(project,outputDir))throw new InvalidOperationException("Runtime art output must not be the project directory or an ancestor.");
            if(SameOrInside(outputDir,project)&&!SameOrInside(outputDir,Path.Combine(project,"Builds"))&&!SameOrInside(outputDir,Path.Combine(project,"Library")))
                throw new InvalidOperationException("In-project runtime art output must stay in Builds or Library.");
            string output=Path.GetFullPath(Path.Combine(outputDir,Path.GetFileNameWithoutExtension(executable)+"_Data","StreamingAssets"));
            if(!SameOrInside(output,outputDir)||SameOrInside(output,Application.dataPath))throw new InvalidOperationException("Runtime art output escapes its Player directory.");
            RejectReparsePoints(output);return output;
        }
        // A testable post-build entry. Validate every dependency before cleaning only our known Player subdirectories.
        public static void ExportRuntimeArt(string playerExecutablePath)
        {
            string output=ExportDirectory(playerExecutablePath);var plan=CreatePlan();
            foreach(var pair in plan.Files)
            {
                if(!File.Exists(pair.Value))throw new FileNotFoundException("Runtime art derivative is missing. Run Prepare Actor Textures before building: "+pair.Key,pair.Value);
                if(pair.Key.EndsWith(".ycart",StringComparison.Ordinal))
                    using(var reader=new BinaryReader(File.OpenRead(pair.Value)))
                        if(reader.BaseStream.Length<21||reader.ReadInt32()!=0x59434131)throw new InvalidDataException("Invalid prepared art: "+pair.Key);
            }
            // No cleanup can reach Assets: all canonical targets are checked below the resolved Player output.
            var managed=new List<string>(Roots){"ShopTrial"};
            foreach(string root in managed)
            {
                string target=Path.GetFullPath(Path.Combine(output,root));
                if(!SameOrInside(target,output)||target==output)throw new InvalidOperationException("Invalid managed art output: "+target);
                RejectReparsePoints(target);
                if(Directory.Exists(target))
                    foreach(string child in Directory.GetDirectories(target,"*",SearchOption.AllDirectories))RejectReparsePoints(child);
            }
            foreach(string root in managed)
            {string target=Path.Combine(output,root);if(Directory.Exists(target))Directory.Delete(target,true);}
            int prepared=0;
            foreach(var pair in plan.Files)
            {
                string target=Path.GetFullPath(Path.Combine(output,pair.Key));
                if(!SameOrInside(target,output))throw new InvalidOperationException("Invalid runtime art output: "+pair.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(pair.Value,target,true);
                if(pair.Key.EndsWith(".ycart",StringComparison.Ordinal))prepared++;
            }
            long bytes=0;foreach(var pair in plan.Files)bytes+=new FileInfo(Path.Combine(output,pair.Key)).Length;
            Debug.Log("PREPARED_ART_PLAYER_PASS: installed="+plan.Files.Count+"; prepared="+prepared+"; manifests="+plan.Manifests+"; raw_ui_png=1; runtime_art_bytes="+bytes+"; stale_cache_excluded=true; source PNGs retained in Art.");
        }
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes));}
        static Texture2D Decode(string file){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(file),false)){UnityEngine.Object.DestroyImmediate(texture);throw new InvalidDataException(file);}return texture;}
        static void Write(string path,Texture2D texture)
        {
            // Same compression and pixels as the runtime path, performed once while preparing the build.
            if(texture.width%4==0&&texture.height%4==0)texture.Compress(true);texture.Apply(false,false);var bytes=texture.GetRawTextureData<byte>().ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path));using(var file=File.Create(path))
            {
                using(var writer=new BinaryWriter(file,System.Text.Encoding.UTF8,true)){writer.Write(0x59434131);writer.Write(texture.width);writer.Write(texture.height);writer.Write((int)texture.format);writer.Write(bytes.Length);}
                using(var zip=new DeflateStream(file,System.IO.Compression.CompressionLevel.Fastest))zip.Write(bytes,0,bytes.Length);
            }
        }
    }
}
