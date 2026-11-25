using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        double v08WindAt, v08EternalAt, v08GolemAt;
        readonly Dictionary<int,string> steamOriginalArts = new Dictionary<int,string>();
        bool FiveStar(FormationActor actor) => actor is FormationFighter fighter &&
            FormationCampaignRules.IsThreeStarFiveCost(Spec(actor),fighter.Star,Campaign?.TalulahCost??3);
        internal bool ControlImmune(FormationActor actor) => Campaign!=null && FiveStar(actor);
        bool IsHiddenDriver(FormationActor actor) => Stack(actor,"steam_hidden")>0;
        bool IsMech(FormationActor actor) => Stack(actor,"steam_mech")>0;
        static bool ControlKey(string key) => key is "stun" or "freeze" or "silence" or "root" or "bind" or "forced_move";
        bool LiveFive(string id) => Units.Any(unit=>unit.Enabled&&unit.Alive&&!IsHiddenDriver(unit)&&Key(unit)==id&&FiveStar(unit));
        FormationFighter ResolveV08Carry(int instance)
        {
            var selected=Units.FirstOrDefault(unit=>unit.Enabled&&unit.Instance==instance);
            if(selected?.Alive==true)return selected;
            return selected!=null&&IsHiddenDriver(selected)?Units.FirstOrDefault(unit=>unit.Enabled&&unit.Alive&&IsMech(unit)):null;
        }
        void ResetSteamPresentation()
        {
            foreach(var unit in Units)
                if(steamOriginalArts.TryGetValue(unit.Instance,out var original))
                {unit.ArtId=original;unit.Name=FormationUnitCatalog.Find(original)?.Name??unit.Name;}
            steamOriginalArts.Clear();
        }
        void SetupV08Battle()
        {
            v08WindAt=2;v08EternalAt=1;v08GolemAt=8;
            if(LiveFive("B13"))foreach(var ally in Units.Where(unit=>unit.Enabled))State(ally).Stacks["saint_life"]=1;
            ExpandV08Sea();SetupSteamMech();
        }
        void RefreshV08PatriotAura()
        {
            bool patriot=LiveFive("M20");
            foreach(var ally in Units.Where(unit=>unit.Enabled))
            {
                var state=State(ally);float contribution=Stack(ally,"patriot_hp_amount");
                bool active=Stack(ally,"patriot_hp_active")>0;
                if(active!=patriot && contribution>0)
                {
                    float ratio=ally.MaxHealth>0?ally.Health/ally.MaxHealth:0;
                    ally.MaxHealth=Math.Max(1,ally.MaxHealth+(patriot?contribution:-contribution));
                    ally.Health=ally.MaxHealth*ratio;state.Stacks["patriot_hp_active"]=patriot?1:0;
                }
            }
        }
        void TickV08Battle(float dt)
        {
            RefreshV08PatriotAura();
            if(Campaign.HasIberiaOption(0))foreach(var ally in Units.Where(unit=>unit.Enabled&&unit.Alive&&!IsHiddenDriver(unit)))
                CombatHeal(ally,ally,ally.MaxHealth*.02f*dt,false,false,false);
            if(Level("雪怪")>=6 && Clock+1e-8>=v08WindAt)
            {v08WindAt+=8;foreach(var enemy in Defenders.Where(enemy=>enemy.Alive).ToArray())Cold(enemy);BattleEvents.Add($"{Clock:0.00}:雪怪寒风");}
            if(Level("深池")>=7)
            {
                var owner=Units.FirstOrDefault(unit=>unit.Enabled&&Tagged(unit,"深池"));
                foreach(var enemy in Defenders.Where(enemy=>enemy.Alive))
                {
                    if(Stack(enemy,"eternal_max")==0)State(enemy).Stacks["eternal_max"]=enemy.MaxHealth;
                    GiveBuff(enemy,"eternal_heal_down",.5f,double.PositiveInfinity);
                }
                if(owner!=null && Clock+1e-8>=v08EternalAt)
                {v08EternalAt+=1;foreach(var enemy in Defenders.Where(enemy=>enemy.Alive).ToArray())CombatDamage(owner,enemy,Stack(enemy,"eternal_max")*.01f,2,false,false);}
            }
            if(Level("泥岩")>=5 && Clock+1e-8>=v08GolemAt)
            {
                v08GolemAt+=8;var golem=Summons.FirstOrDefault(unit=>unit.ArtId=="GOLEM"&&unit.Alive&&State(unit).Stage==5);
                if(golem!=null)foreach(var ally in Units.Where(unit=>unit.Enabled&&unit.Alive))Shield(golem,ally,ally.MaxHealth*.2f,8,"v08-golem");
            }
        }
        bool V08PreventLethal(FormationFighter target)
        {
            if(target.Health>0 || Summons.Contains(target) || IsHiddenDriver(target) || Stack(target,"saint_life")==0)return false;
            State(target).Stacks["saint_life"]=0;target.Health=target.MaxHealth;
            BattleEvents.Add($"{Clock:0.00}:圣徒免死:{target.Instance}");return true;
        }
        bool V08Execute(FormationFighter source,FormationDefender target)
        {
            if(!Campaign.HasIberiaOption(2)||Summons.Contains(source)||IsHiddenDriver(source)||!target.Alive)return false;
            int thresholdPercent=State(target).Boss||State(target).Elite?5:12;
            if((double)target.Health*100 >= (double)target.MaxHealth*thresholdPercent)return false;
            target.Health=0;return true;
        }
        float V08SaintExtra(FormationFighter source,bool skill)
        {
            if(Buff(source,"saint_damage")<=0)return 0;
            // The cap belongs to each recipient's individual native action, not each victim.
            if(skill&&Stack(source,"saint_v08")>0)
            {
                if(Stack(source,"saint_segments")>=12)return 0;
                AddStack(source,"saint_segments",1,12);
            }
            return Buff(source,"saint_damage");
        }
        bool TryV08Skill(FormationFighter unit,FormationDefender target,FormationPresentationAction presentation,FormationSuiClawStrike opening)
        {
            if(IsMech(unit))
            {
                SkillSequence(unit,target,3,1.2f,0,1.1f,1.8f,3);
                if(Level("蒸汽")>=3)SkillSequence(unit,target,3,1.8f,1,1.1f,100,int.MaxValue,allBoard:true);
                return true;
            }
            if(!FiveStar(unit))return false;
            var state=State(unit);
            switch(Key(unit))
            {
                case "TAL":
                    if(state.Casts==1)GiveBuff(unit,"v08_invulnerable",1,Math.Max(0,presentation.EndAt-Clock));
                    SkillSequence(unit,target,6,25,2,2,100,int.MaxValue,allBoard:true);
                    AddZone(unit,target.Position,100,.4f,8,int.MaxValue,"burn");return true;
                case "WIS":
                    if(Campaign.WisadelSkill==1)SkillSequence(unit,target,12,15,2,1);
                    else if(Campaign.WisadelSkill==2){GiveBuff(unit,"wisadel",1,8);presentation.EndAt=Math.Max(presentation.EndAt,Clock+8);}
                    else
                    {
                        state.BusyUntil=presentation.At+8.4;unit.ActionUntil=Math.Max(unit.ActionUntil,state.BusyUntil);presentation.EndAt=Math.Max(presentation.EndAt,state.BusyUntil);
                        for(int index=0;index<12;index++)
                        {
                            int shell=index;double launch=presentation.LaunchedAt+index*.7;
                            ScheduleOwned(unit,launch,()=>{if(!unit.Alive)return;var victim=FighterTarget(unit);if(victim==null)return;
                                QueueSkillProjectile(unit,presentation,victim,shell,"Skill_3_Loop",launch,()=>NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.Attack*20,2,true,0,true),false);});
                        }
                    }return true;
                case "B08":
                    NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.Attack*200,2,true,0,true);
                    AddZone(unit,target.Position,100,10,8,int.MaxValue,"burn");return true;
                case "M20":
                    foreach(var ally in Units.Where(ally=>ally.Enabled&&ally.Alive))
                    {Shield(unit,ally,ally.MaxHealth*.6f,12,"patriot");GiveBuff(ally,"patriot_dr",.5f,12);}return true;
                case "B10":
                    foreach(var ally in Units.Where(ally=>ally.Enabled&&ally.Alive))
                    {CombatHeal(unit,ally,ally.MaxHealth,true,true);GiveBuff(ally,"can_attack",3,12);GiveBuff(ally,"can_power",3,12);GiveBuff(ally,"can_as",1,12);}
                    if(state.Casts==1)BattleGold+=10;return true;
                case "B11":
                    foreach(var ally in Units.Where(ally=>ally.Enabled&&ally.Alive))GiveBuff(ally,"king_damage",5,15);
                    foreach(var enemy in Defenders.Where(enemy=>enemy.Alive))GiveBuff(enemy,"v08_king_mr",1,15);PromptSupport(unit);return true;
                case "B12":
                    NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.Attack*200,2,true,0,true);GainSP(unit,18,true);return true;
                case "B13":
                    foreach(var ally in Units.Where(ally=>ally.Enabled&&ally.Alive))
                    {GiveBuff(ally,"saint_damage",unit.Attack*30,15);State(ally).Stacks["saint_v08"]=1;State(ally).Stacks["saint_segments"]=0;}return true;
                case "B21":
                    Vector2 centre=Campaign.SuiCentre.HasValue?(Vector2)Campaign.SuiCentre.Value:unit.SpawnCenter;
                    SettleSuiClawStrike(unit,opening,centre,true);opening.LiftAt=double.PositiveInfinity;
                    state.BusyUntil=Clock+86400;presentation.EndAt=state.BusyUntil;
                    NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.Attack*200,2,true,0,true);
                    ScheduleSuiV08Pulse(unit);return true;
            }
            return false;
        }
        void ScheduleSuiV08Pulse(FormationFighter owner)
        {
            ScheduleOwned(owner,Clock+4,()=>
            {
                if(!owner.Alive||Phase!=FormationPhase.Fighting)return;
                NativeSegment(owner,Defenders.Where(enemy=>enemy.Alive).ToArray(),owner.Attack*50,2,true,0,true);
                ScheduleSuiV08Pulse(owner);
            });
        }
        void V08NativeReturn(FormationFighter unit)
        {
            if(Key(unit)=="M20" && FiveStar(unit) && Stack(unit,"patriot_phase_blast")==0)
            {AddStack(unit,"patriot_phase_blast",1,1);NativeSegment(unit,Defenders.Where(enemy=>enemy.Alive).ToArray(),unit.MaxHealth*10,2,true,0,true);}
            if(Key(unit)=="B08" && FiveStar(unit) && Stack(unit,"fire_recall")==0)
            {
                AddStack(unit,"fire_recall",1,1);
                foreach(var ally in Units.Where(ally=>ally.Enabled&&!ally.Alive&&!IsHiddenDriver(ally)&&!State(ally).NativePending).ToArray())
                {ally.RebornAt=0;ReturnCampaignFighter(ally);ally.Health=ally.MaxHealth;}
            }
        }
        void ExpandV08Sea()
        {
            if(Level("海嗣")<6||seaCells.Count==0)return;
            var goals=Units.Where(unit=>unit.Enabled).SelectMany(unit=>
                Enumerable.Range(unit.DeploymentCells.xMin,unit.DeploymentCells.width).SelectMany(x=>Enumerable.Range(unit.DeploymentCells.yMin,unit.DeploymentCells.height).Select(y=>new Vector2Int(x,y))))
                .Distinct().OrderBy(cell=>cell.y*Width+cell.x).ToArray();
            foreach(var goal in goals)
            {
                var queue=new Queue<Vector2Int>();var previous=new Dictionary<Vector2Int,Vector2Int>();queue.Enqueue(goal);previous[goal]=goal;
                Vector2Int? reached=null;
                while(queue.Count>0)
                {
                    var cell=queue.Dequeue();if(seaCells.Contains(cell)){reached=cell;break;}
                    var neighbours=new[]{cell+Vector2Int.left,cell+Vector2Int.right,cell+Vector2Int.up,cell+Vector2Int.down}.OrderBy(next=>next.y*Width+next.x);
                    foreach(var next in neighbours)
                        if(next.x>=0&&next.x<Width&&next.y>=0&&next.y<Height&&!previous.ContainsKey(next)&&
                            (Terrain==null||Terrain.IsWalkable(next)&&Terrain.CanTravel(cell,next)))
                        {previous[next]=cell;queue.Enqueue(next);}
                }
                if(!reached.HasValue)continue;
                var path=reached.Value;while(true){if(!seaCells.Contains(path))seaCells.Add(path);if(path==goal)break;path=previous[path];}
            }
        }
        void SetupSteamMech()
        {
            var drivers=Units.Where(unit=>unit.Enabled&&Tagged(unit,"蒸汽")).ToArray();if(drivers.Length==0)return;
            var main=drivers.FirstOrDefault(unit=>unit.Instance==Campaign.SteamMainDriver)??drivers.First();
            float hp=drivers.Sum(unit=>unit.MaxHealth)*1.6f,atk=drivers.Max(unit=>unit.Attack)+(drivers.Sum(unit=>unit.Attack)-drivers.Max(unit=>unit.Attack))*.5f;
            float armor=drivers.Max(unit=>unit.Armor),mr=drivers.Max(unit=>unit.MagicResistance);
            foreach(var driver in drivers)
            {
                steamOriginalArts[driver.Instance]=driver.ArtId;
                if(driver!=main){State(driver).Stacks["steam_hidden"]=1;driver.Alive=false;driver.RespawnRemaining=0;driver.IsMoving=false;}
            }
            var state=State(main);foreach(var driver in drivers)state.Tags.UnionWith(State(driver).Tags);state.Stacks["steam_mech"]=1;state.Spec=FormationCampaignRules.Find("STEAM");
            main.Name="蒸汽骑士 · 原型机";main.ArtId="STEAM";main.MaxHealth=hp*(Level("蒸汽")>=3?2:1);main.Health=main.MaxHealth;
            state.BaseAttack=atk*(Level("蒸汽")>=3?2:1);state.AttackPct=0;
            state.BaseArmor=armor+(Level("蒸汽")>=2?30:0);state.BaseMR=mr+(Level("蒸汽")>=2?20:0);state.BaseSP=24;
            state.BaseInterval=2.2f;state.BaseRange=1.2f;state.BaseSpeed=.6f;main.SkillCharge=8;
            state.Stacks["patriot_hp_amount"]=drivers.Sum(driver=>Stack(driver,"patriot_hp_amount"))*1.6f*(Level("蒸汽")>=3?2:1);
            RefreshCampaignStats(main);
        }
    }
}
