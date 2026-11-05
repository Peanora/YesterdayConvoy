"""Pack approved native captures, preserving encoded RGB even at alpha zero.

Run with --source <external V13 directory>. --allow-missing supports the first
batch while extra captures are still running; a complete run is strict.
This tool never launches Unity or modifies source captures.
"""
from pathlib import Path
import argparse,hashlib,io,json,math,uuid
from PIL import Image

BOUND={"attack-A-caster","attack-A-hit","ambient-A-foot","movement-A-foot","current-firecast-A-hand"}
NATIVE_TO_GAME=1.60714285694
MAX_PAGE=2048

def write_bytes(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    if not path.exists() or path.read_bytes()!=data:path.write_bytes(data)

def write_json(path,data):
    write_bytes(path,(json.dumps(data,ensure_ascii=False,indent=2)+"\n").encode("utf8"))

def meta(path,project,folder=False):
    target=path.with_name(path.name+".meta")
    if target.exists():return
    guid=uuid.uuid5(uuid.NAMESPACE_URL,"YesterdayConvoy/"+path.relative_to(project).as_posix()).hex
    text="fileFormatVersion: 2\nguid: "+guid+"\n"
    # PNGs intentionally omit TextureImporter: Unity writes its current schema.
    # Never emit an obsolete serializedVersion, or rewrite an existing meta/GUID.
    if folder:text+="folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix==".json":text+="TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    write_bytes(target,text.encode("utf8"))

def entries(source,core_only=False):
    plan=source/"runtime-sampling-v1"
    batch=json.loads((plan/("jobs.json" if core_only else "all-new-jobs.json")).read_text(encoding="utf8"))["jobs"]
    result=[]
    for job in batch:
        source_key=job["output"].split("/")[-1]
        key="firecast-A-hand" if source_key=="current-firecast-A-hand" else source_key
        result.append((key,source/"native-probe"/job["output"],source_key in BOUND,job))
    full=json.loads((plan/"bindings-full.json").read_text(encoding="utf8"))
    for item in full["resources"]:
        if item.get("kind")!="FX_EXISTING_NATIVE_RESULT":continue
        if item["family"]=="ORB":key="orb-"+item["id"].removeprefix("existing-orb-")
        else:key="original-"+item["id"].removeprefix("existing-v13-")
        folder=Path(item["folder"])
        # Resolve recorded external references against this source tree, not an old checkout.
        folder=source/folder.relative_to(source)
        result.append((key,folder,False,None))
    if len({r[0] for r in result})!=len(result):raise ValueError("Duplicate runtime clip key")
    return result

def pack_clip(key,folder,bound,job,out,project,source):
    report_path=folder/"report.json"
    report=json.loads(report_path.read_text(encoding="utf8"))
    if report.get("status")!="PASS" or report.get("edgePixels")!=0 or report.get("clippedPixels")!=0:
        raise ValueError(f"{key}: requires PASS/edgePixels=0/clippedPixels=0: {report_path}")
    shaders=report.get("shaders",[])
    if not shaders or any("supported=True" not in shader for shader in shaders):raise ValueError(f"{key}: original shader support is unverified")
    count=int(report["frames"]);size=int(report["size"]);fps=float(report["fps"])
    if count<=0 or size<=0 or fps!=30:raise ValueError(f"{key}: invalid capture dimensions/fps")
    if job is None:job=json.loads((folder/"job.json").read_text(encoding="utf8"))
    if float(job["fps"])!=30 or not math.isclose(float(job["duration"])*30,count,abs_tol=.001):
        raise ValueError(f"{key}: source duration/frame count mismatch")
    left=max(0,int(report["contentMinX"])-2);right=min(size,int(report["contentMaxX"])+3)
    top=max(0,size-int(report["contentMaxY"])-3);bottom=min(size,size-int(report["contentMinY"])+2)
    crop_width,crop_height=right-left,bottom-top
    # BC7 is block-compressed. Unity can truncate non-4-aligned imported texture
    # dimensions, losing the last logical sprite row. Pad each cell on right/bottom;
    # preserve every original encoded pixel at the top-left without resampling.
    width,height=(crop_width+3)//4*4,(crop_height+3)//4*4
    if not 0<width<=MAX_PAGE or not 0<height<=MAX_PAGE:raise ValueError(f"{key}: invalid whole-clip crop")
    pivot_x=(right-float(report["pivotX"])*size)/width
    pivot_y=(height+top-(1-float(report["pivotY"]))*size)/height
    ppu=float(report["pixelsPerWorldUnit"])/NATIVE_TO_GAME
    if not ppu>0:raise ValueError(f"{key}: invalid orthographic capture PPU")
    columns=min(MAX_PAGE//width,count);rows_capacity=MAX_PAGE//height;per_page=columns*rows_capacity
    pages=[];source_hash=hashlib.sha256();page_info=[];checked=0
    for first in range(0,count,per_page):
        page_count=min(per_page,count-first);rows=math.ceil(page_count/columns)
        atlas=Image.new("RGBA",(columns*width,rows*height),(0,0,0,0))
        for i in range(page_count):
            path=folder/f"{first+i:04d}-hdr.png";data=path.read_bytes();source_hash.update(data)
            with Image.open(io.BytesIO(data)) as frame:
                if frame.mode!="RGBA" or frame.size!=(size,size):raise ValueError(f"{key}: unexpected source PNG format {path}")
                cropped=frame.crop((left,top,right,bottom)).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            x,y=i%columns*width,i//columns*height
            # No alpha mask: copying with a mask would destroy original additive RGB at A=0.
            atlas.paste(cropped,(x,y))
            if atlas.crop((x,y,x+crop_width,y+crop_height)).tobytes()!=cropped.tobytes():raise ValueError(f"{key}: encoded pixel copy failed")
            checked+=1
        name=f"{key}-{len(pages):02d}.png";output=out/name
        data=io.BytesIO();atlas.save(data,format="PNG",compress_level=6);binary=data.getvalue();write_bytes(output,binary);meta(output,project)
        with Image.open(output) as saved:
            if saved.mode!="RGBA" or saved.size!=atlas.size or saved.tobytes()!=atlas.tobytes():raise ValueError(f"{key}: lossless atlas roundtrip failed")
        pages.append(dict(file=name,count=page_count))
        page_info.append(dict(file=name,bytes=len(binary),width=atlas.width,height=atlas.height,sha256=hashlib.sha256(binary).hexdigest()))
    clip=dict(key=key,count=count,width=width,height=height,columns=columns,ppu=ppu,pivotX=pivot_x,pivotY=pivot_y,
              duration=float(job["duration"]),hdrRange=float(report["hdrRange"]),status="BOUND" if bound else "IMPORTED_NOT_BOUND",pages=pages)
    info=dict(key=key,status=clip["status"],source=folder.relative_to(source).as_posix(),sourceCount=count,
              sourceCrop=[left,top,right,bottom],sourcePPU=float(report["pixelsPerWorldUnit"]),runtimePPU=ppu,
              cellAlignment=4,paddingRight=width-crop_width,paddingBottom=height-crop_height,
              runtimeCellWidth=width,runtimeCellHeight=height,
              sourcePivot=[report["pivotX"],report["pivotY"]],runtimePivot=[pivot_x,pivot_y],mirrorX=True,
              sourceFrameSetSha256=source_hash.hexdigest(),sourceReportSha256=hashlib.sha256(report_path.read_bytes()).hexdigest(),
              encodedFramesLosslesslyChecked=checked,pages=page_info)
    return clip,info

def verify_package(project,source):
    """Read-only source-to-atlas verification; never creates files or metas."""
    roots=[project/"Assets/Art/VFX/Skills/Talulah/Resources/TalulahNative",project/"Assets/StreamingAssets/TalulahUnboundVfx"]
    package=json.loads((roots[1]/"package-report.json").read_text(encoding="utf8"))
    infos={i["key"]:i for i in package["clips"]};clip_count=page_count=frame_count=0;png_bytes=0
    fields={"key","count","width","height","columns","ppu","pivotX","pivotY","duration","hdrRange","status","pages"}
    for root in roots:
        manifest=json.loads((root/"manifest.json").read_text(encoding="utf8"));referenced=set()
        if set(manifest)!={"version","fps","clips"} or manifest["version"]!=1 or manifest["fps"]!=30:raise ValueError("Manifest schema mismatch")
        for clip in manifest["clips"]:
            if set(clip)!=fields:raise ValueError(f"{clip['key']}: clip schema mismatch")
            info=infos[clip["key"]];folder=source/info["source"];report_path=folder/"report.json";report=json.loads(report_path.read_text(encoding="utf8"))
            if report.get("status")!="PASS" or report.get("edgePixels")!=0 or report.get("clippedPixels")!=0:raise ValueError("Unverified source capture")
            if hashlib.sha256(report_path.read_bytes()).hexdigest()!=info["sourceReportSha256"]:raise ValueError("Source report changed")
            width,height=clip["width"],clip["height"];left,top,right,bottom=info["sourceCrop"];cw,ch=right-left,bottom-top;size=report["size"]
            if width%4 or height%4 or width!=(cw+3)//4*4 or height!=(ch+3)//4*4:raise ValueError(f"{clip['key']}: BC7 cell alignment mismatch")
            px=(right-report["pivotX"]*size)/width;py=(height+top-(1-report["pivotY"])*size)/height
            if not math.isclose(clip["pivotX"],px,abs_tol=1e-10) or not math.isclose(clip["pivotY"],py,abs_tol=1e-10):raise ValueError("Padded pivot mismatch")
            if not math.isclose(clip["ppu"],report["pixelsPerWorldUnit"]/NATIVE_TO_GAME,abs_tol=1e-10):raise ValueError("Runtime PPU changed")
            if clip["status"]!=("BOUND" if root==roots[0] else "IMPORTED_NOT_BOUND"):raise ValueError("Binding status mismatch")
            source_hash=hashlib.sha256();n=0
            page_infos={p["file"]:p for p in info["pages"]}
            for page in clip["pages"]:
                path=root/page["file"];data=path.read_bytes();referenced.add(page["file"])
                if hashlib.sha256(data).hexdigest()!=page_infos[page["file"]]["sha256"]:raise ValueError("Atlas PNG changed")
                with Image.open(io.BytesIO(data)) as atlas:
                    if atlas.mode!="RGBA" or atlas.width%4 or atlas.height%4 or max(atlas.size)>2048:raise ValueError("BC7 page alignment/format mismatch")
                    if atlas.width!=clip["columns"]*width or atlas.height%height or page["count"]>clip["columns"]*(atlas.height//height):raise ValueError("Sprite grid capacity mismatch")
                    for i in range(page["count"]):
                        frame_bytes=(folder/f"{n:04d}-hdr.png").read_bytes();source_hash.update(frame_bytes)
                        with Image.open(io.BytesIO(frame_bytes)) as frame:expected=frame.crop((left,top,right,bottom)).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                        x,y=i%clip["columns"]*width,i//clip["columns"]*height
                        if atlas.crop((x,y,x+cw,y+ch)).tobytes()!=expected.tobytes():raise ValueError("Original encoded pixel mismatch")
                        if width>cw:
                            padding=atlas.crop((x+cw,y,x+width,y+height)).tobytes()
                            if padding!=bytes(len(padding)):raise ValueError("Right padding is not zero")
                        if height>ch:
                            padding=atlas.crop((x,y+ch,x+width,y+height)).tobytes()
                            if padding!=bytes(len(padding)):raise ValueError("Bottom padding is not zero")
                        n+=1
                page_count+=1;png_bytes+=len(data)
            if n!=clip["count"] or n!=report["frames"] or source_hash.hexdigest()!=info["sourceFrameSetSha256"]:raise ValueError("Frame count/source hash mismatch")
            clip_count+=1;frame_count+=n
        if referenced!={p.name for p in root.glob("*.png")}:raise ValueError("Stale/unreferenced atlas page")
    if (page_count,frame_count,png_bytes)!=(package["pages"],package["frames"],package["pngBytes"]):raise ValueError("Package totals mismatch")
    print("TALULAH_NATIVE_VERIFY_PASS",json.dumps(dict(clips=clip_count,pages=page_count,frames=frame_count,pngBytes=png_bytes,cellAlignment=4,readOnly=True,unityRun=False)))

def main():
    ap=argparse.ArgumentParser();ap.add_argument("--source",type=Path,required=True);ap.add_argument("--project",type=Path,default=Path(__file__).resolve().parents[1])
    ap.add_argument("--allow-missing",action="store_true");ap.add_argument("--core-only",action="store_true",help="Package original 14 jobs plus existing references; no extra batch")
    ap.add_argument("--verify-only",action="store_true",help="Read-only source/PNG/schema/grid/padding/pivot verification; no writes")
    args=ap.parse_args();source=args.source.resolve();project=args.project.resolve()
    if args.verify_only:
        verify_package(project,source);return
    bound_root=project/"Assets/Art/VFX/Skills/Talulah/Resources/TalulahNative"
    unbound_root=project/"Assets/StreamingAssets/TalulahUnboundVfx"
    for root in (bound_root,unbound_root):root.mkdir(parents=True,exist_ok=True);meta(root,project,True)
    ready=[];missing=[]
    for entry in entries(source,args.core_only):
        if not (entry[1]/"report.json").exists():missing.append(entry[0])
        else:ready.append(entry)
    if missing and not args.allow_missing:raise FileNotFoundError("Captures not ready: "+", ".join(missing))
    manifests={True:dict(version=1,fps=30,clips=[]),False:dict(version=1,fps=30,clips=[])};infos=[]
    for key,folder,bound,job in ready:
        clip,info=pack_clip(key,folder,bound,job,bound_root if bound else unbound_root,project,source)
        manifests[bound]["clips"].append(clip);infos.append(info)
        print("PACKED",key,clip["count"],f"{clip['width']}x{clip['height']}",len(clip["pages"]),sum(p["bytes"] for p in info["pages"]),flush=True)
    for is_bound,root in [(True,bound_root),(False,unbound_root)]:
        write_json(root/"manifest.json",manifests[is_bound]);meta(root/"manifest.json",project)
    summary=dict(status="PARTIAL" if args.core_only or missing else "PASS",unityRun=False,missing=missing,
        boundClips=[c["key"] for c in manifests[True]["clips"]],unboundClips=[c["key"] for c in manifests[False]["clips"]],
        missingBoundClips=sorted(({"firecast-A-hand"} | (BOUND-{"current-firecast-A-hand"}))-{c["key"] for c in manifests[True]["clips"]}),
        frames=sum(i["sourceCount"] for i in infos),pages=sum(len(i["pages"]) for i in infos),pngBytes=sum(p["bytes"] for i in infos for p in i["pages"]),
        boundPngBytes=sum(p["bytes"] for i in infos if i["status"]=="BOUND" for p in i["pages"]),
        unboundPngBytes=sum(p["bytes"] for i in infos if i["status"]=="IMPORTED_NOT_BOUND" for p in i["pages"]),
        cellAlignment=4,nativeToGameScale=NATIVE_TO_GAME,encoding="Original sqrt(RGB/hdrRange) plus effective alpha; unchanged encoded PNG values; right/bottom zero padding",clips=infos)
    report=unbound_root/"package-report.json";write_json(report,summary);meta(report,project)
    print("TALULAH_NATIVE_PACKAGE_"+summary["status"],json.dumps({k:v for k,v in summary.items() if k!="clips"},ensure_ascii=False),flush=True)

if __name__=="__main__":main()
