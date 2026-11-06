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
        static string GuerrillaEffect()
        {
            var lines=new List<string>{"普通实体首次有效直接攻击/受击+1军令，每种卡最多2点；支援/分体不产点。所有档整令不限次数。"};
            foreach(int tier in new[]{2,4,6,8,9})
            {
                var rule=GuerrillaRules.ForTier(tier);
                string people=rule.ParticipantLimit==int.MaxValue?"全部合格普通实体":"最多"+rule.ParticipantLimit+"人";
                lines.Add($"{tier}档：整令{rule.Cost}点 / {people} / {Mathf.RoundToInt(rule.AttackMultiplier*100)}%攻击"+(rule.InitialOrders>0?$"，开战+{rule.InitialOrders}军令。":"。"));
            }
            lines.Add("0.9演出秒，对每名存活守军各结算一份参与者预算，逐名过护甲/抗性；不递归产点。");
            lines.Add("2/4/6需普攻收手且有射程内目标；8/9可在移动/普攻中参加，无需射程目标。技能、复起、控制锁照旧。");
            lines.Add("4档参与近战护盾10%生命3秒；6/8/9档15%生命5秒。");
            lines.Add("仅6档可拆两次3点半令，后半排除前半参与者；半令未完成不能插入整令。暂停或无合格单位/存活守军不扣费。");
            return string.Join("\n",lines);
        }
        public static SynergySummary[] From(IEnumerable<string> lineup,IEnumerable<string> extraMembers=null)
        {
            var ids=lineup.Distinct().ToArray();
            var transfers=new HashSet<string>((extraMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.Guerrilla(id)));
            string[] names={"整合运动","雪怪小队","感染生物","游击队","泥岩小队","牧群","深池","海嗣","萨卡兹"};
            int[][] levels={new[]{2,4,6},new[]{2,5},new[]{2,4,6},new[]{2,4,6,8,9},new[]{2,4,5},new[]{2,4},new[]{2,4},new[]{2,4},new[]{2,4,6}};
            string[] effects={
                "2档：部署2种不同卡触发1名援军，本场1次。\n4档：部署3种触发2名所选援军，本场2次。\n6档：部署3种触发士兵与弩手各1，本场3次；援军获15%生命护盾4秒。援军不扣蓝门防御。",
                "冻结守军在2.5秒内受到两种不同原卡直接命中时碎冰。\n2档：50法伤，半径0.9/最多2人；消耗冻结，目标冷却4秒。\n5档：120法伤，半径1.5/最多5人；保留冻结，目标冷却3秒。\n仅5档：3次碎冰或霜星手动冰封发动8秒暴风雪，开启清零寒意；每次碎冰只计1点，天气期间不积累、不叠加、不续时。\n0/2/4/6秒全场寒潮，每次(60+目标最大生命2%)法伤、过法抗；碎冰/寒潮按雪怪平均星级每升1星+35%。\n寒冷：攻速70%；再次受寒冷冻结1.2秒。天气冻结结束后保护3秒，只防控制、不免寒潮伤害。",
                "战前选主形态，6档再选另一种2档副形态。\n集群2/4/6：2只×55%、2只×70%、3只×55%生命/攻击；一次扣费，额外分体不扣蓝门。\n甲壳：18/30/35%生命护盾6秒，护甲+0/2/3。\n爆裂：原体死亡造成35/65/85法伤，半径1/1.2/1.3，最多2/3/3人；每升一星+35%。原有死亡爆炸独立保留并按自身范围/伤害类型结算；额外爆裂只算一次，不重复寒冷。",
                GuerrillaEffect(),
                "2/4/5档：胚体 / 进阶培养 / 终极资格。正式胜利且2种小队成员真实交战+3培养；4档且巨像交战再+1。补给不培养。每个关间最多投入10金币。\nI/II/III/IV需培养0/4/12/24、投入0/20/45/80、成员2/2/4/5；IV还需5人全二星。\n生命360/550/1800/6000，攻击60/84/240/1600，DP4/5/8/14。每场一次，不扣门血。IV至多3目标、每第三击全场80%攻击法伤、每次命中回8%生命，免疫控制。",
                "牧群：全队非五费正常实体最终死亡后生成短时转化体。2档前3个、35%生命/攻击、4甲、10秒；4档前5个、45%生命/攻击、6甲、12秒，可保留远程基础攻击（2.5射程、攻击再乘85%）。\n转化沿原位置/路线，穿门0，不复制主动、爆炸、羁绊资源或再次转化。原动作较长时攻击间隔保留完整动作。",
                "深池：被灼烧守军遭正常伤害击杀留下火种。2档最多4枚，传播半径1.2、最多2人、15法伤/秒持续3秒；4档最多6枚、半径1.6、3人、25/秒。\n4档可消耗2火种手动引爆，半径1.8、最多4人、120法伤，每场2次。伤害每成员平均星级增加25%。衍生击杀不再产火种。",
                "海嗣：胜利时每种实际交战成员+1培养，2档最多2、4档最多3；0/3/7经验到I/II/III。\n屏障：首次远程直接命中减伤30%；II前2次30%；III前2次40%并给附近非海嗣一份20%保护。\n猎食：对低于35%生命目标伤害+12/18/25%；III首次击杀治疗附近两人各5%生命。\n渗透：各实体首次命中使护甲-3/5/7，4/5/5秒，同名不叠。4档II阶段解锁另一种I阶段副适应。",
                "萨卡兹：战前指定承契卡，其余普通友军首次由直接伤害跨入40%生命以下贡献1点，每种卡最多2点。4点手动选择存活承契实体爆发，不扣DP。\n2/4/6档：攻击+20/30/40%、15/25/30%生命盾，持续6/8/10秒，本场1/2/2次。6档其他普通友军各获承契者10%生命盾、最多200。\n自损、转化体、巨像和援军不贡献；同一实体只贡献一次。"};
            return names.Select((name,i)=>new SynergySummary{Name=name,Members=ids.Where(id=>CampaignBalance.Tags(id).Contains(name)||name=="游击队"&&transfers.Contains(id)).ToArray(),Thresholds=levels[i],Effect=effects[i]})
                .Select(s=>{s.Count=s.Members.Length;s.Tier=s.Name=="游击队"?GuerrillaRules.TierForCount(s.Count):s.Thresholds.Where(n=>s.Count>=n).DefaultIfEmpty(0).Max();return s;})
                .Where(s=>s.Count>0).OrderByDescending(s=>s.Tier>0).ThenByDescending(s=>s.Count).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        string inspectedSynergy;
        Vector2 guerrillaDetailScroll;
        Rect SynergyListRect=>new Rect(16,69,157,28+Mathf.Min(350,CurrentSynergies.Length*46));
        Rect SynergyDetailRect=>inspectedSynergy=="感染生物"?InfectionFormPanelRect:new Rect(252,90,620,415);
        SynergySummary[] CurrentSynergies=>SynergySummary.From(shopLineup,PreparedGuerrillaTransferIds);
        bool SynergyBlocksMap(Vector2 point)=>CampaignActive&&(SynergyListRect.Contains(point)||inspectedSynergy!=null);
        void DrawSynergySidebar()
        {
            if(!CampaignActive)return;
            DrawSnowstormFeedback();
            var summaries=CurrentSynergies;var bounds=SynergyListRect;
            Fill(new Rect(bounds.x,bounds.y,bounds.width,25),ApprovedUi.Paper);
            GUI.Label(new Rect(bounds.x+8,bounds.y,55,25),"羁绊",CampaignText(12,weight:600));
            GUI.Label(new Rect(bounds.x+58,bounds.y,91,25),summaries.Count(s=>s.Tier>0)+"条已激活",CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
            if(summaries.Length==0){inspectedSynergy=null;return;}
            Rect viewport=new Rect(bounds.x,bounds.y+28,bounds.width,bounds.height-28);
            float contentWidth=summaries.Length*46>viewport.height?139:157;
            campaignSynergyScroll=GUI.BeginScrollView(viewport,campaignSynergyScroll,new Rect(0,0,contentWidth,Mathf.Max(1,summaries.Length*46)),false,false);
            try
            {
                for(int i=0;i<summaries.Length;i++)
                {
                    var s=summaries[i];Rect row=new Rect(0,i*46,contentWidth,41);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))
                    {inspectedSynergy=inspectedSynergy==s.Name?null:s.Name;campaignConstructOpen=false;CancelShopDrag();}
                    bool active=s.Tier>0,snowstorm=s.Name=="雪怪小队"&&SnowstormHudActive;
                    Color ink=snowstorm?SnowstormInk:active?ApprovedUi.Ink:ApprovedUi.Muted;
                    Fill(row,snowstorm?SnowstormPaper:active?ApprovedUi.Paper:new Color(.85f,.89f,.91f,.89f));
                    Fill(new Rect(row.x,row.y,3,row.height),snowstorm?SnowstormBlue:active?(s.Name=="感染生物"?InfectionOrange:new Color(.72f,.59f,.30f)):ApprovedUi.Rule);
                    GUI.Label(new Rect(9,row.y+3,contentWidth-43,21),s.Name+(s.Name=="感染生物"&&InfectionChoiceEditable?" ›":""),CampaignText(13,color:ink,weight:600));
                    GUI.Label(new Rect(contentWidth-31,row.y+1,23,23),s.Count.ToString(),CampaignText(15,TextAnchor.MiddleRight,ink,600));
                    if(s.Name=="感染生物")DrawInfectionFormRow(row,s.Tier);
                    else if(snowstorm)DrawSnowstormRow(row);
                    else if(s.Name=="雪怪小队"&&SnowstormChargeHudVisible)DrawSnowstormChargeRow(row);
                    else
                    {
                        float pitch=s.Thresholds.Length>3?12:22,width=s.Thresholds.Length>3?10:19;
                        for(int t=0;t<s.Thresholds.Length;t++)Fill(new Rect(10+t*pitch,row.y+31,width,2),s.Count>=s.Thresholds[t]?new Color(.66f,.53f,.23f):new Color(.68f,.75f,.80f));
                        string target=s.Tier>0?s.Tier+"档":s.Implemented?s.Count+"/"+s.Thresholds[0]:"待实现";
                        GUI.Label(new Rect(contentWidth-62,row.y+22,54,16),target,CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
                    }
                }
            }
            finally {GUI.EndScrollView();}
            var detail=summaries.FirstOrDefault(s=>s.Name==inspectedSynergy);if(detail==null){inspectedSynergy=null;return;}
            if(detail.Name=="感染生物"){DrawInfectionFormPanel(detail);return;}
            Fill(SynergyDetailRect,ApprovedUi.Paper);ApprovedUi.Border(SynergyDetailRect,ApprovedUi.Rule);
            GUI.Label(new Rect(268,102,510,28),$"{detail.Name} · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":detail.Implemented?"未达门槛":"待实现"),CampaignText(17,weight:700));
            if(LoopButton(new Rect(803,102,53,28),"关闭")){inspectedSynergy=null;return;}
            var body=CampaignText(14,TextAnchor.UpperLeft);body.wordWrap=true;
            if(detail.Name=="游击队")
            {
                var detailViewport=new Rect(268,149,588,269);float content=body.CalcHeight(new GUIContent(detail.Effect),detailViewport.width-18)+8;
                guerrillaDetailScroll=GUI.BeginScrollView(detailViewport,guerrillaDetailScroll,new Rect(0,0,detailViewport.width-18,Mathf.Max(detailViewport.height,content)),false,false);
                try{GUI.Label(new Rect(0,0,detailViewport.width-18,content),detail.Effect,body);}
                finally{GUI.EndScrollView();}
            }
            else GUI.Label(new Rect(268,149,588,269),detail.Effect,body);
            string members=string.Join("、",detail.Members.Select(id=>shop.Definition(id).Name+(detail.Name=="游击队"&&IsGuerrillaTransfer(id)?"（转职）":"")));
            Fill(new Rect(268,430,588,1),ApprovedUi.Rule);
            var memberStyle=CampaignText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);memberStyle.wordWrap=true;
            GUI.Label(new Rect(268,441,588,54),"编队成员："+members+"\n按战前编队锁定；未部署或死亡不改变档位。",memberStyle);
        }
    }
}
