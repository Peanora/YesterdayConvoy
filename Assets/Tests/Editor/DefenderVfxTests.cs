using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderVfxTests
    {
        [TestCase("snow-back")][TestCase("snow-front")][TestCase("wolf-hit")]
        [TestCase("yu-hit")][TestCase("wang-ink")][TestCase("saria-heal")]
        [TestCase("gold-sweep")][TestCase("gold-hit")]
        public void AttackFlipbooksHaveFiniteWindowAndNoTerminalHold(string key)
        {
            var clip=DefenderVfxArt.Load(key);
            Assert.That(clip.Frames.Length,Is.GreaterThan(30));Assert.That(clip.At(.1),Is.Not.Null);
            Assert.That(clip.At(-1),Is.Null);Assert.That(clip.At(clip.Duration),Is.Null);Assert.That(clip.At(1000),Is.Null);
            Assert.That(clip.At(double.PositiveInfinity),Is.Null);
            foreach(var frame in clip.Frames)
            {
                Assert.That(frame.bounds.size.magnitude,Is.LessThan(15));
                Assert.That(frame.texture.width,Is.LessThanOrEqualTo(2048));
                Assert.That(frame.texture.filterMode,Is.EqualTo(FilterMode.Bilinear));
            }
        }
        [TestCase("wolf-wake")][TestCase("yu-fire")][TestCase("chess-link")][TestCase("saria-field")]
        public void PersistentSkillsUseSeamlessSimulationTimeLoops(string key)
        {
            var clip=DefenderVfxArt.Load(key);Assert.That(clip.Loop,Is.True);
            Assert.That(clip.At(.1),Is.SameAs(clip.At(clip.Duration+.1)));
            Assert.That(clip.At(-.1),Is.Null);
        }
    }
}
