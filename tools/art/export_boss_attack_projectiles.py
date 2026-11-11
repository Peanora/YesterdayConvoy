from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('--source',required=True,type=Path);a=p.parse_args();r=a.source
g=json.loads((r/'COMPLETE-NATIVE-BINDINGS.json').read_text(encoding='utf8'));extra=json.loads((r/'ADDITIONAL-NATIVE-BINDINGS.json').read_text(encoding='utf8'));g['projectiles'].update(extra['projectiles'])
exact=json.loads((r/'EXACT-FX-REFERENCES.json').read_text(encoding='utf8'))['units'];old={'B02','B06','B16','B17','B18','B19','B20'}
modes={'B04':{'Default':1,'Reborn':2},'B07':{'Default':1,'Reborn':2},'B08':{'Default':1,'M2':3,'Reborn_Choice':2},'B09':{'Default':1,'m1':2,'m2':2},'B11':{'s1':1,'s2':2},'B12':{'s1':1,'s2':1,'s3':1,'s4':2,'s5':2},'B13':{'Default':1,'M1_R2':2,'M2_R4':3},'B14':{'Default':1,'S1':2},'B22':{'Default':1,'M1':1,'M2':2},'B23':{'Default':1,'M1':1,'M2':2},'B25':{'Default':1,'Second':2},'B26':{'YinMode':-1,'YangMode':-1}}
rows=[]
def emit(task,mode,role,ab,phase,region=0,source='mode'):
 if not ab:return
 nested=ab.get('abilities',[])
 if nested:
  for child in nested:emit(task,mode,role,child,phase,region,source+'/'+ab.get('class',''))
  return
 pk=ab.get('projectileKey');pr=g['projectiles'].get(pk,{})if pk else {};movement=pr.get('movement')or {};flags={}
 for b in pr.get('behaviours',[]):
  for key,val in b.get('fields',{}).items():
   if key in ['_stopWhenSourceInvalid','_managedBySource','_clearMainEffectWhenReached','_stopAfterMaxHit','_fixDealHitTargetLeft','_alwaysHitTraceTargetInTheEnd']:flags[key]=val
 rows.append(dict(cardId='CARD_'+task,task=task,mode=mode,phase=phase,region=region,polarity='yin'if mode=='YinMode'else 'yang'if mode=='YangMode'else '',branch=role,abilityClass=ab.get('class'),animation=ab.get('animKey'),ranged='Ranged'in ab.get('class','')or bool(pk),melee='Melee'in ab.get('class',''),projectileKey=pk,speed=movement.get('speed'),speedKey=movement.get('speedKey'),movementClass=movement.get('class'),lifeTime=pr.get('lifeTime'),stopWhenSourceInvalid=flags.get('_stopWhenSourceInvalid',False),nativeFlags=flags,movement=movement,source=source+'/'+ab.get('node','')))
for u in exact:
 t=u['task']
 if t in old:continue
 actors=[(g['units'][u['model']],None)]
 if t=='B15':actors.append((extra['units']['enemy_1603_hlystp2'],2))
 if t=='B24':actors.append((extra['units']['enemy_1602_blksnk2'],2))
 for actor,override in actors:
  for mode in actor['modes']:
   phase=override or modes.get(t,{}).get(mode['name'],1);region=int(mode['name'][1:])if t=='B12'and mode['name']in ['s1','s2','s3']else 0
   for role in ['attack','combat']:emit(t,mode['name'],role,mode.get(role),phase,region)
  if t=='B13':
   for key,phase in [('m1attack',2),('m2attack',3)]:
    for ability in actor['skills'][key]['abilities']:
     if ability.get('projectileKey'):emit(t,key,'attack',ability,phase,source='native EnemySkill used by attack FSM')
out=r/'runtime-additions/NATIVE-ATTACK-PROJECTILES.json';out.write_text(json.dumps(dict(sourceVersion='26-10-29-15-36-11-33649006',rows=rows,note='Explicit native mode/blocked combat branches. EmptyActive/EmptyAnimated alone do not imply ranged or melee; do not invent speed for missing projectile. B23 combat is the blocked branch.'),ensure_ascii=False,indent=2),encoding='utf8')
for row in rows:
 if row['projectileKey']:print(row['task'],row['phase'],row['region'],row['branch'],row['animation'],row['projectileKey'],row['speed'],row['stopWhenSourceInvalid'])
print(out)
