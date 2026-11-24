// Capture only the original game#9 April outfit using the existing local Spine worker.
// Usage: node capture_wisadel_april.cjs <existing-review-root> <WIS-source-root>
const fs=require('fs'),path=require('path'),http=require('http'),vm=require('vm'),crypto=require('crypto');
const [reviewArg,sourceArg]=process.argv.slice(2);
if(!reviewArg||!sourceArg)throw Error('Expected existing review and WIS-source directories');
const review=path.resolve(reviewArg),root=path.resolve(sourceArg),out=path.join(root,'capture');
const {chromium}=require('C:/Users/31016/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const model='char_1035_wisdel_game_9',folder=path.join(root,'source/front'),sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const context={console};vm.createContext(context);vm.runInContext(fs.readFileSync(path.join(review,'spine-webgl.js'),'utf8'),context);
const sp=context.spine,atlas=new sp.TextureAtlas(fs.readFileSync(path.join(folder,model+'.atlas'),'utf8'),name=>{
 const b=fs.readFileSync(path.join(folder,name));return{getImage:()=>({width:b.readUInt32BE(16),height:b.readUInt32BE(20)}),setFilters(){},setWraps(){},dispose(){}};
});
const data=new sp.SkeletonBinary(new sp.AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(path.join(folder,model+'.skel'))));
const skeleton=new sp.Skeleton(data),skin=data.findSkin('default')?'default':data.skins[0].name,animations=[];
for(const animation of data.animations){
 const bounds=[Infinity,Infinity,-Infinity,-Infinity];let measured=0;
 for(let i=0;i<=Math.ceil(animation.duration*60);i++){
  skeleton.setToSetupPose();skeleton.setSkinByName(skin);skeleton.setSlotsToSetupPose();
  animation.apply(skeleton,0,Math.min(animation.duration,i/60),false,[],1,sp.MixBlend.replace,sp.MixDirection.mixIn);skeleton.updateWorldTransform();
  for(const slot of skeleton.drawOrder){
   const a=slot.getAttachment();if(!a||slot.color.a<.02||(a.color&&a.color.a<.02)||!slot.bone.active)continue;
   let vertices;if(a instanceof sp.RegionAttachment){vertices=new Float32Array(8);a.computeWorldVertices(slot.bone,vertices,0,2);}
   else if(a instanceof sp.MeshAttachment){vertices=new Float32Array(a.worldVerticesLength);a.computeWorldVertices(slot,0,a.worldVerticesLength,vertices,0,2);}else continue;
   for(let j=0;j<vertices.length;j+=2){bounds[0]=Math.min(bounds[0],vertices[j]);bounds[1]=Math.min(bounds[1],vertices[j+1]);bounds[2]=Math.max(bounds[2],vertices[j]);bounds[3]=Math.max(bounds[3],vertices[j+1]);measured++;}
  }
 }
 const events=[];for(const timeline of animation.timelines)if(timeline instanceof sp.EventTimeline)for(const e of timeline.events)
  events.push({time:e.time,name:e.data.name,int:e.intValue,float:e.floatValue,string:e.stringValue});
 animations.push({name:animation.name,duration:animation.duration,bounds:measured?bounds:null,events,verticesMeasured:measured,
  loop:/idle|move|loop/i.test(animation.name)&&!/start|begin|end|die/i.test(animation.name)});
}
const report={task:'WIS_APRIL_G9',cardId:'CARD_WISADEL_APRIL',view:'main',name:'维维美 · 绝对主角',model,folder:'source/front',skin,
 version:data.version,bones:data.bones.length,slots:data.slots.length,animations};
fs.writeFileSync(path.join(root,'model-report.json'),JSON.stringify(report,null,2));fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{
 const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\//,'');
 const base=['model-worker.html','spine-webgl.js'].includes(relative)?review:root;
 if(base===root&&!relative.startsWith('source/front/'))return res.writeHead(403).end();
 const file=path.resolve(base,relative);if(!file.startsWith(base+path.sep))return res.writeHead(403).end();
 fs.readFile(file,(error,bytes)=>{if(error)return res.writeHead(404).end();res.setHeader('Content-Type',relative.endsWith('.html')?'text/html':relative.endsWith('.js')?'text/javascript':relative.endsWith('.png')?'image/png':'application/octet-stream');res.end(bytes);});
});
(async()=>{
 let browser;try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  browser=await chromium.launch({headless:true,channel:'chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const page=await browser.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+'/model-worker.html');
  await page.evaluate(config=>modelWorker.loadModel(config),{...report,cell:512});
  const captured={version:1,task:report.task,cardId:report.cardId,method:'ORIGINAL_SPINE_GAME9_60FPS',worldUnitsPerSourceBone:.0045,
   originalClientBoneScale:.0027,sourceFiles:[],clips:[],files:[],alphaChecks:[]};
  for(const a of animations.filter(x=>x.duration>0)){
   const bounds=[Math.min(0,a.bounds[0]),Math.min(0,a.bounds[1]),Math.max(0,a.bounds[2]),Math.max(0,a.bounds[3])];
   const cell=512,columns=4,capacity=16,margin=1.14,viewport=Math.max(bounds[2]-bounds[0],bounds[3]-bounds[1],50)*margin;
   const cx=(bounds[0]+bounds[2])/2,cy=(bounds[1]+bounds[3])/2,count=Math.ceil(a.duration*60)+1,pages=[];
   for(let first=0;first<count;first+=capacity){
    const n=Math.min(capacity,count-first),file=a.name+'-'+String(first/capacity).padStart(3,'0')+'.png';
    const uri=await page.evaluate(args=>modelWorker.renderPage(...args),[{name:a.name,duration:a.duration,loop:false,captureBounds:bounds},first,n,columns,60]);
    const bytes=Buffer.from(uri.split(',')[1],'base64');fs.writeFileSync(path.join(out,file),bytes);
    pages.push({file,count:n,width:columns*cell,height:Math.ceil(n/columns)*cell});captured.files.push({file,bytes:bytes.length,sha256:sha(bytes)});
   }
   captured.clips.push({key:a.name,source:a.name,count,duration:a.duration,fps:60,cell,columns,pivotX:.5-cx/viewport,pivotY:.5-cy/viewport,
    pixelsPerUnit:cell/viewport/.0045,loop:a.loop,transparentWait:false,events:a.events,captureBounds:bounds,pages});
   for(const ratio of [0,.25,.5,.75,.999]){
    const check=await page.evaluate(({name,time,bounds})=>{modelWorker.renderAt(name,time,{loop:false,bounds});return modelWorker.pixels();},{name:a.name,time:a.duration*ratio,bounds});
    captured.alphaChecks.push({source:a.name,time:a.duration*ratio,...check});if(check.edge>0)throw Error('Capture edge clipping: '+a.name);
   }
   await page.evaluate(({name,bounds})=>modelWorker.renderAt(name,0,{loop:false,bounds}),{name:a.name,bounds});
   fs.writeFileSync(path.join(out,a.name+'-first.png'),Buffer.from((await page.evaluate(()=>modelWorker.png())).split(',')[1],'base64'));
   fs.writeFileSync(path.join(out,'capture.json'),JSON.stringify(captured,null,2));console.log(a.name+' '+count+' frames '+pages.length+' pages');
  }
  if(errors.length)throw Error(errors.join('\n'));
  for(const name of [model+'.skel',model+'.atlas',model+'.png']){const bytes=fs.readFileSync(path.join(folder,name));captured.sourceFiles.push({file:'source/front/'+name,bytes:bytes.length,sha256:sha(bytes)});}
  captured.completed=true;fs.writeFileSync(path.join(out,'capture.json'),JSON.stringify(captured,null,2));
  console.log('WISADEL_APRIL_CAPTURE_PASS '+captured.clips.length+' clips '+captured.files.length+' pages');
 }finally{if(browser)await browser.close();server.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
