"""Paper-design checks only. Does not import Unity or simulate the actual combat AI."""
import collections
import json
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'docs/proposals/SYNERGY-BALANCE-v1.0.data.json'


def star_stats(role, star, data):
    group = 'five' if role['cost'] == 5 else 'four' if role['cost'] == 4 else 'normal'
    scale = data['stars'][group]
    return dict(hp=math.floor(role['hp'] * scale['hp'][star - 1] + .5),
                attack=round(role['attack'] * scale['ability' if role.get('attack_kind') == 'heal' else 'attack'][star - 1], 2),
                armor=role['armor'] + (0, 2, 4)[star - 1],
                dp=role['dp'] + (star - 1) * (2 if role['cost'] == 5 else 1))


def search(cost, tier_probability, species, copies, samples, rng):
    """Exact aggregated shop sampling for buying ONLY this species.

    Other species are never purchased, so their stock can be aggregated. Same-tier
    displayed non-target cards are reserved inside each 5-slot shop and returned
    on refresh. Target cards are purchased before refreshing. No tier can empty
    with the species counts used here. First shop is free; subsequent shops cost 2.
    """
    totals = []
    shops_total = []
    for _ in range(samples):
        held = 0
        shops = 0
        while held < copies:
            shops += 1
            other = (species - 1) * 9
            remaining = 9 - held
            for _slot in range(5):
                if rng.random() >= tier_probability:
                    continue
                if rng.randrange(remaining + other) < remaining:
                    remaining -= 1
                    held += 1
                    if held == copies:
                        break
                else:
                    other -= 1
        shops_total.append(shops)
        totals.append(cost * copies + 2 * (shops - 1))
    totals.sort()
    shops_total.sort()
    return dict(mean_gold=round(sum(totals) / samples, 1), median_gold=totals[len(totals)//2],
                p90_gold=totals[math.ceil(samples*.9)-1], mean_shops=round(sum(shops_total)/samples, 1))


def economy(data, cards, rolls, blocks, orders):
    gold, level, xp = 10, 1, 0
    result = []
    def add_xp(amount):
        nonlocal xp, level
        xp += amount
        while level < 10 and xp >= data['xp'][level]:
            xp -= data['xp'][level]
            level += 1
        if level == 10:
            xp = 0
    for i in range(8):
        start = gold
        expense = cards[i] + rolls[i] + 4*blocks[i] + orders[i]
        assert expense <= gold, (i+1, gold, expense)
        gold -= expense
        add_xp(4*blocks[i])
        battle_level = level
        interest = min(5, gold//10)
        reward = data['base_reward'][i] + interest + min(3, i)
        gold += reward
        add_xp(data['natural_xp'][i])
        result.append(dict(round=i+1, start=start, spend=expense, level=battle_level,
                           end=gold, next_level=level, xp=xp, interest=interest))
    return result


def main():
    data = json.loads(DATA.read_text(encoding='utf-8'))
    roster = data['roster']
    assert len(roster) == len({r['id'] for r in roster}) == 41
    assert sum(data['xp'][1:10]) == 168
    assert all(sum(row) == 100 for row in data['odds'])
    counts = collections.Counter(r['cost'] for r in roster if r['status'] != 'FUTURE')
    for trait, thresholds in data['traits'].items():
        members = [r for r in roster if trait in r['tags']]
        assert len(members) >= max(thresholds), (trait, len(members), thresholds)
    for r in roster:
        assert len(r['tags']) == len(set(r['tags']))
        assert 1 <= r['cost'] <= 5 and r['dp'] > 0 and r['hp'] > 0 and r['interval'] > 0
        assert r['attack'] >= 0 and r['armor'] >= 0 and r['range'] > 0
        previous = None
        for star in range(1, 4):
            stats = star_stats(r, star, data)
            if previous:
                assert stats['hp'] > previous['hp'] and stats['attack'] >= previous['attack']
            previous = stats
    by_id={r['id']:r for r in roster}
    for lineup in data['lineups']:
        assert len(lineup['ids'])==len(set(lineup['ids']))<=10
        tags=collections.Counter(tag for rid in lineup['ids'] for tag in by_id[rid]['tags'])
        tiers={tag:max([0]+[n for n in levels if tags[tag]>=n]) for tag,levels in data['traits'].items()}
        for tag,expected in lineup['expected'].items():assert tiers[tag]==expected,(lineup['name'],tag,tiers[tag],expected)
    rng = random.Random(20261104)
    cases = [(3,1,3),(3,1,9),(5,1,9),(6,2,3),(6,2,9),(7,3,3),(7,3,9),
             (8,4,3),(9,5,1),(9,5,3),(10,5,9)]
    simulations = []
    for level, cost, copies in cases:
        simulations.append(dict(level=level, cost=cost, target_copies=copies, species=counts[cost],
            **search(cost, data['odds'][level-1][cost-1]/100, counts[cost], copies, 5000, rng)))
    simulations.append(dict(level=10,cost=5,target_copies=9,species=8,
        **search(5,.25,8,9,5000,rng)))
    paths = {
        'balanced': economy(data,[3,4,3,6,5,4,3,8],[0,0,0,2,4,4,2,6],[0,2,2,1,2,3,5,1],[0]*8),
        'reroll_with_orders': economy(data,[2,1,0,2,3,3,2,6],[8,4,2,2,2,2,2,4],[0,0,1,1,2,2,3,4],[0,3,3,3,3,3,3,0]),
        'fast_nine': economy(data,[1,1,1,1,2,2,1,5],[0,0,0,0,0,0,0,6],[1,2,2,3,4,4,5,4],[0]*8),
    }
    output = dict(status='PAPER_MODEL_ONLY_NOT_GAME_BALANCE_VALIDATION', seed=20261104,
                  monte_carlo_samples_per_case=5000, active_species_by_cost=dict(counts),
                  simulations=simulations,economy_paths=paths,
                  strengths={str(c): [round([1,1.45,2.1,3.1,4.6][c-1] * math.sqrt(
                      data['stars']['five' if c==5 else 'four' if c==4 else 'normal']['hp'][s] *
                      data['stars']['five' if c==5 else 'four' if c==4 else 'normal']['attack'][s]),2)
                      for s in range(3)] for c in range(1,6)})
    path=ROOT/'artifacts/synergy-plan-analysis.json';path.parent.mkdir(exist_ok=True)
    path.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('PAPER_CHECK_PASS: 41 profiles; 9 trait thresholds; 7 lineup memberships; 3 feasible cash budgets; 12 shop scenarios x 5000; no Unity/combat playtest.')
    for row in simulations: print(row)
    for name, rows in paths.items():print(name, 'battle levels', [r['level'] for r in rows], 'gold before final fight',rows[-1]['start'])


if __name__ == '__main__':
    main()
