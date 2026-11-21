using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>v0.7 combat. All events use the battle clock; the campaign owns economic state.</summary>
    public sealed partial class FormationBattleModel
    {
        const float AnastasioSkillRange = 1.1f;

        public FormationCampaignRun Campaign { get; private set; }
        public int BattleGold { get; private set; }
        public int InitialDefenderCount { get; private set; }
        public bool BossAlive => Defenders.Any(d => d.Alive && State(d).Boss);
        public float EncounterProgress => initialGuardHealth <= 0 ? 0 : Mathf.Clamp01(1 - Defenders.Where(d => State(d).InitialGuard).Sum(d =>
            State(d).LowestHealth + (d.BossHealthPhase == 1 ? d.MaxHealth : 0)) / initialGuardHealth);
        internal bool IsInitialGuard(FormationDefender guard) => State(guard).InitialGuard;
        public readonly List<FormationFighter> Summons = new List<FormationFighter>();
        public IReadOnlyDictionary<string,int> SynergyCounts => synergyCounts;
        public IReadOnlyDictionary<string,int> ActiveSynergies => synergyLevels;
        public readonly List<string> BattleEvents = new List<string>();
        public sealed class CastEvent {public double At;public int Instance;public string UnitId;}
        public readonly List<CastEvent> SkillCasts = new List<CastEvent>();
        readonly Dictionary<int,CombatState> combat = new Dictionary<int,CombatState>();
        readonly Dictionary<string,int> synergyCounts = new Dictionary<string,int>(), synergyLevels = new Dictionary<string,int>();
        readonly List<CombatEvent> events = new List<CombatEvent>();
        readonly List<CombatZone> zones = new List<CombatZone>();
        readonly List<Vector2> seaCells = new List<Vector2>();
        public IReadOnlyList<Vector2> SeaCells => seaCells;
        readonly HashSet<string> seaBursts = new HashSet<string>();
        readonly HashSet<int> relaySources = new HashSet<int>(), finalDeaths = new HashSet<int>();
        int actionSequence, killedGuards, nextSummon = -100000, seaExtra;
        float initialGuardHealth, resonanceBeats;
        double nextSeaDecay;

        sealed class CombatState
        {
            public FormationCampaignRules.UnitDefinition Spec;
            public readonly Dictionary<string,float> Stacks = new Dictionary<string,float>();
            public readonly Dictionary<string,CombatBuff> Buffs = new Dictionary<string,CombatBuff>();
            public readonly List<CombatShield> Shields = new List<CombatShield>();
            public readonly HashSet<int> SkillTargets = new HashSet<int>();
            public readonly HashSet<int> MeasuredTargets = new HashSet<int>();
            public readonly HashSet<string> Tags = new HashSet<string>();
            public readonly Dictionary<int,float> SkillDamage = new Dictionary<int,float>(), SkillHealing = new Dictionary<int,float>(), SkillShields = new Dictionary<int,float>();
            public readonly Dictionary<string,int> ItemStacks = new Dictionary<string,int>();
            public float BaseAttack, BaseArmor, BaseMR, BaseInterval, BaseSpeed, BaseRange, BaseSP, AttackPct, Power, DamageAmp, AttackSpeed, NaturalRate;
            public float GuardInitialSP, GuardActionSeconds;
            public float SPFraction, Echo, ComboProgress, SkillSPReturned, PromptReturned, RecordDamage, LowestHealth;
            public int LifeVersion, Action, ActionHits, ComboPerAction, Autos, Casts, NativeLives, LastTarget=-1, HandoffStacks, SkillBeats;
            public int RidingCarrier, Passenger, Template, Stage;
            public double NativeReturnAt, NativeReturnHealth, BusyUntil, ExpiresAt, NextPulseAt, RecordUntil;
            public bool NativePending, Boss, Elite, InitialGuard, RelayQuickUsed, EmergencyUsedThisLife;
            public double NextEmergencyAt, LastShieldSP=-100, LastReflect=-100, LastFold=-100, LastHealSP=-100, LastResShield=-100, LastSnowBreak=-100, LastBless=-100;
            public float ShieldAbsorbed, ReflectAbsorbed, HealStored, DamageStored, ArtsStored, FurnaceStored, CrawlerStored;
            public FormationActor RecordOwner;
        }
        sealed class CombatBuff { public float Value; public double Until; }
        sealed class CombatShield { public string Key; public float Amount; public double Until; public FormationActor Source; public bool Renewal; }
        sealed class CombatEvent { public double At; public int Order; public Action Fire; }
        sealed class CombatZone { public FormationActor Source; public Vector2 Center; public float Radius, Multiplier; public int Type, Cap; public double StartedAt, Until, Next; public string Kind; public Vector2Int[] Cells; }
        CombatState State(FormationActor a)
        {
            if (!combat.TryGetValue(a.Id,out var s)) combat[a.Id] = s = new CombatState();
            return s;
        }
        static FormationCampaignRules.UnitDefinition Spec(FormationActor a) => FormationCampaignRules.Find(a.ArtId) ?? FormationCampaignRules.Find("CARD_"+a.ArtId);
        string Key(FormationActor a) => State(a).Spec?.Id ?? Spec(a)?.Id ?? a.ArtId;
        int Level(string tag) => synergyLevels.TryGetValue(tag,out var n) ? n : 0;
        bool Tagged(FormationActor a,string tag) => State(a).Tags.Contains(tag);
        bool HasItem(FormationActor a,string key) => a is FormationFighter f && Campaign.Equipment(f.Instance).Any(i => i.Key == key);
        float Stack(FormationActor a,string k) => State(a).Stacks.TryGetValue(k,out var n) ? n : 0;
        float AddStack(FormationActor a,string k,float n,float cap=float.MaxValue) => State(a).Stacks[k] = Mathf.Min(cap,Stack(a,k)+n);
        float Buff(FormationActor a,string k) => State(a).Buffs.TryGetValue(k,out var b) && b.Until > Clock ? b.Value : 0;
        void GiveBuff(FormationActor a,string k,float value,double duration) { if(a!=null)State(a).Buffs[k]=new CombatBuff{Value=value,Until=Clock+duration}; }
        void Schedule(double at,Action fire) => events.Add(new CombatEvent{At=at,Order=++actionSequence,Fire=fire});
        void ScheduleOwned(FormationActor owner,double at,Action fire) => ScheduleOwnedPresentation(owner,at,fire);
        FormationFighter Carry => Units.FirstOrDefault(u => u.Enabled && u.Instance == Campaign.MainCarryInstance && u.Alive) ?? Units.FirstOrDefault(u=>u.Enabled&&u.Instance==Campaign.SecondCarryInstance&&u.Alive) ?? Units.Where(u => u.Enabled&&u.Alive).OrderByDescending(u=>u.Attack).FirstOrDefault();
        FormationFighter PairMember(int instance) => Units.FirstOrDefault(u => u.Enabled&&u.Instance==instance);
        FormationFighter Named(string id) => Units.FirstOrDefault(u => u.Enabled && Key(u)==id);
        bool HasLive(string id) => Named(id)?.Alive == true;
        public void BindCampaign(FormationCampaignRun run)
        {
            if(Phase!=FormationPhase.Preparation)throw new InvalidOperationException("Bind campaign in preparation only.");
            Campaign=run;
            if(run!=null) { ConfigureCampaignTeam(); BuildCampaignDefenders(); RefreshSeaPreview(); }
        }

        void ConfigureCampaignTeam()
        {
            foreach(var key in combat.Keys.Where(k=>k<100).ToArray())combat.Remove(key); synergyCounts.Clear(); synergyLevels.Clear();
            foreach(var unit in Units.Where(u=>u.Enabled))
            {
                var s=State(unit);s.Spec=Spec(unit);if(s.Spec==null)continue;
                foreach(var tag in s.Spec.Factions.Concat(s.Spec.Styles))s.Tags.Add(tag);
                foreach(var item in Campaign.Equipment(unit.Instance))
                {
                    string tag=BadgeTag(item.Key);if(tag!=null)s.Tags.Add(tag);
                }
                foreach(var tag in s.Tags)synergyCounts[tag]=synergyCounts.TryGetValue(tag,out var n)?n+1:1;
            }
            foreach(var rule in FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles))
            {
                int n=synergyCounts.TryGetValue(rule.Id,out var count)?count:0;
                synergyLevels[rule.Id]=rule.Levels.Where(l=>l<=n).DefaultIfEmpty(0).Max();
            }
            foreach(var unit in Units.Where(u=>u.Enabled))ConfigureCampaignFighter(unit);
        }
        static string BadgeTag(string key)
        {
            if(key==null)return null;
            if(key.StartsWith("徽章:"))return key.Split(':')[1];
            if(key.StartsWith("BADGE:"))return key.Substring(6);
            if(key.StartsWith("BADGE_"))return key.Substring(6);
            if(key.EndsWith("徽章"))return key.Replace("徽章","").Replace("运动","").Replace("小队","").Replace("时刻","").Replace("异响","").Replace("接力","");
            return null;
        }
        void ConfigureCampaignFighter(FormationFighter u)
        {
            var s=State(u);var p=s.Spec;if(p==null)return;
            float[] stars={1,1.8f,3.24f,5.832f};int star=Mathf.Clamp(u.Star-1,0,3);
            float hp=0,attack=0,power=0,aspeed=0,armor=0,mr=0,sp=0;
            foreach(var item in Campaign.Equipment(u.Instance))
            {
                var r=FormationCampaignRules.FindRecord(item.Key);
                if(r==null&&item.Key.StartsWith("徽章:")&&item.Key.Split(':').Length>2)r=FormationCampaignRules.FindRecord(item.Key.Split(':')[2]);
                if(r==null)continue;
                foreach(var stat in r.Stats)
                    switch(stat.Key){case "hp_pct":hp+=stat.Value;break;case "attack_pct":attack+=stat.Value;break;case "power":power+=stat.Value;break;case "attack_speed":aspeed+=stat.Value;break;case "armor":armor+=stat.Value;break;case "mr":mr+=stat.Value;break;case "sp_per_second":sp+=stat.Value;break;}
                if(item.Key=="EE")hp+=Mathf.Min(10,item.Stacks)*.02f;
            }
            if(Campaign.HasAugment("CS1")){hp+=.08f;armor+=3;}if(Campaign.HasAugment("CS2"))aspeed+=.1f;
            if(Campaign.HasAugment("CG6"))hp+=.1f;if(Campaign.HasAugment("CP1"))hp+=.05f;if(Campaign.HasAugment("CP6"))power+=.1f;
            if(Tagged(u,"萨卡兹")&&Level("萨卡兹")>0)
            {
                int l=Level("萨卡兹");bool blood=Campaign.SarkazPact==0||Campaign.SarkazPact==2&&l>=6,heavy=Campaign.SarkazPact==1||Campaign.SarkazPact==2&&l>=6;
                if(blood){hp-=.12f;attack+=l>=6?.55f:l>=4?.35f:.2f;}
                if(heavy){hp+=l>=6?.7f:l>=4?.45f:.25f;s.BaseInterval=p.Interval*1.15f;}else s.BaseInterval=p.Interval;
            }else s.BaseInterval=p.Interval;
            if(Tagged(u,"迅攻")){int l=Level("迅攻");aspeed+=l>=6?.5f:l>=4?.3f:l>=2?.15f:0;}
            s.BaseArmor=p.Armor+new[]{0,4,10,18}[star]+armor;s.BaseMR=p.MagicResistance+new[]{0,5,10,15}[star]+mr;
            attack+=Campaign.PreplanId=="P12"?Campaign.VeteranStacks(p.Id)*.02f:0;
            if(Campaign.HasAugment("CG3"))attack+=Mathf.Min(.24f,Mathf.Floor(s.BaseArmor/10)*.04f);
            s.BaseAttack=p.Attack*stars[star];s.AttackPct=attack;s.Power=power;s.AttackSpeed=aspeed;s.NaturalRate=1+sp;
            s.BaseRange=p.Range+(Campaign.HasAugment("CG4")?.4f:0);s.BaseSpeed=p.MoveSpeed*(Campaign.HasAugment("CS2")?1.1f:1);
            s.BaseSP=p.RequiredSP;
            if(p.Id=="TAL")
            {
                int c=Campaign.TalulahCost;s.BaseAttack=(c==5?560:c==4?310:190)*stars[star];s.BaseSP=c==5?28:c==4?24:22;
                p=Spec(u);u.MaxHealth=(c==5?2800:c==4?1900:1250)*stars[star]*(1+hp);s.BaseArmor=(c==5?26:c==4?18:12)+armor+new[]{0,4,10,18}[star];s.BaseMR=(c==5?40:c==4?30:25)+mr+new[]{0,5,10,15}[star];
                if(Campaign.TalulahEnhanced&&c==3){s.AttackPct+=.2f;s.BaseSP=18;}
            }else u.MaxHealth=p.Health*stars[star]*(1+hp);
            if(p.Id=="WIS") { if(Campaign.WisadelSkill==1)s.BaseSP=3;else if(Campaign.WisadelSkill==2)s.BaseSP=24; }
            RefreshCampaignStats(u);u.Health=u.MaxHealth;u.Position=u.SpawnCenter;u.Alive=true;u.Shield=0;
        }
        void RefreshCampaignStats(FormationActor a)
        {
            var s=State(a);float aspeed=s.AttackSpeed+Buff(a,"as")+Buff(a,"herald_as")+Buff(a,"can_as")+Stack(a,"flywheel")*.02f+Stack(a,"rush")*.03f;
            int relay=Level("接棒");float per=relay>=6?.12f:relay>=4?.08f:.05f;aspeed+=s.HandoffStacks*per+Buff(a,"stage_as");
            if(Tagged(a,"海嗣")&&Level("海嗣")>=5&&seaBursts.Count>=4)aspeed+=.35f;
            a.Interval=s.BaseInterval<=0?0:s.BaseInterval/Mathf.Clamp(1+aspeed-(Buff(a,"cold")>0?.2f:0)-Buff(a,"nerve_slow"),0.1f,3);
            a.Attack=s.BaseAttack*(1+s.AttackPct+Buff(a,"attack")+Buff(a,"can_attack")+Buff(a,"patriot_attack")+Stack(a,"stone")*.05f+Stack(a,"blooddebt")*.03f);
            a.Armor=s.BaseArmor+Stack(a,"fold");a.MagicResistance=s.BaseMR+Stack(a,"fold");a.Range=s.BaseRange;
            float guardSlow = a is FormationFighter fighter ? GuardMovementSlow(fighter) : 0;
            a.MoveSpeed=s.BaseSpeed*(1+Buff(a,"move"))*(1-guardSlow);
            if(a is FormationFighter f)
            {
                float baseRequired=s.BaseSP;
                if(s.Spec?.Recovery!="攻击"&&!(s.Spec?.Id=="WIS"&&Campaign.WisadelSkill==1))baseRequired=Mathf.Max(s.BaseSP*.75f,s.BaseSP-Mathf.Min(5,Stack(a,"manual")));
                a.SkillRequired=baseRequired*(1+Stack(a,"reactor")*.05f);
            }
        }
        float Power(FormationActor a) => 1+State(a).Power+Buff(a,"power")+Buff(a,"yi_power")+Stack(a,"measure")*.02f+Stack(a,"coil")*.03f+Stack(a,"prism")*.03f+Stack(a,"furnace")*.04f+(Campaign.HasAugment("CP5")?Mathf.Min(.4f,State(a).HandoffStacks*.08f):0);
        bool AttackRecovery(FormationActor a) => State(a).Spec?.Recovery=="攻击" || Key(a)=="WIS"&&Campaign.WisadelSkill==1;
        void GainSP(FormationActor a,float amount,bool attackPoint=false,bool echo=false)
        {
            if(a==null||a.SkillRequired<=0||a is FormationFighter fighter&&SnowBossFreezeRemaining(fighter)>0)return;var s=State(a);
            if(AttackRecovery(a)&&!attackPoint){s.SPFraction+=amount;amount=Mathf.Floor(s.SPFraction/3);s.SPFraction-=amount*3;}
            float actual=Mathf.Min(amount,Mathf.Max(0,a.SkillRequired-a.SkillCharge));a.SkillCharge+=actual;
            if(echo&&amount>actual)s.Echo=Mathf.Min(new[]{0,0,4,6,8,12}[Mathf.Clamp(Level("同频"),0,5)],s.Echo+amount-actual);
        }
        void BeginCampaignBattle()
        {
            ConfigureCampaignTeam();events.Clear();zones.Clear();Summons.Clear();relaySources.Clear();finalDeaths.Clear();seaCells.Clear();seaBursts.Clear();seaExtra=0;
            BattleGold=0;killedGuards=0;actionSequence=0;resonanceBeats=0;nextSeaDecay=1;BattleEvents.Clear();SkillCasts.Clear();
            snowBossFrozen.Clear();
            foreach(var d in Defenders){var s=State(d);s.LowestHealth=d.MaxHealth;d.Health=d.MaxHealth;d.Alive=true;d.SkillCharge=s.GuardInitialSP;ResetGuardKit(d);}
            InitialDefenderCount=Defenders.Count(d=>State(d).InitialGuard);initialGuardHealth=Defenders.Where(d=>State(d).InitialGuard).Sum(d=>d.MaxHealth*(d.IsTwoPhaseBoss?2:1));
            foreach(var u in Units.Where(u=>u.Enabled))
            {
                var s=State(u);u.PhaseStage=0;s.NativeLives=0;u.NativePhase=0;u.NativePhaseChangedAt=0;u.NativePhaseReturningUntil=0;s.LastTarget=-1;s.Action=0;u.SkillCharge=s.Spec?.InitialSP??0;
                if(Key(u)=="TAL")u.SkillCharge=Campaign.TalulahCost==5?16:Campaign.TalulahCost==4?12:8;
                if(Key(u)=="WIS")u.SkillCharge=Campaign.WisadelSkill==1?0:Campaign.WisadelSkill==2?12:16;
                if(Campaign.HasAugment("CS3"))GainSP(u,u.SkillRequired*.2f,true);if(HasItem(u,"CF"))GainSP(u,u.SkillRequired*.25f,true);
                if(Key(u)=="M27")Shield(u,u,u.MaxHealth*.25f,999,"native");
                if(HasItem(u,"CD"))Shield(u,u,u.MaxHealth*.1f,6,"curtain");
                if(Campaign.HasAugment("CS6")&&Units.Any(v=>v!=u&&v.Enabled&&Vector2.Distance(v.SpawnCenter,u.SpawnCenter)<=1.5f))Shield(u,u,u.MaxHealth*.08f,6,"mutual");
            }
            SetupPairs();SetupSea();SpawnGolem();
            var king=Named("B11");if(king?.Star>=3)CastCampaignSkill(king,FighterTarget(king),true);
        }

        void BuildCampaignDefenders()
        {
            foreach (var key in combat.Keys.Where(k => k >= 100).ToArray()) combat.Remove(key);
            Defenders.Clear();
            var node = Campaign.CurrentNode;
            LimitSeconds = node.TimeLimit;
            if (Campaign.GuardTrialArt is { } trialArt)
            {
                PlaceSingleGuard(trialArt);
                return;
            }
            if (node.DefenderCount == 0) return;

            var boss = node.Id == FormationCampaignRules.SupernovaBossNodeId ? FormationCampaignRules.SupernovaBoss :
                FormationCampaignRules.Bosses.FirstOrDefault(entry => entry.NodeId == node.Id);
            if (boss?.ArtId is "D27" or "D31")
            {
                var cell = new Vector2Int(Width - 5, 4);
                if (Terrain != null && (!Terrain.CanPlaceDefender(cell) || !Terrain.IsWalkable(cell) || Terrain.HeightAt(cell) > 0))
                    throw new InvalidOperationException($"No legal boss cell for campaign node {node.Id} at {cell}.");
                AddCampaignDefender(node, null, boss.ArtId, cell, 1, boss);
                return;
            }

            var rng = new FormationRandom(unchecked((uint)Seed ^ ((uint)(Campaign.NodeIndex + 1) * 0x9e3779b9u)));
            var north = FormationCampaignRules.NorthGuardFormation;
            var south = FormationCampaignRules.SouthGuardFormation;
            bool preferNorth = rng.Next(2) == 0;
            var formation = preferNorth ? north : south;
            if (!CanPlaceGuardFormation(node, formation))
            {
                formation = preferNorth ? south : north;
                if (!CanPlaceGuardFormation(node, formation))
                    throw new InvalidOperationException($"No legal guard formation for campaign node {node.Id}.");
            }

            int tier = node.Kind == "遭遇" ? Campaign.CurrentEncounterTier : 1;
            for (int i = 0; i < node.Front.Length; i++)
                AddCampaignDefender(node, formation, node.Front[i], formation.Front[i], tier, boss);
            for (int i = 0; i < node.Rear.Length; i++)
                AddCampaignDefender(node, formation, node.Rear[i], formation.Rear[i], tier, boss);
        }

        bool CanPlaceGuardFormation(FormationCampaignRules.NodeDefinition node, FormationCampaignRules.GuardFormation formation)
        {
            if (Terrain == null) return true;
            for (int i = 0; i < node.Front.Length; i++)
            {
                Vector2 cell = formation.Front[i];
                if (!Terrain.CanPlaceDefender(cell) || Terrain.HeightAt(cell) > 0) return false;
            }
            for (int i = 0; i < node.Rear.Length; i++)
            {
                Vector2 cell = formation.Rear[i];
                if (!Terrain.CanPlaceDefender(cell)) return false;
            }
            foreach (var id in node.Front.Concat(node.Rear))
            {
                var cell = formation.SkillCell(id);
                if (cell.HasValue && !Terrain.IsWalkable(cell.Value)) return false;
            }
            return true;
        }

        void AddCampaignDefender(FormationCampaignRules.NodeDefinition node, FormationCampaignRules.GuardFormation formation,
            string artId, Vector2Int cell, int tier, FormationCampaignRules.BossDefinition boss)
        {
            var guard = FormationCampaignRules.DefenderByArtId(artId);
            var role = FormationCampaignRules.Defenders[guard.Role];
            bool isBoss = boss?.ArtId == artId;
            bool elite = guard.Rarity == 6;
            float health = FormationCampaignRules.RoundedGuardStat(role.Health, node.HealthScale, elite ? 1.35m : 1m);
            float attack = FormationCampaignRules.RoundedGuardStat(role.Attack, node.AttackScale, elite ? 1.10m : 1m);
            var d = new FormationDefender
            {
                Id = 100 + Defenders.Count,
                Kind = int.Parse(artId.Substring(1)) - 1,
                ArtId = artId,
                Name = guard.Name,
                Healer = guard.Role == 5,
                Position = cell,
                SkillCell = formation?.SkillCell(artId),
                IsSnowBoss = isBoss && artId == "D27",
                SnowBossPhase = isBoss && artId == "D27" ? 1 : 0,
                IsSupernovaBoss = isBoss && artId == "D31",
                SupernovaBossPhase = isBoss && artId == "D31" ? 1 : 0,
                MaxHealth = isBoss ? boss.Health : FormationCampaignRules.RoundedGuardStat(health, FormationCampaignRules.EncounterHealthScale(tier), 1m),
                Attack = isBoss ? boss.Attack : FormationCampaignRules.RoundedGuardStat(attack, FormationCampaignRules.EncounterAttackScale(tier), 1m),
                Armor = isBoss ? boss.Armor : role.Armor,
                MagicResistance = isBoss ? boss.MagicResistance : role.MagicResistance,
                Range = isBoss ? boss.Range : role.Range,
                Interval = isBoss ? boss.Interval : artId == "D32" ? ChenAttackInterval : role.Interval,
                SkillRequired = isBoss ? boss.RequiredSP : guard.RequiredSP ?? role.RequiredSP,
            };
            if (!d.IsSupernovaBoss) ConfigureArchivedGuard(d);
            ResetActor(d, true, Vector2.left);
            var state = State(d);
            state.Template = guard.Role;
            state.Boss = isBoss;
            state.Elite = elite;
            state.InitialGuard = true;
            state.LowestHealth = d.MaxHealth;
            state.BaseAttack = d.Attack;
            state.BaseArmor = d.Armor;
            state.BaseMR = d.MagicResistance;
            state.BaseInterval = d.Interval;
            state.BaseRange = d.Range;
            state.GuardInitialSP = isBoss ? boss.InitialSP : guard.InitialSP ?? role.InitialSP;
            state.GuardActionSeconds = isBoss ? boss.ActionSeconds : role.ActionSeconds;
            d.SkillCharge = state.GuardInitialSP;
            Defenders.Add(d);
        }

        void TickCampaign(float dt)
        {
            Hits.RemoveAll(h=>h.At<Clock-3);
            presentationActions.RemoveAll(action=>Math.Max(action.EndAt,action.ContactAt)<Clock-6);
            CompleteSnowBossTransition();
            CompleteSupernovaBossTransition();
            foreach(var a in Units.Cast<FormationActor>().Concat(Defenders).Concat(Summons).ToArray())
            {
                var s=State(a);a.IsMoving=false;
                s.Shields.RemoveAll(sh=>sh.Until<=Clock||sh.Amount<=.0001f);a.Shield=s.Shields.Sum(sh=>sh.Amount);
                if(a is FormationFighter u&&!a.Alive&&u.Enabled)
                {
                    double at=s.NativePending?s.NativeReturnAt:u.RebornAt;u.RespawnRemaining=(float)Math.Max(0,at-Clock);
                    if(Clock+1e-8>=at)ReturnCampaignFighter(u);continue;
                }
                if(!a.Alive)continue;
                if(a is FormationFighter frozen && SnowBossFreezeRemaining(frozen)>0)continue;
                if(s.Spec!=null)
                {
                    RefreshCampaignStats(a);
                    if(!AttackRecovery(a)&&a.SkillRequired>0)
                    {
                        float rate=s.NaturalRate+Buff(a,"king_sp")+Buff(a,"yi_sp")+Stack(a,"reactor")*.12f;
                        if(a==Carry&&HasLive("B11"))rate+=1;if(Key(a)=="B21"&&s.BusyUntil<=Clock)rate=4;
                        a.SkillCharge=Mathf.Min(a.SkillRequired,a.SkillCharge+Mathf.Min(5,rate)*dt);
                    }
                    if(Key(a)=="M21")CombatHeal(a,(FormationFighter)a,a.MaxHealth*.02f*dt,false,false,showFeedback:false);
                    if(Key(a)=="M34"&&(Clock>=5||a.LastHitAt>=0||a.Health<a.MaxHealth)) { if(Stack(a,"awake")==0)AddStack(a,"awake",1,1);SelfLoss((FormationFighter)a,a.MaxHealth*.02f*dt); }
                }
                else if(a is FormationDefender defender && UsesGuardTimeCharge(defender) &&
                    (defender.ArtId is not ("D30" or "D32") && !IsArchivedSignatureGuard(defender.ArtId) || !GuardSkillActive(defender)))
                    a.SkillCharge=Mathf.Min(a.SkillRequired,a.SkillCharge+dt);
                if(s.RecordUntil>0&&Clock>=s.RecordUntil){s.RecordUntil=0;var target=(s.RecordOwner as FormationFighter)!=null?FighterTarget((FormationFighter)s.RecordOwner):null;if(target!=null)CombatDamage(a,target,s.RecordDamage,0,true,false);s.RecordDamage=0;}
            }
            // A fixed ordered queue prevents action haste, multihit, or deaths from recursing.
            while(true)
            {
                var next=events.Where(e=>e.At<=Clock+1e-8).OrderBy(e=>e.At).ThenBy(e=>e.Order).FirstOrDefault();if(next==null)break;events.Remove(next);next.Fire();
            }
            TickZones();
            foreach(var summon in Summons.ToArray())
            {
                if(State(summon).ExpiresAt<=Clock){Summons.Remove(summon);continue;}
                if(summon.Alive)TickCampaignFighter(summon,dt,true);
            }
            if(Phase==FormationPhase.Fighting)
            {
                foreach(var u in Units.Where(u=>u.Enabled&&u.Alive).ToArray())TickCampaignFighter(u,dt,false);
                foreach(var d in Defenders.Where(d=>d.Alive).ToArray())TickCampaignDefender(d);
                if(RemainingDefenders==0){ReleaseSuiClawsOnClear();Phase=FormationPhase.Exiting;foreach(var u in Units)u.TargetId=-1;}
            }
            if(Phase==FormationPhase.Exiting)foreach(var u in Units.Where(u=>u.Enabled&&u.Alive))if(Clock>=u.ActionUntil)Move(u,new Vector2(Width-1,u.Position.y),dt,false);
            UpdateRiders();
        }

        void TickCampaignFighter(FormationFighter u,float dt,bool summon)
        {
            var s=State(u);
            if(s.NativePending||Buff(u,"stun")>0||Buff(u,"freeze")>0||SnowBossFreezeRemaining(u)>0)return;
            if(s.RidingCarrier!=0){var carrier=Units.FirstOrDefault(v=>v.Id==s.RidingCarrier&&v.Alive);if(carrier!=null)u.Position=CampaignRiderPosition(carrier);}
            bool cadejo=u.ArtId=="B14";
            bool entered=HasEnteredBattleArea(u);
            if(!entered&&!cadejo) { if(Clock>=u.ActionUntil&&s.RidingCarrier==0)Move(u,new Vector2(DeploymentColumns+1,u.Position.y),dt,true);return; }
            if(Key(u)=="M34"&&Stack(u,"awake")==0)return;
            var target=FighterTarget(u);u.TargetId=target?.Id??-1;
            if(Clock<u.ActionUntil||s.BusyUntil>Clock)return;
            if(MoveSnowBossInduced(u,dt))return;
            if(cadejo&&(!entered||target==null||Vector2.Distance(u.Position,target.Position)>u.Range))
            {
                AdvanceCadejoEngagement(u,target);
                return;
            }
            if(target==null)return;
            if(Key(u)=="M03"&&Stack(u,"contact")==0){AddStack(u,"contact",1,1);GainSP(u,2,true);}
            bool healer=s.Spec?.DamageType=="治疗";
            float targetDistance = Vector2.Distance(u.Position, target.Position);
            if(targetDistance>u.Range&&!healer)
            {if(s.RidingCarrier==0)Move(u,target.Position,dt,true);return;}
            if(TalulahRainEnd(u)+1e-8>=Clock)return;
            Face(u,target.Position);
            if(!summon&&u.SkillRequired>0&&u.SkillCharge+1e-5>=u.SkillRequired&&Buff(u,"silence")==0)
            {
                if (Key(u) != "B23" || targetDistance <= AnastasioSkillRange)
                {
                    if (CastCampaignSkill(u, target)) return;
                }
                else if (s.RidingCarrier == 0 && (Terrain == null ||
                    Terrain.TryApproach(u.Position, target.Position, AnastasioSkillRange, out _, out _)))
                {
                    Move(u, target.Position, dt, true, AnastasioSkillRange);
                    return;
                }
            }
            if(u.Interval<=0||Clock+1e-8<u.NextAttackAt)return;
            if(healer)
            {
                var wounded=Units.Where(v=>v.Enabled&&v.Alive&&Vector2.Distance(u.Position,v.Position)<=u.Range&&
                    (v.Health<v.MaxHealth-.0001f||Level("复苏")>=2&&Tagged(v,"复苏"))).OrderBy(v=>v.Health/v.MaxHealth).FirstOrDefault();
                if(wounded==null)return;
                var heal=BeginAction(u,.35,false,wounded,true);
                QueueOrdinaryAction(u,wounded,heal,()=>{CombatHeal(u,wounded,u.Attack*(Key(u)=="B10"?1.4f:1.1f),false,true);CompleteAuto(u,false);});return;
            }
            if(!AllowsOrdinaryAttack(u))return;
            var attack=BeginAction(u,Math.Min(.36,u.Interval*.45),false,target);
            QueueOrdinaryAction(u,target,attack,()=>ResolveOrdinaryAttack(u,target));
        }
        void ResolveOrdinaryAttack(FormationFighter u,FormationDefender target)
        {
            var s=State(u);s.Autos++;
            if(s.LastTarget!=target.Id){s.Stacks["rush"]=Mathf.Floor(Stack(u,"rush")/2);s.LastTarget=target.Id;}else if(Tagged(u,"迅攻")&&Level("迅攻")>=4)AddStack(u,"rush",1,10);
            string key=Key(u);float mult=key=="M16"?.75f:key=="M20"?.4f:key=="WIS"&&Campaign.WisadelSkill==2&&Buff(u,"wisadel")>0?2:1;
            if(key=="M30"&&Stack(u,"fireautos")>0){mult=2.2f;s.Stacks["fireautos"]--;}
            if(key=="M13"&&Buff(target,"freeze")>0)mult*=1.8f;
            int count=key=="GOLEM"&&s.Stage>=3?3:key=="M16"?2:key=="M24"?3:key=="WIS"&&Buff(u,"wisadel")>0?(u.Star>=3?5:3):1;
            var victims=Nearby(target.Position,count>1?1.8f:0,count,target);
            int segments=key=="M20"?3:1;
            for(int seg=0;seg<segments;seg++)NativeSegment(u,victims,u.Attack*mult+(key=="SLUG"?u.MaxHealth*.03f:0),s.Spec?.DamageType=="法"?1:0,false,0,false);
            if(key=="M29"&&u.Shield>0)foreach(var v in victims)CombatDamage(u,v,u.Attack*.8f,1,false,false);
            if(key=="SLUG"&&s.Autos%4==0)foreach(var v in Nearby(target.Position,1.2f,3,target))CombatDamage(u,v,(u.Attack+u.MaxHealth*.03f)*.6f,0,false,false);
            foreach(var v in victims)
            {
                if(key=="M35")Element(u,v,6,false);if(key=="M34")Element(u,v,10,false);if(key=="M36")Element(u,v,15,false);if(key=="M30")Element(u,v,mult>1?24:12,true);
                if(HasItem(u,"AD")){AddStack(v,"armorbreak",1,5);GiveBuff(v,"armorbreak",1,6);}
                if(Tagged(u,"雪怪")&&Level("雪怪")>=2&&s.Autos%3==0)Cold(v);
            }
            if(key=="M28")AddStack(u,"stone",1,10);
            if(key=="B14"&&s.Autos%3==0)SpawnDog(u);
            CompleteAuto(u,true);
        }
        FormationPresentationAction BeginAction(FormationActor a,double duration,bool skill,FormationActor target=null,bool heal=false)
        {
            PreserveActivePresentationContext(a);
            var s=State(a);s.Action=++actionSequence;s.ActionHits=0;s.ComboPerAction=0;s.SkillTargets.Clear();s.SkillSPReturned=0;s.PromptReturned=0;s.SkillBeats=0;
            a.LastActionAt=Clock;a.ActionUntil=Clock+duration;a.NextAttackAt=Math.Max(a.NextAttackAt,Clock+(skill?duration:a.Interval));
            if(skill){a.LastSkillAt=Clock;a.SkillAnimation=Key(a)=="B22"?"Attack_2":Key(a)=="WIS"?(Campaign.WisadelSkill==1?"Skill_1":Campaign.WisadelSkill==2?"Skill_2_Begin":"Skill_3_Begin"):"skill";}
            bool wisadelSkillTwoAttack=!skill&&Key(a)=="WIS"&&Campaign.WisadelSkill==2&&Buff(a,"wisadel")>0;
            a.PresentationAction=skill?a.SkillAnimation:wisadelSkillTwoAttack?"Skill_2_Loop":"attack";a.PresentationStartedAt=Clock;a.PresentationDuration=duration;
            s.Stacks["tape_auto"]=0;s.Stacks["last_skill_raw"]=0;s.SkillDamage.Clear();s.SkillHealing.Clear();s.SkillShields.Clear();
            if(target==null&&a is FormationFighter fighter)
            {
                heal=State(a).Spec?.DamageType=="治疗";
                target=heal?Units.Where(u=>u.Enabled&&u.Alive).OrderBy(u=>u.Health/u.MaxHealth).FirstOrDefault():FighterTarget(fighter);
            }
            if(heal&&target!=null)Face(a,target.Position);
            return BeginPresentation(a,target,skill,heal,duration);
        }
        void CompleteAuto(FormationFighter u,bool attack)
        {
            var s=State(u);
            if(attack&&AttackRecovery(u))GainSP(u,1,true);
            if(Key(u)=="B22")GainSP(u,1);
            if(attack&&HasItem(u,"AB"))AddStack(u,"flywheel",1,15);
            if(attack&&HasItem(u,"BF"))AddStack(u,"coil",1,8);
            if(attack&&s.Autos%4==0&&HasItem(u,"BB")){var t=FighterTarget(u);if(t!=null)CombatDamage(u,t,u.Attack*.35f,0,false,false);}
            if(attack&&s.Autos%3==0)
            {
                if(HasItem(u,"BC"))GainSP(u,AttackRecovery(u)?1:2,AttackRecovery(u));
                if(Tagged(u,"迅攻")&&Level("迅攻")>=6)GainSP(u,AttackRecovery(u)?1:2,AttackRecovery(u));
                if(u==Carry&&(HasLive("B11")||Buff(u,"yi_sp")>0)&&AttackRecovery(u))GainSP(u,1,true);
            }
            if(u==Carry&&HasLive("B13")&&Clock-s.LastBless>=.35)
            {s.LastBless=Clock;if(!AttackRecovery(u))GainSP(u,1);else if(s.Autos%2==0)GainSP(u,1,true);}
            if(AttackRecovery(u)&&Buff(u,"king_sp")>0)GainSP(u,1,true);
            Beat(u,1,false);
        }
    }
}
