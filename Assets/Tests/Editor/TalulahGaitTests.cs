using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class TalulahGaitTests
    {
        [Test]
        public void StanceMatchesTravelAndSwingClearsGroundWithContinuousLoop()
        {
            Vector2 a=TalulahRig.FootCycle(.1f,0),b=TalulahRig.FootCycle(.2f,0);
            Assert.That((b.x-a.x)+.1f*.72f,Is.EqualTo(0).Within(.00001),"Stance foot cancels body travel instead of sliding.");
            Assert.That(a.y,Is.EqualTo(b.y)); Assert.That(TalulahRig.FootCycle(.81f,0).y,Is.GreaterThan(.14f));
            Assert.That(Vector2.Distance(TalulahRig.FootCycle(.9999f,0),TalulahRig.FootCycle(0,0)),Is.LessThan(.001f));
            Assert.That(Vector2.Distance(TalulahRig.FootCycle(.6199f,0),TalulahRig.FootCycle(.6201f,0)),Is.LessThan(.001f));
        }
    }
}
