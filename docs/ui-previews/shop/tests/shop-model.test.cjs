const test=require('node:test');
const assert=require('node:assert/strict');
const {create,ODDS}=require('../shop-model.js');
const role=(id,cost=1)=>({id,name:id,cost});
const same=s=>JSON.stringify(s);
test('nine copies conserve inventory through two-star and chained three-star merges',()=>{
 const m=create([role('M08')]);let merges=[];
 for(let i=0;i<5;i++){const r=m.purchase(i);assert(r.ok);merges.push(...r.merges);m.check();}
 assert(m.refresh().ok);
 for(let i=0;i<3;i++){const r=m.purchase(i);assert(r.ok);merges.push(...r.merges);}
 assert.deepEqual(m.snapshot().bench.map(c=>c.star),[3]);assert.equal(merges.filter(x=>x.star===2).length,3);assert.equal(merges.filter(x=>x.star===3).length,1);
 assert.equal(m.snapshot().pool.M08,0);const before=same(m.snapshot());assert.equal(m.refresh().ok,false);assert.equal(same(m.snapshot()),before);
 assert.equal(m.sell(m.snapshot().bench[0].instance).value,8);assert.equal(m.snapshot().pool.M08,9);m.check();
});
test('locked refresh, duplicate purchase and insufficient funds never mutate state',()=>{
 const m=create([role('M08')]);m.toggleLock();let before=same(m.snapshot());assert.equal(m.refresh().ok,false);assert.equal(same(m.snapshot()),before);m.toggleLock();
 m.purchase(0);before=same(m.snapshot());assert.equal(m.purchase(0).ok,false);assert.equal(same(m.snapshot()),before);
 while(m.upgrade().ok){}before=same(m.snapshot());assert.equal(m.upgrade().ok,false);assert.equal(same(m.snapshot()),before);m.check();
});
test('XP crosses levels with carry and keeps the current five offers unchanged',()=>{
 const m=create([role('M08'),role('M04',2)]),before=same(m.snapshot().offers);
 const r=m.upgrade();assert.equal(r.to,2);assert.equal(m.snapshot().xp,2);assert.equal(same(m.snapshot().offers),before);
 m.upgrade();assert.equal(m.snapshot().level,3);assert.equal(m.snapshot().xp,2);assert.equal(same(m.snapshot().offers),before);
 for(let i=0;i<10;i++)assert.equal(ODDS[i+1].reduce((a,b)=>a+b),100);
});
test('starting levels cannot draw forbidden higher costs, even when all low cards are held',()=>{
 const m=create([role('M08'),role('M04',2),role('M20',5)]);
 for(let i=0;i<5;i++)m.purchase(i);m.refresh();for(let i=0;i<3;i++)m.purchase(i);
 assert.equal(m.refresh().ok,false);assert.equal(m.snapshot().pool.M04,9);assert.equal(m.snapshot().pool.M20,9);
});
test('full bench rejects purchase atomically; a full-bench merge requires explicit confirmation',()=>{
 let rejected=false,confirmed=false;
 for(let seed=1;seed<=100&&(!rejected||!confirmed);seed++){
  const m=create(Array.from({length:30},(_,i)=>role('R'+i)),{seed});
  for(let turns=0;turns<35;turns++){
   const s=m.snapshot();if(s.gold<2)break;
   for(let i=0;i<s.offers.length;i++){
    const before=same(m.snapshot()),r=m.purchase(i);
    if(r.requiresConfirmation){assert.equal(same(m.snapshot()),before);assert(m.purchase(i,true).ok);assert(m.snapshot().bench.length<=10);confirmed=true;}
    else if(!r.ok&&r.reason.includes('已满')){assert.equal(same(m.snapshot()),before);rejected=true;}
    m.check();
   }
   if(!m.refresh().ok)break;
  }
 }
 assert(rejected,'full bench rejection reached');assert(confirmed,'full bench merge confirmation reached');
});
test('weighted refresh and sell sequences preserve every pool across 100 seeded runs',()=>{
 const roles=Array.from({length:20},(_,i)=>role('R'+i,i%5+1));
 for(let seed=1;seed<=100;seed++){
  const m=create(roles,{seed});
  for(let n=0;n<45;n++){
   const s=m.snapshot();if(n%7===0)m.upgrade();else if(n%5===0&&s.bench.length)m.sell(s.bench[0].instance);else if(n%3===0)m.refresh();else m.purchase(n%5,true);
   m.check();assert(m.snapshot().gold>=0);
  }
 }
});
