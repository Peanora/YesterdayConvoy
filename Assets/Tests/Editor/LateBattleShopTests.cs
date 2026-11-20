using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class LateBattleShopTests
    {
        [Test]public void SecondStageChangesDefenseCoverageWithoutMutatingAuthoredSkills()
        {
            var first=BattleShopStages.Defenders(1,true);var second=BattleShopStages.Defenders(2,true);
            Assert.That(first.Zip(second,(a,b)=>a.position!=b.position).Count(v=>v),Is.EqualTo(4));
            for(int i=0;i<6;i++){Assert.That(first[i].skill.kind,Is.EqualTo(second[i].skill.kind));Assert.That(first[i].health,Is.EqualTo(second[i].health));}
        }
    }
}
