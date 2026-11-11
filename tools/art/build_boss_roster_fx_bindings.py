"""Create event bindings from exact native Actor/Projectile graphs, never from a code-prefix search."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('--source',required=True,type=Path);p.add_argument('--project',default=Path.cwd(),type=Path);a=p.parse_args()
source=a.source;project=a.project;root=project/'Assets/Data/ArtCatalogs/BossRosterFx'
graph=json.loads((source/'COMPLETE-NATIVE-BINDINGS.json').read_text(encoding='utf8'))
additional=json.loads((source/'ADDITIONAL-NATIVE-BINDINGS.json').read_text(encoding='utf8'))
graph['projectiles'].update(additional['projectiles'])
units=json.loads((source/'EXACT-FX-REFERENCES.json').read_text(encoding='utf8'))['units']
old={'B02','B06','B16','B17','B18','B19','B20'}
mode_phases={
 'B04':{'Default':1,'Reborn':2},'B07':{'Default':1,'Reborn':2},'B08':{'Default':1,'Reborn_Choice':2,'M2':3},
 'B09':{'Default':1,'m1':2,'m2':2},'B11':{'s1':1,'s2':2},'B12':{'s1':1,'s2':1,'s3':1,'s4':2,'s5':2},
 'B13':{'Default':1,'M1_R2':2,'M2_R4':3},'B14':{'Default':1,'S1':2},'B22':{'Default':1,'M1':1,'M2':2},
 'B23':{'Default':1,'M1':1,'M2':2},'B25':{'Default':1,'Second':2},'B26':{'YinMode':-1,'YangMode':-1}}

for unit in units:
 task=unit['task']
 if task in old or not (root/task/'manifest.json').exists():continue
 package=json.loads((root/task/'manifest.json').read_text(encoding='utf8'));clips={c['key']:c for c in package['clips']};rows=[];seen=set();proof=[]
 def add(kind,key,skill='',native='',phase=-1,hit=-1,attach='owner',loop=False,finish=False,animation='',region=0,direction='',evidence='',projectile_life=False,allow_unapplied=False):
  if key not in clips:return
  loop=bool(loop and clips[key]['loop']);finish=bool(finish or loop)
  row=dict(kind=kind,skill=skill,nativeSkill=native.lower(),key=key,attach=attach,phase=phase,hitIndex=hit,loop=loop,finishOnEnd=finish,animation=animation,region=region,direction=direction,offsetX=0,offsetY=0,offsetZ=0,projectileLife=projectile_life,allowUnapplied=allow_unapplied)
  ident=json.dumps(row,sort_keys=True)
  if ident not in seen:seen.add(ident);rows.append(row);proof.append(dict(binding=len(rows)-1,source=evidence))
 def projectile(pk,skill,native,phase=-1,attack=False,animation='',region=0):
  pr=graph['projectiles'].get(pk,{})
  if pr.get('main'):add('ProjectileSpawn',pr['main'],skill,native,phase,attach='projectile',loop=True,finish=True,animation=animation,region=region,evidence='Projectile '+pk+'._mainEffect',projectile_life=True)
  for role in ['onHit','onReached','onStop','onTrigger']:
   for key in pr.get(role,[]):add('Impact',key,skill,native,phase,attach='target',region=region,evidence='Projectile '+pk+'.'+role,allow_unapplied=role in ['onReached','onStop'])
  for role,attach in [('onStart','owner'),('toTargetOnStart','target'),('toTileOnStart','target')]:
   for key in pr.get(role,[]):add('ProjectileSpawn',key,skill,native,phase,attach=attach,loop=role!='onStart',finish=True,region=region,evidence='Projectile '+pk+'.'+role,projectile_life=True)
 def use(ur,native,skill='',phase=-1,attack=False,animation='',region=0):
  role=ur.get('role');event=ur.get('startEvent','');source_list=ur.get('list','');origin=ur.get('from','')
  if event in ['ON_CAST_END','ON_SPELL_END'] or role=='end':kind='SkillEnd'
  elif role=='hit' and source_list not in ['castTarget','inputTarget']:kind='Impact'
  elif event=='ON_SPELL_ON':kind='ProjectileSpawn'
  elif role in ['start','loop','range','cast','ready','hit']:kind='AttackStart'if attack else 'SkillStart'
  else:return
  if ur.get('event')in ['ON_AFTER_OUTPUT_DAMAGE','ON_OUTPUT_DAMAGE']:kind='Impact'
  attach='target'if source_list in ['castTarget','inputTarget','hit']or kind=='Impact'or ur.get('on')in ['targets','target']else 'owner'
  keys=ur.get('keys',[]);slots=ur.get('slots');loop=not ur.get('oneshot',False) and role in ['loop','range','ready']
  for key in keys:
   direction=''
   if slots and key in slots:
    index=slots.index(key);direction='right'if index==1 else 'left'if index==3 else ''
   add(kind,key,skill,native,phase,ur.get('group',-1),attach,loop,loop,animation,region,direction,origin)
 main=graph['units'][unit['model']];sources=[(main,-1)]
 if task=='B15':sources.append((additional['units']['enemy_1603_hlystp2'],2))
 if task=='B24':sources.append((additional['units']['enemy_1602_blksnk2'],2))
 for actor,actor_phase in sources:
  for mode in actor['modes']:
   for branch in ['attack','combat']:
    attack=mode.get(branch)or{};phase=actor_phase if actor_phase>=0 else mode_phases.get(task,{}).get(mode['name'],1)
    region=int(mode['name'][1:])if task=='B12'and mode['name']in ['s1','s2','s3']else 0
    animation=attack.get('animKey')or''
    for role,kind,attach in [('cast','AttackStart','owner'),('hit','Impact','target')]:
     for emit in attack.get(role,[]):
      slots=emit.get('slots')
      for key in emit.get('effects',[]):
       direction=''
       if slots and key in slots:direction='right'if slots.index(key)==1 else 'left'if slots.index(key)==3 else ''
       add(kind,key,'attack','',phase,emit.get('group',-1),attach,False,False,animation,region,direction,mode['name']+'/'+attack.get('node','')+'/'+role)
    if attack.get('projectileKey'):projectile(attack['projectileKey'],'attack','',phase,True,animation,region)
  for native,spec in actor.get('skills',{}).items():
   for ur in spec.get('uses',[]):use(ur,native,phase=actor_phase)
   for pk in spec.get('projectiles',[]):projectile(pk,'',native,actor_phase)
  if actor.get('startEffect'):add('Summon',actor['startEffect'],'deploy',phase=actor_phase,evidence='Actor._startEffect')
  if actor.get('deadEffect'):add('Death',actor['deadEffect'],'death',phase=actor_phase,evidence='Actor._deadEffect')
  for ability in actor.get('abilities',[]):
   if ability.get('when')!='passive':continue
   modes=ability.get('modes',[]);phase=mode_phases.get(task,{}).get(modes[0],1)if modes else 1
   for ur in ability.get('uses',[]):
    for key in ur.get('keys',[]):add('Summon'if phase==1 else 'StateEnd',key,'deploy'if phase==1 else 'reborn',phase=phase,loop=True,finish=True,evidence='Native passive '+ability['node'])
 if task=='B13':
  for native,phase in [('m1attack',2),('m2attack',3)]:
   spec=main['skills'][native]
   for ur in spec.get('uses',[]):use(ur,'',skill='attack',phase=phase,attack=True)
   for pk in spec.get('projectiles',[]):projectile(pk,'attack','',phase,True)
 if task=='B22':
  add('StateStart','common_invincible_03','flee',loop=True,finish=True,evidence='Native HpRatioCtrl invulnerability buff')
  for key in ['enemy_mirbst_buff_02_1','enemy_mirbst_buff_02_2','enemy_mirbst_buff_02_3','enemy_mirbst_buff_03']:add('StateStart',key,'rage',loop=True,finish=True,evidence='Native M0Passive/M2LimitCrazyBonus effect manager')
 # Direct unconditional owner buffs are sourced from passive talents, not name-matched families.
 for name,talent in main.get('talents',{}).items():
  for ur in talent.get('uses',[]):
   nodes=talent.get('nodes',[])
   if not any(n.get('when')in ['passive','deploy']for n in nodes)or ur.get('event')or ur.get('on')!='owner'or ur.get('life',{}).get('type')!='INFINITY':continue
   mode=ur.get('mode')or next((n.get('mode')for n in nodes if n.get('mode')),None)
   phase=mode_phases.get(task,{}).get(mode,1)
   if task=='B12':continue # Scene-region transitions use their own actual invulnerability facts.
   for key in ur.get('keys',[]):add('Summon'if phase==1 else 'StateEnd',key,'deploy'if phase==1 else 'reborn',phase=phase,loop=True,finish=True,evidence='Direct unconditional native passive '+name)
 if task=='B04':
  for ur in main['talents']['ClearSp']['uses']:
   if ur.get('on')!='targets':use(ur,'ClearSp',phase=2)
 if task=='B11':
  for native,phase,talent in [('Suicide_1',1,'SuicideEffectS1'),('Suicide',2,'SuicideEffectS2')]:
   for ur in main['talents'][talent]['uses']:
    for key in ur.get('keys',[]):add('SkillStart',key,'annihilation',native,phase,loop=True,finish=True,evidence='Original native suicide countdown '+talent)
 if task=='B12':
  for ability in main.get('abilities',[]):
   if ability.get('node','').endswith('/Doom'):
    for ur in ability.get('uses',[]):use(ur,'AmiyDoom',skill='eternal')
 # Rebirth source uses are explicit native talent records, not every name-matched die/reborn resource.
 for name,talent in main.get('talents',{}).items():
  if name.lower()not in ['reborn','rebirth','revive','m1revive','revivetalent']:continue
  for ur in talent.get('uses',[]):
   for key in ur.get('keys',[]):add('PhaseStart',key,'reborn',loop=not ur.get('oneshot',True),finish=True,evidence='Native talent '+name)
 payload=dict(version=1,task=task,bindings=rows,sourceEvidence=proof,note='Only exact original native references. Actual event NativeSkillKey/Phase/Animation/HitIndex select bindings; no damage is caused by this data.')
 package['bindings']=rows
 (root/task/'manifest.json').write_text(json.dumps(package,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 (root/task/'bindings.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 print(task,len(rows),'bindings',len({r['key']for r in rows}),'used sources /',len(clips),flush=True)
