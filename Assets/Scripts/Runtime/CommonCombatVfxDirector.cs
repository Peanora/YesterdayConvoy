using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Ordinary impact families are original common assets, adapted to this game's existing events.
    // Actor-specific owners are checked first by the caller; this director never creates gameplay.
    internal sealed class CommonCombatVfxDirector:IDisposable
    {
        const string Hit="common/common_hit_01",EnemyHit="common/common_enemy_hit_01",RangerHit="common/common_ranger_hit_01";
        const string MagicHit="common/common_magic_hit_01",FireHit="common/common_firemagic_hit_01",CutHit="common/common_cuting_hit_01";
        const string HealHit="common/common_heal_hit_01",HealStart="common/common_heal_start_01";
        const string BowTrail="common/common_gongjian_trail_01",MagicTrail="common/common_magic_trail_01";
        const string Birth="common/common_char_birth_01",Death="common/common_enemy_die_01";
        const string EnemyMagicTrail="common/common_magic_trail_01_m",EnemyMagicHit="common/common_magic_hit_01_m";
        const string AttackBuff="common/common_ignite_attack",DefenseBuff="common/common_ignite_defense",SupportBuff="common/common_ignite_support";
        static readonly string[] required={Hit,EnemyHit,RangerHit,MagicHit,FireHit,CutHit,HealHit,HealStart,BowTrail,MagicTrail,Birth,Death,EnemyMagicTrail,EnemyMagicHit,AttackBuff,DefenseBuff,SupportBuff,"monster/firstar_attack_01_hit"};
        internal static IReadOnlyList<string> RequiredKeys=>Array.AsReadOnly(required);
        internal static IEnumerable<ArtSequence> SequencesFor()
        {
            foreach(string key in required)
            {
                // Only this native bow TrailRenderer has a documented capture-gap safeguard.
                // Production now includes it; never silently skip other missing keys.
                if(key==BowTrail&&!OriginalCombatVfxArt.TryGet(key,out _))continue;
                yield return OriginalCombatVfxArt.Get(key).Sequence;
            }
        }
        readonly OriginalCombatVfxLayer layer;
        readonly HashSet<(string,double)> casts=new HashSet<(string,double)>();
        internal int VisibleCount=>layer.VisibleCount;
        internal IEnumerable<SpriteRenderer> Renderers=>layer.Renderers;
        internal CommonCombatVfxDirector(Transform root){layer=new OriginalCombatVfxLayer(root);}
        internal void Begin(){layer.Begin();casts.Clear();}
        static bool OriginalMonsterOwns(string id)=>id=="CARD_WISADEL_APRIL"||id=="CARD_TALULAH_TRIAL"||id=="CARD_M20"||id=="CARD_M28"||id=="CARD_M25"||id=="CARD_M32";
        static string DefenderArt(DefenderState d)
        {
            if(d==null)return null;
            if(!string.IsNullOrEmpty(d.Settings.ArtId))return d.Settings.ArtId;
            string id=d.EntityId;
            if(d.Settings.Name=="克洛丝"&&(id=="kroos"||id.StartsWith("kroos-",StringComparison.Ordinal)&&int.TryParse(id.Substring(6),out int flank)&&flank>0))return "D07";
            return null;
        }
        static bool CommonDefender(string art)=>art=="D01"||art=="D02"||art=="D04"||art=="D06"||art=="D07"||art=="D08"||art=="D09"||art=="D10";
        readonly struct Binding
        {
            internal readonly string Trail,Hit,Cast,TargetCast;
            internal Binding(string trail,string hit,string cast=null,string targetCast=null){Trail=trail;Hit=hit;Cast=cast;TargetCast=targetCast;}
        }
        static Binding MonsterBinding(DeployedUnitState u,EngagementSettings settings,CombatShot shot)
        {
            switch(u.CardId)
            {
                case "CARD_M05":return new Binding("monster/mslime_attack_01_trail","monster/mslime_attack_01_hit");
                case "CARD_M11":return new Binding(BowTrail,Hit);
                case "CARD_M12":return new Binding("monster/snmage_attack_01_trail","monster/snmage_attack_01_hit",shot.ColdDuration>0?"monster/snmage_skill_01_start":"monster/snmage_attack_01_start");
                case "CARD_M14":return new Binding("monster/firstar_attack_01_trail","monster/firstar_attack_01_hit",u.ReviveUsed?"monster/firstar_attack_02_start":"monster/firstar_attack_01_start");
                case "CARD_M34":return new Binding(null,"monster/enemy_dslntc_attack_01_hit");
                case "CARD_M30":return new Binding(null,"monster/enemy_duskls_attack_hit","monster/enemy_duskls_attack_start");
                case "CARD_M36":return new Binding("monster/enemy_dsmant_attack_01_trail","monster/enemy_dsmant_attack_01_hit","monster/enemy_dsmant_attack_01_start");
                case "CARD_M38":return new Binding(EnemyMagicTrail,EnemyMagicHit);
                case "CARD_M16":return new Binding("monster/sotisp_attack_01_trail","monster/sotisp_attack_01_hit");
                case "CARD_M26":case "CARD_M42":return new Binding("monster/enemy_mdsnpr_attack_01_trail","monster/enemy_mdsnpr_attack_01_hit","monster/enemy_mdsnpr_attack_01_start");
            }
            // Other ordinary monsters intentionally share the verified original common family.
            // This fallback is recorded as adaptation, not a recovered actor-specific projectile.
            if(settings.Effects.DamageType==CombatDamageType.Arts)return new Binding(settings.Melee?null:MagicTrail,MagicHit);
            return new Binding(settings.Melee?null:BowTrail,u.CardId=="CARD_M37"?CutHit:settings.Melee?EnemyHit:RangerHit);
        }
        static bool TryBinding(CombatShot shot,ManualBattle battle,CardDefinition sourceCard,out Binding binding)
        {
            binding=default;
            if(shot.FireRain)return false;
            if(shot.FromDefender)
            {
                var d=battle.FindDefender(shot.SourceId);string art=DefenderArt(d);if(!CommonDefender(art))return false;
                if(shot.Healing){binding=new Binding(null,HealHit,HealStart);return true;}
                switch(art)
                {
                    case "D07":binding=new Binding(BowTrail,Hit);break;
                    case "D08":binding=shot.Empowered?new Binding("defender/D08/stward_skill_01_trail","defender/D08/stward_skill_01_hit"):new Binding(MagicTrail,MagicHit);break;
                    case "D09":binding=new Binding(null,FireHit,targetCast:"defender/D09/lava_attack_01_hit");break;
                    default:binding=new Binding(null,Hit);break;
                }
                return true;
            }
            var u=battle.FindUnit(shot.SourceId);
            if(u==null||OriginalMonsterOwns(sourceCard?.CardId??u.CardId))return false;
            // Flamerazer's beam owner consumes FlameSprays only. Its separately emitted
            // blocked/silenced ordinary CombatShots use the common hit-family adaptation.
            binding=MonsterBinding(u,battle.UnitCombat(u),shot);return true;
        }
        // true means this event is owned even in its delay, empty, cancelled or finished interval.
        // Do not use the layer's current visible-frame boolean to resurrect an old diagnostic circle.
        internal bool PresentShot(CombatShot shot,ManualBattle battle,Camera camera,double presentationClock,CardDefinition sourceCard)
        {
            if(shot==null||battle==null||!TryBinding(shot,battle,sourceCard,out var binding))return false;
            if(camera==null||shot.Erased)return true;
            var defender=shot.FromDefender?battle.FindDefender(shot.SourceId):null;
            var monster=shot.FromDefender?null:battle.FindUnit(shot.SourceId);
            double windup=defender!=null?defender.Settings.Windup/defender.LastActionRate:monster!=null?battle.UnitCombat(monster).AttackWindup/monster.LastAttackRate:0;
            double castAt=shot.StartedAt-windup;
            if(binding.Cast!=null&&casts.Add((shot.SourceId,castAt)))
                layer.ShowGround(binding.Cast,presentationClock-castAt,shot.Origin,camera,false,7);
            // A failed projectile does not revoke an already emitted caster's native tail.
            // Its cancelled flight/impact (and any not-yet-emitted target cue) stay hidden.
            if(shot.Resolved&&!shot.Applied)return true;
            double age=presentationClock-shot.StartedAt;
            if(binding.TargetCast!=null)layer.ShowGround(binding.TargetCast,age,shot.Destination,camera,false,4);
            if(age<0)return true;
            var target=shot.FromDefender?battle.FindUnit(shot.TargetId):null;
            Vector3 end=target!=null?target.Motion.Position:shot.Destination;
            // Body hit/launch anchors are explicit presentation attachments, not a scale change.
            Vector3 start=shot.Origin+camera.transform.up*.75f,hit=end+camera.transform.up*.8f;
            if(!shot.Resolved&&presentationClock<shot.ImpactAt&&binding.Trail!=null)
            {
                double flight=shot.ImpactAt-shot.StartedAt;float t=Mathf.Clamp01((float)(age/Math.Max(.001,flight)));
                if(TryTrail(binding.Trail,out var clip))
                    layer.ShowOriented(binding.Trail,age,Vector3.Lerp(start,hit,t),camera,hit-start,11,clip.Loop);
            }
            if(shot.Applied)
                layer.Show(binding.Hit,presentationClock-shot.ImpactAt,hit,camera,false,12);
            return true;
        }
        static string PuffKey(bool death,string cardId,string defenderArt)
        {
            // Defender death bodies and the five already-native monster lifecycles have their own owners.
            if(!string.IsNullOrEmpty(defenderArt)||death&&OriginalMonsterOwns(cardId)&&cardId!="CARD_WISADEL_APRIL")return null;
            return death?Death:Birth;
        }
        internal static double EventDuration(bool death,string cardId=null,string defenderArt=null)
        {string key=PuffKey(death,cardId,defenderArt);return key==null?0:OriginalCombatVfxArt.Get(key).Duration;}
        internal bool PresentPuff(Vector3 position,double age,bool death,Camera camera,string cardId=null,string defenderArt=null)
        {
            string key=PuffKey(death,cardId,defenderArt);
            if(key!=null)layer.ShowGround(key,age,position,camera,false,death?5:-1);
            return true;
        }
        internal static bool OwnsDefenderBuff(DefenderSkillKind kind)=>(int)kind>=(int)DefenderSkillKind.CardiganRecovery&&(int)kind<=(int)DefenderSkillKind.OrchidSwiftStrike;
        static string BuffKey(DefenderSkillKind kind)
        {
            switch(kind)
            {
                case DefenderSkillKind.CardiganRecovery:return HealHit;
                case DefenderSkillKind.BeagleGuard:return DefenseBuff;
                case DefenderSkillKind.SpotHealingMode:case DefenderSkillKind.HibiscusHealing:case DefenderSkillKind.AnselRange:return SupportBuff;
                case DefenderSkillKind.MelanthaStrength:case DefenderSkillKind.MidnightEnchant:case DefenderSkillKind.PlumeSwiftStrike:
                case DefenderSkillKind.LavaChant:case DefenderSkillKind.OrchidSwiftStrike:return AttackBuff;
                default:return null; // Kroos/Steward attack recovery has no invented persistent aura.
            }
        }
        internal void PresentDefenderBuffs(ManualBattle battle,Camera camera,double clock)
        {
            if(battle==null||camera==null)return;
            foreach(var d in battle.Defenders)
            {
                if(!LowStarBuff(d))continue;
                string key=BuffKey(d.Settings.Skill.Kind);double age=clock-d.SkillStartedAt;
                if(key==null||age<0||double.IsInfinity(age)||double.IsNaN(age))continue;
                var clip=OriginalCombatVfxArt.Get(key);if(clip.Loop&&(!battle.DefenderSkillActive(d)||clock>=d.SkillActiveUntil))continue;
                layer.ShowGround(key,age,d.Position,camera,false,5,clip.Loop);
            }
        }
        // These are original common/ice assets adapted to this project's new synergy rules.
        // Their model event timestamps and damage remain untouched.
        internal void PresentBattleBursts(ManualBattle battle,Camera camera,double clock)
        {
            if(battle==null||camera==null)return;
            foreach(var burst in battle.InfectionBursts)if(burst.Resolved)
                layer.ShowGround(FireHit,clock-burst.At,burst.Position,camera,false,8);
            foreach(var pulse in battle.GolemPulses)
                layer.ShowGround(CutHit,clock-pulse.At,pulse.Position,camera,false,8);
            foreach(var shatter in battle.ShatterEvents)
                layer.ShowGround("monster/firstar_attack_01_hit",clock-shatter.At,shatter.Position,camera,false,8);
        }
        static void IncludeEnd(ref double end,double began,double duration,double clock)
        {
            if(!double.IsNaN(began)&&!double.IsInfinity(began)&&began<=clock+1e-6&&duration>0)end=Math.Max(end,began+duration);
        }
        static void IncludeClip(ref double end,string key,double began,double clock)
        {if(key!=null&&!double.IsInfinity(began)&&!double.IsNaN(began)&&began<=clock+1e-6)IncludeEnd(ref end,began,OriginalCombatVfxArt.Get(key).Duration,clock);}
        static bool TryTrail(string key,out OriginalCombatVfxArt.Clip clip)
        {if(key==BowTrail)return OriginalCombatVfxArt.TryGet(key,out clip);clip=OriginalCombatVfxArt.Get(key);return true;}
        // Only already emitted, owned events contribute. There is no maximum-clip global delay.
        internal static double PendingUntil(ManualBattle battle)
        {
            if(battle==null)return double.NegativeInfinity;
            double end=double.NegativeInfinity,clock=battle.Clock;
            foreach(var shot in battle.CombatShots)
            {
                if(shot.Erased||!TryBinding(shot,battle,null,out var binding))continue;
                var d=shot.FromDefender?battle.FindDefender(shot.SourceId):null;
                var u=shot.FromDefender?null:battle.FindUnit(shot.SourceId);
                double windup=d!=null?d.Settings.Windup/d.LastActionRate:u!=null?battle.UnitCombat(u).AttackWindup/u.LastAttackRate:0;
                IncludeClip(ref end,binding.Cast,shot.StartedAt-windup,clock);
                if(shot.Resolved&&!shot.Applied)continue;
                IncludeClip(ref end,binding.TargetCast,shot.StartedAt,clock);
                if(!shot.Resolved&&binding.Trail!=null&&shot.StartedAt<=clock+1e-6)
                {
                    if(TryTrail(binding.Trail,out var clip))
                        end=Math.Max(end,clip.Loop?shot.ImpactAt:Math.Min(shot.ImpactAt,shot.StartedAt+clip.Duration));
                }
                if(shot.Applied)IncludeClip(ref end,binding.Hit,shot.ImpactAt,clock);
            }
            foreach(var burst in battle.InfectionBursts)if(burst.Resolved)IncludeClip(ref end,FireHit,burst.At,clock);
            foreach(var pulse in battle.GolemPulses)IncludeClip(ref end,CutHit,pulse.At,clock);
            foreach(var shatter in battle.ShatterEvents)IncludeClip(ref end,"monster/firstar_attack_01_hit",shatter.At,clock);
            foreach(var u in battle.Units)
            {
                IncludeEnd(ref end,u.SpawnTime,EventDuration(false,u.CardId),clock);
                if(u.Resolution==UnitResolution.Dead)IncludeEnd(ref end,u.DeathTime,EventDuration(true,u.CardId),clock);
            }
            foreach(var d in battle.Defenders)
            {
                string key=BuffKey(d.Settings.Skill.Kind);
                if(key==null||!LowStarBuff(d)||double.IsInfinity(d.SkillStartedAt)||double.IsNaN(d.SkillStartedAt)||d.SkillStartedAt>clock+1e-6)continue;
                var clip=OriginalCombatVfxArt.Get(key);
                // A looping buff stops with its owner/state. It never extends terminal time indefinitely.
                if(!clip.Loop&&d.Alive)IncludeEnd(ref end,d.SkillStartedAt,clip.Duration,clock);
            }
            return end;
        }
        static bool LowStarBuff(DefenderState d)=>d!=null&&d.Alive&&!string.IsNullOrEmpty(d.Settings.ArtId)&&d.Settings.ArtId.Length==3&&d.Settings.ArtId[0]=='D'&&
            int.TryParse(d.Settings.ArtId.Substring(1),out int artNumber)&&artNumber>=1&&artNumber<=12;
        internal void End()=>layer.End();
        internal void Clear(){layer.Clear();casts.Clear();}
        public void Dispose()=>layer.Dispose();
    }
}
