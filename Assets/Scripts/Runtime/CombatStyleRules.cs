using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    public enum CombatStyle { None, Assault, Marksman, Mystic, Bulwark, Frenzy, Coordination }

    // A second, independent tag axis. CampaignBalance.Tags remains the faction API.
    public static class CombatStyleRules
    {
        public sealed class Definition
        {
            public readonly CombatStyle Style;
            public readonly string Name;
            readonly string[] members;
            public IReadOnlyList<string> Members=>Array.AsReadOnly(members);
            public int[] Thresholds=>Style==CombatStyle.Coordination?new[]{2,4}:new[]{2,4,6};
            internal Definition(CombatStyle style,string name,params string[] ids){Style=style;Name=name;members=ids;}
        }
        static readonly Definition[] definitions={
            new Definition(CombatStyle.Assault,"强攻","CARD_ORIGINIUM_SLUG","CARD_M04","CARD_M06","CARD_M13","CARD_M19","CARD_M20","CARD_M31","CARD_M37"),
            new Definition(CombatStyle.Marksman,"射手","CARD_M08","CARD_M11","CARD_M16","CARD_M26","CARD_M36","CARD_WISADEL_APRIL"),
            new Definition(CombatStyle.Mystic,"秘术","CARD_M09","CARD_M12","CARD_M42","CARD_M38","CARD_M14","CARD_M32","CARD_TALULAH_TRIAL"),
            new Definition(CombatStyle.Bulwark,"壁垒","CARD_M07","CARD_M10","CARD_M18","CARD_M27","CARD_M43","CARD_M29","CARD_M33","CARD_M28"),
            new Definition(CombatStyle.Frenzy,"狂袭","CARD_M02","CARD_M03","CARD_M15","CARD_M22","CARD_M23","CARD_M24","CARD_M30","CARD_M34","CARD_M35"),
            new Definition(CombatStyle.Coordination,"协同","CARD_M05","CARD_M17","CARD_M21","CARD_M25"),
        };
        public static IReadOnlyList<Definition> Definitions=>Array.AsReadOnly(definitions);
        public static Definition Find(string name)=>definitions.FirstOrDefault(d=>d.Name==name);
        public static Definition Find(CombatStyle style)=>definitions.FirstOrDefault(d=>d.Style==style);
        public static CombatStyle For(string id)=>definitions.FirstOrDefault(d=>d.Members.Contains(id))?.Style??CombatStyle.None;
        public static int Tier(CombatStyle style,int count)=>Find(style)?.Thresholds.Where(t=>count>=t).DefaultIfEmpty(0).Max()??0;
        public static float Pierce(int tier)=>tier>=6?.50f:tier>=4?.35f:tier>=2?.20f:0;
        public static float Exposure(int tier)=>tier>=6?.18f:tier>=4?.12f:tier>=2?.06f:0;
        public static float Pulse(int tier)=>tier>=6?1:tier>=4?.70f:tier>=2?.40f:0;
        public static float Shield(int tier)=>tier>=6?.20f:tier>=4?.14f:tier>=2?.08f:0;
        public static float RageAttack(int tier)=>tier>=6?.35f:tier>=4?.25f:tier>=2?.15f:0;
        public static float Leech(int tier)=>tier>=6?.16f:tier>=4?.12f:tier>=2?.08f:0;
        public static int SP(int tier)=>tier>=4?8:tier>=2?5:0;
        static string Percent(float value)=>(value*100).ToString("0")+"%";
        public const string Common="打法与阵营同时生效；按战前不同卡种锁档，仅最高档生效。备战、支援、分体、巨像与灵体不计数，也不独立触发或领取效果。跨波保留记录，暂停冻结计时，重开清空本场状态；不额外乘总星倍率。";
        public static string Effect(CombatStyle style,int tier)
        {
            switch(style)
            {
                case CombatStyle.Assault:return $"成员普攻有效命中正在阻挡普通友军的守军，施加4秒破阵。全队普通实体直接物理伤害忽略其{Percent(Pierce(tier))}当前正护甲；同名取高刷新。先破甲，再计算忽略；首击不追溯加成。";
                case CombatStyle.Marksman:return $"两种不同射手卡在2.5秒内直接命中同一守军，使其暴露4秒。全队普通实体远程直接伤害＋{Percent(Exposure(tier))}；同种多实体不能独立凑双来源，同名刷新不叠层。";
                case CombatStyle.Mystic:return $"成员每完成3次有效普攻，产生一次{Percent(Pulse(tier))}自身实际攻击力的法术脉冲：命中目标及其1格内最近另外2人。每个实体独立计数，每次完整普攻最多计一次；脉冲为派生伤害，不递归触发羁绊。";
                case CombatStyle.Bulwark:return $"成员每次正常部署后，首次承受守军有效直接攻击且存活，给自己和1.5格内最近{(tier>=6?2:1)}名普通友军各自最大生命{Percent(Shield(tier))}护盾，持续5秒。同名取高刷新，不相加；复起不刷新触发资格。";
                case CombatStyle.Frenzy:return $"成员首次降至50%生命或以下且存活，进入8秒狂热：攻击＋{Percent(RageAttack(tier))}，普通攻击额外吸取主目标实际生命伤害的{Percent(Leech(tier))}。新增吸血每次完整普攻合计最多恢复自身最大生命5%；原生吸血保留，自损可触发，群攻副目标/持续伤害不计新增吸血，复起不刷新。攻击与同类队伍加成相加。";
                case CombatStyle.Coordination:return $"每种成员本场首次有效直接命中或有效治疗贡献1份记录。每集齐两种，消耗两份，为当前在场且可行动、有SP技能的普通实体回复{SP(tier)} SP；2档最多1次、4档最多2次。重复部署不刷来源，满技力溢出不储存，不重置弹药或技能冷却。";
                default:throw new ArgumentOutOfRangeException(nameof(style));
            }
        }
        public static IEnumerable<SynergySummary> Summaries(IEnumerable<string> lineup)
        {
            var ids=new HashSet<string>(lineup,StringComparer.Ordinal);
            foreach(var d in definitions)
            {
                var chosen=d.Members.Where(ids.Contains).ToArray();int tier=Tier(d.Style,chosen.Length);
                yield return new SynergySummary{Name=d.Name,Members=chosen,Count=chosen.Length,Tier=tier,Thresholds=d.Thresholds,
                    Effect=Common+"\n"+string.Join("\n",d.Thresholds.Select(t=>t+"档："+Effect(d.Style,t)))};
            }
        }
    }
}
