using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Explicit standalone verification of the built payload; ordinary startup never installs this driver.
    public sealed class ArtPayloadEvidence:MonoBehaviour
    {
        static readonly string[] Roots={"DefenderArt","MonsterCombatArt","TalulahOriginal","OriginiumSlugOriginal","OriginalCombatVfx","TalulahUnboundVfx","BossRoster","BossRosterFx"};
        [Serializable] sealed class Manifest {public int version;public Clip[] clips;}
        [Serializable] sealed class Clip {public Page[] pages;}
        [Serializable] sealed class Page {public string file;}
        [Serializable] sealed class PackageResult {public string root;public int manifests,preparedPages;}
        [Serializable] sealed class Result
        {
            public bool passed,standalone,shopPngDecoded;
            public int manifests,preparedPages,pngFallbacks;
            public PackageResult[] packages;
            public string[] errors;
        }
        static bool started;
        readonly List<string> errors=new List<string>();
        string payloadRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(started||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-artPayloadSmokeTest")<0)return;
            started=true;
            var go=new GameObject("Built art payload verification");
            DontDestroyOnLoad(go);go.AddComponent<ArtPayloadEvidence>();
        }

        string Relative(string file)=>Path.GetRelativePath(payloadRoot,file).Replace('\\','/');
        PackageResult PackageFor(Result result,string relative)
        {
            int slash=relative.IndexOf('/');string root=slash<0?relative:relative.Substring(0,slash);
            foreach(var package in result.packages)if(package.root==root)return package;
            throw new InvalidDataException("Unexpected prepared art package: "+relative);
        }
        void Failure(string file,Exception error)
        {
            string message=file+": "+error.GetType().Name+": "+error.Message;
            errors.Add(message);Debug.LogError("ART_PAYLOAD_PAGE_FAIL "+message);
        }
        void InspectManifest(string file,Result result)
        {
            string relative=Relative(file);var package=PackageFor(result,relative);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(file));
            if(manifest==null||manifest.version!=1||manifest.clips==null||manifest.clips.Length==0)
                throw new InvalidDataException("Invalid runtime manifest.");
            foreach(var clip in manifest.clips)
            {
                if(clip?.pages==null||clip.pages.Length==0)throw new InvalidDataException("Manifest clip has no pages.");
                foreach(var page in clip.pages)
                {
                    if(page==null||string.IsNullOrEmpty(page.file)||Path.IsPathRooted(page.file)||page.file.Contains("\\")||
                        !page.file.EndsWith(".png",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid atlas path.");
                    foreach(string part in page.file.Split('/'))
                        if(part.Length==0||part=="."||part=="..")throw new InvalidDataException("Invalid atlas path.");
                    string atlas=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file),page.file));
                    if(!atlas.StartsWith(payloadRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Atlas escapes the payload directory.");
                    // The Talulah phase manifest retains unused native clips; only its selected pages ship.
                    if(package.root!="TalulahUnboundVfx"&&!File.Exists(atlas+".ycart"))
                        throw new FileNotFoundException("Manifest atlas was not packaged as prepared art.",Relative(atlas));
                }
            }
            package.manifests++;result.manifests++;
        }
        void InspectPage(string file,Result result)
        {
            string relative=Relative(file);var package=PackageFor(result,relative);
            var resource=new ArtMemoryCache.Resource{Owner="payload-verification"};
            int pngBefore=ArtMemoryCache.RuntimePngLoads,preparedBefore=ArtMemoryCache.PreparedLoads;
            try
            {
                var texture=ArtMemoryCache.Texture(relative.Substring(0,relative.Length-6),resource);
                int fallback=ArtMemoryCache.RuntimePngLoads-pngBefore;result.pngFallbacks+=fallback;
                if(texture==null||texture.width<=0||texture.height<=0)throw new InvalidDataException("Prepared texture has no dimensions.");
                if(fallback!=0||ArtMemoryCache.PreparedLoads-preparedBefore!=1)
                    throw new InvalidDataException("Page did not use exactly one prepared texture upload.");
                package.preparedPages++;result.preparedPages++;
            }
            finally{ArtMemoryCache.Dispose(resource);}
        }
        void InspectShop(Result result)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                string file=Path.Combine(payloadRoot,"ShopTrial/originium.png");
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(file),false)||texture.width<=0||texture.height<=0)
                    throw new InvalidDataException("Shop PNG could not be decoded.");
                result.shopPngDecoded=true;
            }
            finally{Destroy(texture);}
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var result=new Result{standalone=!Application.isEditor,packages=new PackageResult[Roots.Length]};
            for(int i=0;i<Roots.Length;i++)result.packages[i]=new PackageResult{root=Roots[i]};
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/art-payload-player");
            string[] manifests=Array.Empty<string>(),pages=Array.Empty<string>();
            try
            {
                payloadRoot=Path.GetFullPath(Application.streamingAssetsPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                if(!result.standalone)throw new InvalidOperationException("Run this probe in a standalone Player.");
                manifests=Directory.GetFiles(payloadRoot,"manifest.json",SearchOption.AllDirectories);
                pages=Directory.GetFiles(payloadRoot,"*.ycart",SearchOption.AllDirectories);
                Array.Sort(manifests,StringComparer.Ordinal);Array.Sort(pages,StringComparer.Ordinal);
            }
            catch(Exception error){Failure("payload inventory",error);}
            foreach(string file in manifests)
            {
                try{InspectManifest(file,result);}catch(Exception error){Failure(Relative(file),error);}
            }
            foreach(string file in pages)
            {
                try{InspectPage(file,result);}catch(Exception error){Failure(Relative(file),error);}
                yield return null; // Destroy each page before decoding the next one; never retain the entire payload.
            }
            try{InspectShop(result);}catch(Exception error){Failure("ShopTrial/originium.png",error);}
            foreach(var package in result.packages)
                if(package.manifests==0||package.preparedPages==0)errors.Add("Missing runtime package coverage: "+package.root);
            result.passed=result.standalone&&result.shopPngDecoded&&errors.Count==0;
            result.errors=errors.ToArray();
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            }
            catch(Exception error){Debug.LogException(error);result.passed=false;}
            Debug.Log((result.passed?"ART_PAYLOAD_PLAYER_PASS":"ART_PAYLOAD_PLAYER_FAIL")+
                ": manifests="+result.manifests+"; prepared_pages="+result.preparedPages+"; png_fallbacks="+result.pngFallbacks+"; errors="+errors.Count);
            Application.Quit(result.passed?0:1);
        }
    }
}
