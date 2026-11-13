using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    // The running game reads only Resources/NativeDefenders/Catalog and bundled
    // TextAssets. External paths are accepted exclusively by this import action.
    public static class OriginalNativeFxImportTools
    {
        const string Catalog="Assets/Data/ArtCatalogs/NativeDefenders/manifest.json";
        const string Root="Assets/Art/VFX/NativeDefenders/Resources/NativeDefenders";
        [Serializable] sealed class Package {public int version;public Bundle[] bundles;public Effect[] effects;}
        [Serializable] sealed class Bundle {public string id,resource,sha256;public long bytes;public string[] dependencies;}
        [Serializable] sealed class Effect {public string key,bundle,asset,sourcePrefab;public float duration;public Node[] nodes;public Renderer[] renderers;}
        [Serializable] sealed class Node {public int[] indices;public long transformPathId;}
        [Serializable] sealed class Renderer {public int[] indices;public string kind,path;public SourceMaterial[] materials;}
        [Serializable] sealed class SourceMaterial {public string sourceId,name,shader,resourceShader;}

        public static void Apply()
        {
            EnsureBuiltinReferences();
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-nativeFxPreparedDir");
            if(at<0||at+1>=args.Length)throw new ArgumentException("Supply -nativeFxPreparedDir with the prepared native-source package directory.");
            ApplyPrepared(args[at+1]);
        }
        public static void ApplyPrepared(string directory)
        {
            EnsureBuiltinReferences();
            string input=Path.GetFullPath(directory);var data=File.ReadAllBytes(Path.Combine(input,"manifest.json"));var package=Read(data);
            Folder(Path.GetDirectoryName(Catalog));Folder(Root+"/Bundles");
            foreach(var bundle in package.bundles)
            {
                string source=Path.Combine(input,"bundles",bundle.id+".bytes");CheckBundle(bundle,source);
                File.Copy(source,Root+"/Bundles/"+bundle.id+".bytes",true);
            }
            File.WriteAllBytes(Catalog,data);File.WriteAllBytes(Root+"/Catalog.bytes",data);
            string provenance=Path.Combine(input,"sources.json");
            if(File.Exists(provenance))File.Copy(provenance,"Assets/Data/ArtCatalogs/NativeDefenders/sources.json",true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate();Debug.Log("ORIGINAL_NATIVE_FX_IMPORT_PASS bundles="+package.bundles.Length+" effects="+package.effects.Length);
        }
        public static void EnsureBuiltinReferences()
        {
            // Must happen before loading Android source bundles with identical
            // Shader names. These Material references compile real Unity stock
            // shaders into Player and avoid ambiguous runtime Shader.Find.
            Folder(Root+"/Shaders");
            EnsureBuiltin("Legacy Shaders/Particles/Alpha Blended Premultiply","BuiltinPremultiply");
            EnsureBuiltin("Particles/Standard Unlit","BuiltinStandardUnlit");
            AssetDatabase.SaveAssets();
        }
        static void EnsureBuiltin(string name,string file)
        {
            string path=Root+"/Shaders/"+file+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=material!=null?material.shader:Shader.Find(name);
            string origin=shader!=null?AssetDatabase.GetAssetPath(shader):"";
            bool builtin=origin.IndexOf("unity_builtin_extra",StringComparison.OrdinalIgnoreCase)>=0||origin.IndexOf("unity default resources",StringComparison.OrdinalIgnoreCase)>=0;
            if(shader==null||shader.name!=name||!shader.isSupported||!builtin)
                throw new InvalidOperationException("Cannot bind genuine supported Unity stock shader '"+name+"' before native bundle loading. Resolved origin='"+origin+"'. Run import with a graphics device; never substitute the unsupported Android shader.");
            if(material==null){material=new Material(shader){name=file};AssetDatabase.CreateAsset(material,path);}
            else if(material.shader!=shader){material.shader=shader;EditorUtility.SetDirty(material);}
        }
        public static void Validate()
        {
            var source=File.ReadAllBytes(Catalog);var runtime=File.ReadAllBytes(Root+"/Catalog.bytes");
            if(!source.SequenceEqual(runtime))throw new InvalidDataException("Native source catalog differs from runtime Resources catalog.");
            var package=Read(source);var ids=new HashSet<string>(package.bundles.Select(b=>b.id),StringComparer.Ordinal);var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in package.bundles)
            {
                CheckBundle(b,Root+"/Bundles/"+b.id+".bytes");
                foreach(string dependency in b.dependencies??Array.Empty<string>())if(!ids.Contains(dependency)||dependency==b.id)throw new InvalidDataException("Invalid native bundle dependency "+dependency);
            }
            foreach(var effect in package.effects)
            {
                if(string.IsNullOrEmpty(effect.key)||!effect.key.StartsWith("defender/",StringComparison.Ordinal)||effect.key.StartsWith("defender/D27/",StringComparison.Ordinal)||!keys.Add(effect.key)||!ids.Contains(effect.bundle)||string.IsNullOrEmpty(effect.asset)||effect.asset.Contains("..")||!effect.asset.EndsWith(".prefab",StringComparison.Ordinal)||string.IsNullOrEmpty(effect.sourcePrefab)||effect.duration<=0||effect.nodes==null||effect.renderers==null)
                    throw new InvalidDataException("Invalid original native effect "+effect.key);
                foreach(var node in effect.nodes)if(node.indices==null||node.indices.Any(i=>i<0)||node.transformPathId==0)throw new InvalidDataException("Missing source Transform binding in "+effect.key);
                foreach(var renderer in effect.renderers)
                {
                    if(renderer.indices==null||renderer.materials==null)throw new InvalidDataException("Invalid renderer-slot binding "+effect.key);
                    foreach(var material in renderer.materials)if(material!=null&&(string.IsNullOrEmpty(material.sourceId)||string.IsNullOrEmpty(material.shader)||material.resourceShader!=null&&(material.resourceShader.Contains("..")||material.resourceShader.Contains("\\"))))
                        throw new InvalidDataException("Invalid serialized original material "+effect.key);
                }
            }
            Debug.Log("ORIGINAL_NATIVE_FX_VALIDATE_PASS bundles="+package.bundles.Length+" effects="+package.effects.Length+" bytes="+package.bundles.Sum(b=>b.bytes));
        }
        static Package Read(byte[] bytes)
        {
            var package=JsonUtility.FromJson<Package>(System.Text.Encoding.UTF8.GetString(bytes));
            if(package==null||package.version!=1||package.bundles==null||package.effects==null||package.bundles.Length==0||package.effects.Length==0||package.bundles.Select(b=>b.id).Distinct(StringComparer.Ordinal).Count()!=package.bundles.Length)throw new InvalidDataException("Invalid original native catalog.");
            // Match runtime handling of original null material slots. Unity's
            // JsonUtility creates empty DTOs for some null array elements.
            foreach(var effect in package.effects)foreach(var renderer in effect.renderers??Array.Empty<Renderer>())
                for(int slot=0;slot<(renderer.materials?.Length??0);slot++)
                {
                    var m=renderer.materials[slot];
                    if(m!=null&&m.sourceId==null&&m.name==null&&m.shader==null&&m.resourceShader==null)renderer.materials[slot]=null;
                }
            foreach(var bundle in package.bundles)if(string.IsNullOrEmpty(bundle.id)||bundle.id.Any(c=>!char.IsLetterOrDigit(c)&&c!='_')||bundle.resource!="NativeDefenders/Bundles/"+bundle.id||bundle.bytes<=0||bundle.bytes>=100000000||string.IsNullOrEmpty(bundle.sha256)||bundle.sha256.Length!=64)
                throw new InvalidDataException("Unsafe original native bundle record.");
            return package;
        }
        static void CheckBundle(Bundle row,string file)
        {
            var info=new FileInfo(file);if(!info.Exists||info.Length!=row.bytes)throw new InvalidDataException("Missing/truncated native bundle "+row.id);
            using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=row.sha256)throw new InvalidDataException("Native bundle SHA differs "+row.id);
        }
        static void Folder(string path)
        {
            path=path.Replace('\\','/');if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
    public sealed class OriginalNativeFxBuildCheck:IPreprocessBuildWithReport
    {
        public int callbackOrder=>-150;
        public void OnPreprocessBuild(BuildReport report){if(File.Exists("Assets/Data/ArtCatalogs/NativeDefenders/manifest.json"))OriginalNativeFxImportTools.Validate();}
    }
}
