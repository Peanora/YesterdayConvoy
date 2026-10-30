/* Browser preview rules only; no Unity state is read or changed. */
(function(root,factory){if(typeof module==='object'&&module.exports)module.exports=factory();else root.ShopModel=factory();})(typeof globalThis!=='undefined'?globalThis:this,function(){
 'use strict';
 const ODDS=[null,[100,0,0,0,0],[100,0,0,0,0],[100,0,0,0,0],[65,25,10,0,0],[45,33,20,2,0],[30,40,25,5,0],[19,30,40,10,1],[18,25,32,22,3],[15,20,25,30,10],[5,10,20,40,25]];
 // Delegated trial curve, not the reference games' current XP curve.
 const XP=[0,2,4,6,10,20,32,48,70,90,0];
 const clone=x=>JSON.parse(JSON.stringify(x));
 const weight=star=>3**(star-1);
 function create(roles,options={}){
  const byId=new Map(roles.map(r=>[r.id,{id:r.id,name:r.name,cost:r.cost}]));
  let s,serial=0,randomSeed=options.seed||73191;
  function rng(){randomSeed=(Math.imul(randomSeed,1664525)+1013904223)>>>0;return randomSeed/4294967296;}
  const fail=reason=>({ok:false,reason});
  function available(pool,level){return [...byId.values()].filter(r=>ODDS[level][r.cost-1]>0&&pool[r.id]>0);}
  function draw(pool,level){
   const result=[];
   for(let slot=0;slot<5;slot++){
    const list=available(pool,level);if(!list.length)break;
    const costs=[1,2,3,4,5].filter(c=>list.some(r=>r.cost===c));
    let n=rng()*costs.reduce((v,c)=>v+ODDS[level][c-1],0),cost=costs[costs.length-1];
    for(const c of costs){n-=ODDS[level][c-1];if(n<0){cost=c;break;}}
    const candidates=list.filter(r=>r.cost===cost);let k=rng()*candidates.reduce((v,r)=>v+pool[r.id],0),chosen=candidates[candidates.length-1];
    for(const r of candidates){k-=pool[r.id];if(k<0){chosen=r;break;}}
    pool[chosen.id]--;result.push({id:chosen.id,sold:false});
   }
   return result;
  }
  function check(){
   if(s.gold<0||s.bench.length>10||s.level<1||s.level>10)throw Error('Invalid shop bounds');
   for(const r of byId.values()){
    const total=s.pool[r.id]+s.offers.filter(o=>o&&!o.sold&&o.id===r.id).length+s.bench.filter(c=>c.id===r.id).reduce((n,c)=>n+weight(c.star),0);
    if(total!==9||s.pool[r.id]<0)throw Error('Inventory invariant '+r.id);
   }
   return true;
  }
  function reset(mode=options.mode||'normal'){
   serial=0;randomSeed=options.seed||73191;
   s={gold:32,level:1,xp:0,locked:false,offers:[],bench:[],pool:Object.fromEntries([...byId.keys()].map(id=>[id,9]))};
   const add=(id,star)=>{s.pool[id]-=weight(star);s.bench.push({instance:++serial,id,star});};
   if(mode==='normal'){
    const first=available(s.pool,1);if(first.length)add(first[Math.floor(rng()*first.length)].id,1);
    s.offers=draw(s.pool,1);
   }else{
    const three=mode==='star3'||mode==='full3';
    s.level=three?7:5;s.xp=XP[s.level]-2;s.gold=40;
    if(three){add('M08',2);add('M08',2);add('M08',1);add('M08',1);}else{add('M08',1);add('M08',1);}
    const ids=three?['M08','M11','slug','M18','M20']:['M08','M11','slug','M18','M04'];
    s.offers=ids.filter(id=>byId.has(id)&&s.pool[id]>0&&ODDS[s.level][byId.get(id).cost-1]>0).map(id=>{s.pool[id]--;return{id,sold:false};});
    if(mode==='full2'||mode==='full3')for(const r of byId.values()){if(s.bench.length===10)break;if(r.id!=='M08'&&r.cost<=2&&s.pool[r.id]>0)add(r.id,1);}
   }
   check();return snapshot();
  }
  function snapshot(){return {...clone(s),xpRequired:XP[s.level],odds:[...ODDS[s.level]]};}
  function purchase(index,confirmFull=false){
   const offer=s.offers[index];if(!offer||offer.sold)return fail('这张棋子已售出');
   const r=byId.get(offer.id);if(s.gold<r.cost)return fail('源石不足');
   const bench=clone(s.bench),incoming={instance:serial+1,id:r.id,star:1};bench.push(incoming);
   const merges=[];let selected=incoming.instance;
   for(let star=1;star<3;star++){
    let group=bench.filter(c=>c.id===r.id&&c.star===star);
    while(group.length>=3){
     const materials=group.slice(0,3),keep=materials[0],remove=new Set(materials.slice(1).map(c=>c.instance));
     keep.star++;for(let j=bench.length-1;j>=0;j--)if(remove.has(bench[j].instance))bench.splice(j,1);
     merges.push({id:r.id,star:star+1,target:keep.instance,materials:materials.map(c=>c.instance)});selected=keep.instance;
     group=bench.filter(c=>c.id===r.id&&c.star===star);
    }
   }
   if(bench.length>10)return fail('备战区已满，先出售一名棋子');
   if(s.bench.length>=10&&!confirmFull)return {ok:false,requiresConfirmation:true,reason:'备战区已满，是否购买并合成？',index};
   serial++;s.gold-=r.cost;offer.sold=true;s.bench=bench;check();
   return {ok:true,kind:'purchase',id:r.id,cost:r.cost,instance:selected,merges};
  }
  function refresh(){
   if(s.locked)return fail('已锁定，先解锁再刷新');if(s.gold<2)return fail('源石不足，刷新需要 2 源石');
   const pool={...s.pool};for(const o of s.offers)if(o&&!o.sold)pool[o.id]++;
   if(!available(pool,s.level).length)return fail('当前等级无可抽库存，本次不扣源石');
   s.offers=draw(pool,s.level);s.pool=pool;s.gold-=2;check();return {ok:true,kind:'refresh',cost:2,count:s.offers.length};
  }
  function upgrade(){
   if(s.level>=10)return fail('已达到最高等级');if(s.gold<4)return fail('源石不足，升级需要 4 源石');
   s.gold-=4;s.xp+=4;const from=s.level;while(s.level<10&&s.xp>=XP[s.level]){s.xp-=XP[s.level];s.level++;}if(s.level===10)s.xp=0;
   check();return {ok:true,kind:'upgrade',cost:4,from,to:s.level,gainedXp:4};
  }
  function saleQuote(instance){
   const c=s.bench.find(c=>c.instance===instance);if(!c)return fail('先选择要出售的棋子');
   const r=byId.get(c.id),copies=weight(c.star);
   return {ok:true,instance:c.instance,id:c.id,name:r.name,star:c.star,copies,value:copies*r.cost-(c.star===1?0:1)};
  }
  function sell(instance){
   const i=s.bench.findIndex(c=>c.instance===instance);if(i<0)return fail('先选择要出售的棋子');
   const c=s.bench[i],value=saleQuote(instance).value;
   s.bench.splice(i,1);s.pool[c.id]+=weight(c.star);s.gold+=value;check();return {ok:true,kind:'sell',value,id:c.id};
  }
  const api={snapshot,purchase,refresh,upgrade,saleQuote,sell,reset,check,toggleLock(){s.locked=!s.locked;return {ok:true,kind:'lock',locked:s.locked};}};
  reset();return api;
 }
 return {create,ODDS,XP};
});
