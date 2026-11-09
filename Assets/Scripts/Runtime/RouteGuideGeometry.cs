using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public readonly struct RouteGuideSpan
    {
        public readonly Vector3 From,To;
        public readonly double FromDistance,ToDistance;
        public readonly int EdgeIndex;
        public readonly bool Head;
        public RouteGuideSpan(Vector3 from,Vector3 to,double fromDistance,double toDistance,int edgeIndex,bool head)
        {From=from;To=to;FromDistance=fromDistance;ToDistance=toDistance;EdgeIndex=edgeIndex;Head=head;}
    }
    public readonly struct RouteGuideProjection
    {
        public readonly RouteGuideSpan Source;
        public readonly Vector2 From,To;
        public readonly bool HeadVisible;
        public RouteGuideProjection(RouteGuideSpan source,Vector2 from,Vector2 to,bool headVisible)
        {Source=source;From=from;To=to;HeadVisible=headVisible;}
    }
    public readonly struct WaveRouteGuideStroke
    {
        public readonly string LaneId;
        public readonly bool Open,Pulse;
        public readonly RouteGuideProjection Projection;
        public WaveRouteGuideStroke(string laneId,bool open,bool pulse,RouteGuideProjection projection)
        {LaneId=laneId;Open=open;Pulse=pulse;Projection=projection;}
    }

    /// <summary>Route-distance spans and projection into the root tactical canvas; owns no GUI group or clock.</summary>
    public static class RouteGuideGeometry
    {
        public const double PulseSpacing=1.1,PulseLength=.46,PulseSpeed=.8;

        public static List<RouteGuideSpan> FullPath(IReadOnlyList<Vector3> points)
        {
            if(points==null||points.Count<2)throw new ArgumentException("A route requires at least two points.",nameof(points));
            var result=new List<RouteGuideSpan>();double distance=0;
            for(int i=1;i<points.Count;i++)
            {
                double length=Vector3.Distance(points[i-1],points[i]);
                if(length>0)result.Add(new RouteGuideSpan(points[i-1],points[i],distance,distance+length,i-1,false));
                distance+=length;
            }
            return result;
        }

        public static List<RouteGuideSpan> Pulses(IReadOnlyList<Vector3> points,double simulationClock)
        {
            if(double.IsNaN(simulationClock)||double.IsInfinity(simulationClock)||simulationClock<0)
                throw new ArgumentOutOfRangeException(nameof(simulationClock));
            var edges=FullPath(points);var result=new List<RouteGuideSpan>();
            if(edges.Count==0)return result;
            double phase=simulationClock*PulseSpeed%PulseSpacing;
            double routeLength=edges[edges.Count-1].ToDistance;
            double start=Math.Floor(-phase/PulseSpacing)*PulseSpacing+phase;
            for(;start<routeLength;start+=PulseSpacing)
            {
                double end=Math.Min(routeLength,start+PulseLength);
                foreach(var edge in edges)
                {
                    double from=Math.Max(edge.FromDistance,start),to=Math.Min(edge.ToDistance,end);
                    if(to-from<=.0000001)continue;
                    double length=edge.ToDistance-edge.FromDistance;
                    Vector3 a=Vector3.LerpUnclamped(edge.From,edge.To,(float)((from-edge.FromDistance)/length));
                    Vector3 b=Vector3.LerpUnclamped(edge.From,edge.To,(float)((to-edge.FromDistance)/length));
                    // A pulse crossing a corner is split along both real edges, with one head at its end.
                    result.Add(new RouteGuideSpan(a,b,from,to,edge.EdgeIndex,Math.Abs(to-end)<.0000001));
                }
            }
            return result;
        }

        public static bool TryProject(RouteGuideSpan span,Func<Vector3,Vector3> worldToScreen,TacticalViewport viewport,
            Rect canvasClip,out RouteGuideProjection projection)
        {
            if(worldToScreen==null)throw new ArgumentNullException(nameof(worldToScreen));
            Vector3 screenA=worldToScreen(span.From),screenB=worldToScreen(span.To);
            projection=default;
            if(screenA.z<=0||screenB.z<=0)return false;
            Vector2 from=viewport.FromScreen(screenA),to=viewport.FromScreen(screenB);
            bool head=span.Head&&Inside(canvasClip,to);
            if(!TryClip(canvasClip,ref from,ref to)||(to-from).sqrMagnitude<.0001f)return false;
            projection=new RouteGuideProjection(span,from,to,head);return true;
        }

        // Numerical clipping keeps the canvas origin fixed while each stroke is rotated.
        public static bool TryClip(Rect clip,ref Vector2 from,ref Vector2 to)
        {
            Vector2 delta=to-from;float first=0,last=1;
            if(!ClipBoundary(-delta.x,from.x-clip.xMin,ref first,ref last)||
                !ClipBoundary(delta.x,clip.xMax-from.x,ref first,ref last)||
                !ClipBoundary(-delta.y,from.y-clip.yMin,ref first,ref last)||
                !ClipBoundary(delta.y,clip.yMax-from.y,ref first,ref last))return false;
            Vector2 origin=from;from=origin+delta*first;to=origin+delta*last;return true;
        }
        static bool ClipBoundary(float direction,float distance,ref float first,ref float last)
        {
            if(Mathf.Abs(direction)<.000001f)return distance>=0;
            float ratio=distance/direction;
            if(direction<0){if(ratio>last)return false;if(ratio>first)first=ratio;}
            else {if(ratio<first)return false;if(ratio<last)last=ratio;}
            return true;
        }
        static bool Inside(Rect rect,Vector2 point)=>point.x>=rect.xMin&&point.x<=rect.xMax&&point.y>=rect.yMin&&point.y<=rect.yMax;

        public static Matrix4x4 StrokeMatrix(Matrix4x4 canvasMatrix,Vector2 from,Vector2 to)
        {
            float angle=Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg;
            return canvasMatrix*Matrix4x4.TRS(new Vector3(from.x,from.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        }
    }
}
