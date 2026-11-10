using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class CombatFeedbackBubble
    {
        public readonly CombatFeedbackTarget TargetType;
        public readonly string TargetId;
        public readonly CombatFeedbackKind Kind;
        public readonly CombatFeedbackStatus Status;
        public readonly double StartedAt;
        public readonly long Bucket;
        public Vector3 WorldPosition {get;internal set;}
        public float Amount {get;internal set;}
        internal CombatFeedbackBubble(CombatFeedbackEvent e)
        {TargetType=e.TargetType;TargetId=e.TargetId;Kind=e.Kind;Status=e.Status;StartedAt=e.At;Bucket=(long)Math.Floor(e.At/CombatFeedbackPresentation.MergeWindow);WorldPosition=e.WorldPosition;Amount=e.Amount;}
    }

    /// <summary>Consumes applied results only. Does not know armor, attack, skill or HP calculation rules.</summary>
    public sealed class CombatFeedbackPresentation
    {
        public const int MaximumVisible=96;
        public const double MergeWindow=.15;
        public const double Lifetime=1.05;
        readonly List<CombatFeedbackBubble> bubbles=new List<CombatFeedbackBubble>(MaximumVisible);
        readonly ReadOnlyCollection<CombatFeedbackBubble> visible;
        ManualBattle bound;
        int run;
        long consumed;
        public IReadOnlyList<CombatFeedbackBubble> Visible=>visible;
        public long ConsumedSequence=>consumed;
        public CombatFeedbackPresentation(){visible=bubbles.AsReadOnly();}
        public void Clear(){bound=null;run=0;consumed=0;bubbles.Clear();}
        public void Sync(ManualBattle battle,double presentationClock)
        {
            if(battle==null){Clear();return;}
            if(!ReferenceEquals(bound,battle)||run!=battle.RunId)
            {bound=battle;run=battle.RunId;consumed=0;bubbles.Clear();}
            for(int i=bubbles.Count-1;i>=0;i--)
                if(presentationClock-bubbles[i].StartedAt>=Lifetime)bubbles.RemoveAt(i);
            foreach(var e in battle.CombatFeedback)
            {
                if(e.Sequence<=consumed)continue;consumed=e.Sequence;
                // A bounded journal may have overrun while hidden. Never replay stale combat.
                if(presentationClock-e.At>=Lifetime)continue;
                long bucket=(long)Math.Floor(e.At/MergeWindow);CombatFeedbackBubble match=null;
                foreach(var b in bubbles)
                    if(b.TargetType==e.TargetType&&b.TargetId==e.TargetId&&b.Kind==e.Kind&&b.Status==e.Status&&b.Bucket==bucket){match=b;break;}
                if(match!=null){match.Amount+=e.Amount;match.WorldPosition=e.WorldPosition;continue;}
                if(bubbles.Count==MaximumVisible)bubbles.RemoveAt(0);
                bubbles.Add(new CombatFeedbackBubble(e));
            }
        }
        public static string StatusText(CombatFeedbackStatus status)
        {
            switch(status)
            {
                case CombatFeedbackStatus.Cold:return "寒冷";
                case CombatFeedbackStatus.Frozen:return "冻结";
                case CombatFeedbackStatus.Hindered:return "停顿";
                case CombatFeedbackStatus.Fear:return "恐惧";
                case CombatFeedbackStatus.Stunned:return "眩晕";
                case CombatFeedbackStatus.Silenced:return "沉默";
                case CombatFeedbackStatus.Burning:return "灼烧";
                case CombatFeedbackStatus.Neural:return "神经损伤";
                case CombatFeedbackStatus.Vulnerable:return "脆弱";
                case CombatFeedbackStatus.Shackled:return "束缚";
                case CombatFeedbackStatus.Lured:return "诱导";
                case CombatFeedbackStatus.Shielded:return "护盾";
                case CombatFeedbackStatus.Ruptured:return "破阵";
                case CombatFeedbackStatus.Exposed:return "暴露";
                case CombatFeedbackStatus.Frenzy:return "狂热";
                case CombatFeedbackStatus.ArcanePulse:return "秘术脉冲";
                case CombatFeedbackStatus.Bulwark:return "壁垒掩护";
                case CombatFeedbackStatus.Coordination:return "协同接续";
                default:return string.Empty;
            }
        }
        public static string AmountText(CombatFeedbackBubble bubble)
        {
            if(bubble.Kind==CombatFeedbackKind.StatusApplied)return StatusText(bubble.Status);
            float value=bubble.Amount;
            // Keep tiny real periodic heals visible without rounding +0 or pretending they healed +1.
            string number=value<.1f?"<0.1":value<10?value.ToString("0.#"):Math.Round(value,MidpointRounding.AwayFromZero).ToString("0");
            if(bubble.Kind==CombatFeedbackKind.SkillPointsRestored)return "+"+number+" SP";
            return bubble.Kind==CombatFeedbackKind.HealthRestored?"+"+number:
                bubble.Kind==CombatFeedbackKind.ShieldAbsorbed?"护盾 "+number:"−"+number;
        }
        public static Color ColorFor(CombatFeedbackKind kind,CombatFeedbackStatus status=CombatFeedbackStatus.None)
        {
            if(kind==CombatFeedbackKind.SkillPointsRestored||status==CombatFeedbackStatus.Coordination)return new Color(.72f,.65f,1);
            if(status==CombatFeedbackStatus.Frenzy||status==CombatFeedbackStatus.Ruptured)return new Color(1,.58f,.30f);
            if(status==CombatFeedbackStatus.ArcanePulse)return new Color(.79f,.64f,1);
            if(kind==CombatFeedbackKind.HealthLost)return new Color(1,.47f,.29f);
            if(kind==CombatFeedbackKind.HealthRestored)return new Color(.48f,1,.58f);
            if(kind==CombatFeedbackKind.ShieldAbsorbed||status==CombatFeedbackStatus.Shielded)return new Color(.35f,.92f,1);
            if(status==CombatFeedbackStatus.Burning)return new Color(1,.58f,.27f);
            if(status==CombatFeedbackStatus.Fear||status==CombatFeedbackStatus.Silenced)return new Color(.84f,.69f,1);
            return new Color(.57f,.86f,1);
        }
    }
}
