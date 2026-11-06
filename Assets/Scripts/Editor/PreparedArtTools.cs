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
    // Deterministic build derivative. Source PNGs and GUIDs stay in Git, compiled blocks stay in Library/Builds.
    public sealed class PreparedArtTools:IPreprocessBuildWithReport,IPostprocessBuildWithReport
    {
        public int callbackOrder=>-100;
        static bool verifiedThisSession;
        public void OnPreprocessBuild(BuildReport report)=>Prepare(!verifiedThisSession);
        public void OnPostprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.StandaloneWindows64)return;
            string output=Path.Combine(Path.GetDirectoryName(report.summary.outputPath),Path.GetFileNameWithoutExtension(report.summary.outputPath)+"_Data/StreamingAssets");
            int installed=0,replaced=0;var phasePages=TalulahPhasePages();
            foreach(string file in Directory.GetFiles(Cache,"*.ycart",SearchOption.AllDirectories))
            {
                string relative=file.Substring(Cache.Length+1);
                if(relative.Replace('\\','/').StartsWith("TalulahUnboundVfx/",StringComparison.Ordinal)&&!phasePages.Contains(Path.GetFileName(relative.Substring(0,relative.Length-6))))continue;
                string target=Path.Combine(output,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,true);
                installed++;string source=target.Substring(0,target.Length-6);if(File.Exists(source)){File.Delete(source);replaced++;} // Player copy only; never touches Assets.
            }
            long bytes=0;foreach(string file in Directory.GetFiles(Path.GetDirectoryName(report.summary.outputPath),"*",SearchOption.AllDirectories))bytes+=new FileInfo(file).Length;
            Debug.Log("PREPARED_ART_PLAYER_PASS: installed="+installed+"; actor_png_replaced="+replaced+"; player_directory_bytes="+bytes+"; source PNGs retained in project.");
        }
        static string Cache=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt"));
        static readonly string[] Roots={"MonsterCombatArt","DefenderArt","TalulahOriginal","OriginiumSlugOriginal","TalulahUnboundVfx","OriginalCombatVfx"};
        static readonly string[] TalulahPhaseKeys={"attack-B-caster","attack-B-hit","ambient-B-foot","movement-B-foot","ambient-B-head","ambient-B-wing","first_transition_start","first_transition_end","final_transition_start","final_transition_end","ignite-B-caster"};
        [InitializeOnLoadMethod]static void InstallPreparation()
        {EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingEditMode)Prepare(false);};}
        [Serializable]sealed class Manifest {public string task;public float ppu;public bool rearVariant,auxiliary;public Clip[] clips;}
        [Serializable]sealed class Clip {public string key;public int cell,cellWidth,cellHeight;public Page[] pages;}
        [Serializable]sealed class Page {public string file;}
        static HashSet<string> TalulahPhasePages()
        {
            string path=Path.Combine(Application.streamingAssetsPath,"TalulahUnboundVfx/manifest.json");
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));var pages=new HashSet<string>(StringComparer.Ordinal);
            foreach(string key in TalulahPhaseKeys)
            {
                var clip=Array.Find(data.clips,c=>c.key==key);
                if(clip?.pages==null||clip.pages.Length==0)throw new InvalidDataException("Missing required Talulah phase VFX: "+key);
                foreach(var page in clip.pages)
                {
                    if(string.IsNullOrEmpty(page.file)||Path.GetFileName(page.file)!=page.file)throw new InvalidDataException("Invalid Talulah phase VFX page");
                    pages.Add(page.file);
                }
            }
            return pages;
        }
        [MenuItem("Yesterday Convoy/Art/Prepare Actor Textures")]
        public static void PrepareAll()=>Prepare(true);
        static void Prepare(bool strict)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();int changed=0,total=0;long hashed=0;
            foreach(string folder in Roots)
            {
                string root=Path.Combine(Application.streamingAssetsPath,folder);if(!Directory.Exists(root))continue;
                var phasePages=folder=="TalulahUnboundVfx"?TalulahPhasePages():null;
                foreach(string file in Directory.GetFiles(root,"*.png",SearchOption.AllDirectories))
                {
                    if(phasePages!=null&&!phasePages.Contains(Path.GetFileName(file)))continue;
                    string relative=file.Substring(Application.streamingAssetsPath.Length+1),target=Path.Combine(Cache,relative+".ycart");
                    var info=new FileInfo(file);string stamp=info.Length+"|"+info.LastWriteTimeUtc.Ticks;
                    string record=File.Exists(target+".hash")?File.ReadAllText(target+".hash"):"";total++;
                    if(!strict&&File.Exists(target)&&record.EndsWith("\n"+stamp,StringComparison.Ordinal))continue;
                    byte[] source=File.ReadAllBytes(file);hashed+=source.Length;string hash=Hash(source);
                    if(File.Exists(target)&&(record==hash||record.StartsWith(hash+"\n",StringComparison.Ordinal))){File.WriteAllText(target+".hash",hash+"\n"+stamp);continue;}
                    var texture=Decode(file);try{Write(target,texture);File.WriteAllText(target+".hash",hash+"\n"+stamp);changed++;}finally{UnityEngine.Object.DestroyImmediate(texture);}
                }
                string[] manifests=folder=="MonsterCombatArt"||folder=="DefenderArt"?Directory.GetFiles(root,"manifest.json",SearchOption.AllDirectories):new[]{Path.Combine(root,"manifest.json")};
                foreach(string manifest in manifests)
                {
                    if(!File.Exists(manifest)||Path.GetFileName(Path.GetDirectoryName(manifest))=="ExtraClips")continue;
                    var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));if(data==null||data.rearVariant||data.auxiliary)continue;
                    string dir=Path.GetDirectoryName(manifest),portrait=Path.Combine(dir,"portrait.png");bool idleCrop=data.task!=null&&data.task.StartsWith("M")&&int.TryParse(data.task.Substring(1),out int n)&&n<=10;
                    var idle=Array.Find(data.clips,c=>c.key=="idle");if(idleCrop)portrait=Path.Combine(dir,idle.pages[0].file);
                    if(!File.Exists(portrait))continue;
                    string target=Path.Combine(Cache,dir.Substring(Application.streamingAssetsPath.Length+1),"card-preview.png.ycart");string hash=Hash(File.ReadAllBytes(portrait))+"-crop-v1";
                    if(File.Exists(target)&&File.Exists(target+".hash")&&File.ReadAllText(target+".hash")==hash)continue;
                    var texture=Decode(portrait);Texture2D crop=null;
                    try
                    {
                        if(idleCrop)
                        {
                            int cell=idle.cell,lo=cell,hi=-1,left=cell,right=-1;var pixels=texture.GetPixels32();int bottom=texture.height-cell;
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
            }
            if(strict)verifiedThisSession=true;
            Debug.Log("PREPARED_ART_PASS: source_png="+total+"; rebuilt="+changed+"; hash_bytes="+hashed+"; strict="+strict+"; elapsed_ms="+watch.ElapsedMilliseconds);
        }
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes));}
        static Texture2D Decode(string file){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(file),false)){UnityEngine.Object.DestroyImmediate(texture);throw new InvalidDataException(file);}return texture;}
        static void Write(string path,Texture2D texture)
        {
            // Same compression API/quality as the old Player path, performed once while preparing the build.
            // Non-block-aligned tiny portraits stay RGBA32 to preserve their exact shape.
            if(texture.width%4==0&&texture.height%4==0)texture.Compress(true);texture.Apply(false,false);var bytes=texture.GetRawTextureData<byte>().ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path));using(var file=File.Create(path))
            {
                using(var writer=new BinaryWriter(file,System.Text.Encoding.UTF8,true)){writer.Write(0x59434131);writer.Write(texture.width);writer.Write(texture.height);writer.Write((int)texture.format);writer.Write(bytes.Length);}
                using(var zip=new DeflateStream(file,System.IO.Compression.CompressionLevel.Fastest))zip.Write(bytes,0,bytes.Length);
            }
        }
    }
}
