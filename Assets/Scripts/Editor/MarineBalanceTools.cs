using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class MarineBalanceTools
    {
        [Serializable]sealed class Case
        {
            public string roster,outcome;public bool effectEnabled;public int star,stage,tier,tiles,bursts,deployed;
            public float seconds,firstBurst,bonusNeural,groundNeural;
        }
        [Serializable]sealed class Report
        {
            public string scope="Deterministic combat-only budget audit. Real CampaignBalance star profiles, real stage 6/10/13 defenders and saved MarineTrial lanes. Formal stage DP, normal costs/CD/uses. Fixed round-robin deployment; no shop/economy or claim of optimal play. Effect-off cases keep the same four cards and disable only the synergy snapshot.";
            public List<Case> cases=new List<Case>();
        }
        public static void Run()
        {
            var report=new Report();
            foreach(int star in new[]{1,2,3})foreach(int stage in new[]{6,10,13})
            {
                foreach(var ids in new[]{new[]{"CARD_M33","CARD_M34"},new[]{"CARD_M35","CARD_M36"},new[]{"CARD_M33","CARD_M34","CARD_M35","CARD_M36"}})
                    report.cases.Add(Simulate(ids,star,stage,true));
                report.cases.Add(Simulate(new[]{"CARD_M33","CARD_M34","CARD_M35","CARD_M36"},star,stage,false));
            }
            Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/marine-balance.json",JsonUtility.ToJson(report,true));
            foreach(var c in report.cases)Debug.Log($"MARINE_BALANCE stage={c.stage} star={c.star} tier={c.tier} effect={c.effectEnabled} {c.roster}: {c.outcome} at {c.seconds:F2}s first={c.firstBurst:F2} bursts={c.bursts} tiles={c.tiles} bonus={c.bonusNeural:F1}");
        }
        static Case Simulate(string[] ids,int star,int stage,bool enabled)
        {
            var definition=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/Data/SynergyTrials/Encounters/marine-defenders.asset");
            var map=UnityEngine.Object.Instantiate(definition);map.defenders=CampaignPlan.Guards(stage,false);
            try
            {
                int dp=CampaignPlan.BattleDP[stage-1];
                int defense=Mathf.Clamp(1+(stage-2)/2,1,7);
                var profiles=ids.Select(id=>CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/"+id.Substring(5)),star,dp,defense)).ToArray();
                var b=new ManualBattle(profiles[0].Unit,map.route,definition.battle.Engagement.DefenderPosition,profiles[0].Combat,null,map.Snapshot(),map.LaneSnapshot());
                b.ConfigureCardLoadout(profiles.Skip(1));b.ConfigureStrongholdMap(false);
                b.ConfigureSynergies((enabled?ids:Array.Empty<string>()).Select(id=>new ShopTrialModel.Piece{Id=id,Star=star}));
                var ground=new List<Vector3>();for(int row=-1;row<=7;row++)for(int col=2;col<=18;col++)
                {char tile=StrongholdChallengeLayout.Tile(col,row);if(tile!='#'&&tile!='H'&&tile!='h')ground.Add(StrongholdMapLayout.Cell(col,row));}
                b.ConfigureMarineTerrain(ground);foreach(var d in b.Defenders)b.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
                b.Begin();long request=0;float first=-1;int wave=0;
                for(int step=0;step<6000&&b.Phase==BattlePhase.Running;step++)
                {
                    if(step%20==0)
                    {
                        for(int i=0;i<ids.Length;i++)b.TryDeployRole(b.RunId,++request,b.Lanes[(wave+i)%b.Lanes.Count].Points[0],ids[i]);
                        wave++;
                        foreach(var u in b.Units.Where(u=>u.Active&&b.IsSkillReady(u.EntityId)).ToArray())b.TryActivateSkill(b.RunId,++request,u.EntityId);
                    }
                    b.Advance(.05f);if(first<0&&b.NeuralBursts>0)first=(float)b.Clock;
                }
                return new Case{roster=string.Join("+",ids),effectEnabled=enabled,star=star,stage=stage,tier=b.MarineTier,outcome=b.Phase.ToString(),seconds=(float)b.Clock,
                    firstBurst=first,tiles=b.MarineCells.Count,bursts=b.NeuralBursts,deployed=b.Units.Count,bonusNeural=b.MarineBonusNeuralDealt,groundNeural=b.MarineGroundNeuralDealt};
            }
            finally{UnityEngine.Object.DestroyImmediate(map);}
        }
    }
}
