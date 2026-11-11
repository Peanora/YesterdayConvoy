"""Generate source-backed automatic timing contracts; never edits combat logic or assets."""
from pathlib import Path
import argparse,hashlib,json,re,sys

# (task, phase, runtime skill, first wait, repeat wait, range, blocked, priority,
#  attack SP, initial SP, cost, stop recovery during action, condition, max casts)
ROWS=[
 ('B01',1,'c4',9,21,2.5,False,0,0,0,0,False,'Standard',-1),
 ('B03',1,'blink',15,15,0,True,0,0,0,0,False,'Standard',-1),
 ('B04',1,'dash',0,30,0,True,0,0,0,0,False,'Standard',-1),
 ('B04',1,'blizzard',15,15,1.5,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B04',2,'dash',0,30,0,True,0,0,0,0,False,'Standard',-1),
 ('B04',2,'blizzard',15,15,1.5,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B05',1,'blink',2,15,0,True,1,0,0,0,False,'Standard',-1),
 ('B05',1,'flare',0,15,0,False,0,0,0,0,False,'InAttackRange',-1),
 ('B07',1,'sacrifice',5,70,0,False,0,0,0,0,False,'FlamechaserAlly',-1),
 ('B07',1,'dominate',5,30,2,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B07',2,'sacrifice',5,60,0,False,0,0,0,0,False,'FlamechaserAlly',-1),
 ('B07',2,'dominate',0,30,2,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B08',1,'grantFire',0,0,0,False,0,10,0,10,False,'InAttackRange',-1),
 ('B08',1,'blind',30,55,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B08',3,'grantFire',0,0,0,False,0,5,0,5,False,'InAttackRange',-1),
 ('B08',3,'blind',40,55,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B08',3,'flameDance',30,70,0,False,0,0,0,0,False,'BurningTarget',-1),
 ('B09',1,'absurdity',15,35,0,False,0,0,0,0,False,'ActiveCamera',-1),
 ('B09',2,'absurdity',17,35,0,False,0,0,0,0,False,'ActiveCamera',-1),
 ('B09',2,'intermission',10,32,0,False,0,0,0,0,False,'CameraTarget',-1),
 ('B10',1,'potion',23,23,0,False,0,0,0,23,False,'Standard',-1),
 ('B11',1,'sluggish',10,45,2.2,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B11',2,'sluggish',10,45,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B11',2,'edict',10,50,0,False,0,0,0,0,False,'Standard',-1),
 ('B12',1,'survival',180,180,0,False,0,0,0,180,True,'Standard',1),
 ('B12',2,'survival',280,280,0,False,0,0,0,280,True,'Standard',1),
 ('B12',2,'blackCrown',0,0,0,False,0,0,0,0,False,'BlackCrownCharge',-1),
 ('B12',2,'idea',15,15,0,False,0,0,0,0,False,'Standard',-1),
 ('B13',2,'rite',13,20,0,False,0,0,0,0,False,'Standard',-1),
 ('B13',3,'rite',13,20,0,False,0,0,0,0,False,'Standard',-1),
 ('B14',1,'traverse',20,40,0,False,1,0,0,0,False,'Standard',-1),
 ('B14',1,'roar',20,20,0,False,0,0,0,0,False,'Standard',-1),
 ('B15',1,'sow',15,30,0,False,0,0,0,0,False,'Standard',-1),
 ('B15',2,'sow',15,30,0,False,0,0,0,0,False,'Standard',-1),
 ('B21',1,'insomnia',30,50,0,False,0,0,20,50,True,'AwayModeTarget',-1),
 ('B21',2,'insomnia',30,50,0,False,0,0,20,50,True,'AwayModeTarget',-1),
 ('B22',2,'rock',10,20,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B23',1,'gospel',10,20,0,False,0,0,0,0,False,'InAttackRange',-1),
 ('B23',1,'seek',5,10,0,False,0,0,0,0,False,'CannonAvailable',-1),
 ('B23',2,'gospel',10,20,0,False,0,0,0,0,False,'InAttackRange',-1),
 ('B24',1,'incite',15,30,0,False,0,0,0,0,False,'Standard',-1),
 ('B24',2,'incite',15,30,0,False,0,0,0,0,False,'Standard',-1),
 ('B25',1,'judgement',0,0,0,False,0,5,0,5,False,'InAttackRange',-1),
 ('B25',1,'retribution',60,60,0,False,0,0,0,0,False,'InAttackRange',-1),
 ('B25',2,'judgement',0,0,0,False,0,4,0,4,False,'InAttackRange',-1),
 ('B25',2,'retribution',50,50,0,False,0,0,0,0,False,'InAttackRange',-1),
 ('B26',1,'balance',20,20,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B26',1,'beams',25,25,0,False,0,0,0,0,False,'Standard',-1),
 ('B26',2,'balance',20,20,0,False,0,0,0,0,False,'GlobalTarget',-1),
 ('B26',2,'beams',25,25,0,False,0,0,0,0,False,'Standard',-1),
]
NEW_TASKS={'B01','B03','B04','B05','B07','B08','B09','B10','B11','B12','B13','B14','B15','B21','B22','B23','B24','B25','B26'}
EXCLUSIONS={
 'B02':'heal=普攻治疗，沿既有自动普攻。',
 'B04':'killWindow=每损失25%HP阈值，沿已自动阈值；不变成CD循环。',
 'B06':'gaze=源初5/CD15；collapse=二形初20/CD15。旧Core机制负责，For不重复。',
 'B08':'慢步/回血/形态选择是死亡与场上旧火状态；phase2无周期技能。',
 'B09':'意兴盎然=初始0、每3次攻击回复的强化下一普攻，保留普攻所有权，当前无独立runtime技能ID。',
 'B10':'旧10s周期被动必须关闭，避免与23SP自动喷洒重复。',
 'B11':'folly=每损失50%HP和复起起始强制触发；annihilation=每形态300s一次超时/随后退场，不入周期For；二形进入时强制edict与周期10/50需唯一所有权。源另有一形闪现5/30，当前未有独立runtime技能ID，本轮不新增机制。',
 'B12':'blackCrown=二形计数>0且范围内有目标，初始计数1，创意到达+1；AOEAttack native为EnemySkill而非普攻。survival每区域/模式最多1次、首次两区结束清SP；eternal=最终复起后强制触发。思维混乱/终结实相减费等原遗物条件未移植，使用基准180/280，不能称包含全部原关卡修正。',
 'B13':'sacrament=真实被击倒与外波结束触发，不是周期。地→飞、飞→地各最多2次；原至少1.167/.967s且等待后两波结束，再飞行登场1.65/地面登场1.58s。无原外波时仅采用原最短等待+登场片长是明确适配，回满HP，不能无限半血回血。多重普攻沿现有自动攻击。',
 'B14':'echoes/猎犬→痛楚波=死亡与对象条件，不周期。原450s湮灭为超时退场、当前无独立runtime技能ID，本轮不新增机制。痛楚与猎犬自身技能沿既有独立自动对象时钟。',
 'B15':'obituary=0%/第二形态HP阈值，domainBurst=搭档倒下联动；以TL-10或TL-S-4原关卡条件为准。二形讹谬有blkhlySharedSkill身份但副页无独立CD表，沿第一形态15/30作最小适配，SourceStatus.Adapted；未取得二形独立时点。',
 'B16':'ignite19/19、detonate35/35、二形energy75/50已有Core自动；原两次阶段火雨已自动，手动六波演示雨不再额外自动。',
 'B17':'投枪=毁灭姿态初15/CD20，沿既有阶段调度。生命献祭/献祭表9999不是9999s周期，来自重生阶段强制链，不能照填For。',
 'B18':'冰环10.5/10.5、黑冰35/35与二形寒潮0，旧Core负责；不重复额外FrostSeal试玩施法。',
 'B19':'屏障刷新17/17既有自动；占据35/35且存在非己方留声机，旧Core负责，不首帧满35SP。',
 'B20':'专3：S1攻击回复0/2；S2自动15/25，持续25s期间停充；S3自动40/50，首10s、六发弹药及末发动作期间停充。魂灵0/5自动独立时钟。旧Core负责，For不重复。',
 'B21':'mountains=进入停留模式3s后的复合跟随，非9999s周期；awaken=损失50%HP强制链；起飞=20s停留/10s阈值冷却结束链。insomnia原最小CD5同时消耗50SP、初始20；通常首30s，停留模式阻回；≥6角色类目标时+1.5SP/s，不能永久固定30/50秒。原终不寐600s一次退场当前无独立runtime技能ID，不虚构到普通不寐周期。',
 'B22':'rage/flee=已自动被动与阶段，不定时手动施放；rock仅二形。',
 'B23':'pierce=二形每损失33.5%HP且被阻挡，源CD5；属于阈值/阻挡条件，不定时每5s冲刺。',
 'B24':'deathKnock=致命/二形HP阈值，deployMarked=真实待部署区再部署交互，不是周期主动。二形incite由blkhlySharedSkill延续15/30。',
 'B25':'penalty=附近目标死亡/撤退事件，absorb=真实塑能基质接近与0.1s尝试间隔，均不是周期施法。judgement按实际普通攻击回SP5/4，不按每弹/每目标计。',
}

def number(v):
 return str(int(v))if int(v)==v else str(v)
def csfloat(v):return number(v)+'f'
def csharp():
 lines=['using System;','using System.Collections.Generic;','namespace YesterdayConvoy','{',
 '    internal enum AutomaticSkillCondition { Standard,GlobalTarget,InAttackRange,FlamechaserAlly,BurningTarget,ActiveCamera,CameraTarget,CannonAvailable,BlackCrownCharge,AwayModeTarget }',
 '    internal enum AutomaticTimingSourceStatus { Exact,Adapted }',
 '    internal readonly struct AutomaticMonsterTiming','    {',
 '        internal readonly string Id;',
 '        internal readonly float Initial,Interval,Range,InitialSP,SPCost,RecoveryRate;',
 '        internal readonly int Priority,AttackSP,MaximumCasts;',
 '        internal readonly bool Blocked,StartsAfterAction;',
 '        internal readonly AutomaticSkillCondition Condition;',
 '        internal readonly AutomaticTimingSourceStatus SourceStatus;',
 '        internal AutomaticMonsterTiming(string id,float initial,float interval,float range=0,bool blocked=false,int priority=0,',
 '            bool startsAfterAction=false,int attackSP=0,float initialSP=0,float spCost=0,float recoveryRate=1,',
 '            AutomaticSkillCondition condition=AutomaticSkillCondition.Standard,AutomaticTimingSourceStatus sourceStatus=AutomaticTimingSourceStatus.Exact,int maximumCasts=-1)',
 '        {Id=id;Initial=initial;Interval=interval;Range=range;Blocked=blocked;Priority=priority;StartsAfterAction=startsAfterAction;AttackSP=attackSP;InitialSP=initialSP;SPCost=spCost;RecoveryRate=recoveryRate;Condition=condition;SourceStatus=sourceStatus;MaximumCasts=maximumCasts;}',
 '    }','    internal static class AutomaticMonsterTimings','    {',
 '        // Generated by tools/generate_automatic_monster_timings.py from the selected original source.',
 '        // Initial/Interval are seconds only for timed skills. AttackSP uses actual ordinary attack counts.',
 '        // Natural SP timings are reference waits; the scheduler must use InitialSP/SPCost/RecoveryRate,',
 '        // stop recovery only when explicitly required, and retain target/phase/counter gates.',
 '        // Threshold, death, timeout and composite follow-up abilities are deliberately absent.']
 groups={}
 for row in ROWS:groups.setdefault((row[0],row[1]),[]).append(row)
 for (task,phase),rows in groups.items():
  lines.append('        static readonly AutomaticMonsterTiming[] '+task.lower()+'p'+str(phase)+'={')
  for row in rows:
   t,ph,id,first,period,range,blocked,priority,attack,initial,cost,stop,condition,maximum=row
   args=['"'+id+'"',csfloat(first),csfloat(period)]
   if range:args.append('range:'+csfloat(range))
   if blocked:args.append('blocked:true')
   if priority:args.append('priority:'+str(priority))
   if stop:args.append('startsAfterAction:true')
   if attack:args.append('attackSP:'+str(attack))
   if initial:args.append('initialSP:'+csfloat(initial))
   if cost:args.append('spCost:'+csfloat(cost))
   if condition!='Standard':args.append('condition:AutomaticSkillCondition.'+condition)
   if maximum!=-1:args.append('maximumCasts:'+str(maximum))
   if t=='B15'and ph==2:args.append('sourceStatus:AutomaticTimingSourceStatus.Adapted')
   lines.append('            new AutomaticMonsterTiming('+','.join(args)+'),')
  lines.append('        };')
 lines+=['        internal static IReadOnlyList<AutomaticMonsterTiming> For(string cardId,int phase)=>For(cardId,phase,1);',
 '        internal static IReadOnlyList<AutomaticMonsterTiming> For(string cardId,int phase,int region)',
 '        {','            // Source first-form regions 1/2/3 all cost 180 SP; fourth mode costs 280.',
 '            if(phase<=0||cardId=="CARD_B12"&&(region<1||region>3))return Array.Empty<AutomaticMonsterTiming>();',
 '            switch(cardId)','            {']
 for task in sorted(NEW_TASKS):
  expr=':'.join('phase=='+str(phase)+'?'+task.lower()+'p'+str(phase)for t,phase in groups if t==task)+':Array.Empty<AutomaticMonsterTiming>()'
  lines.append('                case "CARD_'+task+'":return '+expr+';')
 lines+=['                default:return Array.Empty<AutomaticMonsterTiming>();','            }','        }','    }','}']
 return '\n'.join(lines)+'\n'

def document(source):
 path=source/'skills/ROSTER-SKILL-MAP.json';raw=path.read_bytes();roster=json.loads(raw.decode('utf8'))['roster']
 lines=['# 怪物技能自动触发：原作时机合同','',
 '本轮把已有技能的释放入口改为自动。本表登记源字段与适配，不把生成器检查当作实机证据；正式测试和独立Player结果见QA最新条目。','',
 '数据以本次选定26位的PRTS原文、`original.sourceSkillRows`和精确native Actor为准，避开旧试玩技能的8SP/半满初始化。原两体B15/B24取2026活动第二形态，不混同章节旧黑蛇。','',
 '## 运行合同','',
 '游戏控制器在开战与逐帧统一调用 `EnableAutomaticSkills()`，各模式没有玩家可切换的手动技能开关。独立模型复现应同样启用此调度；保留的低层手动事务夹具用于隔离单项机制，不代表普通玩法。','',
 '- 保留 `Id/Initial/Interval/Range/Priority/Blocked` 与二参 `For`；新增自然/攻击SP、动作期间停充、条件、每区域次数上限与来源状态。',
 '- `AttackSP>0` 时 Initial/Interval=0，仅每轮真实普通攻击记1次，不按命中目标数、弹道数或多段伤害记数。',
 '- `SPCost>0 && AttackSP==0` 为自然SP。Initial/Interval只是基准1SP/s的等候参考，需实际保存初始SP、动态回复与阻回。',
 '- `MaximumCasts=1` 按原模式/区域独立计数。B12原首两区域结束清SP，区域切换须刷新区域合同。',
 '- `StartsAfterAction`仅标明确SP停充，不把native `_resetMainAbilityCdWhenCastEnd`（普攻CD）误作技能CD从动作结束起计。',
 '- `Condition`决定可施放事实；未满足时保留就绪状态，不支出技力或重置CD。范围0不表示无目标即可施放，仍须条件与已有技能检查。','',
 '|角色|本作阶段|技能ID|首次/周期参考秒|SP初始/消耗|攻击回复|条件/半径|每模式上限|来源状态|',
 '|---|---|---|---|---|---|---|---|---|']
 for row in ROWS:
  task,phase,id,first,period,range,blocked,priority,attack,initial,cost,stop,condition,maximum=row
  lines.append('|'+task+'|'+str(phase)+'|'+id+'|'+number(first)+' / '+number(period)+'|'+number(initial)+' / '+number(cost)+'|'+str(attack)+'|'+('Blocked; 'if blocked else'')+condition+(' / '+number(range)if range else'')+('；停充'if stop else'')+'|'+str(maximum)+'|'+('Adapted'if task=='B15'and phase==2 else'Exact')+'|')
 lines+=['','## 非周期与已有自动所有权','']
 for u in roster:
  task=u['task'];lines.append('- '+task+' '+u['name']+'：'+EXCLUSIONS.get(task,'仅本表周期合同；未额外添加阈值或死亡定时器。'))
 lines+=['','## 原始26位技能时点摘要','',
 '以下为来源字段，斜杠/空白保留其原义，9999不自动解释为普通周期。该表不把通用8SP写回原作数字。','',
 '|角色|原技能|原首次CD|原CD|原SP消耗|来源|','|---|---|---|---|---|---|']
 for u in roster:
  for row in u['original']['sourceSkillRows']:
   name=row['name'].replace('\n',' ').replace('|','/');lines.append('|'+u['task']+'|'+name+'|'+row.get('initialCooldown','').replace('\n',' / ')+'|'+row.get('cooldown','').replace('\n',' / ')+'|'+row.get('spCost','').replace('\n',' / ')+'|[PRTS]('+u['reference']+')|')
 lines+=['','## 来源边界与适配','',
 '- `SourceStatus.Exact`表示所填基准时点/计费方式有本次原文或native证据，不表示本作伤害、地图、遗物、外部波次与完整Torappu客户端完全复刻。',
 '- B21原最低5秒冷却与50SP双门：初始20SP通常30秒就绪；停留阻回20秒，至少6个角色类目标时原SP回复额外+1.5。实际适配必须保存这些事实，不能硬编固定50秒轮询替代SP。',
 '- B11二形入场强制诏制、B13有限死亡返回和B12计数黑冠均由相应真实状态入口负责。原B11每形态300秒灭亡仅登记来源，未引入本作的倒计时判负或一个额外周期清场。',
 '- B13启圣仪式的原CD即使不在对应模式也不会暂停，地/飞返回不要重新给13秒初始CD。',
 '- B13本作缺原外波时采用原最短等待加登场动作，并保留地/飞各2次上限，是明确时序适配；不把来源空白填成任意周期。',
 '- B11原一形闪现、B14原450秒湮灭与B21原600秒终不寐当前没有独立runtime技能ID；本轮没有扩这些技能机制，也没有用同名试玩主动冒称已补。',
 '- 旧7核心按现有专属director/状态机自动化，未加入新19的For避免双播、双结算。',
 '- 固定守军不会像原作玩家临时向Boss近处部署。技能就绪而目标还停在较远普攻边界时，本作沿已有路线靠近技能范围；阻挡类仅向仍有阻挡容量的地面干员靠近。目标合法性和原条件仍须满足，没有增加动态寻路。','',
 '来源：项目外 `'+str(path)+'`，SHA256 `'+hashlib.sha256(raw).hexdigest()+'`；各 `Bxx/source-wiki.txt` 与 `native-fx/AUTOMATIC-TIMING-NATIVE-RAW.json` / `AUTOMATIC-TIMING-NATIVE-SKILLS.json` 保存原字段。源清单中的旧 `integration.NOT_IMPORTED` 属预览历史状态，不是当前工程是否接入的判据。','']
 return '\n'.join(lines)

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--source',type=Path,required=True);ap.add_argument('--project',type=Path,default=Path.cwd());ap.add_argument('--check',action='store_true');a=ap.parse_args()
 assert {r[0]for r in ROWS}==NEW_TASKS
 for row in ROWS:
  assert row[3]>=0 and row[4]>=0 and(row[8]==0 or row[3]==row[4]==0)
 outputs={a.project/'Assets/Scripts/Runtime/AutomaticMonsterTimings.cs':csharp(),a.project/'docs/AUTOMATIC_MONSTER_SKILLS.md':document(a.source)}
 for file,text in outputs.items():
  if a.check:assert file.read_text(encoding='utf8')==text,str(file)
  else:file.parent.mkdir(parents=True,exist_ok=True);file.write_text(text,encoding='utf8')
 print('AUTOMATIC_TIMING_CONTRACTS_'+('CHECK_PASS'if a.check else'GENERATED'),len(ROWS),'rows',len(NEW_TASKS),'new cards')
if __name__=='__main__':main()
