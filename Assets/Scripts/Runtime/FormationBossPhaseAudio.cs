using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const int BossPhaseVoiceCount = 4;
        const float BossMusicVolume = .42f;
        const float SnowBossMusicUnderVoice = .23f;
        const float SupernovaMusicUnderVoice = .12f;

        readonly System.Random bossPhaseVoiceRandom = new System.Random();
        readonly List<int> bossPhaseVoiceStarts = new List<int>(2);
        readonly int[] bossPhaseVoiceOrder = new int[2];
        readonly AudioClip[] bossPhaseVoiceClips = new AudioClip[BossPhaseVoiceCount];
        FormationBattleModel bossPhaseAudioBattle;
        FormationDefender bossPhaseAudioGuard;
        AudioSource bossPhaseMusic, bossPhaseVoice;
        int lastBossVoicePhase;
        bool bossPhaseAudioPaused;

        internal IReadOnlyList<int> BossPhaseVoiceStarts => bossPhaseVoiceStarts;
        internal AudioSource BossPhaseMusic => bossPhaseMusic;
        internal AudioSource BossPhaseVoice => bossPhaseVoice;

        void UpdateBossPhaseAudio()
        {
            if (!CombatViewActive)
            {
                if (bossPhaseAudioBattle != null) StopBossPhaseAudio();
                return;
            }

            if (bossPhaseAudioBattle != Model)
            {
                StopBossPhaseAudio();
                bossPhaseAudioBattle = Model;
                bossPhaseAudioGuard = Model.Defenders.FirstOrDefault(guard => guard.IsTwoPhaseBoss);
                if (bossPhaseAudioGuard == null) return;

                bool supernova = bossPhaseAudioGuard.IsSupernovaBoss;
                string root = supernova ? "SupernovaBoss/" : SnowBossAudioRoot;
                for (int i = 0; i < BossPhaseVoiceCount; i++)
                    bossPhaseVoiceClips[i] = Resources.Load<AudioClip>(root + "voice-" + (i + 1));
                bossPhaseVoiceOrder[0] = bossPhaseVoiceRandom.Next(BossPhaseVoiceCount);
                bossPhaseVoiceOrder[1] = bossPhaseVoiceRandom.Next(BossPhaseVoiceCount - 1);
                if (bossPhaseVoiceOrder[1] >= bossPhaseVoiceOrder[0]) bossPhaseVoiceOrder[1]++;
                bossPhaseVoiceStarts.Clear();
                lastBossVoicePhase = 0;

                bossPhaseMusic ??= CreateBossAudioSource(loop: true, volume: BossMusicVolume);
                bossPhaseVoice ??= CreateBossAudioSource(loop: false, volume: .86f);
                bossPhaseMusic.clip = Resources.Load<AudioClip>(root + (supernova ? "arsonist" : "battle"));
                bossPhaseMusic.volume = BossMusicVolume;
                bossPhaseMusic.mute = bossPhaseVoice.mute = !soundEnabled;
                if (bossPhaseMusic.clip != null) bossPhaseMusic.Play();
            }

            if (bossPhaseAudioGuard == null) return;
            bossPhaseMusic.mute = bossPhaseVoice.mute = !soundEnabled;
            if (Model.Paused != bossPhaseAudioPaused)
            {
                bossPhaseAudioPaused = Model.Paused;
                if (bossPhaseAudioPaused)
                {
                    bossPhaseMusic.Pause();
                    bossPhaseVoice.Pause();
                }
                else
                {
                    bossPhaseMusic.UnPause();
                    bossPhaseVoice.UnPause();
                }
            }
            if (bossPhaseAudioPaused) return;

            float voiceMusicVolume = bossPhaseAudioGuard.IsSupernovaBoss ? SupernovaMusicUnderVoice : SnowBossMusicUnderVoice;
            int phase = bossPhaseAudioGuard.BossHealthPhase;
            if (phase != lastBossVoicePhase && phase >= 1 && phase <= bossPhaseVoiceOrder.Length)
            {
                lastBossVoicePhase = phase;
                int index = bossPhaseVoiceOrder[phase - 1];
                bossPhaseVoice.clip = bossPhaseVoiceClips[index];
                if (bossPhaseVoice.clip != null)
                {
                    bossPhaseVoiceStarts.Add(index + 1);
                    bossPhaseVoice.Play();
                    bossPhaseMusic.volume = voiceMusicVolume;
                }
            }
            float musicVolume = bossPhaseVoice.isPlaying ? voiceMusicVolume : BossMusicVolume;
            bossPhaseMusic.volume = Mathf.MoveTowards(bossPhaseMusic.volume, musicVolume, Time.unscaledDeltaTime * 2f);
        }

        void StopBossPhaseAudio()
        {
            if (bossPhaseMusic != null) bossPhaseMusic.Stop();
            if (bossPhaseVoice != null) bossPhaseVoice.Stop();
            bossPhaseAudioBattle = null;
            bossPhaseAudioGuard = null;
            bossPhaseAudioPaused = false;
        }

        AudioSource CreateBossAudioSource(bool loop, float volume)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.loop = loop;
            source.volume = volume;
            return source;
        }
    }
}
