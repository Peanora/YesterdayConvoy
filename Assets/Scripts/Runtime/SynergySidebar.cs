using System;
using System.Collections.Generic;
using System.Globalization;
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
        static string ReunionEffect()=>
            "按战前编队不同整合卡种锁档；每种卡只属于本队、雪怪、游击、牧群中的一种编制。\n"+
            "2档：2种，无编制限制；每轮部署2种，追加1名士兵，本场1轮，共1名。\n"+
            "4档：4种，无编制限制；每轮部署3种，追加1名所选士兵或弩手，本场2轮，共2名。\n"+
            "6档：6种且至少2编制；每轮部署3种，追加士兵、弩手各1，本场2轮，共4名。\n"+
            "8档：8种且至少3编制；每轮部署3种，追加士兵、弩手各1，本场3轮，共6名。援军首次实际交战时获得自身最大生命20%护盾，持续5模拟秒。\n"+
            "集结只计正常整合卡的成功部署，同种本轮只计一次；不要求同时存活，也无时间限制。每轮触发即清空，下轮可复用同种卡；触发部署不重复计入下一轮。战斗中不额外检查编制。\n"+
            "援军随触发部署的红门与路线出现，不消耗DP或卡牌次数，穿门不扣防御。援军不参与集结、军令、牧群转化、碎冰等羁绊资源，不成为承契者；仍可正常攻击、承伤与接受基础治疗。\n"+
            "援军基础生命/攻击/护甲：2档士兵80/14/0；4档士兵120/22/2、弩手90/24/0；6档士兵160/28/4、弩手110/30/1；8档士兵200/36/6、弩手140/38/2。\n"+
            "生命、攻击乘√(S/K)，四舍五入；S为全部整合编队成员总星级，K为当前档位2/4/6/8。额外成员也贡献星级，备战、支援与重复部署不计；护甲不成长。均为物理普攻，间隔1.8秒、速度0.7；士兵射程0.75、弩手2.7。暂停冻结行动及护盾时间。";
        static string GuerrillaEffect()
        {
            var lines=new List<string>{"普通实体首次有效直接攻击/受击+1军令，每种卡最多2点；支援/分体不产点。达到本档点数且有合格出手者时自动整令，不限次数；演出结束后才能再次触发。"};
            foreach(int tier in new[]{2,4,6,8,9})
            {
                var rule=GuerrillaRules.ForTier(tier);
                string people=rule.ParticipantLimit==int.MaxValue?"全部合格普通实体":"最多"+rule.ParticipantLimit+"人";
                lines.Add($"{tier}档：整令{rule.Cost}点 / {people} / {Mathf.RoundToInt(rule.AttackMultiplier*100)}%攻击"+(rule.InitialOrders>0?$"，开战+{rule.InitialOrders}军令。":"。"));
            }
            lines.Add("0.9/1.2/1.5/1.8/2.8演出秒全场五段命中；前四段各15%，蓄力末段40%。原完整预算先过护甲/抗性再分段，不递归产点。1×/2×演出等速。");
            lines.Add("2/4/6需普攻收手且有射程内目标；8/9可在移动/普攻中参加，无需射程目标。技能、复起、控制锁照旧。");
            lines.Add("4档参与近战护盾10%生命3秒；6/8/9档15%生命5秒。");
            lines.Add("仅6档保留手动两次3点半令，后半排除前半参与者；半令未完成时自动整令等待。暂停、演出中或无合格单位/存活守军不扣费。");
            return string.Join("\n",lines);
        }
        static readonly Dictionary<string,string> v2EffectCache=new Dictionary<string,string>();
        static string V2Effect(string name)
        {
            if(v2EffectCache.TryGetValue(name,out var cached))return cached;
            float V(string id,int tier,string key)=>CampaignV2Balance.SynergyValue(id,tier,key);
            string N(float value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
            string P(float value)=>N(value*100)+"%";
            var lines=new List<string>();
            switch(name)
            {
                case "整合运动":
                    lines.Add("战前按不同整合卡种与编制锁档：2/4档不限编制；6档需至少2编制，8档需至少3编制。成功部署才累计集结，同种本轮只计一次；不要求同时存活。每轮清空后可复用，触发部署不重复计入下一轮。");
                    foreach(int t in new[]{2,4,6,8})
                    {
                        string melee=$"士兵 {N(V("reunion",t,"meleeHP"))}/{N(V("reunion",t,"meleeAttack"))}/{N(V("reunion",t,"meleeArmor"))}";
                        string ranged=t>=4?$"；弩手 {N(V("reunion",t,"rangedHP"))}/{N(V("reunion",t,"rangedAttack"))}/{N(V("reunion",t,"rangedArmor"))}":"";
                        lines.Add($"{t}档：每轮{N(V("reunion",t,"distinctKindsPerMuster"))}种，{N(V("reunion",t,"musterLimit"))}轮，每轮{N(V("reunion",t,"supportsPerMuster"))}名。基础生命/攻击/护甲：{melee}{ranged}。");
                    }
                    lines.Add("4档可选士兵或弩手，6/8档一近一远；8档援军首次实际交战获得20%最大生命护盾，持续5模拟秒。生命、攻击乘√(S/K)后四舍五入，护甲不成长；间隔1.8秒、速度0.7，近/远射程0.75/2.7。");
                    lines.Add("援军沿触发红门固定路线出现，不花DP/次数，穿门不扣防御；不产羁绊资源、不成为承契者。S计全部整合编队成员总星，额外成员也贡献；备战、重复部署不计，暂停冻结行动及护盾。");break;
                case "雪怪小队":
                    lines.Add("冻结守军在2.5秒内受两种不同原卡直接命中才碎冰。碎冰及天气伤害乘(S/K)^0.4，S为全部雪怪编队总星，K为当前2/5档。");
                    foreach(int t in new[]{2,5})lines.Add($"{t}档：{N(V("snow",t,"shatterArts"))}法伤，半径{N(V("snow",t,"shatterRadius"))}/至多{N(V("snow",t,"shatterTargets"))}人，目标冷却{N(V("snow",t,"targetShatterCooldownSeconds"))}秒；"+(t==2?"消耗冻结。":"保留冻结。"));
                    lines.Add($"仅5档：{N(V("snow",5,"weatherCharges"))}次碎冰或霜星手动冰封开启{N(V("snow",5,"weatherDurationSeconds"))}秒暴风雪，清零寒意；天气期间不积累、不叠加、不续时。");
                    lines.Add($"0/2/4/6秒全场寒潮，每次({N(V("snow",5,"weatherFlatArts"))}+目标最大生命{P(V("snow",5,"weatherTargetMaxHPFraction"))})法伤、过法抗。寒冷攻速70%，再次受寒冻结1.2秒；天气冻结结束另保护3秒，只防控制，不免伤。");break;
                case "感染生物":
                    lines.Add("战前选择主形态；6档可再选另一种2档副形态。一次部署只扣一次DP/次数；额外分体不扣蓝门，不重复产羁绊资源。");
                    foreach(int t in new[]{2,4,6})lines.Add($"{t}档集群：{N(V("infection",t,"swarmBodies"))}只，各{P(V("infection",t,"swarmHPFraction"))}生命/{P(V("infection",t,"swarmAttackFraction"))}攻击；甲壳：{P(V("infection",t,"shellShieldFraction"))}生命护盾{N(V("infection",t,"shellShieldSeconds"))}秒、护甲+{N(V("infection",t,"shellArmorAdd"))}；爆裂：{N(V("infection",t,"burstBaseArts"))}法伤，半径{N(V("infection",t,"burstRadius"))}/至多{N(V("infection",t,"burstTargets"))}人。");
                    lines.Add("爆裂仅原体最终死亡后0.2秒触发一次，每升一星+35%；原生死亡爆炸独立保留自身预算，不重复寒冷。");break;
                case "游击队":
                    lines.Add("普通实体首次有效直接攻击/受击+1军令，每种卡最多2点；支援/分体不产点。足点且有合格单位时自动整令，不限次数；演出结束后再触发。");
                    foreach(int t in new[]{2,4,6,8,9})
                    {
                        string people=t>=8?"全部合格普通实体":$"最多{N(V("guerrilla",t,"participantLimit"))}人";
                        lines.Add($"{t}档：整令{N(V("guerrilla",t,"fullOrderCost"))}点，{people}，{P(V("guerrilla",t,"attackMultiplier"))}攻击；开战+{N(V("guerrilla",t,"initialOrders"))}点。"+(t>=4?$"参与近战获{P(V("guerrilla",t,"meleeShieldFraction"))}生命护盾{N(V("guerrilla",t,"shieldSeconds"))}秒。":""));
                    }
                    lines.Add("0.9/1.2/1.5/1.8/2.8演出秒全场五段，前四段各15%、末段40%；完整预算先过甲/抗再分段，不递归产点，1×/2×演出等速。2/4/6需普攻收手且有射程内目标；8/9可移动/普攻中参加，无需射程目标，技能/复起/控制锁照旧。");
                    lines.Add("仅6档可手动两次3点、各至多6人的半令，后半排除前半参与者；未完成半令时自动整令等待。暂停、演出或无合格单位/活守军时不扣费。正式徽章获取尚未开放。");break;
                case "泥岩小队":
                    lines.Add("按战前不同种类与总星级S确定上限：2种至II；4种且S≥7至III；5种且S≥10至IV；6种且S≥12至V。实际战斗结束获得档位+⌊S/2⌋经验；补给/中途退出不发，达到当前上限不预存未来经验，历史经验保留，新挑战清零。不投入金币。");
                    foreach(int s in Enumerable.Range(1,5))lines.Add($"巨像{s}：累计XP{N(CampaignV2Balance.SummonValue(s,"requiredXP"))}，生命{N(CampaignV2Balance.SummonValue(s,"hp"))}，攻击{N(CampaignV2Balance.SummonValue(s,"attack"))}，护甲{N(CampaignV2Balance.SummonValue(s,"armor"))}/法抗{N(CampaignV2Balance.SummonValue(s,"resistance"))}，DP{N(CampaignV2Balance.SummonValue(s,"dp"))}。");
                    lines.Add("每场只部署一次，不扣蓝门，无主动技能，普攻间隔5.2秒。II每第三击主目标额外80物伤；III主目标120、周围至多2人各100。IV/V至多3目标、可打高台，每第三击全场80%攻击法伤，每轮成功普攻回8%生命一次，免疫控制。正式转职徽章获取未定。");break;
                case "牧群":
                    lines.Add("全队非五费正常实体最终死亡后，沿原位置/路线生成转化体；穿门0，不复制主动、爆炸、羁绊资源，不再转化。");
                    foreach(int t in new[]{2,4})lines.Add($"{t}档：前{N(V("herd",t,"conversionsPerBattle"))}个，{P(V("herd",t,"spiritHPFraction"))}生命/{P(V("herd",t,"spiritAttackFraction"))}攻击，{N(V("herd",t,"spiritArmor"))}甲，有效存活{N(V("herd",t,"effectiveLifetimeSeconds"))}秒。"+(t==4?"可继承远程基础攻击，2.5射程、攻击再乘85%。":"近战射程0.75。"));
                    lines.Add("倒下0.7秒、复起0.55秒后开始计算有效存活时间；间隔至少2秒，保留更长完整动作，暂停冻结。");break;
                case "深池":
                    lines.Add("成员普攻/原生技能有效命中施加燎烧，同名取高刷新；独立于原生灼燃，不自带减法抗。燎烧或原生灼燃目标被正常伤害/深池燎烧消灭后留火种，首击击杀也有效；伤害乘(S/K)立方根。");
                    foreach(int t in new[]{2,4})lines.Add($"{t}档：{N(V("deep-pool",t,"burnArtsPerSecond"))}法伤/秒，持续{N(V("deep-pool",t,"burnDurationSeconds"))}秒；本场火种累计至多{N(V("deep-pool",t,"seedCumulativeLimit"))}枚，传播半径{N(V("deep-pool",t,"spreadRadius"))}/至多{N(V("deep-pool",t,"spreadTargets"))}人，已消耗仍计上限。");
                    lines.Add($"4档：每次消耗1枚火种，优先有活目标的火种；{N(V("deep-pool",4,"detonationArts"))}法伤，半径{N(V("deep-pool",4,"detonationRadius"))}/至多{N(V("deep-pool",4,"detonationTargets"))}人，每场{N(V("deep-pool",4,"detonationCasts"))}次，不返火种。S计全部深池编队成员总星。");break;
                case "海嗣":
                    lines.Add("每种海嗣首次有效输出或外来承伤在脚下落种一次，重复部署/无敌/自损/支援不重复。溟痕只铺通行地面；每次向相邻地面扩散一圈，为尚未接敌的种类预留根格，高台不铺。");
                    foreach(int t in new[]{2,4})lines.Add($"{t}档：每{N(V("marine",t,"spreadIntervalSeconds"))}秒扩散，至多{N(V("marine",t,"tileLimit"))}格；海嗣站溟痕上，原生神经损伤×{N(V("marine",t,"nativeNeuralMultiplier"))}。地面守军每秒积累{N(V("marine",t,"groundNeuralPerSecond"))}点，该地面积累不乘倍率。");
                    lines.Add("神经积累100爆发100真实伤害、10秒眩晕，其间不积累。壳海只负责前锋落种，无新增神经普攻。无存活海嗣时休眠，后续部署恢复，战后清空。");break;
                case "萨卡兹":
                    lines.Add("战前指定一张萨卡兹编队卡为承契者，所有正常部署实体统一受益，开战锁定。部署自动触发，不花DP/技力，无手动释放与低血/阵亡充能。");
                    foreach(int t in new[]{2,4,6,8,9})lines.Add($"{t}档：全体萨卡兹最大生命+{P(V("sarkaz",t,"allSarkazHPAdd"))}；承契者攻击+{P(V("sarkaz",t,"holderAttackAdd"))}、一次{P(V("sarkaz",t,"holderShieldMaxHPFraction"))}自身最终最大生命护盾；其他萨卡兹共享{P(V("sarkaz",t,"otherShareFraction"))}攻击加成与入场盾。");
                    lines.Add("护盾同名取高、不叠加，无时限，耗尽不补发，复起不重新触发；支援/巨像/转化体/分体不参与。各档持续本场，9档为众魂归乡。这里只指定承契者，不提供未定的转职徽章。");break;
                default:throw new ArgumentException("Unknown v2 synergy: "+name);
            }
            return v2EffectCache[name]="正式战役 v2\n"+string.Join("\n",lines);
        }
        public static SynergySummary[] From(IEnumerable<string> lineup,IEnumerable<string> extraMembers=null,IEnumerable<string> extraMudrockMembers=null,bool campaignV2=false)
        {
            var ids=lineup.Distinct().ToArray();
            var transfers=new HashSet<string>((extraMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.Guerrilla(id)));
            var mudrockTransfers=new HashSet<string>((extraMudrockMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.MudrockCrew(id)));
            string[] names={"整合运动","雪怪小队","感染生物","游击队","泥岩小队","牧群","深池","海嗣","萨卡兹"};
            int[][] levels={ReunionRules.Thresholds,new[]{2,5},new[]{2,4,6},new[]{2,4,6,8,9},new[]{2,4,5,6},new[]{2,4},new[]{2,4},new[]{2,4},new[]{2,4,6,8,9}};
            string[] effects={
                ReunionEffect(),
                "冻结守军在2.5秒内受到两种不同原卡直接命中时碎冰。\n2档：50法伤，半径0.9/最多2人；消耗冻结，目标冷却4秒。\n5档：120法伤，半径1.5/最多5人；保留冻结，目标冷却3秒。\n仅5档：3次碎冰或霜星手动冰封发动8秒暴风雪，开启清零寒意；每次碎冰只计1点，天气期间不积累、不叠加、不续时。\n0/2/4/6秒全场寒潮，每次(60+目标最大生命2%)法伤、过法抗；碎冰/寒潮乘(S/K)^0.4；S为全部雪怪编队成员总星级，K为当前档2或5，额外成员也贡献。\n寒冷：攻速70%；再次受寒冷冻结1.2秒。天气冻结结束后保护3秒，只防控制、不免寒潮伤害。",
                "战前选主形态，6档再选另一种2档副形态。\n集群2/4/6：2只×55%、2只×70%、3只×55%生命/攻击；一次扣费，额外分体不扣蓝门。\n甲壳：18/30/35%生命护盾6秒，护甲+0/2/3。\n爆裂：原体死亡造成35/65/85法伤，半径1/1.2/1.3，最多2/3/3人；每升一星+35%。原有死亡爆炸独立保留并按自身范围/伤害类型结算；额外爆裂只算一次，不重复寒冷。",
                GuerrillaEffect(),
                "2/4/5/6档：按战前编队的不同种类与总星级S确定培养上限。\n2种可养至II；4种且S≥7至III；5种且S≥10至IV；6种且S≥12至V。同名只计实际出战卡，备战不计；持徽章转职的成员也计入。\nI/II/III/IV/V累计经验门槛0/6/16/30/48。每场实际战斗结束自动获得：档位＋⌊S/2⌋经验，胜负都结算；补给与中途退出不发。达到当前上限停止获得，不预存未来经验，历史经验不扣。\n生命360/550/1800/6000/7500，攻击60/84/240/1600/2000，DP4/5/8/14/14。每场一次、不扣门血；IV/V至多3目标、每第三击全场80%攻击法伤、每次命中回8%生命一次、免疫控制。",
                "牧群：全队非五费正常实体最终死亡后生成短时转化体。2档前3个、35%生命/攻击、4甲、10秒；4档前5个、45%生命/攻击、6甲、12秒，可保留远程基础攻击（2.5射程、攻击再乘85%）。\n转化沿原位置/路线，穿门0，不复制主动、爆炸、羁绊资源或再次转化。原动作较长时攻击间隔保留完整动作。",
                "深池：成员普攻/原生技能有效命中即燎烧，持续4秒，同名取高刷新。2档15法伤/秒，4档25/秒；独立于原生灼燃，不自带减法抗。\n燎烧或原生灼燃目标被正常伤害/深池燎烧消灭后留火种，首击击杀也有效。2档本场累计最多4枚、传播半径1.8/最多2人；4档6枚、2.2/3人，已消耗仍计上限。\n4档消耗1火种引爆，优先有活目标的火种，半径1.8/最多4人、180法伤，每场2次。引爆不返火种；伤害乘(S/K)的立方根；S为全部深池编队成员总星级，K为当前档2或4，额外成员也贡献。",
                "海嗣 · 溟痕蔓延：每种海嗣本场首次有效输出或外来承伤，在脚下落种一次；重复部署、无敌、自损和支援不重复落种。\n2/4档：每6/4秒向相邻通行地面扩散一圈，全场覆盖至多8/16格；为尚未接敌的每种海嗣预留1格，高台不铺。\n海嗣站在溟痕上，原生神经损伤×1.5/2；地面守军踩溟痕每秒积累3点神经损伤，该地面积累不再乘倍率。壳海负责前锋落种，不新增神经普攻。\n神经积累100时爆发：100真实伤害、10秒眩晕，其间不再积累。无存活海嗣时溟痕休眠，后续部署恢复；战后清空。\n战斗中点击「查看溟痕范围」突出边界；守军神经条和海嗣×倍率标识显示真实状态。",
                "众魂之契：战前把侧栏旁的萨卡兹徽章拖到萨卡兹编队卡，指定该卡为承契者；该卡所有正常部署实体统一受益，开战后锁定。羁绊外角色不能成为承契者。\n各档部署时自动触发，持续本场，无需手动释放、低血或阵亡充能，不消耗DP/技力。\n2/4/6/8/9档，全体萨卡兹最大生命+10/20/30/40/60%。\n2档：承契者攻击+35%、一次20%生命护盾。\n4档：攻击+60%、一次30%护盾。\n6档：攻击+90%、一次40%护盾；其他萨卡兹获一半攻击加成与入场盾。\n8档：攻击+120%、一次50%护盾；其他萨卡兹共享完整效果。\n9档「众魂归乡」：全体萨卡兹攻击永久+150%，部署时获得一次自身最大生命60%护盾。\n护盾按各自最终最大生命计算，同名取高、不累计；没有时限，但消耗后不定时补发。复起不重新触发；巨像、援军、转化体与分体不参与。"};
            return names.Select((name,i)=>new SynergySummary{Name=name,Members=ids.Where(id=>CampaignBalance.Tags(id).Contains(name)||name=="游击队"&&transfers.Contains(id)||name=="泥岩小队"&&mudrockTransfers.Contains(id)).ToArray(),Thresholds=levels[i],Effect=campaignV2?V2Effect(name):effects[i]})
                .Select(s=>{s.Count=s.Members.Length;s.Tier=s.Name=="整合运动"?ReunionRules.TierFor(s.Members):s.Name=="游击队"?GuerrillaRules.TierForCount(s.Count):s.Thresholds.Where(n=>s.Count>=n).DefaultIfEmpty(0).Max();return s;})
                .Where(s=>s.Count>0).OrderByDescending(s=>s.Tier>0).ThenByDescending(s=>s.Count).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        string inspectedSynergy;
        Vector2 guerrillaDetailScroll;
        Vector2 sarkazDetailScroll;
        Vector2 snowDetailScroll,deepDetailScroll;
        Vector2 reunionDetailScroll;
        ManualBattle reunionNoticeBattle;
        int reunionNoticeRun,reunionObservedMusters;
        double reunionArrivalUntil=double.NegativeInfinity;
        const float SynergyRowPitch=48;
        const float SynergyHeaderHeight=26;
        Rect SynergyBoundsFor(int count,bool preparation)
        {
            int rows=Mathf.Min(count,preparation?9:4);
            // Nine complete preparation rows still leave the construction button above y552.
            float top=preparation&&rows==9?60:65;
            return new Rect(12,top,174,SynergyHeaderHeight+rows*SynergyRowPitch);
        }
        Rect SynergyViewportFor(Rect bounds)=>new Rect(bounds.x,bounds.y+SynergyHeaderHeight,bounds.width,bounds.height-SynergyHeaderHeight);
        float SynergyWidthFor(int count,Rect viewport)=>count*SynergyRowPitch>viewport.height?156:174;
        Rect SynergyRowAtWidth(int index,float width)=>new Rect(0,index*SynergyRowPitch,width,SynergyRowPitch);
        Rect SynergyListRect=>SynergyBoundsFor(CurrentSynergies.Length,Loop!=null&&Loop.CanShop);
        Rect SynergyRowsViewportRect=>SynergyViewportFor(SynergyListRect);
        float SynergyContentWidth=>SynergyWidthFor(CurrentSynergies.Length,SynergyRowsViewportRect);
        Rect SynergyRowContentRect(int index)=>SynergyRowAtWidth(index,SynergyContentWidth);
        Rect SynergyRowScreenRect(int index)
        {
            var row=SynergyRowContentRect(index);var viewport=SynergyRowsViewportRect;
            return new Rect(viewport.x+row.x-campaignSynergyScroll.x,viewport.y+row.y-campaignSynergyScroll.y,row.width,row.height);
        }
        Rect SynergyBadgeRect(Rect row)=>new Rect(row.x+2,row.y+1,44,44);
        Rect SynergyGlyphRect(Rect row)=>new Rect(row.x+10,row.y+9,28,28);
        Rect SynergyCountRect(Rect row)=>new Rect(row.x+31.44f,row.y+30.44f,16.8f,16.8f);
        Rect SynergyNameRect(Rect row)=>new Rect(row.x+58,row.y,row.width-64,20);
        Rect SynergyThresholdRect(Rect row)=>new Rect(row.x+56,row.y+20,row.width-62,15);
        Rect SynergySupplementRect(Rect row)=>new Rect(row.x+58,row.y+35,row.width-64,12);
        Rect SynergyProgressRect(Rect row)=>new Rect(row.x+58,row.y+47,row.width-64,1);
        Rect SynergyDetailRect=>inspectedSynergy=="感染生物"?InfectionFormPanelRect:new Rect(252,90,620,415);
        SynergySummary[] CurrentSynergies=>SynergySummary.From(shopLineup,PreparedGuerrillaTransferIds,PreparedMudrockTransferIds,Battle?.CampaignV2Enabled==true);
        bool SynergyBlocksMap(Vector2 point)=>CampaignActive&&(SynergyListRect.Contains(point)||inspectedSynergy!=null);
        void DrawSynergySidebar()
        {
            if(!CampaignActive)return;
            DrawSnowstormFeedback();DrawMarineFieldFeedback();DrawReunionArrivalFeedback();
            var summaries=CurrentSynergies;var bounds=SynergyBoundsFor(summaries.Length,Loop.CanShop);
            Fill(new Rect(0,bounds.y,bounds.xMax,bounds.height),new Color(.065f,.11f,.165f,.95f));
            GUI.Label(new Rect(bounds.x+1,bounds.y,55,22),"羁绊",CampaignText(12,color:new Color(.92f,.95f,.98f),weight:600));
            GUI.Label(new Rect(bounds.x+65,bounds.y,bounds.width-70,22),summaries.Count(s=>s.Tier>0)+"条已激活",CampaignText(10,TextAnchor.MiddleRight,new Color(.61f,.70f,.80f)));
            Fill(new Rect(bounds.x,bounds.y+22,bounds.width,1),new Color(.54f,.65f,.77f,.17f));
            if(summaries.Length==0){inspectedSynergy=null;return;}
            Rect viewport=SynergyViewportFor(bounds);float contentWidth=SynergyWidthFor(summaries.Length,viewport);
            campaignSynergyScroll=GUI.BeginScrollView(viewport,campaignSynergyScroll,new Rect(0,0,contentWidth,Mathf.Max(1,summaries.Length*SynergyRowPitch)),false,false);
            try
            {
                for(int i=0;i<summaries.Length;i++)
                {
                    var s=summaries[i];Rect row=SynergyRowAtWidth(i,contentWidth);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))
                    {
                        if(s.Name=="泥岩小队")OpenGolemTrainingPanel();
                        else {inspectedSynergy=inspectedSynergy==s.Name?null:s.Name;campaignConstructOpen=false;CancelShopDrag();}
                    }
                    bool active=s.Tier>0,snowstorm=s.Name=="雪怪小队"&&SnowstormHudActive;
                    Color ink=active?new Color(.95f,.965f,.98f):new Color(.65f,.70f,.77f);
                    SynergyBadgePresentation.DrawBadge(SynergyBadgeRect(row),s.Name,s.Tier,s.Count,scene.InterfaceFont);
                    GUI.Label(SynergyNameRect(row),s.Name+(s.Name=="泥岩小队"||s.Name=="感染生物"&&InfectionChoiceEditable?" ›":""),CampaignText(14,color:ink,weight:600));
                    SynergyBadgePresentation.DrawThresholds(SynergyThresholdRect(row),s.Thresholds,s.Tier,scene.InterfaceFont);
                    if(s.Name=="整合运动")DrawReunionRow(row,s);
                    else if(s.Name=="感染生物")DrawInfectionFormRow(row,s.Tier);
                    else if(s.Name=="泥岩小队")DrawGolemSidebarProgress(row);
                    else if(s.Name=="海嗣"&&s.Tier>0)DrawMarineRow(row);
                    else if(snowstorm)DrawSnowstormRow(row);
                    else if(s.Name=="雪怪小队"&&SnowstormChargeHudVisible)DrawSnowstormChargeRow(row);
                }
            }
            finally {GUI.EndScrollView();}
            var detail=summaries.FirstOrDefault(s=>s.Name==inspectedSynergy);if(detail==null){inspectedSynergy=null;return;}
            if(detail.Name=="感染生物"){DrawInfectionFormPanel(detail);return;}
            if(detail.Name=="整合运动"){DrawReunionDetail(detail);return;}
            Fill(SynergyDetailRect,ApprovedUi.Paper);ApprovedUi.Border(SynergyDetailRect,ApprovedUi.Rule);
            GUI.Label(new Rect(268,102,510,28),$"{detail.Name} · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":detail.Implemented?"未达门槛":"待实现"),CampaignText(17,weight:700));
            if(LoopButton(new Rect(803,102,53,28),"关闭")){inspectedSynergy=null;return;}
            if(detail.Tier>0&&(detail.Name=="雪怪小队"||detail.Name=="深池"))
            {
                int stars=detail.Name=="深池"?Battle.DeepTotalStars:Battle.SnowTotalStars;
                float multiplier=detail.Name=="深池"?Battle.DeepStarMultiplier:Battle.SnowStarMultiplier;
                GUI.Label(new Rect(268,131,588,17),$"编队总星 {stars} · 伤害×{multiplier:0.###} · 开战锁定",CampaignText(11,color:ApprovedUi.Muted));
            }
            var body=CampaignText(14,TextAnchor.UpperLeft);body.wordWrap=true;
            if(detail.Name=="游击队"||detail.Name=="萨卡兹"||detail.Name=="雪怪小队"||detail.Name=="深池")
            {
                var detailViewport=new Rect(268,149,588,269);float content=body.CalcHeight(new GUIContent(detail.Effect),detailViewport.width-18)+8;
                var scroll=detail.Name=="萨卡兹"?sarkazDetailScroll:detail.Name=="雪怪小队"?snowDetailScroll:detail.Name=="深池"?deepDetailScroll:guerrillaDetailScroll;
                scroll=GUI.BeginScrollView(detailViewport,scroll,new Rect(0,0,detailViewport.width-18,Mathf.Max(detailViewport.height,content)),false,false);
                if(detail.Name=="萨卡兹")sarkazDetailScroll=scroll;else if(detail.Name=="雪怪小队")snowDetailScroll=scroll;else if(detail.Name=="深池")deepDetailScroll=scroll;else guerrillaDetailScroll=scroll;
                try{GUI.Label(new Rect(0,0,detailViewport.width-18,content),detail.Effect,body);}
                finally{GUI.EndScrollView();}
            }
            else GUI.Label(new Rect(268,149,588,269),detail.Effect,body);
            string members=string.Join("、",detail.Members.Select(id=>shop.Definition(id).Name+((detail.Name=="游击队"&&IsGuerrillaTransfer(id)||detail.Name=="泥岩小队"&&IsMudrockTransfer(id))?"（转职）":"")));
            Fill(new Rect(268,430,588,1),ApprovedUi.Rule);
            var memberStyle=CampaignText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);memberStyle.wordWrap=true;
            GUI.Label(new Rect(268,441,588,54),"编队成员："+members+"\n按战前编队锁定；未部署或死亡不改变档位。",memberStyle);
        }

        static string ReunionCompositionProgress(SynergySummary summary)
        {
            int next=summary.Thresholds.FirstOrDefault(tier=>tier>summary.Tier);
            if(next==0)next=8;
            int formations=ReunionRules.FormationCount(summary.Members),required=ReunionRules.RequiredFormations(next);
            return $"人数{summary.Count}/{next} · 编制{formations}"+(next>=6?$"/{required}":"");
        }

        void DrawReunionRow(Rect row,SynergySummary summary)
        {
            GUI.Label(SynergySupplementRect(row),ReunionCompositionProgress(summary),CampaignText(9,color:new Color(.77f,.88f,.82f)));
        }

        float DrawReunionBattleActions(float x,float y)
        {
            if(Battle.ReunionTier==0)return y;
            bool complete=Battle.MusterUsed>=Battle.ReunionMusterLimit;
            Rect panel=new Rect(x,y,157,65);Fill(panel,ApprovedUi.Paper);
            if(GUI.Button(panel,GUIContent.none,GUIStyle.none)){inspectedSynergy="整合运动";campaignConstructOpen=false;CancelShopDrag();}
            string label=complete?$"援军已齐 · {Battle.MusterUsed}/{Battle.ReunionMusterLimit}轮":$"集结 {Battle.MusterMemberIds.Count}/{Battle.ReunionMusterRequirement} · {Battle.MusterUsed}/{Battle.ReunionMusterLimit}轮";
            GUI.Label(new Rect(x+7,y+1,143,19),label,CampaignText(11,weight:600));
            DrawReunionMusterPortraits(new Vector2(x+7,y+24),complete?Array.Empty<string>():Battle.MusterMemberIds,complete?0:Battle.ReunionMusterRequirement,32,8);
            if(complete)GUI.Label(new Rect(x+7,y+25,143,28),"点击查看编制与援军",CampaignText(10,color:ApprovedUi.Muted));
            Fill(new Rect(x+7,y+60,143,2),ApprovedUi.Rule);
            Fill(new Rect(x+7,y+60,143*(complete?1:Mathf.Clamp01((float)Battle.MusterMemberIds.Count/Battle.ReunionMusterRequirement)),2),ApprovedUi.Gold);
            return y+69;
        }

        void DrawReunionMusterPortraits(Vector2 origin,IReadOnlyList<string> ids,int slots,float size,float gap)
        {
            for(int i=0;i<slots;i++)
            {
                Rect icon=new Rect(origin.x+i*(size+gap),origin.y,size,size);
                Fill(icon,i<ids.Count?new Color(.72f,.86f,.76f):new Color(.86f,.88f,.87f));
                ApprovedUi.Border(icon,i<ids.Count?new Color(.27f,.54f,.36f):ApprovedUi.Rule);
                if(i<ids.Count&&shopDefinitions.TryGetValue(ids[i],out var definition))
                {
                    DrawTestPortrait(new Rect(icon.x+2,icon.y+2,size-4,size-4),definition.Card.Portrait);
                    GUI.Label(icon,new GUIContent("",shop.Definition(ids[i]).Name),GUIStyle.none);
                }
                else GUI.Label(icon,"待集结",CampaignText(9,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            }
        }

        bool ReunionBattleHudVisible=>CampaignActive&&Loop.CanDeploy&&Battle!=null&&Battle.Phase==BattlePhase.Running&&
            !loopMenuOpen&&!loopTestSettlementOpen&&!shopVisible&&!ShopPanelTransitioning&&!CampaignUiModalOpen;

        void DrawReunionArrivalFeedback()
        {
            if(Battle==null)return;
            if(!ReferenceEquals(reunionNoticeBattle,Battle)||reunionNoticeRun!=Battle.RunId)
            {
                reunionNoticeBattle=Battle;reunionNoticeRun=Battle.RunId;reunionObservedMusters=0;
                reunionArrivalUntil=double.NegativeInfinity;
            }
            if(Battle.MusterUsed<reunionObservedMusters||Battle.Phase!=BattlePhase.Running)reunionArrivalUntil=double.NegativeInfinity;
            if(Battle.MusterUsed>reunionObservedMusters&&Battle.Phase==BattlePhase.Running)reunionArrivalUntil=Battle.Clock+2;
            reunionObservedMusters=Battle.MusterUsed;
            if(!ReunionBattleHudVisible||Battle.ReunionTier==0||Battle.Clock>=reunionArrivalUntil)return;
            Rect notice=new Rect(440,184,432,43);Fill(notice,ApprovedUi.Paper);ApprovedUi.Border(notice,ApprovedUi.Rule);
            Fill(new Rect(notice.x,notice.y,3,notice.height),ApprovedUi.Gold);
            GUI.Label(new Rect(notice.x+14,notice.y+3,404,22),$"整合运动 · 援军抵达 {Battle.MusterUsed}/{Battle.ReunionMusterLimit}轮",CampaignText(15,weight:600));
            GUI.Label(new Rect(notice.x+14,notice.y+25,404,15),"沿刚才部署的红门进军 · 不额外消耗DP或次数",CampaignText(10,color:ApprovedUi.Muted));
        }

        void DrawReunionDetail(SynergySummary detail)
        {
            Fill(SynergyDetailRect,ApprovedUi.Paper);ApprovedUi.Border(SynergyDetailRect,ApprovedUi.Rule);
            GUI.Label(new Rect(268,102,510,28),$"整合运动 · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":"未达2种"),CampaignText(17,weight:700));
            if(LoopButton(new Rect(803,102,53,28),"关闭")){inspectedSynergy=null;return;}
            GUI.Label(new Rect(268,135,588,22),ReunionCompositionProgress(detail)+"  ·  2/4不限编制，6需2编制，8需3编制",CampaignText(12,color:ApprovedUi.Muted));
            Rect viewport=new Rect(268,164,588,326);float width=viewport.width-18;
            var body=CampaignText(12,TextAnchor.UpperLeft);body.wordWrap=true;
            float rulesHeight=body.CalcHeight(new GUIContent(detail.Effect),width)+10;
            reunionDetailScroll=GUI.BeginScrollView(viewport,reunionDetailScroll,new Rect(0,0,width,344+rulesHeight),false,false);
            try
            {
                if(Battle.ReunionTier>0)
                {
                    bool complete=Battle.MusterUsed>=Battle.ReunionMusterLimit;
                    GUI.Label(new Rect(0,0,width,21),$"本轮集结 {Battle.MusterMemberIds.Count}/{Battle.ReunionMusterRequirement} · 已触发 {Battle.MusterUsed}/{Battle.ReunionMusterLimit}轮"+(complete?" · 本场已用尽":""),CampaignText(13,weight:600));
                    DrawReunionMusterPortraits(new Vector2(0,25),Battle.MusterMemberIds,complete?0:Battle.ReunionMusterRequirement,30,9);
                    GUI.Label(new Rect(130,25,width-130,32),$"整合总星 {Battle.ReunionTotalStars} · 援军生命/攻击×{Battle.ReunionStarMultiplier:0.##}",CampaignText(11,color:ApprovedUi.Muted));
                }
                else GUI.Label(new Rect(0,0,width,45),"先把两种不同整合卡编入队伍。成功部署才累计本轮集结。",body);
                bool enabled=GUI.enabled;GUI.enabled=enabled&&Loop.CanShop&&Battle.ReunionTier==4;
                if(LoopButton(new Rect(0,63,190,27),reunionRanged?"四档援军：弩手":"四档援军：士兵")){reunionRanged=!reunionRanged;SyncShopDeck(false);}
                GUI.enabled=enabled;
                GUI.Label(new Rect(202,66,width-202,21),Loop.CanShop?"仅影响四档，可在开战前更换":"开战时锁定；六/八档一近一远",CampaignText(10,color:ApprovedUi.Muted));
                for(int formation=1;formation<=4;formation++)
                {
                    float top=102+(formation-1)*54;var members=detail.Members.Where(id=>ReunionRules.FormationOf(id)==formation).ToArray();
                    GUI.Label(new Rect(0,top,78,20),ReunionRules.FormationName(formation),CampaignText(12,weight:600));
                    GUI.Label(new Rect(0,top+21,78,17),members.Length+"种入编",CampaignText(10,color:ApprovedUi.Muted));
                    if(members.Length==0)GUI.Label(new Rect(84,top+4,width-84,31),"尚未编入",CampaignText(11,color:ApprovedUi.Muted));
                    for(int i=0;i<members.Length;i++)
                    {
                        float left=84+i*64;string id=members[i];
                        if(shopDefinitions.TryGetValue(id,out var definition))DrawTestPortrait(new Rect(left+9,top,30,30),definition.Card.Portrait);
                        GUI.Label(new Rect(left-4,top+32,62,17),shop.Definition(id).Name,CampaignText(9,TextAnchor.MiddleCenter));
                    }
                }
                Fill(new Rect(0,323,width,1),ApprovedUi.Rule);
                GUI.Label(new Rect(0,332,width,rulesHeight),detail.Effect,body);
            }
            finally{GUI.EndScrollView();}
        }
    }
}
