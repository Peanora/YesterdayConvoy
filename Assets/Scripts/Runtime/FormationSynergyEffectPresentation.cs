using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        // These are verified original *common* status layers adapted to the new
        // game's synergies. They are not invented character-specific source VFX.
        static readonly string[] FormationSynergyEffectKeys={
            "common/common_ignite_support","common/common_char_birth_01",
            "common/common_speedup_01","common/common_ignite_defense"};
        sealed class SynergyVisualPulse
        {
            internal string Key;
            internal int Instance;
            internal double At,Until;
        }
        sealed class SynergyVisualPrevious
        {
            internal int Action;
            internal float Prompt,Rush,Renewal;
        }
        readonly Dictionary<string,SynergyVisualPulse> formationSynergyPulses=new Dictionary<string,SynergyVisualPulse>();
        readonly Dictionary<int,SynergyVisualPrevious> formationSynergyPrevious=new Dictionary<int,SynergyVisualPrevious>();
        FormationBattleModel formationSynergyObserved;
        int formationSynergyEventCursor,formationSynergyRound;
        double formationSynergyLastClock=-1;

        static IEnumerable<ArtSequence> FormationSynergyEffectSequences()
        {foreach(string key in FormationSynergyEffectKeys)yield return OriginalCombatVfxArt.Get(key).Sequence;}

        void AddFormationSynergyPulse(string key,int instance,double at)
        {
            var owner=Model.Units.FirstOrDefault(u=>u.Enabled&&u.Instance==instance);
            if(owner==null)return;
            var clip=OriginalCombatVfxArt.Get(key);
            double duration=clip.Duration/LegacyVisualRate;
            if(double.IsNaN(at)||double.IsInfinity(at)||at<0||Model.Clock-at>duration)return;
            at=Math.Min(at,Model.Clock);
            string identity=instance.ToString(CultureInfo.InvariantCulture)+"/"+key;
            if(formationSynergyPulses.TryGetValue(identity,out var active)&&at<=active.Until)
            {
                // Repeated real grants extend a native loop without restarting its
                // startup every frame. A birth one-shot finishes once; no fake loop.
                if(clip.Loop)active.Until=Math.Max(active.Until,at+duration);
                return;
            }
            formationSynergyPulses[identity]=new SynergyVisualPulse{Key=key,Instance=instance,At=at,Until=at+duration};
        }
        static bool TryFormationEventTime(string text,out double value)
        {return double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||double.TryParse(text,NumberStyles.Float,CultureInfo.CurrentCulture,out value);}

        void PresentFormationSynergyEffects()
        {
            if(Model==null||Model.Campaign==null||Model.Phase==FormationPhase.Preparation)
            {ResetFormationSynergyEffects();return;}
            if(formationSynergyObserved!=Model||formationSynergyRound!=Model.Round||Model.Clock<formationSynergyLastClock||Model.BattleEvents.Count<formationSynergyEventCursor)
            {
                ResetFormationSynergyEffects();formationSynergyObserved=Model;formationSynergyRound=Model.Round;
            }
            formationSynergyLastClock=Model.Clock;
            // List position, not timestamp/receiver, is the event identity: multiple
            // legitimate same-tick resonance or relay events remain distinct inputs.
            for(int index=formationSynergyEventCursor;index<Model.BattleEvents.Count;index++)
            {
                string raw=Model.BattleEvents[index];if(string.IsNullOrEmpty(raw))continue;
                var parts=raw.Split(':');if(parts.Length<3||!TryFormationEventTime(parts[0],out double at))continue;
                if(parts[1]=="同频"&&int.TryParse(parts[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out int leader))
                {
                    AddFormationSynergyPulse("common/common_ignite_support",leader,at);
                    AddFormationSynergyPulse("common/common_speedup_01",leader,at);
                }
                else if(parts[1]=="接棒")
                {
                    int arrow=parts[2].IndexOf("->",StringComparison.Ordinal);
                    if(arrow>=0&&int.TryParse(parts[2].Substring(arrow+2),NumberStyles.Integer,CultureInfo.InvariantCulture,out int receiver))
                    {
                        AddFormationSynergyPulse("common/common_char_birth_01",receiver,at);
                        AddFormationSynergyPulse("common/common_ignite_support",receiver,at);
                    }
                }
            }
            formationSynergyEventCursor=Model.BattleEvents.Count;
            foreach(var sample in Model.SynergyVisualSamples())
            {
                if(!formationSynergyPrevious.TryGetValue(sample.Unit.Id,out var previous))
                    formationSynergyPrevious[sample.Unit.Id]=previous=new SynergyVisualPrevious();
                bool promptIncreased=sample.Prompt>0&&(sample.Action!=previous.Action||sample.Prompt>previous.Prompt+.0001f);
                if(sample.Unit.Alive)
                {
                    if(promptIncreased&&sample.PartnerInstance>0)
                        AddFormationSynergyPulse("common/common_ignite_support",sample.PartnerInstance,Model.Clock);
                    if(sample.Rush>previous.Rush+.0001f)
                        AddFormationSynergyPulse("common/common_speedup_01",sample.Unit.Instance,Model.Clock);
                    if(sample.Renewal>previous.Renewal+.0001f)
                        AddFormationSynergyPulse("common/common_ignite_defense",sample.Unit.Instance,Model.Clock);
                }
                previous.Action=sample.Action;previous.Prompt=sample.Prompt;previous.Rush=sample.Rush;previous.Renewal=sample.Renewal;
            }
            foreach(var pair in formationSynergyPulses.ToArray())
            {
                var pulse=pair.Value;
                if(Model.Clock>pulse.Until){formationSynergyPulses.Remove(pair.Key);continue;}
                var target=Model.Units.FirstOrDefault(u=>u.Enabled&&u.Instance==pulse.Instance);
                if(target==null||!target.Alive||!FormationActorVisibleInView(target))continue;
                var original=OriginalCombatVfxArt.Get(pulse.Key);
                bool ground=pulse.Key=="common/common_char_birth_01";
                // Owner state glows follow the actual mounted display body. The
                // original birth floor cue stays on its real terrain contact point.
                LegacyOriginal(pulse.Key,(Model.Clock-pulse.At)*LegacyVisualRate,target.Position,ground?-3:34,original.Loop,
                    ground,LegacyScale(target),ground?(Vector3?)null:FormationDisplayPoint(target)+battleCamera.transform.up*.1f);
            }
        }
        void ResetFormationSynergyEffects()
        {
            formationSynergyPulses.Clear();formationSynergyPrevious.Clear();formationSynergyObserved=null;
            formationSynergyEventCursor=0;formationSynergyRound=0;formationSynergyLastClock=-1;
        }
    }

    public sealed partial class FormationBattleModel
    {
        internal sealed class SynergyVisualSample
        {
            internal FormationFighter Unit;
            internal int Action,PartnerInstance;
            internal float Prompt,Rush,Renewal;
        }
        // Pure reads: no State() lazy allocation, SP grants, damage, buff updates or
        // new event writes. Each value is already produced by the combat mechanics.
        internal IEnumerable<SynergyVisualSample> SynergyVisualSamples()
        {
            if(Campaign==null)yield break;
            foreach(var unit in Units)
            {
                if(!unit.Enabled||!combat.TryGetValue(unit.Id,out var state))continue;
                bool prompt=synergyLevels.TryGetValue("提词",out int promptTier)&&promptTier>=2&&state.Tags.Contains("提词");
                bool rush=synergyLevels.TryGetValue("迅攻",out int rushTier)&&rushTier>=4&&state.Tags.Contains("迅攻");
                var partner=prompt?PromptPartner(unit):null;
                yield return new SynergyVisualSample
                {
                    Unit=unit,Action=state.Action,PartnerInstance=partner?.Instance??0,
                    Prompt=prompt?state.PromptReturned:0,
                    Rush=rush&&state.Stacks.TryGetValue("rush",out float stacks)?stacks:0,
                    Renewal=state.Shields.Where(s=>s.Renewal&&s.Amount>0&&s.Until>Clock).Sum(s=>s.Amount)
                };
            }
        }
    }
}
