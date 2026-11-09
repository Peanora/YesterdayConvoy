using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace YesterdayConvoy
{
    public sealed class SynergyTierUi
    {public int tier;public string text;public bool active;}
    public sealed class SynergyMemberUi
    {public string id;public bool selected,transfer;}
    public sealed class SynergyDetailUiModel
    {
        public string common,source;
        public SynergyTierUi[] tiers;
        public SynergyMemberUi[] members;
        static string N(float n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
        static string P(float n)=>N(n*100)+"%";
        static float V(string id,int tier,string key)=>CampaignV2Balance.SynergyValue(id,tier,key);
        static string G(int stage,string key)=>N(CampaignV2Balance.SummonValue(stage,key));
        public static SynergyDetailUiModel Build(SynergySummary summary,bool v2,System.Collections.Generic.IEnumerable<string> roleIds)
        {
            var chosen=summary.Members.ToHashSet(StringComparer.Ordinal);
            var all=roleIds.Where(id=>CampaignBalance.Tags(id).Contains(summary.Name)).Concat(chosen).Distinct(StringComparer.Ordinal);
            var model=new SynergyDetailUiModel{source=v2?"正式行动 v2":"当前试验规则",
                common=Common(summary.Name),members=all.Select(id=>new SynergyMemberUi{id=id,selected=chosen.Contains(id),transfer=!CampaignBalance.Tags(id).Contains(summary.Name)})
                    .OrderByDescending(m=>m.selected).ThenBy(m=>m.id,StringComparer.Ordinal).ToArray()};
            model.tiers=summary.Thresholds.Select(t=>new SynergyTierUi{tier=t,active=t==summary.Tier&&t>0,
                text=v2?V2Tier(summary.Name,t):LegacyTier(summary,t)}).ToArray();
            return model;
        }
        static string Common(string name)
        {
            switch(name)
            {
                case "整合运动":return "按战前最高达成档强化整合成员最大生命与攻击，包含五费；援军和派生体不再独立叠加。成功部署不同整合成员完成集结，援军沿该红门的固定路线出现。每轮触发后清空，可复用同种卡；不要求同时存活。援军不花部署费用/次数，穿门不扣防御，不再产生羁绊资源。援军生命与攻击乘√(整合编队成员总星/当前档门槛)，四舍五入。";
                case "雪怪小队":return "冻结目标受到不同原卡的有效直接命中时触发碎冰。天气只由已完成的碎冰或霜星主动冰封开启；天气期间不积累、不叠加、不续时。碎冰与天气伤害乘编队成员平均星级的0.4次方，均过法抗。";
                case "感染生物":return "战前选择主形态；最高档可以再选另一种低档副形态。集群一次部署只花一次费用/次数，分体不扣蓝门。甲壳支撑接敌，爆裂只在原体最终死亡时触发；原生死亡爆炸预算独立。";
                case "游击队":return "普通实体首次有效直接攻击/受击产生军令，同种卡有产点上限。足点且有合格单位时自动整令，演出结束才可再触发，不限总次数。全场多段按完整预算过甲抗后分配，不递归产点；技能、复起、控制锁照旧。";
                case "泥岩小队":return "编队人数和总星级决定可培养上限。实际战斗结束获得羁绊档位＋成员总星级一半（向下取整）的经验；补给或中途退出不发。封顶不预存未来经验，历史经验保留，新挑战清零，不投入金币。巨像每场限一次，不扣蓝门，无主动技能。";
                case "牧群":return "全队非五费正常实体最终死亡后，沿原位置和路线生成短时灵体。倒下与复起后才计算有效寿命；穿门不扣防御，不复制主动、爆炸、羁绊资源，也不再次转化。";
                case "深池":return "成员普攻或原生技能有效命中施加燎烧，同名取高并刷新；独立于原生灼燃，不自带减法抗。燃烧目标被正常伤害或深池燎烧消灭后留下火种，首击击杀也有效，消耗后仍计累计上限。伤害乘编队成员平均星级的立方根。";
                case "海嗣":return "每种海嗣首次有效输出或外来承伤在脚下落种。溟痕只在通行地面四邻扩散，为尚未接敌的种类预留根格；高台不铺。无存活海嗣时休眠，后续部署恢复，战后清空。壳海不增加神经普攻。";
                case "萨卡兹":return "战前指定萨卡兹编队卡为承契者，其正常部署实体统一受益。部署自动触发，不花DP/技力；入场盾每实体仅一次，无时限，耗尽不补发，复起不重新触发。支援、巨像、转化体和分体不参与。";
                default:return "按战前编队中的不同角色种类锁定，未部署、死亡或穿门不改变档位。";
            }
        }
        static string V2Tier(string name,int t)
        {
            switch(name)
            {
                case "整合运动":
                    string stat(string p)=>$"生命{N(V("reunion",t,p+"HP"))}/攻击{N(V("reunion",t,p+"Attack"))}/甲{N(V("reunion",t,p+"Armor"))}";
                    return $"整合成员生命/攻击＋{P(V("reunion",t,"memberStatBonus"))}（含五费，援军不额外叠加）。需{N(V("reunion",t,"requiredFormations"))}种编制；每轮部署{N(V("reunion",t,"distinctKindsPerMuster"))}种，限{N(V("reunion",t,"musterLimit"))}轮，每轮{N(V("reunion",t,"supportsPerMuster"))}名。士兵{stat("melee")}"+(t>=4?$"；弩手{stat("ranged")}":"")+"。"+(t==4?"可选近/远援军。":t>=6?"每轮一近一远。":"")+(t==8?$"首次实际交战获{P(V("reunion",t,"entryShieldFraction"))}生命盾{N(V("reunion",t,"entryShieldSeconds"))}秒。":"");
                case "雪怪小队":
                    return $"{N(V("snow",t,"requiredDistinctHitKinds"))}种在{N(V("snow",t,"hitWindowSeconds"))}秒内命中：碎冰{N(V("snow",t,"shatterArts"))}法伤，半径{N(V("snow",t,"shatterRadius"))}/至多{N(V("snow",t,"shatterTargets"))}人，目标冷却{N(V("snow",t,"targetShatterCooldownSeconds"))}秒，"+(V("snow",t,"consumeFreeze")>0?"消耗冻结。":"保留冻结。")+(t==5?$"{N(V("snow",t,"weatherCharges"))}次碎冰开启{N(V("snow",t,"weatherDurationSeconds"))}秒天气，{string.Join("/",CampaignV2Balance.SynergyList("snow",t,"weatherPulseAt").Select(N))}秒寒潮：{N(V("snow",t,"weatherFlatArts"))}＋目标最大生命{P(V("snow",t,"weatherTargetMaxHPFraction"))}法伤。":"");
                case "感染生物":return $"集群{N(V("infection",t,"swarmBodies"))}只，各{P(V("infection",t,"swarmHPFraction"))}生命/{P(V("infection",t,"swarmAttackFraction"))}攻击；甲壳{P(V("infection",t,"shellShieldFraction"))}生命盾{N(V("infection",t,"shellShieldSeconds"))}秒、甲＋{N(V("infection",t,"shellArmorAdd"))}；爆裂{N(V("infection",t,"burstBaseArts"))}法伤，半径{N(V("infection",t,"burstRadius"))}/至多{N(V("infection",t,"burstTargets"))}人。"+(t==6?$"另选{N(V("infection",t,"secondaryTier"))}档副形态。":"");
                case "游击队":
                    string participants=t>=8?"全部合格普通实体":$"至多{N(V("guerrilla",t,"participantLimit"))}人";
                    return $"整令{N(V("guerrilla",t,"fullOrderCost"))}点，开战＋{N(V("guerrilla",t,"initialOrders"))}点，{participants}，{P(V("guerrilla",t,"attackMultiplier"))}攻击预算。"+(t>=4?$"参与近战获{P(V("guerrilla",t,"meleeShieldFraction"))}生命盾{N(V("guerrilla",t,"shieldSeconds"))}秒。":"")+(t==6?$"可手动两次{N(V("guerrilla",t,"halfOrderCost"))}点半令，各至多{N(V("guerrilla",t,"halfParticipantLimit"))}人，后半不复用前半参与者。":"")+(t>=8?"移动/普攻中可参与，无需射程内目标。":"普攻收手且射程内有目标才可参与。");
                case "泥岩小队":
                    int s=(int)V("mudrock",t,"maximumStage");
                    return $"总星至少{N(V("mudrock",t,"requiredTotalStars"))}，培养上限{new[]{"","I","II","III","IV","V"}[s]}，累计XP门槛{G(s,"requiredXP")}。该阶段巨像：生命{G(s,"hp")}、攻击{G(s,"attack")}、甲{G(s,"armor")}/法抗{G(s,"resistance")}、DP{G(s,"dp")}。"+(s>=4?"至多三目标并可打高台，第三击全场法伤、每轮成功普攻回血、免疫控制。":"实际第三次普攻命中触发本阶段追加伤害。");
                case "牧群":return $"前{N(V("herd",t,"conversionsPerBattle"))}个死亡转化，{P(V("herd",t,"spiritHPFraction"))}生命/{P(V("herd",t,"spiritAttackFraction"))}攻击，甲{N(V("herd",t,"spiritArmor"))}，有效寿命{N(V("herd",t,"effectiveLifetimeSeconds"))}秒。"+(t==4?$"可继承远程基础攻击，射程{N(V("herd",t,"spiritRange"))}，攻击再乘{P(V("herd",t,"rangedAttackMultiplier"))}。":"近战基础攻击。");
                case "深池":return $"燎烧{N(V("deep-pool",t,"burnArtsPerSecond"))}法伤/秒，{N(V("deep-pool",t,"burnDurationSeconds"))}秒；火种累计{N(V("deep-pool",t,"seedCumulativeLimit"))}枚，传播半径{N(V("deep-pool",t,"spreadRadius"))}/至多{N(V("deep-pool",t,"spreadTargets"))}人。"+(t==4?$"耗{N(V("deep-pool",t,"detonationSeedCost"))}火种引爆{N(V("deep-pool",t,"detonationArts"))}法伤，半径{N(V("deep-pool",t,"detonationRadius"))}/至多{N(V("deep-pool",t,"detonationTargets"))}人，本场{N(V("deep-pool",t,"detonationCasts"))}次，不返种。":"");
                case "海嗣":return $"每{N(V("marine",t,"spreadIntervalSeconds"))}秒扩散，至多{N(V("marine",t,"tileLimit"))}格；海嗣站溟痕上原生神经损伤×{N(V("marine",t,"nativeNeuralMultiplier"))}，地面守军每秒积累{N(V("marine",t,"groundNeuralPerSecond"))}点，不吃该倍率。积累{N(V("marine",t,"neuralThreshold"))}爆发{N(V("marine",t,"neuralBurstTrueDamage"))}真实伤害及{N(V("marine",t,"neuralBurstDurationSeconds"))}秒眩晕。";
                case "萨卡兹":return $"全体萨卡兹生命＋{P(V("sarkaz",t,"allSarkazHPAdd"))}；承契者攻击＋{P(V("sarkaz",t,"holderAttackAdd"))}，一次{P(V("sarkaz",t,"holderShieldMaxHPFraction"))}自身最终最大生命盾。其他萨卡兹共享{P(V("sarkaz",t,"otherShareFraction"))}攻击加成与入场盾。";
                default:throw new ArgumentException(name);
            }
        }
        static string LegacyTier(SynergySummary s,int t)
        {
            // Parse the existing legacy presentation instead of inventing a second tuning table.
            string text=s.Effect;
            string R(string pattern,int group=1){var m=Regex.Match(text,pattern);if(!m.Success)throw new FormatException();return m.Groups[group].Value;}
            try
            {
                if(s.Name=="整合运动")
                {
                    var melee=ReunionRules.ForSupport(t,false);var ranged=ReunionRules.ForSupport(t,true);
                    return $"整合成员生命/攻击＋{P(t*.05f)}（含五费，援军不额外叠加）。需{ReunionRules.RequiredFormations(t)}种编制；每轮{ReunionRules.KindsPerMuster(t)}种、限{ReunionRules.MusterLimit(t)}轮、每轮{ReunionRules.SupportsPerMuster(t)}名。士兵{N(melee.Health)}/{N(melee.Attack)}/{N(melee.Armor)}（生命/攻击/甲）。"+(t>=4?$"弩手{N(ranged.Health)}/{N(ranged.Attack)}/{N(ranged.Armor)}。":"")+(t==8?R("援军首次实际交战时[^。]+。",0):"");
                }
                if(s.Name=="游击队")
                {
                    var r=GuerrillaRules.ForTier(t);return $"整令{r.Cost}点，开战＋{r.InitialOrders}点，"+(r.ParticipantLimit==int.MaxValue?"全部合格普通实体":$"至多{r.ParticipantLimit}人")+$"，{P(r.AttackMultiplier)}攻击预算。"+(t>=4?R(t==4?"4档参与近战护盾[^；]+":"6/8/9档[^。]+")+"。":"")+(r.AllowsHalf?"可手动两次半令，后半排除前半参与者。":"")+(r.ExpandedEligibility?"移动/普攻中可参与，无需射程内目标。":"普攻收手且射程内有目标。");
                }
                if(s.Name=="雪怪小队")return R(t+"档：[^\\n]+",0)+(t==5?"\n"+R("仅5档：[^\\n]+",0)+"\n"+R("0/2/4/6秒[^\\n]+",0):"");
                if(s.Name=="感染生物")
                {
                    int i=Array.IndexOf(s.Thresholds,t);string[] parts=R("集群2/4/6：([^；]+)；").Split('、');
                    string[] shields=R("甲壳：([\\d/]+)%").Split('/'),armor=R("护甲\\+([\\d/]+)").Split('/'),blast=R("爆裂：原体死亡造成([\\d/]+)法伤").Split('/'),radius=R("半径([\\d./]+)，最多").Split('/'),targets=R("最多([\\d/]+)人").Split('/');
                    return $"集群{parts[i]}生命/攻击；甲壳{shields[i]}%生命盾、甲＋{armor[i]}；爆裂{blast[i]}法伤，半径{radius[i]}/至多{targets[i]}人。"+(t==6?"另选2档副形态。":"");
                }
                if(s.Name=="牧群")return R(t+"档前[^；\\n]+",0);
                if(s.Name=="深池")return (t==2?R("2档[\\d.]+法伤/秒",0):R("4档[\\d.]+/秒",0))+"；"+(t==2?R("2档本场[^；]+",0):R("4档[\\d]+枚[^。]+",0)+"。\n"+R("4档消耗[^\\n]+",0));
                if(s.Name=="海嗣")
                {
                    int i=t==2?0:1;var seconds=R("2/4档：每([\\d/]+)秒").Split('/');var cells=R("至多([\\d/]+)格").Split('/');var factor=R("原生神经损伤×([\\d./]+)").Split('/');
                    return $"每{seconds[i]}秒扩散，至多{cells[i]}格；站溟痕上原生神经损伤×{factor[i]}。"+R("地面守军踩溟痕[^。]+。",0)+R("神经积累[^。]+。",0);
                }
                if(s.Name=="萨卡兹")
                {
                    var hp=R("最大生命\\+([\\d/]+)%").Split('/');return "全体萨卡兹生命＋"+hp[Array.IndexOf(s.Thresholds,t)]+"%。"+R("(?m)^"+t+"档[^\\n]+",0);
                }
                if(s.Name=="泥岩小队")
                {
                    int cap=GolemGrowth.Capacity(t,t*3);var hp=R("生命([\\d/]+)，攻击").Split('/');var atk=R("，攻击([\\d/]+)，DP").Split('/');var dp=R("，DP([\\d/]+)。").Split('/');
                    return $"培养上限{new[]{"","I","II","III","IV","V"}[cap]}，累计经验{GolemGrowth.Threshold(cap)}；本阶段生命{hp[cap-1]}、攻击{atk[cap-1]}、DP{dp[cap-1]}。"+(t>=4?R(t+"种且S≥[^；。]+",0):"");
                }
            }
            catch(FormatException){return "原试验说明："+s.Effect;}
            return s.Effect;
        }
    }
}
