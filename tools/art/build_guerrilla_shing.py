"""Rebuild the original, sample-free Guerrilla Order metallic flash sound.

Requires only Python 3.10+ standard library. This is a designed approximation of
the requested short "sing/shing" cue, not extracted audio from another game.
Run from any directory: py tools/art/build_guerrilla_shing.py
"""

from __future__ import annotations

import argparse
import array
import hashlib
import json
import math
import random
import sys
import wave
from pathlib import Path


RATE = 48_000
DURATION = 0.64
PEAK_DBFS = -6.0
SEED = 20261106


def smoothstep(value: float) -> float:
    value = min(1.0, max(0.0, value))
    return value * value * (3.0 - 2.0 * value)


def synthesize() -> tuple[array.array, dict]:
    """Layer a quick airy swipe, an inharmonic metal ring and a quiet shimmer."""
    rng = random.Random(SEED)
    frames = round(RATE * DURATION)
    channels: tuple[list[float], list[float]] = ([], [])
    # Non-harmonic resonances suggest struck metal rather than a musical chord.
    modes = ((1840.0, 1.0, 0.122), (2916.0, 0.39, 0.085),
             (4265.0, 0.23, 0.066), (6323.0, 0.12, 0.040))
    low_noise = band_noise = 0.0
    highpass_alpha = math.exp(-2.0 * math.pi * 1300.0 / RATE)
    lowpass_alpha = 1.0 - math.exp(-2.0 * math.pi * 7600.0 / RATE)
    for frame in range(frames):
        t = frame / RATE
        attack = smoothstep(t / 0.004)
        end_fade = 1.0 - smoothstep((t - (DURATION - 0.08)) / 0.08)
        noise = rng.uniform(-1.0, 1.0)
        low_noise = highpass_alpha * low_noise + (1.0 - highpass_alpha) * noise
        high_noise = noise - low_noise
        band_noise += lowpass_alpha * (high_noise - band_noise)
        swipe = band_noise * (1.0 - math.exp(-t / 0.0015)) * math.exp(-t / 0.025)
        for channel in range(2):
            # Subtle detuning and a low tail offset keep a coherent center image.
            detune = 1.0 + (-0.0008 if channel == 0 else 0.0008)
            metal = 0.0
            for index, (frequency, weight, decay) in enumerate(modes):
                # Integrate a short upward pitch edge; the ring settles in 25ms.
                phase = 2.0 * math.pi * detune * frequency * (
                    t - 0.07 * 0.016 * (1.0 - math.exp(-t / 0.016)))
                modulation = 0.65 * math.exp(-t / 0.022) * math.sin(
                    2.0 * math.pi * (417.0 + index * 31.0) * t)
                metal += weight * math.sin(phase + modulation) * math.exp(-t / decay)
            # A restrained falling overtone gives the start its blade-like "sh".
            slide_phase = 2.0 * math.pi * (
                2050.0 * t + 3200.0 * 0.021 * (1.0 - math.exp(-t / 0.021)))
            slide = math.sin(slide_phase + channel * 0.018) * math.exp(-t / 0.043)
            # Quiet late shimmer: no long reverberant or ambient bed.
            tail = 0.075 * math.sin(2.0 * math.pi * 3750.0 * detune * t)
            tail *= math.exp(-t / 0.155) * (1.0 + 0.3 * math.sin(2.0 * math.pi * 31.0 * t))
            sample = (0.72 * metal + 0.19 * slide + 0.48 * swipe + tail)
            channels[channel].append(sample * attack * end_fade)
    # Remove the tiny finite-window DC offset, retaining exact silent endpoints.
    for data in channels:
        mean = sum(data) / frames
        for frame in range(frames):
            t = frame / RATE
            boundary = smoothstep(t / 0.004) * (
                1.0 - smoothstep((t - (DURATION - 0.08)) / 0.08))
            data[frame] -= mean * boundary
        data[0] = data[-1] = 0.0
    raw_peak = max(abs(sample) for data in channels for sample in data)
    gain = (10.0 ** (PEAK_DBFS / 20.0)) / raw_peak
    pcm = array.array("h")
    for frame in range(frames):
        for channel in range(2):
            pcm.append(round(channels[channel][frame] * gain * 32767.0))
    if sys.byteorder != "little":
        pcm.byteswap()

    amplitudes = [sample / 32768.0 for sample in pcm]
    peak = max(abs(value) for value in amplitudes)
    rms = math.sqrt(sum(value * value for value in amplitudes) / len(amplitudes))
    report = {
        "sample_rate_hz": RATE,
        "channels": 2,
        "sample_format": "signed 16-bit little-endian PCM",
        "frames": frames,
        "duration_seconds": frames / RATE,
        "sample_peak_dbfs": round(20.0 * math.log10(peak), 4),
        "rms_dbfs": round(20.0 * math.log10(rms), 4),
        "clipped_samples": sum(abs(sample) >= 32767 for sample in pcm),
        "first_frame": list(pcm[:2]),
        "last_frame": list(pcm[-2:]),
        "deterministic_seed": SEED,
    }
    return pcm, report


def write_if_changed(path: Path, content: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != content:
        path.write_bytes(content)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    root = args.project_root.resolve()
    relative = "Assets/Audio/SFX/Resources/GuerrillaOrder/shing.wav"
    output = root / relative
    pcm, report = synthesize()
    output.parent.mkdir(parents=True, exist_ok=True)
    # wave writes fixed PCM headers and no timestamps, so output is reproducible.
    with wave.open(str(output), "wb") as stream:
        stream.setnchannels(2)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(pcm.tobytes())
    report.update({
        "path": relative,
        "bytes": output.stat().st_size,
        "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "source": "Original procedural synthesis from mathematical oscillators and seeded noise; no external audio samples",
        "purpose": "One short metallic sing/shing cue for approved Guerrilla Order full-screen cut-in",
        "generator": "tools/art/build_guerrilla_shing.py",
        "creation_date": "2026-11-06",
        "rights": "Project-authored procedural sound; no third-party recording or game audio sampled. Does not assert rights in referenced character art.",
        "verification": "PCM metadata, endpoint silence, clipping and peak/RMS measured by this generator. ffprobe/ffmpeg verification is performed separately.",
        "listening_status": "Not auditioned by the authoring agent; subjective timbre and final in-game balance require listening feedback.",
        "reference_limit": "Currency Wars reference video was not successfully played; not claimed to replicate its exact sound.",
    })
    record = root / "docs/art/source-records/guerrilla-shing.json"
    write_if_changed(record, (json.dumps(report, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
