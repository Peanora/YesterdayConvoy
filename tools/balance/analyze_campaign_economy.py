"""Campaign income / final-shop pursuit estimates; not a combat or strategy solver."""
import json, random
from pathlib import Path

BASE=[10,10,12,14,16,22,16,18,18,24,20,22,28]

def chase(species, held, budget, rng):
    spent=0
    while held<9:
        rest=(species-1)*9
        remaining=9-held
        for slot in range(5):
            if rng.random()>=.25:continue
            if rng.randrange(rest+remaining)<remaining:
                if spent+5>budget:return False
                spent+=5;remaining-=1;held+=1
                if held==9:return True
            else:rest-=1
        if spent+2>budget:return False
        spent+=2
    return True

def main():
    gold=10;rows=[]
    for i,base in enumerate(BASE):
        interest=min(5,gold//10);streak=min(3,i);gold+=base+interest+streak
        rows.append(dict(stage=i+1,base=base,interest=interest,streak=streak,save_all_balance=gold))
    result=dict(min_gross_including_start=10+sum(BASE)+sum(min(3,i) for i in range(13)),
                save_all_gross=gold,before_final_save_all=rows[-2]['save_all_balance'],rows=rows,chase=[])
    rng=random.Random(20261104)
    for species in (3,7):
        for held in (0,3):
            for budget in (100,140,180):
                wins=sum(chase(species,held,budget,rng) for _ in range(10000))
                result['chase'].append(dict(five_cost_species=species,initial_target_copies=held,budget=budget,success_percent=wins/100))
    out=Path(__file__).resolve().parents[2]/'artifacts/campaign-economy.json'
    out.parent.mkdir(exist_ok=True);out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
