"""昨日圆车数值v2：文档附录的独立预算/库存模型，不是Unity战斗复刻。

运行：python docs/balance/v2/budget_model.py --trials 2000
参数：inputs/*.json；输出：verification/model-results.json。
模拟不修改或读取运行中的Unity工程，所有数值由独立JSON加载。
"""
from __future__ import annotations
import argparse
import json
import math
import random
from pathlib import Path

HERE = Path(__file__).resolve().parent

def physical(attack, armor):
    return max(1., attack - armor)

def magic(attack, resistance):
    return max(1., attack * (1 - resistance / 100))

def duel(a, b):
    """同一射程内单体普攻事件；无技能/路径/控制；同刻互相命中。"""
    ah, bh = a['hp'], b['hp']
    ta, tb = a['interval'], b['interval']
    t = 0
    while ah > 0 and bh > 0 and t < 3600:
        t = min(ta, tb)
        da, db = ta == t, tb == t
        if da:
            bh -= physical(a['attack'], b.get('armor', 0))
            ta += a['interval']
        if db:
            ah -= physical(b['attack'], a.get('armor', 0))
            tb += b['interval']
    return dict(seconds=round(t, 3), result='互相击倒' if ah <= 0 and bh <= 0 else '攻击方存活' if ah > 0 else '攻击方先倒', attackerHP=round(max(0, ah), 2), defenderHP=round(max(0, bh), 2))

def load(name, inputs):
    return json.loads((inputs / name).read_text(encoding='utf-8-sig'))

def percentile(values, p):
    values = sorted(values)
    return values[min(len(values)-1, int(len(values)*p))]

def acquisition(level, cost, copies, start, rng, counts, stock, odds):
    """逐槽先费用、同费剩余张数加权；同店占库，未买全返；只买目标。"""
    c = cost - 1
    assert 0 <= start < copies <= stock[c]
    owned, shops = start, 0
    while owned < copies:
        shops += 1
        if shops > 100000:
            raise AssertionError('不可达获取目标或库存模型错误')
        reserved, found = [0]*5, 0
        for _ in range(5):
            available = [counts[i]*stock[i] - (owned if i == c else 0) - reserved[i] for i in range(5)]
            weights = [odds[level-1][i] if available[i] > 0 else 0 for i in range(5)]
            total_weight = sum(weights)
            if not total_weight:
                break
            r, selected = rng.random()*total_weight, 4
            for i, w in enumerate(weights):
                r -= w
                if r < 0:
                    selected = i
                    break
            if selected == c and rng.random() < (stock[c]-owned-found)/available[c]:
                found += 1
            reserved[selected] += 1
        assert owned + found <= stock[c]
        owned += min(copies-owned, found)
    return (copies-start)*cost + 2*(shops-1)

def economy_forecast(config, policy, base_gold):
    e = config['economy']
    gold, level, xp, streak = e['initialGold'], e['initialLevel'], 0, 0
    rows = []
    for i in range(13):
        opening = gold
        # 先留本关买牌/刷新预算，再买经验。未达人口计划如实记不足。
        cards = min(gold, policy['cardBudgets'][i]); gold -= cards
        refresh = min((gold//e['refreshCost'])*e['refreshCost'], policy['refreshBudgets'][i]); gold -= refresh
        xp_spend = 0
        while level < policy['levels'][i] and gold >= e['xpBuyGold']:
            gold -= e['xpBuyGold']; xp += e['xpBuyAmount']; xp_spend += e['xpBuyGold']
            while level < 10 and xp >= e['xpToNext'][level]:
                xp -= e['xpToNext'][level]; level += 1
            if level==10:
                xp=0
        before_reward, before_level = gold, level
        interest = min(e['interestCap'], gold//e['interestStep'])
        streak += 1
        bonus = min(3, max(0, streak-1))
        gold += base_gold[i] + interest + bonus
        xp += e['naturalXP'][i]
        while level < 10 and xp >= e['xpToNext'][level]:
            xp -= e['xpToNext'][level]; level += 1
        if level==10:
            xp=0
        assert gold >= 0 and level <= 10
        rows.append(dict(stage=i+1, openingGold=opening, cardBudget=cards, refreshGold=refresh, xpGold=xp_spend,
                         levelBeforeFight=before_level, targetLevel=policy['levels'][i], targetReached=before_level>=policy['levels'][i],
                         goldBeforeReward=before_reward, baseGold=base_gold[i], interest=interest, streakGold=bonus,
                         closingGold=gold, levelAfterReward=level, xpAfterReward=xp))
    return dict(policy=policy['name'], rows=rows, totalSources=e['initialGold']+sum(r['baseGold']+r['interest']+r['streakGold'] for r in rows),
                preFinalSources=e['initialGold']+sum(r['baseGold']+r['interest']+r['streakGold'] for r in rows[:-1]),cashBeforeFinal=rows[-1]['goldBeforeReward'],
                totalCards=sum(r['cardBudget'] for r in rows), totalRefresh=sum(r['refreshGold'] for r in rows),
                totalXP=sum(r['xpGold'] for r in rows), finalLevel=level, finalGold=gold,
                levelMisses=sum(not r['targetReached'] for r in rows), note='假设全部胜利，仅现金分配模型；购买预算不保证商店命中，也不证明能通关。无出售/额外经济羁绊收入。')

def defender_pressure(unit, tier, armor, resistance, aura_amp=1.):
    """已选遭遇的完整自动周期/持续技能状态；不是瞬时爆发或全图合计。"""
    base=next(s for s in unit['stages'] if int(s['tier'])==tier)
    params={p['key']:p['tier'+str(tier)] for p in unit['skills'][0]['parameters']}
    atk,interval=base['attack'],base['interval']
    crit,mult=base.get('criticalChance',0),base.get('criticalMultiplier',1.5)
    def hit(attack):
        if base['damageType']=='Arts':
            return magic(attack,resistance)*aura_amp
        return (1-crit)*physical(attack,armor)+crit*physical(attack*mult,armor)
    cycle=0 if base.get('noOrdinaryAttack') else hit(atk)/interval
    if unit['id']=='D07':
        cycle=(4*hit(atk)+params['hits']*hit(atk*params['attackMultiplier']))/(5*interval)
    elif unit['id']=='D08':
        cycle=(4*hit(atk)+hit(atk*params['attackMultiplier']))/(5*interval)
    sustained,window=cycle,10
    # 一次性强化已经计入完整周期；有持续时间的普通攻速/攻击强化另算。
    duration=params.get('duration',0)
    if duration>0 and unit['id'] not in ('D03','D10','D11','D29') and not base.get('noOrdinaryAttack'):
        active=hit(atk*params.get('attackMultiplier',1))*params.get('attackSpeedMultiplier',1)/interval
        if active>sustained:
            sustained,window=active,min(10,duration)
    metric=base.get('conditionalSkillMetrics',{})
    kind=base.get('damageType','Physical')
    for key in ['physicalDpsPerTargetArmor0_12_30','physicalDpsPerTargetAtFullChargeArmor0_12_30']:
        if key in metric:
            # 当前采用这两个高级技能的关卡均为12甲样本，禁止静默外推。
            assert armor==12
            active=metric[key][1]+metric.get('selfKasimirTrueDpsPerTargetAtFullCharge',0)
            if active>sustained:
                sustained,window,kind=active,min(10,duration),'Physical/mixed'
    for key in ['normalArtsDpsPerTarget','finalArtsDpsPerTarget']:
        if key in metric:
            active=metric[key]*(1-resistance/100)*aura_amp
            if active>sustained:
                sustained,window,kind=active,min(10,metric.get('finalProtectionSeconds',duration)),'arts'
    return dict(cycleDPS=cycle,sustainedConditionalDPS=sustained,windowSeconds=window,damageType=kind)

def defense_stages(config, defenders, players=None):
    by_id = {u['id']: u for u in defenders['units']}
    details, totals = [], []
    for stage in config['stages']:
        group = []
        player_armor = 0 if stage['stage'] <= 4 else 8 if stage['stage'] <= 8 else config['measurement']['baselinePlayerArmor']
        player_resistance = 0 if stage['stage'] <= 4 else 10 if stage['stage'] <= 8 else config['measurement']['baselinePlayerResistance']
        aura_amp=1.
        for gid,gtier,_ in stage['guardSpecs']:
            if gid=='D29':
                aura_amp=next(p['tier'+str(gtier)] for p in by_id[gid]['skills'][0]['parameters'] if p['key']=='damageAmplification')
        healer_focus, danger_peak, danger_name, danger_window, danger_kind = 0., 0., '', 10, ''
        for guard_id, tier, scale in stage['guardSpecs']:
            unit = by_id[guard_id]
            base = next(s for s in unit['stages'] if int(s['tier']) == tier)
            # 集火压力明确计入治疗，不能只以全队总DPS判断能否打穿医疗覆盖。
            landed_hp=math.floor(base['hp']*scale+.5)
            normal_hps = base.get('ordinarySingleTargetHps', 0)
            healer_focus = max(healer_focus, landed_hp/config['measurement']['focusWindowSeconds']+normal_hps if normal_hps else 0)
            metric = base.get('conditionalSkillMetrics',{})
            pressure=defender_pressure(unit,tier,player_armor,player_resistance,aura_amp)
            peak=pressure['sustainedConditionalDPS']
            if peak>danger_peak:
                danger_peak,danger_name=peak,unit['name']
                danger_window=pressure['windowSeconds']
                danger_kind=pressure['damageType']
            row = dict(stage=stage['stage'], id=guard_id, name=unit['name'], tier=tier, hpScale=scale,
                       hp=math.floor(base['hp']*scale+.5), attack=base['attack'], armor=base['armor'],
                       resistance=base.get('resistance', 0), interval=base['interval'], range=base.get('range', 0), blocks=base.get('blocks', 0),
                       fullAutoCycleDPS=round(pressure['cycleDPS'],3),conditionalSustainedDPS=round(peak,3),conditionalWindowSeconds=pressure['windowSeconds'],
                       note='生命按关卡系数调整；其余取对应T档。普通周期包含自动强化，持续技能状态另列；不包含瞬时弹丸尖峰。医疗攻击为治疗量。')
            group.append(row); details.append(row)
        hp = sum(r['hp'] for r in group)
        mechanic_budget = stage.get('mechanicBudget',0)
        required = hp + stage['healBudget'] + stage['shieldBudget'] + mechanic_budget
        recommended = math.ceil(required*config['measurement']['baseMargin'])
        window = stage['window']
        raw = recommended/window/config['measurement']['uptimeOrdinary']/config['measurement']['nominalDamageConversion'] if window else 0
        totals.append(dict(stage=stage['stage'], type=stage['type'], guards='、'.join(r['name'] for r in group) or '无守军',
                           dp=stage['dp'], defense=stage['defense'], totalHP=hp, healBudget=stage['healBudget'], shieldBudget=stage['shieldBudget'], mechanicBudget=mechanic_budget,
                           damageFloor=required, recommendedDamage=recommended, measurementSeconds=window,
                           effectiveDPS=round(recommended/window, 2) if window else 0, rosterDPSAt65PercentUptimeAnd80PercentConversion=round(raw, 2),
                           noviceDPSAt45Percent=round(recommended/window/config['measurement']['uptimeNovice']/config['measurement']['nominalDamageConversion'], 2) if window else 0,
                           focusDPS=round(max(healer_focus,max((r['hp'] for r in group), default=0)/config['measurement']['focusWindowSeconds']),2),
                           dangerName=danger_name,dangerSingleTargetDPS=round(danger_peak,2),
                           tankHPFor10Seconds=math.ceil(danger_peak*10*config['measurement']['baseMargin']),
                           dangerWindowSeconds=danger_window if group else 0,
                           tankHPForDangerWindow=math.ceil(danger_peak*danger_window*config['measurement']['baseMargin']),
                           dangerDamageType=danger_kind,
                           playerArmorSample=player_armor,playerResistanceSample=player_resistance,
                           protectionWaitSeconds=stage.get('protectionWait',0),
                           note=stage['note']))
        if players and danger_kind=='arts':
            tank=next(s for u in players['units'] if u['id']=='CARD_M18' for s in u['stars'] if s['star']==2)
            actual_dps=danger_peak/(1-player_resistance/100)*(1-tank['resistance']/100)
            totals[-1]['exampleTank']={'name':'二星游击队盾卫','hp':tank['hp'],'armor':tank['armor'],'resistance':tank['resistance'],
                                      'conditionalDPSReceived':round(actual_dps,2),'peakWindowSeconds':danger_window,
                                      'requiredHPWithMargin':math.ceil(actual_dps*danger_window*config['measurement']['baseMargin']),
                                      'note':'仅对单名危险守军的同一条件峰值；未合计其他守军、其他技能或己方治疗。'}
    return details, totals

def reference_lineups(players, config):
    units={u['id']:u for u in players['units']}
    results=[]
    for upgrade in [False,True]:
        specs=[('CARD_M18',2),('CARD_M24',2),('CARD_M42',2),('CARD_M27',2),('CARD_M19',3 if upgrade else 2),('CARD_M17',1)]
        rows=[]
        for uid,star in specs:
            u=units[uid]; s=next(v for v in u['stars'] if v['star']==star)
            attack=s['attack']*1.08
            hit=magic(attack,0) if s['damageType']=='arts' else physical(attack,26)
            rate=1.5 if uid=='CARD_M27' else 1
            rows.append(dict(id=uid,name=u['name'],star=star,dp=s['dp'],cardGold=u['cost']*s['copies'],nominalDPS=round(hit/s['interval']*rate,6)))
        nominal=sum(r['nominalDPS'] for r in rows)
        results.append(dict(name='四费二星混合候选'+('，游击队萨卡兹战士三星' if upgrade else ''),units=rows,
                            firstDeploymentDP=sum(r['dp'] for r in rows),cardPurchaseGold=sum(r['cardGold'] for r in rows),slots=len(rows),
                            targetArmor=26,targetResistance=0,nominalDPS=round(nominal,3),effectiveAt65Percent=round(nominal*.65,3),
                            effectiveDamage75Seconds=round(nominal*.65*75,2),
                            assumptions='六人全程可打同一目标，传令兵一星8%光环始终有效，M27盾未破；未计其他羁绊伤害，未计来自守军的打断/减员。牌价不含搜卡、升级与过渡。此处已逐段扣甲，不再额外乘80%净伤率。',
                            result='仅静态供给量级，不是可达阵容或通关证明'))
    return results

def validate(config, players=None, defenders=None, synergies=None):
    checks=[]
    def check(name, value, expected=True):
        assert value == expected, (name, value, expected)
        checks.append(dict(name=name, observed=value, expected=expected, result='通过'))
    check('最低物理伤害', physical(20, 30), 1.)
    check('逐段扣甲：4段30攻击打20甲', sum(physical(30,20) for _ in range(4)),40.)
    check('法抗样例100伤/30抗', magic(100,30),70.)
    check('同刻攻击已知例', duel(dict(hp=100,attack=30,interval=2),dict(hp=100,attack=30,interval=2))['seconds'],8)
    check('概率每行100', all(sum(r)==100 for r in config['economy']['odds']))
    check('13关含5补给', sum(s['type']=='补给' for s in config['stages']),5)
    check('8场正式战斗', sum(s['type']!='补给' for s in config['stages']),8)
    check('补给不存在战斗门槛', all(not s['guardSpecs'] and s['window']==0 for s in config['stages'] if s['type']=='补给'))
    check('唯一目标满池首店可得三张', acquisition(1,1,3,0,random.Random(7),[1,0,0,0,0],[30,25,18,10,9],config['economy']['odds']),3)
    if players:
        check('玩家43种',len(players['units']),43)
        check('玩家ID唯一',len(set(u['id'] for u in players['units'])),43)
        check('玩家逐星138行',sum(len(u['stars']) for u in players['units']),138)
        check('逐星部署费按每星加1',all(s['dp']==u['base']['dp']+s['star']-1 for u in players['units'] for s in u['stars']))
        check('逐星基础牌占用',all(s['copies']==3**(s['star']-1) for u in players['units'] for s in u['stars']))
        check('玩家星级生命攻击非递减',all(all(u['stars'][i]['hp']<=u['stars'][i+1]['hp'] and u['stars'][i]['attack']<=u['stars'][i+1]['attack'] for i in range(len(u['stars'])-1)) for u in players['units']))
        check('玩家普通攻击间隔不短于已有动作窗',all(s['interval']>=s.get('attackDuration',0) for u in players['units'] for s in u['stars']))
        check('部署次数受费用档约束',all(s['uses']==[8,6,4,3,2][u['cost']-1] for u in players['units'] for s in u['stars']))
        check('33DP候选静态DPS独立复核',round(reference_lineups(players,config)[0]['nominalDPS'],3),247.295)
    if defenders:
        check('实际接入守军22种',len(defenders['units']),22)
        check('所有守军三个强度档',all(len(u['stages'])==3 for u in defenders['units']))
        units={u['id']:u for u in defenders['units']}
        check('玫兰莎持续开技对8甲31.52',round(defender_pressure(units['D04'],2,8,10)['sustainedConditionalDPS'],2),31.52)
        check('克洛丝完整自动周期对8甲',round(defender_pressure(units['D07'],2,8,10)['cycleDPS'],3),25.515)
        check('史都华德完整自动周期对0抗',round(defender_pressure(units['D08'],2,8,0)['cycleDPS'],3),28.844)
        for s in config['stages']:
            if s.get('mechanicBudget'):
                extra=sum(next(t for t in units[uid]['stages'] if t['tier']==tier).get('conditionalSkillMetrics',{}).get('maximumExtraBudgetAboveBase',0)*scale for uid,tier,scale in s['guardSpecs'])
                check('第'+str(s['stage'])+'关星熊额外预算',s['mechanicBudget'],math.ceil(extra))
    if synergies:
        check('九条常规羁绊',len(synergies['synergies']),9)
        check('全部29档羁绊',sum(len(s['tiers']) for s in synergies['synergies']),29)
        check('羁绊ID无重复',len({s['id'] for s in synergies['synergies']}),9)
    return checks

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--input-dir',type=Path,default=HERE/'inputs')
    parser.add_argument('--output-dir',type=Path,default=HERE/'verification')
    parser.add_argument('--trials',type=int,default=2000,help='每种子每获取场景次数，三种子合计默认6000')
    parser.add_argument('--skip-acquisition',action='store_true')
    args=parser.parse_args()
    config=load('campaign.json',args.input_dir)
    players=load('player-roster.json',args.input_dir)
    defenders=load('defenders.json',args.input_dir)
    synergies=load('synergies.json',args.input_dir)
    checks=validate(config,players,defenders,synergies)
    details,totals=defense_stages(config,defenders,players)
    forecasts=[economy_forecast(config,p,config['economy']['baseGold']) for p in config['policies']]
    old_forecasts=[economy_forecast(config,p,config['economy']['currentBaseGold']) for p in config['policies']]
    counts=[sum(u['cost']==c for u in players['units']) for c in range(1,6)]
    acquisitions=[]
    if not args.skip_acquisition:
        for level,cost,copies,start in [(3,1,9,1),(5,1,9,0),(6,2,9,0),(7,3,9,0),(8,4,3,0),(9,5,3,0),(10,5,3,0),(5,1,27,0)]:
            results=[]
            for seed in [73191,20261108,31016]:
                rng=random.Random(seed+level*100+cost*1000+copies)
                results.extend(acquisition(level,cost,copies,start,rng,counts,config['economy']['stock'],config['economy']['odds']) for _ in range(args.trials))
            acquisitions.append(dict(level=level,cost=cost,targetCopies=copies,startCopies=start,trials=len(results),mean=round(sum(results)/len(results),2),
                                     p10=percentile(results,.1),median=percentile(results,.5),p90=percentile(results,.9),
                                     note='固定等级只买单目标，首店免费；不含升级/委托/其他牌/跨关免费店/利息损失'))
    sensitivity=[dict(stage=s['stage'],hpScale=factor,recommendedDamage=math.ceil((s['totalHP']*factor+s['healBudget']+s['shieldBudget']+s['mechanicBudget'])*config['measurement']['baseMargin']),
                      effectiveDPS=round((s['totalHP']*factor+s['healBudget']+s['shieldBudget']+s['mechanicBudget'])*config['measurement']['baseMargin']/s['measurementSeconds'],2))
                 for s in totals if s['measurementSeconds'] for factor in [.9,1,1.1]]
    result=dict(modelVersion='2.0-draft-budget-model',status='纸面设计，未运行Unity',counts=dict(players=len(players['units']),starRows=sum(len(u['stars']) for u in players['units']),defenders=len(defenders['units']),synergies=len(synergies['synergies'])),
                checks=checks,stageGuards=details,stages=totals,economy=forecasts,currentEconomyBaseline=old_forecasts,acquisition=acquisitions,sensitivity=sensitivity,referenceLineups=reference_lineups(players,config),
                seeds=[73191,20261108,31016],modelLimitations=['非Unity路径/阻挡/目标选择/完整技能模拟，无真实通关率','治疗/护盾预算是显式遭遇假设，击杀顺序可降低实际需求','普通伤害预算基于清场路线；允许绕行穿门时不存在固定总伤必要阈值','聚合DPS足够仍可能因扛不住、打不到目标或穿门数不足而失败','高档游击/巨像V与银狼三星不作为普通13关必备','经济策略假设全胜，仅检验现金分配，不能证明买牌命中或阵容战斗可行'])
    args.output_dir.mkdir(parents=True,exist_ok=True)
    (args.output_dir/'model-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(dict(counts=result['counts'],checks=len(checks),stages=totals,economy=[{k:v for k,v in f.items() if k not in ('rows','note')} for f in forecasts],acquisition=acquisitions),ensure_ascii=False,indent=2))

if __name__=='__main__':
    main()
