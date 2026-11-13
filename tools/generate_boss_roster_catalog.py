"""Generate approved Boss identity and provisional integration stats from reviewed source metadata."""
import argparse,json,re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);a=p.parse_args()
models=json.loads((a.source/'MODEL-REPORT.json').read_text('utf8'))['models']
costs={'B01':1,'B03':1,'B04':2,'B05':2,'B07':2,'B08':4,'B09':2,'B10':4,'B11':4,'B12':3,'B13':4,'B14':3,'B15':3,'B21':5,'B22':1,'B23':3,'B24':3,'B25':3,'B26':4}
rows=[]
for task,cost in costs.items():
 m=next(m for m in models if m['task']==task)
 wiki=(a.source/task/'source-wiki.txt').read_text('utf8')
 def field(k):
  found=re.search(r'\|'+k+r'=([^\r\n]*)',wiki);return found.group(1).strip() if found else ''
 mode=field('攻击方式');damage=field('伤害类型')
 radius=field('攻击范围半径');radius=float(radius) if radius else 2.5 if '远程'in mode else .85
 if task=='B13':radius=.85
 attack=next((x for k in ['Attack','Attack_a','A_Attack','A_Attack_1','Attack_1'] for x in m['animations'] if x['name']==k),None)
 duration=attack['duration'] if attack else 0
 contact=next((e['time'] for e in attack.get('events',[]) if e['name']=='OnAttack'),duration*.4) if attack else 0
 values=[json.dumps(x,ensure_ascii=False)for x in [task,m['name'],task+'_'+m['model']]]+[str(cost)]+[f'{v:.8f}f'for v in [200+90*cost,25+12*cost,2*cost,radius,.35 if task=='B10' else .65,max(2.4,duration+.1),contact,duration]]+['true'if mode=='近战'or task=='B13'else'false','CombatDamageType.Arts'if damage=='法术'else'CombatDamageType.Physical']
 rows.append('            new Entry('+','.join(values)+'),')
text='''using System;
using System.Collections.Generic;
using System.Linq;
namespace YesterdayConvoy
{
    // Integration values are provisional; existing seven core cards retain their authored profiles.
    public static class BossRosterCatalog
    {
        public readonly struct Entry
        {
            public readonly string Task,CardId,Name,UnitId;
            public readonly int Cost;
            public int DeploymentCost=>Task=="B10"?8:Cost+1;
            public readonly bool Melee;
            public readonly CombatDamageType DamageType;
            public readonly float Health,Attack,Armor,Range,Speed,Interval,Windup,AttackDuration;
            public Entry(string task,string name,string unit,int cost,float hp,float attack,float armor,float range,float speed,float interval,float windup,float duration,bool melee,CombatDamageType type)
            {Task=task;CardId="CARD_"+task;Name=name;UnitId=unit;Cost=cost;Health=hp;Attack=attack;Armor=armor;Range=range;Speed=speed;Interval=interval;Windup=windup;AttackDuration=duration;Melee=melee;DamageType=type;}
        }
        static readonly Entry[] entries={
'''+ '\n'.join(rows)+'''
        };
        static readonly Dictionary<string,Entry> byId=entries.ToDictionary(x=>x.CardId,StringComparer.Ordinal);
        public static IReadOnlyList<Entry> Entries=>Array.AsReadOnly(entries);
        public static bool Contains(string cardId)=>cardId!=null&&byId.ContainsKey(cardId);
        public static bool TryGet(string cardId,out Entry entry){entry=default;return cardId!=null&&byId.TryGetValue(cardId,out entry);}
    }
}
'''
out=Path(__file__).resolve().parents[1]/'Assets/Scripts/Runtime/BossRosterCatalog.cs'
out.write_text(text,encoding='utf8');print('Generated',len(rows),'new Boss identities and provisional profiles')
