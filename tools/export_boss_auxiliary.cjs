// Reuse the verified original Spine worker. Writes only the new BossRoster assets/catalogs.
const fs=require('fs'),path=require('path'),http=require('http'),crypto=require('crypto'),vm=require('vm');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'C:/Users/31016/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const source=path.resolve(process.argv[2]||''),args=process.argv.slice(3),outputAt=args.indexOf('--output-root');
const project=outputAt<0?path.resolve(__dirname,'..'):path.resolve(args[outputAt+1]);
if(outputAt>=0)args.splice(outputAt,2);
if(!fs.existsSync(path.join(source,'auxiliary/MODEL-REPORT.json')))throw Error('Pass the approved external preview directory');
let models=JSON.parse(fs.readFileSync(path.join(source,'auxiliary/MODEL-REPORT.json'),'utf8')).models;
const additions=[
 {task:'P12I',name:'仅剩的创意',folder:'native-fx/runtime-additions/Idea/source',alias:'enemy_2094_skzamb',nativeModel:'enemy_2094_skzamb',reference:'https://prts.wiki/w/仅剩的创意/spine'},
 {task:'P25M',name:'塑能基质',folder:'native-fx/runtime-additions/Matrix/source',alias:'enemy_10212_klhz',nativeModel:'enemy_10212_klhz',reference:'https://prts.wiki/w/塑能基质/spine'},
 {task:'P15C',name:'惘核（OB19）',folder:'native-fx/runtime-additions/CoreRed/source',alias:'enemy_10262_dsjred',nativeModel:'enemy_10262_dsjred',reference:'https://prts.wiki/w/“惘核”(OB19)/spine'},
 {task:'P24D',name:'殆因',folder:'native-fx/runtime-additions/CoreBlack/source',alias:'enemy_10263_dsjblk',nativeModel:'enemy_10263_dsjblk',reference:'https://prts.wiki/w/“殆因”/spine'},
 {task:'P20',name:'维维美·魂灵之影',folder:'native-fx/runtime-additions/Wward/source',alias:'model',nativeModel:'token_10035_wisdel_wward_sale#14',reference:JSON.parse(fs.readFileSync(path.join(source,'native-fx/runtime-additions/Wward/ACQUISITION.json'),'utf8')).url},
 {task:'P14H',name:'卡德霍·猎犬proto',folder:'native-fx/runtime-additions/HoundProto/source',alias:'enemy_2137_shsdgo',nativeModel:'enemy_2137_shsdgo',reference:'https://prts.wiki/w/猎犬proto/spine'}
];
for(const extra of additions.filter(e=>fs.existsSync(path.join(source,e.folder,e.alias+'.skel')))){
 const tokenSource=path.join(source,extra.folder);
 const context={console};vm.createContext(context);vm.runInContext(fs.readFileSync(path.join(source,'spine-webgl.js'),'utf8'),context);const sp=context.spine;
 const atlas=new sp.TextureAtlas(fs.readFileSync(path.join(tokenSource,extra.alias+'.atlas'),'utf8'),name=>{const p=fs.readFileSync(path.join(tokenSource,name));return{getImage:()=>({width:p.readUInt32BE(16),height:p.readUInt32BE(20)}),setFilters(){},setWraps(){},dispose(){}}});
 const raw=fs.readFileSync(path.join(tokenSource,extra.alias+'.skel')),loader=new sp.AtlasAttachmentLoader(atlas),data=raw[0]===123?new sp.SkeletonJson(loader).readSkeletonData(JSON.parse(raw.toString('utf8'))):new sp.SkeletonBinary(loader).readSkeletonData(new Uint8Array(raw)),s=new sp.Skeleton(data),animations=[];
 for(const a of data.animations){
  const bounds=[Infinity,Infinity,-Infinity,-Infinity];let measured=0;
  for(let i=0;i<=Math.ceil(a.duration*120);i++){
   s.setToSetupPose();s.setSkinByName('default');s.setSlotsToSetupPose();a.apply(s,0,Math.min(i/120,a.duration),false,[],1,sp.MixBlend.replace,sp.MixDirection.mixIn);s.updateWorldTransform();
   for(const slot of s.drawOrder){const at=slot.getAttachment();if(!at||!slot.bone.active||slot.color.a<.02||at.color?.a<.02)continue;let vv;if(at instanceof sp.RegionAttachment){vv=new Float32Array(8);at.computeWorldVertices(slot.bone,vv,0,2);}else if(at instanceof sp.MeshAttachment){vv=new Float32Array(at.worldVerticesLength);at.computeWorldVertices(slot,0,at.worldVerticesLength,vv,0,2);}else continue;
    for(let k=0;k<vv.length;k+=2){bounds[0]=Math.min(bounds[0],vv[k]);bounds[1]=Math.min(bounds[1],vv[k+1]);bounds[2]=Math.max(bounds[2],vv[k]);bounds[3]=Math.max(bounds[3],vv[k+1]);measured++;}}
  }
  const events=[];for(const tl of a.timelines)if(tl instanceof sp.EventTimeline)for(const e of tl.events)events.push({time:e.time,name:e.data.name,int:e.intValue,float:e.floatValue,string:e.stringValue});
  animations.push({name:a.name,duration:a.duration,bounds:measured?bounds:null,boundsSampleFPS:120,verticesMeasured:measured,events,loop:/idle|move|loop/i.test(a.name)});
 }
 models.push({task:extra.task,name:extra.name,reference:extra.reference,view:'main',model:extra.alias,nativeModel:extra.nativeModel,folder:extra.folder,version:data.version,skin:'default',animations});
}
const selected=new Set(args);if(selected.size)models=models.filter(m=>selected.has(m.task));
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const server=http.createServer((req,res)=>{let file=path.resolve(source,'.'+decodeURIComponent(new URL(req.url,'http://local').pathname));if(!file.startsWith(source+path.sep)){res.writeHead(403).end();return}fs.readFile(file,(e,b)=>{if(e){res.writeHead(404).end();return}res.setHeader('Content-Type',file.endsWith('.html')?'text/html':file.endsWith('.js')?'text/javascript':'application/octet-stream');res.end(b)})});
(async()=>{let browser;try{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 browser=await chromium.launch({headless:true,channel:'chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const page=await browser.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:'+server.address().port+'/model-worker.html');
 for(const original of models){
  const m={...original,folder:original.folder.startsWith('native-fx/')?original.folder:'auxiliary/'+original.folder,cell:384,captureMargin:1.14};
  await page.evaluate(m=>modelWorker.loadModel(m),m);
  const bounds=await page.evaluate(model=>{
   const b=[0,0,0,0],offset=new spine.Vector2(),size=new spine.Vector2(),s=active.skeleton;
   for(const clip of model.animations.filter(a=>a.duration>0)){
    const a=active.data.findAnimation(clip.name),count=Math.ceil(clip.duration*120)+1;
    for(let i=0;i<count;i++){
     s.setToSetupPose();s.setSkinByName(model.skin);s.setSlotsToSetupPose();s.color.set(1,1,1,1);
     a.apply(s,0,Math.min(i/120,clip.duration),false,[],1,spine.MixBlend.replace,spine.MixDirection.mixIn);s.updateWorldTransform();
     s.getBounds(offset,size,[]);
     if(Number.isFinite(offset.x)&&Number.isFinite(offset.y)&&Number.isFinite(size.x)&&Number.isFinite(size.y)&&size.x>0&&size.y>0){b[0]=Math.min(b[0],offset.x);b[1]=Math.min(b[1],offset.y);b[2]=Math.max(b[2],offset.x+size.x);b[3]=Math.max(b[3],offset.y+size.y);}
    }
   }return b;
  },m);
  const cell=384,columns=5,capacity=25,viewport=Math.max(bounds[2]-bounds[0],bounds[3]-bounds[1],50)*1.14,cx=(bounds[0]+bounds[2])/2,cy=(bounds[1]+bounds[3])/2;
  const out=path.join(project,'Assets/Art/Characters/Monsters/BossRoster',m.task,'main'),catalog=path.join(project,'Assets/Data/ArtCatalogs/BossRoster',m.task,'main');fs.mkdirSync(out,{recursive:true});fs.mkdirSync(catalog,{recursive:true});
  let alphaSampling=null;
  if(m.task==='P20')alphaSampling=await page.evaluate(bounds=>{
   const renderer=active.renderer,originalDraw=renderer.drawSkeleton,gl=active.gl;
   const pixels=()=>{const p=new Uint8Array(active.canvas.width*active.canvas.height*4);gl.readPixels(0,0,active.canvas.width,active.canvas.height,gl.RGBA,gl.UNSIGNED_BYTE,p);return p;};
   const compare=(a,b)=>{let changed=0,total=0,max=0;for(let i=0;i<a.length;i+=4){let diff=0;for(let c=0;c<4;c++)diff+=Math.abs(a[i+c]-b[i+c]);if(diff){changed++;total+=diff;max=Math.max(max,diff);}}return{changedPixels:changed,totalChannelDifference:total,maxPixelDifference:max};};
   modelWorker.renderAt('Idle',0,{bounds,loop:false});const uncorrected=pixels();
   renderer.drawSkeleton=function(s){return originalDraw.call(this,s,false);};modelWorker.renderAt('Idle',0,{bounds,loop:false});const straight=pixels();
   const textures=new Set(active.skeleton.slots.map(s=>s.getAttachment()?.region?.texture).filter(Boolean));
   for(const texture of textures){texture.bind();gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,true);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,texture.getImage());}
   renderer.drawSkeleton=originalDraw;modelWorker.renderAt('Idle',0,{bounds,loop:false});const uploadPma=pixels();
   // Keep PMA vertex colors and alpha accumulation, as the source SkeletonAnimation
   // requires. The old straight blend path also multiplies framebuffer alpha,
   // so use premultiplication at upload for this decoded texture's transparent bleed.
   gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,false);renderer.drawSkeleton=originalDraw;
   return{sourceMaterial:{pmaVertexColors:1,StraightAlphaInput:0,UseBleed:1,UseAlphaTex:0,slotBlendModes:active.data.slots.map(s=>s.blendMode)},
    selected:'GPU_UPLOAD_PREMULTIPLICATION_FOR_DECODED_UNITY_TRANSPARENT_BLEED',sourcePngUnmodified:true,
    oldPmaVsStraight:compare(uncorrected,straight),straightVsUploadPremultiplied:compare(straight,uploadPma)};
  },bounds);
  const manifest={version:1,task:m.task,name:m.name,view:'main',model:m.nativeModel||m.model,skin:m.skin,fps:60,cell,columns,captureBounds:bounds,captureMargin:1.14,viewport,pivotX:.5-cx/viewport,pivotY:.5-cy/viewport,scale:cell/viewport,status:'APPROVED_ORIGINAL_SOURCE_CAPTURE_NOT_YET_BOUND',clips:[],files:[],zeroDurationStates:m.animations.filter(a=>a.duration===0)};
  if(alphaSampling)manifest.alphaSampling=alphaSampling;
  const qa={task:m.task,boundsSampleFps:120,alphaSampling,clips:[],frames:0,visibleFrames:0,emptyFrames:0,edgeFrames:0,errors:[]};
  for(const clip of m.animations.filter(a=>a.duration>0)){
   const count=Math.ceil(clip.duration*60)+1,pages=[],key=clip.name.replace(/[^a-zA-Z0-9_-]/g,'_');let visible=0,empty=0,edge=0;
   for(let first=0;first<count;first+=capacity){
    const n=Math.min(capacity,count-first),result=await page.evaluate(({clip,first,n,bounds,cell,columns})=>{
     const out=document.createElement('canvas');out.width=columns*cell;out.height=Math.ceil(n/columns)*cell;const ctx=out.getContext('2d');let visible=0,empty=0,edge=0;const emptySourceStates=[];
     for(let i=0;i<n;i++){
      modelWorker.renderAt(clip.name,Math.min((first+i)/60,clip.duration),{bounds,loop:clip.loop});const q=modelWorker.pixels();
      if(q.visible)visible++;else{empty++;emptySourceStates.push({frame:first+i,time:Math.min((first+i)/60,clip.duration),skeletonAlpha:active.skeleton.color.a,opaqueAttachments:active.skeleton.drawOrder.filter(s=>(s.attachment instanceof spine.RegionAttachment||s.attachment instanceof spine.MeshAttachment)&&s.bone.active&&s.color.a*s.attachment.color.a*active.skeleton.color.a>.01).map(s=>s.data.name)});}if(q.edge)edge++;
      ctx.drawImage(active.canvas,i%columns*cell,Math.floor(i/columns)*cell);
     }return{uri:out.toDataURL('image/png'),visible,empty,edge,emptySourceStates};
    },{clip,first,n,bounds,cell,columns});
    const file=key+'-'+String(first/capacity).padStart(3,'0')+'.png',bytes=Buffer.from(result.uri.split(',')[1],'base64');fs.writeFileSync(path.join(out,file),bytes);
    pages.push({file,count:n,width:columns*cell,height:Math.ceil(n/columns)*cell});manifest.files.push({file,sha256:sha(bytes),bytes:bytes.length});visible+=result.visible;empty+=result.empty;edge+=result.edge;
    for(const state of result.emptySourceStates)qa.errors.push({source:clip.name,...state});
   }
   manifest.clips.push({key,source:clip.name,duration:clip.duration,count,fps:60,loop:clip.loop,cell,columns,pivotX:manifest.pivotX,pivotY:manifest.pivotY,events:clip.events,transparentWait:!clip.bounds,pages});
   qa.clips.push({source:clip.name,count,visibleFrames:visible,emptyFrames:empty,edgeFrames:edge});qa.frames+=count;qa.visibleFrames+=visible;qa.emptyFrames+=empty;qa.edgeFrames+=edge;
  }
  const idle=m.animations.find(a=>a.name==='Idle');await page.evaluate(({name,bounds})=>modelWorker.renderAt(name,0,{bounds,loop:false}),{name:idle.name,bounds});
  const uri=await page.evaluate(()=>modelWorker.png());fs.writeFileSync(path.join(out,'portrait.png'),Buffer.from(uri.split(',')[1],'base64'));
  qa.emptySourceStates=qa.errors;qa.errors=[...errors];qa.passed=errors.length===0&&qa.edgeFrames===0&&qa.emptySourceStates.every(s=>s.opaqueAttachments.length===0);
  fs.writeFileSync(path.join(catalog,'CAPTURE-MANIFEST.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(catalog,'CAPTURE-QA.json'),JSON.stringify(qa,null,2));
  if(m.nativeModel)fs.writeFileSync(path.join(catalog,'SOURCE-MODEL-REPORT.json'),JSON.stringify({...m,model:m.nativeModel,bounds},null,2));
  console.log(JSON.stringify({task:m.task,clips:manifest.clips.length,frames:qa.frames,emptyFrames:qa.emptyFrames,edgeFrames:qa.edgeFrames,passed:qa.passed}));
  if(!qa.passed)throw Error(m.task+' auxiliary capture needs inspection');
 }
}finally{if(browser)await browser.close();server.close();}})().catch(e=>{console.error(e);process.exitCode=1});
