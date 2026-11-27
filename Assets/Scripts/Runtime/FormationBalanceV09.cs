using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Local balance candidates. The generated v0.8 catalog remains the immutable baseline.</summary>
    public static class FormationBalanceV09
    {
        public enum Variant { Baseline, A, B, C }
        public static Variant Active { get; private set; } = Variant.B;
        public static float Strength => Active == Variant.Baseline ? 0 : Active == Variant.A ? .65f : Active == Variant.C ? 1.3f : 1;
        public static float HeroStrength => Active == Variant.Baseline ? 0 : Active == Variant.A ? .8f : Active == Variant.C ? 1.25f : 1;
        static readonly Dictionary<string, FormationCampaignRules.UnitDefinition> cache = new Dictionary<string, FormationCampaignRules.UnitDefinition>();
        public static void Select(Variant variant) { Active = variant; cache.Clear(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Select(Variant.B);
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-formationBalanceVariant");
            if (index >= 0 && index + 1 < args.Length && Enum.TryParse<Variant>(args[index + 1], true, out var variant)) Select(variant);
        }
        public static float Towards(float original, float proposed) => Mathf.LerpUnclamped(original, proposed, Strength);
        public static float Hero(float original, float proposed) => Mathf.LerpUnclamped(original, proposed, HeroStrength);
        public static FormationCampaignRules.UnitDefinition Tune(FormationCampaignRules.UnitDefinition raw)
        {
            if (raw == null || Active == Variant.Baseline) return raw;
            if (cache.TryGetValue(raw.Id, out var found)) return found;
            var unit = JsonUtility.FromJson<FormationCampaignRules.UnitDefinition>(JsonUtility.ToJson(raw));
            void Set(float hp = -1, float attack = -1, float interval = -1)
            {
                if (hp > 0) unit.Health = Towards(raw.Health, hp);
                if (attack > 0) unit.Attack = Towards(raw.Attack, attack);
                if (interval > 0) unit.Interval = Towards(raw.Interval, interval);
            }
            switch (raw.Id)
            {
                case "M03": Set(480,50); break; case "M11": Set(460,66); break;
                case "M17": Set(560); break; case "M21": Set(660,52); break;
                case "M35": Set(520,52); break; case "B01": Set(450,70); break;
                case "B22": Set(600,48,1.4f); break; case "M13": Set(attack:120); break;
                case "M16": Set(660,100); break; case "M27": Set(900); break;
                case "M38": Set(700,80); break; case "B03": Set(attack:115); break;
                case "M18": Set(1200); break; case "M24": Set(1020); break;
                case "M42": Set(880); break; case "M31": Set(900); break;
                case "M34": Set(1000); break; case "B09": Set(900); break;
                case "B14": Set(1080); break; case "B23": Set(920,185); break;
                case "B04": Set(attack:290); break; case "WIS": Set(1200,500); break;
                case "B08": Set(attack:480); break; case "B12": Set(attack:550); break;
                case "B21": Set(attack:570); break;
            }
            cache.Add(raw.Id,unit); return unit;
        }
        internal static float Star(int cost,int star,bool health,float original)
        {
            if (Active == Variant.Baseline || star < 3 || cost > 3) return original;
            float target = health ? cost == 1 ? star == 4 ? 7 : 3.8f : cost == 2 ? 3.7f : 3.6f
                : cost == 1 ? star == 4 ? 6.5f : 3.6f : cost == 2 ? 3.5f : 3.35f;
            return Towards(original,target);
        }
        internal static float GuardHealth(string node)
        {
            if(Active==Variant.Baseline)return 1;
            var scales=node switch
            {
                "2-7" => (.90f,.80f,.68f), "3-1" => (.90f,.80f,.68f),
                "3-2" => (.88f,.76f,.64f), "3-4" => (.85f,.70f,.58f),
                "3-5" => (.85f,.70f,.58f), "3-7" => (.50f,.28f,.23f),
                _ => (1f,1f,1f)
            };
            return Active==Variant.A?scales.Item1:Active==Variant.C?scales.Item3:scales.Item2;
        }
        public static readonly FormationCampaignRules.DataRecord[] Heroes =
        {
            Record("H11","碎霜弩机","银","M11","技力3→2；寒冷、冻结或霜痕目标受一次60%攻击法伤。技能320%，冻结480%。攻击附加4秒伤害霜痕，不提供控制。"),
            Record("H21","狂化宿主","银","M21","生命≤60%或技能后的6秒内攻速+60%；技能替换为两段240%，随后6秒30%吸血。自回降至每秒1%，吸血每秒上限8%最大生命。"),
            Record("HB22","狂躁回响","银","B22","技能四段125%，技力14→12；每次施法叠8%攻速，最多80%，本场返场保留，并获得35%最大生命护盾6秒。第五次起追击100%，保留原倍率递增，不增加追击次数。"),
            Record("H13","破冰重锤","金","M13","技力3→2；技能450%，消耗冻结时850%，只有寒冷或霜痕时600%。三个倍率互斥。"),
            Record("H16","定点歼灭","金","M16","普攻改为单目标120%，技能五段110%。持续攻击同一目标每次+6%伤害，至多30%；换目标或停攻2秒清空。"),
            Record("H27","盾锋反攻","金","M27","自身原生护盾存在时攻速+25%；其承受敌方伤害的25%存入下一次盾击，至多自身10%最大生命，死亡清空。"),
            Record("H31","高压喷焰","金","M31","技力20→18，六段各130%法伤；目标上限5→3。保留原动作、灼燃和死亡爆破。"),
            Record("H42","攻坚装药","金","M42","初始技力8→10；技能对护盾增伤25%→60%，对最终护甲≥35的目标再乘1.18。控制和目标数不变。"),
            Record("HB23","赎罪终章","金","B23","技能八段80%，每次施放各段+6个百分点，最多8层；每第三次无第二目标时，追加主技能实际生命伤害25%的单体尾击。")
        };
        static FormationCampaignRules.DataRecord Record(string id,string name,string tier,string unit,string effect) =>
            new FormationCampaignRules.DataRecord {Id=id,Name=name,Kind="专属",Tier=tier,Planes=new[]{1,2},
                Effect=effect+" 指定本体达到三星后，攻击额外提高"+(id is "H11" or "H21" or "HB22"?"100%":id is "H13" or "H16" or "H27"?"50%":"10%")+"。每局限选一个专属，只强化上阵中最早的一名同名本体；赠一张实际占库的一星。付费刷新每5次补卡一次：一费最多1次、二三费最多2次，三星停止。",Extra=unit};
        internal static FormationCampaignRules.DataRecord ForUnit(string id) => Heroes.FirstOrDefault(hero=>hero.Extra==id);
        internal static string ChoiceText(FormationCampaignRules.DataRecord record)
        {
            string skill=record.Id switch
            {
                "H11"=>"雪怪狙击手：技力2、寒冷追伤、贯冰箭增强。",
                "H21"=>"宿主士兵：两段240%，6秒狂化/吸血，自回减半。",
                "HB22"=>"裂兽：四段125%，施法叠攻速并获短盾，迅切增强。",
                "H13"=>"凿冰人：技力2，破冰850%、寒冷600%。",
                "H16"=>"狙击手：改为单体射击，持续集火增伤。",
                "H27"=>"践行者：自身盾吸伤蓄力，下次盾击反攻。",
                "H31"=>"火焰兵：六段130%、技力18，目标上限降至3。",
                "H42"=>"掷能者：初始技力10，破盾/重甲技能增伤。",
                "HB23"=>"酒神：八段斩击，每第三次追加第二轮或尾击。",
                _=>null
            };
            if(skill==null)return record.Effect;
            var spec=FormationCampaignRules.Find(record.Extra);int promotion=spec.Cost==1?100:spec.Cost==2?50:10;
            return skill+$"三星指定本体攻击+{promotion}%。赠1张；付费5刷补卡（限{(spec.Cost==1?1:2)}次）。每局限1专属。";
        }
    }
}
