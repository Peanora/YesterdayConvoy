using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignRun
    {
        public string HeroAugment => augments.FirstOrDefault(id=>FormationBalanceV09.Heroes.Any(hero=>hero.Id==id));
        internal FormationCampaignRules.DataRecord[] EligibleHeroes(string tier,int plane) =>
            FormationBalanceV09.Active==FormationBalanceV09.Variant.Baseline || HeroAugment!=null || plane>2
            ? Array.Empty<FormationCampaignRules.DataRecord>()
            : FormationBalanceV09.Heroes.Where(hero=>hero.Tier==tier && (plane==2 || FormationCampaignRules.Find(hero.Extra).Cost<3)
                && shop.Bench.Any(piece=>piece.Id==FormationCampaignRules.Find(hero.Extra).CardId)).ToArray();
        internal bool ApplyHero(string id)
        {
            var hero=FormationBalanceV09.Heroes.FirstOrDefault(entry=>entry.Id==id);
            if(hero==null || FormationBalanceV09.Active==FormationBalanceV09.Variant.Baseline || HeroAugment!=null)return false;
            augments.Add(id); Give(hero.Extra);
            shop.HeroRecruitId=FormationCampaignRules.Find(hero.Extra).CardId;
            shop.HeroRefreshes=shop.HeroGuarantees=0;
            return true;
        }
    }
    public sealed partial class FormationBattleModel
    {
        FormationFighter HeroCarry=>Campaign?.HeroAugment is string id ? Units.FirstOrDefault(unit=>unit.Enabled&&unit.Alive&&Hero(unit,id)) : null;
        float HeroPromotion(FormationFighter actor)
        {
            string id=Campaign?.HeroAugment;
            if(actor.Star<3||id==null||!Hero(actor,id))return 1;
            int cost=Spec(actor).Cost;
            return FormationBalanceV09.Hero(1,cost==1?2f:cost==2?1.5f:1.1f);
        }
        bool Hero(FormationActor actor,string id)
        {
            if(FormationBalanceV09.Active==FormationBalanceV09.Variant.Baseline || Campaign?.HasAugment(id)!=true ||
                !(actor is FormationFighter fighter) || Summons.Contains(fighter) || IsHiddenDriver(fighter))return false;
            var definition=FormationBalanceV09.Heroes.First(entry=>entry.Id==id);
            return Key(actor)==definition.Extra && Units.Where(unit=>unit.Enabled&&Spec(unit)?.Id==definition.Extra)
                .OrderBy(unit=>unit.Instance).FirstOrDefault()==actor;
        }
        float HeroRequired(FormationActor actor,float original)
        {
            if(Hero(actor,"H11")||Hero(actor,"H13"))return FormationBalanceV09.Hero(original,2);
            if(Hero(actor,"HB22"))return FormationBalanceV09.Hero(original,12);
            if(Hero(actor,"H31"))return FormationBalanceV09.Hero(original,18);
            return original;
        }
        float HeroSpeed(FormationActor actor) => (Hero(actor,"H21")&&(actor.Health<=actor.MaxHealth*.6f||Buff(actor,"hero_leech")>0)?FormationBalanceV09.Hero(0,.6f):0)
            +(Hero(actor,"H27")&&State(actor).Shields.Any(shield=>shield.Source==actor&&shield.Key=="native"&&shield.Amount>0&&shield.Until>Clock)?FormationBalanceV09.Hero(0,.25f):0)
            +(Hero(actor,"HB22")?Mathf.Min(10,State(actor).Casts)*FormationBalanceV09.Hero(0,.08f):0);
        bool HeroFocusExpired(FormationActor actor)=>Clock-Math.Max(Stack(actor,"hero_attack_at"),Stack(actor,"hero_skill_until"))>2;
        void HeroSkillFocus(FormationFighter actor,FormationDefender target,FormationPresentationAction action)
        {
            if(!Hero(actor,"H16")||target==null)return;
            var state=State(actor);
            if(Stack(actor,"hero_target")!=target.Id||HeroFocusExpired(actor))state.Stacks["hero_focus"]=0;
            state.Stacks["hero_target"]=target.Id;state.Stacks["hero_skill_until"]=(float)action.EndAt;
        }
        void HeroOrdinary(FormationFighter actor,FormationDefender target)
        {
            if(Hero(actor,"H16"))
            {
                var state=State(actor);
                if(Stack(actor,"hero_target")!=target.Id || HeroFocusExpired(actor))state.Stacks["hero_focus"]=0;
                state.Stacks["hero_target"]=target.Id;state.Stacks["hero_attack_at"]=(float)Clock;
                AddStack(actor,"hero_focus",1,5);
            }
        }
        void HeroAfterOrdinary(FormationFighter actor,FormationDefender target)
        {
            if(!Hero(actor,"H11")||!target.Alive)return;
            if(Buff(target,"cold")>0||Buff(target,"freeze")>0||Buff(target,"hero_frost")>0)
                CombatDamage(actor,target,actor.Attack*FormationBalanceV09.Hero(0,.6f),1,false,false);
            GiveBuff(target,"hero_frost",1,4);
        }
        float HeroShieldMultiplier(FormationFighter actor,FormationActor target,bool skill) =>
            Key(actor)!="M42"||!skill ? 1 :
            (target.Shield>0?(Hero(actor,"H42")?FormationBalanceV09.Hero(1.25f,1.6f):1.25f):1);
        void HeroAbsorb(FormationActor source,FormationActor target,CombatShield shield,float absorbed)
        {
            if(source is FormationDefender && Hero(target,"H27") && shield.Source==target&&shield.Key=="native"&&!shield.Renewal)
                AddStack(target,"hero_shield",absorbed*FormationBalanceV09.Hero(0,.25f),target.MaxHealth*.1f);
        }
        void HeroLeech(FormationActor source,FormationActor target,float healthLoss,bool native)
        {
            if(!native||healthLoss<=0||!source.Alive||!(target is FormationDefender)||!Hero(source,"H21")||Buff(source,"hero_leech")==0)return;
            var state=State(source);int second=(int)Math.Floor(Clock);
            if(Stack(source,"hero_leech_second")!=second){state.Stacks["hero_leech_second"]=second;state.Stacks["hero_leech_used"]=0;}
            float heal=Mathf.Min(healthLoss*FormationBalanceV09.Hero(0,.3f),Mathf.Max(0,source.MaxHealth*.08f-Stack(source,"hero_leech_used")));
            AddStack(source,"hero_leech_used",heal);CombatHeal(source,(FormationFighter)source,heal,false,false);
        }
        void ClearHeroLife(FormationActor actor)
        {
            var state=State(actor);state.Stacks["hero_shield"]=state.Stacks["hero_focus"]=0;
            state.Stacks["hero_skill_until"]=0;
            state.Buffs.Remove("hero_leech");
        }
    }
}
