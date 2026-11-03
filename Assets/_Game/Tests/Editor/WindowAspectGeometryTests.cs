using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class WindowAspectGeometryTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void DraggingAnyEdgeCouplesBothAxesAndPreservesTheOppositeAnchor(int edge)
        {
            var frame=new Vector2Int(16,39);var proposed=new RectInt(100,200,1168,759);
            var output=WindowAspectGeometry.Resize(proposed,frame,edge,new Vector2Int(1024,576));
            Assert.That(Mathf.Abs((output.width-frame.x)*9-(output.height-frame.y)*16),Is.LessThanOrEqualTo(16));
            if(edge==1||edge==4||edge==7)Assert.That(output.xMax,Is.EqualTo(proposed.xMax));else Assert.That(output.x,Is.EqualTo(proposed.x));
            if(edge==3||edge==4||edge==5)Assert.That(output.yMax,Is.EqualTo(proposed.yMax));else Assert.That(output.y,Is.EqualTo(proposed.y));
            Assert.That(output.width-frame.x,Is.GreaterThan(1024));Assert.That(output.height-frame.y,Is.GreaterThan(576));
        }
        [TestCase(1024,640)][TestCase(1920,1010)][TestCase(980,620)][TestCase(1280,720)]
        public void StartupAndMaximizeFitTheAvailableAreaWithoutStretching(int width,int height)
        {
            var output=WindowAspectGeometry.Fit(width,height);
            Assert.That(output.x,Is.LessThanOrEqualTo(width));Assert.That(output.y,Is.LessThanOrEqualTo(height));
            Assert.That(Mathf.Abs(output.x*9-output.y*16),Is.LessThanOrEqualTo(16));
        }
    }
}
