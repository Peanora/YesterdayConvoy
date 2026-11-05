using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace YesterdayConvoy
{
    internal sealed class LocalMonsterArt
    {
        static readonly Dictionary<string,LocalMonsterArt> cache=new Dictionary<string,LocalMonsterArt>();
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        ArtCatalog catalog;
        internal Sprite Portrait=>catalog.Portrait;
        internal float ReferenceSpeed=>catalog.Speed;
        internal float Height=>catalog.Height;
        internal bool HasAttack=>clips.ContainsKey("attack");
        internal bool HasClip(string key)=>clips.ContainsKey(key);
        internal float DeathDuration=>clips["death"].Duration;
        internal Clip this[string key]=>clips[key];
        internal IEnumerable<ArtSequence> Sequences=>catalog.Clips.Values;
        internal sealed class Clip
        {
            internal ArtSequence Sequence;
            internal Sprite[] Frames=>Sequence.Frames;
            internal float Duration=>Sequence.Duration;
            internal Sprite At(double time,bool loop,out int index)
            {time=Math.Max(0,time);if(loop)time%=Duration;index=Mathf.Clamp((int)(time/Duration*Sequence.Count+1e-5),0,Sequence.Count-1);return Frames[index];}
        }
        internal static LocalMonsterArt For(string cardId)
        {
            if(cardId==null||cardId!=CampaignBalance.GolemCardId&&!cardId.StartsWith("CARD_M",StringComparison.Ordinal))return null;
            string task=cardId==CampaignBalance.GolemCardId?"GOLEM":cardId.Substring(5);
            if(task!="GOLEM"&&(task.Length!=3||!int.TryParse(task.Substring(1),out int number)||number<2||number>38))return null;
            if(cache.TryGetValue(task,out var prior))return prior;
            try
            {
                var catalog=ArtCatalog.Read("MonsterCombatArt/"+task);
                string[] extraKeys=task=="M14" ? new[]{"extra_skill_1"} : task=="M28" ? new[]{"extra_attack_2","extra_skill_1","extra_skill_2"} :
                    task=="M20" ? new[]{"extra_attack_2","extra_idle_2","extra_move_2","extra_skill","extra_revive_1","extra_revive_2","extra_revive_3"} :
                    task=="M19" ? new[]{"extra_attack_2","extra_die_2","extra_idle_2","extra_move_2"} :
                    task=="M38" ? new[]{"extra_skill_begin","extra_skill_loop","extra_skill_end"} :
                    task=="M36" ? new[]{"extra_attack_2","extra_skill_begin","extra_skill_loop","extra_skill_end"} :
                    task=="M34" ? new[]{"extra_skill_begin","extra_skill_idle","extra_skill_move"} :
                    task=="M32" ? new[]{"extra_c1_attack","extra_c1_idle","extra_c1_move","extra_c1_skill_2","extra_c1_skill_1_begin","extra_c1_skill_1_end","extra_c1_skill_1_loop","extra_c2_attack_1","extra_c2_die_1","extra_c2_idle_1","extra_c2_move_1","extra_c2_skill_2","extra_c2_skill_3","extra_c2_skill_1_begin","extra_c2_skill_1_end","extra_c2_skill_1_loop","extra_c2_stun","extra_reborn"} :
                    task=="M31" ? new[]{"extra_skill_down_start","extra_skill_down_loop","extra_skill_down_end","extra_skill_front_start","extra_skill_front_loop","extra_skill_front_end","extra_skill_up_start","extra_skill_up_loop","extra_skill_up_end"} :
                    task=="M30" ? new[]{"extra_die_2","extra_idle_2","extra_move_2","extra_revive"} : Array.Empty<string>();
                catalog.Extras(extraKeys);
                foreach(string required in new[]{"idle","walk","death"})if(!catalog.Clips.ContainsKey(required))throw new InvalidDataException("Missing monster action: "+required);
                var art=new LocalMonsterArt{catalog=catalog};foreach(var pair in catalog.Clips)art.clips.Add(pair.Key,new Clip{Sequence=pair.Value});
                cache.Add(task,art);return art;
            }
            catch(Exception e){Debug.LogWarning("MONSTER_LOCAL_ART_UNAVAILABLE: "+task+"; "+e.Message);return null;}
        }
    }
}
