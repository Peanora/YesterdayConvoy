// Re-sample the already acquired original Spine body, not an image upscaler.
// Uses the existing offline Spine 3.8 worker and an isolated local headless renderer.
const fs=require('fs'),path=require('path'),http=require('http'),crypto=require('crypto');
const args=process.argv.slice(2);
if(args.length<2)throw Error('Usage: node resample_formation_body.cjs <original-review-root> <output-directory> [playwright-module]');
const source=path.resolve(args[0]),out=path.resolve(args[1]);
const {chromium}=require(args[2]||'C:/Users/31016/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const model=JSON.parse(fs.readFileSync(path.join(source,'B08/model-report-main.json'),'utf8'));
const prior=JSON.parse(fs.readFileSync(path.join(source,'atlases/B08/main/manifest.json'),'utf8'));
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{
  const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\//,'');
  if(relative!=='model-worker.html'&&relative!=='spine-webgl.js'&&!relative.startsWith('B08/source/main/'))return res.writeHead(403).end();
  const file=path.resolve(source,relative);
  if(!file.startsWith(source+path.sep))return res.writeHead(403).end();
  fs.readFile(file,(error,data)=>{if(error)return res.writeHead(404).end();res.setHeader('Content-Type',relative.endsWith('.html')?'text/html':relative.endsWith('.js')?'text/javascript':relative.endsWith('.png')?'image/png':'application/octet-stream');res.end(data);});
});
(async()=>{
 let browser;try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  browser=await chromium.launch({headless:true,channel:'chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+'/model-worker.html');
  const manifest={version:1,task:'B08',method:'ORIGINAL_SPINE_PER_MOTION_CAPTURE',fps:60,worldUnitsPerSourceBone:.0045,
    note:'Original bones, source atlas pixels, source events and timings; fixed root pivot/PPU per motion. No AI enhancement, texture resizing or source animation retiming.',clips:[],files:[],alphaChecks:[]};
  let loadedCell=0;
  for(const clip of prior.clips){
   const sourceClip=model.animations.find(a=>a.name===clip.source);
   if(!sourceClip)throw Error('Source action disappeared: '+clip.source);
   const raw=sourceClip.bounds||model.bounds;
   const bounds=[Math.min(0,raw[0]),Math.min(0,raw[1]),Math.max(0,raw[2]),Math.max(0,raw[3])];
   const cell=clip.source.startsWith('B_')?1024:512,columns=2048/cell,capacity=columns*columns;
   const extent=Math.max(bounds[2]-bounds[0],bounds[3]-bounds[1],50),margin=1.14,viewport=extent*margin;
   const cx=(bounds[0]+bounds[2])/2,cy=(bounds[1]+bounds[3])/2;
   if(loadedCell!==cell){await page.evaluate(config=>modelWorker.loadModel(config),{...model,cell,captureBounds:bounds,captureMargin:margin});loadedCell=cell;}
   const pages=[];
   for(let first=0;first<clip.count;first+=capacity){
    const n=Math.min(capacity,clip.count-first),file='hq-'+clip.source+'-'+String(first/capacity).padStart(3,'0')+'.png';
    const uri=await page.evaluate(parameters=>modelWorker.renderPage(...parameters),[{name:clip.source,duration:clip.duration,loop:clip.loop,captureBounds:bounds},first,n,columns,60]);
    const data=Buffer.from(uri.split(',')[1],'base64');fs.writeFileSync(path.join(out,file),data);
    pages.push({file,count:n,width:columns*cell,height:Math.ceil(n/columns)*cell});manifest.files.push({file,sha256:sha(data),bytes:data.length});
   }
   const item={...clip,cell,columns,pivotX:.5-cx/viewport,pivotY:.5-cy/viewport,pixelsPerUnit:cell/viewport/.0045,
      captureBounds:bounds,captureMargin:margin,sourcePixelsPerBone:cell/viewport,pages};
   manifest.clips.push(item);
   for(const ratio of [0,.25,.5,.75,.999]){
    const alpha=await page.evaluate(({name,time,loop,bounds})=>{modelWorker.renderAt(name,time,{loop,bounds});return modelWorker.pixels();},{name:clip.source,time:clip.duration*ratio,loop:clip.loop,bounds});
    manifest.alphaChecks.push({source:clip.source,time:clip.duration*ratio,...alpha});
    if(alpha.edge>0)throw Error('Original body clips the capture edge: '+clip.source+' '+ratio);
    // Source transitions legitimately contain fully transparent frames. Preserve
    // them; report the alpha sample instead of inventing visible replacement art.
   }
   fs.writeFileSync(path.join(out,'capture.json'),JSON.stringify(manifest,null,2));
   console.log(JSON.stringify({source:clip.source,frames:clip.count,cell,pages:pages.length,ppu:item.pixelsPerUnit}));
  }
  if(errors.length)throw Error(errors.join('\n'));
  manifest.completed=true;manifest.sourceFiles=[];
  for(const name of [model.model+'.skel',model.model+'.atlas',model.model+'.png']){
   const file=path.join(source,model.folder,name),data=fs.readFileSync(file);manifest.sourceFiles.push({file:path.relative(source,file).replaceAll('\\','/'),sha256:sha(data),bytes:data.length});
  }
  fs.writeFileSync(path.join(out,'capture.json'),JSON.stringify(manifest,null,2));
  console.log('SOURCE_BODY_RESAMPLE_PASS '+manifest.clips.length+' actions '+manifest.files.length+' pages');
 }finally{if(browser)await browser.close();server.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
