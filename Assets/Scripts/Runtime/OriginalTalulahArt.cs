using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    internal sealed class OriginalTalulahArt
    {
        internal const string CardId="CARD_TALULAH_TRIAL";
        static OriginalTalulahArt cached;
        static readonly string[] RuntimeClips={"idle","walk","attack","cast","death"};
        static readonly string[] PhaseKeys={"idle-b","walk-b","attack-b","cast-b","first-fall","first-return","final-fall","final-end"};
        static readonly string[] PhaseSources={"extra_idle_b","extra_move_b","extra_attack_b","extra_skill_1_b","extra_reborn_1_start","extra_reborn_1_end","extra_reborn_2_start","extra_reborn_2_end"};
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        ArtCatalog catalog;
        internal Sprite Portrait=>catalog.Portrait;
        internal float ReferenceSpeed=>catalog.Speed;
        internal float AttackContact=>catalog.Contact;
        internal float DeathDuration=>clips["death"].Duration;
        internal float DeathDurationFor(DeployedUnitState unit)=>unit!=null&&unit.TalulahPhase==TalulahPhase.Dead?this["final-end"].Duration:DeathDuration;
        // The normal preparation path still warms only the five existing clips.
        // The controller explicitly adds this list when a three-star Talulah is present.
        internal IEnumerable<ArtSequence> SecondPhaseSequences
        {
            get { EnsureSecondPhase();foreach(string key in PhaseKeys)yield return clips[key].Sequence; }
        }
        internal static bool VisualFlip(ManualBattle battle,DeployedUnitState unit,Camera camera,bool previous)
        {
            if(camera==null||!unit.Active||battle.IsPaused||battle.IsSkillActionPlaying(unit)||battle.TalulahTransitionLocked(unit))return previous;
            var target=battle.FindDefender(unit.CombatTargetId);
            if(!unit.IsMoving&&target==null)return previous;
            Vector3 direction=unit.Motion.Direction;
            // Match simulation precedence: lure can move either way along the
            // fixed route, and owns the action before fear is considered.
            if(unit.IsMoving&&!battle.UnitControlImmune(unit))
            {
                if(unit.LureUntil>battle.Clock)
                {if(unit.Motion.DistanceTravelled>unit.LureDistance)direction=-direction;}
                else if(unit.FearUntil>battle.Clock)direction=-direction;
            }
            if(!unit.IsMoving&&target!=null)direction=target.Position-unit.Motion.Position;
            float dx=Vector3.Dot(direction,camera.transform.right);return Mathf.Abs(dx)>.08f?dx<0:previous;
        }
        void EnsureSecondPhase()
        {
            if(clips.ContainsKey("idle-b"))return;
            catalog.Extras(PhaseSources);
            for(int i=0;i<PhaseKeys.Length;i++)
            {
                var sequence=catalog.Clips[PhaseSources[i]];sequence.Name="TalulahOriginal/"+PhaseKeys[i];
                clips.Add(PhaseKeys[i],new Clip{Sequence=sequence});
            }
        }
        internal IEnumerable<ArtSequence> Sequences
        {
            get { foreach(string key in RuntimeClips)yield return clips[key].Sequence; }
        }
        internal Clip this[string key]
        {get{if(!clips.ContainsKey(key))EnsureSecondPhase();return clips[key];}}
        internal sealed class Clip
        {
            internal ArtSequence Sequence;
            internal Sprite[] Frames=>Sequence.Frames;
            internal float Duration=>Sequence.Duration;
            internal Sprite At(double time,bool loop,out int index)
            {time=Math.Max(0,time);if(loop)time%=Duration;index=Mathf.Clamp((int)(time/Duration*Sequence.Count+1e-5),0,Sequence.Count-1);return Frames[index];}
        }
        internal static bool Available(CardDefinition card)=>card!=null&&card.CardId==CardId&&For(card)!=null;
        internal static Sprite BattleSprite(CardDefinition card)=>For(card)?["idle"].Frames[0]??card.BattleSprite;
        internal static Sprite CardPortrait(CardDefinition card)=>For(card)?.Portrait??card.Portrait;
        internal static OriginalTalulahArt For(CardDefinition card)
        {
            if(card==null||card.CardId!=CardId)return null;
            if(cached!=null)return cached;
            try
            {
                var art=new OriginalTalulahArt{catalog=ArtCatalog.Read("TalulahOriginal")};
                // Phase metadata is registered on demand; no B atlas is decoded for a one/two-star card.
                art.catalog.Extras(new[]{"death"});
                foreach(string key in RuntimeClips){var sequence=art.catalog.Clips[key];sequence.Name="TalulahOriginal/"+key;art.clips.Add(key,new Clip{Sequence=sequence});}
                cached=art;return art;
            }
            catch(Exception e){Debug.LogWarning("TALULAH_ORIGINAL_UNAVAILABLE: "+e.Message);return null;}
        }
    }
}
