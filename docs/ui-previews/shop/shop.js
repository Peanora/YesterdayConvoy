(async function(){
 'use strict';
 const $=id=>document.getElementById(id),roles=window.SHOP_ROSTER,byId=new Map(roles.map(r=>[r.id,r]));
 const tags=window.SHOP_SYNERGIES||{};
 const mode=new URLSearchParams(location.search).get('demo')||'star2';
 const model=ShopModel.create(roles,{mode:['normal','star2','star3','upgrade'].includes(mode)?mode:'star2'});
 const pictures=new Map(),motion=matchMedia('(prefers-reduced-motion: reduce)').matches;
 let busy=false,selected=null,toastTimer,confirmIndex=null;
 const starSvg='<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 1.8 3.1 6.3 7 .9-5.1 5 .2 7-5.2-3-6.2 3 1.2-7-5.1-5 7-.9Z"/></svg>';
 const stars=n=>starSvg.repeat(n),colors={1:'116,125,136',2:'62,144,86',3:'47,111,181',4:'128,68,168',5:'172,129,46'};
 const coin=()=>'<img class="currency-icon" src="assets/originium-ui.png" alt="源石" draggable="false">';
 function el(tag,cls){const e=document.createElement(tag);if(cls)e.className=cls;return e;}
 function sleep(ms){return new Promise(r=>setTimeout(r,motion?0:ms));}
 function box(node){const a=node.getBoundingClientRect(),b=$('fx').getBoundingClientRect();return{x:a.x-b.x,y:a.y-b.y,width:a.width,height:a.height,cx:a.x-b.x+a.width/2,cy:a.y-b.y+a.height/2};}
 async function animate(node,keyframes,options){if(motion){node.remove();return;}try{await node.animate(keyframes,{fill:'forwards',...options}).finished;}catch{}node.remove();}
 function toast(message,error=false){clearTimeout(toastTimer);$('toast').textContent=message;$('toast').className='show'+(error?' error':'');toastTimer=setTimeout(()=>$('toast').className='',error?2400:1800);}
 async function portrait(r){
  if(pictures.has(r.id))return pictures.get(r.id);
  const p=new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>{const a=document.createElement('canvas');a.width=r.cell||img.width;a.height=r.cell||img.height;const ctx=a.getContext('2d',{willReadFrequently:true});ctx.drawImage(img,0,0,a.width,a.height,0,0,a.width,a.height);const pixels=ctx.getImageData(0,0,a.width,a.height).data;let l=a.width,t=a.height,right=-1,b=-1;for(let y=0;y<a.height;y++)for(let x=0;x<a.width;x++)if(pixels[(y*a.width+x)*4+3]>8){l=Math.min(l,x);t=Math.min(t,y);right=Math.max(right,x);b=Math.max(b,y);}if(right<0){reject(Error('Empty '+r.id));return;}l=Math.max(0,l-2);t=Math.max(0,t-2);right=Math.min(a.width-1,right+2);b=Math.min(a.height-1,b+2);const out=document.createElement('canvas');out.width=right-l+1;out.height=b-t+1;out.getContext('2d').drawImage(a,l,t,out.width,out.height,0,0,out.width,out.height);resolve(out.toDataURL('image/png'));};img.onerror=()=>reject(Error('Missing '+r.id));img.src=r.image;});pictures.set(r.id,p);return p;
 }
 function captureBench(){return new Map([...document.querySelectorAll('.bench-slot[data-instance]')].map(e=>[Number(e.dataset.instance),box(e)]));}
 function updateSelection(){
  const c=model.saleQuote(selected);$('sell').hidden=!c.ok;$('sell').disabled=busy;
  if(c.ok)$('sell').textContent='出售 '+c.name+' · '+c.value+' 源石';
  document.querySelectorAll('.bench-slot').forEach(e=>e.classList.toggle('selected',Number(e.dataset.instance)===selected));
 }
 async function render(){
  const s=model.snapshot();$('gold').textContent=s.gold;$('level').textContent=s.level;$('xpnum').textContent=s.level===10?'MAX':s.xp+'/'+s.xpRequired;$('xpbar').style.width=(s.level===10?100:s.xp/s.xpRequired*100)+'%';
  $('buyxp').disabled=busy||s.level===10||s.gold<4;$('buyxp').title=s.level===10?'已达到最高等级':s.gold<4?'源石不足，升级需要 4 源石':'4 源石获得 4 经验';$('refresh').disabled=busy||s.gold<2;$('refresh').title=s.gold<2?'源石不足，刷新需要 2 源石':s.locked?'已锁定，先解锁再刷新':'2 源石刷新本轮棋子';
  $('lock').disabled=busy;$('lock').setAttribute('aria-pressed',String(s.locked));$('lock').setAttribute('aria-label',s.locked?'解除锁定':'锁定本轮棋子');
  const shackle=s.locked?'M8 10V7a4 4 0 0 1 8 0v3':'M8 10V6.8a4 4 0 0 1 7.85-1.1';$('lock').innerHTML='<svg viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="'+shackle+'"/><circle cx="12" cy="15" r="1"/><path d="M12 16v2"/></svg>';
  $('odds').innerHTML='<b>下次刷新 · 等级 '+s.level+'</b>'+s.odds.map((p,i)=>'<p><span>'+(i+1)+' 费</span><span>'+p+'%</span></p>').join('')+'<small>升级保留当前商店 · 剩余库存加权抽取</small>';
  $('bench-count').textContent=s.bench.length;$('bench').replaceChildren();
  for(let i=0;i<10;i++){
   const c=s.bench[i],slot=el('button','bench-slot'+(c?'':' empty'));slot.disabled=busy||!c;
   if(c){const r=byId.get(c.id);slot.dataset.instance=c.instance;slot.dataset.star=c.star;slot.innerHTML='<span class="stars">'+stars(c.star)+'</span><img class="bench-portrait" alt="'+r.name+'" draggable="false"><span class="bench-name">'+r.name+'</span>';slot.setAttribute('aria-label',r.name+'，'+c.star+'星，可向下拖至商店出售');slot.title='向下拖至商店出售 · '+model.saleQuote(c.instance).value+' 源石';slot.querySelector('img').src=await portrait(r);slot.onclick=()=>{if(busy)return;selected=c.instance;updateSelection();};}else{slot.setAttribute('aria-label','空备战格 '+(i+1));}
   $('bench').appendChild(slot);
  }
  $('offers').replaceChildren();
  for(let i=0;i<5;i++){
   const o=s.offers[i],card=el('button','offer');card.dataset.slot=i;
   if(!o||o.sold){card.classList.add('purchased');card.disabled=true;card.setAttribute('aria-label',o?'已购买的空格':'暂无棋子');$('offers').appendChild(card);continue;}
   const r=byId.get(o.id),ready=s.bench.filter(c=>c.id===r.id&&c.star===1).length>=2;card.style.setProperty('--rarity-rgb',colors[r.cost]);card.disabled=busy||s.gold<r.cost;card.classList.toggle('merge-ready',ready);card.setAttribute('aria-label',r.name+'，'+r.cost+'源石'+(ready?'，购买可升星':''));card.title=(tags[r.id]||[]).join(' · ')+(ready?' · 购买可升星':'');
   if(s.gold<r.cost)card.title+=' · 源石不足';
   card.innerHTML='<img class="portrait" alt="'+r.name+'"><span class="info"><span class="name">'+r.name+'</span>'+(tags[r.id]||[]).map(t=>'<span class="tag">'+t+'</span>').join('')+'</span><span class="price">'+coin()+r.cost+'</span>';card.querySelector('.portrait').src=await portrait(r);card.onclick=()=>purchase(i);$('offers').appendChild(card);
  }
  updateSelection();
  const starred=s.bench.find(c=>c.id==='M08'&&c.star>1);
  $('hint').textContent=mode==='normal'?'同种同星三张自动合成':mode==='upgrade'?'点击升级，查看等级与经验变化':starred&&starred.star===(mode==='star3'?3:2)?'弩手已晋升'+(starred.star===3?'三星':'两星'):'再招募一名弩手，查看'+(mode==='star3'?'连锁三星':'两星合成');
  document.body.dataset.ready='true';
 }
 function spend(value,positive=false){const n=el('span','deduction');n.textContent=(positive?'+':'−')+value;$('money-fx').appendChild(n);animate(n,[{opacity:1,transform:'translateY(0)'},{opacity:0,transform:'translateY(-25px)'}],{duration:650});}
 async function fly(image,from,to,duration=400,delay=0){const ghost=el('img','flyer');ghost.src=image;Object.assign(ghost.style,{left:from.x+'px',top:from.y+'px',width:from.width+'px',height:from.height+'px'});$('fx').appendChild(ghost);return animate(ghost,[{opacity:.9,transform:'translate(0,0) scale(.9)'},{opacity:1,offset:.65},{opacity:0,transform:`translate(${to.cx-from.cx}px,${to.cy-from.cy}px) scale(.3)`}],{duration,delay,easing:'cubic-bezier(.4,0,.2,1)'});}
 async function burst(target,star,name){
  document.querySelectorAll('.star-banner').forEach(e=>e.remove());
  const gold=star===3,ring=el('div','merge-ring'+(gold?' gold':''));Object.assign(ring.style,{left:target.cx-47+'px',top:target.cy-47+'px'});$('fx').appendChild(ring);animate(ring,[{opacity:0,transform:'scale(.3)'},{opacity:1,offset:.3},{opacity:0,transform:'scale(1.8)'}],{duration:gold?1000:700});
  for(let i=0;i<(gold?24:14);i++){const p=el('i','particle'+(gold?' gold':'')),angle=i*Math.PI*2/(gold?24:14),radius=gold?95:65;Object.assign(p.style,{left:target.cx+'px',top:target.cy+'px'});$('fx').appendChild(p);animate(p,[{opacity:1,transform:'translate(0,0) rotate(45deg)'},{opacity:0,transform:`translate(${Math.cos(angle)*radius}px,${Math.sin(angle)*radius}px) rotate(180deg)`}],{duration:800,delay:i*5,easing:'cubic-bezier(.1,.7,.3,1)'});}
  const banner=el('div','star-banner'+(gold?' gold':''));banner.innerHTML='<span class="banner-stars">'+stars(star)+'</span>';Object.assign(banner.style,{left:Math.max(90,target.cx)+'px',top:target.y+target.height-30+'px'});$('fx').appendChild(banner);animate(banner,[{opacity:0,marginTop:'12px'},{opacity:1,marginTop:'0',offset:.18},{opacity:1,offset:.75},{opacity:0,marginTop:'-14px'}],{duration:gold?1750:1300});
 }
 async function purchase(index,confirmed=false){
  if(busy)return;const source=document.querySelector('.offer[data-slot="'+index+'"] .portrait'),from=source?box(source):null,old=captureBench(),result=model.purchase(index,confirmed);
  if(result.requiresConfirmation){confirmIndex=index;$('confirm-text').textContent=result.reason;$('confirm').hidden=false;document.querySelector('.shop').inert=true;document.querySelector('.reserve').inert=true;document.querySelector('.demo-nav').inert=true;$('confirm-no').focus();return;}
  if(!result.ok){toast(result.reason,true);return;}
  busy=true;selected=result.instance;await render();spend(result.cost);
  const node=document.querySelector('.bench-slot[data-instance="'+result.instance+'"]'),target=box(node),image=await portrait(byId.get(result.id));
  if(from)await fly(image,from,target);
  for(const merge of result.merges){
   await Promise.all(merge.materials.filter(n=>old.has(n)&&n!==merge.target).map((n,i)=>fly(image,old.get(n),target,330,i*40)));
   node.classList.remove('merged');void node.offsetWidth;node.classList.add('merged');await burst(target,merge.star,byId.get(result.id).name);await sleep(merge.star===3?650:430);
  }
  if(result.merges.length)toast(byId.get(result.id).name+'晋升'+(result.merges.at(-1).star===3?'三星':'两星'));
  busy=false;await render();
 }
 $('refresh').onclick=async()=>{
  if(busy)return;const result=model.refresh();if(!result.ok){toast(result.reason,true);if(model.snapshot().locked){$('lock').classList.remove('reject');void $('lock').offsetWidth;$('lock').classList.add('reject');}return;}
  busy=true;$('refresh').disabled=true;$('buyxp').disabled=true;document.querySelectorAll('.offer,.bench-slot').forEach(e=>e.disabled=true);spend(2);
  const old=[...document.querySelectorAll('.offer')];if(!motion)await Promise.all(old.map((e,i)=>e.animate([{opacity:1,transform:'translateY(0)'},{opacity:0,transform:'translateY(13px)'}],{duration:140,delay:i*22,fill:'forwards'}).finished));
  await render();const cards=[...document.querySelectorAll('.offer')];if(!motion)await Promise.all(cards.map((e,i)=>e.animate([{opacity:0,transform:'translateY(17px)',filter:'brightness(1.5)'},{opacity:1,transform:'translateY(0)',filter:'brightness(1)'}],{duration:260,delay:i*50,easing:'cubic-bezier(.2,.8,.2,1)',fill:'both'}).finished));
  busy=false;await render();if(result.count<5)toast('当前库存不足，仅刷新 '+result.count+' 张');
 };
 $('buyxp').onclick=async()=>{
  if(busy)return;const result=model.upgrade();if(!result.ok){toast(result.reason,true);return;}busy=true;
  const pos=box(document.querySelector('.xp-track')),float=el('span','xp-float');float.textContent='+4 经验';Object.assign(float.style,{left:pos.x+'px',top:pos.y-15+'px'});$('fx').appendChild(float);animate(float,[{opacity:0,transform:'translateY(8px)'},{opacity:1,offset:.2},{opacity:0,transform:'translateY(-18px)'}],{duration:750});spend(4);
  if(result.to>result.from){$('xpbar').style.width='100%';await sleep(240);await render();$('level').classList.remove('level-pop');void $('level').offsetWidth;$('level').classList.add('level-pop');const scan=el('div','level-flare');$('fx').appendChild(scan);setTimeout(()=>scan.remove(),900);toast('等级 '+result.to+' · 编队上限 '+result.to+' · 下次刷新使用新概率');}
  else{await render();const track=document.querySelector('.xp-track');track.classList.remove('charged');void track.offsetWidth;track.classList.add('charged');}
  await sleep(400);busy=false;await render();
 };
 $('lock').onclick=async()=>{if(busy)return;const result=model.toggleLock();await render();toast(result.locked?'本轮棋子已锁定':'已解除锁定');};
 async function sellCard(instance,point){
  busy=true;
  try{
   const quote=model.saleQuote(instance),r=model.sell(instance);
   if(!r.ok){toast(r.reason,true);return;}
   selected=null;await render();spend(r.value,true);
   if(point){
    const bounds=$('fx').getBoundingClientRect(),x=point.x-bounds.left,y=point.y-bounds.top;
    await fly('assets/originium-ui.png',{x:x-18,y:y-18,width:36,height:36,cx:x,cy:y},box(document.querySelector('.balance')),350);
   }
   toast('已出售 '+quote.name+' · +'+r.value+' 源石');
  }finally{busy=false;await render();}
 }
 $('sell').onclick=()=>{if(!busy&&selected!==null)sellCard(selected);};
 ShopSellDrag.attach({
  root:document.querySelector('.screen'),bench:$('bench'),area:document.querySelector('.shop-body'),zone:$('sell-zone'),
  quote:instance=>model.saleQuote(instance),canStart:()=>!busy&&confirmIndex===null,
  onStart:()=>{busy=true;},
  onFinish:async ({instance,commit,x,y})=>{if(commit)await sellCard(instance,{x,y});else{busy=false;await render();}}
 });
 function closeConfirm(){ $('confirm').hidden=true;document.querySelector('.shop').inert=false;document.querySelector('.reserve').inert=false;document.querySelector('.demo-nav').inert=false;confirmIndex=null; }
 $('confirm-no').onclick=closeConfirm;$('confirm-yes').onclick=()=>{const index=confirmIndex;closeConfirm();if(index!==null)purchase(index,true);};
 document.addEventListener('keydown',e=>{if(e.key==='Escape'){closeConfirm();selected=null;updateSelection();}if(e.key==='Tab'&&!$('confirm').hidden){e.preventDefault();($('confirm-no')===document.activeElement?$('confirm-yes'):$('confirm-no')).focus();}});
 $('reset').onclick=async()=>{if(busy)return;$('fx').replaceChildren();selected=null;$('toast').className='';$('confirm').hidden=true;model.reset();await render();};
 document.querySelectorAll('.demo-nav a').forEach(a=>a.classList.toggle('active',a.search==='?demo='+mode));
 $('hint').textContent=mode==='normal'?'同种同星三张自动合成':mode==='upgrade'?'点击升级，查看等级与经验变化':'再招募一名弩手，查看'+(mode==='star3'?'连锁三星':'两星合成');
 window.preview={roles,model,state:()=>model.snapshot(),reset:async()=>{model.reset();await render();},ready:()=>!busy,portrait};
 await Promise.all(roles.map(portrait));await render();
})().catch(e=>{console.error(e);document.body.dataset.error=String(e);});
