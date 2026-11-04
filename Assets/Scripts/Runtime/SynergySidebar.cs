using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class SynergySummary
    {
        public string Name {get;internal set;}
        public int Count {get;internal set;}
        public int Tier {get;internal set;}
        public int[] Thresholds {get;internal set;}
        public bool Implemented=>Thresholds.Length>0;
        public string Effect {get;internal set;}
        public string[] Members {get;internal set;}
        public static SynergySummary[] From(IEnumerable<string> lineup)
        {
            var ids=lineup.Distinct().ToArray();
            string[] names={"整合运动","雪怪小队","感染生物","游击队","泥岩小队","牧群","深池","海嗣","萨卡兹"};
            int[][] levels={new[]{2,4,6},new[]{2,4},new[]{2,4,6},new[]{2,4,6},new[]{2},Array.Empty<int>(),Array.Empty<int>(),Array.Empty<int>(),Array.Empty<int>()};
            string[] effects={
                "2档：部署2种不同卡触发1名援军，本场1次。\n4档：部署3种触发2名所选援军，本场2次。\n6档：部署3种触发士兵与弩手各1，本场3次；援军获15%生命护盾4秒。援军不扣蓝门防御。",
                "两种不同卡在2.5秒内直接命中同一冻结目标，消耗冻结触发碎冰；每目标冷却4秒。\n2档：50法伤，半径0.9，最多2人。\n4档：85法伤，半径1.3，最多3人，并给旁目标寒冷3.5秒。伤害按雪怪平均星级每升一星+35%。",
                "战前选主形态，6档再选另一种2档副形态。\n集群2/4/6：2只×55%、2只×70%、3只×55%生命/攻击；一次扣费，额外分体不扣蓝门。\n甲壳：18/30/35%生命护盾6秒，护甲+0/2/3。\n爆裂：原体死亡造成35/65/85法伤，半径1/1.2/1.3，最多2/3/3人；每升一星+35%。",
                "普通实体首次直接攻击或受击+1军令，每种卡最多2点；支援/分体不产点。\n2/4/6档：整令消耗4/5/6点，本场1/2/2次，最多6/8/12人追加60/80/90%攻击。\n4/6档近战获10/15%生命护盾3/5秒。6档可拆两次3点半令，参与者不重复。暂停或无目标不消耗。",
                "2档解锁巨像，右侧支援卡拖到红门，每场一次。\n胜利且实际交战：两种小队成员各+1培养，巨像+2，最多4点。\nI/II/III：累计0/4/10经验，生命360/550/800，攻击60/84/116，护甲8/10/12，DP4/5/6。\nII每第三击附80物伤；III附120物伤并波及另2人各100物伤。III封顶，3档未开放。",
                "该阵营标签已有成员，羁绊效果尚未接入，本场不提供加成。",
                "该阵营标签已有成员，羁绊效果尚未接入，本场不提供加成。",
                "该阵营标签已有成员，羁绊效果尚未接入，本场不提供加成。",
                "该阵营标签已有成员，羁绊效果尚未接入，本场不提供加成。"};
            return names.Select((name,i)=>new SynergySummary{Name=name,Members=ids.Where(id=>CampaignBalance.Tags(id).Contains(name)).ToArray(),Thresholds=levels[i],Effect=effects[i]})
                .Select(s=>{s.Count=s.Members.Length;s.Tier=s.Thresholds.Where(n=>s.Count>=n).DefaultIfEmpty(0).Max();return s;})
                .Where(s=>s.Count>0).OrderByDescending(s=>s.Tier>0).ThenByDescending(s=>s.Count).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        string inspectedSynergy;
        Rect SynergyListRect=>new Rect(16,Loop.CanShop?268:64,230,28+CurrentSynergies.Length*25);
        static Rect SynergyDetailRect=>new Rect(258,70,510,355);
        SynergySummary[] CurrentSynergies=>SynergySummary.From(shopLineup);
        bool SynergyBlocksMap(Vector2 point)=>CampaignActive&&(SynergyListRect.Contains(point)||inspectedSynergy!=null&&SynergyDetailRect.Contains(point));
        void DrawSynergySidebar()
        {
            if(!CampaignActive)return;
            var summaries=CurrentSynergies;Fill(SynergyListRect,LoopInk);
            GUI.Label(new Rect(25,SynergyListRect.y+2,210,23),"羁绊 · 不同种类 / 点击查看",LoopText(12));
            for(int i=0;i<summaries.Length;i++)
            {
                var s=summaries[i];string state=!s.Implemented?"待实现":s.Tier>0?"已激活":"未激活";
                string target=s.Implemented?"/"+(s.Thresholds.FirstOrDefault(t=>t>s.Count)>0?s.Thresholds.First(t=>t>s.Count):s.Thresholds.Last()):"";
                if(LoopButton(new Rect(23,SynergyListRect.y+27+i*25,216,23),$"{s.Name} {s.Count}{target} · {state}"))inspectedSynergy=inspectedSynergy==s.Name?null:s.Name;
            }
            var detail=summaries.FirstOrDefault(s=>s.Name==inspectedSynergy);if(detail==null){inspectedSynergy=null;return;}
            Fill(SynergyDetailRect,LoopPanel);
            GUI.Label(new Rect(272,80,430,28),$"{detail.Name} · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":detail.Implemented?"未达门槛":"待实现"),LoopText(17));
            if(LoopButton(new Rect(714,80,40,28),"关闭")){inspectedSynergy=null;return;}
            var body=LoopText(14);body.wordWrap=true;body.alignment=TextAnchor.UpperLeft;
            GUI.Label(new Rect(272,118,480,210),detail.Effect,body);
            string members=string.Join("、",detail.Members.Select(id=>shop.Definition(id).Name));
            GUI.Label(new Rect(272,335,480,75),"编队成员："+members+"\n按战前编队锁定；未部署或死亡不改变档位。",body);
        }
    }
}
