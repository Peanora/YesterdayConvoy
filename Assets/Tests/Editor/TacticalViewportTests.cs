using NUnit.Framework;
using UnityEngine;
namespace YesterdayConvoy.Tests
{
    public sealed class TacticalViewportTests
    {
        [TestCase(1280,720)][TestCase(1024,640)][TestCase(980,620)][TestCase(900,1000)]
        public void UniformScaleKeepsPointerAndCameraInTheSameCanvas(int width,int height)
        {
            var view=new TacticalViewport(width,height);
            var origin=view.ToScreen(Vector2.zero);var right=view.ToScreen(new Vector2(100,0));var down=view.ToScreen(new Vector2(0,100));
            Assert.That(Vector2.Distance(origin,right),Is.EqualTo(Vector2.Distance(origin,down)).Within(.001f));
            foreach(var point in new[]{Vector2.zero,new Vector2(1280,720),new Vector2(22,620),new Vector2(640,270)})
                Assert.That(Vector2.Distance(point,view.FromScreen(view.ToScreen(point))),Is.LessThan(.001f));
            Rect layout=new Rect(0,56,1280,496),camera=view.CameraRect(layout);
            Assert.That(camera.width*width/(camera.height*height),Is.EqualTo(1280f/496).Within(.0001f));
            Assert.That(camera.x*width,Is.EqualTo(view.Offset.x).Within(.001f));
            var borderPoint=view.FromTopLeft(new Vector2(0,0));
            if(view.Offset.sqrMagnitude>.01f)Assert.That(new Rect(0,0,1280,720).Contains(borderPoint),Is.False);
        }
    }
}
