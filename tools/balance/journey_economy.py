"""Source-backed Journey cash forecast and actual ShopTrialModel seed audit.
Run: py -3 tools/balance/journey_economy.py
Requires Python 3 and .NET SDK. No Unity process or project assets are written.
"""
from __future__ import annotations
import argparse
import csv
import hashlib
import json
import os
import re
import subprocess
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
INPUT = Path(__file__).with_name("journey_economy_scenarios.json")
SOURCES = {"nodes": "Assets/Data/CampaignV2/journey-authoring.json",
           "economy": "Assets/Scripts/Runtime/CampaignJourneyEconomy.cs",
           "shop": "Assets/Scripts/Runtime/ShopTrialModel.cs",
           "run": "Assets/Scripts/Runtime/BattleShopRun.cs",
           "players": "Assets/Data/Resources/CampaignV2/player-roster.json",
           "balance": "Assets/Scripts/Runtime/CampaignBalance.cs",
           "wolf": "Assets/Scripts/Runtime/SilverWolfCombat.cs"}


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def source_parameters():
    stages = json.loads(read(SOURCES["nodes"]))["stages"]
    code, shop = read(SOURCES["economy"]), read(SOURCES["shop"])
    initial = int(re.search(r"InitialGold\s*=\s*(\d+)", code)[1])
    curve = [int(v) for v in re.search(r"LevelExperience\s*=\s*\{([^}]+)\}", code)[1].split(",")]
    streak_text = re.search(r"StreakBonus\(int wins\)\s*=>\s*([^;]+);", code)[1]
    streak = [[int(a), int(b)] for a, b in re.findall(r"wins\s*>=\s*(\d+)\s*\?\s*(\d+)", streak_text)]
    interest = re.search(r"Interest\s*=\s*Math.Min\((\d+),\s*Gold\s*/\s*(\d+)\)", shop)
    upgrade = re.search(r"public Result Upgrade\(\)(.*?)public Result Refresh", shop, re.S)[1]
    paid = re.search(r"Gold\s*-=\s*(\d+);\s*XP\s*\+=\s*(\d+)", upgrade)
    assert paid, "Upgrade conversion changed: update the model explicitly."
    refresh_code = re.search(r"public Result Refresh\(bool free=false\)(.*?)public Result Commission", shop, re.S)[1]
    assert re.search(r"!free\s*&&\s*!CanAfford\(2\)", refresh_code), "Refresh price changed: update the input policy units."
    assert re.search(r"result.Value\s*=\s*role.Cost\s*\+\s*2", shop), "Commission surcharge changed: update the policy."
    assert "countsForStreak:!JourneyV2||!RewardStage" in read(SOURCES["run"]), "Supply streak scope changed."
    assert [s["stage"] for s in stages] == list(range(1, len(stages) + 1)), "Journey numbering must be contiguous."
    assert stages[0]["type"] == "Supply" and stages[-1]["type"] == "Boss"
    return dict(initial=initial, base=[s["baseGold"] for s in stages], natural=[s["naturalXP"] for s in stages],
                curve=curve, modern=True, supplies=[s["stage"] for s in stages if s["type"] == "Supply"],
                streak=streak, interest_cap=int(interest[1]), interest_step=int(interest[2]),
                xp_price=int(paid[1]), xp_amount=int(paid[2]))


def level_up(level, xp, curve):
    while level < 10 and xp >= curve[level]:
        xp -= curve[level]; level += 1
    return level, 0 if level == 10 else xp


def forecast(name, p, policy=None, interest=True):
    gold, level, xp, streak = p["initial"], 1, 0, 0
    rows, failures = [], []
    count = len(p["base"])
    assert len(p["natural"]) == count
    if policy:
        assert all(len(policy[key]) == count for key in ("levels", "cards", "refresh")), "Policy node count differs from its source."
    for i in range(count):
        stage, supply = i + 1, i + 1 in p["supplies"]
        opening, paid = gold, 0
        target = policy["levels"][i] if policy else 1
        while level < target and gold >= p["xp_price"]:
            gold -= p["xp_price"]; paid += p["xp_price"]
            level, xp = level_up(level, xp + p["xp_amount"], p["curve"])
        if level < target: failures.append(f"stage{stage}: level{level} < target{target}")
        cards, refresh = (policy["cards"][i], policy["refresh"][i]) if policy else (0, 0)
        if cards + refresh > gold:
            failures.append(f"stage{stage}: spending {cards + refresh} exceeds {gold}"); break
        gold -= cards + refresh
        held, before_level = gold, level
        bank = min(p["interest_cap"], gold // p["interest_step"]) if interest else 0
        if not p["modern"]:
            streak += 1; bonus = min(3, max(0, streak - 1))
        elif supply:
            bonus = 0
        else:
            streak += 1; bonus = next((bonus for threshold, bonus in p["streak"] if streak >= threshold), 0)
        gold += p["base"][i] + bank + bonus
        level, xp = level_up(level, xp + p["natural"][i], p["curve"])
        rows.append(dict(stage=stage, supply=supply, opening_gold=opening, cards_and_commission=cards,
                         refresh=refresh, paid_xp=paid, battle_level=before_level, held_for_interest=held,
                         base=p["base"][i], interest=bank, streak_gold=bonus, closing_gold=gold, closing_level=level, closing_xp=xp))
    opening_supplies = next((i for i in range(count) if i + 1 not in p["supplies"]), count)
    return dict(name=name, stage_count=count, passed=not failures and len(rows) == count, failures=failures,
                after_opening_supplies=rows[opening_supplies-1]["closing_gold"] if len(rows)>=opening_supplies else None,
                pre_final_opening=rows[count-2]["closing_gold"] if len(rows)>=count-1 else None,
                pre_final_ready=rows[count-1]["held_for_interest"] if len(rows)>=count else None,
                end_gold=gold, end_level=level, end_xp=xp, total_cards=sum(r["cards_and_commission"] for r in rows),
                total_refresh=sum(r["refresh"] for r in rows), total_paid_xp=sum(r["paid_xp"] for r in rows),
                total_income=p["initial"]+sum(r["base"]+r["interest"]+r["streak_gold"] for r in rows), rows=rows)


def roles_from_assets():
    players = {u["id"]: u for u in json.loads(read(SOURCES["players"]))["units"]}
    # The runtime selector also checks Supports(id). Every input below is a known authored player.
    assert re.search(r"ShopEligible\(string id\)\s*=>\s*Supports\(id\)\s*&&\s*id\s*!=\s*SilverWolfCombat.CardId", read(SOURCES["balance"])), "Shop eligibility changed: update the standalone adapter."
    excluded = re.search(r'CardId\s*=\s*"([^"]+)"', read(SOURCES["wolf"]))[1]
    players.pop(excluded)
    cards = {}
    for asset in (ROOT / "Assets/Data").rglob("*.asset"):
        text = asset.read_text(encoding="utf-8-sig")
        match = re.search(r"\n\s+cardId:\s*(\S+)", text)
        if not match or match[1] not in players: continue
        guid = re.search(r"guid:\s*([a-f0-9]+)", asset.with_suffix(".asset.meta").read_text())[1]
        cost = int(re.search(r"\n\s+recruitCost:\s*(\d+)", text)[1])
        assert cost == players[match[1]]["cost"]
        cards[guid] = dict(id=match[1], cost=cost)
    tests = ROOT / "Assets/Data/Resources/MonsterTests"
    definitions = [ROOT / "Assets/Data/Battles/ManualDemo.asset", ROOT / "Assets/Data/Battles/TalulahTrial.asset"]
    definitions += sorted(tests.glob("*.asset"), key=lambda p: re.search(r"m_Name:\s*(.+)", p.read_text(encoding="utf-8-sig"))[1])
    ordered, seen = [], set()
    for definition in definitions:
        text = definition.read_text(encoding="utf-8-sig")
        guid = re.search(r"\n\s+card:\s*\{[^}]*guid:\s*([a-f0-9]+)", text)[1]
        role = cards.get(guid)
        if role and role["id"] not in seen:
            unit = players[role["id"]]["stars"][0]
            power = unit["hp"] * .02 + unit["attack"] / unit["interval"]
            ordered.append({**role, "power": power}); seen.add(role["id"])
    assert seen == set(players), f"Playable pool mismatch: {set(players) - seen}"
    assert len(ordered) == 42 and excluded not in seen
    return ordered


HARNESS = r'''
using System;using System.Linq;using System.Collections.Generic;using System.IO;using System.Text.Json;
using YesterdayConvoy;
public sealed class Role {public string id{get;set;} public int cost{get;set;} public float power{get;set;}}
public sealed class Policy {public string name{get;set;} public int[] levels{get;set;} public int[] cards{get;set;} public int[] refresh{get;set;} public bool commission{get;set;} public bool commissionGift{get;set;}}
public sealed class Input {public int initial{get;set;} public int xpPrice{get;set;} public int[] bases{get;set;} public int[] natural{get;set;} public int[] supplies{get;set;} public int[] seeds{get;set;} public Role[] roles{get;set;} public Policy[] policies{get;set;}}
public static class Program {
    static Dictionary<string,Role> roles;
    // The adapter exposes only the source-verified eligible set; StartingCards itself is production code.
    public static bool IsEligible(string id)=>id!=null&&roles.ContainsKey(id);
    static List<ShopTrialModel.Piece> Select(ShopTrialModel s){var chosen=s.Bench.GroupBy(p=>p.Id).Select(g=>g.OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First()).OrderByDescending(p=>roles[p.Id].power*(1+(p.Star-1)*.8f)).ThenBy(p=>p.Instance).Take(s.Level).ToList();if(!s.SetLineup(chosen.Select(p=>p.Id),chosen.Select(p=>p.Instance)))throw new Exception("Lineup capacity rejected");return chosen;}
    static float Score(ShopTrialModel s,string id){var held=s.Bench.Where(p=>p.Id==id).ToArray();if(held.Any(p=>p.Star>=3))return -1;float score=roles[id].power/roles[id].cost;if(held.Count(p=>p.Star==1)>=2)score+=100;else if(held.Length>0)score+=30;return score;}
    static int Buy(ShopTrialModel s,int budget){for(int turn=0;turn<5;turn++){Select(s);var slots=Enumerable.Range(0,5).Where(i=>s.Offers[i]!=null&&s.Definition(s.Offers[i]).Cost<=budget&&s.CanAfford(s.Definition(s.Offers[i]).Cost)).OrderByDescending(i=>Score(s,s.Offers[i])).ThenBy(i=>i).ToArray();bool bought=false;foreach(int i in slots){if(Score(s,s.Offers[i])<0)continue;var r=s.Purchase(i);if(!r.Success)continue;budget-=r.Value;Select(s);bought=true;break;}if(!bought)break;}return budget;}
    public static int Main(string[] args){
        var input=JsonSerializer.Deserialize<Input>(File.ReadAllText(args[0]));roles=input.roles.ToDictionary(r=>r.id);
        if(roles.Count!=42||roles.ContainsKey("CARD_M41"))throw new Exception("Eligible shop pool drift");
        int stages=input.bases.Length;if(input.natural.Length!=stages)throw new Exception("Node count drift");
        var reports=new List<object>();
        foreach(int seed in input.seeds)foreach(var policy in input.policies){
            if(policy.levels.Length!=stages||policy.cards.Length!=stages||policy.refresh.Length!=stages)throw new Exception("Policy node count drift");
            var s=new ShopTrialModel(input.roles.Select(r=>new ShopTrialModel.Role{Id=r.id,Name=r.id,Cost=r.cost}),seed);s.EnableJourneyEconomy();
            var gifts=CampaignJourneyEconomy.StartingCards(s.Roles,seed);string gift=gifts[0];
            if(gifts.Length!=3||gifts.Distinct().Count()!=3||gifts.Any(id=>roles[id].cost<1||roles[id].cost>2)||!gifts.SequenceEqual(CampaignJourneyEconomy.StartingCards(s.Roles,seed)))throw new Exception("Starting gift rule drift");
            s.Preset(gifts.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),1,0,input.initial);
            if(s.Bench.Count!=3||s.Bench.Any(p=>p.Star!=1)||gifts.Any(id=>s.Available(id)!=s.Capacity(id)-1))throw new Exception("Gift stock mismatch");
            Select(s);var rounds=new List<object>();int cards=0,refresh=0,xp=0,commissions=0,misses=0,income=input.initial;
            for(int i=0;i<stages;i++){
                int stage=i+1,opening=s.Gold,paid=0;
                // Initial preparation and every later unlocked shop each get exactly one free refresh.
                s.BeginPreparation();if(!s.Locked)s.Refresh(true);
                while(s.Level<policy.levels[i]&&s.Gold>=input.xpPrice){int beforeUpgrade=s.Gold;if(!s.Upgrade().Success)break;paid+=beforeUpgrade-s.Gold;xp+=beforeUpgrade-s.Gold;}
                if(s.Level<policy.levels[i])misses++;
                int budget=Math.Min(policy.cards[i],s.Gold),before=budget;
                if(policy.commissionGift&&s.Bench.Where(p=>p.Id==gift).Max(p=>p.Star)<3&&roles[gift].cost+2<=budget){var r=s.Commission(gift);if(r.Success){budget-=r.Value;commissions++;Select(s);}}
                budget=Buy(s,budget);
                if(policy.commission&&!s.CommissionUsed){foreach(string id in s.Bench.Select(p=>p.Id).Distinct().Where(id=>roles[id].cost<=3&&roles[id].cost+2<=budget&&s.Bench.Count(p=>p.Id==id&&p.Star==1)>=2).OrderByDescending(id=>Score(s,id)).ToArray()){var r=s.Commission(id);if(!r.Success)continue;budget-=r.Value;commissions++;Select(s);break;}}
                cards+=before-budget;int refreshBudget=policy.refresh[i];
                while(refreshBudget>=2&&s.Gold>=2){if(!s.Refresh().Success)break;refresh+=2;refreshBudget-=2;int beforeBuy=s.Gold;budget=Buy(s,Math.Min(budget,s.Gold));cards+=beforeBuy-s.Gold;}
                var selected=Select(s);int held=s.Gold,level=s.Level;
                var reward=s.SettleRound("stage-"+stage,true,input.bases[i],input.natural[i],!input.supplies.Contains(stage));income+=reward.Total;s.Validate();
                if(income!=s.Gold+cards+refresh+xp)throw new Exception("Cash conservation mismatch");
                rounds.Add(new{stage,opening,held,level,closing=s.Gold,closingLevel=s.Level,xp=s.XP,paidXP=paid,reward=new{reward.Base,reward.Interest,reward.Streak},lineup=selected.Select(p=>new{id=p.Id,star=p.Star}),stockConserved=true});
            }
            reports.Add(new{seed,policy=policy.name,gift,gifts,startingGiftRulePassed=true,eligibleRoles=roles.Count,level=s.Level,gold=s.Gold,xp=s.XP,totalIncome=income,totalCards=cards,totalRefresh=refresh,totalPaidXP=xp,cashConserved=true,commissions,levelTargetMisses=misses,highestStar=s.Bench.Max(p=>p.Star),threeStars=s.Bench.Count(p=>p.Star>=3),rounds});
        }
        Console.WriteLine(JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));return 0;
    }
}
'''


def actual_shop_audit(out, current, config):
    folder = out / "shop-harness"; folder.mkdir(parents=True, exist_ok=True)
    # Keep production StartingCards without pulling Unity combat dependencies into a cash audit.
    # roles_from_assets verifies runtime eligibility before the adapter exposes those IDs.
    (folder / "LegacyShim.cs").write_text("namespace YesterdayConvoy {public static class CampaignPlan {public static readonly int[] XP={0,2,4,6,10,14,20,28,36,48,0};} public static class CampaignBalance {public static bool ShopEligible(string id)=>global::Program.IsEligible(id);}}", encoding="utf-8")
    links = "".join(f'<Compile Include="{escape(str(ROOT / SOURCES[k]))}" Link="{k}.cs" />' for k in ("shop", "economy"))
    (folder / "shop.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>'+links+'</ItemGroup></Project>', encoding="utf-8")
    (folder / "Program.cs").write_text(HARNESS, encoding="utf-8")
    payload = dict(initial=current["initial"], xpPrice=current["xp_price"], bases=current["base"], natural=current["natural"], supplies=current["supplies"],
                   seeds=config["seeds"], roles=roles_from_assets(), policies=config["policies"])
    input_path = folder / "input.json"; input_path.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    env={**os.environ,"DOTNET_CLI_HOME":str(out/"dotnet-home"),"DOTNET_SKIP_FIRST_TIME_EXPERIENCE":"1","DOTNET_CLI_TELEMETRY_OPTOUT":"1"}
    built = subprocess.run(["dotnet", "build", str(folder / "shop.csproj"), "-v:q", "--nologo"], env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (out / "shop-build.log").write_text(built.stdout+built.stderr, encoding="utf-8")
    if built.returncode: raise RuntimeError("Shop harness build failed; see shop-build.log")
    executed = subprocess.run(["dotnet", str(folder / "bin/Debug/net10.0/shop.dll"), str(input_path)], env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if executed.returncode: raise RuntimeError(executed.stderr)
    result = json.loads(executed.stdout); (out / "seed-shop-budget.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    return result


def main():
    parser=argparse.ArgumentParser();parser.add_argument("--output",default="artifacts/campaign-retune");parser.add_argument("--cash-only",action="store_true");args=parser.parse_args()
    out=(ROOT/args.output).resolve();out.mkdir(parents=True,exist_ok=True)
    config=json.loads(INPUT.read_text(encoding="utf-8-sig"));current=source_parameters();count=len(current["base"])
    baseline={**current,**config["baseline"]};baseline.pop("note",None)
    cases=[forecast("historical-thirteen-node-no-spending",baseline),forecast("current-no-spending",current)]
    cases += [forecast(p["name"]+"-cash-caps",current,p) for p in config["policies"]]
    # The retired thirteen-node values are checked solely against their explicit historical inputs.
    # Candidate policies remain observations; a forecast failure must not become a current balance claim.
    failed={**config["policies"][-1],"refresh":[0,0,0,0,2,0,2,0,2,0,2,4]}
    rejected=forecast("legacy-aggressive-refresh-caps-on-current-run",current,failed)
    extreme_policy=dict(name="theoretical-level10-late-purchase",levels=[1]*(count-1)+[10],cards=[0]*count,refresh=[0]*count)
    extreme=forecast("current-theoretical-level10",current,extreme_policy)
    retired_curve=current["curve"][:];retired_curve[9]=64
    retired64=forecast("retired-level10-xp64-on-current-run",{**current,"curve":retired_curve},extreme_policy)
    retired64["retiredCurve"]=retired_curve
    natural_pre_final=sum(current["natural"][:-1])
    costs=[]
    for level in (7,8,9,10):
        needed=max(0,sum(current["curve"][1:level])-natural_pre_final);paid=((needed+current["xp_amount"]-1)//current["xp_amount"])*current["xp_price"]
        costs.append(dict(level=level,cumulativeXP=sum(current["curve"][1:level]),naturalPreFinalXP=natural_pre_final,minimumPaidGold=paid))
    checks={"historicalBaseline36":cases[0]["after_opening_supplies"]==36,"historicalBaseline301":cases[0]["pre_final_opening"]==301,"historicalBaseline337":cases[0]["end_gold"]==337,
            "currentNodeCount12":count==12,"currentOpeningSupply9":cases[1]["after_opening_supplies"]==9,
            "currentPreFinal112":cases[1]["pre_final_opening"]==112,"currentEnd127":cases[1]["end_gold"]==127,
            "currentBaseTotal68":sum(current["base"])==68,"currentPreFinalBase61":sum(current["base"][:-1])==61,
            "currentNoSpendingLedger":cases[1]["total_income"]==cases[1]["end_gold"],
            "policyArraysMatchLiveNodes":all(all(len(p[key])==count for key in ("levels","cards","refresh")) for p in config["policies"])}
    audit=[] if args.cash_only else actual_shop_audit(out,current,config)
    if audit:
        checks["seedPoliciesCashConserved"]=all(a["cashConserved"] for a in audit)
        checks["seedPoliciesStockConserved"]=all(r["stockConserved"] for a in audit for r in a["rounds"])
        checks["productionThreeGiftRulePassed"]=all(a["startingGiftRulePassed"] and len(a["gifts"])==len(set(a["gifts"]))==3 for a in audit)
        checks["eligibleShopPool42WithoutSilverWolf"]=all(a["eligibleRoles"]==42 and "CARD_M41" not in a["gifts"] for a in audit)
        checks["seedPoliciesUseLiveNodeCount"]=all(len(a["rounds"])==count for a in audit)
    observations=dict(policyBudgetForecastsAffordable=all(c["passed"] for c in cases[2:]),
                      failedCandidatePolicies=[dict(name=c["name"],failures=c["failures"]) for c in cases[2:] if not c["passed"]],
                      seededPolicyLevelTargetMisses=[dict(seed=a["seed"],policy=a["policy"],misses=a["levelTargetMisses"]) for a in audit if a["levelTargetMisses"]],
                      theoreticalLevel10AffordableBeforeFinal=extreme["passed"],
                      theoreticalLevel10MinimumPaidGold=costs[-1]["minimumPaidGold"],
                      noSpendingPreFinalGold=cases[1]["pre_final_opening"])
    report=dict(modelVersion=config["modelVersion"],sources={k:{"path":v,"sha256":hashlib.sha256((ROOT/v).read_bytes()).hexdigest()} for k,v in SOURCES.items()},
                assumptions=[f"All {count} current settlements assume victory. This is an economic budget model, never a combat clear claim.","The separately labelled historical case retains thirteen old nodes and is never a live target.","Actual ShopTrialModel, CampaignJourneyEconomy.StartingCards, Random, inventory, merging and transactions compile in a temporary .NET harness; no Unity.","The CampaignBalance adapter accepts only the source-verified 42 eligible IDs; silver wolf remains outside the shop.","No sales, extra free rerolls, economic synergies, equipment or environment income. Each preparation includes its normal free refresh.","Card and refresh ceilings are candidate human policies. Failed forecasts and missed level targets remain observations, not validation passes."],
                current=current,checks=checks,observations=observations,levelCosts=costs,scenarios=cases,rejectedPlan=rejected,
                theoreticalLevel10=extreme,retired64Lockout=retired64,
                seedPolicySummaries=[{k:v for k,v in a.items() if k!="rounds"} for a in audit])
    (out/"economy-report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    for case in cases:
        with (out/(case["name"]+".csv")).open("w",newline="",encoding="utf-8-sig") as f:
            writer=csv.DictWriter(f,fieldnames=case["rows"][0].keys());writer.writeheader();writer.writerows(case["rows"])
        print(json.dumps({k:v for k,v in case.items() if k!="rows"},ensure_ascii=False))
    print("LEVEL_COSTS",json.dumps(costs));print("CHECKS",json.dumps(checks));print("OBSERVATIONS",json.dumps(observations))
    for item in report["seedPolicySummaries"]:print("SEED",json.dumps(item,ensure_ascii=False))
    if not all(checks.values()):raise SystemExit(1)


if __name__=="__main__":main()
