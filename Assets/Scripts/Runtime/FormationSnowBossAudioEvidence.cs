using System;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Installed only on the evidence Player's own listener. The callback does not alter the mix.
    internal sealed class FormationSnowBossAudioEvidence : MonoBehaviour
    {
        readonly object gate = new object();
        short[] pcm;
        int channels, count, sampleRate;
        long firstBlockUtcTicks;

        internal void Begin()
        {
            sampleRate = AudioSettings.outputSampleRate;
            lock (gate)
                pcm = new short[checked(sampleRate * 2 * 165)];
        }

        void OnAudioFilterRead(float[] input, int channelCount)
        {
            lock (gate)
            {
                if (pcm == null || channelCount <= 0 || count + input.Length > pcm.Length) return;
                if (count == 0)
                {
                    channels = channelCount;
                    firstBlockUtcTicks = DateTime.UtcNow.Ticks;
                }
                if (channelCount != channels) return;
                for (int i = 0; i < input.Length; i++)
                    pcm[count + i] = (short)(Math.Max(-1f, Math.Min(1f, input[i])) * short.MaxValue);
                count += input.Length;
            }
        }

        internal long Save(string path, out double seconds)
        {
            short[] snapshot;
            lock (gate)
            {
                snapshot = pcm;
                pcm = null;
            }
            seconds = count / (double)(Math.Max(1, channels) * sampleRate);
            if (snapshot == null || count == 0) return 0;
            using var writer = new BinaryWriter(File.Create(path));
            writer.Write(0x46464952); // RIFF
            writer.Write(36 + count * sizeof(short));
            writer.Write(0x45564157); // WAVE
            writer.Write(0x20746d66); // fmt
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * sizeof(short));
            writer.Write((short)(channels * sizeof(short)));
            writer.Write((short)16);
            writer.Write(0x61746164); // data
            writer.Write(count * sizeof(short));
            var bytes = new byte[count * sizeof(short)];
            Buffer.BlockCopy(snapshot, 0, bytes, 0, bytes.Length);
            writer.Write(bytes);
            return firstBlockUtcTicks;
        }
    }
}
