"""Generate source action metadata only; provisional combat values stay in BossRosterCatalog."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('E:/珍贵资料/昨日圆车_美术预览/2026-11-11_26位Boss原作整备-v1/skills/ROSTER-SKILL-MAP.json')
rows=json.loads(SOURCE.read_text(encoding='utf-8'))['roster']
OLD={'B02','B06','B16','B17','B18','B19','B20'}
RUNTIME_TEXT={
 ('B01','c4'):'延时炸药附着目标；半血后最多三个目标，安装后3.2秒引爆。',
 ('B10','potion'):'交替喷洒睡眠治疗药剂，或令两名友军短时不死并强化攻击与攻速。',
 ('B11','edict'):'强化友军6秒，结束时对期间曾被晕眩的友军造成法术伤害。',
 ('B12','survival'):'封印最左侧目标，并推进战场区域。',
 ('B12','idea'):'生成朝本体移动的仅剩的创意；返回后补充黑冠次数。',
 ('B21','insomnia'):'现身重击目标周围并封印地面，随后进行三次全场打击。',
 ('B04','dash'):'位移并伤害附近干员。',
 ('B05','flare'):'造成范围物理伤害并生成照明区，抵消区域内的烟雾隐匿。',
 ('B07','dominate'):'封锁目标，延时范围爆炸并造成灼燃。',
 ('B08','blind'):'压制基础攻击最高的目标，并在其附近生成旧日之火。',
 ('B13','sacrament'):'离场后切换地面或飞行姿态返回，回复50%最大生命。',
 ('B14','traverse'):'位移并伤害附近干员，造成法术伤害并降低攻速。',
 ('B23','gospel'):'连续齐射十次，优先选择蚀损累计较低的目标。',
 ('B23','seek'):'强化盐炮6秒并提前炮击。',
 ('B25','judgement'):'蓄力连击，基质越多连击越多。',
}
q=lambda s:json.dumps(s or '',ensure_ascii=False)
f=lambda n:format(float(n),'.8f')+'f'
lines=['using System;','using System.Collections.Generic;','namespace YesterdayConvoy','{','    // Source clip names/events are audited; SP and damage multipliers are integration trial values.','    public static class BossRosterRules','    {','        public sealed class Skill','        {','            public readonly string Id,Name,Description,Animation,NativeKey;public readonly float RequiredSP,Windup,Duration;public readonly int MinimumPhase;public readonly Dictionary<int,string> PhaseAnimations;','            public Skill(string id,string name,string text,string anim,string native,float sp,float wind,float duration,int phase,Dictionary<int,string> variants)','            {Id=id;Name=name;Description=text;Animation=anim;NativeKey=native;RequiredSP=sp;Windup=wind;Duration=duration;MinimumPhase=phase;PhaseAnimations=variants;}','        }','        public sealed class Clip','        {public readonly string Name;public readonly float Duration;public readonly float[] Hits;public Clip(string name,float duration,float[] hits){Name=name;Duration=duration;Hits=hits;}}','        static readonly Dictionary<string,Skill[]> skills=new Dictionary<string,Skill[]>(StringComparer.Ordinal)','        {']
for r in rows:
 if r['task'] in OLD:continue
 lines.append('            {'+q('CARD_'+r['task'])+',new Skill[]{')
 for s in r['skills']:
  n=(s.get('nativeEvidence') or [{}])[0];native=next((x for x in [n.get('path'),n.get('name'),s['id']] if isinstance(x,str) and x),'').rsplit('/',1)[-1]
  if r['task']=='B22':native={'rock':'ThrowStone','flee':'StartRun','rage':'M0Passive'}[s['id']]
  variants='new Dictionary<int,string>{'+','.join('{'+str(k)+','+q(v)+'}' for k,v in s.get('phaseAnimations',{}).items())+'}'
  lines.append('                new Skill('+','.join([q(s['id']),q(s['name']),q(RUNTIME_TEXT.get((r['task'],s['id']),s['description'])),q(s.get('animation')),q(native),f(s['cooldown']),f(s['windup']),f(s['animationDuration']),str(max(1,s['minimumPhase'])),variants])+'),')
 lines.append('            }},')
lines+=['        };','        static readonly Dictionary<string,Dictionary<string,Clip>> clips=new Dictionary<string,Dictionary<string,Clip>>(StringComparer.Ordinal)','        {']
for r in rows:
 if r['task'] in OLD:continue
 lines.append('            {'+q('CARD_'+r['task'])+',new Dictionary<string,Clip>(StringComparer.Ordinal){')
 for a in r['animations'][0]['clips']:
  lines.append('                {'+q(a['name'])+',new Clip('+q(a['name'])+','+f(a['duration'])+',new float[]{'+','.join(f(e['time']) for e in a['events'] if e['name']=='OnAttack')+'})},')
 lines.append('            }},')
lines+=['        };','        public static bool Supports(string card)=>card!=null&&skills.ContainsKey(card);','        public static IReadOnlyList<Skill> Skills(string card)=>Supports(card)?skills[card]:Array.Empty<Skill>();','        public static Clip Action(string card,string raw)=>raw!=null&&clips.TryGetValue(card??"",out var all)&&all.TryGetValue(raw,out var result)?result:null;','        public static string SkillAnimation(string card,Skill skill,int phase)=>skill.PhaseAnimations.TryGetValue(phase,out var value)?value:skill.Animation;','    }','}']
(ROOT/'Assets/Scripts/Runtime/BossRosterRules.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
core=['using System;','using System.Collections.Generic;','namespace YesterdayConvoy','{','    internal static class BossCoreSource','    {','        static readonly Dictionary<string,Dictionary<string,BossRosterRules.Clip>> clips=new Dictionary<string,Dictionary<string,BossRosterRules.Clip>>(StringComparer.Ordinal){']
for r in rows:
 if r['task'] not in ['B16','B18','B20']:continue
 card={'B16':'CARD_TALULAH_TRIAL','B18':'CARD_M14','B20':'CARD_WISADEL_APRIL'}[r['task']]
 core.append('            {'+q(card)+',new Dictionary<string,BossRosterRules.Clip>(StringComparer.Ordinal){')
 for a in r['animations'][0]['clips']:
  core.append('                {'+q(a['name'])+',new BossRosterRules.Clip('+q(a['name'])+','+f(a['duration'])+',new float[]{'+','.join(f(e['time']) for e in a['events'] if e['name']=='OnAttack')+'})},')
 core.append('            }},')
core+=['        };','        internal static BossRosterRules.Clip Action(string card,string raw)=>raw!=null&&clips.TryGetValue(card??"",out var all)&&all.TryGetValue(raw,out var clip)?clip:null;','    }','}']
(ROOT/'Assets/Scripts/Runtime/BossCoreSource.cs').write_text('\n'.join(core)+'\n',encoding='utf-8')
print('Wrote 19 source-backed rule profiles')
